<!-- api/graphics.md -->

# 图形与文本

纹理、画布、裁剪、文本、视频、颜色、渐变映射和尺寸。所有位置和尺寸都以逻辑屏幕像素为单位，即脚本绘制所用的坐标空间（见 `INPUT:GetSurfaceWidth()` / `GetSurfaceHeight()`）。

创建纹理、画布、文本、视频或渐变句柄的脚本拥有该句柄，游戏在卸载该脚本时释放句柄。要提前释放某个句柄，请自行调用 `Dispose()`。

## 纹理

### TEXTURE

把图像文件加载为纹理句柄。

<div class="callout warn">
相对路径相对于脚本目录解析；FromAbsolutePath 变体接受完整路径。文件缺失时返回一个什么都不绘制的空句柄。CreateTexture 异步加载：在后台解码和上传完成之前，句柄不绘制任何内容，且 Width 和 Height 报告为 0。你需要立即获得尺寸或像素时请使用 CreateTextureSync。选项表接受 { maxSize = N }，在解码时缩小图像，使其最长边不超过 N 像素。
</div>

| 方法 | 说明 |
| --- | --- |
| `TEXTURE:CreateTexture()  -> texture` | 创建一个没有图像的空句柄。 |
| `TEXTURE:CreateTexture(path)  -> texture` | 从相对于脚本目录的路径异步加载图像。 |
| `TEXTURE:CreateTexture(path, options)  -> texture` | 同上，附带选项表（`{ maxSize = N }`）。 |
| `TEXTURE:CreateTextureSync(path)  -> texture` | 同步加载图像；返回时尺寸和像素均已可用。 |
| `TEXTURE:CreateTextureFromAbsolutePath(path)  -> texture` | 从完整的文件系统路径加载图像。 |
| `TEXTURE:CreateTextureFromAbsolutePath(path, options)  -> texture` | 同上，附带选项表（`{ maxSize = N }`）。 |
| `TEXTURE:Exists(path)  -> bool` | 返回相对于脚本目录的路径上是否存在文件。 |

```lua
local logo, jacket

function onStart()
    logo = TEXTURE:CreateTexture("Textures/Logo.png")
    jacket = TEXTURE:CreateTexture("Textures/Jacket.png", { maxSize = 512 })
end

function draw()
    logo:DrawAtAnchor(960, 540, "center")
end

function onDestroy()
    logo:Dispose()
    jacket:Dispose()
end
```

### 纹理句柄

由 `TEXTURE` 工厂、`text:GetText` / `GetVerticalText` 以及 `video.Texture` 返回的可绘制 2D 图像。

<div class="callout warn">
锚点名称：topleft、top、topright、left、center、right、bottomleft、bottom、bottomright（不区分大小写；未知名称回退为 topleft）。带锚点的绘制会考虑当前缩放。混合模式名称：Normal、Add、Multi、Sub、Screen。环绕模式名称：Edge、Border、Repeat、Mirror；新句柄默认为 Repeat。在空句柄上每个方法都是空操作，getter 返回下面列出的默认值。
</div>

