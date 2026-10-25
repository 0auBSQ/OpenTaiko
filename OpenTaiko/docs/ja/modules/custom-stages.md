<!-- modules/custom-stages.md -->

# カスタムステージ

カスタムステージは、誰でも作って共有し、インストールできるステージで、スキンのカスタムステージのメニューに並びます。[STAGES:SetNamespace](../api/activities.md) で名前空間 `CStage` を宣言し、共有文字列 `custom_stage_return` を使って、自分を開いたステージへ戻ります。`Stages.CustomStages` はその両方を行い、メニュー向けにカスタムステージを一覧にします。`Global/Modules/Stages/custom_stage_tutorial` もその1つで、カスタムステージの作り方をゲームの中で案内します。

## 宣言する

Script.lua の先頭、関数の外で:

```lua
-- Global/Modules/Stages/lantern_walk/Script.lua, at the top
local CS = require("Stages.CustomStages")

CS.declare{
    title       = CS.allLanguages("menu", "title"),       -- lang/<code>/menu.json, every language
    description = { en = "A calm walk down a street of paper lanterns." },
}
STAGES:SetResolution(1920, 1080)
```

その隣の `STAGES:SetResolution(1920, 1080)` で、ステージが描画する解像度をエンジンに伝えます。解像度の違うスキンでは、切れずに拡大縮小されます。

`CS.declare(info)` は `STAGES:SetNamespace("CStage", info)` と同じです。メニューは info の2つのフィールドを読みます。

| フィールド | 表示 |
| --- | --- |
| `title` | 一覧に出るステージ名。文字列か、言語コードごとのテーブルです。ないときはフォルダー名です。 |
| `description` | ステージを選んでいる間、一覧の下に出る1、2行。省略できます。 |

言語コードごとのテーブルからは、ゲームの言語のテキスト、なければ英語、それもなければテーブルにあるいずれかの言語のテキストが得られます。ほかのフィールドは自由に使えます。メニューは info をそのまま渡します。

## 戻り先

カスタムステージを開くメニューは、共有文字列 `custom_stage_return` に戻り先のステージ (`"<stage>"` か `"<stage>|<transition>"`) を入れてから、カスタムステージへ移動します。カスタムステージは入ったときにこの文字列を読んで消し、出るときはそのステージへ、文字列が空なら `_title` へ移動します。

```lua
local back, backTransition

function activate()
    back, backTransition = CS.takeReturn()   -- "_title" when the string is empty
end

function update(ts)
    if leaving then return Exit("stage", back, backTransition) end
end
```

文字列は入るたびに読まれます。自分の別のステージへ移動して戻ってくるカスタムステージは、マイルームが `my_room_return` でするように、出る前にもう一度文字列を設定します。

## Stages.CustomStages

| 関数 | 説明 |
| --- | --- |
| `CS.declare(info)  -> boolean` | 呼び出し元のステージをカスタムステージとして宣言します。`STAGES:SetNamespace("CStage", info)` です。 |
| `CS.allLanguages(name, id)  -> table` | 呼び出し元のステージの `lang/<code>/<name>.json` にある文字列 `id` を、それを持つ9言語それぞれから集め、言語コードごとのテーブルにします。言語が決まる前に実行される `declare` のためのものです。 |
| `CS.localized(value, code?)  -> string or nil` | 宣言されたテキストを `code` (既定はゲームの言語) で返します。文字列はそのまま、言語コードごとのテーブルは上の説明のとおりです。 |
| `CS.list(code?)  -> table` | 呼び出し元のモジュールが届くカスタムステージをタイトル順に返します。`{ name, isGlobal, title, description, info }` で、`title` と `description` はすでにその言語になっています。 |
| `CS.enter(name, back, transition?)` | `custom_stage_return` に `back` を入れて、ステージ `name` へ移動します。ステージの update で `return CS.enter(...)` と書きます。 |
| `CS.takeReturn()  -> stage, transition` | `custom_stage_return` を読んで消します。ステージ (文字列が空なら `_title`) とトランジション (既定のものなら nil) を返します。 |
| `CS.open(o)`, `CS.update(ts)`, `CS.draw()`, `CS.isOpen()`, `CS.close()` | 呼び出し元の画面に重なるダイアログとしての一覧で、スキンの UI ライブラリ ([UI.Pick](ui.md)) で作られます。`update` は開いている間は nil、`"cancel"`、または選ばれた `CS.list` のエントリを返し、どちらの場合もダイアログは閉じます。`o`: `title`、`ui` (ライブラリのテーブルかその require 名)、`theme`、`sfx`、`navPlayer` (既定は 0: 太鼓を除くキーボードとパッド)。カスタムステージが1つもないときは、そのことを OK ボタンの上に表示します。 |
| `CS.NAMESPACE`, `CS.RETURN_KEY` | `"CStage"` と `"custom_stage_return"` です。 |

文字列は自身の `lang/` フォルダーに9言語であります ([Locale.i18n](locale.md))。Open-World Memories は、タイトル画面の「設定」の上にあるカスタムステージの項目から一覧を開きます。

```lua
-- a menu stage: CS.open{ title = "Custom Stages" } when its entry is chosen, then
function update(ts)
    if CS.isOpen() then
        local r = CS.update(ts)
        if r ~= nil and r ~= "cancel" then return CS.enter(r.name, "_title") end
        return
    end
end

function draw()
    if CS.isOpen() then CS.draw() end
end
```

## カスタムステージの置き場所

| フォルダー | 動くスキン |
| --- | --- |
| `Global/Modules/Stages/<name>/` | すべてのスキン。 |
| `System/<skin>/Modules/Stages/<name>/` | そのスキンだけ。 |

同じ名前が両方にあるとき、スキンのメニューはスキンのステージを、グローバルのメニューはグローバルのステージを一覧にします。`Exit` が開くステージです。ゲームは起動時とスキンの再読み込み時にステージを作るので、ゲームの実行中にインストールしたステージは再起動後に現れます。

## 共有する

カスタムステージはそのフォルダーそのものです。フォルダー自体を zip にして、中身が `<name>/Script.lua` から始まるようにし、読み込むものすべてと素材のクレジットを入れます。プレイヤーはそれをゲームフォルダーの `Global/Modules/Stages/`、またはスキンの `Modules/Stages/` に展開し、ゲームを再起動します。アップデートはファイルを追加・置き換えしますが削除はしないので、同梱のステージが使わないフォルダー名を選んでください。

## チュートリアル

`custom_stage_tutorial` は、カスタムステージを作って共有するために知っておく4つのことをゲームの中で示します。フォルダーの置き場所 (と置いてはいけない場所)、`CStage` 名前空間、使うものはすべて自分のフォルダーに収め外からは付属モジュールだけを使うこと、そしてこのドキュメントを読む場所、OpenTaiko Hub の「ドキュメント」タブです。各ページは `Textures/` フォルダーの画像と `lang/` フォルダーのテキストなので、1ファイルずつ差し替えられます。
