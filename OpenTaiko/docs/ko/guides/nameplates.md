<!-- guides/nameplates.md -->

# 네임플레이트 추가하기

네임플레이트 칭호는 플레이어의 네임플레이트에 플레이어 이름, 단위 칭호와 함께 표시되는 텍스트 플레이트입니다. 플레이어는 플레이하거나, 구매하거나, 스크립트에서 받아서 칭호를 잠금 해제하고, 마이 룸에서 그중 하나를 장착합니다. 네임플레이트 팩으로 자신만의 칭호를 추가할 수 있습니다. 팩은 `Nameplates.json` 파일이 든 폴더로, `Global/Nameplates/`에 둡니다. 스킨도 자체 팩을 포함할 수 있습니다. 게임은 네임플레이트를 표시하는 모든 화면에서 모든 팩의 네임플레이트를 기본 제공 네임플레이트와 함께 나열합니다.

## 시작하기 전에

- UTF-8로 저장하는 텍스트 편집기.
- 팩 id: 소문자, 숫자, `_`로 이루어지고 다른 팩이 쓰지 않는 짧은 이름. 예를 들어 자신의 이름이나 자신의 채보 팩 이름입니다. 게임은 이를 바탕으로 팩 안 모든 네임플레이트의 id를 만듭니다.
- 게임은 시작할 때와 스킨을 로드할 때 팩을 읽습니다. 변경 사항을 보려면 바꿀 때마다 게임을 재시작하십시오.

## 1단계: 팩 폴더 만들기

`Global/Nameplates/` 안에 폴더를 만듭니다(`Global` 폴더는 게임 옆에 있습니다). 폴더 이름은 자유입니다. 팩에는 필수 파일 하나와 번역용 `Lang/` 폴더(선택 사항, 3단계)가 들어갑니다.

```
Global/
  Nameplates/
    Example Pack/
      Nameplates.json
      Lang/
        fr.json
        ja.json
```

스킨도 같은 방식으로 팩을 포함합니다. 스킨 루트에 `Nameplates/` 폴더를 둡니다(`System/<skin>/Nameplates/Nameplates.json`). 게임은 활성 스킨의 팩만 로드하므로, 다른 스킨이 활성인 동안에는 스킨 팩의 네임플레이트가 사라집니다.

## 2단계: Nameplates.json 작성하기

`Nameplates.json`에는 팩 id와 네임플레이트 목록이 들어갑니다.

- `pack`(string, 필수): 팩 id.
- `name`(object, 선택): 팩의 이름으로, 마이 룸 컴퓨터의 팩 선택 바에 표시됩니다. `{ "strings": { "default": "...", "<code>": "..." } }` 형식으로 쓰며, 팩 선택 바에는 현재 언어의 이름이, 없으면 `default`가 표시됩니다. `name`이 없으면 팩 선택 바에 팩 id가 표시됩니다.
- `nameplates`(array, 필수): 네임플레이트마다 객체 하나이며, 다음 필드를 가집니다.
  - `key`(string, 필수): 팩 안에서 고유한 네임플레이트 이름. 네임플레이트의 전체 id는 `<pack>:<key>`이며, 예를 들어 `example_pack:night_sky`입니다. 세이브 파일이 이 id를 저장하므로, 플레이어가 네임플레이트를 가질 수 있게 된 뒤에는 키를 유지하고, 그 키를 다른 네임플레이트에 절대 주지 마십시오.
  - `title`(string, 필수): 칭호 텍스트. 번역이 없는 모든 언어에서 이것이 표시됩니다.
  - `type`(int, 기본값 0): 플레이트 아트로, 스킨의 네임플레이트 텍스처 안 폴더 번호입니다. 기본 제공 스킨의 폴더는 `Modules/ROActivities/nameplate/Textures/Title/0`부터 `45`까지이며, 폴더가 없는 번호는 플레이트를 그리지 않습니다. 아래 표에 기본 제공 스킨의 플레이트가 나와 있습니다.
  - `rarity`(string, 기본값 `Common`): 희귀도 이름(아래 표 참고). 알 수 없는 이름은 `Common`이 됩니다.
  - `unlock`(object, 선택): 잠금 해제 조건으로, `Unlock.json`과 같은 필드 `condition`, `values`, `type`, `references`를 가집니다(조건 id는 [잠금 해제 요소 레퍼런스](unlockables-reference.md) 참고). `unlock`이 없으면 모든 플레이어가 처음부터 네임플레이트를 소유합니다.

