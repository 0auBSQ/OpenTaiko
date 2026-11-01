<!-- modules/saves.md -->

# Saves

## Saves.SavePicker

La pregunta "¿de quién es la partida guardada?" que se hace antes de abrir un stage: una lista de las partidas guardadas locales, cada una con su placa de nombre y una línea de información (o su número de monedas). El stage que pregunta pasa la elección al stage que abre a través de `SHARED`. La lista se construye con la biblioteca de interfaz del skin ([UI.Pick](ui.md)) salvo que el llamador pase otra, y sus títulos vienen de la carpeta `lang/` propia del módulo.

| Función | Descripción |
| --- | --- |
| `SavePicker.open(o)  -> slot or nil` | Abre la lista. Cuando solo hay una partida guardada (o ninguna) no abre nada y devuelve esa ranura (0 si no hay ninguna) de inmediato; si no, devuelve nil y la lista queda abierta. |
| `SavePicker.update(ts)  -> nil, "cancel" or slot` | Llámala cada fotograma mientras la lista esté abierta: nil mientras se muestra, y luego `"cancel"` o la ranura elegida, contada desde 0. La lista se cierra en ambos casos. |
| `SavePicker.draw()` | Dibuja la lista. |
| `SavePicker.isOpen()  -> bool`, `SavePicker.close()` | Si la lista se está mostrando; la cierra. |
| `SavePicker.pass(slot)` | Pasa una ranura al stage que está a punto de abrirse (la cadena compartida `entry_save`). |
| `SavePicker.take()  -> slot or nil` | En el stage abierto: la ranura que se le pasó, leída una sola vez (la cadena se borra); nil cuando llegó sin ninguna o la partida guardada ya no existe. |
| `SavePicker.first()  -> slot` | La primera partida guardada existente, para un stage abierto sin elección. |
| `SavePicker.saves()  -> table` | Las partidas guardadas existentes: `{ { value = slot, save = save file }, ... }`. |
| `SavePicker.text(id)  -> string` | Una de las cadenas del módulo (`title`, `title_room`, `title_shop`) en el idioma del juego. |

| Campo de `o` | Descripción |
| --- | --- |
| `title` | El título de la lista. |
| `kind` | `"room"` o `"shop"` para el título propio del módulo, "¿De quién es la habitación?" o "¿De quién es la tienda?", cuando no se da `title`. |
| `info(slot, save)` | Una función que devuelve el texto mostrado a la derecha de la fila de una partida guardada, o nil. |
| `coins` | true muestra en su lugar el número de monedas de cada partida guardada. |
| `theme`, `sfx` | Se pasan al gestor de la interfaz. |
| `ui` | Una tabla de biblioteca de interfaz, o su nombre de require; [UI.Pick](ui.md) por defecto. |

```lua
local SavePicker = require("Saves.SavePicker")
local NavInput = require("Input.NavInput")
local picking = false

function update(ts)
    if picking then
        local r = SavePicker.update(ts)
        if r == nil then return end
        picking = false
        if r ~= "cancel" then SavePicker.pass(r); return Exit("stage", "my_shop") end
    elseif NavInput.decide() then
        local slot = SavePicker.open{ kind = "shop", coins = true }
        if slot == nil then picking = true
        else SavePicker.pass(slot); return Exit("stage", "my_shop") end
    end
end

function draw()
    if picking then SavePicker.draw() end
end

-- in my_shop's activate
local slot = SavePicker.take() or SavePicker.first()
```
