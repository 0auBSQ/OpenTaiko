<!-- modules/saves.md -->

# Saves

## Saves.SavePicker

ステージを開く前に尋ねる「誰のセーブ?」の質問です。ローカルのセーブの一覧で、それぞれにネームプレートと 1 行の情報 (またはコイン数) が付きます。尋ねたステージは、開くステージに `SHARED` を通じて選択を渡します。一覧は、呼び出し元が指定しない限りスキンの UI ライブラリ ([UI.Pick](ui.md)) で作られ、タイトルはモジュール自身の `lang/` から取られます。

| 関数 | 説明 |
| --- | --- |
| `SavePicker.open(o)  -> slot or nil` | 一覧を開きます。セーブが 1 つしかない (またはない) 場合は何も開かず、そのスロット (ない場合は 0) をすぐに返します。それ以外の場合は nil を返し、一覧は開いたままになります。 |
| `SavePicker.update(ts)  -> nil, "cancel" or slot` | 一覧が開いている間、毎フレーム呼び出します。表示中は nil、その後 `"cancel"` または選ばれた 0 始まりのスロットを返します。どちらの場合も一覧は閉じます。 |
| `SavePicker.draw()` | 一覧を描画します。 |
| `SavePicker.isOpen()  -> bool`, `SavePicker.close()` | 一覧が表示中かどうか。一覧を閉じます。 |
| `SavePicker.pass(slot)` | これから開くステージにスロットを渡します (共有文字列 `entry_save`)。 |
| `SavePicker.take()  -> slot or nil` | 開かれたステージで、渡されたスロットを一度だけ読みます (文字列はクリアされます)。スロットなしで来た場合や、そのセーブがもう存在しない場合は nil。 |
| `SavePicker.first()  -> slot` | 最初に存在するセーブ。選択なしで開かれたステージ用です。 |
| `SavePicker.saves()  -> table` | 存在するセーブ: `{ { value = slot, save = save file }, ... }`。 |
| `SavePicker.text(id)  -> string` | モジュールの文字列 (`title`、`title_room`、`title_shop`) をゲームの言語で返します。 |

| `o` のフィールド | 説明 |
| --- | --- |
| `title` | 一覧のタイトル。 |
| `kind` | `title` が指定されていないとき、モジュール自身の「誰の部屋?」や「誰のショップ?」のタイトルにするための `"room"` または `"shop"`。 |
| `info(slot, save)` | セーブの行の右側に表示するテキストを返す関数、または nil。 |
| `coins` | true にすると、代わりに各セーブのコイン数を表示します。 |
| `theme`, `sfx` | UI マネージャーに渡されます。 |
| `ui` | UI ライブラリのテーブル、またはその require 名。既定は [UI.Pick](ui.md)。 |

```lua
local SavePicker = require("Saves.SavePicker")
local NavInput = require("Input.NavInput")
local picking = false

function update(ts)
    if picking then
        local r = SavePicker.update(ts)
        if r == nil then return end
        picking = false
        if r ~= "cancel" then SavePicker.pass(r); return Exit("stage", "my_shop") end
    elseif NavInput.decide() then
        local slot = SavePicker.open{ kind = "shop", coins = true }
        if slot == nil then picking = true
        else SavePicker.pass(slot); return Exit("stage", "my_shop") end
    end
end

function draw()
    if picking then SavePicker.draw() end
end

-- in my_shop's activate
local slot = SavePicker.take() or SavePicker.first()
```
