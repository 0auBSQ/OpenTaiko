<!-- api/audio.md -->

# Audio

Geluiden laden en afspelen, en de geïnstalleerde hitsound-sets lezen.

## SOUND

Maakt geluid-handles uit audiobestanden. Elke factorymethode wijst het geluid toe aan een volumegroep (geluidseffect, stem, nummerweergave of nummerpreview), en de groep bepaalt welke gebruikersvolume-instelling van toepassing is.

<div class="callout warn">
Relatieve paden worden opgelost ten opzichte van de map van het script; de FromAbsolutePath-varianten nemen een volledig pad. De teruggegeven handle begint leeg terwijl de renderthread de stream in de volgende frames opbouwt; de handle zet aanroepen van Play en Set* die daarvóór worden gedaan in de wachtrij en past ze toe zodra de stream klaar is. Het spel geeft handles vrij wanneer het het script ontlaadt; roep Dispose aan om er een eerder vrij te geven.
</div>

| Methode | Beschrijving |
| --- | --- |
| `SOUND:CreateSFX(path)  -> sound` | Laadt een geluidseffect. |
| `SOUND:CreateVoice(path)  -> sound` | Laadt een stemfragment. |
| `SOUND:CreateBGM(path)  -> sound` | Laadt achtergrondmuziek (groep nummerweergave). |
| `SOUND:CreatePreview(path)  -> sound` | Laadt een nummerpreview (groep nummerpreview). |
| `SOUND:CreateSFXFromAbsolutePath(path)  -> sound` | Laadt een geluidseffect van een volledig pad. |
| `SOUND:CreateVoiceFromAbsolutePath(path)  -> sound` | Laadt een stemfragment van een volledig pad. |
| `SOUND:CreateBGMFromAbsolutePath(path)  -> sound` | Laadt achtergrondmuziek van een volledig pad. |
| `SOUND:CreatePreviewFromAbsolutePath(path)  -> sound` | Laadt een nummerpreview van een volledig pad. |

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

## Geluid-handle

Een afspeelbaar geluid, teruggegeven door de factorymethoden van `SOUND`.

<div class="callout warn">
Tot de stream klaar is, buffert de handle Play en elke Set*-aanroep, en geven de getters de hieronder vermelde standaardwaarden terug. Volume is een percentage (100 = het eigen niveau van het bestand). Pan loopt van -100 (links) via 0 (midden) tot 100 (rechts). Posities en duren zijn in milliseconden. Play herstart de stream vanaf het begin, dus zoek pas na het aanroepen van Play.
</div>

| Methode | Beschrijving |
| --- | --- |
| `sound:Play()  -> nil` | Start (of herstart) het afspelen vanaf het begin. |
| `sound:Start()  -> nil` | Hetzelfde als Play. |
| `sound:Pause()  -> nil` | Pauzeert het afspelen. |
| `sound:Resume()  -> nil` | Hervat het afspelen na Pause. |
| `sound:Stop()  -> nil` | Stopt het afspelen. |
| `sound:Reset()  -> nil` | Springt terug naar het begin. |
| `sound:SetLoop(loop)  -> nil` | Schakelt herhalen in of uit. |
| `sound:GetLoop()  -> bool` | Geeft terug of herhalen is ingeschakeld. |
| `sound:SetVolumePercent(vol)  -> nil` | Stelt het volume in procenten in. |
| `sound:GetVolumePercent()  -> number` | Geeft het volume in procenten terug (100 wanneer niet geladen). |
| `sound:SetVolume(vol)  -> nil` | Oudere naam voor SetVolumePercent (geheel getal). |
| `sound:SetPan(pan)  -> nil` | Stelt de stereopan in, -100..100. |
| `sound:GetPan()  -> int` | Geeft de huidige pan terug (0 wanneer niet geladen). |
| `sound:SetSpeed(speed)  -> nil` | Stelt de afspeelsnelheidsfactor in (1 = normaal). |
| `sound:GetSpeed()  -> number` | Geeft de afspeelsnelheidsfactor terug (1 wanneer niet geladen). |
| `sound:SetTimestampMs(ms)  -> nil` | Springt naar een positie in milliseconden. |
| `sound:SetTimestamp(ms)  -> nil` | Oudere naam voor SetTimestampMs. |
| `sound:GetTimestampMs()  -> number` | Geeft de huidige positie in milliseconden terug (0 wanneer niet geladen). |
| `sound.DurationMs  -> number` | Totale duur in milliseconden (0 wanneer niet geladen). |
| `sound:GetDurationMs()  -> number` | Oudere naam voor DurationMs. |
| `sound.Loaded  -> bool` | True zodra de renderthread de stream heeft opgebouwd. |
| `sound.IsPlaying  -> bool` | True terwijl het geluid speelt. |
| `sound:IsFinished()  -> bool` | True zodra het afspelen het einde heeft bereikt (false tijdens pauze of wanneer niet geladen). |
| `sound.Path  -> string` | Het volledige pad van het geladen bestand (leeg tot het is geladen). |
| `sound:Dispose()  -> nil` | Geeft het geluid vrij. |

## HITSOUNDSLIST

Alleen-lezen lijst van de hitsound-sets die in de huidige skin zijn geïnstalleerd (het spel leest ze uit `Global/HitSounds/`).

<div class="callout warn">
Beschikbaar in elk script. Indices beginnen bij 0. GetByName vergelijkt de mapnaam van de set zonder onderscheid tussen hoofdletters en kleine letters. Beide opzoekmethoden geven nil terug wanneer niets overeenkomt.
</div>

| Methode | Beschrijving |
| --- | --- |
| `HITSOUNDSLIST.Count  -> int` | Aantal hitsound-sets. |
| `HITSOUNDSLIST:GetByIndex(index)  -> hitsoundEntry` | Geeft het item op de gegeven 0-gebaseerde index terug, of nil. |
| `HITSOUNDSLIST:GetByName(name)  -> hitsoundEntry` | Geeft het item terug waarvan de mapnaam overeenkomt, of nil. |

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

## Hitsound-item

Eén hitsound-set, teruggegeven door `HITSOUNDSLIST:GetByIndex` of `GetByName`. Alle leden zijn alleen-lezen.

| Methode | Beschrijving |
| --- | --- |
| `hitsoundEntry.FolderName  -> string` | Mapnaam van de set (bijvoorbeeld `"Taiko"`). |
| `hitsoundEntry.DisplayName  -> string` | Gelokaliseerde weergavenaam; valt terug op de mapnaam. |
| `hitsoundEntry.DonPath  -> string` | Volledig pad van het Don-geluidsbestand van de set. |
| `hitsoundEntry.KaPath  -> string` | Volledig pad van het Ka-geluidsbestand van de set. |
