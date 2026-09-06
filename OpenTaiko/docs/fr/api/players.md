<!-- api/players.md -->

# Joueurs et profils

Fichiers de sauvegarde, plaques de nom, personnages, puchicharas, état de la partie, thèmes et langue courante.

Les indices de joueur commencent à 0 (0 à 4) partout sur cette page, sauf pour THEME:GetThemeSettingForPlayer, qui commence à 1. Les modules en lecture seule (ROActivities et arrière-plans) reçoivent un handle de fichier de sauvegarde dont les méthodes d'écriture journalisent une erreur et ne font rien ; tout le reste ici se comporte de la même façon dans chaque type de module.

## Fichiers de sauvegarde

### GetSaveFile

Fonction globale renvoyant le handle de fichier de sauvegarde d'un emplacement de joueur.

<div class="callout warn">
Appelez-la comme une simple fonction (`GetSaveFile(0)`). Un indice hors limites journalise une erreur et renvoie nil. Chaque appel crée un nouveau handle qui lit des données à jour ; il n'y a donc rien à mettre en cache ni à libérer.
</div>

| Méthode | Description |
| --- | --- |
| `GetSaveFile(player)  -> saveFile` | Renvoie le handle de fichier de sauvegarde pour l'emplacement de joueur (à partir de 0), ou nil si l'indice est hors limites. |

### Handle de fichier de sauvegarde

Le profil d'un joueur : nom, pièces, objets débloqués, déclencheurs et compteurs, statistiques de réussite, personnage équipé, puchichara, plaque de nom et titre de dan.

<div class="callout warn">
Lisez les propriétés avec la syntaxe à point (sf.Name, sf.Coins). Les méthodes d'écriture persistent immédiatement. Les modules en lecture seule bloquent les suivantes : SpendCoins, EarnCoins, UnlockNameplate, UnlockSong, l'affectation de SelectedHitsounds, SetGlobalTrigger, SetGlobalCounter, ChangeCharacter (renvoie false), UnlockPuchichara, ChangePuchichara, UnlockCharacter, ChangeDan, ChangeName et ChangeNameplate.
</div>

