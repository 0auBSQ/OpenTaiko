<!-- modules/songs.md -->

# Songs

## Songs.PlayStrings

舞台为它所启动的那一次演奏设置的共享字符串：要显示的加载背景、段位牌的样式，以及该次演奏的背景或只读活动会读取的任何内容。这些字符串只对盖章时的那张谱面给出值，因此舞台没有回来的那次演奏留下的值不会影响其他演奏。

| 函数 | 说明 |
| --- | --- |
| `PlayStrings.stamp()` | 把当前选中的谱面（`SONGMOUNT`）标记为这些字符串所属的谱面。请在挂载谱面之后、`Exit("play")` 之前调用。 |
| `PlayStrings.clear()` | 结束这个标记；演奏返回后调用。 |
| `PlayStrings.get(key)  -> string` | 选中的谱面就是已标记的谱面时返回共享字符串 `key`，否则返回 `""`。 |
| `PlayStrings.forTitle(key, title)  -> string` | 按谱面标题做同样的检查，供只知道标题的读取方（段位牌）使用。 |

值本身是普通的共享字符串，用 `SHARED:SetSharedString(key, value)` 设置。标记保存在共享字符串 `play_strings_chart` 中。

```lua
local PlayStrings = require("Songs.PlayStrings")

-- the stage, after mounting the chart
SHARED:SetSharedString("my_loading_art", "night")
PlayStrings.stamp()

-- a background script of that play
local art = PlayStrings.get("my_loading_art")   -- "night"; "" for any other chart
```
