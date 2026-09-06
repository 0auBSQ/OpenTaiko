<!-- api/graphics.md -->

# Graphics en tekst

Texturen, canvassen, clipping, tekst, video, kleuren, gradiëntmaps en afmetingen. Alle posities en afmetingen zijn in logische schermpixels, de coördinaatruimte waarin scripts tekenen (zie `INPUT:GetSurfaceWidth()` / `GetSurfaceHeight()`).

Het script dat een textuur-, canvas-, tekst-, video- of gradiënt-handle aanmaakt, is er eigenaar van, en het spel geeft de handle vrij wanneer het dat script ontlaadt. Roep zelf `Dispose()` aan om een handle eerder vrij te geven.

## Texturen

### TEXTURE

Laadt afbeeldingsbestanden in textuur-handles.

<div class="callout warn">
Relatieve paden worden opgelost ten opzichte van de map van het script; de FromAbsolutePath-varianten nemen een volledig pad. Een ontbrekend bestand geeft een lege handle terug die niets tekent. CreateTexture laadt asynchroon: de handle tekent niets en rapporteert een Width en Height van 0 tot het decoderen en uploaden op de achtergrond is afgerond. Gebruik CreateTextureSync wanneer je de afmeting of de pixels meteen nodig hebt. De optietabel accepteert { maxSize = N } om de afbeelding bij het decoderen te verkleinen, zodat de langste zijde hoogstens N pixels is.
</div>

| Methode | Beschrijving |
| --- | --- |
| `TEXTURE:CreateTexture()  -> texture` | Maakt een lege handle zonder afbeelding. |
| `TEXTURE:CreateTexture(path)  -> texture` | Laadt asynchroon een afbeelding van een pad relatief ten opzichte van de scriptmap. |
| `TEXTURE:CreateTexture(path, options)  -> texture` | Hetzelfde, met een optietabel (`{ maxSize = N }`). |
| `TEXTURE:CreateTextureSync(path)  -> texture` | Laadt een afbeelding synchroon; afmeting en pixels zijn bij terugkeer beschikbaar. |
| `TEXTURE:CreateTextureFromAbsolutePath(path)  -> texture` | Laadt een afbeelding van een volledig bestandssysteempad. |
| `TEXTURE:CreateTextureFromAbsolutePath(path, options)  -> texture` | Hetzelfde, met een optietabel (`{ maxSize = N }`). |
| `TEXTURE:Exists(path)  -> bool` | Geeft terug of er een bestand bestaat op het pad relatief ten opzichte van de scriptmap. |

```lua
local logo, jacket

function onStart()
    logo = TEXTURE:CreateTexture("Textures/Logo.png")
    jacket = TEXTURE:CreateTexture("Textures/Jacket.png", { maxSize = 512 })
end

function draw()
    logo:DrawAtAnchor(960, 540, "center")
end

function onDestroy()
    logo:Dispose()
    jacket:Dispose()
end
```

### Textuur-handle

Een tekenbare 2D-afbeelding, teruggegeven door de `TEXTURE`-factory, door `text:GetText` / `GetVerticalText`, en door `video.Texture`.

<div class="callout warn">
Ankernamen: topleft, top, topright, left, center, right, bottomleft, bottom, bottomright (niet hoofdlettergevoelig; een onbekende naam valt terug op topleft). Verankerde tekenaanroepen houden rekening met de huidige schaal. Namen van blendmodi: Normal, Add, Multi, Sub, Screen. Namen van wrapmodi: Edge, Border, Repeat, Mirror; nieuwe handles staan standaard op Repeat. Op een lege handle is elke methode een no-op en geven de getters de hieronder vermelde standaardwaarden terug.
</div>

