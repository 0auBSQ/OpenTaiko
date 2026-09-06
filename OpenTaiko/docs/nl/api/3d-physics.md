<!-- api/3d-physics.md -->

# 3D-engine: fysica <span class="badge-exp">Experimenteel</span>

Starre lichamen, personages en voertuigen, statische botsing, collider-queries met raycasts en navigatiegrafen. Posities zijn wereldeenheden en +Y is omhoog; [3D-engine: rasterizerwereld](3d.md) beschrijft de gedeelde conventies.

## Fysica

### PHYSICS

<span class="badge-exp">Experimenteel</span>

Factory voor fysicawerelden: een statische driehoekensoep plus dynamische character-, rigid-, box- en voertuigbodies.

| Methode | Beschrijving |
| --- | --- |
| `PHYSICS:NewWorld()  -> world` | Maakt een fysicawereld en geeft de handle terug. |

### Fysicawereld-handle

<span class="badge-exp">Experimenteel</span>

<div class="callout warn">
Bouw de statische geometrie tussen BeginStatic en EndStatic, maak bodies, stel elk frame hun snelheid in en roep Step aan. Bodies botsen met de statische soep via collide-and-slide (bol of georiënteerde doos tegen driehoeken) en met elkaar als bollen of capsules in het horizontale vlak, waarbij ze impuls uitwisselen op basis van massa.
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

