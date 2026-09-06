<!-- docs/api/README.md -->

# Referencia de la API

<span class="badge-new">Versión del juego 0.6.1</span>

La referencia de todas las globales que el runtime Lua de OpenTaiko expone a un skin. Elige una categoría en la barra lateral, o empieza por [Módulos y ciclo de vida](activities.md) si todavía no has escrito un módulo.

## Cómo leer una firma

Cada entrada muestra la función tal como la llamas desde Lua.

- Dos puntos significan que llamas a la función sobre un valor, y Lua pasa ese valor como el `self` implícito: llamas a `tex:Draw(x, y)` sobre una textura que cargaste antes.
- Un punto o un nombre suelto es una llamada simple, como `GetSaveFile(0)`.
- El tipo tras la flecha es lo que devuelve la llamada: `TEXTURE:CreateTexture(path) -> texture` devuelve un handle que conservas y dibujas más tarde.

Los nombres de las globales van en mayúsculas (`TEXTURE`, `SOUND`, `INPUT`). El runtime las proporciona dentro de cada script de módulo; nunca las creas tú.

## Categorías

| Categoría | Qué cubre |
| --- | --- |
| [Módulos y ciclo de vida](activities.md) | Los callbacks que recibe un módulo y las utilidades para activities, fondos, transiciones y contadores. |
| [Gráficos y texto](graphics.md) | Texturas, canvas, recorte, renderizado de texto, vídeo, colores y gradientes. |
| [Audio](audio.md) | Carga y reproducción de sonidos. |
| [Entrada](input.md) | Entrada de teclado, mando y puntero, y entrada de texto en pantalla. |
| [Datos y persistencia](data.md) | Datos que sobreviven a un reinicio, carga de JSON e INI, recursos compartidos. |
| [Canciones y charts](songs.md) | La lista de canciones, los nodos de canción y los charts, las puntuaciones y la construcción de dan (exámenes). |
| [Jugadores y perfiles](players.md) | Archivos de guardado, placas de nombre, personajes, puchicharas, estado de la partida, temas e idioma. |
| [Matemáticas](math.md) | Vectores, matrices y cuaterniones. |
| [Red en línea](networking.md) | La global `NET` para las sesiones de OpenTaiko Online. Actualmente experimental. |
| [Motor 3D: mundo del rasterizador](3d.md) | Escenas, objetos, modelos, luces, cámaras, sprites, mapas de alturas y render targets. Actualmente experimental. |
| [Motor 3D: mundo del trazador de rutas](3d-raytrace.md) | El trazador de rutas: materiales, primitivas analíticas y el degradado de cielo. Actualmente experimental. |
| [Motor 3D: físicas](3d-physics.md) | Mundo de físicas, cuerpos y vehículos, colliders, raycasts y búsqueda de rutas. Actualmente experimental. |

## La insignia Experimental

Las funciones marcadas como <span class="badge-exp">Experimental</span> funcionan hoy, pero pueden cambiar entre versiones sin periodo de obsolescencia. El motor 3D y la red en línea llevan actualmente la insignia; el plan es que el motor 3D abandone el estado experimental con la versión 1.0. Si un skin depende de funciones experimentales, pruébalo de nuevo tras cada actualización e indica a qué versión del juego se dirige.
