<!-- api/players.md -->

# 플레이어와 프로필

세이브 파일, 네임플레이트, 캐릭터, 푸치캬라, 플레이 상태, 테마, 현재 언어.

이 페이지에서 플레이어 인덱스는 1부터 시작하는 THEME:GetThemeSettingForPlayer를 제외하고 모두 0부터 시작합니다(0에서 4). 읽기 전용 모듈(ROActivity와 배경)은 쓰기 메서드가 오류를 기록하고 아무것도 하지 않는 세이브 파일 핸들을 받습니다. 그 외의 모든 것은 모든 모듈 종류에서 같게 동작합니다.

## 세이브 파일

### GetSaveFile

플레이어 슬롯의 세이브 파일 핸들을 반환하는 전역 함수입니다.

<div class="callout warn">
일반 함수로 호출하십시오(`GetSaveFile(0)`). 범위 밖 인덱스는 오류를 기록하고 nil을 반환합니다. 호출마다 실시간 데이터를 읽는 새 핸들이 생성되므로 캐시하거나 해제할 것이 없습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `GetSaveFile(player)  -> saveFile` | 0부터 시작하는 플레이어 슬롯의 세이브 파일 핸들을 반환하며, 인덱스가 범위를 벗어나면 nil. |

### 세이브 파일 핸들

플레이어 한 명의 프로필: 이름, 코인, 잠금 해제한 항목, 트리거와 카운터, 클리어 통계, 장착한 캐릭터, 푸치캬라, 네임플레이트, 단위 칭호입니다.

<div class="callout warn">
프로퍼티는 점 문법으로 읽으십시오(sf.Name, sf.Coins). 쓰기 메서드는 즉시 저장합니다. 읽기 전용 모듈은 다음을 차단합니다: SpendCoins, EarnCoins, UnlockNameplate, UnlockSong, SelectedHitsounds 대입, SetGlobalTrigger, SetGlobalCounter, ChangeCharacter(false를 반환), UnlockPuchichara, ChangePuchichara, UnlockCharacter, ChangeDan, ChangeName, ChangeNameplate.
</div>

| 메서드 | 설명 |
| --- | --- |
| `sf.Name  -> string` | 플레이어의 표시 이름. |
| `sf.SaveId  -> integer` | 이 세이브의 숫자 데이터베이스 id. |
| `sf.SaveUID  -> string` | 이 세이브의 고유 문자열 id. |
| `sf.NameplateInfo  -> nameplateInfo` | 장착한 네임플레이트(네임플레이트 정보 핸들 참고). 저장된 id를 알 수 없으면 기본 초보자 네임플레이트. |
| `sf.DanplateInfo  -> danplateInfo` | 현재 단위 칭호(단위 플레이트 정보 핸들 참고). |
| `sf.TotalPlaycount  -> integer` | 이 세이브의 총 플레이 수. |
| `sf.AIBattlePlaycount  -> integer` | AI 배틀 플레이 수. |
| `sf.AIBattleWins  -> integer` | AI 배틀 승리 수. |
| `sf.Coins  -> integer` | 현재 코인 잔액. |
| `sf.TotalEarnedCoins  -> integer` | 세이브 생성 이후 획득한 총 코인. |
| `sf:SpendCoins(price)  -> nil` | 코인을 차감하고(잔액은 0 아래로 내려가지 않음) 저장합니다. |
| `sf:EarnCoins(amount)  -> nil` | 잔액과 총 획득량에 코인을 더하고 저장합니다. |
| `sf:IsNameplateUnlocked(id)  -> bool` | 이 id의 네임플레이트가 잠금 해제되었는지 여부. |
| `sf:UnlockNameplate(id)  -> nil` | 네임플레이트를 잠금 해제하고 저장합니다(이미 해제되었으면 no-op). |
| `sf:IsSongUnlocked(uniqueId)  -> bool` | 이 고유 id의 곡이 잠금 해제되었는지 여부. |
| `sf:UnlockSong(uniqueId)  -> nil` | 곡을 잠금 해제하고 저장합니다(이미 해제되었으면 no-op). |
| `sf.SelectedHitsounds  -> string` | 선택한 히트사운드 세트의 폴더 이름. 다른 이름을 대입하면 저장하고 플레이어의 히트사운드를 다시 로드합니다. |
| `sf:GetGlobalTrigger(name)  -> bool` | 이름이 지정된 불리언 트리거를 읽습니다. |
| `sf:GetGlobalCounter(name)  -> number` | 이름이 지정된 숫자 카운터를 읽습니다. |
| `sf:SetGlobalTrigger(name, value)  -> nil` | 이름이 지정된 불리언 트리거를 설정합니다. |
| `sf:SetGlobalCounter(name, value)  -> nil` | 이름이 지정된 숫자 카운터를 설정합니다. |
| `sf:GetClearStatusCount(difficulty, clearStatus)  -> integer` | 최고 클리어 상태가 정확히 clearStatus(0 없음, 1 어시스트, 2 클리어, 3 풀 콤보, 4 퍼펙트)인 난이도(0 Easy에서 4 Extra Extreme)의 채보 수. 범위 밖 인자는 0. |
| `sf:GetDanBestPlay(node)  -> danBestPlay` | 단위 곡 노드의 모드 없는 최고 플레이(단위 최고 플레이 핸들 참고). 없으면 HasRecord가 false인 핸들. |
| `sf:GetCharacter()  -> character` | 이 슬롯의 플레이어 바인딩 캐릭터 핸들(캐릭터 핸들 참고). |
| `sf.CharacterName  -> string` | 장착한 캐릭터의 폴더 이름. |
| `sf:ChangeCharacter(folderName)  -> bool` | 이 폴더 이름의 캐릭터를 장착합니다. 캐릭터가 장착되었거나 이미 활성이었으면 true, 이 폴더 이름을 가진 로드된 캐릭터가 없으면 false를 반환합니다. |
| `sf:GetPuchichara()  -> puchichara` | 장착한 푸치캬라(푸치캬라 핸들 참고). 해석할 수 없으면 nil. |
| `sf:IsPuchicharaUnlocked(folderName)  -> bool` | 이 폴더 이름의 푸치캬라가 잠금 해제되었는지 여부. |
| `sf:UnlockPuchichara(folderName)  -> nil` | 푸치캬라를 잠금 해제하고 저장합니다(이미 해제되었으면 no-op). |
| `sf:ChangePuchichara(folderName)  -> nil` | 이 폴더 이름의 푸치캬라를 장착하고 저장합니다. 이 메서드는 이름을 검증하지 않습니다. |
| `sf:IsCharacterUnlocked(folderName)  -> bool` | 캐릭터가 잠금 해제되었는지 여부. 장착한 캐릭터는 항상 잠금 해제된 것으로 셉니다. |
| `sf:UnlockCharacter(folderName)  -> nil` | 캐릭터를 잠금 해제하고 저장합니다(이미 해제되었으면 no-op). |
| `sf.DanTitleCount  -> integer` | 기본 칭호를 포함해 사용 가능한 단위 칭호 수(항상 1 이상). |
| `sf:GetDanTitleByIndex(index)  -> danTitleEntry` | 0부터 시작하는 인덱스의 단위 칭호(단위 칭호 항목 핸들 참고). 인덱스 0은 기본 칭호. 범위를 벗어나면 nil. |
| `sf.SelectedDan  -> string` | 활성 단위 칭호의 텍스트. |
| `sf:ChangeDan(title)  -> nil` | 지정한 칭호를 활성화하고, 플레이어가 획득한 것이면 금색과 클리어 상태 플래그를 복사하며, 네임플레이트를 갱신하고 저장합니다. |
| `sf:ChangeName(name)  -> nil` | 표시 이름을 바꾸고, 네임플레이트를 갱신하고 저장합니다. 이 메서드는 빈 이름이나 바뀌지 않은 이름을 무시합니다. |
| `sf:ChangeNameplate(id)  -> nil` | 이 id의 네임플레이트를 장착하고, 네임플레이트를 갱신하고 저장합니다. 데이터베이스에 없는 id는 캐시된 칭호 텍스트를 비웁니다. |

