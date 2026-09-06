<!-- api/songs.md -->

# Chansons et partitions

Demander la liste des chansons, parcourir les nœuds et les partitions, lire les scores et construire des parcours dan (examens).

Conventions utilisées sur cette page :

- Les indices de difficulté commencent à 0 : 0 Easy, 1 Normal, 2 Hard, 3 Extreme (`Oni`), 4 Extra Extreme (`Edit`), 5 Tower, 6 Dan.
- Les indices de joueur et de fichier de sauvegarde commencent à 0 (0 est le joueur 1).
- Les membres écrits avec un point (`node.Title`) sont des propriétés ; les membres écrits avec un deux-points (`node:GetChart(3)`) sont des méthodes.
- Certains membres renvoient des collections C#. Une liste a `.Count` et est indexée à partir de 0 (`list[0]`) ; un tableau a `.Length` et est aussi indexé à partir de 0. Chaque entrée ci-dessous précise lequel elle renvoie.
- La liste des chansons n'est complète qu'après la fin de l'énumération des chansons. Demandez-la depuis la fonction de rappel `afterSongEnum()` (voir [Modules et cycle de vie](activities.md)), ou vérifiez d'abord la globale `IsSongsEnumDone()` ; elle renvoie true une fois l'énumération terminée.

## Demander la liste des chansons

### RequestSongList

Fonction globale qui construit une liste de chansons navigable à partir d'un objet de réglages.

<div class="callout warn">
Disponible comme une simple fonction globale. Passez-lui un objet de réglages créé par GenerateSongListSettings(). L'appel construit l'arbre des chansons une seule fois, à partir des chansons que le jeu a énumérées ; le handle conserve l'objet de réglages par référence, si bien que vous pouvez modifier un champ et appeler ReloadSongList() sur le handle pour reconstruire.
</div>

| Méthode | Description |
| --- | --- |
| `RequestSongList(settings)  -> song list handle` | Construit et renvoie un handle de liste de chansons à partir des réglages de liste donnés. |

```lua
local settings = GenerateSongListSettings()
settings.AppendMainRandomBox = false
settings:SetExcludedGenreFolders({ "Dan", "Tower" })

local list = RequestSongList(settings)
local node = list:GetSelectedSongNode()
```

### GenerateSongListSettings

Fonction globale qui crée un objet de réglages de liste de chansons avec des valeurs par défaut.

| Méthode | Description |
| --- | --- |
| `GenerateSongListSettings()  -> song list settings` | Renvoie un nouvel objet de réglages de liste de chansons avec les valeurs de champ par défaut. |

### Réglages de liste de chansons

Objet de configuration contrôlant les nœuds qu'une liste de chansons inclut et le comportement de la navigation.

<div class="callout warn">
Tous les membres ci-dessous sont des champs publics que Lua lit et écrit directement (settings.HideEmptyFolders = false), sauf les deux méthodes de mutation, qui prennent une table Lua. ExcludedGenreFolders et MandatoryDifficultyList sont des tableaux C# ; définissez-les via leurs méthodes de mutation.
</div>

