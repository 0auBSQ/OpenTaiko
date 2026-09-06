<!-- api/data.md -->

# 데이터와 영속성

재시작 후에도 남는 데이터 저장, SQLite 파일 질의, 모듈 폴더의 파일·JSON·INI 읽기, 모듈 간 리소스 공유, 게임 설정 읽기와 변경.

DATABASE, SQL, STORAGE, JSONLOADER, INILOADER, SHARED에 전달하는 상대 경로는 실행 중인 모듈의 디렉터리(그 `Script.lua`가 있는 폴더)를 기준으로 해석됩니다.

이 페이지의 일부 메서드는 .NET 컬렉션을 반환하며, 이는 Lua 테이블과 다르게 동작합니다.

- 배열(`string[]`, `int[]`, `double[]`)은 인덱스 0부터 시작하며 `.Length`를 노출합니다.
- 딕셔너리(파싱된 JSON, SQL 행, 언어 맵)는 `d["key"]`(JSON에서 파싱된 배열은 `d[1]`)로 인덱싱하고 `d:GetEnumerator()`로 열거합니다. `pairs`와 `#`는 동작하지 않습니다.

```lua
local files = STORAGE:GetFiles("maps", "*.json")
for i = 0, files.Length - 1 do
    print(files[i])
end

local e = dict:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end
```

## 키/값 데이터베이스

### DATABASE

재시작 후에도 문자열 값을 유지하는 LMDB 키/값 저장소를 모듈 폴더 안이나 게임 전체 데이터 폴더에 엽니다.

<div class="callout warn">
모든 Read와 Write는 자체 LMDB 환경을 열고 닫으므로 호출마다 비쌉니다. Lua에 값을 캐시하고 쓸 때 캐시를 갱신하십시오. 읽기 전용 모듈(ROActivity와 배경) 안에서 DATABASE는 Write가 오류를 기록하고 아무것도 하지 않는 저장소를 반환합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `DATABASE:OpenLocalDatabase(path)  -> database` | 모듈 디렉터리 기준 상대 경로에 저장소를 엽니다(필요하면 생성). |
| `DATABASE:OpenGlobalDatabase(path)  -> database` | 게임 폴더의 `Global/ApplicationData/LMDB/` 아래에, 모든 모듈이 공유하는 저장소를 엽니다(필요하면 생성). |

### 데이터베이스 핸들

DATABASE가 반환하는, 문자열 키를 문자열 값에 대응시키는 키/값 저장소입니다.

<div class="callout warn">
값은 문자열만 가능합니다. 숫자와 불리언은 직접 변환하십시오(예: tostring과 tonumber).
</div>

| 메서드 | 설명 |
| --- | --- |
| `database:Write(key, value)  -> nil` | 키 아래에 문자열을 저장하고 커밋합니다. 읽기 전용 모듈에서는 오류를 기록하고 아무것도 하지 않습니다. |
| `database:Read(key)  -> string` | 키 아래에 저장된 문자열을 반환하며, 키가 없거나 읽기가 실패하면 nil을 반환합니다. |
| `database:Dispose()  -> nil` | 아무것도 하지 않습니다. 핸들은 열린 리소스를 보유하지 않습니다. |

```lua
local db

function activate()
    db = DATABASE:OpenGlobalDatabase("GameStatus")
    if db:Read("new_user") == nil then
        db:Write("new_user", "true")
    end
end
```

## SQL 데이터베이스

### SQL

SQL 문을 실행하기 위해 모듈 디렉터리 안의 SQLite 데이터베이스 파일을 엽니다.

| 메서드 | 설명 |
| --- | --- |
| `SQL:OpenSQLDatabase(path)  -> sql` | 모듈 디렉터리 기준 상대 경로의 SQLite 데이터베이스를 엽니다. |

### SQL 핸들

SQL:OpenSQLDatabase가 반환하는 SQLite 연결입니다.

