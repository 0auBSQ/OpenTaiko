<!-- api/3d-raytrace.md -->

# 3D-engine: raytracerwereld <span class="badge-exp">Experimenteel</span>

De path tracer rendert dezelfde scènedata als de rasterizer, met materialen, analytische primitieven en een luchtgradiënt. Schakel een scène ernaar over met `scene:SetMode("raytrace")`; [3D-engine: rasterizerwereld](3d.md) beschrijft scènes, objecten, texturen en de gedeelde conventies.

## Scène-aanroepen die de path tracer gebruikt

| Methode | Beschrijving |
| --- | --- |
| `scene:GetRaytracerStatus()  -> string` | Backend-uitlezing voor de raytrace-modus (GPU-compute-timing, CPU-terugval, of CPU-path-tracer); leeg in rastermodi. |
| `scene:RenderViewRT(texId, cx, cy, cz, yawDeg, pitchDeg, fovDeg, clr, clg, clb, flipX)  -> nil` | Zoals RenderView, maar de CPU-path-tracer rendert het op een kwart van de reflectieresolutie. |

## Path-tracer-materialen en -primitieven

Alleen de path tracer gebruikt materialen, analytische primitieven en het luchtverloop. De path tracer tracet ook bewaarde objecten, met het materiaal dat via ObjSetMaterial is toegewezen, of hun textuur en vlakke kleur wanneer er geen is ingesteld. Materiaal- en primitief-id's beginnen bij 0.

| Methode | Beschrijving |
| --- | --- |
| `scene:SetSky(tr, tg, tb, br, bg, bb, strength)  -> nil` | Luchtverloop (lineair 0-1) van de bovenkleur naar de onderkleur, geschaald met `strength`; het belicht de scène wanneer stralen ontsnappen. Sterkte 0 geeft een zwarte studio. |
| `scene:NewMaterial()  -> id` | Maakt een materiaal (standaard diffuus middengrijs) en geeft zijn id terug. |
| `scene:MatSetType(id, type)  -> nil` | `"diffuse"`, `"metal"`, `"glass"` of `"emissive"`. |
| `scene:MatSetAlbedo(id, r, g, b)  -> nil` | Basiskleur (lineair 0-1); tint de reflectie bij metaal. |
| `scene:MatSetRoughness(id, rough)  -> nil` | 0 = scherpe spiegel of helder glas, 1 = volledig ruw. |
| `scene:MatSetIOR(id, ior)  -> nil` | Brekingsindex voor glas (minimaal 1). |
| `scene:MatSetEmission(id, r, g, b, strength)  -> nil` | Emissiekleur maal sterkte; emissieve oppervlakken belichten de scène. |
| `scene:MatSetTexture(id, texId)  -> nil` | Albedotextuur (een geregistreerd textuur-id), gesampled via UV; -1 = geen. |
| `scene:MatSetNormalMap(id, preset)  -> nil` | Procedurele normalmap: `"none"`, `"wood"`, `"perlin"` of `"waves"`. |
| `scene:MatSetNormalMapTexture(id, texId)  -> nil` | Normalmaptextuur in tangentruimte (RGB-gecodeerde normaal) voor getextureerde geometrie; overschrijft de preset. -1 wist. |
| `scene:AddSphere(cx, cy, cz, r, mat)  -> id` | Bolprimitief. |
| `scene:AddPlane(px, py, pz, nx, ny, nz, mat)  -> id` | Oneindig vlak door een punt met de gegeven normaal (automatisch genormaliseerd). |
| `scene:AddBox(minx, miny, minz, maxx, maxy, maxz, mat)  -> id` | As-uitgelijnde doosprimitief. |
| `scene:AddTorus(cx, cy, cz, R, r, axis, mat)  -> id` | Torus met grote straal `R`, kleine straal `r`, as 0 = X, 1 = Y, 2 = Z. |
| `scene:AddSDF(preset, cx, cy, cz, sx, sy, sz, mat)  -> id` | Ray-marched signed-distance-vorm, per as geschaald: `"sphere"`, `"roundbox"`, `"torus"`, `"capsule"`, `"gyroid"`, `"octahedron"`, `"gem"` of `"diamond"`. |
| `scene:ClearPrimitives()  -> nil` | Verwijdert alle primitieven. |
| `scene:ClearRaytraceModel()  -> nil` | Verwijdert alle materialen, puntlichten en primitieven. |
