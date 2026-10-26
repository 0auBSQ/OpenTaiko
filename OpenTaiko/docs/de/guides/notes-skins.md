<!-- guides/notes-skins.md -->

# Noten- und Bewertungspakete hinzufügen

Ein Skin kann mehrere Looks für die Noten und für die Bewertungstexte anbieten, und der Spieler wählt einen davon in den Einstellungen. Jeder Look ist ein Paket: ein nummerierter Ordner unter `Graphics/5_Game/` des Skins. Ein Notenpaket enthält die Noten, die Trommelwirbel, die Ballons, die Treffereffekte sowie die Ballon- und Trommelwirbel-Blasen; ein Bewertungspaket enthält die Bewertungstexte. Diese Anleitung erklärt die Ordner, die Sheets jedes Pakets, die Datei `Definition.json`, die ein Paket beschreibt, und wie das Spiel ergänzt, was einem Paket fehlt.

## Bevor Sie beginnen

- Ein eigener Skin (siehe [Skins und Themes hinzufügen](skins.md)). Der mitgelieferte Skin `System/Open-World Memories/` hat vier Taiko-Notenpakete, ein Bongo-Notenpaket und vier Bewertungspakete, die Sie kopieren können.
- Ein Bildeditor, der PNG-Dateien mit Transparenz speichert.
- Die Notenzellengröße Ihres Skins: der Layout-Wert `Game_Notes_Size=` (`195,195` im mitgelieferten Skin). Jede Zelle der Noten-Sheets auf dieser Seite hat diese Größe, außer denen des Riesen-Sheets.

## Schritt 1: Die Paketordner anlegen

Das Spiel sucht unter `Graphics/5_Game/` nach drei Ordnern und behandelt jeden nummerierten Unterordner darin als ein Paket, sortiert nach Nummer:

- `Notes Skin (Taiko)/`: Notenpakete für Taiko-Spieler.
- `Notes Skin (Bongo)/`: Notenpakete für Bongo-Spieler.
- `Judges Skin/`: Pakete mit Bewertungstexten, für jeden Spieler.

Paket `1` jedes Ordners ist der Standard und die Basis der anderen (Schritt 7). Die Ordnernummer legt nur die Reihenfolge der Pakete fest; die Einstellungen zeigen den Namen aus `Definition.json`.

```
Graphics/5_Game/
  Notes Skin (Taiko)/
    1/                  <- der Standard-Look, vollständig
      Definition.json
      Notes.png
      ...
    2/                  <- ein anderer Look: nur die abweichenden Dateien
      Definition.json
      Notes.png
  Notes Skin (Bongo)/
    1/
  Judges Skin/
    1/
      Definition.json
      Judge.png
```

## Schritt 2: Die Taiko-Noten-Sheets zeichnen

Ein Sheet hat eine Zeile pro Animationsframe (Schritt 5 legt die Anzahl der Frames fest), und seine Zellen liegen nebeneinander in der unten stehenden Reihenfolge.

| Datei | Inhalt |
| --- | --- |
| `HitCircle.png` | Der Trefferkreis, eine Zelle. |
| `Notes.png` | Don, Ka, großes Don, großes Ka, KaDon, ADLib. |
| `Rolls.png` | Kopf, Körper und Ende des kleinen Trommelwirbels, dann Kopf, Körper und Ende des großen Trommelwirbels. Das Spiel streckt die Körperzelle über die Länge des Trommelwirbels. |
| `Balloons.png` | Der Ballon (2 Zellen breit), die Kusudama (2 Zellen breit), dann Kopf, Körper und Ende des Fuse-Rolls. Bongo-Spieler sehen diese Ballons ebenfalls. |
| `ChipEffect.png` | Optional. Der Blitz an der Stelle, an der eine getroffene Note landet, mit den Spalten von `Notes.png`, eine Zeile. Ohne diese Datei lässt das Spiel die Note selbst aufblitzen. |
| `Notes_Partner.png` | Optional. Der Partner-Look jeder Spalte von `Notes.png` (Schritt 6). |
| `Notes_Giant.png` | Optional. Das riesige Don, Ka, KaDon und ADLib (Schritt 6). |
| `Preview.png` | Optional. Das Bild, das die Einstellungen für das Paket zeigen. Ohne diese Datei zeigen sie `Notes.png`. |

