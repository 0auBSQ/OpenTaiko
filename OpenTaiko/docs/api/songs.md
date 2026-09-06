<!-- api/songs.md -->

# Songs and charts

Requesting the song list, walking nodes and charts, reading scores, and building dan (exam) courses.

Conventions used on this page:

- Difficulty indices are 0-based: 0 Easy, 1 Normal, 2 Hard, 3 Extreme (`Oni`), 4 Extra Extreme (`Edit`), 5 Tower, 6 Dan.
- Player and save-file indices are 0-based (0 is player 1).
- Members written with a dot (`node.Title`) are properties; members written with a colon (`node:GetChart(3)`) are methods.
- Some members return C# collections. A list has `.Count` and is indexed from 0 (`list[0]`); an array has `.Length` and is also indexed from 0. Each entry below says which one it returns.
- The song list is complete only after song enumeration finishes. Request it from the `afterSongEnum()` callback (see [Modules and lifecycle](activities.md)), or check the `IsSongsEnumDone()` global first; it returns true once enumeration has completed.

## Requesting the song list

### RequestSongList

Global function that builds a navigable song list from a settings object.

<div class="callout warn">
Available as a plain global function. Pass it a settings object created by GenerateSongListSettings(). The call builds the song tree once, from the songs the game has enumerated; the handle keeps the settings object by reference, so you can change a field and call the handle's ReloadSongList() to rebuild.
</div>

| Method | Description |
| --- | --- |
| `RequestSongList(settings)  -> song list handle` | Builds and returns a song list handle from the given song list settings. |

```lua
local settings = GenerateSongListSettings()
settings.AppendMainRandomBox = false
settings:SetExcludedGenreFolders({ "Dan", "Tower" })

local list = RequestSongList(settings)
local node = list:GetSelectedSongNode()
```

### GenerateSongListSettings

Global function that creates a song list settings object with default values.

| Method | Description |
| --- | --- |
| `GenerateSongListSettings()  -> song list settings` | Returns a new song list settings object with default field values. |

### Song list settings

Configuration object controlling which nodes a song list includes and how navigation behaves.

<div class="callout warn">
All members below are public fields that Lua reads and writes directly (settings.HideEmptyFolders = false), except the two setter methods, which take a Lua table. ExcludedGenreFolders and MandatoryDifficultyList are C# arrays; set them through their setter methods.
</div>

| Method | Description |
| --- | --- |
| `settings.AppendMainRandomBox  (bool, default true)` | When true, the list appends a random box to its root. |
| `settings.AppendSubRandomBoxes  (bool, default true)` | When true, the list appends a random box at the end of each folder. |
| `settings.SubBackBoxFrequency  (int, default 7)` | Inside each folder, the list inserts a back box at the start and after every N entries; 0 disables generated back boxes. |
| `settings.ExcludedGenreFolders  (string array)` | Genre folder names the list leaves out. Set it with SetExcludedGenreFolders. |
| `settings.RootGenreFolder  (string, default nil)` | When set, the list root becomes the first folder (depth-first) whose genre matches this name; when nil, the root is the top level. |
| `settings.RootGenreFolderNode  (song node, default nil)` | Node form of RootGenreFolder. When set, it takes precedence over the string, which disambiguates folders that share a genre name. |
| `settings.MandatoryDifficultyList  (Difficulty array, default nil)` | Difficulties a song must have to appear in the list; nil means no requirement. Set it with SetMandatoryDifficultyList. |
| `settings.MandatoryDifficultyMatchAll  (bool, default true)` | true requires every listed difficulty (AND); false requires at least one (OR). |
| `settings.HideEmptyFolders  (bool, default true)` | Hides folders that contain no visible song, recursively. |
| `settings.FlattenOpenedFolders  (bool, default true)` | When true, the current page is the whole tree with opened folders expanded in place (closed folders count as single entries). When false, the page holds only the siblings of the cursor node. |
| `settings.ModuloPagination  (bool, default true)` | When true, GetSongNodeAtOffset wraps around the page; when false it returns nil past either end. |
| `settings.ModuloMovement  (bool, default true)` | When true, Move wraps around the page; when false it clamps at either end. |
| `settings.ExcludeHiddenSongs  (bool, default true)` | Excludes songs whose HiddenIndex is 3 (hidden). |
| `settings.ExcludeLockedSongs  (bool, default false)` | When true, pages leave out locked songs, so navigation never lands on them. |
| `settings.IgnoreUnlockables  (bool, default false)` | When true, the list ignores ExcludeLockedSongs and GetRandomNodeInFolder may return locked songs. Node properties such as IsLocked keep reporting the real state. |
| `settings:SetExcludedGenreFolders(table)  -> void` | Sets ExcludedGenreFolders from a Lua table of genre-name strings. |
| `settings:SetMandatoryDifficultyList(table)  -> void` | Sets MandatoryDifficultyList from a Lua table of difficulty indices. |