```lua
local save = GetSaveFile(0)
local entry = CHARACTERLIST:GetByName("Aoi")
if entry and not save:IsCharacterUnlocked(entry.FolderName) then
    local cond = entry.UnlockCondition
    if cond:IsUnlockable(0) and cond:GetCoinPrice() <= save.Coins then
        save:SpendCoins(cond:GetCoinPrice())
        save:UnlockCharacter(entry.FolderName)
    end
end
```

## 네임플레이트와 단위 칭호

### NAMEPLATE

칭호 플레이트, 단위 플레이트, 완전한 플레이어 네임플레이트를 그립니다.

<div class="callout warn">
스킨의 nameplate ROActivity(Modules/ROActivities/nameplate)가 그리기를 수행하고 아트워크와 레이아웃을 정의합니다. opacity는 0에서 255입니다. 텍스트 매개변수는 텍스트 객체로 렌더링한 텍스처를 받습니다(그래픽과 텍스트 참고). rarity는 인덱스로 0 Poor, 1 Common, 2 Uncommon, 3 Rare, 4 Epic, 5 Legendary, 6 Mythical입니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `NAMEPLATE:DrawTitlePlate(x, y, opacity, type, text, rarity, nameplateId)  -> nil` | 지정한 표시 타입, 미리 렌더링한 칭호 텍스처, 희귀도 인덱스, 네임플레이트 id로 칭호 플레이트를 그립니다. |
| `NAMEPLATE:DrawDanPlate(x, y, opacity, danGrade, text)  -> nil` | 미리 렌더링한 칭호 텍스처를 사용해 지정한 등급의 단위 플레이트를 그립니다. |
| `NAMEPLATE:DrawPlayerNameplate(x, y, opacity, player)  -> nil` | 플레이어 슬롯의 완전한 네임플레이트를 그립니다. 빨간 쪽인지 파란 쪽인지는 게임의 1P 쪽 설정을 따릅니다. |
| `NAMEPLATE:DrawNameplateTitleById(id, x, y, opacity, font)  -> nil` | 이 id의 네임플레이트의 지역화된 칭호를 텍스트 객체로 렌더링해 칭호 플레이트로 그립니다. id는 네임플레이트 데이터베이스에 존재해야 합니다. |

### NAMEPLATESLIST

게임이 아는 모든 네임플레이트의 데이터베이스로, 인덱스나 id로 조회하고 필터링할 수 있습니다.

<div class="callout warn">
조회 메서드는 네임플레이트 정보 핸들을 반환합니다. FindWhere는 네임플레이트마다 Lua 함수를 한 번 호출하고 true를 반환한 항목을 남깁니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `NAMEPLATESLIST.Count  -> integer` | 데이터베이스의 네임플레이트 수. |
| `NAMEPLATESLIST:GetByIndex(index)  -> nameplateInfo` | 0부터 시작하는 데이터베이스 위치의 네임플레이트. 범위를 벗어나면 nil. |
| `NAMEPLATESLIST:GetById(id)  -> nameplateInfo` | 이 id의 네임플레이트. 없으면 nil. |
| `NAMEPLATESLIST:GetAll()  -> nameplateInfo[]` | 모든 네임플레이트를 리스트로. |
| `NAMEPLATESLIST:FindWhere(predicate)  -> nameplateInfo[]` | `predicate(info)`가 true를 반환하는 네임플레이트. |

