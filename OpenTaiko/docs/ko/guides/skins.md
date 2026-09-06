<!-- guides/skins.md -->

# 스킨과 테마 추가하기

스킨은 게임의 `System/` 디렉터리 아래에 있는 폴더입니다. 그래픽, 소리, 폰트, 레이아웃 값, 로케일 파일, 그리고 모든 화면을 그리는 Lua 모듈을 제공합니다. 이 가이드는 무엇이 폴더를 스킨으로 만드는지, `SkinConfig.ini` 키, 폴더 구조, Lua 모듈 트리와 그 생명 주기, 그리고 스킨을 설치하고 선택하는 방법을 설명합니다. Lua API 자체(그리기, 소리, 입력 등)는 API 레퍼런스가 다룹니다.

스킨은 각 화면을 실행하는 Lua 모듈도 담고 있습니다. 게임은 특정 모듈을 이름으로 로드하고 특정 스테이지로 이동하므로, 처음부터 만든 스킨은 스킨 선택기에 나오지만 게임이 실행할 수 없습니다. 기본 제공 스킨의 복사본에서 시작하십시오.

## 시작하기 전에

- OpenTaiko 0.6.1 설치, 기본 제공 스킨 `System/Open-World Memories/` 존재.
- `SkinConfig.ini`, 포함되는 `*Config.ini` 파일, Lua 모듈을 위한 일반 텍스트 편집기.
- 화면 동작을 바꾸려면 기초적인 Lua 지식. 순수한 리텍스처(PNG와 OGG 파일 교체, `.ini` 값 편집)에는 Lua가 필요 없습니다.

## 1단계: 무엇이 폴더를 스킨으로 만드는지 이해하기

시작 시 게임은 `System/`의 하위 폴더를 나열합니다. 폴더 안에 `Graphics/1_Title/Background.png`가 있을 때만 스킨으로 셉니다. 다른 폴더는 게임이 건너뜁니다. 선택된 스킨 폴더가 없으면 게임은 `System/Default/`로, 그다음 알파벳 순으로 첫 유효한 스킨으로, 그다음 `System/` 자체로 대체합니다.

이 검사는 폴더를 목록에 올릴 뿐입니다. 스킨이 실행되기 위해 함께 있어야 하는 모듈은 8단계에서 명시합니다.

```
System/
  Open-World Memories/         <- 기본 제공 스킨
  My New Skin/                 <- 새 스킨
    Graphics/
      1_Title/
        Background.png          <- 폴더가 목록에 오르기 위해 필수
    SkinConfig.ini
```

## 2단계: 기본 제공 스킨 복사하기

`System/Open-World Memories/`를 새 형제 폴더(예: `System/My New Skin/`)로 복사하십시오. 복사본에는 게임에 필요한 모든 것이 들어 있습니다: `Graphics/`, `Sounds/`, `Fonts/`, `Locales/`, `Modules/`, `ThemeSettings.json`, `SkinConfig.ini`와 그것이 포함하는 `*Config.ini` 파일. 폴더 이름이 스킨의 식별자이므로(게임이 이를 선택된 스킨으로 기록하고 스킨 선택기가 이를 표시함) 파일 시스템에 안전하게 유지하십시오. 그다음 메타데이터가 스킨을 설명하도록 `SkinConfig.ini`를 편집하십시오.

## 3단계: SkinConfig.ini 편집하기

`SkinConfig.ini`는 한 줄에 설정 하나인 `Key=Value` 파일입니다. 파서는 앞의 공백과 탭을 제거하고, `;`로 시작하는 줄을 주석으로 취급하며, 줄에 `=`가 정확히 하나 있을 때만 읽습니다. 키 대조는 정확히 이루어지며, 파서는 알 수 없는 키를 오류 보고 없이 무시합니다. 스킨 수준 키는 다음과 같습니다.

