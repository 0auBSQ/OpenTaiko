<!-- modules/custom-stages.md -->

# Пользовательские сцены

Пользовательская сцена — это сцена, которую любой может сделать, распространить и установить и которую показывает меню «Пользовательские сцены» скина. Она объявляет пространство имён `CStage` через [STAGES:SetNamespace](../api/activities.md) и возвращается через общую строку `custom_stage_return` к сцене, которая её открыла. `Stages.CustomStages` делает и то и другое и составляет список пользовательских сцен для меню. `Global/Modules/Stages/custom_stage_tutorial` — одна из них: руководство прямо в игре о том, как создавать пользовательские сцены.

## Объявление

В начале Script.lua, вне функций:

```lua
-- Global/Modules/Stages/lantern_walk/Script.lua, at the top
local CS = require("Stages.CustomStages")

CS.declare{
    title       = CS.allLanguages("menu", "title"),       -- lang/<code>/menu.json, every language
    description = { en = "A calm walk down a street of paper lanterns." },
}
```

`CS.declare(info)` — это `STAGES:SetNamespace("CStage", info)`. Меню читают два поля info:

| Поле | Показывает |
| --- | --- |
| `title` | Название сцены в списке: строка или таблица по кодам языков. Имя папки, если поля нет. |
| `description` | Одну-две строки под списком, пока сцена выбрана; необязательно. |

Таблица по кодам языков даёт текст на языке игры, иначе на английском, иначе на любом из имеющихся языков. Остальные поля ваши: меню передают info без изменений.

## Путь назад

Меню, открывающее пользовательскую сцену, записывает в общую строку `custom_stage_return` сцену для возврата, `"<stage>"` или `"<stage>|<transition>"`, а затем переходит в пользовательскую сцену. Та при входе читает и очищает строку и уходит в эту сцену или в `_title`, если строка пуста:

```lua
local back, backTransition

function activate()
    back, backTransition = CS.takeReturn()   -- "_title" when the string is empty
end

function update(ts)
    if leaving then return Exit("stage", back, backTransition) end
end
```

Строка читается при каждом входе. Пользовательская сцена, которая уходит в другую свою сцену и возвращается, перед уходом снова записывает строку, как это делает «Моя комната» с `my_room_return`.

## Stages.CustomStages

| Функция | Описание |
| --- | --- |
| `CS.declare(info)  -> boolean` | Объявляет вызывающую сцену пользовательской: `STAGES:SetNamespace("CStage", info)`. |
| `CS.allLanguages(name, id)  -> table` | Строку `id` из `lang/<code>/<name>.json` вызывающей сцены, взятую из каждого из девяти языков, где она есть, в виде таблицы по кодам языков. Предназначена для `declare`, который выполняется до выбора языка. |
| `CS.localized(value, code?)  -> string or nil` | Объявленный текст на `code` (по умолчанию язык игры): строка как есть, таблица по кодам языков, как описано выше. |
| `CS.list(code?)  -> table` | Пользовательские сцены, доступные вызывающему модулю, упорядоченные по названию: `{ name, isGlobal, title, description, info }`, где `title` и `description` уже на нужном языке. |
| `CS.enter(name, back, transition?)` | Записывает `back` в `custom_stage_return` и переходит в сцену `name`; пишите `return CS.enter(...)` в update сцены. |
| `CS.takeReturn()  -> stage, transition` | Читает и очищает `custom_stage_return`: сцена (`_title`, если строка пуста) и переход (nil для перехода по умолчанию). |
| `CS.open(o)`, `CS.update(ts)`, `CS.draw()`, `CS.isOpen()`, `CS.close()` | Список в виде диалога поверх экрана вызывающей сцены, построенного библиотекой интерфейса скина ([UI.Pick](ui.md)). `update` возвращает nil, пока диалог открыт, `"cancel"` или выбранную запись `CS.list`; в обоих случаях диалог закрывается. `o`: `title`, `ui` (таблица библиотеки или её имя для require), `theme`, `sfx`, `navPlayer` (по умолчанию 0: клавиатура и геймпад, без барабанов). Если пользовательских сцен нет, он сообщает об этом над кнопкой ОК. |
| `CS.NAMESPACE`, `CS.RETURN_KEY` | `"CStage"` и `"custom_stage_return"`. |

Его строки лежат в его собственной папке `lang/`, на девяти языках ([Locale.i18n](locale.md)). Open-World Memories открывает список из пункта «Пользовательские сцены» своего титульного экрана, над «Настройками»:

```lua
-- a menu stage: CS.open{ title = "Custom Stages" } when its entry is chosen, then
function update(ts)
    if CS.isOpen() then
        local r = CS.update(ts)
        if r ~= nil and r ~= "cancel" then return CS.enter(r.name, "_title") end
        return
    end
end

function draw()
    if CS.isOpen() then CS.draw() end
end
```

## Где лежит пользовательская сцена

| Папка | Работает со скинами |
| --- | --- |
| `Global/Modules/Stages/<name>/` | Со всеми. |
| `System/<skin>/Modules/Stages/<name>/` | Только с этим скином. |

Если имя есть в обоих местах, меню скина показывает сцену скина, а глобальное меню — глобальную, то есть ту, которую открыл бы `Exit`. Игра создаёт сцены при запуске и при повторной загрузке скина, поэтому сцена, установленная во время работы игры, появится после перезапуска.

## Поделиться сценой

Пользовательская сцена — это её папка. Запакуйте в zip саму папку, чтобы архив начинался с `<name>/Script.lua`, вместе со всем, что она загружает, и авторами её ресурсов. Игроки распаковывают её в `Global/Modules/Stages/` папки игры или в `Modules/Stages/` скина и перезапускают игру. Обновление добавляет и заменяет файлы, но ничего не удаляет, поэтому выберите имя папки, которое не использует ни одна встроенная сцена.

## Руководство

`custom_stage_tutorial` показывает прямо в игре четыре вещи, которые нужно знать, чтобы сделать пользовательскую сцену и поделиться ею: куда класть её папку (и куда нельзя), пространство имён `CStage`, всё используемое — в её папке, а снаружи только встроенные модули, и где читать эту документацию — вкладку «Документация» в OpenTaiko Hub. Его страницы — это изображения из папки `Textures/` и тексты из папки `lang/`, так что их можно заменять по одному файлу.
