<!-- api/players.md -->

# 玩家与档案

存档、名牌、角色、小角色、演奏状态、主题和当前语言。

本页所有地方的玩家索引都从 0 起（0 到 4），只有 THEME:GetThemeSettingForPlayer 从 1 起。只读模块（只读活动和背景）收到的存档句柄的写入方法会记录一条错误并且不做任何事；这里的其他内容在每种模块类型中行为相同。

## 存档

### GetSaveFile

返回某个玩家槽位存档句柄的全局函数。

<div class="callout warn">
以普通函数的形式调用它（`GetSaveFile(0)`）。超出范围的索引会记录一条错误并返回 nil。每次调用都创建一个读取实时数据的新句柄，因此没有什么需要缓存或释放。
</div>

| 方法 | 说明 |
| --- | --- |
| `GetSaveFile(player)  -> saveFile` | 返回 0 起玩家槽位的存档句柄；索引超出范围时返回 nil。 |

### 存档句柄

一个玩家的档案：名称、金币、已解锁项目、触发器和计数器、过关统计、装备的角色、小角色、名牌和段位称号。

<div class="callout warn">
用点语法读取属性（sf.Name、sf.Coins）。写入方法会立即持久化。只读模块阻止以下方法：SpendCoins、EarnCoins、UnlockNameplate、UnlockSong、对 SelectedHitsounds 赋值、SetGlobalTrigger、SetGlobalCounter、ChangeCharacter（返回 false）、UnlockPuchichara、ChangePuchichara、UnlockCharacter、ChangeDan、ChangeName 和 ChangeNameplate。
</div>

| 方法 | 说明 |
| --- | --- |
| `sf.Name  -> string` | 玩家的显示名称。 |
| `sf.SaveId  -> integer` | 本存档的数字数据库 id。 |
| `sf.SaveUID  -> string` | 本存档的唯一字符串 id。 |
| `sf.NameplateInfo  -> nameplateInfo` | 装备的名牌（见“名牌信息句柄”）；存储的 id 未知时为默认的初学者名牌。 |
| `sf.DanplateInfo  -> danplateInfo` | 当前的段位称号（见“段位牌信息句柄”）。 |
| `sf.TotalPlaycount  -> integer` | 本存档的总演奏次数。 |
| `sf.AIBattlePlaycount  -> integer` | AI 对战次数。 |
| `sf.AIBattleWins  -> integer` | AI 对战胜利次数。 |
| `sf.Coins  -> integer` | 当前金币余额。 |
| `sf.TotalEarnedCoins  -> integer` | 本存档生命周期内获得的金币总数。 |
| `sf:SpendCoins(price)  -> nil` | 扣除金币（余额不会低于 0）并持久化。 |
| `sf:EarnCoins(amount)  -> nil` | 把金币加到余额和总获得数上，并持久化。 |
| `sf:IsNameplateUnlocked(id)  -> bool` | 此 id 的名牌是否已解锁。 |
| `sf:UnlockNameplate(id)  -> nil` | 解锁一个名牌并持久化（已解锁时为空操作）。 |
| `sf:IsSongUnlocked(uniqueId)  -> bool` | 此唯一 id 的歌曲是否已解锁。 |
| `sf:UnlockSong(uniqueId)  -> nil` | 解锁一首歌曲并持久化（已解锁时为空操作）。 |
| `sf.SelectedHitsounds  -> string` | 选定的打击音效组的文件夹名称。赋予不同的名称会持久化并重新加载该玩家的打击音效。 |
| `sf:GetGlobalTrigger(name)  -> bool` | 读取一个具名的布尔触发器。 |
| `sf:GetGlobalCounter(name)  -> number` | 读取一个具名的数值计数器。 |
| `sf:SetGlobalTrigger(name, value)  -> nil` | 设置一个具名的布尔触发器。 |
| `sf:SetGlobalCounter(name, value)  -> nil` | 设置一个具名的数值计数器。 |
| `sf:GetClearStatusCount(difficulty, clearStatus)  -> integer` | 某个难度（0 Easy 到 4 Extra Extreme）中最佳过关状态恰好为 clearStatus（0 无，1 辅助过关，2 过关，3 全连击，4 全良）的谱面数量。参数超出范围时为 0。 |
| `sf:GetDanBestPlay(node)  -> danBestPlay` | 某个段位歌曲节点的无 Mod 最佳成绩（见“段位最佳成绩句柄”）；没有时返回 HasRecord 为 false 的句柄。 |
| `sf:GetCharacter()  -> character` | 绑定到该槽位玩家的角色句柄（见“角色句柄”）。 |
| `sf.CharacterName  -> string` | 装备的角色的文件夹名称。 |
| `sf:ChangeCharacter(folderName)  -> bool` | 装备此文件夹名称的角色。角色已装备或原本就处于装备状态时返回 true，没有已加载的角色使用此文件夹名称时返回 false。 |
| `sf:GetPuchichara()  -> puchichara` | 装备的小角色（见“小角色句柄”）；无法解析时返回 nil。 |
| `sf:IsPuchicharaUnlocked(folderName)  -> bool` | 此文件夹名称的小角色是否已解锁。 |
| `sf:UnlockPuchichara(folderName)  -> nil` | 解锁一个小角色并持久化（已解锁时为空操作）。 |
| `sf:ChangePuchichara(folderName)  -> nil` | 装备此文件夹名称的小角色并持久化。该方法不验证名称。 |
| `sf:IsCharacterUnlocked(folderName)  -> bool` | 该角色是否已解锁。装备中的角色始终视为已解锁。 |
| `sf:UnlockCharacter(folderName)  -> nil` | 解锁一个角色并持久化（已解锁时为空操作）。 |
| `sf.DanTitleCount  -> integer` | 可用的段位称号数量，包括默认称号（始终至少为 1）。 |
| `sf:GetDanTitleByIndex(index)  -> danTitleEntry` | 0 起索引处的段位称号（见“段位称号条目句柄”）。索引 0 是默认称号；超出范围时为 nil。 |
| `sf.SelectedDan  -> string` | 当前段位称号的文本。 |
| `sf:ChangeDan(title)  -> nil` | 把给定称号设为当前称号；若它是玩家获得过的称号，则复制其金色和过关状态标志；然后刷新名牌并持久化。 |
| `sf:ChangeName(name)  -> nil` | 更改显示名称，刷新名牌并持久化。该方法会忽略空名称或未变化的名称。 |
| `sf:ChangeNameplate(id)  -> nil` | 装备此 id 的名牌，刷新名牌并持久化。数据库中不存在的 id 会清除缓存的称号文本。 |

