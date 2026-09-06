<!-- api/songs.md -->

# 歌曲与谱面

请求歌曲列表、遍历节点和谱面、读取成绩，以及构建段位（考核）课程。

本页使用的约定：

- 难度索引从 0 起：0 Easy、1 Normal、2 Hard、3 Extreme (`Oni`)、4 Extra Extreme (`Edit`)、5 Tower（塔）、6 Dan（段位）。
- 玩家和存档索引从 0 起（0 是玩家 1）。
- 用点书写的成员（`node.Title`）是属性；用冒号书写的成员（`node:GetChart(3)`）是方法。
- 某些成员返回 C# 集合。列表有 `.Count` 并从 0 起索引（`list[0]`）；数组有 `.Length` 并同样从 0 起索引。下面每个条目都注明了返回哪一种。
- 歌曲列表只有在歌曲枚举完成后才完整。请在 `afterSongEnum()` 回调中请求它（见[模块与生命周期](activities.md)），或者先检查 `IsSongsEnumDone()` 全局函数；它在枚举完成后返回 true。

## 请求歌曲列表

### RequestSongList

从设置对象构建可导航歌曲列表的全局函数。

<div class="callout warn">
以普通全局函数的形式提供。传给它一个由 GenerateSongListSettings() 创建的设置对象。该调用从游戏已枚举的歌曲一次性构建歌曲树；句柄按引用保留设置对象，因此你可以更改某个字段后调用句柄的 ReloadSongList() 重新构建。
</div>

| 方法 | 说明 |
| --- | --- |
| `RequestSongList(settings)  -> song list handle` | 根据给定的歌曲列表设置构建并返回一个歌曲列表句柄。 |

```lua
local settings = GenerateSongListSettings()
settings.AppendMainRandomBox = false
settings:SetExcludedGenreFolders({ "Dan", "Tower" })

local list = RequestSongList(settings)
local node = list:GetSelectedSongNode()
```

### GenerateSongListSettings

创建带默认值的歌曲列表设置对象的全局函数。

| 方法 | 说明 |
| --- | --- |
| `GenerateSongListSettings()  -> song list settings` | 返回一个字段均为默认值的新歌曲列表设置对象。 |

### 歌曲列表设置

控制歌曲列表包含哪些节点以及导航行为的配置对象。

<div class="callout warn">
下面的所有成员都是 Lua 可直接读写的公共字段（settings.HideEmptyFolders = false），两个 setter 方法除外，它们接受 Lua 表。ExcludedGenreFolders 和 MandatoryDifficultyList 是 C# 数组；请通过其 setter 方法设置。
</div>

| 方法 | 说明 |
| --- | --- |
| `settings.AppendMainRandomBox  (bool, default true)` | 为 true 时，列表在其根部追加一个随机选曲项。 |
| `settings.AppendSubRandomBoxes  (bool, default true)` | 为 true 时，列表在每个文件夹末尾追加一个随机选曲项。 |
| `settings.SubBackBoxFrequency  (int, default 7)` | 在每个文件夹内，列表在开头以及每 N 个条目之后插入一个返回项；0 禁用生成返回项。 |
| `settings.ExcludedGenreFolders  (string array)` | 列表排除的分类文件夹名称。通过 SetExcludedGenreFolders 设置它。 |
| `settings.RootGenreFolder  (string, default nil)` | 设置后，列表的根变为分类与此名称匹配的第一个文件夹（深度优先）；为 nil 时，根是顶层。 |
| `settings.RootGenreFolderNode  (song node, default nil)` | RootGenreFolder 的节点形式。设置后，它优先于字符串，可用于区分同名分类的文件夹。 |
| `settings.MandatoryDifficultyList  (Difficulty array, default nil)` | 歌曲必须拥有才会出现在列表中的难度；nil 表示无要求。通过 SetMandatoryDifficultyList 设置它。 |
| `settings.MandatoryDifficultyMatchAll  (bool, default true)` | true 要求列出的每个难度都存在（AND）；false 要求至少存在一个（OR）。 |
| `settings.HideEmptyFolders  (bool, default true)` | 递归隐藏不包含任何可见歌曲的文件夹。 |
| `settings.FlattenOpenedFolders  (bool, default true)` | 为 true 时，当前页是整棵树，已打开的文件夹就地展开（关闭的文件夹计为单个条目）。为 false 时，当前页只包含光标节点的同级节点。 |
| `settings.ModuloPagination  (bool, default true)` | 为 true 时，GetSongNodeAtOffset 在页内回绕；为 false 时超出任一端返回 nil。 |
| `settings.ModuloMovement  (bool, default true)` | 为 true 时，Move 在页内回绕；为 false 时在任一端停住。 |
| `settings.ExcludeHiddenSongs  (bool, default true)` | 排除 HiddenIndex 为 3（隐藏）的歌曲。 |
| `settings.ExcludeLockedSongs  (bool, default false)` | 为 true 时，页面排除锁定的歌曲，导航永远不会落在它们上面。 |
| `settings.IgnoreUnlockables  (bool, default false)` | 为 true 时，列表忽略 ExcludeLockedSongs，且 GetRandomNodeInFolder 可能返回锁定的歌曲。IsLocked 等节点属性仍报告真实状态。 |
| `settings:SetExcludedGenreFolders(table)  -> void` | 从一张分类名称字符串的 Lua 表设置 ExcludedGenreFolders。 |
| `settings:SetMandatoryDifficultyList(table)  -> void` | 从一张难度索引的 Lua 表设置 MandatoryDifficultyList。 |

