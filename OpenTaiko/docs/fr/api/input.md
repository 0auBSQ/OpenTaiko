<!-- api/input.md -->

# Entrées

Lecture des entrées tambour, du clavier et de la souris, et saisie de texte à l'écran.

## INPUT

Lit les entrées tambour configurées, le clavier brut et la souris, et crée des champs de saisie de texte. Disponible dans chaque script.

<div class="callout warn">
Les noms d'entrée tambour sont les noms de la configuration des touches, comparés sans tenir compte de la casse : LRed, RRed, LBlue, RBlue (joueur 1), LRed2P ... RBlue5P pour les joueurs 2 à 5, Clap, Clap2P ... Clap5P, LeftChange, RightChange, Decide, Cancel, et les touches système (Capture, SongVolumeIncrease, SongVolumeDecrease, DisplayHits, DisplayDebug, QuickConfig, SortSongs, ToggleAutoP1, ToggleAutoP2, ToggleTrainingMode, CycleVideoDisplayMode, et les touches Training*). Un nom inconnu renvoie false. Les méthodes tambour lisent le périphérique que le joueur a associé à cette entrée dans la configuration des touches. INPUT compare les noms de touches clavier sans tenir compte de la casse à la liste de touches du moteur : lettres A-Z, chiffres D0-D9, F1-F15, Space, Return, Escape, Tab, Backspace, Delete, UpArrow, DownArrow, LeftArrow, RightArrow, LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, Home, End, PageUp, PageDown, NumberPad0-9, et les autres touches standard. Les positions de la souris sont en coordonnées logiques de la surface de jeu, corrigées du letterboxing.
</div>

### Entrées tambour

| Méthode | Description |
| --- | --- |
| `INPUT:Pressed(input)  -> bool` | Vrai sur la frame où l'entrée est enfoncée. |
| `INPUT:Pressing(input)  -> bool` | Vrai tant que l'entrée est maintenue. |
| `INPUT:Released(input)  -> bool` | Vrai sur la frame où l'entrée est relâchée. |
| `INPUT:Releasing(input)  -> bool` | Vrai tant que l'entrée n'est pas maintenue. |
| `INPUT:RepeatWhilePressing(input, interval_seconds, callback)  -> nil` | Appelez-la à chaque frame. Tant que le joueur maintient l'entrée, elle appelle `callback()` une fois toutes les `interval_seconds` (le premier appel vient après un intervalle) et s'arrête quand le joueur relâche l'entrée. |

### Clavier

| Méthode | Description |
| --- | --- |
| `INPUT:KeyboardPressed(key)  -> bool` | Vrai sur la frame où la touche est enfoncée. |
| `INPUT:KeyboardPressing(key)  -> bool` | Vrai tant que la touche est maintenue. |
| `INPUT:KeyboardReleased(key)  -> bool` | Vrai sur la frame où la touche est relâchée. |
| `INPUT:KeyboardReleasing(key)  -> bool` | Vrai tant que la touche n'est pas maintenue. |

### Souris

<div class="callout warn">
Noms de bouton : left, right, middle, button4, button5, ou un indice numérique (insensibles à la casse). Les positions sont en coordonnées de la surface de jeu ; les accesseurs de position renvoient -1 quand aucun périphérique souris n'existe. GetMouseDelta et GetScrollDelta renvoient le mouvement depuis l'appel précédent puis se réinitialisent ; appelez donc chacune une fois par frame.
</div>

| Méthode | Description |
| --- | --- |
| `INPUT:GetMouseX()  -> number` | X de la souris en coordonnées de la surface de jeu. |
| `INPUT:GetMouseY()  -> number` | Y de la souris en coordonnées de la surface de jeu. |
| `INPUT:GetMouseXY()  -> number, number` | X et Y de la souris sous forme de deux valeurs de retour. |
| `INPUT:IsMouseInside()  -> bool` | Vrai quand la souris est au-dessus de la surface rendue et faux quand elle est au-dessus d'une bande de letterboxing. |
| `INPUT:GetSurfaceWidth()  -> int` | Largeur de la surface de jeu (l'espace de coordonnées dans lequel les scripts dessinent). |
| `INPUT:GetSurfaceHeight()  -> int` | Hauteur de la surface de jeu. |
| `INPUT:MousePressed(button)  -> bool` | Vrai sur la frame où le bouton est enfoncé. |
| `INPUT:MousePressing(button)  -> bool` | Vrai tant que le bouton est maintenu. |
| `INPUT:MouseReleased(button)  -> bool` | Vrai sur la frame où le bouton est relâché. |
| `INPUT:GetMouseDelta()  -> number, number` | Mouvement de la souris en pixels de fenêtre depuis l'appel précédent, sous forme dx, dy. |
| `INPUT:GetScrollDelta()  -> number, number` | Mouvement de la molette en crans depuis l'appel précédent, sous forme dx, dy (dy est positif en défilant vers le haut). |
| `INPUT:SetMouseLocked(locked)  -> nil` | Verrouille et masque le curseur pour la vue libre (true) ou le restaure (false), et réinitialise la référence du delta. |

```lua
function update()
    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then confirm() end
    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then goBack() end

    local mx, my = INPUT:GetMouseXY()
    hovered = mx >= bx and mx < bx + bw and my >= by and my < by + bh
    if hovered and INPUT:MousePressed("left") then confirm() end

    local _, wheel = INPUT:GetScrollDelta()
    scrollY = scrollY - wheel * 40
end
```

### Saisie de texte

| Méthode | Description |
| --- | --- |
| `INPUT:CreateTextInput(initialText, maxLength)  -> textInput` | Crée un champ de saisie de texte prérempli avec `initialText` et limité à `maxLength` octets d'UTF-8 (64 par défaut). |

## Handle de saisie de texte

Un champ de texte à l'écran renvoyé par `INPUT:CreateTextInput` qui recueille le texte saisi, y compris la composition IME.

<div class="callout warn">
Appelez Update une fois par frame tant que le champ est actif et dessinez DisplayText vous-même. Ne gardez qu'un seul champ de saisie actif à la fois. Dans les builds Debug, une fenêtre superposée affiche aussi le champ ; les builds Release n'affichent rien d'elles-mêmes. Sur iOS et Android, le jeu ouvre la boîte de dialogue de texte native de la plateforme, et Update renvoie true quand le joueur la confirme.
</div>

| Méthode | Description |
| --- | --- |
| `textInput:Update()  -> bool` | Traite la saisie de cette frame. Renvoie true sur la frame où le joueur presse Entrée. |
| `textInput.Text  -> string` | Le texte courant. Lisible et assignable (nil devient une chaîne vide). |
| `textInput.DisplayText  -> string` | Le texte courant avec un curseur clignotant inséré à la position du curseur, pour le dessin. |

```lua
local field, font

function onStart()
    font = TEXT:CreateGlyphCached(28)
end

function activate()
    field = INPUT:CreateTextInput("", 32)
end

function update()
    if field:Update() then
        saveName(field.Text)
    elseif INPUT:KeyboardPressed("Escape") then
        field.Text = ""
    end
end

function draw()
    font:Draw(field.DisplayText, 100, 100)
end

function onDestroy()
    font:Dispose()
end
```
