<!-- modules/pagoda.md -->

# Pagoda

De Pagode van het Onbekende is een beklimming van dan-verdiepingen: elke verdieping trekt drie nummers (en soms een vierde, paarse) uit haar pools, met drie examens die strenger worden naarmate de verdiepingen stijgen. De `Pagoda`-modules bevatten de regels, de data en de opslagsleutels; ze tekenen niets. Een skin tekent zijn eigen pagodeschermen erbovenop, en alle skins delen één voortgang, omdat de voortgang in het opslagbestand staat. De stage `pagoda` van Open-World Memories is zo'n client.

De verdiepingen 1 tot 10 heten 道一 tot 道十, 11 tot 20 扉一 tot 扉十, en vanaf 21 volgen de daken 穹天1, 穹天2, enzovoort. De namen zijn de titel van de dan, in elke taal hetzelfde.

## De modules

| Module | Wat het bevat |
| --- | --- |
| `Pagoda.Levels` | Alles wat uit een verdiepingsnummer volgt: de nummerpools, de naam, het kader en de kleur van het dan-plaatje, de examendrempels, de kans op een paars nummer. Pure opzoekingen. |
| `Pagoda.Floor` | De nummers van een verdieping (een trekking) en haar dan: geeft een trekking met het plaatje en de examens door aan `DANBUILDER`. |
| `Pagoda.Rewards` | Wat een clear oplevert: zijn naamplaatje, munten, meubels voor Mijn kamer, kluissleutels; slaat ze op en toont de pop-ups via de ROActivity `modal`. |
| `Pagoda.Progress` | De blijvende opslagdata (hieronder). |
| `Pagoda.Songs` | De nummerlijst waaruit de pagode put, en de controle of de soundtrack elk poolitem dekt. |
| `Pagoda.Run` | Optioneel: het verloop van een uitdagingsbeklimming en van het oefenen, zonder scherm. |
| `Pagoda.Texts` | De eigen strings van de modules (`lang/<code>/pagoda.json`). |

## Pagoda.Levels

| Functie | Beschrijving |
| --- | --- |
| `Levels.get_pool(level)  -> pool` | De pool van de verdieping, lijsten `{ blue, green, red, purple }` van `{ id, diff }` (het unieke id van een nummer en een moeilijkheid, 0 tot 4). Een verdieping zonder eigen item gebruikt het dichtstbijzijnde eronder. |
| `Levels.load_pools()`, `Levels.clear_pools()` | Leest `pools.json` (één keer); vergeet het weer, na een nieuwe nummerinventarisatie. |
| `Levels.level_name(level)  -> string` | De naam van de verdieping. |
| `Levels.level_tick(level)`, `Levels.level_tick_color(level)` | Het kader (0 tot 4) en de kleur `{ r, g, b }` van het dan-plaatje. |
| `Levels.exam1_gauge(level)  -> red, gold` | Het gauge-examen. |
| `Levels.exam2(level)  -> type, red, gold, lessThan` | Het nauwkeurigheidsexamen (verdiepingen 1 tot 20), of het aantal Ok (vanaf 21). |
| `Levels.exam3_base(level)  -> red, gold` | Het examen op het aantal Bad, vóór de coulance van het paarse nummer. |
| `Levels.purple_prob(level)  -> number` | De kans op een paars vierde nummer (verdieping 20 heeft er altijd een). |
| `Levels.pick_entry(entries, used)`, `Levels.shuffle(t)` | De helpers die de trekkingen gebruiken. |

`pools.json`, naast de modules, koppelt elk verdiepingsnummer aan zijn pool:

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

De pools noemen nummers van de OpenTaiko-soundtrack. `Songs.count_missing_songs()` logt en telt de items die de geïnstalleerde nummers missen (een nummer, of zijn moeilijkheid), zodat een scherm de speler kan waarschuwen.

## Pagoda.Floor

| Functie | Beschrijving |
| --- | --- |
| `Floor.rollFloor(level, avoid?, songList?)  -> roll` | Een blauw, een groen en een rood nummer in willekeurige volgorde, daarna een paars nummer volgens de kans van de verdieping, nooit twee keer hetzelfde nummer. `avoid` is een verzameling nummer-id's die niet gekozen worden zolang de pools andere hebben. Een trekking is `{ songs = { { color, node, entry }, ... }, purple = { node, entry } or nil, speed }`. |
| `Floor.rerollFloor(level, roll, keepMystery, songList?)  -> roll` | Trekt opnieuw, met andere nummers waar de pools dat toelaten; `keepMystery` houdt het paarse nummer. |
| `Floor.exams(level, hadPurple)  -> table` | De drie examens als `{ type, red, gold, lessThan }`. |
| `Floor.buildDan(level, roll?, opts?)  -> bool` | Zet `DANBUILDER` klaar voor de verdieping: titel, plaatje, genre (`opts.genre`, `opts.genreColor`), de nummers en de examens. False als er geen nummer kon worden toegevoegd; anders mount je het met `DANBUILDER:Mount()` en speel je. |
| `Floor.speed(level)  -> number` | De nummersnelheid van de verdieping (een waarde van `CONFIG.SONGSPEED`): x1 onder 穹天18, daarna stijgend. |

## Pagoda.Run

