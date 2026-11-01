<!-- guides/notes-skins.md -->

# 노트와 판정 팩 추가하기

스킨은 노트와 판정 문자의 모습을 여러 개 제공할 수 있고, 플레이어는 설정에서 그중 하나를 고릅니다. 각 모습은 팩이며, 스킨의 `Graphics/5_Game/` 아래에 있는 번호가 매겨진 폴더입니다. 노트 팩에는 노트, 연타, 풍선, 히트 이펙트, 그리고 풍선과 연타의 말풍선이 들어 있고, 판정 팩에는 판정 문자가 들어 있습니다. 이 가이드는 폴더, 각 팩에 들어 있는 시트, 팩을 설명하는 `Definition.json` 파일, 그리고 팩에 없는 것을 게임이 어떻게 채우는지 설명합니다.

## 시작하기 전에

- 직접 만든 스킨([스킨과 테마 추가하기](skins.md) 참고). 기본 제공 스킨 `System/Open-World Memories/`에는 복사해서 쓸 수 있는 태고 노트 팩 4개, 봉고 노트 팩 1개, 판정 팩 4개가 있습니다.
- 투명도가 있는 PNG 파일을 저장할 수 있는 이미지 편집기.
- 스킨의 노트 셀 크기: 레이아웃 값 `Game_Notes_Size=`(기본 제공 스킨에서는 `195,195`). 이 페이지의 노트 시트는 거대 노트 시트를 빼고 모든 셀이 이 크기입니다.

## 1단계: 팩 폴더 만들기

게임은 `Graphics/5_Game/` 아래에서 세 폴더를 찾고, 그 안의 번호가 매겨진 하위 폴더를 각각 하나의 팩으로 취급하여 번호순으로 정렬합니다.

- `Notes Skin (Taiko)/`: 태고 플레이어용 노트 팩.
- `Notes Skin (Bongo)/`: 봉고 플레이어용 노트 팩.
- `Judges Skin/`: 판정 문자 팩. 모든 플레이어가 사용합니다.

각 폴더의 팩 `1`은 기본 팩이며 다른 팩의 바탕이 됩니다(7단계). 폴더 번호는 팩의 순서만 정하고, 설정에는 `Definition.json`의 이름이 표시됩니다.

```
Graphics/5_Game/
  Notes Skin (Taiko)/
    1/                  <- the default look, complete
      Definition.json
      Notes.png
      ...
    2/                  <- another look: only the files that differ
      Definition.json
      Notes.png
  Notes Skin (Bongo)/
    1/
  Judges Skin/
    1/
      Definition.json
      Judge.png
```

## 2단계: 태고 노트 시트 그리기

시트는 애니메이션 프레임마다 한 행을 가지며(프레임 수는 5단계에서 정합니다), 셀은 아래 순서대로 나란히 놓입니다.

| 파일 | 내용 |
| --- | --- |
| `HitCircle.png` | 판정 원, 셀 하나. |
| `Notes.png` | 동, 캇, 큰 동, 큰 캇, KaDon, 애드립. |
| `Rolls.png` | 작은 연타의 머리, 몸통, 꼬리, 그다음 큰 연타의 머리, 몸통, 꼬리. 게임은 몸통 셀을 연타 길이에 맞춰 늘립니다. |
| `Balloons.png` | 풍선(셀 2개 너비), 쿠스다마(셀 2개 너비), 그다음 도화선 연타의 머리, 몸통, 꼬리. 봉고 플레이어에게도 이 풍선이 보입니다. |
| `ChipEffect.png` | 선택 사항. 친 노트가 도착하는 곳에 표시되는 섬광으로, 열은 `Notes.png`와 같고 한 행입니다. 없으면 게임은 노트 자체를 번쩍이게 합니다. |
| `Notes_Partner.png` | 선택 사항. `Notes.png` 각 열의 파트너 모습(6단계). |
| `Notes_Giant.png` | 선택 사항. 거대한 동, 캇, KaDon, 애드립(6단계). |
| `Preview.png` | 선택 사항. 설정이 그 팩에 대해 보여 주는 그림. 없으면 `Notes.png`를 보여 줍니다. |

팩에는 히트 이펙트와 풍선 및 연타의 말풍선도 들어 있습니다.

