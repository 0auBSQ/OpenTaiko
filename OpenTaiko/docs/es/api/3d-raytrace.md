<!-- api/3d-raytrace.md -->

# Motor 3D: mundo del trazador de rutas <span class="badge-exp">Experimental</span>

El trazador de rutas renderiza los mismos datos de escena que el rasterizador, con materiales, primitivas analíticas y un degradado de cielo. Cambia una escena a él con `scene:SetMode("raytrace")`; [Motor 3D: mundo del rasterizador](3d.md) describe las escenas, los objetos, las texturas y las convenciones compartidas.

## Llamadas de escena usadas por el trazador de rutas

| Método | Descripción |
| --- | --- |
| `scene:GetRaytracerStatus()  -> string` | Lectura del backend en modo raytrace (tiempos de cómputo GPU, alternativa CPU, o trazador de rutas CPU); vacío en los modos raster. |

## Materiales y primitivas del trazador de rutas

Solo el trazador de rutas usa materiales, primitivas analíticas y el gradiente de cielo. El trazador de rutas también traza los objetos retenidos, usando el material asignado con ObjSetMaterial, o su textura y color plano cuando no hay ninguno establecido. Los ids de material y primitiva empiezan en 0. El trazador de rutas dibuja los sprites como quads con prueba alfa, deja pasar los rayos por los téxeles recortados de texturas y sprites, y aplica ObjSetTint, ObjSetColor y ObjSetEmissive. Como proyecta sombras reales, omite los sprites planos sobre el suelo (billboard 3) y los sprites de ObjAddSpriteQuad con cutout 0, que sustituyen sombras y calcomanías. Además de las luces puntuales, SetSun ilumina la escena como una luz direccional con sombras y SetAmbient como luz desde todas las direcciones sobre las superficies difusas; el cielo visto directamente o en un espejo sigue siendo solo el degradado.

| Método | Descripción |
| --- | --- |
| `scene:SetSky(tr, tg, tb, br, bg, bb, strength)  -> nil` | Gradiente de cielo (lineal 0-1) del color superior al color inferior, escalado por `strength`; ilumina la escena cuando los rayos escapan. Una intensidad 0 da un estudio negro. |
| `scene:NewMaterial()  -> id` | Crea un material (difuso gris medio por defecto) y devuelve su id. |
| `scene:MatSetType(id, type)  -> nil` | `"diffuse"`, `"metal"`, `"glass"` o `"emissive"`. Una malla de vidrio necesita triángulos cuyas esquinas a, b, c giren en sentido horario en pantalla vistas desde fuera, de modo que `(b - a) × (c - a)` apunte hacia fuera del vidrio: el trazador de rutas usa esta dirección para saber qué lado es el interior. |
| `scene:MatSetAlbedo(id, r, g, b)  -> nil` | Color base (lineal 0-1); tiñe el reflejo en el metal. |
| `scene:MatSetRoughness(id, rough)  -> nil` | 0 = espejo nítido o cristal transparente, 1 = completamente rugoso. |
| `scene:MatSetIOR(id, ior)  -> nil` | Índice de refracción para el cristal (mínimo 1). |
| `scene:MatSetEmission(id, r, g, b, strength)  -> nil` | Color de emisión por intensidad; las superficies emisivas iluminan la escena. No bloquean las luces puntuales, así que una luz puede estar dentro de su bombilla. |
| `scene:MatSetTexture(id, texId)  -> nil` | Textura de albedo (un id de textura registrada) muestreada por UV; -1 = ninguna. |
| `scene:MatSetNormalMap(id, preset)  -> nil` | Mapa de normales procedural: `"none"`, `"wood"`, `"perlin"` o `"waves"`. |
| `scene:MatSetNormalMapTexture(id, texId)  -> nil` | Textura de mapa de normales en espacio tangente (normal codificada en RGB) para geometría texturizada; sobrescribe el preajuste. -1 lo elimina. |
| `scene:AddSphere(cx, cy, cz, r, mat)  -> id` | Primitiva esfera. |
| `scene:AddPlane(px, py, pz, nx, ny, nz, mat)  -> id` | Plano infinito que pasa por un punto con la normal indicada (normalizada automáticamente). |
| `scene:AddBox(minx, miny, minz, maxx, maxy, maxz, mat)  -> id` | Primitiva caja alineada con los ejes. |
| `scene:AddTorus(cx, cy, cz, R, r, axis, mat)  -> id` | Toro con radio mayor `R`, radio menor `r`, eje 0 = X, 1 = Y, 2 = Z. |
| `scene:AddSDF(preset, cx, cy, cz, sx, sy, sz, mat)  -> id` | Forma de distancia con signo trazada por ray marching y escalada por eje: `"sphere"`, `"roundbox"`, `"torus"`, `"capsule"`, `"gyroid"`, `"octahedron"`, `"gem"` o `"diamond"`. |
| `scene:ClearPrimitives()  -> nil` | Elimina todas las primitivas. |
| `scene:ClearRaytraceModel()  -> nil` | Elimina todos los materiales, luces puntuales y primitivas. |
