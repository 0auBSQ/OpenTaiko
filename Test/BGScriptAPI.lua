local playerCount = 0
local p1IsBlue = false

local fps = 0
local deltaTime = 0
local isClear = { false, false }
local towerNightNum = 0

function setConstValues(_playerCount, _p1IsBlue)
    playerCount = _playerCount
    p1IsBlue = _p1IsBlue
end

function updateValues(_deltaTime, _fps, _isClear, _towerNightNum)
    deltaTime = _deltaTime
    fps = _fps
    isClear = _isClear
    towerNightNum = _towerNightNum
    deltaTime = _deltaTime
end