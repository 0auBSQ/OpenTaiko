<!-- api/networking.md -->

# Online-Netzwerk <span class="badge-exp">Experimentell</span>

Das globale Objekt `NET` ist der Client für OpenTaiko Online, das Peer-to-Peer-Protokoll hinter der Online-Lobby und den Mehrspieler-Stages. Ein Spieler erstellt einen Raum und erhält einen Raum-Code; andere Spieler treten mit diesem Code bei. Der Server (der Raumersteller, sofern der Raum nicht migriert wurde) leitet den gesamten Verkehr weiter, und jeder Spieler führt eine Teilnehmerliste darüber, wer sich im Raum befindet.

So verwendet Lua es:

- Alle Daten überqueren die Lua-Grenze als Strings. Stages übertragen strukturierte Daten als JSON.
- Das Netzwerk läuft auf Hintergrund-Threads und legt Ereignisse in eine Warteschlange. Eine Stage leert die Warteschlange jeden Frame, indem sie `NET:Poll()` in einer Schleife aufruft, bis es nil zurückgibt.
- Das Netzwerk ist standardmäßig aus. Die Einstellung "Allow LuaNetworking connections" aktiviert es; solange es aus ist, gibt CreateRoom nil zurück, JoinRoom gibt false zurück, und der Client reiht ein "error"-Ereignis ein.
- Raum-IDs: Der Raumersteller ist ID 1. Jeder Beitretende erhält die nächste ID in Beitrittsreihenfolge. `NET:SelfId()` ist außerhalb eines Raums 0.
- Die Host-Rolle ist vom Besitz der Verbindung getrennt. Sie kennzeichnet den Spieler, der gerade die Lobby steuert (den Song auswählt, das Spiel startet). Sie beginnt beim Ersteller; RotateHost oder SetHostRole übergibt sie an einen anderen Spieler.
- Verlässt der Raumersteller den Raum, migrieren die verbleibenden Spieler automatisch zu einem neuen Server; ein Raum wird nur geschlossen, wenn niemand übernehmen kann.

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

### Raum-Lebenszyklus

<div class="callout warn">
Als globales Objekt NET verfügbar. Rufen Sie SetLocalPlayer vor CreateRoom oder JoinRoom auf, damit die anderen Spieler Ihre Informationen beim Beitritt erhalten. JoinRoom ist asynchron: Das Ergebnis kommt später als "connected"- oder "error"-Ereignis. Das Erstellen oder Beitreten eines Raums verlässt zuerst jeden aktuellen Raum.
</div>

| Methode | Beschreibung |
| --- | --- |
| `NET:SetLocalPlayer(infoJson)  -> void` | Setzt den Info-String des lokalen Spielers (typischerweise JSON mit Namensschild, Charakter und so weiter). Der Client sendet ihn beim Beitritt an die anderen Spieler, und er erscheint in deren "joined"-Ereignissen und PeersJson. Ein leerer String wird zu "{}"; einen String mit mehr als 16384 Zeichen ersetzt der Server durch "{}". |
| `NET:CreateRoom(stageId, payload, maxPlayers)  -> string` | Erstellt einen Raum für die angegebene Stage-ID mit einem optionalen Payload-String und gibt seinen Raum-Code zurück, oder nil, wenn das Netzwerk aus ist oder das Erstellen des Raums fehlgeschlagen ist. maxPlayers begrenzt die Teilnehmerliste (Standard 8). |
| `NET:CreateRoom(stageId, payload)  -> string` | Dasselbe mit der Standardgrenze von 8 Spielern. |
| `NET:PeekStageId(roomCode)  -> string` | Gibt die in einem Raum-Code gespeicherte Stage-ID zurück, ohne zu verbinden, oder nil, wenn der Code ungültig ist. Erlaubt einem Beitrittsbildschirm, zur richtigen Stage zu wechseln. |
| `NET:JoinRoom(roomCode)  -> bool` | Beginnt den Beitritt zum Raum. Gibt sofort false zurück, wenn das Netzwerk aus ist oder der Code fehlerhaft ist; andernfalls kommt das Ergebnis als "connected"- oder "error"-Ereignis. |
| `NET:Leave()  -> void` | Verlässt den Raum und leert die Ereigniswarteschlange. Wenn der Raumersteller geht, während andere bleiben, übernimmt ein anderer Spieler den Raum; andernfalls wird er für alle geschlossen. |
| `NET.PortOverride  (int, settable)` | Wenn größer als 0, verwenden Räume diesen TCP-Port; 0 wählt den Standardport 41234. Im Normalbetrieb bei 0 belassen. |

### Nachrichten

<div class="callout warn">
Die Stage wählt Kanalnamen frei. Die Spielrunden-Mechanik reserviert die Kanalnamen "ps", "ld" und "fn", die Poll() nie erreichen.
</div>

