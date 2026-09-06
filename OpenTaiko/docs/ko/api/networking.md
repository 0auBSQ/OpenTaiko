<!-- api/networking.md -->

# 온라인 네트워킹 <span class="badge-exp">실험적</span>

`NET` 전역은 온라인 로비와 멀티플레이어 스테이지의 기반이 되는 P2P 프로토콜인 OpenTaiko Online의 클라이언트입니다. 한 플레이어가 방을 만들고 방 코드를 받으며, 다른 플레이어는 그 코드로 참가합니다. 서버(방을 만든 사람, 방이 마이그레이션되지 않은 한)가 모든 트래픽을 중계하고, 모든 플레이어는 방에 누가 있는지의 명단을 유지합니다.

Lua에서 사용하는 방법:

- 모든 데이터는 문자열로 Lua 경계를 넘습니다. 스테이지는 구조화된 데이터를 JSON으로 실어 나릅니다.
- 네트워크는 백그라운드 스레드에서 실행되며 이벤트를 큐에 넣습니다. 스테이지는 매 프레임 nil을 반환할 때까지 `NET:Poll()`을 반복 호출해 큐를 비웁니다.
- 네트워킹은 기본적으로 꺼져 있습니다. "Allow LuaNetworking connections" 설정이 이를 켭니다. 꺼져 있는 동안 CreateRoom은 nil을, JoinRoom은 false를 반환하고 클라이언트는 "error" 이벤트를 큐에 넣습니다.
- 방 id: 방을 만든 사람이 id 1입니다. 참가자는 참가 순서대로 다음 id를 받습니다. `NET:SelfId()`는 방 밖에서 0입니다.
- 호스트 역할은 연결 소유와 별개입니다. 현재 로비를 이끄는(곡을 고르고 플레이를 시작하는) 플레이어를 표시합니다. 방을 만든 사람에게서 시작하며, RotateHost나 SetHostRole이 다른 플레이어에게 넘깁니다.
- 방을 만든 사람이 떠나면 남은 플레이어는 자동으로 새 서버로 마이그레이션됩니다. 아무도 넘겨받을 수 없을 때만 방이 닫힙니다.

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

### 방 생명 주기

<div class="callout warn">
전역 NET으로 사용할 수 있습니다. 다른 플레이어가 참가 시 당신의 정보를 받도록 CreateRoom이나 JoinRoom 전에 SetLocalPlayer를 호출하십시오. JoinRoom은 비동기입니다. 결과는 나중에 "connected" 또는 "error" 이벤트로 도착합니다. 방을 만들거나 참가하면 먼저 현재 방을 떠납니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `NET:SetLocalPlayer(infoJson)  -> void` | 로컬 플레이어의 정보 문자열(보통 네임플레이트, 캐릭터 등을 담은 JSON)을 설정합니다. 클라이언트는 참가할 때 이를 다른 플레이어에게 전송하며, 그들의 "joined" 이벤트와 PeersJson에 나타납니다. 빈 문자열은 "{}"가 되고, 16384자를 넘는 문자열은 서버가 "{}"로 대체합니다. |
| `NET:CreateRoom(stageId, payload, maxPlayers)  -> string` | 지정한 스테이지 id로 선택적 payload 문자열과 함께 방을 만들고 방 코드를 반환하며, 네트워킹이 꺼져 있거나 방 생성이 실패하면 nil. maxPlayers는 명단 상한입니다(기본값 8). |
| `NET:CreateRoom(stageId, payload)  -> string` | 기본 상한 8명으로 같은 동작. |
| `NET:PeekStageId(roomCode)  -> string` | 연결하지 않고 방 코드에 저장된 스테이지 id를 반환하며, 코드가 유효하지 않으면 nil. 참가 화면이 올바른 스테이지로 라우팅할 수 있게 합니다. |
| `NET:JoinRoom(roomCode)  -> bool` | 방 참가를 시작합니다. 네트워킹이 꺼져 있거나 코드 형식이 잘못되었으면 즉시 false를 반환하고, 그렇지 않으면 결과가 "connected" 또는 "error" 이벤트로 도착합니다. |
| `NET:Leave()  -> void` | 방을 떠나고 이벤트 큐를 비웁니다. 다른 사람이 남아 있는데 방을 만든 사람이 떠나면 다른 플레이어가 방을 넘겨받고, 그렇지 않으면 모두에게 닫힙니다. |
| `NET.PortOverride  (int, settable)` | 0보다 크면 방이 이 TCP 포트를 사용합니다. 0이면 기본 포트 41234를 선택합니다. 보통은 0으로 두십시오. |

### 메시징

