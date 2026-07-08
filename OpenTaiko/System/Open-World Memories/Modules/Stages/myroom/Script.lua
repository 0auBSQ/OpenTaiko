---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- myroom/Script.lua — the personal room (story-mode My Room) on the OWM3d engine.
-- Walk the room on a physics character, interact with the computer (pc.lua save-file screen) and the
-- phone (PopUI menus + the Lib/dialogue landlord), edit furniture/wall items (editmode.lua on PopUI),
-- and leave through the door. Persisted per SAVE FILE (save id). P2P visiting lives in online.lua.
--
-- vs the old isoengine build: the room is a LIT cosy interior (warm ceiling + window lights, soft
-- shadows, subtle diorama grade), the floor is a raised platform the physics character actually stands
-- on (camera boom collides too), and the day/night sky stays visible through the window (drawSky).

local OWM      = require("OWM3d")
local A        = require("assets")
local Room     = require("room")
local Dialogue = require("dialogue")
local PCS      = require("pc")
local Edit     = require("editmode")
local PopUI    = require("PopUI")
local I18N      = require("i18n")
local MO       = require("online")          -- P2P "visit my room" (lobby + presence); see online.lua
local net      = MO.net

local SCREEN_W, SCREEN_H = 1920, 1080
local RW, RH = 1920, 1080
local FOV = 20
local rad, sin, cos, sqrt = math.rad, math.sin, math.cos, math.sqrt
local FY = Room.FLOOR_Y

local world, map, player, hud
local room
local edit, pcScreen, dlg
local lastTs = 0
local px, py, pz = 0, FY, 0    -- player world position (feet)
local pdir = 2                 -- facing 1↙ 2↘ 3↗ 4↖ (CharaTemplate direction)
local pframeT = 0              -- walk-cycle timer
local pmoving = false
local moveSpeed = 4.2          -- units/sec
local prompt = nil             -- current interaction prompt ("computer"/"phone"/"exit"/"lamp")
local focusKey = nil           -- which in-range interactable is focused (stable across frames; Tab cycles)
local interCount = 0           -- how many interactables are in range this frame (draw shows [Tab] switch when >1)
local msg, msgT = nil, 0       -- transient on-screen message
local editSavedCam = nil       -- full camera state saved on edit-enter, restored on edit-exit
local store = nil              -- LuaDataStorage for persistence
local playerIndex = 0          -- which of the 5 local saves' room we view/edit (chosen at entry)
local openPlayerSelect         -- forward decl (defined after PHONE_THEME; called from activate)
local mode = "play"            -- "play" | "dialogue" | "pc" | "phone" | "edit"
local phoneUI = nil            -- PopUI manager for the phone (menu / number / join panes)
local phoneJustOpened = false  -- consume the keypress edge that opened the phone
local phoneFlow = nil          -- landlord conversation step
local phoneSfx = {}            -- cached dialable-number SFX (stem → sound|false); disposed in onDestroy
local bombFx = nil             -- the "Canabi" bomb-storm state (declared here so onDestroy sees it)