### 歌曲列表句柄

由 RequestSongList 返回的可导航歌曲树，带有光标、文件夹导航和搜索。

<div class="callout warn">
搜索方法接受一个 Lua 函数，该函数收到一个歌曲节点并返回布尔值。返回多个节点的方法返回 C# 列表（.Count，从 0 起索引）。
</div>

| 方法 | 说明 |
| --- | --- |
| `list:ReloadSongList()  -> void` | 根据当前的歌曲和设置重建整棵树，并把光标移到第一个节点。 |
| `list:GetRoot()  -> song node` | 返回树的根节点。 |
| `list:GetSelectedSongNode()  -> song node` | 返回光标所在的节点；列表为空时返回 nil。 |
| `list:GetSongNodeAtOffset(offset)  -> song node` | 返回当前页内距光标给定偏移处的节点，按 ModuloPagination 回绕或返回 nil。 |
| `list:Move(offset)  -> void` | 在当前页内把光标移动给定偏移，按 ModuloMovement 回绕或停住。 |
| `list:OpenFolder()  -> bool` | 打开光标所在的文件夹并把光标移到其第一个子节点；光标不在已关闭的非空文件夹上时返回 false。 |
| `list:CloseFolder()  -> bool` | 关闭包含光标的文件夹并把光标移到该文件夹；没有可关闭的内容时返回 false。离开虚拟文件夹时恢复 OpenVirtualFolder 保存的光标。 |
| `list:OpenVirtualFolder(baseFolder, songs, title)  -> bool` | 打开一个名为 `title` 的临时文件夹，包含 Lua 表 `songs`（键 1..n）中的歌曲节点，带有生成的返回项和末尾的随机选曲项，并把光标移入其中。`baseFolder` 成为虚拟文件夹的父节点。表中没有歌曲节点时返回 false。 |
| `list:GetSongByUniqueId(id)  -> song node` | 返回唯一 id 匹配的第一首歌曲，或 nil。 |
| `list:GetRandomNodeInFolder(node, recursive, predicate)  -> song node` | 在 `node` 的同级节点（包含它的那一页）中随机挑选一首歌曲。`recursive`（默认 true）时，挑选范围也涵盖同级文件夹内的歌曲。除非设置了 IgnoreUnlockables，否则它跳过锁定的歌曲。`predicate` 是可选的。没有符合条件的歌曲时返回 nil。 |
| `list:SearchSongsByPredicate(predicate)  -> list of song nodes` | 返回树中谓词返回 true 的每个歌曲节点。 |
| `list:SearchFirstSongByPredicate(predicate)  -> song node` | 返回谓词返回 true 的第一个歌曲节点，或 nil。 |
| `list:SearchNodesByPredicate(predicate)  -> list of song nodes` | 与 SearchSongsByPredicate 类似，但也会测试文件夹和其他非歌曲节点。 |

