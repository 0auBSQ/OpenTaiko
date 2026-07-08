---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- myroom/online.lua — the P2P "visit my room" logic, built on the NET core. Kept OUT of Script.lua,
-- which owns the stage state machine + drawing and calls in here (mirrors kart_racing/online.lua and
-- doom/online.lua). Script hands over a small `ctx` of accessor closures so this module can swap the
-- room layout + respawn the player without owning those locals.
--
-- Model: the room CREATOR hosts (their room is the shared space); friends JOIN by pasting a code and
-- walk around the host's room as named generic avatars. Guests can't edit, use the phone, or use the
-- computer. The host leaving closes the room (everyone is sent home). A guest leaving (door / Esc) just
-- ends the visit and drops them back in their own room.
--
-- PAYLOAD v2: every v1 key ships unchanged in meaning; v2 adds `"v":2` + `"wallItems"` (id/c/r/mount,
-- phone included). Legacy-renderable wall items (clock) are still mirrored inside `furniture` by
-- Room:toTable(), so an old client visiting a v2 host keeps seeing them; unknown ids fall back to the
-- red placeholder box. A payload WITHOUT `v` is decoded as v1 and migrated by Room:loadTable().
-- Room layout always crosses through Room:toTable()/loadTable(), so stage + net stay consistent.
--
-- SECURITY: the room layout crosses the wire as plain json only, we never load() network data (that
-- would be remote code execution). Maps in floorDeco/wallPaint/inventory ship as [key,value] arrays so
-- the decoder needs no key-enumeration helper; furniture/wallItems ship as object arrays.

local I18N = require("i18n")

local MO = {}
local C   -- the context (set by MO.init)

-- shared network state (Script.lua aliases this as `net`)
MO.net = {
    online = false, isHost = false, connecting = false, gotRoom = false, roomGone = false,
    code = nil, msg = nil,
    nameByPeer = {},        -- peerId -> display name
    posByPeer  = {},        -- peerId -> { x,z, tx,tz, dir, moving, frameT }  (remote avatars, lerped)
    sendT = 0,
}
local net = MO.net

local floor, exp = math.floor, math.exp

function MO.init(ctx) C = ctx end

-- ── identity ────────────────────────────────────────────────────────────────────────────────────
-- JSON string escaper (robust for player names incl. control chars; UTF-8 bytes pass through)
local function jstr(s)
    return '"' .. tostring(s):gsub('[%c\\"]', function(ch)
        local m = { ['"'] = '\\"', ['\\'] = '\\\\', ['\n'] = '\\n', ['\r'] = '\\r', ['\t'] = '\\t' }
        return m[ch] or string.format('\\u%04x', string.byte(ch))
    end) .. '"'
end
function MO.myName()
    local sf = GetSaveFile(MO.playerIndex or 0); local n = sf and sf.Name
    if n == nil or n == "" then n = "Player" end
    return n
end
function MO.selfInfo() return '{"name":' .. jstr(MO.myName()) .. '}' end
function MO.isGuest() return net.online and not net.isHost end
function MO.playerCount() local c = 0; for _ in pairs(net.nameByPeer) do c = c + 1 end return c end

-- ── room layout <-> JSON (NEVER load() network data) ─────────────────────────────────────────────
local function encFurniture(furn)
    local b = {}
    for i, it in ipairs(furn) do
        -- "on" (stacked on a surface) is a v2-additive key: older v2 clients ignore it and draw
        -- the item at floor level; stacked items are ground furniture, so nothing extra is
        -- mirrored into the v1 legacy wall list
        b[i] = string.format('{"id":%s,"c":%d,"r":%d,"facing":%d%s%s}', jstr(it.id),
            it.c or 0, it.r or 0, it.facing or 0, it.on and ',"on":true' or "",
            it.lit == false and ',"lit":false' or "")   -- only OFF needs sending (default on)
    end
    return "[" .. table.concat(b, ",") .. "]"
end
local function encWallItems(items)
    local b = {}
    for i, it in ipairs(items) do
        b[i] = string.format('{"id":%s,"c":%d,"r":%d,"mount":%s}', jstr(it.id), it.c or 0, it.r or 0, jstr(it.mount or "low"))
    end
    return "[" .. table.concat(b, ",") .. "]"
