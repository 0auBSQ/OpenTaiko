<!-- api/networking.md -->

# Red en línea <span class="badge-exp">Experimental</span>

La global `NET` es el cliente de OpenTaiko Online, el protocolo entre pares (peer-to-peer) que está detrás del lobby en línea y de los stages multijugador. Un jugador crea una sala y recibe un código de sala; los demás jugadores se unen con ese código. El servidor (el creador de la sala, salvo que la sala haya migrado) retransmite todo el tráfico, y cada jugador mantiene una lista de participantes de quién está en la sala.

Cómo lo usa Lua:

- Todos los datos cruzan la frontera de Lua como cadenas. Los stages transportan los datos estructurados como JSON.
- La red se ejecuta en hilos en segundo plano y envía eventos a una cola. Un stage vacía la cola cada fotograma llamando a `NET:Poll()` en un bucle hasta que devuelve nil.
- La red está desactivada por defecto. El ajuste "Allow LuaNetworking connections" la activa; mientras está desactivada, CreateRoom devuelve nil, JoinRoom devuelve false y el cliente encola un evento "error".
- Ids de sala: el creador de la sala es el id 1. Cada jugador que se une recibe el siguiente id en orden de entrada. `NET:SelfId()` es 0 fuera de una sala.
- El rol de anfitrión es independiente de poseer la conexión. Marca al jugador que actualmente dirige el lobby (elige la canción, inicia la partida). Empieza en el creador; RotateHost o SetHostRole lo ceden a otro jugador.
- Si el creador de la sala se va, los jugadores restantes migran automáticamente a un nuevo servidor; una sala solo se cierra cuando nadie puede tomar el relevo.

```lua
function update(ts)
    while true do
        local e = NET:Poll()
        if e == nil then break end
        if e.Type == "message" and e.Channel == "chat" then
            addChatLine(e.Peer, e.Data)
        elseif e.Type == "left" then
            removePlayer(e.Peer)
        elseif e.Type == "roomclosed" then
            return Exit("title")
        end
    end
end
```

## NET

### Ciclo de vida de la sala

<div class="callout warn">
Disponible como la global NET. Llama a SetLocalPlayer antes de CreateRoom o JoinRoom para que los demás jugadores reciban tu información al unirte. JoinRoom es asíncrono: el resultado llega después como un evento "connected" o "error". Crear o unirse a una sala abandona antes cualquier sala actual.
</div>

| Método | Descripción |
| --- | --- |
| `NET:SetLocalPlayer(infoJson)  -> void` | Establece la cadena de información del jugador local (normalmente JSON con la placa de nombre, el personaje, etc.). El cliente la envía a los demás jugadores cuando te unes, y aparece en sus eventos "joined" y en PeersJson. Una cadena vacía se convierte en "{}"; el servidor reemplaza por "{}" una cadena de más de 16384 caracteres. |
| `NET:CreateRoom(stageId, payload, maxPlayers)  -> string` | Crea una sala para el id de stage indicado con una cadena de carga útil opcional y devuelve su código de sala, o nil si la red está desactivada o la creación de la sala falló. maxPlayers limita la lista de participantes (8 por defecto). |
| `NET:CreateRoom(stageId, payload)  -> string` | Igual con el límite por defecto de 8 jugadores. |
| `NET:PeekStageId(roomCode)  -> string` | Devuelve el id de stage guardado en un código de sala sin conectarse, o nil si el código es inválido. Permite que una pantalla de unión dirija al stage correcto. |
| `NET:JoinRoom(roomCode)  -> bool` | Empieza a unirse a la sala. Devuelve false de inmediato si la red está desactivada o el código está mal formado; si no, el resultado llega como un evento "connected" o "error". |
| `NET:Leave()  -> void` | Abandona la sala y vacía la cola de eventos. Cuando el creador de la sala se va mientras quedan otros, otro jugador toma el control de la sala; si no, se cierra para todos. |
| `NET.PortOverride  (int, settable)` | Cuando es mayor que 0, las salas usan este puerto TCP; 0 selecciona el puerto por defecto 41234. Déjalo en 0 en uso normal. |

### Mensajería

<div class="callout warn">
El stage elige libremente los nombres de canal. La maquinaria de rondas de juego reserva los nombres de canal "ps", "ld" y "fn", que nunca llegan a Poll().
</div>

