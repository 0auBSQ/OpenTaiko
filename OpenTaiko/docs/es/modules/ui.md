<!-- modules/ui.md -->

# UI

Una biblioteca de interfaz para menús y diálogos, que se maneja con el ratón, el teclado y el mando con un único resaltado compartido. `UI.SimpleUI` es la biblioteca mínima que incluye OpenTaiko; un skin puede darle su propio estilo con `derive`, como hace Open-World Memories con PopUI. Los módulos globales toman su biblioteca de `UI.Pick`, que da la que eligió el skin (o la que indica el stage), para que todas las pantallas de un skin tengan el mismo aspecto.

## UI.Pick

`require("UI.Pick")` devuelve una función; al llamarla devuelve una tabla de biblioteca de interfaz.

| Llamada | Biblioteca |
| --- | --- |
| `Pick(name)` | La biblioteca con ese nombre de require (`"PopUI"`, `"UI.SimpleUI"`, ...), para un stage que quiere una concreta. |
| `Pick()` | La biblioteca indicada por la cadena compartida `ui_library`, que un skin establece una vez (el `_boot` de Open-World Memories establece `"PopUI"`). |
| `Pick()` sin `ui_library` | `UI.SimpleUI`. |

Un nombre que no se carga, o que carga algo sin función `new`, también da `UI.SimpleUI`, así que un nombre que otro skin dejó en la cadena compartida no cuesta nada. La elección es Lua simple y cadenas compartidas: el motor no sabe nada de ella.

```lua
-- in a skin's _boot onStart: the library every global module will use
SHARED:SetSharedString("ui_library", "MyUI")

-- in any module
local UI = require("UI.Pick")()
local ui = UI.new{ navPlayer = 1 }
```

## UI.SimpleUI

`UI.new(opts)` crea un gestor, que posee los widgets, dirige la entrada y los dibuja. Llama a `ui:update(ts)` y `ui:draw()` cada fotograma.

| Campo de `opts` | Descripción |
| --- | --- |
| `theme` | Una tabla de tema fusionada sobre el estilo por defecto de la biblioteca (consulta Temas). |
| `sfx` | Sonidos por nombre, fusionados sobre los predeterminados (consulta Sonidos). |
| `bg` | true dibuja primero el color `bg` del tema sobre toda la pantalla. |
| `navPlayer` | El jugador cuyo tambor también navega, en la forma que acepta `NavInput.getPn`; nil para todos los jugadores. |
| `nav` | `"spatial"`: las flechas llevan al widget más cercano en su dirección (una cuadrícula). Por defecto, Arriba y Abajo recorren los widgets en el orden en que se crearon. |

| Método | Descripción |
| --- | --- |
| `ui:update(ts)  -> "cancel" or nil` | Ejecuta la entrada y las animaciones de un fotograma; devuelve `"cancel"` cuando se pulsó Cancel, para que el stage actúe. |
| `ui:draw()` | Dibuja todos los widgets visibles, en el orden en que se crearon. |
| `ui:button(o)`, `ui:label(o)`, ...  `-> widget` | Crea un widget, lo añade y lo devuelve (consulta Widgets). |
| `ui:add(w)`, `ui:remove(w)` | Añade o quita un widget creado a mano. |
| `ui:clear()` | Quita todos los widgets y libera sus lienzos. |
| `ui:disposeWidgets()` | Libera los lienzos de los widgets. Llámala antes de descartar un gestor: los lienzos no se liberan solos. |
| `ui:focus(w)  -> bool`, `ui:focused()  -> widget` | Mueve el foco del teclado a un widget; el widget que lo tiene. |
| `ui:focusNext()`, `ui:focusPrev()` | Mueve el foco del teclado un paso. |
| `ui:setTheme(t)`, `ui:setSfx(t)` | Reemplaza el tema o los sonidos del gestor; todos los widgets cambian de estilo. |
| `ui:isCapturing()  -> bool` | True mientras un cuadro de texto recibe las teclas: omite entonces tus propios atajos. |
| `ui:mouseActive()  -> bool` | True mientras el ratón sea el último dispositivo usado. |
| `ui:drawText(size, str, x, y, color, opacity?, scale?, maxWidth?)` | Dibuja texto con la fuente de glifos de la interfaz, anclado arriba a la izquierda. |
| `ui:drawTextEx(size, str, x, y, fg, bg, opacity, scale, maxWidth, anchor)` | Lo mismo con un color de contorno y uno de los nueve nombres de anclaje. |
| `ui:measureText(size, str)`, `ui:wrapLines(size, str, maxWidth)`, `ui:ellipsize(size, str, maxWidth)` | Medición, ajuste de líneas y acortamiento de texto con la misma fuente. |
| `ui:textNudge(size)  -> number` | El desplazamiento que centra una línea de texto sobre una línea media. |
| `UI.flushSharedFonts()` | Descarta las fuentes en caché; llámala cuando cambie el idioma (y con él la fuente). |

