<!-- api/aviutl2.md -->

# AviUtl2 프로젝트

`AVIUTL2` 전역은 AviUtl2 동영상 편집기로 저장한 프로젝트 파일(`.aup2`)을 재생합니다. 게임은 프로젝트를 읽고 그 오브젝트, 이동, 효과를 실시간으로 그립니다. 따라서 스킨은 렌더링된 비디오 대신 프로젝트와 그 이미지로 애니메이션을 담을 수 있습니다.

프로젝트 핸들은 그 프로젝트를 로드한 스크립트가 소유하며, 게임은 그 스크립트를 언로드할 때 핸들을 해제합니다. 핸들을 더 일찍 해제하려면 직접 `Dispose()`를 호출하십시오.

<div class="callout warn">
게임은 자체 렌더러로 프로젝트를 그립니다. 위치, 크기, 회전, 이동 곡선, 카메라 제어, 자주 쓰이는 효과는 편집기와 같은 결과를 냅니다. 무작위 이동과 흔들림은 다른 난수 값을 사용하고, 일부 효과는 근사치입니다. 프로젝트를 로드한 뒤 GetUnsupported와 GetApproximated를 읽어 게임이 빠뜨렸거나 단순화한 것을 확인하십시오.
</div>

## 로드

### AVIUTL2

| 메서드 | 설명 |
| --- | --- |
| `AVIUTL2:LoadProject(path)  -> project` | 프로젝트 파일을 열고 핸들을 반환합니다. 경로는 스크립트 디렉터리 기준 상대 경로입니다. |
| `AVIUTL2:LoadProjectFromAbsolutePath(path)  -> project` | 전체 경로에서 프로젝트 파일을 엽니다. |

<div class="callout warn">
프로젝트에는 저장한 컴퓨터의 전체 경로가 기록됩니다. 게임은 각 이미지, 비디오, 사운드, 모델을 프로젝트 파일 근처에서 찾습니다. 먼저 기록된 경로와 프로젝트 자신의 경로가 공유하는 폴더를 기준으로 찾고, 그다음 파일 이름으로 찾습니다. 프로젝트의 파일은 프로젝트 폴더나 그 하위 폴더에 두십시오. 프로젝트가 없거나 읽을 수 없으면 Loaded가 false인 핸들이 반환됩니다.
</div>

## 프로젝트 핸들

`AVIUTL2:LoadProject`가 반환하는 프로젝트입니다. 재생 메서드의 이름은 비디오 핸들의 것과 같습니다.

### 재생

| 메서드 | 설명 |
| --- | --- |
| `project:Start()  -> nil` | 처음부터 재생을 시작합니다. |
| `project:Resume()  -> nil` | Pause 뒤 재생을 재개합니다. |
| `project:Pause()  -> nil` | 사운드를 포함해 재생을 일시정지합니다. |
| `project:Stop()  -> nil` | 재생을 멈춥니다. |
| `project:Reset()  -> nil` | 처음으로 되돌립니다. |
| `project.IsPlaying  -> bool` | 프로젝트가 재생 중이고 아직 끝에 도달하지 않은 동안 true. |
| `project:IsFinished()  -> bool` | 반복하지 않는 프로젝트가 끝에 도달하면 true. |
| `project:GetTimestampMs()  -> number` | 현재 위치(밀리초). |
| `project:SetTimestampMs(ms)  -> nil` | 밀리초 단위 위치로 탐색합니다. |
| `project:GetFrame()  -> number` | 현재 위치(프로젝트의 프레임 단위). |
| `project:SetFrame(frame)  -> nil` | 지정한 프레임으로 탐색합니다. |
| `project:GetSpeed()  -> number` | 현재 재생 속도 배율(1 = 보통). |
| `project:SetSpeed(speed)  -> nil` | 재생 속도 배율을 설정합니다. |
| `project:GetLoop()  -> bool` | 프로젝트가 끝에서 처음부터 다시 시작하도록 설정되어 있으면 true. |
| `project:SetLoop(loop)  -> nil` | 프로젝트가 끝에서 처음부터 다시 시작하게 합니다. |
| `project:GetVolumePercent()  -> number` | 프로젝트 사운드의 볼륨(퍼센트). |
| `project:SetVolumePercent(vol)  -> nil` | 프로젝트 사운드의 볼륨을 퍼센트로 설정합니다. |
| `project:Dispose()  -> nil` | 프로젝트와 그 이미지, 비디오, 사운드를 해제합니다. |

