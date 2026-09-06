<!-- api/3d-physics.md -->

# 3D engine: physics <span class="badge-exp">Experimental</span>

Rigid bodies, characters and vehicles, static collision, collider queries with raycasts, and navigation graphs. Positions are world units and +Y is up; [3D engine: rasterizer world](3d.md) describes the shared conventions.

## Physics

### PHYSICS

<span class="badge-exp">Experimental</span>

Factory for physics worlds: a static triangle soup plus dynamic character, rigid, box and vehicle bodies.

| Method | Description |
| --- | --- |
| `PHYSICS:NewWorld()  -> world` | Create a physics world and return its handle. |

### Physics world handle

<span class="badge-exp">Experimental</span>

<div class="callout warn">
Build the static geometry between BeginStatic and EndStatic, create bodies, set their velocity each frame and call Step. Bodies collide with the static soup using collide-and-slide (sphere or oriented box against triangles) and with each other as spheres or capsules in the horizontal plane, trading momentum by mass.
</div>

```lua
local world = PHYSICS:NewWorld()
world:SetGravity(0, -32, 0)
world:BeginStatic()
world:AddQuad(-20, 0, -20,  -20, 0, 20,  20, 0, 20,  20, 0, -20)
world:EndStatic()

local body = world:NewCharacter(0.35)
body:SetGravityEnabled(true)
body:SetPos(0, 0.35, 0)

-- each frame
body:SetVelocity(moveX, body:GetVy(), moveZ)
world:Step(dt)
local x, y, z = body:GetX(), body:GetY(), body:GetZ()
```

| Method | Description |
| --- | --- |
| `world:SetGravity(x, y, z)  -> nil` | Gravity vector (default 0, -32, 0). |
| `world:SetFloorMaxAngleY(y)  -> nil` | Contact normal Y threshold (default 0.5): contacts above it count as floor, the rest as wall. |
| `world:BeginStatic()  -> nil` | Start rebuilding the static soup; clears existing triangles and collider groups. |
| `world:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | Add a static triangle. Its normal is (b - a) x (c - a); the world skips degenerate triangles. |
| `world:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | Add a static quad as two triangles (a, b, c) and (a, c, d). |
| `world:AddMesh(mc)  -> nil` | Add a MeshCollider's triangles to the soup. |
| `world:BeginGroup(id)  -> nil` | Tag triangles added from now on with a collider group (0 = default) so SetGroupEnabled can toggle the group at runtime. |
| `world:EndStatic()  -> nil` | Finish the soup and build the broadphase grid. |
| `world:SetGroupEnabled(id, on)  -> nil` | Make a collider group solid or passable without rebuilding the soup (doors, streamed interiors). |
| `world:ClearStatic()  -> nil` | Release the static soup and its backing storage now (map unload); also resets collider groups. |
| `world:NewCharacter(radius)  -> body` | Kinematic sphere body: you set its velocity, the world moves it with collide-and-slide. |
| `world:NewRigid(radius, mass)  -> body` | Sphere body with gravity enabled that trades momentum with other bodies. |
| `world:NewBox(hx, hy, hz, mass)  -> body` | Gravity-enabled body that collides with the static soup as an oriented box with the given half-extents (orient it with SetForward). |
| `world:NewVehicle(radius, mass)  -> vehicle` | Car-style body: a character chassis with gravity, ground snap and a steeper floor threshold, plus raycast wheels. |
| `world:NewMeshCollider()  -> mc` | Create an empty MeshCollider. |
| `world:RemoveBody(body)  -> nil` | Remove a body from the world. |
| `world:Step(dt)  -> nil` | Advance the simulation by `dt` seconds. The world sub-steps fast bodies so they do not tunnel through thin walls. |
| `world:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> hit, dist, nx, ny, nz` | Cast a ray (unit direction) against the enabled static triangles; returns whether the ray hit anything, the nearest hit distance and that triangle's normal. |
| `world:GroundYAt(x, startY, z, reach)  -> number` | Ground height straight below (x, startY, z) within `reach`, or NaN when there is none (test with `y ~= y`). |
| `world:StaticTriCount()  -> number` | Number of triangles in the static soup. |
| `world:StaticTriCapacity()  -> number` | Allocated capacity of the triangle list. |

