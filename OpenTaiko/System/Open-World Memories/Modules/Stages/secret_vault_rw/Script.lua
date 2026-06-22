---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local

local Gate  = require("gate")
local Vault = require("vault")

local textures = {}
local sounds   = {}
local texts    = {}

-- "waiting_enum" → "gate" → "vault"
local phase          = "waiting_enum"
local active         = false
local songsEnumerated = false

---------------------------------------
-- Main Functions
---------------------------------------

function draw()
    if phase == "waiting_enum" then
        if texts.label then
            texts.label:GetText("Loading songs..."):DrawAtAnchor(960, 540, "center")
        end
        return
    end

    -- Vault interior always drawn first so it shows through the opening gate
    Vault.draw()
    if phase == "gate" then
        Gate.draw()
    end
end

function update()
    if phase == "waiting_enum" then
        -- handled by afterSongEnum
    elseif phase == "gate" then
        local result = Gate.update()
        if result == "vault" then
            phase = "vault"
            Vault.reset()
        elseif result == "back" then
            return Exit("title", nil)
        end
    elseif phase == "vault" then
        local result = Vault.update()
        if result == "back" then
            if sounds.BGM then sounds.BGM:Stop() end
            return Exit("title", nil)
        end
    end
end

function activate()
    active = true
    Vault.onActivate()  -- clear enterCtr so vault UI stays hidden during gate
    if songsEnumerated then
        phase = "gate"
        Gate.reset()
    else
        phase = "waiting_enum"
    end
end

function deactivate()
    active = false
    if sounds.BGM then sounds.BGM:Stop() end
end

function afterSongEnum()
    songsEnumerated = true
    if active and phase == "waiting_enum" then
        phase = "gate"
        Gate.reset()
    end
end

function onStart()
    texts.title = TEXT:Create(36)
    texts.label = TEXT:Create(22)

    sounds.BGM        = SOUND:CreateBGM("Sounds/BGM.ogg")
    sounds.Cancel     = SOUND:CreateSFX("Sounds/Cancel.ogg")
    sounds.Decide     = SOUND:CreateSFX("Sounds/Decide.ogg")
    sounds.Gate       = SOUND:CreateSFX("Sounds/Gate.ogg")
    sounds.GateShort  = SOUND:CreateSFX("Sounds/GateShort.ogg")
    sounds.NoKey      = SOUND:CreateSFX("Sounds/NoKey.ogg")
    sounds.Skip       = SOUND:CreateSFX("Sounds/Skip.ogg")
    sounds.SongDecide = SOUND:CreateSFX("Sounds/SongDecide.ogg")
    sounds.Unlock     = SOUND:CreateSFX("Sounds/Unlock.ogg")
    sounds.KeySnap    = SOUND:CreateSFX("Sounds/KeySnap.ogg")

    textures["Gate/Bg"]       = TEXTURE:CreateTexture("Textures/Gate/Bg.png")
    textures["Gate/Keyhole"]  = TEXTURE:CreateTexture("Textures/Gate/Keyhole.png")
    textures["Gate/Key"]      = TEXTURE:CreateTexture("Textures/Gate/Key.png")
    textures["Gate/Hover"]    = TEXTURE:CreateTexture("Textures/Gate/Hover.png")
    textures["Gate/Overlay"]  = TEXTURE:CreateTexture("Textures/Gate/Overlay.png")
    textures["Vault/Bg"]         = TEXTURE:CreateTexture("Textures/Vault/Bg.png")
    textures["Vault/Overlay"]    = TEXTURE:CreateTexture("Textures/Vault/Overlay.png")
    textures["Vault/BgTile"]     = TEXTURE:CreateTexture("Textures/Vault/BgTile.png")
    textures["Vault/Chest1"]     = TEXTURE:CreateTexture("Textures/Vault/Chest1.png")
    textures["Vault/Chest2"]     = TEXTURE:CreateTexture("Textures/Vault/Chest2.png")
    textures["Vault/Chest3"]     = TEXTURE:CreateTexture("Textures/Vault/Chest3.png")
    textures["Vault/ChestHover"] = TEXTURE:CreateTexture("Textures/Vault/ChestHover.png")
    textures["Vault/Return"]     = TEXTURE:CreateTexture("Textures/Vault/Return.png")
    textures["Vault/Key1"]       = TEXTURE:CreateTexture("Textures/Vault/Key1.png")
    textures["Vault/Key2"]       = TEXTURE:CreateTexture("Textures/Vault/Key2.png")
    textures["Vault/Key3"]       = TEXTURE:CreateTexture("Textures/Vault/Key3.png")

    Gate.init(textures, sounds, texts)
    Vault.init(textures, sounds, texts)
end

function onDestroy()
    for _, t in pairs(texts)     do if t then t:Dispose() end end
    for _, s in pairs(sounds)    do if s then s:Dispose() end end
    for _, tx in pairs(textures) do if tx then tx:Dispose() end end
end
