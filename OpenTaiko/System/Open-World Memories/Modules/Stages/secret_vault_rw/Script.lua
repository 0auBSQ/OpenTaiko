---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local, redundant-parameter
local Tex = require("tex")
local Gate = require("gate")
local Cabin = require("cabin")
local Caspian = require("caspian")
local Cards = require("cards")
local Opening = require("opening")
local Vault = require("vault")
local NavInput = require("NavInput")
local MusicDuck = require("MusicDuck")

local EXIT_OPEN, EXIT_LOCKED = "vault_water_back", "vault_wood"
local COVER_KEY = "vault_cover"
local FADE = 0.3                 -- the dialogue box and the menu fade in and out this fast
local AMBIENCE_VOLUME = 35
local KICK_SNAPS = 3             -- snapped keys in one visit before Mr.Caspian kicks the player out
local KICKED = ".vault_kicked_out"
local KICK_SHUT_AT = 0.12        -- s after the kick, the door starts to shut
local KICK_PACE = 1.5            -- behind a kicked-out player the door shuts this much faster

-- Cabin/Water.png (the sea when the water shader cannot run) is loaded by cabin.lua only when it is needed
local TEXTURES = {
    "Gate/Door", "Gate/Keyhole", "Gate/Hover", "Gate/HintPanel",
    "Gate/KeyGreed", "Gate/KeyPride", "Gate/KeyWrath", "Gate/KeyEnvy",
    "Cabin/Room", "Cabin/Bubble", "Cabin/Dust",
    "Cabin/Fish1", "Cabin/Fish2", "Cabin/Fish3", "Cabin/Fish4", "Cabin/Fish5", "Cabin/Fish6",
    "Cabin/Seaweed1", "Cabin/Seaweed2", "Cabin/Seaweed3",
    "Cabin/Table", "Cabin/TreasureLeft", "Cabin/TreasureRight",
    "Cabin/Caspian/Neutral", "Cabin/Caspian/Happy", "Cabin/Caspian/Laugh", "Cabin/Caspian/Grumpy",
    "Chest/Chest1", "Chest/Chest2", "Chest/Chest3", "Chest/Chest1Open", "Chest/Chest2Open", "Chest/Chest3Open",
    "Chest/Key1", "Chest/Key2", "Chest/Key3", "Chest/KeyFront1", "Chest/KeyFront2", "Chest/KeyFront3", "Chest/Impact",
    "UI/Card", "UI/CardLeave", "UI/Chip", "UI/Key1", "UI/Key2", "UI/Key3",
}

local SOUNDS = {
    "Cancel", "Decide", "NoKey", "Skip", "Unlock",
    "KeySnap", "Shatter", "ChestDrop", "ChestOpen", "KeyIn", "KeyTurn", "Whoosh", "Punch", "ChestFar",
    "VoiceBlip1", "VoiceBlip2", "VoiceBlip3", "VoiceBlip4",
}

local textures = {}
local sounds   = {}
local fonts    = {}
local musicDuck   -- dims the BGM while the reward modal plays
local ctx = { tex = textures, snd = sounds, fonts = fonts }

local state           = "waiting_enum"
local active          = false
local songsEnumerated = false
local unlocked        = false   -- the vault was already open when the stage started
local now, revealAt   = 0, 0
local boxA, boxTarget   = 0, 0
local menuA, menuTarget = 0, 0
local chipA, chipTarget = 0, 0
local afterTalk = nil           -- what follows the talk in progress
local reward = nil              -- the roll being opened
local nextRemark = "prompt"     -- Mr.Caspian's remark when the menu comes back
local info = { {}, {}, {} }     -- per chest: { left, total, keys }
local keys = { 0, 0, 0 }
local closedAt = nil
local musicOn = false
local snaps = 0                 -- keys snapped this visit
local kickT, shutT = nil, nil   -- the kick, and when the door started to shut behind it

---------------------------------------
-- Helpers
---------------------------------------

local function play(name)
    local s = sounds[name]
    if s then s:Play() end
end

-- the gate's clock: the stage's, except once the door shuts behind a kicked-out player
local function gateNow()
    if shutT == nil then return now end
    return shutT + (now - shutT) * KICK_PACE
end

local function refreshInfo()
    for i = 1, 3 do
        local left, total = Vault.stock(i)
        keys[i] = Vault.keyCount(i)
        info[i] = { left = left, total = total, keys = keys[i] }
    end
