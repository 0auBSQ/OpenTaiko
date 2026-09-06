<!-- api/data.md -->

# Data en persistentie

Data opslaan die een herstart overleeft, SQLite-bestanden bevragen, bestanden, JSON en INI uit de modulemap lezen, resources delen tussen modules, en de spelconfiguratie lezen of wijzigen.

Relatieve paden die aan DATABASE, SQL, STORAGE, JSONLOADER, INILOADER en SHARED worden doorgegeven, worden opgelost ten opzichte van de map van de draaiende module (de map die zijn `Script.lua` bevat).

Sommige methoden op deze pagina geven .NET-collecties terug, die zich anders gedragen dan Lua-tabellen:

- Arrays (`string[]`, `int[]`, `double[]`) beginnen bij index 0 en bieden `.Length`.
- Dictionaries (geparseerde JSON, SQL-rijen, taalmaps) nemen `d["key"]` (of `d[1]` voor arrays die uit JSON zijn geparseerd) en doorloop je met `d:GetEnumerator()`; `pairs` en `#` werken er niet op.

```lua
local files = STORAGE:GetFiles("maps", "*.json")
for i = 0, files.Length - 1 do
    print(files[i])
end

local e = dict:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end
```

## Key-value-database

### DATABASE

Opent LMDB-key-value-stores die stringwaarden over herstarts heen bewaren, ofwel binnen de modulemap ofwel in de spelbrede datamap.

<div class="callout warn">
Elke Read en Write opent en sluit een eigen LMDB-omgeving, dus elke aanroep is duur; cache waarden in Lua en vernieuw de cache wanneer je schrijft. Binnen alleen-lezen modules (ROActivities en achtergronden) geeft DATABASE stores terug waarvan Write een fout logt en niets doet.
</div>

| Methode | Beschrijving |
| --- | --- |
| `DATABASE:OpenLocalDatabase(path)  -> database` | Opent (en maakt zo nodig aan) een store op een pad relatief ten opzichte van de modulemap. |
| `DATABASE:OpenGlobalDatabase(path)  -> database` | Opent (en maakt zo nodig aan) een store onder `Global/ApplicationData/LMDB/` in de spelmap, gedeeld door elke module. |

### Database-handle

Een key-value-store, teruggegeven door DATABASE, die stringsleutels aan stringwaarden koppelt.

<div class="callout warn">
Waarden zijn uitsluitend strings; converteer getallen en booleans zelf (bijvoorbeeld met tostring en tonumber).
</div>

| Methode | Beschrijving |
| --- | --- |
| `database:Write(key, value)  -> nil` | Slaat een string op onder de sleutel en commit hem. Logt een fout en doet niets in alleen-lezen modules. |
| `database:Read(key)  -> string` | Geeft de string terug die onder de sleutel is opgeslagen, of nil als de sleutel ontbreekt of het lezen mislukt. |
| `database:Dispose()  -> nil` | Doet niets; de handle houdt geen open resources vast. |

```lua
local db

function activate()
    db = DATABASE:OpenGlobalDatabase("GameStatus")
    if db:Read("new_user") == nil then
        db:Write("new_user", "true")
    end
end
```

## SQL-database

### SQL

Opent een SQLite-databasebestand binnen de modulemap om SQL-instructies uit te voeren.

| Methode | Beschrijving |
| --- | --- |
| `SQL:OpenSQLDatabase(path)  -> sql` | Opent een SQLite-database op een pad relatief ten opzichte van de modulemap. |

### SQL-handle

Een SQLite-verbinding, teruggegeven door SQL:OpenSQLDatabase.

<div class="callout warn">
Query geeft een dictionary terug met sleutels 1..n; elke rij is een dictionary met kolomnamen als sleutels (zie de opmerking over .NET-collecties bovenaan de pagina). Een mislukte instructie logt de fout en geeft een leeg resultaat terug. Query geeft de instructietekst ongewijzigd door aan SQLite, zonder parameterbinding, dus escape elke waarde die je erin invoegt.
</div>