| Methode | Beschrijving |
| --- | --- |
| `texture:Draw(x, y)  -> nil` | Tekent de hele textuur met de linkerbovenhoek op (x, y). |
| `texture:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | Tekent een bron-subrechthoek (in textuurpixels) met de linkerbovenhoek op (x, y). |
| `texture:DrawAtAnchor(x, y, anchor)  -> nil` | Tekent de hele textuur zo dat het benoemde ankerpunt op (x, y) landt. |
| `texture:DrawRectAtAnchor(x, y, rect_x, rect_y, rect_width, rect_height, anchor)  -> nil` | Tekent een bron-subrechthoek zo dat het benoemde ankerpunt op (x, y) landt. |
| `texture.Loaded  -> bool` | True wanneer de handle een textuur omhult. Het spel zet dit zodra het het bestand vindt, voordat een asynchrone laadbeurt is afgerond. |
| `texture.Width  -> int` | Pixelbreedte; -1 op een lege handle, 0 terwijl een asynchrone laadbeurt loopt. |
| `texture.Height  -> int` | Pixelhoogte; -1 op een lege handle, 0 terwijl een asynchrone laadbeurt loopt. |
| `texture.Pointer  -> int` | Native GL-textuur-id, of 0 als er geen is. |
| `texture:GetScale()  -> vector2` | Huidige tekenschaal als vector2 (`X`, `Y`). |
| `texture:GetOpacity()  -> number` | Huidige dekking, 0..1; -1 op een lege handle. |
| `texture:GetColor()  -> number, number, number` | Huidige tint als drie waarden 0..1 (rood, groen, blauw). |
| `texture:GetRotation()  -> number` | Huidige rotatie in graden. |
| `texture:GetBlendMode()  -> string` | Naam van de huidige blendmodus. |
| `texture:GetWrapMode()  -> string` | Naam van de huidige wrapmodus. |
| `texture:SetScale(scale_x, scale_y)  -> nil` | Stelt de horizontale en verticale tekenschaal in (1 = originele grootte). |
| `texture:SetOpacity(opacity)  -> nil` | Stelt de dekking in, 0..1. |
| `texture:SetColor(color)  -> nil` | Stelt de tint in vanuit een `COLOR`-waarde. De handle negeert de alfa van de kleur; stel transparantie in met SetOpacity. |
| `texture:SetColor(red, green, blue)  -> nil` | Stelt de tint in vanuit drie waarden 0..1. |
| `texture:SetRotation(angle)  -> nil` | Stelt de rotatie om het middelpunt van de textuur in, in graden. |
| `texture:SetBlendMode(mode)  -> nil` | Stelt de blendmodus in op naam (niet hoofdlettergevoelig). De handle negeert onbekende namen. |
| `texture:SetWrapMode(mode)  -> nil` | Stelt de wrapmodus in op naam (niet hoofdlettergevoelig). De handle negeert onbekende namen. |
| `texture:SetUseNoiseEffect(enabled)  -> nil` | Indien ingeschakeld vervangt elke tekenaanroep de kleur van de textuur door geanimeerde willekeurige grijswaardenruis en behoudt haar alfa. |
| `texture:SetGradientMap(gradient)  -> nil` | Past een `GRADIENT`-map toe op elke tekenaanroep van deze textuur (zie GRADIENT). |
| `texture:ClearGradientMap()  -> nil` | Verwijdert de gradiëntmap van deze textuur. |
| `texture:Dispose()  -> nil` | Geeft de textuur vrij. |

## Canvas en clipping

### CANVAS

Maakt schrijfbare pixeloppervlakken die Lua op de CPU bewerkt en als één textuur uploadt.

<div class="callout warn">
Een canvas is een textuur waarvan Lua de pixels zet (SetPixel, FillRect, ...) en daarna met Upload naar de GPU stuurt. Het is geschikt voor softwarerendering en gebakken UI-vormen, waarbij een script eenmaal schildert en uploadt en daarna elk frame het resultaat tekent. Nieuwe canvassen beginnen volledig transparant.
</div>

| Methode | Beschrijving |
| --- | --- |
| `CANVAS:CreateCanvas(width, height)  -> canvas` | Maakt een transparant canvas van de gegeven afmeting (minimaal 1x1). |

```lua
local panel

function onStart()
    panel = CANVAS:CreateCanvas(300, 80)
    panel:FillRect(0, 0, 300, 80, 20, 20, 40, 220)
    panel:FillCircle(40, 40, 24, 255, 200, 0, 255)
    panel:Upload()
