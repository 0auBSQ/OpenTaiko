<!-- api/networking.md -->

# オンラインネットワーク <span class="badge-exp">実験的</span>

`NET` グローバルは、オンラインロビーとマルチプレイヤーステージを支えるピアツーピアプロトコル、OpenTaiko Online のクライアントです。1 人のプレイヤーがルームを作成してルームコードを受け取り、他のプレイヤーはそのコードで参加します。サーバー (ルームが移行していなければルーム作成者) がすべての通信を中継し、各プレイヤーはルームに誰がいるかの参加者リストを保持します。

Lua での使い方:

- すべてのデータは文字列として Lua の境界を越えます。ステージは構造化データを JSON として運びます。
- ネットワークはバックグラウンドスレッドで動作し、イベントをキューに積みます。ステージは毎フレーム、nil が返るまで `NET:Poll()` をループで呼んでキューを空にします。
- ネットワークは既定でオフです。「Allow LuaNetworking connections」設定がこれを有効にします。オフの間、CreateRoom は nil を返し、JoinRoom は false を返し、クライアントは "error" イベントをキューに積みます。
- ルーム id: ルーム作成者は id 1 です。参加者はそれぞれ参加順に次の id を受け取ります。`NET:SelfId()` はルーム外では 0 です。
- ホストロールは接続の所有とは別のものです。現在ロビーを進行するプレイヤー (楽曲を選び、プレイを開始する) を示します。最初は作成者が持ち、RotateHost または SetHostRole が別のプレイヤーに渡します。
- ルーム作成者が離れると、残りのプレイヤーは自動的に新しいサーバーへ移行します。誰も引き継げないときにのみルームが閉じます。

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

### ルームのライフサイクル

<div class="callout warn">
グローバル NET として利用できます。他のプレイヤーが参加時にあなたの情報を受け取れるよう、CreateRoom または JoinRoom の前に SetLocalPlayer を呼んでください。JoinRoom は非同期です。結果は後で "connected" または "error" イベントとして届きます。ルームの作成または参加は、まず現在のルームから離れます。
</div>

| メソッド | 説明 |
| --- | --- |
| `NET:SetLocalPlayer(infoJson)  -> void` | ローカルプレイヤーの情報文字列 (通常はネームプレート、キャラクターなどを含む JSON) を設定します。クライアントは参加時にこれを他のプレイヤーに送り、彼らの "joined" イベントと PeersJson に現れます。空文字列は "{}" になり、16384 文字を超える文字列はサーバーが "{}" に置き換えます。 |
| `NET:CreateRoom(stageId, payload, maxPlayers)  -> string` | 指定したステージ id と任意のペイロード文字列でルームを作成し、そのルームコードを返します。ネットワークがオフかルームの作成に失敗した場合は nil。maxPlayers は参加者リストの上限です (既定 8)。 |
| `NET:CreateRoom(stageId, payload)  -> string` | 同上。既定の上限 8 人。 |
| `NET:PeekStageId(roomCode)  -> string` | 接続せずにルームコードに保存されたステージ id を返します。コードが無効なら nil。参加画面が適切なステージへ振り分けられるようにします。 |
| `NET:JoinRoom(roomCode)  -> bool` | ルームへの参加を開始します。ネットワークがオフかコードが不正なら直ちに false を返します。それ以外の場合、結果は "connected" または "error" イベントとして届きます。 |
| `NET:Leave()  -> void` | ルームを離れ、イベントキューをクリアします。他のプレイヤーが残っている状態でルーム作成者が離れると、別のプレイヤーがルームを引き継ぎます。そうでなければ全員に対して閉じます。 |
| `NET.PortOverride  (int, settable)` | 0 より大きいとき、ルームはこの TCP ポートを使います。0 は既定のポート 41234 を選びます。通常は 0 のままにしてください。 |

### メッセージング

<div class="callout warn">
チャンネル名はステージが自由に選びます。プレイラウンドの仕組みはチャンネル名 "ps"、"ld"、"fn" を予約しており、これらは Poll() には決して届きません。
</div>

| メソッド | 説明 |
| --- | --- |
| `NET:Broadcast(channel, data)  -> void` | 指定したチャンネルでルーム内の他のすべてのプレイヤーにメッセージを送ります。 |
| `NET:SendTo(peerId, channel, data)  -> void` | 指定したチャンネルで id を指定した 1 人のプレイヤーにメッセージを送ります。-1 は Broadcast と同様に他の全員に送ります。 |
| `NET:Poll()  -> NetEvent` | 保留中の次のイベントを取り出して返します。キューが空なら nil。 |

