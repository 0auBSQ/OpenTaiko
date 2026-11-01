-- Asagao's Gift effect: reveals what the song bars hide (encounter/songbars.reveal).

local Easing   = require("Easing")
local assets   = require("encounter/assets")
local songbars = require("encounter/songbars")
local Config   = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.items.charm
local Anim = Animation.encounter.items.charm
local Drop, BarFlash, Splash = Look.drop, Look.bar_flash, Anim.splash

local IMPACT_X, IMPACT_Y = Look.impact.x, Look.impact.y
local DIM_A, DIM_IN, DIM_OUT = Look.dim_opacity, Anim.dim_in_s, Anim.dim_out_s
local DROP_AT, FALL_SEC, DROP_FROM_Y = Anim.drop_at_s, Anim.fall_s, Anim.drop_from_y
local DROP_SCALE, DROP_STRETCH = Drop.scale, Anim.drop_stretch
local RINGS = {}                                  -- delay after the splash (s), opacity, speed (px/s)
for i, r in ipairs(Anim.rings) do RINGS[i] = { r.delay_s, r.opacity, r.speed_px_per_s } end
local RING_LIFE, RING_R, RING_SQUASH = Anim.ring_life_s, 246, Look.ring_squash
local SPLASH_N, SPLASH_SPEED, SPLASH_SPREAD = Splash.count, Splash.speed, Splash.spread_deg   -- spread: deg either side of straight up
local SPLASH_GRAVITY, SPLASH_LIFE, SPLASH_SIZE = Splash.gravity, Splash.life_s, Splash.size
local FLASH_SEC, FLASH_SCALE, FLASH_A = Anim.flash_s, Look.flash_scale, Look.flash_opacity
local BAR_FLASH_SEC, BAR_W, BAR_H, BAR_GLINTS = Anim.bar_flash_s, BarFlash.w, BarFlash.h, BarFlash.glints
local AFTER_SEC = Anim.after_s                    -- s after the last reveal before the menu is back
local WATER = Look.water_color
local SFX = { drop = "Sounds/result/item_drop.ogg", reveal = "Sounds/result/item_reveal.ogg",
    lift = "Sounds/result/dissolve.ogg" }

local _tex = {}
local _sfx = {}
local _on = false
local _t = 0
local _impact = DROP_AT + FALL_SEC
local _reveals = {}                               -- { at, bar, done }
local _flashes = {}                               -- { t, bar }
local _drops = {}
local _splashed, _rang = false, false
local _endAt = 0

local random = math.random

local function rnd(r) return r[1] + random() * (r[2] - r[1]) end

local function ready(tx) return tx ~= nil and tx.Ready end

local function reset(tx)
    tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
    tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

function M.keys() return { "items_drop", "items_ring", "fx_glow", "fx_mote", "fx_glint" } end

local function sound(path)
    local ok, s = pcall(function() return SOUND:CreateSFX(path) end)
    if ok then return s end
    return nil
end

local function play(name)
    local s = _sfx[name]
    if s ~= nil then s:Play() end
end

-- grabs the visit's handles and sounds (encounter/assets opens the keys)
function M.open()
    M.close()
    for _, k in ipairs(M.keys()) do _tex[k] = (assets.get(k)) end
    for n, p in pairs(SFX) do _sfx[n] = sound(p) end
end