### Song list handle

A navigable song tree returned by RequestSongList, with a cursor, folder navigation and search.

<div class="callout warn">
Search methods take a Lua function that receives a song node and returns a boolean. Methods that return several nodes return a C# list (.Count, 0-based indexing).
</div>

| Method | Description |
| --- | --- |
| `list:ReloadSongList()  -> void` | Rebuilds the whole tree from the current songs and settings and moves the cursor to the first node. |
| `list:GetRoot()  -> song node` | Returns the root node of the tree. |
| `list:GetSelectedSongNode()  -> song node` | Returns the node under the cursor, or nil when the list is empty. |
| `list:GetSongNodeAtOffset(offset)  -> song node` | Returns the node at the given offset from the cursor within the current page, wrapping or returning nil per ModuloPagination. |
| `list:Move(offset)  -> void` | Moves the cursor by the given offset within the current page, wrapping or clamping per ModuloMovement. |
| `list:OpenFolder()  -> bool` | Opens the folder under the cursor and moves the cursor to its first child; returns false if the cursor is not on a closed, non-empty folder. |
| `list:CloseFolder()  -> bool` | Closes the folder containing the cursor and moves the cursor to that folder; returns false if there is nothing to close. Leaving a virtual folder restores the cursor saved by OpenVirtualFolder. |
| `list:OpenVirtualFolder(baseFolder, songs, title)  -> bool` | Opens a temporary folder named `title` containing the song nodes from the Lua table `songs` (keys 1..n), with generated back boxes and a trailing random box, and moves the cursor into it. `baseFolder` becomes the virtual folder's parent. Returns false if the table holds no song nodes. |
| `list:GetSongByUniqueId(id)  -> song node` | Returns the first song whose unique id matches, or nil. |
| `list:GetRandomNodeInFolder(node, recursive, predicate)  -> song node` | Picks a random song among the siblings of `node` (the page containing it). With `recursive` (default true), the pick also covers songs inside sibling folders. It skips locked songs unless IgnoreUnlockables is set. `predicate` is optional. Returns nil when nothing qualifies. |
| `list:SearchSongsByPredicate(predicate)  -> list of song nodes` | Returns every song node in the tree for which the predicate returns true. |
| `list:SearchFirstSongByPredicate(predicate)  -> song node` | Returns the first song node for which the predicate returns true, or nil. |
| `list:SearchNodesByPredicate(predicate)  -> list of song nodes` | Like SearchSongsByPredicate, but also tests folders and other non-song nodes. |

```lua
local results = list:SearchSongsByPredicate(function(node)
    return node:GetChart(3) ~= nil   -- has an Extreme chart
end)
for i = 0, results.Count - 1 do
    local node = results[i]
end
```

## Nodes, charts and scores

### Song node

A single entry in the song list: a song, a folder, a back box or a random box.

<div class="callout warn">
The song list handle, SONGMOUNT:ChosenSongNode() and DANBUILDER:GetSong() return song nodes. Properties are read-only. Metadata properties return nil on nodes that are not songs. Navigate the list through the song list handle (Move, OpenFolder, CloseFolder and the search methods).
</div>

