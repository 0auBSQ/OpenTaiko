---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/weather.lua: weather state machine with crossfade

local sin, random, max, min, floor = math.sin, math.random, math.max, math.min, math.floor

local SPR_STREAK, SPR_SPLASH = 905, 906

local Weather = {}
Weather.__index = Weather

-- ==== State table ====
-- emit(self, scene, dt, i, px, py, pz, gy) runs while the state's channel intensity i > 0.01
local STATES = {}

local function emitRain(self, scene, dt, i, px, py, pz, gy, mul, wind)
    local drops = 200 * mul * i * dt
    local nDrops = floor(drops) + ((random() < drops % 1) and 1 or 0)
    scene:PsSetSprite(self.ps, SPR_STREAK)
    if scene.PsSetNextCurves then scene:PsSetNextCurves(self.ps, 0, 0) end
    if scene.PsSetNextRotation then scene:PsSetNextRotation(self.ps, 0, 0) end
    for _ = 1, nDrops do
        -- spawn band: full density to 11 m, fading to 0 at 14 m
        local rr = 11 + random() * 3
        local keep = 1 - max(0, (rr - 11) / 3)
        if random() < keep then
            local a = random() * 6.2832
            local ox = px + math.cos(a) * rr * random()
            local oz = pz + sin(a) * rr * random()
            local top = (py or 0) + 8 + random() * 4
            local g = gy(ox, oz)
            if top < g + 1 then top = g + 6 end
            local fall = 17 + random() * 3
            local life = max(0.05, (top - g) / fall)
            scene:PsEmit(self.ps, ox, top, oz, 1.2 + (wind or 0), -fall, 0.5,
                175, 195, 225, 0.42, 0.30, 0.30, life, 0, 0, 0)
        end
    end
    local sp = 130 * mul * i * dt
    local nSp = floor(sp) + ((random() < sp % 1) and 1 or 0)
    scene:PsSetSprite(self.ps, SPR_SPLASH)
    if scene.PsSetNextCurves then scene:PsSetNextCurves(self.ps, 2, 1) end
    for _ = 1, nSp do
        local ox = px + (random() * 2 - 1) * 10
        local oz = pz + (random() * 2 - 1) * 10
        local g = gy(ox, oz)
        scene:PsEmit(self.ps, ox, g + 0.03, oz, 0, 0.3, 0,
            190, 210, 235, 0.4, 0.05, 0.40, 0.24, 0, 2.0, 0)
    end
end

STATES.clear = { wet = 0, fogScale = 1, gradeDim = 0,
    emit = function() end }

STATES.rain = { wet = 1, fogScale = 0.6, gradeDim = 1,
    emit = function(self, scene, dt, i, px, py, pz, gy)
        emitRain(self, scene, dt, i, px, py, pz, gy, 1, 0)
    end }

STATES.storm = { wet = 1, fogScale = 0.45, gradeDim = 1.6,
    emit = function(self, scene, dt, i, px, py, pz, gy)
        emitRain(self, scene, dt, i, px, py, pz, gy, 1.8, 2.5 * sin(self.t * 0.7))
        -- lightning: 0.12 s grade pulse and world.onThunder
        if self._flash > 0 then self._flash = self._flash - dt end
        if random() < dt * 0.15 * i then
            self._flash = 0.12
            if self.world.onThunder then pcall(self.world.onThunder) end
        end
    end }

STATES.snow = { wet = 0.15, fogScale = 0.7, gradeDim = 0.3,
    emit = function(self, scene, dt, i, px, py, pz, gy)
        local flakes = 60 * i * dt
        local n = floor(flakes) + ((random() < flakes % 1) and 1 or 0)
        if n > 0 then
            local P = require("World.OWM3d.particles")
            P.applySetup(scene, self.ps, "snow")
            for _ = 1, n do
                P.PRESETS.snow(scene, self.ps, px, (py or 0), pz, { spread = 24 })
            end
        end
    end }

STATES.sakura = { wet = 0, fogScale = 1, gradeDim = 0,
    emit = function(self, scene, dt, i, px, py, pz, gy)
        local petals = 26 * i * dt
        local n = floor(petals) + ((random() < petals % 1) and 1 or 0)
        if n > 0 then
            local P = require("World.OWM3d.particles")
            P.applySetup(scene, self.ps, "sakura")
            for _ = 1, n do
                P.PRESETS.sakura(scene, self.ps, px, (py or 0) + 2, pz, { spread = 22 })
            end
        end
    end }

STATES.embers = { wet = 0, fogScale = 0.85, gradeDim = 0.4,
    emit = function(self, scene, dt, i, px, py, pz, gy)
        local n0 = 22 * i * dt
        local n = floor(n0) + ((random() < n0 % 1) and 1 or 0)
        if n > 0 then
            local P = require("World.OWM3d.particles")
            P.applySetup(scene, self.ps, "embers")
            for _ = 1, n do
                P.PRESETS.embers(scene, self.ps,
                    px + (random() * 2 - 1) * 14, (py or 0) + random() * 5, pz + (random() * 2 - 1) * 14,
                    { spread = 2, speed = 0.6, life = 3.5 })
            end
        end
    end }

Weather.STATES = STATES

