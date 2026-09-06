<!-- api/activities.md -->

# 모듈과 생명 주기

엔진은 모듈의 Script.lua에서 정해진 콜백 집합을 호출합니다. 이 페이지는 그 콜백과 함께 액티비티·배경·트랜지션을 구동하는 전역, 그리고 모든 모듈이 받는 카운터, 카메라, 진단 헬퍼를 나열합니다. 모듈을 작성해 본 적이 없다면 먼저 [모듈의 동작 방식](../getting-started.md)을 읽으십시오.

## 생명 주기 콜백

엔진은 각 모듈의 Script.lua에서 최상위 전역 함수를 이름으로 찾아 정해진 시점에 호출합니다. 정의하지 않은 콜백은 엔진이 건너뜁니다. 엔진이 어떤 콜백을 호출하는지는 모듈 종류가 결정합니다.

<div class="callout warn">
스킨이 로드될 때 엔진은 모듈을 Transitions, Stages, Activities, ROActivities 순서로 생성하고 시작합니다. 각 종류 안에서 엔진은 모든 모듈의 Script.lua(최상위 코드)를 먼저 실행한 뒤 각각에 onStart를 호출합니다. 따라서 스테이지의 onStart가 실행되는 동안 액티비티와 ROActivity는 아직 로드되지 않았고 거기서 찾으면 nil이 반환되므로, activate에서 찾으십시오. 스킨 변경이나 종료 시에는 onDestroy가 Stages, 그다음 ROActivities와 Activities, 그다음 Transitions 순으로 실행됩니다.
</div>

### 스테이지, 액티비티, ROActivity

| 메서드 | 설명 |
| --- | --- |
| `onStart()` | 엔진이 모듈을 생성한 뒤 한 번 호출됩니다. 부팅 시와 엔진이 스킨을 로드하거나 다시 로드할 때마다 일어납니다. 코루틴으로 실행되므로(아래 LOADING 참고) 여기서 에셋을 로드하십시오. |
| `activate(...)` | 스테이지: 엔진이 스테이지에 들어갈 때마다 코루틴으로 호출됩니다. 액티비티/ROActivity: 호스트가 자신의 인자와 함께 `handle:Activate(...)`로 호출하며, 반환값은 호스트로 돌아갑니다. 엔진은 CHARACTERLIST와 PUCHICHARALIST 전역을 실행 직전에 갱신합니다. |
| `update(timestamp)` | 매 프레임 draw 앞에서 호출됩니다. timestamp는 밀리초 단위 게임 시계입니다. 스테이지는 Exit를 호출한 뒤 update를 더 받지 않으며, draw는 페이드아웃 동안 계속 실행됩니다. 액티비티/ROActivity: 호스트가 `handle:Update()`를 통해 호출합니다. |
| `draw(...)` | 매 프레임 호출됩니다. 액티비티/ROActivity: 호스트가 자신의 인자와 함께 `handle:Draw(...)`를 통해 호출하며, 반환값은 호스트로 돌아갑니다. |
| `deactivate(...)` | 스테이지: 엔진이 스테이지를 떠날 때 호출됩니다. 액티비티/ROActivity: 호스트가 `handle:Deactivate(...)`를 통해 호출하거나 모듈이 스스로 `DEACTIVATE()`로 호출하며, 반환값은 호스트로 돌아갑니다. |
| `afterSongEnum()` | 곡 열거가 끝날 때마다 호출됩니다. 부팅 시와 곡의 소프트 또는 하드 리로드 후를 포함하며, 모듈이 활성 상태가 아니어도 호출됩니다. |
| `onDestroy()` | 엔진이 스킨을 언로드하기 전에 호출되어 모듈이 보유한 것을 해제할 수 있게 합니다. |
| `reloadLanguage(lang)` | 게임 언어가 바뀔 때 로드된 모든 모듈에서 호출됩니다. lang은 새 언어 코드입니다. |

### 배경

각 화면은 자체 배경을 호스팅합니다. 아래 배경 절에서 state 인자와 이벤트 훅을 설명합니다.

| 메서드 | 설명 |
| --- | --- |
| `onStart()` | 호스트가 배경을 처음 활성화할 때 한 번, 동기적으로 호출됩니다. |
| `activate(state)` | 호스트가 배경을 활성화할 때마다 호출됩니다. 재활성화 시 onStart가 다시 실행되지는 않습니다. |
| `update(timestamp, state)` | 매 프레임 호출됩니다(게임플레이가 일시정지된 동안은 제외). timestamp는 밀리초 단위의 `state.timeStamp`입니다. |
| `draw(state)` | 매 프레임 호출됩니다. |
| `reloadLanguage(lang)` | 게임 언어가 바뀔 때 호출됩니다. |

