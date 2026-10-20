<!-- modules/online.md -->

# Online

온라인 로비의 로직입니다: 코드로 열고 참가하는 방, 플레이어 목록, 각 플레이어의 난이도·모드·준비 상태, 방의 곡, 라운드 시작, 결과, 그리고 다음 라운드. `Online` 모듈은 아무것도 그리지 않습니다. 스킨이 그 위에 자신의 로비 화면을 그리고 자신의 곡 선택을 연결합니다. 모든 스킨의 로비가 같은 메시지를 주고받으므로, 서로 다른 스킨의 플레이어가 한 방에서 만납니다. Open-World Memories의 `onlinelobby` 스테이지가 그런 클라이언트 중 하나입니다. 이 모듈들은 실험적인 [NET 전역](../api/networking.md) 위에서 동작합니다.

## 모듈

| 모듈 | 담고 있는 것 |
| --- | --- |
| `Online.Lobby` | 세션: 그 상태, 화면이 하는 모든 동작, 곡 선택과의 연결, 이벤트 처리. |
| `Online.Protocol` | 메시지: 채널 이름과 각 페이로드의 JSON. |
| `Online.Identity` | 플레이어가 자신에 대해 보내는 것 (이름, 캐릭터, 푸치캐릭터, 네임플레이트, 단위 플레이트, 팔레트). |
| `Online.Media` | 이 기기에 있는 방의 곡: 그 레벨, 공유 텍스처 `preimage`의 재킷, 공유 사운드 `presound`의 미리듣기. |
| `Online.Texts` | 모듈 자체의 문자열 (`lang/<code>/online.json`). |

스킨에는 `Online.Lobby`만 있으면 되며, 다른 모듈에서 쓰는 것은 이것이 다시 내보냅니다. 세션은 Lua 상태마다 하나이고, 모듈은 그 상태를 `Lobby.net`에 둡니다.

## 세션 상태

스킨은 화면을 그리기 위해 매 프레임 `Lobby.net`을 읽습니다:

| 필드 | 설명 |
| --- | --- |
| `online`, `connecting` | 방에 있음; 참가 응답을 기다리는 중. |
| `isHost`, `code` | 이 기기가 방을 운영함 (방을 만들었음); 방 코드. |
| `roomGone` | 방이 닫혔음: 나가서 스킨의 메뉴로 돌아갑니다. |
| `msg`, `msgCode`, `msgArg` | 마지막 메시지 (이미 게임 언어로 된 것), 그 고정 코드와 인수 (메시지 참고). |
| `notice` | 방에 있는 동안 도착한 메시지. 한 번 표시하고 지웁니다. |
| `nameByPeer`, `infoByPeer` | id별 플레이어: 이름, 그리고 `Online.Identity`의 정보 (`name`, `char`, `puchi`, `dan`, `npk`, `dgold`, `dtype`, `palStops`, `palBlend`). |
| `diffByPeer`, `modByPeer`, `readyByPeer` | 각 플레이어의 난이도 (0에서 4), 모드, 준비 상태. |
| `lackByPeer`, `watchByPeer` | 방의 곡이 없는 플레이어; 아직 결과 화면에 있는 플레이어. |
| `song` | 방의 곡 `{ uid, title, diff, speed, dyn }`. 호스트 역할이 곡을 고를 때까지 nil. |
| `songLevels`, `songPlus`, `songSubtitle`, `songBpm`, `iLackSong` | 이 기기의 곡: 가진 각 난이도의 레벨과 플러스 표시, 부제와 BPM; 이 기기에 없으면 `iLackSong`. |
| `resultByPeer` | 라운드 결과: `{ name, sc, gr, gd, ms, co, cl, fc, pf, ac, ab }` (점수, 량, 가, 불가, 콤보, 클리어, 풀 콤보, 전량, 정확도, 중단). |
| `resultsT`, `resultsReadyT` | 결과 화면에 머문 초; 결과가 모두 모이면 설정됩니다. |

## Online.Lobby

