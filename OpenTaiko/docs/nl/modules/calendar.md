<!-- modules/calendar.md -->

# Calendar

## Calendar.almanac

Datumpatronen voor gebeurtenissen die met de kalender komen en gaan: een uitgelicht winkelitem met Kerstmis, een aprilgrapscène, een maandelijkse bonus. Dagen zijn dagen in Japanse standaardtijd (UTC+9), ongeacht de klok van de speler, zodat elke speler dezelfde dag ziet. Een patroon kan als ondoorzichtig `enc:`-token worden geschreven, zodat een verrassing niet in de broncode te zien is.

| Patroon | Komt overeen met |
| --- | --- |
| `"2026-06-15"` | Die dag. |
| `"2026-06-15+30"` | Die dag en de 29 dagen erna. |
| `"2026-06-15..2026-07-31"` | De periode, beide uiteinden inbegrepen (beide volledig uitgeschreven). |
| `"*-06-15"`, `"*-06-15+7"` | Die dag, of die week, elk jaar. |
| `"*-*-01"`, `"2026-*-13"` | De 1e van elke maand; elke 13e in 2026. |
| `"*-06-*"`, `"2026-12-*"` | Een hele maand elk jaar; één maand. |
| `{ "...", "..." }` | Een van meerdere patronen. |

Een item is een tabel `{ code, when, priority, sticky }`: het is actief zolang `when` overeenkomt. `sticky = true` houdt het actief voor een opslagbestand zodra het daar eenmaal heeft overeengekomen, via de trigger `.almanac_<code>` van dat opslagbestand. `code` kan zelf een `enc:`-token zijn.

| Functie | Beschrijving |
| --- | --- |
| `Almanac.matches(spec)  -> bool` | Of vandaag overeenkomt met een patroon, een token of een lijst daarvan. |
| `Almanac.active(entry, save?)  -> bool` | Of een item vandaag actief is; met een opslagbestand blijft een sticky item dat eenmaal heeft overeengekomen actief, en een overeenkomst zet zijn trigger. |
| `Almanac.pick(entries, save?, skip?)  -> entry` | Het actieve item met de hoogste `priority` (bij gelijkspel het eerste), of nil. Geeft `skip(entry)` true terug, dan valt het item af (bijvoorbeeld een dat de speler al bezit). |
| `Almanac.encrypt(pattern)  -> string` | Het `enc:`-token van een patroon of een code. |
| `Almanac.decrypt(token)  -> string` | De tekst van een token, of nil als het geen geldig token is. |
| `Almanac.plain(s)  -> string` | Een ontsleuteld token, of `s` ongewijzigd. |
| `Almanac.parse(spec)  -> table` | Eén geparseerd patroon (nil als het misvormd is), voor `matchesPattern`. |
| `Almanac.matchesPattern(p)  -> bool` | Of vandaag overeenkomt met een geparseerd patroon. |

```lua
local Almanac = require("Calendar.almanac")
local EVENTS = {
    { code = "winter", when = "*-12-20+12", priority = 80 },
    { code = "spring", when = "*-03-20+14", priority = 50, sticky = true },
}
local now = Almanac.pick(EVENTS, GetSaveFile(0))
if now ~= nil then banner = now.code end
```
