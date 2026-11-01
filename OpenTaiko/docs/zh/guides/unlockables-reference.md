<!-- guides/unlockables-reference.md -->

# 解锁条件参考

歌曲、角色、小角色和名牌使用同一种解锁条件格式。歌曲、角色和小角色把它放在 `Unlock.json` 文件中，名牌则放在 `Nameplates.json` 中各自条目的 `unlock` 对象里。每个文件放在哪里，由[谱面解锁条件](unlockables.md)、[添加角色](characters.md)、[添加小角色](puchicharas.md)和[添加名牌](nameplates.md)这几篇指南说明。本页列出各字段、条件 id 以及它们使用的数值。

## 条件字段

| 字段 | 类型 | 含义 |
| --- | --- | --- |
| `condition` | string | 条件 id（见“条件 id”）。 |
| `values` | int 数组 | 条件的数值参数。 |
| `type` | string | 基于数值的条件所使用的比较方式（见“比较类型”）。 |
| `references` | string 数组 | 歌曲 id、分类名称、谱师名称或标志名称，取决于条件。 |

歌曲的 `Unlock.json` 还有自己的字段（`hidden_index`、`rarity`、`custom_unlock_text`），见[谱面解锁条件](unlockables.md)指南。

## 条件 id

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
| `andcomb` | 玩家必须满足所有子条件（见[谱面解锁条件](unlockables.md)指南中组合条件的示例） | `[]` | 每个条目一个子条件，以 JSON 字符串表示 |
| `orcomb` | 玩家必须满足至少一个子条件（见[谱面解锁条件](unlockables.md)指南中组合条件的示例） | `[]` | 每个条目一个子条件，以 JSON 字符串表示 |

游戏会检查值的数量：`ch`/`cs`/`cm`/`ce`/`tp`/`ap`/`aw`/`gt`/`gc` 需要恰好 1 个值，`sd` 恰好 2 个，`dp`/`lp` 恰好 3 个，`sp`/`sg`/`sc` 每个引用需要 2 个值，且引用数量与值对数量相同。数量错误会使条件失败，并在游戏中显示错误消息。

只有在提供购买的界面中显式购买才能解锁金币条件。游戏在每次演奏后于结算界面检查其他所有条件；玩家满足某个条件时，游戏解锁该项目并显示一条通知。

## 比较类型

| `type` | 比较方式 |
| --- | --- |
| `l` | 小于 |
| `le` | 小于等于 |
| `e` | 等于 |
| `me` | 大于等于（默认） |
| `m` | 大于 |
| `d` | 不等于 |

无论 `type` 为何值，金币条件始终使用 `me`。

## 过关状态值

状态要求表示此状态或更好。

| 值 | 过关状态 |
| --- | --- |
| 0 | 已游玩 |
| 1 | 辅助过关 |
| 2 | 过关 |
| 3 | 全连击 |
| 4 | 全良 |

## 难度值

| 值 | 难度 |
| --- | --- |
| 0 | Easy |
| 1 | Normal |
| 2 | Hard |
| 3 | Extreme |
| 4 | Extra Extreme |

对 `dp`，难度 3 同时计入 Extra Extreme 谱面。`dp` 和 `lp` 只计入常规谱面，并跳过段位和塔的谱面。`sp` 接受 `-1` 表示任意难度。

## 演奏 Mod 值

存档按谱面、难度和演奏 Mod 保存玩家的最佳成绩。演奏 Mod 值把一次演奏的 Mod 打包成 8 个字节，最低字节在前：没有 Mod 的演奏得到 8925478921。

| 字节 | Mod | 值 |
| --- | --- | --- |
| 1（最低） | 流速 | 0 到 99：9 为 1.0x，每级 0.1x |
| 2 | 隐身 | 0 Off、1 Doron、2 Stealth、3 Hidden、4 Flashlight |
| 3 | 随机 | 0 Off、1 Random、2 Mirror、3 SuperRandom、4 MirrorRandom |
| 4 | 歌曲速度 | 2 到 200：20 为 1.0x，每级 0.05x |
| 5 | 判定区间 | 0 Loose（宽松）、1 Lenient（较宽松）、2 Normal（普通）、3 Strict（严格）、4 Rigorous（极严） |
| 6 | 判定 Mod | 0 关闭、1 Just、2 Safe |
| 7 | 未使用 | 0 |
| 8（最高） | 趣味 Mod | 0 None、1 Avalanche、2 Minesweeper、3 Dynamic Beat |

特殊 Mod（Flawless、Timed）不包含在该值中。