| 파일 | 내용 |
| --- | --- |
| `Hit/Explosion.png` | 판정 원 위의 폭발로, 한 행에 정사각형 프레임 7개입니다. 얼쑤 행, 좋다 행, 그리고 선택적으로 큰 노트의 얼쑤 행과 좋다 행이 이어집니다. 2행뿐이면 큰 노트는 처음 두 행을 사용합니다. |
| `Hit/Explosion_Big.png` | 큰 노트 위에 추가로 나오는 폭발. 그림 한 장이며 두 판정 모두에 그려집니다. |
| `Hit/FireWorks.png` | 날아가는 큰 노트가 남기는 불꽃. |
| `Hit/Bomb.png` | 친 폭탄이나 실패한 도화선 연타의 폭발. 한 행에 7프레임. |
| `Hit/Great/`, `Hit/Great_Big/`, `Hit/Good/`, `Hit/Good_Big/` | 선택적인 프레임 폴더(`0.png`부터 `14.png`). 작은 노트나 큰 노트에서 얼쑤(`Great`)나 좋다(`Good`)가 나왔을 때 레인 위에 그려집니다. |
| `Kusudama/` | 쿠스다마의 `Script.lua`와 `Kusudama_Number.png`를 포함한 그림들. |
| `Fuseroll/` | 도화선 연타의 말풍선인 `Fuse.png`와 그 숫자인 `Number_Fuse.png`. |
| `Breaking.png` | 풍선 말풍선이 터지는 그림. 높이가 같은 그림 6장을 위아래로 쌓습니다. |
| `Balloon.png` | 풍선 말풍선. |
| `Roll.png` | 연타 카운터의 말풍선. 애니메이션 프레임을 나란히 놓습니다. |

## 3단계: 봉고 노트 팩 그리기

봉고 팩에는 봉고 플레이어에게 달라지는 것만 들어 있습니다.

| 파일 | 내용 |
| --- | --- |
| `Notes.png` | 빨강, 노랑, 분홍, 클랩, 애드립. |
| `Rolls.png` | 빨강, 분홍, 노랑, 클랩 연타. 각각 머리, 몸통, 꼬리. |
| `HitCircle.png` | 선택 사항. 없으면 봉고 플레이어에게 태고 팩의 판정 원이 보입니다. |
| `ChipEffect.png`, `Notes_Partner.png` | 선택 사항. 열은 이 팩의 `Notes.png`와 같습니다. |
| `Notes_Giant.png` | 선택 사항. 열은 이 팩의 `Notes.png`와 같습니다. |
| `Preview.png` | 선택 사항. |

풍선, 히트 이펙트, 말풍선은 항상 선택된 태고 팩에서 가져옵니다.

## 4단계: 판정 팩 그리기

판정 팩에는 `Judge.png`가 들어 있습니다. 높이가 같은 5개 행으로, 얼쑤, 좋다, 에구, 애드립, 그리고 폭탄의 Boom입니다. 판정 팩의 `Definition.json`에는 `Name`만 있으면 됩니다. 행을 투명하게 두면 그 판정이 숨겨집니다. 기본 제공 스킨의 "(Without Good)" 팩은 얼쑤 행을 이렇게 합니다.

## 5단계: Definition.json 작성하기

`Definition.json`은 팩의 이름을 정하고 시트를 설명합니다. 모든 필드는 선택 사항이며, 이 파일이 없는 팩은 폴더 번호를 이름으로 표시하고 아래의 기본값을 사용합니다.

```json
{
	"Name": { "strings": { "default": "Sky Spirits", "ja": "スカイスピリッツ" } },
	"Notes": {
		"Frames": 3,
		"Animation": [
			{ "FromCombo": 0, "Rows": [0] },
			{ "FromCombo": 50, "Rows": [1, 1, 0, 0], "RowsOnEntry": [0] },
			{ "FromCombo": 150, "Rows": [0, 1] },
			{ "FromCombo": 300, "Rows": [0, 2], "RowsOnEntry": [0, 1] }
		]
	},
	"Rolls": { "Frames": 3, "Animation": [] },
	"Balloons": { "Frames": 3, "Animation": [] },
	"FlyingNotesFrame": 1,
	"PartnerNotesScale": 1.4142,
	"GiantNotesScale": 2.0
}
```

| 필드 | 기본값 | 의미 |
| --- | --- | --- |
| `Name` | 폴더 번호 | 설정에 표시되는 이름. 언어 코드마다 문자열 하나이며, 그 밖의 모든 언어에는 `default`를 씁니다. |
| `Notes`, `Rolls`, `Balloons` | 1프레임, 애니메이션 없음 | 시트의 행 수(`Frames`)와 애니메이션(`Animation`). `Notes`는 `Notes_Partner.png`와 `Notes_Giant.png`의 행도 정합니다. |
| `FlyingNotesFrame` | `0` | 친 뒤 게이지로 날아가는 노트에 쓰는 `Notes.png`의 행. |
| `PartnerNotesScale` | 2의 제곱근 | 팩에 `Notes_Partner.png`가 없을 때 작은 파트너 노트의 배율(6단계). |
| `GiantNotesScale` | `2` | 팩에 `Notes_Giant.png`가 없을 때 거대 노트의 배율(6단계). |