| Methode | Beschreibung |
| --- | --- |
| `NET:Broadcast(channel, data)  -> void` | Sendet eine Nachricht auf dem angegebenen Kanal an jeden anderen Spieler im Raum. |
| `NET:SendTo(peerId, channel, data)  -> void` | Sendet eine Nachricht auf dem angegebenen Kanal an einen Spieler per ID. -1 sendet an alle anderen, wie Broadcast. |
| `NET:Poll()  -> NetEvent` | Entfernt das nächste ausstehende Ereignis und gibt es zurück, oder nil, wenn die Warteschlange leer ist. |

### Raumzustand und Host-Rolle

| Methode | Beschreibung |
| --- | --- |
| `NET:SelfId()  -> int` | Die ID dieses Spielers (1 für den Raumersteller), oder 0, wenn nicht in einem Raum. |
| `NET:Connected()  -> bool` | True, wenn in einem Raum: Dieser Spieler hat eine eigene ID und ist entweder der Server oder mit ihm verbunden. |
| `NET:IsHost()  -> bool` | True, wenn dieser Spieler die Raumverbindung besitzt (der Raumersteller oder der Spieler, der nach einer Migration übernommen hat). |
| `NET:HasHostRole()  -> bool` | True, wenn dieser Spieler gerade die Host-Rolle innehat. |
| `NET:HostRoleId()  -> int` | Die ID des Spielers, der die Host-Rolle innehat. |
| `NET:PeerCount()  -> int` | Anzahl der Spieler in der Teilnehmerliste, einschließlich sich selbst. |
| `NET:PeersJson()  -> string` | Die Teilnehmerliste als JSON-Array in Beitrittsreihenfolge. Jeder Eintrag ist {id, info, isHost, hostRole}: info ist der String, den dieser Spieler an SetLocalPlayer übergeben hat, isHost ist true für ID 1, hostRole ist true für den aktuellen Inhaber der Host-Rolle. |
| `NET:RotateHost()  -> void` | Übergibt die Host-Rolle an den nächsten Spieler in Beitrittsreihenfolge. Funktioniert nur auf dem Server; Aufrufe anderer Spieler ignoriert es. |
| `NET:SetHostRole(peerId)  -> void` | Gibt die Host-Rolle an den angegebenen Spieler und verkündet dies im Raum. Funktioniert nur auf dem Server und nur für IDs in der Teilnehmerliste. |

### Spielrunden

Während eines Online-Songs hält das Spiel die Lobby-Stage an, und der Spielbildschirm steuert den Austausch selbst: Er sendet den laufenden Punktestand des lokalen Spielers, versorgt die entfernten Spots aus dem Netz und wartet an den Lade- und Abschluss-Barrieren. Die Lobby muss nur festlegen, wer in welchem Spot spielt, und die Runde öffnen und schließen.

<div class="callout warn">
Spots sind die Spielerpositionen des Spielbildschirms: Spot 0 ist immer der lokale Spieler, Spots ab 1 sind entfernte Spieler. Setzen Sie die Zuordnung mit SetPlaySpots, rufen Sie dann BeginPlaySync unmittelbar vor dem Betreten des Spiels auf und EndPlaySync, sobald die Lobby die Kontrolle zurückerhält. Der Spielbildschirm ruft die Barrieren-, Score-Push- und Wahrscheinlichkeitsmethoden auf; sie erscheinen hier, weil dasselbe Objekt sie bereitstellt.
</div>

