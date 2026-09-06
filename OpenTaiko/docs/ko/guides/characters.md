<!-- guides/characters.md -->

# 캐릭터 추가하기

캐릭터는 게임 설치 폴더의 `Global/Characters/` 아래에 있는 폴더입니다. 게임이 거기서 발견하는 모든 하위 폴더가 선택 가능한 캐릭터 하나가 됩니다. 폴더에는 `Metadata.json`(이름, 희귀도, 제작자), `CharaConfig.txt`(위치와 애니메이션 타이밍), 애니메이션 콘텐츠, 그리고 선택적으로 `Effects.json`, `Unlock.json`, `Palettes.json`과 음성 클립이 들어 있습니다. 애니메이션 콘텐츠는 게임 내장 캐릭터 스크립트가 렌더링하는 번호 매겨진 PNG 프레임 폴더이거나, 캐릭터별 `Script.lua`가 그리기로 한 무엇이든 될 수 있습니다(기본 제공 3D 템플릿은 glTF 모델을 그립니다).

호환성: OpenTaiko 0.6.1은 0.6.0용으로 만든 캐릭터를 여전히 변경 없이 로드합니다. 이 페이지는 현재 구성을 설명하므로 새 캐릭터에는 이 구성을 사용하십시오.

## 시작하기 전에

- OpenTaiko 0.6.1 설치. 게임은 캐릭터를 게임 실행 파일 옆의 `Global/Characters/`에서 읽으며, 모든 스킨이 이를 공유합니다.
- JSON과 INI 형식 파일을 위한 텍스트 편집기.
- 2D 캐릭터: 투명 배경의 번호 매겨진 PNG 프레임(`0.png`, `1.png`, ...)으로 내보낸 아트, 애니메이션 상태마다 폴더 하나.
- 3D 캐릭터: 애니메이션 클립을 담은 `model.glb`(바이너리 glTF)와 정지 이미지 `Render.png`.
- 기본 제공 `01 - Template`(2D)과 `01 - Template3D` 폴더. 둘 중 하나를 복사해 시작점으로 삼으십시오.

## 1단계: 검색, 순서, 식별 이해하기

부팅 시 게임은 `Global/Characters/`의 하위 폴더를 나열하고 파일 시스템이 반환하는 순서대로 폴더마다 캐릭터 하나를 만듭니다. 게임은 목록을 정렬하지 않으므로, 기본 제공 폴더는 순서를 예측 가능하게 하기 위해 숫자 접두어(`00 - None`, `01 - Template`, `02 - Student (A)`, ...)를 붙입니다. `00 - None`을 첫 번째로 유지하십시오. 인덱스 0은 빈 슬롯이며 저장된 캐릭터가 없을 때의 대체입니다.

세이브 파일은 선택한 캐릭터를 폴더 이름(`characterName`)으로 저장하고 부팅마다 인덱스로 다시 해석합니다. 다른 폴더를 추가하거나 제거해도 저장된 선택은 깨지지 않지만, 폴더 이름을 바꾸면 그것을 참조하던 세이브는 `00 - None`으로 대체됩니다. 두 캐릭터가 표시 이름을 공유할 수는 있습니다. 폴더 이름은 고유해야 합니다.

게임은 캐릭터를 부팅 시 한 번, 그리고 스킨을 다시 로드할 때 다시 열거합니다. 게임이 실행 중인 동안 추가한 폴더는 다음 부팅이나 스킨 리로드 후에 나타납니다.

## 2단계: 폴더와 Metadata.json 만들기

`30 - MyChara` 같은 폴더를 만들고 `Metadata.json`을 추가합니다.