| 方法 | 说明 |
| --- | --- |
| `texture:Draw(x, y)  -> nil` | 以 (x, y) 为左上角绘制整张纹理。 |
| `texture:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | 以 (x, y) 为左上角绘制源子矩形（以纹理像素为单位）。 |
| `texture:DrawAtAnchor(x, y, anchor)  -> nil` | 绘制整张纹理，使指定锚点落在 (x, y)。 |
| `texture:DrawRectAtAnchor(x, y, rect_x, rect_y, rect_width, rect_height, anchor)  -> nil` | 绘制源子矩形，使指定锚点落在 (x, y)。 |
| `texture.Loaded  -> bool` | 句柄包装了一张纹理时为 true。游戏找到文件后立即置位，早于异步加载完成。 |
| `texture.Width  -> int` | 像素宽度；空句柄为 -1，异步加载未完成时为 0。 |
| `texture.Height  -> int` | 像素高度；空句柄为 -1，异步加载未完成时为 0。 |
| `texture.Pointer  -> int` | 原生 GL 纹理 id；没有时为 0。 |
| `texture:GetScale()  -> vector2` | 当前绘制缩放，以 vector2（`X`、`Y`）表示。 |
| `texture:GetOpacity()  -> number` | 当前不透明度，0..1；空句柄为 -1。 |
| `texture:GetColor()  -> number, number, number` | 当前着色，以三个 0..1 的值表示（红、绿、蓝）。 |
| `texture:GetRotation()  -> number` | 当前旋转角度（度）。 |
| `texture:GetBlendMode()  -> string` | 当前混合模式名称。 |
| `texture:GetWrapMode()  -> string` | 当前环绕模式名称。 |
| `texture:SetScale(scale_x, scale_y)  -> nil` | 设置水平和垂直绘制缩放（1 = 原始尺寸）。 |
| `texture:SetOpacity(opacity)  -> nil` | 设置不透明度，0..1。 |
| `texture:SetColor(color)  -> nil` | 从 `COLOR` 值设置着色。句柄会忽略颜色的 alpha；请用 SetOpacity 设置透明度。 |
| `texture:SetColor(red, green, blue)  -> nil` | 从三个 0..1 的值设置着色。 |
| `texture:SetRotation(angle)  -> nil` | 设置绕纹理中心的旋转角度（度）。 |
| `texture:SetBlendMode(mode)  -> nil` | 按名称设置混合模式（不区分大小写）。句柄会忽略未知名称。 |
| `texture:SetWrapMode(mode)  -> nil` | 按名称设置环绕模式（不区分大小写）。句柄会忽略未知名称。 |
| `texture:SetUseNoiseEffect(enabled)  -> nil` | 启用时，每次绘制都会把纹理的颜色替换为动态的随机灰度噪点，并保留其 alpha。 |
| `texture:SetGradientMap(gradient)  -> nil` | 对该纹理的每次绘制应用一个 `GRADIENT` 映射（见 GRADIENT）。 |
| `texture:ClearGradientMap()  -> nil` | 移除该纹理的渐变映射。 |
| `texture:Dispose()  -> nil` | 释放纹理。 |

## 画布与裁剪

### CANVAS

创建可写的像素表面，由 Lua 在 CPU 上编辑并作为一张纹理上传。

<div class="callout warn">
画布是一张由 Lua 设置像素（SetPixel、FillRect 等）然后通过 Upload 推送到 GPU 的纹理。它适合软件渲染和烘焙 UI 形状：脚本绘制并上传一次，之后每帧绘制结果。新画布初始为完全透明。
</div>

| 方法 | 说明 |
| --- | --- |
| `CANVAS:CreateCanvas(width, height)  -> canvas` | 创建指定尺寸的透明画布（最小 1x1）。 |

```lua
local panel

function onStart()
    panel = CANVAS:CreateCanvas(300, 80)
    panel:FillRect(0, 0, 300, 80, 20, 20, 40, 220)
    panel:FillCircle(40, 40, 24, 255, 200, 0, 255)
    panel:Upload()
end

function draw()
    panel:Draw(100, 100)
end

function onDestroy()
    panel:Dispose()
