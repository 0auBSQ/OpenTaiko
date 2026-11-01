---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/camera.lua — the world camera: named rigs (iso / interior / orbit presets), smoothed follow,
-- orbit/zoom, rig blending, and a PHYSICS-AWARE BOOM so the camera never clips through terrain,
-- walls or models.
--
-- Boom: each frame the desired camera position is target - forward*dist. Five rays are cast from the
-- (slightly lifted) target toward that position — one central plus four offset by the near-plane pad
-- perpendicular to the view — and the camera distance is clamped to the nearest hit minus the pad.
-- Snapping IN is instant (a wall between camera and player must never occlude); easing back OUT is
-- smoothed so the camera doesn't pop when the obstruction clears.

local sin, cos, pi = math.sin, math.cos, math.pi

local Camera = {}
Camera.__index = Camera

-- built-in rig presets (a map/trigger can also define its own {yaw,pitch,fov,dist} tables)
Camera.RIGS = {
    iso      = { yaw = 45, pitch = -33, fov = 20, dist = 46 },
    interior = { yaw = 45, pitch = -28, fov = 26, dist = 24 },
    orbit    = { yaw = 45, pitch = -20, fov = 45, dist = 10 },
}

function Camera.new(world, o)
    o = o or {}
    local self = setmetatable({}, Camera)
    self.world = world
    self.yaw   = o.yaw or 45
    self.canonYaw = self.yaw
    self.pitch = o.pitch or -33
    self.fov   = o.fov or 20
    self.dist  = o.dist or 46
    self.tx, self.ty, self.tz = 0, 0.4, 0        -- look-at
    self.gx, self.gy, self.gz = 0, 0.4, 0        -- follow goal
    self.followK = 0

    self.collide = false                          -- opt-in camera physics (world.phys must have geometry)
    self.pad = 0.32                               -- clearance kept off surfaces (= character radius)
    self.minBoom = o.minBoom or 2.5               -- the boom never crowds closer than this
    self.lift = 0.6                               -- ray origin height above the follow target (eye-ish)
    self.easeOut = 6.0                            -- speed the boom relaxes back out (per second)
    self._boomDist = self.dist                    -- smoothed physical distance actually applied

    -- rig blending
    self._blend = nil                             -- { from={}, to={}, t, dur }
    return self
end

-- ── rig control ───────────────────────────────────────────────────────────────────────────────────
function Camera:setRig(rig)
    if type(rig) == "string" then rig = Camera.RIGS[rig] end
    if rig == nil then return end
    self.yaw   = rig.yaw or self.yaw
    self.pitch = rig.pitch or self.pitch
    self.fov   = rig.fov or self.fov
    self.dist  = rig.dist or self.dist
    self._boomDist = self.dist
    self._blend = nil
end
-- smoothly blend toward a rig over dur seconds (yaw takes the short way around)
function Camera:blendRig(rig, dur)
    if type(rig) == "string" then rig = Camera.RIGS[rig] end
    if rig == nil then return end
    self._blend = {
        from = { yaw = self.yaw, pitch = self.pitch, fov = self.fov, dist = self.dist },
        to = { yaw = rig.yaw or self.yaw, pitch = rig.pitch or self.pitch,
               fov = rig.fov or self.fov, dist = rig.dist or self.dist },
        t = 0, dur = dur or 0.35,
    }
end

function Camera:setTarget(x, y, z)
    self.tx, self.ty, self.tz = x, y or self.ty, z
    self.gx, self.gy, self.gz = self.tx, self.ty, self.tz
end
function Camera:follow(x, y, z, k) self.gx, self.gy, self.gz = x, y or self.gy, z; self.followK = k or 8 end
function Camera:orbit(dyaw, dpitch)
    self.yaw = (self.yaw + (dyaw or 0)) % 360
    if dpitch then
        self.pitch = self.pitch + dpitch
        if self.pitch > -8 then self.pitch = -8 elseif self.pitch < -80 then self.pitch = -80 end
    end
end
function Camera:zoom(d)
    self.dist = self.dist + d
    if self.dist < 3 then self.dist = 3 elseif self.dist > 90 then self.dist = 90 end
end
function Camera:setYaw(y) self.yaw = y end
function Camera:setPitch(p) self.pitch = p end
function Camera:getYaw() return self.yaw end

-- ── per-frame ─────────────────────────────────────────────────────────────────────────────────────
local function angleLerp(a, b, t)
    local d = (b - a + 180) % 360 - 180
    return a + d * t
