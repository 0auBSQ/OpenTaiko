<!-- getting-started.md -->

# 모듈의 동작 방식

OpenTaiko 0.6.1에서는 스킨이 Lua로 화면을 추가하고 교체할 수 있습니다. 스킨의 Modules 폴더에는 모듈마다 하나의 폴더가 있고, 각 모듈에는 정해진 전역 콜백 함수 집합을 정의하는 Script.lua가 있습니다. 게임은 각 Script.lua를 독립된 샌드박스 Lua 상태로 로드하고, 엔진 전역(TEXTURE, SOUND, INPUT, CONFIG 등 [API 레퍼런스](api/README.md)에 문서화된 것들)을 등록한 뒤, 적절한 시점에 콜백을 호출합니다. 정확한 시그니처는 [모듈과 생명 주기](api/activities.md)에 나열되어 있습니다.

## 시작하기 전에

- OpenTaiko 0.6.1과 Modules 디렉터리가 있는 스킨 폴더. 기본 제공 스킨은 System/Open-World Memories입니다.
- 텍스트 편집기와 기초적인 Lua 지식(함수, 테이블, require).
- System/Open-World Memories/Modules/Stages 아래의 기본 제공 스테이지. 작은 것(demo1, demo3)은 콜백의 형태를 보여 주고, 큰 것은 실제 화면이 어떻게 구성되는지 보여 줍니다.

## 모듈이 있는 위치

모듈 종류마다 Modules 아래에 자체 폴더가 있고, 각 모듈은 모듈 id를 이름으로 하는 폴더 하나입니다.

```
Modules/
  Stages/        <name>/Script.lua   전체 화면
  Activities/    <name>/Script.lua   스테이지가 구동하는 하위 화면
  ROActivities/  <name>/Script.lua   읽기 전용 하위 화면과 오버레이
  Transitions/   <name>/Script.lua   스테이지 사이의 페이드 (가장 먼저 로드됨)
  Lib/           require로 접근하는 공용 .lua 파일. 모듈로 스캔되지 않음
```

진입 파일은 항상 Script.lua입니다. TEXTURE, SOUND, VIDEO 및 다른 로더에 전달하는 에셋 경로는 모듈 폴더 기준의 상대 경로입니다. 기본 제공 모듈은 관례상 Textures, Sounds, Videos, Databases 하위 폴더에 에셋을 두고, 번역은 lang 폴더에 둡니다.

두 종류의 스크립트는 다른 곳에 있습니다.

- 배경(화면 배경, 게임플레이 레이어, 몹, 클리어 애니메이션, 쿠스다마)은 스킨의 Graphics 폴더 아래, 꾸미는 화면의 디렉터리에 있는 Script.lua 파일입니다. [모듈과 생명 주기](api/activities.md)의 배경 절을 참고하십시오.
- 캐릭터는 Global/Characters 아래의 폴더입니다. 캐릭터 폴더는 자체 Script.lua를 가질 수 있으며, 없으면 게임은 내장 캐릭터 스크립트를 사용합니다. [캐릭터 추가하기](guides/characters.md)를 참고하십시오.

## Script.lua는 전역 함수를 정의합니다

모듈의 Script.lua는 정해진 이름의 최상위 전역 함수를 정의하고, 게임은 각각을 전역으로 읽습니다. 게임은 로컬 테이블에 담아 반환한 함수를 찾지 못하며, 이름을 잘못 쓴 함수(onStart 대신 OnStart)도 호출하지 않습니다. 정의되지 않은 콜백을 no-op으로 처리하고 아무것도 보고하지 않기 때문입니다. 파일의 나머지 부분은 모두 로컬이어도 되고, require로 로드하는 여러 파일로 모듈을 나눌 수도 있습니다.

demo3가 복사해서 쓰기 좋은 최소 형태입니다.

```lua
-- Modules/Stages/mystage/Script.lua
local text = nil
local textTex = nil

function onStart()          -- 스킨이 로드될 때 한 번: 여기서 에셋을 로드
    text = TEXT:Create(16)
end

function activate()         -- 스테이지에 들어갈 때마다
    textTex = text:GetText("Hello")
end

function update(timestamp)  -- 매 프레임: 입력과 상태 변경
    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        return Exit("title", nil)
    end
end

function draw()             -- 매 프레임: 그리기만
    if textTex ~= nil then textTex:Draw(200, 200) end
end

function deactivate()       -- 스테이지를 떠날 때: 소리 정지, 데이터베이스 닫기
end

function onDestroy()        -- 스킨이 언로드되기 전: 생성한 것을 해제
    if textTex ~= nil then textTex:Dispose() end
end
```

