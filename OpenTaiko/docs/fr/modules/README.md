<!-- modules/README.md -->

# Modules fournis

OpenTaiko livre une bibliothèque de modules Lua que tout skin peut utiliser : des utilitaires (easing, couleurs, JSON, entrées, localisation), une bibliothèque d'interface, une boîte de dialogue, le moteur de monde 3D, ainsi que les règles et les données de la pagode, de la chambre forte secrète et de la boutique de pièces. Ils se trouvent dans le dossier du jeu, pas dans un skin, si bien qu'un nouveau skin en dispose sans rien copier. Cette section décrit chacun d'eux ; la [référence de l'API](../api/README.md) couvre les globales du moteur sur lesquelles ils reposent.

## Global/Modules

`Global/Modules/` dans le dossier d'installation du jeu a les mêmes sous-dossiers que le `Modules/` d'un skin : `Transitions/`, `Stages/`, `Activities/`, `ROActivities/` et `Lib/`. Le jeu charge chaque catégorie globale juste après celle du skin, quel que soit le skin actif. Les modules de ces pages sont les fichiers de `Global/Modules/Lib/`, que l'on atteint avec `require` ; `Global/Modules/Stages/` contient aussi des stages que tout skin peut ouvrir, comme [Ma Chambre](my-room.md).

Le dossier arrive avec la build du jeu et chaque mise à jour le remplace, donc un skin ne doit pas y déposer de fichiers. Pour modifier un module fourni, copiez-le dans votre propre skin sous un autre nom.

## L'ordre de require

`require` transforme les points en dossiers et ignore la casse, si bien que `require("Core.Easing")` charge `Core/Easing.lua`. Les dossiers qu'il parcourt dépendent du module qui s'exécute :

| Appelant | Dossiers parcourus, dans l'ordre |
| --- | --- |
| Un module du skin (`System/<skin>/Modules/...`) | son propre dossier, le `Modules/Lib/` du skin, puis `Global/Modules/Lib/` |
| Un module global (`Global/Modules/...`) | son propre dossier, `Global/Modules/Lib/`, puis le `Modules/Lib/` du skin |

Un fichier de Lib n'a pas d'état Lua à lui : il s'exécute dans l'état du module qui l'a chargé avec require, si bien que les require qu'il contient suivent l'ordre de cet appelant. Un fichier du `Modules/Lib/` du skin ayant le même chemin qu'un module fourni le remplace donc pour chaque require fait depuis les modules du skin, y compris les require faits à l'intérieur d'autres modules fournis. Tenez vos propres fichiers de Lib à l'écart des noms de dossiers ci-dessous, sauf si vous voulez remplacer un module.

## Nommage

Chaque composant est un dossier de `Global/Modules/Lib/`, et ses modules se chargent sous la forme `<Component>.<Module>` :

| Composant | Modules | Page |
| --- | --- | --- |
| `Core` | `Easing`, `Color`, `Util`, `Json`, `Here` | [Core](core.md) |
| `Input` | `NavInput` | [Input](input.md) |
| `Audio` | `MusicDuck` | [Audio](audio.md) |
| `Locale` | `i18n` | [Locale](locale.md) |
| `Calendar` | `almanac` | [Calendar](calendar.md) |
| `Songs` | `PlayStrings` | [Songs](songs.md) |
| `World` | `OWM3d` et ses parties | [World](world.md) |
| `UI` | `SimpleUI`, `Pick` | [UI](ui.md) |
| `Saves` | `SavePicker` | [Saves](saves.md) |
| `Characters` | `Student` | [Characters](characters.md) |
| `VN` | `Dialogue` | [VN](vn.md) |
| `Pagoda` | `Levels`, `Floor`, `Rewards`, `Progress`, `Songs`, `Run`, `Texts` | [Pagoda](pagoda.md) |
| `Vault` | `Keys`, `Chests`, `Texts` | [Vault](vault.md) |
| `Shop` | `Daily`, `Catalog`, `Texts`, `Seasonal`, `Popup` | [Shop](shop.md) |

## Les noms que conserve Open-World Memories

Le skin fourni gardait autrefois la plupart de ces fichiers dans son propre `Modules/Lib/`. Chaque ancien fichier est désormais un relais de deux lignes, si bien qu'un module du skin qui charge encore l'ancien nom obtient le module global, et les deux noms partagent une seule instance (un seul état, un seul cache) :

