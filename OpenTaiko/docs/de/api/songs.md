<!-- api/songs.md -->

# Songs und Charts

Anfordern der Songliste, Durchlaufen von Knoten und Charts, Lesen von Scores und Aufbau von Dan-Kursen (Prüfungen).

Auf dieser Seite verwendete Konventionen:

- Schwierigkeitsindizes sind 0-basiert: 0 Easy, 1 Normal, 2 Hard, 3 Extreme (`Oni`), 4 Extra Extreme (`Edit`), 5 Tower, 6 Dan.
- Spieler- und Spielstandindizes sind 0-basiert (0 ist Spieler 1).
- Member, die mit einem Punkt geschrieben werden (`node.Title`), sind Eigenschaften; Member, die mit einem Doppelpunkt geschrieben werden (`node:GetChart(3)`), sind Methoden.
- Einige Member geben C#-Collections zurück. Eine Liste hat `.Count` und ist ab 0 indiziert (`list[0]`); ein Array hat `.Length` und ist ebenfalls ab 0 indiziert. Jeder Eintrag unten gibt an, welches davon er zurückgibt.
- Die Songliste ist erst vollständig, nachdem die Song-Enumeration abgeschlossen ist. Fordern Sie sie aus dem Callback `afterSongEnum()` an (siehe [Module und Lebenszyklus](activities.md)) oder prüfen Sie zuerst die globale Funktion `IsSongsEnumDone()`; sie gibt true zurück, sobald die Enumeration abgeschlossen ist.

## Die Songliste anfordern

### RequestSongList

Globale Funktion, die aus einem Einstellungsobjekt eine navigierbare Songliste aufbaut.

<div class="callout warn">
Als einfache globale Funktion verfügbar. Übergeben Sie ihr ein Einstellungsobjekt, das mit GenerateSongListSettings() erzeugt wurde. Der Aufruf baut den Songbaum einmal aus den vom Spiel enumerierten Songs auf; das Handle hält das Einstellungsobjekt als Referenz, sodass Sie ein Feld ändern und ReloadSongList() des Handles aufrufen können, um neu aufzubauen.
</div>

| Methode | Beschreibung |
| --- | --- |
| `RequestSongList(settings)  -> song list handle` | Baut aus den angegebenen Songlisten-Einstellungen ein Songlisten-Handle auf und gibt es zurück. |

```lua
local settings = GenerateSongListSettings()
settings.AppendMainRandomBox = false
settings:SetExcludedGenreFolders({ "Dan", "Tower" })

local list = RequestSongList(settings)
local node = list:GetSelectedSongNode()
```

### GenerateSongListSettings

Globale Funktion, die ein Songlisten-Einstellungsobjekt mit Standardwerten erzeugt.

| Methode | Beschreibung |
| --- | --- |
| `GenerateSongListSettings()  -> song list settings` | Gibt ein neues Songlisten-Einstellungsobjekt mit Standardfeldwerten zurück. |

### Songlisten-Einstellungen

Konfigurationsobjekt, das steuert, welche Knoten eine Songliste enthält und wie sich die Navigation verhält.

<div class="callout warn">
Alle Member unten sind öffentliche Felder, die Lua direkt liest und schreibt (settings.HideEmptyFolders = false), mit Ausnahme der beiden Setter-Methoden, die eine Lua-Tabelle nehmen. ExcludedGenreFolders und MandatoryDifficultyList sind C#-Arrays; setzen Sie sie über ihre Setter-Methoden.
</div>