```lua
local save = GetSaveFile(0)
local entry = CHARACTERLIST:GetByName("Aoi")
if entry and not save:IsCharacterUnlocked(entry.FolderName) then
    local cond = entry.UnlockCondition
    if cond:IsUnlockable(0) and cond:GetCoinPrice() <= save.Coins then
        save:SpendCoins(cond:GetCoinPrice())
        save:UnlockCharacter(entry.FolderName)
    end
end
```

## 名牌与段位称号

### NAMEPLATE

绘制称号牌、段位牌和完整的玩家名牌。

<div class="callout warn">
皮肤的 nameplate 只读活动（Modules/ROActivities/nameplate）负责绘制，并定义美术和布局。不透明度为 0 到 255。文本参数接受由文本对象渲染出的纹理（见“图形与文本”）；rarity 是索引：0 Poor、1 Common、2 Uncommon、3 Rare、4 Epic、5 Legendary、6 Mythical。
</div>

| 方法 | 说明 |
| --- | --- |
| `NAMEPLATE:DrawTitlePlate(x, y, opacity, type, text, rarity, nameplateId)  -> nil` | 以给定的显示类型、预渲染的称号纹理、稀有度索引和名牌 id 绘制一块称号牌。 |
| `NAMEPLATE:DrawDanPlate(x, y, opacity, danGrade, text)  -> nil` | 使用预渲染的称号纹理为给定等级绘制一块段位牌。 |
| `NAMEPLATE:DrawPlayerNameplate(x, y, opacity, player)  -> nil` | 绘制某个玩家槽位的完整名牌；红侧或蓝侧遵循游戏的 1P 侧设置。 |
| `NAMEPLATE:DrawNameplateTitleById(id, x, y, opacity, font)  -> nil` | 用一个文本对象渲染此 id 名牌的本地化称号，并将其绘制为称号牌。该 id 必须存在于名牌数据库中。 |

### NAMEPLATESLIST

游戏已知的所有名牌的数据库，支持按索引或 id 查找以及过滤。

<div class="callout warn">
查询方法返回名牌信息句柄。FindWhere 对每个名牌调用一次 Lua 函数，并保留其返回 true 的条目。
</div>

| 方法 | 说明 |
| --- | --- |
| `NAMEPLATESLIST.Count  -> integer` | 数据库中名牌的数量。 |
| `NAMEPLATESLIST:GetByIndex(index)  -> nameplateInfo` | 0 起数据库位置处的名牌；超出范围时为 nil。 |
| `NAMEPLATESLIST:GetById(id)  -> nameplateInfo` | 此 id 的名牌；未找到时为 nil。 |
| `NAMEPLATESLIST:GetAll()  -> nameplateInfo[]` | 以列表形式返回所有名牌。 |
| `NAMEPLATESLIST:FindWhere(predicate)  -> nameplateInfo[]` | `predicate(info)` 返回 true 的名牌。 |

### 名牌信息句柄

一个名牌称号：本地化文本、显示类型、id、稀有度和解锁条件。

<div class="callout warn">
sf.NameplateInfo 和 NAMEPLATESLIST 返回这些句柄。默认的初学者名牌的 id 为 -1，稀有度为 "Common"，没有解锁条件。
</div>

