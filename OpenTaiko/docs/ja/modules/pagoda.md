<!-- modules/pagoda.md -->

# Pagoda

未知の塔は段位の階を登っていくモードです。各階はプールから 3 曲 (ときには 4 曲目の紫の曲も) を抽選し、階が上がるにつれて厳しくなる 3 つの試験があります。`Pagoda` モジュールはルール、データ、セーブキーを持ち、何も描画しません。スキンはその上に独自の塔の画面を描き、進行状況はセーブファイルにあるため、すべてのスキンが 1 つの進行状況を共有します。Open-World Memories の `pagoda` ステージはそのようなクライアントの 1 つです。

1 階から 10 階は 道一 から 道十、11 階から 20 階は 扉一 から 扉十、21 階からは屋根の 穹天1、穹天2 と続きます。名前は段位の称号で、どの言語でも同じです。

## モジュール一覧

| モジュール | 内容 |
| --- | --- |
| `Pagoda.Levels` | 階の番号から導かれるすべて: 楽曲のプール、名前、段位プレートの枠と色、試験の基準値、紫の曲の確率。純粋な参照です。 |
| `Pagoda.Floor` | 階の曲 (抽選結果) とその段位。抽選結果をプレートと試験とともに `DANBUILDER` に渡します。 |
| `Pagoda.Rewards` | クリアで得られるもの: ネームプレート、コイン、マイルームの家具、金庫の鍵。それらを保存し、`modal` ROActivity でポップアップを表示します。 |
| `Pagoda.Progress` | 永続的なセーブデータ (下記)。 |
| `Pagoda.Songs` | 塔が曲を選ぶ楽曲リストと、サウンドトラックがプールのすべての項目を満たしているかの確認。 |
| `Pagoda.Run` | 任意: チャレンジと練習の流れを、画面なしで扱います。 |
| `Pagoda.Texts` | モジュール自身の文字列 (`lang/<code>/pagoda.json`)。 |

## Pagoda.Levels

| 関数 | 説明 |
| --- | --- |
| `Levels.get_pool(level)  -> pool` | 階のプール。`{ id, diff }` (曲の固有 id と難易度 0 から 4) のリスト `{ blue, green, red, purple }` です。専用の項目がない階は、それより下で最も近いものを使います。 |
| `Levels.load_pools()`, `Levels.clear_pools()` | `pools.json` を読み込みます (一度だけ)。楽曲の再列挙の後で、読み込んだ内容を忘れます。 |
| `Levels.level_name(level)  -> string` | 階の名前。 |
| `Levels.level_tick(level)`, `Levels.level_tick_color(level)` | 段位プレートの枠 (0 から 4) と色 `{ r, g, b }`。 |
| `Levels.exam1_gauge(level)  -> red, gold` | ゲージの試験。 |
| `Levels.exam2(level)  -> type, red, gold, lessThan` | 精度の試験 (1 階から 20 階)、または可の数の試験 (21 階から)。 |
| `Levels.exam3_base(level)  -> red, gold` | 紫の曲による緩和を適用する前の、不可の数の試験。 |
| `Levels.purple_prob(level)  -> number` | 4 曲目に紫の曲が入る確率 (20 階には必ず入ります)。 |
| `Levels.pick_entry(entries, used)`, `Levels.shuffle(t)` | 抽選が使うヘルパー。 |

モジュールの隣にある `pools.json` は、各階の番号をそのプールに対応付けます。

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

プールは OpenTaiko のサウンドトラックの曲を指定します。`Songs.count_missing_songs()` は、インストールされた楽曲に欠けている項目 (曲、またはその難易度) をログに出力して数えるため、画面でプレイヤーに警告できます。

## Pagoda.Floor

| 関数 | 説明 |
| --- | --- |
| `Floor.rollFloor(level, avoid?, songList?)  -> roll` | 青、緑、赤の曲を 1 曲ずつシャッフルした順で選び、その階の確率で紫の曲を加えます。同じ曲は二度選ばれません。`avoid` は、プールに他の曲がある限り選ばない曲 id の集合です。抽選結果は `{ songs = { { color, node, entry }, ... }, purple = { node, entry } or nil, speed }` です。 |
| `Floor.rerollFloor(level, roll, keepMystery, songList?)  -> roll` | プールが許す範囲で別の曲を選んで抽選し直します。`keepMystery` は紫の曲を残します。 |
| `Floor.exams(level, hadPurple)  -> table` | 3 つの試験を `{ type, red, gold, lessThan }` として返します。 |
| `Floor.buildDan(level, roll?, opts?)  -> bool` | その階のために `DANBUILDER` を設定します: タイトル、プレート、ジャンル (`opts.genre`、`opts.genreColor`)、曲、試験。曲を 1 曲も追加できなかった場合は false。それ以外の場合は `DANBUILDER:Mount()` でマウントしてプレイします。 |
| `Floor.speed(level)  -> number` | その階の曲の速度 (`CONFIG.SONGSPEED` の値)。穹天18 より下は x1 で、それ以降は上がっていきます。 |

## Pagoda.Run

チャレンジは開放されている開始階のいずれかから始まり、クリアするごとに 1 階登り、最初の敗北で終わります。練習は、チャレンジで到達した最高の階 (最低でも 11 階) までの任意の階を遊べ、何も保存しません。