Das Paket enthält außerdem die Treffereffekte sowie die Ballon- und Trommelwirbel-Blasen:

| Datei | Inhalt |
| --- | --- |
| `Hit/Explosion.png` | Die Explosion über dem Trefferkreis, 7 quadratische Frames pro Zeile: eine Zeile für Gut, eine Zeile für Ok, dann optional eine Zeile für Gut und eine Zeile für Ok bei großen Noten. Bei 2 Zeilen verwenden große Noten die ersten beiden. |
| `Hit/Explosion_Big.png` | Die zusätzliche Explosion über einer großen Note, ein einzelnes Bild, für beide Bewertungen gezeichnet. |
| `Hit/FireWorks.png` | Die Funken, die eine fliegende große Note hinter sich lässt. |
| `Hit/Bomb.png` | Die Explosion einer getroffenen Bombe oder eines verfehlten Fuse-Rolls, 7 Frames in einer Zeile. |
| `Hit/Great/`, `Hit/Great_Big/`, `Hit/Good/`, `Hit/Good_Big/` | Optionale Frame-Ordner (`0.png` bis `14.png`), die das Spiel bei einem Gut (`Great`) oder einem Ok (`Good`) auf einer kleinen oder großen Note auf der Spur zeichnet. |
| `Kusudama/` | Die `Script.lua` der Kusudama und ihre Bilder, einschließlich `Kusudama_Number.png`. |
| `Fuseroll/` | `Fuse.png`, die Blase des Fuse-Rolls, und `Number_Fuse.png`, seine Ziffern. |
| `Breaking.png` | Die Bilder der platzenden Ballonblase: 6 gleich hohe Bilder, übereinander. |
| `Balloon.png` | Die Ballonblase. |
| `Roll.png` | Die Blase des Trommelwirbelzählers, ihre Animationsframes nebeneinander. |

## Schritt 3: Ein Bongo-Notenpaket zeichnen

Ein Bongo-Paket enthält nur, was sich für Bongo-Spieler unterscheidet:

| Datei | Inhalt |
| --- | --- |
| `Notes.png` | Rot, Gelb, Rosa, Klatschen, ADLib. |
| `Rolls.png` | Die Trommelwirbel in Rot, Rosa, Gelb und Klatschen, jeweils als Kopf, Körper und Ende. |
| `HitCircle.png` | Optional. Ohne diese Datei sehen Bongo-Spieler den Trefferkreis des Taiko-Pakets. |
| `ChipEffect.png`, `Notes_Partner.png` | Optional, mit den Spalten dieser `Notes.png`. |
| `Notes_Giant.png` | Optional, mit den Spalten dieser `Notes.png`. |
| `Preview.png` | Optional. |

Die Ballons, die Treffereffekte und die Blasen kommen immer aus dem ausgewählten Taiko-Paket.

## Schritt 4: Ein Bewertungspaket zeichnen

Ein Bewertungspaket enthält `Judge.png`, 5 gleich hohe Zeilen: Gut, Ok, Übel, ADLib und das Boom der Bombe. Seine `Definition.json` braucht nur einen `Name`. Lassen Sie eine Zeile transparent, um diese Bewertung auszublenden; die Pakete "(Without Good)" des mitgelieferten Skins tun das für die Zeile Gut.

## Schritt 5: Definition.json schreiben

`Definition.json` benennt das Paket und beschreibt seine Sheets. Jedes Feld ist optional; ein Paket ohne diese Datei zeigt seine Ordnernummer als Namen und verwendet die unten stehenden Standardwerte.

```json
{
	"Name": { "strings": { "default": "Sky Spirits", "ja": "スカイスピリッツ" } },
	"Notes": {
		"Frames": 3,
		"Animation": [
			{ "FromCombo": 0, "Rows": [0] },
			{ "FromCombo": 50, "Rows": [1, 1, 0, 0], "RowsOnEntry": [0] },
			{ "FromCombo": 150, "Rows": [0, 1] },
			{ "FromCombo": 300, "Rows": [0, 2], "RowsOnEntry": [0, 1] }
		]
	},
	"Rolls": { "Frames": 3, "Animation": [] },
	"Balloons": { "Frames": 3, "Animation": [] },
	"FlyingNotesFrame": 1,
	"PartnerNotesScale": 1.4142,
	"GiantNotesScale": 2.0
}
```

