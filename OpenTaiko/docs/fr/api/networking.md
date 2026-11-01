<!-- api/networking.md -->

# Réseau en ligne <span class="badge-exp">Expérimental</span>

La globale `NET` est le client d'OpenTaiko Online, le protocole pair-à-pair derrière le lobby en ligne et les stages multijoueurs. Un joueur crée un salon et reçoit un code de salon ; les autres joueurs le rejoignent avec ce code. Le serveur (le créateur du salon, sauf si le salon a migré) relaie tout le trafic, et chaque joueur tient une liste des participants présents dans le salon.

Comment Lua l'utilise :

- Toutes les données traversent la frontière Lua sous forme de chaînes. Les stages transportent les données structurées en JSON.
- Le réseau s'exécute sur des threads d'arrière-plan et pousse des événements dans une file. Un stage vide la file à chaque frame en appelant `NET:Poll()` en boucle jusqu'à ce qu'elle renvoie nil.
- Le réseau est désactivé par défaut. Le réglage "Allow LuaNetworking connections" l'active ; tant qu'il est désactivé, CreateRoom renvoie nil, JoinRoom renvoie false et le client met en file un événement "error".
- Identifiants de salon : le créateur du salon porte l'identifiant 1. Chaque nouvel arrivant reçoit l'identifiant suivant dans l'ordre d'arrivée. `NET:SelfId()` vaut 0 hors d'un salon.
- Le rôle d'hôte est distinct de la possession de la connexion. Il désigne le joueur qui pilote actuellement le lobby (choisit la chanson, lance la partie). Il commence chez le créateur ; RotateHost ou SetHostRole le transmet à un autre joueur.
- Si le créateur du salon part, les joueurs restants migrent automatiquement vers un nouveau serveur ; un salon ne se ferme que quand personne ne peut prendre le relais.

```lua
function update(ts)
    while true do
        local e = NET:Poll()
        if e == nil then break end
        if e.Type == "message" and e.Channel == "chat" then
            addChatLine(e.Peer, e.Data)
        elseif e.Type == "left" then
            removePlayer(e.Peer)
        elseif e.Type == "roomclosed" then
            return Exit("title")
        end
    end
end
```

## NET

### Cycle de vie du salon

<div class="callout warn">
Disponible comme la globale NET. Appelez SetLocalPlayer avant CreateRoom ou JoinRoom pour que les autres joueurs reçoivent vos informations à l'arrivée. JoinRoom est asynchrone : le résultat arrive plus tard sous forme d'événement "connected" ou "error". Créer ou rejoindre un salon quitte d'abord tout salon courant.
</div>

| Méthode | Description |
| --- | --- |
| `NET:SetLocalPlayer(infoJson)  -> void` | Définit la chaîne d'informations du joueur local (typiquement du JSON avec la plaque de nom, le personnage et ainsi de suite). Le client l'envoie aux autres joueurs quand vous rejoignez, et elle apparaît dans leurs événements "joined" et dans PeersJson. Une chaîne vide devient "{}" ; le serveur remplace une chaîne de plus de 16384 caractères par "{}". |
| `NET:CreateRoom(stageId, payload, maxPlayers)  -> string` | Crée un salon pour l'identifiant de stage donné avec une chaîne de charge utile facultative et renvoie son code de salon, ou nil si le réseau est désactivé ou si la création du salon a échoué. maxPlayers plafonne la liste des participants (8 par défaut). |
| `NET:CreateRoom(stageId, payload)  -> string` | Idem avec le plafond par défaut de 8 joueurs. |
| `NET:PeekStageId(roomCode)  -> string` | Renvoie l'identifiant de stage stocké dans un code de salon sans se connecter, ou nil si le code est invalide. Permet à un écran de connexion d'aiguiller vers le bon stage. |
| `NET:JoinRoom(roomCode)  -> bool` | Commence à rejoindre le salon. Renvoie false immédiatement si le réseau est désactivé ou si le code est mal formé ; sinon le résultat arrive sous forme d'événement "connected" ou "error". |
| `NET:Leave()  -> void` | Quitte le salon et vide la file d'événements. Quand le créateur du salon part alors que d'autres restent, un autre joueur reprend le salon ; sinon il se ferme pour tout le monde. |
| `NET.PortOverride  (int, settable)` | Quand supérieur à 0, les salons utilisent ce port TCP ; 0 sélectionne le port par défaut 41234. Laissez-le à 0 en usage normal. |

### Messagerie

