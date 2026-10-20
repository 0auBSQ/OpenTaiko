<!-- modules/custom-stages.md -->

# Stages personalizados

Un stage personalizado es un stage que cualquiera puede crear, compartir e instalar, y que lista el menú Stages personalizados de una skin. Declara el espacio de nombres `CStage` con [STAGES:SetNamespace](../api/activities.md), y vuelve mediante la cadena compartida `custom_stage_return` al stage que lo abrió. `Stages.CustomStages` hace ambas cosas y lista los stages personalizados para un menú. `Global/Modules/Stages/custom_stage_tutorial` es uno: una guía, dentro del juego, para crear stages personalizados.

## Declararlo

Arriba en Script.lua, fuera de toda función:

```lua
-- Global/Modules/Stages/lantern_walk/Script.lua, at the top
local CS = require("Stages.CustomStages")

CS.declare{
    title       = CS.allLanguages("menu", "title"),       -- lang/<code>/menu.json, every language
    description = { en = "A calm walk down a street of paper lanterns." },
}
```

`CS.declare(info)` es `STAGES:SetNamespace("CStage", info)`. Los menús leen dos campos de info:

| Campo | Muestra |
| --- | --- |
| `title` | El nombre del stage en la lista: una cadena, o una tabla por código de idioma. El nombre de la carpeta si falta. |
| `description` | Una o dos líneas bajo la lista mientras el stage está seleccionado; opcional. |

Una tabla por código de idioma da el texto en el idioma del juego, si no en inglés, si no en cualquier idioma que tenga. Los demás campos son tuyos: los menús entregan info sin tocarla.

## El camino de vuelta

El menú que abre un stage personalizado pone la cadena compartida `custom_stage_return` al stage al que volver, `"<stage>"` o `"<stage>|<transition>"`, y luego sale hacia el stage personalizado. Este lee y vacía la cadena al entrar, y sale hacia ese stage, o hacia `_title` si la cadena está vacía:

```lua
local back, backTransition

function activate()
    back, backTransition = CS.takeReturn()   -- "_title" when the string is empty
end

function update(ts)
    if leaving then return Exit("stage", back, backTransition) end
end
```

La cadena se lee en cada entrada. Un stage personalizado que sale hacia otro de sus stages y vuelve pone la cadena de nuevo antes de salir, como hace Mi Habitación con `my_room_return`.

## Stages.CustomStages

| Función | Descripción |
| --- | --- |
| `CS.declare(info)  -> boolean` | Declara el stage que llama como stage personalizado: `STAGES:SetNamespace("CStage", info)`. |
| `CS.allLanguages(name, id)  -> table` | La cadena `id` de `lang/<code>/<name>.json` del stage que llama, de cada uno de los nueve idiomas que la tienen, como una tabla por código de idioma. Pensada para `declare`, que se ejecuta antes de elegir un idioma. |
| `CS.localized(value, code?)  -> string or nil` | Un texto declarado en `code` (por defecto el idioma del juego): una cadena tal cual, una tabla por código de idioma como se describe arriba. |
| `CS.list(code?)  -> table` | Los stages personalizados que alcanza el módulo que llama, ordenados por título: `{ name, isGlobal, title, description, info }`, con `title` y `description` ya en el idioma. |
| `CS.enter(name, back, transition?)` | Pone `custom_stage_return` a `back` y sale hacia el stage `name`; escribe `return CS.enter(...)` en el update de un stage. |
| `CS.takeReturn()  -> stage, transition` | Lee y vacía `custom_stage_return`: el stage (`_title` si la cadena está vacía) y la transición (nil para la predeterminada). |
| `CS.open(o)`, `CS.update(ts)`, `CS.draw()`, `CS.isOpen()`, `CS.close()` | La lista como un diálogo sobre la pantalla del que llama, construido con la biblioteca de UI de la skin ([UI.Pick](ui.md)). `update` devuelve nil mientras está abierto, `"cancel"`, o la entrada elegida de `CS.list`; en ambos casos el diálogo se cierra. `o`: `title`, `ui` (una tabla de biblioteca o su nombre de require), `theme`, `sfx`, `navPlayer` (por defecto 0: el teclado y el mando, no los tambores). Si no hay ningún stage personalizado instalado, lo dice sobre un botón Aceptar. |
| `CS.NAMESPACE`, `CS.RETURN_KEY` | `"CStage"` y `"custom_stage_return"`. |

Sus cadenas están en su propia carpeta `lang/`, en los nueve idiomas ([Locale.i18n](locale.md)). Open-World Memories abre la lista desde la entrada Stages personalizados de su pantalla de título, sobre Ajustes:

```lua
-- a menu stage: CS.open{ title = "Custom Stages" } when its entry is chosen, then
function update(ts)
    if CS.isOpen() then
        local r = CS.update(ts)
        if r ~= nil and r ~= "cancel" then return CS.enter(r.name, "_title") end
        return
    end
end

function draw()
    if CS.isOpen() then CS.draw() end
end
```

## Dónde vive un stage personalizado

| Carpeta | Funciona con |
| --- | --- |
| `Global/Modules/Stages/<name>/` | Todas las skins. |
| `System/<skin>/Modules/Stages/<name>/` | Solo esa skin. |

Cuando un nombre existe en ambos lugares, un menú de una skin lista el stage de la skin y un menú global el global, el stage que abriría `Exit`. El juego crea los stages al iniciarse y al volver a cargar la skin, así que un stage instalado con el juego en marcha aparece tras reiniciarlo.

## Compartir uno

Un stage personalizado es su carpeta. Comprime la propia carpeta en un zip, de modo que el archivo empiece por `<name>/Script.lua`, con todo lo que carga y los créditos de sus recursos. Los jugadores la extraen en `Global/Modules/Stages/` de su carpeta del juego, o en `Modules/Stages/` de una skin, y reinician el juego. Una actualización añade y reemplaza archivos pero no borra ninguno, así que elige un nombre de carpeta que no use ningún stage incluido.

## El tutorial

`custom_stage_tutorial` muestra dentro del juego las cuatro cosas que saber para crear un stage personalizado y compartirlo: dónde va su carpeta (y dónde no), el espacio de nombres `CStage`, todo lo que usa dentro de su carpeta y de fuera solo los módulos incluidos, y dónde leer esta documentación, la pestaña Documentación del OpenTaiko Hub. Sus páginas son las imágenes de su carpeta `Textures/` y los textos de su carpeta `lang/`, así que se pueden reemplazar archivo por archivo.
