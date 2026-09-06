<!-- api/3d-raytrace.md -->

# 3D-Engine: Raytracer-Welt <span class="badge-exp">Experimentell</span>

Der Pathtracer rendert dieselben Szenendaten wie der Rasterizer, mit Materialien, analytischen Primitiven und einem Himmelsverlauf. Schalten Sie eine Szene mit `scene:SetMode("raytrace")` um; [3D-Engine: Rasterizer-Welt](3d.md) beschreibt Szenen, Objekte, Texturen und die gemeinsamen Konventionen.

## Vom Pathtracer genutzte Szenenaufrufe

| Methode | Beschreibung |
| --- | --- |
| `scene:GetRaytracerStatus()  -> string` | Backend-Anzeige für den Raytrace-Modus (GPU-Compute-Timing, CPU-Rückfall oder CPU-Pathtracer); leer in Raster-Modi. |
| `scene:RenderViewRT(texId, cx, cy, cz, yawDeg, pitchDeg, fovDeg, clr, clg, clb, flipX)  -> nil` | Wie RenderView, aber der CPU-Pathtracer rendert es bei einem Viertel der Reflexionsauflösung. |

## Pathtracer-Materialien und -Primitive

Nur der Pathtracer verwendet Materialien, analytische Primitive und den Himmelsverlauf. Der Pathtracer traced auch persistente Objekte, mit dem über ObjSetMaterial zugewiesenen Material oder, wenn keines gesetzt ist, mit ihrer Textur und flachen Farbe. Material- und Primitiv-IDs beginnen bei 0.

| Methode | Beschreibung |
| --- | --- |
| `scene:SetSky(tr, tg, tb, br, bg, bb, strength)  -> nil` | Himmelsverlauf (linear 0-1) von der oberen zur unteren Farbe, skaliert mit `strength`; er beleuchtet die Szene, wenn Strahlen entkommen. Stärke 0 ergibt ein schwarzes Studio. |
| `scene:NewMaterial()  -> id` | Erzeugt ein Material (Standard diffuses Mittelgrau) und gibt seine ID zurück. |
| `scene:MatSetType(id, type)  -> nil` | `"diffuse"`, `"metal"`, `"glass"` oder `"emissive"`. |
| `scene:MatSetAlbedo(id, r, g, b)  -> nil` | Grundfarbe (linear 0-1); färbt bei Metall die Reflexion. |
| `scene:MatSetRoughness(id, rough)  -> nil` | 0 = scharfer Spiegel oder klares Glas, 1 = vollständig rau. |
| `scene:MatSetIOR(id, ior)  -> nil` | Brechungsindex für Glas (Minimum 1). |
| `scene:MatSetEmission(id, r, g, b, strength)  -> nil` | Emissionsfarbe mal Stärke; emissive Flächen beleuchten die Szene. |
| `scene:MatSetTexture(id, texId)  -> nil` | Albedo-Textur (eine registrierte Textur-ID), per UV abgetastet; -1 = keine. |
| `scene:MatSetNormalMap(id, preset)  -> nil` | Prozedurale Normal-Map: `"none"`, `"wood"`, `"perlin"` oder `"waves"`. |
| `scene:MatSetNormalMapTexture(id, texId)  -> nil` | Normal-Map-Textur im Tangentenraum (RGB-kodierte Normale) für texturierte Geometrie; überschreibt das Preset. -1 hebt auf. |
| `scene:AddSphere(cx, cy, cz, r, mat)  -> id` | Kugel-Primitiv. |
| `scene:AddPlane(px, py, pz, nx, ny, nz, mat)  -> id` | Unendliche Ebene durch einen Punkt mit der angegebenen Normalen (wird automatisch normalisiert). |
| `scene:AddBox(minx, miny, minz, maxx, maxy, maxz, mat)  -> id` | Achsenparalleles Quader-Primitiv. |
| `scene:AddTorus(cx, cy, cz, R, r, axis, mat)  -> id` | Torus mit Hauptradius `R`, Nebenradius `r`, Achse 0 = X, 1 = Y, 2 = Z. |
| `scene:AddSDF(preset, cx, cy, cz, sx, sy, sz, mat)  -> id` | Per Raymarching gerenderte Signed-Distance-Form, pro Achse skaliert: `"sphere"`, `"roundbox"`, `"torus"`, `"capsule"`, `"gyroid"`, `"octahedron"`, `"gem"` oder `"diamond"`. |
| `scene:ClearPrimitives()  -> nil` | Entfernt alle Primitive. |
| `scene:ClearRaytraceModel()  -> nil` | Entfernt alle Materialien, Punktlichter und Primitive. |
