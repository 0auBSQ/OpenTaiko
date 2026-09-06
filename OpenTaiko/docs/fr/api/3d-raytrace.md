<!-- api/3d-raytrace.md -->

# Moteur 3D : monde du path tracer <span class="badge-exp">Expérimental</span>

Le path tracer rend les mêmes données de scène que le rastériseur, avec des matériaux, des primitives analytiques et un dégradé de ciel. Basculez une scène avec `scene:SetMode("raytrace")` ; [Moteur 3D : monde du rastériseur](3d.md) décrit les scènes, les objets, les textures et les conventions communes.

## Appels de scène utilisés par le path tracer

| Méthode | Description |
| --- | --- |
| `scene:GetRaytracerStatus()  -> string` | Relevé du back-end en mode raytrace (chronométrage du calcul GPU, repli CPU, ou path tracer CPU) ; vide en modes raster. |
| `scene:RenderViewRT(texId, cx, cy, cz, yawDeg, pitchDeg, fovDeg, clr, clg, clb, flipX)  -> nil` | Comme RenderView, mais le path tracer CPU le rend au quart de la résolution des reflets. |

## Matériaux et primitives du path tracer

Seul le path tracer utilise les matériaux, les primitives analytiques et le dégradé de ciel. Le path tracer trace aussi les objets retenus, avec le matériau affecté par ObjSetMaterial, ou leur texture et leur couleur unie quand aucun n'est défini. Les identifiants de matériau et de primitive commencent à 0.

| Méthode | Description |
| --- | --- |
| `scene:SetSky(tr, tg, tb, br, bg, bb, strength)  -> nil` | Dégradé de ciel (linéaire 0-1) de la couleur du haut à la couleur du bas, multiplié par `strength` ; il éclaire la scène quand les rayons s'échappent. Une intensité de 0 donne un studio noir. |
| `scene:NewMaterial()  -> id` | Crée un matériau (diffus gris moyen par défaut) et renvoie son identifiant. |
| `scene:MatSetType(id, type)  -> nil` | `"diffuse"`, `"metal"`, `"glass"` ou `"emissive"`. |
| `scene:MatSetAlbedo(id, r, g, b)  -> nil` | Couleur de base (linéaire 0-1) ; teinte le reflet pour le métal. |
| `scene:MatSetRoughness(id, rough)  -> nil` | 0 = miroir net ou verre clair, 1 = entièrement rugueux. |
| `scene:MatSetIOR(id, ior)  -> nil` | Indice de réfraction pour le verre (minimum 1). |
| `scene:MatSetEmission(id, r, g, b, strength)  -> nil` | Couleur d'émission multipliée par l'intensité ; les surfaces émissives éclairent la scène. |
| `scene:MatSetTexture(id, texId)  -> nil` | Texture d'albédo (un identifiant de texture enregistrée) échantillonnée par UV ; -1 = aucune. |
| `scene:MatSetNormalMap(id, preset)  -> nil` | Carte de normales procédurale : `"none"`, `"wood"`, `"perlin"` ou `"waves"`. |
| `scene:MatSetNormalMapTexture(id, texId)  -> nil` | Texture de carte de normales en espace tangent (normale encodée en RGB) pour la géométrie texturée ; remplace le préréglage. -1 efface. |
| `scene:AddSphere(cx, cy, cz, r, mat)  -> id` | Primitive sphère. |
| `scene:AddPlane(px, py, pz, nx, ny, nz, mat)  -> id` | Plan infini passant par un point avec la normale donnée (normalisée automatiquement). |
| `scene:AddBox(minx, miny, minz, maxx, maxy, maxz, mat)  -> id` | Primitive boîte alignée sur les axes. |
| `scene:AddTorus(cx, cy, cz, R, r, axis, mat)  -> id` | Tore de grand rayon `R`, petit rayon `r`, axe 0 = X, 1 = Y, 2 = Z. |
| `scene:AddSDF(preset, cx, cy, cz, sx, sy, sz, mat)  -> id` | Forme à distance signée par ray marching, mise à l'échelle par axe : `"sphere"`, `"roundbox"`, `"torus"`, `"capsule"`, `"gyroid"`, `"octahedron"`, `"gem"` ou `"diamond"`. |
| `scene:ClearPrimitives()  -> nil` | Retire toutes les primitives. |
| `scene:ClearRaytraceModel()  -> nil` | Retire tous les matériaux, lumières ponctuelles et primitives. |
