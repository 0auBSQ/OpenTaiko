<!-- modules/world.md -->

# World <span class="badge-exp">実験的</span>

## World.OWM3d

マイルームと同梱スキンのストーリーシーンを支える 3D ワールドエンジンで、エンジンの[ラスタライザーの世界](../api/3d.md)と[物理](../api/3d-physics.md)の上に作られています。マップデータを遊べる場所に変えます。衝突判定が一致するグリッドまたはハイトマップの地形、glTF モデル、衝突して滑るキャラクター、壁を突き抜けないカメラ、トリガー、昼夜のサイクル、天候、水。

<div class="callout">
OWM3d は、その土台の 3D エンジンと同じく実験的です。関数、マップ形式、既定値は、非推奨期間なしにリリース間で変わることがあります。これを使うスキンは、アップデートのたびにテストし直してください。
</div>

`require("World.OWM3d")` は、ステージが自分で作成する部品を持つファサードを返します。

| フィールド | 内容 |
| --- | --- |
| `World` | ワールド (`World.OWM3d.core`)。`World.new{ rw, rh, screenW, screenH, gridW, gridH, lit, hour, yaw, pitch, fov, dist }` がシーンとすべてのサブシステムを作ります。 |
| `Camera` | カメラリグ (`iso`、`interior`、`orbit`、または `yaw`、`pitch`、`fov`、`dist` のテーブル)、追従、周回、ズーム、そして衝突ブーム。 |
| `Json` | マップファイルのリーダー。`Json.loadMap(dir)` は `<dir>/map.json` を、既定値を適用したプレーンな Lua テーブルに読み込みます。 |
| `IsoMap` | グリッドマップ。セルのグリッドの床、スロープ、壁、建物を、同じ三角形の衝突判定とともに作ります。 |
| `Terrain` | ハイトマップのマップ。高さフィールドからのメッシュ、スプラットテクスチャ、草。 |
| `NPC` | ホーム地点の周りを歩き回る、待機する、または巡回するキャラクターで、会話中はプレイヤーの方を向きます。 |
| `ModelIcon` | 3D モデルのサムネイル。小さなシーンに一度レンダリングし、その後はテクスチャのように描画します (カタログやショップのアイコン)。 |
| `version` | ファサードのバージョン番号 (1)。 |

`World.new` で作ったワールドは、残りをフィールドとして持ちます。`world.phys` (名前付きコライダーグループ、キャラクターコントローラー、レイキャスト)、`world.models` (モデルのインスタンス)、`world.triggers` (グループの切り替え、カメラリグの変更、テレポート、マップの変更、関数の実行を行うボリューム)、`world.particles`、`world.weather` (`clear`、`rain`、`storm`、`snow`、`sakura`、`embers`)、`world.water`、`world.daynight`、`world.cam`。各部品はそれ自体も `World.OWM3d.<part>` というモジュールです。

```lua
local OWM = require("World.OWM3d")
local world, last = nil, 0

function onStart()
    world = OWM.World.new{ rw = 1280, rh = 720, screenW = 1920, screenH = 1080, lit = true }
    world:registerMap("plaza", { type = "folder", dir = "maps/plaza" })
    world:loadMap("plaza", "default")
end

function update(ts)
    local dt = (ts - last) / 1000; last = ts
    world:update(dt, px, py, pz)   -- the player's position drives the triggers and the camera
end

function draw()
    world:render()
    world:blit()
end
```

マップフォルダ、モデル、テクスチャはステージのファイルです。相対パスは呼び出し元のステージのフォルダを基準に解決されます。ステージがワールドを使い終えたら `world:dispose()` を呼び出してください。
