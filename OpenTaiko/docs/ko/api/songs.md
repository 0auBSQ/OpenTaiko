<!-- api/songs.md -->

# 곡과 채보

곡 목록 요청, 노드와 채보 탐색, 스코어 읽기, 단위(시험) 코스 구성.

이 페이지에서 사용하는 규약:

- 난이도 인덱스는 0부터 시작합니다: 0 Easy, 1 Normal, 2 Hard, 3 Extreme (`Oni`), 4 Extra Extreme (`Edit`), 5 Tower, 6 Dan.
- 플레이어와 세이브 파일 인덱스는 0부터 시작합니다(0이 플레이어 1).
- 점으로 적힌 멤버(`node.Title`)는 프로퍼티이고, 콜론으로 적힌 멤버(`node:GetChart(3)`)는 메서드입니다.
- 일부 멤버는 C# 컬렉션을 반환합니다. 리스트는 `.Count`가 있고 0부터 인덱싱하며(`list[0]`), 배열은 `.Length`가 있고 역시 0부터 인덱싱합니다. 아래 각 항목에 어느 것을 반환하는지 적혀 있습니다.
- 곡 목록은 곡 열거가 끝난 뒤에야 완전합니다. `afterSongEnum()` 콜백에서 요청하거나([모듈과 생명 주기](activities.md) 참고), 먼저 `IsSongsEnumDone()` 전역을 확인하십시오. 열거가 끝나면 true를 반환합니다.

## 곡 목록 요청

### RequestSongList

설정 객체로부터 탐색 가능한 곡 목록을 만드는 전역 함수입니다.

<div class="callout warn">
일반 전역 함수로 사용할 수 있습니다. GenerateSongListSettings()로 만든 설정 객체를 넘기십시오. 호출은 게임이 열거한 곡으로 곡 트리를 한 번 만듭니다. 핸들은 설정 객체를 참조로 유지하므로 필드를 바꾸고 핸들의 ReloadSongList()를 호출해 다시 만들 수 있습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `RequestSongList(settings)  -> song list handle` | 지정한 곡 목록 설정으로 곡 목록 핸들을 만들어 반환합니다. |

```lua
local settings = GenerateSongListSettings()
settings.AppendMainRandomBox = false
settings:SetExcludedGenreFolders({ "Dan", "Tower" })

local list = RequestSongList(settings)
local node = list:GetSelectedSongNode()
```

### GenerateSongListSettings

기본값을 가진 곡 목록 설정 객체를 만드는 전역 함수입니다.

| 메서드 | 설명 |
| --- | --- |
| `GenerateSongListSettings()  -> song list settings` | 기본 필드 값을 가진 새 곡 목록 설정 객체를 반환합니다. |

### 곡 목록 설정

곡 목록에 어떤 노드를 포함하고 탐색이 어떻게 동작하는지 제어하는 설정 객체입니다.

<div class="callout warn">
아래 멤버는 모두 Lua에서 직접 읽고 쓰는 공개 필드입니다(settings.HideEmptyFolders = false). 예외는 Lua 테이블을 받는 두 setter 메서드입니다. ExcludedGenreFolders와 MandatoryDifficultyList는 C# 배열이므로 setter 메서드로 설정하십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `settings.AppendMainRandomBox  (bool, default true)` | true이면 목록이 루트에 랜덤 박스를 덧붙입니다. |
| `settings.AppendSubRandomBoxes  (bool, default true)` | true이면 목록이 각 폴더 끝에 랜덤 박스를 덧붙입니다. |
| `settings.SubBackBoxFrequency  (int, default 7)` | 각 폴더 안에서 목록이 시작 부분과 N개 항목마다 뒤로 가기 박스를 삽입합니다. 0이면 생성되는 뒤로 가기 박스를 끕니다. |
| `settings.ExcludedGenreFolders  (string array)` | 목록에서 제외하는 장르 폴더 이름. SetExcludedGenreFolders로 설정하십시오. |
| `settings.RootGenreFolder  (string, default nil)` | 설정되면 목록 루트가 장르가 이 이름과 일치하는 첫 폴더(깊이 우선)가 됩니다. nil이면 루트는 최상위입니다. |
| `settings.RootGenreFolderNode  (song node, default nil)` | RootGenreFolder의 노드 형태. 설정되면 문자열보다 우선하며, 장르 이름을 공유하는 폴더를 구분할 수 있습니다. |
| `settings.MandatoryDifficultyList  (Difficulty array, default nil)` | 곡이 목록에 나타나기 위해 가져야 하는 난이도. nil은 요구 사항 없음을 뜻합니다. SetMandatoryDifficultyList로 설정하십시오. |
| `settings.MandatoryDifficultyMatchAll  (bool, default true)` | true는 나열된 모든 난이도를 요구하고(AND), false는 하나 이상을 요구합니다(OR). |
| `settings.HideEmptyFolders  (bool, default true)` | 보이는 곡이 없는 폴더를 재귀적으로 숨깁니다. |
| `settings.FlattenOpenedFolders  (bool, default true)` | true이면 현재 페이지는 열린 폴더가 제자리에 펼쳐진 전체 트리입니다(닫힌 폴더는 항목 하나로 셈). false이면 페이지는 커서 노드의 형제만 담습니다. |
| `settings.ModuloPagination  (bool, default true)` | true이면 GetSongNodeAtOffset이 페이지를 순환하고, false이면 어느 끝을 넘어서든 nil을 반환합니다. |
| `settings.ModuloMovement  (bool, default true)` | true이면 Move가 페이지를 순환하고, false이면 어느 끝에서든 멈춥니다. |
| `settings.ExcludeHiddenSongs  (bool, default true)` | HiddenIndex가 3(숨김)인 곡을 제외합니다. |
| `settings.ExcludeLockedSongs  (bool, default false)` | true이면 페이지가 잠긴 곡을 제외하므로 탐색이 그 위에 놓이지 않습니다. |
| `settings.IgnoreUnlockables  (bool, default false)` | true이면 목록이 ExcludeLockedSongs를 무시하고 GetRandomNodeInFolder가 잠긴 곡을 반환할 수 있습니다. IsLocked 같은 노드 프로퍼티는 계속 실제 상태를 보고합니다. |
| `settings:SetExcludedGenreFolders(table)  -> void` | 장르 이름 문자열의 Lua 테이블로 ExcludedGenreFolders를 설정합니다. |
| `settings:SetMandatoryDifficultyList(table)  -> void` | 난이도 인덱스의 Lua 테이블로 MandatoryDifficultyList를 설정합니다. |

