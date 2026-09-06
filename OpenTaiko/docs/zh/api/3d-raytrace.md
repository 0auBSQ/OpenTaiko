<!-- api/3d-raytrace.md -->

# 3D 引擎：光线追踪世界 <span class="badge-exp">实验性</span>

路径追踪器渲染与光栅化器相同的场景数据，并使用材质、解析式图元和天空渐变。用 `scene:SetMode("raytrace")` 切换场景；[3D 引擎：光栅化世界](3d.md)描述了场景、对象、纹理和共用约定。

## 路径追踪器使用的场景调用

| 方法 | 说明 |
| --- | --- |
| `scene:GetRaytracerStatus()  -> string` | 光线追踪模式的后端读数（GPU 计算耗时、CPU 回退，或 CPU 路径追踪器）；光栅模式下为空。 |
| `scene:RenderViewRT(texId, cx, cy, cz, yawDeg, pitchDeg, fovDeg, clr, clg, clb, flipX)  -> nil` | 与 RenderView 类似，但由 CPU 路径追踪器以反射分辨率的四分之一渲染。 |

## 路径追踪器材质与图元

只有路径追踪器使用材质、解析图元和天空渐变。路径追踪器同样会追踪保留对象，使用 ObjSetMaterial 指定的材质，未指定时使用其纹理和纯色。材质和图元 id 从 0 起。

| 方法 | 说明 |
| --- | --- |
| `scene:SetSky(tr, tg, tb, br, bg, bb, strength)  -> nil` | 从顶部颜色到底部颜色的天空渐变（线性 0-1），乘以 `strength`；光线逃逸时它照亮场景。强度 0 得到黑色摄影棚。 |
| `scene:NewMaterial()  -> id` | 创建一个材质（默认为漫反射中灰色）并返回其 id。 |
| `scene:MatSetType(id, type)  -> nil` | `"diffuse"`、`"metal"`、`"glass"` 或 `"emissive"`。 |
| `scene:MatSetAlbedo(id, r, g, b)  -> nil` | 基色（线性 0-1）；对金属会为反射着色。 |
| `scene:MatSetRoughness(id, rough)  -> nil` | 0 = 清晰的镜面或透明玻璃，1 = 完全粗糙。 |
| `scene:MatSetIOR(id, ior)  -> nil` | 玻璃的折射率（最小 1）。 |
| `scene:MatSetEmission(id, r, g, b, strength)  -> nil` | 发光颜色乘以强度；自发光表面会照亮场景。 |
| `scene:MatSetTexture(id, texId)  -> nil` | 按 UV 采样的反照率纹理（已注册的纹理 id）；-1 = 无。 |
| `scene:MatSetNormalMap(id, preset)  -> nil` | 程序化法线贴图：`"none"`、`"wood"`、`"perlin"` 或 `"waves"`。 |
| `scene:MatSetNormalMapTexture(id, texId)  -> nil` | 用于带纹理几何体的切线空间法线贴图纹理（RGB 编码的法线）；覆盖预设。-1 清除。 |
| `scene:AddSphere(cx, cy, cz, r, mat)  -> id` | 球体图元。 |
| `scene:AddPlane(px, py, pz, nx, ny, nz, mat)  -> id` | 经过一点、具有给定法线（自动归一化）的无限平面。 |
| `scene:AddBox(minx, miny, minz, maxx, maxy, maxz, mat)  -> id` | 轴对齐盒子图元。 |
| `scene:AddTorus(cx, cy, cz, R, r, axis, mat)  -> id` | 主半径 `R`、次半径 `r` 的圆环，axis 0 = X，1 = Y，2 = Z。 |
| `scene:AddSDF(preset, cx, cy, cz, sx, sy, sz, mat)  -> id` | 按轴缩放的光线步进有符号距离场形状：`"sphere"`、`"roundbox"`、`"torus"`、`"capsule"`、`"gyroid"`、`"octahedron"`、`"gem"` 或 `"diamond"`。 |
| `scene:ClearPrimitives()  -> nil` | 移除所有图元。 |
| `scene:ClearRaytraceModel()  -> nil` | 移除所有材质、点光源和图元。 |
