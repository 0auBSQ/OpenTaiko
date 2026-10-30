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

角色的 Script.lua 定义另一组函数：loadAnimation、disposeAnimation、availableAnimation、setAnimationDuration、resetAnimationCounter、update、draw、getDrawSize、getAIBattlePosition、loadVoice、disposeVoice 和 playVoice。[添加角色](../guides/characters.md)对此有详细介绍。

### Exit

引擎只在舞台脚本中注册的函数；调用它即是请求引擎离开舞台。

<div class="callout warn">
仅在 Modules/Stages 脚本中可用；活动和只读活动由宿主驱动，不会获得它。接受 0 到 3 个参数，任意位置都可以是 nil。调用本身即是请求退出；自带舞台在 update 中写 `return Exit(...)`，以便该帧内不再运行其他内容。target 为 "title"、"play"、"stage" 或 "legacy"；nil 或任何其他值都表示 "title"。target 为 "stage" 时，name 是要跳转到的 Modules/Stages 模块；target 为 "legacy" 时，name 是 "heya"、"config"、"exit" 或 "onlinelounge" 之一（其他值都会进入标题界面）。transition 指定一个 Modules/Transitions 模块；如果你省略它或引擎找不到它，引擎会使用名为 "default" 的模块，如果皮肤完全没有过渡，则播放纯黑色淡出。舞台和过渡的名称优先取调用方一侧：皮肤舞台先找到皮肤中该名称的模块，Global/Modules 中的舞台先找到全局模块，各自再回退到另一侧。"heya" 键作为别名保留：它会打开皮肤在 Modules/Stages 中的 `myroom` 模块，皮肤没有该模块时则进入标题界面。
</div>

| 方法 | 说明 |
| --- | --- |
| `Exit(target?, name?, transition?)  -> number` | 请求舞台退出到指定目的地，可选地指定目标模块和过渡模块；返回 0。 |

```lua
function update(timestamp)
    if INPUT:KeyboardPressed("S") then
        return Exit("stage", "mystage")        -- 跳转到 Modules/Stages/mystage
    end
    if INPUT:Pressed("Cancel") then
        return Exit("title", nil, "nokon_curtain")   -- 通过指定名称的过渡返回标题界面
    end
end
```

### STAGES

用于舞台命名空间的全局对象：舞台声明一个命名空间，任何模块都能列出某个命名空间的舞台，例如用来制作为它而做的舞台菜单。

<div class="callout warn">
在每个模块中注册为全局 STAGES，但只有舞台脚本才有命名空间：在其他模块中，SetNamespace 什么也不做并返回 false。一个舞台同时只有一个命名空间；之后的调用会替换它，nil 或 "" 会清除它。该调用在 Script.lua 顶部、任何函数之外，以及任何回调中都有效；Script.lua 的顶部在加载皮肤、引擎创建舞台时运行，因此舞台在任何 onStart 运行之前就已在列表中。引擎把 info 作为普通数据保存而不读取：字符串、数字、布尔值以及由它们组成的表，最多嵌套 8 层；函数和引擎对象会被略去。ListNamespace 为每个舞台名返回一个条目，即从调用模块执行 Exit("stage", name) 会打开的舞台：皮肤模块得到该名称的皮肤舞台，全局模块得到全局舞台，各自找不到时转向另一侧，并且只有声明了该命名空间的舞台才会列出。条目按名称排序，每个 info 都是新的副本。皮肤的舞台全部创建后所有舞台才都存在，所以请在 onStart 或之后获取列表；在 Script.lua 顶部，只有比调用者先创建的舞台。皮肤的自定义舞台菜单所列出的 CStage 命名空间在 [自定义舞台](../modules/custom-stages.md) 中介绍。
</div>