| Methode | Beschreibung |
| --- | --- |
| `settings.AppendMainRandomBox  (bool, default true)` | Wenn true, hängt die Liste an ihre Wurzel eine Zufalls-Box an. |
| `settings.AppendSubRandomBoxes  (bool, default true)` | Wenn true, hängt die Liste am Ende jedes Ordners eine Zufalls-Box an. |
| `settings.SubBackBoxFrequency  (int, default 7)` | Innerhalb jedes Ordners fügt die Liste am Anfang und nach jeweils N Einträgen eine Zurück-Box ein; 0 deaktiviert generierte Zurück-Boxen. |
| `settings.ExcludedGenreFolders  (string array)` | Namen von Genre-Ordnern, die die Liste weglässt. Setzen Sie es mit SetExcludedGenreFolders. |
| `settings.RootGenreFolder  (string, default nil)` | Wenn gesetzt, wird der erste Ordner (Tiefensuche), dessen Genre diesem Namen entspricht, zur Wurzel der Liste; bei nil ist die Wurzel die oberste Ebene. |
| `settings.RootGenreFolderNode  (song node, default nil)` | Knotenform von RootGenreFolder. Wenn gesetzt, hat er Vorrang vor dem String, was Ordner mit gleichem Genrenamen eindeutig macht. |
| `settings.MandatoryDifficultyList  (Difficulty array, default nil)` | Schwierigkeitsgrade, die ein Song haben muss, um in der Liste zu erscheinen; nil bedeutet keine Anforderung. Setzen Sie es mit SetMandatoryDifficultyList. |
| `settings.MandatoryDifficultyMatchAll  (bool, default true)` | true verlangt jeden aufgeführten Schwierigkeitsgrad (UND); false verlangt mindestens einen (ODER). |
| `settings.HideEmptyFolders  (bool, default true)` | Blendet Ordner ohne sichtbaren Song aus, rekursiv. |
| `settings.FlattenOpenedFolders  (bool, default true)` | Wenn true, ist die aktuelle Seite der gesamte Baum, wobei geöffnete Ordner an Ort und Stelle ausgeklappt sind (geschlossene Ordner zählen als einzelne Einträge). Wenn false, enthält die Seite nur die Geschwister des Cursor-Knotens. |
| `settings.ModuloPagination  (bool, default true)` | Wenn true, läuft GetSongNodeAtOffset am Seitenende um; wenn false, gibt es jenseits der beiden Enden nil zurück. |
| `settings.ModuloMovement  (bool, default true)` | Wenn true, läuft Move am Seitenende um; wenn false, stoppt es an den beiden Enden. |
| `settings.ExcludeHiddenSongs  (bool, default true)` | Schließt Songs aus, deren HiddenIndex 3 (versteckt) ist. |
| `settings.ExcludeLockedSongs  (bool, default false)` | Wenn true, lassen die Seiten gesperrte Songs weg, sodass die Navigation nie auf ihnen landet. |
| `settings.IgnoreUnlockables  (bool, default false)` | Wenn true, ignoriert die Liste ExcludeLockedSongs, und GetRandomNodeInFolder kann gesperrte Songs zurückgeben. Knoteneigenschaften wie IsLocked melden weiterhin den tatsächlichen Zustand. |
| `settings:SetExcludedGenreFolders(table)  -> void` | Setzt ExcludedGenreFolders aus einer Lua-Tabelle von Genrenamen-Strings. |
| `settings:SetMandatoryDifficultyList(table)  -> void` | Setzt MandatoryDifficultyList aus einer Lua-Tabelle von Schwierigkeitsindizes. |

### Songlisten-Handle

Ein navigierbarer Songbaum, der von RequestSongList zurückgegeben wird, mit Cursor, Ordnernavigation und Suche.

<div class="callout warn">
Suchmethoden nehmen eine Lua-Funktion, die einen Song-Knoten erhält und einen Boolean zurückgibt. Methoden, die mehrere Knoten zurückgeben, liefern eine C#-Liste (.Count, 0-basierte Indizierung).
</div>

| Methode | Beschreibung |
| --- | --- |
| `list:ReloadSongList()  -> void` | Baut den gesamten Baum aus den aktuellen Songs und Einstellungen neu auf und setzt den Cursor auf den ersten Knoten. |
| `list:GetRoot()  -> song node` | Gibt den Wurzelknoten des Baums zurück. |
| `list:GetSelectedSongNode()  -> song node` | Gibt den Knoten unter dem Cursor zurück, oder nil, wenn die Liste leer ist. |
| `list:GetSongNodeAtOffset(offset)  -> song node` | Gibt den Knoten mit dem angegebenen Abstand vom Cursor innerhalb der aktuellen Seite zurück; läuft um oder gibt nil zurück, je nach ModuloPagination. |
| `list:Move(offset)  -> void` | Bewegt den Cursor um den angegebenen Abstand innerhalb der aktuellen Seite; läuft um oder stoppt am Ende, je nach ModuloMovement. |
| `list:OpenFolder()  -> bool` | Öffnet den Ordner unter dem Cursor und setzt den Cursor auf sein erstes Kind; gibt false zurück, wenn der Cursor nicht auf einem geschlossenen, nicht-leeren Ordner steht. |
| `list:CloseFolder()  -> bool` | Schließt den Ordner, der den Cursor enthält, und setzt den Cursor auf diesen Ordner; gibt false zurück, wenn es nichts zu schließen gibt. Beim Verlassen eines virtuellen Ordners wird der von OpenVirtualFolder gespeicherte Cursor wiederhergestellt. |
| `list:OpenVirtualFolder(baseFolder, songs, title)  -> bool` | Öffnet einen temporären Ordner namens `title`, der die Song-Knoten aus der Lua-Tabelle `songs` (Schlüssel 1..n) enthält, mit generierten Zurück-Boxen und einer abschließenden Zufalls-Box, und setzt den Cursor hinein. `baseFolder` wird zum übergeordneten Ordner des virtuellen Ordners. Gibt false zurück, wenn die Tabelle keine Song-Knoten enthält. |
| `list:GetSongByUniqueId(id)  -> song node` | Gibt den ersten Song zurück, dessen eindeutige ID passt, oder nil. |
| `list:GetRandomNodeInFolder(node, recursive, predicate)  -> song node` | Wählt einen zufälligen Song unter den Geschwistern von `node` (die Seite, die ihn enthält). Mit `recursive` (Standard true) umfasst die Auswahl auch Songs in Geschwisterordnern. Gesperrte Songs überspringt sie, sofern nicht IgnoreUnlockables gesetzt ist. `predicate` ist optional. Gibt nil zurück, wenn nichts in Frage kommt. |
| `list:SearchSongsByPredicate(predicate)  -> list of song nodes` | Gibt jeden Song-Knoten im Baum zurück, für den das Prädikat true liefert. |
| `list:SearchFirstSongByPredicate(predicate)  -> song node` | Gibt den ersten Song-Knoten zurück, für den das Prädikat true liefert, oder nil. |
| `list:SearchNodesByPredicate(predicate)  -> list of song nodes` | Wie SearchSongsByPredicate, prüft aber auch Ordner und andere Nicht-Song-Knoten. |

