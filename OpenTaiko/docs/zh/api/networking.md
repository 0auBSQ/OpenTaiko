<!-- api/networking.md -->

# 在线联机 <span class="badge-exp">实验性</span>

`NET` 全局对象是 OpenTaiko Online 的客户端，这是在线大厅和多人舞台背后的点对点协议。一位玩家创建房间并获得房间码；其他玩家用该房间码加入。服务端（房间创建者，除非房间已迁移）中继所有流量，每位玩家都维护一份房间内成员的名单。

Lua 的使用方式：

- 所有数据跨越 Lua 边界时都是字符串。舞台以 JSON 携带结构化数据。
- 网络运行在后台线程上，并把事件推入一个队列。舞台每帧循环调用 `NET:Poll()` 直到它返回 nil，以清空队列。
- 联机默认关闭。“Allow LuaNetworking connections”设置会启用它；关闭期间，CreateRoom 返回 nil，JoinRoom 返回 false，客户端会排入一个 "error" 事件。
- 房间 id：房间创建者的 id 为 1。每位加入者按加入顺序获得下一个 id。不在房间内时 `NET:SelfId()` 为 0。
- 主持人角色与持有连接是分开的。它标记当前驱动大厅的玩家（选曲、开始演奏）。它最初属于创建者；RotateHost 或 SetHostRole 会把它交给另一位玩家。
- 房间创建者离开时，其余玩家会自动迁移到新的服务端；只有在没有人能够接管时房间才会关闭。

```lua
function update(ts)
    while true do
        local e = NET:Poll()
        if e == nil then break end
        if e.Type == "message" and e.Channel == "chat" then
            addChatLine(e.Peer, e.Data)
        elseif e.Type == "left" then
            removePlayer(e.Peer)
        elseif e.Type == "roomclosed" then
            return Exit("title")
        end
    end
end
```

## NET

### 房间生命周期

<div class="callout warn">
以全局对象 NET 的形式提供。请在 CreateRoom 或 JoinRoom 之前调用 SetLocalPlayer，以便其他玩家在你加入时收到你的信息。JoinRoom 是异步的：结果稍后以 "connected" 或 "error" 事件到达。创建或加入房间会先离开当前所在的房间。
</div>

| 方法 | 说明 |
| --- | --- |
| `NET:SetLocalPlayer(infoJson)  -> void` | 设置本地玩家的信息字符串（通常是包含名牌、角色等的 JSON）。客户端会在你加入时把它发送给其他玩家，它会出现在他们的 "joined" 事件和 PeersJson 中。空字符串变为 "{}"；服务端会把超过 16384 个字符的字符串替换为 "{}"。 |
| `NET:CreateRoom(stageId, payload, maxPlayers)  -> string` | 为给定的舞台 id 创建一个房间，附带可选的负载字符串，并返回其房间码；联机关闭或创建房间失败时返回 nil。maxPlayers 限制名单人数（默认 8）。 |
| `NET:CreateRoom(stageId, payload)  -> string` | 同上，使用默认的 8 人上限。 |
| `NET:PeekStageId(roomCode)  -> string` | 不连接即返回房间码中存储的舞台 id；房间码无效时返回 nil。让加入界面可以路由到正确的舞台。 |
| `NET:JoinRoom(roomCode)  -> bool` | 开始加入房间。联机关闭或房间码格式错误时立即返回 false；否则结果以 "connected" 或 "error" 事件到达。 |
| `NET:Leave()  -> void` | 离开房间并清空事件队列。房间创建者在其他人仍在时离开，会由另一位玩家接管房间；否则房间对所有人关闭。 |
| `NET.PortOverride  (int, settable)` | 大于 0 时，房间使用此 TCP 端口；0 选择默认端口 41234。正常使用时保持为 0。 |

### 消息

<div class="callout warn">
频道名称由舞台自由选择。演奏回合机制保留了频道名称 "ps"、"ld" 和 "fn"，它们永远不会到达 Poll()。
</div>