| 方法 | 说明 |
| --- | --- |
| `info.Title  -> string` | 当前语言的称号文本。 |
| `info.Type  -> integer` | 传给 NAMEPLATE:DrawTitlePlate 的显示类型代码。 |
| `info.Id  -> integer` | 名牌 id（默认初学者名牌为 -1）。 |
| `info.Rarity  -> string` | 稀有度名称："Poor"、"Common"、"Uncommon"、"Rare"、"Epic"、"Legendary" 或 "Mythical"。 |
| `info.UnlockCondition  -> unlockCondition` | 解锁条件（见“解锁条件句柄”）。 |

### 段位牌信息句柄

玩家显示在名牌上的当前段位称号。

<div class="callout warn">
sf.DanplateInfo 返回此句柄。值反映读取时刻的存档状态。
</div>

| 方法 | 说明 |
| --- | --- |
| `info.Title  -> string` | 当前段位称号的文本。 |
| `info.Gold  -> bool` | 玩家是否以金合格获得当前称号。 |
| `info.ClearStatus  -> integer` | 当前称号的过关状态代码。 |

### 段位称号条目句柄

玩家可以选择的一个段位称号。

<div class="callout warn">
sf:GetDanTitleByIndex 返回这些条目。索引 0 是默认称号（非金色，过关状态 0）；之后的索引是玩家获得的称号。
</div>

| 方法 | 说明 |
| --- | --- |
| `entry.Title  -> string` | 称号文本。 |
| `entry.IsGold  -> bool` | 玩家是否以金合格获得该称号。 |
| `entry.ClearStatus  -> integer` | 该称号记录的最佳过关状态。 |

### 段位最佳成绩句柄

一条段位记录的最佳考核结果。

<div class="callout warn">
sf:GetDanBestPlay 返回此句柄。读取考核之前请先检查 HasRecord。GetExam 返回 .NET 数组：从 0 起索引并读取 `.Length`。
</div>

| 方法 | 说明 |
| --- | --- |
| `play.HasRecord  -> bool` | 该歌曲是否存在记录。 |
| `play:GetExam(slot)  -> int[]` | 考核槽位 1 到 7 的最佳成绩：整个课程的考核为一个值，单曲考核为每首歌曲一个值。记录缺失或槽位无效时为空。 |

## 角色与小角色

### CHARACTER

创建角色句柄，并公开标准动画和语音槽位的名称。

<div class="callout warn">
CreateCharacter 返回一个拥有自己资源的句柄；请检查 IsValid，用完后调用 Dispose。GetPlayerCharacter 返回一个跟随玩家装备角色的句柄，无需释放。GetPlayerGradientMap 返回一个渐变映射（见“图形与文本”）。ANIM_* 和 VOICE_* 成员是只读字符串；把它们传给角色句柄的动画和语音方法。
</div>

