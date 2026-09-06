<!-- guides/puchicharas.md -->

# Ajouter un puchichara

Un puchichara est le petit compagnon qui rebondit à côté de la jauge pendant la partie. Chaque puchichara est un dossier sous `Global/PuchiChara/` dans le dossier d'installation du jeu, contenant une feuille de sprites à deux frames et quelques fichiers JSON facultatifs. Vous n'écrivez aucun code : créez le dossier avec les bons noms de fichiers et le jeu le prend en compte au prochain démarrage.

Compatibilité : OpenTaiko 0.6.1 charge toujours sans modification les puchicharas créés pour la version 0.6.0.

## Avant de commencer

- OpenTaiko 0.6.1 installé. Le jeu lit les puchicharas depuis `Global/PuchiChara/` à côté de l'exécutable du jeu, et tous les skins partagent le dossier.
- Un éditeur d'images qui exporte du PNG avec transparence.
- Un éditeur de texte pour les fichiers JSON.
- Facultatif : un court extrait `.ogg` pour `Welcome.ogg`.

## Étape 1 : Créer le dossier

Le jeu liste les sous-dossiers de `Global/PuchiChara/` au démarrage et traite chacun d'eux comme un puchichara. Le nom du dossier est l'identité : le jeu l'écrit dans le fichier de sauvegarde quand le joueur sélectionne le puchichara et enregistre le déblocage sous ce nom. Choisissez un nom stable : après un renommage, les sélections existantes retombent sur le premier dossier et le déblocage enregistré ne correspond plus. Les dossiers fournis utilisent un préfixe de tri, par exemple `00 - None`, `01a - OpenTaiko-Kun` et `02 - Bol`. Le jeu ne trie pas la liste ; le préfixe garde donc l'ordre du système de fichiers prévisible. Le premier dossier est la solution de repli quand une sauvegarde référence un dossier qui n'existe plus ; gardez donc `00 - None` en premier.

```
Global/PuchiChara/
    00 - None/
    01a - OpenTaiko-Kun/
    02 - Bol/
    99 - MyMascot/          <-- votre nouveau dossier
```

## Étape 2 : Dessiner la feuille de sprites (Chara.png)

Le jeu dessine le compagnon à partir de `Chara.png`, une feuille de sprites horizontale. La disposition des frames vient de la valeur de skin `Game_PuchiChara`, qui vaut par défaut `256,256,2` (largeur de frame, hauteur de frame, nombre de frames) ; les feuilles fournies font donc 512x256 pixels : deux frames de 256x256 côte à côte, la frame 0 à gauche. Pendant la partie, le jeu alterne entre les frames et ajoute un rebond vertical (rebond au repos dans les menus, rebond synchronisé sur le rythme en jeu) ; les deux frames doivent donc être deux poses du même personnage. Utilisez un fond transparent. Si `Chara.png` est absent, l'entrée se charge tout de même mais ne dessine rien.

Les dossiers fournis contiennent aussi un `Chara.xcf` (source GIMP) et un `PuchiConfig.txt`. Le jeu ne lit ni l'un ni l'autre.

```ini
Chara.png : 512 x 256 PNG, transparency
  +-----------------+-----------------+
  |    frame 0      |    frame 1      |
  |   256 x 256     |   256 x 256     |
  +-----------------+-----------------+
```

## Étape 3 : Écrire Metadata.json

Créez `Metadata.json` avec ces champs :

- `name`, `author`, `description` : chacun une chaîne simple ou un objet localisé `{ "strings": { "default": "...", "ja": "...", ... } }` où `default` est la valeur de repli et les autres clés sont des codes de langue du jeu.
- `rarity` : l'une des valeurs `Poor`, `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythical`. La rareté définit la couleur et le niveau de la notification de déblocage. Chaque rareté a un multiplicateur de pièces de 1 ; elle ne change donc pas les gains. Une valeur inconnue se comporte comme `Common`.

Si le fichier est absent, l'entrée se charge avec le nom `(None)`, la rareté `Common` et l'auteur `(None)`.

```json
{
    "name": {
        "strings": {
            "default": "MyMascot",
            "ja": "マイマスコット"
        }
    },
    "rarity": "Rare",
    "description": {
        "strings": {
            "default": "A friendly companion.\nWaves during play."
        }
    },
    "author": "YourName"
}
```

## Étape 4 (facultatif) : Ajouter Effects.json

`Effects.json` donne au puchichara des effets de gameplay. Tous les champs sont désactivés par défaut quand le fichier est absent :