| 方法 | 说明 |
| --- | --- |
| `NET:Broadcast(channel, data)  -> void` | 在给定频道上向房间内的其他所有玩家发送一条消息。 |
| `NET:SendTo(peerId, channel, data)  -> void` | 在给定频道上按 id 向一位玩家发送一条消息。-1 发送给其他所有人，与 Broadcast 相同。 |
| `NET:Poll()  -> NetEvent` | 移除并返回下一个待处理事件；队列为空时返回 nil。 |

### 房间状态与主持人角色

| 方法 | 说明 |
| --- | --- |
| `NET:SelfId()  -> int` | 本玩家的 id（房间创建者为 1）；不在房间内时为 0。 |
| `NET:Connected()  -> bool` | 在房间内时为 true：本玩家拥有自身 id，且是服务端或已连接到服务端。 |
| `NET:IsHost()  -> bool` | 本玩家持有房间连接（房间创建者，或迁移后接管的玩家）时为 true。 |
| `NET:HasHostRole()  -> bool` | 本玩家当前持有主持人角色时为 true。 |
| `NET:HostRoleId()  -> int` | 持有主持人角色的玩家的 id。 |
| `NET:PeerCount()  -> int` | 名单中的玩家数量，包括自己。 |
| `NET:PeersJson()  -> string` | 以 JSON 数组按加入顺序给出的名单。每个条目为 {id, info, isHost, hostRole}：info 是该玩家传给 SetLocalPlayer 的字符串，isHost 对 id 1 为 true，hostRole 对当前主持人角色持有者为 true。 |
| `NET:RotateHost()  -> void` | 把主持人角色交给加入顺序中的下一位玩家。仅在服务端上有效；它会忽略其他玩家的调用。 |
| `NET:SetHostRole(peerId)  -> void` | 把主持人角色交给指定玩家并向房间宣布。仅在服务端上有效，且只对名单中的 id 有效。 |

### 演奏回合

在线歌曲进行期间，游戏挂起大厅舞台，由演奏界面自行驱动数据交换：它广播本地玩家的实时分数，用网络数据填充远程玩家位，并等待加载和结束屏障。大厅只需要声明谁在哪个玩家位演奏，并开启和关闭回合。

<div class="callout warn">
玩家位是演奏界面上的玩家位置：位 0 始终是本地玩家，位 1 及以上是远程玩家。用 SetPlaySpots 设置映射，然后在进入演奏前调用 BeginPlaySync，并在大厅重新获得控制后调用 EndPlaySync。演奏界面调用屏障、分数推送和概率方法；它们出现在这里是因为同一个对象公开了它们。
</div>