| 方法 | 说明 |
| --- | --- |
| `CHARACTER:CreateCharacter(folderName)  -> character` | 从 Global/Characters/{folderName} 加载一个独立的角色。文件夹不存在时 IsValid 为 false。 |
| `CHARACTER:GetPlayerCharacter(player)  -> character` | 绑定到玩家槽位的句柄，每次调用都会解析当前装备的角色。 |
| `CHARACTER:GetPlayerGradientMap(player)  -> gradientMap` | 玩家槽位当前启用的调色板渐变；未设置时为 nil。 |
| `CHARACTER.ANIM_PREVIEW  -> string` | 预览姿势（菜单和商店）。 |
| `CHARACTER.ANIM_RENDER  -> string` | 完整立绘姿势。 |
| `CHARACTER.ANIM_GAME_NORMAL  -> string` | 演奏，普通状态。 |
| `CHARACTER.ANIM_GAME_CLEAR  -> string` | 演奏，魂槽处于过关区域。 |
| `CHARACTER.ANIM_GAME_MAX  -> string` | 演奏，魂槽已满。 |
| `CHARACTER.ANIM_GAME_GOGO  -> string` | 演奏，GoGo 时间。 |
| `CHARACTER.ANIM_GAME_GOGO_MAX  -> string` | 演奏，魂槽已满的 GoGo 时间。 |
| `CHARACTER.ANIM_GAME_MISS  -> string` | 演奏，失误。 |
| `CHARACTER.ANIM_GAME_MISS_DOWN  -> string` | 演奏，魂槽较低时的失误。 |
| `CHARACTER.ANIM_GAME_10COMBO  -> string` | 演奏，10 连击里程碑。 |
| `CHARACTER.ANIM_GAME_10COMBO_MAX  -> string` | 演奏，魂槽已满时的 10 连击里程碑。 |
| `CHARACTER.ANIM_GAME_CLEARED  -> string` | 演奏，歌曲过关。 |
| `CHARACTER.ANIM_GAME_FAILED  -> string` | 演奏，歌曲失败。 |
| `CHARACTER.ANIM_GAME_CLEAR_OUT  -> string` | 离开过关状态的过渡。 |
| `CHARACTER.ANIM_GAME_CLEAR_IN  -> string` | 进入过关状态的过渡。 |
| `CHARACTER.ANIM_GAME_MAX_OUT  -> string` | 离开满槽状态的过渡。 |
| `CHARACTER.ANIM_GAME_MAX_IN  -> string` | 进入满槽状态的过渡。 |
| `CHARACTER.ANIM_GAME_MISS_IN  -> string` | 进入失误的过渡。 |
| `CHARACTER.ANIM_GAME_MISS_DOWN_IN  -> string` | 进入低魂槽失误的过渡。 |
| `CHARACTER.ANIM_GAME_RETURN  -> string` | 回到普通状态。 |
| `CHARACTER.ANIM_GAME_GOGOSTART  -> string` | GoGo 开始爆发。 |
| `CHARACTER.ANIM_GAME_GOGOSTART_CLEAR  -> string` | 过关状态下的 GoGo 开始爆发。 |
| `CHARACTER.ANIM_GAME_GOGOSTART_MAX  -> string` | 魂槽已满时的 GoGo 开始爆发。 |
| `CHARACTER.ANIM_GAME_BALLOON_BREAKING  -> string` | 气球被击打中。 |
| `CHARACTER.ANIM_GAME_BALLOON_BROKE  -> string` | 气球爆开。 |
| `CHARACTER.ANIM_GAME_BALLOON_MISS  -> string` | 气球未击破。 |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING  -> string` | 花球被击打中。 |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BROKE  -> string` | 花球击破。 |
| `CHARACTER.ANIM_GAME_KUSUDAMA_MISS  -> string` | 花球未击破。 |
| `CHARACTER.ANIM_GAME_KUSUDAMA_IDLE  -> string` | 花球待机。 |
| `CHARACTER.ANIM_GAME_TOWER_STANDING  -> string` | 塔模式，站立。 |
| `CHARACTER.ANIM_GAME_TOWER_STANDING_TIRED  -> string` | 塔模式，疲劳时站立。 |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING  -> string` | 塔模式，攀爬。 |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING_TIRED  -> string` | 塔模式，疲劳时攀爬。 |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING  -> string` | 塔模式，奔跑。 |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING_TIRED  -> string` | 塔模式，疲劳时奔跑。 |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR  -> string` | 塔模式，过关。 |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR_TIRED  -> string` | 塔模式，疲劳时过关。 |
| `CHARACTER.ANIM_GAME_TOWER_FAIL  -> string` | 塔模式，失败。 |
| `CHARACTER.ANIM_MENU_WAIT  -> string` | 菜单，等待。 |
| `CHARACTER.ANIM_MENU_START  -> string` | 菜单，开始。 |
| `CHARACTER.ANIM_MENU_NORMAL  -> string` | 菜单，普通。 |
| `CHARACTER.ANIM_MENU_SELECT  -> string` | 菜单，选择。 |
| `CHARACTER.ANIM_ENTRY_NORMAL  -> string` | 进入界面，普通。 |
| `CHARACTER.ANIM_ENTRY_JUMP  -> string` | 进入界面，跳跃。 |
| `CHARACTER.ANIM_RESULT_NORMAL  -> string` | 结算，普通。 |
| `CHARACTER.ANIM_RESULT_CLEAR  -> string` | 结算，过关。 |
| `CHARACTER.ANIM_RESULT_FAILED_IN  -> string` | 结算，进入失败状态。 |
| `CHARACTER.ANIM_RESULT_FAILED  -> string` | 结算，失败。 |
| `CHARACTER.VOICE_END_FAILED  -> string` | 歌曲结束，失败。 |
| `CHARACTER.VOICE_END_CLEAR  -> string` | 歌曲结束，过关。 |
| `CHARACTER.VOICE_END_FULLCOMBO  -> string` | 歌曲结束，全连击。 |
| `CHARACTER.VOICE_END_ALLPERFECT  -> string` | 歌曲结束，全良。 |
| `CHARACTER.VOICE_END_AIBATTLE_WIN  -> string` | 歌曲结束，AI 对战胜利。 |
| `CHARACTER.VOICE_END_AIBATTLE_LOSE  -> string` | 歌曲结束，AI 对战失败。 |
| `CHARACTER.VOICE_MENU_SONGSELECT  -> string` | 进入选曲界面。 |
| `CHARACTER.VOICE_MENU_SONGDECIDE  -> string` | 确认歌曲。 |
| `CHARACTER.VOICE_MENU_SONGDECIDE_AI  -> string` | 在 AI 对战中确认歌曲。 |
| `CHARACTER.VOICE_MENU_DIFFSELECT  -> string` | 难度选择。 |
| `CHARACTER.VOICE_MENU_DANSELECTSTART  -> string` | 进入段位选择。 |
| `CHARACTER.VOICE_MENU_DANSELECTPROMPT  -> string` | 段位选择提示。 |
| `CHARACTER.VOICE_MENU_DANSELECTCONFIRM  -> string` | 确认段位课程。 |
| `CHARACTER.VOICE_TITLE_SANKA  -> string` | 标题界面进入。 |
| `CHARACTER.VOICE_TOWER_MISS  -> string` | 塔模式失误。 |
| `CHARACTER.VOICE_RESULT_BESTSCORE  -> string` | 结算，新的最高分。 |
| `CHARACTER.VOICE_RESULT_CLEARFAILED  -> string` | 结算，失败。 |
| `CHARACTER.VOICE_RESULT_CLEARSUCCESS  -> string` | 结算，过关。 |
| `CHARACTER.VOICE_RESULT_DANFAILED  -> string` | 结算，段位不合格。 |
| `CHARACTER.VOICE_RESULT_DANREDPASS  -> string` | 结算，段位合格。 |
| `CHARACTER.VOICE_RESULT_DANGOLDPASS  -> string` | 结算，段位金合格。 |