| Feld | Standard | Bedeutung |
| --- | --- | --- |
| `Name` | die Ordnernummer | Der Name in den Einstellungen, ein String pro Sprachcode, mit `default` für jede andere Sprache. |
| `Notes`, `Rolls`, `Balloons` | 1 Frame, keine Animation | Die Zeilenanzahl des Sheets (`Frames`) und seine Animation (`Animation`). `Notes` legt auch die Zeilen von `Notes_Partner.png` und `Notes_Giant.png` fest. |
| `FlyingNotesFrame` | `0` | Die Zeile von `Notes.png` für die Noten, die nach einem Treffer zur Leiste fliegen. |
| `PartnerNotesScale` | die Quadratwurzel aus 2 | Die Skalierung einer kleinen Partnernote, wenn das Paket keine `Notes_Partner.png` hat (Schritt 6). |
| `GiantNotesScale` | `2` | Die Skalierung einer Riesennote, wenn das Paket keine `Notes_Giant.png` hat (Schritt 6). |

`Animation` ist eine Liste von Stufen. Das Spiel verwendet die Stufe mit dem höchsten `FromCombo`, das die Combo des Spielers erreicht hat. Ein Beat hat 4 Teilschritte, und `Rows` gibt die Zeile an, die bei jedem Teilschritt gezeichnet wird; eine kürzere Liste wiederholt sich, sodass `[0, 1]` bei jedem Teilschritt wechselt und `[0]` stillsteht. `RowsOnEntry` ersetzt `Rows` für etwa zwei Drittel einer Sekunde, nachdem die Combo diese Stufe erreicht hat. Ohne Stufen zeigt das Sheet seine erste Zeile. Die Tabelle des Beispiels ist die Animation, die das Spiel schon immer verwendet hat, und der Einfachmodus der Einstellungen hält jedes Sheet auf seiner ersten Zeile.

## Schritt 6: Partner- und Riesennoten

Eine Partnernote behält in den meisten Fällen ihre Größe, aber die kleinen (ein kleines Don oder Ka, ein ADLib, eine Bombe, jede Bongo-Note) nehmen einen großen Look an. Eine Riesennote ist viel größer und wackelt langsam. Jede hat ihr eigenes optionales Sheet:

- `Notes_Partner.png` hat die Spalten von `Notes.png` und ebenso viele Zeilen. Eine kleine Partnernote verwendet die Zelle ihrer eigenen Spalte.
- `Notes_Giant.png` hat ebenso viele Zeilen wie `Notes.png`. In einem Taiko-Paket sind seine Spalten Don, Ka, KaDon und ADLib, weil eine Riesennote unabhängig von ihrer Größe groß aussieht: Ein kleines Don und ein großes Don verwenden beide die Don-Zelle. In einem Bongo-Paket sind seine Spalten die von `Notes.png`. Die Zellengröße ist die Breite des Sheets geteilt durch seine Spaltenanzahl, sodass eine Riesenzelle größer als eine Notenzelle sein kann.

Ohne diese Sheets baut das Spiel die Looks aus `Notes.png`:

| Note | Partner, ohne `Notes_Partner.png` | Riese, ohne `Notes_Giant.png` |
| --- | --- | --- |
| Kleines Taiko-Don oder -Ka | Das große Don oder Ka | Das große Don oder Ka, mal `GiantNotesScale` |
| Jede andere kleine Note | Die Note, mal `PartnerNotesScale` | Die Note, mal `PartnerNotesScale` und `GiantNotesScale` |
| Großes Don, großes Ka, KaDon | Unverändert | Die Note, mal `GiantNotesScale` |

Setzen Sie beide Skalierungen, auch wenn das Paket beide Sheets hat: Wer das Paket kopiert und ein Sheet entfernt, erhält dann eine sinnvolle Größe. Eine skalierte Note ist ein gestrecktes Bild und wirkt weicher als eine Zelle, die in ihrer Größe gezeichnet ist.

