<!-- api/graphics.md -->

# Grafik und Text

Texturen, Canvases, Clipping, Text, Video, Farben, Gradient-Maps und Größen. Alle Positionen und Größen sind in logischen Bildschirmpixeln, dem Koordinatenraum, in dem Skripte zeichnen (siehe `INPUT:GetSurfaceWidth()` / `GetSurfaceHeight()`).

Das Skript, das ein Textur-, Canvas-, Text-, Video- oder Gradient-Handle erzeugt, besitzt es, und das Spiel gibt das Handle frei, wenn es dieses Skript entlädt. Rufen Sie `Dispose()` selbst auf, um ein Handle früher freizugeben.

## Texturen

### TEXTURE

Lädt Bilddateien in Textur-Handles.

<div class="callout warn">
Relative Pfade werden gegen das Verzeichnis des Skripts aufgelöst; die FromAbsolutePath-Varianten nehmen einen vollständigen Pfad. Eine fehlende Datei liefert ein leeres Handle, das nichts zeichnet. CreateTexture lädt asynchron: Das Handle zeichnet nichts und meldet Width und Height von 0, bis das Dekodieren und Hochladen im Hintergrund abgeschlossen sind. Verwenden Sie CreateTextureSync, wenn Sie die Größe oder die Pixel sofort benötigen. Die Optionstabelle akzeptiert { maxSize = N }, um das Bild beim Dekodieren so zu verkleinern, dass seine längste Seite höchstens N Pixel misst.
</div>

| Methode | Beschreibung |
| --- | --- |
| `TEXTURE:CreateTexture()  -> texture` | Erzeugt ein leeres Handle ohne Bild. |
| `TEXTURE:CreateTexture(path)  -> texture` | Lädt ein Bild asynchron aus einem Pfad relativ zum Skriptverzeichnis. |
| `TEXTURE:CreateTexture(path, options)  -> texture` | Dasselbe, mit einer Optionstabelle (`{ maxSize = N }`). |
| `TEXTURE:CreateTextureSync(path)  -> texture` | Lädt ein Bild synchron; Größe und Pixel stehen bei Rückkehr zur Verfügung. |
| `TEXTURE:CreateTextureFromAbsolutePath(path)  -> texture` | Lädt ein Bild aus einem vollständigen Dateisystempfad. |
| `TEXTURE:CreateTextureFromAbsolutePath(path, options)  -> texture` | Dasselbe, mit einer Optionstabelle (`{ maxSize = N }`). |
| `TEXTURE:Exists(path)  -> bool` | Gibt zurück, ob unter dem Pfad relativ zum Skriptverzeichnis eine Datei existiert. |

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

### Textur-Handle

Ein zeichenbares 2D-Bild, das von der Fabrik `TEXTURE`, von `text:GetText` / `GetVerticalText` und von `video.Texture` zurückgegeben wird.

<div class="callout warn">
Ankernamen: topleft, top, topright, left, center, right, bottomleft, bottom, bottomright (Groß-/Kleinschreibung egal; ein unbekannter Name fällt auf topleft zurück). Verankertes Zeichnen berücksichtigt die aktuelle Skalierung. Namen der Blend-Modi: Normal, Add, Multi, Sub, Screen. Namen der Wrap-Modi: Edge, Border, Repeat, Mirror; neue Handles verwenden standardmäßig Repeat. Auf einem leeren Handle ist jede Methode ein No-op, und die Getter liefern die unten aufgeführten Standardwerte.
</div>

