<!-- modules/vault.md -->

# Vault

秘密金库里有三个宝箱，用玩家在其他地方（未知之塔过关、金币商店）获得的钥匙打开。`Vault` 模块包含钥匙、宝箱的奖励池和存档键；它们不绘制任何内容。皮肤在其之上绘制自己的金库界面，而且由于状态保存在存档中，所有皮肤共享同一个金库。Open-World Memories 的 `secret_vault_rw` 舞台就是这样的一个客户端。

## 模块一览

| 模块 | 内容 |
| --- | --- |
| `Vault.Keys` | 三种钥匙：它们的数量和名称，以及金库是否已开启。 |
| `Vault.Chests` | 三个宝箱、它们的加权奖励池和奖励弹窗。 |
| `Vault.Texts` | 模块自己的字符串（`lang/<code>/vault.json`）。 |

## Vault.Keys

钥匙用编号或 id 指定：1 `"simple"`、2 `"gold"`、3 `"optk"`。每把钥匙打开编号相同的宝箱。

| 函数 | 说明 |
| --- | --- |
| `Keys.count(key)  -> number` | 玩家 1 持有的该钥匙数量。 |
| `Keys.set(key, n)`, `Keys.add(key, n?)` | 设置数量；增加 `n`（默认 1）。 |
| `Keys.name(key)  -> string` | 钥匙在游戏语言中的名称。 |
| `Keys.vaultOpen()  -> bool` | 金库是否已开启：即由皮肤自己的金库入口设置的触发器 `.vault_opened`。金库关闭期间，其他模块会给金币而不是钥匙。 |
| `Keys.index(key)  -> number` | 钥匙的编号。 |

## Vault.Chests

宝箱 1 为木制、2 为金制、3 为白金。打开一个宝箱会消耗一把对应的钥匙，并从其加权池中抽出一个奖励：什么都没有（钥匙折断）、金币、下一个宝箱的钥匙、玩家没有的名牌（按宝箱分别为 id 361 到 368、369 到 375、376 到 380）、一件我的房间的家具（已拥有的家具保留五分之一的几率），或原声音乐 `Secret Vault` 文件夹中尚未获得的一首歌，歌曲按其最高谱面等级分配到各个宝箱。

| 函数 | 说明 |
| --- | --- |
| `Chests.enter()  -> bool` | 每次进入时调用一次，先于其他一切：发放首次进入的免费 simple 钥匙（此时返回 true），并构建歌曲池。 |
| `Chests.roll(i)  -> reward` | 用一把钥匙打开宝箱 `i`（请先检查 `keyCount`），立即保存奖励并返回它。 |
| `Chests.showReward(reward)  -> bool` | 通过 `modal` 只读活动开始播放奖励的弹窗；钥匙折断或奖励为钥匙时没有弹窗，返回 false。 |
| `Chests.keyCount(i)  -> number` | 为宝箱 `i` 持有的钥匙数。 |
| `Chests.name(i)  -> string` | 宝箱在游戏语言中的名称。 |
| `Chests.stock(i)  -> left, total` | 宝箱中还能获得的东西（歌曲、名牌、家具）的剩余数和总数。 |
| `Chests.chestPool(i)  -> table` | 该宝箱在此存档上当前的加权池：`{ w, type, ... }` 条目。 |
| `Chests.songPool(i)`, `Chests.maxChartLevel(node)` | 宝箱的歌曲；某首歌的最高谱面等级。 |

奖励是 `{ type, openedChest, ... }`，其中 `type` 为 `"snap"`、`"coins"`（`amount`）、`"key"`（`target`，宝箱编号）、`"nameplate"`（`id`、`info`）、`"furniture"`（`id`、`rarity`）或 `"song"`（`node`）。弹窗的运行方式与 `modal` 只读活动相同：在 `IsActive` 期间调用它的 `Update` 和 `Draw`，就像 `Pagoda.Rewards.update` 和 `draw` 所做的那样。家具弹窗显示由[我的房间](my-room.md)提供的共享纹理 `myroom_thumb_<id>`。

## 存档键

所有内容都保存在玩家 1 的存档中（`GetSaveFile(0)`）：

| 键 | 类型 | 内容 |
| --- | --- | --- |
| `.vault_key_count_simple`, `.vault_key_count_gold`, `.vault_key_count_optk` | 计数器 | 持有的钥匙。 |
| `.vault_opened` | 触发器 | 金库已开启（由皮肤设置）。 |
| `.vault_first_key_given` | 触发器 | 首次进入的钥匙已发放。 |
| `.vault_song_unlocked_<unique id>` | 触发器 | 已获得这首 `Secret Vault` 歌曲。 |
| `.myroom_<id>`, `.myroom_held_<id>` | 计数器 | 获得的家具：交给我的房间的，以及已拥有的。 |

名牌和金币通过存档自身的方法（`UnlockNameplate`、`EarnCoins`）处理。

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