| Methode | Beschreibung |
| --- | --- |
| `NET:SetPlaySpots(json)  -> void` | Setzt die Zuordnung von Spots zu Spielern für das bevorstehende Spiel aus einem JSON-Array von IDs, wobei Index 0 der eigene ist (zum Beispiel "[1,3,2]"). Ein nicht parsbarer String löscht die Zuordnung. |
| `NET:PlaySpotCount()  -> int` | Anzahl der Einträge in der aktuellen Spot-Zuordnung (0, wenn keine gesetzt ist). |
| `NET:BeginPlaySync(selfName)  -> void` | Öffnet eine Spielrunde: löscht vorherige Punktestände und merkt sich den Anzeigenamen des lokalen Spielers für die Nachrichten mit dem laufenden Punktestand. |
| `NET:EndPlaySync()  -> void` | Schließt die Spielrunde und löscht die Barrieren und die Ergebnisse pro Spot. |
| `NET:IsRemoteSpot(spot)  -> bool` | True, wenn der Spot während einer offenen Spielrunde einem entfernten Spieler zugeordnet ist. |
| `NET:IsSpotActive(spot)  -> bool` | True, wenn der dem Spot zugeordnete Spieler noch in der Teilnehmerliste ist (Spot 0 ist immer true). False bedeutet, dass der Spieler mitten im Spiel ausgestiegen ist. |
| `NET:GetSpotPlayJson(spot)  -> string` | Das letzte JSON des laufenden Punktestands, das ein entfernter Spot gesendet hat, oder ein leerer String. Schlüssel: n (Name), s (Score), g (Gauge), a (Genauigkeit), gr (Great), gd (Good), ms (Bad), co (Combo). |
| `NET:GetSelfPlayScore()  -> string` | Das letzte JSON des laufenden Punktestands, das dieser Client gesendet hat. |
| `NET:LivePlayScoresJson()  -> string` | Das letzte JSON des laufenden Punktestands jedes Spielers, als JSON-Array von {id, d}, wobei d der Score-JSON-String ist. Das Array enthält den eigenen Spieler, sobald dieser Client mindestens einmal gesendet hat. |
| `NET:GetSpotResultJson(spot)  -> string` | Das Endergebnis-JSON, das ein entfernter Spot am Ende des Songs gemeldet hat, oder ein leerer String. Schlüssel: cl (Clear), fc (Full Combo), pf (Perfect), mx (Regenbogen-Gauge) als Booleans; gr, gd, ms, rl (Trommelwirbel), bl (Ballons), ad (Ad-libs), hc (höchste Combo), sc (Score) als Ganzzahlen. |
| `NET:GetSpotClearLevel(spot)  -> int` | Clear-Stufe aus dem Endergebnis eines entfernten Spots: 2 Regenbogen, 1 Clear, 0 gescheitert, -1, wenn noch kein Ergebnis eingetroffen ist. |
| `NET:GetSpotJudge(spot, key)  -> int` | Ein Ganzzahlfeld des Endergebnisses eines entfernten Spots per Schlüssel (siehe GetSpotResultJson), oder -1, wenn noch kein Ergebnis eingetroffen ist. |
| `NET:GetSpotBadOdds(spot)  -> int` | Wahrscheinlichkeit in Promille (0-1000), dass das Spiel den nächsten Auto-Treffer eines entfernten Spots als Bad wertet, abgeleitet aus dessen gesendeten Wertungszahlen. |
| `NET:GetSpotGoodOdds(spot)  -> int` | Wahrscheinlichkeit in Promille (0-1000), dass das Spiel den nächsten Auto-Treffer eines entfernten Spots als Good wertet. |
| `NET:PushPlayScore(json)  -> void` | Sendet das JSON des laufenden Punktestands des lokalen Spielers. Der Spielbildschirm ruft es mehrmals pro Sekunde auf. |
| `NET:ReportLoaded()  -> void` | Meldet einmalig, dass dieser Client das Laden des Songs abgeschlossen hat. |
| `NET:LoadBarrierReady(timeoutMs)  -> bool` | True, sobald jeder Spieler in der Teilnehmerliste das Laden gemeldet hat, oder sobald das Timeout seit der Meldung dieses Clients abgelaufen ist (der Server entfernt dann die Spieler, die nie gemeldet haben). |
| `NET:ReportFinished(resultJson)  -> void` | Meldet einmalig, dass dieser Client den Song beendet hat, mit seinem Endergebnis-JSON. |
| `NET:FinishBarrierReady(timeoutMs)  -> bool` | True, sobald jeder Spieler in der Teilnehmerliste das Beenden gemeldet hat, oder sobald das Timeout abgelaufen ist. |
| `NET:BarrierReset()  -> void` | Löscht die Lade- und Abschluss-Barrieren und die Ergebnisse pro Spot. |

## NetEvent

Das von NET:Poll() zurückgegebene Objekt, das ein Netzwerkereignis beschreibt.

<div class="callout warn">
Lesen Sie seine Felder direkt; es hat keine Methoden. Peer ist 0, wenn das Ereignis keinen zugehörigen Spieler hat.
</div>

| Methode | Beschreibung |
| --- | --- |
| `event.Type  -> string` | Die Ereignisart, einer der unten aufgeführten Werte. |
| `event.Peer  -> int` | Die zugehörige Spieler-ID: Ihre eigene ID bei "connected", der Absender bei "message", der betroffene Spieler bei "joined", "left" und "hostrole"; sonst 0. |
| `event.Channel  -> string` | Der Kanal eines "message"-Ereignisses; leer bei anderen Arten. |
| `event.Data  -> string` | Die Nutzdaten des Ereignisses; siehe unten. |

| Typ | Bedeutung |
| --- | --- |
| `connected` | Sie sind einem Raum beigetreten. Data ist ein JSON-Objekt {selfId, stageId, payload, hostRoleId}. Der Raumersteller erhält es nie. Spieler, die bereits im Raum sind, erscheinen in PeersJson und lösen keine "joined"-Ereignisse aus. |
| `joined` | Ein Spieler ist nach Ihnen beigetreten. Peer ist seine ID, Data ist sein Info-String. |
| `left` | Ein Spieler ist gegangen oder der Server hat ihn entfernt. Peer ist seine ID. |
| `message` | Eine Nachricht von einem anderen Spieler. Peer ist der Absender, Channel und Data sind die Nachricht. |
| `hostrole` | Die Host-Rolle hat den Inhaber gewechselt. Peer ist die ID des neuen Inhabers. |
| `roomclosed` | Der Raum wurde geschlossen, oder die Wiederherstellung der Verbindung ist fehlgeschlagen. |
| `error` | Eine Statusmeldung, die dem Spieler angezeigt werden soll. Data ist der Text. Der Client löst es aus, wenn das Netzwerk aus ist, wenn das Erstellen oder Beitreten fehlschlägt, wenn der Raum voll ist, sowie als Fortschrittshinweise während einer Host-Migration. |
