<!-- api/graphics.md -->

# Graphics and text

Textures, canvases, clipping, text, video, colours, gradient maps and sizes. All positions and sizes are in logical screen pixels, the coordinate space scripts draw in (see `INPUT:GetSurfaceWidth()` / `GetSurfaceHeight()`).

The script that creates a texture, canvas, text, video or gradient handle owns it, and the game disposes the handle when it unloads that script. Call `Dispose()` yourself to free a handle earlier.

## Textures

### TEXTURE

Loads image files into texture handles.

<div class="callout warn">
Relative paths resolve against the script's directory; the FromAbsolutePath variants take a full path. A missing file returns an empty handle that draws nothing. CreateTexture loads asynchronously: the handle draws nothing and reports Width and Height of 0 until the background decode and upload finish. Use CreateTextureSync when you need the size or the pixels right away. The options table accepts { maxSize = N } to downscale the image at decode time so its longest side is at most N pixels.
</div>

| Method | Description |
| --- | --- |
| `TEXTURE:CreateTexture()  -> texture` | Creates an empty handle with no image. |
| `TEXTURE:CreateTexture(path)  -> texture` | Loads an image asynchronously from a path relative to the script directory. |
| `TEXTURE:CreateTexture(path, options)  -> texture` | Same, with an options table (`{ maxSize = N }`). |
| `TEXTURE:CreateTextureSync(path)  -> texture` | Loads an image synchronously; size and pixels are available on return. |
| `TEXTURE:CreateTextureFromAbsolutePath(path)  -> texture` | Loads an image from a full filesystem path. |
| `TEXTURE:CreateTextureFromAbsolutePath(path, options)  -> texture` | Same, with an options table (`{ maxSize = N }`). |
| `TEXTURE:Exists(path)  -> bool` | Returns whether a file exists at the path relative to the script directory. |

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

### Texture handle

A drawable 2D image returned by the `TEXTURE` factory, by `text:GetText` / `GetVerticalText`, and by `video.Texture`.

<div class="callout warn">
Anchor names: topleft, top, topright, left, center, right, bottomleft, bottom, bottomright (case-insensitive; an unknown name falls back to topleft). Anchored draws account for the current scale. Blend mode names: Normal, Add, Multi, Sub, Screen. Wrap mode names: Edge, Border, Repeat, Mirror; new handles default to Repeat. On an empty handle every method is a no-op and the getters return the defaults listed below.
</div>

