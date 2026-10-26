<!-- guides/notes-skins.md -->

# Ajouter des packs de notes et de jugements

Un skin peut proposer plusieurs looks pour les notes et pour les textes de jugement, et le joueur en choisit un dans les paramètres. Chaque look est un pack : un dossier numéroté sous `Graphics/5_Game/` du skin. Un pack de notes contient les notes, les roulements, les ballons, les effets de frappe et les bulles de ballon et de roulement ; un pack de jugements contient les textes de jugement. Ce guide explique les dossiers, les planches de chaque pack, le fichier `Definition.json` qui décrit un pack, et comment le jeu complète ce qui manque à un pack.

## Avant de commencer

- Un skin à vous (voir [Ajouter des skins et des thèmes](skins.md)). Le skin fourni `System/Open-World Memories/` contient quatre packs de notes Taiko, un pack de notes Bongo et quatre packs de jugements à copier.
- Un éditeur d'images qui enregistre des fichiers PNG avec transparence.
- La taille de cellule des notes de votre skin : la valeur de disposition `Game_Notes_Size=` (`195,195` dans le skin fourni). Chaque cellule des planches de notes de cette page a cette taille, sauf celles de la planche géante.

## Étape 1 : Créer les dossiers des packs

Le jeu cherche trois dossiers sous `Graphics/5_Game/` et traite chaque sous-dossier numéroté qu'ils contiennent comme un pack, trié par numéro :

- `Notes Skin (Taiko)/` : packs de notes pour les joueurs Taiko.
- `Notes Skin (Bongo)/` : packs de notes pour les joueurs Bongo.
- `Judges Skin/` : packs de textes de jugement, pour tous les joueurs.

Le pack `1` de chaque dossier est le pack par défaut et la base des autres (étape 7). Le numéro du dossier ne fait qu'ordonner les packs ; les paramètres affichent le nom tiré de `Definition.json`.

```
Graphics/5_Game/
  Notes Skin (Taiko)/
    1/                  <- le look par défaut, complet
      Definition.json
      Notes.png
      ...
    2/                  <- un autre look : seulement les fichiers qui diffèrent
      Definition.json
      Notes.png
  Notes Skin (Bongo)/
    1/
  Judges Skin/
    1/
      Definition.json
      Judge.png
```

## Étape 2 : Dessiner les planches de notes Taiko

Une planche a une ligne par image d'animation (l'étape 5 fixe le nombre d'images), et ses cellules sont placées côte à côte dans l'ordre ci-dessous.

| Fichier | Contenu |
| --- | --- |
| `HitCircle.png` | Le cercle de frappe, une cellule. |
| `Notes.png` | Don, Ka, grand Don, grand Ka, KaDon, ADLib. |
| `Rolls.png` | La tête, le corps et la queue du petit roulement, puis la tête, le corps et la queue du grand roulement. Le jeu étire la cellule du corps sur toute la longueur du roulement. |
| `Balloons.png` | Le ballon (2 cellules de large), le kusudama (2 cellules de large), puis la tête, le corps et la queue du roulement à mèche. Les joueurs Bongo voient aussi ces ballons. |
| `ChipEffect.png` | Facultatif. L'éclair affiché là où arrive une note frappée, avec les colonnes de `Notes.png`, une ligne. Sans ce fichier, le jeu fait clignoter la note elle-même. |
| `Notes_Partner.png` | Facultatif. Le look partenaire de chaque colonne de `Notes.png` (étape 6). |
| `Notes_Giant.png` | Facultatif. Les Don, Ka, KaDon et ADLib géants (étape 6). |
| `Preview.png` | Facultatif. L'image que les paramètres affichent pour le pack. Sans ce fichier, ils affichent `Notes.png`. |

Le pack contient aussi les effets de frappe et les bulles de ballon et de roulement :

| Fichier | Contenu |
| --- | --- |
| `Hit/Explosion.png` | L'explosion au-dessus du cercle de frappe, 7 images carrées par ligne : une ligne pour Bon, une ligne pour Ok, puis, en option, une ligne pour Bon et une ligne pour Ok sur les grandes notes. Avec 2 lignes, les grandes notes utilisent les deux premières. |
| `Hit/Explosion_Big.png` | L'explosion supplémentaire au-dessus d'une grande note, une seule image, dessinée pour les deux jugements. |
| `Hit/FireWorks.png` | Les étincelles qu'une grande note en vol laisse derrière elle. |
| `Hit/Bomb.png` | L'explosion d'une bombe frappée ou d'un roulement à mèche raté, 7 images sur une ligne. |
| `Hit/Great/`, `Hit/Great_Big/`, `Hit/Good/`, `Hit/Good_Big/` | Dossiers d'images facultatifs (`0.png` à `14.png`) que le jeu dessine sur la piste pour un Bon (`Great`) ou un Ok (`Good`) sur une petite ou une grande note. |
| `Kusudama/` | Le `Script.lua` du kusudama et ses images, dont `Kusudama_Number.png`. |
| `Fuseroll/` | `Fuse.png`, la bulle du roulement à mèche, et `Number_Fuse.png`, ses chiffres. |
| `Breaking.png` | Les images d'éclatement de la bulle de ballon : 6 images de même hauteur, l'une au-dessus de l'autre. |
| `Balloon.png` | La bulle de ballon. |
| `Roll.png` | La bulle du compteur de roulement, ses images d'animation côte à côte. |