### ルームの状態とホストロール

| メソッド | 説明 |
| --- | --- |
| `NET:SelfId()  -> int` | このプレイヤーの id (ルーム作成者は 1)。ルーム外なら 0。 |
| `NET:Connected()  -> bool` | ルーム内にいるとき true: このプレイヤーが自分の id を持ち、サーバーであるか、サーバーに接続しています。 |
| `NET:IsHost()  -> bool` | このプレイヤーがルームの接続を所有しているとき true (ルーム作成者、または移行後に引き継いだプレイヤー)。 |
| `NET:HasHostRole()  -> bool` | このプレイヤーが現在ホストロールを持っているとき true。 |
| `NET:HostRoleId()  -> int` | ホストロールを持つプレイヤーの id。 |
| `NET:PeerCount()  -> int` | 自分を含む参加者リスト内のプレイヤー数。 |
| `NET:PeersJson()  -> string` | 参加順の JSON 配列としての参加者リスト。各エントリは {id, info, isHost, hostRole} です。info はそのプレイヤーが SetLocalPlayer に渡した文字列、isHost は id 1 で true、hostRole は現在のホストロール保持者で true です。 |
| `NET:RotateHost()  -> void` | ホストロールを参加順で次のプレイヤーに渡します。サーバーでのみ動作し、他のプレイヤーからの呼び出しは無視します。 |
| `NET:SetHostRole(peerId)  -> void` | 指定したプレイヤーにホストロールを与え、ルームに通知します。サーバーでのみ、参加者リストにある id に対してのみ動作します。 |

### プレイラウンド

オンラインの楽曲中はゲームがロビーステージを一時停止し、ゲームプレイ画面が自身でやり取りを進めます。ローカルプレイヤーの進行中のスコアをブロードキャストし、リモートのスポットに通信から受け取ったデータを流し込み、ローディングと終了のバリアで待機します。ロビーがすべきことは、誰がどのスポットでプレイするかを宣言し、ラウンドを開いて閉じることだけです。

<div class="callout warn">
スポットはゲームプレイ画面のプレイヤー位置です。スポット 0 は常にローカルプレイヤー、スポット 1 以降はリモートプレイヤーです。SetPlaySpots で対応付けを設定し、プレイに入る直前に BeginPlaySync を、ロビーが制御を取り戻したら EndPlaySync を呼びます。バリア、スコア送信、確率のメソッドはゲームプレイ画面が呼びます。同じオブジェクトがこれらを公開しているため、ここに列挙しています。
</div>

