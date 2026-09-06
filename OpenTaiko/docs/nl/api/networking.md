<!-- api/networking.md -->

# Online netwerken <span class="badge-exp">Experimenteel</span>

De `NET`-global is de client voor OpenTaiko Online, het peer-to-peer-protocol achter de online lobby en de multiplayerstages. Eén speler maakt een room aan en ontvangt een roomcode; andere spelers treden toe met die code. De server (de maker van de room, tenzij de room is gemigreerd) stuurt al het verkeer door, en elke speler houdt een spelerslijst bij van wie zich in de room bevindt.

Hoe Lua het gebruikt:

- Alle data passeert de Lua-grens als strings. Stages dragen gestructureerde data als JSON over.
- Het netwerk draait op achtergrondthreads en zet gebeurtenissen in een wachtrij. Een stage leegt de wachtrij elk frame door `NET:Poll()` in een lus aan te roepen tot die nil teruggeeft.
- Netwerken staat standaard uit. De instelling "Allow LuaNetworking connections" schakelt het in; zolang het uit staat, geeft CreateRoom nil terug, geeft JoinRoom false terug, en zet de client een "error"-gebeurtenis in de wachtrij.
- Room-id's: de maker van de room is id 1. Elke toetreder ontvangt het volgende id in toetredingsvolgorde. `NET:SelfId()` is 0 buiten een room.
- De hostrol staat los van het bezit van de verbinding. Ze markeert de speler die momenteel de lobby aanstuurt (het nummer kiest, de spelbeurt start). Ze begint bij de maker; RotateHost of SetHostRole draagt ze over aan een andere speler.
- Als de maker van de room vertrekt, migreren de overblijvende spelers automatisch naar een nieuwe server; een room sluit pas wanneer niemand het kan overnemen.

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

### Levenscyclus van een room

<div class="callout warn">
Beschikbaar als de global NET. Roep SetLocalPlayer aan vóór CreateRoom of JoinRoom, zodat de andere spelers je info bij toetreding ontvangen. JoinRoom is asynchroon: de uitkomst komt later als een "connected"- of "error"-gebeurtenis. Een room aanmaken of ertoe toetreden verlaat eerst elke huidige room.
</div>

| Methode | Beschrijving |
| --- | --- |
| `NET:SetLocalPlayer(infoJson)  -> void` | Stelt de infostring van de lokale speler in (doorgaans JSON met het naamplaatje, personage enzovoort). De client stuurt ze bij toetreding naar de andere spelers, en ze verschijnt in hun "joined"-gebeurtenissen en PeersJson. Een lege string wordt "{}"; de server vervangt een string langer dan 16384 tekens door "{}". |
| `NET:CreateRoom(stageId, payload, maxPlayers)  -> string` | Maakt een room aan voor het gegeven stage-id met een optionele payloadstring en geeft de roomcode terug, of nil als netwerken uit staat of het aanmaken van de room is mislukt. maxPlayers begrenst de spelerslijst (standaard 8). |
| `NET:CreateRoom(stageId, payload)  -> string` | Hetzelfde met de standaardlimiet van 8 spelers. |
| `NET:PeekStageId(roomCode)  -> string` | Geeft het stage-id terug dat in een roomcode is opgeslagen, zonder verbinding te maken, of nil als de code ongeldig is. Laat een toetredingsscherm naar de juiste stage routeren. |
| `NET:JoinRoom(roomCode)  -> bool` | Begint met toetreden tot de room. Geeft onmiddellijk false terug als netwerken uit staat of de code misvormd is; anders komt het resultaat als een "connected"- of "error"-gebeurtenis. |
| `NET:Leave()  -> void` | Verlaat de room en wist de gebeurteniswachtrij. Wanneer de maker van de room vertrekt terwijl anderen blijven, neemt een andere speler de room over; anders sluit hij voor iedereen. |
| `NET.PortOverride  (int, settable)` | Indien groter dan 0 gebruiken rooms deze TCP-poort; 0 kiest de standaardpoort 41234. Laat het op 0 bij normaal gebruik. |

### Berichten

<div class="callout warn">
De stage kiest kanaalnamen vrij. Het mechanisme voor speelrondes reserveert de kanaalnamen "ps", "ld" en "fn", die Poll() nooit bereiken.
</div>

