<!-- api/aviutl2.md -->

# AviUtl2 项目

`AVIUTL2` 全局对象播放由 AviUtl2 视频编辑器保存的项目文件（`.aup2`）。游戏读取项目，并实时绘制其中的对象、移动和效果，因此皮肤可以用项目及其图片来附带一段动画，代替渲染好的视频。

加载项目的脚本拥有该项目的句柄，游戏在卸载该脚本时释放句柄。要提前释放句柄，请自行调用 `Dispose()`。

<div class="callout warn">
游戏用自己的渲染器绘制项目。位置、尺寸、旋转、移动曲线、摄像机控制和常用效果与编辑器一致。随机移动和抖动使用不同的随机值，部分效果是近似实现。加载项目后，请读取 GetUnsupported 和 GetApproximated，查看游戏略去或简化了哪些内容。
</div>

## 加载

### AVIUTL2

| 方法 | 说明 |
| --- | --- |
| `AVIUTL2:LoadProject(path)  -> project` | 打开一个项目文件并返回其句柄。路径相对于脚本目录。 |
| `AVIUTL2:LoadProjectFromAbsolutePath(path)  -> project` | 从完整路径打开项目文件。 |

<div class="callout warn">
项目里保存的是保存它的那台机器上的完整路径。游戏在项目文件附近查找每个图片、视频、声音和模型：先按保存的路径与项目自身路径共有的文件夹查找，再按文件名查找。请把项目的文件放在项目所在的文件夹或其子文件夹中。项目缺失或无法读取时，返回的句柄的 Loaded 为 false。
</div>

## 项目句柄

由 `AVIUTL2:LoadProject` 返回的项目。它的播放方法与视频句柄的方法同名。

### 播放

| 方法 | 说明 |
| --- | --- |
| `project:Start()  -> nil` | 从开头开始播放。 |
| `project:Resume()  -> nil` | 在 Pause 之后恢复播放。 |
| `project:Pause()  -> nil` | 暂停播放，声音也一并暂停。 |
| `project:Stop()  -> nil` | 停止播放。 |
| `project:Reset()  -> nil` | 跳回开头。 |
| `project.IsPlaying  -> bool` | 项目正在播放且尚未到达结尾时为 true。 |
| `project:IsFinished()  -> bool` | 不循环的项目到达结尾后为 true。 |
| `project:GetTimestampMs()  -> number` | 当前位置（毫秒）。 |
| `project:SetTimestampMs(ms)  -> nil` | 跳转到以毫秒计的位置。 |
| `project:GetFrame()  -> number` | 当前位置（以项目的帧计）。 |
| `project:SetFrame(frame)  -> nil` | 跳转到指定的帧。 |
| `project:GetSpeed()  -> number` | 当前播放速度倍率（1 = 正常）。 |
| `project:SetSpeed(speed)  -> nil` | 设置播放速度倍率。 |
| `project:GetLoop()  -> bool` | 项目会在结尾处从头再播时为 true。 |
| `project:SetLoop(loop)  -> nil` | 让项目在结尾处从头再播。 |
| `project:GetVolumePercent()  -> number` | 项目声音的音量（百分比）。 |
| `project:SetVolumePercent(vol)  -> nil` | 以百分比设置项目声音的音量。 |
| `project:Dispose()  -> nil` | 释放项目及其图片、视频和声音。 |

### 绘制

| 方法 | 说明 |
| --- | --- |
| `project:Draw(x, y)  -> nil` | 绘制当前帧，左上角位于 (x, y)，使用 SetScale 设置的缩放。 |
| `project:DrawFit(x, y, w, h)  -> nil` | 在保持宽高比的前提下，以矩形能容纳的最大尺寸绘制当前帧，并在矩形内居中。 |
| `project:SetScale(scaleX, scaleY)  -> nil` | 设置 Draw 使用的缩放（1 = 项目本身的尺寸）。 |
| `project:SetOpacity(opacity)  -> nil` | 设置整帧的不透明度，范围为 0 到 1。 |
| `project:SetBackground(r, g, b, a)  -> nil` | 用一种颜色填充对象后面的画面，每个值的范围为 0 到 255。alpha 为 0 时画面保持透明。 |
| `project:ClearBackground()  -> nil` | 恢复为项目文件指定的背景。 |
| `project.HasProjectBackground  -> bool` | 场景有自己的背景色时为 true。 |
| `project.ProjectBackground  -> int` | 场景的背景色，格式为 0xRRGGBB；没有背景色时为 -1。 |

<div class="callout warn">
没有背景色的场景在未绘制对象的地方是透明的，因此项目会绘制在脚本此前绘制的内容之上。编辑器把这样的场景显示在黑色背景上。要得到相同的画面，请先绘制一个黑色矩形，或调用 SetBackground(0, 0, 0, 255)。
</div>

