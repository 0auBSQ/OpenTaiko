-- Level-10 cutscene (lines: story/cutscenes/level10.lua).

local assets   = require("encounter/assets")
local scene    = require("encounter/scene")
local view     = require("encounter/bands/view")
local sprite   = require("encounter/sprite")
local motion   = require("encounter/motion")
local Easing   = require("Easing")
local Emitter  = require("fx/emitter")
local presets  = require("fx/presets")
local Dissolve = require("fx/dissolve")
local Trail    = require("fx/dust_trail")
local air      = require("encounter/cutscenes/stair_air")
local Config   = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.cutscenes.level10
local Anim = Animation.encounter.cutscenes.level10

local REF_W, REF_H = 1920, 1080
local OVERSCAN = Look.overscan_px               -- px past each screen edge, for the shakes
local SCRIM_ALPHA = Look.scrim_opacity
local IDLE = { P = Anim.idle.period_s, A = Anim.idle.squash }   -- encounter/motion "counter": period, squash

local EMERGE_SEC, EMERGE_SINK, EMERGE_BEAT = Anim.emerge_s, Anim.emerge_sink_px, Anim.emerge_beat_s   -- fade-in, start depth (px), hold after it
local EMERGE_SHAKE, EMERGE_SHAKE_SEC = Anim.emerge_shake_px, Anim.emerge_shake_s

local DARKEN_SEC = Anim.darken_s
local DARK = Look.dark_color                    -- same as fx/dissolve
local BG_DIM = Look.bg_dim                      -- share of brightness the background loses
local RUMBLE = Anim.rumble_px                   -- px, until the reveal
local BUILD_VOL = 80                            -- buildup loop volume, %
local BUILD_FADE = Anim.build_fade_s            -- buildup fade-in
local BUILD_OUT = Anim.build_out_s              -- buildup fade-out at the reveal
local SMOKE_RATE = Anim.smoke_per_s             -- puffs/s at full darkness

local POP, POP_SEC = Anim.pop, Anim.pop_s
local GATHER_BEAT = Anim.gather_beat_s          -- delay before the swirl gathers
local LOOM_SEC, LOOM_GROW, LOOM_DROP = Anim.loom_s, Anim.loom_grow, Anim.loom_drop_px

-- 1920 space: SWIRL = the dust's swirl, TRAIL = its trail's two inner control points;
-- the trail ends DOOR_DEPTH down the doorway
local SWIRL = Config.plain(Look.swirl)
local TRAIL = Config.plain(Look.trail)
local DOOR_DEPTH = Look.door_depth
local DOOR_FALLBACK = Look.door_fallback

local SFX = {
    slam = "Sounds/result/slam.ogg", buildup = "Sounds/result/buildup.ogg",
    dissolve = "Sounds/result/dissolve.ogg", wind = "Sounds/result/wind.ogg",
}
local PHASE_SFX = { enter = { "slam" }, clear = { "buildup", "slam", "dissolve", "wind" }, fail = {} }
local SNAKE_KEYS = { "cut10_bg", "cut10_guard", "cut10_reflect" }

local _bg, _bgMeta = nil, nil
local _guard, _guardMeta = nil, nil
local _reflect, _reflectMeta = nil, nil
local _monster, _monsterMeta = nil, nil
local _footX, _footY = 0, 0
local _idle = nil
local _pts = {}
local _sfx = {}
local _smoke, _smokeAcc = nil, 0
local _trail, _flying = nil, false
local _dis, _disO, _gatherT = nil, nil, 0
local _form = "guard"           -- guard | monster
local _hidden = false
local _emergeT, _darkT, _buildT, _popT, _loomT = nil, nil, nil, nil, nil
local _buildOutT = nil
local _o ={ sx = 1, sy = 1, rot = 0, pivot = "foot", alpha = 1, r = 1, g = 1, b = 1 }

local function freeSounds()
    for _, s in pairs(_sfx) do s:Stop() ; s:Dispose() end
    _sfx = {}
end

local function play(name)
    local s = _sfx[name]
    if s ~= nil then s:Play() end
end

