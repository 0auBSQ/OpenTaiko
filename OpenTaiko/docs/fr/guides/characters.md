<!-- guides/characters.md -->

# Ajouter un personnage

Un personnage est un dossier sous `Global/Characters/` dans le dossier d'installation du jeu. Chaque sous-dossier que le jeu y trouve devient un personnage sélectionnable. Un dossier contient un `Metadata.json` (nom, rareté, auteur), un `CharaConfig.txt` (positions et minutage des animations), le contenu des animations, et éventuellement `Effects.json`, `Unlock.json`, `Palettes.json` et des extraits vocaux. Le contenu des animations est soit des dossiers de frames PNG numérotées, que le script de personnage intégré au jeu rend, soit tout ce qu'un `Script.lua` propre au personnage choisit de dessiner (le modèle 3D fourni dessine un modèle glTF).

Compatibilité : OpenTaiko 0.6.1 charge toujours sans modification les personnages créés pour la version 0.6.0. Cette page décrit la structure actuelle ; utilisez-la pour les nouveaux personnages.

## Avant de commencer

- OpenTaiko 0.6.1 installé. Le jeu lit les personnages depuis `Global/Characters/` à côté de l'exécutable du jeu, et tous les skins les partagent.
- Un éditeur de texte pour les fichiers JSON et de type INI.
- Pour un personnage 2D : les dessins exportés en frames PNG numérotées (`0.png`, `1.png`, ...) avec un fond transparent, un dossier par état d'animation.
- Pour un personnage 3D : un `model.glb` (glTF binaire) contenant les clips d'animation, et une image fixe `Render.png`.
- Les dossiers fournis `01 - Template` (2D) et `01 - Template3D`. Copiez l'un d'eux comme point de départ.

## Étape 1 : Comprendre la découverte, l'ordre et l'identité

Au démarrage, le jeu liste les sous-dossiers de `Global/Characters/` et crée un personnage par dossier, dans l'ordre renvoyé par le système de fichiers. Le jeu ne trie pas la liste ; les dossiers fournis portent donc un préfixe numérique (`00 - None`, `01 - Template`, `02 - Student (A)`, ...) pour garder un ordre prévisible. Gardez `00 - None` en premier : l'indice 0 est l'emplacement vide et la solution de repli quand un personnage sauvegardé est absent.

Les fichiers de sauvegarde stockent le personnage choisi par nom de dossier (`characterName`) et le résolvent de nouveau en indice à chaque démarrage. Ajouter ou retirer d'autres dossiers ne casse jamais une sélection sauvegardée, mais renommer un dossier fait retomber sur `00 - None` les sauvegardes qui le référençaient. Deux personnages peuvent partager un nom d'affichage ; le nom de dossier doit être unique.

Le jeu énumère les personnages une fois au démarrage et de nouveau au rechargement du skin ; un dossier ajouté pendant que le jeu tourne apparaît après le prochain démarrage ou rechargement du skin.

## Étape 2 : Créer le dossier et Metadata.json

Créez un dossier tel que `30 - MyChara` et ajoutez `Metadata.json` :

- `name` : nom d'affichage. Soit une chaîne simple, soit un objet localisé `{ "strings": { "default": "...", "ja": "...", ... } }`. `default` est la valeur de repli ; les autres clés sont des codes de langue du jeu.
- `rarity` : l'une des valeurs `Poor`, `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Mythical`. La rareté ne contrôle que la couleur et le niveau de la notification de déblocage ; chaque rareté a un multiplicateur de pièces de 1.
- `author` : chaîne simple ou objet localisé.
- `description` : facultatif, chaîne simple ou objet localisé.
- `speechtext` : facultatif, tableau de six objets localisés que l'écran de résultats affiche dans la bulle de dialogue du personnage. Le jeu choisit l'entrée selon le résultat, dans cet ordre : échec avec jauge basse, échec avec jauge à 40 % ou plus, réussite, réussite avec jauge pleine, full combo, all perfect. Si vous en fournissez moins de six, le jeu répète la dernière.

Si `Metadata.json` est absent, le personnage se charge tout de même avec le nom `(None)`, la rareté `Common` et l'auteur `(None)`.

```json
{
  "name": {
    "strings": {
      "default": "My Character",
      "ja": "マイキャラ"
    }
  },
  "rarity": "Common",
  "author": {
    "strings": {
      "default": "Your Name"
    }
  }
}
```

