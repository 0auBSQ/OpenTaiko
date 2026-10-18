<!-- modules/ui.md -->

# UI

一个用于菜单和对话框的 UI 库，可用鼠标、键盘和手柄操作，共用一个高亮。`UI.SimpleUI` 是 OpenTaiko 自带的最小库；皮肤可以用 `derive` 给它换上自己的外观，Open-World Memories 就用 PopUI 这样做了。全局模块从 `UI.Pick` 获取它们的库，它给出皮肤选定的库（或舞台指定的库），因此皮肤的每个界面外观一致。

## UI.Pick

`require("UI.Pick")` 返回一个函数；调用它会返回一张 UI 库表。

| 调用 | 库 |
| --- | --- |
| `Pick(name)` | 具有该 require 名称的库（`"PopUI"`、`"UI.SimpleUI"`……），供需要特定库的舞台使用。 |
| `Pick()` | 由共享字符串 `ui_library` 指定的库，由皮肤设置一次（Open-World Memories 的 `_boot` 设置为 `"PopUI"`）。 |
| 没有 `ui_library` 时的 `Pick()` | `UI.SimpleUI`。 |

无法加载的名称，或加载出来的东西没有 `new` 函数的名称，同样会得到 `UI.SimpleUI`，因此另一个皮肤留在共享字符串中的名称不会造成任何问题。这个选择完全由普通的 Lua 和共享字符串完成：引擎对此一无所知。

```lua
-- in a skin's _boot onStart: the library every global module will use
SHARED:SetSharedString("ui_library", "MyUI")

-- in any module
local UI = require("UI.Pick")()
local ui = UI.new{ navPlayer = 1 }
```

## UI.SimpleUI

`UI.new(opts)` 创建一个管理器，它拥有控件、分发输入并绘制控件。每帧调用 `ui:update(ts)` 和 `ui:draw()`。

| `opts` 字段 | 说明 |
| --- | --- |
| `theme` | 合并到库默认外观之上的主题表（见主题）。 |
| `sfx` | 按名称给出的声音，合并到默认值之上（见声音）。 |
| `bg` | 为 true 时先在整个屏幕上绘制主题的 `bg` 颜色。 |
| `navPlayer` | 也可以用鼓导航的玩家，格式与 `NavInput.getPn` 接受的相同；nil 表示所有玩家。 |
| `nav` | `"spatial"`：方向键移动到该方向上最近的控件（网格）。默认情况下，上下键按控件的创建顺序逐个移动。 |

| 方法 | 说明 |
| --- | --- |
| `ui:update(ts)  -> "cancel" or nil` | 运行一帧的输入和动画；按下 Cancel 时返回 `"cancel"`，供舞台处理。 |
| `ui:draw()` | 按创建顺序绘制所有可见控件。 |
| `ui:button(o)`, `ui:label(o)`, ...  `-> widget` | 创建一个控件，添加并返回它（见控件）。 |
| `ui:add(w)`, `ui:remove(w)` | 添加或移除一个手动创建的控件。 |
| `ui:clear()` | 移除所有控件并释放它们的画布。 |
| `ui:disposeWidgets()` | 释放控件的画布。画布不会自行释放，请在丢弃管理器之前调用它。 |
| `ui:focus(w)  -> bool`, `ui:focused()  -> widget` | 把按键焦点移到某个控件；持有焦点的控件。 |
| `ui:focusNext()`, `ui:focusPrev()` | 逐个移动按键焦点。 |
| `ui:setTheme(t)`, `ui:setSfx(t)` | 替换管理器的主题或声音；所有控件都会重新应用样式。 |
| `ui:isCapturing()  -> bool` | 文本框接收按键期间为 true：此时请跳过你自己的快捷键。 |
| `ui:mouseActive()  -> bool` | 最近使用的设备是鼠标时为 true。 |
| `ui:drawText(size, str, x, y, color, opacity?, scale?, maxWidth?)` | 用 UI 的字形字体绘制文本，以左上角为锚点。 |
| `ui:drawTextEx(size, str, x, y, fg, bg, opacity, scale, maxWidth, anchor)` | 同上，另加描边颜色和九个锚点名称之一。 |
| `ui:measureText(size, str)`, `ui:wrapLines(size, str, maxWidth)`, `ui:ellipsize(size, str, maxWidth)` | 用同一字体测量、换行和截断文本。 |
| `ui:textNudge(size)  -> number` | 让一行文本在中线上居中的偏移量。 |
| `UI.flushSharedFonts()` | 丢弃缓存的字体；语言（从而字体）改变时调用。 |

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

