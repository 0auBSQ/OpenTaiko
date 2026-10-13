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

local CENTER_X, CENTER_Y = 540, 540
-- where each character stands (its picture's top left)
local CHARACTERS = {
    { x = 1058, y = 131 },
    { x = 925, y = 25 },
    { x = 1092, y = 65 },
    { x = 951, y = 288 },
    { x = 1142, y = 17 },
    { x = 1108, y = 35 },
    { x = 915, y = 189 },
    { x = 1168, y = 21 },
}
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
    circles[1]:SetRotation(timer * 30)
    circles[2]:SetRotation(timer * -20)
    circles[3]:SetRotation(timer * 10)
    for i = 3, 1, -1 do circles[i]:DrawAtAnchor(CENTER_X, CENTER_Y, "center") end
    logo:DrawAtAnchor(CENTER_X, CENTER_Y, "center")
    if speech ~= nil then speech:DrawAtAnchor(CENTER_X, CENTER_Y, "center") end
    if character ~= nil then character:Draw(CHARACTERS[id].x, CHARACTERS[id].y) end
end

function afterSongEnum() end

function onDestroy()
    freeAll()
end
