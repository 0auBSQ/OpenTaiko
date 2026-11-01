<!-- api/3d-physics.md -->

# Motor 3D: físicas <span class="badge-exp">Experimental</span>

Cuerpos rígidos, personajes y vehículos, colisión estática, consultas de colliders con raycasts y grafos de navegación. Las posiciones son unidades de mundo y +Y es arriba; [Motor 3D: mundo del rasterizador](3d.md) describe las convenciones compartidas.

## Físicas

### PHYSICS

<span class="badge-exp">Experimental</span>

Fábrica de mundos físicos: una sopa de triángulos estática más cuerpos dinámicos de personaje, rígidos, de caja y de vehículo.

| Método | Descripción |
| --- | --- |
| `PHYSICS:NewWorld()  -> world` | Crea un mundo físico y devuelve su handle. |

### Handle de mundo físico

<span class="badge-exp">Experimental</span>

<div class="callout warn">
Construye la geometría estática entre BeginStatic y EndStatic, crea cuerpos, establece su velocidad cada fotograma y llama a Step. Los cuerpos colisionan con la sopa estática mediante collide-and-slide (esfera o caja orientada contra triángulos) y entre sí como esferas o cápsulas en el plano horizontal, intercambiando cantidad de movimiento según la masa.
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

-- cada fotograma
body:SetVelocity(moveX, body:GetVy(), moveZ)
world:Step(dt)
local x, y, z = body:GetX(), body:GetY(), body:GetZ()
```

| Método | Descripción |
| --- | --- |
| `world:SetGravity(x, y, z)  -> nil` | Vector de gravedad (0, -32, 0 por defecto). |
| `world:SetFloorMaxAngleY(y)  -> nil` | Umbral de la Y de la normal de contacto (0.5 por defecto): los contactos por encima cuentan como suelo, el resto como pared. |
| `world:BeginStatic()  -> nil` | Empieza a reconstruir la sopa estática; borra los triángulos y grupos de collider existentes. |
| `world:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | Añade un triángulo estático. Su normal es (b - a) x (c - a); el mundo omite los triángulos degenerados. |
| `world:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | Añade un quad estático como dos triángulos (a, b, c) y (a, c, d). |
| `world:AddMesh(mc)  -> nil` | Añade los triángulos de un MeshCollider a la sopa. |
| `world:BeginGroup(id)  -> nil` | Etiqueta los triángulos añadidos a partir de ahora con un grupo de collider (0 = por defecto) para que SetGroupEnabled pueda activar o desactivar el grupo en tiempo de ejecución. |
| `world:EndStatic()  -> nil` | Termina la sopa y construye la cuadrícula de fase amplia (broadphase). |
| `world:SetGroupEnabled(id, on)  -> nil` | Hace que un grupo de collider sea sólido o atravesable sin reconstruir la sopa (puertas, interiores cargados por streaming). |
| `world:ClearStatic()  -> nil` | Libera ahora la sopa estática y su almacenamiento (descarga de mapa); también reinicia los grupos de collider. |
| `world:NewCharacter(radius)  -> body` | Cuerpo esférico cinemático: tú estableces su velocidad, el mundo lo mueve con collide-and-slide. |
| `world:NewRigid(radius, mass)  -> body` | Cuerpo esférico con gravedad activada que intercambia cantidad de movimiento con otros cuerpos. |
| `world:NewBox(hx, hy, hz, mass)  -> body` | Cuerpo con gravedad activada que colisiona con la sopa estática como una caja orientada con las semiextensiones indicadas (oriéntalo con SetForward). |
| `world:NewVehicle(radius, mass)  -> vehicle` | Cuerpo de tipo coche: un chasis de personaje con gravedad, ajuste al suelo y un umbral de suelo más pronunciado, más ruedas por raycast. |
| `world:NewMeshCollider()  -> mc` | Crea un MeshCollider vacío. |
| `world:RemoveBody(body)  -> nil` | Elimina un cuerpo del mundo. |
| `world:Step(dt)  -> nil` | Avanza la simulación `dt` segundos. El mundo subdivide en pasos los cuerpos rápidos para que no atraviesen paredes finas. |
| `world:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> hit, dist, nx, ny, nz` | Lanza un rayo (dirección unitaria) contra los triángulos estáticos activados; devuelve si el rayo golpeó algo, la distancia del impacto más cercano y la normal de ese triángulo. |
| `world:GroundYAt(x, startY, z, reach)  -> number` | Altura del suelo justo debajo de (x, startY, z) dentro de `reach`, o NaN cuando no hay ninguno (compruébalo con `y ~= y`). |
| `world:StaticTriCount()  -> number` | Número de triángulos en la sopa estática. |
| `world:StaticTriCapacity()  -> number` | Capacidad asignada de la lista de triángulos. |

### Handle de cuerpo físico

<span class="badge-exp">Experimental</span>

<div class="callout warn">
NewCharacter, NewRigid y NewBox devuelven este handle. Las posiciones se refieren al centro del cuerpo. El mundo resuelve los contactos cuerpo contra cuerpo solo en el plano horizontal y los omite entre cuerpos separados más de 2 unidades en Y.
</div>

| Método | Descripción |
| --- | --- |
| `body:SetPos(x, y, z)  -> nil` | Establece la posición del centro. |
| `body:SetVelocity(x, y, z)  -> nil` | Establece la velocidad. |
| `body:AddVelocity(x, y, z)  -> nil` | Suma a la velocidad. |
| `body:SetRadius(r)  -> nil` | Radio de esfera y de fase amplia (mínimo 0.01). |
| `body:SetCapsule(halfLen)  -> nil` | Usa una cápsula horizontal de esta semilongitud a lo largo del eje hacia delante para los contactos cuerpo contra cuerpo; la colisión estática sigue usando el radio de la esfera. |
| `body:SetBox(hx, hy, hz)  -> nil` | Colisiona con la sopa estática como una caja orientada con semiextensiones (derecha, arriba, delante). |
| `body:SetForward(fx, fz)  -> nil` | Eje horizontal hacia delante (normalizado automáticamente) que orienta la cápsula o la caja. |
| `body:SetGravityEnabled(g)  -> nil` | Aplica la gravedad del mundo a este cuerpo. |
| `body:SetEnabled(e)  -> nil` | Step omite los cuerpos desactivados. |
| `body:SetSnap(s)  -> nil` | Mantiene un cuerpo en el suelo pegado a un terreno descendente dentro de un pequeño hueco; sin snap, un cuerpo que corre cuesta abajo abandona el suelo y salta. |
| `body:SetSmoothContacts(on)  -> nil` | Variante del solucionador de personaje: resuelve el contacto más profundo por iteración, suelda las juntas del suelo, registra las normales de pared y baja escalones hasta terreno transitable. Desactivado por defecto. |
| `body:SetCollisionLayer(l)  -> nil` | Índice de capa de colisión 0..30 para los contactos cuerpo contra cuerpo (0 por defecto). |
| `body:SetCollisionMask(m)  -> nil` | Máscara de bits de las capas con las que colisiona este cuerpo (bit i = capa i); todas por defecto, 0 = ninguna. |
| `body:GetPos()  -> x, y, z` | Posición del centro. |
| `body:GetVelocity()  -> x, y, z` | Velocidad. |
| `body:GetX()  -> number` | X del centro. |
| `body:GetY()  -> number` | Y del centro. |
| `body:GetZ()  -> number` | Z del centro. |
| `body:GetVx()  -> number` | Velocidad X. |
| `body:GetVy()  -> number` | Velocidad Y. |
| `body:GetVz()  -> number` | Velocidad Z. |
| `body:Speed()  -> number` | Magnitud de la velocidad. |
| `body:IsOnFloor()  -> boolean` | Verdadero cuando el cuerpo descansó sobre el suelo durante el último paso. |
| `body:HitWall()  -> boolean` | Verdadero cuando el cuerpo tocó una pared durante el último paso. |
| `body:GetWallNx()  -> number` | X de la última normal de contacto con pared (se establece cuando SetSmoothContacts está activado). |
| `body:GetWallNz()  -> number` | Z de la última normal de contacto con pared (se establece cuando SetSmoothContacts está activado). |
| `body:GetImpulseX()  -> number` | X del cambio neto de velocidad causado por los contactos cuerpo contra cuerpo durante el último paso. |
| `body:GetImpulseZ()  -> number` | Z del cambio neto de velocidad causado por los contactos cuerpo contra cuerpo durante el último paso. |

### Handle de cuerpo de vehículo

<span class="badge-exp">Experimental</span>

<div class="callout warn">
NewVehicle devuelve este handle. El chasis es un cuerpo de personaje que Step mueve como de costumbre; cada llamada a UpdateWheels lanza las ruedas como rayos hacia abajo desde el chasis, lo que da un indicador de contacto con el suelo multirrueda, la compresión de suspensión por rueda y un cabeceo y alabeo del chasis que siguen la pendiente. Tu código de conducción establece la velocidad del chasis.
</div>

| Método | Descripción |
| --- | --- |
| `vehicle:AddWheel(lx, lz, radius, rest, steered, powered)  -> wheel` | Añade una rueda en el desplazamiento en el espacio del chasis (`lx` derecha, `lz` delante) con el radio y la longitud de reposo de suspensión indicados. El vehículo guarda `steered` y `powered` para tu código de conducción y nunca los lee por sí mismo. |
| `vehicle:GetWheel(i)  -> wheel` | Rueda `i` (desde 0), o nil. |
| `vehicle:WheelCount()  -> number` | Número de ruedas. |
| `vehicle:UpdateWheels(dt, speed)  -> nil` | Lanza los rayos de las ruedas y actualiza el contacto con el suelo, la compresión, el cabeceo, el alabeo y el giro de las ruedas (`speed` en unidades del mundo por segundo). Llámalo una vez por fotograma después de Step. |
| `vehicle.Body  -> body` | El handle del cuerpo del chasis. |
| `vehicle:SetPos(x, y, z)  -> nil` | Establece la posición del chasis. |
| `vehicle:SetVelocity(x, y, z)  -> nil` | Establece la velocidad del chasis. |
| `vehicle:AddVelocity(x, y, z)  -> nil` | Suma a la velocidad del chasis. |
| `vehicle:SetForward(fx, fz)  -> nil` | Establece el eje hacia delante y deriva el rumbo. |
| `vehicle:SetYaw(y)  -> nil` | Establece el rumbo en radianes y deriva el eje hacia delante. |
| `vehicle:SetGravityEnabled(g)  -> nil` | Aplica la gravedad al chasis. |
| `vehicle:SetEnabled(e)  -> nil` | Activa o desactiva el cuerpo del chasis. |
| `vehicle:SetCapsule(halfLen)  -> nil` | Semilongitud de la cápsula para los contactos chasis contra cuerpo. |
| `vehicle:SetRadius(r)  -> nil` | Radio del chasis. |
| `vehicle:GetX()  -> number` | X del chasis. |
| `vehicle:GetY()  -> number` | Y del chasis. |
| `vehicle:GetZ()  -> number` | Z del chasis. |
| `vehicle:GetVx()  -> number` | Velocidad X del chasis. |
| `vehicle:GetVy()  -> number` | Velocidad Y del chasis. |
| `vehicle:GetVz()  -> number` | Velocidad Z del chasis. |
| `vehicle:IsOnFloor()  -> boolean` | Verdadero cuando al menos tres ruedas tocan el suelo o el chasis descansa sobre el suelo. |
| `vehicle:HitWall()  -> boolean` | Verdadero cuando el chasis tocó una pared durante el último paso. |
| `vehicle:GetImpulseX()  -> number` | X del cambio de velocidad cuerpo contra cuerpo aplicado al chasis. |
| `vehicle:GetImpulseZ()  -> number` | Z del cambio de velocidad cuerpo contra cuerpo aplicado al chasis. |
| `vehicle:GetPitch()  -> number` | Cabeceo del chasis en radianes, suavizado hacia los contactos de las ruedas. |
| `vehicle:GetRoll()  -> number` | Alabeo del chasis en radianes, suavizado hacia los contactos de las ruedas. |

### Handle de rueda

<span class="badge-exp">Experimental</span>

AddWheel y GetWheel devuelven este handle; léelo después de UpdateWheels.

| Método | Descripción |
| --- | --- |
| `wheel:IsGrounded()  -> boolean` | Verdadero cuando la rueda toca el suelo. |
| `wheel:GetCompression()  -> number` | Compresión de la suspensión de 0 (totalmente extendida o en el aire) a 1 (totalmente comprimida). |
| `wheel:GetSpin()  -> number` | Ángulo de rodadura acumulado en radianes para los visuales de la rueda. |

### Handle de MeshCollider

<span class="badge-exp">Experimental</span>

Una lista de triángulos para la sopa estática de un mundo; rellénala a mano o con model:BuildCollider.

| Método | Descripción |
| --- | --- |
| `mc:AddTri(ax, ay, az, bx, by, bz, cx, cy, cz)  -> nil` | Añade un triángulo. |
| `mc:AddQuad(ax, ay, az, bx, by, bz, cx, cy, cz, dx, dy, dz)  -> nil` | Añade un quad como dos triángulos. |
| `mc:TriCount()  -> number` | Número de triángulos recogidos. |
| `mc:Clear()  -> nil` | Elimina todos los triángulos. |
| `mc:AddToWorld(world)  -> nil` | Añade los triángulos a la sopa estática de un mundo (entre BeginStatic y EndStatic); igual que world:AddMesh(mc). |

## Colliders y raycasting

### COLLIDERS

<span class="badge-exp">Experimental</span>

Fábrica de formas de consulta (esfera, caja alineada con los ejes, cápsula) y rayos para pruebas de impacto, independientes de cualquier mundo físico.

| Método | Descripción |
| --- | --- |
| `COLLIDERS:Sphere(cx, cy, cz, r)  -> collider` | Esfera de radio `r` centrada en (cx, cy, cz). |
| `COLLIDERS:Box(cx, cy, cz, hx, hy, hz)  -> collider` | Caja alineada con los ejes con semiextensiones (hx, hy, hz) centrada en (cx, cy, cz). |
| `COLLIDERS:Capsule(cx, cy, cz, hsx, hsy, hsz, r)  -> collider` | Cápsula de radio `r` alrededor del segmento de centro - (hsx, hsy, hsz) a centro + (hsx, hsy, hsz). |
| `COLLIDERS:CapsuleY(cx, cy, cz, halfHeight, r)  -> collider` | Cápsula vertical con la semialtura y el radio indicados. |
| `COLLIDERS:Ray(ox, oy, oz, dx, dy, dz, len)  -> ray` | Rayo reutilizable con un origen, una dirección (normalizada automáticamente) y una longitud. |

### Handle de collider

<span class="badge-exp">Experimental</span>

| Método | Descripción |
| --- | --- |
| `collider:SetCenter(x, y, z)  -> nil` | Mueve la forma a un nuevo centro. |
| `collider:SetRadius(r)  -> nil` | Establece el radio (solo colliders de esfera). |
| `collider:SetHalfExtents(hx, hy, hz)  -> nil` | Establece las semiextensiones (solo colliders de caja). |
| `collider:Raycast(ox, oy, oz, dx, dy, dz, maxDist)  -> number` | Distancia a lo largo del rayo (dirección normalizada automáticamente) hasta el primer impacto dentro de `maxDist`, o -1 si no hay impacto. |
| `collider:HitsRay(ray)  -> boolean` | Verdadero cuando el rayo golpea la forma dentro de su longitud. |
| `collider:CollideWith(other)  -> boolean` | Verdadero cuando la forma se solapa con otro collider, o cuando `other` es un rayo que la golpea. |
| `collider:SetTag(tag)  -> nil` | Adjunta cualquier valor Lua para que puedas asociar un impacto a tu propio objeto. |
| `collider:GetTag()  -> any` | El valor adjunto. |

### Handle de rayo

<span class="badge-exp">Experimental</span>

| Método | Descripción |
| --- | --- |
| `ray:Set(ox, oy, oz, dx, dy, dz, len)  -> nil` | Reinicia el origen, la dirección (normalizada automáticamente) y la longitud. |

## Búsqueda de rutas

### PATHFIND

<span class="badge-exp">Experimental</span>

Fábrica de grafos de navegación ponderados que FindPath busca con A*.

| Método | Descripción |
| --- | --- |
| `PATHFIND:NewGraph()  -> graph` | Crea un grafo vacío y devuelve su handle. |

### Handle de grafo de navegación

<span class="badge-exp">Experimental</span>

<div class="callout warn">
Los nodos llevan una posición en el mundo; los índices empiezan en 1. Cada arista lleva un peso y un punto de tránsito, la posición por la que pasa un móvil al tomar esa arista (por ejemplo el punto medio de una puerta). FindPath devuelve los puntos de tránsito de las aristas que tomó, seguidos del punto de destino.
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

| Método | Descripción |
| --- | --- |
| `graph:AddNode(x, y, z)  -> number` | Añade un nodo y devuelve su índice. |
| `graph:AddEdge(a, b, w, px, py, pz)  -> nil` | Añade una arista dirigida del nodo `a` al nodo `b` con peso `w` a través del punto de tránsito (px, py, pz). |
| `graph:Link(a, b, w, px, py, pz)  -> nil` | Añade aristas en ambas direcciones a través de un único punto de tránsito compartido. |
| `graph:NearestNode(x, y, z)  -> number` | Índice del nodo más cercano a (x, y, z), o 0 cuando el grafo está vacío. |
| `graph:FindPath(start, goal, goalX, goalY, goalZ)  -> path` | Ejecuta A* del nodo `start` al nodo `goal`; devuelve una ruta que termina en (goalX, goalY, goalZ), o nil cuando es inalcanzable. |
| `graph:NodeCount()  -> number` | Número de nodos. |
| `graph:Clear()  -> nil` | Elimina todos los nodos y aristas. |

### Handle de ruta de navegación

<span class="badge-exp">Experimental</span>

Los índices de los puntos de ruta empiezan en 1.

| Método | Descripción |
| --- | --- |
| `path:Count()  -> number` | Número de puntos de ruta. |
| `path:X(i)  -> number` | X del punto de ruta `i`. |
| `path:Y(i)  -> number` | Y del punto de ruta `i`. |
| `path:Z(i)  -> number` | Z del punto de ruta `i`. |
