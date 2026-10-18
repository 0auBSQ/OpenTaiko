<!-- modules/vn.md -->

# VN

视觉小说场景的组成部分：对话框、脚本运行器和场景播放器。`VN.Scene` 把另外两者组合在一起；如果你的舞台自己绘制场景，就只使用运行器和对话框。

## VN.Dialogue

一个打字机式的对话框：说话人名称、立绘区域、逐字显示并自动换行的文本，以及选项。它自行运转：每帧调用 `dlg:update(dt)` 和 `dlg:draw()`，并在它报告 `"done"` 后读取 `dlg.result`。无论皮肤选择了哪个库，对话框都使用 UI 库的形状和声音绘制，并有自己的外观（`theme` 选项）。

脚本是节点 `{ name, text, portrait, choices }` 的列表。带有 `choices`（`{ label, value }` 的列表）的节点在其文本显示完后显示这些选项；选择其中一个会把 `dlg.result` 设为它的 `value` 并结束对话。没有经过选择就结束的脚本把 `dlg.result` 设为 true。

`text` 中的标签在打字机到达它们时执行：

| 标签 | 效果 |
| --- | --- |
| `{pause:sec}` | 暂停显示（默认 0.4 秒）。 |
| `{speed:cps}` | 改变显示速度，单位为每秒字符数。 |
| `{shake:amp,dur}` | 震动屏幕：`onShake(amp, dur)`；没有提供 `onShake` 处理函数时震动 3D 摄像机。 |
| `{sfx:name}`, `{voice:name}`, `{expr:name}` | 调用 `onSfx(name)`、`onVoice(name)` 或 `onExpr(name)`（在一句台词中途改变表情）。 |

| `opts` 字段 | 说明 |
| --- | --- |
| `gfont`, `gfontName` | 用于文本和名称的字形字体（`TEXT:CreateGlyphCached`）：支持自动换行的 UTF-8 文本。名称回退到 `gfont`。 |
| `fonts` | 改用 `{ name, text }` 文本字体，这是较旧的方式（ASCII）。 |
| `ui` | `"popui"`（默认：带阴影和名称胶囊的圆角框）或 `"plain"`（扁平面板）。 |
| `theme` | 对话框的颜色：`face`、`face2`、`outline`、`namePill`、`namePill2`、`nameText`、`text`、`choiceRow`、`choiceSel`、`choiceText`、`caret`、`shadow`、`textEdge`。 |
| `box` | `{ x, y, w, h }`：对话框的位置（默认横跨屏幕底部）。 |
| `portraitSize`, `portraits` | 立绘方块（0 表示隐藏，用于由舞台绘制大精灵的情况）；按说话人名称给出的 `{ name = texture }`。 |
| `cps` | 每个节点开始时的显示速度（默认 42）。 |
| `mouse` | true：左键点击也可以推进和确认。 |
| `advanceInput` | 一个函数，对额外的推进输入（鼓面）返回 true。 |
| `drawBox` | 为 false 时只绘制文本、选项和光标，叠加在你自己的对话框图片上。 |
| `onBlip`, `blipEvery` | 打字音效：每显示 `blipEvery` 个字符调用一次 `onBlip()`（3）。 |
| `onSfx`, `onVoice`, `onExpr`, `onShake` | 标签回调。 |
| `sfx` | 用于推进和选择的按名称给出的声音，与 [UI](ui.md) 中相同。 |
| `opacity`, `caret` | 整个对话框的不透明度，以及用于隐藏推进标记的 false；两者都可以在任意帧改变。 |

| 方法 | 说明 |
| --- | --- |
| `Dialogue.new(opts)  -> dlg` | 创建一个对话框。 |
| `dlg:start(script)` | 从第一个节点开始运行脚本。 |
| `dlg:update(dt)  -> "running" or "done"` | 运行一帧：Decide、Space（或点击，或 `advanceInput`）先显示整句，然后推进；上下键选择选项。 |
| `dlg:draw()` | 绘制对话框。 |
| `dlg:active()  -> bool` | 是否有脚本正在运行。 |
| `dlg.result` | 结束后所选的值，或 true。 |
| `dlg:dispose()` | 释放烘焙的对话框画布。 |

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

逐行运行一份脚本：条件判断、随机抽取、把分支接入的选项，以及调用方注册的舞台指令。它不绘制任何内容：由 `VN.Scene`（见下文）或你自己的舞台推进它，并把它的正文行放到 `VN.Dialogue` 对话框上。

一行是一张表，每个字段都可选：