- `Name=`: 표시 이름. 메타데이터일 뿐이며, 스킨은 폴더 이름이 선택합니다.
- `Version=`, `Creator=`: 자유 형식 문자열(기본값 `Unknown`). 게임은 이를 검증하지 않습니다.
- `DefaultLocale=`: 활성 게임 언어에 `Locales/` 아래 파일이 없을 때 게임이 사용하는 로케일 id(기본값 `en`).
- `Resolution=W,H`: 레이아웃 값을 작성하는 기준 해상도(기본값 `1280,720`). 기본 제공 스킨은 `1920,1080`을 사용합니다.
- `Resolutions=`: 선택 가능한 렌더 스케일 배율(4단계).
- `AIBattleCharacter=`: AI 상대에 사용하는 캐릭터 폴더(5단계).
- `FontName<LANG>=`과 `BoxFontName<LANG>=`: 게임 언어별 폰트 파일. `<LANG>`은 대문자 언어 코드(`EN`, `JA`, `FR`, `ES`, `NL`, `DE`, `RU`, `KO`, `ZH`)입니다. 경로는 스킨 루트 기준 상대 경로이며(절대 경로도 가능) 파일이 존재해야 합니다. 없으면 파서가 키를 버립니다.

다른 모든 키(`Game_*`, `Result_*`, `Title_*` 등)는 화면 레이아웃 값입니다. 같은 파서가 읽으므로 6단계의 포함 파일에 둘 수 있습니다.

```ini
;스킨 정보
Name=My New Skin
DefaultLocale=en
Version=1.0.0
Creator=Your Name
Resolution=1920,1080
;선택 가능한 렌더 스케일 배율 (<=1; 소수 또는 a/b 분수, 쉼표 구분). 1은 항상 사용 가능하며 기본값입니다.
Resolutions=1,2/3,1/3
;AI 배틀 슬롯에 사용하는 캐릭터 폴더.
AIBattleCharacter=10v2 - AItritus
FontNameEN=Fonts/MPLUSRounded1c-Medium.ttf
FontNameJA=Fonts/MPLUSRounded1c-Medium.ttf
BoxFontNameEN=Fonts/MPLUSRounded1c-Regular.ttf
BoxFontNameJA=Fonts/MPLUSRounded1c-Regular.ttf
```

## 4단계: Resolutions 옵션

`Resolutions=`는 설정 메뉴가 제공하는 렌더 스케일 배율의 쉼표 구분 목록입니다. 게임은 `Resolution` 곱하기 선택한 배율로 렌더링하고 결과를 창에 맞게 확대합니다. 창 크기는 바뀌지 않습니다. 각 토큰은 소수(`0.5`) 또는 분수(`2/3`)입니다. 파서는 0 < 값 <= 1 범위 밖의 토큰, 파싱할 수 없는 토큰, 중복을 버리고, `1`이 없으면 추가하며, 목록을 `1`이 첫 번째가 되도록 정렬합니다. 세미콜론은 주석 줄을 시작하므로 구분자는 쉼표여야 합니다. 설정 메뉴는 각 항목을 픽셀 크기와 함께 표시합니다. 예를 들어 1920x1080 스킨에서는 `2/3 (1280x720)`입니다.

```ini
Resolution=1920,1080
Resolutions=1,2/3,1/3
; 다음 옵션이 만들어집니다:
;   1     -> 1920x1080  (기본값)
;   2/3   -> 1280x720
;   1/3   -> 640x360
```

## 5단계: AIBattleCharacter 옵션

`AIBattleCharacter=`는 AI 배틀 모드의 AI 상대에 게임이 사용하는 `Global/Characters/` 아래 폴더를 지정합니다. 기본값은 `10v2 - AItritus`입니다. 지정한 폴더가 존재해야 합니다.

```ini
;AI 배틀 슬롯에 사용하는 캐릭터 폴더.
AIBattleCharacter=10v2 - AItritus
```

## 6단계: #include로 설정 나누기

파서는 `#include SomeFile.ini` 형태의 줄을 만나면 그 파일을 그 자리에서 재귀적으로 읽습니다. 경로는 스킨 루트 기준 상대 경로입니다. 기본 제공 `SkinConfig.ini`는 메타데이터와 폰트 키만 가지고 그다음 화면마다 파일 하나를 포함합니다. 스킨을 복사할 때 이 줄들을 유지하고, 화면을 다시 조정하려면 개별 `*Config.ini` 파일을 편집하십시오.

