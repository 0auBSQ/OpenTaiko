<!-- guides/unlockables.md -->

# Adding Unlock Conditions to Custom Charts

You can lock a custom song behind a condition (a coin price, clearing other songs, a total play count, a story flag, and so on) by placing an `Unlock.json` file in the song's folder next to its `.tja` and `uniqueID.json`. When the game builds the song list it reads that file and keeps the song locked until the player meets the condition. Conditions attach to individual songs: `box.def` has no unlock key, so you cannot lock a genre folder this way.

## Before you start

- A custom chart that already appears in the song list: a folder under `Songs/` with a `.tja` and, once the game has scanned it, a `uniqueID.json`.
- A text editor that saves UTF-8.
- For conditions that reference other songs: the `id` value from each referenced song's `uniqueID.json`.
- The game stores unlock progress per save file, so you must meet the condition in game to see the song unlock. The `Ignore Song Unlockables` option in the Game settings treats every song as unlocked while you test.

## Step 1: Create the Unlock.json file

Create `Unlock.json` in the song folder. Every field is optional and has a default:

- `hidden_index` (int, default 0): how the song list presents the locked song (see the table below). The game clamps values to 0-3.
- `rarity` (string, default `Common`): the rarity name (see the table below). It sets the colour of the rarity display and the tier of the unlock notification. An empty value becomes `Common`.
- `condition` (string, default `ch`): the condition id (Step 2).
- `values` (int array, default `[100]`): the numeric parameters of the condition.
- `type` (string, default `me`): the comparison that value-based conditions use: `l` less than, `le` less or equal, `e` equal, `me` more or equal, `m` more than, `d` different. Coin conditions always use `me`.
- `references` (string array, default `[""]`): song ids, genre names, charter names or flag names, depending on the condition.
- `custom_unlock_text` (object, optional): replaces the generated hint text. A localized object `{ "strings": { "default": "...", "ja": "..." } }`; the game uses the key for the active language, then `default`, and shows the generated text if neither exists.

The keys are lower-case with underscores, exactly as listed.

Hidden index values:

| Value | In the song list |
| --- | --- |
| 0 | Shown with a lock icon; the audio preview plays |
| 1 | Greyed out; no preview |
| 2 | Greyed out, with the title and preview image obscured |
| 3 | Hidden until unlocked |

Rarity values:

| Rarity | Notification tier |
| --- | --- |
| <span class="rarity rarity-poor">Poor</span> | 0 |
| <span class="rarity rarity-common">Common</span> | 0 |
| <span class="rarity rarity-uncommon">Uncommon</span> | 1 |
| <span class="rarity rarity-rare">Rare</span> | 2 |
| <span class="rarity rarity-epic">Epic</span> | 3 |
| <span class="rarity rarity-legendary">Legendary</span> | 4 |
| <span class="rarity rarity-mythical">Mythical</span> | 4 |

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "cm",
  "values": [500],
  "type": "me",
  "references": [""],
  "custom_unlock_text": {
    "strings": {
      "default": "Buy this song for 500 coins.",
      "ja": "500コインで解放できます。"
    }
  }
}
```

## Step 2: Pick a condition

| Id | Meaning | `values` | `references` |
|----|---------|----------|--------------|
| `ch`, `cs`, `cm` | Coin purchase. The game evaluates the three ids identically (price in coins, `type` forced to `me`, never granted automatically). `cm` is the id intended for songs; skin scripts can read the id and use it to decide where they offer the purchase. | `[price]` | unused |
| `ce` | Coins earned in total since the save file's creation | `[coins]` | unused |
| `tp` | Total play count | `[plays]` | unused |
| `ap` | AI battle play count | `[plays]` | unused |
| `aw` | AI battle win count | `[wins]` | unused |
| `sd` | Distinct charts that reached a clear status | `[chart count, clear status]` | unused |
| `dp` | Charts of one difficulty that reached a clear status | `[difficulty, clear status, chart count]` | unused |
| `lp` | Charts of one star level that reached a clear status | `[level, clear status, chart count]` | unused |
| `sp` | Specific songs that reached a clear status | `[difficulty, clear status]` per song (`-1` = any difficulty) | one song id per pair |
| `sg` | Songs inside named genres that reached a clear status | `[song count, clear status]` per genre | one genre name per pair |
| `sc` | Charts by named charters that reached a clear status | `[chart count, clear status]` per charter | one charter name per pair |
| `gt` | Global trigger (a named on/off flag in the save file that scripts set) | `[1]` for ON, `[0]` for OFF | `[trigger name]` |
| `gc` | Global counter (a named number in the save file that scripts set) | `[value]`, compared with `type` | `[counter name]` |
| `ig` | Impossible to get: never unlocks | none | none |
| `andcomb` | The player must meet all child conditions (see the combined examples) | `[]` | one child condition per entry, as a JSON string |
| `orcomb` | The player must meet at least one child condition (see the combined examples) | `[]` | one child condition per entry, as a JSON string |

Clear status values (a status requirement means this status or better):

| Value | Clear status |
| --- | --- |
| 0 | played |
| 1 | assisted clear |
| 2 | clear |
| 3 | full combo |
| 4 | all perfect |

Difficulty values:

| Value | Difficulty |
| --- | --- |
| 0 | Easy |
| 1 | Normal |
| 2 | Hard |
| 3 | Extreme |
| 4 | Extra Extreme |

For `dp`, difficulty 3 also counts Extra Extreme charts. `dp` and `lp` only count regular charts and skip Dan and Tower charts. `sp` accepts `-1` to mean any difficulty.

The game checks value counts: `ch`/`cs`/`cm`/`ce`/`tp`/`ap`/`aw`/`gt`/`gc` need exactly 1 value, `sd` exactly 2, `dp`/`lp` exactly 3, and `sp`/`sg`/`sc` need 2 values per reference with as many references as pairs. A wrong count makes the condition fail with an error message in game.

Only an explicit purchase in a screen that offers it unlocks a coin condition (the shipped skin offers locked songs for purchase from song select, whichever coin id you use). The game checks all other conditions on the results screen after each play; when the player meets one, the game adds the song id to the save file and shows a notification.

## Step 3: Test it in game

Start the game and open song select. Depending on `hidden_index` the song shows a lock, is greyed out, is obscured, or is absent. Buy it or meet the condition, then confirm that it stays unlocked after a restart. Turn on `Ignore Song Unlockables` in the Game settings to bypass every lock while you iterate.

## Examples

These are the condition patterns the bundled songs use, written as `Unlock.json` files. Replace the ids, names and numbers with your own.

**Buy the song with coins (`cm`).** The most common pattern. The price is the single value, and the game ignores `type`. `hidden_index` 0 keeps the song visible so the player can find it and buy it. The generated hint already states the price, so you need no `custom_unlock_text`.

```json
{
  "hidden_index": 0,
  "rarity": "Uncommon",
  "condition": "cm",
  "values": [500]
}
```

**Total play count (`tp`).** The bundled chapters open their songs one after another with rising play counts (10, 15, 20 and so on), so the player always has a next song in reach. `ap` (AI battle plays), `aw` (AI battle wins) and `ce` (coins earned in total) use the same single-value layout.

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "tp",
  "values": [50]
}
```

**Clear a specific song (`sp`).** The pattern most bundled songs use for a sequel or a remix. `-1` as the difficulty accepts a clear on any difficulty; the id is the `id` field of the referenced song's `uniqueID.json` (the game generates a 64-character id the first time it scans a song without one, and bundled songs may carry a hand-written id). The example unlocks after the player clears the referenced song.

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "sp",
  "values": [-1, 2],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"]
}
```

**Full combo several songs (`sp`).** Each additional song adds a value pair and a reference, and the player must satisfy every referenced song. The example requires a full combo (3) on two songs.

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sp",
  "values": [-1, 3, -1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU", "4aJ2I19XyEG2cEA9tlmdnZSz2H43OKsVBPLi52UfRhSjDGNgTGGqhmqkbWfPDoyw"]
}
```

**Clear songs of a genre (`sg`).** Each entry is `[song count, clear status]` with the genre name in `references`. The genre is the one the game records when the player plays the song: the `#GENRE` of the enclosing folder's `box.def`, or the chart's own `#GENRE` when the folder has none. The bundled chapters use this for their medleys. The example unlocks after the player clears 10 distinct songs of the genre.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sg",
  "values": [10, 2],
  "references": ["OpenTaiko Chapter II"]
}
```

**Clear charts by a charter (`sc`).** The same layout with `#NOTESDESIGNER` names as the references. The example requires 5 cleared charts by one charter; a second charter adds another value pair and reference.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sc",
  "values": [5, 2],
  "references": ["bol"]
}
```

**Clear a number of distinct charts (`sd`).** Counts every chart the save file has at the given status or better. The example requires 10 full combos.

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sd",
  "values": [10, 3]
}
```

