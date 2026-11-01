<!-- modules/characters.md -->

# Characters

## Characters.Student

The story student the player walks as, A or B, and that student's colour palette, so every story scene draws the same student in the same colours. The students are the characters `02v2 - Student (A)` and `03v2 - Student (B)` of `Global/Characters/`.

Everything is kept in the save file:

| Save key | Kind | Holds |
| --- | --- | --- |
| `story_student` | counter | The chosen student: 1 is B; any other value, unset included, reads as A. |
| `.character_palette_<folder>` | counter | The 0-based palette index of the student's character, the same counter the character customize screen writes, so both stay in step. A and B each keep their own. |
| `.character_playcount_<folder>` | counter | The plays with that character, which unlock palettes. |

A student's palettes are the character's `Palettes.json`, followed by the palettes of the module's own copy (`Global/Modules/Lib/Characters/Student/<id>/Palettes.json`) that the character's file does not have, matched by `name`. A character without palettes, or one that is not installed, still has the copy's. An index outside the list reads as the default palette, 0.

| Function | Description |
| --- | --- |
| `Student.current(save)  -> "a" or "b"` | The student the save has chosen. |
| `Student.ensure(save)  -> "a" or "b"` | The same, writing A into the counter when it holds no student. |
| `Student.set(save, id)  -> id` | Chooses a student. |
| `Student.folder(id)  -> string` | The student's character folder name. |
| `Student.index(save, id)  -> number` | The student's palette index, always a valid one. |
| `Student.setIndex(save, id, idx)  -> number` | Stores a palette index; when that character is the one the save has equipped, its live palette changes too. |
| `Student.palettes(id)  -> table` | The palette list: `{ name, plays, blend, stops }` entries, entry `i` for index `i - 1`; `name` is in the game's language when the file gives one. |
| `Student.count(id)`, `Student.entry(id, idx)`, `Student.clamp(id, idx)` | The number of palettes, one entry, an index made valid. |
| `Student.unlocked(save, id, idx)  -> bool` | Whether the palette is open: no play requirement, or enough plays with the character. |
| `Student.gradient(id, idx)  -> gradient` | The palette as a `GRADIENT` map, or nil for a palette without colours. |
| `Student.withPalette(id, idx, fn, ...)` | Runs `fn` (2D drawing) with the palette active. Returns pcall's results. |
| `Student.registerSprite(scene, spriteId, tex, id, idx)  -> bool` | Registers `tex` as a billboard sprite of a 3D scene, recoloured in the palette. |
| `Student.reload()` | Forgets the parsed palette files, which are read again on the next query. |

`palettes`, `count`, `entry`, `clamp`, `gradient` and `registerSprite` take an optional last argument, `visitor`: true for the list of another player's student (an online visitor), cached apart from the player's own.

```lua
local Student = require("Characters.Student")
local save = GetSaveFile(0)
local id  = Student.current(save)
local idx = Student.index(save, id)
Student.withPalette(id, idx, function() tex:Draw(x, y) end)
```
