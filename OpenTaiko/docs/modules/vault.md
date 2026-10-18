<!-- modules/vault.md -->

# Vault

The secret vault holds three chests, opened with keys the player wins elsewhere (pagoda clears, the coin shop). The `Vault` modules hold the keys, the chests' reward pools and the save keys; they draw nothing. A skin draws its own vault screen on top of them, and every skin shares one vault, because its state lives in the save file. Open-World Memories' `secret_vault_rw` stage is one such client.

## The modules

| Module | What it holds |
| --- | --- |
| `Vault.Keys` | The three keys: their counts and names, and whether the vault is open. |
| `Vault.Chests` | The three chests, their weighted reward pools and the reward pop-ups. |
| `Vault.Texts` | The modules' own strings (`lang/<code>/vault.json`). |

## Vault.Keys

A key is named by its number or its id: 1 `"simple"`, 2 `"gold"`, 3 `"optk"`. Each opens the chest of the same number.

| Function | Description |
| --- | --- |
| `Keys.count(key)  -> number` | How many of that key player 1 holds. |
| `Keys.set(key, n)`, `Keys.add(key, n?)` | Sets the count; adds `n` (1 by default). |
| `Keys.name(key)  -> string` | The key's name in the game's language. |
| `Keys.vaultOpen()  -> bool` | Whether the vault is open: the trigger `.vault_opened`, which the skin's own way into the vault sets. While it is shut, other modules give coins instead of keys. |
| `Keys.index(key)  -> number` | The key's number. |

## Vault.Chests

The chests are 1 wooden, 2 golden and 3 platinum. Opening one spends one of its keys and rolls one reward from its weighted pool: nothing (the key snaps), coins, the next chest's key, a nameplate the player lacks (ids 361 to 368, 369 to 375, 376 to 380 by chest), a piece of My Room furniture (a piece already owned keeps a fifth of its chance), or a song of the soundtrack's `Secret Vault` folder not yet won, sorted into the chests by its highest chart level.

| Function | Description |
| --- | --- |
| `Chests.enter()  -> bool` | Once per visit, before anything else: gives the first visit's free simple key (returns true then) and builds the song pools. |
| `Chests.roll(i)  -> reward` | Opens chest `i` with one of its keys (check `keyCount` first), saves the reward at once and returns it. |
| `Chests.showReward(reward)  -> bool` | Starts the reward's pop-up through the `modal` ROActivity; false for a snap or a key, which have none. |
| `Chests.keyCount(i)  -> number` | The keys held for chest `i`. |
| `Chests.name(i)  -> string` | The chest's name in the game's language. |
| `Chests.stock(i)  -> left, total` | What is left to win in the chest (songs, nameplates, furniture) and its total. |
| `Chests.chestPool(i)  -> table` | The chest's weighted pool as it stands on this save: `{ w, type, ... }` entries. |
| `Chests.songPool(i)`, `Chests.maxChartLevel(node)` | The chest's songs; a song's highest chart level. |

A reward is `{ type, openedChest, ... }`, `type` being `"snap"`, `"coins"` (`amount`), `"key"` (`target`, the chest number), `"nameplate"` (`id`, `info`), `"furniture"` (`id`, `rarity`) or `"song"` (`node`). The pop-up runs as the `modal` ROActivity does: call its `Update` and `Draw` while `IsActive`, as `Pagoda.Rewards.update` and `draw` do. A furniture pop-up shows the shared texture `myroom_thumb_<id>` that [My Room](my-room.md) provides.

## Save keys

Everything is kept in player 1's save (`GetSaveFile(0)`):

| Key | Kind | Holds |
| --- | --- | --- |
| `.vault_key_count_simple`, `.vault_key_count_gold`, `.vault_key_count_optk` | counters | The keys held. |
| `.vault_opened` | trigger | The vault is open (set by the skin). |
| `.vault_first_key_given` | trigger | The first visit's key was given. |
| `.vault_song_unlocked_<unique id>` | trigger | That `Secret Vault` song was won. |
| `.myroom_<id>`, `.myroom_held_<id>` | counters | Furniture won: handed to My Room, and owned. |

Nameplates and coins go through the save's own methods (`UnlockNameplate`, `EarnCoins`).

```lua
local Chests = require("Vault.Chests")

function activate()
    if Chests.enter() then showFreeKeyNotice = true end
end

local function open(i)
    if Chests.keyCount(i) < 1 then return end
    local reward = Chests.roll(i)
    if not Chests.showReward(reward) then playSnap(reward) end
end
```
