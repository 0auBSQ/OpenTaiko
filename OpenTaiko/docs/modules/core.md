<!-- modules/core.md -->

# Core

Small helpers with no screen of their own: easing curves, colour maths, table utilities, a JSON encoder and decoder, and the folder of the current file. They touch no engine state when they load, so any module, read-only ones included, can require them.

## Core.Easing

Easing curves on a 0 to 1 ratio, for animations driven by your own clock (a timeline, a particle's age). For an engine-clocked tween, the `COUNTER` global with SetEasing is the other choice. Every curve clamps its input to 0..1 and maps 0 to 0 and 1 to 1, except `hump`.

| Function | Description |
| --- | --- |
| `Easing.linear(t)  -> number` | The clamped ratio itself. |
| `Easing.inQuad(t)`, `outQuad`, `inOutQuad`, `inCubic`, `outCubic`, `inOutCubic`, `inSine`, `outSine`, `inOutSine`, `inExpo`, `outExpo`  `-> number` | The usual curves, by name. |
| `Easing.inBack(t, s?)`, `Easing.outBack(t, s?)  -> number` | Curves that overshoot; `s` is the overshoot amount (1.7 by default). |
| `Easing.inBounce(t)`, `Easing.outBounce(t)`, `Easing.outElastic(t)  -> number` | Bouncing and elastic curves. |
| `Easing.hump(t)  -> number` | 0, up to 1 at the middle, back to 0: for squashes and flashes. |
| `Easing.span(t, t0, dur)  -> number` | The progress of the window that starts at `t0` and lasts `dur`, clamped to 0..1. |
| `Easing.lerp(a, b, k)  -> number` | `a + (b - a) * k`. |
| `Easing.clamp01(t)  -> number` | `t` clamped to 0..1. |
| `Easing.get(kind, fn)  -> function` | A curve by the engine's easing names: `kind` is `"IN"`, `"OUT"` or `"INOUT"`, `fn` is `"LINEAR"`, `"SINE"`, `"QUAD"`, `"CUBIC"`, `"EXPO"`, `"BACK"`, `"BOUNCE"` or `"ELASTIC"`. A pair with no curve gives the QUAD one. |

```lua
local Easing = require("Core.Easing")
local k = Easing.outBack(Easing.span(t, startTime, 0.4))
x = Easing.lerp(x0, x1, k)
```

## Core.Color

Colour conversions and adjustments. Every channel is a float from 0 to 1, hue included (0 to 1 around the wheel); functions take and return separate `r, g, b` values.

| Function | Description |
| --- | --- |
| `Color.rgbToHsv(r, g, b)  -> h, s, v` | RGB to HSV. |
| `Color.hsvToRgb(h, s, v)  -> r, g, b` | HSV to RGB. |
| `Color.rgbToHsl(r, g, b)  -> h, s, l` | RGB to HSL. |
| `Color.hslToRgb(h, s, l)  -> r, g, b` | HSL to RGB. |
| `Color.mix(r, g, b, r2, g2, b2, t)  -> r, g, b` | Linear blend: `t` = 0 keeps the first colour, 1 gives the second. |
| `Color.lighten(r, g, b, amount)  -> r, g, b` | Moves the HSL lightness toward white by `amount` of the remaining range (1 = white); hue and saturation stay. |
| `Color.darken(r, g, b, amount)  -> r, g, b` | Moves the HSL lightness toward black by `amount` (1 = black). |
| `Color.saturate(r, g, b, factor, add?)  -> r, g, b` | Multiplies the HSV saturation by `factor`, adds `add`, clamps to 1. |
| `Color.brighten(r, g, b, factor)  -> r, g, b` | Multiplies the HSV value by `factor`, clamped to 1. |
| `Color.ramp(from, to, n)  -> table` | `n` colours stepped from `{r, g, b}` to `{r, g, b}`, as a list of `{r, g, b}`. |
| `Color.fromEngine(col)  -> r, g, b, a` | The channels of a `COLOR` object, as floats. |
| `Color.toEngine(r, g, b, a?)  -> color` | A `COLOR` object from floats (alpha 1 by default). |
| `Color.rampEngine(fromCol, toCol, n)  -> table` | A ramp of `n` `COLOR` objects between two `COLOR` objects. |
| `Color.clamp01(v)  -> number` | `v` clamped to 0..1. |

## Core.Util

| Function | Description |
| --- | --- |
| `Util.clamp(v, lo, hi)  -> number` | `v` held between `lo` and `hi`. |
| `Util.lerp(a, b, t)  -> number` | Linear interpolation. |
| `Util.round(v)  -> number` | Rounds to the nearest whole number (halves up). |
| `Util.deepcopy(o)  -> value` | A deep copy of a Lua table, metatables and cycles included. |
| `Util.merge(base, over)  -> table` | A copy of `base` with `over` merged in: sub-tables merge recursively, other values replace. |
| `Util.cloneTable(dict)  -> table` | A .NET dictionary (a parsed JSON object, an SQL row set) copied into a Lua table, recursively. |

## Core.Json

JSON text from Lua values and back, in plain Lua. Unlike `JSONLOADER`, which parses files into .NET dictionaries, it works on Lua tables, so it suits data you keep in a save string (`sf:SetGlobalString`) or a database value.

| Function | Description |
| --- | --- |
| `Json.encode(value)  -> string` | A table with the keys 1..n is written as an array, any other table as an object with its keys sorted, so the same value always gives the same text. Functions and userdata are left out of objects; whole numbers are written without a decimal point. A table that contains itself raises an error. |
| `Json.decode(text)  -> value` or `nil, message` | Parses JSON text. JSON `null` becomes `Json.null`; on a syntax error it returns nil and a message with the position. |
| `Json.null` | The value that stands for JSON `null`; it encodes back to `null`. |

```lua
local Json = require("Core.Json")
local sf = GetSaveFile(0)
local data = Json.decode(sf:GetGlobalString("my_feature")) or { v = 1, items = {} }
data.items[#data.items + 1] = "lamp"
sf:SetGlobalString("my_feature", Json.encode(data))
```

## Core.Here

A function that returns the folder of the file calling it, from the arguments `require` passes to that file. It is how a Lib file finds its own assets (see [Assets resolve against the caller](README.md)).

| Function | Description |
| --- | --- |
| `require("Core.Here")(...)  -> string` | The absolute folder of the file, with forward slashes and a trailing `/`; nil when the file was not loaded through `require`. Call it at the file's top level, where `...` holds the module name and the file path. |