```lua
local UI = require("UI.Pick")()
local ui

function activate()
    ui = UI.new{ navPlayer = 1 }
    ui:panel{ x = 560, y = 240, w = 800, h = 600, title = "Options" }
    ui:toggle{ text = "Hints", x = 620, y = 360, value = true, onChange = function(v) hints = v end }
    ui:button{ text = "Back", x = 860, y = 720, onClick = function() leaving = true end }
end

function update(ts)
    if ui:update(ts) == "cancel" or leaving then return Exit("stage", "_title") end
end

function draw() ui:draw() end

function deactivate() ui:disposeWidgets() end
```

## Widgets

Todos los widgets aceptan `x`, `y`, `w` y `h`, además de `enabled`, `visible`, `focusable`, `mouseOnly` (se resalta y se pulsa con el ratón, pero las teclas lo saltan), `style` o `theme` (campos de tema solo para este widget), `sfx` y `disabledLook = "fade"`. Las opciones se convierten en campos del widget; cámbialas con los métodos (`setEnabled(b)`, `setVisible(b)`, `setText(t)`, ...), que vuelven a aplicar el estilo del widget.

| Fábrica | Opciones y métodos |
| --- | --- |
| `ui:button(o)` | `text` o `icon` (`"play"`, `"pause"`, `"stop"`, `"back"`, `"close"`, `"lock"`, `"repeat"`, ...), `accent`, `onClick(b)`. Su tamaño se ajusta a la etiqueta cuando se omiten `w`/`h`. `setText`, `setIcon`. |
| `ui:label(o)` | `text`, `size` (`"title"`, `"label"`, `"small"` o un número), `color`, `align`, `chip` (un fondo redondeado). No recibe el foco. `setText`. |
| `ui:toggle(o)`, `ui:checkbox(o)` | `text`, `value`, `onChange(v)`. `setValue(v, silent)`. |
| `ui:slider(o)` | `min`, `max`, `step`, `value`, `showValue`, `valueW`, `onChange(v)`. Se arrastra, o se usa Izquierda/Derecha con el foco. `setValue(v, silent)`. |
| `ui:textbox(o)` | `value`, `placeholder`, `maxLen`, `onChange`, `onSubmit`. Haz clic o pulsa Decide para escribir. |
| `ui:panel(o)` | `title`. Un contenedor dibujado detrás de los widgets creados después de él; `panel:content()` da su rectángulo interior. No recibe el foco. |
| `ui:bubble(o)` | `text`, `tail` (`"bottom"`, `"top"`, `"left"`, `"right"`), `tailPos`, `typewriter`, `onDone`. `setText`, `setTail`, `setSize`. |
| `ui:menu(o)` | `items` (cadenas, o `{ text, value, locked, mark }`), `selected`, `rowHeight`, `onChange(i, item)`, `onSelect(i, item)`, `onLockedSelect`, `drawLocked`, `hoverSelects`, `scrollbar`. Una lista desplazable. `setSelected(i)`. |
| `ui:chooser(o)` | `options`, `index`, `wrap`, `onChange(i, option)`. Un selector de opciones paso a paso (Izquierda/Derecha). `value()`, `setOptions`, `setIndex`. |
| `ui:settingsList(o)` | `rows` (`{ header = "..." }` o `{ opt = option }`), `onFocusRow`, `onActivateRow`, `scrollbar`. La lista de filas de opciones de la pantalla de ajustes. `setRows`. |
| `ui:coins(o)` | Un contador de monedas (consulta más abajo). No recibe el foco. |

Las clases de widgets son campos de la biblioteca (`UI.Button`, `UI.Menu`, ...) para heredar de ellas, junto con `UI.Widget` (la clase base), `UI.Shape` (las formas redondeadas, sombras e iconos que hornean los widgets), `UI.Theme`, `UI.Sfx` y `UI.Cache` (lienzos reutilizados).