end

function draw()
    panel:Draw(100, 100)
end

function onDestroy()
    panel:Dispose()
end
```

### Canvas-handle

Een schrijfbare RGBA-pixelbuffer, teruggegeven door `CANVAS:CreateCanvas`, tekenbaar zoals een textuur.

<div class="callout warn">
De kleurargumenten r, g, b, a zijn gehele getallen 0-255. Pixelbewerkingen stapelen zich op in een dirty-rechthoek en bereiken de GPU pas wanneer je Upload aanroept (BlitPacked uploadt zelf). Tekencoördinaten zijn gehele getallen. Ankernamen zijn dezelfde als bij de textuur-handle.
</div>

| Methode | Beschrijving |
| --- | --- |
| `canvas.Width  -> int` | Breedte in pixels. |
| `canvas.Height  -> int` | Hoogte in pixels. |
| `canvas.Pointer  -> int` | Native GL-textuur-id, of 0 als er geen is. |
| `canvas:SetPixel(x, y, r, g, b, a)  -> nil` | Zet één pixel. Het canvas negeert coördinaten buiten bereik. |
| `canvas:FillRect(x, y, w, h, r, g, b, a)  -> nil` | Vult een as-uitgelijnde rechthoek, geclipt op het canvas. |
| `canvas:FillCircle(cx, cy, radius, r, g, b, a)  -> nil` | Vult een schijf met de gegeven straal in pixels. |
| `canvas:StrokeLine(x0, y0, x1, y1, radius, r, g, b, a)  -> nil` | Schildert een dikke lijn als overlappende schijven met de gegeven straal. |
| `canvas:PasteTexture(texture, x, y)  -> nil` | Alfa-blendt een textuur op het canvas met de linkerbovenhoek op (x, y). Het canvas leest de pixels van de textuur eenmaal per textuur-handle van de GPU terug, een trage bewerking die in setupcode thuishoort. |
| `canvas:PasteTextureTransformed(texture, x, y, scale, rotationDeg, anchor)  -> nil` | Alfa-blendt een textuur geschaald met `scale` en met de klok mee geroteerd over `rotationDeg`, met nearest-neighbour-sampling. Met anker "center" landt het middelpunt van de textuur op (x, y); elke andere waarde plaatst de linkerbovenhoek daar. |
| `canvas:Clear(r, g, b, a)  -> nil` | Vult het hele canvas met één kleur. |
| `canvas:ClearTransparent()  -> nil` | Zet het hele canvas terug naar volledig transparant. |
| `canvas:CopyFrom(other)  -> nil` | Kopieert de pixels van een ander canvas met dezelfde afmeting naar dit canvas. De aanroep doet niets wanneer de afmetingen verschillen. |
| `canvas:BlitPacked(data, count)  -> nil` | Vult het oppervlak vanuit een 1-geïndexeerde Lua-array van gepackte `0xRRGGBB`-integers in rij-volgorde (pixels worden ondoorzichtig; een negatieve waarde wordt een transparante pixel), en uploadt daarna. |
| `canvas:Upload()  -> nil` | Stuurt de openstaande bewerkingen (alleen het gewijzigde gebied) naar de GPU. No-op wanneer er niets is veranderd. |
| `canvas:Draw(x, y)  -> nil` | Tekent het canvas met de linkerbovenhoek op (x, y). |
| `canvas:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | Tekent een bron-subrechthoek (in canvaspixels) met de linkerbovenhoek op (x, y). |
| `canvas:DrawAtAnchor(x, y, anchor)  -> nil` | Tekent het canvas zo dat het benoemde ankerpunt op (x, y) landt. |
| `canvas:SetScale(scale_x, scale_y)  -> nil` | Stelt de horizontale en verticale tekenschaal in. |
| `canvas:SetOpacity(opacity)  -> nil` | Stelt de dekking in, 0..1. |
| `canvas:SetColor(red, green, blue)  -> nil` | Stelt de tint in vanuit drie waarden 0..1. |
| `canvas:Dispose()  -> nil` | Geeft de GPU-textuur en de CPU-buffer van het canvas vrij. |

