<!-- modules/README.md -->

# Meegeleverde modules

OpenTaiko levert een bibliotheek van Lua-modules mee die elke skin kan gebruiken: hulpmodules (easing, kleuren, JSON, invoer, lokalisatie), een UI-bibliotheek, een dialoogvenster, de 3D-wereldengine, en de regels en data van de pagode, de geheime kluis en de muntwinkel. Ze staan in de spelmap, niet in een skin, dus een nieuwe skin krijgt ze zonder iets te kopiëren. Deze sectie beschrijft ze een voor een; de [API-referentie](../api/README.md) behandelt de globals van de engine waarop ze gebouwd zijn.

## Global/Modules

`Global/Modules/` in de installatiemap van het spel heeft dezelfde submappen als de `Modules/` van een skin: `Transitions/`, `Stages/`, `Activities/`, `ROActivities/` en `Lib/`. Het spel laadt elke globale categorie direct na die van de skin, voor welke skin er ook actief is. De modules op deze pagina's zijn de bestanden van `Global/Modules/Lib/`, die je met `require` bereikt; `Global/Modules/Stages/` bevat daarnaast stages die elke skin kan openen, zoals [Mijn kamer](my-room.md).

De map komt mee met de build van het spel en wordt bij elke update vervangen, dus een skin hoort er geen bestanden in te zetten. Wil je een meegeleverde module aanpassen, kopieer die dan onder een andere naam naar je eigen skin.

## De require-volgorde

`require` maakt van punten mappen en negeert hoofdletters, dus `require("Core.Easing")` laadt `Core/Easing.lua`. Welke mappen het doorzoekt, hangt af van de module die draait:

| Aanroeper | Doorzocht, in volgorde |
| --- | --- |
| Een module van de skin (`System/<skin>/Modules/...`) | zijn eigen map, de `Modules/Lib/` van de skin, daarna `Global/Modules/Lib/` |
| Een globale module (`Global/Modules/...`) | zijn eigen map, `Global/Modules/Lib/`, daarna de `Modules/Lib/` van de skin |

Een Lib-bestand heeft geen eigen Lua-state: het draait in de state van de module die het met require heeft geladen, dus de requires erin volgen de volgorde van die aanroeper. Een bestand in de `Modules/Lib/` van de skin met hetzelfde pad als een meegeleverde module vervangt die daarom voor elke require vanuit de modules van de skin, ook voor de requires binnen andere meegeleverde modules. Houd je eigen Lib-bestanden weg uit de mapnamen hieronder, tenzij je een module wilt vervangen.

## Naamgeving

Elk onderdeel is een map van `Global/Modules/Lib/`, en zijn modules laad je met require als `<Component>.<Module>`:

| Onderdeel | Modules | Pagina |
| --- | --- | --- |
| `Core` | `Easing`, `Color`, `Util`, `Json`, `Here` | [Core](core.md) |
| `Input` | `NavInput` | [Input](input.md) |
| `Audio` | `MusicDuck` | [Audio](audio.md) |
| `Locale` | `i18n` | [Locale](locale.md) |
| `Calendar` | `almanac` | [Calendar](calendar.md) |
| `Songs` | `PlayStrings` | [Songs](songs.md) |
| `World` | `OWM3d` en zijn onderdelen | [World](world.md) |
| `UI` | `SimpleUI`, `Pick` | [UI](ui.md) |
| `Saves` | `SavePicker` | [Saves](saves.md) |
| `Characters` | `Student` | [Characters](characters.md) |
| `VN` | `Dialogue` | [VN](vn.md) |
| `Pagoda` | `Levels`, `Floor`, `Rewards`, `Progress`, `Songs`, `Run`, `Texts` | [Pagoda](pagoda.md) |
| `Vault` | `Keys`, `Chests`, `Texts` | [Vault](vault.md) |
| `Shop` | `Daily`, `Catalog`, `Texts`, `Seasonal`, `Popup` | [Shop](shop.md) |
| `Online` | `Lobby`, `Protocol`, `Identity`, `Media`, `Texts` | [Online](online.md) |
| `Stages` | `CustomStages` | [Eigen stages](custom-stages.md) |

## De namen die Open-World Memories behoudt

De meegeleverde skin had de meeste van deze bestanden vroeger in zijn eigen `Modules/Lib/`. Elk oud bestand is nu een doorverwijzing van twee regels, dus een module van de skin die nog de oude naam laadt, krijgt de globale module, en beide namen delen één instantie (één state, één cache):

