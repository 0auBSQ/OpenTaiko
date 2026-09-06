<!-- api/activities.md -->

# 模块与生命周期

引擎会在模块的 Script.lua 中调用一组固定的回调。本页列出这些回调，以及驱动活动、背景和过渡的全局对象，还有每个模块都会获得的计数器、相机和诊断辅助对象。如果你还没有写过模块，请先阅读[模块的工作原理](../getting-started.md)。

## 生命周期回调

引擎在每个模块的 Script.lua 中按名称查找顶层全局函数，并在固定的时机调用它们。引擎会跳过你未定义的回调。模块类型决定引擎调用哪些回调。

<div class="callout warn">
皮肤加载时，引擎按以下顺序创建并启动模块：Transitions、Stages、Activities、ROActivities。在每一类型内部，引擎先运行每个模块的 Script.lua（其顶层代码），然后对每个模块调用 onStart。因此舞台的 onStart 运行时，Activities 和 ROActivities 尚未加载，在那里查找会返回 nil：请在 activate 中查找它们。切换皮肤或退出时，onDestroy 先在 Stages 上运行，然后是 ROActivities 和 Activities，最后是 Transitions。
</div>

### 舞台、活动与只读活动

| 方法 | 说明 |
| --- | --- |
| `onStart()` | 引擎创建模块后调用一次；创建发生在启动时，以及每次引擎加载或重新加载皮肤时。以协程方式运行（见下文 LOADING）；在这里加载资源。 |
| `activate(...)` | 舞台：每次引擎进入舞台时以协程方式调用。活动/只读活动：宿主通过 `handle:Activate(...)` 以自己的参数调用它；返回值回到宿主。引擎会在它运行前刷新 CHARACTERLIST 和 PUCHICHARALIST 全局对象。 |
| `update(timestamp)` | 每帧在 draw 之前调用；timestamp 是以毫秒计的游戏时钟。舞台一旦调用了 Exit 就不再收到 update；draw 会在淡出期间继续运行。活动/只读活动：宿主通过 `handle:Update()` 调用它。 |
| `draw(...)` | 每帧调用。活动/只读活动：宿主通过 `handle:Draw(...)` 以自己的参数调用它；返回值回到宿主。 |
| `deactivate(...)` | 舞台：引擎离开舞台时调用。活动/只读活动：宿主通过 `handle:Deactivate(...)` 调用它，或由模块自身通过 `DEACTIVATE()` 调用；返回值回到宿主。 |
| `afterSongEnum()` | 每次歌曲枚举完成时调用，包括启动时以及歌曲软重载或硬重载之后，即使模块并未激活。 |
| `onDestroy()` | 引擎卸载皮肤前调用，以便模块释放其持有的资源。 |
| `reloadLanguage(lang)` | 游戏语言变更时对每个已加载模块调用；lang 是新的语言代码。 |

### 背景

每个界面承载自己的背景。下文的“背景”一节描述 state 参数和事件钩子。

| 方法 | 说明 |
| --- | --- |
| `onStart()` | 宿主首次激活背景时同步调用一次。 |
| `activate(state)` | 宿主每次激活背景时调用；再次激活不会重新运行 onStart。 |
| `update(timestamp, state)` | 每帧调用（演奏暂停期间除外）；timestamp 是以毫秒计的 `state.timeStamp`。 |
| `draw(state)` | 每帧调用。 |
| `reloadLanguage(lang)` | 游戏语言变更时调用。 |

引擎不会在背景上调用 afterSongEnum 或 onDestroy；宿主释放背景时，引擎会释放背景创建的资源。

### 过渡

| 方法 | 说明 |
| --- | --- |
| `onStart()` | 皮肤加载时、在舞台和活动之前以协程方式调用一次。 |
| `fadeOut(t)` | 在即将离开的舞台之上绘制淡出；t 从 0 变到 1。 |
| `loading(progress, elapsed)` | 绘制加载画面；progress 为 0 到 1，elapsed 是自加载开始以来的秒数。 |
| `fadeIn(t)` | 在新舞台之上绘制淡入；t 从 0 变到 1。 |
| `onDestroy()` | 引擎卸载皮肤前、在舞台和活动之后调用。 |
| `reloadLanguage(lang)` | 游戏语言变更时调用。 |

下文的“过渡”一节描述各阶段的时序。

### 角色

