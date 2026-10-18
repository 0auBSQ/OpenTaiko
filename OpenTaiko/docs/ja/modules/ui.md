<!-- modules/ui.md -->

# UI

マウス、キーボード、パッドで操作し、1 つのハイライトを共有するメニューとダイアログ用の UI ライブラリです。`UI.SimpleUI` は OpenTaiko が同梱する最小限のライブラリで、スキンは `derive` で独自の見た目を与えられます。Open-World Memories は PopUI でそうしています。グローバルのモジュールは `UI.Pick` からライブラリを受け取ります。これはスキンが選んだもの (またはステージが指定したもの) を返すため、スキンのすべての画面が同じ見た目になります。

## UI.Pick

`require("UI.Pick")` は関数を返し、それを呼び出すと UI ライブラリのテーブルが返ります。

| 呼び出し | ライブラリ |
| --- | --- |
| `Pick(name)` | その require 名のライブラリ (`"PopUI"`、`"UI.SimpleUI"`、...)。特定のライブラリを使いたいステージ用です。 |
| `Pick()` | 共有文字列 `ui_library` が指定するライブラリ。スキンが一度設定します (Open-World Memories の `_boot` は `"PopUI"` を設定します)。 |
| `ui_library` がないときの `Pick()` | `UI.SimpleUI`。 |

読み込めない名前や、`new` 関数を持たないものを読み込む名前も `UI.SimpleUI` になるため、別のスキンが共有文字列に残した名前があっても問題は起きません。この選択は普通の Lua と共有文字列だけで行われ、エンジンは何も関知しません。

```lua
-- in a skin's _boot onStart: the library every global module will use
SHARED:SetSharedString("ui_library", "MyUI")

-- in any module
local UI = require("UI.Pick")()
local ui = UI.new{ navPlayer = 1 }
```

## UI.SimpleUI

`UI.new(opts)` はマネージャーを作ります。マネージャーはウィジェットを所有し、入力を振り分け、それらを描画します。毎フレーム `ui:update(ts)` と `ui:draw()` を呼び出してください。

| `opts` のフィールド | 説明 |
| --- | --- |
| `theme` | ライブラリの既定の見た目の上にマージするテーマのテーブル (テーマ設定を参照)。 |
| `sfx` | 名前ごとのサウンドで、既定値の上にマージされます (サウンドを参照)。 |
| `bg` | true にすると、まず画面全体にテーマの `bg` 色を描画します。 |
| `navPlayer` | 太鼓でも操作できるプレイヤー。`NavInput.getPn` と同じ形式で指定し、nil ならすべてのプレイヤーです。 |
| `nav` | `"spatial"`: 矢印キーで、その方向にある最も近いウィジェットへ移動します (グリッド)。既定では上下キーで、作成した順にウィジェットを移動します。 |

| メソッド | 説明 |
| --- | --- |
| `ui:update(ts)  -> "cancel" or nil` | 1 フレーム分の入力とアニメーションを処理します。Cancel が押されたときは、ステージが対応できるよう `"cancel"` を返します。 |
| `ui:draw()` | 表示中のすべてのウィジェットを、作成した順に描画します。 |
| `ui:button(o)`, `ui:label(o)`, ...  `-> widget` | ウィジェットを作成して追加し、それを返します (ウィジェットを参照)。 |
| `ui:add(w)`, `ui:remove(w)` | 手作業で作ったウィジェットを追加または削除します。 |
| `ui:clear()` | すべてのウィジェットを削除し、そのキャンバスを解放します。 |
| `ui:disposeWidgets()` | ウィジェットのキャンバスを解放します。キャンバスは自動では解放されないため、マネージャーを手放す前に呼び出してください。 |
| `ui:focus(w)  -> bool`, `ui:focused()  -> widget` | キーフォーカスをウィジェットに移します。フォーカスを持つウィジェット。 |
| `ui:focusNext()`, `ui:focusPrev()` | キーフォーカスを順に移動します。 |
| `ui:setTheme(t)`, `ui:setSfx(t)` | マネージャーのテーマまたはサウンドを置き換えます。すべてのウィジェットのスタイルが更新されます。 |
| `ui:isCapturing()  -> bool` | テキストボックスがキー入力を受け取っている間は true。その間は独自のホットキーを処理しないでください。 |
| `ui:mouseActive()  -> bool` | 最後に使われたデバイスがマウスである間は true。 |
| `ui:drawText(size, str, x, y, color, opacity?, scale?, maxWidth?)` | UI のグリフフォントで、左上を基準にテキストを描画します。 |
| `ui:drawTextEx(size, str, x, y, fg, bg, opacity, scale, maxWidth, anchor)` | 同じ処理に、縁取りの色と 9 つのアンカー名のいずれかを加えたもの。 |
| `ui:measureText(size, str)`, `ui:wrapLines(size, str, maxWidth)`, `ui:ellipsize(size, str, maxWidth)` | 同じフォントでのテキストの計測、折り返し、省略。 |
| `ui:textNudge(size)  -> number` | テキストの 1 行を中心線の上で中央に揃えるオフセット。 |
| `UI.flushSharedFonts()` | キャッシュしたフォントを破棄します。言語 (したがってフォント) が変わったときに呼び出してください。 |