```
; SkinConfig.ini의 끝부분 (기본 제공 스킨, 순서대로)
#include OtherConfig.ini
#include TitleConfig.ini
#include ConfigConfig.ini
#include SongSelectConfig.ini
#include HeyaConfig.ini
#include SongLoadingConfig.ini
#include GameConfig.ini
#include ModIconsConfig.ini
#include NameplateConfig.ini
#include AIResultConfig.ini
#include ResultConfig.ini
#include DaniSelectConfig.ini
#include DanResultConfig.ini
#include TowerResultConfig.ini
#include TowerSelectConfig.ini
#include OnlineLoungeConfig.ini
#include OpenEncyclopediaConfig.ini
#include ModalConfig.ini
#include Game4PConfig.ini
#include Result4PConfig.ini
#include Modal4PConfig.ini
```

## 7단계: 스킨 폴더 구조 익히기

기본 제공 스킨을 기준으로, 스킨 루트에는 다음이 있습니다.

- `Graphics/`: 번호가 매겨진 화면별 폴더(`0_Startup`, `1_Title`, `2_Config`, `3_DaniSelect`, `5_Game`, `6_Result`, `7_DanResult`, `7_Exit`, `8_TowerResult`, `10_Heya`, `12_OnlineLounge`, `13_TowerSelect`, `15_OpenEncyclopedia`)로 묶인 이미지와 상단의 몇몇 공용 이미지. 애니메이션 배경은 속한 폴더의 이미지 옆에 놓인 `Script.lua` 파일입니다(예: `Graphics/0_Startup/Script.lua`와 `Graphics/5_Game/5_Background/` 아래 폴더).
- `Sounds/`: 게임이 고정 파일 이름으로 로드하는 시스템 소리와 BGM. 예: `Sounds/Move.ogg`, `Sounds/Decide.ogg`, `Sounds/Cancel.ogg`, `Sounds/BGM/Title.ogg`, `Sounds/BGM/SongSelect.ogg`, `Sounds/BGM/Result.ogg`. 파일이 없으면 그 소리는 재생되지 않습니다.
- `Fonts/`: `FontName` 키가 참조하는 `.ttf` 파일.
- `Locales/`: 언어별 JSON 파일(`en.json`, `ja.json`, ...) 하나씩, 형태는 `{ "Entries": { "KEY": "text" } }`. 이 문자열은 스킨 자체 설정의 라벨이며 Lua는 `THEME:GetSkinString(key)`로 이를 읽습니다. 활성 언어에 키가 없으면 게임은 `DefaultLocale` 파일에서 찾습니다.
- `Modules/`: Lua 모듈 트리(8단계).
- `ThemeSettings.json`: 옵션 화면이 테마 설정 아래에 표시하는 설정 배열. 각 항목은 `id`, `type`(`bool`, `int`, `double`, `string`, `enum`), `scope`(기본값 `global`, 또는 세이브 파일마다 값 하나인 `save`), 지역화된 `label`과 `description`, `default`, 그리고 타입에 따라 `min`/`max` 또는 `options`를 가집니다.
- `SkinConfig.ini`와 포함되는 `*Config.ini` 파일.
- `README.txt`, `LICENSE.md`, `Licenses/`: 저작자 표시 파일. 게임은 읽지 않습니다.

```
My New Skin/
  SkinConfig.ini
  ThemeSettings.json
  Graphics/           화면별 이미지. 일부 폴더는 배경 Script.lua를 가짐
  Sounds/             고정 이름 .ogg 시스템 소리와 BGM/
  Fonts/              FontName 키가 지정하는 .ttf 파일
  Locales/            en.json, ja.json, ... ({ "Entries": { ... } })
  Modules/            Lua 모듈 트리 (8단계)
  <screen>Config.ini  #include로 끌어오는 레이아웃 파일
```

## 8단계: Modules 트리와 게임이 요구하는 모듈

스킨이 로드되면 게임은 `Modules/`의 네 하위 폴더를 스캔하고 그 안의 모든 직계 하위 폴더를 진입 파일이 `Script.lua`인 모듈 하나로 취급합니다.

