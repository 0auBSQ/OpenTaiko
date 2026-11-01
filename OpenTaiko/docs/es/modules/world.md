<!-- modules/world.md -->

# World <span class="badge-exp">Experimental</span>

## World.OWM3d

El motor de mundos 3D que hay detrás de Mi Habitación y de las escenas de historia del skin incluido, construido sobre el [mundo del rasterizador](../api/3d.md) y las [físicas](../api/3d-physics.md) del motor. Convierte datos de mapa en un lugar jugable: terreno en cuadrícula o con mapa de alturas con su colisión correspondiente, modelos glTF, personajes que colisionan y se deslizan, una cámara que no atraviesa paredes, triggers, un ciclo de día y noche, clima y agua.

<div class="callout">
OWM3d es experimental, como el motor 3D sobre el que se apoya: sus funciones, su formato de mapa y sus valores por defecto pueden cambiar entre versiones sin periodo de obsolescencia. Vuelve a probar un skin que lo use después de cada actualización.
</div>

`require("World.OWM3d")` devuelve una fachada con las partes que un stage crea por sí mismo:

| Campo | Qué es |
| --- | --- |
| `World` | El mundo (`World.OWM3d.core`): `World.new{ rw, rh, screenW, screenH, gridW, gridH, lit, hour, yaw, pitch, fov, dist }` crea una escena y todos los subsistemas. |
| `Camera` | Los rigs de cámara (`iso`, `interior`, `orbit`, o una tabla de `yaw`, `pitch`, `fov`, `dist`), el seguimiento, la órbita y el zoom, y el brazo con colisión. |
| `Json` | El lector de archivos de mapa: `Json.loadMap(dir)` lee `<dir>/map.json` en tablas Lua simples con los valores por defecto aplicados. |
| `IsoMap` | Mapas en cuadrícula: los suelos, rampas, paredes y edificios de una cuadrícula de celdas, con los mismos triángulos como colisión. |
| `Terrain` | Mapas de alturas: una malla a partir de un campo de alturas, texturizado splat y hierba. |
| `NPC` | Personajes que deambulan, esperan o patrullan alrededor de un punto de origen, y miran al jugador mientras hablan. |
| `ModelIcon` | Una miniatura de un modelo 3D, renderizada una vez en una escena pequeña y dibujada después como una textura (iconos de catálogo y de tienda). |
| `version` | El número de versión de la fachada (1). |

Un mundo creado con `World.new` lleva el resto como campos: `world.phys` (grupos de colisionadores con nombre, controladores de personaje, raycasts), `world.models` (instancias de modelos), `world.triggers` (volúmenes que activan o desactivan grupos, cambian el rig de cámara, teletransportan, cambian de mapa o ejecutan una función), `world.particles`, `world.weather` (`clear`, `rain`, `storm`, `snow`, `sakura`, `embers`), `world.water`, `world.daynight` y `world.cam`. Cada parte es también un módulo propio, `World.OWM3d.<part>`.

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

Las carpetas de mapas, los modelos y las texturas son archivos del stage: las rutas relativas se resuelven respecto a la carpeta del stage que llama. Llama a `world:dispose()` cuando el stage termine con el mundo.