| Methode | Beschrijving |
| --- | --- |
| `sql:Query(query)  -> rows` | Voert de SQL-tekst uit en geeft de resultaatrijen terug. |

```lua
local db

function activate()
    db = SQL:OpenSQLDatabase("Databases/Items.db3")
    local rows = db:Query("SELECT * FROM itempool WHERE Slot = 'regular'")
    for i = 1, rows.Count do
        print(rows[i]["Code"])
    end
end
```

## Bestanden, JSON en INI

### STORAGE

Bestandstoegang met de modulemap als basis, plus helpers voor het delen van online lobbycodes.

<div class="callout warn">
WriteText schrijft alleen binnen de modulemap: het weigert absolute paden en paden die buiten de map uitkomen. ReadText accepteert ook een absoluut pad. Lobbycode-helpers gebruiken de gedeelde map `Global/Lobbycodes/` naast het uitvoerbare bestand. Alleen-lezen modules kunnen elke STORAGE-methode gebruiken.
</div>

| Methode | Beschrijving |
| --- | --- |
| `STORAGE:GetFiles(dir, pattern)  -> string[]` | Somt de bestanden op in een submap van de modulemap die overeenkomen met een zoekpatroon zoals `"*.png"`. De items zijn paden relatief ten opzichte van de modulemap, inclusief de submap. De map moet bestaan. |
| `STORAGE:FileExists(path)  -> bool` | True als er een bestand bestaat op het pad relatief ten opzichte van de modulemap. |
| `STORAGE:DirectoryExists(path)  -> bool` | True als er een map bestaat op het pad relatief ten opzichte van de modulemap. |
| `STORAGE:WriteText(name, contents)  -> bool` | Schrijft tekst naar een bestand onder de modulemap en maakt submappen aan. Geeft false terug voor absolute of ontsnappende paden, of bij een fout. |
| `STORAGE:ReadText(name)  -> string` | Geeft de ruwe tekst van een bestand terug (relatief ten opzichte van de modulemap, of absoluut), of nil als het ontbreekt of onleesbaar is. |
| `STORAGE:GetFullPath(name)  -> string` | Geeft het absolute pad van een bestand onder de modulemap terug, of nil voor lege of absolute invoer. |
| `STORAGE:WriteLobbyCode(name, contents)  -> bool` | Schrijft een bestand naar `Global/Lobbycodes/`. Geeft false terug voor lege, absolute of `..`-namen, of bij een fout. |
| `STORAGE:RevealLobbyCodes()  -> bool` | Opent de bestandsverkenner van het besturingssysteem op `Global/Lobbycodes/`. |
| `STORAGE:RevealInExplorer(name)  -> bool` | Opent de bestandsverkenner van het besturingssysteem met het benoemde modulebestand geselecteerd (Windows en macOS), of elders de map ervan. |

### JSONLOADER

Parseert JSON-bestanden en -strings uit de modulemap.

<div class="callout warn">
Geparseerde waarden zijn .NET-dictionaries: objecten hebben de ledennamen als sleutel, arrays hebben de sleutels 1..n. Direct indexeren van een ontbrekende sleutel (`d["x"]`) werpt een fout op, dus gebruik JsonGet voor opzoekingen die kunnen mislukken. Getallen worden gehele getallen of doubles; strings, booleans en null worden hun Lua-equivalenten. LoadJson geeft een JsonNode-boom terug; indexeer die met `node["member"]` en converteer bladeren met ExtractNumber / ExtractText.
</div>

