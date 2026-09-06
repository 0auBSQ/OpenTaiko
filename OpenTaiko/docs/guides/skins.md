<!-- guides/skins.md -->

# Adding Skins and Themes: Creating a New Skin for OpenTaiko

A skin is a folder under the game's `System/` directory. It supplies the graphics, sounds, fonts, layout values, locale files and the Lua modules that draw every screen. This guide explains what makes a folder a skin, the `SkinConfig.ini` keys, the folder layout, the Lua module tree and its lifecycle, and how you install and select a skin. The API reference covers the Lua API itself (drawing, sound, input and so on).

A skin also carries the Lua modules that run each screen. The game loads specific modules by name and jumps to specific stages, so the skin chooser lists a skin built from scratch but the game cannot run it. Start from a copy of the shipped skin.

## Before you start

- OpenTaiko 0.6.1 installed, with the shipped skin `System/Open-World Memories/` present.
- A plain-text editor for `SkinConfig.ini`, the included `*Config.ini` files and the Lua modules.
- Basic Lua if you intend to change screen behaviour. A pure re-texture (replacing PNG and OGG files and editing `.ini` values) needs no Lua.

## Step 1: Understand what makes a folder a skin

At start-up the game lists the subfolders of `System/`. A folder counts as a skin only if `Graphics/1_Title/Background.png` exists inside it; the game skips any other folder. If the selected skin folder is missing, the game falls back to `System/Default/`, then to the first valid skin in alphabetical order, then to `System/` itself.

This check only gets the folder listed. Step 8 names the modules that must also exist before the skin runs.

```
System/
  Open-World Memories/         <- the shipped skin
  My New Skin/                 <- your skin
    Graphics/
      1_Title/
        Background.png          <- required for the folder to be listed
    SkinConfig.ini
```

## Step 2: Copy the shipped skin

Copy `System/Open-World Memories/` to a new sibling folder, for example `System/My New Skin/`. The copy contains everything the game needs: `Graphics/`, `Sounds/`, `Fonts/`, `Locales/`, `Modules/`, `ThemeSettings.json`, `SkinConfig.ini` and the `*Config.ini` files it includes. The folder name is the skin's identity (the game records it as the selected skin and the skin chooser displays it), so keep it file-system safe. Then edit `SkinConfig.ini` so the metadata describes your skin.

## Step 3: Edit SkinConfig.ini

`SkinConfig.ini` is a `Key=Value` file, one setting per line. The parser trims leading spaces and tabs, treats lines starting with `;` as comments, and reads a line only when it contains exactly one `=`. Key matching is exact, and the parser ignores an unknown key without reporting an error. The skin-level keys are:

- `Name=`: display name. Metadata only; the folder name selects the skin.
- `Version=`, `Creator=`: free-form strings (default `Unknown`). The game does not validate them.
- `DefaultLocale=`: locale id the game uses when the active game language has no file under `Locales/` (default `en`).
- `Resolution=W,H`: the resolution you author the layout values for (default `1280,720`). The shipped skin uses `1920,1080`.
- `Resolutions=`: selectable render-scale multipliers (Step 4).
- `AIBattleCharacter=`: character folder used for the AI opponent (Step 5).
- `FontName<LANG>=` and `BoxFontName<LANG>=`: font file per game language, where `<LANG>` is the upper-cased language code (`EN`, `JA`, `FR`, `ES`, `NL`, `DE`, `RU`, `KO`, `ZH`). The path is relative to the skin root (an absolute path also works) and the file must exist, otherwise the parser drops the key.

Every other key (`Game_*`, `Result_*`, `Title_*` and so on) is a screen layout value. The same parser reads them, which is why they can live in the included files of Step 6.

```ini
;Skin information
Name=My New Skin
DefaultLocale=en
Version=1.0.0
Creator=Your Name
Resolution=1920,1080
;Selectable render-scale multipliers (<=1; decimal or a/b fraction, comma-separated). 1 is always available and is the default.
Resolutions=1,2/3,1/3
;Character folder used for the AI battle slot.
AIBattleCharacter=10v2 - AItritus
FontNameEN=Fonts/MPLUSRounded1c-Medium.ttf
FontNameJA=Fonts/MPLUSRounded1c-Medium.ttf
BoxFontNameEN=Fonts/MPLUSRounded1c-Regular.ttf
BoxFontNameJA=Fonts/MPLUSRounded1c-Regular.ttf
```

## Step 4: The Resolutions option

`Resolutions=` is a comma-separated list of the render-scale multipliers that the settings menu offers. The game renders at `Resolution` times the chosen multiplier and upscales the result to the window; the window size does not change. Each token is a decimal (`0.5`) or a fraction (`2/3`). The parser drops tokens outside the range 0 < value <= 1, unparsable tokens and duplicates, adds `1` if it is missing, and sorts the list with `1` first. The separator must be a comma, because a semicolon starts a comment line. The settings menu shows each entry with its pixel size, for example `2/3 (1280x720)` for a 1920x1080 skin.

