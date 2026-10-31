<!-- api/activities.md -->

# Modules et cycle de vie

Le moteur appelle un ensemble fixe de fonctions de rappel dans le Script.lua d'un module. Cette page les liste, avec les globales qui pilotent les activités, les arrière-plans et les transitions, ainsi que les assistants de compteur, de caméra et de diagnostic que chaque module reçoit. Si vous n'avez jamais écrit de module, lisez d'abord [Fonctionnement des modules](../getting-started.md).

## Fonctions de rappel du cycle de vie

Le moteur recherche par leur nom des fonctions globales de premier niveau dans le Script.lua de chaque module et les appelle à des moments fixes. Le moteur ignore une fonction de rappel que vous ne définissez pas. Le type de module décide des fonctions de rappel que le moteur appelle.

<div class="callout warn">
Au chargement d'un skin, le moteur crée et démarre les modules dans cet ordre : Transitions, Stages, Activities, ROActivities. Au sein de chaque type, le moteur exécute d'abord le Script.lua de chaque module (son code de premier niveau), puis appelle onStart sur chacun d'eux. Les Activities et les ROActivities ne sont donc pas encore chargées pendant le onStart d'un stage, et une recherche à ce moment renvoie nil : recherchez-les dans activate. Au changement de skin ou à la sortie, onDestroy s'exécute sur les Stages, puis sur les ROActivities et les Activities, puis sur les Transitions.
</div>

### Stages, Activities et ROActivities

| Méthode | Description |
| --- | --- |
| `onStart()` | Appelée une fois après que le moteur a créé le module, c'est-à-dire au démarrage et de nouveau à chaque fois que le moteur charge ou recharge le skin. S'exécute dans une coroutine (voir LOADING ci-dessous) ; chargez les ressources ici. |
| `activate(...)` | Stage : appelée à chaque fois que le moteur entre dans le stage, dans une coroutine. Activity/ROActivity : l'hôte l'appelle via `handle:Activate(...)` avec ses propres arguments ; les valeurs de retour reviennent à l'hôte. Le moteur rafraîchit les globales CHARACTERLIST et PUCHICHARALIST juste avant son exécution. |
| `update(timestamp)` | Appelée à chaque frame avant draw ; timestamp est l'horloge du jeu en millisecondes. Un stage cesse de recevoir update dès qu'il a appelé Exit ; draw continue pendant le fondu de sortie. Activity/ROActivity : l'hôte l'appelle via `handle:Update()`. |
| `draw(...)` | Appelée à chaque frame. Activity/ROActivity : l'hôte l'appelle via `handle:Draw(...)` avec ses propres arguments ; les valeurs de retour reviennent à l'hôte. |
| `deactivate(...)` | Stage : appelée quand le moteur quitte le stage. Activity/ROActivity : l'hôte l'appelle via `handle:Deactivate(...)`, ou le module l'appelle sur lui-même via `DEACTIVATE()` ; les valeurs de retour reviennent à l'hôte. |
| `afterSongEnum()` | Appelée chaque fois que l'énumération des chansons se termine, y compris au démarrage et après un rechargement léger ou complet des chansons, même quand le module n'est pas actif. |
| `onDestroy()` | Appelée avant que le moteur ne décharge le skin, pour que le module libère ce qu'il détient. |
| `reloadLanguage(lang)` | Appelée sur chaque module chargé quand la langue du jeu change ; lang est le nouveau code de langue. |

### Arrière-plans

Chaque écran héberge ses propres arrière-plans. La section Arrière-plans ci-dessous décrit l'argument d'état et les crochets d'événement.

| Méthode | Description |
| --- | --- |
| `onStart()` | Appelée une fois, de façon synchrone, la première fois que l'hôte active l'arrière-plan. |
| `activate(state)` | Appelée à chaque activation de l'arrière-plan par l'hôte ; une réactivation ne relance pas onStart. |
| `update(timestamp, state)` | Appelée à chaque frame (sauf pendant la pause du gameplay) ; timestamp est `state.timeStamp` en millisecondes. |
| `draw(state)` | Appelée à chaque frame. |
| `reloadLanguage(lang)` | Appelée quand la langue du jeu change. |

Le moteur n'appelle ni afterSongEnum ni onDestroy sur les arrière-plans ; quand l'hôte libère un arrière-plan, le moteur libère les ressources qu'il a créées.

### Transitions

| Méthode | Description |
| --- | --- |
| `onStart()` | Appelée une fois au chargement du skin, avant les stages et les activités, dans une coroutine. |
| `fadeOut(t)` | Dessine le fondu de sortie par-dessus le stage sortant ; t va de 0 à 1. |
| `loading(progress, elapsed)` | Dessine l'écran de chargement ; progress va de 0 à 1, elapsed est le temps en secondes écoulé depuis le début du chargement. |
| `fadeIn(t)` | Dessine le fondu d'entrée par-dessus le nouveau stage ; t va de 0 à 1. |
| `onDestroy()` | Appelée avant que le moteur ne décharge le skin, après les stages et les activités. |
| `reloadLanguage(lang)` | Appelée quand la langue du jeu change. |

La section Transitions ci-dessous décrit le minutage des phases.

### Personnages

Le Script.lua d'un personnage définit un ensemble différent : loadAnimation, disposeAnimation, availableAnimation, setAnimationDuration, resetAnimationCounter, update, draw, getDrawSize, getAIBattlePosition, loadVoice, disposeVoice et playVoice. [Ajouter des personnages](../guides/characters.md) le traite.

### Exit

Une fonction que le moteur enregistre uniquement dans les scripts de Stage ; l'appeler demande au moteur de quitter le stage.