end
```

### 画布句柄

由 `CANVAS:CreateCanvas` 返回的可写 RGBA 像素缓冲区，可以像纹理一样绘制。

<div class="callout warn">
颜色参数 r、g、b、a 为 0-255 的整数。像素编辑会累积在一个脏矩形中，只有在你调用 Upload 时才到达 GPU（BlitPacked 会自行上传）。绘制坐标为整数。锚点名称与纹理句柄相同。
</div>

| 方法 | 说明 |
| --- | --- |
| `canvas.Width  -> int` | 像素宽度。 |
| `canvas.Height  -> int` | 像素高度。 |
| `canvas.Pointer  -> int` | 原生 GL 纹理 id；没有时为 0。 |
| `canvas:SetPixel(x, y, r, g, b, a)  -> nil` | 设置一个像素。画布会忽略超出范围的坐标。 |
| `canvas:FillRect(x, y, w, h, r, g, b, a)  -> nil` | 填充一个轴对齐矩形，裁剪到画布范围内。 |
| `canvas:FillCircle(cx, cy, radius, r, g, b, a)  -> nil` | 填充一个给定像素半径的圆盘。 |
| `canvas:StrokeLine(x0, y0, x1, y1, radius, r, g, b, a)  -> nil` | 用给定半径的重叠圆盘绘制一条粗线。 |
| `canvas:PasteTexture(texture, x, y)  -> nil` | 以 (x, y) 为左上角把一张纹理以 alpha 混合方式贴到画布上。画布对每个纹理句柄只从 GPU 回读一次像素，这是一个缓慢的操作，应放在初始化代码中。 |
| `canvas:PasteTextureTransformed(texture, x, y, scale, rotationDeg, anchor)  -> nil` | 以 alpha 混合方式贴上一张按 `scale` 缩放、按 `rotationDeg` 顺时针旋转的纹理，使用最近邻采样。anchor 为 "center" 时纹理中心落在 (x, y)；其他任何值都把其左上角放在那里。 |
| `canvas:Clear(r, g, b, a)  -> nil` | 用一种颜色填充整张画布。 |
| `canvas:ClearTransparent()  -> nil` | 把整张画布重置为完全透明。 |
| `canvas:CopyFrom(other)  -> nil` | 把另一张相同尺寸画布的像素复制到本画布。尺寸不同时该调用不做任何事。 |
| `canvas:BlitPacked(data, count)  -> nil` | 用一个以 1 起始、按行主序排列、打包为 `0xRRGGBB` 整数的 Lua 数组填充表面（像素变为不透明；负值变为透明像素），然后上传。 |
| `canvas:Upload()  -> nil` | 把待处理的编辑（仅变更区域）推送到 GPU。没有变更时为空操作。 |
| `canvas:Draw(x, y)  -> nil` | 以 (x, y) 为左上角绘制画布。 |
| `canvas:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | 以 (x, y) 为左上角绘制源子矩形（以画布像素为单位）。 |
| `canvas:DrawAtAnchor(x, y, anchor)  -> nil` | 绘制画布，使指定锚点落在 (x, y)。 |
| `canvas:SetScale(scale_x, scale_y)  -> nil` | 设置水平和垂直绘制缩放。 |
| `canvas:SetOpacity(opacity)  -> nil` | 设置不透明度，0..1。 |
| `canvas:SetColor(red, green, blue)  -> nil` | 从三个 0..1 的值设置着色。 |
| `canvas:Dispose()  -> nil` | 释放画布的 GPU 纹理和 CPU 缓冲区。 |

### GRAPHICS

用于滚动面板的裁剪（scissor）。

<div class="callout warn">
SetClip 接受逻辑屏幕坐标并映射到当前视口，因此在渲染缩放和黑边下裁剪效果一致。SetClip 和 ClearClip 之间绘制在矩形之外的每个像素都会被 GPU 丢弃。SetClip 会替换之前的矩形（没有栈）；请让 SetClip 和 ClearClip 成对出现。游戏在每帧开始时禁用裁剪。
</div>

| 方法 | 说明 |
| --- | --- |
| `GRAPHICS:SetClip(x, y, w, h)  -> nil` | 启用对给定矩形的裁剪。 |
| `GRAPHICS:ClearClip()  -> nil` | 禁用裁剪。 |

```lua
GRAPHICS:SetClip(100, 200, 600, 400)
for i, row in ipairs(rows) do
    row.tex:Draw(100, 200 + (i - 1) * 48 - scrollY)
end
GRAPHICS:ClearClip()
```

## 文本

### TEXT

为皮肤的主字体创建字体渲染器。

<div class="callout warn">
Create 返回一个把整个字符串渲染为缓存纹理的文本句柄；用于很少变化的标签。CreateGlyphCached 返回一个字形文本句柄，为每个字符缓存一张纹理并在绘制时组合字符串；用于经常变化的文本（计时器、分数、输入内容）和自动换行的文本块。样式参数可以是 bold、italic、underline、strikeout 中的任意几个（不区分大小写；其他值表示常规）。在同一脚本中，如果脚本先不带样式参数调用某个工厂方法，之后又恰好带一个样式参数调用同一方法，自带的 NLua 版本会出错。要么从不传样式，要么从第一次调用起就始终传样式，要么传两个样式标记（例如 "bold"、"regular"）。
</div>

