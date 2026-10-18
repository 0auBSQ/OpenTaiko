<!-- modules/calendar.md -->

# Calendar

## Calendar.almanac

Datumsmuster für Ereignisse, die mit dem Kalender kommen und gehen: ein hervorgehobener Ladenartikel zu Weihnachten, eine Aprilscherz-Szene, ein monatlicher Bonus. Tage sind Tage der Japanischen Normalzeit (UTC+9), unabhängig von der Uhr des Spielers, sodass jeder Spieler denselben Tag sieht. Ein Muster kann als undurchsichtiges `enc:`-Token geschrieben werden, damit eine Überraschung nicht im Quelltext zu sehen ist.

| Muster | Trifft zu auf |
| --- | --- |
| `"2026-06-15"` | Diesen Tag. |
| `"2026-06-15+30"` | Diesen Tag und die 29 Tage danach. |
| `"2026-06-15..2026-07-31"` | Den Zeitraum, beide Enden eingeschlossen (beide vollständig ausgeschrieben). |
| `"*-06-15"`, `"*-06-15+7"` | Diesen Tag oder diese Woche, jedes Jahr. |
| `"*-*-01"`, `"2026-*-13"` | Den 1. jedes Monats; jeden 13. im Jahr 2026. |
| `"*-06-*"`, `"2026-12-*"` | Einen ganzen Monat jedes Jahr; einen einzelnen Monat. |
| `{ "...", "..." }` | Eines von mehreren Mustern. |

Ein Eintrag ist eine Tabelle `{ code, when, priority, sticky }`: Er ist aktiv, solange `when` zutrifft. `sticky = true` hält ihn für einen Spielstand aktiv, sobald er dort einmal zugetroffen hat, über den Trigger `.almanac_<code>` dieses Spielstands. `code` kann selbst ein `enc:`-Token sein.

| Funktion | Beschreibung |
| --- | --- |
| `Almanac.matches(spec)  -> bool` | Ob der heutige Tag auf ein Muster, ein Token oder eine Liste davon zutrifft. |
| `Almanac.active(entry, save?)  -> bool` | Ob ein Eintrag heute aktiv ist; mit einem Spielstand bleibt ein sticky-Eintrag, der einmal zugetroffen hat, aktiv, und ein Treffer setzt seinen Trigger. |
| `Almanac.pick(entries, save?, skip?)  -> entry` | Der aktive Eintrag mit der höchsten `priority` (bei Gleichstand der erste), oder nil. Gibt `skip(entry)` true zurück, wird der Eintrag ausgeschlossen (zum Beispiel einer, den der Spieler schon besitzt). |
| `Almanac.encrypt(pattern)  -> string` | Das `enc:`-Token eines Musters oder eines Codes. |
| `Almanac.decrypt(token)  -> string` | Der Text eines Tokens, oder nil, wenn es kein gültiges Token ist. |
| `Almanac.plain(s)  -> string` | Ein entschlüsseltes Token, oder `s` unverändert. |
| `Almanac.parse(spec)  -> table` | Ein einzelnes geparstes Muster (nil, wenn es fehlerhaft ist), für `matchesPattern`. |
| `Almanac.matchesPattern(p)  -> bool` | Ob der heutige Tag auf ein geparstes Muster zutrifft. |

```lua
local Almanac = require("Calendar.almanac")
local EVENTS = {
    { code = "winter", when = "*-12-20+12", priority = 80 },
    { code = "spring", when = "*-03-20+14", priority = 50, sticky = true },
}
local now = Almanac.pick(EVENTS, GetSaveFile(0))
if now ~= nil then banner = now.code end
```
