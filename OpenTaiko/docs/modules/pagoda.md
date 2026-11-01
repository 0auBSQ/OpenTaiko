<!-- modules/pagoda.md -->

# Pagoda

The Pagoda of the Unknown is a climb of dan floors: each floor rolls three songs (and sometimes a fourth, purple one) from its pools, with three exams that tighten as the floors rise. The `Pagoda` modules hold the rules, the data and the save keys; they draw nothing. A skin draws its own pagoda screens on top of them, and every skin shares one progress, because the progress lives in the save file. Open-World Memories' `pagoda` stage is one such client.

Floors 1 to 10 are named 道一 to 道十, 11 to 20 扉一 to 扉十, and from 21 the roofs 穹天1, 穹天2, and so on. The names are the dan's title, the same in every language.

## The modules

| Module | What it holds |
| --- | --- |
| `Pagoda.Levels` | Everything derived from a floor number: the song pools, the name, the dan plate's frame and colour, the exam thresholds, the purple-song chance. Pure lookups. |
| `Pagoda.Floor` | A floor's songs (a roll) and its dan: hands a roll to `DANBUILDER` with the plate and the exams. |
| `Pagoda.Rewards` | What a clear gives: its nameplate, coins, My Room furniture, vault keys; saves them and shows the pop-ups through the `modal` ROActivity. |
| `Pagoda.Progress` | The persistent save data (below). |
| `Pagoda.Songs` | The song list the pagoda draws from, and the check that the soundtrack covers every pool entry. |
| `Pagoda.Run` | Optional: the flow of a challenge run and of practice, without a screen. |
| `Pagoda.Texts` | The modules' own strings (`lang/<code>/pagoda.json`). |

## Pagoda.Levels

| Function | Description |
| --- | --- |
| `Levels.get_pool(level)  -> pool` | The floor's pool, `{ blue, green, red, purple }` lists of `{ id, diff }` (a song's unique id and a difficulty, 0 to 4). A floor without its own entry uses the nearest one below. |
| `Levels.load_pools()`, `Levels.clear_pools()` | Reads `pools.json` (once); forgets it, after a song re-enumeration. |
| `Levels.level_name(level)  -> string` | The floor's name. |
| `Levels.level_tick(level)`, `Levels.level_tick_color(level)` | The dan plate's frame (0 to 4) and colour `{ r, g, b }`. |
| `Levels.exam1_gauge(level)  -> red, gold` | The gauge exam. |
| `Levels.exam2(level)  -> type, red, gold, lessThan` | The accuracy exam (floors 1 to 20), or the Ok count (from 21). |
| `Levels.exam3_base(level)  -> red, gold` | The bad-count exam before the purple-song leniency. |
| `Levels.purple_prob(level)  -> number` | The chance of a purple fourth song (floor 20 always has one). |
| `Levels.pick_entry(entries, used)`, `Levels.shuffle(t)` | The helpers the rolls use. |

`pools.json`, next to the modules, maps each floor number to its pool:

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

The pools name songs of the OpenTaiko soundtrack. `Songs.count_missing_songs()` logs and counts the entries the installed songs lack (a song, or its difficulty), so a screen can warn the player.

## Pagoda.Floor

| Function | Description |
| --- | --- |
| `Floor.rollFloor(level, avoid?, songList?)  -> roll` | One blue, one green and one red song in a shuffled order, then a purple song at the floor's chance, never the same song twice. `avoid` is a set of song ids not to pick while the pools have others. A roll is `{ songs = { { color, node, entry }, ... }, purple = { node, entry } or nil, speed }`. |
| `Floor.rerollFloor(level, roll, keepMystery, songList?)  -> roll` | Rolls again, with other songs where the pools allow; `keepMystery` keeps the purple song. |
| `Floor.exams(level, hadPurple)  -> table` | The three exams as `{ type, red, gold, lessThan }`. |
| `Floor.buildDan(level, roll?, opts?)  -> bool` | Sets `DANBUILDER` up for the floor: title, plate, genre (`opts.genre`, `opts.genreColor`), the songs and the exams. False when no song could be added; otherwise mount it with `DANBUILDER:Mount()` and play. |
| `Floor.speed(level)  -> number` | The floor's song speed (a `CONFIG.SONGSPEED` value): x1 below 穹天18, rising after it. |

## Pagoda.Run

A challenge run starts on one of the open starting floors, climbs one floor per clear and ends on its first loss. Practice plays any floor up to the highest a run reached (at least 11) and saves nothing.

