<!-- guides/nameplates.md -->

# Añadir placas de nombre

Un título de placa de nombre es la placa de texto que aparece en la placa de nombre de un jugador junto a su nombre y su título de dan. Los jugadores desbloquean títulos jugando, comprándolos o recibiéndolos de un script, y equipan uno en Mi Habitación. Puedes añadir tus propios títulos con un paquete de placas de nombre: una carpeta con un archivo `Nameplates.json` que colocas en `Global/Nameplates/`. Un skin puede traer su propio paquete. El juego lista las placas de nombre de cada paquete junto a las incluidas, en todas las pantallas que muestran placas de nombre.

## Antes de empezar

- Un editor de texto que guarde en UTF-8.
- Un id de paquete: un nombre corto de letras minúsculas, dígitos y `_` que ningún otro paquete use, por ejemplo tu nombre o el de tu paquete de charts. El juego construye a partir de él el id de cada placa de nombre del paquete.
- El juego lee los paquetes al arrancar y al cargar un skin. Reinicia el juego tras cada cambio para verlo.

## Paso 1: Crear la carpeta del paquete

Crea una carpeta dentro de `Global/Nameplates/`, junto al juego. El nombre de la carpeta es libre. El paquete contiene un archivo obligatorio y una carpeta `Lang/` opcional para las traducciones (Paso 3):

```
Global/
  Nameplates/
    Example Pack/
      Nameplates.json
      Lang/
        fr.json
        ja.json
```

Un skin incluye su paquete de la misma forma, como una carpeta `Nameplates/` en la raíz del skin (`System/<skin>/Nameplates/Nameplates.json`). El juego solo carga el paquete del skin activo, así que las placas de nombre del paquete de un skin desaparecen mientras otro skin está activo.

## Paso 2: Escribir Nameplates.json

`Nameplates.json` contiene el id del paquete y la lista de placas de nombre:

- `pack` (string, obligatorio): el id del paquete.
- `name` (object, opcional): el nombre del paquete, que aparece en la barra de paquetes del ordenador de Mi Habitación, como `{ "strings": { "default": "...", "<code>": "..." } }`: la barra muestra el nombre del idioma actual o, si no lo hay, `default`. Sin él, la barra de paquetes muestra el id del paquete.
- `nameplates` (array, obligatorio): un objeto por placa de nombre, con estos campos:
  - `key` (string, obligatorio): un nombre para la placa de nombre, único dentro del paquete. El id completo de la placa es `<pack>:<key>`, por ejemplo `example_pack:night_sky`. Los archivos de guardado almacenan este id, así que no cambies una clave una vez que los jugadores puedan tener la placa, y nunca se la des a otra placa.
  - `title` (string, obligatorio): el texto del título. Todos los idiomas sin traducción lo muestran.
  - `type` (int, por defecto 0): el arte de la placa, un número de carpeta en las texturas de placas de nombre del skin. En el skin incluido las carpetas van de `Modules/ROActivities/nameplate/Textures/Title/0` a `45`, y un número sin carpeta no dibuja ninguna placa.
  - `rarity` (string, por defecto `Common`): el nombre de la rareza (véase la tabla siguiente). Un nombre desconocido se convierte en `Common`.
  - `unlock` (object, opcional): la condición de desbloqueo, con los mismos campos que un `Unlock.json`: `condition`, `values`, `type` y `references` (consulta la guía [Desbloqueables de charts](unlockables.md) para los ids de condición). Sin `unlock`, todos los jugadores poseen la placa de nombre desde el principio.

Valores de rareza (las estrellas son las del skin incluido):

| Rareza | Estrellas en la placa | Nivel de notificación |
| --- | --- | --- |
| <span class="rarity rarity-poor">Poor</span> | 0 | 0 |
| <span class="rarity rarity-common">Common</span> | 0 | 0 |
| <span class="rarity rarity-uncommon">Uncommon</span> | 0 | 1 |
| <span class="rarity rarity-rare">Rare</span> | 1 | 2 |
| <span class="rarity rarity-epic">Epic</span> | 2 | 3 |
| <span class="rarity rarity-legendary">Legendary</span> | 3 | 4 |
| <span class="rarity rarity-mythical">Mythical</span> | 4 | 4 |

El juego comprueba las condiciones de desbloqueo en la pantalla de resultados tras cada partida. Cuando el jugador cumple una, el juego desbloquea la placa de nombre y muestra una notificación. Las condiciones de monedas (`ch`, `cs`, `cm`) solo se desbloquean mediante una compra: en el skin incluido, el jugador compra la placa de nombre en el ordenador de Mi Habitación. El ordenador de Mi Habitación también deja fuera de su lista las placas Mythical bloqueadas hasta que el jugador las posee.

```json
{
  "pack": "example_pack",
  "name": { "strings": { "default": "Example Pack", "fr": "Pack d'exemple" } },
  "nameplates": [
    {
      "key": "night_sky",
      "title": "Night Sky Walker",
      "type": 8,
      "rarity": "Rare",
      "unlock": { "condition": "tp", "values": [50] }
    },
    {
      "key": "hello",
      "title": "Hello There"
    }
  ]
}
```

## Paso 3: Añadir traducciones

Las traducciones son opcionales. Cada archivo de la carpeta `Lang/` del paquete contiene los títulos de un idioma, una línea por placa de nombre, con las claves del paquete (sin el id del paquete). El nombre del archivo es el código del idioma:

| Archivo | Idioma |
| --- | --- |
| `de.json` | Alemán |
| `en.json` | Inglés |
| `es.json` | Español |
| `fr.json` | Francés |
| `ja.json` | Japonés |
| `ko.json` | Coreano |
| `nl.json` | Neerlandés |
| `ru.json` | Ruso |
| `zh.json` | Chino |