### 곡 목록 핸들

RequestSongList가 반환하는, 커서·폴더 탐색·검색이 있는 탐색 가능한 곡 트리입니다.

<div class="callout warn">
검색 메서드는 곡 노드를 받아 불리언을 반환하는 Lua 함수를 받습니다. 여러 노드를 반환하는 메서드는 C# 리스트(.Count, 0부터 인덱싱)를 반환합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `list:ReloadSongList()  -> void` | 현재 곡과 설정으로 트리 전체를 다시 만들고 커서를 첫 노드로 옮깁니다. |
| `list:GetRoot()  -> song node` | 트리의 루트 노드를 반환합니다. |
| `list:GetSelectedSongNode()  -> song node` | 커서 아래의 노드를 반환하며, 목록이 비어 있으면 nil. |
| `list:GetSongNodeAtOffset(offset)  -> song node` | 현재 페이지 안에서 커서로부터 지정한 오프셋의 노드를 반환합니다. ModuloPagination에 따라 순환하거나 nil을 반환합니다. |
| `list:Move(offset)  -> void` | 현재 페이지 안에서 커서를 지정한 오프셋만큼 옮깁니다. ModuloMovement에 따라 순환하거나 끝에서 멈춥니다. |
| `list:OpenFolder()  -> bool` | 커서 아래의 폴더를 열고 커서를 첫 자식으로 옮깁니다. 커서가 닫힌 비어 있지 않은 폴더 위에 없으면 false를 반환합니다. |
| `list:CloseFolder()  -> bool` | 커서를 포함하는 폴더를 닫고 커서를 그 폴더로 옮깁니다. 닫을 것이 없으면 false를 반환합니다. 가상 폴더를 떠나면 OpenVirtualFolder가 저장한 커서를 복원합니다. |
| `list:OpenVirtualFolder(baseFolder, songs, title)  -> bool` | Lua 테이블 `songs`(키 1..n)의 곡 노드를 담은 `title`이라는 임시 폴더를 생성된 뒤로 가기 박스와 끝의 랜덤 박스와 함께 열고, 커서를 그 안으로 옮깁니다. `baseFolder`가 가상 폴더의 부모가 됩니다. 테이블에 곡 노드가 없으면 false를 반환합니다. |
| `list:GetSongByUniqueId(id)  -> song node` | 고유 id가 일치하는 첫 곡을 반환하며, 없으면 nil. |
| `list:GetRandomNodeInFolder(node, recursive, predicate)  -> song node` | `node`의 형제(그것을 포함하는 페이지) 중에서 곡을 무작위로 고릅니다. `recursive`(기본값 true)이면 형제 폴더 안의 곡도 선택 대상에 포함합니다. IgnoreUnlockables가 설정되지 않은 한 잠긴 곡은 건너뜁니다. `predicate`는 선택 사항입니다. 조건에 맞는 것이 없으면 nil을 반환합니다. |
| `list:SearchSongsByPredicate(predicate)  -> list of song nodes` | 트리에서 조건 함수가 true를 반환하는 모든 곡 노드를 반환합니다. |
| `list:SearchFirstSongByPredicate(predicate)  -> song node` | 조건 함수가 true를 반환하는 첫 곡 노드를 반환하며, 없으면 nil. |
| `list:SearchNodesByPredicate(predicate)  -> list of song nodes` | SearchSongsByPredicate와 같지만 폴더와 다른 비곡 노드도 검사합니다. |

