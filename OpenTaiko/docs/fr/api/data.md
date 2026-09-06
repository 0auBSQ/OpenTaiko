<!-- api/data.md -->

# Données et persistance

Sauvegarder des données qui survivent à un redémarrage, interroger des fichiers SQLite, lire des fichiers, du JSON et de l'INI depuis le dossier du module, partager des ressources entre modules, et lire ou modifier la configuration du jeu.

Les chemins relatifs passés à DATABASE, SQL, STORAGE, JSONLOADER, INILOADER et SHARED sont résolus par rapport au répertoire du module en cours d'exécution (le dossier qui contient son `Script.lua`).

Certaines méthodes de cette page renvoient des collections .NET, qui se comportent différemment des tables Lua :

- Les tableaux (`string[]`, `int[]`, `double[]`) commencent à l'indice 0 et exposent `.Length`.
- Les dictionnaires (JSON analysé, lignes SQL, cartes de langues) prennent `d["key"]` (ou `d[1]` pour les tableaux analysés depuis du JSON) et s'énumèrent via `d:GetEnumerator()` ; `pairs` et `#` ne fonctionnent pas sur eux.

```lua
local files = STORAGE:GetFiles("maps", "*.json")
for i = 0, files.Length - 1 do
    print(files[i])
end

local e = dict:GetEnumerator()
while e:MoveNext() do
    print(e.Current.Key, e.Current.Value)
end
```

## Base de données clé/valeur

### DATABASE

Ouvre des magasins clé/valeur LMDB qui conservent des valeurs chaîne entre les redémarrages, soit dans le dossier du module, soit dans le dossier de données global du jeu.

<div class="callout warn">
Chaque Read et chaque Write ouvre et ferme son propre environnement LMDB ; chaque appel est donc coûteux : mettez les valeurs en cache en Lua et rafraîchissez le cache quand vous écrivez. Dans les modules en lecture seule (ROActivities et arrière-plans), DATABASE renvoie des magasins dont Write journalise une erreur et ne fait rien.
</div>

| Méthode | Description |
| --- | --- |
| `DATABASE:OpenLocalDatabase(path)  -> database` | Ouvre (en le créant si nécessaire) un magasin à un chemin relatif au répertoire du module. |
| `DATABASE:OpenGlobalDatabase(path)  -> database` | Ouvre (en le créant si nécessaire) un magasin sous `Global/ApplicationData/LMDB/` dans le dossier du jeu, partagé par tous les modules. |

### Handle de base de données

Un magasin clé/valeur renvoyé par DATABASE, associant des clés chaîne à des valeurs chaîne.

<div class="callout warn">
Les valeurs sont uniquement des chaînes ; convertissez vous-même les nombres et les booléens (par exemple avec tostring et tonumber).
</div>

| Méthode | Description |
| --- | --- |
| `database:Write(key, value)  -> nil` | Stocke une chaîne sous la clé et la valide. Journalise une erreur et ne fait rien dans les modules en lecture seule. |
| `database:Read(key)  -> string` | Renvoie la chaîne stockée sous la clé, ou nil si la clé est absente ou si la lecture échoue. |
| `database:Dispose()  -> nil` | Ne fait rien ; le handle ne détient aucune ressource ouverte. |

```lua
local db

function activate()
    db = DATABASE:OpenGlobalDatabase("GameStatus")
    if db:Read("new_user") == nil then
        db:Write("new_user", "true")
    end
end
```

## Base de données SQL

### SQL

Ouvre un fichier de base de données SQLite dans le répertoire du module pour exécuter des instructions SQL.

| Méthode | Description |
| --- | --- |
| `SQL:OpenSQLDatabase(path)  -> sql` | Ouvre une base de données SQLite à un chemin relatif au répertoire du module. |

### Handle SQL

Une connexion SQLite renvoyée par SQL:OpenSQLDatabase.

