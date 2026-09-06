<!-- guides/unlockables.md -->

# 为自定义谱面添加解锁条件

在歌曲文件夹中、`.tja` 和 `uniqueID.json` 旁边放置一个 `Unlock.json` 文件，即可把自定义歌曲锁在某个条件之后（金币价格、过关其他歌曲、总游玩次数、剧情标志等）。游戏构建歌曲列表时会读取该文件，并在玩家满足条件之前保持歌曲锁定。条件附加在单首歌曲上：`box.def` 没有解锁键，因此你无法用这种方式锁定分类文件夹。

## 开始之前

- 一张已经出现在歌曲列表中的自定义谱面：`Songs/` 下的一个文件夹，包含 `.tja`，并且在游戏扫描过之后有一个 `uniqueID.json`。
- 一个能保存为 UTF-8 的文本编辑器。
- 对于引用其他歌曲的条件：每首被引用歌曲的 `uniqueID.json` 中的 `id` 值。
- 游戏按存档存储解锁进度，因此你必须在游戏中满足条件才能看到歌曲解锁。你测试时，游戏设置中的 `Ignore Song Unlockables` 选项会把每首歌曲都视为已解锁。

## 第 1 步：创建 Unlock.json 文件

在歌曲文件夹中创建 `Unlock.json`。每个字段都是可选的并有默认值：

- `hidden_index`（int，默认 0）：歌曲列表呈现锁定歌曲的方式（见下表）。游戏会把值限制在 0-3。
- `rarity`（string，默认 `Common`）：稀有度名称（见下表）。它决定稀有度显示的颜色和解锁通知的等级。空值变为 `Common`。
- `condition`（string，默认 `ch`）：条件 id（第 2 步）。
- `values`（int 数组，默认 `[100]`）：条件的数值参数。
- `type`（string，默认 `me`）：基于数值的条件所使用的比较方式：`l` 小于，`le` 小于等于，`e` 等于，`me` 大于等于，`m` 大于，`d` 不等于。金币条件始终使用 `me`。
- `references`（string 数组，默认 `[""]`）：歌曲 id、分类名称、谱师名称或标志名称，取决于条件。
- `custom_unlock_text`（object，可选）：替换生成的提示文本。一个本地化对象 `{ "strings": { "default": "...", "ja": "..." } }`；游戏使用当前语言的键，然后是 `default`，两者都不存在时显示生成的文本。

键为带下划线的小写，与列出的完全一致。

hidden_index 的值：

| 值 | 在歌曲列表中 |
| --- | --- |
| 0 | 显示并带锁图标；播放试听音频 |
| 1 | 灰显；不试听 |
| 2 | 灰显，标题和预览图片被遮挡 |
| 3 | 解锁前隐藏 |

稀有度值：