```lua
local results = list:SearchSongsByPredicate(function(node)
    return node:GetChart(3) ~= nil   -- Extreme 채보가 있음
end)
for i = 0, results.Count - 1 do
    local node = results[i]
end
```

## 노드, 채보, 스코어

### 곡 노드

곡 목록의 항목 하나: 곡, 폴더, 뒤로 가기 박스, 또는 랜덤 박스입니다.

<div class="callout warn">
곡 목록 핸들, SONGMOUNT:ChosenSongNode(), DANBUILDER:GetSong()이 곡 노드를 반환합니다. 프로퍼티는 읽기 전용입니다. 메타데이터 프로퍼티는 곡이 아닌 노드에서 nil을 반환합니다. 목록 탐색은 곡 목록 핸들(Move, OpenFolder, CloseFolder, 검색 메서드)을 통해 하십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `node.NotNull  (bool)` | 노드가 실제 곡 목록 항목을 감싸고 있으면 true. |
| `node.IsFolder  (bool)` | 노드가 폴더이면 true. |
| `node.IsRandom  (bool)` | 노드가 랜덤 박스이면 true. |
| `node.IsReturn  (bool)` | 노드가 뒤로 가기 박스이면 true. |
| `node.IsSong  (bool)` | 노드가 플레이 가능한 곡이면 true. |
| `node.SongCount  (int)` | 직계 자식 곡 수. |
| `node.RecursiveSongCount  (int)` | 하위 폴더를 포함해 이 노드 아래의 곡 수. |
| `node.VisibleSongCount  (int)` | HiddenIndex가 3이 아닌 직계 자식 곡 수. |
| `node.RecursiveVisibleSongCount  (int)` | 하위 폴더를 포함해 이 노드 아래의 보이는 곡 수. |
| `node.BoxType  (string)` | 폴더 박스 스타일 문자열. 없으면 nil. |
| `node.BgType  (string)` | 배경 스타일 문자열. 없으면 nil. |
| `node.BoxChara  (string)` | 박스 캐릭터 문자열. 없으면 nil. |
| `node.ForeColor  (color)` | 노드의 전경색. 없으면 nil. |
| `node.BackColor  (color)` | 노드의 배경색. 없으면 nil. |
| `node.BoxColor  (color)` | 노드의 박스 색. 없으면 nil. |
| `node.Title  (string)` | 표시 제목. 뒤로 가기 박스와 랜덤 박스는 부모 폴더의 제목으로 만든 지역화된 "Return" / "Random" 텍스트를 반환합니다. |
| `node.Subtitle  (string)` | 곡의 부제. 없으면 nil. |
| `node.Genre  (string)` | 장르 문자열. 없으면 nil. |
| `node.UniqueId  (string)` | 곡의 고유 id. 없으면 nil. |
| `node.Maker  (string)` | MAKER 필드를 문자열 하나로. 없으면 nil. |
| `node.Charters  (string array)` | MAKER 필드를 쉼표로 나눈 것. |
| `node.Side  (int)` | SIDE 값: 0 normal, 1 ex, 2 both. |
| `node.Explicit  (bool)` | 곡에 explicit 플래그가 있으면 true. 곡이 아니면 nil. |
| `node.HasVideo  (bool)` | 곡에 배경 동영상이 있으면 true. 곡이 아니면 nil. |
| `node.DemoStart  (int)` | 미리 듣기 BGM 오프셋(밀리초). |
| `node.AudioPath  (string)` | 곡 BGM 파일의 절대 경로. 없으면 빈 문자열. |
| `node.HasPreimage  (bool)` | 곡이 프리이미지를 선언했으면 true. |
| `node.PreimagePath  (string)` | 프리이미지의 절대 경로. 먼저 HasPreimage를 확인하십시오. 프리이미지가 없으면 곡 폴더만 됩니다. |
| `node:GetPreimage()  -> texture` | 디스크에서 프리이미지를 로드해 새 텍스처를 반환하며, 곡에 없으면 nil. 다 쓰면 텍스처를 해제하십시오. |
| `node.ChartMd5  (string)` | 채보 파일의 MD5(대문자 16진수). 없으면 빈 문자열. 설치 간에 같은 값을 유지하며, UniqueId는 그렇지 않습니다. |
| `node:GetChart(diff)  -> chart` | 지정한 난이도 인덱스의 채보를 반환하며, 곡에 그런 채보가 없으면 nil. |
| `node:GetCustomCommand(key)  -> string` | 전역 범위 커스텀 명령(첫 COURSE 앞에 둔 점 접두어 헤더. key는 점을 포함, 예: ".VAULT_NAME")의 값을 반환하며, 없으면 nil. |
| `node:GetCustomCommands()  -> dictionary` | 모든 전역 범위 커스텀 명령을 C# 딕셔너리 객체로 반환합니다. 조회에는 GetCustomCommand를 사용하는 편이 좋습니다. |
| `node.UnlockCondition  (unlock condition)` | 잠금 해제 조건 객체(잠금 해제 조건 참고). |
| `node.UnlockText  (string)` | 곡이 정의한 커스텀 잠금 해제 텍스트. 없으면 생성된 조건 메시지. |
| `node.IsLocked  (bool)` | 이 곡이 현재 잠겨 있으면 true. 곡이 아니면 항상 false. |
| `node.HiddenIndex  (int)` | 잠금 해제 시스템의 표시 상태: 0 표시, 1 회색 처리, 2 흐림 처리, 3 숨김(곡이 아니면 0). |
| `node.Rarity  (string)` | 희귀도 라벨. 잠금 해제 항목이 없는 곡은 "Common", 곡이 아니면 "-". |
| `node:Mount(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | 플레이어별 난이도 인덱스(기본값 0)로 이 곡을 플레이용으로 선택합니다. 처음 CONFIG.PlayerCount개의 인덱스만 검사하며, 노드가 곡이 아니거나 활성 플레이어의 난이도가 없거나 범위를 벗어나면 false를 반환합니다. |
| `node:MountIfNotLocked(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | Mount와 같지만 곡이 잠겨 있으면 마운트하지 않고 false를 반환합니다. |

### 채보

곡의 난이도 하나: 레벨, BPM, 채보 제작자, 타워와 단위 데이터, 최고 스코어, 커스텀 명령입니다.

<div class="callout warn">
곡 노드의 GetChart(diff)가 채보를 반환합니다. 프로퍼티는 읽기 전용입니다. BPM, Life, TotalFloorCount, TowerType, DanTick은 채보에 채보 정보가 없으면 nil을 반환합니다. Difficulty와 LevelIcon은 열거형 객체입니다. 비교는 DifficultyAsInt, IsPlus, IsMinus를 통해 하십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `chart.NotNull  (bool)` | 채보가 실제 채보 데이터를 감싸고 있으면 true. |
| `chart.Parent  (song node)` | 이 채보가 속한 곡 노드. |
| `chart.Difficulty  (enum)` | 열거형 객체로서의 난이도. |
| `chart.DifficultyAsInt  (int)` | 난이도 인덱스. |
| `chart.Level  (int)` | 별 레벨. |
| `chart.LevelDecimal  (number)` | 소수부를 포함한 레벨(예: 12.888). 지정되지 않았으면 정수 레벨. |
| `chart.LevelFirstDecimal  (int)` | LevelDecimal의 첫 소수 자리(0-9). |
| `chart.LevelIcon  (enum)` | 열거형 객체로서의 레벨 아이콘. |
| `chart.IsPlus  (bool)` | 레벨 아이콘이 "plus"이면 true. |
| `chart.IsMinus  (bool)` | 레벨 아이콘이 "minus"이면 true. |
| `chart.NotesDesigner  (string)` | NOTESDESIGNER 필드를 문자열 하나로. |
| `chart.Charters  (string array)` | NOTESDESIGNER 필드를 쉼표로 나눈 것. |
| `chart.BPM  (number)` | 주 BPM. 없으면 nil. |
| `chart.BaseBPM  (number)` | 기준 BPM. 없으면 nil. |
| `chart.MinBPM  (number)` | 최소 BPM. 없으면 nil. |
| `chart.MaxBPM  (number)` | 최대 BPM. 없으면 nil. |
| `chart.Life  (int)` | 타워 라이프 수. 없으면 nil. |
| `chart.TotalFloorCount  (int)` | 타워 층 수. 없으면 nil. |
| `chart.TowerType  (string)` | 타워 타입 문자열. 없으면 nil. |
| `chart.DanTick  (int)` | 단위 플레이트 틱 값. 없으면 nil. |
| `chart.DanTickColor  (color)` | 단위 플레이트 틱 색(채보에 채보 정보가 없으면 흰색). |
| `chart.DanSongs  (array of dan songs)` | 이 단위 채보를 이루는 곡. |
| `chart.DanExams  (array of dan exams)` | 이 단위 채보의 전체 시험 조건. |
| `chart:GetSongExam(songIdx, examSlot)  -> dan exam` | 1부터 시작하는 곡 인덱스와 1부터 시작하는 시험 슬롯의 곡별 시험을 반환합니다. 없으면 결과의 IsSet이 false입니다. |
| `chart:GetPlayerBestScore(save)  -> best score info` | 이 채보에 대한 지정한 세이브 파일의 최고 플레이 요약을 반환합니다. |
| `chart:GetCustomCommand(key)  -> string` | 채보 범위 커스텀 명령(이 COURSE 블록 안의 점 접두어 헤더. key는 점을 포함)의 값을 반환하며, 없으면 nil. |
| `chart:GetCustomCommands()  -> dictionary` | 모든 채보 범위 커스텀 명령을 C# 딕셔너리 객체로 반환합니다. |
| `chart.SongFolder  (string)` | 채보 파일이 있는 절대 폴더. |
| `chart.ChartPath  (string)` | 채보 파일의 절대 경로. |
| `chart.UniqueId  (string)` | 곡의 고유 id. 없으면 빈 문자열. |
| `chart:Select(player)  -> bool` | 이 채보를 지정한 플레이어의 선택 난이도로 표시합니다. 플레이어 0은 선택 곡도 설정합니다. 채보가 유효하지 않으면 false를 반환합니다. |

### 최고 스코어 정보

채보 하나에 대한 세이브 파일의 최고 플레이 요약입니다.

<div class="callout warn">
chart:GetPlayerBestScore(save)가 이 객체를 반환합니다. 모든 멤버는 읽기 전용입니다. 유효하지 않은 세이브 인덱스는 빈 레코드가 됩니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `info.ScoreRank  (int)` | 도달한 최고 스코어 랭크. |
| `info.ClearStatus  (int)` | 도달한 최고 클리어 상태. |
| `info.HighScore  (int)` | 최고 스코어. |
| `info.HasBeenPlayed  (bool)` | 결과와 관계없이 채보에 기록된 플레이가 하나 이상 있으면 true. |
| `info.PlayCount  (int)` | 모든 모드 변형을 합친 이 채보의 총 플레이 수. |

## 단위 시험

### 단위 곡

단위 코스 안의 곡 항목 하나입니다.

<div class="callout warn">
chart.DanSongs의 원소입니다. 모든 멤버는 읽기 전용입니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `dansong.Title  (string)` | 곡 제목. |
| `dansong.SubTitle  (string)` | 곡 부제. |
| `dansong.Genre  (string)` | 곡 장르. |
| `dansong.Level  (int)` | 곡의 별 레벨. |
| `dansong.Difficulty  (enum)` | 열거형 객체로서의 난이도. |
| `dansong.DifficultyAsInt  (int)` | 난이도 인덱스. |

### 단위 시험

단위 코스의 합격/불합격 조건 하나입니다.

<div class="callout warn">
chart.DanExams의 원소이거나 chart:GetSongExam()이 반환합니다. 모든 멤버는 읽기 전용입니다. TypeAsInt 값: 0 게이지, 1 perfect 판정, 2 good 판정, 3 bad 판정, 4 스코어, 5 연타, 6 히트, 7 콤보, 8 정확도, 9 애드립 판정, 10 지뢰 판정. RangeAsInt 값: 0 "이상", 1 "미만".
</div>

| 메서드 | 설명 |
| --- | --- |
| `danexam.IsSet  (bool)` | 이 시험 슬롯이 활성이면 true. |
| `danexam.RedValue  (int)` | 빨강(합격) 기준값. |
| `danexam.GoldValue  (int)` | 금색 기준값. |
| `danexam.TypeAsInt  (int)` | 시험 유형. |
| `danexam.RangeAsInt  (int)` | 비교 방향. |

### DANBUILDER

곡 노드, 난이도, 시험 조건으로 단위 코스를 메모리에서 조립한 뒤 플레이용으로 마운트하는 전역입니다.

<div class="callout warn">
전역 DANBUILDER로 사용할 수 있습니다. 곡과 슬롯 인덱스는 1부터 시작합니다. 예외는 AddSong에 넘기는 난이도로, 0부터 시작하는 난이도 인덱스입니다. 시험 슬롯은 1에서 7까지입니다. 시험 유형 문자열(대소문자 구분 없음, 괄호 안은 축약형): "judgeperfect" (jp), "judgegood" (jg), "judgebad" (jb), "score" (s), "roll" (r), "hit" (h), "combo" (c), "accuracy" (a), "judgeadlib" (ja), "judgemine" (jm). 다른 문자열은 게이지를 뜻합니다. lessThan = true는 시험을 "미만" 검사로, false는 "이상" 검사로 만듭니다. 빌더는 호출 사이에 상태를 유지하므로 새 코스를 만들기 전에 Clear()를 호출하십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `DANBUILDER.SongCount  (int)` | 지금까지 추가된 곡 수. |
| `DANBUILDER:AddSong(node, diff)  -> void` | 0부터 시작하는 난이도 인덱스로 곡 노드를 덧붙입니다. |
| `DANBUILDER:GetSong(i)  -> song node` | 1부터 시작하는 인덱스 i의 곡 노드를 반환하며, 없으면 nil. |
| `DANBUILDER:GetSongDiff(i)  -> int` | 1부터 시작하는 인덱스 i의 곡에 저장된 난이도 인덱스를 반환하며, 없으면 -1. |
| `DANBUILDER:SetTitle(title)  -> void` | 코스 제목을 설정합니다(기본값 "Dynamic Dan"). |
| `DANBUILDER:SetSubtitle(subtitle)  -> void` | 코스 부제를 설정합니다. |
| `DANBUILDER:SetDanTick(tick)  -> void` | 단위 플레이트 틱 값을 설정합니다(기본값 2). |
| `DANBUILDER:SetDanTickColor(r, g, b)  -> void` | 0-255 성분으로 단위 플레이트 틱 색을 설정합니다(기본값 흰색). |
| `DANBUILDER:SetGlobalExam(slot, type, red, gold, lessThan)  -> void` | 지정한 슬롯에 코스 전체 시험을 설정합니다. |
| `DANBUILDER:SetPerSongExam(songIndex, slot, type, red, gold, lessThan)  -> void` | 1부터 시작하는 곡 인덱스와 슬롯으로 곡 하나에 적용되는 시험을 설정합니다. |
| `DANBUILDER:Clear()  -> void` | 모든 곡과 시험을 제거하고 메타데이터를 기본값으로 재설정합니다. |
| `DANBUILDER:Mount()  -> bool` | 코스 채보를 메모리에 만들고 플레이어 1의 Dan 난이도로 플레이용으로 선택합니다. 빌더에 곡이 없거나 빌드가 실패하면 false를 반환합니다. |

```lua
DANBUILDER:Clear()
DANBUILDER:SetTitle("Custom course")
DANBUILDER:AddSong(list:GetSongByUniqueId(id1), 3)
DANBUILDER:AddSong(list:GetSongByUniqueId(id2), 3)
DANBUILDER:SetGlobalExam(1, "gauge", 90, 100, false)
DANBUILDER:SetPerSongExam(2, 2, "judgebad", 10, 5, true)
if DANBUILDER:Mount() then
    return Exit("play")
end
```

## 잠금 해제, 가상 슬롯, 모드 아이콘

### 잠금 해제 조건

무엇이 곡을 잠금 해제하는지와 플레이어가 현재 조건을 충족하는지를 설명합니다.

<div class="callout warn">
node.UnlockCondition이 이 객체를 반환합니다. HasCondition은 프로퍼티이고 나머지는 메서드입니다. 잠금 해제 항목이 없는 곡은 HasCondition = false, IsUnlockable = true를 보고합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `cond.HasCondition  (bool)` | 곡에 명시적 잠금 해제 조건이 있으면 true. |
| `cond:GetConditionMessage()  -> string` | 조건의 사람이 읽을 수 있는 설명을 반환하며, 없으면 빈 문자열. |
| `cond:GetConditionType()  -> string` | 조건 유형 id(예: "ch", "cs", "gt", "gc", "ig")를 반환하며, 없으면 빈 문자열. |
| `cond:GetCoinPrice()  -> int` | 코인 비용을 반환하며, 없으면 0. |
| `cond:IsUnlockable(player)  -> bool` | 지정한 플레이어가 조건을 충족하면 true를 반환합니다. |
| `cond:GetBlockedMessage(player)  -> string` | 플레이어가 조건을 충족하지 못하는 이유를 반환하며, 충족하면 빈 문자열. |

### VIRTUALSLOTS

다섯 개의 가상 캐릭터 슬롯(V1-V5)을 읽고 쓰며, 플레이어 스팟을 슬롯의 비주얼을 표시하도록 리다이렉트하는 전역입니다.

<div class="callout warn">
전역 VIRTUALSLOTS로 사용할 수 있습니다. 슬롯 인덱스는 1에서 5까지입니다. 범위 밖 인덱스는 setter가 무시하고 getter는 기본값을 반환합니다. 이 메서드들은 디스크에 아무것도 기록하지 않습니다. AI 슬롯은 엔진이 관리하며 이 전역으로는 편집할 수 없습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `VIRTUALSLOTS:GetCharacter(slot)  -> string` | 슬롯의 캐릭터 폴더 이름을 반환하며, 없으면 "None". |
| `VIRTUALSLOTS:SetCharacter(slot, folderName)  -> void` | 슬롯의 캐릭터 폴더 이름을 설정합니다. |
| `VIRTUALSLOTS:GetPuchichara(slot)  -> string` | 슬롯의 푸치캬라 폴더 이름을 반환하며, 없으면 "None". |
| `VIRTUALSLOTS:SetPuchichara(slot, folderName)  -> void` | 슬롯의 푸치캬라 폴더 이름을 설정합니다. |
| `VIRTUALSLOTS:GetNameplateName(slot)  -> string` | 슬롯의 네임플레이트 플레이어 이름을 반환하며, 없으면 "VSlot". |
| `VIRTUALSLOTS:SetNameplateName(slot, name)  -> void` | 슬롯의 네임플레이트 플레이어 이름을 설정합니다. |
| `VIRTUALSLOTS:GetNameplateTitle(slot)  -> string` | 슬롯의 네임플레이트 칭호 텍스트를 반환합니다. |
| `VIRTUALSLOTS:SetNameplateTitle(slot, title)  -> void` | 슬롯의 네임플레이트 칭호 텍스트를 설정합니다. |
| `VIRTUALSLOTS:GetNameplateDan(slot)  -> string` | 슬롯의 네임플레이트 단위 텍스트를 반환합니다. |
| `VIRTUALSLOTS:SetNameplateDan(slot, dan)  -> void` | 슬롯의 네임플레이트 단위 텍스트를 설정합니다. |
| `VIRTUALSLOTS:SetNameplateById(slot, nameplateId)  -> void` | 네임플레이트 데이터베이스의 id로 네임플레이트를 적용합니다. 칭호 텍스트, 타입, 희귀도를 설정합니다. 알 수 없는 id는 id만 기록합니다. |
| `VIRTUALSLOTS:SetNameplateType(slot, type)  -> void` | 네임플레이트 칭호 타입(스타일 인덱스)을 직접 설정합니다. |
| `VIRTUALSLOTS:SetNameplateRarity(slot, rarity)  -> void` | 네임플레이트 칭호 희귀도 인덱스를 직접 설정합니다. |
| `VIRTUALSLOTS:SetNameplateDanType(slot, danType)  -> void` | 단위 플레이트 타입을 설정합니다. |
| `VIRTUALSLOTS:SetNameplateDanGold(slot, gold)  -> void` | 단위 플레이트를 금색으로 표시할지 설정합니다. |
| `VIRTUALSLOTS:MountSlot(playerSpot, slotInfo)  -> void` | 플레이어 스팟 1-5가 `slotInfo`의 비주얼을 표시하게 합니다: "1P"-"5P"(플레이어의 세이브 파일), "AI", 또는 "V1"-"V5". 이 덮어쓰기는 그 스팟에 대한 다음 MountSlot 호출까지 유지됩니다. |

### MODICONS

플레이어의 활성 모드 아이콘을 화면 위치에 그리는 전역입니다.

<div class="callout warn">
전역 MODICONS로 사용할 수 있습니다. 그리기는 modicons ROActivity가 수행하며, 첫 Draw 호출이 이를 활성화합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `MODICONS:Draw(player, x, y, alpha)  -> void` | 메뉴 레이아웃으로 지정한 플레이어의 모드 아이콘을 (x, y)에 그립니다. alpha는 선택 사항입니다(기본값 255). |

## 리플레이와 선택된 곡

### REPLAY

채보의 저장된 리플레이를 나열하고 재생을 시작하는 전역입니다.

<div class="callout warn">
전역 REPLAY로 사용할 수 있습니다. ListReplays는 리플레이 헤더의 C# 배열(.Length, 0부터 시작)을 반환합니다. Watch는 리플레이를 로드하고 다음 플레이 한 번에 한해 재생을 준비합니다. 게임은 리플레이의 모드를 메모리에서 적용하고 나중에 이전 모드를 복원합니다. songFolder와 chartPath는 채보의 SongFolder와 ChartPath에서 얻습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN, chartPath)  -> array of replay headers` | 채보와 난이도의 리플레이를 스코어순으로 최대 topN개 반환합니다. chartPath가 있으면 목록이 ChecksumMismatch를 계산할 수 있습니다. |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN)  -> array of replay headers` | 채보 경로 없이 같은 동작(목록이 ChecksumMismatch 계산을 건너뜀). |
| `REPLAY:ListReplaysAsync(songFolder, uniqueId, difficulty, topN, chartPath)  -> replay list handle` | 같은 나열을 백그라운드 스레드에서 실행하고 폴링할 핸들을 반환합니다. |
| `REPLAY:Watch(filepath, chartPath)  -> bool` | 리플레이 파일을 로드하고 다음 플레이의 재생을 준비합니다. 파일 로드에 실패하거나 리플레이를 볼 수 없으면 false를 반환합니다. chartPath는 게임 내 "유효하지 않은 리플레이" 경고를 켭니다. |
| `REPLAY:Watch(filepath)  -> bool` | 채보 경로 없이 같은 동작. |
| `REPLAY.MODFLAG  (object)` | ModFlags의 비트 값: None (0), Mirror (1), Random (2), SuperRandom (4), Invisible (8), PerfectMemory (16), Avalanche (32), Minesweeper (64), Just (128), Safe (256), DynamicBeat (512). |

```lua
local flags = header.ModFlags
local mirrored = (flags & REPLAY.MODFLAG.Mirror) ~= 0
```

### 리플레이 목록 핸들

REPLAY:ListReplaysAsync가 반환하는 핸들입니다.

<div class="callout warn">
매 프레임 IsDone을 폴링하고, true가 되면 Result를 읽으십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `handle.IsDone  (bool)` | 백그라운드 나열이 끝나면 true. |
| `handle.Result  (array of replay headers)` | 나열된 리플레이(IsDone이 true가 될 때까지 비어 있음). |

### 리플레이 헤더

저장된 리플레이 하나의 메타데이터입니다.

<div class="callout warn">
REPLAY:ListReplays가 반환하는 배열이나 리플레이 목록 핸들의 Result의 원소입니다. 모든 멤버는 읽기 전용입니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `rep.FilePath  (string)` | 리플레이 파일의 절대 경로. REPLAY:Watch에 넘기십시오. |
| `rep.PlayerName  (string)` | 리플레이를 기록한 플레이어의 이름. |
| `rep.Score  (int)` | 최종 스코어. |
| `rep.ClearStatus  (int)` | 플레이의 클리어 상태. |
| `rep.ScoreRank  (int)` | 플레이의 스코어 랭크. |
| `rep.Good  (int)` | Good(perfect) 판정 수. |
| `rep.Ok  (int)` | Ok 판정 수. |
| `rep.Bad  (int)` | Bad(miss) 판정 수. |
| `rep.Roll  (int)` | 연타 히트 수. |
| `rep.MaxCombo  (int)` | 최대 콤보. |
| `rep.Boom  (int)` | 지뢰 히트 수. |
| `rep.ADLib  (int)` | 애드립 히트 수. |
| `rep.ModFlags  (int)` | 사용한 모드의 비트마스크(REPLAY.MODFLAG 참고). |
| `rep.ScrollSpeed  (int)` | 플레이의 스크롤 속도 설정. |
| `rep.SongSpeed  (int)` | 플레이의 곡 속도 설정. |
| `rep.JudgeStrictness  (int)` | 플레이의 판정 범위 설정. |
| `rep.Date  (string)` | "yyyy-MM-dd HH:mm" 형식의 플레이 날짜. |
| `rep.Timestamp  (int)` | 원시 틱으로서의 플레이 날짜. |
| `rep.ChartUniqueID  (string)` | 채보의 고유 id. |
| `rep.ChartDifficulty  (int)` | 기록된 플레이의 난이도 인덱스. |
| `rep.ChartChecksum  (string)` | 리플레이와 함께 저장된 채보 MD5. |
| `rep.RandomSeed  (int)` | 노트 셔플 시드. 파일에 저장된 것이 없으면 -1. |
| `rep.GameMode  (int)` | 기록된 플레이의 게임 모드. |
| `rep.GameVersion  (int)` | 리플레이를 기록한 게임 버전. |
| `rep.Watchable  (bool)` | 게임이 리플레이를 충실히 재생할 수 있으면 true. |
| `rep.UnwatchableReason  (string)` | Watchable이 false일 때 리플레이를 볼 수 없는 이유. |
| `rep.OldVersion  (bool)` | 이전 게임 버전이 리플레이를 기록했으면 true. |
| `rep.ChecksumMismatch  (bool)` | 채보 파일이 더 이상 기록과 일치하지 않으면 true(채보 경로를 넘겼을 때만 계산됨). |

### SONGMOUNT

현재 플레이용으로 선택된 곡에 대한 읽기 전용 전역입니다.

<div class="callout warn">
전역 SONGMOUNT로 사용할 수 있습니다. 곡 노드의 Mount(), 채보의 Select(), DANBUILDER:Mount()가 설정한 상태를 반영합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `SONGMOUNT:ChosenUniqueId()  -> string` | 선택된 곡의 고유 id를 반환하며, 없으면 빈 문자열. |
| `SONGMOUNT:ChosenDifficulty()  -> int` | 플레이어 1에 대해 선택된 난이도 인덱스를 반환합니다. |
| `SONGMOUNT:ChosenSongNode()  -> song node` | 선택된 곡을 곡 노드(자식 없음)로 반환하며, 선택된 것이 없으면 nil. |
