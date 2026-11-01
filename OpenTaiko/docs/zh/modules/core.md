<!-- modules/core.md -->

# Core

没有自己界面的小型辅助工具：缓动曲线、颜色计算、表工具、JSON 编码器和解码器，以及当前文件所在的文件夹。它们在加载时不触碰任何引擎状态，因此任何模块（包括只读模块）都可以 require 它们。

## Core.Easing

作用于 0 到 1 比例的缓动曲线，用于由你自己的时钟（时间轴、粒子的存活时间）驱动的动画。对于由引擎时钟驱动的补间，也可以选择 `COUNTER` 全局对象的 SetEasing。除 `hump` 外，每条曲线都把输入限制在 0..1，并把 0 映射到 0、1 映射到 1。

| 函数 | 说明 |
| --- | --- |
| `Easing.linear(t)  -> number` | 限制后的比例本身。 |
| `Easing.inQuad(t)`, `outQuad`, `inOutQuad`, `inCubic`, `outCubic`, `inOutCubic`, `inSine`, `outSine`, `inOutSine`, `inExpo`, `outExpo`  `-> number` | 常见曲线，按名称区分。 |
| `Easing.inBack(t, s?)`, `Easing.outBack(t, s?)  -> number` | 会冲过头的曲线；`s` 是冲过头的量（默认 1.7）。 |
| `Easing.inBounce(t)`, `Easing.outBounce(t)`, `Easing.outElastic(t)  -> number` | 弹跳和弹性曲线。 |
| `Easing.hump(t)  -> number` | 从 0 在中点升到 1，再回到 0：用于挤压和闪光。 |
| `Easing.span(t, t0, dur)  -> number` | 从 `t0` 开始、持续 `dur` 的窗口的进度，限制在 0..1。 |
| `Easing.lerp(a, b, k)  -> number` | `a + (b - a) * k`. |
| `Easing.clamp01(t)  -> number` | 限制在 0..1 的 `t`。 |
| `Easing.get(kind, fn)  -> function` | 按引擎缓动名称取得的曲线：`kind` 为 `"IN"`、`"OUT"` 或 `"INOUT"`，`fn` 为 `"LINEAR"`、`"SINE"`、`"QUAD"`、`"CUBIC"`、`"EXPO"`、`"BACK"`、`"BOUNCE"` 或 `"ELASTIC"`。没有对应曲线的组合会得到 QUAD 曲线。 |

```lua
local Easing = require("Core.Easing")
local k = Easing.outBack(Easing.span(t, startTime, 0.4))
x = Easing.lerp(x0, x1, k)
```

## Core.Color

颜色转换与调整。每个通道都是 0 到 1 的浮点数，色相也不例外（0 到 1 绕色轮一周）；函数接收并返回分开的 `r, g, b` 值。

| 函数 | 说明 |
| --- | --- |
| `Color.rgbToHsv(r, g, b)  -> h, s, v` | RGB 转 HSV。 |
| `Color.hsvToRgb(h, s, v)  -> r, g, b` | HSV 转 RGB。 |
| `Color.rgbToHsl(r, g, b)  -> h, s, l` | RGB 转 HSL。 |
| `Color.hslToRgb(h, s, l)  -> r, g, b` | HSL 转 RGB。 |
| `Color.mix(r, g, b, r2, g2, b2, t)  -> r, g, b` | 线性混合：`t` = 0 保持第一个颜色，1 得到第二个颜色。 |
| `Color.lighten(r, g, b, amount)  -> r, g, b` | 把 HSL 亮度按剩余范围的 `amount` 比例移向白色（1 = 白色）；色相和饱和度不变。 |
| `Color.darken(r, g, b, amount)  -> r, g, b` | 把 HSL 亮度按 `amount` 移向黑色（1 = 黑色）。 |
| `Color.saturate(r, g, b, factor, add?)  -> r, g, b` | 把 HSV 饱和度乘以 `factor`，再加上 `add`，并限制到 1。 |
| `Color.brighten(r, g, b, factor)  -> r, g, b` | 把 HSV 明度乘以 `factor`，并限制到 1。 |
| `Color.ramp(from, to, n)  -> table` | 从 `{r, g, b}` 到 `{r, g, b}` 逐步变化的 `n` 个颜色，以 `{r, g, b}` 列表的形式返回。 |
| `Color.fromEngine(col)  -> r, g, b, a` | `COLOR` 对象的各个通道，以浮点数返回。 |
| `Color.toEngine(r, g, b, a?)  -> color` | 由浮点数构成的 `COLOR` 对象（alpha 默认为 1）。 |
| `Color.rampEngine(fromCol, toCol, n)  -> table` | 两个 `COLOR` 对象之间由 `n` 个 `COLOR` 对象组成的渐变。 |
| `Color.clamp01(v)  -> number` | 限制在 0..1 的 `v`。 |

## Core.Util

| 函数 | 说明 |
| --- | --- |
| `Util.clamp(v, lo, hi)  -> number` | 保持在 `lo` 与 `hi` 之间的 `v`。 |
| `Util.lerp(a, b, t)  -> number` | 线性插值。 |
| `Util.round(v)  -> number` | 四舍五入到最接近的整数（0.5 向上取）。 |
| `Util.deepcopy(o)  -> value` | Lua 表的深拷贝，包括元表和循环引用。 |
| `Util.merge(base, over)  -> table` | 合并了 `over` 的 `base` 的副本：子表递归合并，其他值直接替换。 |
| `Util.cloneTable(dict)  -> table` | 把 .NET 字典（解析后的 JSON 对象、SQL 行集）递归复制到 Lua 表中。 |

## Core.Json

用纯 Lua 在 Lua 值和 JSON 文本之间互相转换。与把文件解析为 .NET 字典的 `JSONLOADER` 不同，它处理的是 Lua 表，因此适合存放在存档字符串（`sf:SetGlobalString`）或数据库值中的数据。

| 函数 | 说明 |
| --- | --- |
| `Json.encode(value)  -> string` | 键为 1..n 的表写成数组，其他表写成键已排序的对象，因此同一个值总是得到同样的文本。函数和 userdata 不写入对象；整数写成不带小数点的形式。包含自身的表会引发错误。 |
| `Json.decode(text)  -> value` 或 `nil, message` | 解析 JSON 文本。JSON `null` 变为 `Json.null`；遇到语法错误时返回 nil 和一条带位置的消息。 |
| `Json.null` | 代表 JSON `null` 的值；编码后还原为 `null`。 |

```lua
local Json = require("Core.Json")
local sf = GetSaveFile(0)
local data = Json.decode(sf:GetGlobalString("my_feature")) or { v = 1, items = {} }
data.items[#data.items + 1] = "lamp"
sf:SetGlobalString("my_feature", Json.encode(data))
```

## Core.Here

一个函数，根据 `require` 传给调用它的文件的参数，返回该文件所在的文件夹。Lib 文件就是靠它找到自己的资源（见[资源相对于调用方解析](README.md)）。

| 函数 | 说明 |
| --- | --- |
| `require("Core.Here")(...)  -> string` | 文件的绝对文件夹，使用正斜杠并以 `/` 结尾；文件不是通过 `require` 加载时为 nil。请在文件顶层调用它，那里的 `...` 存放着模块名和文件路径。 |
