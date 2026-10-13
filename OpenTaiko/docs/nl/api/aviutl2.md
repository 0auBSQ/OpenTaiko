<!-- api/aviutl2.md -->

# AviUtl2-projecten

De `AVIUTL2`-global speelt projectbestanden af die met de video-editor AviUtl2 zijn opgeslagen (`.aup2`). Het spel leest het project en tekent de objecten, bewegingen en effecten ervan live, zodat een skin een animatie kan meeleveren als project met zijn afbeeldingen in plaats van een gerenderde video.

Het script dat een project laadt, is eigenaar van de handle, en het spel geeft de handle vrij wanneer het dat script ontlaadt. Roep zelf `Dispose()` aan om hem eerder vrij te geven.

<div class="callout warn">
Het spel tekent het project met zijn eigen renderer. Posities, afmetingen, rotaties, bewegingscurven, camerabesturing en de gangbare effecten volgen de editor. Willekeurige bewegingen en trillingen gebruiken andere willekeurige waarden, en sommige effecten zijn benaderingen. Lees na het laden van een project GetUnsupported en GetApproximated om te zien wat het spel heeft weggelaten of vereenvoudigd.
</div>

## Laden

### AVIUTL2

| Methode | Beschrijving |
| --- | --- |
| `AVIUTL2:LoadProject(path)  -> project` | Opent een projectbestand en geeft zijn handle terug. Het pad is relatief ten opzichte van de scriptmap. |
| `AVIUTL2:LoadProjectFromAbsolutePath(path)  -> project` | Opent een projectbestand van een volledig pad. |

<div class="callout warn">
Een project bewaart de volledige paden van de computer waarop het is opgeslagen. Het spel zoekt elke afbeelding, elke video, elk geluid en elk model naast het projectbestand, eerst via de mappen die het opgeslagen pad deelt met dat van het project zelf, daarna op bestandsnaam. Bewaar de bestanden van het project in zijn map of in submappen daarvan. Een ontbrekend of onleesbaar project geeft een handle terug waarvan Loaded false is.
</div>

## Project-handle

Een project, teruggegeven door `AVIUTL2:LoadProject`. Zijn afspeelmethoden dragen dezelfde namen als die van een video-handle.

### Afspelen

| Methode | Beschrijving |
| --- | --- |
| `project:Start()  -> nil` | Start het afspelen vanaf het begin. |
| `project:Resume()  -> nil` | Hervat het afspelen na Pause. |
| `project:Pause()  -> nil` | Pauzeert het afspelen, inclusief de geluiden. |
| `project:Stop()  -> nil` | Stopt het afspelen. |
| `project:Reset()  -> nil` | Springt terug naar het begin. |
| `project.IsPlaying  -> bool` | True zolang het project speelt en zijn einde niet heeft bereikt. |
| `project:IsFinished()  -> bool` | True zodra een project dat niet herhaalt zijn einde heeft bereikt. |
| `project:GetTimestampMs()  -> number` | Huidige positie in milliseconden. |
| `project:SetTimestampMs(ms)  -> nil` | Springt naar een positie in milliseconden. |
| `project:GetFrame()  -> number` | Huidige positie in frames van het project. |
| `project:SetFrame(frame)  -> nil` | Springt naar een frame. |
| `project:GetSpeed()  -> number` | Huidige afspeelsnelheidsfactor (1 = normaal). |
| `project:SetSpeed(speed)  -> nil` | Stelt de afspeelsnelheidsfactor in. |
| `project:GetLoop()  -> bool` | True wanneer het project aan zijn einde opnieuw begint. |
| `project:SetLoop(loop)  -> nil` | Laat het project aan zijn einde opnieuw beginnen. |
| `project:GetVolumePercent()  -> number` | Volume van de geluiden van het project in procent. |
| `project:SetVolumePercent(vol)  -> nil` | Stelt het volume van de geluiden van het project in procent in. |
| `project:Dispose()  -> nil` | Geeft het project, zijn afbeeldingen, video's en geluiden vrij. |

### Tekenen

| Methode | Beschrijving |
| --- | --- |
| `project:Draw(x, y)  -> nil` | Tekent het huidige frame met de linkerbovenhoek op (x, y), op de schaal die met SetScale is ingesteld. |
| `project:DrawFit(x, y, w, h)  -> nil` | Tekent het huidige frame zo groot als het kader toelaat, met behoud van zijn verhoudingen, gecentreerd in het kader. |
| `project:SetScale(scaleX, scaleY)  -> nil` | Stelt de schaal in die Draw gebruikt (1 = de eigen grootte van het project). |
| `project:SetOpacity(opacity)  -> nil` | Stelt de dekking van het hele frame in, van 0 tot 1. |
| `project:SetBackground(r, g, b, a)  -> nil` | Vult het frame achter de objecten met een kleur, elke waarde van 0 tot 255. Een alfa van 0 laat het frame doorzichtig. |
| `project:ClearBackground()  -> nil` | Keert terug naar de achtergrond die het projectbestand vraagt. |
| `project.HasProjectBackground  -> bool` | True wanneer de scène een eigen achtergrondkleur heeft. |
| `project.ProjectBackground  -> int` | De achtergrondkleur van de scène als 0xRRGGBB, of -1 wanneer de scène er geen heeft. |

