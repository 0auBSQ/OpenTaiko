<!-- modules/custom-stages.md -->

# 커스텀 스테이지

커스텀 스테이지는 누구나 만들고 공유하고 설치할 수 있는 스테이지로, 스킨의 커스텀 스테이지 메뉴에 표시됩니다. [STAGES:SetNamespace](../api/activities.md)로 네임스페이스 `CStage`를 선언하고, 공유 문자열 `custom_stage_return`을 통해 자신을 연 스테이지로 돌아갑니다. `Stages.CustomStages`는 이 둘을 처리하고, 메뉴를 위해 커스텀 스테이지 목록을 만듭니다. `Global/Modules/Stages/custom_stage_tutorial`도 그중 하나로, 커스텀 스테이지를 만드는 방법을 게임 안에서 안내합니다.

## 선언하기

Script.lua 맨 위, 함수 밖에서:

```lua
-- Global/Modules/Stages/lantern_walk/Script.lua, at the top
local CS = require("Stages.CustomStages")

CS.declare{
    title       = CS.allLanguages("menu", "title"),       -- lang/<code>/menu.json, every language
    description = { en = "A calm walk down a street of paper lanterns." },
}
STAGES:SetResolution(1920, 1080)
```

그 옆의 `STAGES:SetResolution(1920, 1080)`은 스테이지가 그리는 해상도를 엔진에 알려, 해상도가 다른 스킨에서 잘리지 않고 크기가 조정되게 합니다.

`CS.declare(info)`는 `STAGES:SetNamespace("CStage", info)`입니다. 메뉴는 info의 두 필드를 읽습니다.

| 필드 | 표시 |
| --- | --- |
| `title` | 목록에 나오는 스테이지 이름. 문자열이거나 언어 코드별 테이블입니다. 없으면 폴더 이름입니다. |
| `description` | 스테이지를 선택한 동안 목록 아래에 나오는 한두 줄. 선택 사항입니다. |

언어 코드별 테이블은 게임 언어의 텍스트를, 없으면 영어를, 그것도 없으면 테이블에 있는 아무 언어의 텍스트를 줍니다. 다른 필드는 자유롭게 쓰세요. 메뉴는 info를 그대로 넘깁니다.

## 돌아가는 길

커스텀 스테이지를 여는 메뉴는 공유 문자열 `custom_stage_return`에 돌아갈 스테이지 `"<stage>"` 또는 `"<stage>|<transition>"`을 넣은 뒤 커스텀 스테이지로 이동합니다. 커스텀 스테이지는 들어올 때 이 문자열을 읽고 비운 뒤, 나갈 때 그 스테이지로, 문자열이 비어 있으면 `_title`로 갑니다.

```lua
local back, backTransition

function activate()
    back, backTransition = CS.takeReturn()   -- "_title" when the string is empty
end

function update(ts)
    if leaving then return Exit("stage", back, backTransition) end
end
```

문자열은 들어올 때마다 읽힙니다. 자신의 다른 스테이지로 갔다가 돌아오는 커스텀 스테이지는, 마이 룸이 `my_room_return`으로 하듯 나가기 전에 문자열을 다시 설정합니다.

## Stages.CustomStages

| 함수 | 설명 |
| --- | --- |
| `CS.declare(info)  -> boolean` | 호출한 스테이지를 커스텀 스테이지로 선언합니다: `STAGES:SetNamespace("CStage", info)`. |
| `CS.allLanguages(name, id)  -> table` | 호출한 스테이지의 `lang/<code>/<name>.json`에 있는 문자열 `id`를, 그것이 있는 아홉 언어 각각에서 모아 언어 코드별 테이블로 만듭니다. 언어가 정해지기 전에 실행되는 `declare`를 위한 것입니다. |
| `CS.localized(value, code?)  -> string or nil` | 선언된 텍스트를 `code`(기본값은 게임 언어)로 돌려줍니다. 문자열은 그대로, 언어 코드별 테이블은 위의 설명대로입니다. |
| `CS.list(code?)  -> table` | 호출한 모듈이 닿는 커스텀 스테이지를 제목순으로 돌려줍니다: `{ name, isGlobal, title, description, info }`, `title`과 `description`은 이미 그 언어입니다. |
| `CS.enter(name, back, transition?)` | `custom_stage_return`을 `back`으로 설정하고 스테이지 `name`으로 이동합니다. 스테이지의 update에서 `return CS.enter(...)`로 쓰세요. |
| `CS.takeReturn()  -> stage, transition` | `custom_stage_return`을 읽고 비웁니다: 스테이지(문자열이 비면 `_title`)와 전환 효과(기본값이면 nil). |
| `CS.open(o)`, `CS.update(ts)`, `CS.draw()`, `CS.isOpen()`, `CS.close()` | 호출자의 화면 위에 뜨는 대화 상자 형태의 목록으로, 스킨의 UI 라이브러리([UI.Pick](ui.md))로 만듭니다. `update`는 열려 있는 동안 nil, `"cancel"`, 또는 선택된 `CS.list` 항목을 돌려주며, 두 경우 모두 대화 상자는 닫힙니다. `o`: `title`, `ui`(라이브러리 테이블 또는 require 이름), `theme`, `sfx`, `navPlayer`(기본값 0: 북을 제외한 키보드와 패드). 설치된 커스텀 스테이지가 없으면 확인 버튼 위에 그렇게 표시합니다. |
| `CS.NAMESPACE`, `CS.RETURN_KEY` | `"CStage"`와 `"custom_stage_return"`. |

문자열은 자체 `lang/` 폴더에 아홉 언어로 있습니다([Locale.i18n](locale.md)). Open-World Memories는 타이틀 화면의 설정 위에 있는 커스텀 스테이지 항목에서 목록을 엽니다.

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

## 커스텀 스테이지의 위치

| 폴더 | 작동하는 스킨 |
| --- | --- |
| `Global/Modules/Stages/<name>/` | 모든 스킨. |
| `System/<skin>/Modules/Stages/<name>/` | 그 스킨만. |

같은 이름이 양쪽에 있으면, 스킨의 메뉴는 스킨의 스테이지를, 전역 메뉴는 전역 스테이지를 보여 줍니다. `Exit`가 열 스테이지입니다. 게임은 시작할 때와 스킨을 다시 불러올 때 스테이지를 만들므로, 게임 실행 중에 설치한 스테이지는 다시 시작한 뒤에 나타납니다.

## 공유하기

커스텀 스테이지는 그 폴더 자체입니다. 폴더 자체를 zip으로 묶어 압축 파일이 `<name>/Script.lua`로 시작하게 하고, 불러오는 모든 것과 에셋 크레딧을 넣으세요. 플레이어는 이를 게임 폴더의 `Global/Modules/Stages/`나 스킨의 `Modules/Stages/`에 압축 해제하고 게임을 다시 시작합니다. 업데이트는 파일을 추가하고 바꾸지만 지우지는 않으니, 기본 제공 스테이지가 쓰지 않는 폴더 이름을 고르세요.

## 튜토리얼

`custom_stage_tutorial`은 커스텀 스테이지를 만들고 공유하기 위해 알아야 할 네 가지를 게임 안에서 보여 줍니다. 폴더를 둘 곳(과 두지 말아야 할 곳), `CStage` 네임스페이스, 쓰는 모든 것을 자기 폴더에 두고 밖에서는 제공 모듈만 쓰기, 그리고 이 문서를 읽는 곳인 OpenTaiko Hub의 문서 탭입니다. 각 페이지는 `Textures/` 폴더의 이미지와 `lang/` 폴더의 텍스트라서 파일 하나씩 바꿀 수 있습니다.