### GRAPHICS

Scissor-clipping voor scrollende panelen.

<div class="callout warn">
SetClip neemt logische schermcoördinaten en zet ze om naar de huidige viewport, zodat clipping hetzelfde werkt onder renderschaling en letterboxing. De GPU verwerpt elke pixel die tussen SetClip en ClearClip buiten de rechthoek wordt getekend. SetClip vervangt de vorige rechthoek (er is geen stack); houd SetClip en ClearClip gepaard. Het spel schakelt de scissor aan het begin van elk frame uit.
</div>

| Methode | Beschrijving |
| --- | --- |
| `GRAPHICS:SetClip(x, y, w, h)  -> nil` | Schakelt clipping op de gegeven rechthoek in. |
| `GRAPHICS:ClearClip()  -> nil` | Schakelt clipping uit. |

```lua
GRAPHICS:SetClip(100, 200, 600, 400)
for i, row in ipairs(rows) do
    row.tex:Draw(100, 200 + (i - 1) * 48 - scrollY)
end
GRAPHICS:ClearClip()
```

## Tekst

### TEXT

Maakt lettertyperenderers voor het hoofdlettertype van de skin.

<div class="callout warn">
Create geeft een tekst-handle terug die hele strings naar gecachte texturen rendert; gebruik die voor labels die zelden veranderen. CreateGlyphCached geeft een glyphtekst-handle terug die één textuur per teken cachet en strings bij het tekenen samenstelt; gebruik die voor tekst die vaak verandert (timers, scores, getypte invoer) en voor blokken met regelafbreking. Stijlargumenten zijn een van bold, italic, underline, strikeout (niet hoofdlettergevoelig; andere waarden betekenen regular). Binnen één script faalt de meegeleverde NLua-versie als het script dezelfde factorymethode eerst zonder stijlargument en later met precies één stijlargument aanroept. Geef ofwel nooit een stijl door, geef er vanaf de eerste aanroep altijd een door, of geef twee stijltokens door (bijvoorbeeld "bold", "regular").
</div>

| Methode | Beschrijving |
| --- | --- |
| `TEXT:Create(size, ...style)  -> text` | Maakt een renderer voor hele strings op de gegeven pixelgrootte. |
| `TEXT:CreateGlyphCached(size, ...style)  -> glyphText` | Maakt een uit glyphs samengestelde tekstrenderer op de gegeven pixelgrootte. |

#### Inline kleurtags

Beide renderers honoreren deze tags binnen de string. Tags kunnen nesten; een niet-gesloten tag loopt door tot het einde van de string. Meetfuncties negeren ze.

| Tag | Effect |
| --- | --- |
| `<c.#rrggbb>` ... `</c>` | Vulkleur. |
| `<c.#rrggbb.#rrggbb>` ... `</c>` | Vulkleur en contourkleur. |
| `<g.#rrggbb.#rrggbb>` ... `</g>` | Verticale gradiëntvulling (bovenkleur, onderkleur). |

### Tekst-handle

Een renderer voor hele strings, teruggegeven door `TEXT:Create`.

<div class="callout warn">
GetText en GetVerticalText cachen één textuur per unieke combinatie van string, vulkleur, contourkleur en afmetingslimiet, en bewaren die tot je de handle vrijgeeft. Een string die elk frame verandert, voegt daardoor elk frame een textuur toe en lekt GPU-geheugen; teken zulke tekst met DrawDirect of een glyphtekst-handle. forecolor en backcolor zijn COLOR-waarden; de standaardwaarden zijn een witte vulling en een ondoorzichtige zwarte contour. De vlag centered centreert elke regel van een meerregelige string; de cachesleutel sluit haar uit, dus de eerste aanroep voor een gegeven string bepaalt haar.
</div>