- `name`: 표시 이름. 일반 문자열 또는 지역화 객체 `{ "strings": { "default": "...", "ja": "...", ... } }`. `default`가 대체값이고 다른 키는 게임 언어 코드입니다.
- `rarity`: `Poor`, `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythical` 중 하나. 희귀도는 색상과 잠금 해제 알림 등급만 제어합니다. 모든 희귀도의 코인 배율은 1입니다.
- `author`: 일반 문자열 또는 지역화 객체.
- `description`: 선택 사항, 일반 문자열 또는 지역화 객체.
- `speechtext`: 선택 사항, 결과 화면이 캐릭터의 말풍선에 표시하는 여섯 개의 지역화 객체 배열. 게임은 결과에 따라 이 순서로 항목을 고릅니다: 게이지가 낮은 실패, 게이지 40% 이상의 실패, 클리어, 게이지 가득 찬 클리어, 풀 콤보, 올 퍼펙트. 여섯 개보다 적게 주면 게임은 마지막 것을 반복합니다.

`Metadata.json`이 없어도 캐릭터는 이름 `(None)`, 희귀도 `Common`, 제작자 `(None)`으로 로드됩니다.

```json
{
  "name": {
    "strings": {
      "default": "My Character",
      "ja": "マイキャラ"
    }
  },
  "rarity": "Common",
  "author": {
    "strings": {
      "default": "Your Name"
    }
  }
}
```

## 3단계(2D 경로): 프레임 폴더 추가하기

폴더에 `Script.lua`가 없으면 게임은 내장 스크립트(게임 설치 폴더의 `CharaScript.lua`)로 캐릭터를 렌더링합니다. 그 스크립트는 각 애니메이션 상태를 하위 폴더에 대응시키고 거기서 `0.png`, `1.png`, `2.png`, ...를 로드합니다. 로드는 처음 빠진 번호에서 멈추므로 번호는 연속이어야 합니다.

| 애니메이션 상태 | 폴더 |
|---|---|
| Game/Normal, Game/Clear, Game/Max | `Normal`, `Clear`, `Clear_Max` |
| Game/Gogo, Game/Gogo_Max | `GoGo`, `GoGo_Max` |
| Game/Miss, Game/Miss_Down | `Miss`, `MissDown` |
| Game/10combo, Game/10combo_Max | `10combo`, `10combo_Max` |
| Game/Cleared, Game/Failed | `Cleared`, `Failed` |
| Game/Clear_In, Game/Clear_Out | `Clearin`, `ClearOut` |
| Game/Max_In, Game/Max_Out | `Soulin`, `SoulOut` |
| Game/Miss_In, Game/Miss_Down_In, Game/Return | `MissIn`, `MissDownIn`, `Return` |
| Game/GoGoStart, Game/GoGoStart_Clear, Game/GoGoStart_Max | `GoGoStart`, `GoGoStart_Clear`, `GoGoStart_Max` |
| Game/Balloon_Breaking, Game/Balloon_Broke, Game/Balloon_Miss | `Balloon_Breaking`, `Balloon_Broke`, `Balloon_Miss` |
| Game/Kusudama_Breaking, Game/Kusudama_Broke, Game/Kusudama_Miss, Game/Kusudama_Idle | `Kusudama_Breaking`, `Kusudama_Broke`, `Kusudama_Miss`, `Kusudama_Idle` |
| Game/Tower/Standing, Climbing, Running, Clear, Fail (및 `_Tired` 변형) | `Tower_Char/Standing`, `Tower_Char/Climbing`, `Tower_Char/Running`, `Tower_Char/Clear`, `Tower_Char/Fail` (그리고 `Tower_Char/Standing_Tired` 등) |
| Menu/Wait, Menu/Start, Menu/Normal, Menu/Select | `Menu_Wait`, `Menu_Start`, `Menu_Loop`, `Menu_Select` |
| Entry/Normal, Entry/Jump | `Title_Normal`, `Title_Entry` |
| Result/Normal, Result/Clear, Result/Failed_In, Result/Failed | `Result_Normal`, `Result_Clear`, `Result_Failed_In`, `Result_Failed` |

