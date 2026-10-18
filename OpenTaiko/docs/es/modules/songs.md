<!-- modules/songs.md -->

# Songs

## Songs.PlayStrings

Cadenas compartidas que un stage establece para la partida que inicia: el fondo de carga que se muestra, el estilo de una placa de dan, cualquier cosa que lea un fondo o una ROActivity de esa partida. Las cadenas solo responden para el chart con el que se marcaron, así que los valores que deja una partida de la que el stage nunca volvió no llegan a ninguna otra partida.

| Función | Descripción |
| --- | --- |
| `PlayStrings.stamp()` | Marca el chart elegido ahora (`SONGMOUNT`) como aquel al que pertenecen las cadenas. Llámala justo después de montar el chart, antes de `Exit("play")`. |
| `PlayStrings.clear()` | Termina la marca; llámala cuando la partida haya vuelto. |
| `PlayStrings.get(key)  -> string` | La cadena compartida `key` mientras el chart elegido sea el marcado, y si no `""`. |
| `PlayStrings.forTitle(key, title)  -> string` | La misma comprobación por título del chart, para un lector que solo conoce el título (una placa de dan). |

Los valores en sí son cadenas compartidas normales, establecidas con `SHARED:SetSharedString(key, value)`. La marca se guarda en la cadena compartida `play_strings_chart`.

```lua
local PlayStrings = require("Songs.PlayStrings")

-- the stage, after mounting the chart
SHARED:SetSharedString("my_loading_art", "night")
PlayStrings.stamp()

-- a background script of that play
local art = PlayStrings.get("my_loading_art")   -- "night"; "" for any other chart
```