| Methode | Beschrijving |
| --- | --- |
| `text:GetText(text, centered, max_width, forecolor, backcolor)  -> texture` | Rendert een horizontale string naar een gecachte textuur. Alle argumenten na `text` zijn optioneel. Wanneer de gerenderde textuur breder is dan `max_width`, tekent ze horizontaal samengedrukt zodat ze past. |
| `text:GetVerticalText(text, centered, max_height, forecolor, backcolor)  -> texture` | Rendert een verticaal gestapelde string naar een gecachte textuur. Wanneer de gerenderde textuur hoger is dan `max_height`, tekent ze verticaal samengedrukt zodat ze past. |
| `text:DrawDirect(text, x, y, r, g, b, opacity, spacing)  -> number` | Tekent een string glyph voor glyph vanuit een cache per teken en geeft de x-positie na de laatste glyph terug. `r`, `g`, `b` zijn 0-255 (standaard 255), `opacity` is 0..1 (standaard 1), `spacing` is de ruimte tussen glyphs in pixels (standaard -6). |
| `text:Dispose()  -> nil` | Geeft het lettertype en elke gecachte textuur vrij. |

```lua
local font, title

function onStart()
    font = TEXT:Create(28)
    title = font:GetText("Song select", true, 600)
end

function draw()
    title:DrawAtAnchor(960, 80, "center")
    font:DrawDirect(tostring(score), 40, 40, 255, 255, 0)
end

function onDestroy()
    font:Dispose()
end
```

### Glyphtekst-handle

Een uit glyphs samengestelde renderer, teruggegeven door `TEXT:CreateGlyphCached`.

<div class="callout warn">
forecolor en backcolor zijn COLOR-waarden; de standaardwaarden zijn een witte vulling en een zwarte contour. Elk argument na de positie is optioneel. maxWidth (0 = geen limiet) drukt elke glyph horizontaal samen zodat de hele regel past. anchor gebruikt dezelfde negen namen als de textuur-handle. '\n' begint een nieuwe regel; Draw stapelt regels links uitgelijnd. Draw geeft de x-coördinaat van de rechterrand van het getekende vak terug. De vakgeometrie komt overeen met GetText-texturen (25 px opvulling aan elke kant en onder de inkt), zodat een glyphtekst en een GetText-textuur die op hetzelfde punt worden getekend, uitlijnen.
</div>

| Methode | Beschrijving |
| --- | --- |
| `glyphText.LineHeight  -> number` | Regelafstand van meerregelige tekst, in pixels. |
| `glyphText.BoxHeight  -> number` | Hoogte van een vak van één regel (inkt plus opvulling), overeenkomend met een GetText-textuur van één regel. |
| `glyphText:Measure(text, scale)  -> number` | Inktbreedte van de breedste regel, maal `scale` (standaard 1). |
| `glyphText:Draw(text, x, y, forecolor, backcolor, opacity, scale, maxWidth, anchor, scaleY, rotationDeg)  -> number` | Tekent de tekst. `opacity` 0..1 (standaard 1); `scale` (standaard 1); `scaleY` overschrijft de verticale schaal wanneer > 0; `rotationDeg` roteert het hele blok om (x, y). Geeft de x van de rechterrand van het vak terug. |
| `glyphText:SetClipY(y0, y1)  -> nil` | Beperkt volgende rechtopstaande Draw-aanroepen tot de verticale band y0..y1 in schermruimte; Draw slaat glyphs buiten de band over en snijdt glyphs op de randen ervan af. Geef y1 <= y0 door om te wissen. Geroteerde tekenaanroepen negeren de band. |
| `glyphText:WrapToLines(text, wrapWidth, scale)  -> string[]` | Breekt regels af op `wrapWidth` en geeft de regels terug als gewone strings (tags verwijderd). |
| `glyphText:MeasureWrapped(text, wrapWidth, scale, lineSpacing)  -> number` | Hoogte die DrawWrapped zou gebruiken, zonder te tekenen. `lineSpacing` vermenigvuldigt de regelafstand (standaard 1). |
| `glyphText:DrawWrapped(text, x, y, wrapWidth, forecolor, backcolor, opacity, scale, lineSpacing)  -> number` | Breekt regels af op `wrapWidth` en tekent links uitgelijnd vanaf (x, y). Geeft de getekende hoogte terug. |
| `glyphText:Dispose()  -> nil` | Geeft het lettertype en elke gecachte glyph vrij. |

