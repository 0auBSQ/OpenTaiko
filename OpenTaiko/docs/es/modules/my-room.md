<!-- modules/my-room.md -->

# Mi Habitación

Mi Habitación es un stage global, `my_room` en `Global/Modules/Stages/`: la habitación del jugador, que puede recorrer, amueblar con lo que le dan la pagoda, la bóveda y la tienda de monedas, y abrir a visitantes en línea. Cualquier skin puede abrirla, y todos los skins muestran la misma habitación, porque sus datos viven en el archivo de guardado.

## Entrar y salir

Establece la cadena compartida `my_room_return` con el stage al que Mi Habitación debe volver y luego sal hacia `my_room`:

```lua
SHARED:SetSharedString("my_room_return", "my_menu")               -- back to my_menu
-- or, with the transition to play on the way back:
SHARED:SetSharedString("my_room_return", "my_menu|my_transition")
return Exit("stage", "my_room")
```

Mi Habitación lee la cadena y la borra al entrar. Cuando está vacía, la vuelta es a la pantalla de título (`_title`).

La cadena compartida `my_room_place` recuerda de quién era la habitación y dónde estaba el jugador, en los viajes fuera de la habitación y de vuelta: la matriz de pruebas (un mueble que lista stages que visitar) y la cápsula (otro que abre un stage distinto) llevan las dos fuera de la habitación. Una pantalla de título puede borrarla (establecerla a `""`) para que la siguiente visita empiece en la puerta.

Open-World Memories conserva un stage de reenvío, `myroom`, para las antiguas llamadas `Exit("stage", "myroom")` y la salida `"heya"` del motor; desaparecerá en la versión oficial, así que el código nuevo debe salir hacia `my_room`.

## Sus datos

| Clave | Tipo | Contenido |
| --- | --- | --- |
| `my_room` | cadena de guardado | La habitación, como JSON `{ v, room, jukebox }` (`sf:GetGlobalString("my_room")`). Solo se escribe para la ranura propia de la partida guardada: nunca para un visitante en línea ni para una ranura vacía. |
| `.myroom_<id>` | contador | Muebles que da otro stage: Mi Habitación los añade a las existencias de la habitación una sola vez, en la siguiente visita, y lleva un registro de lo que ya ha recogido. |
| `.myroom_held_<id>` | contador | Cuántos ejemplares de ese mueble posee la partida guardada. Mi Habitación lo mantiene al día; un stage que da un mueble también lo aumenta en el acto. |
| `.myroom_iw`, `.myroom_ih`, `.myroom_items` | contadores | El ancho y el alto de la habitación y el número de muebles colocados, que escribe Mi Habitación para que una lista de partidas guardadas los muestre (solo léelos). |
| `myroom_thumb_<id>` | textura compartida | La imagen de un mueble de la tienda o de recompensa, creada cuando arranca Mi Habitación, que muestran las ventanas emergentes de compra y de recompensa. |

Para dar un mueble desde tu propio stage, suma 1 a ambos contadores, como hacen [Pagoda.Rewards](pagoda.md) y [Vault.Chests](vault.md). Mientras la cadena `my_room` no esté definida, una migración temporal lee la habitación del antiguo almacén de Open-World Memories; se eliminará en la versión oficial.

## En qué se apoya

- Sus menús y diálogos usan la biblioteca de interfaz del skin a través de [UI.Pick](ui.md), elegida en el primer uso (PopUI en Open-World Memories, SimpleUI en un skin que no indica ninguna), y su indicador de monedas es el widget de monedas de esa biblioteca.
- Sus cadenas, incluidos los nombres de los muebles (`lang/<code>/items.json`, con el id del mueble como clave), viven en su propia carpeta `lang/`, en los nueve idiomas.
- El catálogo de muebles es `data/furniture.json`, en la carpeta del stage.
- La matriz de pruebas solo lista los stages de depuración presentes en `Global/Modules/Stages/`. Estos stages de depuración y la ROActivity `video_player` que usa la televisión también son módulos globales.
- La cápsula abre el stage `mysterypod` cuando existe en `Global/Modules/Stages/`, o cuando el propio stage `mysterypod` de un skin establece la cadena compartida `mysterypod_installed` a `"1"` en su `onStart`.
- Las visitas en línea usan el id de habitación `"myroom"` con la versión 2 de su contenido, igual que antes del traslado.
