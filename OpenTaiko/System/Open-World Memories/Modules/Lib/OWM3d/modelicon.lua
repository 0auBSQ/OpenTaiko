---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/modelicon.lua — 3D-model preview thumbnails: renders a glTF/GLB into its own small
-- offscreen scene ONCE (lazily, on first draw) and then draws that image like any texture.
-- Made for catalog/edit-mode icons now and shop/modal previews later — res is caller-chosen,
-- so the same API serves a 96px grid button and a 512px shop card.
--
--   local ModelIcon = require("OWM3d").ModelIcon
--   local icon = ModelIcon.new{ file = "Models/chair.glb", res = 128 }   -- allocate (cheap; no render yet)
--   icon:draw(x, y, w, h)                    -- first call renders once, then it's a plain blit
--   icon:drawAtAnchor(x, y, w, h, "center")
--   icon:dispose()                           -- free the GPU scene when the UI closes
--   ModelIcon.disposeAll()                   -- safety net (stage deactivate)
--
-- Memory: each icon owns one res×res scene (colour + depth). Dispose icons when their UI closes;
-- anything missed is still freed with the stage (scenes sit in the engine's per-script dispose
-- list), but don't lean on that for high-res shop previews — those should be created on open and
-- disposed on close. Failed model loads render a transparent image (draw() is always safe).
--
-- opts: file (required) · res (128) · yaw (32) · pitch (-24) · fov (28) · margin (1.12)
--       bg = {r,g,b,a} (default transparent) · sun = {dx,dy,dz,r,g,b} · anim = index (posed at t=0)

local rad, sin, cos, max, huge = math.rad, math.sin, math.cos, math.max, math.huge

local ModelIcon = {}
ModelIcon.__index = ModelIcon

local live = setmetatable({}, { __mode = "k" })   -- weak: disposeAll never keeps icons alive

-- ── per-frame render budget ───────────────────────────────────────────────────────────────────────
-- Rendering many first-time icons in one frame stalls it (each is a scene + GLB load). A stage that
-- calls ModelIcon.newFrame() every update caps first-renders per frame (icons stagger in over a few
-- frames); draw() returns false while an icon is still waiting so the caller can show a fallback.
-- Stages that never call newFrame keep the render-immediately behaviour.
ModelIcon.budget = 5
local frameLeft = math.huge
function ModelIcon.newFrame() frameLeft = ModelIcon.budget end
function ModelIcon:ready() return self._rendered and not self._disposed end

function ModelIcon.new(opts)
    opts = opts or {}
    local self = setmetatable({}, ModelIcon)
    self.file = opts.file
    self.res = opts.res or 128
    self.yaw = opts.yaw or 45                      -- front-3/4 matching the in-room camera (was 24)
    self.pitch = opts.pitch or -22                 -- gentle top-down (was -18)
    self.fov = opts.fov or 28
    self.margin = opts.margin or 1.08              -- tight-fit padding (~8%); framing is now projected-AABB
    self.bg = opts.bg                              -- nil = transparent
    self.sun = opts.sun
    self.anim = opts.anim
    self.cpu = opts.cpu                            -- force CPU raster (needed to capture pixels via ShareCanvasAs)
    self.faceThin = opts.faceThin                  -- flat wall items: look along the thin (face) axis
    self.autoFrame = opts.autoFrame                -- pick the front-3/4 yaw that shows the most of the model
    self._rendered = false
    self._disposed = false
    live[self] = true
    return self
end