### 角色句柄

一个可绘制的角色：播放具名的动画和语音，并携带按句柄的绘制状态（不透明度、缩放、着色、旋转、混合与环绕模式、调色板渐变）。

<div class="callout warn">
CHARACTER:GetPlayerCharacter、CHARACTER:CreateCharacter、sf:GetCharacter 和角色列表条目的 Character 属性返回角色句柄。只有来自 CreateCharacter 的句柄拥有自己的资源并需要 Dispose。句柄存储 Set* 的值并在之后的每次绘制时应用；绘制方法的缩放和不透明度参数与存储的值相乘。存储的不透明度为 0.0 到 1.0，按次绘制的不透明度为 0 到 255。动画和语音名称是 CHARACTER 常量。
</div>

| 方法 | 说明 |
| --- | --- |
| `char.IsValid  -> bool` | 句柄是否解析到一个已加载的角色。 |
| `char.FolderName  -> string` | 文件夹名称；无效时为空字符串。 |
| `char.FullPath  -> string` | 文件夹绝对路径；无效时为空字符串。 |
| `char.DisplayName  -> string` | 本地化显示名称，回退为文件夹名称。 |
| `char:SetPaletteGradient(stops, blend?)  -> nil` | 应用由至少两个色标的表构建的调色板渐变，可选混合强度（默认 1.0）。绑定玩家的句柄还会把该渐变存储到玩家槽位上。传入 nil 可清除。 |
| `char:ClearPaletteGradient()  -> nil` | 移除调色板渐变（对绑定玩家的句柄，也移除玩家槽位的渐变）。 |
| `char:SetOpacity(opacity)  -> nil` | 存储的不透明度，0.0 透明到 1.0 不透明。 |
| `char:SetScale(scaleX, scaleY)  -> nil` | 存储的缩放；负的 X 为水平镜像。 |
| `char:SetColor(color)  -> nil` | 由颜色值设置存储的着色。 |
| `char:SetColor(r, g, b)  -> nil` | 由三个 0.0 到 1.0 的通道设置存储的着色。 |
| `char:SetRotation(degrees)  -> nil` | 存储的旋转角度（度）。 |
| `char:SetBlendMode(mode)  -> nil` | 存储的混合模式："normal"、"add"、"multi"、"sub" 或 "screen"。 |
| `char:SetWrapMode(mode)  -> nil` | 存储的纹理环绕模式："edge"、"border"、"repeat" 或 "mirror"。 |
| `char:GetScale()  -> vector2` | 存储的缩放。 |
| `char:GetColor()  -> tuple` | 以 .NET 元组返回存储的着色，字段为 Item1、Item2 和 Item3（红、绿、蓝）。 |
| `char:GetRotation()  -> number` | 存储的旋转角度（度）。 |
| `char:GetBlendMode()  -> string` | 存储的混合模式。 |
| `char:GetWrapMode()  -> string` | 存储的环绕模式。 |
| `char:Draw(x, y, animation, scaleX?, scaleY?, opacity?)  -> nil` | 在 x, y 绘制该动画。默认：缩放 1，不透明度 255。 |
| `char:DrawAtAnchor(x, y, animation, anchor?, scaleX?, scaleY?, opacity?)  -> nil` | 绘制该动画，把指定锚点（默认 "bottom"）放在 x, y。 |
| `char:DrawRect(x, y, w, h, animation, scaleX?, scaleY?, opacity?)  -> nil` | 在矩形的左上角绘制该动画。该方法为布局代码接受 w 和 h，但它们不影响绘制。 |
| `char:DrawRectAtAnchor(x, y, clipW, clipH, animation, opacity?, clipX?, clipY?)  -> nil` | 以 x, y 为左上角绘制该动画，并裁剪到偏移 clipX, clipY 的 clipW x clipH 矩形。缩放、着色和旋转只来自存储的状态。 |
| `char:Update(animation, looping?)  -> bool` | 推进动画（默认循环），并返回它是否仍在播放。 |
| `char:LoadAnimation(animation)  -> nil` | 加载该动画的帧。 |
| `char:DisposeAnimation(animation)  -> nil` | 释放该动画的帧。 |
| `char:AvailableAnimation(animation)  -> bool` | 角色是否提供该动画。 |
| `char:SetAnimationDuration(animation, duration)  -> nil` | 设置该动画的播放时长。 |
| `char:SetAnimationCyclesFromBPM(animation, bpm)  -> nil` | 由 BPM 设置该动画的周期长度。 |
| `char:ResetAnimationCounter(animation)  -> nil` | 让动画从第一帧重新开始。 |
| `char:GetAnimationSize(animation)  -> vector2` | 该动画当前帧在皮肤分辨率下的绘制尺寸；不可用时为 (0, 0)。 |
| `char:LoadVoice(voice)  -> nil` | 加载一段语音。 |
| `char:DisposeVoice(voice)  -> nil` | 释放一段语音。 |
| `char:PlayVoice(voice)  -> nil` | 播放一段语音。 |
| `char:Dispose()  -> nil` | 释放角色的资源（仅限来自 CreateCharacter 的句柄）。 |

