<!-- api/activities.md -->

# Module und Lebenszyklus

Die Engine ruft in der Script.lua eines Moduls einen festen Satz von Callbacks auf. Diese Seite listet sie auf, zusammen mit den globalen Objekten, die Activities, Hintergründe und Übergänge steuern, sowie den Counter-, Kamera- und Diagnose-Hilfsobjekten, die jedes Modul erhält. Falls Sie noch kein Modul geschrieben haben, lesen Sie zuerst [So funktionieren Module](../getting-started.md).

## Lebenszyklus-Callbacks

Die Engine schlägt globale Funktionen auf oberster Ebene in der Script.lua jedes Moduls per Name nach und ruft sie an festen Punkten auf. Einen Callback, den Sie nicht definieren, überspringt die Engine. Die Modulart entscheidet, welche Callbacks die Engine aufruft.

<div class="callout warn">
Beim Laden eines Skins erzeugt und startet die Engine die Module in dieser Reihenfolge: Transitions, Stages, Activities, ROActivities. Innerhalb jeder Art führt die Engine zuerst die Script.lua jedes Moduls aus (ihren Code auf oberster Ebene) und ruft dann auf jedem davon onStart auf. Activities und ROActivities sind daher noch nicht geladen, während das onStart einer Stage läuft, und ein Nachschlagen dort liefert nil: Schlagen Sie sie in activate nach. Beim Skin-Wechsel oder beim Beenden läuft onDestroy auf den Stages, dann auf ROActivities und Activities, dann auf den Transitions.
</div>

### Stages, Activities und ROActivities

| Methode | Beschreibung |
| --- | --- |
| `onStart()` | Wird einmal aufgerufen, nachdem die Engine das Modul erzeugt hat, also beim Start und erneut, wann immer die Engine den Skin lädt oder neu lädt. Läuft als Coroutine (siehe LOADING unten); laden Sie hier Assets. |
| `activate(...)` | Stage: wird jedes Mal aufgerufen, wenn die Engine die Stage betritt, als Coroutine. Activity/ROActivity: der Host ruft es über `handle:Activate(...)` mit seinen eigenen Argumenten auf; die Rückgabewerte gehen an den Host zurück. Die Engine aktualisiert die globalen Objekte CHARACTERLIST und PUCHICHARALIST unmittelbar davor. |
| `update(timestamp)` | Wird jeden Frame vor draw aufgerufen; timestamp ist die Spieluhr in Millisekunden. Eine Stage erhält kein update mehr, sobald sie Exit aufgerufen hat; draw läuft bis zum Ende des Ausblendens weiter. Activity/ROActivity: der Host ruft es über `handle:Update()` auf. |
| `draw(...)` | Wird jeden Frame aufgerufen. Activity/ROActivity: der Host ruft es über `handle:Draw(...)` mit seinen eigenen Argumenten auf; die Rückgabewerte gehen an den Host zurück. |
| `deactivate(...)` | Stage: wird aufgerufen, wenn die Engine die Stage verlässt. Activity/ROActivity: der Host ruft es über `handle:Deactivate(...)` auf, oder das Modul ruft es auf sich selbst über `DEACTIVATE()` auf; die Rückgabewerte gehen an den Host zurück. |
| `afterSongEnum()` | Wird jedes Mal aufgerufen, wenn die Song-Enumeration abgeschlossen ist, auch beim Start und nach einem weichen oder harten Neuladen der Songs, selbst wenn das Modul nicht aktiv ist. |
| `onDestroy()` | Wird aufgerufen, bevor die Engine den Skin entlädt, damit das Modul freigeben kann, was es hält. |
| `reloadLanguage(lang)` | Wird auf jedem geladenen Modul aufgerufen, wenn sich die Spielsprache ändert; lang ist der neue Sprachcode. |

### Hintergründe

Jeder Bildschirm hostet seine eigenen Hintergründe. Der Abschnitt Hintergründe weiter unten beschreibt das state-Argument und die Ereignis-Hooks.

| Methode | Beschreibung |
| --- | --- |
| `onStart()` | Wird einmal, synchron, beim ersten Aktivieren des Hintergrunds durch den Host aufgerufen. |
| `activate(state)` | Wird bei jedem Aktivieren des Hintergrunds durch den Host aufgerufen; eine erneute Aktivierung führt onStart nicht noch einmal aus. |
| `update(timestamp, state)` | Wird jeden Frame aufgerufen (nicht während das Spiel pausiert ist); timestamp ist `state.timeStamp` in Millisekunden. |
| `draw(state)` | Wird jeden Frame aufgerufen. |
| `reloadLanguage(lang)` | Wird aufgerufen, wenn sich die Spielsprache ändert. |

Die Engine ruft afterSongEnum und onDestroy auf Hintergründen nicht auf; wenn der Host einen Hintergrund freigibt, gibt die Engine die Ressourcen frei, die er erzeugt hat.

### Übergänge

| Methode | Beschreibung |
| --- | --- |
| `onStart()` | Wird einmal beim Laden des Skins aufgerufen, vor den Stages und Activities, als Coroutine. |
| `fadeOut(t)` | Zeichnet das Ausblenden über die ausgehende Stage; t läuft von 0 bis 1. |
| `loading(progress, elapsed)` | Zeichnet den Ladebildschirm; progress läuft von 0 bis 1, elapsed ist die Zeit in Sekunden seit Beginn des Ladens. |
| `fadeIn(t)` | Zeichnet das Einblenden über die neue Stage; t läuft von 0 bis 1. |
| `onDestroy()` | Wird aufgerufen, bevor die Engine den Skin entlädt, nach den Stages und Activities. |
| `reloadLanguage(lang)` | Wird aufgerufen, wenn sich die Spielsprache ändert. |

Der Abschnitt Übergänge weiter unten beschreibt die zeitliche Abfolge der Phasen.

### Charaktere

Die Script.lua eines Charakters definiert einen anderen Satz: loadAnimation, disposeAnimation, availableAnimation, setAnimationDuration, resetAnimationCounter, update, draw, getDrawSize, getAIBattlePosition, loadVoice, disposeVoice und playVoice. [Charaktere hinzufügen](../guides/characters.md) behandelt ihn.

### Exit

Eine Funktion, die die Engine nur in Stage-Skripten registriert; ihr Aufruf bittet die Engine, die Stage zu verlassen.

<div class="callout warn">
Nur in Skripten unter Modules/Stages verfügbar; Activities und ROActivities steuert ihr Host, und sie erhalten sie nicht. Akzeptiert 0 bis 3 Argumente und toleriert nil an jeder Position. Der Aufruf selbst fordert das Verlassen an; die mitgelieferten Stages schreiben `return Exit(...)` innerhalb von update, damit in diesem Frame nichts anderes mehr läuft. target ist "title", "play", "stage" oder "legacy"; nil oder ein beliebiger anderer Wert bedeutet "title". Ist target "stage", so ist name das Modul unter Modules/Stages, zu dem gesprungen wird; ist target "legacy", so ist name eines von "heya", "config", "exit" oder "onlinelounge" (jeder andere Wert führt zum Titel). transition benennt ein Modul unter Modules/Transitions; lassen Sie es weg oder findet die Engine es nicht, verwendet die Engine das Modul namens "default", und hat der Skin gar keine Übergänge, wird ein einfaches schwarzes Ausblenden abgespielt. Die Namen von Stage und Übergang bevorzugen die Seite des Aufrufers: Eine Stage des Skins findet zuerst das Modul dieses Namens im Skin, eine Stage in Global/Modules das globale, und jede greift auf die andere Seite zurück. Der Schlüssel "heya" bleibt als Alias erhalten: Er öffnet das Modul `myroom` des Skins unter Modules/Stages, oder den Titel, wenn der Skin keines hat.
</div>