| Method | Description |
| --- | --- |
| `texture:Draw(x, y)  -> nil` | Draws the whole texture with its top-left at (x, y). |
| `texture:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | Draws a source sub-rectangle (in texture pixels) with its top-left at (x, y). |
| `texture:DrawAtAnchor(x, y, anchor)  -> nil` | Draws the whole texture so the named anchor point lands on (x, y). |
| `texture:DrawRectAtAnchor(x, y, rect_x, rect_y, rect_width, rect_height, anchor)  -> nil` | Draws a source sub-rectangle so the named anchor point lands on (x, y). |
| `texture.Loaded  -> bool` | True when the handle wraps a texture. The game sets it as soon as it finds the file, before an asynchronous load finishes. |
| `texture.Width  -> int` | Pixel width; -1 on an empty handle, 0 while an asynchronous load is pending. |
| `texture.Height  -> int` | Pixel height; -1 on an empty handle, 0 while an asynchronous load is pending. |
| `texture.Pointer  -> int` | Native GL texture id, or 0 if none. |
| `texture:GetScale()  -> vector2` | Current draw scale as a vector2 (`X`, `Y`). |
| `texture:GetOpacity()  -> number` | Current opacity, 0..1; -1 on an empty handle. |
| `texture:GetColor()  -> number, number, number` | Current tint as three 0..1 values (red, green, blue). |
| `texture:GetRotation()  -> number` | Current rotation in degrees. |
| `texture:GetBlendMode()  -> string` | Current blend mode name. |
| `texture:GetWrapMode()  -> string` | Current wrap mode name. |
| `texture:SetScale(scale_x, scale_y)  -> nil` | Sets the horizontal and vertical draw scale (1 = original size). |
| `texture:SetOpacity(opacity)  -> nil` | Sets the opacity, 0..1. |
| `texture:SetColor(color)  -> nil` | Sets the tint from a `COLOR` value. The handle ignores the colour's alpha; set transparency with SetOpacity. |
| `texture:SetColor(red, green, blue)  -> nil` | Sets the tint from three 0..1 values. |
| `texture:SetRotation(angle)  -> nil` | Sets the rotation about the texture centre, in degrees. |
| `texture:SetBlendMode(mode)  -> nil` | Sets the blend mode by name (case-insensitive). The handle ignores unknown names. |
| `texture:SetWrapMode(mode)  -> nil` | Sets the wrap mode by name (case-insensitive). The handle ignores unknown names. |
| `texture:SetUseNoiseEffect(enabled)  -> nil` | When enabled, every draw replaces the texture's colour with animated random greyscale noise and keeps its alpha. |
| `texture:SetGradientMap(gradient)  -> nil` | Applies a `GRADIENT` map to every draw of this texture (see GRADIENT). |
| `texture:ClearGradientMap()  -> nil` | Removes the per-texture gradient map. |
| `texture:Dispose()  -> nil` | Frees the texture. |

## Canvas and clipping

### CANVAS

Creates writable pixel surfaces that Lua edits on the CPU and uploads as one texture.

<div class="callout warn">
A canvas is a texture whose pixels Lua sets (SetPixel, FillRect, ...) and then pushes to the GPU with Upload. It suits software rendering and baked UI shapes, where a script paints and uploads once and then draws the result every frame. New canvases start fully transparent.
</div>

| Method | Description |
| --- | --- |
| `CANVAS:CreateCanvas(width, height)  -> canvas` | Creates a transparent canvas of the given size (minimum 1x1). |

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

### Canvas handle

A writable RGBA pixel buffer returned by `CANVAS:CreateCanvas`, drawable like a texture.

<div class="callout warn">
Colour arguments r, g, b, a are 0-255 integers. Pixel edits accumulate in a dirty rectangle and reach the GPU only when you call Upload (BlitPacked uploads by itself). Drawing coordinates are integers. Anchor names are the same as for the texture handle.
</div>

| Method | Description |
| --- | --- |
| `canvas.Width  -> int` | Width in pixels. |
| `canvas.Height  -> int` | Height in pixels. |
| `canvas.Pointer  -> int` | Native GL texture id, or 0 if none. |
| `canvas:SetPixel(x, y, r, g, b, a)  -> nil` | Sets one pixel. The canvas ignores out-of-range coordinates. |
| `canvas:FillRect(x, y, w, h, r, g, b, a)  -> nil` | Fills an axis-aligned rectangle, clipped to the canvas. |
| `canvas:FillCircle(cx, cy, radius, r, g, b, a)  -> nil` | Fills a disc of the given radius in pixels. |
| `canvas:StrokeLine(x0, y0, x1, y1, radius, r, g, b, a)  -> nil` | Paints a thick line as overlapping discs of the given radius. |
| `canvas:PasteTexture(texture, x, y)  -> nil` | Alpha-blends a texture onto the canvas with its top-left at (x, y). The canvas reads the texture's pixels back from the GPU once per texture handle, a slow operation that belongs in setup code. |
| `canvas:PasteTextureTransformed(texture, x, y, scale, rotationDeg, anchor)  -> nil` | Alpha-blends a texture scaled by `scale` and rotated clockwise by `rotationDeg`, using nearest-neighbour sampling. With anchor "center" the texture centre lands on (x, y); any other value places its top-left there. |
| `canvas:Clear(r, g, b, a)  -> nil` | Fills the whole canvas with one colour. |
| `canvas:ClearTransparent()  -> nil` | Resets the whole canvas to fully transparent. |
| `canvas:CopyFrom(other)  -> nil` | Copies the pixels of another canvas of the same size into this one. The call does nothing when the sizes differ. |
| `canvas:BlitPacked(data, count)  -> nil` | Fills the surface from a 1-indexed Lua array of packed `0xRRGGBB` integers in row-major order (pixels become opaque; a negative value becomes a transparent pixel), then uploads. |
| `canvas:Upload()  -> nil` | Pushes pending edits (only the changed region) to the GPU. No-op when nothing changed. |
| `canvas:Draw(x, y)  -> nil` | Draws the canvas with its top-left at (x, y). |
| `canvas:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | Draws a source sub-rectangle (in canvas pixels) with its top-left at (x, y). |
| `canvas:DrawAtAnchor(x, y, anchor)  -> nil` | Draws the canvas so the named anchor point lands on (x, y). |
| `canvas:SetScale(scale_x, scale_y)  -> nil` | Sets the horizontal and vertical draw scale. |
| `canvas:SetOpacity(opacity)  -> nil` | Sets the opacity, 0..1. |
| `canvas:SetColor(red, green, blue)  -> nil` | Sets the tint from three 0..1 values. |
| `canvas:Dispose()  -> nil` | Frees the canvas's GPU texture and CPU buffer. |