내장 스크립트는 폴더 루트에서 정지 이미지 두 개를 읽습니다. `Render.png`(전체 크기 초상화. 예를 들어 방에서처럼 게임이 Render 애니메이션 타입을 요청하는 곳에 그려짐)와 `Preview.png`(썸네일. 없으면 스크립트가 `Normal/0.png`를 사용함)입니다.

없는 상태는 다른 상태로 대체되므로 캐릭터는 일부만 제공해도 됩니다. 대체 체인은 다음과 같습니다: Clear -> Normal, Max -> Clear, Miss -> Normal, Miss_Down -> Miss, Gogo -> Normal, Gogo_Max -> Gogo, 10combo_Max -> 10combo, GoGoStart_Clear -> GoGoStart, GoGoStart_Max -> GoGoStart_Clear, Tower `_Tired` 상태 -> 해당 일반 상태, Tower/Fail -> Tower/Standing_Tired, Kusudama_Idle -> Normal, Menu/Wait -> Gogo, Menu/Start와 Menu/Select와 Entry/Jump -> 10combo, Menu/Normal과 Entry/Normal과 Result/Normal -> Normal, Result/Clear -> Clear, Result/Failed_In -> Miss_In, Result/Failed -> Miss. 대체가 없는 상태(예: Cleared, Failed, Return, 풍선 상태)는 없으면 아무것도 그리지 않습니다. 동작하는 캐릭터의 최소 조건은 `Normal/0.png`입니다.

```
30 - MyChara/
  Metadata.json
  CharaConfig.txt
  Render.png
  Normal/0.png 1.png 2.png ...
  Clear/0.png ...
  GoGo/0.png ...
  Miss/0.png ...
  Menu_Loop/0.png ...
  Result_Clear/0.png ...
  Sounds/                (선택적 음성 클립, 6단계 참고)
```

## 4단계: CharaConfig.txt 작성하기

`CharaConfig.txt`는 `Key=Value` 텍스트 파일이며 `;`로 시작하는 줄은 주석입니다. 내장 스크립트는 다음 키를 읽습니다(기본 제공 3D 템플릿도 위치 키를 읽습니다).

- `Chara_Resolution=W,H`(기본값 `1280,720`): 아래 좌표를 작성하는 기준 해상도. 게임은 그릴 때 위치를 이 해상도에서 스킨 해상도로 스케일합니다.
- `Chara_LegacyMode`(기본값 `1`): 0.6.0의 앵커링과 오프셋 보정을 유지합니다. 이전 버전에서 이식한 캐릭터는 이에 의존합니다.
- `Game_Chara_X=...` / `Game_Chara_Y=...`: 게임플레이 위치. 스크립트는 각 목록의 첫 값을 사용합니다. `Game_Chara_Offset=X,Y`는 대안 형식입니다.
- `Game_Chara_X_AI=...` / `Game_Chara_Y_AI=...`: AI 배틀용 플레이어별 값 하나. 두 키가 모두 있으면 이 캐릭터에 대해 스킨의 AI 배틀 위치를 대체합니다.
- `Game_Chara_Balloon_X` / `Game_Chara_Balloon_Y`, `Game_Chara_Kusudama_X` / `Game_Chara_Kusudama_Y`: 풍선과 쿠스다마 시퀀스 동안의 위치(첫 값 사용). `Game_Chara_Balloon_Offset`, `Game_Chara_Kusudama_Offset`, `Game_Chara_Tower_Offset`은 `X,Y` 쌍을 받습니다.
- `Menu_Offset=X,Y`, `Menu_Chara_Scale`, `Result_Offset=X,Y`, `Heya_Chara_Render_Offset=X,Y`: 메뉴, 결과, 방 렌더의 오프셋.
- `Game_Chara_Motion_<State>=0,1,2,...`: 상태의 프레임 재생 순서를 0부터 시작하는 프레임 인덱스로. 생략하면 프레임이 파일 순서로 재생됩니다. 상태 이름은 폴더 이름을 따릅니다. 예: `Game_Chara_Motion_Normal`, `Game_Chara_Motion_GoGo`, `Game_Chara_Motion_Miss_Down`, `Game_Chara_Motion_Balloon_Broke`, `Game_Chara_Motion_Tower_Climbing`.
- `Game_Chara_Beat_<State>=N`: 상태의 한 루프가 차지하는 박자 수. 예: `Game_Chara_Beat_Normal=1`, `Game_Chara_Beat_GoGo=2`.
- 메뉴, 타이틀, 결과 상태는 `Menu_Chara_Motion_Loop/Wait/Start/Select`, `Title_Chara_Motion_Normal/Entry`, `Result_Chara_Motion_Normal/Clear/Failed_In/Failed`를 사용하며, 대응하는 `_Beat_` 키 또는 밀리초 단위 고정 길이를 씁니다: `Chara_Menu_Loop_AnimationDuration`, `Chara_Menu_Wait_AnimationDuration`, `Chara_Menu_Start_AnimationDuration`, `Chara_Menu_Select_AnimationDuration`, `Chara_Normal_AnimationDuration`, `Chara_Entry_AnimationDuration`, `Chara_Result_Normal_AnimationDuration`, `Chara_Result_Clear_AnimationDuration`, `Chara_Result_Failed_In_AnimationDuration`, `Chara_Result_Failed_AnimationDuration`.

