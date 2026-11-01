<!-- api/3d-physics.md -->

# 3D-Engine: Physik <span class="badge-exp">Experimentell</span>

Starrkörper, Charaktere und Fahrzeuge, statische Kollision, Collider-Abfragen mit Raycasts und Navigationsgraphen. Positionen sind Welteinheiten, +Y zeigt nach oben; [3D-Engine: Rasterizer-Welt](3d.md) beschreibt die gemeinsamen Konventionen.

## Physik

### PHYSICS

<span class="badge-exp">Experimentell</span>

Fabrik für Physikwelten: eine statische Dreiecksmenge plus dynamische Charakter-, Starr-, Quader- und Fahrzeugkörper.

| Methode | Beschreibung |
| --- | --- |
| `PHYSICS:NewWorld()  -> world` | Erzeugt eine Physikwelt und gibt ihr Handle zurück. |

### Physikwelt-Handle

<span class="badge-exp">Experimentell</span>

<div class="callout warn">
Bauen Sie die statische Geometrie zwischen BeginStatic und EndStatic auf, erzeugen Sie Körper, setzen Sie jeden Frame ihre Geschwindigkeit und rufen Sie Step auf. Körper kollidieren mit der statischen Dreiecksmenge per Collide-and-Slide (Kugel oder orientierter Quader gegen Dreiecke) und untereinander als Kugeln oder Kapseln in der horizontalen Ebene, wobei Impuls nach Masse ausgetauscht wird.
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

