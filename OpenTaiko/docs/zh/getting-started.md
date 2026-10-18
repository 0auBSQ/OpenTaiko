<!-- getting-started.md -->

# 模块的工作原理

OpenTaiko 0.6.1 允许皮肤用 Lua 添加和替换界面。皮肤的 Modules 文件夹中每个模块占一个文件夹，每个模块都有一个 Script.lua，其中定义一组固定名称的全局回调函数。游戏把每个 Script.lua 加载到各自独立的沙箱 Lua 状态中，注册引擎全局对象（TEXTURE、SOUND、INPUT、CONFIG 以及 [API 参考](api/README.md)中记录的其他对象），并在合适的时机调用这些回调。[模块与生命周期](api/activities.md)列出了确切的函数签名。

## 开始之前

- OpenTaiko 0.6.1，以及一个带有 Modules 目录的皮肤文件夹。自带皮肤位于 System/Open-World Memories。
- 一个文本编辑器和基础的 Lua 知识（函数、表、require）。
- System/Open-World Memories/Modules/Stages 和 Global/Modules/Stages 下的自带舞台。Global/Modules/Stages/demo_overall 中的小演示展示了回调的形态；较大的舞台展示了真实界面的组织方式。

## 模块存放在哪里

每种模块类型在 Modules 下都有自己的文件夹，每个模块是一个文件夹，其名称即模块的 id：

```
Modules/
  Stages/        <name>/Script.lua   完整界面
  Activities/    <name>/Script.lua   由舞台驱动的子界面
  ROActivities/  <name>/Script.lua   只读的子界面与覆盖层
  Transitions/   <name>/Script.lua   舞台之间的淡入淡出（最先加载）
  Lib/           可通过 require 访问的共享 .lua 文件；不会被当作模块扫描
```

入口文件始终是 Script.lua。你传给 TEXTURE、SOUND、VIDEO 和其他加载器的资源路径相对于模块文件夹；按照惯例，自带模块把它们放在 Textures、Sounds、Videos 和 Databases 子文件夹中，并把翻译放在 lang 文件夹中。

有两类脚本位于别处：

- 背景（界面背景、演奏图层、观众、过关动画、花球）是皮肤 Graphics 文件夹下、位于其所装饰界面目录中的 Script.lua 文件。参见[模块与生命周期](api/activities.md)的“背景”一节。
- 角色是 Global/Characters 下的文件夹。角色文件夹可以自带 Script.lua；没有的话，游戏使用内置的角色脚本。参见[添加角色](guides/characters.md)。

## Script.lua 定义全局函数

模块的 Script.lua 定义固定名称的顶层全局函数，游戏把每一个都当作全局变量读取。包在局部表中再返回的函数游戏永远找不到；拼错的名称（例如把 onStart 写成 OnStart）游戏也永远不会调用，因为它把未定义的回调视为空操作，并且不报告任何信息。文件中的其他内容都可以是局部的，你也可以用 require 把模块拆分到多个文件中。

下面是可供复制的最小形态：

```lua
-- Modules/Stages/mystage/Script.lua
local text = nil
local textTex = nil

function onStart()          -- 皮肤加载时调用一次：在这里加载资源
    text = TEXT:Create(16)
end

function activate()         -- 每次进入舞台时调用
    textTex = text:GetText("Hello")
end

function update(timestamp)  -- 每帧调用：处理输入与状态变化
    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        return Exit("title", nil)
    end
end

function draw()             -- 每帧调用：只做绘制
    if textTex ~= nil then textTex:Draw(200, 200) end
end

function deactivate()       -- 离开舞台时调用：停止声音、关闭数据库
end

function onDestroy()        -- 皮肤卸载前调用：释放你创建的对象
    if textTex ~= nil then textTex:Dispose() end
end
```

## 舞台的生命周期

- onStart()：游戏在皮肤加载完成后调用它一次，此后每次重新加载皮肤时再次调用，无论舞台是否显示在屏幕上。在这里加载纹理、声音和视频。它以协程方式运行，因此耗时较长的加载可以借助 LOADING 辅助对象分散到多帧中，在加载进度条后面进行。
- activate()：游戏在每次进入舞台时调用它。在这里重置每次访问的状态、开始播放音乐并打开数据库。它同样以协程方式运行，并且可以使用 LOADING。游戏会在 activate 运行前刷新角色和小角色列表（CHARACTERLIST、PUCHICHARALIST），因此请在这里读取它们；onStart 在该刷新之前运行。
- update(timestamp)：游戏每帧在 draw 之前调用它，并传入以毫秒计的游戏时钟。在这里处理输入并改变状态。舞台一旦调用了 Exit，游戏就停止调用 update，而在淡出期间继续调用 draw。
- draw()：游戏每帧调用它。只做绘制，并尽量减少每帧的内存分配。
- deactivate()：游戏在离开舞台时调用它。demo_overall 的数据库演示在这里释放数据库；它的第一个演示在这里停止音乐和视频。
- afterSongEnum()：游戏在每次歌曲枚举完成时调用它，包括启动时以及软重载或硬重载之后，即使舞台并未激活。当模块依赖歌曲列表时使用它。
- onDestroy()：游戏在卸载皮肤之前调用它。demo_overall 的第一个演示在这里释放它的纹理、视频、文本纹理和声音。
- reloadLanguage(lang)：游戏在语言变更时调用它（见下文的本地化一节）。

