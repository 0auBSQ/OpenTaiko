<!-- modules/songs.md -->

# Songs

## Songs.PlayStrings

Общие строки, которые сцена задаёт для одной игры, которую она запускает: какой фон загрузки показать, стиль таблички дана, всё, что читает фон или ROActivity этой игры. Строки действуют только для чарта, которым они помечены, поэтому значения, оставшиеся от игры, из которой сцена так и не вернулась, не попадают ни в какую другую игру.

| Функция | Описание |
| --- | --- |
| `PlayStrings.stamp()` | Помечает выбранный сейчас чарт (`SONGMOUNT`) как тот, которому принадлежат строки. Вызывайте сразу после монтирования чарта, перед `Exit("play")`. |
| `PlayStrings.clear()` | Снимает пометку; вызывайте, когда игра вернулась. |
| `PlayStrings.get(key)  -> string` | Общая строка `key`, пока выбранный чарт совпадает с помеченным, иначе `""`. |
| `PlayStrings.forTitle(key, title)  -> string` | Та же проверка по названию чарта, для читателя, которому известно только название (табличка дана). |

Сами значения — обычные общие строки, задаваемые через `SHARED:SetSharedString(key, value)`. Пометка хранится в общей строке `play_strings_chart`.

```lua
local PlayStrings = require("Songs.PlayStrings")

-- the stage, after mounting the chart
SHARED:SetSharedString("my_loading_art", "night")
PlayStrings.stamp()

-- a background script of that play
local art = PlayStrings.get("my_loading_art")   -- "night"; "" for any other chart
```