## 스테이지 생명 주기

- onStart(): 게임은 스킨이 로드된 뒤 한 번, 그리고 스킨을 다시 로드할 때마다 스테이지가 화면에 있든 없든 이 함수를 호출합니다. 여기서 텍스처, 소리, 비디오를 로드하십시오. 코루틴으로 실행되므로 긴 로드는 LOADING 헬퍼로 로딩 바 뒤에서 여러 프레임에 걸쳐 나눌 수 있습니다.
- activate(): 게임은 스테이지에 들어갈 때마다 이 함수를 호출합니다. 방문마다 초기화할 상태를 재설정하고, 음악을 시작하고, 데이터베이스를 여는 곳입니다. 역시 코루틴으로 실행되며 LOADING을 사용할 수 있습니다. 게임은 캐릭터와 푸치캬라 목록(CHARACTERLIST, PUCHICHARALIST)을 activate 실행 직전에 갱신하므로 여기서 읽으십시오. onStart는 그 갱신보다 먼저 실행됩니다.
- update(timestamp): 게임은 매 프레임 draw 앞에서 이 함수를 호출하며 밀리초 단위 게임 시계를 전달합니다. 여기서 입력을 처리하고 상태를 바꾸십시오. 스테이지가 Exit를 호출한 뒤에는 게임이 update 호출을 멈추고 페이드아웃 동안 draw만 계속 호출합니다.
- draw(): 게임은 매 프레임 이 함수를 호출합니다. 그리기만 하고, 프레임당 할당을 적게 유지하십시오.
- deactivate(): 게임은 스테이지를 떠날 때 이 함수를 호출합니다. demo3는 여기서 데이터베이스를 해제하고, demo1은 음악과 비디오를 정지합니다.
- afterSongEnum(): 게임은 곡 열거가 끝날 때마다, 부팅 시와 소프트 또는 하드 리로드 후에, 스테이지가 활성 상태가 아니어도 이 함수를 호출합니다. 모듈이 곡 목록에 의존할 때 사용하십시오.
- onDestroy(): 게임은 스킨을 언로드하기 전에 이 함수를 호출합니다. demo1은 여기서 텍스처, 비디오, 텍스트 텍스처, 소리를 해제합니다.
- reloadLanguage(lang): 게임은 언어가 바뀔 때 이 함수를 호출합니다(아래 지역화 절 참고).

스킨이 로드되면 게임은 모듈을 종류별로, Transitions를 먼저, 그다음 Stages, Activities, ROActivities 순으로 생성합니다. 한 종류 안에서는 모든 Script.lua를 먼저 실행한 뒤에 onStart를 호출합니다. 따라서 스테이지의 onStart가 실행되는 동안에는 액티비티와 ROActivity가 아직 존재하지 않으므로, activate에서 찾으십시오.

다른 종류는 이 집합의 변형을 사용합니다. 액티비티와 ROActivity는 같은 콜백을 가지지만, activate, deactivate, draw, update는 호스트 스테이지가 호출하고 그 반환값도 호스트 스테이지가 받습니다. 배경은 activate(state), update(timestamp, state), draw(state)로 상태 객체를 받으며 clearIn, playEndAnime, kusuBroke 같은 이벤트 훅을 정의할 수 있습니다. 트랜지션은 fadeOut(t), loading(progress, elapsed), fadeIn(t)를 정의합니다. 캐릭터는 자체 애니메이션과 음성 집합을 정의합니다. [모듈과 생명 주기](api/activities.md)에 모두 나열되어 있습니다.

## 모듈 종류 고르기

