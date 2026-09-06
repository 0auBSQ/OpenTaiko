<!-- docs/README.md -->

# Documentation OpenTaiko

<span class="badge-new">Version du jeu 0.6.1</span>

<div class="callout warn">Cette documentation est encore en cours de relecture et peut évoluer jusqu’à la sortie.</div>

OpenTaiko dessine presque tous les écrans situés en dehors du cœur du gameplay avec des scripts Lua livrés dans un skin : les menus, la sélection de chansons, les scènes de la chambre et de l'histoire, les arrière-plans, l'écran de résultats. Ce site documente l'API Lua qu'utilisent ces scripts et détaille pas à pas les tâches les plus courantes qu'un créateur de skin souhaite accomplir.

## Par où commencer

- Si vous n'avez pas encore écrit de module, lisez [Fonctionnement des modules](getting-started.md) : l'organisation des dossiers d'un skin, les types de modules et les fonctions de rappel du cycle de vie qu'un script reçoit.
- Pour retrouver une fonction, ouvrez la [référence de l'API](api/) et choisissez une catégorie dans la barre latérale.
- Pour un objectif précis, les guides parcourent pas à pas les [personnages](guides/characters.md), les [puchicharas](guides/puchicharas.md), les [skins et thèmes](guides/skins.md) et le [déblocage de partitions](guides/unlockables.md).

## Ce que vous pouvez construire

| Domaine | Emplacement | Description |
| --- | --- | --- |
| Modules | Le dossier `Modules/` du skin | Des stages (des écrans complets avec leurs propres entrées, leur dessin et leur état : une sélection de chansons, une chambre, une scène d'histoire), des activités (des sous-écrans et des superpositions comme les boîtes de dialogue et les plaques de nom) et des transitions (les écrans de fondu et de chargement entre deux stages). |
| Arrière-plans | Le dossier `Graphics/` du skin | Des scripts qui décorent un écran : l'écran de démarrage, la chambre, le décor et la foule du gameplay, l'écran de résultats. |
| Skins et thèmes | `System/`, à côté du jeu | Un ensemble visuel complet qu'un joueur installe et sélectionne, avec les réglages de thème qu'il expose dans l'écran des options. |
| Personnages et puchicharas | `Global/Characters/` et `Global/PuchiChara/`, partagés par tous les skins | Les danseurs et les petites mascottes qui réagissent pendant la partie. |
| Déblocage de partitions | Le dossier de la chanson, à côté de la partition | Un `Unlock.json` qui garde une chanson verrouillée jusqu'à ce que le joueur l'ait méritée. |

## Sections expérimentales

Certaines pages portent un badge <span class="badge-exp">Expérimental</span> dans la barre latérale. Les fonctionnalités qu'elles décrivent fonctionnent dans la version actuelle, mais leur API peut encore changer d'une version à l'autre sans période de dépréciation. Un skin qui s'appuie sur elles doit indiquer la version du jeu qu'il vise, et vous devez le tester de nouveau après chaque mise à jour.

## Langues

La documentation existe dans toutes les langues livrées avec OpenTaiko ; choisissez-en une avec le sélecteur de langue en haut de la barre latérale. Le site affiche une page en anglais tant qu'aucune traduction n'existe.
