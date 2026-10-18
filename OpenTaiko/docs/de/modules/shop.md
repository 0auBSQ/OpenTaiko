<!-- modules/shop.md -->

# Shop

Die Logik des Münzladens: der Katalog, das Angebot des Tages, Neuauslosungen, Preise, Käufe und was sie gewähren. Die `Shop`-Module zeichnen nichts. Ein Skin zeichnet seinen eigenen Laden und wählt seine eigenen Übergänge darauf; jeder Skin verkauft dieselben Artikel nach denselben Regeln, und die Käufe landen im Spielstand. Die Stage `coin_shop` von Open-World Memories (das Magazin und sein Ladenbesitzer) ist ein solcher Client.

## Die Module

| Modul | Inhalt |
| --- | --- |
| `Shop.Daily` | Der Tagesladen eines Spielstands: die Auslosung, das Angebot, ausverkaufte Plätze, Neuauslosungen und Käufe, von einem Besuch zum nächsten behalten. |
| `Shop.Catalog` | Die Artikel: die Tabelle `itempool` von `Items.db3` neben dem Modul, und was der Kauf jedes Typs gewährt. |
| `Shop.Seasonal` | Die hervorgehobenen Artikel, die an bestimmten Tagen in den großen Platz gesetzt werden. |
| `Shop.Popup` | Ein optionales Kauf-Pop-up über die ROActivity `modal`, für einen Skin, der eines will. |
| `Shop.Texts` | Die eigenen Strings der Module (`lang/<code>/shop.json`), einschließlich der Artikelnamen. |

## Shop.Daily

An jedem JST-Tag lost der Laden einen großen (hervorgehobenen) Platz aus den Zeilen `major` und vier normale Plätze aus den Zeilen `regular` aus, gewichtet nach `PoolSize`; ergibt die große Auslosung die Zeile `empty`, hat der Tag stattdessen sechs normale Plätze. Ein saisonaler Eintrag kann den großen Platz übernehmen. Eine Neuauslosung kostet 10 Münzen, verdoppelt sich mit jeder weiteren Neuauslosung des Tages und füllt jeden Platz wieder auf.

| Funktion | Beschreibung |
| --- | --- |
| `Daily.open(index, opts?)  -> shop` | Der heutige Laden für den Spielstand-Slot `index`: geladen, wenn er heute schon ausgelost wurde, sonst ausgelost und gespeichert. `opts.store` benennt den lokalen Speicher (`"shop_state"`), `opts.seasonal` die hervorgehobenen Einträge (`Shop.Seasonal`). |
| `shop.big`, `shop.normals[1..6]` | Die Artikel: Katalogzeilen plus `LocalizedName`, `Stock` und `SoldOut`; nil für einen leeren Platz. |
| `shop:item(slot)  -> item` | Platz 1 bis 6, oder `Daily.SLOT_BIG`. |
| `shop:refusal(slot, qty?)  -> string or nil` | Warum der Platz gerade nicht gekauft werden kann: `"none"`, `"soldout"`, `"poor"`. |
| `shop:buy(slot, qty?)  -> true, or false and a reason` | Kauft `qty` Einheiten (auf den Bestand begrenzt): gewährt sie, zieht den Preis ab, senkt den Bestand und speichert. |
| `shop:reroll()  -> true, or false and "poor"` | Zahlt den Preis der Neuauslosung und lost ein neues Angebot aus. |
| `shop:rerollPrice()  -> number` | Der Preis der nächsten Neuauslosung. |
| `shop:close()` | Gibt den Speicher frei; rufen Sie es auf, wenn der Bildschirm schließt. |

Der Zustand wird pro Spielstand (über seine `SaveUID`) in einem [lokalen DATABASE-Speicher](../api/data.md) der aufrufenden Stage gehalten: der Tag, die Neuauslosungen, die Ausverkauft-Maske sowie Artikelcode und Bestand jedes Platzes. Die Laden-Stages zweier Skins führen daher getrennte Tagesangebote, während das Gekaufte geteilt wird.