| Método | Descripción |
| --- | --- |
| `NET:Broadcast(channel, data)  -> void` | Envía un mensaje por el canal indicado a todos los demás jugadores de la sala. |
| `NET:SendTo(peerId, channel, data)  -> void` | Envía un mensaje por el canal indicado a un jugador por id. -1 envía a todos los demás, como Broadcast. |
| `NET:Poll()  -> NetEvent` | Extrae y devuelve el siguiente evento pendiente, o nil cuando la cola está vacía. |

### Estado de la sala y rol de anfitrión

| Método | Descripción |
| --- | --- |
| `NET:SelfId()  -> int` | El id de este jugador (1 para el creador de la sala), o 0 cuando no está en una sala. |
| `NET:Connected()  -> bool` | Verdadero cuando está en una sala: este jugador tiene un id propio y es el servidor o está conectado a él. |
| `NET:IsHost()  -> bool` | Verdadero cuando este jugador posee la conexión de la sala (el creador de la sala, o el jugador que tomó el relevo tras una migración). |
| `NET:HasHostRole()  -> bool` | Verdadero cuando este jugador tiene actualmente el rol de anfitrión. |
| `NET:HostRoleId()  -> int` | El id del jugador que tiene el rol de anfitrión. |
| `NET:PeerCount()  -> int` | Número de jugadores en la lista de participantes, incluido uno mismo. |
| `NET:PeersJson()  -> string` | La lista de participantes como array JSON en orden de entrada. Cada entrada es {id, info, isHost, hostRole}: info es la cadena que ese jugador dio a SetLocalPlayer, isHost es true para el id 1, hostRole es true para quien tiene actualmente el rol de anfitrión. |
| `NET:RotateHost()  -> void` | Cede el rol de anfitrión al siguiente jugador en orden de entrada. Funciona solo en el servidor; ignora las llamadas de los demás jugadores. |
| `NET:SetHostRole(peerId)  -> void` | Da el rol de anfitrión al jugador indicado y lo anuncia a la sala. Funciona solo en el servidor y solo para ids de la lista de participantes. |

### Rondas de juego

Durante una canción en línea el juego suspende el stage del lobby y la pantalla de juego dirige el intercambio por sí misma: difunde la puntuación en curso del jugador local, alimenta los puestos remotos desde la red y espera en las barreras de carga y de fin. El lobby solo tiene que declarar quién juega en cada puesto y abrir y cerrar la ronda.

<div class="callout warn">
Los puestos son las posiciones de jugador de la pantalla de juego: el puesto 0 es siempre el jugador local, los puestos 1 en adelante son jugadores remotos. Establece la asignación con SetPlaySpots, luego llama a BeginPlaySync justo antes de entrar en la partida, y a EndPlaySync cuando el lobby recupere el control. La pantalla de juego llama a los métodos de barrera, envío de puntuación y probabilidades; aparecen aquí porque el mismo objeto los expone.
</div>

