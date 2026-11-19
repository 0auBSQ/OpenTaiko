---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/modelicon.lua: 3D model thumbnails (one offscreen render, then drawn as a texture)

local rad, sin, cos, max, huge = math.rad, math.sin, math.cos, math.max, math.huge

local ModelIcon = {}
ModelIcon.__index = ModelIcon

local live = setmetatable({}, { __mode = "k" })   -- weak: disposeAll never keeps icons alive

-- ==== Render budget ====
-- newFrame() each update caps first renders per frame; draw() returns false while waiting
-- without newFrame calls icons render at once
ModelIcon.budget = 5
local frameLeft = math.huge
function ModelIcon.newFrame() frameLeft = ModelIcon.budget end
function ModelIcon:ready() return self._rendered and not self._disposed end

-- opts: file, res (128), yaw (45), pitch (-22), fov (28), margin (1.08)
-- bg = {r,g,b,a} (nil = transparent), sun = {dx,dy,dz,r,g,b}, anim = index (posed at t=0)
function ModelIcon.new(opts)
    opts = opts or {}
    local self = setmetatable({}, ModelIcon)
    self.file = opts.file
    self.res = opts.res or 128
    self.yaw = opts.yaw or 45                      -- front 3/4 view
    self.pitch = opts.pitch or -22
    self.fov = opts.fov or 28
    self.margin = opts.margin or 1.08              -- fit padding (~8%)
    self.bg = opts.bg                              -- nil = transparent
    self.sun = opts.sun
    self.anim = opts.anim
    self.cpu = opts.cpu                            -- CPU raster (needed for ShareCanvasAs)
    self.faceThin = opts.faceThin                  -- wall items: look along the thin axis
    self.autoFrame = opts.autoFrame                -- pick the front 3/4 yaw with the largest footprint
    self._rendered = false
    self._disposed = false
    live[self] = true
    return self
end

-- ==== Builder icons ====
-- populate(world) fills { scene, _boxTarget, lit } with geometry around the origin
-- and registers its textures into world.scene; opts.frame = { cx, cy, cz, radius }
function ModelIcon.newBuilder(opts)
    opts = opts or {}
    local self = ModelIcon.new(opts)
    self.populate = opts.populate
    self.frame = opts.frame or { cx = 0, cy = 0.5, cz = 0, radius = 1.0 }
    return self
end

-- ==== Auto framing ====
-- 8 AABB corners as offsets from the centre
local function aabbCorners(mnx, mny, mnz, mxx, mxy, mxz, cx, cy, cz)
    return {
        { mnx - cx, mny - cy, mnz - cz }, { mnx - cx, mny - cy, mxz - cz },
        { mnx - cx, mxy - cy, mnz - cz }, { mnx - cx, mxy - cy, mxz - cz },
        { mxx - cx, mny - cy, mnz - cz }, { mxx - cx, mny - cy, mxz - cz },
        { mxx - cx, mxy - cy, mnz - cz }, { mxx - cx, mxy - cy, mxz - cz },
    }
end
-- camera right/up/forward for (yaw, pitch) in degrees (same as camera.lua)
local function camBasis(yaw, pitch)
    local yr, pr = rad(yaw), rad(pitch)
    return cos(yr), -sin(yr),                                   -- right  (rx, rz;  ry = 0)
           -sin(pr) * sin(yr), cos(pr), -sin(pr) * cos(yr),     -- up     (ux, uy, uz)
           sin(yr) * cos(pr), sin(pr), cos(yr) * cos(pr)        -- forward(fx, fy, fz)
end
-- smallest distance (looking at the bbox centre) with every corner inside the fov square
-- perspective-aware; margin ~ 1.08
local function fitDistance(corners, yaw, pitch, fov, margin)
    local rx, rz, ux, uy, uz, fx, fy, fz = camBasis(yaw, pitch)
    local tanH = math.tan(rad(fov) * 0.5)
    local dist = 0.001
    for _, o in ipairs(corners) do
        local dr = o[1] * rx + o[3] * rz
        local du = o[1] * ux + o[2] * uy + o[3] * uz
        local df = o[1] * fx + o[2] * fy + o[3] * fz
        local need = max(math.abs(dr), math.abs(du)) / tanH - df   -- dist to fit this corner
        if need > dist then dist = need end
    end
    return dist * margin
end
-- front 3/4 yaw with the largest projected footprint (models face +z); 225 wins ties
local function bestFrontYaw(corners, pitch)
    local best, bestArea = 225, -1
    for _, yaw in ipairs({ 225, 210, 240, 135, 150, 120 }) do
        local rx, rz, ux, uy, uz = camBasis(yaw, pitch)
        local minr, maxr, minu, maxu = huge, -huge, huge, -huge
        for _, o in ipairs(corners) do
            local dr = o[1] * rx + o[3] * rz
            local du = o[1] * ux + o[2] * uy + o[3] * uz
            if dr < minr then minr = dr end; if dr > maxr then maxr = dr end
            if du < minu then minu = du end; if du > maxu then maxu = du end
        end
        local area = (maxr - minr) * (maxu - minu)
        if area > bestArea + 1e-6 then bestArea, best = area, yaw end
    end
    return best
