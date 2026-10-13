<!-- api/aviutl2.md -->

# AviUtl2-Projekte

Das globale Objekt `AVIUTL2` spielt Projektdateien ab, die der Videoeditor AviUtl2 gespeichert hat (`.aup2`). Das Spiel liest das Projekt und zeichnet dessen Objekte, Bewegungen und Effekte live, sodass ein Skin eine Animation als Projekt mit dessen Bildern anstelle eines gerenderten Videos mitliefern kann.

Das Skript, das ein Projekt lädt, besitzt dessen Handle, und das Spiel gibt das Handle frei, wenn es dieses Skript entlädt. Rufen Sie `Dispose()` selbst auf, um es früher freizugeben.

<div class="callout warn">
Das Spiel zeichnet das Projekt mit seinem eigenen Renderer. Positionen, Größen, Drehungen, Bewegungskurven, Kamerasteuerung und die gängigen Effekte folgen dem Editor. Zufällige Bewegungen und Wackeleffekte verwenden andere Zufallswerte, und einige Effekte sind Annäherungen. Lesen Sie nach dem Laden eines Projekts GetUnsupported und GetApproximated, um zu sehen, was das Spiel weggelassen oder vereinfacht hat.
</div>

## Laden

### AVIUTL2

| Methode | Beschreibung |
| --- | --- |
| `AVIUTL2:LoadProject(path)  -> project` | Öffnet eine Projektdatei und gibt ihr Handle zurück. Der Pfad ist relativ zum Skriptverzeichnis. |
| `AVIUTL2:LoadProjectFromAbsolutePath(path)  -> project` | Öffnet eine Projektdatei aus einem vollständigen Pfad. |

<div class="callout warn">
Ein Projekt speichert die vollständigen Pfade des Rechners, auf dem es gespeichert wurde. Das Spiel sucht jedes Bild, jedes Video, jeden Ton und jedes Modell neben der Projektdatei, zuerst anhand der Ordner, die der gespeicherte Pfad mit dem Pfad des Projekts gemeinsam hat, dann anhand des Dateinamens. Legen Sie die Dateien des Projekts in dessen Ordner oder in Unterordner davon. Ein fehlendes oder unlesbares Projekt liefert ein Handle, dessen Loaded false ist.
</div>

## Projekt-Handle

Ein Projekt, das von `AVIUTL2:LoadProject` zurückgegeben wird. Seine Wiedergabemethoden tragen dieselben Namen wie die eines Video-Handles.

### Wiedergabe

| Methode | Beschreibung |
| --- | --- |
| `project:Start()  -> nil` | Startet die Wiedergabe von Anfang an. |
| `project:Resume()  -> nil` | Setzt die Wiedergabe nach Pause fort. |
| `project:Pause()  -> nil` | Pausiert die Wiedergabe einschließlich der Töne. |
| `project:Stop()  -> nil` | Stoppt die Wiedergabe. |
| `project:Reset()  -> nil` | Springt zurück zum Anfang. |
| `project.IsPlaying  -> bool` | True, solange das Projekt spielt und sein Ende noch nicht erreicht hat. |
| `project:IsFinished()  -> bool` | True, sobald ein Projekt ohne Schleife sein Ende erreicht hat. |
| `project:GetTimestampMs()  -> number` | Aktuelle Position in Millisekunden. |
| `project:SetTimestampMs(ms)  -> nil` | Springt zu einer Position in Millisekunden. |
| `project:GetFrame()  -> number` | Aktuelle Position in Frames des Projekts. |
| `project:SetFrame(frame)  -> nil` | Springt zu einem Frame. |
| `project:GetSpeed()  -> number` | Aktueller Geschwindigkeitsfaktor der Wiedergabe (1 = normal). |
| `project:SetSpeed(speed)  -> nil` | Setzt den Geschwindigkeitsfaktor der Wiedergabe. |
| `project:GetLoop()  -> bool` | True, wenn das Projekt an seinem Ende von vorn beginnt. |
| `project:SetLoop(loop)  -> nil` | Lässt das Projekt an seinem Ende von vorn beginnen. |
| `project:GetVolumePercent()  -> number` | Lautstärke der Töne des Projekts in Prozent. |
| `project:SetVolumePercent(vol)  -> nil` | Setzt die Lautstärke der Töne des Projekts in Prozent. |
| `project:Dispose()  -> nil` | Gibt das Projekt, seine Bilder, Videos und Töne frei. |

