<!-- modules/online.md -->

# Online

De logica van de online lobby: kamers die met een code worden geopend en betreden, de spelerslijst, de moeilijkheid, mods en gereedheid van elke speler, het nummer van de kamer, de start van een ronde, de resultaten en de volgende ronde. De `Online`-modules tekenen niets; een skin tekent zijn eigen lobbyschermen en koppelt er zijn eigen nummerkeuze aan. De lobby van elke skin spreekt dezelfde berichten, zodat spelers met verschillende skins elkaar in één kamer treffen. De stage `onlinelobby` van Open-World Memories is zo'n client. De modules draaien op de [globale NET](../api/networking.md), die experimenteel is.

## De modules

| Module | Wat erin zit |
| --- | --- |
| `Online.Lobby` | De sessie: haar toestand, elke actie van een scherm, de koppeling met de nummerkeuze en het verwerken van gebeurtenissen. |
| `Online.Protocol` | De berichten: de kanaalnamen en de JSON van elke inhoud. |
| `Online.Identity` | Wat een speler over zichzelf stuurt (naam, personage, puchichara, naambord, dan-plaat, palet). |
| `Online.Media` | Het nummer van de kamer op deze machine: de niveaus, de hoes in de gedeelde textuur `preimage`, het voorproefje in het gedeelde geluid `presound`. |
| `Online.Texts` | De eigen teksten van de modules (`lang/<code>/online.json`). |

Een skin heeft alleen `Online.Lobby` nodig, dat doorgeeft wat het van de andere gebruikt. Er is één sessie per Lua-toestand: de module bewaart haar toestand in `Lobby.net`.

## De toestand van de sessie

De skin leest `Lobby.net` elke frame om zijn schermen te tekenen:

| Veld | Beschrijving |
| --- | --- |
| `online`, `connecting` | In een kamer; wacht op het antwoord op een deelname. |
| `isHost`, `code` | Deze machine draait de kamer (ze heeft hem gemaakt); de kamercode. |
| `roomGone` | De kamer is gesloten: verlaten en terug naar het menu van de skin. |
| `msg`, `msgCode`, `msgArg` | Het laatste bericht, al in de taal van het spel, zijn vaste code en zijn argument (zie Berichten). |
| `notice` | Een bericht dat binnenkwam in een kamer, om één keer te tonen en dan te wissen. |
| `nameByPeer`, `infoByPeer` | De spelers per id: de namen, en de info van `Online.Identity` (`name`, `char`, `puchi`, `dan`, `npk`, `dgold`, `dtype`, `palStops`, `palBlend`). |
| `diffByPeer`, `modByPeer`, `readyByPeer` | De moeilijkheid (0 tot 4), mods en gereedheid van elke speler. |
| `lackByPeer`, `watchByPeer` | De spelers zonder het nummer van de kamer; de spelers die nog bij de resultaten zijn. |
| `song` | Het nummer van de kamer `{ uid, title, diff, speed, dyn }`, nil tot de hostrol er een kiest. |
| `songLevels`, `songPlus`, `songSubtitle`, `songBpm`, `iLackSong` | Het nummer op deze machine: het niveau en het plusteken van elke moeilijkheid die het heeft, de ondertitel en de BPM; `iLackSong` als deze machine het niet heeft. |
| `resultByPeer` | De resultaten van de ronde: `{ name, sc, gr, gd, ms, co, cl, fc, pf, ac, ab }` (score, great, good, bad, combo, gehaald, full combo, perfect, nauwkeurigheid, afgebroken). |
| `resultsT`, `resultsReadyT` | Seconden bij de resultaten; gezet zodra ze compleet zijn. |

## Online.Lobby

| Functie | Beschrijving |
| --- | --- |
| `Lobby.host()  -> bool` | Opent een kamer (hoogstens `Lobby.MAXP` = 5 spelers) en bewaart de code met `STORAGE:WriteLobbyCode`; false met `net.msg` als NET weigert. |
| `Lobby.join(code)  -> bool` | Begint met deelnemen aan de kamer van een code (spaties worden genegeerd); het antwoord komt via `drain`. False met `net.msg` als de code leeg of ongeldig is, of voor een ander soort kamer. |
| `Lobby.leave()` | Beëindigt de sessie (ook na een mislukte deelname) en geeft de spelersplaatsen terug aan de opslagbestanden. |
| `Lobby.drain()` | Verwerkt elke wachtende NET-gebeurtenis; roep het elke frame aan zolang `online` of `connecting` waar is. |
| `Lobby.amController()  -> bool` | Deze speler heeft de hostrol: hij kiest het nummer en start de ronde. |
| `Lobby.peerIds()`, `Lobby.count()` | De id's van de spelers op volgorde; hun aantal. |
| `Lobby.spotOf[id]` | De lokale spelersplaats van elke speler (0 voor jezelf), voor `NAMEPLATE:DrawPlayerNameplate`. |
| `Lobby.myDiff()`, `Lobby.setDiff(d)`, `Lobby.stepDiff(dir)  -> bool` | Mijn moeilijkheid; die instellen; die naar de volgende verplaatsen die het nummer heeft (false aan het eind van de rij). |
| `Lobby.availDiffs()`, `Lobby.diffAvailable(d)`, `Lobby.fixDiff()` | De moeilijkheden van het nummer; de mijne op een daarvan houden (roep het elke lobbyframe aan). |
| `Lobby.myReady()`, `Lobby.setReady(b)` | Mijn gereedheid. |
| `Lobby.broadcastMods()` | Leest de mods van speler 0 uit `CONFIG` en stuurt ze; roep het aan als de modkeuze van de skin sluit. |
| `Lobby.applyModsToSlot(slot, mods)` | Zet de mods van een speler op een spelersplaats van `CONFIG`, om hun pictogrammen te tekenen met de ROActivity `modicons`. |
| `Lobby.adjustSpeed(delta)`, `Lobby.setSpeed(sp)` | De nummersnelheid van de kamer (alleen de hostrol). |
| `Lobby.startProblem()  -> "need_song", "need_players" or nil` | Waarom de ronde nog niet kan starten. Gereedheid is niet nodig. |
| `Lobby.hostStart()  -> bool` | Start de ronde (alleen de hostrol). |
| `Lobby.takeGo()  -> bool` | Eén keer waar als de ronde begonnen is; roep dan `launchPlay` aan. |
| `Lobby.launchPlay()  -> true, "eject" or false` | Laadt het nummer voor elke speler en opent de speelronde: bij true gaat de skin naar `"play"`; `"eject"` betekent dat deze machine het nummer niet heeft en de kamer moet verlaten; false komt met `net.msg`. |
| `Lobby.returnedFromPlay()` | Roep het aan als de stage na dat spel weer wordt betreden: het stuurt mijn resultaat en begint de resultaten. |
| `Lobby.tickResults(dt)  -> bool` | Elke frame bij de resultaten: waar zodra elk resultaat binnen is of `Lobby.RESULTS_WAIT` (20) seconden voorbij zijn. |
| `Lobby.standings()  -> rows` | De resultaten, beste score eerst: `{ id, name, r }`, `r` nil zolang die speler nog speelt. |
| `Lobby.nextRound()` | Terug naar de lobby: de hostrol gaat naar de volgende speler. |
| `Lobby.tickPreview(dt)`, `Lobby.stopPreview()` | Herhaalt het voorproefje van het nummer op de lobby- en resultatenschermen; stopt het. |
| `Lobby.onEvent` | Een optionele functie `(kind, peer, data)` die wordt aangeroepen voor elke gebeurtenis die `drain` verwerkt (`"joined"`, `"left"`, `"song"`, `"ready"`, `"go"`, `"result"`, ...). |