### 네임플레이트 정보 핸들

네임플레이트 칭호 하나: 지역화된 텍스트, 표시 타입, id, 희귀도, 잠금 해제 조건입니다.

<div class="callout warn">
sf.NameplateInfo와 NAMEPLATESLIST가 이 핸들을 반환합니다. 기본 초보자 네임플레이트는 id -1, 희귀도 "Common"이며 잠금 해제 조건이 없습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `info.Title  -> string` | 현재 언어의 칭호 텍스트. |
| `info.Type  -> integer` | NAMEPLATE:DrawTitlePlate에 넘기는 표시 타입 코드. |
| `info.Id  -> integer` | 네임플레이트 id(기본 초보자 네임플레이트는 -1). |
| `info.Rarity  -> string` | 희귀도 이름: "Poor", "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythical". |
| `info.UnlockCondition  -> unlockCondition` | 잠금 해제 조건(잠금 해제 조건 핸들 참고). |

### 단위 플레이트 정보 핸들

네임플레이트에 표시되는 플레이어의 활성 단위 칭호입니다.

<div class="callout warn">
sf.DanplateInfo가 이 핸들을 반환합니다. 값은 읽는 시점의 세이브 파일을 반영합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `info.Title  -> string` | 활성 단위 칭호의 텍스트. |
| `info.Gold  -> bool` | 플레이어가 활성 칭호를 금색 합격으로 얻었는지 여부. |
| `info.ClearStatus  -> integer` | 활성 칭호의 클리어 상태 코드. |

### 단위 칭호 항목 핸들

플레이어가 선택할 수 있는 단위 칭호 하나입니다.

<div class="callout warn">
sf:GetDanTitleByIndex가 이 항목을 반환합니다. 인덱스 0은 기본 칭호(금색 아님, 클리어 상태 0)이고, 이후 인덱스는 플레이어가 획득한 칭호입니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `entry.Title  -> string` | 칭호 텍스트. |
| `entry.IsGold  -> bool` | 플레이어가 칭호를 금색 합격으로 얻었는지 여부. |
| `entry.ClearStatus  -> integer` | 칭호에 기록된 최고 클리어 상태. |

### 단위 최고 플레이 핸들

단위 기록 하나의 최고 시험 결과입니다.

<div class="callout warn">
sf:GetDanBestPlay가 이 핸들을 반환합니다. 시험을 읽기 전에 HasRecord를 확인하십시오. GetExam은 .NET 배열을 반환합니다. 0부터 인덱싱하고 `.Length`를 읽으십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `play.HasRecord  -> bool` | 곡에 대한 기록이 있는지 여부. |
| `play:GetExam(slot)  -> int[]` | 시험 슬롯 1에서 7의 최고 스코어: 코스 전체 시험은 값 하나, 곡별 시험은 곡마다 하나. 기록이 없거나 슬롯이 유효하지 않으면 비어 있음. |

## 캐릭터와 푸치캬라

### CHARACTER

캐릭터 핸들을 만들고 표준 애니메이션과 음성 슬롯의 이름을 노출합니다.

