<!-- api/data.md -->

# Daten und Persistenz

Speichern von Daten, die einen Neustart überdauern, Abfragen von SQLite-Dateien, Lesen von Dateien, JSON und INI aus dem Modulordner, gemeinsame Nutzung von Ressourcen zwischen Modulen sowie Lesen oder Ändern der Spielkonfiguration.

Relative Pfade, die an DATABASE, SQL, STORAGE, JSONLOADER, INILOADER und SHARED übergeben werden, werden gegen das Verzeichnis des laufenden Moduls aufgelöst (der Ordner, der seine `Script.lua` enthält).

Einige Methoden auf dieser Seite geben .NET-Collections zurück, die sich anders verhalten als Lua-Tabellen:

- Arrays (`string[]`, `int[]`, `double[]`) beginnen bei Index 0 und stellen `.Length` bereit.
- Dictionaries (geparstes JSON, SQL-Zeilen, Sprachtabellen) nehmen `d["key"]` entgegen (oder `d[1]` für aus JSON geparste Arrays) und lassen sich über `d:GetEnumerator()` aufzählen; `pairs` und `#` funktionieren auf ihnen nicht.

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

## Key-Value-Datenbank

### DATABASE

Öffnet LMDB-Key-Value-Speicher, die String-Werte über Neustarts hinweg aufbewahren, entweder im Modulordner oder im spielweiten Datenordner.

<div class="callout warn">
Jedes Read und Write öffnet und schließt seine eigene LMDB-Umgebung, sodass jeder Aufruf teuer ist; speichern Sie Werte in Lua zwischen und aktualisieren Sie den Cache, wenn Sie schreiben. In schreibgeschützten Modulen (ROActivities und Hintergründen) liefert DATABASE Speicher, deren Write einen Fehler protokolliert und nichts tut.
</div>

| Methode | Beschreibung |
| --- | --- |
| `DATABASE:OpenLocalDatabase(path)  -> database` | Öffnet (und erzeugt bei Bedarf) einen Speicher unter einem Pfad relativ zum Modulverzeichnis. |
| `DATABASE:OpenGlobalDatabase(path)  -> database` | Öffnet (und erzeugt bei Bedarf) einen Speicher unter `Global/ApplicationData/LMDB/` im Spielordner, der von jedem Modul gemeinsam genutzt wird. |

### Database-Handle

Ein Key-Value-Speicher, der von DATABASE zurückgegeben wird und String-Schlüssel auf String-Werte abbildet.

<div class="callout warn">
Werte sind ausschließlich Strings; konvertieren Sie Zahlen und Booleans selbst (zum Beispiel mit tostring und tonumber).
</div>

| Methode | Beschreibung |
| --- | --- |
| `database:Write(key, value)  -> nil` | Speichert einen String unter dem Schlüssel und committet ihn. Protokolliert in schreibgeschützten Modulen einen Fehler und tut nichts. |
| `database:Read(key)  -> string` | Gibt den unter dem Schlüssel gespeicherten String zurück, oder nil, wenn der Schlüssel fehlt oder das Lesen fehlschlägt. |
| `database:Dispose()  -> nil` | Tut nichts; das Handle hält keine offenen Ressourcen. |

```lua
local db

function activate()
    db = DATABASE:OpenGlobalDatabase("GameStatus")
    if db:Read("new_user") == nil then
        db:Write("new_user", "true")
    end
end
```

## SQL-Datenbank

### SQL

Öffnet eine SQLite-Datenbankdatei im Modulverzeichnis zum Ausführen von SQL-Anweisungen.

| Methode | Beschreibung |
| --- | --- |
| `SQL:OpenSQLDatabase(path)  -> sql` | Öffnet eine SQLite-Datenbank unter einem Pfad relativ zum Modulverzeichnis. |

### SQL-Handle

Eine SQLite-Verbindung, die von SQL:OpenSQLDatabase zurückgegeben wird.

<div class="callout warn">
Query gibt ein Dictionary mit den Schlüsseln 1..n zurück; jede Zeile ist ein Dictionary mit Spaltennamen als Schlüssel (siehe den Hinweis zu .NET-Collections oben auf der Seite). Eine fehlgeschlagene Anweisung protokolliert den Fehler und liefert ein leeres Ergebnis. Query reicht den Anweisungstext unverändert und ohne Parameterbindung an SQLite weiter; escapen Sie daher jeden Wert, den Sie hineinfügen.
</div>

