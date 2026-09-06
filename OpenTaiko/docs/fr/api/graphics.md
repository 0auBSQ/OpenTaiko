<!-- api/graphics.md -->

# Graphismes et texte

Textures, canvas, découpage, texte, vidéo, couleurs, cartes de dégradé et tailles. Toutes les positions et tailles sont en pixels d'écran logiques, l'espace de coordonnées dans lequel les scripts dessinent (voir `INPUT:GetSurfaceWidth()` / `GetSurfaceHeight()`).

Le script qui crée un handle de texture, de canvas, de texte, de vidéo ou de dégradé en est propriétaire, et le jeu libère le handle quand il décharge ce script. Appelez `Dispose()` vous-même pour libérer un handle plus tôt.

## Textures

### TEXTURE

Charge des fichiers image dans des handles de texture.

<div class="callout warn">
Les chemins relatifs sont résolus par rapport au répertoire du script ; les variantes FromAbsolutePath prennent un chemin complet. Un fichier manquant renvoie un handle vide qui ne dessine rien. CreateTexture charge de façon asynchrone : le handle ne dessine rien et rapporte Width et Height à 0 jusqu'à la fin du décodage et de l'envoi en arrière-plan. Utilisez CreateTextureSync quand vous avez besoin de la taille ou des pixels immédiatement. La table d'options accepte { maxSize = N } pour réduire l'image au décodage de sorte que son plus grand côté fasse au plus N pixels.
</div>

| Méthode | Description |
| --- | --- |
| `TEXTURE:CreateTexture()  -> texture` | Crée un handle vide sans image. |
| `TEXTURE:CreateTexture(path)  -> texture` | Charge une image de façon asynchrone depuis un chemin relatif au répertoire du script. |
| `TEXTURE:CreateTexture(path, options)  -> texture` | Idem, avec une table d'options (`{ maxSize = N }`). |
| `TEXTURE:CreateTextureSync(path)  -> texture` | Charge une image de façon synchrone ; la taille et les pixels sont disponibles au retour. |
| `TEXTURE:CreateTextureFromAbsolutePath(path)  -> texture` | Charge une image depuis un chemin complet du système de fichiers. |
| `TEXTURE:CreateTextureFromAbsolutePath(path, options)  -> texture` | Idem, avec une table d'options (`{ maxSize = N }`). |
| `TEXTURE:Exists(path)  -> bool` | Indique si un fichier existe au chemin relatif au répertoire du script. |

```lua
local logo, jacket

function onStart()
    logo = TEXTURE:CreateTexture("Textures/Logo.png")
    jacket = TEXTURE:CreateTexture("Textures/Jacket.png", { maxSize = 512 })
end

function draw()
    logo:DrawAtAnchor(960, 540, "center")
end

function onDestroy()
    logo:Dispose()
    jacket:Dispose()
end
```

### Handle de texture

Une image 2D dessinable renvoyée par la fabrique `TEXTURE`, par `text:GetText` / `GetVerticalText`, et par `video.Texture`.

<div class="callout warn">
Noms d'ancre : topleft, top, topright, left, center, right, bottomleft, bottom, bottomright (insensibles à la casse ; un nom inconnu retombe sur topleft). Les dessins ancrés tiennent compte de l'échelle courante. Noms de mode de fusion : Normal, Add, Multi, Sub, Screen. Noms de mode de répétition : Edge, Border, Repeat, Mirror ; les nouveaux handles utilisent Repeat par défaut. Sur un handle vide, chaque méthode est sans effet et les accesseurs renvoient les valeurs par défaut listées ci-dessous.
</div>

