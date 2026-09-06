<!-- api/input.md -->

# 입력

북 입력, 키보드, 마우스를 읽고 화면 텍스트 입력을 처리합니다.

## INPUT

설정된 북 입력, 원시 키보드, 마우스를 읽고 텍스트 입력 위젯을 만듭니다. 모든 스크립트에서 사용할 수 있습니다.

<div class="callout warn">
북 입력 이름은 키 설정 이름이며 대소문자 구분 없이 대조됩니다. LRed, RRed, LBlue, RBlue(플레이어 1), 플레이어 2-5용 LRed2P ... RBlue5P, Clap, Clap2P ... Clap5P, LeftChange, RightChange, Decide, Cancel, 그리고 시스템 키(Capture, SongVolumeIncrease, SongVolumeDecrease, DisplayHits, DisplayDebug, QuickConfig, SortSongs, ToggleAutoP1, ToggleAutoP2, ToggleTrainingMode, CycleVideoDisplayMode, Training* 키)입니다. 알 수 없는 이름은 false를 반환합니다. 북 메서드는 플레이어가 키 설정에서 그 입력에 바인딩한 장치를 읽습니다. INPUT은 키보드 키 이름을 엔진의 키 목록과 대소문자 구분 없이 대조합니다. 문자 A-Z, 숫자 D0-D9, F1-F15, Space, Return, Escape, Tab, Backspace, Delete, UpArrow, DownArrow, LeftArrow, RightArrow, LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, Home, End, PageUp, PageDown, NumberPad0-9, 그리고 다른 표준 키입니다. 마우스 위치는 레터박스를 보정한 논리 게임 표면 좌표입니다.
</div>

### 북 입력

| 메서드 | 설명 |
| --- | --- |
| `INPUT:Pressed(input)  -> bool` | 입력이 눌리는 프레임에 true. |
| `INPUT:Pressing(input)  -> bool` | 입력이 눌려 있는 동안 true. |
| `INPUT:Released(input)  -> bool` | 입력이 떼어지는 프레임에 true. |
| `INPUT:Releasing(input)  -> bool` | 입력이 눌려 있지 않은 동안 true. |
| `INPUT:RepeatWhilePressing(input, interval_seconds, callback)  -> nil` | 매 프레임 호출하십시오. 플레이어가 입력을 누르고 있는 동안 `interval_seconds`마다 `callback()`을 한 번 호출하고(첫 호출은 한 간격 뒤), 플레이어가 입력을 떼면 멈춥니다. |

### 키보드

| 메서드 | 설명 |
| --- | --- |
| `INPUT:KeyboardPressed(key)  -> bool` | 키가 눌리는 프레임에 true. |
| `INPUT:KeyboardPressing(key)  -> bool` | 키가 눌려 있는 동안 true. |
| `INPUT:KeyboardReleased(key)  -> bool` | 키가 떼어지는 프레임에 true. |
| `INPUT:KeyboardReleasing(key)  -> bool` | 키가 눌려 있지 않은 동안 true. |

### 마우스

<div class="callout warn">
버튼 이름: left, right, middle, button4, button5, 또는 숫자 인덱스(대소문자 구분 없음). 위치는 게임 표면 좌표이며, 마우스 장치가 없으면 위치 getter는 -1을 반환합니다. GetMouseDelta와 GetScrollDelta는 이전 호출 이후의 이동량을 반환하고 재설정되므로 프레임마다 한 번씩만 호출하십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `INPUT:GetMouseX()  -> number` | 게임 표면 좌표의 마우스 x. |
| `INPUT:GetMouseY()  -> number` | 게임 표면 좌표의 마우스 y. |
| `INPUT:GetMouseXY()  -> number, number` | 마우스 x와 y를 두 반환값으로. |
| `INPUT:IsMouseInside()  -> bool` | 마우스가 렌더링된 표면 위에 있으면 true, 레터박스 테두리 위에 있으면 false. |
| `INPUT:GetSurfaceWidth()  -> int` | 게임 표면(스크립트가 그리는 좌표 공간)의 너비. |
| `INPUT:GetSurfaceHeight()  -> int` | 게임 표면의 높이. |
| `INPUT:MousePressed(button)  -> bool` | 버튼이 눌리는 프레임에 true. |
| `INPUT:MousePressing(button)  -> bool` | 버튼이 눌려 있는 동안 true. |
| `INPUT:MouseReleased(button)  -> bool` | 버튼이 떼어지는 프레임에 true. |
| `INPUT:GetMouseDelta()  -> number, number` | 이전 호출 이후의 마우스 이동량(창 픽셀)을 dx, dy로. |
| `INPUT:GetScrollDelta()  -> number, number` | 이전 호출 이후의 휠 이동량(눈금)을 dx, dy로(위로 스크롤하면 dy가 양수). |
| `INPUT:SetMouseLocked(locked)  -> nil` | 자유 시점을 위해 커서를 잠그고 숨기거나(true) 복원하며(false), 델타 기준을 재설정합니다. |

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

### 텍스트 입력

| 메서드 | 설명 |
| --- | --- |
| `INPUT:CreateTextInput(initialText, maxLength)  -> textInput` | `initialText`로 미리 채워지고 UTF-8 `maxLength`바이트(기본값 64)로 제한된 텍스트 입력 위젯을 만듭니다. |

## 텍스트 입력 핸들

`INPUT:CreateTextInput`이 반환하는, IME 조합을 포함해 입력된 텍스트를 모으는 화면 텍스트 필드입니다.

<div class="callout warn">
필드가 활성인 동안 프레임마다 한 번 Update를 호출하고 DisplayText를 직접 그리십시오. 한 번에 하나의 텍스트 입력만 활성으로 유지하십시오. Debug 빌드에서는 오버레이 창도 필드를 표시하지만, Release 빌드는 스스로 아무것도 표시하지 않습니다. iOS와 Android에서는 게임이 플랫폼의 네이티브 텍스트 대화 상자를 열며, 플레이어가 확인하면 Update가 true를 반환합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `textInput:Update()  -> bool` | 이 프레임의 입력을 처리합니다. 플레이어가 Enter를 누르는 프레임에 true를 반환합니다. |
| `textInput.Text  -> string` | 현재 텍스트. 읽고 대입할 수 있습니다(nil은 빈 문자열이 됨). |
| `textInput.DisplayText  -> string` | 그리기용으로, 커서 위치에 깜빡이는 캐럿이 삽입된 현재 텍스트. |

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
