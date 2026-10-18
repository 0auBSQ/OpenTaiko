<!-- modules/calendar.md -->

# Calendar

## Calendar.almanac

달력에 따라 나타났다 사라지는 이벤트를 위한 날짜 패턴입니다. 크리스마스의 추천 상품, 만우절 장면, 매달의 보너스 같은 것입니다. 날짜는 플레이어의 시계와 상관없이 일본 표준시(UTC+9)의 날짜이므로 모든 플레이어가 같은 날을 봅니다. 패턴은 불투명한 `enc:` 토큰으로 쓸 수 있어 깜짝 이벤트가 소스에 드러나지 않습니다.

| 패턴 | 일치하는 날 |
| --- | --- |
| `"2026-06-15"` | 그날. |
| `"2026-06-15+30"` | 그날과 그 뒤의 29일. |
| `"2026-06-15..2026-07-31"` | 양 끝을 포함한 기간(둘 다 완전히 적습니다). |
| `"*-06-15"`, `"*-06-15+7"` | 매년의 그날, 또는 그 주. |
| `"*-*-01"`, `"2026-*-13"` | 매달 1일; 2026년의 모든 13일. |
| `"*-06-*"`, `"2026-12-*"` | 매년의 한 달 전체; 특정한 한 달. |
| `{ "...", "..." }` | 여러 패턴 중 하나. |

항목은 테이블 `{ code, when, priority, sticky }`이며, `when`이 일치하는 동안 활성입니다. `sticky = true`이면 어떤 세이브 파일에서 한 번 일치한 뒤에는 그 세이브의 트리거 `.almanac_<code>`를 통해 그 세이브 파일에서 계속 활성으로 남습니다. `code` 자체도 `enc:` 토큰일 수 있습니다.

| 함수 | 설명 |
| --- | --- |
| `Almanac.matches(spec)  -> bool` | 오늘이 패턴, 토큰, 또는 그 목록과 일치하는지 여부. |
| `Almanac.active(entry, save?)  -> bool` | 항목이 오늘 활성인지 여부. 세이브 파일을 주면 한 번 일치한 sticky 항목은 계속 활성이고, 일치하면 그 트리거가 설정됩니다. |
| `Almanac.pick(entries, save?, skip?)  -> entry` | `priority`가 가장 높은 활성 항목(동점이면 첫 번째), 또는 nil. `skip(entry)`가 true를 반환하면 그 항목은 제외됩니다(예: 플레이어가 이미 가진 것). |
| `Almanac.encrypt(pattern)  -> string` | 패턴이나 코드의 `enc:` 토큰. |
| `Almanac.decrypt(token)  -> string` | 토큰의 텍스트. 올바른 토큰이 아니면 nil. |
| `Almanac.plain(s)  -> string` | 복호화한 토큰, 또는 `s` 그대로. |
| `Almanac.parse(spec)  -> table` | 파싱한 패턴 하나(형식이 잘못되면 nil). `matchesPattern`에 씁니다. |
| `Almanac.matchesPattern(p)  -> bool` | 오늘이 파싱된 패턴과 일치하는지 여부. |

```lua
local Almanac = require("Calendar.almanac")
local EVENTS = {
    { code = "winter", when = "*-12-20+12", priority = 80 },
    { code = "spring", when = "*-03-20+14", priority = 50, sticky = true },
}
local now = Almanac.pick(EVENTS, GetSaveFile(0))
if now ~= nil then banner = now.code end
```