### GRAPHICS

Scissor clipping for scrolling panels.

<div class="callout warn">
SetClip takes logical screen coordinates and maps them to the current viewport, so clipping is the same under render scaling and letterboxing. The GPU discards every pixel drawn outside the rectangle between SetClip and ClearClip. SetClip replaces the previous rectangle (there is no stack); keep SetClip and ClearClip paired. The game disables the scissor at the start of every frame.
</div>

| Method | Description |
| --- | --- |
| `GRAPHICS:SetClip(x, y, w, h)  -> nil` | Enables clipping to the given rectangle. |
| `GRAPHICS:ClearClip()  -> nil` | Disables clipping. |

```lua
GRAPHICS:SetClip(100, 200, 600, 400)
for i, row in ipairs(rows) do
    row.tex:Draw(100, 200 + (i - 1) * 48 - scrollY)
end
GRAPHICS:ClearClip()
```

## Text

### TEXT

Creates font renderers for the skin's main font.

<div class="callout warn">
Create returns a text handle that renders whole strings to cached textures; use it for labels that rarely change. CreateGlyphCached returns a glyph text handle that caches one texture per character and composes strings at draw time; use it for text that changes often (timers, scores, typed input) and for word-wrapped blocks. Style arguments are any of bold, italic, underline, strikeout (case-insensitive; other values mean regular). Within one script, the bundled NLua version fails if the script calls the same factory method first with no style argument and later with exactly one style argument. Either never pass a style, always pass one from the first call, or pass two style tokens (for example "bold", "regular").
</div>

| Method | Description |
| --- | --- |
| `TEXT:Create(size, ...style)  -> text` | Creates a whole-string text renderer at the given pixel size. |
| `TEXT:CreateGlyphCached(size, ...style)  -> glyphText` | Creates a glyph-composed text renderer at the given pixel size. |

#### Inline colour tags

Both renderers honour these tags inside the string. Tags nest; an unclosed tag runs to the end of the string. Measuring functions ignore them.

| Tag | Effect |
| --- | --- |
| `<c.#rrggbb>` ... `</c>` | Fill colour. |
| `<c.#rrggbb.#rrggbb>` ... `</c>` | Fill colour and outline colour. |
| `<g.#rrggbb.#rrggbb>` ... `</g>` | Vertical gradient fill (top colour, bottom colour). |

### Text handle

A whole-string renderer returned by `TEXT:Create`.

<div class="callout warn">
GetText and GetVerticalText cache one texture per unique combination of string, fill colour, outline colour and size limit, and keep it until you dispose the handle. A string that changes every frame therefore adds a texture every frame and leaks GPU memory; draw such text with DrawDirect or a glyph text handle. forecolor and backcolor are COLOR values; the defaults are a white fill and an opaque black outline. The centered flag centres each line of a multi-line string; the cache key excludes it, so the first call for a given string decides it.
</div>

