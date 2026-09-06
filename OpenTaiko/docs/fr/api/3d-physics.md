<!-- api/3d-physics.md -->

# Moteur 3D : physique <span class="badge-exp">Expérimental</span>

Corps rigides, personnages et véhicules, collision statique, requêtes de colliders par raycast et graphes de navigation. Les positions sont en unités du monde et +Y pointe vers le haut ; [Moteur 3D : monde du rastériseur](3d.md) décrit les conventions communes.

## Physique

### PHYSICS

<span class="badge-exp">Expérimental</span>

Fabrique de mondes physiques : une soupe de triangles statique plus des corps dynamiques de type personnage, rigide, boîte et véhicule.

| Méthode | Description |
| --- | --- |
| `PHYSICS:NewWorld()  -> world` | Crée un monde physique et renvoie son handle. |

### Handle de monde physique

<span class="badge-exp">Expérimental</span>

<div class="callout warn">
Construisez la géométrie statique entre BeginStatic et EndStatic, créez des corps, définissez leur vitesse à chaque frame et appelez Step. Les corps entrent en collision avec la soupe statique par collide-and-slide (sphère ou boîte orientée contre des triangles) et entre eux comme des sphères ou des capsules dans le plan horizontal, en échangeant de la quantité de mouvement selon leur masse.
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

