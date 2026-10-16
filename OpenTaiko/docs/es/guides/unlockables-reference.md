<!-- guides/unlockables-reference.md -->

# Referencia de desbloqueables

Las canciones, los personajes, los puchicharas y las placas de nombre usan un mismo formato de condición de desbloqueo. Las canciones, los personajes y los puchicharas lo guardan en un archivo `Unlock.json`, y las placas de nombre en el objeto `unlock` de su entrada en `Nameplates.json`. Las guías explican dónde va cada archivo: [Desbloqueables de charts](unlockables.md), [Añadir personajes](characters.md), [Añadir puchicharas](puchicharas.md) y [Añadir placas de nombre](nameplates.md). Esta página enumera los campos, los ids de condición y los números que usan.

## Campos de la condición

| Campo | Tipo | Significado |
| --- | --- | --- |
| `condition` | string | El id de la condición (consulta Ids de condición). |
| `values` | int array | Los parámetros numéricos de la condición. |
| `type` | string | La comparación que usan las condiciones basadas en valores (consulta Tipos de comparación). |
| `references` | string array | Ids de canción, nombres de género, nombres de autor o nombres de bandera, según la condición. |

El `Unlock.json` de una canción añade sus propios campos (`hidden_index`, `rarity`, `custom_unlock_text`), descritos en la guía [Desbloqueables de charts](unlockables.md).

## Ids de condición

| Id | Significado | `values` | `references` |
|----|---------|----------|--------------|
| `ch`, `cs`, `cm` | Compra con monedas. El juego evalúa los tres ids de forma idéntica (precio en monedas, `type` forzado a `me`, nunca se concede automáticamente). `cm` es el id pensado para canciones; los scripts del skin pueden leer el id y usarlo para decidir dónde ofrecen la compra. | `[price]` | sin uso |
| `ce` | Monedas ganadas en total desde la creación del archivo de guardado | `[coins]` | sin uso |
| `tp` | Número total de partidas | `[plays]` | sin uso |
| `ap` | Número de partidas de batalla contra la IA | `[plays]` | sin uso |
| `aw` | Número de victorias en batalla contra la IA | `[wins]` | sin uso |
| `sd` | Charts distintos que alcanzaron un estado de clear | `[chart count, clear status]` | sin uso |
| `dp` | Charts de una dificultad que alcanzaron un estado de clear | `[difficulty, clear status, chart count]` | sin uso |
| `lp` | Charts de un nivel de estrellas que alcanzaron un estado de clear | `[level, clear status, chart count]` | sin uso |
| `sp` | Canciones concretas que alcanzaron un estado de clear | `[difficulty, clear status]` por canción (`-1` = cualquier dificultad) | un id de canción por par |
| `sg` | Canciones dentro de géneros con nombre que alcanzaron un estado de clear | `[song count, clear status]` por género | un nombre de género por par |
| `sc` | Charts de autores con nombre que alcanzaron un estado de clear | `[chart count, clear status]` por autor | un nombre de autor por par |
| `gt` | Trigger global (una bandera con nombre de sí/no en el archivo de guardado que establecen los scripts) | `[1]` para ON, `[0]` para OFF | `[trigger name]` |
| `gc` | Contador global (un número con nombre en el archivo de guardado que establecen los scripts) | `[value]`, comparado con `type` | `[counter name]` |
| `ig` | Imposible de obtener: nunca se desbloquea | ninguno | ninguno |
| `andcomb` | El jugador debe cumplir todas las condiciones hijas (consulta los ejemplos combinados de la guía [Desbloqueables de charts](unlockables.md)) | `[]` | una condición hija por entrada, como cadena JSON |
| `orcomb` | El jugador debe cumplir al menos una condición hija (consulta los ejemplos combinados de la guía [Desbloqueables de charts](unlockables.md)) | `[]` | una condición hija por entrada, como cadena JSON |

El juego comprueba el número de valores: `ch`/`cs`/`cm`/`ce`/`tp`/`ap`/`aw`/`gt`/`gc` necesitan exactamente 1 valor, `sd` exactamente 2, `dp`/`lp` exactamente 3, y `sp`/`sg`/`sc` necesitan 2 valores por referencia con tantas referencias como pares. Un número incorrecto hace que la condición falle con un mensaje de error en el juego.

Solo una compra explícita en una pantalla que la ofrezca desbloquea una condición de monedas. El juego comprueba todas las demás condiciones en la pantalla de resultados tras cada partida; cuando el jugador cumple una, el juego desbloquea el objeto y muestra una notificación.

## Tipos de comparación

| `type` | Comparación |
| --- | --- |
| `l` | menor que |
| `le` | menor o igual |
| `e` | igual |
| `me` | mayor o igual (por defecto) |
| `m` | mayor que |
| `d` | distinto |

Las condiciones de monedas siempre usan `me`, sea cual sea el valor de `type`.

## Valores de estado de clear

Un requisito de estado significa este estado o mejor.

| Valor | Estado de clear |
| --- | --- |
| 0 | jugada |
| 1 | clear asistido |
| 2 | clear |
| 3 | full combo |
| 4 | todo perfecto |

## Valores de dificultad

| Valor | Dificultad |
| --- | --- |
| 0 | Easy |
| 1 | Normal |
| 2 | Hard |
| 3 | Extreme |
| 4 | Extra Extreme |

Para `dp`, la dificultad 3 también cuenta los charts Extra Extreme. `dp` y `lp` solo cuentan charts normales y omiten los charts Dan y Torre. `sp` acepta `-1` para indicar cualquier dificultad.

## El valor de los mods de juego

El archivo de guardado conserva las mejores partidas de un jugador por chart, dificultad y mods de juego. El valor de los mods de juego empaqueta los mods de una partida en 8 bytes, empezando por el byte más bajo: una partida sin mods da 8925478921.

| Byte | Mod | Valores |
| --- | --- | --- |
| 1 (el más bajo) | Velocidad de desplazamiento | 0 a 99: 9 es 1.0x, cada paso es 0.1x |
| 2 | Stealth | 0 Off, 1 Doron, 2 Stealth, 3 Hidden, 4 Flashlight |
| 3 | Aleatorio | 0 Off, 1 Random, 2 Mirror, 3 SuperRandom, 4 MirrorRandom |
| 4 | Velocidad de la canción | 2 a 200: 20 es 1.0x, cada paso es 0.05x |
| 5 | Ventana de juicio | 0 Loose, 1 Lenient, 2 Normal, 3 Strict, 4 Rigorous |
| 6 | Mod de juicio | 0 desactivado, 1 Just, 2 Safe |
| 7 | Sin uso | 0 |
| 8 (el más alto) | Mod fun | 0 None, 1 Avalanche, 2 Minesweeper, 3 Dynamic Beat |

Los mods especiales (Flawless, Timed) no forman parte del valor.