角色的 Script.lua 定义另一组函数：loadAnimation、disposeAnimation、availableAnimation、setAnimationDuration、resetAnimationCounter、update、draw、getDrawSize、getHeyaRenderOffset、getAIBattlePosition、loadVoice、disposeVoice 和 playVoice。[添加角色](../guides/characters.md)对此有详细介绍。

### Exit

引擎只在舞台脚本中注册的函数；调用它即是请求引擎离开舞台。

<div class="callout warn">
仅在 Modules/Stages 脚本中可用；活动和只读活动由宿主驱动，不会获得它。接受 0 到 3 个参数，任意位置都可以是 nil。调用本身即是请求退出；自带舞台在 update 中写 `return Exit(...)`，以便该帧内不再运行其他内容。target 为 "title"、"play"、"stage" 或 "legacy"；nil 或任何其他值都表示 "title"。target 为 "stage" 时，name 是要跳转到的 Modules/Stages 模块；target 为 "legacy" 时，name 是 "heya"、"config"、"exit" 或 "onlinelounge" 之一（其他值都会进入标题界面）。transition 指定一个 Modules/Transitions 模块；如果你省略它或引擎找不到它，引擎会使用名为 "default" 的模块，如果皮肤完全没有过渡，则播放纯黑色淡出。
</div>

| 方法 | 说明 |
| --- | --- |
| `Exit(target?, name?, transition?)  -> number` | 请求舞台退出到指定目的地，可选地指定目标模块和过渡模块；返回 0。 |

```lua
function update(timestamp)
    if INPUT:KeyboardPressed("S") then
        return Exit("stage", "demo2")          -- 跳转到 Modules/Stages/demo2
    end
    if INPUT:Pressed("Cancel") then
        return Exit("title", nil, "nokon_curtain")   -- 通过指定名称的过渡返回标题界面
    end
end
```

### LOADING

供以协程方式运行的回调使用的加载进度条辅助对象：每种模块类型的 onStart，以及舞台的 activate。

<div class="callout warn">
在每个模块中定义为 LOADING 全局对象。这些回调运行在引擎持有的协程上，引擎每帧恢复一次：一次恢复用完其时间预算后引擎会自动让出，你也可以自己用 coroutine.yield(progress) 或 LOADING:Tick(sub) 让出。你通过 LOADING:Add 注册的块会在回调主体返回后按顺序运行，每完成一个块进度条前进一次；块的 weight 是它在进度条中所占的份额（默认 1）。LOADING:Tick(sub) 在块内部让出一帧，并报告该块内 0 到 1 的进度。在协程回调之外（活动或只读活动的 activate，以及任何 update 或 draw），LOADING:Tick 会抛出 Lua 错误，因为没有可让出的目标，而通过 LOADING:Add 排队的块永远不会运行。
</div>

| 方法 | 说明 |
| --- | --- |
| `LOADING:Add(fn)  -> nil` | 注册一个在回调返回后运行的加载块。 |
| `LOADING:Add(label, fn)  -> nil` | 注册一个带标签的加载块。 |
| `LOADING:Add(label, weight, fn)  -> nil` | 注册一个带标签且显式指定权重的加载块。 |
| `LOADING:Tick(sub)  -> nil` | 在块内部让出一帧，并报告当前块内 0 到 1 的子进度。 |

```lua
function onStart()
    LOADING:Add("textures", 3, function()
        for i, name in ipairs(names) do
            tx[name] = TEXTURE:CreateTexture(name)
            LOADING:Tick(i / #names)
        end
    end)
    LOADING:Add("sounds", 1, function()
        bgm = SOUND:CreateBGM("Sounds/BGM.ogg")
    end)
end
```

## 活动

### ACTIVITY

按名称查找已加载活动的全局对象。

<div class="callout warn">
在除只读活动和背景之外的每个模块中注册为 ACTIVITY 全局对象；在只读活动和背景中它为 nil，这些模块使用 ROACTIVITY。引擎从 Modules/Activities/{name} 加载活动。ACTIVITY 也公开了 GetROActivity，其行为与 ROACTIVITY:GetROActivity 相同。
</div>

| 方法 | 说明 |
| --- | --- |
| `ACTIVITY:GetActivity(name)  -> activity handle` | 返回具有给定文件夹名称的已加载活动的句柄；未加载时返回 nil。 |
| `ACTIVITY:GetROActivity(name)  -> activity handle` | 与 ROACTIVITY:GetROActivity 相同。 |