```json
{
  "night_sky": "Promeneur du ciel nocturne",
  "hello": "Bonjour"
}
```

Una placa de nombre ausente del archivo de un idioma muestra su `title` en ese idioma.

## Paso 4: Probarlo en el juego

Inicia el juego y abre el ordenador de Mi Habitación. En la pestaña Placas de nombre, elige tu paquete en la barra de paquetes que hay sobre la lista: la lista muestra las placas de nombre del paquete, las bloqueadas con su pista de desbloqueo. Equipa una y comprueba su título, su placa y sus estrellas en la placa de nombre del jugador. Cambia el idioma del juego para comprobar las traducciones.

El juego omite un paquete que no puede leer y muestra una notificación de error que lo nombra. Un paquete sin un id `pack` válido, o con el id de un paquete que el juego ya cargó, cuenta como ilegible. El juego omite una placa de nombre suelta sin `key` o sin `title`, o con una clave que ya existe en el paquete, y escribe el motivo en `OpenTaiko.log`.

## Placas de nombre desde un script

Los scripts Lua trabajan con los mismos ids. Todos los métodos que reciben un id de placa de nombre aceptan el id completo como cadena; las placas incluidas también aceptan su número. Un script puede dar una placa de nombre con la condición `ig`, que el juego nunca desbloquea al jugar:

```lua
local save = GetSaveFile(0)
local id = "example_pack:night_sky"
if not save:IsNameplateUnlocked(id) then
    save:UnlockNameplate(id)
end
save:ChangeNameplate(id)
```

UnlockNameplate no muestra ninguna notificación, así que un script que quiera una la muestra por su cuenta. Consulta [Jugadores y perfiles](../api/players.md) para los métodos de placas de nombre.

## Ejemplos

Estos paquetes muestran patrones habituales. Sustituye los ids, los títulos y los números por los tuyos.

**Una placa de nombre gratuita.** Sin `unlock`, todos los jugadores poseen la placa de nombre y pueden equiparla de inmediato.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "welcome", "title": "Welcome Aboard", "type": 0, "rarity": "Common" }
  ]
}
```

**Desbloquear jugando (`tp`).** La placa de nombre se desbloquea tras 100 partidas. `ap` (partidas de batalla contra la IA), `aw` (victorias en batalla contra la IA) y `ce` (monedas ganadas en total) usan la misma disposición de valor único.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "regular", "title": "Regular Drummer", "type": 3, "rarity": "Uncommon",
      "unlock": { "condition": "tp", "values": [100] } }
  ]
}
```

**Superar una canción de tu paquete de charts (`sp`).** Un paquete de charts puede dar recompensas por sus propias canciones. La referencia es el campo `id` del `uniqueID.json` de la canción, y `[3, 3]` pide un full combo (3) en Extreme (3).

```json
{
  "pack": "example_charts",
  "nameplates": [
    { "key": "storm_fc", "title": "Storm Chaser", "type": 33, "rarity": "Epic",
      "unlock": { "condition": "sp", "values": [3, 3],
                  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"] } }
  ]
}
```

**Comprarla con monedas (`ch`).** El precio es el único valor. En el skin incluido, el jugador compra la placa de nombre en el ordenador de Mi Habitación.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "golden", "title": "Golden Touch", "type": 11, "rarity": "Rare",
      "unlock": { "condition": "ch", "values": [800] } }
  ]
}
```

**Una recompensa que solo da un script (`ig`).** El juego nunca desbloquea una placa de nombre `ig` al jugar, así que un script la da con UnlockNameplate (consulta Placas de nombre desde un script). Hasta entonces, el ordenador de Mi Habitación muestra para ella la pista genérica de condición inválida.

```json
{
  "pack": "example_story",
  "nameplates": [
    { "key": "finale", "title": "Curtain Call", "type": 19, "rarity": "Legendary",
      "unlock": { "condition": "ig", "values": [] } }
  ]
}
```

**Un paquete de skin con traducciones.** Un skin guarda su paquete en `System/<skin>/Nameplates/`, con sus traducciones en `System/<skin>/Nameplates/Lang/`.

```json
{
  "pack": "example_skin",
  "nameplates": [
    { "key": "visitor", "title": "First Visit", "type": 22, "rarity": "Common",
      "unlock": { "condition": "tp", "values": [1] } }
  ]
}
```

```json
{
  "visitor": "初めての訪問"
}
```

## Resolución de problemas y notas

- El paquete no aparece: la carpeta no está directamente dentro de `Global/Nameplates/`, el archivo no se llama `Nameplates.json`, falta el id `pack` o usa caracteres distintos de letras minúsculas, dígitos y `_`, u otro paquete ya usa el mismo id. La notificación de error nombra el paquete.
- Falta una placa de nombre en la lista: no tiene `key` o no tiene `title`, o su clave repite una anterior. `OpenTaiko.log` nombra la entrada.
- Una traducción no aparece: la clave del archivo `Lang` debe ser la clave del paquete sin el id del paquete (`night_sky` para `example_pack:night_sky`), y el nombre del archivo debe ser el código del idioma.
- La placa no muestra arte: el skin no tiene carpeta para ese `type`.
- En línea, los demás jugadores solo ven el título de una placa de nombre de un paquete cuando tienen el mismo paquete. En caso contrario, ven la placa de nombre del jugador sin título.
- Si retiras un paquete, los jugadores que llevaban una de sus placas de nombre no muestran ningún título hasta que el paquete vuelve. Sus desbloqueos permanecen en el archivo de guardado.
- El lector JSON del juego acepta comentarios y comas finales; los validadores estrictos los rechazan.