기본값을 포함한 전체 키 목록은 내장 `CharaScript.lua` 상단의 `load_chara_config_defs` 테이블입니다. 스크립트는 모르는 키를 무시하므로, 기본 제공 `01 - Template/CharaConfig.txt`에는 이 파일에서 효과가 없는 스킨 측 키도 몇 개 들어 있습니다.

```ini
Chara_Version=0.6.1.0
Chara_Resolution=1920,1080

;캐릭터 X 위치 (1P,2P)
Game_Chara_X=0,0
;캐릭터 Y 위치 (1P,2P)
Game_Chara_Y=0,805

;Normal 상태의 프레임 순서와 루프당 박자 수
Game_Chara_Motion_Normal=0,1,2,3,4,5,6
Game_Chara_Beat_Normal=1

;GoGo 프레임 순서와 루프당 박자 수
Game_Chara_Motion_GoGo=0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15
Game_Chara_Beat_GoGo=2
```

## 5단계(3D 경로): model.glb와 캐릭터별 Script.lua 제공하기

캐릭터 폴더에 `Script.lua`가 있으면 내장 스크립트를 완전히 대체합니다. 게임은 그러면 다음 전역 함수를 이름으로 호출합니다.

- `loadAnimation(animationType)`, `disposeAnimation(animationType)`
- 불리언을 반환하는 `availableAnimation(animationType)`. 게임은 옛 오타 `avaialbeAnimation`도 여전히 허용합니다. `availableAnimation`을 먼저 시도하고 `avaialbeAnimation`으로 대체합니다. 기본 제공 3D 템플릿은 아직 옛 이름을 사용합니다.
- `setAnimationDuration(animationType, durationMs)`, `resetAnimationCounter(animationType)`
- 루프하지 않는 애니메이션이 끝났을 때 `true`를 반환하는 `update(delta, animationType, looping)`
- `draw(animationType, x, y, scaleX, scaleY, opacity, color, contextType, anchor, clipW, clipH, clipX, clipY, rotation, blendMode, wrapMode, gradientMap)`
- 너비와 높이를 반환하는 `getDrawSize(animationType)`
- x와 y를 반환하는 `getHeyaRenderOffset()`; x와 y를 반환하거나 스킨의 위치를 쓰려면 `nil`을 반환하는 `getAIBattlePosition(player, charaScale)`
- `loadVoice(voiceType)`, `disposeVoice(voiceType)`, `playVoice(voiceType)`

