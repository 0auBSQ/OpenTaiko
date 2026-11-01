<!-- modules/ui.md -->

# UI

Библиотека интерфейса для меню и диалогов, которой управляют мышью, клавиатурой и геймпадом с одной общей подсветкой. `UI.SimpleUI` — минимальная библиотека, которая поставляется с OpenTaiko; скин может придать ей своё оформление через `derive`, как это делает Open-World Memories с PopUI. Глобальные модули берут библиотеку из `UI.Pick`, который отдаёт ту, что выбрал скин (или ту, что назвала сцена), поэтому все экраны скина выглядят одинаково.

## UI.Pick

`require("UI.Pick")` возвращает функцию; её вызов возвращает таблицу библиотеки интерфейса.

| Вызов | Библиотека |
| --- | --- |
| `Pick(name)` | Библиотека с этим именем для require (`"PopUI"`, `"UI.SimpleUI"`, ...), для сцены, которой нужна конкретная. |
| `Pick()` | Библиотека, названная в общей строке `ui_library`, которую скин задаёт один раз (`_boot` в Open-World Memories задаёт `"PopUI"`). |
| `Pick()` без `ui_library` | `UI.SimpleUI`. |

Имя, которое не загружается или загружает что-то без функции `new`, тоже даёт `UI.SimpleUI`, так что имя, оставленное в общей строке другим скином, ничего не ломает. Выбор — это обычный Lua и общие строки: движок о нём ничего не знает.

```lua
-- in a skin's _boot onStart: the library every global module will use
SHARED:SetSharedString("ui_library", "MyUI")

-- in any module
local UI = require("UI.Pick")()
local ui = UI.new{ navPlayer = 1 }
```

## UI.SimpleUI

`UI.new(opts)` создаёт менеджер, который владеет виджетами, распределяет ввод и рисует их. Вызывайте `ui:update(ts)` и `ui:draw()` каждый кадр.

| Поле `opts` | Описание |
| --- | --- |
| `theme` | Таблица темы, наложенная на оформление библиотеки по умолчанию (см. «Темы оформления»). |
| `sfx` | Звуки по имени, наложенные на звуки по умолчанию (см. «Звуки»). |
| `bg` | true — сначала залить весь экран цветом `bg` из темы. |
| `navPlayer` | Игрок, чей барабан тоже управляет навигацией, в форме, которую принимает `NavInput.getPn`; nil — все игроки. |
| `nav` | `"spatial"`: стрелки переводят к ближайшему виджету в своём направлении (сетка). По умолчанию «вверх» и «вниз» перебирают виджеты в порядке их создания. |

| Метод | Описание |
| --- | --- |
| `ui:update(ts)  -> "cancel" or nil` | Обрабатывает ввод и анимации одного кадра; возвращает `"cancel"`, если был нажат Cancel, чтобы сцена отреагировала. |
| `ui:draw()` | Рисует все видимые виджеты в порядке их создания. |
| `ui:button(o)`, `ui:label(o)`, ...  `-> widget` | Создаёт виджет, добавляет его и возвращает (см. «Виджеты»). |
| `ui:add(w)`, `ui:remove(w)` | Добавляет или удаляет виджет, созданный вручную. |
| `ui:clear()` | Удаляет все виджеты и освобождает их холсты. |
| `ui:disposeWidgets()` | Освобождает холсты виджетов. Вызывайте перед тем, как отбросить менеджер: холсты сами не освобождаются. |
| `ui:focus(w)  -> bool`, `ui:focused()  -> widget` | Переводит фокус клавиатуры на виджет; виджет, у которого фокус. |
| `ui:focusNext()`, `ui:focusPrev()` | Переводит фокус клавиатуры на шаг. |
| `ui:setTheme(t)`, `ui:setSfx(t)` | Заменяет тему или звуки менеджера; все виджеты меняют оформление. |
| `ui:isCapturing()  -> bool` | True, пока клавиши принимает текстовое поле: в это время пропускайте свои горячие клавиши. |
| `ui:mouseActive()  -> bool` | True, пока последним использованным устройством была мышь. |
| `ui:drawText(size, str, x, y, color, opacity?, scale?, maxWidth?)` | Рисует текст глифовым шрифтом интерфейса с привязкой к левому верхнему углу. |
| `ui:drawTextEx(size, str, x, y, fg, bg, opacity, scale, maxWidth, anchor)` | То же с цветом обводки и одним из девяти имён привязки. |
| `ui:measureText(size, str)`, `ui:wrapLines(size, str, maxWidth)`, `ui:ellipsize(size, str, maxWidth)` | Измерение, перенос и сокращение текста тем же шрифтом. |
| `ui:textNudge(size)  -> number` | Смещение, которое центрирует строку текста на средней линии. |
| `UI.flushSharedFonts()` | Сбрасывает закэшированные шрифты; вызывайте при смене языка (а значит, и шрифта). |