| Function | Description |
| --- | --- |
| `Run.startOptions()  -> table` | The starting floors open on this save: 1, 6 and 11 always, 16 after clearing 15, 21 after clearing 20. |
| `Run.new(start, songList?)  -> run` | A run from that floor. Its fields: `level` (the current floor), `tower` (the highest floor shown cleared), `roll`, `over`. |
| `run:rollFloor(avoid?)`, `run:reroll(keepMystery)  -> roll` | The current floor's songs, to show before the play. |
| `run:enter()` | The run reached its floor: practice offers it from now on. |
| `run:buildDan(opts?)  -> bool` | `DANBUILDER` set up for the current floor; mount it and play. |
| `run:clear()  -> result` | After a clear: grants and saves the rewards, raises the highest floor, climbs. Returns `{ level, rewards, shrouded }`: `rewards` is the list `Rewards.grant` returns, `shrouded` whether the floor sat above the tower. |
| `run:fail()` | After a loss: remembers the floor, counts the play, ends the run. |
| `Run.practiceLevels()  -> table`, `Run.practiceDan(level, opts?)  -> bool` | The floors practice offers; `DANBUILDER` set up for a practice play with a fresh roll. |

## Pagoda.Rewards

| Function | Description |
| --- | --- |
| `Rewards.grant(level)  -> table` | Grants and saves a clear's rewards on player 1's save, and returns the pop-ups to show in order: `{ kind = "nameplate", id }`, `{ kind = "furniture", id }`, `{ kind = "key", key, count }`, `{ kind = "coins", amount }`. |
| `Rewards.KEY_PICTURE` | The prefix of the shared textures the key pop-ups show (`pagoda_reward_key_<id>`). The modules ship no key pictures: the skin shares one per key before showing a key pop-up. |
| `Rewards.show(r)  -> bool` | Starts the pop-up of one reward through the `modal` ROActivity. |
| `Rewards.update()  -> bool`, `Rewards.draw()`, `Rewards.stop()` | Runs the pop-up (true while it plays; it reads its own input), draws it, drops it. |
| `Rewards.poolOf(level)`, `Rewards.nameplateOf(level)` | A floor's reward pool, and the nameplate its first clear gives. |

A floor's first clear gives its nameplate (ids 381 to 397, on the floors listed in `Rewards.NAMEPLATES`). Then the coins (a range, at a 50% chance) and each entry of the floor's pool roll on their own chance (`Rewards.POOLS`, `Rewards.ROOF_POOLS`), so one clear can give several things. Vault keys become coins while the secret vault is shut, and all of a clear's coins make one pop-up. A furniture pop-up shows the shared texture `myroom_thumb_<id>` that [My Room](my-room.md) provides.

## Save keys

Everything is kept in player 1's save (`GetSaveFile(0)`):

| Key | Kind | Holds |
| --- | --- | --- |
| `pagoda_highest_level` | counter | The highest floor cleared in a challenge run. |
| `pagoda_highest_reached` | counter | The highest floor a run reached, which practice offers up to. |
| `pagoda_plays` | counter | Challenge runs played (counted at the loss). |
| `pagoda_lost_<level>` | trigger | A run was lost on that floor. |
| `pagoda_story_first_seen` | trigger | The opening story has played (an older save may hold the counter `pagoda_story_seen` instead). |
| `pagoda_held_mods`, `pagoda_held_mods_lo`, `pagoda_held_mods_hi` | trigger, counters | Player 1's mod flags held during a pagoda play, in two 32-bit halves (`Progress.hold_mods(flags)`, `held_mods()`, `drop_held_mods()`). |
| `pagoda_structure_version` | counter | The one-time renumbering of old saves (`Progress.migrate_save()`) has run. |
| `.myroom_<id>`, `.myroom_held_<id>` | counters | Furniture won: handed to My Room, and owned. |
| `.vault_key_count_<key>` | counters | Vault keys won, through [Vault.Keys](vault.md). |

Nameplates and coins go through the save's own methods (`UnlockNameplate`, `EarnCoins`).

```lua
local Run     = require("Pagoda.Run")
local Rewards = require("Pagoda.Rewards")
local run = Run.new(Run.startOptions()[1])

run:rollFloor()                  -- show run.roll's songs
run:enter()
if run:buildDan() and DANBUILDER:Mount() then return Exit("play") end

-- back from the play
local r = run:clear()            -- or run:fail()
for id in pairs(Rewards.KEYS) do SHARED:SetSharedTexture(Rewards.KEY_PICTURE .. id, "Textures/key_" .. id .. ".png") end
for _, reward in ipairs(r.rewards) do queue[#queue + 1] = reward end   -- then Rewards.show / update / draw
```