```lua
local results = list:SearchSongsByPredicate(function(node)
    return node:GetChart(3) ~= nil   -- 拥有 Extreme 谱面
end)
for i = 0, results.Count - 1 do
    local node = results[i]
end
```

## 节点、谱面与成绩

### 歌曲节点

歌曲列表中的单个条目：一首歌曲、一个文件夹、一个返回项或一个随机选曲项。

<div class="callout warn">
歌曲列表句柄、SONGMOUNT:ChosenSongNode() 和 DANBUILDER:GetSong() 返回歌曲节点。属性均为只读。元数据属性在非歌曲节点上返回 nil。请通过歌曲列表句柄（Move、OpenFolder、CloseFolder 和搜索方法）在列表中导航。
</div>

| 方法 | 说明 |
| --- | --- |
| `node.NotNull  (bool)` | 节点包装了真实的歌曲列表条目时为 true。 |
| `node.IsFolder  (bool)` | 节点是文件夹时为 true。 |
| `node.IsRandom  (bool)` | 节点是随机选曲项时为 true。 |
| `node.IsReturn  (bool)` | 节点是返回项时为 true。 |
| `node.IsSong  (bool)` | 节点是可演奏的歌曲时为 true。 |
| `node.SongCount  (int)` | 直接子歌曲的数量。 |
| `node.RecursiveSongCount  (int)` | 本节点下（含子文件夹）的歌曲数量。 |
| `node.VisibleSongCount  (int)` | HiddenIndex 不为 3 的直接子歌曲的数量。 |
| `node.RecursiveVisibleSongCount  (int)` | 本节点下（含子文件夹）的可见歌曲数量。 |
| `node.BoxType  (string)` | 文件夹框样式字符串，或 nil。 |
| `node.BgType  (string)` | 背景样式字符串，或 nil。 |
| `node.BoxChara  (string)` | 框角色字符串，或 nil。 |
| `node.ForeColor  (color)` | 节点的前景色，或 nil。 |
| `node.BackColor  (color)` | 节点的背景色，或 nil。 |
| `node.BoxColor  (color)` | 节点的框颜色，或 nil。 |
| `node.Title  (string)` | 显示标题。返回项和随机选曲项返回由父文件夹标题构成的本地化“返回”/“随机”文本。 |
| `node.Subtitle  (string)` | 歌曲的副标题，或 nil。 |
| `node.Genre  (string)` | 分类字符串，或 nil。 |
| `node.UniqueId  (string)` | 歌曲的唯一 id，或 nil。 |
| `node.Maker  (string)` | MAKER 字段的完整字符串，或 nil。 |
| `node.Charters  (string array)` | 按逗号分割的 MAKER 字段。 |
| `node.Side  (int)` | SIDE 值：0 普通，1 ex，2 两者。 |
| `node.Explicit  (bool)` | 歌曲被标记为含不宜内容时为 true；非歌曲为 nil。 |
| `node.HasVideo  (bool)` | 歌曲有背景影片时为 true；非歌曲为 nil。 |
| `node.DemoStart  (int)` | 试听 BGM 的偏移（毫秒）。 |
| `node.AudioPath  (string)` | 歌曲 BGM 文件的绝对路径，或空字符串。 |
| `node.HasPreimage  (bool)` | 歌曲声明了预览图片时为 true。 |
| `node.PreimagePath  (string)` | 预览图片的绝对路径。请先检查 HasPreimage；没有预览图片时它只是歌曲文件夹。 |
| `node:GetPreimage()  -> texture` | 从磁盘加载预览图片并返回一张新纹理；歌曲没有预览图片时返回 nil。用完后请释放该纹理。 |
| `node.ChartMd5  (string)` | 谱面文件的 MD5（大写十六进制），或空字符串。它在不同安装之间保持不变；UniqueId 则不会。 |
| `node:GetChart(diff)  -> chart` | 返回给定难度索引的谱面；歌曲没有该谱面时返回 nil。 |
| `node:GetCustomCommand(key)  -> string` | 返回一个全局范围自定义命令的值（放在第一个 COURSE 之前、以点开头的头部，key 包含点，例如 ".VAULT_NAME"），或 nil。 |
| `node:GetCustomCommands()  -> dictionary` | 以 C# 字典对象返回所有全局范围的自定义命令。查找时优先使用 GetCustomCommand。 |
| `node.UnlockCondition  (unlock condition)` | 解锁条件对象（见“解锁条件”）。 |
| `node.UnlockText  (string)` | 歌曲定义了自定义解锁文本时为该文本，否则为生成的条件消息。 |
| `node.IsLocked  (bool)` | 该歌曲当前被锁定时为 true；非歌曲始终为 false。 |
| `node.HiddenIndex  (int)` | 解锁系统的显示状态：0 显示，1 灰显，2 模糊，3 隐藏（非歌曲为 0）。 |
| `node.Rarity  (string)` | 稀有度标签；没有解锁条目的歌曲为 "Common"，非歌曲为 "-"。 |
| `node:Mount(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | 按每个玩家给定的难度索引（默认 0）选定本歌曲用于演奏。它只检查前 CONFIG.PlayerCount 个索引；节点不是歌曲，或某个活跃玩家的难度缺失或超出范围时返回 false。 |
| `node:MountIfNotLocked(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | 与 Mount 相同，但歌曲被锁定时返回 false 且不挂载。 |

### 谱面

歌曲的一个难度：等级、BPM、谱师、塔与段位数据、最佳成绩和自定义命令。

<div class="callout warn">
歌曲节点的 GetChart(diff) 返回谱面。属性均为只读。谱面没有谱面信息时，BPM、Life、TotalFloorCount、TowerType 和 DanTick 返回 nil。Difficulty 和 LevelIcon 是枚举对象；请通过 DifficultyAsInt、IsPlus 和 IsMinus 比较它们。
</div>

| 方法 | 说明 |
| --- | --- |
| `chart.NotNull  (bool)` | 谱面包装了真实的谱面数据时为 true。 |
| `chart.Parent  (song node)` | 本谱面所属的歌曲节点。 |
| `chart.Difficulty  (enum)` | 以枚举对象表示的难度。 |
| `chart.DifficultyAsInt  (int)` | 难度索引。 |
| `chart.Level  (int)` | 星级。 |
| `chart.LevelDecimal  (number)` | 带小数部分的等级（例如 12.888）；未设定时为整数等级。 |
| `chart.LevelFirstDecimal  (int)` | LevelDecimal 的第一位小数（0-9）。 |
| `chart.LevelIcon  (enum)` | 以枚举对象表示的等级图标。 |
| `chart.IsPlus  (bool)` | 等级图标为 "plus" 时为 true。 |
| `chart.IsMinus  (bool)` | 等级图标为 "minus" 时为 true。 |
| `chart.NotesDesigner  (string)` | NOTESDESIGNER 字段的完整字符串。 |
| `chart.Charters  (string array)` | 按逗号分割的 NOTESDESIGNER 字段。 |
| `chart.BPM  (number)` | 主 BPM，或 nil。 |
| `chart.BaseBPM  (number)` | 基础 BPM，或 nil。 |
| `chart.MinBPM  (number)` | 最低 BPM，或 nil。 |
| `chart.MaxBPM  (number)` | 最高 BPM，或 nil。 |
| `chart.Life  (int)` | 塔模式的生命数，或 nil。 |
| `chart.TotalFloorCount  (int)` | 塔的层数，或 nil。 |
| `chart.TowerType  (string)` | 塔类型字符串，或 nil。 |
| `chart.DanTick  (int)` | 段位牌刻度值，或 nil。 |
| `chart.DanTickColor  (color)` | 段位牌刻度颜色（谱面没有谱面信息时为白色）。 |
| `chart.DanSongs  (array of dan songs)` | 组成本段位谱面的歌曲。 |
| `chart.DanExams  (array of dan exams)` | 本段位谱面的全局考核条件。 |
| `chart:GetSongExam(songIdx, examSlot)  -> dan exam` | 返回给定 1 起歌曲索引和 1 起考核槽位的单曲考核；不存在时结果的 IsSet 为 false。 |
| `chart:GetPlayerBestScore(save)  -> best score info` | 返回给定存档在本谱面上的最佳成绩摘要。 |
| `chart:GetCustomCommand(key)  -> string` | 返回一个谱面范围自定义命令的值（本 COURSE 块内以点开头的头部，key 包含点），或 nil。 |
| `chart:GetCustomCommands()  -> dictionary` | 以 C# 字典对象返回所有谱面范围的自定义命令。 |
| `chart.SongFolder  (string)` | 存放谱面文件的绝对文件夹路径。 |
| `chart.ChartPath  (string)` | 谱面文件的绝对路径。 |
| `chart.UniqueId  (string)` | 歌曲的唯一 id，或空字符串。 |
| `chart:Select(player)  -> bool` | 把本谱面标记为给定玩家选定的难度；玩家 0 同时设置选定的歌曲。谱面无效时返回 false。 |

### 最佳成绩信息

某个存档在一个谱面上的最佳成绩摘要。

<div class="callout warn">
chart:GetPlayerBestScore(save) 返回此对象。所有成员均为只读。无效的存档索引得到一条空记录。
</div>

| 方法 | 说明 |
| --- | --- |
| `info.ScoreRank  (int)` | 达到过的最佳分数评级。 |
| `info.ClearStatus  (int)` | 达到过的最佳过关状态。 |
| `info.HighScore  (int)` | 最高分。 |
| `info.HasBeenPlayed  (bool)` | 谱面至少有一次记录在案的演奏（无论结果如何）时为 true。 |
| `info.PlayCount  (int)` | 本谱面的总演奏次数，所有 Mod 变体合计。 |

## 段位考核

### 段位歌曲

段位课程中的一个歌曲条目。

<div class="callout warn">
chart.DanSongs 的元素。所有成员均为只读。
</div>

| 方法 | 说明 |
| --- | --- |
| `dansong.Title  (string)` | 歌曲的标题。 |
| `dansong.SubTitle  (string)` | 歌曲的副标题。 |
| `dansong.Genre  (string)` | 歌曲的分类。 |
| `dansong.Level  (int)` | 歌曲的星级。 |
| `dansong.Difficulty  (enum)` | 以枚举对象表示的难度。 |
| `dansong.DifficultyAsInt  (int)` | 难度索引。 |

### 段位考核条件

段位课程的一条合格/不合格条件。

<div class="callout warn">
chart.DanExams 的元素，或由 chart:GetSongExam() 返回。所有成员均为只读。TypeAsInt 的值：0 魂槽，1 良判定，2 可判定，3 不可判定，4 分数，5 连打，6 击打数，7 连击，8 准确率，9 ADLIB 判定，10 地雷判定。RangeAsInt 的值：0“至少”，1“少于”。
</div>

| 方法 | 说明 |
| --- | --- |
| `danexam.IsSet  (bool)` | 该考核槽位启用时为 true。 |
| `danexam.RedValue  (int)` | 红（合格）阈值。 |
| `danexam.GoldValue  (int)` | 金阈值。 |
| `danexam.TypeAsInt  (int)` | 考核类型。 |
| `danexam.RangeAsInt  (int)` | 比较方向。 |

### DANBUILDER

用于在内存中由歌曲节点、难度和考核条件组装段位课程并挂载以供演奏的全局对象。

<div class="callout warn">
以全局对象 DANBUILDER 的形式提供。歌曲和槽位索引从 1 起，但传给 AddSong 的难度是从 0 起的难度索引。考核槽位为 1 到 7。考核类型字符串（不区分大小写，括号内为简写）："judgeperfect"（jp）、"judgegood"（jg）、"judgebad"（jb）、"score"（s）、"roll"（r）、"hit"（h）、"combo"（c）、"accuracy"（a）、"judgeadlib"（ja）、"judgemine"（jm）；其他任何字符串都表示魂槽。lessThan = true 使考核成为“少于”检查，false 为“至少”检查。构建器在调用之间保留状态；构建新课程前请调用 Clear()。
</div>

| 方法 | 说明 |
| --- | --- |
| `DANBUILDER.SongCount  (int)` | 目前已添加的歌曲数量。 |
| `DANBUILDER:AddSong(node, diff)  -> void` | 以给定的 0 起难度索引追加一个歌曲节点。 |
| `DANBUILDER:GetSong(i)  -> song node` | 返回 1 起索引 i 处的歌曲节点，或 nil。 |
| `DANBUILDER:GetSongDiff(i)  -> int` | 返回为 1 起索引 i 处的歌曲存储的难度索引，或 -1。 |
| `DANBUILDER:SetTitle(title)  -> void` | 设置课程标题（默认 "Dynamic Dan"）。 |
| `DANBUILDER:SetSubtitle(subtitle)  -> void` | 设置课程副标题。 |
| `DANBUILDER:SetDanTick(tick)  -> void` | 设置段位牌刻度值（默认 2）。 |
| `DANBUILDER:SetDanTickColor(r, g, b)  -> void` | 从 0-255 的分量设置段位牌刻度颜色（默认白色）。 |
| `DANBUILDER:SetGlobalExam(slot, type, red, gold, lessThan)  -> void` | 在给定槽位设置一条课程范围的考核。 |
| `DANBUILDER:SetPerSongExam(songIndex, slot, type, red, gold, lessThan)  -> void` | 按 1 起歌曲索引和槽位设置一条只作用于一首歌曲的考核。 |
| `DANBUILDER:Clear()  -> void` | 移除所有歌曲和考核，并把元数据重置为默认值。 |
| `DANBUILDER:Mount()  -> bool` | 在内存中构建课程谱面，并以段位难度为玩家 1 选定它用于演奏；构建器中没有歌曲或构建失败时返回 false。 |

```lua
DANBUILDER:Clear()
DANBUILDER:SetTitle("Custom course")
DANBUILDER:AddSong(list:GetSongByUniqueId(id1), 3)
DANBUILDER:AddSong(list:GetSongByUniqueId(id2), 3)
DANBUILDER:SetGlobalExam(1, "gauge", 90, 100, false)
DANBUILDER:SetPerSongExam(2, 2, "judgebad", 10, 5, true)
if DANBUILDER:Mount() then
    return Exit("play")
end
```

## 解锁、虚拟槽位与 Mod 图标

### 解锁条件

描述解锁歌曲的条件，以及玩家当前是否满足该条件。

<div class="callout warn">
node.UnlockCondition 返回此对象。HasCondition 是属性；其余是方法。没有解锁条目的歌曲报告 HasCondition = false 和 IsUnlockable = true。
</div>

| 方法 | 说明 |
| --- | --- |
| `cond.HasCondition  (bool)` | 歌曲有显式解锁条件时为 true。 |
| `cond:GetConditionMessage()  -> string` | 返回条件的可读描述，或空字符串。 |
| `cond:GetConditionType()  -> string` | 返回条件类型 id（例如 "ch"、"cs"、"gt"、"gc"、"ig"），或空字符串。 |
| `cond:GetCoinPrice()  -> int` | 返回金币价格，或 0。 |
| `cond:IsUnlockable(player)  -> bool` | 给定玩家满足条件时返回 true。 |
| `cond:GetBlockedMessage(player)  -> string` | 返回该玩家未满足条件的原因；已满足时返回空字符串。 |

### VIRTUALSLOTS

用于读写五个虚拟角色槽位（V1-V5）以及把玩家位重定向为显示某个槽位视觉内容的全局对象。

<div class="callout warn">
以全局对象 VIRTUALSLOTS 的形式提供。槽位索引为 1 到 5；setter 忽略超出范围的索引，getter 对其返回默认值。这些方法不会向磁盘写入任何内容。AI 槽位由引擎管理，本全局对象无法编辑它。
</div>

| 方法 | 说明 |
| --- | --- |
| `VIRTUALSLOTS:GetCharacter(slot)  -> string` | 返回该槽位的角色文件夹名称，或 "None"。 |
| `VIRTUALSLOTS:SetCharacter(slot, folderName)  -> void` | 设置该槽位的角色文件夹名称。 |
| `VIRTUALSLOTS:GetPuchichara(slot)  -> string` | 返回该槽位的小角色文件夹名称，或 "None"。 |
| `VIRTUALSLOTS:SetPuchichara(slot, folderName)  -> void` | 设置该槽位的小角色文件夹名称。 |
| `VIRTUALSLOTS:GetNameplateName(slot)  -> string` | 返回该槽位名牌上的玩家名，或 "VSlot"。 |
| `VIRTUALSLOTS:SetNameplateName(slot, name)  -> void` | 设置该槽位名牌上的玩家名。 |
| `VIRTUALSLOTS:GetNameplateTitle(slot)  -> string` | 返回该槽位名牌的称号文本。 |
| `VIRTUALSLOTS:SetNameplateTitle(slot, title)  -> void` | 设置该槽位名牌的称号文本。 |
| `VIRTUALSLOTS:GetNameplateDan(slot)  -> string` | 返回该槽位名牌的段位文本。 |
| `VIRTUALSLOTS:SetNameplateDan(slot, dan)  -> void` | 设置该槽位名牌的段位文本。 |
| `VIRTUALSLOTS:SetNameplateById(slot, nameplateId)  -> void` | 按 id 从名牌数据库应用一个名牌：设置称号文本、类型和稀有度。未知的 id 只记录该 id。 |
| `VIRTUALSLOTS:SetNameplateType(slot, type)  -> void` | 直接设置名牌称号类型（样式索引）。 |
| `VIRTUALSLOTS:SetNameplateRarity(slot, rarity)  -> void` | 直接设置名牌称号的稀有度索引。 |
| `VIRTUALSLOTS:SetNameplateDanType(slot, danType)  -> void` | 设置段位牌类型。 |
| `VIRTUALSLOTS:SetNameplateDanGold(slot, gold)  -> void` | 设置段位牌是否显示为金色。 |
| `VIRTUALSLOTS:MountSlot(playerSpot, slotInfo)  -> void` | 让玩家位 1-5 显示 `slotInfo` 的视觉内容："1P"-"5P"（某个玩家的存档）、"AI"，或 "V1"-"V5"。该覆盖会一直保持，直到对同一玩家位的下一次 MountSlot 调用。 |

### MODICONS

在屏幕位置绘制玩家当前启用的 Mod 图标的全局对象。

<div class="callout warn">
以全局对象 MODICONS 的形式提供。绘制由 modicons 只读活动完成；第一次 Draw 调用会激活它。
</div>

| 方法 | 说明 |
| --- | --- |
| `MODICONS:Draw(player, x, y, alpha)  -> void` | 使用菜单布局在 (x, y) 绘制给定玩家的 Mod 图标；alpha 可选（默认 255）。 |

## 回放与选定的歌曲

### REPLAY

列出某个谱面已保存的回放并开始播放其中之一的全局对象。

<div class="callout warn">
以全局对象 REPLAY 的形式提供。ListReplays 返回回放头信息的 C# 数组（.Length，从 0 起）。Watch 加载一个回放并只为下一次演奏预备回放；游戏在内存中应用回放的 Mod，之后恢复先前的 Mod。songFolder 和 chartPath 来自谱面的 SongFolder 和 ChartPath。
</div>

| 方法 | 说明 |
| --- | --- |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN, chartPath)  -> array of replay headers` | 返回该谱面和难度最多 topN 个回放，按分数排序。chartPath 让列表能够计算 ChecksumMismatch。 |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN)  -> array of replay headers` | 同上，但不带谱面路径（列表跳过 ChecksumMismatch）。 |
| `REPLAY:ListReplaysAsync(songFolder, uniqueId, difficulty, topN, chartPath)  -> replay list handle` | 在后台线程运行同样的列举，并返回一个可轮询的句柄。 |
| `REPLAY:Watch(filepath, chartPath)  -> bool` | 加载回放文件并为下一次演奏预备回放；文件加载失败或回放不可观看时返回 false。chartPath 启用游戏内的“无效回放”警告。 |
| `REPLAY:Watch(filepath)  -> bool` | 同上，但不带谱面路径。 |
| `REPLAY.MODFLAG  (object)` | ModFlags 的位值：None（0）、Mirror（1）、Random（2）、SuperRandom（4）、Invisible（8）、PerfectMemory（16）、Avalanche（32）、Minesweeper（64）、Just（128）、Safe（256）、DynamicBeat（512）。 |