```lua
local UI = require("UI.Pick")()
local ui

function activate()
    ui = UI.new{ navPlayer = 1 }
    ui:panel{ x = 560, y = 240, w = 800, h = 600, title = "Options" }
    ui:toggle{ text = "Hints", x = 620, y = 360, value = true, onChange = function(v) hints = v end }
    ui:button{ text = "Back", x = 860, y = 720, onClick = function() leaving = true end }
end

function update(ts)
    if ui:update(ts) == "cancel" or leaving then return Exit("stage", "_title") end
end

function draw() ui:draw() end

function deactivate() ui:disposeWidgets() end
```

## Виджеты

Каждый виджет принимает `x`, `y`, `w` и `h`, а также `enabled`, `visible`, `focusable`, `mouseOnly` (наводится и нажимается мышью, но клавиши его пропускают), `style` или `theme` (поля темы только для этого виджета), `sfx` и `disabledLook = "fade"`. Параметры становятся полями виджета; меняйте их методами (`setEnabled(b)`, `setVisible(b)`, `setText(t)`, ...), которые заново применяют оформление виджета.

| Фабрика | Параметры и методы |
| --- | --- |
| `ui:button(o)` | `text` или `icon` (`"play"`, `"pause"`, `"stop"`, `"back"`, `"close"`, `"lock"`, `"repeat"`, ...), `accent`, `onClick(b)`. Если `w`/`h` не заданы, размер подгоняется под надпись. `setText`, `setIcon`. |
| `ui:label(o)` | `text`, `size` (`"title"`, `"label"`, `"small"` или число), `color`, `align`, `chip` (скруглённый фон). Не получает фокус. `setText`. |
| `ui:toggle(o)`, `ui:checkbox(o)` | `text`, `value`, `onChange(v)`. `setValue(v, silent)`. |
| `ui:slider(o)` | `min`, `max`, `step`, `value`, `showValue`, `valueW`, `onChange(v)`. Перетаскивание или «влево»/«вправо» при фокусе. `setValue(v, silent)`. |
| `ui:textbox(o)` | `value`, `placeholder`, `maxLen`, `onChange`, `onSubmit`. Щелчок или Decide начинает ввод. |
| `ui:panel(o)` | `title`. Контейнер, рисуемый позади виджетов, созданных после него; `panel:content()` даёт его внутренний прямоугольник. Не получает фокус. |
| `ui:bubble(o)` | `text`, `tail` (`"bottom"`, `"top"`, `"left"`, `"right"`), `tailPos`, `typewriter`, `onDone`. `setText`, `setTail`, `setSize`. |
| `ui:menu(o)` | `items` (строки или `{ text, value, locked, mark }`), `selected`, `rowHeight`, `onChange(i, item)`, `onSelect(i, item)`, `onLockedSelect`, `drawLocked`, `hoverSelects`, `scrollbar`. Прокручиваемый список. `setSelected(i)`. |
| `ui:chooser(o)` | `options`, `index`, `wrap`, `onChange(i, option)`. Переключатель вариантов («влево»/«вправо»). `value()`, `setOptions`, `setIndex`. |
| `ui:settingsList(o)` | `rows` (`{ header = "..." }` или `{ opt = option }`), `onFocusRow`, `onActivateRow`, `scrollbar`. Список строк параметров экрана настроек. `setRows`. |
| `ui:coins(o)` | Счётчик монет (см. ниже). Не получает фокус. |

Классы виджетов — поля библиотеки (`UI.Button`, `UI.Menu`, ...) для наследования, вместе с `UI.Widget` (базовый класс), `UI.Shape` (скруглённые фигуры, тени и значки, которые запекают виджеты), `UI.Theme`, `UI.Sfx` и `UI.Cache` (пул холстов).

