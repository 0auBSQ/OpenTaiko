<!-- modules/online.md -->

# Online

オンラインロビーのロジックです: コードで開いて参加するルーム、プレイヤー一覧、各プレイヤーの難易度・モッド・準備状態、ルームの曲、ラウンドの開始、リザルト、そして次のラウンド。`Online` モジュールは何も描画しません。スキンはその上に独自のロビー画面を描き、独自の選曲画面をつなぎます。どのスキンのロビーも同じメッセージを話すので、異なるスキンのプレイヤーが 1 つのルームで出会えます。Open-World Memories の `onlinelobby` ステージはそのようなクライアントの 1 つです。これらのモジュールは実験的な [NET グローバル](../api/networking.md) の上で動きます。

## モジュール一覧

| モジュール | 内容 |
| --- | --- |
| `Online.Lobby` | セッション: その状態、画面が行うすべての操作、選曲画面との接続、イベントの処理。 |
| `Online.Protocol` | メッセージ: チャンネル名と各ペイロードの JSON。 |
| `Online.Identity` | プレイヤーが自分について送るもの (名前、キャラクター、ぷちキャラ、ネームプレート、段位プレート、パレット)。 |
| `Online.Media` | このマシン上のルームの曲: そのレベル、共有テクスチャ `preimage` のジャケット、共有サウンド `presound` のプレビュー。 |
| `Online.Texts` | モジュール自身の文字列 (`lang/<code>/online.json`)。 |

スキンに必要なのは `Online.Lobby` だけで、ほかのモジュールから使うものはそこから再エクスポートされます。セッションは Lua ステートごとに 1 つで、モジュールはその状態を `Lobby.net` に保持します。

## セッションの状態

スキンは毎フレーム `Lobby.net` を読んで画面を描きます:

| フィールド | 説明 |
| --- | --- |
| `online`, `connecting` | ルームにいる; 参加の応答を待っている。 |
| `isHost`, `code` | このマシンがルームを動かしている (作成した); ルームコード。 |
| `roomGone` | ルームが閉じた: 退出してスキンのメニューに戻る。 |
| `msg`, `msgCode`, `msgArg` | 最後のメッセージ (ゲームの言語に翻訳済み)、その固定コードと引数 (「メッセージ」を参照)。 |
| `notice` | ルームにいる間に届いたメッセージ。一度表示してから消します。 |
| `nameByPeer`, `infoByPeer` | id ごとのプレイヤー: 名前と `Online.Identity` の情報 (`name`, `char`, `puchi`, `dan`, `npk`, `dgold`, `dtype`, `palStops`, `palBlend`)。 |
| `diffByPeer`, `modByPeer`, `readyByPeer` | 各プレイヤーの難易度 (0 から 4)、モッド、準備状態。 |
| `lackByPeer`, `watchByPeer` | ルームの曲を持っていないプレイヤー; まだリザルトにいるプレイヤー。 |
| `song` | ルームの曲 `{ uid, title, diff, speed, dyn }`。ホスト役が選ぶまでは nil。 |
| `songLevels`, `songPlus`, `songSubtitle`, `songBpm`, `iLackSong` | このマシン上の曲: 持っている各難易度のレベルとプラス記号、サブタイトルと BPM; このマシンにない場合は `iLackSong`。 |
| `resultByPeer` | ラウンドのリザルト: `{ name, sc, gr, gd, ms, co, cl, fc, pf, ac, ab }` (スコア、良、可、不可、コンボ、クリア、フルコンボ、全良、精度、中断)。 |
| `resultsT`, `resultsReadyT` | リザルトにいる秒数; すべて揃った時点で設定されます。 |

## Online.Lobby

| 関数 | 説明 |
| --- | --- |
| `Lobby.host()  -> bool` | ルームを開き (最大 `Lobby.MAXP` = 5 人)、そのコードを `STORAGE:WriteLobbyCode` で保存します; NET が拒否したときは `net.msg` 付きで false。 |
| `Lobby.join(code)  -> bool` | コードのルームへの参加を始めます (空白は無視); 応答は `drain` で届きます。コードが空、無効、または別の種類のルームのものなら `net.msg` 付きで false。 |
| `Lobby.leave()` | セッションを終了し (参加に失敗した後も)、プレイヤーの席をセーブファイルに戻します。 |
| `Lobby.drain()` | 待機中の NET イベントをすべて処理します; `online` か `connecting` の間は毎フレーム呼びます。 |
| `Lobby.amController()  -> bool` | このプレイヤーがホスト役: 曲を選び、ラウンドを開始します。 |
| `Lobby.peerIds()`, `Lobby.count()` | プレイヤーの id を順に; その人数。 |
| `Lobby.spotOf[id]` | 各プレイヤーのローカルの席 (自分は 0)。`NAMEPLATE:DrawPlayerNameplate` 用。 |
| `Lobby.myDiff()`, `Lobby.setDiff(d)`, `Lobby.stepDiff(dir)  -> bool` | 自分の難易度; その設定; 曲にある次の難易度への移動 (列の端では false)。 |
| `Lobby.availDiffs()`, `Lobby.diffAvailable(d)`, `Lobby.fixDiff()` | 曲にある難易度; 自分の難易度をそのいずれかに保ちます (ロビーの毎フレーム呼びます)。 |
| `Lobby.myReady()`, `Lobby.setReady(b)` | 自分の準備状態。 |
| `Lobby.broadcastMods()` | プレイヤー 0 のモッドを `CONFIG` から読んで送ります; スキンのモッド選択が閉じたときに呼びます。 |
| `Lobby.applyModsToSlot(slot, mods)` | あるプレイヤーのモッドを `CONFIG` のプレイヤー枠に置き、`modicons` ROActivity でそのアイコンを描けるようにします。 |
| `Lobby.adjustSpeed(delta)`, `Lobby.setSpeed(sp)` | ルームの曲の速度 (ホスト役のみ)。 |
| `Lobby.startProblem()  -> "need_song", "need_players" or nil` | ラウンドをまだ開始できない理由。準備状態は必須ではありません。 |
| `Lobby.hostStart()  -> bool` | ラウンドを開始します (ホスト役のみ)。 |
| `Lobby.takeGo()  -> bool` | ラウンドが始まったときに一度だけ true; そのあと `launchPlay` を呼びます。 |
| `Lobby.launchPlay()  -> true, "eject" or false` | 全プレイヤーの曲をマウントしてプレイラウンドを開きます: true ならスキンは `"play"` へ出ます; `"eject"` はこのマシンが曲を持っておらずルームを出る必要があるという意味; false は `net.msg` 付き。 |
| `Lobby.returnedFromPlay()` | そのプレイの後にステージへ戻ったときに呼びます: 自分のリザルトを送り、リザルトを開始します。 |
| `Lobby.tickResults(dt)  -> bool` | リザルトの毎フレーム: すべてのリザルトが届くか `Lobby.RESULTS_WAIT` (20) 秒が過ぎると true。 |
| `Lobby.standings()  -> rows` | リザルト、スコアの高い順: `{ id, name, r }`。そのプレイヤーがまだプレイ中なら `r` は nil。 |
| `Lobby.nextRound()` | ロビーに戻ります: ホスト役が次のプレイヤーに移ります。 |
| `Lobby.tickPreview(dt)`, `Lobby.stopPreview()` | ロビーとリザルトの画面で曲のプレビューをループします; それを止めます。 |
| `Lobby.onEvent` | `drain` が処理する各イベント (`"joined"`, `"left"`, `"song"`, `"ready"`, `"go"`, `"result"`, ...) で呼ばれる任意の関数 `(kind, peer, data)`。 |