| Methode | Beschrijving |
| --- | --- |
| `NET:Broadcast(channel, data)  -> void` | Stuurt een bericht op het gegeven kanaal naar elke andere speler in de room. |
| `NET:SendTo(peerId, channel, data)  -> void` | Stuurt een bericht op het gegeven kanaal naar één speler op id. -1 stuurt naar alle anderen, zoals Broadcast. |
| `NET:Poll()  -> NetEvent` | Verwijdert en geeft de volgende wachtende gebeurtenis terug, of nil wanneer de wachtrij leeg is. |

### Roomstatus en hostrol

| Methode | Beschrijving |
| --- | --- |
| `NET:SelfId()  -> int` | Het id van deze speler (1 voor de maker van de room), of 0 wanneer niet in een room. |
| `NET:Connected()  -> bool` | True wanneer in een room: deze speler heeft een eigen id en is ofwel de server ofwel ermee verbonden. |
| `NET:IsHost()  -> bool` | True wanneer deze speler de roomverbinding bezit (de maker van de room, of de speler die na een migratie heeft overgenomen). |
| `NET:HasHostRole()  -> bool` | True wanneer deze speler momenteel de hostrol heeft. |
| `NET:HostRoleId()  -> int` | Het id van de speler die de hostrol heeft. |
| `NET:PeerCount()  -> int` | Aantal spelers in de spelerslijst, inclusief jezelf. |
| `NET:PeersJson()  -> string` | De spelerslijst als JSON-array in toetredingsvolgorde. Elk item is {id, info, isHost, hostRole}: info is de string die die speler aan SetLocalPlayer heeft gegeven, isHost is true voor id 1, hostRole is true voor de huidige houder van de hostrol. |
| `NET:RotateHost()  -> void` | Draagt de hostrol over aan de volgende speler in toetredingsvolgorde. Werkt alleen op de server; het negeert aanroepen van andere spelers. |
| `NET:SetHostRole(peerId)  -> void` | Geeft de hostrol aan de gegeven speler en kondigt dit aan in de room. Werkt alleen op de server en alleen voor id's in de spelerslijst. |

### Speelrondes

Tijdens een online nummer schort het spel de lobbystage op en stuurt het gameplayscherm de uitwisseling zelf aan: het broadcast de tussenstand van de lokale speler, voedt de spots op afstand vanaf het netwerk, en wacht op de laad- en voltooiingsbarrières. De lobby hoeft alleen te bepalen wie op welke spot speelt en de ronde te openen en te sluiten.

<div class="callout warn">
Spots zijn de spelerposities van het gameplayscherm: spot 0 is altijd de lokale speler, spots 1 en hoger zijn spelers op afstand. Stel de toewijzing in met SetPlaySpots, roep dan BeginPlaySync aan vlak voordat de spelbeurt wordt betreden, en EndPlaySync zodra de lobby de controle terugkrijgt. Het gameplayscherm roept de barrière-, scoreverzend- en kansmethoden aan; ze staan hier vermeld omdat hetzelfde object ze beschikbaar stelt.
</div>

