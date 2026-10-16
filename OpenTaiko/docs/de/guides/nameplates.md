<!-- guides/nameplates.md -->

# Namensschilder hinzufügen

Ein Namensschildtitel ist das Textschild, das auf dem Namensschild eines Spielers zusammen mit dem Spielernamen und dem Dan-Titel erscheint. Spieler schalten Titel frei, indem sie spielen, sie kaufen oder sie von einem Skript erhalten, und rüsten einen davon in My Room aus. Mit einem Namensschild-Paket können Sie eigene Titel hinzufügen: ein Ordner mit einer Datei `Nameplates.json`, den Sie in `Global/Nameplates/` ablegen. Ein Skin kann ein eigenes Paket mitliefern. Das Spiel listet die Namensschilder jedes Pakets neben den mitgelieferten auf, in jedem Bildschirm, der Namensschilder zeigt.

## Bevor Sie beginnen

- Ein Texteditor, der UTF-8 speichert.
- Eine Paket-ID: ein kurzer Name aus Kleinbuchstaben, Ziffern und `_`, den kein anderes Paket verwendet, zum Beispiel Ihr Name oder der Name Ihres Chart-Pakets. Das Spiel bildet daraus die ID jedes Namensschilds im Paket.
- Das Spiel liest die Pakete beim Start und beim Laden eines Skins. Starten Sie das Spiel nach jeder Änderung neu, um sie zu sehen.

## Schritt 1: Den Paketordner anlegen

Legen Sie im Ordner `Global/Nameplates/` neben dem Spiel einen Ordner an. Den Ordnernamen können Sie frei wählen. Das Paket enthält eine erforderliche Datei und einen optionalen Ordner `Lang/` für Übersetzungen (Schritt 3):

```
Global/
  Nameplates/
    Example Pack/
      Nameplates.json
      Lang/
        fr.json
        ja.json
```

Ein Skin liefert sein Paket auf dieselbe Weise mit, als Ordner `Nameplates/` in der Skin-Wurzel (`System/<skin>/Nameplates/Nameplates.json`). Das Spiel lädt nur das Paket des aktiven Skins, sodass die Namensschilder eines Skin-Pakets verschwinden, solange ein anderer Skin aktiv ist.

## Schritt 2: Nameplates.json schreiben

`Nameplates.json` enthält die Paket-ID und die Liste der Namensschilder:

- `pack` (string, erforderlich): die Paket-ID.
- `name` (object, optional): der Name des Pakets, den die Paketleiste des PCs in My Room anzeigt, als `{ "strings": { "default": "...", "<code>": "..." } }`: Die Paketleiste zeigt den Namen der aktuellen Sprache, sonst `default`. Ohne ihn zeigt die Paketleiste die Paket-ID.
- `nameplates` (array, erforderlich): ein Objekt pro Namensschild, mit diesen Feldern:
  - `key` (string, erforderlich): ein Name für das Namensschild, eindeutig innerhalb des Pakets. Die vollständige ID des Namensschilds ist `<pack>:<key>`, zum Beispiel `example_pack:night_sky`. Spielstände speichern diese ID; behalten Sie einen Schlüssel also bei, sobald Spieler das Namensschild besitzen können, und geben Sie ihn nie einem anderen Namensschild.
  - `title` (string, erforderlich): der Titeltext. Jede Sprache ohne Übersetzung zeigt ihn an.
  - `type` (int, Standard 0): die Grafik des Schilds, eine Ordnernummer in den Namensschild-Texturen des Skins. Im mitgelieferten Skin sind die Ordner `Modules/ROActivities/nameplate/Textures/Title/0` bis `45`, und für eine Nummer ohne Ordner zeichnet der Skin kein Schild.
  - `rarity` (string, Standard `Common`): der Name der Seltenheit (siehe Tabelle unten). Ein unbekannter Name wird zu `Common`.
  - `unlock` (object, optional): die Freischaltbedingung, mit denselben Feldern wie in einer `Unlock.json`: `condition`, `values`, `type` und `references` (die Bedingungs-IDs stehen in der Anleitung [Chart-Freischaltungen](unlockables.md)). Ohne `unlock` besitzt jeder Spieler das Namensschild von Anfang an.

Seltenheitswerte (die Sterne sind die des mitgelieferten Skins):

