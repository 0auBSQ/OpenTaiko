<!-- api/3d-physics.md -->

# 3D エンジン: 物理 <span class="badge-exp">実験的</span>

剛体、キャラクター、乗り物、静的な衝突、レイキャストによるコライダー問い合わせ、ナビゲーショングラフ。位置はワールド単位で +Y が上です。共通の規約は [3D エンジン: ラスタライザーの世界](3d.md) で説明しています。

## 物理

### PHYSICS

<span class="badge-exp">実験的</span>

物理ワールドのファクトリです。静的な三角形群と、動的なキャラクター、剛体、ボックス、車両のボディからなります。

| メソッド | 説明 |
| --- | --- |
| `PHYSICS:NewWorld()  -> world` | 物理ワールドを作成し、そのハンドルを返します。 |

### 物理ワールドハンドル

<span class="badge-exp">実験的</span>

<div class="callout warn">
BeginStatic と EndStatic の間で静的ジオメトリを構築し、ボディを作成し、毎フレームその速度を設定して Step を呼びます。ボディは静的な三角形群と collide-and-slide (球または向き付きボックス対三角形) で衝突し、ボディ同士は水平面内で球またはカプセルとして衝突し、質量に応じて運動量をやり取りします。
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

| メソッド | 説明 |
| --- | --- |
| `world:SetGravity(x, y, z)  -> nil` | 重力ベクトル (既定 0, -32, 0)。 |
| `world:SetFloorMaxAngleY(y)  -> nil` | 接触法線の Y のしきい値 (既定 0.5)。これを超える接触は床、それ以外は壁とみなされます。 |
| `world:BeginStatic()  -> nil` | 静的な三角形群の再構築を開始します。既存の三角形とコライダーグループをクリアします。 |
| `world:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | 静的な三角形を追加します。法線は (b - a) x (c - a) です。退化した三角形はワールドがスキップします。 |
| `world:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | 静的なクワッドを 2 つの三角形 (a, b, c) と (a, c, d) として追加します。 |
| `world:AddMesh(mc)  -> nil` | MeshCollider の三角形を静的な三角形群に追加します。 |
| `world:BeginGroup(id)  -> nil` | これ以降に追加される三角形にコライダーグループ (0 = 既定) のタグを付け、SetGroupEnabled で実行時にグループを切り替えられるようにします。 |
| `world:EndStatic()  -> nil` | 三角形群を確定し、ブロードフェーズのグリッドを構築します。 |
| `world:SetGroupEnabled(id, on)  -> nil` | 三角形群を再構築せずにコライダーグループを固体または通過可能にします (ドア、ストリーミングされる内装)。 |
| `world:ClearStatic()  -> nil` | 静的な三角形群とその記憶領域を今すぐ解放します (マップのアンロード)。コライダーグループもリセットされます。 |
| `world:NewCharacter(radius)  -> body` | キネマティックな球のボディ: 速度を設定すると、ワールドが collide-and-slide で動かします。 |
| `world:NewRigid(radius, mass)  -> body` | 重力が有効で、他のボディと運動量をやり取りする球のボディ。 |
| `world:NewBox(hx, hy, hz, mass)  -> body` | 指定した半幅を持つ向き付きボックスとして静的な三角形群と衝突する、重力が有効なボディ (SetForward で向きを設定します)。 |
| `world:NewVehicle(radius, mass)  -> vehicle` | 車のようなボディ: 重力、地面へのスナップ、より急な床のしきい値を持つキャラクターのシャーシと、レイキャストのホイール。 |
| `world:NewMeshCollider()  -> mc` | 空の MeshCollider を作成します。 |
| `world:RemoveBody(body)  -> nil` | ワールドからボディを取り除きます。 |
| `world:Step(dt)  -> nil` | シミュレーションを `dt` 秒進めます。ワールドは速いボディが薄い壁をすり抜けないようサブステップします。 |
| `world:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> hit, dist, nx, ny, nz` | 有効な静的三角形に対してレイ (単位方向) を飛ばします。レイが何かに当たったか、最も近いヒットまでの距離、その三角形の法線を返します。 |
| `world:GroundYAt(x, startY, z, reach)  -> number` | (x, startY, z) の真下、`reach` 以内の地面の高さ。なければ NaN (`y ~= y` で判定します)。 |
| `world:StaticTriCount()  -> number` | 静的な三角形群の三角形数。 |
| `world:StaticTriCapacity()  -> number` | 三角形リストの確保済み容量。 |

