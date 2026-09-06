<!-- guides/skins.md -->

# スキンとテーマの追加

スキンは、ゲームの `System/` ディレクトリの下にあるフォルダです。グラフィック、サウンド、フォント、レイアウト値、ロケールファイル、そしてすべての画面を描画する Lua モジュールを提供します。このガイドでは、フォルダをスキンたらしめるもの、`SkinConfig.ini` のキー、フォルダ構成、Lua モジュールツリーとそのライフサイクル、そしてスキンをインストールして選択する方法を説明します。Lua API 自体 (描画、サウンド、入力など) は API リファレンスで扱います。

スキンは各画面を動かす Lua モジュールも含んでいます。ゲームは特定のモジュールを名前で読み込み、特定のステージへジャンプするため、ゼロから作ったスキンはスキン選択画面には載りますが、ゲームはそれを動作させられません。同梱スキンのコピーから始めてください。

## 始める前に

- OpenTaiko 0.6.1 がインストールされ、同梱スキン `System/Open-World Memories/` が存在すること。
- `SkinConfig.ini`、インクルードされる `*Config.ini` ファイル、Lua モジュール用のプレーンテキストエディタ。
- 画面の動作を変えるつもりなら基本的な Lua の知識。純粋な再テクスチャ (PNG と OGG ファイルの置き換えと `.ini` の値の編集) に Lua は不要です。

## ステップ 1: フォルダをスキンたらしめるものを理解する

起動時にゲームは `System/` のサブフォルダを列挙します。フォルダは、その中に `Graphics/1_Title/Background.png` が存在する場合にのみスキンとみなされ、それ以外のフォルダはゲームがスキップします。選択されたスキンフォルダが存在しない場合、ゲームは `System/Default/`、次にアルファベット順で最初の有効なスキン、次に `System/` 自体にフォールバックします。

このチェックはフォルダをリストに載せるだけです。ステップ 8 で、スキンが動作する前に存在していなければならないモジュールを挙げます。

```
System/
  Open-World Memories/         <- the shipped skin
  My New Skin/                 <- your skin
    Graphics/
      1_Title/
        Background.png          <- required for the folder to be listed
    SkinConfig.ini
```

## ステップ 2: 同梱スキンをコピーする

`System/Open-World Memories/` を新しい隣のフォルダ、例えば `System/My New Skin/` にコピーします。コピーにはゲームが必要とするすべてが含まれています: `Graphics/`、`Sounds/`、`Fonts/`、`Locales/`、`Modules/`、`ThemeSettings.json`、`SkinConfig.ini`、およびそれがインクルードする `*Config.ini` ファイル。フォルダ名がスキンの識別子です (ゲームはこれを選択中のスキンとして記録し、スキン選択画面はこれを表示します) ので、ファイルシステムで安全な名前にしてください。次に、メタデータがあなたのスキンを表すように `SkinConfig.ini` を編集します。

## ステップ 3: SkinConfig.ini を編集する

`SkinConfig.ini` は 1 行に 1 つの設定を持つ `Key=Value` ファイルです。パーサーは先頭の空白とタブを取り除き、`;` で始まる行をコメントとして扱い、行にちょうど 1 つの `=` が含まれているときにのみその行を読み取ります。キーの照合は厳密で、パーサーは不明なキーをエラーを報告せずに無視します。スキンレベルのキーは次のとおりです。

- `Name=`: 表示名。メタデータのみで、スキンを選択するのはフォルダ名です。
- `Version=`、`Creator=`: 自由形式の文字列 (既定 `Unknown`)。ゲームはこれらを検証しません。
- `DefaultLocale=`: アクティブなゲーム言語のファイルが `Locales/` にないときにゲームが使うロケール id (既定 `en`)。
- `Resolution=W,H`: レイアウト値を作成する基準となる解像度 (既定 `1280,720`)。同梱スキンは `1920,1080` を使っています。
- `Resolutions=`: 選択可能なレンダースケールの倍率 (ステップ 4)。
- `AIBattleCharacter=`: AI の対戦相手に使うキャラクターフォルダ (ステップ 5)。
- `FontName<LANG>=` と `BoxFontName<LANG>=`: ゲーム言語ごとのフォントファイル。`<LANG>` は大文字の言語コード (`EN`、`JA`、`FR`、`ES`、`NL`、`DE`、`RU`、`KO`、`ZH`) です。パスはスキンのルートからの相対パス (絶対パスも可) で、ファイルが存在しなければパーサーはキーを破棄します。

それ以外のすべてのキー (`Game_*`、`Result_*`、`Title_*` など) は画面のレイアウト値です。同じパーサーがこれらを読み取るため、ステップ 6 のインクルードされるファイルに置くことができます。

