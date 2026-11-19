---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/water.lua: water planes
-- reflect = true: wave mesh with a screen-tex reflection; false: plain ripple mesh

local sin, floor, max, min, sqrt = math.sin, math.floor, math.max, math.min, math.sqrt

local TEX_BASE = 9200          -- reflection texture ids (one per reflective plane)

local Water = {}
Water.__index = Water

-- invisible fence around a water def {x0, z0, x1, z1, y}
-- call inside the static build session
function Water.emitFence(phys, wd)
    local y0, y1w = (wd.y or 0) - 1.4, (wd.y or 0) + 0.6
    phys:addQuad(wd.x0, y0, wd.z0, wd.x1, y0, wd.z0, wd.x1, y1w, wd.z0, wd.x0, y1w, wd.z0)
    phys:addQuad(wd.x0, y0, wd.z1, wd.x1, y0, wd.z1, wd.x1, y1w, wd.z1, wd.x0, y1w, wd.z1)
    phys:addQuad(wd.x0, y0, wd.z0, wd.x0, y0, wd.z1, wd.x0, y1w, wd.z1, wd.x0, y1w, wd.z0)
    phys:addQuad(wd.x1, y0, wd.z0, wd.x1, y0, wd.z1, wd.x1, y1w, wd.z1, wd.x1, y1w, wd.z0)
end

function Water.new(world)
    local self = setmetatable({}, Water)
    self.world = world
    self.planes = {}
    self.t = 0
    self._meshT = 0
    self._frame = 0
    local scene = world.scene
    if scene.SetReflectionScale then scene:SetReflectionScale(2) end   -- 1/16 pixels per re-render
    if scene.SetReflectionGpu then scene:SetReflectionGpu(true) end    -- GPU off-screen pass (CPU fallback inside)
    return self
end

local FOAM_SPR = 916
local foamScenes = setmetatable({}, { __mode = "k" })   -- scenes with the foam sprite

local function ensureFoamSprite(scene)
    if scene.SetSpriteRGBA == nil then return false end
    if foamScenes[scene] then return true end
    foamScenes[scene] = true
    if scene.MakeSoftCircle then scene:MakeSoftCircle(FOAM_SPR, 32, 2.2) end
    return true
end

