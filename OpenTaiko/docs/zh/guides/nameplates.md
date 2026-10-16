<!-- guides/nameplates.md -->

# 添加名牌

名牌称号是显示在玩家名牌上的文字牌，与玩家名称和段位称号一起显示。玩家通过演奏、购买或由脚本授予来解锁称号，并在我的房间中装备其中一个。你可以用名牌包添加自己的称号：名牌包是一个包含 `Nameplates.json` 文件的文件夹，放在 `Global/Nameplates/` 中。皮肤也可以带有自己的名牌包。在每个显示名牌的界面中，游戏都会把每个名牌包的名牌与自带的名牌列在一起。

## 开始之前

- 一个能保存为 UTF-8 的文本编辑器。
- 一个名牌包 id：由小写字母、数字和 `_` 组成、其他名牌包都没有使用的短名称，例如你的名字或你的谱面包名称。游戏以它为基础构建包内每个名牌的 id。
- 游戏在启动时和加载皮肤时读取名牌包。每次修改后请重启游戏以查看效果。

## 第 1 步：创建名牌包文件夹

在游戏旁边的 `Global/Nameplates/` 中创建一个文件夹。文件夹名称可以任意取。名牌包包含一个必需的文件，以及一个用于翻译的可选 `Lang/` 文件夹（第 3 步）：

```
Global/
  Nameplates/
    Example Pack/
      Nameplates.json
      Lang/
        fr.json
        ja.json
```

皮肤以同样的方式带上自己的名牌包：在皮肤根目录放一个 `Nameplates/` 文件夹（`System/<skin>/Nameplates/Nameplates.json`）。游戏只加载当前皮肤的名牌包，因此在其他皮肤处于激活状态时，皮肤名牌包中的名牌会消失。

## 第 2 步：编写 Nameplates.json

`Nameplates.json` 包含名牌包 id 和名牌列表：

- `pack`（string，必需）：名牌包 id。
- `name`（object，可选）：名牌包的名称，显示在我的房间电脑的名牌包选择栏上，写法为 `{ "strings": { "default": "...", "<code>": "..." } }`：选择栏显示当前语言的名称，没有时显示 `default`。没有它时，名牌包选择栏显示名牌包 id。
- `nameplates`（array，必需）：每个名牌一个对象，包含以下字段：
  - `key`（string，必需）：名牌的名称，在包内唯一。名牌的完整 id 是 `<pack>:<key>`，例如 `example_pack:night_sky`。存档会保存这个 id，因此一旦玩家可以获得该名牌，就不要再更改它的键，也永远不要把这个键用于其他名牌。
  - `title`（string，必需）：称号文本。没有翻译的每种语言都显示它。
  - `type`（int，默认 0）：牌的美术，即皮肤名牌纹理中的文件夹编号。自带皮肤中的文件夹是 `Modules/ROActivities/nameplate/Textures/Title/0` 到 `45`，没有对应文件夹的编号不绘制牌。
  - `rarity`（string，默认 `Common`）：稀有度名称（见下表）。未知的名称变为 `Common`。
  - `unlock`（object，可选）：解锁条件，字段与 `Unlock.json` 相同：`condition`、`values`、`type` 和 `references`（条件 id 见[谱面解锁条件](unlockables.md)指南）。没有 `unlock` 时，每个玩家从一开始就拥有该名牌。

稀有度值（星数为自带皮肤的设定）：

| 稀有度 | 牌上的星数 | 通知等级 |
| --- | --- | --- |
| <span class="rarity rarity-poor">Poor</span> | 0 | 0 |
| <span class="rarity rarity-common">Common</span> | 0 | 0 |
| <span class="rarity rarity-uncommon">Uncommon</span> | 0 | 1 |
| <span class="rarity rarity-rare">Rare</span> | 1 | 2 |
| <span class="rarity rarity-epic">Epic</span> | 2 | 3 |
| <span class="rarity rarity-legendary">Legendary</span> | 3 | 4 |
| <span class="rarity rarity-mythical">Mythical</span> | 4 | 4 |

游戏在每次演奏后于结算界面检查解锁条件。玩家满足某个条件时，游戏解锁该名牌并显示一条通知。金币条件（`ch`、`cs`、`cm`）只能通过购买解锁：在自带皮肤中，玩家在我的房间的电脑上购买名牌。在玩家拥有之前，我的房间的电脑也不会在列表中显示锁定的 Mythical 名牌。

```json
{
  "pack": "example_pack",
  "name": { "strings": { "default": "Example Pack", "fr": "Pack d'exemple" } },
  "nameplates": [
    {
      "key": "night_sky",
      "title": "Night Sky Walker",
      "type": 8,
      "rarity": "Rare",
      "unlock": { "condition": "tp", "values": [50] }
    },
    {
      "key": "hello",
      "title": "Hello There"
    }
  ]
}
```

## 第 3 步：添加翻译

翻译是可选的。名牌包 `Lang/` 文件夹中的每个文件存放一种语言的称号，每个名牌一行，使用包内的键（不带名牌包 id）。文件名是语言代码：

| 文件 | 语言 |
| --- | --- |
| `de.json` | 德语 |
| `en.json` | 英语 |
| `es.json` | 西班牙语 |
| `fr.json` | 法语 |
| `ja.json` | 日语 |
| `ko.json` | 韩语 |
| `nl.json` | 荷兰语 |
| `ru.json` | 俄语 |
| `zh.json` | 中文 |

