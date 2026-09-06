<!-- api/3d-physics.md -->

# 3D 엔진: 물리 <span class="badge-exp">실험적</span>

강체, 캐릭터와 차량, 정적 충돌, 레이캐스트를 이용한 콜라이더 질의, 내비게이션 그래프. 위치는 월드 단위이며 +Y가 위입니다. 공통 규약은 [3D 엔진: 래스터라이저 월드](3d.md)에서 설명합니다.

## 물리

### PHYSICS

<span class="badge-exp">실험적</span>

물리 월드의 팩토리입니다. 정적 삼각형 집합과 동적 캐릭터, 강체, 상자, 차량 바디로 이루어집니다.

| 메서드 | 설명 |
| --- | --- |
| `PHYSICS:NewWorld()  -> world` | 물리 월드를 만들고 핸들을 반환합니다. |

### 물리 월드 핸들

<span class="badge-exp">실험적</span>

<div class="callout warn">
BeginStatic과 EndStatic 사이에 정적 지오메트리를 만들고, 바디를 생성하고, 매 프레임 속도를 설정한 뒤 Step을 호출하십시오. 바디는 충돌-슬라이드(collide-and-slide)로 정적 집합과 충돌하고(구 또는 방향 상자 대 삼각형), 서로는 수평면에서 구 또는 캡슐로 충돌하며 질량에 따라 운동량을 교환합니다.
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

