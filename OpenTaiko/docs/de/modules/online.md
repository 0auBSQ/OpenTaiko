<!-- modules/online.md -->

# Online

Die Logik der Online-Lobby: Räume, die per Code geöffnet und betreten werden, die Spielerliste, Schwierigkeit, Mods und Bereitschaft jedes Spielers, der Song des Raums, der Start einer Runde, die Ergebnisse und die nächste Runde. Die `Online`-Module zeichnen nichts; ein Skin zeichnet seine eigenen Lobby-Bildschirme und bindet seine eigene Songauswahl darauf an. Die Lobby jedes Skins spricht dieselben Nachrichten, sodass sich Spieler mit verschiedenen Skins in einem Raum treffen. Die Stage `onlinelobby` von Open-World Memories ist ein solcher Client. Die Module laufen auf dem [globalen NET](../api/networking.md), das experimentell ist.

## Die Module

| Modul | Inhalt |
| --- | --- |
| `Online.Lobby` | Die Sitzung: ihr Zustand, jede Aktion eines Bildschirms, die Anbindung der Songauswahl und das Abarbeiten der Ereignisse. |
| `Online.Protocol` | Die Nachrichten: Kanalnamen und das JSON jeder Nutzlast. |
| `Online.Identity` | Was ein Spieler über sich sendet (Name, Charakter, Puchichara, Namensschild, Dan-Plakette, Palette). |
| `Online.Media` | Der Song des Raums auf diesem Rechner: seine Level, sein Cover in der geteilten Textur `preimage`, seine Vorschau im geteilten Sound `presound`. |
| `Online.Texts` | Die eigenen Texte der Module (`lang/<code>/online.json`). |

Ein Skin braucht nur `Online.Lobby`, das weitergibt, was es von den anderen nutzt. Es gibt eine Sitzung pro Lua-Zustand: Das Modul hält seinen Zustand in `Lobby.net`.

## Der Zustand der Sitzung

Der Skin liest `Lobby.net` in jedem Frame, um seine Bildschirme zu zeichnen:

| Feld | Beschreibung |
| --- | --- |
| `online`, `connecting` | In einem Raum; wartet auf die Antwort eines Beitritts. |
| `isHost`, `code` | Dieser Rechner betreibt den Raum (er hat ihn erstellt); der Raumcode. |
| `roomGone` | Der Raum wurde geschlossen: verlassen und zum Menü des Skins zurückkehren. |
| `msg`, `msgCode`, `msgArg` | Die letzte Meldung, bereits in der Sprache des Spiels, ihr fester Code und ihr Argument (siehe Meldungen). |
| `notice` | Eine Meldung, die im Raum eingetroffen ist, einmal anzuzeigen und dann zu leeren. |
| `nameByPeer`, `infoByPeer` | Die Spieler nach ID: Namen und die Infos von `Online.Identity` (`name`, `char`, `puchi`, `dan`, `npk`, `dgold`, `dtype`, `palStops`, `palBlend`). |
| `diffByPeer`, `modByPeer`, `readyByPeer` | Schwierigkeit (0 bis 4), Mods und Bereitschaft jedes Spielers. |
| `lackByPeer`, `watchByPeer` | Die Spieler ohne den Song des Raums; die Spieler, die noch bei den Ergebnissen sind. |
| `song` | Der Song des Raums `{ uid, title, diff, speed, dyn }`, nil, bis die Host-Rolle einen wählt. |
| `songLevels`, `songPlus`, `songSubtitle`, `songBpm`, `iLackSong` | Der Song auf diesem Rechner: Level und Plus-Zeichen jeder vorhandenen Schwierigkeit, Untertitel und BPM; `iLackSong`, wenn dieser Rechner ihn nicht hat. |
| `resultByPeer` | Die Ergebnisse der Runde: `{ name, sc, gr, gd, ms, co, cl, fc, pf, ac, ab }` (Punkte, Great, Good, Bad, Combo, Clear, Full Combo, Perfect, Genauigkeit, abgebrochen). |
| `resultsT`, `resultsReadyT` | Sekunden bei den Ergebnissen; gesetzt, sobald sie vollständig sind. |

## Online.Lobby