<div class="callout warn">
Query는 1..n을 키로 하는 딕셔너리를 반환하며, 각 행은 열 이름을 키로 하는 딕셔너리입니다(페이지 상단의 .NET 컬렉션 참고). 실패한 문은 오류를 기록하고 빈 결과를 반환합니다. Query는 문 텍스트를 매개변수 바인딩 없이 SQLite에 그대로 전달하므로, 문에 끼워 넣는 값은 모두 이스케이프하십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `sql:Query(query)  -> rows` | SQL 텍스트를 실행하고 결과 행을 반환합니다. |

```lua
local db

function activate()
    db = SQL:OpenSQLDatabase("Databases/Items.db3")
    local rows = db:Query("SELECT * FROM itempool WHERE Slot = 'regular'")
    for i = 1, rows.Count do
        print(rows[i]["Code"])
    end
end
```

## 파일, JSON, INI

### STORAGE

모듈 디렉터리를 루트로 하는 파일 접근과 온라인 로비 코드 공유 헬퍼입니다.

<div class="callout warn">
WriteText는 모듈 폴더 안에만 씁니다. 절대 경로와 폴더 밖으로 해석되는 경로를 거부합니다. ReadText는 절대 경로도 받습니다. 로비 코드 헬퍼는 실행 파일 옆의 공용 `Global/Lobbycodes/` 폴더를 사용합니다. 읽기 전용 모듈도 모든 STORAGE 메서드를 사용할 수 있습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `STORAGE:GetFiles(dir, pattern)  -> string[]` | 모듈 폴더의 하위 디렉터리에서 `"*.png"` 같은 검색 패턴에 맞는 파일을 나열합니다. 항목은 하위 디렉터리를 포함한 모듈 폴더 기준 상대 경로입니다. 디렉터리가 존재해야 합니다. |
| `STORAGE:FileExists(path)  -> bool` | 모듈 폴더 기준 상대 경로에 파일이 있으면 true. |
| `STORAGE:DirectoryExists(path)  -> bool` | 모듈 폴더 기준 상대 경로에 디렉터리가 있으면 true. |
| `STORAGE:WriteText(name, contents)  -> bool` | 하위 디렉터리를 만들면서 모듈 폴더 아래의 파일에 텍스트를 씁니다. 절대 경로나 밖으로 벗어나는 경로, 또는 실패 시 false를 반환합니다. |
| `STORAGE:ReadText(name)  -> string` | 파일(모듈 폴더 기준 상대 경로 또는 절대 경로)의 원시 텍스트를 반환하며, 없거나 읽을 수 없으면 nil을 반환합니다. |
| `STORAGE:GetFullPath(name)  -> string` | 모듈 폴더 아래 파일의 절대 경로를 반환하며, 빈 입력이나 절대 경로 입력이면 nil을 반환합니다. |
| `STORAGE:WriteLobbyCode(name, contents)  -> bool` | `Global/Lobbycodes/`에 파일을 씁니다. 빈 이름, 절대 경로, `..` 이름, 또는 실패 시 false를 반환합니다. |
| `STORAGE:RevealLobbyCodes()  -> bool` | OS 파일 탐색기를 `Global/Lobbycodes/`에서 엽니다. |
| `STORAGE:RevealInExplorer(name)  -> bool` | 지정한 모듈 파일을 선택한 상태로 OS 파일 탐색기를 엽니다(Windows와 macOS). 다른 곳에서는 그 폴더를 엽니다. |

### JSONLOADER

모듈 디렉터리의 JSON 파일과 문자열을 파싱합니다.

