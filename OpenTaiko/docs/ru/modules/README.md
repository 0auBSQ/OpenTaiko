<!-- modules/README.md -->

# Поставляемые модули

OpenTaiko поставляется с библиотекой Lua-модулей, которыми может пользоваться любой скин: вспомогательные модули (сглаживание, цвета, JSON, ввод, локализация), библиотека интерфейса, окно диалога, движок 3D-мира, а также правила и данные пагоды, секретного хранилища и магазина за монеты. Они лежат в папке игры, а не в скине, поэтому новый скин получает их, ничего не копируя. Этот раздел описывает каждый из них; [справочник API](../api/README.md) описывает глобальные объекты движка, на которых они построены.

## Global/Modules

`Global/Modules/` в папке установки игры содержит те же подпапки, что и `Modules/` скина: `Transitions/`, `Stages/`, `Activities/`, `ROActivities/` и `Lib/`. Игра загружает каждую глобальную категорию сразу после категории скина, каким бы ни был активный скин. Модули на этих страницах — это файлы `Global/Modules/Lib/`, к которым обращаются через `require`; в `Global/Modules/Stages/` также лежат сцены, которые может открыть любой скин, например [Моя комната](my-room.md).

Папка приходит вместе со сборкой игры и заменяется при каждом обновлении, поэтому скин не должен класть в неё файлы. Чтобы изменить поставляемый модуль, скопируйте его в свой скин под другим именем.

## Порядок require

`require` превращает точки в папки и не учитывает регистр, поэтому `require("Core.Easing")` загружает `Core/Easing.lua`. Папки, в которых он ищет, зависят от выполняющегося модуля:

| Вызывающий модуль | Где ищет, по порядку |
| --- | --- |
| Модуль скина (`System/<skin>/Modules/...`) | своя папка, `Modules/Lib/` скина, затем `Global/Modules/Lib/` |
| Глобальный модуль (`Global/Modules/...`) | своя папка, `Global/Modules/Lib/`, затем `Modules/Lib/` скина |

У файла из Lib нет собственного состояния Lua: он выполняется в состоянии модуля, который подключил его через require, поэтому вызовы require внутри него следуют порядку этого вызывающего модуля. Значит, файл в `Modules/Lib/` скина с тем же путём, что и поставляемый модуль, заменяет его для всех вызовов require из модулей скина, включая вызовы require внутри других поставляемых модулей. Не используйте для своих файлов Lib имена папок из таблицы ниже, если только вы не хотите заменить модуль.

## Именование

Каждый компонент — это папка в `Global/Modules/Lib/`, а его модули подключаются как `<Component>.<Module>`:

| Компонент | Модули | Страница |
| --- | --- | --- |
| `Core` | `Easing`, `Color`, `Util`, `Json`, `Here` | [Core](core.md) |
| `Input` | `NavInput` | [Input](input.md) |
| `Audio` | `MusicDuck` | [Audio](audio.md) |
| `Locale` | `i18n` | [Locale](locale.md) |
| `Calendar` | `almanac` | [Calendar](calendar.md) |
| `Songs` | `PlayStrings` | [Songs](songs.md) |
| `World` | `OWM3d` и его части | [World](world.md) |
| `UI` | `SimpleUI`, `Pick` | [UI](ui.md) |
| `Saves` | `SavePicker` | [Saves](saves.md) |
| `Characters` | `Student` | [Characters](characters.md) |
| `VN` | `Dialogue` | [VN](vn.md) |
| `Pagoda` | `Levels`, `Floor`, `Rewards`, `Progress`, `Songs`, `Run`, `Texts` | [Pagoda](pagoda.md) |
| `Vault` | `Keys`, `Chests`, `Texts` | [Vault](vault.md) |
| `Shop` | `Daily`, `Catalog`, `Texts`, `Seasonal`, `Popup` | [Shop](shop.md) |

## Имена, которые сохраняет Open-World Memories

Раньше поставляемый скин хранил большую часть этих файлов в своей папке `Modules/Lib/`. Теперь каждый старый файл — это переадресация из двух строк, поэтому модуль скина, который всё ещё подключает старое имя, получает глобальный модуль, и оба имени используют один экземпляр (одно состояние, один кэш):