| Methode | Beschreibung |
| --- | --- |
| `sql:Query(query)  -> rows` | Führt den SQL-Text aus und gibt die Ergebniszeilen zurück. |

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

## Dateien, JSON und INI

### STORAGE

Dateizugriff mit dem Modulverzeichnis als Wurzel, plus Hilfsfunktionen zum Teilen von Online-Lobby-Codes.

<div class="callout warn">
WriteText schreibt nur innerhalb des Modulordners: Es lehnt absolute Pfade und Pfade ab, die außerhalb aufgelöst werden. ReadText akzeptiert auch einen absoluten Pfad. Die Lobby-Code-Hilfsfunktionen verwenden den gemeinsamen Ordner `Global/Lobbycodes/` neben der ausführbaren Datei. Schreibgeschützte Module können jede STORAGE-Methode verwenden.
</div>

| Methode | Beschreibung |
| --- | --- |
| `STORAGE:GetFiles(dir, pattern)  -> string[]` | Listet die Dateien in einem Unterverzeichnis des Modulordners auf, die einem Suchmuster wie `"*.png"` entsprechen. Die Einträge sind Pfade relativ zum Modulordner, einschließlich des Unterverzeichnisses. Das Verzeichnis muss existieren. |
| `STORAGE:FileExists(path)  -> bool` | True, wenn unter dem Pfad relativ zum Modulordner eine Datei existiert. |
| `STORAGE:DirectoryExists(path)  -> bool` | True, wenn unter dem Pfad relativ zum Modulordner ein Verzeichnis existiert. |
| `STORAGE:WriteText(name, contents)  -> bool` | Schreibt Text in eine Datei unter dem Modulordner und legt Unterverzeichnisse an. Gibt false zurück bei absoluten oder ausbrechenden Pfaden oder bei Fehlschlag. |
| `STORAGE:ReadText(name)  -> string` | Gibt den rohen Text einer Datei zurück (relativ zum Modulordner oder absolut), oder nil, wenn sie fehlt oder nicht lesbar ist. |
| `STORAGE:GetFullPath(name)  -> string` | Gibt den absoluten Pfad einer Datei unter dem Modulordner zurück, oder nil bei leerer oder absoluter Eingabe. |
| `STORAGE:WriteLobbyCode(name, contents)  -> bool` | Schreibt eine Datei nach `Global/Lobbycodes/`. Gibt false zurück bei leeren, absoluten oder `..`-Namen oder bei Fehlschlag. |
| `STORAGE:RevealLobbyCodes()  -> bool` | Öffnet den Dateibrowser des Betriebssystems bei `Global/Lobbycodes/`. |
| `STORAGE:RevealInExplorer(name)  -> bool` | Öffnet den Dateibrowser des Betriebssystems mit der benannten Moduldatei ausgewählt (Windows und macOS) oder andernorts mit ihrem Ordner. |

### JSONLOADER

Parst JSON-Dateien und -Strings aus dem Modulverzeichnis.

<div class="callout warn">
Geparste Werte sind .NET-Dictionaries: Objekte sind nach Membernamen indiziert, Arrays mit 1..n. Das direkte Indizieren eines fehlenden Schlüssels (`d["x"]`) löst einen Fehler aus; verwenden Sie daher JsonGet für Abfragen, die fehlschlagen können. Zahlen werden zu Ganzzahlen oder Doubles; Strings, Booleans und null werden auf ihre Lua-Entsprechungen abgebildet. LoadJson gibt einen JsonNode-Baum zurück; indizieren Sie ihn mit `node["member"]` und konvertieren Sie Blätter mit ExtractNumber / ExtractText.
</div>