```lua
local chara = CHARACTER:GetPlayerCharacter(0)
chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)

function update()
    chara:Update(CHARACTER.ANIM_MENU_NORMAL)
end

function draw()
    chara:DrawAtAnchor(960, 1000, CHARACTER.ANIM_MENU_NORMAL, "bottom")
end
```

### CHARACTERLIST

所有已加载角色的列表。

<div class="callout warn">
皮肤加载其角色时重建该列表，重新加载皮肤时释放它，因此在没有角色加载期间该全局对象可能为 nil。查询方法返回角色列表条目。
</div>

| 方法 | 说明 |
| --- | --- |
| `CHARACTERLIST.Count  -> integer` | 已加载角色的数量。 |
| `CHARACTERLIST:GetAll()  -> characterEntry[]` | 以列表形式返回所有角色。 |
| `CHARACTERLIST:GetByIndex(index)  -> characterEntry` | 0 起索引处的条目；超出范围时为 nil。 |
| `CHARACTERLIST:GetByName(folderName)  -> characterEntry` | 此文件夹名称的条目；未找到时为 nil。 |

### 角色列表条目

一个 CHARACTERLIST 条目：文件夹名称、显示名称、稀有度、一个角色句柄和解锁条件。

<div class="callout warn">
列表拥有 Character 属性中的共享句柄；不要释放它。绘制前先在其上加载动画。
</div>

| 方法 | 说明 |
| --- | --- |
| `entry.FolderName  -> string` | 文件夹名称；存档用它作为键。 |
| `entry.DisplayName  -> string` | 本地化显示名称。 |
| `entry.Rarity  -> string` | 稀有度名称（列表见“名牌信息句柄”）。 |
| `entry.Character  -> character` | 本条目的角色句柄。 |
| `entry.UnlockCondition  -> unlockCondition` | 解锁条件（见“解锁条件句柄”）。 |

### PUCHICHARALIST

所有已加载小角色的列表，以及每个玩家的当前选择。

<div class="callout warn">
皮肤加载其小角色纹理时重建该列表，重新加载皮肤时释放它，因此在它们未加载期间该全局对象可能为 nil。查询方法返回小角色句柄。
</div>

| 方法 | 说明 |
| --- | --- |
| `PUCHICHARALIST.Count  -> integer` | 已加载小角色的数量。 |
| `PUCHICHARALIST:GetAll()  -> puchichara[]` | 以列表形式返回所有小角色。 |
| `PUCHICHARALIST:GetByIndex(index)  -> puchichara` | 0 起索引处的小角色；超出范围时为 nil。 |
| `PUCHICHARALIST:GetByName(folderName)  -> puchichara` | 此文件夹名称的小角色；未找到时为 nil。 |
| `PUCHICHARALIST:GetPlayerPuchichara(player)  -> puchichara` | 某个玩家槽位装备的小角色；无法解析时为 nil。 |

### 小角色句柄

一个小角色：其纹理、本地化名称和作者、稀有度、文件夹名称和解锁条件。

<div class="callout warn">
PUCHICHARALIST 和 sf:GetPuchichara 返回这些句柄。列表拥有这些纹理；不要释放它们。图像缺失时得到空纹理。
</div>

