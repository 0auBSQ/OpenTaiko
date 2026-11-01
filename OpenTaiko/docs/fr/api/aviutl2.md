<!-- api/aviutl2.md -->

# Projets AviUtl2

La globale `AVIUTL2` joue les fichiers de projet enregistrés par l'éditeur vidéo AviUtl2 (`.aup2`). Le jeu lit le projet et dessine ses objets, ses mouvements et ses effets en direct, si bien qu'un skin peut fournir une animation sous la forme d'un projet et de ses images à la place d'une vidéo rendue.

Le script qui charge un projet est propriétaire de son handle, et le jeu libère le handle quand il décharge ce script. Appelez `Dispose()` vous-même pour le libérer plus tôt.

<div class="callout warn">
Le jeu dessine le projet avec son propre moteur de rendu. Les positions, les tailles, les rotations, les courbes de mouvement, le contrôle de caméra et les effets courants suivent l'éditeur. Les mouvements aléatoires et les tremblements utilisent des valeurs aléatoires différentes, et certains effets sont des approximations. Après le chargement d'un projet, lisez GetUnsupported et GetApproximated pour voir ce que le jeu a omis ou simplifié.
</div>

## Chargement

### AVIUTL2

| Méthode | Description |
| --- | --- |
| `AVIUTL2:LoadProject(path)  -> project` | Ouvre un fichier de projet et renvoie son handle. Le chemin est relatif au répertoire du script. |
| `AVIUTL2:LoadProjectFromAbsolutePath(path)  -> project` | Ouvre un fichier de projet depuis un chemin complet. |

<div class="callout warn">
Un projet enregistre les chemins complets de la machine sur laquelle il a été sauvegardé. Le jeu cherche chaque image, vidéo, son et modèle à côté du fichier de projet, d'abord d'après les dossiers que le chemin enregistré partage avec celui du projet, puis d'après le nom de fichier. Gardez les fichiers du projet dans son dossier ou dans ses sous-dossiers. Un projet manquant ou illisible renvoie un handle dont Loaded vaut false.
</div>

## Handle de projet

Un projet renvoyé par `AVIUTL2:LoadProject`. Ses méthodes de lecture portent les mêmes noms que celles d'un handle de vidéo.

### Lecture

| Méthode | Description |
| --- | --- |
| `project:Start()  -> nil` | Démarre la lecture depuis le début. |
| `project:Resume()  -> nil` | Reprend la lecture après Pause. |
| `project:Pause()  -> nil` | Met la lecture en pause, sons compris. |
| `project:Stop()  -> nil` | Arrête la lecture. |
| `project:Reset()  -> nil` | Revient au début. |
| `project.IsPlaying  -> bool` | True tant que le projet joue et n'a pas atteint sa fin. |
| `project:IsFinished()  -> bool` | True une fois qu'un projet qui ne boucle pas a atteint sa fin. |
| `project:GetTimestampMs()  -> number` | Position courante en millisecondes. |
| `project:SetTimestampMs(ms)  -> nil` | Se positionne à une position en millisecondes. |
| `project:GetFrame()  -> number` | Position courante en frames du projet. |
| `project:SetFrame(frame)  -> nil` | Se positionne à une frame. |
| `project:GetSpeed()  -> number` | Multiplicateur de vitesse de lecture courant (1 = normal). |
| `project:SetSpeed(speed)  -> nil` | Définit le multiplicateur de vitesse de lecture. |
| `project:GetLoop()  -> bool` | True quand le projet recommence à sa fin. |
| `project:SetLoop(loop)  -> nil` | Fait recommencer le projet à sa fin. |
| `project:GetVolumePercent()  -> number` | Volume des sons du projet en pourcentage. |
| `project:SetVolumePercent(vol)  -> nil` | Règle le volume des sons du projet en pourcentage. |
| `project:Dispose()  -> nil` | Libère le projet, ses images, ses vidéos et ses sons. |

### Dessin

| Méthode | Description |
| --- | --- |
| `project:Draw(x, y)  -> nil` | Dessine la frame courante avec son coin supérieur gauche en (x, y), à l'échelle définie par SetScale. |
| `project:DrawFit(x, y, w, h)  -> nil` | Dessine la frame courante aussi grande que le cadre le permet, en conservant ses proportions, centrée dans le cadre. |
| `project:SetScale(scaleX, scaleY)  -> nil` | Définit l'échelle utilisée par Draw (1 = la taille propre du projet). |
| `project:SetOpacity(opacity)  -> nil` | Définit l'opacité de toute la frame, de 0 à 1. |
| `project:SetBackground(r, g, b, a)  -> nil` | Remplit la frame derrière les objets avec une couleur, chaque valeur allant de 0 à 255. Un alpha de 0 laisse la frame transparente. |
| `project:ClearBackground()  -> nil` | Revient à l'arrière-plan demandé par le fichier de projet. |
| `project.HasProjectBackground  -> bool` | True quand la scène a sa propre couleur d'arrière-plan. |
| `project.ProjectBackground  -> int` | La couleur d'arrière-plan de la scène sous la forme 0xRRGGBB, ou -1 quand elle n'en a pas. |

