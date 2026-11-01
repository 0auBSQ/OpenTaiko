-- OWM3d sky shared by every pagoda screen.

local save = require("save")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Sky = Layout.stage.sky
local GREY_STOPS = Sky.grey_stops
local GREY_BLEND = Sky.grey_blend
M.GREY_STOPS, M.GREY_BLEND = GREY_STOPS, GREY_BLEND     -- for art that stands in for the sky

local CLEAR_LEVEL = 20       -- beating this level ends the grey sky
local ROOF_LEVEL  = 21
local BASE_HOUR   = Sky.hour
local HOUR_STEP   = Sky.hour_step        -- hours per roof floor
local MAX_HOUR    = Sky.max_hour
local BASE_PITCH  = Sky.pitch_deg
local HOUR_STIFFNESS = Animation.stage.sky.hour_stiffness

-- hour, r, g, b
local FILTER_KEYS = Sky.filter_keys

local _world = nil
local _ramp  = nil
local _hour, _pitch = nil, nil
local _hourVel, _glided = 0, nil        -- the hour glide's speed (hours/s) and the hour it last gave
local _baseYaw, _yaw = nil, nil
local _speed = 1

function M.setup(world)
    _world = world
    _hour, _pitch = nil, nil
    _hourVel, _glided = 0, nil
    _baseYaw = world.cam.yaw
    _yaw = _baseYaw
    _speed = 1
    if _ramp == nil then
        local ok, ramp = pcall(function() return GRADIENT:Create(GREY_STOPS, GREY_BLEND) end)
        if ok then _ramp = ramp end
    end
end

function M.dispose()
    if _ramp ~= nil then pcall(_ramp.Dispose, _ramp) end
    _ramp = nil
    _world = nil
    _hour, _pitch = nil, nil
    _baseYaw, _yaw = nil, nil
    _speed = 1
end

-- 1 while level 20 has not been beaten, else 0; highest defaults to the saved one
function M.rule(highest)
    return (highest or save.highest_level()) >= CLEAR_LEVEL and 0 or 1
end

local function render(grey)
    _world:render()
    if grey >= 0.999 then
        if _ramp ~= nil then GRADIENT:SetActive(_ramp) end
        _world:blit()
    elseif grey <= 0.001 then
        _world:blit()
    else
        _world:blit()
        local sc = _world.scene
        sc:SetOpacity(grey)
        if _ramp ~= nil then GRADIENT:SetActive(_ramp) end
        sc:Draw(0, 0)
        sc:SetOpacity(1)
    end
end

-- grey: 0 blue .. 1 grey; visible = false skips the sky entirely. Returns false on error.
function M.draw(grey, visible)
    if _world == nil or not visible then return true end
    local ok = pcall(render, grey or 0)
    GRADIENT:ClearActive()
    return ok
end

-- sets hour and pitch when they change (nil keeps the current one); applied at once, for the next draw
function M.setView(hour, pitch)
    if _world == nil then return end
    if hour ~= nil and hour ~= _hour then
        _world.daynight:setHour(hour)
        _hour = hour
        if _world.lit then _world.daynight:update(0) end
    end
    if pitch ~= nil and pitch ~= _pitch then
        _world.cam:setPitch(pitch)
        _pitch = pitch
        _world.cam:apply()
    end
end

-- the shown hour
function M.hour()
    return _hour or BASE_HOUR
end

-- moves the shown hour toward target over dt (critically damped spring); pass the result to setView.
-- Setting the hour any other way stops the glide.
function M.glideHour(target, dt)
    local h = _hour
    if h == nil then return target end
    if h ~= _glided then _hourVel = 0 end
    local w, x = HOUR_STIFFNESS, h - target
    local k = (_hourVel + w * x) * dt
    local e = math.exp(-w * dt)
    x, _hourVel = (x + k) * e, (_hourVel - w * k) * e
    if math.abs(x) < 0.002 and math.abs(_hourVel) < 0.01 then x, _hourVel = 0, 0 end
    _glided = target + x
    return _glided
end

-- yaw in degrees (lower pans right on screen); nil restores the default
function M.setYaw(deg)
    if _world == nil then return end
    local y = deg or _baseYaw
    if y == nil or y == _yaw then return end
    _world.cam:setYaw(y)
    _yaw = y
    _world.cam:apply()
end

-- yaw in degrees
function M.yaw()
    return _yaw
end

-- multiplier the stage applies to the sky's clock (1 = normal)
function M.setSpeed(k)
    _speed = k or 1
end

function M.speed()
    return _speed
end

-- keepHour: the hour stays for the next screen to glide home
function M.reset(keepHour)
    M.setView(not keepHour and BASE_HOUR or nil, BASE_PITCH)
    M.setYaw(nil)
    _speed = 1
end

function M.hourFor(level)
    if level == nil or level < ROOF_LEVEL then return BASE_HOUR end
    return math.min(MAX_HOUR, BASE_HOUR + HOUR_STEP * (level - ROOF_LEVEL))
end

-- time-of-day colour multiplier, linear between keyframes
function M.filterFor(hour)
    local first, last = FILTER_KEYS[1], FILTER_KEYS[#FILTER_KEYS]
    if hour == nil or hour <= first[1] then return first[2], first[3], first[4] end
    if hour >= last[1] then return last[2], last[3], last[4] end
    for i = 2, #FILTER_KEYS do
        local b = FILTER_KEYS[i]
        if hour <= b[1] then
            local a = FILTER_KEYS[i - 1]
            local t = (hour - a[1]) / (b[1] - a[1])
            return a[2] + (b[2] - a[2]) * t,
                   a[3] + (b[3] - a[3]) * t,
                   a[4] + (b[4] - a[4]) * t
        end
    end
    return last[2], last[3], last[4]
end

return M
