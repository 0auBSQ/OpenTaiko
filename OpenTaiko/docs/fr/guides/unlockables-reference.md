<!-- guides/unlockables-reference.md -->

# Référence des déblocages

Les chansons, les personnages, les puchicharas et les plaques de nom utilisent un même format de condition de déblocage. Les chansons, les personnages et les puchicharas le placent dans un fichier `Unlock.json`, les plaques de nom dans l'objet `unlock` de leur entrée dans `Nameplates.json`. Les guides expliquent où va chaque fichier : [Déblocage de partitions](unlockables.md), [Ajouter des personnages](characters.md), [Ajouter des puchicharas](puchicharas.md) et [Ajouter des plaques de nom](nameplates.md). Cette page liste les champs, les identifiants de condition et les nombres qu'ils utilisent.

## Champs de la condition

| Champ | Type | Signification |
| --- | --- | --- |
| `condition` | string | L'identifiant de condition (voir Identifiants de condition). |
| `values` | int array | Les paramètres numériques de la condition. |
| `type` | string | La comparaison que les conditions à valeur utilisent (voir Types de comparaison). |
| `references` | string array | Identifiants de chanson, noms de genre, noms de créateur ou noms de drapeau, selon la condition. |

Le `Unlock.json` d'une chanson ajoute ses propres champs (`hidden_index`, `rarity`, `custom_unlock_text`), que décrit le guide [Déblocage de partitions](unlockables.md).

## Identifiants de condition

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
| `andcomb` | Le joueur doit remplir toutes les conditions enfants (voir les exemples combinés du guide [Déblocage de partitions](unlockables.md)) | `[]` | une condition enfant par entrée, sous forme de chaîne JSON |
| `orcomb` | Le joueur doit remplir au moins une condition enfant (voir les exemples combinés du guide [Déblocage de partitions](unlockables.md)) | `[]` | une condition enfant par entrée, sous forme de chaîne JSON |

Le jeu vérifie le nombre de valeurs : `ch`/`cs`/`cm`/`ce`/`tp`/`ap`/`aw`/`gt`/`gc` exigent exactement 1 valeur, `sd` exactement 2, `dp`/`lp` exactement 3, et `sp`/`sg`/`sc` exigent 2 valeurs par référence avec autant de références que de paires. Un mauvais nombre fait échouer la condition avec un message d'erreur en jeu.

Seul un achat explicite dans un écran qui le propose débloque une condition de pièces. Le jeu vérifie toutes les autres conditions sur l'écran de résultats après chaque partie ; quand le joueur en remplit une, le jeu débloque l'objet et affiche une notification.

## Types de comparaison

| `type` | Comparaison |
| --- | --- |
| `l` | inférieur |
| `le` | inférieur ou égal |
| `e` | égal |
| `me` | supérieur ou égal (par défaut) |
| `m` | supérieur |
| `d` | différent |

Les conditions de pièces utilisent toujours `me`, quelle que soit la valeur de `type`.

## Valeurs de statut de réussite

Une exigence de statut signifie ce statut ou mieux.

| Valeur | Statut de réussite |
| --- | --- |
| 0 | jouée |
| 1 | réussite assistée |
| 2 | réussite |
| 3 | full combo |
| 4 | all perfect |

## Valeurs de difficulté

| Valeur | Difficulté |
| --- | --- |
| 0 | Easy |
| 1 | Normal |
| 2 | Hard |
| 3 | Extreme |
| 4 | Extra Extreme |

Pour `dp`, la difficulté 3 compte aussi les partitions Extra Extreme. `dp` et `lp` ne comptent que les partitions ordinaires et sautent les partitions Dan et Tour. `sp` accepte `-1` pour signifier n'importe quelle difficulté.

## La valeur des mods de jeu

Le fichier de sauvegarde conserve les meilleures parties d'un joueur par partition, difficulté et mods de jeu. La valeur des mods de jeu compacte les mods d'une partie dans 8 octets, en commençant par l'octet le plus bas : une partie sans mods donne 8925478921.

| Octet | Mod | Valeurs |
| --- | --- | --- |
| 1 (le plus bas) | Vitesse de défilement | 0 à 99 : 9 vaut 1,0x, chaque pas vaut 0,1x |
| 2 | Furtif | 0 Off, 1 Doron, 2 Stealth, 3 Hidden, 4 Flashlight |
| 3 | Aléatoire | 0 Off, 1 Random, 2 Mirror, 3 SuperRandom, 4 MirrorRandom |
| 4 | Vitesse de la chanson | 2 à 200 : 20 vaut 1,0x, chaque pas vaut 0,05x |
| 5 | Fenêtre de jugement | 0 Loose, 1 Lenient, 2 Normal, 3 Strict, 4 Rigorous |
| 6 | Mod de jugement | 0 désactivé, 1 Just, 2 Safe |
| 7 | Inutilisé | 0 |
| 8 (le plus haut) | Mod fun | 0 None, 1 Avalanche, 2 Minesweeper, 3 Dynamic Beat |

Les mods spéciaux (Flawless, Timed) ne font pas partie de la valeur.
