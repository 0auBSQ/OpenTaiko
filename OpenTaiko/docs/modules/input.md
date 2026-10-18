<!-- modules/input.md -->

# Input

## Input.NavInput

Menu navigation over every device at once: the keyboard arrows, Return and Escape, the key config's universal inputs (`RightChange`, `LeftChange`, `Decide`, `Cancel`), and each player's drum (blue rims to move, red centres to decide). A screen asks "did the player press decide?" without listing keys, and the player's own key config applies.

Each binding is a function that returns true on that frame. Add `Pressed` (the same as no suffix), `Pressing` or `Released` to the name for the other input events: `decide()`, `decidePressing()`, `leftReleased()`.

| Binding | Answers to |
| --- | --- |
| `right`, `left` | The right or left arrow, `RightChange` / `LeftChange`, a blue rim. |
| `rightKeyboard`, `leftKeyboard` | The arrow key only. |
| `up`, `down` | The up or down arrow (drums have no vertical input). |
| `upOrPadLeft`, `downOrPadRight` | The up (down) arrow, or what `left` (`right`) answers to on a pad or a drum: a vertical list a drum can scroll. |
| `decide` | Return, `Decide`, a red centre. |
| `cancel` | Escape, `Cancel`. |
| `rightOtherPlayer`, `leftOtherPlayer` | Per-player sets only: the arrow, or another player's blue rim. |

| Function or field | Description |
| --- | --- |
| `NavInput.decide()`, `NavInput.left()`, ...  `-> bool` | The bindings for every player at once: the keyboard, the universal inputs and any player's drum. |
| `NavInput.getPn(player)  -> table` | The bindings of one player: 1 to 5 for that player's drum plus the keyboard and universal inputs, 0 for the keyboard and universal inputs only, `""` or nil for every player (the same as the shorthands above). |
| `NavInput.p[player]` | The tables of `getPn` for 0 to 5 and `""`, built once. |
| `NavInput.bindingNames(binding, player?)  -> string[]` | What a binding answers to, as display names for a hint: the keyboard key first, then the key config's bindings (`"Pad 3"`, `"MIDI 38"`, `"Mouse Left"`), without repeats. |
| `NavInput.keyName(key)  -> string` | A keyboard key's display name (`"Return"` gives `"Enter"`, `"D1"` gives `"1"`); nil for none. |

```lua
local NavInput = require("Input.NavInput")
local p1 = NavInput.getPn(1)

function update(ts)
    if p1.right() then cursor = cursor + 1 end
    if p1.left() then cursor = cursor - 1 end
    if NavInput.cancel() then return Exit("stage", "_title") end
end
```
