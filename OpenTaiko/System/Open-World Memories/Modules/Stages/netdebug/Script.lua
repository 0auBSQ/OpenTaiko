---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- netdebug/Script.lua — a tiny smoke test for the P2P NET core (the "OpenTaiko Online" protocol).
--
-- Run TWO copies of the game on the same machine:
--   In copy A press  H  to HOST a room (the room code is shown + written to this stage's netroom.txt /
--                       netroom.json so you can open/copy it, and so copy B can auto-read it on J).
--   In copy B press  J  to JOIN (reads netroom.json written by the host and connects to 127.0.0.1).
--   Then  B  broadcasts a ping to all peers,  R  (host) rotates the host role,  L  leaves,  Esc quits.
-- The panel shows your role / ids / peer roster and a live event log, so you can confirm joins, messages,
-- host-role hand-off and disconnects all flow correctly before the real stages are built on top.

-- positions, sizes and colours: Config/layout.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local LOG = Layout.log

local fontBig, fontMid, fontSmall
local log = {}            -- recent event lines (newest last)
local code = nil          -- the room code when hosting
local pingN = 0
local SW, SH = 1920, 1080

local function add(line)
    log[#log + 1] = line
    while #log > LOG.rows do table.remove(log, 1) end
end

function onStart()
    SW = INPUT:GetSurfaceWidth();  if SW <= 0 then SW = 1920 end
    SH = INPUT:GetSurfaceHeight(); if SH <= 0 then SH = 1080 end
    fontBig   = TEXT:Create(Layout.fonts.big)
    fontMid   = TEXT:Create(Layout.fonts.mid)
    fontSmall = TEXT:Create(Layout.fonts.small)
end
function activate() log = {}; code = nil; pingN = 0; add("Ready. H = host, J = join.") end
function deactivate() end
function afterSongEnum() end
function onDestroy() if NET then NET:Leave() end end

local function txt(font, str, c)
    return font:GetText(str, false, Layout.text.max_width,
        Config.color(c or Layout.text.default_color),
        Config.color(Layout.text.outline_color))
end

local function host()
    NET:SetLocalPlayer('{"name":"HostTester","char":"Bot","plate":0}')
    code = NET:CreateRoom("netdebug", "payload-test", 8)
    if code == nil then add("CreateRoom FAILED (port in use / no socket)"); return end
    STORAGE:WriteText("netroom.txt", code)                       -- human-openable / copy-pasteable
    STORAGE:WriteText("netroom.json", '{"code":"' .. code .. '"}') -- the joiner auto-reads this
    add("Hosting. Code written to netroom.txt (len " .. #code .. ").")
end

local function join()
    local d = JSONLOADER:JsonParseFileAny("netroom.json")
    local c = d and JSONLOADER:JsonGet(d, "code") or nil
    if not c then add("No netroom.json yet — host first (press H in the other copy)."); return end
    NET:SetLocalPlayer('{"name":"GuestTester-' .. math.random(100, 999) .. '","char":"Bot","plate":0}')
    if NET:JoinRoom(c) then add("Joining " .. string.sub(c, 1, LOG.join_code_chars) .. "...") else add("JoinRoom: bad code.") end
end

function update(ts)
    if INPUT:KeyboardPressed("Escape") or INPUT:Pressed("Cancel") then
        NET:Leave(); return Exit("stage", "_title")
    end
    if INPUT:KeyboardPressed("H") then host() end
    if INPUT:KeyboardPressed("J") then join() end
    if INPUT:KeyboardPressed("B") then
        pingN = pingN + 1
        NET:Broadcast("ping", "n=" .. pingN .. " from=" .. NET:SelfId())
        add("-> broadcast ping n=" .. pingN)
    end
    if INPUT:KeyboardPressed("R") then if NET:IsHost() then NET:RotateHost() else add("Only the server can rotate.") end end
    if INPUT:KeyboardPressed("L") then NET:Leave(); add("Left the room.") end

    -- drain every queued network event this frame
    while true do
        local e = NET:Poll(); if e == nil then break end
        if e.Type == "connected" then add("CONNECTED " .. e.Data)
        elseif e.Type == "joined" then add("JOINED peer " .. e.Peer .. " " .. e.Data)
        elseif e.Type == "left" then add("LEFT peer " .. e.Peer)
        elseif e.Type == "message" then add("MSG [" .. e.Channel .. "] from " .. e.Peer .. ": " .. e.Data)
        elseif e.Type == "hostrole" then add("HOST ROLE -> peer " .. e.Peer)
        elseif e.Type == "roomclosed" then add("ROOM CLOSED (host gone).")
        elseif e.Type == "error" then add("ERROR: " .. e.Data)
        else add("EVENT " .. e.Type) end
    end
    return nil
end

function draw()
    local T, K = Layout.title, Layout.keys
    txt(fontBig, "Net Debug  —  OpenTaiko Online smoke test", T.color):Draw(T.x, T.y)
    txt(fontSmall, "H host   J join   B broadcast ping   R rotate host (server)   L leave   Esc back", K.color):Draw(K.x, K.y)

    local role = NET:IsHost() and "HOST/SERVER" or (NET:Connected() and "CLIENT" or "(not connected)")
    local line = string.format("Role: %s    self id: %d    host-role id: %d    connected: %s    peers: %d",
        role, NET:SelfId(), NET:HostRoleId(), tostring(NET:Connected()), NET:PeerCount())
    local S = Layout.status
    txt(fontMid, line, S.color):Draw(S.x, S.y)

    if code ~= nil then
        local RC = Layout.room_code
        txt(fontSmall, "Room code (also in netroom.txt): " .. string.sub(code, 1, RC.max_chars) .. (#code > RC.max_chars and "..." or ""), RC.color):Draw(RC.x, RC.y)
    end
    local R = Layout.roster
    txt(fontSmall, "Roster: " .. NET:PeersJson(), R.color):Draw(R.x, R.y)

    txt(fontMid, "Event log", LOG.title_color):Draw(LOG.title_x, LOG.title_y)
    local y = LOG.y
    for i = 1, #log do
        txt(fontSmall, log[i], LOG.color):Draw(LOG.x, y); y = y + LOG.row_height
    end
end
