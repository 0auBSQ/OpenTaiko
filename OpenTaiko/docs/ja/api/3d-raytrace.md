<!-- api/3d-raytrace.md -->

# 3D エンジン: レイトレーサーの世界 <span class="badge-exp">実験的</span>

パストレーサーは、ラスタライザーと同じシーンデータを、マテリアル、解析的プリミティブ、空のグラデーションを使って描画します。シーンを `scene:SetMode("raytrace")` で切り替えます。シーン、オブジェクト、テクスチャ、共通の規約は [3D エンジン: ラスタライザーの世界](3d.md) で説明しています。

## パストレーサーが使うシーン呼び出し

| メソッド | 説明 |
| --- | --- |
| `scene:GetRaytracerStatus()  -> string` | レイトレースモードのバックエンドの状態 (GPU コンピュートのタイミング、CPU フォールバック、または CPU パストレーサー)。ラスタモードでは空。 |

## パストレーサーのマテリアルとプリミティブ

マテリアル、解析的プリミティブ、空のグラデーションを使うのはパストレーサーだけです。パストレーサーは保持されるオブジェクトもトレースし、ObjSetMaterial で割り当てられたマテリアル、または設定されていなければテクスチャと単色を使います。マテリアルとプリミティブの id は 0 から始まります。パストレーサーはスプライトをアルファテストした四角形として描き、テクスチャとスプライトの切り抜かれたテクセルでは光線を通し、ObjSetTint、ObjSetColor、ObjSetEmissive を適用します。パストレーサーは実際の影を落とすため、影やデカールの代わりとなる、地面に平らに置くスプライト (billboard 3) と cutout が 0 の ObjAddSpriteQuad スプライトは省きます。点光源に加えて、SetSun は影付きの平行光源として、SetAmbient は拡散面にあらゆる方向から届く光としてシーンを照らします。直接見える空や鏡に映る空は、グラデーションのままです。

| メソッド | 説明 |
| --- | --- |
| `scene:SetSky(tr, tg, tb, br, bg, bb, strength)  -> nil` | 上の色から下の色への空のグラデーション (リニア 0-1) を `strength` でスケールしたもの。レイが抜けたときにシーンを照らします。強さ 0 は黒いスタジオになります。 |
| `scene:NewMaterial()  -> id` | マテリアル (既定は中間グレーの拡散反射) を作成し、その id を返します。 |
| `scene:MatSetType(id, type)  -> nil` | `"diffuse"`、`"metal"`、`"glass"`、`"emissive"`。ガラスのメッシュでは、三角形の頂点 a、b、c が外から見て画面上で時計回りに並び、`(b - a) × (c - a)` がガラスの外側を向く必要があります。パストレーサーはこの向きで内側を判断します。 |
| `scene:MatSetAlbedo(id, r, g, b)  -> nil` | ベース色 (リニア 0-1)。金属では反射に色を付けます。 |
| `scene:MatSetRoughness(id, rough)  -> nil` | 0 = 鋭い鏡または透明なガラス、1 = 完全に粗い。 |
| `scene:MatSetIOR(id, ior)  -> nil` | ガラスの屈折率 (最小 1)。 |
| `scene:MatSetEmission(id, r, g, b, strength)  -> nil` | 発光色に強さを掛けたもの。発光するサーフェスはシーンを照らします。点光源を遮らないので、ライトを電球の中に置けます。 |
| `scene:MatSetTexture(id, texId)  -> nil` | UV でサンプリングされるアルベドテクスチャ (登録済みのテクスチャ id)。-1 = なし。 |
| `scene:MatSetNormalMap(id, preset)  -> nil` | プロシージャルな法線マップ: `"none"`、`"wood"`、`"perlin"`、`"waves"`。 |
| `scene:MatSetNormalMapTexture(id, texId)  -> nil` | テクスチャ付きジオメトリ用のタンジェント空間の法線マップテクスチャ (RGB エンコードされた法線)。プリセットを上書きします。-1 でクリア。 |
| `scene:AddSphere(cx, cy, cz, r, mat)  -> id` | 球のプリミティブ。 |
| `scene:AddPlane(px, py, pz, nx, ny, nz, mat)  -> id` | 点を通り指定した法線 (自動的に正規化) を持つ無限平面。 |
| `scene:AddBox(minx, miny, minz, maxx, maxy, maxz, mat)  -> id` | 軸に平行なボックスのプリミティブ。 |
| `scene:AddTorus(cx, cy, cz, R, r, axis, mat)  -> id` | 大半径 `R`、小半径 `r` のトーラス。axis は 0 = X、1 = Y、2 = Z。 |
| `scene:AddSDF(preset, cx, cy, cz, sx, sy, sz, mat)  -> id` | 軸ごとにスケールされるレイマーチングの符号付き距離形状: `"sphere"`、`"roundbox"`、`"torus"`、`"capsule"`、`"gyroid"`、`"octahedron"`、`"gem"`、`"diamond"`。 |
| `scene:ClearPrimitives()  -> nil` | すべてのプリミティブを取り除きます。 |
| `scene:ClearRaytraceModel()  -> nil` | すべてのマテリアル、ポイントライト、プリミティブを取り除きます。 |