-- ── builder icons: a 3D snapshot of PROCEDURAL geometry (shaded-box furniture etc.) ──────────────
-- Same lifecycle/draw/budget contract as model icons, but instead of loading a GLB the caller's
-- populate(world) fills a minimal fake world — { scene, _boxTarget, lit } — with geometry around
-- the origin (register any textures it samples into world.scene first). Camera framing comes from
-- opts.frame = { cx=, cy=, cz=, radius= } (radius ≈ half the item's diagonal).
--
--   local icon = ModelIcon.newBuilder{
--       res = 128, frame = { cx = 0, cy = 0.6, cz = 0, radius = 1.1 },
--       populate = function(world) A.registerInto(world.scene); buildChair(world, 0, 0, 0, 0) end,
--   }
function ModelIcon.newBuilder(opts)
    opts = opts or {}
    local self = ModelIcon.new(opts)
    self.populate = opts.populate
    self.frame = opts.frame or { cx = 0, cy = 0.5, cz = 0, radius = 1.0 }
    return self
end

-- ── automated framing (research-backed) ────────────────────────────────────────────────────────────
-- The 8 corners of an AABB, as offsets from its centre, for a given (min,max).
local function aabbCorners(mnx, mny, mnz, mxx, mxy, mxz, cx, cy, cz)
    return {
        { mnx - cx, mny - cy, mnz - cz }, { mnx - cx, mny - cy, mxz - cz },
        { mnx - cx, mxy - cy, mnz - cz }, { mnx - cx, mxy - cy, mxz - cz },
        { mxx - cx, mny - cy, mnz - cz }, { mxx - cx, mny - cy, mxz - cz },
        { mxx - cx, mxy - cy, mnz - cz }, { mxx - cx, mxy - cy, mxz - cz },
    }
end
-- camera right/up/forward basis for (yaw,pitch) degrees (matches Lua3DScene/camera.lua)
local function camBasis(yaw, pitch)
    local yr, pr = rad(yaw), rad(pitch)
    return cos(yr), -sin(yr),                                   -- right  (rx, rz;  ry = 0)
           -sin(pr) * sin(yr), cos(pr), -sin(pr) * cos(yr),     -- up     (ux, uy, uz)
           sin(yr) * cos(pr), sin(pr), cos(yr) * cos(pr)        -- forward(fx, fy, fz)
end
-- TIGHT frame-to-fit: the smallest camera distance (looking AT the bbox centre) that keeps every corner
-- inside a square frame of the given fov, accounting for perspective depth — so a flat/elongated model
-- fills the frame instead of floating in a bounding-sphere's worth of empty space. margin ≈ 1.08.
local function fitDistance(corners, yaw, pitch, fov, margin)
    local rx, rz, ux, uy, uz, fx, fy, fz = camBasis(yaw, pitch)
    local tanH = math.tan(rad(fov) * 0.5)
    local dist = 0.001
    for _, o in ipairs(corners) do
        local dr = o[1] * rx + o[3] * rz
        local du = o[1] * ux + o[2] * uy + o[3] * uz
        local df = o[1] * fx + o[2] * fy + o[3] * fz
        local need = max(math.abs(dr), math.abs(du)) / tanH - df   -- dist to fit THIS corner (perspective)
        if need > dist then dist = need end
    end
    return dist * margin
end
-- FRONT-3/4 auto view: among front-hemisphere 3/4 yaws, pick the one whose projected footprint is
-- largest (shows the most of the model). Stays in front (never the back); 45 wins ties (room default).
local function bestFrontYaw(corners, pitch)
    local best, bestArea = 45, -1
    for _, yaw in ipairs({ 45, 30, 60, 315, 330, 300 }) do
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
            -- builder icon: the caller fills a minimal fake world with procedural geometry
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
            -- flat wall item (a clock, thin in X): look along the THINNEST axis so its FACE is shown
            -- head-on, not edge-on (the vertical-bar silhouette). WALL_GLB says the face is the − side
            -- for X (default cam sees the −x front) and the + side for Z.
            if rx <= ry and rx <= rz then self.yaw, self.pitch = 90, -6      -- thin X → face on −x
            elseif rz <= rx and rz <= ry then self.yaw, self.pitch = 180, -6 -- thin Z → face on +z
            else self.yaw, self.pitch = 0, -78 end                          -- thin Y → look down
        elseif self.autoFrame then
            self.yaw = bestFrontYaw(corners, self.pitch)                     -- best front-3/4 (most coverage)
        end

        local obj = scene:NewObject()
        local ai = self.anim or -1
        model:Pose(scene, obj, ai, 0, 0, 0, 0, 0, 1)
        dist = fitDistance(corners, self.yaw, self.pitch, self.fov, self.margin)   -- TIGHT projected-AABB fit
        end

        local yr, pr = rad(self.yaw), rad(self.pitch)
        local fx = sin(yr) * cos(pr)
        local fy = sin(pr)
        local fz = cos(yr) * cos(pr)
        scene:SetCameraFov(self.fov)
        scene:SetCameraAngles(self.yaw, self.pitch)
        scene:SetCameraPosition(cx - fx * dist, cy - fy * dist, cz - fz * dist)

        -- key light from over the camera's shoulder (warm), matching the catalog look
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

-- draws the thumbnail at (x, y) scaled to w×h (defaults to the native res). Safe to call every
-- frame — the 3D render happens exactly once.
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

-- Render (if needed) and copy the result into the global shared-texture store under `key`, so another
-- stage can draw this thumbnail without re-loading the model. Only meaningful for a cpu=true icon
-- (the scene's CPU buffer is what ShareCanvasAs copies); a no-op if the engine lacks the API.
function ModelIcon:shareToSharedTexture(key)
    if self._disposed then return false end
    if not self._rendered then self:_render() end
    local scene = self.scene
    if scene == nil or scene.ShareCanvasAs == nil then return false end
    local ok = pcall(function() scene:ShareCanvasAs(key) end)
    return ok
end

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