`Animation`은 티어의 목록입니다. 게임은 플레이어의 콤보가 도달한 티어 중 `FromCombo`가 가장 큰 티어를 사용합니다. 한 박자는 4스텝이며, `Rows`는 각 스텝에서 그릴 행을 정합니다. 목록이 짧으면 반복되므로 `[0, 1]`은 스텝마다 번갈아 바뀌고 `[0]`은 멈춰 있습니다. `RowsOnEntry`는 콤보가 그 티어에 도달한 뒤 약 3분의 2초 동안 `Rows`를 대신합니다. 티어가 없으면 시트는 첫 행을 표시합니다. 예시의 표는 게임이 예전부터 써 온 애니메이션이며, 설정의 간단모드에서는 모든 시트가 첫 행에 머뭅니다.

두 개의 선택 필드로 티어를 조정할 수 있습니다. `FromComboByDifficulty`는 일부 난이도에 대해 티어의 콤보를 정하며, 키는 `Easy`, `Normal`, `Hard`, `Extreme`, `ExtraExtreme`입니다. 다른 난이도는 `FromCombo`를 사용하므로, `"FromCombo": 100, "FromComboByDifficulty": { "Easy": 30 }`은 Easy에서 30 콤보, 다른 난이도에서 100 콤보부터 티어를 시작합니다. 타워 채보는 Normal 쪽이면 Normal로, 다른 쪽이면 Extreme으로 취급되고, 단위에서는 플레이 중인 곡의 난이도를 사용합니다.

`RowsByNote`는 일부 노트에 고유한 `Rows`를 주며, 키는 아래 표의 이름입니다. 다른 노트는 `Rows`를 유지합니다. `Notes.png`에서 `"Rows": [0, 2], "RowsByNote": { "BigDon": [1, 2] }`는 큰 동을 제외한 모든 노트에서 행 0과 2를 번갈아 보여 주고, 큰 동은 행 1과 2를 번갈아 보여 줍니다. 연타의 몸통과 꼬리는 머리를 따르고, `RowsOnEntry`는 여전히 모든 노트에 적용됩니다.

| 시트 | 노트 이름 |
| --- | --- |
| `Notes.png`, 태고 | `Don`, `Ka`, `BigDon`, `BigKa`, `KaDon`, `ADLib` |
| `Notes.png`, 봉고 | `Red`, `Yellow`, `Pink`, `Clap`, `ADLib` |
| `Rolls.png`, 태고 | `Roll`, `BigRoll` |
| `Rolls.png`, 봉고 | `RedRoll`, `PinkRoll`, `YellowRoll`, `ClapRoll` |
| `Balloons.png` | `Balloon`, `Kusudama`, `FuseRoll` |

## 6단계: 파트너 노트와 거대 노트

파트너 노트는 대부분 크기가 그대로지만, 작은 노트(작은 동이나 캇, 애드립, 폭탄, 모든 봉고 노트)는 큰 모습이 됩니다. 거대 노트는 훨씬 크고 천천히 흔들립니다. 각각 선택적인 시트가 있습니다.

- `Notes_Partner.png`는 `Notes.png`와 같은 열과 같은 수의 행을 가집니다. 작은 파트너 노트는 자기 열의 셀을 사용합니다.
- `Notes_Giant.png`는 `Notes.png`와 같은 수의 행을 가집니다. 태고 팩에서는 열이 동, 캇, KaDon, 애드립입니다. 거대 노트는 원래 크기와 상관없이 크게 보이므로, 작은 동과 큰 동은 둘 다 동 셀을 사용합니다. 봉고 팩에서는 열이 `Notes.png`와 같습니다. 셀 크기는 시트의 너비를 열 수로 나눈 값이므로, 거대 노트의 셀은 노트 셀보다 클 수 있습니다.

이 시트들이 없으면 게임은 `Notes.png`로 모습을 만듭니다.

| 노트 | 파트너(`Notes_Partner.png` 없음) | 거대(`Notes_Giant.png` 없음) |
| --- | --- | --- |
| 태고의 작은 동이나 캇 | 큰 동이나 큰 캇 | 큰 동이나 큰 캇에 `GiantNotesScale`을 곱한 크기 |
| 그 밖의 작은 노트 | 노트에 `PartnerNotesScale`을 곱한 크기 | 노트에 `PartnerNotesScale`과 `GiantNotesScale`을 곱한 크기 |
| 큰 동, 큰 캇, KaDon | 변화 없음 | 노트에 `GiantNotesScale`을 곱한 크기 |

