<!-- modules/world.md -->

# World <span class="badge-exp">Experimenteel</span>

## World.OWM3d

De 3D-wereldengine achter Mijn kamer en de verhaalscènes van de meegeleverde skin, gebouwd op de [rasterizerwereld](../api/3d.md) en de [fysica](../api/3d-physics.md) van de engine. Hij maakt van kaartdata een speelbare plek: terrein uit een raster of een heightmap met bijpassende collision, glTF-modellen, personages die botsen en glijden, een camera die niet door muren gaat, triggers, een dag-en-nachtcyclus, weer en water.

<div class="callout">
OWM3d is experimenteel, net als de 3D-engine waarop het rust: zijn functies, zijn kaartformaat en zijn standaardwaarden kunnen tussen releases veranderen zonder overgangsperiode. Test een skin die het gebruikt na elke update opnieuw.
</div>

`require("World.OWM3d")` geeft een façade terug met de onderdelen die een stage zelf aanmaakt:

| Veld | Wat het is |
| --- | --- |
| `World` | De wereld (`World.OWM3d.core`): `World.new{ rw, rh, screenW, screenH, gridW, gridH, lit, hour, yaw, pitch, fov, dist }` maakt een scène en elk subsysteem aan. |
| `Camera` | De camera-opstellingen (`iso`, `interior`, `orbit`, of een tabel met `yaw`, `pitch`, `fov`, `dist`), volgen, cirkelen en zoomen, en de botsingsarm. |
| `Json` | De lezer van kaartbestanden: `Json.loadMap(dir)` leest `<dir>/map.json` in gewone Lua-tabellen, met de standaardwaarden toegepast. |
| `IsoMap` | Rasterkaarten: de vloeren, hellingen, muren en gebouwen van een raster van cellen, met dezelfde driehoeken als collision. |
| `Terrain` | Heightmapkaarten: een mesh uit een hoogteveld, splat-texturering en gras. |
| `NPC` | Personages die rond een thuispunt rondzwerven, stilstaan of patrouilleren, en zich tijdens een gesprek naar de speler draaien. |
| `ModelIcon` | Een miniatuur van een 3D-model, één keer in een kleine scène gerenderd en daarna als een textuur getekend (catalogus- en winkelpictogrammen). |
| `version` | Het versienummer van de façade (1). |

Een wereld die met `World.new` is gemaakt, draagt de rest als velden: `world.phys` (benoemde collidergroepen, character controllers, raycasts), `world.models` (modelinstanties), `world.triggers` (volumes die groepen schakelen, de camera-opstelling wisselen, teleporteren, van kaart wisselen of een functie uitvoeren), `world.particles`, `world.weather` (`clear`, `rain`, `storm`, `snow`, `sakura`, `embers`), `world.water`, `world.daynight` en `world.cam`. Elk onderdeel is ook een eigen module, `World.OWM3d.<part>`.

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

Kaartmappen, modellen en texturen zijn bestanden van de stage: relatieve paden worden opgelost ten opzichte van de map van de aanroepende stage. Roep `world:dispose()` aan wanneer de stage klaar is met de wereld.