| Méthode | Description |
| --- | --- |
| `settings.AppendMainRandomBox  (bool, default true)` | Quand true, la liste ajoute une boîte aléatoire à sa racine. |
| `settings.AppendSubRandomBoxes  (bool, default true)` | Quand true, la liste ajoute une boîte aléatoire à la fin de chaque dossier. |
| `settings.SubBackBoxFrequency  (int, default 7)` | Dans chaque dossier, la liste insère une boîte de retour au début et toutes les N entrées ; 0 désactive les boîtes de retour générées. |
| `settings.ExcludedGenreFolders  (string array)` | Noms de dossiers de genre que la liste laisse de côté. Définissez-le avec SetExcludedGenreFolders. |
| `settings.RootGenreFolder  (string, default nil)` | Quand défini, la racine de la liste devient le premier dossier (en profondeur d'abord) dont le genre correspond à ce nom ; quand nil, la racine est le niveau supérieur. |
| `settings.RootGenreFolderNode  (song node, default nil)` | Forme nœud de RootGenreFolder. Quand défini, il prend le pas sur la chaîne, ce qui lève l'ambiguïté entre dossiers qui partagent un nom de genre. |
| `settings.MandatoryDifficultyList  (Difficulty array, default nil)` | Difficultés qu'une chanson doit avoir pour apparaître dans la liste ; nil signifie aucune exigence. Définissez-le avec SetMandatoryDifficultyList. |
| `settings.MandatoryDifficultyMatchAll  (bool, default true)` | true exige chaque difficulté listée (ET) ; false en exige au moins une (OU). |
| `settings.HideEmptyFolders  (bool, default true)` | Masque les dossiers qui ne contiennent aucune chanson visible, récursivement. |
| `settings.FlattenOpenedFolders  (bool, default true)` | Quand true, la page courante est l'arbre entier avec les dossiers ouverts déployés sur place (les dossiers fermés comptent comme une seule entrée). Quand false, la page ne contient que les frères et sœurs du nœud du curseur. |
| `settings.ModuloPagination  (bool, default true)` | Quand true, GetSongNodeAtOffset boucle sur la page ; quand false, elle renvoie nil au-delà de chaque extrémité. |
| `settings.ModuloMovement  (bool, default true)` | Quand true, Move boucle sur la page ; quand false, elle se bloque à chaque extrémité. |
| `settings.ExcludeHiddenSongs  (bool, default true)` | Exclut les chansons dont HiddenIndex vaut 3 (masquée). |
| `settings.ExcludeLockedSongs  (bool, default false)` | Quand true, les pages laissent de côté les chansons verrouillées, si bien que la navigation ne s'y arrête jamais. |
| `settings.IgnoreUnlockables  (bool, default false)` | Quand true, la liste ignore ExcludeLockedSongs et GetRandomNodeInFolder peut renvoyer des chansons verrouillées. Les propriétés de nœud telles qu'IsLocked continuent de rapporter l'état réel. |
| `settings:SetExcludedGenreFolders(table)  -> void` | Définit ExcludedGenreFolders à partir d'une table Lua de chaînes de noms de genre. |
| `settings:SetMandatoryDifficultyList(table)  -> void` | Définit MandatoryDifficultyList à partir d'une table Lua d'indices de difficulté. |

### Handle de liste de chansons

Un arbre de chansons navigable renvoyé par RequestSongList, avec un curseur, une navigation par dossiers et une recherche.

<div class="callout warn">
Les méthodes de recherche prennent une fonction Lua qui reçoit un nœud de chanson et renvoie un booléen. Les méthodes qui renvoient plusieurs nœuds renvoient une liste C# (.Count, indexation à partir de 0).
</div>

| Méthode | Description |
| --- | --- |
| `list:ReloadSongList()  -> void` | Reconstruit tout l'arbre à partir des chansons et réglages courants et place le curseur sur le premier nœud. |
| `list:GetRoot()  -> song node` | Renvoie le nœud racine de l'arbre. |
| `list:GetSelectedSongNode()  -> song node` | Renvoie le nœud sous le curseur, ou nil quand la liste est vide. |
| `list:GetSongNodeAtOffset(offset)  -> song node` | Renvoie le nœud au décalage donné par rapport au curseur dans la page courante, en bouclant ou en renvoyant nil selon ModuloPagination. |
| `list:Move(offset)  -> void` | Déplace le curseur du décalage donné dans la page courante, en bouclant ou en se bloquant selon ModuloMovement. |
| `list:OpenFolder()  -> bool` | Ouvre le dossier sous le curseur et place le curseur sur son premier enfant ; renvoie false si le curseur n'est pas sur un dossier fermé et non vide. |
| `list:CloseFolder()  -> bool` | Ferme le dossier contenant le curseur et place le curseur sur ce dossier ; renvoie false s'il n'y a rien à fermer. Quitter un dossier virtuel restaure le curseur sauvegardé par OpenVirtualFolder. |
| `list:OpenVirtualFolder(baseFolder, songs, title)  -> bool` | Ouvre un dossier temporaire nommé `title` contenant les nœuds de chanson de la table Lua `songs` (clés 1..n), avec des boîtes de retour générées et une boîte aléatoire finale, et place le curseur dedans. `baseFolder` devient le parent du dossier virtuel. Renvoie false si la table ne contient aucun nœud de chanson. |
| `list:GetSongByUniqueId(id)  -> song node` | Renvoie la première chanson dont l'identifiant unique correspond, ou nil. |
| `list:GetRandomNodeInFolder(node, recursive, predicate)  -> song node` | Choisit une chanson au hasard parmi les frères et sœurs de `node` (la page qui le contient). Avec `recursive` (true par défaut), le tirage couvre aussi les chansons des dossiers frères. Il ignore les chansons verrouillées sauf si IgnoreUnlockables est défini. `predicate` est facultatif. Renvoie nil quand rien ne convient. |
| `list:SearchSongsByPredicate(predicate)  -> list of song nodes` | Renvoie chaque nœud de chanson de l'arbre pour lequel le prédicat renvoie true. |
| `list:SearchFirstSongByPredicate(predicate)  -> song node` | Renvoie le premier nœud de chanson pour lequel le prédicat renvoie true, ou nil. |
| `list:SearchNodesByPredicate(predicate)  -> list of song nodes` | Comme SearchSongsByPredicate, mais teste aussi les dossiers et autres nœuds non-chanson. |

```lua
local results = list:SearchSongsByPredicate(function(node)
    return node:GetChart(3) ~= nil   -- possède une partition Extreme
end)
for i = 0, results.Count - 1 do
    local node = results[i]
end
```

## Nœuds, partitions et scores

### Nœud de chanson

Une entrée unique de la liste des chansons : une chanson, un dossier, une boîte de retour ou une boîte aléatoire.

<div class="callout warn">
Le handle de liste de chansons, SONGMOUNT:ChosenSongNode() et DANBUILDER:GetSong() renvoient des nœuds de chanson. Les propriétés sont en lecture seule. Les propriétés de métadonnées renvoient nil sur les nœuds qui ne sont pas des chansons. Naviguez dans la liste via le handle de liste de chansons (Move, OpenFolder, CloseFolder et les méthodes de recherche).
</div>

| Méthode | Description |
| --- | --- |
| `node.NotNull  (bool)` | Vrai quand le nœud enveloppe une vraie entrée de la liste des chansons. |
| `node.IsFolder  (bool)` | Vrai quand le nœud est un dossier. |
| `node.IsRandom  (bool)` | Vrai quand le nœud est une boîte aléatoire. |
| `node.IsReturn  (bool)` | Vrai quand le nœud est une boîte de retour. |
| `node.IsSong  (bool)` | Vrai quand le nœud est une chanson jouable. |
| `node.SongCount  (int)` | Nombre de chansons enfants directes. |
| `node.RecursiveSongCount  (int)` | Nombre de chansons sous ce nœud, sous-dossiers compris. |
| `node.VisibleSongCount  (int)` | Nombre de chansons enfants directes dont HiddenIndex n'est pas 3. |
| `node.RecursiveVisibleSongCount  (int)` | Nombre de chansons visibles sous ce nœud, sous-dossiers compris. |
| `node.BoxType  (string)` | La chaîne de style de boîte du dossier, ou nil. |
| `node.BgType  (string)` | La chaîne de style d'arrière-plan, ou nil. |
| `node.BoxChara  (string)` | La chaîne de personnage de boîte, ou nil. |
| `node.ForeColor  (color)` | La couleur de premier plan du nœud, ou nil. |
| `node.BackColor  (color)` | La couleur d'arrière-plan du nœud, ou nil. |
| `node.BoxColor  (color)` | La couleur de boîte du nœud, ou nil. |
| `node.Title  (string)` | Le titre affiché. Les boîtes de retour et aléatoires renvoient le texte localisé "Return" / "Random" construit à partir du titre du dossier parent. |
| `node.Subtitle  (string)` | Le sous-titre de la chanson, ou nil. |
| `node.Genre  (string)` | La chaîne de genre, ou nil. |
| `node.UniqueId  (string)` | L'identifiant unique de la chanson, ou nil. |
| `node.Maker  (string)` | Le champ MAKER en une seule chaîne, ou nil. |
| `node.Charters  (string array)` | Le champ MAKER scindé aux virgules. |
| `node.Side  (int)` | La valeur SIDE : 0 normal, 1 ex, 2 les deux. |
| `node.Explicit  (bool)` | Vrai quand la chanson est marquée explicite ; nil pour les non-chansons. |
| `node.HasVideo  (bool)` | Vrai quand la chanson a une vidéo d'arrière-plan ; nil pour les non-chansons. |
| `node.DemoStart  (int)` | Le décalage de la musique d'aperçu en millisecondes. |
| `node.AudioPath  (string)` | Le chemin absolu du fichier de musique de la chanson, ou une chaîne vide. |
| `node.HasPreimage  (bool)` | Vrai quand la chanson déclare une image d'aperçu (preimage). |
| `node.PreimagePath  (string)` | Le chemin absolu de l'image d'aperçu. Vérifiez d'abord HasPreimage ; sans image d'aperçu, ce n'est que le dossier de la chanson. |
| `node:GetPreimage()  -> texture` | Charge l'image d'aperçu depuis le disque et renvoie une nouvelle texture, ou nil si la chanson n'en a pas. Libérez la texture quand vous en avez terminé. |
| `node.ChartMd5  (string)` | MD5 du fichier de partition (hexadécimal en majuscules), ou une chaîne vide. Il reste le même entre installations ; UniqueId non. |
| `node:GetChart(diff)  -> chart` | Renvoie la partition pour l'indice de difficulté donné, ou nil si la chanson n'a pas cette partition. |
| `node:GetCustomCommand(key)  -> string` | Renvoie la valeur d'une commande personnalisée de portée globale (un en-tête préfixé d'un point placé avant le premier COURSE ; la clé inclut le point, par exemple ".VAULT_NAME"), ou nil. |
| `node:GetCustomCommands()  -> dictionary` | Renvoie toutes les commandes personnalisées de portée globale sous forme d'objet dictionnaire C#. Préférez GetCustomCommand pour les recherches. |
| `node.UnlockCondition  (unlock condition)` | L'objet de condition de déblocage (voir Condition de déblocage). |
| `node.UnlockText  (string)` | Le texte de déblocage personnalisé si la chanson en définit un, sinon le message de condition généré. |
| `node.IsLocked  (bool)` | Vrai quand cette chanson est actuellement verrouillée ; toujours false pour les non-chansons. |
| `node.HiddenIndex  (int)` | L'état d'affichage du système de déblocage : 0 affichée, 1 grisée, 2 floutée, 3 masquée (0 pour les non-chansons). |
| `node.Rarity  (string)` | Le libellé de rareté ; "Common" pour les chansons sans entrée de déblocage, "-" pour les non-chansons. |
| `node:Mount(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | Sélectionne cette chanson pour la partie avec l'indice de difficulté donné par joueur (0 par défaut). Elle ne vérifie que les CONFIG.PlayerCount premiers indices et renvoie false si le nœud n'est pas une chanson ou si la difficulté d'un joueur actif est absente ou hors limites. |
| `node:MountIfNotLocked(p1diff, p2diff, p3diff, p4diff, p5diff)  -> bool` | Identique à Mount, mais renvoie false sans monter quand la chanson est verrouillée. |

### Partition

Une difficulté d'une chanson : niveau, BPM, créateurs, données Tour et Dan, meilleurs scores et commandes personnalisées.

<div class="callout warn">
GetChart(diff) d'un nœud de chanson renvoie une partition. Les propriétés sont en lecture seule. BPM, Life, TotalFloorCount, TowerType et DanTick renvoient nil quand la partition n'a pas d'informations de partition. Difficulty et LevelIcon sont des objets enum ; comparez-les via DifficultyAsInt, IsPlus et IsMinus.
</div>

| Méthode | Description |
| --- | --- |
| `chart.NotNull  (bool)` | Vrai quand la partition enveloppe de vraies données de partition. |
| `chart.Parent  (song node)` | Le nœud de chanson auquel cette partition appartient. |
| `chart.Difficulty  (enum)` | La difficulté sous forme d'objet enum. |
| `chart.DifficultyAsInt  (int)` | L'indice de difficulté. |
| `chart.Level  (int)` | Le niveau en étoiles. |
| `chart.LevelDecimal  (number)` | Le niveau avec sa partie fractionnaire (par exemple 12.888), ou le niveau entier quand aucune n'a été renseignée. |
| `chart.LevelFirstDecimal  (int)` | Le premier chiffre décimal de LevelDecimal (0-9). |
| `chart.LevelIcon  (enum)` | L'icône de niveau sous forme d'objet enum. |
| `chart.IsPlus  (bool)` | Vrai quand l'icône de niveau est "plus". |
| `chart.IsMinus  (bool)` | Vrai quand l'icône de niveau est "minus". |
| `chart.NotesDesigner  (string)` | Le champ NOTESDESIGNER en une seule chaîne. |
| `chart.Charters  (string array)` | Le champ NOTESDESIGNER scindé aux virgules. |
| `chart.BPM  (number)` | Le BPM principal, ou nil. |
| `chart.BaseBPM  (number)` | Le BPM de base, ou nil. |
| `chart.MinBPM  (number)` | Le BPM minimum, ou nil. |
| `chart.MaxBPM  (number)` | Le BPM maximum, ou nil. |
| `chart.Life  (int)` | Nombre de vies en mode Tour, ou nil. |
| `chart.TotalFloorCount  (int)` | Nombre d'étages de la Tour, ou nil. |
| `chart.TowerType  (string)` | Chaîne de type de Tour, ou nil. |
| `chart.DanTick  (int)` | Valeur de graduation de la plaque de dan, ou nil. |
| `chart.DanTickColor  (color)` | Couleur de graduation de la plaque de dan (blanc quand la partition n'a pas d'informations de partition). |
| `chart.DanSongs  (array of dan songs)` | Les chansons qui composent cette partition Dan. |
| `chart.DanExams  (array of dan exams)` | Les conditions d'examen globales de cette partition Dan. |
| `chart:GetSongExam(songIdx, examSlot)  -> dan exam` | Renvoie l'examen par chanson pour l'indice de chanson (à partir de 1) et l'emplacement d'examen (à partir de 1) ; le résultat a IsSet = false quand il n'en existe aucun. |
| `chart:GetPlayerBestScore(save)  -> best score info` | Renvoie le résumé de la meilleure partie du fichier de sauvegarde donné pour cette partition. |
| `chart:GetCustomCommand(key)  -> string` | Renvoie la valeur d'une commande personnalisée de portée partition (un en-tête préfixé d'un point à l'intérieur de ce bloc COURSE ; la clé inclut le point), ou nil. |
| `chart:GetCustomCommands()  -> dictionary` | Renvoie toutes les commandes personnalisées de portée partition sous forme d'objet dictionnaire C#. |
| `chart.SongFolder  (string)` | Le dossier absolu contenant le fichier de partition. |
| `chart.ChartPath  (string)` | Le chemin absolu du fichier de partition. |
| `chart.UniqueId  (string)` | L'identifiant unique de la chanson, ou une chaîne vide. |
| `chart:Select(player)  -> bool` | Marque cette partition comme la difficulté choisie pour le joueur donné ; le joueur 0 définit aussi la chanson choisie. Renvoie false quand la partition n'est pas valide. |

### Informations de meilleur score

Le résumé de la meilleure partie d'un fichier de sauvegarde pour une partition.

<div class="callout warn">
chart:GetPlayerBestScore(save) renvoie cet objet. Tous les membres sont en lecture seule. Un indice de sauvegarde invalide donne un enregistrement vide.
</div>

| Méthode | Description |
| --- | --- |
| `info.ScoreRank  (int)` | Le meilleur rang de score atteint. |
| `info.ClearStatus  (int)` | Le meilleur statut de réussite atteint. |
| `info.HighScore  (int)` | Le meilleur score. |
| `info.HasBeenPlayed  (bool)` | Vrai quand la partition a au moins une partie enregistrée, quel que soit le résultat. |
| `info.PlayCount  (int)` | Nombre total de parties sur cette partition, toutes variantes de mods confondues. |

## Examens dan

### Chanson de dan

Une entrée de chanson dans un parcours Dan.

<div class="callout warn">
Éléments de chart.DanSongs. Tous les membres sont en lecture seule.
</div>

| Méthode | Description |
| --- | --- |
| `dansong.Title  (string)` | Le titre de la chanson. |
| `dansong.SubTitle  (string)` | Le sous-titre de la chanson. |
| `dansong.Genre  (string)` | Le genre de la chanson. |
| `dansong.Level  (int)` | Le niveau en étoiles de la chanson. |
| `dansong.Difficulty  (enum)` | La difficulté sous forme d'objet enum. |
| `dansong.DifficultyAsInt  (int)` | L'indice de difficulté. |

### Examen de dan

Une condition de réussite/échec d'un parcours Dan.

<div class="callout warn">
Éléments de chart.DanExams, ou renvoyés par chart:GetSongExam(). Tous les membres sont en lecture seule. Valeurs de TypeAsInt : 0 jauge, 1 jugements parfaits, 2 jugements bons, 3 jugements mauvais, 4 score, 5 roulements, 6 frappes, 7 combo, 8 précision, 9 jugements ad-lib, 10 jugements de mines. Valeurs de RangeAsInt : 0 "au moins", 1 "moins de".
</div>

| Méthode | Description |
| --- | --- |
| `danexam.IsSet  (bool)` | Vrai quand cet emplacement d'examen est activé. |
| `danexam.RedValue  (int)` | Le seuil rouge (réussite). |
| `danexam.GoldValue  (int)` | Le seuil or. |
| `danexam.TypeAsInt  (int)` | Le type d'examen. |
| `danexam.RangeAsInt  (int)` | Le sens de comparaison. |

### DANBUILDER

Globale permettant d'assembler en mémoire un parcours Dan à partir de nœuds de chanson, de difficultés et de conditions d'examen, puis de le monter pour la partie.

<div class="callout warn">
Disponible comme la globale DANBUILDER. Les indices de chanson et d'emplacement commencent à 1, sauf la difficulté passée à AddSong, qui est un indice de difficulté à partir de 0. Les emplacements d'examen vont de 1 à 7. Chaînes de type d'examen (insensibles à la casse, forme courte entre parenthèses) : "judgeperfect" (jp), "judgegood" (jg), "judgebad" (jb), "score" (s), "roll" (r), "hit" (h), "combo" (c), "accuracy" (a), "judgeadlib" (ja), "judgemine" (jm) ; toute autre chaîne signifie jauge. lessThan = true fait de l'examen une vérification "moins de", false une vérification "au moins". Le constructeur conserve son état entre les appels ; appelez Clear() avant de construire un nouveau parcours.
</div>

| Méthode | Description |
| --- | --- |
| `DANBUILDER.SongCount  (int)` | Nombre de chansons ajoutées jusqu'ici. |
| `DANBUILDER:AddSong(node, diff)  -> void` | Ajoute un nœud de chanson à l'indice de difficulté donné (à partir de 0). |
| `DANBUILDER:GetSong(i)  -> song node` | Renvoie le nœud de chanson à l'indice i (à partir de 1), ou nil. |
| `DANBUILDER:GetSongDiff(i)  -> int` | Renvoie l'indice de difficulté stocké pour la chanson à l'indice i (à partir de 1), ou -1. |
| `DANBUILDER:SetTitle(title)  -> void` | Définit le titre du parcours ("Dynamic Dan" par défaut). |
| `DANBUILDER:SetSubtitle(subtitle)  -> void` | Définit le sous-titre du parcours. |
| `DANBUILDER:SetDanTick(tick)  -> void` | Définit la valeur de graduation de la plaque de dan (2 par défaut). |
| `DANBUILDER:SetDanTickColor(r, g, b)  -> void` | Définit la couleur de graduation de la plaque de dan à partir de composantes 0-255 (blanc par défaut). |
| `DANBUILDER:SetGlobalExam(slot, type, red, gold, lessThan)  -> void` | Définit un examen à l'échelle du parcours dans l'emplacement donné. |
| `DANBUILDER:SetPerSongExam(songIndex, slot, type, red, gold, lessThan)  -> void` | Définit un examen qui s'applique à une seule chanson, par indice de chanson (à partir de 1) et emplacement. |
| `DANBUILDER:Clear()  -> void` | Retire toutes les chansons et tous les examens et remet les métadonnées à leurs valeurs par défaut. |
| `DANBUILDER:Mount()  -> bool` | Construit la partition du parcours en mémoire et la sélectionne pour la partie à la difficulté Dan pour le joueur 1 ; renvoie false si le constructeur ne contient aucune chanson ou si la construction a échoué. |

```lua
DANBUILDER:Clear()
DANBUILDER:SetTitle("Custom course")
DANBUILDER:AddSong(list:GetSongByUniqueId(id1), 3)
DANBUILDER:AddSong(list:GetSongByUniqueId(id2), 3)
DANBUILDER:SetGlobalExam(1, "gauge", 90, 100, false)
DANBUILDER:SetPerSongExam(2, 2, "judgebad", 10, 5, true)
if DANBUILDER:Mount() then
    return Exit("play")