Regelafbreking breekt af op spaties, tussen CJK-tekens, en binnen een woord dat breder is dan de afbreekbreedte.

```lua
local font, white

function onStart()
    font = TEXT:CreateGlyphCached(24)
    white = COLOR:CreateColorFromRGBA(255, 255, 255)
end

function draw()
    font:Draw("Time " .. string.format("%.1f", t), 960, 40, white, nil, 1, 1, 0, "center")
    font:DrawWrapped(description, 200, 300, 800, white)
end

function onDestroy()
    font:Dispose()
end
```

## Video

### VIDEO

Laadt videobestanden in afspeelbare decoder-handles.

<div class="callout warn">
Paden zijn relatief ten opzichte van de scriptmap. De decoder opent op een achtergrondthread; tot hij klaar is, tekent de handle niets, zet hij aanroepen van Start, zoeken en snelheid in de wachtrij, en past hij ze toe zodra de decoder is gekoppeld. Een ontbrekend bestand geeft een lege handle terug.
</div>

| Methode | Beschrijving |
| --- | --- |
| `VIDEO:CreateVideo(path)  -> video` | Opent een videobestand en geeft zijn handle terug. |

### Video-handle

Een videodecoder, teruggegeven door `VIDEO:CreateVideo`.

<div class="callout warn">
Posities zijn in milliseconden; Duration is in seconden. Lees elk frame Texture om het huidige frame als textuur-handle te krijgen. Die handle omhult de eigen frametextuur van de video: teken hem en laat het vrijgeven over aan de video-handle. SetSpeed negeert waarden van 0 of lager.
</div>

| Methode | Beschrijving |
| --- | --- |
| `video:Start()  -> nil` | Start het afspelen. |
| `video:Resume()  -> nil` | Hervat het afspelen na Pause. |
| `video:Pause()  -> nil` | Pauzeert het afspelen. |
| `video:Stop()  -> nil` | Stopt het afspelen. |
| `video:Reset()  -> nil` | Springt terug naar het begin. |
| `video.Width  -> int` | Framebreedte in pixels, of -1 tot de decoder klaar is. |
| `video.Height  -> int` | Framehoogte in pixels, of -1 tot de decoder klaar is. |
| `video.Duration  -> number` | Totale duur in seconden (1 tot de decoder klaar is). |
| `video.DurationMs  -> number` | Totale duur in milliseconden. |
| `video.Texture  -> texture` | Het huidige gedecodeerde frame. |
| `video:IsFinished()  -> bool` | True zodra de stream is geëindigd en er geen frames meer over zijn. |
| `video:GetTimestampMs()  -> number` | Huidige afspeelpositie in milliseconden. |
| `video:SetTimestampMs(ms)  -> nil` | Springt naar een positie in milliseconden. |
| `video:GetSpeed()  -> number` | Huidige afspeelsnelheidsfactor (1 = normaal). |
| `video:SetSpeed(speed)  -> nil` | Stelt de afspeelsnelheidsfactor in. |
| `video:GetPlayPosition()  -> number` | Huidige positie in seconden (oudere naam voor GetTimestampMs / 1000). |
| `video:SetPlayPosition(seconds)  -> nil` | Springt naar een positie in seconden (oudere naam voor SetTimestampMs). |
| `video:GetPlaySpeed()  -> number` | Oudere naam voor GetSpeed. |
| `video:SetPlaySpeed(speed)  -> nil` | Oudere naam voor SetSpeed. |
| `video:Dispose()  -> nil` | Geeft de decoder en zijn frametextuur vrij. |

```lua
local intro

function onStart()
    intro = VIDEO:CreateVideo("Videos/intro.mp4")
end

function activate()
    intro:Start()
end

function draw()
    intro.Texture:Draw(0, 0)
    if intro:IsFinished() then Exit("title", nil) end
end

function deactivate()
    intro:Stop()
end

function onDestroy()
    intro:Dispose()
end
```