| Methode | Beschreibung |
| --- | --- |
| `Exit(target?, name?, transition?)  -> number` | Fordert das Verlassen der Stage in Richtung des angegebenen Ziels an, optional mit Angabe eines Zielmoduls und eines Übergangsmoduls; gibt 0 zurück. |

```lua
function update(timestamp)
    if INPUT:KeyboardPressed("S") then
        return Exit("stage", "mystage")        -- Sprung zu Modules/Stages/mystage
    end
    if INPUT:Pressed("Cancel") then
        return Exit("title", nil, "nokon_curtain")   -- zurück zum Titel über einen benannten Übergang
    end
end
```

### STAGES

Globale für Stage-Namespaces: Eine Stage deklariert einen Namespace, und jedes Modul listet die Stages eines Namespace auf, etwa um ein Menü der dafür gebauten Stages zu erstellen.

<div class="callout warn">
In jedem Modul als globale STAGES registriert, aber nur ein Stage-Skript hat einen Namespace: In einem anderen Modul bewirkt SetNamespace nichts und gibt false zurück. Eine Stage hat jeweils einen Namespace; ein späterer Aufruf ersetzt ihn, und nil oder "" löscht ihn. Der Aufruf funktioniert oben in Script.lua, außerhalb jeder Funktion, ebenso wie in jedem Callback; der Anfang von Script.lua läuft, wenn die Engine die Stage beim Laden des Skins erzeugt, sodass die Stage gelistet wird, bevor irgendein onStart läuft. Die Engine speichert info als einfache Daten, ohne sie zu lesen: Strings, Zahlen, Booleans und Tabellen daraus, bis zu 8 Ebenen tief verschachtelt; Funktionen und Engine-Objekte werden weggelassen. ListNamespace gibt einen Eintrag pro Stage-Name zurück, und zwar die Stage, die Exit("stage", name) aus dem aufrufenden Modul öffnen würde: Ein Skin-Modul erhält die Stage dieses Namens im Skin, ein globales Modul die globale, jede greift auf die andere Seite zurück, und diese Stage wird nur gelistet, wenn sie den Namespace deklariert hat. Die Einträge sind nach Namen sortiert, und jedes info ist eine frische Kopie. Jede Stage existiert, sobald die Stages des Skins erzeugt sind; listen Sie sie also in onStart oder später auf; oben in Script.lua sind nur die vor dem Aufrufer erzeugten Stages da. [Eigene Stages](../modules/custom-stages.md) beschreibt den Namespace CStage, den die Menüs Eigene Stages der Skins auflisten.
</div>

| Methode | Beschreibung |
| --- | --- |
| `STAGES:SetNamespace(namespace, info?)  -> boolean` | Deklariert den Namespace der aufrufenden Stage, mit einer optionalen info-Tabelle, die an die auflistenden Stages zurückgegeben wird; false außerhalb eines Stage-Skripts. |
| `STAGES:SetResolution(width, height)  -> boolean` | Legt die Auflösung fest, in der die aufrufende Stage zeichnet. Weicht die des Skins ab, wird alles, was die Stage zeichnet, auf die des Skins skaliert, zentriert und mit gleichem Seitenverhältnis, wie ein ganzer Skin dieser Auflösung; Mauskoordinaten, `INPUT:GetSurfaceWidth/Height` und Clips folgen, und eine ROActivity des Skins, die sie öffnet, behält die Auflösung des Skins. 0 hebt sie auf; false außerhalb eines Stage-Skripts. |
| `STAGES:ListNamespace(namespace)  -> table` | Die Stages des Namespace als Array von `{ name, isGlobal, info }`-Tabellen, ab 1 indiziert: der Ordnername, true für eine Stage in Global/Modules/Stages, und das deklarierte info (eine leere Tabelle, wenn keines angegeben wurde). |

```lua
-- Modules/Stages/color_tool/Script.lua, at the top
STAGES:SetNamespace("MyTools", { title = { en = "Colour picker", fr = "Pipette" } })
STAGES:SetResolution(1920, 1080)

-- another stage lists them
function activate()
    for _, s in ipairs(STAGES:ListNamespace("MyTools")) do
        debugLog(s.name .. (s.isGlobal and " (global)" or ""))
    end
end
```

### LOADING

Ladebalken-Hilfsobjekt für die Callbacks, die als Coroutine laufen: onStart jeder Modulart und activate einer Stage.

<div class="callout warn">
In jedem Modul als globales Objekt LOADING definiert. Diese Callbacks laufen auf einer von der Engine verwalteten Coroutine, die die Engine jeden Frame fortsetzt: Die Engine unterbricht automatisch, sobald eine Fortsetzung ihr Zeitbudget verbraucht hat, und Sie können auch selbst mit coroutine.yield(progress) oder LOADING:Tick(sub) unterbrechen. Blöcke, die Sie mit LOADING:Add registrieren, laufen der Reihe nach, nachdem der Rumpf des Callbacks zurückgekehrt ist, und der Balken rückt nach jedem Block vor; die Gewichtung eines Blocks ist sein Anteil am Balken (Standard 1). LOADING:Tick(sub) unterbricht innerhalb eines Blocks für einen Frame und meldet einen Anteil von 0 bis 1 innerhalb dieses Blocks. Außerhalb eines Coroutine-Callbacks (activate einer Activity oder ROActivity oder irgendein update oder draw) löst LOADING:Tick einen Lua-Fehler aus, weil es nichts gibt, an das unterbrochen werden könnte, und mit LOADING:Add eingereihte Blöcke laufen nie.
</div>

| Methode | Beschreibung |
| --- | --- |
| `LOADING:Add(fn)  -> nil` | Registriert einen Ladeblock, der nach Rückkehr des Callbacks läuft. |
| `LOADING:Add(label, fn)  -> nil` | Registriert einen beschrifteten Ladeblock. |
| `LOADING:Add(label, weight, fn)  -> nil` | Registriert einen beschrifteten Ladeblock mit expliziter Gewichtung. |
| `LOADING:Tick(sub)  -> nil` | Unterbricht innerhalb eines Blocks für einen Frame und meldet einen Teilfortschritt von 0 bis 1 innerhalb des aktuellen Blocks. |

```lua
function onStart()
    LOADING:Add("textures", 3, function()
        for i, name in ipairs(names) do
            tx[name] = TEXTURE:CreateTexture(name)
            LOADING:Tick(i / #names)
        end
    end)
    LOADING:Add("sounds", 1, function()
        bgm = SOUND:CreateBGM("Sounds/BGM.ogg")
    end)
end
```

## Activities

### ACTIVITY

Globales Objekt zum Nachschlagen einer geladenen Activity per Name.

