---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/physics.lua: named collider groups and characters over PhysicsWorld

local Phys = {}
Phys.__index = Phys

function Phys.new(world)
    local self = setmetatable({}, Phys)
    self.world = world
    self.w = PHYSICS:NewWorld()
    self.w:SetGravity(0, -32, 0)
    self.groups = { default = 0 }        -- name → engine group id
    self._nextGroup = 1
    self._triCount = 0
    self.bodies = {}                     -- controller list (for cleanup)
    return self
end

function Phys:hasGeometry() return self._triCount > 0 end

-- ==== Collider groups ====
function Phys:groupId(name)
    if name == nil or name == "default" then return 0 end
    local id = self.groups[name]
    if id == nil then
        id = self._nextGroup
        self._nextGroup = self._nextGroup + 1
        self.groups[name] = id
    end
    return id
end
-- toggle a named group solid (true) or passable (false)
function Phys:setGroupSolid(name, solid)
    self.w:SetGroupEnabled(self:groupId(name), solid and true or false)
end

-- ==== Static build session ====
-- emit collision between beginStatic() and endStatic()
function Phys:beginStatic()
    self.w:BeginStatic()
    self.groups = { default = 0 }
    self._nextGroup = 1
    self._triCount = 0
    self._pendingSolid = {}              -- groups declared passable during the build
end
function Phys:setGroup(name)
    self.w:BeginGroup(self:groupId(name))
end
function Phys:addTri(ax, ay, az, bx, by, bz, cx, cy, cz)
    self.w:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)
    self._triCount = self._triCount + 1
end
function Phys:addQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)
    self.w:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)
    self._triCount = self._triCount + 2
end
-- axis-aligned box (6 faces)
function Phys:addBox(x0, y0, z0, x1, y1, z1)
    self:addQuad(x0,y0,z0, x1,y0,z0, x1,y1,z0, x0,y1,z0)   -- -z
    self:addQuad(x1,y0,z1, x0,y0,z1, x0,y1,z1, x1,y1,z1)   -- +z
    self:addQuad(x0,y0,z1, x0,y0,z0, x0,y1,z0, x0,y1,z1)   -- -x
    self:addQuad(x1,y0,z0, x1,y0,z1, x1,y1,z1, x1,y1,z0)   -- +x
    self:addQuad(x0,y1,z0, x1,y1,z0, x1,y1,z1, x0,y1,z1)   -- top
    self:addQuad(x0,y0,z1, x1,y0,z1, x1,y0,z0, x0,y0,z0)   -- bottom
end
-- model triangles (bind pose, placed) via GltfModel.BuildCollider
function Phys:addModelMesh(model, x, y, z, yaw, scale)
    if model == nil or model.BuildCollider == nil then return end
    local mc = self.w:NewMeshCollider()
    model:BuildCollider(mc, x, y, z, yaw or 0, scale or 1)
    self.w:AddMesh(mc)
    self._triCount = self._triCount + mc:TriCount()
