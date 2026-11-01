<!-- modules/shop.md -->

# Shop

金币商店的逻辑：商品目录、当天的库存、换一批、价格、购买以及购买所得。`Shop` 模块不绘制任何内容。皮肤在其之上绘制自己的商店并选择自己的过渡；所有皮肤按相同的规则出售相同的商品，购买结果写入存档。Open-World Memories 的 `coin_shop` 舞台（杂志和店主）就是这样的一个客户端。

## 模块一览

| 模块 | 内容 |
| --- | --- |
| `Shop.Daily` | 某个存档当天的商店：抽选、库存、售罄的栏位、换一批和购买，在两次进入之间保留。 |
| `Shop.Catalog` | 商品：模块旁边 `Items.db3` 中的 `itempool` 表，以及购买每种类型所得的内容。 |
| `Shop.Seasonal` | 在指定日期固定到大栏位的精选商品。 |
| `Shop.Popup` | 通过 `modal` 只读活动显示的可选购买弹窗，供需要它的皮肤使用。 |
| `Shop.Texts` | 模块自己的字符串（`lang/<code>/shop.json`），包括商品名称。 |

## Shop.Daily

每个 JST 日，商店从 `major` 行中抽出一个大（精选）栏位，从 `regular` 行中抽出四个普通栏位，按 `PoolSize` 加权；大栏位抽到 `empty` 行时，当天改为六个普通栏位。季节条目可以占据大栏位。换一批花费 10 金币，当天每换一次价格翻倍，并让所有栏位恢复库存。

| 函数 | 说明 |
| --- | --- |
| `Daily.open(index, opts?)  -> shop` | 存档槽位 `index` 今天的商店：今天已经抽选过就加载，否则抽选并保存。`opts.store` 指定本地存储的名称（`"shop_state"`），`opts.seasonal` 指定精选条目（`Shop.Seasonal`）。 |
| `shop.big`, `shop.normals[1..6]` | 商品：目录行加上 `LocalizedName`、`Stock` 和 `SoldOut`；空栏位为 nil。 |
| `shop:item(slot)  -> item` | 栏位 1 到 6，或 `Daily.SLOT_BIG`。 |
| `shop:refusal(slot, qty?)  -> string or nil` | 该栏位现在无法购买的原因：`"none"`、`"soldout"`、`"poor"`。 |
| `shop:buy(slot, qty?)  -> true, or false and a reason` | 购买 `qty` 件（受库存限制）：发放、扣除价格、减少库存并保存。 |
| `shop:reroll()  -> true, or false and "poor"` | 支付换一批的价格并抽选新的库存。 |
| `shop:rerollPrice()  -> number` | 下一次换一批的价格。 |
| `shop:close()` | 释放存储；关闭界面时调用。 |

状态按存档（其 `SaveUID`）保存在调用方舞台的 [DATABASE 本地存储](../api/data.md)中：日期、换一批的次数、售罄掩码，以及每个栏位的商品代码和库存。因此，两个皮肤的商店舞台各自拥有独立的每日库存，而已购买的东西是共享的。

## Shop.Catalog

一条 `itempool` 行：

| 列 | 说明 |
| --- | --- |
| `Code` | 唯一代码：我的房间的家具为 `furn_<id>`，还有 `vault_key_*`、`nameplate_<n>`…… |
| `Type` | 购买所得：`nameplate`（`RefInt` 是名牌 id）、`triggerable`（设置触发器 `RefText`）、`counterable`（把计数器 `RefText` 增加购买数量）、`empty`（当天大栏位保持为空）。 |
| `Price`, `Name` | 以金币计的价格，以及模块没有该代码的字符串时使用的 `LANG` JSON 名称。 |
| `Slot`, `PoolSize` | `regular` 或 `major`；该行在其池中的权重。 |
| `Condition` | 该行要被提供所必须已设置的触发器。 |
| `StockMin`, `StockMax` | `StockMax` 大于 0 时，按件购买的范围库存（涂料、地板）。 |
| `OneTime` | 1：一生只能购买一次，之后不再提供。 |

| 函数 | 说明 |
| --- | --- |
| `Catalog.pools()  -> { normal, big }`, `Catalog.rows(slot)`, `Catalog.find(code)` | 这些行，只读取一次。 |
| `Catalog.name(item)  -> string` | 商品的名称：模块中该代码的字符串，否则为该行的 `Name`。 |
| `Catalog.grant(save, item, qty)` | 按类型发放一次购买（`Catalog.GRANTS`，皮肤可以用自己的类型扩展它）。 |
| `Catalog.isOwned(item, save)`, `Catalog.isIncluded(item, save)` | 存档是否拥有某件唯一商品；该行能否为该存档抽选。 |
| `Catalog.furnitureId(code)  -> string` | `furn_<id>` 代码对应的我的房间家具 id，否则为 nil。 |

## Shop.Seasonal 和 Shop.Popup

`Shop.Seasonal` 是一个 [almanac](calendar.md) 条目 `{ code, when, priority, sticky }` 的列表，其中 `code` 是商品代码：某个条目匹配且存档未拥有该商品时，它占据大栏位。皮肤可以把自己的列表传给 `Daily.open`。

`Shop.Popup.show(item, qty, picture?)` 通过 `modal` 只读活动播放购买弹窗（`picture` 是共享纹理名称；家具默认为 `myroom_thumb_<id>`）；播放期间 `Popup.update()` 返回 true，用 `Popup.draw()` 绘制，用 `Popup.stop()` 停止。

## 存档键

购买写入买家的存档：

| 键 | 类型 | 内容 |
| --- | --- | --- |
| `.myroom_<id>` | 计数器 | 购买的家具（`counterable` 行），在下次进入我的房间时交给它。 |
| `.vault_key_count_<key>` | 计数器 | 购买的金库钥匙。 |
| `<RefText>` | 触发器 | 已购买的 `triggerable` 行。 |
| `<RefText>_owned` | 触发器 | 已购买的一次性行：之后不再提供。 |
| `.almanac_<code>` | 触发器 | 某个 sticky 季节条目已在该存档上匹配过。 |

名牌和金币通过存档自身的方法（`UnlockNameplate`、`SpendCoins`）处理。

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