| Methode | Beschrijving |
| --- | --- |
| `JSONLOADER:LoadJson(name)  -> JsonNode` | Parseert een JSON-bestand relatief ten opzichte van de modulemap naar een JsonNode-boom. Werpt een fout op als het bestand ontbreekt. |
| `JSONLOADER:ExtractNumber(value)  -> number` | Converteert een JsonNode-blad naar een getal; geeft 0 terug voor nil of niet-numerieke waarden. |
| `JSONLOADER:ExtractText(value)  -> string` | Converteert een JsonNode-blad naar een string; geeft nil terug voor nil. |
| `JSONLOADER:JsonParseFile(name)  -> dict` | Parseert een JSON-bestand waarvan de root een object is (lege dictionary als het bestand leeg is). Werpt een fout op als het bestand ontbreekt of de root geen object is. |
| `JSONLOADER:JsonParseFileAny(name)  -> dict` | Parseert een JSON-bestand waarvan de root een object of een array is (relatief of absoluut pad). Geeft nil terug als het bestand ontbreekt of leeg is. |
| `JSONLOADER:JsonParseString(json)  -> dict` | Parseert een JSON-string waarvan de root een object is (lege dictionary indien leeg). Werpt een fout op als de root geen object is. |
| `JSONLOADER:JsonParseStringAny(json)  -> dict` | Parseert een JSON-string waarvan de root een object of een array is. Geeft nil terug voor lege of ongeldige invoer. |
| `JSONLOADER:JsonGet(dict, key)  -> value` | Zoekt een stringsleutel op in een object of een integersleutel in een array; geeft nil terug wanneer die ontbreekt of wanneer dict geen geparseerde waarde is. |
| `JSONLOADER:JsonCount(dict)  -> int` | Aantal leden in een geparseerd object of een geparseerde array, of 0 voor al het andere. |

```lua
local data = JSONLOADER:JsonParseFileAny("Config/layout.json")
local colors = data and JSONLOADER:JsonGet(data, "colors")
local n = JSONLOADER:JsonCount(colors)
for i = 1, n do
    print(JSONLOADER:JsonGet(colors, i))
end
```

### INILOADER

Laadt een plat `key=value`-bestand uit de modulemap.

<div class="callout warn">
De loader splitst elke regel op de eerste `=`, slaat regels zonder over, en laat een herhaalde sleutel de eerdere waarde overschrijven. Hij kent geen secties, commentaren of aanhalingstekens. Een ontbrekend bestand levert een lege handle op.
</div>

| Methode | Beschrijving |
| --- | --- |
| `INILOADER:LoadIni(name)  -> ini` | Leest een `key=value`-bestand relatief ten opzichte van de modulemap. |

### INI-handle

Een geparseerd INI-bestand, teruggegeven door INILOADER:LoadIni, met getypeerde getters.

<div class="callout warn">
Getters geven de meegegeven standaardwaarde terug wanneer de sleutel ontbreekt. Wanneer de sleutel bestaat maar de waarde niet te parseren is, geven de numerieke getters 0 terug. Array-getters splitsen op komma's en geven een lege array terug wanneer de sleutel ontbreekt.
</div>

| Methode | Beschrijving |
| --- | --- |
| `ini:GetBool(key, default)  -> bool` | True wanneer de waarde als het gehele getal 1 parseert; de standaardwaarde wanneer de sleutel ontbreekt. |
| `ini:GetInt(key, default)  -> int` | De waarde als geheel getal. |
| `ini:GetDouble(key, default)  -> number` | De waarde als double. |
| `ini:GetString(key, default)  -> string` | De ruwe stringwaarde. |
| `ini:GetStringArray(key)  -> string[]` | De waarde gesplitst op komma's. |
| `ini:GetIntArray(key)  -> int[]` | De waarde gesplitst op komma's, elk deel geparseerd als geheel getal (0 indien niet parseerbaar). |
| `ini:GetDoubleArray(key)  -> double[]` | De waarde gesplitst op komma's, elk deel geparseerd als double (0 indien niet parseerbaar). |

## Gedeelde resources en configuratie

### SHARED

Een spelbrede opslag van texturen, geluiden en strings die stagewissels overleeft, zodat elke module een resource kan gebruiken die eenmaal is geladen (bijvoorbeeld bij het opstarten).