```lua
local UI = require("UI.Pick")()
local ui

function activate()
    ui = UI.new{ navPlayer = 1 }
    ui:panel{ x = 560, y = 240, w = 800, h = 600, title = "Options" }
    ui:toggle{ text = "Hints", x = 620, y = 360, value = true, onChange = function(v) hints = v end }
    ui:button{ text = "Back", x = 860, y = 720, onClick = function() leaving = true end }
end

function update(ts)
    if ui:update(ts) == "cancel" or leaving then return Exit("stage", "_title") end
end

function draw() ui:draw() end

function deactivate() ui:disposeWidgets() end
```

## ウィジェット

すべてのウィジェットは `x`、`y`、`w`、`h` に加えて、`enabled`、`visible`、`focusable`、`mouseOnly` (ホバーとクリックはできるが、キー操作では飛ばされる)、`style` または `theme` (このウィジェットだけのテーマフィールド)、`sfx`、`disabledLook = "fade"` を受け取ります。オプションはウィジェットのフィールドになります。変更するにはメソッド (`setEnabled(b)`、`setVisible(b)`、`setText(t)`、...) を使ってください。メソッドはウィジェットのスタイルを更新します。

| ファクトリ | オプションとメソッド |
| --- | --- |
| `ui:button(o)` | `text` または `icon` (`"play"`、`"pause"`、`"stop"`、`"back"`、`"close"`、`"lock"`、`"repeat"`、...)、`accent`、`onClick(b)`。`w`/`h` を省略するとサイズはラベルに合わせます。`setText`、`setIcon`。 |
| `ui:label(o)` | `text`、`size` (`"title"`、`"label"`、`"small"` または数値)、`color`、`align`、`chip` (角丸の背景)。フォーカスできません。`setText`。 |
| `ui:toggle(o)`, `ui:checkbox(o)` | `text`、`value`、`onChange(v)`。`setValue(v, silent)`。 |
| `ui:slider(o)` | `min`、`max`、`step`、`value`、`showValue`、`valueW`、`onChange(v)`。ドラッグするか、フォーカス中に左右キーで操作します。`setValue(v, silent)`。 |
| `ui:textbox(o)` | `value`、`placeholder`、`maxLen`、`onChange`、`onSubmit`。クリックするか Decide で入力を始めます。 |
| `ui:panel(o)` | `title`。後から作られたウィジェットの背後に描画されるコンテナで、`panel:content()` が内側の矩形を返します。フォーカスできません。 |
| `ui:bubble(o)` | `text`、`tail` (`"bottom"`、`"top"`、`"left"`、`"right"`)、`tailPos`、`typewriter`、`onDone`。`setText`、`setTail`、`setSize`。 |
| `ui:menu(o)` | `items` (文字列、または `{ text, value, locked, mark }`)、`selected`、`rowHeight`、`onChange(i, item)`、`onSelect(i, item)`、`onLockedSelect`、`drawLocked`、`hoverSelects`、`scrollbar`。スクロールするリスト。`setSelected(i)`。 |
| `ui:chooser(o)` | `options`、`index`、`wrap`、`onChange(i, option)`。オプションを切り替えるステッパー (左右キー)。`value()`、`setOptions`、`setIndex`。 |
| `ui:settingsList(o)` | `rows` (`{ header = "..." }` または `{ opt = option }`)、`onFocusRow`、`onActivateRow`、`scrollbar`。設定画面のオプション行のリスト。`setRows`。 |
| `ui:coins(o)` | コインカウンター (下記参照)。フォーカスできません。 |

