<!-- api/input.md -->

# Input

Reading the drum inputs, the keyboard and the mouse, and on-screen text entry.

## INPUT

Reads the configured drum inputs, the raw keyboard and the mouse, and creates text-input widgets. Available in every script.

<div class="callout warn">
Drum input names are the key-config names, matched case-insensitively: LRed, RRed, LBlue, RBlue (player 1), LRed2P ... RBlue5P for players 2-5, Clap, Clap2P ... Clap5P, LeftChange, RightChange, Decide, Cancel, and the system keys (Capture, SongVolumeIncrease, SongVolumeDecrease, DisplayHits, DisplayDebug, QuickConfig, SortSongs, ToggleAutoP1, ToggleAutoP2, ToggleTrainingMode, CycleVideoDisplayMode, and the Training* keys). An unknown name returns false. The drum methods read whichever device the player bound to that input in the key config. INPUT matches keyboard key names case-insensitively against the engine's key list: letters A-Z, digits D0-D9, F1-F15, Space, Return, Escape, Tab, Backspace, Delete, UpArrow, DownArrow, LeftArrow, RightArrow, LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, Home, End, PageUp, PageDown, NumberPad0-9, and the other standard keys. Mouse positions are in logical game-surface coordinates, corrected for letterboxing.
</div>

### Drum inputs

| Method | Description |
| --- | --- |
| `INPUT:Pressed(input)  -> bool` | True on the frame the input goes down. |
| `INPUT:Pressing(input)  -> bool` | True while the input is held. |
| `INPUT:Released(input)  -> bool` | True on the frame the input goes up. |
| `INPUT:Releasing(input)  -> bool` | True while the input is not held. |
| `INPUT:RepeatWhilePressing(input, interval_seconds, callback)  -> nil` | Call it every frame. While the player holds the input, it calls `callback()` once every `interval_seconds` (the first call comes after one interval) and stops when the player releases the input. |
| `INPUT:GetBindingCount(input)  -> int` | How many bindings the player gave the input in the key config (0 for an unknown name). |
| `INPUT:GetBindingDevice(input, index)  -> string` | The device of the binding at `index` (0 to count - 1): Keyboard, Gamepad, Joystick, MidiIn or Mouse. An empty string when `index` is out of range. |
| `INPUT:GetBindingName(input, index)  -> string` | The binding's key or button: a keyboard key name as `KeyboardPressed` takes it (Return, F), the button name the gamepad or joystick reports (A, Start, Button3), a MIDI note (C4[60]) or a mouse button name as `MousePressed` takes it. An empty string when `index` is out of range. |
| `INPUT:GetEvents(part)  -> inputEvent[]` | This frame's presses and releases of the inputs of a key-config part, oldest first, each with the time the input thread recorded for it (the events the gameplay screen judges). `part` is "Taiko" (the default: the drums of players 1 to 5, the Clap keys, LeftChange, RightChange, Decide, Cancel and the Training* keys) or "System" (Capture to CycleVideoDisplayMode), matched case-insensitively. Returns a .NET array of input events (index from 0, `.Length`); every call in a frame returns that frame's events. An empty array for an unknown part and during a transition's fade-in, when the stage receives no input. One key bound to several inputs gives one event for each of them: with the default keys, F gives Decide, then LRed. |

### Keyboard

| Method | Description |
| --- | --- |
| `INPUT:KeyboardPressed(key)  -> bool` | True on the frame the key goes down. |
| `INPUT:KeyboardPressing(key)  -> bool` | True while the key is held. |
| `INPUT:KeyboardReleased(key)  -> bool` | True on the frame the key goes up. |
| `INPUT:KeyboardReleasing(key)  -> bool` | True while the key is not held. |
| `INPUT:GetKeyboardEvents()  -> inputEvent[]` | This frame's key presses and releases, oldest first, each with the time the input thread recorded for it, as a .NET array of input events (index from 0, `.Length`). An empty array during a transition's fade-in, when the stage receives no input. |

### Mouse

<div class="callout warn">
Button names: left, right, middle, button4, button5, or a numeric index (case-insensitive). Positions are in game-surface coordinates; the position getters return -1 when no mouse device exists. GetMouseDelta and GetScrollDelta return the movement since the previous call and reset, so call each once per frame.
</div>

| Method | Description |
| --- | --- |
| `INPUT:GetMouseX()  -> number` | Mouse x in game-surface coordinates. |
| `INPUT:GetMouseY()  -> number` | Mouse y in game-surface coordinates. |
| `INPUT:GetMouseXY()  -> number, number` | Mouse x and y as two return values. |
| `INPUT:IsMouseInside()  -> bool` | True when the mouse is over the rendered surface and false when it is over a letterbox border. |
| `INPUT:GetSurfaceWidth()  -> int` | Width of the game surface (the coordinate space scripts draw in). |
| `INPUT:GetSurfaceHeight()  -> int` | Height of the game surface. |
| `INPUT:MousePressed(button)  -> bool` | True on the frame the button goes down. |
| `INPUT:MousePressing(button)  -> bool` | True while the button is held. |
| `INPUT:MouseReleased(button)  -> bool` | True on the frame the button goes up. |
| `INPUT:GetMouseDelta()  -> number, number` | Mouse movement in window pixels since the previous call, as dx, dy. |
| `INPUT:GetScrollDelta()  -> number, number` | Wheel movement in notches since the previous call, as dx, dy (dy is positive when scrolling up). |
| `INPUT:SetMouseLocked(locked)  -> nil` | Locks and hides the cursor for free-look (true) or restores it (false), and resets the delta baseline. |

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

### Text input

| Method | Description |
| --- | --- |
| `INPUT:CreateTextInput(initialText, maxLength)  -> textInput` | Creates a text-input widget pre-filled with `initialText` and limited to `maxLength` bytes of UTF-8 (default 64). |

## Input event

One press or release in the arrays that `INPUT:GetEvents` and `INPUT:GetKeyboardEvents` return.

<div class="callout warn">
All members are read-only. Ms is on the clock of `fps.ms` and never later than the call that returned the event; the input thread records it to the millisecond. Before the sound device exists the input thread records no time, and Ms is the time of the call.
</div>

| Method | Description |
| --- | --- |
| `inputEvent.Name  -> string` | The input name as `Pressed` takes it (LRed, RBlue2P, Decide, Capture ...), or for a keyboard event the key name as `KeyboardPressed` takes it (Return, F). |
| `inputEvent.Player  -> int` | The player the input belongs to (0 to 4), or -1 for an input of no player (Decide, Cancel, LeftChange, RightChange, the Training* and system keys) and for every keyboard event. |
| `inputEvent.Pressed  -> bool` | True for a press, false for a release. |
| `inputEvent.Ms  -> number` | The time the input thread recorded for the event, in ms on the clock of `fps.ms`. |

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

## Text-input handle

An on-screen text field returned by `INPUT:CreateTextInput` that collects typed text, including IME composition.

<div class="callout warn">
Call Update once per frame while the field is active and draw DisplayText yourself. Keep only one text input active at a time. In Debug builds an overlay window also shows the field; Release builds show nothing on their own. On iOS and Android the game opens the platform's native text dialog, and Update returns true when the player confirms it.
</div>

| Method | Description |
| --- | --- |
| `textInput:Update()  -> bool` | Processes this frame's typing. Returns true on the frame the player presses Enter. |
| `textInput.Text  -> string` | The current text. Readable and assignable (nil becomes an empty string). |
| `textInput.DisplayText  -> string` | The current text with a blinking caret inserted at the cursor position, for drawing. |

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
