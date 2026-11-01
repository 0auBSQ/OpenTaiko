<!-- guides/notes-skins.md -->

# Añadir paquetes de notas y juicios

Un skin puede ofrecer varios aspectos para las notas y para los textos de juicio, y el jugador elige uno en los ajustes. Cada aspecto es un paquete: una carpeta numerada dentro de `Graphics/5_Game/` del skin. Un paquete de notas contiene las notas, los redobles, los globos, los efectos de golpe y las burbujas de globo y de redoble; un paquete de juicios contiene los textos de juicio. Esta guía explica las carpetas, las hojas que contiene cada paquete, el archivo `Definition.json` que describe un paquete y cómo el juego completa lo que le falta a un paquete.

## Antes de empezar

- Un skin propio (consulta [Añadir skins y temas](skins.md)). El skin incluido `System/Open-World Memories/` tiene cuatro paquetes de notas Taiko, un paquete de notas Bongo y cuatro paquetes de juicios que puedes copiar.
- Un editor de imágenes que guarde archivos PNG con transparencia.
- El tamaño de celda de nota de tu skin: el valor de disposición `Game_Notes_Size=` (`195,195` en el skin incluido). Todas las celdas de las hojas de notas de esta página tienen ese tamaño, salvo las de la hoja gigante.

## Paso 1: Crear las carpetas de paquetes

El juego busca tres carpetas dentro de `Graphics/5_Game/` y trata cada subcarpeta numerada que contienen como un paquete, ordenado por número:

- `Notes Skin (Taiko)/`: paquetes de notas para los jugadores de Taiko.
- `Notes Skin (Bongo)/`: paquetes de notas para los jugadores de Bongo.
- `Judges Skin/`: paquetes de textos de juicio, para todos los jugadores.

El paquete `1` de cada carpeta es el paquete por defecto y la base de los demás (Paso 7). El número de carpeta solo ordena los paquetes; los ajustes muestran el nombre de `Definition.json`.

```
Graphics/5_Game/
  Notes Skin (Taiko)/
    1/                  <- el aspecto por defecto, completo
      Definition.json
      Notes.png
      ...
    2/                  <- otro aspecto: solo los archivos que cambian
      Definition.json
      Notes.png
  Notes Skin (Bongo)/
    1/
  Judges Skin/
    1/
      Definition.json
      Judge.png
```

## Paso 2: Dibujar las hojas de notas Taiko

Una hoja tiene una fila por fotograma de animación (el Paso 5 fija el número de fotogramas), y sus celdas van una junto a otra en el orden de abajo.

| Archivo | Contenido |
| --- | --- |
| `HitCircle.png` | El círculo de golpe, una celda. |
| `Notes.png` | Don, Ka, Don grande, Ka grande, KaDon, ADLib. |
| `Rolls.png` | La cabeza, el cuerpo y la cola del redoble pequeño, luego la cabeza, el cuerpo y la cola del redoble grande. El juego estira la celda del cuerpo a lo largo del redoble. |
| `Balloons.png` | El globo (2 celdas de ancho), el kusudama (2 celdas de ancho), luego la cabeza, el cuerpo y la cola del redoble de mecha. Los jugadores de Bongo también ven estos globos. |
| `ChipEffect.png` | Opcional. El destello que aparece donde aterriza una nota golpeada, con las columnas de `Notes.png`, una fila. Sin este archivo, el juego hace destellar la propia nota. |
| `Notes_Partner.png` | Opcional. El aspecto de nota compañera de cada columna de `Notes.png` (Paso 6). |
| `Notes_Giant.png` | Opcional. El Don, el Ka, el KaDon y el ADLib gigantes (Paso 6). |
| `Preview.png` | Opcional. La imagen que los ajustes muestran para el paquete. Sin este archivo, muestran `Notes.png`. |

El paquete también contiene los efectos de golpe y las burbujas de globo y de redoble:

