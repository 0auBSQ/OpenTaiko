<!-- api/songs.md -->

# Nummers en charts

De nummerlijst opvragen, knopen en charts doorlopen, scores lezen, en dan-cursussen (examens) bouwen.

Conventies op deze pagina:

- Moeilijkheidsindices beginnen bij 0: 0 Easy, 1 Normal, 2 Hard, 3 Extreme (`Oni`), 4 Extra Extreme (`Edit`), 5 Tower, 6 Dan.
- Speler- en opslagbestandindices beginnen bij 0 (0 is speler 1).
- Leden geschreven met een punt (`node.Title`) zijn eigenschappen; leden geschreven met een dubbele punt (`node:GetChart(3)`) zijn methoden.
- Sommige leden geven C#-collecties terug. Een lijst heeft `.Count` en begint bij index 0 (`list[0]`); een array heeft `.Length` en begint eveneens bij index 0. Elk item hieronder vermeldt welke van de twee het teruggeeft.
- De nummerlijst is pas compleet nadat de nummerenumeratie is afgerond. Vraag hem op vanuit de callback `afterSongEnum()` (zie [Modules en levenscyclus](activities.md)), of controleer eerst de global `IsSongsEnumDone()`; die geeft true terug zodra de enumeratie is voltooid.

## De nummerlijst opvragen

### RequestSongList

Globale functie die een navigeerbare nummerlijst bouwt uit een instellingenobject.

<div class="callout warn">
Beschikbaar als een gewone globale functie. Geef er een instellingenobject aan door dat met GenerateSongListSettings() is gemaakt. De aanroep bouwt de nummerboom eenmalig, uit de nummers die het spel heeft geënumereerd; de handle bewaart het instellingenobject via referentie, dus je kunt een veld wijzigen en ReloadSongList() van de handle aanroepen om opnieuw te bouwen.
</div>

| Methode | Beschrijving |
| --- | --- |
| `RequestSongList(settings)  -> song list handle` | Bouwt en geeft een nummerlijst-handle terug uit de gegeven nummerlijstinstellingen. |

```lua
local settings = GenerateSongListSettings()
settings.AppendMainRandomBox = false
settings:SetExcludedGenreFolders({ "Dan", "Tower" })

local list = RequestSongList(settings)
local node = list:GetSelectedSongNode()
```

### GenerateSongListSettings

Globale functie die een nummerlijstinstellingenobject met standaardwaarden maakt.

| Methode | Beschrijving |
| --- | --- |
| `GenerateSongListSettings()  -> song list settings` | Geeft een nieuw nummerlijstinstellingenobject met standaardveldwaarden terug. |

### Nummerlijstinstellingen

Configuratieobject dat bepaalt welke knopen een nummerlijst bevat en hoe de navigatie zich gedraagt.

<div class="callout warn">
Alle onderstaande leden zijn publieke velden die Lua rechtstreeks leest en schrijft (settings.HideEmptyFolders = false), behalve de twee settermethoden, die een Lua-tabel nemen. ExcludedGenreFolders en MandatoryDifficultyList zijn C#-arrays; stel ze in via hun settermethoden.
</div>

