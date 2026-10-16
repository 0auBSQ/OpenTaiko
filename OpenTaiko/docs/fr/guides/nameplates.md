<!-- guides/nameplates.md -->

# Ajouter des plaques de nom

Un titre de plaque de nom est la plaque de texte affichée sur la plaque de nom d'un joueur, avec le nom du joueur et le titre de dan. Les joueurs débloquent des titres en jouant, en les achetant ou en les recevant d'un script, et en équipent un dans My Room. Vous pouvez ajouter vos propres titres avec un pack de plaques de nom : un dossier contenant un fichier `Nameplates.json` que vous placez dans `Global/Nameplates/`. Un skin peut fournir son propre pack. Le jeu liste les plaques de nom de chaque pack à côté des plaques fournies, dans chaque écran qui affiche des plaques de nom.

## Avant de commencer

- Un éditeur de texte qui enregistre en UTF-8.
- Un identifiant de pack : un nom court composé de lettres minuscules, de chiffres et de `_` qu'aucun autre pack n'utilise, par exemple votre nom ou le nom de votre pack de partitions. Le jeu en tire l'identifiant de chaque plaque de nom du pack.
- Le jeu lit les packs au démarrage et quand il charge un skin. Redémarrez le jeu après chaque modification pour la voir.

## Étape 1 : Créer le dossier du pack

Créez un dossier dans `Global/Nameplates/`, à côté du jeu. Le nom du dossier est libre. Le pack contient un fichier requis et un dossier `Lang/` facultatif pour les traductions (étape 3) :

```
Global/
  Nameplates/
    Example Pack/
      Nameplates.json
      Lang/
        fr.json
        ja.json
```

Un skin fournit son pack de la même façon, sous la forme d'un dossier `Nameplates/` à la racine du skin (`System/<skin>/Nameplates/Nameplates.json`). Le jeu ne charge que le pack du skin actif ; les plaques de nom d'un pack de skin disparaissent donc tant qu'un autre skin est actif.

## Étape 2 : Écrire Nameplates.json

`Nameplates.json` contient l'identifiant du pack et la liste des plaques de nom :

- `pack` (string, requis) : l'identifiant du pack.
- `name` (object, facultatif) : le nom du pack, affiché dans la barre des packs du PC de My Room, sous la forme `{ "strings": { "default": "...", "<code>": "..." } }` : la barre affiche le nom de la langue courante, sinon `default`. Sans ce champ, la barre des packs affiche l'identifiant du pack.
- `nameplates` (array, requis) : un objet par plaque de nom, avec ces champs :
  - `key` (string, requis) : un nom pour la plaque de nom, unique dans le pack. L'identifiant complet de la plaque de nom est `<pack>:<key>`, par exemple `example_pack:night_sky`. Les fichiers de sauvegarde stockent cet identifiant ; gardez donc une clé dès que des joueurs peuvent posséder la plaque de nom, et ne la donnez jamais à une autre plaque de nom.
  - `title` (string, requis) : le texte du titre. Toute langue sans traduction l'affiche.
  - `type` (int, par défaut 0) : le graphisme de la plaque, un numéro de dossier dans les textures de plaques de nom du skin. Dans le skin fourni, les dossiers vont de `Modules/ROActivities/nameplate/Textures/Title/0` à `45`, et le skin ne dessine aucune plaque pour un numéro sans dossier.
  - `rarity` (string, par défaut `Common`) : le nom de la rareté (voir le tableau ci-dessous). Un nom inconnu devient `Common`.
  - `unlock` (object, facultatif) : la condition de déblocage, avec les mêmes champs que dans un `Unlock.json` : `condition`, `values`, `type` et `references` (le guide [Déblocage de partitions](unlockables.md) liste les identifiants de condition). Sans `unlock`, chaque joueur possède la plaque de nom dès le départ.

Valeurs de rareté (les étoiles sont celles du skin fourni) :

