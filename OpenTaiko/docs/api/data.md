<!-- api/data.md -->

# Data and persistence

Saving data that survives a restart, querying SQLite files, reading files, JSON and INI from the module folder, sharing resources between modules, and reading or changing the game configuration.

Relative paths passed to DATABASE, SQL, STORAGE, JSONLOADER, INILOADER and SHARED resolve against the directory of the running module (the folder that holds its `Script.lua`).

Some methods on this page return .NET collections, which behave differently from Lua tables:

- Arrays (`string[]`, `int[]`, `double[]`) start at index 0 and expose `.Length`.
- Dictionaries (parsed JSON, SQL rows, language maps) take `d["key"]` (or `d[1]` for arrays parsed from JSON) and enumerate through `d:GetEnumerator()`; `pairs` and `#` do not work on them.

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

## Key-value database

### DATABASE

Opens LMDB key/value stores that persist string values across restarts, either inside the module folder or in the game-wide data folder.

<div class="callout warn">
Every Read and Write opens and closes its own LMDB environment, so each call is expensive; cache values in Lua and refresh the cache when you write. Inside read-only modules (ROActivities and backgrounds) DATABASE returns stores whose Write logs an error and does nothing.
</div>

| Method | Description |
| --- | --- |
| `DATABASE:OpenLocalDatabase(path)  -> database` | Opens (creating if needed) a store at a path relative to the module directory. |
| `DATABASE:OpenGlobalDatabase(path)  -> database` | Opens (creating if needed) a store under `Global/ApplicationData/LMDB/` in the game folder, shared by every module. |

### Database handle

A key/value store returned by DATABASE, mapping string keys to string values.

<div class="callout warn">
Values are strings only; convert numbers and booleans yourself (for example with tostring and tonumber).
</div>

| Method | Description |
| --- | --- |
| `database:Write(key, value)  -> nil` | Stores a string under the key and commits it. Logs an error and does nothing in read-only modules. |
| `database:Read(key)  -> string` | Returns the string stored under the key, or nil if the key is missing or the read fails. |
| `database:Dispose()  -> nil` | Does nothing; the handle holds no open resources. |

```lua
local db

function activate()
    db = DATABASE:OpenGlobalDatabase("GameStatus")
    if db:Read("new_user") == nil then
        db:Write("new_user", "true")
    end
end
```

## SQL database

### SQL

Opens a SQLite database file inside the module directory for running SQL statements.

| Method | Description |
| --- | --- |
| `SQL:OpenSQLDatabase(path)  -> sql` | Opens a SQLite database at a path relative to the module directory. |

### SQL handle

A SQLite connection returned by SQL:OpenSQLDatabase.

<div class="callout warn">
Query returns a dictionary keyed 1..n; each row is a dictionary keyed by column name (see the note on .NET collections at the top of the page). A failed statement logs the error and returns an empty result. Query passes the statement text to SQLite unchanged, with no parameter binding, so escape any value you splice into it.
</div>

| Method | Description |
| --- | --- |
| `sql:Query(query)  -> rows` | Executes the SQL text and returns the result rows. |

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

## Files, JSON and INI

### STORAGE

File access rooted at the module directory, plus helpers for sharing online lobby codes.

<div class="callout warn">
WriteText only writes inside the module folder: it rejects absolute paths and paths that resolve outside it. ReadText also accepts an absolute path. Lobby-code helpers use the shared `Global/Lobbycodes/` folder next to the executable. Read-only modules can use every STORAGE method.
</div>

| Method | Description |
| --- | --- |
| `STORAGE:GetFiles(dir, pattern)  -> string[]` | Lists the files in a subdirectory of the module folder matching a search pattern such as `"*.png"`. Entries are paths relative to the module folder, including the subdirectory. The directory must exist. |
| `STORAGE:FileExists(path)  -> bool` | True if a file exists at the path relative to the module folder. |
| `STORAGE:DirectoryExists(path)  -> bool` | True if a directory exists at the path relative to the module folder. |
| `STORAGE:WriteText(name, contents)  -> bool` | Writes text to a file under the module folder, creating subdirectories. Returns false for absolute or escaping paths, or on failure. |
| `STORAGE:ReadText(name)  -> string` | Returns the raw text of a file (relative to the module folder, or absolute), or nil if it is missing or unreadable. |
| `STORAGE:GetFullPath(name)  -> string` | Returns the absolute path of a file under the module folder, or nil for empty or absolute input. |
| `STORAGE:WriteLobbyCode(name, contents)  -> bool` | Writes a file into `Global/Lobbycodes/`. Returns false for empty, absolute or `..` names, or on failure. |
| `STORAGE:RevealLobbyCodes()  -> bool` | Opens the OS file browser at `Global/Lobbycodes/`. |
| `STORAGE:RevealInExplorer(name)  -> bool` | Opens the OS file browser with the named module file selected (Windows and macOS), or its folder elsewhere. |