| 함수 | 설명 |
| --- | --- |
| `Lobby.host()  -> bool` | 방을 열고 (최대 `Lobby.MAXP` = 5명) 그 코드를 `STORAGE:WriteLobbyCode`로 저장합니다; NET이 거부하면 `net.msg`와 함께 false. |
| `Lobby.join(code)  -> bool` | 코드의 방에 참가를 시작합니다 (공백은 무시); 응답은 `drain`으로 옵니다. 코드가 비었거나, 잘못되었거나, 다른 종류의 방 코드이면 `net.msg`와 함께 false. |
| `Lobby.leave()` | 세션을 끝내고 (참가에 실패한 뒤에도) 플레이어 자리를 세이브 파일에 돌려줍니다. |
| `Lobby.drain()` | 대기 중인 모든 NET 이벤트를 처리합니다; `online`이나 `connecting`인 동안 매 프레임 호출합니다. |
| `Lobby.amController()  -> bool` | 이 플레이어가 호스트 역할: 곡을 고르고 라운드를 시작합니다. |
| `Lobby.peerIds()`, `Lobby.count()` | 순서대로의 플레이어 id; 그 수. |
| `Lobby.spotOf[id]` | 각 플레이어의 로컬 자리 (자신은 0). `NAMEPLATE:DrawPlayerNameplate`용. |
| `Lobby.myDiff()`, `Lobby.setDiff(d)`, `Lobby.stepDiff(dir)  -> bool` | 내 난이도; 설정; 곡에 있는 다음 난이도로 이동 (줄 끝에서는 false). |
| `Lobby.availDiffs()`, `Lobby.diffAvailable(d)`, `Lobby.fixDiff()` | 곡에 있는 난이도; 내 난이도를 그중 하나로 유지 (로비의 매 프레임 호출). |
| `Lobby.myReady()`, `Lobby.setReady(b)` | 내 준비 상태. |
| `Lobby.broadcastMods()` | 플레이어 0의 모드를 `CONFIG`에서 읽어 보냅니다; 스킨의 모드 선택이 닫힐 때 호출합니다. |
| `Lobby.applyModsToSlot(slot, mods)` | 한 플레이어의 모드를 `CONFIG`의 플레이어 슬롯에 놓아 `modicons` ROActivity로 그 아이콘을 그릴 수 있게 합니다. |
| `Lobby.adjustSpeed(delta)`, `Lobby.setSpeed(sp)` | 방의 곡 속도 (호스트 역할만). |
| `Lobby.startProblem()  -> "need_song", "need_players" or nil` | 라운드를 아직 시작할 수 없는 이유. 준비 상태는 필요하지 않습니다. |
| `Lobby.hostStart()  -> bool` | 라운드를 시작합니다 (호스트 역할만). |
| `Lobby.takeGo()  -> bool` | 라운드가 시작되면 한 번 true; 그다음 `launchPlay`를 호출합니다. |
| `Lobby.launchPlay()  -> true, "eject" or false` | 모든 플레이어의 곡을 마운트하고 플레이 라운드를 엽니다: true면 스킨은 `"play"`로 나갑니다; `"eject"`는 이 기기에 곡이 없어 방을 나가야 한다는 뜻; false는 `net.msg`와 함께 옵니다. |
| `Lobby.returnedFromPlay()` | 그 플레이 후 스테이지에 다시 들어왔을 때 호출합니다: 내 결과를 보내고 결과 화면을 시작합니다. |
| `Lobby.tickResults(dt)  -> bool` | 결과 화면의 매 프레임: 모든 결과가 오거나 `Lobby.RESULTS_WAIT` (20)초가 지나면 true. |
| `Lobby.standings()  -> rows` | 결과, 높은 점수 순: `{ id, name, r }`. 그 플레이어가 아직 플레이 중이면 `r`은 nil. |
| `Lobby.nextRound()` | 로비로 돌아갑니다: 호스트 역할이 다음 플레이어에게 넘어갑니다. |
| `Lobby.tickPreview(dt)`, `Lobby.stopPreview()` | 로비와 결과 화면에서 곡 미리듣기를 반복합니다; 그것을 멈춥니다. |
| `Lobby.onEvent` | `drain`이 처리하는 각 이벤트 (`"joined"`, `"left"`, `"song"`, `"ready"`, `"go"`, `"result"`, ...)마다 호출되는 선택적 함수 `(kind, peer, data)`. |