| Rareté | Étoiles sur la plaque | Niveau de notification |
| --- | --- | --- |
| <span class="rarity rarity-poor">Poor</span> | 0 | 0 |
| <span class="rarity rarity-common">Common</span> | 0 | 0 |
| <span class="rarity rarity-uncommon">Uncommon</span> | 0 | 1 |
| <span class="rarity rarity-rare">Rare</span> | 1 | 2 |
| <span class="rarity rarity-epic">Epic</span> | 2 | 3 |
| <span class="rarity rarity-legendary">Legendary</span> | 3 | 4 |
| <span class="rarity rarity-mythical">Mythical</span> | 4 | 4 |

Le jeu vérifie les conditions de déblocage sur l'écran de résultats après chaque partie. Quand le joueur en remplit une, le jeu débloque la plaque de nom et affiche une notification. Les conditions de pièces (`ch`, `cs`, `cm`) ne se débloquent que par un achat : dans le skin fourni, le joueur achète la plaque de nom depuis le PC de My Room. Le PC de My Room garde aussi les plaques de nom Mythical verrouillées hors de sa liste jusqu'à ce que le joueur les possède.

```json
{
  "pack": "example_pack",
  "name": { "strings": { "default": "Example Pack", "fr": "Pack d'exemple" } },
  "nameplates": [
    {
      "key": "night_sky",
      "title": "Night Sky Walker",
      "type": 8,
      "rarity": "Rare",
      "unlock": { "condition": "tp", "values": [50] }
    },
    {
      "key": "hello",
      "title": "Hello There"
    }
  ]
}
```

## Étape 3 : Ajouter des traductions

Les traductions sont facultatives. Chaque fichier du dossier `Lang/` du pack contient les titres d'une langue, une ligne par plaque de nom, avec les clés du pack (sans l'identifiant du pack). Le nom du fichier est le code de la langue :

| Fichier | Langue |
| --- | --- |
| `de.json` | Allemand |
| `en.json` | Anglais |
| `es.json` | Espagnol |
| `fr.json` | Français |
| `ja.json` | Japonais |
| `ko.json` | Coréen |
| `nl.json` | Néerlandais |
| `ru.json` | Russe |
| `zh.json` | Chinois |

```json
{
  "night_sky": "Promeneur du ciel nocturne",
  "hello": "Bonjour"
}
```

Une plaque de nom absente du fichier d'une langue affiche son `title` dans cette langue.

## Étape 4 : Tester en jeu

Lancez le jeu et ouvrez le PC de My Room. Dans l'onglet Plaques de nom, choisissez votre pack dans la barre des packs au-dessus de la liste : la liste affiche les plaques de nom du pack, les verrouillées avec leur indication de déblocage. Équipez-en une, puis vérifiez son titre, sa plaque et ses étoiles sur la plaque de nom du joueur. Changez la langue du jeu pour vérifier les traductions.

Le jeu ignore un pack qu'il ne peut pas lire et affiche une notification d'erreur qui le nomme. Un pack sans identifiant `pack` valide, ou avec l'identifiant d'un pack que le jeu a déjà chargé, compte comme illisible. Le jeu ignore seulement la plaque de nom concernée quand elle n'a pas de `key` ou de `title`, ou quand sa clé existe déjà dans le pack, et écrit la raison dans `OpenTaiko.log`.

## Plaques de nom depuis un script

Les scripts Lua utilisent les mêmes identifiants. Chaque méthode qui prend un identifiant de plaque de nom accepte l'identifiant complet sous forme de chaîne ; les plaques de nom fournies acceptent aussi leur numéro. Un script peut donner une plaque de nom qui porte la condition `ig`, qu'aucune partie ne débloque :

```lua
local save = GetSaveFile(0)
local id = "example_pack:night_sky"
if not save:IsNameplateUnlocked(id) then
    save:UnlockNameplate(id)
end
save:ChangeNameplate(id)
```

UnlockNameplate n'affiche aucune notification ; un script qui en veut une l'affiche lui-même. La page [Joueurs et profils](../api/players.md) décrit les méthodes de plaque de nom.

## Exemples

Ces packs montrent des schémas courants. Remplacez les identifiants, les titres et les nombres par les vôtres.

