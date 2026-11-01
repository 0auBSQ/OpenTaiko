<!-- modules/input.md -->

# Input

## Input.NavInput

모든 장치를 한꺼번에 다루는 메뉴 탐색입니다. 키보드 화살표, Return과 Escape, 키 설정의 공통 입력(`RightChange`, `LeftChange`, `Decide`, `Cancel`), 그리고 각 플레이어의 북(파란 테두리로 이동, 빨간 면으로 결정). 화면은 키를 나열하지 않고 "플레이어가 결정을 눌렀는가?"를 물을 수 있으며, 플레이어 자신의 키 설정이 적용됩니다.

각 바인딩은 그 프레임에 true를 반환하는 함수입니다. 다른 입력 이벤트에는 이름에 `Pressed`(접미사가 없는 것과 같음), `Pressing`, `Released`를 붙입니다: `decide()`, `decidePressing()`, `leftReleased()`.

| 바인딩 | 반응하는 입력 |
| --- | --- |
| `right`, `left` | 오른쪽 또는 왼쪽 화살표, `RightChange` / `LeftChange`, 파란 테두리. |
| `rightKeyboard`, `leftKeyboard` | 화살표 키만. |
| `up`, `down` | 위 또는 아래 화살표(북에는 세로 입력이 없습니다). |
| `upOrPadLeft`, `downOrPadRight` | 위(아래) 화살표, 또는 패드나 북에서 `left`(`right`)가 반응하는 입력: 북으로 스크롤할 수 있는 세로 목록에 씁니다. |
| `decide` | Return, `Decide`, 빨간 면. |
| `cancel` | Escape, `Cancel`. |
| `rightOtherPlayer`, `leftOtherPlayer` | 플레이어별 세트 전용: 화살표, 또는 다른 플레이어의 파란 테두리. |

| 함수 또는 필드 | 설명 |
| --- | --- |
| `NavInput.decide()`, `NavInput.left()`, ...  `-> bool` | 모든 플레이어에 대한 바인딩: 키보드, 공통 입력, 모든 플레이어의 북. |
| `NavInput.getPn(player)  -> table` | 플레이어 한 명의 바인딩: 1~5는 그 플레이어의 북과 키보드와 공통 입력, 0은 키보드와 공통 입력만, `""` 또는 nil은 모든 플레이어(위의 약식과 같음). |
| `NavInput.p[player]` | 0~5와 `""`에 대한 `getPn`의 테이블로, 한 번만 만들어집니다. |
| `NavInput.bindingNames(binding, player?)  -> string[]` | 바인딩이 반응하는 입력을 힌트용 표시 이름으로 반환합니다. 키보드 키가 먼저, 그다음 키 설정의 바인딩(`"Pad 3"`, `"MIDI 38"`, `"Mouse Left"`)이며 중복은 없습니다. |
| `NavInput.keyName(key)  -> string` | 키보드 키의 표시 이름(`"Return"`은 `"Enter"`, `"D1"`은 `"1"`). 키가 없으면 nil. |

```lua
local NavInput = require("Input.NavInput")
local p1 = NavInput.getPn(1)

function update(ts)
    if p1.right() then cursor = cursor + 1 end
    if p1.left() then cursor = cursor - 1 end
    if NavInput.cancel() then return Exit("stage", "_title") end
end
```