end
local function encMap(m, valIsNum)
    local b = {}
    for k, v in pairs(m) do b[#b + 1] = "[" .. jstr(k) .. "," .. (valIsNum and string.format("%d", v) or jstr(v)) .. "]" end
    return "[" .. table.concat(b, ",") .. "]"
end
function MO.roomToJson(room)
    local t = room:toTable()
    return string.format('{"v":2,"tier":%d,"iw":%d,"ih":%d,"exitCol":%d,"furniture":%s,"floorDeco":%s,"wallPaint":%s,"inventory":%s,"wallItems":%s}',
        t.tier or 1, t.iw or 5, t.ih or 5, t.exitCol or 2,
        encFurniture(t.furniture or {}), encMap(t.floorDeco or {}, false), encMap(t.wallPaint or {}, false),
        encMap(t.inventory or {}, true), encWallItems(t.wallItems or {}))
end

local function decFurniture(arr)
    local out = {}; local n = arr and JSONLOADER:JsonCount(arr) or 0
    for i = 1, n do
        local e = JSONLOADER:JsonGet(arr, i)
        out[i] = { id = JSONLOADER:JsonGet(e, "id"), c = floor(JSONLOADER:JsonGet(e, "c") or 0),
                   r = floor(JSONLOADER:JsonGet(e, "r") or 0), facing = floor(JSONLOADER:JsonGet(e, "facing") or 0),
                   on = (JSONLOADER:JsonGet(e, "on") == true) and true or nil,
                   lit = (JSONLOADER:JsonGet(e, "lit") == false) and false or nil }
    end
    return out
end
local function decWallItems(arr)
    local out = {}; local n = arr and JSONLOADER:JsonCount(arr) or 0
    for i = 1, n do
        local e = JSONLOADER:JsonGet(arr, i)
        local mount = JSONLOADER:JsonGet(e, "mount")
        out[i] = { id = JSONLOADER:JsonGet(e, "id"), c = floor(JSONLOADER:JsonGet(e, "c") or 0),
                   r = floor(JSONLOADER:JsonGet(e, "r") or 0), mount = (mount == "high") and "high" or "low" }
    end
    return out
end
local function decMap(arr, valIsNum)
    local out = {}; local n = arr and JSONLOADER:JsonCount(arr) or 0
    for i = 1, n do
        local pair = JSONLOADER:JsonGet(arr, i)
        local k = pair and JSONLOADER:JsonGet(pair, 1); local v = pair and JSONLOADER:JsonGet(pair, 2)
        if k ~= nil then out[k] = valIsNum and floor(v or 0) or v end
    end
    return out
end
-- returns a plain table in the Room:toTable() shape. A missing "v" marks a v1 peer's payload — the
-- table is returned WITHOUT v so Room:loadTable() runs its v1 migration on it.
function MO.roomFromJson(json)
    local root = JSONLOADER:JsonParseStringAny(json); if not root then return nil end
    local t = {
        tier    = floor(JSONLOADER:JsonGet(root, "tier") or 1),
        iw      = floor(JSONLOADER:JsonGet(root, "iw") or 5),
        ih      = floor(JSONLOADER:JsonGet(root, "ih") or 5),
        exitCol = floor(JSONLOADER:JsonGet(root, "exitCol") or 2),
        furniture = decFurniture(JSONLOADER:JsonGet(root, "furniture")),
        floorDeco = decMap(JSONLOADER:JsonGet(root, "floorDeco"), false),
        wallPaint = decMap(JSONLOADER:JsonGet(root, "wallPaint"), false),
        inventory = decMap(JSONLOADER:JsonGet(root, "inventory"), true),
    }
    local v = JSONLOADER:JsonGet(root, "v")
    if v ~= nil and floor(v) >= 2 then
        t.v = floor(v)
        t.wallItems = decWallItems(JSONLOADER:JsonGet(root, "wallItems"))
    end
    return t
end

-- ── roster ──────────────────────────────────────────────────────────────────────────────────────
function MO.refreshRoster()
    local names = {}
    local arr = JSONLOADER:JsonParseStringAny(NET:PeersJson()); local n = arr and JSONLOADER:JsonCount(arr) or 0
    for i = 1, n do
        local e = JSONLOADER:JsonGet(arr, i); local id = e and JSONLOADER:JsonGet(e, "id")
        if id then
            id = floor(id)
            local io = e and JSONLOADER:JsonGet(e, "info"); io = io and JSONLOADER:JsonParseStringAny(io) or nil
            names[id] = (io and JSONLOADER:JsonGet(io, "name")) or ("Visitor " .. id)
        end
    end
    net.nameByPeer = names
    local selfId = NET:SelfId()
    for id in pairs(net.posByPeer) do if not names[id] or id == selfId then net.posByPeer[id] = nil end end
end

-- ── session control ─────────────────────────────────────────────────────────────────────────────
local function resetState()
    net.online, net.isHost, net.connecting, net.gotRoom, net.roomGone = false, false, false, false, false
    net.code, net.msg = nil, net.msg
    net.nameByPeer, net.posByPeer, net.sendT = {}, {}, 0
end

function MO.host()
    NET:SetLocalPlayer(MO.selfInfo())
    local code = NET:CreateRoom("myroom", "", 8)
    if not code or code == "" then net.msg = I18N.tr("Could not open the room."); return false end
    net.code, net.online, net.isHost, net.connecting = code, true, true, false
    net.nameByPeer, net.posByPeer = {}, {}
    STORAGE:WriteLobbyCode("myroom.txt", code)
    STORAGE:RevealLobbyCodes()
    MO.refreshRoster()
    net.msg = I18N.tr("Room open! The code was saved to a folder — share it so friends can Join by phone.")
    return true
end

function MO.join(code)
    code = (code or ""):gsub("%s", "")               -- strip whitespace from a pasted code
    if code == "" then net.msg = I18N.tr("No code entered."); return false end
    local sid = NET:PeekStageId(code)
    if sid ~= "myroom" then net.msg = sid and ("That code is for a '" .. sid .. "' room, not My Room.") or "That code isn't valid."; return false end
    NET:SetLocalPlayer(MO.selfInfo())
    net.connecting, net.isHost, net.gotRoom = true, false, false
    net.nameByPeer, net.posByPeer = {}, {}
    NET:JoinRoom(code)
    net.msg = I18N.tr("Connecting…")
    return true
end

function MO.leave()
    if net.online or net.connecting then NET:Leave() end
    resetState()
end

function MO.sendRoomTo(id) NET:SendTo(id, "room", MO.roomToJson(C.getRoom())) end
-- the host re-shares the layout after an edit so visitors stay in sync
function MO.onRoomChanged() if net.online and net.isHost then NET:Broadcast("room", MO.roomToJson(C.getRoom())) end end

-- ── presence sync ───────────────────────────────────────────────────────────────────────────────
function MO.broadcastPos(dt, x, z, dir, moving)
    if not net.online then return end
    net.sendT = (net.sendT or 0) + dt
    if net.sendT < 0.066 then return end             -- ~15 Hz
    net.sendT = 0
    NET:Broadcast("pos", string.format('{"x":%.2f,"z":%.2f,"d":%d,"m":%d}', x, z, dir or 2, moving and 1 or 0))
end
function MO.applyPos(id, data)
    local s = JSONLOADER:JsonParseStringAny(data); if not s then return end
    local x, z = JSONLOADER:JsonGet(s, "x"), JSONLOADER:JsonGet(s, "z")
    local d = floor(JSONLOADER:JsonGet(s, "d") or 2)
    local m = JSONLOADER:JsonGet(s, "m"); m = (m == 1 or m == true)
    local p = net.posByPeer[id]
    if not p then net.posByPeer[id] = { x = x or 0, z = z or 0, tx = x or 0, tz = z or 0, dir = d, moving = m, frameT = 0 }
    else p.tx = x or p.tx; p.tz = z or p.tz; p.dir = d; p.moving = m end
end
function MO.lerpRemotes(dt)
    local k = 1 - exp(-12.0 * dt)
    for _, p in pairs(net.posByPeer) do
        p.x = p.x + (p.tx - p.x) * k
        p.z = p.z + (p.tz - p.z) * k
        p.frameT = p.moving and (p.frameT + dt) or 0
    end
end

-- ── event drain (call every frame while online/connecting) ──────────────────────────────────────
function MO.drain()
    while true do
        local e = NET:Poll(); if e == nil then break end
        local ty = e.Type
        if ty == "connected" then
            net.online, net.connecting = true, false; MO.refreshRoster()
        elseif ty == "joined" then
            MO.refreshRoster(); if net.isHost then MO.sendRoomTo(e.Peer) end
        elseif ty == "left" then
            net.posByPeer[e.Peer] = nil; MO.refreshRoster()
        elseif ty == "roomclosed" then
            net.roomGone = true
        elseif ty == "error" then
            net.msg = e.Data or "Network error."; if not net.online then net.connecting = false end
        elseif ty == "message" then
            if e.Channel == "pos" then
                MO.applyPos(e.Peer, e.Data)
            elseif e.Channel == "room" then
                local t = MO.roomFromJson(e.Data)
                if t then C.applyRoom(t); C.rebuildRoom(); C.spawnAtEntrance(); net.gotRoom = true end
            end
        end
    end
end

return MO