```ini
Resolution=1920,1080
Resolutions=1,2/3,1/3
; produces the options:
;   1     -> 1920x1080  (default)
;   2/3   -> 1280x720
;   1/3   -> 640x360
```

## Step 5: The AIBattleCharacter option

`AIBattleCharacter=` names the folder under `Global/Characters/` that the game uses for the AI opponent in AI battle mode. The default is `10v2 - AItritus`. The named folder must exist.

```ini
;Character folder used for the AI battle slot.
AIBattleCharacter=10v2 - AItritus
```

## Step 6: Split the configuration with #include

When the parser meets a line of the form `#include SomeFile.ini`, it reads that file in place, recursively. The path is relative to the skin root. The shipped `SkinConfig.ini` holds only the metadata and font keys and then includes one file per screen. Keep these lines when you copy the skin, and edit the individual `*Config.ini` files to retune a screen.

```
; tail of SkinConfig.ini (shipped skin, in order)
#include OtherConfig.ini
#include TitleConfig.ini
#include ConfigConfig.ini
#include SongSelectConfig.ini
#include HeyaConfig.ini
#include SongLoadingConfig.ini
#include GameConfig.ini
#include ModIconsConfig.ini
#include NameplateConfig.ini
#include AIResultConfig.ini
#include ResultConfig.ini
#include DaniSelectConfig.ini
#include DanResultConfig.ini
#include TowerResultConfig.ini
#include TowerSelectConfig.ini
#include OnlineLoungeConfig.ini
#include OpenEncyclopediaConfig.ini
#include ModalConfig.ini
#include Game4PConfig.ini
#include Result4PConfig.ini
#include Modal4PConfig.ini
```

## Step 7: Learn the skin folder layout

With the shipped skin as the reference, the skin root contains:

- `Graphics/`: images grouped in numbered per-screen folders (`0_Startup`, `1_Title`, `2_Config`, `3_DaniSelect`, `5_Game`, `6_Result`, `7_DanResult`, `7_Exit`, `8_TowerResult`, `10_Heya`, `12_OnlineLounge`, `13_TowerSelect`, `15_OpenEncyclopedia`) plus a few shared images at the top. Animated backgrounds are `Script.lua` files that sit next to the images of the folder they belong to (for example `Graphics/0_Startup/Script.lua` and the folders under `Graphics/5_Game/5_Background/`).
- `Sounds/`: system sounds and BGM that the game loads by fixed file name, for example `Sounds/Move.ogg`, `Sounds/Decide.ogg`, `Sounds/Cancel.ogg`, `Sounds/BGM/Title.ogg`, `Sounds/BGM/SongSelect.ogg`, `Sounds/BGM/Result.ogg`. If a file is missing, that sound does not play.
- `Fonts/`: the `.ttf` files referenced by the `FontName` keys.
- `Locales/`: one JSON file per language (`en.json`, `ja.json`, ...) with the shape `{ "Entries": { "KEY": "text" } }`. These strings label the skin's own settings, and Lua reads them through `THEME:GetSkinString(key)`. When a key is missing from the active language, the game looks it up in the `DefaultLocale` file.
- `Modules/`: the Lua module tree (Step 8).
- `ThemeSettings.json`: an array of settings that the options screen shows under Theme Settings. Each entry has `id`, `type` (`bool`, `int`, `double`, `string` or `enum`), `scope` (`global`, the default, or `save` for one value per save file), localized `label` and `description`, `default`, and `min`/`max` or `options` depending on the type.
- `SkinConfig.ini` and the included `*Config.ini` files.
- `README.txt`, `LICENSE.md`, `Licenses/`: attribution files. The game does not read them.

```
My New Skin/
  SkinConfig.ini
  ThemeSettings.json
  Graphics/           images by screen; some folders carry a background Script.lua
  Sounds/             fixed-name .ogg system sounds and BGM/
  Fonts/              .ttf files named by the FontName keys
  Locales/            en.json, ja.json, ... ({ "Entries": { ... } })
  Modules/            the Lua module tree (Step 8)
  <screen>Config.ini  layout files pulled in via #include
```

## Step 8: The Modules tree and the modules the game requires

When the skin loads, the game scans four subfolders of `Modules/` and treats every direct subfolder inside them as one module whose entry file is `Script.lua`:

- `Modules/Transitions/`: transitions that play between stages. The game loads them first, so they are ready for the first stage switch.
- `Modules/Stages/`: full screens. You enter a stage with `Exit("stage", "<folder name>")`.
- `Modules/Activities/`: reusable sub-screens layered on a stage (for example `confirm_dialog`, `mod_select_dialog`, `song_select_core`).
- `Modules/ROActivities/`: read-only overlays the game drives directly.

The game does not scan `Modules/Lib/`. You load files there with `require`: a module's search path is its own folder followed by `Modules/Lib/`, so `require("dialogue")` resolves to `Modules/Lib/dialogue.lua`. You can also place stages and activities under `Global/Stages/` and `Global/Activities/` in the game's install folder; the game loads those for every skin.

Within each category the game creates every module first and then runs `onStart` on each, in the order Transitions, Stages, Activities, ROActivities.

