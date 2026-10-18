<!-- modules/calendar.md -->

# Calendar

## Calendar.almanac

Date patterns for events that come and go with the calendar: a featured shop item at Christmas, an April Fools scene, a monthly bonus. Days are Japan Standard Time days (UTC+9) whatever the player's clock, so every player sees the same day. A pattern can be written as an opaque `enc:` token, so a surprise does not show in the source.

| Pattern | Matches |
| --- | --- |
| `"2026-06-15"` | That day. |
| `"2026-06-15+30"` | That day and the 29 after it. |
| `"2026-06-15..2026-07-31"` | The range, both ends included (both fully written). |
| `"*-06-15"`, `"*-06-15+7"` | That day, or that week, every year. |
| `"*-*-01"`, `"2026-*-13"` | The 1st of every month; every 13th of 2026. |
| `"*-06-*"`, `"2026-12-*"` | A whole month every year; one month. |
| `{ "...", "..." }` | Any of several patterns. |

An entry is a table `{ code, when, priority, sticky }`: it is active while `when` matches. `sticky = true` keeps it active for a save file once it has matched there, through the trigger `.almanac_<code>` on that save. `code` may itself be an `enc:` token.

| Function | Description |
| --- | --- |
| `Almanac.matches(spec)  -> bool` | Whether today matches a pattern, a token, or a list of them. |
| `Almanac.active(entry, save?)  -> bool` | Whether an entry is active today; with a save file, a sticky entry that matched once stays active, and a match sets its trigger. |
| `Almanac.pick(entries, save?, skip?)  -> entry` | The active entry with the highest `priority` (the first one on a tie), or nil. `skip(entry)` returning true rules an entry out (one the player already owns, for example). |
| `Almanac.encrypt(pattern)  -> string` | The `enc:` token of a pattern or a code. |
| `Almanac.decrypt(token)  -> string` | The text of a token, or nil when it is not a valid token. |
| `Almanac.plain(s)  -> string` | A token decrypted, or `s` as it is. |
| `Almanac.parse(spec)  -> table` | One pattern parsed (nil when malformed), for `matchesPattern`. |
| `Almanac.matchesPattern(p)  -> bool` | Whether today matches a parsed pattern. |

```lua
local Almanac = require("Calendar.almanac")
local EVENTS = {
    { code = "winter", when = "*-12-20+12", priority = 80 },
    { code = "spring", when = "*-03-20+14", priority = 50, sticky = true },
}
local now = Almanac.pick(EVENTS, GetSaveFile(0))
if now ~= nil then banner = now.code end
```