```ini
;Skin information
Name=My New Skin
DefaultLocale=en
Version=1.0.0
Creator=Your Name
Resolution=1920,1080
;Selectable render-scale multipliers (<=1; decimal or a/b fraction, comma-separated). 1 is always available and is the default.
Resolutions=1,2/3,1/3
;Character folder used for the AI battle slot.
AIBattleCharacter=10v2 - AItritus
FontNameEN=Fonts/MPLUSRounded1c-Medium.ttf
FontNameJA=Fonts/MPLUSRounded1c-Medium.ttf
BoxFontNameEN=Fonts/MPLUSRounded1c-Regular.ttf
BoxFontNameJA=Fonts/MPLUSRounded1c-Regular.ttf
```

## ステップ 4: Resolutions オプション

`Resolutions=` は、設定メニューが提示するレンダースケール倍率のカンマ区切りリストです。ゲームは `Resolution` に選択した倍率を掛けた解像度で描画し、結果をウィンドウに拡大します。ウィンドウサイズは変わりません。各トークンは小数 (`0.5`) または分数 (`2/3`) です。パーサーは 0 < 値 <= 1 の範囲外のトークン、解析できないトークン、重複を破棄し、`1` がなければ追加し、リストを `1` を先頭にソートします。セミコロンはコメント行を始めるため、区切りはカンマでなければなりません。設定メニューは各エントリをピクセルサイズとともに表示します。例えば 1920x1080 のスキンでは `2/3 (1280x720)` となります。

```ini
Resolution=1920,1080
Resolutions=1,2/3,1/3
; produces the options:
;   1     -> 1920x1080  (default)
;   2/3   -> 1280x720
;   1/3   -> 640x360
```

## ステップ 5: AIBattleCharacter オプション

`AIBattleCharacter=` は、AI バトルモードで AI の対戦相手にゲームが使う `Global/Characters/` の下のフォルダを指定します。既定は `10v2 - AItritus` です。指定したフォルダは存在している必要があります。

```ini
;Character folder used for the AI battle slot.
AIBattleCharacter=10v2 - AItritus
```

## ステップ 6: #include で設定を分割する

パーサーは `#include SomeFile.ini` の形の行に出会うと、そのファイルをその場で再帰的に読み込みます。パスはスキンのルートからの相対パスです。同梱の `SkinConfig.ini` はメタデータとフォントのキーだけを持ち、その後に画面ごとに 1 ファイルをインクルードしています。スキンをコピーするときはこれらの行を残し、画面を調整するには個々の `*Config.ini` ファイルを編集してください。

```
; tail of SkinConfig.ini (shipped skin, in order)
#include OtherConfig.ini
#include TitleConfig.ini
#include ConfigConfig.ini
#include SongSelectConfig.ini
#include HeyaConfig.ini
#include SongLoadingConfig.ini
#include GameConfig.ini
#include ModIconsConfig.ini
#include NameplateConfig.ini
#include AIResultConfig.ini
#include ResultConfig.ini
#include DaniSelectConfig.ini
#include DanResultConfig.ini
#include TowerResultConfig.ini
#include TowerSelectConfig.ini
#include OnlineLoungeConfig.ini
#include OpenEncyclopediaConfig.ini
#include ModalConfig.ini
#include Game4PConfig.ini
#include Result4PConfig.ini
#include Modal4PConfig.ini
```

## ステップ 7: スキンのフォルダ構成を学ぶ

同梱スキンを基準にすると、スキンのルートには次のものがあります。