| Funktion | Beschreibung |
| --- | --- |
| `Lobby.host()  -> bool` | Öffnet einen Raum (höchstens `Lobby.MAXP` = 5 Spieler) und speichert seinen Code mit `STORAGE:WriteLobbyCode`; false mit `net.msg`, wenn NET ablehnt. |
| `Lobby.join(code)  -> bool` | Beginnt den Beitritt zum Raum eines Codes (Leerzeichen werden ignoriert); die Antwort kommt über `drain`. False mit `net.msg`, wenn der Code leer, ungültig oder für eine andere Art von Raum ist. |
| `Lobby.leave()` | Beendet die Sitzung (auch nach einem gescheiterten Beitritt) und gibt die Spielerplätze den Speicherständen zurück. |
| `Lobby.drain()` | Verarbeitet jedes wartende NET-Ereignis; in jedem Frame aufrufen, solange `online` oder `connecting` gilt. |
| `Lobby.amController()  -> bool` | Dieser Spieler hat die Host-Rolle: Er wählt den Song und startet die Runde. |
| `Lobby.peerIds()`, `Lobby.count()` | Die IDs der Spieler der Reihe nach; ihre Anzahl. |
| `Lobby.spotOf[id]` | Der lokale Spielerplatz jedes Spielers (0 für sich selbst), für `NAMEPLATE:DrawPlayerNameplate`. |
| `Lobby.myDiff()`, `Lobby.setDiff(d)`, `Lobby.stepDiff(dir)  -> bool` | Meine Schwierigkeit; sie setzen; sie zur nächsten verschieben, die der Song hat (false am Ende der Reihe). |
| `Lobby.availDiffs()`, `Lobby.diffAvailable(d)`, `Lobby.fixDiff()` | Die Schwierigkeiten des Songs; meine auf einer davon halten (in jedem Lobby-Frame aufrufen). |
| `Lobby.myReady()`, `Lobby.setReady(b)` | Meine Bereitschaft. |
| `Lobby.broadcastMods()` | Liest die Mods von Spieler 0 aus `CONFIG` und sendet sie; aufrufen, wenn die Mod-Auswahl des Skins schließt. |
| `Lobby.applyModsToSlot(slot, mods)` | Legt die Mods eines Spielers auf einen `CONFIG`-Spielerplatz, um ihre Symbole mit der ROActivity `modicons` zu zeichnen. |
| `Lobby.adjustSpeed(delta)`, `Lobby.setSpeed(sp)` | Die Songgeschwindigkeit des Raums (nur Host-Rolle). |
| `Lobby.startProblem()  -> "need_song", "need_players" or nil` | Warum die Runde noch nicht starten kann. Bereitschaft ist nicht nötig. |
| `Lobby.hostStart()  -> bool` | Startet die Runde (nur Host-Rolle). |
| `Lobby.takeGo()  -> bool` | Einmal true, wenn die Runde begonnen hat; dann `launchPlay` aufrufen. |
| `Lobby.launchPlay()  -> true, "eject" or false` | Lädt den Song für jeden Spieler und öffnet die Spielrunde: bei true wechselt der Skin zu `"play"`; `"eject"` heißt, dieser Rechner hat den Song nicht und muss den Raum verlassen; false kommt mit `net.msg`. |
| `Lobby.returnedFromPlay()` | Aufrufen, wenn die Stage nach diesem Spiel erneut betreten wird: Es sendet mein Ergebnis und beginnt die Ergebnisse. |
| `Lobby.tickResults(dt)  -> bool` | In jedem Frame bei den Ergebnissen: true, sobald jedes Ergebnis da ist oder `Lobby.RESULTS_WAIT` (20) Sekunden vergangen sind. |
| `Lobby.standings()  -> rows` | Die Ergebnisse, beste Punktzahl zuerst: `{ id, name, r }`, `r` nil, solange dieser Spieler noch spielt. |
| `Lobby.nextRound()` | Zurück in die Lobby: Die Host-Rolle geht an den nächsten Spieler. |
| `Lobby.tickPreview(dt)`, `Lobby.stopPreview()` | Wiederholt die Songvorschau auf den Lobby- und Ergebnisbildschirmen; stoppt sie. |
| `Lobby.onEvent` | Eine optionale Funktion `(kind, peer, data)`, aufgerufen für jedes Ereignis, das `drain` verarbeitet (`"joined"`, `"left"`, `"song"`, `"ready"`, `"go"`, `"result"`, ...). |

