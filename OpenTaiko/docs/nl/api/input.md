<!-- api/input.md -->

# Invoer

De druminvoer, het toetsenbord en de muis lezen, en tekstinvoer op het scherm.

## INPUT

Leest de ingestelde druminvoer, het ruwe toetsenbord en de muis, en maakt tekstinvoerwidgets. Beschikbaar in elk script.

<div class="callout warn">
Namen van druminvoer zijn de namen uit de toetsconfiguratie, vergeleken zonder onderscheid tussen hoofdletters en kleine letters: LRed, RRed, LBlue, RBlue (speler 1), LRed2P ... RBlue5P voor spelers 2-5, Clap, Clap2P ... Clap5P, LeftChange, RightChange, Decide, Cancel, en de systeemtoetsen (Capture, SongVolumeIncrease, SongVolumeDecrease, DisplayHits, DisplayDebug, QuickConfig, SortSongs, ToggleAutoP1, ToggleAutoP2, ToggleTrainingMode, CycleVideoDisplayMode, en de Training*-toetsen). Een onbekende naam geeft false terug. De drummethoden lezen welk apparaat de speler ook aan die invoer heeft gekoppeld in de toetsconfiguratie. INPUT vergelijkt toetsenbordtoetsnamen zonder onderscheid tussen hoofdletters en kleine letters met de toetsenlijst van de engine: letters A-Z, cijfers D0-D9, F1-F15, Space, Return, Escape, Tab, Backspace, Delete, UpArrow, DownArrow, LeftArrow, RightArrow, LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, Home, End, PageUp, PageDown, NumberPad0-9, en de andere standaardtoetsen. Muisposities zijn in logische coördinaten van het speloppervlak, gecorrigeerd voor letterboxing.
</div>

### Druminvoer

| Methode | Beschrijving |
| --- | --- |
| `INPUT:Pressed(input)  -> bool` | True in het frame waarin de invoer wordt ingedrukt. |
| `INPUT:Pressing(input)  -> bool` | True terwijl de invoer ingedrukt wordt gehouden. |
| `INPUT:Released(input)  -> bool` | True in het frame waarin de invoer wordt losgelaten. |
| `INPUT:Releasing(input)  -> bool` | True terwijl de invoer niet ingedrukt is. |
| `INPUT:RepeatWhilePressing(input, interval_seconds, callback)  -> nil` | Roep het elk frame aan. Terwijl de speler de invoer ingedrukt houdt, roept het eenmaal per `interval_seconds` `callback()` aan (de eerste aanroep komt na één interval) en het stopt wanneer de speler de invoer loslaat. |

### Toetsenbord

| Methode | Beschrijving |
| --- | --- |
| `INPUT:KeyboardPressed(key)  -> bool` | True in het frame waarin de toets wordt ingedrukt. |
| `INPUT:KeyboardPressing(key)  -> bool` | True terwijl de toets ingedrukt wordt gehouden. |
| `INPUT:KeyboardReleased(key)  -> bool` | True in het frame waarin de toets wordt losgelaten. |
| `INPUT:KeyboardReleasing(key)  -> bool` | True terwijl de toets niet ingedrukt is. |

### Muis

<div class="callout warn">
Knopnamen: left, right, middle, button4, button5, of een numerieke index (niet hoofdlettergevoelig). Posities zijn in coördinaten van het speloppervlak; de positie-getters geven -1 terug wanneer er geen muisapparaat bestaat. GetMouseDelta en GetScrollDelta geven de beweging sinds de vorige aanroep terug en resetten, dus roep elk ervan eenmaal per frame aan.
</div>

| Methode | Beschrijving |
| --- | --- |
| `INPUT:GetMouseX()  -> number` | Muis-x in coördinaten van het speloppervlak. |
| `INPUT:GetMouseY()  -> number` | Muis-y in coördinaten van het speloppervlak. |
| `INPUT:GetMouseXY()  -> number, number` | Muis-x en -y als twee retourwaarden. |
| `INPUT:IsMouseInside()  -> bool` | True wanneer de muis zich boven het gerenderde oppervlak bevindt en false wanneer ze boven een letterboxrand staat. |
| `INPUT:GetSurfaceWidth()  -> int` | Breedte van het speloppervlak (de coördinaatruimte waarin scripts tekenen). |
| `INPUT:GetSurfaceHeight()  -> int` | Hoogte van het speloppervlak. |
| `INPUT:MousePressed(button)  -> bool` | True in het frame waarin de knop wordt ingedrukt. |
| `INPUT:MousePressing(button)  -> bool` | True terwijl de knop ingedrukt wordt gehouden. |
| `INPUT:MouseReleased(button)  -> bool` | True in het frame waarin de knop wordt losgelaten. |
| `INPUT:GetMouseDelta()  -> number, number` | Muisbeweging in vensterpixels sinds de vorige aanroep, als dx, dy. |
| `INPUT:GetScrollDelta()  -> number, number` | Wielbeweging in klikken sinds de vorige aanroep, als dx, dy (dy is positief bij omhoog scrollen). |
| `INPUT:SetMouseLocked(locked)  -> nil` | Vergrendelt en verbergt de cursor voor vrij rondkijken (true) of herstelt hem (false), en reset de basislijn van de delta. |

```lua
function update()
    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then confirm() end
    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then goBack() end

    local mx, my = INPUT:GetMouseXY()
    hovered = mx >= bx and mx < bx + bw and my >= by and my < by + bh
    if hovered and INPUT:MousePressed("left") then confirm() end

    local _, wheel = INPUT:GetScrollDelta()
    scrollY = scrollY - wheel * 40
end
```

### Tekstinvoer

| Methode | Beschrijving |
| --- | --- |
| `INPUT:CreateTextInput(initialText, maxLength)  -> textInput` | Maakt een tekstinvoerwidget, vooraf gevuld met `initialText` en beperkt tot `maxLength` bytes UTF-8 (standaard 64). |

## Tekstinvoer-handle

Een tekstveld op het scherm, teruggegeven door `INPUT:CreateTextInput`, dat getypte tekst verzamelt, inclusief IME-samenstelling.

<div class="callout warn">
Roep Update eenmaal per frame aan terwijl het veld actief is en teken DisplayText zelf. Houd maar één tekstinvoer tegelijk actief. In Debug-builds toont een overlayvenster het veld ook; Release-builds tonen uit zichzelf niets. Op iOS en Android opent het spel het native tekstdialoogvenster van het platform, en geeft Update true terug wanneer de speler dat bevestigt.
</div>

| Methode | Beschrijving |
| --- | --- |
| `textInput:Update()  -> bool` | Verwerkt het typen van dit frame. Geeft true terug in het frame waarin de speler op Enter drukt. |
| `textInput.Text  -> string` | De huidige tekst. Leesbaar en toewijsbaar (nil wordt een lege string). |
| `textInput.DisplayText  -> string` | De huidige tekst met een knipperende caret op de cursorpositie ingevoegd, om te tekenen. |

```lua
local field, font

function onStart()
    font = TEXT:CreateGlyphCached(28)
end

function activate()
    field = INPUT:CreateTextInput("", 32)
end

function update()
    if field:Update() then
        saveName(field.Text)
    elseif INPUT:KeyboardPressed("Escape") then
        field.Text = ""
    end
end

function draw()
    font:Draw(field.DisplayText, 100, 100)
end

function onDestroy()
    font:Dispose()
end
```