end

local function startMusic()
    if musicOn then return end
    musicOn = true
    if sounds.BGM then sounds.BGM:SetLoop(true); sounds.BGM:Play() end
    if sounds.Underwater then
        sounds.Underwater:SetLoop(true)
        sounds.Underwater:SetVolumePercent(AMBIENCE_VOLUME)
        sounds.Underwater:Play()
    end
end

local function stopMusic()
    musicOn = false
    if sounds.BGM then sounds.BGM:Stop() end
    if sounds.Underwater then sounds.Underwater:Stop() end
end

local function talk(event, after)
    Caspian.talk(event)
    afterTalk = after
    boxTarget = 1
    state = "talk"
end

local function openMenu(remark)
    state = "menu"
    menuTarget, boxTarget, chipTarget = 1, 1, 1
    refreshInfo()
    Cards.pop(now)
    Caspian.remark(remark or "prompt")
end

local function enterCabin()
    local gifted = Vault.enter()
    refreshInfo()
    Cards.reset(now, 2)
    chipTarget = 1
    startMusic()
    local save = GetSaveFile(0)
    -- kicked out last time: a warning instead of a greeting, once
    local kicked = save ~= nil and save:GetGlobalTrigger(KICKED) == true
    if kicked then save:SetGlobalTrigger(KICKED, false) end
    if save and save:GetGlobalTrigger(".vault_caspian_met") ~= true then
        save:SetGlobalTrigger(".vault_caspian_met", true)
        Caspian.talkFirst(gifted)
        afterTalk = function() openMenu("prompt") end
        boxTarget = 1
        state = "talk"
    else
        talk(kicked and "warn" or "greet", function() openMenu("prompt") end)
    end
end

local function startGate()
    if unlocked then
        Gate.reset("unlocked", now)
        state = "gate_intro"
    else
        Gate.reset("locked", now, math.max(now, revealAt))
        state = "gate_locked"
    end
end

local function leaveCabin()
    menuTarget = 0
    talk("bye", function()
        boxTarget, chipTarget = 0, 0
        Gate.startClose(now)
        closedAt = nil
        state = "closing"
    end)
end

local function exitTo(trans)
    stopMusic()
    Opening.reset()
    return Exit("title", nil, trans)
end

-- Mr.Caspian's kick; the next visit's greeting is a warning
local function kickOut()
    boxTarget, chipTarget, menuTarget = 0, 0, 0
    local save = GetSaveFile(0)
    if save then save:SetGlobalTrigger(KICKED, true) end
    Caspian.hop()
    Opening.kick(now)
    kickT, shutT, closedAt = now, nil, nil
    state = "kicked"
end

---------------------------------------
-- Menu
---------------------------------------

local function menuInput()
    local nav = NavInput.p[1]
    if nav.right() then
        Cards.move(1); play("Skip")
    elseif nav.left() then
        Cards.move(-1); play("Skip")
    elseif nav.cancel() then
        if Cards.selected() ~= 1 then
            Cards.select(1); play("Cancel")
        else
            play("Cancel"); leaveCabin()
        end
    elseif nav.decide() then
        local c = Cards.chest()
        if c == nil then
            play("Decide"); leaveCabin()
        elseif Vault.keyCount(c) < 1 then
            play("NoKey")
            Cards.shake(now)
            Caspian.remark("nokey")
        else
            play("Decide")
            Cards.setConfirm(true, now)
            Caspian.remark((info[c].left or 0) <= 0 and "empty" or "confirm")
            state = "confirm"
        end
    end
end

local function confirmInput()
    local nav = NavInput.p[1]
    local c = Cards.chest()
    local decide, cancel, right, left = nav.decide(), nav.cancel(), nav.right(), nav.left()
    if decide and c ~= nil then
        reward = Vault.roll(c)   -- roll at once (anti save-scum)
        play("Decide")
        Cards.setConfirm(false, now)
        refreshInfo()
        Opening.start(c, reward, now)
        menuTarget, boxTarget = 0, 0
        nextRemark = "prompt"
        state = "opening"
    elseif cancel or left or right then
        Cards.setConfirm(false, now)
        if cancel then
            play("Cancel")
        else
            Cards.move(right and 1 or -1)
            play("Skip")
        end
        Caspian.remark("prompt")
        state = "menu"
    end
end

---------------------------------------
-- Main Functions
---------------------------------------

