<!-- modules/ui.md -->

# UI

마우스, 키보드, 패드로 조작하며 하나의 하이라이트를 공유하는 메뉴와 대화 상자용 UI 라이브러리입니다. `UI.SimpleUI`는 OpenTaiko가 함께 제공하는 최소한의 라이브러리이며, 스킨은 `derive`로 여기에 자신만의 모양을 입힐 수 있습니다. Open-World Memories는 PopUI로 그렇게 합니다. 전역 모듈은 `UI.Pick`에서 라이브러리를 받는데, 이는 스킨이 고른 라이브러리(또는 스테이지가 지정한 라이브러리)를 주므로 스킨의 모든 화면이 같은 모양이 됩니다.

## UI.Pick

`require("UI.Pick")`은 함수를 반환하며, 그 함수를 호출하면 UI 라이브러리 테이블이 반환됩니다.

| 호출 | 라이브러리 |
| --- | --- |
| `Pick(name)` | 그 require 이름의 라이브러리(`"PopUI"`, `"UI.SimpleUI"`, ...). 특정 라이브러리를 원하는 스테이지용입니다. |
| `Pick()` | 공유 문자열 `ui_library`가 가리키는 라이브러리로, 스킨이 한 번 설정합니다(Open-World Memories의 `_boot`는 `"PopUI"`를 설정합니다). |
| `ui_library`가 없을 때의 `Pick()` | `UI.SimpleUI`. |

로드되지 않는 이름이나 `new` 함수가 없는 것을 로드하는 이름도 `UI.SimpleUI`가 되므로, 다른 스킨이 공유 문자열에 남긴 이름이 있어도 문제가 생기지 않습니다. 이 선택은 평범한 Lua와 공유 문자열만으로 이루어지며, 엔진은 이에 대해 아무것도 모릅니다.

```lua
-- in a skin's _boot onStart: the library every global module will use
SHARED:SetSharedString("ui_library", "MyUI")

-- in any module
local UI = require("UI.Pick")()
local ui = UI.new{ navPlayer = 1 }
```

## UI.SimpleUI

`UI.new(opts)`는 매니저를 만듭니다. 매니저는 위젯을 소유하고, 입력을 전달하고, 위젯을 그립니다. 매 프레임 `ui:update(ts)`와 `ui:draw()`를 호출하십시오.

| `opts` 필드 | 설명 |
| --- | --- |
| `theme` | 라이브러리의 기본 모양 위에 병합하는 테마 테이블(테마 설정 참고). |
| `sfx` | 이름별 소리로, 기본값 위에 병합됩니다(소리 참고). |
| `bg` | true이면 먼저 화면 전체에 테마의 `bg` 색을 그립니다. |
| `navPlayer` | 북으로도 탐색하는 플레이어. `NavInput.getPn`과 같은 형식이며, nil이면 모든 플레이어입니다. |
| `nav` | `"spatial"`: 화살표 키가 그 방향에서 가장 가까운 위젯으로 이동합니다(격자). 기본적으로는 위아래 키가 만든 순서대로 위젯을 오갑니다. |

| 메서드 | 설명 |
| --- | --- |
| `ui:update(ts)  -> "cancel" or nil` | 한 프레임 동안 입력과 애니메이션을 처리합니다. Cancel이 눌리면 스테이지가 처리하도록 `"cancel"`을 반환합니다. |
| `ui:draw()` | 보이는 모든 위젯을 만든 순서대로 그립니다. |
| `ui:button(o)`, `ui:label(o)`, ...  `-> widget` | 위젯을 만들어 추가하고 반환합니다(위젯 참고). |
| `ui:add(w)`, `ui:remove(w)` | 직접 만든 위젯을 추가하거나 제거합니다. |
| `ui:clear()` | 모든 위젯을 제거하고 그 캔버스를 해제합니다. |
| `ui:disposeWidgets()` | 위젯의 캔버스를 해제합니다. 캔버스는 저절로 해제되지 않으므로 매니저를 버리기 전에 호출하십시오. |
| `ui:focus(w)  -> bool`, `ui:focused()  -> widget` | 키 포커스를 위젯으로 옮깁니다; 포커스를 가진 위젯. |
| `ui:focusNext()`, `ui:focusPrev()` | 키 포커스를 차례로 옮깁니다. |
| `ui:setTheme(t)`, `ui:setSfx(t)` | 매니저의 테마나 소리를 교체합니다. 모든 위젯의 스타일이 다시 적용됩니다. |
| `ui:isCapturing()  -> bool` | 텍스트 상자가 키 입력을 받는 동안 true. 그동안은 자신의 단축키를 건너뛰십시오. |
| `ui:mouseActive()  -> bool` | 마지막으로 쓴 장치가 마우스인 동안 true. |
| `ui:drawText(size, str, x, y, color, opacity?, scale?, maxWidth?)` | UI의 글리프 폰트로 왼쪽 위를 기준으로 텍스트를 그립니다. |
| `ui:drawTextEx(size, str, x, y, fg, bg, opacity, scale, maxWidth, anchor)` | 같은 기능에 외곽선 색과 아홉 가지 앵커 이름 중 하나를 더한 것. |
| `ui:measureText(size, str)`, `ui:wrapLines(size, str, maxWidth)`, `ui:ellipsize(size, str, maxWidth)` | 같은 폰트로 텍스트를 측정하고, 줄바꿈하고, 줄입니다. |
| `ui:textNudge(size)  -> number` | 텍스트 한 줄을 가운데 선에 맞춰 중앙에 놓는 오프셋. |
| `UI.flushSharedFonts()` | 캐시한 폰트를 버립니다. 언어(그리고 그에 따른 폰트)가 바뀌면 호출하십시오. |

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