### 그리기

| 메서드 | 설명 |
| --- | --- |
| `project:Draw(x, y)  -> nil` | 현재 프레임을 왼쪽 위 모서리가 (x, y)에 오도록, SetScale로 설정한 배율로 그립니다. |
| `project:DrawFit(x, y, w, h)  -> nil` | 현재 프레임을 비율을 유지한 채 상자에 들어가는 가장 큰 크기로, 상자 가운데에 그립니다. |
| `project:SetScale(scaleX, scaleY)  -> nil` | Draw가 사용하는 배율을 설정합니다(1 = 프로젝트 본래 크기). |
| `project:SetOpacity(opacity)  -> nil` | 프레임 전체의 불투명도를 0에서 1 사이로 설정합니다. |
| `project:SetBackground(r, g, b, a)  -> nil` | 오브젝트 뒤의 프레임을 한 가지 색으로 채웁니다. 각 값은 0에서 255입니다. 알파가 0이면 프레임은 투명하게 남습니다. |
| `project:ClearBackground()  -> nil` | 프로젝트 파일이 지정한 배경으로 되돌립니다. |
| `project.HasProjectBackground  -> bool` | 씬에 자체 배경색이 있으면 true. |
| `project.ProjectBackground  -> int` | 씬의 배경색(0xRRGGBB). 배경색이 없으면 -1. |

<div class="callout warn">
배경색이 없는 씬은 오브젝트가 그려지지 않은 곳이 투명합니다. 그래서 프로젝트는 스크립트가 먼저 그린 것 위에 그려집니다. 편집기는 이런 씬을 검은색 위에 표시합니다. 같은 화면을 얻으려면 먼저 검은 상자를 그리거나 SetBackground(0, 0, 0, 255)를 호출하십시오.
</div>

### 스크립트가 그리는 이미지

스크립트는 프로젝트의 이미지를 자신의 그리기로 대체할 수 있습니다. 예를 들어 움직이는 프레임 안에 플레이어의 네임플레이트를 표시할 수 있습니다.

| 메서드 | 설명 |
| --- | --- |
| `project:SetImageDrawer(fileName, width, height, draw)  -> nil` | 이미지 파일 `fileName`을 표시하는 모든 오브젝트가 함수 `draw`가 그린 것을 표시합니다. 게임은 매 프레임 `draw`를 호출하며, 그리기 영역은 `width` × `height` 크기이고 왼쪽 위 모서리가 (0, 0)입니다. |
| `project:ClearImageDrawer(fileName)  -> nil` | 오브젝트가 다시 이미지 파일을 표시합니다. |

게임은 그런 오브젝트가 있는 레이어마다 `draw(rank, layer)`를 한 번씩 호출합니다. `rank`는 그 레이어들을 맨 위 레이어부터 0에서 시작해 센 번호이고, `layer`는 프로젝트에서의 레이어 번호입니다. `draw` 안에서는 평소의 그리기 호출을 사용하십시오. 게임이 그 호출을 이미지 안으로 돌리며, 그 이미지는 오브젝트와 함께 이동하고 크기가 바뀌고 페이드됩니다.

<div class="callout warn">
함수는 SetImageDrawer를 호출하는 스크립트에 속해야 합니다. `draw` 안에서 그린 부드러운 가장자리는 화면에 그릴 때보다 약간 가늘게 나옵니다.
</div>

### 프로젝트 읽기