end
```

## Déblocages, emplacements virtuels et icônes de mods

### Condition de déblocage

Décrit ce qui débloque une chanson et si un joueur remplit actuellement la condition.

<div class="callout warn">
node.UnlockCondition renvoie cet objet. HasCondition est une propriété ; le reste sont des méthodes. Les chansons sans entrée de déblocage rapportent HasCondition = false et IsUnlockable = true.
</div>

| Méthode | Description |
| --- | --- |
| `cond.HasCondition  (bool)` | Vrai quand la chanson a une condition de déblocage explicite. |
| `cond:GetConditionMessage()  -> string` | Renvoie la description lisible de la condition, ou une chaîne vide. |
| `cond:GetConditionType()  -> string` | Renvoie l'identifiant de type de condition (par exemple "ch", "cs", "gt", "gc", "ig"), ou une chaîne vide. |
| `cond:GetCoinPrice()  -> int` | Renvoie le coût en pièces, ou 0. |
| `cond:IsUnlockable(player)  -> bool` | Renvoie true quand le joueur donné remplit la condition. |
| `cond:GetBlockedMessage(player)  -> string` | Renvoie la raison pour laquelle le joueur ne remplit pas la condition, ou une chaîne vide quand il la remplit. |

### VIRTUALSLOTS

Globale permettant de lire et d'écrire les cinq emplacements de personnage virtuels (V1-V5) et de rediriger un spot de joueur pour afficher les visuels d'un emplacement.

<div class="callout warn">
Disponible comme la globale VIRTUALSLOTS. Les indices d'emplacement vont de 1 à 5 ; les mutateurs ignorent les indices hors limites et les accesseurs renvoient les valeurs par défaut pour ceux-ci. Ces méthodes n'écrivent rien sur le disque. Le moteur gère l'emplacement IA, que cette globale ne peut pas modifier.
</div>

| Méthode | Description |
| --- | --- |
| `VIRTUALSLOTS:GetCharacter(slot)  -> string` | Renvoie le nom de dossier du personnage de l'emplacement, ou "None". |
| `VIRTUALSLOTS:SetCharacter(slot, folderName)  -> void` | Définit le nom de dossier du personnage de l'emplacement. |
| `VIRTUALSLOTS:GetPuchichara(slot)  -> string` | Renvoie le nom de dossier du puchichara de l'emplacement, ou "None". |
| `VIRTUALSLOTS:SetPuchichara(slot, folderName)  -> void` | Définit le nom de dossier du puchichara de l'emplacement. |
| `VIRTUALSLOTS:GetNameplateName(slot)  -> string` | Renvoie le nom de joueur de la plaque de nom de l'emplacement, ou "VSlot". |
| `VIRTUALSLOTS:SetNameplateName(slot, name)  -> void` | Définit le nom de joueur de la plaque de nom de l'emplacement. |
| `VIRTUALSLOTS:GetNameplateTitle(slot)  -> string` | Renvoie le texte de titre de la plaque de nom de l'emplacement. |
| `VIRTUALSLOTS:SetNameplateTitle(slot, title)  -> void` | Définit le texte de titre de la plaque de nom de l'emplacement. |
| `VIRTUALSLOTS:GetNameplateDan(slot)  -> string` | Renvoie le texte de dan de la plaque de nom de l'emplacement. |
| `VIRTUALSLOTS:SetNameplateDan(slot, dan)  -> void` | Définit le texte de dan de la plaque de nom de l'emplacement. |
| `VIRTUALSLOTS:SetNameplateById(slot, nameplateId)  -> void` | Applique une plaque de nom depuis la base de données des plaques par identifiant : définit le texte de titre, le type et la rareté. Un identifiant inconnu n'enregistre que l'identifiant. |
| `VIRTUALSLOTS:SetNameplateType(slot, type)  -> void` | Définit directement le type de titre de la plaque de nom (indice de style). |
| `VIRTUALSLOTS:SetNameplateRarity(slot, rarity)  -> void` | Définit directement l'indice de rareté du titre de la plaque de nom. |
| `VIRTUALSLOTS:SetNameplateDanType(slot, danType)  -> void` | Définit le type de la plaque de dan. |
| `VIRTUALSLOTS:SetNameplateDanGold(slot, gold)  -> void` | Définit si la plaque de dan apparaît en or. |
| `VIRTUALSLOTS:MountSlot(playerSpot, slotInfo)  -> void` | Fait afficher au spot de joueur 1-5 les visuels de `slotInfo` : "1P"-"5P" (le fichier de sauvegarde d'un joueur), "AI", ou "V1"-"V5". La substitution dure jusqu'au prochain appel de MountSlot pour ce spot. |

### MODICONS

Globale permettant de dessiner les icônes des mods actifs d'un joueur à une position de l'écran.

<div class="callout warn">
Disponible comme la globale MODICONS. La ROActivity modicons se charge du dessin ; le premier appel de Draw l'active.
</div>

| Méthode | Description |
| --- | --- |
| `MODICONS:Draw(player, x, y, alpha)  -> void` | Dessine les icônes de mods du joueur donné en (x, y) avec la disposition menu ; alpha est facultatif (255 par défaut). |

## Replays et chanson sélectionnée

### REPLAY

Globale permettant de lister les replays sauvegardés d'une partition et d'en lancer la lecture.

<div class="callout warn">
Disponible comme la globale REPLAY. ListReplays renvoie un tableau C# d'en-têtes de replay (.Length, indexation à partir de 0). Watch charge un replay et arme sa lecture pour la prochaine partie uniquement ; le jeu applique les mods du replay en mémoire et restaure les mods précédents ensuite. songFolder et chartPath proviennent des propriétés SongFolder et ChartPath d'une partition.
</div>

| Méthode | Description |
| --- | --- |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN, chartPath)  -> array of replay headers` | Renvoie jusqu'à topN replays de la partition et de la difficulté, classés par score. chartPath permet au listage de calculer ChecksumMismatch. |
| `REPLAY:ListReplays(songFolder, uniqueId, difficulty, topN)  -> array of replay headers` | Idem sans chemin de partition (le listage saute ChecksumMismatch). |
| `REPLAY:ListReplaysAsync(songFolder, uniqueId, difficulty, topN, chartPath)  -> replay list handle` | Exécute le même listage sur un thread d'arrière-plan et renvoie un handle à interroger. |
| `REPLAY:Watch(filepath, chartPath)  -> bool` | Charge le fichier de replay et arme sa lecture pour la prochaine partie ; renvoie false quand le chargement du fichier échoue ou que le replay n'est pas visionnable. chartPath active les avertissements "replay invalide" en jeu. |
| `REPLAY:Watch(filepath)  -> bool` | Idem sans chemin de partition. |
| `REPLAY.MODFLAG  (object)` | Valeurs de bits pour ModFlags : None (0), Mirror (1), Random (2), SuperRandom (4), Invisible (8), PerfectMemory (16), Avalanche (32), Minesweeper (64), Just (128), Safe (256), DynamicBeat (512). |

