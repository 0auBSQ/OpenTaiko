<!-- guides/skins.md -->

# 添加皮肤与主题：为 OpenTaiko 创建新皮肤

皮肤是游戏 `System/` 目录下的一个文件夹。它提供图像、声音、字体、布局值、区域设置文件，以及绘制每个界面的 Lua 模块。本指南说明什么使一个文件夹成为皮肤、`SkinConfig.ini` 的键、文件夹布局、Lua 模块树及其生命周期，以及你如何安装和选择皮肤。API 参考涵盖 Lua API 本身（绘制、声音、输入等）。

皮肤还携带运行每个界面的 Lua 模块。游戏按名称加载特定的模块并跳转到特定的舞台，因此皮肤选择器虽然会列出从零开始构建的皮肤，但游戏无法运行它。请从自带皮肤的副本开始。

## 开始之前

- 已安装 OpenTaiko 0.6.1，且自带皮肤 `System/Open-World Memories/` 存在。
- 一个用于编辑 `SkinConfig.ini`、被包含的 `*Config.ini` 文件和 Lua 模块的纯文本编辑器。
- 如果打算更改界面行为，需要基础的 Lua 知识。纯换图（替换 PNG 和 OGG 文件并编辑 `.ini` 值）不需要 Lua。

## 第 1 步：了解什么使一个文件夹成为皮肤

启动时游戏列出 `System/` 的子文件夹。只有内部存在 `Graphics/1_Title/Background.png` 的文件夹才算皮肤；游戏会跳过其他文件夹。如果选定的皮肤文件夹缺失，游戏依次回退到 `System/Default/`、按字母顺序的第一个有效皮肤，最后是 `System/` 本身。

此检查只是让文件夹被列出。第 8 步指明皮肤要能运行还必须存在哪些模块。

```
System/
  Open-World Memories/         <- 自带皮肤
  My New Skin/                 <- 你的皮肤
    Graphics/
      1_Title/
        Background.png          <- 文件夹被列出所必需
    SkinConfig.ini
```

## 第 2 步：复制自带皮肤

把 `System/Open-World Memories/` 复制到一个新的同级文件夹，例如 `System/My New Skin/`。副本包含游戏需要的一切：`Graphics/`、`Sounds/`、`Fonts/`、`Locales/`、`Modules/`、`ThemeSettings.json`、`SkinConfig.ini` 及其包含的 `*Config.ini` 文件。文件夹名称是皮肤的身份（游戏把它记录为选定的皮肤，皮肤选择器也显示它），因此请让它对文件系统安全。然后编辑 `SkinConfig.ini`，让元数据描述你的皮肤。

## 第 3 步：编辑 SkinConfig.ini

`SkinConfig.ini` 是一个 `Key=Value` 文件，每行一项设置。解析器会去掉行首的空格和制表符，把以 `;` 开头的行视为注释，并且只在一行恰好包含一个 `=` 时才读取它。键的匹配是精确的，解析器会忽略未知的键且不报错。皮肤级别的键有：

- `Name=`：显示名称。仅为元数据；选择皮肤依据的是文件夹名称。
- `Version=`、`Creator=`：任意字符串（默认 `Unknown`）。游戏不校验它们。
- `DefaultLocale=`：当前游戏语言在 `Locales/` 下没有文件时游戏使用的区域设置 id（默认 `en`）。
- `Resolution=W,H`：你编写布局值时所依据的分辨率（默认 `1280,720`）。自带皮肤使用 `1920,1080`。
- `Resolutions=`：可选的渲染缩放倍率（第 4 步）。
- `AIBattleCharacter=`：AI 对手使用的角色文件夹（第 5 步）。
- `FontName<LANG>=` 和 `BoxFontName<LANG>=`：每种游戏语言的字体文件，其中 `<LANG>` 是大写的语言代码（`EN`、`JA`、`FR`、`ES`、`NL`、`DE`、`RU`、`KO`、`ZH`）。路径相对于皮肤根目录（绝对路径也可以），且文件必须存在，否则解析器会丢弃该键。

其他所有键（`Game_*`、`Result_*`、`Title_*` 等）都是界面布局值。同一个解析器读取它们，因此它们可以放在第 6 步的被包含文件中。