<div class="callout warn">
CreateCharacter는 자체 리소스를 소유하는 핸들을 반환합니다. IsValid를 확인하고 다 쓰면 Dispose를 호출하십시오. GetPlayerCharacter는 플레이어가 장착한 캐릭터를 따라가는 핸들을 반환하며 해제가 필요 없습니다. GetPlayerGradientMap은 그라디언트 맵을 반환합니다(그래픽과 텍스트 참고). ANIM_*와 VOICE_* 멤버는 읽기 전용 문자열입니다. 캐릭터 핸들의 애니메이션과 음성 메서드에 넘기십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `CHARACTER:CreateCharacter(folderName)  -> character` | Global/Characters/{folderName}에서 독립 캐릭터를 로드합니다. 폴더가 없으면 IsValid가 false입니다. |
| `CHARACTER:GetPlayerCharacter(player)  -> character` | 호출마다 장착한 캐릭터를 해석하는, 플레이어 슬롯에 바인딩된 핸들. |
| `CHARACTER:GetPlayerGradientMap(player)  -> gradientMap` | 플레이어 슬롯에 활성인 팔레트 그라디언트. 설정되지 않았으면 nil. |
| `CHARACTER.ANIM_PREVIEW  -> string` | 미리 보기 포즈(메뉴와 상점). |
| `CHARACTER.ANIM_RENDER  -> string` | 전체 렌더 포즈. |
| `CHARACTER.ANIM_GAME_NORMAL  -> string` | 게임플레이, 보통 상태. |
| `CHARACTER.ANIM_GAME_CLEAR  -> string` | 게임플레이, 게이지가 클리어 구간. |
| `CHARACTER.ANIM_GAME_MAX  -> string` | 게임플레이, 게이지 가득 참. |
| `CHARACTER.ANIM_GAME_GOGO  -> string` | 게임플레이, 고고타임. |
| `CHARACTER.ANIM_GAME_GOGO_MAX  -> string` | 게임플레이, 게이지가 가득 찬 고고타임. |
| `CHARACTER.ANIM_GAME_MISS  -> string` | 게임플레이, 미스. |
| `CHARACTER.ANIM_GAME_MISS_DOWN  -> string` | 게임플레이, 게이지가 낮은 미스. |
| `CHARACTER.ANIM_GAME_10COMBO  -> string` | 게임플레이, 10콤보 달성. |
| `CHARACTER.ANIM_GAME_10COMBO_MAX  -> string` | 게임플레이, 게이지가 가득 찬 10콤보 달성. |
| `CHARACTER.ANIM_GAME_CLEARED  -> string` | 게임플레이, 곡 클리어. |
| `CHARACTER.ANIM_GAME_FAILED  -> string` | 게임플레이, 곡 실패. |
| `CHARACTER.ANIM_GAME_CLEAR_OUT  -> string` | 클리어 상태에서 나가는 전환. |
| `CHARACTER.ANIM_GAME_CLEAR_IN  -> string` | 클리어 상태로 들어가는 전환. |
| `CHARACTER.ANIM_GAME_MAX_OUT  -> string` | 게이지 가득 참 상태에서 나가는 전환. |
| `CHARACTER.ANIM_GAME_MAX_IN  -> string` | 게이지 가득 참 상태로 들어가는 전환. |
| `CHARACTER.ANIM_GAME_MISS_IN  -> string` | 미스로 들어가는 전환. |
| `CHARACTER.ANIM_GAME_MISS_DOWN_IN  -> string` | 게이지가 낮은 미스로 들어가는 전환. |
| `CHARACTER.ANIM_GAME_RETURN  -> string` | 보통 상태로 복귀. |
| `CHARACTER.ANIM_GAME_GOGOSTART  -> string` | 고고 시작 버스트. |
| `CHARACTER.ANIM_GAME_GOGOSTART_CLEAR  -> string` | 클리어 상태에서의 고고 시작 버스트. |
| `CHARACTER.ANIM_GAME_GOGOSTART_MAX  -> string` | 게이지가 가득 찬 고고 시작 버스트. |
| `CHARACTER.ANIM_GAME_BALLOON_BREAKING  -> string` | 풍선을 치는 중. |
| `CHARACTER.ANIM_GAME_BALLOON_BROKE  -> string` | 풍선 터짐. |
| `CHARACTER.ANIM_GAME_BALLOON_MISS  -> string` | 풍선 놓침. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING  -> string` | 쿠스다마를 치는 중. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BROKE  -> string` | 쿠스다마 터짐. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_MISS  -> string` | 쿠스다마 놓침. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_IDLE  -> string` | 쿠스다마 대기. |
| `CHARACTER.ANIM_GAME_TOWER_STANDING  -> string` | 타워 모드, 서 있음. |
| `CHARACTER.ANIM_GAME_TOWER_STANDING_TIRED  -> string` | 타워 모드, 지친 채 서 있음. |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING  -> string` | 타워 모드, 오르는 중. |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING_TIRED  -> string` | 타워 모드, 지친 채 오르는 중. |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING  -> string` | 타워 모드, 달리는 중. |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING_TIRED  -> string` | 타워 모드, 지친 채 달리는 중. |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR  -> string` | 타워 모드, 클리어. |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR_TIRED  -> string` | 타워 모드, 지친 채 클리어. |
| `CHARACTER.ANIM_GAME_TOWER_FAIL  -> string` | 타워 모드, 실패. |
| `CHARACTER.ANIM_MENU_WAIT  -> string` | 메뉴, 대기. |
| `CHARACTER.ANIM_MENU_START  -> string` | 메뉴, 시작. |
| `CHARACTER.ANIM_MENU_NORMAL  -> string` | 메뉴, 보통. |
| `CHARACTER.ANIM_MENU_SELECT  -> string` | 메뉴, 선택. |
| `CHARACTER.ANIM_ENTRY_NORMAL  -> string` | 엔트리 화면, 보통. |
| `CHARACTER.ANIM_ENTRY_JUMP  -> string` | 엔트리 화면, 점프. |
| `CHARACTER.ANIM_RESULT_NORMAL  -> string` | 결과, 보통. |
| `CHARACTER.ANIM_RESULT_CLEAR  -> string` | 결과, 클리어. |
| `CHARACTER.ANIM_RESULT_FAILED_IN  -> string` | 결과, 실패 상태로 들어감. |
| `CHARACTER.ANIM_RESULT_FAILED  -> string` | 결과, 실패. |
| `CHARACTER.VOICE_END_FAILED  -> string` | 곡 종료, 실패. |
| `CHARACTER.VOICE_END_CLEAR  -> string` | 곡 종료, 클리어. |
| `CHARACTER.VOICE_END_FULLCOMBO  -> string` | 곡 종료, 풀 콤보. |
| `CHARACTER.VOICE_END_ALLPERFECT  -> string` | 곡 종료, 올 퍼펙트. |
| `CHARACTER.VOICE_END_AIBATTLE_WIN  -> string` | 곡 종료, AI 배틀 승리. |
| `CHARACTER.VOICE_END_AIBATTLE_LOSE  -> string` | 곡 종료, AI 배틀 패배. |
| `CHARACTER.VOICE_MENU_SONGSELECT  -> string` | 곡 선택 진입. |
| `CHARACTER.VOICE_MENU_SONGDECIDE  -> string` | 곡 확정. |
| `CHARACTER.VOICE_MENU_SONGDECIDE_AI  -> string` | AI 배틀에서 곡 확정. |
| `CHARACTER.VOICE_MENU_DIFFSELECT  -> string` | 난이도 선택. |
| `CHARACTER.VOICE_MENU_DANSELECTSTART  -> string` | 단위 선택 진입. |
| `CHARACTER.VOICE_MENU_DANSELECTPROMPT  -> string` | 단위 선택 프롬프트. |
| `CHARACTER.VOICE_MENU_DANSELECTCONFIRM  -> string` | 단위 코스 확정. |
| `CHARACTER.VOICE_TITLE_SANKA  -> string` | 타이틀 화면 엔트리. |
| `CHARACTER.VOICE_TOWER_MISS  -> string` | 타워 모드 미스. |
| `CHARACTER.VOICE_RESULT_BESTSCORE  -> string` | 결과, 새 최고 스코어. |
| `CHARACTER.VOICE_RESULT_CLEARFAILED  -> string` | 결과, 실패. |
| `CHARACTER.VOICE_RESULT_CLEARSUCCESS  -> string` | 결과, 클리어. |
| `CHARACTER.VOICE_RESULT_DANFAILED  -> string` | 결과, 단위 불합격. |
| `CHARACTER.VOICE_RESULT_DANREDPASS  -> string` | 결과, 단위 합격. |
| `CHARACTER.VOICE_RESULT_DANGOLDPASS  -> string` | 결과, 단위 금색 합격. |

