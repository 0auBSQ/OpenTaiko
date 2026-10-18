<!-- modules/input.md -->

# Input

## Input.NavInput

すべてのデバイスを同時に扱うメニュー操作です。キーボードの矢印キー、Return と Escape、キー設定の共通入力 (`RightChange`、`LeftChange`、`Decide`、`Cancel`)、そして各プレイヤーの太鼓 (青い縁で移動、赤い面で決定)。画面はキーを列挙せずに「プレイヤーは決定を押したか」を問うことができ、プレイヤー自身のキー設定が適用されます。

各バインドは、そのフレームで true を返す関数です。他の入力イベントには名前に `Pressed` (接尾辞なしと同じ)、`Pressing`、`Released` を付けます: `decide()`、`decidePressing()`、`leftReleased()`。

| バインド | 反応する入力 |
| --- | --- |
| `right`, `left` | 右または左の矢印キー、`RightChange` / `LeftChange`、青い縁。 |
| `rightKeyboard`, `leftKeyboard` | 矢印キーのみ。 |
| `up`, `down` | 上または下の矢印キー (太鼓には縦方向の入力がありません)。 |
| `upOrPadLeft`, `downOrPadRight` | 上 (下) の矢印キー、またはパッドや太鼓で `left` (`right`) が反応する入力。太鼓でスクロールできる縦のリスト用です。 |
| `decide` | Return、`Decide`、赤い面。 |
| `cancel` | Escape、`Cancel`。 |
| `rightOtherPlayer`, `leftOtherPlayer` | プレイヤー別のセットのみ: 矢印キー、または他のプレイヤーの青い縁。 |

| 関数またはフィールド | 説明 |
| --- | --- |
| `NavInput.decide()`, `NavInput.left()`, ...  `-> bool` | すべてのプレイヤーに対するバインド: キーボード、共通入力、どのプレイヤーの太鼓も含みます。 |
| `NavInput.getPn(player)  -> table` | 1 人のプレイヤーのバインド。1 から 5 はそのプレイヤーの太鼓とキーボードと共通入力、0 はキーボードと共通入力のみ、`""` または nil はすべてのプレイヤー (上の省略形と同じ) です。 |
| `NavInput.p[player]` | 0 から 5 と `""` に対する `getPn` のテーブル。一度だけ作られます。 |
| `NavInput.bindingNames(binding, player?)  -> string[]` | バインドが反応する入力を、ヒント用の表示名で返します。最初にキーボードのキー、次にキー設定のバインド (`"Pad 3"`、`"MIDI 38"`、`"Mouse Left"`) で、重複はありません。 |
| `NavInput.keyName(key)  -> string` | キーボードのキーの表示名 (`"Return"` は `"Enter"`、`"D1"` は `"1"`)。キーがなければ nil。 |

```lua
local NavInput = require("Input.NavInput")
local p1 = NavInput.getPn(1)

function update(ts)
    if p1.right() then cursor = cursor + 1 end
    if p1.left() then cursor = cursor - 1 end
    if NavInput.cancel() then return Exit("stage", "_title") end
end
```