## Étape 3 (approche 2D) : Ajouter les dossiers de frames

Quand le dossier n'a pas de `Script.lua`, le jeu rend le personnage avec son script intégré (`CharaScript.lua` dans le dossier d'installation du jeu). Ce script associe chaque état d'animation à un sous-dossier et y charge `0.png`, `1.png`, `2.png`, ... Le chargement s'arrête au premier indice manquant ; la numérotation doit donc être contiguë.

| État d'animation | Dossier |
|---|---|
| Game/Normal, Game/Clear, Game/Max | `Normal`, `Clear`, `Clear_Max` |
| Game/Gogo, Game/Gogo_Max | `GoGo`, `GoGo_Max` |
| Game/Miss, Game/Miss_Down | `Miss`, `MissDown` |
| Game/10combo, Game/10combo_Max | `10combo`, `10combo_Max` |
| Game/Cleared, Game/Failed | `Cleared`, `Failed` |
| Game/Clear_In, Game/Clear_Out | `Clearin`, `ClearOut` |
| Game/Max_In, Game/Max_Out | `Soulin`, `SoulOut` |
| Game/Miss_In, Game/Miss_Down_In, Game/Return | `MissIn`, `MissDownIn`, `Return` |
| Game/GoGoStart, Game/GoGoStart_Clear, Game/GoGoStart_Max | `GoGoStart`, `GoGoStart_Clear`, `GoGoStart_Max` |
| Game/Balloon_Breaking, Game/Balloon_Broke, Game/Balloon_Miss | `Balloon_Breaking`, `Balloon_Broke`, `Balloon_Miss` |
| Game/Kusudama_Breaking, Game/Kusudama_Broke, Game/Kusudama_Miss, Game/Kusudama_Idle | `Kusudama_Breaking`, `Kusudama_Broke`, `Kusudama_Miss`, `Kusudama_Idle` |
| Game/Tower/Standing, Climbing, Running, Clear, Fail (et les variantes `_Tired`) | `Tower_Char/Standing`, `Tower_Char/Climbing`, `Tower_Char/Running`, `Tower_Char/Clear`, `Tower_Char/Fail` (plus `Tower_Char/Standing_Tired` et ainsi de suite) |
| Menu/Wait, Menu/Start, Menu/Normal, Menu/Select | `Menu_Wait`, `Menu_Start`, `Menu_Loop`, `Menu_Select` |
| Entry/Normal, Entry/Jump | `Title_Normal`, `Title_Entry` |
| Result/Normal, Result/Clear, Result/Failed_In, Result/Failed | `Result_Normal`, `Result_Clear`, `Result_Failed_In`, `Result_Failed` |