```lua
-- moved to Global/Modules/Lib/Core/Easing.lua
return require("Core.Easing")
```

| Oude naam | Meegeleverde module |
| --- | --- |
| `Easing`, `Color`, `Util` | `Core.Easing`, `Core.Color`, `Core.Util` |
| `NavInput`, `MusicDuck`, `PlayStrings` | `Input.NavInput`, `Audio.MusicDuck`, `Songs.PlayStrings` |
| `i18n`, `almanac` | `Locale.i18n`, `Calendar.almanac` |
| `OWM3d`, `OWM3d.<part>` | `World.OWM3d`, `World.OWM3d.<part>` |
| `PopUI.<part>` (de gedeelde code: `manager`, `widget`, `shape`, `util`, `cache`, `sfx`, elke `widgets.*` behalve `widgets.coins`) | `UI.SimpleUI.<part>` |
| `SavePicker`, `Student`, `dialogue` | `Saves.SavePicker`, `Characters.Student`, `VN.Dialogue` |

`PopUI` zelf blijft een module van de skin: het is SimpleUI met het eigen uiterlijk van de skin (zie [UI](ui.md)). Nieuwe code hoort de meegeleverde namen te gebruiken.

## Assets worden opgelost ten opzichte van de aanroeper

Relatieve paden die aan `TEXTURE`, `SOUND`, `VIDEO`, `JSONLOADER`, `SQL`, `DATABASE`, `STORAGE` en `SHARED` worden doorgegeven, worden opgelost ten opzichte van de map van de aanroepende module, nooit ten opzichte van het Lib-bestand dat de aanroep doet. Een meegeleverde module die eigen bestanden meelevert (een afbeelding, een JSON-pool, een database), vindt ze via `Core.Here`, dat de eigen map van het bestand teruggeeft, en laadt ze met de varianten voor absolute paden:

```lua
-- Global/Modules/Lib/MyPart/Thing.lua
local HERE = require("Core.Here")(...)          -- ".../Global/Modules/Lib/MyPart/"

local icon = TEXTURE:CreateTextureFromAbsolutePath(HERE .. "icon.png")
local beep = SOUND:CreateSFXFromAbsolutePath(HERE .. "beep.ogg")
local data = JSONLOADER:JsonParseFileAny(HERE .. "data.json")
SHARED:SetSharedTextureUsingAbsolutePath("thing_icon", HERE .. "icon.png")
```

Geef `...` door vanaf het hoogste niveau van het bestand: daar geeft `require` elk bestand zijn modulenaam en zijn absolute pad mee. Een relatief pad dat een aanroeper aan een meegeleverde module doorgeeft (een modelbestand voor `World.OWM3d`, een geluid voor een widget), is van de aanroeper en wordt zoals gewoonlijk opgelost ten opzichte van de map van de aanroeper.

## Strings horen bij de module

`THEME:GetSkinString` leest alleen de `Locales/` van de actieve skin, dus een meegeleverde module gebruikt het nooit voor zijn eigen tekst. Zijn strings staan in een map `lang/` ernaast, één JSON-bestand per onderdeel en taal (`lang/en/vault.json`, `lang/ja/vault.json`, ...), gelezen via [Locale.i18n](locale.md) met de map van de module:

```lua
local HERE = require("Core.Here")(...)
local T = require("Locale.i18n").texts("vault", HERE)   -- HERE .. "lang/<code>/vault.json"
local name = T:tr("chest_1")
```

Engels (`lang/en/`) is de terugval voor een taal waarin een bestand of een sleutel ontbreekt. Een globale stage of activity doet hetzelfde zonder het mapargument: `texts(name)` leest de map `lang/` van de stage zelf.

## Gedeelde status

De texturen, geluiden en strings van `SHARED` vormen één naamruimte voor het hele spel, voor modules van de skin en globale modules gelijk. De meegeleverde modules gebruiken een paar vaste sleutels, vermeld op hun pagina's (bijvoorbeeld de gedeelde string `ui_library` en de texturen `myroom_thumb_<id>`). Geef je eigen sleutels een prefix zodat ze niet kunnen botsen. Opslagdata (triggers, tellers en strings) hoort bij het opslagbestand, niet bij een module; zo geven de pagode, de kluis, de winkel en Mijn kamer elkaar items door.