### Physics body handle

<span class="badge-exp">Experimental</span>

<div class="callout warn">
NewCharacter, NewRigid and NewBox return this handle. Positions refer to the body centre. The world resolves body-versus-body contacts in the horizontal plane only and skips them between bodies more than 2 units apart in Y.
</div>

| Method | Description |
| --- | --- |
| `body:SetPos(x, y, z)  -> nil` | Set the centre position. |
| `body:SetVelocity(x, y, z)  -> nil` | Set the velocity. |
| `body:AddVelocity(x, y, z)  -> nil` | Add to the velocity. |
| `body:SetRadius(r)  -> nil` | Sphere and broadphase radius (minimum 0.01). |
| `body:SetCapsule(halfLen)  -> nil` | Use a horizontal capsule of this half-length along the forward axis for body-versus-body contacts; static collision keeps using the sphere radius. |
| `body:SetBox(hx, hy, hz)  -> nil` | Collide with the static soup as an oriented box with half-extents (right, up, forward). |
| `body:SetForward(fx, fz)  -> nil` | Horizontal forward axis (normalised automatically) that orients the capsule or box. |
| `body:SetGravityEnabled(g)  -> nil` | Apply world gravity to this body. |
| `body:SetEnabled(e)  -> nil` | Step skips disabled bodies. |
| `body:SetSnap(s)  -> nil` | Keep a grounded body glued to descending ground within a small gap; without snap, a body running downhill leaves the ground and hops. |
| `body:SetSmoothContacts(on)  -> nil` | Character solver variant: resolves the deepest contact per iteration, welds floor seams, records wall normals and steps down onto walkable ground. Off by default. |
| `body:SetCollisionLayer(l)  -> nil` | Collision layer index 0..30 for body-versus-body contacts (default 0). |
| `body:SetCollisionMask(m)  -> nil` | Bitmask of layers this body collides with (bit i = layer i); default all, 0 = none. |
| `body:GetPos()  -> x, y, z` | Centre position. |
| `body:GetVelocity()  -> x, y, z` | Velocity. |
| `body:GetX()  -> number` | Centre X. |
| `body:GetY()  -> number` | Centre Y. |
| `body:GetZ()  -> number` | Centre Z. |
| `body:GetVx()  -> number` | Velocity X. |
| `body:GetVy()  -> number` | Velocity Y. |
| `body:GetVz()  -> number` | Velocity Z. |
| `body:Speed()  -> number` | Velocity magnitude. |
| `body:IsOnFloor()  -> boolean` | True when the body rested on floor during the last step. |
| `body:HitWall()  -> boolean` | True when the body touched a wall during the last step. |
| `body:GetWallNx()  -> number` | X of the last wall contact normal (set when SetSmoothContacts is on). |
| `body:GetWallNz()  -> number` | Z of the last wall contact normal (set when SetSmoothContacts is on). |
| `body:GetImpulseX()  -> number` | X of the net velocity change dealt by body-versus-body contacts during the last step. |
| `body:GetImpulseZ()  -> number` | Z of the net velocity change dealt by body-versus-body contacts during the last step. |

### Vehicle body handle

<span class="badge-exp">Experimental</span>

<div class="callout warn">
NewVehicle returns this handle. The chassis is a character body that Step moves as usual; each UpdateWheels call casts the wheels as rays down from the chassis, giving a multi-wheel grounded flag, per-wheel suspension compression and a chassis pitch and roll that follow the slope. Your driving code sets the chassis velocity.
</div>