```lua
local flags = header.ModFlags
local mirrored = (flags & REPLAY.MODFLAG.Mirror) ~= 0
```

### Handle de liste de replays

Handle renvoyé par REPLAY:ListReplaysAsync.

<div class="callout warn">
Interrogez IsDone à chaque frame ; une fois qu'il vaut true, lisez Result.
</div>

| Méthode | Description |
| --- | --- |
| `handle.IsDone  (bool)` | Vrai une fois le listage en arrière-plan terminé. |
| `handle.Result  (array of replay headers)` | Les replays listés (vide tant qu'IsDone n'est pas true). |

### En-tête de replay

Métadonnées d'un replay sauvegardé.

<div class="callout warn">
Éléments du tableau renvoyé par REPLAY:ListReplays ou par le Result d'un handle de liste de replays. Tous les membres sont en lecture seule.
</div>

| Méthode | Description |
| --- | --- |
| `rep.FilePath  (string)` | Le chemin absolu du fichier de replay ; passez-le à REPLAY:Watch. |
| `rep.PlayerName  (string)` | Le nom du joueur qui a enregistré le replay. |
| `rep.Score  (int)` | Le score final. |
| `rep.ClearStatus  (int)` | Le statut de réussite de la partie. |
| `rep.ScoreRank  (int)` | Le rang de score de la partie. |
| `rep.Good  (int)` | Nombre de jugements Good (parfait). |
| `rep.Ok  (int)` | Nombre de jugements Ok. |
| `rep.Bad  (int)` | Nombre de jugements Bad (raté). |
| `rep.Roll  (int)` | Nombre de frappes de roulement. |
| `rep.MaxCombo  (int)` | Combo maximum. |
| `rep.Boom  (int)` | Nombre de mines frappées. |
| `rep.ADLib  (int)` | Nombre de frappes ad-lib. |
| `rep.ModFlags  (int)` | Masque de bits des mods utilisés (voir REPLAY.MODFLAG). |
| `rep.ScrollSpeed  (int)` | Le réglage de vitesse de défilement de la partie. |
| `rep.SongSpeed  (int)` | Le réglage de vitesse de la chanson de la partie. |
| `rep.JudgeStrictness  (int)` | Le réglage de fenêtre de jugement de la partie. |
| `rep.Date  (string)` | La date de la partie au format "yyyy-MM-dd HH:mm". |
| `rep.Timestamp  (int)` | La date de la partie en ticks bruts. |
| `rep.ChartUniqueID  (string)` | L'identifiant unique de la partition. |
| `rep.ChartDifficulty  (int)` | L'indice de difficulté de la partie enregistrée. |
| `rep.ChartChecksum  (string)` | Le MD5 de la partition stocké avec le replay. |
| `rep.RandomSeed  (int)` | La graine de mélange des notes, ou -1 quand le fichier n'en stocke aucune. |
| `rep.GameMode  (int)` | Le mode de jeu de la partie enregistrée. |
| `rep.GameVersion  (int)` | La version du jeu qui a enregistré le replay. |
| `rep.Watchable  (bool)` | Vrai quand le jeu peut rejouer le replay fidèlement. |
| `rep.UnwatchableReason  (string)` | Pourquoi le replay n'est pas visionnable, quand Watchable est false. |
| `rep.OldVersion  (bool)` | Vrai quand une version plus ancienne du jeu a enregistré le replay. |
| `rep.ChecksumMismatch  (bool)` | Vrai quand le fichier de partition ne correspond plus à l'enregistrement (calculé uniquement quand vous passez un chemin de partition). |

### SONGMOUNT

Globale en lecture seule pour la chanson actuellement sélectionnée pour la partie.

<div class="callout warn">
Disponible comme la globale SONGMOUNT. Elle reflète l'état défini par le Mount() d'un nœud de chanson, le Select() d'une partition ou DANBUILDER:Mount().
</div>

| Méthode | Description |
| --- | --- |
| `SONGMOUNT:ChosenUniqueId()  -> string` | Renvoie l'identifiant unique de la chanson sélectionnée, ou une chaîne vide. |
| `SONGMOUNT:ChosenDifficulty()  -> int` | Renvoie l'indice de difficulté sélectionné pour le joueur 1. |
| `SONGMOUNT:ChosenSongNode()  -> song node` | Renvoie la chanson sélectionnée sous forme de nœud de chanson (sans enfants), ou nil quand rien n'est sélectionné. |
