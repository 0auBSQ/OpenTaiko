---@diagnostic disable: undefined-global  -- TEXTURE/fps injected by CLuaScript at runtime
-- Dan Fail end animation: the two gates slam shut, foxes peek/wave, then a localized speech bubble fades in.
-- Ported from the old ScriptBG func: API to the ROActivity LuaTexture API. Group C (clear/end anime):
--   playEndAnime(player) is the per-play (re)start (called by the host with the index directly) — counters
--   AND the per-play speech randomization (seed/special_chance/random line) reset here. update/draw take the
--   `state` table; the old `player` arg is now `state.player`. Textures load once in onStart (sync, behind the
--   loading bar) — every speech variant is pre-loaded because the language (state.lang) isn't known in onStart,
--   so draw just selects which already-loaded variant to show.

--Speech tex rects: (0,33,607,220)(608,0,630,253)
--Speech positions (center pos): (329.5,812)(1566,935.5)
--Tsubaki tex rects (body+arms): (692,0,884,1017)(692,1018,253,334)(946,1018,174,345)(1122,1018,377,320)
--Tsubaki positions (body): (1461,63)(1565,63)(1730,63)
--Tsubaki positions (arms): (1574,717)(1670,715)(1569,646)
--Ume tex rects (body+arms): (0,0,691,819)(0,979,248,243)(77,820,105,158)(249,820,322,300)
--Ume positions (body): (-178,297)(-346,297)(-473,297)
--Ume positions (arms): (93,750)(77,750)(-40,717)
--Star position: (1430,427.5)(489,571)
--Star rect: ()

local speech_rect = {0,33,607,220,608,0,630,253}
local speech_pos = {329.5,812,1566,935.5}

-- x,y,w,h
local tsubaki_body_rect = {692,0,884,1017}
local tsubaki_arm1_rect = {692,1018,253,334}
local tsubaki_arm2_rect = {946,1018,174,345}
local tsubaki_arm3_rect = {1122,1018,377,320}
local ume_body_rect = {0,0,691,819}
local ume_arm1_rect = {0,979,248,243}
local ume_arm2_rect = {77,820,105,158}
local ume_arm3_rect = {249,820,322,300}
local star_red_rect = {1759,0,50,98}
local star_blue_rect = {1761,98,48,93}
local light_red_rect = {1577,192,232,240}
local light_blue_rect = {1577,433,232,228}

-- x,y,x,y,x,y
local tsubaki_body_pos = {1461,63,1565,63,1730,63}
local tsubaki_arm_pos = {1574,717,1670,715,1569,646}
local ume_body_pos = {-178,297,-346,297,-473,297}
local ume_arm_pos = {93,750,77,750,-40,717}
local star_pos = {1430,427.5,489,571}
local light_pos = {1436,430,496,573}

--Slam time: 1.142s

local animeCounter = 0

local bg_width = 1920
local bg_height = 1080

local dan_in_width = 960
local dan_in_height = 1080

local dan_in_move = 0
local dan_first_in_move = 0
local dan_second_in_move = 0
local dan_in_slam = 0

local seed = 0
local special_chance = 0
local speech_line = 0          -- per-play random line index (0-4), chosen in playEndAnime; lang resolved in draw

local tx = {}                  -- name -> LuaTexture

-- Resolve the per-play speech key from the language (state.lang) + the per-play randomization. Mirrors the old
-- init() selection exactly: special line wins, otherwise a per-language line (en is the fallback for unknown lang).
local function speechKey(lang)
    if special_chance == 1 then
        return "Speech/special/0.png"
    elseif lang == "ja" then
        return "Speech/ja/" .. tostring(speech_line) .. ".png"
    elseif lang == "ru" then
        return "Speech/ru/" .. tostring(speech_line) .. ".png"
    elseif lang == "zh" then
        return "Speech/zh/" .. tostring(speech_line) .. ".png"
    else
        return "Speech/en/" .. tostring(speech_line) .. ".png"
    end