<div class="callout warn">
In jedem Modul als globales Objekt ACTIVITY registriert, außer in ROActivities und Hintergründen, wo es nil ist; diese Module verwenden ROACTIVITY. Die Engine lädt Activities aus Modules/Activities/{name}. ACTIVITY stellt außerdem GetROActivity bereit, das sich wie ROACTIVITY:GetROActivity verhält. Eine Suche bevorzugt die Seite des Aufrufers: Ein Modul des Skins erhält die Activity dieses Namens aus dem Skin, ein Modul in Global/Modules die aus Global/Modules/Activities, und jede Seite greift auf die andere zurück, wenn sie keine hat. Eine Lib-Datei sucht von der Seite des Moduls aus, das sie per require geladen hat.
</div>

| Methode | Beschreibung |
| --- | --- |
| `ACTIVITY:GetActivity(name)  -> activity handle` | Gibt das Handle der geladenen Activity mit dem angegebenen Ordnernamen zurück, oder nil, wenn keine geladen ist. |
| `ACTIVITY:GetROActivity(name)  -> activity handle` | Wie ROACTIVITY:GetROActivity. |

### ROACTIVITY

Globales Objekt zum Nachschlagen einer geladenen schreibgeschützten Activity (ROActivity) per Name.

<div class="callout warn">
In jedem Modul als globales Objekt ROACTIVITY registriert. Die Engine lädt ROActivities aus Modules/ROActivities/{name} und gibt ihnen die schreibgeschützten globalen Objekte CONFIG, DATABASE und GetSaveFile, sodass ihre Skripte den Spielzustand nicht ändern können (siehe den Abschnitt zu schreibgeschützten Modulen unter So funktionieren Module). Activities und ROActivities sind per Name identifizierte Singletons: eine Instanz pro Ordner, die jeder Host gemeinsam nutzt. GetROActivity bevorzugt die Seite des Aufrufers so, wie es ACTIVITY:GetActivity tut, zwischen Modules/ROActivities und Global/Modules/ROActivities.
</div>

| Methode | Beschreibung |
| --- | --- |
| `ROACTIVITY:GetROActivity(name)  -> activity handle` | Gibt das Handle der geladenen ROActivity mit dem angegebenen Ordnernamen zurück, oder nil, wenn keine geladen ist. |

### Activity-Handle

Das Objekt, das ACTIVITY:GetActivity und ROACTIVITY:GetROActivity zurückgeben. Ein Host verwendet es, um die Callbacks des Moduls zu steuern.

<div class="callout warn">
Activate, Deactivate und Draw leiten ihre Argumente an die Callbacks activate, deactivate und draw des Moduls weiter. Update ruft update mit der aktuellen Spielzeit in Millisekunden auf. Jede dieser Methoden gibt die vom Callback zurückgegebenen Werte als ab 0 indiziertes Array zurück, oder nil, wenn der Callback nichts zurückgegeben hat oder nicht definiert ist; lesen Sie den ersten Wert mit `result[0]`. Call ruft eine beliebige globale Funktion auf, die das Skript des Moduls definiert.
</div>

| Methode | Beschreibung |
| --- | --- |
| `handle.IsActive  -> boolean` | True, nachdem Activate gelaufen ist und bis Deactivate (oder das eigene DEACTIVATE() des Moduls) gelaufen ist. |
| `handle:Activate(...)  -> array` | Ruft den activate-Callback des Moduls mit den angegebenen Argumenten auf. |
| `handle:Deactivate(...)  -> array` | Ruft den deactivate-Callback des Moduls mit den angegebenen Argumenten auf. |
| `handle:Update()  -> array` | Ruft den update-Callback des Moduls mit der aktuellen Spielzeit in Millisekunden auf. |
| `handle:Draw(...)  -> array` | Ruft den draw-Callback des Moduls mit den angegebenen Argumenten auf. |
| `handle:Call(functionName, ...)  -> array` | Ruft die benannte globale Funktion des Modulskripts mit den angegebenen Argumenten auf. |

```lua
local act = nil

function activate()
    if act == nil then act = ACTIVITY:GetActivity("song_select_core") end
    act:Activate()
end

function update(timestamp)
    local result = act:Update()
    local signal = result ~= nil and result[0] or nil
    if signal == "play" then return Exit("play", nil) end
    if signal == "cancel" then return Exit("title", nil) end
end

function draw()
    act:Draw()
end

function deactivate()
    act:Deactivate()
end
```

### DEACTIVATE

Funktion, die die Engine in Activity- und ROActivity-Skripten registriert; sie erlaubt dem Modul, sich selbst zu deaktivieren.

<div class="callout warn">
Der Aufruf markiert das Modul als inaktiv (handle.IsActive wird false) und führt den eigenen deactivate-Callback des Moduls aus. Die mitgelieferten Dialoge rufen sie auf, wenn der Spieler bestätigt oder abbricht, und der Host beobachtet IsActive, um zu erfahren, dass der Dialog geschlossen wurde.
</div>

| Methode | Beschreibung |
| --- | --- |
| `DEACTIVATE(...)` | Deaktiviert das aktuelle Modul und ruft seinen deactivate-Callback mit den angegebenen Argumenten auf. |

### Von der Engine gehostete ROActivities

Die Engine schlägt einige ROActivities unter festem Ordnernamen nach und steuert sie selbst. Ein Skin ersetzt eine davon, indem er einen Ordner unter Modules/ROActivities mit diesem Namen mitliefert; die unten aufgeführten Aufrufstellen der Engine legen die Callbacks fest, die er definieren muss. Fehlt einer, zeichnet die Engine die entsprechende Funktion nicht.

