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
local PHONE    = I18N.texts("phone")        -- lang/<code>/phone.json
local HUD      = I18N.texts("hud")          -- lang/<code>/hud.json
local MO       = require("online")          -- P2P "visit my room" (lobby + presence); see online.lua
local JB       = require("jukebox")         -- the Jukebox furniture's audio player; see jukebox.lua
local Pod      = require("pod")             -- the Mysterious Pod's entry sequence; see pod.lua
local CoinBox  = require("CoinBox")         -- Lib: the coin purse laid over the landlord's offers
local Landlord = require("landlord")        -- the landlord's phone call; see landlord.lua
local NavInput = require("NavInput")
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
local purse = nil              -- Lib/CoinBox: the purse shown over the landlord's offers
local playPhoneSound           -- forward decl: Sounds/<stem>.ogg, lazily loaded (defined with the dialer)
local ringT, ringScript, ringDue = 0, nil, false  -- an outgoing call: coin, then the ring, then the answer
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
-- the jukebox's playing song persists alongside the room (own key, same LMDB): every playback op
-- writes it (JB ctx.onState) and loadRoom resumes it, so leaving/re-entering keeps the music going
local function jukeboxKey() return (saveKey():gsub("^room_", "jukebox_")) end
local persistLocked = false    -- deactivate snapshots the live state, then LOCKS so the teardown
                               -- stopAll (which fires onState with an empty state) can't wipe it
local function persistJukebox()
    if persistLocked or (IsSongsEnumDone and not IsSongsEnumDone()) then return end
    if net.online and not net.isHost then return end   -- never persist a host's song into a guest save
    if store == nil then return end
    local st = JB.persistState()
    store:Write(jukeboxKey(), st and serialize(st) or "")