<div class="callout warn">Das Spiel liest Notes_Partner.png, Notes_Giant.png und ChipEffect.png nur aus dem ausgewählten Paket. Sie müssen zu den eigenen Noten des Pakets passen, daher springen die Kopien aus Paket 1 nie für sie ein.</div>

## Schritt 7: Paket 1 den Rest ergänzen lassen

Fehlt dem ausgewählten Paket eine Datei, nimmt das Spiel sie aus Paket 1 desselben Ordners. Ein Paket kann daher nur enthalten, was es ändert: Ein Paket, das nur eine Zeile von `Hit/Explosion.png` leert, enthält eine `Definition.json` und diese eine Datei. Die Regeln:

- Das Spiel prüft jede Datei einzeln, auch innerhalb von Ordnern (`Hit/Great/3.png`). Die Kusudama ist die Ausnahme: Ein Paket ohne eigenen Ordner `Kusudama/` verwendet den gesamten Ordner von Paket 1.
- Der Rückfall reicht nur bis Paket 1. Ein Paket, das Paket 3 abwandelt, muss eigene Kopien der Sheets von Paket 3 enthalten.
- `Definition.json` kommt nie aus Paket 1, und die drei Sheets aus Schritt 6 ebenso wenig.
- Ein Bongo-Paket fällt auf Bongo-Paket 1 zurück.

## Schritt 8: Ein Paket wählen

Hat ein Skin in einem der drei Ordner mindestens 2 Pakete, zeigt die Theme-Seite der Einstellungen zuerst einen Abschnitt Noten-Skins, mit einer Auswahl für diesen Ordner und der Vorschau des Pakets darunter. Das Spiel lädt das gewählte Paket sofort und merkt sich die Wahl für den Skin in `ThemeSettings.db3`. Bei einem einzigen Paket erscheint die Auswahl nicht, und das Spiel verwendet dieses Paket.

Ein Lua-Modul liest eine Datei des ausgewählten Pakets mit `THEME:GetNotesSkinFile(kind, file)` (siehe [Spieler und Profile](../api/players.md)). Die mitgelieferte Songauswahl verwendet es, um die Bewertungstexte ihrer Replay-Liste zu zeichnen:

```lua
local file = THEME:GetNotesSkinFile("judges", "Judge.png")   -- "5_Game/Judges Skin/2/Judge.png", oder ""
if file == "" then file = "5_Game/Judge.png" end              -- ein Skin ohne Bewertungspakete
```

## Skins ohne Pakete

Ein Skin ohne Ordner `Notes Skin (Taiko)/` behält die Dateien, die er vor der Einführung der Pakete hatte: `Notes.png` (der Trefferkreis, die 4 Noten, die Trommelwirbel und der Ballon), `Swap.png` (KaDon), `Adlib.png`, `Kusu.png`, `FuseRoll.png`, `ChipEffect.png`, den Ordner `10_Effects/Hit/` und die Ballondateien von `11_Balloon/`. Ohne Ordner `Notes Skin (Bongo)/` kommen Bongo-Noten und -Trommelwirbel aus `Notes_Konga.png`, und ohne Ordner `Judges Skin/` kommen die Bewertungen aus `5_Game/Judge.png`. Diese Dateien haben keine Partner- oder Riesen-Sheets, daher verwenden Partner- und Riesennoten die skalierten Looks aus Schritt 6.

## Fehlerbehebung und Hinweise

- Die Einstellungen zeigen die Auswahl nicht: Der Ordner enthält ein einziges Paket, oder die Paketordner tragen keine Zahlen als Namen.
- Ein Paket zeigt die Noten eines anderen Pakets: Der Rückfall hat die Datei aus Paket 1 genommen, weil sie dem Paket fehlt, oder die Groß-/Kleinschreibung des Dateinamens weicht auf einem System ab, das sie unterscheidet.
- Noten zeigen den falschen Frame oder ein abgeschnittenes Bild: `Frames` passt nicht zur Höhe des Sheets geteilt durch die Zellenhöhe.
- Die Partner- oder Riesennoten eines kopierten Pakets sehen aus wie die alten: Die Kopie hat `Notes_Partner.png` oder `Notes_Giant.png` vom Original behalten.
- Bongo-Spieler sehen keine Noten: `Notes.png` fehlt sowohl im ausgewählten Bongo-Paket als auch in Bongo-Paket 1.
