<!-- modules/core.md -->

# Core

Pequeñas utilidades sin pantalla propia: curvas de easing, cálculos de color, utilidades de tablas, un codificador y decodificador JSON, y la carpeta del archivo actual. No tocan ningún estado del motor al cargarse, así que cualquier módulo, incluidos los de solo lectura, puede cargarlas con require.

## Core.Easing

Curvas de easing sobre una proporción de 0 a 1, para animaciones que sigue tu propio reloj (una línea de tiempo, la edad de una partícula). Para una interpolación con el reloj del motor, la otra opción es la global `COUNTER` con SetEasing. Todas las curvas limitan su entrada a 0..1 y llevan 0 a 0 y 1 a 1, excepto `hump`.

| Función | Descripción |
| --- | --- |
| `Easing.linear(t)  -> number` | La propia proporción limitada. |
| `Easing.inQuad(t)`, `outQuad`, `inOutQuad`, `inCubic`, `outCubic`, `inOutCubic`, `inSine`, `outSine`, `inOutSine`, `inExpo`, `outExpo`  `-> number` | Las curvas habituales, por su nombre. |
| `Easing.inBack(t, s?)`, `Easing.outBack(t, s?)  -> number` | Curvas que se pasan del final; `s` es la cantidad de exceso (1.7 por defecto). |
| `Easing.inBounce(t)`, `Easing.outBounce(t)`, `Easing.outElastic(t)  -> number` | Curvas con rebote y elásticas. |
| `Easing.hump(t)  -> number` | 0, sube a 1 en el centro y vuelve a 0: para aplastamientos y destellos. |
| `Easing.span(t, t0, dur)  -> number` | El progreso de la ventana que empieza en `t0` y dura `dur`, limitado a 0..1. |
| `Easing.lerp(a, b, k)  -> number` | `a + (b - a) * k`. |
| `Easing.clamp01(t)  -> number` | `t` limitado a 0..1. |
| `Easing.get(kind, fn)  -> function` | Una curva según los nombres de easing del motor: `kind` es `"IN"`, `"OUT"` o `"INOUT"`, `fn` es `"LINEAR"`, `"SINE"`, `"QUAD"`, `"CUBIC"`, `"EXPO"`, `"BACK"`, `"BOUNCE"` o `"ELASTIC"`. Una combinación sin curva da la QUAD. |

```lua
local Easing = require("Core.Easing")
local k = Easing.outBack(Easing.span(t, startTime, 0.4))
x = Easing.lerp(x0, x1, k)
```

## Core.Color

Conversiones y ajustes de color. Cada canal es un float de 0 a 1, incluido el tono (de 0 a 1 alrededor del círculo cromático); las funciones reciben y devuelven valores `r, g, b` separados.

| Función | Descripción |
| --- | --- |
| `Color.rgbToHsv(r, g, b)  -> h, s, v` | De RGB a HSV. |
| `Color.hsvToRgb(h, s, v)  -> r, g, b` | De HSV a RGB. |
| `Color.rgbToHsl(r, g, b)  -> h, s, l` | De RGB a HSL. |
| `Color.hslToRgb(h, s, l)  -> r, g, b` | De HSL a RGB. |
| `Color.mix(r, g, b, r2, g2, b2, t)  -> r, g, b` | Mezcla lineal: `t` = 0 conserva el primer color y 1 da el segundo. |
| `Color.lighten(r, g, b, amount)  -> r, g, b` | Acerca la luminosidad HSL al blanco en `amount` del rango restante (1 = blanco); el tono y la saturación se mantienen. |
| `Color.darken(r, g, b, amount)  -> r, g, b` | Acerca la luminosidad HSL al negro en `amount` (1 = negro). |
| `Color.saturate(r, g, b, factor, add?)  -> r, g, b` | Multiplica la saturación HSV por `factor`, le suma `add` y la limita a 1. |
| `Color.brighten(r, g, b, factor)  -> r, g, b` | Multiplica el valor HSV por `factor`, limitado a 1. |
| `Color.ramp(from, to, n)  -> table` | `n` colores escalonados de `{r, g, b}` a `{r, g, b}`, como una lista de `{r, g, b}`. |
| `Color.fromEngine(col)  -> r, g, b, a` | Los canales de un objeto `COLOR`, como floats. |
| `Color.toEngine(r, g, b, a?)  -> color` | Un objeto `COLOR` a partir de floats (alfa 1 por defecto). |
| `Color.rampEngine(fromCol, toCol, n)  -> table` | Una rampa de `n` objetos `COLOR` entre dos objetos `COLOR`. |
| `Color.clamp01(v)  -> number` | `v` limitado a 0..1. |

## Core.Util

| Función | Descripción |
| --- | --- |
| `Util.clamp(v, lo, hi)  -> number` | `v` mantenido entre `lo` y `hi`. |
| `Util.lerp(a, b, t)  -> number` | Interpolación lineal. |
| `Util.round(v)  -> number` | Redondea al entero más cercano (las mitades hacia arriba). |
| `Util.deepcopy(o)  -> value` | Una copia profunda de una tabla Lua, incluidas metatablas y ciclos. |
| `Util.merge(base, over)  -> table` | Una copia de `base` con `over` fusionado: las subtablas se fusionan recursivamente y los demás valores se reemplazan. |
| `Util.cloneTable(dict)  -> table` | Un diccionario .NET (un objeto JSON analizado, un conjunto de filas SQL) copiado recursivamente en una tabla Lua. |

## Core.Json

Texto JSON a partir de valores Lua y viceversa, en Lua puro. A diferencia de `JSONLOADER`, que analiza archivos en diccionarios .NET, trabaja con tablas Lua, así que conviene para datos que guardas en una cadena del archivo de guardado (`sf:SetGlobalString`) o en un valor de base de datos.

| Función | Descripción |
| --- | --- |
| `Json.encode(value)  -> string` | Una tabla con las claves 1..n se escribe como array, y cualquier otra tabla como objeto con sus claves ordenadas, así que el mismo valor da siempre el mismo texto. Las funciones y los userdata se omiten en los objetos; los números enteros se escriben sin punto decimal. Una tabla que se contiene a sí misma lanza un error. |
| `Json.decode(text)  -> value` o `nil, message` | Analiza texto JSON. El `null` de JSON se convierte en `Json.null`; ante un error de sintaxis devuelve nil y un mensaje con la posición. |
| `Json.null` | El valor que representa el `null` de JSON; se vuelve a codificar como `null`. |

```lua
local Json = require("Core.Json")
local sf = GetSaveFile(0)
local data = Json.decode(sf:GetGlobalString("my_feature")) or { v = 1, items = {} }
data.items[#data.items + 1] = "lamp"
sf:SetGlobalString("my_feature", Json.encode(data))
```

## Core.Here

Una función que devuelve la carpeta del archivo que la llama, a partir de los argumentos que `require` pasa a ese archivo. Así es como un archivo de Lib encuentra sus propios recursos (consulta [Los recursos se resuelven respecto al llamador](README.md)).

| Función | Descripción |
| --- | --- |
| `require("Core.Here")(...)  -> string` | La carpeta absoluta del archivo, con barras normales y una `/` final; nil cuando el archivo no se cargó con `require`. Llámala en el nivel superior del archivo, donde `...` contiene el nombre del módulo y la ruta del archivo. |