| Method | Description |
| --- | --- |
| `vehicle:AddWheel(lx, lz, radius, rest, steered, powered)  -> wheel` | Add a wheel at chassis-space offset (`lx` right, `lz` forward) with the given radius and suspension rest length. The vehicle stores `steered` and `powered` for your driving code and never reads them itself. |
| `vehicle:GetWheel(i)  -> wheel` | Wheel `i` (0-based), or nil. |
| `vehicle:WheelCount()  -> number` | Number of wheels. |
| `vehicle:UpdateWheels(dt, speed)  -> nil` | Raycast the wheels and update grounded, compression, pitch, roll and wheel spin (`speed` in world units per second). Call once per frame after Step. |
| `vehicle.Body  -> body` | The chassis body handle. |
| `vehicle:SetPos(x, y, z)  -> nil` | Set the chassis position. |
| `vehicle:SetVelocity(x, y, z)  -> nil` | Set the chassis velocity. |
| `vehicle:AddVelocity(x, y, z)  -> nil` | Add to the chassis velocity. |
| `vehicle:SetForward(fx, fz)  -> nil` | Set the forward axis and derive the heading. |
| `vehicle:SetYaw(y)  -> nil` | Set the heading in radians and derive the forward axis. |
| `vehicle:SetGravityEnabled(g)  -> nil` | Apply gravity to the chassis. |
| `vehicle:SetEnabled(e)  -> nil` | Enable or disable the chassis body. |
| `vehicle:SetCapsule(halfLen)  -> nil` | Capsule half-length for chassis-versus-body contacts. |
| `vehicle:SetRadius(r)  -> nil` | Chassis radius. |
| `vehicle:GetX()  -> number` | Chassis X. |
| `vehicle:GetY()  -> number` | Chassis Y. |
| `vehicle:GetZ()  -> number` | Chassis Z. |
| `vehicle:GetVx()  -> number` | Chassis velocity X. |
| `vehicle:GetVy()  -> number` | Chassis velocity Y. |
| `vehicle:GetVz()  -> number` | Chassis velocity Z. |
| `vehicle:IsOnFloor()  -> boolean` | True when at least three wheels are grounded or the chassis rests on floor. |
| `vehicle:HitWall()  -> boolean` | True when the chassis touched a wall during the last step. |
| `vehicle:GetImpulseX()  -> number` | X of the body-versus-body velocity change dealt to the chassis. |
| `vehicle:GetImpulseZ()  -> number` | Z of the body-versus-body velocity change dealt to the chassis. |
| `vehicle:GetPitch()  -> number` | Chassis pitch in radians, eased toward the wheel contacts. |
| `vehicle:GetRoll()  -> number` | Chassis roll in radians, eased toward the wheel contacts. |

### Wheel handle

<span class="badge-exp">Experimental</span>

AddWheel and GetWheel return this handle; read it after UpdateWheels.

| Method | Description |
| --- | --- |
| `wheel:IsGrounded()  -> boolean` | True when the wheel touches the ground. |
| `wheel:GetCompression()  -> number` | Suspension compression from 0 (fully extended or airborne) to 1 (fully compressed). |
| `wheel:GetSpin()  -> number` | Accumulated roll angle in radians for wheel visuals. |

### MeshCollider handle

<span class="badge-exp">Experimental</span>

A triangle list for a world's static soup; fill it by hand or with model:BuildCollider.

| Method | Description |
| --- | --- |
| `mc:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | Add one triangle. |
| `mc:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | Add a quad as two triangles. |
| `mc:TriCount()  -> number` | Number of triangles collected. |
| `mc:Clear()  -> nil` | Remove all triangles. |
| `mc:AddToWorld(world)  -> nil` | Add the triangles to a world's static soup (between BeginStatic and EndStatic); same as world:AddMesh(mc). |

## Colliders and raycasting

### COLLIDERS

<span class="badge-exp">Experimental</span>

Factory for query shapes (sphere, axis-aligned box, capsule) and rays for hit tests, independent of any physics world.

