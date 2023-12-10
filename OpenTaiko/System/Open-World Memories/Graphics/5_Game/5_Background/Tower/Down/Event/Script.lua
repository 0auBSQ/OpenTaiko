local towerUpProgress = 0
local lastNightNum = 0
local skyHeight = 7434

function clearIn(player)

end

function clearOut(player)

end

function init()
    func:AddGraph("Sky_Gradient.png");
end

function update()

    towerUpProgress = towerUpProgress + ((deltaTime * (bpm[0] / 120)) / 140);
    if towerUpProgress > 1 then
      towerUpProgress = 1
    elseif towerUpProgress > lastNightNum then
      towerUpProgress = lastNightNum
    end

    if towerNightNum ~= lastNightNum then
      towerUpProgress = lastNightNum
      lastNightNum = towerNightNum
    end

end

function draw()
    func:DrawRectGraph(0, 540, 0, skyHeight - (towerUpProgress * skyHeight), 1920, 540, "Sky_Gradient.png");
    -- Debugging stuff
    --func:DrawNum(0, 540, towerUpProgress);
    --func:DrawNum(0, 556, towerUpProgress * skyHeight);
    --func:DrawNum(0, 572, skyHeight / 140);
end