### 캐릭터 핸들

그릴 수 있는 캐릭터: 이름이 지정된 애니메이션과 음성을 재생하고 핸들별 그리기 상태(불투명도, 스케일, 틴트, 회전, 블렌드와 래핑 모드, 팔레트 그라디언트)를 가집니다.

<div class="callout warn">
CHARACTER:GetPlayerCharacter, CHARACTER:CreateCharacter, sf:GetCharacter, 캐릭터 목록 항목의 Character 프로퍼티가 캐릭터 핸들을 반환합니다. CreateCharacter의 핸들만 리소스를 소유하며 Dispose가 필요합니다. 핸들은 Set* 값을 저장하고 이후의 모든 그리기에 적용합니다. 그리기 메서드의 스케일과 불투명도 인자는 저장된 값과 곱해집니다. 저장된 불투명도는 0.0에서 1.0, 그리기별 불투명도는 0에서 255입니다. 애니메이션과 음성 이름은 CHARACTER 상수입니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `char.IsValid  -> bool` | 핸들이 로드된 캐릭터로 해석되는지 여부. |
| `char.FolderName  -> string` | 폴더 이름. 유효하지 않으면 빈 문자열. |
| `char.FullPath  -> string` | 절대 폴더 경로. 유효하지 않으면 빈 문자열. |
| `char.DisplayName  -> string` | 지역화된 표시 이름. 없으면 폴더 이름으로 대체. |
| `char:SetPaletteGradient(stops, blend?)  -> nil` | 최소 두 개의 색상 정지점 테이블로 만든 팔레트 그라디언트를 선택적 블렌드 양(기본값 1.0)과 함께 적용합니다. 플레이어 바인딩 핸들은 그라디언트를 플레이어 슬롯에도 저장합니다. nil을 넘기면 해제됩니다. |
| `char:ClearPaletteGradient()  -> nil` | 팔레트 그라디언트를(플레이어 바인딩 핸들에서는 플레이어 슬롯의 그라디언트도) 제거합니다. |
| `char:SetOpacity(opacity)  -> nil` | 저장 불투명도, 0.0 투명에서 1.0 불투명. |
| `char:SetScale(scaleX, scaleY)  -> nil` | 저장 스케일. 음수 X는 가로로 뒤집습니다. |
| `char:SetColor(color)  -> nil` | 색상 값으로 저장 틴트를 설정합니다. |
| `char:SetColor(r, g, b)  -> nil` | 0.0에서 1.0 채널 세 개로 저장 틴트를 설정합니다. |
| `char:SetRotation(degrees)  -> nil` | 저장 회전(도). |
| `char:SetBlendMode(mode)  -> nil` | 저장 블렌드 모드: "normal", "add", "multi", "sub", "screen". |
| `char:SetWrapMode(mode)  -> nil` | 저장 텍스처 래핑 모드: "edge", "border", "repeat", "mirror". |
| `char:GetScale()  -> vector2` | 저장 스케일. |
| `char:GetColor()  -> tuple` | 저장 틴트를 Item1, Item2, Item3(빨강, 초록, 파랑) 필드를 가진 .NET 튜플로. |
| `char:GetRotation()  -> number` | 저장 회전(도). |
| `char:GetBlendMode()  -> string` | 저장 블렌드 모드. |
| `char:GetWrapMode()  -> string` | 저장 래핑 모드. |
| `char:Draw(x, y, animation, scaleX?, scaleY?, opacity?)  -> nil` | x, y에 애니메이션을 그립니다. 기본값: 스케일 1, 불투명도 255. |
| `char:DrawAtAnchor(x, y, animation, anchor?, scaleX?, scaleY?, opacity?)  -> nil` | 지정한 앵커 지점(기본값 "bottom")이 x, y에 오도록 애니메이션을 그립니다. |
| `char:DrawRect(x, y, w, h, animation, scaleX?, scaleY?, opacity?)  -> nil` | 사각형의 왼쪽 위 모서리에 애니메이션을 그립니다. 이 메서드는 레이아웃 코드를 위해 w와 h를 받지만, 둘은 그리기에 영향을 주지 않습니다. |
| `char:DrawRectAtAnchor(x, y, clipW, clipH, animation, opacity?, clipX?, clipY?)  -> nil` | 왼쪽 위 모서리를 x, y에 두고 clipX, clipY만큼 오프셋된 clipW x clipH 사각형으로 잘라 애니메이션을 그립니다. 스케일, 틴트, 회전은 저장 상태에서만 옵니다. |
| `char:Update(animation, looping?)  -> bool` | 애니메이션을 전진시키고(기본값 루프) 아직 재생 중인지 반환합니다. |
| `char:LoadAnimation(animation)  -> nil` | 애니메이션의 프레임을 로드합니다. |
| `char:DisposeAnimation(animation)  -> nil` | 애니메이션의 프레임을 해제합니다. |
| `char:AvailableAnimation(animation)  -> bool` | 캐릭터가 애니메이션을 제공하는지 여부. |
| `char:SetAnimationDuration(animation, duration)  -> nil` | 애니메이션의 재생 길이를 설정합니다. |
| `char:SetAnimationCyclesFromBPM(animation, bpm)  -> nil` | BPM으로 애니메이션의 사이클 길이를 설정합니다. |
| `char:ResetAnimationCounter(animation)  -> nil` | 애니메이션을 첫 프레임부터 다시 시작합니다. |
| `char:GetAnimationSize(animation)  -> vector2` | 스킨 해상도에서 애니메이션 현재 프레임의 그려지는 크기. 사용할 수 없으면 (0, 0). |
| `char:LoadVoice(voice)  -> nil` | 음성 클립을 로드합니다. |
| `char:DisposeVoice(voice)  -> nil` | 음성 클립을 해제합니다. |
| `char:PlayVoice(voice)  -> nil` | 음성 클립을 재생합니다. |
| `char:Dispose()  -> nil` | 캐릭터의 리소스를 놓습니다(CreateCharacter의 핸들만). |