皮肤加载时，游戏按类型逐一创建模块：先是 Transitions，然后是 Stages、Activities 和 ROActivities。在每一类型内部，它先运行所有的 Script.lua，再调用各自的 onStart。因此舞台的 onStart 运行时，Activities 和 ROActivities 尚不存在；请在 activate 中查找它们。

其他类型使用这组回调的变体。Activities 和 ROActivities 拥有相同的回调，但由承载它们的舞台调用 activate、deactivate、draw 和 update 并接收其返回值。背景在 activate(state)、update(timestamp, state) 和 draw(state) 中收到一个状态对象，并可以定义 clearIn、playEndAnime 和 kusuBroke 等事件钩子。过渡定义 fadeOut(t)、loading(progress, elapsed) 和 fadeIn(t)。角色定义自己的一组动画和语音函数。[模块与生命周期](api/activities.md)列出了全部这些回调。

## 选择模块类型

- 舞台（Modules/Stages）：游戏切换到的完整界面。它拥有整帧画面，处理输入，并通过调用 Exit 离开。凡是自成一个界面的内容都用它。
- 活动（Modules/Activities）：舞台在内部使用的子界面，例如对话框。它是一个单例，你通过 ACTIVITY:GetActivity(name) 查找它；宿主舞台调用它的 Activate、Update、Draw 和 Deactivate。用于可能写入游戏状态的共享组件。
- 只读活动（Modules/ROActivities）：活动的只读形式，你通过 ROACTIVITY:GetROActivity(name) 查找它。它获得只读的 CONFIG、DATABASE 和 GetSaveFile，且没有 ACTIVITY 全局对象。用于只读取状态的组件，这涵盖了大多数可复用的 UI。引擎自身的若干覆盖层也以固定名称的只读活动形式承载（nameplate、modal、modicons、danplate、popup_menu、config_ui、song_enum、cutscene）；皮肤只要提供同名文件夹并保留引擎调用的回调，即可替换其中之一。
- 背景：Graphics 下的 Script.lua，绘制在引擎某个界面的背后或之上。背景获得与只读活动相同的只读全局对象。
- 过渡（Modules/Transitions）：游戏在舞台之间播放的淡出、加载和淡入。舞台通过 Exit 的第三个参数按名称选择过渡；舞台未指定名称或名称不存在时，游戏回退到名为 default 的过渡，进入演奏时则播放名为 song_loading 的过渡。
- 角色：参见[添加角色](guides/characters.md)。

## 使用 Exit 离开舞台

只有舞台拥有 Exit 全局函数。它最多接受三个参数，任意位置都可以是 nil：目标（"title"、"play"、"stage" 或 "legacy"；nil 表示 "title"），目标为 "stage" 时的目的舞台名称（目标为 "legacy" 时则是一个旧版界面键），以及过渡模块的名称。自带舞台在 update 中写 `return Exit(...)`，以便该帧内不再运行其他内容。

```lua
-- 位于某个舞台的 update() 内
if INPUT:KeyboardPressed("S") == true then
    sounds.Skip:Play()
    return Exit("stage", "mystage") -- 跳转到 Modules/Stages/mystage
end
-- ...
return Exit("title", nil)           -- 返回标题界面
```

## 沙箱

每个 Script.lua 都运行在受限的 Lua 状态中：

- os 只保留 time、date 和 difftime。沙箱移除了 io、debug、loadfile 和 dofile，import 不做任何事。
- package 缩减为一个自定义加载器：package.path 和 package.cpath 为空，沙箱替换了标准搜索器，因此只有下面列出的路径可以被搜索。
- require 先在模块自己的文件夹中查找，再在皮肤的 Modules/Lib 文件夹中查找，然后在游戏文件夹的 Global/Modules/Lib 中查找，并加载找到的第一个文件。Global/Modules 中的模块会先查找 Global/Modules/Lib，再查找皮肤的 Modules/Lib。同名的模块文件和 Lib 文件会解析为模块文件。名称中的点会变成路径分隔符，因此 require("DBControllers.dbScores") 和 require("DBControllers/dbScores") 都会加载 DBControllers/dbScores.lua。查找不区分大小写，非 ASCII 路径也可以正常使用。
- Lib 文件运行在 require 它的模块的 Lua 状态中，因此其中的 require 遵循该模块的顺序。游戏在 Global/Modules/Lib 中自带一套 Lib 文件库，使用带命名空间的名称，例如 require("Core.Easing") 和 require("UI.Pick")；[内置模块](modules/README.md)对它们做了说明。