### 物理ボディハンドル

<span class="badge-exp">実験的</span>

<div class="callout warn">
NewCharacter、NewRigid、NewBox がこのハンドルを返します。位置はボディの中心を指します。ワールドはボディ同士の接触を水平面内でのみ解決し、Y が 2 単位より離れたボディ間ではスキップします。
</div>

| メソッド | 説明 |
| --- | --- |
| `body:SetPos(x, y, z)  -> nil` | 中心位置を設定します。 |
| `body:SetVelocity(x, y, z)  -> nil` | 速度を設定します。 |
| `body:AddVelocity(x, y, z)  -> nil` | 速度に加算します。 |
| `body:SetRadius(r)  -> nil` | 球とブロードフェーズの半径 (最小 0.01)。 |
| `body:SetCapsule(halfLen)  -> nil` | ボディ同士の接触に、前方軸に沿ったこの半長の水平カプセルを使います。静的な衝突は引き続き球の半径を使います。 |
| `body:SetBox(hx, hy, hz)  -> nil` | 半幅 (右、上、前) を持つ向き付きボックスとして静的な三角形群と衝突します。 |
| `body:SetForward(fx, fz)  -> nil` | カプセルまたはボックスの向きを決める水平の前方軸 (自動的に正規化)。 |
| `body:SetGravityEnabled(g)  -> nil` | このボディにワールドの重力を適用します。 |
| `body:SetEnabled(e)  -> nil` | Step は無効なボディをスキップします。 |
| `body:SetSnap(s)  -> nil` | 接地したボディを小さな隙間の範囲で下り坂の地面に貼り付けたままにします。スナップなしでは、下り坂を走るボディは地面から離れて跳ねます。 |
| `body:SetSmoothContacts(on)  -> nil` | キャラクターソルバーの変種: 反復ごとに最も深い接触を解決し、床の継ぎ目を溶接し、壁の法線を記録し、歩行可能な地面へ段差を降ります。既定はオフ。 |
| `body:SetCollisionLayer(l)  -> nil` | ボディ同士の接触に使う衝突レイヤーのインデックス 0..30 (既定 0)。 |
| `body:SetCollisionMask(m)  -> nil` | このボディが衝突するレイヤーのビットマスク (ビット i = レイヤー i)。既定はすべて、0 = なし。 |
| `body:GetPos()  -> x, y, z` | 中心位置。 |
| `body:GetVelocity()  -> x, y, z` | 速度。 |
| `body:GetX()  -> number` | 中心の X。 |
| `body:GetY()  -> number` | 中心の Y。 |
| `body:GetZ()  -> number` | 中心の Z。 |
| `body:GetVx()  -> number` | 速度の X。 |
| `body:GetVy()  -> number` | 速度の Y。 |
| `body:GetVz()  -> number` | 速度の Z。 |
| `body:Speed()  -> number` | 速度の大きさ。 |
| `body:IsOnFloor()  -> boolean` | 最後のステップでボディが床に乗っていたとき true。 |
| `body:HitWall()  -> boolean` | 最後のステップでボディが壁に触れたとき true。 |
| `body:GetWallNx()  -> number` | 最後の壁の接触法線の X (SetSmoothContacts が有効なとき設定されます)。 |
| `body:GetWallNz()  -> number` | 最後の壁の接触法線の Z (SetSmoothContacts が有効なとき設定されます)。 |
| `body:GetImpulseX()  -> number` | 最後のステップでボディ同士の接触によって与えられた正味の速度変化の X。 |
| `body:GetImpulseZ()  -> number` | 最後のステップでボディ同士の接触によって与えられた正味の速度変化の Z。 |

### 車両ボディハンドル

<span class="badge-exp">実験的</span>

<div class="callout warn">
NewVehicle がこのハンドルを返します。シャーシは Step が通常どおり動かすキャラクターボディです。UpdateWheels の呼び出しごとにホイールをシャーシから下向きのレイとして飛ばし、複数ホイールの接地フラグ、ホイールごとのサスペンション圧縮、斜面に追従するシャーシのピッチとロールを与えます。運転コードがシャーシの速度を設定します。
</div>