| Archivo | Contenido |
| --- | --- |
| `Hit/Explosion.png` | El estallido sobre el círculo de golpe, 7 fotogramas cuadrados por fila: una fila para Bien, una fila para Vale y luego, de forma opcional, una fila para Bien y una fila para Vale en notas grandes. Con 2 filas, las notas grandes usan las dos primeras. |
| `Hit/Explosion_Big.png` | El estallido adicional sobre una nota grande, una sola imagen, dibujada para ambos juicios. |
| `Hit/FireWorks.png` | Las chispas que deja tras de sí una nota grande en vuelo. |
| `Hit/Bomb.png` | La explosión de una bomba golpeada o de un redoble de mecha fallido, 7 fotogramas en una fila. |
| `Hit/Great/`, `Hit/Great_Big/`, `Hit/Good/`, `Hit/Good_Big/` | Carpetas de fotogramas opcionales (`0.png` a `14.png`) que el juego dibuja en el carril para un Bien (`Great`) o un Vale (`Good`) en una nota pequeña o grande. |
| `Kusudama/` | El `Script.lua` del kusudama y sus imágenes, incluido `Kusudama_Number.png`. |
| `Fuseroll/` | `Fuse.png`, la burbuja del redoble de mecha, y `Number_Fuse.png`, sus dígitos. |
| `Breaking.png` | Las imágenes de la burbuja de globo al reventar: 6 imágenes de la misma altura, una encima de otra. |
| `Balloon.png` | La burbuja de globo. |
| `Roll.png` | La burbuja del contador de redobles, con sus fotogramas de animación uno junto a otro. |

## Paso 3: Dibujar un paquete de notas Bongo

Un paquete Bongo contiene solo lo que cambia para los jugadores de Bongo:

| Archivo | Contenido |
| --- | --- |
| `Notes.png` | Rojo, amarillo, rosa, aplauso, ADLib. |
| `Rolls.png` | Los redobles rojo, rosa, amarillo y de aplauso, cada uno como cabeza, cuerpo y cola. |
| `HitCircle.png` | Opcional. Sin este archivo, los jugadores de Bongo ven el círculo de golpe del paquete Taiko. |
| `ChipEffect.png`, `Notes_Partner.png` | Opcional, con las columnas de este `Notes.png`. |
| `Notes_Giant.png` | Opcional, con las columnas de este `Notes.png`. |
| `Preview.png` | Opcional. |

Los globos, los efectos de golpe y las burbujas siempre vienen del paquete Taiko seleccionado.

## Paso 4: Dibujar un paquete de juicios

Un paquete de juicios contiene `Judge.png`, 5 filas de la misma altura: Bien, Vale, Mal, ADLib y el Boom de la bomba. Su `Definition.json` solo necesita un `Name`. Deja una fila transparente para ocultar ese juicio; los paquetes "(Without Good)" del skin incluido lo hacen con la fila de Bien.

## Paso 5: Escribir Definition.json

`Definition.json` da nombre al paquete y describe sus hojas. Todos los campos son opcionales; un paquete sin este archivo muestra su número de carpeta como nombre y usa los valores por defecto de abajo.

```json
{
	"Name": { "strings": { "default": "Sky Spirits", "ja": "スカイスピリッツ" } },
	"Notes": {
		"Frames": 3,
		"Animation": [
			{ "FromCombo": 0, "Rows": [0] },
			{ "FromCombo": 50, "Rows": [1, 1, 0, 0], "RowsOnEntry": [0] },
			{ "FromCombo": 150, "Rows": [0, 1] },
			{ "FromCombo": 300, "Rows": [0, 2], "RowsOnEntry": [0, 1] }
		]
	},
	"Rolls": { "Frames": 3, "Animation": [] },
	"Balloons": { "Frames": 3, "Animation": [] },
	"FlyingNotesFrame": 1,
	"PartnerNotesScale": 1.4142,
	"GiantNotesScale": 2.0
}
```

| Campo | Por defecto | Significado |
| --- | --- | --- |
| `Name` | el número de carpeta | El nombre en los ajustes, una cadena por código de idioma, con `default` para cualquier otro idioma. |
| `Notes`, `Rolls`, `Balloons` | 1 fotograma, sin animación | El número de filas de la hoja (`Frames`) y su animación (`Animation`). `Notes` también fija las filas de `Notes_Partner.png` y `Notes_Giant.png`. |
| `FlyingNotesFrame` | `0` | La fila de `Notes.png` de las notas que vuelan al medidor tras un golpe. |
| `PartnerNotesScale` | la raíz cuadrada de 2 | La escala de una nota compañera pequeña cuando el paquete no tiene `Notes_Partner.png` (Paso 6). |
| `GiantNotesScale` | `2` | La escala de una nota gigante cuando el paquete no tiene `Notes_Giant.png` (Paso 6). |