| Methode | Beschrijving |
| --- | --- |
| `settings.AppendMainRandomBox  (bool, default true)` | Indien true voegt de lijst een randombox aan zijn root toe. |
| `settings.AppendSubRandomBoxes  (bool, default true)` | Indien true voegt de lijst aan het einde van elke map een randombox toe. |
| `settings.SubBackBoxFrequency  (int, default 7)` | Binnen elke map voegt de lijst aan het begin en na elke N items een terugbox in; 0 schakelt gegenereerde terugboxen uit. |
| `settings.ExcludedGenreFolders  (string array)` | Namen van genremappen die de lijst weglaat. Stel het in met SetExcludedGenreFolders. |
| `settings.RootGenreFolder  (string, default nil)` | Indien gezet wordt de root van de lijst de eerste map (diepte-eerst) waarvan het genre met deze naam overeenkomt; indien nil is de root het hoogste niveau. |
| `settings.RootGenreFolderNode  (song node, default nil)` | Knoopvorm van RootGenreFolder. Indien gezet heeft hij voorrang op de string, wat mappen met dezelfde genrenaam onderscheidt. |
| `settings.MandatoryDifficultyList  (Difficulty array, default nil)` | Moeilijkheden die een nummer moet hebben om in de lijst te verschijnen; nil betekent geen vereiste. Stel het in met SetMandatoryDifficultyList. |
| `settings.MandatoryDifficultyMatchAll  (bool, default true)` | true vereist elke vermelde moeilijkheid (AND); false vereist er minstens één (OR). |
| `settings.HideEmptyFolders  (bool, default true)` | Verbergt mappen die geen zichtbaar nummer bevatten, recursief. |
| `settings.FlattenOpenedFolders  (bool, default true)` | Indien true is de huidige pagina de hele boom met geopende mappen ter plekke uitgevouwen (gesloten mappen tellen als enkele items). Indien false bevat de pagina alleen de broers en zussen van de cursorknoop. |
| `settings.ModuloPagination  (bool, default true)` | Indien true loopt GetSongNodeAtOffset rond over de pagina; indien false geeft het nil terug voorbij een van beide uiteinden. |
| `settings.ModuloMovement  (bool, default true)` | Indien true loopt Move rond over de pagina; indien false stopt het aan een van beide uiteinden. |
| `settings.ExcludeHiddenSongs  (bool, default true)` | Sluit nummers uit waarvan HiddenIndex 3 is (verborgen). |
| `settings.ExcludeLockedSongs  (bool, default false)` | Indien true laten pagina's vergrendelde nummers weg, zodat de navigatie er nooit op belandt. |
| `settings.IgnoreUnlockables  (bool, default false)` | Indien true negeert de lijst ExcludeLockedSongs en kan GetRandomNodeInFolder vergrendelde nummers teruggeven. Knoopeigenschappen zoals IsLocked blijven de werkelijke status rapporteren. |
| `settings:SetExcludedGenreFolders(table)  -> void` | Stelt ExcludedGenreFolders in vanuit een Lua-tabel van genrenaamstrings. |
| `settings:SetMandatoryDifficultyList(table)  -> void` | Stelt MandatoryDifficultyList in vanuit een Lua-tabel van moeilijkheidsindices. |

### Nummerlijst-handle

Een navigeerbare nummerboom, teruggegeven door RequestSongList, met een cursor, mapnavigatie en zoeken.

<div class="callout warn">
Zoekmethoden nemen een Lua-functie die een nummerknoop ontvangt en een boolean teruggeeft. Methoden die meerdere knopen teruggeven, geven een C#-lijst terug (.Count, indexering vanaf 0).
</div>

| Methode | Beschrijving |
| --- | --- |
| `list:ReloadSongList()  -> void` | Bouwt de hele boom opnieuw op uit de huidige nummers en instellingen en zet de cursor op de eerste knoop. |
| `list:GetRoot()  -> song node` | Geeft de rootknoop van de boom terug. |
| `list:GetSelectedSongNode()  -> song node` | Geeft de knoop onder de cursor terug, of nil wanneer de lijst leeg is. |
| `list:GetSongNodeAtOffset(offset)  -> song node` | Geeft de knoop op de gegeven offset ten opzichte van de cursor binnen de huidige pagina terug, rondlopend of nil teruggevend volgens ModuloPagination. |
| `list:Move(offset)  -> void` | Verplaatst de cursor met de gegeven offset binnen de huidige pagina, rondlopend of begrensd volgens ModuloMovement. |
| `list:OpenFolder()  -> bool` | Opent de map onder de cursor en zet de cursor op het eerste kind; geeft false terug als de cursor niet op een gesloten, niet-lege map staat. |
| `list:CloseFolder()  -> bool` | Sluit de map die de cursor bevat en zet de cursor op die map; geeft false terug als er niets te sluiten valt. Bij het verlaten van een virtuele map wordt de door OpenVirtualFolder bewaarde cursor hersteld. |
| `list:OpenVirtualFolder(baseFolder, songs, title)  -> bool` | Opent een tijdelijke map met de naam `title` die de nummerknopen uit de Lua-tabel `songs` (sleutels 1..n) bevat, met gegenereerde terugboxen en een randombox aan het einde, en zet de cursor erin. `baseFolder` wordt de bovenliggende map van de virtuele map. Geeft false terug als de tabel geen nummerknopen bevat. |
| `list:GetSongByUniqueId(id)  -> song node` | Geeft het eerste nummer terug waarvan het unieke id overeenkomt, of nil. |
| `list:GetRandomNodeInFolder(node, recursive, predicate)  -> song node` | Kiest een willekeurig nummer onder de broers en zussen van `node` (de pagina die het bevat). Met `recursive` (standaard true) omvat de keuze ook nummers binnen naburige mappen. Het slaat vergrendelde nummers over tenzij IgnoreUnlockables is gezet. `predicate` is optioneel. Geeft nil terug wanneer niets in aanmerking komt. |
| `list:SearchSongsByPredicate(predicate)  -> list of song nodes` | Geeft elke nummerknoop in de boom terug waarvoor het predicaat true teruggeeft. |
| `list:SearchFirstSongByPredicate(predicate)  -> song node` | Geeft de eerste nummerknoop terug waarvoor het predicaat true teruggeeft, of nil. |
| `list:SearchNodesByPredicate(predicate)  -> list of song nodes` | Zoals SearchSongsByPredicate, maar test ook mappen en andere niet-nummerknopen. |

