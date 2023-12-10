local bgWidth_1 = 1984
local bgWidth_4 = 962
local bgScrollX_1 = 0
local bgScrollX_4 = 0
local sunX = -320
local sunY = -320
local sunRot = 0

local flowerScrollX_1 = 0
local flowerScrollX_2 = 150
local flowerScrollX_3 = 300
local flowerScrollY_1 = 0
local flowerScrollY_2 = 200
local flowerScrollY_3 = 350
local flowerRot1 = 0
local flowerRot2 = 0
local flowerRot3 = 0
local flowerSway_1 = 0
local flowerSwayFinal_1 = 0
local flowerSway_2 = 0
local flowerSwayFinal_2 = 0
local flowerSway_3 = 0
local flowerSwayFinal_3 = 0

function clearIn(player)
end

function clearOut(player)
end

function init()
    func:AddGraph("Skybox.png");
	func:AddGraph("Dojo.png");
	func:AddGraph("Bush.png");
    func:AddGraph("Clouds.png");
    func:AddGraph("Flower1.png");
    func:AddGraph("Flower2.png");
    func:AddGraph("Flower3.png");
    func:AddGraph("Sun.png");
end

function update()
  bgScrollX_1 = bgScrollX_1 + (59.1 * deltaTime);
  if bgScrollX_1 > bgWidth_1 then
      bgScrollX_1 = 0;
  end

  flowerScrollX_1 = flowerScrollX_1 + (75 * deltaTime);
  flowerScrollX_2 = flowerScrollX_2 + (75 * deltaTime);
  flowerScrollX_3 = flowerScrollX_3 + (75 * deltaTime);

  bgScrollX_4 = bgScrollX_4 + (100 * deltaTime);
  if bgScrollX_4 > bgWidth_4 then
    bgScrollX_4 = 0;
  end

  flowerScrollY_1 = flowerScrollY_1 + (100 * deltaTime);
  if flowerScrollY_1 > 500 then
    flowerScrollY_1 = 0;
    flowerScrollX_1 = 0;
  end
  flowerScrollY_2 = flowerScrollY_2 + (100 * deltaTime);
  if flowerScrollY_2 > 500 then
    flowerScrollY_2 = 0;
    flowerScrollX_2 = 200;
  end
  flowerScrollY_3 = flowerScrollY_3 + (100 * deltaTime);
  if flowerScrollY_3 > 500 then
    flowerScrollY_3 = 0;
    flowerScrollX_3 = -150;
  end

  flowerSway_1 = flowerSway_1 + (50 * deltaTime);
  flowerSwayFinal_1 = 70 * math.cos((5 * flowerSway_1) / 150) * math.sin((2 * flowerSway_1) / 150);
  flowerSway_2 = flowerSway_2 + (70 * deltaTime);
  flowerSwayFinal_2 = 60 * math.cos((5 * flowerSway_2) / 120) * math.sin((2 * flowerSway_2) / 120);
  flowerSway_3 = flowerSway_3 + (36 * deltaTime);
  flowerSwayFinal_3 = 40 * math.cos((5 * flowerSway_2) / 130) * math.sin((2 * flowerSway_2) / 130);

  sunRot = sunRot - (8 * deltaTime);
  flowerRot1 = flowerRot1 + (28 * deltaTime);
  flowerRot2 = flowerRot2 + (40 * deltaTime);
  flowerRot3 = flowerRot3 + (50 * deltaTime);
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
      func:SetRotation(flowerRot1 + (i * 60), "Flower1.png");
      func:SetRotation(flowerRot2 + (i * 60), "Flower2.png");
      func:SetRotation(flowerRot3 + (i * 60), "Flower3.png");
      func:DrawGraph((i * 600) - flowerScrollX_1 + flowerSwayFinal_1, -70 + flowerScrollY_1 + (-75 * (i % 2)), "Flower1.png" );
      func:DrawGraph((i * 500) - flowerScrollX_2 + flowerSwayFinal_2, -70 + flowerScrollY_2 + (-60 * (i % 2)), "Flower2.png" );
      func:DrawGraph((i * 700) - flowerScrollX_3 + flowerSwayFinal_3, -70 + flowerScrollY_3 + (-50 * (i % 2)), "Flower3.png" );
    end
end
