
--func:DrawText(x, y, text);
--func:DrawNum(x, y, num);
--func:AddGraph("filename");
--func:DrawGraph(x, y, "filename");
--func:SetOpacity(opacity, "filename");
--func:SetScale(xscale, yscale, "filename");

local fps = 0
local deltaTime = 0
local isClear = { false, false, false, false }
local towerNightOpacity = 0

function updateValues(_deltaTime, _fps, _isClear, _towerNightOpacity)
    deltaTime = _deltaTime
    fps = _fps
    towerNightOpacity = _towerNightOpacity
    deltaTime = _deltaTime
end

function clearIn(player)
end

function clearOut(player)
end

function init()
end

function update()
end

function draw()
end
