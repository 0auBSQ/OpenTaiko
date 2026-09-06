<!-- getting-started.md -->

# So funktionieren Module

OpenTaiko 0.6.1 erlaubt es einem Skin, Bildschirme mit Lua hinzuzufügen und zu ersetzen. Der Modules-Ordner eines Skins enthält einen Ordner pro Modul, und jedes Modul besitzt eine Script.lua, die einen festen Satz globaler Callback-Funktionen definiert. Das Spiel lädt jede Script.lua in einen eigenen, abgeschotteten Lua-Zustand, registriert die globalen Objekte der Engine (TEXTURE, SOUND, INPUT, CONFIG und die weiteren, die in der [API-Referenz](api/README.md) dokumentiert sind) und ruft die Callbacks zum passenden Zeitpunkt auf. [Module und Lebenszyklus](api/activities.md) listet die genauen Signaturen auf.

## Bevor Sie beginnen

- OpenTaiko 0.6.1 und ein Skin-Ordner mit einem Modules-Verzeichnis. Der mitgelieferte Skin ist System/Open-World Memories.
- Ein Texteditor und Grundkenntnisse in Lua (Funktionen, Tabellen, require).
- Die mitgelieferten Stages unter System/Open-World Memories/Modules/Stages. Die kleinen, demo1 und demo3, zeigen die Form der Callbacks; die größeren zeigen, wie echte Bildschirme aufgebaut sind.

## Wo Module liegen

Jede Modulart hat ihren eigenen Ordner unter Modules, und jedes Modul ist ein Ordner, dessen Name die ID des Moduls ist:

```
Modules/
  Stages/        <name>/Script.lua   Vollbildschirme
  Activities/    <name>/Script.lua   Unterbildschirme, die von einer Stage gesteuert werden
  ROActivities/  <name>/Script.lua   schreibgeschützte Unterbildschirme und Overlays
  Transitions/   <name>/Script.lua   Überblendungen zwischen Stages (werden zuerst geladen)
  Lib/           gemeinsame .lua-Dateien, erreichbar über require; werden nicht als Module gescannt
```

Die Einstiegsdatei ist immer Script.lua. Die Asset-Pfade, die Sie an TEXTURE, SOUND, VIDEO und die anderen Lader übergeben, sind relativ zum Modulordner; die mitgelieferten Module legen sie per Konvention in den Unterordnern Textures, Sounds, Videos und Databases ab und speichern Übersetzungen in einem lang-Ordner.

Zwei Arten von Skripten liegen an anderer Stelle:

- Hintergründe (Bildschirmhintergründe, Gameplay-Ebenen, Mobs, Clear-Animationen, das Kusudama) sind Script.lua-Dateien unter dem Graphics-Ordner des Skins, im Verzeichnis des Bildschirms, den sie dekorieren. Siehe den Abschnitt Hintergründe unter [Module und Lebenszyklus](api/activities.md).
- Charaktere sind Ordner unter Global/Characters. Ein Charakterordner kann eine eigene Script.lua enthalten; ohne eine solche verwendet das Spiel sein eingebautes Charakterskript. Siehe [Charaktere hinzufügen](guides/characters.md).

## Script.lua definiert globale Funktionen

Die Script.lua eines Moduls definiert globale Funktionen auf oberster Ebene mit festen Namen, und das Spiel liest jede davon als globale Variable. Eine Funktion, die Sie in eine lokale Tabelle packen und zurückgeben, findet das Spiel nie, und einen falsch geschriebenen Namen (OnStart anstelle von onStart) ruft es nie auf, weil es einen undefinierten Callback als No-op behandelt und nichts meldet. Alles andere in der Datei kann lokal sein, und Sie können das Modul auf mehrere Dateien aufteilen, die mit require geladen werden.

demo3 ist die minimale Vorlage zum Kopieren:

```lua
-- Modules/Stages/mystage/Script.lua
local text = nil
local textTex = nil

function onStart()          -- einmal beim Laden des Skins: hier Assets laden
    text = TEXT:Create(16)
end

function activate()         -- bei jedem Betreten der Stage
    textTex = text:GetText("Hello")
end

function update(timestamp)  -- jeden Frame: Eingabe und Zustandsänderungen
    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        return Exit("title", nil)
    end
end

function draw()             -- jeden Frame: nur zeichnen
    if textTex ~= nil then textTex:Draw(200, 200) end
end

function deactivate()       -- beim Verlassen der Stage: Sounds stoppen, Datenbanken schließen
end

function onDestroy()        -- bevor der Skin entladen wird: Erzeugtes freigeben
    if textTex ~= nil then textTex:Dispose() end
end
```