| 方法 | 说明 |
| --- | --- |
| `TEXT:Create(size, ...style)  -> text` | 创建一个指定像素大小的整串文本渲染器。 |
| `TEXT:CreateGlyphCached(size, ...style)  -> glyphText` | 创建一个指定像素大小的字形组合文本渲染器。 |

#### 内联颜色标签

两种渲染器都识别字符串中的这些标签。标签可以嵌套；未闭合的标签一直作用到字符串末尾。测量函数会忽略它们。

| 标签 | 效果 |
| --- | --- |
| `<c.#rrggbb>` ... `</c>` | 填充颜色。 |
| `<c.#rrggbb.#rrggbb>` ... `</c>` | 填充颜色和描边颜色。 |
| `<g.#rrggbb.#rrggbb>` ... `</g>` | 垂直渐变填充（顶部颜色、底部颜色）。 |

### 文本句柄

由 `TEXT:Create` 返回的整串渲染器。

<div class="callout warn">
GetText 和 GetVerticalText 为字符串、填充颜色、描边颜色和尺寸上限的每个不同组合缓存一张纹理，并保留到你释放句柄为止。因此每帧变化的字符串会每帧新增一张纹理并泄漏 GPU 内存；这类文本请用 DrawDirect 或字形文本句柄绘制。forecolor 和 backcolor 是 COLOR 值；默认是白色填充和不透明的黑色描边。centered 标志使多行字符串的每一行居中；缓存键不包含它，因此由给定字符串的第一次调用决定。
</div>

| 方法 | 说明 |
| --- | --- |
| `text:GetText(text, centered, max_width, forecolor, backcolor)  -> texture` | 把一个横排字符串渲染为缓存纹理。`text` 之后的所有参数都是可选的。渲染出的纹理宽于 `max_width` 时，它会横向压缩绘制以适配。 |
| `text:GetVerticalText(text, centered, max_height, forecolor, backcolor)  -> texture` | 把一个竖排字符串渲染为缓存纹理。渲染出的纹理高于 `max_height` 时，它会纵向压缩绘制以适配。 |
| `text:DrawDirect(text, x, y, r, g, b, opacity, spacing)  -> number` | 从逐字符缓存中逐字形绘制字符串，并返回最后一个字形之后的 x 位置。`r`、`g`、`b` 为 0-255（默认 255），`opacity` 为 0..1（默认 1），`spacing` 是字形之间的像素间距（默认 -6）。 |
| `text:Dispose()  -> nil` | 释放字体和所有缓存的纹理。 |

```lua
local font, title

function onStart()
    font = TEXT:Create(28)
    title = font:GetText("Song select", true, 600)
end

function draw()
    title:DrawAtAnchor(960, 80, "center")
    font:DrawDirect(tostring(score), 40, 40, 255, 255, 0)
end

function onDestroy()
    font:Dispose()
end
```

### 字形文本句柄

由 `TEXT:CreateGlyphCached` 返回的字形组合渲染器。

<div class="callout warn">
forecolor 和 backcolor 是 COLOR 值；默认是白色填充和黑色描边。位置之后的每个参数都是可选的。maxWidth（0 = 不限制）横向压缩每个字形，使整行适配。anchor 使用与纹理句柄相同的九个名称。'\n' 开始新的一行；Draw 将各行左对齐堆叠。Draw 返回绘制框右边缘的 x 坐标。框的几何与 GetText 纹理一致（墨迹的两侧和下方各有 25 像素的内边距），因此在同一位置绘制的字形文本和 GetText 纹理能够对齐。
</div>

