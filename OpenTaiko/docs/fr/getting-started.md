<!-- getting-started.md -->

# Fonctionnement des modules

OpenTaiko 0.6.1 permet à un skin d'ajouter et de remplacer des écrans en Lua. Le dossier Modules d'un skin contient un dossier par module, et chaque module possède un Script.lua qui définit un ensemble fixe de fonctions de rappel globales. Le jeu charge chaque Script.lua dans son propre état Lua isolé (bac à sable), enregistre les globales du moteur (TEXTURE, SOUND, INPUT, CONFIG et les autres documentées dans la [référence de l'API](api/README.md)) et appelle les fonctions de rappel au bon moment. [Modules et cycle de vie](api/activities.md) liste les signatures exactes.

## Avant de commencer

- OpenTaiko 0.6.1 et un dossier de skin contenant un répertoire Modules. Le skin fourni est System/Open-World Memories.
- Un éditeur de texte et des bases de Lua (fonctions, tables, require).
- Les stages fournis sous System/Open-World Memories/Modules/Stages. Les petits, demo1 et demo3, montrent la forme des fonctions de rappel ; les plus grands montrent comment de vrais écrans sont organisés.

## Où se trouvent les modules

Chaque type de module a son propre dossier sous Modules, et chaque module est un dossier dont le nom est l'identifiant du module :

```
Modules/
  Stages/        <name>/Script.lua   écrans complets
  Activities/    <name>/Script.lua   sous-écrans pilotés par un stage
  ROActivities/  <name>/Script.lua   sous-écrans et superpositions en lecture seule
  Transitions/   <name>/Script.lua   fondus entre les stages (chargés en premier)
  Lib/           fichiers .lua partagés accessibles via require ; non analysés comme modules
```

Le fichier d'entrée est toujours Script.lua. Les chemins de ressources que vous passez à TEXTURE, SOUND, VIDEO et aux autres chargeurs sont relatifs au dossier du module ; par convention, les modules fournis les rangent dans les sous-dossiers Textures, Sounds, Videos et Databases, et placent les traductions dans un dossier lang.

Deux types de scripts vivent ailleurs :

- Les arrière-plans (arrière-plans d'écran, couches de gameplay, foules, animations de réussite, le kusudama) sont des fichiers Script.lua placés sous le dossier Graphics du skin, dans le répertoire de l'écran qu'ils décorent. Voir la section Arrière-plans de [Modules et cycle de vie](api/activities.md).
- Les personnages sont des dossiers sous Global/Characters. Un dossier de personnage peut embarquer son propre Script.lua ; sans lui, le jeu utilise son script de personnage intégré. Voir [Ajouter des personnages](guides/characters.md).

## Script.lua définit des fonctions globales

Le Script.lua d'un module définit des fonctions globales de premier niveau aux noms fixes, et le jeu lit chacune d'elles comme une globale. Le jeu ne trouve jamais une fonction enveloppée dans une table locale puis renvoyée, et il n'appelle jamais un nom mal orthographié (OnStart à la place de onStart), car il traite une fonction de rappel non définie comme une opération vide et ne signale rien. Tout le reste du fichier peut être local, et vous pouvez répartir le module sur plusieurs fichiers chargés avec require.

demo3 est la forme minimale à copier :

```lua
-- Modules/Stages/mystage/Script.lua
local text = nil
local textTex = nil

function onStart()          -- une fois au chargement du skin : chargez les ressources ici
    text = TEXT:Create(16)
end

function activate()         -- à chaque entrée dans le stage
    textTex = text:GetText("Hello")
end

function update(timestamp)  -- à chaque frame : entrées et changements d'état
    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        return Exit("title", nil)
    end
end

function draw()             -- à chaque frame : dessin uniquement
    if textTex ~= nil then textTex:Draw(200, 200) end
end

function deactivate()       -- quand le stage est quitté : arrêtez les sons, fermez les bases de données
end

function onDestroy()        -- avant le déchargement du skin : libérez ce que vous avez créé
    if textTex ~= nil then textTex:Dispose() end
end
```

## Le cycle de vie d'un stage

- onStart() : le jeu l'appelle une fois après le chargement du skin, et de nouveau après chaque rechargement du skin, que le stage soit à l'écran ou non. Chargez ici les textures, les sons et les vidéos. Elle s'exécute dans une coroutine, si bien qu'un chargement long peut s'étaler sur plusieurs frames derrière la barre de chargement grâce à l'assistant LOADING.
- activate() : le jeu l'appelle à chaque entrée dans le stage. Réinitialisez ici l'état propre à chaque visite, lancez la musique et ouvrez les bases de données. Elle s'exécute aussi dans une coroutine et peut utiliser LOADING. Le jeu rafraîchit les listes de personnages et de puchicharas (CHARACTERLIST, PUCHICHARALIST) juste avant l'exécution d'activate ; lisez-les donc ici, car onStart s'exécute avant ce rafraîchissement.
- update(timestamp) : le jeu l'appelle à chaque frame avant draw et lui passe l'horloge du jeu en millisecondes. Gérez ici les entrées et modifiez l'état. Une fois que le stage a appelé Exit, le jeu cesse d'appeler update et continue d'appeler draw pendant le fondu de sortie.
- draw() : le jeu l'appelle à chaque frame. Dessinez uniquement, et limitez les allocations par frame.
- deactivate() : le jeu l'appelle quand il quitte le stage. demo3 y libère ses bases de données ; demo1 y arrête sa musique et sa vidéo.
- afterSongEnum() : le jeu l'appelle chaque fois que l'énumération des chansons se termine, au démarrage et après un rechargement léger ou complet, même quand le stage n'est pas actif. Utilisez-la quand le module dépend de la liste des chansons.
- onDestroy() : le jeu l'appelle avant de décharger le skin. demo1 y libère sa texture, sa vidéo, sa texture de texte et ses sons.
- reloadLanguage(lang) : le jeu l'appelle quand la langue change (voir la section sur la localisation ci-dessous).

Au chargement d'un skin, le jeu crée les modules type par type, d'abord les Transitions, puis les Stages, les Activities et les ROActivities. Au sein d'un type, il exécute chaque Script.lua avant d'appeler le moindre onStart. Les Activities et les ROActivities n'existent donc pas encore pendant le onStart d'un stage ; recherchez-les dans activate.

Les autres types utilisent des variantes de cet ensemble. Les Activities et les ROActivities ont les mêmes fonctions de rappel, mais le stage hôte invoque activate, deactivate, draw et update et reçoit leurs valeurs de retour. Les arrière-plans reçoivent un objet d'état dans activate(state), update(timestamp, state) et draw(state) et peuvent définir des crochets d'événement tels que clearIn, playEndAnime et kusuBroke. Les transitions définissent fadeOut(t), loading(progress, elapsed) et fadeIn(t). Les personnages définissent leur propre jeu d'animations et de voix. [Modules et cycle de vie](api/activities.md) les liste tous.

## Choisir un type de module

- Stage (Modules/Stages) : un écran complet vers lequel le jeu bascule. Il possède la frame, gère les entrées et se quitte en appelant Exit. Utilisez-le pour tout ce qui constitue un écran à part entière.
- Activity (Modules/Activities) : un sous-écran qu'un stage utilise depuis l'intérieur, comme une boîte de dialogue. C'est un singleton que vous obtenez avec ACTIVITY:GetActivity(name) ; le stage hôte appelle ses méthodes Activate, Update, Draw et Deactivate. Utilisez-la pour des éléments partagés susceptibles d'écrire l'état du jeu.
- ROActivity (Modules/ROActivities) : la forme en lecture seule d'une Activity, que vous obtenez avec ROACTIVITY:GetROActivity(name). Elle reçoit CONFIG, DATABASE et GetSaveFile en lecture seule et n'a pas de globale ACTIVITY. Utilisez-la pour les éléments qui ne font que lire l'état, ce qui couvre la plupart des interfaces réutilisables. Le moteur héberge plusieurs de ses propres superpositions sous forme de ROActivities aux noms fixes (nameplate, modal, modicons, danplate, popup_menu, config_ui, song_enum) ; un skin en remplace une en fournissant un dossier de ce nom, en conservant les fonctions de rappel que le moteur appelle.
- Arrière-plan : un Script.lua sous Graphics qui dessine derrière ou par-dessus l'un des écrans du moteur. Les arrière-plans reçoivent les mêmes globales en lecture seule que les ROActivities.
- Transition (Modules/Transitions) : le fondu de sortie, le chargement et le fondu d'entrée que le jeu joue entre deux stages. Un stage en choisit une par son nom dans le troisième argument d'Exit ; le jeu se rabat sur celle nommée default quand le stage n'en nomme aucune ou que le nom n'existe pas, et joue celle nommée song_loading pour entrer dans le gameplay.
- Personnage : voir [Ajouter des personnages](guides/characters.md).

## Quitter un stage avec Exit

Seuls les stages disposent de la globale Exit. Elle accepte jusqu'à trois arguments et tolère nil à n'importe quelle position : la cible ("title", "play", "stage" ou "legacy" ; nil signifie "title"), le nom du stage de destination quand la cible est "stage" (ou une clé legacy quand elle est "legacy"), et le nom d'un module de transition. Les stages fournis écrivent `return Exit(...)` à l'intérieur d'update pour que rien d'autre ne s'exécute dans cette frame.

```lua
-- extrait de demo1/Script.lua, dans update()
if INPUT:KeyboardPressed("S") == true then
    sounds.Skip:Play()
    return Exit("stage", "demo2")   -- aller vers Modules/Stages/demo2
end
-- ...
return Exit("title", nil)           -- retour à l'écran titre
```

## Le bac à sable

Chaque Script.lua s'exécute dans un état Lua restreint :

- os ne conserve que time, date et difftime. Le bac à sable retire io, debug, loadfile et dofile, et import ne fait rien.
- package se réduit à un chargeur personnalisé : package.path et package.cpath sont vides et le bac à sable remplace les chercheurs standard, si bien que seuls les chemins ci-dessous sont consultables.
- require cherche d'abord dans le dossier du module lui-même, puis dans le dossier Modules/Lib du skin, et charge le premier fichier trouvé. Un fichier du module et un fichier de Lib portant le même nom se résolvent vers le fichier du module. Les points du nom deviennent des séparateurs de chemin : require("DBControllers.dbScores") et require("DBControllers/dbScores") chargent tous deux DBControllers/dbScores.lua. Les chemins non ASCII fonctionnent.

```lua
-- extrait de intro_nokon/Script.lua
local DBScores  = require("DBControllers/dbScores")  -- le sous-dossier du module
local I18N      = require("i18n")                     -- Modules/Lib/i18n.lua
local Opening   = require("opening")                  -- le dossier du module
local Dialogue  = require("nokon_dialogue")           -- le dossier du module
```

## Modules en lecture seule

Le jeu crée les ROActivities et les arrière-plans avec des globales restreintes avant l'exécution de leur Script.lua : CONFIG est une vue en lecture seule, GetSaveFile(player) renvoie un fichier de sauvegarde en lecture seule, DATABASE ouvre des magasins en lecture seule, et ACTIVITY vaut nil (utilisez ROACTIVITY). Une écriture à travers l'un d'eux journalise une notification d'erreur, ne fait rien et ne lève aucune erreur Lua. Un module qui doit modifier les paramètres, les données de sauvegarde ou une base de données doit être une Activity ou un Stage.

## Localisation avec lang/

Le skin fourni traduit les chaînes propres à chaque module avec la bibliothèque partagée Modules/Lib/i18n.lua. La chaîne anglaise du code est la clé : le module embarque un lang/ja.lua qui renvoie une table associant chaque chaîne anglaise à sa traduction japonaise, et la bibliothèque cherche les chaînes dans cette table.

La bibliothèque a trois fonctions :

- detect() lit la langue actuelle du jeu via la globale LANG et charge le dictionnaire correspondant. Quand la langue est le japonais, elle fait un require de lang/ja, résolu dans le dossier du module, si bien que chaque module a son propre dictionnaire ; pour toute autre langue, elle ne charge rien. Tant que vous ne l'appelez pas, aucun dictionnaire n'est chargé et chaque chaîne reste en anglais.
- tr(s) renvoie la traduction de s tirée du dictionnaire chargé, ou s elle-même quand le dictionnaire n'a pas d'entrée pour elle ou qu'aucun dictionnaire n'est chargé.
- trf(fmt, ...) traduit la chaîne de format fmt de la même façon, puis la formate avec string.format.

Appelez detect() dans activate, puis construisez votre texte avec tr et trf. activate s'exécute à chaque entrée dans le module, si bien qu'une langue changée par le joueur dans les réglages prend effet à la visite suivante, et le module n'a besoin d'aucun autre point d'accroche. Les clés doivent correspondre exactement à la source anglaise, ponctuation, espacement et sauts de ligne compris, et la traduction doit conserver tels quels les substituts tels que %s ou {Player 1 name}.

```lua
-- Modules/Stages/mystage/lang/ja.lua
local T = {}
T["Nokon"] = "ノコン"
T["Alright, quiz time!"] = "さあ、クイズの時間である！"
return T
```

```lua
-- Modules/Stages/mystage/Script.lua
local I18N = require("i18n")
local title

function activate()
    I18N.detect()
    title = I18N.tr("Alright, quiz time!")
end
```

Le jeu appelle aussi une fonction globale reloadLanguage(lang) sur chaque module chargé quand la langue change. Seul un module qui reste à l'écran pendant le changement de langue en a besoin, par exemple un écran avec un sélecteur de langue ; appelez-y detect() de nouveau et reconstruisez le texte pré-rendu.

## Points de vigilance

- Le jeu libère les textures, sons, vidéos et objets de texte créés par un module quand il libère le module, si bien qu'un rechargement du skin ne les fait pas fuir. Libérez dans deactivate les ressources que vous ouvrez à chaque visite, comme les bases de données, comme le fait demo3, et libérez dans onDestroy ce que vous avez créé, comme le fait demo1.
- GetText met en cache une texture par chaîne distincte sur son objet texte. Une chaîne qui change à chaque frame ajoute une texture à chaque frame et le jeu ralentit progressivement. Dessinez les valeurs changeantes avec le moteur de glyphes (TEXT:CreateGlyphCached), ou conservez une seule texture jusqu'à ce que la valeur change.
- LOADING ne fonctionne que dans les fonctions de rappel exécutées en coroutine : le onStart de n'importe quel module et l'activate d'un stage. Appeler LOADING:Tick depuis l'activate d'une Activity, ou depuis update ou draw, lève une erreur Lua.
- onStart et afterSongEnum s'exécutent alors que le module est hors écran. Écrivez-les de façon à ce qu'ils fonctionnent sans que le stage soit visible.