엔진은 배경에서 afterSongEnum이나 onDestroy를 호출하지 않습니다. 호스트가 배경을 해제하면 엔진이 배경이 생성한 리소스를 해제합니다.

### 트랜지션

| 메서드 | 설명 |
| --- | --- |
| `onStart()` | 스킨이 로드될 때 스테이지와 액티비티보다 먼저, 코루틴으로 한 번 호출됩니다. |
| `fadeOut(t)` | 나가는 스테이지 위에 페이드아웃을 그립니다. t는 0에서 1까지 변합니다. |
| `loading(progress, elapsed)` | 로딩 화면을 그립니다. progress는 0에서 1, elapsed는 로드가 시작된 뒤 경과한 초입니다. |
| `fadeIn(t)` | 새 스테이지 위에 페이드인을 그립니다. t는 0에서 1까지 변합니다. |
| `onDestroy()` | 엔진이 스킨을 언로드하기 전, 스테이지와 액티비티 다음에 호출됩니다. |
| `reloadLanguage(lang)` | 게임 언어가 바뀔 때 호출됩니다. |

아래 트랜지션 절에서 각 단계의 타이밍을 설명합니다.

### 캐릭터

캐릭터의 Script.lua는 다른 집합을 정의합니다. loadAnimation, disposeAnimation, availableAnimation, setAnimationDuration, resetAnimationCounter, update, draw, getDrawSize, getHeyaRenderOffset, getAIBattlePosition, loadVoice, disposeVoice, playVoice입니다. [캐릭터 추가하기](../guides/characters.md)에서 다룹니다.

### Exit

엔진이 스테이지 스크립트에만 등록하는 함수입니다. 호출하면 엔진에 스테이지를 떠나도록 요청합니다.

<div class="callout warn">
Modules/Stages 스크립트에서만 사용할 수 있습니다. 액티비티와 ROActivity는 호스트가 구동하므로 이 함수를 받지 않습니다. 0~3개의 인자를 받고 어느 위치에서든 nil을 허용합니다. 호출 자체가 종료를 요청합니다. 기본 제공 스테이지는 그 프레임에서 다른 것이 실행되지 않도록 update 안에서 `return Exit(...)`를 씁니다. target은 "title", "play", "stage", "legacy" 중 하나이며, nil이나 다른 값은 "title"을 뜻합니다. target이 "stage"이면 name은 이동할 Modules/Stages 모듈이고, "legacy"이면 name은 "heya", "config", "exit", "onlinelounge" 중 하나입니다(다른 값은 타이틀로 갑니다). transition은 Modules/Transitions 모듈을 지정합니다. 생략했거나 엔진이 찾을 수 없으면 엔진은 "default"라는 이름의 모듈을 사용하고, 스킨에 트랜지션이 전혀 없으면 단순한 검은 페이드아웃이 재생됩니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `Exit(target?, name?, transition?)  -> number` | 지정한 목적지로 스테이지 종료를 요청합니다. 선택적으로 대상 모듈과 트랜지션 모듈을 지정할 수 있으며, 0을 반환합니다. |

```lua
function update(timestamp)
    if INPUT:KeyboardPressed("S") then
        return Exit("stage", "demo2")          -- Modules/Stages/demo2로 이동
    end
    if INPUT:Pressed("Cancel") then
        return Exit("title", nil, "nokon_curtain")   -- 지정한 트랜지션을 거쳐 타이틀로 돌아가기
    end
end
```

### LOADING

코루틴으로 실행되는 콜백(모든 모듈 종류의 onStart, 그리고 스테이지의 activate)을 위한 로딩 바 헬퍼입니다.

<div class="callout warn">
모든 모듈에 LOADING 전역으로 정의됩니다. 이 콜백들은 엔진이 매 프레임 재개하는 엔진 소유 코루틴에서 실행됩니다. 한 번의 재개가 시간 예산을 다 쓰면 엔진이 자동으로 양보하며, coroutine.yield(progress)나 LOADING:Tick(sub)으로 직접 양보할 수도 있습니다. LOADING:Add로 등록한 블록은 콜백 본문이 반환된 뒤 순서대로 실행되고, 각 블록이 끝날 때마다 바가 전진합니다. 블록의 weight는 바에서 차지하는 비중입니다(기본값 1). LOADING:Tick(sub)은 블록 안에서 한 프레임 양보하고 그 블록 안의 0~1 진행률을 보고합니다. 코루틴이 아닌 콜백(액티비티나 ROActivity의 activate, 모든 update나 draw)에서는 양보할 대상이 없으므로 LOADING:Tick이 Lua 오류를 발생시키고, LOADING:Add로 큐에 넣은 블록은 실행되지 않습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `LOADING:Add(fn)  -> nil` | 콜백이 반환된 뒤 실행할 로드 블록을 등록합니다. |
| `LOADING:Add(label, fn)  -> nil` | 라벨이 있는 로드 블록을 등록합니다. |
| `LOADING:Add(label, weight, fn)  -> nil` | 라벨과 명시적 weight가 있는 로드 블록을 등록합니다. |
| `LOADING:Tick(sub)  -> nil` | 블록 안에서 한 프레임 양보하고 현재 블록 안의 0~1 하위 진행률을 보고합니다. |