| Método | Descripción |
| --- | --- |
| `NET:SetPlaySpots(json)  -> void` | Establece la asignación de puesto a jugador para la próxima partida a partir de un array JSON de ids, siendo el índice 0 uno mismo (por ejemplo "[1,3,2]"). Una cadena no analizable borra la asignación. |
| `NET:PlaySpotCount()  -> int` | Número de entradas en la asignación de puestos actual (0 cuando no hay ninguna). |
| `NET:BeginPlaySync(selfName)  -> void` | Abre una ronda de juego: borra las puntuaciones anteriores y registra el nombre mostrado del jugador local para los mensajes de puntuación en curso. |
| `NET:EndPlaySync()  -> void` | Cierra la ronda de juego y borra las barreras y los resultados por puesto. |
| `NET:IsRemoteSpot(spot)  -> bool` | Verdadero cuando el puesto corresponde a un jugador remoto durante una ronda de juego abierta. |
| `NET:IsSpotActive(spot)  -> bool` | Verdadero cuando el jugador asignado al puesto sigue en la lista de participantes (el puesto 0 es siempre true). False significa que el jugador se desconectó a mitad de partida. |
| `NET:GetSpotPlayJson(spot)  -> string` | El último JSON de puntuación en curso que envió un puesto remoto, o una cadena vacía. Claves: n (nombre), s (puntuación), g (medidor), a (precisión), gr (great), gd (good), ms (bad), co (combo). |
| `NET:GetSelfPlayScore()  -> string` | El último JSON de puntuación en curso que difundió este cliente. |
| `NET:LivePlayScoresJson()  -> string` | El último JSON de puntuación en curso de cada jugador, como array JSON de {id, d} donde d es la cadena JSON de puntuación. El array incluye a uno mismo una vez que este cliente ha difundido al menos una vez. |
| `NET:GetSpotResultJson(spot)  -> string` | El JSON de resultado final que un puesto remoto reportó al final de la canción, o una cadena vacía. Claves: cl (clear), fc (full combo), pf (perfecto), mx (medidor arcoíris) como booleanos; gr, gd, ms, rl (redobles), bl (globos), ad (ad-libs), hc (combo más alto), sc (puntuación) como enteros. |
| `NET:GetSpotClearLevel(spot)  -> int` | Nivel de clear del resultado final de un puesto remoto: 2 arcoíris, 1 clear, 0 fallo, -1 cuando no ha llegado ningún resultado. |
| `NET:GetSpotJudge(spot, key)  -> int` | Un campo entero del resultado final de un puesto remoto por clave (consulta GetSpotResultJson), o -1 cuando no ha llegado ningún resultado. |
| `NET:GetSpotBadOdds(spot)  -> int` | Probabilidad por mil (0-1000) de que el juego juzgue como bad el siguiente golpe automático de un puesto remoto, derivada de sus recuentos de juicio difundidos. |
| `NET:GetSpotGoodOdds(spot)  -> int` | Probabilidad por mil (0-1000) de que el juego juzgue como good el siguiente golpe automático de un puesto remoto. |
| `NET:PushPlayScore(json)  -> void` | Difunde el JSON de puntuación en curso del jugador local. La pantalla de juego lo llama varias veces por segundo. |
| `NET:ReportLoaded()  -> void` | Informa una vez de que este cliente terminó de cargar la canción. |
| `NET:LoadBarrierReady(timeoutMs)  -> bool` | Verdadero una vez que todos los jugadores de la lista de participantes reportaron la carga, o una vez transcurrido el tiempo de espera desde que este cliente reportó (el servidor descarta entonces a los jugadores que nunca reportaron). |
| `NET:ReportFinished(resultJson)  -> void` | Informa una vez de que este cliente terminó la canción, con su JSON de resultado final. |
| `NET:FinishBarrierReady(timeoutMs)  -> bool` | Verdadero una vez que todos los jugadores de la lista de participantes reportaron el fin, o una vez transcurrido el tiempo de espera. |
| `NET:BarrierReset()  -> void` | Borra las barreras de carga y de fin y los resultados por puesto. |

## NetEvent

El objeto que devuelve NET:Poll(), que describe un evento de red.

<div class="callout warn">
Lee sus campos directamente; no tiene métodos. Peer es 0 cuando el evento no tiene un jugador relacionado.
</div>

| Método | Descripción |
| --- | --- |
| `event.Type  -> string` | El tipo de evento, uno de los valores listados abajo. |
| `event.Peer  -> int` | El id del jugador relacionado: tu propio id para "connected", el remitente para "message", el jugador afectado para "joined", "left" y "hostrole"; 0 en los demás casos. |
| `event.Channel  -> string` | El canal de un evento "message"; vacío para los demás tipos. |
| `event.Data  -> string` | La carga útil del evento; ver abajo. |

| Tipo | Significado |
| --- | --- |
| `connected` | Te uniste a una sala. Data es un objeto JSON {selfId, stageId, payload, hostRoleId}. El creador de la sala nunca lo recibe. Los jugadores que ya estaban en la sala aparecen en PeersJson y no lanzan eventos "joined". |
| `joined` | Un jugador se unió después de ti. Peer es su id, Data es su cadena de información. |
| `left` | Un jugador se fue o el servidor lo descartó. Peer es su id. |
| `message` | Un mensaje de otro jugador. Peer es el remitente, Channel y Data son el mensaje. |
| `hostrole` | El rol de anfitrión cambió de manos. Peer es el id del nuevo titular. |
| `roomclosed` | La sala se cerró, o la recuperación de la conexión falló. |
| `error` | Un mensaje de estado para mostrar al jugador. Data es el texto. El cliente lo lanza cuando la red está desactivada, cuando falla crear o unirse, cuando la sala está llena, y como avisos de progreso durante una migración de anfitrión. |
