<!-- modules/audio.md -->

# Audio

## Audio.MusicDuck

Приглушает музыку сцены, пока поверх неё должно быть слышно что-то другое (всплывающее окно награды, реплика), а затем возвращает её. Громкость плавно переходит между полной и приглушённой по кривой smoothstep.

| Функция | Описание |
| --- | --- |
| `MusicDuck.new(opts)  -> duck` | `opts`: `sound` (музыка или nil, если ваш код сам применяет множитель), `base` (её громкость в процентах на полном уровне, 100), `level` (множитель приглушения, 0.3), `inS` (секунды на приглушение, 0.35), `outS` (секунды на возврат, 0.8). |
| `duck:update(dt, dimmed)  -> number` | Вызывайте каждый кадр с длительностью кадра в секундах; `dimmed`, равное true, приглушает, false — восстанавливает. Применяет `base * factor` к громкости звука в процентах и возвращает множитель (от 0 до 1). |
| `duck:reset()` | Сразу возвращает полную громкость (для сцены, которая перезапускает свою музыку). |
| `duck:apply()` | Ещё раз записывает текущую громкость в звук. |

```lua
local MusicDuck = require("Audio.MusicDuck")
local duck, last = nil, 0

function activate()
    duck = MusicDuck.new{ sound = bgm, base = 80 }
end

function update(ts)
    local dt = (ts - last) / 1000; last = ts
    duck:update(dt, rewardPopupShowing)
end
```