function M.keys(level, ctx, phase)
    local keys = {}
    for _, k in ipairs(SNAKE_KEYS) do keys[#keys + 1] = k end
    for _, k in ipairs(air.keys()) do keys[#keys + 1] = k end
    if phase == "clear" then
        keys[#keys + 1] = "cut10_monster"
        for _, k in ipairs(Trail.keys()) do keys[#keys + 1] = k end
        for _, k in ipairs(Emitter.texKeys(presets.guard_smoke)) do keys[#keys + 1] = k end
    end
    return keys
end

-- where the dust enters the doorway
local function doorPoint()
    local d = assets.data("cut10_door") or DOOR_FALLBACK
    return { x = d.x + d.w * 0.5, y = d.y + d.h * DOOR_DEPTH }
end

local function reset()
    _smoke, _smokeAcc, _trail, _flying = nil, 0, nil, false
    _dis, _disO, _gatherT = nil, nil, 0
    _form, _hidden = "guard", false
    _emergeT, _darkT, _buildT, _popT, _loomT = nil, nil, nil, nil, nil
    _buildOutT = nil
end

function M.open(level, ctx, phase)
    freeSounds()
    reset()
    air.open()
    _bg, _bgMeta = assets.get("cut10_bg")
    _guard, _guardMeta = assets.get("cut10_guard")
    _reflect, _reflectMeta = assets.get("cut10_reflect")
    _monster, _monsterMeta = assets.get("cut10_monster")
    if _guardMeta ~= nil then _footX, _footY = _guardMeta.x + _guardMeta.fx, _guardMeta.y + _guardMeta.fy end
    _idle = motion.new("counter", IDLE, 10)
    _hidden = phase == "enter"
    _pts = assets.data("cut10_dust") or {}
    if phase == "clear" then
        _smoke = Emitter.new(presets.guard_smoke, (assets.get("fx_dust")))
        _trail = Trail.new(_pts, SWIRL, { TRAIL[1], TRAIL[2], doorPoint() })
    end
    for _, n in ipairs(PHASE_SFX[phase] or {}) do
        local ok, s = pcall(function() return SOUND:CreateSFX(SFX[n]) end)
        if ok and s ~= nil then _sfx[n] = s end
    end
end

-- the timelines' current values
local function shown()
    if _emergeT ~= nil then return Easing.outQuad(_emergeT / EMERGE_SEC) end
    return _hidden and 0 or 1
end

local function rise()
    if _emergeT == nil then return 0 end
    return -EMERGE_SINK * (1 - Easing.outCubic(_emergeT / EMERGE_SEC))
end

local function darkness()
    return (_darkT ~= nil) and Easing.inOutSine(_darkT / DARKEN_SEC) or 0
end

local function popScale()
    return (_popT ~= nil) and 1 + POP * (1 - Easing.outCubic(_popT / POP_SEC)) or 1
end

local function loom()
    return (_loomT ~= nil) and Easing.inOutSine(_loomT / LOOM_SEC) or 0
end

-- returns the foot on screen (with the shake) and the reflection's pivot
local function feet()
    local drop, r = LOOM_DROP * loom(), rise()
    local x, y = view.xf(_footX, _footY + drop - r)
    local rx, ry = view.xf(_footX, _footY + drop + r)
    return x, y, rx, ry
end

-- x, y = screen px as the dissolve draws them (without the shake)
local function eatenAt(x, y) return _dis:eaten(x, y) end
local function placeAt(x, y)
    local fx, fy = feet()
    local ox, oy = view.shake()
    return sprite.point(_monsterMeta, fx - ox, fy - oy, _disO, x, y)
end

local function smoke(dt)
    if _smoke == nil or #_pts == 0 or _monsterMeta == nil then return end
    _smokeAcc = _smokeAcc + dt * SMOKE_RATE * darkness()
    while _smokeAcc >= 1 do
        _smokeAcc = _smokeAcc - 1
        local p = _pts[math.random(#_pts)]
        _smoke:burst(1, _monsterMeta.x + p[1], _monsterMeta.y + p[2])
    end
end

function M.update(dt)
    air.update(dt)
    if _dis == nil and _idle ~= nil then motion.step(_idle, dt) end
    if _emergeT ~= nil then _emergeT = _emergeT + dt end
    if _darkT ~= nil then
        _darkT = _darkT + dt
        if _form == "guard" then smoke(dt) end
    end
    if _buildT ~= nil and _buildT < BUILD_FADE then
        _buildT = _buildT + dt
        if _sfx.buildup ~= nil then _sfx.buildup:SetVolumePercent(BUILD_VOL * math.min(1, _buildT / BUILD_FADE)) end
    end
    if _buildOutT ~= nil then
        _buildOutT = _buildOutT + dt
        local s = _sfx.buildup
        if _buildOutT >= BUILD_OUT then
            if s ~= nil then s:Stop() end
            _buildOutT = nil
        elseif s ~= nil then
            s:SetVolumePercent(BUILD_VOL * (1 - _buildOutT / BUILD_OUT))
        end
    end
    if _popT ~= nil then _popT = _popT + dt end
    if _loomT ~= nil then _loomT = _loomT + dt end
    if _dis ~= nil then
        _dis:update(dt)
        if _trail ~= nil then _trail:shed(eatenAt, placeAt) end
        if _dis:done() then _gatherT = _gatherT + dt end
    end
    if _smoke ~= nil then _smoke:update(dt) end
    if _trail ~= nil then _trail:update(dt) end
end

function M.event(name, line)
    if name == "emerge" then
        _emergeT = 0
        play("slam")
        scene.shake(EMERGE_SHAKE, EMERGE_SHAKE_SEC)
    elseif name == "darken" then
        _darkT, _buildT = 0, 0
        scene.rumble(RUMBLE)
        local s = _sfx.buildup
        if s ~= nil then
            s:SetLoop(true)
            s:SetVolumePercent(0)
            s:Play()
        end
    elseif name == "reveal" then
        _form, _popT, _buildT = "monster", 0, nil
        if _darkT == nil then _darkT = DARKEN_SEC end
        scene.rumble(0)
        _buildOutT = 0
        play("slam")
    elseif name == "dissolve" then
        if _monsterMeta == nil or _idle == nil then return end
        local s = view.scale() * popScale()
        _disO = { sx = _idle.pose.sx * s, sy = _idle.pose.sy * s, rot = 0, pivot = "foot" }
        _dis, _gatherT = Dissolve.new("cut10_monster"), 0
        play("dissolve")
    elseif name == "trail" then
        if _trail ~= nil then
            _trail:fly()
            _flying = true
        end
        play("wind")
    elseif name == "loom" then
        _loomT = 0
    end
end

function M.busy()
    if _emergeT ~= nil and _emergeT < EMERGE_SEC + EMERGE_BEAT then return true end
    if _darkT ~= nil and _darkT < DARKEN_SEC then return true end
    if _dis ~= nil and (not _dis:done() or _gatherT < GATHER_BEAT) then return true end
    if _flying and not _trail:done() then return true end
    return false
end

function M.scrimAlpha()
    return SCRIM_ALPHA
end

local function setO(sx, sy, a, r, g, b)
    _o.sx, _o.sy, _o.alpha, _o.r, _o.g, _o.b = sx, sy, a, r, g, b
end

local function drawSnake()
    local a = shown()
    if a <= 0 or _idle == nil then return end
    local sx, sy
    if _disO ~= nil then
        sx, sy = _disO.sx, _disO.sy
    else
        local s = view.scale() * popScale() * (1 + LOOM_GROW * loom())
        sx, sy = _idle.pose.sx * s, _idle.pose.sy * s
    end
    local k = darkness()
    local r, g, b = 1 + (DARK[1] - 1) * k, 1 + (DARK[2] - 1) * k, 1 + (DARK[3] - 1) * k
    local x, y, rx, ry = feet()
    setO(sx, sy, (_dis ~= nil) and a * _dis:remaining() or a, r, g, b)
    sprite.draw(_reflect, _reflectMeta, rx, ry, _o)
    if _dis ~= nil then
        _dis:draw(_monster, _monsterMeta, x, y, _disO, 1, 1, 1, 1)
    elseif _form == "monster" then
        setO(sx, sy, a, 1, 1, 1)
        sprite.draw(_monster, _monsterMeta, x, y, _o)
    else
        setO(sx, sy, a, r, g, b)
        sprite.draw(_guard, _guardMeta, x, y, _o)
    end
end

-- while shaking, a scaled copy under it covers the edges the shake uncovers
local function drawBackdrop(ox, oy, lit)
    if _bg == nil or not _bg.Ready then return end
    if ox ~= 0 or oy ~= 0 then
        local x, y = view.xf(-OVERSCAN, -OVERSCAN * REF_H / REF_W)
        local s = view.scale() * (REF_W + 2 * OVERSCAN) / REF_W
        _bg:SetScale(s, s)
        _bg:SetColor(lit, lit, lit)
        _bg:DrawAtAnchor(x, y, "topleft")
        _bg:SetScale(1, 1)
        _bg:SetColor(1, 1, 1)
    end
    view.drawPlaced(_bg, _bgMeta, lit, lit, lit, 1)
end

function M.draw(ox, oy)
    ox, oy = view.shake(ox, oy)
    local lit = 1 - BG_DIM * darkness()
    drawBackdrop(ox, oy, lit)
    air.drawBack(ox, oy, lit)
    air.drawMid(ox, oy, lit)
    drawSnake()
    if _smoke ~= nil then _smoke:draw(ox, oy, 1, 1, 1, 1) end
    if _trail ~= nil then _trail:draw(ox, oy) end
    air.drawFront(ox, oy, lit)
end

function M.close()
    freeSounds()
    air.close()
    if _smoke ~= nil then _smoke:clear() end
    if _trail ~= nil then _trail:clear() end
    reset()
    _bg, _bgMeta, _guard, _guardMeta, _reflect, _reflectMeta, _monster, _monsterMeta = nil, nil, nil, nil, nil, nil, nil, nil
    _idle, _pts = nil, {}
end

return M
