<!-- modules/input.md -->

# Input

## Input.NavInput

同时覆盖所有设备的菜单导航：键盘方向键、Return 和 Escape、按键设置中的通用输入（`RightChange`、`LeftChange`、`Decide`、`Cancel`），以及每位玩家的鼓（蓝色鼓边移动，红色鼓面确认）。界面无需列出按键就能询问“玩家按了确认吗？”，并且会套用玩家自己的按键设置。

每个绑定都是一个在该帧返回 true 的函数。在名称后加上 `Pressed`（与不加后缀相同）、`Pressing` 或 `Released` 可得到其他输入事件：`decide()`、`decidePressing()`、`leftReleased()`。

| 绑定 | 响应的输入 |
| --- | --- |
| `right`, `left` | 右或左方向键、`RightChange` / `LeftChange`、蓝色鼓边。 |
| `rightKeyboard`, `leftKeyboard` | 仅方向键。 |
| `up`, `down` | 上或下方向键（鼓没有纵向输入）。 |
| `upOrPadLeft`, `downOrPadRight` | 上（下）方向键，或 `left`（`right`）在手柄或鼓上响应的输入：用于可以用鼓滚动的纵向列表。 |
| `decide` | Return、`Decide`、红色鼓面。 |
| `cancel` | Escape、`Cancel`。 |
| `rightOtherPlayer`, `leftOtherPlayer` | 仅限按玩家的绑定集：方向键，或另一位玩家的蓝色鼓边。 |

| 函数或字段 | 说明 |
| --- | --- |
| `NavInput.decide()`, `NavInput.left()`, ...  `-> bool` | 同时面向所有玩家的绑定：键盘、通用输入以及任意玩家的鼓。 |
| `NavInput.getPn(player)  -> table` | 某一位玩家的绑定：1 到 5 表示该玩家的鼓加上键盘和通用输入，0 表示仅键盘和通用输入，`""` 或 nil 表示所有玩家（与上面的简写相同）。 |
| `NavInput.p[player]` | 0 到 5 以及 `""` 对应的 `getPn` 表，只构建一次。 |
| `NavInput.bindingNames(binding, player?)  -> string[]` | 某个绑定响应的输入，以用于提示的显示名称返回：先是键盘按键，然后是按键设置中的绑定（`"Pad 3"`、`"MIDI 38"`、`"Mouse Left"`），不重复。 |
| `NavInput.keyName(key)  -> string` | 键盘按键的显示名称（`"Return"` 得到 `"Enter"`，`"D1"` 得到 `"1"`）；没有按键时为 nil。 |

```lua
local NavInput = require("Input.NavInput")
local p1 = NavInput.getPn(1)

function update(ts)
    if p1.right() then cursor = cursor + 1 end
    if p1.left() then cursor = cursor - 1 end
    if NavInput.cancel() then return Exit("stage", "_title") end
end
```