<div class="callout warn">
Query renvoie un dictionnaire indexé 1..n ; chaque ligne est un dictionnaire indexé par nom de colonne (voir la note sur les collections .NET en haut de la page). Une instruction en échec journalise l'erreur et renvoie un résultat vide. Query transmet le texte de l'instruction à SQLite sans le modifier, sans liaison de paramètres ; échappez donc toute valeur que vous y insérez.
</div>

| Méthode | Description |
| --- | --- |
| `sql:Query(query)  -> rows` | Exécute le texte SQL et renvoie les lignes de résultat. |

```lua
local db

function activate()
    db = SQL:OpenSQLDatabase("Databases/Items.db3")
    local rows = db:Query("SELECT * FROM itempool WHERE Slot = 'regular'")
    for i = 1, rows.Count do
        print(rows[i]["Code"])
    end
end
```

## Fichiers, JSON et INI

### STORAGE

Accès aux fichiers enraciné dans le répertoire du module, plus des assistants pour partager les codes de lobby en ligne.

<div class="callout warn">
WriteText n'écrit que dans le dossier du module : il rejette les chemins absolus et les chemins qui se résolvent en dehors. ReadText accepte aussi un chemin absolu. Les assistants de code de lobby utilisent le dossier partagé `Global/Lobbycodes/` à côté de l'exécutable. Les modules en lecture seule peuvent utiliser chaque méthode de STORAGE.
</div>

| Méthode | Description |
| --- | --- |
| `STORAGE:GetFiles(dir, pattern)  -> string[]` | Liste les fichiers d'un sous-répertoire du dossier du module correspondant à un motif de recherche tel que `"*.png"`. Les entrées sont des chemins relatifs au dossier du module, sous-répertoire compris. Le répertoire doit exister. |
| `STORAGE:FileExists(path)  -> bool` | Vrai si un fichier existe au chemin relatif au dossier du module. |
| `STORAGE:DirectoryExists(path)  -> bool` | Vrai si un répertoire existe au chemin relatif au dossier du module. |
| `STORAGE:WriteText(name, contents)  -> bool` | Écrit du texte dans un fichier sous le dossier du module, en créant les sous-répertoires. Renvoie false pour les chemins absolus ou sortants, ou en cas d'échec. |
| `STORAGE:ReadText(name)  -> string` | Renvoie le texte brut d'un fichier (relatif au dossier du module, ou absolu), ou nil s'il est absent ou illisible. |
| `STORAGE:GetFullPath(name)  -> string` | Renvoie le chemin absolu d'un fichier sous le dossier du module, ou nil pour une entrée vide ou absolue. |
| `STORAGE:WriteLobbyCode(name, contents)  -> bool` | Écrit un fichier dans `Global/Lobbycodes/`. Renvoie false pour les noms vides, absolus ou contenant `..`, ou en cas d'échec. |
| `STORAGE:RevealLobbyCodes()  -> bool` | Ouvre l'explorateur de fichiers du système sur `Global/Lobbycodes/`. |
| `STORAGE:RevealInExplorer(name)  -> bool` | Ouvre l'explorateur de fichiers du système avec le fichier de module nommé sélectionné (Windows et macOS), ou son dossier ailleurs. |

### JSONLOADER

Analyse des fichiers et des chaînes JSON depuis le répertoire du module.

<div class="callout warn">
Les valeurs analysées sont des dictionnaires .NET : les objets sont indexés par nom de membre, les tableaux sont indexés 1..n. Indexer directement une clé absente (`d["x"]`) lève une erreur ; utilisez JsonGet pour les recherches qui peuvent échouer. Les nombres deviennent des entiers ou des doubles ; les chaînes, les booléens et null correspondent à leurs équivalents Lua. LoadJson renvoie un arbre de JsonNode ; indexez-le avec `node["member"]` et convertissez les feuilles avec ExtractNumber / ExtractText.
</div>