### Zeichnen

| Methode | Beschreibung |
| --- | --- |
| `project:Draw(x, y)  -> nil` | Zeichnet den aktuellen Frame mit seiner oberen linken Ecke bei (x, y), in der mit SetScale gesetzten Skalierung. |
| `project:DrawFit(x, y, w, h)  -> nil` | Zeichnet den aktuellen Frame so groß, wie es das Rechteck erlaubt, mit beibehaltenem Seitenverhältnis und im Rechteck zentriert. |
| `project:SetScale(scaleX, scaleY)  -> nil` | Setzt die Skalierung, die Draw verwendet (1 = die eigene Größe des Projekts). |
| `project:SetOpacity(opacity)  -> nil` | Setzt die Deckkraft des gesamten Frames, von 0 bis 1. |
| `project:SetBackground(r, g, b, a)  -> nil` | Füllt den Frame hinter den Objekten mit einer Farbe, jeder Wert von 0 bis 255. Ein Alpha von 0 lässt den Frame durchsichtig. |
| `project:ClearBackground()  -> nil` | Kehrt zu dem Hintergrund zurück, den die Projektdatei vorgibt. |
| `project.HasProjectBackground  -> bool` | True, wenn die Szene eine eigene Hintergrundfarbe hat. |
| `project.ProjectBackground  -> int` | Die Hintergrundfarbe der Szene als 0xRRGGBB, oder -1, wenn sie keine hat. |

<div class="callout warn">
Eine Szene ohne Hintergrundfarbe ist dort durchsichtig, wo kein Objekt gezeichnet wird, sodass das Projekt über alles zeichnet, was das Skript zuvor gezeichnet hat. Der Editor zeigt eine solche Szene auf Schwarz. Zeichnen Sie zuerst ein schwarzes Rechteck oder rufen Sie SetBackground(0, 0, 0, 255) auf, um dasselbe Bild zu erhalten.
</div>

### Vom Skript gezeichnete Bilder

Ein Skript kann ein Bild des Projekts durch seine eigene Zeichnung ersetzen, zum Beispiel um das Namensschild eines Spielers in einem animierten Rahmen zu zeigen.

| Methode | Beschreibung |
| --- | --- |
| `project:SetImageDrawer(fileName, width, height, draw)  -> nil` | Jedes Objekt, das die Bilddatei `fileName` zeigt, zeigt das, was die Funktion `draw` zeichnet. Das Spiel ruft `draw` jeden Frame mit einer Zeichenfläche von `width` mal `height` auf, deren obere linke Ecke (0, 0) ist. |
| `project:ClearImageDrawer(fileName)  -> nil` | Die Objekte zeigen wieder die Bilddatei. |

Das Spiel ruft `draw(rank, layer)` einmal für jede Ebene auf, die ein solches Objekt enthält. `rank` zählt diese Ebenen ab 0, beginnend mit der obersten, und `layer` ist die Nummer der Ebene im Projekt. Verwenden Sie in `draw` die üblichen Zeichenaufrufe: Das Spiel leitet sie in das Bild um, das sich dann mit dem Objekt bewegt, skaliert und ausblendet.

<div class="callout warn">
Die Funktion muss zu dem Skript gehören, das SetImageDrawer aufruft. Weiche Kanten, die in `draw` gezeichnet werden, fallen etwas dünner aus als auf dem Bildschirm.
</div>

### Das Projekt auslesen