```ini
;皮肤信息
Name=My New Skin
DefaultLocale=en
Version=1.0.0
Creator=Your Name
Resolution=1920,1080
;可选的渲染缩放倍率（<=1；小数或 a/b 分数，逗号分隔）。1 始终可用且为默认值。
Resolutions=1,2/3,1/3
;AI 对战槽位使用的角色文件夹。
AIBattleCharacter=10v2 - AItritus
FontNameEN=Fonts/MPLUSRounded1c-Medium.ttf
FontNameJA=Fonts/MPLUSRounded1c-Medium.ttf
BoxFontNameEN=Fonts/MPLUSRounded1c-Regular.ttf
BoxFontNameJA=Fonts/MPLUSRounded1c-Regular.ttf
```

## 第 4 步：Resolutions 选项

`Resolutions=` 是设置菜单提供的渲染缩放倍率的逗号分隔列表。游戏以 `Resolution` 乘以所选倍率渲染，并把结果放大到窗口；窗口尺寸不变。每个标记是小数（`0.5`）或分数（`2/3`）。解析器会丢弃超出 0 < 值 <= 1 范围的标记、无法解析的标记和重复项，缺少 `1` 时补上，并把列表排序为 `1` 在前。分隔符必须是逗号，因为分号会开始一个注释行。设置菜单显示每个条目及其像素尺寸，例如 1920x1080 的皮肤显示 `2/3 (1280x720)`。

```ini
Resolution=1920,1080
Resolutions=1,2/3,1/3
; 产生以下选项：
;   1     -> 1920x1080  （默认）
;   2/3   -> 1280x720
;   1/3   -> 640x360
```

## 第 5 步：AIBattleCharacter 选项

`AIBattleCharacter=` 指定游戏在 AI 对战模式中用于 AI 对手的 `Global/Characters/` 下的文件夹。默认为 `10v2 - AItritus`。指定的文件夹必须存在。

```ini
;AI 对战槽位使用的角色文件夹。
AIBattleCharacter=10v2 - AItritus
```

## 第 6 步：用 #include 拆分配置

解析器遇到形如 `#include SomeFile.ini` 的行时，会就地递归读取该文件。路径相对于皮肤根目录。自带的 `SkinConfig.ini` 只保留元数据和字体键，然后为每个界面包含一个文件。复制皮肤时请保留这些行，并编辑各个 `*Config.ini` 文件来调整某个界面。

```
; SkinConfig.ini 的尾部（自带皮肤，按顺序）
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

## 第 7 步：了解皮肤文件夹布局

以自带皮肤为参考，皮肤根目录包含：

- `Graphics/`：按编号的界面文件夹分组的图像（`0_Startup`、`1_Title`、`2_Config`、`3_DaniSelect`、`5_Game`、`6_Result`、`7_DanResult`、`7_Exit`、`8_TowerResult`、`10_Heya`、`12_OnlineLounge`、`13_TowerSelect`、`15_OpenEncyclopedia`），加上顶层的几张共享图像。动画背景是位于其所属文件夹图像旁边的 `Script.lua` 文件（例如 `Graphics/0_Startup/Script.lua` 和 `Graphics/5_Game/5_Background/` 下的文件夹）。
- `Sounds/`：游戏按固定文件名加载的系统声音和 BGM，例如 `Sounds/Move.ogg`、`Sounds/Decide.ogg`、`Sounds/Cancel.ogg`、`Sounds/BGM/Title.ogg`、`Sounds/BGM/SongSelect.ogg`、`Sounds/BGM/Result.ogg`。如果某个文件缺失，该声音就不播放。
- `Fonts/`：`FontName` 键引用的 `.ttf` 文件。
- `Locales/`：每种语言一个 JSON 文件（`en.json`、`ja.json`、...），形如 `{ "Entries": { "KEY": "text" } }`。这些字符串为皮肤自己的设置提供标签，Lua 通过 `THEME:GetSkinString(key)` 读取它们。当前语言中缺失某个键时，游戏会在 `DefaultLocale` 文件中查找它。
- `Modules/`：Lua 模块树（第 8 步）。
- `ThemeSettings.json`：选项界面在“主题设置”下显示的设置数组。每个条目有 `id`、`type`（`bool`、`int`、`double`、`string` 或 `enum`）、`scope`（`global`，默认，或 `save` 表示每个存档一个值）、本地化的 `label` 和 `description`、`default`，以及视类型而定的 `min`/`max` 或 `options`。
- `SkinConfig.ini` 及其包含的 `*Config.ini` 文件。
- `README.txt`、`LICENSE.md`、`Licenses/`：署名文件。游戏不读取它们。

```
My New Skin/
  SkinConfig.ini
  ThemeSettings.json
  Graphics/           按界面分组的图像；部分文件夹带有背景 Script.lua
  Sounds/             固定文件名的 .ogg 系统声音和 BGM/
  Fonts/              由 FontName 键指定的 .ttf 文件
  Locales/            en.json、ja.json、...（{ "Entries": { ... } }）
  Modules/            Lua 模块树（第 8 步）
  <screen>Config.ini  通过 #include 引入的布局文件
