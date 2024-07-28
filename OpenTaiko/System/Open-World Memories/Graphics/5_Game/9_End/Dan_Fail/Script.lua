--func:DrawText(x, y, text);
--func:DrawNum(x, y, num);
--func:AddGraph("filename");
--func:DrawGraph(x, y, filename);
--func:SetOpacity(opacity, "filename");
--func:SetScale(xscale, yscale, "filename");
--func:SetColor(r, g, b, "filename");

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

local speech_text = "Speech/en/0.png"

function playEndAnime(player)
end

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Dan_In.png")
    func:AddGraph("Dan_In_Shadow.png")
    func:AddGraph("Slam.png")
    func:AddGraph("Message.png")
    func:AddGraph("Foxes.png")

    seed = os.time()
    math.randomseed(seed)
    special_chance = math.random(1, 100)

    if special_chance == 1 then
      speech_text = "Speech/special/0.png"
    elseif lang == "ja" then
      speech_text = "Speech/ja/"..tostring(math.random(0, 4))..".png"
    elseif lang == "ru" then
      speech_text = "Speech/ru/"..tostring(math.random(0, 4))..".png"
    elseif lang == "zh" then
      speech_text = "Speech/zh/"..tostring(math.random(0, 4))..".png"
    else
      speech_text = "Speech/en/"..tostring(math.random(0, 4))..".png"
    end

    func:AddGraph(speech_text)
    func:AddGraph("Speech/Speech.png")
end

function update(player)

    animeCounter = animeCounter + deltaTime

    dan_first_in_move = math.min(960 * animeCounter, 140)
    dan_second_in_move = math.min(math.max(2880 * (animeCounter - 1.14), 0), 820)

    dan_in_move = dan_first_in_move + dan_second_in_move

    if dan_in_move == dan_in_width then
      dan_in_slam = dan_in_slam + deltaTime
    end

end