| Methode | Beschreibung |
| --- | --- |
| `texture:Draw(x, y)  -> nil` | Zeichnet die gesamte Textur mit ihrer linken oberen Ecke bei (x, y). |
| `texture:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | Zeichnet einen Quell-Teilbereich (in Texturpixeln) mit seiner linken oberen Ecke bei (x, y). |
| `texture:DrawAtAnchor(x, y, anchor)  -> nil` | Zeichnet die gesamte Textur so, dass der benannte Ankerpunkt auf (x, y) liegt. |
| `texture:DrawRectAtAnchor(x, y, rect_x, rect_y, rect_width, rect_height, anchor)  -> nil` | Zeichnet einen Quell-Teilbereich so, dass der benannte Ankerpunkt auf (x, y) liegt. |
| `texture.Loaded  -> bool` | True, wenn das Handle eine Textur kapselt. Das Spiel setzt es, sobald es die Datei findet, noch bevor ein asynchroner Ladevorgang abgeschlossen ist. |
| `texture.Width  -> int` | Pixelbreite; -1 auf einem leeren Handle, 0 solange ein asynchroner Ladevorgang aussteht. |
| `texture.Height  -> int` | Pixelhöhe; -1 auf einem leeren Handle, 0 solange ein asynchroner Ladevorgang aussteht. |
| `texture.Pointer  -> int` | Native GL-Textur-ID, oder 0, falls keine. |
| `texture:GetScale()  -> vector2` | Aktuelle Zeichenskalierung als vector2 (`X`, `Y`). |
| `texture:GetOpacity()  -> number` | Aktuelle Deckkraft, 0..1; -1 auf einem leeren Handle. |
| `texture:GetColor()  -> number, number, number` | Aktuelle Einfärbung als drei Werte 0..1 (Rot, Grün, Blau). |
| `texture:GetRotation()  -> number` | Aktuelle Rotation in Grad. |
| `texture:GetBlendMode()  -> string` | Name des aktuellen Blend-Modus. |
| `texture:GetWrapMode()  -> string` | Name des aktuellen Wrap-Modus. |
| `texture:SetScale(scale_x, scale_y)  -> nil` | Setzt die horizontale und vertikale Zeichenskalierung (1 = Originalgröße). |
| `texture:SetOpacity(opacity)  -> nil` | Setzt die Deckkraft, 0..1. |
| `texture:SetColor(color)  -> nil` | Setzt die Einfärbung aus einem `COLOR`-Wert. Das Handle ignoriert das Alpha der Farbe; setzen Sie die Transparenz mit SetOpacity. |
| `texture:SetColor(red, green, blue)  -> nil` | Setzt die Einfärbung aus drei Werten 0..1. |
| `texture:SetRotation(angle)  -> nil` | Setzt die Rotation um den Texturmittelpunkt, in Grad. |
| `texture:SetBlendMode(mode)  -> nil` | Setzt den Blend-Modus per Name (Groß-/Kleinschreibung egal). Unbekannte Namen ignoriert das Handle. |
| `texture:SetWrapMode(mode)  -> nil` | Setzt den Wrap-Modus per Name (Groß-/Kleinschreibung egal). Unbekannte Namen ignoriert das Handle. |
| `texture:SetUseNoiseEffect(enabled)  -> nil` | Wenn aktiviert, ersetzt jeder Zeichenvorgang die Farbe der Textur durch animiertes zufälliges Graustufenrauschen und behält ihr Alpha bei. |
| `texture:SetGradientMap(gradient)  -> nil` | Wendet eine `GRADIENT`-Map auf jedes Zeichnen dieser Textur an (siehe GRADIENT). |
| `texture:ClearGradientMap()  -> nil` | Entfernt die texturspezifische Gradient-Map. |
| `texture:Dispose()  -> nil` | Gibt die Textur frei. |

## Canvas und Clipping

### CANVAS

Erzeugt beschreibbare Pixelflächen, die Lua auf der CPU bearbeitet und als eine Textur hochlädt.

<div class="callout warn">
Ein Canvas ist eine Textur, deren Pixel Lua setzt (SetPixel, FillRect, ...) und dann mit Upload zur GPU überträgt. Es eignet sich für Software-Rendering und gebackene UI-Formen, bei denen ein Skript einmal malt und hochlädt und das Ergebnis dann jeden Frame zeichnet. Neue Canvases beginnen vollständig transparent.
</div>

| Methode | Beschreibung |
| --- | --- |
| `CANVAS:CreateCanvas(width, height)  -> canvas` | Erzeugt ein transparentes Canvas der angegebenen Größe (mindestens 1x1). |

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

### Canvas-Handle

Ein beschreibbarer RGBA-Pixelpuffer, der von `CANVAS:CreateCanvas` zurückgegeben wird und wie eine Textur zeichenbar ist.

<div class="callout warn">
Die Farbargumente r, g, b, a sind Ganzzahlen von 0 bis 255. Pixeländerungen sammeln sich in einem Dirty-Rechteck und erreichen die GPU erst, wenn Sie Upload aufrufen (BlitPacked lädt selbst hoch). Zeichenkoordinaten sind Ganzzahlen. Die Ankernamen sind dieselben wie beim Textur-Handle.
</div>

| Methode | Beschreibung |
| --- | --- |
| `canvas.Width  -> int` | Breite in Pixeln. |
| `canvas.Height  -> int` | Höhe in Pixeln. |
| `canvas.Pointer  -> int` | Native GL-Textur-ID, oder 0, falls keine. |
| `canvas:SetPixel(x, y, r, g, b, a)  -> nil` | Setzt ein Pixel. Koordinaten außerhalb des Bereichs ignoriert das Canvas. |
| `canvas:FillRect(x, y, w, h, r, g, b, a)  -> nil` | Füllt ein achsenparalleles Rechteck, beschnitten auf das Canvas. |
| `canvas:FillCircle(cx, cy, radius, r, g, b, a)  -> nil` | Füllt eine Scheibe mit dem angegebenen Radius in Pixeln. |
| `canvas:StrokeLine(x0, y0, x1, y1, radius, r, g, b, a)  -> nil` | Malt eine dicke Linie als überlappende Scheiben des angegebenen Radius. |
| `canvas:PasteTexture(texture, x, y)  -> nil` | Blendet eine Textur per Alpha auf das Canvas, mit ihrer linken oberen Ecke bei (x, y). Das Canvas liest die Pixel der Textur einmal pro Textur-Handle von der GPU zurück, ein langsamer Vorgang, der in den Initialisierungscode gehört. |
| `canvas:PasteTextureTransformed(texture, x, y, scale, rotationDeg, anchor)  -> nil` | Blendet eine um `scale` skalierte und um `rotationDeg` im Uhrzeigersinn gedrehte Textur per Alpha ein, mit Nearest-Neighbour-Abtastung. Mit anchor "center" liegt der Texturmittelpunkt auf (x, y); jeder andere Wert platziert dort die linke obere Ecke. |
| `canvas:Clear(r, g, b, a)  -> nil` | Füllt das gesamte Canvas mit einer Farbe. |
| `canvas:ClearTransparent()  -> nil` | Setzt das gesamte Canvas auf vollständig transparent zurück. |
| `canvas:CopyFrom(other)  -> nil` | Kopiert die Pixel eines anderen Canvas gleicher Größe in dieses. Der Aufruf tut nichts, wenn die Größen abweichen. |
| `canvas:BlitPacked(data, count)  -> nil` | Füllt die Fläche aus einem 1-indizierten Lua-Array gepackter `0xRRGGBB`-Ganzzahlen in Zeilenreihenfolge (Pixel werden deckend; ein negativer Wert wird zu einem transparenten Pixel) und lädt dann hoch. |
| `canvas:Upload()  -> nil` | Überträgt ausstehende Änderungen (nur den geänderten Bereich) zur GPU. No-op, wenn sich nichts geändert hat. |
| `canvas:Draw(x, y)  -> nil` | Zeichnet das Canvas mit seiner linken oberen Ecke bei (x, y). |
| `canvas:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | Zeichnet einen Quell-Teilbereich (in Canvas-Pixeln) mit seiner linken oberen Ecke bei (x, y). |
| `canvas:DrawAtAnchor(x, y, anchor)  -> nil` | Zeichnet das Canvas so, dass der benannte Ankerpunkt auf (x, y) liegt. |
| `canvas:SetScale(scale_x, scale_y)  -> nil` | Setzt die horizontale und vertikale Zeichenskalierung. |
| `canvas:SetOpacity(opacity)  -> nil` | Setzt die Deckkraft, 0..1. |
| `canvas:SetColor(red, green, blue)  -> nil` | Setzt die Einfärbung aus drei Werten 0..1. |
| `canvas:Dispose()  -> nil` | Gibt die GPU-Textur und den CPU-Puffer des Canvas frei. |