<div class="callout warn">
Set*-methoden laden op een achtergrondthread en wisselen de resource in op de renderthread; de optionele callback onCreate ontvangt de nieuwe handle zodra die op zijn plaats staat. Een nieuwere Set* op dezelfde sleutel verwerpt elke laadbeurt die nog loopt. Het inwisselen geeft de vorige resource vrij, wat elke handle ongeldig maakt die je vóór de herlaadbeurt hebt opgehaald; haal hem opnieuw op met Get*. De UsingAbsolutePath-varianten nemen een volledig pad. Zie Graphics en tekst voor textuur-handles en Audio voor geluid-handles.
</div>

| Methode | Beschrijving |
| --- | --- |
| `SHARED:SetSharedString(key, value)  -> nil` | Slaat een string op onder een sleutel. |
| `SHARED:GetSharedString(key)  -> string` | Geeft de string terug die onder een sleutel is opgeslagen, of een lege string. |
| `SHARED:GetSharedTexture(key)  -> texture` | Geeft de gedeelde textuur voor een sleutel terug, of een lege textuur als er geen is gezet. |
| `SHARED:GetSharedSound(key)  -> sound` | Geeft het gedeelde geluid voor een sleutel terug, of een leeg geluid als er geen is gezet. |
| `SHARED:ClearSharedTexture(key)  -> nil` | Geeft de textuur die onder een sleutel is opgeslagen vrij en vervangt die door een lege. |
| `SHARED:ClearSharedSound(key)  -> nil` | Geeft het geluid dat onder een sleutel is opgeslagen vrij en vervangt dat door een leeg. |
| `SHARED:SetSharedTexture(key, path, onCreate?)  -> nil` | Laadt een textuur van een pad relatief ten opzichte van de module in de opslag. |
| `SHARED:SetSharedTexture(key, path, options, onCreate?)  -> nil` | Hetzelfde, met een optietabel; `{ maxSize = N }` begrenst de langste zijde van de gedecodeerde textuur op N pixels. |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, onCreate?)  -> nil` | Laadt een textuur van een absoluut pad. |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, options, onCreate?)  -> nil` | Laadt een textuur van een absoluut pad met een optietabel. |
| `SHARED:SetSharedSFX(key, path, onCreate?)  -> nil` | Laadt een geluidseffect van een pad relatief ten opzichte van de module. |
| `SHARED:SetSharedBGM(key, path, onCreate?)  -> nil` | Laadt achtergrondmuziek (volumegroep nummerweergave) van een pad relatief ten opzichte van de module. |
| `SHARED:SetSharedVoice(key, path, onCreate?)  -> nil` | Laadt een stemfragment van een pad relatief ten opzichte van de module. |
| `SHARED:SetSharedPreview(key, path, onCreate?)  -> nil` | Laadt een nummerpreview van een pad relatief ten opzichte van de module. |
| `SHARED:SetSharedSFXUsingAbsolutePath(key, path, onCreate?)  -> nil` | Laadt een geluidseffect van een absoluut pad. |
| `SHARED:SetSharedBGMUsingAbsolutePath(key, path, onCreate?)  -> nil` | Laadt achtergrondmuziek van een absoluut pad. |
| `SHARED:SetSharedVoiceUsingAbsolutePath(key, path, onCreate?)  -> nil` | Laadt een stemfragment van een absoluut pad. |
| `SHARED:SetSharedPreviewUsingAbsolutePath(key, path, onCreate?)  -> nil` | Laadt een nummerpreview van een absoluut pad. |

```lua
-- in de opstartstage
function onStart()
    SHARED:SetSharedSFX("Decide", "Sounds/Decide.ogg")
end

-- in elke latere module
SHARED:GetSharedSound("Decide"):Play()
```

### CONFIG

Leest en wijzigt de spelconfiguratie: aantal spelers, modi, scoreberekening, gameplaymods per speler en volumeniveaus.

<div class="callout warn">
Eigenschappen gebruiken puntsyntaxis (`CONFIG.PlayerCount`), methoden gebruiken dubbelepuntsyntaxis. Spelerindices beginnen bij 0 (0 tot 4); setters negeren indices buiten bereik en getters geven daarvoor een standaardwaarde terug. Binnen alleen-lezen modules (ROActivities en achtergronden) logt elke setter een fout en doet niets. Wijzigingen gelden onmiddellijk in het geheugen; het spel schrijft Config.ini wanneer het normaal wordt afgesloten.
</div>