- `Modules/Transitions/`: 스테이지 사이에 재생되는 트랜지션. 게임은 첫 스테이지 전환에 대비해 이를 가장 먼저 로드합니다.
- `Modules/Stages/`: 전체 화면. 스테이지에는 `Exit("stage", "<folder name>")`로 들어갑니다.
- `Modules/Activities/`: 스테이지 위에 겹치는 재사용 가능한 하위 화면(예: `confirm_dialog`, `mod_select_dialog`, `song_select_core`).
- `Modules/ROActivities/`: 게임이 직접 구동하는 읽기 전용 오버레이.

게임은 `Modules/Lib/`를 스캔하지 않습니다. 그곳의 파일은 `require`로 로드합니다. 모듈의 검색 경로는 자체 폴더 다음에 `Modules/Lib/`이므로 `require("dialogue")`는 `Modules/Lib/dialogue.lua`로 해석됩니다. 스테이지와 액티비티는 게임 설치 폴더의 `Global/Stages/`와 `Global/Activities/`에 둘 수도 있으며, 게임은 그것들을 모든 스킨에 대해 로드합니다.

각 범주 안에서 게임은 모든 모듈을 먼저 만든 뒤 각각에 `onStart`를 실행합니다. 순서는 Transitions, Stages, Activities, ROActivities입니다.

게임은 이 모듈들을 이름으로 찾으며, 기본 제공 스킨은 모두 제공합니다.

- 스테이지 `_boot`와 `_title`. 둘 중 하나라도 없으면 게임은 오류로 멈춥니다.
- ROActivity `modal`, `config_ui`, `nameplate`, `popup_menu`, `modicons`, `song_enum`, `danplate`.
- 트랜지션 `default`와 `song_loading`. `song_loading`은 게임이 곡을 로드하는 동안 재생되고, `default`는 `Exit`가 트랜지션을 지정하지 않았거나 존재하지 않는 것을 지정했을 때 게임이 사용합니다. 트랜지션 모듈이 전혀 없는 스킨은 단순한 검은 페이드로 대체됩니다.

스킨을 만들 때 이 모듈들을 모두 유지하고, 자신의 모듈을 그 옆에 추가하십시오.

```
Modules/
  Transitions/   <name>/Script.lua   (가장 먼저 로드됨. 게임이 "default"와 "song_loading"을 사용)
  Stages/        <name>/Script.lua   ("_boot"와 "_title" 필수)
  Activities/    <name>/Script.lua
  ROActivities/  <name>/Script.lua   (modal, config_ui, nameplate, popup_menu, modicons, song_enum, danplate 필수)
  Lib/           require로 접근하는 공용 .lua 파일. 스캔되지 않음
```

## 9단계: 스테이지의 Script.lua와 생명 주기

`Script.lua`는 게임이 모듈을 생성할 때 엔진 전역(`TEXTURE`, `SOUND`, `INPUT`, `CONFIG`, `THEME` 등)이 이미 정의된 상태로 한 번 실행됩니다. 그다음 게임은 전역 함수를 이름으로 찾아 호출합니다. 스테이지의 경우:

- `onStart()`: 스킨이 로드될 때 한 번. 코루틴으로 실행되므로 무거운 로드는 `coroutine.yield()`나 `LOADING` 헬퍼를 호출해 로딩 바 뒤에서 작업을 여러 프레임에 나눌 수 있습니다.
- `activate()`: 게임이 스테이지에 들어갈 때마다. 역시 코루틴입니다. 게임은 `CHARACTERLIST`와 `PUCHICHARALIST` 전역을 실행 직전에 갱신하므로, 이에 의존하는 것은 여기서 만드십시오. `onStart`에서는 아직 비어 있습니다.
- `update(timestamp)`: 매 프레임. 스테이지를 떠나려면 `Exit(target, name, transition)`을 반환하십시오. `target`은 `"title"`, `"play"`, `"stage"`(`name` = 스테이지 폴더), `"legacy"`(`name` = `heya`, `config`, `exit`, `onlinelounge`)입니다. `transition`은 `Modules/Transitions/` 아래 폴더이며 기본값은 `default`입니다.
- `draw()`: 매 프레임.
- `deactivate()`: 게임이 스테이지를 떠날 때.
- `afterSongEnum()`: 곡 목록 열거가 끝났을 때.
- `onDestroy()`: 게임이 스킨을 해체할 때.