```lua
local results = list:SearchSongsByPredicate(function(node)
    return node:GetChart(3) ~= nil   -- hat ein Extreme-Chart
end)
for i = 0, results.Count - 1 do
    local node = results[i]
end
```

## Knoten, Charts und Scores

### Song-Knoten

Ein einzelner Eintrag in der Songliste: ein Song, ein Ordner, eine Zurück-Box oder eine Zufalls-Box.

<div class="callout warn">
Das Songlisten-Handle, SONGMOUNT:ChosenSongNode() und DANBUILDER:GetSong() geben Song-Knoten zurück. Eigenschaften sind schreibgeschützt. Metadaten-Eigenschaften geben auf Knoten, die keine Songs sind, nil zurück. Navigieren Sie in der Liste über das Songlisten-Handle (Move, OpenFolder, CloseFolder und die Suchmethoden).
</div>

| Methode | Beschreibung |
| --- | --- |
| `node.NotNull  (bool)` | True, wenn der Knoten einen echten Songlisteneintrag kapselt. |
| `node.IsFolder  (bool)` | True, wenn der Knoten ein Ordner ist. |
| `node.IsRandom  (bool)` | True, wenn der Knoten eine Zufalls-Box ist. |
| `node.IsReturn  (bool)` | True, wenn der Knoten eine Zurück-Box ist. |
| `node.IsSong  (bool)` | True, wenn der Knoten ein spielbarer Song ist. |
| `node.SongCount  (int)` | Anzahl der direkten Kind-Songs. |
| `node.RecursiveSongCount  (int)` | Anzahl der Songs unter diesem Knoten, einschließlich Unterordnern. |
| `node.VisibleSongCount  (int)` | Anzahl der direkten Kind-Songs, deren HiddenIndex nicht 3 ist. |
| `node.RecursiveVisibleSongCount  (int)` | Anzahl der sichtbaren Songs unter diesem Knoten, einschließlich Unterordnern. |
| `node.BoxType  (string)` | Der Stil-String der Ordner-Box, oder nil. |
| `node.BgType  (string)` | Der Hintergrundstil-String, oder nil. |
| `node.BoxChara  (string)` | Der Box-Charakter-String, oder nil. |
| `node.ForeColor  (color)` | Die Vordergrundfarbe des Knotens, oder nil. |
| `node.BackColor  (color)` | Die Hintergrundfarbe des Knotens, oder nil. |
| `node.BoxColor  (color)` | Die Box-Farbe des Knotens, oder nil. |
| `node.Title  (string)` | Der Anzeigetitel. Zurück- und Zufalls-Boxen geben den lokalisierten Text "Return" / "Random" zurück, der aus dem Titel des übergeordneten Ordners gebildet wird. |
| `node.Subtitle  (string)` | Der Untertitel des Songs, oder nil. |
| `node.Genre  (string)` | Der Genre-String, oder nil. |
| `node.UniqueId  (string)` | Die eindeutige ID des Songs, oder nil. |
| `node.Maker  (string)` | Das Feld MAKER als ein String, oder nil. |
| `node.Charters  (string array)` | Das Feld MAKER, an Kommas geteilt. |
| `node.Side  (int)` | Der SIDE-Wert: 0 normal, 1 ex, 2 beide. |
| `node.Explicit  (bool)` | True, wenn der Song als explizit gekennzeichnet ist; nil für Nicht-Songs. |
| `node.HasVideo  (bool)` | True, wenn der Song ein Hintergrundvideo hat; nil für Nicht-Songs. |
| `node.DemoStart  (int)` | Der Offset der Vorschau-BGM in Millisekunden. |
| `node.AudioPath  (string)` | Der absolute Pfad der BGM-Datei des Songs, oder ein leerer String. |
| `node.HasPreimage  (bool)` | True, wenn der Song ein Preimage deklariert. |
| `node.PreimagePath  (string)` | Der absolute Pfad des Preimage. Prüfen Sie zuerst HasPreimage; ohne Preimage ist dies nur der Songordner. |
| `node:GetPreimage()  -> texture` | Lädt das Preimage von der Festplatte und gibt eine neue Textur zurück, oder nil, wenn der Song keines hat. Geben Sie die Textur frei, wenn Sie fertig sind. |
| `node.ChartMd5  (string)` | MD5 der Chart-Datei (Hex in Großbuchstaben), oder ein leerer String. Bleibt über Installationen hinweg gleich; UniqueId nicht. |
| `node:GetChart(diff)  -> chart` | Gibt das Chart für den angegebenen Schwierigkeitsindex zurück, oder nil, wenn der Song kein solches Chart hat. |
| `node:GetCustomCommand(key)  -> string` | Gibt den Wert eines benutzerdefinierten Befehls auf globaler Ebene zurück (ein mit Punkt beginnender Header vor dem ersten COURSE; key enthält den Punkt, zum Beispiel ".VAULT_NAME"), oder nil. |
| `node:GetCustomCommands()  -> dictionary` | Gibt alle benutzerdefinierten Befehle auf globaler Ebene als C#-Dictionary-Objekt zurück. Bevorzugen Sie GetCustomCommand für Abfragen. |
| `node.UnlockCondition  (unlock condition)` | Das Freischaltbedingungsobjekt (siehe Freischaltbedingung). |
| `node.UnlockText  (string)` | Der benutzerdefinierte Freischalttext, falls der Song einen definiert, andernfalls die generierte Bedingungsmeldung. |
| `node.IsLocked  (bool)` | True, wenn dieser Song gerade gesperrt ist; für Nicht-Songs immer false. |
| `node.HiddenIndex  (int)` | Der Anzeigezustand des Freischaltsystems: 0 angezeigt, 1 ausgegraut, 2 verschleiert, 3 versteckt (0 für Nicht-Songs). |
| `node.Rarity  (string)` | Die Seltenheitsbezeichnung; "Common" für Songs ohne Freischalteintrag, "-" für Nicht-Songs. |
| `node:Mount(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | Wählt diesen Song mit dem angegebenen Schwierigkeitsindex pro Spieler zum Spielen aus (Standard 0). Prüft nur die ersten CONFIG.PlayerCount Indizes und gibt false zurück, wenn der Knoten kein Song ist oder der Schwierigkeitsgrad eines aktiven Spielers fehlt oder außerhalb des Bereichs liegt. |
| `node:MountIfNotLocked(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | Wie Mount, gibt aber false zurück, ohne zu mounten, wenn der Song gesperrt ist. |

### Chart

Ein Schwierigkeitsgrad eines Songs: Level, BPM, Chart-Autoren, Tower- und Dan-Daten, Bestleistungen und benutzerdefinierte Befehle.

<div class="callout warn">
GetChart(diff) eines Song-Knotens gibt ein Chart zurück. Eigenschaften sind schreibgeschützt. BPM, Life, TotalFloorCount, TowerType und DanTick geben nil zurück, wenn das Chart keine Chart-Info hat. Difficulty und LevelIcon sind Enum-Objekte; vergleichen Sie sie über DifficultyAsInt, IsPlus und IsMinus.
</div>

| Methode | Beschreibung |
| --- | --- |
| `chart.NotNull  (bool)` | True, wenn das Chart echte Chart-Daten kapselt. |
| `chart.Parent  (song node)` | Der Song-Knoten, zu dem dieses Chart gehört. |
| `chart.Difficulty  (enum)` | Der Schwierigkeitsgrad als Enum-Objekt. |
| `chart.DifficultyAsInt  (int)` | Der Schwierigkeitsindex. |
| `chart.Level  (int)` | Das Sternelevel. |
| `chart.LevelDecimal  (number)` | Das Level mit seinem Nachkommaanteil (zum Beispiel 12.888), oder das ganzzahlige Level, wenn keiner angegeben wurde. |
| `chart.LevelFirstDecimal  (int)` | Die erste Nachkommastelle von LevelDecimal (0-9). |
| `chart.LevelIcon  (enum)` | Das Level-Symbol als Enum-Objekt. |
| `chart.IsPlus  (bool)` | True, wenn das Level-Symbol "plus" ist. |
| `chart.IsMinus  (bool)` | True, wenn das Level-Symbol "minus" ist. |
| `chart.NotesDesigner  (string)` | Das Feld NOTESDESIGNER als ein String. |
| `chart.Charters  (string array)` | Das Feld NOTESDESIGNER, an Kommas geteilt. |
| `chart.BPM  (number)` | Die Haupt-BPM, oder nil. |
| `chart.BaseBPM  (number)` | Die Basis-BPM, oder nil. |
| `chart.MinBPM  (number)` | Die minimale BPM, oder nil. |
| `chart.MaxBPM  (number)` | Die maximale BPM, oder nil. |
| `chart.Life  (int)` | Anzahl der Tower-Leben, oder nil. |
| `chart.TotalFloorCount  (int)` | Anzahl der Tower-Stockwerke, oder nil. |
| `chart.TowerType  (string)` | Tower-Typ-String, oder nil. |
| `chart.DanTick  (int)` | Tick-Wert des Dan-Schilds, oder nil. |
| `chart.DanTickColor  (color)` | Tick-Farbe des Dan-Schilds (weiß, wenn das Chart keine Chart-Info hat). |
| `chart.DanSongs  (array of dan songs)` | Die Songs, aus denen dieses Dan-Chart besteht. |
| `chart.DanExams  (array of dan exams)` | Die globalen Prüfungsbedingungen dieses Dan-Charts. |
| `chart:GetSongExam(songIdx, examSlot)  -> dan exam` | Gibt die Prüfung pro Song für den 1-basierten Songindex und den 1-basierten Prüfungsslot zurück; das Ergebnis hat IsSet = false, wenn keine existiert. |
| `chart:GetPlayerBestScore(save)  -> best score info` | Gibt die Zusammenfassung der Bestleistung des angegebenen Spielstands für dieses Chart zurück. |
| `chart:GetCustomCommand(key)  -> string` | Gibt den Wert eines benutzerdefinierten Befehls auf Chart-Ebene zurück (ein mit Punkt beginnender Header innerhalb dieses COURSE-Blocks; key enthält den Punkt), oder nil. |
| `chart:GetCustomCommands()  -> dictionary` | Gibt alle benutzerdefinierten Befehle auf Chart-Ebene als C#-Dictionary-Objekt zurück. |
| `chart.SongFolder  (string)` | Der absolute Ordner, der die Chart-Datei enthält. |
| `chart.ChartPath  (string)` | Der absolute Pfad der Chart-Datei. |
| `chart.UniqueId  (string)` | Die eindeutige ID des Songs, oder ein leerer String. |
| `chart:Select(player)  -> bool` | Markiert dieses Chart als gewählten Schwierigkeitsgrad für den angegebenen Spieler; Spieler 0 setzt auch den gewählten Song. Gibt false zurück, wenn das Chart nicht gültig ist. |

### Bestleistungs-Info

Die Zusammenfassung der Bestleistung eines Spielstands für ein Chart.

<div class="callout warn">
chart:GetPlayerBestScore(save) gibt dieses Objekt zurück. Alle Member sind schreibgeschützt. Ein ungültiger Spielstandindex ergibt einen leeren Datensatz.
</div>

| Methode | Beschreibung |
| --- | --- |
| `info.ScoreRank  (int)` | Der beste erreichte Score-Rang. |
| `info.ClearStatus  (int)` | Der beste erreichte Clear-Status. |
| `info.HighScore  (int)` | Der Highscore. |
| `info.HasBeenPlayed  (bool)` | True, wenn das Chart mindestens ein aufgezeichnetes Spiel hat, unabhängig vom Ergebnis. |
| `info.PlayCount  (int)` | Gesamtzahl der Spiele auf diesem Chart, alle Mod-Varianten zusammen. |

## Dan-Prüfungen

### Dan-Song

Ein Songeintrag innerhalb eines Dan-Kurses.

<div class="callout warn">
Elemente von chart.DanSongs. Alle Member sind schreibgeschützt.
</div>

| Methode | Beschreibung |
| --- | --- |
| `dansong.Title  (string)` | Der Titel des Songs. |
| `dansong.SubTitle  (string)` | Der Untertitel des Songs. |
| `dansong.Genre  (string)` | Das Genre des Songs. |
| `dansong.Level  (int)` | Das Sternelevel des Songs. |
| `dansong.Difficulty  (enum)` | Der Schwierigkeitsgrad als Enum-Objekt. |
| `dansong.DifficultyAsInt  (int)` | Der Schwierigkeitsindex. |

### Dan-Prüfung

Eine Bestanden/Nicht-bestanden-Bedingung eines Dan-Kurses.

<div class="callout warn">
Elemente von chart.DanExams oder Rückgabe von chart:GetSongExam(). Alle Member sind schreibgeschützt. TypeAsInt-Werte: 0 Gauge, 1 Perfect-Wertungen, 2 Good-Wertungen, 3 Bad-Wertungen, 4 Score, 5 Trommelwirbel, 6 Treffer, 7 Combo, 8 Genauigkeit, 9 Ad-lib-Wertungen, 10 Minen-Wertungen. RangeAsInt-Werte: 0 "mindestens", 1 "weniger als".
</div>

| Methode | Beschreibung |
| --- | --- |
| `danexam.IsSet  (bool)` | True, wenn dieser Prüfungsslot aktiviert ist. |
| `danexam.RedValue  (int)` | Der rote (Bestanden-)Schwellenwert. |
| `danexam.GoldValue  (int)` | Der Gold-Schwellenwert. |
| `danexam.TypeAsInt  (int)` | Der Prüfungstyp. |
| `danexam.RangeAsInt  (int)` | Die Vergleichsrichtung. |

### DANBUILDER

Globales Objekt zum Zusammenstellen eines Dan-Kurses im Speicher aus Song-Knoten, Schwierigkeitsgraden und Prüfungsbedingungen, um ihn anschließend zum Spielen zu mounten.

<div class="callout warn">
Als globales Objekt DANBUILDER verfügbar. Song- und Slot-Indizes sind 1-basiert, mit Ausnahme des an AddSong übergebenen Schwierigkeitsgrads, der ein 0-basierter Schwierigkeitsindex ist. Prüfungsslots reichen von 1 bis 7. Prüfungstyp-Strings (Groß-/Kleinschreibung egal, Kurzform in Klammern): "judgeperfect" (jp), "judgegood" (jg), "judgebad" (jb), "score" (s), "roll" (r), "hit" (h), "combo" (c), "accuracy" (a), "judgeadlib" (ja), "judgemine" (jm); jeder andere String bedeutet Gauge. lessThan = true macht die Prüfung zu einer "weniger als"-Prüfung, false zu einer "mindestens"-Prüfung. Der Builder behält seinen Zustand zwischen Aufrufen; rufen Sie Clear() auf, bevor Sie einen neuen Kurs aufbauen.
</div>

| Methode | Beschreibung |
| --- | --- |
| `DANBUILDER.SongCount  (int)` | Anzahl der bisher hinzugefügten Songs. |
| `DANBUILDER:AddSong(node, diff)  -> void` | Hängt einen Song-Knoten mit dem angegebenen 0-basierten Schwierigkeitsindex an. |
| `DANBUILDER:GetSong(i)  -> song node` | Gibt den Song-Knoten am 1-basierten Index i zurück, oder nil. |
| `DANBUILDER:GetSongDiff(i)  -> int` | Gibt den für den Song am 1-basierten Index i gespeicherten Schwierigkeitsindex zurück, oder -1. |
| `DANBUILDER:SetTitle(title)  -> void` | Setzt den Kurstitel (Standard "Dynamic Dan"). |
| `DANBUILDER:SetSubtitle(subtitle)  -> void` | Setzt den Kursuntertitel. |
| `DANBUILDER:SetDanTick(tick)  -> void` | Setzt den Tick-Wert des Dan-Schilds (Standard 2). |
| `DANBUILDER:SetDanTickColor(r, g, b)  -> void` | Setzt die Tick-Farbe des Dan-Schilds aus Komponenten 0-255 (Standard weiß). |
| `DANBUILDER:SetGlobalExam(slot, type, red, gold, lessThan)  -> void` | Setzt eine kursweite Prüfung im angegebenen Slot. |
| `DANBUILDER:SetPerSongExam(songIndex, slot, type, red, gold, lessThan)  -> void` | Setzt eine Prüfung, die für einen Song gilt, per 1-basiertem Songindex und Slot. |
| `DANBUILDER:Clear()  -> void` | Entfernt alle Songs und Prüfungen und setzt die Metadaten auf die Standardwerte zurück. |
| `DANBUILDER:Mount()  -> bool` | Baut das Kurs-Chart im Speicher auf und wählt es zum Spielen mit dem Schwierigkeitsgrad Dan für Spieler 1 aus; gibt false zurück, wenn der Builder keine Songs enthält oder der Aufbau fehlgeschlagen ist. |

```lua
DANBUILDER:Clear()
DANBUILDER:SetTitle("Custom course")
DANBUILDER:AddSong(list:GetSongByUniqueId(id1), 3)
DANBUILDER:AddSong(list:GetSongByUniqueId(id2), 3)
DANBUILDER:SetGlobalExam(1, "gauge", 90, 100, false)
DANBUILDER:SetPerSongExam(2, 2, "judgebad", 10, 5, true)
if DANBUILDER:Mount() then
    return Exit("play")
end
```

## Freischaltungen, virtuelle Slots und Mod-Symbole

### Freischaltbedingung

Beschreibt, was einen Song freischaltet und ob ein Spieler die Bedingung gerade erfüllt.

<div class="callout warn">
node.UnlockCondition gibt dieses Objekt zurück. HasCondition ist eine Eigenschaft; der Rest sind Methoden. Songs ohne Freischalteintrag melden HasCondition = false und IsUnlockable = true.
</div>

| Methode | Beschreibung |
| --- | --- |
| `cond.HasCondition  (bool)` | True, wenn der Song eine explizite Freischaltbedingung hat. |
| `cond:GetConditionMessage()  -> string` | Gibt die lesbare Beschreibung der Bedingung zurück, oder einen leeren String. |
| `cond:GetConditionType()  -> string` | Gibt die Bedingungstyp-ID zurück (zum Beispiel "ch", "cs", "gt", "gc", "ig"), oder einen leeren String. |
| `cond:GetCoinPrice()  -> int` | Gibt die Münzkosten zurück, oder 0. |
| `cond:IsUnlockable(player)  -> bool` | Gibt true zurück, wenn der angegebene Spieler die Bedingung erfüllt. |
| `cond:GetBlockedMessage(player)  -> string` | Gibt zurück, warum der Spieler die Bedingung nicht erfüllt, oder einen leeren String, wenn er sie erfüllt. |

### VIRTUALSLOTS

Globales Objekt zum Lesen und Schreiben der fünf virtuellen Charakter-Slots (V1-V5) und zum Umleiten eines Spielerplatzes, damit er die Darstellung eines Slots zeigt.

<div class="callout warn">
Als globales Objekt VIRTUALSLOTS verfügbar. Slot-Indizes reichen von 1 bis 5; Setter ignorieren Indizes außerhalb des Bereichs, und Getter geben für sie die Standardwerte zurück. Diese Methoden schreiben nichts auf die Festplatte. Die Engine verwaltet den KI-Slot, den dieses globale Objekt nicht bearbeiten kann.
</div>

| Methode | Beschreibung |
| --- | --- |
| `VIRTUALSLOTS:GetCharacter(slot)  -> string` | Gibt den Charakter-Ordnernamen des Slots zurück, oder "None". |
| `VIRTUALSLOTS:SetCharacter(slot, folderName)  -> void` | Setzt den Charakter-Ordnernamen des Slots. |
| `VIRTUALSLOTS:GetPuchichara(slot)  -> string` | Gibt den Puchichara-Ordnernamen des Slots zurück, oder "None". |
| `VIRTUALSLOTS:SetPuchichara(slot, folderName)  -> void` | Setzt den Puchichara-Ordnernamen des Slots. |
| `VIRTUALSLOTS:GetNameplateName(slot)  -> string` | Gibt den Spielernamen des Namensschilds des Slots zurück, oder "VSlot". |
| `VIRTUALSLOTS:SetNameplateName(slot, name)  -> void` | Setzt den Spielernamen des Namensschilds des Slots. |
| `VIRTUALSLOTS:GetNameplateTitle(slot)  -> string` | Gibt den Titeltext des Namensschilds des Slots zurück. |
| `VIRTUALSLOTS:SetNameplateTitle(slot, title)  -> void` | Setzt den Titeltext des Namensschilds des Slots. |
| `VIRTUALSLOTS:GetNameplateDan(slot)  -> string` | Gibt den Dan-Text des Namensschilds des Slots zurück. |
| `VIRTUALSLOTS:SetNameplateDan(slot, dan)  -> void` | Setzt den Dan-Text des Namensschilds des Slots. |
| `VIRTUALSLOTS:SetNameplateById(slot, nameplateId)  -> void` | Wendet ein Namensschild aus der Namensschild-Datenbank per ID an: setzt Titeltext, Typ und Seltenheit. Eine unbekannte ID zeichnet nur die ID auf. |
| `VIRTUALSLOTS:SetNameplateType(slot, type)  -> void` | Setzt den Titeltyp des Namensschilds (Stilindex) direkt. |
| `VIRTUALSLOTS:SetNameplateRarity(slot, rarity)  -> void` | Setzt den Seltenheitsindex des Namensschildtitels direkt. |
| `VIRTUALSLOTS:SetNameplateDanType(slot, danType)  -> void` | Setzt den Typ des Dan-Schilds. |
| `VIRTUALSLOTS:SetNameplateDanGold(slot, gold)  -> void` | Setzt, ob das Dan-Schild golden erscheint. |
| `VIRTUALSLOTS:MountSlot(playerSpot, slotInfo)  -> void` | Lässt den Spielerplatz 1-5 die Darstellung von `slotInfo` zeigen: "1P"-"5P" (der Spielstand eines Spielers), "AI" oder "V1"-"V5". Die Überschreibung gilt bis zum nächsten MountSlot-Aufruf für diesen Platz. |

### MODICONS

Globales Objekt zum Zeichnen der aktiven Mod-Symbole eines Spielers an einer Bildschirmposition.

<div class="callout warn">
Als globales Objekt MODICONS verfügbar. Die ROActivity modicons übernimmt das Zeichnen; der erste Draw-Aufruf aktiviert sie.
</div>

| Methode | Beschreibung |
| --- | --- |
| `MODICONS:Draw(player, x, y, alpha)  -> void` | Zeichnet die Mod-Symbole des angegebenen Spielers bei (x, y) im Menü-Layout; alpha ist optional (Standard 255). |

## Replays und der ausgewählte Song

### REPLAY

Globales Objekt zum Auflisten der gespeicherten Replays eines Charts und zum Starten der Wiedergabe eines davon.

<div class="callout warn">
Als globales Objekt REPLAY verfügbar. ListReplays gibt ein C#-Array von Replay-Headern zurück (.Length, 0-basiert). Watch lädt ein Replay und bereitet die Wiedergabe nur für das nächste Spiel vor; das Spiel wendet die Mods des Replays im Speicher an und stellt danach die vorherigen Mods wieder her. songFolder und chartPath stammen aus SongFolder und ChartPath eines Charts.
</div>

| Methode | Beschreibung |
| --- | --- |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN, chartPath)  -> array of replay headers` | Gibt bis zu topN Replays des Charts und Schwierigkeitsgrads zurück, nach Score sortiert. chartPath lässt die Auflistung ChecksumMismatch berechnen. |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN)  -> array of replay headers` | Dasselbe ohne Chart-Pfad (die Auflistung überspringt ChecksumMismatch). |
| `REPLAY:ListReplaysAsync(songFolder, uniqueId, difficulty, topN, chartPath)  -> replay list handle` | Führt dieselbe Auflistung auf einem Hintergrund-Thread aus und gibt ein Handle zum Abfragen zurück. |
| `REPLAY:Watch(filepath, chartPath)  -> bool` | Lädt die Replay-Datei und bereitet die Wiedergabe für das nächste Spiel vor; gibt false zurück, wenn das Laden der Datei fehlschlägt oder das Replay nicht ansehbar ist. chartPath aktiviert die Warnungen "ungültiges Replay" im Spiel. |
| `REPLAY:Watch(filepath)  -> bool` | Dasselbe ohne Chart-Pfad. |
| `REPLAY.MODFLAG  (object)` | Bitwerte für ModFlags: None (0), Mirror (1), Random (2), SuperRandom (4), Invisible (8), PerfectMemory (16), Avalanche (32), Minesweeper (64), Just (128), Safe (256), DynamicBeat (512). |

