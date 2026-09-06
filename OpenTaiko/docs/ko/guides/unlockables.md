<!-- guides/unlockables.md -->

# 커스텀 채보에 잠금 해제 조건 추가하기

커스텀 곡은 곡 폴더의 `.tja`와 `uniqueID.json` 옆에 `Unlock.json` 파일을 두어 조건(코인 가격, 다른 곡 클리어, 총 플레이 횟수, 스토리 플래그 등) 뒤에 잠글 수 있습니다. 게임은 곡 목록을 만들 때 그 파일을 읽고 플레이어가 조건을 충족할 때까지 곡을 잠가 둡니다. 조건은 개별 곡에 붙습니다. `box.def`에는 잠금 해제 키가 없으므로 장르 폴더는 이 방법으로 잠글 수 없습니다.

## 시작하기 전에

- 이미 곡 목록에 나타나는 커스텀 채보: `Songs/` 아래에 `.tja`가 있고, 게임이 한 번 스캔한 뒤에는 `uniqueID.json`도 있는 폴더.
- UTF-8로 저장하는 텍스트 편집기.
- 다른 곡을 참조하는 조건의 경우: 참조하는 각 곡의 `uniqueID.json`에 있는 `id` 값.
- 게임은 잠금 해제 진행을 세이브 파일마다 저장하므로, 곡이 잠금 해제되는 것을 보려면 게임 안에서 조건을 충족해야 합니다. 게임 설정의 `Ignore Song Unlockables` 옵션은 테스트하는 동안 모든 곡을 잠금 해제된 것으로 취급합니다.

## 1단계: Unlock.json 파일 만들기

곡 폴더에 `Unlock.json`을 만듭니다. 모든 필드는 선택 사항이며 기본값이 있습니다.

- `hidden_index`(int, 기본값 0): 곡 목록이 잠긴 곡을 표시하는 방식(아래 표 참고). 게임은 값을 0-3으로 제한합니다.
- `rarity`(string, 기본값 `Common`): 희귀도 이름(아래 표 참고). 희귀도 표시 색상과 잠금 해제 알림 등급을 정합니다. 빈 값은 `Common`이 됩니다.
- `condition`(string, 기본값 `ch`): 조건 id(2단계).
- `values`(int 배열, 기본값 `[100]`): 조건의 숫자 매개변수.
- `type`(string, 기본값 `me`): 값 기반 조건에 사용하는 비교: `l` 미만, `le` 이하, `e` 같음, `me` 이상, `m` 초과, `d` 다름. 코인 조건은 항상 `me`를 사용합니다.
- `references`(string 배열, 기본값 `[""]`): 조건에 따라 곡 id, 장르 이름, 채보 제작자 이름, 플래그 이름.
- `custom_unlock_text`(object, 선택): 생성되는 힌트 텍스트를 대체합니다. 지역화 객체 `{ "strings": { "default": "...", "ja": "..." } }`. 게임은 활성 언어의 키, 그다음 `default`를 사용하며, 둘 다 없으면 생성된 텍스트를 표시합니다.

키는 나열된 그대로 밑줄이 있는 소문자입니다.

hidden_index 값:

| 값 | 곡 목록에서의 표시 |
| --- | --- |
| 0 | 잠금 아이콘과 함께 표시. 오디오 미리 듣기 재생 |
| 1 | 회색 처리. 미리 듣기 없음 |
| 2 | 회색 처리. 제목과 미리 보기 이미지를 가림 |
| 3 | 잠금 해제까지 숨김 |

희귀도 값:

| 희귀도 | 알림 등급 |
| --- | --- |
| <span class="rarity rarity-poor">Poor</span> | 0 |
| <span class="rarity rarity-common">Common</span> | 0 |
| <span class="rarity rarity-uncommon">Uncommon</span> | 1 |
| <span class="rarity rarity-rare">Rare</span> | 2 |
| <span class="rarity rarity-epic">Epic</span> | 3 |
| <span class="rarity rarity-legendary">Legendary</span> | 4 |
| <span class="rarity rarity-mythical">Mythical</span> | 4 |

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "cm",
  "values": [500],
  "type": "me",
  "references": [""],
  "custom_unlock_text": {
    "strings": {
      "default": "Buy this song for 500 coins.",
      "ja": "500コインで解放できます。"
    }
  }
}
```

## 2단계: 조건 고르기

| Id | 의미 | `values` | `references` |
|----|---------|----------|--------------|
| `ch`, `cs`, `cm` | 코인 구매. 게임은 세 id를 동일하게 평가합니다(코인 가격, `type`은 `me`로 강제, 자동으로 부여되지 않음). `cm`은 곡용으로 의도된 id입니다. 스킨 스크립트는 id를 읽어 구매를 어디서 제공할지 정하는 데 쓸 수 있습니다. | `[price]` | 사용 안 함 |
| `ce` | 세이브 파일 생성 이후 획득한 총 코인 | `[coins]` | 사용 안 함 |
| `tp` | 총 플레이 횟수 | `[plays]` | 사용 안 함 |
| `ap` | AI 배틀 플레이 횟수 | `[plays]` | 사용 안 함 |
| `aw` | AI 배틀 승리 횟수 | `[wins]` | 사용 안 함 |
| `sd` | 클리어 상태에 도달한 서로 다른 채보 수 | `[chart count, clear status]` | 사용 안 함 |
| `dp` | 클리어 상태에 도달한 특정 난이도의 채보 수 | `[difficulty, clear status, chart count]` | 사용 안 함 |
| `lp` | 클리어 상태에 도달한 특정 별 레벨의 채보 수 | `[level, clear status, chart count]` | 사용 안 함 |
| `sp` | 클리어 상태에 도달한 특정 곡 | 곡마다 `[difficulty, clear status]`(`-1` = 아무 난이도) | 쌍마다 곡 id 하나 |
| `sg` | 클리어 상태에 도달한 지정 장르 안의 곡 | 장르마다 `[song count, clear status]` | 쌍마다 장르 이름 하나 |
| `sc` | 클리어 상태에 도달한 지정 채보 제작자의 채보 | 제작자마다 `[chart count, clear status]` | 쌍마다 제작자 이름 하나 |
| `gt` | 전역 트리거(스크립트가 설정하는 세이브 파일의 이름 있는 on/off 플래그) | ON이면 `[1]`, OFF이면 `[0]` | `[trigger name]` |
| `gc` | 전역 카운터(스크립트가 설정하는 세이브 파일의 이름 있는 숫자) | `type`으로 비교하는 `[value]` | `[counter name]` |
| `ig` | 획득 불가: 절대 잠금 해제되지 않음 | 없음 | 없음 |
| `andcomb` | 플레이어가 모든 자식 조건을 충족해야 함(조합 예시 참고) | `[]` | 항목마다 자식 조건 하나, JSON 문자열로 |
| `orcomb` | 플레이어가 자식 조건 중 하나 이상을 충족해야 함(조합 예시 참고) | `[]` | 항목마다 자식 조건 하나, JSON 문자열로 |

클리어 상태 값(상태 요구 사항은 이 상태 이상을 뜻합니다):

| 값 | 클리어 상태 |
| --- | --- |
| 0 | 플레이함 |
| 1 | 어시스트 클리어 |
| 2 | 클리어 |
| 3 | 풀 콤보 |
| 4 | 올 퍼펙트 |

난이도 값:

| 값 | 난이도 |
| --- | --- |
| 0 | Easy |
| 1 | Normal |
| 2 | Hard |
| 3 | Extreme |
| 4 | Extra Extreme |

`dp`에서 난이도 3은 Extra Extreme 채보도 셉니다. `dp`와 `lp`는 일반 채보만 세며 단위와 타워 채보는 건너뜁니다. `sp`는 아무 난이도를 뜻하는 `-1`을 허용합니다.

게임은 값 개수를 검사합니다. `ch`/`cs`/`cm`/`ce`/`tp`/`ap`/`aw`/`gt`/`gc`는 정확히 값 1개, `sd`는 정확히 2개, `dp`/`lp`는 정확히 3개, `sp`/`sg`/`sc`는 참조마다 값 2개와 쌍 수만큼의 참조가 필요합니다. 개수가 틀리면 조건은 게임 내 오류 메시지와 함께 실패합니다.

코인 조건은 이를 제공하는 화면에서의 명시적 구매만이 잠금 해제합니다(기본 제공 스킨은 어떤 코인 id를 쓰든 곡 선택에서 잠긴 곡의 구매를 제공합니다). 다른 모든 조건은 게임이 플레이마다 결과 화면에서 검사합니다. 플레이어가 조건을 충족하면 게임은 곡 id를 세이브 파일에 추가하고 알림을 표시합니다.

## 3단계: 게임에서 테스트하기

게임을 시작하고 곡 선택을 여십시오. `hidden_index`에 따라 곡은 잠금을 표시하거나, 회색 처리되거나, 가려지거나, 없습니다. 구매하거나 조건을 충족한 뒤 재시작 후에도 잠금 해제 상태가 유지되는지 확인하십시오. 반복 작업 중에는 게임 설정에서 `Ignore Song Unlockables`를 켜서 모든 잠금을 우회하십시오.

## 예시

기본 제공 곡이 사용하는 조건 패턴을 `Unlock.json` 파일로 적은 것입니다. id, 이름, 숫자는 자신의 것으로 바꾸십시오.

**코인으로 곡 구매(`cm`).** 가장 흔한 패턴입니다. 가격이 유일한 값이며 게임은 `type`을 무시합니다. `hidden_index` 0은 플레이어가 찾아서 구매할 수 있도록 곡을 보이게 유지합니다. 생성되는 힌트가 이미 가격을 알려 주므로 `custom_unlock_text`는 필요 없습니다.

```json
{
  "hidden_index": 0,
  "rarity": "Uncommon",
  "condition": "cm",
  "values": [500]
}
```

**총 플레이 횟수(`tp`).** 기본 제공 챕터는 플레이 횟수를 점점 올려 가며(10, 15, 20 등) 곡을 하나씩 차례로 열어, 플레이어가 언제나 다음 곡에 닿을 수 있게 합니다. `ap`(AI 배틀 플레이 횟수), `aw`(AI 배틀 승리 횟수), `ce`(획득한 총 코인)는 같은 단일 값 레이아웃을 사용합니다.

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "tp",
  "values": [50]
}
```

