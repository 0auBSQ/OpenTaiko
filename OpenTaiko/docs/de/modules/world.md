<!-- modules/world.md -->

# World <span class="badge-exp">Experimentell</span>

## World.OWM3d

Die 3D-Welt-Engine hinter Mein Zimmer und den Story-Szenen des mitgelieferten Skins, aufgebaut auf der [Rasterizer-Welt](../api/3d.md) und der [Physik](../api/3d-physics.md) der Engine. Sie macht aus Kartendaten einen begehbaren Ort: Raster- oder Heightmap-Terrain mit passender Kollision, glTF-Modelle, Figuren, die kollidieren und gleiten, eine Kamera, die nicht durch Wände schneidet, Trigger, einen Tag-Nacht-Zyklus, Wetter und Wasser.

<div class="callout">
OWM3d ist experimentell, wie die 3D-Engine, auf der es aufsetzt: Seine Funktionen, sein Kartenformat und seine Standardwerte können sich zwischen Versionen ohne Übergangsfrist ändern. Testen Sie einen Skin, der es verwendet, nach jedem Update erneut.
</div>

`require("World.OWM3d")` gibt eine Fassade mit den Teilen zurück, die eine Stage selbst erzeugt:

| Feld | Was es ist |
| --- | --- |
| `World` | Die Welt (`World.OWM3d.core`): `World.new{ rw, rh, screenW, screenH, gridW, gridH, lit, hour, yaw, pitch, fov, dist }` erzeugt eine Szene und jedes Subsystem. |
| `Camera` | Die Kamera-Rigs (`iso`, `interior`, `orbit` oder eine Tabelle aus `yaw`, `pitch`, `fov`, `dist`), Folgen, Umkreisen und Zoomen sowie der Kollisionsausleger. |
| `Json` | Der Leser für Kartendateien: `Json.loadMap(dir)` liest `<dir>/map.json` in einfache Lua-Tabellen, mit angewendeten Standardwerten. |
| `IsoMap` | Rasterkarten: die Böden, Rampen, Wände und Gebäude eines Zellrasters, mit denselben Dreiecken als Kollision. |
| `Terrain` | Heightmap-Karten: ein Mesh aus einem Höhenfeld, Splat-Texturierung und Gras. |
| `NPC` | Figuren, die um einen Heimatpunkt herumwandern, stehen bleiben oder patrouillieren und sich beim Sprechen dem Spieler zuwenden. |
| `ModelIcon` | Ein Vorschaubild eines 3D-Modells, einmal in eine kleine Szene gerendert und dann wie eine Textur gezeichnet (Katalog- und Ladensymbole). |
| `version` | Die Versionsnummer der Fassade (1). |

Eine mit `World.new` erzeugte Welt trägt den Rest als Felder: `world.phys` (benannte Collider-Gruppen, Character-Controller, Raycasts), `world.models` (Modellinstanzen), `world.triggers` (Volumen, die Gruppen umschalten, das Kamera-Rig wechseln, teleportieren, die Karte wechseln oder eine Funktion ausführen), `world.particles`, `world.weather` (`clear`, `rain`, `storm`, `snow`, `sakura`, `embers`), `world.water`, `world.daynight` und `world.cam`. Jeder Teil ist auch ein eigenes Modul, `World.OWM3d.<part>`.

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

Kartenordner, Modelle und Texturen sind Dateien der Stage: Relative Pfade werden gegen den Ordner der aufrufenden Stage aufgelöst. Rufen Sie `world:dispose()` auf, wenn die Stage mit der Welt fertig ist.