<div class="callout warn">
Disponible uniquement dans les scripts de Modules/Stages ; leur hôte pilote les Activities et les ROActivities, qui n'en disposent pas. Accepte de 0 à 3 arguments et tolère nil à n'importe quelle position. L'appel lui-même demande la sortie ; les stages fournis écrivent `return Exit(...)` dans update pour que rien d'autre ne s'exécute dans cette frame. target vaut "title", "play", "stage" ou "legacy" ; nil ou toute autre valeur signifie "title". Quand target est "stage", name est le module de Modules/Stages vers lequel aller ; quand target est "legacy", name est l'un de "heya", "config", "exit" ou "onlinelounge" (toute autre valeur mène à l'écran titre). transition nomme un module de Modules/Transitions ; si vous l'omettez ou que le moteur ne le trouve pas, le moteur utilise le module nommé "default", et si le skin n'a aucune transition, un simple fondu au noir est joué. Les noms de stage et de transition privilégient le côté de l'appelant : un stage du skin trouve d'abord le module de ce nom dans le skin, un stage de Global/Modules le module global, chacun se rabattant sur l'autre. La clé "heya" reste disponible comme alias : elle ouvre le module `myroom` du skin dans Modules/Stages, ou l'écran titre quand le skin n'en possède pas.
</div>

| Méthode | Description |
| --- | --- |
| `Exit(target?, name?, transition?)  -> number` | Demande la sortie du stage vers la destination donnée, en nommant éventuellement un module cible et un module de transition ; renvoie 0. |

```lua
function update(timestamp)
    if INPUT:KeyboardPressed("S") then
        return Exit("stage", "mystage")        -- aller vers Modules/Stages/mystage
    end
    if INPUT:Pressed("Cancel") then
        return Exit("title", nil, "nokon_curtain")   -- retour à l'écran titre via une transition nommée
    end
end
```

### STAGES

Global des espaces de noms de stages : un stage déclare un espace de noms, et tout module liste les stages d'un espace de noms, par exemple pour construire un menu des stages faits pour lui.

<div class="callout warn">
Enregistré comme global STAGES dans chaque module, mais seul un script de Stage a un espace de noms : dans un autre module, SetNamespace ne fait rien et renvoie false. Un stage a un seul espace de noms à la fois ; un appel ultérieur le remplace, et nil ou "" l'efface. L'appel fonctionne en haut de Script.lua, hors de toute fonction, comme dans n'importe quel callback ; le haut de Script.lua s'exécute quand le moteur crée le stage au chargement du skin, si bien que le stage est listé avant tout onStart. Le moteur garde info comme des données simples sans les lire : chaînes, nombres, booléens et tables de ceux-ci, imbriquées jusqu'à 8 niveaux ; les fonctions et les objets du moteur sont laissés de côté. ListNamespace renvoie une entrée par nom de stage, le stage que Exit("stage", name) ouvrirait depuis le module appelant : un module de skin obtient le stage de ce nom du skin et un module global celui de Global, chacun se rabattant sur l'autre côté, et ce stage n'est listé que s'il a déclaré l'espace de noms. Les entrées sont triées par nom, et chaque info est une copie neuve. Tous les stages existent une fois ceux du skin créés : listez-les dans onStart ou plus tard ; en haut de Script.lua, seuls les stages créés avant l'appelant sont là. [Stages personnalisés](../modules/custom-stages.md) décrit l'espace de noms CStage, que listent les menus Stages personnalisés des skins.
</div>

| Méthode | Description |
| --- | --- |
| `STAGES:SetNamespace(namespace, info?)  -> boolean` | Déclare l'espace de noms du stage appelant, avec une table info facultative rendue aux stages qui le listent ; false hors d'un script de Stage. |
| `STAGES:SetResolution(width, height)  -> boolean` | Déclare la résolution dans laquelle dessine le stage appelant. Si celle du skin diffère, tout ce que dessine le stage est remis à l'échelle de celle du skin, centré et en gardant ses proportions, comme un skin entier de cette résolution ; les coordonnées de la souris, `INPUT:GetSurfaceWidth/Height` et les découpes suivent, et une ROActivity du skin qu'il ouvre garde la résolution du skin. 0 l'efface ; false hors d'un script de Stage. |
| `STAGES:ListNamespace(namespace)  -> table` | Les stages de l'espace de noms sous forme de tableau de tables `{ name, isGlobal, info }`, indexé à partir de 1 : le nom du dossier, true pour un stage de Global/Modules/Stages, et l'info déclarée (une table vide si aucune n'a été donnée). |

```lua
-- Modules/Stages/color_tool/Script.lua, at the top
STAGES:SetNamespace("MyTools", { title = { en = "Colour picker", fr = "Pipette" } })
STAGES:SetResolution(1920, 1080)

-- another stage lists them
function activate()
    for _, s in ipairs(STAGES:ListNamespace("MyTools")) do
        debugLog(s.name .. (s.isGlobal and " (global)" or ""))
    end
end
```

### LOADING

Assistant de barre de chargement pour les fonctions de rappel qui s'exécutent en coroutine : le onStart de chaque type de module, et l'activate d'un Stage.

<div class="callout warn">
Défini comme la globale LOADING dans chaque module. Ces fonctions de rappel s'exécutent sur une coroutine détenue par le moteur, que le moteur reprend à chaque frame : le moteur cède automatiquement la main quand une reprise a épuisé son budget de temps, et vous pouvez aussi céder vous-même avec coroutine.yield(progress) ou LOADING:Tick(sub). Les blocs que vous enregistrez avec LOADING:Add s'exécutent après le retour du corps de la fonction, dans l'ordre, et la barre avance après chacun d'eux ; le poids d'un bloc est sa part de la barre (1 par défaut). LOADING:Tick(sub) cède une frame depuis l'intérieur d'un bloc et rapporte une fraction de 0 à 1 au sein de ce bloc. Hors d'une fonction de rappel en coroutine (l'activate d'une Activity ou d'une ROActivity, ou n'importe quel update ou draw), LOADING:Tick lève une erreur Lua car il n'y a rien à qui céder la main, et les blocs mis en file avec LOADING:Add ne s'exécutent jamais.
</div>

| Méthode | Description |
| --- | --- |
| `LOADING:Add(fn)  -> nil` | Enregistre un bloc de chargement à exécuter après le retour de la fonction de rappel. |
| `LOADING:Add(label, fn)  -> nil` | Enregistre un bloc de chargement étiqueté. |
| `LOADING:Add(label, weight, fn)  -> nil` | Enregistre un bloc de chargement étiqueté avec un poids explicite. |
| `LOADING:Tick(sub)  -> nil` | Cède une frame à l'intérieur d'un bloc, en rapportant une fraction de sous-progression de 0 à 1 au sein du bloc courant. |

```lua
function onStart()
    LOADING:Add("textures", 3, function()
        for i, name in ipairs(names) do
            tx[name] = TEXTURE:CreateTexture(name)
            LOADING:Tick(i / #names)
        end
    end)
    LOADING:Add("sounds", 1, function()
        bgm = SOUND:CreateBGM("Sounds/BGM.ogg")
    end)
end
```

## Activités

### ACTIVITY

Globale permettant de rechercher une Activity chargée par son nom.

<div class="callout warn">
Enregistrée comme la globale ACTIVITY dans chaque module sauf les ROActivities et les arrière-plans, où elle vaut nil ; ces modules utilisent ROACTIVITY. Le moteur charge les Activities depuis Modules/Activities/{name}. ACTIVITY expose aussi GetROActivity, qui se comporte comme ROACTIVITY:GetROActivity. Une recherche privilégie le côté de l'appelant : un module du skin obtient l'Activity de ce nom du skin, un module de Global/Modules celle de Global/Modules/Activities, chacun se rabattant sur l'autre quand son côté n'en a pas. Un fichier de Lib cherche depuis le côté du module qui l'a chargé avec require.
</div>

| Méthode | Description |
| --- | --- |
| `ACTIVITY:GetActivity(name)  -> activity handle` | Renvoie le handle de l'Activity chargée portant le nom de dossier donné, ou nil si aucune n'est chargée. |
| `ACTIVITY:GetROActivity(name)  -> activity handle` | Identique à ROACTIVITY:GetROActivity. |

### ROACTIVITY

Globale permettant de rechercher une Activity en lecture seule (ROActivity) chargée par son nom.

<div class="callout warn">
Enregistrée comme la globale ROACTIVITY dans chaque module. Le moteur charge les ROActivities depuis Modules/ROActivities/{name} et leur donne les globales CONFIG, DATABASE et GetSaveFile en lecture seule, si bien que leurs scripts ne peuvent pas modifier l'état du jeu (voir la section sur les modules en lecture seule de Fonctionnement des modules). Les Activities et les ROActivities sont des singletons indexés par nom : une instance par dossier, que tous les hôtes partagent. GetROActivity privilégie le côté de l'appelant comme le fait ACTIVITY:GetActivity, entre Modules/ROActivities et Global/Modules/ROActivities.
</div>

| Méthode | Description |
| --- | --- |
| `ROACTIVITY:GetROActivity(name)  -> activity handle` | Renvoie le handle de la ROActivity chargée portant le nom de dossier donné, ou nil si aucune n'est chargée. |

### Handle d'activité

L'objet que renvoient ACTIVITY:GetActivity et ROACTIVITY:GetROActivity. Un hôte l'utilise pour piloter les fonctions de rappel du module.

<div class="callout warn">
Activate, Deactivate et Draw transmettent leurs arguments aux fonctions activate, deactivate et draw du module. Update appelle update avec le temps de jeu courant en millisecondes. Chacune d'elles renvoie les valeurs renvoyées par la fonction de rappel sous forme de tableau indexé à partir de 0, ou nil quand la fonction n'a rien renvoyé ou n'est pas définie ; lisez la première valeur avec `result[0]`. Call invoque n'importe quelle fonction globale que le script du module définit.
</div>

| Méthode | Description |
| --- | --- |
| `handle.IsActive  -> boolean` | Vrai après l'exécution d'Activate et jusqu'à celle de Deactivate (ou du DEACTIVATE() du module lui-même). |
| `handle:Activate(...)  -> array` | Appelle la fonction activate du module avec les arguments donnés. |
| `handle:Deactivate(...)  -> array` | Appelle la fonction deactivate du module avec les arguments donnés. |
| `handle:Update()  -> array` | Appelle la fonction update du module avec le temps de jeu courant en millisecondes. |
| `handle:Draw(...)  -> array` | Appelle la fonction draw du module avec les arguments donnés. |
| `handle:Call(functionName, ...)  -> array` | Appelle la fonction globale nommée du script du module avec les arguments donnés. |

```lua
local act = nil

function activate()
    if act == nil then act = ACTIVITY:GetActivity("song_select_core") end
    act:Activate()
end

function update(timestamp)
    local result = act:Update()
    local signal = result ~= nil and result[0] or nil
    if signal == "play" then return Exit("play", nil) end
    if signal == "cancel" then return Exit("title", nil) end
end

function draw()
    act:Draw()
end

function deactivate()
    act:Deactivate()
end
```

### DEACTIVATE

Fonction que le moteur enregistre dans les scripts d'Activity et de ROActivity ; elle permet au module de se désactiver lui-même.

<div class="callout warn">
L'appeler marque le module comme inactif (handle.IsActive passe à false) et exécute la fonction deactivate du module. Les boîtes de dialogue fournies l'appellent quand le joueur confirme ou annule, et l'hôte surveille IsActive pour savoir que la boîte s'est fermée.
</div>

| Méthode | Description |
| --- | --- |
| `DEACTIVATE(...)` | Désactive le module courant et appelle sa fonction deactivate avec les arguments donnés. |

### ROActivities hébergées par le moteur

Le moteur recherche certaines ROActivities par un nom de dossier fixe et les pilote lui-même. Un skin en remplace une en fournissant un dossier Modules/ROActivities de ce nom ; les points d'appel du moteur listés ci-dessous imposent les fonctions de rappel qu'elle doit définir. Quand l'une d'elles manque, le moteur ne dessine pas la fonctionnalité correspondante.

| Nom | Ce que le moteur appelle |
| --- | --- |
| `nameplate` | `activate(player, name, title, dan, data)` quand la plaque d'un joueur change ; `update()` une fois par frame ; `draw(mode, ...)` avec mode 0 = plaque complète `(x, y, opacity, player, side)`, 1 = plaque de dan `(x, y, opacity, danGrade, textTexture)`, 2 = plaque de titre `(x, y, opacity, type, textTexture, rarity, nameplateId)`. |
| `modal` | `activate(player, rarity, modalType, ...)` pour chaque fenêtre modale de déblocage en file, puis `update()` et `draw()` à chaque frame. Le script appelle DEACTIVATE() pour fermer la fenêtre ; le moteur active ensuite la suivante. |
| `modicons` | `activate()` une fois, puis `draw(x, y, player, layout, alpha)` avec layout "menu" ou "game". La globale MODICONS l'enveloppe. |
| `danplate` | `draw(x, y, opacity, danTick, r, g, b, titleText)` sur l'écran de résultats et dans les parcours dan. |
| `results_tower` | `activate(info)` à l'ouverture du résultat de la tour, avec la partie sous forme de LuaResultInfo (des accesseurs en lecture seule comme `ReachedFloor`, `TotalFloors`, `Lives`, `TowerRank`, `IsNewBestFloor`, `GetScore(player)`) ; `update()` et `draw()` à chaque image ; `skip()` via Call à la validation, qui renvoie true tant que l'écran s'anime (cette validation ne fait alors que passer l'animation) ; `deactivate()`. |
| `results_regular` | `activate(info)` à l'ouverture du résultat d'une partie, d'un combat contre l'IA aussi, avec la partie sous forme de LuaResultInfo (des accesseurs par joueur à partir de 0 comme `GetGauge(p)`, `GetClearType(p)`, `GetScoreRank(p)`, `GetScore(p)`, et `IsAIBattle`, `GetAISectionEnd(i)`) ; `update()` et `draw()` à chaque image par-dessus le fond du résultat ; `skip()` via Call à la validation, qui renvoie true tant que l'écran s'anime (le fond reçoit alors `skipAnime`) ; `deactivate()`. |
| `results_dan` | `activate(info)` à l'ouverture du résultat d'une partie dan, avec la partie sous forme de LuaResultInfo (`DanResult`, `DanCombo`, `GetDanSong(i)`, `GetExam(i)`, `exam:GetSong(i)`, `exam.PreviousBestAmount`, `DanGradeChange`, `DanGrade`) ; `update()` et `draw()` à chaque image par-dessus le fond du résultat dan ; `skip()` via Call à la validation, qui renvoie true tant que l'écran s'anime ; puis `advance()` via Call à la validation, qui renvoie true quand le module montre un écran de plus avant les fenêtres de récompense (le moteur a déjà mis un grade dan nouveau ou meilleur sur la plaque du joueur, le module dessine donc `PreviousNameplateDan` jusqu'à le révéler) ; `swap()` via Call sur gauche ou droite, qui renvoie true quand le module passe d'un de ses écrans à l'autre ; `deactivate()`. |
| `tower_hud` | `activate(maxFloor)` au début d'une partie de tour ; `update()` et `draw()` à chaque image au-dessus de l'arrière-plan supérieur ; `deactivate()`. Il lit l'étage et les vies dans PLAYSTATE. |
| `popup_menu` | `activate(title, items, fontSize, ...)` où items sont les libellés joints par des sauts de ligne, suivis des positions PopupMenu du skin ; `draw(selected)` à chaque frame ; `deactivate()` à la fermeture. |
| `config_ui` | `activate(model)` avec le modèle des paramètres ; `update()` à chaque frame, renvoyant "exit" pour quitter l'écran des paramètres, suivi en option du nom du module Modules/Transitions pour le retour (le fondu simple joue à la place quand un changement de skin, de résolution ou de périphérique audio reste à appliquer) ; `draw()` ; `reload(model)` via Call quand le moteur reconstruit le modèle ; `deactivate()`. |
| `song_enum` | `activate()`, puis `draw(isCommandSongDataGet, done, total)` à chaque frame pendant que l'analyse des chansons s'exécute ; `deactivate()`. |
| `cutscene` | `activate(paths, fadeOutSeconds)` avec les fichiers vidéo dans l'ordre et un fondu au noir après le dernier (0 pour aucun) ; `update()` à chaque frame, renvoyant "finished" une fois toutes les vidéos jouées et le fondu terminé ; `draw()` ; `deactivate()`. Il joue chaque fichier avec sa propre piste audio et possède le popup de pause ; l'usage de la cinématique regarde l'appelant. |

Les trois ROActivities de résultats jouent elles-mêmes la musique de l'écran de résultats. Tant qu'une fenêtre de récompense s'affiche, le moteur la baisse via Call avec `setMusicLevel(percent)`, de 0 à 100. Une fois l'écran passé au noir, le moteur désactive le module, et sa musique s'arrête avec lui.

### Informations de résultat

L'objet LuaResultInfo que l'étape de résultats passe au callback activate de results_tower, results_regular et results_dan.

<div class="callout warn">
L'étape de résultats le remplit une fois la partie enregistrée, et il ne change plus ensuite. Chaque membre est en lecture seule : les propriétés utilisent la syntaxe à point (`info.Title`), les méthodes la syntaxe à deux-points. Les arguments de joueur commencent à 0 ; hors limites, une méthode renvoie 0, false ou une chaîne vide. Un membre marqué d'un nom de module n'est rempli que pour cet écran ; sur les autres, il garde cette valeur par défaut.
</div>

| Méthode | Description |
| --- | --- |
| `info.Title  -> string` | Le titre du chart. |
| `info.Subtitle  -> string` | Le sous-titre du chart. |
| `info.PlayerCount  -> number` | Nombre de joueurs (1 pour les résultats de tour et de dan). |
| `info.PlayerSide  -> number` | Le côté de la plaque du joueur 1 : 1 quand il joue du côté bleu, sinon 0. |
| `info:GetGoodCount(player)  -> number` | Coups Good. |
| `info:GetOkCount(player)  -> number` | Coups Ok. |
| `info:GetBadCount(player)  -> number` | Coups Bad. |
| `info:GetRollCount(player)  -> number` | Coups de roulement. |
| `info:GetMaxCombo(player)  -> number` | Combo maximal. |
| `info:GetADLibCount(player)  -> number` | Notes AD-LIB touchées. |
| `info:GetBoomCount(player)  -> number` | Bombes touchées. |
| `info:GetScore(player)  -> number` | Score final. |
| `info:GetPreviousBestScore(player)  -> number` | Le meilleur score du joueur sur ce chart et cette difficulté avant l'enregistrement de cette partie, 0 s'il n'y en a pas. |
| `info:IsSaved(player)  -> boolean` | Vrai quand la partie a été enregistrée dans la sauvegarde du joueur (jamais en auto, pour un replay regardé ni pour un dan construit à l'exécution comme ceux de la pagode). |
| `info:GetDifficulty(player)  -> number` | results_regular: la difficulté jouée, de 0 Easy à 4 Extra. |
| `info:GetGauge(player)  -> number` | results_regular: la jauge d'âme en fin de partie, de 0 à 100. |
| `info:GetGaugeType(player)  -> number` | results_regular: 0 normale, 1 difficile, 2 extrême. |
| `info:GetNorma(player)  -> number` | results_regular: la valeur de jauge à partir de laquelle une jauge normale réussit. |
| `info:GetKillZone(player)  -> number` | results_regular: la zone mortelle de la jauge extrême, de 0 à 100 (0 pour les autres jauges). |
| `info:GetClearType(player)  -> number` | results_regular: 0 non réussie, 1 réussie avec aide, 2 réussie, 3 full combo, 4 parfaite. |
| `info:GetScoreRank(player)  -> number` | results_regular: 0 aucun, puis les rangs de score de 1 à 7. |
| `info:IsCleared(player)  -> boolean` | results_regular: vrai quand la partie est réussie ; dans un combat contre l'IA, quand le joueur 1 a gagné. |
| `info:GetSpeechText(player, mood)  -> string` | results_regular: la réplique de résultat du personnage du joueur pour une humeur, de 0 (la pire) à 5 (parfaite). |
| `info.IsAIBattle  -> boolean` | results_regular: vrai pour un combat contre l'IA. |
| `info.AISectionCount  -> number` | results_regular: les sections du combat contre l'IA. |
| `info:GetAISectionEnd(section)  -> number` | results_regular: comment une section (à partir de 0) s'est terminée : 0 pas jouée jusqu'au bout, 1 gagnée, 2 perdue. |
| `info.TowerType  -> string` | results_tower: le TOWERTYPE du chart, "" s'il n'en indique aucun. |
| `info.TowerSide  -> number` | results_tower: 0 Sweet, 1 Spicy. |
| `info.ReachedFloor  -> number` | results_tower: l'étage atteint. |
| `info.TotalFloors  -> number` | results_tower: les étages de la tour. |
| `info.Lives  -> number` | results_tower: les vies restantes. |
| `info.MaxLives  -> number` | results_tower: les vies au départ. |
| `info.TowerRank  -> number` | results_tower: 0 aucun, puis de 1 à 7 : 10, 25, 50 et 75 %, réussie, full combo, parfaite. |
| `info.PreviousBestFloor  -> number` | results_tower: le meilleur étage avant l'enregistrement de cette partie. |
| `info.IsNewBestFloor  -> boolean` | results_tower: vrai quand une partie enregistrée a battu le meilleur étage. |
| `info.IsNewBestScore  -> boolean` | results_tower: vrai quand une partie enregistrée a battu le meilleur score. |
| `info.IsTired  -> boolean` | results_tower: vrai quand la partie s'est terminée avec le personnage de la tour fatigué (peu de vies restantes). |
| `info.Bpm  -> number` | results_tower: le BPM sur lequel la partie s'est terminée. |
| `info.DanTick  -> number` | results_dan: les traits de la plaque de dan (DANTICK). |
| `info.DanTickColor  -> color` | results_dan: la couleur des traits (DANTICKCOLOR). |
| `info.DanResult  -> number` | results_dan: 0 échoué, 1 réussi (rouge), 2 réussi (or). |
| `info.DanCombo  -> number` | results_dan: 0, 1 aucun Bad (full combo), 2 aucun Bad ni Ok (parfait). |
| `info.TotalHits  -> number` | results_dan: les coups Good, Ok et de roulement ensemble. |
| `info.DanSongCount  -> number` | results_dan: les chansons du dan. |
| `info.DanSongReached  -> number` | results_dan: la chanson sur laquelle la partie s'est terminée, à partir de 0. |
| `info.DanExamSong  -> number` | results_dan: la chanson dont les examens par chanson ont été affichés en dernier, à partir de 0. |
| `info:GetDanSong(song)  -> dan song` | results_dan: la chanson i du dan (à partir de 0), ou nil. |
| `info.ExamCount  -> number` | results_dan: les examens du dan. |
| `info:GetExam(exam)  -> dan exam` | results_dan: l'examen i (à partir de 0, dans l'ordre du chart), ou nil. |
| `info.DanGradeChange  -> number` | results_dan: 0 aucun, 1 un nouveau grade dan, 2 un meilleur grade sur un dan déjà réussi (or, ou un meilleur clear). Le moteur met un grade nouveau ou meilleur sur la plaque du joueur. |
| `info.DanGrade  -> dan grade` | results_dan: le grade du dan dans la sauvegarde après cette partie ; son Title vaut "" si le dan n'a jamais été réussi. |
| `info.PreviousNameplateDan  -> dan grade` | results_dan: le grade dan que la plaque affichait avant cette partie (Title "" s'il n'y en avait pas). |

### Chanson de dan

L'objet que renvoie info:GetDanSong, en lecture seule.

| Méthode | Description |
| --- | --- |
| `song.Title  -> string` | Le titre de la chanson ; "???" pour une chanson que le chart garde cachée. |
| `song.Subtitle  -> string` | Le sous-titre de la chanson ; "" pour une chanson que le chart garde cachée. |
| `song.Difficulty  -> number` | De 0 Easy à 4 Extra. |
| `song.Level  -> number` | Le niveau de la chanson. |
| `song.Good  -> number` | Coups Good sur la chanson. |
| `song.Ok  -> number` | Coups Ok sur la chanson. |
| `song.Bad  -> number` | Coups Bad sur la chanson. |
| `song.Roll  -> number` | Coups de roulement sur la chanson. |

### Examen de dan

L'objet que renvoient info:GetExam et exam:GetSong, en lecture seule : un examen tel que la partie s'est terminée.

| Méthode | Description |
| --- | --- |
| `exam.Type  -> number` | 0 jauge, 1 Good, 2 Ok, 3 Bad, 4 score, 5 roulements, 6 coups, 7 combo, 8 précision, 9 AD-LIB, 10 BOOM ; LANG:GetExamName(type) donne son nom. |
| `exam.Range  -> number` | 0 au moins Value, 1 moins que Value. |
| `exam.Value  -> number` | La valeur à atteindre pour réussir (rouge). |
| `exam.Amount  -> number` | La valeur affichée ; pour un examen « moins que », ce qu'il restait avant de l'échouer. |
| `exam.PreviousBestAmount  -> number` | Le meilleur Amount du joueur sur cet examen avant l'enregistrement de cette partie, dans les mêmes termes (plus grand est meilleur) ; -1 s'il n'y en a pas. Pour un examen par chanson, chaque chanson (exam:GetSong) a le sien. |
| `exam.Percent  -> number` | Amount par rapport à Value, de 0 à 100. |
| `exam.Status  -> number` | 0 échoué, 1 réussi, 2 réussi en or. |
| `exam.IsPerSong  -> boolean` | Vrai pour un examen jugé chanson par chanson ; l'examen lui-même est alors celui de la dernière chanson. |
| `exam:GetSong(song)  -> dan exam` | L'examen par chanson sur la chanson i (à partir de 0), ou nil. |

### Grade de dan

L'objet que renvoient info.DanGrade et info.PreviousNameplateDan, en lecture seule.

| Méthode | Description |
| --- | --- |
| `grade.Title  -> string` | Le titre du dan tel que la plaque l'affiche (jusqu'aux quatre premiers caractères du titre du chart) ; "" s'il n'y en a pas. |
| `grade.Gold  -> boolean` | true pour une réussite en or. |
| `grade.Tier  -> number` | 0 clear, 1 full combo, 2 perfect. |

## Arrière-plans

### Module d'arrière-plan

Un Script.lua qui dessine un arrière-plan d'écran, une couche de gameplay, une foule, une animation de réussite ou un effet de kusudama, hébergé par les écrans propres du moteur.

<div class="callout warn">
Les arrière-plans vivent hors du dossier Modules, sous le dossier Graphics du skin, dans le répertoire de l'écran qu'ils décorent, par exemple Graphics/0_Startup/Script.lua, Graphics/6_Result/Script.lua, Graphics/5_Game/5_Background/Normal/Up/{variant}/Script.lua, Graphics/5_Game/5_Background/Normal/Down/{variant}/Script.lua, Graphics/5_Game/3_Mob/{variant}/Script.lua, Graphics/5_Game/9_End/{result}/Script.lua et Graphics/5_Game/11_Balloon/Kusudama/Script.lua. Quand un dossier contient plusieurs variantes, le moteur en choisit une au hasard (ou d'après le préréglage de scène de la partition) à chaque partie. L'écran hôte crée une instance d'arrière-plan (les arrière-plans de gameplay à chaque fois que le moteur entre dans l'écran de jeu) et la libère avec l'écran, si bien que plusieurs sont actives en même temps pendant le gameplay. Un script d'arrière-plan reçoit les mêmes globales qu'une ROActivity (CONFIG, DATABASE et GetSaveFile en lecture seule ; pas d'ACTIVITY). Les crochets d'événement ci-dessous sont facultatifs, et le moteur appelle chacun d'eux une fois quand son événement se produit. Pour la tour, le `TOWERTYPE` du chart nomme le dossier Down.
</div>