| Méthode | Description |
| --- | --- |
| `texture:Draw(x, y)  -> nil` | Dessine la texture entière avec son coin supérieur gauche en (x, y). |
| `texture:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | Dessine un sous-rectangle source (en pixels de texture) avec son coin supérieur gauche en (x, y). |
| `texture:DrawAtAnchor(x, y, anchor)  -> nil` | Dessine la texture entière de sorte que le point d'ancrage nommé tombe en (x, y). |
| `texture:DrawRectAtAnchor(x, y, rect_x, rect_y, rect_width, rect_height, anchor)  -> nil` | Dessine un sous-rectangle source de sorte que le point d'ancrage nommé tombe en (x, y). |
| `texture.Loaded  -> bool` | Vrai quand le handle enveloppe une texture. Le jeu le définit dès qu'il trouve le fichier, avant la fin d'un chargement asynchrone. |
| `texture.Width  -> int` | Largeur en pixels ; -1 sur un handle vide, 0 pendant qu'un chargement asynchrone est en attente. |
| `texture.Height  -> int` | Hauteur en pixels ; -1 sur un handle vide, 0 pendant qu'un chargement asynchrone est en attente. |
| `texture.Pointer  -> int` | Identifiant de texture GL natif, ou 0 s'il n'y en a pas. |
| `texture:GetScale()  -> vector2` | Échelle de dessin courante sous forme de vector2 (`X`, `Y`). |
| `texture:GetOpacity()  -> number` | Opacité courante, 0..1 ; -1 sur un handle vide. |
| `texture:GetColor()  -> number, number, number` | Teinte courante sous forme de trois valeurs 0..1 (rouge, vert, bleu). |
| `texture:GetRotation()  -> number` | Rotation courante en degrés. |
| `texture:GetBlendMode()  -> string` | Nom du mode de fusion courant. |
| `texture:GetWrapMode()  -> string` | Nom du mode de répétition courant. |
| `texture:SetScale(scale_x, scale_y)  -> nil` | Définit l'échelle de dessin horizontale et verticale (1 = taille d'origine). |
| `texture:SetOpacity(opacity)  -> nil` | Définit l'opacité, 0..1. |
| `texture:SetColor(color)  -> nil` | Définit la teinte à partir d'une valeur `COLOR`. Le handle ignore l'alpha de la couleur ; réglez la transparence avec SetOpacity. |
| `texture:SetColor(red, green, blue)  -> nil` | Définit la teinte à partir de trois valeurs 0..1. |
| `texture:SetRotation(angle)  -> nil` | Définit la rotation autour du centre de la texture, en degrés. |
| `texture:SetBlendMode(mode)  -> nil` | Définit le mode de fusion par son nom (insensible à la casse). Le handle ignore les noms inconnus. |
| `texture:SetWrapMode(mode)  -> nil` | Définit le mode de répétition par son nom (insensible à la casse). Le handle ignore les noms inconnus. |
| `texture:SetUseNoiseEffect(enabled)  -> nil` | Quand activé, chaque dessin remplace la couleur de la texture par un bruit aléatoire animé en niveaux de gris et conserve son alpha. |
| `texture:SetGradientMap(gradient)  -> nil` | Applique une carte `GRADIENT` à chaque dessin de cette texture (voir GRADIENT). |
| `texture:ClearGradientMap()  -> nil` | Retire la carte de dégradé propre à la texture. |
| `texture:Dispose()  -> nil` | Libère la texture. |

## Canvas et découpage

### CANVAS

Crée des surfaces de pixels modifiables que Lua édite sur le CPU et envoie sous forme d'une seule texture.

<div class="callout warn">
Un canvas est une texture dont Lua définit les pixels (SetPixel, FillRect, ...) puis les pousse vers le GPU avec Upload. Il convient au rendu logiciel et aux formes d'interface préparées à l'avance, où un script peint et envoie une fois puis dessine le résultat à chaque frame. Les nouveaux canvas commencent entièrement transparents.
</div>

| Méthode | Description |
| --- | --- |
| `CANVAS:CreateCanvas(width, height)  -> canvas` | Crée un canvas transparent de la taille donnée (minimum 1x1). |

```lua
local panel

function onStart()
    panel = CANVAS:CreateCanvas(300, 80)
    panel:FillRect(0, 0, 300, 80, 20, 20, 40, 220)
    panel:FillCircle(40, 40, 24, 255, 200, 0, 255)
    panel:Upload()