### GRAPHICS

Scissor-Clipping für scrollende Panels.

<div class="callout warn">
SetClip nimmt logische Bildschirmkoordinaten und bildet sie auf den aktuellen Viewport ab, sodass das Clipping unter Render-Skalierung und Letterboxing gleich ist. Die GPU verwirft jedes Pixel, das zwischen SetClip und ClearClip außerhalb des Rechtecks gezeichnet wird. SetClip ersetzt das vorherige Rechteck (es gibt keinen Stack); halten Sie SetClip und ClearClip paarweise. Das Spiel deaktiviert den Scissor zu Beginn jedes Frames.
</div>

| Methode | Beschreibung |
| --- | --- |
| `GRAPHICS:SetClip(x, y, w, h)  -> nil` | Aktiviert das Clipping auf das angegebene Rechteck. |
| `GRAPHICS:ClearClip()  -> nil` | Deaktiviert das Clipping. |

```lua
GRAPHICS:SetClip(100, 200, 600, 400)
for i, row in ipairs(rows) do
    row.tex:Draw(100, 200 + (i - 1) * 48 - scrollY)
end
GRAPHICS:ClearClip()
```

## Text

### TEXT

Erzeugt Schrift-Renderer für die Hauptschrift des Skins.

<div class="callout warn">
Create gibt ein Text-Handle zurück, das ganze Strings in zwischengespeicherte Texturen rendert; verwenden Sie es für Beschriftungen, die sich selten ändern. CreateGlyphCached gibt ein Glyphtext-Handle zurück, das eine Textur pro Zeichen zwischenspeichert und Strings beim Zeichnen zusammensetzt; verwenden Sie es für Text, der sich oft ändert (Timer, Scores, getippte Eingaben), und für Blöcke mit Zeilenumbruch. Stilargumente sind beliebige aus bold, italic, underline, strikeout (Groß-/Kleinschreibung egal; andere Werte bedeuten regular). Innerhalb eines Skripts scheitert die mitgelieferte NLua-Version, wenn das Skript dieselbe Fabrikmethode zuerst ohne Stilargument und später mit genau einem Stilargument aufruft. Übergeben Sie entweder nie einen Stil, immer einen ab dem ersten Aufruf, oder zwei Stil-Tokens (zum Beispiel "bold", "regular").
</div>