```lua
-- moved to Global/Modules/Lib/Core/Easing.lua
return require("Core.Easing")
```

| Ancien nom | Module fourni |
| --- | --- |
| `Easing`, `Color`, `Util` | `Core.Easing`, `Core.Color`, `Core.Util` |
| `NavInput`, `MusicDuck`, `PlayStrings` | `Input.NavInput`, `Audio.MusicDuck`, `Songs.PlayStrings` |
| `i18n`, `almanac` | `Locale.i18n`, `Calendar.almanac` |
| `OWM3d`, `OWM3d.<part>` | `World.OWM3d`, `World.OWM3d.<part>` |
| `PopUI.<part>` (le code partagé : `manager`, `widget`, `shape`, `util`, `cache`, `sfx`, tous les `widgets.*` sauf `widgets.coins`) | `UI.SimpleUI.<part>` |
| `SavePicker`, `Student`, `dialogue` | `Saves.SavePicker`, `Characters.Student`, `VN.Dialogue` |

`PopUI` reste lui-même un module du skin : c'est SimpleUI avec l'apparence propre au skin (voir [UI](ui.md)). Le nouveau code doit charger les noms fournis.

## Les ressources se résolvent par rapport à l'appelant

Les chemins relatifs passés à `TEXTURE`, `SOUND`, `VIDEO`, `JSONLOADER`, `SQL`, `DATABASE`, `STORAGE` et `SHARED` sont résolus par rapport au dossier du module appelant, jamais par rapport au fichier de Lib qui fait l'appel. Un module fourni qui embarque ses propres fichiers (une image, un pool JSON, une base de données) les trouve grâce à `Core.Here`, qui renvoie le dossier du fichier lui-même, et les charge avec les variantes à chemin absolu :

```lua
-- Global/Modules/Lib/MyPart/Thing.lua
local HERE = require("Core.Here")(...)          -- ".../Global/Modules/Lib/MyPart/"

local icon = TEXTURE:CreateTextureFromAbsolutePath(HERE .. "icon.png")
local beep = SOUND:CreateSFXFromAbsolutePath(HERE .. "beep.ogg")
local data = JSONLOADER:JsonParseFileAny(HERE .. "data.json")
SHARED:SetSharedTextureUsingAbsolutePath("thing_icon", HERE .. "icon.png")
```

Passez `...` depuis le niveau supérieur du fichier : c'est là que `require` donne à chaque fichier son nom de module et son chemin absolu. Un chemin relatif qu'un appelant passe à un module fourni (un fichier de modèle pour `World.OWM3d`, un son pour un widget) appartient à l'appelant et se résout par rapport au dossier de l'appelant, comme d'habitude.

## Les chaînes vivent avec le module

`THEME:GetSkinString` ne lit que les `Locales/` du skin actif, si bien qu'un module fourni ne l'utilise jamais pour ses propres textes. Ses chaînes se trouvent dans un dossier `lang/` à côté de lui, un fichier JSON par fonctionnalité et par langue (`lang/en/vault.json`, `lang/ja/vault.json`, ...), lues via [Locale.i18n](locale.md) avec le dossier du module :

```lua
local HERE = require("Core.Here")(...)
local T = require("Locale.i18n").texts("vault", HERE)   -- HERE .. "lang/<code>/vault.json"
local name = T:tr("chest_1")
```

L'anglais (`lang/en/`) sert de repli pour une langue à laquelle il manque un fichier ou une clé. Un stage ou une activité globale fait de même sans l'argument de dossier : `texts(name)` lit le dossier `lang/` du stage lui-même.

## État partagé

Les textures, sons et chaînes de `SHARED` forment un seul espace de noms pour tout le jeu, modules du skin et modules globaux confondus. Les modules fournis utilisent quelques clés fixes, listées sur leurs pages (par exemple la chaîne partagée `ui_library` et les textures `myroom_thumb_<id>`). Préfixez vos propres clés pour qu'elles ne puissent pas entrer en collision. Les données de sauvegarde (déclencheurs, compteurs et chaînes) appartiennent au fichier de sauvegarde, pas à un module : c'est ainsi que la pagode, la chambre forte, la boutique et Ma Chambre se transmettent des objets.