**특정 곡 클리어(`sp`).** 대부분의 기본 제공 곡이 속편이나 리믹스에 사용하는 패턴입니다. 난이도 `-1`은 아무 난이도의 클리어를 허용합니다. id는 참조하는 곡 `uniqueID.json`의 `id` 필드입니다(게임은 id가 없는 곡을 처음 스캔할 때 64자 id를 생성하며, 기본 제공 곡은 손으로 쓴 id를 가질 수 있습니다). 예는 플레이어가 참조한 곡을 클리어한 뒤 잠금 해제됩니다.

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "sp",
  "values": [-1, 2],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"]
}
```

**여러 곡 풀 콤보(`sp`).** 곡을 추가할 때마다 값 쌍 하나와 참조 하나가 더해지며, 플레이어는 참조한 모든 곡을 충족해야 합니다. 예는 두 곡의 풀 콤보(3)를 요구합니다.

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sp",
  "values": [-1, 3, -1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU", "4aJ2I19XyEG2cEA9tlmdnZSz2H43OKsVBPLi52UfRhSjDGNgTGGqhmqkbWfPDoyw"]
}
```

**장르의 곡 클리어(`sg`).** 각 항목은 `[song count, clear status]`이고 장르 이름은 `references`에 둡니다. 장르는 플레이어가 곡을 플레이할 때 게임이 기록하는 것입니다. 상위 폴더 `box.def`의 `#GENRE`, 또는 폴더에 없으면 채보 자체의 `#GENRE`입니다. 기본 제공 챕터는 이를 메들리에 사용합니다. 예는 플레이어가 그 장르의 서로 다른 곡 10곡을 클리어한 뒤 잠금 해제됩니다.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sg",
  "values": [10, 2],
  "references": ["OpenTaiko Chapter II"]
}
```

**채보 제작자의 채보 클리어(`sc`).** `#NOTESDESIGNER` 이름을 참조로 하는 같은 레이아웃입니다. 예는 한 제작자의 클리어한 채보 5개를 요구합니다. 두 번째 제작자는 값 쌍과 참조를 하나씩 더합니다.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sc",
  "values": [5, 2],
  "references": ["bol"]
}
```

**서로 다른 채보 일정 수 클리어(`sd`).** 세이브 파일이 지정한 상태 이상으로 가진 모든 채보를 셉니다. 예는 풀 콤보 10개를 요구합니다.

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sd",
  "values": [10, 3]
}
```

**특정 난이도나 별 레벨의 채보 클리어(`dp` / `lp`).** `dp`는 한 난이도의 채보를, `lp`는 한 별 레벨의 채보를 셉니다. 첫 예는 클리어한 Extreme 채보 20개를, 두 번째 예는 클리어한 별 레벨 7 채보 1개를 요구합니다.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "dp",
  "values": [3, 2, 20]
}
```

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "lp",
  "values": [7, 2, 1]
}
```