| Methode | Beschreibung |
| --- | --- |
| `TEXT:Create(size, ...style)  -> text` | Erzeugt einen Ganzstring-Text-Renderer mit der angegebenen Pixelgröße. |
| `TEXT:CreateGlyphCached(size, ...style)  -> glyphText` | Erzeugt einen aus Glyphen zusammengesetzten Text-Renderer mit der angegebenen Pixelgröße. |

#### Inline-Farb-Tags

Beide Renderer beachten diese Tags innerhalb des Strings. Tags können verschachtelt werden; ein nicht geschlossenes Tag gilt bis zum Ende des Strings. Messfunktionen ignorieren sie.

| Tag | Wirkung |
| --- | --- |
| `<c.#rrggbb>` ... `</c>` | Füllfarbe. |
| `<c.#rrggbb.#rrggbb>` ... `</c>` | Füllfarbe und Umrissfarbe. |
| `<g.#rrggbb.#rrggbb>` ... `</g>` | Vertikaler Verlauf als Füllung (obere Farbe, untere Farbe). |

### Text-Handle

Ein Ganzstring-Renderer, der von `TEXT:Create` zurückgegeben wird.

<div class="callout warn">
GetText und GetVerticalText speichern eine Textur pro eindeutiger Kombination aus String, Füllfarbe, Umrissfarbe und Größenlimit zwischen und behalten sie, bis Sie das Handle freigeben. Ein String, der sich jeden Frame ändert, fügt daher jeden Frame eine Textur hinzu und leckt GPU-Speicher; zeichnen Sie solchen Text mit DrawDirect oder einem Glyphtext-Handle. forecolor und backcolor sind COLOR-Werte; die Standardwerte sind eine weiße Füllung und ein deckender schwarzer Umriss. Das Flag centered zentriert jede Zeile eines mehrzeiligen Strings; der Cache-Schlüssel schließt es aus, sodass der erste Aufruf für einen bestimmten String darüber entscheidet.
</div>