| メソッド | 説明 |
| --- | --- |
| `vehicle:AddWheel(lx, lz, radius, rest, steered, powered)  -> wheel` | シャーシ空間のオフセット (`lx` 右、`lz` 前) に、指定した半径とサスペンションの自然長でホイールを追加します。乗り物は `steered` と `powered` を運転コードのために保存するだけで、自身では決して読みません。 |
| `vehicle:GetWheel(i)  -> wheel` | ホイール `i` (0 始まり)。なければ nil。 |
| `vehicle:WheelCount()  -> number` | ホイールの数。 |
| `vehicle:UpdateWheels(dt, speed)  -> nil` | ホイールのレイキャストを行い、接地、圧縮、ピッチ、ロール、ホイールの回転を更新します (`speed` は毎秒のワールド単位)。Step の後に 1 フレームに 1 回呼びます。 |
| `vehicle.Body  -> body` | シャーシのボディハンドル。 |
| `vehicle:SetPos(x, y, z)  -> nil` | シャーシの位置を設定します。 |
| `vehicle:SetVelocity(x, y, z)  -> nil` | シャーシの速度を設定します。 |
| `vehicle:AddVelocity(x, y, z)  -> nil` | シャーシの速度に加算します。 |
| `vehicle:SetForward(fx, fz)  -> nil` | 前方軸を設定し、向きを導出します。 |
| `vehicle:SetYaw(y)  -> nil` | 向きをラジアンで設定し、前方軸を導出します。 |
| `vehicle:SetGravityEnabled(g)  -> nil` | シャーシに重力を適用します。 |
| `vehicle:SetEnabled(e)  -> nil` | シャーシのボディを有効または無効にします。 |
| `vehicle:SetCapsule(halfLen)  -> nil` | シャーシとボディの接触に使うカプセルの半長。 |
| `vehicle:SetRadius(r)  -> nil` | シャーシの半径。 |
| `vehicle:GetX()  -> number` | シャーシの X。 |
| `vehicle:GetY()  -> number` | シャーシの Y。 |
| `vehicle:GetZ()  -> number` | シャーシの Z。 |
| `vehicle:GetVx()  -> number` | シャーシの速度の X。 |
| `vehicle:GetVy()  -> number` | シャーシの速度の Y。 |
| `vehicle:GetVz()  -> number` | シャーシの速度の Z。 |
| `vehicle:IsOnFloor()  -> boolean` | 3 つ以上のホイールが接地しているか、シャーシが床に乗っているとき true。 |
| `vehicle:HitWall()  -> boolean` | 最後のステップでシャーシが壁に触れたとき true。 |
| `vehicle:GetImpulseX()  -> number` | シャーシに与えられたボディ同士の速度変化の X。 |
| `vehicle:GetImpulseZ()  -> number` | シャーシに与えられたボディ同士の速度変化の Z。 |
| `vehicle:GetPitch()  -> number` | ホイールの接触に向かって緩和されたシャーシのピッチ (ラジアン)。 |
| `vehicle:GetRoll()  -> number` | ホイールの接触に向かって緩和されたシャーシのロール (ラジアン)。 |

### ホイールハンドル

<span class="badge-exp">実験的</span>

AddWheel と GetWheel がこのハンドルを返します。UpdateWheels の後に読み取ってください。

| メソッド | 説明 |
| --- | --- |
| `wheel:IsGrounded()  -> boolean` | ホイールが地面に触れているとき true。 |
| `wheel:GetCompression()  -> number` | サスペンションの圧縮。0 (完全に伸びているか空中) から 1 (完全に圧縮)。 |
| `wheel:GetSpin()  -> number` | ホイールの見た目用の、蓄積された回転角 (ラジアン)。 |

### MeshCollider ハンドル

<span class="badge-exp">実験的</span>

ワールドの静的な三角形群のための三角形リストです。手作業で、または model:BuildCollider で埋めます。

| メソッド | 説明 |
| --- | --- |
| `mc:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | 三角形を 1 つ追加します。 |
| `mc:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | クワッドを 2 つの三角形として追加します。 |
| `mc:TriCount()  -> number` | 集められた三角形の数。 |
| `mc:Clear()  -> nil` | すべての三角形を取り除きます。 |
| `mc:AddToWorld(world)  -> nil` | 三角形をワールドの静的な三角形群に追加します (BeginStatic と EndStatic の間で)。world:AddMesh(mc) と同じです。 |

## コライダーとレイキャスト

### COLLIDERS

<span class="badge-exp">実験的</span>

物理ワールドとは独立した、ヒットテストのための問い合わせ形状 (球、軸に平行なボックス、カプセル) とレイのファクトリです。

