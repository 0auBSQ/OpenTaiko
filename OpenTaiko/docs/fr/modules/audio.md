<!-- modules/audio.md -->

# Audio

## Audio.MusicDuck

Baisse la musique d'un stage pendant que quelque chose par-dessus doit être entendu (un pop-up de récompense, une réplique parlée), puis la remonte. Le volume passe en douceur du plein volume au niveau atténué avec un smoothstep.

| Fonction | Description |
| --- | --- |
| `MusicDuck.new(opts)  -> duck` | `opts` : `sound` (la musique, ou nil quand votre code applique le facteur lui-même), `base` (son volume en pourcentage à plein régime, 100), `level` (le facteur atténué, 0.3), `inS` (secondes pour atténuer, 0.35), `outS` (secondes pour revenir, 0.8). |
| `duck:update(dt, dimmed)  -> number` | À appeler à chaque frame avec la durée de la frame en secondes ; `dimmed` à true atténue, à false rétablit. Applique `base * factor` au volume en pourcentage du son et renvoie le facteur (de 0 à 1). |
| `duck:reset()` | Revient immédiatement au plein volume (pour un stage qui relance sa musique). |
| `duck:apply()` | Réécrit le volume actuel dans le son. |

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