## Étape 3 : Dessiner un pack de notes Bongo

Un pack Bongo ne contient que ce qui diffère pour les joueurs Bongo :

| Fichier | Contenu |
| --- | --- |
| `Notes.png` | Rouge, jaune, rose, clap, ADLib. |
| `Rolls.png` | Les roulements rouge, rose, jaune et clap, chacun en tête, corps et queue. |
| `HitCircle.png` | Facultatif. Sans ce fichier, les joueurs Bongo voient le cercle de frappe du pack Taiko. |
| `ChipEffect.png`, `Notes_Partner.png` | Facultatifs, avec les colonnes de ce `Notes.png`. |
| `Notes_Giant.png` | Facultatif, avec les colonnes de ce `Notes.png`. |
| `Preview.png` | Facultatif. |

Les ballons, les effets de frappe et les bulles viennent toujours du pack Taiko sélectionné.

## Étape 4 : Dessiner un pack de jugements

Un pack de jugements contient `Judge.png`, 5 lignes de même hauteur : Bon, Ok, Mauvais, ADLib et le Boom de la bombe. Son `Definition.json` n'a besoin que d'un `Name`. Laissez une ligne transparente pour masquer ce jugement ; les packs « (Without Good) » du skin fourni le font pour la ligne Bon.

## Étape 5 : Écrire Definition.json

`Definition.json` nomme le pack et décrit ses planches. Chaque champ est facultatif ; un pack sans ce fichier affiche son numéro de dossier comme nom et utilise les valeurs par défaut ci-dessous.

```json
{
	"Name": { "strings": { "default": "Sky Spirits", "ja": "スカイスピリッツ" } },
	"Notes": {
		"Frames": 3,
		"Animation": [
			{ "FromCombo": 0, "Rows": [0] },
			{ "FromCombo": 50, "Rows": [1, 1, 0, 0], "RowsOnEntry": [0] },
			{ "FromCombo": 150, "Rows": [0, 1] },
			{ "FromCombo": 300, "Rows": [0, 2], "RowsOnEntry": [0, 1] }
		]
	},
	"Rolls": { "Frames": 3, "Animation": [] },
	"Balloons": { "Frames": 3, "Animation": [] },
	"FlyingNotesFrame": 1,
	"PartnerNotesScale": 1.4142,
	"GiantNotesScale": 2.0
}
```

| Champ | Par défaut | Signification |
| --- | --- | --- |
| `Name` | le numéro du dossier | Le nom dans les paramètres, une chaîne par code de langue, avec `default` pour toutes les autres langues. |
| `Notes`, `Rolls`, `Balloons` | 1 image, pas d'animation | Le nombre de lignes de la planche (`Frames`) et son animation (`Animation`). `Notes` fixe aussi les lignes de `Notes_Partner.png` et de `Notes_Giant.png`. |
| `FlyingNotesFrame` | `0` | La ligne de `Notes.png` des notes qui volent vers la jauge après une frappe. |
| `PartnerNotesScale` | la racine carrée de 2 | L'échelle d'une petite note partenaire quand le pack n'a pas de `Notes_Partner.png` (étape 6). |
| `GiantNotesScale` | `2` | L'échelle d'une note géante quand le pack n'a pas de `Notes_Giant.png` (étape 6). |

`Animation` est une liste de paliers. Le jeu utilise le palier au `FromCombo` le plus élevé que le combo du joueur a atteint. Un temps compte 4 subdivisions, et `Rows` donne la ligne dessinée à chaque subdivision ; une liste plus courte se répète, donc `[0, 1]` alterne à chaque subdivision et `[0]` reste fixe. `RowsOnEntry` remplace `Rows` pendant environ deux tiers de seconde après que le combo a atteint ce palier. Sans paliers, la planche affiche sa première ligne. Le tableau de l'exemple est l'animation que le jeu a toujours utilisée, et le Mode simple des paramètres garde chaque planche sur sa première ligne.

## Étape 6 : Notes partenaires et notes géantes

Une note partenaire garde sa taille dans la plupart des cas, mais les petites (un petit Don ou Ka, un ADLib, une bombe, toutes les notes Bongo) prennent un look de grande note. Une note géante est bien plus grande et oscille lentement. Chacune a sa propre planche facultative :

