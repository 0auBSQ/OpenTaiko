---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- My Room: the personal room global stage, on OWM3d
-- saved per save file in its "my_room" string (store.lua)

-- drawn in 1920x1080: a skin of another resolution rescales it
STAGES:SetResolution(1920, 1080)

local OWM      = require("World.OWM3d")
local A        = require("assets")
local Room     = require("room")
local Dialogue = require("VN.Dialogue")
local PCS      = require("pc")
local Edit     = require("editmode")
local UI       = require("uilib")
local SavePicker = require("Saves.SavePicker")
local I18N      = require("Locale.i18n")
local PHONE    = I18N.texts("phone")        -- lang/<code>/phone.json
local HUD      = I18N.texts("hud")          -- lang/<code>/hud.json
local MO       = require("online")
local JB       = require("jukebox")
local TV       = require("tv")
local Pod      = require("pod")
local MX       = require("matrix")
local MR       = require("mirror")
local FT       = require("fishtank")
local LT       = require("lantern")
local WB       = require("workbench")
local RB       = require("recycle")
local AV       = require("avatar")
local Student  = require("Characters.Student")
local Purse    = require("purse")
local Landlord = require("landlord")
-- Mr.Caspian's and Gris's numbers (vault small talks): only Open-World Memories has Vault.Talk
-- with another skin they answer like any other number
local Talk     = nil
do
    local ok, mod = pcall(require, "Vault.Talk")
    if ok and type(mod) == "table" then Talk = mod end