### ROACTIVITY

按名称查找已加载只读活动（ROActivity）的全局对象。

<div class="callout warn">
在每个模块中注册为 ROACTIVITY 全局对象。引擎从 Modules/ROActivities/{name} 加载只读活动，并给它们只读的 CONFIG、DATABASE 和 GetSaveFile 全局对象，因此其脚本无法更改游戏状态（见“模块的工作原理”中的只读模块一节）。活动和只读活动都是以名称为键的单例：每个文件夹一个实例，由所有宿主共享。
</div>

| 方法 | 说明 |
| --- | --- |
| `ROACTIVITY:GetROActivity(name)  -> activity handle` | 返回具有给定文件夹名称的已加载只读活动的句柄；未加载时返回 nil。 |

### 活动句柄

ACTIVITY:GetActivity 和 ROACTIVITY:GetROActivity 返回的对象。宿主用它来驱动模块的回调。

<div class="callout warn">
Activate、Deactivate 和 Draw 把它们的参数转发给模块的 activate、deactivate 和 draw 回调。Update 以当前游戏时间（毫秒）调用 update。它们每个都把回调的返回值作为从 0 起索引的数组返回；回调没有返回值或未定义时返回 nil；用 `result[0]` 读取第一个值。Call 可以调用模块脚本定义的任意全局函数。
</div>

| 方法 | 说明 |
| --- | --- |
| `handle.IsActive  -> boolean` | Activate 运行后为 true，直到 Deactivate（或模块自身的 DEACTIVATE()）运行为止。 |
| `handle:Activate(...)  -> array` | 以给定参数调用模块的 activate 回调。 |
| `handle:Deactivate(...)  -> array` | 以给定参数调用模块的 deactivate 回调。 |
| `handle:Update()  -> array` | 以当前游戏时间（毫秒）调用模块的 update 回调。 |
| `handle:Draw(...)  -> array` | 以给定参数调用模块的 draw 回调。 |
| `handle:Call(functionName, ...)  -> array` | 以给定参数调用模块脚本中指定名称的全局函数。 |

```lua
local act = nil

function activate()
    if act == nil then act = ACTIVITY:GetActivity("song_select_core") end
    act:Activate()
end

function update(timestamp)
    local result = act:Update()
    local signal = result ~= nil and result[0] or nil
    if signal == "play" then return Exit("play", nil) end
    if signal == "cancel" then return Exit("title", nil) end
end

function draw()
    act:Draw()
end

function deactivate()
    act:Deactivate()
end
```

### DEACTIVATE

引擎在活动和只读活动脚本内部注册的函数；它让模块可以自行停用。

<div class="callout warn">
调用它会把模块标记为未激活（handle.IsActive 变为 false），并运行模块自己的 deactivate 回调。自带的对话框在玩家确认或取消时调用它，宿主通过观察 IsActive 得知对话框已关闭。
</div>

| 方法 | 说明 |
| --- | --- |
| `DEACTIVATE(...)` | 停用当前模块，并以给定参数调用其 deactivate 回调。 |

### 由引擎承载的只读活动

引擎按固定的文件夹名称查找某些只读活动并自行驱动它们。皮肤提供同名的 Modules/ROActivities 文件夹即可替换其中之一；它必须定义的回调由下面列出的引擎调用点决定。缺少某一个时，引擎就不会绘制对应的功能。

| 名称 | 引擎调用的内容 |
| --- | --- |
| `nameplate` | 玩家的名牌变更时调用 `activate(player, name, title, dan, data)`；每帧调用一次 `update()`；调用 `draw(mode, ...)`，其中 mode 0 = 完整名牌 `(x, y, opacity, player, side)`，1 = 段位牌 `(x, y, opacity, danGrade, textTexture)`，2 = 称号牌 `(x, y, opacity, type, textTexture, rarity, nameplateId)`。 |
| `modal` | 对每个排队的解锁弹窗调用 `activate(player, rarity, modalType, ...)`，然后每帧调用 `update()` 和 `draw()`。脚本调用 DEACTIVATE() 关闭弹窗；引擎随后激活下一个。 |
| `modicons` | 调用一次 `activate()`，然后调用 `draw(x, y, player, layout, alpha)`，layout 为 "menu" 或 "game"。MODICONS 全局对象封装了它。 |
| `danplate` | 在结算界面和段位课程中调用 `draw(x, y, opacity, danTick, r, g, b, titleText)`。 |
| `popup_menu` | 调用 `activate(title, items, fontSize, ...)`，其中 items 是用换行符连接的标签，其后是 PopupMenu 的皮肤位置；每帧调用 `draw(selected)`；关闭时调用 `deactivate()`。 |
| `config_ui` | 以设置模型调用 `activate(model)`；每帧调用 `update()`，返回 "exit" 即离开设置界面；调用 `draw()`；引擎重建模型时通过 Call 调用 `reload(model)`；调用 `deactivate()`。 |
| `song_enum` | 调用 `activate()`，然后在歌曲扫描进行期间每帧调用 `draw(isCommandSongDataGet, done, total)`；调用 `deactivate()`。 |

