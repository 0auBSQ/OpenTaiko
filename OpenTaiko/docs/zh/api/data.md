<!-- api/data.md -->

# 数据与持久化

保存在重启后仍保留的数据、查询 SQLite 文件、从模块文件夹读取文件、JSON 和 INI、在模块之间共享资源，以及读取或更改游戏配置。

传给 DATABASE、SQL、STORAGE、JSONLOADER、INILOADER 和 SHARED 的相对路径相对于正在运行的模块的目录（存放其 `Script.lua` 的文件夹）解析。

本页的某些方法返回 .NET 集合，其行为与 Lua 表不同：

- 数组（`string[]`、`int[]`、`double[]`）从索引 0 开始，并公开 `.Length`。
- 字典（解析后的 JSON、SQL 行、语言映射）用 `d["key"]` 取值（从 JSON 解析出的数组则用 `d[1]`），并通过 `d:GetEnumerator()` 枚举；`pairs` 和 `#` 对它们不起作用。

```lua
local files = STORAGE:GetFiles("maps", "*.json")
for i = 0, files.Length - 1 do
    print(files[i])
end

local e = dict:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end
```

## 键值数据库

### DATABASE

打开在重启后仍保留字符串值的 LMDB 键值存储，位于模块文件夹内或游戏全局的数据文件夹中。

<div class="callout warn">
每次 Read 和 Write 都会打开并关闭各自的 LMDB 环境，因此每次调用都很昂贵；请在 Lua 中缓存值，并在写入时刷新缓存。在只读模块（只读活动和背景）中，DATABASE 返回的存储的 Write 会记录一条错误并且不做任何事。
</div>

| 方法 | 说明 |
| --- | --- |
| `DATABASE:OpenLocalDatabase(path)  -> database` | 在相对于模块目录的路径上打开（必要时创建）一个存储。 |
| `DATABASE:OpenGlobalDatabase(path)  -> database` | 在游戏文件夹的 `Global/ApplicationData/LMDB/` 下打开（必要时创建）一个存储，由所有模块共享。 |

### 数据库句柄

由 DATABASE 返回的键值存储，把字符串键映射到字符串值。

<div class="callout warn">
值只能是字符串；请自行转换数字和布尔值（例如用 tostring 和 tonumber）。
</div>

| 方法 | 说明 |
| --- | --- |
| `database:Write(key, value)  -> nil` | 把一个字符串存储在该键下并提交。在只读模块中记录一条错误并且不做任何事。 |
| `database:Read(key)  -> string` | 返回存储在该键下的字符串；键缺失或读取失败时返回 nil。 |
| `database:Dispose()  -> nil` | 不做任何事；句柄不持有打开的资源。 |

```lua
local db

function activate()
    db = DATABASE:OpenGlobalDatabase("GameStatus")
    if db:Read("new_user") == nil then
        db:Write("new_user", "true")
    end
end
```

## SQL 数据库

### SQL

打开模块目录内的 SQLite 数据库文件以运行 SQL 语句。

| 方法 | 说明 |
| --- | --- |
| `SQL:OpenSQLDatabase(path)  -> sql` | 在相对于模块目录的路径上打开一个 SQLite 数据库。 |

### SQL 句柄

由 SQL:OpenSQLDatabase 返回的 SQLite 连接。

<div class="callout warn">
Query 返回一个以 1..n 为键的字典；每一行是一个以列名为键的字典（见页首关于 .NET 集合的说明）。失败的语句会记录错误并返回空结果。Query 把语句文本原样传给 SQLite，不做参数绑定，因此请对拼接进去的任何值进行转义。
</div>

| 方法 | 说明 |
| --- | --- |
| `sql:Query(query)  -> rows` | 执行 SQL 文本并返回结果行。 |

```lua
local db

function activate()
    db = SQL:OpenSQLDatabase("Databases/Items.db3")
    local rows = db:Query("SELECT * FROM itempool WHERE Slot = 'regular'")
    for i = 1, rows.Count do
        print(rows[i]["Code"])
    end
end
```

## 文件、JSON 与 INI

### STORAGE

以模块目录为根的文件访问，以及用于分享在线大厅码的辅助方法。

