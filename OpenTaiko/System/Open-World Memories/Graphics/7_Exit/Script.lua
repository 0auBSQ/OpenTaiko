---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Exit/shutdown background: rotating circles + logo + a localized speech bubble + a random character.

local timer = 0
local speech = "Speech/en.png"
local char = "Character/1.png"
local id = 1

local pos = {
    { x = 1058, y = 131 },
    { x = 925, y = 25 },
    { x = 1092, y = 65 },
    { x = 951, y = 288 },
    { x = 1142, y = 17 },
    { x = 1108, y = 35 },
    { x = 915, y = 189 },
    { x = 1168, y = 21 }
}

local total_chars = 8

local center_pos = { 540, 540 }

local tx = {}

function clearIn(player)
end

function clearOut(player)
end

local function selectSpeech(state)
    -- Default to en if an unsupported language is being used
    speech = "Speech/en.png"
    if state.lang == "ja" then
        speech = "Speech/ja.png"
    elseif state.lang == "fr" then
        speech = "Speech/fr.png"
    elseif state.lang == "es" then
        speech = "Speech/es.png"
    elseif state.lang == "ru" then
        speech = "Speech/ru.png"
    elseif state.lang == "zh" then
        speech = "Speech/zh.png"
    end
end

function onStart()
    tx["Background.png"] = TEXTURE:CreateTextureSync("Background.png")
    tx["Logo.png"] = TEXTURE:CreateTextureSync("Logo.png")
    tx["Circle1.png"] = TEXTURE:CreateTextureSync("Circle1.png")
    tx["Circle2.png"] = TEXTURE:CreateTextureSync("Circle2.png")
    tx["Circle3.png"] = TEXTURE:CreateTextureSync("Circle3.png")
    tx["Effect.png"] = TEXTURE:CreateTextureSync("Effect.png")

    -- The language isn't known until activate/update, so load every speech variant up front.
    for _, l in ipairs({ "en", "ja", "fr", "es", "ru", "zh" }) do
        tx["Speech/" .. l .. ".png"] = TEXTURE:CreateTextureSync("Speech/" .. l .. ".png")
    end

    -- Random character
    math.randomseed(os.time())
    id = math.random(total_chars)
    char = "Character/" .. tostring(id) .. ".png"
    tx[char] = TEXTURE:CreateTextureSync(char)

    timer = 0
end

function update(timestamp, state)
    selectSpeech(state)
    timer = timer + fps.deltaTime
end

function draw(state)
    tx["Circle1.png"]:SetRotation(timer * 30)
    tx["Circle2.png"]:SetRotation(timer * -20)
    tx["Circle3.png"]:SetRotation(timer * 10)

    tx["Background.png"]:Draw(0, 0)
    tx["Effect.png"]:Draw(0, 0)
    tx["Circle3.png"]:DrawAtAnchor(center_pos[1], center_pos[2], "center")
    tx["Circle2.png"]:DrawAtAnchor(center_pos[1], center_pos[2], "center")
    tx["Circle1.png"]:DrawAtAnchor(center_pos[1], center_pos[2], "center")
    tx["Logo.png"]:DrawAtAnchor(center_pos[1], center_pos[2], "center")
    tx[speech]:DrawAtAnchor(center_pos[1], center_pos[2], "center")
    tx[char]:Draw(pos[id].x, pos[id].y)
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end