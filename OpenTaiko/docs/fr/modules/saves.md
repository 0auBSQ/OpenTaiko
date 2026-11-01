<!-- modules/saves.md -->

# Saves

## Saves.SavePicker

La question "la sauvegarde de qui ?" posée avant l'ouverture d'un stage : une liste des sauvegardes locales, chacune avec sa plaque de nom et une ligne d'information (ou son nombre de pièces). Le stage qui pose la question transmet le choix au stage qu'il ouvre via `SHARED`. La liste est construite avec la bibliothèque d'interface du skin ([UI.Pick](ui.md)), sauf si l'appelant en passe une, et ses titres viennent du `lang/` propre au module.

| Fonction | Description |
| --- | --- |
| `SavePicker.open(o)  -> slot or nil` | Ouvre la liste. Quand il n'existe qu'une sauvegarde (ou aucune), elle n'ouvre rien et renvoie immédiatement cet emplacement (0 s'il n'y en a aucune) ; sinon elle renvoie nil et la liste reste ouverte. |
| `SavePicker.update(ts)  -> nil, "cancel" or slot` | À appeler à chaque frame tant que la liste est ouverte : nil tant qu'elle est affichée, puis `"cancel"` ou l'emplacement choisi (à partir de 0). La liste se ferme dans les deux cas. |
| `SavePicker.draw()` | Dessine la liste. |
| `SavePicker.isOpen()  -> bool`, `SavePicker.close()` | Si la liste est affichée ; la ferme. |
| `SavePicker.pass(slot)` | Transmet un emplacement au stage sur le point de s'ouvrir (la chaîne partagée `entry_save`). |
| `SavePicker.take()  -> slot or nil` | Dans le stage ouvert : l'emplacement qui lui a été transmis, lu une seule fois (la chaîne est vidée) ; nil s'il est arrivé sans emplacement ou si la sauvegarde n'existe plus. |
| `SavePicker.first()  -> slot` | La première sauvegarde existante, pour un stage ouvert sans choix. |
| `SavePicker.saves()  -> table` | Les sauvegardes existantes : `{ { value = slot, save = save file }, ... }`. |
| `SavePicker.text(id)  -> string` | L'une des chaînes du module (`title`, `title_room`, `title_shop`) dans la langue du jeu. |

| Champ de `o` | Description |
| --- | --- |
| `title` | Le titre de la liste. |
| `kind` | `"room"` ou `"shop"` pour le titre propre au module, "La chambre de qui ?" ou "La boutique de qui ?", quand `title` n'est pas fourni. |
| `info(slot, save)` | Une fonction qui renvoie le texte affiché à droite de la ligne d'une sauvegarde, ou nil. |
| `coins` | true affiche à la place le nombre de pièces de chaque sauvegarde. |
| `theme`, `sfx` | Transmis au gestionnaire d'interface. |
| `ui` | Une table de bibliothèque d'interface, ou son nom de require ; [UI.Pick](ui.md) par défaut. |

```lua
local SavePicker = require("Saves.SavePicker")
local NavInput = require("Input.NavInput")
local picking = false

function update(ts)
    if picking then
        local r = SavePicker.update(ts)
        if r == nil then return end
        picking = false
        if r ~= "cancel" then SavePicker.pass(r); return Exit("stage", "my_shop") end
    elseif NavInput.decide() then
        local slot = SavePicker.open{ kind = "shop", coins = true }
        if slot == nil then picking = true
        else SavePicker.pass(slot); return Exit("stage", "my_shop") end
    end
end

function draw()
    if picking then SavePicker.draw() end
end

-- in my_shop's activate
local slot = SavePicker.take() or SavePicker.first()
```
