<!-- modules/pagoda.md -->

# Pagoda

Die Pagode des Unbekannten ist ein Aufstieg über Dan-Etagen: Jede Etage lost aus ihren Pools drei Songs aus (und manchmal einen vierten, violetten), mit drei Prüfungen, die mit steigenden Etagen strenger werden. Die `Pagoda`-Module enthalten die Regeln, die Daten und die Spielstand-Schlüssel; sie zeichnen nichts. Ein Skin zeichnet seine eigenen Pagodenbildschirme darauf, und alle Skins teilen sich einen Fortschritt, weil der Fortschritt im Spielstand liegt. Die Stage `pagoda` von Open-World Memories ist ein solcher Client.

Die Etagen 1 bis 10 heißen 道一 bis 道十, 11 bis 20 扉一 bis 扉十, und ab 21 folgen die Dächer 穹天1, 穹天2 und so weiter. Die Namen sind der Titel des Dan, in jeder Sprache gleich.

## Die Module

| Modul | Inhalt |
| --- | --- |
| `Pagoda.Levels` | Alles, was sich aus einer Etagennummer ableitet: die Song-Pools, der Name, Rahmen und Farbe des Dan-Schilds, die Prüfungsschwellen, die Chance auf einen violetten Song. Reine Nachschlagefunktionen. |
| `Pagoda.Floor` | Die Songs einer Etage (eine Auslosung) und ihr Dan: übergibt eine Auslosung mit Schild und Prüfungen an `DANBUILDER`. |
| `Pagoda.Rewards` | Was ein Clear einbringt: sein Namensschild, Münzen, Möbel für Mein Zimmer, Tresorschlüssel; speichert sie und zeigt die Pop-ups über die ROActivity `modal`. |
| `Pagoda.Progress` | Die dauerhaften Spielstanddaten (unten). |
| `Pagoda.Songs` | Die Songliste, aus der die Pagode schöpft, und die Prüfung, ob der Soundtrack jeden Pool-Eintrag abdeckt. |
| `Pagoda.Run` | Optional: der Ablauf eines Herausforderungslaufs und des Trainings, ohne Bildschirm. |
| `Pagoda.Texts` | Die eigenen Strings der Module (`lang/<code>/pagoda.json`). |

## Pagoda.Levels

| Funktion | Beschreibung |
| --- | --- |
| `Levels.get_pool(level)  -> pool` | Der Pool der Etage, Listen `{ blue, green, red, purple }` aus `{ id, diff }` (die eindeutige ID eines Songs und ein Schwierigkeitsgrad, 0 bis 4). Eine Etage ohne eigenen Eintrag verwendet den nächsten darunter. |
| `Levels.load_pools()`, `Levels.clear_pools()` | Liest `pools.json` (einmal); vergisst es wieder, nach einer erneuten Song-Aufzählung. |
| `Levels.level_name(level)  -> string` | Der Name der Etage. |
| `Levels.level_tick(level)`, `Levels.level_tick_color(level)` | Der Rahmen (0 bis 4) und die Farbe `{ r, g, b }` des Dan-Schilds. |
| `Levels.exam1_gauge(level)  -> red, gold` | Die Gauge-Prüfung. |
| `Levels.exam2(level)  -> type, red, gold, lessThan` | Die Genauigkeitsprüfung (Etagen 1 bis 20) oder die Ok-Anzahl (ab 21). |
| `Levels.exam3_base(level)  -> red, gold` | Die Bad-Anzahl-Prüfung vor der Nachsicht durch den violetten Song. |
| `Levels.purple_prob(level)  -> number` | Die Chance auf einen violetten vierten Song (Etage 20 hat immer einen). |
| `Levels.pick_entry(entries, used)`, `Levels.shuffle(t)` | Die Hilfsfunktionen, die die Auslosungen verwenden. |

`pools.json` neben den Modulen ordnet jeder Etagennummer ihren Pool zu:

```json
{
  "1": {
    "comment": "一題",
    "blue":   [ { "id": "<song unique id>", "diff": 0 } ],
    "green":  [ { "id": "<song unique id>", "diff": 0 } ],
    "red":    [ { "id": "<song unique id>", "diff": 0 } ],
    "purple": [ { "id": "<song unique id>", "diff": 0 } ]
  }
}
```

