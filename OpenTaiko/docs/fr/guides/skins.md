<!-- guides/skins.md -->

# Créer un nouveau skin

Un skin est un dossier sous le répertoire `System/` du jeu. Il fournit les graphismes, les sons, les polices, les valeurs de disposition, les fichiers de locale et les modules Lua qui dessinent chaque écran. Ce guide explique ce qui fait d'un dossier un skin, les clés de `SkinConfig.ini`, l'organisation des dossiers, l'arborescence des modules Lua et son cycle de vie, et comment vous installez et sélectionnez un skin. La référence de l'API couvre l'API Lua elle-même (dessin, son, entrées et ainsi de suite).

Un skin porte aussi les modules Lua qui font tourner chaque écran. Le jeu charge des modules précis par leur nom et bascule vers des stages précis ; le sélecteur de skin liste donc un skin construit de zéro, mais le jeu ne peut pas le faire tourner. Partez d'une copie du skin fourni.

## Avant de commencer

- OpenTaiko 0.6.1 installé, avec le skin fourni `System/Open-World Memories/` présent.
- Un éditeur de texte brut pour `SkinConfig.ini`, les fichiers `*Config.ini` inclus et les modules Lua.
- Des bases de Lua si vous comptez modifier le comportement des écrans. Un simple changement de textures (remplacer les fichiers PNG et OGG et modifier les valeurs `.ini`) ne nécessite pas de Lua.

## Étape 1 : Comprendre ce qui fait d'un dossier un skin

Au démarrage, le jeu liste les sous-dossiers de `System/`. Un dossier ne compte comme skin que si `Graphics/1_Title/Background.png` existe à l'intérieur ; le jeu ignore tout autre dossier. Si le dossier du skin sélectionné est absent, le jeu retombe sur `System/Default/`, puis sur le premier skin valide par ordre alphabétique, puis sur `System/` lui-même.

Cette vérification ne fait que lister le dossier. L'étape 8 nomme les modules qui doivent aussi exister pour que le skin fonctionne.

```
System/
  Open-World Memories/         <- le skin fourni
  My New Skin/                 <- votre skin
    Graphics/
      1_Title/
        Background.png          <- requis pour que le dossier soit listé
    SkinConfig.ini
```

## Étape 2 : Copier le skin fourni

Copiez `System/Open-World Memories/` vers un nouveau dossier voisin, par exemple `System/My New Skin/`. La copie contient tout ce dont le jeu a besoin : `Graphics/`, `Sounds/`, `Fonts/`, `Locales/`, `Modules/`, `ThemeSettings.json`, `SkinConfig.ini` et les fichiers `*Config.ini` qu'il inclut. Le nom du dossier est l'identité du skin (le jeu l'enregistre comme skin sélectionné et le sélecteur de skin l'affiche) ; gardez-le donc compatible avec le système de fichiers. Modifiez ensuite `SkinConfig.ini` pour que les métadonnées décrivent votre skin.

## Étape 3 : Modifier SkinConfig.ini

`SkinConfig.ini` est un fichier `Key=Value`, un réglage par ligne. L'analyseur retire les espaces et tabulations en début de ligne, traite les lignes commençant par `;` comme des commentaires, et ne lit une ligne que si elle contient exactement un `=`. La comparaison des clés est exacte, et l'analyseur ignore une clé inconnue sans signaler d'erreur. Les clés au niveau du skin sont :

- `Name=` : nom d'affichage. Métadonnée uniquement ; le nom du dossier sélectionne le skin.
- `Version=`, `Creator=` : chaînes libres (par défaut `Unknown`). Le jeu ne les valide pas.
- `DefaultLocale=` : identifiant de locale que le jeu utilise quand la langue active du jeu n'a pas de fichier sous `Locales/` (par défaut `en`).
- `Resolution=W,H` : la résolution pour laquelle vous définissez les valeurs de disposition (par défaut `1280,720`). Le skin fourni utilise `1920,1080`.
- `Resolutions=` : multiplicateurs d'échelle de rendu sélectionnables (étape 4).
- `AIBattleCharacter=` : dossier de personnage utilisé pour l'adversaire IA (étape 5).
- `FontName<LANG>=` et `BoxFontName<LANG>=` : fichier de police par langue du jeu, où `<LANG>` est le code de langue en majuscules (`EN`, `JA`, `FR`, `ES`, `NL`, `DE`, `RU`, `KO`, `ZH`). Le chemin est relatif à la racine du skin (un chemin absolu fonctionne aussi) et le fichier doit exister, sinon l'analyseur abandonne la clé.

Toute autre clé (`Game_*`, `Result_*`, `Title_*` et ainsi de suite) est une valeur de disposition d'écran. Le même analyseur les lit, ce qui explique qu'elles puissent vivre dans les fichiers inclus de l'étape 6.