| 메서드 | 설명 |
| --- | --- |
| `project.Loaded  -> bool` | 파일을 읽었으면 true. |
| `project.Width  -> int` | 씬의 픽셀 너비. 아무것도 로드되지 않았으면 -1. |
| `project.Height  -> int` | 씬의 픽셀 높이. 아무것도 로드되지 않았으면 -1. |
| `project.FrameRate  -> number` | 씬의 초당 프레임 수. |
| `project.FrameCount  -> int` | 씬의 길이(프레임). |
| `project.DurationMs  -> number` | 씬의 길이(밀리초). |
| `project.SceneCount  -> int` | 프로젝트의 씬 수. |
| `project:GetScene()  -> int` | 재생 중인 씬의 인덱스. |
| `project:SetScene(index)  -> nil` | 프로젝트의 다른 씬을 처음부터 재생합니다. |
| `project:GetSceneName(index)  -> string` | 씬의 이름. |
| `project.ObjectCount  -> int` | 씬의 오브젝트 수. |
| `project:GetProjectValue(key)  -> string` | 프로젝트의 설정값. 파일에 적힌 그대로의 문자열입니다. |
| `project:GetSceneValue(key)  -> string` | 재생 중인 씬의 설정값. |
| `project:GetValue(section, key)  -> string` | 파일의 임의 섹션에 있는 설정값. 섹션 이름으로 지정합니다(오브젝트는 `"12"`, 그 두 번째 효과는 `"12.1"`). |
| `project:GetValueAtFrame(objectId, effect, key, frame)  -> number` | 오브젝트의 효과가 가진 숫자 설정의 특정 프레임에서의 값. 이동이 적용된 값입니다. |

### 그려지는 내용 확인

| 메서드 | 설명 |
| --- | --- |
| `project.UnsupportedCount  -> int` | 프로젝트가 사용하지만 게임이 그리지 않는 것의 수. 없는 파일도 포함합니다. |
| `project:GetUnsupported()  -> string` | 그 목록. 한 줄에 하나씩입니다. |
| `project.ApproximatedCount  -> int` | 게임이 단순화해서 그리는 것의 수. |
| `project:GetApproximated()  -> string` | 그 목록. 한 줄에 하나씩입니다. |
| `project:AddScriptFolder(path)  -> nil` | 프로젝트의 효과가 이름으로 참조할 수 있는 AviUtl 스크립트 파일 폴더를 추가합니다. 프로젝트 파일 옆의 `Script` 폴더는 게임이 자동으로 읽습니다. |
| `project.ScriptErrorCount  -> int` | 실패한 스크립트 파일의 수. |
| `project:GetScriptErrors()  -> string` | 실패한 각 스크립트의 첫 번째 오류. 한 줄에 하나씩입니다. |
| `project.GraphicsMemoryMB  -> number` | 프로젝트가 자체 그리기를 위해 현재 보유한 그래픽 메모리(메가바이트). |
| `project:SetSurfaceBudgetMB(mb)  -> nil` | 프로젝트가 프레임 사이에 재사용하려고 보유하는 그래픽 메모리의 양을 설정합니다. |

<div class="callout warn">
프로젝트와 함께 배포된 스크립트 파일은 닫힌 환경에서 실행됩니다. 파일을 읽거나 쓸 수 없고, 프로그램을 시작하거나 네이티브 모듈을 로드할 수 없습니다.
</div>

```lua
local intro
local box

function onStart()
    intro = AVIUTL2:LoadProject("Intro/intro.aup2")
    box = TEXTURE:CreateTexture("Intro/player_box.png")
    -- 각 플레이어의 네임플레이트. 프로젝트의 플레이어 상자 안에 그려집니다
    intro:SetImageDrawer("player_box.png", 520, 216, function(rank)
        box:Draw(0, 0)
        NAMEPLATE:DrawPlayerNameplate(170, 88, 255, rank)
    end)
end

function activate()
    intro:SetBackground(0, 0, 0, 255)
    intro:Start()
end

function draw()
    intro:DrawFit(0, 0, 1920, 1080)
    if intro:IsFinished() then Exit("title", nil) end
end

function deactivate()
    intro:Stop()
end

function onDestroy()
    intro:Dispose()
    box:Dispose()
end
```