Die Pools nennen Songs des OpenTaiko-Soundtracks. `Songs.count_missing_songs()` protokolliert und zählt die Einträge, die den installierten Songs fehlen (ein Song oder sein Schwierigkeitsgrad), damit ein Bildschirm den Spieler warnen kann.

## Pagoda.Floor

| Funktion | Beschreibung |
| --- | --- |
| `Floor.rollFloor(level, avoid?, songList?)  -> roll` | Ein blauer, ein grüner und ein roter Song in gemischter Reihenfolge, dann ein violetter Song mit der Chance der Etage, nie derselbe Song zweimal. `avoid` ist eine Menge von Song-IDs, die nicht gewählt werden, solange die Pools andere haben. Eine Auslosung ist `{ songs = { { color, node, entry }, ... }, purple = { node, entry } or nil, speed }`. |
| `Floor.rerollFloor(level, roll, keepMystery, songList?)  -> roll` | Lost erneut aus, mit anderen Songs, wo die Pools es erlauben; `keepMystery` behält den violetten Song. |
| `Floor.exams(level, hadPurple)  -> table` | Die drei Prüfungen als `{ type, red, gold, lessThan }`. |
| `Floor.buildDan(level, roll?, opts?)  -> bool` | Richtet `DANBUILDER` für die Etage ein: Titel, Schild, Genre (`opts.genre`, `opts.genreColor`), die Songs und die Prüfungen. False, wenn kein Song hinzugefügt werden konnte; andernfalls mounten Sie es mit `DANBUILDER:Mount()` und spielen. |
| `Floor.speed(level)  -> number` | Die Songgeschwindigkeit der Etage (ein Wert von `CONFIG.SONGSPEED`): x1 unterhalb von 穹天18, danach steigend. |

## Pagoda.Run

Ein Herausforderungslauf beginnt auf einer der offenen Startetagen, steigt pro Clear eine Etage auf und endet mit der ersten Niederlage. Das Training spielt jede Etage bis zur höchsten, die ein Lauf erreicht hat (mindestens 11), und speichert nichts.

| Funktion | Beschreibung |
| --- | --- |
| `Run.startOptions()  -> table` | Die auf diesem Spielstand offenen Startetagen: 1, 6 und 11 immer, 16 nach dem Clear von 15, 21 nach dem Clear von 20. |
| `Run.new(start, songList?)  -> run` | Ein Lauf ab dieser Etage. Seine Felder: `level` (die aktuelle Etage), `tower` (die höchste als geschafft angezeigte Etage), `roll`, `over`. |
| `run:rollFloor(avoid?)`, `run:reroll(keepMystery)  -> roll` | Die Songs der aktuellen Etage, die vor dem Spiel gezeigt werden. |
| `run:enter()` | Der Lauf hat seine Etage erreicht: Das Training bietet sie ab jetzt an. |
| `run:buildDan(opts?)  -> bool` | `DANBUILDER`, eingerichtet für die aktuelle Etage; mounten Sie es und spielen Sie. |
| `run:clear()  -> result` | Nach einem Clear: vergibt und speichert die Belohnungen, hebt die höchste Etage an, steigt auf. Gibt `{ level, rewards, shrouded }` zurück: `rewards` ist die Liste, die `Rewards.grant` zurückgibt, `shrouded`, ob die Etage über dem Turm lag. |
| `run:fail()` | Nach einer Niederlage: merkt sich die Etage, zählt das Spiel, beendet den Lauf. |
| `Run.practiceLevels()  -> table`, `Run.practiceDan(level, opts?)  -> bool` | Die Etagen, die das Training anbietet; `DANBUILDER`, eingerichtet für ein Trainingsspiel mit frischer Auslosung. |

## Pagoda.Rewards