<div class="callout warn">
WriteText 只在模块文件夹内写入：它拒绝绝对路径和解析到文件夹之外的路径。ReadText 也接受绝对路径。大厅码辅助方法使用可执行文件旁的共享 `Global/Lobbycodes/` 文件夹。只读模块可以使用 STORAGE 的每一个方法。
</div>

| 方法 | 说明 |
| --- | --- |
| `STORAGE:GetFiles(dir, pattern)  -> string[]` | 列出模块文件夹某个子目录中匹配搜索模式（例如 `"*.png"`）的文件。条目是相对于模块文件夹的路径，包含该子目录。目录必须存在。 |
| `STORAGE:FileExists(path)  -> bool` | 相对于模块文件夹的路径上存在文件时为 true。 |
| `STORAGE:DirectoryExists(path)  -> bool` | 相对于模块文件夹的路径上存在目录时为 true。 |
| `STORAGE:WriteText(name, contents)  -> bool` | 把文本写入模块文件夹下的文件，并创建所需的子目录。对于绝对路径、逃逸路径或失败时返回 false。 |
| `STORAGE:ReadText(name)  -> string` | 返回文件的原始文本（相对于模块文件夹，或绝对路径）；文件缺失或不可读时返回 nil。 |
| `STORAGE:GetFullPath(name)  -> string` | 返回模块文件夹下某个文件的绝对路径；输入为空或为绝对路径时返回 nil。 |
| `STORAGE:WriteLobbyCode(name, contents)  -> bool` | 把一个文件写入 `Global/Lobbycodes/`。对于空名称、绝对路径、含 `..` 的名称或失败时返回 false。 |
| `STORAGE:RevealLobbyCodes()  -> bool` | 在 `Global/Lobbycodes/` 打开操作系统的文件浏览器。 |
| `STORAGE:RevealInExplorer(name)  -> bool` | 打开操作系统的文件浏览器并选中指定的模块文件（Windows 和 macOS），在其他平台上则打开其所在文件夹。 |

### JSONLOADER

解析模块目录中的 JSON 文件和字符串。

<div class="callout warn">
解析后的值是 .NET 字典：对象以成员名为键，数组以 1..n 为键。直接索引一个缺失的键（`d["x"]`）会抛出错误，因此对可能失败的查找请使用 JsonGet。数字变为整数或双精度数；字符串、布尔值和 null 映射为对应的 Lua 值。LoadJson 返回一棵 JsonNode 树；用 `node["member"]` 索引它，并用 ExtractNumber / ExtractText 转换叶节点。
</div>

| 方法 | 说明 |
| --- | --- |
| `JSONLOADER:LoadJson(name)  -> JsonNode` | 把相对于模块目录的 JSON 文件解析为 JsonNode 树。文件缺失时抛出错误。 |
| `JSONLOADER:ExtractNumber(value)  -> number` | 把 JsonNode 叶节点转换为数字；对 nil 或非数字值返回 0。 |
| `JSONLOADER:ExtractText(value)  -> string` | 把 JsonNode 叶节点转换为字符串；对 nil 返回 nil。 |
| `JSONLOADER:JsonParseFile(name)  -> dict` | 解析根为对象的 JSON 文件（文件为空时得到空字典）。文件缺失或根不是对象时抛出错误。 |
| `JSONLOADER:JsonParseFileAny(name)  -> dict` | 解析根为对象或数组的 JSON 文件（相对或绝对路径）。文件缺失或为空时返回 nil。 |
| `JSONLOADER:JsonParseString(json)  -> dict` | 解析根为对象的 JSON 字符串（为空时得到空字典）。根不是对象时抛出错误。 |
| `JSONLOADER:JsonParseStringAny(json)  -> dict` | 解析根为对象或数组的 JSON 字符串。输入为空或无效时返回 nil。 |
| `JSONLOADER:JsonGet(dict, key)  -> value` | 在对象中查找字符串键，或在数组中查找整数键；键缺失或 dict 不是解析后的值时返回 nil。 |
| `JSONLOADER:JsonCount(dict)  -> int` | 解析后的对象或数组的成员数量；其他情况为 0。 |

```lua
local data = JSONLOADER:JsonParseFileAny("Config/layout.json")
local colors = data and JSONLOADER:JsonGet(data, "colors")
local n = JSONLOADER:JsonCount(colors)
for i = 1, n do
    print(JSONLOADER:JsonGet(colors, i))
end
```