- `Notes_Partner.png` a les colonnes de `Notes.png` et autant de lignes. Une petite note partenaire utilise la cellule de sa propre colonne.
- `Notes_Giant.png` a autant de lignes que `Notes.png`. Dans un pack Taiko, ses colonnes sont Don, Ka, KaDon et ADLib, car une note géante paraît grande quelle que soit sa taille : un petit Don et un grand Don utilisent tous deux la cellule Don. Dans un pack Bongo, ses colonnes sont celles de `Notes.png`. La taille de cellule est la largeur de la planche divisée par son nombre de colonnes, donc une cellule géante peut être plus grande qu'une cellule de note.

Sans ces planches, le jeu construit les looks à partir de `Notes.png` :

| Note | Partenaire, sans `Notes_Partner.png` | Géante, sans `Notes_Giant.png` |
| --- | --- | --- |
| Petit Don ou Ka Taiko | Le grand Don ou Ka | Le grand Don ou Ka, multiplié par `GiantNotesScale` |
| Toute autre petite note | La note, multipliée par `PartnerNotesScale` | La note, multipliée par `PartnerNotesScale` et `GiantNotesScale` |
| Grand Don, grand Ka, KaDon | Inchangée | La note, multipliée par `GiantNotesScale` |

Définissez les deux échelles même quand le pack a les deux planches : un créateur de skin qui copie le pack et retire une planche obtient alors une taille cohérente. Une note mise à l'échelle est une image étirée et paraît plus floue qu'une cellule dessinée à sa taille.

<div class="callout warn">Le jeu lit Notes_Partner.png, Notes_Giant.png et ChipEffect.png uniquement dans le pack sélectionné. Ils doivent correspondre aux notes propres du pack, donc les copies du pack 1 ne les remplacent jamais.</div>

## Étape 7 : Laisser le pack 1 compléter le reste

Quand il manque un fichier au pack sélectionné, le jeu le prend dans le pack 1 du même dossier. Un pack peut donc ne contenir que ce qu'il change : un pack qui vide seulement une ligne de `Hit/Explosion.png` contient un `Definition.json` et ce seul fichier. Les règles :

- Le jeu vérifie chaque fichier séparément, y compris dans les dossiers (`Hit/Great/3.png`). Le kusudama fait exception : un pack sans son propre dossier `Kusudama/` utilise tout le dossier du pack 1.
- Le repli ne remonte qu'au pack 1. Un pack qui décline le pack 3 doit contenir ses propres copies des planches du pack 3.
- `Definition.json` n'est jamais pris dans le pack 1, pas plus que les trois planches de l'étape 6.
- Un pack Bongo se replie sur le pack Bongo 1.

## Étape 8 : Choisir un pack

Quand un skin a au moins 2 packs dans l'un des trois dossiers, la page du thème des paramètres affiche en premier une section Skins des notes, avec un choix pour ce dossier et l'aperçu du pack en dessous. Le jeu charge aussitôt le pack choisi et retient le choix pour le skin dans `ThemeSettings.db3`. Avec un seul pack, le choix n'apparaît pas et le jeu utilise ce pack.

Un module Lua lit un fichier du pack sélectionné avec `THEME:GetNotesSkinFile(kind, file)` (voir [Joueurs et profils](../api/players.md)). La sélection des chansons fournie l'utilise pour dessiner les textes de jugement de sa liste de replays :

```lua
local file = THEME:GetNotesSkinFile("judges", "Judge.png")   -- "5_Game/Judges Skin/2/Judge.png", ou ""
if file == "" then file = "5_Game/Judge.png" end              -- un skin sans packs de jugements
```

## Skins sans packs

Un skin sans dossier `Notes Skin (Taiko)/` garde les fichiers qu'il avait avant l'arrivée des packs : `Notes.png` (le cercle de frappe, les 4 notes, les roulements et le ballon), `Swap.png` (KaDon), `Adlib.png`, `Kusu.png`, `FuseRoll.png`, `ChipEffect.png`, le dossier `10_Effects/Hit/` et les fichiers de ballon de `11_Balloon/`. Sans dossier `Notes Skin (Bongo)/`, les notes et roulements Bongo viennent de `Notes_Konga.png`, et sans dossier `Judges Skin/`, les jugements viennent de `5_Game/Judge.png`. Ces fichiers n'ont pas de planches partenaires ni géantes, donc les notes partenaires et géantes utilisent les looks mis à l'échelle de l'étape 6.

## Dépannage et remarques

- Les paramètres n'affichent pas le choix : le dossier contient un seul pack, ou les dossiers des packs ne portent pas des numéros comme noms.
- Un pack affiche les notes d'un autre pack : le repli a pris le fichier du pack 1 parce qu'il manque au pack, ou la casse du nom de fichier diffère sur un système sensible à la casse.
- Les notes affichent la mauvaise image ou une image coupée : `Frames` ne correspond pas à la hauteur de la planche divisée par la hauteur de cellule.
- Les notes partenaires ou géantes d'un pack copié ressemblent aux anciennes : la copie a gardé le `Notes_Partner.png` ou le `Notes_Giant.png` de l'original.
- Les joueurs Bongo ne voient aucune note : `Notes.png` manque à la fois dans le pack Bongo sélectionné et dans le pack Bongo 1.