| Name | Aufrufe der Engine |
| --- | --- |
| `nameplate` | `activate(player, name, title, dan, data)`, wenn sich das Namensschild eines Spielers ändert; `update()` einmal pro Frame; `draw(mode, ...)` mit mode 0 = vollständiges Namensschild `(x, y, opacity, player, side)`, 1 = Dan-Schild `(x, y, opacity, danGrade, textTexture)`, 2 = Titelschild `(x, y, opacity, type, textTexture, rarity, nameplateId)`. |
| `modal` | `activate(player, rarity, modalType, ...)` für jedes eingereihte Freischalt-Modal, dann `update()` und `draw()` jeden Frame. Das Skript ruft DEACTIVATE() auf, um das Modal zu schließen; die Engine aktiviert dann das nächste. |
| `modicons` | `activate()` einmal, dann `draw(x, y, player, layout, alpha)` mit layout "menu" oder "game". Das globale Objekt MODICONS kapselt dies. |
| `danplate` | `draw(x, y, opacity, danTick, r, g, b, titleText)` auf dem Ergebnisbildschirm und in Dan-Kursen. |
| `results_tower` | `activate(info)` beim Öffnen des Tower-Ergebnisses, mit dem Spiel als LuaResultInfo (schreibgeschützte Getter wie `ReachedFloor`, `TotalFloors`, `Lives`, `TowerRank`, `IsNewBestFloor`, `GetScore(player)`); `update()` und `draw()` in jedem Frame; `skip()` über Call beim Bestätigen, gibt true zurück, solange es noch animiert (diese Bestätigung überspringt dann nur); `deactivate()`. |
| `results_regular` | `activate(info)` beim Öffnen eines Ergebnisses, auch eines KI-Kampfs, mit dem Spiel als LuaResultInfo (Getter je Spieler ab 0 wie `GetGauge(p)`, `GetClearType(p)`, `GetScoreRank(p)`, `GetScore(p)`, sowie `IsAIBattle`, `GetAISectionEnd(i)`); `update()` und `draw()` in jedem Frame über dem Ergebnis-Hintergrund; `skip()` über Call beim Bestätigen, gibt true zurück, solange es noch animiert (der Hintergrund erhält dann `skipAnime`); `deactivate()`. |
| `results_dan` | `activate(info)` beim Öffnen des Ergebnisses eines Dan-Spiels, mit dem Spiel als LuaResultInfo (`DanResult`, `DanCombo`, `GetDanSong(i)`, `GetExam(i)`, `exam:GetSong(i)`, `exam.PreviousBestAmount`, `DanGradeChange`, `DanGrade`); `update()` und `draw()` in jedem Frame über dem Dan-Ergebnis-Hintergrund; `skip()` über Call beim Bestätigen, gibt true zurück, solange es noch animiert; danach `advance()` über Call beim Bestätigen, gibt true zurück, wenn es vor den Belohnungs-Pop-ups noch einen Bildschirm zeigt (die Engine hat einen neuen oder besseren Dan-Rang schon auf das Namensschild gesetzt, also zeichnet das Modul `PreviousNameplateDan`, bis es ihn enthüllt); `swap()` über Call bei links oder rechts, gibt true zurück, wenn es zwischen seinen Bildschirmen wechselt; `deactivate()`. |
| `tower_hud` | `activate(maxFloor)` beim Start eines Tower-Spiels; `update()` und `draw()` in jedem Frame über dem oberen Hintergrund; `deactivate()`. Es liest das Stockwerk und die Leben aus PLAYSTATE. |
| `popup_menu` | `activate(title, items, fontSize, ...)`, wobei items die durch Zeilenumbrüche verbundenen Beschriftungen sind, gefolgt von den PopupMenu-Positionen des Skins; `draw(selected)` jeden Frame; `deactivate()` beim Schließen. |
| `config_ui` | `activate(model)` mit dem Einstellungsmodell; `update()` jeden Frame, gibt "exit" zurück, um den Einstellungsbildschirm zu verlassen, optional gefolgt vom Namen des Modules/Transitions-Moduls für den Rückweg (stattdessen spielt die einfache Blende, wenn ein Wechsel von Skin, Auflösung oder Audiogerät noch angewendet werden muss); `draw()`; `reload(model)` über Call, wenn die Engine das Modell neu aufbaut; `deactivate()`. |
| `song_enum` | `activate()`, dann `draw(isCommandSongDataGet, done, total)` jeden Frame, während der Song-Scan läuft; `deactivate()`. |
| `cutscene` | `activate(paths, fadeOutSeconds)` mit den Videodateien in Reihenfolge und einem Ausblenden nach Schwarz nach der letzten (0 für keines); `update()` jeden Frame, gibt "finished" zurück, sobald jedes Video gespielt hat und das Ausblenden vorbei ist; `draw()`; `deactivate()`. Spielt jede Datei mit ihrer eigenen Tonspur und besitzt das Pause-Popup; wofür die Cutscene dient, ist Sache des Aufrufers. |

Die drei results-ROActivities spielen die Musik des Ergebnisbildschirms selbst. Während ein Belohnungs-Pop-up angezeigt wird, dämpft die Engine sie über Call mit `setMusicLevel(percent)`, von 0 bis 100. Sobald der Bildschirm zu Schwarz ausgeblendet ist, deaktiviert die Engine das Modul, und seine Musik endet damit.

### Ergebnisinfo

Das LuaResultInfo-Objekt, das die Ergebnisstufe an den activate-Callback von results_tower, results_regular und results_dan übergibt.

<div class="callout warn">
Die Ergebnisstufe füllt es, nachdem das Spiel gespeichert wurde, und danach ändert es sich nicht mehr. Jedes Mitglied ist schreibgeschützt: Eigenschaften verwenden die Punkt-Syntax (`info.Title`), Methoden die Doppelpunkt-Syntax. Spielerargumente beginnen bei 0; außerhalb des Bereichs gibt eine Methode 0, false oder einen leeren String zurück. Ein mit einem Modulnamen markiertes Mitglied wird nur für diesen Bildschirm gefüllt; auf den anderen behält es diesen Standardwert.
</div>