end

function playEndAnime(player)
    animeCounter = 0

    dan_in_move = 0
    dan_first_in_move = 0
    dan_second_in_move = 0
    dan_in_slam = 0

    -- per-play speech randomization (was in init() on the old API; redone each play here)
    seed = os.time()
    math.randomseed(seed)
    special_chance = math.random(1, 100)
    speech_line = math.random(0, 4)
end

function clearIn(player)
end

function clearOut(player)
end

function onStart()
    tx["Dan_In.png"] = TEXTURE:CreateTextureSync("Dan_In.png")
    tx["Dan_In_Shadow.png"] = TEXTURE:CreateTextureSync("Dan_In_Shadow.png")
    tx["Slam.png"] = TEXTURE:CreateTextureSync("Slam.png")
    tx["Message.png"] = TEXTURE:CreateTextureSync("Message.png")
    tx["Foxes.png"] = TEXTURE:CreateTextureSync("Foxes.png")

    tx["Speech/Speech.png"] = TEXTURE:CreateTextureSync("Speech/Speech.png")

    -- onStart runs before state arrives, so the language is unknown here — pre-load every speech variant and let
    -- draw pick the one for state.lang + the per-play random line.
    tx["Speech/special/0.png"] = TEXTURE:CreateTextureSync("Speech/special/0.png")
    for i = 0, 4 do
        tx["Speech/en/" .. i .. ".png"] = TEXTURE:CreateTextureSync("Speech/en/" .. i .. ".png")
        tx["Speech/ja/" .. i .. ".png"] = TEXTURE:CreateTextureSync("Speech/ja/" .. i .. ".png")
        tx["Speech/ru/" .. i .. ".png"] = TEXTURE:CreateTextureSync("Speech/ru/" .. i .. ".png")
        tx["Speech/zh/" .. i .. ".png"] = TEXTURE:CreateTextureSync("Speech/zh/" .. i .. ".png")
    end
end

function update(timestamp, state)
    local player = state.player

    animeCounter = animeCounter + fps.deltaTime

    dan_first_in_move = math.min(960 * animeCounter, 140)
    dan_second_in_move = math.min(math.max(2880 * (animeCounter - 1.14), 0), 820)

    dan_in_move = dan_first_in_move + dan_second_in_move

    if dan_in_move == dan_in_width then
      dan_in_slam = dan_in_slam + fps.deltaTime
    end

end