```lua
function onStart()
    LOADING:Add("textures", 3, function()
        for i, name in ipairs(names) do
            tx[name] = TEXTURE:CreateTexture(name)
            LOADING:Tick(i / #names)
        end
    end)
    LOADING:Add("sounds", 1, function()
        bgm = SOUND:CreateBGM("Sounds/BGM.ogg")
    end)
end
```

## 액티비티

### ACTIVITY

로드된 액티비티를 이름으로 찾는 전역입니다.

<div class="callout warn">
ROActivity와 배경을 제외한 모든 모듈에 ACTIVITY 전역으로 등록됩니다. ROActivity와 배경에서는 nil이며, 그 모듈들은 ROACTIVITY를 사용합니다. 엔진은 액티비티를 Modules/Activities/{name}에서 로드합니다. ACTIVITY는 ROACTIVITY:GetROActivity와 같은 동작을 하는 GetROActivity도 노출합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `ACTIVITY:GetActivity(name)  -> activity handle` | 지정한 폴더 이름의 로드된 액티비티 핸들을 반환하며, 로드된 것이 없으면 nil을 반환합니다. |
| `ACTIVITY:GetROActivity(name)  -> activity handle` | ROACTIVITY:GetROActivity와 같습니다. |

### ROACTIVITY

로드된 읽기 전용 액티비티(ROActivity)를 이름으로 찾는 전역입니다.

<div class="callout warn">
모든 모듈에 ROACTIVITY 전역으로 등록됩니다. 엔진은 ROActivity를 Modules/ROActivities/{name}에서 로드하고 읽기 전용 CONFIG, DATABASE, GetSaveFile 전역을 주므로, 그 스크립트는 게임 상태를 바꿀 수 없습니다(모듈의 동작 방식의 읽기 전용 모듈 절 참고). 액티비티와 ROActivity는 이름을 키로 하는 싱글턴입니다. 폴더마다 인스턴스 하나가 있고 모든 호스트가 이를 공유합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `ROACTIVITY:GetROActivity(name)  -> activity handle` | 지정한 폴더 이름의 로드된 ROActivity 핸들을 반환하며, 로드된 것이 없으면 nil을 반환합니다. |

### 액티비티 핸들

ACTIVITY:GetActivity와 ROACTIVITY:GetROActivity가 반환하는 객체입니다. 호스트는 이것으로 모듈의 콜백을 구동합니다.

<div class="callout warn">
Activate, Deactivate, Draw는 인자를 모듈의 activate, deactivate, draw 콜백에 전달합니다. Update는 현재 게임 시간(밀리초)으로 update를 호출합니다. 각각은 콜백이 반환한 값을 0부터 시작하는 배열로 반환하며, 콜백이 아무것도 반환하지 않았거나 정의되지 않았으면 nil을 반환합니다. 첫 값은 `result[0]`으로 읽습니다. Call은 모듈 스크립트가 정의한 임의의 전역 함수를 호출합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `handle.IsActive  -> boolean` | Activate가 실행된 뒤부터 Deactivate(또는 모듈 자신의 DEACTIVATE())가 실행될 때까지 true입니다. |
| `handle:Activate(...)  -> array` | 지정한 인자로 모듈의 activate 콜백을 호출합니다. |
| `handle:Deactivate(...)  -> array` | 지정한 인자로 모듈의 deactivate 콜백을 호출합니다. |
| `handle:Update()  -> array` | 현재 게임 시간(밀리초)으로 모듈의 update 콜백을 호출합니다. |
| `handle:Draw(...)  -> array` | 지정한 인자로 모듈의 draw 콜백을 호출합니다. |
| `handle:Call(functionName, ...)  -> array` | 지정한 인자로 모듈 스크립트의 이름이 지정된 전역 함수를 호출합니다. |

