<!-- modules/characters.md -->

# Characters

## Characters.Student

L'élève de l'histoire que le joueur incarne, A ou B, et la palette de couleurs de cet élève, pour que chaque scène d'histoire dessine le même élève dans les mêmes couleurs. Les élèves sont les personnages `02v2 - Student (A)` et `03v2 - Student (B)` de `Global/Characters/`.

Tout est conservé dans le fichier de sauvegarde :

| Clé de sauvegarde | Nature | Contenu |
| --- | --- | --- |
| `story_student` | compteur | L'élève choisi : 1 vaut B ; toute autre valeur, absence de valeur comprise, se lit comme A. |
| `.character_palette_<folder>` | compteur | L'indice de palette (à partir de 0) du personnage de l'élève, le même compteur que celui qu'écrit l'écran de personnalisation du personnage, si bien que les deux restent synchronisés. A et B ont chacun le leur. |
| `.character_playcount_<folder>` | compteur | Les parties jouées avec ce personnage, qui débloquent des palettes. |

Les palettes d'un élève sont le `Palettes.json` du personnage, suivi des palettes de la copie propre au module (`Global/Modules/Lib/Characters/Student/<id>/Palettes.json`) que le fichier du personnage n'a pas, rapprochées par `name`. Un personnage sans palettes, ou qui n'est pas installé, a quand même celles de la copie. Un indice hors de la liste se lit comme la palette par défaut, 0.

| Fonction | Description |
| --- | --- |
| `Student.current(save)  -> "a" or "b"` | L'élève qu'a choisi la sauvegarde. |
| `Student.ensure(save)  -> "a" or "b"` | La même chose, en écrivant A dans le compteur quand il ne contient aucun élève. |
| `Student.set(save, id)  -> id` | Choisit un élève. |
| `Student.folder(id)  -> string` | Le nom du dossier de personnage de l'élève. |
| `Student.index(save, id)  -> number` | L'indice de palette de l'élève, toujours valide. |
| `Student.setIndex(save, id, idx)  -> number` | Enregistre un indice de palette ; quand ce personnage est celui que la sauvegarde a équipé, sa palette active change aussi. |
| `Student.palettes(id)  -> table` | La liste des palettes : des entrées `{ name, plays, blend, stops }`, l'entrée `i` pour l'indice `i - 1` ; `name` est dans la langue du jeu quand le fichier en fournit une. |
| `Student.count(id)`, `Student.entry(id, idx)`, `Student.clamp(id, idx)` | Le nombre de palettes, une entrée, un indice rendu valide. |
| `Student.unlocked(save, id, idx)  -> bool` | Si la palette est ouverte : aucune exigence de parties, ou assez de parties avec le personnage. |
| `Student.gradient(id, idx)  -> gradient` | La palette sous forme de table `GRADIENT`, ou nil pour une palette sans couleurs. |
| `Student.withPalette(id, idx, fn, ...)` | Exécute `fn` (du dessin 2D) avec la palette active. Renvoie les résultats de pcall. |
| `Student.registerSprite(scene, spriteId, tex, id, idx)  -> bool` | Enregistre `tex` comme sprite billboard d'une scène 3D, recoloré dans la palette. |
| `Student.reload()` | Oublie les fichiers de palettes analysés, qui seront relus à la prochaine requête. |

`palettes`, `count`, `entry`, `clamp`, `gradient` et `registerSprite` prennent un dernier argument facultatif, `visitor` : true pour la liste de l'élève d'un autre joueur (un visiteur en ligne), mise en cache séparément de celle du joueur.

```lua
local Student = require("Characters.Student")
local save = GetSaveFile(0)
local id  = Student.current(save)
local idx = Student.index(save, id)
Student.withPalette(id, idx, function() tex:Draw(x, y) end)
```
