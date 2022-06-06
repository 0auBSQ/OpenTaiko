
--func:DrawText(x, y, text);
--func:DrawNum(x, y, num);
--func:AddGraph("filename");
--func:DrawGraph(x, y, filename);
--func:SetOpacity(opacity, "filename");
--func:SetScale(xscale, yscale, "filename");
--func:SetColor(r, g, b, "filename");

local fps = 0
local deltaTime = 0
local isClear = { false, false }
local towerNightNum = 0

function updateValues(_deltaTime, _fps, _isClear, _towerNightNum)
    deltaTime = _deltaTime
    fps = _fps
    isClear = _isClear
    towerNightNum = _towerNightNum
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