희귀도 값(별 수는 기본 제공 스킨 기준입니다):

| 희귀도 | 플레이트의 별 | 알림 등급 |
| --- | --- | --- |
| <span class="rarity rarity-poor">Poor</span> | 0 | 0 |
| <span class="rarity rarity-common">Common</span> | 0 | 0 |
| <span class="rarity rarity-uncommon">Uncommon</span> | 0 | 1 |
| <span class="rarity rarity-rare">Rare</span> | 1 | 2 |
| <span class="rarity rarity-epic">Epic</span> | 2 | 3 |
| <span class="rarity rarity-legendary">Legendary</span> | 3 | 4 |
| <span class="rarity rarity-mythical">Mythical</span> | 4 | 4 |

기본 제공 스킨 Open-World Memories의 플레이트 타입(`type`)입니다. 마이 룸의 컴퓨터는 네임플레이트를 타입별로 묶어, 스킨의 `Modules/Stages/myroom/lang/<code>/nameplate_types.json`에 있는 섹션 이름 아래에 표시합니다. 그곳에 이름이 없는 타입은 "플레이트 타입"과 번호로 표시되며, 타입 10과 38의 섹션은 플레이어가 그 타입의 네임플레이트를 소유할 때까지 "???"로 표시됩니다.

| 타입 | 이름 | 효과 |
| --- | --- | --- |
| 0 | 나무 |  |
| 1 | 구리 | 반짝임 |
| 2 | 자수정 | 반짝임 |
| 3 | 자연 | 반짝임 |
| 4 | 빨강 | 반짝임 |
| 5 | 파랑 | 반짝임 |
| 6 | 재 | 반짝임 |
| 7 | 무지개 | 애니메이션 |
| 8 | 우주 |  |
| 9 | 다이아몬드 |  |
| 10 | 비참 |  |
| 11 | 금 |  |
| 12 | 진주 |  |
| 13 | 플래티넘 |  |
| 14 | Kohaku(캐릭터 플레이트) |  |
| 15 | Dashy |  |
| 16 | 파랑 |  |
| 17 | 빨강 |  |
| 18 | 하늘보라 |  |
| 19 | AI |  |
| 20 | 하늘 |  |
| 21 | 노랑 |  |
| 22 | 크림 |  |
| 23 | 바다 |  |
| 24 | 초록 |  |
| 25 | 챕터 I |  |
| 26 | 챕터 II |  |
| 27 | 챕터 III |  |
| 28 | OpTk 라이트 |  |
| 29 | OpTk 다크 |  |
| 30 | NEP0 |  |
| 31 | 밤 |  |
| 32 | 챕터 IV |  |
| 33 | 클리어 실적 |  |
| 34 | 풀콤보 실적 |  |
| 35 | 퍼펙트 실적 |  |
| 36 | 템플릿 |  |
| 37 | 챕터 V |  |
| 38 | V-ENTURE |  |
| 39 | 클리어 기록 |  |
| 40 | 풀콤보 기록 |  |
| 41 | 퍼펙트 기록 |  |
| 42 | Ryuto Setsujin |  |
| 43 | 해돋이 |  |
| 44 | 챕터 VI |  |
| 45 | 새로운 세계 |  |

게임은 플레이마다 결과 화면에서 잠금 해제 조건을 검사합니다. 플레이어가 조건을 충족하면 게임은 네임플레이트를 잠금 해제하고 알림을 표시합니다. 코인 조건(`ch`, `cs`, `cm`)은 구매로만 잠금 해제됩니다. 기본 제공 스킨에서는 플레이어가 마이 룸의 컴퓨터에서 네임플레이트를 구매합니다. 마이 룸의 컴퓨터는 잠긴 Mythical 네임플레이트를 플레이어가 소유할 때까지 목록에 표시하지 않습니다.