| Methode | Beschrijving |
| --- | --- |
| `CONFIG.ConfigIsNew  -> bool (read-only)` | True wanneer het spel de configuratie bij deze opstart heeft aangemaakt. |
| `CONFIG.Language  -> string (read-only)` | Het taal-id dat in Config.ini is opgeslagen. |
| `CONFIG.PlayerCount  -> int` | Aantal actieve spelers. De setter negeert waarden buiten 1..5. |
| `CONFIG.IsAIBattleMode  -> bool` | Of de AI-gevechtsmodus is ingeschakeld. |
| `CONFIG.AILevel  -> int` | AI-moeilijkheidsniveau, bij schrijven begrensd op 1..10. |
| `CONFIG.IsTrainingMode  -> bool` | Of de trainingsmodus is ingeschakeld. |
| `CONFIG.UseModernScoringMethod  -> bool` | Of het spel de moderne (shin-uchi) scoreberekening gebruikt. |
| `CONFIG.UsedLegacyScoringMethod  -> int` | Generatie van de legacy-scoreberekening (zie `CONFIG.LEGACY_SCORING`), bij schrijven begrensd op 0..3. |
| `CONFIG.AreSongUnlockablesDisabled  -> bool (read-only)` | Of het spel ontgrendelvoorwaarden van nummers negeert. |
| `CONFIG.SongSpeed  -> int` | Nummersnelheid in twintigsten van de factor: 20 is 1,0x. Bij schrijven begrensd op 2..200 (0,1x tot 10x). |
| `CONFIG.MasterVolume  -> int` | Hoofdvolume, bij schrijven begrensd op 0..100. |
| `CONFIG.SoundEffectVolume  -> int` | Volume van geluidseffecten, bij schrijven begrensd op 0..100. |
| `CONFIG.VoiceVolume  -> int` | Stemvolume, bij schrijven begrensd op 0..100. |
| `CONFIG.SongVolume  -> int` | Volume van de nummerweergave, bij schrijven begrensd op 0..100. |
| `CONFIG.PreviewVolume  -> int` | Volume van de nummerpreview, bij schrijven begrensd op 0..100. |
| `CONFIG:GetGameType(player)  -> int` | Het speltype van de speler (zie `CONFIG.GAMETYPE`); Taiko voor indices buiten bereik. |
| `CONFIG:SetGameType(player, gameType)  -> nil` | Stelt het speltype van de speler in en negeert niet-gedefinieerde waarden. |
| `CONFIG:GetDefaultCourse(player)  -> int` | De standaardmoeilijkheid (zie `CONFIG.DEFAULT_COURSE`); Normal voor indices buiten bereik. Eén globale instelling geldt voor elke speler, ondanks het spelerargument. |
| `CONFIG:SetDefaultCourse(player, difficulty)  -> nil` | Stelt de globale standaardmoeilijkheid in, begrensd van Easy tot één voorbij Extra Extreme (de gecombineerde weergave Extra/Extra-Extra). |
| `CONFIG:GetScrollSpeed(player)  -> int` | De scrollsnelheidswaarde van de speler: 9 is 1,0x, elke stap is 0,1x (zie `CONFIG.SCROLLSPEED`). 9 voor indices buiten bereik. |
| `CONFIG:SetScrollSpeed(player, speed)  -> nil` | Stelt de scrollsnelheidswaarde van de speler in, begrensd op 0..99. |
| `CONFIG:GetTimingZone(player)  -> int` | Het beoordelingsvenster van de speler: 0 Loose, 1 Lenient, 2 Normal, 3 Strict, 4 Rigorous. 2 voor indices buiten bereik. |
| `CONFIG:SetTimingZone(player, zone)  -> nil` | Stelt het beoordelingsvenster van de speler in, begrensd op 0..4. |
| `CONFIG:GetAutoStatus(player)  -> bool` | True wanneer de speler op auto-play staat of een replay bekijkt. |
| `CONFIG:SetAutoStatus(player, isAuto)  -> nil` | Schakelt auto-play voor de speler in of uit. |
| `CONFIG:GetRandomMod(player)  -> int` | De random-mod van de speler (zie `CONFIG.RANDOM`); Off voor indices buiten bereik. |
| `CONFIG:SetRandomMod(player, mode)  -> nil` | Stelt de random-mod van de speler in en negeert niet-gedefinieerde waarden. |
| `CONFIG:GetFunMod(player)  -> int` | De fun-mod van de speler (zie `CONFIG.FUN`); None voor indices buiten bereik. |
| `CONFIG:SetFunMod(player, mod)  -> nil` | Stelt de fun-mod van de speler in en negeert niet-gedefinieerde waarden. |
| `CONFIG:GetStealthMod(player)  -> int` | De stealth-mod van de speler (zie `CONFIG.STEALTH`); Off voor indices buiten bereik. |
| `CONFIG:SetStealthMod(player, mode)  -> nil` | Stelt de stealth-mod van de speler in en negeert niet-gedefinieerde waarden. |
| `CONFIG:GetJusticeMod(player)  -> int` | De beoordelingsmod van de speler: 0 uit, 1 Just (Ok telt als Bad), 2 Safe (Bad telt als Ok). |
| `CONFIG:SetJusticeMod(player, mode)  -> nil` | Stelt de beoordelingsmod van de speler in, begrensd op 0..2. |
| `CONFIG:GetModFlags(player)  -> integer` | Pakt scrollsnelheid, stealth, random, nummersnelheid, beoordelingsvenster, beoordelings- en fun-mods in één 64-bits waarde (één byte elk). |
| `CONFIG:SetModFlags(player, flags)  -> nil` | Past een door GetModFlags geproduceerde waarde weer toe op de speler (nummersnelheid is globaal). |