```lua
local act = nil

function activate()
    if act == nil then act = ACTIVITY:GetActivity("song_select_core") end
    act:Activate()
end

function update(timestamp)
    local result = act:Update()
    local signal = result ~= nil and result[0] or nil
    if signal == "play" then return Exit("play", nil) end
    if signal == "cancel" then return Exit("title", nil) end
end

function draw()
    act:Draw()
end

function deactivate()
    act:Deactivate()
end
```

### DEACTIVATE

엔진이 액티비티와 ROActivity 스크립트 안에 등록하는 함수입니다. 모듈이 스스로를 비활성화할 수 있게 합니다.

<div class="callout warn">
호출하면 모듈을 비활성으로 표시하고(handle.IsActive가 false가 됨) 모듈 자신의 deactivate 콜백을 실행합니다. 기본 제공 대화 상자는 플레이어가 확인하거나 취소할 때 이를 호출하며, 호스트는 IsActive를 지켜보고 대화 상자가 닫혔음을 압니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `DEACTIVATE(...)` | 현재 모듈을 비활성화하고 지정한 인자로 그 deactivate 콜백을 호출합니다. |

### 엔진이 호스팅하는 ROActivity

엔진은 일부 ROActivity를 정해진 폴더 이름으로 찾아 직접 구동합니다. 스킨은 그 이름의 Modules/ROActivities 폴더를 제공해 이를 교체합니다. 정의해야 하는 콜백은 아래에 나열된 엔진 호출 지점이 정합니다. 하나가 없으면 엔진은 해당 기능을 그리지 않습니다.

| 이름 | 엔진이 호출하는 것 |
| --- | --- |
| `nameplate` | 플레이어의 플레이트가 바뀔 때 `activate(player, name, title, dan, data)`; 프레임마다 한 번 `update()`; `draw(mode, ...)`에서 mode 0 = 전체 플레이트 `(x, y, opacity, player, side)`, 1 = 단위 플레이트 `(x, y, opacity, danGrade, textTexture)`, 2 = 칭호 플레이트 `(x, y, opacity, type, textTexture, rarity, nameplateId)`. |
| `modal` | 큐에 있는 각 잠금 해제 모달에 대해 `activate(player, rarity, modalType, ...)`, 그다음 매 프레임 `update()`와 `draw()`. 스크립트는 DEACTIVATE()를 호출해 모달을 닫고, 엔진은 그다음 것을 활성화합니다. |
| `modicons` | 한 번 `activate()`, 그다음 layout이 "menu" 또는 "game"인 `draw(x, y, player, layout, alpha)`. MODICONS 전역이 이를 감쌉니다. |
| `danplate` | 결과 화면과 단위 코스에서 `draw(x, y, opacity, danTick, r, g, b, titleText)`. |
| `popup_menu` | `activate(title, items, fontSize, ...)`. items는 줄바꿈으로 이어진 라벨이고, 그 뒤에 PopupMenu 스킨 위치가 옵니다. 매 프레임 `draw(selected)`; 닫힐 때 `deactivate()`. |
| `config_ui` | 설정 모델과 함께 `activate(model)`; 매 프레임 `update()`를 호출하며 "exit"를 반환하면 설정 화면을 떠남; `draw()`; 엔진이 모델을 다시 만들 때 Call을 통한 `reload(model)`; `deactivate()`. |
| `song_enum` | `activate()`, 그다음 곡 스캔이 실행되는 동안 매 프레임 `draw(isCommandSongDataGet, done, total)`; `deactivate()`. |

## 배경

### 배경 모듈

화면 배경, 게임플레이 레이어, 몹, 클리어 애니메이션, 쿠스다마 효과 중 하나를 그리는 Script.lua로, 엔진 자체 화면이 호스팅합니다.