-- elk frame
body:SetVelocity(moveX, body:GetVy(), moveZ)
world:Step(dt)
local x, y, z = body:GetX(), body:GetY(), body:GetZ()
```

| Methode | Beschrijving |
| --- | --- |
| `world:SetGravity(x, y, z)  -> nil` | Zwaartekrachtvector (standaard 0, -32, 0). |
| `world:SetFloorMaxAngleY(y)  -> nil` | Drempel voor de Y van de contactnormaal (standaard 0.5): contacten erboven tellen als vloer, de rest als muur. |
| `world:BeginStatic()  -> nil` | Begint met het opnieuw opbouwen van de statische soep; wist bestaande driehoeken en collidergroepen. |
| `world:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | Voegt een statische driehoek toe. Zijn normaal is (b - a) x (c - a); de wereld slaat gedegenereerde driehoeken over. |
| `world:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | Voegt een statische quad toe als twee driehoeken (a, b, c) en (a, c, d). |
| `world:AddMesh(mc)  -> nil` | Voegt de driehoeken van een MeshCollider toe aan de soep. |
| `world:BeginGroup(id)  -> nil` | Tagt driehoeken die vanaf nu worden toegevoegd met een collidergroep (0 = standaard), zodat SetGroupEnabled de groep tijdens runtime kan in- of uitschakelen. |
| `world:EndStatic()  -> nil` | Rondt de soep af en bouwt het broadphase-raster. |
| `world:SetGroupEnabled(id, on)  -> nil` | Maakt een collidergroep vast of doorlaatbaar zonder de soep opnieuw op te bouwen (deuren, gestreamde interieurs). |
| `world:ClearStatic()  -> nil` | Geeft de statische soep en zijn opslag nu vrij (ontladen van een kaart); reset ook de collidergroepen. |
| `world:NewCharacter(radius)  -> body` | Kinematische bolbody: jij stelt de snelheid in, de wereld verplaatst hem met collide-and-slide. |
| `world:NewRigid(radius, mass)  -> body` | Bolbody met zwaartekracht ingeschakeld die impuls uitwisselt met andere bodies. |
| `world:NewBox(hx, hy, hz, mass)  -> body` | Body met zwaartekracht ingeschakeld die met de statische soep botst als georiënteerde doos met de gegeven halve afmetingen (oriënteer hem met SetForward). |
| `world:NewVehicle(radius, mass)  -> vehicle` | Autoachtige body: een characterchassis met zwaartekracht, grondsnap en een steilere vloerdrempel, plus raycastwielen. |
| `world:NewMeshCollider()  -> mc` | Maakt een lege MeshCollider. |
| `world:RemoveBody(body)  -> nil` | Verwijdert een body uit de wereld. |
| `world:Step(dt)  -> nil` | Laat de simulatie `dt` seconden vorderen. De wereld verwerkt snelle bodies in substappen, zodat ze niet door dunne muren tunnelen. |
| `world:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> hit, dist, nx, ny, nz` | Werpt een straal (eenheidsrichting) tegen de ingeschakelde statische driehoeken; geeft terug of de straal iets heeft geraakt, de dichtstbijzijnde trefafstand en de normaal van die driehoek. |
| `world:GroundYAt(x, startY, z, reach)  -> number` | Grondhoogte recht onder (x, startY, z) binnen `reach`, of NaN wanneer er geen is (test met `y ~= y`). |
| `world:StaticTriCount()  -> number` | Aantal driehoeken in de statische soep. |
| `world:StaticTriCapacity()  -> number` | Toegewezen capaciteit van de driehoekenlijst. |

### Fysicabody-handle

<span class="badge-exp">Experimenteel</span>

<div class="callout warn">
NewCharacter, NewRigid en NewBox geven deze handle terug. Posities verwijzen naar het middelpunt van de body. De wereld lost contacten tussen bodies alleen in het horizontale vlak op en slaat ze over tussen bodies die meer dan 2 eenheden in Y uit elkaar liggen.
</div>

| Methode | Beschrijving |
| --- | --- |
| `body:SetPos(x, y, z)  -> nil` | Stelt de middelpuntpositie in. |
| `body:SetVelocity(x, y, z)  -> nil` | Stelt de snelheid in. |
| `body:AddVelocity(x, y, z)  -> nil` | Telt op bij de snelheid. |
| `body:SetRadius(r)  -> nil` | Bol- en broadphasestraal (minimaal 0.01). |
| `body:SetCapsule(halfLen)  -> nil` | Gebruikt een horizontale capsule met deze halve lengte langs de voorwaartse as voor contacten tussen bodies; statische botsing blijft de bolstraal gebruiken. |
| `body:SetBox(hx, hy, hz)  -> nil` | Botst met de statische soep als georiënteerde doos met halve afmetingen (rechts, omhoog, vooruit). |
| `body:SetForward(fx, fz)  -> nil` | Horizontale voorwaartse as (automatisch genormaliseerd) die de capsule of doos oriënteert. |
| `body:SetGravityEnabled(g)  -> nil` | Past de wereldzwaartekracht op deze body toe. |
| `body:SetEnabled(e)  -> nil` | Step slaat uitgeschakelde bodies over. |
| `body:SetSnap(s)  -> nil` | Houdt een body op de grond vastgeplakt aan dalende grond binnen een kleine spleet; zonder snap verlaat een body die bergaf loopt de grond en springt hij op. |
| `body:SetSmoothContacts(on)  -> nil` | Variant van de charactersolver: lost per iteratie het diepste contact op, last vloernaden aan elkaar, registreert muurnormalen en stapt omlaag op beloopbare grond. Standaard uit. |
| `body:SetCollisionLayer(l)  -> nil` | Botsingslaagindex 0..30 voor contacten tussen bodies (standaard 0). |
| `body:SetCollisionMask(m)  -> nil` | Bitmasker van lagen waarmee deze body botst (bit i = laag i); standaard alle, 0 = geen. |
| `body:GetPos()  -> x, y, z` | Middelpuntpositie. |
| `body:GetVelocity()  -> x, y, z` | Snelheid. |
| `body:GetX()  -> number` | Middelpunt-X. |
| `body:GetY()  -> number` | Middelpunt-Y. |
| `body:GetZ()  -> number` | Middelpunt-Z. |
| `body:GetVx()  -> number` | Snelheid X. |
| `body:GetVy()  -> number` | Snelheid Y. |
| `body:GetVz()  -> number` | Snelheid Z. |
| `body:Speed()  -> number` | Grootte van de snelheid. |
| `body:IsOnFloor()  -> boolean` | True wanneer de body tijdens de laatste stap op de vloer rustte. |
| `body:HitWall()  -> boolean` | True wanneer de body tijdens de laatste stap een muur raakte. |
| `body:GetWallNx()  -> number` | X van de laatste muurcontactnormaal (gezet wanneer SetSmoothContacts aan staat). |
| `body:GetWallNz()  -> number` | Z van de laatste muurcontactnormaal (gezet wanneer SetSmoothContacts aan staat). |
| `body:GetImpulseX()  -> number` | X van de netto snelheidsverandering die tijdens de laatste stap door contacten tussen bodies is toegebracht. |
| `body:GetImpulseZ()  -> number` | Z van de netto snelheidsverandering die tijdens de laatste stap door contacten tussen bodies is toegebracht. |

### Voertuigbody-handle

<span class="badge-exp">Experimenteel</span>

<div class="callout warn">
NewVehicle geeft deze handle terug. Het chassis is een characterbody die Step zoals gewoonlijk verplaatst; elke UpdateWheels-aanroep werpt de wielen als stralen vanuit het chassis naar beneden, wat een gegrond-vlag over meerdere wielen, veringcompressie per wiel en een chassispitch en -roll oplevert die de helling volgen. Je eigen bestuurcode stelt de chassissnelheid in.
</div>

| Methode | Beschrijving |
| --- | --- |
| `vehicle:AddWheel(lx, lz, radius, rest, steered, powered)  -> wheel` | Voegt een wiel toe op chassisoffset (`lx` rechts, `lz` vooruit) met de gegeven straal en rustlengte van de vering. Het voertuig slaat `steered` en `powered` op voor je bestuurcode en leest ze zelf nooit. |
| `vehicle:GetWheel(i)  -> wheel` | Wiel `i` (vanaf 0), of nil. |
| `vehicle:WheelCount()  -> number` | Aantal wielen. |
| `vehicle:UpdateWheels(dt, speed)  -> nil` | Werpt de wielstralen en werkt gegrond, compressie, pitch, roll en wielrotatie bij (`speed` in wereldeenheden per seconde). Eenmaal per frame aanroepen na Step. |
| `vehicle.Body  -> body` | De chassisbody-handle. |
| `vehicle:SetPos(x, y, z)  -> nil` | Stelt de chassispositie in. |
| `vehicle:SetVelocity(x, y, z)  -> nil` | Stelt de chassissnelheid in. |
| `vehicle:AddVelocity(x, y, z)  -> nil` | Telt op bij de chassissnelheid. |
| `vehicle:SetForward(fx, fz)  -> nil` | Stelt de voorwaartse as in en leidt de koers af. |
| `vehicle:SetYaw(y)  -> nil` | Stelt de koers in radialen in en leidt de voorwaartse as af. |
| `vehicle:SetGravityEnabled(g)  -> nil` | Past zwaartekracht op het chassis toe. |
| `vehicle:SetEnabled(e)  -> nil` | Schakelt de chassisbody in of uit. |
| `vehicle:SetCapsule(halfLen)  -> nil` | Halve capsulelengte voor contacten tussen chassis en bodies. |
| `vehicle:SetRadius(r)  -> nil` | Chassisstraal. |
| `vehicle:GetX()  -> number` | Chassis-X. |
| `vehicle:GetY()  -> number` | Chassis-Y. |
| `vehicle:GetZ()  -> number` | Chassis-Z. |
| `vehicle:GetVx()  -> number` | Chassissnelheid X. |
| `vehicle:GetVy()  -> number` | Chassissnelheid Y. |
| `vehicle:GetVz()  -> number` | Chassissnelheid Z. |
| `vehicle:IsOnFloor()  -> boolean` | True wanneer minstens drie wielen gegrond zijn of het chassis op de vloer rust. |
| `vehicle:HitWall()  -> boolean` | True wanneer het chassis tijdens de laatste stap een muur raakte. |
| `vehicle:GetImpulseX()  -> number` | X van de snelheidsverandering die door contacten tussen bodies aan het chassis is toegebracht. |
| `vehicle:GetImpulseZ()  -> number` | Z van de snelheidsverandering die door contacten tussen bodies aan het chassis is toegebracht. |
| `vehicle:GetPitch()  -> number` | Chassispitch in radialen, soepel richting de wielcontacten bewogen. |
| `vehicle:GetRoll()  -> number` | Chassisroll in radialen, soepel richting de wielcontacten bewogen. |

### Wiel-handle

<span class="badge-exp">Experimenteel</span>

AddWheel en GetWheel geven deze handle terug; lees hem na UpdateWheels.

| Methode | Beschrijving |
| --- | --- |
| `wheel:IsGrounded()  -> boolean` | True wanneer het wiel de grond raakt. |
| `wheel:GetCompression()  -> number` | Veringcompressie van 0 (volledig uitgestrekt of in de lucht) tot 1 (volledig ingedrukt). |
| `wheel:GetSpin()  -> number` | Opgetelde rolhoek in radialen voor de wielvisuals. |

### MeshCollider-handle

<span class="badge-exp">Experimenteel</span>

Een driehoekenlijst voor de statische soep van een wereld; vul hem met de hand of met model:BuildCollider.

| Methode | Beschrijving |
| --- | --- |
| `mc:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | Voegt één driehoek toe. |
| `mc:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | Voegt een quad toe als twee driehoeken. |
| `mc:TriCount()  -> number` | Aantal verzamelde driehoeken. |
| `mc:Clear()  -> nil` | Verwijdert alle driehoeken. |
| `mc:AddToWorld(world)  -> nil` | Voegt de driehoeken toe aan de statische soep van een wereld (tussen BeginStatic en EndStatic); hetzelfde als world:AddMesh(mc). |

## Colliders en raycasting

### COLLIDERS

<span class="badge-exp">Experimenteel</span>

Factory voor queryvormen (bol, as-uitgelijnde doos, capsule) en stralen voor treftests, onafhankelijk van welke fysicawereld dan ook.

| Methode | Beschrijving |
| --- | --- |
| `COLLIDERS:Sphere(cx, cy, cz, r)  -> collider` | Bol met straal `r`, gecentreerd op (cx, cy, cz). |
| `COLLIDERS:Box(cx, cy, cz, hx, hy, hz)  -> collider` | As-uitgelijnde doos met halve afmetingen (hx, hy, hz), gecentreerd op (cx, cy, cz). |
| `COLLIDERS:Capsule(cx, cy, cz, hsx, hsy, hsz, r)  -> collider` | Capsule met straal `r` rond het segment van middelpunt - (hsx, hsy, hsz) tot middelpunt + (hsx, hsy, hsz). |
| `COLLIDERS:CapsuleY(cx, cy, cz, halfHeight, r)  -> collider` | Verticale capsule met de gegeven halve hoogte en straal. |
| `COLLIDERS:Ray(ox, oy, oz, dx, dy, dz, len)  -> ray` | Herbruikbare straal met een oorsprong, een richting (automatisch genormaliseerd) en een lengte. |

### Collider-handle

<span class="badge-exp">Experimenteel</span>

| Methode | Beschrijving |
| --- | --- |
| `collider:SetCenter(x, y, z)  -> nil` | Verplaatst de vorm naar een nieuw middelpunt. |
| `collider:SetRadius(r)  -> nil` | Stelt de straal in (alleen bolcolliders). |
| `collider:SetHalfExtents(hx, hy, hz)  -> nil` | Stelt de halve afmetingen in (alleen dooscolliders). |
| `collider:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> number` | Afstand langs de straal (richting automatisch genormaliseerd) tot de eerste treffer binnen `maxDist`, of -1 bij een misser. |
| `collider:HitsRay(ray)  -> boolean` | True wanneer de straal de vorm binnen zijn lengte raakt. |
| `collider:CollideWith(other)  -> boolean` | True wanneer de vorm een andere collider overlapt, of wanneer `other` een straal is die hem raakt. |
| `collider:SetTag(tag)  -> nil` | Koppelt een willekeurige Lua-waarde, zodat je een treffer naar je eigen object kunt terugleiden. |
| `collider:GetTag()  -> any` | De gekoppelde waarde. |

### Straal-handle

<span class="badge-exp">Experimenteel</span>

| Methode | Beschrijving |
| --- | --- |
| `ray:Set(ox, oy, oz, dx, dy, dz, len)  -> nil` | Zet de oorsprong, richting (automatisch genormaliseerd) en lengte opnieuw. |

## Pathfinding

### PATHFIND

<span class="badge-exp">Experimenteel</span>

Factory voor gewogen navigatiegrafen die FindPath met A* doorzoekt.

| Methode | Beschrijving |
| --- | --- |
| `PATHFIND:NewGraph()  -> graph` | Maakt een lege graaf en geeft de handle terug. |

### Navigatiegraaf-handle

<span class="badge-exp">Experimenteel</span>

<div class="callout warn">
Knopen dragen een wereldpositie; indices beginnen bij 1. Elke kant draagt een gewicht en een doorgangspunt, de positie waar een beweger doorheen gaat wanneer hij die kant neemt (bijvoorbeeld het midden van een deuropening). FindPath geeft de doorgangspunten van de kanten die het heeft genomen terug, gevolgd door het doelpunt.
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

| Methode | Beschrijving |
| --- | --- |
| `graph:AddNode(x, y, z)  -> number` | Voegt een knoop toe en geeft zijn index terug. |
| `graph:AddEdge(a, b, w, px, py, pz)  -> nil` | Voegt een gerichte kant toe van knoop `a` naar knoop `b` met gewicht `w` via doorgangspunt (px, py, pz). |
| `graph:Link(a, b, w, px, py, pz)  -> nil` | Voegt kanten in beide richtingen toe via één gedeeld doorgangspunt. |
| `graph:NearestNode(x, y, z)  -> number` | Index van de knoop die het dichtst bij (x, y, z) ligt, of 0 wanneer de graaf leeg is. |
| `graph:FindPath(start, goal, goalX, goalY, goalZ)  -> path` | Voert A* uit van knoop `start` naar knoop `goal`; geeft een pad terug dat eindigt op (goalX, goalY, goalZ), of nil wanneer onbereikbaar. |
| `graph:NodeCount()  -> number` | Aantal knopen. |
| `graph:Clear()  -> nil` | Verwijdert alle knopen en kanten. |

### Navigatiepad-handle

<span class="badge-exp">Experimenteel</span>

Waypoint-indices beginnen bij 1.

| Methode | Beschrijving |
| --- | --- |
| `path:Count()  -> number` | Aantal waypoints. |
| `path:X(i)  -> number` | X van waypoint `i`. |
| `path:Y(i)  -> number` | Y van waypoint `i`. |
| `path:Z(i)  -> number` | Z van waypoint `i`. |