모두 선택 사항입니다. 정의하지 않은 함수는 게임이 건너뜁니다. 액티비티, ROActivity, 트랜지션은 자체 훅 집합으로 같은 패턴을 따릅니다.

```lua
-- Modules/Stages/my_stage/Script.lua
function onStart()
  -- 일회성 설정. 무거운 로드 중에는 coroutine.yield()를 호출할 수 있음
end

function activate()
  -- 스테이지에 들어갈 때마다 실행
end

function update(ts)
  if INPUT:Pressed("Cancel") then
    return Exit("stage", "_title")   -- 이 스테이지를 떠남
  end
  return nil
end

function draw()
  -- 프레임별 렌더링
end

function deactivate() end
function afterSongEnum() end
function onDestroy() end
```

## 10단계: lang/으로 모듈 지역화하기

모듈은 `Script.lua` 옆의 `lang/` 하위 폴더에 자체 번역을 둘 수 있습니다. 모듈 자체 폴더가 `require` 경로에 있으므로 `require("lang.ja")`는 `lang/ja.lua`로 해석됩니다. 기본 제공 스킨은 헬퍼 `Modules/Lib/i18n.lua`를 통해 큰 스테이지에서 이렇게 합니다(예: `Modules/Stages/myroom/lang/ja.lua`와 `Modules/Stages/intro_nokon/lang/ja.lua`). 이는 7단계의 스킨 전체 `Locales/` 폴더와는 별개입니다.

```
Modules/Stages/my_stage/
  Script.lua
  lang/
    ja.lua        -- require("lang.ja")
```

## 11단계: 스킨 설치와 선택

폴더를 `System/` 아래에 두십시오. 설정을 열고 외형 섹션으로 가서 스킨 옵션에서 스킨을 고르십시오. 선택기는 모든 유효한 스킨을 폴더 이름으로 나열하고 `Graphics/1_Title/Background.png`를 썸네일로 보여 줍니다. 스킨을 바꾸면 게임은 현재 스킨을 해체하고, 새 스킨을 로드하며, 로딩 바 뒤에서 모든 Lua 모듈을 다시 로드합니다.

게임은 선택을 `Config.ini`에 `System/` 기준 상대 경로로 `SkinPath=`로 기록합니다. 폴더 이름만 쓰며(예: Windows에서 `SkinPath=My New Skin\`), 파일의 주석에 보이는 `./My New Skin/` 형태도 허용합니다.

```ini
; Config.ini에서 (게임 내에서 스킨을 고를 때 기록됨):
; System/ 기준 스킨 폴더 경로
SkinPath=My New Skin\
```

## 문제 해결과 참고

- 선택기에 스킨이 나오지 않음: `Graphics/1_Title/Background.png`가 없거나 폴더가 `System/` 바로 아래에 있지 않습니다.
- 스킨을 선택한 직후 게임이 오류를 냄: 필수 모듈이 없거나(8단계) 그중 하나가 Lua 오류를 발생시켰습니다. 스킨은 전환해서 테스트하십시오.
- `SkinConfig.ini` 키가 효과가 없음: 키 철자가 틀렸거나, 줄에 `=`가 둘 이상 있거나, 값 파싱에 실패했습니다. 파서는 알 수 없는 키를 보고하지 않고 무시합니다.
- `Resolutions=`에 `1`만 표시됨: 목록에 세미콜론(주석 표시)을 썼거나 모든 값이 0 < 값 <= 1 밖이었습니다.
- 폰트 키가 효과가 없음: 파일 경로가 스킨 루트 기준으로 존재하지 않습니다.
- `onStart`에서 `CHARACTERLIST`나 `PUCHICHARALIST`가 비어 있음: 게임은 모듈을 생성한 뒤에 이를 채웁니다. `activate`에서 사용하십시오.
- 스킨 폴더 이름을 바꾸면 식별자가 바뀝니다. `Config.ini`의 `SkinPath`가 새 이름을 가리켜야 합니다.
- `Name=`, `Version=`, `Creator=`는 정보용일 뿐입니다. 게임은 이들에 대해 호환성 검사를 하지 않습니다.