<div class="callout warn">
파싱된 값은 .NET 딕셔너리입니다. 객체는 멤버 이름을, 배열은 1..n을 키로 합니다. 없는 키를 직접 인덱싱하면(`d["x"]`) 오류가 발생하므로 실패할 수 있는 조회에는 JsonGet을 사용하십시오. 숫자는 정수나 double이 되고, 문자열, 불리언, null은 Lua 대응물에 대응됩니다. LoadJson은 JsonNode 트리를 반환합니다. `node["member"]`로 인덱싱하고 ExtractNumber / ExtractText로 말단 값을 변환하십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `JSONLOADER:LoadJson(name)  -> JsonNode` | 모듈 디렉터리 기준 상대 경로의 JSON 파일을 JsonNode 트리로 파싱합니다. 파일이 없으면 오류가 발생합니다. |
| `JSONLOADER:ExtractNumber(value)  -> number` | JsonNode 말단을 숫자로 변환합니다. nil이나 숫자가 아닌 값이면 0을 반환합니다. |
| `JSONLOADER:ExtractText(value)  -> string` | JsonNode 말단을 문자열로 변환합니다. nil이면 nil을 반환합니다. |
| `JSONLOADER:JsonParseFile(name)  -> dict` | 루트가 객체인 JSON 파일을 파싱합니다(파일이 비어 있으면 빈 딕셔너리). 파일이 없거나 루트가 객체가 아니면 오류가 발생합니다. |
| `JSONLOADER:JsonParseFileAny(name)  -> dict` | 루트가 객체 또는 배열인 JSON 파일을 파싱합니다(상대 또는 절대 경로). 파일이 없거나 비어 있으면 nil을 반환합니다. |
| `JSONLOADER:JsonParseString(json)  -> dict` | 루트가 객체인 JSON 문자열을 파싱합니다(비어 있으면 빈 딕셔너리). 루트가 객체가 아니면 오류가 발생합니다. |
| `JSONLOADER:JsonParseStringAny(json)  -> dict` | 루트가 객체 또는 배열인 JSON 문자열을 파싱합니다. 비어 있거나 잘못된 입력이면 nil을 반환합니다. |
| `JSONLOADER:JsonGet(dict, key)  -> value` | 객체에서 문자열 키를, 배열에서 정수 키를 조회합니다. 없거나 dict가 파싱된 값이 아니면 nil을 반환합니다. |
| `JSONLOADER:JsonCount(dict)  -> int` | 파싱된 객체나 배열의 멤버 수. 그 외에는 0. |

```lua
local data = JSONLOADER:JsonParseFileAny("Config/layout.json")
local colors = data and JSONLOADER:JsonGet(data, "colors")
local n = JSONLOADER:JsonCount(colors)
for i = 1, n do
    print(JSONLOADER:JsonGet(colors, i))
end
```

### INILOADER

모듈 디렉터리에서 평면적인 `key=value` 파일을 로드합니다.

<div class="callout warn">
로더는 각 줄을 첫 `=`에서 나누고, `=`가 없는 줄은 건너뛰며, 반복되는 키는 이전 값을 덮어쓰게 합니다. 섹션, 주석, 인용은 없습니다. 없는 파일은 빈 핸들이 됩니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `INILOADER:LoadIni(name)  -> ini` | 모듈 디렉터리 기준 상대 경로의 `key=value` 파일을 읽습니다. |

### INI 핸들

INILOADER:LoadIni가 반환하는, 타입별 getter가 있는 파싱된 INI 파일입니다.

<div class="callout warn">
getter는 키가 없으면 제공된 기본값을 반환합니다. 키는 있지만 값을 파싱할 수 없으면 숫자 getter는 0을 반환합니다. 배열 getter는 쉼표로 나누며 키가 없으면 빈 배열을 반환합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `ini:GetBool(key, default)  -> bool` | 값이 정수 1로 파싱되면 true. 키가 없으면 기본값. |
| `ini:GetInt(key, default)  -> int` | 값을 정수로. |
| `ini:GetDouble(key, default)  -> number` | 값을 double로. |
| `ini:GetString(key, default)  -> string` | 원시 문자열 값. |
| `ini:GetStringArray(key)  -> string[]` | 쉼표로 나눈 값. |
| `ini:GetIntArray(key)  -> int[]` | 쉼표로 나누고 각 부분을 정수로 파싱한 값(파싱할 수 없으면 0). |
| `ini:GetDoubleArray(key)  -> double[]` | 쉼표로 나누고 각 부분을 double로 파싱한 값(파싱할 수 없으면 0). |

## 공유 리소스와 설정

### SHARED

스테이지 전환 후에도 남는 게임 전체의 텍스처, 소리, 문자열 저장소로, 한 번(예: 부팅 시) 로드한 리소스를 모든 모듈이 사용할 수 있게 합니다.