| Méthode | Description |
| --- | --- |
| `JSONLOADER:LoadJson(name)  -> JsonNode` | Analyse un fichier JSON relatif au répertoire du module en un arbre JsonNode. Lève une erreur si le fichier est absent. |
| `JSONLOADER:ExtractNumber(value)  -> number` | Convertit une feuille JsonNode en nombre ; renvoie 0 pour nil ou une valeur non numérique. |
| `JSONLOADER:ExtractText(value)  -> string` | Convertit une feuille JsonNode en chaîne ; renvoie nil pour nil. |
| `JSONLOADER:JsonParseFile(name)  -> dict` | Analyse un fichier JSON dont la racine est un objet (dictionnaire vide si le fichier est vide). Lève une erreur si le fichier est absent ou si la racine n'est pas un objet. |
| `JSONLOADER:JsonParseFileAny(name)  -> dict` | Analyse un fichier JSON dont la racine est un objet ou un tableau (chemin relatif ou absolu). Renvoie nil si le fichier est absent ou vide. |
| `JSONLOADER:JsonParseString(json)  -> dict` | Analyse une chaîne JSON dont la racine est un objet (dictionnaire vide si elle est vide). Lève une erreur si la racine n'est pas un objet. |
| `JSONLOADER:JsonParseStringAny(json)  -> dict` | Analyse une chaîne JSON dont la racine est un objet ou un tableau. Renvoie nil pour une entrée vide ou invalide. |
| `JSONLOADER:JsonGet(dict, key)  -> value` | Recherche une clé chaîne dans un objet ou une clé entière dans un tableau ; renvoie nil en cas d'absence ou quand dict n'est pas une valeur analysée. |
| `JSONLOADER:JsonCount(dict)  -> int` | Nombre de membres d'un objet ou d'un tableau analysé, ou 0 pour toute autre chose. |

```lua
local data = JSONLOADER:JsonParseFileAny("Config/layout.json")
local colors = data and JSONLOADER:JsonGet(data, "colors")
local n = JSONLOADER:JsonCount(colors)
for i = 1, n do
    print(JSONLOADER:JsonGet(colors, i))
end
```

### INILOADER

Charge un fichier `key=value` plat depuis le répertoire du module.

<div class="callout warn">
Le chargeur scinde chaque ligne au premier `=`, ignore les lignes sans `=`, et laisse une clé répétée écraser la valeur précédente. Il n'a ni sections, ni commentaires, ni guillemets. Un fichier absent donne un handle vide.
</div>

| Méthode | Description |
| --- | --- |
| `INILOADER:LoadIni(name)  -> ini` | Lit un fichier `key=value` relatif au répertoire du module. |

### Handle INI

Un fichier INI analysé renvoyé par INILOADER:LoadIni, avec des accesseurs typés.

<div class="callout warn">
Les accesseurs renvoient la valeur par défaut fournie quand la clé est absente. Quand la clé existe mais que sa valeur ne peut pas être analysée, les accesseurs numériques renvoient 0. Les accesseurs de tableau scindent aux virgules et renvoient un tableau vide quand la clé est absente.
</div>

| Méthode | Description |
| --- | --- |
| `ini:GetBool(key, default)  -> bool` | Vrai quand la valeur s'analyse comme l'entier 1 ; la valeur par défaut quand la clé est absente. |
| `ini:GetInt(key, default)  -> int` | La valeur sous forme d'entier. |
| `ini:GetDouble(key, default)  -> number` | La valeur sous forme de double. |
| `ini:GetString(key, default)  -> string` | La valeur chaîne brute. |
| `ini:GetStringArray(key)  -> string[]` | La valeur scindée aux virgules. |
| `ini:GetIntArray(key)  -> int[]` | La valeur scindée aux virgules, chaque partie analysée comme un entier (0 si non analysable). |
| `ini:GetDoubleArray(key)  -> double[]` | La valeur scindée aux virgules, chaque partie analysée comme un double (0 si non analysable). |

## Ressources partagées et configuration

### SHARED

Un magasin global au jeu de textures, de sons et de chaînes qui survit aux changements de stage, si bien que chaque module peut utiliser une ressource chargée une fois (par exemple au démarrage).

