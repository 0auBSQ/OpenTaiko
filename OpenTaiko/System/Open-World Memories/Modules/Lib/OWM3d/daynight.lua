---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/daynight.lua — the day/night cycle: animated sun (direction + colour), ambient, colour
-- grade, and the map's scheduled point lights (nightOnly lamps come on at dusk, flicker jitters
-- torches/fires). Owns the scene light list while active; drawSky (core) follows world.hour.
--
-- Hour source: world.hour when the map pins it (sky.mode "fixed"), else real local time; call
-- enableCycle(secondsPerDay) for an animated cycle that wraps 24h.

local sin, cos, pi, max, min = math.sin, math.cos, math.pi, math.max, math.min

local DayNight = {}
DayNight.__index = DayNight

function DayNight.new(world)
    local self = setmetatable({}, DayNight)
    self.world = world
    self.hour = world.hour or 12
    self.cycleSpeed = 0            -- game-hours per real second (0 = follow world.hour / clock)
    self.lightDefs = nil           -- the current map's light definitions
    self._lightsDirty = true
    self._nightF = -1              -- last applied night factor (rebuild lights when it crosses)
    self._flicker = false
    self._t = 0
    return self
end

function DayNight:setHour(h) self.hour = h % 24; self.world.hour = self.hour; self._lightsDirty = true end
function DayNight:enableCycle(secondsPerDay)
    self.cycleSpeed = (secondsPerDay and secondsPerDay > 0) and (24 / secondsPerDay) or 0
end

-- adopt a map's light list (maps.lua calls this on load; nil on unload)
function DayNight:setLights(defs)
    self.lightDefs = defs
    self._flicker = false
    for _, l in ipairs(defs or {}) do if (l.flicker or 0) > 0 then self._flicker = true end end
    self._lightsDirty = true
end

-- 0 = full day, 1 = full night (smooth dawn/dusk ramps)
local function nightFactor(hour)
    if hour >= 6 and hour <= 19 then
        local d = sin(pi * (hour - 6) / 13)
        return 1 - min(1, d * 3.2)          -- short ramps at the edges, 0 through the day
    end
    return 1
end
DayNight.nightFactor = nightFactor

function DayNight:update(dt)
    local world = self.world
    self._t = self._t + dt
    if self.cycleSpeed > 0 then
        self.hour = (self.hour + dt * self.cycleSpeed) % 24
        world.hour = self.hour
    elseif world.hour ~= nil then
        self.hour = world.hour
    else
        local t = os.date("*t")
        self.hour = t.hour + t.min / 60
        world.hour = self.hour
    end
    local hour = self.hour
    local scene = world.scene

    local daylight = 0
    if hour >= 6 and hour <= 19 then daylight = max(0, sin(pi * (hour - 6) / 13)) end
    local nightF = nightFactor(hour)

    -- ── sun / moon ──
    -- azimuth swings east→west through the day; the moon reuses the arc, dim and blue
    local prog = (hour >= 6 and hour <= 19) and (hour - 6) / 13 or ((hour > 19 and hour - 19 or hour + 5) / 11)
    local yawDeg = 55 + prog * 110                       -- stays oblique so shadows keep shape
    local elDeg = 14 + ((daylight > 0) and daylight or 0.35) * 48
    local yr, el = yawDeg * pi / 180, elDeg * pi / 180
    local dx, dz = sin(yr), cos(yr)
    -- sun COLOUR from the shared palette keyframes (the same Catmull-Rom ring the sky uses), so
    -- the light warms/cools smoothly through the whole day instead of the old binary "warm" step
    local sr, sg, sb
    if daylight > 0 then
        local pal = require("OWM3d.sky").paletteAt(hour)
        local dim = (0.25 + 0.75 * min(1, daylight * 2.5)) * 0.92
        sr, sg, sb = pal.sun[1] * dim, pal.sun[2] * dim * 0.92, pal.sun[3] * dim * 0.82
    else
        sr, sg, sb = 0.13, 0.15, 0.24                    -- moonlight (keeps the shadow map alive)
    end
    local ca = cos(el)
    scene:SetSun(-dx * ca, sin(el), -dz * ca, sr, sg, sb)
    world.lightDx, world.lightDz = dx, dz
    world.lightInvTan = 1.0 / max(0.18, math.tan(el))
    scene:SetGroundSpriteLight(dx, dz, min(world.lightInvTan, 3.2))
    if world.shadowObj then scene:ObjSetTint(world.shadowObj, 0, 0, 0) end

    -- feed the procedural GPU sky (core.World:render pushes the camera-dependent uniforms)
    self.sunX, self.sunY, self.sunZ = -dx * ca, sin(el), -dz * ca
    self.dayF = min(1, daylight * 2.2)
    self.nightF = nightF
    if not self._skyInstalled and scene.SetSkyShader then
        self._skyInstalled = true
        require("OWM3d.sky").install(scene)
    end

    -- ── ambient: tinted by the sky's zenith (bounce light follows the dome's hue) ──
    local zen = require("OWM3d.sky").paletteAt(hour).zen
    local ar = (0.42 - 0.25 * nightF) * (0.85 + 0.5 * zen[1])
    local ag = (0.42 - 0.24 * nightF) * (0.85 + 0.4 * zen[2])
    local ab = (0.47 - 0.20 * nightF) * (0.85 + 0.35 * zen[3])
    scene:SetAmbient(ar, ag, ab)

    -- ── colour grade (dusk warmth, night blues); weather may darken it further ──
    if scene.SetColorGrade then
        local duskW = 0
        if daylight > 0.03 and daylight < 0.45 then duskW = 1 - math.abs(daylight - 0.24) / 0.21 end
        duskW = max(0, min(1, duskW))
        local exp = 1.0 - 0.22 * nightF
        local gr = 1.0 + 0.10 * duskW - 0.16 * nightF
        local gg = 1.0 - 0.02 * duskW - 0.09 * nightF
        local gb = 1.0 - 0.12 * duskW + 0.13 * nightF
        local weather = world.weather
        if weather and weather.applyGrade then exp, gr, gg, gb = weather:applyGrade(exp, gr, gg, gb) end
        scene:SetColorGrade(exp, gr, gg, gb)
    end

    -- ── scheduled point lights ──
    local lampsOn = nightF > 0.35
    if self._lightsDirty or self._flicker or (lampsOn ~= self._lampsOn) then
        self._lightsDirty = false
        self._lampsOn = lampsOn
        scene:ClearLights()
        for i, l in ipairs(self.lightDefs or {}) do
            if not l.nightOnly or lampsOn then
                local it = l.intensity or 1
                local fl = l.flicker or 0
                if fl > 0 then
                    local w = sin(self._t * 9.1 + i * 2.3) * 0.6 + sin(self._t * 23.7 + i * 5.1) * 0.4
                    it = it * (1 + fl * 0.35 * w)
                end
                scene:AddLightRanged(l.x, l.y, l.z, l.r, l.g, l.b, it, l.range or 6)
            end
        end
    end
    self._nightF = nightF
end

return DayNight