## 控件

每个控件都接受 `x`、`y`、`w` 和 `h`，以及 `enabled`、`visible`、`focusable`、`mouseOnly`（可悬停和点击，但按键导航会跳过它）、`style` 或 `theme`（仅用于此控件的主题字段）、`sfx` 和 `disabledLook = "fade"`。这些选项会成为控件的字段；请用方法（`setEnabled(b)`、`setVisible(b)`、`setText(t)`……）修改它们，这些方法会重新应用控件的样式。

| 工厂 | 选项和方法 |
| --- | --- |
| `ui:button(o)` | `text` 或 `icon`（`"play"`、`"pause"`、`"stop"`、`"back"`、`"close"`、`"lock"`、`"repeat"`……）、`accent`、`onClick(b)`。省略 `w`/`h` 时大小贴合标签。`setText`、`setIcon`。 |
| `ui:label(o)` | `text`、`size`（`"title"`、`"label"`、`"small"` 或数字）、`color`、`align`、`chip`（圆角背景）。不可获得焦点。`setText`。 |
| `ui:toggle(o)`, `ui:checkbox(o)` | `text`、`value`、`onChange(v)`。`setValue(v, silent)`。 |
| `ui:slider(o)` | `min`、`max`、`step`、`value`、`showValue`、`valueW`、`onChange(v)`。拖动，或在获得焦点时按左/右键。`setValue(v, silent)`。 |
| `ui:textbox(o)` | `value`、`placeholder`、`maxLen`、`onChange`、`onSubmit`。点击或按 Decide 开始输入。 |
| `ui:panel(o)` | `title`。绘制在之后创建的控件后面的容器；`panel:content()` 给出它的内部矩形。不可获得焦点。 |
| `ui:bubble(o)` | `text`、`tail`（`"bottom"`、`"top"`、`"left"`、`"right"`）、`tailPos`、`typewriter`、`onDone`。`setText`、`setTail`、`setSize`。 |
| `ui:menu(o)` | `items`（字符串，或 `{ text, value, locked, mark }`）、`selected`、`rowHeight`、`onChange(i, item)`、`onSelect(i, item)`、`onLockedSelect`、`drawLocked`、`hoverSelects`、`scrollbar`。一个可滚动列表。`setSelected(i)`。 |
| `ui:chooser(o)` | `options`、`index`、`wrap`、`onChange(i, option)`。一个选项步进器（左/右键）。`value()`、`setOptions`、`setIndex`。 |
| `ui:settingsList(o)` | `rows`（`{ header = "..." }` 或 `{ opt = option }`）、`onFocusRow`、`onActivateRow`、`scrollbar`。设置界面的选项行列表。`setRows`。 |
| `ui:coins(o)` | 金币计数器（见下文）。不可获得焦点。 |

控件类是库的字段（`UI.Button`、`UI.Menu`……），可用于派生子类，此外还有 `UI.Widget`（基类）、`UI.Shape`（控件烘焙的圆角形状、阴影和图标）、`UI.Theme`、`UI.Sfx` 和 `UI.Cache`（池化的画布）。

## 金币控件

