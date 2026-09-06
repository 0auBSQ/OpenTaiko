<!-- api/audio.md -->

# Audio

Laden und Abspielen von Sounds sowie Auslesen der installierten Hitsound-Sets.

## SOUND

Erzeugt Sound-Handles aus Audiodateien. Jede Fabrikmethode ordnet den Sound einer Lautstärkegruppe zu (Soundeffekt, Stimme, Songwiedergabe oder Songvorschau), und die Gruppe bestimmt, welche Lautstärkeeinstellung des Benutzers gilt.

<div class="callout warn">
Relative Pfade werden gegen das Verzeichnis des Skripts aufgelöst; die FromAbsolutePath-Varianten nehmen einen vollständigen Pfad. Das zurückgegebene Handle beginnt leer, während der Render-Thread den Stream in den folgenden Frames aufbaut; das Handle reiht Play- und Set*-Aufrufe, die davor erfolgen, ein und wendet sie an, sobald der Stream bereit ist. Das Spiel gibt Handles frei, wenn es das Skript entlädt; rufen Sie Dispose auf, um eines früher freizugeben.
</div>

| Methode | Beschreibung |
| --- | --- |
| `SOUND:CreateSFX(path)  -> sound` | Lädt einen Soundeffekt. |
| `SOUND:CreateVoice(path)  -> sound` | Lädt einen Sprachclip. |
| `SOUND:CreateBGM(path)  -> sound` | Lädt Hintergrundmusik (Gruppe Songwiedergabe). |
| `SOUND:CreatePreview(path)  -> sound` | Lädt einen Songvorschau-Clip (Gruppe Songvorschau). |
| `SOUND:CreateSFXFromAbsolutePath(path)  -> sound` | Lädt einen Soundeffekt aus einem vollständigen Pfad. |
| `SOUND:CreateVoiceFromAbsolutePath(path)  -> sound` | Lädt einen Sprachclip aus einem vollständigen Pfad. |
| `SOUND:CreateBGMFromAbsolutePath(path)  -> sound` | Lädt Hintergrundmusik aus einem vollständigen Pfad. |
| `SOUND:CreatePreviewFromAbsolutePath(path)  -> sound` | Lädt einen Songvorschau-Clip aus einem vollständigen Pfad. |

```lua
local bgm, decide

function onStart()
    bgm = SOUND:CreateBGM("Sounds/BGM.ogg")
    bgm:SetLoop(true)
    decide = SOUND:CreateSFX("Sounds/Decide.ogg")
end

function activate()
    bgm:Play()
end

function update()
    if INPUT:Pressed("Decide") then decide:Play() end
end

function deactivate()
    bgm:Stop()
end

function onDestroy()
    bgm:Dispose()
    decide:Dispose()
end
```

## Sound-Handle

Ein abspielbarer Sound, der von den Fabrikmethoden von `SOUND` zurückgegeben wird.

<div class="callout warn">
Bis der Stream bereit ist, puffert das Handle Play und jeden Set*-Aufruf, und die Getter liefern die unten aufgeführten Standardwerte. Die Lautstärke ist ein Prozentwert (100 = der eigene Pegel der Datei). Pan reicht von -100 (links) über 0 (Mitte) bis 100 (rechts). Positionen und Dauern sind in Millisekunden. Play startet den Stream von vorn; suchen Sie daher nach dem Aufruf von Play.
</div>

