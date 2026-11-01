<!-- modules/online.md -->

# Online

La lógica de la sala en línea: salas que se abren y a las que se entra con un código, la lista de jugadores, la dificultad, los mods y el estado listo de cada jugador, la canción de la sala, el inicio de una ronda, los resultados y la ronda siguiente. Los módulos `Online` no dibujan nada; una skin dibuja sus propias pantallas de sala y conecta encima su propia selección de canciones. La sala de cada skin habla los mismos mensajes, así que jugadores con skins distintas se encuentran en una misma sala. La stage `onlinelobby` de Open-World Memories es uno de esos clientes. Los módulos funcionan sobre el [global NET](../api/networking.md), que es experimental.

## Los módulos

| Módulo | Qué contiene |
| --- | --- |
| `Online.Lobby` | La sesión: su estado, cada acción de una pantalla, la conexión con la selección de canciones y el procesado de eventos. |
| `Online.Protocol` | Los mensajes: los nombres de los canales y el JSON de cada contenido. |
| `Online.Identity` | Lo que un jugador envía de sí mismo (nombre, personaje, puchichara, placa de nombre, placa de dan, paleta). |
| `Online.Media` | La canción de la sala en esta máquina: sus niveles, su portada en la textura compartida `preimage`, su vista previa en el sonido compartido `presound`. |
| `Online.Texts` | Los textos propios de los módulos (`lang/<code>/online.json`). |

Una skin solo necesita `Online.Lobby`, que reexporta lo que usa de los demás. Hay una sesión por estado de Lua: el módulo guarda su estado en `Lobby.net`.

## El estado de la sesión

La skin lee `Lobby.net` en cada frame para dibujar sus pantallas:

| Campo | Descripción |
| --- | --- |
| `online`, `connecting` | En una sala; esperando la respuesta a una entrada. |
| `isHost`, `code` | Esta máquina ejecuta la sala (la creó); el código de la sala. |
| `roomGone` | La sala se cerró: salir y volver al menú de la skin. |
| `msg`, `msgCode`, `msgArg` | El último mensaje, ya en el idioma del juego, su código estable y su argumento (ver Mensajes). |
| `notice` | Un mensaje que llegó estando en una sala, para mostrarlo una vez y borrarlo. |
| `nameByPeer`, `infoByPeer` | Los jugadores por id: los nombres y la información de `Online.Identity` (`name`, `char`, `puchi`, `dan`, `npk`, `dgold`, `dtype`, `palStops`, `palBlend`). |
| `diffByPeer`, `modByPeer`, `readyByPeer` | La dificultad (0 a 4), los mods y el estado listo de cada jugador. |
| `lackByPeer`, `watchByPeer` | Los jugadores sin la canción de la sala; los jugadores que siguen en los resultados. |
| `song` | La canción de la sala `{ uid, title, diff, speed, dyn }`, nil hasta que el rol de anfitrión elige una. |
| `songLevels`, `songPlus`, `songSubtitle`, `songBpm`, `iLackSong` | La canción en esta máquina: el nivel y la marca plus de cada dificultad que tiene, su subtítulo y su BPM; `iLackSong` cuando esta máquina no la tiene. |
| `resultByPeer` | Los resultados de la ronda: `{ name, sc, gr, gd, ms, co, cl, fc, pf, ac, ab }` (puntuación, great, good, bad, combo, superado, full combo, perfecto, precisión, abandonado). |
| `resultsT`, `resultsReadyT` | Segundos en los resultados; se fija cuando están completos. |

## Online.Lobby

