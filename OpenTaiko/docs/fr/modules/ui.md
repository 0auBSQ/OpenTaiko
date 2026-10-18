<!-- modules/ui.md -->

# UI

Une bibliothèque d'interface pour les menus et les boîtes de dialogue, pilotée à la souris, au clavier et à la manette avec une seule surbrillance partagée. `UI.SimpleUI` est la bibliothèque minimale que livre OpenTaiko ; un skin peut lui donner sa propre apparence avec `derive`, comme le fait Open-World Memories avec PopUI. Les modules globaux obtiennent leur bibliothèque de `UI.Pick`, qui donne celle que le skin a choisie (ou celle que le stage désigne), si bien que tous les écrans d'un skin se ressemblent.

## UI.Pick

`require("UI.Pick")` renvoie une fonction ; l'appeler renvoie une table de bibliothèque d'interface.

| Appel | Bibliothèque |
| --- | --- |
| `Pick(name)` | La bibliothèque portant ce nom de require (`"PopUI"`, `"UI.SimpleUI"`, ...), pour un stage qui en veut une en particulier. |
| `Pick()` | La bibliothèque désignée par la chaîne partagée `ui_library`, qu'un skin définit une fois (le `_boot` d'Open-World Memories définit `"PopUI"`). |
| `Pick()` sans `ui_library` | `UI.SimpleUI`. |

Un nom qui ne se charge pas, ou qui charge quelque chose sans fonction `new`, donne aussi `UI.SimpleUI`, si bien qu'un nom laissé dans la chaîne partagée par un autre skin ne coûte rien. Le choix n'est que du Lua et des chaînes partagées : le moteur n'en sait rien.

```lua
-- in a skin's _boot onStart: the library every global module will use
SHARED:SetSharedString("ui_library", "MyUI")

-- in any module
local UI = require("UI.Pick")()
local ui = UI.new{ navPlayer = 1 }
```

## UI.SimpleUI

`UI.new(opts)` crée un gestionnaire, qui possède les widgets, leur distribue les entrées et les dessine. Appelez `ui:update(ts)` et `ui:draw()` à chaque frame.

| Champ de `opts` | Description |
| --- | --- |
| `theme` | Une table de thème fusionnée par-dessus l'apparence par défaut de la bibliothèque (voir Thèmes). |
| `sfx` | Des sons par nom, fusionnés par-dessus ceux par défaut (voir Sons). |
| `bg` | true dessine d'abord la couleur `bg` du thème sur tout l'écran. |
| `navPlayer` | Le joueur dont le tambour navigue aussi, sous la forme que prend `NavInput.getPn` ; nil pour tous les joueurs. |
| `nav` | `"spatial"` : les flèches vont au widget le plus proche dans leur direction (une grille). Par défaut, Haut et Bas parcourent les widgets dans l'ordre de leur création. |

| Méthode | Description |
| --- | --- |
| `ui:update(ts)  -> "cancel" or nil` | Exécute les entrées et les animations pour une frame ; renvoie `"cancel"` quand Cancel a été pressé, pour que le stage réagisse. |
| `ui:draw()` | Dessine chaque widget visible, dans l'ordre de leur création. |
| `ui:button(o)`, `ui:label(o)`, ...  `-> widget` | Crée un widget, l'ajoute et le renvoie (voir Widgets). |
| `ui:add(w)`, `ui:remove(w)` | Ajoute ou retire un widget créé à la main. |
| `ui:clear()` | Retire tous les widgets et libère leurs canevas. |
| `ui:disposeWidgets()` | Libère les canevas des widgets. Appelez-la avant d'abandonner un gestionnaire : les canevas ne se libèrent pas d'eux-mêmes. |
| `ui:focus(w)  -> bool`, `ui:focused()  -> widget` | Donne le focus clavier à un widget ; le widget qui le détient. |
| `ui:focusNext()`, `ui:focusPrev()` | Déplace le focus clavier d'un cran. |
| `ui:setTheme(t)`, `ui:setSfx(t)` | Remplace le thème ou les sons du gestionnaire ; chaque widget se redessine dans le nouveau style. |
| `ui:isCapturing()  -> bool` | True tant qu'une zone de texte capte les touches : ignorez alors vos propres raccourcis. |
| `ui:mouseActive()  -> bool` | True tant que la souris est le dernier périphérique utilisé. |
| `ui:drawText(size, str, x, y, color, opacity?, scale?, maxWidth?)` | Dessine du texte avec la police de glyphes de l'interface, ancré en haut à gauche. |
| `ui:drawTextEx(size, str, x, y, fg, bg, opacity, scale, maxWidth, anchor)` | La même chose avec une couleur de contour et l'un des neuf noms d'ancrage. |
| `ui:measureText(size, str)`, `ui:wrapLines(size, str, maxWidth)`, `ui:ellipsize(size, str, maxWidth)` | Mesure, retour à la ligne et raccourcissement de texte avec la même police. |
| `ui:textNudge(size)  -> number` | Le décalage qui centre une ligne de texte sur une ligne médiane. |
| `UI.flushSharedFonts()` | Abandonne les polices en cache ; appelez-la quand la langue (et donc la police) change. |

