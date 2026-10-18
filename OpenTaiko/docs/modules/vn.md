<!-- modules/vn.md -->

# VN

The pieces of a visual-novel scene: the dialogue box, the script runner and the scene player. `VN.Scene` puts the other two together; use the runner and the box alone when your stage draws the scene itself.

## VN.Dialogue

A typewriter dialogue box: a speaker name, a portrait area, word-wrapped text revealed letter by letter, and choices. It drives itself: call `dlg:update(dt)` and `dlg:draw()` every frame, and read `dlg.result` once it reports `"done"`. The box draws with the UI library's shapes and sounds, in a look of its own (the `theme` option), whichever library the skin picked.

A script is a list of nodes `{ name, text, portrait, choices }`. A node with `choices` (a list of `{ label, value }`) shows them once its text is out; choosing one sets `dlg.result` to its `value` and ends the dialogue. A script that ends without a choice sets `dlg.result` to true.

Tags inside `text` run when the typewriter reaches them:

| Tag | Effect |
| --- | --- |
| `{pause:sec}` | Holds the reveal (0.4 s by default). |
| `{speed:cps}` | Changes the reveal speed, in characters per second. |
| `{shake:amp,dur}` | Shakes the screen: `onShake(amp, dur)`, or the 3D camera when no `onShake` handler is given. |
| `{sfx:name}`, `{voice:name}`, `{expr:name}` | Call `onSfx(name)`, `onVoice(name)` or `onExpr(name)` (an expression change in the middle of a line). |

| `opts` field | Description |
| --- | --- |
| `gfont`, `gfontName` | Glyph fonts (`TEXT:CreateGlyphCached`) for the text and the name: UTF-8 text with word wrap. The name falls back to `gfont`. |
| `fonts` | `{ name, text }` text fonts instead, the older path (ASCII). |
| `ui` | `"popui"` (the default: a rounded box with a shadow and a name pill) or `"plain"` (a flat panel). |
| `theme` | The box colours: `face`, `face2`, `outline`, `namePill`, `namePill2`, `nameText`, `text`, `choiceRow`, `choiceSel`, `choiceText`, `caret`, `shadow`, `textEdge`. |
| `box` | `{ x, y, w, h }`: the box's place (by default across the bottom of the screen). |
| `portraitSize`, `portraits` | The portrait square (0 hides it, for big sprites drawn by the stage); `{ name = texture }` by speaker name. |
| `cps` | The reveal speed each node starts at (42 by default). |
| `mouse` | true: a left click also advances and confirms. |
| `advanceInput` | A function returning true for an extra advance input (a drum pad). |
| `drawBox` | false draws only the text, choices and caret, over your own box art. |
| `onBlip`, `blipEvery` | A typing blip: `onBlip()` every `blipEvery` revealed characters (3). |
| `onSfx`, `onVoice`, `onExpr`, `onShake` | The tag callbacks. |
| `sfx` | Sounds by name for advancing and choosing, as in [the UI](ui.md). |
| `opacity`, `caret` | The whole box's opacity, and false to hide the advance marker; both may change any frame. |

| Method | Description |
| --- | --- |
| `Dialogue.new(opts)  -> dlg` | Makes a box. |
| `dlg:start(script)` | Starts a script from its first node. |
| `dlg:update(dt)  -> "running" or "done"` | Runs one frame: Decide, Space (or a click, or `advanceInput`) reveals the whole line, then advances; Up and Down pick a choice. |
| `dlg:draw()` | Draws the box. |
| `dlg:active()  -> bool` | Whether a script is running. |
| `dlg.result` | The chosen value, or true, once done. |
| `dlg:dispose()` | Frees the baked box canvases. |

```lua
local Dialogue = require("VN.Dialogue")
local dlg, answer, last = nil, nil, 0

function activate()
    dlg = Dialogue.new{ gfont = TEXT:CreateGlyphCached(30), portraitSize = 0 }
    dlg:start{
        { name = "Nokon", text = "Welcome back.{pause:0.5} Ready?" },
        { name = "Nokon", text = "Pick one.", choices = { { label = "Yes", value = 1 }, { label = "Later", value = 2 } } },
    }
end

function update(ts)
    local dt = (ts - last) / 1000; last = ts
    if dlg:update(dt) == "done" then answer = dlg.result end
end

function draw() dlg:draw() end
```

## VN.Runner

Walks a script of lines: guards, random picks, choices that splice their branch in, and the stage directions the caller registers. It draws nothing: `VN.Scene` (below) or your own stage steps it and puts its prose lines on a `VN.Dialogue` box.

A line is a table, every field optional:

| Field | Description |
| --- | --- |
| `text` | The prose, with the box's inline tags. A line with `choices` and no `text` (or `""`) keeps the previous prose line on screen, already read, under its choices. |
| `who`, `name` | The speaker's id; the name shown, which wins over the speaker's own (`"???"`). |
| `expr` | The speaker's expression. |
| `choices`, `branch` | `{ { label, value }, ... }`; `branch = { [value] = { lines } }` is spliced in after the choice. The value taken goes to `ctx.choice` and `ctx.choices[value]`. |
| `when` | `function(ctx)` returning true when the line plays; a guard that raises an error counts as false. |
| `pick` | `{ { lines = { ... }, weight = n }, ... }`: one option drawn by weight (1 by default), never the same one twice in a row, spliced in after the line. |
| any other key | A stage direction (`enter`, `bg`, `fade`, `sfx`, `flag`, ...), run by the function registered for it. |

| `opts` field | Description |
| --- | --- |
| `lines`, `ctx` | The script (the runner walks a copy) and the table the guards read. |
| `tr` | `fn(s)`: translates texts, names and choice labels; without it they show as written. |
| `speakerName` | The name a line with `who` and no `name` shows: a string, a `{ [who] = name }` table or `fn(who, line, ctx)`. |
| `playerTag`, `playerName` | The tag replaced by the player's name in texts (`"<player name>"`), and that name: a string or `fn(ctx)`, `ctx.playerName` by default. |
| `directions`, `order` | `{ [key] = fn(line, runner) }`, and the order they are tried in (`Runner.ORDER`, then the other keys sorted). |
| `onLine`, `onChoose` | `fn(line, runner)` for every line that plays, before its directions; `fn(line, value, runner)` when a choice is taken, a resumed one included. |
| `random` | `fn()` returning a number in [0, 1), for the picks (`math.random` by default). |

A direction returns `false` when it does not handle the line (the next one in the order is tried), nil or true to go on to the next line, `"hold"` to stop the walk until the caller steps again, or `"end"` to end the script. Unless the caller registers its own `text` direction, a line with text or choices stops the walk as `"line"`.

| Method | Description |
| --- | --- |
| `Runner.new(opts)  -> runner` | Makes a runner on `opts.lines`. |
| `runner:step()  -> verdict, line` | Walks to the next stop: `"line"` (a prose line to show), `"hold"` or `"end"`. |
| `runner:present(dlg, line?)  -> node, carried` | Starts the current line on a `VN.Dialogue` box; a bare choice carries the previous prose line, fully revealed. |
| `runner:choose(value)  -> bool` | Reports the box's result: a choice value is recorded and its branch spliced in. |
| `runner:current()  -> line`, `runner:load(lines)` | The current line; starts over on a script. |
| `runner:ahead(fn, otherwise)  -> value` | The first answer `fn(line)` gives (true or false) for the lines after the current one; `otherwise` when the script ends first. |
| `runner:bookmark()  -> mark` | `{ idx, picks, rolls }`: the line on screen, the choices taken and the picks drawn. |
| `runner:resume(mark, replay?)` | Walks silently back to the bookmarked line, taking the same choices and picks; `replay = { [key] = fn(line, runner) }` re-applies the directions that leave state behind (a background, a flag). The next `step` shows the bookmarked line. |

The helpers `Runner.passes(line, ctx)`, `Runner.node(line, ctx, o, carry)`, `Runner.start(dlg, line, ctx, o, carry)`, `Runner.resolveChoice(lines, idx, ctx, value)` and `Runner.resolvePick(lines, idx, random, forced)` work without a runner, for a stage that walks its script itself.

## VN.Scene

A scene player built on the runner and the box: background layers that show once loaded, a cast of sprites, screen fades and shakes, and the dialogue box with per-speaker typing blips. It loads nothing of its own: the backgrounds, the cast's textures, the music, the sounds and the saved bookmark all come from the caller.

