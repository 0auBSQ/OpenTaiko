<!-- api/input.md -->

# Entrada

Lectura de las entradas del tambor, el teclado y el ratón, y entrada de texto en pantalla.

## INPUT

Lee las entradas del tambor configuradas, el teclado en bruto y el ratón, y crea widgets de entrada de texto. Disponible en todos los scripts.

<div class="callout warn">
Los nombres de las entradas del tambor son los nombres de la configuración de teclas, comparados sin distinguir mayúsculas: LRed, RRed, LBlue, RBlue (jugador 1), LRed2P ... RBlue5P para los jugadores 2-5, Clap, Clap2P ... Clap5P, LeftChange, RightChange, Decide, Cancel, y las teclas de sistema (Capture, SongVolumeIncrease, SongVolumeDecrease, DisplayHits, DisplayDebug, QuickConfig, SortSongs, ToggleAutoP1, ToggleAutoP2, ToggleTrainingMode, CycleVideoDisplayMode y las teclas Training*). Un nombre desconocido devuelve false. Los métodos del tambor leen el dispositivo que el jugador haya asignado a esa entrada en la configuración de teclas. INPUT compara los nombres de las teclas del teclado sin distinguir mayúsculas contra la lista de teclas del motor: letras A-Z, dígitos D0-D9, F1-F15, Space, Return, Escape, Tab, Backspace, Delete, UpArrow, DownArrow, LeftArrow, RightArrow, LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, Home, End, PageUp, PageDown, NumberPad0-9 y las demás teclas estándar. Las posiciones del ratón están en coordenadas lógicas de la superficie de juego, corregidas para el letterboxing.
</div>

### Entradas del tambor

| Método | Descripción |
| --- | --- |
| `INPUT:Pressed(input)  -> bool` | Verdadero en el fotograma en que la entrada se pulsa. |
| `INPUT:Pressing(input)  -> bool` | Verdadero mientras la entrada se mantiene pulsada. |
| `INPUT:Released(input)  -> bool` | Verdadero en el fotograma en que la entrada se suelta. |
| `INPUT:Releasing(input)  -> bool` | Verdadero mientras la entrada no está pulsada. |
| `INPUT:RepeatWhilePressing(input, interval_seconds, callback)  -> nil` | Llámalo cada fotograma. Mientras el jugador mantiene pulsada la entrada, llama a `callback()` una vez cada `interval_seconds` (la primera llamada llega tras un intervalo) y se detiene cuando el jugador suelta la entrada. |

### Teclado

| Método | Descripción |
| --- | --- |
| `INPUT:KeyboardPressed(key)  -> bool` | Verdadero en el fotograma en que la tecla se pulsa. |
| `INPUT:KeyboardPressing(key)  -> bool` | Verdadero mientras la tecla se mantiene pulsada. |
| `INPUT:KeyboardReleased(key)  -> bool` | Verdadero en el fotograma en que la tecla se suelta. |
| `INPUT:KeyboardReleasing(key)  -> bool` | Verdadero mientras la tecla no está pulsada. |

### Ratón

<div class="callout warn">
Nombres de botón: left, right, middle, button4, button5, o un índice numérico (sin distinguir mayúsculas). Las posiciones están en coordenadas de la superficie de juego; los getters de posición devuelven -1 cuando no existe ningún dispositivo de ratón. GetMouseDelta y GetScrollDelta devuelven el movimiento desde la llamada anterior y se reinician, así que llama a cada uno una vez por fotograma.
</div>

| Método | Descripción |
| --- | --- |
| `INPUT:GetMouseX()  -> number` | X del ratón en coordenadas de la superficie de juego. |
| `INPUT:GetMouseY()  -> number` | Y del ratón en coordenadas de la superficie de juego. |
| `INPUT:GetMouseXY()  -> number, number` | X e Y del ratón como dos valores de retorno. |
| `INPUT:IsMouseInside()  -> bool` | Verdadero cuando el ratón está sobre la superficie renderizada y falso cuando está sobre un borde de letterbox. |
| `INPUT:GetSurfaceWidth()  -> int` | Anchura de la superficie de juego (el espacio de coordenadas en el que dibujan los scripts). |
| `INPUT:GetSurfaceHeight()  -> int` | Altura de la superficie de juego. |
| `INPUT:MousePressed(button)  -> bool` | Verdadero en el fotograma en que el botón se pulsa. |
| `INPUT:MousePressing(button)  -> bool` | Verdadero mientras el botón se mantiene pulsado. |
| `INPUT:MouseReleased(button)  -> bool` | Verdadero en el fotograma en que el botón se suelta. |
| `INPUT:GetMouseDelta()  -> number, number` | Movimiento del ratón en píxeles de ventana desde la llamada anterior, como dx, dy. |
| `INPUT:GetScrollDelta()  -> number, number` | Movimiento de la rueda en muescas desde la llamada anterior, como dx, dy (dy es positivo al desplazar hacia arriba). |
| `INPUT:SetMouseLocked(locked)  -> nil` | Bloquea y oculta el cursor para vista libre (true) o lo restaura (false), y reinicia la base del delta. |

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

### Entrada de texto

| Método | Descripción |
| --- | --- |
| `INPUT:CreateTextInput(initialText, maxLength)  -> textInput` | Crea un widget de entrada de texto rellenado con `initialText` y limitado a `maxLength` bytes de UTF-8 (64 por defecto). |

## Handle de entrada de texto

Un campo de texto en pantalla devuelto por `INPUT:CreateTextInput` que recoge el texto escrito, incluida la composición IME.

<div class="callout warn">
Llama a Update una vez por fotograma mientras el campo esté activo y dibuja DisplayText tú mismo. Mantén solo una entrada de texto activa a la vez. En compilaciones Debug una ventana superpuesta también muestra el campo; las compilaciones Release no muestran nada por sí mismas. En iOS y Android el juego abre el diálogo de texto nativo de la plataforma, y Update devuelve true cuando el jugador lo confirma.
</div>

| Método | Descripción |
| --- | --- |
| `textInput:Update()  -> bool` | Procesa lo escrito en este fotograma. Devuelve true en el fotograma en que el jugador pulsa Intro. |
| `textInput.Text  -> string` | El texto actual. Legible y asignable (nil se convierte en una cadena vacía). |
| `textInput.DisplayText  -> string` | El texto actual con un cursor parpadeante insertado en la posición del cursor, para dibujarlo. |

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