| Methode | Beschreibung |
| --- | --- |
| `info.Title  -> string` | Der Titel des Charts. |
| `info.Subtitle  -> string` | Der Untertitel des Charts. |
| `info.PlayerCount  -> number` | Anzahl der Spieler (1 bei den Turm- und Dan-Ergebnissen). |
| `info.PlayerSide  -> number` | Die Namensschild-Seite von Spieler 1: 1, wenn er auf der blauen Seite spielt, sonst 0. |
| `info:GetGoodCount(player)  -> number` | Good-Treffer. |
| `info:GetOkCount(player)  -> number` | Ok-Treffer. |
| `info:GetBadCount(player)  -> number` | Bad-Treffer. |
| `info:GetRollCount(player)  -> number` | Wirbel-Treffer. |
| `info:GetMaxCombo(player)  -> number` | Höchste Combo. |
| `info:GetADLibCount(player)  -> number` | Getroffene AD-LIB-Noten. |
| `info:GetBoomCount(player)  -> number` | Getroffene Bomben. |
| `info:GetScore(player)  -> number` | Endpunktzahl. |
| `info:GetPreviousBestScore(player)  -> number` | Die beste Punktzahl des Spielers auf diesem Chart und Schwierigkeitsgrad, bevor dieses Spiel gespeichert wurde, 0 wenn keine. |
| `info:IsSaved(player)  -> boolean` | True, wenn das Spiel in den Spielstand des Spielers eingegangen ist (nie mit Auto oder bei einer angesehenen Wiederholung). |
| `info:GetDifficulty(player)  -> number` | results_regular: der gespielte Schwierigkeitsgrad, 0 Easy bis 4 Extra. |
| `info:GetGauge(player)  -> number` | results_regular: die Seelenanzeige am Ende des Spiels, 0 bis 100. |
| `info:GetGaugeType(player)  -> number` | results_regular: 0 normal, 1 hart, 2 extrem. |
| `info:GetNorma(player)  -> number` | results_regular: der Anzeigewert, ab dem eine normale Anzeige als geschafft gilt. |
| `info:GetKillZone(player)  -> number` | results_regular: die Todeszone der extremen Anzeige, 0 bis 100 (0 bei den anderen Anzeigen). |
| `info:GetClearType(player)  -> number` | results_regular: 0 nicht geschafft, 1 geschafft mit Hilfe, 2 geschafft, 3 Full Combo, 4 perfekt. |
| `info:GetScoreRank(player)  -> number` | results_regular: 0 keiner, dann die Punkteränge von 1 bis 7. |
| `info:IsCleared(player)  -> boolean` | results_regular: true, wenn das Spiel geschafft wurde; in einem KI-Kampf, wenn Spieler 1 gewonnen hat. |
| `info:GetSpeechText(player, mood)  -> string` | results_regular: der Ergebnissatz der Figur des Spielers für eine Stimmung, 0 (schlechteste) bis 5 (perfekt). |
| `info.IsAIBattle  -> boolean` | results_regular: true bei einem KI-Kampf. |
| `info.AISectionCount  -> number` | results_regular: die Abschnitte des KI-Kampfs. |
| `info:GetAISectionEnd(section)  -> number` | results_regular: wie ein Abschnitt (ab 0) endete: 0 nicht zu Ende gespielt, 1 gewonnen, 2 verloren. |
| `info.TowerType  -> string` | results_tower: der TOWERTYPE des Charts, "" wenn er keinen nennt. |
| `info.TowerSide  -> number` | results_tower: 0 Sweet, 1 Spicy. |
| `info.ReachedFloor  -> number` | results_tower: das erreichte Stockwerk. |
| `info.TotalFloors  -> number` | results_tower: die Stockwerke des Turms. |
| `info.Lives  -> number` | results_tower: die verbleibenden Leben. |
| `info.MaxLives  -> number` | results_tower: die Leben zu Beginn. |
| `info.TowerRank  -> number` | results_tower: 0 keiner, dann 1 bis 7: 10, 25, 50 und 75 %, geschafft, Full Combo, perfekt. |
| `info.PreviousBestFloor  -> number` | results_tower: das beste Stockwerk, bevor dieses Spiel gespeichert wurde. |
| `info.IsNewBestFloor  -> boolean` | results_tower: true, wenn ein gespeichertes Spiel das beste Stockwerk übertroffen hat. |
| `info.IsNewBestScore  -> boolean` | results_tower: true, wenn ein gespeichertes Spiel die beste Punktzahl übertroffen hat. |
| `info.IsTired  -> boolean` | results_tower: true, wenn das Spiel mit erschöpfter Turmfigur endete (wenige Leben übrig). |
| `info.Bpm  -> number` | results_tower: die BPM, mit der das Spiel endete. |
| `info.DanTick  -> number` | results_dan: die Striche des Dan-Schilds (DANTICK). |
| `info.DanTickColor  -> color` | results_dan: die Farbe der Striche (DANTICKCOLOR). |
| `info.DanResult  -> number` | results_dan: 0 nicht bestanden, 1 bestanden (rot), 2 bestanden (gold). |
| `info.DanCombo  -> number` | results_dan: 0, 1 kein Bad (Full Combo), 2 kein Bad und kein Ok (perfekt). |
| `info.TotalHits  -> number` | results_dan: Good-, Ok- und Wirbel-Treffer zusammen. |
| `info.DanSongCount  -> number` | results_dan: die Songs des Dans. |
| `info.DanSongReached  -> number` | results_dan: der Song, mit dem das Spiel endete, ab 0. |
| `info.DanExamSong  -> number` | results_dan: der Song, dessen Prüfungen pro Song zuletzt angezeigt wurden, ab 0. |
| `info:GetDanSong(song)  -> dan song` | results_dan: Song i des Dans (ab 0) oder nil. |
| `info.ExamCount  -> number` | results_dan: die Prüfungen des Dans. |
| `info:GetExam(exam)  -> dan exam` | results_dan: Prüfung i (ab 0, in der Reihenfolge des Charts) oder nil. |
| `info.DanGradeChange  -> number` | results_dan: 0 keine, 1 ein neuer Dan-Rang, 2 ein besserer Rang in einem schon bestandenen Dan (Gold oder ein besseres Clear). Die Engine setzt einen neuen oder besseren Rang auf das Namensschild des Spielers. |
| `info.DanGrade  -> dan grade` | results_dan: der Rang des Dans im Spielstand nach diesem Spiel; sein Title ist "", wenn der Dan nie bestanden wurde. |
| `info.PreviousNameplateDan  -> dan grade` | results_dan: der Dan-Rang, den das Namensschild vor diesem Spiel zeigte (Title "" für keinen). |

### Dan-Song

Das Objekt, das info:GetDanSong zurückgibt, schreibgeschützt.

| Methode | Beschreibung |
| --- | --- |
| `song.Title  -> string` | Der Titel des Songs; "???" für einen Song, den der Chart verborgen hält. |
| `song.Subtitle  -> string` | Der Untertitel des Songs; "" für einen Song, den der Chart verborgen hält. |
| `song.Difficulty  -> number` | 0 Easy bis 4 Extra. |
| `song.Level  -> number` | Das Level des Songs. |
| `song.Good  -> number` | Good-Treffer im Song. |
| `song.Ok  -> number` | Ok-Treffer im Song. |
| `song.Bad  -> number` | Bad-Treffer im Song. |
| `song.Roll  -> number` | Wirbel-Treffer im Song. |

### Dan-Prüfung

Das Objekt, das info:GetExam und exam:GetSong zurückgeben, schreibgeschützt: eine Prüfung, wie das Spiel endete.

| Methode | Beschreibung |
| --- | --- |
| `exam.Type  -> number` | 0 Anzeige, 1 Good, 2 Ok, 3 Bad, 4 Punktzahl, 5 Wirbel, 6 Treffer, 7 Combo, 8 Genauigkeit, 9 AD-LIB, 10 BOOM; LANG:GetExamName(type) liefert den Namen. |
| `exam.Range  -> number` | 0 mindestens Value, 1 weniger als Value. |
| `exam.Value  -> number` | Der Wert zum Bestehen (rot). |
| `exam.Amount  -> number` | Der angezeigte Wert; bei einer "weniger als"-Prüfung, was bis zum Nichtbestehen übrig war. |
| `exam.PreviousBestAmount  -> number` | Der beste Amount des Spielers bei dieser Prüfung, bevor dieses Spiel gespeichert wurde, im selben Maß (größer ist besser); -1, wenn keiner. Bei einer Prüfung pro Song hat jeder Song (exam:GetSong) seinen eigenen. |
| `exam.Percent  -> number` | Amount im Verhältnis zu Value, 0 bis 100. |
| `exam.Status  -> number` | 0 nicht bestanden, 1 bestanden, 2 in Gold bestanden. |
| `exam.IsPerSong  -> boolean` | True bei einer Prüfung, die Song für Song bewertet wird; die Prüfung selbst ist dann die des letzten Songs. |
| `exam:GetSong(song)  -> dan exam` | Die Prüfung pro Song in Song i (ab 0) oder nil. |

### Dan-Rang

Das Objekt, das info.DanGrade und info.PreviousNameplateDan zurückgeben, schreibgeschützt.

| Methode | Beschreibung |
| --- | --- |
| `grade.Title  -> string` | Der Titel des Dans, wie ihn das Namensschild zeigt (die ersten zwei Zeichen des Chart-Titels); "" für keinen. |
| `grade.Gold  -> boolean` | True bei einem goldenen Bestehen. |
| `grade.Tier  -> number` | 0 Clear, 1 Full Combo, 2 Perfect. |

## Hintergründe

### Hintergrundmodul

Eine Script.lua, die einen Bildschirmhintergrund, eine Gameplay-Ebene, einen Mob, eine Clear-Animation oder einen Kusudama-Effekt zeichnet, gehostet von den eigenen Bildschirmen der Engine.

