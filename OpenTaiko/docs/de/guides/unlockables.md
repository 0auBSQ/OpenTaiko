<!-- guides/unlockables.md -->

# Freischaltbedingungen für Charts

Sie können einen eigenen Song hinter einer Bedingung sperren (ein Münzpreis, das Schaffen anderer Songs, eine Gesamtspielanzahl, ein Story-Flag und so weiter), indem Sie eine `Unlock.json`-Datei im Ordner des Songs neben seiner `.tja` und `uniqueID.json` ablegen. Wenn das Spiel die Songliste aufbaut, liest es diese Datei und hält den Song gesperrt, bis der Spieler die Bedingung erfüllt. Bedingungen gelten für einzelne Songs: `box.def` hat keinen Freischaltschlüssel, sodass Sie einen Genre-Ordner auf diese Weise nicht sperren können.

## Bevor Sie beginnen

- Ein eigenes Chart, das bereits in der Songliste erscheint: ein Ordner unter `Songs/` mit einer `.tja` und, sobald das Spiel ihn gescannt hat, einer `uniqueID.json`.
- Ein Texteditor, der UTF-8 speichert.
- Für Bedingungen, die auf andere Songs verweisen: der `id`-Wert aus der `uniqueID.json` jedes referenzierten Songs.
- Das Spiel speichert den Freischaltfortschritt pro Spielstand, sodass Sie die Bedingung im Spiel erfüllen müssen, um die Freischaltung des Songs zu sehen. Die Option `Ignore Song Unlockables` in den Spieleinstellungen behandelt jeden Song als freigeschaltet, während Sie testen.

## Schritt 1: Die Datei Unlock.json anlegen

Legen Sie `Unlock.json` im Songordner an. Jedes Feld ist optional und hat einen Standardwert:

- `hidden_index` (int, Standard 0): wie die Songliste den gesperrten Song darstellt (siehe Tabelle unten). Das Spiel begrenzt Werte auf 0-3.
- `rarity` (string, Standard `Common`): der Name der Seltenheit (siehe Tabelle unten). Sie legt die Farbe der Seltenheitsanzeige und die Stufe der Freischaltbenachrichtigung fest. Ein leerer Wert wird zu `Common`.
- `condition` (string, Standard `ch`): die Bedingungs-ID (Schritt 2).
- `values` (int array, Standard `[100]`): die numerischen Parameter der Bedingung.
- `type` (string, Standard `me`): der Vergleich, den wertbasierte Bedingungen verwenden: `l` kleiner als, `le` kleiner oder gleich, `e` gleich, `me` größer oder gleich, `m` größer als, `d` ungleich. Münzbedingungen verwenden immer `me`.
- `references` (string array, Standard `[""]`): Song-IDs, Genrenamen, Chart-Autorennamen oder Flag-Namen, je nach Bedingung.
- `custom_unlock_text` (object, optional): ersetzt den generierten Hinweistext. Ein lokalisiertes Objekt `{ "strings": { "default": "...", "ja": "..." } }`; das Spiel verwendet den Schlüssel der aktiven Sprache, dann `default`, und zeigt den generierten Text an, wenn keiner von beiden existiert.

Die Schlüssel sind kleingeschrieben mit Unterstrichen, genau wie aufgeführt.

Werte für den Hidden-Index:

| Wert | In der Songliste |
| --- | --- |
| 0 | Angezeigt mit Schlosssymbol; die Audiovorschau wird abgespielt |
| 1 | Ausgegraut; keine Vorschau |
| 2 | Ausgegraut, mit verschleiertem Titel und Vorschaubild |
| 3 | Versteckt bis zur Freischaltung |

Seltenheitswerte:

| Seltenheit | Benachrichtigungsstufe |
| --- | --- |
| <span class="rarity rarity-poor">Poor</span> | 0 |
| <span class="rarity rarity-common">Common</span> | 0 |
| <span class="rarity rarity-uncommon">Uncommon</span> | 1 |
| <span class="rarity rarity-rare">Rare</span> | 2 |
| <span class="rarity rarity-epic">Epic</span> | 3 |
| <span class="rarity rarity-legendary">Legendary</span> | 4 |
| <span class="rarity rarity-mythical">Mythical</span> | 4 |

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "cm",
  "values": [500],
  "type": "me",
  "references": [""],
  "custom_unlock_text": {
    "strings": {
      "default": "Buy this song for 500 coins.",
      "ja": "500コインで解放できます。"
    }
  }
}
```

## Schritt 2: Eine Bedingung wählen

| ID | Bedeutung | `values` | `references` |
|----|---------|----------|--------------|
| `ch`, `cs`, `cm` | Münzkauf. Das Spiel wertet die drei IDs identisch aus (Preis in Münzen, `type` auf `me` erzwungen, nie automatisch gewährt). `cm` ist die für Songs vorgesehene ID; Skin-Skripte können die ID lesen und damit entscheiden, wo sie den Kauf anbieten. | `[price]` | unbenutzt |
| `ce` | Insgesamt seit Erstellung des Spielstands verdiente Münzen | `[coins]` | unbenutzt |
| `tp` | Gesamtspielanzahl | `[plays]` | unbenutzt |
| `ap` | Anzahl der KI-Kampf-Spiele | `[plays]` | unbenutzt |
| `aw` | Anzahl der KI-Kampf-Siege | `[wins]` | unbenutzt |
| `sd` | Verschiedene Charts, die einen Clear-Status erreicht haben | `[chart count, clear status]` | unbenutzt |
| `dp` | Charts eines Schwierigkeitsgrads, die einen Clear-Status erreicht haben | `[difficulty, clear status, chart count]` | unbenutzt |
| `lp` | Charts eines Sternelevels, die einen Clear-Status erreicht haben | `[level, clear status, chart count]` | unbenutzt |
| `sp` | Bestimmte Songs, die einen Clear-Status erreicht haben | `[difficulty, clear status]` pro Song (`-1` = beliebiger Schwierigkeitsgrad) | eine Song-ID pro Paar |
| `sg` | Songs in benannten Genres, die einen Clear-Status erreicht haben | `[song count, clear status]` pro Genre | ein Genrename pro Paar |
| `sc` | Charts benannter Chart-Autoren, die einen Clear-Status erreicht haben | `[chart count, clear status]` pro Chart-Autor | ein Chart-Autorenname pro Paar |
| `gt` | Globaler Trigger (ein benanntes Ein/Aus-Flag im Spielstand, das Skripte setzen) | `[1]` für EIN, `[0]` für AUS | `[trigger name]` |
| `gc` | Globaler Zähler (eine benannte Zahl im Spielstand, die Skripte setzen) | `[value]`, verglichen mit `type` | `[counter name]` |
| `ig` | Unerreichbar: schaltet nie frei | keine | keine |
| `andcomb` | Der Spieler muss alle Kindbedingungen erfüllen (siehe die kombinierten Beispiele) | `[]` | eine Kindbedingung pro Eintrag, als JSON-String |
| `orcomb` | Der Spieler muss mindestens eine Kindbedingung erfüllen (siehe die kombinierten Beispiele) | `[]` | eine Kindbedingung pro Eintrag, als JSON-String |

Clear-Status-Werte (eine Statusanforderung bedeutet diesen Status oder besser):

| Wert | Clear-Status |
| --- | --- |
| 0 | gespielt |
| 1 | assistierter Clear |
| 2 | Clear |
| 3 | Full Combo |
| 4 | All Perfect |

Schwierigkeitswerte:

| Wert | Schwierigkeitsgrad |
| --- | --- |
| 0 | Easy |
| 1 | Normal |
| 2 | Hard |
| 3 | Extreme |
| 4 | Extra Extreme |

Bei `dp` zählt Schwierigkeitsgrad 3 auch Extra-Extreme-Charts. `dp` und `lp` zählen nur reguläre Charts und überspringen Dan- und Tower-Charts. `sp` akzeptiert `-1` für einen beliebigen Schwierigkeitsgrad.

Das Spiel prüft die Anzahl der Werte: `ch`/`cs`/`cm`/`ce`/`tp`/`ap`/`aw`/`gt`/`gc` brauchen genau 1 Wert, `sd` genau 2, `dp`/`lp` genau 3, und `sp`/`sg`/`sc` brauchen 2 Werte pro Referenz mit so vielen Referenzen wie Paaren. Eine falsche Anzahl lässt die Bedingung mit einer Fehlermeldung im Spiel scheitern.

Nur ein expliziter Kauf in einem Bildschirm, der ihn anbietet, schaltet eine Münzbedingung frei (der mitgelieferte Skin bietet gesperrte Songs in der Songauswahl zum Kauf an, unabhängig davon, welche Münz-ID Sie verwenden). Alle anderen Bedingungen prüft das Spiel nach jedem Spiel auf dem Ergebnisbildschirm; erfüllt der Spieler eine, fügt das Spiel die Song-ID zum Spielstand hinzu und zeigt eine Benachrichtigung an.

## Schritt 3: Im Spiel testen

Starten Sie das Spiel und öffnen Sie die Songauswahl. Je nach `hidden_index` zeigt der Song ein Schloss, ist ausgegraut, verschleiert oder abwesend. Kaufen Sie ihn oder erfüllen Sie die Bedingung und bestätigen Sie dann, dass er nach einem Neustart freigeschaltet bleibt. Aktivieren Sie `Ignore Song Unlockables` in den Spieleinstellungen, um jede Sperre zu umgehen, während Sie iterieren.

## Beispiele

Dies sind die Bedingungsmuster, die die mitgelieferten Songs verwenden, geschrieben als `Unlock.json`-Dateien. Ersetzen Sie die IDs, Namen und Zahlen durch Ihre eigenen.

**Den Song mit Münzen kaufen (`cm`).** Das häufigste Muster. Der Preis ist der einzige Wert, und das Spiel ignoriert `type`. `hidden_index` 0 hält den Song sichtbar, damit der Spieler ihn finden und kaufen kann. Der generierte Hinweis nennt bereits den Preis, sodass Sie keinen `custom_unlock_text` brauchen.

```json
{
  "hidden_index": 0,
  "rarity": "Uncommon",
  "condition": "cm",
  "values": [500]
}
```

**Gesamtspielanzahl (`tp`).** Die mitgelieferten Kapitel öffnen ihre Songs nacheinander mit steigenden Spielanzahlen (10, 15, 20 und so weiter), sodass der Spieler immer einen nächsten Song in Reichweite hat. `ap` (KI-Kampf-Spiele), `aw` (KI-Kampf-Siege) und `ce` (insgesamt verdiente Münzen) verwenden dasselbe Layout mit einem einzelnen Wert.

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "tp",
  "values": [50]
}
```