| Méthode | Description |
| --- | --- |
| `clearIn(player)` | Arrière-plans de gameplay Up et Down : la jauge du joueur a atteint la zone de réussite. |
| `clearOut(player)` | Arrière-plans de gameplay Up et Down : la jauge du joueur est retombée sous la zone de réussite. |
| `playEndAnime(player)` | Animations de réussite (Graphics/5_Game/9_End) : l'animation de fin démarre pour le joueur. |
| `kusuIn()` / `kusuBroke()` / `kusuMiss()` | Kusudama : le ballon apparaît, le joueur le brise, ou le joueur le rate. |
| `skipAnime()` | Arrière-plan de résultats : le joueur a passé l'animation des résultats. |

### État d'arrière-plan

L'objet que l'hôte passe aux fonctions activate, update et draw d'un arrière-plan.

<div class="callout warn">
Une instance par hôte, que l'hôte met à jour sur place à chaque frame. Les champs tableau partagent les tableaux par joueur du moteur et sont indexés à partir de 0 (`state.gauge[0]` est le joueur 1). Seuls les hôtes de gameplay rafraîchissent les champs de gameplay ; les autres hôtes les laissent à leurs valeurs par défaut, et timeStamp reste à -1 hors gameplay. L'état ne porte aucun minutage des frames : lisez la globale fps.
</div>

