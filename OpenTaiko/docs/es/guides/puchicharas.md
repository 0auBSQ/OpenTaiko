<!-- guides/puchicharas.md -->

# Añadir un puchichara

Un puchichara es el pequeño compañero que rebota junto al medidor durante la partida. Cada puchichara es una carpeta dentro de `Global/PuchiChara/` en la carpeta de instalación del juego, que contiene una hoja de sprites de dos fotogramas y unos pocos archivos JSON opcionales. No escribes código: crea la carpeta con los nombres de archivo correctos y el juego la detecta en el siguiente arranque.

Compatibilidad: OpenTaiko 0.6.1 sigue cargando sin cambios los puchicharas creados para 0.6.0.

## Antes de empezar

- OpenTaiko 0.6.1 instalado. El juego lee los puchicharas de `Global/PuchiChara/` junto al ejecutable del juego, y todos los skins comparten la carpeta.
- Un editor de imágenes que exporte PNG con transparencia.
- Un editor de texto para los archivos JSON.
- Opcional: un clip `.ogg` corto para `Welcome.ogg`.

## Paso 1: Crear la carpeta

El juego lista las subcarpetas de `Global/PuchiChara/` en el arranque y trata cada una como un puchichara. El nombre de carpeta es la identidad: el juego lo escribe en el archivo de guardado cuando el jugador selecciona el puchichara y registra el desbloqueo bajo él. Elige un nombre estable: tras un renombrado, las selecciones existentes recurren a la primera carpeta y el desbloqueo registrado ya no coincide. Las carpetas incluidas usan un prefijo de ordenación, por ejemplo `00 - None`, `01a - OpenTaiko-Kun` y `02 - Bol`. El juego no ordena la lista, así que el prefijo mantiene predecible el orden del sistema de archivos. La primera carpeta es la alternativa cuando un guardado referencia una carpeta que ya no existe, así que mantén `00 - None` en primer lugar.

```
Global/PuchiChara/
    00 - None/
    01a - OpenTaiko-Kun/
    02 - Bol/
    99 - MyMascot/          <-- tu carpeta nueva
```

## Paso 2: Dibujar la hoja de sprites (Chara.png)

El juego dibuja el compañero a partir de `Chara.png`, una hoja de sprites horizontal. La disposición de los fotogramas proviene del valor de skin `Game_PuchiChara`, que por defecto es `256,256,2` (anchura de fotograma, altura de fotograma, número de fotogramas), así que las hojas incluidas son de 512x256 píxeles: dos fotogramas de 256x256 uno junto a otro, el fotograma 0 a la izquierda. Durante la partida el juego alterna los fotogramas y añade un rebote vertical (rebote en reposo en los menús, rebote sincronizado con el pulso en el juego), así que los dos fotogramas deberían ser dos poses del mismo personaje. Usa un fondo transparente. Si falta `Chara.png`, la entrada se carga igualmente pero no dibuja nada.

Las carpetas incluidas también contienen un `Chara.xcf` (fuente de GIMP) y un `PuchiConfig.txt`. El juego no lee ninguno de los dos.

```ini
Chara.png : 512 x 256 PNG, transparency
  +-----------------+-----------------+
  |    frame 0      |    frame 1      |
  |   256 x 256     |   256 x 256     |
  +-----------------+-----------------+
```

## Paso 3: Escribir Metadata.json

Crea `Metadata.json` con estos campos:

- `name`, `author`, `description`: cada uno una cadena simple o un objeto localizado `{ "strings": { "default": "...", "ja": "...", ... } }` donde `default` es la alternativa y las demás claves son códigos de idioma del juego.
- `rarity`: uno de `Poor`, `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythical`. La rareza establece el color y el nivel de la notificación de desbloqueo. Todas las rarezas tienen un multiplicador de monedas de 1, así que no cambia las ganancias. Un valor desconocido se comporta como `Common`.

Si el archivo está ausente, la entrada se carga con el nombre `(None)`, la rareza `Common` y el autor `(None)`.

```json
{
    "name": {
        "strings": {
            "default": "MyMascot",
            "ja": "マイマスコット"
        }
    },
    "rarity": "Rare",
    "description": {
        "strings": {
            "default": "A friendly companion.\nWaves during play."
        }
    },
    "author": "YourName"
}
```

## Paso 4 (opcional): Añadir Effects.json

`Effects.json` da al puchichara efectos de juego. Todos los campos están desactivados por defecto cuando el archivo está ausente:

- `allpurple` (bool): las notas grandes don y ka se convierten en notas moradas que aceptan cualquiera de los dos tambores.
- `autoroll` (int): golpes automáticos por segundo en redobles y globos. Cualquier valor por encima de 0 pone el multiplicador de monedas a 0.
- `showadlib` (bool): muestra las notas ADLIB ocultas. Multiplica las monedas por 0.9.
- `splitlane` (bool): dibuja las notas don y ka en carriles separados.

Omite el archivo para un compañero puramente cosmético.

```json
{
    "allpurple": false,
    "autoroll": 0,
    "showadlib": false,
    "splitlane": false
}
```

## Paso 5 (opcional): Añadir Welcome.ogg y Render.png

- `Welcome.ogg`: un clip de voz que el juego carga en el grupo de sonido Voice. La pantalla de la habitación integrada del juego lo reproduce cuando el jugador selecciona el puchichara; los stages Lua no pueden acceder a él, así que un skin con su propia pantalla de habitación no lo reproduce.
- `Render.png`: una imagen fija a tamaño completo que los stages Lua pueden dibujar como retrato (la textura `render` de una entrada de `PUCHICHARALIST`). Es una imagen única de cualquier tamaño. Ninguno de los puchicharas incluidos tiene una.

Ambos archivos son opcionales.

```
MyMascot/
    Chara.png       (obligatorio, la hoja de sprites animada)
    Metadata.json   (nombre, rareza, autor, descripción)
    Effects.json    (efectos de juego opcionales)
    Unlock.json     (condición de desbloqueo opcional)
    Welcome.ogg     (clip de voz opcional)
    Render.png      (retrato opcional)
```

## Paso 6 (opcional): Añadir una condición de desbloqueo (Unlock.json)

Sin `Unlock.json` el puchichara está disponible de inmediato. Para bloquearlo, añade un `Unlock.json` con los campos `condition`, `type`, `values` y `references`; el formato y los ids de condición son los mismos que para las canciones y los personajes (consulta la guía de desbloqueables). El ejemplo de abajo se desbloquea una vez que el jugador ha ganado 500 monedas en total. Ejemplos incluidos: OpenTaiko-Kun cuesta 100 monedas (`"condition": "ch"`), Bol requiere 20 charts superados del autor `bol` (`"condition": "sc"`), y Tinyfox requiere una canción jugada del género `Project Outfox Serenity` (`"condition": "sg"`).

El jugador compra las condiciones de monedas en la pantalla de la habitación. El juego comprueba las demás condiciones en la pantalla de resultados tras cada partida; cuando el jugador cumple una, añade el puchichara a la lista de desbloqueados del archivo de guardado y muestra una notificación.

```json
{
    "condition": "ce",
    "type": "me",
    "values": [
        500
    ]
}
```

## Paso 7: Reiniciar y seleccionarlo

Reinicia el juego (o recarga el skin desde los ajustes) para que el juego reconstruya la lista. Abre la pantalla de la habitación y elige la nueva entrada en la lista de puchicharas. Una vez seleccionado aparece durante la partida junto al medidor. Si no aparece ningún compañero durante la partida, comprueba que la opción `Draw PuchiChara` de los ajustes del sistema está activada.

## Resolución de problemas y notas

- Los nombres de archivo son exactos: `Chara.png`, `Metadata.json`, `Effects.json`, `Unlock.json`, `Welcome.ogg`, `Render.png`. El juego ignora un archivo mal nombrado y aplica el valor por defecto.
- Los archivos de guardado y los registros de desbloqueo guardan el nombre de carpeta tal cual. Renombrar una carpeta hace que las selecciones existentes recurran a la primera carpeta.
- El juego corta la hoja de sprites con el tamaño de fotograma `Game_PuchiChara` del skin (por defecto `256,256,2`). Corta una hoja de otro tamaño con los mismos números, así que los fotogramas salen recortados o desalineados. Si un skin sobrescribe `Game_PuchiChara`, ajústate a ese valor. El skin incluido no lo sobrescribe.
- `autoroll` por encima de 0 anula las ganancias de monedas y `showadlib` las reduce a 0.9, así que un compañero cosmético que establezca cualquiera de los dos reduce las ganancias de su jugador.
- Los archivos JSON incluidos contienen comas finales. El analizador JSON del juego las acepta; los validadores estrictos las rechazan.
- El juego construye la lista una vez en el arranque y al recargar el skin. Una carpeta añadida mientras el juego está en ejecución aparece tras el siguiente arranque o recarga del skin.