- 스테이지(Modules/Stages): 게임이 전환해 들어가는 전체 화면. 프레임을 소유하고, 입력을 처리하며, Exit를 호출해 떠납니다. 독립된 화면이 되는 모든 것에 사용하십시오.
- 액티비티(Modules/Activities): 대화 상자처럼 스테이지가 안에서 사용하는 하위 화면. ACTIVITY:GetActivity(name)로 찾는 싱글턴이며, 호스트 스테이지가 그 Activate, Update, Draw, Deactivate를 호출합니다. 게임 상태를 쓸 수 있는 공용 요소에 사용하십시오.
- ROActivity(Modules/ROActivities): 액티비티의 읽기 전용 형태로, ROACTIVITY:GetROActivity(name)로 찾습니다. 읽기 전용 CONFIG, DATABASE, GetSaveFile을 받고 ACTIVITY 전역이 없습니다. 상태를 읽기만 하는 요소에 사용하십시오. 재사용 가능한 UI 대부분이 여기에 해당합니다. 엔진은 자체 오버레이 몇 가지를 정해진 이름의 ROActivity로 호스팅합니다(nameplate, modal, modicons, danplate, popup_menu, config_ui, song_enum). 스킨은 같은 이름의 폴더를 제공해 이를 교체하되, 엔진이 호출하는 콜백은 유지해야 합니다.
- 배경: Graphics 아래에 있으며 엔진 화면 중 하나의 뒤나 위에 그리는 Script.lua. 배경은 ROActivity와 같은 읽기 전용 전역을 받습니다.
- 트랜지션(Modules/Transitions): 게임이 스테이지 사이에 재생하는 페이드아웃, 로딩, 페이드인. 스테이지는 Exit의 세 번째 인자에서 이름으로 하나를 고릅니다. 스테이지가 이름을 지정하지 않았거나 이름이 존재하지 않으면 게임은 default라는 이름의 것으로 대체하고, 게임플레이에 들어갈 때는 song_loading이라는 이름의 것을 재생합니다.
- 캐릭터: [캐릭터 추가하기](guides/characters.md)를 참고하십시오.

## Exit로 스테이지 떠나기

Exit 전역은 스테이지에만 있습니다. 최대 세 개의 인자를 받고 어느 위치에서든 nil을 허용합니다. 대상("title", "play", "stage", "legacy"; nil은 "title"을 뜻함), 대상이 "stage"일 때의 목적지 스테이지 이름(또는 "legacy"일 때의 레거시 키), 트랜지션 모듈 이름입니다. 기본 제공 스테이지는 그 프레임에서 다른 것이 실행되지 않도록 update 안에서 `return Exit(...)`를 씁니다.

```lua
-- demo1/Script.lua의 update() 안에서
if INPUT:KeyboardPressed("S") == true then
    sounds.Skip:Play()
    return Exit("stage", "demo2")   -- Modules/Stages/demo2로 이동
end
-- ...
return Exit("title", nil)           -- 타이틀 화면으로 돌아가기
```

## 샌드박스

모든 Script.lua는 제한된 Lua 상태에서 실행됩니다.

- os에는 time, date, difftime만 남습니다. 샌드박스는 io, debug, loadfile, dofile을 제거하며, import는 아무것도 하지 않습니다.
- package는 커스텀 로더로 축소됩니다. package.path와 package.cpath는 비어 있고 샌드박스가 표준 searcher를 교체하므로, 아래 경로만 검색할 수 있습니다.
- require는 먼저 모듈 자체 폴더를, 그다음 스킨의 Modules/Lib 폴더를 찾아 처음 발견한 파일을 로드합니다. 같은 이름의 모듈 파일과 Lib 파일이 있으면 모듈 파일로 해석됩니다. 이름의 점은 경로 구분자가 되므로 require("DBControllers.dbScores")와 require("DBControllers/dbScores")는 둘 다 DBControllers/dbScores.lua를 로드합니다. 비ASCII 경로도 동작합니다.

```lua
-- intro_nokon/Script.lua에서 발췌
local DBScores  = require("DBControllers/dbScores")  -- 모듈 자체의 하위 폴더
local I18N      = require("i18n")                     -- Modules/Lib/i18n.lua
local Opening   = require("opening")                  -- 모듈 폴더
local Dialogue  = require("nokon_dialogue")           -- 모듈 폴더
```

## 읽기 전용 모듈

