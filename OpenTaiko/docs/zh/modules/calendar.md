<!-- modules/calendar.md -->

# Calendar

## Calendar.almanac

用于随日历出现和消失的活动的日期模式：圣诞节的精选商品、愚人节的场景、每月的奖励。日期按日本标准时间（UTC+9）计算，与玩家的时钟无关，因此所有玩家看到的是同一天。模式可以写成不透明的 `enc:` 令牌，这样惊喜就不会出现在源码中。

| 模式 | 匹配 |
| --- | --- |
| `"2026-06-15"` | 那一天。 |
| `"2026-06-15+30"` | 那一天及其后的 29 天。 |
| `"2026-06-15..2026-07-31"` | 该区间，包含两端（两端都要完整写出）。 |
| `"*-06-15"`, `"*-06-15+7"` | 每年的那一天，或那一周。 |
| `"*-*-01"`, `"2026-*-13"` | 每月 1 日；2026 年的每个 13 日。 |
| `"*-06-*"`, `"2026-12-*"` | 每年的一整个月；某一个月。 |
| `{ "...", "..." }` | 多个模式中的任意一个。 |

条目是一张表 `{ code, when, priority, sticky }`：`when` 匹配期间它处于激活状态。`sticky = true` 使它在某个存档上匹配过一次后，通过该存档上的触发器 `.almanac_<code>` 对这个存档保持激活。`code` 本身也可以是一个 `enc:` 令牌。

| 函数 | 说明 |
| --- | --- |
| `Almanac.matches(spec)  -> bool` | 今天是否匹配某个模式、令牌或它们的列表。 |
| `Almanac.active(entry, save?)  -> bool` | 某个条目今天是否激活；传入存档时，匹配过一次的 sticky 条目会保持激活，并且一次匹配会设置其触发器。 |
| `Almanac.pick(entries, save?, skip?)  -> entry` | `priority` 最高的激活条目（平局时取第一个），或 nil。`skip(entry)` 返回 true 时排除该条目（例如玩家已经拥有的条目）。 |
| `Almanac.encrypt(pattern)  -> string` | 某个模式或代码的 `enc:` 令牌。 |
| `Almanac.decrypt(token)  -> string` | 令牌的文本；不是有效令牌时为 nil。 |
| `Almanac.plain(s)  -> string` | 解密后的令牌，或原样的 `s`。 |
| `Almanac.parse(spec)  -> table` | 解析后的单个模式（格式错误时为 nil），供 `matchesPattern` 使用。 |
| `Almanac.matchesPattern(p)  -> bool` | 今天是否匹配一个已解析的模式。 |

```lua
local Almanac = require("Calendar.almanac")
local EVENTS = {
    { code = "winter", when = "*-12-20+12", priority = 80 },
    { code = "spring", when = "*-03-20+14", priority = 50, sticky = true },
}
local now = Almanac.pick(EVENTS, GetSaveFile(0))
if now ~= nil then banner = now.code end
```
