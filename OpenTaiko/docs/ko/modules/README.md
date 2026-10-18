<!-- modules/README.md -->

# 제공 모듈

OpenTaiko는 모든 스킨이 사용할 수 있는 Lua 모듈 라이브러리를 함께 제공합니다. 헬퍼(이징, 색, JSON, 입력, 현지화), UI 라이브러리, 대화 상자, 3D 월드 엔진, 그리고 미지의 탑, 비밀 금고, 코인 숍의 규칙과 데이터입니다. 이 모듈들은 스킨이 아니라 게임 폴더에 있으므로, 새 스킨은 아무것도 복사하지 않고 이를 사용할 수 있습니다. 이 절에서는 각 모듈을 설명합니다. 모듈의 바탕이 되는 엔진 전역은 [API 레퍼런스](../api/README.md)에서 다룹니다.

## Global/Modules

게임 설치 폴더의 `Global/Modules/`에는 스킨의 `Modules/`와 같은 하위 폴더 `Transitions/`, `Stages/`, `Activities/`, `ROActivities/`, `Lib/`가 있습니다. 게임은 어느 스킨이 활성화되어 있든 각 범주에서 스킨의 모듈 바로 다음에 전역 모듈을 로드합니다. 이 페이지들의 모듈은 `Global/Modules/Lib/`의 파일이며 `require`로 불러옵니다. `Global/Modules/Stages/`에는 [마이 룸](my-room.md)처럼 어느 스킨에서나 열 수 있는 스테이지도 있습니다.

이 폴더는 게임 빌드와 함께 배포되고 업데이트마다 교체되므로, 스킨은 이 폴더에 파일을 넣지 않아야 합니다. 제공 모듈을 바꾸려면 다른 이름으로 자신의 스킨에 복사하십시오.

## require 검색 순서

`require`는 점을 폴더로 바꾸고 대소문자를 구분하지 않으므로, `require("Core.Easing")`는 `Core/Easing.lua`를 로드합니다. 검색하는 폴더는 실행 중인 모듈에 따라 달라집니다.

| 호출하는 모듈 | 검색 순서 |
| --- | --- |
| 스킨 모듈(`System/<skin>/Modules/...`) | 자체 폴더, 스킨의 `Modules/Lib/`, 그다음 `Global/Modules/Lib/` |
| 전역 모듈(`Global/Modules/...`) | 자체 폴더, `Global/Modules/Lib/`, 그다음 스킨의 `Modules/Lib/` |

Lib 파일에는 자체 Lua 상태가 없습니다. 자신을 require한 모듈의 상태에서 실행되므로, 그 안의 require는 호출한 모듈의 순서를 따릅니다. 따라서 스킨의 `Modules/Lib/`에 제공 모듈과 경로가 같은 파일이 있으면, 다른 제공 모듈 내부의 require를 포함해 스킨 모듈에서 하는 모든 require에서 그 파일이 제공 모듈을 대신합니다. 모듈을 교체할 의도가 아니라면 자신의 Lib 파일을 아래 폴더 이름에 두지 마십시오.

## 이름 규칙

각 구성 요소는 `Global/Modules/Lib/`의 폴더이며, 그 모듈은 `<Component>.<Module>`로 require합니다.

| 구성 요소 | 모듈 | 페이지 |
| --- | --- | --- |
| `Core` | `Easing`, `Color`, `Util`, `Json`, `Here` | [Core](core.md) |
| `Input` | `NavInput` | [Input](input.md) |
| `Audio` | `MusicDuck` | [Audio](audio.md) |
| `Locale` | `i18n` | [Locale](locale.md) |
| `Calendar` | `almanac` | [Calendar](calendar.md) |
| `Songs` | `PlayStrings` | [Songs](songs.md) |
| `World` | `OWM3d`와 그 부품 | [World](world.md) |
| `UI` | `SimpleUI`, `Pick` | [UI](ui.md) |
| `Saves` | `SavePicker` | [Saves](saves.md) |
| `Characters` | `Student` | [Characters](characters.md) |
| `VN` | `Dialogue` | [VN](vn.md) |
| `Pagoda` | `Levels`, `Floor`, `Rewards`, `Progress`, `Songs`, `Run`, `Texts` | [Pagoda](pagoda.md) |
| `Vault` | `Keys`, `Chests`, `Texts` | [Vault](vault.md) |
| `Shop` | `Daily`, `Catalog`, `Texts`, `Seasonal`, `Popup` | [Shop](shop.md) |

## Open-World Memories가 유지하는 이름

기본 제공 스킨은 예전에 이 파일들 대부분을 자체 `Modules/Lib/`에 두었습니다. 이제 예전 파일은 각각 두 줄짜리 전달 파일이므로, 예전 이름을 아직 require하는 스킨 모듈은 전역 모듈을 받고, 두 이름은 하나의 인스턴스(하나의 상태, 하나의 캐시)를 공유합니다.