The game looks up these modules by name, and the shipped skin provides all of them:

- Stages `_boot` and `_title`. The game stops with an error if either is missing.
- ROActivities `modal`, `config_ui`, `nameplate`, `popup_menu`, `modicons`, `song_enum` and `danplate`.
- Transitions `default` and `song_loading`. `song_loading` plays while the game loads a song; the game uses `default` when `Exit` names no transition or names one that does not exist. A skin with no transition modules at all falls back to a plain black fade.

Keep all of these in place when building a skin; add your own modules alongside them.

```
Modules/
  Transitions/   <name>/Script.lua   (loaded first; "default" and "song_loading" used by the game)
  Stages/        <name>/Script.lua   ("_boot" and "_title" required)
  Activities/    <name>/Script.lua
  ROActivities/  <name>/Script.lua   (modal, config_ui, nameplate, popup_menu, modicons, song_enum, danplate required)
  Lib/           shared .lua files reached with require, not scanned
```

## Step 9: A stage's Script.lua and its lifecycle

`Script.lua` runs once when the game creates the module, with the engine globals (`TEXTURE`, `SOUND`, `INPUT`, `CONFIG`, `THEME` and the rest) already defined. The game then looks up global functions by name and calls them. For a stage:

- `onStart()`: once, when the skin loads. Runs as a coroutine, so heavy loading can call `coroutine.yield()` or the `LOADING` helpers to spread work across frames behind the loading bar.
- `activate()`: each time the game enters the stage. Also a coroutine. The game refreshes the `CHARACTERLIST` and `PUCHICHARALIST` globals right before it runs, so build anything that depends on them here; in `onStart` they are still empty.
- `update(timestamp)`: every frame. Return `Exit(target, name, transition)` to leave the stage. `target` is `"title"`, `"play"`, `"stage"` (with `name` = a stage folder) or `"legacy"` (with `name` = `heya`, `config`, `exit` or `onlinelounge`); `transition` is a folder under `Modules/Transitions/` and defaults to `default`.
- `draw()`: every frame.
- `deactivate()`: when the game leaves the stage.
- `afterSongEnum()`: when the song list has finished enumerating.
- `onDestroy()`: when the game tears the skin down.

All of them are optional; the game skips a function you do not define. Activities, ROActivities and Transitions follow the same pattern with their own hook sets.

```lua
-- Modules/Stages/my_stage/Script.lua
function onStart()
  -- one-time setup; may coroutine.yield() during heavy loads
end

function activate()
  -- runs each time the stage is entered
end

function update(ts)
  if INPUT:Pressed("Cancel") then
    return Exit("stage", "_title")   -- leave this stage
  end
  return nil
end

function draw()
  -- per-frame rendering
end

function deactivate() end
function afterSongEnum() end
function onDestroy() end
```

## Step 10: Localize a module with lang/

A module can keep its own translations in a `lang/` subfolder next to `Script.lua`. Because the module's own folder is on its `require` path, `require("lang.ja")` resolves to `lang/ja.lua`. The shipped skin does this for its larger stages (for example `Modules/Stages/myroom/lang/ja.lua` and `Modules/Stages/intro_nokon/lang/ja.lua`) through the helper `Modules/Lib/i18n.lua`. This is separate from the skin-wide `Locales/` folder of Step 7.

```
Modules/Stages/my_stage/
  Script.lua
  lang/
    ja.lua        -- require("lang.ja")
```

## Step 11: Install and select the skin

Place the folder under `System/`. Open the settings, go to the Appearance section and pick the skin from the Skin option; the chooser lists every valid skin by folder name and shows its `Graphics/1_Title/Background.png` as a thumbnail. When you change the skin, the game tears down the current one, loads the new one and reloads all of its Lua modules behind a loading bar.

The game writes the choice to `Config.ini` as `SkinPath=`, relative to `System/`. It writes the bare folder name (for example `SkinPath=My New Skin\` on Windows) and also accepts the `./My New Skin/` form shown in the file's comment.

```ini
; In Config.ini (written when a skin is picked in-game):
; Skin folder path, relative to System/
SkinPath=My New Skin\
```

## Troubleshooting and notes

- The chooser does not list the skin: `Graphics/1_Title/Background.png` is missing, or the folder is not directly under `System/`.
- The game errors right after you select the skin: a required module is missing (Step 8) or one of them raised a Lua error. Test a skin by switching to it.
- A `SkinConfig.ini` key has no effect: the key is misspelled, the line contains more than one `=`, or the value failed to parse. The parser ignores unknown keys without reporting them.
- `Resolutions=` shows only `1`: the list used semicolons (a comment marker) or every value was outside 0 < value <= 1.
- A font key has no effect: the file path does not exist relative to the skin root.
- `CHARACTERLIST` or `PUCHICHARALIST` is empty in `onStart`: the game fills them after it creates the modules. Use them from `activate`.
- Renaming the skin folder changes its identity; `SkinPath` in `Config.ini` must point at the new name.
- `Name=`, `Version=` and `Creator=` are informational only. The game makes no compatibility check on them.