| Seltenheit | Sterne auf dem Schild | Benachrichtigungsstufe |
| --- | --- | --- |
| <span class="rarity rarity-poor">Poor</span> | 0 | 0 |
| <span class="rarity rarity-common">Common</span> | 0 | 0 |
| <span class="rarity rarity-uncommon">Uncommon</span> | 0 | 1 |
| <span class="rarity rarity-rare">Rare</span> | 1 | 2 |
| <span class="rarity rarity-epic">Epic</span> | 2 | 3 |
| <span class="rarity rarity-legendary">Legendary</span> | 3 | 4 |
| <span class="rarity rarity-mythical">Mythical</span> | 4 | 4 |

Das Spiel prüft die Freischaltbedingungen nach jedem Spiel auf dem Ergebnisbildschirm. Erfüllt der Spieler eine, schaltet das Spiel das Namensschild frei und zeigt eine Benachrichtigung an. Münzbedingungen (`ch`, `cs`, `cm`) schalten nur durch einen Kauf frei: Im mitgelieferten Skin kauft der Spieler das Namensschild am PC in My Room. Der PC in My Room lässt außerdem gesperrte Mythical-Namensschilder aus seiner Liste, bis der Spieler sie besitzt.

```json
{
  "pack": "example_pack",
  "name": { "strings": { "default": "Example Pack", "fr": "Pack d'exemple" } },
  "nameplates": [
    {
      "key": "night_sky",
      "title": "Night Sky Walker",
      "type": 8,
      "rarity": "Rare",
      "unlock": { "condition": "tp", "values": [50] }
    },
    {
      "key": "hello",
      "title": "Hello There"
    }
  ]
}
```

## Schritt 3: Übersetzungen hinzufügen

Übersetzungen sind optional. Jede Datei im Ordner `Lang/` des Pakets enthält die Titel einer Sprache, eine Zeile pro Namensschild, mit den Schlüsseln des Pakets (ohne die Paket-ID). Der Dateiname ist der Sprachcode:

| Datei | Sprache |
| --- | --- |
| `de.json` | Deutsch |
| `en.json` | Englisch |
| `es.json` | Spanisch |
| `fr.json` | Französisch |
| `ja.json` | Japanisch |
| `ko.json` | Koreanisch |
| `nl.json` | Niederländisch |
| `ru.json` | Russisch |
| `zh.json` | Chinesisch |

```json
{
  "night_sky": "Promeneur du ciel nocturne",
  "hello": "Bonjour"
}
```

Ein Namensschild, das in der Datei einer Sprache fehlt, zeigt in dieser Sprache seinen `title`.

## Schritt 4: Im Spiel testen

Starten Sie das Spiel und öffnen Sie den PC in My Room. Wählen Sie im Reiter Namensschilder Ihr Paket in der Paketleiste über der Liste: Die Liste zeigt die Namensschilder des Pakets, die gesperrten mit ihrem Freischalthinweis. Rüsten Sie eines aus und prüfen Sie Titel, Schild und Sterne auf dem Namensschild des Spielers. Wechseln Sie die Spielsprache, um die Übersetzungen zu prüfen.

Das Spiel überspringt ein Paket, das es nicht lesen kann, und zeigt eine Fehlerbenachrichtigung an, die das Paket nennt. Ein Paket ohne gültige `pack`-ID oder mit der ID eines Pakets, das das Spiel bereits geladen hat, gilt als unlesbar. Ein einzelnes Namensschild ohne `key` oder `title` oder mit einem Schlüssel, der im Paket bereits existiert, überspringt das Spiel und schreibt den Grund in `OpenTaiko.log`.

## Namensschilder aus einem Skript

Lua-Skripte arbeiten mit denselben IDs. Jede Methode, die eine Namensschild-ID nimmt, akzeptiert die vollständige ID als String; für die mitgelieferten Namensschilder akzeptiert sie auch deren Nummer. Ein Skript kann ein Namensschild mit der Bedingung `ig` vergeben, das sich durch Spielen nie freischaltet:

```lua
local save = GetSaveFile(0)
local id = "example_pack:night_sky"
if not save:IsNameplateUnlocked(id) then
    save:UnlockNameplate(id)
end
save:ChangeNameplate(id)
```

UnlockNameplate zeigt keine Benachrichtigung an; ein Skript, das eine möchte, zeigt sie selbst an. Die Namensschild-Methoden stehen unter [Spieler und Profile](../api/players.md).

## Beispiele

