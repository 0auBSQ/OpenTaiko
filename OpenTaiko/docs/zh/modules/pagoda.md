<!-- modules/pagoda.md -->

# Pagoda

未知之塔是一层层攀登的段位挑战：每层从自己的池中抽出三首歌（有时还有第四首紫色歌曲），并有三项随楼层升高而愈发严格的考核。`Pagoda` 模块包含规则、数据和存档键；它们不绘制任何内容。皮肤在其之上绘制自己的塔界面，而且由于进度保存在存档中，所有皮肤共享同一份进度。Open-World Memories 的 `pagoda` 舞台就是这样的一个客户端。

第 1 到 10 层名为 道一 到 道十，第 11 到 20 层为 扉一 到 扉十，从第 21 层起是屋顶 穹天1、穹天2，依此类推。这些名称是段位的称号，在所有语言中都相同。

## 模块一览

| 模块 | 内容 |
| --- | --- |
| `Pagoda.Levels` | 由楼层编号推导出的一切：歌曲池、名称、段位牌的边框和颜色、考核阈值、紫色歌曲的几率。纯查询。 |
| `Pagoda.Floor` | 一层的歌曲（一次抽选）及其段位：把抽选结果连同段位牌和考核交给 `DANBUILDER`。 |
| `Pagoda.Rewards` | 过关的奖励：名牌、金币、我的房间的家具、金库钥匙；保存它们，并通过 `modal` 只读活动显示弹窗。 |
| `Pagoda.Progress` | 持久的存档数据（见下文）。 |
| `Pagoda.Songs` | 塔从中抽歌的歌曲列表，以及检查原声音乐是否覆盖池中每个条目。 |
| `Pagoda.Run` | 可选：不带界面的挑战和练习流程。 |
| `Pagoda.Texts` | 模块自己的字符串（`lang/<code>/pagoda.json`）。 |

## Pagoda.Levels

| 函数 | 说明 |
| --- | --- |
| `Levels.get_pool(level)  -> pool` | 该层的池：由 `{ id, diff }`（歌曲的唯一 id 和难度，0 到 4）组成的列表 `{ blue, green, red, purple }`。没有自己条目的楼层使用其下方最近的条目。 |
| `Levels.load_pools()`, `Levels.clear_pools()` | 读取 `pools.json`（仅一次）；在重新枚举歌曲之后丢弃它。 |
| `Levels.level_name(level)  -> string` | 该层的名称。 |
| `Levels.level_tick(level)`, `Levels.level_tick_color(level)` | 段位牌的边框（0 到 4）和颜色 `{ r, g, b }`。 |
| `Levels.exam1_gauge(level)  -> red, gold` | 魂槽考核。 |
| `Levels.exam2(level)  -> type, red, gold, lessThan` | 准确率考核（第 1 到 20 层），或“可”数考核（第 21 层起）。 |
| `Levels.exam3_base(level)  -> red, gold` | 应用紫色歌曲宽限之前的“不可”数考核。 |
| `Levels.purple_prob(level)  -> number` | 出现紫色第四首歌的几率（第 20 层必定出现）。 |
| `Levels.pick_entry(entries, used)`, `Levels.shuffle(t)` | 抽选所用的辅助函数。 |

模块旁边的 `pools.json` 把每个楼层编号映射到它的池：

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

这些池指定的是 OpenTaiko 原声音乐中的歌曲。`Songs.count_missing_songs()` 记录并统计已安装歌曲所缺少的条目（某首歌或其难度），以便界面向玩家发出警告。

## Pagoda.Floor

| 函数 | 说明 |
| --- | --- |
| `Floor.rollFloor(level, avoid?, songList?)  -> roll` | 按打乱的顺序各抽一首蓝色、绿色和红色歌曲，再按该层的几率加入一首紫色歌曲，同一首歌不会出现两次。`avoid` 是一组歌曲 id，只要池中还有其他歌曲就不会抽到它们。抽选结果为 `{ songs = { { color, node, entry }, ... }, purple = { node, entry } or nil, speed }`。 |
| `Floor.rerollFloor(level, roll, keepMystery, songList?)  -> roll` | 在池允许的范围内换用其他歌曲重新抽选；`keepMystery` 保留紫色歌曲。 |
| `Floor.exams(level, hadPurple)  -> table` | 以 `{ type, red, gold, lessThan }` 的形式返回三项考核。 |
| `Floor.buildDan(level, roll?, opts?)  -> bool` | 为该层设置 `DANBUILDER`：标题、段位牌、类别（`opts.genre`、`opts.genreColor`）、歌曲和考核。一首歌都加不进去时为 false；否则用 `DANBUILDER:Mount()` 挂载后开始演奏。 |
| `Floor.speed(level)  -> number` | 该层的歌曲速度（一个 `CONFIG.SONGSPEED` 值）：穹天18 以下为 x1，之后逐渐提高。 |