<div class="callout warn">
배경은 Modules 폴더 밖, 스킨의 Graphics 폴더 아래 꾸미는 화면의 디렉터리에 있습니다. 예를 들어 Graphics/0_Startup/Script.lua, Graphics/10_Heya/Script.lua, Graphics/6_Result/Script.lua, Graphics/5_Game/5_Background/Normal/Up/{variant}/Script.lua, Graphics/5_Game/5_Background/Normal/Down/{variant}/Script.lua, Graphics/5_Game/3_Mob/{variant}/Script.lua, Graphics/5_Game/9_End/{result}/Script.lua, Graphics/5_Game/11_Balloon/Kusudama/Script.lua입니다. 폴더에 여러 변형이 있으면 엔진이 플레이마다 하나를 무작위로(또는 채보의 장면 프리셋에서) 고릅니다. 호스트 화면이 배경 인스턴스를 생성하고(게임플레이 배경은 엔진이 게임 화면에 들어갈 때마다) 화면과 함께 해제하므로, 게임플레이 중에는 여러 개가 동시에 살아 있습니다. 배경 스크립트는 ROActivity와 같은 전역(읽기 전용 CONFIG, DATABASE, GetSaveFile; ACTIVITY 없음)을 받습니다. 아래 이벤트 훅은 선택 사항이며, 엔진은 각 훅을 해당 이벤트가 일어날 때 한 번 호출합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `clearIn(player)` | 게임플레이 Up 및 Down 배경: 플레이어의 게이지가 클리어 구간에 도달했습니다. |
| `clearOut(player)` | 게임플레이 Up 및 Down 배경: 플레이어의 게이지가 클리어 구간에서 벗어났습니다. |
| `playEndAnime(player)` | 클리어 애니메이션(Graphics/5_Game/9_End): 플레이어의 종료 애니메이션이 시작됩니다. |
| `kusuIn()` / `kusuBroke()` / `kusuMiss()` | 쿠스다마: 풍선이 나타남, 플레이어가 터뜨림, 플레이어가 놓침. |
| `skipAnime()` | 결과 배경: 플레이어가 결과 애니메이션을 건너뛰었습니다. |

### 배경 state

호스트가 배경의 activate, update, draw에 전달하는 객체입니다.

<div class="callout warn">
호스트마다 인스턴스 하나가 있으며 호스트가 매 프레임 제자리에서 갱신합니다. 배열 필드는 엔진의 플레이어별 배열을 공유하며 0부터 시작합니다(`state.gauge[0]`이 플레이어 1). 게임플레이 필드는 게임플레이 호스트만 갱신합니다. 다른 호스트는 기본값으로 두며, 게임플레이 밖에서는 timeStamp가 -1로 유지됩니다. state에는 프레임 타이밍이 없으므로 fps 전역을 읽으십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `state.playerCount  -> number` | 플레이어 수. |
| `state.p1IsBlue  -> boolean` | 플레이어 1이 파란 쪽을 사용하면 true. |
| `state.lang  -> string` | 현재 언어 코드. |
| `state.simplemode  -> boolean` | 심플 모드가 켜져 있으면 true. |
| `state.puchicharaRarities  -> string[]` | 각 플레이어 푸치캬라의 희귀도. |
| `state.characterRarities  -> string[]` | 각 플레이어 캐릭터의 희귀도. |
| `state.isClear  -> boolean[]` | 각 플레이어가 현재 클리어 구간에 있는지 여부. |
| `state.gauge  -> number[]` | 각 플레이어의 게이지 값. |
| `state.bpm  -> number[]` | 각 플레이어의 현재 BPM. |
| `state.gogo  -> boolean[]` | 각 플레이어가 고고타임인지 여부. |
| `state.towerNightNum  -> number` | 타워의 낮에서 밤으로의 계수, 0에서 1. |
| `state.battleState  -> number` | AI 배틀 상태 코드. |
| `state.battleWin  -> boolean` | 플레이어가 AI 배틀에서 이기고 있으면 true. |
| `state.timeStamp  -> number` | 채보에 동기화된 시간(초). 게임플레이 밖에서는 -1. |
| `state.paused  -> boolean` | 게임플레이가 일시정지된 동안 true. |
| `state.player  -> number` | 플레이어별 호스트(클리어 애니메이션)가 그리고 있는 플레이어. |

## 트랜지션

### 트랜지션 모듈

두 스테이지 사이의 페이드아웃, 로딩, 페이드인 단계를 그리는 Modules/Transitions/{name}/Script.lua입니다.

<div class="callout warn">
Exit의 세 번째 인자가 트랜지션을 선택합니다. 호출에서 이름을 지정하지 않았거나 엔진이 그 이름을 찾을 수 없으면 엔진은 "default"를 사용합니다. Exit("play") 뒤의 게임플레이 로드는 항상 "song_loading"이라는 이름의 트랜지션을 사용하며, 스킨에 없으면 "default"를 사용합니다. 엔진은 단계를 순서대로 구동합니다. t가 1에 도달할 때까지 나가는 스테이지 위에서 매 프레임 fadeOut(t)를 호출하고, 그다음 나가는 스테이지를 언마운트하고 새 스테이지를 로드하는 동안 매 프레임 loading(progress, elapsed)를 호출하며, 그다음 t가 1에 도달할 때까지 새 스테이지 위에서 fadeIn(t)를 호출합니다. 각 페이드는 스크립트가 FADE_OUT_SECONDS나 FADE_IN_SECONDS를 설정하지 않는 한 0.5초 동안 지속됩니다. 양수가 아닌 값은 엔진이 무시합니다. 스테이지 전환에서 엔진은 로드가 0.5초보다 오래 걸린 뒤에만 loading을 호출합니다. 그 전에는 fadeOut(1)을 호출해 짧은 로드에서 로딩 화면이 깜빡이지 않게 합니다. 곡 로딩 경로는 로딩 단계를 즉시 보여 줍니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `FADE_OUT_SECONDS  -> number` | 선택적 최상위 전역: 페이드아웃 길이(초). |
| `FADE_IN_SECONDS  -> number` | 선택적 최상위 전역: 페이드인 길이(초). |