function draw()
    local dt = Tex.dt()
    now = now + dt
    Cards.tick(dt)
    boxA = Tex.approach(boxA, boxTarget, dt / FADE)
    menuA = Tex.approach(menuA, menuTarget, dt / FADE)
    chipA = Tex.approach(chipA, chipTarget, dt / FADE)

    local gnow = gateNow()
    if not Gate.covers(gnow) then
        Cabin.drawBack(now, dt)
        Opening.drawBack(now)
        Caspian.drawBody(now)
        Cabin.drawFront(now)
        Opening.draw(now, dt)
    end
    Gate.draw(gnow, dt, state == "gate_locked")
    if state == "gate_locked" or state == "gate_unlocking" then Gate.drawPanel(now, dt) end

    if state == "waiting_enum" and fonts.text then
        Tex.text(fonts.text, Tex.tr("VAULT_LOADING", "Loading songs..."), Tex.frame(960, 540),
            Tex.col(244, 234, 214), Tex.col(20, 16, 12), 1, 1200, "center")
    end

    Cards.draw(now, menuA, info)
    Cards.drawChips(now, chipA, keys, (state == "menu" or state == "confirm") and Cards.chest() or nil)
    Opening.drawBanner(now)
    Caspian.drawBox(boxA)
    Opening.drawKick(now)

    if state == "modal" then
        local modal = ROACTIVITY:GetROActivity("modal")
        if modal then modal:Draw() end
    end
end

function update()
    local dt = Tex.dt()
    Gate.update(gateNow())
    local caspian = Caspian.update(dt, now)

    if musicDuck then
        local modal = ROACTIVITY:GetROActivity("modal")
        musicDuck:update(dt, modal ~= nil and modal.IsActive)
    end

    if Opening.active() then
        local ev = Opening.update(now)
        if ev == "talk" then
            local snapped = not (reward and reward.type == "key")
            if snapped then snaps = snaps + 1 end
            talk(snapped and "snap" or "keygot", function()
                if snapped then
                    Opening.punch(now)      -- the box stays up for his "hey"
                else
                    boxTarget = 0
                    Opening.leave(now)
                end
                state = "opening"
            end)
            return
        elseif ev == "hey" then
            talk("hey", function()
                Opening.leave(now)
                if snaps >= KICK_SNAPS then
                    talk("kick", kickOut)
                else
                    boxTarget = 0
                    state = "opening"
                end
            end)
            return
        elseif ev == "modal" then
            if Vault.showReward(reward) then
                state = "modal"
                return
            end
            Opening.leave(now)
        elseif ev == "done" then
            reward = nil
            -- behind the kick the punched chest ends while he talks: the menu does not come back
            if state == "opening" then
                openMenu(nextRemark)
                return
            end
        end
    end

    if state == "waiting_enum" then
        -- While songs are loading or unavailable, only allow Cancel/Escape to exit.
        if NavInput.cancel() then
            play("Cancel")
            return exitTo(unlocked and EXIT_OPEN or EXIT_LOCKED)
        end
    elseif state == "gate_locked" then
        local r = Gate.input(now)
        if r == "back" then
            return exitTo(EXIT_LOCKED)
        elseif r == "unlock" then
            local save = GetSaveFile(0)
            if save then save:SetGlobalTrigger(".vault_opened", true) end
            Gate.startUnlock(now)
            state = "gate_unlocking"
        end
    elseif state == "gate_unlocking" then
        if Gate.moving(now) then startMusic() end
        if Gate.isOpen(now) then enterCabin() end
    elseif state == "gate_intro" then
        if not Gate.moving(now) and now >= revealAt then
            Gate.startOpen(now)
            startMusic()
        end
        if Gate.isOpen(now) then enterCabin() end
    elseif state == "talk" then
        if caspian == "done" then
            local nxt = afterTalk
            afterTalk = nil
            if nxt then nxt() end
        end
    elseif state == "menu" then
        menuInput()
    elseif state == "confirm" then
        confirmInput()
    elseif state == "modal" then
        local modal = ROACTIVITY:GetROActivity("modal")
        if modal then modal:Update() end
        if modal == nil or not modal.IsActive then
            nextRemark = "happy"
            Opening.leave(now)
            state = "opening"
        end
    elseif state == "closing" then
        if Gate.isClosed(now) then
            closedAt = closedAt or now
            if now - closedAt >= 0.35 then return exitTo(EXIT_OPEN) end
        end
    elseif state == "kicked" then
        if shutT == nil and now - kickT >= KICK_SHUT_AT then
            shutT = now
            Gate.startClose(now)
        end
        if shutT ~= nil and Gate.isClosed(gateNow()) then
            closedAt = closedAt or now
            if now - closedAt >= 0.35 then return exitTo(EXIT_OPEN) end
        end
    end
