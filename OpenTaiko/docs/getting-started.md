<!-- getting-started.md -->

# How modules work

OpenTaiko 0.6.1 lets a skin add and replace screens with Lua. A skin's Modules folder holds one folder per module, and each module has a Script.lua that defines a fixed set of global callback functions. The game loads every Script.lua into its own sandboxed Lua state, registers the engine globals (TEXTURE, SOUND, INPUT, CONFIG and the others documented in the [API reference](api/README.md)), and calls the callbacks at the right time. [Modules and lifecycle](api/activities.md) lists the exact signatures.

## Before you start

- OpenTaiko 0.6.1 and a skin folder with a Modules directory. The bundled skin is System/Open-World Memories.
- A text editor and basic Lua (functions, tables, require).
- The bundled stages under System/Open-World Memories/Modules/Stages. The small ones, demo1 and demo3, show the callback shape; the larger ones show how real screens are organized.

## Where modules live

Each module kind has its own folder under Modules, and each module is one folder whose name is the module's id:

```
Modules/
  Stages/        <name>/Script.lua   full screens
  Activities/    <name>/Script.lua   sub-screens driven by a stage
  ROActivities/  <name>/Script.lua   read-only sub-screens and overlays
  Transitions/   <name>/Script.lua   fades between stages (loaded first)
  Lib/           shared .lua files reachable through require; not scanned as modules
```

The entry file is always Script.lua. The asset paths you pass to TEXTURE, SOUND, VIDEO and the other loaders are relative to the module folder; the bundled modules keep them in Textures, Sounds, Videos and Databases subfolders by convention, and put translations in a lang folder.

Two kinds of script live elsewhere:

- Backgrounds (screen backgrounds, gameplay layers, mobs, clear animations, the kusudama) are Script.lua files under the skin's Graphics folder, in the directory of the screen they decorate. See the Backgrounds section of [Modules and lifecycle](api/activities.md).
- Characters are folders under Global/Characters. A character folder may carry its own Script.lua; without one the game uses its built-in character script. See [Adding characters](guides/characters.md).

## Script.lua defines global functions

A module's Script.lua defines top-level global functions with fixed names, and the game reads each one as a global. The game never finds a function you wrap in a local table and return, and it never calls a misspelled name (OnStart in place of onStart), because it treats an undefined callback as a no-op and reports nothing. Everything else in the file can be local, and you can split the module across files loaded with require.

demo3 is the minimal shape to copy:

```lua
-- Modules/Stages/mystage/Script.lua
local text = nil
local textTex = nil

function onStart()          -- once when the skin loads: load assets here
    text = TEXT:Create(16)
end

function activate()         -- each time the stage is entered
    textTex = text:GetText("Hello")
end

function update(timestamp)  -- every frame: input and state changes
    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        return Exit("title", nil)
    end
end

function draw()             -- every frame: drawing only
    if textTex ~= nil then textTex:Draw(200, 200) end
end

function deactivate()       -- when the stage is left: stop sounds, close databases
end

function onDestroy()        -- before the skin is unloaded: dispose what you created
    if textTex ~= nil then textTex:Dispose() end
end
```

## The stage lifecycle

- onStart(): the game calls it once after the skin loads, and again after every skin reload, whether or not the stage is on screen. Load textures, sounds and videos here. It runs as a coroutine, so a long load can spread across frames behind the loading bar with the LOADING helper.
- activate(): the game calls it on every entry into the stage. Reset per-visit state, start music and open databases here. It also runs as a coroutine and can use LOADING. The game refreshes the character and puchichara lists (CHARACTERLIST, PUCHICHARALIST) right before activate runs, so read them here; onStart runs before that refresh.
- update(timestamp): the game calls it every frame before draw and passes the game clock in milliseconds. Handle input and change state here. Once the stage has called Exit, the game stops calling update and keeps calling draw through the fade-out.
- draw(): the game calls it every frame. Draw only, and keep per-frame allocations low.
- deactivate(): the game calls it when leaving the stage. demo3 disposes its databases here; demo1 stops its music and video.
- afterSongEnum(): the game calls it every time song enumeration finishes, at boot and after a soft or hard reload, even when the stage is not active. Use it when the module depends on the song list.
- onDestroy(): the game calls it before it unloads the skin. demo1 disposes its texture, video, text texture and sounds here.
- reloadLanguage(lang): the game calls it when the language changes (see the localization section below).

When a skin loads, the game creates the modules kind by kind, Transitions first, then Stages, Activities and ROActivities. Within a kind it runs every Script.lua before calling any onStart. Activities and ROActivities therefore do not exist yet while a stage's onStart runs; look them up in activate.

The other kinds use variations of this set. Activities and ROActivities have the same callbacks, but the hosting stage invokes activate, deactivate, draw and update and receives their return values. Backgrounds receive a state object in activate(state), update(timestamp, state) and draw(state) and can define event hooks such as clearIn, playEndAnime and kusuBroke. Transitions define fadeOut(t), loading(progress, elapsed) and fadeIn(t). Characters define an animation and voice set of their own. [Modules and lifecycle](api/activities.md) lists all of them.

## Choosing a module kind