<div class="callout warn">
Set* 메서드는 백그라운드 스레드에서 로드하고 렌더 스레드에서 리소스를 교체합니다. 선택적 onCreate 콜백은 새 핸들이 자리를 잡으면 이를 받습니다. 같은 키에 대한 더 새로운 Set*는 진행 중인 로드를 버립니다. 교체는 이전 리소스를 해제하므로 리로드 전에 가져온 핸들은 무효가 됩니다. Get*로 다시 가져오십시오. UsingAbsolutePath 변형은 전체 경로를 받습니다. 텍스처 핸들은 그래픽과 텍스트를, 사운드 핸들은 오디오를 참고하십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `SHARED:SetSharedString(key, value)  -> nil` | 키 아래에 문자열을 저장합니다. |
| `SHARED:GetSharedString(key)  -> string` | 키 아래에 저장된 문자열을 반환하며, 없으면 빈 문자열. |
| `SHARED:GetSharedTexture(key)  -> texture` | 키의 공유 텍스처를 반환하며, 설정된 것이 없으면 빈 텍스처. |
| `SHARED:GetSharedSound(key)  -> sound` | 키의 공유 사운드를 반환하며, 설정된 것이 없으면 빈 사운드. |
| `SHARED:ClearSharedTexture(key)  -> nil` | 키 아래에 저장된 텍스처를 해제하고 빈 것으로 대체합니다. |
| `SHARED:ClearSharedSound(key)  -> nil` | 키 아래에 저장된 사운드를 해제하고 빈 것으로 대체합니다. |
| `SHARED:SetSharedTexture(key, path, onCreate?)  -> nil` | 모듈 기준 상대 경로에서 텍스처를 저장소로 로드합니다. |
| `SHARED:SetSharedTexture(key, path, options, onCreate?)  -> nil` | 같지만 options 테이블을 받습니다. `{ maxSize = N }`은 디코드된 텍스처의 긴 변을 N픽셀로 제한합니다. |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, onCreate?)  -> nil` | 절대 경로에서 텍스처를 로드합니다. |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, options, onCreate?)  -> nil` | options 테이블과 함께 절대 경로에서 텍스처를 로드합니다. |
| `SHARED:SetSharedSFX(key, path, onCreate?)  -> nil` | 모듈 기준 상대 경로에서 효과음을 로드합니다. |
| `SHARED:SetSharedBGM(key, path, onCreate?)  -> nil` | 모듈 기준 상대 경로에서 배경 음악(곡 재생 볼륨 그룹)을 로드합니다. |
| `SHARED:SetSharedVoice(key, path, onCreate?)  -> nil` | 모듈 기준 상대 경로에서 음성 클립을 로드합니다. |
| `SHARED:SetSharedPreview(key, path, onCreate?)  -> nil` | 모듈 기준 상대 경로에서 곡 미리 듣기 클립을 로드합니다. |
| `SHARED:SetSharedSFXUsingAbsolutePath(key, path, onCreate?)  -> nil` | 절대 경로에서 효과음을 로드합니다. |
| `SHARED:SetSharedBGMUsingAbsolutePath(key, path, onCreate?)  -> nil` | 절대 경로에서 배경 음악을 로드합니다. |
| `SHARED:SetSharedVoiceUsingAbsolutePath(key, path, onCreate?)  -> nil` | 절대 경로에서 음성 클립을 로드합니다. |
| `SHARED:SetSharedPreviewUsingAbsolutePath(key, path, onCreate?)  -> nil` | 절대 경로에서 곡 미리 듣기 클립을 로드합니다. |

```lua
-- 부트 스테이지에서
function onStart()
    SHARED:SetSharedSFX("Decide", "Sounds/Decide.ogg")
end

-- 이후의 어느 모듈에서든
SHARED:GetSharedSound("Decide"):Play()
```

### CONFIG

게임 설정을 읽고 바꿉니다. 플레이어 수, 모드, 스코어링, 플레이어별 게임플레이 모드, 볼륨 수준입니다.

