<!-- guides/unlockables.md -->

# Condiciones de desbloqueo para charts personalizados

Puedes bloquear una canción personalizada tras una condición (un precio en monedas, superar otras canciones, un número total de partidas, una bandera de historia, etc.) colocando un archivo `Unlock.json` en la carpeta de la canción junto a su `.tja` y su `uniqueID.json`. Cuando el juego construye la lista de canciones, lee ese archivo y mantiene la canción bloqueada hasta que el jugador cumple la condición. Las condiciones se asocian a canciones individuales: `box.def` no tiene clave de desbloqueo, así que no puedes bloquear una carpeta de género de esta forma.

## Antes de empezar

- Un chart personalizado que ya aparezca en la lista de canciones: una carpeta dentro de `Songs/` con un `.tja` y, una vez que el juego lo ha escaneado, un `uniqueID.json`.
- Un editor de texto que guarde en UTF-8.
- Para las condiciones que referencian otras canciones: el valor `id` del `uniqueID.json` de cada canción referenciada.
- El juego guarda el progreso de desbloqueo por archivo de guardado, así que debes cumplir la condición en el juego para ver la canción desbloquearse. La opción `Ignore Song Unlockables` de los ajustes de juego trata todas las canciones como desbloqueadas mientras pruebas.

## Paso 1: Crear el archivo Unlock.json

Crea `Unlock.json` en la carpeta de la canción. Todos los campos son opcionales y tienen un valor por defecto:

- `hidden_index` (int, por defecto 0): cómo presenta la lista de canciones la canción bloqueada (véase la tabla siguiente). El juego limita los valores a 0-3.
- `rarity` (string, por defecto `Common`): el nombre de la rareza (véase la tabla siguiente). Establece el color con que se muestra la rareza y el nivel de la notificación de desbloqueo. Un valor vacío se convierte en `Common`.
- `condition` (string, por defecto `ch`): el id de la condición (Paso 2).
- `values` (int array, por defecto `[100]`): los parámetros numéricos de la condición.
- `type` (string, por defecto `me`): la comparación que usan las condiciones basadas en valores: `l` menor que, `le` menor o igual, `e` igual, `me` mayor o igual, `m` mayor que, `d` distinto. Las condiciones de monedas siempre usan `me`.
- `references` (string array, por defecto `[""]`): ids de canción, nombres de género, nombres de autor o nombres de bandera, según la condición.
- `custom_unlock_text` (object, opcional): reemplaza el texto de pista generado. Un objeto localizado `{ "strings": { "default": "...", "ja": "..." } }`; el juego usa la clave del idioma activo, luego `default`, y muestra el texto generado si no existe ninguna.

Las claves van en minúsculas con guiones bajos, exactamente como se listan.

Valores del índice oculto:

| Valor | En la lista de canciones |
| --- | --- |
| 0 | Se muestra con un icono de candado; la vista previa de audio suena |
| 1 | Atenuada; sin vista previa |
| 2 | Atenuada, con el título y la imagen de vista previa ocultos |
| 3 | Oculta hasta que se desbloquea |

Valores de rareza:

| Rareza | Nivel de notificación |
| --- | --- |
| <span class="rarity rarity-poor">Poor</span> | 0 |
| <span class="rarity rarity-common">Common</span> | 0 |
| <span class="rarity rarity-uncommon">Uncommon</span> | 1 |
| <span class="rarity rarity-rare">Rare</span> | 2 |
| <span class="rarity rarity-epic">Epic</span> | 3 |
| <span class="rarity rarity-legendary">Legendary</span> | 4 |
| <span class="rarity rarity-mythical">Mythical</span> | 4 |

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "cm",
  "values": [500],
  "type": "me",
  "references": [""],
  "custom_unlock_text": {
    "strings": {
      "default": "Buy this song for 500 coins.",
      "ja": "500コインで解放できます。"
    }
  }
}
```

## Paso 2: Elegir una condición

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
| `andcomb` | El jugador debe cumplir todas las condiciones hijas (consulta los ejemplos combinados) | `[]` | una condición hija por entrada, como cadena JSON |
| `orcomb` | El jugador debe cumplir al menos una condición hija (consulta los ejemplos combinados) | `[]` | una condición hija por entrada, como cadena JSON |

Valores de estado de clear (un requisito de estado significa este estado o mejor):

| Valor | Estado de clear |
| --- | --- |
| 0 | jugada |
| 1 | clear asistido |
| 2 | clear |
| 3 | full combo |
| 4 | todo perfecto |

Valores de dificultad:

| Valor | Dificultad |
| --- | --- |
| 0 | Easy |
| 1 | Normal |
| 2 | Hard |
| 3 | Extreme |
| 4 | Extra Extreme |

Para `dp`, la dificultad 3 también cuenta los charts Extra Extreme. `dp` y `lp` solo cuentan charts normales y omiten los charts Dan y Torre. `sp` acepta `-1` para indicar cualquier dificultad.

El juego comprueba el número de valores: `ch`/`cs`/`cm`/`ce`/`tp`/`ap`/`aw`/`gt`/`gc` necesitan exactamente 1 valor, `sd` exactamente 2, `dp`/`lp` exactamente 3, y `sp`/`sg`/`sc` necesitan 2 valores por referencia con tantas referencias como pares. Un número incorrecto hace que la condición falle con un mensaje de error en el juego.

Solo una compra explícita en una pantalla que la ofrezca desbloquea una condición de monedas (el skin incluido ofrece las canciones bloqueadas para su compra desde la selección de canciones, sea cual sea el id de monedas que uses). El juego comprueba todas las demás condiciones en la pantalla de resultados tras cada partida; cuando el jugador cumple una, el juego añade el id de la canción al archivo de guardado y muestra una notificación.

## Paso 3: Probarlo en el juego

Inicia el juego y abre la selección de canciones. Según `hidden_index`, la canción muestra un candado, aparece atenuada, oculta su información o está ausente. Cómprala o cumple la condición, y luego confirma que sigue desbloqueada tras un reinicio. Activa `Ignore Song Unlockables` en los ajustes de juego para saltarte todos los bloqueos mientras iteras.

## Ejemplos

Estos son los patrones de condición que usan las canciones incluidas, escritos como archivos `Unlock.json`. Sustituye los ids, los nombres y los números por los tuyos.

**Comprar la canción con monedas (`cm`).** El patrón más común. El precio es el único valor, y el juego ignora `type`. `hidden_index` 0 mantiene la canción visible para que el jugador pueda encontrarla y comprarla. La pista generada ya indica el precio, así que no necesitas `custom_unlock_text`.

```json
{
  "hidden_index": 0,
  "rarity": "Uncommon",
  "condition": "cm",
  "values": [500]
}
```

**Número total de partidas (`tp`).** Los capítulos incluidos abren sus canciones una tras otra con números de partidas crecientes (10, 15, 20, etc.), de modo que el jugador siempre tiene una siguiente canción a su alcance. `ap` (partidas de batalla contra la IA), `aw` (victorias en batalla contra la IA) y `ce` (monedas ganadas en total) usan la misma disposición de valor único.

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "tp",
  "values": [50]
}
```

**Superar una canción concreta (`sp`).** El patrón que la mayoría de las canciones incluidas usan para una secuela o un remix. `-1` como dificultad acepta un clear en cualquier dificultad; el id es el campo `id` del `uniqueID.json` de la canción referenciada (el juego genera un id de 64 caracteres la primera vez que escanea una canción sin uno, y las canciones incluidas pueden llevar un id escrito a mano). El ejemplo se desbloquea cuando el jugador supera la canción referenciada.

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "sp",
  "values": [-1, 2],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"]
}
```

**Hacer full combo en varias canciones (`sp`).** Cada canción adicional añade un par de valores y una referencia, y el jugador debe cumplir el requisito de cada canción referenciada. El ejemplo requiere un full combo (3) en dos canciones.

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sp",
  "values": [-1, 3, -1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU", "4aJ2I19XyEG2cEA9tlmdnZSz2H43OKsVBPLi52UfRhSjDGNgTGGqhmqkbWfPDoyw"]
}
```

**Superar canciones de un género (`sg`).** Cada entrada es `[song count, clear status]` con el nombre del género en `references`. El género es el que el juego registra cuando el jugador juega la canción: el `#GENRE` del `box.def` de la carpeta contenedora, o el `#GENRE` propio del chart cuando la carpeta no tiene ninguno. Los capítulos incluidos usan esto para sus medleys. El ejemplo se desbloquea cuando el jugador supera 10 canciones distintas del género.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sg",
  "values": [10, 2],
  "references": ["OpenTaiko Chapter II"]
}
```

**Superar charts de un autor (`sc`).** La misma disposición con nombres de `#NOTESDESIGNER` como referencias. El ejemplo requiere 5 charts superados de un autor; un segundo autor añade otro par de valores y otra referencia.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sc",
  "values": [5, 2],
  "references": ["bol"]
}
```

**Superar un número de charts distintos (`sd`).** Cuenta todos los charts que el archivo de guardado tiene en el estado indicado o mejor. El ejemplo requiere 10 full combos.

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sd",
  "values": [10, 3]
}
```

