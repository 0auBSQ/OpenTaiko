<!-- modules/calendar.md -->

# Calendar

## Calendar.almanac

Des motifs de dates pour les événements qui vont et viennent avec le calendrier : un article mis en avant dans la boutique à Noël, une scène de poisson d'avril, un bonus mensuel. Les jours sont ceux de l'heure normale du Japon (UTC+9), quelle que soit l'horloge du joueur, si bien que tous les joueurs voient le même jour. Un motif peut s'écrire sous la forme d'un jeton opaque `enc:`, pour qu'une surprise n'apparaisse pas dans le code source.

| Motif | Correspond à |
| --- | --- |
| `"2026-06-15"` | Ce jour-là. |
| `"2026-06-15+30"` | Ce jour-là et les 29 suivants. |
| `"2026-06-15..2026-07-31"` | La période, bornes comprises (toutes deux écrites en entier). |
| `"*-06-15"`, `"*-06-15+7"` | Ce jour-là, ou cette semaine-là, chaque année. |
| `"*-*-01"`, `"2026-*-13"` | Le 1er de chaque mois ; chaque 13 du mois en 2026. |
| `"*-06-*"`, `"2026-12-*"` | Un mois entier chaque année ; un seul mois. |
| `{ "...", "..." }` | L'un de plusieurs motifs. |

Une entrée est une table `{ code, when, priority, sticky }` : elle est active tant que `when` correspond. `sticky = true` la garde active pour un fichier de sauvegarde une fois qu'elle y a correspondu, grâce au déclencheur `.almanac_<code>` de cette sauvegarde. `code` peut lui-même être un jeton `enc:`.

| Fonction | Description |
| --- | --- |
| `Almanac.matches(spec)  -> bool` | Si aujourd'hui correspond à un motif, un jeton, ou une liste de ceux-ci. |
| `Almanac.active(entry, save?)  -> bool` | Si une entrée est active aujourd'hui ; avec un fichier de sauvegarde, une entrée sticky qui a correspondu une fois reste active, et une correspondance définit son déclencheur. |
| `Almanac.pick(entries, save?, skip?)  -> entry` | L'entrée active de plus haute `priority` (la première en cas d'égalité), ou nil. Si `skip(entry)` renvoie true, l'entrée est écartée (une que le joueur possède déjà, par exemple). |
| `Almanac.encrypt(pattern)  -> string` | Le jeton `enc:` d'un motif ou d'un code. |
| `Almanac.decrypt(token)  -> string` | Le texte d'un jeton, ou nil s'il ne s'agit pas d'un jeton valide. |
| `Almanac.plain(s)  -> string` | Un jeton déchiffré, ou `s` tel quel. |
| `Almanac.parse(spec)  -> table` | Un motif analysé (nil s'il est mal formé), pour `matchesPattern`. |
| `Almanac.matchesPattern(p)  -> bool` | Si aujourd'hui correspond à un motif analysé. |

```lua
local Almanac = require("Calendar.almanac")
local EVENTS = {
    { code = "winter", when = "*-12-20+12", priority = 80 },
    { code = "spring", when = "*-03-20+14", priority = 50, sticky = true },
}
local now = Almanac.pick(EVENTS, GetSaveFile(0))
if now ~= nil then banner = now.code end
```