| 字段 | 说明 |
| --- | --- |
| `text` | 正文，可使用对话框的内联标签。带有 `choices` 但没有 `text`（或为 `""`）的行会把上一条正文行以已读状态留在屏幕上，并在其下方显示选项。 |
| `who`, `name` | 说话人的 id；显示的名称，优先于说话人自己的名称（`"???"`）。 |
| `expr` | 说话人的表情。 |
| `choices`, `branch` | `{ { label, value }, ... }`；`branch = { [value] = { lines } }` 在做出选择后接入。选中的值写入 `ctx.choice` 和 `ctx.choices[value]`。 |
| `when` | 该行要播放时返回 true 的 `function(ctx)`；抛出错误的条件视为 false。 |
| `pick` | `{ { lines = { ... }, weight = n }, ... }`：按权重（默认 1）抽出一项，绝不连续两次抽到同一项，并接在该行之后。 |
| 任何其他键 | 一条舞台指令（`enter`、`bg`、`fade`、`sfx`、`flag`……），由为它注册的函数执行。 |

| `opts` 字段 | 说明 |
| --- | --- |
| `lines`, `ctx` | 脚本（运行器遍历的是它的副本），以及条件读取的表。 |
| `tr` | `fn(s)`：翻译文本、名称和选项标签；没有它时按原样显示。 |
| `speakerName` | 带有 `who` 而没有 `name` 的行所显示的名称：一个字符串、一张 `{ [who] = name }` 表，或 `fn(who, line, ctx)`。 |
| `playerTag`, `playerName` | 文本中被替换为玩家名称的标签（`"<player name>"`），以及该名称：一个字符串或 `fn(ctx)`，默认为 `ctx.playerName`。 |
| `directions`, `order` | `{ [key] = fn(line, runner) }`，以及尝试它们的顺序（`Runner.ORDER`，然后是其余已排序的键）。 |
| `onLine`, `onChoose` | 对每一条播放的行、在其指令之前调用的 `fn(line, runner)`；做出选择时（包括恢复时的选择）调用的 `fn(line, value, runner)`。 |
| `random` | 返回 [0, 1) 之间数字的 `fn()`，用于抽取（默认 `math.random`）。 |

指令不处理该行时返回 `false`（尝试顺序中的下一条），返回 nil 或 true 表示继续下一行，返回 `"hold"` 表示停止遍历直到调用方再次推进，返回 `"end"` 表示结束脚本。除非调用方注册了自己的 `text` 指令，否则带有文本或选项的行会以 `"line"` 停止遍历。

| 方法 | 说明 |
| --- | --- |
| `Runner.new(opts)  -> runner` | 基于 `opts.lines` 创建一个运行器。 |
| `runner:step()  -> verdict, line` | 前进到下一个停止点：`"line"`（要显示的正文行）、`"hold"` 或 `"end"`。 |
| `runner:present(dlg, line?)  -> node, carried` | 在一个 `VN.Dialogue` 对话框上开始当前行；只有选项的行会带上上一条正文行，并完整显示。 |
| `runner:choose(value)  -> bool` | 报告对话框的结果：记录选项的值并接入其分支。 |
| `runner:current()  -> line`, `runner:load(lines)` | 当前行；从头开始运行一份脚本。 |
| `runner:ahead(fn, otherwise)  -> value` | 对当前行之后的各行，`fn(line)` 给出的第一个答案（true 或 false）；脚本先结束时为 `otherwise`。 |
| `runner:bookmark()  -> mark` | `{ idx, picks, rolls }`：屏幕上的行、已做的选择和已抽取的结果。 |
| `runner:resume(mark, replay?)` | 按相同的选择和抽取结果静默地回到书签所在的行；`replay = { [key] = fn(line, runner) }` 重新应用会留下状态的指令（背景、标志）。下一次 `step` 显示书签所在的行。 |

辅助函数 `Runner.passes(line, ctx)`、`Runner.node(line, ctx, o, carry)`、`Runner.start(dlg, line, ctx, o, carry)`、`Runner.resolveChoice(lines, idx, ctx, value)` 和 `Runner.resolvePick(lines, idx, random, forced)` 无需运行器即可使用，供自己遍历脚本的舞台使用。

## VN.Scene

基于运行器和对话框构建的场景播放器：加载完成后显示的背景图层、一组角色精灵、屏幕淡入淡出和震动，以及带有按说话人区分的打字音效的对话框。它自己不加载任何东西：背景、角色的纹理、音乐、声音和保存的书签都来自调用方。

一个场景是 `{ bg, music, lines }`：第一张背景、音乐（否则使用背景的音乐），以及运行器格式的各行，并内置了以下指令：

| 行 | 效果 |
| --- | --- |
| `{ enter = who, expr, side }` | 一名角色登场，淡入（并滑入）；场景会等到登场完成。已在屏幕上的角色只改变表情。 |
| `{ leave = who }` | 角色退场；场景会等到退场完成。 |
| `{ expr = name }` | `who` 的表情，或当前焦点角色的表情。 |
| `{ bg = key }` | 背景，在其图层加载完成后（或 `backgrounds.wait` 秒后）显示。 |
| `{ fade = "out" or "in", keepSprite }` | 覆盖场景的淡入淡出；`keepSprite = true` 让角色保持明亮。 |
| `{ shake = true }`, `{ sfx = name }` | 屏幕震动（`shake` 也可以是振幅或 `{ amp, dur }`）；一个声音。 |
| `{ flag = name, value }` | 设置 `ctx.flags[name]`（没有 `value` 时为 true）。 |
| `{ outcome = value }` | 场景淡出并报告 `value`。 |