| Method | Description |
| --- | --- |
| `node.NotNull  (bool)` | True when the node wraps a real song list entry. |
| `node.IsFolder  (bool)` | True when the node is a folder. |
| `node.IsRandom  (bool)` | True when the node is a random box. |
| `node.IsReturn  (bool)` | True when the node is a back box. |
| `node.IsSong  (bool)` | True when the node is a playable song. |
| `node.SongCount  (int)` | Number of direct child songs. |
| `node.RecursiveSongCount  (int)` | Number of songs under this node, including subfolders. |
| `node.VisibleSongCount  (int)` | Number of direct child songs whose HiddenIndex is not 3. |
| `node.RecursiveVisibleSongCount  (int)` | Number of visible songs under this node, including subfolders. |
| `node.BoxType  (string)` | The folder box style string, or nil. |
| `node.BgType  (string)` | The background style string, or nil. |
| `node.BoxChara  (string)` | The box character string, or nil. |
| `node.ForeColor  (color)` | The node's foreground color, or nil. |
| `node.BackColor  (color)` | The node's background color, or nil. |
| `node.BoxColor  (color)` | The node's box color, or nil. |
| `node.Title  (string)` | The display title. Back and random boxes return the localized "Return" / "Random" text built from the parent folder's title. |
| `node.Subtitle  (string)` | The song's subtitle, or nil. |
| `node.Genre  (string)` | The genre string, or nil. |
| `node.UniqueId  (string)` | The song's unique id, or nil. |
| `node.Maker  (string)` | The MAKER field as one string, or nil. |
| `node.Charters  (string array)` | The MAKER field split on commas. |
| `node.Side  (int)` | The SIDE value: 0 normal, 1 ex, 2 both. |
| `node.Explicit  (bool)` | True when the song is flagged explicit; nil for non-songs. |
| `node.HasVideo  (bool)` | True when the song has a background movie; nil for non-songs. |
| `node.DemoStart  (int)` | The preview BGM offset in milliseconds. |
| `node.AudioPath  (string)` | The absolute path of the song's BGM file, or an empty string. |
| `node.HasPreimage  (bool)` | True when the song declares a preimage. |
| `node.PreimagePath  (string)` | The absolute path of the preimage. Check HasPreimage first; without a preimage this is only the song folder. |
| `node:GetPreimage()  -> texture` | Loads the preimage from disk and returns a new texture, or nil if the song has none. Dispose the texture when you are done with it. |
| `node.ChartMd5  (string)` | MD5 of the chart file (uppercase hex), or an empty string. It stays the same across installs; UniqueId does not. |
| `node:GetChart(diff)  -> chart` | Returns the chart for the given difficulty index, or nil if the song has no such chart. |
| `node:GetCustomCommand(key)  -> string` | Returns the value of a global-scope custom command (a dot-prefixed header placed before the first COURSE, key includes the dot, for example ".VAULT_NAME"), or nil. |
| `node:GetCustomCommands()  -> dictionary` | Returns all global-scope custom commands as a C# dictionary object. Prefer GetCustomCommand for lookups. |
| `node.UnlockCondition  (unlock condition)` | The unlock condition object (see Unlock condition). |
| `node.UnlockText  (string)` | The custom unlock text if the song defines one, otherwise the generated condition message. |
| `node.IsLocked  (bool)` | True when this song is currently locked; always false for non-songs. |
| `node.HiddenIndex  (int)` | The unlock-system display state: 0 displayed, 1 grayed, 2 blurred, 3 hidden (0 for non-songs). |
| `node.Rarity  (string)` | The rarity label; "Common" for songs without an unlock entry, "-" for non-songs. |
| `node:Mount(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | Selects this song for play with the given difficulty index per player (defaults 0). It checks only the first CONFIG.PlayerCount indices and returns false if the node is not a song or an active player's difficulty is missing or out of range. |
| `node:MountIfNotLocked(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | Same as Mount, but returns false without mounting when the song is locked. |

### Chart

One difficulty of a song: level, BPM, charters, Tower and Dan data, best scores and custom commands.

<div class="callout warn">
A song node's GetChart(diff) returns a chart. Properties are read-only. BPM, Life, TotalFloorCount, TowerType and DanTick return nil when the chart has no chart info. Difficulty and LevelIcon are enum objects; compare them through DifficultyAsInt, IsPlus and IsMinus.
</div>

| Method | Description |
| --- | --- |
| `chart.NotNull  (bool)` | True when the chart wraps real chart data. |
| `chart.Parent  (song node)` | The song node this chart belongs to. |
| `chart.Difficulty  (enum)` | The difficulty as an enum object. |
| `chart.DifficultyAsInt  (int)` | The difficulty index. |
| `chart.Level  (int)` | The star level. |
| `chart.LevelDecimal  (number)` | The level with its fractional part (for example 12.888), or the integer level when none was authored. |
| `chart.LevelFirstDecimal  (int)` | The first decimal digit of LevelDecimal (0-9). |
| `chart.LevelIcon  (enum)` | The level icon as an enum object. |
| `chart.IsPlus  (bool)` | True when the level icon is "plus". |
| `chart.IsMinus  (bool)` | True when the level icon is "minus". |
| `chart.NotesDesigner  (string)` | The NOTESDESIGNER field as one string. |
| `chart.Charters  (string array)` | The NOTESDESIGNER field split on commas. |
| `chart.BPM  (number)` | The main BPM, or nil. |
| `chart.BaseBPM  (number)` | The base BPM, or nil. |
| `chart.MinBPM  (number)` | The minimum BPM, or nil. |
| `chart.MaxBPM  (number)` | The maximum BPM, or nil. |
| `chart.Life  (int)` | Tower life count, or nil. |
| `chart.TotalFloorCount  (int)` | Tower floor count, or nil. |
| `chart.TowerType  (string)` | Tower type string, or nil. |
| `chart.DanTick  (int)` | Dan plate tick value, or nil. |
| `chart.DanTickColor  (color)` | Dan plate tick color (white when the chart has no chart info). |
| `chart.DanSongs  (array of dan songs)` | The songs that make up this Dan chart. |
| `chart.DanExams  (array of dan exams)` | The global exam conditions of this Dan chart. |
| `chart:GetSongExam(songIdx, examSlot)  -> dan exam` | Returns the per-song exam for the 1-based song index and 1-based exam slot; the result has IsSet = false when none exists. |
| `chart:GetPlayerBestScore(save)  -> best score info` | Returns the best-play summary of the given save file for this chart. |
| `chart:GetCustomCommand(key)  -> string` | Returns the value of a chart-scope custom command (a dot-prefixed header inside this COURSE block, key includes the dot), or nil. |
| `chart:GetCustomCommands()  -> dictionary` | Returns all chart-scope custom commands as a C# dictionary object. |
| `chart.SongFolder  (string)` | The absolute folder holding the chart file. |
| `chart.ChartPath  (string)` | The absolute path of the chart file. |
| `chart.UniqueId  (string)` | The song's unique id, or an empty string. |
| `chart:Select(player)  -> bool` | Marks this chart as the chosen difficulty for the given player; player 0 also sets the chosen song. Returns false when the chart is not valid. |

### Best score info

A save file's best-play summary for one chart.

<div class="callout warn">
chart:GetPlayerBestScore(save) returns this object. All members are read-only. An invalid save index yields an empty record.
</div>

| Method | Description |
| --- | --- |
| `info.ScoreRank  (int)` | The best score rank reached. |
| `info.ClearStatus  (int)` | The best clear status reached. |
| `info.HighScore  (int)` | The high score. |
| `info.HasBeenPlayed  (bool)` | True when the chart has at least one recorded play, whatever the result. |
| `info.PlayCount  (int)` | Total number of plays on this chart, all mod variants combined. |

## Dan exams

### Dan song

One song entry inside a Dan course.

<div class="callout warn">
Elements of chart.DanSongs. All members are read-only.
</div>

| Method | Description |
| --- | --- |
| `dansong.Title  (string)` | The song's title. |
| `dansong.SubTitle  (string)` | The song's subtitle. |
| `dansong.Genre  (string)` | The song's genre. |
| `dansong.Level  (int)` | The song's star level. |
| `dansong.Difficulty  (enum)` | The difficulty as an enum object. |
| `dansong.DifficultyAsInt  (int)` | The difficulty index. |

### Dan exam

One pass/fail condition of a Dan course.

<div class="callout warn">
Elements of chart.DanExams, or returned by chart:GetSongExam(). All members are read-only. TypeAsInt values: 0 gauge, 1 perfect judges, 2 good judges, 3 bad judges, 4 score, 5 rolls, 6 hits, 7 combo, 8 accuracy, 9 ad-lib judges, 10 mine judges. RangeAsInt values: 0 "at least", 1 "less than".
</div>

| Method | Description |
| --- | --- |
| `danexam.IsSet  (bool)` | True when this exam slot is enabled. |
| `danexam.RedValue  (int)` | The red (pass) threshold. |
| `danexam.GoldValue  (int)` | The gold threshold. |
| `danexam.TypeAsInt  (int)` | The exam type. |
| `danexam.RangeAsInt  (int)` | The comparison direction. |

### DANBUILDER

Global for assembling a Dan course in memory from song nodes, difficulties and exam conditions, then mounting it for play.

<div class="callout warn">
Available as the global DANBUILDER. Song and slot indices are 1-based, except the difficulty passed to AddSong, which is a 0-based difficulty index. Exam slots run from 1 to 7. Exam type strings (case-insensitive, short form in parentheses): "judgeperfect" (jp), "judgegood" (jg), "judgebad" (jb), "score" (s), "roll" (r), "hit" (h), "combo" (c), "accuracy" (a), "judgeadlib" (ja), "judgemine" (jm); any other string means gauge. lessThan = true makes the exam a "less than" check, false an "at least" check. The builder keeps its state between calls; call Clear() before building a new course.
</div>

| Method | Description |
| --- | --- |
| `DANBUILDER.SongCount  (int)` | Number of songs added so far. |
| `DANBUILDER:AddSong(node, diff)  -> void` | Appends a song node at the given 0-based difficulty index. |
| `DANBUILDER:GetSong(i)  -> song node` | Returns the song node at 1-based index i, or nil. |
| `DANBUILDER:GetSongDiff(i)  -> int` | Returns the difficulty index stored for the song at 1-based index i, or -1. |
| `DANBUILDER:SetTitle(title)  -> void` | Sets the course title (default "Dynamic Dan"). |
| `DANBUILDER:SetSubtitle(subtitle)  -> void` | Sets the course subtitle. |
| `DANBUILDER:SetDanTick(tick)  -> void` | Sets the Dan plate tick value (default 2). |
| `DANBUILDER:SetDanTickColor(r, g, b)  -> void` | Sets the Dan plate tick color from 0-255 components (default white). |
| `DANBUILDER:SetGlobalExam(slot, type, red, gold, lessThan)  -> void` | Sets a course-wide exam in the given slot. |
| `DANBUILDER:SetPerSongExam(songIndex, slot, type, red, gold, lessThan)  -> void` | Sets an exam that applies to one song, by 1-based song index and slot. |
| `DANBUILDER:Clear()  -> void` | Removes all songs and exams and resets the metadata to defaults. |
| `DANBUILDER:Mount()  -> bool` | Builds the course chart in memory and selects it for play at the Dan difficulty for player 1; returns false if the builder holds no songs or the build failed. |

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

## Unlocks, virtual slots and mod icons

### Unlock condition

Describes what unlocks a song and whether a player currently meets the condition.

<div class="callout warn">
node.UnlockCondition returns this object. HasCondition is a property; the rest are methods. Songs without an unlock entry report HasCondition = false and IsUnlockable = true.
</div>

| Method | Description |
| --- | --- |
| `cond.HasCondition  (bool)` | True when the song has an explicit unlock condition. |
| `cond:GetConditionMessage()  -> string` | Returns the human-readable description of the condition, or an empty string. |
| `cond:GetConditionType()  -> string` | Returns the condition type id (for example "ch", "cs", "gt", "gc", "ig"), or an empty string. |
| `cond:GetCoinPrice()  -> int` | Returns the coin cost, or 0. |
| `cond:IsUnlockable(player)  -> bool` | Returns true when the given player meets the condition. |
| `cond:GetBlockedMessage(player)  -> string` | Returns why the player does not meet the condition, or an empty string when they do. |

### VIRTUALSLOTS

Global for reading and writing the five virtual character slots (V1-V5) and for redirecting a player spot to display a slot's visuals.

<div class="callout warn">
Available as the global VIRTUALSLOTS. Slot indices run from 1 to 5; setters ignore out-of-range indices and getters return the defaults for them. These methods write nothing to disk. The engine manages the AI slot, which this global cannot edit.
</div>

| Method | Description |
| --- | --- |
| `VIRTUALSLOTS:GetCharacter(slot)  -> string` | Returns the character folder name of the slot, or "None". |
| `VIRTUALSLOTS:SetCharacter(slot, folderName)  -> void` | Sets the character folder name of the slot. |
| `VIRTUALSLOTS:GetPuchichara(slot)  -> string` | Returns the puchichara folder name of the slot, or "None". |
| `VIRTUALSLOTS:SetPuchichara(slot, folderName)  -> void` | Sets the puchichara folder name of the slot. |
| `VIRTUALSLOTS:GetNameplateName(slot)  -> string` | Returns the nameplate player name of the slot, or "VSlot". |
| `VIRTUALSLOTS:SetNameplateName(slot, name)  -> void` | Sets the nameplate player name of the slot. |
| `VIRTUALSLOTS:GetNameplateTitle(slot)  -> string` | Returns the nameplate title text of the slot. |
| `VIRTUALSLOTS:SetNameplateTitle(slot, title)  -> void` | Sets the nameplate title text of the slot. |
| `VIRTUALSLOTS:GetNameplateDan(slot)  -> string` | Returns the nameplate dan text of the slot. |
| `VIRTUALSLOTS:SetNameplateDan(slot, dan)  -> void` | Sets the nameplate dan text of the slot. |
| `VIRTUALSLOTS:SetNameplateById(slot, nameplateId)  -> void` | Applies a nameplate from the nameplate database by id: sets the title text, type and rarity. An unknown id only records the id. |
| `VIRTUALSLOTS:SetNameplateType(slot, type)  -> void` | Sets the nameplate title type (style index) directly. |
| `VIRTUALSLOTS:SetNameplateRarity(slot, rarity)  -> void` | Sets the nameplate title rarity index directly. |
| `VIRTUALSLOTS:SetNameplateDanType(slot, danType)  -> void` | Sets the dan plate type. |
| `VIRTUALSLOTS:SetNameplateDanGold(slot, gold)  -> void` | Sets whether the dan plate appears gold. |
| `VIRTUALSLOTS:MountSlot(playerSpot, slotInfo)  -> void` | Makes player spot 1-5 display the visuals of `slotInfo`: "1P"-"5P" (a player's save file), "AI", or "V1"-"V5". The override lasts until the next MountSlot call for that spot. |

### MODICONS

Global for drawing a player's active mod icons at a screen position.

<div class="callout warn">
Available as the global MODICONS. The modicons ROActivity does the drawing; the first Draw call activates it.
</div>

| Method | Description |
| --- | --- |
| `MODICONS:Draw(player, x, y, alpha)  -> void` | Draws the mod icons of the given player at (x, y) using the menu layout; alpha is optional (default 255). |

## Replays and the selected song

### REPLAY

Global for listing a chart's saved replays and starting playback of one.

<div class="callout warn">
Available as the global REPLAY. ListReplays returns a C# array of replay headers (.Length, 0-based). Watch loads a replay and arms playback for the next play only; the game applies the replay's mods in memory and restores the previous mods afterwards. songFolder and chartPath come from a chart's SongFolder and ChartPath.
</div>

| Method | Description |
| --- | --- |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN, chartPath)  -> array of replay headers` | Returns up to topN replays of the chart and difficulty, ranked by score. chartPath lets the listing compute ChecksumMismatch. |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN)  -> array of replay headers` | Same without a chart path (the listing skips ChecksumMismatch). |
| `REPLAY:ListReplaysAsync(songFolder, uniqueId, difficulty, topN, chartPath)  -> replay list handle` | Runs the same listing on a background thread and returns a handle to poll. |
| `REPLAY:Watch(filepath, chartPath)  -> bool` | Loads the replay file and arms playback for the next play; returns false when the file fails to load or the replay is not watchable. chartPath enables the in-game "invalid replay" warnings. |
| `REPLAY:Watch(filepath)  -> bool` | Same without a chart path. |
| `REPLAY.MODFLAG  (object)` | Bit values for ModFlags: None (0), Mirror (1), Random (2), SuperRandom (4), Invisible (8), PerfectMemory (16), Avalanche (32), Minesweeper (64), Just (128), Safe (256), DynamicBeat (512). |

```lua
local flags = header.ModFlags
local mirrored = (flags & REPLAY.MODFLAG.Mirror) ~= 0
```

### Replay list handle

Handle returned by REPLAY:ListReplaysAsync.

<div class="callout warn">
Poll IsDone each frame; once it is true, read Result.
</div>

| Method | Description |
| --- | --- |
| `handle.IsDone  (bool)` | True once the background listing has finished. |
| `handle.Result  (array of replay headers)` | The listed replays (empty until IsDone is true). |

### Replay header

Metadata of one saved replay.

<div class="callout warn">
Elements of the array returned by REPLAY:ListReplays or a replay list handle's Result. All members are read-only.
</div>

| Method | Description |
| --- | --- |
| `rep.FilePath  (string)` | The absolute path of the replay file; pass it to REPLAY:Watch. |
| `rep.PlayerName  (string)` | The name of the player who recorded the replay. |
| `rep.Score  (int)` | The final score. |
| `rep.ClearStatus  (int)` | The clear status of the play. |
| `rep.ScoreRank  (int)` | The score rank of the play. |
| `rep.Good  (int)` | Good (perfect) judge count. |
| `rep.Ok  (int)` | Ok judge count. |
| `rep.Bad  (int)` | Bad (miss) judge count. |
| `rep.Roll  (int)` | Roll hit count. |
| `rep.MaxCombo  (int)` | Maximum combo. |
| `rep.Boom  (int)` | Mine hit count. |
| `rep.ADLib  (int)` | Ad-lib hit count. |
| `rep.ModFlags  (int)` | Bitmask of the mods used (see REPLAY.MODFLAG). |
| `rep.ScrollSpeed  (int)` | The scroll speed setting of the play. |
| `rep.SongSpeed  (int)` | The song speed setting of the play. |
| `rep.JudgeStrictness  (int)` | The timing zone setting of the play. |
| `rep.Date  (string)` | The play date formatted as "yyyy-MM-dd HH:mm". |
| `rep.Timestamp  (int)` | The play date as raw ticks. |
| `rep.ChartUniqueID  (string)` | The unique id of the chart. |
| `rep.ChartDifficulty  (int)` | The difficulty index of the recorded play. |
| `rep.ChartChecksum  (string)` | The chart MD5 stored with the replay. |
| `rep.RandomSeed  (int)` | The note-shuffle seed, or -1 when the file stores none. |
| `rep.GameMode  (int)` | The game mode of the recorded play. |
| `rep.GameVersion  (int)` | The game version that recorded the replay. |
| `rep.Watchable  (bool)` | True when the game can play the replay back faithfully. |
| `rep.UnwatchableReason  (string)` | Why the replay is not watchable, when Watchable is false. |
| `rep.OldVersion  (bool)` | True when an older game version recorded the replay. |
| `rep.ChecksumMismatch  (bool)` | True when the chart file no longer matches the recording (computed only when you pass a chart path). |

### SONGMOUNT

Read-only global for the song currently selected for play.

<div class="callout warn">
Available as the global SONGMOUNT. It reflects the state set by a song node's Mount(), a chart's Select() or DANBUILDER:Mount().
</div>

| Method | Description |
| --- | --- |
| `SONGMOUNT:ChosenUniqueId()  -> string` | Returns the unique id of the selected song, or an empty string. |
| `SONGMOUNT:ChosenDifficulty()  -> int` | Returns the difficulty index selected for player 1. |
| `SONGMOUNT:ChosenSongNode()  -> song node` | Returns the selected song as a song node (without children), or nil when nothing is selected. |
