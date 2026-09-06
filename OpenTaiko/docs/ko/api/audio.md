<!-- api/audio.md -->

# 오디오

소리를 로드하고 재생하며, 설치된 히트사운드 세트를 읽습니다.

## SOUND

오디오 파일에서 사운드 핸들을 만듭니다. 각 팩토리 메서드는 소리를 볼륨 그룹(효과음, 음성, 곡 재생, 곡 미리 듣기)에 배정하며, 그룹이 어떤 사용자 볼륨 설정이 적용되는지를 결정합니다.

<div class="callout warn">
상대 경로는 스크립트 디렉터리를 기준으로 해석됩니다. FromAbsolutePath 변형은 전체 경로를 받습니다. 반환된 핸들은 렌더 스레드가 이후 프레임에 걸쳐 스트림을 만드는 동안 빈 상태로 시작합니다. 핸들은 그 전에 호출한 Play와 Set* 호출을 큐에 넣어 두었다가 스트림이 준비되면 적용합니다. 게임은 스크립트를 언로드할 때 핸들을 해제합니다. 더 일찍 해제하려면 Dispose를 호출하십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `SOUND:CreateSFX(path)  -> sound` | 효과음을 로드합니다. |
| `SOUND:CreateVoice(path)  -> sound` | 음성 클립을 로드합니다. |
| `SOUND:CreateBGM(path)  -> sound` | 배경 음악을 로드합니다(곡 재생 그룹). |
| `SOUND:CreatePreview(path)  -> sound` | 곡 미리 듣기 클립을 로드합니다(곡 미리 듣기 그룹). |
| `SOUND:CreateSFXFromAbsolutePath(path)  -> sound` | 전체 경로에서 효과음을 로드합니다. |
| `SOUND:CreateVoiceFromAbsolutePath(path)  -> sound` | 전체 경로에서 음성 클립을 로드합니다. |
| `SOUND:CreateBGMFromAbsolutePath(path)  -> sound` | 전체 경로에서 배경 음악을 로드합니다. |
| `SOUND:CreatePreviewFromAbsolutePath(path)  -> sound` | 전체 경로에서 곡 미리 듣기 클립을 로드합니다. |

```lua
local bgm, decide

function onStart()
    bgm = SOUND:CreateBGM("Sounds/BGM.ogg")
    bgm:SetLoop(true)
    decide = SOUND:CreateSFX("Sounds/Decide.ogg")
end

function activate()
    bgm:Play()
end

function update()
    if INPUT:Pressed("Decide") then decide:Play() end
end

function deactivate()
    bgm:Stop()
end

function onDestroy()
    bgm:Dispose()
    decide:Dispose()
end
```

## 사운드 핸들

`SOUND` 팩토리 메서드가 반환하는 재생 가능한 소리입니다.

<div class="callout warn">
스트림이 준비될 때까지 핸들은 Play와 모든 Set* 호출을 버퍼링하고 getter는 아래에 나열된 기본값을 반환합니다. Volume은 백분율입니다(100 = 파일 자체의 레벨). Pan은 -100(왼쪽)에서 0(가운데)을 거쳐 100(오른쪽)까지입니다. 위치와 길이는 밀리초 단위입니다. Play는 스트림을 처음부터 다시 시작하므로 Play를 호출한 뒤에 탐색하십시오.
</div>

