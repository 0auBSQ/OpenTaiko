<!-- modules/pagoda.md -->

# Pagoda

La Pagoda de lo Desconocido es un ascenso por pisos de dan: cada piso sortea tres canciones (y a veces una cuarta, morada) de sus conjuntos, con tres exámenes que se endurecen a medida que suben los pisos. Los módulos `Pagoda` contienen las reglas, los datos y las claves de guardado; no dibujan nada. Un skin dibuja sus propias pantallas de la pagoda encima de ellos, y todos los skins comparten un mismo progreso, porque el progreso vive en el archivo de guardado. El stage `pagoda` de Open-World Memories es uno de esos clientes.

Los pisos 1 a 10 se llaman 道一 a 道十, los 11 a 20 扉一 a 扉十, y a partir del 21 los tejados 穹天1, 穹天2, etc. Los nombres son el título del dan, iguales en todos los idiomas.

## Los módulos

| Módulo | Qué contiene |
| --- | --- |
| `Pagoda.Levels` | Todo lo que se deriva de un número de piso: los conjuntos de canciones, el nombre, el marco y el color de la placa de dan, los umbrales de los exámenes, la probabilidad de la canción morada. Consultas puras. |
| `Pagoda.Floor` | Las canciones de un piso (un sorteo) y su dan: entrega un sorteo a `DANBUILDER` con la placa y los exámenes. |
| `Pagoda.Rewards` | Lo que da un clear: su placa de nombre, monedas, muebles de Mi Habitación, llaves de la bóveda; los guarda y muestra las ventanas emergentes a través de la ROActivity `modal`. |
| `Pagoda.Progress` | Los datos de guardado persistentes (más abajo). |
| `Pagoda.Songs` | La lista de canciones de la que saca la pagoda, y la comprobación de que la banda sonora cubre todas las entradas de los conjuntos. |
| `Pagoda.Run` | Opcional: el flujo de un intento de desafío y de la práctica, sin pantalla. |
| `Pagoda.Texts` | Las cadenas propias de los módulos (`lang/<code>/pagoda.json`). |

## Pagoda.Levels

| Función | Descripción |
| --- | --- |
| `Levels.get_pool(level)  -> pool` | El conjunto del piso, listas `{ blue, green, red, purple }` de `{ id, diff }` (el id único de una canción y una dificultad, de 0 a 4). Un piso sin entrada propia usa la más cercana por debajo. |
| `Levels.load_pools()`, `Levels.clear_pools()` | Lee `pools.json` (una vez); lo olvida, tras una nueva enumeración de canciones. |
| `Levels.level_name(level)  -> string` | El nombre del piso. |
| `Levels.level_tick(level)`, `Levels.level_tick_color(level)` | El marco de la placa de dan (de 0 a 4) y su color `{ r, g, b }`. |
| `Levels.exam1_gauge(level)  -> red, gold` | El examen de la barra. |
| `Levels.exam2(level)  -> type, red, gold, lessThan` | El examen de precisión (pisos 1 a 20), o el número de Ok (a partir del 21). |
| `Levels.exam3_base(level)  -> red, gold` | El examen del número de Bad antes de la tolerancia por la canción morada. |
| `Levels.purple_prob(level)  -> number` | La probabilidad de una cuarta canción morada (el piso 20 siempre tiene una). |
| `Levels.pick_entry(entries, used)`, `Levels.shuffle(t)` | Las utilidades que usan los sorteos. |

`pools.json`, junto a los módulos, asocia cada número de piso con su conjunto:

```json
{
  "1": {
    "comment": "一題",
    "blue":   [ { "id": "<song unique id>", "diff": 0 } ],
    "green":  [ { "id": "<song unique id>", "diff": 0 } ],
    "red":    [ { "id": "<song unique id>", "diff": 0 } ],
    "purple": [ { "id": "<song unique id>", "diff": 0 } ]
  }
}
```

Los conjuntos nombran canciones de la banda sonora de OpenTaiko. `Songs.count_missing_songs()` registra y cuenta las entradas que les faltan a las canciones instaladas (una canción, o su dificultad), para que una pantalla pueda avisar al jugador.

## Pagoda.Floor

| Función | Descripción |
| --- | --- |
| `Floor.rollFloor(level, avoid?, songList?)  -> roll` | Una canción azul, una verde y una roja en orden aleatorio, y luego una canción morada según la probabilidad del piso, sin repetir nunca una canción. `avoid` es un conjunto de ids de canciones que no se eligen mientras los conjuntos tengan otras. Un sorteo es `{ songs = { { color, node, entry }, ... }, purple = { node, entry } or nil, speed }`. |
| `Floor.rerollFloor(level, roll, keepMystery, songList?)  -> roll` | Vuelve a sortear, con otras canciones donde los conjuntos lo permitan; `keepMystery` conserva la canción morada. |
| `Floor.exams(level, hadPurple)  -> table` | Los tres exámenes como `{ type, red, gold, lessThan }`. |
| `Floor.buildDan(level, roll?, opts?)  -> bool` | Prepara `DANBUILDER` para el piso: título, placa, género (`opts.genre`, `opts.genreColor`), las canciones y los exámenes. False cuando no se pudo añadir ninguna canción; si no, móntalo con `DANBUILDER:Mount()` y juega. |
| `Floor.speed(level)  -> number` | La velocidad de canción del piso (un valor de `CONFIG.SONGSPEED`): x1 por debajo de 穹天18, y en aumento a partir de ahí. |

## Pagoda.Run

Un intento de desafío empieza en uno de los pisos iniciales disponibles, sube un piso por cada clear y termina en su primera derrota. La práctica juega cualquier piso hasta el más alto que alcanzó un intento (al menos el 11) y no guarda nada.

