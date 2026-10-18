<!-- modules/world.md -->

# World <span class="badge-exp">实验性</span>

## World.OWM3d

驱动我的房间和自带皮肤剧情场景的 3D 世界引擎，构建在引擎的[光栅化世界](../api/3d.md)和[物理](../api/3d-physics.md)之上。它把地图数据变成一个可游玩的地方：带有匹配碰撞的网格或高度图地形、glTF 模型、会碰撞和滑动的角色、不会穿墙的摄像机、触发区、昼夜循环、天气和水。

<div class="callout">
OWM3d 和它所依托的 3D 引擎一样处于实验阶段：它的函数、地图格式和默认值可能在版本之间变化，而不设弃用期。每次更新后，请重新测试使用它的皮肤。
</div>

`require("World.OWM3d")` 返回一个门面，其中包含舞台需要自己创建的部分：

| 字段 | 是什么 |
| --- | --- |
| `World` | 世界（`World.OWM3d.core`）：`World.new{ rw, rh, screenW, screenH, gridW, gridH, lit, hour, yaw, pitch, fov, dist }` 创建一个场景及所有子系统。 |
| `Camera` | 摄像机装置（`iso`、`interior`、`orbit`，或一张包含 `yaw`、`pitch`、`fov`、`dist` 的表），跟随、环绕和缩放，以及碰撞吊臂。 |
| `Json` | 地图文件读取器：`Json.loadMap(dir)` 把 `<dir>/map.json` 读入应用了默认值的普通 Lua 表。 |
| `IsoMap` | 网格地图：由单元格网格构成的地板、坡道、墙壁和建筑，并以相同的三角形作为碰撞。 |
| `Terrain` | 高度图地图：由高度场生成的网格、splat 纹理和草地。 |
| `NPC` | 在家位置附近漫游、待机或巡逻，并在对话时面向玩家的角色。 |
| `ModelIcon` | 3D 模型的缩略图，先渲染到一个小场景中一次，之后像纹理一样绘制（目录和商店图标）。 |
| `version` | 门面的版本号（1）。 |

用 `World.new` 创建的世界把其余部分作为字段持有：`world.phys`（命名的碰撞体组、角色控制器、射线检测）、`world.models`（模型实例）、`world.triggers`（可切换组、更改摄像机装置、传送、切换地图或运行函数的区域）、`world.particles`、`world.weather`（`clear`、`rain`、`storm`、`snow`、`sakura`、`embers`）、`world.water`、`world.daynight` 和 `world.cam`。每个部分本身也是一个模块，即 `World.OWM3d.<part>`。

```lua
local OWM = require("World.OWM3d")
local world, last = nil, 0

function onStart()
    world = OWM.World.new{ rw = 1280, rh = 720, screenW = 1920, screenH = 1080, lit = true }
    world:registerMap("plaza", { type = "folder", dir = "maps/plaza" })
    world:loadMap("plaza", "default")
end

function update(ts)
    local dt = (ts - last) / 1000; last = ts
    world:update(dt, px, py, pz)   -- the player's position drives the triggers and the camera
end

function draw()
    world:render()
    world:blit()
end
```

地图文件夹、模型和纹理都是舞台的文件：相对路径相对于调用方舞台的文件夹解析。舞台用完世界后，请调用 `world:dispose()`。
