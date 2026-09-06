<!-- guides/unlockables.md -->

# Conditions de déblocage des partitions

Vous pouvez verrouiller une chanson personnalisée derrière une condition (un prix en pièces, la réussite d'autres chansons, un nombre total de parties, un drapeau d'histoire, et ainsi de suite) en plaçant un fichier `Unlock.json` dans le dossier de la chanson, à côté de son `.tja` et de son `uniqueID.json`. Quand le jeu construit la liste des chansons, il lit ce fichier et garde la chanson verrouillée jusqu'à ce que le joueur remplisse la condition. Les conditions s'attachent aux chansons individuelles : `box.def` n'a pas de clé de déblocage, vous ne pouvez donc pas verrouiller un dossier de genre de cette façon.

## Avant de commencer

- Une partition personnalisée qui apparaît déjà dans la liste des chansons : un dossier sous `Songs/` avec un `.tja` et, une fois que le jeu l'a analysé, un `uniqueID.json`.
- Un éditeur de texte qui enregistre en UTF-8.
- Pour les conditions qui référencent d'autres chansons : la valeur `id` du `uniqueID.json` de chaque chanson référencée.
- Le jeu stocke la progression du déblocage par fichier de sauvegarde ; vous devez donc remplir la condition en jeu pour voir la chanson se débloquer. L'option `Ignore Song Unlockables` des paramètres de jeu traite chaque chanson comme débloquée pendant vos tests.

## Étape 1 : Créer le fichier Unlock.json

Créez `Unlock.json` dans le dossier de la chanson. Chaque champ est facultatif et a une valeur par défaut :

- `hidden_index` (int, par défaut 0) : la façon dont la liste des chansons présente la chanson verrouillée (voir le tableau ci-dessous). Le jeu borne les valeurs à 0-3.
- `rarity` (string, par défaut `Common`) : le nom de la rareté (voir le tableau ci-dessous). Elle définit la couleur de l'affichage de la rareté et le niveau de la notification de déblocage. Une valeur vide devient `Common`.
- `condition` (string, par défaut `ch`) : l'identifiant de condition (étape 2).
- `values` (int array, par défaut `[100]`) : les paramètres numériques de la condition.
- `type` (string, par défaut `me`) : la comparaison que les conditions à valeur utilisent : `l` inférieur, `le` inférieur ou égal, `e` égal, `me` supérieur ou égal, `m` supérieur, `d` différent. Les conditions de pièces utilisent toujours `me`.
- `references` (string array, par défaut `[""]`) : identifiants de chanson, noms de genre, noms de créateur ou noms de drapeau, selon la condition.
- `custom_unlock_text` (object, facultatif) : remplace le texte d'indication généré. Un objet localisé `{ "strings": { "default": "...", "ja": "..." } }` ; le jeu utilise la clé de la langue active, puis `default`, et affiche le texte généré si aucune n'existe.

Les clés sont en minuscules avec des tirets bas, exactement comme listées.

Valeurs de l'index masqué :

| Valeur | Dans la liste des chansons |
| --- | --- |
| 0 | Affichée avec une icône de cadenas ; l'aperçu audio est joué |
| 1 | Grisée ; pas d'aperçu |
| 2 | Grisée, avec le titre et l'image d'aperçu masqués |
| 3 | Absente jusqu'au déblocage |

Valeurs de rareté :