| 方法 | 说明 |
| --- | --- |
| `glyphText.LineHeight  -> number` | 多行文本的行距（像素）。 |
| `glyphText.BoxHeight  -> number` | 单行框的高度（墨迹加内边距），与单行 GetText 纹理一致。 |
| `glyphText:Measure(text, scale)  -> number` | 最宽一行的墨迹宽度，乘以 `scale`（默认 1）。 |
| `glyphText:Draw(text, x, y, forecolor, backcolor, opacity, scale, maxWidth, anchor, scaleY, rotationDeg)  -> number` | 绘制文本。`opacity` 为 0..1（默认 1）；`scale`（默认 1）；`scaleY` 大于 0 时覆盖垂直缩放；`rotationDeg` 使整块文本绕 (x, y) 旋转。返回框右边缘的 x。 |
| `glyphText:SetClipY(y0, y1)  -> nil` | 把之后的正向 Draw 调用限制在屏幕空间的垂直区间 y0..y1 内；Draw 会跳过区间外的字形，并切割区间边缘上的字形。传入 y1 <= y0 可清除。旋转的绘制会忽略该区间。 |
| `glyphText:WrapToLines(text, wrapWidth, scale)  -> string[]` | 按 `wrapWidth` 自动换行，并以纯字符串形式返回各行（标签已去除）。 |
| `glyphText:MeasureWrapped(text, wrapWidth, scale, lineSpacing)  -> number` | DrawWrapped 将占用的高度，但不绘制。`lineSpacing` 乘以行距（默认 1）。 |
| `glyphText:DrawWrapped(text, x, y, wrapWidth, forecolor, backcolor, opacity, scale, lineSpacing)  -> number` | 按 `wrapWidth` 自动换行，并从 (x, y) 开始左对齐绘制。返回绘制的高度。 |
| `glyphText:Dispose()  -> nil` | 释放字体和所有缓存的字形。 |

自动换行在空格处、CJK 字符之间，以及宽于换行宽度的单词内部断行。

```lua
local font, white

function onStart()
    font = TEXT:CreateGlyphCached(24)
    white = COLOR:CreateColorFromRGBA(255, 255, 255)
end

function draw()
    font:Draw("Time " .. string.format("%.1f", t), 960, 40, white, nil, 1, 1, 0, "center")
    font:DrawWrapped(description, 200, 300, 800, white)
end

function onDestroy()
    font:Dispose()
end
```

## 视频

### VIDEO

把视频文件加载为可播放的解码器句柄。

<div class="callout warn">
路径相对于脚本目录。解码器在后台线程中打开；就绪之前句柄不绘制任何内容，会把 Start、跳转和速度调用排队，并在解码器附加后应用它们。文件缺失时返回空句柄。
</div>

| 方法 | 说明 |
| --- | --- |
| `VIDEO:CreateVideo(path)  -> video` | 打开一个视频文件并返回其句柄。 |

### 视频句柄

由 `VIDEO:CreateVideo` 返回的视频解码器。

<div class="callout warn">
位置以毫秒计；Duration 以秒计。每帧读取 Texture 以获得作为纹理句柄的当前帧。该句柄包装了视频自己的帧纹理：绘制它即可，释放交给视频句柄处理。SetSpeed 会忽略 0 或以下的值。
</div>

| 方法 | 说明 |
| --- | --- |
| `video:Start()  -> nil` | 开始播放。 |
| `video:Resume()  -> nil` | 在 Pause 之后恢复播放。 |
| `video:Pause()  -> nil` | 暂停播放。 |
| `video:Stop()  -> nil` | 停止播放。 |
| `video:Reset()  -> nil` | 跳回开头。 |
| `video.Width  -> int` | 帧宽度（像素）；解码器就绪前为 -1。 |
| `video.Height  -> int` | 帧高度（像素）；解码器就绪前为 -1。 |
| `video.Duration  -> number` | 总时长（秒）（解码器就绪前为 1）。 |
| `video.DurationMs  -> number` | 总时长（毫秒）。 |
| `video.Texture  -> texture` | 当前解码出的帧。 |
| `video:IsFinished()  -> bool` | 流已结束且没有剩余帧时为 true。 |
| `video:GetTimestampMs()  -> number` | 当前播放位置（毫秒）。 |
| `video:SetTimestampMs(ms)  -> nil` | 跳转到以毫秒计的位置。 |
| `video:GetSpeed()  -> number` | 当前播放速度倍率（1 = 正常）。 |
| `video:SetSpeed(speed)  -> nil` | 设置播放速度倍率。 |
| `video:GetPlayPosition()  -> number` | 当前位置（秒）（GetTimestampMs / 1000 的旧名称）。 |
| `video:SetPlayPosition(seconds)  -> nil` | 跳转到以秒计的位置（SetTimestampMs 的旧名称）。 |
| `video:GetPlaySpeed()  -> number` | GetSpeed 的旧名称。 |
| `video:SetPlaySpeed(speed)  -> nil` | SetSpeed 的旧名称。 |
| `video:Dispose()  -> nil` | 释放解码器及其帧纹理。 |

