<!-- api/3d-raytrace.md -->

# 3D 엔진: 레이트레이서 월드 <span class="badge-exp">실험적</span>

패스 트레이서는 래스터라이저와 같은 씬 데이터를 재질, 해석적 프리미티브, 하늘 그라데이션으로 렌더링합니다. 씬은 `scene:SetMode("raytrace")`로 전환합니다. 씬, 오브젝트, 텍스처와 공통 규약은 [3D 엔진: 래스터라이저 월드](3d.md)에서 설명합니다.

## 패스 트레이서가 사용하는 씬 호출

| 메서드 | 설명 |
| --- | --- |
| `scene:GetRaytracerStatus()  -> string` | raytrace 모드의 백엔드 상태(GPU 컴퓨트 타이밍, CPU 대체, 또는 CPU 패스 트레이서). 래스터 모드에서는 빈 문자열. |
| `scene:RenderViewRT(texId, cx, cy, cz, yawDeg, pitchDeg, fovDeg, clr, clg, clb, flipX)  -> nil` | RenderView와 같지만 CPU 패스 트레이서가 반사 해상도의 1/4로 렌더링합니다. |

## 패스 트레이서 재질과 프리미티브

재질, 해석적 프리미티브, 하늘 그라디언트는 패스 트레이서만 사용합니다. 패스 트레이서는 유지되는 객체도 ObjSetMaterial로 지정한 재질을, 없으면 텍스처와 플랫 색을 사용해 트레이싱합니다. 재질과 프리미티브 id는 0부터 시작합니다.

| 메서드 | 설명 |
| --- | --- |
| `scene:SetSky(tr, tg, tb, br, bg, bb, strength)  -> nil` | 위 색에서 아래 색으로의 하늘 그라디언트(선형 0-1)에 `strength`를 곱합니다. 광선이 벗어날 때 씬을 비춥니다. 강도 0은 검은 스튜디오가 됩니다. |
| `scene:NewMaterial()  -> id` | 재질(기본값 디퓨즈 중간 회색)을 만들고 id를 반환합니다. |
| `scene:MatSetType(id, type)  -> nil` | `"diffuse"`, `"metal"`, `"glass"`, `"emissive"`. |
| `scene:MatSetAlbedo(id, r, g, b)  -> nil` | 기본 색(선형 0-1). 금속에서는 반사에 틴트를 줍니다. |
| `scene:MatSetRoughness(id, rough)  -> nil` | 0 = 선명한 거울 또는 투명 유리, 1 = 완전히 거침. |
| `scene:MatSetIOR(id, ior)  -> nil` | 유리의 굴절률(최소 1). |
| `scene:MatSetEmission(id, r, g, b, strength)  -> nil` | 발광 색 곱하기 강도. 발광 표면은 씬을 비춥니다. |
| `scene:MatSetTexture(id, texId)  -> nil` | UV로 샘플링되는 알베도 텍스처(등록된 텍스처 id). -1 = 없음. |
| `scene:MatSetNormalMap(id, preset)  -> nil` | 절차적 노멀 맵: `"none"`, `"wood"`, `"perlin"`, `"waves"`. |
| `scene:MatSetNormalMapTexture(id, texId)  -> nil` | 텍스처 지오메트리용 탄젠트 공간 노멀 맵 텍스처(RGB 인코딩 법선). 프리셋을 덮어씁니다. -1은 해제합니다. |
| `scene:AddSphere(cx, cy, cz, r, mat)  -> id` | 구 프리미티브. |
| `scene:AddPlane(px, py, pz, nx, ny, nz, mat)  -> id` | 한 점을 지나고 지정한 법선(자동 정규화)을 가진 무한 평면. |
| `scene:AddBox(minx, miny, minz, maxx, maxy, maxz, mat)  -> id` | 축 정렬 상자 프리미티브. |
| `scene:AddTorus(cx, cy, cz, R, r, axis, mat)  -> id` | 주 반지름 `R`, 부 반지름 `r`, 축 0 = X, 1 = Y, 2 = Z인 토러스. |
| `scene:AddSDF(preset, cx, cy, cz, sx, sy, sz, mat)  -> id` | 축별로 스케일된 레이 마칭 부호 거리 형태: `"sphere"`, `"roundbox"`, `"torus"`, `"capsule"`, `"gyroid"`, `"octahedron"`, `"gem"`, `"diamond"`. |
| `scene:ClearPrimitives()  -> nil` | 모든 프리미티브를 제거합니다. |
| `scene:ClearRaytraceModel()  -> nil` | 모든 재질, 점광원, 프리미티브를 제거합니다. |