## 背景

### 背景模块

绘制一个界面背景、演奏图层、观众、过关动画或花球效果的 Script.lua，由引擎自身的界面承载。

<div class="callout warn">
背景位于 Modules 文件夹之外，在皮肤的 Graphics 文件夹下、其所装饰界面的目录中，例如 Graphics/0_Startup/Script.lua、Graphics/10_Heya/Script.lua、Graphics/6_Result/Script.lua、Graphics/5_Game/5_Background/Normal/Up/{variant}/Script.lua、Graphics/5_Game/5_Background/Normal/Down/{variant}/Script.lua、Graphics/5_Game/3_Mob/{variant}/Script.lua、Graphics/5_Game/9_End/{result}/Script.lua 和 Graphics/5_Game/11_Balloon/Kusudama/Script.lua。当一个文件夹包含多个变体时，引擎每次演奏随机挑选一个（或按谱面的场景预设挑选）。宿主界面创建背景实例（演奏背景在每次引擎进入游戏界面时创建）并随界面一起释放，因此演奏期间会同时存在多个实例。背景脚本获得与只读活动相同的全局对象（只读的 CONFIG、DATABASE 和 GetSaveFile；没有 ACTIVITY）。下面的事件钩子是可选的，引擎在对应事件发生时调用每个钩子一次。
</div>

| 方法 | 说明 |
| --- | --- |
| `clearIn(player)` | 演奏的 Up 和 Down 背景：该玩家的魂槽进入了过关区域。 |
| `clearOut(player)` | 演奏的 Up 和 Down 背景：该玩家的魂槽跌出了过关区域。 |
| `playEndAnime(player)` | 过关动画（Graphics/5_Game/9_End）：该玩家的结束动画开始。 |
| `kusuIn()` / `kusuBroke()` / `kusuMiss()` | 花球：花球出现、玩家击破它，或玩家未能击破它。 |
| `skipAnime()` | 结算背景：玩家跳过了结算动画。 |

### 背景状态

宿主传给背景的 activate、update 和 draw 的对象。

<div class="callout warn">
每个宿主一个实例，由宿主每帧就地更新。数组字段与引擎的按玩家数组共享，并从 0 起索引（`state.gauge[0]` 是玩家 1）。只有演奏宿主会刷新演奏字段；其他宿主将它们保持为默认值，且演奏之外 timeStamp 保持为 -1。state 不携带帧时序：请读取 fps 全局对象。
</div>

| 方法 | 说明 |
| --- | --- |
| `state.playerCount  -> number` | 玩家数量。 |
| `state.p1IsBlue  -> boolean` | 玩家 1 使用蓝色侧时为 true。 |
| `state.lang  -> string` | 当前语言代码。 |
| `state.simplemode  -> boolean` | 简易模式开启时为 true。 |
| `state.puchicharaRarities  -> string[]` | 每个玩家的小角色稀有度。 |
| `state.characterRarities  -> string[]` | 每个玩家的角色稀有度。 |
| `state.isClear  -> boolean[]` | 每个玩家当前是否处于过关区域。 |
| `state.gauge  -> number[]` | 每个玩家的魂槽值。 |
| `state.bpm  -> number[]` | 每个玩家当前的 BPM。 |
| `state.gogo  -> boolean[]` | 每个玩家是否处于 GoGo 时间。 |
| `state.towerNightNum  -> number` | 塔模式的昼夜系数，0 到 1。 |
| `state.battleState  -> number` | AI 对战状态代码。 |
| `state.battleWin  -> boolean` | 玩家在 AI 对战中领先时为 true。 |
| `state.timeStamp  -> number` | 与谱面同步的时间（秒）；演奏之外为 -1。 |
| `state.paused  -> boolean` | 演奏暂停期间为 true。 |
| `state.player  -> number` | 按玩家承载的宿主（过关动画）所绘制的玩家。 |