-- pro Frame
body:SetVelocity(moveX, body:GetVy(), moveZ)
world:Step(dt)
local x, y, z = body:GetX(), body:GetY(), body:GetZ()
```

| Methode | Beschreibung |
| --- | --- |
| `world:SetGravity(x, y, z)  -> nil` | Schwerkraftvektor (Standard 0, -32, 0). |
| `world:SetFloorMaxAngleY(y)  -> nil` | Schwellwert für den Y-Wert der Kontaktnormalen (Standard 0.5): Kontakte darüber zählen als Boden, der Rest als Wand. |
| `world:BeginStatic()  -> nil` | Beginnt den Neuaufbau der statischen Dreiecksmenge; löscht vorhandene Dreiecke und Collider-Gruppen. |
| `world:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | Fügt ein statisches Dreieck hinzu. Seine Normale ist (b - a) x (c - a); degenerierte Dreiecke überspringt die Welt. |
| `world:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | Fügt ein statisches Quad als zwei Dreiecke (a, b, c) und (a, c, d) hinzu. |
| `world:AddMesh(mc)  -> nil` | Fügt die Dreiecke eines MeshColliders zur Dreiecksmenge hinzu. |
| `world:BeginGroup(id)  -> nil` | Markiert ab jetzt hinzugefügte Dreiecke mit einer Collider-Gruppe (0 = Standard), damit SetGroupEnabled die Gruppe zur Laufzeit umschalten kann. |
| `world:EndStatic()  -> nil` | Schließt die Dreiecksmenge ab und baut das Broadphase-Raster auf. |
| `world:SetGroupEnabled(id, on)  -> nil` | Macht eine Collider-Gruppe fest oder passierbar, ohne die Dreiecksmenge neu aufzubauen (Türen, gestreamte Innenräume). |
| `world:ClearStatic()  -> nil` | Gibt die statische Dreiecksmenge und ihren Speicher sofort frei (Entladen einer Karte); setzt auch die Collider-Gruppen zurück. |
| `world:NewCharacter(radius)  -> body` | Kinematischer Kugelkörper: Sie setzen seine Geschwindigkeit, die Welt bewegt ihn per Collide-and-Slide. |
| `world:NewRigid(radius, mass)  -> body` | Kugelkörper mit aktivierter Schwerkraft, der Impuls mit anderen Körpern austauscht. |
| `world:NewBox(hx, hy, hz, mass)  -> body` | Körper mit aktivierter Schwerkraft, der mit der statischen Dreiecksmenge als orientierter Quader mit den angegebenen halben Ausdehnungen kollidiert (mit SetForward ausrichten). |
| `world:NewVehicle(radius, mass)  -> vehicle` | Autoähnlicher Körper: ein Charakter-Chassis mit Schwerkraft, Bodenhaftung und steilerer Bodenschwelle, plus Raycast-Rädern. |
| `world:NewMeshCollider()  -> mc` | Erzeugt einen leeren MeshCollider. |
| `world:RemoveBody(body)  -> nil` | Entfernt einen Körper aus der Welt. |
| `world:Step(dt)  -> nil` | Schreibt die Simulation um `dt` Sekunden fort. Die Welt berechnet schnelle Körper in Teilschritten, damit sie nicht durch dünne Wände tunneln. |
| `world:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> hit, dist, nx, ny, nz` | Wirft einen Strahl (Einheitsrichtung) gegen die aktivierten statischen Dreiecke; gibt zurück, ob der Strahl etwas getroffen hat, den nächsten Trefferabstand und die Normale dieses Dreiecks. |
| `world:GroundYAt(x, startY, z, reach)  -> number` | Bodenhöhe direkt unter (x, startY, z) innerhalb von `reach`, oder NaN, wenn keine vorhanden ist (mit `y ~= y` prüfen). |
| `world:StaticTriCount()  -> number` | Anzahl der Dreiecke in der statischen Dreiecksmenge. |
| `world:StaticTriCapacity()  -> number` | Reservierte Kapazität der Dreiecksliste. |

### Physikkörper-Handle

<span class="badge-exp">Experimentell</span>

<div class="callout warn">
NewCharacter, NewRigid und NewBox geben dieses Handle zurück. Positionen beziehen sich auf den Körpermittelpunkt. Die Welt löst Kontakte zwischen Körpern nur in der horizontalen Ebene auf und überspringt sie zwischen Körpern, die in Y mehr als 2 Einheiten auseinanderliegen.
</div>

| Methode | Beschreibung |
| --- | --- |
| `body:SetPos(x, y, z)  -> nil` | Setzt die Mittelpunktposition. |
| `body:SetVelocity(x, y, z)  -> nil` | Setzt die Geschwindigkeit. |
| `body:AddVelocity(x, y, z)  -> nil` | Addiert zur Geschwindigkeit. |
| `body:SetRadius(r)  -> nil` | Kugel- und Broadphase-Radius (Minimum 0.01). |
| `body:SetCapsule(halfLen)  -> nil` | Verwendet für Kontakte zwischen Körpern eine horizontale Kapsel dieser halben Länge entlang der Vorwärtsachse; die statische Kollision verwendet weiterhin den Kugelradius. |
| `body:SetBox(hx, hy, hz)  -> nil` | Kollidiert mit der statischen Dreiecksmenge als orientierter Quader mit halben Ausdehnungen (rechts, oben, vorwärts). |
| `body:SetForward(fx, fz)  -> nil` | Horizontale Vorwärtsachse (wird automatisch normalisiert), die Kapsel oder Quader ausrichtet. |
| `body:SetGravityEnabled(g)  -> nil` | Wendet die Weltschwerkraft auf diesen Körper an. |
| `body:SetEnabled(e)  -> nil` | Step überspringt deaktivierte Körper. |
| `body:SetSnap(s)  -> nil` | Hält einen am Boden befindlichen Körper innerhalb eines kleinen Abstands an abfallendem Boden fest; ohne Snap verlässt ein bergab laufender Körper den Boden und hüpft. |
| `body:SetSmoothContacts(on)  -> nil` | Variante des Charakter-Solvers: löst pro Iteration den tiefsten Kontakt, verschweißt Bodennähte, zeichnet Wandnormalen auf und steigt auf begehbaren Boden hinab. Standardmäßig aus. |
| `body:SetCollisionLayer(l)  -> nil` | Kollisionsebenen-Index 0..30 für Kontakte zwischen Körpern (Standard 0). |
| `body:SetCollisionMask(m)  -> nil` | Bitmaske der Ebenen, mit denen dieser Körper kollidiert (Bit i = Ebene i); Standard alle, 0 = keine. |
| `body:GetPos()  -> x, y, z` | Mittelpunktposition. |
| `body:GetVelocity()  -> x, y, z` | Geschwindigkeit. |
| `body:GetX()  -> number` | Mittelpunkt X. |
| `body:GetY()  -> number` | Mittelpunkt Y. |
| `body:GetZ()  -> number` | Mittelpunkt Z. |
| `body:GetVx()  -> number` | Geschwindigkeit X. |
| `body:GetVy()  -> number` | Geschwindigkeit Y. |
| `body:GetVz()  -> number` | Geschwindigkeit Z. |
| `body:Speed()  -> number` | Betrag der Geschwindigkeit. |
| `body:IsOnFloor()  -> boolean` | True, wenn der Körper im letzten Schritt auf Boden ruhte. |
| `body:HitWall()  -> boolean` | True, wenn der Körper im letzten Schritt eine Wand berührt hat. |
| `body:GetWallNx()  -> number` | X der letzten Wandkontaktnormalen (gesetzt, wenn SetSmoothContacts aktiv ist). |
| `body:GetWallNz()  -> number` | Z der letzten Wandkontaktnormalen (gesetzt, wenn SetSmoothContacts aktiv ist). |
| `body:GetImpulseX()  -> number` | X der Netto-Geschwindigkeitsänderung durch Kontakte zwischen Körpern im letzten Schritt. |
| `body:GetImpulseZ()  -> number` | Z der Netto-Geschwindigkeitsänderung durch Kontakte zwischen Körpern im letzten Schritt. |

### Fahrzeugkörper-Handle

<span class="badge-exp">Experimentell</span>

<div class="callout warn">
NewVehicle gibt dieses Handle zurück. Das Chassis ist ein Charakterkörper, den Step wie gewohnt bewegt; jeder UpdateWheels-Aufruf wirft die Räder als Strahlen vom Chassis nach unten und liefert ein Mehrrad-Bodenkontakt-Flag, die Federungskompression pro Rad sowie Nick- und Rollwinkel des Chassis, die der Neigung folgen. Ihr Fahrcode setzt die Chassis-Geschwindigkeit.
</div>

| Methode | Beschreibung |
| --- | --- |
| `vehicle:AddWheel(lx, lz, radius, rest, steered, powered)  -> wheel` | Fügt ein Rad am Chassis-Offset (`lx` rechts, `lz` vorwärts) mit dem angegebenen Radius und der Ruhelänge der Federung hinzu. Das Fahrzeug speichert `steered` und `powered` für Ihren Fahrcode und liest sie selbst nie. |
| `vehicle:GetWheel(i)  -> wheel` | Rad `i` (0-basiert), oder nil. |
| `vehicle:WheelCount()  -> number` | Anzahl der Räder. |
| `vehicle:UpdateWheels(dt, speed)  -> nil` | Wirft die Radstrahlen und aktualisiert Bodenkontakt, Kompression, Nick- und Rollwinkel sowie Raddrehung (`speed` in Welteinheiten pro Sekunde). Einmal pro Frame nach Step aufrufen. |
| `vehicle.Body  -> body` | Das Handle des Chassis-Körpers. |
| `vehicle:SetPos(x, y, z)  -> nil` | Setzt die Chassis-Position. |
| `vehicle:SetVelocity(x, y, z)  -> nil` | Setzt die Chassis-Geschwindigkeit. |
| `vehicle:AddVelocity(x, y, z)  -> nil` | Addiert zur Chassis-Geschwindigkeit. |
| `vehicle:SetForward(fx, fz)  -> nil` | Setzt die Vorwärtsachse und leitet die Ausrichtung ab. |
| `vehicle:SetYaw(y)  -> nil` | Setzt die Ausrichtung in Bogenmaß und leitet die Vorwärtsachse ab. |
| `vehicle:SetGravityEnabled(g)  -> nil` | Wendet Schwerkraft auf das Chassis an. |
| `vehicle:SetEnabled(e)  -> nil` | Aktiviert oder deaktiviert den Chassis-Körper. |
| `vehicle:SetCapsule(halfLen)  -> nil` | Halbe Kapsellänge für Kontakte zwischen Chassis und Körpern. |
| `vehicle:SetRadius(r)  -> nil` | Chassis-Radius. |
| `vehicle:GetX()  -> number` | Chassis X. |
| `vehicle:GetY()  -> number` | Chassis Y. |
| `vehicle:GetZ()  -> number` | Chassis Z. |
| `vehicle:GetVx()  -> number` | Chassis-Geschwindigkeit X. |
| `vehicle:GetVy()  -> number` | Chassis-Geschwindigkeit Y. |
| `vehicle:GetVz()  -> number` | Chassis-Geschwindigkeit Z. |
| `vehicle:IsOnFloor()  -> boolean` | True, wenn mindestens drei Räder Bodenkontakt haben oder das Chassis auf Boden ruht. |
| `vehicle:HitWall()  -> boolean` | True, wenn das Chassis im letzten Schritt eine Wand berührt hat. |
| `vehicle:GetImpulseX()  -> number` | X der Geschwindigkeitsänderung durch Kontakte zwischen Körpern, die dem Chassis zugefügt wurde. |
| `vehicle:GetImpulseZ()  -> number` | Z der Geschwindigkeitsänderung durch Kontakte zwischen Körpern, die dem Chassis zugefügt wurde. |
| `vehicle:GetPitch()  -> number` | Nickwinkel des Chassis in Bogenmaß, sanft an die Radkontakte angeglichen. |
| `vehicle:GetRoll()  -> number` | Rollwinkel des Chassis in Bogenmaß, sanft an die Radkontakte angeglichen. |

### Rad-Handle

<span class="badge-exp">Experimentell</span>

AddWheel und GetWheel geben dieses Handle zurück; lesen Sie es nach UpdateWheels aus.

| Methode | Beschreibung |
| --- | --- |
| `wheel:IsGrounded()  -> boolean` | True, wenn das Rad den Boden berührt. |
| `wheel:GetCompression()  -> number` | Federungskompression von 0 (vollständig ausgefahren oder in der Luft) bis 1 (vollständig eingefedert). |
| `wheel:GetSpin()  -> number` | Akkumulierter Drehwinkel in Bogenmaß für die Raddarstellung. |

### MeshCollider-Handle

<span class="badge-exp">Experimentell</span>

Eine Dreiecksliste für die statische Dreiecksmenge einer Welt; befüllen Sie sie von Hand oder mit model:BuildCollider.

| Methode | Beschreibung |
| --- | --- |
| `mc:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | Fügt ein Dreieck hinzu. |
| `mc:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | Fügt ein Quad als zwei Dreiecke hinzu. |
| `mc:TriCount()  -> number` | Anzahl der gesammelten Dreiecke. |
| `mc:Clear()  -> nil` | Entfernt alle Dreiecke. |
| `mc:AddToWorld(world)  -> nil` | Fügt die Dreiecke zur statischen Dreiecksmenge einer Welt hinzu (zwischen BeginStatic und EndStatic); wie world:AddMesh(mc). |

## Collider und Raycasting

### COLLIDERS

<span class="badge-exp">Experimentell</span>

Fabrik für Abfrageformen (Kugel, achsenparalleler Quader, Kapsel) und Strahlen für Treffertests, unabhängig von jeder Physikwelt.

| Methode | Beschreibung |
| --- | --- |
| `COLLIDERS:Sphere(cx, cy, cz, r)  -> collider` | Kugel mit Radius `r`, zentriert bei (cx, cy, cz). |
| `COLLIDERS:Box(cx, cy, cz, hx, hy, hz)  -> collider` | Achsenparalleler Quader mit halben Ausdehnungen (hx, hy, hz), zentriert bei (cx, cy, cz). |
| `COLLIDERS:Capsule(cx, cy, cz, hsx, hsy, hsz, r)  -> collider` | Kapsel mit Radius `r` um das Segment von Mittelpunkt - (hsx, hsy, hsz) bis Mittelpunkt + (hsx, hsy, hsz). |
| `COLLIDERS:CapsuleY(cx, cy, cz, halfHeight, r)  -> collider` | Vertikale Kapsel mit der angegebenen halben Höhe und dem Radius. |
| `COLLIDERS:Ray(ox, oy, oz, dx, dy, dz, len)  -> ray` | Wiederverwendbarer Strahl mit Ursprung, Richtung (wird automatisch normalisiert) und Länge. |

### Collider-Handle

<span class="badge-exp">Experimentell</span>

| Methode | Beschreibung |
| --- | --- |
| `collider:SetCenter(x, y, z)  -> nil` | Verschiebt die Form zu einem neuen Mittelpunkt. |
| `collider:SetRadius(r)  -> nil` | Setzt den Radius (nur Kugel-Collider). |
| `collider:SetHalfExtents(hx, hy, hz)  -> nil` | Setzt die halben Ausdehnungen (nur Quader-Collider). |
| `collider:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> number` | Abstand entlang des Strahls (Richtung wird automatisch normalisiert) bis zum ersten Treffer innerhalb von `maxDist`, oder -1 bei keinem Treffer. |
| `collider:HitsRay(ray)  -> boolean` | True, wenn der Strahl die Form innerhalb seiner Länge trifft. |
| `collider:CollideWith(other)  -> boolean` | True, wenn die Form einen anderen Collider überlappt oder wenn `other` ein Strahl ist, der sie trifft. |
| `collider:SetTag(tag)  -> nil` | Hängt einen beliebigen Lua-Wert an, damit Sie einen Treffer auf Ihr eigenes Objekt zurückführen können. |
| `collider:GetTag()  -> any` | Der angehängte Wert. |

### Ray-Handle

<span class="badge-exp">Experimentell</span>

| Methode | Beschreibung |
| --- | --- |
| `ray:Set(ox, oy, oz, dx, dy, dz, len)  -> nil` | Setzt Ursprung, Richtung (wird automatisch normalisiert) und Länge zurück. |

## Wegfindung

### PATHFIND

<span class="badge-exp">Experimentell</span>

Fabrik für gewichtete Navigationsgraphen, die FindPath mit A* durchsucht.

| Methode | Beschreibung |
| --- | --- |
| `PATHFIND:NewGraph()  -> graph` | Erzeugt einen leeren Graphen und gibt sein Handle zurück. |

### Navigationsgraph-Handle

<span class="badge-exp">Experimentell</span>

<div class="callout warn">
Knoten tragen eine Weltposition; Indizes beginnen bei 1. Jede Kante trägt ein Gewicht und einen Durchgangspunkt, die Position, die ein Beweger beim Nehmen dieser Kante passiert (zum Beispiel die Mitte einer Tür). FindPath gibt die Durchgangspunkte der genommenen Kanten zurück, gefolgt vom Zielpunkt.
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

| Methode | Beschreibung |
| --- | --- |
| `graph:AddNode(x, y, z)  -> number` | Fügt einen Knoten hinzu und gibt seinen Index zurück. |
| `graph:AddEdge(a, b, w, px, py, pz)  -> nil` | Fügt eine gerichtete Kante von Knoten `a` zu Knoten `b` mit Gewicht `w` durch den Durchgangspunkt (px, py, pz) hinzu. |
| `graph:Link(a, b, w, px, py, pz)  -> nil` | Fügt Kanten in beide Richtungen durch einen gemeinsamen Durchgangspunkt hinzu. |
| `graph:NearestNode(x, y, z)  -> number` | Index des Knotens, der (x, y, z) am nächsten liegt, oder 0, wenn der Graph leer ist. |
| `graph:FindPath(start, goal, goalX, goalY, goalZ)  -> path` | Führt A* von Knoten `start` zu Knoten `goal` aus; gibt einen Pfad zurück, der bei (goalX, goalY, goalZ) endet, oder nil, wenn unerreichbar. |
| `graph:NodeCount()  -> number` | Anzahl der Knoten. |
| `graph:Clear()  -> nil` | Entfernt alle Knoten und Kanten. |

### Navigationspfad-Handle

<span class="badge-exp">Experimentell</span>

Wegpunkt-Indizes beginnen bei 1.

| Methode | Beschreibung |
| --- | --- |
| `path:Count()  -> number` | Anzahl der Wegpunkte. |
| `path:X(i)  -> number` | X des Wegpunkts `i`. |
| `path:Y(i)  -> number` | Y des Wegpunkts `i`. |
| `path:Z(i)  -> number` | Z des Wegpunkts `i`. |
