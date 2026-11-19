-- screen between a story/clear and a floor (selector tower scene)
-- update(): nil while running, "done" once faded out

local scene    = require("selector_scene")
local selector = require("selector")
local skybox   = require("skybox")
local Config   = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local M = {}

local Time = Animation.stage.floor_transition
local SCATTER_SEC = Time.scatter_s
local SETTLE_SEC  = Time.settle_s       -- after dispersal, before the climb
local SCROLL_SEC  = Time.scroll_s       -- camera climb
local HOLD_SEC    = Time.hold_s         -- on the destination plaque
local FADE_SEC    = Time.fade_s

local _tex = nil
local _phase = "idle"          -- idle|scatter|settle|scroll|hold|fadeout
local _from, _to, _highest = 1, 1, 0
local _cam, _camFrom, _camTo = 0, 0, 0
local _t, _fade = 0, 0
local _scP = 0                 -- linear 0..1 dispersal
local _sfx = {}

local function sk(key, fallback)
    local s = THEME:GetSkinString(key)
    if s == nil or s == "" then return fallback end
    return s
end

local function loadSfx(name)
    local ok, s = pcall(function() return SOUND:CreateSFX("Sounds/" .. name .. ".ogg") end)
    if ok and s ~= nil then return s end
    return nil
end

local function playSfx(name)
    local s = _sfx[name]
    if s ~= nil then s:Play() end
end

-- roof sky hours as in the selector; noon below
local function sky_hour(level)
    return skybox.hourFor(level)
end

local function smoothstep(p) return p * p * (3 - 2 * p) end

function M.setup(tex)
    _tex = tex
    for _, n in ipairs({ "scatter", "arrive" }) do
        if _sfx[n] == nil then _sfx[n] = loadSfx(n) end
    end
end

function M.active() return _phase ~= "idle" end

-- grey, visible for the sky; nil while idle
function M.sky()
    if _phase == "idle" then return nil end
    if _phase == "scatter" and _from == 20 then return 1 - smoothstep(_scP), true end
    return skybox.rule(_highest), true
end

-- from: floor just played (== to after a story)
-- to: floor entered next
-- highest: highest level shown beaten after the clear (door darkening, sky)
-- scatter: from was shrouded before the clear (its darkness disperses first)
function M.open(from, to, highest, scatter)
    _from, _to, _highest = from or to, to, highest or 0
    _camFrom = scene.floor_center(_from) - THEME:GetResolution().Y * 0.5
    _camTo   = scene.floor_center(_to)   - THEME:GetResolution().Y * 0.5
    _cam, _t, _fade = _camFrom, 0, 0
    _scP = 0
    -- sky takes the departing floor's hour now (first update follows this draw)
    selector.applySky(sky_hour(_from), _cam)
    if scatter and _from >= 11 and _from <= 20 then
        scene.setScatter(_from, 0)
        _phase = "scatter"
        playSfx("scatter")
    else
        scene.setScatter(nil, 0)
        _phase = (_from == _to) and "hold" or "scroll"
    end
end

function M.update(dt)
    if _phase == "idle" then return nil end
    _t = _t + dt

    if _phase == "scatter" then
        selector.applySky(sky_hour(_from), _cam)
        _scP = math.min(1, _t / SCATTER_SEC)
        scene.setScatter(_from, smoothstep(_scP))
        if _t >= SCATTER_SEC then
            scene.setScatter(nil, 0)
            _t = 0
            _phase = "settle"
        end
        return nil
    end

    if _phase == "settle" then
        selector.applySky(sky_hour(_from), _cam)
        if _t >= SETTLE_SEC then
            _t = 0
            _phase = (_from == _to) and "hold" or "scroll"
        end
        return nil
    end

    if _phase == "scroll" then
        local p = math.min(1, _t / SCROLL_SEC)
        local e = 1 - (1 - p) * (1 - p) * (1 - p)          -- ease-out cubic
        _cam = _camFrom + (_camTo - _camFrom) * e
        -- sky glides to the next floor's hour during climb and hold
        selector.applySky(skybox.glideHour(sky_hour(_to), dt), _cam)
        if p >= 1 then
            _t = 0 ; _phase = "hold"
            playSfx("arrive")
        end
        return nil
    end

    if _phase == "hold" then
        _cam = _camTo
        selector.applySky(skybox.glideHour(sky_hour(_to), dt), _cam)
        if _t >= HOLD_SEC then _t = 0 ; _phase = "fadeout" end
        return nil
    end

    if _phase == "fadeout" then
        selector.applySky(skybox.glideHour(sky_hour(_to), dt), _cam)
        _fade = math.min(1, _fade + dt / FADE_SEC)
        if _fade >= 1 then
            _phase = "idle"
            scene.setScatter(nil, 0)
            return "done"
        end
        return nil
    end
    return nil
end

function M.draw()
    if _phase == "idle" then return end
    local res = THEME:GetResolution()

    -- beaten floor stays dark until the dispersal ends (pre-clear highest)
    local shown = (_phase == "scatter") and math.min(_highest, _from - 1) or _highest
    scene.draw(_cam, shown)

    -- plaque level: floor left, then floor entered
    local plaqueLevel = (_phase == "scroll" or _phase == "hold" or _phase == "fadeout") and _to or _from
    selector.drawPlaque(plaqueLevel, sk("PAGODA_NEXT", "Next..."))

    if _fade > 0 then
        local b = _tex.black1
        if b ~= nil and b.Loaded then
            b:SetScale(res.X / math.max(1, b.Width), res.Y / math.max(1, b.Height))
            b:SetOpacity(_fade) ; b:Draw(0, 0)
            b:SetOpacity(1) ; b:SetScale(1, 1)
        end
    end
end

return M