## Виджет монет

`ui:coins{ amount, plain, size, iconSize, color, edge }` показывает сумму рядом со значком монеты на плашке (`plain = true`: только значок и число, выровненные по правому краю). Каждая библиотека рисует его по-своему: значок SimpleUI — это `Coin.png`, лежащий рядом с ней, а PopUI сохраняет коробку монет поставляемого скина.

| Метод | Описание |
| --- | --- |
| `c:setAmount(n)` | Сразу показывает `n`. |
| `c:add(n)` | Прокручивает число на `n`. |
| `c:gain(n)`, `c:pay(n)` | То же, с `+n` или `-n` над ним и звуком `coin` или `pay`. |
| `c:amount()  -> number` | Сумма, к которой оно движется. |
| `c:setPrice(n)`, `c:setBonus(n)` | Цена или бонус `+n`, показанные над ним; nil убирает их. |
| `c:drawCoin(x, y, sx, sy, opacity)` | Рисует только значок монеты с центром в (x, y). |

## Темы оформления

Тема — это одна таблица. Менеджер вычисляет оформление каждого виджета так: оформление библиотеки по умолчанию, затем `theme` менеджера, затем `style` виджета; таблицы сливаются поле за полем, поэтому тема называет только то, что меняет.

| Поле | Описание |
| --- | --- |
| `colors` | Цвета `{r, g, b, a}` от 0 до 255: `bg`, `surface`/`surface2` (лицевая сторона, верх и низ), `primary`/`primary2`, `accent`/`accent2`, `outline`, `text`, `textOnAccent`, `textDisabled`, `shadow`, `gloss`, `focusRing`, `track`, а также `gain`/`loss` для монет. |
| `radius`, `radiusSmall` | Радиусы скругления для крупных и мелких элементов. |
| `outlineWidth` | Толщина обводки. |
| `gloss`, `shadow`, `textOutline` | Верхний блик (bool), отбрасываемая тень (`{ dx, dy, layers, grow }`), буквы с обводкой (bool). |
| `font` | Размеры текста: `small`, `label`, `button`, `title`. |
| `anim` | Масштабы и длительности наведения и нажатия, их сглаживание (`{ "OUT", "QUAD" }`), `typewriterCps`. |

```lua
local ui = UI.new{ theme = { colors = { accent = { 220, 90, 60, 255 } }, radius = 14 } }
ui:button{ text = "Delete", x = 100, y = 100, style = { colors = { primary = { 180, 40, 40, 255 } } } }
```

## Звуки

Таблицы `sfx` сопоставляют имени дескриптор звука, функцию или имя другой записи: `move`, `hover`, `click`, `toggle`, `decide`, `cancel`, `skip`, `error`, а также `coin` и `pay` для виджета монет. По умолчанию они проигрывают общие звуки `Move`, `Decide`, `Cancel`, `Skip` и `Error`, поэтому скин, который регистрирует их через `SHARED:SetSharedSFX`, даёт свои звуки всем глобальным экранам; отсутствующий звук просто не проигрывается.

## derive: библиотека с вашим оформлением

`UI.SimpleUI.derive(spec)` возвращает новую библиотеку с виджетами, менеджером, фокусом и вводом SimpleUI и собственным оформлением:

| Поле `spec` | Описание |
| --- | --- |
| `Theme` | Модуль темы: таблица, чей `DEFAULT` — полная тема по умолчанию (скопируйте `UI.SimpleUI.theme` из SimpleUI и измените её). |
| `Coins` | Необязательный класс виджета монет, для счётчика монет, нарисованного по-вашему. |
| `name` | Имя библиотеки. |

```lua
-- System/My Skin/Modules/Lib/MyUI.lua
local SimpleUI = require("UI.SimpleUI")
return SimpleUI.derive{
    name  = "MyUI",
    Theme = require("MyUI.theme"),
}
```

Укажите её в `ui_library`, и каждый глобальный модуль будет строить свои экраны с ней. `Modules/Lib/PopUI.lua` в Open-World Memories устроен именно так: SimpleUI с темой Bubblegum и собственным виджетом монет.