| Methode | Beschrijving |
| --- | --- |
| `NET:SetPlaySpots(json)  -> void` | Stelt de toewijzing van spot naar speler voor de komende spelbeurt in vanuit een JSON-array van id's, waarbij index 0 jezelf is (bijvoorbeeld "[1,3,2]"). Een niet-parseerbare string wist de toewijzing. |
| `NET:PlaySpotCount()  -> int` | Aantal items in de huidige spottoewijzing (0 wanneer er geen is ingesteld). |
| `NET:BeginPlaySync(selfName)  -> void` | Opent een speelronde: wist vorige scores en registreert de weergavenaam van de lokale speler voor de tussenstandberichten. |
| `NET:EndPlaySync()  -> void` | Sluit de speelronde en wist de barrières en de resultaten per spot. |
| `NET:IsRemoteSpot(spot)  -> bool` | True wanneer de spot tijdens een open speelronde aan een speler op afstand is toegewezen. |
| `NET:IsSpotActive(spot)  -> bool` | True wanneer de speler die aan de spot is toegewezen nog in de spelerslijst staat (spot 0 is altijd true). False betekent dat de speler halverwege de spelbeurt is weggevallen. |
| `NET:GetSpotPlayJson(spot)  -> string` | De laatste tussenstand-JSON die een spot op afstand heeft gestuurd, of een lege string. Sleutels: n (naam), s (score), g (gauge), a (nauwkeurigheid), gr (great), gd (good), ms (bad), co (combo). |
| `NET:GetSelfPlayScore()  -> string` | De laatste tussenstand-JSON die deze client heeft gebroadcast. |
| `NET:LivePlayScoresJson()  -> string` | De laatste tussenstand-JSON van elke speler, als JSON-array van {id, d} waarbij d de score-JSON-string is. De array neemt jezelf mee zodra deze client minstens eenmaal heeft gebroadcast. |
| `NET:GetSpotResultJson(spot)  -> string` | De eindresultaat-JSON die een spot op afstand aan het einde van het nummer heeft gerapporteerd, of een lege string. Sleutels: cl (clear), fc (full combo), pf (perfect), mx (regenbooggauge) als booleans; gr, gd, ms, rl (roffels), bl (ballonnen), ad (ad-libs), hc (hoogste combo), sc (score) als gehele getallen. |
| `NET:GetSpotClearLevel(spot)  -> int` | Clearniveau uit het eindresultaat van een spot op afstand: 2 regenboog, 1 clear, 0 mislukt, -1 wanneer er nog geen resultaat is aangekomen. |
| `NET:GetSpotJudge(spot, key)  -> int` | Eén integerveld van het eindresultaat van een spot op afstand op sleutel (zie GetSpotResultJson), of -1 wanneer er nog geen resultaat is aangekomen. |
| `NET:GetSpotBadOdds(spot)  -> int` | Kans in promille (0-1000) dat het spel de volgende automatische slag van een spot op afstand als bad beoordeelt, afgeleid van zijn gebroadcaste beoordelingsaantallen. |
| `NET:GetSpotGoodOdds(spot)  -> int` | Kans in promille (0-1000) dat het spel de volgende automatische slag van een spot op afstand als good beoordeelt. |
| `NET:PushPlayScore(json)  -> void` | Broadcast de tussenstand-JSON van de lokale speler. Het gameplayscherm roept het meerdere keren per seconde aan. |
| `NET:ReportLoaded()  -> void` | Rapporteert eenmalig dat deze client klaar is met het laden van het nummer. |
| `NET:LoadBarrierReady(timeoutMs)  -> bool` | True zodra elke speler in de spelerslijst heeft gerapporteerd dat hij geladen is, of zodra de time-out is verstreken sinds deze client heeft gerapporteerd (de server laat dan de spelers vallen die nooit hebben gerapporteerd). |
| `NET:ReportFinished(resultJson)  -> void` | Rapporteert eenmalig dat deze client het nummer heeft voltooid, met zijn eindresultaat-JSON. |
| `NET:FinishBarrierReady(timeoutMs)  -> bool` | True zodra elke speler in de spelerslijst heeft gerapporteerd dat hij klaar is, of zodra de time-out is verstreken. |
| `NET:BarrierReset()  -> void` | Wist de laad- en voltooiingsbarrières en de resultaten per spot. |

## NetEvent

Het object dat NET:Poll() teruggeeft en één netwerkgebeurtenis beschrijft.

<div class="callout warn">
Lees de velden rechtstreeks; het heeft geen methoden. Peer is 0 wanneer de gebeurtenis geen bijbehorende speler heeft.
</div>

| Methode | Beschrijving |
| --- | --- |
| `event.Type  -> string` | De soort gebeurtenis, een van de hieronder vermelde waarden. |
| `event.Peer  -> int` | Het bijbehorende speler-id: je eigen id voor "connected", de afzender voor "message", de betrokken speler voor "joined", "left" en "hostrole"; anders 0. |
| `event.Channel  -> string` | Het kanaal van een "message"-gebeurtenis; leeg voor andere soorten. |
| `event.Data  -> string` | De payload van de gebeurtenis; zie hieronder. |

| Type | Betekenis |
| --- | --- |
| `connected` | Je bent tot een room toegetreden. Data is een JSON-object {selfId, stageId, payload, hostRoleId}. De maker van de room ontvangt het nooit. Spelers die al in de room zijn, verschijnen in PeersJson en werpen geen "joined"-gebeurtenissen op. |
| `joined` | Een speler is na jou toegetreden. Peer is zijn id, Data is zijn infostring. |
| `left` | Een speler is vertrokken of de server heeft hem laten vallen. Peer is zijn id. |
| `message` | Een bericht van een andere speler. Peer is de afzender, Channel en Data zijn het bericht. |
| `hostrole` | De hostrol is van eigenaar gewisseld. Peer is het id van de nieuwe houder. |
| `roomclosed` | De room is gesloten, of het herstel van de verbinding is mislukt. |
| `error` | Een statusbericht om aan de speler te tonen. Data is de tekst. De client werpt het op wanneer netwerken uit staat, wanneer aanmaken of toetreden mislukt, wanneer de room vol is, en als voortgangsmeldingen tijdens een hostmigratie. |