| 메서드 | 설명 |
| --- | --- |
| `sound:Play()  -> nil` | 처음부터 재생을 시작(또는 재시작)합니다. |
| `sound:Start()  -> nil` | Play와 같습니다. |
| `sound:Pause()  -> nil` | 재생을 일시정지합니다. |
| `sound:Resume()  -> nil` | Pause 뒤 재생을 재개합니다. |
| `sound:Stop()  -> nil` | 재생을 멈춥니다. |
| `sound:Reset()  -> nil` | 처음으로 되돌립니다. |
| `sound:SetLoop(loop)  -> nil` | 루프를 켜거나 끕니다. |
| `sound:GetLoop()  -> bool` | 루프가 켜져 있는지 반환합니다. |
| `sound:SetVolumePercent(vol)  -> nil` | 볼륨을 퍼센트로 설정합니다. |
| `sound:GetVolumePercent()  -> number` | 볼륨을 퍼센트로 반환합니다(로드되지 않았으면 100). |
| `sound:SetVolume(vol)  -> nil` | SetVolumePercent의 옛 이름(정수). |
| `sound:SetPan(pan)  -> nil` | 스테레오 팬을 설정합니다, -100..100. |
| `sound:GetPan()  -> int` | 현재 팬을 반환합니다(로드되지 않았으면 0). |
| `sound:SetSpeed(speed)  -> nil` | 재생 속도 배율을 설정합니다(1 = 보통). |
| `sound:GetSpeed()  -> number` | 재생 속도 배율을 반환합니다(로드되지 않았으면 1). |
| `sound:SetTimestampMs(ms)  -> nil` | 밀리초 단위 위치로 탐색합니다. |
| `sound:SetTimestamp(ms)  -> nil` | SetTimestampMs의 옛 이름. |
| `sound:GetTimestampMs()  -> number` | 현재 위치를 밀리초로 반환합니다(로드되지 않았으면 0). |
| `sound.DurationMs  -> number` | 전체 길이(밀리초)(로드되지 않았으면 0). |
| `sound:GetDurationMs()  -> number` | DurationMs의 옛 이름. |
| `sound.Loaded  -> bool` | 렌더 스레드가 스트림을 만들면 true. |
| `sound.IsPlaying  -> bool` | 소리가 재생 중인 동안 true. |
| `sound:IsFinished()  -> bool` | 재생이 끝에 도달하면 true(일시정지 중이거나 로드되지 않았으면 false). |
| `sound.Path  -> string` | 로드된 파일의 전체 경로(로드될 때까지 빈 문자열). |
| `sound:Dispose()  -> nil` | 소리를 해제합니다. |

## HITSOUNDSLIST

현재 스킨에 설치된 히트사운드 세트의 읽기 전용 목록입니다(게임이 `Global/HitSounds/`에서 읽음).

<div class="callout warn">
모든 스크립트에서 사용할 수 있습니다. 인덱스는 0부터 시작합니다. GetByName은 세트의 폴더 이름을 대소문자 구분 없이 대조합니다. 두 조회 모두 일치하는 것이 없으면 nil을 반환합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `HITSOUNDSLIST.Count  -> int` | 히트사운드 세트 수. |
| `HITSOUNDSLIST:GetByIndex(index)  -> hitsoundEntry` | 지정한 0부터 시작하는 인덱스의 항목을 반환하며, 없으면 nil. |
| `HITSOUNDSLIST:GetByName(name)  -> hitsoundEntry` | 폴더 이름이 일치하는 항목을 반환하며, 없으면 nil. |

```lua
local don

function onStart()
    for i = 0, HITSOUNDSLIST.Count - 1 do
        local hs = HITSOUNDSLIST:GetByIndex(i)
        debugLog(hs.FolderName .. " -> " .. hs.DisplayName)
    end
    local taiko = HITSOUNDSLIST:GetByName("Taiko")
    if taiko then
        don = SOUND:CreateSFXFromAbsolutePath(taiko.DonPath)
    end
end

function onDestroy()
    if don then don:Dispose() end
end
```

## 히트사운드 항목

`HITSOUNDSLIST:GetByIndex` 또는 `GetByName`이 반환하는 히트사운드 세트 하나입니다. 모든 멤버는 읽기 전용입니다.

| 메서드 | 설명 |
| --- | --- |
| `hitsoundEntry.FolderName  -> string` | 세트의 폴더 이름(예: `"Taiko"`). |
| `hitsoundEntry.DisplayName  -> string` | 지역화된 표시 이름. 없으면 폴더 이름으로 대체됩니다. |
| `hitsoundEntry.DonPath  -> string` | 세트의 동(Don) 소리 파일의 전체 경로. |
| `hitsoundEntry.KaPath  -> string` | 세트의 카(Ka) 소리 파일의 전체 경로. |