## Shop.Catalog

Eine Zeile von `itempool`:

| Spalte | Beschreibung |
| --- | --- |
| `Code` | Der eindeutige Code: `furn_<id>` für Möbel von Mein Zimmer, `vault_key_*`, `nameplate_<n>`, ... |
| `Type` | Was ein Kauf gewährt: `nameplate` (`RefInt` ist die Namensschild-ID), `triggerable` (setzt den Trigger `RefText`), `counterable` (erhöht den Zähler `RefText` um die Menge), `empty` (der große Platz bleibt an diesem Tag leer). |
| `Price`, `Name` | Der Preis in Münzen, und ein `LANG`-JSON-Name, der verwendet wird, wenn das Modul keinen String für den Code hat. |
| `Slot`, `PoolSize` | `regular` oder `major`; das Gewicht der Zeile in ihrem Pool. |
| `Condition` | Ein Trigger, der gesetzt sein muss, damit die Zeile angeboten wird. |
| `StockMin`, `StockMax` | Ein Bestandsbereich, der Einheit für Einheit gekauft wird, wenn `StockMax` über 0 liegt (Farben, Bodenbeläge). |
| `OneTime` | 1: nur ein einziges Mal kaufbar, danach nie wieder angeboten. |

| Funktion | Beschreibung |
| --- | --- |
| `Catalog.pools()  -> { normal, big }`, `Catalog.rows(slot)`, `Catalog.find(code)` | Die Zeilen, einmal gelesen. |
| `Catalog.name(item)  -> string` | Der Name des Artikels: der String des Moduls für den Code, sonst der `Name` der Zeile. |
| `Catalog.grant(save, item, qty)` | Gewährt einen Kauf nach seinem Typ (`Catalog.GRANTS`, das ein Skin um eigene Typen erweitern kann). |
| `Catalog.isOwned(item, save)`, `Catalog.isIncluded(item, save)` | Ob der Spielstand einen einmaligen Artikel besitzt; ob die Zeile für ihn ausgelost werden kann. |
| `Catalog.furnitureId(code)  -> string` | Die Möbel-ID von Mein Zimmer zu einem Code `furn_<id>`, sonst nil. |

## Shop.Seasonal und Shop.Popup

`Shop.Seasonal` ist eine Liste von [Almanach](calendar.md)-Einträgen `{ code, when, priority, sticky }`, wobei `code` ein Artikelcode ist: Solange einer zutrifft und der Spielstand den Artikel nicht besitzt, übernimmt er den großen Platz. Ein Skin kann seine eigene Liste an `Daily.open` übergeben.

`Shop.Popup.show(item, qty, picture?)` spielt ein Kauf-Pop-up über die ROActivity `modal` ab (`picture` ist der Name einer gemeinsamen Textur; Möbel verwenden standardmäßig `myroom_thumb_<id>`); `Popup.update()` gibt true zurück, solange es läuft, dazu kommen `Popup.draw()` und `Popup.stop()`.

## Spielstand-Schlüssel

Ein Kauf schreibt in den Spielstand des Käufers:

| Schlüssel | Art | Inhalt |
| --- | --- | --- |
| `.myroom_<id>` | Zähler | Gekaufte Möbel (Zeilen `counterable`), beim nächsten Besuch an Mein Zimmer übergeben. |
| `.vault_key_count_<key>` | Zähler | Gekaufte Tresorschlüssel. |
| `<RefText>` | Trigger | Eine gekaufte Zeile `triggerable`. |
| `<RefText>_owned` | Trigger | Eine gekaufte einmalige Zeile: Sie wird nie wieder angeboten. |
| `.almanac_<code>` | Trigger | Ein sticky-Saisoneintrag hat für diesen Spielstand zugetroffen. |

Namensschilder und Münzen laufen über die eigenen Methoden des Spielstands (`UnlockNameplate`, `SpendCoins`).

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