<div class="callout warn">
Les méthodes Set* chargent sur un thread d'arrière-plan et substituent la ressource sur le thread de rendu ; la fonction de rappel facultative onCreate reçoit le nouveau handle une fois en place. Un Set* plus récent sur la même clé abandonne tout chargement encore en cours. La substitution libère la ressource précédente, ce qui invalide tout handle récupéré avant le rechargement ; récupérez-le de nouveau avec Get*. Les variantes UsingAbsolutePath prennent un chemin complet. Voir Graphismes et texte pour les handles de texture et Audio pour les handles de son.
</div>

| Méthode | Description |
| --- | --- |
| `SHARED:SetSharedString(key, value)  -> nil` | Stocke une chaîne sous une clé. |
| `SHARED:GetSharedString(key)  -> string` | Renvoie la chaîne stockée sous une clé, ou une chaîne vide. |
| `SHARED:GetSharedTexture(key)  -> texture` | Renvoie la texture partagée d'une clé, ou une texture vide si aucune n'a été définie. |
| `SHARED:GetSharedSound(key)  -> sound` | Renvoie le son partagé d'une clé, ou un son vide si aucun n'a été défini. |
| `SHARED:ClearSharedTexture(key)  -> nil` | Libère la texture stockée sous une clé et la remplace par une texture vide. |
| `SHARED:ClearSharedSound(key)  -> nil` | Libère le son stocké sous une clé et le remplace par un son vide. |
| `SHARED:SetSharedTexture(key, path, onCreate?)  -> nil` | Charge dans le magasin une texture depuis un chemin relatif au module. |
| `SHARED:SetSharedTexture(key, path, options, onCreate?)  -> nil` | Idem, avec une table d'options ; `{ maxSize = N }` limite le plus grand côté de la texture décodée à N pixels. |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, onCreate?)  -> nil` | Charge une texture depuis un chemin absolu. |
| `SHARED:SetSharedTextureUsingAbsolutePath(key, path, options, onCreate?)  -> nil` | Charge une texture depuis un chemin absolu avec une table d'options. |
| `SHARED:SetSharedSFX(key, path, onCreate?)  -> nil` | Charge un effet sonore depuis un chemin relatif au module. |
| `SHARED:SetSharedBGM(key, path, onCreate?)  -> nil` | Charge une musique de fond (groupe de volume de lecture de chanson) depuis un chemin relatif au module. |
| `SHARED:SetSharedVoice(key, path, onCreate?)  -> nil` | Charge un extrait vocal depuis un chemin relatif au module. |
| `SHARED:SetSharedPreview(key, path, onCreate?)  -> nil` | Charge un extrait d'aperçu de chanson depuis un chemin relatif au module. |
| `SHARED:SetSharedSFXUsingAbsolutePath(key, path, onCreate?)  -> nil` | Charge un effet sonore depuis un chemin absolu. |
| `SHARED:SetSharedBGMUsingAbsolutePath(key, path, onCreate?)  -> nil` | Charge une musique de fond depuis un chemin absolu. |
| `SHARED:SetSharedVoiceUsingAbsolutePath(key, path, onCreate?)  -> nil` | Charge un extrait vocal depuis un chemin absolu. |
| `SHARED:SetSharedPreviewUsingAbsolutePath(key, path, onCreate?)  -> nil` | Charge un extrait d'aperçu de chanson depuis un chemin absolu. |

```lua
-- dans le stage de démarrage
function onStart()
    SHARED:SetSharedSFX("Decide", "Sounds/Decide.ogg")
end

-- dans n'importe quel module ultérieur
SHARED:GetSharedSound("Decide"):Play()
```

### CONFIG

Lit et modifie la configuration du jeu : nombre de joueurs, modes, calcul du score, mods de gameplay par joueur et niveaux de volume.

<div class="callout warn">
Les propriétés utilisent la syntaxe à point (`CONFIG.PlayerCount`), les méthodes la syntaxe à deux-points. Les indices de joueur commencent à 0 (0 à 4) ; les mutateurs ignorent les indices hors limites et les accesseurs renvoient une valeur par défaut pour ceux-ci. Dans les modules en lecture seule (ROActivities et arrière-plans), chaque mutateur journalise une erreur et ne fait rien. Les changements s'appliquent immédiatement en mémoire ; le jeu écrit Config.ini quand il se ferme normalement.
</div>