| Rareté | Niveau de notification |
| --- | --- |
| <span class="rarity rarity-poor">Poor</span> | 0 |
| <span class="rarity rarity-common">Common</span> | 0 |
| <span class="rarity rarity-uncommon">Uncommon</span> | 1 |
| <span class="rarity rarity-rare">Rare</span> | 2 |
| <span class="rarity rarity-epic">Epic</span> | 3 |
| <span class="rarity rarity-legendary">Legendary</span> | 4 |
| <span class="rarity rarity-mythical">Mythical</span> | 4 |

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "cm",
  "values": [500],
  "type": "me",
  "references": [""],
  "custom_unlock_text": {
    "strings": {
      "default": "Buy this song for 500 coins.",
      "ja": "500コインで解放できます。"
    }
  }
}
```

## Étape 2 : Choisir une condition

| Id | Signification | `values` | `references` |
|----|---------|----------|--------------|
| `ch`, `cs`, `cm` | Achat en pièces. Le jeu évalue les trois identifiants de la même façon (prix en pièces, `type` forcé à `me`, jamais accordé automatiquement). `cm` est l'identifiant destiné aux chansons ; les scripts de skin peuvent lire l'identifiant et s'en servir pour décider où ils proposent l'achat. | `[price]` | inutilisé |
| `ce` | Pièces gagnées au total depuis la création du fichier de sauvegarde | `[coins]` | inutilisé |
| `tp` | Nombre total de parties | `[plays]` | inutilisé |
| `ap` | Nombre de parties en bataille contre l'IA | `[plays]` | inutilisé |
| `aw` | Nombre de victoires en bataille contre l'IA | `[wins]` | inutilisé |
| `sd` | Partitions distinctes ayant atteint un statut de réussite | `[chart count, clear status]` | inutilisé |
| `dp` | Partitions d'une difficulté ayant atteint un statut de réussite | `[difficulty, clear status, chart count]` | inutilisé |
| `lp` | Partitions d'un niveau d'étoiles ayant atteint un statut de réussite | `[level, clear status, chart count]` | inutilisé |
| `sp` | Chansons précises ayant atteint un statut de réussite | `[difficulty, clear status]` par chanson (`-1` = n'importe quelle difficulté) | un identifiant de chanson par paire |
| `sg` | Chansons de genres nommés ayant atteint un statut de réussite | `[song count, clear status]` par genre | un nom de genre par paire |
| `sc` | Partitions de créateurs nommés ayant atteint un statut de réussite | `[chart count, clear status]` par créateur | un nom de créateur par paire |
| `gt` | Déclencheur global (un drapeau on/off nommé dans le fichier de sauvegarde que les scripts définissent) | `[1]` pour ON, `[0]` pour OFF | `[trigger name]` |
| `gc` | Compteur global (un nombre nommé dans le fichier de sauvegarde que les scripts définissent) | `[value]`, comparé avec `type` | `[counter name]` |
| `ig` | Impossible à obtenir : ne se débloque jamais | aucune | aucune |
| `andcomb` | Le joueur doit remplir toutes les conditions enfants (voir les exemples combinés) | `[]` | une condition enfant par entrée, sous forme de chaîne JSON |
| `orcomb` | Le joueur doit remplir au moins une condition enfant (voir les exemples combinés) | `[]` | une condition enfant par entrée, sous forme de chaîne JSON |

Valeurs de statut de réussite (une exigence de statut signifie ce statut ou mieux) :

| Valeur | Statut de réussite |
| --- | --- |
| 0 | jouée |
| 1 | réussite assistée |
| 2 | réussite |
| 3 | full combo |
| 4 | all perfect |

Valeurs de difficulté :

| Valeur | Difficulté |
| --- | --- |
| 0 | Easy |
| 1 | Normal |
| 2 | Hard |
| 3 | Extreme |
| 4 | Extra Extreme |

Pour `dp`, la difficulté 3 compte aussi les partitions Extra Extreme. `dp` et `lp` ne comptent que les partitions ordinaires et sautent les partitions Dan et Tour. `sp` accepte `-1` pour signifier n'importe quelle difficulté.

Le jeu vérifie le nombre de valeurs : `ch`/`cs`/`cm`/`ce`/`tp`/`ap`/`aw`/`gt`/`gc` exigent exactement 1 valeur, `sd` exactement 2, `dp`/`lp` exactement 3, et `sp`/`sg`/`sc` exigent 2 valeurs par référence avec autant de références que de paires. Un mauvais nombre fait échouer la condition avec un message d'erreur en jeu.

Seul un achat explicite dans un écran qui le propose débloque une condition de pièces (le skin fourni propose l'achat des chansons verrouillées depuis la sélection de chansons, quel que soit l'identifiant de pièces que vous utilisez). Le jeu vérifie toutes les autres conditions sur l'écran de résultats après chaque partie ; quand le joueur en remplit une, le jeu ajoute l'identifiant de la chanson au fichier de sauvegarde et affiche une notification.

## Étape 3 : Tester en jeu

Lancez le jeu et ouvrez la sélection de chansons. Selon `hidden_index`, la chanson affiche un cadenas, est grisée, est masquée, ou est absente. Achetez-la ou remplissez la condition, puis confirmez qu'elle reste débloquée après un redémarrage. Activez `Ignore Song Unlockables` dans les paramètres de jeu pour contourner tous les verrous pendant que vous itérez.

## Exemples

Voici les schémas de condition qu'utilisent les chansons fournies, écrits sous forme de fichiers `Unlock.json`. Remplacez les identifiants, les noms et les nombres par les vôtres.

**Acheter la chanson avec des pièces (`cm`).** Le schéma le plus courant. Le prix est l'unique valeur, et le jeu ignore `type`. `hidden_index` 0 garde la chanson visible pour que le joueur puisse la trouver et l'acheter. L'indication générée mentionne déjà le prix ; vous n'avez donc besoin d'aucun `custom_unlock_text`.

```json
{
  "hidden_index": 0,
  "rarity": "Uncommon",
  "condition": "cm",
  "values": [500]
}
```

**Nombre total de parties (`tp`).** Les chapitres fournis ouvrent leurs chansons l'une après l'autre avec des nombres de parties croissants (10, 15, 20 et ainsi de suite), pour que le joueur ait toujours une prochaine chanson à sa portée. `ap` (parties en bataille contre l'IA), `aw` (victoires en bataille contre l'IA) et `ce` (pièces gagnées au total) utilisent la même disposition à valeur unique.

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "tp",
  "values": [50]
}
```

