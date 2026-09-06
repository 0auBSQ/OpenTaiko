<!-- api/input.md -->

# Ввод

Чтение ввода с барабана, клавиатуры и мыши, а также экранный ввод текста.

## INPUT

Читает настроенные входы барабана, необработанную клавиатуру и мышь, а также создаёт виджеты ввода текста. Доступен в каждом скрипте.

<div class="callout warn">
Имена входов барабана — это имена из настроек клавиш, сопоставляемые без учёта регистра: LRed, RRed, LBlue, RBlue (игрок 1), LRed2P ... RBlue5P для игроков 2-5, Clap, Clap2P ... Clap5P, LeftChange, RightChange, Decide, Cancel и системные клавиши (Capture, SongVolumeIncrease, SongVolumeDecrease, DisplayHits, DisplayDebug, QuickConfig, SortSongs, ToggleAutoP1, ToggleAutoP2, ToggleTrainingMode, CycleVideoDisplayMode и клавиши Training*). Неизвестное имя возвращает false. Методы барабана читают то устройство, которое игрок привязал к данному входу в настройках клавиш. Имена клавиш клавиатуры INPUT сопоставляет без учёта регистра со списком клавиш движка: буквы A-Z, цифры D0-D9, F1-F15, Space, Return, Escape, Tab, Backspace, Delete, UpArrow, DownArrow, LeftArrow, RightArrow, LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, Home, End, PageUp, PageDown, NumberPad0-9 и другие стандартные клавиши. Позиции мыши задаются в логических координатах игровой поверхности с поправкой на леттербоксинг.
</div>

### Входы барабана

| Метод | Описание |
| --- | --- |
| `INPUT:Pressed(input)  -> bool` | Истина в кадре, когда вход нажимается. |
| `INPUT:Pressing(input)  -> bool` | Истина, пока вход удерживается. |
| `INPUT:Released(input)  -> bool` | Истина в кадре, когда вход отпускается. |
| `INPUT:Releasing(input)  -> bool` | Истина, пока вход не удерживается. |
| `INPUT:RepeatWhilePressing(input, interval_seconds, callback)  -> nil` | Вызывайте его каждый кадр. Пока игрок удерживает вход, он вызывает `callback()` раз в `interval_seconds` (первый вызов происходит по истечении одного интервала) и прекращает, когда игрок отпускает вход. |

### Клавиатура

| Метод | Описание |
| --- | --- |
| `INPUT:KeyboardPressed(key)  -> bool` | Истина в кадре, когда клавиша нажимается. |
| `INPUT:KeyboardPressing(key)  -> bool` | Истина, пока клавиша удерживается. |
| `INPUT:KeyboardReleased(key)  -> bool` | Истина в кадре, когда клавиша отпускается. |
| `INPUT:KeyboardReleasing(key)  -> bool` | Истина, пока клавиша не удерживается. |

### Мышь

<div class="callout warn">
Имена кнопок: left, right, middle, button4, button5 или числовой индекс (без учёта регистра). Позиции задаются в координатах игровой поверхности; геттеры позиции возвращают -1, если устройства мыши нет. GetMouseDelta и GetScrollDelta возвращают перемещение с предыдущего вызова и сбрасываются, поэтому вызывайте каждый из них один раз за кадр.
</div>

| Метод | Описание |
| --- | --- |
| `INPUT:GetMouseX()  -> number` | X мыши в координатах игровой поверхности. |
| `INPUT:GetMouseY()  -> number` | Y мыши в координатах игровой поверхности. |
| `INPUT:GetMouseXY()  -> number, number` | X и Y мыши как два возвращаемых значения. |
| `INPUT:IsMouseInside()  -> bool` | Истина, когда мышь находится над отрисованной поверхностью, и ложь, когда она над полосой леттербоксинга. |
| `INPUT:GetSurfaceWidth()  -> int` | Ширина игровой поверхности (системы координат, в которой рисуют скрипты). |
| `INPUT:GetSurfaceHeight()  -> int` | Высота игровой поверхности. |
| `INPUT:MousePressed(button)  -> bool` | Истина в кадре, когда кнопка нажимается. |
| `INPUT:MousePressing(button)  -> bool` | Истина, пока кнопка удерживается. |
| `INPUT:MouseReleased(button)  -> bool` | Истина в кадре, когда кнопка отпускается. |
| `INPUT:GetMouseDelta()  -> number, number` | Перемещение мыши в пикселях окна с предыдущего вызова, как dx, dy. |
| `INPUT:GetScrollDelta()  -> number, number` | Перемещение колеса в делениях с предыдущего вызова, как dx, dy (dy положителен при прокрутке вверх). |
| `INPUT:SetMouseLocked(locked)  -> nil` | Захватывает и скрывает курсор для свободного обзора (true) или восстанавливает его (false), а также сбрасывает базу отсчёта дельты. |

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

### Ввод текста

| Метод | Описание |
| --- | --- |
| `INPUT:CreateTextInput(initialText, maxLength)  -> textInput` | Создаёт виджет ввода текста, заранее заполненный `initialText` и ограниченный `maxLength` байтами UTF-8 (по умолчанию 64). |

## Дескриптор ввода текста

Экранное текстовое поле, возвращаемое `INPUT:CreateTextInput`, которое собирает вводимый текст, включая композицию IME.

<div class="callout warn">
Вызывайте Update один раз за кадр, пока поле активно, и рисуйте DisplayText сами. Держите активным только одно поле ввода одновременно. В Debug-сборках поле дополнительно показывается в окне-наложении; Release-сборки сами по себе ничего не показывают. На iOS и Android игра открывает системный диалог ввода текста платформы, и Update возвращает true, когда игрок его подтверждает.
</div>

| Метод | Описание |
| --- | --- |
| `textInput:Update()  -> bool` | Обрабатывает ввод этого кадра. Возвращает true в кадре, когда игрок нажимает Enter. |
| `textInput.Text  -> string` | Текущий текст. Доступен для чтения и присваивания (nil становится пустой строкой). |
| `textInput.DisplayText  -> string` | Текущий текст с мигающей кареткой в позиции курсора, для отрисовки. |

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
