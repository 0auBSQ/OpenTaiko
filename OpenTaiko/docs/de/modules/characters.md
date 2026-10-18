<!-- modules/characters.md -->

# Characters

## Characters.Student

Die Story-Schülerfigur, mit der der Spieler unterwegs ist, A oder B, und ihre Farbpalette, sodass jede Story-Szene dieselbe Figur in denselben Farben zeichnet. Die Figuren sind die Charaktere `02v2 - Student (A)` und `03v2 - Student (B)` aus `Global/Characters/`.

Alles wird im Spielstand gespeichert:

| Spielstand-Schlüssel | Art | Inhalt |
| --- | --- | --- |
| `story_student` | Zähler | Die gewählte Figur: 1 ist B; jeder andere Wert, auch ein ungesetzter, gilt als A. |
| `.character_palette_<folder>` | Zähler | Der 0-basierte Palettenindex des Charakters der Figur, derselbe Zähler, den der Bildschirm zum Anpassen des Charakters schreibt, sodass beide übereinstimmen. A und B haben jeweils ihren eigenen. |
| `.character_playcount_<folder>` | Zähler | Die Spiele mit diesem Charakter, die Paletten freischalten. |

Die Paletten einer Figur sind die `Palettes.json` des Charakters, gefolgt von den Paletten der moduleigenen Kopie (`Global/Modules/Lib/Characters/Student/<id>/Palettes.json`), die die Datei des Charakters nicht hat, abgeglichen über `name`. Ein Charakter ohne Paletten, oder einer, der nicht installiert ist, hat trotzdem die der Kopie. Ein Index außerhalb der Liste gilt als Standardpalette, 0.

| Funktion | Beschreibung |
| --- | --- |
| `Student.current(save)  -> "a" or "b"` | Die Figur, die der Spielstand gewählt hat. |
| `Student.ensure(save)  -> "a" or "b"` | Dasselbe, wobei A in den Zähler geschrieben wird, wenn er keine Figur enthält. |
| `Student.set(save, id)  -> id` | Wählt eine Figur. |
| `Student.folder(id)  -> string` | Der Name des Charakterordners der Figur. |
| `Student.index(save, id)  -> number` | Der Palettenindex der Figur, immer ein gültiger. |
| `Student.setIndex(save, id, idx)  -> number` | Speichert einen Palettenindex; ist dieser Charakter der, den der Spielstand ausgerüstet hat, ändert sich auch seine aktive Palette. |
| `Student.palettes(id)  -> table` | Die Palettenliste: Einträge `{ name, plays, blend, stops }`, Eintrag `i` für Index `i - 1`; `name` ist in der Sprache des Spiels, wenn die Datei eine angibt. |
| `Student.count(id)`, `Student.entry(id, idx)`, `Student.clamp(id, idx)` | Die Anzahl der Paletten, ein einzelner Eintrag, ein gültig gemachter Index. |
| `Student.unlocked(save, id, idx)  -> bool` | Ob die Palette offen ist: keine Spielanforderung, oder genug Spiele mit dem Charakter. |
| `Student.gradient(id, idx)  -> gradient` | Die Palette als `GRADIENT`-Map, oder nil für eine Palette ohne Farben. |
| `Student.withPalette(id, idx, fn, ...)` | Führt `fn` (2D-Zeichnen) mit aktiver Palette aus. Gibt die Ergebnisse von pcall zurück. |
| `Student.registerSprite(scene, spriteId, tex, id, idx)  -> bool` | Registriert `tex` als Billboard-Sprite einer 3D-Szene, in der Palette umgefärbt. |
| `Student.reload()` | Vergisst die geparsten Palettendateien, die bei der nächsten Abfrage neu gelesen werden. |

`palettes`, `count`, `entry`, `clamp`, `gradient` und `registerSprite` nehmen ein optionales letztes Argument, `visitor`: true für die Liste der Figur eines anderen Spielers (eines Online-Besuchers), getrennt von der eigenen des Spielers zwischengespeichert.

```lua
local Student = require("Characters.Student")
local save = GetSaveFile(0)
local id  = Student.current(save)
local idx = Student.index(save, id)
Student.withPalette(id, idx, function() tex:Draw(x, y) end)
```
