<!-- modules/pagoda.md -->

# Pagoda

La Pagode de l'Inconnu est une ascension d'étages de dan : chaque étage tire trois chansons de ses pools (et parfois une quatrième, violette), avec trois examens qui se durcissent à mesure que les étages montent. Les modules `Pagoda` contiennent les règles, les données et les clés de sauvegarde ; ils ne dessinent rien. Un skin dessine ses propres écrans de pagode par-dessus, et tous les skins partagent une même progression, parce que la progression vit dans le fichier de sauvegarde. Le stage `pagoda` d'Open-World Memories est l'un de ces clients.

Les étages 1 à 10 s'appellent 道一 à 道十, les étages 11 à 20 扉一 à 扉十, et à partir de 21 viennent les toits 穹天1, 穹天2, et ainsi de suite. Ces noms sont le titre du dan, identique dans toutes les langues.

## Les modules

| Module | Ce qu'il contient |
| --- | --- |
| `Pagoda.Levels` | Tout ce qui découle d'un numéro d'étage : les pools de chansons, le nom, le cadre et la couleur de la plaque de dan, les seuils des examens, la probabilité d'une chanson violette. De pures consultations. |
| `Pagoda.Floor` | Les chansons d'un étage (un tirage) et son dan : transmet un tirage à `DANBUILDER` avec la plaque et les examens. |
| `Pagoda.Rewards` | Ce que rapporte une réussite : sa plaque de nom, des pièces, des meubles de Ma Chambre, des clés de la chambre forte ; les enregistre et affiche les pop-ups via la ROActivity `modal`. |
| `Pagoda.Progress` | Les données de sauvegarde persistantes (ci-dessous). |
| `Pagoda.Songs` | La liste de chansons dans laquelle puise la pagode, et la vérification que la bande-son couvre chaque entrée des pools. |
| `Pagoda.Run` | Facultatif : le déroulement d'une ascension en mode défi et de l'entraînement, sans écran. |
| `Pagoda.Texts` | Les chaînes propres aux modules (`lang/<code>/pagoda.json`). |

## Pagoda.Levels

| Fonction | Description |
| --- | --- |
| `Levels.get_pool(level)  -> pool` | Le pool de l'étage, des listes `{ blue, green, red, purple }` de `{ id, diff }` (l'identifiant unique d'une chanson et une difficulté, de 0 à 4). Un étage sans entrée propre utilise la plus proche en dessous. |
| `Levels.load_pools()`, `Levels.clear_pools()` | Lit `pools.json` (une fois) ; l'oublie, après une nouvelle énumération des chansons. |
| `Levels.level_name(level)  -> string` | Le nom de l'étage. |
| `Levels.level_tick(level)`, `Levels.level_tick_color(level)` | Le cadre (de 0 à 4) et la couleur `{ r, g, b }` de la plaque de dan. |
| `Levels.exam1_gauge(level)  -> red, gold` | L'examen de jauge. |
| `Levels.exam2(level)  -> type, red, gold, lessThan` | L'examen de précision (étages 1 à 20), ou le nombre de Ok (à partir de 21). |
| `Levels.exam3_base(level)  -> red, gold` | L'examen du nombre de Bad avant l'indulgence due à la chanson violette. |
| `Levels.purple_prob(level)  -> number` | La probabilité d'une quatrième chanson violette (l'étage 20 en a toujours une). |
| `Levels.pick_entry(entries, used)`, `Levels.shuffle(t)` | Les utilitaires qu'utilisent les tirages. |

`pools.json`, à côté des modules, associe à chaque numéro d'étage son pool :

```json
{
  "1": {
    "comment": "一題",
    "blue":   [ { "id": "<song unique id>", "diff": 0 } ],
    "green":  [ { "id": "<song unique id>", "diff": 0 } ],
    "red":    [ { "id": "<song unique id>", "diff": 0 } ],
    "purple": [ { "id": "<song unique id>", "diff": 0 } ]
  }
}
```

Les pools désignent des chansons de la bande-son d'OpenTaiko. `Songs.count_missing_songs()` journalise et compte les entrées qui manquent aux chansons installées (une chanson, ou sa difficulté), pour qu'un écran puisse avertir le joueur.

## Pagoda.Floor

| Fonction | Description |
| --- | --- |
| `Floor.rollFloor(level, avoid?, songList?)  -> roll` | Une chanson bleue, une verte et une rouge dans un ordre mélangé, puis une chanson violette selon la probabilité de l'étage, jamais deux fois la même chanson. `avoid` est un ensemble d'identifiants de chansons à ne pas tirer tant que les pools en ont d'autres. Un tirage est `{ songs = { { color, node, entry }, ... }, purple = { node, entry } or nil, speed }`. |
| `Floor.rerollFloor(level, roll, keepMystery, songList?)  -> roll` | Tire de nouveau, avec d'autres chansons quand les pools le permettent ; `keepMystery` garde la chanson violette. |
| `Floor.exams(level, hadPurple)  -> table` | Les trois examens sous la forme `{ type, red, gold, lessThan }`. |
| `Floor.buildDan(level, roll?, opts?)  -> bool` | Prépare `DANBUILDER` pour l'étage : titre, plaque, genre (`opts.genre`, `opts.genreColor`), les chansons et les examens. False quand aucune chanson n'a pu être ajoutée ; sinon montez-le avec `DANBUILDER:Mount()` et jouez. |
| `Floor.speed(level)  -> number` | La vitesse des chansons de l'étage (une valeur de `CONFIG.SONGSPEED`) : x1 en dessous de 穹天18, puis croissante. |

## Pagoda.Run