```lua
local flags = header.ModFlags
local mirrored = (flags & REPLAY.MODFLAG.Mirror) ~= 0
```

### Replay-Listen-Handle

Handle, das von REPLAY:ListReplaysAsync zurückgegeben wird.

<div class="callout warn">
Fragen Sie IsDone jeden Frame ab; sobald es true ist, lesen Sie Result.
</div>

| Methode | Beschreibung |
| --- | --- |
| `handle.IsDone  (bool)` | True, sobald die Auflistung im Hintergrund abgeschlossen ist. |
| `handle.Result  (array of replay headers)` | Die aufgelisteten Replays (leer, bis IsDone true ist). |

### Replay-Header

Metadaten eines gespeicherten Replays.

<div class="callout warn">
Elemente des Arrays, das REPLAY:ListReplays oder das Result eines Replay-Listen-Handles zurückgibt. Alle Member sind schreibgeschützt.
</div>

| Methode | Beschreibung |
| --- | --- |
| `rep.FilePath  (string)` | Der absolute Pfad der Replay-Datei; übergeben Sie ihn an REPLAY:Watch. |
| `rep.PlayerName  (string)` | Der Name des Spielers, der das Replay aufgezeichnet hat. |
| `rep.Score  (int)` | Der Endscore. |
| `rep.ClearStatus  (int)` | Der Clear-Status des Spiels. |
| `rep.ScoreRank  (int)` | Der Score-Rang des Spiels. |
| `rep.Good  (int)` | Anzahl der Good-Wertungen (Perfect). |
| `rep.Ok  (int)` | Anzahl der Ok-Wertungen. |
| `rep.Bad  (int)` | Anzahl der Bad-Wertungen (Miss). |
| `rep.Roll  (int)` | Anzahl der Trommelwirbel-Treffer. |
| `rep.MaxCombo  (int)` | Maximale Combo. |
| `rep.Boom  (int)` | Anzahl der getroffenen Minen. |
| `rep.ADLib  (int)` | Anzahl der Ad-lib-Treffer. |
| `rep.ModFlags  (int)` | Bitmaske der verwendeten Mods (siehe REPLAY.MODFLAG). |
| `rep.ScrollSpeed  (int)` | Die Scrollgeschwindigkeits-Einstellung des Spiels. |
| `rep.SongSpeed  (int)` | Die Songgeschwindigkeits-Einstellung des Spiels. |
| `rep.JudgeStrictness  (int)` | Die Trefferfenster-Einstellung des Spiels. |
| `rep.Date  (string)` | Das Spieldatum, formatiert als "yyyy-MM-dd HH:mm". |
| `rep.Timestamp  (int)` | Das Spieldatum als rohe Ticks. |
| `rep.ChartUniqueID  (string)` | Die eindeutige ID des Charts. |
| `rep.ChartDifficulty  (int)` | Der Schwierigkeitsindex des aufgezeichneten Spiels. |
| `rep.ChartChecksum  (string)` | Die mit dem Replay gespeicherte Chart-MD5. |
| `rep.RandomSeed  (int)` | Der Seed der Notenmischung, oder -1, wenn die Datei keinen speichert. |
| `rep.GameMode  (int)` | Der Spielmodus des aufgezeichneten Spiels. |
| `rep.GameVersion  (int)` | Die Spielversion, die das Replay aufgezeichnet hat. |
| `rep.Watchable  (bool)` | True, wenn das Spiel das Replay originalgetreu wiedergeben kann. |
| `rep.UnwatchableReason  (string)` | Warum das Replay nicht ansehbar ist, wenn Watchable false ist. |
| `rep.OldVersion  (bool)` | True, wenn eine ältere Spielversion das Replay aufgezeichnet hat. |
| `rep.ChecksumMismatch  (bool)` | True, wenn die Chart-Datei nicht mehr zur Aufzeichnung passt (nur berechnet, wenn Sie einen Chart-Pfad übergeben). |

### SONGMOUNT

Schreibgeschütztes globales Objekt für den aktuell zum Spielen ausgewählten Song.

<div class="callout warn">
Als globales Objekt SONGMOUNT verfügbar. Es spiegelt den Zustand wider, der durch Mount() eines Song-Knotens, Select() eines Charts oder DANBUILDER:Mount() gesetzt wurde.
</div>

| Methode | Beschreibung |
| --- | --- |
| `SONGMOUNT:ChosenUniqueId()  -> string` | Gibt die eindeutige ID des ausgewählten Songs zurück, oder einen leeren String. |
| `SONGMOUNT:ChosenDifficulty()  -> int` | Gibt den für Spieler 1 ausgewählten Schwierigkeitsindex zurück. |
| `SONGMOUNT:ChosenSongNode()  -> song node` | Gibt den ausgewählten Song als Song-Knoten zurück (ohne Kinder), oder nil, wenn nichts ausgewählt ist. |
