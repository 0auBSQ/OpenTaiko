<!-- modules/vault.md -->

# Vault

秘密の金庫には 3 つの宝箱があり、プレイヤーが他の場所 (未知の塔のクリア、コインショップ) で手に入れた鍵で開けます。`Vault` モジュールは鍵、宝箱の報酬プール、セーブキーを持ち、何も描画しません。スキンはその上に独自の金庫の画面を描き、状態はセーブファイルにあるため、すべてのスキンが 1 つの金庫を共有します。Open-World Memories の `secret_vault_rw` ステージはそのようなクライアントの 1 つです。

## モジュール一覧

| モジュール | 内容 |
| --- | --- |
| `Vault.Keys` | 3 種類の鍵: その数と名前、そして金庫が開いているかどうか。 |
| `Vault.Chests` | 3 つの宝箱、その重み付きの報酬プール、報酬のポップアップ。 |
| `Vault.Texts` | モジュール自身の文字列 (`lang/<code>/vault.json`)。 |

## Vault.Keys

鍵は番号か id で指定します: 1 `"simple"`、2 `"gold"`、3 `"optk"`。それぞれ同じ番号の宝箱を開けます。

| 関数 | 説明 |
| --- | --- |
| `Keys.count(key)  -> number` | プレイヤー 1 が持っているその鍵の数。 |
| `Keys.set(key, n)`, `Keys.add(key, n?)` | 数を設定します。`n` (既定は 1) を加えます。 |
| `Keys.name(key)  -> string` | ゲームの言語での鍵の名前。 |
| `Keys.vaultOpen()  -> bool` | 金庫が開いているかどうか: スキン独自の金庫への入り口が設定するトリガー `.vault_opened`。閉じている間、他のモジュールは鍵の代わりにコインを与えます。 |
| `Keys.index(key)  -> number` | 鍵の番号。 |

## Vault.Chests

宝箱は 1 が木、2 が金、3 がプラチナです。開けるとその鍵を 1 つ使い、重み付きのプールから報酬を 1 つ抽選します: 何もなし (鍵が折れる)、コイン、次の宝箱の鍵、プレイヤーが持っていないネームプレート (宝箱ごとに id 361 から 368、369 から 375、376 から 380)、マイルームの家具 (すでに持っている家具は確率の 5 分の 1 を残します)、またはサウンドトラックの `Secret Vault` フォルダのまだ獲得していない曲で、曲は最高の譜面レベルによって宝箱に振り分けられます。

| 関数 | 説明 |
| --- | --- |
| `Chests.enter()  -> bool` | 訪問ごとに一度、他のどの処理よりも先に呼びます。初回訪問の無料の simple 鍵を与え (そのとき true を返します)、曲のプールを作ります。 |
| `Chests.roll(i)  -> reward` | 鍵を 1 つ使って宝箱 `i` を開け (先に `keyCount` を確認してください)、報酬をただちに保存して返します。 |
| `Chests.showReward(reward)  -> bool` | `modal` ROActivity で報酬のポップアップを開始します。鍵が折れた場合と鍵の報酬にはポップアップがないため false。 |
| `Chests.keyCount(i)  -> number` | 宝箱 `i` 用に持っている鍵の数。 |
| `Chests.name(i)  -> string` | ゲームの言語での宝箱の名前。 |
| `Chests.stock(i)  -> left, total` | 宝箱でまだ獲得できるもの (曲、ネームプレート、家具) の残りと、その合計。 |
| `Chests.chestPool(i)  -> table` | このセーブでの現在の宝箱の重み付きプール: `{ w, type, ... }` の要素。 |
| `Chests.songPool(i)`, `Chests.maxChartLevel(node)` | 宝箱の曲。曲の最高の譜面レベル。 |

報酬は `{ type, openedChest, ... }` で、`type` は `"snap"`、`"coins"` (`amount`)、`"key"` (`target`、宝箱の番号)、`"nameplate"` (`id`、`info`)、`"furniture"` (`id`、`rarity`)、`"song"` (`node`) のいずれかです。ポップアップは `modal` ROActivity と同じように動作します。`Pagoda.Rewards.update` と `draw` のように、`IsActive` の間その `Update` と `Draw` を呼び出してください。家具のポップアップは、[マイルーム](my-room.md)が提供する共有テクスチャ `myroom_thumb_<id>` を表示します。

## セーブキー

すべてはプレイヤー 1 のセーブ (`GetSaveFile(0)`) に保存されます。

| キー | 種類 | 内容 |
| --- | --- | --- |
| `.vault_key_count_simple`, `.vault_key_count_gold`, `.vault_key_count_optk` | カウンター | 持っている鍵。 |
| `.vault_opened` | トリガー | 金庫が開いている (スキンが設定)。 |
| `.vault_first_key_given` | トリガー | 初回訪問の鍵を与えた。 |
| `.vault_song_unlocked_<unique id>` | トリガー | その `Secret Vault` の曲を獲得した。 |
| `.myroom_<id>`, `.myroom_held_<id>` | カウンター | 獲得した家具: マイルームに渡すものと、所持しているもの。 |

ネームプレートとコインはセーブ自身のメソッド (`UnlockNameplate`、`EarnCoins`) を通じて扱います。

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