<div class="callout warn">
Une scène sans couleur d'arrière-plan est transparente là où aucun objet n'est dessiné, si bien que le projet se dessine par-dessus ce que le script a dessiné avant lui. L'éditeur affiche une telle scène sur du noir. Dessinez d'abord un rectangle noir, ou appelez SetBackground(0, 0, 0, 255), pour obtenir la même image.
</div>

### Images dessinées par le script

Un script peut remplacer une image du projet par son propre dessin, par exemple pour afficher la plaque de nom d'un joueur dans un cadre animé.

| Méthode | Description |
| --- | --- |
| `project:SetImageDrawer(fileName, width, height, draw)  -> nil` | Chaque objet qui affiche le fichier image `fileName` affiche ce que dessine la fonction `draw`. Le jeu appelle `draw` à chaque frame avec une zone de dessin de `width` par `height` dont le coin supérieur gauche est (0, 0). |
| `project:ClearImageDrawer(fileName)  -> nil` | Les objets affichent de nouveau le fichier image. |

Le jeu appelle `draw(rank, layer)` une fois pour chaque calque qui contient un tel objet. `rank` compte ces calques à partir de 0, en commençant par celui du dessus, et `layer` est le numéro du calque dans le projet. Dans `draw`, utilisez les appels de dessin habituels : le jeu les redirige vers l'image, qui se déplace, change d'échelle et s'estompe ensuite avec l'objet.

<div class="callout warn">
La fonction doit appartenir au script qui appelle SetImageDrawer. Les bords doux dessinés dans `draw` ressortent légèrement plus fins qu'à l'écran.
</div>

### Consulter le projet

| Méthode | Description |
| --- | --- |
| `project.Loaded  -> bool` | True quand le fichier a été lu. |
| `project.Width  -> int` | Largeur de la scène en pixels, ou -1 quand rien n'est chargé. |
| `project.Height  -> int` | Hauteur de la scène en pixels, ou -1 quand rien n'est chargé. |
| `project.FrameRate  -> number` | Frames par seconde de la scène. |
| `project.FrameCount  -> int` | Longueur de la scène en frames. |
| `project.DurationMs  -> number` | Durée de la scène en millisecondes. |
| `project.SceneCount  -> int` | Nombre de scènes dans le projet. |
| `project:GetScene()  -> int` | Index de la scène en cours de lecture. |
| `project:SetScene(index)  -> nil` | Joue une autre scène du projet, depuis son début. |
| `project:GetSceneName(index)  -> string` | Nom d'une scène. |
| `project.ObjectCount  -> int` | Nombre d'objets dans la scène. |
| `project:GetProjectValue(key)  -> string` | Un réglage du projet, tel qu'il est écrit dans le fichier. |
| `project:GetSceneValue(key)  -> string` | Un réglage de la scène en cours de lecture. |
| `project:GetValue(section, key)  -> string` | Un réglage de n'importe quelle section du fichier, d'après le nom de la section (`"12"` pour un objet, `"12.1"` pour son deuxième effet). |
| `project:GetValueAtFrame(objectId, effect, key, frame)  -> number` | Un réglage numérique de l'effet d'un objet à une frame, avec son mouvement appliqué. |

### Vérifier ce qui est dessiné

| Méthode | Description |
| --- | --- |
| `project.UnsupportedCount  -> int` | Nombre d'éléments que le projet utilise et que le jeu ne dessine pas, fichiers manquants compris. |
| `project:GetUnsupported()  -> string` | Ces éléments, un par ligne. |
| `project.ApproximatedCount  -> int` | Nombre d'éléments que le jeu dessine de façon simplifiée. |
| `project:GetApproximated()  -> string` | Ces éléments, un par ligne. |
| `project:AddScriptFolder(path)  -> nil` | Ajoute un dossier de fichiers de script AviUtl que les effets du projet peuvent nommer. Le jeu lit de lui-même un dossier `Script` placé à côté du fichier de projet. |
| `project.ScriptErrorCount  -> int` | Nombre de fichiers de script qui ont échoué. |
| `project:GetScriptErrors()  -> string` | La première erreur de chaque script en échec, une par ligne. |
| `project.GraphicsMemoryMB  -> number` | Mémoire graphique que le projet occupe actuellement pour son propre dessin, en mégaoctets. |
| `project:SetSurfaceBudgetMB(mb)  -> nil` | Définit la quantité de mémoire graphique que le projet conserve pour la réutiliser entre les frames. |

<div class="callout warn">
Les fichiers de script fournis avec un projet s'exécutent dans un environnement fermé : ils ne peuvent ni lire ni écrire de fichiers, ni lancer de programmes, ni charger de modules natifs.
</div>

```lua
local intro
local box

function onStart()
    intro = AVIUTL2:LoadProject("Intro/intro.aup2")
    box = TEXTURE:CreateTexture("Intro/player_box.png")
    -- la plaque de nom de chaque joueur, dessinée dans les cadres de joueur du projet
    intro:SetImageDrawer("player_box.png", 520, 216, function(rank)
        box:Draw(0, 0)
        NAMEPLATE:DrawPlayerNameplate(170, 88, 255, rank)
    end)
end

function activate()
    intro:SetBackground(0, 0, 0, 255)
    intro:Start()
end

function draw()
    intro:DrawFit(0, 0, 1920, 1080)
    if intro:IsFinished() then Exit("title", nil) end
end

function deactivate()
    intro:Stop()
end

function onDestroy()
    intro:Dispose()
    box:Dispose()
end
```
