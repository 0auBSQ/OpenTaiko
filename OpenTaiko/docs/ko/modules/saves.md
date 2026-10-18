<!-- modules/saves.md -->

# Saves

## Saves.SavePicker

스테이지를 열기 전에 묻는 "누구의 세이브?" 질문입니다. 로컬 세이브 목록으로, 각 세이브에 네임플레이트와 정보 한 줄(또는 코인 수)이 붙습니다. 묻는 스테이지는 여는 스테이지에 `SHARED`를 통해 선택을 넘깁니다. 목록은 호출하는 쪽이 따로 넘기지 않으면 스킨의 UI 라이브러리([UI.Pick](ui.md))로 만들어지며, 제목은 모듈 자체의 `lang/`에서 가져옵니다.

| 함수 | 설명 |
| --- | --- |
| `SavePicker.open(o)  -> slot or nil` | 목록을 엽니다. 세이브가 하나뿐이면(또는 없으면) 아무것도 열지 않고 그 슬롯(없으면 0)을 즉시 반환합니다. 그렇지 않으면 nil을 반환하고 목록은 열린 채로 남습니다. |
| `SavePicker.update(ts)  -> nil, "cancel" or slot` | 목록이 열려 있는 동안 매 프레임 호출합니다. 표시 중에는 nil, 그다음 `"cancel"` 또는 고른 0부터 시작하는 슬롯을 반환합니다. 어느 경우든 목록은 닫힙니다. |
| `SavePicker.draw()` | 목록을 그립니다. |
| `SavePicker.isOpen()  -> bool`, `SavePicker.close()` | 목록이 표시 중인지 여부; 목록을 닫습니다. |
| `SavePicker.pass(slot)` | 곧 열릴 스테이지에 슬롯을 넘깁니다(공유 문자열 `entry_save`). |
| `SavePicker.take()  -> slot or nil` | 열린 스테이지에서: 넘겨받은 슬롯을 한 번만 읽습니다(문자열은 지워집니다). 슬롯 없이 왔거나 그 세이브가 더 이상 없으면 nil. |
| `SavePicker.first()  -> slot` | 존재하는 첫 번째 세이브. 선택 없이 열린 스테이지용입니다. |
| `SavePicker.saves()  -> table` | 존재하는 세이브들: `{ { value = slot, save = save file }, ... }`. |
| `SavePicker.text(id)  -> string` | 모듈의 문자열(`title`, `title_room`, `title_shop`) 하나를 게임 언어로 반환합니다. |

| `o` 필드 | 설명 |
| --- | --- |
| `title` | 목록의 제목. |
| `kind` | `title`을 주지 않았을 때 모듈 자체의 "누구의 방?" 또는 "누구의 숍?" 제목을 쓰기 위한 `"room"` 또는 `"shop"`. |
| `info(slot, save)` | 세이브 행의 오른쪽에 표시할 텍스트를 반환하는 함수, 또는 nil. |
| `coins` | true이면 대신 각 세이브의 코인 수를 표시합니다. |
| `theme`, `sfx` | UI 매니저에 전달됩니다. |
| `ui` | UI 라이브러리 테이블, 또는 그 require 이름. 기본값은 [UI.Pick](ui.md). |

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
