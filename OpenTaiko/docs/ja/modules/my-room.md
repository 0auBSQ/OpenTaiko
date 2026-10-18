<!-- modules/my-room.md -->

# マイルーム

マイルームはグローバルのステージ、`Global/Modules/Stages/` の `my_room` です。プレイヤーの部屋で、プレイヤーはその中を歩き回り、未知の塔、金庫、コインショップで得たもので家具を置き、オンラインの訪問者に公開します。どのスキンからも開け、データはセーブファイルにあるため、どのスキンでも同じ部屋が表示されます。

## 入室と退室

共有文字列 `my_room_return` に、マイルームが戻る先のステージを設定してから、`my_room` へ移動します。

```lua
SHARED:SetSharedString("my_room_return", "my_menu")               -- back to my_menu
-- or, with the transition to play on the way back:
SHARED:SetSharedString("my_room_return", "my_menu|my_transition")
return Exit("stage", "my_room")
```

マイルームは入るときにこの文字列を読み、クリアします。空の場合、戻り先はタイトル画面 (`_title`) です。

共有文字列 `my_room_place` は、部屋から出て戻ってくる間、誰の部屋で、プレイヤーがどこに立っていたかを覚えています。プレースホルダー・マトリックス (訪れるステージを一覧する家具) とポッド (別のステージを開く家具) は、どちらも部屋の外へ通じています。タイトル画面がこれをクリアする (`""` に設定する) と、次の訪問はドアから始まります。

Open-World Memories は、古い `Exit("stage", "myroom")` の呼び出しとエンジンの `"heya"` の退出のために、転送用のステージ `myroom` を残しています。これは正式リリースで削除されるため、新しいコードは `my_room` に移動してください。

## データ

| キー | 種類 | 内容 |
| --- | --- | --- |
| `my_room` | セーブ文字列 | 部屋そのもの。JSON `{ v, room, jukebox }` 形式です (`sf:GetGlobalString("my_room")`)。セーブ自身のスロットにだけ書き込まれ、オンラインの訪問者や空のスロットには書き込まれません。 |
| `.myroom_<id>` | カウンター | 別のステージが与える家具。マイルームは次の訪問時にこれを一度だけ部屋の在庫に加え、すでに受け取ったものを台帳に記録します。 |
| `.myroom_held_<id>` | カウンター | セーブがその家具を所持している数。マイルームが最新に保ち、家具を与えるステージもその場で増やします。 |
| `.myroom_iw`, `.myroom_ih`, `.myroom_items` | カウンター | 部屋の幅と高さ、置かれた家具の数。セーブの一覧に表示するためにマイルームが書き込みます (読み取りだけにしてください)。 |
| `myroom_thumb_<id>` | 共有テクスチャ | ショップや報酬の家具の画像。マイルームの開始時に作られ、購入と報酬のポップアップが表示します。 |

自分のステージから家具を与えるには、[Pagoda.Rewards](pagoda.md) と [Vault.Chests](vault.md) のように、両方のカウンターに 1 を加えます。`my_room` の文字列が未設定の間は、一時的な移行処理が Open-World Memories の古いストアから部屋を読み込みます。これは正式リリースで削除されます。

## 土台となるもの

- メニューとダイアログは [UI.Pick](ui.md) を通じてスキンの UI ライブラリを使います。ライブラリは最初の使用時に選ばれ (Open-World Memories では PopUI、何も指定しないスキンでは SimpleUI)、コイン表示はそのライブラリのコインウィジェットです。
- 家具の名前 (`lang/<code>/items.json`、家具 id がキー) を含む文字列は、自身の `lang/` フォルダに 9 言語で置かれています。
- 家具のカタログは、ステージのフォルダの `data/furniture.json` です。
- プレースホルダー・マトリックスは、`Global/Modules/Stages/` にあるデバッグステージだけを一覧します。これらのデバッグステージと、テレビが使う `video_player` ROActivity もグローバルのモジュールです。
- ポッドは、`Global/Modules/Stages/` にステージ `mysterypod` が存在するとき、またはスキン独自の `mysterypod` ステージが `onStart` で共有文字列 `mysterypod_installed` を `"1"` に設定したときに、そのステージを開きます。
- オンラインの訪問では、移動前と同じく、ルーム id `"myroom"` とそのペイロードのバージョン 2 を使います。