**Une plaque de nom gratuite.** Sans `unlock`, chaque joueur possède la plaque de nom et peut l'équiper tout de suite.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "welcome", "title": "Welcome Aboard", "type": 0, "rarity": "Common" }
  ]
}
```

**Débloquer en jouant (`tp`).** La plaque de nom se débloque après 100 parties. `ap` (parties en bataille contre l'IA), `aw` (victoires en bataille contre l'IA) et `ce` (pièces gagnées au total) utilisent la même disposition à valeur unique.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "regular", "title": "Regular Drummer", "type": 3, "rarity": "Uncommon",
      "unlock": { "condition": "tp", "values": [100] } }
  ]
}
```

**Réussir une chanson de votre pack de partitions (`sp`).** Un pack de partitions peut récompenser la réussite de ses propres chansons. La référence est le champ `id` du `uniqueID.json` de la chanson, et `[3, 3]` demande un full combo (3) en Extreme (3).

```json
{
  "pack": "example_charts",
  "nameplates": [
    { "key": "storm_fc", "title": "Storm Chaser", "type": 33, "rarity": "Epic",
      "unlock": { "condition": "sp", "values": [3, 3],
                  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"] } }
  ]
}
```

**L'acheter avec des pièces (`ch`).** Le prix est l'unique valeur. Dans le skin fourni, le joueur achète la plaque de nom depuis le PC de My Room.

```json
{
  "pack": "example_pack",
  "nameplates": [
    { "key": "golden", "title": "Golden Touch", "type": 11, "rarity": "Rare",
      "unlock": { "condition": "ch", "values": [800] } }
  ]
}
```

**Une récompense que seul un script donne (`ig`).** Aucune partie ne débloque une plaque de nom `ig` ; un script la donne donc avec UnlockNameplate (voir Plaques de nom depuis un script). D'ici là, le PC de My Room affiche pour elle l'indication générique de condition invalide.

```json
{
  "pack": "example_story",
  "nameplates": [
    { "key": "finale", "title": "Curtain Call", "type": 19, "rarity": "Legendary",
      "unlock": { "condition": "ig", "values": [] } }
  ]
}
```

**Un pack de skin avec des traductions.** Un skin range son pack dans `System/<skin>/Nameplates/`, et ses traductions dans `System/<skin>/Nameplates/Lang/`.

```json
{
  "pack": "example_skin",
  "nameplates": [
    { "key": "visitor", "title": "First Visit", "type": 22, "rarity": "Common",
      "unlock": { "condition": "tp", "values": [1] } }
  ]
}
```

```json
{
  "visitor": "初めての訪問"
}
```

## Dépannage et remarques

- Le pack n'apparaît pas : le dossier n'est pas directement dans `Global/Nameplates/`, le fichier ne s'appelle pas `Nameplates.json`, l'identifiant `pack` manque ou contient d'autres caractères que des lettres minuscules, des chiffres et `_`, ou un autre pack utilise déjà le même identifiant. La notification d'erreur nomme le pack.
- Une plaque de nom manque dans la liste : elle n'a pas de `key` ou pas de `title`, ou sa clé répète une clé précédente. `OpenTaiko.log` nomme l'entrée.
- Une traduction ne s'affiche pas : la clé du fichier `Lang` doit être la clé du pack sans l'identifiant du pack (`night_sky` pour `example_pack:night_sky`), et le nom du fichier doit être le code de la langue.
- La plaque n'affiche aucun graphisme : le skin n'a pas de dossier pour ce `type`.
- En ligne, les autres joueurs ne voient le titre d'une plaque de nom de pack que s'ils ont le même pack. Sinon, ils voient la plaque de nom du joueur sans titre.
- Si vous retirez un pack, les joueurs qui portaient une de ses plaques de nom n'affichent aucun titre jusqu'au retour du pack. Leurs déblocages restent dans le fichier de sauvegarde.
- Le lecteur JSON du jeu accepte les commentaires et les virgules finales ; les validateurs stricts les rejettent.