Une ascension en mode défi commence sur l'un des étages de départ ouverts, monte d'un étage par réussite et se termine au premier échec. L'entraînement joue n'importe quel étage jusqu'au plus haut qu'une ascension a atteint (au moins 11) et n'enregistre rien.

| Fonction | Description |
| --- | --- |
| `Run.startOptions()  -> table` | Les étages de départ ouverts sur cette sauvegarde : 1, 6 et 11 toujours, 16 après avoir réussi le 15, 21 après avoir réussi le 20. |
| `Run.new(start, songList?)  -> run` | Une ascension depuis cet étage. Ses champs : `level` (l'étage actuel), `tower` (l'étage le plus haut affiché comme réussi), `roll`, `over`. |
| `run:rollFloor(avoid?)`, `run:reroll(keepMystery)  -> roll` | Les chansons de l'étage actuel, à montrer avant la partie. |
| `run:enter()` | L'ascension a atteint son étage : l'entraînement le propose désormais. |
| `run:buildDan(opts?)  -> bool` | `DANBUILDER` préparé pour l'étage actuel ; montez-le et jouez. |
| `run:clear()  -> result` | Après une réussite : accorde et enregistre les récompenses, relève l'étage le plus haut, monte. Renvoie `{ level, rewards, shrouded }` : `rewards` est la liste que renvoie `Rewards.grant`, `shrouded` indique si l'étage se trouvait au-dessus de la tour. |
| `run:fail()` | Après un échec : retient l'étage, compte la partie, termine l'ascension. |
| `Run.practiceLevels()  -> table`, `Run.practiceDan(level, opts?)  -> bool` | Les étages que propose l'entraînement ; `DANBUILDER` préparé pour une partie d'entraînement avec un nouveau tirage. |

## Pagoda.Rewards

| Fonction | Description |
| --- | --- |
| `Rewards.grant(level)  -> table` | Accorde et enregistre les récompenses d'une réussite sur la sauvegarde du joueur 1, et renvoie les pop-ups à afficher dans l'ordre : `{ kind = "nameplate", id }`, `{ kind = "furniture", id }`, `{ kind = "key", key, count }`, `{ kind = "coins", amount }`. |
| `Rewards.setup()` | Partage une fois les images des clés (`pagoda_reward_key_<id>`) ; appelez-la avant d'afficher un pop-up de clé. |
| `Rewards.show(r)  -> bool` | Lance le pop-up d'une récompense via la ROActivity `modal`. |
| `Rewards.update()  -> bool`, `Rewards.draw()`, `Rewards.stop()` | Fait tourner le pop-up (true tant qu'il joue ; il lit ses propres entrées), le dessine, l'abandonne. |
| `Rewards.poolOf(level)`, `Rewards.nameplateOf(level)` | Le pool de récompenses d'un étage, et la plaque de nom que donne sa première réussite. |

La première réussite d'un étage donne sa plaque de nom (identifiants 381 à 397, sur les étages listés dans `Rewards.NAMEPLATES`). Ensuite, les pièces (une fourchette, avec 50 % de chances) et chaque entrée du pool de l'étage sont tirées chacune selon sa propre probabilité (`Rewards.POOLS`, `Rewards.ROOF_POOLS`), si bien qu'une réussite peut rapporter plusieurs choses. Les clés de la chambre forte se changent en pièces tant que la chambre forte secrète est fermée, et toutes les pièces d'une réussite tiennent en un seul pop-up. Un pop-up de meuble affiche la texture partagée `myroom_thumb_<id>` que fournit [Ma Chambre](my-room.md).

## Clés de sauvegarde

Tout est conservé dans la sauvegarde du joueur 1 (`GetSaveFile(0)`) :

| Clé | Nature | Contenu |
| --- | --- | --- |
| `pagoda_highest_level` | compteur | L'étage le plus haut réussi lors d'une ascension en mode défi. |
| `pagoda_highest_reached` | compteur | L'étage le plus haut qu'une ascension a atteint, jusqu'auquel l'entraînement propose des étages. |
| `pagoda_plays` | compteur | Les ascensions en mode défi jouées (comptées à l'échec). |
| `pagoda_lost_<level>` | déclencheur | Une ascension a échoué à cet étage. |
| `pagoda_story_first_seen` | déclencheur | L'histoire d'ouverture a été jouée (une sauvegarde plus ancienne peut contenir à la place le compteur `pagoda_story_seen`). |
| `pagoda_held_mods`, `pagoda_held_mods_lo`, `pagoda_held_mods_hi` | déclencheur, compteurs | Les indicateurs de mods du joueur 1 retenus pendant une partie de la pagode, en deux moitiés de 32 bits (`Progress.hold_mods(flags)`, `held_mods()`, `drop_held_mods()`). |
| `pagoda_structure_version` | compteur | La renumérotation unique des anciennes sauvegardes (`Progress.migrate_save()`) a eu lieu. |
| `.myroom_<id>`, `.myroom_held_<id>` | compteurs | Les meubles gagnés : transmis à Ma Chambre, et possédés. |
| `.vault_key_count_<key>` | compteurs | Les clés de la chambre forte gagnées, via [Vault.Keys](vault.md). |

Les plaques de nom et les pièces passent par les méthodes propres à la sauvegarde (`UnlockNameplate`, `EarnCoins`).

```lua
local Run     = require("Pagoda.Run")
local Rewards = require("Pagoda.Rewards")
local run = Run.new(Run.startOptions()[1])

run:rollFloor()                  -- show run.roll's songs
run:enter()
if run:buildDan() and DANBUILDER:Mount() then return Exit("play") end

-- back from the play
local r = run:clear()            -- or run:fail()
Rewards.setup()
for _, reward in ipairs(r.rewards) do queue[#queue + 1] = reward end   -- then Rewards.show / update / draw
```
