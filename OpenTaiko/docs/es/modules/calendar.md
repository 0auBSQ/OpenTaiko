<!-- modules/calendar.md -->

# Calendar

## Calendar.almanac

Patrones de fecha para eventos que van y vienen con el calendario: un artículo destacado de la tienda en Navidad, una escena de broma del 1 de abril, una bonificación mensual. Los días son días de la hora estándar de Japón (UTC+9), sea cual sea el reloj del jugador, así que todos los jugadores ven el mismo día. Un patrón puede escribirse como un token opaco `enc:`, para que una sorpresa no se vea en el código fuente.

| Patrón | Coincide con |
| --- | --- |
| `"2026-06-15"` | Ese día. |
| `"2026-06-15+30"` | Ese día y los 29 siguientes. |
| `"2026-06-15..2026-07-31"` | El intervalo, con ambos extremos incluidos (los dos escritos completos). |
| `"*-06-15"`, `"*-06-15+7"` | Ese día, o esa semana, todos los años. |
| `"*-*-01"`, `"2026-*-13"` | El día 1 de cada mes; cada día 13 de 2026. |
| `"*-06-*"`, `"2026-12-*"` | Un mes entero todos los años; un solo mes. |
| `{ "...", "..." }` | Cualquiera de varios patrones. |

Una entrada es una tabla `{ code, when, priority, sticky }`: está activa mientras `when` coincida. `sticky = true` la mantiene activa para un archivo de guardado una vez que ha coincidido en él, mediante el trigger `.almanac_<code>` de ese archivo. `code` también puede ser un token `enc:`.

| Función | Descripción |
| --- | --- |
| `Almanac.matches(spec)  -> bool` | Si hoy coincide con un patrón, un token o una lista de ellos. |
| `Almanac.active(entry, save?)  -> bool` | Si una entrada está activa hoy; con un archivo de guardado, una entrada sticky que coincidió una vez sigue activa, y una coincidencia activa su trigger. |
| `Almanac.pick(entries, save?, skip?)  -> entry` | La entrada activa con la `priority` más alta (la primera en caso de empate), o nil. Si `skip(entry)` devuelve true, esa entrada queda descartada (una que el jugador ya posee, por ejemplo). |
| `Almanac.encrypt(pattern)  -> string` | El token `enc:` de un patrón o de un código. |
| `Almanac.decrypt(token)  -> string` | El texto de un token, o nil cuando no es un token válido. |
| `Almanac.plain(s)  -> string` | Un token descifrado, o `s` tal cual. |
| `Almanac.parse(spec)  -> table` | Un patrón analizado (nil si está mal formado), para `matchesPattern`. |
| `Almanac.matchesPattern(p)  -> bool` | Si hoy coincide con un patrón analizado. |

```lua
local Almanac = require("Calendar.almanac")
local EVENTS = {
    { code = "winter", when = "*-12-20+12", priority = 80 },
    { code = "spring", when = "*-03-20+14", priority = 50, sticky = true },
}
local now = Almanac.pick(EVENTS, GetSaveFile(0))
if now ~= nil then banner = now.code end
```