```lua
local chara = CHARACTER:GetPlayerCharacter(0)
chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)

function update()
    chara:Update(CHARACTER.ANIM_MENU_NORMAL)
end

function draw()
    chara:DrawAtAnchor(960, 1000, CHARACTER.ANIM_MENU_NORMAL, "bottom")
end
```

### CHARACTERLIST

로드된 모든 캐릭터의 목록입니다.

<div class="callout warn">
스킨은 캐릭터를 로드할 때 목록을 다시 만들고 스킨 리로드 시 해제하므로, 캐릭터가 로드되지 않은 동안에는 전역이 nil일 수 있습니다. 조회 메서드는 캐릭터 목록 항목을 반환합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `CHARACTERLIST.Count  -> integer` | 로드된 캐릭터 수. |
| `CHARACTERLIST:GetAll()  -> characterEntry[]` | 모든 캐릭터를 리스트로. |
| `CHARACTERLIST:GetByIndex(index)  -> characterEntry` | 0부터 시작하는 인덱스의 항목. 범위를 벗어나면 nil. |
| `CHARACTERLIST:GetByName(folderName)  -> characterEntry` | 이 폴더 이름의 항목. 없으면 nil. |

### 캐릭터 목록 항목

CHARACTERLIST 항목 하나: 폴더 이름, 표시 이름, 희귀도, 캐릭터 핸들, 잠금 해제 조건입니다.

<div class="callout warn">
Character 프로퍼티의 공유 핸들은 목록이 소유합니다. 해제하지 마십시오. 그리기 전에 애니메이션을 로드하십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `entry.FolderName  -> string` | 폴더 이름. 세이브 파일이 키로 사용합니다. |
| `entry.DisplayName  -> string` | 지역화된 표시 이름. |
| `entry.Rarity  -> string` | 희귀도 이름(목록은 네임플레이트 정보 핸들 참고). |
| `entry.Character  -> character` | 이 항목의 캐릭터 핸들. |
| `entry.UnlockCondition  -> unlockCondition` | 잠금 해제 조건(잠금 해제 조건 핸들 참고). |

### PUCHICHARALIST

로드된 모든 푸치캬라의 목록과 각 플레이어의 현재 선택입니다.

<div class="callout warn">
스킨은 푸치캬라 텍스처를 로드할 때 목록을 다시 만들고 스킨 리로드 시 해제하므로, 로드되지 않은 동안에는 전역이 nil일 수 있습니다. 조회 메서드는 푸치캬라 핸들을 반환합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `PUCHICHARALIST.Count  -> integer` | 로드된 푸치캬라 수. |
| `PUCHICHARALIST:GetAll()  -> puchichara[]` | 모든 푸치캬라를 리스트로. |
| `PUCHICHARALIST:GetByIndex(index)  -> puchichara` | 0부터 시작하는 인덱스의 푸치캬라. 범위를 벗어나면 nil. |
| `PUCHICHARALIST:GetByName(folderName)  -> puchichara` | 이 폴더 이름의 푸치캬라. 없으면 nil. |
| `PUCHICHARALIST:GetPlayerPuchichara(player)  -> puchichara` | 플레이어 슬롯이 장착한 푸치캬라. 해석할 수 없으면 nil. |

### 푸치캬라 핸들

푸치캬라 하나: 텍스처, 지역화된 이름과 제작자, 희귀도, 폴더 이름, 잠금 해제 조건입니다.