| Méthode | Description |
| --- | --- |
| `CONFIG.ConfigIsNew  -> bool (read-only)` | Vrai quand le jeu a créé la configuration à ce démarrage. |
| `CONFIG.Language  -> string (read-only)` | L'identifiant de langue stocké dans Config.ini. |
| `CONFIG.PlayerCount  -> int` | Nombre de joueurs actifs. Le mutateur ignore les valeurs hors de 1..5. |
| `CONFIG.IsAIBattleMode  -> bool` | Si le mode bataille contre l'IA est activé. |
| `CONFIG.AILevel  -> int` | Niveau de difficulté de l'IA, borné à 1..10 à l'écriture. |
| `CONFIG.IsTrainingMode  -> bool` | Si le mode entraînement est activé. |
| `CONFIG.UseModernScoringMethod  -> bool` | Si le jeu utilise la méthode de calcul du score moderne (shin-uchi). |
| `CONFIG.UsedLegacyScoringMethod  -> int` | Génération de calcul du score historique (voir `CONFIG.LEGACY_SCORING`), bornée à 0..3 à l'écriture. |
| `CONFIG.AreSongUnlockablesDisabled  -> bool (read-only)` | Si le jeu ignore les conditions de déblocage des chansons. |
| `CONFIG.SongSpeed  -> int` | Vitesse de la chanson en vingtièmes du multiplicateur : 20 vaut 1,0x. Bornée à 2..200 (0,1x à 10x) à l'écriture. |
| `CONFIG.MasterVolume  -> int` | Volume principal, borné à 0..100 à l'écriture. |
| `CONFIG.SoundEffectVolume  -> int` | Volume des effets sonores, borné à 0..100 à l'écriture. |
| `CONFIG.VoiceVolume  -> int` | Volume des voix, borné à 0..100 à l'écriture. |
| `CONFIG.SongVolume  -> int` | Volume de lecture des chansons, borné à 0..100 à l'écriture. |
| `CONFIG.PreviewVolume  -> int` | Volume des aperçus de chanson, borné à 0..100 à l'écriture. |
| `CONFIG:GetGameType(player)  -> int` | Le type de jeu du joueur (voir `CONFIG.GAMETYPE`) ; Taiko pour les indices hors limites. |
| `CONFIG:SetGameType(player, gameType)  -> nil` | Définit le type de jeu du joueur et ignore les valeurs non définies. |
| `CONFIG:GetDefaultCourse(player)  -> int` | La difficulté par défaut (voir `CONFIG.DEFAULT_COURSE`) ; Normal pour les indices hors limites. Un seul réglage global s'applique à tous les joueurs malgré l'argument player. |
| `CONFIG:SetDefaultCourse(player, difficulty)  -> nil` | Définit la difficulté par défaut globale, bornée de Easy à une valeur au-delà d'Extra Extreme (l'affichage combiné Extra/Extra-Extra). |
| `CONFIG:GetScrollSpeed(player)  -> int` | La valeur de vitesse de défilement du joueur : 9 vaut 1,0x, chaque pas vaut 0,1x (voir `CONFIG.SCROLLSPEED`). 9 pour les indices hors limites. |
| `CONFIG:SetScrollSpeed(player, speed)  -> nil` | Définit la valeur de vitesse de défilement du joueur, bornée à 0..99. |
| `CONFIG:GetTimingZone(player)  -> int` | La fenêtre de jugement du joueur : 0 Loose, 1 Lenient, 2 Normal, 3 Strict, 4 Rigorous. 2 pour les indices hors limites. |
| `CONFIG:SetTimingZone(player, zone)  -> nil` | Définit la fenêtre de jugement du joueur, bornée à 0..4. |
| `CONFIG:GetAutoStatus(player)  -> bool` | Vrai quand le joueur est en jeu automatique ou regarde un replay. |
| `CONFIG:SetAutoStatus(player, isAuto)  -> nil` | Active ou désactive le jeu automatique pour le joueur. |
| `CONFIG:GetRandomMod(player)  -> int` | Le mod aléatoire du joueur (voir `CONFIG.RANDOM`) ; Off pour les indices hors limites. |
| `CONFIG:SetRandomMod(player, mode)  -> nil` | Définit le mod aléatoire du joueur et ignore les valeurs non définies. |
| `CONFIG:GetFunMod(player)  -> int` | Le mod fun du joueur (voir `CONFIG.FUN`) ; None pour les indices hors limites. |
| `CONFIG:SetFunMod(player, mod)  -> nil` | Définit le mod fun du joueur et ignore les valeurs non définies. |
| `CONFIG:GetStealthMod(player)  -> int` | Le mod furtif du joueur (voir `CONFIG.STEALTH`) ; Off pour les indices hors limites. |
| `CONFIG:SetStealthMod(player, mode)  -> nil` | Définit le mod furtif du joueur et ignore les valeurs non définies. |
| `CONFIG:GetJusticeMod(player)  -> int` | Le mod de jugement du joueur : 0 désactivé, 1 Just (Ok compte comme Bad), 2 Safe (Bad compte comme Ok). |
| `CONFIG:SetJusticeMod(player, mode)  -> nil` | Définit le mod de jugement du joueur, borné à 0..2. |
| `CONFIG:GetModFlags(player)  -> integer` | Compacte la vitesse de défilement, le mod furtif, le mod aléatoire, la vitesse de la chanson, la fenêtre de jugement, le mod de jugement et le mod fun dans une seule valeur 64 bits (un octet chacun). |
| `CONFIG:SetModFlags(player, flags)  -> nil` | Applique au joueur une valeur produite par GetModFlags (la vitesse de la chanson est globale). |