| Methode | Beschreibung |
| --- | --- |
| `text:GetText(text, centered, max_width, forecolor, backcolor)  -> texture` | Rendert einen horizontalen String in eine zwischengespeicherte Textur. Alle Argumente nach `text` sind optional. Ist die gerenderte Textur breiter als `max_width`, erscheint sie beim Zeichnen horizontal gestaucht, damit sie passt. |
| `text:GetVerticalText(text, centered, max_height, forecolor, backcolor)  -> texture` | Rendert einen vertikal gestapelten String in eine zwischengespeicherte Textur. Ist die gerenderte Textur höher als `max_height`, erscheint sie beim Zeichnen vertikal gestaucht, damit sie passt. |
| `text:DrawDirect(text, x, y, r, g, b, opacity, spacing)  -> number` | Zeichnet einen String Glyphe für Glyphe aus einem Zeichen-Cache und gibt die x-Position nach der letzten Glyphe zurück. `r`, `g`, `b` sind 0-255 (Standard 255), `opacity` ist 0..1 (Standard 1), `spacing` ist der Abstand zwischen Glyphen in Pixeln (Standard -6). |
| `text:Dispose()  -> nil` | Gibt die Schrift und jede zwischengespeicherte Textur frei. |

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

### Glyphtext-Handle

Ein aus Glyphen zusammengesetzter Renderer, der von `TEXT:CreateGlyphCached` zurückgegeben wird.

<div class="callout warn">
forecolor und backcolor sind COLOR-Werte; die Standardwerte sind eine weiße Füllung und ein schwarzer Umriss. Jedes Argument nach der Position ist optional. maxWidth (0 = kein Limit) staucht jede Glyphe horizontal, damit die gesamte Zeile passt. anchor verwendet dieselben neun Namen wie das Textur-Handle. '\n' beginnt eine neue Zeile; Draw stapelt Zeilen linksbündig. Draw gibt die x-Koordinate der rechten Kante des gezeichneten Kastens zurück. Die Kastengeometrie entspricht GetText-Texturen (25 px Abstand auf jeder Seite und unterhalb der Schrift), sodass ein Glyphtext und eine GetText-Textur, die am selben Punkt gezeichnet werden, bündig liegen.
</div>

| Methode | Beschreibung |
| --- | --- |
| `glyphText.LineHeight  -> number` | Zeilenabstand mehrzeiligen Texts, in Pixeln. |
| `glyphText.BoxHeight  -> number` | Höhe eines einzeiligen Kastens (Schrift plus Abstand), passend zu einer einzeiligen GetText-Textur. |
| `glyphText:Measure(text, scale)  -> number` | Schriftbreite der breitesten Zeile, mal `scale` (Standard 1). |
| `glyphText:Draw(text, x, y, forecolor, backcolor, opacity, scale, maxWidth, anchor, scaleY, rotationDeg)  -> number` | Zeichnet den Text. `opacity` 0..1 (Standard 1); `scale` (Standard 1); `scaleY` überschreibt, wenn > 0, die vertikale Skalierung; `rotationDeg` dreht den gesamten Block um (x, y). Gibt die rechte Kante x des Kastens zurück. |
| `glyphText:SetClipY(y0, y1)  -> nil` | Beschränkt nachfolgende aufrechte Draw-Aufrufe auf das vertikale Band y0..y1 im Bildschirmraum; Draw überspringt Glyphen außerhalb des Bandes und schneidet Glyphen an seinen Rändern an. Übergeben Sie y1 <= y0 zum Aufheben. Gedrehte Zeichenaufrufe ignorieren das Band. |
| `glyphText:WrapToLines(text, wrapWidth, scale)  -> string[]` | Bricht bei `wrapWidth` um und gibt die Zeilen als reine Strings zurück (Tags entfernt). |
| `glyphText:MeasureWrapped(text, wrapWidth, scale, lineSpacing)  -> number` | Höhe, die DrawWrapped verwenden würde, ohne zu zeichnen. `lineSpacing` multipliziert den Zeilenabstand (Standard 1). |
| `glyphText:DrawWrapped(text, x, y, wrapWidth, forecolor, backcolor, opacity, scale, lineSpacing)  -> number` | Bricht bei `wrapWidth` um und zeichnet linksbündig ab (x, y). Gibt die gezeichnete Höhe zurück. |
| `glyphText:Dispose()  -> nil` | Gibt die Schrift und jede zwischengespeicherte Glyphe frei. |