-- à chaque frame
body:SetVelocity(moveX, body:GetVy(), moveZ)
world:Step(dt)
local x, y, z = body:GetX(), body:GetY(), body:GetZ()
```

| Méthode | Description |
| --- | --- |
| `world:SetGravity(x, y, z)  -> nil` | Vecteur de gravité (0, -32, 0 par défaut). |
| `world:SetFloorMaxAngleY(y)  -> nil` | Seuil du Y de la normale de contact (0.5 par défaut) : les contacts au-dessus comptent comme sol, les autres comme mur. |
| `world:BeginStatic()  -> nil` | Commence la reconstruction de la soupe statique ; efface les triangles et les groupes de colliders existants. |
| `world:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | Ajoute un triangle statique. Sa normale est (b - a) x (c - a) ; le monde ignore les triangles dégénérés. |
| `world:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | Ajoute un quad statique sous forme de deux triangles (a, b, c) et (a, c, d). |
| `world:AddMesh(mc)  -> nil` | Ajoute les triangles d'un MeshCollider à la soupe. |
| `world:BeginGroup(id)  -> nil` | Marque les triangles ajoutés à partir de maintenant avec un groupe de colliders (0 = par défaut) pour que SetGroupEnabled puisse activer ou désactiver le groupe à l'exécution. |
| `world:EndStatic()  -> nil` | Termine la soupe et construit la grille de broadphase. |
| `world:SetGroupEnabled(id, on)  -> nil` | Rend un groupe de colliders solide ou traversable sans reconstruire la soupe (portes, intérieurs chargés en flux). |
| `world:ClearStatic()  -> nil` | Libère maintenant la soupe statique et son stockage (déchargement de carte) ; réinitialise aussi les groupes de colliders. |
| `world:NewCharacter(radius)  -> body` | Corps sphérique cinématique : vous définissez sa vitesse, le monde le déplace par collide-and-slide. |
| `world:NewRigid(radius, mass)  -> body` | Corps sphérique avec gravité activée qui échange de la quantité de mouvement avec les autres corps. |
| `world:NewBox(hx, hy, hz, mass)  -> body` | Corps avec gravité qui entre en collision avec la soupe statique comme une boîte orientée des demi-étendues données (orientez-la avec SetForward). |
| `world:NewVehicle(radius, mass)  -> vehicle` | Corps de type voiture : un châssis de personnage avec gravité, accroche au sol et seuil de sol plus raide, plus des roues par lancer de rayon. |
| `world:NewMeshCollider()  -> mc` | Crée un MeshCollider vide. |
| `world:RemoveBody(body)  -> nil` | Retire un corps du monde. |
| `world:Step(dt)  -> nil` | Fait avancer la simulation de `dt` secondes. Le monde sous-échantillonne les corps rapides pour qu'ils ne traversent pas les murs fins. |
| `world:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> hit, dist, nx, ny, nz` | Lance un rayon (direction unitaire) contre les triangles statiques activés ; renvoie si le rayon a touché quelque chose, la distance de l'impact le plus proche et la normale de ce triangle. |
| `world:GroundYAt(x, startY, z, reach)  -> number` | Hauteur du sol juste sous (x, startY, z) dans la limite de `reach`, ou NaN s'il n'y en a pas (testez avec `y ~= y`). |
| `world:StaticTriCount()  -> number` | Nombre de triangles dans la soupe statique. |
| `world:StaticTriCapacity()  -> number` | Capacité allouée de la liste de triangles. |

### Handle de corps physique

<span class="badge-exp">Expérimental</span>

<div class="callout warn">
NewCharacter, NewRigid et NewBox renvoient ce handle. Les positions désignent le centre du corps. Le monde résout les contacts corps contre corps dans le plan horizontal uniquement et les ignore entre corps distants de plus de 2 unités en Y.
</div>

| Méthode | Description |
| --- | --- |
| `body:SetPos(x, y, z)  -> nil` | Définit la position du centre. |
| `body:SetVelocity(x, y, z)  -> nil` | Définit la vitesse. |
| `body:AddVelocity(x, y, z)  -> nil` | Ajoute à la vitesse. |
| `body:SetRadius(r)  -> nil` | Rayon de la sphère et de la broadphase (minimum 0.01). |
| `body:SetCapsule(halfLen)  -> nil` | Utilise une capsule horizontale de cette demi-longueur le long de l'axe avant pour les contacts corps contre corps ; la collision statique continue d'utiliser le rayon de la sphère. |
| `body:SetBox(hx, hy, hz)  -> nil` | Entre en collision avec la soupe statique comme une boîte orientée de demi-étendues (droite, haut, avant). |
| `body:SetForward(fx, fz)  -> nil` | Axe avant horizontal (normalisé automatiquement) qui oriente la capsule ou la boîte. |
| `body:SetGravityEnabled(g)  -> nil` | Applique la gravité du monde à ce corps. |
| `body:SetEnabled(e)  -> nil` | Step ignore les corps désactivés. |
| `body:SetSnap(s)  -> nil` | Garde un corps au sol collé à un sol descendant dans un petit écart ; sans snap, un corps qui dévale une pente quitte le sol et saute. |
| `body:SetSmoothContacts(on)  -> nil` | Variante du solveur de personnage : résout le contact le plus profond par itération, soude les jointures de sol, enregistre les normales de mur et descend sur un sol praticable. Désactivé par défaut. |
| `body:SetCollisionLayer(l)  -> nil` | Indice de couche de collision 0..30 pour les contacts corps contre corps (0 par défaut). |
| `body:SetCollisionMask(m)  -> nil` | Masque de bits des couches avec lesquelles ce corps entre en collision (bit i = couche i) ; toutes par défaut, 0 = aucune. |
| `body:GetPos()  -> x, y, z` | Position du centre. |
| `body:GetVelocity()  -> x, y, z` | Vitesse. |
| `body:GetX()  -> number` | X du centre. |
| `body:GetY()  -> number` | Y du centre. |
| `body:GetZ()  -> number` | Z du centre. |
| `body:GetVx()  -> number` | Vitesse X. |
| `body:GetVy()  -> number` | Vitesse Y. |
| `body:GetVz()  -> number` | Vitesse Z. |
| `body:Speed()  -> number` | Norme de la vitesse. |
| `body:IsOnFloor()  -> boolean` | Vrai quand le corps reposait sur le sol lors du dernier pas. |
| `body:HitWall()  -> boolean` | Vrai quand le corps a touché un mur lors du dernier pas. |
| `body:GetWallNx()  -> number` | X de la dernière normale de contact avec un mur (définie quand SetSmoothContacts est activé). |
| `body:GetWallNz()  -> number` | Z de la dernière normale de contact avec un mur (définie quand SetSmoothContacts est activé). |
| `body:GetImpulseX()  -> number` | X du changement net de vitesse infligé par les contacts corps contre corps lors du dernier pas. |
| `body:GetImpulseZ()  -> number` | Z du changement net de vitesse infligé par les contacts corps contre corps lors du dernier pas. |

### Handle de corps véhicule

<span class="badge-exp">Expérimental</span>

<div class="callout warn">
NewVehicle renvoie ce handle. Le châssis est un corps de personnage que Step déplace comme d'habitude ; chaque appel d'UpdateWheels lance les roues comme des rayons vers le bas depuis le châssis, fournissant un drapeau de contact au sol multi-roues, une compression de suspension par roue et un tangage et un roulis du châssis qui suivent la pente. Votre code de conduite définit la vitesse du châssis.
</div>

| Méthode | Description |
| --- | --- |
| `vehicle:AddWheel(lx, lz, radius, rest, steered, powered)  -> wheel` | Ajoute une roue au décalage en espace châssis (`lx` droite, `lz` avant) avec le rayon et la longueur de repos de suspension donnés. Le véhicule stocke `steered` et `powered` pour votre code de conduite et ne les lit jamais lui-même. |
| `vehicle:GetWheel(i)  -> wheel` | Roue `i` (à partir de 0), ou nil. |
| `vehicle:WheelCount()  -> number` | Nombre de roues. |
| `vehicle:UpdateWheels(dt, speed)  -> nil` | Lance les rayons des roues et met à jour le contact au sol, la compression, le tangage, le roulis et la rotation des roues (`speed` en unités monde par seconde). À appeler une fois par frame après Step. |
| `vehicle.Body  -> body` | Le handle du corps du châssis. |
| `vehicle:SetPos(x, y, z)  -> nil` | Définit la position du châssis. |
| `vehicle:SetVelocity(x, y, z)  -> nil` | Définit la vitesse du châssis. |
| `vehicle:AddVelocity(x, y, z)  -> nil` | Ajoute à la vitesse du châssis. |
| `vehicle:SetForward(fx, fz)  -> nil` | Définit l'axe avant et en déduit le cap. |
| `vehicle:SetYaw(y)  -> nil` | Définit le cap en radians et en déduit l'axe avant. |
| `vehicle:SetGravityEnabled(g)  -> nil` | Applique la gravité au châssis. |
| `vehicle:SetEnabled(e)  -> nil` | Active ou désactive le corps du châssis. |
| `vehicle:SetCapsule(halfLen)  -> nil` | Demi-longueur de capsule pour les contacts châssis contre corps. |
| `vehicle:SetRadius(r)  -> nil` | Rayon du châssis. |
| `vehicle:GetX()  -> number` | X du châssis. |
| `vehicle:GetY()  -> number` | Y du châssis. |
| `vehicle:GetZ()  -> number` | Z du châssis. |
| `vehicle:GetVx()  -> number` | Vitesse X du châssis. |
| `vehicle:GetVy()  -> number` | Vitesse Y du châssis. |
| `vehicle:GetVz()  -> number` | Vitesse Z du châssis. |
| `vehicle:IsOnFloor()  -> boolean` | Vrai quand au moins trois roues touchent le sol ou que le châssis repose sur le sol. |
| `vehicle:HitWall()  -> boolean` | Vrai quand le châssis a touché un mur lors du dernier pas. |
| `vehicle:GetImpulseX()  -> number` | X du changement de vitesse corps contre corps infligé au châssis. |
| `vehicle:GetImpulseZ()  -> number` | Z du changement de vitesse corps contre corps infligé au châssis. |
| `vehicle:GetPitch()  -> number` | Tangage du châssis en radians, lissé vers les contacts des roues. |
| `vehicle:GetRoll()  -> number` | Roulis du châssis en radians, lissé vers les contacts des roues. |

### Handle de roue

<span class="badge-exp">Expérimental</span>

AddWheel et GetWheel renvoient ce handle ; lisez-le après UpdateWheels.

| Méthode | Description |
| --- | --- |
| `wheel:IsGrounded()  -> boolean` | Vrai quand la roue touche le sol. |
| `wheel:GetCompression()  -> number` | Compression de la suspension de 0 (entièrement détendue ou en l'air) à 1 (entièrement comprimée). |
| `wheel:GetSpin()  -> number` | Angle de roulement accumulé en radians pour le visuel des roues. |

### Handle MeshCollider

<span class="badge-exp">Expérimental</span>

Une liste de triangles pour la soupe statique d'un monde ; remplissez-la à la main ou avec model:BuildCollider.

| Méthode | Description |
| --- | --- |
| `mc:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | Ajoute un triangle. |
| `mc:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | Ajoute un quad sous forme de deux triangles. |
| `mc:TriCount()  -> number` | Nombre de triangles collectés. |
| `mc:Clear()  -> nil` | Retire tous les triangles. |
| `mc:AddToWorld(world)  -> nil` | Ajoute les triangles à la soupe statique d'un monde (entre BeginStatic et EndStatic) ; identique à world:AddMesh(mc). |

## Colliders et lancer de rayons

### COLLIDERS

<span class="badge-exp">Expérimental</span>

Fabrique de formes de requête (sphère, boîte alignée sur les axes, capsule) et de rayons pour les tests de collision, indépendants de tout monde physique.

| Méthode | Description |
| --- | --- |
| `COLLIDERS:Sphere(cx, cy, cz, r)  -> collider` | Sphère de rayon `r` centrée en (cx, cy, cz). |
| `COLLIDERS:Box(cx, cy, cz, hx, hy, hz)  -> collider` | Boîte alignée sur les axes de demi-étendues (hx, hy, hz) centrée en (cx, cy, cz). |
| `COLLIDERS:Capsule(cx, cy, cz, hsx, hsy, hsz, r)  -> collider` | Capsule de rayon `r` autour du segment allant de centre - (hsx, hsy, hsz) à centre + (hsx, hsy, hsz). |
| `COLLIDERS:CapsuleY(cx, cy, cz, halfHeight, r)  -> collider` | Capsule verticale de la demi-hauteur et du rayon donnés. |
| `COLLIDERS:Ray(ox, oy, oz, dx, dy, dz, len)  -> ray` | Rayon réutilisable avec une origine, une direction (normalisée automatiquement) et une longueur. |

### Handle de collider

<span class="badge-exp">Expérimental</span>

| Méthode | Description |
| --- | --- |
| `collider:SetCenter(x, y, z)  -> nil` | Déplace la forme vers un nouveau centre. |
| `collider:SetRadius(r)  -> nil` | Définit le rayon (colliders sphère uniquement). |
| `collider:SetHalfExtents(hx, hy, hz)  -> nil` | Définit les demi-étendues (colliders boîte uniquement). |
| `collider:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> number` | Distance le long du rayon (direction normalisée automatiquement) jusqu'au premier impact dans la limite de `maxDist`, ou -1 en cas d'absence d'impact. |
| `collider:HitsRay(ray)  -> boolean` | Vrai quand le rayon touche la forme dans la limite de sa longueur. |
| `collider:CollideWith(other)  -> boolean` | Vrai quand la forme chevauche un autre collider, ou quand `other` est un rayon qui la touche. |
| `collider:SetTag(tag)  -> nil` | Attache n'importe quelle valeur Lua pour que vous puissiez rattacher un impact à votre propre objet. |
| `collider:GetTag()  -> any` | La valeur attachée. |

### Handle de rayon

<span class="badge-exp">Expérimental</span>

| Méthode | Description |
| --- | --- |
| `ray:Set(ox, oy, oz, dx, dy, dz, len)  -> nil` | Redéfinit l'origine, la direction (normalisée automatiquement) et la longueur. |

## Recherche de chemin

### PATHFIND

<span class="badge-exp">Expérimental</span>

Fabrique de graphes de navigation pondérés que FindPath parcourt avec A*.

| Méthode | Description |
| --- | --- |
| `PATHFIND:NewGraph()  -> graph` | Crée un graphe vide et renvoie son handle. |

### Handle de graphe de navigation

<span class="badge-exp">Expérimental</span>

<div class="callout warn">
Les nœuds portent une position monde ; les indices commencent à 1. Chaque arête porte un poids et un point de transit, la position par laquelle passe un mobile qui emprunte cette arête (par exemple le milieu d'une porte). FindPath renvoie les points de transit des arêtes qu'il a empruntées, suivis du point d'arrivée.
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

| Méthode | Description |
| --- | --- |
| `graph:AddNode(x, y, z)  -> number` | Ajoute un nœud et renvoie son indice. |
| `graph:AddEdge(a, b, w, px, py, pz)  -> nil` | Ajoute une arête orientée du nœud `a` au nœud `b` de poids `w` passant par le point de transit (px, py, pz). |
| `graph:Link(a, b, w, px, py, pz)  -> nil` | Ajoute des arêtes dans les deux sens passant par un même point de transit. |
| `graph:NearestNode(x, y, z)  -> number` | Indice du nœud le plus proche de (x, y, z), ou 0 quand le graphe est vide. |
| `graph:FindPath(start, goal, goalX, goalY, goalZ)  -> path` | Exécute A* du nœud `start` au nœud `goal` ; renvoie un chemin se terminant en (goalX, goalY, goalZ), ou nil s'il est inaccessible. |
| `graph:NodeCount()  -> number` | Nombre de nœuds. |
| `graph:Clear()  -> nil` | Retire tous les nœuds et toutes les arêtes. |

### Handle de chemin de navigation

<span class="badge-exp">Expérimental</span>

Les indices de points de passage commencent à 1.

| Méthode | Description |
| --- | --- |
| `path:Count()  -> number` | Nombre de points de passage. |
| `path:X(i)  -> number` | X du point de passage `i`. |
| `path:Y(i)  -> number` | Y du point de passage `i`. |
| `path:Z(i)  -> number` | Z du point de passage `i`. |
