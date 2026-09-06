<!-- api/input.md -->

# Eingabe

Auslesen der Trommel-Eingaben, der Tastatur und der Maus sowie Texteingabe auf dem Bildschirm.

## INPUT

Liest die konfigurierten Trommel-Eingaben, die rohe Tastatur und die Maus und erzeugt Texteingabe-Widgets. In jedem Skript verfügbar.

<div class="callout warn">
Namen der Trommel-Eingaben sind die Namen der Tastenkonfiguration, ohne Beachtung der Groß-/Kleinschreibung: LRed, RRed, LBlue, RBlue (Spieler 1), LRed2P ... RBlue5P für die Spieler 2-5, Clap, Clap2P ... Clap5P, LeftChange, RightChange, Decide, Cancel sowie die Systemtasten (Capture, SongVolumeIncrease, SongVolumeDecrease, DisplayHits, DisplayDebug, QuickConfig, SortSongs, ToggleAutoP1, ToggleAutoP2, ToggleTrainingMode, CycleVideoDisplayMode und die Training*-Tasten). Ein unbekannter Name liefert false. Die Trommelmethoden lesen das Gerät, das der Spieler in der Tastenkonfiguration dieser Eingabe zugewiesen hat. INPUT vergleicht Tastaturtastennamen ohne Beachtung der Groß-/Kleinschreibung mit der Tastenliste der Engine: Buchstaben A-Z, Ziffern D0-D9, F1-F15, Space, Return, Escape, Tab, Backspace, Delete, UpArrow, DownArrow, LeftArrow, RightArrow, LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, Home, End, PageUp, PageDown, NumberPad0-9 und die anderen Standardtasten. Mauspositionen sind in logischen Spieloberflächen-Koordinaten, korrigiert um das Letterboxing.
</div>

### Trommel-Eingaben

| Methode | Beschreibung |
| --- | --- |
| `INPUT:Pressed(input)  -> bool` | True im Frame, in dem die Eingabe gedrückt wird. |
| `INPUT:Pressing(input)  -> bool` | True, solange die Eingabe gehalten wird. |
| `INPUT:Released(input)  -> bool` | True im Frame, in dem die Eingabe losgelassen wird. |
| `INPUT:Releasing(input)  -> bool` | True, solange die Eingabe nicht gehalten wird. |
| `INPUT:RepeatWhilePressing(input, interval_seconds, callback)  -> nil` | Rufen Sie es jeden Frame auf. Solange der Spieler die Eingabe hält, ruft es `callback()` einmal alle `interval_seconds` auf (der erste Aufruf erfolgt nach einem Intervall) und stoppt, wenn der Spieler die Eingabe loslässt. |

### Tastatur

| Methode | Beschreibung |
| --- | --- |
| `INPUT:KeyboardPressed(key)  -> bool` | True im Frame, in dem die Taste gedrückt wird. |
| `INPUT:KeyboardPressing(key)  -> bool` | True, solange die Taste gehalten wird. |
| `INPUT:KeyboardReleased(key)  -> bool` | True im Frame, in dem die Taste losgelassen wird. |
| `INPUT:KeyboardReleasing(key)  -> bool` | True, solange die Taste nicht gehalten wird. |

### Maus

<div class="callout warn">
Tastennamen: left, right, middle, button4, button5 oder ein numerischer Index (Groß-/Kleinschreibung egal). Positionen sind in Spieloberflächen-Koordinaten; die Positions-Getter geben -1 zurück, wenn kein Mausgerät existiert. GetMouseDelta und GetScrollDelta geben die Bewegung seit dem vorherigen Aufruf zurück und setzen sie zurück; rufen Sie beide daher einmal pro Frame auf.
</div>

| Methode | Beschreibung |
| --- | --- |
| `INPUT:GetMouseX()  -> number` | Maus-x in Spieloberflächen-Koordinaten. |
| `INPUT:GetMouseY()  -> number` | Maus-y in Spieloberflächen-Koordinaten. |
| `INPUT:GetMouseXY()  -> number, number` | Maus-x und -y als zwei Rückgabewerte. |
| `INPUT:IsMouseInside()  -> bool` | True, wenn sich die Maus über der gerenderten Fläche befindet, und false, wenn sie über einem Letterbox-Rand ist. |
| `INPUT:GetSurfaceWidth()  -> int` | Breite der Spieloberfläche (der Koordinatenraum, in dem Skripte zeichnen). |
| `INPUT:GetSurfaceHeight()  -> int` | Höhe der Spieloberfläche. |
| `INPUT:MousePressed(button)  -> bool` | True im Frame, in dem die Taste gedrückt wird. |
| `INPUT:MousePressing(button)  -> bool` | True, solange die Taste gehalten wird. |
| `INPUT:MouseReleased(button)  -> bool` | True im Frame, in dem die Taste losgelassen wird. |
| `INPUT:GetMouseDelta()  -> number, number` | Mausbewegung in Fensterpixeln seit dem vorherigen Aufruf, als dx, dy. |
| `INPUT:GetScrollDelta()  -> number, number` | Radbewegung in Rasterschritten seit dem vorherigen Aufruf, als dx, dy (dy ist beim Hochscrollen positiv). |
| `INPUT:SetMouseLocked(locked)  -> nil` | Sperrt und verbirgt den Cursor für freie Umschau (true) oder stellt ihn wieder her (false) und setzt die Delta-Basis zurück. |

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

### Texteingabe

| Methode | Beschreibung |
| --- | --- |
| `INPUT:CreateTextInput(initialText, maxLength)  -> textInput` | Erzeugt ein Texteingabe-Widget, vorbelegt mit `initialText` und begrenzt auf `maxLength` Bytes UTF-8 (Standard 64). |

## Texteingabe-Handle

Ein Textfeld auf dem Bildschirm, das von `INPUT:CreateTextInput` zurückgegeben wird und getippten Text einschließlich IME-Eingabe sammelt.

<div class="callout warn">
Rufen Sie Update einmal pro Frame auf, solange das Feld aktiv ist, und zeichnen Sie DisplayText selbst. Halten Sie nur eine Texteingabe gleichzeitig aktiv. In Debug-Builds zeigt zusätzlich ein Overlay-Fenster das Feld; Release-Builds zeigen von sich aus nichts. Auf iOS und Android öffnet das Spiel den nativen Textdialog der Plattform, und Update gibt true zurück, wenn der Spieler ihn bestätigt.
</div>

| Methode | Beschreibung |
| --- | --- |
| `textInput:Update()  -> bool` | Verarbeitet die Tastatureingaben dieses Frames. Gibt in dem Frame, in dem der Spieler Enter drückt, true zurück. |
| `textInput.Text  -> string` | Der aktuelle Text. Lesbar und zuweisbar (nil wird zu einem leeren String). |
| `textInput.DisplayText  -> string` | Der aktuelle Text mit einem blinkenden Cursor an der Cursorposition, zum Zeichnen. |

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
