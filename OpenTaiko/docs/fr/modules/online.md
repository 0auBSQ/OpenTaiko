<!-- modules/online.md -->

# Online

La logique du salon en ligne : les salons ouverts et rejoints par code, la liste des joueurs, la difficulté, les mods et l'état prêt de chaque joueur, le morceau du salon, le début d'une manche, les résultats et la manche suivante. Les modules `Online` ne dessinent rien ; un skin dessine ses propres écrans de salon et y branche sa propre sélection de morceaux. Le salon de chaque skin parle les mêmes messages, si bien que des joueurs sur des skins différents se retrouvent dans un même salon. La stage `onlinelobby` d'Open-World Memories est un de ces clients. Les modules s'appuient sur le [global NET](../api/networking.md), qui est expérimental.

## Les modules

| Module | Contenu |
| --- | --- |
| `Online.Lobby` | La session : son état, chaque action d'un écran, le branchement de la sélection de morceaux et le traitement des événements. |
| `Online.Protocol` | Les messages : les noms des canaux et le JSON de chaque contenu. |
| `Online.Identity` | Ce qu'un joueur envoie sur lui-même (nom, personnage, puchichara, plaque de nom, plaque de dan, palette). |
| `Online.Media` | Le morceau du salon sur cette machine : ses niveaux, sa jaquette dans la texture partagée `preimage`, son aperçu dans le son partagé `presound`. |
| `Online.Texts` | Les textes propres aux modules (`lang/<code>/online.json`). |

Un skin n'a besoin que de `Online.Lobby`, qui réexporte ce qu'il utilise des autres. Il y a une session par état Lua : le module garde son état dans `Lobby.net`.

## L'état de la session

Le skin lit `Lobby.net` à chaque frame pour dessiner ses écrans :