<div class="callout warn">
Een scène zonder achtergrondkleur is doorzichtig waar geen object is getekend, dus het project tekent over alles heen wat het script ervoor heeft getekend. De editor toont zo'n scène op zwart. Teken eerst een zwart vlak, of roep SetBackground(0, 0, 0, 255) aan, om hetzelfde beeld te krijgen.
</div>

### Afbeeldingen die het script tekent

Een script kan een afbeelding van het project vervangen door zijn eigen tekening, bijvoorbeeld om het naamplaatje van een speler in een geanimeerd kader te tonen.

| Methode | Beschrijving |
| --- | --- |
| `project:SetImageDrawer(fileName, width, height, draw)  -> nil` | Elk object dat het afbeeldingsbestand `fileName` toont, toont wat de functie `draw` tekent. Het spel roept `draw` elk frame aan met een tekengebied van `width` bij `height` waarvan de linkerbovenhoek (0, 0) is. |
| `project:ClearImageDrawer(fileName)  -> nil` | De objecten tonen het afbeeldingsbestand weer. |

Het spel roept `draw(rank, layer)` één keer aan voor elke laag die zo'n object bevat. `rank` telt die lagen vanaf 0, te beginnen bij de bovenste, en `layer` is het nummer van de laag in het project. Gebruik binnen `draw` de gewone tekenaanroepen: het spel leidt ze om naar de afbeelding, die vervolgens met het object meebeweegt, meeschaalt en meevervaagt.

<div class="callout warn">
De functie moet horen bij het script dat SetImageDrawer aanroept. Zachte randen die binnen `draw` worden getekend, komen iets dunner uit dan op het scherm.
</div>

### Het project uitlezen

| Methode | Beschrijving |
| --- | --- |
| `project.Loaded  -> bool` | True wanneer het bestand is gelezen. |
| `project.Width  -> int` | Breedte van de scène in pixels, of -1 wanneer er niets is geladen. |
| `project.Height  -> int` | Hoogte van de scène in pixels, of -1 wanneer er niets is geladen. |
| `project.FrameRate  -> number` | Frames per seconde van de scène. |
| `project.FrameCount  -> int` | Lengte van de scène in frames. |
| `project.DurationMs  -> number` | Lengte van de scène in milliseconden. |
| `project.SceneCount  -> int` | Aantal scènes in het project. |
| `project:GetScene()  -> int` | Index van de scène die wordt afgespeeld. |
| `project:SetScene(index)  -> nil` | Speelt een andere scène van het project af, vanaf het begin ervan. |
| `project:GetSceneName(index)  -> string` | Naam van een scène. |
| `project.ObjectCount  -> int` | Aantal objecten in de scène. |
| `project:GetProjectValue(key)  -> string` | Een instelling van het project, zoals die in het bestand staat. |
| `project:GetSceneValue(key)  -> string` | Een instelling van de scène die wordt afgespeeld. |
| `project:GetValue(section, key)  -> string` | Een instelling van een willekeurige sectie van het bestand, op naam van de sectie (`"12"` voor een object, `"12.1"` voor zijn tweede effect). |
| `project:GetValueAtFrame(objectId, effect, key, frame)  -> number` | Een numerieke instelling van een effect van een object op een frame, met de beweging ervan toegepast. |

### Controleren wat er wordt getekend

| Methode | Beschrijving |
| --- | --- |
| `project.UnsupportedCount  -> int` | Aantal dingen die het project gebruikt en die het spel niet tekent, inclusief ontbrekende bestanden. |
| `project:GetUnsupported()  -> string` | Die dingen, één per regel. |
| `project.ApproximatedCount  -> int` | Aantal dingen die het spel vereenvoudigd tekent. |
| `project:GetApproximated()  -> string` | Die dingen, één per regel. |
| `project:AddScriptFolder(path)  -> nil` | Voegt een map toe met AviUtl-scriptbestanden waarnaar de effecten van het project kunnen verwijzen. Het spel leest een map `Script` naast het projectbestand uit zichzelf. |
| `project.ScriptErrorCount  -> int` | Aantal scriptbestanden die zijn mislukt. |
| `project:GetScriptErrors()  -> string` | De eerste fout van elk mislukt script, één per regel. |
| `project.GraphicsMemoryMB  -> number` | Grafisch geheugen dat het project op dit moment vasthoudt voor zijn eigen tekenwerk, in megabytes. |
| `project:SetSurfaceBudgetMB(mb)  -> nil` | Stelt in hoeveel grafisch geheugen het project bewaart voor hergebruik tussen frames. |

<div class="callout warn">
Scriptbestanden die met een project worden meegeleverd, draaien in een afgesloten omgeving: ze kunnen geen bestanden lezen of schrijven, geen programma's starten en geen native modules laden.
</div>

```lua
local intro
local box

function onStart()
    intro = AVIUTL2:LoadProject("Intro/intro.aup2")
    box = TEXTURE:CreateTexture("Intro/player_box.png")
    -- het naamplaatje van elke speler, getekend in de spelervakken van het project
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
