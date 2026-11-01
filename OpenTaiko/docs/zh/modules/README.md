<!-- modules/README.md -->

# 内置模块

OpenTaiko 自带一套所有皮肤都能使用的 Lua 模块库：辅助工具（缓动、颜色、JSON、输入、本地化）、一个 UI 库、一个对话框、3D 世界引擎，以及未知之塔、秘密金库和金币商店的规则与数据。它们位于游戏文件夹而不是皮肤中，因此新皮肤无需复制任何文件即可使用。本节逐一介绍它们；它们所依赖的引擎全局对象见 [API 参考](../api/README.md)。

## Global/Modules

游戏安装目录中的 `Global/Modules/` 与皮肤的 `Modules/` 有相同的子文件夹：`Transitions/`、`Stages/`、`Activities/`、`ROActivities/` 和 `Lib/`。无论启用的是哪个皮肤，游戏都会在每个分类中紧接着皮肤的模块加载全局模块。这些页面介绍的模块是 `Global/Modules/Lib/` 中的文件，通过 `require` 加载；`Global/Modules/Stages/` 中还有任何皮肤都能打开的舞台，例如[我的房间](my-room.md)。

这个文件夹随游戏版本一起发布，并在每次更新时被替换，因此皮肤不应往里面放文件。要修改某个内置模块，请以另一个名称把它复制到你自己的皮肤中。

## require 的查找顺序

`require` 把点转换为文件夹并且不区分大小写，因此 `require("Core.Easing")` 会加载 `Core/Easing.lua`。它查找的文件夹取决于正在运行的模块：

| 调用方 | 依次查找 |
| --- | --- |
| 皮肤模块（`System/<skin>/Modules/...`） | 自己的文件夹、皮肤的 `Modules/Lib/`，然后是 `Global/Modules/Lib/` |
| 全局模块（`Global/Modules/...`） | 自己的文件夹、`Global/Modules/Lib/`，然后是皮肤的 `Modules/Lib/` |

Lib 文件没有自己的 Lua 状态：它运行在 require 它的模块的状态中，因此其中的 require 遵循调用方的顺序。所以，如果皮肤的 `Modules/Lib/` 中有一个与内置模块路径相同的文件，那么在皮肤模块发起的所有 require 中（包括其他内置模块内部的 require），它都会取代该内置模块。除非你打算替换某个模块，否则不要把自己的 Lib 文件放在下面这些文件夹名下。

## 命名

每个组件是 `Global/Modules/Lib/` 中的一个文件夹，其模块以 `<Component>.<Module>` 的形式 require：

| 组件 | 模块 | 页面 |
| --- | --- | --- |
| `Core` | `Easing`, `Color`, `Util`, `Json`, `Here` | [Core](core.md) |
| `Input` | `NavInput` | [Input](input.md) |
| `Audio` | `MusicDuck` | [Audio](audio.md) |
| `Locale` | `i18n` | [Locale](locale.md) |
| `Calendar` | `almanac` | [Calendar](calendar.md) |
| `Songs` | `PlayStrings` | [Songs](songs.md) |
| `World` | `OWM3d` 及其各部分 | [World](world.md) |
| `UI` | `SimpleUI`, `Pick` | [UI](ui.md) |
| `Saves` | `SavePicker` | [Saves](saves.md) |
| `Characters` | `Student` | [Characters](characters.md) |
| `VN` | `Dialogue` | [VN](vn.md) |
| `Pagoda` | `Levels`, `Floor`, `Rewards`, `Progress`, `Songs`, `Run`, `Texts` | [Pagoda](pagoda.md) |
| `Vault` | `Keys`, `Chests`, `Texts` | [Vault](vault.md) |
| `Shop` | `Daily`, `Catalog`, `Texts`, `Seasonal`, `Popup` | [Shop](shop.md) |
| `Online` | `Lobby`, `Protocol`, `Identity`, `Media`, `Texts` | [Online](online.md) |
| `Stages` | `CustomStages` | [自定义舞台](custom-stages.md) |

## Open-World Memories 保留的名称