end

function draw()
    panel:Draw(100, 100)
end

function onDestroy()
    panel:Dispose()
end
```

### Handle de canvas

Un tampon de pixels RGBA modifiable renvoyé par `CANVAS:CreateCanvas`, dessinable comme une texture.

<div class="callout warn">
Les arguments de couleur r, g, b, a sont des entiers 0-255. Les modifications de pixels s'accumulent dans un rectangle sale et n'atteignent le GPU que quand vous appelez Upload (BlitPacked envoie de lui-même). Les coordonnées de dessin sont des entiers. Les noms d'ancre sont les mêmes que pour le handle de texture.
</div>

| Méthode | Description |
| --- | --- |
| `canvas.Width  -> int` | Largeur en pixels. |
| `canvas.Height  -> int` | Hauteur en pixels. |
| `canvas.Pointer  -> int` | Identifiant de texture GL natif, ou 0 s'il n'y en a pas. |
| `canvas:SetPixel(x, y, r, g, b, a)  -> nil` | Définit un pixel. Le canvas ignore les coordonnées hors limites. |
| `canvas:FillRect(x, y, w, h, r, g, b, a)  -> nil` | Remplit un rectangle aligné sur les axes, découpé aux bords du canvas. |
| `canvas:FillCircle(cx, cy, radius, r, g, b, a)  -> nil` | Remplit un disque du rayon donné en pixels. |
| `canvas:StrokeLine(x0, y0, x1, y1, radius, r, g, b, a)  -> nil` | Peint une ligne épaisse sous forme de disques superposés du rayon donné. |
| `canvas:PasteTexture(texture, x, y)  -> nil` | Fusionne en alpha une texture sur le canvas avec son coin supérieur gauche en (x, y). Le canvas relit les pixels de la texture depuis le GPU une fois par handle de texture, une opération lente qui a sa place dans le code d'initialisation. |
| `canvas:PasteTextureTransformed(texture, x, y, scale, rotationDeg, anchor)  -> nil` | Fusionne en alpha une texture mise à l'échelle par `scale` et pivotée dans le sens horaire de `rotationDeg`, avec un échantillonnage au plus proche voisin. Avec l'ancre "center", le centre de la texture tombe en (x, y) ; toute autre valeur y place son coin supérieur gauche. |
| `canvas:Clear(r, g, b, a)  -> nil` | Remplit tout le canvas d'une seule couleur. |
| `canvas:ClearTransparent()  -> nil` | Remet tout le canvas entièrement transparent. |
| `canvas:CopyFrom(other)  -> nil` | Copie dans celui-ci les pixels d'un autre canvas de même taille. L'appel ne fait rien quand les tailles diffèrent. |
| `canvas:BlitPacked(data, count)  -> nil` | Remplit la surface à partir d'un tableau Lua indexé à partir de 1 d'entiers `0xRRGGBB` compactés, ligne par ligne (les pixels deviennent opaques ; une valeur négative devient un pixel transparent), puis envoie. |
| `canvas:Upload()  -> nil` | Pousse les modifications en attente (seulement la région modifiée) vers le GPU. Sans effet quand rien n'a changé. |
| `canvas:Draw(x, y)  -> nil` | Dessine le canvas avec son coin supérieur gauche en (x, y). |
| `canvas:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | Dessine un sous-rectangle source (en pixels de canvas) avec son coin supérieur gauche en (x, y). |
| `canvas:DrawAtAnchor(x, y, anchor)  -> nil` | Dessine le canvas de sorte que le point d'ancrage nommé tombe en (x, y). |
| `canvas:SetScale(scale_x, scale_y)  -> nil` | Définit l'échelle de dessin horizontale et verticale. |
| `canvas:SetOpacity(opacity)  -> nil` | Définit l'opacité, 0..1. |
| `canvas:SetColor(red, green, blue)  -> nil` | Définit la teinte à partir de trois valeurs 0..1. |
| `canvas:Dispose()  -> nil` | Libère la texture GPU et le tampon CPU du canvas. |