Le script intégré lit deux images fixes à la racine du dossier : `Render.png` (le portrait en taille réelle, dessiné partout où le jeu demande le type d'animation Render, par exemple dans la chambre) et `Preview.png` (la vignette ; en son absence, le script utilise `Normal/0.png`).

Les états manquants retombent sur un autre état, si bien qu'un personnage peut n'en fournir qu'un sous-ensemble. La chaîne de repli est : Clear -> Normal, Max -> Clear, Miss -> Normal, Miss_Down -> Miss, Gogo -> Normal, Gogo_Max -> Gogo, 10combo_Max -> 10combo, GoGoStart_Clear -> GoGoStart, GoGoStart_Max -> GoGoStart_Clear, les états Tower `_Tired` -> leur état normal, Tower/Fail -> Tower/Standing_Tired, Kusudama_Idle -> Normal, Menu/Wait -> Gogo, Menu/Start et Menu/Select et Entry/Jump -> 10combo, Menu/Normal et Entry/Normal et Result/Normal -> Normal, Result/Clear -> Clear, Result/Failed_In -> Miss_In, Result/Failed -> Miss. Les états sans repli (par exemple Cleared, Failed, Return, les états de ballon) ne dessinent rien quand ils sont absents. Le minimum pour un personnage fonctionnel est `Normal/0.png`.

```
30 - MyChara/
  Metadata.json
  CharaConfig.txt
  Render.png
  Normal/0.png 1.png 2.png ...
  Clear/0.png ...
  GoGo/0.png ...
  Miss/0.png ...
  Menu_Loop/0.png ...
  Result_Clear/0.png ...
  Sounds/                (extraits vocaux facultatifs, voir l'étape 6)
```

## Étape 4 : Écrire CharaConfig.txt

`CharaConfig.txt` est un fichier texte `Key=Value` ; les lignes commençant par `;` sont des commentaires. Le script intégré lit ces clés (le modèle 3D fourni lit aussi les clés de position) :

- `Chara_Resolution=W,H` (par défaut `1280,720`) : la résolution pour laquelle vous définissez les coordonnées ci-dessous. Le jeu met les positions à l'échelle de cette résolution vers celle du skin au moment du dessin.
- `Chara_LegacyMode` (par défaut `1`) : conserve l'ancrage et les corrections de décalage de la 0.6.0. Les personnages portés depuis d'anciennes versions en dépendent.
- `Game_Chara_X=...` / `Game_Chara_Y=...` : position en jeu ; le script utilise la première valeur de chaque liste. `Game_Chara_Offset=X,Y` est une forme alternative.
- `Game_Chara_X_AI=...` / `Game_Chara_Y_AI=...` : une valeur par joueur pour la bataille contre l'IA. Quand les deux clés sont présentes, elles remplacent la position de bataille contre l'IA du skin pour ce personnage.
- `Game_Chara_Balloon_X` / `Game_Chara_Balloon_Y`, `Game_Chara_Kusudama_X` / `Game_Chara_Kusudama_Y` : positions pendant les séquences de ballon et de kusudama (première valeur utilisée). `Game_Chara_Balloon_Offset`, `Game_Chara_Kusudama_Offset` et `Game_Chara_Tower_Offset` prennent une paire `X,Y`.
- `Menu_Offset=X,Y`, `Menu_Chara_Scale`, `Result_Offset=X,Y`, `Heya_Chara_Render_Offset=X,Y` : décalages pour le menu, les résultats et le rendu dans la chambre.
- `Game_Chara_Motion_<State>=0,1,2,...` : l'ordre de lecture des frames d'un état, sous forme d'indices de frame à partir de 0. Quand elle est omise, les frames sont lues dans l'ordre des fichiers. Les noms d'état suivent les noms de dossier, par exemple `Game_Chara_Motion_Normal`, `Game_Chara_Motion_GoGo`, `Game_Chara_Motion_Miss_Down`, `Game_Chara_Motion_Balloon_Broke`, `Game_Chara_Motion_Tower_Climbing`.
- `Game_Chara_Beat_<State>=N` : le nombre de temps que couvre une boucle de l'état, par exemple `Game_Chara_Beat_Normal=1`, `Game_Chara_Beat_GoGo=2`.
- Les états de menu, de titre et de résultats utilisent `Menu_Chara_Motion_Loop/Wait/Start/Select`, `Title_Chara_Motion_Normal/Entry`, `Result_Chara_Motion_Normal/Clear/Failed_In/Failed`, avec les clés `_Beat_` correspondantes ou des durées fixes en millisecondes : `Chara_Menu_Loop_AnimationDuration`, `Chara_Menu_Wait_AnimationDuration`, `Chara_Menu_Start_AnimationDuration`, `Chara_Menu_Select_AnimationDuration`, `Chara_Normal_AnimationDuration`, `Chara_Entry_AnimationDuration`, `Chara_Result_Normal_AnimationDuration`, `Chara_Result_Clear_AnimationDuration`, `Chara_Result_Failed_In_AnimationDuration`, `Chara_Result_Failed_AnimationDuration`.

La liste complète des clés, avec leurs valeurs par défaut, est la table `load_chara_config_defs` en tête du `CharaScript.lua` intégré. Le script ignore les clés qu'il ne connaît pas ; le `01 - Template/CharaConfig.txt` fourni contient donc aussi quelques clés côté skin sans effet dans ce fichier.

```ini
Chara_Version=0.6.1.0
Chara_Resolution=1920,1080

;Position X du personnage (1P,2P)
Game_Chara_X=0,0
;Position Y du personnage (1P,2P)
Game_Chara_Y=0,805

;Ordre des frames de l'état normal et temps par boucle
Game_Chara_Motion_Normal=0,1,2,3,4,5,6
Game_Chara_Beat_Normal=1

;Ordre des frames GoGo et temps par boucle
Game_Chara_Motion_GoGo=0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15
Game_Chara_Beat_GoGo=2
```

## Étape 5 (approche 3D) : Fournir model.glb et un Script.lua propre au personnage

Quand `Script.lua` existe dans le dossier du personnage, il remplace entièrement le script intégré. Le jeu appelle alors ces fonctions globales par leur nom :

- `loadAnimation(animationType)`, `disposeAnimation(animationType)`
- `availableAnimation(animationType)` renvoyant un booléen. Le jeu accepte toujours l'ancienne faute d'orthographe `avaialbeAnimation` : il essaie d'abord `availableAnimation` puis retombe sur `avaialbeAnimation`. Le modèle 3D fourni utilise encore l'ancien nom.
- `setAnimationDuration(animationType, durationMs)`, `resetAnimationCounter(animationType)`
- `update(delta, animationType, looping)` renvoyant `true` quand une animation non bouclée est terminée
- `draw(animationType, x, y, scaleX, scaleY, opacity, color, contextType, anchor, clipW, clipH, clipX, clipY, rotation, blendMode, wrapMode, gradientMap)`
- `getDrawSize(animationType)` renvoyant la largeur et la hauteur
- `getHeyaRenderOffset()` renvoyant x et y ; `getAIBattlePosition(player, charaScale)` renvoyant x et y, ou `nil` pour utiliser la position du skin
- `loadVoice(voiceType)`, `disposeVoice(voiceType)`, `playVoice(voiceType)`

Les types d'animation sont les chaînes derrière les constantes `CHARACTER.ANIM_*` (`"Game/Normal"`, `"Menu/Normal"`, ...), plus les deux types spéciaux `CHARACTER.ANIM_PREVIEW` (vignette) et `CHARACTER.ANIM_RENDER` (portrait complet). Les types de voix sont les constantes `CHARACTER.VOICE_*`. La chaîne de repli de l'étape 3 s'applique aussi aux personnages scriptés : le jeu interroge `availableAnimation` et parcourt les alternatives jusqu'à en trouver une disponible.

Le dossier fourni `01 - Template3D` ne contient que `CharaConfig.txt`, `Effects.json`, `Metadata.json`, `model.glb`, `Render.png` et `Script.lua`. Son script charge `model.glb` avec `MODEL:Load`, le rend dans une scène qu'il crée avec `SCENE3D:CreateScene`, lit les clés de position de `CharaConfig.txt` et associe chaque type d'animation à un indice de clip et à un nombre de temps dans une table `CLIP`. Pour créer un personnage 3D, copiez le dossier, remplacez `model.glb` et `Render.png`, et modifiez `CLIP` pour que chaque type pointe vers le bon indice de clip de votre modèle.

```lua
-- extrait de 01 - Template3D/Script.lua
local CLIP = {
  [CHARACTER.ANIM_GAME_NORMAL] = { clip = 0,  beat = 1 };
  [CHARACTER.ANIM_GAME_CLEAR]  = { clip = 1,  beat = 1 };
  [CHARACTER.ANIM_GAME_GOGO]   = { clip = 2,  beat = 4 };
  [CHARACTER.ANIM_MENU_NORMAL] = { clip = 14, beat = 2 };
  [CHARACTER.ANIM_RESULT_CLEAR]= { clip = 19, beat = 1 };
  -- une entrée par état d'animation pris en charge par le modèle
}

function loadAnimation(animationType)
  -- construisez les données de clip / d'aperçu / de rendu et marquez-les disponibles
end

function availableAnimation(animationType)
  return animations[animationType] ~= nil
end
```

## Étape 6 : Fichiers facultatifs : Effects.json, Unlock.json, Palettes.json, voix

- `Effects.json` : `gauge` (`Normal`, `Hard` ou `Extreme` ; par défaut `Normal`) sélectionne le type de jauge d'âme. `Hard` multiplie les gains de pièces par 1,5 et `Extreme` par 1,8 sauf si le jeu force la jauge normale. Quand le mod fun Minesweeper est actif, `bombFactor` (1-100, par défaut 20) est le pourcentage de notes que le mod transforme en bombes et `fuseRollFactor` (0-100, par défaut 0) le pourcentage de ballons qu'il transforme en roulements à mèche.
- `Unlock.json` : quand il est présent, le personnage reste verrouillé jusqu'à ce que le joueur remplisse la condition. Le format et les identifiants de condition correspondent à ceux des chansons ; voir le guide sur le déblocage. Le joueur achète les conditions de pièces dans l'écran de la chambre ; le jeu vérifie les autres conditions automatiquement sur l'écran de résultats. Exemples fournis : Kuro utilise `{ "condition": "dp", "type": "me", "values": [3, 3, 10] }` (dix réussites de partitions Extreme avec full combo ou mieux) et Aoi utilise `{ "condition": "ch", "type": "me", "values": [200] }` (200 pièces).
- `Palettes.json` : un tableau de palettes de couleurs que le joueur peut appliquer au personnage. Chaque entrée a `name`, `blend` (0-1), `stops` (un tableau de points d'arrêt de dégradé `[position, R, G, B]` ou `[position, R, G, B, A]` ; fournissez-en au moins deux) et `plays`, le nombre de parties avec ce personnage qui débloque la palette (0 ou absent signifie disponible immédiatement). Une entrée avec `"stops": null` est la version par défaut sans teinte.
- Voix : le script intégré charge des fichiers `.ogg` depuis des chemins fixes dans le dossier du personnage, par exemple `Sounds/Clear/Clear.ogg`, `Sounds/Clear/Failed.ogg`, `Sounds/Clear/FullCombo.ogg`, `Sounds/Clear/AllPerfect.ogg`, `Sounds/Menu/SongSelect.ogg`, `Sounds/Menu/SongDecide.ogg`, `Sounds/Menu/DiffSelect.ogg`, `Sounds/Title/Sanka.ogg`, `Sounds/Result/BestScore.ogg`, `Sounds/Result/ClearSuccess.ogg`, `Sounds/Result/ClearFailed.ogg`. La liste complète est la table `voice_files` en tête du `CharaScript.lua` intégré. Le script saute les fichiers manquants.

```json
{
  "gauge": "Normal",
  "bombFactor": 20,
  "fuseRollFactor": 0
}
```

```json
{
  "condition": "ch",
  "type": "me",
  "values": [ 200 ]
}
```

```json
[
  { "name": "Default", "stops": null },
  { "name": "Green", "blend": 1.0, "stops": [ [0, 0, 0, 0], [0.25, 0, 255, 0] ], "plays": 10 }
]
```

## Étape 7 : Redémarrer et sélectionner le personnage

Redémarrez le jeu (ou rechargez le skin depuis les paramètres). Le personnage apparaît dans la liste des personnages de l'écran de la chambre, où les personnages verrouillés affichent leur condition de déblocage. Les stages Lua peuvent aussi lire la liste via la globale `CHARACTERLIST`, qui expose pour chaque entrée le nom de dossier, le nom d'affichage, la rareté et la condition de déblocage.

## Dépannage et remarques

- Le personnage n'apparaît pas : vérifiez que le dossier est directement sous `Global/Characters/` et redémarrez le jeu. Le jeu construit la liste une fois au démarrage.
- Le personnage ne dessine rien : `Normal/0.png` est absent, ou les noms de dossier ne correspondent pas au tableau de l'étape 3. Les frames doivent être nommées `0.png`, `1.png`, ... sans trou ; un trou termine l'animation à cet indice sans erreur.
- Le personnage est hors écran ou de la mauvaise taille : `Chara_Resolution` doit correspondre à la résolution pour laquelle vous avez défini les valeurs de position. Quand la clé est absente, le jeu suppose `1280,720`.
- Seule une partie du jeu d'animations est jouée : les états sans repli (Cleared, Failed, Return, les états Balloon et Kusudama) ont besoin de leur propre dossier.
- Un personnage 3D affiche toutes ses animations comme indisponibles : `Script.lua` doit définir `availableAnimation` (ou `avaialbeAnimation`) et renvoyer `true` pour les types chargés.
- Un `Script.lua` présent remplace complètement le script intégré. Un personnage scripté peut toujours charger des dossiers de PNG numérotés, mais seulement si le script les charge lui-même.
- Les sauvegardes référencent le nom de dossier ; renommer un dossier que des joueurs ont déjà sélectionné réinitialise donc leur sélection sur l'emplacement vide.
- Les fichiers JSON fournis contiennent des virgules finales. L'analyseur JSON du jeu les accepte ; les validateurs stricts les rejettent.
