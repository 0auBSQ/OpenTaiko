<!-- modules/online.md -->

# Online

The online lobby's logic: rooms opened and joined by code, the roster, each player's difficulty, mods and readiness, the room's song, the start of a round, the results and the next round. The `Online` modules draw nothing; a skin draws its own lobby screens and wires its own song select on top of them. Every skin's lobby speaks the same messages, so players on different skins meet in one room. Open-World Memories' `onlinelobby` stage is one such client. The modules run on the [NET global](../api/networking.md), which is experimental.

## The modules

| Module | What it holds |
| --- | --- |
| `Online.Lobby` | The session: its state, every action a screen takes, the song-select wiring and the event drain. |
| `Online.Protocol` | The messages: channel names and the JSON of each payload. |
| `Online.Identity` | What a player sends about themselves (name, character, puchichara, nameplate, dan plate, palette). |
| `Online.Media` | The room's song on this machine: its levels, its jacket in the shared texture `preimage`, its preview in the shared sound `presound`. |
| `Online.Texts` | The modules' own strings (`lang/<code>/online.json`). |

A skin only needs `Online.Lobby`, which re-exports what it uses of the others. There is one session per Lua state: the module keeps its state in `Lobby.net`.

## The session state

The skin reads `Lobby.net` each frame to draw its screens:

| Field | Description |
| --- | --- |
| `online`, `connecting` | In a room; waiting for a join to answer. |
| `isHost`, `code` | This machine runs the room (it created it); the room code. |
| `roomGone` | The room closed: leave and go back to the skin's menu. |
| `msg`, `msgCode`, `msgArg` | The last message, already in the game's language, its stable code and its argument (see Messages). |
| `notice` | A message that arrived while in a room, to show once and clear. |
| `nameByPeer`, `infoByPeer` | The players by id: names, and the info of `Online.Identity` (`name`, `char`, `puchi`, `dan`, `npk`, `dgold`, `dtype`, `palStops`, `palBlend`). |
| `diffByPeer`, `modByPeer`, `readyByPeer` | Each player's difficulty (0 to 4), mods and readiness. |
| `lackByPeer`, `watchByPeer` | The players without the room's song; the players still on the results. |
| `song` | The room's song `{ uid, title, diff, speed, dyn }`, nil until the host role picks one. |
| `songLevels`, `songPlus`, `songSubtitle`, `songBpm`, `iLackSong` | The song on this machine: the level and plus mark of each difficulty it has, its subtitle and BPM; `iLackSong` when this machine lacks it. |
| `resultByPeer` | The results of the round: `{ name, sc, gr, gd, ms, co, cl, fc, pf, ac, ab }` (score, great, good, bad, combo, clear, full combo, perfect, accuracy, aborted). |
| `resultsT`, `resultsReadyT` | Seconds on the results; set once they are complete. |

## Online.Lobby