| Méthode | Description |
| --- | --- |
| `sf.Name  -> string` | Le nom affiché du joueur. |
| `sf.SaveId  -> integer` | L'identifiant numérique de base de données de cette sauvegarde. |
| `sf.SaveUID  -> string` | L'identifiant chaîne unique de cette sauvegarde. |
| `sf.NameplateInfo  -> nameplateInfo` | La plaque de nom équipée (voir Handle d'informations de plaque de nom), ou la plaque de débutant par défaut si l'identifiant stocké est inconnu. |
| `sf.DanplateInfo  -> danplateInfo` | Le titre de dan courant (voir Handle d'informations de plaque de dan). |
| `sf.TotalPlaycount  -> integer` | Nombre total de parties sur cette sauvegarde. |
| `sf.AIBattlePlaycount  -> integer` | Nombre de parties en bataille contre l'IA. |
| `sf.AIBattleWins  -> integer` | Nombre de victoires en bataille contre l'IA. |
| `sf.Coins  -> integer` | Solde de pièces courant. |
| `sf.TotalEarnedCoins  -> integer` | Total des pièces gagnées sur toute la vie de la sauvegarde. |
| `sf:SpendCoins(price)  -> nil` | Déduit des pièces (le solde ne descend jamais sous 0) et persiste. |
| `sf:EarnCoins(amount)  -> nil` | Ajoute des pièces au solde et au total gagné, et persiste. |
| `sf:IsNameplateUnlocked(id)  -> bool` | Si la plaque de nom portant cet identifiant est débloquée. |
| `sf:UnlockNameplate(id)  -> nil` | Débloque une plaque de nom et persiste (sans effet si déjà débloquée). |
| `sf:IsSongUnlocked(uniqueId)  -> bool` | Si la chanson portant cet identifiant unique est débloquée. |
| `sf:UnlockSong(uniqueId)  -> nil` | Débloque une chanson et persiste (sans effet si déjà débloquée). |
| `sf.SelectedHitsounds  -> string` | Nom de dossier du jeu de sons de frappe sélectionné. Affecter un nom différent le persiste et recharge les sons de frappe du joueur. |
| `sf:GetGlobalTrigger(name)  -> bool` | Lit un déclencheur booléen nommé. |
| `sf:GetGlobalCounter(name)  -> number` | Lit un compteur numérique nommé. |
| `sf:SetGlobalTrigger(name, value)  -> nil` | Définit un déclencheur booléen nommé. |
| `sf:SetGlobalCounter(name, value)  -> nil` | Définit un compteur numérique nommé. |
| `sf:GetClearStatusCount(difficulty, clearStatus)  -> integer` | Nombre de partitions d'une difficulté (0 Easy à 4 Extra Extreme) dont le meilleur statut de réussite vaut exactement clearStatus (0 aucun, 1 assisté, 2 réussite, 3 full combo, 4 perfect). 0 pour des arguments hors limites. |
| `sf:GetDanBestPlay(node)  -> danBestPlay` | La meilleure partie sans mod pour un nœud de chanson dan (voir Handle de meilleure partie de dan) ; un handle avec HasRecord à false s'il n'y en a pas. |
| `sf:GetCharacter()  -> character` | Le handle de personnage lié au joueur pour cet emplacement (voir Handle de personnage). |
| `sf.CharacterName  -> string` | Nom de dossier du personnage équipé. |
| `sf:ChangeCharacter(folderName)  -> bool` | Équipe le personnage portant ce nom de dossier. Renvoie true quand le personnage est désormais équipé ou était déjà actif, false si aucun personnage chargé ne porte ce nom de dossier. |
| `sf:GetPuchichara()  -> puchichara` | Le puchichara équipé (voir Handle de puchichara), ou nil s'il ne peut pas être résolu. |
| `sf:IsPuchicharaUnlocked(folderName)  -> bool` | Si le puchichara portant ce nom de dossier est débloqué. |
| `sf:UnlockPuchichara(folderName)  -> nil` | Débloque un puchichara et persiste (sans effet si déjà débloqué). |
| `sf:ChangePuchichara(folderName)  -> nil` | Équipe le puchichara portant ce nom de dossier et persiste. La méthode ne valide pas le nom. |
| `sf:IsCharacterUnlocked(folderName)  -> bool` | Si le personnage est débloqué. Le personnage équipé compte toujours comme débloqué. |
| `sf:UnlockCharacter(folderName)  -> nil` | Débloque un personnage et persiste (sans effet si déjà débloqué). |
| `sf.DanTitleCount  -> integer` | Nombre de titres de dan disponibles, titre par défaut compris (toujours au moins 1). |
| `sf:GetDanTitleByIndex(index)  -> danTitleEntry` | Le titre de dan à un indice (à partir de 0) (voir Handle d'entrée de titre de dan). L'indice 0 est le titre par défaut ; nil si hors limites. |
| `sf.SelectedDan  -> string` | Texte du titre de dan actif. |
| `sf:ChangeDan(title)  -> nil` | Rend actif le titre donné, en copiant ses drapeaux or et statut de réussite s'il s'agit d'un titre gagné par le joueur, rafraîchit la plaque de nom et persiste. |
| `sf:ChangeName(name)  -> nil` | Change le nom affiché, rafraîchit la plaque de nom et persiste. La méthode ignore les noms vides ou inchangés. |
| `sf:ChangeNameplate(id)  -> nil` | Équipe la plaque de nom portant cet identifiant, rafraîchit la plaque de nom et persiste. Un identifiant absent de la base de données efface le texte de titre mis en cache. |

```lua
local save = GetSaveFile(0)
local entry = CHARACTERLIST:GetByName("Aoi")
if entry and not save:IsCharacterUnlocked(entry.FolderName) then
    local cond = entry.UnlockCondition
    if cond:IsUnlockable(0) and cond:GetCoinPrice() <= save.Coins then
        save:SpendCoins(cond:GetCoinPrice())
        save:UnlockCharacter(entry.FolderName)
    end
end
```

## Plaques de nom et titres de dan

### NAMEPLATE

Dessine les plaques de titre, les plaques de dan et les plaques de nom complètes des joueurs.

<div class="callout warn">
La ROActivity nameplate du skin (Modules/ROActivities/nameplate) se charge du dessin et définit les graphismes et la disposition. L'opacité va de 0 à 255. Les paramètres de texte prennent une texture rendue depuis un objet texte (voir Graphismes et texte) ; rarity est l'indice 0 Poor, 1 Common, 2 Uncommon, 3 Rare, 4 Epic, 5 Legendary, 6 Mythical.
</div>

| Méthode | Description |
| --- | --- |
| `NAMEPLATE:DrawTitlePlate(x, y, opacity, type, text, rarity, nameplateId)  -> nil` | Dessine une plaque de titre avec le type d'affichage donné, la texture de titre pré-rendue, l'indice de rareté et l'identifiant de plaque de nom. |
| `NAMEPLATE:DrawDanPlate(x, y, opacity, danGrade, text)  -> nil` | Dessine une plaque de dan pour le grade donné avec une texture de titre pré-rendue. |
| `NAMEPLATE:DrawPlayerNameplate(x, y, opacity, player)  -> nil` | Dessine la plaque de nom complète d'un emplacement de joueur ; le côté rouge ou bleu suit le réglage de côté 1P du jeu. |
| `NAMEPLATE:DrawNameplateTitleById(id, x, y, opacity, font)  -> nil` | Rend le titre localisé de la plaque de nom portant cet identifiant avec un objet texte et le dessine sous forme de plaque de titre. L'identifiant doit exister dans la base de données des plaques de nom. |

### NAMEPLATESLIST

La base de données de toutes les plaques de nom que le jeu connaît, avec recherche par indice ou identifiant et filtrage.

<div class="callout warn">
Les méthodes de requête renvoient des handles d'informations de plaque de nom. FindWhere appelle une fonction Lua une fois par plaque de nom et conserve les entrées pour lesquelles elle renvoie true.
</div>

| Méthode | Description |
| --- | --- |
| `NAMEPLATESLIST.Count  -> integer` | Nombre de plaques de nom dans la base de données. |
| `NAMEPLATESLIST:GetByIndex(index)  -> nameplateInfo` | La plaque de nom à une position de base de données (à partir de 0), ou nil si hors limites. |
| `NAMEPLATESLIST:GetById(id)  -> nameplateInfo` | La plaque de nom portant cet identifiant, ou nil si introuvable. |
| `NAMEPLATESLIST:GetAll()  -> nameplateInfo[]` | Toutes les plaques de nom sous forme de liste. |
| `NAMEPLATESLIST:FindWhere(predicate)  -> nameplateInfo[]` | Les plaques de nom pour lesquelles `predicate(info)` renvoie true. |

### Handle d'informations de plaque de nom

Un titre de plaque de nom : texte localisé, type d'affichage, identifiant, rareté et condition de déblocage.

<div class="callout warn">
sf.NameplateInfo et NAMEPLATESLIST renvoient ces handles. La plaque de débutant par défaut a l'identifiant -1, la rareté "Common" et aucune condition de déblocage.
</div>

| Méthode | Description |
| --- | --- |
| `info.Title  -> string` | Texte du titre dans la langue courante. |
| `info.Type  -> integer` | Code de type d'affichage passé à NAMEPLATE:DrawTitlePlate. |
| `info.Id  -> integer` | Identifiant de la plaque de nom (-1 pour la plaque de débutant par défaut). |
| `info.Rarity  -> string` | Nom de rareté : "Poor", "Common", "Uncommon", "Rare", "Epic", "Legendary" ou "Mythical". |
| `info.UnlockCondition  -> unlockCondition` | La condition de déblocage (voir Handle de condition de déblocage). |

### Handle d'informations de plaque de dan

Le titre de dan actif du joueur tel qu'affiché sur la plaque de nom.

<div class="callout warn">
sf.DanplateInfo renvoie ce handle. Les valeurs reflètent le fichier de sauvegarde au moment de la lecture.
</div>

| Méthode | Description |
| --- | --- |
| `info.Title  -> string` | Texte du titre de dan actif. |
| `info.Gold  -> bool` | Si le joueur a obtenu le titre actif avec une réussite or. |
| `info.ClearStatus  -> integer` | Code de statut de réussite du titre actif. |

### Handle d'entrée de titre de dan

Un titre de dan que le joueur peut sélectionner.

<div class="callout warn">
sf:GetDanTitleByIndex renvoie ces entrées. L'indice 0 est le titre par défaut (pas or, statut de réussite 0) ; les indices suivants sont les titres gagnés par le joueur.
</div>

| Méthode | Description |
| --- | --- |
| `entry.Title  -> string` | Texte du titre. |
| `entry.IsGold  -> bool` | Si le joueur a obtenu le titre avec une réussite or. |
| `entry.ClearStatus  -> integer` | Meilleur statut de réussite enregistré pour le titre. |

### Handle de meilleure partie de dan

Les meilleurs résultats d'examen d'un enregistrement de dan.

<div class="callout warn">
sf:GetDanBestPlay renvoie ce handle. Vérifiez HasRecord avant de lire les examens. GetExam renvoie un tableau .NET : indexez à partir de 0 et lisez `.Length`.
</div>

| Méthode | Description |
| --- | --- |
| `play.HasRecord  -> bool` | Si un enregistrement existe pour la chanson. |
| `play:GetExam(slot)  -> int[]` | Meilleurs scores pour l'emplacement d'examen 1 à 7 : une valeur pour un examen sur tout le parcours, une par chanson pour les examens par chanson. Vide pour un enregistrement absent ou un emplacement invalide. |

## Personnages et puchicharas

### CHARACTER

Crée des handles de personnage et expose les noms des emplacements d'animation et de voix standard.

<div class="callout warn">
CreateCharacter renvoie un handle qui possède ses ressources ; vérifiez IsValid et appelez Dispose quand vous avez terminé. GetPlayerCharacter renvoie un handle qui suit le personnage équipé du joueur et n'a pas besoin d'être libéré. GetPlayerGradientMap renvoie une carte de dégradé (voir Graphismes et texte). Les membres ANIM_* et VOICE_* sont des chaînes en lecture seule ; passez-les aux méthodes d'animation et de voix du handle de personnage.
</div>

| Méthode | Description |
| --- | --- |
| `CHARACTER:CreateCharacter(folderName)  -> character` | Charge un personnage autonome depuis Global/Characters/{folderName}. IsValid est false si le dossier n'existe pas. |
| `CHARACTER:GetPlayerCharacter(player)  -> character` | Un handle lié à un emplacement de joueur qui résout le personnage équipé à chaque appel. |
| `CHARACTER:GetPlayerGradientMap(player)  -> gradientMap` | Le dégradé de palette actif pour un emplacement de joueur, ou nil si aucun n'est défini. |
| `CHARACTER.ANIM_PREVIEW  -> string` | Pose d'aperçu (menus et boutiques). |
| `CHARACTER.ANIM_RENDER  -> string` | Pose de rendu complet. |
| `CHARACTER.ANIM_GAME_NORMAL  -> string` | Gameplay, état normal. |
| `CHARACTER.ANIM_GAME_CLEAR  -> string` | Gameplay, jauge dans la zone de réussite. |
| `CHARACTER.ANIM_GAME_MAX  -> string` | Gameplay, jauge pleine. |
| `CHARACTER.ANIM_GAME_GOGO  -> string` | Gameplay, go-go time. |
| `CHARACTER.ANIM_GAME_GOGO_MAX  -> string` | Gameplay, go-go time avec jauge pleine. |
| `CHARACTER.ANIM_GAME_MISS  -> string` | Gameplay, raté. |
| `CHARACTER.ANIM_GAME_MISS_DOWN  -> string` | Gameplay, raté avec jauge basse. |
| `CHARACTER.ANIM_GAME_10COMBO  -> string` | Gameplay, palier de 10 combos. |
| `CHARACTER.ANIM_GAME_10COMBO_MAX  -> string` | Gameplay, palier de 10 combos avec jauge pleine. |
| `CHARACTER.ANIM_GAME_CLEARED  -> string` | Gameplay, chanson réussie. |
| `CHARACTER.ANIM_GAME_FAILED  -> string` | Gameplay, chanson échouée. |
| `CHARACTER.ANIM_GAME_CLEAR_OUT  -> string` | Transition hors de l'état de réussite. |
| `CHARACTER.ANIM_GAME_CLEAR_IN  -> string` | Transition vers l'état de réussite. |
| `CHARACTER.ANIM_GAME_MAX_OUT  -> string` | Transition hors de l'état jauge pleine. |
| `CHARACTER.ANIM_GAME_MAX_IN  -> string` | Transition vers l'état jauge pleine. |
| `CHARACTER.ANIM_GAME_MISS_IN  -> string` | Transition vers un raté. |
| `CHARACTER.ANIM_GAME_MISS_DOWN_IN  -> string` | Transition vers un raté avec jauge basse. |
| `CHARACTER.ANIM_GAME_RETURN  -> string` | Retour à l'état normal. |
| `CHARACTER.ANIM_GAME_GOGOSTART  -> string` | Éclat de départ du go-go. |
| `CHARACTER.ANIM_GAME_GOGOSTART_CLEAR  -> string` | Éclat de départ du go-go dans l'état de réussite. |
| `CHARACTER.ANIM_GAME_GOGOSTART_MAX  -> string` | Éclat de départ du go-go avec jauge pleine. |
| `CHARACTER.ANIM_GAME_BALLOON_BREAKING  -> string` | Ballon en train d'être frappé. |
| `CHARACTER.ANIM_GAME_BALLOON_BROKE  -> string` | Ballon éclaté. |
| `CHARACTER.ANIM_GAME_BALLOON_MISS  -> string` | Ballon raté. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BREAKING  -> string` | Kusudama en train d'être frappé. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_BROKE  -> string` | Kusudama brisé. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_MISS  -> string` | Kusudama raté. |
| `CHARACTER.ANIM_GAME_KUSUDAMA_IDLE  -> string` | Kusudama au repos. |
| `CHARACTER.ANIM_GAME_TOWER_STANDING  -> string` | Mode Tour, debout. |
| `CHARACTER.ANIM_GAME_TOWER_STANDING_TIRED  -> string` | Mode Tour, debout et fatigué. |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING  -> string` | Mode Tour, escalade. |
| `CHARACTER.ANIM_GAME_TOWER_CLIMBING_TIRED  -> string` | Mode Tour, escalade et fatigué. |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING  -> string` | Mode Tour, course. |
| `CHARACTER.ANIM_GAME_TOWER_RUNNING_TIRED  -> string` | Mode Tour, course et fatigué. |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR  -> string` | Mode Tour, réussite. |
| `CHARACTER.ANIM_GAME_TOWER_CLEAR_TIRED  -> string` | Mode Tour, réussite et fatigué. |
| `CHARACTER.ANIM_GAME_TOWER_FAIL  -> string` | Mode Tour, échec. |
| `CHARACTER.ANIM_MENU_WAIT  -> string` | Menu, attente. |
| `CHARACTER.ANIM_MENU_START  -> string` | Menu, démarrage. |
| `CHARACTER.ANIM_MENU_NORMAL  -> string` | Menu, normal. |
| `CHARACTER.ANIM_MENU_SELECT  -> string` | Menu, sélection. |
| `CHARACTER.ANIM_ENTRY_NORMAL  -> string` | Écran d'entrée, normal. |
| `CHARACTER.ANIM_ENTRY_JUMP  -> string` | Écran d'entrée, saut. |
| `CHARACTER.ANIM_RESULT_NORMAL  -> string` | Résultats, normal. |
| `CHARACTER.ANIM_RESULT_CLEAR  -> string` | Résultats, réussite. |
| `CHARACTER.ANIM_RESULT_FAILED_IN  -> string` | Résultats, entrée dans l'état d'échec. |
| `CHARACTER.ANIM_RESULT_FAILED  -> string` | Résultats, échec. |
| `CHARACTER.VOICE_END_FAILED  -> string` | Fin de chanson, échec. |
| `CHARACTER.VOICE_END_CLEAR  -> string` | Fin de chanson, réussite. |
| `CHARACTER.VOICE_END_FULLCOMBO  -> string` | Fin de chanson, full combo. |
| `CHARACTER.VOICE_END_ALLPERFECT  -> string` | Fin de chanson, all perfect. |
| `CHARACTER.VOICE_END_AIBATTLE_WIN  -> string` | Fin de chanson, bataille contre l'IA gagnée. |
| `CHARACTER.VOICE_END_AIBATTLE_LOSE  -> string` | Fin de chanson, bataille contre l'IA perdue. |
| `CHARACTER.VOICE_MENU_SONGSELECT  -> string` | Entrée dans la sélection de chansons. |
| `CHARACTER.VOICE_MENU_SONGDECIDE  -> string` | Chanson confirmée. |
| `CHARACTER.VOICE_MENU_SONGDECIDE_AI  -> string` | Chanson confirmée en bataille contre l'IA. |
| `CHARACTER.VOICE_MENU_DIFFSELECT  -> string` | Sélection de la difficulté. |
| `CHARACTER.VOICE_MENU_DANSELECTSTART  -> string` | Entrée dans la sélection de dan. |
| `CHARACTER.VOICE_MENU_DANSELECTPROMPT  -> string` | Invite de la sélection de dan. |
| `CHARACTER.VOICE_MENU_DANSELECTCONFIRM  -> string` | Parcours dan confirmé. |
| `CHARACTER.VOICE_TITLE_SANKA  -> string` | Entrée sur l'écran titre. |
| `CHARACTER.VOICE_TOWER_MISS  -> string` | Raté en mode Tour. |
| `CHARACTER.VOICE_RESULT_BESTSCORE  -> string` | Résultats, nouveau meilleur score. |
| `CHARACTER.VOICE_RESULT_CLEARFAILED  -> string` | Résultats, échec. |
| `CHARACTER.VOICE_RESULT_CLEARSUCCESS  -> string` | Résultats, réussite. |
| `CHARACTER.VOICE_RESULT_DANFAILED  -> string` | Résultats, dan échoué. |
| `CHARACTER.VOICE_RESULT_DANREDPASS  -> string` | Résultats, dan réussi. |
| `CHARACTER.VOICE_RESULT_DANGOLDPASS  -> string` | Résultats, dan réussi avec l'or. |

### Handle de personnage

Un personnage dessinable : joue des animations et des voix nommées et porte un état de dessin propre au handle (opacité, échelle, teinte, rotation, mode de fusion et de répétition, dégradé de palette).

<div class="callout warn">
CHARACTER:GetPlayerCharacter, CHARACTER:CreateCharacter, sf:GetCharacter et la propriété Character d'une entrée de liste de personnages renvoient des handles de personnage. Seuls les handles issus de CreateCharacter possèdent leurs ressources et nécessitent Dispose. Le handle stocke les valeurs Set* et les applique à chaque dessin suivant ; les arguments d'échelle et d'opacité des méthodes de dessin se multiplient avec les valeurs stockées. L'opacité stockée va de 0.0 à 1.0, l'opacité par dessin de 0 à 255. Les noms d'animation et de voix sont les constantes CHARACTER.
</div>

| Méthode | Description |
| --- | --- |
| `char.IsValid  -> bool` | Si le handle se résout vers un personnage chargé. |
| `char.FolderName  -> string` | Nom de dossier, ou une chaîne vide si invalide. |
| `char.FullPath  -> string` | Chemin absolu du dossier, ou une chaîne vide si invalide. |
| `char.DisplayName  -> string` | Nom d'affichage localisé, retombant sur le nom de dossier. |
| `char:SetPaletteGradient(stops, blend?)  -> nil` | Applique un dégradé de palette construit à partir d'une table d'au moins deux points d'arrêt de couleur, avec un taux de mélange facultatif (1.0 par défaut). Les handles liés à un joueur stockent aussi le dégradé sur l'emplacement du joueur. Passer nil l'efface. |
| `char:ClearPaletteGradient()  -> nil` | Retire le dégradé de palette (et le dégradé de l'emplacement du joueur pour les handles liés à un joueur). |
| `char:SetOpacity(opacity)  -> nil` | Opacité stockée, de 0.0 transparent à 1.0 opaque. |
| `char:SetScale(scaleX, scaleY)  -> nil` | Échelle stockée ; un X négatif fait un miroir horizontal. |
| `char:SetColor(color)  -> nil` | Teinte stockée à partir d'une valeur de couleur. |
| `char:SetColor(r, g, b)  -> nil` | Teinte stockée à partir de trois canaux de 0.0 à 1.0. |
| `char:SetRotation(degrees)  -> nil` | Rotation stockée en degrés. |
| `char:SetBlendMode(mode)  -> nil` | Mode de fusion stocké : "normal", "add", "multi", "sub" ou "screen". |
| `char:SetWrapMode(mode)  -> nil` | Mode de répétition de texture stocké : "edge", "border", "repeat" ou "mirror". |
| `char:GetScale()  -> vector2` | Échelle stockée. |
| `char:GetColor()  -> tuple` | Teinte stockée sous forme de tuple .NET avec les champs Item1, Item2 et Item3 (rouge, vert, bleu). |
| `char:GetRotation()  -> number` | Rotation stockée en degrés. |
| `char:GetBlendMode()  -> string` | Mode de fusion stocké. |
| `char:GetWrapMode()  -> string` | Mode de répétition stocké. |
| `char:Draw(x, y, animation, scaleX?, scaleY?, opacity?)  -> nil` | Dessine l'animation en x, y. Valeurs par défaut : échelle 1, opacité 255. |
| `char:DrawAtAnchor(x, y, animation, anchor?, scaleX?, scaleY?, opacity?)  -> nil` | Dessine l'animation avec le point d'ancrage nommé ("bottom" par défaut) placé en x, y. |
| `char:DrawRect(x, y, w, h, animation, scaleX?, scaleY?, opacity?)  -> nil` | Dessine l'animation au coin supérieur gauche du rectangle. La méthode accepte w et h pour le code de mise en page, mais ils n'affectent pas le dessin. |
| `char:DrawRectAtAnchor(x, y, clipW, clipH, animation, opacity?, clipX?, clipY?)  -> nil` | Dessine l'animation avec son coin supérieur gauche en x, y, découpée à un rectangle clipW par clipH décalé de clipX, clipY. L'échelle, la teinte et la rotation viennent uniquement de l'état stocké. |
| `char:Update(animation, looping?)  -> bool` | Fait avancer l'animation (en boucle par défaut) et renvoie si elle est encore en cours. |
| `char:LoadAnimation(animation)  -> nil` | Charge les frames de l'animation. |
| `char:DisposeAnimation(animation)  -> nil` | Libère les frames de l'animation. |
| `char:AvailableAnimation(animation)  -> bool` | Si le personnage fournit l'animation. |
| `char:SetAnimationDuration(animation, duration)  -> nil` | Définit la durée de lecture de l'animation. |
| `char:SetAnimationCyclesFromBPM(animation, bpm)  -> nil` | Définit la longueur de cycle de l'animation à partir d'un BPM. |
| `char:ResetAnimationCounter(animation)  -> nil` | Redémarre l'animation depuis sa première frame. |
| `char:GetAnimationSize(animation)  -> vector2` | Taille dessinée de la frame courante de l'animation à la résolution du skin, ou (0, 0) si indisponible. |
| `char:LoadVoice(voice)  -> nil` | Charge un extrait vocal. |
| `char:DisposeVoice(voice)  -> nil` | Libère un extrait vocal. |
| `char:PlayVoice(voice)  -> nil` | Joue un extrait vocal. |
| `char:Dispose()  -> nil` | Libère les ressources du personnage (handles issus de CreateCharacter uniquement). |

```lua
local chara = CHARACTER:GetPlayerCharacter(0)
chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)

function update()
    chara:Update(CHARACTER.ANIM_MENU_NORMAL)
end

function draw()
    chara:DrawAtAnchor(960, 1000, CHARACTER.ANIM_MENU_NORMAL, "bottom")
end
```

### CHARACTERLIST

La liste de tous les personnages chargés.

<div class="callout warn">
Le skin reconstruit la liste quand il charge ses personnages et la libère au rechargement du skin ; la globale peut donc valoir nil tant qu'aucun personnage n'est chargé. Les méthodes de requête renvoient des entrées de liste de personnages.
</div>

| Méthode | Description |
| --- | --- |
| `CHARACTERLIST.Count  -> integer` | Nombre de personnages chargés. |
| `CHARACTERLIST:GetAll()  -> characterEntry[]` | Tous les personnages sous forme de liste. |
| `CHARACTERLIST:GetByIndex(index)  -> characterEntry` | L'entrée à un indice (à partir de 0), ou nil si hors limites. |
| `CHARACTERLIST:GetByName(folderName)  -> characterEntry` | L'entrée portant ce nom de dossier, ou nil si introuvable. |

### Entrée de liste de personnages

Une entrée de CHARACTERLIST : nom de dossier, nom d'affichage, rareté, un handle de personnage et la condition de déblocage.

<div class="callout warn">
La liste détient le handle partagé de la propriété Character ; ne le libérez pas. Chargez les animations dessus avant de dessiner.
</div>

| Méthode | Description |
| --- | --- |
| `entry.FolderName  -> string` | Nom de dossier ; les fichiers de sauvegarde l'utilisent comme clé. |
| `entry.DisplayName  -> string` | Nom d'affichage localisé. |
| `entry.Rarity  -> string` | Nom de rareté (voir le Handle d'informations de plaque de nom pour la liste). |
| `entry.Character  -> character` | Handle de personnage pour cette entrée. |
| `entry.UnlockCondition  -> unlockCondition` | La condition de déblocage (voir Handle de condition de déblocage). |

### PUCHICHARALIST

La liste de tous les puchicharas chargés, plus la sélection courante de chaque joueur.

<div class="callout warn">
Le skin reconstruit la liste quand il charge ses textures de puchichara et la libère au rechargement du skin ; la globale peut donc valoir nil tant qu'elles ne sont pas chargées. Les méthodes de requête renvoient des handles de puchichara.
</div>

| Méthode | Description |
| --- | --- |
| `PUCHICHARALIST.Count  -> integer` | Nombre de puchicharas chargés. |
| `PUCHICHARALIST:GetAll()  -> puchichara[]` | Tous les puchicharas sous forme de liste. |
| `PUCHICHARALIST:GetByIndex(index)  -> puchichara` | Le puchichara à un indice (à partir de 0), ou nil si hors limites. |
| `PUCHICHARALIST:GetByName(folderName)  -> puchichara` | Le puchichara portant ce nom de dossier, ou nil si introuvable. |
| `PUCHICHARALIST:GetPlayerPuchichara(player)  -> puchichara` | Le puchichara équipé par un emplacement de joueur, ou nil s'il ne peut pas être résolu. |

### Handle de puchichara

Un puchichara : ses textures, son nom et son auteur localisés, sa rareté, son nom de dossier et sa condition de déblocage.

<div class="callout warn">
PUCHICHARALIST et sf:GetPuchichara renvoient ces handles. La liste détient les textures ; ne les libérez pas. Une image manquante donne une texture vide.
</div>

| Méthode | Description |
| --- | --- |
| `puchi.tx  -> texture` | Feuille de sprites chargée depuis Chara.png. |
| `puchi.render  -> texture` | Rendu complet chargé depuis Render.png. |
| `puchi.Name  -> string` | Nom d'affichage localisé. |
| `puchi.Author  -> string` | Nom d'auteur localisé. |
| `puchi.Rarity  -> string` | Nom de rareté (voir le Handle d'informations de plaque de nom pour la liste). |
| `puchi.FolderName  -> string` | Nom de dossier ; les fichiers de sauvegarde l'utilisent comme clé. |
| `puchi.UnlockCondition  -> unlockCondition` | La condition de déblocage (voir Handle de condition de déblocage). |
| `puchi:GetUnlockMessage()  -> string` | Raccourci pour `puchi.UnlockCondition:GetConditionMessage()`. |

## État de la partie et déblocages

### PLAYSTATE

Résultats en direct de la partie courante ou la plus récente : nombres de jugements, score, combo, vérifications de réussite, et statut Tour et dan.

<div class="callout warn">
Les valeurs proviennent de l'écran de jeu ; elles sont donc significatives pendant une partie et sur les écrans qui la suivent. Les indices de joueur commencent à 0 ; les méthodes ne vérifient pas leurs limites. Les vérifications de dan évaluent toujours le joueur 0.
</div>

| Méthode | Description |
| --- | --- |
| `PLAYSTATE.LastRegisteredFloor  -> integer` | Mode Tour : le dernier étage atteint. |
| `PLAYSTATE.MaxNumberOfLives  -> integer` | Mode Tour : le nombre maximum de vies. |
| `PLAYSTATE.CurrentNumberOfLives  -> integer` | Mode Tour : le nombre de vies courant. |
| `PLAYSTATE.InvincibilityDurationSpeedDependent  -> number` | Mode Tour : la durée d'invincibilité ajustée à la vitesse de la chanson. |
| `PLAYSTATE.InvincibilityDuration  -> integer` | Mode Tour : la durée d'invincibilité de base. |
| `PLAYSTATE:WasPlayEndedNormally()  -> bool` | Si la partie précédente est allée jusqu'au bout. |
| `PLAYSTATE:WasPlayAborted()  -> bool` | Si le joueur a quitté la partie précédente prématurément. |
| `PLAYSTATE:GetGoodCount(player)  -> integer` | Nombre de jugements Good. |
| `PLAYSTATE:GetOkCount(player)  -> integer` | Nombre de jugements Ok. |
| `PLAYSTATE:GetBadCount(player)  -> integer` | Nombre de jugements Bad. |
| `PLAYSTATE:GetRollCount(player)  -> integer` | Nombre de frappes de roulement. |
| `PLAYSTATE:GetADLibCount(player)  -> integer` | Nombre de notes ADLib frappées. |
| `PLAYSTATE:GetMissedADLibCount(player)  -> integer` | Nombre de notes ADLib ratées. |
| `PLAYSTATE:GetBoomCount(player)  -> integer` | Nombre de notes mine frappées. |
| `PLAYSTATE:GetAvoidedBoomCount(player)  -> integer` | Nombre de notes mine évitées. |
| `PLAYSTATE:GetScore(player)  -> integer` | Score courant. |
| `PLAYSTATE:GetCombo(player)  -> integer` | Combo courant. |
| `PLAYSTATE:GetHighestCombo(player)  -> integer` | Combo le plus élevé atteint. |
| `PLAYSTATE:IsClear(player)  -> bool` | Si la jauge atteint la ligne de réussite. |
| `PLAYSTATE:IsAssistedClear(player)  -> bool` | Si la partie est une réussite alors qu'un mod réduisant le score est actif. |
| `PLAYSTATE:IsFullCombo(player)  -> bool` | Réussite, non assistée, sans jugement Bad et sans mine frappée. |
| `PLAYSTATE:IsPerfect(player)  -> bool` | Full combo sans jugement Ok. |
| `PLAYSTATE:IsAlive()  -> bool` | Mode Tour : s'il reste des vies. |
| `PLAYSTATE:IsPass()  -> bool` | Mode dan : si le statut d'examen n'est pas un échec. |
| `PLAYSTATE:IsRedPass()  -> bool` | Mode dan : si le statut d'examen est une réussite standard. |
| `PLAYSTATE:IsGoldPass()  -> bool` | Mode dan : si le statut d'examen est une réussite or. |
| `PLAYSTATE:IsDanClear()  -> bool` | Mode dan : réussi et non assisté. |
| `PLAYSTATE:IsDanFullCombo()  -> bool` | Mode dan : réussite de dan sans jugement Bad et sans mine frappée. |
| `PLAYSTATE:IsDanPerfect()  -> bool` | Mode dan : full combo de dan sans jugement Ok. |

### Handle de condition de déblocage

La condition de déblocage d'une plaque de nom, d'un personnage ou d'un puchichara.

<div class="callout warn">
La propriété UnlockCondition des handles d'informations de plaque de nom, des entrées de liste de personnages et des handles de puchichara renvoie ce handle. Un objet sans condition (HasCondition false) est disponible par défaut : IsUnlockable renvoie true et les messages sont vides. Le vocabulaire des conditions correspond à celui d'Unlock.json et du déblocage de partitions ; voir le guide <a href="../guides/unlockables.md">Déblocage de partitions</a>.
</div>

| Méthode | Description |
| --- | --- |
| `cond.HasCondition  -> bool` | Si l'objet a une condition de déblocage. |
| `cond:GetConditionType()  -> string` | L'identifiant de type de condition (par exemple "ch", "cs", "gt", "gc" ou "ig"), ou une chaîne vide. |
| `cond:GetCoinPrice()  -> integer` | Prix en pièces de la condition, ou 0. |
| `cond:GetConditionMessage()  -> string` | Description localisée de la condition. |
| `cond:IsUnlockable(player)  -> bool` | Si le joueur remplit actuellement la condition. |
| `cond:GetBlockedMessage(player)  -> string` | Pourquoi le joueur ne remplit pas la condition, ou une chaîne vide quand elle est remplie. |

## Thème et langue

### THEME

La résolution du skin, les réglages du thème, les chaînes localisées propres au skin et les définitions des réglages du thème.

<div class="callout warn">
Le skin déclare les réglages du thème dans ThemeSettings.json et stocke leurs valeurs dans le ThemeSettings.db3 voisin. Les accesseurs renvoient toujours les valeurs de réglage sous forme de chaînes ; un réglage absent renvoie sa valeur par défaut déclarée, ou une chaîne vide si aucune déclaration n'existe. GetThemeSettingForPlayer prend un numéro de joueur à partir de 1. Les indices de définition commencent à 0.
</div>

| Méthode | Description |
| --- | --- |
| `THEME:GetResolution()  -> vector2` | La résolution du skin. |
| `THEME:GetThemeSetting(settingId)  -> string` | Valeur d'un réglage de portée globale. |
| `THEME:GetThemeSettingForPlayer(settingId, player)  -> string` | Valeur d'un réglage de portée sauvegarde pour le joueur (à partir de 1), ou sa valeur par défaut si la sauvegarde n'a pas de valeur. |
| `THEME:GetSkinString(key)  -> string` | Chaîne localisée depuis le dossier Locales du skin : d'abord la langue courante, puis la locale par défaut du skin, puis `[LOCALE NOT FOUND: key]`. |
| `THEME:GetDefinitionCount()  -> integer` | Nombre de définitions de réglage dans ThemeSettings.json. |
| `THEME:GetDefinitionId(index)  -> string` | Identifiant de la définition à un indice (à partir de 0), ou une chaîne vide. |
| `THEME:GetDefinitionScope(index)  -> string` | Portée de la définition : "global" ou "save". |
| `THEME:GetDefinitionType(index)  -> string` | Type de la définition : "bool", "int", "double", "string" ou "enum". |

### LANG

Chaînes localisées du jeu, changement de langue et valeurs de texte multilingues.

<div class="callout warn">
GetString formate l'entrée avec les arguments supplémentaires. GetLanguageIds et GetLanguageNames renvoient des tableaux .NET (à partir de 0, `.Length`) ; GetAvailableLanguages renvoie un dictionnaire à énumérer avec `:GetEnumerator()` (voir Données et persistance). FromDict prend un objet JSON analysé par JSONLOADER (il n'accepte pas de table Lua) ; AsLocalizationData prend un JsonNode issu de JSONLOADER:LoadJson.
</div>

| Méthode | Description |
| --- | --- |
| `LANG:GetString(key, ...)  -> string` | La chaîne localisée pour une clé, avec les substituts de format remplis à partir des arguments supplémentaires. |
| `LANG:ChangeLanguage(id)  -> bool` | Bascule la langue active si l'identifiant existe et diffère de la langue courante, puis appelle `reloadLanguage` sur chaque script chargé ; renvoie si la bascule a eu lieu. Elle laisse CONFIG.Language inchangé. |
| `LANG:GetLanguageIds()  -> string[]` | Identifiants des langues disponibles. |
| `LANG:GetLanguageNames()  -> string[]` | Noms d'affichage des langues disponibles, dans le même ordre. |
| `LANG:GetAvailableLanguages()  -> dict` | Identifiant de langue vers nom d'affichage. |
| `LANG:GetExamName(type)  -> string` | Nom localisé d'un type d'examen de dan. |
| `LANG:AsLocalizationData(node)  -> localizationData` | Construit une valeur de localisation à partir d'un JsonNode de la forme `{ "strings": { "<lang>": "text" } }`. |
| `LANG:FromDict(dict)  -> localizationData` | Construit une valeur de localisation à partir d'un objet JSON analysé associant des identifiants de langue à du texte. |
| `LANG:FromString(json)  -> localizationData` | Construit une valeur de localisation à partir d'une chaîne d'objet JSON associant des identifiants de langue à du texte ; une valeur vide si la chaîne ne s'analyse pas. |

```lua
local langs = LANG:GetAvailableLanguages()
local e = langs:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end

local name = LANG:FromString('{"ja":"太鼓","default":"Taiko"}'):GetString("")
```

### Handle de données de localisation

Un ensemble de chaînes indexées par identifiant de langue qui se résout vers la langue courante.

<div class="callout warn">
LANG:AsLocalizationData, LANG:FromDict et LANG:FromString renvoient ce handle. Ordre de résolution : l'identifiant de la langue courante, puis la clé "default", puis la valeur de repli passée à GetString.
</div>

| Méthode | Description |
| --- | --- |
| `loc:GetString(fallback)  -> string` | Le texte pour la langue courante, ou "default", ou la valeur de repli. |
| `loc:SetString(langId, text)  -> nil` | Définit le texte pour un identifiant de langue. |
| `loc:GetAllStrings()  -> string[]` | Tous les textes stockés, sans ordre particulier. |
