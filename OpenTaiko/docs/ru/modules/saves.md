<!-- modules/saves.md -->

# Saves

## Saves.SavePicker

Вопрос «чьё сохранение?», который задаётся перед открытием сцены: список локальных сохранений, у каждого его табличка имени и строка сведений (или число монет). Сцена, которая задаёт вопрос, передаёт выбор открываемой сцене через `SHARED`. Список строится библиотекой интерфейса скина ([UI.Pick](ui.md)), если вызывающий модуль не передал другую, а его заголовки берутся из собственной папки `lang/` модуля.

| Функция | Описание |
| --- | --- |
| `SavePicker.open(o)  -> slot or nil` | Открывает список. Если сохранение только одно (или их нет), ничего не открывает и сразу возвращает этот слот (0, если сохранений нет); иначе возвращает nil, и список остаётся открытым. |
| `SavePicker.update(ts)  -> nil, "cancel" or slot` | Вызывайте каждый кадр, пока список открыт: nil, пока он на экране, затем `"cancel"` или выбранный слот, считая с 0. В обоих случаях список закрывается. |
| `SavePicker.draw()` | Рисует список. |
| `SavePicker.isOpen()  -> bool`, `SavePicker.close()` | Показан ли список; закрывает его. |
| `SavePicker.pass(slot)` | Передаёт слот сцене, которая сейчас откроется (общая строка `entry_save`). |
| `SavePicker.take()  -> slot or nil` | В открытой сцене: переданный ей слот, читается один раз (строка очищается); nil, если сцена открыта без слота или сохранения больше нет. |
| `SavePicker.first()  -> slot` | Первое существующее сохранение, для сцены, открытой без выбора. |
| `SavePicker.saves()  -> table` | Существующие сохранения: `{ { value = slot, save = save file }, ... }`. |
| `SavePicker.text(id)  -> string` | Одна из строк модуля (`title`, `title_room`, `title_shop`) на языке игры. |

| Поле `o` | Описание |
| --- | --- |
| `title` | Заголовок списка. |
| `kind` | `"room"` или `"shop"` — собственный заголовок модуля «Чья комната?» или «Чей магазин?», если `title` не задан. |
| `info(slot, save)` | Функция, возвращающая текст справа в строке сохранения, или nil. |
| `coins` | true — показывать вместо этого число монет каждого сохранения. |
| `theme`, `sfx` | Передаются менеджеру интерфейса. |
| `ui` | Таблица библиотеки интерфейса или её имя для require; по умолчанию [UI.Pick](ui.md). |

```lua
local SavePicker = require("Saves.SavePicker")
local NavInput = require("Input.NavInput")
local picking = false

function update(ts)
    if picking then
        local r = SavePicker.update(ts)
        if r == nil then return end
        picking = false
        if r ~= "cancel" then SavePicker.pass(r); return Exit("stage", "my_shop") end
    elseif NavInput.decide() then
        local slot = SavePicker.open{ kind = "shop", coins = true }
        if slot == nil then picking = true
        else SavePicker.pass(slot); return Exit("stage", "my_shop") end
    end
end

function draw()
    if picking then SavePicker.draw() end
end

-- in my_shop's activate
local slot = SavePicker.take() or SavePicker.first()
```