Des tables de constantes sur CONFIG donnent des noms aux valeurs entières ci-dessus. `SONGSPEED` et `SCROLLSPEED` convertissent aussi entre valeurs stockées et multiplicateurs.

| Membre | Description |
| --- | --- |
| `CONFIG.SONGSPEED.Normal  -> int` | 20, la valeur stockée pour 1,0x. |
| `CONFIG.SONGSPEED:ToActual(value)  -> number` | Convertit une vitesse de chanson stockée en son multiplicateur (value / 20). |
| `CONFIG.SONGSPEED:FromActual(multiplier)  -> int` | Convertit un multiplicateur en la vitesse de chanson stockée la plus proche. |
| `CONFIG.SCROLLSPEED.Normal  -> int` | 9, la valeur stockée pour 1,0x. |
| `CONFIG.SCROLLSPEED:ToActual(value)  -> number` | Convertit une vitesse de défilement stockée en son multiplicateur ((value + 1) / 10). |
| `CONFIG.SCROLLSPEED:FromActual(multiplier)  -> int` | Convertit un multiplicateur en la vitesse de défilement stockée la plus proche. |
| `CONFIG.GAMETYPE` | `Taiko`, `Konga`. |
| `CONFIG.DEFAULT_COURSE` | `Easy`, `Normal`, `Hard`, `Oni`, `Edit`. |
| `CONFIG.LEGACY_SCORING` | `Gen1Oni`, `Gen1_2`, `Gen2`, `Gen3`. |
| `CONFIG.RANDOM` | `Off`, `Random`, `Mirror`, `SuperRandom`, `MirrorRandom`. |
| `CONFIG.STEALTH` | `Off`, `Doron`, `Stealth`. |
| `CONFIG.FUN` | `None`, `Avalanche`, `Minesweeper`, `DynamicBeat`, `Total`. |
| `CONFIG.JUSTICE` | `None`, `Just`, `Safe`. |

```lua
local multiplier = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
if CONFIG:GetRandomMod(0) == CONFIG.RANDOM.Mirror then
    -- partition en miroir
end
```