```ini
;Informations du skin
Name=My New Skin
DefaultLocale=en
Version=1.0.0
Creator=Your Name
Resolution=1920,1080
;Multiplicateurs d'échelle de rendu sélectionnables (<=1 ; décimal ou fraction a/b, séparés par des virgules). 1 est toujours disponible et est la valeur par défaut.
Resolutions=1,2/3,1/3
;Dossier de personnage utilisé pour l'emplacement de bataille contre l'IA.
AIBattleCharacter=10v2 - AItritus
FontNameEN=Fonts/MPLUSRounded1c-Medium.ttf
FontNameJA=Fonts/MPLUSRounded1c-Medium.ttf
BoxFontNameEN=Fonts/MPLUSRounded1c-Regular.ttf
BoxFontNameJA=Fonts/MPLUSRounded1c-Regular.ttf
```

## Étape 4 : L'option Resolutions

`Resolutions=` est une liste, séparée par des virgules, des multiplicateurs d'échelle de rendu que le menu des paramètres propose. Le jeu effectue le rendu à `Resolution` fois le multiplicateur choisi et agrandit le résultat à la taille de la fenêtre ; la taille de la fenêtre ne change pas. Chaque jeton est un décimal (`0.5`) ou une fraction (`2/3`). L'analyseur abandonne les jetons hors de l'intervalle 0 < valeur <= 1, les jetons non analysables et les doublons, ajoute `1` s'il manque et trie la liste avec `1` en premier. Le séparateur doit être une virgule, car un point-virgule commence une ligne de commentaire. Le menu des paramètres affiche chaque entrée avec sa taille en pixels, par exemple `2/3 (1280x720)` pour un skin en 1920x1080.

```ini
Resolution=1920,1080
Resolutions=1,2/3,1/3
; produit les options :
;   1     -> 1920x1080  (par défaut)
;   2/3   -> 1280x720
;   1/3   -> 640x360
```

## Étape 5 : L'option AIBattleCharacter

`AIBattleCharacter=` nomme le dossier sous `Global/Characters/` que le jeu utilise pour l'adversaire IA en mode bataille contre l'IA. La valeur par défaut est `10v2 - AItritus`. Le dossier nommé doit exister.

```ini
;Dossier de personnage utilisé pour l'emplacement de bataille contre l'IA.
AIBattleCharacter=10v2 - AItritus
```

## Étape 6 : Scinder la configuration avec #include

Quand l'analyseur rencontre une ligne de la forme `#include SomeFile.ini`, il lit ce fichier sur place, récursivement. Le chemin est relatif à la racine du skin. Le `SkinConfig.ini` fourni ne contient que les métadonnées et les clés de police, puis inclut un fichier par écran. Conservez ces lignes quand vous copiez le skin, et modifiez les fichiers `*Config.ini` individuels pour réajuster un écran.

```
; fin de SkinConfig.ini (skin fourni, dans l'ordre)
#include OtherConfig.ini
#include TitleConfig.ini
#include ConfigConfig.ini
#include SongSelectConfig.ini
#include HeyaConfig.ini
#include SongLoadingConfig.ini
#include GameConfig.ini
#include ModIconsConfig.ini
#include NameplateConfig.ini
#include AIResultConfig.ini
#include ResultConfig.ini
#include DaniSelectConfig.ini
#include DanResultConfig.ini
#include TowerResultConfig.ini
#include TowerSelectConfig.ini
#include OnlineLoungeConfig.ini
#include OpenEncyclopediaConfig.ini
#include ModalConfig.ini
#include Game4PConfig.ini
#include Result4PConfig.ini
#include Modal4PConfig.ini
```

## Étape 7 : Connaître l'organisation du dossier du skin

Avec le skin fourni comme référence, la racine du skin contient :