```lua
FADE_OUT_SECONDS = 0.3
FADE_IN_SECONDS = 0.3

local pixel = nil

local function cover(alpha)
    pixel:SetColor(0, 0, 0)
    pixel:SetOpacity(alpha)
    pixel:SetScale(8000, 8000)
    pixel:Draw(0, 0)
end

function onStart()
    pixel = TEXTURE:CreateTexture("pixel.png")
end

function fadeOut(t) cover(t) end
function fadeIn(t) cover(1.0 - t) end
function loading(progress, elapsed) cover(1.0) end

function onDestroy()
    if pixel ~= nil then pixel:Dispose() end
end
```

## 타이밍과 카메라

### COUNTER

시간에 따라 값을 시작값에서 끝값으로 움직이는 애니메이션 카운터의 팩토리입니다.

<div class="callout warn">
COUNTER 전역으로 등록됩니다. 카운터는 Tick을 호출할 때만 프레임 델타를 interval로 나눈 만큼 전진합니다. interval은 값 1단위당 초입니다. interval의 부호는 방향과 일치해야 합니다. end가 begin보다 크면 양수, 작으면 음수입니다. 부호가 맞지 않으면 카운터는 양 끝을 맞바꾸고 첫 tick에서 끝납니다. CreateCounterDuration은 전체 지속 시간을 받고 부호를 스스로 정합니다. interval이 0이거나 begin과 end가 같으면 카운터는 첫 tick에서 끝납니다. 카운터는 값이 끝에 도달할 때, 그리고 루프나 바운스 중에는 완료된 사이클마다 한 번 선택적 ended 함수를 호출합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `COUNTER:CreateCounter(begin, end, interval, ended?)  -> counter` | 1단위당 interval초로 begin에서 end로 움직이며 완료 시 ended를 호출하는 카운터를 만듭니다. |
| `COUNTER:CreateCounterDuration(begin, end, seconds, ended?)  -> counter` | 지정한 초 동안 begin에서 end로 움직이는 카운터를 만듭니다. seconds가 양수가 아니거나 begin과 end가 같으면 빈 카운터를 반환합니다. |
| `COUNTER:EmptyCounter()  -> counter` | 값이 0에 머무는 비활성 카운터를 만듭니다. 자리 표시자로 쓸 수 있습니다. |

### 카운터 핸들

COUNTER가 만든 카운터입니다.

<div class="callout warn">
매 프레임 Value를 읽고 매 프레임 Tick을 호출해 전진시키십시오. 시작하지 않았거나 멈춘 카운터는 Tick을 무시합니다. Begin, End, Interval은 읽고 쓸 수 있는 필드입니다. SetLoop과 SetBounce는 서로 배타적입니다. SetEasing은 보고되는 Value의 형태만 바꾸며, 카운터는 내부적으로 여전히 선형으로 전진합니다. 리스너는 마지막 tick을 포함한 모든 tick에서 현재 값을 받습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `counter.Value  -> number` | 현재 값. 이징이 설정되어 있으면 적용된 값이며, 대입하면 카운터가 그 값으로 점프합니다. |
| `counter.Begin  -> number` | 시작값(읽기/쓰기 가능). |
| `counter.End  -> number` | 끝값(읽기/쓰기 가능). |
| `counter.Interval  -> number` | 값 1단위당 초(읽기/쓰기 가능). |
| `counter:Start()  -> nil` | 값을 Begin으로 재설정하고 tick을 시작합니다. |
| `counter:Resume()  -> nil` | 값을 재설정하지 않고 tick을 시작합니다. |
| `counter:Stop()  -> nil` | tick을 멈춥니다. |
| `counter:Pause()  -> nil` | Stop과 같습니다. |
| `counter:Reset()  -> nil` | tick 여부를 바꾸지 않고 값을 Begin으로 되돌립니다. |
| `counter:Tick()  -> nil` | 값을 한 프레임만큼 전진시키고, 필요에 따라 리스너와 ended 함수를 호출합니다. |
| `counter:SetLoop(loop)  -> nil` | 값이 끝에 도달하면 Begin으로 되돌아갑니다. 바운스를 끕니다. |
| `counter:SetBounce(bounce)  -> nil` | 값이 어느 끝에든 도달하면 방향을 뒤집습니다. 루프를 끕니다. |
| `counter:GetLoop()  -> boolean` | 루프가 켜져 있는지 여부. |
| `counter:GetBounce()  -> boolean` | 바운스가 켜져 있는지 여부. |
| `counter:SetEasing(type, function)  -> nil` | 보고되는 값에 이징 곡선을 적용합니다. type은 IN, OUT, INOUT, OUTIN이고 function은 LINEAR, SINE, QUAD, CUBIC, QUART, QUINT, EXPO, CIRC, ELASTIC, BACK, BOUNCE입니다(대소문자 구분 없음. 알 수 없는 이름은 카운터가 무시함). |
| `counter:ClearEasing()  -> nil` | 이징을 제거합니다. |
| `counter:Listen(listener)  -> nil` | 각 tick에서 현재 값과 함께 호출되는 함수를 등록합니다. |
| `counter:ClearListeners()  -> nil` | 모든 리스너를 제거합니다. |

