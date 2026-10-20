<!-- modules/online.md -->

# Online

在线大厅的逻辑：通过代码打开和加入的房间、玩家列表、每位玩家的难度、模组和准备状态、房间的歌曲、一轮的开始、结算以及下一轮。`Online` 模块不绘制任何内容；皮肤在其上绘制自己的大厅界面，并接入自己的选曲界面。每个皮肤的大厅都使用相同的消息，因此使用不同皮肤的玩家可以在同一个房间相遇。Open-World Memories 的 `onlinelobby` 舞台就是这样的一个客户端。这些模块运行在实验性的 [NET 全局对象](../api/networking.md) 之上。

## 模块

| 模块 | 内容 |
| --- | --- |
| `Online.Lobby` | 会话：它的状态、界面执行的每个操作、与选曲界面的连接以及事件处理。 |
| `Online.Protocol` | 消息：频道名称和每种负载的 JSON。 |
| `Online.Identity` | 玩家发送的自身信息（名字、角色、小角色、名牌、段位牌、调色板）。 |
| `Online.Media` | 本机上房间的歌曲：它的等级、共享纹理 `preimage` 中的封面、共享声音 `presound` 中的试听。 |
| `Online.Texts` | 模块自己的字符串（`lang/<code>/online.json`）。 |

皮肤只需要 `Online.Lobby`，它会重新导出其他模块中用到的部分。每个 Lua 状态只有一个会话：模块把状态保存在 `Lobby.net` 中。

## 会话状态

皮肤每帧读取 `Lobby.net` 来绘制界面：

| 字段 | 说明 |
| --- | --- |
| `online`, `connecting` | 在房间中；正在等待加入的回应。 |
| `isHost`, `code` | 本机运行该房间（房间由它创建）；房间代码。 |
| `roomGone` | 房间已关闭：退出并返回皮肤的菜单。 |
| `msg`, `msgCode`, `msgArg` | 最近一条消息（已是游戏语言）、它的固定代码和参数（见"消息"）。 |
| `notice` | 在房间中收到的消息，显示一次后清除。 |
| `nameByPeer`, `infoByPeer` | 按 id 的玩家：名字，以及 `Online.Identity` 的信息（`name`、`char`、`puchi`、`dan`、`npk`、`dgold`、`dtype`、`palStops`、`palBlend`）。 |
| `diffByPeer`, `modByPeer`, `readyByPeer` | 每位玩家的难度（0 到 4）、模组和准备状态。 |
| `lackByPeer`, `watchByPeer` | 没有房间歌曲的玩家；仍在结算界面的玩家。 |
| `song` | 房间的歌曲 `{ uid, title, diff, speed, dyn }`，在房主角色选曲前为 nil。 |
| `songLevels`, `songPlus`, `songSubtitle`, `songBpm`, `iLackSong` | 本机上的歌曲：它拥有的每个难度的等级和加号标记、副标题和 BPM；本机没有该歌曲时为 `iLackSong`。 |
| `resultByPeer` | 本轮结算：`{ name, sc, gr, gd, ms, co, cl, fc, pf, ac, ab }`（分数、良、可、不可、连击、通关、全连、全良、准确率、中断）。 |
| `resultsT`, `resultsReadyT` | 在结算界面的秒数；结算齐全时设置。 |

## Online.Lobby

| 函数 | 说明 |
| --- | --- |
| `Lobby.host()  -> bool` | 打开一个房间（最多 `Lobby.MAXP` = 5 名玩家）并用 `STORAGE:WriteLobbyCode` 保存其代码；NET 拒绝时返回 false 并设置 `net.msg`。 |
| `Lobby.join(code)  -> bool` | 开始加入代码对应的房间（忽略空格）；回应通过 `drain` 到达。代码为空、无效或属于其他类型的房间时返回 false 并设置 `net.msg`。 |
| `Lobby.leave()` | 结束会话（加入失败后也一样），并把玩家位置还给存档。 |
| `Lobby.drain()` | 处理所有排队的 NET 事件；在 `online` 或 `connecting` 期间每帧调用。 |
| `Lobby.amController()  -> bool` | 该玩家持有房主角色：由其选曲并开始本轮。 |
| `Lobby.peerIds()`, `Lobby.count()` | 按顺序的玩家 id；玩家人数。 |
| `Lobby.spotOf[id]` | 每位玩家的本地位置（自己为 0），供 `NAMEPLATE:DrawPlayerNameplate` 使用。 |
| `Lobby.myDiff()`, `Lobby.setDiff(d)`, `Lobby.stepDiff(dir)  -> bool` | 我的难度；设置它；移到歌曲拥有的下一个难度（到行尾时为 false）。 |
| `Lobby.availDiffs()`, `Lobby.diffAvailable(d)`, `Lobby.fixDiff()` | 歌曲拥有的难度；让我的难度保持在其中之一（在大厅每帧调用）。 |
| `Lobby.myReady()`, `Lobby.setReady(b)` | 我的准备状态。 |
| `Lobby.broadcastMods()` | 从 `CONFIG` 读取玩家 0 的模组并发送；在皮肤的模组选择关闭时调用。 |
| `Lobby.applyModsToSlot(slot, mods)` | 把某位玩家的模组放到 `CONFIG` 的一个玩家槽位上，以便用 `modicons` ROActivity 绘制其图标。 |
| `Lobby.adjustSpeed(delta)`, `Lobby.setSpeed(sp)` | 房间的歌曲速度（仅房主角色）。 |
| `Lobby.startProblem()  -> "need_song", "need_players" or nil` | 本轮暂时不能开始的原因。不要求准备状态。 |
| `Lobby.hostStart()  -> bool` | 开始本轮（仅房主角色）。 |
| `Lobby.takeGo()  -> bool` | 本轮开始时返回一次 true；之后调用 `launchPlay`。 |
| `Lobby.launchPlay()  -> true, "eject" or false` | 为每位玩家挂载歌曲并打开游玩回合：为 true 时皮肤退出到 `"play"`；`"eject"` 表示本机没有该歌曲，必须离开房间；false 时附带 `net.msg`。 |
| `Lobby.returnedFromPlay()` | 在那次游玩后重新进入舞台时调用：发送我的结算并开始结算界面。 |
| `Lobby.tickResults(dt)  -> bool` | 结算界面的每一帧：所有结算到齐或经过 `Lobby.RESULTS_WAIT`（20）秒后返回 true。 |
| `Lobby.standings()  -> rows` | 结算，分数最高者在前：`{ id, name, r }`，该玩家仍在游玩时 `r` 为 nil。 |
| `Lobby.nextRound()` | 返回大厅：房主角色交给下一位玩家。 |
| `Lobby.tickPreview(dt)`, `Lobby.stopPreview()` | 在大厅和结算界面循环播放歌曲试听；停止试听。 |
| `Lobby.onEvent` | 可选函数 `(kind, peer, data)`，`drain` 处理的每个事件（`"joined"`、`"left"`、`"song"`、`"ready"`、`"go"`、`"result"`，...）都会调用它。 |