A scene is `{ bg, music, lines }`: the first background, the music (else the background's), and lines in the runner's form with these directions built in:

| Line | Effect |
| --- | --- |
| `{ enter = who, expr, side }` | A cast member comes on, fading (and sliding) in; the scene waits until it has. One already on screen only changes its face. |
| `{ leave = who }` | It goes; the scene waits until it has. |
| `{ expr = name }` | The face of `who`, or of the focused member. |
| `{ bg = key }` | The background, shown once its layers have loaded (or after `backgrounds.wait` seconds). |
| `{ fade = "out" or "in", keepSprite }` | A fade over the scene; `keepSprite = true` keeps the cast lit. |
| `{ shake = true }`, `{ sfx = name }` | A screen shake (`shake` may also be an amplitude, or `{ amp, dur }`); a sound. |
| `{ flag = name, value }` | Sets `ctx.flags[name]` (true when `value` is absent). |
| `{ outcome = value }` | The scene fades out and reports `value`. |

A line of a cast member who has not entered yet fades them in first. A direction of the caller's that returns `"hold"` holds the scene until `scene:continue()`, or until the function given to `scene:waitFor(fn)` returns true (`fn(dt)` is called every frame).

| `opts` field | Description |
| --- | --- |
| `box` | `VN.Dialogue.new` options; the scene fills `onBlip`, `onShake`, `onExpr` and `onSfx` when they are not set. Or `dialogue`, a box of your own. |
| `cast` | `{ [who] = member }`. A member: `body` (a texture, or `fn(expr)` for one image per expression), `face` (`fn(expr)`, drawn over the body at `faceX`, `faceY`), `name`, `expr` (the one it enters with), `side` (`"left"`, `"center"`, `"right"`) or `x`, `y` and `anchor` (`"top"` or `"bottom"`), `scale` or `height` (the scale that fits the body to that many pixels), `fade` (0.35 s), `slide` (px), `bounce` (0.36 s, 0 for none) and `squash` for the bounce on a face change. |
| `sides`, `focus`, `dim` | The x of each side; whose face an `expr` without a speaker changes; the colour `{ r, g, b }` of the members who are not speaking (none: never dimmed). |
| `backgrounds` | `defs` (`{ [key] = def }`) and `path` (`"Textures/bg/%s.png"` for a key without a def), or `resolve(key)`; `open(layer)` for textures the caller owns (a borrowed texture is opened again if its owner releases it); `wait` (2 s); `prepare`, `show`, `update`, `drawBehind`, `drawOver`, `close` hooks. A def is a layer or `{ layers = { ... } }`, a layer `{ path, x, y, scale, scroll }`, and a def's `music` plays while it shows. |
| `music` | `{ play(key), fadeOut(sec), playing(), update(dt), stop() }`, all optional. |
| `sound`, `blip`, `clickSound` | `fn(name)` plays a sound by name; `blip(node)` returns the typing blip of a box node (`node.speaker`), nil for silence; the advance click. |
| `cover` or `drawCover` | The fade's texture (or `fn()` returning one), stretched over the screen; or `fn(alpha)` that draws the fade. |
| `drawBox`, `fadeSec`, `outcome` | `fn(dlg, scene)` drawing the box instead of `dlg:draw()`; the fade time (0.45 s); the result when the lines just run out (`"done"`). |
| `tr`, `speakerName`, `playerTag`, `playerName`, `random`, `onLine`, `order` | Passed to the runner. |
| `directions`, `replay` | Extra directions, or replacements for the built-in ones; how a resumed bookmark re-applies them. |
| `onBookmark`, `hooks` | `fn(mark)` at each line that starts, with a bookmark to save; `hooks = { prose, start, expr, choose, update, draw, dispose }`. |

| Method | Description |
| --- | --- |
| `Scene.new(opts)  -> scene` | Makes a player. |
| `scene:play(scene, ctx, onDone, resume?)` | Starts a scene. `ctx` is read by the guards and receives the choices; `onDone(result)` gets the outcome; `resume` is a bookmark to continue from. |
| `scene:update(dt)  -> "running" or "done"`, `scene:draw()` | Call every frame. |
| `scene:continue()`, `scene:waitFor(fn)` | Ends a hold; holds until `fn(dt)` returns true. |
| `scene:setBackground(key)`, `scene:setExpr(who, expr)`, `scene:shake(amp, dur)`, `scene:sound(name)` | The built-in effects, for your own directions. |
| `scene:bookmark()  -> mark`, `scene:active()  -> bool` | The runner's bookmark; whether a scene is playing. |
| `scene:reset()`, `scene:dispose()` | Stops the scene; frees what the scene opened (and the box, unless it is yours). |

```lua
local Scene = require("VN.Scene")
local vn = Scene.new{
    box = { gfont = font30, gfontName = font34 },
    cast = { aoi = { name = "Aoi", body = aoiTex, face = function(e) return faces[e] end,
                     faceX = 210, faceY = 140, side = "right", anchor = "bottom", expr = "normal" } },
    backgrounds = { path = "Textures/bg/%s.png" }, cover = blackTex,
    sound = function(n) sfx[n]:Play() end,
    blip = function(node) return node and node.speaker and "blip" end,
    onBookmark = function(mark) save.vn = mark end,
}
vn:play({ bg = "park", lines = {
    { text = "A quiet afternoon." },
    { who = "aoi", expr = "smile", text = "Hi, <player name>!" },
    { text = "", choices = { { label = "Hi!", value = "hi" }, { label = "...", value = "shy" } },
      branch = { shy = { { who = "aoi", expr = "sad", text = "Not talkative today?" } } } },
    { bg = "street", sfx = "door" }, { fade = "out" }, { fade = "in" }, { outcome = "met" },
} }, { playerName = name }, function(result) met = result end, save.vn)
-- then vn:update(dt) and vn:draw() every frame
```

The box draws through `VN.Dialogue`, so its chrome follows the UI library [UI.Pick](ui.md) gives.