<div class="callout warn">
PUCHICHARALIST와 sf:GetPuchichara가 이 핸들을 반환합니다. 텍스처는 목록이 소유하므로 해제하지 마십시오. 이미지가 없으면 빈 텍스처가 됩니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `puchi.tx  -> texture` | Chara.png에서 로드한 스프라이트 시트. |
| `puchi.render  -> texture` | Render.png에서 로드한 전체 렌더. |
| `puchi.Name  -> string` | 지역화된 표시 이름. |
| `puchi.Author  -> string` | 지역화된 제작자 이름. |
| `puchi.Rarity  -> string` | 희귀도 이름(목록은 네임플레이트 정보 핸들 참고). |
| `puchi.FolderName  -> string` | 폴더 이름. 세이브 파일이 키로 사용합니다. |
| `puchi.UnlockCondition  -> unlockCondition` | 잠금 해제 조건(잠금 해제 조건 핸들 참고). |
| `puchi:GetUnlockMessage()  -> string` | `puchi.UnlockCondition:GetConditionMessage()`의 단축형. |

## 플레이 상태와 잠금 해제

### PLAYSTATE

현재 또는 가장 최근 플레이의 실시간 결과: 판정 수, 스코어, 콤보, 클리어 검사, 타워와 단위 상태입니다.

<div class="callout warn">
값은 게임플레이 화면에서 오므로 플레이 중과 그 뒤의 화면에서 의미가 있습니다. 플레이어 인덱스는 0부터 시작하며, 메서드는 범위 검사를 하지 않습니다. 단위 검사는 항상 플레이어 0을 평가합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `PLAYSTATE.LastRegisteredFloor  -> integer` | 타워 모드: 마지막으로 도달한 층. |
| `PLAYSTATE.MaxNumberOfLives  -> integer` | 타워 모드: 최대 라이프 수. |
| `PLAYSTATE.CurrentNumberOfLives  -> integer` | 타워 모드: 현재 라이프 수. |
| `PLAYSTATE.InvincibilityDurationSpeedDependent  -> number` | 타워 모드: 곡 속도에 맞춰 조정된 무적 시간. |
| `PLAYSTATE.InvincibilityDuration  -> integer` | 타워 모드: 기본 무적 시간. |
| `PLAYSTATE:WasPlayEndedNormally()  -> bool` | 이전 플레이가 끝까지 진행되었는지 여부. |
| `PLAYSTATE:WasPlayAborted()  -> bool` | 플레이어가 이전 플레이를 도중에 종료했는지 여부. |
| `PLAYSTATE:GetGoodCount(player)  -> integer` | Good 판정 수. |
| `PLAYSTATE:GetOkCount(player)  -> integer` | Ok 판정 수. |
| `PLAYSTATE:GetBadCount(player)  -> integer` | Bad 판정 수. |
| `PLAYSTATE:GetRollCount(player)  -> integer` | 연타 히트 수. |
| `PLAYSTATE:GetADLibCount(player)  -> integer` | 맞힌 ADLib 노트 수. |
| `PLAYSTATE:GetMissedADLibCount(player)  -> integer` | 놓친 ADLib 노트 수. |
| `PLAYSTATE:GetBoomCount(player)  -> integer` | 맞힌 지뢰 노트 수. |
| `PLAYSTATE:GetAvoidedBoomCount(player)  -> integer` | 피한 지뢰 노트 수. |
| `PLAYSTATE:GetScore(player)  -> integer` | 현재 스코어. |
| `PLAYSTATE:GetCombo(player)  -> integer` | 현재 콤보. |
| `PLAYSTATE:GetHighestCombo(player)  -> integer` | 도달한 최고 콤보. |
| `PLAYSTATE:IsClear(player)  -> bool` | 게이지가 클리어 선을 충족하는지 여부. |
| `PLAYSTATE:IsAssistedClear(player)  -> bool` | 스코어를 낮추는 모드가 활성인 채로 클리어했는지 여부. |
| `PLAYSTATE:IsFullCombo(player)  -> bool` | 어시스트가 아닌 클리어이며 Bad 판정과 맞힌 지뢰가 없음. |
| `PLAYSTATE:IsPerfect(player)  -> bool` | Ok 판정이 없는 풀 콤보. |
| `PLAYSTATE:IsAlive()  -> bool` | 타워 모드: 라이프가 남아 있는지 여부. |
| `PLAYSTATE:IsPass()  -> bool` | 단위 모드: 시험 상태가 불합격이 아닌지 여부. |
| `PLAYSTATE:IsRedPass()  -> bool` | 단위 모드: 시험 상태가 일반 합격인지 여부. |
| `PLAYSTATE:IsGoldPass()  -> bool` | 단위 모드: 시험 상태가 금색 합격인지 여부. |
| `PLAYSTATE:IsDanClear()  -> bool` | 단위 모드: 합격이며 어시스트가 아님. |
| `PLAYSTATE:IsDanFullCombo()  -> bool` | 단위 모드: Bad 판정과 맞힌 지뢰가 없는 단위 클리어. |
| `PLAYSTATE:IsDanPerfect()  -> bool` | 단위 모드: Ok 판정이 없는 단위 풀 콤보. |

### 잠금 해제 조건 핸들

네임플레이트, 캐릭터, 푸치캬라의 잠금 해제 요건입니다.