Der Zeilenumbruch bricht an Leerzeichen, zwischen CJK-Zeichen und innerhalb eines Wortes, das breiter als die Umbruchbreite ist.

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

Lädt Videodateien in abspielbare Decoder-Handles.

<div class="callout warn">
Pfade sind relativ zum Skriptverzeichnis. Der Decoder öffnet auf einem Hintergrund-Thread; bis er bereit ist, zeichnet das Handle nichts, reiht Aufrufe von Start, Suchen und Geschwindigkeit ein und wendet sie an, sobald der Decoder angebunden ist. Eine fehlende Datei liefert ein leeres Handle.
</div>

| Methode | Beschreibung |
| --- | --- |
| `VIDEO:CreateVideo(path)  -> video` | Öffnet eine Videodatei und gibt ihr Handle zurück. |

### Video-Handle

Ein Video-Decoder, der von `VIDEO:CreateVideo` zurückgegeben wird.

<div class="callout warn">
Positionen sind in Millisekunden; Duration ist in Sekunden. Lesen Sie Texture jeden Frame, um das aktuelle Bild als Textur-Handle zu erhalten. Dieses Handle kapselt die eigene Bildtextur des Videos: Zeichnen Sie es und überlassen Sie die Freigabe dem Video-Handle. SetSpeed ignoriert Werte von 0 oder darunter.
</div>

| Methode | Beschreibung |
| --- | --- |
| `video:Start()  -> nil` | Startet die Wiedergabe. |
| `video:Resume()  -> nil` | Setzt die Wiedergabe nach Pause fort. |
| `video:Pause()  -> nil` | Pausiert die Wiedergabe. |
| `video:Stop()  -> nil` | Stoppt die Wiedergabe. |
| `video:Reset()  -> nil` | Springt zurück zum Anfang. |
| `video.Width  -> int` | Bildbreite in Pixeln, oder -1, bis der Decoder bereit ist. |
| `video.Height  -> int` | Bildhöhe in Pixeln, oder -1, bis der Decoder bereit ist. |
| `video.Duration  -> number` | Gesamtdauer in Sekunden (1, bis der Decoder bereit ist). |
| `video.DurationMs  -> number` | Gesamtdauer in Millisekunden. |
| `video.Texture  -> texture` | Das aktuell dekodierte Bild. |
| `video:IsFinished()  -> bool` | True, sobald der Stream beendet ist und keine Bilder mehr verbleiben. |
| `video:GetTimestampMs()  -> number` | Aktuelle Wiedergabeposition in Millisekunden. |
| `video:SetTimestampMs(ms)  -> nil` | Springt zu einer Position in Millisekunden. |
| `video:GetSpeed()  -> number` | Aktueller Geschwindigkeitsfaktor der Wiedergabe (1 = normal). |
| `video:SetSpeed(speed)  -> nil` | Setzt den Geschwindigkeitsfaktor der Wiedergabe. |
| `video:GetPlayPosition()  -> number` | Aktuelle Position in Sekunden (älterer Name für GetTimestampMs / 1000). |
| `video:SetPlayPosition(seconds)  -> nil` | Springt zu einer Position in Sekunden (älterer Name für SetTimestampMs). |
| `video:GetPlaySpeed()  -> number` | Älterer Name für GetSpeed. |
| `video:SetPlaySpeed(speed)  -> nil` | Älterer Name für SetSpeed. |
| `video:Dispose()  -> nil` | Gibt den Decoder und seine Bildtextur frei. |

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