```lua
-- moved to Global/Modules/Lib/Core/Easing.lua
return require("Core.Easing")
```

| 예전 이름 | 제공 모듈 |
| --- | --- |
| `Easing`, `Color`, `Util` | `Core.Easing`, `Core.Color`, `Core.Util` |
| `NavInput`, `MusicDuck`, `PlayStrings` | `Input.NavInput`, `Audio.MusicDuck`, `Songs.PlayStrings` |
| `i18n`, `almanac` | `Locale.i18n`, `Calendar.almanac` |
| `OWM3d`, `OWM3d.<part>` | `World.OWM3d`, `World.OWM3d.<part>` |
| `PopUI.<part>`(공유 코드: `manager`, `widget`, `shape`, `util`, `cache`, `sfx`, `widgets.coins`를 제외한 모든 `widgets.*`) | `UI.SimpleUI.<part>` |
| `SavePicker`, `Student`, `dialogue` | `Saves.SavePicker`, `Characters.Student`, `VN.Dialogue` |

`PopUI` 자체는 스킨 모듈로 남습니다. 스킨 고유의 모양을 입힌 SimpleUI입니다([UI](ui.md) 참고). 새 코드는 제공 모듈의 이름을 require해야 합니다.

## 에셋은 호출한 모듈 기준으로 해석된다

`TEXTURE`, `SOUND`, `VIDEO`, `JSONLOADER`, `SQL`, `DATABASE`, `STORAGE`, `SHARED`에 전달하는 상대 경로는 호출한 모듈의 폴더를 기준으로 해석되며, 호출을 실행하는 Lib 파일이 기준이 되는 일은 없습니다. 자체 파일(그림, JSON 풀, 데이터베이스)을 함께 제공하는 제공 모듈은 파일 자신의 폴더를 반환하는 `Core.Here`로 그 파일을 찾고, 절대 경로 버전의 로더로 로드합니다.

```lua
-- Global/Modules/Lib/MyPart/Thing.lua
local HERE = require("Core.Here")(...)          -- ".../Global/Modules/Lib/MyPart/"

local icon = TEXTURE:CreateTextureFromAbsolutePath(HERE .. "icon.png")
local beep = SOUND:CreateSFXFromAbsolutePath(HERE .. "beep.ogg")
local data = JSONLOADER:JsonParseFileAny(HERE .. "data.json")
SHARED:SetSharedTextureUsingAbsolutePath("thing_icon", HERE .. "icon.png")
```

`...`는 파일의 최상위에서 전달하십시오. 그곳에서 `require`가 각 파일에 모듈 이름과 절대 경로를 넘겨줍니다. 호출한 쪽이 제공 모듈에 넘기는 상대 경로(`World.OWM3d`의 모델 파일, 위젯의 소리)는 호출한 쪽의 것이며, 평소처럼 호출한 쪽의 폴더를 기준으로 해석됩니다.

## 문자열은 모듈과 함께 둔다

`THEME:GetSkinString`은 활성 스킨의 `Locales/`만 읽으므로, 제공 모듈은 자신의 텍스트에 이를 쓰지 않습니다. 그 문자열은 모듈 옆의 `lang/` 폴더에 기능과 언어마다 JSON 파일 하나씩(`lang/en/vault.json`, `lang/ja/vault.json`, ...) 있으며, 모듈의 폴더를 지정해 [Locale.i18n](locale.md)으로 읽습니다.

```lua
local HERE = require("Core.Here")(...)
local T = require("Locale.i18n").texts("vault", HERE)   -- HERE .. "lang/<code>/vault.json"
local name = T:tr("chest_1")
```

영어(`lang/en/`)는 파일이나 키가 없는 언어의 폴백입니다. 전역 스테이지나 액티비티는 폴더 인자 없이 같은 일을 합니다. `texts(name)`은 스테이지 자체의 `lang/` 폴더를 읽습니다.

## 공유 상태

`SHARED`의 텍스처, 소리, 문자열은 스킨 모듈과 전역 모듈을 가리지 않고 게임 전체에서 하나의 이름 공간입니다. 제공 모듈은 몇 가지 고정 키를 사용하며, 각 페이지에 목록이 있습니다(예: 공유 문자열 `ui_library`, 텍스처 `myroom_thumb_<id>`). 충돌하지 않도록 자신의 키에는 접두사를 붙이십시오. 세이브 데이터(트리거, 카운터, 문자열)는 모듈이 아니라 세이브 파일에 속하며, 미지의 탑, 금고, 숍, 마이 룸은 이를 통해 서로 아이템을 주고받습니다.
