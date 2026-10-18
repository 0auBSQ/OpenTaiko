<!-- modules/characters.md -->

# Characters

## Characters.Student

玩家操控的剧情学生（A 或 B）以及该学生的调色板，让每个剧情场景都以相同的颜色绘制同一个学生。这两个学生是 `Global/Characters/` 中的角色 `02v2 - Student (A)` 和 `03v2 - Student (B)`。

所有内容都保存在存档中：

| 存档键 | 类型 | 内容 |
| --- | --- | --- |
| `story_student` | 计数器 | 选中的学生：1 为 B；任何其他值（包括未设置）都视为 A。 |
| `.character_palette_<folder>` | 计数器 | 学生角色的从 0 起的调色板索引，与角色自定义界面写入的是同一个计数器，因此两者始终一致。A 和 B 各有自己的值。 |
| `.character_playcount_<folder>` | 计数器 | 使用该角色的演奏次数，用于解锁调色板。 |

学生的调色板是角色的 `Palettes.json`，后面接上模块自带副本（`Global/Modules/Lib/Characters/Student/<id>/Palettes.json`）中角色文件没有的调色板（按 `name` 匹配）。没有调色板的角色，或者没有安装的角色，仍然拥有副本中的调色板。超出列表范围的索引视为默认调色板 0。

| 函数 | 说明 |
| --- | --- |
| `Student.current(save)  -> "a" or "b"` | 存档选中的学生。 |
| `Student.ensure(save)  -> "a" or "b"` | 同上，但计数器中没有学生时写入 A。 |
| `Student.set(save, id)  -> id` | 选择一个学生。 |
| `Student.folder(id)  -> string` | 学生的角色文件夹名。 |
| `Student.index(save, id)  -> number` | 学生的调色板索引，总是有效值。 |
| `Student.setIndex(save, id, idx)  -> number` | 保存一个调色板索引；如果该角色就是存档当前装备的角色，其实际调色板也会改变。 |
| `Student.palettes(id)  -> table` | 调色板列表：`{ name, plays, blend, stops }` 条目，条目 `i` 对应索引 `i - 1`；文件提供翻译时，`name` 使用游戏的语言。 |
| `Student.count(id)`, `Student.entry(id, idx)`, `Student.clamp(id, idx)` | 调色板数量、单个条目、修正为有效值的索引。 |
| `Student.unlocked(save, id, idx)  -> bool` | 该调色板是否已开放：没有演奏次数要求，或使用该角色的演奏次数足够。 |
| `Student.gradient(id, idx)  -> gradient` | 以 `GRADIENT` 映射返回该调色板；没有颜色的调色板为 nil。 |
| `Student.withPalette(id, idx, fn, ...)` | 在该调色板生效的情况下运行 `fn`（2D 绘制）。返回 pcall 的结果。 |
| `Student.registerSprite(scene, spriteId, tex, id, idx)  -> bool` | 把 `tex` 注册为 3D 场景的公告板精灵，并按该调色板重新着色。 |
| `Student.reload()` | 丢弃已解析的调色板文件，下次查询时重新读取。 |

`palettes`、`count`、`entry`、`clamp`、`gradient` 和 `registerSprite` 接受一个可选的最后参数 `visitor`：为 true 时得到另一位玩家的学生（在线访客）的列表，与玩家自己的列表分开缓存。

```lua
local Student = require("Characters.Student")
local save = GetSaveFile(0)
local id  = Student.current(save)
local idx = Student.index(save, id)
Student.withPalette(id, idx, function() tex:Draw(x, y) end)
```
