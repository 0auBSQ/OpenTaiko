<!-- modules/README.md -->

# Provided modules

OpenTaiko ships a library of Lua modules that every skin can use: helpers (easing, colours, JSON, input, localization), a UI library, a dialogue box, the 3D world engine, and the rules and data of the pagoda, the secret vault and the coin shop. They live in the game folder, not in a skin, so a new skin gets them without copying anything. This section describes each one; the [API reference](../api/README.md) covers the engine globals they are built on.

## Global/Modules

`Global/Modules/` in the game's install folder has the same subfolders as a skin's `Modules/`: `Transitions/`, `Stages/`, `Activities/`, `ROActivities/` and `Lib/`. The game loads every global category right after the skin's, for whichever skin is active. The modules on these pages are the files of `Global/Modules/Lib/`, reached with `require`; `Global/Modules/Stages/` also holds stages any skin can open, such as [My Room](my-room.md).

The folder arrives with the game build and is replaced by each update, so a skin should not ship files into it. To change a provided module, copy it into your own skin under another name.

## The require order

`require` turns dots into folders and ignores case, so `require("Core.Easing")` loads `Core/Easing.lua`. The folders it searches depend on the module that runs:

| Caller | Searched, in order |
| --- | --- |
| A skin module (`System/<skin>/Modules/...`) | its own folder, the skin's `Modules/Lib/`, then `Global/Modules/Lib/` |
| A global module (`Global/Modules/...`) | its own folder, `Global/Modules/Lib/`, then the skin's `Modules/Lib/` |

A Lib file has no Lua state of its own: it runs in the state of the module that required it, so the requires inside it follow that caller's order. A file in the skin's `Modules/Lib/` with the same path as a provided module therefore replaces it for every require made from the skin's modules, the requires inside other provided modules included. Keep your own Lib files out of the folder names below unless you mean to replace a module.

## Naming

Each component is a folder of `Global/Modules/Lib/`, and its modules are required as `<Component>.<Module>`:

| Component | Modules | Page |
| --- | --- | --- |
| `Core` | `Easing`, `Color`, `Util`, `Json`, `Here` | [Core](core.md) |
| `Input` | `NavInput` | [Input](input.md) |
| `Audio` | `MusicDuck` | [Audio](audio.md) |
| `Locale` | `i18n` | [Locale](locale.md) |
| `Calendar` | `almanac` | [Calendar](calendar.md) |
| `Songs` | `PlayStrings` | [Songs](songs.md) |
| `World` | `OWM3d` and its parts | [World](world.md) |
| `UI` | `SimpleUI`, `Pick` | [UI](ui.md) |
| `Saves` | `SavePicker` | [Saves](saves.md) |
| `Characters` | `Student` | [Characters](characters.md) |
| `VN` | `Dialogue` | [VN](vn.md) |
| `Pagoda` | `Levels`, `Floor`, `Rewards`, `Progress`, `Songs`, `Run`, `Texts` | [Pagoda](pagoda.md) |
| `Vault` | `Keys`, `Chests`, `Texts` | [Vault](vault.md) |
| `Shop` | `Daily`, `Catalog`, `Texts`, `Seasonal`, `Popup` | [Shop](shop.md) |
| `Online` | `Lobby`, `Protocol`, `Identity`, `Media`, `Texts` | [Online](online.md) |
| `Stages` | `CustomStages` | [Custom stages](custom-stages.md) |

## The names Open-World Memories keeps

The shipped skin used to carry most of these files in its own `Modules/Lib/`. Each old file is now a forwarder of two lines, so a skin module that still requires the old name gets the global module, and both names share one instance (one state, one cache):

```lua
-- moved to Global/Modules/Lib/Core/Easing.lua
return require("Core.Easing")
```

| Old name | Provided module |
| --- | --- |
| `Easing`, `Color`, `Util` | `Core.Easing`, `Core.Color`, `Core.Util` |
| `NavInput`, `MusicDuck`, `PlayStrings` | `Input.NavInput`, `Audio.MusicDuck`, `Songs.PlayStrings` |
| `i18n`, `almanac` | `Locale.i18n`, `Calendar.almanac` |
| `OWM3d`, `OWM3d.<part>` | `World.OWM3d`, `World.OWM3d.<part>` |
| `PopUI.<part>` (the shared code: `manager`, `widget`, `shape`, `util`, `cache`, `sfx`, every `widgets.*` but `widgets.coins`) | `UI.SimpleUI.<part>` |
| `SavePicker`, `Student`, `dialogue` | `Saves.SavePicker`, `Characters.Student`, `VN.Dialogue` |

`PopUI` itself stays a skin module: it is SimpleUI with the skin's own look (see [UI](ui.md)). New code should require the provided names.

## Assets resolve against the caller

Relative paths given to `TEXTURE`, `SOUND`, `VIDEO`, `JSONLOADER`, `SQL`, `DATABASE`, `STORAGE` and `SHARED` resolve against the folder of the calling module, never against the Lib file that makes the call. A provided module that ships its own files (a picture, a JSON pool, a database) finds them through `Core.Here`, which returns the file's own folder, and loads them with the absolute-path variants:

```lua
-- Global/Modules/Lib/MyPart/Thing.lua
local HERE = require("Core.Here")(...)          -- ".../Global/Modules/Lib/MyPart/"

local icon = TEXTURE:CreateTextureFromAbsolutePath(HERE .. "icon.png")
local beep = SOUND:CreateSFXFromAbsolutePath(HERE .. "beep.ogg")
local data = JSONLOADER:JsonParseFileAny(HERE .. "data.json")
SHARED:SetSharedTextureUsingAbsolutePath("thing_icon", HERE .. "icon.png")
```

Pass `...` from the file's top level: `require` hands each file its module name and its absolute path there. A relative path a caller passes to a provided module (a model file for `World.OWM3d`, a sound for a widget) is the caller's, and resolves against the caller's folder as usual.

## Strings live with the module

`THEME:GetSkinString` reads only the active skin's `Locales/`, so a provided module never uses it for its own text. Its strings sit in a `lang/` folder next to it, one JSON file per feature and language (`lang/en/vault.json`, `lang/ja/vault.json`, ...), read through [Locale.i18n](locale.md) with the module's folder:

```lua
local HERE = require("Core.Here")(...)
local T = require("Locale.i18n").texts("vault", HERE)   -- HERE .. "lang/<code>/vault.json"
local name = T:tr("chest_1")
```

English (`lang/en/`) is the fallback for a language that lacks a file or a key. A global stage or activity does the same without the folder argument: `texts(name)` reads the `lang/` folder of the stage itself.

## Shared state

`SHARED` textures, sounds and strings form one namespace for the whole game, skin and global modules alike. The provided modules use a few fixed keys, listed on their pages (for example the shared string `ui_library`, the textures `myroom_thumb_<id>`). Prefix your own keys so they cannot collide. Save data (triggers, counters and strings) belongs to the save file, not to a module, which is how the pagoda, the vault, the shop and My Room hand items to one another.