### JSONLOADER

Parses JSON files and strings from the module directory.

<div class="callout warn">
Parsed values are .NET dictionaries: objects are keyed by member name, arrays are keyed 1..n. Indexing a missing key directly (`d["x"]`) raises an error, so use JsonGet for lookups that may fail. Numbers become integers or doubles; strings, booleans and null map to their Lua equivalents. LoadJson returns a JsonNode tree; index it with `node["member"]` and convert leaves with ExtractNumber / ExtractText.
</div>

| Method | Description |
| --- | --- |
| `JSONLOADER:LoadJson(name)  -> JsonNode` | Parses a JSON file relative to the module directory into a JsonNode tree. Raises an error if the file is missing. |
| `JSONLOADER:ExtractNumber(value)  -> number` | Converts a JsonNode leaf to a number; returns 0 for nil or non-numeric values. |
| `JSONLOADER:ExtractText(value)  -> string` | Converts a JsonNode leaf to a string; returns nil for nil. |
| `JSONLOADER:JsonParseFile(name)  -> dict` | Parses a JSON file whose root is an object (empty dictionary if the file is blank). Raises an error if the file is missing or the root is not an object. |
| `JSONLOADER:JsonParseFileAny(name)  -> dict` | Parses a JSON file whose root is an object or an array (relative or absolute path). Returns nil if the file is missing or blank. |
| `JSONLOADER:JsonParseString(json)  -> dict` | Parses a JSON string whose root is an object (empty dictionary if blank). Raises an error if the root is not an object. |
| `JSONLOADER:JsonParseStringAny(json)  -> dict` | Parses a JSON string whose root is an object or an array. Returns nil for blank or invalid input. |
| `JSONLOADER:JsonGet(dict, key)  -> value` | Looks up a string key in an object or an integer key in an array; returns nil when missing or when dict is not a parsed value. |
| `JSONLOADER:JsonCount(dict)  -> int` | Number of members in a parsed object or array, or 0 for anything else. |

```lua
local data = JSONLOADER:JsonParseFileAny("Config/layout.json")
local colors = data and JSONLOADER:JsonGet(data, "colors")
local n = JSONLOADER:JsonCount(colors)
for i = 1, n do
    print(JSONLOADER:JsonGet(colors, i))
end
```

### INILOADER

Loads a flat `key=value` file from the module directory.

<div class="callout warn">
The loader splits each line on the first `=`, skips lines without one, and lets a repeated key overwrite the earlier value. It has no sections, comments or quoting. A missing file yields an empty handle.
</div>

| Method | Description |
| --- | --- |
| `INILOADER:LoadIni(name)  -> ini` | Reads a `key=value` file relative to the module directory. |

### INI handle

A parsed INI file returned by INILOADER:LoadIni with typed getters.

<div class="callout warn">
Getters return the supplied default when the key is missing. When the key exists but its value fails to parse, the numeric getters return 0. Array getters split on commas and return an empty array when the key is missing.
</div>

| Method | Description |
| --- | --- |
| `ini:GetBool(key, default)  -> bool` | True when the value parses as the integer 1; the default when the key is missing. |
| `ini:GetInt(key, default)  -> int` | The value as an integer. |
| `ini:GetDouble(key, default)  -> number` | The value as a double. |
| `ini:GetString(key, default)  -> string` | The raw string value. |
| `ini:GetStringArray(key)  -> string[]` | The value split on commas. |
| `ini:GetIntArray(key)  -> int[]` | The value split on commas, each part parsed as an integer (0 if unparseable). |
| `ini:GetDoubleArray(key)  -> double[]` | The value split on commas, each part parsed as a double (0 if unparseable). |

## Shared resources and configuration

### SHARED

A game-wide store of textures, sounds and strings that survives stage changes, so every module can use a resource loaded once (for example at boot).

<div class="callout warn">
Set* methods load on a background thread and swap the resource in on the render thread; the optional onCreate callback receives the new handle once it is in place. A newer Set* on the same key discards any load still in flight. Swapping disposes the previous resource, which invalidates any handle fetched before the reload; fetch it again with Get*. The UsingAbsolutePath variants take a full path. See Graphics and text for texture handles and Audio for sound handles.
</div>

