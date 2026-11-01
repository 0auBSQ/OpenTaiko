<!-- modules/input.md -->

# Input

## Input.NavInput

Navegación de menús con todos los dispositivos a la vez: las flechas del teclado, Return y Escape, las entradas universales de la configuración de teclas (`RightChange`, `LeftChange`, `Decide`, `Cancel`) y el tambor de cada jugador (los bordes azules para moverse, los centros rojos para confirmar). Una pantalla pregunta "¿ha pulsado el jugador confirmar?" sin enumerar teclas, y se aplica la configuración de teclas del propio jugador.

Cada asignación es una función que devuelve true en ese fotograma. Añade `Pressed` (igual que sin sufijo), `Pressing` o `Released` al nombre para los demás eventos de entrada: `decide()`, `decidePressing()`, `leftReleased()`.

| Asignación | Responde a |
| --- | --- |
| `right`, `left` | La flecha derecha o izquierda, `RightChange` / `LeftChange`, un borde azul. |
| `rightKeyboard`, `leftKeyboard` | Solo la tecla de flecha. |
| `up`, `down` | La flecha arriba o abajo (los tambores no tienen entrada vertical). |
| `upOrPadLeft`, `downOrPadRight` | La flecha arriba (abajo), o aquello a lo que responde `left` (`right`) en un mando o un tambor: una lista vertical que un tambor puede recorrer. |
| `decide` | Return, `Decide`, un centro rojo. |
| `cancel` | Escape, `Cancel`. |
| `rightOtherPlayer`, `leftOtherPlayer` | Solo en los conjuntos por jugador: la flecha, o el borde azul de otro jugador. |

| Función o campo | Descripción |
| --- | --- |
| `NavInput.decide()`, `NavInput.left()`, ...  `-> bool` | Las asignaciones de todos los jugadores a la vez: el teclado, las entradas universales y el tambor de cualquier jugador. |
| `NavInput.getPn(player)  -> table` | Las asignaciones de un jugador: de 1 a 5 para el tambor de ese jugador más el teclado y las entradas universales, 0 solo para el teclado y las entradas universales, `""` o nil para todos los jugadores (igual que los atajos de arriba). |
| `NavInput.p[player]` | Las tablas de `getPn` para 0 a 5 y `""`, construidas una sola vez. |
| `NavInput.bindingNames(binding, player?)  -> string[]` | A qué responde una asignación, como nombres visibles para una indicación: primero la tecla del teclado, luego las asignaciones de la configuración de teclas (`"Pad 3"`, `"MIDI 38"`, `"Mouse Left"`), sin repeticiones. |
| `NavInput.keyName(key)  -> string` | El nombre visible de una tecla del teclado (`"Return"` da `"Enter"`, `"D1"` da `"1"`); nil si no hay ninguna. |

```lua
local NavInput = require("Input.NavInput")
local p1 = NavInput.getPn(1)

function update(ts)
    if p1.right() then cursor = cursor + 1 end
    if p1.left() then cursor = cursor - 1 end
    if NavInput.cancel() then return Exit("stage", "_title") end
end
```
