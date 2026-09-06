<!-- api/networking.md -->

# Online networking <span class="badge-exp">Experimental</span>

The `NET` global is the client for OpenTaiko Online, the peer-to-peer protocol behind the online lobby and the multiplayer stages. One player creates a room and receives a room code; other players join with that code. The server (the room creator, unless the room has migrated) relays all traffic, and every player keeps a roster of who is in the room.

How Lua uses it:

- All data crosses the Lua boundary as strings. Stages carry structured data as JSON.
- The network runs on background threads and pushes events into a queue. A stage drains the queue each frame by calling `NET:Poll()` in a loop until it returns nil.
- Networking is off by default. The "Allow LuaNetworking connections" setting enables it; while it is off, CreateRoom returns nil, JoinRoom returns false, and the client queues an "error" event.
- Room ids: the room creator is id 1. Each joiner receives the next id in join order. `NET:SelfId()` is 0 outside a room.
- The host role is separate from owning the connection. It marks the player who currently drives the lobby (picks the song, starts the play). It starts with the creator; RotateHost or SetHostRole hands it to another player.
- If the room creator leaves, the remaining players migrate to a new server automatically; a room only closes when nobody can take over.

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

### Room lifecycle

<div class="callout warn">
Available as the global NET. Call SetLocalPlayer before CreateRoom or JoinRoom so the other players receive your info on join. JoinRoom is asynchronous: the outcome arrives later as a "connected" or "error" event. Creating or joining a room leaves any current room first.
</div>

| Method | Description |
| --- | --- |
| `NET:SetLocalPlayer(infoJson)  -> void` | Sets the local player's info string (typically JSON with the nameplate, character and so on). The client sends it to the other players when you join, and it appears in their "joined" events and PeersJson. An empty string becomes "{}"; the server replaces a string longer than 16384 characters with "{}". |
| `NET:CreateRoom(stageId, payload, maxPlayers)  -> string` | Creates a room for the given stage id with an optional payload string and returns its room code, or nil if networking is off or room creation failed. maxPlayers caps the roster (default 8). |
| `NET:CreateRoom(stageId, payload)  -> string` | Same with the default cap of 8 players. |
| `NET:PeekStageId(roomCode)  -> string` | Returns the stage id stored in a room code without connecting, or nil if the code is invalid. Lets a join screen route to the right stage. |
| `NET:JoinRoom(roomCode)  -> bool` | Starts joining the room. Returns false immediately if networking is off or the code is malformed; otherwise the result arrives as a "connected" or "error" event. |
| `NET:Leave()  -> void` | Leaves the room and clears the event queue. When the room creator leaves while others remain, another player takes over the room; otherwise it closes for everyone. |
| `NET.PortOverride  (int, settable)` | When greater than 0, rooms use this TCP port; 0 selects the default port 41234. Leave it at 0 in normal use. |

### Messaging

<div class="callout warn">
The stage chooses channel names freely. The play-round machinery reserves the channel names "ps", "ld" and "fn", which never reach Poll().
</div>

| Method | Description |
| --- | --- |
| `NET:Broadcast(channel, data)  -> void` | Sends a message on the given channel to every other player in the room. |
| `NET:SendTo(peerId, channel, data)  -> void` | Sends a message on the given channel to one player by id. -1 sends to everyone else, like Broadcast. |
| `NET:Poll()  -> NetEvent` | Removes and returns the next pending event, or nil when the queue is empty. |

### Room state and host role

| Method | Description |
| --- | --- |
| `NET:SelfId()  -> int` | This player's id (1 for the room creator), or 0 when not in a room. |
| `NET:Connected()  -> bool` | True when in a room: this player has a self id and is either the server or connected to it. |
| `NET:IsHost()  -> bool` | True when this player owns the room connection (the room creator, or the player that took over after a migration). |
| `NET:HasHostRole()  -> bool` | True when this player currently holds the host role. |
| `NET:HostRoleId()  -> int` | The id of the player holding the host role. |
| `NET:PeerCount()  -> int` | Number of players in the roster, including self. |
| `NET:PeersJson()  -> string` | The roster as a JSON array in join order. Each entry is {id, info, isHost, hostRole}: info is the string that player gave to SetLocalPlayer, isHost is true for id 1, hostRole is true for the current host-role holder. |
| `NET:RotateHost()  -> void` | Hands the host role to the next player in join order. Works only on the server; it ignores calls from other players. |
| `NET:SetHostRole(peerId)  -> void` | Gives the host role to the given player and announces it to the room. Works only on the server and only for ids in the roster. |

### Play rounds

During an online song the game suspends the lobby stage and the gameplay screen drives the exchange itself: it broadcasts the local player's running score, feeds remote spots from the wire, and waits on the loading and finish barriers. The lobby only has to declare who plays in which spot and to open and close the round.