```json
{
  "pack": "example_pack",
  "name": { "strings": { "default": "Example Pack", "fr": "Pack d'exemple" } },
  "nameplates": [
    {
      "key": "night_sky",
      "title": "Night Sky Walker",
      "type": 8,
      "rarity": "Rare",
      "unlock": { "condition": "tp", "values": [50] }
    },
    {
      "key": "hello",
      "title": "Hello There"
    }
  ]
}
```

## 3단계: 번역 추가하기

번역은 선택 사항입니다. 팩의 `Lang/` 폴더에 있는 각 파일은 한 언어의 칭호를 담으며, 팩의 키(팩 id 제외)로 네임플레이트마다 한 줄씩 씁니다. 파일 이름은 언어 코드입니다.

| 파일 | 언어 |
| --- | --- |
| `de.json` | 독일어 |
| `en.json` | 영어 |
| `es.json` | 스페인어 |
| `fr.json` | 프랑스어 |
| `ja.json` | 일본어 |
| `ko.json` | 한국어 |
| `nl.json` | 네덜란드어 |
| `ru.json` | 러시아어 |
| `zh.json` | 중국어 |

```json
{
  "night_sky": "Promeneur du ciel nocturne",
  "hello": "Bonjour"
}
```

언어 파일에 없는 네임플레이트는 그 언어에서 `title`을 표시합니다.

## 4단계: 게임에서 테스트하기

게임을 시작하고 마이 룸의 컴퓨터를 여십시오. 네임플레이트 탭을 열고 목록 위의 팩 선택 바에서 자신의 팩을 고르십시오. 목록에 그 팩의 네임플레이트가 나타나며, 잠긴 것은 잠금 해제 힌트와 함께 표시됩니다. 하나를 장착하고 플레이어의 네임플레이트에서 칭호, 플레이트, 별을 확인하십시오. 번역을 확인하려면 게임 언어를 바꾸십시오.

게임은 읽을 수 없는 팩을 건너뛰고 그 팩의 이름을 담은 오류 알림을 표시합니다. 유효한 `pack` id가 없는 팩이나 게임이 이미 로드한 팩의 id를 가진 팩은 읽을 수 없는 것으로 취급됩니다. `key`나 `title`이 없거나 팩 안에 이미 있는 키를 가진 네임플레이트는 게임이 그 네임플레이트만 건너뛰고 이유를 `OpenTaiko.log`에 기록합니다.

## 스크립트에서 네임플레이트 다루기

Lua 스크립트도 같은 id를 사용합니다. 네임플레이트 id를 받는 모든 메서드는 전체 id를 문자열로 받으며, 기본 제공 네임플레이트는 번호로도 지정할 수 있습니다. 스크립트는 플레이로는 절대 잠금 해제되지 않는 `ig` 조건의 네임플레이트를 줄 수 있습니다.

```lua
local save = GetSaveFile(0)
local id = "example_pack:night_sky"
if not save:IsNameplateUnlocked(id) then
    save:UnlockNameplate(id)
end
save:ChangeNameplate(id)
```

UnlockNameplate는 알림을 표시하지 않으므로, 알림이 필요한 스크립트는 직접 표시합니다. 네임플레이트 메서드는 [플레이어와 프로필](../api/players.md)을 참고하십시오.

## 예시

아래 팩은 흔한 패턴을 보여 줍니다. id, 칭호, 숫자는 자신의 것으로 바꾸십시오.