| Methode | Beschreibung |
| --- | --- |
| `JSONLOADER:LoadJson(name)  -> JsonNode` | Parst eine JSON-Datei relativ zum Modulverzeichnis in einen JsonNode-Baum. Löst einen Fehler aus, wenn die Datei fehlt. |
| `JSONLOADER:ExtractNumber(value)  -> number` | Konvertiert ein JsonNode-Blatt in eine Zahl; gibt 0 für nil oder nicht-numerische Werte zurück. |
| `JSONLOADER:ExtractText(value)  -> string` | Konvertiert ein JsonNode-Blatt in einen String; gibt nil für nil zurück. |
| `JSONLOADER:JsonParseFile(name)  -> dict` | Parst eine JSON-Datei, deren Wurzel ein Objekt ist (leeres Dictionary, wenn die Datei leer ist). Löst einen Fehler aus, wenn die Datei fehlt oder die Wurzel kein Objekt ist. |
| `JSONLOADER:JsonParseFileAny(name)  -> dict` | Parst eine JSON-Datei, deren Wurzel ein Objekt oder ein Array ist (relativer oder absoluter Pfad). Gibt nil zurück, wenn die Datei fehlt oder leer ist. |
| `JSONLOADER:JsonParseString(json)  -> dict` | Parst einen JSON-String, dessen Wurzel ein Objekt ist (leeres Dictionary, wenn leer). Löst einen Fehler aus, wenn die Wurzel kein Objekt ist. |
| `JSONLOADER:JsonParseStringAny(json)  -> dict` | Parst einen JSON-String, dessen Wurzel ein Objekt oder ein Array ist. Gibt nil bei leerer oder ungültiger Eingabe zurück. |
| `JSONLOADER:JsonGet(dict, key)  -> value` | Schlägt einen String-Schlüssel in einem Objekt oder einen Ganzzahl-Schlüssel in einem Array nach; gibt nil zurück, wenn er fehlt oder dict kein geparster Wert ist. |
| `JSONLOADER:JsonCount(dict)  -> int` | Anzahl der Member in einem geparsten Objekt oder Array, oder 0 für alles andere. |

```lua
local data = JSONLOADER:JsonParseFileAny("Config/layout.json")
local colors = data and JSONLOADER:JsonGet(data, "colors")
local n = JSONLOADER:JsonCount(colors)
for i = 1, n do
    print(JSONLOADER:JsonGet(colors, i))
end
```

### INILOADER

Lädt eine flache `key=value`-Datei aus dem Modulverzeichnis.

<div class="callout warn">
Der Lader teilt jede Zeile am ersten `=`, überspringt Zeilen ohne eines und lässt einen wiederholten Schlüssel den früheren Wert überschreiben. Er kennt keine Abschnitte, Kommentare oder Anführungszeichen. Eine fehlende Datei ergibt ein leeres Handle.
</div>

| Methode | Beschreibung |
| --- | --- |
| `INILOADER:LoadIni(name)  -> ini` | Liest eine `key=value`-Datei relativ zum Modulverzeichnis. |

### INI-Handle

Eine geparste INI-Datei, die von INILOADER:LoadIni zurückgegeben wird, mit typisierten Gettern.

<div class="callout warn">
Getter geben den übergebenen Standardwert zurück, wenn der Schlüssel fehlt. Existiert der Schlüssel, lässt sich sein Wert aber nicht parsen, geben die numerischen Getter 0 zurück. Array-Getter teilen an Kommas und geben ein leeres Array zurück, wenn der Schlüssel fehlt.
</div>

| Methode | Beschreibung |
| --- | --- |
| `ini:GetBool(key, default)  -> bool` | True, wenn der Wert als Ganzzahl 1 geparst wird; der Standardwert, wenn der Schlüssel fehlt. |
| `ini:GetInt(key, default)  -> int` | Der Wert als Ganzzahl. |
| `ini:GetDouble(key, default)  -> number` | Der Wert als Double. |
| `ini:GetString(key, default)  -> string` | Der rohe String-Wert. |
| `ini:GetStringArray(key)  -> string[]` | Der Wert, an Kommas geteilt. |
| `ini:GetIntArray(key)  -> int[]` | Der Wert, an Kommas geteilt, jeder Teil als Ganzzahl geparst (0, wenn nicht parsbar). |
| `ini:GetDoubleArray(key)  -> double[]` | Der Wert, an Kommas geteilt, jeder Teil als Double geparst (0, wenn nicht parsbar). |

## Gemeinsame Ressourcen und Konfiguration