- `Graphics/`: 番号付きの画面ごとのフォルダ (`0_Startup`、`1_Title`、`2_Config`、`3_DaniSelect`、`5_Game`、`6_Result`、`7_DanResult`、`7_Exit`、`8_TowerResult`、`10_Heya`、`12_OnlineLounge`、`13_TowerSelect`、`15_OpenEncyclopedia`) にまとめられた画像と、最上位にあるいくつかの共有画像。アニメーションする背景は、それが属するフォルダの画像の隣に置かれた `Script.lua` ファイルです (例えば `Graphics/0_Startup/Script.lua` や `Graphics/5_Game/5_Background/` の下のフォルダ)。
- `Sounds/`: ゲームが決まったファイル名で読み込むシステムサウンドと BGM。例: `Sounds/Move.ogg`、`Sounds/Decide.ogg`、`Sounds/Cancel.ogg`、`Sounds/BGM/Title.ogg`、`Sounds/BGM/SongSelect.ogg`、`Sounds/BGM/Result.ogg`。ファイルがなければそのサウンドは再生されません。
- `Fonts/`: `FontName` キーで参照される `.ttf` ファイル。
- `Locales/`: 言語ごとに 1 つの JSON ファイル (`en.json`、`ja.json`、...) で、形式は `{ "Entries": { "KEY": "text" } }`。これらの文字列はスキン自身の設定のラベルとなり、Lua は `THEME:GetSkinString(key)` を通じてこれらを読み取ります。アクティブな言語にキーがない場合、ゲームは `DefaultLocale` のファイルからそれを検索します。
- `Modules/`: Lua モジュールツリー (ステップ 8)。
- `ThemeSettings.json`: オプション画面がテーマ設定の下に表示する設定の配列。各エントリは `id`、`type` (`bool`、`int`、`double`、`string`、`enum`)、`scope` (既定の `global`、またはセーブファイルごとに 1 つの値を持つ `save`)、ローカライズされた `label` と `description`、`default`、そして型に応じて `min`/`max` または `options` を持ちます。
- `SkinConfig.ini` とインクルードされる `*Config.ini` ファイル。
- `README.txt`、`LICENSE.md`、`Licenses/`: 帰属表示のファイル。ゲームは読み取りません。

```
My New Skin/
  SkinConfig.ini
  ThemeSettings.json
  Graphics/           images by screen; some folders carry a background Script.lua
  Sounds/             fixed-name .ogg system sounds and BGM/
  Fonts/              .ttf files named by the FontName keys
  Locales/            en.json, ja.json, ... ({ "Entries": { ... } })
  Modules/            the Lua module tree (Step 8)
  <screen>Config.ini  layout files pulled in via #include
```

## ステップ 8: Modules ツリーとゲームが必要とするモジュール

スキンの読み込み時、ゲームは `Modules/` の 4 つのサブフォルダを走査し、その中の直接のサブフォルダをそれぞれ、エントリファイルが `Script.lua` である 1 つのモジュールとして扱います。

- `Modules/Transitions/`: ステージ間で再生されるトランジション。ゲームは最初のステージ切り替えに備えて、これらを最初に読み込みます。
- `Modules/Stages/`: 完全な画面。ステージには `Exit("stage", "<folder name>")` で入ります。
- `Modules/Activities/`: ステージの上に重ねる再利用可能なサブ画面 (例えば `confirm_dialog`、`mod_select_dialog`、`song_select_core`)。
- `Modules/ROActivities/`: ゲームが直接駆動する読み取り専用のオーバーレイ。

ゲームは `Modules/Lib/` を走査しません。そこにあるファイルは `require` で読み込みます。モジュールの検索パスは自身のフォルダ、次に `Modules/Lib/` なので、`require("dialogue")` は `Modules/Lib/dialogue.lua` に解決されます。ステージとアクティビティはゲームのインストールフォルダの `Global/Stages/` と `Global/Activities/` の下にも置くことができ、ゲームはそれらをすべてのスキンで読み込みます。

各カテゴリ内で、ゲームはまずすべてのモジュールを作成し、その後 Transitions、Stages、Activities、ROActivities の順にそれぞれの `onStart` を実行します。

ゲームは次のモジュールを名前で参照し、同梱スキンはそのすべてを提供しています。

- ステージ `_boot` と `_title`。どちらかがなければゲームはエラーで停止します。
- ROActivity `modal`、`config_ui`、`nameplate`、`popup_menu`、`modicons`、`song_enum`、`danplate`。
- トランジション `default` と `song_loading`。`song_loading` はゲームが楽曲を読み込む間に再生され、`Exit` がトランジションを指定しないか、存在しないものを指定したときにゲームは `default` を使います。トランジションモジュールをまったく持たないスキンは単純な黒のフェードにフォールバックします。

スキンを作るときはこれらすべてを残し、独自のモジュールをその隣に追加してください。

```
Modules/
  Transitions/   <name>/Script.lua   (loaded first; "default" and "song_loading" used by the game)
  Stages/        <name>/Script.lua   ("_boot" and "_title" required)
  Activities/    <name>/Script.lua
  ROActivities/  <name>/Script.lua   (modal, config_ui, nameplate, popup_menu, modicons, song_enum, danplate required)
  Lib/           shared .lua files reached with require, not scanned
```

## ステップ 9: ステージの Script.lua とそのライフサイクル

`Script.lua` は、エンジンのグローバル (`TEXTURE`、`SOUND`、`INPUT`、`CONFIG`、`THEME` など) がすでに定義された状態で、ゲームがモジュールを作成するときに 1 回実行されます。その後ゲームはグローバル関数を名前で探して呼び出します。ステージの場合:

- `onStart()`: スキンの読み込み時に 1 回。コルーチンとして実行されるため、重い読み込みでは `coroutine.yield()` や `LOADING` ヘルパーを呼んで、ローディングバーの裏で作業を複数フレームに分散できます。
- `activate()`: ゲームがステージに入るたび。これもコルーチンです。ゲームは `CHARACTERLIST` と `PUCHICHARALIST` グローバルをこの直前に更新するので、それらに依存するものはここで構築してください。`onStart` ではまだ空です。
- `update(timestamp)`: 毎フレーム。ステージを離れるには `Exit(target, name, transition)` を返します。`target` は `"title"`、`"play"`、`"stage"` (`name` = ステージフォルダ)、`"legacy"` (`name` = `heya`、`config`、`exit`、`onlinelounge`) のいずれかで、`transition` は `Modules/Transitions/` の下のフォルダで、既定は `default` です。
- `draw()`: 毎フレーム。
- `deactivate()`: ゲームがステージを離れるとき。
- `afterSongEnum()`: 楽曲リストの列挙が完了したとき。
- `onDestroy()`: ゲームがスキンを破棄するとき。

すべて任意で、定義していない関数はゲームがスキップします。アクティビティ、ROActivity、トランジションも、それぞれのフックのセットで同じパターンに従います。

```lua
-- Modules/Stages/my_stage/Script.lua
function onStart()
  -- one-time setup; may coroutine.yield() during heavy loads
end

function activate()
  -- runs each time the stage is entered
end

function update(ts)
  if INPUT:Pressed("Cancel") then
    return Exit("stage", "_title")   -- leave this stage
  end
  return nil
end

function draw()
  -- per-frame rendering
end

function deactivate() end
function afterSongEnum() end
function onDestroy() end
```

## ステップ 10: lang/ でモジュールをローカライズする

モジュールは `Script.lua` の隣の `lang/` サブフォルダに独自の翻訳を置けます。モジュール自身のフォルダが `require` パスにあるため、`require("lang.ja")` は `lang/ja.lua` に解決されます。同梱スキンは大きなステージ (例えば `Modules/Stages/myroom/lang/ja.lua` や `Modules/Stages/intro_nokon/lang/ja.lua`) で、ヘルパー `Modules/Lib/i18n.lua` を通じてこれを行っています。これはステップ 7 のスキン全体の `Locales/` フォルダとは別のものです。

```
Modules/Stages/my_stage/
  Script.lua
  lang/
    ja.lua        -- require("lang.ja")
```

## ステップ 11: スキンをインストールして選択する

フォルダを `System/` の下に置きます。設定を開き、外観のセクションでスキンのオプションからスキンを選びます。選択画面はすべての有効なスキンをフォルダ名で一覧し、`Graphics/1_Title/Background.png` をサムネイルとして表示します。スキンを変更すると、ゲームは現在のスキンを破棄し、新しいスキンを読み込み、そのすべての Lua モジュールをローディングバーの裏で再読み込みします。

ゲームは選択を `Config.ini` に `System/` からの相対パスとして `SkinPath=` に書き込みます。フォルダ名だけを書き込み (例えば Windows では `SkinPath=My New Skin\`)、ファイルのコメントに示されている `./My New Skin/` の形も受け付けます。

```ini
; In Config.ini (written when a skin is picked in-game):
; Skin folder path, relative to System/
SkinPath=My New Skin\
```

## トラブルシューティングと注意点

- 選択画面にスキンが載らない: `Graphics/1_Title/Background.png` がないか、フォルダが `System/` の直下にありません。
- スキンを選択した直後にゲームがエラーになる: 必要なモジュールがない (ステップ 8) か、そのいずれかが Lua エラーを発生させています。スキンは切り替えてテストしてください。
- `SkinConfig.ini` のキーが効かない: キーのスペルが違う、行に複数の `=` がある、または値の解析に失敗しています。パーサーは不明なキーを報告せずに無視します。
- `Resolutions=` に `1` しか表示されない: リストがセミコロン (コメント記号) を使っているか、すべての値が 0 < 値 <= 1 の範囲外です。
- フォントキーが効かない: スキンのルートからの相対パスにファイルが存在しません。
- `onStart` で `CHARACTERLIST` や `PUCHICHARALIST` が空: ゲームはモジュールの作成後にこれらを設定します。`activate` から使ってください。
- スキンフォルダの名前を変えると識別子が変わります。`Config.ini` の `SkinPath` は新しい名前を指す必要があります。
- `Name=`、`Version=`、`Creator=` は情報のみです。ゲームはこれらに対する互換性チェックを行いません。