-- ==== Construction ====
function Weather.new(world)
    local self = setmetatable({}, Weather)
    self.world = world
    self.mode = "clear"
    self.prevMode = "clear"
    self.intensity = 0      -- current-state channel 0..1
    self.prevI = 0          -- previous-state channel fading out
    self.wet = 0            -- ground wetness 0..1
    self.fade = 2.0
    self.t = 0
    self._flash = 0
    local scene = world.scene
    self.enabled = scene.NewParticleSystem ~= nil
    if self.enabled then
        self.ps = scene:NewParticleSystem()
        if scene.PsSetCap then scene:PsSetCap(self.ps, 6000) end
        if scene.SetSpriteRGBA then
            -- rain streak sprite (8×24)
            local px, w, h = {}, 8, 24
            for y = 0, h - 1 do
                for x = 0, w - 1 do
                    local cx = math.abs(x - (w - 1) * 0.5) / (w * 0.5)
                    local a = max(0, 1 - cx * 2.4) * (0.35 + 0.65 * (y / (h - 1)))
                    local al = floor(a * 255 + 0.5)
                    px[y * w + x + 1] = al * 16777216 + 0xE6EEFA
                end
            end
            scene:SetSpriteRGBA(SPR_STREAK, px, w, h)
            if scene.SetSpriteFilter then scene:SetSpriteFilter(SPR_STREAK, "linear") end
        end
        if scene.MakeSoftCircle then scene:MakeSoftCircle(SPR_SPLASH, 24, 2.2) end
    end
    return self
end

-- ==== State transitions ====
-- fade: seconds (default 2)
function Weather:setWeather(mode, fade)
    if STATES[mode] == nil then
        if mode ~= nil then print("[OWM3d] unknown weather '" .. tostring(mode) .. "' -> clear") end
        mode = "clear"
    end
    if mode == self.mode then
        if fade == 0 then self.intensity = (mode == "clear") and 0 or 1 end
    else
        self.prevMode = self.mode
        self.prevI = self.intensity
        self.mode = mode
        self.intensity = 0
    end
    self.fade = fade or 2.0
    -- fade 0: snap at once (map unload, clear all)
    if fade == 0 then
        self.prevI = 0
        self.intensity = (mode == "clear") and 0 or 1
        self.wet = STATES[mode].wet * self.intensity
        local scene = self.world.scene
        if scene.SetWetness then scene:SetWetness(self.wet) end
        if self.enabled then scene:PsClear(self.ps) end
        self.world.weatherFogScale = 1
        self._flash = 0
    end
end

function Weather:isRaining() return (self.mode == "rain" or self.mode == "storm") and self.intensity > 0.05 end

-- live effects: { { kind, intensity, fading }, ... }
function Weather:activeEffects()
    local fx = {}
    if self.mode ~= "clear" and self.intensity > 0.01 then
        fx[#fx + 1] = { kind = self.mode, intensity = self.intensity }
    end
    if self.prevMode ~= "clear" and self.prevI > 0.01 then
        fx[#fx + 1] = { kind = self.prevMode, intensity = self.prevI, fading = true }
    end
    return fx
end

-- weather dim on the colour grade (called from daynight:update)
function Weather:applyGrade(exp, r, g, b)
    local dim = (STATES[self.mode].gradeDim or 0) * self.intensity
        + (STATES[self.prevMode].gradeDim or 0) * self.prevI
    if self._flash > 0 then
        -- lightning flash overrides the dim
        return exp * 1.9, r * 1.05, g * 1.05, b * 1.15
    end
    if dim <= 0 then return exp, r, g, b end
    dim = min(dim, 1.6)
    return exp * (1 - 0.125 * dim), r * (1 - 0.0625 * dim), g * (1 - 0.044 * dim), b
end

-- px, py, pz: player focus; groundYFn(wx, wz) -> ground height (optional)
function Weather:update(dt, px, py, pz, groundYFn)
    local world = self.world
    self.t = self.t + dt
    local step = dt / max(0.05, self.fade)
    local target = (self.mode == "clear") and 0 or 1
    if self.intensity < target then self.intensity = min(target, self.intensity + step)
    elseif self.intensity > target then self.intensity = max(target, self.intensity - step * 1.6) end
    if self.prevI > 0 then self.prevI = max(0, self.prevI - step * 1.6) end

    -- wetness: rise 0.5/s, dry 0.045/s
    local wetTarget = STATES[self.mode].wet * self.intensity + STATES[self.prevMode].wet * self.prevI
    if wetTarget > 1 then wetTarget = 1 end
    if self.wet < wetTarget then self.wet = min(wetTarget, self.wet + dt * 0.5)
    elseif self.wet > wetTarget then self.wet = max(wetTarget, self.wet - dt * 0.045) end
    local scene = world.scene
    if scene.SetWetness then scene:SetWetness(self.wet) end
    local fs = STATES[self.mode].fogScale * self.intensity + 1 * (1 - self.intensity)
    if self.prevI > 0 then fs = min(fs, STATES[self.prevMode].fogScale * self.prevI + 1 * (1 - self.prevI)) end
    world.weatherFogScale = fs

    if not self.enabled then return end
    if px ~= nil then
        local gy = function(wx, wz)
            if groundYFn then return groundYFn(wx, wz) end
            local map = world.maps.current
            if map and map.heightAt then return map:heightAt(wx, wz) end
            return 0
        end
        if self.intensity > 0.01 then
            STATES[self.mode].emit(self, scene, dt, self.intensity, px, py, pz, gy)
        end
        if self.prevI > 0.01 then
            STATES[self.prevMode].emit(self, scene, dt, self.prevI, px, py, pz, gy)
        end
    end
    scene:PsUpdate(self.ps, dt)
end

return Weather