**Réussir une chanson précise (`sp`).** Le schéma que la plupart des chansons fournies utilisent pour une suite ou un remix. `-1` comme difficulté accepte une réussite dans n'importe quelle difficulté ; l'identifiant est le champ `id` du `uniqueID.json` de la chanson référencée (le jeu génère un identifiant de 64 caractères la première fois qu'il analyse une chanson qui n'en a pas, et les chansons fournies peuvent porter un identifiant écrit à la main). L'exemple se débloque une fois que le joueur a réussi la chanson référencée.

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "sp",
  "values": [-1, 2],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"]
}
```

**Faire un full combo sur plusieurs chansons (`sp`).** Chaque chanson supplémentaire ajoute une paire de valeurs et une référence, et le joueur doit remplir la condition pour chaque chanson référencée. L'exemple exige un full combo (3) sur deux chansons.

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sp",
  "values": [-1, 3, -1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU", "4aJ2I19XyEG2cEA9tlmdnZSz2H43OKsVBPLi52UfRhSjDGNgTGGqhmqkbWfPDoyw"]
}
```

**Réussir des chansons d'un genre (`sg`).** Chaque entrée est `[song count, clear status]` avec le nom du genre dans `references`. Le genre est celui que le jeu enregistre quand le joueur joue la chanson : le `#GENRE` du `box.def` du dossier englobant, ou le `#GENRE` de la partition elle-même quand le dossier n'en a pas. Les chapitres fournis s'en servent pour leurs medleys. L'exemple se débloque une fois que le joueur a réussi 10 chansons distinctes du genre.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sg",
  "values": [10, 2],
  "references": ["OpenTaiko Chapter II"]
}
```

**Réussir des partitions d'un créateur (`sc`).** La même disposition, avec des noms `#NOTESDESIGNER` comme références. L'exemple exige 5 partitions réussies d'un créateur ; un second créateur ajoute une autre paire de valeurs et une autre référence.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "sc",
  "values": [5, 2],
  "references": ["bol"]
}
```

**Réussir un certain nombre de partitions distinctes (`sd`).** Compte chaque partition que le fichier de sauvegarde enregistre au statut donné ou mieux. L'exemple exige 10 full combos.

```json
{
  "hidden_index": 2,
  "rarity": "Rare",
  "condition": "sd",
  "values": [10, 3]
}
```

**Réussir des partitions d'une difficulté ou d'un niveau d'étoiles (`dp` / `lp`).** `dp` compte les partitions d'une difficulté, `lp` les partitions d'un niveau d'étoiles. Le premier exemple exige 20 partitions Extreme réussies, le second une partition 7 étoiles réussie.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "dp",
  "values": [3, 2, 20]
}
```

```json
{
  "hidden_index": 1,
  "rarity": "Common",
  "condition": "lp",
  "values": [7, 2, 1]
}
```

**L'une ou l'autre de deux conditions (`orcomb`).** `values` est vide et chaque entrée de `references` est une condition enfant complète (`condition`, `type`, `values`, `references`) écrite sous forme de chaîne JSON. Les chansons fournies s'en servent pour offrir un raccourci : jouer assez de chansons, ou payer. `orcomb` utilise la branche satisfaite la moins chère quand il calcule le prix d'un achat.

```json
{
  "hidden_index": 0,
  "rarity": "Common",
  "condition": "orcomb",
  "values": [],
  "references": [
    "{\"condition\": \"tp\", \"type\": \"me\", \"values\": [100]}",
    "{\"condition\": \"cm\", \"type\": \"me\", \"values\": [200]}"
  ]
}
```