| Función | Descripción |
| --- | --- |
| `Run.startOptions()  -> table` | Los pisos iniciales disponibles en esta partida guardada: 1, 6 y 11 siempre, 16 tras superar el 15, 21 tras superar el 20. |
| `Run.new(start, songList?)  -> run` | Un intento desde ese piso. Sus campos: `level` (el piso actual), `tower` (el piso más alto mostrado como superado), `roll`, `over`. |
| `run:rollFloor(avoid?)`, `run:reroll(keepMystery)  -> roll` | Las canciones del piso actual, para mostrarlas antes de jugar. |
| `run:enter()` | El intento alcanzó su piso: la práctica lo ofrece a partir de ahora. |
| `run:buildDan(opts?)  -> bool` | `DANBUILDER` preparado para el piso actual; móntalo y juega. |
| `run:clear()  -> result` | Tras un clear: concede y guarda las recompensas, sube el piso más alto y asciende. Devuelve `{ level, rewards, shrouded }`: `rewards` es la lista que devuelve `Rewards.grant`, y `shrouded` indica si el piso estaba por encima de la torre. |
| `run:fail()` | Tras una derrota: recuerda el piso, cuenta la partida jugada y termina el intento. |
| `Run.practiceLevels()  -> table`, `Run.practiceDan(level, opts?)  -> bool` | Los pisos que ofrece la práctica; `DANBUILDER` preparado para una partida de práctica con un sorteo nuevo. |

## Pagoda.Rewards

| Función | Descripción |
| --- | --- |
| `Rewards.grant(level)  -> table` | Concede y guarda las recompensas de un clear en la partida guardada del jugador 1, y devuelve las ventanas emergentes que se deben mostrar, en orden: `{ kind = "nameplate", id }`, `{ kind = "furniture", id }`, `{ kind = "key", key, count }`, `{ kind = "coins", amount }`. |
| `Rewards.KEY_PICTURE` | El prefijo de las texturas compartidas que muestran las ventanas de las llaves (`pagoda_reward_key_<id>`). Los módulos no incluyen imágenes de llaves: el skin comparte una por llave antes de mostrar la ventana de una llave. |
| `Rewards.show(r)  -> bool` | Inicia la ventana emergente de una recompensa a través de la ROActivity `modal`. |
| `Rewards.update()  -> bool`, `Rewards.draw()`, `Rewards.stop()` | Ejecuta la ventana emergente (true mientras se reproduce; lee su propia entrada), la dibuja, la descarta. |
| `Rewards.poolOf(level)`, `Rewards.nameplateOf(level)` | El conjunto de recompensas de un piso, y la placa de nombre que da su primer clear. |

El primer clear de un piso da su placa de nombre (ids 381 a 397, en los pisos listados en `Rewards.NAMEPLATES`). Después, las monedas (un intervalo, con un 50 % de probabilidad) y cada entrada del conjunto del piso se sortean con su propia probabilidad (`Rewards.POOLS`, `Rewards.ROOF_POOLS`), así que un clear puede dar varias cosas. Las llaves de la bóveda se convierten en monedas mientras la bóveda secreta está cerrada, y todas las monedas de un clear forman una sola ventana emergente. La ventana de un mueble muestra la textura compartida `myroom_thumb_<id>` que proporciona [Mi Habitación](my-room.md).

## Claves de guardado

Todo se guarda en la partida guardada del jugador 1 (`GetSaveFile(0)`):

| Clave | Tipo | Contenido |
| --- | --- | --- |
| `pagoda_highest_level` | contador | El piso más alto superado en un intento de desafío. |
| `pagoda_highest_reached` | contador | El piso más alto que alcanzó un intento, hasta el que llega la práctica. |
| `pagoda_plays` | contador | Intentos de desafío jugados (contados en la derrota). |
| `pagoda_lost_<level>` | trigger | Se perdió un intento en ese piso. |
| `pagoda_story_first_seen` | trigger | La historia inicial ya se ha reproducido (una partida guardada antigua puede tener en su lugar el contador `pagoda_story_seen`). |
| `pagoda_held_mods`, `pagoda_held_mods_lo`, `pagoda_held_mods_hi` | trigger, contadores | Los indicadores de mods del jugador 1 retenidos durante una partida de la pagoda, en dos mitades de 32 bits (`Progress.hold_mods(flags)`, `held_mods()`, `drop_held_mods()`). |
| `pagoda_structure_version` | contador | Se ha ejecutado la renumeración única de las partidas guardadas antiguas (`Progress.migrate_save()`). |
| `.myroom_<id>`, `.myroom_held_<id>` | contadores | Muebles ganados: entregados a Mi Habitación, y en posesión. |
| `.vault_key_count_<key>` | contadores | Llaves de la bóveda ganadas, a través de [Vault.Keys](vault.md). |

Las placas de nombre y las monedas pasan por los métodos propios del archivo de guardado (`UnlockNameplate`, `EarnCoins`).

```lua
local Run     = require("Pagoda.Run")
local Rewards = require("Pagoda.Rewards")
local run = Run.new(Run.startOptions()[1])

run:rollFloor()                  -- show run.roll's songs
run:enter()
if run:buildDan() and DANBUILDER:Mount() then return Exit("play") end

-- back from the play
local r = run:clear()            -- or run:fail()
for id in pairs(Rewards.KEYS) do SHARED:SetSharedTexture(Rewards.KEY_PICTURE .. id, "Textures/key_" .. id .. ".png") end
for _, reward in ipairs(r.rewards) do queue[#queue + 1] = reward end   -- then Rewards.show / update / draw
```