```

## 第 8 步：Modules 树与游戏要求的模块

皮肤加载时，游戏扫描 `Modules/` 的四个子文件夹，并把其中的每个直接子文件夹视为一个入口文件为 `Script.lua` 的模块：

- `Modules/Transitions/`：在舞台之间播放的过渡。游戏最先加载它们，以便首次切换舞台时已就绪。
- `Modules/Stages/`：完整界面。你通过 `Exit("stage", "<folder name>")` 进入舞台。
- `Modules/Activities/`：叠加在舞台上的可复用子界面（例如 `confirm_dialog`、`mod_select_dialog`、`song_select_core`）。
- `Modules/ROActivities/`：由游戏直接驱动的只读覆盖层。

游戏不扫描 `Modules/Lib/`。你通过 `require` 加载其中的文件：模块的搜索路径是其自己的文件夹，其次是 `Modules/Lib/`，因此 `require("dialogue")` 解析为 `Modules/Lib/dialogue.lua`。你也可以把舞台和活动放在游戏安装目录的 `Global/Stages/` 和 `Global/Activities/` 下；游戏会为每个皮肤加载那些。

在每个分类内，游戏先创建所有模块，然后对每个模块运行 `onStart`，顺序为 Transitions、Stages、Activities、ROActivities。

游戏按名称查找以下模块，自带皮肤提供了全部：

- 舞台 `_boot` 和 `_title`。任一缺失时游戏会报错停止。
- 只读活动 `modal`、`config_ui`、`nameplate`、`popup_menu`、`modicons`、`song_enum` 和 `danplate`。
- 过渡 `default` 和 `song_loading`。`song_loading` 在游戏加载歌曲时播放；`Exit` 未指定过渡或指定的过渡不存在时，游戏使用 `default`。完全没有过渡模块的皮肤回退为纯黑色淡入淡出。

构建皮肤时请保留所有这些模块；把你自己的模块添加在它们旁边。

```
Modules/
  Transitions/   <name>/Script.lua   （最先加载；游戏使用 "default" 和 "song_loading"）
  Stages/        <name>/Script.lua   （"_boot" 和 "_title" 必需）
  Activities/    <name>/Script.lua
  ROActivities/  <name>/Script.lua   （modal、config_ui、nameplate、popup_menu、modicons、song_enum、danplate 必需）
  Lib/           通过 require 访问的共享 .lua 文件，不会被扫描