**Plusieurs conditions à la fois (`andcomb`).** La même disposition ; le joueur doit remplir chaque condition enfant. Les enfants peuvent eux-mêmes être des combinaisons, et `andcomb` additionne les prix en pièces à l'intérieur d'une combinaison. L'exemple exige la réussite d'une chanson et la réussite de n'importe quelle partition 7 étoiles.

```json
{
  "hidden_index": 1,
  "rarity": "Uncommon",
  "condition": "andcomb",
  "values": [],
  "references": [
    "{\"condition\": \"sp\", \"type\": \"me\", \"values\": [-1, 2], \"references\": [\"4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU\"]}",
    "{\"condition\": \"lp\", \"type\": \"me\", \"values\": [7, 2, 1], \"references\": [\"\"]}"
  ]
}
```

**Drapeau d'histoire avec une indication personnalisée (`gt`).** `gt` lit un drapeau on/off nommé dans le fichier de sauvegarde. Des scripts Lua (scènes d'histoire, cinématiques) définissent les drapeaux ; n'utilisez donc `gt` que lorsqu'un script que vous contrôlez définit le drapeau. `values` vaut `[1]` pour ON ou `[0]` pour OFF ; `references` contient le nom du drapeau. Un déblocage d'histoire n'a pas d'indication générée explicite ; `custom_unlock_text` est donc utile ici. Vous pouvez fournir n'importe quelle clé de langue ; `default` est la valeur de repli.

```json
{
  "hidden_index": 3,
  "rarity": "Legendary",
  "condition": "gt",
  "values": [1],
  "references": ["story_done"],
  "custom_unlock_text": {
    "strings": {
      "default": "Finish the story to unlock this song.",
      "ja": "ストーリーをクリアするとこの曲が解放されます。"
    }
  }
}
```

**Chanson secrète avec une énigme.** Les chansons secrètes fournies combinent un `hidden_index` élevé (le titre et l'aperçu restent masqués, ou la chanson reste absente jusqu'au déblocage) avec un `custom_unlock_text` qui suggère la condition au lieu de l'énoncer. N'importe quelle condition fonctionne en dessous ; l'exemple cache une exigence de full combo derrière une énigme.

```json
{
  "hidden_index": 2,
  "rarity": "Epic",
  "condition": "sp",
  "values": [-1, 3],
  "references": ["4xtDLgcLGqMLaXd7mq344TtjhhR4dopA4ykf5wTCfFq8UOdHFBlwpW2lSJ9ndcVU"],
  "custom_unlock_text": {
    "strings": {
      "default": "Perfect the storm that came before this one."
    }
  }
}
```

## Dépannage et remarques

- La chanson n'est pas verrouillée : le fichier ne s'appelle pas `Unlock.json`, il n'est pas dans le dossier qui contient le `.tja` et le `uniqueID.json`, ou `Ignore Song Unlockables` est activé.
- L'indication dit que la condition est invalide : le nombre de valeurs ne correspond pas à la condition (voir l'étape 2), ou `sp`/`sg`/`sc` ont un nombre de références différent du nombre de paires de valeurs.
- Une clé n'a aucun effet : les clés sont `hidden_index`, `rarity`, `condition`, `values`, `type`, `references`, `custom_unlock_text`, toutes en minuscules. Le jeu ignore tout le reste et applique la valeur par défaut.
- `sp` ne se débloque jamais : la référence doit être l'identifiant du `uniqueID.json` de la chanson cible (un titre ou un nom de fichier ne correspond jamais), et le joueur doit pouvoir atteindre la difficulté et le statut (le statut 3 est un full combo, une partition que le joueur a seulement réussie ne compte donc pas).
- `sg` ne se débloque jamais : le nom du genre doit correspondre au genre que le jeu enregistre pour les chansons jouées (le `#GENRE` du `box.def` du dossier, ou le `#GENRE` de la partition quand il n'y a pas de genre de dossier).
- `gt`/`gc` ne se débloquent jamais : rien ne définit le drapeau nommé. La partition ne peut pas le définir elle-même.
- Rien ne débloque `ig` en jouant. Utilisez-le uniquement pour du contenu qu'un autre système accorde.
- `custom_unlock_text` doit être un objet `{ "strings": { ... } }` ; une simple chaîne ne fonctionne pas.
- Les fichiers `Unlock.json` fournis contiennent des virgules finales. L'analyseur JSON du jeu les accepte ; les validateurs stricts les rejettent.
