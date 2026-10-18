<!-- modules/vn.md -->

# VN

Les éléments d'une scène de visual novel : la boîte de dialogue, l'exécuteur de scripts et le lecteur de scènes. `VN.Scene` assemble les deux autres ; utilisez l'exécuteur et la boîte seuls quand votre stage dessine lui-même la scène.

## VN.Dialogue

Une boîte de dialogue à effet machine à écrire : un nom de locuteur, une zone de portrait, un texte avec retour à la ligne révélé lettre par lettre, et des choix. Elle se pilote toute seule : appelez `dlg:update(dt)` et `dlg:draw()` à chaque frame, et lisez `dlg.result` une fois qu'elle signale `"done"`. La boîte se dessine avec les formes et les sons de la bibliothèque d'interface, dans une apparence qui lui est propre (l'option `theme`), quelle que soit la bibliothèque choisie par le skin.

Un script est une liste de nœuds `{ name, text, portrait, choices }`. Un nœud avec `choices` (une liste de `{ label, value }`) les affiche une fois son texte entièrement révélé ; en choisir un définit `dlg.result` à sa `value` et termine le dialogue. Un script qui se termine sans choix définit `dlg.result` à true.

Les balises dans `text` s'exécutent quand la machine à écrire les atteint :

| Balise | Effet |
| --- | --- |
| `{pause:sec}` | Suspend la révélation (0.4 s par défaut). |
| `{speed:cps}` | Change la vitesse de révélation, en caractères par seconde. |
| `{shake:amp,dur}` | Secoue l'écran : `onShake(amp, dur)`, ou la caméra 3D quand aucun gestionnaire `onShake` n'est fourni. |
| `{sfx:name}`, `{voice:name}`, `{expr:name}` | Appellent `onSfx(name)`, `onVoice(name)` ou `onExpr(name)` (un changement d'expression au milieu d'une réplique). |

| Champ de `opts` | Description |
| --- | --- |
| `gfont`, `gfontName` | Des polices de glyphes (`TEXT:CreateGlyphCached`) pour le texte et le nom : du texte UTF-8 avec retour à la ligne. Le nom se replie sur `gfont`. |
| `fonts` | Des polices de texte `{ name, text }` à la place, l'ancienne méthode (ASCII). |
| `ui` | `"popui"` (par défaut : une boîte arrondie avec une ombre et une pastille de nom) ou `"plain"` (un panneau plat). |
| `theme` | Les couleurs de la boîte : `face`, `face2`, `outline`, `namePill`, `namePill2`, `nameText`, `text`, `choiceRow`, `choiceSel`, `choiceText`, `caret`, `shadow`, `textEdge`. |
| `box` | `{ x, y, w, h }` : la place de la boîte (par défaut en travers du bas de l'écran). |
| `portraitSize`, `portraits` | Le carré du portrait (0 le masque, pour de grands sprites dessinés par le stage) ; `{ name = texture }` par nom de locuteur. |
| `cps` | La vitesse de révélation à laquelle commence chaque nœud (42 par défaut). |
| `mouse` | true : un clic gauche fait aussi avancer et valide. |
| `advanceInput` | Une fonction qui renvoie true pour une entrée d'avance supplémentaire (un pad de tambour). |
| `drawBox` | false ne dessine que le texte, les choix et le curseur, par-dessus votre propre image de boîte. |
| `onBlip`, `blipEvery` | Un bip de frappe : `onBlip()` tous les `blipEvery` caractères révélés (3). |
| `onSfx`, `onVoice`, `onExpr`, `onShake` | Les fonctions de rappel des balises. |
| `sfx` | Des sons par nom pour avancer et choisir, comme dans [l'interface](ui.md). |
| `opacity`, `caret` | L'opacité de toute la boîte, et false pour masquer l'indicateur d'avance ; les deux peuvent changer à n'importe quelle frame. |

| Méthode | Description |
| --- | --- |
| `Dialogue.new(opts)  -> dlg` | Crée une boîte. |
| `dlg:start(script)` | Lance un script à partir de son premier nœud. |
| `dlg:update(dt)  -> "running" or "done"` | Exécute une frame : Decide, Espace (ou un clic, ou `advanceInput`) révèle toute la réplique, puis avance ; Haut et Bas sélectionnent un choix. |
| `dlg:draw()` | Dessine la boîte. |
| `dlg:active()  -> bool` | Si un script est en cours. |
| `dlg.result` | La valeur choisie, ou true, une fois terminé. |
| `dlg:dispose()` | Libère les canevas précalculés de la boîte. |

```lua
local Dialogue = require("VN.Dialogue")
local dlg, answer, last = nil, nil, 0

function activate()
    dlg = Dialogue.new{ gfont = TEXT:CreateGlyphCached(30), portraitSize = 0 }
    dlg:start{
        { name = "Nokon", text = "Welcome back.{pause:0.5} Ready?" },
        { name = "Nokon", text = "Pick one.", choices = { { label = "Yes", value = 1 }, { label = "Later", value = 2 } } },
    }
end

function update(ts)
    local dt = (ts - last) / 1000; last = ts
    if dlg:update(dt) == "done" then answer = dlg.result end
end

function draw() dlg:draw() end
```

## VN.Runner

Parcourt un script de lignes : conditions, tirages au sort, choix qui insèrent leur branche, et les indications de mise en scène que l'appelant enregistre. Il ne dessine rien : `VN.Scene` (ci-dessous) ou votre propre stage le fait avancer et place ses lignes de texte dans une boîte `VN.Dialogue`.

Une ligne est une table, dont chaque champ est facultatif :

| Champ | Description |
| --- | --- |
| `text` | Le texte, avec les balises en ligne de la boîte. Une ligne avec `choices` et sans `text` (ou avec `""`) garde à l'écran la ligne de texte précédente, déjà lue, sous ses choix. |
| `who`, `name` | L'identifiant du locuteur ; le nom affiché, qui l'emporte sur celui du locuteur (`"???"`). |
| `expr` | L'expression du locuteur. |
| `choices`, `branch` | `{ { label, value }, ... }` ; `branch = { [value] = { lines } }` est inséré après le choix. La valeur retenue va dans `ctx.choice` et `ctx.choices[value]`. |
| `when` | `function(ctx)` qui renvoie true quand la ligne doit être jouée ; une condition qui lève une erreur compte comme false. |
| `pick` | `{ { lines = { ... }, weight = n }, ... }` : une option tirée selon son poids (1 par défaut), jamais la même deux fois de suite, insérée après la ligne. |
| toute autre clé | Une indication de mise en scène (`enter`, `bg`, `fade`, `sfx`, `flag`, ...), exécutée par la fonction enregistrée pour elle. |

| Champ de `opts` | Description |
| --- | --- |
| `lines`, `ctx` | Le script (l'exécuteur en parcourt une copie) et la table que lisent les conditions. |
| `tr` | `fn(s)` : traduit les textes, les noms et les libellés des choix ; sans elle, ils s'affichent tels qu'écrits. |
| `speakerName` | Le nom qu'affiche une ligne avec `who` et sans `name` : une chaîne, une table `{ [who] = name }` ou `fn(who, line, ctx)`. |
| `playerTag`, `playerName` | La balise remplacée par le nom du joueur dans les textes (`"<player name>"`), et ce nom : une chaîne ou `fn(ctx)`, `ctx.playerName` par défaut. |
| `directions`, `order` | `{ [key] = fn(line, runner) }`, et l'ordre dans lequel elles sont essayées (`Runner.ORDER`, puis les autres clés triées). |
| `onLine`, `onChoose` | `fn(line, runner)` pour chaque ligne jouée, avant ses indications ; `fn(line, value, runner)` quand un choix est fait, y compris un choix rejoué à la reprise. |
| `random` | `fn()` qui renvoie un nombre dans [0, 1), pour les tirages (`math.random` par défaut). |

Une indication renvoie `false` quand elle ne traite pas la ligne (la suivante dans l'ordre est alors essayée), nil ou true pour passer à la ligne suivante, `"hold"` pour arrêter le parcours jusqu'à ce que l'appelant le relance, ou `"end"` pour terminer le script. Sauf si l'appelant enregistre sa propre indication `text`, une ligne avec du texte ou des choix arrête le parcours avec `"line"`.

| Méthode | Description |
| --- | --- |
| `Runner.new(opts)  -> runner` | Crée un exécuteur sur `opts.lines`. |
| `runner:step()  -> verdict, line` | Avance jusqu'à l'arrêt suivant : `"line"` (une ligne de texte à afficher), `"hold"` ou `"end"`. |
| `runner:present(dlg, line?)  -> node, carried` | Lance la ligne courante dans une boîte `VN.Dialogue` ; un choix seul reprend la ligne de texte précédente, entièrement révélée. |
| `runner:choose(value)  -> bool` | Transmet le résultat de la boîte : une valeur de choix est enregistrée et sa branche insérée. |
| `runner:current()  -> line`, `runner:load(lines)` | La ligne courante ; recommence sur un script. |
| `runner:ahead(fn, otherwise)  -> value` | La première réponse (true ou false) que donne `fn(line)` pour les lignes qui suivent la ligne courante ; `otherwise` si le script se termine avant. |
| `runner:bookmark()  -> mark` | `{ idx, picks, rolls }` : la ligne à l'écran, les choix faits et les tirages effectués. |
| `runner:resume(mark, replay?)` | Avance silencieusement jusqu'à la ligne marquée, en refaisant les mêmes choix et les mêmes tirages ; `replay = { [key] = fn(line, runner) }` réapplique les indications qui laissent un état derrière elles (un arrière-plan, un drapeau). Le `step` suivant affiche la ligne marquée. |

Les utilitaires `Runner.passes(line, ctx)`, `Runner.node(line, ctx, o, carry)`, `Runner.start(dlg, line, ctx, o, carry)`, `Runner.resolveChoice(lines, idx, ctx, value)` et `Runner.resolvePick(lines, idx, random, forced)` fonctionnent sans exécuteur, pour un stage qui parcourt lui-même son script.

## VN.Scene

Un lecteur de scènes construit sur l'exécuteur et la boîte : des calques d'arrière-plan qui s'affichent une fois chargés, une distribution de sprites, des fondus et des secousses d'écran, et la boîte de dialogue avec des bips de frappe propres à chaque locuteur. Il ne charge rien lui-même : les arrière-plans, les textures de la distribution, la musique, les sons et le marque-page enregistré viennent tous de l'appelant.

Une scène est `{ bg, music, lines }` : le premier arrière-plan, la musique (sinon celle de l'arrière-plan), et des lignes sous la forme de l'exécuteur, avec ces indications intégrées :

| Ligne | Effet |
| --- | --- |
| `{ enter = who, expr, side }` | Un membre de la distribution entre en scène, en fondu (et en glissant) ; la scène attend qu'il soit arrivé. Un membre déjà à l'écran change seulement de visage. |
| `{ leave = who }` | Il sort ; la scène attend qu'il soit parti. |
| `{ expr = name }` | Le visage de `who`, ou du membre qui a le focus. |
| `{ bg = key }` | L'arrière-plan, affiché une fois ses calques chargés (ou après `backgrounds.wait` secondes). |
| `{ fade = "out" or "in", keepSprite }` | Un fondu sur la scène ; `keepSprite = true` garde la distribution éclairée. |
| `{ shake = true }`, `{ sfx = name }` | Une secousse de l'écran (`shake` peut aussi être une amplitude, ou `{ amp, dur }`) ; un son. |
| `{ flag = name, value }` | Définit `ctx.flags[name]` (true quand `value` est absent). |
| `{ outcome = value }` | La scène se termine en fondu et renvoie `value`. |

Une ligne d'un membre de la distribution qui n'est pas encore entré le fait d'abord apparaître en fondu. Une indication de l'appelant qui renvoie `"hold"` suspend la scène jusqu'à `scene:continue()`, ou jusqu'à ce que la fonction passée à `scene:waitFor(fn)` renvoie true (`fn(dt)` est appelée à chaque frame).

| Champ de `opts` | Description |
| --- | --- |
| `box` | Les options de `VN.Dialogue.new` ; la scène remplit `onBlip`, `onShake`, `onExpr` et `onSfx` quand ils ne sont pas définis. Ou `dialogue`, une boîte à vous. |
| `cast` | `{ [who] = member }`. Un membre : `body` (une texture, ou `fn(expr)` pour une image par expression), `face` (`fn(expr)`, dessiné par-dessus le corps en `faceX`, `faceY`), `name`, `expr` (celle avec laquelle il entre), `side` (`"left"`, `"center"`, `"right"`) ou `x`, `y` et `anchor` (`"top"` ou `"bottom"`), `scale` ou `height` (l'échelle qui ramène le corps à ce nombre de pixels), `fade` (0.35 s), `slide` (px), `bounce` (0.36 s, 0 pour aucun) et `squash` pour le rebond lors d'un changement de visage. |
| `sides`, `focus`, `dim` | L'abscisse de chaque côté ; le membre dont un `expr` sans locuteur change le visage ; la couleur `{ r, g, b }` des membres qui ne parlent pas (sans elle : jamais assombris). |
| `backgrounds` | `defs` (`{ [key] = def }`) et `path` (`"Textures/bg/%s.png"` pour une clé sans définition), ou `resolve(key)` ; `open(layer)` pour les textures qui appartiennent à l'appelant (une texture empruntée est rouverte si son propriétaire la libère) ; `wait` (2 s) ; les points d'accroche `prepare`, `show`, `update`, `drawBehind`, `drawOver`, `close`. Une définition est un calque ou `{ layers = { ... } }`, un calque est `{ path, x, y, scale, scroll }`, et la `music` d'une définition joue tant qu'elle est affichée. |
| `music` | `{ play(key), fadeOut(sec), playing(), update(dt), stop() }`, tous facultatifs. |
| `sound`, `blip`, `clickSound` | `fn(name)` joue un son par son nom ; `blip(node)` renvoie le bip de frappe d'un nœud de la boîte (`node.speaker`), nil pour le silence ; le clic d'avance. |
| `cover` ou `drawCover` | La texture du fondu (ou `fn()` qui en renvoie une), étirée sur l'écran ; ou `fn(alpha)` qui dessine le fondu. |
| `drawBox`, `fadeSec`, `outcome` | `fn(dlg, scene)` qui dessine la boîte à la place de `dlg:draw()` ; la durée du fondu (0.45 s) ; le résultat quand les lignes s'épuisent simplement (`"done"`). |
| `tr`, `speakerName`, `playerTag`, `playerName`, `random`, `onLine`, `order` | Transmis à l'exécuteur. |
| `directions`, `replay` | Des indications supplémentaires, ou des remplaçantes pour celles intégrées ; la façon dont un marque-page repris les réapplique. |
| `onBookmark`, `hooks` | `fn(mark)` à chaque ligne qui commence, avec un marque-page à enregistrer ; `hooks = { prose, start, expr, choose, update, draw, dispose }`. |

| Méthode | Description |
| --- | --- |
| `Scene.new(opts)  -> scene` | Crée un lecteur. |
| `scene:play(scene, ctx, onDone, resume?)` | Lance une scène. `ctx` est lu par les conditions et reçoit les choix ; `onDone(result)` reçoit l'issue ; `resume` est un marque-page à partir duquel reprendre. |
| `scene:update(dt)  -> "running" or "done"`, `scene:draw()` | À appeler à chaque frame. |
| `scene:continue()`, `scene:waitFor(fn)` | Met fin à une suspension ; suspend jusqu'à ce que `fn(dt)` renvoie true. |
| `scene:setBackground(key)`, `scene:setExpr(who, expr)`, `scene:shake(amp, dur)`, `scene:sound(name)` | Les effets intégrés, pour vos propres indications. |
| `scene:bookmark()  -> mark`, `scene:active()  -> bool` | Le marque-page de l'exécuteur ; si une scène est en cours. |
| `scene:reset()`, `scene:dispose()` | Arrête la scène ; libère ce que la scène a ouvert (et la boîte, sauf si elle est à vous). |

```lua
local Scene = require("VN.Scene")
local vn = Scene.new{
    box = { gfont = font30, gfontName = font34 },
    cast = { aoi = { name = "Aoi", body = aoiTex, face = function(e) return faces[e] end,
                     faceX = 210, faceY = 140, side = "right", anchor = "bottom", expr = "normal" } },
    backgrounds = { path = "Textures/bg/%s.png" }, cover = blackTex,
    sound = function(n) sfx[n]:Play() end,
    blip = function(node) return node and node.speaker and "blip" end,
    onBookmark = function(mark) save.vn = mark end,
}
vn:play({ bg = "park", lines = {
    { text = "A quiet afternoon." },
    { who = "aoi", expr = "smile", text = "Hi, <player name>!" },
    { text = "", choices = { { label = "Hi!", value = "hi" }, { label = "...", value = "shy" } },
      branch = { shy = { { who = "aoi", expr = "sad", text = "Not talkative today?" } } } },
    { bg = "street", sfx = "door" }, { fade = "out" }, { fade = "in" }, { outcome = "met" },
} }, { playerName = name }, function(result) met = result end, save.vn)
-- then vn:update(dt) and vn:draw() every frame
```

La boîte se dessine via `VN.Dialogue`, si bien que son habillage suit la bibliothèque d'interface que donne [UI.Pick](ui.md).