<div class="callout warn">
Le stage choisit librement les noms de canal. La mécanique des manches de jeu réserve les noms de canal "ps", "ld" et "fn", qui n'atteignent jamais Poll().
</div>

| Méthode | Description |
| --- | --- |
| `NET:Broadcast(channel, data)  -> void` | Envoie un message sur le canal donné à tous les autres joueurs du salon. |
| `NET:SendTo(peerId, channel, data)  -> void` | Envoie un message sur le canal donné à un joueur par identifiant. -1 envoie à tous les autres, comme Broadcast. |
| `NET:Poll()  -> NetEvent` | Retire et renvoie le prochain événement en attente, ou nil quand la file est vide. |

### État du salon et rôle d'hôte

| Méthode | Description |
| --- | --- |
| `NET:SelfId()  -> int` | L'identifiant de ce joueur (1 pour le créateur du salon), ou 0 hors d'un salon. |
| `NET:Connected()  -> bool` | Vrai dans un salon : ce joueur possède un identifiant propre et est soit le serveur, soit connecté à lui. |
| `NET:IsHost()  -> bool` | Vrai quand ce joueur possède la connexion du salon (le créateur du salon, ou le joueur qui a pris le relais après une migration). |
| `NET:HasHostRole()  -> bool` | Vrai quand ce joueur détient actuellement le rôle d'hôte. |
| `NET:HostRoleId()  -> int` | L'identifiant du joueur qui détient le rôle d'hôte. |
| `NET:PeerCount()  -> int` | Nombre de joueurs dans la liste des participants, soi-même compris. |
| `NET:PeersJson()  -> string` | La liste des participants sous forme de tableau JSON dans l'ordre d'arrivée. Chaque entrée est {id, info, isHost, hostRole} : info est la chaîne que ce joueur a donnée à SetLocalPlayer, isHost est true pour l'identifiant 1, hostRole est true pour le détenteur courant du rôle d'hôte. |
| `NET:RotateHost()  -> void` | Transmet le rôle d'hôte au joueur suivant dans l'ordre d'arrivée. Ne fonctionne que sur le serveur ; elle ignore les appels des autres joueurs. |
| `NET:SetHostRole(peerId)  -> void` | Donne le rôle d'hôte au joueur donné et l'annonce au salon. Ne fonctionne que sur le serveur et uniquement pour les identifiants de la liste des participants. |

### Manches de jeu

Pendant une chanson en ligne, le jeu suspend le stage de lobby et l'écran de jeu pilote lui-même l'échange : il diffuse le score courant du joueur local, alimente les spots distants depuis le réseau et attend aux barrières de chargement et de fin. Le lobby n'a qu'à déclarer qui joue dans quel spot et à ouvrir et fermer la manche.

<div class="callout warn">
Les spots sont les positions de joueur de l'écran de jeu : le spot 0 est toujours le joueur local, les spots 1 et suivants sont les joueurs distants. Définissez la correspondance avec SetPlaySpots, puis appelez BeginPlaySync juste avant d'entrer en jeu, et EndPlaySync une fois que le lobby reprend la main. L'écran de jeu appelle les méthodes de barrière, d'envoi de score et de probabilités ; elles figurent ici parce que le même objet les expose.
</div>