| Method | Description |
| --- | --- |
| `text:GetText(text, centered, max_width, forecolor, backcolor)  -> texture` | Renders a horizontal string to a cached texture. All arguments after `text` are optional. When the rendered texture is wider than `max_width`, it draws squeezed horizontally to fit. |
| `text:GetVerticalText(text, centered, max_height, forecolor, backcolor)  -> texture` | Renders a vertically stacked string to a cached texture. When the rendered texture is taller than `max_height`, it draws squeezed vertically to fit. |
| `text:DrawDirect(text, x, y, r, g, b, opacity, spacing)  -> number` | Draws a string glyph by glyph from a per-character cache and returns the x position after the last glyph. `r`, `g`, `b` are 0-255 (default 255), `opacity` is 0..1 (default 1), `spacing` is the gap between glyphs in pixels (default -6). |
| `text:Dispose()  -> nil` | Frees the font and every cached texture. |

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

### Glyph text handle

A glyph-composed renderer returned by `TEXT:CreateGlyphCached`.

<div class="callout warn">
forecolor and backcolor are COLOR values; the defaults are a white fill and a black outline. Every argument after the position is optional. maxWidth (0 = no limit) squeezes each glyph horizontally so the whole line fits. anchor uses the same nine names as the texture handle. '\n' starts a new line; Draw stacks lines left-aligned. Draw returns the x coordinate of the right edge of the drawn box. Box geometry matches GetText textures (25 px padding on each side and below the ink), so a glyph text and a GetText texture drawn at the same point line up.
</div>

| Method | Description |
| --- | --- |
| `glyphText.LineHeight  -> number` | Line pitch of multi-line text, in pixels. |
| `glyphText.BoxHeight  -> number` | Height of a one-line box (ink plus padding), matching a one-line GetText texture. |
| `glyphText:Measure(text, scale)  -> number` | Ink width of the widest line, times `scale` (default 1). |
| `glyphText:Draw(text, x, y, forecolor, backcolor, opacity, scale, maxWidth, anchor, scaleY, rotationDeg)  -> number` | Draws the text. `opacity` 0..1 (default 1); `scale` (default 1); `scaleY` when > 0 overrides the vertical scale; `rotationDeg` rotates the whole block about (x, y). Returns the right-edge x of the box. |
| `glyphText:SetClipY(y0, y1)  -> nil` | Restricts subsequent upright Draw calls to the vertical band y0..y1 in screen space; Draw skips glyphs outside the band and slices glyphs on its edges. Pass y1 <= y0 to clear. Rotated draws ignore the band. |
| `glyphText:WrapToLines(text, wrapWidth, scale)  -> string[]` | Word-wraps to `wrapWidth` and returns the lines as plain strings (tags stripped). |
| `glyphText:MeasureWrapped(text, wrapWidth, scale, lineSpacing)  -> number` | Height DrawWrapped would use, without drawing. `lineSpacing` multiplies the line pitch (default 1). |
| `glyphText:DrawWrapped(text, x, y, wrapWidth, forecolor, backcolor, opacity, scale, lineSpacing)  -> number` | Word-wraps to `wrapWidth` and draws left-aligned from (x, y). Returns the drawn height. |
| `glyphText:Dispose()  -> nil` | Frees the font and every cached glyph. |

Word wrapping breaks at spaces, between CJK characters, and inside a word that is wider than the wrap width.

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

## Video

### VIDEO

Loads video files into playable decoder handles.

<div class="callout warn">
Paths are relative to the script directory. The decoder opens on a background thread; until it is ready the handle draws nothing, queues Start, seek and speed calls, and applies them once the decoder attaches. A missing file returns an empty handle.
</div>

| Method | Description |
| --- | --- |
| `VIDEO:CreateVideo(path)  -> video` | Opens a video file and returns its handle. |

### Video handle

A video decoder returned by `VIDEO:CreateVideo`.

<div class="callout warn">
Positions are in milliseconds; Duration is in seconds. Read Texture every frame to get the current frame as a texture handle. That handle wraps the video's own frame texture: draw it and leave disposal to the video handle. SetSpeed ignores values of 0 or below.
</div>