```lua
local fade = nil

function activate()
    fade = COUNTER:CreateCounterDuration(0, 1, 0.5, function() debugLog("fade done") end)
    fade:SetEasing("OUT", "QUAD")
    fade:Start()
end

function update(timestamp)
    fade:Tick()
end

function draw()
    background:SetOpacity(fade.Value)
    background:Draw(0, 0)
end
```

### GLOBALCAMERA

화면 전체 2D 카메라입니다. 렌더링된 프레임 전체를 이동, 확대/축소, 회전하고 감쇠하는 화면 흔들림을 더합니다.

<div class="callout warn">
GLOBALCAMERA 전역으로 등록됩니다. TJA #CAMERA 명령과 같은 화면 변환을 구동하며, 블릿된 3D 씬을 포함해 그려지는 모든 것에 영향을 줍니다. 오프셋은 1280x720 기준 픽셀, 회전은 도 단위이며, zoom 1은 확대/축소 없음을 뜻합니다. 기본 변환은 직접 바꿀 때까지 유지됩니다. 매 프레임 Update(dt)를 호출해 적용하고 흔들림을 전진시키며, 스테이지를 떠날 때 Reset()을 호출해 다음 스테이지로 넘어가지 않게 하십시오. 3D 씬 자체의 카메라는 씬 객체에 설정합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `GLOBALCAMERA:SetOffset(x, y)  -> nil` | 화면을 (x, y) 픽셀만큼 이동합니다. |
| `GLOBALCAMERA:SetZoom(sx, sy)  -> nil` | X와 Y 계수를 따로 두어 화면을 확대/축소합니다. |
| `GLOBALCAMERA:SetUniformZoom(s)  -> nil` | 화면을 균일하게 확대/축소합니다. |
| `GLOBALCAMERA:SetRotation(deg)  -> nil` | 화면을 중심을 기준으로 회전합니다. |
| `GLOBALCAMERA:GetOffsetX()  -> number` | 기본 X 오프셋. |
| `GLOBALCAMERA:GetOffsetY()  -> number` | 기본 Y 오프셋. |
| `GLOBALCAMERA:GetZoomX()  -> number` | X 확대/축소 계수. |
| `GLOBALCAMERA:GetZoomY()  -> number` | Y 확대/축소 계수. |
| `GLOBALCAMERA:GetRotation()  -> number` | 기본 회전(도). |
| `GLOBALCAMERA:Shake(amplitudePx, seconds, rotAmpDeg?)  -> nil` | 지정한 픽셀 진폭에서 지정한 초 동안 선형으로 감쇠하는 흔들림을 시작하며, 선택적으로 도 단위의 회전 흔들림을 더합니다. seconds가 0 이하인 호출은 카메라가 무시합니다. 새 흔들림은 진폭이 현재 것과 같거나 더 클 때만 현재 흔들림을 대체합니다. |
| `GLOBALCAMERA.IsShaking  -> boolean` | 흔들림이 아직 감쇠 중이면 true. |
| `GLOBALCAMERA:Update(dt)  -> nil` | 흔들림을 dt초만큼(0.25로 제한) 전진시키고 기본 변환과 흔들림을 화면에 적용합니다. |
| `GLOBALCAMERA:Reset()  -> nil` | 카메라를 중앙으로 되돌리고, 확대/축소와 회전을 재설정하며, 모든 흔들림을 멈춥니다. |