end

-- the dialogue's name tag holds NAME_INK px of ink at size 30: a longer name gets a smaller font
local NAME_SIZE, NAME_INK = 30, 240
local nameFor = nil
local function fitNameFont()
    local nm = Tex.tr("VAULT_CASPIAN", "Mr.Caspian")
    if nm == nameFor then return end
    nameFor = nm
    if fonts.name then fonts.name:Dispose() end
    fonts.name = TEXT:CreateGlyphCached(NAME_SIZE)
    local ink = fonts.name:Measure(nm)
    if ink > NAME_INK then
        fonts.name:Dispose()
        fonts.name = TEXT:CreateGlyphCached(math.max(18, math.floor(NAME_SIZE * NAME_INK / ink)))
    end
    Caspian.setNameFont(fonts.name)
end

local function loadTextures()
    for _, name in ipairs(TEXTURES) do
        local path = "Textures/" .. name .. ".png"
        if TEXTURE:Exists(path) then textures[name] = TEXTURE:CreateTexture(path) end
    end
end

local function freeTextures()
    for name, tex in pairs(textures) do
        tex:Dispose()
        textures[name] = nil
    end
end

function activate()
    active = true
    now = 0
    -- the way in hides the stage this long into its fade-in; the door waits for it
    revealAt = tonumber(SHARED:GetSharedString(COVER_KEY)) or 0
    SHARED:SetSharedString(COVER_KEY, "")
    local save = GetSaveFile(0)
    unlocked = save ~= nil and save:GetGlobalTrigger(".vault_opened") == true
    Tex.resetStrings()
    fitNameFont()
    loadTextures()
    if musicDuck then musicDuck:reset() end
    boxA, boxTarget, menuA, menuTarget, chipA, chipTarget = 0, 0, 0, 0, 0, 0
    afterTalk, reward, nextRemark, closedAt, musicOn = nil, nil, "prompt", nil, false
    snaps, kickT, shutT = 0, nil, nil
    Opening.reset()
    if unlocked then Cabin.open() end         -- behind the way in; a first unlock makes it when the door opens
    Cabin.reset()
    Caspian.reset()
    Cards.reset(0, 2)
    Gate.reset(unlocked and "unlocked" or "locked", now, revealAt)
    if songsEnumerated then
        startGate()
    else
        state = "waiting_enum"
    end
end

function deactivate()
    active = false
    stopMusic()
    Opening.reset()
    Cabin.close()
    Caspian.reset()
    Caspian.dispose()
    freeTextures()
end

function afterSongEnum()
    songsEnumerated = true
    if active and state == "waiting_enum" then
        startGate()
    end
end

function onStart()
    fonts.big   = TEXT:CreateGlyphCached(46)
    fonts.title = TEXT:CreateGlyphCached(34)
    fonts.talk  = TEXT:CreateGlyphCached(30)
    fonts.text  = TEXT:CreateGlyphCached(28)
    fonts.small = TEXT:CreateGlyphCached(23)

    if STORAGE:FileExists("Sounds/BGM.ogg") then sounds.BGM = SOUND:CreateBGM("Sounds/BGM.ogg") end
    if STORAGE:FileExists("Sounds/Underwater.ogg") then sounds.Underwater = SOUND:CreateSFX("Sounds/Underwater.ogg") end
    for _, name in ipairs(SOUNDS) do
        local path = "Sounds/" .. name .. ".ogg"
        if STORAGE:FileExists(path) then sounds[name] = SOUND:CreateSFX(path) end
    end
    musicDuck = MusicDuck.new{ sound = sounds.BGM }

    Gate.init(ctx)
    Cabin.init(ctx)
    Caspian.init(ctx)
    fitNameFont()
    Cards.init(ctx)
    Opening.init(ctx)
end

function onDestroy()
    Cabin.close()
    Caspian.dispose()
    freeTextures()
    for _, f in pairs(fonts)  do if f then f:Dispose() end end
    for _, s in pairs(sounds) do if s then s:Dispose() end end
end