<div class="callout warn">
Hintergründe liegen außerhalb des Modules-Ordners, unter dem Graphics-Ordner des Skins im Verzeichnis des Bildschirms, den sie dekorieren, zum Beispiel Graphics/0_Startup/Script.lua, Graphics/6_Result/Script.lua, Graphics/5_Game/5_Background/Normal/Up/{variant}/Script.lua, Graphics/5_Game/5_Background/Normal/Down/{variant}/Script.lua, Graphics/5_Game/3_Mob/{variant}/Script.lua, Graphics/5_Game/9_End/{result}/Script.lua und Graphics/5_Game/11_Balloon/Kusudama/Script.lua. Wo ein Ordner mehrere Varianten enthält, wählt die Engine bei jedem Spiel eine zufällig (oder aus dem Szenen-Preset des Charts) aus. Der Host-Bildschirm erzeugt eine Hintergrundinstanz (die Gameplay-Hintergründe bei jedem Betreten des Spielbildschirms durch die Engine) und gibt sie mit dem Bildschirm frei, sodass während des Spiels mehrere gleichzeitig aktiv sind. Ein Hintergrundskript erhält dieselben globalen Objekte wie eine ROActivity (schreibgeschützte CONFIG, DATABASE und GetSaveFile; kein ACTIVITY). Die Ereignis-Hooks unten sind optional, und die Engine ruft jeden einmal auf, wenn sein Ereignis eintritt. Beim Tower benennt das `TOWERTYPE` des Charts den Down-Ordner.
</div>

| Methode | Beschreibung |
| --- | --- |
| `clearIn(player)` | Gameplay-Hintergründe Up und Down: Die Gauge des Spielers hat die Clear-Zone erreicht. |
| `clearOut(player)` | Gameplay-Hintergründe Up und Down: Die Gauge des Spielers ist aus der Clear-Zone gefallen. |
| `playEndAnime(player)` | Clear-Animationen (Graphics/5_Game/9_End): Die Endanimation für den Spieler beginnt. |
| `kusuIn()` / `kusuBroke()` / `kusuMiss()` | Kusudama: Der Ballon erscheint, der Spieler zerbricht ihn oder der Spieler verfehlt ihn. |
| `skipAnime()` | Ergebnishintergrund: Der Spieler hat die Ergebnisanimation übersprungen. |

### Hintergrundzustand

Das Objekt, das der Host an activate, update und draw eines Hintergrunds übergibt.

<div class="callout warn">
Eine Instanz pro Host, die der Host jeden Frame an Ort und Stelle aktualisiert. Die Array-Felder teilen sich die Pro-Spieler-Arrays der Engine und sind ab 0 indiziert (`state.gauge[0]` ist Spieler 1). Nur Gameplay-Hosts aktualisieren die Gameplay-Felder; andere Hosts belassen sie bei ihren Standardwerten, und timeStamp bleibt außerhalb des Spiels bei -1. Der Zustand enthält kein Frame-Timing: Lesen Sie das globale Objekt fps.
</div>

| Methode | Beschreibung |
| --- | --- |
| `state.playerCount  -> number` | Anzahl der Spieler. |
| `state.p1IsBlue  -> boolean` | True, wenn Spieler 1 die blaue Seite verwendet. |
| `state.lang  -> string` | Aktueller Sprachcode. |
| `state.simplemode  -> boolean` | True, wenn Simple Mode aktiv ist. |
| `state.puchicharaRarities  -> string[]` | Seltenheit des Puchicharas jedes Spielers. |
| `state.characterRarities  -> string[]` | Seltenheit des Charakters jedes Spielers. |
| `state.isClear  -> boolean[]` | Ob sich jeder Spieler gerade in der Clear-Zone befindet. In einem Dan mit normaler Gauge wird es beim ersten Treffer wahr und falsch, sobald das Spiel oder eine Prüfung scheitert. |
| `state.gauge  -> number[]` | Gauge-Wert jedes Spielers. |
| `state.bpm  -> number[]` | Aktuelle BPM jedes Spielers. |
| `state.gogo  -> boolean[]` | Ob sich jeder Spieler in der Go-Go-Time befindet. |
| `state.towerNightNum  -> number` | Tag-zu-Nacht-Faktor des Tower-Modus von 0 bis 1. |
| `state.towerFloor  -> number` | Gerade gespieltes Stockwerk, ab 0 (Tower-Gameplay). |
| `state.towerMaxFloor  -> number` | Anzahl der Stockwerke des Charts (Tower-Gameplay). |
| `state.battleState  -> number` | Zustandscode des KI-Kampfs. |
| `state.battleWin  -> boolean` | True, wenn der Spieler den KI-Kampf gerade gewinnt. |
| `state.timeStamp  -> number` | Chart-synchrone Zeit in Sekunden; -1 außerhalb des Spiels. |
| `state.paused  -> boolean` | True, während das Spiel pausiert ist. |
| `state.player  -> number` | Der Spieler, für den ein Pro-Spieler-Host (Clear-Animationen) zeichnet. |
| `state.hitRing  -> number` | Anzahl der letzten Treffer, die hitNote, hitBig und hitJudge aufbewahren (32). |
| `state.hitCount  -> number[]` | Treffer jedes Spielers seit dem Start des Hintergrunds: gewertete Noten (Miss eingeschlossen), Trommelwirbel-Treffer und geplatzte Ballons. Der n-te Treffer, ab 0 gezählt, steht an Index n % hitRing von hitNote, hitBig und hitJudge. |
| `state.hitNote  -> string[][]` | Note jedes aufbewahrten Treffers, pro Spieler: "don", "ka" oder "kadon". |
| `state.hitBig  -> boolean[][]` | True, wenn der aufbewahrte Treffer eine große Note war, pro Spieler. |
| `state.hitJudge  -> string[][]` | Wertung jedes aufbewahrten Treffers, pro Spieler: "perfect", "great", "good", "poor", "miss", "roll" oder "balloon". |
| `state.landCount  -> number[]` | Fliegende Noten jedes Spielers, die seit dem Start des Hintergrunds das Ende ihres Flugs erreicht haben. |
| `state:SetFlyTarget(player, x, y)` | Solange dieser Gameplay-Hintergrund angezeigt wird, landen die fliegenden Noten des Spielers auf (x, y) in Bildschirmpixeln, und der Landeeffekt des Skins entfällt. |
| `state:ClearFlyTarget(player)` | Die fliegenden Noten des Spielers landen wieder an der üblichen Stelle des Skins. |

## Übergänge

### Übergangsmodul

Eine Modules/Transitions/{name}/Script.lua, die die Phasen Ausblenden, Laden und Einblenden zwischen zwei Stages zeichnet.

<div class="callout warn">
Das dritte Argument von Exit wählt den Übergang aus; die Engine verwendet "default", wenn der Aufruf keinen benennt oder die Engine den Namen nicht findet. Das Laden ins Spielgeschehen nach Exit("play") verwendet immer den Übergang namens "song_loading", oder "default", wenn der Skin keinen hat. Die Engine steuert die Phasen der Reihe nach: Sie ruft fadeOut(t) jeden Frame über der ausgehenden Stage auf, bis t 1 erreicht; dann entfernt sie die ausgehende Stage und lädt die neue, während sie loading(progress, elapsed) jeden Frame aufruft; dann ruft sie fadeIn(t) über der neuen Stage auf, bis t 1 erreicht. Jedes Ein- und Ausblenden dauert 0,5 Sekunden, sofern das Skript nicht FADE_OUT_SECONDS oder FADE_IN_SECONDS setzt; einen Wert, der keine positive Zahl ist, ignoriert die Engine. Bei einem Stage-Wechsel ruft die Engine loading erst auf, wenn das Laden länger als 0,5 Sekunden gedauert hat; davor ruft sie fadeOut(1) auf, damit kurze Ladevorgänge keinen Ladebildschirm aufblitzen lassen. Der Song-Ladepfad zeigt die Ladephase sofort. Eine BGM, die die neue Stage während des Ladens oder des Einblendens startet, wartet und spielt, wenn das Einblenden endet, außer beim Laden ins Spielgeschehen; die eigenen Sounds des Übergangs spielen sofort. Während des Einblendens erhält die neue Stage keine Eingaben, außer wenn das Einblenden das Spielgeschehen zeigt. Bricht der Spieler das Laden ins Gameplay ab, kehrt die Engine zur Songauswahl zurück und blendet sie mit fadeIn(t) ein; jede Transition endet also mit einem Aufruf von fadeIn(1).
</div>