| メソッド | 説明 |
| --- | --- |
| `COLLIDERS:Sphere(cx, cy, cz, r)  -> collider` | (cx, cy, cz) を中心とする半径 `r` の球。 |
| `COLLIDERS:Box(cx, cy, cz, hx, hy, hz)  -> collider` | (cx, cy, cz) を中心とし、半幅 (hx, hy, hz) を持つ軸に平行なボックス。 |
| `COLLIDERS:Capsule(cx, cy, cz, hsx, hsy, hsz, r)  -> collider` | 中心 - (hsx, hsy, hsz) から中心 + (hsx, hsy, hsz) までの線分の周りの半径 `r` のカプセル。 |
| `COLLIDERS:CapsuleY(cx, cy, cz, halfHeight, r)  -> collider` | 指定した半高さと半径の垂直カプセル。 |
| `COLLIDERS:Ray(ox, oy, oz, dx, dy, dz, len)  -> ray` | 原点、方向 (自動的に正規化)、長さを持つ再利用可能なレイ。 |

### コライダーハンドル

<span class="badge-exp">実験的</span>

| メソッド | 説明 |
| --- | --- |
| `collider:SetCenter(x, y, z)  -> nil` | 形状を新しい中心へ移動します。 |
| `collider:SetRadius(r)  -> nil` | 半径を設定します (球コライダーのみ)。 |
| `collider:SetHalfExtents(hx, hy, hz)  -> nil` | 半幅を設定します (ボックスコライダーのみ)。 |
| `collider:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> number` | レイ (方向は自動的に正規化) に沿った、`maxDist` 以内の最初のヒットまでの距離。外れたら -1。 |
| `collider:HitsRay(ray)  -> boolean` | レイがその長さの範囲内で形状に当たるとき true。 |
| `collider:CollideWith(other)  -> boolean` | 形状が別のコライダーと重なるとき、または `other` がそれに当たるレイであるとき true。 |
| `collider:SetTag(tag)  -> nil` | ヒットを自分のオブジェクトに対応付けられるよう、任意の Lua 値を取り付けます。 |
| `collider:GetTag()  -> any` | 取り付けられた値。 |

### レイハンドル

<span class="badge-exp">実験的</span>

| メソッド | 説明 |
| --- | --- |
| `ray:Set(ox, oy, oz, dx, dy, dz, len)  -> nil` | 原点、方向 (自動的に正規化)、長さをリセットします。 |

## 経路探索

### PATHFIND

<span class="badge-exp">実験的</span>

FindPath が A* で探索する重み付きナビゲーショングラフのファクトリです。

| メソッド | 説明 |
| --- | --- |
| `PATHFIND:NewGraph()  -> graph` | 空のグラフを作成し、そのハンドルを返します。 |

### ナビゲーショングラフハンドル

<span class="badge-exp">実験的</span>

<div class="callout warn">
ノードはワールド位置を持ち、インデックスは 1 から始まります。各エッジは重みと通過点、つまりそのエッジを通るときに移動体が通過する位置 (例えば出入り口の中点) を持ちます。FindPath は通った各エッジの通過点と、その後に目標点を返します。
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

| メソッド | 説明 |
| --- | --- |
| `graph:AddNode(x, y, z)  -> number` | ノードを追加し、そのインデックスを返します。 |
| `graph:AddEdge(a, b, w, px, py, pz)  -> nil` | ノード `a` からノード `b` へ、重み `w` で通過点 (px, py, pz) を通る有向エッジを追加します。 |
| `graph:Link(a, b, w, px, py, pz)  -> nil` | 1 つの共有通過点を通る両方向のエッジを追加します。 |
| `graph:NearestNode(x, y, z)  -> number` | (x, y, z) に最も近いノードのインデックス。グラフが空なら 0。 |
| `graph:FindPath(start, goal, goalX, goalY, goalZ)  -> path` | ノード `start` からノード `goal` へ A* を実行し、(goalX, goalY, goalZ) で終わる経路を返します。到達不能なら nil。 |
| `graph:NodeCount()  -> number` | ノードの数。 |
| `graph:Clear()  -> nil` | すべてのノードとエッジを取り除きます。 |

### ナビゲーション経路ハンドル

<span class="badge-exp">実験的</span>

ウェイポイントのインデックスは 1 から始まります。

| メソッド | 説明 |
| --- | --- |
| `path:Count()  -> number` | ウェイポイントの数。 |
| `path:X(i)  -> number` | ウェイポイント `i` の X。 |
| `path:Y(i)  -> number` | ウェイポイント `i` の Y。 |
| `path:Z(i)  -> number` | ウェイポイント `i` の Z。 |
