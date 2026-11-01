<!-- modules/shop.md -->

# Shop

La logique de la boutique de pièces : le catalogue, le stock du jour, les renouvellements, les prix, les achats et ce qu'ils accordent. Les modules `Shop` ne dessinent rien. Un skin dessine sa propre boutique et choisit ses propres transitions par-dessus ; chaque skin vend les mêmes articles selon les mêmes règles, et les achats atterrissent dans le fichier de sauvegarde. Le stage `coin_shop` d'Open-World Memories (le magazine et son marchand) est l'un de ces clients.

## Les modules

| Module | Ce qu'il contient |
| --- | --- |
| `Shop.Daily` | La boutique du jour d'une sauvegarde : le tirage, le stock, les emplacements épuisés, les renouvellements et les achats, conservés d'une visite à l'autre. |
| `Shop.Catalog` | Les articles : la table `itempool` de `Items.db3` à côté du module, et ce qu'accorde l'achat de chaque type. |
| `Shop.Seasonal` | Les articles mis en avant, placés dans le grand emplacement à des dates données. |
| `Shop.Popup` | Un pop-up d'achat facultatif via la ROActivity `modal`, pour un skin qui en veut un. |
| `Shop.Texts` | Les chaînes propres aux modules (`lang/<code>/shop.json`), noms d'articles compris. |

## Shop.Daily

Chaque jour JST, la boutique tire un grand emplacement (mis en avant) parmi les lignes `major` et quatre emplacements normaux parmi les lignes `regular`, pondérés par `PoolSize` ; quand le grand tirage donne la ligne `empty`, la journée a six emplacements normaux à la place. Une entrée saisonnière peut prendre le grand emplacement. Un renouvellement coûte 10 pièces, doublant à chaque renouvellement de la journée, et remet chaque emplacement en stock.

| Fonction | Description |
| --- | --- |
| `Daily.open(index, opts?)  -> shop` | La boutique du jour pour l'emplacement de sauvegarde `index` : chargée si elle a déjà été tirée aujourd'hui, sinon tirée et enregistrée. `opts.store` nomme le magasin local (`"shop_state"`), `opts.seasonal` les entrées mises en avant (`Shop.Seasonal`). |
| `shop.big`, `shop.normals[1..6]` | Les articles : des lignes du catalogue plus `LocalizedName`, `Stock` et `SoldOut` ; nil pour un emplacement vide. |
| `shop:item(slot)  -> item` | L'emplacement 1 à 6, ou `Daily.SLOT_BIG`. |
| `shop:refusal(slot, qty?)  -> string or nil` | Pourquoi l'emplacement ne peut pas être acheté maintenant : `"none"`, `"soldout"`, `"poor"`. |
| `shop:buy(slot, qty?)  -> true, or false and a reason` | Achète `qty` unités (limitées au stock) : les accorde, dépense le prix, baisse le stock et enregistre. |
| `shop:reroll()  -> true, or false and "poor"` | Dépense le prix du renouvellement et tire un nouveau stock. |
| `shop:rerollPrice()  -> number` | Le prix du prochain renouvellement. |
| `shop:close()` | Libère le magasin ; appelez-la quand l'écran se ferme. |

L'état est conservé par fichier de sauvegarde (son `SaveUID`) dans un [magasin local DATABASE](../api/data.md) du stage appelant : le jour, les renouvellements, le masque des emplacements épuisés, et le code d'article et le stock de chaque emplacement. Les stages de boutique de deux skins tiennent donc des stocks du jour séparés, tandis que ce qui a été acheté est partagé.

## Shop.Catalog

Une ligne de `itempool` :

| Colonne | Description |
| --- | --- |
| `Code` | Le code unique : `furn_<id>` pour les meubles de Ma Chambre, `vault_key_*`, `nameplate_<n>`, ... |
| `Type` | Ce qu'accorde un achat : `nameplate` (`RefInt` est l'identifiant de la plaque de nom), `triggerable` (définit le déclencheur `RefText`), `counterable` (augmente le compteur `RefText` de la quantité), `empty` (le grand emplacement reste vide ce jour-là). |
| `Price`, `Name` | Le prix en pièces, et un nom JSON `LANG` utilisé quand le module n'a pas de chaîne pour le code. |
| `Slot`, `PoolSize` | `regular` ou `major` ; le poids de la ligne dans son pool. |
| `Condition` | Un déclencheur qui doit être défini pour que la ligne soit proposée. |
| `StockMin`, `StockMax` | Un stock variable acheté unité par unité quand `StockMax` est supérieur à 0 (peintures, revêtements de sol). |
| `OneTime` | 1 : achetable une seule fois, puis plus jamais proposé. |

| Fonction | Description |
| --- | --- |
| `Catalog.pools()  -> { normal, big }`, `Catalog.rows(slot)`, `Catalog.find(code)` | Les lignes, lues une fois. |
| `Catalog.name(item)  -> string` | Le nom de l'article : la chaîne du module pour le code, sinon le `Name` de la ligne. |
| `Catalog.grant(save, item, qty)` | Accorde un achat selon son type (`Catalog.GRANTS`, qu'un skin peut étendre avec ses propres types). |
| `Catalog.isOwned(item, save)`, `Catalog.isIncluded(item, save)` | Si la sauvegarde possède un article unique ; si la ligne peut être tirée pour elle. |
| `Catalog.furnitureId(code)  -> string` | L'identifiant de meuble de Ma Chambre d'un code `furn_<id>`, sinon nil. |

## Shop.Seasonal et Shop.Popup

`Shop.Seasonal` est une liste d'entrées d'[almanach](calendar.md) `{ code, when, priority, sticky }`, `code` étant un code d'article : tant que l'une d'elles correspond et que la sauvegarde ne possède pas l'article, il prend le grand emplacement. Un skin peut passer sa propre liste à `Daily.open`.

`Shop.Popup.show(item, qty, picture?)` joue un pop-up d'achat via la ROActivity `modal` (`picture` est un nom de texture partagée ; pour les meubles, c'est `myroom_thumb_<id>` par défaut) ; `Popup.update()` renvoie true tant qu'il joue, à côté de `Popup.draw()` et `Popup.stop()`.

## Clés de sauvegarde

Un achat écrit dans la sauvegarde de l'acheteur :

| Clé | Nature | Contenu |
| --- | --- | --- |
| `.myroom_<id>` | compteur | Les meubles achetés (lignes `counterable`), transmis à Ma Chambre à sa prochaine visite. |
| `.vault_key_count_<key>` | compteur | Les clés de la chambre forte achetées. |
| `<RefText>` | déclencheur | Une ligne `triggerable` achetée. |
| `<RefText>_owned` | déclencheur | Une ligne unique achetée : elle n'est plus jamais proposée. |
| `.almanac_<code>` | déclencheur | Une entrée saisonnière sticky a correspondu pour cette sauvegarde. |

Les plaques de nom et les pièces passent par les méthodes propres à la sauvegarde (`UnlockNameplate`, `SpendCoins`).

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