| Funktion | Beschreibung |
| --- | --- |
| `Rewards.grant(level)  -> table` | Vergibt und speichert die Belohnungen eines Clears auf dem Spielstand von Spieler 1 und gibt die anzuzeigenden Pop-ups in Reihenfolge zurück: `{ kind = "nameplate", id }`, `{ kind = "furniture", id }`, `{ kind = "key", key, count }`, `{ kind = "coins", amount }`. |
| `Rewards.setup()` | Teilt die Schlüsselbilder einmalig (`pagoda_reward_key_<id>`); rufen Sie es auf, bevor Sie ein Schlüssel-Pop-up zeigen. |
| `Rewards.show(r)  -> bool` | Startet das Pop-up einer Belohnung über die ROActivity `modal`. |
| `Rewards.update()  -> bool`, `Rewards.draw()`, `Rewards.stop()` | Führt das Pop-up aus (true, solange es läuft; es liest seine eigene Eingabe), zeichnet es, verwirft es. |
| `Rewards.poolOf(level)`, `Rewards.nameplateOf(level)` | Der Belohnungspool einer Etage und das Namensschild, das ihr erster Clear vergibt. |

Der erste Clear einer Etage vergibt ihr Namensschild (IDs 381 bis 397, auf den in `Rewards.NAMEPLATES` aufgeführten Etagen). Danach werden die Münzen (ein Bereich, mit 50 % Chance) und jeder Eintrag des Etagenpools jeweils mit ihrer eigenen Chance ausgelost (`Rewards.POOLS`, `Rewards.ROOF_POOLS`), sodass ein Clear mehrere Dinge einbringen kann. Tresorschlüssel werden zu Münzen, solange der Geheimtresor verschlossen ist, und alle Münzen eines Clears ergeben ein einziges Pop-up. Ein Möbel-Pop-up zeigt die gemeinsame Textur `myroom_thumb_<id>`, die [Mein Zimmer](my-room.md) bereitstellt.

## Spielstand-Schlüssel

Alles wird im Spielstand von Spieler 1 gespeichert (`GetSaveFile(0)`):

| Schlüssel | Art | Inhalt |
| --- | --- | --- |
| `pagoda_highest_level` | Zähler | Die höchste in einem Herausforderungslauf geschaffte Etage. |
| `pagoda_highest_reached` | Zähler | Die höchste Etage, die ein Lauf erreicht hat; bis zu ihr bietet das Training Etagen an. |
| `pagoda_plays` | Zähler | Gespielte Herausforderungsläufe (bei der Niederlage gezählt). |
| `pagoda_lost_<level>` | Trigger | Ein Lauf ging auf dieser Etage verloren. |
| `pagoda_story_first_seen` | Trigger | Die Eröffnungsgeschichte wurde abgespielt (ein älterer Spielstand kann stattdessen den Zähler `pagoda_story_seen` enthalten). |
| `pagoda_held_mods`, `pagoda_held_mods_lo`, `pagoda_held_mods_hi` | Trigger, Zähler | Die Mod-Flags von Spieler 1, die während eines Pagodenspiels festgehalten werden, in zwei 32-Bit-Hälften (`Progress.hold_mods(flags)`, `held_mods()`, `drop_held_mods()`). |
| `pagoda_structure_version` | Zähler | Die einmalige Neunummerierung alter Spielstände (`Progress.migrate_save()`) ist gelaufen. |
| `.myroom_<id>`, `.myroom_held_<id>` | Zähler | Gewonnene Möbel: an Mein Zimmer übergeben, und im Besitz. |
| `.vault_key_count_<key>` | Zähler | Gewonnene Tresorschlüssel, über [Vault.Keys](vault.md). |

Namensschilder und Münzen laufen über die eigenen Methoden des Spielstands (`UnlockNameplate`, `EarnCoins`).

```lua
local Run     = require("Pagoda.Run")
local Rewards = require("Pagoda.Rewards")
local run = Run.new(Run.startOptions()[1])

run:rollFloor()                  -- show run.roll's songs
run:enter()
if run:buildDan() and DANBUILDER:Mount() then return Exit("play") end

-- back from the play
local r = run:clear()            -- or run:fail()
Rewards.setup()
for _, reward in ipairs(r.rewards) do queue[#queue + 1] = reward end   -- then Rewards.show / update / draw
```