- `allpurple` (bool) : les grosses notes don et ka deviennent des notes violettes qui acceptent l'un ou l'autre tambour.
- `autoroll` (int) : frappes automatiques par seconde sur les roulements et les ballons. Toute valeur supérieure à 0 met le multiplicateur de pièces à 0.
- `showadlib` (bool) : affiche les notes ADLIB cachées. Multiplie les pièces par 0,9.
- `splitlane` (bool) : dessine les notes don et ka sur des voies séparées.

Omettez le fichier pour un compagnon purement cosmétique.

```json
{
    "allpurple": false,
    "autoroll": 0,
    "showadlib": false,
    "splitlane": false
}
```

## Étape 5 (facultatif) : Ajouter Welcome.ogg et Render.png

- `Welcome.ogg` : un extrait vocal que le jeu charge dans le groupe de sons Voix. L'écran de la chambre intégré au jeu le joue quand le joueur sélectionne le puchichara ; les stages Lua ne peuvent pas y accéder, si bien qu'un skin doté de son propre écran de chambre ne le joue pas.
- `Render.png` : une image fixe en taille réelle que les stages Lua peuvent dessiner comme portrait (la texture `render` d'une entrée de `PUCHICHARALIST`). C'est une image unique de taille quelconque. Aucun des puchicharas fournis n'en inclut.

Les deux fichiers sont facultatifs.

```
MyMascot/
    Chara.png       (requis, la feuille de sprites animée)
    Metadata.json   (nom, rareté, auteur, description)
    Effects.json    (effets de gameplay facultatifs)
    Unlock.json     (condition de déblocage facultative)
    Welcome.ogg     (extrait vocal facultatif)
    Render.png      (portrait facultatif)
```

## Étape 6 (facultatif) : Ajouter une condition de déblocage (Unlock.json)

Sans `Unlock.json`, le puchichara est disponible immédiatement. Pour le verrouiller, ajoutez un `Unlock.json` avec les champs `condition`, `type`, `values` et `references` ; le format et les identifiants de condition sont les mêmes que pour les chansons et les personnages (voir le guide sur le déblocage). L'exemple ci-dessous se débloque une fois que le joueur a gagné 500 pièces au total. Exemples fournis : OpenTaiko-Kun coûte 100 pièces (`"condition": "ch"`), Bol exige 20 partitions réussies du créateur `bol` (`"condition": "sc"`), et Tinyfox exige une chanson jouée dans le genre `Project Outfox Serenity` (`"condition": "sg"`).

Le joueur achète les conditions de pièces dans l'écran de la chambre. Le jeu vérifie les autres conditions sur l'écran de résultats après chaque partie ; quand le joueur en remplit une, il ajoute le puchichara à la liste des déblocages du fichier de sauvegarde et affiche une notification.

```json
{
    "condition": "ce",
    "type": "me",
    "values": [
        500
    ]
}
```

## Étape 7 : Redémarrer et le sélectionner

Redémarrez le jeu (ou rechargez le skin depuis les paramètres) pour que le jeu reconstruise la liste. Ouvrez l'écran de la chambre et choisissez la nouvelle entrée dans la liste des puchicharas. Une fois sélectionné, il apparaît pendant la partie à côté de la jauge. Si aucun compagnon n'apparaît pendant la partie, vérifiez que l'option `Draw PuchiChara` des paramètres système est activée.

## Dépannage et remarques

- Les noms de fichiers sont exacts : `Chara.png`, `Metadata.json`, `Effects.json`, `Unlock.json`, `Welcome.ogg`, `Render.png`. Le jeu ignore un fichier mal nommé et applique la valeur par défaut.
- Les fichiers de sauvegarde et les enregistrements de déblocage stockent le nom du dossier tel quel. Renommer un dossier fait retomber les sélections existantes sur le premier dossier.
- Le jeu découpe la feuille de sprites avec la taille de frame `Game_PuchiChara` du skin (par défaut `256,256,2`). Il découpe une feuille d'une autre taille avec les mêmes valeurs ; les frames en ressortent rognées ou décalées. Si un skin remplace `Game_PuchiChara`, adaptez-vous à cette valeur. Le skin fourni ne la remplace pas.
- `autoroll` supérieur à 0 annule les gains de pièces et `showadlib` les réduit à 0,9 ; un compagnon cosmétique qui définit l'un ou l'autre réduit donc les gains de son joueur.
- Les fichiers JSON fournis contiennent des virgules finales. L'analyseur JSON du jeu les accepte ; les validateurs stricts les rejettent.
- Le jeu construit la liste une fois au démarrage et au rechargement du skin. Un dossier ajouté pendant que le jeu tourne apparaît après le prochain démarrage ou rechargement du skin.