**무료 네임플레이트.** `unlock`이 없으면 모든 플레이어가 네임플레이트를 소유하고 바로 장착할 수 있습니다.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "welcome", "title": "Welcome Aboard", "type": 0, "rarity": "Common" }
  ]
}
```

**플레이로 잠금 해제(`tp`).** 네임플레이트는 100회 플레이 후 잠금 해제됩니다. `ap`(AI 배틀 플레이 횟수), `aw`(AI 배틀 승리 횟수), `ce`(획득한 총 코인)는 같은 단일 값 레이아웃을 사용합니다.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "regular", "title": "Regular Drummer", "type": 3, "rarity": "Uncommon",
      "unlock": { "condition": "tp", "values": [100] } }
  ]
}
```

**자신의 채보 팩에 있는 곡 클리어(`sp`).** 채보 팩은 자신의 곡에 보상을 걸 수 있습니다. 참조는 곡 `uniqueID.json`의 `id` 필드이며, `[3, 3]`은 Extreme(3)에서의 풀 콤보(3)를 요구합니다.

```json
{
  "pack": "example_charts",
  "nameplates": [
    { "key": "storm_fc", "title": "Storm Chaser", "type": 33, "rarity": "Epic",
      "unlock": { "condition": "sp", "values": [3, 3],
                  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"] } }
  ]
}
```

**코인으로 구매(`ch`).** 가격이 유일한 값입니다. 기본 제공 스킨에서는 플레이어가 마이 룸의 컴퓨터에서 네임플레이트를 구매합니다.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "golden", "title": "Golden Touch", "type": 11, "rarity": "Rare",
      "unlock": { "condition": "ch", "values": [800] } }
  ]
}
```

**스크립트만 주는 보상(`ig`).** 플레이로는 `ig` 네임플레이트가 절대 잠금 해제되지 않으므로, 스크립트가 UnlockNameplate로 줍니다(스크립트에서 네임플레이트 다루기 참고). 그때까지 마이 룸의 컴퓨터는 이 네임플레이트에 조건이 유효하지 않다는 일반 힌트를 표시합니다.

```json
{
  "pack": "example_story",
  "nameplates": [
    { "key": "finale", "title": "Curtain Call", "type": 19, "rarity": "Legendary",
      "unlock": { "condition": "ig", "values": [] } }
  ]
}
```

**번역이 있는 스킨 팩.** 스킨은 팩을 `System/<skin>/Nameplates/`에, 번역을 `System/<skin>/Nameplates/Lang/`에 둡니다.

```json
{
  "pack": "example_skin",
  "nameplates": [
    { "key": "visitor", "title": "First Visit", "type": 22, "rarity": "Common",
      "unlock": { "condition": "tp", "values": [1] } }
  ]
}
```

```json
{
  "visitor": "初めての訪問"
}
```

## 문제 해결과 참고

- 팩이 나타나지 않음: 폴더가 `Global/Nameplates/` 바로 아래에 없거나, 파일 이름이 `Nameplates.json`이 아니거나, `pack` id가 없거나 소문자, 숫자, `_` 외의 문자를 사용하거나, 다른 팩이 이미 같은 id를 사용합니다. 오류 알림에 그 팩의 이름이 나옵니다.
- 네임플레이트가 목록에 없음: `key`나 `title`이 없거나, 키가 앞의 항목과 중복됩니다. `OpenTaiko.log`에 그 항목의 이름이 나옵니다.
- 번역이 표시되지 않음: `Lang` 파일의 키는 팩 id를 뺀 팩의 키여야 하며(`example_pack:night_sky`라면 `night_sky`), 파일 이름은 언어 코드여야 합니다.
- 플레이트에 아트가 없음: 스킨에 그 `type`의 폴더가 없습니다.
- 온라인에서 다른 플레이어는 같은 팩을 가지고 있을 때만 팩 네임플레이트의 칭호를 봅니다. 그렇지 않으면 그 플레이어의 네임플레이트를 칭호 없이 봅니다.
- 팩을 제거하면 그 팩의 네임플레이트를 장착했던 플레이어는 팩이 돌아올 때까지 칭호 없이 표시됩니다. 잠금 해제 기록은 세이브 파일에 남습니다.
- 게임의 JSON 파서는 주석과 후행 쉼표를 허용하고, 엄격한 검사기는 거부합니다.
