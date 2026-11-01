<!-- api/3d-raytrace.md -->

# 3D engine: raytracer world <span class="badge-exp">Experimental</span>

The path tracer renders the same scene data as the rasterizer, with materials, analytic primitives and a sky gradient. Switch a scene to it with `scene:SetMode("raytrace")`; [3D engine: rasterizer world](3d.md) describes scenes, objects, textures and the shared conventions.

## Scene calls used by the path tracer

| Method | Description |
| --- | --- |
| `scene:GetRaytracerStatus()  -> string` | Backend readout for raytrace mode (GPU compute timing, CPU fallback, or CPU path tracer); empty in raster modes. |

## Path tracer materials and primitives

Only the path tracer uses materials, analytic primitives and the sky gradient. The path tracer also traces retained objects, using the material assigned with ObjSetMaterial, or their texture and flat colour when none is set. Material and primitive ids start at 0. The path tracer draws sprites as alpha-tested quads, lets rays through the cut-out texels of textures and sprites, and applies ObjSetTint, ObjSetColor and ObjSetEmissive. It casts real shadows, so it leaves out ground-flat sprites (billboard 3) and ObjAddSpriteQuad sprites with cutout 0, which stand in for shadows and decals. Besides the point lights, SetSun lights the scene as a directional light with shadows, and SetAmbient as light from every direction on diffuse surfaces; the sky seen directly or in a mirror stays the plain gradient.

| Method | Description |
| --- | --- |
| `scene:SetSky(tr, tg, tb, br, bg, bb, strength)  -> nil` | Sky gradient (linear 0-1) from the top colour to the bottom colour, scaled by `strength`; it lights the scene when rays escape. Strength 0 gives a black studio. |
| `scene:NewMaterial()  -> id` | Create a material (default diffuse mid-grey) and return its id. |
| `scene:MatSetType(id, type)  -> nil` | `"diffuse"`, `"metal"`, `"glass"` or `"emissive"`. A glass mesh needs triangles whose corners a, b, c run clockwise on screen when seen from outside, so that `(b - a) × (c - a)` points out of the glass: the path tracer uses this direction to tell which side is inside. |
| `scene:MatSetAlbedo(id, r, g, b)  -> nil` | Base colour (linear 0-1); tints the reflection for metal. |
| `scene:MatSetRoughness(id, rough)  -> nil` | 0 = sharp mirror or clear glass, 1 = fully rough. |
| `scene:MatSetIOR(id, ior)  -> nil` | Index of refraction for glass (minimum 1). |
| `scene:MatSetEmission(id, r, g, b, strength)  -> nil` | Emission colour times strength; emissive surfaces light the scene. They do not block point lights, so a light can sit inside its bulb. |
| `scene:MatSetTexture(id, texId)  -> nil` | Albedo texture (a registered texture id) sampled by UV; -1 = none. |
| `scene:MatSetNormalMap(id, preset)  -> nil` | Procedural normal map: `"none"`, `"wood"`, `"perlin"` or `"waves"`. |
| `scene:MatSetNormalMapTexture(id, texId)  -> nil` | Tangent-space normal-map texture (RGB-encoded normal) for textured geometry; overrides the preset. -1 clears. |
| `scene:AddSphere(cx, cy, cz, r, mat)  -> id` | Sphere primitive. |
| `scene:AddPlane(px, py, pz, nx, ny, nz, mat)  -> id` | Infinite plane through a point with the given normal (normalised automatically). |
| `scene:AddBox(minx, miny, minz, maxx, maxy, maxz, mat)  -> id` | Axis-aligned box primitive. |
| `scene:AddTorus(cx, cy, cz, R, r, axis, mat)  -> id` | Torus with major radius `R`, minor radius `r`, axis 0 = X, 1 = Y, 2 = Z. |
| `scene:AddSDF(preset, cx, cy, cz, sx, sy, sz, mat)  -> id` | Ray-marched signed-distance shape scaled per axis: `"sphere"`, `"roundbox"`, `"torus"`, `"capsule"`, `"gyroid"`, `"octahedron"`, `"gem"` or `"diamond"`. |
| `scene:ClearPrimitives()  -> nil` | Remove all primitives. |
| `scene:ClearRaytraceModel()  -> nil` | Remove all materials, point lights and primitives. |