### SHARED

Ein spielweiter Speicher für Texturen, Sounds und Strings, der Stage-Wechsel überdauert, sodass jedes Modul eine einmal (zum Beispiel beim Start) geladene Ressource verwenden kann.

<div class="callout warn">
Set*-Methoden laden auf einem Hintergrund-Thread und tauschen die Ressource auf dem Render-Thread ein; der optionale onCreate-Callback erhält das neue Handle, sobald es eingesetzt ist. Ein neueres Set* auf denselben Schlüssel verwirft jeden noch laufenden Ladevorgang. Das Austauschen gibt die vorherige Ressource frei, was jedes vor dem Neuladen geholte Handle ungültig macht; holen Sie es mit Get* erneut. Die UsingAbsolutePath-Varianten nehmen einen vollständigen Pfad. Siehe Grafik und Text für Textur-Handles und Audio für Sound-Handles.
</div>

| Methode | Beschreibung |
| --- | --- |
| `SHARED:SetSharedString(key, value)  -> nil` | Speichert einen String unter einem Schlüssel. |
| `SHARED:GetSharedString(key)  -> string` | Gibt den unter einem Schlüssel gespeicherten String zurück, oder einen leeren String. |
| `SHARED:GetSharedTexture(key)  -> texture` | Gibt die gemeinsame Textur für einen Schlüssel zurück, oder eine leere Textur, wenn keine gesetzt wurde. |
| `SHARED:GetSharedSound(key)  -> sound` | Gibt den gemeinsamen Sound für einen Schlüssel zurück, oder einen leeren Sound, wenn keiner gesetzt wurde. |
| `SHARED:ClearSharedTexture(key)  -> nil` | Gibt die unter einem Schlüssel gespeicherte Textur frei und ersetzt sie durch eine leere. |
| `SHARED:ClearSharedSound(key)  -> nil` | Gibt den unter einem Schlüssel gespeicherten Sound frei und ersetzt ihn durch einen leeren. |
| `SHARED:SetSharedTexture(key, path, onCreate?)  -> nil` | Lädt eine Textur aus einem modulrelativen Pfad in den Speicher. |
| `SHARED:SetSharedTexture(key, path, options, onCreate?)  -> nil` | Dasselbe, mit einer Optionstabelle; `{ maxSize = N }` begrenzt die längere Seite der dekodierten Textur auf N Pixel. |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, onCreate?)  -> nil` | Lädt eine Textur aus einem absoluten Pfad. |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, options, onCreate?)  -> nil` | Lädt eine Textur aus einem absoluten Pfad mit einer Optionstabelle. |
| `SHARED:SetSharedSFX(key, path, onCreate?)  -> nil` | Lädt einen Soundeffekt aus einem modulrelativen Pfad. |
| `SHARED:SetSharedBGM(key, path, onCreate?)  -> nil` | Lädt Hintergrundmusik (Lautstärkegruppe Songwiedergabe) aus einem modulrelativen Pfad. |
| `SHARED:SetSharedVoice(key, path, onCreate?)  -> nil` | Lädt einen Sprachclip aus einem modulrelativen Pfad. |
| `SHARED:SetSharedPreview(key, path, onCreate?)  -> nil` | Lädt einen Songvorschau-Clip aus einem modulrelativen Pfad. |
| `SHARED:SetSharedSFXUsingAbsolutePath(key, path, onCreate?)  -> nil` | Lädt einen Soundeffekt aus einem absoluten Pfad. |
| `SHARED:SetSharedBGMUsingAbsolutePath(key, path, onCreate?)  -> nil` | Lädt Hintergrundmusik aus einem absoluten Pfad. |
| `SHARED:SetSharedVoiceUsingAbsolutePath(key, path, onCreate?)  -> nil` | Lädt einen Sprachclip aus einem absoluten Pfad. |
| `SHARED:SetSharedPreviewUsingAbsolutePath(key, path, onCreate?)  -> nil` | Lädt einen Songvorschau-Clip aus einem absoluten Pfad. |

```lua
-- in der Boot-Stage
function onStart()
    SHARED:SetSharedSFX("Decide", "Sounds/Decide.ogg")
end

-- in jedem späteren Modul
SHARED:GetSharedSound("Decide"):Play()
```

