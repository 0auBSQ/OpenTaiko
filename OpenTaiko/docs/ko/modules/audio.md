<!-- modules/audio.md -->

# Audio

## Audio.MusicDuck

스테이지 위에서 무언가를 들려줘야 하는 동안(보상 팝업, 음성) 스테이지의 음악을 낮췄다가 다시 되돌립니다. 볼륨은 스무드스텝으로 최대와 낮춘 수준 사이를 부드럽게 오갑니다.

| 함수 | 설명 |
| --- | --- |
| `MusicDuck.new(opts)  -> duck` | `opts`: `sound`(음악, 코드에서 직접 배율을 적용하면 nil), `base`(최대일 때의 볼륨 퍼센트, 100), `level`(낮춘 배율, 0.3), `inS`(낮추는 데 걸리는 초, 0.35), `outS`(돌아오는 데 걸리는 초, 0.8). |
| `duck:update(dt, dimmed)  -> number` | 매 프레임 프레임 시간(초)과 함께 호출합니다. `dimmed`가 true이면 낮추고 false이면 되돌립니다. 소리의 볼륨 퍼센트에 `base * factor`를 적용하고 배율(0~1)을 반환합니다. |
| `duck:reset()` | 즉시 최대 볼륨으로 돌아갑니다(음악을 다시 시작하는 스테이지용). |
| `duck:apply()` | 현재 볼륨을 소리에 다시 기록합니다. |

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