<div class="callout warn">
프로퍼티는 점 문법(`CONFIG.PlayerCount`), 메서드는 콜론 문법을 사용합니다. 플레이어 인덱스는 0부터 시작합니다(0에서 4). setter는 범위 밖 인덱스를 무시하고 getter는 그에 대해 기본값을 반환합니다. 읽기 전용 모듈(ROActivity와 배경) 안에서는 모든 setter가 오류를 기록하고 아무것도 하지 않습니다. 변경은 메모리에 즉시 적용되며, 게임은 정상 종료될 때 Config.ini를 기록합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `CONFIG.ConfigIsNew  -> bool (read-only)` | 이번 부팅에서 게임이 설정을 생성했으면 true. |
| `CONFIG.Language  -> string (read-only)` | Config.ini에 저장된 언어 id. |
| `CONFIG.PlayerCount  -> int` | 활성 플레이어 수. setter는 1..5 밖의 값을 무시합니다. |
| `CONFIG.IsAIBattleMode  -> bool` | AI 배틀 모드가 켜져 있는지 여부. |
| `CONFIG.AILevel  -> int` | AI 난이도 레벨. 쓸 때 1..10으로 제한됩니다. |
| `CONFIG.IsTrainingMode  -> bool` | 트레이닝 모드가 켜져 있는지 여부. |
| `CONFIG.UseModernScoringMethod  -> bool` | 게임이 현대식(신우치) 스코어링 방식을 사용하는지 여부. |
| `CONFIG.UsedLegacyScoringMethod  -> int` | 레거시 스코어링 세대(`CONFIG.LEGACY_SCORING` 참고). 쓸 때 0..3으로 제한됩니다. |
| `CONFIG.AreSongUnlockablesDisabled  -> bool (read-only)` | 게임이 곡 잠금 해제 조건을 무시하는지 여부. |
| `CONFIG.SongSpeed  -> int` | 곡 속도(배율의 1/20 단위: 20이 1.0x). 쓸 때 2..200(0.1x에서 10x)으로 제한됩니다. |
| `CONFIG.MasterVolume  -> int` | 마스터 볼륨. 쓸 때 0..100으로 제한됩니다. |
| `CONFIG.SoundEffectVolume  -> int` | 효과음 볼륨. 쓸 때 0..100으로 제한됩니다. |
| `CONFIG.VoiceVolume  -> int` | 음성 볼륨. 쓸 때 0..100으로 제한됩니다. |
| `CONFIG.SongVolume  -> int` | 곡 재생 볼륨. 쓸 때 0..100으로 제한됩니다. |
| `CONFIG.PreviewVolume  -> int` | 곡 미리 듣기 볼륨. 쓸 때 0..100으로 제한됩니다. |
| `CONFIG:GetGameType(player)  -> int` | 플레이어의 게임 타입(`CONFIG.GAMETYPE` 참고). 범위 밖 인덱스는 Taiko. |
| `CONFIG:SetGameType(player, gameType)  -> nil` | 플레이어의 게임 타입을 설정하고 정의되지 않은 값은 무시합니다. |
| `CONFIG:GetDefaultCourse(player)  -> int` | 기본 난이도(`CONFIG.DEFAULT_COURSE` 참고). 범위 밖 인덱스는 Normal. player 인자와 무관하게 전역 설정 하나가 모든 플레이어에 적용됩니다. |
| `CONFIG:SetDefaultCourse(player, difficulty)  -> nil` | 전역 기본 난이도를 설정합니다. Easy에서 Extra Extreme 다음 하나(Extra/Extra-Extra 통합 표시)까지로 제한됩니다. |
| `CONFIG:GetScrollSpeed(player)  -> int` | 플레이어의 스크롤 속도 값: 9가 1.0x이고 한 단계가 0.1x입니다(`CONFIG.SCROLLSPEED` 참고). 범위 밖 인덱스는 9. |
| `CONFIG:SetScrollSpeed(player, speed)  -> nil` | 플레이어의 스크롤 속도 값을 설정합니다. 0..99로 제한됩니다. |
| `CONFIG:GetTimingZone(player)  -> int` | 플레이어의 판정 범위: 0 Loose, 1 Lenient, 2 Normal, 3 Strict, 4 Rigorous. 범위 밖 인덱스는 2. |
| `CONFIG:SetTimingZone(player, zone)  -> nil` | 플레이어의 판정 범위를 설정합니다. 0..4로 제한됩니다. |
| `CONFIG:GetAutoStatus(player)  -> bool` | 플레이어가 오토 플레이 중이거나 리플레이를 보고 있으면 true. |
| `CONFIG:SetAutoStatus(player, isAuto)  -> nil` | 플레이어의 오토 플레이를 켜거나 끕니다. |
| `CONFIG:GetRandomMod(player)  -> int` | 플레이어의 랜덤 모드(`CONFIG.RANDOM` 참고). 범위 밖 인덱스는 Off. |
| `CONFIG:SetRandomMod(player, mode)  -> nil` | 플레이어의 랜덤 모드를 설정하고 정의되지 않은 값은 무시합니다. |
| `CONFIG:GetFunMod(player)  -> int` | 플레이어의 펀 모드(`CONFIG.FUN` 참고). 범위 밖 인덱스는 None. |
| `CONFIG:SetFunMod(player, mod)  -> nil` | 플레이어의 펀 모드를 설정하고 정의되지 않은 값은 무시합니다. |
| `CONFIG:GetStealthMod(player)  -> int` | 플레이어의 스텔스 모드(`CONFIG.STEALTH` 참고). 범위 밖 인덱스는 Off. |
| `CONFIG:SetStealthMod(player, mode)  -> nil` | 플레이어의 스텔스 모드를 설정하고 정의되지 않은 값은 무시합니다. |
| `CONFIG:GetJusticeMod(player)  -> int` | 플레이어의 판정 모드: 0 꺼짐, 1 Just(Ok가 Bad로 처리됨), 2 Safe(Bad가 Ok로 처리됨). |
| `CONFIG:SetJusticeMod(player, mode)  -> nil` | 플레이어의 판정 모드를 설정합니다. 0..2로 제한됩니다. |
| `CONFIG:GetModFlags(player)  -> integer` | 스크롤 속도, 스텔스, 랜덤, 곡 속도, 판정 범위, 판정, 펀 모드를 64비트 값 하나(각 1바이트)로 묶습니다. |
| `CONFIG:SetModFlags(player, flags)  -> nil` | GetModFlags가 만든 값을 플레이어에게 다시 적용합니다(곡 속도는 전역). |

