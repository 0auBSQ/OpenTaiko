<!-- modules/world.md -->

# World <span class="badge-exp">Expérimental</span>

## World.OWM3d

Le moteur de monde 3D derrière Ma Chambre et les scènes d'histoire du skin fourni, construit sur le [monde du rastériseur](../api/3d.md) et la [physique](../api/3d-physics.md) du moteur. Il transforme des données de carte en un lieu jouable : un terrain en grille ou en heightmap avec la collision correspondante, des modèles glTF, des personnages qui entrent en collision et glissent, une caméra qui ne traverse pas les murs, des déclencheurs, un cycle jour-nuit, la météo et l'eau.

<div class="callout">
OWM3d est expérimental, comme le moteur 3D sur lequel il repose : ses fonctions, son format de carte et ses valeurs par défaut peuvent changer d'une version à l'autre sans période de dépréciation. Testez de nouveau un skin qui l'utilise après chaque mise à jour.
</div>

`require("World.OWM3d")` renvoie une façade avec les parties qu'un stage crée lui-même :

| Champ | Ce que c'est |
| --- | --- |
| `World` | Le monde (`World.OWM3d.core`) : `World.new{ rw, rh, screenW, screenH, gridW, gridH, lit, hour, yaw, pitch, fov, dist }` crée une scène et chaque sous-système. |
| `Camera` | Les configurations de caméra (`iso`, `interior`, `orbit`, ou une table de `yaw`, `pitch`, `fov`, `dist`), le suivi, l'orbite et le zoom, et la perche anticollision. |
| `Json` | Le lecteur de fichiers de carte : `Json.loadMap(dir)` lit `<dir>/map.json` dans de simples tables Lua, valeurs par défaut appliquées. |
| `IsoMap` | Les cartes en grille : les sols, rampes, murs et bâtiments d'une grille de cellules, avec les mêmes triangles comme collision. |
| `Terrain` | Les cartes en heightmap : un maillage tiré d'un champ de hauteurs, du texturage par splat et de l'herbe. |
| `NPC` | Des personnages qui flânent, attendent ou patrouillent autour d'un point d'attache, et se tournent vers le joueur quand ils parlent. |
| `ModelIcon` | Une vignette d'un modèle 3D, rendue une fois dans une petite scène puis dessinée comme une texture (icônes de catalogue et de boutique). |
| `version` | Le numéro de version de la façade (1). |

Un monde créé avec `World.new` porte le reste sous forme de champs : `world.phys` (groupes de colliders nommés, contrôleurs de personnage, raycasts), `world.models` (instances de modèles), `world.triggers` (des volumes qui basculent des groupes, changent la configuration de caméra, téléportent, changent de carte ou exécutent une fonction), `world.particles`, `world.weather` (`clear`, `rain`, `storm`, `snow`, `sakura`, `embers`), `world.water`, `world.daynight` et `world.cam`. Chaque partie est aussi un module à part entière, `World.OWM3d.<part>`.

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

Les dossiers de cartes, les modèles et les textures sont des fichiers du stage : les chemins relatifs se résolvent par rapport au dossier du stage appelant. Appelez `world:dispose()` quand le stage n'a plus besoin du monde.