Constantentabellen op CONFIG geven namen aan de bovenstaande integerwaarden. `SONGSPEED` en `SCROLLSPEED` converteren ook tussen opgeslagen waarden en factoren.

| Lid | Beschrijving |
| --- | --- |
| `CONFIG.SONGSPEED.Normal  -> int` | 20, de opgeslagen waarde voor 1,0x. |
| `CONFIG.SONGSPEED:ToActual(value)  -> number` | Converteert een opgeslagen nummersnelheid naar haar factor (value / 20). |
| `CONFIG.SONGSPEED:FromActual(multiplier)  -> int` | Converteert een factor naar de dichtstbijzijnde opgeslagen nummersnelheid. |
| `CONFIG.SCROLLSPEED.Normal  -> int` | 9, de opgeslagen waarde voor 1,0x. |
| `CONFIG.SCROLLSPEED:ToActual(value)  -> number` | Converteert een opgeslagen scrollsnelheid naar haar factor ((value + 1) / 10). |
| `CONFIG.SCROLLSPEED:FromActual(multiplier)  -> int` | Converteert een factor naar de dichtstbijzijnde opgeslagen scrollsnelheid. |
| `CONFIG.GAMETYPE` | `Taiko`, `Konga`. |
| `CONFIG.DEFAULT_COURSE` | `Easy`, `Normal`, `Hard`, `Oni`, `Edit`. |
| `CONFIG.LEGACY_SCORING` | `Gen1Oni`, `Gen1_2`, `Gen2`, `Gen3`. |
| `CONFIG.RANDOM` | `Off`, `Random`, `Mirror`, `SuperRandom`, `MirrorRandom`. |
| `CONFIG.STEALTH` | `Off`, `Doron`, `Stealth`. |
| `CONFIG.FUN` | `None`, `Avalanche`, `Minesweeper`, `DynamicBeat`, `Total`. |
| `CONFIG.JUSTICE` | `None`, `Just`, `Safe`. |

```lua
local multiplier = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
if CONFIG:GetRandomMod(0) == CONFIG.RANDOM.Mirror then
    -- gespiegelde chart
end
```
