---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/particles.lua — ambient particle emitters: engine-defined presets (fire, embers, smoke,
-- leaves, fireflies, fountain_jet, splash, snow, rain-area) driven by map defs or added directly.
--
--   world.particles:add{ preset = "fire", x = 4, y = 1.2, z = 7, rate = 40 }
--   params (per schema): speed, size, life, gravity, spread, rateScale
--
-- Continuous presets emit `rate` particles/sec (fractional accumulation); one-shots (splash) use
-- burst(). Fireflies auto-dim by day when a daynight cycle is present.

local sin, random, floor, max, min = math.sin, math.random, math.floor, math.max, math.min

local SPR_SOFT, SPR_CRYSTAL = 910, 911

local Particles = {}
Particles.__index = Particles

function Particles.new(world)
    local self = setmetatable({}, Particles)
    self.world = world
    self.emitters = {}
    self.t = 0
    local scene = world.scene
    self.enabled = scene.NewParticleSystem ~= nil
    if self.enabled then
        self.ps = scene:NewParticleSystem()
        if scene.PsSetCap then scene:PsSetCap(self.ps, 5000) end
        if scene.MakeSoftCircle then scene:MakeSoftCircle(SPR_SOFT, 32, 2.0) end
        if scene.MakeCrystal then scene:MakeCrystal(SPR_CRYSTAL, 24) end
    end
    return self
end

-- ── presets: emit ONE particle at (x,y,z) with the emitter's params ────────────────────────────────
-- p = params (speed/size/life/gravity/spread), s = scene, ps = system.
-- Each preset also has a SETUP entry (sprite / spin / size+fade curves) applied once per batch —
-- the v2 punch: flames bloom then die, embers flash and drift, petals and leaves actually tumble.
local PRESETS = {}
-- setup: { sprite = SPR_*, rot = startDeg (-1 random), rotVel = deg/s, size = curve, fade = curve }
local SETUP = {
    fire       = { rot = -1, rotVel = 40,  size = 2, fade = 2 },
    embers     = { rot = -1, rotVel = 0,   size = 1, fade = 2 },
    smoke      = { rot = -1, rotVel = 25,  size = 0, fade = 1 },
    leaves     = { rot = -1, rotVel = 180, size = 0, fade = 1 },
    fireflies  = { rot = 0,  rotVel = 0,   size = 0, fade = 2 },
    sakura     = { rot = -1, rotVel = 120, size = 0, fade = 1 },
    snow       = { rot = -1, rotVel = 90,  size = 0, fade = 1, crystal = true },
    splash     = { rot = -1, rotVel = 0,   size = 2, fade = 1 },
    fountain_jet = { rot = 0, rotVel = 0,  size = 0, fade = 1 },
    rain       = { rot = 0,  rotVel = 0,   size = 0, fade = 0 },
}

PRESETS.fire = function(s, ps, x, y, z, p)
    local sp = (p.speed or 1.5) * (0.6 + random() * 0.8)
    local a = random() * 6.2832
    local rr = (p.spread or 0.22) * random()
    s:PsEmit(ps, x + math.cos(a) * rr, y, z + sin(a) * rr,
        (random() - 0.5) * 0.35, sp, (random() - 0.5) * 0.35,
        255, 130 + random() * 80, 36, 0.9, (p.size or 0.55), 0.10,
        (p.life or 1.35) * (0.8 + random() * 0.45), 0.35, 1.1, 1)
end

PRESETS.embers = function(s, ps, x, y, z, p)
    local a = random() * 6.2832
    local rr = (p.spread or 0.5) * random()
    s:PsEmit(ps, x + math.cos(a) * rr, y + random() * 0.3, z + sin(a) * rr,
        (random() - 0.5) * 0.7, (p.speed or 0.9) * (0.5 + random()), (random() - 0.5) * 0.7,
        255, 150 + random() * 70, 46, 0.95, (p.size or 0.16), 0.03,
        (p.life or 2.2) * (0.8 + random() * 0.5), -0.22, 0.5, 1)
end

PRESETS.smoke = function(s, ps, x, y, z, p)
    s:PsEmit(ps, x + (random() - 0.5) * 0.35, y, z + (random() - 0.5) * 0.35,
        (random() - 0.5) * 0.3, (p.speed or 0.6) * (0.6 + random() * 0.5), (random() - 0.5) * 0.3,
        105, 105, 112, 0.25, (p.size or 0.5), (p.size or 0.5) * 3.6,
        (p.life or 4.0) * (0.75 + random() * 0.5), -0.05, 1.0, 0)
end

PRESETS.leaves = function(s, ps, x, y, z, p)
    local g = 90 + random() * 90
    s:PsEmit(ps, x + (random() - 0.5) * (p.spread or 3.0), y + random() * 2, z + (random() - 0.5) * (p.spread or 3.0),
        (random() - 0.5) * 1.2, -((p.speed or 0.6) * (0.4 + random() * 0.5)), (random() - 0.5) * 1.2,
        70 + random() * 60, g, 30, 0.95, (p.size or 0.28), (p.size or 0.28) * 0.9,
        (p.life or 5.0) * (0.8 + random() * 0.4), 0.06, 0.6, 0)
end

PRESETS.fireflies = function(s, ps, x, y, z, p)
    s:PsEmit(ps, x + (random() - 0.5) * (p.spread or 4.0), y + 0.4 + random() * 1.4, z + (random() - 0.5) * (p.spread or 4.0),
        (random() - 0.5) * 0.5, (random() - 0.3) * 0.35, (random() - 0.5) * 0.5,
        190, 255, 110, 0.95, (p.size or 0.14), 0.05,
        (p.life or 4.0) * (0.75 + random() * 0.5), -0.02, 1.4, 1)
end