```lua
-- 摘自 intro_nokon/Script.lua
local DBScores  = require("DBControllers/dbScores")  -- 模块自己的子文件夹
local I18N      = require("i18n")                     -- Modules/Lib/i18n.lua
local Opening   = require("opening")                  -- 模块文件夹
local Dialogue  = require("nokon_dialogue")           -- 模块文件夹
```

## 只读模块

游戏在只读活动和背景的 Script.lua 运行之前，就以受限的全局对象创建它们：CONFIG 是只读视图，GetSaveFile(player) 返回只读的存档，DATABASE 打开只读的存储，ACTIVITY 为 nil（请改用 ROACTIVITY）。通过这些对象进行的写入会记录一条错误通知，不做任何事，也不抛出 Lua 错误。需要更改设置、存档数据或数据库的模块必须是活动或舞台。

## 通过 lang/ 进行本地化

模块用内置模块 [Locale.i18n](modules/locale.md) 而不是皮肤的 Locales 文件夹来翻译自己的字符串，因此译文随模块一起移动。它的文本放在 Script.lua 旁边的 lang/<code>/<name>.json 中，每个功能一个文件，以稳定的 id 为键；lang/en 存放英文，并作为所有其他语言的回退。

这个库的主要函数：

- texts(name) 返回模块自己文件夹中 lang/<code>/<name>.json 的文本。Lib 文件把自己的文件夹作为第二个参数传入（见[内置模块](modules/README.md)）。
- detect() 通过全局对象 LANG 读取当前游戏语言，此后的每次查找都使用它。在你调用它之前，查找使用英文。
- texts:tr(id) 返回 id 的文本，没有任何语言包含它时返回 id 本身；texts:trf(id, ...) 把该文本用作 string.format 模式。

在 activate 中调用 detect()，然后用 texts:tr 和 texts:trf 构建文本。activate 在每次进入模块时运行，因此玩家在设置中更改的语言会在下次进入时生效，模块不需要其他钩子。每个语言文件都与英文文件拥有相同的键，译文必须原样保留 %s 之类的占位符。

```
Modules/Stages/mystage/lang/en/quiz.json   { "speaker": "Nokon", "start": "Alright, quiz time!" }
Modules/Stages/mystage/lang/ja/quiz.json   { "speaker": "ノコン", "start": "さあ、クイズの時間である！" }
```

```lua
-- Modules/Stages/mystage/Script.lua
local I18N = require("Locale.i18n")
local T = I18N.texts("quiz")
local title

function activate()
    I18N.detect()
    title = T:tr("start")
end
```

自带皮肤中一些较旧的舞台（例如 intro_nokon）改用这个库的第一种形式：一个返回从每个英文字符串映射到其译文的表的 lang/ja.lua，用 I18N.tr(s) 读取。[Locale.i18n](modules/locale.md) 对两种形式都做了说明。

语言变更时，游戏还会对每个已加载模块调用全局函数 reloadLanguage(lang)。只有在语言变更期间仍停留在屏幕上的模块才需要它，例如带有语言选择器的界面；在那里再次调用 detect() 并重建预渲染的文本。

## 注意事项

- 游戏在释放模块时会释放该模块创建的纹理、声音、视频和文本对象，因此重新加载皮肤不会泄漏它们。每次访问时打开的资源（例如数据库）请像 demo_overall 的数据库演示那样在 deactivate 中释放；你创建的对象请像它的第一个演示那样在 onDestroy 中释放。
- GetText 会在其文本对象上为每个不同的字符串缓存一张纹理。每帧都在变化的字符串会每帧新增一张纹理，游戏会逐渐变慢。请用字形渲染器（TEXT:CreateGlyphCached）绘制变化的数值，或者在数值变化之前一直保留同一张纹理。
- LOADING 只在以协程方式运行的回调中可用：任意模块的 onStart，以及舞台的 activate。在活动的 activate 中，或者在 update 或 draw 中调用 LOADING:Tick 会抛出 Lua 错误。
- onStart 和 afterSongEnum 会在模块不在屏幕上时运行。编写时要保证它们在舞台不可见的情况下也能正常工作。
