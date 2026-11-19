---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- exit_screen ROActivity
-- goodbye screen; draws only (the caller decides timing and what follows)

-- positions: Config/layout.json; circle speeds: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local CENTER_X, CENTER_Y = Layout.center.x, Layout.center.y
-- character top-left, one per Textures/Character/<n>.png
local CHARACTERS = Layout.characters
local CIRCLE_SPEEDS = Animation.circle_speeds_deg_per_s
-- languages with a bubble (others: English)
local SPEECH = { en = true, ja = true, fr = true, es = true, ru = true, zh = true }

local background, effect, logo
local circles = {}
local character, speech
local id = 1
local timer = 0

local function free(tex)
    if tex ~= nil then pcall(function() tex:Dispose() end) end
end

local function freeAll()
    free(background) ; free(effect) ; free(logo) ; free(character) ; free(speech)
    for _, c in ipairs(circles) do free(c) end
    background, effect, logo, character, speech = nil, nil, nil, nil, nil
    circles = {}
end

function onStart() end

-- loads the pictures, picks a character and bubble; circles start from rest
function activate()
    freeAll()
    timer = 0
    -- sync load: short screen, whole from the first frame
    background = TEXTURE:CreateTextureSync("Textures/Background.png")
    effect = TEXTURE:CreateTextureSync("Textures/Effect.png")
    logo = TEXTURE:CreateTextureSync("Textures/Logo.png")
    for i = 1, 3 do circles[i] = TEXTURE:CreateTextureSync("Textures/Circle" .. i .. ".png") end
    id = math.random(#CHARACTERS)
    character = TEXTURE:CreateTextureSync("Textures/Character/" .. id .. ".png")
    local lang = "en"
    pcall(function() lang = LANG:GetLanguageId() end)
    if not SPEECH[lang] then lang = "en" end
    speech = TEXTURE:CreateTextureSync("Textures/Speech/" .. lang .. ".png")
end

-- frees the pictures (nothing held while hidden)
function deactivate()
    freeAll()
end

function update()
    timer = timer + fps.deltaTime
end

function draw()
    if background == nil then return end
    background:Draw(0, 0)
    effect:Draw(0, 0)
    for i = 1, 3 do circles[i]:SetRotation(timer * CIRCLE_SPEEDS[i]) end
    for i = 3, 1, -1 do circles[i]:DrawAtAnchor(CENTER_X, CENTER_Y, "center") end
    logo:DrawAtAnchor(CENTER_X, CENTER_Y, "center")
    if speech ~= nil then speech:DrawAtAnchor(CENTER_X, CENTER_Y, "center") end
    if character ~= nil then character:Draw(CHARACTERS[id].x, CHARACTERS[id].y) end
end

function afterSongEnum() end

function onDestroy()
    freeAll()
end