PRESETS.fountain_jet = function(s, ps, x, y, z, p)
    local a = random() * 6.2832
    local rr = 0.05 * random()
    s:PsEmit(ps, x + math.cos(a) * rr, y, z + sin(a) * rr,
        (random() - 0.5) * 0.65, (p.speed or 3.2) * (0.85 + random() * 0.3), (random() - 0.5) * 0.65,
        185, 215, 240, 0.6, (p.size or 0.11), 0.05,
        (p.life or 1.1), (p.gravity or 5.2), 0.15, 0)
end

PRESETS.snow = function(s, ps, x, y, z, p)
    s:PsEmit(ps, x + (random() - 0.5) * (p.spread or 12), y + 6 + random() * 4, z + (random() - 0.5) * (p.spread or 12),
        (random() - 0.5) * 0.5, -((p.speed or 1.1) * (0.7 + random() * 0.5)), (random() - 0.5) * 0.5,
        235, 242, 255, 0.9, (p.size or 0.12), (p.size or 0.12),
        (p.life or 8.0), 0, 0.4, 0)
end

PRESETS.sakura = function(s, ps, x, y, z, p)
    -- pink petals tumbling down with sideways drift
    s:PsEmit(ps, x + (random() - 0.5) * (p.spread or 10), y + 3 + random() * 3, z + (random() - 0.5) * (p.spread or 10),
        (random() - 0.2) * 1.4, -((p.speed or 0.55) * (0.5 + random() * 0.6)), (random() - 0.5) * 1.4,
        248, 178 + random() * 30, 198 + random() * 25, 0.95, (p.size or 0.16), (p.size or 0.16) * 0.85,
        (p.life or 6.5) * (0.8 + random() * 0.4), 0.03, 0.9, 0)
end

PRESETS.splash = function(s, ps, x, y, z, p)
    -- one droplet of a splash ring (use burst(...,"splash", 8-14) for the full crown)
    local a = random() * 6.2832
    local sp = (p.speed or 2.4) * (0.6 + random() * 0.7)
    s:PsEmit(ps, x, y + 0.05, z,
        math.cos(a) * sp * 0.7, sp, sin(a) * sp * 0.7,
        200, 225, 245, 0.7, (p.size or 0.14), 0.28,
        (p.life or 0.38), (p.gravity or 9.0), 0.2, 0)
end

PRESETS.rain = function(s, ps, x, y, z, p)
    -- an area shower fixed to the emitter (weather.lua does the player-following version)
    local fall = (p.speed or 15)
    s:PsEmit(ps, x + (random() - 0.5) * (p.spread or 8), y + 6 + random() * 3, z + (random() - 0.5) * (p.spread or 8),
        0.8, -fall, 0.4, 175, 195, 225, 0.4, (p.size or 0.22), (p.size or 0.22),
        (p.life or 0.6), 0, 0, 0)
end

Particles.PRESETS = PRESETS

-- apply a preset's batch setup (sprite + spin + curves); guarded so older engine builds just
-- get the flat look instead of erroring
local function applySetup(scene, ps, preset)
    local su = SETUP[preset] or SETUP.fire
    scene:PsSetSprite(ps, su.crystal and SPR_CRYSTAL or SPR_SOFT)
    if scene.PsSetNextRotation then scene:PsSetNextRotation(ps, su.rot, su.rotVel) end
    if scene.PsSetNextCurves then scene:PsSetNextCurves(ps, su.size, su.fade) end
end
Particles.applySetup = applySetup

-- ── emitters ───────────────────────────────────────────────────────────────────────────────────────
-- e: { preset, x, y, z, rate, params }
function Particles:add(e)
    local em = {
        preset = e.preset or "embers",
        x = e.x or 0, y = e.y or 0, z = e.z or 0,
        rate = (e.rate or 20) * ((e.params and e.params.rateScale) or 1),
        params = e.params or {},
        acc = 0, active = e.active ~= false,
    }
    self.emitters[#self.emitters + 1] = em
    return em
end

function Particles:burst(preset, x, y, z, count, params)
    if not self.enabled then return end
    local fn = PRESETS[preset]; if fn == nil then return end
    local scene = self.world.scene
    applySetup(scene, self.ps, preset)
    for _ = 1, count or 10 do fn(scene, self.ps, x, y, z, params or {}) end
end

function Particles:update(dt)
    if not self.enabled then return end
    local scene = self.world.scene
    self.t = self.t + dt
    -- fireflies only glow at night when a day/night cycle is running
    local nightF = 1
    local dn = self.world.daynight
    if dn and dn.nightFactor then nightF = dn.nightFactor(dn.hour) end
    for _, em in ipairs(self.emitters) do
        if em.active then
            local rate = em.rate
            if em.preset == "fireflies" then rate = rate * nightF end
            if rate > 0 then
                em.acc = em.acc + rate * dt
                local n = floor(em.acc)
                if n > 0 then
                    em.acc = em.acc - n
                    if n > 40 then n = 40 end
                    local fn = PRESETS[em.preset]
                    if fn ~= nil then
                        applySetup(scene, self.ps, em.preset)
                        for _ = 1, n do fn(scene, self.ps, em.x, em.y, em.z, em.params) end
                    end
                end
            end
        end
    end
    scene:PsUpdate(self.ps, dt)
end

function Particles:remove(em)
    for i = #self.emitters, 1, -1 do
        if self.emitters[i] == em then table.remove(self.emitters, i) end
    end
end

function Particles:clear()
    self.emitters = {}
    if self.enabled then self.world.scene:PsClear(self.ps) end
end

function Particles:fromDefs(defs)
    for _, d in ipairs(defs or {}) do
        self:add{ preset = d.preset, x = d.x, y = d.y, z = d.z, rate = d.rate, params = d.params }
    end
end

return Particles