| Method | Description |
| --- | --- |
| `video:Start()  -> nil` | Starts playback. |
| `video:Resume()  -> nil` | Resumes playback after Pause. |
| `video:Pause()  -> nil` | Pauses playback. |
| `video:Stop()  -> nil` | Stops playback. |
| `video:Reset()  -> nil` | Seeks back to the start. |
| `video.Width  -> int` | Frame width in pixels, or -1 until the decoder is ready. |
| `video.Height  -> int` | Frame height in pixels, or -1 until the decoder is ready. |
| `video.Duration  -> number` | Total duration in seconds (1 until the decoder is ready). |
| `video.DurationMs  -> number` | Total duration in milliseconds. |
| `video.Texture  -> texture` | The current decoded frame. |
| `video:IsFinished()  -> bool` | True once the stream has ended and no frames remain. |
| `video:GetTimestampMs()  -> number` | Current playback position in milliseconds. |
| `video:SetTimestampMs(ms)  -> nil` | Seeks to a position in milliseconds. |
| `video:GetSpeed()  -> number` | Current playback speed multiplier (1 = normal). |
| `video:SetSpeed(speed)  -> nil` | Sets the playback speed multiplier. |
| `video:GetPlayPosition()  -> number` | Current position in seconds (older name for GetTimestampMs / 1000). |
| `video:SetPlayPosition(seconds)  -> nil` | Seeks to a position in seconds (older name for SetTimestampMs). |
| `video:GetPlaySpeed()  -> number` | Older name for GetSpeed. |
| `video:SetPlaySpeed(speed)  -> nil` | Older name for SetSpeed. |
| `video:Dispose()  -> nil` | Frees the decoder and its frame texture. |

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

## Colour, gradient and size

### COLOR

Creates colour values.

<div class="callout warn">
Channels are 0-255. The texture, canvas and text methods that take a colour accept these values.
</div>

| Method | Description |
| --- | --- |
| `COLOR:CreateColorFromRGBA(r, g, b, a)  -> color` | Creates a colour from red, green, blue and optional alpha (default 255). |
| `COLOR:CreateColorFromARGB(a, r, g, b)  -> color` | Creates a colour from alpha, red, green and blue. |
| `COLOR:CreateColorFromHex(value)  -> color` | Parses an `AARRGGBB` hexadecimal string without a prefix (for example `"ff2080ff"`). A six-digit string yields alpha 0. An unparsable string yields opaque white. |

### Color handle

A colour value returned by the `COLOR` factory.

| Method | Description |
| --- | --- |
| `color.R  -> int` | Red channel, 0-255 (read/write). |
| `color.G  -> int` | Green channel, 0-255 (read/write). |
| `color.B  -> int` | Blue channel, 0-255 (read/write). |
| `color.A  -> int` | Alpha channel, 0-255 (read/write). |

### GRADIENT

Gradient maps recolour texture draws by mapping each pixel's luminance to a colour ramp, either on one texture or on every draw.

<div class="callout warn">
Create takes a table of at least two stops, each an array { position 0-1, r 0-255, g 0-255, b 0-255 [, a 0-255] }, and a blend amount (0 = no effect, 1 = full replacement, values between mix; default 1). Fewer than two stops raises an error. Identical stops and blend reuse one cached GPU texture for the whole program. Apply to a single texture with texture:SetGradientMap, or to every texture draw with SetActive.
</div>

| Method | Description |
| --- | --- |
| `GRADIENT:Create(stops, blend)  -> gradient` | Builds (or reuses) a gradient map from colour stops and a blend amount. |
| `GRADIENT:SetActive(gradient)  -> nil` | Applies the gradient to every texture draw until ClearActive. |
| `GRADIENT:ClearActive()  -> nil` | Removes the global gradient map. |

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

### Gradient handle

A gradient map returned by `GRADIENT:Create`.

| Method | Description |
| --- | --- |
| `gradient.BlendStrength  -> number` | Blend amount, 0..1 (read-only). |
| `gradient:Dispose()  -> nil` | Releases the handle. The shared GPU texture stays cached. |

### SIZE

Creates width/height value objects.

| Method | Description |
| --- | --- |
| `SIZE:CreateSize(width, height)  -> size` | Creates a size object. |

### Size handle

A width and height pair returned by `SIZE:CreateSize`.

| Method | Description |
| --- | --- |
| `size.Width  -> int` | Width (read/write). |
| `size.Height  -> int` | Height (read/write). |