| Methode | Beschreibung |
| --- | --- |
| `FADE_OUT_SECONDS  -> number` | Optionale globale Variable auf oberster Ebene: Dauer des Ausblendens in Sekunden. |
| `FADE_IN_SECONDS  -> number` | Optionale globale Variable auf oberster Ebene: Dauer des Einblendens in Sekunden. |

```lua
FADE_OUT_SECONDS = 0.3
FADE_IN_SECONDS = 0.3

local pixel = nil

local function cover(alpha)
    pixel:SetColor(0, 0, 0)
    pixel:SetOpacity(alpha)
    pixel:SetScale(8000, 8000)
    pixel:Draw(0, 0)
end

function onStart()
    pixel = TEXTURE:CreateTexture("pixel.png")
end

function fadeOut(t) cover(t) end
function fadeIn(t) cover(1.0 - t) end
function loading(progress, elapsed) cover(1.0) end

function onDestroy()
    if pixel ~= nil then pixel:Dispose() end
end
```

## Timing und Kamera

### COUNTER

Fabrik für Animations-Counter, die einen Wert über die Zeit von einem Anfangs- zu einem Endwert bewegen.

<div class="callout warn">
Als globales Objekt COUNTER registriert. Ein Counter rückt nur vor, wenn Sie Tick aufrufen, und zwar um das Frame-Delta geteilt durch interval, wobei interval die Anzahl der Sekunden pro Werteinheit ist. Das Vorzeichen von interval muss zur Richtung passen: positiv, wenn end größer als begin ist, negativ, wenn es kleiner ist. Passen die Vorzeichen nicht, vertauscht der Counter die Enden und endet beim ersten Tick. CreateCounterDuration nimmt die Gesamtdauer und wählt das Vorzeichen selbst. Ein interval von null oder gleiche Werte für begin und end beenden den Counter beim ersten Tick. Der Counter ruft die optionale Funktion ended auf, wenn der Wert das Ende erreicht, und beim Schleifen oder Pendeln einmal pro abgeschlossenem Zyklus.
</div>

| Methode | Beschreibung |
| --- | --- |
| `COUNTER:CreateCounter(begin, end, interval, ended?)  -> counter` | Erzeugt einen Counter, der sich mit interval Sekunden pro Einheit von begin nach end bewegt und bei Abschluss ended aufruft. |
| `COUNTER:CreateCounterDuration(begin, end, seconds, ended?)  -> counter` | Erzeugt einen Counter, der sich über die angegebene Anzahl Sekunden von begin nach end bewegt; gibt einen leeren Counter zurück, wenn seconds nicht positiv ist oder begin gleich end ist. |
| `COUNTER:EmptyCounter()  -> counter` | Erzeugt einen inaktiven Counter, dessen Wert bei 0 bleibt, als Platzhalter verwendbar. |

### Counter-Handle

Ein von COUNTER erzeugter Counter.

<div class="callout warn">
Lesen Sie Value jeden Frame und rufen Sie Tick jeden Frame auf, um ihn vorzurücken; ein Counter, den Sie nicht gestartet haben oder der gestoppt ist, ignoriert Tick. Begin, End und Interval sind les- und schreibbare Felder. SetLoop und SetBounce schließen einander aus. SetEasing formt nur den gemeldeten Value; darunter rückt der Counter weiterhin linear vor. Listener erhalten bei jedem Tick den aktuellen Wert, einschließlich des letzten.
</div>

| Methode | Beschreibung |
| --- | --- |
| `counter.Value  -> number` | Der aktuelle Wert, mit Easing, falls gesetzt; eine Zuweisung lässt den Counter springen. |
| `counter.Begin  -> number` | Der Anfangswert (les- und schreibbar). |
| `counter.End  -> number` | Der Endwert (les- und schreibbar). |
| `counter.Interval  -> number` | Sekunden pro Werteinheit (les- und schreibbar). |
| `counter:Start()  -> nil` | Setzt den Wert auf Begin zurück und beginnt zu ticken. |
| `counter:Resume()  -> nil` | Beginnt zu ticken, ohne den Wert zurückzusetzen. |
| `counter:Stop()  -> nil` | Stoppt das Ticken. |
| `counter:Pause()  -> nil` | Wie Stop. |
| `counter:Reset()  -> nil` | Setzt den Wert auf Begin zurück, ohne zu ändern, ob er tickt. |
| `counter:Tick()  -> nil` | Rückt den Wert um einen Frame vor und ruft dabei bei Bedarf Listener und die Funktion ended auf. |
| `counter:SetLoop(loop)  -> nil` | Springt zurück auf Begin, wenn der Wert das Ende erreicht; schaltet das Pendeln aus. |
| `counter:SetBounce(bounce)  -> nil` | Kehrt die Richtung um, wenn der Wert eines der Enden erreicht; schaltet das Schleifen aus. |
| `counter:GetLoop()  -> boolean` | Ob das Schleifen aktiv ist. |
| `counter:GetBounce()  -> boolean` | Ob das Pendeln aktiv ist. |
| `counter:SetEasing(type, function)  -> nil` | Wendet eine Easing-Kurve auf den gemeldeten Wert an; type ist IN, OUT, INOUT oder OUTIN und function ist LINEAR, SINE, QUAD, CUBIC, QUART, QUINT, EXPO, CIRC, ELASTIC, BACK oder BOUNCE (Groß-/Kleinschreibung egal; einen unbekannten Namen ignoriert der Counter). |
| `counter:ClearEasing()  -> nil` | Entfernt das Easing. |
| `counter:Listen(listener)  -> nil` | Registriert eine Funktion, die bei jedem Tick mit dem aktuellen Wert aufgerufen wird. |
| `counter:ClearListeners()  -> nil` | Entfernt alle Listener. |

```lua
local fade = nil

function activate()
    fade = COUNTER:CreateCounterDuration(0, 1, 0.5, function() debugLog("fade done") end)
    fade:SetEasing("OUT", "QUAD")
    fade:Start()
end

function update(timestamp)
    fade:Tick()
end

function draw()
    background:SetOpacity(fade.Value)
    background:Draw(0, 0)
end
```

### GLOBALCAMERA

Die 2D-Kamera für den gesamten Bildschirm: Sie verschiebt, zoomt und dreht das gesamte gerenderte Bild und fügt ein abklingendes Bildschirmwackeln hinzu.

<div class="callout warn">
Als globales Objekt GLOBALCAMERA registriert. Sie steuert dieselbe Bildschirmtransformation wie die TJA-Befehle #CAMERA und wirkt auf alles Gezeichnete, einschließlich eingeblendeter 3D-Szenen. Offsets sind in Referenzpixeln bei 1280x720, die Rotation ist in Grad, und ein Zoom von 1 bedeutet keine Skalierung. Die Basistransformation bleibt bestehen, bis Sie sie ändern: Rufen Sie jeden Frame Update(dt) auf, um sie anzuwenden und das Wackeln fortzuschreiben, und Reset(), wenn Sie die Stage verlassen, damit sie nicht in die nächste übernommen wird. Die eigene Kamera einer 3D-Szene setzen Sie auf dem Szenenobjekt.
</div>

