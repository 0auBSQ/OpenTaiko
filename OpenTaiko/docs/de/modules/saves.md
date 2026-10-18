<!-- modules/saves.md -->

# Saves

## Saves.SavePicker

Die Frage "Wessen Spielstand?", bevor sich eine Stage öffnet: eine Liste der lokalen Spielstände, jeder mit seinem Namensschild und einer Informationszeile (oder seinem Münzstand). Die fragende Stage reicht die Wahl über `SHARED` an die Stage weiter, die sie öffnet. Die Liste wird mit der UI-Bibliothek des Skins ([UI.Pick](ui.md)) gebaut, sofern der Aufrufer keine übergibt, und ihre Titel stammen aus dem eigenen `lang/` des Moduls.

| Funktion | Beschreibung |
| --- | --- |
| `SavePicker.open(o)  -> slot or nil` | Öffnet die Liste. Gibt es nur einen Spielstand (oder keinen), öffnet sie nichts und gibt sofort diesen Slot zurück (0, wenn keiner existiert); andernfalls gibt sie nil zurück, und die Liste bleibt offen. |
| `SavePicker.update(ts)  -> nil, "cancel" or slot` | In jedem Frame aufrufen, solange die Liste offen ist: nil, solange sie angezeigt wird, dann `"cancel"` oder der gewählte 0-basierte Slot. Die Liste schließt sich in beiden Fällen. |
| `SavePicker.draw()` | Zeichnet die Liste. |
| `SavePicker.isOpen()  -> bool`, `SavePicker.close()` | Ob die Liste angezeigt wird; schließt sie. |
| `SavePicker.pass(slot)` | Reicht einen Slot an die Stage weiter, die gleich geöffnet wird (der gemeinsame String `entry_save`). |
| `SavePicker.take()  -> slot or nil` | In der geöffneten Stage: der an sie übergebene Slot, einmal gelesen (der String wird geleert); nil, wenn sie ohne einen kam oder der Spielstand nicht mehr existiert. |
| `SavePicker.first()  -> slot` | Der erste existierende Spielstand, für eine Stage, die ohne Wahl geöffnet wurde. |
| `SavePicker.saves()  -> table` | Die existierenden Spielstände: `{ { value = slot, save = save file }, ... }`. |
| `SavePicker.text(id)  -> string` | Einer der Strings des Moduls (`title`, `title_room`, `title_shop`) in der Sprache des Spiels. |

| Feld von `o` | Beschreibung |
| --- | --- |
| `title` | Der Titel der Liste. |
| `kind` | `"room"` oder `"shop"` für den moduleigenen Titel "Wessen Zimmer?" oder "Wessen Laden?", wenn `title` nicht angegeben ist. |
| `info(slot, save)` | Eine Funktion, die den rechts in der Zeile eines Spielstands angezeigten Text zurückgibt, oder nil. |
| `coins` | true zeigt stattdessen den Münzstand jedes Spielstands. |
| `theme`, `sfx` | Werden an den UI-Manager übergeben. |
| `ui` | Eine UI-Bibliothekstabelle oder ihr require-Name; standardmäßig [UI.Pick](ui.md). |

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