| Méthode | Description |
| --- | --- |
| `state.playerCount  -> number` | Nombre de joueurs. |
| `state.p1IsBlue  -> boolean` | Vrai quand le joueur 1 utilise le côté bleu. |
| `state.lang  -> string` | Code de langue courant. |
| `state.simplemode  -> boolean` | Vrai quand le mode simple est activé. |
| `state.puchicharaRarities  -> string[]` | Rareté du puchichara de chaque joueur. |
| `state.characterRarities  -> string[]` | Rareté du personnage de chaque joueur. |
| `state.isClear  -> boolean[]` | Si chaque joueur est actuellement dans la zone de réussite. Dans un dan avec la jauge normale, il devient vrai au premier coup et faux dès que la partie ou un examen échoue. |
| `state.gauge  -> number[]` | Valeur de la jauge de chaque joueur. |
| `state.bpm  -> number[]` | BPM courant de chaque joueur. |
| `state.gogo  -> boolean[]` | Si chaque joueur est en go-go time. |
| `state.towerNightNum  -> number` | Facteur jour-vers-nuit de la Tour, de 0 à 1. |
| `state.towerFloor  -> number` | Étage en cours, à partir de 0 (jeu de la tour). |
| `state.towerMaxFloor  -> number` | Nombre d'étages du chart (jeu de la tour). |
| `state.battleState  -> number` | Code d'état de la bataille contre l'IA. |
| `state.battleWin  -> boolean` | Vrai quand le joueur est en train de gagner la bataille contre l'IA. |
| `state.timeStamp  -> number` | Temps synchronisé sur la partition, en secondes ; -1 hors gameplay. |
| `state.paused  -> boolean` | Vrai pendant la pause du gameplay. |
| `state.player  -> number` | Le joueur pour lequel un hôte par joueur (animations de réussite) dessine. |
| `state.hitRing  -> number` | Nombre de coups récents conservés dans hitNote, hitBig et hitJudge (32). |
| `state.hitCount  -> number[]` | Coups de chaque joueur depuis le démarrage de l'arrière-plan : notes jugées (ratés compris), frappes de roulement et ballons éclatés. Le n-ième coup, compté à partir de 0, se trouve à l'indice n % hitRing de hitNote, hitBig et hitJudge. |
| `state.hitNote  -> string[][]` | Note de chaque coup conservé, par joueur : "don", "ka" ou "kadon". |
| `state.hitBig  -> boolean[][]` | Vrai quand le coup conservé était une grosse note, par joueur. |
| `state.hitJudge  -> string[][]` | Jugement de chaque coup conservé, par joueur : "perfect", "great", "good", "poor", "miss", "roll" ou "balloon". |
| `state.landCount  -> number[]` | Notes volantes de chaque joueur arrivées au bout de leur vol depuis le démarrage de l'arrière-plan. |
| `state:SetFlyTarget(player, x, y)` | Tant que cet arrière-plan de gameplay est affiché, les notes volantes du joueur atterrissent sur (x, y) en pixels d'écran et l'effet d'atterrissage du skin est omis. |
| `state:ClearFlyTarget(player)` | Les notes volantes du joueur atterrissent de nouveau à l'emplacement habituel du skin. |