end

function Camera:update(dt)
    -- rig blend
    local bl = self._blend
    if bl then
        bl.t = bl.t + dt
        local f = bl.dur > 0 and math.min(1, bl.t / bl.dur) or 1
        f = f * f * (3 - 2 * f)                                   -- smoothstep
        self.yaw   = angleLerp(bl.from.yaw, bl.to.yaw, f)
        self.pitch = bl.from.pitch + (bl.to.pitch - bl.from.pitch) * f
        self.fov   = bl.from.fov + (bl.to.fov - bl.from.fov) * f
        self.dist  = bl.from.dist + (bl.to.dist - bl.from.dist) * f
        if f >= 1 then self._blend = nil end
    end
    -- smoothed follow
    if self.followK > 0 then
        local k = 1 - math.exp(-self.followK * dt)
        self.tx = self.tx + (self.gx - self.tx) * k
        self.ty = self.ty + (self.gy - self.ty) * k
        self.tz = self.tz + (self.gz - self.tz) * k
    end
    -- physics boom
    local want = self.dist
    if self.collide then
        local clear = self:clearance(want)
        local floorD = math.max(self.minBoom or 2.5, want * 0.25)  -- never crowd the player
        if clear < floorD then clear = floorD end
        if clear < self._boomDist * 0.97 then
            -- move IN fast but SMOOTHLY (a hard cut reads as a jump), with a little hysteresis so
            -- grazing hits don't make the camera pump in and out every frame
            local k = 1 - math.exp(-(self.easeIn or 18.0) * dt)
            self._boomDist = self._boomDist + (clear - self._boomDist) * k
        else
            local k = 1 - math.exp(-self.easeOut * dt)             -- ease back OUT
            self._boomDist = self._boomDist + (math.min(clear, want) - self._boomDist) * k
        end
        if self._boomDist > want then self._boomDist = want end
        if self._boomDist < floorD then self._boomDist = floorD end
    else
        self._boomDist = want
    end
    self:apply()
end

-- max unobstructed boom distance toward the current camera direction. Five padded rays are cast
-- (centre + 4 offsets); the SECOND-SMALLEST clearance is used, so a single thin occluder — a lamp
-- post, a tree trunk, a fence picket — never yanks the camera, while real walls (which block
-- several rays) still clamp it.
function Camera:clearance(maxDist)
    local phys = self.world.phys
    if phys == nil or not phys:hasGeometry() then return maxDist end
    local yr, pr = self.yaw * pi / 180, self.pitch * pi / 180
    local fx, fy, fz = sin(yr) * cos(pr), sin(pr), cos(yr) * cos(pr)
    -- ray dir = from target TOWARD the camera = -forward
    local dx, dy, dz = -fx, -fy, -fz
    -- perpendicular basis (right + up of the view) for the padded offset rays
    local rx, rz = cos(yr), -sin(yr)                               -- camera right (horizontal)
    local ux = -sin(pr) * sin(yr); local uy = cos(pr); local uz = -sin(pr) * cos(yr)   -- camera up
    local ox, oy, oz = self.tx, self.ty + self.lift, self.tz
    local pad = self.pad
    local hits = {}
    local function ray(ex, ey, ez)
        local d = phys:raycast(ex, ey, ez, dx, dy, dz, maxDist + pad)
        hits[#hits + 1] = d and (d - pad) or maxDist
    end
    ray(ox, oy, oz)
    ray(ox + rx * pad, oy, oz + rz * pad)
    ray(ox - rx * pad, oy, oz - rz * pad)
    ray(ox + ux * pad, oy + uy * pad, oz + uz * pad)
    ray(ox - ux * pad, oy + uy * pad, oz - uz * pad)
    table.sort(hits)
    local best = hits[2] or hits[1]                                -- ignore the single worst outlier
    if best > maxDist then best = maxDist end
    if best < 0.4 then best = 0.4 end
    return best
end

function Camera:apply()
    local yr, pr = self.yaw * pi / 180, self.pitch * pi / 180
    local fx, fy, fz = sin(yr) * cos(pr), sin(pr), cos(yr) * cos(pr)
    local d = self._boomDist or self.dist
    local scene = self.world.scene
    scene:SetCameraFov(self.fov)
    scene:SetCameraAngles(self.yaw, self.pitch)
    scene:SetCameraPosition(self.tx - fx * d, self.ty - fy * d, self.tz - fz * d)
end

return Camera