온라인 플레이는 같은 인원의 오프라인 플레이와 같아 보입니다: 다른 플레이어는 로컬 자리 2에서 5를 가상 슬롯으로 차지하고, 각자의 캐릭터와 네임플레이트로 표시되며, 게임 화면이 네트워크에서 온 데이터로 그들을 플레이시킵니다.

## 곡 선택과의 연결

호스트 역할의 플레이어는 스킨의 곡 선택에서 방의 곡을 고릅니다. 곡 선택은 곡만 골라야 합니다 (난이도 단계 없음, 오토 없음); 그 속도 키는 `CONFIG.SongSpeed`를 바꿉니다. 세 번의 호출로 연결합니다:

1. 직접 활성화하는 대신 `Lobby.openSongSelect(open)`: `open`은 곡 선택을 활성화하는 함수입니다. 먼저 방의 속도가 `CONFIG.SongSpeed`에 들어갑니다.
2. 곡이 확정되면: 곡 선택을 비활성화한 다음 `Lobby.songPicked(uid?)`. `uid`의 기본값은 `SONGMOUNT:ChosenUniqueId()`입니다.
3. 곡 없이 곡 선택을 나가면: 그것을 비활성화한 다음 `Lobby.songPickCancelled()`. 거기서 바꾼 속도를 유지하고 방의 재킷과 미리듣기를 되돌립니다.

공유 문자열 `presound_path`는 `presound`의 오디오 이름입니다: 방의 곡을 거기서 계속 재생해 둔 곡 선택은 미리듣기를 넘겨주고, 로비는 다시 시작하지 않고 그대로 재생합니다.

## 메시지

`net.msg`는 바로 표시할 수 있습니다. 메시지를 직접 쓰는 스킨은 대신 `net.msgCode`를 읽습니다:

| 코드 | 언제 |
| --- | --- |
| `open_failed`, `join_failed` | NET이 이유 없이 거부했습니다. |
| `room_open` | 방이 열렸고 그 코드가 저장되었습니다. |
| `connecting` | 참가가 시작되었습니다. |
| `no_code`, `code_invalid` | 코드가 비었거나 방 코드가 아닙니다. |
| `code_other` | 다른 종류의 방 코드입니다; `msgArg`는 그 스테이지 id입니다. |
| `chart_failed` | `launchPlay`가 채보를 마운트하지 못했습니다. |
| `net` | NET 자체에서 온 영어 텍스트 (네트워크 꺼짐, 연결 끊김, ...). |

네트워크는 플레이어가 설정에서 허용할 때까지 꺼져 있습니다. 스킨은 로비를 열기 전에 `CONFIG.AreLuaNetworkingConnectionsAllowed`를 확인해야 합니다; NET도 `net` 메시지로 거부합니다.

## Online.Protocol

모든 메시지는 하나의 NET 채널 위의 JSON 문자열입니다. 코드 안의 방 id는 어느 스테이지가 열었든 `"onlinelobby"` (`Protocol.ROOM_ID`)입니다.

| 채널 | 페이로드 | 보내는 때 |
| --- | --- | --- |
| `song` | `{"u":uid,"t":title,"d":0,"s":speed,"b":dyn}` | 호스트 역할이, 곡이나 그 속도가 바뀔 때, 그리고 새로 온 각 플레이어에게. |
| `diff`, `mod`, `ready` | `{"d":n}`, `{"r","st","ju","tz","ss","sm"}`, `{"r":bool}` | 플레이어가 그것을 바꿀 때, 그리고 새로 온 각 플레이어에게. |
| `have`, `watch` | `{"h":bool}`, `{"w":bool}` | 플레이어가 곡을 가졌는지; 결과 화면에 있는지. |
| `go` | `{}` | 라운드가 시작됩니다. |
| `result` | `{"n","sc","gr","gd","ms","co","cl","fc","pf","ac","ab"}` | 곡이 끝난 뒤 플레이어의 결과. |

## 최소한의 로비

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

스킨의 메뉴에서 `Lobby.host()`나 `Lobby.join(code)`를 호출하면 `screen`이 `"lobby"`가 됩니다; 곡은 위의 곡 선택 연결이 정합니다.