```lua
local UI = require("UI.Pick")()
local ui

function activate()
    ui = UI.new{ navPlayer = 1 }
    ui:panel{ x = 560, y = 240, w = 800, h = 600, title = "Options" }
    ui:toggle{ text = "Hints", x = 620, y = 360, value = true, onChange = function(v) hints = v end }
    ui:button{ text = "Back", x = 860, y = 720, onClick = function() leaving = true end }
end

function update(ts)
    if ui:update(ts) == "cancel" or leaving then return Exit("stage", "_title") end
end

function draw() ui:draw() end

function deactivate() ui:disposeWidgets() end
```

## Widgets

Chaque widget prend `x`, `y`, `w` et `h`, plus `enabled`, `visible`, `focusable`, `mouseOnly` (survolé et cliqué, mais les touches le sautent), `style` ou `theme` (des champs de thème pour ce widget seulement), `sfx` et `disabledLook = "fade"`. Les options deviennent les champs du widget ; modifiez-les avec les méthodes (`setEnabled(b)`, `setVisible(b)`, `setText(t)`, ...), qui redessinent le widget dans son nouveau style.

| Fabrique | Options et méthodes |
| --- | --- |
| `ui:button(o)` | `text` ou `icon` (`"play"`, `"pause"`, `"stop"`, `"back"`, `"close"`, `"lock"`, `"repeat"`, ...), `accent`, `onClick(b)`. Sa taille s'adapte au libellé quand `w`/`h` sont omis. `setText`, `setIcon`. |
| `ui:label(o)` | `text`, `size` (`"title"`, `"label"`, `"small"` ou un nombre), `color`, `align`, `chip` (un fond arrondi). Ne prend pas le focus. `setText`. |
| `ui:toggle(o)`, `ui:checkbox(o)` | `text`, `value`, `onChange(v)`. `setValue(v, silent)`. |
| `ui:slider(o)` | `min`, `max`, `step`, `value`, `showValue`, `valueW`, `onChange(v)`. Glisser, ou Gauche/Droite quand il a le focus. `setValue(v, silent)`. |
| `ui:textbox(o)` | `value`, `placeholder`, `maxLen`, `onChange`, `onSubmit`. Cliquer ou Decide pour saisir. |
| `ui:panel(o)` | `title`. Un conteneur dessiné derrière les widgets créés après lui ; `panel:content()` donne son rectangle intérieur. Ne prend pas le focus. |
| `ui:bubble(o)` | `text`, `tail` (`"bottom"`, `"top"`, `"left"`, `"right"`), `tailPos`, `typewriter`, `onDone`. `setText`, `setTail`, `setSize`. |
| `ui:menu(o)` | `items` (des chaînes, ou `{ text, value, locked, mark }`), `selected`, `rowHeight`, `onChange(i, item)`, `onSelect(i, item)`, `onLockedSelect`, `drawLocked`, `hoverSelects`, `scrollbar`. Une liste déroulante. `setSelected(i)`. |
| `ui:chooser(o)` | `options`, `index`, `wrap`, `onChange(i, option)`. Un sélecteur d'options (Gauche/Droite). `value()`, `setOptions`, `setIndex`. |
| `ui:settingsList(o)` | `rows` (`{ header = "..." }` ou `{ opt = option }`), `onFocusRow`, `onActivateRow`, `scrollbar`. La liste de lignes d'options de l'écran des réglages. `setRows`. |
| `ui:coins(o)` | Un compteur de pièces (voir plus bas). Ne prend pas le focus. |

Les classes de widgets sont des champs de la bibliothèque (`UI.Button`, `UI.Menu`, ...) pour en dériver des sous-classes, avec `UI.Widget` (la classe de base), `UI.Shape` (les formes arrondies, ombres et icônes que les widgets précalculent), `UI.Theme`, `UI.Sfx` et `UI.Cache` (des canevas mis en commun).