### GRAPHICS

Découpage par ciseaux (scissor) pour les panneaux défilants.

<div class="callout warn">
SetClip prend des coordonnées d'écran logiques et les projette sur la fenêtre d'affichage courante, si bien que le découpage est identique avec la mise à l'échelle du rendu et le letterboxing. Le GPU écarte chaque pixel dessiné hors du rectangle entre SetClip et ClearClip. SetClip remplace le rectangle précédent (il n'y a pas de pile) ; gardez SetClip et ClearClip appariés. Le jeu désactive le ciseau au début de chaque frame.
</div>

| Méthode | Description |
| --- | --- |
| `GRAPHICS:SetClip(x, y, w, h)  -> nil` | Active le découpage au rectangle donné. |
| `GRAPHICS:ClearClip()  -> nil` | Désactive le découpage. |

```lua
GRAPHICS:SetClip(100, 200, 600, 400)
for i, row in ipairs(rows) do
    row.tex:Draw(100, 200 + (i - 1) * 48 - scrollY)
end
GRAPHICS:ClearClip()
```

## Texte

### TEXT

Crée des moteurs de rendu de police pour la police principale du skin.

<div class="callout warn">
Create renvoie un handle de texte qui rend des chaînes entières dans des textures mises en cache ; utilisez-le pour les libellés qui changent rarement. CreateGlyphCached renvoie un handle de texte par glyphes qui met en cache une texture par caractère et compose les chaînes au moment du dessin ; utilisez-le pour le texte qui change souvent (chronomètres, scores, saisie) et pour les blocs avec retour à la ligne automatique. Les arguments de style sont l'un de bold, italic, underline, strikeout (insensibles à la casse ; les autres valeurs signifient regular). Dans un même script, la version de NLua fournie échoue si le script appelle la même méthode de fabrique d'abord sans argument de style puis avec exactement un argument de style. Soit ne passez jamais de style, soit passez-en toujours un dès le premier appel, soit passez deux jetons de style (par exemple "bold", "regular").
</div>

| Méthode | Description |
| --- | --- |
| `TEXT:Create(size, ...style)  -> text` | Crée un moteur de rendu de chaînes entières à la taille en pixels donnée. |
| `TEXT:CreateGlyphCached(size, ...style)  -> glyphText` | Crée un moteur de rendu de texte composé par glyphes à la taille en pixels donnée. |

#### Balises de couleur en ligne

Les deux moteurs de rendu reconnaissent ces balises dans la chaîne. Les balises s'imbriquent ; une balise non fermée court jusqu'à la fin de la chaîne. Les fonctions de mesure les ignorent.

| Balise | Effet |
| --- | --- |
| `<c.#rrggbb>` ... `</c>` | Couleur de remplissage. |
| `<c.#rrggbb.#rrggbb>` ... `</c>` | Couleur de remplissage et couleur de contour. |
| `<g.#rrggbb.#rrggbb>` ... `</g>` | Remplissage en dégradé vertical (couleur du haut, couleur du bas). |

### Handle de texte

Un moteur de rendu de chaînes entières renvoyé par `TEXT:Create`.

<div class="callout warn">
GetText et GetVerticalText mettent en cache une texture par combinaison unique de chaîne, couleur de remplissage, couleur de contour et limite de taille, et la conservent jusqu'à ce que vous libériez le handle. Une chaîne qui change à chaque frame ajoute donc une texture à chaque frame et fait fuir la mémoire GPU ; dessinez un tel texte avec DrawDirect ou un handle de texte par glyphes. forecolor et backcolor sont des valeurs COLOR ; les valeurs par défaut sont un remplissage blanc et un contour noir opaque. Le drapeau centered centre chaque ligne d'une chaîne multiligne ; la clé de cache l'exclut, si bien que le premier appel pour une chaîne donnée le décide.
</div>