### INILOADER

从模块目录加载一个扁平的 `key=value` 文件。

<div class="callout warn">
加载器在第一个 `=` 处分割每一行，跳过没有 `=` 的行，并让重复的键覆盖先前的值。它没有节、注释或引号。文件缺失时得到一个空句柄。
</div>

| 方法 | 说明 |
| --- | --- |
| `INILOADER:LoadIni(name)  -> ini` | 读取相对于模块目录的 `key=value` 文件。 |

### INI 句柄

由 INILOADER:LoadIni 返回的解析后的 INI 文件，带有类型化的 getter。

<div class="callout warn">
键缺失时 getter 返回所提供的默认值。键存在但其值解析失败时，数值 getter 返回 0。数组 getter 按逗号分割，键缺失时返回空数组。
</div>

| 方法 | 说明 |
| --- | --- |
| `ini:GetBool(key, default)  -> bool` | 值解析为整数 1 时为 true；键缺失时为默认值。 |
| `ini:GetInt(key, default)  -> int` | 值作为整数。 |
| `ini:GetDouble(key, default)  -> number` | 值作为双精度数。 |
| `ini:GetString(key, default)  -> string` | 原始字符串值。 |
| `ini:GetStringArray(key)  -> string[]` | 按逗号分割的值。 |
| `ini:GetIntArray(key)  -> int[]` | 按逗号分割的值，每一部分解析为整数（无法解析时为 0）。 |
| `ini:GetDoubleArray(key)  -> double[]` | 按逗号分割的值，每一部分解析为双精度数（无法解析时为 0）。 |

## 共享资源与配置

### SHARED

在舞台切换之后仍然保留的游戏全局纹理、声音和字符串存储，因此每个模块都可以使用加载一次的资源（例如在启动时加载的）。

<div class="callout warn">
Set* 方法在后台线程加载，并在渲染线程上换入资源；可选的 onCreate 回调在新句柄就位后收到它。对同一个键的更新的 Set* 会丢弃仍在进行中的加载。换入会释放先前的资源，这会使重新加载之前获取的任何句柄失效；请用 Get* 重新获取。UsingAbsolutePath 变体接受完整路径。纹理句柄见“图形与文本”，声音句柄见“音频”。
</div>

| 方法 | 说明 |
| --- | --- |
| `SHARED:SetSharedString(key, value)  -> nil` | 在某个键下存储一个字符串。 |
| `SHARED:GetSharedString(key)  -> string` | 返回存储在某个键下的字符串，或空字符串。 |
| `SHARED:GetSharedTexture(key)  -> texture` | 返回某个键的共享纹理；未设置时返回空纹理。 |
| `SHARED:GetSharedSound(key)  -> sound` | 返回某个键的共享声音；未设置时返回空声音。 |
| `SHARED:ClearSharedTexture(key)  -> nil` | 释放存储在某个键下的纹理，并用空纹理替换。 |
| `SHARED:ClearSharedSound(key)  -> nil` | 释放存储在某个键下的声音，并用空声音替换。 |
| `SHARED:SetSharedTexture(key, path, onCreate?)  -> nil` | 从相对于模块的路径把一张纹理加载到存储中。 |
| `SHARED:SetSharedTexture(key, path, options, onCreate?)  -> nil` | 同上，附带选项表；`{ maxSize = N }` 把解码后纹理的长边限制为 N 像素。 |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, onCreate?)  -> nil` | 从绝对路径加载一张纹理。 |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, options, onCreate?)  -> nil` | 从绝对路径加载一张纹理，附带选项表。 |
| `SHARED:SetSharedSFX(key, path, onCreate?)  -> nil` | 从相对于模块的路径加载一个音效。 |
| `SHARED:SetSharedBGM(key, path, onCreate?)  -> nil` | 从相对于模块的路径加载背景音乐（歌曲播放音量组）。 |
| `SHARED:SetSharedVoice(key, path, onCreate?)  -> nil` | 从相对于模块的路径加载一段语音。 |
| `SHARED:SetSharedPreview(key, path, onCreate?)  -> nil` | 从相对于模块的路径加载一段歌曲试听片段。 |
| `SHARED:SetSharedSFXUsingAbsolutePath(key, path, onCreate?)  -> nil` | 从绝对路径加载一个音效。 |
| `SHARED:SetSharedBGMUsingAbsolutePath(key, path, onCreate?)  -> nil` | 从绝对路径加载背景音乐。 |
| `SHARED:SetSharedVoiceUsingAbsolutePath(key, path, onCreate?)  -> nil` | 从绝对路径加载一段语音。 |
| `SHARED:SetSharedPreviewUsingAbsolutePath(key, path, onCreate?)  -> nil` | 从绝对路径加载一段歌曲试听片段。 |