| 方法 | 说明 |
| --- | --- |
| `puchi.tx  -> texture` | 从 Chara.png 加载的精灵图集。 |
| `puchi.render  -> texture` | 从 Render.png 加载的完整立绘。 |
| `puchi.Name  -> string` | 本地化显示名称。 |
| `puchi.Author  -> string` | 本地化作者名称。 |
| `puchi.Rarity  -> string` | 稀有度名称（列表见“名牌信息句柄”）。 |
| `puchi.FolderName  -> string` | 文件夹名称；存档用它作为键。 |
| `puchi.UnlockCondition  -> unlockCondition` | 解锁条件（见“解锁条件句柄”）。 |
| `puchi:GetUnlockMessage()  -> string` | `puchi.UnlockCondition:GetConditionMessage()` 的快捷方式。 |

## 演奏状态与解锁

### PLAYSTATE

当前或最近一次演奏的实时结果：判定计数、分数、连击、过关检查，以及塔和段位状态。

<div class="callout warn">
这些值来自演奏界面，因此在演奏期间和之后的界面上才有意义。玩家索引从 0 起；这些方法不做范围检查。段位检查始终评估玩家 0。
</div>

| 方法 | 说明 |
| --- | --- |
| `PLAYSTATE.LastRegisteredFloor  -> integer` | 塔模式：到达的最后一层。 |
| `PLAYSTATE.MaxNumberOfLives  -> integer` | 塔模式：最大生命数。 |
| `PLAYSTATE.CurrentNumberOfLives  -> integer` | 塔模式：当前生命数。 |
| `PLAYSTATE.InvincibilityDurationSpeedDependent  -> number` | 塔模式：按歌曲速度调整后的无敌时长。 |
| `PLAYSTATE.InvincibilityDuration  -> integer` | 塔模式：基础无敌时长。 |
| `PLAYSTATE:WasPlayEndedNormally()  -> bool` | 上一次演奏是否进行到了结尾。 |
| `PLAYSTATE:WasPlayAborted()  -> bool` | 玩家是否提前退出了上一次演奏。 |
| `PLAYSTATE:GetGoodCount(player)  -> integer` | Good（良）判定的数量。 |
| `PLAYSTATE:GetOkCount(player)  -> integer` | Ok（可）判定的数量。 |
| `PLAYSTATE:GetBadCount(player)  -> integer` | Bad（不可）判定的数量。 |
| `PLAYSTATE:GetRollCount(player)  -> integer` | 连打击打的数量。 |
| `PLAYSTATE:GetADLibCount(player)  -> integer` | 击中的 ADLIB 音符数量。 |
| `PLAYSTATE:GetMissedADLibCount(player)  -> integer` | 错过的 ADLIB 音符数量。 |
| `PLAYSTATE:GetBoomCount(player)  -> integer` | 击中的地雷音符数量。 |
| `PLAYSTATE:GetAvoidedBoomCount(player)  -> integer` | 避开的地雷音符数量。 |
| `PLAYSTATE:GetScore(player)  -> integer` | 当前分数。 |
| `PLAYSTATE:GetCombo(player)  -> integer` | 当前连击。 |
| `PLAYSTATE:GetHighestCombo(player)  -> integer` | 达到过的最高连击。 |
| `PLAYSTATE:IsClear(player)  -> bool` | 魂槽是否达到过关线。 |
| `PLAYSTATE:IsAssistedClear(player)  -> bool` | 是否在启用降分 Mod 的情况下过关。 |
| `PLAYSTATE:IsFullCombo(player)  -> bool` | 过关、非辅助、没有 Bad 判定且未击中地雷。 |
| `PLAYSTATE:IsPerfect(player)  -> bool` | 全连击且没有 Ok 判定。 |
| `PLAYSTATE:IsAlive()  -> bool` | 塔模式：是否还有生命。 |
| `PLAYSTATE:IsPass()  -> bool` | 段位模式：考核状态是否不为不合格。 |
| `PLAYSTATE:IsRedPass()  -> bool` | 段位模式：考核状态是否为普通合格。 |
| `PLAYSTATE:IsGoldPass()  -> bool` | 段位模式：考核状态是否为金合格。 |
| `PLAYSTATE:IsDanClear()  -> bool` | 段位模式：合格且非辅助。 |
| `PLAYSTATE:IsDanFullCombo()  -> bool` | 段位模式：段位过关且没有 Bad 判定、未击中地雷。 |
| `PLAYSTATE:IsDanPerfect()  -> bool` | 段位模式：段位全连击且没有 Ok 判定。 |

### 解锁条件句柄

名牌、角色或小角色的解锁要求。

<div class="callout warn">
名牌信息句柄、角色列表条目和小角色句柄的 UnlockCondition 属性返回此句柄。没有条件的项目（HasCondition 为 false）默认可用：IsUnlockable 返回 true，消息为空。条件的词汇与 Unlock.json 和谱面解锁条件一致；见<a href="../guides/unlockables.md">谱面解锁条件</a>指南。
</div>

