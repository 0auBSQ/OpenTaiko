<!-- api/input.md -->

# 输入

读取鼓面输入、键盘和鼠标，以及屏幕文本输入。

## INPUT

读取已配置的鼓面输入、原始键盘和鼠标，并创建文本输入控件。在每个脚本中可用。

<div class="callout warn">
鼓面输入名称即按键设置中的名称，不区分大小写：LRed、RRed、LBlue、RBlue（玩家 1），LRed2P ... RBlue5P（玩家 2-5），Clap、Clap2P ... Clap5P，LeftChange、RightChange、Decide、Cancel，以及系统键（Capture、SongVolumeIncrease、SongVolumeDecrease、DisplayHits、DisplayDebug、QuickConfig、SortSongs、ToggleAutoP1、ToggleAutoP2、ToggleTrainingMode、CycleVideoDisplayMode 和 Training* 系列键）。未知名称返回 false。鼓面方法读取玩家在按键设置中绑定到该输入的任何设备。INPUT 以不区分大小写的方式把键盘按键名称与引擎的按键列表匹配：字母 A-Z、数字 D0-D9、F1-F15、Space、Return、Escape、Tab、Backspace、Delete、UpArrow、DownArrow、LeftArrow、RightArrow、LeftShift、RightShift、LeftControl、RightControl、LeftAlt、RightAlt、Home、End、PageUp、PageDown、NumberPad0-9 以及其他标准按键。鼠标位置以逻辑游戏画面坐标表示，已针对黑边进行校正。
</div>

### 鼓面输入

| 方法 | 说明 |
| --- | --- |
| `INPUT:Pressed(input)  -> bool` | 输入按下的那一帧为 true。 |
| `INPUT:Pressing(input)  -> bool` | 输入按住期间为 true。 |
| `INPUT:Released(input)  -> bool` | 输入松开的那一帧为 true。 |
| `INPUT:Releasing(input)  -> bool` | 输入未按住期间为 true。 |
| `INPUT:RepeatWhilePressing(input, interval_seconds, callback)  -> nil` | 请每帧调用它。玩家按住输入期间，它每隔 `interval_seconds` 调用一次 `callback()`（第一次调用在一个间隔之后），并在玩家松开输入时停止。 |

### 键盘

| 方法 | 说明 |
| --- | --- |
| `INPUT:KeyboardPressed(key)  -> bool` | 按键按下的那一帧为 true。 |
| `INPUT:KeyboardPressing(key)  -> bool` | 按键按住期间为 true。 |
| `INPUT:KeyboardReleased(key)  -> bool` | 按键松开的那一帧为 true。 |
| `INPUT:KeyboardReleasing(key)  -> bool` | 按键未按住期间为 true。 |

### 鼠标

<div class="callout warn">
按钮名称：left、right、middle、button4、button5，或一个数字索引（不区分大小写）。位置以游戏画面坐标表示；没有鼠标设备时位置 getter 返回 -1。GetMouseDelta 和 GetScrollDelta 返回自上次调用以来的移动量并重置，因此每帧各调用一次。
</div>

| 方法 | 说明 |
| --- | --- |
| `INPUT:GetMouseX()  -> number` | 鼠标在游戏画面坐标中的 x。 |
| `INPUT:GetMouseY()  -> number` | 鼠标在游戏画面坐标中的 y。 |
| `INPUT:GetMouseXY()  -> number, number` | 以两个返回值给出鼠标的 x 和 y。 |
| `INPUT:IsMouseInside()  -> bool` | 鼠标位于渲染画面之上时为 true，位于黑边上时为 false。 |
| `INPUT:GetSurfaceWidth()  -> int` | 游戏画面的宽度（脚本绘制所用的坐标空间）。 |
| `INPUT:GetSurfaceHeight()  -> int` | 游戏画面的高度。 |
| `INPUT:MousePressed(button)  -> bool` | 按钮按下的那一帧为 true。 |
| `INPUT:MousePressing(button)  -> bool` | 按钮按住期间为 true。 |
| `INPUT:MouseReleased(button)  -> bool` | 按钮松开的那一帧为 true。 |
| `INPUT:GetMouseDelta()  -> number, number` | 自上次调用以来鼠标移动的窗口像素数，以 dx、dy 表示。 |
| `INPUT:GetScrollDelta()  -> number, number` | 自上次调用以来滚轮移动的格数，以 dx、dy 表示（向上滚动时 dy 为正）。 |
| `INPUT:SetMouseLocked(locked)  -> nil` | 锁定并隐藏光标以实现自由视角（true），或恢复光标（false），并重置增量基准。 |

```lua
function update()
    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then confirm() end
    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then goBack() end

    local mx, my = INPUT:GetMouseXY()
    hovered = mx >= bx and mx < bx + bw and my >= by and my < by + bh
    if hovered and INPUT:MousePressed("left") then confirm() end

    local _, wheel = INPUT:GetScrollDelta()
    scrollY = scrollY - wheel * 40
end
```

### 文本输入

| 方法 | 说明 |
| --- | --- |
| `INPUT:CreateTextInput(initialText, maxLength)  -> textInput` | 创建一个预填 `initialText`、长度限制为 `maxLength` 字节 UTF-8（默认 64）的文本输入控件。 |

## 文本输入句柄

由 `INPUT:CreateTextInput` 返回的屏幕文本框，收集键入的文本，包括输入法组合输入。

<div class="callout warn">
文本框激活期间每帧调用一次 Update，并自行绘制 DisplayText。同一时间只保持一个文本输入处于激活状态。在 Debug 构建中会有一个覆盖窗口同时显示该文本框；Release 构建自身不显示任何内容。在 iOS 和 Android 上游戏会打开平台原生的文本对话框，玩家确认时 Update 返回 true。
</div>

| 方法 | 说明 |
| --- | --- |
| `textInput:Update()  -> bool` | 处理本帧的键入。玩家按下回车的那一帧返回 true。 |
| `textInput.Text  -> string` | 当前文本。可读可赋值（nil 变为空字符串）。 |
| `textInput.DisplayText  -> string` | 在光标位置插入了闪烁光标的当前文本，用于绘制。 |

```lua
local field, font

function onStart()
    font = TEXT:CreateGlyphCached(28)
end

function activate()
    field = INPUT:CreateTextInput("", 32)
end

function update()
    if field:Update() then
        saveName(field.Text)
    elseif INPUT:KeyboardPressed("Escape") then
        field.Text = ""
    end
end

function draw()
    font:Draw(field.DisplayText, 100, 100)
end

function onDestroy()
    font:Dispose()
end
```
