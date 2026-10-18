<!-- modules/world.md -->

# World <span class="badge-exp">실험적</span>

## World.OWM3d

마이 룸과 기본 제공 스킨의 스토리 장면을 움직이는 3D 월드 엔진으로, 엔진의 [래스터라이저 월드](../api/3d.md)와 [물리](../api/3d-physics.md) 위에 만들어졌습니다. 맵 데이터를 플레이할 수 있는 장소로 바꿉니다. 충돌이 일치하는 격자 또는 하이트맵 지형, glTF 모델, 충돌하며 미끄러지는 캐릭터, 벽을 뚫지 않는 카메라, 트리거, 낮과 밤의 순환, 날씨, 물.

<div class="callout">
OWM3d는 그 바탕인 3D 엔진과 마찬가지로 실험적입니다. 함수, 맵 형식, 기본값이 사용 중단 기간 없이 릴리스 사이에 바뀔 수 있습니다. 이를 쓰는 스킨은 업데이트마다 다시 테스트하십시오.
</div>

`require("World.OWM3d")`는 스테이지가 직접 만드는 부품을 담은 파사드를 반환합니다.

| 필드 | 내용 |
| --- | --- |
| `World` | 월드(`World.OWM3d.core`): `World.new{ rw, rh, screenW, screenH, gridW, gridH, lit, hour, yaw, pitch, fov, dist }`가 장면과 모든 하위 시스템을 만듭니다. |
| `Camera` | 카메라 리그(`iso`, `interior`, `orbit`, 또는 `yaw`, `pitch`, `fov`, `dist`의 테이블), 따라가기, 궤도 회전, 줌, 그리고 충돌 붐. |
| `Json` | 맵 파일 리더: `Json.loadMap(dir)`은 `<dir>/map.json`을 기본값을 적용한 일반 Lua 테이블로 읽습니다. |
| `IsoMap` | 격자 맵: 셀 격자의 바닥, 경사로, 벽, 건물을 같은 삼각형의 충돌과 함께 만듭니다. |
| `Terrain` | 하이트맵 맵: 높이 필드로 만든 메시, 스플랫 텍스처링, 풀. |
| `NPC` | 집 지점 주위를 돌아다니거나, 대기하거나, 순찰하는 캐릭터로, 대화하는 동안 플레이어를 바라봅니다. |
| `ModelIcon` | 3D 모델의 썸네일. 작은 장면에 한 번 렌더링한 뒤 텍스처처럼 그립니다(카탈로그와 숍의 아이콘). |
| `version` | 파사드의 버전 번호(1). |

`World.new`로 만든 월드는 나머지를 필드로 가집니다: `world.phys`(이름 붙은 콜라이더 그룹, 캐릭터 컨트롤러, 레이캐스트), `world.models`(모델 인스턴스), `world.triggers`(그룹 전환, 카메라 리그 변경, 순간 이동, 맵 변경, 함수 실행을 하는 볼륨), `world.particles`, `world.weather`(`clear`, `rain`, `storm`, `snow`, `sakura`, `embers`), `world.water`, `world.daynight`, `world.cam`. 각 부품은 그 자체로도 `World.OWM3d.<part>`라는 모듈입니다.

```lua
local OWM = require("World.OWM3d")
local world, last = nil, 0

function onStart()
    world = OWM.World.new{ rw = 1280, rh = 720, screenW = 1920, screenH = 1080, lit = true }
    world:registerMap("plaza", { type = "folder", dir = "maps/plaza" })
    world:loadMap("plaza", "default")
end

function update(ts)
    local dt = (ts - last) / 1000; last = ts
    world:update(dt, px, py, pz)   -- the player's position drives the triggers and the camera
end

function draw()
    world:render()
    world:blit()
end
```

맵 폴더, 모델, 텍스처는 스테이지의 파일이므로, 상대 경로는 호출한 스테이지의 폴더를 기준으로 해석됩니다. 스테이지가 월드를 다 쓰면 `world:dispose()`를 호출하십시오.