| 方法 | 说明 |
| --- | --- |
| `cond.HasCondition  -> bool` | 该项目是否有解锁条件。 |
| `cond:GetConditionType()  -> string` | 条件类型 id（例如 "ch"、"cs"、"gt"、"gc" 或 "ig"），或空字符串。 |
| `cond:GetCoinPrice()  -> integer` | 条件的金币价格，或 0。 |
| `cond:GetConditionMessage()  -> string` | 条件的本地化描述。 |
| `cond:IsUnlockable(player)  -> bool` | 该玩家当前是否满足条件。 |
| `cond:GetBlockedMessage(player)  -> string` | 该玩家未满足条件的原因；已满足时为空字符串。 |

## 主题与语言

### THEME

皮肤的分辨率、主题设置、皮肤范围的本地化字符串，以及主题设置的定义。

<div class="callout warn">
皮肤在 ThemeSettings.json 中声明主题设置，并把其值存储在旁边的 ThemeSettings.db3 中。getter 始终以字符串返回设置值；缺失的设置返回其声明的默认值，没有声明时返回空字符串。GetThemeSettingForPlayer 接受 1 起的玩家编号。定义索引从 0 起。
</div>

| 方法 | 说明 |
| --- | --- |
| `THEME:GetResolution()  -> vector2` | 皮肤的分辨率。 |
| `THEME:GetThemeSetting(settingId)  -> string` | 一个全局范围设置的值。 |
| `THEME:GetThemeSettingForPlayer(settingId, player)  -> string` | 1 起玩家的一个存档范围设置的值；存档没有值时为其默认值。 |
| `THEME:GetSkinString(key)  -> string` | 来自皮肤 Locales 文件夹的本地化字符串：先查当前语言，再查皮肤的默认区域设置，然后是 `[LOCALE NOT FOUND: key]`。 |
| `THEME:GetDefinitionCount()  -> integer` | ThemeSettings.json 中设置定义的数量。 |
| `THEME:GetDefinitionId(index)  -> string` | 0 起索引处定义的 id，或空字符串。 |
| `THEME:GetDefinitionScope(index)  -> string` | 定义的范围："global" 或 "save"。 |
| `THEME:GetDefinitionType(index)  -> string` | 定义的类型："bool"、"int"、"double"、"string" 或 "enum"。 |

### LANG

本地化的游戏字符串、语言切换和多语言文本值。

<div class="callout warn">
GetString 用额外的参数格式化条目。GetLanguageIds 和 GetLanguageNames 返回 .NET 数组（从 0 起，`.Length`）；GetAvailableLanguages 返回一个用 `:GetEnumerator()` 枚举的字典（见“数据与持久化”）。FromDict 接受来自 JSONLOADER 的解析后 JSON 对象（它不接受 Lua 表）；AsLocalizationData 接受来自 JSONLOADER:LoadJson 的 JsonNode。
</div>

| 方法 | 说明 |
| --- | --- |
| `LANG:GetString(key, ...)  -> string` | 某个键的本地化字符串，格式占位符由额外参数填充。 |
| `LANG:ChangeLanguage(id)  -> bool` | 若该 id 存在且不同于当前语言，则切换当前语言，然后对每个已加载脚本调用 `reloadLanguage`；返回是否切换了。它不会改动 CONFIG.Language。 |
| `LANG:GetLanguageIds()  -> string[]` | 可用语言的 id。 |
| `LANG:GetLanguageNames()  -> string[]` | 可用语言的显示名称，顺序相同。 |
| `LANG:GetAvailableLanguages()  -> dict` | 语言 id 到显示名称的映射。 |
| `LANG:GetExamName(type)  -> string` | 一种段位考核类型的本地化名称。 |
| `LANG:AsLocalizationData(node)  -> localizationData` | 从形如 `{ "strings": { "<lang>": "text" } }` 的 JsonNode 构建一个本地化值。 |
| `LANG:FromDict(dict)  -> localizationData` | 从一个把语言 id 映射到文本的解析后 JSON 对象构建一个本地化值。 |
| `LANG:FromString(json)  -> localizationData` | 从一个把语言 id 映射到文本的 JSON 对象字符串构建一个本地化值；字符串无法解析时为空值。 |

```lua
local langs = LANG:GetAvailableLanguages()
local e = langs:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end

local name = LANG:FromString('{"ja":"太鼓","default":"Taiko"}'):GetString("")
```

### 本地化数据句柄

一组以语言 id 为键、解析为当前语言的字符串。

<div class="callout warn">
LANG:AsLocalizationData、LANG:FromDict 和 LANG:FromString 返回此句柄。解析顺序：当前语言 id，然后是 "default" 键，然后是传给 GetString 的回退值。
</div>

| 方法 | 说明 |
| --- | --- |
| `loc:GetString(fallback)  -> string` | 当前语言的文本，或 "default"，或回退值。 |
| `loc:SetString(langId, text)  -> nil` | 设置某个语言 id 的文本。 |
| `loc:GetAllStrings()  -> string[]` | 存储的所有文本，顺序不定。 |