### 由脚本绘制的图片

脚本可以用自己的绘制内容替换项目中的某张图片，例如在带动画的边框里显示玩家的名牌。

| 方法 | 说明 |
| --- | --- |
| `project:SetImageDrawer(fileName, width, height, draw)  -> nil` | 所有显示图片文件 `fileName` 的对象都改为显示函数 `draw` 绘制的内容。游戏每帧调用 `draw`，绘制区域的大小为 `width` × `height`，其左上角为 (0, 0)。 |
| `project:ClearImageDrawer(fileName)  -> nil` | 这些对象重新显示图片文件。 |

对于每个包含这类对象的图层，游戏调用一次 `draw(rank, layer)`。`rank` 从最上面的图层开始，从 0 起为这些图层编号，`layer` 是该图层在项目中的编号。在 `draw` 内使用平常的绘制调用即可：游戏把它们重定向到这张图片中，图片随后跟着对象一起移动、缩放和淡入淡出。

<div class="callout warn">
该函数必须属于调用 SetImageDrawer 的脚本。在 `draw` 内绘制的柔和边缘会比直接绘制在屏幕上时略细。
</div>

### 读取项目

| 方法 | 说明 |
| --- | --- |
| `project.Loaded  -> bool` | 文件已读取时为 true。 |
| `project.Width  -> int` | 场景宽度（像素）；未加载任何内容时为 -1。 |
| `project.Height  -> int` | 场景高度（像素）；未加载任何内容时为 -1。 |
| `project.FrameRate  -> number` | 场景的每秒帧数。 |
| `project.FrameCount  -> int` | 场景长度（帧）。 |
| `project.DurationMs  -> number` | 场景长度（毫秒）。 |
| `project.SceneCount  -> int` | 项目中的场景数量。 |
| `project:GetScene()  -> int` | 正在播放的场景的索引。 |
| `project:SetScene(index)  -> nil` | 从头播放项目的另一个场景。 |
| `project:GetSceneName(index)  -> string` | 场景的名称。 |
| `project.ObjectCount  -> int` | 场景中的对象数量。 |
| `project:GetProjectValue(key)  -> string` | 项目的一项设置，内容与文件中所写一致。 |
| `project:GetSceneValue(key)  -> string` | 正在播放的场景的一项设置。 |
| `project:GetValue(section, key)  -> string` | 文件中任意区段的一项设置，按区段名称指定（对象为 `"12"`，它的第二个效果为 `"12.1"`）。 |
| `project:GetValueAtFrame(objectId, effect, key, frame)  -> number` | 对象某个效果的一项数值设置在某一帧的值，已应用其移动。 |

### 检查绘制内容

| 方法 | 说明 |
| --- | --- |
| `project.UnsupportedCount  -> int` | 项目用到而游戏不绘制的内容数量，包括缺失的文件。 |
| `project:GetUnsupported()  -> string` | 这些内容的列表，每行一项。 |
| `project.ApproximatedCount  -> int` | 游戏以简化方式绘制的内容数量。 |
| `project:GetApproximated()  -> string` | 这些内容的列表，每行一项。 |
| `project:AddScriptFolder(path)  -> nil` | 添加一个存放 AviUtl 脚本文件的文件夹，项目的效果可以按名称引用其中的脚本。游戏会自动读取项目文件旁边的 `Script` 文件夹。 |
| `project.ScriptErrorCount  -> int` | 执行失败的脚本文件数量。 |
| `project:GetScriptErrors()  -> string` | 每个失败脚本的第一条错误，每行一项。 |
| `project.GraphicsMemoryMB  -> number` | 项目当前为自身绘制占用的图形内存（MB）。 |
| `project:SetSurfaceBudgetMB(mb)  -> nil` | 设置项目为在帧之间复用而保留的图形内存量。 |

<div class="callout warn">
随项目附带的脚本文件在封闭环境中运行：它们无法读写文件、启动程序或加载原生模块。
</div>

```lua
local intro
local box

function onStart()
    intro = AVIUTL2:LoadProject("Intro/intro.aup2")
    box = TEXTURE:CreateTexture("Intro/player_box.png")
    -- 每位玩家的名牌，绘制到项目的玩家框中
    intro:SetImageDrawer("player_box.png", 520, 216, function(rank)
        box:Draw(0, 0)
        NAMEPLATE:DrawPlayerNameplate(170, 88, 255, rank)
    end)
end

function activate()
    intro:SetBackground(0, 0, 0, 255)
    intro:Start()
end

function draw()
    intro:DrawFit(0, 0, 1920, 1080)
    if intro:IsFinished() then Exit("title", nil) end
end

function deactivate()
    intro:Stop()
end

function onDestroy()
    intro:Dispose()
    box:Dispose()
end
```