## Transitions

### Module de transition

Un Modules/Transitions/{name}/Script.lua qui dessine les phases de fondu de sortie, de chargement et de fondu d'entrée entre deux stages.

<div class="callout warn">
Le troisième argument d'Exit sélectionne la transition ; le moteur utilise "default" quand l'appel n'en nomme aucune ou que le moteur ne trouve pas le nom. Le chargement vers le gameplay après Exit("play") utilise toujours la transition nommée "song_loading", ou "default" si le skin n'en a pas. Le moteur enchaîne les phases dans l'ordre : il appelle fadeOut(t) à chaque frame par-dessus le stage sortant jusqu'à ce que t atteigne 1, puis démonte le stage sortant et charge le nouveau tout en appelant loading(progress, elapsed) à chaque frame, puis appelle fadeIn(t) par-dessus le nouveau stage jusqu'à ce que t atteigne 1. Chaque fondu dure 0,5 seconde sauf si le script définit FADE_OUT_SECONDS ou FADE_IN_SECONDS ; le moteur ignore une valeur qui n'est pas un nombre positif. Lors d'un changement de stage, le moteur n'appelle loading qu'une fois que le chargement a dépassé 0,5 seconde ; avant cela, il appelle fadeOut(1) pour que les chargements courts ne fassent pas clignoter un écran de chargement. Le chemin de chargement de chanson affiche la phase de chargement immédiatement. Une BGM que le nouveau stage lance pendant le chargement ou le fondu d'entrée attend et joue à la fin du fondu d'entrée, sauf lors du chargement vers le gameplay ; les sons propres de la transition jouent tout de suite. Pendant le fondu d'entrée, le nouveau stage ne reçoit aucune entrée, sauf quand le fondu révèle le gameplay. Si le joueur annule le chargement vers le jeu, le moteur revient à la sélection des chansons et la révèle avec fadeIn(t) : chaque transition se termine donc par un appel à fadeIn(1).
</div>