**Einen bestimmten Song schaffen (`sp`).** Das Muster, das die meisten mitgelieferten Songs für eine Fortsetzung oder einen Remix verwenden. `-1` als Schwierigkeitsgrad akzeptiert einen Clear auf jedem Schwierigkeitsgrad; die ID ist das Feld `id` der `uniqueID.json` des referenzierten Songs (das Spiel erzeugt eine 64-stellige ID, wenn es einen Song ohne ID zum ersten Mal scannt, und mitgelieferte Songs können eine handgeschriebene ID tragen). Das Beispiel schaltet frei, nachdem der Spieler den referenzierten Song geschafft hat.

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "sp",
  "values": [-1, 2],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"]
}
```

**Mehrere Songs mit Full Combo schaffen (`sp`).** Jeder weitere Song fügt ein Wertepaar und eine Referenz hinzu, und der Spieler muss die Anforderung für jeden referenzierten Song erfüllen. Das Beispiel verlangt eine Full Combo (3) bei zwei Songs.

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sp",
  "values": [-1, 3, -1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU", "4aJ2I19XyEG2cEA9tlmdnZSz2H43OKsVBPLi52UfRhSjDGNgTGGqhmqkbWfPDoyw"]
}
```

**Songs eines Genres schaffen (`sg`).** Jeder Eintrag ist `[song count, clear status]` mit dem Genrenamen in `references`. Das Genre ist dasjenige, das das Spiel festhält, wenn der Spieler den Song spielt: das `#GENRE` der `box.def` des umschließenden Ordners, oder das eigene `#GENRE` des Charts, wenn der Ordner keines hat. Die mitgelieferten Kapitel verwenden dies für ihre Medleys. Das Beispiel schaltet frei, nachdem der Spieler 10 verschiedene Songs des Genres geschafft hat.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sg",
  "values": [10, 2],
  "references": ["OpenTaiko Chapter II"]
}
```

**Charts eines Chart-Autors schaffen (`sc`).** Dasselbe Layout mit `#NOTESDESIGNER`-Namen als Referenzen. Das Beispiel verlangt 5 geschaffte Charts eines Chart-Autors; ein zweiter Chart-Autor fügt ein weiteres Wertepaar und eine weitere Referenz hinzu.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sc",
  "values": [5, 2],
  "references": ["bol"]
}
```

**Eine Anzahl verschiedener Charts schaffen (`sd`).** Zählt jedes Chart, das der Spielstand mit dem angegebenen Status oder besser enthält. Das Beispiel verlangt 10 Full Combos.

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sd",
  "values": [10, 3]
}
```

**Charts eines Schwierigkeitsgrads oder Sternelevels schaffen (`dp` / `lp`).** `dp` zählt Charts eines Schwierigkeitsgrads, `lp` Charts eines Sternelevels. Das erste Beispiel verlangt 20 geschaffte Extreme-Charts, das zweite ein geschafftes 7-Sterne-Chart.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "dp",
  "values": [3, 2, 20]
}
```

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "lp",
  "values": [7, 2, 1]
}
```

