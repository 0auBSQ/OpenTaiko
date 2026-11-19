---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/camera.lua: world camera rigs and collision boom

local sin, cos, pi = math.sin, math.cos, math.pi

local Camera = {}
Camera.__index = Camera

-- rig presets; setRig/blendRig also take a {yaw, pitch, fov, dist} table
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

    self.collide = false                          -- needs world.phys geometry
    self.pad = 0.32                               -- surface clearance (= character radius)
    self.minBoom = o.minBoom or 2.5               -- min boom distance
    self.lift = 0.6                               -- ray origin height above target
    self.easeOut = 6.0                            -- boom ease-out rate (1/s)
    self._boomDist = self.dist                    -- applied boom distance (smoothed)

    self._blend = nil                             -- { from={}, to={}, t, dur }
    return self
end

-- ==== Rig control ====
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
-- blend to rig over dur seconds (yaw takes the short way)
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

-- ==== Per frame ====
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
    -- collision boom
    local want = self.dist
    if self.collide then
        local clear = self:clearance(want)
        local floorD = math.max(self.minBoom or 2.5, want * 0.25)
        if clear < floorD then clear = floorD end
        if clear < self._boomDist * 0.97 then
            -- ease in fast; 3% hysteresis against grazing hits
            local k = 1 - math.exp(-(self.easeIn or 18.0) * dt)
            self._boomDist = self._boomDist + (clear - self._boomDist) * k
        else
            local k = 1 - math.exp(-self.easeOut * dt)             -- ease out
            self._boomDist = self._boomDist + (math.min(clear, want) - self._boomDist) * k
        end
        if self._boomDist > want then self._boomDist = want end
        if self._boomDist < floorD then self._boomDist = floorD end
    else
        self._boomDist = want
    end
    self:apply()
end

-- max unobstructed boom distance (5 padded rays)
-- uses the second-smallest hit: a single thin occluder is ignored
function Camera:clearance(maxDist)
    local phys = self.world.phys
    if phys == nil or not phys:hasGeometry() then return maxDist end
    local yr, pr = self.yaw * pi / 180, self.pitch * pi / 180
    local fx, fy, fz = sin(yr) * cos(pr), sin(pr), cos(yr) * cos(pr)
    -- ray dir: target to camera (-forward)
    local dx, dy, dz = -fx, -fy, -fz
    -- view right/up for the offset rays
    local rx, rz = cos(yr), -sin(yr)                               -- camera right
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
    local best = hits[2] or hits[1]                                -- skip one outlier
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
