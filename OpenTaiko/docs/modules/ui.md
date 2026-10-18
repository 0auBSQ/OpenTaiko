<!-- modules/ui.md -->

# UI

A UI library for menus and dialogs, driven by the mouse, the keyboard and the pad with one shared highlight. `UI.SimpleUI` is the minimal library OpenTaiko ships; a skin can give it its own look with `derive`, as Open-World Memories does with PopUI. Global modules take their library from `UI.Pick`, which gives the one the skin chose (or one the stage names), so every screen of a skin looks the same.

## UI.Pick

`require("UI.Pick")` returns a function; calling it returns a UI library table.

| Call | Library |
| --- | --- |
| `Pick(name)` | The library with that require name (`"PopUI"`, `"UI.SimpleUI"`, ...), for a stage that wants a specific one. |
| `Pick()` | The library named by the shared string `ui_library`, which a skin sets once (Open-World Memories' `_boot` sets `"PopUI"`). |
| `Pick()` with no `ui_library` | `UI.SimpleUI`. |

A name that does not load, or that loads something without a `new` function, gives `UI.SimpleUI` as well, so a name another skin left in the shared string costs nothing. The choice is plain Lua and shared strings: the engine knows nothing of it.

```lua
-- in a skin's _boot onStart: the library every global module will use
SHARED:SetSharedString("ui_library", "MyUI")

-- in any module
local UI = require("UI.Pick")()
local ui = UI.new{ navPlayer = 1 }
```

## UI.SimpleUI

`UI.new(opts)` makes a manager, which owns the widgets, routes the input and draws them. Call `ui:update(ts)` and `ui:draw()` every frame.

| `opts` field | Description |
| --- | --- |
| `theme` | A theme table merged over the library's default look (see Theming). |
| `sfx` | Sounds by name, merged over the defaults (see Sounds). |
| `bg` | true draws the theme's `bg` colour over the whole screen first. |
| `navPlayer` | The player whose drum also navigates, as `NavInput.getPn` takes it; nil for every player. |
| `nav` | `"spatial"`: the arrow keys move to the nearest widget in their direction (a grid). By default Up and Down step through the widgets in the order they were made. |

| Method | Description |
| --- | --- |
| `ui:update(ts)  -> "cancel" or nil` | Runs the input and the animations for one frame; returns `"cancel"` when Cancel was pressed, for the stage to act on. |
| `ui:draw()` | Draws every visible widget, in the order they were made. |
| `ui:button(o)`, `ui:label(o)`, ...  `-> widget` | Makes a widget, adds it and returns it (see Widgets). |
| `ui:add(w)`, `ui:remove(w)` | Adds or removes a widget made by hand. |
| `ui:clear()` | Removes every widget and frees its canvases. |
| `ui:disposeWidgets()` | Frees the widgets' canvases. Call it before dropping a manager: canvases are not freed on their own. |
| `ui:focus(w)  -> bool`, `ui:focused()  -> widget` | Moves the key focus to a widget; the widget that holds it. |
| `ui:focusNext()`, `ui:focusPrev()` | Steps the key focus. |
| `ui:setTheme(t)`, `ui:setSfx(t)` | Replaces the manager's theme or sounds; every widget restyles. |
| `ui:isCapturing()  -> bool` | True while a text box takes the keys: skip your own hotkeys then. |
| `ui:mouseActive()  -> bool` | True while the mouse is the device used last. |
| `ui:drawText(size, str, x, y, color, opacity?, scale?, maxWidth?)` | Draws text with the UI's glyph font, top-left anchored. |
| `ui:drawTextEx(size, str, x, y, fg, bg, opacity, scale, maxWidth, anchor)` | The same with an outline colour and one of the nine anchor names. |
| `ui:measureText(size, str)`, `ui:wrapLines(size, str, maxWidth)`, `ui:ellipsize(size, str, maxWidth)` | Text measuring, wrapping and shortening with the same font. |
| `ui:textNudge(size)  -> number` | The offset that centres a line of text on a middle line. |
| `UI.flushSharedFonts()` | Drops the cached fonts; call it when the language (and so the font) changes. |

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

## Widgets

Every widget takes `x`, `y`, `w` and `h`, plus `enabled`, `visible`, `focusable`, `mouseOnly` (hovered and clicked, but the keys step over it), `style` or `theme` (theme fields for this widget only), `sfx` and `disabledLook = "fade"`. The options become the widget's fields; change them with the methods (`setEnabled(b)`, `setVisible(b)`, `setText(t)`, ...), which restyle the widget.

| Factory | Options and methods |
| --- | --- |
| `ui:button(o)` | `text` or `icon` (`"play"`, `"pause"`, `"stop"`, `"back"`, `"close"`, `"lock"`, `"repeat"`, ...), `accent`, `onClick(b)`. Its size fits the label when `w`/`h` are omitted. `setText`, `setIcon`. |
| `ui:label(o)` | `text`, `size` (`"title"`, `"label"`, `"small"` or a number), `color`, `align`, `chip` (a rounded background). Not focusable. `setText`. |
| `ui:toggle(o)`, `ui:checkbox(o)` | `text`, `value`, `onChange(v)`. `setValue(v, silent)`. |
| `ui:slider(o)` | `min`, `max`, `step`, `value`, `showValue`, `valueW`, `onChange(v)`. Drag, or Left/Right while focused. `setValue(v, silent)`. |
| `ui:textbox(o)` | `value`, `placeholder`, `maxLen`, `onChange`, `onSubmit`. Click or Decide to type. |
| `ui:panel(o)` | `title`. A container drawn behind the widgets made after it; `panel:content()` gives its inner rectangle. Not focusable. |
| `ui:bubble(o)` | `text`, `tail` (`"bottom"`, `"top"`, `"left"`, `"right"`), `tailPos`, `typewriter`, `onDone`. `setText`, `setTail`, `setSize`. |
| `ui:menu(o)` | `items` (strings, or `{ text, value, locked, mark }`), `selected`, `rowHeight`, `onChange(i, item)`, `onSelect(i, item)`, `onLockedSelect`, `drawLocked`, `hoverSelects`, `scrollbar`. A scrolling list. `setSelected(i)`. |
| `ui:chooser(o)` | `options`, `index`, `wrap`, `onChange(i, option)`. An option stepper (Left/Right). `value()`, `setOptions`, `setIndex`. |
| `ui:settingsList(o)` | `rows` (`{ header = "..." }` or `{ opt = option }`), `onFocusRow`, `onActivateRow`, `scrollbar`. The settings screen's list of option rows. `setRows`. |
| `ui:coins(o)` | A coin counter (see below). Not focusable. |

The widget classes are fields of the library (`UI.Button`, `UI.Menu`, ...) for subclassing, together with `UI.Widget` (the base class), `UI.Shape` (the rounded shapes, shadows and icons the widgets bake), `UI.Theme`, `UI.Sfx` and `UI.Cache` (pooled canvases).

## The coins widget

`ui:coins{ amount, plain, size, iconSize, color, edge }` shows an amount next to the coin icon, on a pill (`plain = true`: the icon and the number only, right-aligned). Each library draws it its own way: SimpleUI's icon is the `Coin.png` beside it, PopUI keeps the shipped skin's coin box.

| Method | Description |
| --- | --- |
| `c:setAmount(n)` | Shows `n` at once. |
| `c:add(n)` | Rolls the number by `n`. |
| `c:gain(n)`, `c:pay(n)` | The same, with a `+n` or `-n` above it and the sound `coin` or `pay`. |
| `c:amount()  -> number` | The amount it is heading to. |
| `c:setPrice(n)`, `c:setBonus(n)` | A price, or a `+n` bonus, shown above it; nil removes it. |
| `c:drawCoin(x, y, sx, sy, opacity)` | Draws the coin icon alone, centred on (x, y). |

## Theming

A theme is one table. The manager resolves each widget's look as the library's default, then the manager's `theme`, then the widget's `style`; tables merge field by field, so a theme only names what it changes.

| Field | Description |
| --- | --- |
| `colors` | `{r, g, b, a}` colours from 0 to 255: `bg`, `surface`/`surface2` (face, top and bottom), `primary`/`primary2`, `accent`/`accent2`, `outline`, `text`, `textOnAccent`, `textDisabled`, `shadow`, `gloss`, `focusRing`, `track`, and `gain`/`loss` for the coins. |
| `radius`, `radiusSmall` | Corner radii for large and small elements. |
| `outlineWidth` | Border thickness. |
| `gloss`, `shadow`, `textOutline` | The top sheen (bool), the drop shadow (`{ dx, dy, layers, grow }`), outlined letters (bool). |
| `font` | Text sizes: `small`, `label`, `button`, `title`. |
| `anim` | Hover and press scales and times, their easings (`{ "OUT", "QUAD" }`), `typewriterCps`. |

```lua
local ui = UI.new{ theme = { colors = { accent = { 220, 90, 60, 255 } }, radius = 14 } }
ui:button{ text = "Delete", x = 100, y = 100, style = { colors = { primary = { 180, 40, 40, 255 } } } }
```

## Sounds

`sfx` tables map a name to a sound handle, a function, or the name of another entry: `move`, `hover`, `click`, `toggle`, `decide`, `cancel`, `skip`, `error`, plus `coin` and `pay` for the coins widget. By default they play the shared sounds `Move`, `Decide`, `Cancel`, `Skip` and `Error`, so a skin that registers those with `SHARED:SetSharedSFX` gives every global screen its sounds; a missing one stays silent.

## derive: a library with your look

`UI.SimpleUI.derive(spec)` returns a new library with SimpleUI's widgets, manager, focus and input, and its own look:

| `spec` field | Description |
| --- | --- |
| `Theme` | A theme module: a table whose `DEFAULT` is the full default theme (copy SimpleUI's `UI.SimpleUI.theme` and change it). |
| `Coins` | An optional coins widget class, for a coin counter drawn your way. |
| `name` | The library's name. |

```lua
-- System/My Skin/Modules/Lib/MyUI.lua
local SimpleUI = require("UI.SimpleUI")
return SimpleUI.derive{
    name  = "MyUI",
    Theme = require("MyUI.theme"),
}
```

Name it in `ui_library` and every global module builds its screens with it. Open-World Memories' `Modules/Lib/PopUI.lua` is this pattern: SimpleUI with its Bubblegum theme and its own coins widget.