```

## 第 9 步：舞台的 Script.lua 及其生命周期

`Script.lua` 在游戏创建模块时运行一次，此时引擎全局对象（`TEXTURE`、`SOUND`、`INPUT`、`CONFIG`、`THEME` 等）已定义。游戏随后按名称查找全局函数并调用它们。对舞台而言：

- `onStart()`：皮肤加载时调用一次。以协程方式运行，因此耗时的加载可以调用 `coroutine.yield()` 或 `LOADING` 辅助对象，把工作分散到加载进度条后面的多帧中。
- `activate()`：每次游戏进入舞台时调用。同样是协程。游戏会在它运行前刷新 `CHARACTERLIST` 和 `PUCHICHARALIST` 全局对象，因此依赖它们的内容请在这里构建；在 `onStart` 中它们仍然为空。
- `update(timestamp)`：每帧调用。返回 `Exit(target, name, transition)` 以离开舞台。`target` 为 `"title"`、`"play"`、`"stage"`（`name` = 舞台文件夹）或 `"legacy"`（`name` = `heya`、`config`、`exit` 或 `onlinelounge`）；`transition` 是 `Modules/Transitions/` 下的文件夹，默认为 `default`。
- `draw()`：每帧调用。
- `deactivate()`：游戏离开舞台时调用。
- `afterSongEnum()`：歌曲列表枚举完成时调用。
- `onDestroy()`：游戏拆除皮肤时调用。

它们全部是可选的；游戏会跳过你未定义的函数。活动、只读活动和过渡遵循相同的模式，各有自己的钩子集合。

```lua
-- Modules/Stages/my_stage/Script.lua
function onStart()
  -- 一次性初始化；耗时加载期间可以 coroutine.yield()
end

function activate()
  -- 每次进入舞台时运行
end

function update(ts)
  if INPUT:Pressed("Cancel") then
    return Exit("stage", "_title")   -- 离开此舞台
  end
  return nil
end

function draw()
  -- 每帧渲染
end

function deactivate() end
function afterSongEnum() end
function onDestroy() end
```

## 第 10 步：用 lang/ 本地化模块

模块可以把自己的翻译放在 `Script.lua` 旁边的 `lang/` 子文件夹中。由于模块自己的文件夹在其 `require` 路径上，`require("lang.ja")` 会解析为 `lang/ja.lua`。自带皮肤通过辅助文件 `Modules/Lib/i18n.lua` 为其较大的舞台这样做（例如 `Modules/Stages/myroom/lang/ja.lua` 和 `Modules/Stages/intro_nokon/lang/ja.lua`）。这与第 7 步中皮肤范围的 `Locales/` 文件夹是分开的。

```
Modules/Stages/my_stage/
  Script.lua
  lang/
    ja.lua        -- require("lang.ja")
```

## 第 11 步：安装并选择皮肤

把文件夹放到 `System/` 下。打开设置，进入“外观”分区，从“皮肤”选项中挑选皮肤；选择器按文件夹名称列出每个有效皮肤，并把其 `Graphics/1_Title/Background.png` 显示为缩略图。你更改皮肤时，游戏会拆除当前皮肤、加载新皮肤，并在加载进度条后面重新加载它的所有 Lua 模块。

游戏把选择以 `SkinPath=` 写入 `Config.ini`，相对于 `System/`。它写入的是纯文件夹名称（例如 Windows 上为 `SkinPath=My New Skin\`），也接受文件注释中显示的 `./My New Skin/` 形式。

```ini
; 在 Config.ini 中（在游戏内挑选皮肤时写入）：
; 皮肤文件夹路径，相对于 System/
SkinPath=My New Skin\
```

## 故障排除与注意事项

- 选择器没有列出皮肤：`Graphics/1_Title/Background.png` 缺失，或文件夹不直接位于 `System/` 下。
- 选择皮肤后游戏立刻报错：缺少必需的模块（第 8 步），或其中某个模块抛出了 Lua 错误。请通过切换到皮肤来测试它。
- 某个 `SkinConfig.ini` 键不起作用：键拼写错误、该行包含多于一个 `=`，或值解析失败。解析器会忽略未知的键且不报告。
- `Resolutions=` 只显示 `1`：列表使用了分号（注释标记），或所有值都在 0 < 值 <= 1 之外。
- 字体键不起作用：文件路径相对于皮肤根目录不存在。
- `CHARACTERLIST` 或 `PUCHICHARALIST` 在 `onStart` 中为空：游戏在创建模块之后才填充它们。请在 `activate` 中使用。
- 重命名皮肤文件夹会改变其身份；`Config.ini` 中的 `SkinPath` 必须指向新名称。
- `Name=`、`Version=` 和 `Creator=` 仅供参考。游戏不会基于它们做兼容性检查。
