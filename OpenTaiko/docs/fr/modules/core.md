<!-- modules/core.md -->

# Core

De petits utilitaires sans écran propre : courbes d'easing, calculs de couleurs, outils pour les tables, un encodeur et décodeur JSON, et le dossier du fichier courant. Ils ne touchent à aucun état du moteur au chargement, si bien que tout module, y compris en lecture seule, peut les charger.

## Core.Easing

Des courbes d'easing sur un rapport de 0 à 1, pour des animations pilotées par votre propre horloge (une timeline, l'âge d'une particule). Pour une interpolation cadencée par le moteur, la globale `COUNTER` avec SetEasing est l'autre option. Chaque courbe borne son entrée à 0..1 et envoie 0 sur 0 et 1 sur 1, sauf `hump`.

| Fonction | Description |
| --- | --- |
| `Easing.linear(t)  -> number` | Le rapport borné lui-même. |
| `Easing.inQuad(t)`, `outQuad`, `inOutQuad`, `inCubic`, `outCubic`, `inOutCubic`, `inSine`, `outSine`, `inOutSine`, `inExpo`, `outExpo`  `-> number` | Les courbes habituelles, par leur nom. |
| `Easing.inBack(t, s?)`, `Easing.outBack(t, s?)  -> number` | Des courbes qui dépassent leur cible ; `s` est l'amplitude du dépassement (1.7 par défaut). |
| `Easing.inBounce(t)`, `Easing.outBounce(t)`, `Easing.outElastic(t)  -> number` | Des courbes à rebond et élastiques. |
| `Easing.hump(t)  -> number` | 0, monte jusqu'à 1 au milieu, revient à 0 : pour les écrasements et les éclairs. |
| `Easing.span(t, t0, dur)  -> number` | La progression de la fenêtre qui commence à `t0` et dure `dur`, bornée à 0..1. |
| `Easing.lerp(a, b, k)  -> number` | `a + (b - a) * k`. |
| `Easing.clamp01(t)  -> number` | `t` borné à 0..1. |
| `Easing.get(kind, fn)  -> function` | Une courbe désignée par les noms d'easing du moteur : `kind` vaut `"IN"`, `"OUT"` ou `"INOUT"`, `fn` vaut `"LINEAR"`, `"SINE"`, `"QUAD"`, `"CUBIC"`, `"EXPO"`, `"BACK"`, `"BOUNCE"` ou `"ELASTIC"`. Une paire sans courbe donne la courbe QUAD. |

```lua
local Easing = require("Core.Easing")
local k = Easing.outBack(Easing.span(t, startTime, 0.4))
x = Easing.lerp(x0, x1, k)
```

## Core.Color

Conversions et ajustements de couleurs. Chaque canal est un flottant de 0 à 1, teinte comprise (de 0 à 1 sur un tour du cercle chromatique) ; les fonctions prennent et renvoient des valeurs `r, g, b` séparées.

| Fonction | Description |
| --- | --- |
| `Color.rgbToHsv(r, g, b)  -> h, s, v` | RGB vers HSV. |
| `Color.hsvToRgb(h, s, v)  -> r, g, b` | HSV vers RGB. |
| `Color.rgbToHsl(r, g, b)  -> h, s, l` | RGB vers HSL. |
| `Color.hslToRgb(h, s, l)  -> r, g, b` | HSL vers RGB. |
| `Color.mix(r, g, b, r2, g2, b2, t)  -> r, g, b` | Mélange linéaire : `t` = 0 garde la première couleur, 1 donne la seconde. |
| `Color.lighten(r, g, b, amount)  -> r, g, b` | Déplace la luminosité HSL vers le blanc de `amount` de l'écart restant (1 = blanc) ; la teinte et la saturation restent. |
| `Color.darken(r, g, b, amount)  -> r, g, b` | Déplace la luminosité HSL vers le noir de `amount` (1 = noir). |
| `Color.saturate(r, g, b, factor, add?)  -> r, g, b` | Multiplie la saturation HSV par `factor`, ajoute `add` et borne à 1. |
| `Color.brighten(r, g, b, factor)  -> r, g, b` | Multiplie la valeur HSV par `factor`, bornée à 1. |
| `Color.ramp(from, to, n)  -> table` | `n` couleurs en dégradé de `{r, g, b}` à `{r, g, b}`, sous forme de liste de `{r, g, b}`. |
| `Color.fromEngine(col)  -> r, g, b, a` | Les canaux d'un objet `COLOR`, en flottants. |
| `Color.toEngine(r, g, b, a?)  -> color` | Un objet `COLOR` à partir de flottants (alpha 1 par défaut). |
| `Color.rampEngine(fromCol, toCol, n)  -> table` | Un dégradé de `n` objets `COLOR` entre deux objets `COLOR`. |
| `Color.clamp01(v)  -> number` | `v` borné à 0..1. |

## Core.Util

| Fonction | Description |
| --- | --- |
| `Util.clamp(v, lo, hi)  -> number` | `v` maintenu entre `lo` et `hi`. |
| `Util.lerp(a, b, t)  -> number` | Interpolation linéaire. |
| `Util.round(v)  -> number` | Arrondit à l'entier le plus proche (les moitiés vers le haut). |
| `Util.deepcopy(o)  -> value` | Une copie profonde d'une table Lua, métatables et cycles compris. |
| `Util.merge(base, over)  -> table` | Une copie de `base` dans laquelle `over` est fusionné : les sous-tables fusionnent récursivement, les autres valeurs sont remplacées. |
| `Util.cloneTable(dict)  -> table` | Un dictionnaire .NET (un objet JSON analysé, un ensemble de lignes SQL) copié récursivement dans une table Lua. |

## Core.Json

Du texte JSON à partir de valeurs Lua et inversement, en Lua pur. Contrairement à `JSONLOADER`, qui analyse des fichiers en dictionnaires .NET, il travaille sur des tables Lua, ce qui le rend adapté aux données que vous gardez dans une chaîne de sauvegarde (`sf:SetGlobalString`) ou dans une valeur de base de données.

| Fonction | Description |
| --- | --- |
| `Json.encode(value)  -> string` | Une table aux clés 1..n est écrite comme un tableau, toute autre table comme un objet aux clés triées, si bien que la même valeur donne toujours le même texte. Les fonctions et les userdata sont omises des objets ; les nombres entiers s'écrivent sans point décimal. Une table qui se contient elle-même lève une erreur. |
| `Json.decode(text)  -> value` ou `nil, message` | Analyse du texte JSON. Le `null` JSON devient `Json.null` ; en cas d'erreur de syntaxe, renvoie nil et un message indiquant la position. |
| `Json.null` | La valeur qui représente le `null` JSON ; elle se réencode en `null`. |

```lua
local Json = require("Core.Json")
local sf = GetSaveFile(0)
local data = Json.decode(sf:GetGlobalString("my_feature")) or { v = 1, items = {} }
data.items[#data.items + 1] = "lamp"
sf:SetGlobalString("my_feature", Json.encode(data))
```

## Core.Here

Une fonction qui renvoie le dossier du fichier qui l'appelle, à partir des arguments que `require` passe à ce fichier. C'est ainsi qu'un fichier de Lib trouve ses propres ressources (voir [Les ressources se résolvent par rapport à l'appelant](README.md)).

| Fonction | Description |
| --- | --- |
| `require("Core.Here")(...)  -> string` | Le dossier absolu du fichier, avec des barres obliques et un `/` final ; nil quand le fichier n'a pas été chargé via `require`. Appelez-la au niveau supérieur du fichier, là où `...` contient le nom du module et le chemin du fichier. |