## 过渡

### 过渡模块

绘制两个舞台之间的淡出、加载和淡入阶段的 Modules/Transitions/{name}/Script.lua。

<div class="callout warn">
Exit 的第三个参数选择过渡；调用未指定名称或引擎找不到该名称时，引擎使用 "default"。Exit("play") 之后进入演奏的加载始终使用名为 "song_loading" 的过渡，皮肤没有它时使用 "default"。引擎按顺序驱动各阶段：在即将离开的舞台之上每帧调用 fadeOut(t) 直到 t 达到 1，然后卸载该舞台并加载新舞台，期间每帧调用 loading(progress, elapsed)，然后在新舞台之上调用 fadeIn(t) 直到 t 达到 1。每段淡入淡出持续 0.5 秒，除非脚本设置了 FADE_OUT_SECONDS 或 FADE_IN_SECONDS；引擎会忽略不是正数的值。切换舞台时，只有当加载耗时超过 0.5 秒后引擎才会调用 loading；在此之前它调用的是 fadeOut(1)，以免短暂的加载闪现加载画面。歌曲加载路径则会立即显示加载阶段。
</div>

| 方法 | 说明 |
| --- | --- |
| `FADE_OUT_SECONDS  -> number` | 可选的顶层全局变量：淡出的长度（秒）。 |
| `FADE_IN_SECONDS  -> number` | 可选的顶层全局变量：淡入的长度（秒）。 |

```lua
FADE_OUT_SECONDS = 0.3
FADE_IN_SECONDS = 0.3

local pixel = nil

local function cover(alpha)
    pixel:SetColor(0, 0, 0)
    pixel:SetOpacity(alpha)
    pixel:SetScale(8000, 8000)
    pixel:Draw(0, 0)
end

function onStart()
    pixel = TEXTURE:CreateTexture("pixel.png")
end

function fadeOut(t) cover(t) end
function fadeIn(t) cover(1.0 - t) end
function loading(progress, elapsed) cover(1.0) end

function onDestroy()
    if pixel ~= nil then pixel:Dispose() end
end
```

## 时序与相机

### COUNTER

动画计数器的工厂，计数器让一个值随时间从起始值变化到结束值。

<div class="callout warn">
注册为 COUNTER 全局对象。计数器只在你调用 Tick 时前进，前进量为帧间隔除以 interval，其中 interval 是每单位值所需的秒数。interval 的符号必须与方向一致：end 大于 begin 时为正，小于时为负。符号不一致时，计数器会交换两端并在第一次 tick 时就结束。CreateCounterDuration 接受总时长，并自行决定符号。interval 为零或 begin 与 end 相等时，计数器在第一次 tick 时就结束。值到达终点时计数器调用可选的 ended 函数；循环或往返模式下每完成一个周期调用一次。
</div>

| 方法 | 说明 |
| --- | --- |
| `COUNTER:CreateCounter(begin, end, interval, ended?)  -> counter` | 创建一个以每单位 interval 秒的速度从 begin 变化到 end 的计数器，完成时调用 ended。 |
| `COUNTER:CreateCounterDuration(begin, end, seconds, ended?)  -> counter` | 创建一个在给定秒数内从 begin 变化到 end 的计数器；seconds 不为正或 begin 等于 end 时返回空计数器。 |
| `COUNTER:EmptyCounter()  -> counter` | 创建一个值始终为 0 的惰性计数器，可用作占位符。 |

### 计数器句柄

由 COUNTER 创建的计数器。

<div class="callout warn">
每帧读取 Value 并每帧调用 Tick 使其前进；你尚未启动或已停止的计数器会忽略 Tick。Begin、End 和 Interval 是可读写的字段。SetLoop 和 SetBounce 互斥。SetEasing 只改变报告出来的 Value 的形状；计数器底层仍然线性前进。监听器在每次 tick（包括最后一次）时收到当前值。
</div>