## Kleur, gradiënt en afmeting

### COLOR

Maakt kleurwaarden.

<div class="callout warn">
Kanalen zijn 0-255. De textuur-, canvas- en tekstmethoden die een kleur nemen, accepteren deze waarden.
</div>

| Methode | Beschrijving |
| --- | --- |
| `COLOR:CreateColorFromRGBA(r, g, b, a)  -> color` | Maakt een kleur uit rood, groen, blauw en optioneel alfa (standaard 255). |
| `COLOR:CreateColorFromARGB(a, r, g, b)  -> color` | Maakt een kleur uit alfa, rood, groen en blauw. |
| `COLOR:CreateColorFromHex(value)  -> color` | Parseert een hexadecimale `AARRGGBB`-string zonder voorvoegsel (bijvoorbeeld `"ff2080ff"`). Een string van zes cijfers levert alfa 0 op. Een niet-parseerbare string levert ondoorzichtig wit op. |

### Color-handle

Een kleurwaarde, teruggegeven door de `COLOR`-factory.

| Methode | Beschrijving |
| --- | --- |
| `color.R  -> int` | Roodkanaal, 0-255 (lezen/schrijven). |
| `color.G  -> int` | Groenkanaal, 0-255 (lezen/schrijven). |
| `color.B  -> int` | Blauwkanaal, 0-255 (lezen/schrijven). |
| `color.A  -> int` | Alfakanaal, 0-255 (lezen/schrijven). |

### GRADIENT

Gradiëntmaps herkleuren textuurtekeningen door de luminantie van elke pixel op een kleurverloop af te beelden, ofwel op één textuur ofwel op elke tekenaanroep.

<div class="callout warn">
Create neemt een tabel van minstens twee stops, elk een array { positie 0-1, r 0-255, g 0-255, b 0-255 [, a 0-255] }, en een blendhoeveelheid (0 = geen effect, 1 = volledige vervanging, tussenliggende waarden mengen; standaard 1). Minder dan twee stops werpt een fout op. Identieke stops en blend hergebruiken één gecachte GPU-textuur voor het hele programma. Pas toe op één textuur met texture:SetGradientMap, of op elke textuurtekening met SetActive.
</div>

| Methode | Beschrijving |
| --- | --- |
| `GRADIENT:Create(stops, blend)  -> gradient` | Bouwt (of hergebruikt) een gradiëntmap uit kleurstops en een blendhoeveelheid. |
| `GRADIENT:SetActive(gradient)  -> nil` | Past de gradiënt toe op elke textuurtekening tot ClearActive. |
| `GRADIENT:ClearActive()  -> nil` | Verwijdert de globale gradiëntmap. |

```lua
local sepia

function onStart()
    sepia = GRADIENT:Create({
        { 0.0,  40,  20,   0 },
        { 1.0, 255, 230, 180 },
    }, 0.8)
end

function draw()
    GRADIENT:SetActive(sepia)
    background:Draw(0, 0)
    GRADIENT:ClearActive()
end

function onDestroy()
    sepia:Dispose()
end
```

### Gradiënt-handle

Een gradiëntmap, teruggegeven door `GRADIENT:Create`.

| Methode | Beschrijving |
| --- | --- |
| `gradient.BlendStrength  -> number` | Blendhoeveelheid, 0..1 (alleen-lezen). |
| `gradient:Dispose()  -> nil` | Geeft de handle vrij. De gedeelde GPU-textuur blijft gecachet. |

### SIZE

Maakt breedte/hoogte-waardeobjecten.

| Methode | Beschrijving |
| --- | --- |
| `SIZE:CreateSize(width, height)  -> size` | Maakt een afmetingsobject. |

### Size-handle

Een paar van breedte en hoogte, teruggegeven door `SIZE:CreateSize`.

| Methode | Beschrijving |
| --- | --- |
| `size.Width  -> int` | Breedte (lezen/schrijven). |
| `size.Height  -> int` | Hoogte (lezen/schrijven). |