Online spelen ziet eruit als offline spelen met hetzelfde aantal spelers: de andere spelers nemen de lokale plaatsen 2 tot 5 in als virtuele slots, met hun eigen personages en naamborden, en het speelscherm laat ze spelen volgens het netwerk.

## De koppeling met de nummerkeuze

De speler met de hostrol kiest het nummer van de kamer in de nummerkeuze van de skin. Die mag alleen een nummer kiezen (geen moeilijkheidsstap, geen Auto); de snelheidstoetsen veranderen `CONFIG.SongSpeed`. Drie aanroepen koppelen haar:

1. `Lobby.openSongSelect(open)` in plaats van haar direct te activeren: `open` is een functie die de nummerkeuze activeert. De snelheid van de kamer komt eerst in `CONFIG.SongSpeed`.
2. Als een nummer is bevestigd: deactiveer de nummerkeuze, dan `Lobby.songPicked(uid?)`. `uid` is standaard `SONGMOUNT:ChosenUniqueId()`.
3. Als de nummerkeuze zonder nummer wordt verlaten: deactiveer haar, dan `Lobby.songPickCancelled()`, dat een daar gewijzigde snelheid houdt en de hoes en het voorproefje van de kamer terugzet.

De gedeelde string `presound_path` noemt de audio in `presound`: een nummerkeuze die het nummer van de kamer erin laat spelen, geeft het voorproefje terug, en de lobby laat het doorspelen in plaats van het opnieuw te starten.

## Berichten

`net.msg` is klaar om te tonen. Een skin die zijn berichten zelf formuleert, leest in plaats daarvan `net.msgCode`:

| Code | Wanneer |
| --- | --- |
| `open_failed`, `join_failed` | NET weigerde zonder reden. |
| `room_open` | De kamer is open en de code bewaard. |
| `connecting` | Een deelname is begonnen. |
| `no_code`, `code_invalid` | De code is leeg, of geen kamercode. |
| `code_other` | De code is voor een ander soort kamer; `msgArg` is de stage-id. |
| `chart_failed` | `launchPlay` kon de chart niet laden. |
| `net` | Een tekst van NET zelf (netwerk uit, verbinding verloren, ...), in het Engels. |

Het netwerk staat uit tot de speler het in de instellingen toestaat. Een skin hoort `CONFIG.AreLuaNetworkingConnectionsAllowed` te controleren voordat hij zijn lobby opent; NET weigert ook met een `net`-bericht.

## Online.Protocol

Elk bericht is een JSON-string op één NET-kanaal. De kamer-id in de code is `"onlinelobby"` (`Protocol.ROOM_ID`), welke stage hem ook opende.

| Kanaal | Inhoud | Verzonden |
| --- | --- | --- |
| `song` | `{"u":uid,"t":title,"d":0,"s":speed,"b":dyn}` | Door de hostrol, als het nummer of de snelheid verandert, en naar elke nieuwkomer. |
| `diff`, `mod`, `ready` | `{"d":n}`, `{"r","st","ju","tz","ss","sm"}`, `{"r":bool}` | Als een speler ze verandert, en naar elke nieuwkomer. |
| `have`, `watch` | `{"h":bool}`, `{"w":bool}` | Of de speler het nummer heeft; of hij bij de resultaten is. |
| `go` | `{}` | De ronde begint. |
| `result` | `{"n","sc","gr","gd","ms","co","cl","fc","pf","ac","ab"}` | Het resultaat van een speler na het nummer. |

## Een minimale lobby

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

`Lobby.host()` of `Lobby.join(code)` vanuit het menu van de skin zetten `screen` op `"lobby"`; de koppeling met de nummerkeuze hierboven zet het nummer.