## El widget de monedas

`ui:coins{ amount, plain, size, iconSize, color, edge }` muestra una cantidad junto al icono de moneda, sobre una píldora (`plain = true`: solo el icono y el número, alineados a la derecha). Cada biblioteca lo dibuja a su manera: el icono de SimpleUI es el `Coin.png` que tiene al lado, y PopUI conserva la caja de monedas del skin incluido.

| Método | Descripción |
| --- | --- |
| `c:setAmount(n)` | Muestra `n` de inmediato. |
| `c:add(n)` | Hace rodar el número en `n`. |
| `c:gain(n)`, `c:pay(n)` | Lo mismo, con un `+n` o `-n` encima y el sonido `coin` o `pay`. |
| `c:amount()  -> number` | La cantidad a la que se dirige. |
| `c:setPrice(n)`, `c:setBonus(n)` | Un precio, o una bonificación `+n`, mostrados encima; nil los quita. |
| `c:drawCoin(x, y, sx, sy, opacity)` | Dibuja solo el icono de moneda, centrado en (x, y). |

## Temas

Un tema es una sola tabla. El gestor resuelve el estilo de cada widget como el predeterminado de la biblioteca, luego el `theme` del gestor y luego el `style` del widget; las tablas se fusionan campo a campo, así que un tema solo nombra lo que cambia.

| Campo | Descripción |
| --- | --- |
| `colors` | Colores `{r, g, b, a}` de 0 a 255: `bg`, `surface`/`surface2` (cara, arriba y abajo), `primary`/`primary2`, `accent`/`accent2`, `outline`, `text`, `textOnAccent`, `textDisabled`, `shadow`, `gloss`, `focusRing`, `track`, y `gain`/`loss` para las monedas. |
| `radius`, `radiusSmall` | Radios de esquina para elementos grandes y pequeños. |
| `outlineWidth` | Grosor del borde. |
| `gloss`, `shadow`, `textOutline` | El brillo superior (bool), la sombra proyectada (`{ dx, dy, layers, grow }`), las letras con contorno (bool). |
| `font` | Tamaños de texto: `small`, `label`, `button`, `title`. |
| `anim` | Escalas y tiempos al pasar el cursor y al pulsar, sus easings (`{ "OUT", "QUAD" }`), `typewriterCps`. |

```lua
local ui = UI.new{ theme = { colors = { accent = { 220, 90, 60, 255 } }, radius = 14 } }
ui:button{ text = "Delete", x = 100, y = 100, style = { colors = { primary = { 180, 40, 40, 255 } } } }
```

## Sonidos

Las tablas `sfx` asocian un nombre a un handle de sonido, a una función o al nombre de otra entrada: `move`, `hover`, `click`, `toggle`, `decide`, `cancel`, `skip`, `error`, además de `coin` y `pay` para el widget de monedas. Por defecto reproducen los sonidos compartidos `Move`, `Decide`, `Cancel`, `Skip` y `Error`, así que un skin que los registre con `SHARED:SetSharedSFX` da sus sonidos a todas las pantallas globales; uno que falte se queda en silencio.

## derive: una biblioteca con tu estilo

`UI.SimpleUI.derive(spec)` devuelve una nueva biblioteca con los widgets, el gestor, el foco y la entrada de SimpleUI, y su propio estilo:

| Campo de `spec` | Descripción |
| --- | --- |
| `Theme` | Un módulo de tema: una tabla cuyo `DEFAULT` es el tema por defecto completo (copia `UI.SimpleUI.theme` de SimpleUI y modifícalo). |
| `Coins` | Una clase de widget de monedas opcional, para un contador de monedas dibujado a tu manera. |
| `name` | El nombre de la biblioteca. |

```lua
-- System/My Skin/Modules/Lib/MyUI.lua
local SimpleUI = require("UI.SimpleUI")
return SimpleUI.derive{
    name  = "MyUI",
    Theme = require("MyUI.theme"),
}
```

Indícala en `ui_library` y todos los módulos globales construirán sus pantallas con ella. El `Modules/Lib/PopUI.lua` de Open-World Memories sigue este patrón: SimpleUI con su tema Bubblegum y su propio widget de monedas.
