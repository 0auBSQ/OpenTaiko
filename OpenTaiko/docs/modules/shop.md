<!-- modules/shop.md -->

# Shop

The coin shop's logic: the catalogue, the stock of the day, rerolls, prices, purchases and what they grant. The `Shop` modules draw nothing. A skin draws its own shop and picks its own transitions on top of them; every skin sells the same items by the same rules, and the purchases land in the save file. Open-World Memories' `coin_shop` stage (the magazine and its keeper) is one such client.

## The modules

| Module | What it holds |
| --- | --- |
| `Shop.Daily` | One save's shop of the day: the roll, the stock, sold-out slots, rerolls and purchases, kept from one visit to the next. |
| `Shop.Catalog` | The items: the `itempool` table of `Items.db3` next to the module, and what buying each type grants. |
| `Shop.Seasonal` | The featured items pinned into the big slot on given dates. |
| `Shop.Popup` | An optional purchase pop-up through the `modal` ROActivity, for a skin that wants one. |
| `Shop.Texts` | The modules' own strings (`lang/<code>/shop.json`), item names included. |

## Shop.Daily

Each JST day the shop rolls a big (featured) slot from the `major` rows and four normal slots from the `regular` rows, weighted by `PoolSize`; when the big roll gives the `empty` row, the day has six normal slots instead. A seasonal entry can take the big slot. A reroll costs 10 coins, doubling with each reroll of the day, and puts every slot back in stock.

| Function | Description |
| --- | --- |
| `Daily.open(index, opts?)  -> shop` | Today's shop for save slot `index`: loaded when it was already rolled today, rolled and saved otherwise. `opts.store` names the local store (`"shop_state"`), `opts.seasonal` the featured entries (`Shop.Seasonal`). |
| `shop.big`, `shop.normals[1..6]` | The items: catalogue rows plus `LocalizedName`, `Stock` and `SoldOut`; nil for an empty slot. |
| `shop:item(slot)  -> item` | Slot 1 to 6, or `Daily.SLOT_BIG`. |
| `shop:refusal(slot, qty?)  -> string or nil` | Why the slot can't be bought now: `"none"`, `"soldout"`, `"poor"`. |
| `shop:buy(slot, qty?)  -> true, or false and a reason` | Buys `qty` units (clamped to the stock): grants them, spends the price, lowers the stock and saves. |
| `shop:reroll()  -> true, or false and "poor"` | Spends the reroll price and rolls a new stock. |
| `shop:rerollPrice()  -> number` | The price of the next reroll. |
| `shop:close()` | Releases the store; call it when the screen closes. |

The state is kept per save file (its `SaveUID`) in a [DATABASE local store](../api/data.md) of the calling stage: the day, the rerolls, the sold-out mask, and each slot's item code and stock. Two skins' shop stages therefore keep separate daily stocks, while what was bought is shared.

## Shop.Catalog

An `itempool` row:

| Column | Description |
| --- | --- |
| `Code` | The unique code: `furn_<id>` for My Room furniture, `vault_key_*`, `nameplate_<n>`, ... |
| `Type` | What a purchase grants: `nameplate` (`RefInt` is the nameplate id), `triggerable` (sets the trigger `RefText`), `counterable` (raises the counter `RefText` by the quantity), `empty` (the big slot stays empty that day). |
| `Price`, `Name` | The price in coins, and a `LANG` JSON name used when the module has no string for the code. |
| `Slot`, `PoolSize` | `regular` or `major`; the row's weight in its pool. |
| `Condition` | A trigger that must be set for the row to be offered. |
| `StockMin`, `StockMax` | A ranged stock bought unit by unit when `StockMax` is above 0 (paints, floorings). |
| `OneTime` | 1: bought once ever, then never offered again. |

| Function | Description |
| --- | --- |
| `Catalog.pools()  -> { normal, big }`, `Catalog.rows(slot)`, `Catalog.find(code)` | The rows, read once. |
| `Catalog.name(item)  -> string` | The item's name: the module's string for the code, else the row's `Name`. |
| `Catalog.grant(save, item, qty)` | Grants a purchase by its type (`Catalog.GRANTS`, which a skin may extend with its own types). |
| `Catalog.isOwned(item, save)`, `Catalog.isIncluded(item, save)` | Whether the save owns a unique item; whether the row can be rolled for it. |
| `Catalog.furnitureId(code)  -> string` | The My Room furniture id of a `furn_<id>` code, else nil. |

## Shop.Seasonal and Shop.Popup

`Shop.Seasonal` is a list of [almanac](calendar.md) entries `{ code, when, priority, sticky }`, `code` being an item code: while one matches and the save does not own the item, it takes the big slot. A skin can pass its own list to `Daily.open`.

`Shop.Popup.show(item, qty, picture?)` plays a purchase pop-up through the `modal` ROActivity (`picture` is a shared texture name; furniture defaults to `myroom_thumb_<id>`); `Popup.update()` returns true while it plays, then `Popup.draw()` and `Popup.stop()`.

## Save keys

A purchase writes to the buyer's save:

| Key | Kind | Holds |
| --- | --- | --- |
| `.myroom_<id>` | counter | Furniture bought (`counterable` rows), handed to My Room on its next visit. |
| `.vault_key_count_<key>` | counter | Vault keys bought. |
| `<RefText>` | trigger | A `triggerable` row bought. |
| `<RefText>_owned` | trigger | A one-time row bought: it is never offered again. |
| `.almanac_<code>` | trigger | A sticky seasonal entry has matched for this save. |

Nameplates and coins go through the save's own methods (`UnlockNameplate`, `SpendCoins`).

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