| メソッド | 説明 |
| --- | --- |
| `NET:SetPlaySpots(json)  -> void` | 次のプレイのスポットからプレイヤーへの対応付けを、インデックス 0 が自分である id の JSON 配列 (例: "[1,3,2]") から設定します。解析できない文字列は対応付けをクリアします。 |
| `NET:PlaySpotCount()  -> int` | 現在のスポット対応付けのエントリ数 (設定されていなければ 0)。 |
| `NET:BeginPlaySync(selfName)  -> void` | プレイラウンドを開きます: 以前のスコアをクリアし、進行中スコアのメッセージ用にローカルプレイヤーの表示名を記録します。 |
| `NET:EndPlaySync()  -> void` | プレイラウンドを閉じ、バリアとスポットごとの結果をクリアします。 |
| `NET:IsRemoteSpot(spot)  -> bool` | 開いているプレイラウンド中に、スポットがリモートプレイヤーに対応しているとき true。 |
| `NET:IsSpotActive(spot)  -> bool` | スポットに対応するプレイヤーがまだ参加者リストにいるとき true (スポット 0 は常に true)。false はプレイヤーがプレイ中に切断したことを意味します。 |
| `NET:GetSpotPlayJson(spot)  -> string` | リモートスポットが送信した最新の進行中スコアの JSON。なければ空文字列。キー: n (名前)、s (スコア)、g (ゲージ)、a (精度)、gr (良)、gd (可)、ms (不可)、co (コンボ)。 |
| `NET:GetSelfPlayScore()  -> string` | このクライアントが最後にブロードキャストした進行中スコアの JSON。 |
| `NET:LivePlayScoresJson()  -> string` | すべてのプレイヤーの最後の進行中スコアの JSON を、{id, d} の JSON 配列として。d はスコアの JSON 文字列です。このクライアントが少なくとも 1 回ブロードキャストすると、配列に自分が含まれます。 |
| `NET:GetSpotResultJson(spot)  -> string` | リモートスポットが楽曲終了時に報告した最終結果の JSON。なければ空文字列。キー: cl (クリア)、fc (フルコンボ)、pf (全良)、mx (虹ゲージ) は真偽値、gr、gd、ms、rl (連打)、bl (風船)、ad (アドリブ)、hc (最大コンボ)、sc (スコア) は整数。 |
| `NET:GetSpotClearLevel(spot)  -> int` | リモートスポットの最終結果によるクリアレベル: 2 虹、1 クリア、0 失敗、結果が届いていなければ -1。 |
| `NET:GetSpotJudge(spot, key)  -> int` | リモートスポットの最終結果の整数フィールド 1 つをキーで取得します (GetSpotResultJson を参照)。結果が届いていなければ -1。 |
| `NET:GetSpotBadOdds(spot)  -> int` | ゲームがリモートスポットの次の自動ヒットを不可と判定する千分率の確率 (0-1000)。ブロードキャストされた判定数から導かれます。 |
| `NET:GetSpotGoodOdds(spot)  -> int` | ゲームがリモートスポットの次の自動ヒットを良と判定する千分率の確率 (0-1000)。 |
| `NET:PushPlayScore(json)  -> void` | ローカルプレイヤーの進行中スコアの JSON をブロードキャストします。ゲームプレイ画面が毎秒数回これを呼びます。 |
| `NET:ReportLoaded()  -> void` | このクライアントが楽曲の読み込みを完了したことを 1 回報告します。 |
| `NET:LoadBarrierReady(timeoutMs)  -> bool` | 参加者リストの全員が読み込み完了を報告したか、このクライアントの報告からタイムアウトが経過すると true (サーバーは報告のなかったプレイヤーを切断します)。 |
| `NET:ReportFinished(resultJson)  -> void` | このクライアントが楽曲を終えたことを、最終結果の JSON とともに 1 回報告します。 |
| `NET:FinishBarrierReady(timeoutMs)  -> bool` | 参加者リストの全員が終了を報告したか、タイムアウトが経過すると true。 |
| `NET:BarrierReset()  -> void` | ローディングと終了のバリア、およびスポットごとの結果をクリアします。 |

## NetEvent

NET:Poll() が返す、1 つのネットワークイベントを表すオブジェクトです。

<div class="callout warn">
フィールドを直接読み取ります。メソッドはありません。イベントに関連するプレイヤーがいない場合、Peer は 0 です。
</div>

| メソッド | 説明 |
| --- | --- |
| `event.Type  -> string` | イベントの種類。以下に挙げる値のいずれか。 |
| `event.Peer  -> int` | 関連するプレイヤー id: "connected" では自分の id、"message" では送信者、"joined"、"left"、"hostrole" では影響を受けたプレイヤー。それ以外は 0。 |
| `event.Channel  -> string` | "message" イベントのチャンネル。他の種類では空。 |
| `event.Data  -> string` | イベントのペイロード。以下を参照。 |

| 種類 | 意味 |
| --- | --- |
| `connected` | ルームに参加しました。Data は JSON オブジェクト {selfId, stageId, payload, hostRoleId} です。ルーム作成者は決して受け取りません。すでにルームにいるプレイヤーは PeersJson に現れ、"joined" イベントは発生しません。 |
| `joined` | あなたの後にプレイヤーが参加しました。Peer はその id、Data はその情報文字列です。 |
| `left` | プレイヤーが離れたか、サーバーが切断しました。Peer はその id です。 |
| `message` | 別のプレイヤーからのメッセージ。Peer は送信者、Channel と Data がメッセージです。 |
| `hostrole` | ホストロールが移りました。Peer は新しい保持者の id です。 |
| `roomclosed` | ルームが閉じたか、接続の回復に失敗しました。 |
| `error` | プレイヤーに表示するステータスメッセージ。Data はそのテキストです。クライアントは、ネットワークがオフのとき、作成または参加に失敗したとき、ルームが満員のとき、およびホスト移行中の進行通知としてこれを発生させます。 |
