---@diagnostic disable: undefined-global, undefined-field
-- _test_gradient/Script.lua
-- Shows "05v2 - Aoi" with different gradient map palettes.
-- P toggles between preview (centered) and render (top-left, scale 0.5).
-- Return / Escape exits.

local CHARACTER_NAME = "05v2 - Aoi"

local character  = nil
local missing    = false
local gradients  = {}
local textFont   = nil
local labelCache = {}
local useRender  = false

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
    {
        name  = "Silver",
        blend = 1.0,
        stops = { {0, 20, 20, 25}, {0.3, 80, 82, 90}, {0.6, 180, 185, 195}, {0.85, 230, 232, 240}, {1, 255, 255, 255} },
    },
}

-- 5 columns evenly spaced: 1920 / 6 = 320
local POSITIONS = { 320, 640, 960, 1280, 1600 }

-- ── Lifecycle ─────────────────────────────────────────────────────────────────

function onStart()
    textFont  = TEXT:Create(24)
    character = CHARACTER:CreateCharacter(CHARACTER_NAME)
    missing   = character == nil or not character.IsValid

    if not missing then
        for i = 1, #PALETTES do
            if PALETTES[i].stops ~= nil then
                gradients[i] = GRADIENT:Create(PALETTES[i].stops, PALETTES[i].blend or 1.0)
            else
                gradients[i] = nil
            end
        end
    end
end

function activate()
    if not missing and character ~= nil then
        character:LoadAnimation(CHARACTER.ANIM_PREVIEW)
        character:LoadAnimation(CHARACTER.ANIM_RENDER)
    end
end

function deactivate()
    if not missing and character ~= nil then
        character:DisposeAnimation(CHARACTER.ANIM_PREVIEW)
        character:DisposeAnimation(CHARACTER.ANIM_RENDER)
    end
end

function onDestroy()
    if character ~= nil then
        character:Dispose()
        character = nil
    end
    if textFont ~= nil then
        textFont:Dispose()
        textFont = nil
    end
end

-- ── Update ────────────────────────────────────────────────────────────────────

function update(ts)
    if INPUT:KeyboardPressed("return") or INPUT:KeyboardPressed("escape") or
       INPUT:Pressed("Decide") or INPUT:Pressed("Cancel") then
        Exit("stage", "_title")
        return
    end

    if missing or character == nil then return end

    character:Update(CHARACTER.ANIM_PREVIEW, true)
    character:Update(CHARACTER.ANIM_RENDER, true)

    if INPUT:KeyboardPressed("p") then
        useRender = not useRender
        labelCache["mode"] = nil
    end
end

-- ── Draw ──────────────────────────────────────────────────────────────────────

function draw()
    if missing or textFont == nil then
        if textFont ~= nil then
            if not labelCache["missing"] then
                labelCache["missing"] = textFont:GetText('This stage requires the character "' .. CHARACTER_NAME .. '"')
            end
            if labelCache["missing"] ~= nil then
                labelCache["missing"]:DrawAtAnchor(960, 540, "center")
            end
        end
        return
    end

    if character == nil then return end

    local anim = useRender and CHARACTER.ANIM_RENDER or CHARACTER.ANIM_PREVIEW

    for i = 1, #PALETTES do
        if gradients[i] then GRADIENT:SetActive(gradients[i]) end

        if useRender then
            character:DrawAtAnchor(POSITIONS[i] - 400, 0, anim, "topleft", 0.5, 0.5)
        else
            character:DrawAtAnchor(POSITIONS[i], 540, anim, "center", 1, 1)
        end

        if gradients[i] then GRADIENT:ClearActive() end

        if textFont ~= nil and not labelCache[i] then
            labelCache[i] = textFont:GetText(PALETTES[i].name)
        end
        if labelCache[i] ~= nil then labelCache[i]:DrawAtAnchor(POSITIONS[i], 715, "top") end
    end

    if textFont ~= nil then
        if not labelCache["mode"] then
            labelCache["mode"] = textFont:GetText(useRender and "Render  (P to switch)" or "Preview  (P to switch)")
        end
        if labelCache["mode"] ~= nil then labelCache["mode"]:DrawAtAnchor(960, 30, "top") end

        if not labelCache["hint"] then
            labelCache["hint"] = textFont:GetText("Return / Escape  ->  back to title")
        end
        if labelCache["hint"] ~= nil then labelCache["hint"]:DrawAtAnchor(960, 1075, "bottom") end
    end
end