| 方法 | 说明 |
| --- | --- |
| `NET:SetPlaySpots(json)  -> void` | 从一个 id 的 JSON 数组设置即将开始的演奏的玩家位到玩家的映射，索引 0 为自己（例如 "[1,3,2]"）。无法解析的字符串会清除映射。 |
| `NET:PlaySpotCount()  -> int` | 当前玩家位映射中的条目数量（未设置时为 0）。 |
| `NET:BeginPlaySync(selfName)  -> void` | 开启一个演奏回合：清除先前的分数，并记录本地玩家的显示名称以用于实时分数消息。 |
| `NET:EndPlaySync()  -> void` | 关闭演奏回合，并清除屏障和各玩家位的结果。 |
| `NET:IsRemoteSpot(spot)  -> bool` | 在开启的演奏回合中该玩家位映射到远程玩家时为 true。 |
| `NET:IsSpotActive(spot)  -> bool` | 映射到该玩家位的玩家仍在名单中时为 true（位 0 始终为 true）。false 表示该玩家在演奏中途掉线。 |
| `NET:GetSpotPlayJson(spot)  -> string` | 某个远程玩家位最近发送的实时分数 JSON，或空字符串。键：n（名称）、s（分数）、g（魂槽）、a（准确率）、gr（良）、gd（可）、ms（不可）、co（连击）。 |
| `NET:GetSelfPlayScore()  -> string` | 本客户端最后广播的实时分数 JSON。 |
| `NET:LivePlayScoresJson()  -> string` | 每位玩家最后的实时分数 JSON，以 {id, d} 的 JSON 数组给出，其中 d 是分数 JSON 字符串。本客户端至少广播过一次后，数组也会包含自己。 |
| `NET:GetSpotResultJson(spot)  -> string` | 某个远程玩家位在歌曲结束时报告的最终结果 JSON，或空字符串。键：cl（过关）、fc（全连击）、pf（全良）、mx（彩虹魂槽）为布尔值；gr、gd、ms、rl（连打）、bl（气球）、ad（ADLIB）、hc（最高连击）、sc（分数）为整数。 |
| `NET:GetSpotClearLevel(spot)  -> int` | 由远程玩家位最终结果得出的过关等级：2 彩虹，1 过关，0 失败，-1 尚未收到结果。 |
| `NET:GetSpotJudge(spot, key)  -> int` | 按键取远程玩家位最终结果中的一个整数字段（见 GetSpotResultJson）；尚未收到结果时为 -1。 |
| `NET:GetSpotBadOdds(spot)  -> int` | 游戏把远程玩家位下一次自动击打判为不可的千分比概率（0-1000），由其广播的判定计数推导。 |
| `NET:GetSpotGoodOdds(spot)  -> int` | 游戏把远程玩家位下一次自动击打判为良的千分比概率（0-1000）。 |
| `NET:PushPlayScore(json)  -> void` | 广播本地玩家的实时分数 JSON。演奏界面每秒调用它数次。 |
| `NET:ReportLoaded()  -> void` | 报告一次本客户端已完成歌曲加载。 |
| `NET:LoadBarrierReady(timeoutMs)  -> bool` | 名单中的每位玩家都报告已加载后为 true，或自本客户端报告后超时已过时为 true（服务端随后移除从未报告的玩家）。 |
| `NET:ReportFinished(resultJson)  -> void` | 报告一次本客户端已完成歌曲，附带其最终结果 JSON。 |
| `NET:FinishBarrierReady(timeoutMs)  -> bool` | 名单中的每位玩家都报告已完成后为 true，或超时已过时为 true。 |
| `NET:BarrierReset()  -> void` | 清除加载和结束屏障以及各玩家位的结果。 |

## NetEvent

由 NET:Poll() 返回的对象，描述一个网络事件。

<div class="callout warn">
直接读取其字段；它没有方法。事件没有相关玩家时 Peer 为 0。
</div>

| 方法 | 说明 |
| --- | --- |
| `event.Type  -> string` | 事件类型，为下面列出的值之一。 |
| `event.Peer  -> int` | 相关玩家的 id："connected" 为你自己的 id，"message" 为发送者，"joined"、"left" 和 "hostrole" 为受影响的玩家；其他情况为 0。 |
| `event.Channel  -> string` | "message" 事件的频道；其他类型为空。 |
| `event.Data  -> string` | 事件负载；见下文。 |

| 类型 | 含义 |
| --- | --- |
| `connected` | 你加入了一个房间。Data 是 JSON 对象 {selfId, stageId, payload, hostRoleId}。房间创建者永远不会收到此事件。已在房间内的玩家出现在 PeersJson 中，不会触发 "joined" 事件。 |
| `joined` | 有玩家在你之后加入。Peer 是其 id，Data 是其信息字符串。 |
| `left` | 有玩家离开或被服务端移除。Peer 是其 id。 |
| `message` | 来自另一位玩家的消息。Peer 是发送者，Channel 和 Data 是消息内容。 |
| `hostrole` | 主持人角色易手。Peer 是新持有者的 id。 |
| `roomclosed` | 房间已关闭，或连接恢复失败。 |
| `error` | 要显示给玩家的状态消息。Data 是文本。客户端在联机关闭、创建或加入失败、房间已满时触发它，并在主持人迁移期间将其作为进度通知触发。 |
