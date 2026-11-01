<!-- modules/saves.md -->

# Saves

## Saves.SavePicker

在打开舞台之前提出的“用谁的存档？”问题：本地存档的列表，每个存档带有名牌和一行信息（或其金币数）。提问的舞台通过 `SHARED` 把选择交给它要打开的舞台。除非调用方另行传入，列表使用皮肤的 UI 库（[UI.Pick](ui.md)）构建，标题来自模块自己的 `lang/`。

| 函数 | 说明 |
| --- | --- |
| `SavePicker.open(o)  -> slot or nil` | 打开列表。只有一个存档（或没有存档）时不打开任何东西，并立即返回该槽位（没有时为 0）；否则返回 nil，列表保持打开。 |
| `SavePicker.update(ts)  -> nil, "cancel" or slot` | 列表打开期间每帧调用：显示期间返回 nil，然后返回 `"cancel"` 或所选的从 0 起的槽位。两种情况下列表都会关闭。 |
| `SavePicker.draw()` | 绘制列表。 |
| `SavePicker.isOpen()  -> bool`, `SavePicker.close()` | 列表是否显示中；关闭列表。 |
| `SavePicker.pass(slot)` | 把一个槽位交给即将打开的舞台（共享字符串 `entry_save`）。 |
| `SavePicker.take()  -> slot or nil` | 在被打开的舞台中：读取交给它的槽位，只读一次（字符串会被清除）；没有带槽位进入或该存档已不存在时为 nil。 |
| `SavePicker.first()  -> slot` | 第一个存在的存档，供没有经过选择就打开的舞台使用。 |
| `SavePicker.saves()  -> table` | 现有的存档：`{ { value = slot, save = save file }, ... }`。 |
| `SavePicker.text(id)  -> string` | 模块的某个字符串（`title`、`title_room`、`title_shop`），使用游戏的语言。 |

| `o` 字段 | 说明 |
| --- | --- |
| `title` | 列表的标题。 |
| `kind` | `"room"` 或 `"shop"`，在未给出 `title` 时使用模块自带的“谁的房间？”或“谁的商店？”标题。 |
| `info(slot, save)` | 一个返回显示在存档行右侧文本的函数，或 nil。 |
| `coins` | 为 true 时改为显示每个存档的金币数。 |
| `theme`, `sfx` | 传给 UI 管理器。 |
| `ui` | 一张 UI 库表或它的 require 名称；默认是 [UI.Pick](ui.md)。 |

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