| Méthode | Description |
| --- | --- |
| `text:GetText(text, centered, max_width, forecolor, backcolor)  -> texture` | Rend une chaîne horizontale dans une texture mise en cache. Tous les arguments après `text` sont facultatifs. Quand la texture rendue est plus large que `max_width`, elle se dessine comprimée horizontalement pour tenir. |
| `text:GetVerticalText(text, centered, max_height, forecolor, backcolor)  -> texture` | Rend une chaîne empilée verticalement dans une texture mise en cache. Quand la texture rendue est plus haute que `max_height`, elle se dessine comprimée verticalement pour tenir. |
| `text:DrawDirect(text, x, y, r, g, b, opacity, spacing)  -> number` | Dessine une chaîne glyphe par glyphe depuis un cache par caractère et renvoie la position x après le dernier glyphe. `r`, `g`, `b` sont 0-255 (255 par défaut), `opacity` est 0..1 (1 par défaut), `spacing` est l'écart entre les glyphes en pixels (-6 par défaut). |
| `text:Dispose()  -> nil` | Libère la police et toutes les textures mises en cache. |

```lua
local font, title

function onStart()
    font = TEXT:Create(28)
    title = font:GetText("Song select", true, 600)
end

function draw()
    title:DrawAtAnchor(960, 80, "center")
    font:DrawDirect(tostring(score), 40, 40, 255, 255, 0)
end

function onDestroy()
    font:Dispose()
end
```

### Handle de texte par glyphes

Un moteur de rendu composé par glyphes renvoyé par `TEXT:CreateGlyphCached`.