end

function ModelIcon:_render()
    self._rendered = true
    local ok = pcall(function()
        local scene = SCENE3D:CreateScene(self.res, self.res)
        self.scene = scene
        scene:SetMode(self.cpu and "raster_cpu" or "raster")
        scene:SetThreads(2)
        scene:SetLighting(true)
        scene:SetAmbient(0.52, 0.52, 0.58)
        local cx, cy, cz, dist
        if self.populate then
            -- builder icon
            local obj = scene:NewObject()
            scene:ObjSetLit(obj, true)
            self.populate({ scene = scene, _boxTarget = obj, lit = true })
            local fr = self.frame
            cx, cy, cz = fr.cx or 0, fr.cy or 0.5, fr.cz or 0
            local radius = max(0.001, fr.radius or 1.0)
            dist = radius * self.margin / math.tan(rad(self.fov) * 0.5)
        else
        local model = MODEL:Load(self.file)
        if model == nil then error("no model") end
        model:Register(scene)
        local minX, minY, minZ, maxX, maxY, maxZ = model:GetBounds()
        cx, cy, cz = (minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2
        local rx, ry, rz = (maxX - minX) / 2, (maxY - minY) / 2, (maxZ - minZ) / 2
        local corners = aabbCorners(minX, minY, minZ, maxX, maxY, maxZ, cx, cy, cz)
        if self.faceThin then
            -- wall item: look along the thinnest axis (face on its + side)
            if rx <= ry and rx <= rz then self.yaw, self.pitch = 270, -6     -- thin X → face on +x
            elseif rz <= rx and rz <= ry then self.yaw, self.pitch = 180, -6 -- thin Z → face on +z
            else self.yaw, self.pitch = 0, -78 end                          -- thin Y → look down
        elseif self.autoFrame then
            self.yaw = bestFrontYaw(corners, self.pitch)
        end

        local obj = scene:NewObject()
        local ai = self.anim or -1
        model:Pose(scene, obj, ai, 0, 0, 0, 0, 0, 1)
        dist = fitDistance(corners, self.yaw, self.pitch, self.fov, self.margin)
        end

        local yr, pr = rad(self.yaw), rad(self.pitch)
        local fx = sin(yr) * cos(pr)
        local fy = sin(pr)
        local fz = cos(yr) * cos(pr)
        scene:SetCameraFov(self.fov)
        scene:SetCameraAngles(self.yaw, self.pitch)
        scene:SetCameraPosition(cx - fx * dist, cy - fy * dist, cz - fz * dist)

        -- key light over the camera's shoulder
        local s = self.sun
        if s then scene:SetSun(s[1], s[2], s[3], s[4], s[5], s[6])
        else scene:SetSun(-fx + 0.35, 0.9, -fz + 0.2, 0.9, 0.86, 0.78) end

        local bg = self.bg
        if bg then scene:Clear(bg[1], bg[2], bg[3], bg[4] or 255)
        else scene:Clear(0, 0, 0, 0) end
        scene:Render()
        scene:Upload()
    end)
    if not ok and self.scene then
        pcall(function() self.scene:Clear(0, 0, 0, 0); self.scene:Upload() end)
    end
end

-- w, h default to res; renders once on first call
-- returns false while waiting on the budget
function ModelIcon:draw(x, y, w, h, opacity)
    if self._disposed then return false end
    if not self._rendered then
        if frameLeft <= 0 then return false end
        frameLeft = frameLeft - 1
        self:_render()
    end
    local scene = self.scene
    if scene == nil then return false end
    scene:SetOpacity(opacity or 1.0)
    scene:SetScale((w or self.res) / self.res, (h or self.res) / self.res)
    scene:Draw(x, y)
    scene:SetScale(1, 1); scene:SetOpacity(1.0)
    return true
end

function ModelIcon:drawAtAnchor(x, y, w, h, anchor, opacity)
    if self._disposed then return false end
    if not self._rendered then
        if frameLeft <= 0 then return false end
        frameLeft = frameLeft - 1
        self:_render()
    end
    local scene = self.scene
    if scene == nil then return false end
    scene:SetOpacity(opacity or 1.0)
    scene:SetScale((w or self.res) / self.res, (h or self.res) / self.res)
    scene:DrawAtAnchor(x, y, anchor or "center")
    scene:SetScale(1, 1); scene:SetOpacity(1.0)
end

-- copy the render into the shared-texture store under key (cpu = true icons only)
function ModelIcon:shareToSharedTexture(key)
    if self._disposed then return false end
    if not self._rendered then self:_render() end
    local scene = self.scene
    if scene == nil or scene.ShareCanvasAs == nil then return false end
    local ok = pcall(function() scene:ShareCanvasAs(key) end)
    return ok
end

-- call when the UI closes; disposeAll on stage exit
function ModelIcon:dispose()
    if self._disposed then return end
    self._disposed = true
    if self.scene then pcall(function() self.scene:Dispose() end); self.scene = nil end
    live[self] = nil
end

function ModelIcon.disposeAll()
    for icon in pairs(live) do icon:dispose() end
end

return ModelIcon
