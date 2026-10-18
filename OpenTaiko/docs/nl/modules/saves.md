<!-- modules/saves.md -->

# Saves

## Saves.SavePicker

De vraag "wiens opslagbestand?" die gesteld wordt voordat een stage opent: een lijst van de lokale opslagbestanden, elk met zijn naamplaatje en een regel informatie (of zijn aantal munten). De stage die de vraag stelt, geeft de keuze via `SHARED` door aan de stage die hij opent. De lijst wordt gebouwd met de UI-bibliotheek van de skin ([UI.Pick](ui.md)), tenzij de aanroeper er een meegeeft, en de titels komen uit de eigen `lang/` van de module.

| Functie | Beschrijving |
| --- | --- |
| `SavePicker.open(o)  -> slot or nil` | Opent de lijst. Is er maar één opslagbestand (of geen), dan opent het niets en geeft het meteen dat slot terug (0 als er geen is); anders geeft het nil terug en blijft de lijst open. |
| `SavePicker.update(ts)  -> nil, "cancel" or slot` | Roep het elk frame aan zolang de lijst open is: nil zolang ze getoond wordt, daarna `"cancel"` of het gekozen 0-gebaseerde slot. De lijst sluit in beide gevallen. |
| `SavePicker.draw()` | Tekent de lijst. |
| `SavePicker.isOpen()  -> bool`, `SavePicker.close()` | Of de lijst getoond wordt; sluit haar. |
| `SavePicker.pass(slot)` | Geeft een slot door aan de stage die op het punt staat te openen (de gedeelde string `entry_save`). |
| `SavePicker.take()  -> slot or nil` | In de geopende stage: het slot dat eraan is doorgegeven, één keer gelezen (de string wordt geleegd); nil als de stage zonder slot kwam of het opslagbestand niet meer bestaat. |
| `SavePicker.first()  -> slot` | Het eerste bestaande opslagbestand, voor een stage die zonder keuze is geopend. |
| `SavePicker.saves()  -> table` | De bestaande opslagbestanden: `{ { value = slot, save = save file }, ... }`. |
| `SavePicker.text(id)  -> string` | Een van de strings van de module (`title`, `title_room`, `title_shop`) in de taal van het spel. |

| Veld van `o` | Beschrijving |
| --- | --- |
| `title` | De titel van de lijst. |
| `kind` | `"room"` of `"shop"` voor de eigen titel van de module, "Wiens kamer?" of "Wiens winkel?", als `title` niet is opgegeven. |
| `info(slot, save)` | Een functie die de tekst teruggeeft die rechts in de rij van een opslagbestand staat, of nil. |
| `coins` | true toont in plaats daarvan het aantal munten van elk opslagbestand. |
| `theme`, `sfx` | Worden doorgegeven aan de UI-manager. |
| `ui` | Een UI-bibliotheektabel, of haar require-naam; standaard [UI.Pick](ui.md). |

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
