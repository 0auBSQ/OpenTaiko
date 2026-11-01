<!-- modules/audio.md -->

# Audio

## Audio.MusicDuck

Senkt die Musik einer Stage, solange etwas darüber gehört werden muss (ein Belohnungs-Pop-up, eine Sprachzeile), und hebt sie danach wieder an. Die Lautstärke gleitet mit einem Smoothstep zwischen voll und dem abgesenkten Pegel.

| Funktion | Beschreibung |
| --- | --- |
| `MusicDuck.new(opts)  -> duck` | `opts`: `sound` (die Musik, oder nil, wenn Ihr Code den Faktor selbst anwendet), `base` (ihre Lautstärke in Prozent bei voller Stärke, 100), `level` (der abgesenkte Faktor, 0.3), `inS` (Sekunden bis zum Absenken, 0.35), `outS` (Sekunden bis zur Rückkehr, 0.8). |
| `duck:update(dt, dimmed)  -> number` | Rufen Sie es in jedem Frame mit der Framezeit in Sekunden auf; `dimmed` true senkt ab, false stellt wieder her. Wendet `base * factor` auf die Lautstärke des Sounds in Prozent an und gibt den Faktor zurück (0 bis 1). |
| `duck:reset()` | Sofort zurück auf volle Lautstärke (für eine Stage, die ihre Musik neu startet). |
| `duck:apply()` | Schreibt die aktuelle Lautstärke erneut in den Sound. |

```lua
local MusicDuck = require("Audio.MusicDuck")
local duck, last = nil, 0

function activate()
    duck = MusicDuck.new{ sound = bgm, base = 80 }
end

function update(ts)
    local dt = (ts - last) / 1000; last = ts
    duck:update(dt, rewardPopupShowing)
end
```