```json
{
  "night_sky": "Promeneur du ciel nocturne",
  "hello": "Bonjour"
}
```

某种语言的文件中缺少的名牌，在该语言中显示其 `title`。

## 第 4 步：在游戏中测试

启动游戏并打开我的房间的电脑。在“名牌”选项卡中，从列表上方的名牌包选择栏选择你的名牌包：列表会显示该名牌包的名牌，锁定的名牌带有解锁提示。装备其中一个，然后在玩家的名牌上检查它的称号、牌和星数。切换游戏语言以检查翻译。

游戏会跳过无法读取的名牌包，并显示一条写明该名牌包的错误通知。没有有效 `pack` id 的名牌包，或使用了游戏已加载的名牌包 id 的名牌包，都算作无法读取。对于缺少 `key` 或 `title`、或者键在包内已经存在的单个名牌，游戏会跳过它，并把原因写入 `OpenTaiko.log`。

## 从脚本使用名牌

Lua 脚本使用相同的 id。每个接受名牌 id 的方法都接受字符串形式的完整 id；自带的名牌也接受其编号。脚本可以授予带有 `ig` 条件的名牌，演奏永远不会解锁这种名牌：

```lua
local save = GetSaveFile(0)
local id = "example_pack:night_sky"
if not save:IsNameplateUnlocked(id) then
    save:UnlockNameplate(id)
end
save:ChangeNameplate(id)
```

UnlockNameplate 不显示通知，因此需要通知的脚本要自己显示。名牌相关的方法见[玩家与档案](../api/players.md)。

## 示例

这些名牌包展示了常见的模式。请把其中的 id、称号和数字换成你自己的。

**免费名牌。** 没有 `unlock` 时，每个玩家都拥有该名牌，并可以立即装备它。

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "welcome", "title": "Welcome Aboard", "type": 0, "rarity": "Common" }
  ]
}
```

**通过演奏解锁（`tp`）。** 名牌在演奏 100 次后解锁。`ap`（AI 对战次数）、`aw`（AI 对战胜利次数）和 `ce`（累计获得的金币）使用相同的单值布局。

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "regular", "title": "Regular Drummer", "type": 3, "rarity": "Uncommon",
      "unlock": { "condition": "tp", "values": [100] } }
  ]
}
```

**过关你的谱面包中的歌曲（`sp`）。** 谱面包可以为自己的歌曲设置奖励。引用是该歌曲 `uniqueID.json` 中的 `id` 字段，`[3, 3]` 要求在 Extreme（3）上达成全连击（3）。

```json
{
  "pack": "example_charts",
  "nameplates": [
    { "key": "storm_fc", "title": "Storm Chaser", "type": 33, "rarity": "Epic",
      "unlock": { "condition": "sp", "values": [3, 3],
                  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"] } }
  ]
}
```

**用金币购买（`ch`）。** 价格是唯一的值。在自带皮肤中，玩家在我的房间的电脑上购买名牌。

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "golden", "title": "Golden Touch", "type": 11, "rarity": "Rare",
      "unlock": { "condition": "ch", "values": [800] } }
  ]
}
```

**只由脚本授予的奖励（`ig`）。** 演奏永远不会解锁 `ig` 名牌，因此由脚本用 UnlockNameplate 授予它（见“从脚本使用名牌”）。在此之前，我的房间的电脑为它显示通用的无效条件提示。

```json
{
  "pack": "example_story",
  "nameplates": [
    { "key": "finale", "title": "Curtain Call", "type": 19, "rarity": "Legendary",
      "unlock": { "condition": "ig", "values": [] } }
  ]
}
```

**带翻译的皮肤名牌包。** 皮肤把名牌包放在 `System/<skin>/Nameplates/`，把翻译放在 `System/<skin>/Nameplates/Lang/`。

```json
{
  "pack": "example_skin",
  "nameplates": [
    { "key": "visitor", "title": "First Visit", "type": 22, "rarity": "Common",
      "unlock": { "condition": "tp", "values": [1] } }
  ]
}
```

```json
{
  "visitor": "初めての訪問"
}
```

## 故障排除与注意事项

- 名牌包没有出现：文件夹不是直接位于 `Global/Nameplates/` 中，文件名不是 `Nameplates.json`，`pack` id 缺失或使用了小写字母、数字和 `_` 以外的字符，或者其他名牌包已经使用了相同的 id。错误通知会写明该名牌包。
- 某个名牌不在列表中：它没有 `key` 或 `title`，或者它的键与前面的某个键重复。`OpenTaiko.log` 会写明该条目。
- 翻译没有显示：`Lang` 文件中的键必须是去掉名牌包 id 的包内键（`example_pack:night_sky` 对应 `night_sky`），并且文件名必须是语言代码。
- 牌上没有美术：皮肤没有该 `type` 的文件夹。
- 在线时，其他玩家只有在拥有相同的名牌包时才能看到名牌包名牌的称号。否则，他们看到的是没有称号的玩家名牌。
- 如果你移除了一个名牌包，装备过其中名牌的玩家在名牌包恢复之前不显示称号。他们的解锁记录保留在存档中。
- 游戏的 JSON 解析器接受注释和尾随逗号；严格的校验器会拒绝它们。
