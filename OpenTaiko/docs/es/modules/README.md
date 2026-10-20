<!-- modules/README.md -->

# Módulos proporcionados

OpenTaiko incluye una biblioteca de módulos Lua que cualquier skin puede usar: utilidades (easing, colores, JSON, entrada, localización), una biblioteca de interfaz, un cuadro de diálogo, el motor de mundos 3D, y las reglas y los datos de la pagoda, la bóveda secreta y la tienda de monedas. Viven en la carpeta del juego, no en un skin, así que un skin nuevo los obtiene sin copiar nada. Esta sección describe cada uno; la [referencia de la API](../api/README.md) cubre las globales del motor sobre las que se construyen.

## Global/Modules

`Global/Modules/` en la carpeta de instalación del juego tiene las mismas subcarpetas que el `Modules/` de un skin: `Transitions/`, `Stages/`, `Activities/`, `ROActivities/` y `Lib/`. El juego carga cada categoría global justo después de la del skin, sea cual sea el skin activo. Los módulos de estas páginas son los archivos de `Global/Modules/Lib/`, a los que se llega con `require`; `Global/Modules/Stages/` también contiene stages que cualquier skin puede abrir, como [Mi Habitación](my-room.md).

La carpeta llega con la build del juego y cada actualización la reemplaza, así que un skin no debe colocar archivos en ella. Para cambiar un módulo proporcionado, cópialo en tu propio skin con otro nombre.

## El orden de require

`require` convierte los puntos en carpetas y no distingue mayúsculas de minúsculas, así que `require("Core.Easing")` carga `Core/Easing.lua`. Las carpetas en las que busca dependen del módulo que se ejecuta:

| Llamador | Carpetas buscadas, en orden |
| --- | --- |
| Un módulo de skin (`System/<skin>/Modules/...`) | su propia carpeta, el `Modules/Lib/` del skin y luego `Global/Modules/Lib/` |
| Un módulo global (`Global/Modules/...`) | su propia carpeta, `Global/Modules/Lib/` y luego el `Modules/Lib/` del skin |

Un archivo de Lib no tiene estado Lua propio: se ejecuta en el estado del módulo que lo cargó con require, así que los require que contiene siguen el orden de ese llamador. Por tanto, un archivo del `Modules/Lib/` del skin con la misma ruta que un módulo proporcionado lo reemplaza en todos los require hechos desde los módulos del skin, incluidos los require dentro de otros módulos proporcionados. Mantén tus propios archivos de Lib fuera de los nombres de carpeta de abajo, salvo que quieras reemplazar un módulo.

## Nombres

Cada componente es una carpeta de `Global/Modules/Lib/`, y sus módulos se cargan como `<Component>.<Module>`:

| Componente | Módulos | Página |
| --- | --- | --- |
| `Core` | `Easing`, `Color`, `Util`, `Json`, `Here` | [Core](core.md) |
| `Input` | `NavInput` | [Input](input.md) |
| `Audio` | `MusicDuck` | [Audio](audio.md) |
| `Locale` | `i18n` | [Locale](locale.md) |
| `Calendar` | `almanac` | [Calendar](calendar.md) |
| `Songs` | `PlayStrings` | [Songs](songs.md) |
| `World` | `OWM3d` y sus partes | [World](world.md) |
| `UI` | `SimpleUI`, `Pick` | [UI](ui.md) |
| `Saves` | `SavePicker` | [Saves](saves.md) |
| `Characters` | `Student` | [Characters](characters.md) |
| `VN` | `Dialogue` | [VN](vn.md) |
| `Pagoda` | `Levels`, `Floor`, `Rewards`, `Progress`, `Songs`, `Run`, `Texts` | [Pagoda](pagoda.md) |
| `Vault` | `Keys`, `Chests`, `Texts` | [Vault](vault.md) |
| `Shop` | `Daily`, `Catalog`, `Texts`, `Seasonal`, `Popup` | [Shop](shop.md) |
| `Online` | `Lobby`, `Protocol`, `Identity`, `Media`, `Texts` | [Online](online.md) |
| `Stages` | `CustomStages` | [Stages personalizados](custom-stages.md) |

## Los nombres que conserva Open-World Memories