## 위젯

모든 위젯은 `x`, `y`, `w`, `h`와 함께 `enabled`, `visible`, `focusable`, `mouseOnly`(마우스로 올리고 클릭할 수 있지만 키 탐색은 건너뜀), `style` 또는 `theme`(이 위젯만의 테마 필드), `sfx`, `disabledLook = "fade"`를 받습니다. 옵션은 위젯의 필드가 되며, 바꿀 때는 위젯의 스타일을 다시 적용하는 메서드(`setEnabled(b)`, `setVisible(b)`, `setText(t)`, ...)를 쓰십시오.

| 팩토리 | 옵션과 메서드 |
| --- | --- |
| `ui:button(o)` | `text` 또는 `icon`(`"play"`, `"pause"`, `"stop"`, `"back"`, `"close"`, `"lock"`, `"repeat"`, ...), `accent`, `onClick(b)`. `w`/`h`를 생략하면 크기가 레이블에 맞춰집니다. `setText`, `setIcon`. |
| `ui:label(o)` | `text`, `size`(`"title"`, `"label"`, `"small"` 또는 숫자), `color`, `align`, `chip`(둥근 배경). 포커스를 받지 않습니다. `setText`. |
| `ui:toggle(o)`, `ui:checkbox(o)` | `text`, `value`, `onChange(v)`. `setValue(v, silent)`. |
| `ui:slider(o)` | `min`, `max`, `step`, `value`, `showValue`, `valueW`, `onChange(v)`. 드래그하거나, 포커스된 상태에서 왼쪽/오른쪽 키로 조작합니다. `setValue(v, silent)`. |
| `ui:textbox(o)` | `value`, `placeholder`, `maxLen`, `onChange`, `onSubmit`. 클릭하거나 Decide를 눌러 입력합니다. |
| `ui:panel(o)` | `title`. 이후에 만든 위젯 뒤에 그려지는 컨테이너이며, `panel:content()`가 안쪽 사각형을 줍니다. 포커스를 받지 않습니다. |
| `ui:bubble(o)` | `text`, `tail`(`"bottom"`, `"top"`, `"left"`, `"right"`), `tailPos`, `typewriter`, `onDone`. `setText`, `setTail`, `setSize`. |
| `ui:menu(o)` | `items`(문자열, 또는 `{ text, value, locked, mark }`), `selected`, `rowHeight`, `onChange(i, item)`, `onSelect(i, item)`, `onLockedSelect`, `drawLocked`, `hoverSelects`, `scrollbar`. 스크롤되는 목록. `setSelected(i)`. |
| `ui:chooser(o)` | `options`, `index`, `wrap`, `onChange(i, option)`. 옵션을 넘기는 스테퍼(왼쪽/오른쪽). `value()`, `setOptions`, `setIndex`. |
| `ui:settingsList(o)` | `rows`(`{ header = "..." }` 또는 `{ opt = option }`), `onFocusRow`, `onActivateRow`, `scrollbar`. 설정 화면의 옵션 행 목록. `setRows`. |
| `ui:coins(o)` | 코인 카운터(아래 참고). 포커스를 받지 않습니다. |

위젯 클래스는 하위 클래스를 만들 수 있도록 라이브러리의 필드(`UI.Button`, `UI.Menu`, ...)이며, 그 밖에 `UI.Widget`(기본 클래스), `UI.Shape`(위젯이 구워 두는 둥근 도형, 그림자, 아이콘), `UI.Theme`, `UI.Sfx`, `UI.Cache`(풀링된 캔버스)도 있습니다.

## 코인 위젯

