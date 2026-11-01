<!-- modules/songs.md -->

# Songs

## Songs.PlayStrings

스테이지가 시작하는 한 번의 플레이를 위해 설정하는 공유 문자열입니다. 보여 줄 로딩 배경, 단위 플레이트의 스타일 등 그 플레이의 배경이나 ROActivity가 읽는 것이면 무엇이든 됩니다. 문자열은 찍어 둔 채보에 대해서만 값을 주므로, 스테이지가 돌아오지 않은 플레이가 남긴 값이 다른 플레이에 닿지 않습니다.

| 함수 | 설명 |
| --- | --- |
| `PlayStrings.stamp()` | 지금 선택된 채보(`SONGMOUNT`)를 문자열이 속한 채보로 찍어 둡니다. 채보를 마운트한 직후, `Exit("play")` 전에 호출하십시오. |
| `PlayStrings.clear()` | 찍어 둔 표시를 끝냅니다. 플레이에서 돌아왔을 때 호출하십시오. |
| `PlayStrings.get(key)  -> string` | 선택된 채보가 찍어 둔 채보인 동안에는 공유 문자열 `key`, 아니면 `""`. |
| `PlayStrings.forTitle(key, title)  -> string` | 같은 확인을 채보 제목으로 합니다. 제목만 아는 쪽(단위 플레이트)에 씁니다. |

값 자체는 `SHARED:SetSharedString(key, value)`로 설정하는 일반 공유 문자열입니다. 찍어 둔 표시는 공유 문자열 `play_strings_chart`에 보관됩니다.

```lua
local PlayStrings = require("Songs.PlayStrings")

-- the stage, after mounting the chart
SHARED:SetSharedString("my_loading_art", "night")
PlayStrings.stamp()

-- a background script of that play
local art = PlayStrings.get("my_loading_art")   -- "night"; "" for any other chart
```
