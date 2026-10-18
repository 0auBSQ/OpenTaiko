<!-- modules/shop.md -->

# Shop

コインショップのロジックです: カタログ、その日の在庫、入れ替え、価格、購入、そして購入で得られるもの。`Shop` モジュールは何も描画しません。スキンはその上に独自のショップを描き、独自のトランジションを選びます。どのスキンも同じルールで同じアイテムを売り、購入はセーブファイルに記録されます。Open-World Memories の `coin_shop` ステージ (雑誌とその店主) はそのようなクライアントの 1 つです。

## モジュール一覧

| モジュール | 内容 |
| --- | --- |
| `Shop.Daily` | 1 つのセーブのその日のショップ: 抽選、在庫、売り切れの枠、入れ替え、購入。訪問から次の訪問まで保持されます。 |
| `Shop.Catalog` | アイテム: モジュールの隣にある `Items.db3` の `itempool` テーブルと、各種類の購入で得られるもの。 |
| `Shop.Seasonal` | 決まった日付に大きな枠に固定される注目アイテム。 |
| `Shop.Popup` | `modal` ROActivity による任意の購入ポップアップ。必要とするスキン用です。 |
| `Shop.Texts` | モジュール自身の文字列 (`lang/<code>/shop.json`)。アイテム名を含みます。 |

## Shop.Daily

ショップは JST の毎日、`major` の行から大きな (注目) 枠を 1 つ、`regular` の行から通常枠を 4 つ、`PoolSize` で重み付けして抽選します。大きな枠の抽選が `empty` の行になった日は、代わりに通常枠が 6 つになります。季節のエントリが大きな枠を取ることもあります。入れ替えは 10 コインで、その日の入れ替えのたびに倍になり、すべての枠の在庫を元に戻します。

| 関数 | 説明 |
| --- | --- |
| `Daily.open(index, opts?)  -> shop` | セーブスロット `index` の今日のショップ。今日すでに抽選済みなら読み込み、そうでなければ抽選して保存します。`opts.store` はローカルストアの名前 (`"shop_state"`)、`opts.seasonal` は注目エントリ (`Shop.Seasonal`) を指定します。 |
| `shop.big`, `shop.normals[1..6]` | アイテム: カタログの行に `LocalizedName`、`Stock`、`SoldOut` を加えたもの。空の枠は nil。 |
| `shop:item(slot)  -> item` | 枠 1 から 6、または `Daily.SLOT_BIG`。 |
| `shop:refusal(slot, qty?)  -> string or nil` | その枠を今買えない理由: `"none"`、`"soldout"`、`"poor"`。 |
| `shop:buy(slot, qty?)  -> true, or false and a reason` | `qty` 個 (在庫で制限) を購入します: 付与し、価格を支払い、在庫を減らして保存します。 |
| `shop:reroll()  -> true, or false and "poor"` | 入れ替えの価格を支払い、新しい在庫を抽選します。 |
| `shop:rerollPrice()  -> number` | 次の入れ替えの価格。 |
| `shop:close()` | ストアを解放します。画面を閉じるときに呼び出してください。 |

状態はセーブファイルごと (その `SaveUID`) に、呼び出し元のステージの [DATABASE のローカルストア](../api/data.md)に保持されます: 日付、入れ替え回数、売り切れのマスク、各枠のアイテムコードと在庫。そのため、2 つのスキンのショップステージはそれぞれ別の日替わり在庫を持ちますが、購入したものは共有されます。

## Shop.Catalog

`itempool` の行:

| 列 | 説明 |
| --- | --- |
| `Code` | 一意のコード: マイルームの家具は `furn_<id>`、`vault_key_*`、`nameplate_<n>`、... |
| `Type` | 購入で得られるもの: `nameplate` (`RefInt` がネームプレートの id)、`triggerable` (トリガー `RefText` を設定)、`counterable` (カウンター `RefText` を数量だけ増やす)、`empty` (その日は大きな枠が空のまま)。 |
| `Price`, `Name` | コインでの価格と、モジュールがそのコードの文字列を持たないときに使う `LANG` JSON の名前。 |
| `Slot`, `PoolSize` | `regular` または `major`。プール内での行の重み。 |
| `Condition` | その行が提供されるために設定されていなければならないトリガー。 |
| `StockMin`, `StockMax` | `StockMax` が 0 より大きいとき、1 個ずつ購入する範囲付きの在庫 (塗料、床材)。 |
| `OneTime` | 1: 一度だけ購入でき、その後は二度と提供されません。 |

| 関数 | 説明 |
| --- | --- |
| `Catalog.pools()  -> { normal, big }`, `Catalog.rows(slot)`, `Catalog.find(code)` | 行。一度だけ読み込まれます。 |
| `Catalog.name(item)  -> string` | アイテムの名前: そのコードに対するモジュールの文字列、なければ行の `Name`。 |
| `Catalog.grant(save, item, qty)` | 購入を種類に応じて付与します (`Catalog.GRANTS`。スキンが独自の種類で拡張できます)。 |
| `Catalog.isOwned(item, save)`, `Catalog.isIncluded(item, save)` | セーブが一意のアイテムを所持しているかどうか。その行がそのセーブで抽選対象になるかどうか。 |
| `Catalog.furnitureId(code)  -> string` | `furn_<id>` コードのマイルームの家具 id。それ以外は nil。 |

## Shop.Seasonal と Shop.Popup

`Shop.Seasonal` は [almanac](calendar.md) のエントリ `{ code, when, priority, sticky }` のリストで、`code` はアイテムコードです。エントリが一致し、セーブがそのアイテムを所持していない間、そのアイテムが大きな枠を取ります。スキンは独自のリストを `Daily.open` に渡せます。

`Shop.Popup.show(item, qty, picture?)` は `modal` ROActivity で購入のポップアップを再生します (`picture` は共有テクスチャの名前で、家具の既定は `myroom_thumb_<id>`)。再生中は `Popup.update()` が true を返し、描画は `Popup.draw()`、停止は `Popup.stop()` で行います。

## セーブキー

購入は購入者のセーブに書き込まれます。

| キー | 種類 | 内容 |
| --- | --- | --- |
| `.myroom_<id>` | カウンター | 購入した家具 (`counterable` の行)。次の訪問時にマイルームに渡されます。 |
| `.vault_key_count_<key>` | カウンター | 購入した金庫の鍵。 |
| `<RefText>` | トリガー | 購入した `triggerable` の行。 |
| `<RefText>_owned` | トリガー | 購入した一度限りの行。二度と提供されません。 |
| `.almanac_<code>` | トリガー | 季節の sticky エントリがこのセーブで一致した。 |

ネームプレートとコインはセーブ自身のメソッド (`UnlockNameplate`、`SpendCoins`) を通じて扱います。

```lua
local Daily = require("Shop.Daily")
local shop

function activate()
    shop = Daily.open(slot)
end

local function buy(i)
    local ok, why = shop:buy(i, 1)
    if not ok then showRefusal(why) end
end

function deactivate()
    shop:close()
end
```
