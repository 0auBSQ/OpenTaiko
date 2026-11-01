<!-- modules/locale.md -->

# Locale

## Locale.i18n

Las traducciones propias de un módulo, guardadas junto a él en lugar de en los `Locales/` del skin. Los textos viven en `lang/<code>/<name>.json`, un archivo por funcionalidad, con ids estables como claves; `lang/en/` contiene el inglés y es la reserva para todos los demás idiomas. Los códigos son los ids de idioma del juego: `en`, `de`, `es`, `fr`, `ja`, `ko`, `nl`, `ru`, `zh`.

```
Global/Modules/Stages/my_stage/
  Script.lua
  lang/
    en/menu.json      { "title": "Settings", "count": "%d items", "tips": ["One", "Two"] }
    ja/menu.json      { "title": "設定", "count": "%d 件" }
```

| Función o campo | Descripción |
| --- | --- |
| `I18N.texts(name, dir?)  -> texts` | Los textos de `lang/<code>/<name>.json`. `dir` es la carpeta que contiene `lang/`: omítelo (o pasa `""`) en un stage o una activity para usar su propia carpeta, y pasa la carpeta de `Core.Here` en un archivo de Lib. Los archivos se leen en el primer uso y se conservan. |
| `I18N.detect()` | Lee el idioma actual del juego y lo convierte en el que usan todas las búsquedas. Llámala en `activate`, que se ejecuta en cada entrada, para que un idioma cambiado en los ajustes se aplique en la siguiente visita. |
| `I18N.load(code)` | Usa el idioma `code` (un banco de pruebas o una vista previa de idioma). |
| `I18N.currentCode()  -> string` | El id del idioma actual del juego, `"en"` cuando el motor no da ninguno. |
| `I18N.code` | El idioma que usan ahora las búsquedas. |
| `I18N.localizedPath(path, code?)  -> string` | `<name>_<code>.<ext>` cuando existe una textura con ese nombre junto a `path`, y si no `path`: para imágenes con texto incrustado. |

| Método | Descripción |
| --- | --- |
| `texts:tr(id)  -> string` | El texto de `id`; el propio id cuando ningún idioma lo tiene, lo que hace visible el hueco en pantalla. |
| `texts:trf(id, ...)  -> string` | El texto de `id` usado como patrón de `string.format` con los argumentos. |
| `texts:get(...)  -> string` | La cadena en una ruta de claves (`texts:get("menu", "title")` para objetos anidados), nil si falta o está vacía. |
| `texts:list(...)  -> table` | El array de cadenas en una ruta de claves; cada entrada recurre al inglés por separado. |
| `texts:loc(...)  -> handle` | Un handle cuyo `GetString(fallback)` lee la ruta de claves en el momento de la llamada, así que un nombre guardado al cargar sigue un cambio de idioma posterior. |
| `texts:node(...)  -> value` | El nodo JSON en bruto en una ruta de claves (en el idioma actual, si no en inglés). |

```lua
local I18N = require("Locale.i18n")
local T = I18N.texts("menu")
local title, count

function activate()
    I18N.detect()
    title = T:tr("title")
    count = T:trf("count", #items)
end
```

<div class="callout warn">
Las búsquedas siguen `I18N.code`, que solo cambia cuando se ejecuta `detect` o `load`. Un módulo que permanece en pantalla mientras el jugador cambia el idioma puede definir la global `reloadLanguage(lang)` y llamar a `detect` ahí. Los módulos de texto de Pagoda, Vault y Shop comprueban el idioma por sí mismos en cada búsqueda.
</div>

Los módulos más antiguos usan en su lugar un diccionario de inglés exacto: `lang/<code>.lua` devuelve una tabla que asocia cada cadena en inglés con su traducción, `detect` carga la del idioma actual, y las funciones `I18N.tr(s)` e `I18N.trf(fmt, ...)` buscan las cadenas en ella. Esa forma sigue funcionando; los módulos nuevos deben usar `texts`.
