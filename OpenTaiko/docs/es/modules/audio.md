<!-- modules/audio.md -->

# Audio

## Audio.MusicDuck

Baja la música de un stage mientras algo que suena encima necesita oírse (una ventana de recompensa, una frase de voz) y luego la recupera. El volumen pasa suavemente entre el nivel completo y el atenuado con un smoothstep.

| Función | Descripción |
| --- | --- |
| `MusicDuck.new(opts)  -> duck` | `opts`: `sound` (la música, o nil cuando tu código aplica el factor por sí mismo), `base` (su porcentaje de volumen a nivel completo, 100), `level` (el factor atenuado, 0.3), `inS` (segundos para atenuar, 0.35), `outS` (segundos para recuperar, 0.8). |
| `duck:update(dt, dimmed)  -> number` | Llámala cada fotograma con la duración del fotograma en segundos; `dimmed` a true atenúa y a false restaura. Aplica `base * factor` al porcentaje de volumen del sonido y devuelve el factor (de 0 a 1). |
| `duck:reset()` | Vuelve al volumen completo de inmediato (un stage que reinicia su música). |
| `duck:apply()` | Vuelve a escribir el volumen actual en el sonido. |

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