## Der Lebenszyklus einer Stage

- onStart(): Das Spiel ruft es einmal nach dem Laden des Skins auf und erneut nach jedem Neuladen des Skins, unabhängig davon, ob die Stage auf dem Bildschirm ist. Laden Sie hier Texturen, Sounds und Videos. Es läuft als Coroutine, sodass sich ein langer Ladevorgang mit dem Hilfsobjekt LOADING hinter dem Ladebalken auf mehrere Frames verteilen kann.
- activate(): Das Spiel ruft es bei jedem Betreten der Stage auf. Setzen Sie hier den Zustand pro Besuch zurück, starten Sie Musik und öffnen Sie Datenbanken. Es läuft ebenfalls als Coroutine und kann LOADING verwenden. Das Spiel aktualisiert die Charakter- und Puchichara-Listen (CHARACTERLIST, PUCHICHARALIST) unmittelbar bevor activate läuft; lesen Sie sie also hier. onStart läuft vor dieser Aktualisierung.
- update(timestamp): Das Spiel ruft es jeden Frame vor draw auf und übergibt die Spieluhr in Millisekunden. Verarbeiten Sie hier Eingaben und ändern Sie den Zustand. Sobald die Stage Exit aufgerufen hat, ruft das Spiel update nicht mehr auf und ruft draw bis zum Ende des Ausblendens weiter auf.
- draw(): Das Spiel ruft es jeden Frame auf. Zeichnen Sie nur, und halten Sie die Allokationen pro Frame gering.
- deactivate(): Das Spiel ruft es beim Verlassen der Stage auf. demo3 gibt hier seine Datenbanken frei; demo1 stoppt seine Musik und sein Video.
- afterSongEnum(): Das Spiel ruft es jedes Mal auf, wenn die Song-Enumeration abgeschlossen ist, beim Start und nach einem weichen oder harten Neuladen, auch wenn die Stage nicht aktiv ist. Verwenden Sie es, wenn das Modul von der Songliste abhängt.
- onDestroy(): Das Spiel ruft es auf, bevor es den Skin entlädt. demo1 gibt hier seine Textur, sein Video, seine Texttextur und seine Sounds frei.
- reloadLanguage(lang): Das Spiel ruft es auf, wenn sich die Sprache ändert (siehe den Abschnitt zur Lokalisierung weiter unten).

Beim Laden eines Skins erzeugt das Spiel die Module Art für Art, zuerst Transitions, dann Stages, Activities und ROActivities. Innerhalb einer Art führt es jede Script.lua aus, bevor es irgendein onStart aufruft. Activities und ROActivities existieren daher noch nicht, während das onStart einer Stage läuft; schlagen Sie sie in activate nach.

Die anderen Arten verwenden Varianten dieses Satzes. Activities und ROActivities haben dieselben Callbacks, aber die hostende Stage ruft activate, deactivate, draw und update auf und erhält ihre Rückgabewerte. Hintergründe erhalten ein Zustandsobjekt in activate(state), update(timestamp, state) und draw(state) und können Ereignis-Hooks wie clearIn, playEndAnime und kusuBroke definieren. Übergänge definieren fadeOut(t), loading(progress, elapsed) und fadeIn(t). Charaktere definieren einen eigenen Satz aus Animationen und Stimmen. [Module und Lebenszyklus](api/activities.md) listet alle auf.

## Eine Modulart wählen

