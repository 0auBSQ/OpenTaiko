<!-- modules/README.md -->

# 提供モジュール

OpenTaiko には、どのスキンからも使える Lua モジュールのライブラリが同梱されています。ヘルパー (イージング、色、JSON、入力、ローカライズ)、UI ライブラリ、ダイアログボックス、3D ワールドエンジン、そして未知の塔、秘密の金庫、コインショップのルールとデータです。これらはスキンではなくゲームフォルダにあるため、新しいスキンは何もコピーせずに使えます。この節ではそれぞれを説明します。それらが土台とするエンジンのグローバルは [API リファレンス](../api/README.md)で扱います。

## Global/Modules

ゲームのインストールフォルダの `Global/Modules/` には、スキンの `Modules/` と同じサブフォルダ `Transitions/`、`Stages/`、`Activities/`、`ROActivities/`、`Lib/` があります。ゲームは、どのスキンが有効であっても、各カテゴリでスキンのモジュールの直後にグローバルのモジュールを読み込みます。これらのページのモジュールは `Global/Modules/Lib/` のファイルで、`require` で読み込みます。`Global/Modules/Stages/` には、[マイルーム](my-room.md)のように、どのスキンからも開けるステージもあります。

このフォルダはゲームのビルドとともに届き、アップデートのたびに置き換えられるため、スキンはここにファイルを入れないでください。提供モジュールを変更するには、別の名前で自分のスキンにコピーしてください。

## require の検索順

`require` はドットをフォルダに変換し、大文字と小文字を区別しないため、`require("Core.Easing")` は `Core/Easing.lua` を読み込みます。検索するフォルダは、実行中のモジュールによって変わります。

| 呼び出し元 | 検索するフォルダ (順番に) |
| --- | --- |
| スキンのモジュール (`System/<skin>/Modules/...`) | 自身のフォルダ、スキンの `Modules/Lib/`、次に `Global/Modules/Lib/` |
| グローバルのモジュール (`Global/Modules/...`) | 自身のフォルダ、`Global/Modules/Lib/`、次にスキンの `Modules/Lib/` |

Lib ファイルは自分の Lua ステートを持ちません。require したモジュールのステートで実行されるため、その中の require は呼び出し元の順序に従います。したがって、スキンの `Modules/Lib/` に提供モジュールと同じパスのファイルがあると、スキンのモジュールからの require では、他の提供モジュール内部の require も含めて、すべてそのファイルが代わりに使われます。モジュールを置き換えるつもりがない限り、自分の Lib ファイルを下のフォルダ名に置かないでください。

## 命名

各コンポーネントは `Global/Modules/Lib/` のフォルダで、そのモジュールは `<Component>.<Module>` として require します。

| コンポーネント | モジュール | ページ |
| --- | --- | --- |
| `Core` | `Easing`, `Color`, `Util`, `Json`, `Here` | [Core](core.md) |
| `Input` | `NavInput` | [Input](input.md) |
| `Audio` | `MusicDuck` | [Audio](audio.md) |
| `Locale` | `i18n` | [Locale](locale.md) |
| `Calendar` | `almanac` | [Calendar](calendar.md) |
| `Songs` | `PlayStrings` | [Songs](songs.md) |
| `World` | `OWM3d` とその部品 | [World](world.md) |
| `UI` | `SimpleUI`, `Pick` | [UI](ui.md) |
| `Saves` | `SavePicker` | [Saves](saves.md) |
| `Characters` | `Student` | [Characters](characters.md) |
| `VN` | `Dialogue` | [VN](vn.md) |
| `Pagoda` | `Levels`, `Floor`, `Rewards`, `Progress`, `Songs`, `Run`, `Texts` | [Pagoda](pagoda.md) |
| `Vault` | `Keys`, `Chests`, `Texts` | [Vault](vault.md) |
| `Shop` | `Daily`, `Catalog`, `Texts`, `Seasonal`, `Popup` | [Shop](shop.md) |
| `Online` | `Lobby`, `Protocol`, `Identity`, `Media`, `Texts` | [Online](online.md) |
| `Stages` | `CustomStages` | [カスタムステージ](custom-stages.md) |

## Open-World Memories が残す名前

同梱スキンは以前、これらのファイルの大半を自身の `Modules/Lib/` に持っていました。現在、古いファイルはそれぞれ 2 行の転送ファイルになっているため、古い名前をまだ require するスキンのモジュールはグローバルのモジュールを受け取り、両方の名前が 1 つのインスタンス (1 つの状態、1 つのキャッシュ) を共有します。

