---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/core.lua — the World: a Lua3DScene, its layer objects, and the frame pipeline that every
-- subsystem (camera / physics / maps / models / triggers / actors) hangs off.
--
-- OWM3d is the story-mode successor to Lib/isoengine.lua (which stays frozen for its remaining
-- consumers). Same cell→world convention: cell (c,r) on a W×H grid maps to world x = c..c+1,
-- z = (gridH-1-r)..(gridH-r); row 0 is the FAR side; 1 cell = 1 world unit; +Y is up.
--
--   local OWM = require("OWM3d")
--   local world = OWM.World.new{ rw = 1280, rh = 720, screenW = 1920, screenH = 1080, lit = true }
--   world:registerMap("plaza", { type = "folder", dir = "maps/plaza" })
--   world:loadMap("plaza", "default")
--   -- per frame: world:update(dt) ; world:render() ; world:blit()

local floor, sin, cos, pi = math.floor, math.sin, math.cos, math.pi

local Camera    = require("OWM3d.camera")
local Phys      = require("OWM3d.physics")
local Maps      = require("OWM3d.maps")
local Models    = require("OWM3d.models")
local Triggers  = require("OWM3d.triggers")
local Actors    = require("OWM3d.actors")
local DayNight  = require("OWM3d.daynight")
local Weather   = require("OWM3d.weather")
local Water     = require("OWM3d.water")
local Particles = require("OWM3d.particles")

local World = {}
World.__index = World

-- ── day/night sky (hour → gradient); daynight.lua takes over in the visuals phase ─────────────────
local function lerp(a, b, t) return a + (b - a) * t end
local function lerpRGB(a, b, t) return { lerp(a[1], b[1], t), lerp(a[2], b[2], t), lerp(a[3], b[3], t) } end
local SKY_KEYS = {
    { 0,  { 6, 8, 22 },    { 18, 16, 40 } },
    { 5,  { 14, 16, 40 },  { 60, 44, 70 } },
    { 7,  { 80, 110, 180 },{ 240, 150, 110 } },
    { 10, { 90, 140, 220 },{ 170, 200, 235 } },
    { 13, { 95, 150, 230 },{ 180, 205, 240 } },
    { 17, { 90, 130, 210 },{ 210, 180, 160 } },
    { 19, { 60, 60, 120 }, { 240, 130, 80 } },
    { 21, { 20, 22, 55 },  { 70, 50, 90 } },
    { 24, { 6, 8, 22 },    { 18, 16, 40 } },
}
local function skyAt(hour)
    for i = 1, #SKY_KEYS - 1 do
        local a, b = SKY_KEYS[i], SKY_KEYS[i + 1]
        if hour >= a[1] and hour <= b[1] then
            local t = (b[1] - a[1] > 0) and (hour - a[1]) / (b[1] - a[1]) or 0
            return lerpRGB(a[2], b[2], t), lerpRGB(a[3], b[3], t)
        end
    end
    return SKY_KEYS[1][2], SKY_KEYS[1][3]
end
World.skyAt = skyAt   -- exposed for daynight.lua / harnesses