| Méthode | Description |
| --- | --- |
| `FADE_OUT_SECONDS  -> number` | Globale de premier niveau facultative : durée du fondu de sortie en secondes. |
| `FADE_IN_SECONDS  -> number` | Globale de premier niveau facultative : durée du fondu d'entrée en secondes. |

```lua
FADE_OUT_SECONDS = 0.3
FADE_IN_SECONDS = 0.3

local pixel = nil

local function cover(alpha)
    pixel:SetColor(0, 0, 0)
    pixel:SetOpacity(alpha)
    pixel:SetScale(8000, 8000)
    pixel:Draw(0, 0)
end

function onStart()
    pixel = TEXTURE:CreateTexture("pixel.png")
end

function fadeOut(t) cover(t) end
function fadeIn(t) cover(1.0 - t) end
function loading(progress, elapsed) cover(1.0) end

function onDestroy()
    if pixel ~= nil then pixel:Dispose() end
end
```

## Minutage et caméra

### COUNTER

Fabrique de compteurs d'animation qui font évoluer une valeur d'une valeur de début à une valeur de fin au fil du temps.

<div class="callout warn">
Enregistrée comme la globale COUNTER. Un compteur n'avance que quand vous appelez Tick, du delta de frame divisé par interval, où interval est le nombre de secondes par unité de valeur. Le signe d'interval doit correspondre à la direction : positif quand end est supérieur à begin, négatif quand il est inférieur. Si les signes ne correspondent pas, le compteur échange les extrémités et se termine à son premier tick. CreateCounterDuration prend la durée totale et choisit le signe lui-même. Un interval nul ou des valeurs begin et end égales terminent le compteur à son premier tick. Le compteur appelle la fonction ended facultative quand la valeur atteint la fin, et une fois par cycle terminé en boucle ou en aller-retour.
</div>