| Methode | Beschreibung |
| --- | --- |
| `GLOBALCAMERA:SetOffset(x, y)  -> nil` | Verschiebt den Bildschirm um (x, y) Pixel. |
| `GLOBALCAMERA:SetZoom(sx, sy)  -> nil` | Zoomt den Bildschirm mit getrennten X- und Y-Faktoren. |
| `GLOBALCAMERA:SetUniformZoom(s)  -> nil` | Zoomt den Bildschirm gleichmäßig. |
| `GLOBALCAMERA:SetRotation(deg)  -> nil` | Dreht den Bildschirm um seinen Mittelpunkt. |
| `GLOBALCAMERA:GetOffsetX()  -> number` | Der Basis-X-Offset. |
| `GLOBALCAMERA:GetOffsetY()  -> number` | Der Basis-Y-Offset. |
| `GLOBALCAMERA:GetZoomX()  -> number` | Der X-Zoomfaktor. |
| `GLOBALCAMERA:GetZoomY()  -> number` | Der Y-Zoomfaktor. |
| `GLOBALCAMERA:GetRotation()  -> number` | Die Basisrotation in Grad. |
| `GLOBALCAMERA:Shake(amplitudePx, seconds, rotAmpDeg?)  -> nil` | Startet ein Wackeln, das über die angegebenen Sekunden linear von der angegebenen Pixelamplitude abklingt, mit optionalem Rotationswackeln in Grad. Einen Aufruf mit seconds von 0 oder weniger ignoriert die Kamera; ein neues Wackeln ersetzt das aktuelle nur, wenn seine Amplitude gleich groß oder größer ist. |
| `GLOBALCAMERA.IsShaking  -> boolean` | True, solange ein Wackeln noch abklingt. |
| `GLOBALCAMERA:Update(dt)  -> nil` | Schreibt das Wackeln um dt Sekunden fort (begrenzt auf 0,25) und wendet die Basistransformation plus Wackeln auf den Bildschirm an. |
| `GLOBALCAMERA:Reset()  -> nil` | Zentriert die Kamera neu, setzt Zoom und Rotation zurück und stoppt jedes Wackeln. |

```lua
function update(timestamp)
    if INPUT:Pressed("LRed") or INPUT:Pressed("RRed") then GLOBALCAMERA:Shake(18, 0.35) end
    GLOBALCAMERA:Update(fps.deltaTime)
end

function deactivate()
    GLOBALCAMERA:Reset()
end
```

## Song-Enumeration

Zwei globale Funktionen melden den Zustand des Song-Scans. Verwenden Sie sie zusammen mit dem Callback afterSongEnum.

| Methode | Beschreibung |
| --- | --- |
| `IsSongsEnumerating()  -> boolean` | True, solange ein Durchlauf der Song-Enumeration läuft. |
| `IsSongsEnumDone()  -> boolean` | True, sobald der Song-Scan abgeschlossen ist. Vor dem Start des Scans und während er läuft ist es false; IsSongsEnumerating ist sowohl im nicht gestarteten als auch im abgeschlossenen Zustand false, prüfen Sie also diese Funktion, um zu wissen, dass die Liste bereit ist. |

## Diagnose

### info

Schreibgeschütztes Objekt mit grundlegendem Spielzustand und dem eigenen Verzeichnis des Moduls.

<div class="callout warn">
Als globales Objekt info registriert und pro Modul erzeugt. Jedes Feld berechnet seinen Wert beim Zugriff; online fragt bei jedem Lesen das Betriebssystem ab.
</div>

| Methode | Beschreibung |
| --- | --- |
| `info.playerCount  -> number` | Die konfigurierte Spieleranzahl. |
| `info.lang  -> string` | Der aktuelle Sprachcode. |
| `info.simplemode  -> boolean` | True, wenn Simple Mode aktiv ist. |
| `info.p1IsBlue  -> boolean` | True, wenn Spieler 1 die blaue Seite verwendet. |
| `info.online  -> boolean` | True, wenn eine nutzbare Netzwerkschnittstelle verfügbar ist. |
| `info.dir  -> string` | Das Verzeichnis dieses Moduls. |

### fps

Schreibgeschütztes Objekt mit dem Frame-Timing und einer hochauflösenden Uhr.

| Methode | Beschreibung |
| --- | --- |
| `fps.deltaTime  -> number` | Seit dem vorherigen Frame vergangene Sekunden. |
| `fps.fps  -> number` | Die aktuell gemessenen Frames pro Sekunde. |
| `fps.ms  -> number` | Eine monotone Uhr in Millisekunden, um Abschnitte von Lua durch Differenzbildung zu messen. |

### debugLog

| Methode | Beschreibung |
| --- | --- |
| `debugLog(message)  -> nil` | Schreibt den String mit einem Präfix, das ihn als Lua-Log kennzeichnet, in das Trace-Log der Engine. |

## Weitere globale Objekte

Die Engine registriert diese globalen Objekte in jedem Modul; ihre eigenen Seiten dokumentieren sie.

| Globales Objekt | Seite |
| --- | --- |
| `GetSaveFile(player)` | [Spieler und Profile](players.md). Gibt in ROActivities und Hintergründen ein schreibgeschütztes Handle zurück. |
| `RequestSongList(settings)`, `GenerateSongListSettings()` | [Songs und Charts](songs.md). |
| `MODICONS` | [Songs und Charts](songs.md). |
| `CONFIG`, `DATABASE`, `SHARED`, `STORAGE`, `JSONLOADER`, `INILOADER`, `SQL` | [Daten und Persistenz](data.md). |
| `TEXTURE`, `CANVAS`, `GRAPHICS`, `TEXT`, `VIDEO`, `COLOR`, `GRADIENT`, `SIZE` | [Grafik und Text](graphics.md). |
| `SOUND`, `HITSOUNDSLIST` | [Audio](audio.md). |
| `INPUT` | [Eingabe](input.md). |
| `NAMEPLATE`, `NAMEPLATESLIST`, `CHARACTER`, `CHARACTERLIST`, `PUCHICHARALIST`, `PLAYSTATE`, `THEME`, `LANG` | [Spieler und Profile](players.md). |
| `VECTOR`, `VECTOR2`, `VECTOR3`, `VECTOR4`, `MATRIX`, `MATRIX2`, `MATRIX3`, `MATRIX4`, `QUATERNION` | [Mathematik](math.md). |
| `SONGMOUNT`, `REPLAY`, `DANBUILDER`, `VIRTUALSLOTS` | [Songs und Charts](songs.md). |
| `NET` | [Online-Netzwerk](networking.md). |
| `SCENE3D`, `MODEL`, `PHYSICS`, `COLLIDERS`, `PATHFIND`, `HEIGHTMAP` | [3D-Engine: Rasterizer-Welt](3d.md), [3D-Engine: Raytracer-Welt](3d-raytrace.md), [3D-Engine: Physik](3d-physics.md) <span class="badge-exp">Experimentell</span>. |