| Función | Descripción |
| --- | --- |
| `Lobby.host()  -> bool` | Abre una sala (como mucho `Lobby.MAXP` = 5 jugadores) y guarda su código con `STORAGE:WriteLobbyCode`; false con `net.msg` cuando NET lo rechaza. |
| `Lobby.join(code)  -> bool` | Empieza a entrar en la sala de un código (se ignoran los espacios); la respuesta llega por `drain`. False con `net.msg` cuando el código está vacío, no es válido o es de otro tipo de sala. |
| `Lobby.leave()` | Termina la sesión (también tras una entrada fallida) y devuelve los puestos de jugador a las partidas guardadas. |
| `Lobby.drain()` | Procesa cada evento de NET pendiente; llámala en cada frame mientras `online` o `connecting` sea verdadero. |
| `Lobby.amController()  -> bool` | Este jugador tiene el rol de anfitrión: elige la canción e inicia la ronda. |
| `Lobby.peerIds()`, `Lobby.count()` | Los ids de los jugadores en orden; su número. |
| `Lobby.spotOf[id]` | El puesto local de cada jugador (0 para uno mismo), para `NAMEPLATE:DrawPlayerNameplate`. |
| `Lobby.myDiff()`, `Lobby.setDiff(d)`, `Lobby.stepDiff(dir)  -> bool` | Mi dificultad; fijarla; moverla a la siguiente que tenga la canción (false al final de la fila). |
| `Lobby.availDiffs()`, `Lobby.diffAvailable(d)`, `Lobby.fixDiff()` | Las dificultades de la canción; mantener la mía en una de ellas (llámala en cada frame de la sala). |
| `Lobby.myReady()`, `Lobby.setReady(b)` | Mi estado listo. |
| `Lobby.broadcastMods()` | Lee los mods del jugador 0 en `CONFIG` y los envía; llámala cuando se cierre la selección de mods de la skin. |
| `Lobby.applyModsToSlot(slot, mods)` | Pone los mods de un jugador en un puesto de jugador de `CONFIG`, para dibujar sus iconos con la ROActivity `modicons`. |
| `Lobby.adjustSpeed(delta)`, `Lobby.setSpeed(sp)` | La velocidad de la canción de la sala (solo el rol de anfitrión). |
| `Lobby.startProblem()  -> "need_song", "need_players" or nil` | Por qué la ronda aún no puede empezar. No hace falta estar listo. |
| `Lobby.hostStart()  -> bool` | Inicia la ronda (solo el rol de anfitrión). |
| `Lobby.takeGo()  -> bool` | Verdadero una vez cuando la ronda empezó; entonces llama a `launchPlay`. |
| `Lobby.launchPlay()  -> true, "eject" or false` | Monta la canción para cada jugador y abre la ronda de juego: con true la skin sale a `"play"`; `"eject"` significa que esta máquina no tiene la canción y debe salir de la sala; false llega con `net.msg`. |
| `Lobby.returnedFromPlay()` | Llámala cuando se vuelva a entrar en la stage tras esa partida: envía mi resultado e inicia los resultados. |
| `Lobby.tickResults(dt)  -> bool` | En cada frame en los resultados: verdadero en cuanto llegan todos los resultados o pasan `Lobby.RESULTS_WAIT` (20) segundos. |
| `Lobby.standings()  -> rows` | Los resultados, la mejor puntuación primero: `{ id, name, r }`, `r` nil mientras ese jugador sigue jugando. |
| `Lobby.nextRound()` | De vuelta a la sala: el rol de anfitrión pasa al siguiente jugador. |
| `Lobby.tickPreview(dt)`, `Lobby.stopPreview()` | Repite la vista previa de la canción en las pantallas de sala y resultados; la detiene. |
| `Lobby.onEvent` | Una función opcional `(kind, peer, data)` llamada por cada evento que procesa `drain` (`"joined"`, `"left"`, `"song"`, `"ready"`, `"go"`, `"result"`, ...). |

El juego en línea se ve como el juego sin conexión con el mismo número de jugadores: los demás jugadores ocupan los puestos locales 2 a 5 como ranuras virtuales, con sus propios personajes y placas de nombre, y la pantalla de juego los hace jugar según la red.

## La conexión con la selección de canciones

El jugador con el rol de anfitrión elige la canción de la sala en la selección de canciones de la skin. Esta solo debe elegir una canción (sin paso de dificultad, sin Auto); sus teclas de velocidad cambian `CONFIG.SongSpeed`. Tres llamadas la conectan:

1. `Lobby.openSongSelect(open)` en lugar de activarla directamente: `open` es una función que activa la selección de canciones. Primero se pone la velocidad de la sala en `CONFIG.SongSpeed`.
2. Cuando se confirma una canción: desactiva la selección de canciones y luego llama a `Lobby.songPicked(uid?)`. `uid` es por defecto `SONGMOUNT:ChosenUniqueId()`.
3. Cuando se sale de la selección de canciones sin canción: desactívala y luego llama a `Lobby.songPickCancelled()`, que conserva una velocidad cambiada allí y devuelve la portada y la vista previa de la sala.

La cadena compartida `presound_path` nombra el audio de `presound`: una selección de canciones que deja sonando en él la canción de la sala devuelve la vista previa, y la sala la mantiene en vez de reiniciarla.

## Mensajes

`net.msg` está listo para mostrarse. Una skin que redacta sus propios mensajes lee en su lugar `net.msgCode`:

| Código | Cuándo |
| --- | --- |
| `open_failed`, `join_failed` | NET lo rechazó sin decir por qué. |
| `room_open` | La sala está abierta y su código guardado. |
| `connecting` | Empezó una entrada. |
| `no_code`, `code_invalid` | El código está vacío o no es un código de sala. |
| `code_other` | El código es de otro tipo de sala; `msgArg` es su id de stage. |
| `chart_failed` | `launchPlay` no pudo montar el chart. |
| `net` | Un texto del propio NET (red desactivada, conexión perdida, ...), en inglés. |

La red está desactivada hasta que el jugador la permite en los ajustes. Una skin debería comprobar `CONFIG.AreLuaNetworkingConnectionsAllowed` antes de abrir su sala; NET también lo rechaza con un mensaje `net`.

## Online.Protocol

Cada mensaje es una cadena JSON en un canal de NET. El id de sala en el código es `"onlinelobby"` (`Protocol.ROOM_ID`), sea cual sea la stage que la abrió.

| Canal | Contenido | Se envía |
| --- | --- | --- |
| `song` | `{"u":uid,"t":title,"d":0,"s":speed,"b":dyn}` | Por el rol de anfitrión, cuando cambia la canción o su velocidad, y a cada recién llegado. |
| `diff`, `mod`, `ready` | `{"d":n}`, `{"r","st","ju","tz","ss","sm"}`, `{"r":bool}` | Cuando un jugador los cambia, y a cada recién llegado. |
| `have`, `watch` | `{"h":bool}`, `{"w":bool}` | Si el jugador tiene la canción; si está en los resultados. |
| `go` | `{}` | Empieza la ronda. |
| `result` | `{"n","sc","gr","gd","ms","co","cl","fc","pf","ac","ab"}` | El resultado de un jugador tras la canción. |

## Una sala mínima

```lua
local Lobby = require("Online.Lobby")
local net = Lobby.net
local screen, back = "menu", false

function activate()
    if back then back = false; Lobby.returnedFromPlay(); screen = "results" end
end

function update(ts)
    local dt = 1 / 60
    if net.online or net.connecting then
        Lobby.drain()
        if net.roomGone then Lobby.leave(); screen = "menu" end
    end
    if screen == "lobby" then
        Lobby.tickPreview(dt)
        Lobby.fixDiff()
        if Lobby.takeGo() then
            local r = Lobby.launchPlay()
            if r == true then back = true; return Exit("play", nil) end
            if r == "eject" then Lobby.leave(); screen = "menu" end
        end
    elseif screen == "results" then
        if Lobby.tickResults(dt) and decidePressed() then Lobby.nextRound(); screen = "lobby" end
    end
end

function onDestroy() Lobby.leave() end
```

`Lobby.host()` o `Lobby.join(code)` desde el menú de la skin cambian `screen` a `"lobby"`; la conexión con la selección de canciones de arriba fija la canción.