function draw(state)
    local player = state.player

    local speech_text = speechKey(state.lang)

    -- Foxes (back)
    if animeCounter >= 0.2 and 0.275 >= animeCounter then
      tx["Foxes.png"]:DrawRect(tsubaki_body_pos[5], tsubaki_body_pos[6], tsubaki_body_rect[1], tsubaki_body_rect[2], tsubaki_body_rect[3], tsubaki_body_rect[4])
      tx["Foxes.png"]:DrawRect(ume_body_pos[5], ume_body_pos[6], ume_body_rect[1], ume_body_rect[2], ume_body_rect[3], ume_body_rect[4])
      -- Draw these arms behind the wall
      tx["Foxes.png"]:DrawRect(tsubaki_arm_pos[5], tsubaki_arm_pos[6], tsubaki_arm3_rect[1], tsubaki_arm3_rect[2], tsubaki_arm3_rect[3], tsubaki_arm3_rect[4])
      tx["Foxes.png"]:DrawRect(ume_arm_pos[5], ume_arm_pos[6], ume_arm3_rect[1], ume_arm3_rect[2], ume_arm3_rect[3], ume_arm3_rect[4])
    elseif animeCounter >= 0.275 and 0.35 >= animeCounter then
      tx["Foxes.png"]:DrawRect(tsubaki_body_pos[3], tsubaki_body_pos[4], tsubaki_body_rect[1], tsubaki_body_rect[2], tsubaki_body_rect[3], tsubaki_body_rect[4])
      tx["Foxes.png"]:DrawRect(ume_body_pos[3], ume_body_pos[4], ume_body_rect[1], ume_body_rect[2], ume_body_rect[3], ume_body_rect[4])
    elseif animeCounter >= 0.35 and 0.95 >= animeCounter then
      tx["Foxes.png"]:DrawRect(tsubaki_body_pos[1], tsubaki_body_pos[2], tsubaki_body_rect[1], tsubaki_body_rect[2], tsubaki_body_rect[3], tsubaki_body_rect[4])
      tx["Foxes.png"]:DrawRect(ume_body_pos[1], ume_body_pos[2], ume_body_rect[1], ume_body_rect[2], ume_body_rect[3], ume_body_rect[4])
    elseif animeCounter >= 0.95 and 1.025 >= animeCounter then
      tx["Foxes.png"]:DrawRect(tsubaki_body_pos[3], tsubaki_body_pos[4], tsubaki_body_rect[1], tsubaki_body_rect[2], tsubaki_body_rect[3], tsubaki_body_rect[4])
      tx["Foxes.png"]:DrawRect(ume_body_pos[3], ume_body_pos[4], ume_body_rect[1], ume_body_rect[2], ume_body_rect[3], ume_body_rect[4])
    elseif animeCounter >= 1.025 and 1.1 >= animeCounter then
      tx["Foxes.png"]:DrawRect(tsubaki_body_pos[5], tsubaki_body_pos[6], tsubaki_body_rect[1], tsubaki_body_rect[2], tsubaki_body_rect[3], tsubaki_body_rect[4])
      tx["Foxes.png"]:DrawRect(ume_body_pos[5], ume_body_pos[6], ume_body_rect[1], ume_body_rect[2], ume_body_rect[3], ume_body_rect[4])
      -- Draw these arms behind the wall
      tx["Foxes.png"]:DrawRect(tsubaki_arm_pos[5], tsubaki_arm_pos[6], tsubaki_arm3_rect[1], tsubaki_arm3_rect[2], tsubaki_arm3_rect[3], tsubaki_arm3_rect[4])
      tx["Foxes.png"]:DrawRect(ume_arm_pos[5], ume_arm_pos[6], ume_arm3_rect[1], ume_arm3_rect[2], ume_arm3_rect[3], ume_arm3_rect[4])
    end

    -- The Gates
    tx["Dan_In.png"]:DrawRect(dan_in_move - dan_in_width, 0, 0, 0, dan_in_width, dan_in_height)
    tx["Dan_In.png"]:DrawRect(bg_width - dan_in_move, 0, dan_in_width, 0, dan_in_width, dan_in_height)

    -- Foxes (front)
    if animeCounter >= 0.2 and 0.275 >= animeCounter then
      -- none
    elseif animeCounter >= 0.275 and 0.35 >= animeCounter then
      tx["Foxes.png"]:DrawRect(tsubaki_arm_pos[3], tsubaki_arm_pos[4], tsubaki_arm2_rect[1], tsubaki_arm2_rect[2], tsubaki_arm2_rect[3], tsubaki_arm2_rect[4])
      tx["Foxes.png"]:DrawRect(ume_arm_pos[3], ume_arm_pos[4], ume_arm2_rect[1], ume_arm2_rect[2], ume_arm2_rect[3], ume_arm2_rect[4])
    elseif animeCounter >= 0.35 and 0.95 >= animeCounter then
      tx["Foxes.png"]:DrawRect(tsubaki_arm_pos[1], tsubaki_arm_pos[2], tsubaki_arm1_rect[1], tsubaki_arm1_rect[2], tsubaki_arm1_rect[3], tsubaki_arm1_rect[4])
      tx["Foxes.png"]:DrawRect(ume_arm_pos[1], ume_arm_pos[2], ume_arm1_rect[1], ume_arm1_rect[2], ume_arm1_rect[3], ume_arm1_rect[4])
    elseif animeCounter >= 0.95 and 1.025 >= animeCounter then
      tx["Foxes.png"]:DrawRect(tsubaki_arm_pos[3], tsubaki_arm_pos[4], tsubaki_arm2_rect[1], tsubaki_arm2_rect[2], tsubaki_arm2_rect[3], tsubaki_arm2_rect[4])
      tx["Foxes.png"]:DrawRect(ume_arm_pos[3], ume_arm_pos[4], ume_arm2_rect[1], ume_arm2_rect[2], ume_arm2_rect[3], ume_arm2_rect[4])
    elseif animeCounter >= 1.025 and 1.1 >= animeCounter then
      -- none
    end

    -- Gates Slam
    if dan_in_slam > 0 then
      tx["Slam.png"]:SetOpacity((255 - math.min(dan_in_slam * 1020, 255)) / 255)
      tx["Slam.png"]:DrawRect(760 - (dan_in_slam * 400), 0, 0, 0, 200, dan_in_height)
      tx["Slam.png"]:DrawRect(960 + (dan_in_slam * 400), 0, 200, 0, 200, dan_in_height)

      tx["Foxes.png"]:SetOpacity(math.max(math.min((math.min(dan_in_slam - 0.7, 0.25) - math.max(0, dan_in_slam - 0.95)) * 1020, 255), 0) / 255)
      tx["Foxes.png"]:DrawRectAtAnchor(light_pos[1], light_pos[2], light_blue_rect[1], light_blue_rect[2], light_blue_rect[3], light_blue_rect[4], "center")
      tx["Foxes.png"]:DrawRectAtAnchor(light_pos[3], light_pos[4], light_red_rect[1], light_red_rect[2], light_red_rect[3], light_red_rect[4], "center")
      tx["Foxes.png"]:SetRotation(dan_in_slam * 720)
      tx["Foxes.png"]:DrawRectAtAnchor(star_pos[1], star_pos[2], star_blue_rect[1], star_blue_rect[2], star_blue_rect[3], star_blue_rect[4], "center")
      tx["Foxes.png"]:SetRotation(dan_in_slam * -720)
      tx["Foxes.png"]:DrawRectAtAnchor(star_pos[3], star_pos[4], star_red_rect[1], star_red_rect[2], star_red_rect[3], star_red_rect[4], "center")
      tx["Foxes.png"]:SetRotation(0)

      tx["Dan_In_Shadow.png"]:SetOpacity(math.max(math.min((dan_in_slam - 1.2) * 510, 255), 0) / 255)
      tx["Message.png"]:SetOpacity(math.max(math.min((dan_in_slam - 0.2) * 1020, 255), 0) / 255)
      tx["Dan_In_Shadow.png"]:Draw(0, 0)
      tx["Message.png"]:Draw(0, 0)
    end

    -- Speech Bubble
    if dan_in_slam > 1.2 then
      local speech_fadein = math.max(math.min((dan_in_slam - 1.7) * 1020, 255), 0)

      tx["Speech/Speech.png"]:SetOpacity(speech_fadein / 255)
      tx[speech_text]:SetOpacity(speech_fadein / 255)
      tx["Speech/Speech.png"]:DrawRectAtAnchor(speech_pos[1], speech_pos[2], speech_rect[1], speech_rect[2], speech_rect[3], speech_rect[4], "center")
      tx["Speech/Speech.png"]:DrawRectAtAnchor(speech_pos[3], speech_pos[4], speech_rect[5], speech_rect[6], speech_rect[7], speech_rect[8], "center")
      tx[speech_text]:DrawRectAtAnchor(speech_pos[1], speech_pos[2], speech_rect[1], speech_rect[2], speech_rect[3], speech_rect[4], "center")
      tx[speech_text]:DrawRectAtAnchor(speech_pos[3], speech_pos[4], speech_rect[5], speech_rect[6], speech_rect[7], speech_rect[8], "center")
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