| 方法 | 说明 |
| --- | --- |
| `counter.Value  -> number` | 当前值，若设置了缓动则已应用；对其赋值会让计数器跳转。 |
| `counter.Begin  -> number` | 起始值（可读写）。 |
| `counter.End  -> number` | 结束值（可读写）。 |
| `counter.Interval  -> number` | 每单位值所需的秒数（可读写）。 |
| `counter:Start()  -> nil` | 把值重置为 Begin 并开始计时。 |
| `counter:Resume()  -> nil` | 开始计时但不重置值。 |
| `counter:Stop()  -> nil` | 停止计时。 |
| `counter:Pause()  -> nil` | 与 Stop 相同。 |
| `counter:Reset()  -> nil` | 把值设回 Begin，不改变是否在计时。 |
| `counter:Tick()  -> nil` | 让值前进一帧，并视情况调用监听器和 ended 函数。 |
| `counter:SetLoop(loop)  -> nil` | 值到达终点时回绕到 Begin；关闭往返。 |
| `counter:SetBounce(bounce)  -> nil` | 值到达任一端时反转方向；关闭循环。 |
| `counter:GetLoop()  -> boolean` | 是否开启了循环。 |
| `counter:GetBounce()  -> boolean` | 是否开启了往返。 |
| `counter:SetEasing(type, function)  -> nil` | 对报告出来的值应用缓动曲线；type 为 IN、OUT、INOUT 或 OUTIN，function 为 LINEAR、SINE、QUAD、CUBIC、QUART、QUINT、EXPO、CIRC、ELASTIC、BACK 或 BOUNCE（不区分大小写；计数器会忽略未知名称）。 |
| `counter:ClearEasing()  -> nil` | 移除缓动。 |
| `counter:Listen(listener)  -> nil` | 注册一个在每次 tick 时以当前值调用的函数。 |
| `counter:ClearListeners()  -> nil` | 移除所有监听器。 |

```lua
local fade = nil

function activate()
    fade = COUNTER:CreateCounterDuration(0, 1, 0.5, function() debugLog("fade done") end)
    fade:SetEasing("OUT", "QUAD")
    fade:Start()
end

function update(timestamp)
    fade:Tick()
end

function draw()
    background:SetOpacity(fade.Value)
    background:Draw(0, 0)
end
```

### GLOBALCAMERA

全屏 2D 相机：它平移、缩放和旋转整个渲染出的画面，并添加一个逐渐衰减的屏幕震动。

<div class="callout warn">
注册为 GLOBALCAMERA 全局对象。它驱动的屏幕变换与 TJA 的 #CAMERA 命令相同，影响所有绘制的内容，包括位块传送的 3D 场景。偏移以 1280x720 参考像素为单位，旋转以度为单位，缩放为 1 表示不缩放。基础变换会一直保持直到你更改它：每帧调用 Update(dt) 以应用它并推进震动，离开舞台时调用 Reset() 以免带到下一个舞台。3D 场景自己的相机由你在场景对象上设置。
</div>

| 方法 | 说明 |
| --- | --- |
| `GLOBALCAMERA:SetOffset(x, y)  -> nil` | 把屏幕平移 (x, y) 像素。 |
| `GLOBALCAMERA:SetZoom(sx, sy)  -> nil` | 以独立的 X 和 Y 系数缩放屏幕。 |
| `GLOBALCAMERA:SetUniformZoom(s)  -> nil` | 等比缩放屏幕。 |
| `GLOBALCAMERA:SetRotation(deg)  -> nil` | 绕屏幕中心旋转屏幕。 |
| `GLOBALCAMERA:GetOffsetX()  -> number` | 基础 X 偏移。 |
| `GLOBALCAMERA:GetOffsetY()  -> number` | 基础 Y 偏移。 |
| `GLOBALCAMERA:GetZoomX()  -> number` | X 缩放系数。 |
| `GLOBALCAMERA:GetZoomY()  -> number` | Y 缩放系数。 |
| `GLOBALCAMERA:GetRotation()  -> number` | 基础旋转角度（度）。 |
| `GLOBALCAMERA:Shake(amplitudePx, seconds, rotAmpDeg?)  -> nil` | 开始一次震动，从给定的像素振幅在给定秒数内线性衰减，可选地附带以度为单位的旋转晃动。相机会忽略 seconds 不大于 0 的调用；新的震动只有在振幅等于或大于当前震动时才会替换它。 |
| `GLOBALCAMERA.IsShaking  -> boolean` | 震动仍在衰减时为 true。 |
| `GLOBALCAMERA:Update(dt)  -> nil` | 把震动推进 dt 秒（上限 0.25），并把基础变换加上震动应用到屏幕。 |
| `GLOBALCAMERA:Reset()  -> nil` | 让相机回到中心，重置缩放和旋转，并停止所有震动。 |

