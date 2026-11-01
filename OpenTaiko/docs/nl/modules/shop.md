<!-- modules/shop.md -->

# Shop

De logica van de muntwinkel: de catalogus, de voorraad van de dag, nieuwe trekkingen, prijzen, aankopen en wat ze opleveren. De `Shop`-modules tekenen niets. Een skin tekent zijn eigen winkel en kiest zijn eigen transities erbovenop; elke skin verkoopt dezelfde items volgens dezelfde regels, en de aankopen komen in het opslagbestand terecht. De stage `coin_shop` van Open-World Memories (het tijdschrift en zijn winkelier) is zo'n client.

## De modules

| Module | Wat het bevat |
| --- | --- |
| `Shop.Daily` | De winkel van de dag van één opslagbestand: de trekking, de voorraad, uitverkochte plekken, nieuwe trekkingen en aankopen, bewaard van het ene bezoek tot het volgende. |
| `Shop.Catalog` | De items: de tabel `itempool` van `Items.db3` naast de module, en wat de aankoop van elk type oplevert. |
| `Shop.Seasonal` | De uitgelichte items die op bepaalde data in de grote plek worden gezet. |
| `Shop.Popup` | Een optionele aankooppop-up via de ROActivity `modal`, voor een skin die er een wil. |
| `Shop.Texts` | De eigen strings van de modules (`lang/<code>/shop.json`), inclusief de itemnamen. |

## Shop.Daily

Elke JST-dag trekt de winkel een grote (uitgelichte) plek uit de rijen `major` en vier gewone plekken uit de rijen `regular`, gewogen op `PoolSize`; geeft de grote trekking de rij `empty`, dan heeft de dag in plaats daarvan zes gewone plekken. Een seizoensitem kan de grote plek innemen. Een nieuwe trekking kost 10 munten, verdubbelt bij elke volgende nieuwe trekking van de dag, en vult elke plek weer aan.

| Functie | Beschrijving |
| --- | --- |
| `Daily.open(index, opts?)  -> shop` | De winkel van vandaag voor opslagslot `index`: geladen als hij vandaag al getrokken is, anders getrokken en opgeslagen. `opts.store` noemt de lokale store (`"shop_state"`), `opts.seasonal` de uitgelichte items (`Shop.Seasonal`). |
| `shop.big`, `shop.normals[1..6]` | De items: catalogusrijen plus `LocalizedName`, `Stock` en `SoldOut`; nil voor een lege plek. |
| `shop:item(slot)  -> item` | Plek 1 tot 6, of `Daily.SLOT_BIG`. |
| `shop:refusal(slot, qty?)  -> string or nil` | Waarom de plek nu niet gekocht kan worden: `"none"`, `"soldout"`, `"poor"`. |
| `shop:buy(slot, qty?)  -> true, or false and a reason` | Koopt `qty` eenheden (begrensd tot de voorraad): kent ze toe, betaalt de prijs, verlaagt de voorraad en slaat op. |
| `shop:reroll()  -> true, or false and "poor"` | Betaalt de prijs van een nieuwe trekking en trekt een nieuwe voorraad. |
| `shop:rerollPrice()  -> number` | De prijs van de volgende nieuwe trekking. |
| `shop:close()` | Geeft de store vrij; roep het aan wanneer het scherm sluit. |

De status wordt per opslagbestand (via zijn `SaveUID`) bewaard in een [lokale DATABASE-store](../api/data.md) van de aanroepende stage: de dag, de nieuwe trekkingen, het masker van uitverkochte plekken, en de itemcode en voorraad van elke plek. De winkelstages van twee skins houden daarom aparte dagvoorraden bij, terwijl wat gekocht is gedeeld wordt.

## Shop.Catalog

Een rij van `itempool`:

| Kolom | Beschrijving |
| --- | --- |
| `Code` | De unieke code: `furn_<id>` voor meubels van Mijn kamer, `vault_key_*`, `nameplate_<n>`, ... |
| `Type` | Wat een aankoop oplevert: `nameplate` (`RefInt` is het id van het naamplaatje), `triggerable` (zet de trigger `RefText`), `counterable` (verhoogt de teller `RefText` met de hoeveelheid), `empty` (de grote plek blijft die dag leeg). |
| `Price`, `Name` | De prijs in munten, en een `LANG`-JSON-naam die wordt gebruikt als de module geen string voor de code heeft. |
| `Slot`, `PoolSize` | `regular` of `major`; het gewicht van de rij in haar pool. |
| `Condition` | Een trigger die gezet moet zijn voordat de rij wordt aangeboden. |
| `StockMin`, `StockMax` | Een voorraadbereik dat per eenheid wordt gekocht wanneer `StockMax` boven 0 ligt (verf, vloerbedekking). |
| `OneTime` | 1: maar één keer te kopen, daarna nooit meer aangeboden. |

| Functie | Beschrijving |
| --- | --- |
| `Catalog.pools()  -> { normal, big }`, `Catalog.rows(slot)`, `Catalog.find(code)` | De rijen, één keer gelezen. |
| `Catalog.name(item)  -> string` | De naam van het item: de string van de module voor de code, anders de `Name` van de rij. |
| `Catalog.grant(save, item, qty)` | Kent een aankoop toe volgens zijn type (`Catalog.GRANTS`, dat een skin met eigen types kan uitbreiden). |
| `Catalog.isOwned(item, save)`, `Catalog.isIncluded(item, save)` | Of het opslagbestand een uniek item bezit; of de rij ervoor getrokken kan worden. |
| `Catalog.furnitureId(code)  -> string` | Het meubel-id van Mijn kamer bij een code `furn_<id>`, anders nil. |

## Shop.Seasonal en Shop.Popup

`Shop.Seasonal` is een lijst van [almanak](calendar.md)-items `{ code, when, priority, sticky }`, waarbij `code` een itemcode is: zolang er een overeenkomt en het opslagbestand het item niet bezit, neemt het de grote plek in. Een skin kan zijn eigen lijst aan `Daily.open` doorgeven.

`Shop.Popup.show(item, qty, picture?)` speelt een aankooppop-up af via de ROActivity `modal` (`picture` is de naam van een gedeelde textuur; voor meubels is dat standaard `myroom_thumb_<id>`); `Popup.update()` geeft true terug zolang hij speelt, met daarnaast `Popup.draw()` en `Popup.stop()`.

## Opslagsleutels

Een aankoop schrijft naar het opslagbestand van de koper:

| Sleutel | Soort | Inhoud |
| --- | --- | --- |
| `.myroom_<id>` | teller | Gekochte meubels (rijen `counterable`), bij het volgende bezoek doorgegeven aan Mijn kamer. |
| `.vault_key_count_<key>` | teller | Gekochte kluissleutels. |
| `<RefText>` | trigger | Een gekochte rij `triggerable`. |
| `<RefText>_owned` | trigger | Een gekochte eenmalige rij: die wordt nooit meer aangeboden. |
| `.almanac_<code>` | trigger | Een sticky seizoensitem heeft voor dit opslagbestand overeengekomen. |

Naamplaatjes en munten lopen via de eigen methoden van het opslagbestand (`UnlockNameplate`, `SpendCoins`).

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
