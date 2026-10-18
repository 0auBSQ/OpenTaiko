<!-- modules/characters.md -->

# Characters

## Characters.Student

플레이어가 조종하는 스토리의 학생(A 또는 B)과 그 학생의 색 팔레트로, 모든 스토리 장면이 같은 학생을 같은 색으로 그리게 합니다. 학생은 `Global/Characters/`의 캐릭터 `02v2 - Student (A)`와 `03v2 - Student (B)`입니다.

모든 것은 세이브 파일에 보관됩니다.

| 세이브 키 | 종류 | 내용 |
| --- | --- | --- |
| `story_student` | 카운터 | 고른 학생: 1은 B이고, 설정되지 않은 경우를 포함해 그 밖의 값은 A로 읽힙니다. |
| `.character_palette_<folder>` | 카운터 | 학생 캐릭터의 0부터 시작하는 팔레트 인덱스. 캐릭터 커스터마이즈 화면이 기록하는 것과 같은 카운터이므로 둘은 항상 일치합니다. A와 B는 각자 자기 값을 가집니다. |
| `.character_playcount_<folder>` | 카운터 | 그 캐릭터로 한 플레이 횟수로, 팔레트를 잠금 해제합니다. |

학생의 팔레트는 캐릭터의 `Palettes.json`에, 모듈 자체 사본(`Global/Modules/Lib/Characters/Student/<id>/Palettes.json`)의 팔레트 중 캐릭터 파일에 없는 것(`name`으로 대조)을 이어 붙인 것입니다. 팔레트가 없는 캐릭터나 설치되지 않은 캐릭터도 사본의 팔레트는 가집니다. 목록 범위를 벗어난 인덱스는 기본 팔레트인 0으로 읽힙니다.

| 함수 | 설명 |
| --- | --- |
| `Student.current(save)  -> "a" or "b"` | 세이브가 고른 학생. |
| `Student.ensure(save)  -> "a" or "b"` | 같지만, 카운터에 학생이 없으면 A를 기록합니다. |
| `Student.set(save, id)  -> id` | 학생을 고릅니다. |
| `Student.folder(id)  -> string` | 학생의 캐릭터 폴더 이름. |
| `Student.index(save, id)  -> number` | 학생의 팔레트 인덱스로, 항상 유효한 값입니다. |
| `Student.setIndex(save, id, idx)  -> number` | 팔레트 인덱스를 저장합니다. 그 캐릭터가 세이브에서 장착 중인 캐릭터이면 실제 팔레트도 바뀝니다. |
| `Student.palettes(id)  -> table` | 팔레트 목록: `{ name, plays, blend, stops }` 항목이며, 항목 `i`가 인덱스 `i - 1`에 해당합니다. 파일이 번역을 주면 `name`은 게임 언어로 됩니다. |
| `Student.count(id)`, `Student.entry(id, idx)`, `Student.clamp(id, idx)` | 팔레트 개수, 항목 하나, 유효하게 만든 인덱스. |
| `Student.unlocked(save, id, idx)  -> bool` | 팔레트가 열려 있는지 여부: 플레이 조건이 없거나, 그 캐릭터로 충분히 플레이했는지. |
| `Student.gradient(id, idx)  -> gradient` | 팔레트를 `GRADIENT` 맵으로 반환합니다. 색이 없는 팔레트이면 nil. |
| `Student.withPalette(id, idx, fn, ...)` | 팔레트를 적용한 상태로 `fn`(2D 그리기)을 실행합니다. pcall의 결과를 반환합니다. |
| `Student.registerSprite(scene, spriteId, tex, id, idx)  -> bool` | `tex`를 팔레트로 색을 바꾼 3D 장면의 빌보드 스프라이트로 등록합니다. |
| `Student.reload()` | 파싱해 둔 팔레트 파일을 잊습니다. 다음 조회 때 다시 읽습니다. |

`palettes`, `count`, `entry`, `clamp`, `gradient`, `registerSprite`는 선택적인 마지막 인자 `visitor`를 받습니다. true이면 다른 플레이어의 학생(온라인 방문자) 목록이 되며, 플레이어 자신의 것과 따로 캐시됩니다.

```lua
local Student = require("Characters.Student")
local save = GetSaveFile(0)
local id  = Student.current(save)
local idx = Student.index(save, id)
Student.withPalette(id, idx, function() tex:Draw(x, y) end)
```