- Stage (Modules/Stages): ein vollständiger Bildschirm, zu dem das Spiel wechselt. Sie besitzt den Frame, verarbeitet Eingaben und wird durch Aufruf von Exit verlassen. Verwenden Sie sie für alles, was ein eigener Bildschirm ist.
- Activity (Modules/Activities): ein Unterbildschirm, den eine Stage von innen heraus verwendet, etwa ein Dialog. Sie ist ein Singleton, das Sie mit ACTIVITY:GetActivity(name) nachschlagen; die hostende Stage ruft ihre Activate, Update, Draw und Deactivate auf. Verwenden Sie sie für gemeinsam genutzte Elemente, die Spielzustand schreiben dürfen.
- ROActivity (Modules/ROActivities): die schreibgeschützte Form einer Activity, die Sie mit ROACTIVITY:GetROActivity(name) nachschlagen. Sie erhält schreibgeschützte CONFIG, DATABASE und GetSaveFile und hat kein globales Objekt ACTIVITY. Verwenden Sie sie für Elemente, die Zustand nur lesen, was die meisten wiederverwendbaren UI-Elemente abdeckt. Die Engine hostet mehrere eigene Overlays als ROActivities mit festen Namen (nameplate, modal, modicons, danplate, popup_menu, config_ui, song_enum); ein Skin ersetzt eines davon, indem er einen Ordner mit diesem Namen mitliefert und die Callbacks beibehält, die die Engine aufruft.
- Hintergrund: eine Script.lua unter Graphics, die hinter oder über einem der Engine-Bildschirme zeichnet. Hintergründe erhalten dieselben schreibgeschützten globalen Objekte wie ROActivities.
- Übergang (Modules/Transitions): das Ausblenden, der Ladebildschirm und das Einblenden, die das Spiel zwischen Stages abspielt. Eine Stage wählt einen Übergang im dritten Argument von Exit per Name; das Spiel greift auf den Übergang namens default zurück, wenn die Stage keinen angibt oder der Name nicht existiert, und spielt den Übergang namens song_loading beim Wechsel ins Spielgeschehen ab.
- Charakter: siehe [Charaktere hinzufügen](guides/characters.md).

## Eine Stage mit Exit verlassen

Nur Stages besitzen das globale Objekt Exit. Es nimmt bis zu drei Argumente und akzeptiert nil an jeder Position: das Ziel ("title", "play", "stage" oder "legacy"; nil bedeutet "title"), den Namen der Ziel-Stage, wenn das Ziel "stage" ist (bzw. einen Legacy-Schlüssel, wenn es "legacy" ist), und den Namen eines Übergangsmoduls. Die mitgelieferten Stages schreiben `return Exit(...)` innerhalb von update, damit in diesem Frame nichts anderes mehr läuft.

```lua
-- aus demo1/Script.lua, innerhalb von update()
if INPUT:KeyboardPressed("S") == true then
    sounds.Skip:Play()
    return Exit("stage", "demo2")   -- Sprung zu Modules/Stages/demo2
end
-- ...
return Exit("title", nil)           -- zurück zum Titelbildschirm
```

## Die Sandbox

Jede Script.lua läuft in einem eingeschränkten Lua-Zustand:

- os behält nur time, date und difftime. Die Sandbox entfernt io, debug, loadfile und dofile, und import tut nichts.
- package schrumpft auf einen eigenen Lader: package.path und package.cpath sind leer und die Sandbox ersetzt die Standard-Searcher, sodass nur die unten genannten Pfade durchsucht werden.
- require sucht zuerst im eigenen Ordner des Moduls, dann im Ordner Modules/Lib des Skins, und lädt die erste gefundene Datei. Eine Moduldatei und eine Lib-Datei mit demselben Namen lösen zur Moduldatei auf. Punkte im Namen werden zu Pfadtrennzeichen, sodass require("DBControllers.dbScores") und require("DBControllers/dbScores") beide DBControllers/dbScores.lua laden. Nicht-ASCII-Pfade funktionieren.

```lua
-- aus intro_nokon/Script.lua
local DBScores  = require("DBControllers/dbScores")  -- der eigene Unterordner des Moduls
local I18N      = require("i18n")                     -- Modules/Lib/i18n.lua
local Opening   = require("opening")                  -- der Modulordner
local Dialogue  = require("nokon_dialogue")           -- der Modulordner
```

## Schreibgeschützte Module

Das Spiel erzeugt ROActivities und Hintergründe mit eingeschränkten globalen Objekten, bevor ihre Script.lua läuft: CONFIG ist eine schreibgeschützte Ansicht, GetSaveFile(player) liefert einen schreibgeschützten Spielstand, DATABASE öffnet schreibgeschützte Speicher, und ACTIVITY ist nil (verwenden Sie ROACTIVITY). Ein Schreibzugriff über eines dieser Objekte protokolliert eine Fehlerbenachrichtigung, tut nichts und löst keinen Lua-Fehler aus. Ein Modul, das Einstellungen, Spielstände oder eine Datenbank ändern muss, muss eine Activity oder eine Stage sein.