-- ── construction ──────────────────────────────────────────────────────────────────────────────────
function World.new(o)
    o = o or {}
    local self = setmetatable({}, World)
    self.RW, self.RH = o.rw or 1280, o.rh or 720
    self.SW, self.SH = o.screenW or 1920, o.screenH or 1080
    self.gridW, self.gridH = o.gridW or 7, o.gridH or 7
    self.lit = o.lit ~= false

    local scene = SCENE3D:CreateScene(self.RW, self.RH)
    scene:SetMode("raster"); scene:SetLighting(self.lit); scene:SetThreads(8)
    if self.lit then
        scene:SetAmbient(0.40, 0.40, 0.46)
        scene:SetShadows(true)
    end
    scene:SetCameraNear(0.05)
    scene:SetFog(false, 0, 0, 0, 0, 0)
    self.scene = scene

    self.diorama = nil
    self.fogOn, self.fogNear, self.fogFar = true, 22, 120
    self.hour = o.hour                       -- nil = real local time

    -- ── layer objects (same roles as isoengine; models allocate extra objects on demand) ──
    local function obj(litFlag) local id = scene:NewObject(); scene:ObjSetLit(id, litFlag and self.lit or false); return id end
    self.floorObj  = obj(true)
    self.wallObj   = obj(true)
    self.roofObj   = obj(true)               -- separate so interiors can hide roofs
    self.prop3dObj = obj(false)
    self._boxTarget = self.prop3dObj
    self.doorObj   = obj(true)               -- swinging door panels (rebuilt per frame)
    self.gridObj   = obj(false); scene:ObjSetPass(self.gridObj, 1, 255, 255, 255, 42)
    self.ghostObj  = obj(false); scene:ObjSetPass(self.ghostObj, 1, 0, 0, 0, 150)
    self.hiliteObj = obj(false); scene:ObjSetPass(self.hiliteObj, 1, 80, 220, 120, 120)
    self.propObj   = obj(false); scene:ObjSetPass(self.propObj, 0, 0, 0, 0, 255)
    self.actorObj  = obj(false); scene:ObjSetPass(self.actorObj, 0, 0, 0, 0, 255)
    self.npcObj    = obj(false); scene:ObjSetPass(self.npcObj, 0, 0, 0, 0, 255)
    self.shadowObj = obj(false); scene:ObjSetPass(self.shadowObj, 1, 0, 0, 0, 150)
    self.bubbleObj = obj(false); scene:ObjSetPass(self.bubbleObj, 0, 0, 0, 0, 255)
    if scene.ObjSetCastShadow then scene:ObjSetCastShadow(self.bubbleObj, false) end
    self.markerObj = obj(false); scene:ObjSetPass(self.markerObj, 1, 255, 214, 110, 150)
    self.waterObj  = obj(false); scene:ObjSetPass(self.waterObj, 1, 255, 255, 255, 185)

    -- ── subsystems ──
    self.phys      = Phys.new(self)
    self.cam       = Camera.new(self, { yaw = o.yaw, pitch = o.pitch, fov = o.fov, dist = o.dist })
    self.models    = Models.new(self)
    self.triggers  = Triggers.new(self)
    self.actors    = Actors.new(self)
    self.maps      = Maps.new(self)
    self.weather   = Weather.new(self)
    self.water     = Water.new(self)
    self.particles = Particles.new(self)
    self.daynight  = DayNight.new(self)

    -- initial shadow-casting light ≈ camera; daynight:update animates the sun from here on
    if self.lit then self:setLight(self.cam.canonYaw, self.cam.pitch) end
    self.cam:apply()
    return self
end

-- ── coordinates (identical math to isoengine — parity-tested) ─────────────────────────────────────
function World:setGridSize(w, h) self.gridW, self.gridH = w, h end
function World:cellToWorld(c, r) return c + 0.5, (self.gridH - 1 - r) + 0.5 end
function World:worldToCell(wx, wz) return floor(wx), self.gridH - 1 - floor(wz) end
function World:footprintCenter(c, r, w, h) return c + w * 0.5, self.gridH - r - h * 0.5 end

function World:screenToCell(mx, my)
    local sx2, sy2 = self.SW / self.RW, self.SH / self.RH
    local best, bc, br = 1e18, nil, nil
    for r = 0, self.gridH - 1 do
        for c = 0, self.gridW - 1 do
            local wx, wz = self:cellToWorld(c, r)
            local px, py, depth = self.scene:WorldToScreen(wx, 0, wz)
            if depth > 0 then
                local dx, dy = px * sx2 - mx, py * sy2 - my
                local d = dx * dx + dy * dy
                if d < best then best, bc, br = d, c, r end
            end
        end
    end
    if best < (self.SW * 0.06) ^ 2 then return bc, br end
    return nil
end

function World:project(wx, wy, wz)
    local sx, sy, depth = self.scene:WorldToScreen(wx, wy, wz)
    return sx * (self.SW / self.RW), sy * (self.SH / self.RH), depth
end