**두 조건 중 하나(`orcomb`).** `values`는 비어 있고 `references`의 각 항목은 JSON 문자열로 쓴 완전한 자식 조건(`condition`, `type`, `values`, `references`)입니다. 기본 제공 곡은 이를 지름길을 제공하는 데 사용합니다. 곡을 충분히 플레이하거나, 아니면 지불하는 것입니다. `orcomb`는 구매 가격을 매길 때 충족된 가장 싼 분기를 사용합니다.

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "orcomb",
  "values": [],
  "references": [
    "{\"condition\": \"tp\", \"type\": \"me\", \"values\": [100]}",
    "{\"condition\": \"cm\", \"type\": \"me\", \"values\": [200]}"
  ]
}
```

**여러 조건 모두(`andcomb`).** 같은 레이아웃이며, 플레이어는 모든 자식을 충족해야 합니다. 자식은 스스로 조합일 수 있고, `andcomb`는 조합 안의 코인 가격을 합산합니다. 예는 한 곡의 클리어와 아무 별 레벨 7 채보의 클리어를 요구합니다.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "andcomb",
  "values": [],
  "references": [
    "{\"condition\": \"sp\", \"type\": \"me\", \"values\": [-1, 2], \"references\": [\"4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU\"]}",
    "{\"condition\": \"lp\", \"type\": \"me\", \"values\": [7, 2, 1], \"references\": [\"\"]}"
  ]
}
```

**커스텀 힌트가 있는 스토리 플래그(`gt`).** `gt`는 세이브 파일에서 이름 있는 on/off 플래그를 읽습니다. 플래그는 Lua 스크립트(스토리 장면, 컷신)가 설정하므로, 자신이 제어하는 스크립트가 플래그를 설정할 때만 `gt`를 사용하십시오. `values`는 ON이면 `[1]`, OFF이면 `[0]`이고 `references`는 플래그 이름을 담습니다. 스토리 잠금 해제에는 자명한 생성 힌트가 없으므로 여기서 `custom_unlock_text`가 유용합니다. 어떤 언어 키든 제공할 수 있으며 `default`가 대체값입니다.

```json
{
  "hidden_index": 3,
  "rarity": "Legendary",
  "condition": "gt",
  "values": [1],
  "references": ["story_done"],
  "custom_unlock_text": {
    "strings": {
      "default": "Finish the story to unlock this song.",
      "ja": "ストーリーをクリアするとこの曲が解放されます。"
    }
  }
}
```

**수수께끼가 있는 비밀 곡.** 기본 제공 비밀 곡은 높은 `hidden_index`(제목과 미리 보기가 가려지거나, 잠금 해제까지 곡이 없는 상태)와 조건을 말하는 대신 암시하는 `custom_unlock_text`를 조합합니다. 그 아래에는 어떤 조건이든 쓸 수 있습니다. 예는 풀 콤보 요구 사항을 수수께끼 뒤에 숨깁니다.

```json
{
  "hidden_index": 2,
  "rarity": "Epic",
  "condition": "sp",
  "values": [-1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"],
  "custom_unlock_text": {
    "strings": {
      "default": "Perfect the storm that came before this one."
    }
  }
}
```

## 문제 해결과 참고

- 곡이 잠기지 않음: 파일 이름이 `Unlock.json`이 아니거나, `.tja`와 `uniqueID.json`이 있는 폴더에 없거나, `Ignore Song Unlockables`가 켜져 있습니다.
- 힌트에 조건이 유효하지 않다고 나옴: 값 개수가 조건과 맞지 않거나(2단계 참고), `sp`/`sg`/`sc`의 참조 수가 값 쌍의 수와 다릅니다.
- 키가 효과가 없음: 키는 `hidden_index`, `rarity`, `condition`, `values`, `type`, `references`, `custom_unlock_text`이며 모두 소문자입니다. 게임은 그 외를 무시하고 기본값을 적용합니다.
- `sp`가 잠금 해제되지 않음: 참조는 대상 곡의 `uniqueID.json` id여야 하며(제목이나 파일 이름은 절대 일치하지 않음), 플레이어가 그 난이도와 상태에 도달할 수 있어야 합니다(상태 3은 풀 콤보이므로 플레이어가 클리어만 한 채보는 셈에 들지 않음).
- `sg`가 잠금 해제되지 않음: 장르 이름이 플레이한 곡에 대해 게임이 기록한 장르(폴더 `box.def`의 `#GENRE`, 또는 폴더 장르가 없으면 채보 `#GENRE`)와 일치해야 합니다.
- `gt`/`gc`가 잠금 해제되지 않음: 아무것도 지정한 플래그를 설정하지 않습니다. 채보는 스스로 설정할 수 없습니다.
- 플레이 중에는 아무것도 `ig`를 잠금 해제하지 않습니다. 다른 시스템이 부여하는 콘텐츠에만 사용하십시오.
- `custom_unlock_text`는 `{ "strings": { ... } }` 객체여야 합니다. 문자열만 넣으면 동작하지 않습니다.
- 기본 제공 `Unlock.json` 파일에는 후행 쉼표가 있습니다. 게임의 JSON 파서는 이를 허용하고, 엄격한 검사기는 거부합니다.