## Lokalisierung mit lang/

Der mitgelieferte Skin übersetzt die eigenen Strings jedes Moduls mit der gemeinsamen Bibliothek Modules/Lib/i18n.lua. Der englische String im Code ist der Schlüssel: Das Modul liefert eine lang/ja.lua mit, die eine Tabelle zurückgibt, die jeden englischen String auf seine japanische Übersetzung abbildet, und die Bibliothek schlägt Strings in dieser Tabelle nach.

Die Bibliothek hat drei Funktionen:

- detect() liest die aktuelle Spielsprache über das globale Objekt LANG und lädt das Wörterbuch dafür. Ist die Sprache Japanisch, lädt sie lang/ja per require; der Pfad wird im Modulordner aufgelöst, sodass jedes Modul sein eigenes Wörterbuch hat. Für jede andere Sprache lädt sie nichts. Bis Sie sie aufrufen, ist kein Wörterbuch geladen und jeder String bleibt englisch.
- tr(s) gibt die Übersetzung von s aus dem geladenen Wörterbuch zurück, oder s selbst, wenn das Wörterbuch keinen Eintrag dafür hat oder kein Wörterbuch geladen ist.
- trf(fmt, ...) übersetzt den Formatstring fmt auf dieselbe Weise und formatiert ihn dann mit string.format.

Rufen Sie detect() in activate auf und bauen Sie Ihre Texte danach mit tr und trf. activate läuft bei jedem Betreten des Moduls, sodass eine vom Spieler in den Einstellungen geänderte Sprache beim nächsten Besuch wirksam wird und das Modul keinen weiteren Hook braucht. Schlüssel müssen exakt mit der englischen Quelle übereinstimmen, einschließlich Zeichensetzung, Leerzeichen und Zeilenumbrüchen, und die Übersetzung muss Platzhalter wie %s oder {Player 1 name} unverändert beibehalten.

```lua
-- Modules/Stages/mystage/lang/ja.lua
local T = {}
T["Nokon"] = "ノコン"
T["Alright, quiz time!"] = "さあ、クイズの時間である！"
return T
```

```lua
-- Modules/Stages/mystage/Script.lua
local I18N = require("i18n")
local title

function activate()
    I18N.detect()
    title = I18N.tr("Alright, quiz time!")
end
```

Das Spiel ruft bei einem Sprachwechsel außerdem auf jedem geladenen Modul eine globale Funktion reloadLanguage(lang) auf. Nur ein Modul, das während des Sprachwechsels auf dem Bildschirm bleibt, braucht sie, etwa ein Bildschirm mit einer Sprachauswahl; rufen Sie dort erneut detect() auf und erzeugen Sie die vorgerenderten Texte neu.

## Worauf Sie achten sollten

- Das Spiel gibt die Texturen, Sounds, Videos und Textobjekte, die ein Modul erzeugt hat, frei, wenn es das Modul freigibt, sodass ein Neuladen des Skins sie nicht leckt. Geben Sie Ressourcen, die Sie pro Besuch öffnen, etwa Datenbanken, in deactivate frei, wie es demo3 tut, und geben Sie das Erzeugte in onDestroy frei, wie es demo1 tut.
- GetText speichert auf seinem Textobjekt eine Textur pro unterschiedlichem String zwischen. Ein String, der sich jeden Frame ändert, fügt jeden Frame eine Textur hinzu, und das Spiel wird zunehmend langsamer. Zeichnen Sie sich ändernde Werte mit dem Glyphen-Renderer (TEXT:CreateGlyphCached) oder behalten Sie eine Textur, bis sich der Wert ändert.
- LOADING funktioniert nur in Callbacks, die als Coroutine laufen: onStart jedes Moduls und activate einer Stage. Ein Aufruf von LOADING:Tick aus dem activate einer Activity oder aus update oder draw löst einen Lua-Fehler aus.
- onStart und afterSongEnum laufen, während das Modul nicht auf dem Bildschirm ist. Schreiben Sie sie so, dass sie funktionieren, ohne dass die Stage sichtbar ist.
