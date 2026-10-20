<!-- modules/custom-stages.md -->

# Stages personnalisés

Un stage personnalisé est un stage que chacun peut créer, partager et installer, et que liste le menu Stages personnalisés d'un skin. Il déclare l'espace de noms `CStage` avec [STAGES:SetNamespace](../api/activities.md), et revient par la chaîne partagée `custom_stage_return` au stage qui l'a ouvert. `Stages.CustomStages` fait les deux, et liste les stages personnalisés pour un menu. `Global/Modules/Stages/custom_stage_tutorial` en est un : un guide, dans le jeu, pour créer des stages personnalisés.

## Le déclarer

En haut de Script.lua, hors de toute fonction :

```lua
-- Global/Modules/Stages/lantern_walk/Script.lua, at the top
local CS = require("Stages.CustomStages")

CS.declare{
    title       = CS.allLanguages("menu", "title"),       -- lang/<code>/menu.json, every language
    description = { en = "A calm walk down a street of paper lanterns." },
}
```

`CS.declare(info)` équivaut à `STAGES:SetNamespace("CStage", info)`. Les menus lisent deux champs d'info :

| Champ | Affiche |
| --- | --- |
| `title` | Le nom du stage dans la liste : une chaîne, ou une table par code de langue. Le nom du dossier s'il manque. |
| `description` | Une ou deux lignes sous la liste tant que le stage est sélectionné ; facultatif. |

Une table par code de langue donne le texte dans la langue du jeu, sinon en anglais, sinon dans n'importe quelle langue qu'elle contient. Les autres champs sont à vous : les menus transmettent info tel quel.

## Le chemin du retour

Le menu qui ouvre un stage personnalisé règle la chaîne partagée `custom_stage_return` sur le stage où revenir, `"<stage>"` ou `"<stage>|<transition>"`, puis part vers le stage personnalisé. Celui-ci lit et vide la chaîne à l'entrée, et part vers ce stage, ou vers `_title` si la chaîne est vide :

```lua
local back, backTransition

function activate()
    back, backTransition = CS.takeReturn()   -- "_title" when the string is empty
end

function update(ts)
    if leaving then return Exit("stage", back, backTransition) end
end
```

La chaîne est lue à chaque entrée. Un stage personnalisé qui part vers un autre de ses stages et revient règle de nouveau la chaîne avant de partir, comme Ma Chambre le fait avec `my_room_return`.

## Stages.CustomStages

| Fonction | Description |
| --- | --- |
| `CS.declare(info)  -> boolean` | Déclare le stage appelant comme stage personnalisé : `STAGES:SetNamespace("CStage", info)`. |
| `CS.allLanguages(name, id)  -> table` | La chaîne `id` du `lang/<code>/<name>.json` du stage appelant, prise dans chacune des neuf langues qui l'ont, sous forme de table par code de langue. Prévu pour `declare`, qui s'exécute avant qu'une langue soit choisie. |
| `CS.localized(value, code?)  -> string or nil` | Un texte déclaré dans `code` (par défaut la langue du jeu) : une chaîne telle quelle, une table par code de langue comme décrit plus haut. |
| `CS.list(code?)  -> table` | Les stages personnalisés que le module appelant atteint, triés par titre : `{ name, isGlobal, title, description, info }`, avec `title` et `description` déjà dans la langue. |
| `CS.enter(name, back, transition?)` | Règle `custom_stage_return` sur `back` et part vers le stage `name` ; écrivez `return CS.enter(...)` dans l'update d'un stage. |
| `CS.takeReturn()  -> stage, transition` | Lit et vide `custom_stage_return` : le stage (`_title` si la chaîne est vide) et la transition (nil pour celle par défaut). |
| `CS.open(o)`, `CS.update(ts)`, `CS.draw()`, `CS.isOpen()`, `CS.close()` | La liste sous forme de boîte de dialogue au-dessus de l'écran de l'appelant, construite avec la bibliothèque d'interface du skin ([UI.Pick](ui.md)). `update` renvoie nil tant qu'elle est ouverte, `"cancel"`, ou l'entrée choisie de `CS.list` ; la boîte est fermée dans les deux cas. `o` : `title`, `ui` (une table de bibliothèque ou son nom de require), `theme`, `sfx`, `navPlayer` (par défaut 0 : le clavier et la manette, pas les tambours). Sans stage personnalisé installé, elle le dit au-dessus d'un bouton OK. |
| `CS.NAMESPACE`, `CS.RETURN_KEY` | `"CStage"` et `"custom_stage_return"`. |

Ses chaînes se trouvent dans son propre dossier `lang/`, dans les neuf langues ([Locale.i18n](locale.md)). Open-World Memories ouvre la liste depuis l'entrée Stages personnalisés de son écran titre, au-dessus de Paramètres :

```lua
-- a menu stage: CS.open{ title = "Custom Stages" } when its entry is chosen, then
function update(ts)
    if CS.isOpen() then
        local r = CS.update(ts)
        if r ~= nil and r ~= "cancel" then return CS.enter(r.name, "_title") end
        return
    end
end

function draw()
    if CS.isOpen() then CS.draw() end
end
```

## Où se trouve un stage personnalisé

| Dossier | Fonctionne avec |
| --- | --- |
| `Global/Modules/Stages/<name>/` | Tous les skins. |
| `System/<skin>/Modules/Stages/<name>/` | Ce skin seulement. |

Quand un nom existe aux deux endroits, un menu d'un skin liste le stage du skin et un menu global celui de Global, le stage que `Exit` ouvrirait. Le jeu crée les stages au démarrage et quand il recharge le skin : un stage installé pendant que le jeu tourne apparaît après un redémarrage.

## En partager un

Un stage personnalisé est son dossier. Zippez le dossier lui-même, pour que l'archive s'ouvre sur `<name>/Script.lua`, avec tout ce qu'il charge et les crédits de ses fichiers. Les joueurs l'extraient dans `Global/Modules/Stages/` de leur dossier du jeu, ou dans `Modules/Stages/` d'un skin, puis relancent le jeu. Une mise à jour ajoute et remplace des fichiers mais n'en supprime aucun : choisissez un nom de dossier qu'aucun stage fourni n'utilise.

## Le tutoriel

`custom_stage_tutorial` montre dans le jeu les quatre choses à savoir pour créer un stage personnalisé et le partager : où va son dossier (et où pas), l'espace de noms `CStage`, tout ce qu'il utilise dans son dossier et de l'extérieur seulement les modules fournis, et où lire cette documentation, l'onglet Documentation de l'OpenTaiko Hub. Ses pages sont les images de son dossier `Textures/` et les textes de son dossier `lang/`, si bien qu'on peut les remplacer fichier par fichier.