-- def: { rect = {x0, z0, x1, z1}, y, amp, reflect, ripple, tex, color = {r,g,b}, deepColor = {r,g,b} }
function Water:add(def)
    local scene = self.world.scene
    local p = {
        x0 = def.rect[1], z0 = def.rect[2], x1 = def.rect[3], z1 = def.rect[4],
        y = def.y or 0, amp = def.amp or 0.05,
        reflect = def.reflect and true or false,
        ripple = def.ripple or 0.6,
        texName = def.tex,
        col = def.color or { 52, 96, 122 },              -- shallow colour
        deep = def.deepColor or { 12, 34, 56 },          -- deep colour (depth tint)
    }
    p.cx, p.cz = (p.x0 + p.x1) * 0.5, (p.z0 + p.z1) * 0.5
    p.obj = scene:NewObject()
    scene:ObjSetLit(p.obj, false)
    if p.reflect then
        p.texId = TEX_BASE + #self.planes
        -- transparent pass (~70% alpha) over the bottom
        scene:ObjSetPass(p.obj, 1, p.col[1], p.col[2], p.col[3], 178)
        scene:ObjSetScreenTex(p.obj, true)
        if scene.ObjSetScreenTexRipple then scene:ObjSetScreenTexRipple(p.obj, p.ripple * 3.0, 1.7, 2.1) end
        -- per-pixel Schlick fresnel + depth tint (shade slot = depth)
        if scene.ObjSetFresnel then scene:ObjSetFresnel(p.obj, 0.04) end
        if scene.ObjSetWaterColors then scene:ObjSetWaterColors(p.obj, p.deep[1], p.deep[2], p.deep[3]) end
        self:_mesh(p, 0)
    else
        scene:ObjSetPass(p.obj, 1, p.col[1] + 30, p.col[2] + 50, p.col[3] + 60, 185)
        self:_mesh(p, 0)
    end
    scene:ObjSetBounds(p.obj, p.x0, p.y - p.amp - 0.1, p.z0, p.x1, p.y + p.amp + 0.1, p.z1)
    self.planes[#self.planes + 1] = p
    return p
end

-- depth factor 0 (shore) to 1 (deep) from the map ground height
function Water:_depthAt(p, x, z)
    local m = self.world.maps and self.world.maps.current
    if m == nil or m.heightAt == nil then return 1 end
    local ok, g = pcall(m.heightAt, m, x, z)
    if not ok or g == nil then return 1 end
    local d = (p.y - g) / 2.5
    if d < 0 then d = 0 elseif d > 1 then d = 1 end
    return d
end

-- shore foam where the ground meets the waterline; built once (static ground)
function Water:_buildFoam(p)
    if p.foamObj ~= nil or not p.reflect then return end
    local scene = self.world.scene
    local m = self.world.maps and self.world.maps.current
    if m == nil or m.heightAt == nil or not ensureFoamSprite(scene) then return end
    local made = false
    local obj = scene:NewObject()
    scene:ObjSetLit(obj, false)
    scene:ObjSetPass(obj, 1, 255, 255, 255, 150)
    if scene.ObjSetCastShadow then scene:ObjSetCastShadow(obj, false) end
    local step = 1.0
    for x = p.x0, p.x1 - step, step do
        for z = p.z0, p.z1 - step, step do
            local ok, g = pcall(m.heightAt, m, x + step * 0.5, z + step * 0.5)
            if ok and g and math.abs(g - p.y) < 0.18 then
                local fy = p.y + 0.015
                scene:ObjAddSpriteQuad(obj, x, fy, z, x + step, fy, z,
                    x + step, fy, z + step, x, fy, z + step, FOAM_SPR, 0)
                made = true
            end
        end
    end
    if made then
        scene:ObjSetBounds(obj, p.x0, p.y - 0.2, p.z0, p.x1, p.y + 0.2, p.z1)
        p.foamObj = obj
    else
        scene:DeleteObject(obj)
    end
end

-- wave mesh; reflective planes put the depth factor in the shade slot
-- depth samples are cached (static ground)
function Water:_mesh(p, t)
    local scene = self.world.scene
    local span = max(p.x1 - p.x0, p.z1 - p.z0)
    local seg = max(4, min(14, floor(span / 3)))
    local dx, dz = (p.x1 - p.x0) / seg, (p.z1 - p.z0) / seg
    local function h(x, z)
        return p.y + p.amp * (0.6 * sin(x * 1.9 + t * 1.7) + 0.4 * sin(z * 2.3 + t * 2.1)
                            + 0.3 * sin((x + z) * 1.1 - t * 1.3))
    end
    if p.reflect and p._depths == nil and self.world.maps and self.world.maps.current then
        p._depths = {}
        for i = 0, seg - 1 do
            for j = 0, seg - 1 do
                p._depths[i * seg + j] = self:_depthAt(p, p.x0 + (i + 0.5) * dx, p.z0 + (j + 0.5) * dz)
            end
        end
        p._depthSeg = seg
    end
    scene:ObjBegin(p.obj)
    if p.reflect then
        local fresnel = scene.ObjSetFresnel ~= nil
        for i = 0, seg - 1 do
            for j = 0, seg - 1 do
                local x0, z0 = p.x0 + i * dx, p.z0 + j * dz
                local x1, z1 = x0 + dx, z0 + dz
                -- shade = depth (fresnel) or fixed reflectivity (fallback)
                local shade = fresnel and ((p._depths and p._depthSeg == seg) and p._depths[i * seg + j] or 1) or 0.55
                scene:ObjAddQuadTex(p.obj,
                    x0, h(x0, z0), z0, x1, h(x1, z0), z0,
                    x1, h(x1, z1), z1, x0, h(x0, z1), z1, p.texId, 1, 1, shade)
            end
        end
    else
        for i = 0, seg - 1 do
            for j = 0, seg - 1 do
                local x0, z0 = p.x0 + i * dx, p.z0 + j * dz
                local x1, z1 = x0 + dx, z0 + dz
                scene:ObjAddQuadFlat(p.obj,
                    x0, h(x0, z0), z0, x1, h(x1, z0), z0,
                    x1, h(x1, z1), z1, x0, h(x0, z1), z1)
            end
        end
    end
end

function Water:update(dt)
    if #self.planes == 0 then return end
    local world, scene = self.world, self.world.scene
    self.t = self.t + dt
    self._frame = self._frame + 1

    -- remesh at ~12 Hz (shader ripple runs every frame)
    self._meshT = self._meshT + dt
    local remesh = self._meshT >= 0.085
    if remesh then self._meshT = 0 end

    local camX, camY, camZ = scene:GetCameraPosition()
    for _, p in ipairs(self.planes) do
        if p.reflect then
            if remesh then
                self:_mesh(p, self.t)
                self:_buildFoam(p)
                if p.foamObj then
                    -- foam alpha pulse
                    local a = 120 + floor(55 * (0.5 + 0.5 * sin(self.t * 1.9)))
                    scene:ObjSetPass(p.foamObj, 1, 255, 255, 255, a)
                end
            end
            local ddx, ddz = p.cx - camX, p.cz - camZ
            local near = camY > p.y and ddx * ddx + ddz * ddz < 90 * 90
            local sky = world.curSky and world.curSky.hor or { 120, 150, 190 }
            if near and scene.RenderViewReflect then
                -- planar reflection every frame (GPU pass at 1/16 res)
                scene:RenderViewReflect(p.texId, p.cx, p.y, p.cz, 0, 1, 0,
                    floor(sky[1]), floor(sky[2]), floor(sky[3]))
            elseif near and self._frame % 2 == 0 then
                -- fallback: mirrored capture on alternate frames
                scene:RenderView(p.texId,
                    camX, 2 * p.y - camY, camZ,
                    scene:GetCameraYaw(), -scene:GetCameraPitch(), scene:GetCameraFov(),
                    floor(sky[1]), floor(sky[2]), floor(sky[3]), 1)
            end
        elseif remesh then
            self:_mesh(p, self.t)
        end
    end
end

function Water:clear()
    local scene = self.world.scene
    for _, p in ipairs(self.planes) do
        scene:DeleteObject(p.obj)
        if p.foamObj then scene:DeleteObject(p.foamObj) end
        -- free the reflection texture; the next map reuses the id
        if p.texId and scene.UnregisterTexture then scene:UnregisterTexture(p.texId) end
    end
    self.planes = {}
    self._frame = 0
    self._meshT = 0
    self.t = 0
end

function Water:fromDefs(defs)
    for _, d in ipairs(defs or {}) do
        self:add{
            rect = { d.x0, d.z0, d.x1, d.z1 }, y = d.y, amp = d.amp,
            reflect = d.reflect, ripple = d.ripple, tex = d.tex,
            color = d.color, deepColor = d.deepColor,
        }
    end
end

return Water
