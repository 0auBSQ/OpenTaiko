local bgWidth_1 = 1984
local bgWidth_4 = 962
local bgScrollX_1 = 0
local bgScrollX_4 = 0
local snowHeight = 276
local sunX = -320
local sunY = -320
local sunRot = 0
local snowSway_1 = 0
local snowSway_2 = 0
local snowSway_3 = 0
local snowSwayFinal_1 = 0
local snowSwayFinal_2 = 0
local snowSwayFinal_3 = 0
local snowScrollY_1 = 0
local snowScrollY_2 = 0
local snowScrollY_3 = 0

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Skybox.png");
	  func:AddGraph("Dojo.png");
	  func:AddGraph("Bush.png");
    func:AddGraph("Clouds.png");
    func:AddGraph("Snow1.png");
    func:AddGraph("Snow2.png");
    func:AddGraph("Snow3.png");
    func:AddGraph("Sun.png");
end

function update()
    bgScrollX_1 = bgScrollX_1 + (59.1 * deltaTime);
    if bgScrollX_1 > bgWidth_1 then
        bgScrollX_1 = 0;
    end

    bgScrollX_4 = bgScrollX_4 + (100 * deltaTime);
    if bgScrollX_4 > bgWidth_4 then
        bgScrollX_4 = 0;
    end

    snowSway_1 = snowSway_1 + (50 * deltaTime);

    snowSwayFinal_1 = 30 * math.sin((2 * snowSway_1) / 150);
    snowSwayFinal_2 = 25 * math.sin((2 * snowSway_1) / 150);
    snowSwayFinal_3 = 20 * math.sin((2 * snowSway_1) / 150);

    snowScrollY_1 = snowScrollY_1 + (60 * deltaTime);
    if snowScrollY_1 > snowHeight then
        snowScrollY_1 = 0;
    end
    snowScrollY_2 = snowScrollY_2 + (75 * deltaTime);
    if snowScrollY_2 > snowHeight then
        snowScrollY_2 = 0;
    end
    snowScrollY_3 = snowScrollY_3 + (90 * deltaTime);
    if snowScrollY_3 > snowHeight then
        snowScrollY_3 = 0;
    end

	sunRot = sunRot - (8 * deltaTime);
end


function draw()
    func:DrawGraph(0, 0, "Skybox.png");
	  func:SetRotation(sunRot, "Sun.png");
	  func:DrawGraph(sunX, sunY, "Sun.png");
    for i = 0, 4 do
        func:DrawGraph((i * bgWidth_1) - bgScrollX_1, 0, "Clouds.png");
    end
	func:DrawGraph(0, 0, "Dojo.png");
	func:DrawGraph(0, 0, "Bush.png");
	for i = 0, 4 do
        for j = 0, 2 do
          func:DrawGraph((i * bgWidth_4) - snowSwayFinal_1 + -300, snowScrollY_1 - (snowHeight * j), "Snow1.png");
          func:DrawGraph((i * bgWidth_4) - snowSwayFinal_2 + -300, snowScrollY_2 - (snowHeight * j), "Snow2.png");
          func:DrawGraph((i * bgWidth_4) - snowSwayFinal_3 + -300, snowScrollY_3 - (snowHeight * j), "Snow3.png");
        end
    end
end
