---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/models.lua — 3D model instances in the world (animated or static glTF/GLB).
--
--   local inst = world.models:add{
--       file = "models/inn.glb", x = 4, y = 0, z = 7, yaw = 90, scale = 1,
--       collide = "mesh" | "box" | "none",           -- collision baked into the static soup
--       group = "inn",                                -- visibility + collider group (trigger-toggleable)
--       visible = true,
--       anim = { index = 0, speed = 1, loop = true, playing = true },
--       parts = { { part = 2, emissive = {1,0.8,0.4, 2.0} },   -- routed to its own object:
--                 { part = 3, alpha = 150 },                    -- transparent pass
--                 { part = 1, unlit = true },                   -- full-bright
--                 { part = 0, mirror = true } },                -- planar screen-tex reflection
--   }
--   inst:setVisible(on) ; inst:play(i) ; inst:setPaused(p) ; inst:setTransform(x,y,z,yaw,scale)
--
-- Static instances are posed ONCE at build; animated ones re-pose per frame. Collision must be
-- emitted inside the map build's beginStatic/endStatic window — maps.lua orders this correctly.

local Models = {}
Models.__index = Models

function Models.new(world)
    local self = setmetatable({}, Models)
    self.world = world
    self.cache = {}          -- file → IModel (kept for the stage's lifetime; glTF loads are heavy)
    self.list = {}           -- live instances
    self.groups = {}         -- group name → { instances }
    return self
end

local Inst = {}
Inst.__index = Inst

-- drop the model file cache (full unload between maps; instances must already be cleared)
function Models:purgeCache()
    self.cache = {}
end

function Models:loadModel(file)
    local m = self.cache[file]
    if m == nil then
        local ok, loaded = pcall(function() return MODEL:Load(file) end)
        if not ok or loaded == nil then return nil end
        m = loaded
        m:Register(self.world.scene)
        self.cache[file] = m
    end
    return m
end

-- create an instance (renderable immediately; collision happens via emitCollision during map build)
function Models:add(o)
    local model = self:loadModel(o.file)
    if model == nil then return nil end
    local world = self.world
    local scene = world.scene

    local inst = setmetatable({}, Inst)
    inst.models = self
    inst.model = model
    inst.file = o.file
    inst.x, inst.y, inst.z = o.x or 0, o.y or 0, o.z or 0
    inst.yaw, inst.scale = o.yaw or 0, o.scale or 1
    -- anchor="bounds": place by the model's BOUNDS instead of its file origin (many kits keep the
    -- origin at a corner) — the bounds' XZ centre lands on (x,z) and minY sits on y.
    if o.anchor == "bounds" and model.GetBounds then
        local okB, minX, minY, minZ, maxX, _, maxZ = pcall(function() return model:GetBounds() end)
        if okB and minX ~= nil then
            -- remember the bounds anchor so setTransform can re-apply it (a moved instance must
            -- keep its anchor point on the given coords, not drift back to the file origin).
            -- ROTATION MATCHES GltfModel.Pose: x' = c*px + s*pz, z' = -s*px + c*pz — the previous
            -- formula used the opposite handedness, exactly inverting the offset at yaw 90/270
            -- (rotated furniture flew off its tile).
            inst.anchorB = { bcx = (minX + maxX) * 0.5, bcz = (minZ + maxZ) * 0.5, minY = minY }
            local yr = inst.yaw * math.pi / 180
            local c, s = math.cos(yr), math.sin(yr)
            inst.x = inst.x - (inst.anchorB.bcx * c + inst.anchorB.bcz * s) * inst.scale
            inst.z = inst.z - (-inst.anchorB.bcx * s + inst.anchorB.bcz * c) * inst.scale
            inst.y = inst.y - inst.anchorB.minY * inst.scale
        end
    end
    inst.group = o.group
    inst.collide = o.collide or "box"
    inst.box = o.box
    inst.visible = o.visible ~= false
    inst.objs = {}

    -- default object
    inst.obj = scene:NewObject()
    scene:ObjSetLit(inst.obj, world.lit)
    inst.objs[#inst.objs + 1] = inst.obj

    -- per-part routing + flags (needs the additive GltfModel.SetPartObject; guarded for ObjModel).
    -- A `parts` entry targets either a part INDEX (part=N) or, more robustly, all parts of a named
    -- MATERIAL (material="Blue") resolved via PartCount/PartMaterial/MaterialName. Flags: unlit, alpha,
    -- mirror, emissive={r,g,b,scale} (0..1), color={r,g,b} (0..1 flat albedo OVERRIDE — recolours the part).
    inst.parts = {}
    if o.parts and model.SetPartObject then
        local function resolveParts(p)
            if p.material and model.PartCount and model.PartMaterial and model.MaterialName then
                local out = {}
                for i = 0, model:PartCount() - 1 do
                    local mat = model:PartMaterial(i)
                    if mat >= 0 and model:MaterialName(mat) == p.material then out[#out + 1] = i end
                end
                return out
            end
            if p.part ~= nil then return { p.part } end
            return {}
        end
        for _, p in ipairs(o.parts) do
            local idxs = resolveParts(p)
            if #idxs > 0 then
                local po = scene:NewObject()
                scene:ObjSetLit(po, world.lit and not p.unlit)
                if p.unlit then scene:ObjSetLit(po, false) end
                if p.alpha ~= nil then scene:ObjSetPass(po, 1, 255, 255, 255, math.floor(p.alpha)) end
                if p.mirror then scene:ObjSetScreenTex(po, true) end
                if p.emissive and scene.ObjSetEmissive then
                    local e = p.emissive
                    local s = e[4] or 1
                    scene:ObjSetEmissive(po, (e[1] or 1) * s, (e[2] or 1) * s, (e[3] or 1) * s)
                end
                if p.color and scene.ObjSetColor then
                    local col = p.color
                    scene:ObjSetColor(po, col[1] or 1, col[2] or 1, col[3] or 1)
                end
                inst.objs[#inst.objs + 1] = po
                for _, idx in ipairs(idxs) do
                    inst.parts[#inst.parts + 1] = { part = idx, obj = po }
                end
            end
        end
    end

    -- animation state
    local animCount = model:AnimCount()
    if o.anim and animCount > 0 then
        local ai = math.min(o.anim.index or 0, animCount - 1)
        inst.anim = {
            index = ai,
            speed = o.anim.speed or 1,
            loop = o.anim.loop ~= false,
            playing = o.anim.playing ~= false,
            t = 0,
            dur = model:Duration(ai),
        }
    end

    inst:pose()
    -- IMPORTANT: SetPartObject state lives on the shared model — clear the routing after posing so
    -- another instance of the same file doesn't inherit this instance's part mapping.
    if #inst.parts > 0 then
        for _, p in ipairs(inst.parts) do model:SetPartObject(p.part, -1) end
    end
    inst:applyVisible()

    self.list[#self.list + 1] = inst
    if inst.group then
        self.groups[inst.group] = self.groups[inst.group] or {}
        table.insert(self.groups[inst.group], inst)
    end
    return inst
end

-- emit this instance's collision into the CURRENT static build (maps.lua calls during rebuild)
function Models:emitCollision(inst)
    local phys = self.world.phys
    if inst.collide == "none" then return end
    if inst.group then phys:setGroup(inst.group) end
    if inst.collide == "mesh" then
        phys:addModelMesh(inst.model, inst.x, inst.y, inst.z, inst.yaw, inst.scale)
    else -- "box"
        local hx, hy, hz
        if inst.box then
            hx, hy, hz = inst.box[1], inst.box[2], inst.box[3]
            phys:addBox(inst.x - hx, inst.y, inst.z - hz, inst.x + hx, inst.y + hy * 2, inst.z + hz)
        elseif inst.model.GetBounds then
            -- SNUG rotated-AABB fit. The old code used the max-extent as a square radius, so a
            -- 2×1 table got a 2×2 collider — furniture blocked far outside its visual shape.
            local mnx, mny, mnz, mxx, mxy, mxz = inst.model:GetBounds()
            local s = inst.scale
            local bcx, bcz = (mnx + mxx) * 0.5 * s, (mnz + mxz) * 0.5 * s   -- bounds centre (model space)
            local hex, hez = (mxx - mnx) * 0.5 * s, (mxz - mnz) * 0.5 * s   -- half extents
            local yr = inst.yaw * math.pi / 180
            local c, si = math.cos(yr), math.sin(yr)
            local wcx = inst.x + (bcx * c + bcz * si)                        -- world footprint centre
            local wcz = inst.z + (-bcx * si + bcz * c)                       -- (Pose handedness)
            local whx = math.abs(hex * c) + math.abs(hez * si)               -- rotated AABB half extents
            local whz = math.abs(hex * si) + math.abs(hez * c)
            phys:addBox(wcx - whx, inst.y + mny * s, wcz - whz, wcx + whx, inst.y + mxy * s, wcz + whz)
        end
    end
    if inst.group then phys:setGroup("default") end
    -- groups declared hidden start passable
    if inst.group and not inst.visible then phys:deferGroupSolid(inst.group, false) end
end

-- per-frame: advance + re-pose animated instances
function Models:update(dt)
    for _, inst in ipairs(self.list) do
        local a = inst.anim
        if a and a.playing and inst.visible then
            a.t = a.t + dt * a.speed
            if a.dur > 0 then
                if a.loop then a.t = a.t % a.dur
                elseif a.t > a.dur then a.t = a.dur; a.playing = false end
            end
            inst:pose()
        end
    end
end

-- toggle a whole group's visibility + solidity (the trigger action)
function Models:toggleGroup(group, visible, solid)
    for _, inst in ipairs(self.groups[group] or {}) do
        if visible ~= nil then inst:setVisible(visible) end
    end
    if solid ~= nil then self.world.phys:setGroupSolid(group, solid) end
end

function Models:clear()
    local scene = self.world.scene
    for _, inst in ipairs(self.list) do
        for _, id in ipairs(inst.objs) do scene:DeleteObject(id) end
    end
    self.list = {}
    self.groups = {}
    -- the model cache is kept: reloading a map reuses parsed GLBs
end

-- remove ONE instance (its scene objects + registry entries). Stages that rebuild a single item
-- must remove-and-re-add (or reuse via setTransform) — re-adding without removing duplicates the
-- geometry at every previous position.
function Models:remove(inst)
    if inst == nil then return end
    local scene = self.world.scene
    for _, id in ipairs(inst.objs or {}) do scene:DeleteObject(id) end
    for i = #self.list, 1, -1 do
        if self.list[i] == inst then table.remove(self.list, i) end
    end
    if inst.group and self.groups[inst.group] then
        local g = self.groups[inst.group]
        for i = #g, 1, -1 do
            if g[i] == inst then table.remove(g, i) end
        end
    end
end

-- ── instance methods ─────────────────────────────────────────────────────────────────────────────
function Inst:pose()
    local m = self.model
    -- restore this instance's part routing for the pose call (cleared afterwards — shared model)
    for _, p in ipairs(self.parts) do m:SetPartObject(p.part, p.obj) end
    local ai, t = -1, 0
    if self.anim then ai, t = self.anim.index, self.anim.t end
    m:Pose(self.models.world.scene, self.obj, ai, t, self.x, self.y, self.z, self.yaw, self.scale)
    for _, p in ipairs(self.parts) do m:SetPartObject(p.part, -1) end
end

function Inst:applyVisible()
    local scene = self.models.world.scene
    for _, id in ipairs(self.objs) do scene:ObjSetVisible(id, self.visible) end
end

function Inst:setVisible(on)
    on = on and true or false
    if on == self.visible then return end
    self.visible = on
    self:applyVisible()
end

function Inst:setTransform(x, y, z, yaw, scale)
    self.yaw = yaw or self.yaw
    self.scale = scale or self.scale
    if self.anchorB and (x or y or z) then
        -- bounds-anchored instance: incoming coords are the ANCHOR point (XZ bounds centre on the
        -- ground) — recompute the origin placement with the current yaw/scale. Rotation matches
        -- GltfModel.Pose (x' = c*px + s*pz, z' = -s*px + c*pz).
        local ax, ay, az = x or self.x, y or self.y, z or self.z
        local yr = self.yaw * math.pi / 180
        local c, s = math.cos(yr), math.sin(yr)
        self.x = ax - (self.anchorB.bcx * c + self.anchorB.bcz * s) * self.scale
        self.z = az - (-self.anchorB.bcx * s + self.anchorB.bcz * c) * self.scale
        self.y = ay - self.anchorB.minY * self.scale
    else
        self.x, self.y, self.z = x or self.x, y or self.y, z or self.z
    end
    self:pose()
    -- NOTE: baked collision does NOT move — a moved collidable model needs a map physics rebuild
end

function Inst:play(index)
    local count = self.model:AnimCount()
    if count == 0 then return end
    index = math.min(index or 0, count - 1)
    self.anim = self.anim or { speed = 1, loop = true }
    self.anim.index = index
    self.anim.t = 0
    self.anim.dur = self.model:Duration(index)
    self.anim.playing = true
end

function Inst:setPaused(p)
    if self.anim then self.anim.playing = not p end
end

return Models