end
local function loadRoom()
    persistLocked = false          -- fresh stage entry / room switch: persisting is live again
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
    -- resume this save's jukebox: read BEFORE stopAll (stopping persists an empty state)
    local jbSaved = deserialize(store and store:Read(jukeboxKey()) or nil)
    JB.stopAll()
    if jbSaved then
        JB.restoreState(jbSaved)
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
                         theme = { text = { 255, 255, 255 } },
                         -- {sfx:purse} in a landlord line brings the purse up; any other name is a sound
                         onSfx = function(name)
                             if name == "purse" then Landlord.showPurse() else playPhoneSound(name) end
                         end,
                         -- the landlord's voice: deep, through a scrambler; nobody else on the line blips
                         onBlip = function()
                             if phoneFlow == "landlord" then playPhoneSound("blip_landlord" .. math.random(3)) end
                         end, blipEvery = 3 })

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
    Landlord.reset()                             -- the landlord forgives on every visit
    purse:hide()
    ringT, ringScript, ringDue = 0, nil, false
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
        edit = Edit.new(room, world, function()
            rebuild(); saveRoom(); MO.onRoomChanged()
            JB.onRoomEdited(room)      -- stop playback if its jukebox got removed; refresh its cell
        end)
        MO.init({
            getRoom         = function() return room end,
            applyRoom       = function(t) room:loadTable(t) end,
            rebuildRoom     = function() rebuild() end,
            spawnAtEntrance = function() spawnAtEntrance() end,
            applyJukebox    = function(t) JB.applyNetState(t) end,   -- guest: mirror the host's player
            jukeboxState    = function() return JB.netState() end,   -- host: brief late joiners
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
    PCS.tickBgmFade(math.huge)                     -- force finishing fading out
    persistJukebox()                               -- snapshot the LIVE playing position first...
    persistLocked = true                           -- ...lock it against the teardown's own onState...
    JB.stopAll()                                   -- ...then kill jukebox audio + UI with the stage
    JB.close()
    JB.setDuck(false)                              -- reset the PC-screen crossfade for the next visit
    if phoneUI then phoneUI:disposeWidgets(); phoneUI = nil end
    if hud then hud:disposeWidgets(); hud = nil end
    purse:dispose()
    Pod.reset()
    -- safety net: free any 3D preview icon (edit grid / popup) its owner missed
    pcall(function() OWM.ModelIcon.disposeAll() end)
    if world ~= nil then world.scene:Dispose(); world = nil; player = nil; map = nil end
    mode = "play"
end

function afterSongEnum() JB.onSongEnumDone() end   -- song list ready → jukebox Songs tab unlocks

function onDestroy()
    MO.leave()
    if store then store:Dispose(); store = nil end
    JB.dispose()
    PCS.disposeBgm()
    if Edit.disposeSfx then Edit.disposeSfx() end
    for _, s in pairs(phoneSfx) do if s then pcall(function() s:Dispose() end) end end
    Pod.dispose()
    phoneSfx = {}; bombFx = nil
end

local function kd(k) return INPUT:KeyboardPressing(k) end
local function kp(k) return INPUT:KeyboardPressed(k) end

-- every interactable the player is in range of, as an ordered list (exit → furniture → phone).
-- Each entry: { kind, it (furniture, when applicable), key (stable id for keeping focus) }.
-- Multiplayer rules: GUESTS get nothing but the exit; the HOST, while hosting, keeps only the phone
-- (to stop hosting) and his jukeboxes (playback propagates to every guest) — computer/lamp/pod are
-- solo-only.
local function interactablesInRange()
    local list = {}
    local pc, pr = world:worldToCell(px, pz)
    if room:cellType(pc, pr) == "E" then list[#list + 1] = { kind = "exit", key = "exit" } end
    if not MO.isGuest() then
        local seen = {}
        for _, d in ipairs({ { 0, 0 }, { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }) do
            local it, cat = room:furnitureAt(pc + d[1], pr + d[2])
            if it and cat and not seen[it] then
                if cat.interact == "jukebox" and not net.connecting then
                    seen[it] = true; list[#list + 1] = { kind = "jukebox", it = it, key = "jukebox:" .. tostring(it) }
                elseif not net.online then
                    if cat.computer then
                        seen[it] = true; list[#list + 1] = { kind = "computer", it = it, key = "computer:" .. tostring(it) }
                    elseif it.id == "floorlamp" then
                        seen[it] = true; list[#list + 1] = { kind = "lamp", it = it, key = "lamp:" .. tostring(it) }
                    elseif cat.interact == "pod" then
                        seen[it] = true; list[#list + 1] = { kind = "pod", it = it, key = "pod:" .. tostring(it) }
                    end
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
        -- pulse an emissive ADD on the item's own object(s); the lit lamp keeps its warm base emissive,
        -- and the jukebox's routed screen part keeps its catalog idle glow (else walking away would
        -- leave the screen fully dark until something replays it)
        local br, bg, bb = 0, 0, 0
        if focused.kind == "lamp" and focused.it and focused.it.lit ~= false then br, bg, bb = 0.9, 0.72, 0.36 end
        local partBase = nil
        if focused.kind == "jukebox" and inst.parts then
            partBase = {}
            for _, pp in ipairs(inst.parts) do partBase[pp.obj] = { 0.12, 0.114, 0.096 } end
        end
        local p = 0.30 + 0.30 * (0.5 + 0.5 * sin(lastTs * 0.006))   -- 0.30 .. 0.90 glow
        world._glow = {}
        for _, o in ipairs(inst.objs or { inst.obj }) do
            local rb = partBase and partBase[o] or nil
            local r0, g0, b0 = br, bg, bb
            if rb then r0, g0, b0 = rb[1], rb[2], rb[3] end
            world._glow[#world._glow + 1] = { o = o, r = r0, g = g0, b = b0 }
            pcall(function() scene:ObjSetEmissive(o, r0 + p, g0 + p, b0 + p) end)
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
local function closePhone(hangUp)
    if phoneUI then phoneUI:disposeWidgets(); phoneUI = nil end
    if mode == "phone" then mode = "play" end
    if hangUp then playPhoneSound("phone_hangup") end
end

-- an outgoing call: a coin into the payphone, the ringing tone, then the other side answers with
-- `script` (the dialogue starts once the ring is over)
local function placeCall(script)
    playPhoneSound("phone_coin")
    mode = "dialogue"
    ringT, ringScript, ringDue = 2.6, script, true     -- the ring starts once the coin has dropped
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
playPhoneSound = function(stem)
    if phoneSfx[stem] == nil then
        local s = nil
        -- easter-egg sounds live in the STAGE's own Sounds/ folder (e.g. myroom/Sounds/egg.ogg) —
        -- NOT the skin-wide ../../../Sounds/ (that only has the shared UI sfx like Error.ogg)
        pcall(function() s = SOUND:CreateSFX("Sounds/" .. stem .. ".ogg") end)
        if s then pcall(function() s:SetVolume(100) end) end
        phoneSfx[stem] = s or false
    end
    if phoneSfx[stem] then return pcall(function() phoneSfx[stem]:Play() end) end
    return false
end
local function startBombs() bombFx = { t = 0, dur = 3.0, next = 0 } end  -- { t, dur, next }
local function dialPhone(input)
    local key = tostring(input or ""):gsub("%s+", ""):lower()
    local doc = phoneNumbersDoc()
    local ev = nil
    pcall(function() ev = doc and doc[key] or nil end)
    if ev == nil then                                 -- unassigned number → the placeholder
        local n = (input and input ~= "" and input) or PHONE:tr("the_number")
        mode = "dialogue"; phoneFlow = nil
        dlg:start({ { name = "", text = PHONE:trf("no_answer", n) } })
        return false
    end
    -- guarded like every other data-file read: a malformed entry (bare string, nested field) must
    -- degrade to the no-answer placeholder, not error out of the update path
    local name, text, sound, effect
    local okEv = pcall(function()
        name   = JSONLOADER:ExtractText(ev["name"])
        text   = JSONLOADER:ExtractText(ev["text"])
        sound  = JSONLOADER:ExtractText(ev["sound"])
        effect = JSONLOADER:ExtractText(ev["effect"])
    end)
    if not okEv then
        local n = (input and input ~= "" and input) or PHONE:tr("the_number")
        mode = "dialogue"; phoneFlow = nil
        dlg:start({ { name = "", text = PHONE:trf("no_answer", n) } })
        return
    end
    if text then text = PHONE:tr(text) end       -- the entry's text is a phone.json id (egg_*); names stay as written
    local wantSilent
    if sound and sound ~= "" then wantSilent = playPhoneSound(sound) end
    if effect == "bombs" then startBombs() end
    if text == nil or text == "" then mode = "play"  -- sound-only (e.g. 1122 → egg.ogg), no message
    else
        mode = "dialogue"; phoneFlow = nil
        dlg:start({ { name = name or "", text = text or "" } })
    end
    return wantSilent
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
-- the landlord's phone call lives in landlord.lua (script + answers, lang/<code>/dialogs.json lines);
-- the room only lends it the save file, the room, the extension, and the purse widget
-- lang/<code>/dialogs.json: named dialog lines per language (English is the fallback for a missing
-- file, key or line; the code's own fallback covers a missing English file)
local DIALOGS = I18N.texts("dialogs")
local function dlgLoc(section, key, fallback)
    return DIALOGS:get(section, key) or fallback
end
-- the list of lines at a key path (section, key, ...), each line falling back to English
local function dlgList(...)
    return DIALOGS:list(...)
end

local function doExtend(info)
    local sf = curSave()
    if sf.Coins < info.cost then return false end
    sf:SpendCoins(info.cost)
    local pc, pr = world:worldToCell(px, pz)
    room:extendTo(info.iw, info.ih)
    rebuild()
    px, pz = world:cellToWorld(pc, pr)
    player:setPos(px, FY + 0.05, pz)
    saveRoom()
    return true
end

-- the purse sits on the dialogue box's top edge, level with the speaker's name pill (box y 770,
-- pill at 740..794), at the right
purse = CoinBox.new{ x = SCREEN_W - 60 - 300, y = 770 - 36 }
Landlord.init{
    save = function() return curSave() end,
    room = function() return room end,
    extend = doExtend,
    dlgLoc = dlgLoc, dlgList = dlgList,
    coinBox = purse,
}

-- jukebox context: closures over the stage's live state (guarded — world/room are nil until activate)
JB.init{
    theme = PHONE_THEME,
    isHost = function() return not MO.isGuest() end,     -- solo counts as the authoritative side
    isOnline = function() return net.online end,
    broadcast = function(st) MO.broadcastJukebox(st) end,
    playerPos = function() return px, pz end,
    cellToWorld = function(c, r) if world then return world:cellToWorld(c, r) end end,
    propInstFor = function(it) return world and world._propInst and world._propInst[it] or nil end,
    jukeboxItemAt = function(c, r)
        if room == nil then return nil end
        for _, it in ipairs(room.furniture or {}) do
            if it.id == "jukebox" and it.c == c and it.r == r then return it end
        end
        return nil
    end,
    anyJukeboxItem = function()
        if room == nil then return nil end
        for _, it in ipairs(room.furniture or {}) do if it.id == "jukebox" then return it end end
        return nil
    end,
    onState = function() persistJukebox() end,   -- every playback op → resume state saved per save UID
    scene = function() return world and world.scene or nil end,
}
-- (the PC screen crossfades with the jukebox: JB.setDuck on open/close + PCS.tickBgmFade in update)

local buildPhoneMenu   -- forward decl (the textbox panes route Back into it)

local function buildPhoneTextPane(title, hintText, maxLen, confirmLabel, onConfirm)
    if phoneUI then phoneUI:disposeWidgets() end
    local ui = PopUI.new{ theme = PHONE_THEME, navPlayer = playerIndex + 1 }
    phoneUI = ui
    local x, y, w = SCREEN_W / 2 - 430, 330, 860
    ui:panel{ x = x, y = y, w = w, h = 330, title = title }
    local tb = ui:textbox{ x = x + 40, y = y + 100, w = w - 80, h = 76, value = "", maxLen = maxLen,
                           placeholder = hintText,
                           onSubmit = function(t) return onConfirm(t) end }
    ui:button{ text = confirmLabel, x = x + 40, y = y + 210, w = 300, h = 76, accent = true,
               onClick = function() return onConfirm(tb.value or "") end }
    ui:button{ text = PHONE:tr("back"), x = x + w - 240, y = y + 210, w = 200, h = 76,
               onClick = function() buildPhoneMenu() end, sfx = { click = "cancel" } }
end

buildPhoneMenu = function()
    if phoneUI then phoneUI:disposeWidgets() end
    local ui = PopUI.new{ theme = PHONE_THEME, navPlayer = playerIndex + 1 }
    phoneUI = ui
    local entries
    if net.online and net.isHost then
        -- while hosting, the phone only offers to stop hosting (no landlord/number/invite)
        entries = { { label = PHONE:tr("menu_stop_hosting"), value = "stophost" } }
    else
        entries = {
            { label = PHONE:tr("menu_landlord"), value = "landlord" },
            { label = PHONE:tr("menu_number"), value = "number" },
        }
        if not net.online and not net.connecting then
            entries[#entries + 1] = { label = PHONE:tr("menu_invite"), value = "invite" }
            entries[#entries + 1] = { label = PHONE:tr("menu_join"), value = "join" }
        end
    end
    entries[#entries + 1] = { label = PHONE:tr("menu_hang_up"), value = "close" }
    local items = {}
    for i, e in ipairs(entries) do items[i] = { text = e.label, value = e.value } end
    local x, y, w = SCREEN_W / 2 - 360, 260, 720
    local h = 130 + #items * 78 + 60
    ui:panel{ x = x, y = y, w = w, h = h, title = PHONE:tr("title") }
    ui:menu{
        x = x + 36, y = y + 96, w = w - 72, h = #items * 78, rowHeight = 78, items = items,
        onSelect = function(_, it)
            local v = it.value
            if v == "landlord" then
                closePhone(); phoneFlow = "landlord"
                placeCall(Landlord.script())
            elseif v == "number" then
                buildPhoneTextPane(PHONE:tr("menu_number"), PHONE:tr("number_placeholder"), 16, PHONE:tr("call"), function(t)
                    closePhone();
                    playPhoneSound("phone_coin")
                    return dialPhone(t)  -- looks up data/phone_numbers.json (events/eggs)
                end)
            elseif v == "invite" then
                if not JB.songsEnumReady() then
                    -- visits need the finished song catalogue (the guests' jukebox matching runs on it)
                    closePhone(); mode = "dialogue"; phoneFlow = nil
                    dlg:start({ { name = "", text = dlgLoc("phone", "enum_wait",
                        "There is a note next to the phone, \"Do not use while the song catalogue is loading\" it says.") } })
                else
                    MO.host(); closePhone(); msg = net.msg; msgT = 7
                end
            elseif v == "join" then
                if not JB.songsEnumReady() then
                    closePhone(); mode = "dialogue"; phoneFlow = nil
                    dlg:start({ { name = "", text = dlgLoc("phone", "enum_wait",
                        "There is a note next to the phone, \"Do not use while the song catalogue is loading\" it says.") } })
                else
                buildPhoneTextPane(PHONE:tr("join_title"), PHONE:tr("code_placeholder"), 4096, PHONE:tr("join"), function(t)
                    closePhone()
                    local code = (t ~= "" and t) or nil
                    if not code then SHARED:GetSharedSound("Cancel"):Play(); return true end
                    if MO.join(code) then
                        JB.stopAll()   -- our own music stays home; the host's jukebox takes over
                        msg = net.msg or PHONE:tr("connecting")
                    else
                        msg = net.msg or PHONE:tr("join_failed")
                    end
                    msgT = 6
                end)
                end
            elseif v == "stophost" then
                MO.leave(); closePhone(); msg = PHONE:tr("room_closed"); msgT = 4
            else
                closePhone(true)
                return true
            end
        end,
    }
end

local function openPhone()
    mode = "phone"
    phoneJustOpened = true
    playPhoneSound("phone_pickup")
    buildPhoneMenu()
end

-- ── player-save selector (choose whose room to view/edit; local multiplayer) ──────────────────────
local playerSelUI = nil
local function closePlayerSelect()
    if playerSelUI then playerSelUI:disposeWidgets(); playerSelUI = nil end
end
-- commit to a player: read THAT save (coins/unlocks via GetSaveFile(playerIndex)) and load its room
local function pickPlayer(i)
    MO.playerIndex = i
    if pcScreen then pcScreen.playerIndex = i end
    if edit then edit.playerIndex = i end
    if i ~= playerIndex then
        persistJukebox()
        playerIndex = i
        loadRoom()
        rebuild()
    end
    spawnAtEntrance()
    closePlayerSelect()
    mode = "play"
end
openPlayerSelect = function()
    local entries = {}
    for i = 0, 4 do
        local sf = GetSaveFile(i)
        if sf and sf.SaveUID and sf.SaveUID ~= "" then
            entries[#entries + 1] = { text = HUD:trf("player_entry", i + 1, sf.Name or ""), value = i }
        end
    end
    if #entries <= 1 then                          -- 0 or 1 save → nothing to choose, just enter
        pickPlayer(entries[1] and entries[1].value or 0)
        return
    end
    closePlayerSelect()
    local ui = PopUI.new{ theme = PHONE_THEME, navPlayer = nil } -- accessible by all players
    playerSelUI = ui
    local x, y, w = SCREEN_W / 2 - 380, 210, 760
    local h = 120 + #entries * 78 + 40
    ui:panel{ x = x, y = y, w = w, h = h, title = HUD:tr("whose_room") }
    ui:menu{ x = x + 36, y = y + 92, w = w - 72, h = #entries * 78, rowHeight = 78, items = entries,
             onSelect = function(_, it) pickPlayer(it.value) end }
    mode = "playerselect"
    phoneJustOpened = true                          -- consume the keypress edge that opened My Room
end

local function onDialogueDone(result)
    if phoneFlow == "landlord" then
        local nxt = Landlord.onDone(result)
        if nxt then dlg:start(nxt) ; return end
        playPhoneSound("phone_cut")          -- he hangs up on you, every time
    end
    mode = "play"; phoneFlow = nil
end

-- end an online session and drop back into our OWN room (a visitor leaving / being kicked, or a host
-- closing). Reloads the player's own layout from the DB and respawns at the entrance — staying in-stage.
local function backToOwnRoom(message)
    JB.stopAll()                        -- the host's jukebox does not follow us home
    JB.close()                          -- nor a stranded (invisible) jukebox window eating input
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

-- getting mouse deltas clears the deltas, so passed by the caller instead of getting here.
-- frameDt = this frame's dt (set by update); Q/E orbit at a fixed degrees-per-second rate.
local frameDt = 0
local function panCamera(dmx, dmy)
    local cam = world.cam
    if INPUT:MousePressing("Right") then
        cam:orbit(dmx * YAW_SENS, -dmy * PITCH_SENS)
    end
    if kd("Q") then cam:orbit(-90 * frameDt) end
    if kd("E") then cam:orbit(90 * frameDt) end
    clampCamera()
end

local function zoomCamera(scrollY)
    if scrollY ~= 0 then
        local cam = world.cam
        local d = cam.dist * (1 - scrollY * 0.12)
        local lo, hi = cam.minDist or 6, cam.maxDist or 60
        cam.dist = (d < lo) and lo or ((d > hi) and hi or d)
    end
end

local function getMoveUnitIsoXZ()
    local cam = world.cam
    local fy = rad(cam.yaw)
    local fwdX, fwdZ = sin(fy), cos(fy)
    local rgtX, rgtZ = cos(fy), -sin(fy)
    local mx, mz = 0, 0
    if kd("W") or kd("UpArrow")    then mx = mx + fwdX; mz = mz + fwdZ end
    if kd("S") or kd("DownArrow")  then mx = mx - fwdX; mz = mz - fwdZ end
    if kd("D") or kd("RightArrow") then mx = mx + rgtX; mz = mz + rgtZ end
    if kd("A") or kd("LeftArrow")  then mx = mx - rgtX; mz = mz - rgtZ end
    return mx, mz
end

-- edit-mode camera: RMB drag rotates, wheel zooms, WASD/arrows PAN the look-at target on the ground
-- plane (the player character does not move while editing). Same clamps as play mode.
local function getEditCameraFuncs(dt) return {
    pan = panCamera,
    zoom = zoomCamera,
    move = function()
        local ix, iz = getMoveUnitIsoXZ()
        if ix ~= 0 or iz ~= 0 then
            local cam = world.cam
            local sp = 7 * dt
            cam:setTarget((cam.tx or 0) + ix * sp, cam.ty or (FY + 0.4), (cam.tz or 0) + iz * sp)
        end
    end,
} end

function update(ts)
    local dt = (ts - lastTs) / 1000.0
    lastTs = ts
    if dt < 0 then dt = 0 elseif dt > 0.1 then dt = 0.1 end
    frameDt = dt
    GLOBALCAMERA:Update(dt)
    OWM.ModelIcon.newFrame()       -- per-frame first-render budget for the edit-grid preview icons
    if world == nil or map == nil then return nil end
    tickBombs(dt)                  -- the Canabi bomb storm runs across every mode (incl. its message)

    -- online: drain events + advance remote avatars every frame (even under a menu)
    if net.online or net.connecting then
        MO.drain()
        MO.lerpRemotes(dt)
        if net.roomGone then backToOwnRoom(PHONE:tr("room_host_closed")) end
    end

    -- jukebox playback upkeep runs in EVERY mode (audio outlives the menu: distance volume, track
    -- end/repeat, screen glow, net flush); the returned edge feeds the jukebox modal branch below.
    -- The PC-screen BGM fade is pumped alongside so its fade-out can finish after the screen closes.
    local jbRes = JB.update(dt, ts)
    PCS.tickBgmFade(dt)

    -- ── modal overlays ──
    if mode == "playerselect" then
        if playerSelUI then
            if phoneJustOpened then
                phoneJustOpened = false                 -- consume the edge that opened My Room
            elseif playerSelUI:update(ts) == "cancel" then
                closePlayerSelect(); MO.leave(); GLOBALCAMERA:Reset(); SHARED:GetSharedSound("Cancel"):Play()
                return Exit("stage", "_title")
            end
        else
            mode = "play"
        end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "dialogue" then
        purse:update(dt)
        if ringT > 0 then
            ringT = ringT - dt
            if ringDue and ringT <= 1.6 then ringDue = false ; playPhoneSound("phone_ring") end
            if ringT <= 0 and ringScript then dlg:start(ringScript) ; ringScript = nil end
        elseif dlg:update(dt) == "done" then onDialogueDone(dlg.result) end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "pod" then
        if Pod.update(dt) == "go" then
            MO.leave(); GLOBALCAMERA:Reset(); saveRoom()
            return Exit("stage", Pod.stageName(), "default")
        end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "pc" then
        if pcScreen:update(ts) == "closed" then
            mode = "play"
            JB.setDuck(false, 0.15)        -- PC BGM fades out; the jukebox fades back in just after
        end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "phone" then
        if phoneUI then
            if phoneJustOpened then
                phoneJustOpened = false      -- consume the keypress edge that opened the phone
            elseif phoneUI:update(ts) == "cancel" then
                closePhone(true)
            end
        else
            mode = "play"
        end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "jukebox" then
        if jbRes == "closed" or not JB.isOpen() then mode = "play" end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "edit" then
        if edit:update(ts, getEditCameraFuncs(dt)) == "exit" then
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

    if NavInput.p[playerIndex + 1].cancel() then
        SHARED:GetSharedSound("Cancel"):Play()
        if MO.isGuest() then backToOwnRoom(PHONE:tr("room_left")); return nil end
        MO.leave(); GLOBALCAMERA:Reset(); saveRoom(); return Exit("stage", "_title")
    end
    -- (interactables + Tab are resolved AFTER movement below, where px/pz are current)

    -- camera orbit / zoom (iso_demo2 parity), wall-clamped so both walls always face the camera
    local dmx, dmy = INPUT:GetMouseDelta()
    panCamera(dmx, dmy)
    local _, scrollY = INPUT:GetScrollDelta()
    zoomCamera(scrollY)

    -- movement on the physics character, camera-relative like iso_demo2 (wish dir from cam yaw)
    local ix, iz = getMoveUnitIsoXZ()
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
            SHARED:GetSharedSound("Skip"):Play()
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
            edit:enter(pc, pr); mode = "edit"
            SHARED:GetSharedSound("Decide"):Play()
            return nil
        end
    end

    -- resolve the focused interactable; glow it; act on Enter
    local focused = nil
    if focusKey then for _, e in ipairs(inter) do if e.key == focusKey then focused = e; break end end end
    prompt = focused and focused.kind or nil
    interactGlow(focused)
    if focused and (NavInput.p[playerIndex + 1].decide() or kp("Space")) then
        if focused.kind == "exit" then
            SHARED:GetSharedSound("Cancel"):Play()
            if MO.isGuest() then backToOwnRoom(PHONE:tr("room_left")); return nil end
            MO.leave(); GLOBALCAMERA:Reset(); saveRoom()
            return Exit("stage", "_title")
        elseif focused.kind == "computer" then
            JB.setDuck(true)               -- jukebox fades down first; the PC BGM fades in after it
            SHARED:GetSharedSound("Decide"):Play()
            pcScreen:openScreen(); mode = "pc"
        elseif focused.kind == "phone" then
            SHARED:GetSharedSound("Decide"):Play()
            openPhone()
        elseif focused.kind == "lamp" then
            SHARED:GetSharedSound("Move"):Play()
            focused.it.lit = (focused.it.lit == false) and true or false   -- toggle on/off
            rebuild(); saveRoom(); MO.onRoomChanged()
        elseif focused.kind == "pod" then
            SHARED:GetSharedSound("Decide"):Play()
            if Pod.stageExists() then
                -- the pod opens: step in, rattle, vent (update handles "go")
                mode = "pod"
                Pod.start(world, focused.it, world._propInst and world._propInst[focused.it] or nil, px, pz, playerIndex)
            else
                mode = "dialogue"; phoneFlow = nil
                dlg:start({ { name = "", text = dlgLoc("pod", "locked",
                    "You press the panel on the Mysterious pod, but it does nothing.") } })
            end
        elseif focused.kind == "jukebox" then
            SHARED:GetSharedSound("Decide"):Play()
            JB.openFor(focused.it, Room.displayName("jukebox"), playerIndex)
            mode = "jukebox"
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
    if mode == "pod" then
        -- stepping into the pod: the sprite slides to it and shrinks away, then stays hidden
        local vx, vz, vs = Pod.playerVisual()
        if spr and vx then world.actors:actorSprite(vx, py, vz, wW * vs, hW * vs, spr) end
    elseif spr then
        world.actors:actorSprite(px, py, pz, wW, hW, spr)
    end
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
        if ringT > 0 then
            hud:drawTextEx(26, PHONE:tr("calling"), SCREEN_W / 2, SCREEN_H - 200, { 255, 235, 160 }, { 0, 0, 0, 255 }, 1, 1, 0, "top")
        end
        dlg:draw()
        purse:draw()
    elseif mode == "pod" then
        Pod.draw(hud, SCREEN_W, SCREEN_H)
    elseif mode == "pc" then
        pcScreen:draw()
    elseif mode == "edit" then
        edit:draw()
    elseif mode == "phone" then
        hud:rect(0, 0, SCREEN_W, SCREEN_H, 8, 10, 18, 130)
        if phoneUI then phoneUI:draw() end
    elseif mode == "jukebox" then
        hud:rect(0, 0, SCREEN_W, SCREEN_H, 8, 10, 18, 130)
        JB.draw()
    else
        -- input instructions: TOP-RIGHT, right-aligned, one per line (readability). The contextual
        -- action prompt (bright) leads, then any [Tab] switch, then the persistent controls.
        local instr = {}
        if prompt == "computer" then instr[#instr + 1] = { t = HUD:tr("prompt_computer"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "phone" then instr[#instr + 1] = { t = HUD:tr("prompt_phone"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "lamp" then instr[#instr + 1] = { t = HUD:tr("prompt_lamp"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "jukebox" then instr[#instr + 1] = { t = HUD:tr("prompt_jukebox"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "pod" then instr[#instr + 1] = { t = HUD:tr("prompt_pod"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "exit" then instr[#instr + 1] = { t = HUD:tr("prompt_exit"), c = { 255, 230, 150 }, s = 24 }
        end
        if interCount > 1 then instr[#instr + 1] = { t = HUD:tr("prompt_switch"), c = { 200, 220, 240 }, s = 18 } end
        local ctl = { 210, 216, 230 }
        instr[#instr + 1] = { t = HUD:tr("ctl_move"), c = ctl, s = 18 }
        instr[#instr + 1] = { t = HUD:tr("ctl_orbit"), c = ctl, s = 18 }
        instr[#instr + 1] = { t = HUD:tr("ctl_zoom"), c = ctl, s = 18 }
        if MO.isGuest() then
            instr[#instr + 1] = { t = HUD:tr("ctl_leave_visit"), c = ctl, s = 18 }
        else
            instr[#instr + 1] = { t = HUD:tr("ctl_edit"), c = ctl, s = 18 }
            instr[#instr + 1] = { t = HUD:tr("ctl_leave"), c = ctl, s = 18 }
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