**Superar charts de una dificultad o de un nivel de estrellas (`dp` / `lp`).** `dp` cuenta charts de una dificultad, `lp` charts de un nivel de estrellas. El primer ejemplo requiere 20 charts Extreme superados, el segundo un chart de 7 estrellas superado.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "dp",
  "values": [3, 2, 20]
}
```

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "lp",
  "values": [7, 2, 1]
}
```

**Cualquiera de dos condiciones (`orcomb`).** `values` está vacío y cada entrada de `references` es una condición hija completa (`condition`, `type`, `values`, `references`) escrita como cadena JSON. Las canciones incluidas usan esto para ofrecer un atajo: jugar suficientes canciones, o pagar. `orcomb` usa la rama satisfecha más barata cuando calcula el precio de una compra.

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "orcomb",
  "values": [],
  "references": [
    "{\"condition\": \"tp\", \"type\": \"me\", \"values\": [100]}",
    "{\"condition\": \"cm\", \"type\": \"me\", \"values\": [200]}"
  ]
}
```

**Varias condiciones a la vez (`andcomb`).** La misma disposición; el jugador debe cumplir todas las hijas. Las hijas pueden ser a su vez combinaciones, y `andcomb` suma los precios en monedas dentro de una combinación. El ejemplo requiere superar una canción y superar cualquier chart de 7 estrellas.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "andcomb",
  "values": [],
  "references": [
    "{\"condition\": \"sp\", \"type\": \"me\", \"values\": [-1, 2], \"references\": [\"4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU\"]}",
    "{\"condition\": \"lp\", \"type\": \"me\", \"values\": [7, 2, 1], \"references\": [\"\"]}"
  ]
}
```

**Bandera de historia con pista personalizada (`gt`).** `gt` lee una bandera con nombre de sí/no del archivo de guardado. Los scripts Lua (escenas de historia, cinemáticas) establecen las banderas, así que usa `gt` solo cuando un script que controles establezca la bandera. `values` es `[1]` para ON o `[0]` para OFF; `references` contiene el nombre de la bandera. Un desbloqueo de historia no tiene una pista generada que se explique por sí sola, así que `custom_unlock_text` resulta útil aquí. Puedes proporcionar cualquier clave de idioma; `default` es la alternativa.

```json
{
  "hidden_index": 3,
  "rarity": "Legendary",
  "condition": "gt",
  "values": [1],
  "references": ["story_done"],
  "custom_unlock_text": {
    "strings": {
      "default": "Finish the story to unlock this song.",
      "ja": "ストーリーをクリアするとこの曲が解放されます。"
    }
  }
}
```

**Canción secreta con un acertijo.** Las canciones secretas incluidas combinan un `hidden_index` alto (el título y la vista previa permanecen ocultos, o la canción permanece ausente hasta que se desbloquea) con un `custom_unlock_text` que insinúa la condición en lugar de indicarla. Cualquier condición funciona por debajo; el ejemplo esconde un requisito de full combo tras un acertijo.

```json
{
  "hidden_index": 2,
  "rarity": "Epic",
  "condition": "sp",
  "values": [-1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"],
  "custom_unlock_text": {
    "strings": {
      "default": "Perfect the storm that came before this one."
    }
  }
}
```

## Resolución de problemas y notas

- La canción no está bloqueada: el archivo no se llama `Unlock.json`, no está en la carpeta que contiene el `.tja` y el `uniqueID.json`, o `Ignore Song Unlockables` está activado.
- La pista dice que la condición es inválida: el número de valores no coincide con la condición (consulta el Paso 2), o `sp`/`sg`/`sc` tienen un número de referencias distinto del de pares de valores.
- Una clave no tiene efecto: las claves son `hidden_index`, `rarity`, `condition`, `values`, `type`, `references`, `custom_unlock_text`, todas en minúsculas. El juego ignora cualquier otra cosa y aplica el valor por defecto.
- `sp` nunca se desbloquea: la referencia debe ser el id del `uniqueID.json` de la canción objetivo (un título o un nombre de archivo nunca coincide), y el jugador debe poder alcanzar la dificultad y el estado (el estado 3 es un full combo, así que un chart que el jugador solo ha superado no cuenta).
- `sg` nunca se desbloquea: el nombre del género debe coincidir con el género que el juego registra para las canciones jugadas (el `#GENRE` del `box.def` de la carpeta, o el `#GENRE` del chart cuando no hay género de carpeta).
- `gt`/`gc` nunca se desbloquean: nada establece la bandera con nombre. El chart no puede establecerla por sí mismo.
- Nada desbloquea `ig` jugando. Úsalo solo para contenido que conceda algún otro sistema.
- `custom_unlock_text` debe ser un objeto `{ "strings": { ... } }`; una cadena a secas no funciona.
- Los archivos `Unlock.json` incluidos contienen comas finales. El analizador JSON del juego las acepta; los validadores estrictos las rechazan.
