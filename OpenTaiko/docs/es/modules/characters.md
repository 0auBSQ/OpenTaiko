<!-- modules/characters.md -->

# Characters

## Characters.Student

El estudiante de la historia con el que camina el jugador, A o B, y la paleta de colores de ese estudiante, para que todas las escenas de historia dibujen al mismo estudiante con los mismos colores. Los estudiantes son los personajes `02v2 - Student (A)` y `03v2 - Student (B)` de `Global/Characters/`.

Todo se guarda en el archivo de guardado:

| Clave de guardado | Tipo | Contenido |
| --- | --- | --- |
| `story_student` | contador | El estudiante elegido: 1 es B; cualquier otro valor, incluido sin definir, se lee como A. |
| `.character_palette_<folder>` | contador | El índice de paleta, contado desde 0, del personaje del estudiante: el mismo contador que escribe la pantalla de personalización de personajes, así que ambos van a la par. A y B guardan cada uno el suyo. |
| `.character_playcount_<folder>` | contador | Las partidas jugadas con ese personaje, que desbloquean paletas. |

Las paletas de un estudiante son el `Palettes.json` del personaje, seguidas de las paletas de la copia propia del módulo (`Global/Modules/Lib/Characters/Student/<id>/Palettes.json`) que el archivo del personaje no tiene, emparejadas por `name`. Un personaje sin paletas, o que no está instalado, sigue teniendo las de la copia. Un índice fuera de la lista se lee como la paleta por defecto, 0.

| Función | Descripción |
| --- | --- |
| `Student.current(save)  -> "a" or "b"` | El estudiante que ha elegido la partida guardada. |
| `Student.ensure(save)  -> "a" or "b"` | Lo mismo, escribiendo A en el contador cuando no contiene ningún estudiante. |
| `Student.set(save, id)  -> id` | Elige un estudiante. |
| `Student.folder(id)  -> string` | El nombre de la carpeta de personaje del estudiante. |
| `Student.index(save, id)  -> number` | El índice de paleta del estudiante, siempre válido. |
| `Student.setIndex(save, id, idx)  -> number` | Guarda un índice de paleta; cuando ese personaje es el que la partida guardada tiene equipado, su paleta en uso también cambia. |
| `Student.palettes(id)  -> table` | La lista de paletas: entradas `{ name, plays, blend, stops }`, la entrada `i` para el índice `i - 1`; `name` está en el idioma del juego cuando el archivo lo proporciona. |
| `Student.count(id)`, `Student.entry(id, idx)`, `Student.clamp(id, idx)` | El número de paletas, una entrada, un índice convertido en válido. |
| `Student.unlocked(save, id, idx)  -> bool` | Si la paleta está disponible: sin requisito de partidas, o con suficientes partidas con el personaje. |
| `Student.gradient(id, idx)  -> gradient` | La paleta como mapa `GRADIENT`, o nil para una paleta sin colores. |
| `Student.withPalette(id, idx, fn, ...)` | Ejecuta `fn` (dibujo 2D) con la paleta activa. Devuelve los resultados de pcall. |
| `Student.registerSprite(scene, spriteId, tex, id, idx)  -> bool` | Registra `tex` como sprite billboard de una escena 3D, recoloreado con la paleta. |
| `Student.reload()` | Olvida los archivos de paleta analizados, que se vuelven a leer en la siguiente consulta. |

`palettes`, `count`, `entry`, `clamp`, `gradient` y `registerSprite` aceptan un último argumento opcional, `visitor`: true para la lista del estudiante de otro jugador (un visitante en línea), guardada en caché aparte de la del propio jugador.

```lua
local Student = require("Characters.Student")
local save = GetSaveFile(0)
local id  = Student.current(save)
local idx = Student.index(save, id)
Student.withPalette(id, idx, function() tex:Draw(x, y) end)
```