| Function | Description |
| --- | --- |
| `Lobby.host()  -> bool` | Opens a room (at most `Lobby.MAXP` = 5 players) and saves its code with `STORAGE:WriteLobbyCode`; false with `net.msg` when NET refused. |
| `Lobby.join(code)  -> bool` | Starts joining the room of a code (spaces are ignored); the answer arrives through `drain`. False with `net.msg` when the code is empty, invalid, or for another kind of room. |
| `Lobby.leave()` | Ends the session (also after a failed join) and gives the player spots back to the save files. |
| `Lobby.drain()` | Handles every queued NET event; call it each frame while `online` or `connecting`. |
| `Lobby.amController()  -> bool` | This player holds the host role: they pick the song and start the round. |
| `Lobby.peerIds()`, `Lobby.count()` | The players' ids in order; their number. |
| `Lobby.spotOf[id]` | The local player spot of each player (0 for self), for `NAMEPLATE:DrawPlayerNameplate`. |
| `Lobby.myDiff()`, `Lobby.setDiff(d)`, `Lobby.stepDiff(dir)  -> bool` | My difficulty; set it; move it to the next one the song has (false at the end of the row). |
| `Lobby.availDiffs()`, `Lobby.diffAvailable(d)`, `Lobby.fixDiff()` | The difficulties the song has; keep mine on one of them (call it each lobby frame). |
| `Lobby.myReady()`, `Lobby.setReady(b)` | My readiness. |
| `Lobby.broadcastMods()` | Reads player 0's mods from `CONFIG` and sends them; call it when the skin's mod select closes. |
| `Lobby.applyModsToSlot(slot, mods)` | Puts a player's mods on a `CONFIG` player slot, to draw their icons with the `modicons` ROActivity. |
| `Lobby.adjustSpeed(delta)`, `Lobby.setSpeed(sp)` | The room's song speed (host role only). |
| `Lobby.startProblem()  -> "need_song", "need_players" or nil` | Why the round can't start yet. Readiness is not required. |
| `Lobby.hostStart()  -> bool` | Starts the round (host role only). |
| `Lobby.takeGo()  -> bool` | True once when the round started; then call `launchPlay`. |
| `Lobby.launchPlay()  -> true, "eject" or false` | Mounts the song for every player and opens the play round: on true the skin exits to `"play"`; `"eject"` means this machine lacks the song and must leave the room; false comes with `net.msg`. |
| `Lobby.returnedFromPlay()` | Call it when the stage is entered again after that play: it sends my result and starts the results. |
| `Lobby.tickResults(dt)  -> bool` | Each frame on the results: true once every result is in or `Lobby.RESULTS_WAIT` (20) seconds have passed. |
| `Lobby.standings()  -> rows` | The results, best score first: `{ id, name, r }`, `r` nil while that player still plays. |
| `Lobby.nextRound()` | Back to the lobby: the host role moves to the next player. |
| `Lobby.tickPreview(dt)`, `Lobby.stopPreview()` | Loops the song preview on the lobby and results screens; stops it. |
| `Lobby.onEvent` | An optional function `(kind, peer, data)` called for each event `drain` handles (`"joined"`, `"left"`, `"song"`, `"ready"`, `"go"`, `"result"`, ...). |

Online play looks like offline play at the same player count: the other players take the local spots 2 to 5 as virtual slots, with their own characters and nameplates, and the gameplay screen plays them from the wire.

## The song-select wiring

The player with the host role picks the room's song in the skin's song select. The song select must pick a song only (no difficulty step, no Auto); its speed keys change `CONFIG.SongSpeed`. Three calls connect it:

1. `Lobby.openSongSelect(open)` instead of activating it directly: `open` is a function that activates the song select. The room's speed is put in `CONFIG.SongSpeed` first.
2. When a song is confirmed: deactivate the song select, then `Lobby.songPicked(uid?)`. `uid` defaults to `SONGMOUNT:ChosenUniqueId()`.
3. When the song select is left without a song: deactivate it, then `Lobby.songPickCancelled()`, which keeps a speed changed there and puts the room's jacket and preview back.

The shared string `presound_path` names the audio in `presound`: a song select that leaves the room's song playing in it hands the preview back, and the lobby keeps it going instead of restarting it.

## Messages

`net.msg` is ready to show. A skin that words its messages itself reads `net.msgCode` instead:

| Code | When |
| --- | --- |
| `open_failed`, `join_failed` | NET refused without saying why. |
| `room_open` | The room is open and its code saved. |
| `connecting` | A join started. |
| `no_code`, `code_invalid` | The code is empty, or not a room code. |
| `code_other` | The code is for another kind of room; `msgArg` is its stage id. |
| `chart_failed` | `launchPlay` could not mount the chart. |
| `net` | A text from NET itself (networking off, a lost connection, ...), in English. |

Networking is off until the player allows it in the settings. A skin should check `CONFIG.AreLuaNetworkingConnectionsAllowed` before opening its lobby; NET also refuses with a `net` message.

## Online.Protocol

Every message is a JSON string on one NET channel. The room id in the code is `"onlinelobby"` (`Protocol.ROOM_ID`), whatever the stage that opened it.

| Channel | Payload | Sent |
| --- | --- | --- |
| `song` | `{"u":uid,"t":title,"d":0,"s":speed,"b":dyn}` | By the host role, when the song or its speed changes, and to each newcomer. |
| `diff`, `mod`, `ready` | `{"d":n}`, `{"r","st","ju","tz","ss","sm"}`, `{"r":bool}` | When a player changes them, and to each newcomer. |
| `have`, `watch` | `{"h":bool}`, `{"w":bool}` | Whether the player has the song; whether they are on the results. |
| `go` | `{}` | The round starts. |
| `result` | `{"n","sc","gr","gd","ms","co","cl","fc","pf","ac","ab"}` | A player's result after the song. |

## A minimal lobby

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

`Lobby.host()` or `Lobby.join(code)` from the skin's menu switch `screen` to `"lobby"`; the song-select wiring above sets the song.
