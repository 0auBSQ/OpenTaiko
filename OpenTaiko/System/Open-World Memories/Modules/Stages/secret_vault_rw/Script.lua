---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local

local Gate  = require("gate")
local Vault = require("vault")

local textures = {}
local sounds   = {}
local texts    = {}

-- "gate" while the gate phase is active; "vault" once fully open
local phase = "gate"

---------------------------------------
-- Main Functions
---------------------------------------

function draw()
    -- Vault interior always drawn first so it shows through the opening gate
    Vault.draw()
    if phase == "gate" then
        Gate.draw()
    end
end

function update()
    if phase == "gate" then
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
    phase = "gate"
    Gate.reset()
end

function deactivate()
    if sounds.BGM then sounds.BGM:Stop() end
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

    textures["Gate/Bg"]      = TEXTURE:CreateTexture("Textures/Gate/Bg.png")
    textures["Gate/Keyhole"] = TEXTURE:CreateTexture("Textures/Gate/Keyhole.png")
    textures["Gate/Key"]     = TEXTURE:CreateTexture("Textures/Gate/Key.png")
    textures["Gate/Hover"]   = TEXTURE:CreateTexture("Textures/Gate/Hover.png")
    textures["Gate/Overlay"] = TEXTURE:CreateTexture("Textures/Gate/Overlay.png")
    textures["Vault/Bg"]     = TEXTURE:CreateTexture("Textures/Vault/Bg.png")

    Gate.init(textures, sounds, texts)
    Vault.init(textures, sounds, texts)
end

function onDestroy()
    for _, t in pairs(texts)    do if t then t:Dispose() end end
    for _, s in pairs(sounds)   do if s then s:Dispose() end end
    for _, tx in pairs(textures) do if tx then tx:Dispose() end end
end