## Farbe, Verlauf und Größe

### COLOR

Erzeugt Farbwerte.

<div class="callout warn">
Kanäle sind 0-255. Die Textur-, Canvas- und Textmethoden, die eine Farbe nehmen, akzeptieren diese Werte.
</div>

| Methode | Beschreibung |
| --- | --- |
| `COLOR:CreateColorFromRGBA(r, g, b, a)  -> color` | Erzeugt eine Farbe aus Rot, Grün, Blau und optionalem Alpha (Standard 255). |
| `COLOR:CreateColorFromARGB(a, r, g, b)  -> color` | Erzeugt eine Farbe aus Alpha, Rot, Grün und Blau. |
| `COLOR:CreateColorFromHex(value)  -> color` | Parst einen hexadezimalen `AARRGGBB`-String ohne Präfix (zum Beispiel `"ff2080ff"`). Ein sechsstelliger String ergibt Alpha 0. Ein nicht parsbarer String ergibt deckendes Weiß. |

### Color-Handle

Ein Farbwert, der von der Fabrik `COLOR` zurückgegeben wird.

| Methode | Beschreibung |
| --- | --- |
| `color.R  -> int` | Rotkanal, 0-255 (les-/schreibbar). |
| `color.G  -> int` | Grünkanal, 0-255 (les-/schreibbar). |
| `color.B  -> int` | Blaukanal, 0-255 (les-/schreibbar). |
| `color.A  -> int` | Alphakanal, 0-255 (les-/schreibbar). |

### GRADIENT

Gradient-Maps färben Texturzeichnungen um, indem sie die Leuchtdichte jedes Pixels auf eine Farbrampe abbilden, entweder auf einer Textur oder auf jeder Zeichnung.

<div class="callout warn">
Create nimmt eine Tabelle mit mindestens zwei Stützstellen, jede ein Array { Position 0-1, r 0-255, g 0-255, b 0-255 [, a 0-255] }, sowie eine Mischstärke (0 = keine Wirkung, 1 = vollständiger Ersatz, Werte dazwischen mischen; Standard 1). Weniger als zwei Stützstellen lösen einen Fehler aus. Identische Stützstellen und Mischstärke verwenden für das gesamte Programm eine gemeinsame zwischengespeicherte GPU-Textur. Wenden Sie sie mit texture:SetGradientMap auf eine einzelne Textur an oder mit SetActive auf jede Texturzeichnung.
</div>

| Methode | Beschreibung |
| --- | --- |
| `GRADIENT:Create(stops, blend)  -> gradient` | Baut (oder verwendet wieder) eine Gradient-Map aus Farbstützstellen und einer Mischstärke. |
| `GRADIENT:SetActive(gradient)  -> nil` | Wendet den Verlauf auf jede Texturzeichnung an, bis ClearActive aufgerufen wird. |
| `GRADIENT:ClearActive()  -> nil` | Entfernt die globale Gradient-Map. |

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

### Gradient-Handle

Eine Gradient-Map, die von `GRADIENT:Create` zurückgegeben wird.

| Methode | Beschreibung |
| --- | --- |
| `gradient.BlendStrength  -> number` | Mischstärke, 0..1 (nur lesbar). |
| `gradient:Dispose()  -> nil` | Gibt das Handle frei. Die gemeinsam genutzte GPU-Textur bleibt zwischengespeichert. |

### SIZE

Erzeugt Breite/Höhe-Wertobjekte.

| Methode | Beschreibung |
| --- | --- |
| `SIZE:CreateSize(width, height)  -> size` | Erzeugt ein Größenobjekt. |

### Size-Handle

Ein Paar aus Breite und Höhe, das von `SIZE:CreateSize` zurückgegeben wird.

| Methode | Beschreibung |
| --- | --- |
| `size.Width  -> int` | Breite (les-/schreibbar). |
| `size.Height  -> int` | Höhe (les-/schreibbar). |