Online-Spiel sieht aus wie Offline-Spiel mit derselben Spielerzahl: Die anderen Spieler nehmen die lokalen Plätze 2 bis 5 als virtuelle Slots ein, mit ihren eigenen Charakteren und Namensschildern, und der Spielbildschirm spielt sie nach den Daten aus dem Netz.

## Die Anbindung der Songauswahl

Der Spieler mit der Host-Rolle wählt den Song des Raums in der Songauswahl des Skins. Die Songauswahl darf nur einen Song wählen (kein Schwierigkeitsschritt, kein Auto); ihre Geschwindigkeitstasten ändern `CONFIG.SongSpeed`. Drei Aufrufe verbinden sie:

1. `Lobby.openSongSelect(open)` statt sie direkt zu aktivieren: `open` ist eine Funktion, die die Songauswahl aktiviert. Die Geschwindigkeit des Raums kommt zuerst in `CONFIG.SongSpeed`.
2. Wenn ein Song bestätigt wird: die Songauswahl deaktivieren, dann `Lobby.songPicked(uid?)`. `uid` ist standardmäßig `SONGMOUNT:ChosenUniqueId()`.
3. Wenn die Songauswahl ohne Song verlassen wird: sie deaktivieren, dann `Lobby.songPickCancelled()`, das eine dort geänderte Geschwindigkeit behält und Cover und Vorschau des Raums zurückbringt.

Der geteilte String `presound_path` benennt den Ton in `presound`: Eine Songauswahl, die den Song des Raums darin weiterlaufen lässt, gibt die Vorschau zurück, und die Lobby lässt sie weiterlaufen, statt sie neu zu starten.

## Meldungen

`net.msg` ist bereit zur Anzeige. Ein Skin, der seine Meldungen selbst formuliert, liest stattdessen `net.msgCode`:

| Code | Wann |
| --- | --- |
| `open_failed`, `join_failed` | NET hat ohne Begründung abgelehnt. |
| `room_open` | Der Raum ist offen und sein Code gespeichert. |
| `connecting` | Ein Beitritt hat begonnen. |
| `no_code`, `code_invalid` | Der Code ist leer oder kein Raumcode. |
| `code_other` | Der Code ist für eine andere Art von Raum; `msgArg` ist seine Stage-ID. |
| `chart_failed` | `launchPlay` konnte das Chart nicht laden. |
| `net` | Ein Text von NET selbst (Netzwerk aus, verlorene Verbindung, ...), auf Englisch. |

Das Netzwerk ist aus, bis der Spieler es in den Einstellungen erlaubt. Ein Skin sollte `CONFIG.AreLuaNetworkingConnectionsAllowed` prüfen, bevor er seine Lobby öffnet; NET lehnt außerdem mit einer `net`-Meldung ab.

## Online.Protocol

Jede Nachricht ist ein JSON-String auf einem NET-Kanal. Die Raum-ID im Code ist `"onlinelobby"` (`Protocol.ROOM_ID`), egal welche Stage ihn geöffnet hat.

| Kanal | Nutzlast | Gesendet |
| --- | --- | --- |
| `song` | `{"u":uid,"t":title,"d":0,"s":speed,"b":dyn}` | Von der Host-Rolle, wenn sich der Song oder seine Geschwindigkeit ändert, und an jeden Neuankömmling. |
| `diff`, `mod`, `ready` | `{"d":n}`, `{"r","st","ju","tz","ss","sm"}`, `{"r":bool}` | Wenn ein Spieler sie ändert, und an jeden Neuankömmling. |
| `have`, `watch` | `{"h":bool}`, `{"w":bool}` | Ob der Spieler den Song hat; ob er bei den Ergebnissen ist. |
| `go` | `{}` | Die Runde beginnt. |
| `result` | `{"n","sc","gr","gd","ms","co","cl","fc","pf","ac","ab"}` | Das Ergebnis eines Spielers nach dem Song. |

## Eine minimale Lobby

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

`Lobby.host()` oder `Lobby.join(code)` aus dem Menü des Skins setzen `screen` auf `"lobby"`; die obige Anbindung der Songauswahl setzt den Song.