```lua
local intro

function onStart()
    intro = VIDEO:CreateVideo("Videos/intro.mp4")
end

function activate()
    intro:Start()
end

function draw()
    intro.Texture:Draw(0, 0)
    if intro:IsFinished() then Exit("title", nil) end
end

function deactivate()
    intro:Stop()
end

function onDestroy()
    intro:Dispose()
end
```

## 颜色、渐变与尺寸

### COLOR

创建颜色值。

<div class="callout warn">
通道为 0-255。接受颜色的纹理、画布和文本方法都接受这些值。
</div>

| 方法 | 说明 |
| --- | --- |
| `COLOR:CreateColorFromRGBA(r, g, b, a)  -> color` | 从红、绿、蓝和可选的 alpha（默认 255）创建颜色。 |
| `COLOR:CreateColorFromARGB(a, r, g, b)  -> color` | 从 alpha、红、绿、蓝创建颜色。 |
| `COLOR:CreateColorFromHex(value)  -> color` | 解析不带前缀的 `AARRGGBB` 十六进制字符串（例如 `"ff2080ff"`）。六位字符串得到 alpha 为 0。无法解析的字符串得到不透明白色。 |

### 颜色句柄

由 `COLOR` 工厂返回的颜色值。

| 方法 | 说明 |
| --- | --- |
| `color.R  -> int` | 红色通道，0-255（可读写）。 |
| `color.G  -> int` | 绿色通道，0-255（可读写）。 |
| `color.B  -> int` | 蓝色通道，0-255（可读写）。 |
| `color.A  -> int` | Alpha 通道，0-255（可读写）。 |

### GRADIENT

渐变映射通过把每个像素的亮度映射到一条颜色渐变带来为纹理绘制重新着色，可作用于单张纹理或所有绘制。

<div class="callout warn">
Create 接受一张至少包含两个色标的表，每个色标是数组 { position 0-1, r 0-255, g 0-255, b 0-255 [, a 0-255] }，以及一个混合强度（0 = 无效果，1 = 完全替换，中间值为混合；默认 1）。少于两个色标会抛出错误。相同的色标和混合强度在整个程序中复用同一张缓存的 GPU 纹理。通过 texture:SetGradientMap 应用到单张纹理，或通过 SetActive 应用到所有纹理绘制。
</div>

| 方法 | 说明 |
| --- | --- |
| `GRADIENT:Create(stops, blend)  -> gradient` | 从色标和混合强度构建（或复用）一个渐变映射。 |
| `GRADIENT:SetActive(gradient)  -> nil` | 对所有纹理绘制应用该渐变，直到 ClearActive。 |
| `GRADIENT:ClearActive()  -> nil` | 移除全局渐变映射。 |

```lua
local sepia

function onStart()
    sepia = GRADIENT:Create({
        { 0.0,  40,  20,   0 },
        { 1.0, 255, 230, 180 },
    }, 0.8)
end

function draw()
    GRADIENT:SetActive(sepia)
    background:Draw(0, 0)
    GRADIENT:ClearActive()
end

function onDestroy()
    sepia:Dispose()
end
```

### 渐变句柄

由 `GRADIENT:Create` 返回的渐变映射。

| 方法 | 说明 |
| --- | --- |
| `gradient.BlendStrength  -> number` | 混合强度，0..1（只读）。 |
| `gradient:Dispose()  -> nil` | 释放句柄。共享的 GPU 纹理仍保留在缓存中。 |

### SIZE

创建宽度/高度值对象。

| 方法 | 说明 |
| --- | --- |
| `SIZE:CreateSize(width, height)  -> size` | 创建一个尺寸对象。 |

### 尺寸句柄

由 `SIZE:CreateSize` 返回的宽高对。

| 方法 | 说明 |
| --- | --- |
| `size.Width  -> int` | 宽度（可读写）。 |
| `size.Height  -> int` | 高度（可读写）。 |