`ui:coins{ amount, plain, size, iconSize, color, edge }` 在一个胶囊形底板上、金币图标旁边显示一个数额（`plain = true`：只显示图标和数字，右对齐）。每个库按自己的方式绘制它：SimpleUI 的图标是它旁边的 `Coin.png`，PopUI 保留自带皮肤的金币框。

| 方法 | 说明 |
| --- | --- |
| `c:setAmount(n)` | 立即显示 `n`。 |
| `c:add(n)` | 让数字滚动变化 `n`。 |
| `c:gain(n)`, `c:pay(n)` | 同上，并在上方显示 `+n` 或 `-n`，播放声音 `coin` 或 `pay`。 |
| `c:amount()  -> number` | 它正在滚向的数额。 |
| `c:setPrice(n)`, `c:setBonus(n)` | 在上方显示一个价格或 `+n` 奖励；nil 将其移除。 |
| `c:drawCoin(x, y, sx, sy, opacity)` | 只绘制金币图标，以 (x, y) 为中心。 |

## 主题

主题是一张表。管理器按以下顺序决定每个控件的外观：库的默认值，然后是管理器的 `theme`，然后是控件的 `style`；表按字段合并，因此主题只需写出它要改的内容。

| 字段 | 说明 |
| --- | --- |
| `colors` | 0 到 255 的 `{r, g, b, a}` 颜色：`bg`、`surface`/`surface2`（表面的上部和下部）、`primary`/`primary2`、`accent`/`accent2`、`outline`、`text`、`textOnAccent`、`textDisabled`、`shadow`、`gloss`、`focusRing`、`track`，以及金币用的 `gain`/`loss`。 |
| `radius`, `radiusSmall` | 大元素和小元素的圆角半径。 |
| `outlineWidth` | 边框粗细。 |
| `gloss`, `shadow`, `textOutline` | 顶部光泽（bool）、投影（`{ dx, dy, layers, grow }`）、描边文字（bool）。 |
| `font` | 文字大小：`small`、`label`、`button`、`title`。 |
| `anim` | 悬停和按下时的缩放与时长、它们的缓动（`{ "OUT", "QUAD" }`）、`typewriterCps`。 |

```lua
local ui = UI.new{ theme = { colors = { accent = { 220, 90, 60, 255 } }, radius = 14 } }
ui:button{ text = "Delete", x = 100, y = 100, style = { colors = { primary = { 180, 40, 40, 255 } } } }
```

## 声音

`sfx` 表把一个名称映射到一个声音句柄、一个函数或另一个条目的名称：`move`、`hover`、`click`、`toggle`、`decide`、`cancel`、`skip`、`error`，以及金币控件用的 `coin` 和 `pay`。默认情况下它们播放共享声音 `Move`、`Decide`、`Cancel`、`Skip` 和 `Error`，因此用 `SHARED:SetSharedSFX` 注册了这些声音的皮肤会给每个全局界面配上声音；缺少的声音保持静音。

## derive：带有你自己外观的库

`UI.SimpleUI.derive(spec)` 返回一个新库，它拥有 SimpleUI 的控件、管理器、焦点和输入，以及自己的外观：

| `spec` 字段 | 说明 |
| --- | --- |
| `Theme` | 一个主题模块：一张其 `DEFAULT` 为完整默认主题的表（复制 SimpleUI 的 `UI.SimpleUI.theme` 再修改）。 |
| `Coins` | 可选的金币控件类，用于以你自己的方式绘制金币计数器。 |
| `name` | 库的名称。 |

```lua
-- System/My Skin/Modules/Lib/MyUI.lua
local SimpleUI = require("UI.SimpleUI")
return SimpleUI.derive{
    name  = "MyUI",
    Theme = require("MyUI.theme"),
}
```

在 `ui_library` 中指定它的名称，每个全局模块就会用它构建界面。Open-World Memories 的 `Modules/Lib/PopUI.lua` 就是这种模式：带有 Bubblegum 主题和自己金币控件的 SimpleUI。