**Clear charts of a difficulty or star level (`dp` / `lp`).** `dp` counts charts of one difficulty, `lp` charts of one star level. The first example requires 20 cleared Extreme charts, the second one cleared 7-star chart.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "dp",
  "values": [3, 2, 20]
}
```

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "lp",
  "values": [7, 2, 1]
}
```

**Either of two conditions (`orcomb`).** `values` is empty and each entry of `references` is a complete child condition (`condition`, `type`, `values`, `references`) written as a JSON string. The bundled songs use this to offer a shortcut: play enough songs, or pay. `orcomb` uses the cheapest satisfied branch when it prices a purchase.

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "orcomb",
  "values": [],
  "references": [
    "{\"condition\": \"tp\", \"type\": \"me\", \"values\": [100]}",
    "{\"condition\": \"cm\", \"type\": \"me\", \"values\": [200]}"
  ]
}
```

**All of several conditions (`andcomb`).** The same layout; the player must meet every child. Children may themselves be combinations, and `andcomb` adds up the coin prices inside a combination. The example requires a clear of one song and a clear of any 7-star chart.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "andcomb",
  "values": [],
  "references": [
    "{\"condition\": \"sp\", \"type\": \"me\", \"values\": [-1, 2], \"references\": [\"4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU\"]}",
    "{\"condition\": \"lp\", \"type\": \"me\", \"values\": [7, 2, 1], \"references\": [\"\"]}"
  ]
}
```

**Story flag with a custom hint (`gt`).** `gt` reads a named on/off flag from the save file. Lua scripts (story scenes, cutscenes) set the flags, so use `gt` only when a script you control sets the flag. `values` is `[1]` for ON or `[0]` for OFF; `references` holds the flag name. A story unlock has no self-explanatory generated hint, so `custom_unlock_text` is useful here. You can supply any language key; `default` is the fallback.

```json
{
  "hidden_index": 3,
  "rarity": "Legendary",
  "condition": "gt",
  "values": [1],
  "references": ["story_done"],
  "custom_unlock_text": {
    "strings": {
      "default": "Finish the story to unlock this song.",
      "ja": "ストーリーをクリアするとこの曲が解放されます。"
    }
  }
}
```

**Secret song with a riddle.** The bundled secret songs combine a high `hidden_index` (the title and preview stay obscured, or the song stays absent until unlocked) with a `custom_unlock_text` that hints at the condition instead of stating it. Any condition works underneath; the example hides a full-combo requirement behind a riddle.

```json
{
  "hidden_index": 2,
  "rarity": "Epic",
  "condition": "sp",
  "values": [-1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"],
  "custom_unlock_text": {
    "strings": {
      "default": "Perfect the storm that came before this one."
    }
  }
}
```

## Troubleshooting and notes

- The song is not locked: the file is not named `Unlock.json`, it is not in the folder that holds the `.tja` and `uniqueID.json`, or `Ignore Song Unlockables` is on.
- The hint says the condition is invalid: the number of values does not match the condition (see Step 2), or `sp`/`sg`/`sc` have a different number of references than value pairs.
- A key has no effect: the keys are `hidden_index`, `rarity`, `condition`, `values`, `type`, `references`, `custom_unlock_text`, all lower-case. The game ignores anything else and applies the default.
- `sp` never unlocks: the reference must be the target song's `uniqueID.json` id (a title or file name never matches), and the player must be able to reach the difficulty and status (status 3 is a full combo, so a chart the player has only cleared does not count).
- `sg` never unlocks: the genre name must match the genre the game records for the played songs (the folder's `box.def` `#GENRE`, or the chart `#GENRE` when there is no folder genre).
- `gt`/`gc` never unlock: nothing sets the named flag. The chart cannot set it itself.
- Nothing unlocks `ig` in play. Use it only for content that some other system grants.
- `custom_unlock_text` must be a `{ "strings": { ... } }` object; a bare string does not work.
- The shipped `Unlock.json` files contain trailing commas. The game's JSON parser accepts them; strict validators reject them.
