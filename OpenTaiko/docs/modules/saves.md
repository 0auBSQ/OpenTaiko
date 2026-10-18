<!-- modules/saves.md -->

# Saves

## Saves.SavePicker

The "whose save?" question asked before a stage opens: a list of the local saves, each with its nameplate and a line of information (or its coin count). The stage that asks hands the choice to the stage it opens through `SHARED`. The list is built with the skin's UI library ([UI.Pick](ui.md)) unless the caller passes one, and its titles come from the module's own `lang/`.

| Function | Description |
| --- | --- |
| `SavePicker.open(o)  -> slot or nil` | Opens the list. When there is only one save (or none) it opens nothing and returns that slot (0 when none) at once; otherwise it returns nil and the list stays open. |
| `SavePicker.update(ts)  -> nil, "cancel" or slot` | Call every frame while the list is open: nil while it is up, then `"cancel"` or the chosen 0-based slot. The list closes in both cases. |
| `SavePicker.draw()` | Draws the list. |
| `SavePicker.isOpen()  -> bool`, `SavePicker.close()` | Whether the list is up; closes it. |
| `SavePicker.pass(slot)` | Hands a slot to the stage about to open (the shared string `entry_save`). |
| `SavePicker.take()  -> slot or nil` | In the opened stage: the slot handed to it, read once (the string is cleared); nil when it came without one or the save no longer exists. |
| `SavePicker.first()  -> slot` | The first existing save, for a stage opened without a choice. |
| `SavePicker.saves()  -> table` | The existing saves: `{ { value = slot, save = save file }, ... }`. |
| `SavePicker.text(id)  -> string` | One of the module's strings (`title`, `title_room`, `title_shop`) in the game's language. |

| `o` field | Description |
| --- | --- |
| `title` | The list's title. |
| `kind` | `"room"` or `"shop"` for the module's own "Whose room?" or "Whose shop?" title when `title` is not given. |
| `info(slot, save)` | A function returning the text shown at the right of a save's row, or nil. |
| `coins` | true shows each save's coin count instead. |
| `theme`, `sfx` | Passed to the UI manager. |
| `ui` | A UI library table, or its require name; [UI.Pick](ui.md) by default. |

```lua
local SavePicker = require("Saves.SavePicker")
local NavInput = require("Input.NavInput")
local picking = false

function update(ts)
    if picking then
        local r = SavePicker.update(ts)
        if r == nil then return end
        picking = false
        if r ~= "cancel" then SavePicker.pass(r); return Exit("stage", "my_shop") end
    elseif NavInput.decide() then
        local slot = SavePicker.open{ kind = "shop", coins = true }
        if slot == nil then picking = true
        else SavePicker.pass(slot); return Exit("stage", "my_shop") end
    end
end

function draw()
    if picking then SavePicker.draw() end
end

-- in my_shop's activate
local slot = SavePicker.take() or SavePicker.first()
```
