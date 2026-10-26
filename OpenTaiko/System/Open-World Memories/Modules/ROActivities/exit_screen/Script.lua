---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- exit_screen ROActivity
-- ------------------------------------------------------------------------------------------------------
-- The goodbye picture: three turning circles around the logo, a speech bubble in the player's language
-- and one of the characters, picked at random each time. It only draws; whoever shows it decides how
-- long it stays and what follows (the game closing, or the title screen after an Event Mode session).
--   activate()    loads the pictures, picks the character and the bubble; the circles start from rest
--   update()      every frame
--   draw()        every frame
--   deactivate()  frees the pictures: nothing is held while the screen is not shown

-- positions: Config/layout.json; the circles' turning speeds: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local CENTER_X, CENTER_Y = Layout.center.x, Layout.center.y
-- where each character stands (its picture's top left), one entry per Textures/Character/<n>.png
local CHARACTERS = Layout.characters
local CIRCLE_SPEEDS = Animation.circle_speeds_deg_per_s
-- the languages that have a bubble; the others get the English one
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

function activate()
    freeAll()
    timer = 0
    -- loaded at once: the screen is short, and shows whole from its first frame
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