| Method | Description |
| --- | --- |
| `COLLIDERS:Sphere(cx, cy, cz, r)  -> collider` | Sphere of radius `r` centred at (cx, cy, cz). |
| `COLLIDERS:Box(cx, cy, cz, hx, hy, hz)  -> collider` | Axis-aligned box with half-extents (hx, hy, hz) centred at (cx, cy, cz). |
| `COLLIDERS:Capsule(cx, cy, cz, hsx, hsy, hsz, r)  -> collider` | Capsule of radius `r` around the segment from centre - (hsx, hsy, hsz) to centre + (hsx, hsy, hsz). |
| `COLLIDERS:CapsuleY(cx, cy, cz, halfHeight, r)  -> collider` | Vertical capsule of the given half-height and radius. |
| `COLLIDERS:Ray(ox, oy, oz, dx, dy, dz, len)  -> ray` | Reusable ray with an origin, a direction (normalised automatically) and a length. |

### Collider handle

<span class="badge-exp">Experimental</span>

| Method | Description |
| --- | --- |
| `collider:SetCenter(x, y, z)  -> nil` | Move the shape to a new centre. |
| `collider:SetRadius(r)  -> nil` | Set the radius (sphere colliders only). |
| `collider:SetHalfExtents(hx, hy, hz)  -> nil` | Set the half-extents (box colliders only). |
| `collider:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> number` | Distance along the ray (direction normalised automatically) to the first hit within `maxDist`, or -1 on a miss. |
| `collider:HitsRay(ray)  -> boolean` | True when the ray hits the shape within its length. |
| `collider:CollideWith(other)  -> boolean` | True when the shape overlaps another collider, or when `other` is a ray that hits it. |
| `collider:SetTag(tag)  -> nil` | Attach any Lua value so you can map a hit back to your own object. |
| `collider:GetTag()  -> any` | The attached value. |

### Ray handle

<span class="badge-exp">Experimental</span>

| Method | Description |
| --- | --- |
| `ray:Set(ox, oy, oz, dx, dy, dz, len)  -> nil` | Reset the origin, direction (normalised automatically) and length. |

## Pathfinding

### PATHFIND

<span class="badge-exp">Experimental</span>

Factory for weighted navigation graphs that FindPath searches with A*.

| Method | Description |
| --- | --- |
| `PATHFIND:NewGraph()  -> graph` | Create an empty graph and return its handle. |

### Nav graph handle

<span class="badge-exp">Experimental</span>

<div class="callout warn">
Nodes carry a world position; indices start at 1. Each edge carries a weight and a transit point, the position a mover passes through when taking that edge (for example a doorway midpoint). FindPath returns the transit points of the edges it took, followed by the goal point.
</div>

```lua
local g = PATHFIND:NewGraph()
local a = g:AddNode(0, 0, 0)
local b = g:AddNode(10, 0, 0)
g:Link(a, b, 10, 5, 0, 0)
local path = g:FindPath(g:NearestNode(1, 0, 0), g:NearestNode(9, 0, 0), 9, 0, 0)
if path then
    for i = 1, path:Count() do
        local x, z = path:X(i), path:Z(i)
    end
end
```

| Method | Description |
| --- | --- |
| `graph:AddNode(x, y, z)  -> number` | Add a node and return its index. |
| `graph:AddEdge(a, b, w, px, py, pz)  -> nil` | Add a directed edge from node `a` to node `b` with weight `w` through transit point (px, py, pz). |
| `graph:Link(a, b, w, px, py, pz)  -> nil` | Add edges in both directions through one shared transit point. |
| `graph:NearestNode(x, y, z)  -> number` | Index of the node closest to (x, y, z), or 0 when the graph is empty. |
| `graph:FindPath(start, goal, goalX, goalY, goalZ)  -> path` | Run A* from node `start` to node `goal`; returns a path ending at (goalX, goalY, goalZ), or nil when unreachable. |
| `graph:NodeCount()  -> number` | Number of nodes. |
| `graph:Clear()  -> nil` | Remove all nodes and edges. |

### Nav path handle

<span class="badge-exp">Experimental</span>

Waypoint indices start at 1.

| Method | Description |
| --- | --- |
| `path:Count()  -> number` | Number of waypoints. |
| `path:X(i)  -> number` | X of waypoint `i`. |
| `path:Y(i)  -> number` | Y of waypoint `i`. |
| `path:Z(i)  -> number` | Z of waypoint `i`. |