-- sprite facing from a world move (port of isoengine's live-yaw projection + hysteresis)
local DIRV = { [1] = { -1, -1 }, [2] = { 1, -1 }, [3] = { 1, 1 }, [4] = { -1, 1 } }
function World:facingFromWorld(wx, wz, prev)
    if wx == 0 and wz == 0 then return prev or 2 end
    local yr = self.cam.yaw * pi / 180
    local sx = wx * cos(yr) - wz * sin(yr)
    local sy = wx * sin(yr) + wz * cos(yr)
    local mag = math.sqrt(sx * sx + sy * sy); if mag < 1e-5 then return prev or 2 end
    sx, sy = sx / mag, sy / mag
    if prev and DIRV[prev] then
        local pv = DIRV[prev]
        if (sx * pv[1] + sy * pv[2]) * 0.70710678 > 0.55 then return prev end
    end
    if sy >= 0 then return (sx >= 0) and 3 or 4
    else return (sx >= 0) and 2 or 1 end
end

-- ── shadow light (drives draped sprite shadows + the sun) ─────────────────────────────────────────
function World:setLight(yawDeg, pitchDeg)
    local yr = (yawDeg or self.cam.canonYaw) * pi / 180
    self.lightDx, self.lightDz = sin(yr), cos(yr)
    local el = (-(pitchDeg or self.cam.pitch)) * pi / 180
    self.lightInvTan = 1.0 / math.max(0.18, math.tan(el))
    self.scene:ObjSetTint(self.shadowObj, 0, 0, 0)
    self.scene:SetGroundSpriteLight(self.lightDx, self.lightDz, math.min(self.lightInvTan, 3.2))
    local ca = math.cos(el)
    self.scene:SetSun(-self.lightDx * ca, math.sin(el), -self.lightDz * ca, 0.85, 0.82, 0.72)
end

-- ── post grade / fog ──────────────────────────────────────────────────────────────────────────────
function World:setDiorama(tilt, sat, vig, bloom)
    if tilt == nil then self.diorama = nil
    else self.diorama = { tilt = tilt or 2.2, sat = sat or 1.2, vig = vig or 0.28, bloom = bloom or 0.5 } end
end
function World:setFog(on, near, far)
    self.fogOn = on and true or false
    if near then self.fogNear = near end
    if far then self.fogFar = far end
end
-- thin passthroughs for the Lua-attached post shader (visuals phase; guarded so older builds no-op)
function World:setPostShader(src) if self.scene.SetPostShader then self.scene:SetPostShader(src or "") end end
function World:setPostUniform(slot, a, b, c, d) if self.scene.SetPostUniform then self.scene:SetPostUniform(slot, a or 0, b or 0, c or 0, d or 0) end end

-- ── map registry passthroughs ─────────────────────────────────────────────────────────────────────
function World:registerMap(id, def) self.maps:register(id, def) end
function World:loadMap(id, spawnName) return self.maps:load(id, spawnName) end
function World:unloadMap() self.maps:unload() end
function World:currentMap() return self.maps.current end

-- ── texture name→id pool ──────────────────────────────────────────────────────────────────────────
-- The same name maps to the SAME id for the world's whole lifetime, so re-registering "grass" on a
-- map switch REPLACES the pixels under one id instead of growing the scene's registry with a fresh
-- id per load (the registry only ever holds one entry per distinct texture name).
function World:texIdFor(name)
    self.texPool = self.texPool or { ids = {}, next = 5000 }
    local id = self.texPool.ids[name]
    if id == nil then
        id = self.texPool.next
        self.texPool.next = id + 1
        self.texPool.ids[name] = id
    end
    return id
end

-- ── memory readout (debug overlays / leak hunts) ──────────────────────────────────────────────────
function World:getMemoryStats()
    local s = { luaKB = math.floor(collectgarbage("count")) }
    if self.scene.GetMemoryStats then
        local tex, texBytes, sprites, objects, grass, particles = self.scene:GetMemoryStats()
        s.tex, s.texBytes, s.sprites, s.objects, s.grass, s.particles =
            tex, texBytes, sprites, objects, grass, particles
    end
    if self.phys and self.phys.w and self.phys.w.StaticTriCount then s.physTris = self.phys.w:StaticTriCount() end
    if self.models and self.models.cache then
        local n = 0
        for _ in pairs(self.models.cache) do n = n + 1 end
        s.models = n
    end
    if self.scene.GetRendererStats then s.renderer = self.scene:GetRendererStats() end
    return s
end

-- ── stage-exit disposal (the per-stage tier of the contract) ─────────────────────────────────────
function World:dispose()
    pcall(function() self.maps:unload() end)
    pcall(function() self.scene:Dispose() end)
    if MODEL and MODEL.PurgeCache then pcall(function() MODEL:PurgeCache() end) end
end

-- ── per-frame pipeline ────────────────────────────────────────────────────────────────────────────
-- world:update(dt, px, py, pz): px/py/pz = the player focus (drives triggers + camera collision).
function World:update(dt, px, py, pz)
    self.time = (self.time or 0) + dt
    if self.scene.SetTime then self.scene:SetTime(self.time) end
    self.models:update(dt)
    self.phys:step(dt)
    if px ~= nil then self.triggers:update(dt, px, py or 0, pz or 0) end
    if self.maps.current and self.maps.current.updateDoors then self.maps.current:updateDoors(dt, px, pz) end
    if self.lit then self.daynight:update(dt) end
    self.weather:update(dt, px, py, pz)
    self.particles:update(dt)
    self.cam:update(dt)
    self.water:update(dt)   -- after the camera settles: reflections mirror the final eye
end

function World:drawSky()
    local hour = self.hour
    if not hour then local t = os.date("*t"); hour = t.hour + t.min / 60 end
    local top, hor = skyAt(hour)
    self.curSky = { top = top, hor = hor }          -- fog + reflection clear colours track the hour either way
    -- GPU path: the procedural sky shader paints the frame (clouds/sun/stars/moon) — skip the 2D bands
    if self.scene.SkyShaderActive and self.scene:SkyShaderActive() and self.daynight and self.daynight.sunX then
        local dn = self.daynight
        local cover = 0
        if self.weather and self.weather.isRaining and self.weather:isRaining() then
            cover = 0.45 * self.weather.intensity   -- rain overcasts the dome before the drops land
        end
        require("OWM3d.sky").update(self.scene, self.cam.fov, self.RW, self.RH,
            dn.sunX, dn.sunY, dn.sunZ, dn.dayF or 1, dn.nightF or 0, hour, cover)
        return
    end
    local bands = 18; local bh = self.RH / bands
    for i = 0, bands - 1 do
        local f = i / (bands - 1)
        local c = lerpRGB(top, hor, f)
        self.scene:FillRect(0, floor(i * bh), self.RW, floor(bh) + 2, floor(c[1]), floor(c[2]), floor(c[3]), 255)
    end
end

function World:render()
    self:drawSky()
    local d = self.cam.dist or 0
    if self.fogOn and self.curSky then
        local h = self.curSky.hor
        local wf = self.weatherFogScale or 1   -- rain pulls the fog in
        self.scene:SetFog(true, h[1], h[2], h[3], d + (self.fogNear or 24) * wf, d + (self.fogFar or 60) * wf)
    else
        self.scene:SetFog(false, 0, 0, 0, 0, 0)
    end
    if self.scene.SetDiorama then
        local g = self.diorama
        if g then self.scene:SetDiorama(true, g.tilt, g.sat, g.vig, g.bloom)
        else self.scene:SetDiorama(false, 0, 1, 0, 0) end
    end
    if self.scene.SetShadowFocus then self.scene:SetShadowFocus(self.cam.tx, self.cam.ty, self.cam.tz) end
    self.scene:Render()
    self.scene:Upload()
    -- surface sky/post shader compile failures once (they otherwise degrade silently)
    if self.scene.GetShaderError then
        local err = self.scene:GetShaderError()
        if err ~= "" and err ~= self._lastShaderErr then
            self._lastShaderErr = err
            print("[OWM3d] shader error: " .. err)
        end
    end
end

function World:blit()
    self.scene:SetColor(1, 1, 1); self.scene:SetOpacity(1.0)
    self.scene:SetScale(self.SW / self.RW, self.SH / self.RH)
    self.scene:Draw(0, 0)
end

return World