| 関数 | 説明 |
| --- | --- |
| `Run.startOptions()  -> table` | このセーブで開放されている開始階: 1、6、11 は常に、16 は 15 階をクリアした後、21 は 20 階をクリアした後。 |
| `Run.new(start, songList?)  -> run` | その階から始まるチャレンジ。フィールド: `level` (現在の階)、`tower` (クリア済みとして表示される最高の階)、`roll`、`over`。 |
| `run:rollFloor(avoid?)`, `run:reroll(keepMystery)  -> roll` | 現在の階の曲。プレイの前に表示します。 |
| `run:enter()` | チャレンジがその階に到達したことを記録します。以後、練習でその階を選べます。 |
| `run:buildDan(opts?)  -> bool` | 現在の階のために設定された `DANBUILDER`。マウントしてプレイします。 |
| `run:clear()  -> result` | クリアの後: 報酬を付与して保存し、最高の階を更新し、登ります。`{ level, rewards, shrouded }` を返します。`rewards` は `Rewards.grant` が返すリスト、`shrouded` はその階が塔より上にあったかどうかです。 |
| `run:fail()` | 敗北の後: 階を記録し、プレイを数え、チャレンジを終えます。 |
| `Run.practiceLevels()  -> table`, `Run.practiceDan(level, opts?)  -> bool` | 練習で選べる階。新しい抽選で練習プレイのために設定された `DANBUILDER`。 |

## Pagoda.Rewards

| 関数 | 説明 |
| --- | --- |
| `Rewards.grant(level)  -> table` | プレイヤー 1 のセーブにクリアの報酬を付与して保存し、表示するポップアップを順番に返します: `{ kind = "nameplate", id }`、`{ kind = "furniture", id }`、`{ kind = "key", key, count }`、`{ kind = "coins", amount }`。 |
| `Rewards.KEY_PICTURE` | 鍵のポップアップが表示する共有テクスチャの接頭辞 (`pagoda_reward_key_<id>`)。モジュールは鍵の画像を含みません。鍵のポップアップを表示する前に、スキンが鍵ごとに 1 枚共有します。 |
| `Rewards.show(r)  -> bool` | `modal` ROActivity で 1 つの報酬のポップアップを開始します。 |
| `Rewards.update()  -> bool`, `Rewards.draw()`, `Rewards.stop()` | ポップアップを動かし (再生中は true。入力は自分で読みます)、描画し、破棄します。 |
| `Rewards.poolOf(level)`, `Rewards.nameplateOf(level)` | 階の報酬プールと、その初回クリアで得られるネームプレート。 |

階の初回クリアでは、その階のネームプレート (id 381 から 397、`Rewards.NAMEPLATES` に記載された階) が得られます。次に、コイン (範囲内の額、確率 50%) と階のプールの各項目が、それぞれ独自の確率で抽選されます (`Rewards.POOLS`、`Rewards.ROOF_POOLS`)。そのため 1 回のクリアで複数のものが得られることがあります。秘密の金庫が閉じている間、金庫の鍵はコインになり、1 回のクリアのコインはすべて 1 つのポップアップにまとめられます。家具のポップアップは、[マイルーム](my-room.md)が提供する共有テクスチャ `myroom_thumb_<id>` を表示します。

## セーブキー

すべてはプレイヤー 1 のセーブ (`GetSaveFile(0)`) に保存されます。

| キー | 種類 | 内容 |
| --- | --- | --- |
| `pagoda_highest_level` | カウンター | チャレンジでクリアした最高の階。 |
| `pagoda_highest_reached` | カウンター | チャレンジで到達した最高の階。練習はここまでの階を選べます。 |
| `pagoda_plays` | カウンター | 遊んだチャレンジの数 (敗北時に数えます)。 |
| `pagoda_lost_<level>` | トリガー | その階でチャレンジに敗れた。 |
| `pagoda_story_first_seen` | トリガー | 冒頭のストーリーが再生済み (古いセーブでは代わりにカウンター `pagoda_story_seen` を持つことがあります)。 |
| `pagoda_held_mods`, `pagoda_held_mods_lo`, `pagoda_held_mods_hi` | トリガー、カウンター | 塔のプレイ中に保持されるプレイヤー 1 の MOD フラグ。32 ビットずつ 2 つに分けて保存します (`Progress.hold_mods(flags)`、`held_mods()`、`drop_held_mods()`)。 |
| `pagoda_structure_version` | カウンター | 古いセーブの番号を一度だけ振り直す処理 (`Progress.migrate_save()`) が実行済み。 |
| `.myroom_<id>`, `.myroom_held_<id>` | カウンター | 獲得した家具: マイルームに渡すものと、所持しているもの。 |
| `.vault_key_count_<key>` | カウンター | 獲得した金庫の鍵。[Vault.Keys](vault.md) を通じて扱います。 |

ネームプレートとコインはセーブ自身のメソッド (`UnlockNameplate`、`EarnCoins`) を通じて扱います。

```lua
local Run     = require("Pagoda.Run")
local Rewards = require("Pagoda.Rewards")
local run = Run.new(Run.startOptions()[1])

run:rollFloor()                  -- show run.roll's songs
run:enter()
if run:buildDan() and DANBUILDER:Mount() then return Exit("play") end

-- back from the play
local r = run:clear()            -- or run:fail()
for id in pairs(Rewards.KEYS) do SHARED:SetSharedTexture(Rewards.KEY_PICTURE .. id, "Textures/key_" .. id .. ".png") end
for _, reward in ipairs(r.rewards) do queue[#queue + 1] = reward end   -- then Rewards.show / update / draw
```