```lua
-- 在启动舞台中
function onStart()
    SHARED:SetSharedSFX("Decide", "Sounds/Decide.ogg")
end

-- 在之后的任意模块中
SHARED:GetSharedSound("Decide"):Play()
```

### CONFIG

读取和更改游戏配置：玩家数量、模式、计分、按玩家的演奏 Mod 和音量。

<div class="callout warn">
属性使用点语法（`CONFIG.PlayerCount`），方法使用冒号语法。玩家索引从 0 起（0 到 4）；setter 忽略超出范围的索引，getter 对其返回默认值。在只读模块（只读活动和背景）中，每个 setter 都会记录一条错误并且不做任何事。更改立即在内存中生效；游戏在正常关闭时写入 Config.ini。
</div>

| 方法 | 说明 |
| --- | --- |
| `CONFIG.ConfigIsNew  -> bool (read-only)` | 游戏在本次启动时新建了配置时为 true。 |
| `CONFIG.Language  -> string (read-only)` | 存储在 Config.ini 中的语言 id。 |
| `CONFIG.PlayerCount  -> int` | 活跃玩家数量。setter 会忽略 1..5 之外的值。 |
| `CONFIG.IsAIBattleMode  -> bool` | 是否启用 AI 对战模式。 |
| `CONFIG.AILevel  -> int` | AI 难度等级，写入时限制在 1..10。 |
| `CONFIG.IsTrainingMode  -> bool` | 是否启用练习模式。 |
| `CONFIG.UseModernScoringMethod  -> bool` | 游戏是否使用现代（真打）计分方式。 |
| `CONFIG.UsedLegacyScoringMethod  -> int` | 旧版计分世代（见 `CONFIG.LEGACY_SCORING`），写入时限制在 0..3。 |
| `CONFIG.AreSongUnlockablesDisabled  -> bool (read-only)` | 游戏是否忽略歌曲解锁条件。 |
| `CONFIG.SongSpeed  -> int` | 以倍率的二十分之一为单位的歌曲速度：20 为 1.0x。写入时限制在 2..200（0.1x 到 10x）。 |
| `CONFIG.MasterVolume  -> int` | 主音量，写入时限制在 0..100。 |
| `CONFIG.SoundEffectVolume  -> int` | 音效音量，写入时限制在 0..100。 |
| `CONFIG.VoiceVolume  -> int` | 语音音量，写入时限制在 0..100。 |
| `CONFIG.SongVolume  -> int` | 歌曲播放音量，写入时限制在 0..100。 |
| `CONFIG.PreviewVolume  -> int` | 歌曲试听音量，写入时限制在 0..100。 |
| `CONFIG:GetGameType(player)  -> int` | 该玩家的游戏类型（见 `CONFIG.GAMETYPE`）；索引超出范围时为 Taiko。 |
| `CONFIG:SetGameType(player, gameType)  -> nil` | 设置该玩家的游戏类型，并忽略未定义的值。 |
| `CONFIG:GetDefaultCourse(player)  -> int` | 默认难度（见 `CONFIG.DEFAULT_COURSE`）；索引超出范围时为 Normal。尽管有 player 参数，这是一项适用于每个玩家的全局设置。 |
| `CONFIG:SetDefaultCourse(player, difficulty)  -> nil` | 设置全局默认难度，限制在 Easy 到 Extra Extreme 之后一级（Extra/Extra-Extra 的合并显示）之间。 |
| `CONFIG:GetScrollSpeed(player)  -> int` | 该玩家的流速值：9 为 1.0x，每级 0.1x（见 `CONFIG.SCROLLSPEED`）。索引超出范围时为 9。 |
| `CONFIG:SetScrollSpeed(player, speed)  -> nil` | 设置该玩家的流速值，限制在 0..99。 |
| `CONFIG:GetTimingZone(player)  -> int` | 该玩家的判定区间：0 Loose（宽松）、1 Lenient（较宽松）、2 Normal（普通）、3 Strict（严格）、4 Rigorous（极严）。索引超出范围时为 2。 |
| `CONFIG:SetTimingZone(player, zone)  -> nil` | 设置该玩家的判定区间，限制在 0..4。 |
| `CONFIG:GetAutoStatus(player)  -> bool` | 该玩家处于自动演奏或正在观看回放时为 true。 |
| `CONFIG:SetAutoStatus(player, isAuto)  -> nil` | 为该玩家启用或禁用自动演奏。 |
| `CONFIG:GetRandomMod(player)  -> int` | 该玩家的随机 Mod（见 `CONFIG.RANDOM`）；索引超出范围时为 Off。 |
| `CONFIG:SetRandomMod(player, mode)  -> nil` | 设置该玩家的随机 Mod，并忽略未定义的值。 |
| `CONFIG:GetFunMod(player)  -> int` | 该玩家的趣味 Mod（见 `CONFIG.FUN`）；索引超出范围时为 None。 |
| `CONFIG:SetFunMod(player, mod)  -> nil` | 设置该玩家的趣味 Mod，并忽略未定义的值。 |
| `CONFIG:GetStealthMod(player)  -> int` | 该玩家的隐身 Mod（见 `CONFIG.STEALTH`）；索引超出范围时为 Off。 |
| `CONFIG:SetStealthMod(player, mode)  -> nil` | 设置该玩家的隐身 Mod，并忽略未定义的值。 |
| `CONFIG:GetJusticeMod(player)  -> int` | 该玩家的判定 Mod：0 关闭，1 Just（Ok 计为 Bad），2 Safe（Bad 计为 Ok）。 |
| `CONFIG:SetJusticeMod(player, mode)  -> nil` | 设置该玩家的判定 Mod，限制在 0..2。 |
| `CONFIG:GetModFlags(player)  -> integer` | 把流速、隐身、随机、歌曲速度、判定区间、判定和趣味 Mod 打包成一个 64 位值（每项一个字节）。 |
| `CONFIG:SetModFlags(player, flags)  -> nil` | 把 GetModFlags 生成的值应用回该玩家（歌曲速度是全局的）。 |