El skin incluido llevaba antes la mayoría de estos archivos en su propio `Modules/Lib/`. Cada archivo antiguo es ahora un reenvío de dos líneas, así que un módulo del skin que todavía carga el nombre antiguo con require obtiene el módulo global, y ambos nombres comparten una sola instancia (un estado, una caché):

```lua
-- moved to Global/Modules/Lib/Core/Easing.lua
return require("Core.Easing")
```

| Nombre antiguo | Módulo proporcionado |
| --- | --- |
| `Easing`, `Color`, `Util` | `Core.Easing`, `Core.Color`, `Core.Util` |
| `NavInput`, `MusicDuck`, `PlayStrings` | `Input.NavInput`, `Audio.MusicDuck`, `Songs.PlayStrings` |
| `i18n`, `almanac` | `Locale.i18n`, `Calendar.almanac` |
| `OWM3d`, `OWM3d.<part>` | `World.OWM3d`, `World.OWM3d.<part>` |
| `PopUI.<part>` (el código compartido: `manager`, `widget`, `shape`, `util`, `cache`, `sfx`, todos los `widgets.*` salvo `widgets.coins`) | `UI.SimpleUI.<part>` |
| `SavePicker`, `Student`, `dialogue` | `Saves.SavePicker`, `Characters.Student`, `VN.Dialogue` |

`PopUI` sigue siendo un módulo del skin: es SimpleUI con el estilo propio del skin (consulta [UI](ui.md)). El código nuevo debe usar los nombres proporcionados.

## Los recursos se resuelven respecto al llamador

Las rutas relativas que se pasan a `TEXTURE`, `SOUND`, `VIDEO`, `JSONLOADER`, `SQL`, `DATABASE`, `STORAGE` y `SHARED` se resuelven respecto a la carpeta del módulo que llama, nunca respecto al archivo de Lib que hace la llamada. Un módulo proporcionado que incluye sus propios archivos (una imagen, un conjunto JSON, una base de datos) los encuentra con `Core.Here`, que devuelve la carpeta del propio archivo, y los carga con las variantes de ruta absoluta:

```lua
-- Global/Modules/Lib/MyPart/Thing.lua
local HERE = require("Core.Here")(...)          -- ".../Global/Modules/Lib/MyPart/"

local icon = TEXTURE:CreateTextureFromAbsolutePath(HERE .. "icon.png")
local beep = SOUND:CreateSFXFromAbsolutePath(HERE .. "beep.ogg")
local data = JSONLOADER:JsonParseFileAny(HERE .. "data.json")
SHARED:SetSharedTextureUsingAbsolutePath("thing_icon", HERE .. "icon.png")
```

Pasa `...` desde el nivel superior del archivo: ahí `require` entrega a cada archivo su nombre de módulo y su ruta absoluta. Una ruta relativa que un llamador pasa a un módulo proporcionado (un archivo de modelo para `World.OWM3d`, un sonido para un widget) es del llamador y se resuelve respecto a la carpeta del llamador, como siempre.

## Las cadenas viven con el módulo

`THEME:GetSkinString` solo lee los `Locales/` del skin activo, así que un módulo proporcionado nunca lo usa para su propio texto. Sus cadenas están en una carpeta `lang/` junto a él, un archivo JSON por funcionalidad y por idioma (`lang/en/vault.json`, `lang/ja/vault.json`, ...), y se leen con [Locale.i18n](locale.md) indicando la carpeta del módulo:

```lua
local HERE = require("Core.Here")(...)
local T = require("Locale.i18n").texts("vault", HERE)   -- HERE .. "lang/<code>/vault.json"
local name = T:tr("chest_1")
```

El inglés (`lang/en/`) es la reserva para un idioma al que le falta un archivo o una clave. Un stage o una activity global hace lo mismo sin el argumento de carpeta: `texts(name)` lee la carpeta `lang/` del propio stage.

## Estado compartido

Las texturas, los sonidos y las cadenas de `SHARED` forman un único espacio de nombres para todo el juego, tanto para los módulos del skin como para los globales. Los módulos proporcionados usan algunas claves fijas, listadas en sus páginas (por ejemplo la cadena compartida `ui_library` y las texturas `myroom_thumb_<id>`). Pon un prefijo a tus propias claves para que no puedan colisionar. Los datos de guardado (triggers, contadores y cadenas) pertenecen al archivo de guardado, no a un módulo, y así es como la pagoda, la bóveda, la tienda y Mi Habitación se pasan objetos entre sí.
