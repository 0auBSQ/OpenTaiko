<!-- modules/songs.md -->

# Songs

## Songs.PlayStrings

Shared strings that a stage sets for the one play it starts: the loading background to show, a dan plate's style, anything a background or an ROActivity of that play reads. The strings answer only for the chart they were stamped with, so values left behind by a play the stage never came back from reach no other play.

| Function | Description |
| --- | --- |
| `PlayStrings.stamp()` | Marks the chart chosen now (`SONGMOUNT`) as the one the strings belong to. Call it right after mounting the chart, before `Exit("play")`. |
| `PlayStrings.clear()` | Ends the stamp; call it when the play has come back. |
| `PlayStrings.get(key)  -> string` | The shared string `key` while the chosen chart is the stamped one, else `""`. |
| `PlayStrings.forTitle(key, title)  -> string` | The same check by chart title, for a reader that knows the title only (a dan plate). |

The values themselves are ordinary shared strings, set with `SHARED:SetSharedString(key, value)`. The stamp is kept in the shared string `play_strings_chart`.

```lua
local PlayStrings = require("Songs.PlayStrings")

-- the stage, after mounting the chart
SHARED:SetSharedString("my_loading_art", "night")
PlayStrings.stamp()

-- a background script of that play
local art = PlayStrings.get("my_loading_art")   -- "night"; "" for any other chart
```