- Stage (Modules/Stages): a full screen the game switches to. It owns the frame, handles input and leaves by calling Exit. Use it for anything that is a screen of its own.
- Activity (Modules/Activities): a sub-screen a stage uses from inside, such as a dialog. It is a singleton you look up with ACTIVITY:GetActivity(name); the hosting stage calls its Activate, Update, Draw and Deactivate. Use it for shared pieces that may write game state.
- ROActivity (Modules/ROActivities): the read-only form of an Activity, which you look up with ROACTIVITY:GetROActivity(name). It gets read-only CONFIG, DATABASE and GetSaveFile and has no ACTIVITY global. Use it for pieces that only read state, which covers most reusable UI. The engine hosts several of its own overlays as ROActivities with fixed names (nameplate, modal, modicons, danplate, popup_menu, config_ui, song_enum); a skin replaces one by shipping a folder with that name, keeping the callbacks the engine calls.
- Background: a Script.lua under Graphics that draws behind or on top of one of the engine's screens. Backgrounds get the same read-only globals as ROActivities.
- Transition (Modules/Transitions): the fade-out, loading and fade-in the game plays between stages. A stage picks one by name in the third argument of Exit; the game falls back to the one named default when the stage names none or the name does not exist, and plays the one named song_loading to enter gameplay.
- Character: see [Adding characters](guides/characters.md).

## Leaving a stage with Exit

Only stages have the Exit global. It takes up to three arguments and accepts nil in any position: the target ("title", "play", "stage" or "legacy"; nil means "title"), the name of the destination stage when the target is "stage" (or a legacy key when it is "legacy"), and the name of a transition module. The bundled stages write `return Exit(...)` inside update so that nothing else runs in that frame.

```lua
-- from demo1/Script.lua, inside update()
if INPUT:KeyboardPressed("S") == true then
    sounds.Skip:Play()
    return Exit("stage", "demo2")   -- jump to Modules/Stages/demo2
end
-- ...
return Exit("title", nil)           -- back to the title screen
```

## The sandbox

Every Script.lua runs in a restricted Lua state:

- os keeps only time, date and difftime. The sandbox removes io, debug, loadfile and dofile, and import does nothing.
- package shrinks to a custom loader: package.path and package.cpath are empty and the sandbox replaces the standard searchers, so only the paths below are searchable.
- require first looks in the module's own folder, then in the skin's Modules/Lib folder, and loads the first file it finds. A module file and a Lib file with the same name resolve to the module file. Dots in the name become path separators, so require("DBControllers.dbScores") and require("DBControllers/dbScores") both load DBControllers/dbScores.lua. Non-ASCII paths work.

```lua
-- from intro_nokon/Script.lua
local DBScores  = require("DBControllers/dbScores")  -- the module's own subfolder
local I18N      = require("i18n")                     -- Modules/Lib/i18n.lua
local Opening   = require("opening")                  -- the module folder
local Dialogue  = require("nokon_dialogue")           -- the module folder
```

## Read-only modules

The game creates ROActivities and backgrounds with restricted globals before their Script.lua runs: CONFIG is a read-only view, GetSaveFile(player) returns a read-only save file, DATABASE opens read-only stores, and ACTIVITY is nil (use ROACTIVITY). A write through any of these logs an error notification, does nothing, and raises no Lua error. A module that needs to change settings, save data or a database must be an Activity or a Stage.

## Localization with lang/

The bundled skin translates each module's own strings with the shared library Modules/Lib/i18n.lua. The English string in the code is the key: the module ships a lang/ja.lua that returns a table mapping each English string to its Japanese translation, and the library looks strings up in that table.

The library has three functions:

- detect() reads the current game language through the LANG global and loads the dictionary for it. When the language is Japanese it requires lang/ja, which resolves inside the module folder, so each module has its own dictionary; for any other language it loads nothing. Until you call it, no dictionary is loaded and every string stays English.
- tr(s) returns the translation of s from the loaded dictionary, or s itself when the dictionary has no entry for it or no dictionary is loaded.
- trf(fmt, ...) translates the format string fmt the same way, then formats it with string.format.

Call detect() in activate, then build your text with tr and trf. activate runs on every entry into the module, so a language the player changed in the settings takes effect on the next visit, and the module needs no other hook. Keys must match the English source exactly, including punctuation, spacing and newlines, and the translation must keep placeholders such as %s or {Player 1 name} verbatim.

```lua
-- Modules/Stages/mystage/lang/ja.lua
local T = {}
T["Nokon"] = "ノコン"
T["Alright, quiz time!"] = "さあ、クイズの時間である！"
return T
```

```lua
-- Modules/Stages/mystage/Script.lua
local I18N = require("i18n")
local title

function activate()
    I18N.detect()
    title = I18N.tr("Alright, quiz time!")
end
```

The game also calls a global reloadLanguage(lang) on every loaded module when the language changes. Only a module that stays on screen while the language changes needs it, such as a screen with a language selector; there, call detect() again and rebuild the pre-rendered text.

## Things to watch out for

- The game frees the textures, sounds, videos and text objects a module created when it disposes the module, so a skin reload does not leak them. Dispose resources you open per visit, such as databases, in deactivate, as demo3 does, and dispose what you created in onDestroy, as demo1 does.
- GetText caches one texture per distinct string on its text object. A string that changes every frame adds a texture every frame and the game slows down progressively. Draw changing values with the glyph renderer (TEXT:CreateGlyphCached), or keep one texture until the value changes.
- LOADING only works in callbacks that run as coroutines: onStart of any module, and a stage's activate. Calling LOADING:Tick from an Activity's activate, or from update or draw, raises a Lua error.
- onStart and afterSongEnum run while the module is off screen. Write them so they work without the stage being visible.