-- 매 프레임
body:SetVelocity(moveX, body:GetVy(), moveZ)
world:Step(dt)
local x, y, z = body:GetX(), body:GetY(), body:GetZ()
```

| 메서드 | 설명 |
| --- | --- |
| `world:SetGravity(x, y, z)  -> nil` | 중력 벡터(기본값 0, -32, 0). |
| `world:SetFloorMaxAngleY(y)  -> nil` | 접촉 법선 Y 임계값(기본값 0.5). 이 값보다 큰 접촉은 바닥으로, 나머지는 벽으로 셉니다. |
| `world:BeginStatic()  -> nil` | 정적 집합 재구축을 시작합니다. 기존 삼각형과 콜라이더 그룹을 지웁니다. |
| `world:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | 정적 삼각형을 추가합니다. 법선은 (b - a) x (c - a)이며, 퇴화 삼각형은 월드가 건너뜁니다. |
| `world:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | 정적 쿼드를 두 삼각형 (a, b, c)와 (a, c, d)로 추가합니다. |
| `world:AddMesh(mc)  -> nil` | MeshCollider의 삼각형을 집합에 추가합니다. |
| `world:BeginGroup(id)  -> nil` | 이후 추가되는 삼각형에 콜라이더 그룹(0 = 기본값)을 태그해 SetGroupEnabled로 런타임에 그룹을 토글할 수 있게 합니다. |
| `world:EndStatic()  -> nil` | 집합을 마무리하고 브로드페이즈 그리드를 만듭니다. |
| `world:SetGroupEnabled(id, on)  -> nil` | 집합을 다시 만들지 않고 콜라이더 그룹을 단단하게 또는 통과 가능하게 만듭니다(문, 스트리밍되는 실내). |
| `world:ClearStatic()  -> nil` | 정적 집합과 그 저장 공간을 지금 놓습니다(맵 언로드). 콜라이더 그룹도 재설정됩니다. |
| `world:NewCharacter(radius)  -> body` | 키네마틱 구 바디: 속도를 설정하면 월드가 충돌-슬라이드로 움직입니다. |
| `world:NewRigid(radius, mass)  -> body` | 중력이 켜져 있고 다른 바디와 운동량을 교환하는 구 바디. |
| `world:NewBox(hx, hy, hz, mass)  -> body` | 지정한 반폭의 방향 상자로 정적 집합과 충돌하는 중력 켜진 바디(SetForward로 방향 지정). |
| `world:NewVehicle(radius, mass)  -> vehicle` | 자동차형 바디: 중력, 지면 스냅, 더 가파른 바닥 기준을 가진 캐릭터 차체와 레이캐스트 바퀴. |
| `world:NewMeshCollider()  -> mc` | 빈 MeshCollider를 만듭니다. |
| `world:RemoveBody(body)  -> nil` | 월드에서 바디를 제거합니다. |
| `world:Step(dt)  -> nil` | 시뮬레이션을 `dt`초만큼 전진시킵니다. 월드는 빠른 바디가 얇은 벽을 통과하지 않도록 서브스텝합니다. |
| `world:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> hit, dist, nx, ny, nz` | 활성 정적 삼각형에 대해 광선(단위 방향)을 쏩니다. 광선이 무언가에 맞았는지, 가장 가까운 충돌 거리, 그 삼각형의 법선을 반환합니다. |
| `world:GroundYAt(x, startY, z, reach)  -> number` | (x, startY, z) 바로 아래 `reach` 안의 지면 높이. 없으면 NaN(`y ~= y`로 검사). |
| `world:StaticTriCount()  -> number` | 정적 집합의 삼각형 수. |
| `world:StaticTriCapacity()  -> number` | 삼각형 목록의 할당 용량. |

### 물리 바디 핸들

<span class="badge-exp">실험적</span>

<div class="callout warn">
NewCharacter, NewRigid, NewBox가 이 핸들을 반환합니다. 위치는 바디 중심을 가리킵니다. 월드는 바디 대 바디 접촉을 수평면에서만 해결하며 Y로 2단위 넘게 떨어진 바디 사이에서는 건너뜁니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `body:SetPos(x, y, z)  -> nil` | 중심 위치를 설정합니다. |
| `body:SetVelocity(x, y, z)  -> nil` | 속도를 설정합니다. |
| `body:AddVelocity(x, y, z)  -> nil` | 속도에 더합니다. |
| `body:SetRadius(r)  -> nil` | 구와 브로드페이즈 반지름(최소 0.01). |
| `body:SetCapsule(halfLen)  -> nil` | 바디 대 바디 접촉에 전방 축을 따라 이 반길이의 수평 캡슐을 사용합니다. 정적 충돌은 계속 구 반지름을 사용합니다. |
| `body:SetBox(hx, hy, hz)  -> nil` | 반폭(오른쪽, 위, 앞)의 방향 상자로 정적 집합과 충돌합니다. |
| `body:SetForward(fx, fz)  -> nil` | 캡슐이나 상자의 방향을 정하는 수평 전방 축(자동 정규화). |
| `body:SetGravityEnabled(g)  -> nil` | 이 바디에 월드 중력을 적용합니다. |
| `body:SetEnabled(e)  -> nil` | Step은 비활성 바디를 건너뜁니다. |
| `body:SetSnap(s)  -> nil` | 지면에 있는 바디가 작은 틈 안에서 내려가는 지면에 붙어 있게 합니다. 스냅이 없으면 내리막을 달리는 바디는 지면에서 떨어져 튀어 오릅니다. |
| `body:SetSmoothContacts(on)  -> nil` | 캐릭터 솔버 변형: 반복마다 가장 깊은 접촉을 해결하고, 바닥 이음새를 잇고, 벽 법선을 기록하며, 걸을 수 있는 지면으로 내려섭니다. 기본값 꺼짐. |
| `body:SetCollisionLayer(l)  -> nil` | 바디 대 바디 접촉의 충돌 레이어 인덱스 0..30(기본값 0). |
| `body:SetCollisionMask(m)  -> nil` | 이 바디가 충돌하는 레이어의 비트마스크(비트 i = 레이어 i). 기본값 전체, 0 = 없음. |
| `body:GetPos()  -> x, y, z` | 중심 위치. |
| `body:GetVelocity()  -> x, y, z` | 속도. |
| `body:GetX()  -> number` | 중심 X. |
| `body:GetY()  -> number` | 중심 Y. |
| `body:GetZ()  -> number` | 중심 Z. |
| `body:GetVx()  -> number` | 속도 X. |
| `body:GetVy()  -> number` | 속도 Y. |
| `body:GetVz()  -> number` | 속도 Z. |
| `body:Speed()  -> number` | 속도 크기. |
| `body:IsOnFloor()  -> boolean` | 마지막 스텝 동안 바디가 바닥에 있었으면 true. |
| `body:HitWall()  -> boolean` | 마지막 스텝 동안 바디가 벽에 닿았으면 true. |
| `body:GetWallNx()  -> number` | 마지막 벽 접촉 법선의 X(SetSmoothContacts가 켜져 있을 때 설정). |
| `body:GetWallNz()  -> number` | 마지막 벽 접촉 법선의 Z(SetSmoothContacts가 켜져 있을 때 설정). |
| `body:GetImpulseX()  -> number` | 마지막 스텝 동안 바디 대 바디 접촉이 준 순 속도 변화의 X. |
| `body:GetImpulseZ()  -> number` | 마지막 스텝 동안 바디 대 바디 접촉이 준 순 속도 변화의 Z. |

### 차량 바디 핸들

<span class="badge-exp">실험적</span>

<div class="callout warn">
NewVehicle이 이 핸들을 반환합니다. 차체는 Step이 평소처럼 움직이는 캐릭터 바디이고, UpdateWheels 호출마다 바퀴를 차체에서 아래로 쏘는 광선으로 계산해, 다중 바퀴 접지 플래그, 바퀴별 서스펜션 압축, 경사를 따르는 차체 피치와 롤을 제공합니다. 주행 코드가 차체 속도를 설정합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `vehicle:AddWheel(lx, lz, radius, rest, steered, powered)  -> wheel` | 차체 공간 오프셋(`lx` 오른쪽, `lz` 앞)에 지정한 반지름과 서스펜션 정지 길이의 바퀴를 추가합니다. 차량은 `steered`와 `powered`를 주행 코드를 위해 저장하며 스스로는 읽지 않습니다. |
| `vehicle:GetWheel(i)  -> wheel` | 바퀴 `i`(0부터 시작). 없으면 nil. |
| `vehicle:WheelCount()  -> number` | 바퀴 수. |
| `vehicle:UpdateWheels(dt, speed)  -> nil` | 바퀴를 레이캐스트하고 접지, 압축, 피치, 롤, 바퀴 회전을 갱신합니다(`speed`는 초당 월드 단위). Step 뒤에 프레임마다 한 번 호출하십시오. |
| `vehicle.Body  -> body` | 차체 바디 핸들. |
| `vehicle:SetPos(x, y, z)  -> nil` | 차체 위치를 설정합니다. |
| `vehicle:SetVelocity(x, y, z)  -> nil` | 차체 속도를 설정합니다. |
| `vehicle:AddVelocity(x, y, z)  -> nil` | 차체 속도에 더합니다. |
| `vehicle:SetForward(fx, fz)  -> nil` | 전방 축을 설정하고 헤딩을 도출합니다. |
| `vehicle:SetYaw(y)  -> nil` | 헤딩을 라디안으로 설정하고 전방 축을 도출합니다. |
| `vehicle:SetGravityEnabled(g)  -> nil` | 차체에 중력을 적용합니다. |
| `vehicle:SetEnabled(e)  -> nil` | 차체 바디를 켜거나 끕니다. |
| `vehicle:SetCapsule(halfLen)  -> nil` | 차체 대 바디 접촉의 캡슐 반길이. |
| `vehicle:SetRadius(r)  -> nil` | 차체 반지름. |
| `vehicle:GetX()  -> number` | 차체 X. |
| `vehicle:GetY()  -> number` | 차체 Y. |
| `vehicle:GetZ()  -> number` | 차체 Z. |
| `vehicle:GetVx()  -> number` | 차체 속도 X. |
| `vehicle:GetVy()  -> number` | 차체 속도 Y. |
| `vehicle:GetVz()  -> number` | 차체 속도 Z. |
| `vehicle:IsOnFloor()  -> boolean` | 바퀴가 셋 이상 접지되었거나 차체가 바닥에 있으면 true. |
| `vehicle:HitWall()  -> boolean` | 마지막 스텝 동안 차체가 벽에 닿았으면 true. |
| `vehicle:GetImpulseX()  -> number` | 차체에 가해진 바디 대 바디 속도 변화의 X. |
| `vehicle:GetImpulseZ()  -> number` | 차체에 가해진 바디 대 바디 속도 변화의 Z. |
| `vehicle:GetPitch()  -> number` | 바퀴 접촉을 향해 완화된 차체 피치(라디안). |
| `vehicle:GetRoll()  -> number` | 바퀴 접촉을 향해 완화된 차체 롤(라디안). |

### 바퀴 핸들

<span class="badge-exp">실험적</span>

AddWheel과 GetWheel이 이 핸들을 반환합니다. UpdateWheels 뒤에 읽으십시오.

| 메서드 | 설명 |
| --- | --- |
| `wheel:IsGrounded()  -> boolean` | 바퀴가 지면에 닿아 있으면 true. |
| `wheel:GetCompression()  -> number` | 서스펜션 압축, 0(완전히 늘어남 또는 공중)에서 1(완전히 압축됨). |
| `wheel:GetSpin()  -> number` | 바퀴 비주얼을 위한 누적 회전 각도(라디안). |

### MeshCollider 핸들

<span class="badge-exp">실험적</span>

월드의 정적 집합을 위한 삼각형 목록입니다. 직접 또는 model:BuildCollider로 채우십시오.

| 메서드 | 설명 |
| --- | --- |
| `mc:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | 삼각형 하나를 추가합니다. |
| `mc:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | 쿼드를 두 삼각형으로 추가합니다. |
| `mc:TriCount()  -> number` | 모인 삼각형 수. |
| `mc:Clear()  -> nil` | 모든 삼각형을 제거합니다. |
| `mc:AddToWorld(world)  -> nil` | 삼각형을 월드의 정적 집합에 추가합니다(BeginStatic과 EndStatic 사이). world:AddMesh(mc)와 같습니다. |

## 콜라이더와 레이캐스팅

### COLLIDERS

<span class="badge-exp">실험적</span>

물리 월드와 무관하게 히트 테스트에 사용하는 질의 형태(구, 축 정렬 상자, 캡슐)와 광선의 팩토리입니다.

| 메서드 | 설명 |
| --- | --- |
| `COLLIDERS:Sphere(cx, cy, cz, r)  -> collider` | (cx, cy, cz)를 중심으로 하는 반지름 `r`의 구. |
| `COLLIDERS:Box(cx, cy, cz, hx, hy, hz)  -> collider` | (cx, cy, cz)를 중심으로 하는 반폭 (hx, hy, hz)의 축 정렬 상자. |
| `COLLIDERS:Capsule(cx, cy, cz, hsx, hsy, hsz, r)  -> collider` | 중심 - (hsx, hsy, hsz)에서 중심 + (hsx, hsy, hsz)까지의 선분 주위 반지름 `r`의 캡슐. |
| `COLLIDERS:CapsuleY(cx, cy, cz, halfHeight, r)  -> collider` | 지정한 반높이와 반지름의 수직 캡슐. |
| `COLLIDERS:Ray(ox, oy, oz, dx, dy, dz, len)  -> ray` | 원점, 방향(자동 정규화), 길이를 가진 재사용 가능한 광선. |

### 콜라이더 핸들

<span class="badge-exp">실험적</span>

| 메서드 | 설명 |
| --- | --- |
| `collider:SetCenter(x, y, z)  -> nil` | 형태를 새 중심으로 옮깁니다. |
| `collider:SetRadius(r)  -> nil` | 반지름을 설정합니다(구 콜라이더 전용). |
| `collider:SetHalfExtents(hx, hy, hz)  -> nil` | 반폭을 설정합니다(상자 콜라이더 전용). |
| `collider:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> number` | `maxDist` 안의 첫 충돌까지 광선(방향 자동 정규화)을 따른 거리. 빗나가면 -1. |
| `collider:HitsRay(ray)  -> boolean` | 광선이 그 길이 안에서 형태에 맞으면 true. |
| `collider:CollideWith(other)  -> boolean` | 형태가 다른 콜라이더와 겹치거나, `other`가 이를 맞히는 광선이면 true. |
| `collider:SetTag(tag)  -> nil` | 충돌을 자신의 객체로 되돌릴 수 있도록 임의의 Lua 값을 붙입니다. |
| `collider:GetTag()  -> any` | 붙인 값. |

### 광선 핸들

<span class="badge-exp">실험적</span>

| 메서드 | 설명 |
| --- | --- |
| `ray:Set(ox, oy, oz, dx, dy, dz, len)  -> nil` | 원점, 방향(자동 정규화), 길이를 재설정합니다. |

## 경로 탐색

### PATHFIND

<span class="badge-exp">실험적</span>

FindPath가 A*로 검색하는 가중 내비게이션 그래프의 팩토리입니다.

| 메서드 | 설명 |
| --- | --- |
| `PATHFIND:NewGraph()  -> graph` | 빈 그래프를 만들고 핸들을 반환합니다. |

### 내비게이션 그래프 핸들

<span class="badge-exp">실험적</span>

<div class="callout warn">
노드는 월드 위치를 가지며 인덱스는 1부터 시작합니다. 각 간선은 가중치와 경유점을 가집니다. 경유점은 그 간선을 탈 때 이동체가 지나는 위치입니다(예: 출입구 중간점). FindPath는 지나온 간선의 경유점을 반환하고 마지막에 목표점을 붙입니다.
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

| 메서드 | 설명 |
| --- | --- |
| `graph:AddNode(x, y, z)  -> number` | 노드를 추가하고 인덱스를 반환합니다. |
| `graph:AddEdge(a, b, w, px, py, pz)  -> nil` | 노드 `a`에서 노드 `b`로 가중치 `w`의 유향 간선을 경유점 (px, py, pz)를 지나도록 추가합니다. |
| `graph:Link(a, b, w, px, py, pz)  -> nil` | 공유 경유점 하나를 지나는 양방향 간선을 추가합니다. |
| `graph:NearestNode(x, y, z)  -> number` | (x, y, z)에 가장 가까운 노드의 인덱스. 그래프가 비어 있으면 0. |
| `graph:FindPath(start, goal, goalX, goalY, goalZ)  -> path` | 노드 `start`에서 노드 `goal`까지 A*를 실행합니다. (goalX, goalY, goalZ)에서 끝나는 경로를 반환하며, 도달할 수 없으면 nil. |
| `graph:NodeCount()  -> number` | 노드 수. |
| `graph:Clear()  -> nil` | 모든 노드와 간선을 제거합니다. |

### 내비게이션 경로 핸들

<span class="badge-exp">실험적</span>

웨이포인트 인덱스는 1부터 시작합니다.

| 메서드 | 설명 |
| --- | --- |
| `path:Count()  -> number` | 웨이포인트 수. |
| `path:X(i)  -> number` | 웨이포인트 `i`의 X. |
| `path:Y(i)  -> number` | 웨이포인트 `i`의 Y. |
| `path:Z(i)  -> number` | 웨이포인트 `i`의 Z. |