```lua
local results = list:SearchSongsByPredicate(function(node)
    return node:GetChart(3) ~= nil   -- heeft een Extreme-chart
end)
for i = 0, results.Count - 1 do
    local node = results[i]
end
```

## Knopen, charts en scores

### Nummerknoop

Eén item in de nummerlijst: een nummer, een map, een terugbox of een randombox.

<div class="callout warn">
De nummerlijst-handle, SONGMOUNT:ChosenSongNode() en DANBUILDER:GetSong() geven nummerknopen terug. Eigenschappen zijn alleen-lezen. Metadata-eigenschappen geven nil terug op knopen die geen nummers zijn. Navigeer door de lijst via de nummerlijst-handle (Move, OpenFolder, CloseFolder en de zoekmethoden).
</div>

| Methode | Beschrijving |
| --- | --- |
| `node.NotNull  (bool)` | True wanneer de knoop een echt nummerlijstitem omhult. |
| `node.IsFolder  (bool)` | True wanneer de knoop een map is. |
| `node.IsRandom  (bool)` | True wanneer de knoop een randombox is. |
| `node.IsReturn  (bool)` | True wanneer de knoop een terugbox is. |
| `node.IsSong  (bool)` | True wanneer de knoop een speelbaar nummer is. |
| `node.SongCount  (int)` | Aantal directe kindnummers. |
| `node.RecursiveSongCount  (int)` | Aantal nummers onder deze knoop, inclusief submappen. |
| `node.VisibleSongCount  (int)` | Aantal directe kindnummers waarvan HiddenIndex niet 3 is. |
| `node.RecursiveVisibleSongCount  (int)` | Aantal zichtbare nummers onder deze knoop, inclusief submappen. |
| `node.BoxType  (string)` | De stijlstring van de mapbox, of nil. |
| `node.BgType  (string)` | De stijlstring van de achtergrond, of nil. |
| `node.BoxChara  (string)` | De boxpersonagestring, of nil. |
| `node.ForeColor  (color)` | De voorgrondkleur van de knoop, of nil. |
| `node.BackColor  (color)` | De achtergrondkleur van de knoop, of nil. |
| `node.BoxColor  (color)` | De boxkleur van de knoop, of nil. |
| `node.Title  (string)` | De weergavetitel. Terug- en randomboxen geven de gelokaliseerde tekst "Return" / "Random" terug, opgebouwd uit de titel van de bovenliggende map. |
| `node.Subtitle  (string)` | De ondertitel van het nummer, of nil. |
| `node.Genre  (string)` | De genrestring, of nil. |
| `node.UniqueId  (string)` | Het unieke id van het nummer, of nil. |
| `node.Maker  (string)` | Het veld MAKER als één string, of nil. |
| `node.Charters  (string array)` | Het veld MAKER gesplitst op komma's. |
| `node.Side  (int)` | De SIDE-waarde: 0 normaal, 1 ex, 2 beide. |
| `node.Explicit  (bool)` | True wanneer het nummer als expliciet is gemarkeerd; nil voor niet-nummers. |
| `node.HasVideo  (bool)` | True wanneer het nummer een achtergrondfilm heeft; nil voor niet-nummers. |
| `node.DemoStart  (int)` | De offset van de preview-BGM in milliseconden. |
| `node.AudioPath  (string)` | Het absolute pad van het BGM-bestand van het nummer, of een lege string. |
| `node.HasPreimage  (bool)` | True wanneer het nummer een preimage declareert. |
| `node.PreimagePath  (string)` | Het absolute pad van de preimage. Controleer eerst HasPreimage; zonder preimage is dit alleen de nummermap. |
| `node:GetPreimage()  -> texture` | Laadt de preimage van schijf en geeft een nieuwe textuur terug, of nil als het nummer er geen heeft. Geef de textuur vrij wanneer je klaar bent. |
| `node.ChartMd5  (string)` | MD5 van het chartbestand (hex in hoofdletters), of een lege string. Hij blijft hetzelfde over installaties heen; UniqueId niet. |
| `node:GetChart(diff)  -> chart` | Geeft de chart voor de gegeven moeilijkheidsindex terug, of nil als het nummer die chart niet heeft. |
| `node:GetCustomCommand(key)  -> string` | Geeft de waarde van een aangepast commando op globaal niveau terug (een header met puntvoorvoegsel vóór de eerste COURSE; key bevat de punt, bijvoorbeeld ".VAULT_NAME"), of nil. |
| `node:GetCustomCommands()  -> dictionary` | Geeft alle aangepaste commando's op globaal niveau terug als C#-dictionary-object. Gebruik bij voorkeur GetCustomCommand voor opzoekingen. |
| `node.UnlockCondition  (unlock condition)` | Het ontgrendelvoorwaarde-object (zie Ontgrendelvoorwaarde). |
| `node.UnlockText  (string)` | De aangepaste ontgrendeltekst als het nummer die definieert, anders het gegenereerde voorwaardebericht. |
| `node.IsLocked  (bool)` | True wanneer dit nummer momenteel vergrendeld is; altijd false voor niet-nummers. |
| `node.HiddenIndex  (int)` | De weergavestatus van het ontgrendelsysteem: 0 weergegeven, 1 vergrijsd, 2 vervaagd, 3 verborgen (0 voor niet-nummers). |
| `node.Rarity  (string)` | Het zeldzaamheidslabel; "Common" voor nummers zonder ontgrendelitem, "-" voor niet-nummers. |
| `node:Mount(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | Selecteert dit nummer om te spelen met de gegeven moeilijkheidsindex per speler (standaard 0). Het controleert alleen de eerste CONFIG.PlayerCount indices en geeft false terug als de knoop geen nummer is of de moeilijkheid van een actieve speler ontbreekt of buiten bereik is. |
| `node:MountIfNotLocked(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | Hetzelfde als Mount, maar geeft false terug zonder te mounten wanneer het nummer vergrendeld is. |

### Chart

Eén moeilijkheid van een nummer: niveau, BPM, charters, Tower- en Dan-data, beste scores en aangepaste commando's.

<div class="callout warn">
GetChart(diff) van een nummerknoop geeft een chart terug. Eigenschappen zijn alleen-lezen. BPM, Life, TotalFloorCount, TowerType en DanTick geven nil terug wanneer de chart geen chartinfo heeft. Difficulty en LevelIcon zijn enum-objecten; vergelijk ze via DifficultyAsInt, IsPlus en IsMinus.
</div>

| Methode | Beschrijving |
| --- | --- |
| `chart.NotNull  (bool)` | True wanneer de chart echte chartdata omhult. |
| `chart.Parent  (song node)` | De nummerknoop waartoe deze chart behoort. |
| `chart.Difficulty  (enum)` | De moeilijkheid als enum-object. |
| `chart.DifficultyAsInt  (int)` | De moeilijkheidsindex. |
| `chart.Level  (int)` | Het sterrenniveau. |
| `chart.LevelDecimal  (number)` | Het niveau met zijn decimale deel (bijvoorbeeld 12.888), of het gehele niveau wanneer er geen is opgegeven. |
| `chart.LevelFirstDecimal  (int)` | Het eerste decimale cijfer van LevelDecimal (0-9). |
| `chart.LevelIcon  (enum)` | Het niveaupictogram als enum-object. |
| `chart.IsPlus  (bool)` | True wanneer het niveaupictogram "plus" is. |
| `chart.IsMinus  (bool)` | True wanneer het niveaupictogram "minus" is. |
| `chart.NotesDesigner  (string)` | Het veld NOTESDESIGNER als één string. |
| `chart.Charters  (string array)` | Het veld NOTESDESIGNER gesplitst op komma's. |
| `chart.BPM  (number)` | De hoofd-BPM, of nil. |
| `chart.BaseBPM  (number)` | De basis-BPM, of nil. |
| `chart.MinBPM  (number)` | De minimale BPM, of nil. |
| `chart.MaxBPM  (number)` | De maximale BPM, of nil. |
| `chart.Life  (int)` | Aantal Tower-levens, of nil. |
| `chart.TotalFloorCount  (int)` | Aantal Tower-verdiepingen, of nil. |
| `chart.TowerType  (string)` | Tower-typestring, of nil. |
| `chart.DanTick  (int)` | Tickwaarde van het dan-plaatje, of nil. |
| `chart.DanTickColor  (color)` | Tickkleur van het dan-plaatje (wit wanneer de chart geen chartinfo heeft). |
| `chart.DanSongs  (array of dan songs)` | De nummers waaruit deze Dan-chart bestaat. |
| `chart.DanExams  (array of dan exams)` | De globale examenvoorwaarden van deze Dan-chart. |
| `chart:GetSongExam(songIdx, examSlot)  -> dan exam` | Geeft het examen per nummer terug voor de 1-gebaseerde nummerindex en het 1-gebaseerde examenslot; het resultaat heeft IsSet = false wanneer er geen bestaat. |
| `chart:GetPlayerBestScore(save)  -> best score info` | Geeft de samenvatting van de beste spelbeurt van het gegeven opslagbestand voor deze chart terug. |
| `chart:GetCustomCommand(key)  -> string` | Geeft de waarde van een aangepast commando op chartniveau terug (een header met puntvoorvoegsel binnen dit COURSE-blok; key bevat de punt), of nil. |
| `chart:GetCustomCommands()  -> dictionary` | Geeft alle aangepaste commando's op chartniveau terug als C#-dictionary-object. |
| `chart.SongFolder  (string)` | De absolute map die het chartbestand bevat. |
| `chart.ChartPath  (string)` | Het absolute pad van het chartbestand. |
| `chart.UniqueId  (string)` | Het unieke id van het nummer, of een lege string. |
| `chart:Select(player)  -> bool` | Markeert deze chart als de gekozen moeilijkheid voor de gegeven speler; speler 0 stelt ook het gekozen nummer in. Geeft false terug wanneer de chart niet geldig is. |

### Beste-score-info

De samenvatting van de beste spelbeurt van een opslagbestand voor één chart.

<div class="callout warn">
chart:GetPlayerBestScore(save) geeft dit object terug. Alle leden zijn alleen-lezen. Een ongeldige opslagindex levert een leeg record op.
</div>

| Methode | Beschrijving |
| --- | --- |
| `info.ScoreRank  (int)` | De beste bereikte scorerang. |
| `info.ClearStatus  (int)` | De beste bereikte clearstatus. |
| `info.HighScore  (int)` | De hoogste score. |
| `info.HasBeenPlayed  (bool)` | True wanneer de chart minstens één geregistreerde spelbeurt heeft, ongeacht het resultaat. |
| `info.PlayCount  (int)` | Totaal aantal spelbeurten op deze chart, alle modvarianten samen. |

## Dan-examens

### Dan-nummer

Eén nummeritem binnen een Dan-cursus.

<div class="callout warn">
Elementen van chart.DanSongs. Alle leden zijn alleen-lezen.
</div>

| Methode | Beschrijving |
| --- | --- |
| `dansong.Title  (string)` | De titel van het nummer. |
| `dansong.SubTitle  (string)` | De ondertitel van het nummer. |
| `dansong.Genre  (string)` | Het genre van het nummer. |
| `dansong.Level  (int)` | Het sterrenniveau van het nummer. |
| `dansong.Difficulty  (enum)` | De moeilijkheid als enum-object. |
| `dansong.DifficultyAsInt  (int)` | De moeilijkheidsindex. |

### Dan-examen

Eén slaag-/zakvoorwaarde van een Dan-cursus.

<div class="callout warn">
Elementen van chart.DanExams, of teruggegeven door chart:GetSongExam(). Alle leden zijn alleen-lezen. TypeAsInt-waarden: 0 gauge, 1 perfect-beoordelingen, 2 good-beoordelingen, 3 bad-beoordelingen, 4 score, 5 roffels, 6 slagen, 7 combo, 8 nauwkeurigheid, 9 ad-lib-beoordelingen, 10 mijnbeoordelingen. RangeAsInt-waarden: 0 "minstens", 1 "minder dan".
</div>

| Methode | Beschrijving |
| --- | --- |
| `danexam.IsSet  (bool)` | True wanneer dit examenslot is ingeschakeld. |
| `danexam.RedValue  (int)` | De rode (slaag)drempel. |
| `danexam.GoldValue  (int)` | De gouden drempel. |
| `danexam.TypeAsInt  (int)` | Het examentype. |
| `danexam.RangeAsInt  (int)` | De vergelijkingsrichting. |

### DANBUILDER

Global om in het geheugen een Dan-cursus samen te stellen uit nummerknopen, moeilijkheden en examenvoorwaarden, en die vervolgens te mounten om te spelen.

<div class="callout warn">
Beschikbaar als de global DANBUILDER. Nummer- en slotindices beginnen bij 1, behalve de moeilijkheid die aan AddSong wordt doorgegeven, die een 0-gebaseerde moeilijkheidsindex is. Examenslots lopen van 1 tot 7. Examentypestrings (niet hoofdlettergevoelig, korte vorm tussen haakjes): "judgeperfect" (jp), "judgegood" (jg), "judgebad" (jb), "score" (s), "roll" (r), "hit" (h), "combo" (c), "accuracy" (a), "judgeadlib" (ja), "judgemine" (jm); elke andere string betekent gauge. lessThan = true maakt van het examen een "minder dan"-controle, false een "minstens"-controle. De builder behoudt zijn status tussen aanroepen; roep Clear() aan voordat je een nieuwe cursus bouwt.
</div>

| Methode | Beschrijving |
| --- | --- |
| `DANBUILDER.SongCount  (int)` | Aantal tot nu toe toegevoegde nummers. |
| `DANBUILDER:AddSong(node, diff)  -> void` | Voegt een nummerknoop toe op de gegeven 0-gebaseerde moeilijkheidsindex. |
| `DANBUILDER:GetSong(i)  -> song node` | Geeft de nummerknoop op 1-gebaseerde index i terug, of nil. |
| `DANBUILDER:GetSongDiff(i)  -> int` | Geeft de moeilijkheidsindex terug die is opgeslagen voor het nummer op 1-gebaseerde index i, of -1. |
| `DANBUILDER:SetTitle(title)  -> void` | Stelt de cursustitel in (standaard "Dynamic Dan"). |
| `DANBUILDER:SetSubtitle(subtitle)  -> void` | Stelt de cursusondertitel in. |
| `DANBUILDER:SetDanTick(tick)  -> void` | Stelt de tickwaarde van het dan-plaatje in (standaard 2). |
| `DANBUILDER:SetDanTickColor(r, g, b)  -> void` | Stelt de tickkleur van het dan-plaatje in vanuit componenten 0-255 (standaard wit). |
| `DANBUILDER:SetGlobalExam(slot, type, red, gold, lessThan)  -> void` | Stelt een cursusbreed examen in het gegeven slot in. |
| `DANBUILDER:SetPerSongExam(songIndex, slot, type, red, gold, lessThan)  -> void` | Stelt een examen in dat op één nummer van toepassing is, per 1-gebaseerde nummerindex en slot. |
| `DANBUILDER:Clear()  -> void` | Verwijdert alle nummers en examens en zet de metadata terug op de standaardwaarden. |
| `DANBUILDER:Mount()  -> bool` | Bouwt de cursuschart in het geheugen en selecteert die om te spelen op de Dan-moeilijkheid voor speler 1; geeft false terug als de builder geen nummers bevat of het bouwen is mislukt. |

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

## Ontgrendelingen, virtuele slots en modpictogrammen

### Ontgrendelvoorwaarde

Beschrijft wat een nummer ontgrendelt en of een speler momenteel aan de voorwaarde voldoet.

<div class="callout warn">
node.UnlockCondition geeft dit object terug. HasCondition is een eigenschap; de rest zijn methoden. Nummers zonder ontgrendelitem rapporteren HasCondition = false en IsUnlockable = true.
</div>

| Methode | Beschrijving |
| --- | --- |
| `cond.HasCondition  (bool)` | True wanneer het nummer een expliciete ontgrendelvoorwaarde heeft. |
| `cond:GetConditionMessage()  -> string` | Geeft de leesbare beschrijving van de voorwaarde terug, of een lege string. |
| `cond:GetConditionType()  -> string` | Geeft het id van het voorwaardetype terug (bijvoorbeeld "ch", "cs", "gt", "gc", "ig"), of een lege string. |
| `cond:GetCoinPrice()  -> int` | Geeft de muntprijs terug, of 0. |
| `cond:IsUnlockable(player)  -> bool` | Geeft true terug wanneer de gegeven speler aan de voorwaarde voldoet. |
| `cond:GetBlockedMessage(player)  -> string` | Geeft terug waarom de speler niet aan de voorwaarde voldoet, of een lege string wanneer de speler eraan voldoet. |

### VIRTUALSLOTS

Global om de vijf virtuele personageslots (V1-V5) te lezen en te schrijven en om een spelerplek om te leiden naar de visuals van een slot.

<div class="callout warn">
Beschikbaar als de global VIRTUALSLOTS. Slotindices lopen van 1 tot 5; setters negeren indices buiten bereik en getters geven daarvoor de standaardwaarden terug. Deze methoden schrijven niets naar schijf. De engine beheert het AI-slot, dat deze global niet kan bewerken.
</div>

| Methode | Beschrijving |
| --- | --- |
| `VIRTUALSLOTS:GetCharacter(slot)  -> string` | Geeft de mapnaam van het personage van het slot terug, of "None". |
| `VIRTUALSLOTS:SetCharacter(slot, folderName)  -> void` | Stelt de mapnaam van het personage van het slot in. |
| `VIRTUALSLOTS:GetPuchichara(slot)  -> string` | Geeft de mapnaam van de puchichara van het slot terug, of "None". |
| `VIRTUALSLOTS:SetPuchichara(slot, folderName)  -> void` | Stelt de mapnaam van de puchichara van het slot in. |
| `VIRTUALSLOTS:GetNameplateName(slot)  -> string` | Geeft de spelernaam op het naamplaatje van het slot terug, of "VSlot". |
| `VIRTUALSLOTS:SetNameplateName(slot, name)  -> void` | Stelt de spelernaam op het naamplaatje van het slot in. |
| `VIRTUALSLOTS:GetNameplateTitle(slot)  -> string` | Geeft de titeltekst op het naamplaatje van het slot terug. |
| `VIRTUALSLOTS:SetNameplateTitle(slot, title)  -> void` | Stelt de titeltekst op het naamplaatje van het slot in. |
| `VIRTUALSLOTS:GetNameplateDan(slot)  -> string` | Geeft de dan-tekst op het naamplaatje van het slot terug. |
| `VIRTUALSLOTS:SetNameplateDan(slot, dan)  -> void` | Stelt de dan-tekst op het naamplaatje van het slot in. |
| `VIRTUALSLOTS:SetNameplateById(slot, nameplateId)  -> void` | Past een naamplaatje uit de naamplaatjesdatabase toe op id: stelt de titeltekst, het type en de zeldzaamheid in. Een onbekend id registreert alleen het id. |
| `VIRTUALSLOTS:SetNameplateType(slot, type)  -> void` | Stelt het titeltype (stijlindex) van het naamplaatje rechtstreeks in. |
| `VIRTUALSLOTS:SetNameplateRarity(slot, rarity)  -> void` | Stelt de zeldzaamheidsindex van de naamplaatjestitel rechtstreeks in. |
| `VIRTUALSLOTS:SetNameplateDanType(slot, danType)  -> void` | Stelt het type van het dan-plaatje in. |
| `VIRTUALSLOTS:SetNameplateDanGold(slot, gold)  -> void` | Stelt in of het dan-plaatje goud verschijnt. |
| `VIRTUALSLOTS:MountSlot(playerSpot, slotInfo)  -> void` | Laat spelerplek 1-5 de visuals van `slotInfo` tonen: "1P"-"5P" (het opslagbestand van een speler), "AI", of "V1"-"V5". De overschrijving blijft van kracht tot de volgende MountSlot-aanroep voor die plek. |

### MODICONS

Global om de actieve modpictogrammen van een speler op een schermpositie te tekenen.

<div class="callout warn">
Beschikbaar als de global MODICONS. De ROActivity modicons doet het tekenen; de eerste Draw-aanroep activeert haar.
</div>

| Methode | Beschrijving |
| --- | --- |
| `MODICONS:Draw(player, x, y, alpha)  -> void` | Tekent de modpictogrammen van de gegeven speler op (x, y) met de menulay-out; alpha is optioneel (standaard 255). |

## Replays en het geselecteerde nummer

### REPLAY

Global om de opgeslagen replays van een chart op te sommen en er een af te spelen.

<div class="callout warn">
Beschikbaar als de global REPLAY. ListReplays geeft een C#-array van replay-headers terug (.Length, indexering vanaf 0). Watch laadt een replay en activeert het afspelen voor uitsluitend de volgende spelbeurt; het spel past de mods van de replay in het geheugen toe en herstelt daarna de vorige mods. songFolder en chartPath komen uit SongFolder en ChartPath van een chart.
</div>

| Methode | Beschrijving |
| --- | --- |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN, chartPath)  -> array of replay headers` | Geeft tot topN replays van de chart en moeilijkheid terug, gerangschikt op score. Met chartPath kan de lijst ChecksumMismatch berekenen. |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN)  -> array of replay headers` | Hetzelfde zonder chartpad (de lijst slaat ChecksumMismatch over). |
| `REPLAY:ListReplaysAsync(songFolder, uniqueId, difficulty, topN, chartPath)  -> replay list handle` | Voert dezelfde opsomming uit op een achtergrondthread en geeft een handle terug om te pollen. |
| `REPLAY:Watch(filepath, chartPath)  -> bool` | Laadt het replaybestand en activeert het afspelen voor de volgende spelbeurt; geeft false terug wanneer het laden van het bestand mislukt of de replay niet bekeken kan worden. chartPath schakelt de waarschuwingen "ongeldige replay" in het spel in. |
| `REPLAY:Watch(filepath)  -> bool` | Hetzelfde zonder chartpad. |
| `REPLAY.MODFLAG  (object)` | Bitwaarden voor ModFlags: None (0), Mirror (1), Random (2), SuperRandom (4), Invisible (8), PerfectMemory (16), Avalanche (32), Minesweeper (64), Just (128), Safe (256), DynamicBeat (512). |