end
-- declare a group's initial solidity during the build (applied at endStatic)
function Phys:deferGroupSolid(name, solid)
    self._pendingSolid[#self._pendingSolid + 1] = { name = name, solid = solid and true or false }
end
function Phys:endStatic()
    self.w:EndStatic()
    for _, p in ipairs(self._pendingSolid or {}) do
        self:setGroupSolid(p.name, p.solid)
    end
    self._pendingSolid = {}
end

-- map unload: unlike beginStatic, also trims the C# backing storage
function Phys:clearGeometry()
    if self.w.ClearStatic then self.w:ClearStatic()
    else self.w:BeginStatic(); self.w:EndStatic() end
    self.groups = { default = 0 }
    self._nextGroup = 1
    self._triCount = 0
    self._pendingSolid = {}
end

-- ==== Queries ====
-- returns hit distance (or nil) + normal
function Phys:raycast(ox, oy, oz, dx, dy, dz, maxDist)
    local hit, dist, nx, ny, nz = self.w:Raycast(ox, oy, oz, dx, dy, dz, maxDist)
    if hit then return dist, nx, ny, nz end
    return nil
end
-- ground height straight below (x,z) scanning down from startY, or nil
function Phys:groundY(x, startY, z, reach)
    local gy = self.w:GroundYAt(x, startY, z, reach or 6)
    if gy ~= gy then return nil end   -- NaN
    return gy
end

-- ==== Character controller ====
local Char = {}
Char.__index = Char

-- opts: radius, x/y/z spawn, accel, decel, jumpVel, coyote,
--       layer ("player" | "npc" | number), dynamicCollide (default false: characters pass through each other)
local LAYERS = { player = 1, npc = 2 }
function Phys:newCharacter(opts)
    opts = opts or {}
    local c = setmetatable({}, Char)
    c.phys = self
    c.body = self.w:NewCharacter(opts.radius or 0.32)
    c.body:SetGravityEnabled(true)
    if c.body.SetSmoothContacts then
        -- smooth-contact solver has its own step-down snap
        c.body:SetSmoothContacts(true)
        c.body:SetSnap(false)
    else
        c.body:SetSnap(true)                      -- fallback: snap to descending slopes
    end
    if c.body.SetCollisionMask then
        local layer = LAYERS[opts.layer] or (type(opts.layer) == "number" and opts.layer) or 1
        c.body:SetCollisionLayer(layer)
        if not opts.dynamicCollide then c.body:SetCollisionMask(0) end   -- static geometry only
    end
    c.body:SetPos(opts.x or 0, (opts.y or 0) + (opts.radius or 0.32), opts.z or 0)
    c.radius   = opts.radius or 0.32
    c.accel    = opts.accel or 13
    c.decel    = opts.decel or 16
    c.jumpVel  = opts.jumpVel or 8.5
    c.coyote   = opts.coyote or 0.12
    c._coyoteT = 0
    self.bodies[#self.bodies + 1] = c
    return c
end

-- feet position (body centre is one radius up)
function Char:pos()
    local b = self.body
    return b:GetX(), b:GetY() - self.radius, b:GetZ()
end
function Char:setPos(x, yFeet, z) self.body:SetPos(x, yFeet + self.radius, z) end
function Char:vel() return self.body:GetVx(), self.body:GetVy(), self.body:GetVz() end
function Char:onFloor() return self.body:IsOnFloor() end
function Char:hitWall() return self.body:HitWall() end

-- steer toward the wish direction; jump when grounded (coyote time)
-- world Step() integrates and collides
function Char:move(dt, wishX, wishZ, targetSpeed, wantJump)
    local b = self.body
    local vx, vy, vz = b:GetVx(), b:GetVy(), b:GetVz()
    local mag = math.sqrt(wishX * wishX + wishZ * wishZ)
    local txv, tzv = 0, 0
    if mag > 1e-6 then
        txv, tzv = wishX / mag * targetSpeed, wishZ / mag * targetSpeed
    end
    -- project the wish velocity off recent wall normals (slide along walls)
    -- two normals kept 0.25 s: a single one jitters at convex corners
    if b.GetWallNx and b:HitWall() then
        local nx, nz = b:GetWallNx(), b:GetWallNz()
        if self._wallT and self._wallT > 0 and (nx * (self._wnx or 0) + nz * (self._wnz or 0)) < 0.9 then
            self._wn2x, self._wn2z, self._wall2T = self._wnx, self._wnz, 0.25   -- keep the other face
        end
        self._wallT, self._wnx, self._wnz = 0.25, nx, nz
    else
        if self._wallT and self._wallT > 0 then self._wallT = self._wallT - dt end
        if self._wall2T and self._wall2T > 0 then self._wall2T = self._wall2T - dt end
    end
    if mag > 1e-6 then
        if self._wallT and self._wallT > 0 then
            local into = txv * self._wnx + tzv * self._wnz
            if into < 0 then txv = txv - into * self._wnx; tzv = tzv - into * self._wnz end
        end
        if self._wall2T and self._wall2T > 0 then
            local into2 = txv * self._wn2x + tzv * self._wn2z
            if into2 < 0 then txv = txv - into2 * self._wn2x; tzv = tzv - into2 * self._wn2z end
        end
    end
    local k = 1 - math.exp(-((mag > 1e-6) and self.accel or self.decel) * dt)
    vx = vx + (txv - vx) * k
    vz = vz + (tzv - vz) * k
    -- coyote time: jump grace after leaving an edge
    if b:IsOnFloor() then self._coyoteT = self.coyote else self._coyoteT = math.max(0, self._coyoteT - dt) end
    if wantJump and self._coyoteT > 0 then
        vy = self.jumpVel
        self._coyoteT = 0
    end
    b:SetVelocity(vx, vy, vz)
end

function Char:remove()
    self.phys.w:RemoveBody(self.body)
    for i = #self.phys.bodies, 1, -1 do
        if self.phys.bodies[i] == self then table.remove(self.phys.bodies, i) end
    end
end

-- ==== Camera boom helper ====
-- one-call probe for stages (camera.lua uses raycast)
function Phys:cameraClearance(ox, oy, oz, dx, dy, dz, maxDist, pad)
    pad = pad or 0.32                            -- = character radius
    local best = maxDist
    local d = self:raycast(ox, oy, oz, dx, dy, dz, maxDist + pad)
    if d and d - pad < best then best = d - pad end
    if best < 0.4 then best = 0.4 end
    return best
end

function Phys:step(dt)
    if dt > 0 then self.w:Step(dt) end
end

return Phys