```lua
function update(timestamp)
    if INPUT:Pressed("LRed") or INPUT:Pressed("RRed") then GLOBALCAMERA:Shake(18, 0.35) end
    GLOBALCAMERA:Update(fps.deltaTime)
end

function deactivate()
    GLOBALCAMERA:Reset()
end
```

## 곡 열거

두 전역 함수가 곡 스캔의 상태를 보고합니다. afterSongEnum 콜백과 함께 사용하십시오.

| 메서드 | 설명 |
| --- | --- |
| `IsSongsEnumerating()  -> boolean` | 곡 열거 패스가 실행 중이면 true. |
| `IsSongsEnumDone()  -> boolean` | 곡 스캔이 끝나면 true. 스캔이 시작되기 전과 실행 중에는 false입니다. IsSongsEnumerating은 시작 전 상태와 완료 상태 모두에서 false이므로, 목록이 준비되었는지 알려면 이 함수를 확인하십시오. |

## 진단

### info

기본 게임 상태와 모듈 자체 디렉터리를 담은 읽기 전용 객체입니다.

<div class="callout warn">
info 전역으로 등록되며 모듈마다 생성됩니다. 각 필드는 접근 시 값을 계산합니다. online은 읽을 때마다 운영 체제에 질의합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `info.playerCount  -> number` | 설정된 플레이어 수. |
| `info.lang  -> string` | 현재 언어 코드. |
| `info.simplemode  -> boolean` | 심플 모드가 켜져 있으면 true. |
| `info.p1IsBlue  -> boolean` | 플레이어 1이 파란 쪽을 사용하면 true. |
| `info.online  -> boolean` | 사용 가능한 네트워크 인터페이스가 있으면 true. |
| `info.dir  -> string` | 이 모듈의 디렉터리. |

### fps

프레임 타이밍과 고해상도 시계를 담은 읽기 전용 객체입니다.

| 메서드 | 설명 |
| --- | --- |
| `fps.deltaTime  -> number` | 이전 프레임 이후 경과한 초. |
| `fps.fps  -> number` | 현재 측정된 초당 프레임 수. |
| `fps.ms  -> number` | 밀리초 단위의 단조 시계. 차이를 구해 Lua 구간의 시간을 재는 데 씁니다. |

### debugLog

| 메서드 | 설명 |
| --- | --- |
| `debugLog(message)  -> nil` | Lua 로그임을 표시하는 접두어를 붙여 문자열을 엔진 추적 로그에 기록합니다. |

## 다른 전역

엔진은 이 전역들을 모든 모듈에 등록합니다. 각자의 페이지에서 문서화합니다.

| 전역 | 페이지 |
| --- | --- |
| `GetSaveFile(player)` | [플레이어와 프로필](players.md). ROActivity와 배경 안에서는 읽기 전용 핸들을 반환합니다. |
| `RequestSongList(settings)`, `GenerateSongListSettings()` | [곡과 채보](songs.md). |
| `MODICONS` | [곡과 채보](songs.md). |
| `CONFIG`, `DATABASE`, `SHARED`, `STORAGE`, `JSONLOADER`, `INILOADER`, `SQL` | [데이터와 영속성](data.md). |
| `TEXTURE`, `CANVAS`, `GRAPHICS`, `TEXT`, `VIDEO`, `COLOR`, `GRADIENT`, `SIZE` | [그래픽과 텍스트](graphics.md). |
| `SOUND`, `HITSOUNDSLIST` | [오디오](audio.md). |
| `INPUT` | [입력](input.md). |
| `NAMEPLATE`, `NAMEPLATESLIST`, `CHARACTER`, `CHARACTERLIST`, `PUCHICHARALIST`, `PLAYSTATE`, `THEME`, `LANG` | [플레이어와 프로필](players.md). |
| `VECTOR`, `VECTOR2`, `VECTOR3`, `VECTOR4`, `MATRIX`, `MATRIX2`, `MATRIX3`, `MATRIX4`, `QUATERNION` | [수학](math.md). |
| `SONGMOUNT`, `REPLAY`, `DANBUILDER`, `VIRTUALSLOTS` | [곡과 채보](songs.md). |
| `NET` | [온라인 네트워킹](networking.md). |
| `SCENE3D`, `MODEL`, `PHYSICS`, `COLLIDERS`, `PATHFIND`, `HEIGHTMAP` | [3D 엔진: 래스터라이저 월드](3d.md), [3D 엔진: 레이트레이서 월드](3d-raytrace.md), [3D 엔진: 물리](3d-physics.md) <span class="badge-exp">실험적</span>. |