애니메이션 타입은 `CHARACTER.ANIM_*` 상수 뒤의 문자열(`"Game/Normal"`, `"Menu/Normal"`, ...)과 두 특수 타입 `CHARACTER.ANIM_PREVIEW`(썸네일), `CHARACTER.ANIM_RENDER`(전체 초상화)입니다. 음성 타입은 `CHARACTER.VOICE_*` 상수입니다. 3단계의 대체 체인은 스크립트 캐릭터에도 적용됩니다. 게임은 `availableAnimation`에 묻고 사용 가능한 것이 나올 때까지 대안을 순회합니다.

기본 제공 `01 - Template3D` 폴더에는 `CharaConfig.txt`, `Effects.json`, `Metadata.json`, `model.glb`, `Render.png`, `Script.lua`만 있습니다. 그 스크립트는 `MODEL:Load`로 `model.glb`를 로드하고, `SCENE3D:CreateScene`으로 직접 만든 씬에 렌더링하며, `CharaConfig.txt`의 위치 키를 읽고, `CLIP` 테이블에서 모든 애니메이션 타입을 클립 인덱스와 박자 수에 대응시킵니다. 3D 캐릭터를 만들려면 폴더를 복사하고 `model.glb`와 `Render.png`를 교체한 뒤, 각 타입이 모델의 올바른 클립 인덱스를 가리키도록 `CLIP`을 편집하십시오.

```lua
-- 01 - Template3D/Script.lua에서 발췌
local CLIP = {
  [CHARACTER.ANIM_GAME_NORMAL] = { clip = 0,  beat = 1 };
  [CHARACTER.ANIM_GAME_CLEAR]  = { clip = 1,  beat = 1 };
  [CHARACTER.ANIM_GAME_GOGO]   = { clip = 2,  beat = 4 };
  [CHARACTER.ANIM_MENU_NORMAL] = { clip = 14, beat = 2 };
  [CHARACTER.ANIM_RESULT_CLEAR]= { clip = 19, beat = 1 };
  -- 모델이 지원하는 애니메이션 상태마다 항목 하나
}

function loadAnimation(animationType)
  -- 클립 / 미리 보기 / 렌더 데이터를 만들고 사용 가능으로 표시합니다
end

function availableAnimation(animationType)
  return animations[animationType] ~= nil
end
```

## 6단계: 선택 파일: Effects.json, Unlock.json, Palettes.json, 음성

- `Effects.json`: `gauge`(`Normal`, `Hard`, `Extreme`. 기본값 `Normal`)는 혼 게이지 타입을 고릅니다. 게임이 일반 게이지를 강제하지 않는 한 `Hard`는 코인 획득량을 1.5배, `Extreme`은 1.8배로 합니다. Minesweeper 펀 모드가 활성일 때 `bombFactor`(1-100, 기본값 20)는 모드가 지뢰로 바꾸는 노트의 비율이고, `fuseRollFactor`(0-100, 기본값 0)는 모드가 퓨즈 롤로 바꾸는 풍선의 비율입니다.
- `Unlock.json`: 있으면 플레이어가 조건을 충족할 때까지 캐릭터가 잠긴 상태로 유지됩니다. 형식과 조건 id는 곡의 것과 같습니다. 잠금 해제 요소 가이드를 참고하십시오. 코인 조건은 플레이어가 방 화면에서 구매하고, 다른 조건은 게임이 결과 화면에서 자동으로 검사합니다. 기본 제공 예: Kuro는 `{ "condition": "dp", "type": "me", "values": [3, 3, 10] }`(풀 콤보 이상으로 Extreme 채보 열 곡 클리어), Aoi는 `{ "condition": "ch", "type": "me", "values": [200] }`(200코인)를 사용합니다.
- `Palettes.json`: 플레이어가 캐릭터에 적용할 수 있는 색상 팔레트 배열. 각 항목은 `name`, `blend`(0-1), `stops`(`[position, R, G, B]` 또는 `[position, R, G, B, A]` 그라디언트 정지점 배열. 최소 두 개를 주십시오), 그리고 팔레트를 잠금 해제하는 이 캐릭터의 플레이 횟수 `plays`(0이거나 없으면 즉시 사용 가능)를 가집니다. `"stops": null`인 항목은 틴트 없는 기본값입니다.
- 음성: 내장 스크립트는 캐릭터 폴더 안의 고정 경로에서 `.ogg` 파일을 로드합니다. 예: `Sounds/Clear/Clear.ogg`, `Sounds/Clear/Failed.ogg`, `Sounds/Clear/FullCombo.ogg`, `Sounds/Clear/AllPerfect.ogg`, `Sounds/Menu/SongSelect.ogg`, `Sounds/Menu/SongDecide.ogg`, `Sounds/Menu/DiffSelect.ogg`, `Sounds/Title/Sanka.ogg`, `Sounds/Result/BestScore.ogg`, `Sounds/Result/ClearSuccess.ogg`, `Sounds/Result/ClearFailed.ogg`. 전체 목록은 내장 `CharaScript.lua` 상단의 `voice_files` 테이블입니다. 스크립트는 없는 파일을 건너뜁니다.

