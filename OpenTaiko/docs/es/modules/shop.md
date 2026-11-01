<!-- modules/shop.md -->

# Shop

La lógica de la tienda de monedas: el catálogo, las existencias del día, las renovaciones, los precios, las compras y lo que conceden. Los módulos `Shop` no dibujan nada. Un skin dibuja su propia tienda y elige sus propias transiciones encima de ellos; todos los skins venden los mismos artículos con las mismas reglas, y las compras quedan en el archivo de guardado. El stage `coin_shop` de Open-World Memories (la revista y su dependiente) es uno de esos clientes.

## Los módulos

| Módulo | Qué contiene |
| --- | --- |
| `Shop.Daily` | La tienda del día de una partida guardada: el sorteo, las existencias, las ranuras agotadas, las renovaciones y las compras, conservados de una visita a la siguiente. |
| `Shop.Catalog` | Los artículos: la tabla `itempool` de `Items.db3`, junto al módulo, y lo que concede la compra de cada tipo. |
| `Shop.Seasonal` | Los artículos destacados que ocupan la ranura grande en fechas concretas. |
| `Shop.Popup` | Una ventana emergente de compra opcional a través de la ROActivity `modal`, para un skin que la quiera. |
| `Shop.Texts` | Las cadenas propias de los módulos (`lang/<code>/shop.json`), incluidos los nombres de los artículos. |

## Shop.Daily

Cada día JST, la tienda sortea una ranura grande (destacada) entre las filas `major` y cuatro ranuras normales entre las filas `regular`, ponderadas por `PoolSize`; cuando el sorteo grande da la fila `empty`, el día tiene en su lugar seis ranuras normales. Una entrada de temporada puede ocupar la ranura grande. Una renovación cuesta 10 monedas, el doble con cada renovación del día, y repone las existencias de todas las ranuras.

| Función | Descripción |
| --- | --- |
| `Daily.open(index, opts?)  -> shop` | La tienda de hoy para la ranura de guardado `index`: se carga si ya se sorteó hoy, y si no se sortea y se guarda. `opts.store` nombra el almacén local (`"shop_state"`), `opts.seasonal` las entradas destacadas (`Shop.Seasonal`). |
| `shop.big`, `shop.normals[1..6]` | Los artículos: filas del catálogo más `LocalizedName`, `Stock` y `SoldOut`; nil para una ranura vacía. |
| `shop:item(slot)  -> item` | La ranura 1 a 6, o `Daily.SLOT_BIG`. |
| `shop:refusal(slot, qty?)  -> string or nil` | Por qué la ranura no se puede comprar ahora: `"none"`, `"soldout"`, `"poor"`. |
| `shop:buy(slot, qty?)  -> true, or false and a reason` | Compra `qty` unidades (limitadas a las existencias): las concede, gasta el precio, reduce las existencias y guarda. |
| `shop:reroll()  -> true, or false and "poor"` | Gasta el precio de la renovación y sortea nuevas existencias. |
| `shop:rerollPrice()  -> number` | El precio de la siguiente renovación. |
| `shop:close()` | Libera el almacén; llámala cuando se cierre la pantalla. |

El estado se guarda por archivo de guardado (su `SaveUID`) en un [almacén local de DATABASE](../api/data.md) del stage que llama: el día, las renovaciones, la máscara de agotados, y el código de artículo y las existencias de cada ranura. Por eso las tiendas de dos skins mantienen existencias diarias separadas, mientras que lo comprado se comparte.

## Shop.Catalog

Una fila de `itempool`:

| Columna | Descripción |
| --- | --- |
| `Code` | El código único: `furn_<id>` para los muebles de Mi Habitación, `vault_key_*`, `nameplate_<n>`, ... |
| `Type` | Lo que concede una compra: `nameplate` (`RefInt` es el id de la placa de nombre), `triggerable` (activa el trigger `RefText`), `counterable` (aumenta el contador `RefText` en la cantidad), `empty` (la ranura grande queda vacía ese día). |
| `Price`, `Name` | El precio en monedas, y un nombre JSON de `LANG` que se usa cuando el módulo no tiene cadena para el código. |
| `Slot`, `PoolSize` | `regular` o `major`; el peso de la fila en su conjunto. |
| `Condition` | Un trigger que debe estar activado para que se ofrezca la fila. |
| `StockMin`, `StockMax` | Existencias con rango que se compran unidad a unidad cuando `StockMax` es mayor que 0 (pinturas, suelos). |
| `OneTime` | 1: se compra una sola vez y nunca se vuelve a ofrecer. |

| Función | Descripción |
| --- | --- |
| `Catalog.pools()  -> { normal, big }`, `Catalog.rows(slot)`, `Catalog.find(code)` | Las filas, leídas una vez. |
| `Catalog.name(item)  -> string` | El nombre del artículo: la cadena del módulo para el código, y si no el `Name` de la fila. |
| `Catalog.grant(save, item, qty)` | Concede una compra según su tipo (`Catalog.GRANTS`, que un skin puede ampliar con sus propios tipos). |
| `Catalog.isOwned(item, save)`, `Catalog.isIncluded(item, save)` | Si la partida guardada posee un artículo único; si la fila puede sortearse para ella. |
| `Catalog.furnitureId(code)  -> string` | El id del mueble de Mi Habitación de un código `furn_<id>`, y si no nil. |

## Shop.Seasonal y Shop.Popup

`Shop.Seasonal` es una lista de entradas del [almanaque](calendar.md) `{ code, when, priority, sticky }`, donde `code` es un código de artículo: mientras una coincide y la partida guardada no posee el artículo, ocupa la ranura grande. Un skin puede pasar su propia lista a `Daily.open`.

`Shop.Popup.show(item, qty, picture?)` reproduce una ventana emergente de compra a través de la ROActivity `modal` (`picture` es el nombre de una textura compartida; para los muebles es `myroom_thumb_<id>` por defecto); `Popup.update()` devuelve true mientras se reproduce, y luego `Popup.draw()` y `Popup.stop()`.

## Claves de guardado

Una compra escribe en la partida guardada del comprador:

| Clave | Tipo | Contenido |
| --- | --- | --- |
| `.myroom_<id>` | contador | Muebles comprados (filas `counterable`), entregados a Mi Habitación en su siguiente visita. |
| `.vault_key_count_<key>` | contador | Llaves de la bóveda compradas. |
| `<RefText>` | trigger | Se compró una fila `triggerable`. |
| `<RefText>_owned` | trigger | Se compró una fila de compra única: nunca se vuelve a ofrecer. |
| `.almanac_<code>` | trigger | Una entrada de temporada sticky ha coincidido para esta partida guardada. |

Las placas de nombre y las monedas pasan por los métodos propios del archivo de guardado (`UnlockNameplate`, `SpendCoins`).

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