<div class="callout warn">
채널 이름은 스테이지가 자유롭게 고릅니다. 채널 이름 "ps", "ld", "fn"은 플레이 라운드 메커니즘이 예약하며 Poll()에 도달하지 않습니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `NET:Broadcast(channel, data)  -> void` | 지정한 채널로 방의 다른 모든 플레이어에게 메시지를 보냅니다. |
| `NET:SendTo(peerId, channel, data)  -> void` | 지정한 채널로 id가 지정된 플레이어 한 명에게 메시지를 보냅니다. -1은 Broadcast처럼 다른 모두에게 보냅니다. |
| `NET:Poll()  -> NetEvent` | 대기 중인 다음 이벤트를 꺼내 반환하며, 큐가 비어 있으면 nil. |

### 방 상태와 호스트 역할

| 메서드 | 설명 |
| --- | --- |
| `NET:SelfId()  -> int` | 이 플레이어의 id(방을 만든 사람은 1). 방에 없으면 0. |
| `NET:Connected()  -> bool` | 방에 있으면 true: 이 플레이어가 자신의 id를 가지고 있고 서버이거나 서버에 연결되어 있음. |
| `NET:IsHost()  -> bool` | 이 플레이어가 방 연결을 소유하면 true(방을 만든 사람, 또는 마이그레이션 후 넘겨받은 플레이어). |
| `NET:HasHostRole()  -> bool` | 이 플레이어가 현재 호스트 역할을 가지고 있으면 true. |
| `NET:HostRoleId()  -> int` | 호스트 역할을 가진 플레이어의 id. |
| `NET:PeerCount()  -> int` | 자신을 포함한 명단의 플레이어 수. |
| `NET:PeersJson()  -> string` | 참가 순서의 JSON 배열로 된 명단. 각 항목은 {id, info, isHost, hostRole}입니다. info는 그 플레이어가 SetLocalPlayer에 준 문자열, isHost는 id 1이면 true, hostRole은 현재 호스트 역할 보유자이면 true. |
| `NET:RotateHost()  -> void` | 호스트 역할을 참가 순서의 다음 플레이어에게 넘깁니다. 서버에서만 동작하며 다른 플레이어의 호출은 무시합니다. |
| `NET:SetHostRole(peerId)  -> void` | 지정한 플레이어에게 호스트 역할을 주고 방에 알립니다. 서버에서만, 그리고 명단에 있는 id에 대해서만 동작합니다. |

### 플레이 라운드

온라인 곡 진행 중에는 게임이 로비 스테이지를 중단하고 게임플레이 화면이 교환을 직접 이끕니다. 로컬 플레이어의 진행 중 스코어를 브로드캐스트하고, 원격 스팟에 네트워크에서 받은 값을 채우며, 로딩과 종료 배리어에서 대기합니다. 로비는 누가 어느 스팟에서 플레이하는지 선언하고 라운드를 열고 닫기만 하면 됩니다.