| Champ | Description |
| --- | --- |
| `online`, `connecting` | Dans un salon ; en attente de la réponse d'une connexion. |
| `isHost`, `code` | Cette machine fait tourner le salon (elle l'a créé) ; le code du salon. |
| `roomGone` | Le salon a fermé : le quitter et revenir au menu du skin. |
| `msg`, `msgCode`, `msgArg` | Le dernier message, déjà dans la langue du jeu, son code stable et son argument (voir Messages). |
| `notice` | Un message arrivé pendant qu'on est dans un salon, à afficher une fois puis à effacer. |
| `nameByPeer`, `infoByPeer` | Les joueurs par id : les noms, et les infos d'`Online.Identity` (`name`, `char`, `puchi`, `dan`, `npk`, `dgold`, `dtype`, `palStops`, `palBlend`). |
| `diffByPeer`, `modByPeer`, `readyByPeer` | La difficulté (0 à 4), les mods et l'état prêt de chaque joueur. |
| `lackByPeer`, `watchByPeer` | Les joueurs sans le morceau du salon ; les joueurs encore sur les résultats. |
| `song` | Le morceau du salon `{ uid, title, diff, speed, dyn }`, nil tant que le rôle d'hôte n'en a pas choisi. |
| `songLevels`, `songPlus`, `songSubtitle`, `songBpm`, `iLackSong` | Le morceau sur cette machine : le niveau et la marque plus de chaque difficulté qu'il a, son sous-titre et son BPM ; `iLackSong` quand cette machine ne l'a pas. |
| `resultByPeer` | Les résultats de la manche : `{ name, sc, gr, gd, ms, co, cl, fc, pf, ac, ab }` (score, great, good, bad, combo, réussite, full combo, parfait, précision, abandon). |
| `resultsT`, `resultsReadyT` | Secondes passées sur les résultats ; défini une fois qu'ils sont complets. |

## Online.Lobby

| Fonction | Description |
| --- | --- |
| `Lobby.host()  -> bool` | Ouvre un salon (au plus `Lobby.MAXP` = 5 joueurs) et enregistre son code avec `STORAGE:WriteLobbyCode` ; false avec `net.msg` quand NET refuse. |
| `Lobby.join(code)  -> bool` | Commence à rejoindre le salon d'un code (les espaces sont ignorés) ; la réponse arrive par `drain`. False avec `net.msg` quand le code est vide, invalide, ou pour un autre type de salon. |
| `Lobby.leave()` | Termine la session (aussi après une connexion échouée) et rend les places des joueurs aux sauvegardes. |
| `Lobby.drain()` | Traite chaque événement NET en attente ; à appeler à chaque frame tant que `online` ou `connecting` est vrai. |
| `Lobby.amController()  -> bool` | Ce joueur a le rôle d'hôte : il choisit le morceau et lance la manche. |
| `Lobby.peerIds()`, `Lobby.count()` | Les ids des joueurs dans l'ordre ; leur nombre. |
| `Lobby.spotOf[id]` | La place locale de chaque joueur (0 pour soi), pour `NAMEPLATE:DrawPlayerNameplate`. |
| `Lobby.myDiff()`, `Lobby.setDiff(d)`, `Lobby.stepDiff(dir)  -> bool` | Ma difficulté ; la définir ; la passer à la suivante que le morceau a (false au bout de la rangée). |
| `Lobby.availDiffs()`, `Lobby.diffAvailable(d)`, `Lobby.fixDiff()` | Les difficultés du morceau ; garder la mienne sur l'une d'elles (à appeler à chaque frame du salon). |
| `Lobby.myReady()`, `Lobby.setReady(b)` | Mon état prêt. |
| `Lobby.broadcastMods()` | Lit les mods du joueur 0 dans `CONFIG` et les envoie ; à appeler quand la sélection de mods du skin se ferme. |
| `Lobby.applyModsToSlot(slot, mods)` | Place les mods d'un joueur sur une place de joueur de `CONFIG`, pour dessiner leurs icônes avec la ROActivity `modicons`. |
| `Lobby.adjustSpeed(delta)`, `Lobby.setSpeed(sp)` | La vitesse du morceau du salon (rôle d'hôte seulement). |
| `Lobby.startProblem()  -> "need_song", "need_players" or nil` | Pourquoi la manche ne peut pas encore commencer. L'état prêt n'est pas requis. |
| `Lobby.hostStart()  -> bool` | Lance la manche (rôle d'hôte seulement). |
| `Lobby.takeGo()  -> bool` | Vrai une fois quand la manche a commencé ; appeler alors `launchPlay`. |
| `Lobby.launchPlay()  -> true, "eject" or false` | Monte le morceau pour chaque joueur et ouvre la manche de jeu : sur true le skin sort vers `"play"` ; `"eject"` signifie que cette machine n'a pas le morceau et doit quitter le salon ; false vient avec `net.msg`. |
| `Lobby.returnedFromPlay()` | À appeler quand on revient dans la stage après ce jeu : envoie mon résultat et démarre les résultats. |
| `Lobby.tickResults(dt)  -> bool` | À chaque frame sur les résultats : vrai dès que chaque résultat est arrivé ou que `Lobby.RESULTS_WAIT` (20) secondes sont passées. |
| `Lobby.standings()  -> rows` | Les résultats, meilleur score d'abord : `{ id, name, r }`, `r` nil tant que ce joueur joue encore. |
| `Lobby.nextRound()` | Retour au salon : le rôle d'hôte passe au joueur suivant. |
| `Lobby.tickPreview(dt)`, `Lobby.stopPreview()` | Fait boucler l'aperçu du morceau sur les écrans du salon et des résultats ; l'arrête. |
| `Lobby.onEvent` | Une fonction optionnelle `(kind, peer, data)` appelée pour chaque événement que `drain` traite (`"joined"`, `"left"`, `"song"`, `"ready"`, `"go"`, `"result"`, ...). |

Le jeu en ligne ressemble au jeu hors ligne avec le même nombre de joueurs : les autres joueurs prennent les places locales 2 à 5 comme emplacements virtuels, avec leurs propres personnages et plaques de nom, et l'écran de jeu les fait jouer d'après le réseau.

## Le branchement de la sélection de morceaux

Le joueur qui a le rôle d'hôte choisit le morceau du salon dans la sélection de morceaux du skin. Celle-ci ne doit choisir qu'un morceau (pas d'étape de difficulté, pas d'Auto) ; ses touches de vitesse changent `CONFIG.SongSpeed`. Trois appels la branchent :

1. `Lobby.openSongSelect(open)` au lieu de l'activer directement : `open` est une fonction qui active la sélection de morceaux. La vitesse du salon est d'abord mise dans `CONFIG.SongSpeed`.
2. Quand un morceau est confirmé : désactiver la sélection de morceaux, puis `Lobby.songPicked(uid?)`. `uid` vaut par défaut `SONGMOUNT:ChosenUniqueId()`.
3. Quand on quitte la sélection de morceaux sans morceau : la désactiver, puis `Lobby.songPickCancelled()`, qui garde une vitesse changée là-bas et remet la jaquette et l'aperçu du salon.

La chaîne partagée `presound_path` nomme l'audio dans `presound` : une sélection de morceaux qui y laisse jouer le morceau du salon rend l'aperçu, et le salon le laisse continuer au lieu de le relancer.

## Messages

`net.msg` est prêt à être affiché. Un skin qui rédige lui-même ses messages lit plutôt `net.msgCode` :

| Code | Quand |
| --- | --- |
| `open_failed`, `join_failed` | NET a refusé sans dire pourquoi. |
| `room_open` | Le salon est ouvert et son code enregistré. |
| `connecting` | Une connexion a commencé. |
| `no_code`, `code_invalid` | Le code est vide, ou n'est pas un code de salon. |
| `code_other` | Le code est pour un autre type de salon ; `msgArg` est son id de stage. |
| `chart_failed` | `launchPlay` n'a pas pu monter le chart. |
| `net` | Un texte de NET lui-même (réseau désactivé, connexion perdue, ...), en anglais. |

Le réseau est désactivé tant que le joueur ne l'autorise pas dans les paramètres. Un skin devrait vérifier `CONFIG.AreLuaNetworkingConnectionsAllowed` avant d'ouvrir son salon ; NET refuse aussi avec un message `net`.

## Online.Protocol

Chaque message est une chaîne JSON sur un canal NET. L'id de salon dans le code est `"onlinelobby"` (`Protocol.ROOM_ID`), quelle que soit la stage qui l'a ouvert.

| Canal | Contenu | Envoyé |
| --- | --- | --- |
| `song` | `{"u":uid,"t":title,"d":0,"s":speed,"b":dyn}` | Par le rôle d'hôte, quand le morceau ou sa vitesse change, et à chaque nouvel arrivant. |
| `diff`, `mod`, `ready` | `{"d":n}`, `{"r","st","ju","tz","ss","sm"}`, `{"r":bool}` | Quand un joueur les change, et à chaque nouvel arrivant. |
| `have`, `watch` | `{"h":bool}`, `{"w":bool}` | Si le joueur a le morceau ; s'il est sur les résultats. |
| `go` | `{}` | La manche commence. |
| `result` | `{"n","sc","gr","gd","ms","co","cl","fc","pf","ac","ab"}` | Le résultat d'un joueur après le morceau. |

## Un salon minimal

```lua
local Lobby = require("Online.Lobby")
local net = Lobby.net
local screen, back = "menu", false

function activate()
    if back then back = false; Lobby.returnedFromPlay(); screen = "results" end
end

function update(ts)
    local dt = 1 / 60
    if net.online or net.connecting then
        Lobby.drain()
        if net.roomGone then Lobby.leave(); screen = "menu" end
    end
    if screen == "lobby" then
        Lobby.tickPreview(dt)
        Lobby.fixDiff()
        if Lobby.takeGo() then
            local r = Lobby.launchPlay()
            if r == true then back = true; return Exit("play", nil) end
            if r == "eject" then Lobby.leave(); screen = "menu" end
        end
    elseif screen == "results" then
        if Lobby.tickResults(dt) and decidePressed() then Lobby.nextRound(); screen = "lobby" end
    end
end

function onDestroy() Lobby.leave() end
```

`Lobby.host()` ou `Lobby.join(code)` depuis le menu du skin passent `screen` à `"lobby"` ; le branchement de la sélection de morceaux ci-dessus définit le morceau.
