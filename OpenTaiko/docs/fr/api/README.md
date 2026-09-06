<!-- docs/api/README.md -->

# Référence de l'API

<span class="badge-new">Version du jeu 0.6.1</span>

La référence de chaque globale que l'environnement Lua d'OpenTaiko expose à un skin. Choisissez une catégorie dans la barre latérale, ou commencez par [Modules et cycle de vie](activities.md) si vous n'avez pas encore écrit de module.

## Comment lire une signature

Chaque entrée montre la fonction telle que vous l'appelez depuis Lua.

- Un deux-points signifie que vous appelez la fonction sur une valeur, et Lua passe cette valeur comme `self` implicite : vous appelez `tex:Draw(x, y)` sur une texture que vous avez chargée plus tôt.
- Un point ou un nom nu désigne un appel simple, comme `GetSaveFile(0)`.
- Le type après la flèche est ce que l'appel renvoie : `TEXTURE:CreateTexture(path) -> texture` renvoie un handle que vous conservez et dessinez plus tard.

Les noms des globales sont en majuscules (`TEXTURE`, `SOUND`, `INPUT`). L'environnement Lua les fournit dans chaque script de module ; vous ne les créez jamais.

## Catégories

| Catégorie | Contenu |
| --- | --- |
| [Modules et cycle de vie](activities.md) | Les fonctions de rappel qu'un module reçoit, et les assistants pour les activités, arrière-plans, transitions et compteurs. |
| [Graphismes et texte](graphics.md) | Textures, canvas, découpage, rendu de texte, vidéo, couleurs et dégradés. |
| [Audio](audio.md) | Chargement et lecture des sons. |
| [Entrées](input.md) | Clavier, tambour et pointeur, et saisie de texte à l'écran. |
| [Données et persistance](data.md) | Données qui survivent à un redémarrage, chargement de JSON et d'INI, ressources partagées. |
| [Chansons et partitions](songs.md) | La liste des chansons, les nœuds de chanson et les partitions, les scores, et la construction de dan (examens). |
| [Joueurs et profils](players.md) | Fichiers de sauvegarde, plaques de nom, personnages, puchicharas, état de la partie, thèmes et langue. |
| [Mathématiques](math.md) | Vecteurs, matrices et quaternions. |
| [Réseau en ligne](networking.md) | La globale `NET` pour les sessions OpenTaiko Online. Actuellement expérimental. |
| [Moteur 3D : monde du rastériseur](3d.md) | Scènes, objets, modèles, lumières, caméras, sprites, champs de hauteur et cibles de rendu. Actuellement expérimental. |
| [Moteur 3D : monde du path tracer](3d-raytrace.md) | Le path tracer : matériaux, primitives analytiques et dégradé de ciel. Actuellement expérimental. |
| [Moteur 3D : physique](3d-physics.md) | Monde physique, corps et véhicules, colliders, raycasts et recherche de chemin. Actuellement expérimental. |

## Le badge Expérimental

Les fonctions marquées <span class="badge-exp">Expérimental</span> fonctionnent aujourd'hui mais peuvent changer d'une version à l'autre sans période de dépréciation. Le moteur 3D et le réseau en ligne portent actuellement ce badge ; le plan est que le moteur 3D quitte le statut expérimental avec la version 1.0. Si un skin dépend de fonctions expérimentales, testez-le de nouveau après chaque mise à jour et indiquez la version du jeu qu'il vise.