ウィジェットのクラスはサブクラス化のためにライブラリのフィールド (`UI.Button`、`UI.Menu`、...) になっており、`UI.Widget` (基底クラス)、`UI.Shape` (ウィジェットが焼き込む角丸の図形、影、アイコン)、`UI.Theme`、`UI.Sfx`、`UI.Cache` (プールされたキャンバス) もあります。

## コインウィジェット

`ui:coins{ amount, plain, size, iconSize, color, edge }` は、コインアイコンの隣に金額をピルの上に表示します (`plain = true`: アイコンと数字だけを右揃えで表示)。描き方はライブラリごとに異なります。SimpleUI のアイコンは隣にある `Coin.png` で、PopUI は同梱スキンのコインボックスを使います。

| メソッド | 説明 |
| --- | --- |
| `c:setAmount(n)` | `n` をただちに表示します。 |
| `c:add(n)` | 数字を `n` だけ、回しながら変えます。 |
| `c:gain(n)`, `c:pay(n)` | 同じ処理に加え、上に `+n` または `-n` を表示し、サウンド `coin` または `pay` を鳴らします。 |
| `c:amount()  -> number` | 向かっている先の金額。 |
| `c:setPrice(n)`, `c:setBonus(n)` | 上に価格、または `+n` のボーナスを表示します。nil で消えます。 |
| `c:drawCoin(x, y, sx, sy, opacity)` | コインアイコンだけを (x, y) を中心に描画します。 |

## テーマ設定

テーマは 1 つのテーブルです。マネージャーは各ウィジェットの見た目を、ライブラリの既定値、次にマネージャーの `theme`、次にウィジェットの `style` の順に決めます。テーブルはフィールド単位でマージされるため、テーマには変更するものだけを書きます。

| フィールド | 説明 |
| --- | --- |
| `colors` | 0 から 255 の `{r, g, b, a}` の色: `bg`、`surface`/`surface2` (面の上と下)、`primary`/`primary2`、`accent`/`accent2`、`outline`、`text`、`textOnAccent`、`textDisabled`、`shadow`、`gloss`、`focusRing`、`track`、そしてコイン用の `gain`/`loss`。 |
| `radius`, `radiusSmall` | 大きな要素と小さな要素の角の半径。 |
| `outlineWidth` | 枠線の太さ。 |
| `gloss`, `shadow`, `textOutline` | 上部の光沢 (bool)、ドロップシャドウ (`{ dx, dy, layers, grow }`)、縁取り文字 (bool)。 |
| `font` | 文字サイズ: `small`、`label`、`button`、`title`。 |
| `anim` | ホバーと押下の拡大率と時間、そのイージング (`{ "OUT", "QUAD" }`)、`typewriterCps`。 |

```lua
local ui = UI.new{ theme = { colors = { accent = { 220, 90, 60, 255 } }, radius = 14 } }
ui:button{ text = "Delete", x = 100, y = 100, style = { colors = { primary = { 180, 40, 40, 255 } } } }
```

## サウンド

`sfx` テーブルは名前を、サウンドハンドル、関数、または別のエントリの名前に対応付けます: `move`、`hover`、`click`、`toggle`、`decide`、`cancel`、`skip`、`error`、そしてコインウィジェット用の `coin` と `pay`。既定では共有サウンド `Move`、`Decide`、`Cancel`、`Skip`、`Error` を鳴らすため、これらを `SHARED:SetSharedSFX` で登録したスキンは、すべてのグローバルの画面にサウンドを与えられます。ないものは無音のままです。

## derive: 独自の見た目のライブラリ

`UI.SimpleUI.derive(spec)` は、SimpleUI のウィジェット、マネージャー、フォーカス、入力を持ち、独自の見た目を持つ新しいライブラリを返します。

| `spec` のフィールド | 説明 |
| --- | --- |
| `Theme` | テーマモジュール。`DEFAULT` が完全な既定テーマであるテーブルです (SimpleUI の `UI.SimpleUI.theme` をコピーして変更します)。 |
| `Coins` | 任意のコインウィジェットのクラス。独自の描き方のコインカウンター用です。 |
| `name` | ライブラリの名前。 |

```lua
-- System/My Skin/Modules/Lib/MyUI.lua
local SimpleUI = require("UI.SimpleUI")
return SimpleUI.derive{
    name  = "MyUI",
    Theme = require("MyUI.theme"),
}
```

これを `ui_library` に指定すると、すべてのグローバルのモジュールがこれで画面を作ります。Open-World Memories の `Modules/Lib/PopUI.lua` がこのパターンで、Bubblegum テーマと独自のコインウィジェットを持つ SimpleUI です。