**Eine von zwei Bedingungen (`orcomb`).** `values` ist leer, und jeder Eintrag von `references` ist eine vollständige Kindbedingung (`condition`, `type`, `values`, `references`), geschrieben als JSON-String. Die mitgelieferten Songs bieten damit eine Abkürzung an: genug Songs spielen oder bezahlen. `orcomb` verwendet den günstigsten erfüllten Zweig, wenn es den Preis für einen Kauf berechnet.

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "orcomb",
  "values": [],
  "references": [
    "{\"condition\": \"tp\", \"type\": \"me\", \"values\": [100]}",
    "{\"condition\": \"cm\", \"type\": \"me\", \"values\": [200]}"
  ]
}
```

**Alle von mehreren Bedingungen (`andcomb`).** Dasselbe Layout; der Spieler muss jede Kindbedingung erfüllen. Kinder können selbst Kombinationen sein, und `andcomb` addiert die Münzpreise innerhalb einer Kombination. Das Beispiel verlangt einen Clear eines Songs und einen Clear eines beliebigen 7-Sterne-Charts.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "andcomb",
  "values": [],
  "references": [
    "{\"condition\": \"sp\", \"type\": \"me\", \"values\": [-1, 2], \"references\": [\"4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU\"]}",
    "{\"condition\": \"lp\", \"type\": \"me\", \"values\": [7, 2, 1], \"references\": [\"\"]}"
  ]
}
```

**Story-Flag mit eigenem Hinweis (`gt`).** `gt` liest ein benanntes Ein/Aus-Flag aus dem Spielstand. Lua-Skripte (Story-Szenen, Zwischensequenzen) setzen die Flags; verwenden Sie `gt` also nur, wenn ein von Ihnen kontrolliertes Skript das Flag setzt. `values` ist `[1]` für EIN oder `[0]` für AUS; `references` enthält den Flag-Namen. Eine Story-Freischaltung hat keinen selbsterklärenden generierten Hinweis, weshalb `custom_unlock_text` hier nützlich ist. Sie können jeden Sprachschlüssel angeben; `default` ist der Rückfall.

```json
{
  "hidden_index": 3,
  "rarity": "Legendary",
  "condition": "gt",
  "values": [1],
  "references": ["story_done"],
  "custom_unlock_text": {
    "strings": {
      "default": "Finish the story to unlock this song.",
      "ja": "ストーリーをクリアするとこの曲が解放されます。"
    }
  }
}
```

**Geheimer Song mit einem Rätsel.** Die mitgelieferten geheimen Songs kombinieren einen hohen `hidden_index` (Titel und Vorschau bleiben verschleiert, oder der Song bleibt bis zur Freischaltung abwesend) mit einem `custom_unlock_text`, der die Bedingung andeutet, statt sie zu nennen. Darunter funktioniert jede Bedingung; das Beispiel versteckt eine Full-Combo-Anforderung hinter einem Rätsel.

```json
{
  "hidden_index": 2,
  "rarity": "Epic",
  "condition": "sp",
  "values": [-1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"],
  "custom_unlock_text": {
    "strings": {
      "default": "Perfect the storm that came before this one."
    }
  }
}
```

## Fehlerbehebung und Hinweise

- Der Song ist nicht gesperrt: Die Datei heißt nicht `Unlock.json`, sie liegt nicht in dem Ordner, der die `.tja` und `uniqueID.json` enthält, oder `Ignore Song Unlockables` ist aktiv.
- Der Hinweis meldet, die Bedingung sei ungültig: Die Anzahl der Werte passt nicht zur Bedingung (siehe Schritt 2), oder `sp`/`sg`/`sc` haben eine andere Anzahl Referenzen als Wertepaare.
- Ein Schlüssel hat keine Wirkung: Die Schlüssel sind `hidden_index`, `rarity`, `condition`, `values`, `type`, `references`, `custom_unlock_text`, alle kleingeschrieben. Alles andere ignoriert das Spiel und wendet den Standard an.
- `sp` schaltet nie frei: Die Referenz muss die `uniqueID.json`-ID des Zielsongs sein (ein Titel oder Dateiname passt nie), und der Spieler muss Schwierigkeitsgrad und Status erreichen können (Status 3 ist eine Full Combo, sodass ein Chart, das der Spieler nur geschafft hat, nicht zählt).
- `sg` schaltet nie frei: Der Genrename muss dem Genre entsprechen, das das Spiel für die gespielten Songs festhält (das `#GENRE` der `box.def` des Ordners, oder das `#GENRE` des Charts, wenn es kein Ordner-Genre gibt).
- `gt`/`gc` schalten nie frei: Nichts setzt das benannte Flag. Das Chart kann es nicht selbst setzen.
- Nichts schaltet `ig` im Spiel frei. Verwenden Sie es nur für Inhalte, die ein anderes System gewährt.
- `custom_unlock_text` muss ein `{ "strings": { ... } }`-Objekt sein; ein bloßer String funktioniert nicht.
- Die mitgelieferten `Unlock.json`-Dateien enthalten nachgestellte Kommas. Der JSON-Parser des Spiels akzeptiert sie; strenge Validatoren lehnen sie ab.
