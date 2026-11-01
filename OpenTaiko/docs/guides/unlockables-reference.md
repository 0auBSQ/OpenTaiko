<!-- guides/unlockables-reference.md -->

# Unlockables Reference

Songs, characters, puchicharas and nameplates use one unlock condition format. Songs, characters and puchicharas keep it in an `Unlock.json` file, nameplates in the `unlock` object of their entry in `Nameplates.json`. The guides explain where each file goes: [Chart unlockables](unlockables.md), [Adding characters](characters.md), [Adding puchicharas](puchicharas.md) and [Adding nameplates](nameplates.md). This page lists the fields, the condition ids and the numbers they use.

## Condition fields

| Field | Type | Meaning |
| --- | --- | --- |
| `condition` | string | The condition id (see Condition ids). |
| `values` | int array | The numeric parameters of the condition. |
| `type` | string | The comparison that value-based conditions use (see Comparison types). |
| `references` | string array | Song ids, genre names, charter names or flag names, depending on the condition. |

A song's `Unlock.json` adds its own fields (`hidden_index`, `rarity`, `custom_unlock_text`), described in the [Chart unlockables](unlockables.md) guide.

## Condition ids

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
| `andcomb` | The player must meet all child conditions (see the combined examples of the [Chart unlockables](unlockables.md) guide) | `[]` | one child condition per entry, as a JSON string |
| `orcomb` | The player must meet at least one child condition (see the combined examples of the [Chart unlockables](unlockables.md) guide) | `[]` | one child condition per entry, as a JSON string |

The game checks value counts: `ch`/`cs`/`cm`/`ce`/`tp`/`ap`/`aw`/`gt`/`gc` need exactly 1 value, `sd` exactly 2, `dp`/`lp` exactly 3, and `sp`/`sg`/`sc` need 2 values per reference with as many references as pairs. A wrong count makes the condition fail with an error message in game.

Only an explicit purchase in a screen that offers it unlocks a coin condition. The game checks all other conditions on the results screen after each play; when the player meets one, the game unlocks the item and shows a notification.

## Comparison types

| `type` | Comparison |
| --- | --- |
| `l` | less than |
| `le` | less or equal |
| `e` | equal |
| `me` | more or equal (the default) |
| `m` | more than |
| `d` | different |

Coin conditions always use `me`, whatever `type` says.

## Clear status values

A status requirement means this status or better.

| Value | Clear status |
| --- | --- |
| 0 | played |
| 1 | assisted clear |
| 2 | clear |
| 3 | full combo |
| 4 | all perfect |

## Difficulty values

| Value | Difficulty |
| --- | --- |
| 0 | Easy |
| 1 | Normal |
| 2 | Hard |
| 3 | Extreme |
| 4 | Extra Extreme |

For `dp`, difficulty 3 also counts Extra Extreme charts. `dp` and `lp` only count regular charts and skip Dan and Tower charts. `sp` accepts `-1` to mean any difficulty.

## The play mods value

The save file keeps a player's best plays per chart, difficulty and play mods. The play mods value packs the mods of a play into 8 bytes, the lowest byte first: a play without mods gives 8925478921.

| Byte | Mod | Values |
| --- | --- | --- |
| 1 (lowest) | Scroll speed | 0 to 99: 9 is 1.0x, each step is 0.1x |
| 2 | Stealth | 0 Off, 1 Doron, 2 Stealth, 3 Hidden, 4 Flashlight |
| 3 | Random | 0 Off, 1 Random, 2 Mirror, 3 SuperRandom, 4 MirrorRandom |
| 4 | Song speed | 2 to 200: 20 is 1.0x, each step is 0.05x |
| 5 | Judgement window | 0 Loose, 1 Lenient, 2 Normal, 3 Strict, 4 Rigorous |
| 6 | Judgement mod | 0 off, 1 Just, 2 Safe |
| 7 | Unused | 0 |
| 8 (highest) | Fun mod | 0 None, 1 Avalanche, 2 Minesweeper, 3 Dynamic Beat |

Special mods (Flawless, Timed) are not part of the value.
