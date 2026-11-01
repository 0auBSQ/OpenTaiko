-- The floor's BGM loop on the encounter screen.

local floors = require("floors")

local M = {}

local _bgm = nil

function M.stop()
    if _bgm ~= nil then _bgm:Stop() ; _bgm:Dispose() ; _bgm = nil end
end

function M.start(level)
    M.stop()
    local ok, s = pcall(function() return SOUND:CreateBGM(floors.bgmPath(level)) end)
    if ok and s ~= nil then
        _bgm = s
        _bgm:SetLoop(true)
        _bgm:Play()
    end
end

-- true while a floor loop is loaded
function M.active() return _bgm ~= nil end

return M