| Method | Description |
| --- | --- |
| `SHARED:SetSharedString(key, value)  -> nil` | Stores a string under a key. |
| `SHARED:GetSharedString(key)  -> string` | Returns the string stored under a key, or an empty string. |
| `SHARED:GetSharedTexture(key)  -> texture` | Returns the shared texture for a key, or an empty texture if none was set. |
| `SHARED:GetSharedSound(key)  -> sound` | Returns the shared sound for a key, or an empty sound if none was set. |
| `SHARED:ClearSharedTexture(key)  -> nil` | Disposes the texture stored under a key and replaces it with an empty one. |
| `SHARED:ClearSharedSound(key)  -> nil` | Disposes the sound stored under a key and replaces it with an empty one. |
| `SHARED:SetSharedTexture(key, path, onCreate?)  -> nil` | Loads a texture from a module-relative path into the store. |
| `SHARED:SetSharedTexture(key, path, options, onCreate?)  -> nil` | Same, with an options table; `{ maxSize = N }` clamps the decoded texture's longer side to N pixels. |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, onCreate?)  -> nil` | Loads a texture from an absolute path. |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, options, onCreate?)  -> nil` | Loads a texture from an absolute path with an options table. |
| `SHARED:SetSharedSFX(key, path, onCreate?)  -> nil` | Loads a sound effect from a module-relative path. |
| `SHARED:SetSharedBGM(key, path, onCreate?)  -> nil` | Loads background music (song-playback volume group) from a module-relative path. |
| `SHARED:SetSharedVoice(key, path, onCreate?)  -> nil` | Loads a voice clip from a module-relative path. |
| `SHARED:SetSharedPreview(key, path, onCreate?)  -> nil` | Loads a song-preview clip from a module-relative path. |
| `SHARED:SetSharedSFXUsingAbsolutePath(key, path, onCreate?)  -> nil` | Loads a sound effect from an absolute path. |
| `SHARED:SetSharedBGMUsingAbsolutePath(key, path, onCreate?)  -> nil` | Loads background music from an absolute path. |
| `SHARED:SetSharedVoiceUsingAbsolutePath(key, path, onCreate?)  -> nil` | Loads a voice clip from an absolute path. |
| `SHARED:SetSharedPreviewUsingAbsolutePath(key, path, onCreate?)  -> nil` | Loads a song-preview clip from an absolute path. |

```lua
-- in the boot stage
function onStart()
    SHARED:SetSharedSFX("Decide", "Sounds/Decide.ogg")
end

-- in any later module
SHARED:GetSharedSound("Decide"):Play()
```

### CONFIG

Reads and changes the game configuration: player count, modes, scoring, per-player gameplay mods and volume levels.

<div class="callout warn">
Properties use dot syntax (`CONFIG.PlayerCount`), methods use colon syntax. Player indices are 0-based (0 to 4); setters ignore out-of-range indices and getters return a default for them. Inside read-only modules (ROActivities and backgrounds) every setter logs an error and does nothing. Changes apply immediately in memory; the game writes Config.ini when it closes normally.
</div>