`Animation` es una lista de niveles. El juego usa el nivel con el `FromCombo` más alto que haya alcanzado el combo del jugador. Un pulso tiene 4 subdivisiones, y `Rows` da la fila dibujada en cada subdivisión; una lista más corta se repite, así que `[0, 1]` alterna en cada subdivisión y `[0]` se queda quieta. `RowsOnEntry` sustituye a `Rows` durante unos dos tercios de segundo después de que el combo alcance ese nivel. Sin niveles, la hoja muestra su primera fila. La tabla del ejemplo es la animación que el juego siempre ha usado, y el Modo Simple de los ajustes mantiene cada hoja en su primera fila.

Dos campos opcionales ajustan un nivel. `FromComboByDifficulty` fija el combo del nivel para algunas dificultades, con las claves `Easy`, `Normal`, `Hard`, `Extreme` y `ExtraExtreme`; las demás dificultades usan `FromCombo`, así que `"FromCombo": 100, "FromComboByDifficulty": { "Easy": 30 }` empieza el nivel a 30 de combo en Easy y a 100 en las demás. Un chart Torre cuenta como Normal en su lado Normal y como Extreme en sus otros lados, y un Dan usa la dificultad de la canción que se está jugando.

`RowsByNote` da a algunas notas sus propias `Rows`, con los nombres de abajo como claves; las demás notas conservan `Rows`. En `Notes.png`, `"Rows": [0, 2], "RowsByNote": { "BigDon": [1, 2] }` alterna las filas 0 y 2 en todas las notas salvo el Don grande, que alterna las filas 1 y 2. El cuerpo y la cola de un redoble siguen a su cabeza, y `RowsOnEntry` sigue aplicándose a todas las notas.

| Hoja | Nombres de notas |
| --- | --- |
| `Notes.png`, Taiko | `Don`, `Ka`, `BigDon`, `BigKa`, `KaDon`, `ADLib` |
| `Notes.png`, Bongo | `Red`, `Yellow`, `Pink`, `Clap`, `ADLib` |
| `Rolls.png`, Taiko | `Roll`, `BigRoll` |
| `Rolls.png`, Bongo | `RedRoll`, `PinkRoll`, `YellowRoll`, `ClapRoll` |
| `Balloons.png` | `Balloon`, `Kusudama`, `FuseRoll` |

## Paso 6: Notas compañeras y gigantes

Una nota compañera conserva su tamaño en la mayoría de los casos, pero las pequeñas (un Don o Ka pequeño, un ADLib, una bomba, todas las notas Bongo) toman un aspecto grande. Una nota gigante es mucho más grande y se tambalea despacio. Cada una tiene su propia hoja opcional:

- `Notes_Partner.png` tiene las columnas de `Notes.png` y el mismo número de filas. Una nota compañera pequeña usa la celda de su propia columna.
- `Notes_Giant.png` tiene el mismo número de filas que `Notes.png`. En un paquete Taiko sus columnas son Don, Ka, KaDon y ADLib, porque una nota gigante se ve grande sea cual sea su tamaño: un Don pequeño y un Don grande usan los dos la celda de Don. En un paquete Bongo sus columnas son las de `Notes.png`. El tamaño de celda es el ancho de la hoja dividido entre su número de columnas, así que una celda gigante puede ser mayor que una celda de nota.

Sin estas hojas, el juego construye los aspectos a partir de `Notes.png`:

| Nota | Compañera, sin `Notes_Partner.png` | Gigante, sin `Notes_Giant.png` |
| --- | --- | --- |
| Don o Ka pequeño de Taiko | El Don o Ka grande | El Don o Ka grande, por `GiantNotesScale` |
| Cualquier otra nota pequeña | La nota, por `PartnerNotesScale` | La nota, por `PartnerNotesScale` y `GiantNotesScale` |
| Don grande, Ka grande, KaDon | Sin cambios | La nota, por `GiantNotesScale` |

