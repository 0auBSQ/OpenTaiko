local bgWidth_1 = 1323
local bgWidth_2 = 1280
local bgWidth_3 = 1280
local bgWidth_4 = 641
local bgWidth_5 = 639
local bgScrollX_1 = 0
local bgScrollX_2 = 0
local bgScrollX_3 = 0
local bgScrollX_4 = 0
local bgScrollX_5 = 0

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("0.png")
    func:AddGraph("1.png")
    func:AddGraph("2.png")
    func:AddGraph("3.png")
    func:AddGraph("4.png")
    func:AddGraph("5.png")
end

function update()
    bgScrollX_1 = bgScrollX_1 + (59.1 * deltaTime)
    if bgScrollX_1 > bgWidth_1 then
        bgScrollX_1 = 0;
    end
    
    bgScrollX_2 = bgScrollX_2 + (45.9 * deltaTime)
    if bgScrollX_2 > bgWidth_2 then
        bgScrollX_2 = 0;
    end
    
    bgScrollX_3 = bgScrollX_3 + (43.8 * deltaTime)
    if bgScrollX_3 > bgWidth_3 then
        bgScrollX_3 = 0;
    end
    
    bgScrollX_4 = bgScrollX_4 + (100 * deltaTime)
    if bgScrollX_4 > bgWidth_4 + 200 then
        bgScrollX_4 = 0;
    end
    
    bgScrollX_5 = bgScrollX_5 + (45.9 * deltaTime)
    if bgScrollX_5 > bgWidth_5 then
        bgScrollX_5 = 0;
    end
end


function draw()
    func:DrawGraph(0, 0, "0.png")
    for i = 0, 4 do
        func:DrawGraph((i * bgWidth_1) - bgScrollX_1, 0, "1.png")
        func:DrawGraph((i * bgWidth_2) - bgScrollX_2, 0, "2.png")
        func:DrawGraph((i * bgWidth_3) - bgScrollX_3, 0, "3.png")
        func:DrawGraph((i * bgWidth_4) - bgScrollX_4, -200 + bgScrollX_4, "4.png")
        func:DrawGraph((i * bgWidth_5) - bgScrollX_5, 0, "5.png")
    end
end