| Method | Description |
| --- | --- |
| `CONFIG.ConfigIsNew  -> bool (read-only)` | True when the game created the configuration on this boot. |
| `CONFIG.Language  -> string (read-only)` | The language id stored in Config.ini. |
| `CONFIG.PlayerCount  -> int` | Number of active players. The setter ignores values outside 1..5. |
| `CONFIG.IsAIBattleMode  -> bool` | Whether AI battle mode is enabled. |
| `CONFIG.AILevel  -> int` | AI difficulty level, clamped to 1..10 on write. |
| `CONFIG.IsTrainingMode  -> bool` | Whether training mode is enabled. |
| `CONFIG.UseModernScoringMethod  -> bool` | Whether the game uses the modern (shin-uchi) scoring method. |
| `CONFIG.UsedLegacyScoringMethod  -> int` | Legacy scoring generation (see `CONFIG.LEGACY_SCORING`), clamped to 0..3 on write. |
| `CONFIG.AreSongUnlockablesDisabled  -> bool (read-only)` | Whether the game ignores song unlock conditions. |
| `CONFIG.SongSpeed  -> int` | Song speed in twentieths of the multiplier: 20 is 1.0x. Clamped to 2..200 (0.1x to 10x) on write. |
| `CONFIG.MasterVolume  -> int` | Master volume, clamped to 0..100 on write. |
| `CONFIG.SoundEffectVolume  -> int` | Sound-effect volume, clamped to 0..100 on write. |
| `CONFIG.VoiceVolume  -> int` | Voice volume, clamped to 0..100 on write. |
| `CONFIG.SongVolume  -> int` | Song-playback volume, clamped to 0..100 on write. |
| `CONFIG.PreviewVolume  -> int` | Song-preview volume, clamped to 0..100 on write. |
| `CONFIG:GetGameType(player)  -> int` | The player's game type (see `CONFIG.GAMETYPE`); Taiko for out-of-range indices. |
| `CONFIG:SetGameType(player, gameType)  -> nil` | Sets the player's game type and ignores undefined values. |
| `CONFIG:GetDefaultCourse(player)  -> int` | The default difficulty (see `CONFIG.DEFAULT_COURSE`); Normal for out-of-range indices. One global setting applies to every player despite the player argument. |
| `CONFIG:SetDefaultCourse(player, difficulty)  -> nil` | Sets the global default difficulty, clamped from Easy to one past Extra Extreme (the combined Extreme / Extra Extreme display). |
| `CONFIG:GetScrollSpeed(player)  -> int` | The player's scroll speed value: 9 is 1.0x, each step is 0.1x (see `CONFIG.SCROLLSPEED`). 9 for out-of-range indices. |
| `CONFIG:SetScrollSpeed(player, speed)  -> nil` | Sets the player's scroll speed value, clamped to 0..99. |
| `CONFIG:GetTimingZone(player)  -> int` | The player's judgement window: 0 Loose, 1 Lenient, 2 Normal, 3 Strict, 4 Rigorous. 2 for out-of-range indices. |
| `CONFIG:SetTimingZone(player, zone)  -> nil` | Sets the player's judgement window, clamped to 0..4. |
| `CONFIG:GetAutoStatus(player)  -> bool` | True when the player is on auto-play or watching a replay. |
| `CONFIG:SetAutoStatus(player, isAuto)  -> nil` | Enables or disables auto-play for the player. |
| `CONFIG:GetRandomMod(player)  -> int` | The player's random mod (see `CONFIG.RANDOM`); Off for out-of-range indices. |
| `CONFIG:SetRandomMod(player, mode)  -> nil` | Sets the player's random mod and ignores undefined values. |
| `CONFIG:GetFunMod(player)  -> int` | The player's fun mod (see `CONFIG.FUN`); None for out-of-range indices. |
| `CONFIG:SetFunMod(player, mod)  -> nil` | Sets the player's fun mod and ignores undefined values. |
| `CONFIG:GetStealthMod(player)  -> int` | The player's stealth mod (see `CONFIG.STEALTH`); Off for out-of-range indices. |
| `CONFIG:SetStealthMod(player, mode)  -> nil` | Sets the player's stealth mod and ignores undefined values. |
| `CONFIG:GetJusticeMod(player)  -> int` | The player's judgement mod: 0 off, 1 Just (Ok counts as Bad), 2 Safe (Bad counts as Ok). |
| `CONFIG:SetJusticeMod(player, mode)  -> nil` | Sets the player's judgement mod, clamped to 0..2. |
| `CONFIG:GetModFlags(player)  -> integer` | Packs scroll speed, stealth, random, song speed, timing zone, judgement and fun mods into one 64-bit value (one byte each). |
| `CONFIG:SetModFlags(player, flags)  -> nil` | Applies a value produced by GetModFlags back to the player (song speed is global). |

Constant tables on CONFIG give names to the integer values above. `SONGSPEED` and `SCROLLSPEED` also convert between stored values and multipliers.

| Member | Description |
| --- | --- |
| `CONFIG.SONGSPEED.Normal  -> int` | 20, the stored value for 1.0x. |
| `CONFIG.SONGSPEED:ToActual(value)  -> number` | Converts a stored song speed to its multiplier (value / 20). |
| `CONFIG.SONGSPEED:FromActual(multiplier)  -> int` | Converts a multiplier to the nearest stored song speed. |
| `CONFIG.SCROLLSPEED.Normal  -> int` | 9, the stored value for 1.0x. |
| `CONFIG.SCROLLSPEED:ToActual(value)  -> number` | Converts a stored scroll speed to its multiplier ((value + 1) / 10). |
| `CONFIG.SCROLLSPEED:FromActual(multiplier)  -> int` | Converts a multiplier to the nearest stored scroll speed. |
| `CONFIG.GAMETYPE` | `Taiko`, `Konga`. |
| `CONFIG.DEFAULT_COURSE` | `Easy`, `Normal`, `Hard`, `Oni`, `Edit`. |
| `CONFIG.LEGACY_SCORING` | `Gen1Oni`, `Gen1_2`, `Gen2`, `Gen3`. |
| `CONFIG.RANDOM` | `Off`, `Random`, `Mirror`, `SuperRandom`, `MirrorRandom`. |
| `CONFIG.STEALTH` | `Off`, `Doron`, `Stealth`. |
| `CONFIG.FUN` | `None`, `Avalanche`, `Minesweeper`, `DynamicBeat`, `Total`. |
| `CONFIG.JUSTICE` | `None`, `Just`, `Safe`. |

```lua
local multiplier = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
if CONFIG:GetRandomMod(0) == CONFIG.RANDOM.Mirror then
    -- mirrored chart
end
```
