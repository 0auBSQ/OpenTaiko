local bgWidth_1 = 1984
local bgWidth_4 = 962
local bgScrollX_1 = 0
local bgScrollX_4 = 0
local sunX = -320
local sunY = -320
local sunRot = 0

local leafScrollX_1 = 0
local leafScrollX_2 = 150
local leafScrollX_3 = 300
local leafScrollY_1 = 0
local leafScrollY_2 = 200
local leafScrollY_3 = 350
local leafRot1 = 0
local leafRot2 = 0
local leafRot3 = 0
local leafSway_1 = 0
local leafSwayFinal_1 = 0
local leafSway_2 = 0
local leafSwayFinal_2 = 0
local leafSway_3 = 0
local leafSwayFinal_3 = 0

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Skybox.png");
	  func:AddGraph("Dojo.png");
	  func:AddGraph("Bush.png");
    func:AddGraph("Clouds.png");
    func:AddGraph("Leaf1.png");
    func:AddGraph("Leaf2.png");
    func:AddGraph("Leaf3.png");
    func:AddGraph("Sun.png");
end

function update()
    bgScrollX_1 = bgScrollX_1 + (59.1 * deltaTime);
    if bgScrollX_1 > bgWidth_1 then
        bgScrollX_1 = 0;
    end

    leafScrollX_1 = leafScrollX_1 + (75 * deltaTime);
    leafScrollX_2 = leafScrollX_2 + (75 * deltaTime);
    leafScrollX_3 = leafScrollX_3 + (75 * deltaTime);

    bgScrollX_4 = bgScrollX_4 + (100 * deltaTime);
    if bgScrollX_4 > bgWidth_4 then
        bgScrollX_4 = 0;
    end
    leafScrollY_1 = leafScrollY_1 + (100 * deltaTime);
    if leafScrollY_1 > 500 then
        leafScrollY_1 = 0;
        leafScrollX_1 = 0;
    end
    leafScrollY_2 = leafScrollY_2 + (100 * deltaTime);
    if leafScrollY_2 > 500 then
        leafScrollY_2 = 0;
        leafScrollX_2 = 200;
    end
    leafScrollY_3 = leafScrollY_3 + (100 * deltaTime);
    if leafScrollY_3 > 500 then
        leafScrollY_3 = 0;
        leafScrollX_3 = -150;
    end

    leafSway_1 = leafSway_1 + (50 * deltaTime);
    leafSwayFinal_1 = 70 * math.cos((5 * leafSway_1) / 150) * math.sin((2 * leafSway_1) / 150);
    leafSway_2 = leafSway_2 + (70 * deltaTime);
    leafSwayFinal_2 = 60 * math.cos((5 * leafSway_2) / 120) * math.sin((2 * leafSway_2) / 120);
    leafSway_3 = leafSway_3 + (36 * deltaTime);
    leafSwayFinal_3 = 40 * math.cos((5 * leafSway_2) / 130) * math.sin((2 * leafSway_2) / 130);

	sunRot = sunRot - (8 * deltaTime);
  leafRot1 = leafRot1 + (28 * deltaTime);
  leafRot2 = leafRot2 + (40 * deltaTime);
  leafRot3 = leafRot3 + (50 * deltaTime);
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
        func:SetRotation(leafRot1 + (i * 60), "Leaf1.png");
        func:SetRotation(leafRot2 + (i * 60), "Leaf2.png");
        func:SetRotation(leafRot3 + (i * 60), "Leaf3.png");
        func:DrawGraph((i * 600) - leafScrollX_1 + leafSwayFinal_1, -70 + leafScrollY_1 + (-75 * (i % 2)), "Leaf1.png" );
        func:DrawGraph((i * 500) - leafScrollX_2 + leafSwayFinal_2, -70 + leafScrollY_2 + (-60 * (i % 2)), "Leaf2.png" );
        func:DrawGraph((i * 700) - leafScrollX_3 + leafSwayFinal_3, -70 + leafScrollY_3 + (-50 * (i % 2)), "Leaf3.png" );
    end
end
