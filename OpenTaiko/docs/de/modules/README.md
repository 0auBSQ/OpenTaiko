<!-- modules/README.md -->

# Bereitgestellte Module

OpenTaiko liefert eine Bibliothek von Lua-Modulen mit, die jeder Skin verwenden kann: Hilfsmodule (Easing, Farben, JSON, Eingabe, Lokalisierung), eine UI-Bibliothek, ein Dialogfenster, die 3D-Welt-Engine sowie die Regeln und Daten der Pagode, des Geheimtresors und des Münzladens. Sie liegen im Spielordner, nicht in einem Skin, sodass ein neuer Skin sie erhält, ohne etwas zu kopieren. Dieser Abschnitt beschreibt jedes von ihnen; die [API-Referenz](../api/README.md) behandelt die globalen Objekte der Engine, auf denen sie aufbauen.

## Global/Modules

`Global/Modules/` im Installationsordner des Spiels hat dieselben Unterordner wie das `Modules/` eines Skins: `Transitions/`, `Stages/`, `Activities/`, `ROActivities/` und `Lib/`. Das Spiel lädt jede globale Kategorie direkt nach der des Skins, für den jeweils aktiven Skin. Die Module auf diesen Seiten sind die Dateien von `Global/Modules/Lib/`, die Sie mit `require` erreichen; `Global/Modules/Stages/` enthält außerdem Stages, die jeder Skin öffnen kann, etwa [Mein Zimmer](my-room.md).

Der Ordner kommt mit dem Spiel-Build und wird bei jedem Update ersetzt, daher sollte ein Skin keine Dateien darin ablegen. Um ein bereitgestelltes Modul zu ändern, kopieren Sie es unter einem anderen Namen in Ihren eigenen Skin.

## Die require-Reihenfolge

`require` wandelt Punkte in Ordner um und ignoriert die Groß- und Kleinschreibung, sodass `require("Core.Easing")` die Datei `Core/Easing.lua` lädt. Welche Ordner es durchsucht, hängt vom laufenden Modul ab:

| Aufrufer | Durchsucht, in dieser Reihenfolge |
| --- | --- |
| Ein Modul des Skins (`System/<skin>/Modules/...`) | sein eigener Ordner, das `Modules/Lib/` des Skins, dann `Global/Modules/Lib/` |
| Ein globales Modul (`Global/Modules/...`) | sein eigener Ordner, `Global/Modules/Lib/`, dann das `Modules/Lib/` des Skins |

Eine Lib-Datei hat keinen eigenen Lua-Zustand: Sie läuft im Zustand des Moduls, das sie per require geladen hat, sodass die requires darin der Reihenfolge dieses Aufrufers folgen. Eine Datei im `Modules/Lib/` des Skins mit demselben Pfad wie ein bereitgestelltes Modul ersetzt dieses daher für jedes require aus den Modulen des Skins, auch für die requires innerhalb anderer bereitgestellter Module. Halten Sie Ihre eigenen Lib-Dateien aus den unten genannten Ordnernamen heraus, es sei denn, Sie wollen ein Modul ersetzen.

## Benennung

Jede Komponente ist ein Ordner von `Global/Modules/Lib/`, und ihre Module werden als `<Component>.<Module>` per require geladen:

| Komponente | Module | Seite |
| --- | --- | --- |
| `Core` | `Easing`, `Color`, `Util`, `Json`, `Here` | [Core](core.md) |
| `Input` | `NavInput` | [Input](input.md) |
| `Audio` | `MusicDuck` | [Audio](audio.md) |
| `Locale` | `i18n` | [Locale](locale.md) |
| `Calendar` | `almanac` | [Calendar](calendar.md) |
| `Songs` | `PlayStrings` | [Songs](songs.md) |
| `World` | `OWM3d` und seine Teile | [World](world.md) |
| `UI` | `SimpleUI`, `Pick` | [UI](ui.md) |
| `Saves` | `SavePicker` | [Saves](saves.md) |
| `Characters` | `Student` | [Characters](characters.md) |
| `VN` | `Dialogue` | [VN](vn.md) |
| `Pagoda` | `Levels`, `Floor`, `Rewards`, `Progress`, `Songs`, `Run`, `Texts` | [Pagoda](pagoda.md) |
| `Vault` | `Keys`, `Chests`, `Texts` | [Vault](vault.md) |
| `Shop` | `Daily`, `Catalog`, `Texts`, `Seasonal`, `Popup` | [Shop](shop.md) |

## Die Namen, die Open-World Memories beibehält

Der mitgelieferte Skin trug die meisten dieser Dateien früher in seinem eigenen `Modules/Lib/`. Jede alte Datei ist jetzt eine Weiterleitung aus zwei Zeilen, sodass ein Modul des Skins, das noch den alten Namen per require lädt, das globale Modul erhält, und beide Namen teilen sich eine Instanz (ein Zustand, ein Cache):