게임은 ROActivity와 배경을 Script.lua가 실행되기 전에 제한된 전역으로 생성합니다. CONFIG는 읽기 전용 뷰이고, GetSaveFile(player)는 읽기 전용 세이브 파일을 반환하며, DATABASE는 읽기 전용 저장소를 열고, ACTIVITY는 nil입니다(ROACTIVITY를 사용). 이들을 통한 쓰기는 오류 알림을 기록하고, 아무것도 하지 않으며, Lua 오류를 발생시키지 않습니다. 설정, 세이브 데이터, 데이터베이스를 바꿔야 하는 모듈은 액티비티나 스테이지여야 합니다.

## lang/을 이용한 지역화

기본 제공 스킨은 공유 라이브러리 Modules/Lib/i18n.lua로 각 모듈 자체의 문자열을 번역합니다. 코드의 영어 문자열이 키입니다. 모듈은 각 영어 문자열을 일본어 번역에 대응시키는 테이블을 반환하는 lang/ja.lua를 함께 제공하고, 라이브러리는 그 테이블에서 문자열을 찾습니다.

라이브러리에는 세 가지 함수가 있습니다.

- detect()는 전역 LANG을 통해 현재 게임 언어를 읽고 그 언어의 사전을 로드합니다. 언어가 일본어이면 lang/ja를 require하며, 이는 모듈 폴더 안에서 해석되므로 모듈마다 자기 사전을 가집니다. 그 밖의 언어에서는 아무것도 로드하지 않습니다. 이 함수를 호출하기 전에는 사전이 로드되지 않아 모든 문자열이 영어로 남습니다.
- tr(s)는 로드된 사전에서 s의 번역을 반환하고, 사전에 항목이 없거나 사전이 로드되지 않았으면 s를 그대로 반환합니다.
- trf(fmt, ...)는 서식 문자열 fmt를 같은 방식으로 번역한 뒤 string.format으로 서식을 적용합니다.

activate에서 detect()를 호출한 뒤 tr과 trf로 텍스트를 만드십시오. activate는 모듈에 들어갈 때마다 실행되므로, 플레이어가 설정에서 바꾼 언어는 다음 방문에 적용되고 모듈에는 다른 훅이 필요 없습니다. 키는 구두점, 공백, 줄바꿈까지 영어 원문과 정확히 일치해야 하고, 번역은 %s나 {Player 1 name} 같은 자리 표시자를 그대로 유지해야 합니다.

```lua
-- Modules/Stages/mystage/lang/ja.lua
local T = {}
T["Nokon"] = "ノコン"
T["Alright, quiz time!"] = "さあ、クイズの時間である！"
return T
```

```lua
-- Modules/Stages/mystage/Script.lua
local I18N = require("i18n")
local title

function activate()
    I18N.detect()
    title = I18N.tr("Alright, quiz time!")
end
```

게임은 언어가 바뀔 때 로드된 모든 모듈에서 전역 reloadLanguage(lang)도 호출합니다. 언어 선택기가 있는 화면처럼 언어가 바뀌는 동안 화면에 남아 있는 모듈만 이 함수가 필요합니다. 거기서는 detect()를 다시 호출하고 미리 렌더링한 텍스트를 다시 만드십시오.

## 주의할 점

- 게임은 모듈이 생성한 텍스처, 소리, 비디오, 텍스트 객체를 모듈을 해제할 때 함께 해제하므로, 스킨 리로드로 누수가 생기지는 않습니다. 데이터베이스처럼 방문마다 여는 리소스는 demo3처럼 deactivate에서 해제하고, 생성한 것은 demo1처럼 onDestroy에서 해제하십시오.
- GetText는 텍스트 객체에 서로 다른 문자열마다 텍스처 하나를 캐시합니다. 매 프레임 바뀌는 문자열은 매 프레임 텍스처를 추가하고 게임은 점점 느려집니다. 변하는 값은 글리프 렌더러(TEXT:CreateGlyphCached)로 그리거나, 값이 바뀔 때까지 텍스처 하나를 유지하십시오.
- LOADING은 코루틴으로 실행되는 콜백에서만 동작합니다. 모든 모듈의 onStart와 스테이지의 activate입니다. 액티비티의 activate나 update, draw에서 LOADING:Tick을 호출하면 Lua 오류가 발생합니다.
- onStart와 afterSongEnum은 모듈이 화면에 없는 동안 실행됩니다. 스테이지가 보이지 않아도 동작하도록 작성하십시오.