CONFIG의 상수 테이블은 위의 정수 값에 이름을 붙입니다. `SONGSPEED`와 `SCROLLSPEED`는 저장 값과 배율 사이의 변환도 제공합니다.

| 멤버 | 설명 |
| --- | --- |
| `CONFIG.SONGSPEED.Normal  -> int` | 20, 1.0x의 저장 값. |
| `CONFIG.SONGSPEED:ToActual(value)  -> number` | 저장된 곡 속도를 배율로 변환합니다(value / 20). |
| `CONFIG.SONGSPEED:FromActual(multiplier)  -> int` | 배율을 가장 가까운 저장 곡 속도로 변환합니다. |
| `CONFIG.SCROLLSPEED.Normal  -> int` | 9, 1.0x의 저장 값. |
| `CONFIG.SCROLLSPEED:ToActual(value)  -> number` | 저장된 스크롤 속도를 배율로 변환합니다((value + 1) / 10). |
| `CONFIG.SCROLLSPEED:FromActual(multiplier)  -> int` | 배율을 가장 가까운 저장 스크롤 속도로 변환합니다. |
| `CONFIG.GAMETYPE` | `Taiko`, `Konga`. |
| `CONFIG.DEFAULT_COURSE` | `Easy`, `Normal`, `Hard`, `Oni`, `Edit`. |
| `CONFIG.LEGACY_SCORING` | `Gen1Oni`, `Gen1_2`, `Gen2`, `Gen3`. |
| `CONFIG.RANDOM` | `Off`, `Random`, `Mirror`, `SuperRandom`, `MirrorRandom`. |
| `CONFIG.STEALTH` | `Off`, `Doron`, `Stealth`. |
| `CONFIG.FUN` | `None`, `Avalanche`, `Minesweeper`, `DynamicBeat`, `Total`. |
| `CONFIG.JUSTICE` | `None`, `Just`, `Safe`. |

```lua
local multiplier = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
if CONFIG:GetRandomMod(0) == CONFIG.RANDOM.Mirror then
    -- 미러 채보
end
```