```lua
-- moved to Global/Modules/Lib/Core/Easing.lua
return require("Core.Easing")
```

| Alter Name | Bereitgestelltes Modul |
| --- | --- |
| `Easing`, `Color`, `Util` | `Core.Easing`, `Core.Color`, `Core.Util` |
| `NavInput`, `MusicDuck`, `PlayStrings` | `Input.NavInput`, `Audio.MusicDuck`, `Songs.PlayStrings` |
| `i18n`, `almanac` | `Locale.i18n`, `Calendar.almanac` |
| `OWM3d`, `OWM3d.<part>` | `World.OWM3d`, `World.OWM3d.<part>` |
| `PopUI.<part>` (der gemeinsame Code: `manager`, `widget`, `shape`, `util`, `cache`, `sfx`, jedes `widgets.*` außer `widgets.coins`) | `UI.SimpleUI.<part>` |
| `SavePicker`, `Student`, `dialogue` | `Saves.SavePicker`, `Characters.Student`, `VN.Dialogue` |

`PopUI` selbst bleibt ein Modul des Skins: Es ist SimpleUI im eigenen Aussehen des Skins (siehe [UI](ui.md)). Neuer Code sollte die bereitgestellten Namen per require laden.

## Assets werden gegen den Aufrufer aufgelöst

Relative Pfade, die an `TEXTURE`, `SOUND`, `VIDEO`, `JSONLOADER`, `SQL`, `DATABASE`, `STORAGE` und `SHARED` übergeben werden, werden gegen den Ordner des aufrufenden Moduls aufgelöst, nie gegen die Lib-Datei, die den Aufruf ausführt. Ein bereitgestelltes Modul, das eigene Dateien mitliefert (ein Bild, einen JSON-Pool, eine Datenbank), findet sie über `Core.Here`, das den eigenen Ordner der Datei zurückgibt, und lädt sie mit den Varianten für absolute Pfade:

```lua
-- Global/Modules/Lib/MyPart/Thing.lua
local HERE = require("Core.Here")(...)          -- ".../Global/Modules/Lib/MyPart/"

local icon = TEXTURE:CreateTextureFromAbsolutePath(HERE .. "icon.png")
local beep = SOUND:CreateSFXFromAbsolutePath(HERE .. "beep.ogg")
local data = JSONLOADER:JsonParseFileAny(HERE .. "data.json")
SHARED:SetSharedTextureUsingAbsolutePath("thing_icon", HERE .. "icon.png")
```

Übergeben Sie `...` auf der obersten Ebene der Datei: Dort übergibt `require` jeder Datei ihren Modulnamen und ihren absoluten Pfad. Ein relativer Pfad, den ein Aufrufer an ein bereitgestelltes Modul übergibt (eine Modelldatei für `World.OWM3d`, ein Sound für ein Widget), gehört dem Aufrufer und wird wie gewohnt gegen dessen Ordner aufgelöst.

## Strings liegen beim Modul

`THEME:GetSkinString` liest nur die `Locales/` des aktiven Skins, daher verwendet ein bereitgestelltes Modul es nie für seine eigenen Texte. Seine Strings liegen in einem Ordner `lang/` daneben, eine JSON-Datei pro Funktionsbereich und Sprache (`lang/en/vault.json`, `lang/ja/vault.json`, ...), gelesen über [Locale.i18n](locale.md) mit dem Ordner des Moduls:

```lua
local HERE = require("Core.Here")(...)
local T = require("Locale.i18n").texts("vault", HERE)   -- HERE .. "lang/<code>/vault.json"
local name = T:tr("chest_1")
```

Englisch (`lang/en/`) ist der Rückfall für eine Sprache, der eine Datei oder ein Schlüssel fehlt. Eine globale Stage oder Activity macht dasselbe ohne das Ordnerargument: `texts(name)` liest den Ordner `lang/` der Stage selbst.

## Gemeinsamer Zustand

Die Texturen, Sounds und Strings von `SHARED` bilden einen einzigen Namensraum für das ganze Spiel, für Module des Skins wie für globale Module. Die bereitgestellten Module verwenden einige feste Schlüssel, die auf ihren Seiten aufgeführt sind (zum Beispiel den gemeinsamen String `ui_library` und die Texturen `myroom_thumb_<id>`). Versehen Sie Ihre eigenen Schlüssel mit einem Präfix, damit sie nicht kollidieren können. Spielstanddaten (Trigger, Zähler und Strings) gehören zum Spielstand, nicht zu einem Modul; so reichen die Pagode, der Tresor, der Laden und Mein Zimmer einander Gegenstände weiter.