- `Graphics/` : images regroupées dans des dossiers numérotés par écran (`0_Startup`, `1_Title`, `2_Config`, `3_DaniSelect`, `5_Game`, `6_Result`, `7_DanResult`, `7_Exit`, `8_TowerResult`, `10_Heya`, `12_OnlineLounge`, `13_TowerSelect`, `15_OpenEncyclopedia`) plus quelques images partagées à la racine. Les arrière-plans animés sont des fichiers `Script.lua` qui se trouvent à côté des images du dossier auquel ils appartiennent (par exemple `Graphics/0_Startup/Script.lua` et les dossiers sous `Graphics/5_Game/5_Background/`).
- `Sounds/` : sons système et musiques que le jeu charge par nom de fichier fixe, par exemple `Sounds/Move.ogg`, `Sounds/Decide.ogg`, `Sounds/Cancel.ogg`, `Sounds/BGM/Title.ogg`, `Sounds/BGM/SongSelect.ogg`, `Sounds/BGM/Result.ogg`. Si un fichier est absent, ce son n'est pas joué.
- `Fonts/` : les fichiers `.ttf` référencés par les clés `FontName`.
- `Locales/` : un fichier JSON par langue (`en.json`, `ja.json`, ...) de la forme `{ "Entries": { "KEY": "text" } }`. Ces chaînes étiquettent les réglages propres au skin, et Lua les lit via `THEME:GetSkinString(key)`. Quand une clé est absente de la langue active, le jeu la recherche dans le fichier `DefaultLocale`.
- `Modules/` : l'arborescence des modules Lua (étape 8).
- `ThemeSettings.json` : un tableau de réglages que l'écran des options affiche sous Paramètres du thème. Chaque entrée a `id`, `type` (`bool`, `int`, `double`, `string` ou `enum`), `scope` (`global`, par défaut, ou `save` pour une valeur par fichier de sauvegarde), `label` et `description` localisés, `default`, et `min`/`max` ou `options` selon le type.
- `SkinConfig.ini` et les fichiers `*Config.ini` inclus.
- `README.txt`, `LICENSE.md`, `Licenses/` : fichiers d'attribution. Le jeu ne les lit pas.

```
My New Skin/
  SkinConfig.ini
  ThemeSettings.json
  Graphics/           images par écran ; certains dossiers portent un Script.lua d'arrière-plan
  Sounds/             sons système .ogg à noms fixes et BGM/
  Fonts/              fichiers .ttf nommés par les clés FontName
  Locales/            en.json, ja.json, ... ({ "Entries": { ... } })
  Modules/            l'arborescence des modules Lua (étape 8)
  <screen>Config.ini  fichiers de disposition inclus via #include
```

## Étape 8 : L'arborescence Modules et les modules requis par le jeu

Au chargement du skin, le jeu analyse quatre sous-dossiers de `Modules/` et traite chaque sous-dossier direct qu'ils contiennent comme un module dont le fichier d'entrée est `Script.lua` :

- `Modules/Transitions/` : transitions qui se jouent entre les stages. Le jeu les charge en premier, pour qu'elles soient prêtes au premier changement de stage.
- `Modules/Stages/` : écrans complets. Vous entrez dans un stage avec `Exit("stage", "<folder name>")`.
- `Modules/Activities/` : sous-écrans réutilisables superposés à un stage (par exemple `confirm_dialog`, `mod_select_dialog`, `song_select_core`).
- `Modules/ROActivities/` : superpositions en lecture seule que le jeu pilote directement.

Le jeu n'analyse pas `Modules/Lib/`. Vous chargez les fichiers qui s'y trouvent avec `require` : le chemin de recherche d'un module est son propre dossier suivi de `Modules/Lib/`, si bien que `require("dialogue")` se résout vers `Modules/Lib/dialogue.lua`. Vous pouvez aussi placer des stages et des activités sous `Global/Stages/` et `Global/Activities/` dans le dossier d'installation du jeu ; le jeu charge ceux-là pour tous les skins.

Au sein de chaque catégorie, le jeu crée d'abord chaque module puis exécute `onStart` sur chacun, dans l'ordre Transitions, Stages, Activities, ROActivities.

Le jeu recherche les modules suivants par leur nom, et le skin fourni les propose tous :

- Les stages `_boot` et `_title`. Le jeu s'arrête avec une erreur si l'un des deux manque.
- Les ROActivities `modal`, `config_ui`, `nameplate`, `popup_menu`, `modicons`, `song_enum` et `danplate`.
- Les transitions `default` et `song_loading`. `song_loading` se joue pendant que le jeu charge une chanson ; le jeu utilise `default` quand `Exit` ne nomme aucune transition ou en nomme une qui n'existe pas. Un skin sans aucun module de transition retombe sur un simple fondu au noir.

Conservez tous ces modules en construisant un skin ; ajoutez vos propres modules à côté.

```
Modules/
  Transitions/   <name>/Script.lua   (chargées en premier ; "default" et "song_loading" utilisées par le jeu)
  Stages/        <name>/Script.lua   ("_boot" et "_title" requis)
  Activities/    <name>/Script.lua
  ROActivities/  <name>/Script.lua   (modal, config_ui, nameplate, popup_menu, modicons, song_enum, danplate requis)
  Lib/           fichiers .lua partagés accessibles via require, non analysés
```

## Étape 9 : Le Script.lua d'un stage et son cycle de vie

`Script.lua` s'exécute une fois quand le jeu crée le module, avec les globales du moteur (`TEXTURE`, `SOUND`, `INPUT`, `CONFIG`, `THEME` et les autres) déjà définies. Le jeu recherche ensuite des fonctions globales par leur nom et les appelle. Pour un stage :