<div class="callout warn">
forecolor et backcolor sont des valeurs COLOR ; les valeurs par défaut sont un remplissage blanc et un contour noir. Chaque argument après la position est facultatif. maxWidth (0 = pas de limite) comprime chaque glyphe horizontalement pour que toute la ligne tienne. anchor utilise les mêmes neuf noms que le handle de texture. '\n' commence une nouvelle ligne ; Draw empile les lignes alignées à gauche. Draw renvoie la coordonnée x du bord droit de la boîte dessinée. La géométrie de la boîte correspond aux textures GetText (25 px de marge de chaque côté et sous l'encre), si bien qu'un texte par glyphes et une texture GetText dessinés au même point s'alignent.
</div>

| Méthode | Description |
| --- | --- |
| `glyphText.LineHeight  -> number` | Pas de ligne du texte multiligne, en pixels. |
| `glyphText.BoxHeight  -> number` | Hauteur d'une boîte d'une ligne (encre plus marge), identique à une texture GetText d'une ligne. |
| `glyphText:Measure(text, scale)  -> number` | Largeur d'encre de la ligne la plus large, multipliée par `scale` (1 par défaut). |
| `glyphText:Draw(text, x, y, forecolor, backcolor, opacity, scale, maxWidth, anchor, scaleY, rotationDeg)  -> number` | Dessine le texte. `opacity` 0..1 (1 par défaut) ; `scale` (1 par défaut) ; `scaleY` quand > 0 remplace l'échelle verticale ; `rotationDeg` fait pivoter tout le bloc autour de (x, y). Renvoie le x du bord droit de la boîte. |
| `glyphText:SetClipY(y0, y1)  -> nil` | Restreint les appels Draw droits suivants à la bande verticale y0..y1 en espace écran ; Draw ignore les glyphes hors de la bande et tranche les glyphes sur ses bords. Passez y1 <= y0 pour effacer. Les dessins pivotés ignorent la bande. |
| `glyphText:WrapToLines(text, wrapWidth, scale)  -> string[]` | Effectue le retour à la ligne à `wrapWidth` et renvoie les lignes sous forme de chaînes simples (balises retirées). |
| `glyphText:MeasureWrapped(text, wrapWidth, scale, lineSpacing)  -> number` | Hauteur que DrawWrapped utiliserait, sans dessiner. `lineSpacing` multiplie le pas de ligne (1 par défaut). |
| `glyphText:DrawWrapped(text, x, y, wrapWidth, forecolor, backcolor, opacity, scale, lineSpacing)  -> number` | Effectue le retour à la ligne à `wrapWidth` et dessine aligné à gauche depuis (x, y). Renvoie la hauteur dessinée. |
| `glyphText:Dispose()  -> nil` | Libère la police et tous les glyphes mis en cache. |

Le retour à la ligne se fait aux espaces, entre les caractères CJK, et à l'intérieur d'un mot plus large que la largeur de retour.

```lua
local font, white

function onStart()
    font = TEXT:CreateGlyphCached(24)
    white = COLOR:CreateColorFromRGBA(255, 255, 255)
end

function draw()
    font:Draw("Time " .. string.format("%.1f", t), 960, 40, white, nil, 1, 1, 0, "center")
    font:DrawWrapped(description, 200, 300, 800, white)
end

function onDestroy()
    font:Dispose()
end
```

## Vidéo

### VIDEO

Charge des fichiers vidéo dans des handles de décodeur lisibles.

<div class="callout warn">
Les chemins sont relatifs au répertoire du script. Le décodeur s'ouvre sur un thread d'arrière-plan ; tant qu'il n'est pas prêt, le handle ne dessine rien, met en file les appels Start, de positionnement et de vitesse, et les applique une fois le décodeur attaché. Un fichier manquant renvoie un handle vide.
</div>

| Méthode | Description |
| --- | --- |
| `VIDEO:CreateVideo(path)  -> video` | Ouvre un fichier vidéo et renvoie son handle. |

### Handle de vidéo

Un décodeur vidéo renvoyé par `VIDEO:CreateVideo`.

<div class="callout warn">
Les positions sont en millisecondes ; Duration est en secondes. Lisez Texture à chaque frame pour obtenir l'image courante sous forme de handle de texture. Ce handle enveloppe la texture d'image propre à la vidéo : dessinez-la et laissez sa libération au handle vidéo. SetSpeed ignore les valeurs inférieures ou égales à 0.
</div>

| Méthode | Description |
| --- | --- |
| `video:Start()  -> nil` | Démarre la lecture. |
| `video:Resume()  -> nil` | Reprend la lecture après Pause. |
| `video:Pause()  -> nil` | Met la lecture en pause. |
| `video:Stop()  -> nil` | Arrête la lecture. |
| `video:Reset()  -> nil` | Revient au début. |
| `video.Width  -> int` | Largeur de l'image en pixels, ou -1 tant que le décodeur n'est pas prêt. |
| `video.Height  -> int` | Hauteur de l'image en pixels, ou -1 tant que le décodeur n'est pas prêt. |
| `video.Duration  -> number` | Durée totale en secondes (1 tant que le décodeur n'est pas prêt). |
| `video.DurationMs  -> number` | Durée totale en millisecondes. |
| `video.Texture  -> texture` | L'image décodée courante. |
| `video:IsFinished()  -> bool` | Vrai une fois le flux terminé et qu'il ne reste aucune image. |
| `video:GetTimestampMs()  -> number` | Position de lecture courante en millisecondes. |
| `video:SetTimestampMs(ms)  -> nil` | Se positionne à une position en millisecondes. |
| `video:GetSpeed()  -> number` | Multiplicateur de vitesse de lecture courant (1 = normal). |
| `video:SetSpeed(speed)  -> nil` | Définit le multiplicateur de vitesse de lecture. |
| `video:GetPlayPosition()  -> number` | Position courante en secondes (ancien nom de GetTimestampMs / 1000). |
| `video:SetPlayPosition(seconds)  -> nil` | Se positionne à une position en secondes (ancien nom de SetTimestampMs). |
| `video:GetPlaySpeed()  -> number` | Ancien nom de GetSpeed. |
| `video:SetPlaySpeed(speed)  -> nil` | Ancien nom de SetSpeed. |
| `video:Dispose()  -> nil` | Libère le décodeur et sa texture d'image. |

```lua
local intro

function onStart()
    intro = VIDEO:CreateVideo("Videos/intro.mp4")
end

function activate()
    intro:Start()
end

function draw()
    intro.Texture:Draw(0, 0)
    if intro:IsFinished() then Exit("title", nil) end
end

function deactivate()
    intro:Stop()
end

function onDestroy()
    intro:Dispose()
end
```

## Couleur, dégradé et taille

### COLOR

Crée des valeurs de couleur.

<div class="callout warn">
Les canaux vont de 0 à 255. Les méthodes de texture, de canvas et de texte qui prennent une couleur acceptent ces valeurs.
</div>

| Méthode | Description |
| --- | --- |
| `COLOR:CreateColorFromRGBA(r, g, b, a)  -> color` | Crée une couleur à partir du rouge, du vert, du bleu et d'un alpha facultatif (255 par défaut). |
| `COLOR:CreateColorFromARGB(a, r, g, b)  -> color` | Crée une couleur à partir de l'alpha, du rouge, du vert et du bleu. |
| `COLOR:CreateColorFromHex(value)  -> color` | Analyse une chaîne hexadécimale `AARRGGBB` sans préfixe (par exemple `"ff2080ff"`). Une chaîne de six chiffres donne un alpha de 0. Une chaîne non analysable donne un blanc opaque. |

### Handle de couleur

Une valeur de couleur renvoyée par la fabrique `COLOR`.

| Méthode | Description |
| --- | --- |
| `color.R  -> int` | Canal rouge, 0-255 (lecture/écriture). |
| `color.G  -> int` | Canal vert, 0-255 (lecture/écriture). |
| `color.B  -> int` | Canal bleu, 0-255 (lecture/écriture). |
| `color.A  -> int` | Canal alpha, 0-255 (lecture/écriture). |

### GRADIENT

Les cartes de dégradé recolorent les dessins de texture en projetant la luminance de chaque pixel sur une rampe de couleurs, soit sur une texture, soit sur chaque dessin.

<div class="callout warn">
Create prend une table d'au moins deux points d'arrêt, chacun un tableau { position 0-1, r 0-255, g 0-255, b 0-255 [, a 0-255] }, et un taux de mélange (0 = aucun effet, 1 = remplacement complet, les valeurs intermédiaires mélangent ; 1 par défaut). Moins de deux points d'arrêt lève une erreur. Des points d'arrêt et un mélange identiques réutilisent une seule texture GPU mise en cache pour tout le programme. Appliquez à une seule texture avec texture:SetGradientMap, ou à chaque dessin de texture avec SetActive.
</div>

| Méthode | Description |
| --- | --- |
| `GRADIENT:Create(stops, blend)  -> gradient` | Construit (ou réutilise) une carte de dégradé à partir de points d'arrêt de couleur et d'un taux de mélange. |
| `GRADIENT:SetActive(gradient)  -> nil` | Applique le dégradé à chaque dessin de texture jusqu'à ClearActive. |
| `GRADIENT:ClearActive()  -> nil` | Retire la carte de dégradé globale. |

```lua
local sepia

function onStart()
    sepia = GRADIENT:Create({
        { 0.0,  40,  20,   0 },
        { 1.0, 255, 230, 180 },
    }, 0.8)
end

function draw()
    GRADIENT:SetActive(sepia)
    background:Draw(0, 0)
    GRADIENT:ClearActive()
end

function onDestroy()
    sepia:Dispose()
end
```

### Handle de dégradé

Une carte de dégradé renvoyée par `GRADIENT:Create`.

| Méthode | Description |
| --- | --- |
| `gradient.BlendStrength  -> number` | Taux de mélange, 0..1 (lecture seule). |
| `gradient:Dispose()  -> nil` | Libère le handle. La texture GPU partagée reste en cache. |

### SIZE

Crée des objets de valeur largeur/hauteur.

| Méthode | Description |
| --- | --- |
| `SIZE:CreateSize(width, height)  -> size` | Crée un objet taille. |

### Handle de taille

Une paire largeur et hauteur renvoyée par `SIZE:CreateSize`.

| Méthode | Description |
| --- | --- |
| `size.Width  -> int` | Largeur (lecture/écriture). |
| `size.Height  -> int` | Hauteur (lecture/écriture). |