function draw(player)

    -- Foxes (back)
    if animeCounter >= 0.2 and 0.275 >= animeCounter then
      func:DrawRectGraph(tsubaki_body_pos[5], tsubaki_body_pos[6], tsubaki_body_rect[1], tsubaki_body_rect[2], tsubaki_body_rect[3], tsubaki_body_rect[4], "Foxes.png")
      func:DrawRectGraph(ume_body_pos[5], ume_body_pos[6], ume_body_rect[1], ume_body_rect[2], ume_body_rect[3], ume_body_rect[4], "Foxes.png")
      -- Draw these arms behind the wall
      func:DrawRectGraph(tsubaki_arm_pos[5], tsubaki_arm_pos[6], tsubaki_arm3_rect[1], tsubaki_arm3_rect[2], tsubaki_arm3_rect[3], tsubaki_arm3_rect[4], "Foxes.png")
      func:DrawRectGraph(ume_arm_pos[5], ume_arm_pos[6], ume_arm3_rect[1], ume_arm3_rect[2], ume_arm3_rect[3], ume_arm3_rect[4], "Foxes.png")
    elseif animeCounter >= 0.275 and 0.35 >= animeCounter then
      func:DrawRectGraph(tsubaki_body_pos[3], tsubaki_body_pos[4], tsubaki_body_rect[1], tsubaki_body_rect[2], tsubaki_body_rect[3], tsubaki_body_rect[4], "Foxes.png")
      func:DrawRectGraph(ume_body_pos[3], ume_body_pos[4], ume_body_rect[1], ume_body_rect[2], ume_body_rect[3], ume_body_rect[4], "Foxes.png")
    elseif animeCounter >= 0.35 and 0.95 >= animeCounter then
      func:DrawRectGraph(tsubaki_body_pos[1], tsubaki_body_pos[2], tsubaki_body_rect[1], tsubaki_body_rect[2], tsubaki_body_rect[3], tsubaki_body_rect[4], "Foxes.png")
      func:DrawRectGraph(ume_body_pos[1], ume_body_pos[2], ume_body_rect[1], ume_body_rect[2], ume_body_rect[3], ume_body_rect[4], "Foxes.png")
    elseif animeCounter >= 0.95 and 1.025 >= animeCounter then
      func:DrawRectGraph(tsubaki_body_pos[3], tsubaki_body_pos[4], tsubaki_body_rect[1], tsubaki_body_rect[2], tsubaki_body_rect[3], tsubaki_body_rect[4], "Foxes.png")
      func:DrawRectGraph(ume_body_pos[3], ume_body_pos[4], ume_body_rect[1], ume_body_rect[2], ume_body_rect[3], ume_body_rect[4], "Foxes.png")
    elseif animeCounter >= 1.025 and 1.1 >= animeCounter then
      func:DrawRectGraph(tsubaki_body_pos[5], tsubaki_body_pos[6], tsubaki_body_rect[1], tsubaki_body_rect[2], tsubaki_body_rect[3], tsubaki_body_rect[4], "Foxes.png")
      func:DrawRectGraph(ume_body_pos[5], ume_body_pos[6], ume_body_rect[1], ume_body_rect[2], ume_body_rect[3], ume_body_rect[4], "Foxes.png")
      -- Draw these arms behind the wall
      func:DrawRectGraph(tsubaki_arm_pos[5], tsubaki_arm_pos[6], tsubaki_arm3_rect[1], tsubaki_arm3_rect[2], tsubaki_arm3_rect[3], tsubaki_arm3_rect[4], "Foxes.png")
      func:DrawRectGraph(ume_arm_pos[5], ume_arm_pos[6], ume_arm3_rect[1], ume_arm3_rect[2], ume_arm3_rect[3], ume_arm3_rect[4], "Foxes.png")
    end

    -- The Gates
    func:DrawRectGraph(dan_in_move - dan_in_width, 0, 0, 0, dan_in_width, dan_in_height, "Dan_In.png")
    func:DrawRectGraph(bg_width - dan_in_move, 0, dan_in_width, 0, dan_in_width, dan_in_height, "Dan_In.png")

    -- Foxes (front)
    if animeCounter >= 0.2 and 0.275 >= animeCounter then
      -- none
    elseif animeCounter >= 0.275 and 0.35 >= animeCounter then
      func:DrawRectGraph(tsubaki_arm_pos[3], tsubaki_arm_pos[4], tsubaki_arm2_rect[1], tsubaki_arm2_rect[2], tsubaki_arm2_rect[3], tsubaki_arm2_rect[4], "Foxes.png")
      func:DrawRectGraph(ume_arm_pos[3], ume_arm_pos[4], ume_arm2_rect[1], ume_arm2_rect[2], ume_arm2_rect[3], ume_arm2_rect[4], "Foxes.png")
    elseif animeCounter >= 0.35 and 0.95 >= animeCounter then
      func:DrawRectGraph(tsubaki_arm_pos[1], tsubaki_arm_pos[2], tsubaki_arm1_rect[1], tsubaki_arm1_rect[2], tsubaki_arm1_rect[3], tsubaki_arm1_rect[4], "Foxes.png")
      func:DrawRectGraph(ume_arm_pos[1], ume_arm_pos[2], ume_arm1_rect[1], ume_arm1_rect[2], ume_arm1_rect[3], ume_arm1_rect[4], "Foxes.png")
    elseif animeCounter >= 0.95 and 1.025 >= animeCounter then
      func:DrawRectGraph(tsubaki_arm_pos[3], tsubaki_arm_pos[4], tsubaki_arm2_rect[1], tsubaki_arm2_rect[2], tsubaki_arm2_rect[3], tsubaki_arm2_rect[4], "Foxes.png")
      func:DrawRectGraph(ume_arm_pos[3], ume_arm_pos[4], ume_arm2_rect[1], ume_arm2_rect[2], ume_arm2_rect[3], ume_arm2_rect[4], "Foxes.png")
    elseif animeCounter >= 1.025 and 1.1 >= animeCounter then
      -- none
    end

    -- Gates Slam
    if dan_in_slam > 0 then
      func:SetOpacity(255 - math.min(dan_in_slam * 1020, 255), "Slam.png")
      func:DrawRectGraph(760 - (dan_in_slam * 400), 0, 0, 0, 200, dan_in_height, "Slam.png")
      func:DrawRectGraph(960 + (dan_in_slam * 400), 0, 200, 0, 200, dan_in_height, "Slam.png")

      func:SetOpacity(math.max(math.min((math.min(dan_in_slam - 0.7, 0.25) - math.max(0, dan_in_slam - 0.95)) * 1020, 255), 0), "Foxes.png")
      func:DrawGraphRectCenter(light_pos[1], light_pos[2], light_blue_rect[1], light_blue_rect[2], light_blue_rect[3], light_blue_rect[4], "Foxes.png")
      func:DrawGraphRectCenter(light_pos[3], light_pos[4], light_red_rect[1], light_red_rect[2], light_red_rect[3], light_red_rect[4], "Foxes.png")
      func:SetRotation(dan_in_slam * 720, "Foxes.png")
      func:DrawGraphRectCenter(star_pos[1], star_pos[2], star_blue_rect[1], star_blue_rect[2], star_blue_rect[3], star_blue_rect[4], "Foxes.png")
      func:SetRotation(dan_in_slam * -720, "Foxes.png")
      func:DrawGraphRectCenter(star_pos[3], star_pos[4], star_red_rect[1], star_red_rect[2], star_red_rect[3], star_red_rect[4], "Foxes.png")
      func:SetRotation(0, "Foxes.png")

      func:SetOpacity(math.max(math.min((dan_in_slam - 1.2) * 510, 255), 0), "Dan_In_Shadow.png")
      func:SetOpacity(math.max(math.min((dan_in_slam - 0.2) * 1020, 255), 0), "Message.png")
      func:DrawGraph(0, 0, "Dan_In_Shadow.png")
      func:DrawGraph(0, 0, "Message.png")
    end

    -- Speech Bubble
    if dan_in_slam > 1.2 then
      local speech_fadein = math.max(math.min((dan_in_slam - 1.7) * 1020, 255), 0)

      func:SetOpacity(speech_fadein, "Speech/Speech.png")
      func:SetOpacity(speech_fadein, speech_text)
      func:DrawGraphRectCenter(speech_pos[1], speech_pos[2], speech_rect[1], speech_rect[2], speech_rect[3], speech_rect[4], "Speech/Speech.png")
      func:DrawGraphRectCenter(speech_pos[3], speech_pos[4], speech_rect[5], speech_rect[6], speech_rect[7], speech_rect[8], "Speech/Speech.png")
      func:DrawGraphRectCenter(speech_pos[1], speech_pos[2], speech_rect[1], speech_rect[2], speech_rect[3], speech_rect[4], speech_text)
      func:DrawGraphRectCenter(speech_pos[3], speech_pos[4], speech_rect[5], speech_rect[6], speech_rect[7], speech_rect[8], speech_text)
    end
end