```lua
local flags = header.ModFlags
local mirrored = (flags & REPLAY.MODFLAG.Mirror) ~= 0
```

### 回放列表句柄

由 REPLAY:ListReplaysAsync 返回的句柄。

<div class="callout warn">
每帧轮询 IsDone；它为 true 后读取 Result。
</div>

| 方法 | 说明 |
| --- | --- |
| `handle.IsDone  (bool)` | 后台列举完成后为 true。 |
| `handle.Result  (array of replay headers)` | 列出的回放（IsDone 为 true 之前为空）。 |

### 回放头信息

一个已保存回放的元数据。

<div class="callout warn">
REPLAY:ListReplays 返回的数组或回放列表句柄的 Result 的元素。所有成员均为只读。
</div>

| 方法 | 说明 |
| --- | --- |
| `rep.FilePath  (string)` | 回放文件的绝对路径；把它传给 REPLAY:Watch。 |
| `rep.PlayerName  (string)` | 录制该回放的玩家名称。 |
| `rep.Score  (int)` | 最终分数。 |
| `rep.ClearStatus  (int)` | 该次演奏的过关状态。 |
| `rep.ScoreRank  (int)` | 该次演奏的分数评级。 |
| `rep.Good  (int)` | Good（良）判定数。 |
| `rep.Ok  (int)` | Ok（可）判定数。 |
| `rep.Bad  (int)` | Bad（不可）判定数。 |
| `rep.Roll  (int)` | 连打击打数。 |
| `rep.MaxCombo  (int)` | 最大连击。 |
| `rep.Boom  (int)` | 地雷击中数。 |
| `rep.ADLib  (int)` | ADLIB 击中数。 |
| `rep.ModFlags  (int)` | 使用的 Mod 位掩码（见 REPLAY.MODFLAG）。 |
| `rep.ScrollSpeed  (int)` | 该次演奏的流速设置。 |
| `rep.SongSpeed  (int)` | 该次演奏的歌曲速度设置。 |
| `rep.JudgeStrictness  (int)` | 该次演奏的判定区间设置。 |
| `rep.Date  (string)` | 格式为 "yyyy-MM-dd HH:mm" 的演奏日期。 |
| `rep.Timestamp  (int)` | 以原始 tick 表示的演奏日期。 |
| `rep.ChartUniqueID  (string)` | 谱面的唯一 id。 |
| `rep.ChartDifficulty  (int)` | 所录制演奏的难度索引。 |
| `rep.ChartChecksum  (string)` | 随回放存储的谱面 MD5。 |
| `rep.RandomSeed  (int)` | 音符打乱的种子；文件未存储时为 -1。 |
| `rep.GameMode  (int)` | 所录制演奏的游戏模式。 |
| `rep.GameVersion  (int)` | 录制该回放的游戏版本。 |
| `rep.Watchable  (bool)` | 游戏能够如实播放该回放时为 true。 |
| `rep.UnwatchableReason  (string)` | Watchable 为 false 时，回放不可观看的原因。 |
| `rep.OldVersion  (bool)` | 该回放由旧版游戏录制时为 true。 |
| `rep.ChecksumMismatch  (bool)` | 谱面文件已与录制时不一致时为 true（仅在你传入谱面路径时计算）。 |

### SONGMOUNT

当前选定用于演奏的歌曲的只读全局对象。

<div class="callout warn">
以全局对象 SONGMOUNT 的形式提供。它反映由歌曲节点的 Mount()、谱面的 Select() 或 DANBUILDER:Mount() 设置的状态。
</div>

| 方法 | 说明 |
| --- | --- |
| `SONGMOUNT:ChosenUniqueId()  -> string` | 返回选定歌曲的唯一 id，或空字符串。 |
| `SONGMOUNT:ChosenDifficulty()  -> int` | 返回为玩家 1 选定的难度索引。 |
| `SONGMOUNT:ChosenSongNode()  -> song node` | 以歌曲节点（不含子节点）返回选定的歌曲；未选定时返回 nil。 |
