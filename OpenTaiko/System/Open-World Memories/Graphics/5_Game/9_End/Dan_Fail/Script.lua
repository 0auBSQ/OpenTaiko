--func:DrawText(x, y, text);
--func:DrawNum(x, y, num);
--func:AddGraph("filename");
--func:DrawGraph(x, y, filename);
--func:SetOpacity(opacity, "filename");
--func:SetScale(xscale, yscale, "filename");
--func:SetColor(r, g, b, "filename");

local animeCounter = 0

local bg_width = 1920
local bg_height = 1080

local dan_in_width = 960
local dan_in_height = 1080

local dan_in_move = 0
local dan_first_in_move = 0
local dan_second_in_move = 0
local dan_in_slam = 0

function playEndAnime(player)
end

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("l_back.png")
    func:AddGraph("l_front_1.png")
    func:AddGraph("l_front_2.png")
    func:AddGraph("r_back.png")
    func:AddGraph("r_front_1.png")
    func:AddGraph("r_front_2.png")
    func:AddGraph("Dan_In.png")
    func:AddGraph("Slam.png")
    func:AddGraph("Message.png")
end

function update(player)

    animeCounter = animeCounter + deltaTime

    dan_first_in_move = math.min(960 * animeCounter, 140)
    dan_second_in_move = math.min(math.max(2880 * (animeCounter - 1.5), 0), 820)

    dan_in_move = dan_first_in_move + dan_second_in_move

    if dan_in_move == dan_in_width then
      dan_in_slam = dan_in_slam + deltaTime
    end

end

function draw(player)

    -- Foxes (back)
    if animeCounter >= 0.3 and 0.4 >= animeCounter then
      func:DrawGraph(-40, 267, "l_back.png")
      func:DrawGraph(1624, 267, "r_back.png")
    elseif animeCounter >= 0.4 and 1.3 >= animeCounter then
      func:DrawGraph(20, 267, "l_back.png")
      func:DrawGraph(1564, 267, "r_back.png")
    elseif animeCounter >= 1.3 and 1.4 >= animeCounter then
      func:DrawGraph(-40, 267, "l_back.png")
      func:DrawGraph(1624, 267, "r_back.png")
    elseif animeCounter >= 1.4 and 1.5 >= animeCounter then
      func:DrawGraph(-100, 267, "l_back.png")
      func:DrawGraph(1684, 267, "r_back.png")
    end

    -- The Gates
    func:DrawRectGraph(dan_in_move - dan_in_width, 0, 0, 0, dan_in_width, dan_in_height, "Dan_In.png")
    func:DrawRectGraph(bg_width - dan_in_move, 0, dan_in_width, 0, dan_in_width, dan_in_height, "Dan_In.png")

    -- Foxes (front)
    if animeCounter >= 0.3 and 0.4 >= animeCounter then
      func:DrawGraph(133, 695, "l_front_2.png")
      func:DrawGraph(1728, 695, "r_front_2.png")
    elseif animeCounter >= 0.4 and 1.4 >= animeCounter then
      func:DrawGraph(23, 695, "l_front_1.png")
      func:DrawGraph(1653, 695, "r_front_1.png")
    elseif animeCounter >= 1.4 and 1.5 >= animeCounter then
      func:DrawGraph(133, 695, "l_front_2.png")
      func:DrawGraph(1728, 695, "r_front_2.png")
    end

    -- Gates Slam
    if dan_in_slam > 0 then
      func:SetOpacity(255 - math.min(dan_in_slam * 1020, 255), "Slam.png")
      func:DrawRectGraph(760 - (dan_in_slam * 400), 0, 0, 0, 200, dan_in_height, "Slam.png")
      func:DrawRectGraph(960 + (dan_in_slam * 400), 0, 200, 0, 200, dan_in_height, "Slam.png")

      func:SetOpacity(math.max(math.min((dan_in_slam - 0.2) * 1020, 255), 0), "Message.png")
      func:DrawGraph(0, 0, "Message.png")
    end

    --if animeCounter >= 1.133 then
      --func:DrawGraph(0, 0, "Message.png")
    --end

end