オンラインプレイは同じ人数のオフラインプレイと同じ見た目です: ほかのプレイヤーはローカルの席 2 から 5 を仮想スロットとして使い、それぞれのキャラクターとネームプレートで表示され、ゲーム画面がネットワークからのデータで彼らをプレイさせます。

## 選曲画面との接続

ホスト役のプレイヤーは、スキンの選曲画面でルームの曲を選びます。選曲画面は曲だけを選ぶ必要があります (難易度の選択なし、オートなし); その速度キーは `CONFIG.SongSpeed` を変えます。3 つの呼び出しでつなぎます:

1. 直接アクティブにする代わりに `Lobby.openSongSelect(open)`: `open` は選曲画面をアクティブにする関数です。先にルームの速度が `CONFIG.SongSpeed` に入ります。
2. 曲が決定されたとき: 選曲画面を非アクティブにしてから `Lobby.songPicked(uid?)`。`uid` の既定値は `SONGMOUNT:ChosenUniqueId()` です。
3. 曲を選ばずに選曲画面を抜けたとき: それを非アクティブにしてから `Lobby.songPickCancelled()`。そこで変えた速度を保ち、ルームのジャケットとプレビューを戻します。

共有文字列 `presound_path` は `presound` の音声の名前です: ルームの曲をそこで鳴らしたままにした選曲画面はプレビューを引き渡し、ロビーは再開せずにそのまま鳴らし続けます。

## メッセージ

`net.msg` はそのまま表示できます。メッセージを自分で書くスキンは、代わりに `net.msgCode` を読みます:

| コード | いつ |
| --- | --- |
| `open_failed`, `join_failed` | NET が理由を言わずに拒否した。 |
| `room_open` | ルームが開き、そのコードが保存された。 |
| `connecting` | 参加が始まった。 |
| `no_code`, `code_invalid` | コードが空、またはルームコードではない。 |
| `code_other` | コードが別の種類のルームのもの; `msgArg` はそのステージ id。 |
| `chart_failed` | `launchPlay` が譜面をマウントできなかった。 |
| `net` | NET 自身からの英語のテキスト (ネットワークがオフ、接続が切れた、...)。 |

ネットワークはプレイヤーが設定で許可するまでオフです。スキンはロビーを開く前に `CONFIG.AreLuaNetworkingConnectionsAllowed` を確認するべきです; NET も `net` メッセージで拒否します。

## Online.Protocol

すべてのメッセージは 1 つの NET チャンネル上の JSON 文字列です。コード内のルーム id は、どのステージが開いたかに関係なく `"onlinelobby"` (`Protocol.ROOM_ID`) です。

| チャンネル | ペイロード | 送られるとき |
| --- | --- | --- |
| `song` | `{"u":uid,"t":title,"d":0,"s":speed,"b":dyn}` | ホスト役から、曲かその速度が変わったとき、および新しく来た各プレイヤーへ。 |
| `diff`, `mod`, `ready` | `{"d":n}`, `{"r","st","ju","tz","ss","sm"}`, `{"r":bool}` | プレイヤーがそれらを変えたとき、および新しく来た各プレイヤーへ。 |
| `have`, `watch` | `{"h":bool}`, `{"w":bool}` | プレイヤーが曲を持っているか; リザルトにいるか。 |
| `go` | `{}` | ラウンドが始まる。 |
| `result` | `{"n","sc","gr","gd","ms","co","cl","fc","pf","ac","ab"}` | 曲の後のプレイヤーのリザルト。 |

## 最小限のロビー

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

スキンのメニューからの `Lobby.host()` または `Lobby.join(code)` で `screen` を `"lobby"` にします; 曲は上記の選曲画面との接続で設定されます。