### CONFIG

Liest und ändert die Spielkonfiguration: Spieleranzahl, Modi, Wertung, Gameplay-Mods pro Spieler und Lautstärkepegel.

<div class="callout warn">
Eigenschaften verwenden Punktsyntax (`CONFIG.PlayerCount`), Methoden Doppelpunktsyntax. Spielerindizes sind 0-basiert (0 bis 4); Setter ignorieren Indizes außerhalb des Bereichs, und Getter geben für sie einen Standardwert zurück. In schreibgeschützten Modulen (ROActivities und Hintergründen) protokolliert jeder Setter einen Fehler und tut nichts. Änderungen gelten sofort im Speicher; das Spiel schreibt Config.ini, wenn es normal beendet wird.
</div>

| Methode | Beschreibung |
| --- | --- |
| `CONFIG.ConfigIsNew  -> bool (read-only)` | True, wenn das Spiel die Konfiguration bei diesem Start erzeugt hat. |
| `CONFIG.Language  -> string (read-only)` | Die in Config.ini gespeicherte Sprach-ID. |
| `CONFIG.PlayerCount  -> int` | Anzahl aktiver Spieler. Der Setter ignoriert Werte außerhalb von 1..5. |
| `CONFIG.IsAIBattleMode  -> bool` | Ob der KI-Kampf-Modus aktiviert ist. |
| `CONFIG.AILevel  -> int` | KI-Schwierigkeitsstufe, beim Schreiben auf 1..10 begrenzt. |
| `CONFIG.IsTrainingMode  -> bool` | Ob der Trainingsmodus aktiviert ist. |
| `CONFIG.UseModernScoringMethod  -> bool` | Ob das Spiel die moderne Wertungsmethode (Shin-Uchi) verwendet. |
| `CONFIG.UsedLegacyScoringMethod  -> int` | Generation der Legacy-Wertung (siehe `CONFIG.LEGACY_SCORING`), beim Schreiben auf 0..3 begrenzt. |
| `CONFIG.AreSongUnlockablesDisabled  -> bool (read-only)` | Ob das Spiel Song-Freischaltbedingungen ignoriert. |
| `CONFIG.SongSpeed  -> int` | Songgeschwindigkeit in Zwanzigsteln des Faktors: 20 ist 1,0x. Beim Schreiben auf 2..200 (0,1x bis 10x) begrenzt. |
| `CONFIG.MasterVolume  -> int` | Gesamtlautstärke, beim Schreiben auf 0..100 begrenzt. |
| `CONFIG.SoundEffectVolume  -> int` | Soundeffekt-Lautstärke, beim Schreiben auf 0..100 begrenzt. |
| `CONFIG.VoiceVolume  -> int` | Stimmenlautstärke, beim Schreiben auf 0..100 begrenzt. |
| `CONFIG.SongVolume  -> int` | Songwiedergabe-Lautstärke, beim Schreiben auf 0..100 begrenzt. |
| `CONFIG.PreviewVolume  -> int` | Songvorschau-Lautstärke, beim Schreiben auf 0..100 begrenzt. |
| `CONFIG:GetGameType(player)  -> int` | Der Spieltyp des Spielers (siehe `CONFIG.GAMETYPE`); Taiko für Indizes außerhalb des Bereichs. |
| `CONFIG:SetGameType(player, gameType)  -> nil` | Setzt den Spieltyp des Spielers und ignoriert undefinierte Werte. |
| `CONFIG:GetDefaultCourse(player)  -> int` | Der Standard-Schwierigkeitsgrad (siehe `CONFIG.DEFAULT_COURSE`); Normal für Indizes außerhalb des Bereichs. Trotz des Spielerarguments gilt eine globale Einstellung für jeden Spieler. |
| `CONFIG:SetDefaultCourse(player, difficulty)  -> nil` | Setzt den globalen Standard-Schwierigkeitsgrad, begrenzt von Easy bis eins über Extra Extreme (die kombinierte Extra/Extra-Extra-Anzeige). |
| `CONFIG:GetScrollSpeed(player)  -> int` | Der Scrollgeschwindigkeitswert des Spielers: 9 ist 1,0x, jeder Schritt ist 0,1x (siehe `CONFIG.SCROLLSPEED`). 9 für Indizes außerhalb des Bereichs. |
| `CONFIG:SetScrollSpeed(player, speed)  -> nil` | Setzt den Scrollgeschwindigkeitswert des Spielers, begrenzt auf 0..99. |
| `CONFIG:GetTimingZone(player)  -> int` | Das Trefferfenster des Spielers: 0 Loose, 1 Lenient, 2 Normal, 3 Strict, 4 Rigorous. 2 für Indizes außerhalb des Bereichs. |
| `CONFIG:SetTimingZone(player, zone)  -> nil` | Setzt das Trefferfenster des Spielers, begrenzt auf 0..4. |
| `CONFIG:GetAutoStatus(player)  -> bool` | True, wenn der Spieler auf Auto-Play steht oder ein Replay ansieht. |
| `CONFIG:SetAutoStatus(player, isAuto)  -> nil` | Aktiviert oder deaktiviert Auto-Play für den Spieler. |
| `CONFIG:GetRandomMod(player)  -> int` | Der Random-Mod des Spielers (siehe `CONFIG.RANDOM`); Off für Indizes außerhalb des Bereichs. |
| `CONFIG:SetRandomMod(player, mode)  -> nil` | Setzt den Random-Mod des Spielers und ignoriert undefinierte Werte. |
| `CONFIG:GetFunMod(player)  -> int` | Der Fun-Mod des Spielers (siehe `CONFIG.FUN`); None für Indizes außerhalb des Bereichs. |
| `CONFIG:SetFunMod(player, mod)  -> nil` | Setzt den Fun-Mod des Spielers und ignoriert undefinierte Werte. |
| `CONFIG:GetStealthMod(player)  -> int` | Der Stealth-Mod des Spielers (siehe `CONFIG.STEALTH`); Off für Indizes außerhalb des Bereichs. |
| `CONFIG:SetStealthMod(player, mode)  -> nil` | Setzt den Stealth-Mod des Spielers und ignoriert undefinierte Werte. |
| `CONFIG:GetJusticeMod(player)  -> int` | Der Wertungs-Mod des Spielers: 0 aus, 1 Just (Ok zählt als Bad), 2 Safe (Bad zählt als Ok). |
| `CONFIG:SetJusticeMod(player, mode)  -> nil` | Setzt den Wertungs-Mod des Spielers, begrenzt auf 0..2. |
| `CONFIG:GetModFlags(player)  -> integer` | Packt Scrollgeschwindigkeit, Stealth, Random, Songgeschwindigkeit, Trefferfenster, Wertungs- und Fun-Mods in einen 64-Bit-Wert (je ein Byte). |
| `CONFIG:SetModFlags(player, flags)  -> nil` | Wendet einen von GetModFlags erzeugten Wert wieder auf den Spieler an (Songgeschwindigkeit ist global). |