```lua
-- moved to Global/Modules/Lib/Core/Easing.lua
return require("Core.Easing")
```

| Старое имя | Поставляемый модуль |
| --- | --- |
| `Easing`, `Color`, `Util` | `Core.Easing`, `Core.Color`, `Core.Util` |
| `NavInput`, `MusicDuck`, `PlayStrings` | `Input.NavInput`, `Audio.MusicDuck`, `Songs.PlayStrings` |
| `i18n`, `almanac` | `Locale.i18n`, `Calendar.almanac` |
| `OWM3d`, `OWM3d.<part>` | `World.OWM3d`, `World.OWM3d.<part>` |
| `PopUI.<part>` (общий код: `manager`, `widget`, `shape`, `util`, `cache`, `sfx`, все `widgets.*`, кроме `widgets.coins`) | `UI.SimpleUI.<part>` |
| `SavePicker`, `Student`, `dialogue` | `Saves.SavePicker`, `Characters.Student`, `VN.Dialogue` |

Сам `PopUI` остаётся модулем скина: это SimpleUI с собственным оформлением скина (см. [UI](ui.md)). Новый код должен подключать поставляемые имена.

## Ресурсы разрешаются относительно вызывающего модуля

Относительные пути, передаваемые в `TEXTURE`, `SOUND`, `VIDEO`, `JSONLOADER`, `SQL`, `DATABASE`, `STORAGE` и `SHARED`, разрешаются относительно папки вызывающего модуля и никогда относительно файла Lib, который делает вызов. Поставляемый модуль, у которого есть собственные файлы (картинка, JSON-пул, база данных), находит их через `Core.Here`, возвращающий папку самого файла, и загружает их вариантами с абсолютным путём:

```lua
-- Global/Modules/Lib/MyPart/Thing.lua
local HERE = require("Core.Here")(...)          -- ".../Global/Modules/Lib/MyPart/"

local icon = TEXTURE:CreateTextureFromAbsolutePath(HERE .. "icon.png")
local beep = SOUND:CreateSFXFromAbsolutePath(HERE .. "beep.ogg")
local data = JSONLOADER:JsonParseFileAny(HERE .. "data.json")
SHARED:SetSharedTextureUsingAbsolutePath("thing_icon", HERE .. "icon.png")
```

Передавайте `...` с верхнего уровня файла: именно там `require` передаёт каждому файлу имя его модуля и его абсолютный путь. Относительный путь, который вызывающий модуль передаёт поставляемому модулю (файл модели для `World.OWM3d`, звук для виджета), принадлежит вызывающему модулю и, как обычно, разрешается относительно его папки.

## Строки живут вместе с модулем

`THEME:GetSkinString` читает только `Locales/` активного скина, поэтому поставляемый модуль никогда не использует его для своего текста. Его строки лежат в папке `lang/` рядом с ним, по одному JSON-файлу на функцию и язык (`lang/en/vault.json`, `lang/ja/vault.json`, ...), и читаются через [Locale.i18n](locale.md) с указанием папки модуля:

```lua
local HERE = require("Core.Here")(...)
local T = require("Locale.i18n").texts("vault", HERE)   -- HERE .. "lang/<code>/vault.json"
local name = T:tr("chest_1")
```

Английский (`lang/en/`) служит запасным вариантом для языка, у которого нет файла или ключа. Глобальная сцена или Activity делает то же без аргумента с папкой: `texts(name)` читает папку `lang/` самой сцены.

## Общее состояние

Текстуры, звуки и строки `SHARED` образуют одно пространство имён на всю игру, общее для модулей скина и глобальных модулей. Поставляемые модули используют несколько фиксированных ключей, перечисленных на их страницах (например, общую строку `ui_library` и текстуры `myroom_thumb_<id>`). Добавляйте к своим ключам префикс, чтобы они не пересекались. Данные сохранения (триггеры, счётчики и строки) принадлежат файлу сохранения, а не модулю; так пагода, хранилище, магазин и Моя комната передают друг другу предметы.