| 稀有度 | 通知等级 |
| --- | --- |
| <span class="rarity rarity-poor">Poor</span> | 0 |
| <span class="rarity rarity-common">Common</span> | 0 |
| <span class="rarity rarity-uncommon">Uncommon</span> | 1 |
| <span class="rarity rarity-rare">Rare</span> | 2 |
| <span class="rarity rarity-epic">Epic</span> | 3 |
| <span class="rarity rarity-legendary">Legendary</span> | 4 |
| <span class="rarity rarity-mythical">Mythical</span> | 4 |

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "cm",
  "values": [500],
  "type": "me",
  "references": [""],
  "custom_unlock_text": {
    "strings": {
      "default": "Buy this song for 500 coins.",
      "ja": "500コインで解放できます。"
    }
  }
}
```

## 第 2 步：选择条件

| Id | 含义 | `values` | `references` |
|----|---------|----------|--------------|
| `ch`、`cs`、`cm` | 金币购买。游戏对三个 id 的判定完全相同（价格为金币数，`type` 强制为 `me`，永远不会自动授予）。`cm` 是面向歌曲的 id；皮肤脚本可以读取该 id，并据此决定在哪里提供购买。 | `[price]` | 未使用 |
| `ce` | 自存档创建以来累计获得的金币 | `[coins]` | 未使用 |
| `tp` | 总游玩次数 | `[plays]` | 未使用 |
| `ap` | AI 对战次数 | `[plays]` | 未使用 |
| `aw` | AI 对战胜利次数 | `[wins]` | 未使用 |
| `sd` | 达到某过关状态的不同谱面数 | `[chart count, clear status]` | 未使用 |
| `dp` | 某一难度下达到某过关状态的谱面数 | `[difficulty, clear status, chart count]` | 未使用 |
| `lp` | 某一星级下达到某过关状态的谱面数 | `[level, clear status, chart count]` | 未使用 |
| `sp` | 指定歌曲达到某过关状态 | 每首歌曲 `[difficulty, clear status]`（`-1` = 任意难度） | 每对值一个歌曲 id |
| `sg` | 指定分类内的歌曲达到某过关状态 | 每个分类 `[song count, clear status]` | 每对值一个分类名称 |
| `sc` | 指定谱师的谱面达到某过关状态 | 每位谱师 `[chart count, clear status]` | 每对值一个谱师名称 |
| `gt` | 全局触发器（存档中由脚本设置的具名开关标志） | `[1]` 表示 ON，`[0]` 表示 OFF | `[trigger name]` |
| `gc` | 全局计数器（存档中由脚本设置的具名数值） | `[value]`，用 `type` 比较 | `[counter name]` |
| `ig` | 无法获得：永不解锁 | 无 | 无 |
| `andcomb` | 玩家必须满足所有子条件（见组合条件的示例） | `[]` | 每个条目一个子条件，以 JSON 字符串表示 |
| `orcomb` | 玩家必须满足至少一个子条件（见组合条件的示例） | `[]` | 每个条目一个子条件，以 JSON 字符串表示 |

过关状态值（状态要求表示此状态或更好）：

| 值 | 过关状态 |
| --- | --- |
| 0 | 已游玩 |
| 1 | 辅助过关 |
| 2 | 过关 |
| 3 | 全连击 |
| 4 | 全良 |

难度值：

| 值 | 难度 |
| --- | --- |
| 0 | Easy |
| 1 | Normal |
| 2 | Hard |
| 3 | Extreme |
| 4 | Extra Extreme |

对 `dp`，难度 3 同时计入 Extra Extreme 谱面。`dp` 和 `lp` 只计入常规谱面，并跳过段位和塔的谱面。`sp` 接受 `-1` 表示任意难度。

游戏会检查值的数量：`ch`/`cs`/`cm`/`ce`/`tp`/`ap`/`aw`/`gt`/`gc` 需要恰好 1 个值，`sd` 恰好 2 个，`dp`/`lp` 恰好 3 个，`sp`/`sg`/`sc` 每个引用需要 2 个值，且引用数量与值对数量相同。数量错误会使条件失败，并在游戏中显示错误消息。

只有在提供购买的界面中显式购买才能解锁金币条件（自带皮肤在选曲界面提供锁定歌曲的购买，无论你使用哪个金币 id）。游戏在每次演奏后于结算界面检查其他所有条件；玩家满足某个条件时，游戏把歌曲 id 加入存档并显示一条通知。

## 第 3 步：在游戏中测试

启动游戏并打开选曲界面。根据 `hidden_index`，歌曲会显示锁、灰显、被遮挡或不出现。购买它或满足条件，然后确认重启后它仍保持解锁。在游戏设置中打开 `Ignore Song Unlockables` 可以在你迭代时绕过所有锁定。

## 示例

以下是自带歌曲所使用的条件模式，写成 `Unlock.json` 文件。请把其中的 id、名称和数字换成你自己的。

**用金币购买歌曲（`cm`）。** 最常见的模式。价格是唯一的值，游戏会忽略 `type`。`hidden_index` 0 让歌曲保持可见，玩家可以找到并购买它。生成的提示已经说明了价格，因此你不需要 `custom_unlock_text`。

```json
{
  "hidden_index": 0,
  "rarity": "Uncommon",
  "condition": "cm",
  "values": [500]
}
```

**总游玩次数（`tp`）。** 自带的章节以递增的游玩次数（10、15、20，依此类推）逐首开放歌曲，因此玩家总有下一首歌曲触手可及。`ap`（AI 对战次数）、`aw`（AI 对战胜利次数）和 `ce`（累计获得的金币）使用相同的单值布局。

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "tp",
  "values": [50]
}
```

**过关指定歌曲（`sp`）。** 大多数自带歌曲用于续作或混音版的模式。难度为 `-1` 时接受任意难度的过关；id 是被引用歌曲 `uniqueID.json` 中的 `id` 字段（游戏第一次扫描到没有 id 的歌曲时会生成一个 64 个字符的 id，自带歌曲可能带有手写的 id）。示例在玩家过关被引用的歌曲后解锁。

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "sp",
  "values": [-1, 2],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"]
}
```

**全连击多首歌曲（`sp`）。** 每增加一首歌曲就增加一对值和一个引用，玩家必须满足每首被引用的歌曲。示例要求在两首歌曲上达成全连击（3）。

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sp",
  "values": [-1, 3, -1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU", "4aJ2I19XyEG2cEA9tlmdnZSz2H43OKsVBPLi52UfRhSjDGNgTGGqhmqkbWfPDoyw"]
}
```

**过关某分类的歌曲（`sg`）。** 每个条目是 `[song count, clear status]`，分类名称放在 `references` 中。分类是玩家演奏歌曲时游戏记录的那个：所在文件夹 `box.def` 的 `#GENRE`，文件夹没有时则是谱面自己的 `#GENRE`。自带的章节将它用于各自的组曲。示例在玩家过关该分类中的 10 首不同歌曲后解锁。

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sg",
  "values": [10, 2],
  "references": ["OpenTaiko Chapter II"]
}
```

**过关某位谱师的谱面（`sc`）。** 相同的布局，以 `#NOTESDESIGNER` 名称作为引用。示例要求过关一位谱师的 5 张谱面；第二位谱师则再增加一对值和一个引用。

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sc",
  "values": [5, 2],
  "references": ["bol"]
}
```

**过关一定数量的不同谱面（`sd`）。** 计入存档中达到给定状态或更好的每一张谱面。示例要求 10 张谱面达成全连击。

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sd",
  "values": [10, 3]
}
```

