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
| `INPUT:GetBindingCount(input)  -> int` | Wie viele Belegungen der Spieler der Eingabe in der Tastenkonfiguration gegeben hat (0 bei einem unbekannten Namen). |
| `INPUT:GetBindingDevice(input, index)  -> string` | Das Gerät der Belegung an `index` (0 bis count - 1): Keyboard, Gamepad, Joystick, MidiIn oder Mouse. Ein leerer String, wenn `index` außerhalb des Bereichs liegt. |
| `INPUT:GetBindingName(input, index)  -> string` | Die Taste oder der Knopf der Belegung: ein Tastenname, wie `KeyboardPressed` ihn nimmt (Return, F), der Knopfname, den das Gamepad oder der Joystick meldet (A, Start, Button3), eine MIDI-Note (C4[60]) oder ein Maustastenname, wie `MousePressed` ihn nimmt. Ein leerer String, wenn `index` außerhalb des Bereichs liegt. |
| `INPUT:GetEvents(part)  -> inputEvent[]` | Die Anschläge und das Loslassen der Eingaben eines Teils der Tastenkonfiguration in diesem Frame, das älteste zuerst, jeweils mit der Zeit, die der Eingabe-Thread dafür aufgezeichnet hat (die Ereignisse, die der Spielbildschirm bewertet). `part` ist "Taiko" (der Standard: die Trommeln der Spieler 1 bis 5, die Clap-Tasten, LeftChange, RightChange, Decide, Cancel und die Training*-Tasten) oder "System" (Capture bis CycleVideoDisplayMode), ohne Beachtung der Groß-/Kleinschreibung. Gibt ein .NET-Array von Eingabe-Ereignissen zurück (Index ab 0, `.Length`); jeder Aufruf in einem Frame gibt die Ereignisse dieses Frames zurück. Ein leeres Array bei einem unbekannten Teil und während des Einblendens einer Transition, solange die Stage keine Eingaben erhält. Eine Taste, die mehreren Eingaben zugewiesen ist, liefert für jede ein Ereignis: mit der Standardbelegung liefert F erst Decide, dann LRed. |

### Tastatur

| Methode | Beschreibung |
| --- | --- |
| `INPUT:KeyboardPressed(key)  -> bool` | True im Frame, in dem die Taste gedrückt wird. |
| `INPUT:KeyboardPressing(key)  -> bool` | True, solange die Taste gehalten wird. |
| `INPUT:KeyboardReleased(key)  -> bool` | True im Frame, in dem die Taste losgelassen wird. |
| `INPUT:KeyboardReleasing(key)  -> bool` | True, solange die Taste nicht gehalten wird. |
| `INPUT:GetKeyboardEvents()  -> inputEvent[]` | Die Tastenanschläge und das Loslassen von Tasten in diesem Frame, das älteste zuerst, jeweils mit der Zeit, die der Eingabe-Thread dafür aufgezeichnet hat, als .NET-Array von Eingabe-Ereignissen (Index ab 0, `.Length`). Ein leeres Array während des Einblendens einer Transition, solange die Stage keine Eingaben erhält. |

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

## Eingabe-Ereignis

Ein Anschlag oder ein Loslassen in den Arrays, die `INPUT:GetEvents` und `INPUT:GetKeyboardEvents` zurückgeben.

<div class="callout warn">
Alle Member sind schreibgeschützt. Ms liegt auf der Uhr von `fps.ms` und nie nach dem Aufruf, der das Ereignis zurückgab; der Eingabe-Thread zeichnet die Zeit auf die Millisekunde genau auf. Bevor das Soundgerät existiert, zeichnet der Eingabe-Thread keine Zeit auf, und Ms ist die Zeit des Aufrufs.
</div>

| Methode | Beschreibung |
| --- | --- |
| `inputEvent.Name  -> string` | Der Eingabename, wie `Pressed` ihn nimmt (LRed, RBlue2P, Decide, Capture ...), oder bei einem Tastatur-Ereignis der Tastenname, wie `KeyboardPressed` ihn nimmt (Return, F). |
| `inputEvent.Player  -> int` | Der Spieler, zu dem die Eingabe gehört (0 bis 4), oder -1 für eine Eingabe ohne Spieler (Decide, Cancel, LeftChange, RightChange, die Training*- und Systemtasten) und für jedes Tastatur-Ereignis. |
| `inputEvent.Pressed  -> bool` | True für einen Anschlag, false für ein Loslassen. |
| `inputEvent.Ms  -> number` | Die Zeit, die der Eingabe-Thread für das Ereignis aufgezeichnet hat, in ms auf der Uhr von `fps.ms`. |

```lua
function update()
    local events = INPUT:GetEvents()
    for i = 0, events.Length - 1 do
        local e = events[i]
        if e.Pressed and e.Player == 0 then
            onHit(e.Name, e.Ms)
        end
    end
end
```

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