```lua
-- moved to Global/Modules/Lib/Core/Easing.lua
return require("Core.Easing")
```

| 旧名 | 提供モジュール |
| --- | --- |
| `Easing`, `Color`, `Util` | `Core.Easing`, `Core.Color`, `Core.Util` |
| `NavInput`, `MusicDuck`, `PlayStrings` | `Input.NavInput`, `Audio.MusicDuck`, `Songs.PlayStrings` |
| `i18n`, `almanac` | `Locale.i18n`, `Calendar.almanac` |
| `OWM3d`, `OWM3d.<part>` | `World.OWM3d`, `World.OWM3d.<part>` |
| `PopUI.<part>` (共有コード: `manager`、`widget`、`shape`、`util`、`cache`、`sfx`、`widgets.coins` を除くすべての `widgets.*`) | `UI.SimpleUI.<part>` |
| `SavePicker`, `Student`, `dialogue` | `Saves.SavePicker`, `Characters.Student`, `VN.Dialogue` |

`PopUI` 自体はスキンのモジュールのままです。スキン独自の見た目を持つ SimpleUI です ([UI](ui.md) を参照)。新しいコードは提供モジュールの名前を require してください。

## アセットは呼び出し元を基準に解決される

`TEXTURE`、`SOUND`、`VIDEO`、`JSONLOADER`、`SQL`、`DATABASE`、`STORAGE`、`SHARED` に渡す相対パスは、呼び出し元のモジュールのフォルダを基準に解決され、呼び出しを行う Lib ファイルが基準になることはありません。自分のファイル (画像、JSON のプール、データベース) を同梱する提供モジュールは、ファイル自身のフォルダを返す `Core.Here` でそれらを見つけ、絶対パス版のローダーで読み込みます。

```lua
-- Global/Modules/Lib/MyPart/Thing.lua
local HERE = require("Core.Here")(...)          -- ".../Global/Modules/Lib/MyPart/"

local icon = TEXTURE:CreateTextureFromAbsolutePath(HERE .. "icon.png")
local beep = SOUND:CreateSFXFromAbsolutePath(HERE .. "beep.ogg")
local data = JSONLOADER:JsonParseFileAny(HERE .. "data.json")
SHARED:SetSharedTextureUsingAbsolutePath("thing_icon", HERE .. "icon.png")
```

`...` はファイルのトップレベルから渡してください。そこでは `require` が各ファイルにモジュール名と絶対パスを渡します。呼び出し元が提供モジュールに渡す相対パス (`World.OWM3d` のモデルファイル、ウィジェットのサウンド) は呼び出し元のもので、通常どおり呼び出し元のフォルダを基準に解決されます。

## 文字列はモジュールと一緒に置く

`THEME:GetSkinString` は有効なスキンの `Locales/` しか読まないため、提供モジュールは自分のテキストにこれを使いません。その文字列はモジュールの隣の `lang/` フォルダに、機能と言語ごとに 1 つの JSON ファイル (`lang/en/vault.json`、`lang/ja/vault.json`、...) として置かれ、モジュールのフォルダを指定して [Locale.i18n](locale.md) で読み込みます。

```lua
local HERE = require("Core.Here")(...)
local T = require("Locale.i18n").texts("vault", HERE)   -- HERE .. "lang/<code>/vault.json"
local name = T:tr("chest_1")
```

英語 (`lang/en/`) は、ファイルやキーが欠けている言語のフォールバックです。グローバルのステージやアクティビティは、フォルダ引数なしで同じことを行います。`texts(name)` はステージ自身の `lang/` フォルダを読みます。

## 共有の状態

`SHARED` のテクスチャ、サウンド、文字列は、スキンのモジュールもグローバルのモジュールも含めて、ゲーム全体で 1 つの名前空間です。提供モジュールはいくつかの固定キーを使い、それぞれのページに一覧があります (例えば共有文字列 `ui_library`、テクスチャ `myroom_thumb_<id>`)。衝突しないよう、自分のキーには接頭辞を付けてください。セーブデータ (トリガー、カウンター、文字列) はモジュールではなくセーブファイルに属し、未知の塔、金庫、ショップ、マイルームはこれを通じてアイテムを受け渡します。