<div class="callout warn">
네임플레이트 정보 핸들, 캐릭터 목록 항목, 푸치캬라 핸들의 UnlockCondition 프로퍼티가 이 핸들을 반환합니다. 조건이 없는 항목(HasCondition false)은 기본적으로 사용 가능합니다. IsUnlockable은 true를 반환하고 메시지는 비어 있습니다. 조건 어휘는 Unlock.json과 채보 잠금 해제 요소의 것과 같습니다. <a href="../guides/unlockables.md">채보 잠금 해제 요소</a> 가이드를 참고하십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `cond.HasCondition  -> bool` | 항목에 잠금 해제 조건이 있는지 여부. |
| `cond:GetConditionType()  -> string` | 조건 유형 id(예: "ch", "cs", "gt", "gc", "ig"). 없으면 빈 문자열. |
| `cond:GetCoinPrice()  -> integer` | 조건의 코인 가격. 없으면 0. |
| `cond:GetConditionMessage()  -> string` | 조건의 지역화된 설명. |
| `cond:IsUnlockable(player)  -> bool` | 플레이어가 현재 조건을 충족하는지 여부. |
| `cond:GetBlockedMessage(player)  -> string` | 플레이어가 조건을 충족하지 못하는 이유. 충족하면 빈 문자열. |

## 테마와 언어

### THEME

스킨의 해상도, 테마 설정, 스킨 범위의 지역화 문자열, 테마 설정 정의입니다.

<div class="callout warn">
스킨은 테마 설정을 ThemeSettings.json에 선언하고 값은 그 옆의 ThemeSettings.db3에 저장합니다. getter는 설정 값을 항상 문자열로 반환합니다. 없는 설정은 선언된 기본값을, 선언이 없으면 빈 문자열을 반환합니다. GetThemeSettingForPlayer는 1부터 시작하는 플레이어 번호를 받습니다. 정의 인덱스는 0부터 시작합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `THEME:GetResolution()  -> vector2` | 스킨의 해상도. |
| `THEME:GetThemeSetting(settingId)  -> string` | 전역 범위 설정의 값. |
| `THEME:GetThemeSettingForPlayer(settingId, player)  -> string` | 1부터 시작하는 플레이어의 세이브 범위 설정 값. 세이브에 값이 없으면 기본값. |
| `THEME:GetSkinString(key)  -> string` | 스킨의 Locales 폴더에서 온 지역화 문자열: 현재 언어 먼저, 그다음 스킨의 기본 로케일, 그다음 `[LOCALE NOT FOUND: key]`. |
| `THEME:GetDefinitionCount()  -> integer` | ThemeSettings.json의 설정 정의 수. |
| `THEME:GetDefinitionId(index)  -> string` | 0부터 시작하는 인덱스의 정의 id. 없으면 빈 문자열. |
| `THEME:GetDefinitionScope(index)  -> string` | 정의의 범위: "global" 또는 "save". |
| `THEME:GetDefinitionType(index)  -> string` | 정의의 타입: "bool", "int", "double", "string", "enum". |

### LANG

지역화된 게임 문자열, 언어 전환, 다국어 텍스트 값입니다.

<div class="callout warn">
GetString은 추가 인자로 항목을 형식화합니다. GetLanguageIds와 GetLanguageNames는 .NET 배열(0부터 시작, `.Length`)을 반환합니다. GetAvailableLanguages는 `:GetEnumerator()`로 열거할 딕셔너리를 반환합니다(데이터와 영속성 참고). FromDict는 JSONLOADER가 파싱한 JSON 객체를 받고(Lua 테이블은 받지 않습니다), AsLocalizationData는 JSONLOADER:LoadJson의 JsonNode를 받습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `LANG:GetString(key, ...)  -> string` | 키의 지역화 문자열. 형식 자리 표시자는 추가 인자로 채워집니다. |
| `LANG:ChangeLanguage(id)  -> bool` | id가 존재하고 현재와 다르면 활성 언어를 전환한 뒤 로드된 모든 스크립트에서 `reloadLanguage`를 호출합니다. 전환했는지 반환합니다. CONFIG.Language는 바꾸지 않습니다. |
| `LANG:GetLanguageIds()  -> string[]` | 사용 가능한 언어의 id. |
| `LANG:GetLanguageNames()  -> string[]` | 사용 가능한 언어의 표시 이름, 같은 순서. |
| `LANG:GetAvailableLanguages()  -> dict` | 언어 id에서 표시 이름으로. |
| `LANG:GetExamName(type)  -> string` | 단위 시험 유형의 지역화된 이름. |
| `LANG:AsLocalizationData(node)  -> localizationData` | `{ "strings": { "<lang>": "text" } }` 형태의 JsonNode로 지역화 값을 만듭니다. |
| `LANG:FromDict(dict)  -> localizationData` | 언어 id를 텍스트에 대응시키는 파싱된 JSON 객체로 지역화 값을 만듭니다. |
| `LANG:FromString(json)  -> localizationData` | 언어 id를 텍스트에 대응시키는 JSON 객체 문자열로 지역화 값을 만듭니다. 문자열을 파싱할 수 없으면 빈 값. |

```lua
local langs = LANG:GetAvailableLanguages()
local e = langs:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end

local name = LANG:FromString('{"ja":"太鼓","default":"Taiko"}'):GetString("")
```

### 지역화 데이터 핸들

언어 id를 키로 하는, 현재 언어로 해석되는 문자열 집합입니다.

<div class="callout warn">
LANG:AsLocalizationData, LANG:FromDict, LANG:FromString이 이 핸들을 반환합니다. 해석 순서: 현재 언어 id, 그다음 "default" 키, 그다음 GetString에 넘긴 대체값.
</div>

| 메서드 | 설명 |
| --- | --- |
| `loc:GetString(fallback)  -> string` | 현재 언어의 텍스트, 또는 "default", 또는 대체값. |
| `loc:SetString(langId, text)  -> nil` | 언어 id의 텍스트를 설정합니다. |
| `loc:GetAllStrings()  -> string[]` | 저장된 모든 텍스트, 순서는 정해져 있지 않음. |