```lua
local flags = header.ModFlags
local mirrored = (flags & REPLAY.MODFLAG.Mirror) ~= 0
```

### Replaylijst-handle

Handle teruggegeven door REPLAY:ListReplaysAsync.

<div class="callout warn">
Poll elk frame IsDone; zodra die true is, lees je Result.
</div>

| Methode | Beschrijving |
| --- | --- |
| `handle.IsDone  (bool)` | True zodra de opsomming op de achtergrond is afgerond. |
| `handle.Result  (array of replay headers)` | De opgesomde replays (leeg tot IsDone true is). |

### Replay-header

Metadata van één opgeslagen replay.

<div class="callout warn">
Elementen van de array die REPLAY:ListReplays teruggeeft of van de Result van een replaylijst-handle. Alle leden zijn alleen-lezen.
</div>

| Methode | Beschrijving |
| --- | --- |
| `rep.FilePath  (string)` | Het absolute pad van het replaybestand; geef het door aan REPLAY:Watch. |
| `rep.PlayerName  (string)` | De naam van de speler die de replay heeft opgenomen. |
| `rep.Score  (int)` | De eindscore. |
| `rep.ClearStatus  (int)` | De clearstatus van de spelbeurt. |
| `rep.ScoreRank  (int)` | De scorerang van de spelbeurt. |
| `rep.Good  (int)` | Aantal Good-beoordelingen (perfect). |
| `rep.Ok  (int)` | Aantal Ok-beoordelingen. |
| `rep.Bad  (int)` | Aantal Bad-beoordelingen (mis). |
| `rep.Roll  (int)` | Aantal roffelslagen. |
| `rep.MaxCombo  (int)` | Maximale combo. |
| `rep.Boom  (int)` | Aantal geraakte mijnen. |
| `rep.ADLib  (int)` | Aantal geraakte ad-libs. |
| `rep.ModFlags  (int)` | Bitmasker van de gebruikte mods (zie REPLAY.MODFLAG). |
| `rep.ScrollSpeed  (int)` | De scrollsnelheidsinstelling van de spelbeurt. |
| `rep.SongSpeed  (int)` | De nummersnelheidsinstelling van de spelbeurt. |
| `rep.JudgeStrictness  (int)` | De beoordelingsvensterinstelling van de spelbeurt. |
| `rep.Date  (string)` | De speeldatum, opgemaakt als "yyyy-MM-dd HH:mm". |
| `rep.Timestamp  (int)` | De speeldatum als ruwe ticks. |
| `rep.ChartUniqueID  (string)` | Het unieke id van de chart. |
| `rep.ChartDifficulty  (int)` | De moeilijkheidsindex van de opgenomen spelbeurt. |
| `rep.ChartChecksum  (string)` | De chart-MD5 die bij de replay is opgeslagen. |
| `rep.RandomSeed  (int)` | De seed voor het husselen van noten, of -1 wanneer het bestand er geen opslaat. |
| `rep.GameMode  (int)` | De spelmodus van de opgenomen spelbeurt. |
| `rep.GameVersion  (int)` | De spelversie die de replay heeft opgenomen. |
| `rep.Watchable  (bool)` | True wanneer het spel de replay getrouw kan afspelen. |
| `rep.UnwatchableReason  (string)` | Waarom de replay niet bekeken kan worden, wanneer Watchable false is. |
| `rep.OldVersion  (bool)` | True wanneer een oudere spelversie de replay heeft opgenomen. |
| `rep.ChecksumMismatch  (bool)` | True wanneer het chartbestand niet meer overeenkomt met de opname (alleen berekend wanneer je een chartpad doorgeeft). |

### SONGMOUNT

Alleen-lezen global voor het nummer dat momenteel is geselecteerd om te spelen.

<div class="callout warn">
Beschikbaar als de global SONGMOUNT. Hij weerspiegelt de status die is gezet door Mount() van een nummerknoop, Select() van een chart of DANBUILDER:Mount().
</div>

| Methode | Beschrijving |
| --- | --- |
| `SONGMOUNT:ChosenUniqueId()  -> string` | Geeft het unieke id van het geselecteerde nummer terug, of een lege string. |
| `SONGMOUNT:ChosenDifficulty()  -> int` | Geeft de moeilijkheidsindex terug die voor speler 1 is geselecteerd. |
| `SONGMOUNT:ChosenSongNode()  -> song node` | Geeft het geselecteerde nummer terug als nummerknoop (zonder kinderen), of nil wanneer er niets is geselecteerd. |