| Méthode | Description |
| --- | --- |
| `COUNTER:CreateCounter(begin, end, interval, ended?)  -> counter` | Crée un compteur qui va de begin à end à raison d'interval secondes par unité, en appelant ended à la fin. |
| `COUNTER:CreateCounterDuration(begin, end, seconds, ended?)  -> counter` | Crée un compteur qui va de begin à end sur le nombre de secondes donné ; renvoie un compteur vide si seconds n'est pas positif ou si begin est égal à end. |
| `COUNTER:EmptyCounter()  -> counter` | Crée un compteur inerte dont la valeur reste à 0, utilisable comme substitut. |

### Handle de compteur

Un compteur créé par COUNTER.

<div class="callout warn">
Lisez Value à chaque frame et appelez Tick à chaque frame pour le faire avancer ; un compteur que vous n'avez pas démarré, ou qui s'est arrêté, ignore Tick. Begin, End et Interval sont des champs accessibles en lecture et en écriture. SetLoop et SetBounce s'excluent mutuellement. SetEasing ne façonne que la Value rapportée ; le compteur continue d'avancer linéairement en dessous. Les écouteurs reçoivent la valeur courante à chaque tick, y compris le dernier.
</div>

| Méthode | Description |
| --- | --- |
| `counter.Value  -> number` | La valeur courante, avec l'easing appliqué s'il est défini ; l'assigner fait sauter le compteur. |
| `counter.Begin  -> number` | La valeur de départ (lecture et écriture). |
| `counter.End  -> number` | La valeur de fin (lecture et écriture). |
| `counter.Interval  -> number` | Secondes par unité de valeur (lecture et écriture). |
| `counter:Start()  -> nil` | Remet la valeur à Begin et démarre les ticks. |
| `counter:Resume()  -> nil` | Reprend les ticks sans réinitialiser la valeur. |
| `counter:Stop()  -> nil` | Arrête les ticks. |
| `counter:Pause()  -> nil` | Identique à Stop. |
| `counter:Reset()  -> nil` | Remet la valeur à Begin sans changer si le compteur avance ou non. |
| `counter:Tick()  -> nil` | Fait avancer la valeur d'une frame, en appelant les écouteurs et la fonction ended selon le cas. |
| `counter:SetLoop(loop)  -> nil` | Revient à Begin quand la valeur atteint la fin ; désactive l'aller-retour. |
| `counter:SetBounce(bounce)  -> nil` | Inverse la direction quand la valeur atteint l'une des extrémités ; désactive la boucle. |
| `counter:GetLoop()  -> boolean` | Si la boucle est activée. |
| `counter:GetBounce()  -> boolean` | Si l'aller-retour est activé. |
| `counter:SetEasing(type, function)  -> nil` | Applique une courbe d'easing à la valeur rapportée ; type est IN, OUT, INOUT ou OUTIN et function est LINEAR, SINE, QUAD, CUBIC, QUART, QUINT, EXPO, CIRC, ELASTIC, BACK ou BOUNCE (insensible à la casse ; le compteur ignore un nom inconnu). |
| `counter:ClearEasing()  -> nil` | Retire l'easing. |
| `counter:Listen(listener)  -> nil` | Enregistre une fonction appelée avec la valeur courante à chaque tick. |
| `counter:ClearListeners()  -> nil` | Retire tous les écouteurs. |

```lua
local fade = nil

function activate()
    fade = COUNTER:CreateCounterDuration(0, 1, 0.5, function() debugLog("fade done") end)
    fade:SetEasing("OUT", "QUAD")
    fade:Start()
end

function update(timestamp)
    fade:Tick()
end

function draw()
    background:SetOpacity(fade.Value)
    background:Draw(0, 0)
end
```

### GLOBALCAMERA

La caméra 2D plein écran : elle déplace, zoome et fait pivoter l'ensemble de la frame rendue et ajoute un tremblement d'écran décroissant.

<div class="callout warn">
Enregistrée comme la globale GLOBALCAMERA. Elle pilote la même transformation d'écran que les commandes TJA #CAMERA et affecte tout ce qui est dessiné, y compris les scènes 3D blittées. Les décalages sont en pixels de référence 1280x720, la rotation est en degrés et un zoom de 1 signifie aucune mise à l'échelle. La transformation de base persiste jusqu'à ce que vous la modifiiez : appelez Update(dt) à chaque frame pour l'appliquer et faire avancer le tremblement, et Reset() quand vous quittez le stage pour qu'elle ne se propage pas au suivant. Vous réglez la caméra propre à une scène 3D sur l'objet scène.
</div>

