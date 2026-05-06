---@diagnostic disable: undefined-global, undefined-field
-- _test_gradient/Script.lua
-- Shows P1 character 3 times with 3 different gradient map palettes.

local character = nil
local gradients = {}
local textFont  = nil
local labelCache = {}

-- Palette definitions: list of {position, r, g, b, a} stops (a defaults to 255).
-- A nil stops entry means no gradient (original character colours).
local PALETTES = {
    {
        name  = "Default",
        stops = nil,
    },
    {
        name  = "Crimson",
        blend = 0.5,
        stops = { {0, 0, 0, 0}, {0.25, 0, 0, 0}, {0.5, 247, 2, 2}, {0.75, 242, 140, 215}, {1, 255, 255, 255} },
    },
    {
        name  = "Forest",
        blend = 0.5,
        stops = { {0, 0, 0, 0}, {0.25, 0, 0, 0}, {0.5, 0, 255, 42}, {0.75, 158, 158, 158}, {1, 106, 76, 37} },
    },
    {
        name  = "Golden",
        blend = 1.0,
        stops = { {0, 0, 0, 0}, {0.25, 90, 60, 5}, {0.5, 190, 148, 20}, {0.75, 245, 208, 55}, {1, 255, 252, 200} },
    },
}

-- 1920 / 5 = 384, giving four symmetric columns at 384 / 768 / 1152 / 1536
local POSITIONS = { 384, 768, 1152, 1536 }

-- ── Lifecycle ─────────────────────────────────────────────────────────────────

function onStart()
    character = CHARACTER:GetPlayerCharacter(0)
    textFont  = TEXT:Create(24)
    for i = 1, #PALETTES do
        if PALETTES[i].stops ~= nil then
            gradients[i] = GRADIENT:Create(PALETTES[i].stops, PALETTES[i].blend or 1.0)
        else
            gradients[i] = nil
        end
    end
end

function activate()
    character:LoadAnimation(CHARACTER.ANIM_PREVIEW)
end

function deactivate()
    character:DisposeAnimation(CHARACTER.ANIM_PREVIEW)
end

-- ── Update ────────────────────────────────────────────────────────────────────

function update(ts)
    character:Update(CHARACTER.ANIM_PREVIEW, true)

    if INPUT:KeyboardPressed("return") or INPUT:KeyboardPressed("escape") then
        Exit("stage", "_title")
    end
end

-- ── Draw ──────────────────────────────────────────────────────────────────────

function draw()
    -- Draw the character three times, each with a different gradient palette
    for i = 1, #PALETTES do
        if gradients[i] then GRADIENT:SetActive(gradients[i]) end
        character:DrawAtAnchor(POSITIONS[i], 540, CHARACTER.ANIM_PREVIEW, "center", 1, 1)
        if gradients[i] then GRADIENT:ClearActive() end

        -- Palette name label below
        if not labelCache[i] then
            labelCache[i] = textFont:GetText(PALETTES[i].name)
        end
        labelCache[i]:DrawAtAnchor(POSITIONS[i], 715, "top")
    end

    -- Navigation hint
    if not labelCache["hint"] then
        labelCache["hint"] = textFont:GetText("Return / Escape  →  back to title")
    end
    labelCache["hint"]:DrawAtAnchor(960, 1075, "bottom")
end