<div class="callout warn">
스팟은 게임플레이 화면의 플레이어 위치입니다. 스팟 0은 항상 로컬 플레이어이고 스팟 1 이상은 원격 플레이어입니다. SetPlaySpots로 대응을 설정한 뒤 플레이에 들어가기 직전에 BeginPlaySync를, 로비가 제어를 되찾으면 EndPlaySync를 호출하십시오. 배리어, 스코어 푸시, 확률 메서드는 게임플레이 화면이 호출합니다. 같은 객체가 노출하기 때문에 여기에 나열합니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `NET:SetPlaySpots(json)  -> void` | 다가오는 플레이의 스팟-플레이어 대응을 id의 JSON 배열로 설정합니다. 인덱스 0이 자신입니다(예: "[1,3,2]"). 파싱할 수 없는 문자열은 대응을 비웁니다. |
| `NET:PlaySpotCount()  -> int` | 현재 스팟 대응의 항목 수(설정되지 않았으면 0). |
| `NET:BeginPlaySync(selfName)  -> void` | 플레이 라운드를 엽니다. 이전 스코어를 비우고 진행 중 스코어 메시지를 위해 로컬 플레이어의 표시 이름을 기록합니다. |
| `NET:EndPlaySync()  -> void` | 플레이 라운드를 닫고 배리어와 스팟별 결과를 비웁니다. |
| `NET:IsRemoteSpot(spot)  -> bool` | 열린 플레이 라운드 동안 스팟이 원격 플레이어에 대응되면 true. |
| `NET:IsSpotActive(spot)  -> bool` | 스팟에 대응된 플레이어가 아직 명단에 있으면 true(스팟 0은 항상 true). false는 플레이어가 플레이 도중 이탈했음을 뜻합니다. |
| `NET:GetSpotPlayJson(spot)  -> string` | 원격 스팟이 보낸 최신 진행 중 스코어 JSON. 없으면 빈 문자열. 키: n(이름), s(스코어), g(게이지), a(정확도), gr(great), gd(good), ms(bad), co(콤보). |
| `NET:GetSelfPlayScore()  -> string` | 이 클라이언트가 마지막으로 브로드캐스트한 진행 중 스코어 JSON. |
| `NET:LivePlayScoresJson()  -> string` | 모든 플레이어의 마지막 진행 중 스코어 JSON을 {id, d}의 JSON 배열로. d는 스코어 JSON 문자열입니다. 이 클라이언트가 한 번 이상 브로드캐스트했으면 배열에 자신도 포함됩니다. |
| `NET:GetSpotResultJson(spot)  -> string` | 곡이 끝날 때 원격 스팟이 보고한 최종 결과 JSON. 없으면 빈 문자열. 키: cl(클리어), fc(풀 콤보), pf(퍼펙트), mx(무지개 게이지)는 불리언; gr, gd, ms, rl(연타), bl(풍선), ad(애드립), hc(최고 콤보), sc(스코어)는 정수. |
| `NET:GetSpotClearLevel(spot)  -> int` | 원격 스팟의 최종 결과에서 얻은 클리어 레벨: 2 무지개, 1 클리어, 0 실패, 결과가 도착하지 않았으면 -1. |
| `NET:GetSpotJudge(spot, key)  -> int` | 키로 원격 스팟 최종 결과의 정수 필드 하나(GetSpotResultJson 참고). 결과가 도착하지 않았으면 -1. |
| `NET:GetSpotBadOdds(spot)  -> int` | 브로드캐스트된 판정 수에서 도출한, 게임이 원격 스팟의 다음 자동 히트를 bad로 판정할 천분율 확률(0-1000). |
| `NET:GetSpotGoodOdds(spot)  -> int` | 게임이 원격 스팟의 다음 자동 히트를 good으로 판정할 천분율 확률(0-1000). |
| `NET:PushPlayScore(json)  -> void` | 로컬 플레이어의 진행 중 스코어 JSON을 브로드캐스트합니다. 게임플레이 화면이 초당 여러 번 호출합니다. |
| `NET:ReportLoaded()  -> void` | 이 클라이언트가 곡 로드를 마쳤음을 한 번 보고합니다. |
| `NET:LoadBarrierReady(timeoutMs)  -> bool` | 명단의 모든 플레이어가 로드 완료를 보고했거나, 이 클라이언트가 보고한 뒤 타임아웃이 지나면 true(그러면 서버는 보고하지 않은 플레이어를 제외합니다). |
| `NET:ReportFinished(resultJson)  -> void` | 이 클라이언트가 곡을 마쳤음을 최종 결과 JSON과 함께 한 번 보고합니다. |
| `NET:FinishBarrierReady(timeoutMs)  -> bool` | 명단의 모든 플레이어가 종료를 보고했거나 타임아웃이 지나면 true. |
| `NET:BarrierReset()  -> void` | 로딩과 종료 배리어, 스팟별 결과를 비웁니다. |

## NetEvent

NET:Poll()이 반환하는, 네트워크 이벤트 하나를 설명하는 객체입니다.

<div class="callout warn">
필드를 직접 읽으십시오. 메서드는 없습니다. 이벤트에 관련 플레이어가 없으면 Peer는 0입니다.
</div>

| 메서드 | 설명 |
| --- | --- |
| `event.Type  -> string` | 이벤트 종류. 아래 나열된 값 중 하나. |
| `event.Peer  -> int` | 관련 플레이어 id: "connected"는 자신의 id, "message"는 보낸 사람, "joined", "left", "hostrole"은 영향을 받는 플레이어. 그 외에는 0. |
| `event.Channel  -> string` | "message" 이벤트의 채널. 다른 종류에서는 빈 문자열. |
| `event.Data  -> string` | 이벤트 페이로드. 아래 참고. |

| 종류 | 의미 |
| --- | --- |
| `connected` | 방에 참가했습니다. Data는 JSON 객체 {selfId, stageId, payload, hostRoleId}입니다. 방을 만든 사람은 이 이벤트를 받지 않습니다. 이미 방에 있던 플레이어는 PeersJson에 나타나며 "joined" 이벤트를 발생시키지 않습니다. |
| `joined` | 당신 뒤에 플레이어가 참가했습니다. Peer는 그의 id, Data는 그의 정보 문자열입니다. |
| `left` | 플레이어가 떠났거나 서버가 제외했습니다. Peer는 그의 id입니다. |
| `message` | 다른 플레이어가 보낸 메시지. Peer는 보낸 사람, Channel과 Data는 메시지입니다. |
| `hostrole` | 호스트 역할이 넘어갔습니다. Peer는 새 보유자의 id입니다. |
| `roomclosed` | 방이 닫혔거나 연결 복구가 실패했습니다. |
| `error` | 플레이어에게 보여 줄 상태 메시지. Data는 텍스트입니다. 클라이언트는 네트워킹이 꺼져 있을 때, 방 생성이나 참가가 실패할 때, 방이 가득 찼을 때, 그리고 호스트 마이그레이션 중 진행 알림으로 이를 발생시킵니다. |