| Méthode | Description |
| --- | --- |
| `GLOBALCAMERA:SetOffset(x, y)  -> nil` | Décale l'écran de (x, y) pixels. |
| `GLOBALCAMERA:SetZoom(sx, sy)  -> nil` | Zoome l'écran avec des facteurs X et Y distincts. |
| `GLOBALCAMERA:SetUniformZoom(s)  -> nil` | Zoome l'écran uniformément. |
| `GLOBALCAMERA:SetRotation(deg)  -> nil` | Fait pivoter l'écran autour de son centre. |
| `GLOBALCAMERA:GetOffsetX()  -> number` | Le décalage X de base. |
| `GLOBALCAMERA:GetOffsetY()  -> number` | Le décalage Y de base. |
| `GLOBALCAMERA:GetZoomX()  -> number` | Le facteur de zoom X. |
| `GLOBALCAMERA:GetZoomY()  -> number` | Le facteur de zoom Y. |
| `GLOBALCAMERA:GetRotation()  -> number` | La rotation de base en degrés. |
| `GLOBALCAMERA:Shake(amplitudePx, seconds, rotAmpDeg?)  -> nil` | Démarre un tremblement qui décroît linéairement depuis l'amplitude en pixels donnée sur le nombre de secondes donné, avec une oscillation de rotation facultative en degrés. La caméra ignore un appel avec seconds inférieur ou égal à 0 ; un nouveau tremblement ne remplace le courant que si son amplitude est égale ou supérieure. |
| `GLOBALCAMERA.IsShaking  -> boolean` | Vrai tant qu'un tremblement décroît encore. |
| `GLOBALCAMERA:Update(dt)  -> nil` | Fait avancer le tremblement de dt secondes (plafonné à 0,25) et applique à l'écran la transformation de base plus le tremblement. |
| `GLOBALCAMERA:Reset()  -> nil` | Recentre la caméra, réinitialise le zoom et la rotation et arrête tout tremblement. |

```lua
function update(timestamp)
    if INPUT:Pressed("LRed") or INPUT:Pressed("RRed") then GLOBALCAMERA:Shake(18, 0.35) end
    GLOBALCAMERA:Update(fps.deltaTime)
end

function deactivate()
    GLOBALCAMERA:Reset()
end
```

## Énumération des chansons

Deux fonctions globales rapportent l'état de l'analyse des chansons. Utilisez-les avec la fonction de rappel afterSongEnum.

| Méthode | Description |
| --- | --- |
| `IsSongsEnumerating()  -> boolean` | Vrai pendant qu'une passe d'énumération des chansons est en cours. |
| `IsSongsEnumDone()  -> boolean` | Vrai une fois l'analyse des chansons terminée. Faux avant le début de l'analyse et pendant son exécution ; IsSongsEnumerating est faux à la fois dans l'état non démarré et dans l'état terminé, vérifiez donc cette fonction pour savoir que la liste est prête. |

## Diagnostics

### info

Objet en lecture seule contenant l'état de base du jeu et le répertoire du module.

<div class="callout warn">
Enregistré comme la globale info et créé par module. Chaque champ calcule sa valeur à l'accès ; online interroge le système d'exploitation à chaque fois que vous le lisez.
</div>

| Méthode | Description |
| --- | --- |
| `info.playerCount  -> number` | Le nombre de joueurs configuré. |
| `info.lang  -> string` | Le code de langue courant. |
| `info.simplemode  -> boolean` | Vrai quand le mode simple est activé. |
| `info.p1IsBlue  -> boolean` | Vrai quand le joueur 1 utilise le côté bleu. |
| `info.online  -> boolean` | Vrai quand une interface réseau utilisable est disponible. |
| `info.dir  -> string` | Le répertoire de ce module. |

### fps

Objet en lecture seule contenant le minutage des frames et une horloge haute résolution.

| Méthode | Description |
| --- | --- |
| `fps.deltaTime  -> number` | Secondes écoulées depuis la frame précédente. |
| `fps.fps  -> number` | Le nombre d'images par seconde mesuré actuellement. |
| `fps.ms  -> number` | Une horloge monotone en millisecondes, pour chronométrer des sections de Lua par différence. |

### debugLog

| Méthode | Description |
| --- | --- |
| `debugLog(message)  -> nil` | Écrit la chaîne dans le journal de trace du moteur avec un préfixe qui la marque comme un journal Lua. |

## Autres globales

Le moteur enregistre ces globales dans chaque module ; leurs propres pages les documentent.

| Globale | Page |
| --- | --- |
| `GetSaveFile(player)` | [Joueurs et profils](players.md). Renvoie un handle en lecture seule dans les ROActivities et les arrière-plans. |
| `RequestSongList(settings)`, `GenerateSongListSettings()` | [Chansons et partitions](songs.md). |
| `MODICONS` | [Chansons et partitions](songs.md). |
| `CONFIG`, `DATABASE`, `SHARED`, `STORAGE`, `JSONLOADER`, `INILOADER`, `SQL` | [Données et persistance](data.md). |
| `TEXTURE`, `CANVAS`, `GRAPHICS`, `TEXT`, `VIDEO`, `COLOR`, `GRADIENT`, `SIZE` | [Graphismes et texte](graphics.md). |
| `SOUND`, `HITSOUNDSLIST` | [Audio](audio.md). |
| `INPUT` | [Entrées](input.md). |
| `NAMEPLATE`, `NAMEPLATESLIST`, `CHARACTER`, `CHARACTERLIST`, `PUCHICHARALIST`, `PLAYSTATE`, `THEME`, `LANG` | [Joueurs et profils](players.md). |
| `VECTOR`, `VECTOR2`, `VECTOR3`, `VECTOR4`, `MATRIX`, `MATRIX2`, `MATRIX3`, `MATRIX4`, `QUATERNION` | [Mathématiques](math.md). |
| `SONGMOUNT`, `REPLAY`, `DANBUILDER`, `VIRTUALSLOTS` | [Chansons et partitions](songs.md). |
| `NET` | [Réseau en ligne](networking.md). |
| `SCENE3D`, `MODEL`, `PHYSICS`, `COLLIDERS`, `PATHFIND`, `HEIGHTMAP` | [Moteur 3D : monde du rastériseur](3d.md), [Moteur 3D : monde du path tracer](3d-raytrace.md), [Moteur 3D : physique](3d-physics.md) <span class="badge-exp">Expérimental</span>. |
