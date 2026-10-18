<!-- modules/characters.md -->

# Characters

## Characters.Student

De verhaalleerling als wie de speler rondloopt, A of B, en het kleurenpalet van die leerling, zodat elke verhaalscène dezelfde leerling in dezelfde kleuren tekent. De leerlingen zijn de personages `02v2 - Student (A)` en `03v2 - Student (B)` uit `Global/Characters/`.

Alles wordt in het opslagbestand bewaard:

| Opslagsleutel | Soort | Inhoud |
| --- | --- | --- |
| `story_student` | teller | De gekozen leerling: 1 is B; elke andere waarde, ook geen waarde, geldt als A. |
| `.character_palette_<folder>` | teller | De 0-gebaseerde paletindex van het personage van de leerling, dezelfde teller die het scherm voor het aanpassen van personages schrijft, zodat beide gelijk blijven. A en B hebben elk hun eigen. |
| `.character_playcount_<folder>` | teller | De spelbeurten met dat personage, die paletten ontgrendelen. |

De paletten van een leerling zijn de `Palettes.json` van het personage, gevolgd door de paletten uit de eigen kopie van de module (`Global/Modules/Lib/Characters/Student/<id>/Palettes.json`) die het bestand van het personage niet heeft, gekoppeld op `name`. Een personage zonder paletten, of een dat niet geïnstalleerd is, heeft toch die van de kopie. Een index buiten de lijst geldt als het standaardpalet, 0.

| Functie | Beschrijving |
| --- | --- |
| `Student.current(save)  -> "a" or "b"` | De leerling die het opslagbestand heeft gekozen. |
| `Student.ensure(save)  -> "a" or "b"` | Hetzelfde, waarbij A in de teller wordt geschreven als die geen leerling bevat. |
| `Student.set(save, id)  -> id` | Kiest een leerling. |
| `Student.folder(id)  -> string` | De naam van de personagemap van de leerling. |
| `Student.index(save, id)  -> number` | De paletindex van de leerling, altijd een geldige. |
| `Student.setIndex(save, id, idx)  -> number` | Slaat een paletindex op; is dat personage het personage dat het opslagbestand heeft uitgerust, dan verandert ook zijn actieve palet. |
| `Student.palettes(id)  -> table` | De paletlijst: items `{ name, plays, blend, stops }`, item `i` voor index `i - 1`; `name` is in de taal van het spel als het bestand er een geeft. |
| `Student.count(id)`, `Student.entry(id, idx)`, `Student.clamp(id, idx)` | Het aantal paletten, één item, een geldig gemaakte index. |
| `Student.unlocked(save, id, idx)  -> bool` | Of het palet open is: geen eis aan spelbeurten, of genoeg spelbeurten met het personage. |
| `Student.gradient(id, idx)  -> gradient` | Het palet als `GRADIENT`-map, of nil voor een palet zonder kleuren. |
| `Student.withPalette(id, idx, fn, ...)` | Voert `fn` (2D-tekenwerk) uit met het palet actief. Geeft de resultaten van pcall terug. |
| `Student.registerSprite(scene, spriteId, tex, id, idx)  -> bool` | Registreert `tex` als billboardsprite van een 3D-scène, herkleurd in het palet. |
| `Student.reload()` | Vergeet de geparseerde paletbestanden, die bij de volgende opvraging opnieuw worden gelezen. |

`palettes`, `count`, `entry`, `clamp`, `gradient` en `registerSprite` nemen een optioneel laatste argument, `visitor`: true voor de lijst van de leerling van een andere speler (een online bezoeker), apart gecachet van die van de speler zelf.

```lua
local Student = require("Characters.Student")
local save = GetSaveFile(0)
local id  = Student.current(save)
local idx = Student.index(save, id)
Student.withPalette(id, idx, function() tex:Draw(x, y) end)
```
