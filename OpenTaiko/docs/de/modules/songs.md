<!-- modules/songs.md -->

# Songs

## Songs.PlayStrings

Gemeinsame Strings, die eine Stage für das eine Spiel setzt, das sie startet: der anzuzeigende Ladehintergrund, der Stil eines Dan-Schilds, alles, was ein Hintergrund oder eine ROActivity dieses Spiels liest. Die Strings gelten nur für den Chart, mit dem sie gestempelt wurden, sodass Werte, die ein Spiel zurückgelassen hat, von dem die Stage nie zurückkam, kein anderes Spiel erreichen.

| Funktion | Beschreibung |
| --- | --- |
| `PlayStrings.stamp()` | Markiert den jetzt gewählten Chart (`SONGMOUNT`) als den, zu dem die Strings gehören. Rufen Sie es direkt nach dem Mounten des Charts auf, vor `Exit("play")`. |
| `PlayStrings.clear()` | Beendet den Stempel; rufen Sie es auf, wenn das Spiel zurückgekehrt ist. |
| `PlayStrings.get(key)  -> string` | Der gemeinsame String `key`, solange der gewählte Chart der gestempelte ist, sonst `""`. |
| `PlayStrings.forTitle(key, title)  -> string` | Dieselbe Prüfung anhand des Charttitels, für einen Leser, der nur den Titel kennt (ein Dan-Schild). |

Die Werte selbst sind gewöhnliche gemeinsame Strings, gesetzt mit `SHARED:SetSharedString(key, value)`. Der Stempel liegt im gemeinsamen String `play_strings_chart`.

```lua
local PlayStrings = require("Songs.PlayStrings")

-- the stage, after mounting the chart
SHARED:SetSharedString("my_loading_art", "night")
PlayStrings.stamp()

-- a background script of that play
local art = PlayStrings.get("my_loading_art")   -- "night"; "" for any other chart
```
