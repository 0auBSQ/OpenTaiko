<!-- api/3d-physics.md -->

# 3D 引擎：物理 <span class="badge-exp">实验性</span>

刚体、角色与载具、静态碰撞、通过射线检测查询碰撞体，以及导航图。位置使用世界单位，+Y 朝上；[3D 引擎：光栅化世界](3d.md)描述了共用约定。

## 物理

### PHYSICS

<span class="badge-exp">实验性</span>

物理世界的工厂：一个静态三角形集合，加上动态的角色、刚体、盒子和载具刚体。

| 方法 | 说明 |
| --- | --- |
| `PHYSICS:NewWorld()  -> world` | 创建一个物理世界并返回其句柄。 |

### 物理世界句柄

<span class="badge-exp">实验性</span>

<div class="callout warn">
在 BeginStatic 和 EndStatic 之间构建静态几何体，创建刚体，每帧设置它们的速度并调用 Step。刚体用碰撞滑动（球体或有向盒子对三角形）与静态集合碰撞，彼此之间则在水平面内作为球体或胶囊体碰撞，按质量交换动量。
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

-- 每帧
body:SetVelocity(moveX, body:GetVy(), moveZ)
world:Step(dt)
local x, y, z = body:GetX(), body:GetY(), body:GetZ()
```

| 方法 | 说明 |
| --- | --- |
| `world:SetGravity(x, y, z)  -> nil` | 重力向量（默认 0, -32, 0）。 |
| `world:SetFloorMaxAngleY(y)  -> nil` | 接触法线 Y 的阈值（默认 0.5）：高于它的接触视为地面，其余视为墙壁。 |
| `world:BeginStatic()  -> nil` | 开始重建静态集合；清除现有三角形和碰撞体组。 |
| `world:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | 添加一个静态三角形。其法线为 (b - a) x (c - a)；世界会跳过退化三角形。 |
| `world:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | 以两个三角形 (a, b, c) 和 (a, c, d) 添加一个静态四边形。 |
| `world:AddMesh(mc)  -> nil` | 把一个 MeshCollider 的三角形加入集合。 |
| `world:BeginGroup(id)  -> nil` | 把从现在起添加的三角形标记为某个碰撞体组（0 = 默认），以便 SetGroupEnabled 在运行时切换该组。 |
| `world:EndStatic()  -> nil` | 完成集合并构建粗检测网格。 |
| `world:SetGroupEnabled(id, on)  -> nil` | 让一个碰撞体组变为实体或可穿过，而无需重建集合（门、流式加载的室内）。 |
| `world:ClearStatic()  -> nil` | 立即释放静态集合及其后备存储（卸载地图）；同时重置碰撞体组。 |
| `world:NewCharacter(radius)  -> body` | 运动学球体刚体：你设置其速度，世界用碰撞滑动移动它。 |
| `world:NewRigid(radius, mass)  -> body` | 启用重力、与其他刚体交换动量的球体刚体。 |
| `world:NewBox(hx, hy, hz, mass)  -> body` | 启用重力、以给定半边长的有向盒子与静态集合碰撞的刚体（用 SetForward 确定朝向）。 |
| `world:NewVehicle(radius, mass)  -> vehicle` | 汽车式刚体：一个带重力、地面吸附和更陡地面阈值的角色底盘，加上射线检测的车轮。 |
| `world:NewMeshCollider()  -> mc` | 创建一个空的 MeshCollider。 |
| `world:RemoveBody(body)  -> nil` | 从世界中移除一个刚体。 |
| `world:Step(dt)  -> nil` | 把模拟推进 `dt` 秒。世界会对快速刚体进行子步进，以免穿过薄墙。 |
| `world:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> hit, dist, nx, ny, nz` | 对启用的静态三角形投射一条射线（单位方向）；返回射线是否命中任何物体、最近的命中距离和该三角形的法线。 |
| `world:GroundYAt(x, startY, z, reach)  -> number` | (x, startY, z) 正下方 `reach` 范围内的地面高度；没有时为 NaN（用 `y ~= y` 检测）。 |
| `world:StaticTriCount()  -> number` | 静态集合中的三角形数量。 |
| `world:StaticTriCapacity()  -> number` | 三角形列表已分配的容量。 |

### 物理刚体句柄

<span class="badge-exp">实验性</span>

<div class="callout warn">
NewCharacter、NewRigid 和 NewBox 返回此句柄。位置指刚体中心。世界只在水平面内解算刚体之间的接触，并跳过 Y 方向相距超过 2 单位的刚体之间的接触。
</div>

| 方法 | 说明 |
| --- | --- |
| `body:SetPos(x, y, z)  -> nil` | 设置中心位置。 |
| `body:SetVelocity(x, y, z)  -> nil` | 设置速度。 |
| `body:AddVelocity(x, y, z)  -> nil` | 累加速度。 |
| `body:SetRadius(r)  -> nil` | 球体和粗检测半径（最小 0.01）。 |
| `body:SetCapsule(halfLen)  -> nil` | 在刚体之间的接触中使用沿前向轴、给定半长的水平胶囊体；静态碰撞仍使用球体半径。 |
| `body:SetBox(hx, hy, hz)  -> nil` | 以半边长为（右、上、前）的有向盒子与静态集合碰撞。 |
| `body:SetForward(fx, fz)  -> nil` | 决定胶囊体或盒子朝向的水平前向轴（自动归一化）。 |
| `body:SetGravityEnabled(g)  -> nil` | 对该刚体应用世界重力。 |
| `body:SetEnabled(e)  -> nil` | Step 会跳过被禁用的刚体。 |
| `body:SetSnap(s)  -> nil` | 让着地的刚体在小间隙内紧贴下坡的地面；没有吸附时，下坡奔跑的刚体会离开地面并跳起。 |
| `body:SetSmoothContacts(on)  -> nil` | 角色求解器变体：每次迭代解算最深的接触、焊接地面接缝、记录墙壁法线并向下踏到可行走的地面。默认关闭。 |
| `body:SetCollisionLayer(l)  -> nil` | 刚体之间接触所用的碰撞层索引 0..30（默认 0）。 |
| `body:SetCollisionMask(m)  -> nil` | 该刚体会与之碰撞的层的位掩码（第 i 位 = 第 i 层）；默认全部，0 = 无。 |
| `body:GetPos()  -> x, y, z` | 中心位置。 |
| `body:GetVelocity()  -> x, y, z` | 速度。 |
| `body:GetX()  -> number` | 中心 X。 |
| `body:GetY()  -> number` | 中心 Y。 |
| `body:GetZ()  -> number` | 中心 Z。 |
| `body:GetVx()  -> number` | 速度 X。 |
| `body:GetVy()  -> number` | 速度 Y。 |
| `body:GetVz()  -> number` | 速度 Z。 |
| `body:Speed()  -> number` | 速度大小。 |
| `body:IsOnFloor()  -> boolean` | 刚体在上一步中落在地面上时为 true。 |
| `body:HitWall()  -> boolean` | 刚体在上一步中碰到墙壁时为 true。 |
| `body:GetWallNx()  -> number` | 最近一次墙壁接触法线的 X（SetSmoothContacts 开启时设置）。 |
| `body:GetWallNz()  -> number` | 最近一次墙壁接触法线的 Z（SetSmoothContacts 开启时设置）。 |
| `body:GetImpulseX()  -> number` | 上一步中刚体之间的接触施加的净速度变化的 X。 |
| `body:GetImpulseZ()  -> number` | 上一步中刚体之间的接触施加的净速度变化的 Z。 |

### 载具刚体句柄

<span class="badge-exp">实验性</span>

<div class="callout warn">
NewVehicle 返回此句柄。底盘是一个由 Step 照常移动的角色刚体；每次调用 UpdateWheels 都会把车轮作为射线从底盘向下投射，给出多轮着地标志、每个车轮的悬挂压缩量，以及跟随坡度的底盘俯仰和翻滚。你的驾驶代码负责设置底盘速度。
</div>

| 方法 | 说明 |
| --- | --- |
| `vehicle:AddWheel(lx, lz, radius, rest, steered, powered)  -> wheel` | 在底盘空间偏移（`lx` 向右，`lz` 向前）处添加一个车轮，具有给定半径和悬挂静止长度。载具为你的驾驶代码存储 `steered` 和 `powered`，自身从不读取它们。 |
| `vehicle:GetWheel(i)  -> wheel` | 车轮 `i`（从 0 起），或 nil。 |
| `vehicle:WheelCount()  -> number` | 车轮数量。 |
| `vehicle:UpdateWheels(dt, speed)  -> nil` | 对车轮做射线检测，并更新着地、压缩量、俯仰、翻滚和车轮旋转（`speed` 以世界单位每秒计）。在 Step 之后每帧调用一次。 |
| `vehicle.Body  -> body` | 底盘刚体句柄。 |
| `vehicle:SetPos(x, y, z)  -> nil` | 设置底盘位置。 |
| `vehicle:SetVelocity(x, y, z)  -> nil` | 设置底盘速度。 |
| `vehicle:AddVelocity(x, y, z)  -> nil` | 累加底盘速度。 |
| `vehicle:SetForward(fx, fz)  -> nil` | 设置前向轴并推导朝向。 |
| `vehicle:SetYaw(y)  -> nil` | 以弧度设置朝向并推导前向轴。 |
| `vehicle:SetGravityEnabled(g)  -> nil` | 对底盘应用重力。 |
| `vehicle:SetEnabled(e)  -> nil` | 启用或禁用底盘刚体。 |
| `vehicle:SetCapsule(halfLen)  -> nil` | 底盘与刚体接触所用的胶囊体半长。 |
| `vehicle:SetRadius(r)  -> nil` | 底盘半径。 |
| `vehicle:GetX()  -> number` | 底盘 X。 |
| `vehicle:GetY()  -> number` | 底盘 Y。 |
| `vehicle:GetZ()  -> number` | 底盘 Z。 |
| `vehicle:GetVx()  -> number` | 底盘速度 X。 |
| `vehicle:GetVy()  -> number` | 底盘速度 Y。 |
| `vehicle:GetVz()  -> number` | 底盘速度 Z。 |
| `vehicle:IsOnFloor()  -> boolean` | 至少三个车轮着地或底盘落在地面上时为 true。 |
| `vehicle:HitWall()  -> boolean` | 底盘在上一步中碰到墙壁时为 true。 |
| `vehicle:GetImpulseX()  -> number` | 刚体之间的接触施加给底盘的速度变化的 X。 |
| `vehicle:GetImpulseZ()  -> number` | 刚体之间的接触施加给底盘的速度变化的 Z。 |
| `vehicle:GetPitch()  -> number` | 底盘俯仰（弧度），向车轮接触点平滑过渡。 |
| `vehicle:GetRoll()  -> number` | 底盘翻滚（弧度），向车轮接触点平滑过渡。 |

### 车轮句柄

<span class="badge-exp">实验性</span>

AddWheel 和 GetWheel 返回此句柄；在 UpdateWheels 之后读取它。

| 方法 | 说明 |
| --- | --- |
| `wheel:IsGrounded()  -> boolean` | 车轮接触地面时为 true。 |
| `wheel:GetCompression()  -> number` | 悬挂压缩量，从 0（完全伸展或悬空）到 1（完全压缩）。 |
| `wheel:GetSpin()  -> number` | 供车轮视觉使用的累积滚动角度（弧度）。 |

### MeshCollider 句柄

<span class="badge-exp">实验性</span>

用于世界静态集合的三角形列表；可手动填充或用 model:BuildCollider 填充。

| 方法 | 说明 |
| --- | --- |
| `mc:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | 添加一个三角形。 |
| `mc:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | 以两个三角形添加一个四边形。 |
| `mc:TriCount()  -> number` | 已收集的三角形数量。 |
| `mc:Clear()  -> nil` | 移除所有三角形。 |
| `mc:AddToWorld(world)  -> nil` | 把三角形加入某个世界的静态集合（在 BeginStatic 和 EndStatic 之间）；与 world:AddMesh(mc) 相同。 |

## 碰撞体与射线检测

### COLLIDERS

<span class="badge-exp">实验性</span>

用于命中测试的查询形状（球体、轴对齐盒子、胶囊体）和射线的工厂，独立于任何物理世界。

| 方法 | 说明 |
| --- | --- |
| `COLLIDERS:Sphere(cx, cy, cz, r)  -> collider` | 以 (cx, cy, cz) 为中心、半径为 `r` 的球体。 |
| `COLLIDERS:Box(cx, cy, cz, hx, hy, hz)  -> collider` | 以 (cx, cy, cz) 为中心、半边长为 (hx, hy, hz) 的轴对齐盒子。 |
| `COLLIDERS:Capsule(cx, cy, cz, hsx, hsy, hsz, r)  -> collider` | 围绕从中心 - (hsx, hsy, hsz) 到中心 + (hsx, hsy, hsz) 线段、半径为 `r` 的胶囊体。 |
| `COLLIDERS:CapsuleY(cx, cy, cz, halfHeight, r)  -> collider` | 给定半高和半径的垂直胶囊体。 |
| `COLLIDERS:Ray(ox, oy, oz, dx, dy, dz, len)  -> ray` | 带有原点、方向（自动归一化）和长度的可复用射线。 |

### 碰撞体句柄

<span class="badge-exp">实验性</span>

| 方法 | 说明 |
| --- | --- |
| `collider:SetCenter(x, y, z)  -> nil` | 把形状移动到新的中心。 |
| `collider:SetRadius(r)  -> nil` | 设置半径（仅球体碰撞体）。 |
| `collider:SetHalfExtents(hx, hy, hz)  -> nil` | 设置半边长（仅盒子碰撞体）。 |
| `collider:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> number` | 沿射线（方向自动归一化）到 `maxDist` 内第一次命中的距离；未命中时为 -1。 |
| `collider:HitsRay(ray)  -> boolean` | 射线在其长度内命中该形状时为 true。 |
| `collider:CollideWith(other)  -> boolean` | 该形状与另一个碰撞体重叠，或 `other` 是命中它的射线时为 true。 |
| `collider:SetTag(tag)  -> nil` | 附加任意 Lua 值，以便你把命中映射回自己的对象。 |
| `collider:GetTag()  -> any` | 附加的值。 |

### 射线句柄

<span class="badge-exp">实验性</span>

| 方法 | 说明 |
| --- | --- |
| `ray:Set(ox, oy, oz, dx, dy, dz, len)  -> nil` | 重置原点、方向（自动归一化）和长度。 |

## 寻路

### PATHFIND

<span class="badge-exp">实验性</span>

FindPath 用 A* 搜索的带权导航图的工厂。

| 方法 | 说明 |
| --- | --- |
| `PATHFIND:NewGraph()  -> graph` | 创建一个空图并返回其句柄。 |

### 导航图句柄

<span class="badge-exp">实验性</span>

<div class="callout warn">
节点带有世界位置；索引从 1 起。每条边带有权重和一个通过点，即移动者走这条边时经过的位置（例如门口的中点）。FindPath 返回它所走各边的通过点，最后是目标点。
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

| 方法 | 说明 |
| --- | --- |
| `graph:AddNode(x, y, z)  -> number` | 添加一个节点并返回其索引。 |
| `graph:AddEdge(a, b, w, px, py, pz)  -> nil` | 添加一条从节点 `a` 到节点 `b`、权重为 `w`、经过通过点 (px, py, pz) 的有向边。 |
| `graph:Link(a, b, w, px, py, pz)  -> nil` | 添加双向的边，共用一个通过点。 |
| `graph:NearestNode(x, y, z)  -> number` | 距 (x, y, z) 最近的节点的索引；图为空时为 0。 |
| `graph:FindPath(start, goal, goalX, goalY, goalZ)  -> path` | 从节点 `start` 到节点 `goal` 运行 A*；返回一条以 (goalX, goalY, goalZ) 结尾的路径，不可达时为 nil。 |
| `graph:NodeCount()  -> number` | 节点数量。 |
| `graph:Clear()  -> nil` | 移除所有节点和边。 |

### 导航路径句柄

<span class="badge-exp">实验性</span>

路径点索引从 1 起。

| 方法 | 说明 |
| --- | --- |
| `path:Count()  -> number` | 路径点数量。 |
| `path:X(i)  -> number` | 路径点 `i` 的 X。 |
| `path:Y(i)  -> number` | 路径点 `i` 的 Y。 |
| `path:Z(i)  -> number` | 路径点 `i` 的 Z。 |
