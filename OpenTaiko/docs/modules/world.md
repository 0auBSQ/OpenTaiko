<!-- modules/world.md -->

# World <span class="badge-exp">Experimental</span>

## World.OWM3d

The 3D world engine behind My Room and the shipped skin's story scenes, built on the engine's [rasterizer world](../api/3d.md) and [physics](../api/3d-physics.md). It turns map data into a playable place: grid or heightmap terrain with matching collision, glTF models, characters that collide and slide, a camera that does not clip through walls, triggers, a day and night cycle, weather and water.

<div class="callout">
OWM3d is experimental, like the 3D engine it sits on: its functions, its map format and its defaults can change between releases without a deprecation period. Test a skin that uses it again after each update.
</div>

`require("World.OWM3d")` returns a facade with the parts a stage creates itself:

| Field | What it is |
| --- | --- |
| `World` | The world (`World.OWM3d.core`): `World.new{ rw, rh, screenW, screenH, gridW, gridH, lit, hour, yaw, pitch, fov, dist }` makes a scene and every subsystem. |
| `Camera` | The camera rigs (`iso`, `interior`, `orbit`, or a table of `yaw`, `pitch`, `fov`, `dist`), follow, orbit and zoom, and the collision boom. |
| `Json` | The map file reader: `Json.loadMap(dir)` reads `<dir>/map.json` into plain Lua tables with the defaults applied. |
| `IsoMap` | Grid maps: the floors, ramps, walls and buildings of a cell grid, with the same triangles as collision. |
| `Terrain` | Heightmap maps: a mesh from a height field, splat texturing, and grass. |
| `NPC` | Characters that wander, idle or patrol around a home point, and face the player while talking. |
| `ModelIcon` | A thumbnail of a 3D model, rendered once into a small scene and then drawn like a texture (catalogue and shop icons). |
| `version` | The facade's version number (1). |

A world made with `World.new` carries the rest as fields: `world.phys` (named collider groups, character controllers, raycasts), `world.models` (model instances), `world.triggers` (volumes that toggle groups, change the camera rig, teleport, change map or run a function), `world.particles`, `world.weather` (`clear`, `rain`, `storm`, `snow`, `sakura`, `embers`), `world.water`, `world.daynight` and `world.cam`. Each part is also a module of its own, `World.OWM3d.<part>`.

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

Map folders, models and textures are the stage's files: relative paths resolve against the calling stage's folder. Call `world:dispose()` when the stage is done with the world.