end
local Chests   = require("Vault.Chests")    -- shows the song Gris's call unlocks as a vault reward
local Integrity = require("integrity")
local Store    = require("store")
local NavInput = require("Input.NavInput")
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
local pdir = 2                 -- facing 1↙ 2↘ 3↗ 4↖ (the students' sprite folders)
local pframeT = 0              -- walk-cycle timer
local pmoving = false
local look = { student = "a", palette = 0, set = nil }   -- player's student look (AV sprites in set)
local moveSpeed = 4.2          -- units/sec
local prompt = nil             -- current interaction prompt ("computer"/"phone"/"exit"/"lamp")
local focusKey = nil           -- focused in-range interactable (stable across frames; Tab cycles)
local interCount = 0           -- interactables in range this frame (>1: draw shows the [Tab] hint)
local msg, msgT = nil, 0       -- transient on-screen message
local editSavedCam = nil       -- full camera state saved on edit-enter, restored on edit-exit
local savedJukebox = nil       -- the jukebox state stored with the room (one string holds both)
local playerIndex = 0          -- which of the 5 local saves' room we view/edit (chosen at entry)
local openPlayerSelect         -- forward decl (defined after PHONE_THEME; called from activate)
local takeWayBack              -- forward decl (defined with the save choice; called from activate)
local mode = "play"            -- "play" | "dialogue" | "pc" | "phone" | "edit"
local phoneUI = nil            -- the UI manager of the phone (menu / number / join panes)
local phoneJustOpened = false  -- consume the keypress edge that opened the phone
local phoneFlow = nil          -- landlord conversation step
local purse = nil              -- purse over the landlord's offers
local playPhoneSound           -- forward decl: Sounds/<stem>.ogg, lazily loaded (defined with the dialer)
local ringT, ringScript, ringDue = 0, nil, false  -- outgoing call state (placeCall)
local phoneSfx = {}            -- cached dialable-number SFX (stem → sound|false); disposed in onDestroy
local bombFx = nil             -- "Canabi" bomb-storm state (declared here for onDestroy)
local branchCall = nil         -- the running call that branches (data/phone_numbers.json "start")
local talkCo, talkRun = nil, nil       -- Vault.Talk call (Mr.Caspian or Gris) and its lines
local talkPlay = nil                   -- the song that call asks for: the room leaves for it
local talkAway, talkBack = false, nil  -- left for that song; back: whether it was cleared
local talkCafe = false                 -- Mr.Caspian's call: cafe ambience behind him
local cafeSnd, cafeOn, cafeK, cafeTo, cafeNode = nil, false, 0, nil, nil

-- ==== persistence ====
-- room and jukebox song in one save string (store.lua); never written by a visitor or an empty slot
local function curSave() return GetSaveFile(playerIndex) end
local function canPersist()
    if net.online and not net.isHost then return false end   -- a visitor never writes the host's room into their own save
    local sf = curSave()
    return sf ~= nil and type(sf.SaveUID) == "string" and sf.SaveUID ~= ""
end
local function writeStore()
    if room == nil or not canPersist() then return end
    Store.write(curSave(), room:toTable(), savedJukebox)
end
local saveRoom   -- forward decl (used by loadRoom)
local heldWritten = {}   -- owned counts last written (rewritten on change)
-- the playing song persists with the room: every playback op writes it (JB ctx.onState), loadRoom resumes it
local persistLocked = false    -- set after deactivate's snapshot: the teardown stopAll
                               -- fires onState with an empty state
local function persistJukebox()
    if persistLocked or (IsSongsEnumDone and not IsSongsEnumDone()) then return end
    if not canPersist() then return end   -- never persist a host's song into a guest save
    savedJukebox = JB.persistState()
    writeStore()
end
-- loads the current player's room into the existing room object (Room:reset keeps the table for edit/MO)
-- no saved room: fresh default; coin-shop purchases (global counters) are claimed and saved here
local function loadRoom()
    persistLocked = false          -- fresh stage entry / room switch: persisting is live again
    heldWritten = {}
    if room == nil then room = Room.newDefault() else room:reset() end
    local sf = curSave()
    local t, jbSaved, stored = Store.read(sf)
    local dirty = false
    -- temporary: a save whose string is unset takes its room from OWM's old store, once
    if stored == "" and canPersist() then
        local oldRoom, oldJukebox = Store.legacy(sf, playerIndex)
        if oldRoom ~= nil or oldJukebox ~= nil then
            t, jbSaved, dirty = oldRoom, oldJukebox, true
            if debugLog then debugLog("my_room: moved the room of save " .. tostring(sf.SaveUID) .. " from OWM's old store") end
        end
    end
    savedJukebox = jbSaved
    if t then
        if room:loadTable(t) then dirty = true end   -- a gift joined the stock
        -- pieces the live rules refuse go back to stock
        local evicted = Integrity.sweep(room)
        if #evicted > 0 then
            dirty = true
            if debugLog then debugLog("myroom: returned to stock on load: " .. table.concat(evicted, ", ")) end
        end
    end
    if sf then
        local gained = room:drainShopGrants(function(n) return sf:GetGlobalCounter(n) end)
        if gained then dirty = true end
    end
    if dirty then saveRoom() end   -- persist right away (idempotent)
    -- jbSaved is read before stopAll (stopping persists an empty state)
    JB.stopAll()
    if jbSaved then
        JB.restoreState(jbSaved)
    end
end
saveRoom = function()
    if room == nil or not canPersist() then return end
    writeStore()
    -- room size and piece count, for the title's save list
    local sf = curSave()
    if sf then
        pcall(function()
            sf:SetGlobalCounter(".myroom_iw", room.iw)
            sf:SetGlobalCounter(".myroom_ih", room.ih)
            sf:SetGlobalCounter(".myroom_items", #room.furniture + #room.wallItems)
            -- owned count per reward item (read by the secret vault)
            for _, id in ipairs(Room.REWARD_IDS) do
                local n = room:heldCount(id)
                if heldWritten[id] ~= n then
                    sf:SetGlobalCounter(Room.heldCounter(id), n)
                    heldWritten[id] = n
                end
            end
        end)
    end
end

-- ==== 3D room build ====
-- ceiling lamp plus every light source in the room
local function applyLights()
    local cx, cz = room:centerWorld(world)
    local gh = room:gridH()
    local lights = {
        { x = cx, y = FY + 2.7, z = cz, r = 1.0, g = 0.86, b = 0.62, intensity = 0.95, range = 8 },
    }
    for _, l in ipairs(room:windowLights()) do lights[#lights + 1] = l end      -- daylight through each window
    for _, l in ipairs(room:lampLights(world)) do lights[#lights + 1] = l end   -- lit floor lamps
    for _, l in ipairs(TV.lights(world)) do lights[#lights + 1] = l end         -- TVs that are on
    for _, l in ipairs(MX.lights(world)) do lights[#lights + 1] = l end         -- matrices
    for _, l in ipairs(FT.lights(world)) do lights[#lights + 1] = l end         -- lit fish tanks
    for _, l in ipairs(LT.lights(world)) do lights[#lights + 1] = l end         -- lit lanterns
    for _, l in ipairs(room:itemLights(world)) do lights[#lights + 1] = l end   -- items that always glow
    world.daynight:setLights(lights)
    FT.refresh()
end

local shellBuilt = nil   -- the shell the room was last fully built with (Room:shellKey)
-- "kept" when only the furniture was updated (standing models kept)
local function rebuild()
    if map ~= nil and room._shellPhys ~= nil and shellBuilt == room:shellKey() then
        room:syncProps(world)
        applyLights()
        TV.tick(0)
        MX.tick(0)
        JB.refreshGlow()
        return "kept"
    end
    shellBuilt = room:shellKey()
    -- keep the live orbit, zoom and focus on a rebuild; frame fresh only on the first build
    local hadRoom = (map ~= nil) and (world.cam ~= nil)
    local keepYaw   = world.cam and world.cam.yaw or 45
    local keepPitch = world.cam and world.cam.pitch or -33
    local keepDist  = hadRoom and world.cam.dist or nil
    local keepTx, keepTy, keepTz
    if hadRoom then keepTx, keepTy, keepTz = world.cam.tx, world.cam.ty, world.cam.tz end

    map = world:loadMap("room")
    -- fit distance from interior size and fov (first framing, zoom limits)
    local span = math.max(room.iw, room.ih) + 3
    local fit = span / (2 * math.tan(rad(FOV / 2))) * 1.05
    world.cam:setRig{ yaw = keepYaw, pitch = keepPitch, fov = FOV, dist = keepDist or fit }
    -- no camera collision: the wall clamp and zoom limits keep the framing
    -- (the boom would jump off geometry in the small room)
    world.cam.collide = false
    world.cam.minDist, world.cam.maxDist = fit * 0.45, fit * 1.6
    local cx, cz = room:centerWorld(world)
    if hadRoom then world.cam:setTarget(keepTx, keepTy, keepTz)
    else world.cam:setTarget(cx, FY + 0.4, cz) end
    if world.scene.SetShadowArea then world.scene:SetShadowArea(math.max(room.iw, room.ih) + 5) end
    -- re-set after every loadMap: unloading a map clears the scene light list
    applyLights()
    -- new models start at their catalog glow: apply this frame's glow and colours now
    -- (else a moved piece makes every light blink)
    TV.tick(0)
    MX.tick(0)
    JB.refreshGlow()
end

local function spawnAtEntrance()
    local sc, sr = room:spawnCell()
    px, pz = world:cellToWorld(sc, sr)
    py = FY
    player:setPos(px, py + 0.05, pz)
end

-- student and palette of the room owner's save (set by the mirror and the customize tab)
local function refreshLook()
    local sf = curSave()
    look.student = Student.ensure(sf)
    look.palette = Student.index(sf, look.student)
    look.set = AV.prepare(look.student, look.palette)
end


-- ==== lifecycle ====
function onStart()
    -- gfont (LuaGlyphText): UTF-8-aware layout; the LuaText path walks bytes (Japanese came out empty)
    -- theme.text white + the dialogue's black outline: readable on any background
    dlg = Dialogue.new({ gfont = TEXT:CreateGlyphCached(26),
                         fonts = { name = TEXT:Create(30), text = TEXT:Create(26) }, portraits = {},
                         theme = { text = { 255, 255, 255 } },
                         -- {sfx:purse} in a landlord line brings the purse up; any other name is a sound
                         onSfx = function(name)
                             if name == "purse" then Landlord.showPurse() else playPhoneSound(name) end
                         end,
                         -- landlord voice blips; any other line uses the blip it names, or none
                         onBlip = function()
                             if phoneFlow == "landlord" then playPhoneSound("blip_landlord" .. math.random(3))
                             elseif dlg.node ~= nil and dlg.node.blip ~= nil then playPhoneSound(dlg.node.blip) end
                         end, blipEvery = 3 })

    -- bake the coin-shop furniture previews once as shared textures (the shop reads them by key)
    -- GLB/builder items via Edit.bakeThumbnails, swatches share their PNGs; one bake per session
    local ok, err = pcall(function()
        local n = Edit.bakeThumbnails(Room.SHOP_MODEL_IDS, "myroom_thumb_", 256)
        -- reward items, for the reward pop-up and the secret vault
        local extra = {}
        for _, id in ipairs(Room.REWARD_IDS) do
            local sold = false
            for _, s in ipairs(Room.SHOP_MODEL_IDS) do if s == id then sold = true end end
            if not sold then extra[#extra + 1] = id end
        end
        n = n + Edit.bakeThumbnails(extra, "myroom_thumb_", 256)
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
    takeWayBack()
    INPUT:SetMouseLocked(false)
    Landlord.reset()                             -- refusals reset every visit
    purse:hide()
    ringT, ringScript, ringDue = 0, nil, false
    branchCall = nil
    if talkAway then talkAway = false; talkBack = Talk.cleared() end
    if world ~= nil then return end
    LOADING:Add("Room", 5, function()
        world = OWM.World.new{ rw = RW, rh = RH, screenW = SCREEN_W, screenH = SCREEN_H,
                               fov = FOV, yaw = 45, pitch = -33, lit = true }
        world:setDiorama(2.0, 1.1, 0.22, 0.26)
        world:setFog(false)                        -- interior: no fog
        world.scene:SetAntialias(true)             -- FXAA
        A.buildAll(world.scene)
        AV.attach(world.scene)
        loadRoom()
        world:registerMap("room", { type = "proc", build = function(w) return room:buildMap(w) end })
        player = world.phys:newCharacter{ radius = 0.32, accel = 13, decel = 16 }
        hud = UI.new{}
        pcScreen = PCS.new()
        edit = Edit.new(room, world, function()
            local how = rebuild(); saveRoom(); MO.onRoomChanged()
            JB.onRoomEdited(room)      -- stop playback if its jukebox got removed; refresh its cell
            return how
        end)
        MO.init({
            getRoom         = function() return room end,
            applyRoom       = function(t) room:loadTable(t) end,
            rebuildRoom     = function() rebuild() end,
            spawnAtEntrance = function() spawnAtEntrance() end,
            applyJukebox    = function(t) JB.applyNetState(t) end,   -- guest: mirror the host's player
            jukeboxState    = function() return JB.netState() end,   -- host: state for late joiners
            look            = function() return look.student, look.palette end,   -- shown to the others
        })
        rebuild()
        refreshLook()
        spawnAtEntrance()
        openPlayerSelect()          -- pick whose room to view/edit (skips itself when only one save)
    end)
end

function deactivate()
    GLOBALCAMERA:Reset()
    if mode == "reward" then
        local modal = ROACTIVITY:GetROActivity("modal")
        if modal ~= nil and modal.IsActive then modal:Deactivate() end
        mode = "play"
    end
    saveRoom()
    if pcScreen and pcScreen:active() then pcScreen:close() end
    if mode == "edit" and edit then edit:leave() end
    PCS.tickBgmFade(math.huge)                     -- finish the fade-out
    persistJukebox()                               -- snapshot the live position first,
    persistLocked = true                           -- lock it against the teardown's onState,
    JB.stopAll()                                   -- then stop jukebox audio and UI
    JB.close()
    JB.setDuck(false)                              -- reset the PC-screen crossfade for the next visit
    if phoneUI then phoneUI:disposeWidgets(); phoneUI = nil end
    if hud then hud:disposeWidgets(); hud = nil end
    MX.dispose()
    MR.dispose()
    WB.dispose()
    RB.dispose()
    AV.dispose()
    look.set = nil
    purse:dispose()
    Pod.reset()
    -- free any preview icon its owner missed
    pcall(function() OWM.ModelIcon.disposeAll() end)
    if world ~= nil then world.scene:Dispose(); world = nil; player = nil; map = nil end
    mode = "play"
end

function afterSongEnum() JB.onSongEnumDone() end   -- jukebox Songs tab

function onDestroy()
    MO.leave()
    JB.dispose()
    PCS.disposeBgm()
    if Edit.disposeSfx then Edit.disposeSfx() end
    for _, s in pairs(phoneSfx) do if s then pcall(function() s:Dispose() end) end end
    if cafeSnd then pcall(function() cafeSnd:Dispose() end) end
    cafeSnd = nil
    Pod.dispose()
    phoneSfx = {}; bombFx = nil
end

local function kd(k) return INPUT:KeyboardPressing(k) end
local function kp(k) return INPUT:KeyboardPressed(k) end

-- interactables in range, ordered exit, furniture, phone: { kind, it?, key (stable focus id) }
-- guests get only the exit; a hosting host keeps the phone and jukeboxes (playback reaches guests)
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
                    elseif cat.interact == "tv" then
                        seen[it] = true; list[#list + 1] = { kind = "tv", it = it, key = "tv:" .. tostring(it) }
                    elseif cat.interact == "pod" then
                        seen[it] = true; list[#list + 1] = { kind = "pod", it = it, key = "pod:" .. tostring(it) }
                    elseif cat.interact == "matrix" then
                        seen[it] = true; list[#list + 1] = { kind = "matrix", it = it, key = "matrix:" .. tostring(it) }
                    elseif cat.interact == "mirror" then
                        seen[it] = true; list[#list + 1] = { kind = "mirror", it = it, key = "mirror:" .. tostring(it) }
                    elseif cat.interact == "look" then
                        seen[it] = true; list[#list + 1] = { kind = "look", it = it, key = "look:" .. tostring(it) }
                    elseif cat.interact == "workbench" then
                        seen[it] = true; list[#list + 1] = { kind = "workbench", it = it, key = "workbench:" .. tostring(it) }
                    elseif cat.interact == "recycle" then
                        seen[it] = true; list[#list + 1] = { kind = "recycle", it = it, key = "recycle:" .. tostring(it) }
                    elseif cat.interact == "fishtank" then
                        seen[it] = true; list[#list + 1] = { kind = "fishtank", it = it, key = "fishtank:" .. tostring(it) }
                    elseif cat.interact == "lantern" then
                        seen[it] = true; list[#list + 1] = { kind = "lantern", it = it, key = "lantern:" .. tostring(it) }
                    end
                end
            end
        end
        local fc, fr = room:phoneCell()
        if math.abs(pc - fc) + math.abs(pr - fr) <= 1 then list[#list + 1] = { kind = "phone", key = "phone" } end
    end
    return list
end

-- focus highlight: pulsing emissive on the item's own objects; the exit (no model) gets a footprint square
-- last frame's glow is restored to its base first
local function interactGlow(focused)
    local scene = world.scene
    if world._glow then                                   -- restore last frame's glowed object(s)
        for _, e in ipairs(world._glow) do
            if scene.ObjSetEmissive then pcall(function() scene:ObjSetEmissive(e.o, e.r, e.g, e.b) end) end
        end
        world._glow = nil
    end
    if world._footprint ~= false then                     -- clear the footprint layer unless it is known empty
        scene:ObjSetPass(world.hiliteObj, 1, 110, 185, 255, 0)
        scene:ObjBegin(world.hiliteObj)
        world._footprint = false
    end
    if not focused then return end

    -- live model instance: ground GLB (_propInst) or the wall phone (_wallGlb)
    local inst = nil
    if focused.kind == "phone" then
        local p = room:phoneItem()
        inst = p and room._wallGlb and room._wallGlb[p] or nil
    elseif focused.it then
        inst = world._propInst and world._propInst[focused.it] or nil
    end

    if inst and scene.ObjSetEmissive then
        -- pulse an emissive add over each object's base glow
        -- (the jukebox screen keeps its idle glow, else walking away leaves it dark)
        local br, bg, bb = 0, 0, 0
        if focused.kind == "lamp" and focused.it and focused.it.lit ~= false then br, bg, bb = 0.9, 0.72, 0.36 end
        local partBase = nil
        if focused.kind == "jukebox" and inst.parts then
            partBase = {}
            for _, pp in ipairs(inst.parts) do partBase[pp.obj] = { 0.12, 0.114, 0.096 } end
        end
        if focused.kind == "fishtank" and inst.parts then
            partBase = {}
            for _, pp in ipairs(inst.parts) do partBase[pp.obj] = FT.glowNow(focused.it, pp.material) end
        end
        if focused.kind == "matrix" and inst.parts then
            partBase = {}
            for _, pp in ipairs(inst.parts) do partBase[pp.obj] = MX.glowNow(pp.material, focused.it) end
        end
        -- an on TV's screen keeps its own light: only the body pulses
        local skipObj = (focused.kind == "tv") and TV.screenObj(focused.it) or nil
        local p = 0.30 + 0.30 * (0.5 + 0.5 * sin(lastTs * 0.006))   -- 0.30 .. 0.90 glow
        world._glow = {}
        for _, o in ipairs(inst.objs or { inst.obj }) do
            if o ~= skipObj then
                local rb = partBase and partBase[o] or nil
                local r0, g0, b0 = br, bg, bb
                if rb then r0, g0, b0 = rb[1], rb[2], rb[3] end
                world._glow[#world._glow + 1] = { o = o, r = r0, g = g0, b = b0 }
                pcall(function() scene:ObjSetEmissive(o, r0 + p, g0 + p, b0 + p) end)
            end
        end
        return
    end

    -- exit or model-less interactable: pulsing footprint square
    local gh = room:gridH()
    local pulse = 70 + math.floor(80 * (0.5 + 0.5 * sin(lastTs * 0.006)))
    scene:ObjSetPass(world.hiliteObj, 1, 110, 185, 255, pulse)
    scene:ObjBegin(world.hiliteObj)
    world._footprint = true
    if focused.kind == "exit" then
        local pc, pr = world:worldToCell(px, pz)
        local x0, z0 = pc, gh - 1 - pr
        scene:ObjAddQuadFlat(world.hiliteObj, x0 + 0.06, FY + 0.06, z0 + 0.06, x0 + 0.94, FY + 0.06, z0 + 0.06,
            x0 + 0.94, FY + 0.06, z0 + 0.94, x0 + 0.06, FY + 0.06, z0 + 0.94)
    end
end

-- ==== phone ====
-- UI library menus; the landlord call is a VN.Dialogue conversation
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

-- outgoing call: coin and ring first; `script` starts after the ring
local function placeCall(script)
    playPhoneSound("phone_coin")
    mode = "dialogue"
    ringT, ringScript, ringDue = 2.6, script, true     -- the ring starts once the coin has dropped
end

-- ==== dialable numbers (data/phone_numbers.json) ====
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
        -- egg sounds: the stage's own Sounds/ folder
        pcall(function() s = SOUND:CreateSFX("Sounds/" .. stem .. ".ogg") end)
        if s then pcall(function() s:SetVolume(100) end) end
        phoneSfx[stem] = s or false
    end
    if phoneSfx[stem] then return pcall(function() phoneSfx[stem]:Play() end) end
    return false
end
local function startBombs() bombFx = { t = 0, dur = 3.0, next = 0 } end

-- gives a furniture item with the reward pop-up; false when it can't be given here
local RARITY_MODAL = { Poor = 0, Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4, Mythical = 4 }
local function giveFurniture(id)
    local cat = Room.CATALOG[id]
    if cat == nil or (net.online and not net.isHost) then return false end
    room:invAdd(id, 1)
    saveRoom()
    local rarity = cat.rarity or "Common"
    local modal = ROACTIVITY:GetROActivity("modal")
    if modal ~= nil then
        pcall(function() Edit.bakeThumbnails({ id }, "myroom_thumb_", 512) end)
        modal:Activate(playerIndex + 1, RARITY_MODAL[rarity] or 0, 5, Room.displayName(id), rarity, "myroom_thumb_" .. id,
            Room.displayName("new_furniture"))
        mode = "reward"
    else
        mode = "play"
    end
    return true
end
-- branching call (entry with "start"): each step is a phone.json id of one line or a list, said by the entry's name
-- a step under "choices" ends on them, each leading to its "next"; any other step is an answer, then "close"
-- "daily": one answer per shop day; a later call that day shows the "daily" text, then hangs up
local function evText(v)
    local s = nil
    pcall(function() s = JSONLOADER:ExtractText(v) end)
    if s == "" then return nil end
    return s
end
local function callDay() return require("Shop.Daily").freezeKey() end
local function callCounter(key) return ".myroom_call_" .. key end
local function answeredToday(key)
    local ok, v = pcall(function() return curSave():GetGlobalCounter(callCounter(key)) end)
    return ok and tonumber(v) == callDay()
end
local function callLines(call, step, nodes)
    local lines = PHONE:list(step)
    if #lines == 0 then lines = { PHONE:tr(step) } end
    for _, s in ipairs(lines) do nodes[#nodes + 1] = { name = call.name, text = s, blip = call.blip } end
    return nodes
end
local function callStep(call, step)
    local nodes = callLines(call, step, {})
    local choices = {}
    pcall(function()
        local list = call.ev["choices"][step]
        for i = 0, list.Count - 1 do
            local label, nxt = evText(list[i]["label"]), evText(list[i]["next"])
            if label and nxt then choices[#choices + 1] = { label = PHONE:tr(label), value = nxt } end
        end
    end)
    if #choices > 0 then
        nodes[#nodes].choices = choices
        return nodes
    end
    if call.close then callLines(call, call.close, nodes) end
    call.answered = true
    if call.daily and canPersist() then
        pcall(function() curSave():SetGlobalCounter(callCounter(call.key), callDay()) end)
    end
    return nodes
end
-- cafe ambience behind Mr.Caspian: his first line starts it, Vault.Talk.CAFE lines set its level
local CAFE_VOLUME = 60
local function stopCafe()
    if cafeSnd and cafeOn then pcall(function() cafeSnd:Stop() end) end
    talkCafe, cafeOn, cafeK, cafeTo, cafeNode = false, false, 0, nil, nil
end
local function tickCafe(dt)
    local nd = dlg.activeFlag and dlg.node or nil
    if nd ~= nil and nd ~= cafeNode then
        cafeNode = nd
        if nd.cafe ~= nil then cafeTo = nd.cafe elseif nd.who == "C" and cafeTo == nil then cafeTo = 1 end
    end
    if cafeTo == nil then return end
    if cafeSnd == nil then
        pcall(function() cafeSnd = SOUND:CreateSFX("Sounds/phone_cafe.ogg") end)
        cafeSnd = cafeSnd or false
    end
    if not cafeSnd then return end
    if cafeTo <= 0 then cafeK = 0                                  -- instant cut
    elseif cafeK < cafeTo then cafeK = math.min(cafeTo, cafeK + dt / 0.5)
    else cafeK = math.max(cafeTo, cafeK - dt / 1.5) end
    if not cafeOn then
        cafeOn = true
        cafeSnd:SetLoop(true)
        cafeSnd:SetVolumePercent(0)
        cafeSnd:Play()
    end
    cafeSnd:SetVolumePercent(CAFE_VOLUME * cafeK)
end
local function endCall()
    stopCafe()
    branchCall = nil
    mode = "play"; phoneFlow = nil
    playPhoneSound("phone_cut")
end
local function startBranchCall(key, ev)
    local call = { key = key, ev = ev, name = evText(ev["name"]) or "", blip = evText(ev["blip"]),
                   close = evText(ev["close"]), daily = evText(ev["daily"]) }
    mode = "dialogue"; phoneFlow = nil
    if call.daily and answeredToday(key) then
        call.answered = true
        branchCall = call
        phoneFlow = "branch"
        dlg:start(callLines({ name = "" }, call.daily, {}))
        return
    end
    branchCall = call
    phoneFlow = "branch"
    dlg:start(callStep(call, evText(ev["start"])))
end
-- next step from a choice; nil when the call is over
local function branchNext(result)
    if branchCall == nil or branchCall.answered or type(result) ~= "string" then return nil end
    return callStep(branchCall, result)
end
-- Mr.Caspian's and Gris's numbers: Vault.Talk runs the call, the phone dialogue says its lines
local TALK_BLIP = { C = "blip_caspian", O = "blip_caspian", G = "blip_gris", U = "blip_gris", N = "blip_narrator" }
local talkStep
local function talkNodes(list)
    local nodes = {}
    for i, n in ipairs(list) do
        local node = { name = Talk.name(n.who), text = (Talk.text(n.key):gsub("[{}]", "")), blip = TALK_BLIP[n.who],
                       who = n.who, cafe = Talk.CAFE[n.key] }
        if n.choices then
            node.choices = {}
            for j, c in ipairs(n.choices) do node.choices[j] = { label = (c.label:gsub("[{}]", "")), value = j } end
        end
        nodes[i] = node
    end
    return nodes
end
local function talkResume(...)
    if talkCo ~= nil then
        local ok, r = coroutine.resume(talkCo, ...)
        if not ok then
            if debugLog then debugLog("my_room call: " .. tostring(r)) end
        elseif coroutine.status(talkCo) ~= "dead" and r ~= nil then
            if r[1] == "say" then talkRun = r[2]; return talkStep() end
            if r[1] == "play" then talkPlay = r[2]; return end
            if r[1] == "reward" and Chests.showReward(r[2]) then mode = "reward"; return end
            return talkResume()          -- the vault's staging: nothing to do on the phone
        end
    end
    talkCo, talkRun = nil, nil
    endCall()
end
talkStep = function()
    local kind, x = talkRun:step()
    if kind == "lines" then
        dlg:start(talkNodes(x))
        if x[1].shown then dlg.revealed = dlg.total or 0 end
        return
    end
    talkResume(kind == "action" and x or nil)
end
local function dialPhone(input)
    local key = tostring(input or ""):gsub("%s+", ""):lower()
    local flow = nil
    if Talk then
        Talk.slot = playerIndex
        flow = Talk.call(key)
    end
    if flow ~= nil then
        -- ring, then the call answers
        talkCo, talkRun, talkPlay = flow, nil, nil
        talkCafe = key == Talk.CASPIAN_NUMBER
        mode = "dialogue"; phoneFlow = "talk"
        ringT, ringScript, ringDue = 2.6, talkResume, true
        return true
    end
    local doc = phoneNumbersDoc()
    local ev = nil
    pcall(function() ev = doc and doc[key] or nil end)
    if ev == nil then                                 -- unassigned number: placeholder
        local n = (input and input ~= "" and input) or PHONE:tr("the_number")
        mode = "dialogue"; phoneFlow = nil
        dlg:start({ { name = "", text = PHONE:trf("no_answer", n), blip = "blip_narrator" } })
        return false
    end
    -- pcall: a malformed entry falls back to the no-answer line instead of erroring the update
    local name, text, sound, effect, blip, give
    local okEv = pcall(function()
        give   = JSONLOADER:ExtractText(ev["give"])
        blip   = JSONLOADER:ExtractText(ev["blip"])
        name   = JSONLOADER:ExtractText(ev["name"])
        text   = JSONLOADER:ExtractText(ev["text"])
        sound  = JSONLOADER:ExtractText(ev["sound"])
        effect = JSONLOADER:ExtractText(ev["effect"])
    end)
    if not okEv then
        local n = (input and input ~= "" and input) or PHONE:tr("the_number")
        mode = "dialogue"; phoneFlow = nil
        dlg:start({ { name = "", text = PHONE:trf("no_answer", n), blip = "blip_narrator" } })
        return
    end
    if okEv and give ~= nil and give ~= "" then
        -- reward number: the item once, then quest_cleared while the room holds it
        phoneFlow = nil
        if room:holds(give) or not giveFurniture(give) then
            mode = "dialogue"
            dlg:start({ { name = "", text = PHONE:tr("quest_cleared"), blip = "blip_narrator" } })
        end
        return
    end
    if evText(ev["start"]) then
        startBranchCall(key, ev)
        return
    end
    if text then text = PHONE:tr(text) end       -- the entry's text is a phone.json id (egg_*); names stay as written
    -- a call of several messages: each line has its own name and text id
    local script = nil
    pcall(function()
        local list = ev["lines"]
        if list == nil then return end
        local nodes = {}
        for i = 0, list.Count - 1 do
            local id = JSONLOADER:ExtractText(list[i]["text"])
            if id and id ~= "" then
                local who = JSONLOADER:ExtractText(list[i]["name"]) or ""
                local blip = JSONLOADER:ExtractText(list[i]["blip"])
                if (blip == nil or blip == "") and who == "" then blip = "blip_narrator" end
                if blip == "" then blip = nil end
                nodes[#nodes + 1] = { name = who, text = PHONE:tr(id), blip = blip }
            end
        end
        if #nodes > 0 then script = nodes end
    end)
    local wantSilent
    if sound and sound ~= "" then wantSilent = playPhoneSound(sound) end
    if effect == "bombs" then startBombs() end
    if script then
        mode = "dialogue"; phoneFlow = nil
        dlg:start(script)
    elseif text == nil or text == "" then mode = "play"  -- sound only, no message
    else
        mode = "dialogue"; phoneFlow = nil
        dlg:start({ { name = name or "", text = text or "", blip = (blip ~= nil and blip ~= "") and blip or "blip_narrator" } })
    end
    return wantSilent
end

-- bomb storm: bursts around the player and camera shake, each frame while active
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
-- landlord.lua runs the landlord call with these closures
-- lang/<code>/dialogs.json lines; English fills a missing file, key or line
local DIALOGS = I18N.texts("dialogs")
local function dlgLoc(section, key, fallback)
    return DIALOGS:get(section, key) or fallback
end
-- lines at a key path (section, key, ...), each falling back to English
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

-- purse on the dialogue box's top edge, level with the name pill (box y 770, pill 740..794), right side
purse = Purse.new{ x = SCREEN_W - 60 - 300, y = 770 - 36 }
Landlord.init{
    save = function() return curSave() end,
    room = function() return room end,
    extend = doExtend,
    dlgLoc = dlgLoc, dlgList = dlgList,
    coinBox = purse,
}

-- jukebox ctx: closures over live state (world/room are nil until activate)
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
    onState = function() persistJukebox() end,   -- every playback op saves the resume state
    scene = function() return world and world.scene or nil end,
}
-- PC screen crossfade: JB.setDuck on open/close, PCS.tickBgmFade in update

-- TV ctx: jukebox ducked while a cutscene plays
TV.init{
    theme = PHONE_THEME,
    playerIndex = function() return playerIndex end,
    save = function() return curSave() end,
    room = function() return room end,
    scene = function() return world and world.scene or nil end,
    propInstFor = function(it) return world and world._propInst and world._propInst[it] or nil end,
    catalog = function(id) return Room.CATALOG[id] end,
    displayName = function() return Room.displayName("tv") end,
    onToggled = function() rebuild(); saveRoom(); MO.onRoomChanged() end,
    duck = function(on) JB.setDuck(on, on and 0 or 0.15) end,
}

MX.init{
    theme = PHONE_THEME,
    playerIndex = function() return playerIndex end,
    save = function() return curSave() end,
    room = function() return room end,
    scene = function() return world and world.scene or nil end,
    propInstFor = function(it) return world and world._propInst and world._propInst[it] or nil end,
    displayName = function() return Room.displayName(MX.ID) end,
}

FT.init{
    room = function() return room end,
    scene = function() return world and world.scene or nil end,
    propInstFor = function(it) return world and world._propInst and world._propInst[it] or nil end,
}

LT.init{
    room = function() return room end,
    scene = function() return world and world.scene or nil end,
    propInstFor = function(it) return world and world._propInst and world._propInst[it] or nil end,
}

-- the mirror changes the room owner's save
MR.init{
    theme = PHONE_THEME,
    playerIndex = function() return playerIndex end,
    save = function() return curSave() end,
    displayName = function() return Room.displayName(MR.ID) end,
    onChanged = function() refreshLook() end,
}

WB.init{
    theme = PHONE_THEME,
    playerIndex = function() return playerIndex end,
    room = function() return room end,
    onChanged = function() saveRoom() end,
}

RB.init{
    theme = PHONE_THEME,
    playerIndex = function() return playerIndex end,
    room = function() return room end,
    save = function() return curSave() end,
    onChanged = function() saveRoom() end,
}

local buildPhoneMenu   -- forward decl (the textbox panes route Back into it)

local function buildPhoneTextPane(title, hintText, maxLen, confirmLabel, onConfirm)
    if phoneUI then phoneUI:disposeWidgets() end
    local ui = UI.new{ theme = PHONE_THEME, navPlayer = playerIndex + 1 }
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

-- the player's numbers (Vault.Talk.contacts); choosing one dials it
local function phoneContacts()
    if Talk == nil then return {} end
    Talk.slot = playerIndex
    return Talk.contacts()
end
local function buildContactsPane(list)
    if phoneUI then phoneUI:disposeWidgets() end
    local ui = UI.new{ theme = PHONE_THEME, navPlayer = playerIndex + 1 }
    phoneUI = ui
    local items = {}
    for i, c in ipairs(list) do items[i] = { text = c.name .. "  (" .. c.number .. ")", value = c.number } end
    items[#items + 1] = { text = PHONE:tr("back"), value = "back" }
    local x, y, w = SCREEN_W / 2 - 360, 260, 720
    local h = 130 + #items * 78 + 60
    ui:panel{ x = x, y = y, w = w, h = h, title = PHONE:tr("contacts_title") }
    ui:menu{
        x = x + 36, y = y + 96, w = w - 72, h = #items * 78, rowHeight = 78, items = items,
        onSelect = function(_, it)
            if it.value == "back" then buildPhoneMenu(); return true end
            closePhone()
            playPhoneSound("phone_coin")
            return dialPhone(it.value)
        end,
    }
end

buildPhoneMenu = function()
    if phoneUI then phoneUI:disposeWidgets() end
    local ui = UI.new{ theme = PHONE_THEME, navPlayer = playerIndex + 1 }
    phoneUI = ui
    local entries
    if net.online and net.isHost then
        -- while hosting: stop hosting only
        entries = { { label = PHONE:tr("menu_stop_hosting"), value = "stophost" } }
    else
        entries = {
            { label = PHONE:tr("menu_landlord"), value = "landlord" },
            { label = PHONE:tr("menu_number"), value = "number" },
            -- greyed while the player has no number
            { label = PHONE:tr("menu_contacts"), value = "contacts", locked = #phoneContacts() == 0 },
        }
        if not net.online and not net.connecting then
            entries[#entries + 1] = { label = PHONE:tr("menu_invite"), value = "invite" }
            entries[#entries + 1] = { label = PHONE:tr("menu_join"), value = "join" }
        end
    end
    entries[#entries + 1] = { label = PHONE:tr("menu_hang_up"), value = "close" }
    local items = {}
    for i, e in ipairs(entries) do items[i] = { text = e.label, value = e.value, locked = e.locked } end
    local x, y, w = SCREEN_W / 2 - 360, 260, 720
    local h = 130 + #items * 78 + 60
    ui:panel{ x = x, y = y, w = w, h = h, title = PHONE:tr("title") }
    ui:menu{
        x = x + 36, y = y + 96, w = w - 72, h = #items * 78, rowHeight = 78, items = items,
        drawLocked = function() end,     -- a greyed row, without the padlock
        onSelect = function(_, it)
            local v = it.value
            if v == "contacts" then
                buildContactsPane(phoneContacts())
                return true
            elseif v == "landlord" then
                closePhone(); phoneFlow = "landlord"
                placeCall(Landlord.script())
            elseif v == "number" then
                buildPhoneTextPane(PHONE:tr("menu_number"), PHONE:tr("number_placeholder"), 16, PHONE:tr("call"), function(t)
                    closePhone();
                    playPhoneSound("phone_coin")
                    return dialPhone(t)  -- data/phone_numbers.json lookup
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

-- ==== whose room ====
-- chosen on the title, or kept across a trip to another stage
local RETURN_KEY = "my_room_place"   -- "<slot>,<x>,<y>,<z>": whose room and where the player stood, for a round trip
local BACK_KEY = "my_room_return"    -- the stage to go back to, set by the caller: "<stage>" or "<stage>|<transition>"
local backRaw = ""                   -- that string as read on entry (put back for a round trip)
local backStage, backTransition = "_title", nil
-- read and clear the way back on every entry; empty: _title
takeWayBack = function()
    local ok, s = pcall(function() return SHARED:GetSharedString(BACK_KEY) end)
    pcall(function() SHARED:SetSharedString(BACK_KEY, "") end)
    backRaw = (ok and type(s) == "string") and s or ""
    local stage, transition = backRaw:match("^([^|]*)|?(.*)$")
    backStage = (stage ~= nil and stage ~= "") and stage or "_title"
    backTransition = (transition ~= nil and transition ~= "") and transition or nil
end
local function leaveRoom() return Exit("stage", backStage, backTransition) end
-- commit to a player: their save (GetSaveFile(playerIndex)) and their room
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
    refreshLook()
    spawnAtEntrance()
    mode = "play"
end
-- before a round trip: store RETURN_KEY and put BACK_KEY back
local function rememberPlace()
    pcall(function()
        SHARED:SetSharedString(RETURN_KEY, string.format("%d,%.3f,%.3f,%.3f", playerIndex, px, py, pz))
        SHARED:SetSharedString(BACK_KEY, backRaw)
    end)
end
openPlayerSelect = function()
    local ok, s = pcall(function() return SHARED:GetSharedString(RETURN_KEY) end)
    pcall(function() SHARED:SetSharedString(RETURN_KEY, "") end)
    local slot, x, y, z = nil, nil, nil, nil
    if ok and type(s) == "string" then
        local a, b, c, d = s:match("^(%d+),([%-%d%.]+),([%-%d%.]+),([%-%d%.]+)$")
        slot, x, y, z = tonumber(a), tonumber(b), tonumber(c), tonumber(d)
    end
    local sf = slot and GetSaveFile(slot) or nil
    if sf and sf.SaveUID and sf.SaveUID ~= "" and x and y and z then
        SavePicker.take()
        pickPlayer(slot)
        px, py, pz = x, y, z
        player:setPos(px, py + 0.05, pz)
        return
    end
    pickPlayer(SavePicker.take() or SavePicker.first())
end

local function onDialogueDone(result)
    if phoneFlow == "talk" then
        if talkRun == nil then endCall(); return end
        if talkRun.asking then talkRun:answer(result) end
        talkStep()
        return
    end
    if phoneFlow == "branch" then
        local nxt = branchNext(result)
        if nxt then dlg:start(nxt) ; return end
        endCall()
        return
    end
    if phoneFlow == "landlord" then
        local nxt = Landlord.onDone(result)
        if nxt then dlg:start(nxt) ; return end
        playPhoneSound("phone_cut")          -- landlord hangs up
    end
    mode = "play"; phoneFlow = nil
end

-- end the online session and go back to our own room, in-stage (visitor leaving or kicked, host closing)
local function backToOwnRoom(message)
    JB.stopAll()                        -- the host's jukebox does not follow us home
    JB.close()                          -- nor a hidden jukebox window taking input
    MO.leave()
    closePhone()
    if pcScreen and pcScreen:active() then pcScreen:close() end
    loadRoom(); rebuild(); spawnAtEntrance()
    mode = "play"; pmoving = false
    if message then msg = message; msgT = 4 end
end

-- ==== per-frame ====
local function settlePlayer(dt)
    player:move(dt, 0, 0, 0, false)          -- decelerate + keep gravity while a menu is up
    px, py, pz = player:pos()
end

-- orbit clamp: the room has only a back wall (far z) and a right wall (+x); yaw stays in [10, 80]
-- (yaw 45 looks into that corner; wrap-aware, the far side splits at 225)
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

-- mouse deltas clear on read: the caller passes them in
-- frameDt: this frame's dt (set by update); Q/E orbit at a fixed deg/s
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

-- edit camera: RMB rotates, wheel zooms, WASD/arrows and a middle drag pan the target on the ground
-- (the player does not move while editing); same clamps as play mode
local function getEditCameraFuncs(dt) return {
    pan = panCamera,
    zoom = zoomCamera,
    move = function(dmx, dmy)
        local cam = world.cam
        local tx, tz = cam.tx or 0, cam.tz or 0
        local ix, iz = getMoveUnitIsoXZ()
        if ix ~= 0 or iz ~= 0 then
            local sp = 7 * dt
            tx, tz = tx + ix * sp, tz + iz * sp
        end
        if INPUT:MousePressing("middle") and (dmx ~= 0 or dmy ~= 0) then
            -- world units per pixel at the target's depth; vertical stretched over the ground by pitch
            -- dragging right slides the ground right (target left)
            local fy = rad(cam.yaw)
            local fwdX, fwdZ = sin(fy), cos(fy)
            local rgtX, rgtZ = cos(fy), -sin(fy)
            local k = (cam.dist or 20) * 2 * math.tan(rad((cam.fov or FOV) / 2)) / SCREEN_H
            local ky = k / math.max(0.25, sin(rad(math.abs(cam.pitch or -40))))
            tx = tx - rgtX * dmx * k + fwdX * dmy * ky
            tz = tz - rgtZ * dmx * k + fwdZ * dmy * ky
        end
        if tx ~= (cam.tx or 0) or tz ~= (cam.tz or 0) then
            cam:setTarget(tx, cam.ty or (FY + 0.4), tz)
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
    tickBombs(dt)                  -- bomb storm runs in every mode
    if talkBack ~= nil and mode == "play" then
        -- back from Gris's song: her call goes on
        local cleared = talkBack
        talkBack = nil
        mode = "dialogue"; phoneFlow = "talk"
        talkResume(cleared)
    end

    -- online: drain events + advance remote avatars every frame (even under a menu)
    if net.online or net.connecting then
        MO.drain()
        MO.lerpRemotes(dt)
        -- visitor looks, registered once per look (here, never in draw)
        for _, lk in pairs(net.lookByPeer) do lk.set = AV.prepare(lk.student, lk.palette, true) end
        if net.roomGone then backToOwnRoom(PHONE:tr("room_host_closed")) end
    end

    -- jukebox upkeep runs in every mode (audio outlives the menu); jbRes feeds the jukebox branch below
    -- the PC-screen BGM fade too: its fade-out ends after the screen closes
    local jbRes = JB.update(dt, ts)
    PCS.tickBgmFade(dt)
    TV.tick(dt)                    -- these tick in every mode
    MX.tick(dt)
    FT.tick(dt)
    LT.tick(dt)

    -- ==== modal overlays ====
    if mode == "dialogue" then
        if talkPlay ~= nil then
            -- Gris's song: the room leaves for it, and the call goes on once the room is back
            local node = talkPlay
            talkPlay = nil
            if Talk.mount(node) then
                talkAway = true
                MO.leave(); GLOBALCAMERA:Reset(); saveRoom(); rememberPlace()
                return Exit("play", nil)
            end
            talkResume(false)
        end
        purse:update(dt)
        if ringT > 0 then
            ringT = ringT - dt
            if ringDue and ringT <= 1.6 then ringDue = false ; playPhoneSound("phone_ring") end
            if ringT <= 0 and ringScript then
                local s = ringScript
                ringScript = nil
                if type(s) == "function" then s() else dlg:start(s) end
            end
        elseif dlg:update(dt) == "done" then onDialogueDone(dlg.result) end
        if talkCafe then tickCafe(dt) end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "reward" then
        local modal = ROACTIVITY:GetROActivity("modal")
        if modal ~= nil and modal.IsActive then modal:Update()
        elseif phoneFlow == "talk" then mode = "dialogue"; talkResume()
        else mode = "play" end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "pod" then
        if Pod.update(dt) == "go" then
            MO.leave(); GLOBALCAMERA:Reset(); saveRoom(); rememberPlace()
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
    elseif mode == "tv" then
        if TV.update(dt, ts) == "closed" or not TV.isOpen() then mode = "play" end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "matrix" then
        local res, stage = MX.update(dt, ts)
        if res == "go" and not MX.stageExists(stage) then
            SHARED:GetSharedSound("Error"):Play()
            mode = "play"
        elseif res == "go" then
            -- a debug scene was chosen: leave the room for it the way the door does
            SHARED:GetSharedSound("Decide"):Play()
            mode = "play"
            MO.leave(); GLOBALCAMERA:Reset(); saveRoom(); rememberPlace()
            return Exit("stage", stage)
        end
        if res == "closed" or not MX.isOpen() then mode = "play" end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "mirror" then
        if MR.update(dt, ts) == "closed" or not MR.isOpen() then mode = "play" end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "workbench" then
        if WB.update(dt, ts) == "closed" or not WB.isOpen() then mode = "play" end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "recycle" then
        if RB.update(dt, ts) == "closed" or not RB.isOpen() then mode = "play" end
        settlePlayer(dt); world:update(dt, px, py, pz); return nil
    elseif mode == "edit" then
        if edit:update(ts, getEditCameraFuncs(dt)) == "exit" then
            mode = "play"; pmoving = false
            if editSavedCam then       -- restore the camera from before editing
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
        MO.leave(); GLOBALCAMERA:Reset(); saveRoom(); return leaveRoom()
    end
    -- interactables and Tab resolve after movement, with current px/pz

    -- camera orbit and zoom, wall-clamped
    local dmx, dmy = INPUT:GetMouseDelta()
    panCamera(dmx, dmy)
    local _, scrollY = INPUT:GetScrollDelta()
    zoomCamera(scrollY)

    -- physics character movement, camera-relative (wish dir from cam yaw)
    local ix, iz = getMoveUnitIsoXZ()
    player:move(dt, ix, iz, moveSpeed, false)
    px, py, pz = player:pos()

    -- camera follows the player (smoothed); target clamped to the interior (boom is off)
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

    -- interactables in range: Tab cycles focus with 2+, else enters edit (offline only)
    -- focus stays stable across frames by key
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
            -- save the camera for exit, then frame the room for editing
            editSavedCam = { yaw = world.cam.yaw, pitch = world.cam.pitch, dist = world.cam.dist,
                             tx = world.cam.tx, ty = world.cam.ty, tz = world.cam.tz }
            local span = math.max(room.iw, room.ih) + 3
            local fit = span / (2 * math.tan(rad(FOV / 2))) * 1.05
            fit = math.min(world.cam.maxDist or fit, math.max(world.cam.minDist or fit, fit))
            world.cam:setRig{ yaw = 45, pitch = -40, fov = FOV, dist = fit }
            world.cam:setTarget(room:gridW() * 0.5, FY + 0.4, room:gridH() * 0.5)
            edit:enter(pc, pr); mode = "edit"      -- Edit:enter plays its own open sound
            return nil
        end
    end

    -- focused interactable: glow it, act on Enter
    local focused = nil
    if focusKey then for _, e in ipairs(inter) do if e.key == focusKey then focused = e; break end end end
    prompt = focused and focused.kind or nil
    interactGlow(focused)
    if focused and (NavInput.p[playerIndex + 1].decide() or kp("Space")) then
        if focused.kind == "exit" then
            SHARED:GetSharedSound("Cancel"):Play()
            if MO.isGuest() then backToOwnRoom(PHONE:tr("room_left")); return nil end
            MO.leave(); GLOBALCAMERA:Reset(); saveRoom()
            return leaveRoom()
        elseif focused.kind == "computer" then
            JB.setDuck(true)               -- jukebox fades down first; the PC BGM fades in after it
            SHARED:GetSharedSound("Decide"):Play()
            pcScreen:openScreen(); mode = "pc"
        elseif focused.kind == "phone" then
            SHARED:GetSharedSound("Decide"):Play()
            openPhone()
        elseif focused.kind == "lamp" then
            SHARED:GetSharedSound("Move"):Play()
            focused.it.lit = (focused.it.lit == false) and true or false
            -- only the lamp and the lights change
            local lamp = world._propInst and world._propInst[focused.it] or nil
            if lamp ~= nil then
                Room.lampLook(world, focused.it, lamp)
                applyLights()
            else
                rebuild()
            end
            saveRoom(); MO.onRoomChanged()
        elseif focused.kind == "pod" then
            SHARED:GetSharedSound("Decide"):Play()
            if Pod.stageExists() then
                -- pod sequence (update handles "go")
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
        elseif focused.kind == "tv" then
            SHARED:GetSharedSound("Decide"):Play()
            TV.openFor(focused.it, Room.displayName("tv"))
            mode = "tv"
        elseif focused.kind == "matrix" then
            SHARED:GetSharedSound("Decide"):Play()
            MX.open()
            mode = "matrix"
        elseif focused.kind == "mirror" then
            SHARED:GetSharedSound("Decide"):Play()
            MR.open()
            mode = "mirror"
        elseif focused.kind == "workbench" then
            SHARED:GetSharedSound("Decide"):Play()
            WB.open()
            mode = "workbench"
        elseif focused.kind == "recycle" then
            SHARED:GetSharedSound("Decide"):Play()
            RB.open()
            mode = "recycle"
        elseif focused.kind == "fishtank" then
            SHARED:GetSharedSound("Move"):Play()
            FT.toggle(focused.it)
            applyLights()                  -- the tank's light and its water's glow
            saveRoom(); MO.onRoomChanged()
        elseif focused.kind == "lantern" then
            SHARED:GetSharedSound("Move"):Play()
            LT.toggle(focused.it)
            applyLights()                  -- light; the fire draws while lit
            saveRoom(); MO.onRoomChanged()
        elseif focused.kind == "look" then
            -- keepsake: says its line (dialogs.json, <item id>.look)
            local line = dlgLoc(focused.it.id, "look", nil)
            if line ~= nil then
                SHARED:GetSharedSound("Decide"):Play()
                mode = "dialogue"; phoneFlow = nil
                dlg:start({ { name = "", text = line } })
            end
        end
    end
    if msgT > 0 then msgT = msgT - dt end

    world:update(dt, px, py, pz)
    return nil
end

-- ==== draw ====
-- this frame's actor sprites (six numbers each: x, y, z, w, h, sprite) and the ones the layer holds
-- a map load empties the layer
local actorNow, actorShown, actorNowN, actorShownN, actorMap = {}, {}, 0, 0, nil
local function actorsBegin() actorNowN = 0 end
local function actorSprite(x, y, z, w, h, spr)
    local t, n = actorNow, actorNowN
    t[n + 1], t[n + 2], t[n + 3], t[n + 4], t[n + 5], t[n + 6] = x, y or 0, z, w, h, spr
    actorNowN = n + 6
end
local function actorsEnd()
    local t, n = actorNow, actorNowN
    local same = actorMap == map and n == actorShownN
    if same then for i = 1, n do if t[i] ~= actorShown[i] then same = false; break end end end
    if same then return end
    world.actors:actorsBegin()
    for i = 1, n, 6 do world.actors:actorSprite(t[i], t[i + 1], t[i + 2], t[i + 3], t[i + 4], t[i + 5]) end
    actorNow, actorShown, actorShownN, actorMap = actorShown, t, n, map
end

function draw()
    if world == nil or map == nil or hud == nil then return end
    local hW = 1.7
    local wW = hW * (AV.W / AV.H)
    local function groundAt(x, z) return map:heightAt(x, z) end

    -- actor layer (player and visitors), rewritten only when its sprites change
    -- no blob shadows: the sun shadow map grounds the sprites
    actorsBegin()
    local spr = AV.sprite(look.set, pdir, pmoving, pframeT)
    if mode == "pod" then
        -- pod: Pod.playerVisual moves and scales the sprite, nil once inside
        local vx, vz, vs = Pod.playerVisual()
        if spr and vx then actorSprite(vx, py, vz, wW * vs, hW * vs, spr) end
    elseif spr then
        actorSprite(px, py, pz, wW, hW, spr)
    end
    if net.online then
        for id, p in pairs(net.posByPeer) do
            local lk = net.lookByPeer[id]
            local rspr = AV.sprite(lk and lk.set or nil, p.dir, p.moving, p.frameT)
            local ry = groundAt(p.x, p.z)
            if rspr then actorSprite(p.x, ry, p.z, wW, hW, rspr) end
        end
    end
    actorsEnd()

    FT.draw()
    LT.draw()
    world:render()
    world:blit()

    -- HUD: glyph text through the UI library (dynamic strings never leak textures)
    -- room owner's nameplate at the top left
    if NAMEPLATE and NAMEPLATE.DrawPlayerNameplate then
        pcall(function() NAMEPLATE:DrawPlayerNameplate(40, 18, 255, playerIndex) end)
    end
    hud:drawTextEx(26, HUD:tr("title"), SCREEN_W / 2, 22, { 255, 255, 255 }, { 0, 0, 0, 255 }, 1, 1, 0, "top")

    -- online: status line and a name tag above each visitor
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
    if mode == "dialogue" then
        if ringT > 0 then
            hud:drawTextEx(26, PHONE:tr("calling"), SCREEN_W / 2, SCREEN_H - 200, { 255, 235, 160 }, { 0, 0, 0, 255 }, 1, 1, 0, "top")
        end
        dlg:draw()
        purse:draw()
    elseif mode == "reward" then
        local modal = ROACTIVITY:GetROActivity("modal")
        if modal ~= nil and modal.IsActive then modal:Draw() end
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
    elseif mode == "tv" then
        hud:rect(0, 0, SCREEN_W, SCREEN_H, 8, 10, 18, 130)
        TV.draw()
    elseif mode == "matrix" then
        hud:rect(0, 0, SCREEN_W, SCREEN_H, 8, 10, 18, 130)
        MX.draw()
    elseif mode == "mirror" then
        hud:rect(0, 0, SCREEN_W, SCREEN_H, 8, 10, 18, 130)
        MR.draw()
    elseif mode == "workbench" then
        hud:rect(0, 0, SCREEN_W, SCREEN_H, 8, 10, 18, 130)
        WB.draw()
    elseif mode == "recycle" then
        hud:rect(0, 0, SCREEN_W, SCREEN_H, 8, 10, 18, 130)
        RB.draw()
    else
        -- instructions at the top right, one per line: action prompt first, controls last
        local instr = {}
        if prompt == "computer" then instr[#instr + 1] = { t = HUD:tr("prompt_computer"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "phone" then instr[#instr + 1] = { t = HUD:tr("prompt_phone"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "lamp" then instr[#instr + 1] = { t = HUD:tr("prompt_lamp"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "jukebox" then instr[#instr + 1] = { t = HUD:tr("prompt_jukebox"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "tv" then instr[#instr + 1] = { t = HUD:tr("prompt_tv"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "pod" then instr[#instr + 1] = { t = HUD:tr("prompt_pod"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "matrix" then instr[#instr + 1] = { t = HUD:tr("prompt_matrix"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "mirror" then instr[#instr + 1] = { t = HUD:tr("prompt_mirror"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "look" then instr[#instr + 1] = { t = HUD:tr("prompt_look"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "fishtank" then instr[#instr + 1] = { t = HUD:tr("prompt_fishtank"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "lantern" then instr[#instr + 1] = { t = HUD:tr("prompt_lantern"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "workbench" then instr[#instr + 1] = { t = HUD:tr("prompt_workbench"), c = { 150, 230, 255 }, s = 24 }
        elseif prompt == "recycle" then instr[#instr + 1] = { t = HUD:tr("prompt_recycle"), c = { 150, 230, 255 }, s = 24 }
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
