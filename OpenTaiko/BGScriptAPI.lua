local playerCount = 0
local p1IsBlue = false
local lang = "ja"
local simplemode = false
local puchicharaRarities = { "Common", "Common", "Common", "Common", "Common" }
local characterRarities = { "Common", "Common", "Common", "Common", "Common" }

local fps = 0
local deltaTime = 0
local isClear = { false, false, false, false, false }
local towerNightNum = 0
local battleState = 0
local battleWin = false
local gauge = { 0, 0, 0, 0, 0 }
local bpm = { 0, 0, 0, 0, 0 }
local gogo = { false, false, false, false, false }
local timeStamp = -1


function setConstValues(_playerCount, _p1IsBlue, _lang, _simplemode, _puchicharaRarities, _characterRarities)
    playerCount = _playerCount
    p1IsBlue = _p1IsBlue
    lang = _lang
    simplemode = _simplemode
    puchicharaRarities = _puchicharaRarities
    characterRarities = _characterRarities
end

function updateValues(_deltaTime, _fps, _isClear, _towerNightNum, _battleState, _battleWin, _gauge, _bpm, _gogo, _timestamp)
    deltaTime = _deltaTime
    fps = _fps
    isClear = _isClear
    towerNightNum = _towerNightNum
    battleState = _battleState
    battleWin = _battleWin
    gauge = _gauge
    bpm = _bpm
    gogo = _gogo
    timeStamp = _timestamp
end
