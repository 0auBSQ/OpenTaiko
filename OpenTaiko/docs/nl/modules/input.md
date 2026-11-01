<!-- modules/input.md -->

# Input

## Input.NavInput

Menunavigatie over alle apparaten tegelijk: de pijltjestoetsen van het toetsenbord, Enter en Escape, de universele invoer van de toetsconfiguratie (`RightChange`, `LeftChange`, `Decide`, `Cancel`) en de drum van elke speler (blauwe randen om te bewegen, rode middens om te bevestigen). Een scherm vraagt "heeft de speler bevestigd?" zonder toetsen op te sommen, en de eigen toetsconfiguratie van de speler geldt.

Elke binding is een functie die in dat frame true teruggeeft. Voeg `Pressed` (hetzelfde als geen achtervoegsel), `Pressing` of `Released` aan de naam toe voor de andere invoergebeurtenissen: `decide()`, `decidePressing()`, `leftReleased()`.

| Binding | Reageert op |
| --- | --- |
| `right`, `left` | De pijl naar rechts of links, `RightChange` / `LeftChange`, een blauwe rand. |
| `rightKeyboard`, `leftKeyboard` | Alleen de pijltjestoets. |
| `up`, `down` | De pijl omhoog of omlaag (drums hebben geen verticale invoer). |
| `upOrPadLeft`, `downOrPadRight` | De pijl omhoog (omlaag), of waar `left` (`right`) op een pad of drum op reageert: voor een verticale lijst die een drum kan scrollen. |
| `decide` | Enter, `Decide`, een rood midden. |
| `cancel` | Escape, `Cancel`. |
| `rightOtherPlayer`, `leftOtherPlayer` | Alleen in de sets per speler: de pijl, of de blauwe rand van een andere speler. |

| Functie of veld | Beschrijving |
| --- | --- |
| `NavInput.decide()`, `NavInput.left()`, ...  `-> bool` | De bindingen voor alle spelers tegelijk: het toetsenbord, de universele invoer en de drum van elke speler. |
| `NavInput.getPn(player)  -> table` | De bindingen van één speler: 1 tot 5 voor de drum van die speler plus het toetsenbord en de universele invoer, 0 voor alleen het toetsenbord en de universele invoer, `""` of nil voor alle spelers (hetzelfde als de afkortingen hierboven). |
| `NavInput.p[player]` | De tabellen van `getPn` voor 0 tot 5 en `""`, één keer opgebouwd. |
| `NavInput.bindingNames(binding, player?)  -> string[]` | Waar een binding op reageert, als weergavenamen voor een hint: eerst de toetsenbordtoets, dan de bindingen uit de toetsconfiguratie (`"Pad 3"`, `"MIDI 38"`, `"Mouse Left"`), zonder herhalingen. |
| `NavInput.keyName(key)  -> string` | De weergavenaam van een toetsenbordtoets (`"Return"` geeft `"Enter"`, `"D1"` geeft `"1"`); nil voor geen. |

```lua
local NavInput = require("Input.NavInput")
local p1 = NavInput.getPn(1)

function update(ts)
    if p1.right() then cursor = cursor + 1 end
    if p1.left() then cursor = cursor - 1 end
    if NavInput.cancel() then return Exit("stage", "_title") end
end
```
