<!-- modules/input.md -->

# Input

## Input.NavInput

La navigation dans les menus sur tous les périphériques à la fois : les flèches du clavier, Entrée et Échap, les entrées universelles de la configuration des touches (`RightChange`, `LeftChange`, `Decide`, `Cancel`) et le tambour de chaque joueur (les bords bleus pour se déplacer, les centres rouges pour valider). Un écran demande "le joueur a-t-il validé ?" sans lister de touches, et la configuration des touches du joueur s'applique.

Chaque association est une fonction qui renvoie true pendant cette frame. Ajoutez `Pressed` (identique à l'absence de suffixe), `Pressing` ou `Released` au nom pour les autres événements d'entrée : `decide()`, `decidePressing()`, `leftReleased()`.

| Association | Répond à |
| --- | --- |
| `right`, `left` | La flèche droite ou gauche, `RightChange` / `LeftChange`, un bord bleu. |
| `rightKeyboard`, `leftKeyboard` | La flèche du clavier uniquement. |
| `up`, `down` | La flèche haut ou bas (les tambours n'ont pas d'entrée verticale). |
| `upOrPadLeft`, `downOrPadRight` | La flèche haut (bas), ou ce à quoi `left` (`right`) répond sur une manette ou un tambour : pour une liste verticale qu'un tambour peut faire défiler. |
| `decide` | Entrée, `Decide`, un centre rouge. |
| `cancel` | Échap, `Cancel`. |
| `rightOtherPlayer`, `leftOtherPlayer` | Jeux d'associations par joueur uniquement : la flèche, ou le bord bleu d'un autre joueur. |

| Fonction ou champ | Description |
| --- | --- |
| `NavInput.decide()`, `NavInput.left()`, ...  `-> bool` | Les associations pour tous les joueurs à la fois : le clavier, les entrées universelles et le tambour de n'importe quel joueur. |
| `NavInput.getPn(player)  -> table` | Les associations d'un seul joueur : de 1 à 5 pour le tambour de ce joueur plus le clavier et les entrées universelles, 0 pour le clavier et les entrées universelles seuls, `""` ou nil pour tous les joueurs (comme les raccourcis ci-dessus). |
| `NavInput.p[player]` | Les tables de `getPn` pour 0 à 5 et `""`, construites une fois. |
| `NavInput.bindingNames(binding, player?)  -> string[]` | Ce à quoi répond une association, sous forme de noms affichables pour une indication : la touche du clavier d'abord, puis les associations de la configuration des touches (`"Pad 3"`, `"MIDI 38"`, `"Mouse Left"`), sans doublons. |
| `NavInput.keyName(key)  -> string` | Le nom affichable d'une touche du clavier (`"Return"` donne `"Enter"`, `"D1"` donne `"1"`) ; nil pour aucune. |

```lua
local NavInput = require("Input.NavInput")
local p1 = NavInput.getPn(1)

function update(ts)
    if p1.right() then cursor = cursor + 1 end
    if p1.left() then cursor = cursor - 1 end
    if NavInput.cancel() then return Exit("stage", "_title") end
end
```
