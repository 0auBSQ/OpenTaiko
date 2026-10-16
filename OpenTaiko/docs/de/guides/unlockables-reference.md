<!-- guides/unlockables-reference.md -->

# Referenz der Freischaltbedingungen

Songs, Charaktere, Puchicharas und Namensschilder verwenden dasselbe Format für Freischaltbedingungen. Songs, Charaktere und Puchicharas speichern es in einer `Unlock.json`-Datei, Namensschilder im `unlock`-Objekt ihres Eintrags in `Nameplates.json`. Die Anleitungen erklären, wohin jede Datei gehört: [Chart-Freischaltungen](unlockables.md), [Charaktere hinzufügen](characters.md), [Puchicharas hinzufügen](puchicharas.md) und [Namensschilder hinzufügen](nameplates.md). Diese Seite listet die Felder, die Bedingungs-IDs und die Zahlen, die sie verwenden.

## Bedingungsfelder

| Feld | Typ | Bedeutung |
| --- | --- | --- |
| `condition` | string | Die Bedingungs-ID (siehe Bedingungs-IDs). |
| `values` | int array | Die numerischen Parameter der Bedingung. |
| `type` | string | Der Vergleich, den wertbasierte Bedingungen verwenden (siehe Vergleichsarten). |
| `references` | string array | Song-IDs, Genrenamen, Chart-Autorennamen oder Flag-Namen, je nach Bedingung. |

Die `Unlock.json` eines Songs ergänzt eigene Felder (`hidden_index`, `rarity`, `custom_unlock_text`), die die Anleitung [Chart-Freischaltungen](unlockables.md) beschreibt.

## Bedingungs-IDs

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
| `andcomb` | Der Spieler muss alle Kindbedingungen erfüllen (siehe die kombinierten Beispiele der Anleitung [Chart-Freischaltungen](unlockables.md)) | `[]` | eine Kindbedingung pro Eintrag, als JSON-String |
| `orcomb` | Der Spieler muss mindestens eine Kindbedingung erfüllen (siehe die kombinierten Beispiele der Anleitung [Chart-Freischaltungen](unlockables.md)) | `[]` | eine Kindbedingung pro Eintrag, als JSON-String |

Das Spiel prüft die Anzahl der Werte: `ch`/`cs`/`cm`/`ce`/`tp`/`ap`/`aw`/`gt`/`gc` brauchen genau 1 Wert, `sd` genau 2, `dp`/`lp` genau 3, und `sp`/`sg`/`sc` brauchen 2 Werte pro Referenz mit so vielen Referenzen wie Paaren. Eine falsche Anzahl lässt die Bedingung mit einer Fehlermeldung im Spiel scheitern.

Nur ein expliziter Kauf in einem Bildschirm, der ihn anbietet, schaltet eine Münzbedingung frei. Alle anderen Bedingungen prüft das Spiel nach jedem Spiel auf dem Ergebnisbildschirm; erfüllt der Spieler eine, schaltet das Spiel den Gegenstand frei und zeigt eine Benachrichtigung an.

## Vergleichsarten

| `type` | Vergleich |
| --- | --- |
| `l` | kleiner als |
| `le` | kleiner oder gleich |
| `e` | gleich |
| `me` | größer oder gleich (Standard) |
| `m` | größer als |
| `d` | ungleich |

Münzbedingungen verwenden immer `me`, unabhängig davon, was `type` angibt.

## Clear-Status-Werte

Eine Statusanforderung bedeutet diesen Status oder besser.

| Wert | Clear-Status |
| --- | --- |
| 0 | gespielt |
| 1 | assistierter Clear |
| 2 | Clear |
| 3 | Full Combo |
| 4 | All Perfect |

## Schwierigkeitswerte

| Wert | Schwierigkeitsgrad |
| --- | --- |
| 0 | Easy |
| 1 | Normal |
| 2 | Hard |
| 3 | Extreme |
| 4 | Extra Extreme |

Bei `dp` zählt Schwierigkeitsgrad 3 auch Extra-Extreme-Charts. `dp` und `lp` zählen nur reguläre Charts und überspringen Dan- und Tower-Charts. `sp` akzeptiert `-1` für einen beliebigen Schwierigkeitsgrad.

## Der Wert der Spiel-Mods

Der Spielstand speichert die besten Spiele eines Spielers pro Chart, Schwierigkeitsgrad und Spiel-Mods. Der Wert der Spiel-Mods packt die Mods eines Spiels in 8 Bytes, beginnend mit dem niedrigsten Byte: Ein Spiel ohne Mods ergibt 8925478921.

| Byte | Mod | Werte |
| --- | --- | --- |
| 1 (niedrigstes) | Scrollgeschwindigkeit | 0 bis 99: 9 ist 1,0x, jeder Schritt ist 0,1x |
| 2 | Stealth | 0 Off, 1 Doron, 2 Stealth, 3 Hidden, 4 Flashlight |
| 3 | Random | 0 Off, 1 Random, 2 Mirror, 3 SuperRandom, 4 MirrorRandom |
| 4 | Songgeschwindigkeit | 2 bis 200: 20 ist 1,0x, jeder Schritt ist 0,05x |
| 5 | Trefferfenster | 0 Loose, 1 Lenient, 2 Normal, 3 Strict, 4 Rigorous |
| 6 | Wertungs-Mod | 0 aus, 1 Just, 2 Safe |
| 7 | Unbenutzt | 0 |
| 8 (höchstes) | Fun-Mod | 0 None, 1 Avalanche, 2 Minesweeper, 3 Dynamic Beat |

Spezial-Mods (Flawless, Timed) sind nicht Teil des Werts.