Konstantentabellen auf CONFIG geben den obigen Ganzzahlwerten Namen. `SONGSPEED` und `SCROLLSPEED` konvertieren außerdem zwischen gespeicherten Werten und Faktoren.

| Member | Beschreibung |
| --- | --- |
| `CONFIG.SONGSPEED.Normal  -> int` | 20, der gespeicherte Wert für 1,0x. |
| `CONFIG.SONGSPEED:ToActual(value)  -> number` | Konvertiert eine gespeicherte Songgeschwindigkeit in ihren Faktor (value / 20). |
| `CONFIG.SONGSPEED:FromActual(multiplier)  -> int` | Konvertiert einen Faktor in die nächstgelegene gespeicherte Songgeschwindigkeit. |
| `CONFIG.SCROLLSPEED.Normal  -> int` | 9, der gespeicherte Wert für 1,0x. |
| `CONFIG.SCROLLSPEED:ToActual(value)  -> number` | Konvertiert eine gespeicherte Scrollgeschwindigkeit in ihren Faktor ((value + 1) / 10). |
| `CONFIG.SCROLLSPEED:FromActual(multiplier)  -> int` | Konvertiert einen Faktor in die nächstgelegene gespeicherte Scrollgeschwindigkeit. |
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
    -- gespiegeltes Chart
end
```