## Pagoda.Run

挑战从某个已开放的起始楼层开始，每过一关上升一层，第一次失败时结束。练习可以演奏挑战所到达的最高楼层（至少第 11 层）以内的任意一层，并且不保存任何内容。

| 函数 | 说明 |
| --- | --- |
| `Run.startOptions()  -> table` | 该存档上开放的起始楼层：1、6 和 11 始终开放，16 在通过第 15 层后开放，21 在通过第 20 层后开放。 |
| `Run.new(start, songList?)  -> run` | 从该层开始的一次挑战。其字段：`level`（当前楼层）、`tower`（显示为已通过的最高楼层）、`roll`、`over`。 |
| `run:rollFloor(avoid?)`, `run:reroll(keepMystery)  -> roll` | 当前楼层的歌曲，在演奏前展示。 |
| `run:enter()` | 记录挑战已到达该层：从此练习也会提供这一层。 |
| `run:buildDan(opts?)  -> bool` | 为当前楼层设置好的 `DANBUILDER`；挂载后开始演奏。 |
| `run:clear()  -> result` | 过关后：发放并保存奖励，提高最高楼层，然后上升一层。返回 `{ level, rewards, shrouded }`：`rewards` 是 `Rewards.grant` 返回的列表，`shrouded` 表示该层是否位于塔的上方。 |
| `run:fail()` | 失败后：记住该层，计入一次游玩，结束挑战。 |
| `Run.practiceLevels()  -> table`, `Run.practiceDan(level, opts?)  -> bool` | 练习提供的楼层；为一次练习演奏设置好的 `DANBUILDER`，使用新的抽选结果。 |

## Pagoda.Rewards

| 函数 | 说明 |
| --- | --- |
| `Rewards.grant(level)  -> table` | 在玩家 1 的存档上发放并保存一次过关的奖励，并按顺序返回要显示的弹窗：`{ kind = "nameplate", id }`、`{ kind = "furniture", id }`、`{ kind = "key", key, count }`、`{ kind = "coins", amount }`。 |
| `Rewards.setup()` | 一次性共享钥匙图片（`pagoda_reward_key_<id>`）；请在显示钥匙弹窗之前调用。 |
| `Rewards.show(r)  -> bool` | 通过 `modal` 只读活动开始播放一个奖励的弹窗。 |
| `Rewards.update()  -> bool`, `Rewards.draw()`, `Rewards.stop()` | 运行弹窗（播放期间为 true；它自己读取输入）、绘制它、丢弃它。 |
| `Rewards.poolOf(level)`, `Rewards.nameplateOf(level)` | 某层的奖励池，以及首次通过该层所得的名牌。 |

首次通过某层会获得该层的名牌（id 381 到 397，位于 `Rewards.NAMEPLATES` 列出的楼层）。然后金币（某个范围内的数额，50% 几率）和该层池中的每个条目各按自己的几率抽选（`Rewards.POOLS`、`Rewards.ROOF_POOLS`），因此一次过关可能获得好几样东西。秘密金库关闭期间，金库钥匙会变成金币，而一次过关的所有金币合并为一个弹窗。家具弹窗显示由[我的房间](my-room.md)提供的共享纹理 `myroom_thumb_<id>`。

## 存档键

所有内容都保存在玩家 1 的存档中（`GetSaveFile(0)`）：

| 键 | 类型 | 内容 |
| --- | --- | --- |
| `pagoda_highest_level` | 计数器 | 挑战中通过的最高楼层。 |
| `pagoda_highest_reached` | 计数器 | 挑战到达的最高楼层，练习最多提供到这一层。 |
| `pagoda_plays` | 计数器 | 已进行的挑战次数（在失败时计数）。 |
| `pagoda_lost_<level>` | 触发器 | 曾在该层挑战失败。 |
| `pagoda_story_first_seen` | 触发器 | 开场剧情已播放（较旧的存档可能改为持有计数器 `pagoda_story_seen`）。 |
| `pagoda_held_mods`, `pagoda_held_mods_lo`, `pagoda_held_mods_hi` | 触发器、计数器 | 塔中演奏期间保留的玩家 1 的模组标志，分成两个 32 位的半部分（`Progress.hold_mods(flags)`、`held_mods()`、`drop_held_mods()`）。 |
| `pagoda_structure_version` | 计数器 | 旧存档的一次性重新编号（`Progress.migrate_save()`）已执行。 |
| `.myroom_<id>`, `.myroom_held_<id>` | 计数器 | 获得的家具：交给我的房间的，以及已拥有的。 |
| `.vault_key_count_<key>` | 计数器 | 获得的金库钥匙，通过 [Vault.Keys](vault.md) 处理。 |

名牌和金币通过存档自身的方法（`UnlockNameplate`、`EarnCoins`）处理。

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