**过关某一难度或星级的谱面（`dp` / `lp`）。** `dp` 计入某一难度的谱面，`lp` 计入某一星级的谱面。第一个示例要求过关 20 张 Extreme 谱面，第二个要求过关 1 张 7 星谱面。

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "dp",
  "values": [3, 2, 20]
}
```

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "lp",
  "values": [7, 2, 1]
}
```

**两个条件满足其一（`orcomb`）。** `values` 为空，`references` 的每个条目是一个写成 JSON 字符串的完整子条件（`condition`、`type`、`values`、`references`）。自带歌曲用它提供一条捷径：演奏足够多的歌曲，或者付费。`orcomb` 在为购买定价时使用最便宜的已满足分支。

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "orcomb",
  "values": [],
  "references": [
    "{\"condition\": \"tp\", \"type\": \"me\", \"values\": [100]}",
    "{\"condition\": \"cm\", \"type\": \"me\", \"values\": [200]}"
  ]
}
```

**同时满足多个条件（`andcomb`）。** 相同的布局；玩家必须满足每个子条件。子条件本身也可以是组合，`andcomb` 会把组合内的金币价格相加。示例要求过关一首歌曲并过关任意一张 7 星谱面。

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "andcomb",
  "values": [],
  "references": [
    "{\"condition\": \"sp\", \"type\": \"me\", \"values\": [-1, 2], \"references\": [\"4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU\"]}",
    "{\"condition\": \"lp\", \"type\": \"me\", \"values\": [7, 2, 1], \"references\": [\"\"]}"
  ]
}
```

**带自定义提示的剧情标志（`gt`）。** `gt` 从存档读取一个具名的开关标志。Lua 脚本（剧情场景、过场动画）设置这些标志，因此只在你控制的脚本会设置该标志时才使用 `gt`。`values` 为 `[1]` 表示 ON 或 `[0]` 表示 OFF；`references` 存放标志名称。剧情解锁没有不言自明的生成提示，因此 `custom_unlock_text` 在这里很有用。你可以提供任何语言键；`default` 是回退项。

```json
{
  "hidden_index": 3,
  "rarity": "Legendary",
  "condition": "gt",
  "values": [1],
  "references": ["story_done"],
  "custom_unlock_text": {
    "strings": {
      "default": "Finish the story to unlock this song.",
      "ja": "ストーリーをクリアするとこの曲が解放されます。"
    }
  }
}
```

**带谜语的隐藏歌曲。** 自带的隐藏歌曲把较高的 `hidden_index`（标题和预览保持被遮挡，或者歌曲在解锁前一直不出现）与一个暗示而非说明条件的 `custom_unlock_text` 结合起来。底层可以使用任何条件；示例把全连击要求藏在一条谜语背后。

```json
{
  "hidden_index": 2,
  "rarity": "Epic",
  "condition": "sp",
  "values": [-1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"],
  "custom_unlock_text": {
    "strings": {
      "default": "Perfect the storm that came before this one."
    }
  }
}
```

## 故障排除与注意事项

- 歌曲没有被锁定：文件名不是 `Unlock.json`，它不在存放 `.tja` 和 `uniqueID.json` 的文件夹中，或者 `Ignore Song Unlockables` 已开启。
- 提示说条件无效：值的数量与条件不匹配（见第 2 步），或 `sp`/`sg`/`sc` 的引用数量与值对数量不同。
- 某个键不起作用：键为 `hidden_index`、`rarity`、`condition`、`values`、`type`、`references`、`custom_unlock_text`，全部小写。游戏会忽略其他任何键并使用默认值。
- `sp` 永不解锁：引用必须是目标歌曲 `uniqueID.json` 的 id（标题或文件名永远不会匹配），且玩家必须能够达到该难度和状态（状态 3 是全连击，因此玩家仅过关的谱面不计入）。
- `sg` 永不解锁：分类名称必须与游戏为所演奏歌曲记录的分类一致（文件夹 `box.def` 的 `#GENRE`，没有文件夹分类时为谱面的 `#GENRE`）。
- `gt`/`gc` 永不解锁：没有任何脚本设置该具名标志。谱面自己无法设置它。
- 游戏中没有任何东西能解锁 `ig`。只用于由其他系统授予的内容。
- `custom_unlock_text` 必须是 `{ "strings": { ... } }` 对象；裸字符串无法使用。
- 自带的 `Unlock.json` 文件包含尾随逗号。游戏的 JSON 解析器接受它们；严格的校验器会拒绝它们。
