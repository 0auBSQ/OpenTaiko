-- Story scene music; each background names its loop (story/backgrounds.lua `music`).

local Config = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Volume = Layout.story.music.volume_percent
local Fades  = Animation.story.music
local TRACKS = {
    forest = { path = "Sounds/story/forest.ogg", vol = Volume.forest },
    dojo   = { path = "Sounds/story/dojo.ogg",   vol = Volume.dojo },
}
local FADE_IN, CROSS = Fades.fade_in_s, Fades.crossfade_s
local MIN_FADE_OUT = Fades.min_fade_out_s

local _voices = {}                    -- { key, snd, vol, k = 0..1, target = 0 or 1, rate = per second }
local _key = nil                      -- the loop that should be heard

local function apply(v)
    pcall(function() v.snd:SetVolumePercent(v.vol * v.k) end)
end

local function drop(i)
    local v = table.remove(_voices, i)
    pcall(function() v.snd:Stop() ; v.snd:Dispose() end)
end

-- nil keeps the one playing
function M.play(key)
    if key == nil or key == _key then return end
    local tr = TRACKS[key]
    if tr == nil then return end
    _key = key
    for _, v in ipairs(_voices) do v.target, v.rate = 0, 1 / CROSS end
    local ok, s = pcall(function() return SOUND:CreateBGM(tr.path) end)
    if not ok or s == nil then return end
    local v = { key = key, snd = s, vol = tr.vol, k = 0, target = 1, rate = 1 / ((#_voices > 0) and CROSS or FADE_IN) }
    _voices[#_voices + 1] = v
    pcall(function() s:SetLoop(true) end)
    apply(v)
    pcall(function() s:Play() end)
end

-- fades every loop out over sec seconds
function M.fadeOut(sec)
    _key = nil
    for _, v in ipairs(_voices) do v.target, v.rate = 0, 1 / math.max(MIN_FADE_OUT, sec) end
end

-- true while a loop can still be heard
function M.playing() return #_voices > 0 end

function M.update(dt)
    local i = 1
    while i <= #_voices do
        local v = _voices[i]
        if v.k < v.target then v.k = math.min(v.target, v.k + v.rate * dt)
        elseif v.k > v.target then v.k = math.max(v.target, v.k - v.rate * dt) end
        if v.target == 0 and v.k <= 0 then
            drop(i)
        else
            apply(v)
            i = i + 1
        end
    end
end

function M.stop()
    while #_voices > 0 do drop(#_voices) end
    _key = nil
end

return M