-- starts the effect; each hiding bar is revealed when the first ring reaches it
function M.start()
    _on, _t = true, 0
    _splashed, _rang = false, false
    _reveals, _flashes, _drops = {}, {}, {}
    local last = _impact
    for _, h in ipairs(songbars.hiddenBars()) do
        local d = math.sqrt((h.x - IMPACT_X) ^ 2 + (h.y - IMPACT_Y) ^ 2)
        local at = _impact + d / RINGS[1][3]
        _reveals[#_reveals + 1] = { at = at, bar = h.bar }
        last = math.max(last, at)
    end
    _endAt = last + AFTER_SEC
end

-- true while the menu waits for it
function M.busy()
    return _on and (_t < _endAt or not songbars.settled())
end

-- true while any of it is on screen
function M.active() return _on end

local function splash()
    _splashed = true
    play("drop")
    for _ = 1, SPLASH_N do
        local a = math.rad(-90 + (random() * 2 - 1) * SPLASH_SPREAD)
        local v = rnd(SPLASH_SPEED)
        _drops[#_drops + 1] = { x = IMPACT_X, y = IMPACT_Y, vx = math.cos(a) * v, vy = math.sin(a) * v,
            age = 0, life = rnd(SPLASH_LIFE), s = rnd(SPLASH_SIZE) }
    end
end

function M.update(dt)
    if not _on then return end
    _t = _t + dt
    if not _splashed and _t >= _impact then splash() end
    for _, r in ipairs(_reveals) do
        if not r.done and _t >= r.at then
            r.done = true
            local kind = songbars.reveal(r.bar)
            if kind ~= nil then
                if not _rang then _rang = true ; play("reveal") end
                if kind == "lift" then play("lift") end
                _flashes[#_flashes + 1] = { t = 0, bar = r.bar }
            end
        end
    end
    for _, f in ipairs(_flashes) do f.t = f.t + dt end
    local i = 1
    while i <= #_drops do
        local p = _drops[i]
        p.age = p.age + dt
        if p.age >= p.life then table.remove(_drops, i)
        else
            p.vy = p.vy + SPLASH_GRAVITY * dt
            p.x, p.y = p.x + p.vx * dt, p.y + p.vy * dt
            i = i + 1
        end
    end
    local over = _impact + RINGS[#RINGS][1] + RING_LIFE
    if _t >= math.max(over, _endAt + DIM_OUT) and not M.busy() then _on = false end
end

local function dimAlpha()
    local a = DIM_A * Easing.outQuad(_t / DIM_IN)
    if _t > _endAt then a = a * (1 - Easing.inOutQuad((_t - _endAt) / DIM_OUT)) end
    return a
end

-- drawn under the song bars; black: the stage's black texture
function M.drawUnder(black)
    if not _on or not ready(black) then return end
    local a = dimAlpha()
    if a <= 0 then return end
    local res = THEME:GetResolution()
    black:SetScale(res.X / math.max(1, black.Width), res.Y / math.max(1, black.Height))
    black:SetOpacity(a)
    black:Draw(0, 0)
    reset(black)
end

local function drawAt(tx, x, y, sx, sy, a, c, blend, rot)
    if not ready(tx) or a <= 0.003 then return end
    tx:SetScale(sx, sy)
    if rot ~= nil then tx:SetRotation(rot) end
    tx:SetOpacity(math.min(1, a))
    if c ~= nil then tx:SetColor(c[1], c[2], c[3]) end
    if blend ~= nil then tx:SetBlendMode(blend) end
    tx:DrawAtAnchor(x, y, "center")
    reset(tx)
end

-- drawn over the song bars
function M.drawOver()
    if not _on then return end
    local glow, ring, mote, glint = _tex.fx_glow, _tex.items_ring, _tex.fx_mote, _tex.fx_glint
    if _t >= DROP_AT and _t < _impact then
        local u = (_t - DROP_AT) / FALL_SEC
        local y = DROP_FROM_Y + (IMPACT_Y - DROP_FROM_Y) * Easing.inQuad(u)
        local s = DROP_SCALE
        drawAt(glow, IMPACT_X, y - Anim.trail_rise_px * u, Drop.trail_scale_x, Anim.trail_grow_y * u + Drop.trail_scale_y,
            Drop.trail_opacity * u, WATER, "Add")
        drawAt(glow, IMPACT_X, y, Drop.glow_scale_x, Drop.glow_scale_y, Drop.glow_opacity, WATER, "Add")
        drawAt(_tex.items_drop, IMPACT_X, y, s * (1 - Anim.drop_thin * u), s * (1 + DROP_STRETCH * u), 1)
    end
    if _t >= _impact then
        local since = _t - _impact
        if since < FLASH_SEC then
            local f = 1 - since / FLASH_SEC
            local fs = FLASH_SCALE * (Anim.flash_end_scale - Anim.flash_grow * f)
            drawAt(glow, IMPACT_X, IMPACT_Y, fs, fs,
                FLASH_A * f * f, WATER, "Add")
        end
        for _, r in ipairs(RINGS) do
            local age = since - r[1]
            if age > 0 and age < RING_LIFE then
                local rad = r[3] * age * (1 - Anim.ring_slow * age / RING_LIFE)
                local fade = (1 - age / RING_LIFE) ^ Anim.ring_fade_power
                drawAt(ring, IMPACT_X, IMPACT_Y, rad / RING_R, rad * RING_SQUASH / RING_R, r[2] * fade, WATER, "Add")
            end
        end
        for _, p in ipairs(_drops) do
            local u = p.age / p.life
            local ps = p.s * (1 - Splash.shrink * u)
            drawAt(mote, p.x, p.y, ps, ps, 1 - u, WATER, "Add")
        end
    end
    for _, f in ipairs(_flashes) do
        if f.t < BAR_FLASH_SEC and f.bar.xf ~= nil then
            local xf = f.bar.xf
            local k = 1 - f.t / BAR_FLASH_SEC
            local gw = (glow ~= nil and glow.Width or BarFlash.glow_w_fallback)
            drawAt(glow, xf.x, xf.y, BAR_W * xf.s / gw, BAR_H * xf.s / gw, BarFlash.glow_opacity * k * k, WATER, "Add", xf.rot)
            for g = 1, BAR_GLINTS do
                local lx = (g - (BAR_GLINTS + 1) / 2) * BarFlash.glint_step_px * xf.s
                local a = math.rad(xf.rot)
                local gx, gy = xf.x + lx * math.cos(a), xf.y - lx * math.sin(a) - Anim.glint_rise_px * (1 - k)
                local gs = BarFlash.glint_scale * k + BarFlash.glint_scale_min
                drawAt(glint, gx, gy, gs, gs, k, WATER, "Add", f.t * Anim.glint_spin_deg_per_s + g * Anim.glint_turn_step_deg)
            end
        end
    end
end

function M.close()
    for _, s in pairs(_sfx) do s:Stop() ; s:Dispose() end
    _sfx, _tex = {}, {}
    _on, _reveals, _flashes, _drops = false, {}, {}, {}
end

return M