| 方法 | 说明 |
| --- | --- |
| `STAGES:SetNamespace(namespace, info?)  -> boolean` | 声明调用舞台的命名空间，可选的 info 表会返回给列出它的舞台；在舞台脚本之外返回 false。 |
| `STAGES:SetResolution(width, height)  -> boolean` | 声明调用舞台绘制所用的分辨率。若皮肤的分辨率不同，舞台绘制的一切都会像该分辨率的整个皮肤一样，保持比例居中缩放到皮肤的分辨率；鼠标坐标、`INPUT:GetSurfaceWidth/Height` 和裁剪随之变化，舞台打开的皮肤 ROActivity 保持皮肤的分辨率。0 表示清除；在舞台脚本之外返回 false。 |
| `STAGES:ListNamespace(namespace)  -> table` | 以从 1 开始索引的 `{ name, isGlobal, info }` 表数组返回该命名空间的舞台：文件夹名、位于 Global/Modules/Stages 的舞台为 true，以及声明的 info（未提供时为空表）。 |

```lua
-- Modules/Stages/color_tool/Script.lua, at the top
STAGES:SetNamespace("MyTools", { title = { en = "Colour picker", fr = "Pipette" } })
STAGES:SetResolution(1920, 1080)

-- another stage lists them
function activate()
    for _, s in ipairs(STAGES:ListNamespace("MyTools")) do
        debugLog(s.name .. (s.isGlobal and " (global)" or ""))
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
在除只读活动和背景之外的每个模块中注册为 ACTIVITY 全局对象；在只读活动和背景中它为 nil，这些模块使用 ROACTIVITY。引擎从 Modules/Activities/{name} 加载活动。ACTIVITY 也公开了 GetROActivity，其行为与 ROACTIVITY:GetROActivity 相同。查找优先取调用方一侧：皮肤模块得到皮肤中该名称的活动，Global/Modules 中的模块得到 Global/Modules/Activities 中的那个，自己一侧没有时各自回退到另一侧。Lib 文件从 require 它的模块一侧查找。
</div>

| 方法 | 说明 |
| --- | --- |
| `ACTIVITY:GetActivity(name)  -> activity handle` | 返回具有给定文件夹名称的已加载活动的句柄；未加载时返回 nil。 |
| `ACTIVITY:GetROActivity(name)  -> activity handle` | 与 ROACTIVITY:GetROActivity 相同。 |

### ROACTIVITY

按名称查找已加载只读活动（ROActivity）的全局对象。

<div class="callout warn">
在每个模块中注册为 ROACTIVITY 全局对象。引擎从 Modules/ROActivities/{name} 加载只读活动，并给它们只读的 CONFIG、DATABASE 和 GetSaveFile 全局对象，因此其脚本无法更改游戏状态（见“模块的工作原理”中的只读模块一节）。活动和只读活动都是以名称为键的单例：每个文件夹一个实例，由所有宿主共享。GetROActivity 与 ACTIVITY:GetActivity 一样，在 Modules/ROActivities 和 Global/Modules/ROActivities 之间优先取调用方一侧。
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
| `results_tower` | 打开塔结算画面时以 LuaResultInfo 传入本次游玩并调用 `activate(info)`（只读的 getter，如 `ReachedFloor`、`TotalFloors`、`Lives`、`TowerRank`、`IsNewBestFloor`、`GetScore(player)`）；每帧 `update()` 和 `draw()`；确认时通过 Call 调用 `skip()`，仍在播放动画时返回 true（这次确认只会跳过动画）；`deactivate()`。 |
| `results_regular` | 打开一局（包括 AI 对战）的结算画面时以 LuaResultInfo 传入本局并调用 `activate(info)`（从 0 开始的按玩家 getter，如 `GetGauge(p)`、`GetClearType(p)`、`GetScoreRank(p)`、`GetScore(p)`，以及 `IsAIBattle`、`GetAISectionEnd(i)`）；每帧在结算背景之上 `update()` 和 `draw()`；确认时通过 Call 调用 `skip()`，仍在播放动画时返回 true（背景随后收到 `skipAnime`）；`deactivate()`。 |
| `results_dan` | 打开段位结算画面时以 LuaResultInfo 传入本局并调用 `activate(info)`（`DanResult`、`DanCombo`、`GetDanSong(i)`、`GetExam(i)`、`exam:GetSong(i)`、`exam.PreviousBestAmount`、`DanGradeChange`、`DanGrade`）；每帧在段位结算背景之上 `update()` 和 `draw()`；确认时通过 Call 调用 `skip()`，仍在播放动画时返回 true；随后确认时通过 Call 调用 `advance()`，在奖励弹窗之前还要显示一个画面时返回 true（引擎已把新的或更好的段位写到名牌上，因此模块在揭晓之前绘制 `PreviousNameplateDan`）；按左或右时通过 Call 调用 `swap()`，切换画面时返回 true；`deactivate()`。 |
| `tower_hud` | 塔模式开始时 `activate(maxFloor)`；每帧在上方背景之上 `update()` 和 `draw()`；`deactivate()`。楼层和生命从 PLAYSTATE 读取。 |
| `popup_menu` | 调用 `activate(title, items, fontSize, ...)`，其中 items 是用换行符连接的标签，其后是 PopupMenu 的皮肤位置；每帧调用 `draw(selected)`；关闭时调用 `deactivate()`。 |
| `config_ui` | 以设置模型调用 `activate(model)`；每帧调用 `update()`，返回 "exit" 即离开设置界面，其后可再返回一个 Modules/Transitions 模块名作为返回时的过渡（若有待应用的皮肤、分辨率或声音设备更改，则改为普通淡出）；调用 `draw()`；引擎重建模型时通过 Call 调用 `reload(model)`；调用 `deactivate()`。 |
| `song_enum` | 调用 `activate()`，然后在歌曲扫描进行期间每帧调用 `draw(isCommandSongDataGet, done, total)`；调用 `deactivate()`。 |
| `cutscene` | `activate(paths, fadeOutSeconds)`，依次给出视频文件和最后一个视频之后的淡黑秒数（0 表示不淡出）；每帧调用 `update()`，所有视频播完且淡出结束后返回 "finished"；`draw()`；`deactivate()`。每个文件都带着自身音轨播放，并自带暂停弹窗；过场动画的用途由调用方决定。 |

三个 results 只读活动自行播放结算画面的音乐。奖励弹窗显示期间，引擎通过 Call 调用 `setMusicLevel(percent)`（0 到 100）降低音量。画面淡出为黑色后，引擎停用该模块，其音乐随之停止。

### 结算信息

结算阶段传给 results_tower、results_regular 和 results_dan 的 activate 回调的 LuaResultInfo 对象。

<div class="callout warn">
结算阶段在本局保存后填写它，之后不再改变。所有成员都是只读的：属性使用点语法（`info.Title`），方法使用冒号语法。玩家参数从 0 开始；超出范围时，方法返回 0、false 或空字符串。标有模块名的成员只在该画面中填写；在其他画面中保持该默认值。
</div>

| 方法 | 说明 |
| --- | --- |
| `info.Title  -> string` | 谱面的标题。 |
| `info.Subtitle  -> string` | 谱面的副标题。 |
| `info.PlayerCount  -> number` | 玩家数量（塔和段位的结算中为 1）。 |
| `info.PlayerSide  -> number` | 玩家 1 的名牌一侧：在蓝色侧游玩时为 1，否则为 0。 |
| `info:GetGoodCount(player)  -> number` | 良的数量。 |
| `info:GetOkCount(player)  -> number` | 可的数量。 |
| `info:GetBadCount(player)  -> number` | 不可的数量。 |
| `info:GetRollCount(player)  -> number` | 连打数。 |
| `info:GetMaxCombo(player)  -> number` | 最大连击数。 |
| `info:GetADLibCount(player)  -> number` | 击中的 AD-LIB 音符数。 |
| `info:GetBoomCount(player)  -> number` | 击中的炸弹数。 |
| `info:GetScore(player)  -> number` | 最终分数。 |
| `info:GetPreviousBestScore(player)  -> number` | 保存本局之前，玩家在该谱面和难度上的最高分，没有时为 0。 |
| `info:IsSaved(player)  -> boolean` | 本局记入玩家存档时为 true（自动演奏或观看回放时从不记入）。 |
| `info:GetDifficulty(player)  -> number` | results_regular: 游玩的难度，0 简单 到 4 里魔王。 |
| `info:GetGauge(player)  -> number` | results_regular: 本局结束时的魂槽，0 到 100。 |
| `info:GetGaugeType(player)  -> number` | results_regular: 0 普通，1 困难，2 极限。 |
| `info:GetNorma(player)  -> number` | results_regular: 普通魂槽达成通关的数值。 |
| `info:GetKillZone(player)  -> number` | results_regular: 极限魂槽的危险区，0 到 100（其他魂槽为 0）。 |
| `info:GetClearType(player)  -> number` | results_regular: 0 未通关，1 辅助通关，2 通关，3 全连，4 全良。 |
| `info:GetScoreRank(player)  -> number` | results_regular: 0 无，之后为 1 到 7 的分数等级。 |
| `info:IsCleared(player)  -> boolean` | results_regular: 本局通关时为 true；AI 对战中为玩家 1 获胜时。 |
| `info:GetSpeechText(player, mood)  -> string` | results_regular: 玩家角色按心情的结算台词，0（最差）到 5（全良）。 |
| `info.IsAIBattle  -> boolean` | results_regular: AI 对战时为 true。 |
| `info.AISectionCount  -> number` | results_regular: AI 对战的段落数。 |
| `info:GetAISectionEnd(section)  -> number` | results_regular: 段落（从 0 开始）的结果：0 未打到结尾，1 胜利，2 失败。 |
| `info.TowerType  -> string` | results_tower: 谱面的 TOWERTYPE，未指定时为 ""。 |
| `info.TowerSide  -> number` | results_tower: 0 甜口，1 辣口。 |
| `info.ReachedFloor  -> number` | results_tower: 到达的楼层。 |
| `info.TotalFloors  -> number` | results_tower: 塔的楼层数。 |
| `info.Lives  -> number` | results_tower: 剩余的生命。 |
| `info.MaxLives  -> number` | results_tower: 开始时的生命。 |
| `info.TowerRank  -> number` | results_tower: 0 无，之后为 1 到 7：10、25、50、75 %，通关，全连，全良。 |
| `info.PreviousBestFloor  -> number` | results_tower: 保存本局之前的最佳楼层。 |
| `info.IsNewBestFloor  -> boolean` | results_tower: 已保存的本局超过最佳楼层时为 true。 |
| `info.IsNewBestScore  -> boolean` | results_tower: 已保存的本局超过最高分时为 true。 |
| `info.IsTired  -> boolean` | results_tower: 本局结束时塔角色处于疲惫状态（剩余生命很少）时为 true。 |
| `info.Bpm  -> number` | results_tower: 本局结束时的 BPM。 |
| `info.DanTick  -> number` | results_dan: 段位牌的刻度（DANTICK）。 |
| `info.DanTickColor  -> color` | results_dan: 刻度的颜色（DANTICKCOLOR）。 |
| `info.DanResult  -> number` | results_dan: 0 不合格，1 合格（红），2 合格（金）。 |
| `info.DanCombo  -> number` | results_dan: 0，1 无不可（全连），2 无不可也无可（全良）。 |
| `info.TotalHits  -> number` | results_dan: 良、可和连打的合计。 |
| `info.DanSongCount  -> number` | results_dan: 段位的歌曲数。 |
| `info.DanSongReached  -> number` | results_dan: 本局结束时的歌曲，从 0 开始。 |
| `info.DanExamSong  -> number` | results_dan: 最后显示其单曲考试的歌曲，从 0 开始。 |
| `info:GetDanSong(song)  -> dan song` | results_dan: 段位的第 i 首歌曲（从 0 开始），或 nil。 |
| `info.ExamCount  -> number` | results_dan: 段位的考试数。 |
| `info:GetExam(exam)  -> dan exam` | results_dan: 第 i 项考试（从 0 开始，按谱面顺序），或 nil。 |
| `info.DanGradeChange  -> number` | results_dan: 0 无变化，1 新的段位，2 已合格段位的更好等级（金色或更好的通关）。引擎会把新的或更好的等级写到玩家的名牌上。 |
| `info.DanGrade  -> dan grade` | results_dan: 本局之后存档中该段位的等级；从未合格时其 Title 为 ""。 |
| `info.PreviousNameplateDan  -> dan grade` | results_dan: 本局之前名牌显示的段位（没有时 Title 为 ""）。 |

### 段位歌曲

info:GetDanSong 返回的只读对象。

| 方法 | 说明 |
| --- | --- |
| `song.Title  -> string` | 歌曲的标题；谱面隐藏的歌曲为 "???"。 |
| `song.Subtitle  -> string` | 歌曲的副标题；谱面隐藏的歌曲为 ""。 |
| `song.Difficulty  -> number` | 0 简单 到 4 里魔王。 |
| `song.Level  -> number` | 歌曲的等级。 |
| `song.Good  -> number` | 该曲的良的数量。 |
| `song.Ok  -> number` | 该曲的可的数量。 |
| `song.Bad  -> number` | 该曲的不可的数量。 |
| `song.Roll  -> number` | 该曲的连打数。 |

### 段位考试

info:GetExam 和 exam:GetSong 返回的只读对象：本局结束时的一项考试。

| 方法 | 说明 |
| --- | --- |
| `exam.Type  -> number` | 0 魂槽，1 良，2 可，3 不可，4 分数，5 连打，6 击打数，7 连击，8 精度，9 AD-LIB，10 BOOM；名称由 LANG:GetExamName(type) 给出。 |
| `exam.Range  -> number` | 0 至少 Value，1 少于 Value。 |
| `exam.Value  -> number` | 合格（红）的数值。 |
| `exam.Amount  -> number` | 显示的数值；对于“少于”考试，为失败前剩余的数量。 |
| `exam.PreviousBestAmount  -> number` | 保存本局之前玩家在该考试上的最佳 Amount，口径与 Amount 相同（越大越好）；没有时为 -1。单曲考试的每首歌曲（exam:GetSong）各有其值。 |
| `exam.Percent  -> number` | Amount 相对 Value 的比例，0 到 100。 |
| `exam.Status  -> number` | 0 不合格，1 合格，2 金合格。 |
| `exam.IsPerSong  -> boolean` | 逐曲判定的考试时为 true；此时该考试本身是最后一首歌曲的。 |
| `exam:GetSong(song)  -> dan exam` | 单曲考试在第 i 首歌曲（从 0 开始）上的考试，或 nil。 |

### 段位等级

info.DanGrade 和 info.PreviousNameplateDan 返回的对象，只读。

| 方法 | 说明 |
| --- | --- |
| `grade.Title  -> string` | 名牌上显示的段位名称（谱面标题的前两个字符）；没有时为 ""。 |
| `grade.Gold  -> boolean` | 金色合格时为 true。 |
| `grade.Tier  -> number` | 0 通关，1 全连，2 全良。 |

## 背景

### 背景模块

绘制一个界面背景、演奏图层、观众、过关动画或花球效果的 Script.lua，由引擎自身的界面承载。

<div class="callout warn">
背景位于 Modules 文件夹之外，在皮肤的 Graphics 文件夹下、其所装饰界面的目录中，例如 Graphics/0_Startup/Script.lua、Graphics/6_Result/Script.lua、Graphics/5_Game/5_Background/Normal/Up/{variant}/Script.lua、Graphics/5_Game/5_Background/Normal/Down/{variant}/Script.lua、Graphics/5_Game/3_Mob/{variant}/Script.lua、Graphics/5_Game/9_End/{result}/Script.lua 和 Graphics/5_Game/11_Balloon/Kusudama/Script.lua。当一个文件夹包含多个变体时，引擎每次演奏随机挑选一个（或按谱面的场景预设挑选）。宿主界面创建背景实例（演奏背景在每次引擎进入游戏界面时创建）并随界面一起释放，因此演奏期间会同时存在多个实例。背景脚本获得与只读活动相同的全局对象（只读的 CONFIG、DATABASE 和 GetSaveFile；没有 ACTIVITY）。下面的事件钩子是可选的，引擎在对应事件发生时调用每个钩子一次。 对塔而言，谱面的 `TOWERTYPE` 指定 Down 文件夹。
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
| `state.isClear  -> boolean[]` | 每个玩家当前是否处于过关区域。普通魂槽的段位中，第一次击中时变为 true，游戏失败或有考核不合格时变为 false。 |
| `state.gauge  -> number[]` | 每个玩家的魂槽值。 |
| `state.bpm  -> number[]` | 每个玩家当前的 BPM。 |
| `state.gogo  -> boolean[]` | 每个玩家是否处于 GoGo 时间。 |
| `state.towerNightNum  -> number` | 塔模式的昼夜系数，0 到 1。 |
| `state.towerFloor  -> number` | 正在游玩的楼层，从 0 起（塔模式）。 |
| `state.towerMaxFloor  -> number` | 谱面的楼层数（塔模式）。 |
| `state.battleState  -> number` | AI 对战状态代码。 |
| `state.battleWin  -> boolean` | 玩家在 AI 对战中领先时为 true。 |
| `state.timeStamp  -> number` | 与谱面同步的时间（秒）；演奏之外为 -1。 |
| `state.paused  -> boolean` | 演奏暂停期间为 true。 |
| `state.player  -> number` | 按玩家承载的宿主（过关动画）所绘制的玩家。 |
| `state.hitRing  -> number` | hitNote、hitBig 和 hitJudge 中保留的最近击打数（32）。 |
| `state.hitCount  -> number[]` | 自背景启动以来每个玩家的击打：已判定的音符（包括失误）、连打击打和打爆的气球。从 0 起计数的第 n 次击打位于 hitNote、hitBig 和 hitJudge 的索引 n % hitRing 处。 |
| `state.hitNote  -> string[][]` | 每个玩家保留的各次击打的音符："don"、"ka" 或 "kadon"。 |
| `state.hitBig  -> boolean[][]` | 每个玩家保留的击打是大音符时为 true。 |
| `state.hitJudge  -> string[][]` | 每个玩家保留的各次击打的判定："perfect"、"great"、"good"、"poor"、"miss"、"roll" 或 "balloon"。 |
| `state.landCount  -> number[]` | 自背景启动以来每个玩家已到达飞行终点的飞行音符。 |
| `state:SetFlyTarget(player, x, y)` | 在此演奏背景显示期间，该玩家的飞行音符落在以屏幕像素计的 (x, y) 处，并省略皮肤的落点效果。 |
| `state:ClearFlyTarget(player)` | 该玩家的飞行音符重新落在皮肤通常的位置。 |

## 过渡

### 过渡模块

绘制两个舞台之间的淡出、加载和淡入阶段的 Modules/Transitions/{name}/Script.lua。

<div class="callout warn">
Exit 的第三个参数选择过渡；调用未指定名称或引擎找不到该名称时，引擎使用 "default"。Exit("play") 之后进入演奏的加载始终使用名为 "song_loading" 的过渡，皮肤没有它时使用 "default"。引擎按顺序驱动各阶段：在即将离开的舞台之上每帧调用 fadeOut(t) 直到 t 达到 1，然后卸载该舞台并加载新舞台，期间每帧调用 loading(progress, elapsed)，然后在新舞台之上调用 fadeIn(t) 直到 t 达到 1。每段淡入淡出持续 0.5 秒，除非脚本设置了 FADE_OUT_SECONDS 或 FADE_IN_SECONDS；引擎会忽略不是正数的值。切换舞台时，只有当加载耗时超过 0.5 秒后引擎才会调用 loading；在此之前它调用的是 fadeOut(1)，以免短暂的加载闪现加载画面。歌曲加载路径则会立即显示加载阶段。新舞台在加载或淡入期间启动的 BGM 会等待，并在淡入结束时播放（进入演奏画面的加载除外）；过渡自己的声音会立即播放。淡入期间新舞台不会收到输入，除非淡入显示的是演奏画面。玩家取消进入游戏的加载时，引擎会返回选歌界面，并用 fadeIn(t) 显示它，因此每次过渡都以调用 fadeIn(1) 结束。
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