<div class="callout warn">
Spots are the player positions of the gameplay screen: spot 0 is always the local player, spots 1 and up are remote players. Set the mapping with SetPlaySpots, then call BeginPlaySync right before entering play, and EndPlaySync once the lobby regains control. The gameplay screen calls the barrier, score-push and odds methods; they appear here because the same object exposes them.
</div>

| Method | Description |
| --- | --- |
| `NET:SetPlaySpots(json)  -> void` | Sets the spot-to-player mapping for the upcoming play from a JSON array of ids, index 0 being self (for example "[1,3,2]"). An unparsable string clears the mapping. |
| `NET:PlaySpotCount()  -> int` | Number of entries in the current spot mapping (0 when none is set). |
| `NET:BeginPlaySync(selfName)  -> void` | Opens a play round: clears previous scores and records the local player's display name for the running-score messages. |
| `NET:EndPlaySync()  -> void` | Closes the play round and clears the barriers and per-spot results. |
| `NET:IsRemoteSpot(spot)  -> bool` | True when the spot maps to a remote player during an open play round. |
| `NET:IsSpotActive(spot)  -> bool` | True when the player mapped to the spot is still in the roster (spot 0 is always true). False means the player dropped mid-play. |
| `NET:GetSpotPlayJson(spot)  -> string` | The latest running-score JSON a remote spot sent, or an empty string. Keys: n (name), s (score), g (gauge), a (accuracy), gr (great), gd (good), ms (bad), co (combo). |
| `NET:GetSelfPlayScore()  -> string` | The last running-score JSON this client broadcast. |
| `NET:LivePlayScoresJson()  -> string` | The last running-score JSON of every player, as a JSON array of {id, d} where d is the score JSON string. The array includes self once this client has broadcast at least once. |
| `NET:GetSpotResultJson(spot)  -> string` | The final result JSON a remote spot reported at the end of the song, or an empty string. Keys: cl (clear), fc (full combo), pf (perfect), mx (rainbow gauge) as booleans; gr, gd, ms, rl (rolls), bl (balloons), ad (ad-libs), hc (highest combo), sc (score) as integers. |
| `NET:GetSpotClearLevel(spot)  -> int` | Clear level from a remote spot's final result: 2 rainbow, 1 clear, 0 fail, -1 when no result has arrived. |
| `NET:GetSpotJudge(spot, key)  -> int` | One integer field of a remote spot's final result by key (see GetSpotResultJson), or -1 when no result has arrived. |
| `NET:GetSpotBadOdds(spot)  -> int` | Per-mille chance (0-1000) that the game judges a remote spot's next auto-hit as bad, derived from its broadcast judge counts. |
| `NET:GetSpotGoodOdds(spot)  -> int` | Per-mille chance (0-1000) that the game judges a remote spot's next auto-hit as good. |
| `NET:PushPlayScore(json)  -> void` | Broadcasts the local player's running-score JSON. The gameplay screen calls it several times per second. |
| `NET:ReportLoaded()  -> void` | Reports once that this client finished loading the song. |
| `NET:LoadBarrierReady(timeoutMs)  -> bool` | True once every player in the roster reported loaded, or once the timeout elapsed since this client reported (the server then drops the players that never reported). |
| `NET:ReportFinished(resultJson)  -> void` | Reports once that this client finished the song, with its final result JSON. |
| `NET:FinishBarrierReady(timeoutMs)  -> bool` | True once every player in the roster reported finished, or once the timeout elapsed. |
| `NET:BarrierReset()  -> void` | Clears the loading and finish barriers and the per-spot results. |

## NetEvent

The object returned by NET:Poll(), describing one network event.

<div class="callout warn">
Read its fields directly; it has no methods. Peer is 0 when the event has no related player.
</div>

| Method | Description |
| --- | --- |
| `event.Type  -> string` | The event kind, one of the values listed below. |
| `event.Peer  -> int` | The related player id: your own id for "connected", the sender for "message", the affected player for "joined", "left" and "hostrole"; 0 otherwise. |
| `event.Channel  -> string` | The channel of a "message" event; empty for other kinds. |
| `event.Data  -> string` | The event payload; see below. |

| Type | Meaning |
| --- | --- |
| `connected` | You joined a room. Data is a JSON object {selfId, stageId, payload, hostRoleId}. The room creator never receives it. Players already in the room appear in PeersJson and raise no "joined" events. |
| `joined` | A player joined after you. Peer is their id, Data is their info string. |
| `left` | A player left or the server dropped them. Peer is their id. |
| `message` | A message from another player. Peer is the sender, Channel and Data are the message. |
| `hostrole` | The host role changed hands. Peer is the new holder's id. |
| `roomclosed` | The room closed, or connection recovery failed. |
| `error` | A status message to show the player. Data is the text. The client raises it when networking is off, when creating or joining fails, when the room is full, and as progress notices during a host migration. |