```json
{
  "gauge": "Normal",
  "bombFactor": 20,
  "fuseRollFactor": 0
}
```

```json
{
  "condition": "ch",
  "type": "me",
  "values": [ 200 ]
}
```

```json
[
  { "name": "Default", "stops": null },
  { "name": "Green", "blend": 1.0, "stops": [ [0, 0, 0, 0], [0.25, 0, 255, 0] ], "plays": 10 }
]
```

## 7단계: 재시작하고 캐릭터 선택하기

게임을 재시작하십시오(또는 설정에서 스킨을 다시 로드). 캐릭터가 방 화면의 캐릭터 목록에 나타나며, 잠긴 캐릭터는 잠금 해제 조건을 보여 줍니다. Lua 스테이지도 `CHARACTERLIST` 전역으로 목록을 읽을 수 있으며, 각 항목의 폴더 이름, 표시 이름, 희귀도, 잠금 해제 조건을 노출합니다.

## 문제 해결과 참고

- 캐릭터가 나타나지 않음: 폴더가 `Global/Characters/` 바로 아래에 있는지 확인하고 게임을 재시작하십시오. 게임은 목록을 부팅 시 한 번 만듭니다.
- 캐릭터가 아무것도 그리지 않음: `Normal/0.png`가 없거나 폴더 이름이 3단계의 표와 맞지 않습니다. 프레임은 빈틈 없이 `0.png`, `1.png`, ...로 이름 붙여야 합니다. 빈틈이 있으면 애니메이션은 오류 없이 그 인덱스에서 끝납니다.
- 캐릭터가 화면 밖에 있거나 크기가 잘못됨: `Chara_Resolution`이 위치 값을 작성한 해상도와 일치해야 합니다. 키가 없으면 게임은 `1280,720`을 가정합니다.
- 애니메이션 세트의 일부만 재생됨: 대체가 없는 상태(Cleared, Failed, Return, 풍선과 쿠스다마 상태)는 자체 폴더가 필요합니다.
- 3D 캐릭터의 모든 애니메이션이 사용 불가로 표시됨: `Script.lua`가 `availableAnimation`(또는 `avaialbeAnimation`)을 정의하고 로드된 타입에 대해 `true`를 반환해야 합니다.
- `Script.lua`가 있으면 내장 스크립트를 완전히 대체합니다. 스크립트 캐릭터도 번호 매겨진 PNG 폴더를 로드할 수 있지만, 스크립트가 직접 로드해야 합니다.
- 세이브는 폴더 이름을 참조하므로, 플레이어가 이미 선택한 폴더의 이름을 바꾸면 그들의 선택은 빈 슬롯으로 재설정됩니다.
- 기본 제공 JSON 파일에는 후행 쉼표가 있습니다. 게임의 JSON 파서는 이를 허용하고, 엄격한 검사기는 거부합니다.