## Le widget de pièces

`ui:coins{ amount, plain, size, iconSize, color, edge }` affiche un montant à côté de l'icône de pièce, sur une pastille (`plain = true` : l'icône et le nombre seulement, alignés à droite). Chaque bibliothèque le dessine à sa façon : l'icône de SimpleUI est le `Coin.png` posé à côté, PopUI garde la boîte à pièces du skin fourni.

| Méthode | Description |
| --- | --- |
| `c:setAmount(n)` | Affiche `n` immédiatement. |
| `c:add(n)` | Fait défiler le nombre de `n`. |
| `c:gain(n)`, `c:pay(n)` | La même chose, avec un `+n` ou un `-n` au-dessus et le son `coin` ou `pay`. |
| `c:amount()  -> number` | Le montant vers lequel il se dirige. |
| `c:setPrice(n)`, `c:setBonus(n)` | Un prix, ou un bonus `+n`, affiché au-dessus ; nil le retire. |
| `c:drawCoin(x, y, sx, sy, opacity)` | Dessine l'icône de pièce seule, centrée sur (x, y). |

## Thèmes

Un thème est une seule table. Le gestionnaire détermine l'apparence de chaque widget à partir de celle par défaut de la bibliothèque, puis du `theme` du gestionnaire, puis du `style` du widget ; les tables fusionnent champ par champ, si bien qu'un thème ne nomme que ce qu'il change.

| Champ | Description |
| --- | --- |
| `colors` | Des couleurs `{r, g, b, a}` de 0 à 255 : `bg`, `surface`/`surface2` (la face, en haut et en bas), `primary`/`primary2`, `accent`/`accent2`, `outline`, `text`, `textOnAccent`, `textDisabled`, `shadow`, `gloss`, `focusRing`, `track`, et `gain`/`loss` pour les pièces. |
| `radius`, `radiusSmall` | Les rayons des coins pour les grands et les petits éléments. |
| `outlineWidth` | L'épaisseur de la bordure. |
| `gloss`, `shadow`, `textOutline` | Le reflet du haut (bool), l'ombre portée (`{ dx, dy, layers, grow }`), des lettres détourées (bool). |
| `font` | Les tailles de texte : `small`, `label`, `button`, `title`. |
| `anim` | Les échelles et durées du survol et de l'appui, leurs easings (`{ "OUT", "QUAD" }`), `typewriterCps`. |

```lua
local ui = UI.new{ theme = { colors = { accent = { 220, 90, 60, 255 } }, radius = 14 } }
ui:button{ text = "Delete", x = 100, y = 100, style = { colors = { primary = { 180, 40, 40, 255 } } } }
```

## Sons

Les tables `sfx` associent à un nom un handle de son, une fonction, ou le nom d'une autre entrée : `move`, `hover`, `click`, `toggle`, `decide`, `cancel`, `skip`, `error`, plus `coin` et `pay` pour le widget de pièces. Par défaut, elles jouent les sons partagés `Move`, `Decide`, `Cancel`, `Skip` et `Error`, si bien qu'un skin qui les enregistre avec `SHARED:SetSharedSFX` donne ses sons à chaque écran global ; un son manquant reste muet.

## derive : une bibliothèque à votre image

`UI.SimpleUI.derive(spec)` renvoie une nouvelle bibliothèque qui a les widgets, le gestionnaire, le focus et les entrées de SimpleUI, avec sa propre apparence :

| Champ de `spec` | Description |
| --- | --- |
| `Theme` | Un module de thème : une table dont `DEFAULT` est le thème par défaut complet (copiez le `UI.SimpleUI.theme` de SimpleUI et modifiez-le). |
| `Coins` | Une classe facultative de widget de pièces, pour un compteur de pièces dessiné à votre façon. |
| `name` | Le nom de la bibliothèque. |

```lua
-- System/My Skin/Modules/Lib/MyUI.lua
local SimpleUI = require("UI.SimpleUI")
return SimpleUI.derive{
    name  = "MyUI",
    Theme = require("MyUI.theme"),
}
```

Nommez-la dans `ui_library` et chaque module global construit ses écrans avec elle. Le `Modules/Lib/PopUI.lua` d'Open-World Memories suit exactement ce modèle : SimpleUI avec son thème Bubblegum et son propre widget de pièces.