```lua
function update(timestamp)
    if INPUT:Pressed("LRed") or INPUT:Pressed("RRed") then GLOBALCAMERA:Shake(18, 0.35) end
    GLOBALCAMERA:Update(fps.deltaTime)
end

function deactivate()
    GLOBALCAMERA:Reset()
end
```

## 歌曲枚举

两个全局函数报告歌曲扫描的状态。请与 afterSongEnum 回调配合使用。

| 方法 | 说明 |
| --- | --- |
| `IsSongsEnumerating()  -> boolean` | 歌曲枚举正在进行时为 true。 |
| `IsSongsEnumDone()  -> boolean` | 歌曲扫描完成后为 true。扫描开始之前和进行期间为 false；IsSongsEnumerating 在未开始和已完成两种状态下都为 false，因此请用本函数判断列表是否就绪。 |

## 诊断

### info

包含基础游戏状态和模块自身目录的只读对象。

<div class="callout warn">
注册为 info 全局对象，按模块创建。每个字段在访问时计算其值；online 在你每次读取时都会查询操作系统。
</div>

| 方法 | 说明 |
| --- | --- |
| `info.playerCount  -> number` | 配置的玩家数量。 |
| `info.lang  -> string` | 当前语言代码。 |
| `info.simplemode  -> boolean` | 简易模式开启时为 true。 |
| `info.p1IsBlue  -> boolean` | 玩家 1 使用蓝色侧时为 true。 |
| `info.online  -> boolean` | 存在可用的网络接口时为 true。 |
| `info.dir  -> string` | 本模块的目录。 |

### fps

包含帧时序和高分辨率时钟的只读对象。

| 方法 | 说明 |
| --- | --- |
| `fps.deltaTime  -> number` | 自上一帧以来经过的秒数。 |
| `fps.fps  -> number` | 当前测得的每秒帧数。 |
| `fps.ms  -> number` | 以毫秒计的单调时钟，通过求差来为 Lua 代码段计时。 |

### debugLog

| 方法 | 说明 |
| --- | --- |
| `debugLog(message)  -> nil` | 把字符串写入引擎跟踪日志，并加上标记为 Lua 日志的前缀。 |

## 其他全局对象

引擎在每个模块中注册这些全局对象；它们各自的页面对其有详细记录。

| 全局对象 | 页面 |
| --- | --- |
| `GetSaveFile(player)` | [玩家与档案](players.md)。在只读活动和背景中返回只读句柄。 |
| `RequestSongList(settings)`、`GenerateSongListSettings()` | [歌曲与谱面](songs.md)。 |
| `MODICONS` | [歌曲与谱面](songs.md)。 |
| `CONFIG`、`DATABASE`、`SHARED`、`STORAGE`、`JSONLOADER`、`INILOADER`、`SQL` | [数据与持久化](data.md)。 |
| `TEXTURE`、`CANVAS`、`GRAPHICS`、`TEXT`、`VIDEO`、`COLOR`、`GRADIENT`、`SIZE` | [图形与文本](graphics.md)。 |
| `SOUND`、`HITSOUNDSLIST` | [音频](audio.md)。 |
| `INPUT` | [输入](input.md)。 |
| `NAMEPLATE`、`NAMEPLATESLIST`、`CHARACTER`、`CHARACTERLIST`、`PUCHICHARALIST`、`PLAYSTATE`、`THEME`、`LANG` | [玩家与档案](players.md)。 |
| `VECTOR`、`VECTOR2`、`VECTOR3`、`VECTOR4`、`MATRIX`、`MATRIX2`、`MATRIX3`、`MATRIX4`、`QUATERNION` | [数学](math.md)。 |
| `SONGMOUNT`、`REPLAY`、`DANBUILDER`、`VIRTUALSLOTS` | [歌曲与谱面](songs.md)。 |
| `NET` | [在线联机](networking.md)。 |
| `SCENE3D`、`MODEL`、`PHYSICS`、`COLLIDERS`、`PATHFIND`、`HEIGHTMAP` | [3D 引擎：光栅化世界](3d.md), [3D 引擎：光线追踪世界](3d-raytrace.md), [3D 引擎：物理](3d-physics.md) <span class="badge-exp">实验性</span>。 |