尚未登场的角色的台词会先让该角色淡入。调用方的某条指令返回 `"hold"` 时，场景会暂停，直到调用 `scene:continue()`，或直到传给 `scene:waitFor(fn)` 的函数返回 true（`fn(dt)` 每帧调用一次）。

| `opts` 字段 | 说明 |
| --- | --- |
| `box` | `VN.Dialogue.new` 的选项；未设置 `onBlip`、`onShake`、`onExpr` 和 `onSfx` 时由场景填入。或者用 `dialogue` 传入你自己的对话框。 |
| `cast` | `{ [who] = member }`。一名角色：`body`（一张纹理，或每种表情一张图片的 `fn(expr)`）、`face`（`fn(expr)`，在 `faceX`、`faceY` 处绘制在身体之上）、`name`、`expr`（登场时的表情）、`side`（`"left"`、`"center"`、`"right"`）或 `x`、`y` 和 `anchor`（`"top"` 或 `"bottom"`）、`scale` 或 `height`（让身体适配该像素数的缩放）、`fade`（0.35 秒）、`slide`（像素）、`bounce`（0.36 秒，0 表示无），以及表情变化时弹跳所用的 `squash`。 |
| `sides`, `focus`, `dim` | 每个位置的 x；没有说话人的 `expr` 改变的是谁的表情；未说话角色的颜色 `{ r, g, b }`（无：从不变暗）。 |
| `backgrounds` | `defs`（`{ [key] = def }`）和 `path`（没有 def 的键所用的 `"Textures/bg/%s.png"`），或 `resolve(key)`；用于调用方所拥有纹理的 `open(layer)`（借用的纹理在其所有者释放后会被重新打开）；`wait`（2 秒）；`prepare`、`show`、`update`、`drawBehind`、`drawOver`、`close` 钩子。一个 def 是一个图层或 `{ layers = { ... } }`，一个图层是 `{ path, x, y, scale, scroll }`，def 的 `music` 在其显示期间播放。 |
| `music` | `{ play(key), fadeOut(sec), playing(), update(dt), stop() }`，全部可选。 |
| `sound`, `blip`, `clickSound` | `fn(name)` 按名称播放声音；`blip(node)` 返回对话框节点（`node.speaker`）的打字音效，nil 表示静音；推进时的点击音。 |
| `cover` 或 `drawCover` | 淡入淡出用的纹理（或返回它的 `fn()`），拉伸覆盖整个屏幕；或者绘制淡入淡出的 `fn(alpha)`。 |
| `drawBox`, `fadeSec`, `outcome` | 代替 `dlg:draw()` 绘制对话框的 `fn(dlg, scene)`；淡入淡出时长（0.45 秒）；各行刚好用完时的结果（`"done"`）。 |
| `tr`, `speakerName`, `playerTag`, `playerName`, `random`, `onLine`, `order` | 传给运行器。 |
| `directions`, `replay` | 额外的指令，或内置指令的替代；恢复书签时如何重新应用它们。 |
| `onBookmark`, `hooks` | 在每一行开始时调用的 `fn(mark)`，带有一个可保存的书签；`hooks = { prose, start, expr, choose, update, draw, dispose }`。 |

| 方法 | 说明 |
| --- | --- |
| `Scene.new(opts)  -> scene` | 创建一个播放器。 |
| `scene:play(scene, ctx, onDone, resume?)` | 开始一个场景。`ctx` 由条件读取并接收选择；`onDone(result)` 得到结果；`resume` 是要从中继续的书签。 |
| `scene:update(dt)  -> "running" or "done"`, `scene:draw()` | 每帧调用。 |
| `scene:continue()`, `scene:waitFor(fn)` | 结束暂停；暂停直到 `fn(dt)` 返回 true。 |
| `scene:setBackground(key)`, `scene:setExpr(who, expr)`, `scene:shake(amp, dur)`, `scene:sound(name)` | 内置效果，供你自己的指令使用。 |
| `scene:bookmark()  -> mark`, `scene:active()  -> bool` | 运行器的书签；是否有场景正在播放。 |
| `scene:reset()`, `scene:dispose()` | 停止场景；释放场景打开的东西（以及对话框，除非它是你自己的）。 |

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

对话框通过 `VN.Dialogue` 绘制，因此它的外框跟随 [UI.Pick](ui.md) 给出的 UI 库。