Een uitdagingsbeklimming begint op een van de open startverdiepingen, stijgt één verdieping per clear en eindigt bij de eerste nederlaag. Oefenen speelt elke verdieping tot de hoogste die een beklimming heeft bereikt (minstens 11) en slaat niets op.

| Functie | Beschrijving |
| --- | --- |
| `Run.startOptions()  -> table` | De startverdiepingen die op dit opslagbestand open zijn: 1, 6 en 11 altijd, 16 na een clear van 15, 21 na een clear van 20. |
| `Run.new(start, songList?)  -> run` | Een beklimming vanaf die verdieping. Haar velden: `level` (de huidige verdieping), `tower` (de hoogste verdieping die als gehaald wordt getoond), `roll`, `over`. |
| `run:rollFloor(avoid?)`, `run:reroll(keepMystery)  -> roll` | De nummers van de huidige verdieping, om vóór het spelen te tonen. |
| `run:enter()` | De beklimming heeft haar verdieping bereikt: oefenen biedt die vanaf nu aan. |
| `run:buildDan(opts?)  -> bool` | `DANBUILDER` klaargezet voor de huidige verdieping; mount het en speel. |
| `run:clear()  -> result` | Na een clear: kent de beloningen toe en slaat ze op, verhoogt de hoogste verdieping, klimt. Geeft `{ level, rewards, shrouded }` terug: `rewards` is de lijst die `Rewards.grant` teruggeeft, `shrouded` of de verdieping boven de toren lag. |
| `run:fail()` | Na een nederlaag: onthoudt de verdieping, telt de spelbeurt, beëindigt de beklimming. |
| `Run.practiceLevels()  -> table`, `Run.practiceDan(level, opts?)  -> bool` | De verdiepingen die oefenen aanbiedt; `DANBUILDER` klaargezet voor een oefenbeurt met een nieuwe trekking. |

## Pagoda.Rewards

| Functie | Beschrijving |
| --- | --- |
| `Rewards.grant(level)  -> table` | Kent de beloningen van een clear toe op het opslagbestand van speler 1, slaat ze op en geeft de pop-ups terug die op volgorde getoond moeten worden: `{ kind = "nameplate", id }`, `{ kind = "furniture", id }`, `{ kind = "key", key, count }`, `{ kind = "coins", amount }`. |
| `Rewards.setup()` | Deelt de sleutelafbeeldingen één keer (`pagoda_reward_key_<id>`); roep het aan voordat je een sleutelpop-up toont. |
| `Rewards.show(r)  -> bool` | Start de pop-up van één beloning via de ROActivity `modal`. |
| `Rewards.update()  -> bool`, `Rewards.draw()`, `Rewards.stop()` | Laat de pop-up lopen (true zolang hij speelt; hij leest zijn eigen invoer), tekent hem, laat hem vallen. |
| `Rewards.poolOf(level)`, `Rewards.nameplateOf(level)` | De beloningspool van een verdieping, en het naamplaatje dat haar eerste clear geeft. |

De eerste clear van een verdieping geeft haar naamplaatje (id's 381 tot 397, op de verdiepingen die in `Rewards.NAMEPLATES` staan). Daarna worden de munten (een bereik, met 50% kans) en elk item van de pool van de verdieping elk met hun eigen kans getrokken (`Rewards.POOLS`, `Rewards.ROOF_POOLS`), zodat één clear meerdere dingen kan opleveren. Kluissleutels worden munten zolang de geheime kluis dicht is, en alle munten van een clear komen in één pop-up. Een meubelpop-up toont de gedeelde textuur `myroom_thumb_<id>` die [Mijn kamer](my-room.md) levert.

## Opslagsleutels

Alles wordt bewaard in het opslagbestand van speler 1 (`GetSaveFile(0)`):

| Sleutel | Soort | Inhoud |
| --- | --- | --- |
| `pagoda_highest_level` | teller | De hoogste verdieping die in een uitdagingsbeklimming is gehaald. |
| `pagoda_highest_reached` | teller | De hoogste verdieping die een beklimming heeft bereikt, tot waar oefenen verdiepingen aanbiedt. |
| `pagoda_plays` | teller | Gespeelde uitdagingsbeklimmingen (geteld bij de nederlaag). |
| `pagoda_lost_<level>` | trigger | Een beklimming is op die verdieping verloren. |
| `pagoda_story_first_seen` | trigger | Het openingsverhaal is afgespeeld (een ouder opslagbestand kan in plaats daarvan de teller `pagoda_story_seen` bevatten). |
| `pagoda_held_mods`, `pagoda_held_mods_lo`, `pagoda_held_mods_hi` | trigger, tellers | De modvlaggen van speler 1 die tijdens een pagodebeurt worden vastgehouden, in twee 32-bitshelften (`Progress.hold_mods(flags)`, `held_mods()`, `drop_held_mods()`). |
| `pagoda_structure_version` | teller | De eenmalige hernummering van oude opslagbestanden (`Progress.migrate_save()`) is uitgevoerd. |
| `.myroom_<id>`, `.myroom_held_<id>` | tellers | Gewonnen meubels: doorgegeven aan Mijn kamer, en in bezit. |
| `.vault_key_count_<key>` | tellers | Gewonnen kluissleutels, via [Vault.Keys](vault.md). |

Naamplaatjes en munten lopen via de eigen methoden van het opslagbestand (`UnlockNameplate`, `EarnCoins`).

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