팩에 두 시트가 모두 있어도 두 배율을 모두 설정하십시오. 그러면 팩을 복사하고 시트를 지운 스킨 제작자도 알맞은 크기를 얻습니다. 배율을 적용한 노트는 늘인 그림이므로 그 크기로 그린 셀보다 흐릿해 보입니다.

<div class="callout warn">게임은 Notes_Partner.png, Notes_Giant.png, ChipEffect.png를 선택된 팩에서만 읽습니다. 이 파일들은 그 팩 자신의 노트와 맞아야 하므로, 팩 1의 파일이 대신 쓰이는 일은 없습니다.</div>

## 7단계: 나머지는 팩 1이 채우게 하기

선택된 팩에 파일이 없으면 게임은 같은 폴더의 팩 1에서 그 파일을 가져옵니다. 따라서 팩에는 바꾸는 것만 넣을 수 있습니다. `Hit/Explosion.png`의 한 행만 지우는 팩에는 `Definition.json`과 그 파일 하나만 들어 있습니다. 규칙은 다음과 같습니다.

- 게임은 파일을 하나씩 따로 확인하며, 폴더 안의 파일(`Hit/Great/3.png`)도 마찬가지입니다. 예외는 쿠스다마로, 자체 `Kusudama/` 폴더가 없는 팩은 팩 1의 폴더 전체를 사용합니다.
- 대체는 팩 1까지만 닿습니다. 팩 3을 변형한 팩은 팩 3의 시트 사본을 직접 가지고 있어야 합니다.
- `Definition.json`은 팩 1에서 가져오지 않으며, 6단계의 세 시트도 마찬가지입니다.
- 봉고 팩은 봉고 팩 1로 대체됩니다.

## 8단계: 팩 고르기

스킨의 세 폴더 중 하나에 팩이 2개 이상 있으면, 설정의 테마 페이지 맨 앞에 "노트 스킨" 섹션이 표시되고, 그 폴더에 대한 선택지와 그 아래 팩의 미리보기가 나옵니다. 게임은 고른 팩을 즉시 로드하고, 그 선택을 스킨별로 `ThemeSettings.db3`에 기억합니다. 팩이 하나뿐이면 선택지가 나타나지 않고 게임은 그 팩을 사용합니다.

Lua 모듈은 `THEME:GetNotesSkinFile(kind, file)`로 선택된 팩의 파일을 읽습니다([플레이어와 프로필](../api/players.md) 참고). 기본 제공 곡 선택 화면은 이것으로 리플레이 목록의 판정 문자를 그립니다.

```lua
local file = THEME:GetNotesSkinFile("judges", "Judge.png")   -- "5_Game/Judges Skin/2/Judge.png", or ""
if file == "" then file = "5_Game/Judge.png" end              -- a skin without judges packs
```

## 팩이 없는 스킨

`Notes Skin (Taiko)/` 폴더가 없는 스킨은 팩이 생기기 전에 쓰던 파일을 그대로 사용합니다. `Notes.png`(판정 원, 노트 4종, 연타, 풍선), `Swap.png`(KaDon), `Adlib.png`, `Kusu.png`, `FuseRoll.png`, `ChipEffect.png`, `10_Effects/Hit/` 폴더, 그리고 `11_Balloon/`의 풍선 파일입니다. `Notes Skin (Bongo)/` 폴더가 없으면 봉고 노트와 연타는 `Notes_Konga.png`에서 가져오고, `Judges Skin/` 폴더가 없으면 판정은 `5_Game/Judge.png`에서 가져옵니다. 이 파일들에는 파트너 시트나 거대 노트 시트가 없으므로, 파트너 노트와 거대 노트는 6단계의 배율을 적용한 모습을 사용합니다.

## 문제 해결과 참고

- 설정에 선택지가 나오지 않음: 폴더에 팩이 하나뿐이거나, 팩 폴더 이름이 숫자가 아닙니다.
- 팩에 다른 팩의 노트가 나옴: 팩에 파일이 없어 대체가 팩 1의 파일을 가져왔거나, 대소문자를 구분하는 시스템에서 파일 이름의 대소문자가 다릅니다.
- 노트가 잘못된 프레임이나 잘린 그림으로 나옴: `Frames`가 시트의 높이를 셀 높이로 나눈 값과 맞지 않습니다.
- 복사한 팩의 파트너 노트나 거대 노트가 원래 것처럼 보임: 사본에 원본의 `Notes_Partner.png`나 `Notes_Giant.png`가 남아 있습니다.
- 봉고 플레이어에게 노트가 보이지 않음: 선택된 봉고 팩과 봉고 팩 1 모두에 `Notes.png`가 없습니다.