| Methode | Beschreibung |
| --- | --- |
| `sound:Play()  -> nil` | Startet die Wiedergabe von vorn (oder startet sie neu). |
| `sound:Start()  -> nil` | Wie Play. |
| `sound:Pause()  -> nil` | Pausiert die Wiedergabe. |
| `sound:Resume()  -> nil` | Setzt die Wiedergabe nach Pause fort. |
| `sound:Stop()  -> nil` | Stoppt die Wiedergabe. |
| `sound:Reset()  -> nil` | Springt zurück zum Anfang. |
| `sound:SetLoop(loop)  -> nil` | Aktiviert oder deaktiviert die Schleife. |
| `sound:GetLoop()  -> bool` | Gibt zurück, ob die Schleife aktiviert ist. |
| `sound:SetVolumePercent(vol)  -> nil` | Setzt die Lautstärke in Prozent. |
| `sound:GetVolumePercent()  -> number` | Gibt die Lautstärke in Prozent zurück (100, wenn nicht geladen). |
| `sound:SetVolume(vol)  -> nil` | Älterer Name für SetVolumePercent (Ganzzahl). |
| `sound:SetPan(pan)  -> nil` | Setzt das Stereo-Panning, -100..100. |
| `sound:GetPan()  -> int` | Gibt das aktuelle Panning zurück (0, wenn nicht geladen). |
| `sound:SetSpeed(speed)  -> nil` | Setzt den Geschwindigkeitsfaktor der Wiedergabe (1 = normal). |
| `sound:GetSpeed()  -> number` | Gibt den Geschwindigkeitsfaktor der Wiedergabe zurück (1, wenn nicht geladen). |
| `sound:SetTimestampMs(ms)  -> nil` | Springt zu einer Position in Millisekunden. |
| `sound:SetTimestamp(ms)  -> nil` | Älterer Name für SetTimestampMs. |
| `sound:GetTimestampMs()  -> number` | Gibt die aktuelle Position in Millisekunden zurück (0, wenn nicht geladen). |
| `sound.DurationMs  -> number` | Gesamtdauer in Millisekunden (0, wenn nicht geladen). |
| `sound:GetDurationMs()  -> number` | Älterer Name für DurationMs. |
| `sound.Loaded  -> bool` | True, sobald der Render-Thread den Stream aufgebaut hat. |
| `sound.IsPlaying  -> bool` | True, während der Sound abgespielt wird. |
| `sound:IsFinished()  -> bool` | True, sobald die Wiedergabe das Ende erreicht hat (false während der Pause oder wenn nicht geladen). |
| `sound.Path  -> string` | Der vollständige Pfad der geladenen Datei (leer, bis geladen). |
| `sound:Dispose()  -> nil` | Gibt den Sound frei. |

## HITSOUNDSLIST

Schreibgeschützte Liste der im aktuellen Skin installierten Hitsound-Sets (das Spiel liest sie aus `Global/HitSounds/`).

<div class="callout warn">
In jedem Skript verfügbar. Indizes sind 0-basiert. GetByName vergleicht den Ordnernamen des Sets ohne Beachtung der Groß-/Kleinschreibung. Beide Abfragen geben nil zurück, wenn nichts passt.
</div>

| Methode | Beschreibung |
| --- | --- |
| `HITSOUNDSLIST.Count  -> int` | Anzahl der Hitsound-Sets. |
| `HITSOUNDSLIST:GetByIndex(index)  -> hitsoundEntry` | Gibt den Eintrag am angegebenen 0-basierten Index zurück, oder nil. |
| `HITSOUNDSLIST:GetByName(name)  -> hitsoundEntry` | Gibt den Eintrag zurück, dessen Ordnername passt, oder nil. |

```lua
local don

function onStart()
    for i = 0, HITSOUNDSLIST.Count - 1 do
        local hs = HITSOUNDSLIST:GetByIndex(i)
        debugLog(hs.FolderName .. " -> " .. hs.DisplayName)
    end
    local taiko = HITSOUNDSLIST:GetByName("Taiko")
    if taiko then
        don = SOUND:CreateSFXFromAbsolutePath(taiko.DonPath)
    end
end

function onDestroy()
    if don then don:Dispose() end
end
```

## Hitsound-Eintrag

Ein Hitsound-Set, das von `HITSOUNDSLIST:GetByIndex` oder `GetByName` zurückgegeben wird. Alle Member sind schreibgeschützt.

| Methode | Beschreibung |
| --- | --- |
| `hitsoundEntry.FolderName  -> string` | Ordnername des Sets (zum Beispiel `"Taiko"`). |
| `hitsoundEntry.DisplayName  -> string` | Lokalisierter Anzeigename; fällt auf den Ordnernamen zurück. |
| `hitsoundEntry.DonPath  -> string` | Vollständiger Pfad der Don-Sounddatei des Sets. |
| `hitsoundEntry.KaPath  -> string` | Vollständiger Pfad der Ka-Sounddatei des Sets. |