- `onStart()` : une fois, au chargement du skin. S'exécute dans une coroutine ; un chargement lourd peut donc appeler `coroutine.yield()` ou les assistants `LOADING` pour répartir le travail sur plusieurs frames derrière la barre de chargement.
- `activate()` : à chaque fois que le jeu entre dans le stage. Également une coroutine. Le jeu rafraîchit les globales `CHARACTERLIST` et `PUCHICHARALIST` juste avant son exécution ; construisez donc ici tout ce qui en dépend, car dans `onStart` elles sont encore vides.
- `update(timestamp)` : à chaque frame. Renvoyez `Exit(target, name, transition)` pour quitter le stage. `target` vaut `"title"`, `"play"`, `"stage"` (avec `name` = un dossier de stage) ou `"legacy"` (avec `name` = `heya`, `config`, `exit` ou `onlinelounge`) ; `transition` est un dossier sous `Modules/Transitions/` et vaut `default` par défaut.
- `draw()` : à chaque frame.
- `deactivate()` : quand le jeu quitte le stage.
- `afterSongEnum()` : quand l'énumération de la liste des chansons est terminée.
- `onDestroy()` : quand le jeu démonte le skin.

Toutes sont facultatives ; le jeu ignore une fonction que vous ne définissez pas. Les Activities, les ROActivities et les Transitions suivent le même schéma avec leurs propres jeux de crochets.

```lua
-- Modules/Stages/my_stage/Script.lua
function onStart()
  -- configuration unique ; peut appeler coroutine.yield() pendant les chargements lourds
end

function activate()
  -- s'exécute à chaque entrée dans le stage
end

function update(ts)
  if INPUT:Pressed("Cancel") then
    return Exit("stage", "_title")   -- quitter ce stage
  end
  return nil
end

function draw()
  -- rendu par frame
end

function deactivate() end
function afterSongEnum() end
function onDestroy() end
```

## Étape 10 : Localiser un module avec lang/

Un module peut conserver ses propres traductions dans un sous-dossier `lang/` à côté de `Script.lua`. Comme le dossier du module est sur son chemin `require`, `require("lang.ja")` se résout vers `lang/ja.lua`. Le skin fourni procède ainsi pour ses stages les plus grands (par exemple `Modules/Stages/myroom/lang/ja.lua` et `Modules/Stages/intro_nokon/lang/ja.lua`) via l'assistant `Modules/Lib/i18n.lua`. Ceci est distinct du dossier `Locales/` à l'échelle du skin de l'étape 7.

```
Modules/Stages/my_stage/
  Script.lua
  lang/
    ja.lua        -- require("lang.ja")
```

## Étape 11 : Installer et sélectionner le skin

Placez le dossier sous `System/`. Ouvrez les paramètres, allez dans la section Apparence et choisissez le skin dans l'option Skin ; le sélecteur liste chaque skin valide par nom de dossier et affiche son `Graphics/1_Title/Background.png` en vignette. Quand vous changez de skin, le jeu démonte le skin courant, charge le nouveau et recharge tous ses modules Lua derrière une barre de chargement.

Le jeu écrit le choix dans `Config.ini` sous `SkinPath=`, relatif à `System/`. Il écrit le nom de dossier nu (par exemple `SkinPath=My New Skin\` sous Windows) et accepte aussi la forme `./My New Skin/` montrée dans le commentaire du fichier.

```ini
; Dans Config.ini (écrit quand un skin est choisi en jeu) :
; Chemin du dossier du skin, relatif à System/
SkinPath=My New Skin\
```

## Dépannage et remarques

- Le sélecteur ne liste pas le skin : `Graphics/1_Title/Background.png` est absent, ou le dossier n'est pas directement sous `System/`.
- Le jeu affiche une erreur juste après que vous sélectionnez le skin : un module requis manque (étape 8) ou l'un d'eux a levé une erreur Lua. Testez un skin en basculant dessus.
- Une clé de `SkinConfig.ini` n'a aucun effet : la clé est mal orthographiée, la ligne contient plus d'un `=`, ou la valeur n'a pas pu être analysée. L'analyseur ignore les clés inconnues sans les signaler.
- `Resolutions=` n'affiche que `1` : la liste utilisait des points-virgules (marqueur de commentaire) ou toutes les valeurs étaient hors de 0 < valeur <= 1.
- Une clé de police n'a aucun effet : le chemin du fichier n'existe pas relativement à la racine du skin.
- `CHARACTERLIST` ou `PUCHICHARALIST` est vide dans `onStart` : le jeu les remplit après avoir créé les modules. Utilisez-les depuis `activate`.
- Renommer le dossier du skin change son identité ; `SkinPath` dans `Config.ini` doit pointer vers le nouveau nom.
- `Name=`, `Version=` et `Creator=` sont purement informatifs. Le jeu ne fait aucune vérification de compatibilité dessus.