CONFIG 上的常量表为上述整数值提供名称。`SONGSPEED` 和 `SCROLLSPEED` 还可以在存储值和倍率之间转换。

| 成员 | 说明 |
| --- | --- |
| `CONFIG.SONGSPEED.Normal  -> int` | 20，1.0x 的存储值。 |
| `CONFIG.SONGSPEED:ToActual(value)  -> number` | 把存储的歌曲速度转换为倍率（value / 20）。 |
| `CONFIG.SONGSPEED:FromActual(multiplier)  -> int` | 把倍率转换为最接近的存储歌曲速度。 |
| `CONFIG.SCROLLSPEED.Normal  -> int` | 9，1.0x 的存储值。 |
| `CONFIG.SCROLLSPEED:ToActual(value)  -> number` | 把存储的流速转换为倍率（(value + 1) / 10）。 |
| `CONFIG.SCROLLSPEED:FromActual(multiplier)  -> int` | 把倍率转换为最接近的存储流速。 |
| `CONFIG.GAMETYPE` | `Taiko`、`Konga`。 |
| `CONFIG.DEFAULT_COURSE` | `Easy`、`Normal`、`Hard`、`Oni`、`Edit`。 |
| `CONFIG.LEGACY_SCORING` | `Gen1Oni`、`Gen1_2`、`Gen2`、`Gen3`。 |
| `CONFIG.RANDOM` | `Off`、`Random`、`Mirror`、`SuperRandom`、`MirrorRandom`。 |
| `CONFIG.STEALTH` | `Off`、`Doron`、`Stealth`。 |
| `CONFIG.FUN` | `None`、`Avalanche`、`Minesweeper`、`DynamicBeat`、`Total`。 |
| `CONFIG.JUSTICE` | `None`、`Just`、`Safe`。 |

```lua
local multiplier = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
if CONFIG:GetRandomMod(0) == CONFIG.RANDOM.Mirror then
    -- 镜像谱面
end
```