Diese Pakete zeigen häufige Muster. Ersetzen Sie die IDs, Titel und Zahlen durch Ihre eigenen.

**Ein kostenloses Namensschild.** Ohne `unlock` besitzt jeder Spieler das Namensschild und kann es sofort ausrüsten.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "welcome", "title": "Welcome Aboard", "type": 0, "rarity": "Common" }
  ]
}
```

**Durch Spielen freischalten (`tp`).** Das Namensschild schaltet sich nach 100 Spielen frei. `ap` (KI-Kampf-Spiele), `aw` (KI-Kampf-Siege) und `ce` (insgesamt verdiente Münzen) verwenden dasselbe Layout mit einem einzelnen Wert.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "regular", "title": "Regular Drummer", "type": 3, "rarity": "Uncommon",
      "unlock": { "condition": "tp", "values": [100] } }
  ]
}
```

**Einen Song Ihres Chart-Pakets schaffen (`sp`).** Ein Chart-Paket kann das Schaffen seiner eigenen Songs belohnen. Die Referenz ist das Feld `id` der `uniqueID.json` des Songs, und `[3, 3]` verlangt eine Full Combo (3) auf Extreme (3).

```json
{
  "pack": "example_charts",
  "nameplates": [
    { "key": "storm_fc", "title": "Storm Chaser", "type": 33, "rarity": "Epic",
      "unlock": { "condition": "sp", "values": [3, 3],
                  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"] } }
  ]
}
```

**Mit Münzen kaufen (`ch`).** Der Preis ist der einzige Wert. Im mitgelieferten Skin kauft der Spieler das Namensschild am PC in My Room.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "golden", "title": "Golden Touch", "type": 11, "rarity": "Rare",
      "unlock": { "condition": "ch", "values": [800] } }
  ]
}
```

**Eine Belohnung, die nur ein Skript vergibt (`ig`).** Spielen schaltet ein `ig`-Namensschild nie frei, daher vergibt es ein Skript mit UnlockNameplate (siehe Namensschilder aus einem Skript). Bis dahin zeigt der PC in My Room dafür den allgemeinen Hinweis für eine ungültige Bedingung.

```json
{
  "pack": "example_story",
  "nameplates": [
    { "key": "finale", "title": "Curtain Call", "type": 19, "rarity": "Legendary",
      "unlock": { "condition": "ig", "values": [] } }
  ]
}
```

**Ein Skin-Paket mit Übersetzungen.** Ein Skin legt sein Paket in `System/<skin>/Nameplates/` ab, seine Übersetzungen in `System/<skin>/Nameplates/Lang/`.

```json
{
  "pack": "example_skin",
  "nameplates": [
    { "key": "visitor", "title": "First Visit", "type": 22, "rarity": "Common",
      "unlock": { "condition": "tp", "values": [1] } }
  ]
}
```

```json
{
  "visitor": "初めての訪問"
}
```

## Fehlerbehebung und Hinweise

- Das Paket erscheint nicht: Der Ordner liegt nicht direkt in `Global/Nameplates/`, die Datei heißt nicht `Nameplates.json`, die `pack`-ID fehlt oder enthält andere Zeichen als Kleinbuchstaben, Ziffern und `_`, oder ein anderes Paket verwendet bereits dieselbe ID. Die Fehlerbenachrichtigung nennt das Paket.
- Ein Namensschild fehlt in der Liste: Es hat keinen `key` oder keinen `title`, oder sein Schlüssel wiederholt einen früheren. `OpenTaiko.log` nennt den Eintrag.
- Eine Übersetzung erscheint nicht: Der Schlüssel in der `Lang`-Datei muss der Schlüssel des Pakets ohne die Paket-ID sein (`night_sky` für `example_pack:night_sky`), und der Dateiname muss der Sprachcode sein.
- Das Schild zeigt keine Grafik: Der Skin hat keinen Ordner für diesen `type`.
- Online sehen andere Spieler den Titel eines Paket-Namensschilds nur, wenn sie dasselbe Paket haben. Andernfalls sehen sie das Namensschild des Spielers ohne Titel.
- Wenn Sie ein Paket entfernen, zeigen Spieler, die eines seiner Namensschilder trugen, keinen Titel, bis das Paket zurück ist. Ihre Freischaltungen bleiben im Spielstand.
- Der JSON-Leser des Spiels akzeptiert Kommentare und nachgestellte Kommas; strenge Validatoren lehnen sie ab.