在线游玩看起来与相同人数的离线游玩一样：其他玩家作为虚拟槽位占用本地位置 2 到 5，显示各自的角色和名牌，游戏界面根据网络数据让他们游玩。

## 与选曲界面的连接

持有房主角色的玩家在皮肤的选曲界面中选择房间的歌曲。选曲界面只能选歌（没有难度步骤，没有自动）；它的速度键会改变 `CONFIG.SongSpeed`。三次调用完成连接：

1. 用 `Lobby.openSongSelect(open)` 代替直接激活：`open` 是激活选曲界面的函数。房间的速度会先放入 `CONFIG.SongSpeed`。
2. 确认歌曲时：先停用选曲界面，再调用 `Lobby.songPicked(uid?)`。`uid` 默认为 `SONGMOUNT:ChosenUniqueId()`。
3. 未选歌就离开选曲界面时：先停用它，再调用 `Lobby.songPickCancelled()`，它会保留在那里改变的速度，并恢复房间的封面和试听。

共享字符串 `presound_path` 指明 `presound` 中的音频：让房间歌曲在其中继续播放的选曲界面会交回试听，大厅会继续播放而不是重新开始。

## 消息

`net.msg` 可以直接显示。自己撰写消息的皮肤改为读取 `net.msgCode`：

| 代码 | 何时 |
| --- | --- |
| `open_failed`, `join_failed` | NET 拒绝了但没有说明原因。 |
| `room_open` | 房间已打开，代码已保存。 |
| `connecting` | 开始加入。 |
| `no_code`, `code_invalid` | 代码为空，或不是房间代码。 |
| `code_other` | 代码属于其他类型的房间；`msgArg` 是其舞台 id。 |
| `chart_failed` | `launchPlay` 无法挂载谱面。 |
| `net` | 来自 NET 本身的英文文本（网络已关闭、连接丢失等）。 |

在玩家于设置中允许之前，网络是关闭的。皮肤应在打开大厅前检查 `CONFIG.AreLuaNetworkingConnectionsAllowed`；NET 也会以 `net` 消息拒绝。

## Online.Protocol

每条消息都是一个 NET 频道上的 JSON 字符串。无论由哪个舞台打开，代码中的房间 id 都是 `"onlinelobby"`（`Protocol.ROOM_ID`）。

| 频道 | 负载 | 发送时机 |
| --- | --- | --- |
| `song` | `{"u":uid,"t":title,"d":0,"s":speed,"b":dyn}` | 由房主角色在歌曲或其速度改变时发送，并发给每位新加入者。 |
| `diff`, `mod`, `ready` | `{"d":n}`, `{"r","st","ju","tz","ss","sm"}`, `{"r":bool}` | 玩家改变它们时发送，并发给每位新加入者。 |
| `have`, `watch` | `{"h":bool}`, `{"w":bool}` | 玩家是否拥有该歌曲；是否在结算界面。 |
| `go` | `{}` | 本轮开始。 |
| `result` | `{"n","sc","gr","gd","ms","co","cl","fc","pf","ac","ab"}` | 歌曲结束后玩家的结算。 |

## 最简大厅

```lua
local Lobby = require("Online.Lobby")
local net = Lobby.net
local screen, back = "menu", false

function activate()
    if back then back = false; Lobby.returnedFromPlay(); screen = "results" end
end

function update(ts)
    local dt = 1 / 60
    if net.online or net.connecting then
        Lobby.drain()
        if net.roomGone then Lobby.leave(); screen = "menu" end
    end
    if screen == "lobby" then
        Lobby.tickPreview(dt)
        Lobby.fixDiff()
        if Lobby.takeGo() then
            local r = Lobby.launchPlay()
            if r == true then back = true; return Exit("play", nil) end
            if r == "eject" then Lobby.leave(); screen = "menu" end
        end
    elseif screen == "results" then
        if Lobby.tickResults(dt) and decidePressed() then Lobby.nextRound(); screen = "lobby" end
    end
end

function onDestroy() Lobby.leave() end
```

从皮肤菜单调用 `Lobby.host()` 或 `Lobby.join(code)` 会把 `screen` 设为 `"lobby"`；歌曲由上面的选曲界面连接设置。
