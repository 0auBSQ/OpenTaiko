<!-- modules/audio.md -->

# Audio

## Audio.MusicDuck

Zet de muziek van een stage zachter zolang er iets bovenop gehoord moet worden (een beloningspop-up, een gesproken regel), en brengt haar daarna terug. Het volume glijdt met een smoothstep tussen vol en het gedempte niveau.

| Functie | Beschrijving |
| --- | --- |
| `MusicDuck.new(opts)  -> duck` | `opts`: `sound` (de muziek, of nil wanneer je code de factor zelf toepast), `base` (haar volumepercentage op vol, 100), `level` (de gedempte factor, 0.3), `inS` (seconden om te dempen, 0.35), `outS` (seconden om terug te komen, 0.8). |
| `duck:update(dt, dimmed)  -> number` | Roep het elk frame aan met de frametijd in seconden; `dimmed` true dempt, false herstelt. Past `base * factor` toe op het volumepercentage van het geluid en geeft de factor terug (0 tot 1). |
| `duck:reset()` | Meteen terug naar vol volume (voor een stage die zijn muziek opnieuw start). |
| `duck:apply()` | Schrijft het huidige volume opnieuw naar het geluid. |

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