| Méthode | Description |
| --- | --- |
| `NET:SetPlaySpots(json)  -> void` | Définit la correspondance spot-vers-joueur de la prochaine partie à partir d'un tableau JSON d'identifiants, l'indice 0 étant soi-même (par exemple "[1,3,2]"). Une chaîne non analysable efface la correspondance. |
| `NET:PlaySpotCount()  -> int` | Nombre d'entrées dans la correspondance de spots courante (0 quand aucune n'est définie). |
| `NET:BeginPlaySync(selfName)  -> void` | Ouvre une manche de jeu : efface les scores précédents et enregistre le nom d'affichage du joueur local pour les messages de score courant. |
| `NET:EndPlaySync()  -> void` | Ferme la manche de jeu et efface les barrières et les résultats par spot. |
| `NET:IsRemoteSpot(spot)  -> bool` | Vrai quand le spot correspond à un joueur distant pendant une manche ouverte. |
| `NET:IsSpotActive(spot)  -> bool` | Vrai quand le joueur associé au spot est toujours dans la liste des participants (le spot 0 est toujours true). False signifie que le joueur a quitté en cours de partie. |
| `NET:GetSpotPlayJson(spot)  -> string` | Le dernier JSON de score courant qu'un spot distant a envoyé, ou une chaîne vide. Clés : n (nom), s (score), g (jauge), a (précision), gr (great), gd (good), ms (bad), co (combo). |
| `NET:GetSelfPlayScore()  -> string` | Le dernier JSON de score courant diffusé par ce client. |
| `NET:LivePlayScoresJson()  -> string` | Le dernier JSON de score courant de chaque joueur, sous forme de tableau JSON de {id, d} où d est la chaîne JSON du score. Le tableau inclut soi-même dès que ce client a diffusé au moins une fois. |
| `NET:GetSpotResultJson(spot)  -> string` | Le JSON de résultat final rapporté par un spot distant à la fin de la chanson, ou une chaîne vide. Clés : cl (réussite), fc (full combo), pf (perfect), mx (jauge arc-en-ciel) en booléens ; gr, gd, ms, rl (roulements), bl (ballons), ad (ad-libs), hc (combo le plus élevé), sc (score) en entiers. |
| `NET:GetSpotClearLevel(spot)  -> int` | Niveau de réussite du résultat final d'un spot distant : 2 arc-en-ciel, 1 réussite, 0 échec, -1 quand aucun résultat n'est arrivé. |
| `NET:GetSpotJudge(spot, key)  -> int` | Un champ entier du résultat final d'un spot distant par clé (voir GetSpotResultJson), ou -1 quand aucun résultat n'est arrivé. |
| `NET:GetSpotBadOdds(spot)  -> int` | Probabilité pour mille (0-1000) que le jeu juge bad la prochaine frappe automatique d'un spot distant, dérivée des nombres de jugements diffusés. |
| `NET:GetSpotGoodOdds(spot)  -> int` | Probabilité pour mille (0-1000) que le jeu juge good la prochaine frappe automatique d'un spot distant. |
| `NET:PushPlayScore(json)  -> void` | Diffuse le JSON de score courant du joueur local. L'écran de jeu l'appelle plusieurs fois par seconde. |
| `NET:ReportLoaded()  -> void` | Signale une fois que ce client a terminé de charger la chanson. |
| `NET:LoadBarrierReady(timeoutMs)  -> bool` | Vrai une fois que chaque joueur de la liste des participants a signalé son chargement, ou une fois le délai écoulé depuis le signalement de ce client (le serveur abandonne alors les joueurs qui n'ont jamais signalé). |
| `NET:ReportFinished(resultJson)  -> void` | Signale une fois que ce client a terminé la chanson, avec son JSON de résultat final. |
| `NET:FinishBarrierReady(timeoutMs)  -> bool` | Vrai une fois que chaque joueur de la liste des participants a signalé sa fin, ou une fois le délai écoulé. |
| `NET:BarrierReset()  -> void` | Efface les barrières de chargement et de fin ainsi que les résultats par spot. |

## NetEvent

L'objet renvoyé par NET:Poll(), décrivant un événement réseau.

<div class="callout warn">
Lisez directement ses champs ; il n'a pas de méthodes. Peer vaut 0 quand l'événement n'a pas de joueur associé.
</div>

| Méthode | Description |
| --- | --- |
| `event.Type  -> string` | Le type d'événement, l'une des valeurs listées ci-dessous. |
| `event.Peer  -> int` | L'identifiant du joueur associé : votre propre identifiant pour "connected", l'expéditeur pour "message", le joueur concerné pour "joined", "left" et "hostrole" ; 0 sinon. |
| `event.Channel  -> string` | Le canal d'un événement "message" ; vide pour les autres types. |
| `event.Data  -> string` | La charge utile de l'événement ; voir ci-dessous. |

| Type | Signification |
| --- | --- |
| `connected` | Vous avez rejoint un salon. Data est un objet JSON {selfId, stageId, payload, hostRoleId}. Le créateur du salon ne le reçoit jamais. Les joueurs déjà présents dans le salon apparaissent dans PeersJson et ne lèvent aucun événement "joined". |
| `joined` | Un joueur a rejoint après vous. Peer est son identifiant, Data est sa chaîne d'informations. |
| `left` | Un joueur est parti ou le serveur l'a abandonné. Peer est son identifiant. |
| `message` | Un message d'un autre joueur. Peer est l'expéditeur, Channel et Data sont le message. |
| `hostrole` | Le rôle d'hôte a changé de mains. Peer est l'identifiant du nouveau détenteur. |
| `roomclosed` | Le salon s'est fermé, ou le rétablissement de la connexion a échoué. |
| `error` | Un message d'état à afficher au joueur. Data est le texte. Le client le lève quand le réseau est désactivé, quand la création ou la connexion échoue, quand le salon est plein, et comme notifications de progression pendant une migration d'hôte. |