| Methode | Beschreibung |
| --- | --- |
| `project.Loaded  -> bool` | True, wenn die Datei gelesen wurde. |
| `project.Width  -> int` | Breite der Szene in Pixeln, oder -1, wenn nichts geladen ist. |
| `project.Height  -> int` | Höhe der Szene in Pixeln, oder -1, wenn nichts geladen ist. |
| `project.FrameRate  -> number` | Frames pro Sekunde der Szene. |
| `project.FrameCount  -> int` | Länge der Szene in Frames. |
| `project.DurationMs  -> number` | Länge der Szene in Millisekunden. |
| `project.SceneCount  -> int` | Anzahl der Szenen im Projekt. |
| `project:GetScene()  -> int` | Index der Szene, die gerade abgespielt wird. |
| `project:SetScene(index)  -> nil` | Spielt eine andere Szene des Projekts von ihrem Anfang an ab. |
| `project:GetSceneName(index)  -> string` | Name einer Szene. |
| `project.ObjectCount  -> int` | Anzahl der Objekte in der Szene. |
| `project:GetProjectValue(key)  -> string` | Eine Einstellung des Projekts, wie sie in der Datei steht. |
| `project:GetSceneValue(key)  -> string` | Eine Einstellung der Szene, die gerade abgespielt wird. |
| `project:GetValue(section, key)  -> string` | Eine Einstellung eines beliebigen Abschnitts der Datei, anhand des Abschnittsnamens (`"12"` für ein Objekt, `"12.1"` für dessen zweiten Effekt). |
| `project:GetValueAtFrame(objectId, effect, key, frame)  -> number` | Eine numerische Einstellung des Effekts eines Objekts an einem Frame, mit angewendeter Bewegung. |

### Prüfen, was gezeichnet wird

| Methode | Beschreibung |
| --- | --- |
| `project.UnsupportedCount  -> int` | Anzahl der Dinge, die das Projekt verwendet und die das Spiel nicht zeichnet, einschließlich fehlender Dateien. |
| `project:GetUnsupported()  -> string` | Diese Dinge, eines pro Zeile. |
| `project.ApproximatedCount  -> int` | Anzahl der Dinge, die das Spiel vereinfacht zeichnet. |
| `project:GetApproximated()  -> string` | Diese Dinge, eines pro Zeile. |
| `project:AddScriptFolder(path)  -> nil` | Fügt einen Ordner mit AviUtl-Skriptdateien hinzu, die die Effekte des Projekts benennen können. Einen Ordner `Script` neben der Projektdatei liest das Spiel von selbst. |
| `project.ScriptErrorCount  -> int` | Anzahl der Skriptdateien, die fehlgeschlagen sind. |
| `project:GetScriptErrors()  -> string` | Der erste Fehler jedes fehlgeschlagenen Skripts, einer pro Zeile. |
| `project.GraphicsMemoryMB  -> number` | Grafikspeicher, den das Projekt derzeit für sein eigenes Zeichnen belegt, in Megabyte. |
| `project:SetSurfaceBudgetMB(mb)  -> nil` | Legt fest, wie viel Grafikspeicher das Projekt zur Wiederverwendung zwischen Frames behält. |

<div class="callout warn">
Skriptdateien, die mit einem Projekt ausgeliefert werden, laufen in einer abgeschlossenen Umgebung: Sie können keine Dateien lesen oder schreiben, keine Programme starten und keine nativen Module laden.
</div>

```lua
local intro
local box

function onStart()
    intro = AVIUTL2:LoadProject("Intro/intro.aup2")
    box = TEXTURE:CreateTexture("Intro/player_box.png")
    -- das Namensschild jedes Spielers, gezeichnet in die Spielerkästen des Projekts
    intro:SetImageDrawer("player_box.png", 520, 216, function(rank)
        box:Draw(0, 0)
        NAMEPLATE:DrawPlayerNameplate(170, 88, 255, rank)
    end)
end

function activate()
    intro:SetBackground(0, 0, 0, 255)
    intro:Start()
end

function draw()
    intro:DrawFit(0, 0, 1920, 1080)
    if intro:IsFinished() then Exit("title", nil) end
end

function deactivate()
    intro:Stop()
end

function onDestroy()
    intro:Dispose()
    box:Dispose()
end
```