`ui:coins{ amount, plain, size, iconSize, color, edge }`는 알약 모양 바탕 위에 코인 아이콘과 함께 금액을 표시합니다(`plain = true`: 아이콘과 숫자만 오른쪽 정렬). 그리는 방식은 라이브러리마다 다릅니다. SimpleUI의 아이콘은 옆에 있는 `Coin.png`이고, PopUI는 기본 제공 스킨의 코인 상자를 유지합니다.

| 메서드 | 설명 |
| --- | --- |
| `c:setAmount(n)` | `n`을 즉시 표시합니다. |
| `c:add(n)` | 숫자를 `n`만큼 굴리며 바꿉니다. |
| `c:gain(n)`, `c:pay(n)` | 같은 동작에 더해, 위에 `+n` 또는 `-n`을 띄우고 소리 `coin` 또는 `pay`를 냅니다. |
| `c:amount()  -> number` | 향해 가고 있는 금액. |
| `c:setPrice(n)`, `c:setBonus(n)` | 위에 가격 또는 `+n` 보너스를 표시합니다. nil이면 지웁니다. |
| `c:drawCoin(x, y, sx, sy, opacity)` | 코인 아이콘만 (x, y)를 중심으로 그립니다. |

## 테마 설정

테마는 하나의 테이블입니다. 매니저는 각 위젯의 모양을 라이브러리 기본값, 그다음 매니저의 `theme`, 그다음 위젯의 `style` 순서로 결정합니다. 테이블은 필드 단위로 병합되므로 테마에는 바꾸는 것만 적으면 됩니다.

| 필드 | 설명 |
| --- | --- |
| `colors` | 0~255의 `{r, g, b, a}` 색: `bg`, `surface`/`surface2`(면의 위와 아래), `primary`/`primary2`, `accent`/`accent2`, `outline`, `text`, `textOnAccent`, `textDisabled`, `shadow`, `gloss`, `focusRing`, `track`, 그리고 코인용 `gain`/`loss`. |
| `radius`, `radiusSmall` | 큰 요소와 작은 요소의 모서리 반지름. |
| `outlineWidth` | 테두리 두께. |
| `gloss`, `shadow`, `textOutline` | 위쪽 광택(bool), 드롭 섀도(`{ dx, dy, layers, grow }`), 외곽선 글자(bool). |
| `font` | 글자 크기: `small`, `label`, `button`, `title`. |
| `anim` | 마우스 올림과 누름의 배율과 시간, 그 이징(`{ "OUT", "QUAD" }`), `typewriterCps`. |

```lua
local ui = UI.new{ theme = { colors = { accent = { 220, 90, 60, 255 } }, radius = 14 } }
ui:button{ text = "Delete", x = 100, y = 100, style = { colors = { primary = { 180, 40, 40, 255 } } } }
```

## 소리

`sfx` 테이블은 이름을 소리 핸들, 함수, 또는 다른 항목의 이름에 대응시킵니다: `move`, `hover`, `click`, `toggle`, `decide`, `cancel`, `skip`, `error`, 그리고 코인 위젯용 `coin`과 `pay`. 기본적으로 공유 소리 `Move`, `Decide`, `Cancel`, `Skip`, `Error`를 재생하므로, 이를 `SHARED:SetSharedSFX`로 등록한 스킨은 모든 전역 화면에 소리를 줍니다. 없는 소리는 무음으로 남습니다.

## derive: 자신만의 모양을 입힌 라이브러리

`UI.SimpleUI.derive(spec)`은 SimpleUI의 위젯, 매니저, 포커스, 입력을 가지면서 자신만의 모양을 지닌 새 라이브러리를 반환합니다.

| `spec` 필드 | 설명 |
| --- | --- |
| `Theme` | 테마 모듈: `DEFAULT`가 완전한 기본 테마인 테이블입니다(SimpleUI의 `UI.SimpleUI.theme`를 복사해 수정하십시오). |
| `Coins` | 선택 사항인 코인 위젯 클래스로, 자기 방식으로 그리는 코인 카운터용입니다. |
| `name` | 라이브러리의 이름. |

```lua
-- System/My Skin/Modules/Lib/MyUI.lua
local SimpleUI = require("UI.SimpleUI")
return SimpleUI.derive{
    name  = "MyUI",
    Theme = require("MyUI.theme"),
}
```

이 이름을 `ui_library`에 지정하면 모든 전역 모듈이 이 라이브러리로 화면을 만듭니다. Open-World Memories의 `Modules/Lib/PopUI.lua`가 이 패턴으로, Bubblegum 테마와 자체 코인 위젯을 갖춘 SimpleUI입니다.