Fija ambas escalas aunque el paquete tenga las dos hojas: así, quien copie el paquete y quite una hoja obtiene un tamaño razonable. Una nota escalada es una imagen estirada y se ve más suave que una celda dibujada a su tamaño.

<div class="callout warn">El juego lee Notes_Partner.png, Notes_Giant.png y ChipEffect.png solo del paquete seleccionado. Deben coincidir con las notas propias del paquete, así que las copias del paquete 1 nunca los sustituyen.</div>

## Paso 7: Dejar que el paquete 1 complete el resto

Cuando al paquete seleccionado le falta un archivo, el juego lo toma del paquete 1 de la misma carpeta. Por eso un paquete puede contener solo lo que cambia: un paquete que solo vacía una fila de `Hit/Explosion.png` contiene un `Definition.json` y ese único archivo. Las reglas:

- El juego comprueba cada archivo por separado, también dentro de carpetas (`Hit/Great/3.png`). El kusudama es la excepción: un paquete sin su propia carpeta `Kusudama/` usa la carpeta entera del paquete 1.
- El recurso alternativo solo llega al paquete 1. Un paquete que varía el paquete 3 debe llevar sus propias copias de las hojas del paquete 3.
- `Definition.json` nunca se toma del paquete 1, ni tampoco las tres hojas del Paso 6.
- Un paquete Bongo recurre al paquete Bongo 1.

## Paso 8: Elegir un paquete

Cuando un skin tiene al menos 2 paquetes en una de las tres carpetas, la página del tema de los ajustes muestra primero una sección Apariencia de notas, con una opción para esa carpeta y la vista previa del paquete debajo. El juego carga al momento el paquete elegido y recuerda la elección para el skin en `ThemeSettings.db3`. Con un solo paquete la opción no aparece, y el juego usa ese paquete.

Un módulo Lua lee un archivo del paquete seleccionado con `THEME:GetNotesSkinFile(kind, file)` (consulta [Jugadores y perfiles](../api/players.md)). La selección de canciones incluida lo usa para dibujar los textos de juicio de su lista de repeticiones:

```lua
local file = THEME:GetNotesSkinFile("judges", "Judge.png")   -- "5_Game/Judges Skin/2/Judge.png", o ""
if file == "" then file = "5_Game/Judge.png" end              -- un skin sin paquetes de juicios
```

## Skins sin paquetes

Un skin sin carpeta `Notes Skin (Taiko)/` conserva los archivos que tenía antes de que existieran los paquetes: `Notes.png` (el círculo de golpe, las 4 notas, los redobles y el globo), `Swap.png` (KaDon), `Adlib.png`, `Kusu.png`, `FuseRoll.png`, `ChipEffect.png`, la carpeta `10_Effects/Hit/` y los archivos de globo de `11_Balloon/`. Sin carpeta `Notes Skin (Bongo)/`, las notas y los redobles Bongo vienen de `Notes_Konga.png`, y sin carpeta `Judges Skin/` los juicios vienen de `5_Game/Judge.png`. Estos archivos no tienen hojas de notas compañeras ni gigantes, así que las notas compañeras y gigantes usan los aspectos escalados del Paso 6.

## Resolución de problemas y notas

- Los ajustes no muestran la opción: la carpeta contiene un solo paquete, o las carpetas de paquetes no tienen números como nombre.
- Un paquete muestra las notas de otro paquete: el recurso alternativo tomó el archivo del paquete 1 porque al paquete le falta, o el nombre del archivo difiere en mayúsculas y minúsculas en un sistema que las distingue.
- Las notas muestran el fotograma equivocado o una imagen cortada: `Frames` no coincide con la altura de la hoja dividida entre la altura de celda.
- Las notas compañeras o gigantes de un paquete copiado se ven como las antiguas: la copia conservó `Notes_Partner.png` o `Notes_Giant.png` del original.
- Los jugadores de Bongo no ven notas: falta `Notes.png` tanto en el paquete Bongo seleccionado como en el paquete Bongo 1.