-- ── persistence (local save only; the network payload goes through online.lua as JSON) ────────────
local function serialize(t)
    local function enc(v)
        local tv = type(v)
        if tv == "table" then
            local b = { "{" }
            local arr = (#v > 0)
            for k, val in pairs(v) do
                if arr then b[#b + 1] = enc(val) .. ","
                else b[#b + 1] = "[" .. (type(k) == "string" and string.format("%q", k) or tostring(k)) .. "]=" .. enc(val) .. "," end
            end
            b[#b + 1] = "}"; return table.concat(b)
        elseif tv == "string" then return string.format("%q", v)
        else return tostring(v) end
    end
    return enc(t)
end
local function deserialize(s)
    if not s or s == "" then return nil end
    local f = load("return " .. s)
    if not f then return nil end
    local ok, v = pcall(f)
    return ok and v or nil
end

local function curSave() return GetSaveFile(playerIndex) end
-- rooms are keyed by the save's UUID (SaveUID) so each player save owns its own room; fall back to the
-- slot index if a save predates the SaveUID migration.
local function saveKey()
    local sf = curSave()
    local uid = sf and sf.SaveUID
    if uid == nil or uid == "" then uid = "slot" .. tostring(playerIndex) end
    return "room_" .. uid
end
-- load the CURRENT player's room INTO the existing room object (Room:reset keeps the same table so
-- edit/MO references stay valid); a player with no saved room gets a fresh default. Coin-shop furniture
-- purchases (per-save global counters) are claimed into the inventory here, then persisted right away.
local saveRoom   -- forward decl (loadRoom persists the shop-grant claim through it)
local function loadRoom()
    if room == nil then room = Room.newDefault() else room:reset() end
    store = store or DATABASE:OpenLocalDatabase("myroom")
    local raw = store and store:Read(saveKey()) or nil
    local t = deserialize(raw)
    if t then room:loadTable(t) end
    local sf = curSave()
    if sf then
        local gained = room:drainShopGrants(function(n) return sf:GetGlobalCounter(n) end)
        if gained then saveRoom() end   -- persist the claim ledger + new stock immediately (idempotent)
    end
end
saveRoom = function()
    if net.online and not net.isHost then return end   -- a visitor must never overwrite their own save with the host's room
    if store and room then store:Write(saveKey(), serialize(room:toTable())) end
end

-- ── build / rebuild the 3D room ───────────────────────────────────────────────────────────────────
local function rebuild()
    -- a rebuild (edit / lamp toggle / extend) must NOT jolt the camera: preserve the live orbit,
    -- ZOOM and focus when the room already exists; only frame fresh on the first build.
    local hadRoom = (map ~= nil) and (world.cam ~= nil)
    local keepYaw   = world.cam and world.cam.yaw or 45
    local keepPitch = world.cam and world.cam.pitch or -33
    local keepDist  = hadRoom and world.cam.dist or nil
    local keepTx, keepTy, keepTz
    if hadRoom then keepTx, keepTy, keepTz = world.cam.tx, world.cam.ty, world.cam.tz end

    map = world:loadMap("room")
    -- fit distance from interior size + fov (used only for the first framing + the zoom limits)
    local span = math.max(room.iw, room.ih) + 3
    local fit = span / (2 * math.tan(rad(FOV / 2))) * 1.05
    world.cam:setRig{ yaw = keepYaw, pitch = keepPitch, fov = FOV, dist = keepDist or fit }
    -- NOT physical: the wall clamp + zoom limits keep the framing sane; the boom stays off so the
    -- camera never jumps off geometry in the small room
    world.cam.collide = false
    world.cam.minDist, world.cam.maxDist = fit * 0.45, fit * 1.6
    local cx, cz = room:centerWorld(world)
    if hadRoom then world.cam:setTarget(keepTx, keepTy, keepTz)
    else world.cam:setTarget(cx, FY + 0.4, cz) end
    if world.scene.SetShadowArea then world.scene:SetShadowArea(math.max(room.iw, room.ih) + 5) end
    -- warm cosy interior: a soft ceiling lamp + a cool spill from the window (re-set after every
    -- loadMap — unloading a map clears the scene light list). Kept gentle so the room isn't blown out.
    local gh = room:gridH()
    local lights = {
        { x = cx, y = FY + 2.7, z = cz, r = 1.0, g = 0.86, b = 0.62, intensity = 0.95, range = 8 },
        { x = (room.winC0 + room.winC1 + 1) / 2, y = FY + 1.9, z = gh - 1.7,
          r = 0.75, g = 0.85, b = 1.0, intensity = 0.4, range = 5.5 },
    }
    for _, l in ipairs(room:lampLights(world)) do lights[#lights + 1] = l end   -- lit floor lamps
    world.daynight:setLights(lights)
end

local function spawnAtEntrance()
    local sc, sr = room:spawnCell()
    px, pz = world:cellToWorld(sc, sr)
    py = FY
    player:setPos(px, py + 0.05, pz)
end


-- ── lifecycle ─────────────────────────────────────────────────────────────────────────────────────
function onStart()
    -- gfont (LuaGlyphText) drives the UTF-8-aware layout: the classic LuaText path walks the string
    -- BYTE-by-byte, so multi-byte Japanese rendered as empty glyphs — the landlord/number messages
    -- "disappeared" in JA. A glyph font makes them render in every language. theme.text = white +
    -- the dialogue's black outline = readable white-with-border messages on any background.
    dlg = Dialogue.new({ gfont = TEXT:CreateGlyphCached(26),
                         fonts = { name = TEXT:Create(30), text = TEXT:Create(26) }, portraits = {},
                         theme = { text = { 255, 255, 255 } } })

    -- Bake the coin-shop furniture previews ONCE as SHARED textures (the shop reads them by key,
    -- reusing My Room's models without re-declaring them). GLB/builder furniture is rendered on the CPU
    -- rasterizer and captured (Edit.bakeThumbnails → ShareCanvasAs); paint/floor swatches just share
    -- their surface PNGs. Shared textures outlive this stage, so one bake serves the whole session.
    local ok, err = pcall(function()
        local n = Edit.bakeThumbnails(Room.SHOP_MODEL_IDS, "myroom_thumb_", 256)
        for id, file in pairs(Room.SHOP_SWATCH_TEX) do
            SHARED:SetSharedTexture("myroom_thumb_" .. id, file)
        end
        if debugLog then debugLog("myroom: baked " .. tostring(n) .. "/" .. tostring(#Room.SHOP_MODEL_IDS) .. " shop thumbnails") end
    end)
    if not ok and debugLog then debugLog("myroom: shop thumbnail bake FAILED: " .. tostring(err)) end
end

function reloadLanguage() I18N.detect() end

function activate()
    lastTs = 0
    I18N.detect()
    INPUT:SetMouseLocked(false)
    if world ~= nil then return end
    LOADING:Add("Room", 5, function()
        world = OWM.World.new{ rw = RW, rh = RH, screenW = SCREEN_W, screenH = SCREEN_H,
                               fov = FOV, yaw = 45, pitch = -33, lit = true }
        world:setDiorama(2.0, 1.1, 0.22, 0.26)     -- gentle cosy grade (softer bloom, not blown out)
        world:setFog(false)                        -- an interior needs no distance fog
        A.buildAll(world.scene)
        loadRoom()
        world:registerMap("room", { type = "proc", build = function(w) return room:buildMap(w) end })
        player = world.phys:newCharacter{ radius = 0.32, accel = 13, decel = 16 }
        hud = PopUI.new{}
        pcScreen = PCS.new()
        edit = Edit.new(room, world, function() rebuild(); saveRoom(); MO.onRoomChanged() end)
        MO.init({
            getRoom         = function() return room end,
            applyRoom       = function(t) room:loadTable(t) end,
            rebuildRoom     = function() rebuild() end,
            spawnAtEntrance = function() spawnAtEntrance() end,
        })
        rebuild()
        spawnAtEntrance()
        openPlayerSelect()          -- pick whose room to view/edit (skips itself when only one save)
    end)
end

function deactivate()
    GLOBALCAMERA:Reset()
    saveRoom()
    if pcScreen and pcScreen:active() then pcScreen:close() end
    if mode == "edit" and edit then edit:leave() end
    if phoneUI then phoneUI:disposeWidgets(); phoneUI = nil end
    if hud then hud:disposeWidgets(); hud = nil end
    -- safety net: free any 3D preview icon (edit grid / popup) its owner missed
    pcall(function() OWM.ModelIcon.disposeAll() end)
    if world ~= nil then world.scene:Dispose(); world = nil; player = nil; map = nil end
    mode = "play"
end

function afterSongEnum() end

function onDestroy()
    MO.leave()
    if store then store:Dispose(); store = nil end
    PCS.disposeBgm()
    if Edit.disposeSfx then Edit.disposeSfx() end
    for _, s in pairs(phoneSfx) do if s then pcall(function() s:Dispose() end) end end
    phoneSfx = {}; bombFx = nil
end

local function kd(k) return INPUT:KeyboardPressing(k) end
local function kp(k) return INPUT:KeyboardPressed(k) end

-- every interactable the player is in range of, as an ordered list (exit → computer/lamp → phone).
-- Each entry: { kind, it (furniture, when applicable), key (stable id for keeping focus) }.
local function interactablesInRange()
    local list = {}
    local pc, pr = world:worldToCell(px, pz)
    if room:cellType(pc, pr) == "E" then list[#list + 1] = { kind = "exit", key = "exit" } end
    if not MO.isGuest() then
        local seen = {}
        for _, d in ipairs({ { 0, 0 }, { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }) do
            local it, cat = room:furnitureAt(pc + d[1], pr + d[2])
            if it and cat and not seen[it] then
                if cat.computer then
                    seen[it] = true; list[#list + 1] = { kind = "computer", it = it, key = "computer:" .. tostring(it) }
                elseif it.id == "floorlamp" then
                    seen[it] = true; list[#list + 1] = { kind = "lamp", it = it, key = "lamp:" .. tostring(it) }
                elseif cat.interact == "pod" then
                    seen[it] = true; list[#list + 1] = { kind = "pod", it = it, key = "pod:" .. tostring(it) }
                end
            end
        end
        local fc, fr = room:phoneCell()
        if math.abs(pc - fc) + math.abs(pr - fr) <= 1 then list[#list + 1] = { kind = "phone", key = "phone" } end
    end
    return list
end

-- highlight the focused interactable by GLOWING the item itself (a pulsing emissive on its own 3D
-- object) rather than a square on the ground. The exit (a door cell, no model) keeps a soft footprint
-- square. The previous frame's glow is restored to its base emissive before applying the new one.
local function interactGlow(focused)
    local scene = world.scene
    if world._glow then                                   -- restore last frame's glowed object(s)
        for _, e in ipairs(world._glow) do
            if scene.ObjSetEmissive then pcall(function() scene:ObjSetEmissive(e.o, e.r, e.g, e.b) end) end
        end
        world._glow = nil
    end
    scene:ObjSetPass(world.hiliteObj, 1, 110, 185, 255, 0)   -- clear the footprint layer
    scene:ObjBegin(world.hiliteObj)
    if not focused then return end

    -- the focused item's live model instance: a ground GLB (_propInst) or the wall phone (_wallGlb)
    local inst = nil
    if focused.kind == "phone" then
        local p = room:phoneItem()
        inst = p and room._wallGlb and room._wallGlb[p] or nil
    elseif focused.it then
        inst = world._propInst and world._propInst[focused.it] or nil
    end

    if inst and scene.ObjSetEmissive then
        -- pulse an emissive ADD on the item's own object(s); the lit lamp keeps its warm base emissive
        local br, bg, bb = 0, 0, 0
        if focused.kind == "lamp" and focused.it and focused.it.lit ~= false then br, bg, bb = 0.9, 0.72, 0.36 end
        local p = 0.30 + 0.30 * (0.5 + 0.5 * sin(lastTs * 0.006))   -- 0.30 .. 0.90 glow
        world._glow = {}
        for _, o in ipairs(inst.objs or { inst.obj }) do
            world._glow[#world._glow + 1] = { o = o, r = br, g = bg, b = bb }
            pcall(function() scene:ObjSetEmissive(o, br + p, bg + p, bb + p) end)
        end
        return
    end

    -- exit / model-less interactable: a soft pulsing footprint square
    local gh = room:gridH()
    local pulse = 70 + math.floor(80 * (0.5 + 0.5 * sin(lastTs * 0.006)))
    scene:ObjSetPass(world.hiliteObj, 1, 110, 185, 255, pulse)
    scene:ObjBegin(world.hiliteObj)
    if focused.kind == "exit" then
        local pc, pr = world:worldToCell(px, pz)
        local x0, z0 = pc, gh - 1 - pr
        scene:ObjAddQuadFlat(world.hiliteObj, x0 + 0.06, FY + 0.06, z0 + 0.06, x0 + 0.94, FY + 0.06, z0 + 0.06,
            x0 + 0.94, FY + 0.06, z0 + 0.94, x0 + 0.06, FY + 0.06, z0 + 0.94)
    end
end

-- ── phone (PopUI menus; the landlord stays a Lib/dialogue conversation) ───────────────────────────
local PHONE_THEME = {
    colors = {
        surface  = { 246, 244, 250, 255 }, surface2 = { 224, 226, 240, 255 },
        primary  = { 110, 140, 210, 255 }, primary2 = { 78, 104, 176, 255 },
        outline  = { 52, 58, 92, 255 },    text = { 52, 58, 92, 255 },
    },
}
local function closePhone()
    if phoneUI then phoneUI:disposeWidgets(); phoneUI = nil end
    if mode == "phone" then mode = "play" end
end

-- ── dialable numbers / easter eggs (data/phone_numbers.json) ──────────────────────────────────────
local phoneDoc, phoneDocTried = nil, false
local function phoneNumbersDoc()
    if not phoneDocTried then
        phoneDocTried = true
        pcall(function() phoneDoc = JSONLOADER:LoadJson("data/phone_numbers.json") end)
    end
    return phoneDoc
end
local function playPhoneSound(stem)
    if phoneSfx[stem] == nil then
        local s = nil
        -- easter-egg sounds live in the STAGE's own Sounds/ folder (e.g. myroom/Sounds/egg.ogg) —
        -- NOT the skin-wide ../../../Sounds/ (that only has the shared UI sfx like Error.ogg)
        pcall(function() s = SOUND:CreateSFX("Sounds/" .. stem .. ".ogg") end)
        if s then pcall(function() s:SetVolume(100) end) end
        phoneSfx[stem] = s or false
    end
    if phoneSfx[stem] then pcall(function() phoneSfx[stem]:Play() end) end
end
local function startBombs() bombFx = { t = 0, dur = 3.0, next = 0 } end  -- { t, dur, next }
local function dialPhone(input)
    local key = tostring(input or ""):gsub("%s+", ""):lower()
    local doc = phoneNumbersDoc()
    local ev = nil
    pcall(function() ev = doc and doc[key] or nil end)
    if ev == nil then                                 -- unassigned number → the placeholder
        local n = (input and input ~= "" and input) or I18N.tr("the number")
        mode = "dialogue"; phoneFlow = nil
        dlg:start({ { name = "", text = I18N.trf("You dial %s... It rings, and rings. Nobody picks up.", n) } })
        return
    end
    local name   = JSONLOADER:ExtractText(ev["name"])
    local text   = JSONLOADER:ExtractText(ev["text"])
    local sound  = JSONLOADER:ExtractText(ev["sound"])
    local effect = JSONLOADER:ExtractText(ev["effect"])
    if name then name = I18N.tr(name) end        -- localized via lang/ja.lua (English text = the key)
    if text then text = I18N.tr(text) end
    if sound and sound ~= "" and (text == nil or text == "") then
        playPhoneSound(sound); mode = "play"; return  -- sound-only (e.g. 1122 → egg.ogg), no message
    end
    if sound and sound ~= "" then playPhoneSound(sound) end
    if effect == "bombs" then startBombs() end
    mode = "dialogue"; phoneFlow = nil
    dlg:start({ { name = name or "", text = text or "" } })
end

-- the bomb storm: explosion bursts around the player + screen shake, ticked every frame while active
local function tickBombs(dt)
    if bombFx == nil then return end
    if world == nil or world.particles == nil then bombFx = nil; return end
    bombFx.t = bombFx.t + dt
    bombFx.next = bombFx.next - dt
    if bombFx.next <= 0 then
        bombFx.next = 0.11 + math.random() * 0.12
        local ox = px + (math.random() * 2 - 1) * 2.2
        local oz = pz + (math.random() * 2 - 1) * 2.2
        local oy = FY + 0.2 + math.random() * 1.2
        pcall(function()
            world.particles:burst("fire", ox, oy, oz, 24, { size = 0.6, life = 0.7 })
            world.particles:burst("smoke", ox, oy + 0.3, oz, 10, { size = 0.7, life = 1.4 })
            world.particles:burst("embers", ox, oy, oz, 16, { speed = 3 })
        end)
        pcall(function() GLOBALCAMERA:Shake(16, 0.32, 1.5) end)
    end
    if bombFx.t >= bombFx.dur then bombFx = nil end
end
local function landlordScript()
    if room:canExtend() then
        return { { name = I18N.tr("Landlord"), text = I18N.tr("Ah, hello! Settling in alright?") },
                 { name = I18N.tr("Landlord"), text = I18N.tr("I could knock a wall through and extend your place to a roomier 7x7. Interested?"),
                   choices = { { label = I18N.tr("Yes, extend it!"), value = "yes" }, { label = I18N.tr("Not right now"), value = "no" } } } }
    end
    return { { name = I18N.tr("Landlord"), text = I18N.tr("You've already got the biggest place I offer! Enjoy the space.") } }
end
local function doExtend()
    local pc, pr = world:worldToCell(px, pz)
    room:extendTo(7, 7)
    rebuild()
    px, pz = world:cellToWorld(pc, pr)
    player:setPos(px, FY + 0.05, pz)
    saveRoom()
end

local buildPhoneMenu   -- forward decl (the textbox panes route Back into it)

local function buildPhoneTextPane(title, hintText, maxLen, confirmLabel, onConfirm)
    if phoneUI then phoneUI:disposeWidgets() end
    local ui = PopUI.new{ theme = PHONE_THEME }
    phoneUI = ui
    local x, y, w = SCREEN_W / 2 - 430, 330, 860
    ui:panel{ x = x, y = y, w = w, h = 330, title = title }
    local tb = ui:textbox{ x = x + 40, y = y + 100, w = w - 80, h = 76, value = "", maxLen = maxLen,
                           placeholder = hintText,
                           onSubmit = function(t) onConfirm(t) end }
    ui:button{ text = confirmLabel, x = x + 40, y = y + 210, w = 300, h = 76, accent = true,
               onClick = function() onConfirm(tb.value or "") end }
    ui:button{ text = I18N.tr("Back"), x = x + w - 240, y = y + 210, w = 200, h = 76,
               onClick = function() buildPhoneMenu() end }
end

buildPhoneMenu = function()
    if phoneUI then phoneUI:disposeWidgets() end
    local ui = PopUI.new{ theme = PHONE_THEME }
    phoneUI = ui
    local entries
    if net.online and net.isHost then
        -- while hosting, the phone only offers to stop hosting (no landlord/number/invite)
        entries = { { label = I18N.tr("Stop hosting (close the room)"), value = "stophost" } }
    else
        entries = {
            { label = I18N.tr("Call the landlord"), value = "landlord" },
            { label = I18N.tr("Enter a number"), value = "number" },
        }
        if not net.online and not net.connecting then
            entries[#entries + 1] = { label = I18N.tr("Invite friends (host a room)"), value = "invite" }
            entries[#entries + 1] = { label = I18N.tr("Join a friend (enter code)"), value = "join" }
        end
    end
    entries[#entries + 1] = { label = I18N.tr("Hang up"), value = "close" }
    local items = {}
    for i, e in ipairs(entries) do items[i] = { text = e.label, value = e.value } end
    local x, y, w = SCREEN_W / 2 - 360, 260, 720
    local h = 130 + #items * 78 + 60
    ui:panel{ x = x, y = y, w = w, h = h, title = I18N.tr("Phone") }
    ui:menu{
        x = x + 36, y = y + 96, w = w - 72, h = #items * 78, rowHeight = 78, items = items,
        onSelect = function(_, it)
            local v = it.value
            if v == "landlord" then
                closePhone(); mode = "dialogue"; phoneFlow = "landlord"
                dlg:start(landlordScript())
            elseif v == "number" then
                buildPhoneTextPane(I18N.tr("Enter a number"), I18N.tr("number..."), 16, I18N.tr("Call"), function(t)
                    closePhone(); dialPhone(t)       -- looks up data/phone_numbers.json (events/eggs)
                end)
            elseif v == "invite" then
                MO.host(); closePhone(); msg = net.msg; msgT = 7
            elseif v == "join" then
                buildPhoneTextPane(I18N.tr("Join a friend"), I18N.tr("paste the room code..."), 4096, I18N.tr("Join"), function(t)
                    closePhone()
                    local code = (t ~= "" and t) or nil
                    if code then
                        if MO.join(code) then msg = net.msg or I18N.tr("Connecting…") else msg = net.msg or I18N.tr("Could not join.") end
                        msgT = 6
                    end
                end)
            elseif v == "stophost" then
                MO.leave(); closePhone(); msg = I18N.tr("You closed the room."); msgT = 4
            else
                closePhone()
            end
        end,
    }
end

local function openPhone()
    mode = "phone"
    phoneJustOpened = true
    buildPhoneMenu()
end

-- ── player-save selector (choose whose room to view/edit; local multiplayer) ──────────────────────
local playerSelUI = nil
local function closePlayerSelect()
    if playerSelUI then playerSelUI:disposeWidgets(); playerSelUI = nil end
end
-- commit to a player: read THAT save (coins/unlocks via GetSaveFile(playerIndex)) and load its room
local function pickPlayer(i)
    playerIndex = i
    MO.playerIndex = i
    if pcScreen then pcScreen.playerIndex = i end
    loadRoom()
    rebuild()
    spawnAtEntrance()
    closePlayerSelect()
    mode = "play"
end
openPlayerSelect = function()
    local entries = {}
    for i = 0, 4 do
        local sf = GetSaveFile(i)
        if sf and sf.SaveUID and sf.SaveUID ~= "" then
            entries[#entries + 1] = { text = I18N.trf("Player %d — %s", i + 1, sf.Name or ""), value = i }
        end
    end
    if #entries <= 1 then                          -- 0 or 1 save → nothing to choose, just enter
        pickPlayer(entries[1] and entries[1].value or 0)
        return
    end
    closePlayerSelect()
    local ui = PopUI.new{ theme = PHONE_THEME }
    playerSelUI = ui
    local x, y, w = SCREEN_W / 2 - 380, 210, 760
    local h = 120 + #entries * 78 + 40
    ui:panel{ x = x, y = y, w = w, h = h, title = I18N.tr("Whose room?") }
    ui:menu{ x = x + 36, y = y + 92, w = w - 72, h = #entries * 78, rowHeight = 78, items = entries,
             onSelect = function(_, it) pickPlayer(it.value) end }
    mode = "playerselect"
    phoneJustOpened = true                          -- consume the keypress edge that opened My Room
end

local function onDialogueDone(result)
    if phoneFlow == "landlord" then
        if result == "yes" then doExtend(); phoneFlow = "end"
            dlg:start({ { name = I18N.tr("Landlord"), text = I18N.tr("{shake:14,0.5}There! Mind the dust — enjoy the extra room.") } })
        elseif result == "no" then phoneFlow = "end"
            dlg:start({ { name = I18N.tr("Landlord"), text = I18N.tr("No worries. Ring me whenever.") } })
        else mode = "play"; phoneFlow = nil end           -- "full room" node (no choice)
    else
        mode = "play"; phoneFlow = nil
    end
end

-- end an online session and drop back into our OWN room (a visitor leaving / being kicked, or a host
-- closing). Reloads the player's own layout from the DB and respawns at the entrance — staying in-stage.
local function backToOwnRoom(message)
    MO.leave()
    closePhone()
    if pcScreen and pcScreen:active() then pcScreen:close() end
    loadRoom(); rebuild(); spawnAtEntrance()
    mode = "play"; pmoving = false
    if message then msg = message; msgT = 4 end
end

-- ── per-frame ─────────────────────────────────────────────────────────────────────────────────────
local function settlePlayer(dt)
    player:move(dt, 0, 0, 0, false)          -- decelerate + keep gravity while a menu is up
    px, py, pz = player:pos()
end

-- camera orbit clamp: the room only has a back wall (far z) and a right wall (+x); keep the camera
-- on the open corner so both visible walls always face it. Camera forward yaw 45 looks INTO that
-- corner, so the valid window is [10, 80] degrees (wrap-aware, far side splits at 225).
local YAW_MIN, YAW_MAX = 10, 80
local YAW_SENS, PITCH_SENS = 0.30, 0.18
local function clampCamera()
    local cam = world.cam
    local yaw = cam.yaw % 360
    if yaw > YAW_MAX then
        yaw = (yaw < 225) and YAW_MAX or YAW_MIN
    elseif yaw < YAW_MIN then
        yaw = YAW_MIN
    end
    cam.yaw = yaw
    if cam.pitch > -15 then cam.pitch = -15 elseif cam.pitch < -50 then cam.pitch = -50 end
end

-- edit-mode camera: RMB drag rotates, wheel zooms, WASD/arrows PAN the look-at target on the ground
-- plane (the player character does not move while editing). Same clamps as play mode.
local function editCamera(dt)
    local cam = world.cam
    local dmx, dmy = INPUT:GetMouseDelta()
    if INPUT:MousePressing("Right") then cam:orbit(dmx * YAW_SENS, -dmy * PITCH_SENS) end
    if kd("Q") then cam:orbit(-90 * dt) end
    if kd("E") then cam:orbit(90 * dt) end
    clampCamera()
    local _, scrollY = INPUT:GetScrollDelta()
    if scrollY ~= 0 then
        local d = cam.dist * (1 - scrollY * 0.12)
        local lo, hi = cam.minDist or 6, cam.maxDist or 60
        cam.dist = (d < lo) and lo or ((d > hi) and hi or d)
    end
    local fy = rad(cam.yaw)
    local fwdX, fwdZ = sin(fy), cos(fy)
    local rgtX, rgtZ = cos(fy), -sin(fy)
    local mx, mz = 0, 0
    if kd("W") or kd("UpArrow")    then mx = mx + fwdX; mz = mz + fwdZ end
    if kd("S") or kd("DownArrow")  then mx = mx - fwdX; mz = mz - fwdZ end
    if kd("D") or kd("RightArrow") then mx = mx + rgtX; mz = mz + rgtZ end
    if kd("A") or kd("LeftArrow")  then mx = mx - rgtX; mz = mz - rgtZ end
    if mx ~= 0 or mz ~= 0 then
        local sp = 7 * dt
        cam:setTarget((cam.tx or 0) + mx * sp, cam.ty or (FY + 0.4), (cam.tz or 0) + mz * sp)
    end
end

function update(ts)
    local dt = (ts - lastTs) / 1000.0
    lastTs = ts
    if dt < 0 then dt = 0 elseif dt > 0.1 then dt = 0.1 end
    GLOBALCAMERA:Update(dt)
    OWM.ModelIcon.newFrame()       -- per-frame first-render budget for the edit-grid preview icons
    if world == nil or map == nil then return nil end
    tickBombs(dt)                  -- the Canabi bomb storm runs across every mode (incl. its message)

    -- online: drain events + advance remote avatars every frame (even under a menu)
    if net.online or net.connecting then
        MO.drain()
        MO.lerpRemotes(dt)
        if net.roomGone then backToOwnRoom(I18N.tr("The host closed the room.")) end
    end

    -- ── modal overlays ──
    if mode == "playerselect" then
        if playerSelUI then
            if phoneJustOpened then
                phoneJustOpened = false                 -- consume the edge that opened My Room
            elseif playerSelUI:update(ts) == "cancel" then
                closePlayerSelect(); MO.leave(); GLOBALCAMERA:Reset(); return Exit("stage", "_title")
            end
        else
            mode = "play"
        end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "dialogue" then
        if dlg:update(dt) == "done" then onDialogueDone(dlg.result) end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "pc" then
        if pcScreen:update(ts) == "closed" then mode = "play" end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "phone" then
        if phoneUI then
            if phoneJustOpened then
                phoneJustOpened = false      -- consume the keypress edge that opened the phone
            elseif phoneUI:update(ts) == "cancel" then
                closePhone()
            end
        else
            mode = "play"
        end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "edit" then
        editCamera(dt)                 -- RMB rotate, WASD pan the target, wheel zoom (player is parked)
        if edit:update(ts) == "exit" then
            mode = "play"; pmoving = false
            if editSavedCam then       -- restore the exact camera the player had before editing
                local s = editSavedCam
                world.cam:setRig{ yaw = s.yaw, pitch = s.pitch, fov = FOV, dist = s.dist }
                world.cam:setTarget(s.tx, s.ty, s.tz)
                editSavedCam = nil
            end
        end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    end

    if kp("Escape") then
        if MO.isGuest() then backToOwnRoom(I18N.tr("You left the room.")); return nil end
        MO.leave(); GLOBALCAMERA:Reset(); saveRoom(); return Exit("stage", "_title")
    end
    -- (interactables + Tab are resolved AFTER movement below, where px/pz are current)

    -- camera orbit / zoom (iso_demo2 parity), wall-clamped so both walls always face the camera
    local dmx, dmy = INPUT:GetMouseDelta()
    if INPUT:MousePressing("Right") then
        world.cam:orbit(dmx * YAW_SENS, -dmy * PITCH_SENS)
    end
    if kd("Q") then world.cam:orbit(-90 * dt) end
    if kd("E") then world.cam:orbit(90 * dt) end
    clampCamera()
    local _, scrollY = INPUT:GetScrollDelta()
    if scrollY ~= 0 then
        local d = world.cam.dist * (1 - scrollY * 0.12)
        local lo = world.cam.minDist or 6
        local hi = world.cam.maxDist or 60
        if d < lo then d = lo elseif d > hi then d = hi end
        world.cam.dist = d
    end

    -- movement on the physics character, camera-relative like iso_demo2 (wish dir from cam yaw)
    local fy = rad(world.cam.yaw)
    local fwdX, fwdZ = sin(fy), cos(fy)
    local rgtX, rgtZ = cos(fy), -sin(fy)
    local ix, iz = 0, 0
    if kd("W") or kd("UpArrow")    then ix = ix + fwdX; iz = iz + fwdZ end
    if kd("S") or kd("DownArrow")  then ix = ix - fwdX; iz = iz - fwdZ end
    if kd("D") or kd("RightArrow") then ix = ix + rgtX; iz = iz + rgtZ end
    if kd("A") or kd("LeftArrow")  then ix = ix - rgtX; iz = iz - rgtZ end
    player:move(dt, ix, iz, moveSpeed, false)
    px, py, pz = player:pos()

    -- camera FOLLOWS the player (smoothed target lerp); orbit/zoom stay manual. The target is clamped to
    -- the room interior so the follow never pans the view off the walls (camera boom is off in here).
    local gw, gh = room:gridW(), room:gridH()
    local ftx = math.max(0.75, math.min(gw - 1.75, px))
    local ftz = math.max(0.75, math.min(gh - 2.75, pz))
    world.cam:follow(ftx, FY + 0.7, ftz, 8)

    local vx, _, vz = player:vel()
    local moveLen = sqrt(vx * vx + vz * vz)
    pmoving = moveLen > 0.25
    if pmoving then
        pdir = world:facingFromWorld(vx, vz, pdir)
        pframeT = pframeT + dt * (moveLen / moveSpeed)
    else
        pframeT = 0
    end
    if net.online then MO.broadcastPos(dt, px, pz, pdir, pmoving) end

    -- interactables in range (px/pz are now current): Tab cycles focus when 2+ are reachable,
    -- else Tab enters edit (offline only); focus is kept stable across frames by key.
    local inter = interactablesInRange()
    interCount = #inter
    if focusKey then
        local ok = false
        for _, e in ipairs(inter) do if e.key == focusKey then ok = true; break end end
        if not ok then focusKey = nil end
    end
    if focusKey == nil and #inter > 0 then focusKey = inter[1].key end
    if kp("Tab") then
        if #inter >= 2 then
            local idx = 1
            for i, e in ipairs(inter) do if e.key == focusKey then idx = i; break end end
            focusKey = inter[idx % #inter + 1].key
        elseif not net.online then       -- no room editing while online (hosting or visiting)
            local pc, pr = world:worldToCell(px, pz)
            -- remember the full camera to restore on exit, then reset to a clean, framed editing view
            editSavedCam = { yaw = world.cam.yaw, pitch = world.cam.pitch, dist = world.cam.dist,
                             tx = world.cam.tx, ty = world.cam.ty, tz = world.cam.tz }
            local span = math.max(room.iw, room.ih) + 3
            local fit = span / (2 * math.tan(rad(FOV / 2))) * 1.05
            fit = math.min(world.cam.maxDist or fit, math.max(world.cam.minDist or fit, fit))
            world.cam:setRig{ yaw = 45, pitch = -40, fov = FOV, dist = fit }
            world.cam:setTarget(room:gridW() * 0.5, FY + 0.4, room:gridH() * 0.5)
            edit:enter(pc, pr); mode = "edit"; return nil
        end
    end

    -- resolve the focused interactable; glow it; act on Enter
    local focused = nil
    if focusKey then for _, e in ipairs(inter) do if e.key == focusKey then focused = e; break end end end
    prompt = focused and focused.kind or nil
    interactGlow(focused)
    if focused and (kp("Return") or kp("Space")) then
        if focused.kind == "exit" then
            if MO.isGuest() then backToOwnRoom("You left the room."); return nil end
            MO.leave(); GLOBALCAMERA:Reset(); saveRoom(); return Exit("stage", "_title")
        elseif focused.kind == "computer" then
            pcScreen:openScreen(); mode = "pc"
        elseif focused.kind == "phone" then
            openPhone()
        elseif focused.kind == "lamp" then
            focused.it.lit = (focused.it.lit == false) and true or false   -- toggle on/off
            rebuild(); saveRoom(); MO.onRoomChanged()
        elseif focused.kind == "pod" then
            mode = "dialogue"; phoneFlow = nil
            dlg:start({ { name = "", text = I18N.tr("You press the panel on the Mysterious pod. It hums, but nothing opens. Not yet.") } })
        end
    end
    if msgT > 0 then msgT = msgT - dt end

    world:update(dt, px, py, pz)
    return nil
end

-- ── draw ──────────────────────────────────────────────────────────────────────────────────────────
local function charSprite(dir, moving, t)
    local st = "idle"
    if moving then st = (math.floor(t / 0.16) % 2 == 0) and "run1" or "run2" end
    return (A.CHARA[dir] and A.CHARA[dir][st]) or (A.CHARA[2] and A.CHARA[2].idle)
end

function draw()
    if world == nil or map == nil or hud == nil then return end
    local hW = 1.7
    local wW = hW * (A.CHARA.w / A.CHARA.h)
    local function groundAt(x, z) return map:heightAt(x, z) end

    -- the dynamic actor layer (player + remote visitors). No blob shadows: the sun shadow map
    -- already grounds the sprites, so the shadow layer just stays cleared.
    world.actors:shadowsBegin()
    world.actors:actorsBegin()
    local spr = charSprite(pdir, pmoving, pframeT)
    if spr then world.actors:actorSprite(px, py, pz, wW, hW, spr) end
    if net.online then
        for _, p in pairs(net.posByPeer) do
            local rst = p.moving and ((math.floor(p.frameT / 0.16) % 2 == 0) and "run1" or "run2") or "idle"
            local rspr = (A.CHARA[p.dir] and A.CHARA[p.dir][rst]) or (A.CHARA[2] and A.CHARA[2].idle)
            local ry = groundAt(p.x, p.z)
            if rspr then world.actors:actorSprite(p.x, ry, p.z, wW, hW, rspr) end
        end
    end

    world:render()
    world:blit()

    -- HUD (glyph text via PopUI — dynamic strings never leak textures)
    -- current player's nameplate at the TOP-LEFT = whose room we're viewing
    if NAMEPLATE and NAMEPLATE.DrawPlayerNameplate then
        pcall(function() NAMEPLATE:DrawPlayerNameplate(40, 18, 255, playerIndex) end)
    end
    hud:drawTextEx(26, "My Room", SCREEN_W / 2, 22, { 255, 255, 255 }, { 0, 0, 0, 255 }, 1, 1, 0, "top")

    -- online presence: a status line + a floating name tag above each visitor's avatar
    if net.online then
        local cnt = MO.playerCount()
        hud:drawTextEx(18, (net.isHost and "Hosting" or "Visiting") .. " — " .. cnt .. (cnt == 1 and " person here" or " people here"),
            40, 66, { 150, 230, 200 }, { 0, 0, 0, 255 })
        for id, p in pairs(net.posByPeer) do
            local nm = net.nameByPeer[id]
            if nm then
                local sx, sy, depth = world:project(p.x, groundAt(p.x, p.z) + 1.95, p.z)
                if depth and depth > 0 then
                    hud:drawTextEx(18, nm, sx, sy, { 255, 240, 150 }, { 0, 0, 0, 255 }, 1, 1, 0, "top")
                end
            end
        end
    end

    -- modal overlays on top of the world
    if mode == "playerselect" then
        hud:rect(0, 0, SCREEN_W, SCREEN_H, 8, 10, 18, 150)
        if playerSelUI then playerSelUI:draw() end
    elseif mode == "dialogue" then
        dlg:draw()
    elseif mode == "pc" then
        pcScreen:draw()
    elseif mode == "edit" then
        edit:draw()
    elseif mode == "phone" then
        hud:rect(0, 0, SCREEN_W, SCREEN_H, 8, 10, 18, 130)
        if phoneUI then phoneUI:draw() end
    else
        -- input instructions: TOP-RIGHT, right-aligned, one per line (readability). The contextual
        -- action prompt (bright) leads, then any [Tab] switch, then the persistent controls.
        local instr = {}
        if prompt == "computer" then instr[#instr + 1] = { t = I18N.tr("[Enter] Use computer"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "phone" then instr[#instr + 1] = { t = I18N.tr("[Enter] Use phone"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "lamp" then instr[#instr + 1] = { t = I18N.tr("[Enter] Toggle the lamp"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "exit" then instr[#instr + 1] = { t = I18N.tr("[Enter] Leave the room"), c = { 255, 230, 150 }, s = 24 }
        end
        if interCount > 1 then instr[#instr + 1] = { t = I18N.tr("[Tab] Switch focus"), c = { 200, 220, 240 }, s = 18 } end
        local ctl = { 210, 216, 230 }
        instr[#instr + 1] = { t = I18N.tr("WASD — Move"), c = ctl, s = 18 }
        instr[#instr + 1] = { t = I18N.tr("RMB / Q·E — Orbit"), c = ctl, s = 18 }
        instr[#instr + 1] = { t = I18N.tr("Wheel — Zoom"), c = ctl, s = 18 }
        if MO.isGuest() then
            instr[#instr + 1] = { t = I18N.tr("Esc / Door — Leave visit"), c = ctl, s = 18 }
        else
            instr[#instr + 1] = { t = I18N.tr("Tab — Edit room"), c = ctl, s = 18 }
            instr[#instr + 1] = { t = I18N.tr("Esc — Leave"), c = ctl, s = 18 }
        end
        local iy = 84
        for _, ln in ipairs(instr) do
            hud:drawTextEx(ln.s, ln.t, SCREEN_W - 40, iy, ln.c, { 0, 0, 0, 255 }, 1, 1, 0, "topright")
            iy = iy + ln.s + 16
        end
    end
    if msgT and msgT > 0 and msg then
        hud:drawTextEx(24, msg, SCREEN_W / 2, SCREEN_H - 200, { 255, 235, 160 }, { 0, 0, 0, 255 }, 1, 1, 1700, "top")
    end
end