自带皮肤以前把这些文件中的大部分放在自己的 `Modules/Lib/` 中。现在每个旧文件都是一个两行的转发文件，因此仍然 require 旧名称的皮肤模块会得到全局模块，两个名称共享同一个实例（同一份状态、同一份缓存）：

```lua
-- moved to Global/Modules/Lib/Core/Easing.lua
return require("Core.Easing")
```

| 旧名称 | 内置模块 |
| --- | --- |
| `Easing`, `Color`, `Util` | `Core.Easing`, `Core.Color`, `Core.Util` |
| `NavInput`, `MusicDuck`, `PlayStrings` | `Input.NavInput`, `Audio.MusicDuck`, `Songs.PlayStrings` |
| `i18n`, `almanac` | `Locale.i18n`, `Calendar.almanac` |
| `OWM3d`, `OWM3d.<part>` | `World.OWM3d`, `World.OWM3d.<part>` |
| `PopUI.<part>`（共享代码：`manager`、`widget`、`shape`、`util`、`cache`、`sfx`，以及除 `widgets.coins` 之外的所有 `widgets.*`） | `UI.SimpleUI.<part>` |
| `SavePicker`, `Student`, `dialogue` | `Saves.SavePicker`, `Characters.Student`, `VN.Dialogue` |

`PopUI` 本身仍是皮肤模块：它是带有皮肤自己外观的 SimpleUI（见 [UI](ui.md)）。新代码应 require 内置模块的名称。

## 资源相对于调用方解析

传给 `TEXTURE`、`SOUND`、`VIDEO`、`JSONLOADER`、`SQL`、`DATABASE`、`STORAGE` 和 `SHARED` 的相对路径相对于调用方模块的文件夹解析，而不是相对于发起调用的 Lib 文件。自带文件（图片、JSON 池、数据库）的内置模块通过 `Core.Here` 找到它们，该函数返回文件自己的文件夹，然后用绝对路径版本的加载器加载：

```lua
-- Global/Modules/Lib/MyPart/Thing.lua
local HERE = require("Core.Here")(...)          -- ".../Global/Modules/Lib/MyPart/"

local icon = TEXTURE:CreateTextureFromAbsolutePath(HERE .. "icon.png")
local beep = SOUND:CreateSFXFromAbsolutePath(HERE .. "beep.ogg")
local data = JSONLOADER:JsonParseFileAny(HERE .. "data.json")
SHARED:SetSharedTextureUsingAbsolutePath("thing_icon", HERE .. "icon.png")
```

在文件的顶层传入 `...`：`require` 在那里把模块名和绝对路径交给每个文件。调用方传给内置模块的相对路径（`World.OWM3d` 的模型文件、控件的声音）属于调用方，照常相对于调用方的文件夹解析。

## 字符串与模块放在一起

`THEME:GetSkinString` 只读取当前皮肤的 `Locales/`，因此内置模块不会用它来取自己的文本。它的字符串放在模块旁边的 `lang/` 文件夹中，每个功能、每种语言一个 JSON 文件（`lang/en/vault.json`、`lang/ja/vault.json`……），并通过 [Locale.i18n](locale.md) 指定模块的文件夹来读取：

```lua
local HERE = require("Core.Here")(...)
local T = require("Locale.i18n").texts("vault", HERE)   -- HERE .. "lang/<code>/vault.json"
local name = T:tr("chest_1")
```

英文（`lang/en/`）是缺少文件或键的语言的回退。全局舞台或活动不带文件夹参数即可做同样的事：`texts(name)` 读取舞台自己的 `lang/` 文件夹。

## 共享状态

`SHARED` 的纹理、声音和字符串在整个游戏中构成一个命名空间，皮肤模块和全局模块都在其中。内置模块使用几个固定的键，列在各自的页面上（例如共享字符串 `ui_library`、纹理 `myroom_thumb_<id>`）。请给你自己的键加上前缀，以免冲突。存档数据（触发器、计数器和字符串）属于存档，而不属于某个模块，未知之塔、金库、商店和我的房间正是通过它互相传递物品。
