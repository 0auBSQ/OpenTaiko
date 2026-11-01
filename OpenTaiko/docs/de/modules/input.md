<!-- modules/input.md -->

# Input

## Input.NavInput

Menünavigation über alle Geräte zugleich: die Pfeiltasten der Tastatur, Return und Escape, die universellen Eingaben der Tastenkonfiguration (`RightChange`, `LeftChange`, `Decide`, `Cancel`) und die Trommel jedes Spielers (blaue Ränder zum Bewegen, rote Mitten zum Bestätigen). Ein Bildschirm fragt "hat der Spieler Bestätigen gedrückt?", ohne Tasten aufzuzählen, und die eigene Tastenkonfiguration des Spielers gilt.

Jede Belegung ist eine Funktion, die in diesem Frame true zurückgibt. Hängen Sie `Pressed` (dasselbe wie ohne Suffix), `Pressing` oder `Released` an den Namen an, um die anderen Eingabeereignisse abzufragen: `decide()`, `decidePressing()`, `leftReleased()`.

| Belegung | Reagiert auf |
| --- | --- |
| `right`, `left` | Die rechte oder linke Pfeiltaste, `RightChange` / `LeftChange`, einen blauen Rand. |
| `rightKeyboard`, `leftKeyboard` | Nur die Pfeiltaste. |
| `up`, `down` | Die Pfeiltaste nach oben oder unten (Trommeln haben keine vertikale Eingabe). |
| `upOrPadLeft`, `downOrPadRight` | Die Pfeiltaste nach oben (unten) oder das, worauf `left` (`right`) auf einem Pad oder einer Trommel reagiert: für eine vertikale Liste, die sich mit einer Trommel scrollen lässt. |
| `decide` | Return, `Decide`, eine rote Mitte. |
| `cancel` | Escape, `Cancel`. |
| `rightOtherPlayer`, `leftOtherPlayer` | Nur bei den Sätzen eines einzelnen Spielers: die Pfeiltaste oder der blaue Rand eines anderen Spielers. |

| Funktion oder Feld | Beschreibung |
| --- | --- |
| `NavInput.decide()`, `NavInput.left()`, ...  `-> bool` | Die Belegungen für alle Spieler zugleich: die Tastatur, die universellen Eingaben und die Trommel jedes Spielers. |
| `NavInput.getPn(player)  -> table` | Die Belegungen eines Spielers: 1 bis 5 für die Trommel dieses Spielers plus Tastatur und universelle Eingaben, 0 nur für Tastatur und universelle Eingaben, `""` oder nil für alle Spieler (dasselbe wie die Kurzformen oben). |
| `NavInput.p[player]` | Die Tabellen von `getPn` für 0 bis 5 und `""`, einmal erstellt. |
| `NavInput.bindingNames(binding, player?)  -> string[]` | Worauf eine Belegung reagiert, als Anzeigenamen für einen Hinweis: zuerst die Tastaturtaste, dann die Zuordnungen aus der Tastenkonfiguration (`"Pad 3"`, `"MIDI 38"`, `"Mouse Left"`), ohne Wiederholungen. |
| `NavInput.keyName(key)  -> string` | Der Anzeigename einer Tastaturtaste (`"Return"` ergibt `"Enter"`, `"D1"` ergibt `"1"`); nil für keine. |

```lua
local NavInput = require("Input.NavInput")
local p1 = NavInput.getPn(1)

function update(ts)
    if p1.right() then cursor = cursor + 1 end
    if p1.left() then cursor = cursor - 1 end
    if NavInput.cancel() then return Exit("stage", "_title") end
end
```
