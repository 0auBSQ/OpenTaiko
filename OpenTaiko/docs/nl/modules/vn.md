<!-- modules/vn.md -->

# VN

De onderdelen van een visual-novelscène: het dialoogvenster, de scriptrunner en de scènespeler. `VN.Scene` voegt de andere twee samen; gebruik de runner en het venster los wanneer je stage de scène zelf tekent.

## VN.Dialogue

Een typemachine-dialoogvenster: een sprekersnaam, een portretvak, teruglopende tekst die letter voor letter verschijnt, en keuzes. Het stuurt zichzelf aan: roep `dlg:update(dt)` en `dlg:draw()` elk frame aan, en lees `dlg.result` zodra het `"done"` meldt. Het venster tekent met de vormen en geluiden van de UI-bibliotheek, in een eigen uiterlijk (de optie `theme`), welke bibliotheek de skin ook gekozen heeft.

Een script is een lijst van knopen `{ name, text, portrait, choices }`. Een knoop met `choices` (een lijst van `{ label, value }`) toont die zodra zijn tekst helemaal zichtbaar is; een keuze maken zet `dlg.result` op haar `value` en beëindigt de dialoog. Een script dat zonder keuze eindigt, zet `dlg.result` op true.

Tags in `text` worden uitgevoerd wanneer de typemachine ze bereikt:

| Tag | Effect |
| --- | --- |
| `{pause:sec}` | Houdt het tonen even stil (standaard 0.4 s). |
| `{speed:cps}` | Wijzigt de toonsnelheid, in tekens per seconde. |
| `{shake:amp,dur}` | Schudt het scherm: `onShake(amp, dur)`, of de 3D-camera als er geen `onShake`-handler is opgegeven. |
| `{sfx:name}`, `{voice:name}`, `{expr:name}` | Roepen `onSfx(name)`, `onVoice(name)` of `onExpr(name)` aan (een wisseling van gezichtsuitdrukking midden in een regel). |

| Veld van `opts` | Beschrijving |
| --- | --- |
| `gfont`, `gfontName` | Glyphlettertypen (`TEXT:CreateGlyphCached`) voor de tekst en de naam: UTF-8-tekst met automatische terugloop. De naam valt terug op `gfont`. |
| `fonts` | In plaats daarvan tekstlettertypen `{ name, text }`, de oudere weg (ASCII). |
| `ui` | `"popui"` (de standaard: een afgerond venster met een schaduw en een naampil) of `"plain"` (een plat paneel). |
| `theme` | De kleuren van het venster: `face`, `face2`, `outline`, `namePill`, `namePill2`, `nameText`, `text`, `choiceRow`, `choiceSel`, `choiceText`, `caret`, `shadow`, `textEdge`. |
| `box` | `{ x, y, w, h }`: de plek van het venster (standaard over de onderkant van het scherm). |
| `portraitSize`, `portraits` | Het portretvierkant (0 verbergt het, voor grote sprites die de stage tekent); `{ name = texture }` op sprekersnaam. |
| `cps` | De toonsnelheid waarmee elke knoop begint (standaard 42). |
| `mouse` | true: een linkerklik gaat ook verder en bevestigt. |
| `advanceInput` | Een functie die true teruggeeft voor een extra invoer om verder te gaan (een drumpad). |
| `drawBox` | false tekent alleen de tekst, de keuzes en de cursor, over je eigen vensterafbeelding. |
| `onBlip`, `blipEvery` | Een typgeluidje: `onBlip()` na elke `blipEvery` getoonde tekens (3). |
| `onSfx`, `onVoice`, `onExpr`, `onShake` | De callbacks van de tags. |
| `sfx` | Geluiden op naam voor verdergaan en kiezen, zoals in [de UI](ui.md). |
| `opacity`, `caret` | De dekking van het hele venster, en false om de verdergaan-markering te verbergen; beide mogen elk frame veranderen. |

| Methode | Beschrijving |
| --- | --- |
| `Dialogue.new(opts)  -> dlg` | Maakt een venster. |
| `dlg:start(script)` | Start een script vanaf zijn eerste knoop. |
| `dlg:update(dt)  -> "running" or "done"` | Verwerkt één frame: Decide, Spatie (of een klik, of `advanceInput`) toont de hele regel en gaat daarna verder; Omhoog en Omlaag kiezen een keuze. |
| `dlg:draw()` | Tekent het venster. |
| `dlg:active()  -> bool` | Of er een script loopt. |
| `dlg.result` | De gekozen waarde, of true, zodra het klaar is. |
| `dlg:dispose()` | Geeft de voorgebakken canvassen van het venster vrij. |

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

Loopt een script van regels door: voorwaarden, willekeurige keuzes, keuzes die hun tak invoegen, en de regieaanwijzingen die de aanroeper registreert. Hij tekent niets: `VN.Scene` (hieronder) of je eigen stage laat hem stap voor stap verdergaan en zet zijn tekstregels in een `VN.Dialogue`-venster.

Een regel is een tabel, waarvan elk veld optioneel is:

| Veld | Beschrijving |
| --- | --- |
| `text` | De tekst, met de inline tags van het venster. Een regel met `choices` en zonder `text` (of met `""`) houdt de vorige tekstregel, al gelezen, op het scherm onder zijn keuzes. |
| `who`, `name` | Het id van de spreker; de getoonde naam, die voorgaat op de eigen naam van de spreker (`"???"`). |
| `expr` | De gezichtsuitdrukking van de spreker. |
| `choices`, `branch` | `{ { label, value }, ... }`; `branch = { [value] = { lines } }` wordt na de keuze ingevoegd. De gekozen waarde gaat naar `ctx.choice` en `ctx.choices[value]`. |
| `when` | `function(ctx)` die true teruggeeft wanneer de regel wordt afgespeeld; een voorwaarde die een fout opwerpt, telt als false. |
| `pick` | `{ { lines = { ... }, weight = n }, ... }`: één optie getrokken op gewicht (standaard 1), nooit twee keer achter elkaar dezelfde, ingevoegd na de regel. |
| elke andere sleutel | Een regieaanwijzing (`enter`, `bg`, `fade`, `sfx`, `flag`, ...), uitgevoerd door de functie die ervoor is geregistreerd. |

| Veld van `opts` | Beschrijving |
| --- | --- |
| `lines`, `ctx` | Het script (de runner loopt een kopie door) en de tabel die de voorwaarden lezen. |
| `tr` | `fn(s)`: vertaalt teksten, namen en keuzelabels; zonder deze functie verschijnen ze zoals geschreven. |
| `speakerName` | De naam die een regel met `who` en zonder `name` toont: een string, een tabel `{ [who] = name }` of `fn(who, line, ctx)`. |
| `playerTag`, `playerName` | De tag die in teksten door de naam van de speler wordt vervangen (`"<player name>"`), en die naam: een string of `fn(ctx)`, standaard `ctx.playerName`. |
| `directions`, `order` | `{ [key] = fn(line, runner) }`, en de volgorde waarin ze worden geprobeerd (`Runner.ORDER`, daarna de overige sleutels gesorteerd). |
| `onLine`, `onChoose` | `fn(line, runner)` voor elke regel die wordt afgespeeld, vóór zijn regieaanwijzingen; `fn(line, value, runner)` wanneer een keuze wordt gemaakt, ook een hervatte. |
| `random` | `fn()` die een getal in [0, 1) teruggeeft, voor de willekeurige keuzes (standaard `math.random`). |

Een regieaanwijzing geeft `false` terug als ze de regel niet afhandelt (dan wordt de volgende in de volgorde geprobeerd), nil of true om naar de volgende regel te gaan, `"hold"` om het doorlopen te stoppen tot de aanroeper weer een stap zet, of `"end"` om het script te beëindigen. Tenzij de aanroeper een eigen regieaanwijzing `text` registreert, stopt een regel met tekst of keuzes het doorlopen als `"line"`.

| Methode | Beschrijving |
| --- | --- |
| `Runner.new(opts)  -> runner` | Maakt een runner op `opts.lines`. |
| `runner:step()  -> verdict, line` | Loopt door tot de volgende stop: `"line"` (een tekstregel om te tonen), `"hold"` of `"end"`. |
| `runner:present(dlg, line?)  -> node, carried` | Start de huidige regel in een `VN.Dialogue`-venster; een losse keuze neemt de vorige tekstregel mee, volledig getoond. |
| `runner:choose(value)  -> bool` | Geeft het resultaat van het venster door: een keuzewaarde wordt vastgelegd en haar tak ingevoegd. |
| `runner:current()  -> line`, `runner:load(lines)` | De huidige regel; begint opnieuw met een script. |
| `runner:ahead(fn, otherwise)  -> value` | Het eerste antwoord (true of false) dat `fn(line)` geeft voor de regels na de huidige; `otherwise` als het script eerder eindigt. |
| `runner:bookmark()  -> mark` | `{ idx, picks, rolls }`: de regel op het scherm, de gemaakte keuzes en de getrokken willekeurige keuzes. |
| `runner:resume(mark, replay?)` | Loopt stil door tot de regel van de bladwijzer en maakt daarbij dezelfde keuzes en trekkingen; `replay = { [key] = fn(line, runner) }` past de regieaanwijzingen opnieuw toe die status achterlaten (een achtergrond, een vlag). De volgende `step` toont de regel van de bladwijzer. |

De helpers `Runner.passes(line, ctx)`, `Runner.node(line, ctx, o, carry)`, `Runner.start(dlg, line, ctx, o, carry)`, `Runner.resolveChoice(lines, idx, ctx, value)` en `Runner.resolvePick(lines, idx, random, forced)` werken zonder runner, voor een stage die zijn script zelf doorloopt.

## VN.Scene

Een scènespeler gebouwd op de runner en het venster: achtergrondlagen die verschijnen zodra ze geladen zijn, een cast van sprites, schermfades en -schokken, en het dialoogvenster met typgeluidjes per spreker. Hij laadt zelf niets: de achtergronden, de texturen van de cast, de muziek, de geluiden en de opgeslagen bladwijzer komen allemaal van de aanroeper.

Een scène is `{ bg, music, lines }`: de eerste achtergrond, de muziek (anders die van de achtergrond), en regels in de vorm van de runner, met deze ingebouwde regieaanwijzingen:

| Regel | Effect |
| --- | --- |
| `{ enter = who, expr, side }` | Een castlid komt op, met een fade (en een schuifbeweging); de scène wacht tot het er is. Een castlid dat al op het scherm staat, wisselt alleen van gezicht. |
| `{ leave = who }` | Het gaat af; de scène wacht tot het weg is. |
| `{ expr = name }` | Het gezicht van `who`, of van het castlid met de focus. |
| `{ bg = key }` | De achtergrond, getoond zodra zijn lagen geladen zijn (of na `backgrounds.wait` seconden). |
| `{ fade = "out" or "in", keepSprite }` | Een fade over de scène; `keepSprite = true` houdt de cast verlicht. |
| `{ shake = true }`, `{ sfx = name }` | Een schermschok (`shake` kan ook een amplitude zijn, of `{ amp, dur }`); een geluid. |
| `{ flag = name, value }` | Zet `ctx.flags[name]` (true als `value` ontbreekt). |
| `{ outcome = value }` | De scène fadet uit en meldt `value`. |

Een regel van een castlid dat nog niet is opgekomen, laat het eerst infaden. Een regieaanwijzing van de aanroeper die `"hold"` teruggeeft, houdt de scène stil tot `scene:continue()`, of tot de functie die aan `scene:waitFor(fn)` is meegegeven true teruggeeft (`fn(dt)` wordt elk frame aangeroepen).

| Veld van `opts` | Beschrijving |
| --- | --- |
| `box` | Opties voor `VN.Dialogue.new`; de scène vult `onBlip`, `onShake`, `onExpr` en `onSfx` in als die niet zijn ingesteld. Of `dialogue`, een eigen venster. |
| `cast` | `{ [who] = member }`. Een castlid: `body` (een textuur, of `fn(expr)` voor één afbeelding per uitdrukking), `face` (`fn(expr)`, over het lichaam getekend op `faceX`, `faceY`), `name`, `expr` (de uitdrukking waarmee het opkomt), `side` (`"left"`, `"center"`, `"right"`) of `x`, `y` en `anchor` (`"top"` of `"bottom"`), `scale` of `height` (de schaal die het lichaam op zoveel pixels brengt), `fade` (0.35 s), `slide` (px), `bounce` (0.36 s, 0 voor geen) en `squash` voor het veren bij een gezichtswissel. |
| `sides`, `focus`, `dim` | De x van elke kant; wiens gezicht een `expr` zonder spreker wijzigt; de kleur `{ r, g, b }` van de castleden die niet spreken (zonder: nooit gedimd). |
| `backgrounds` | `defs` (`{ [key] = def }`) en `path` (`"Textures/bg/%s.png"` voor een sleutel zonder definitie), of `resolve(key)`; `open(layer)` voor texturen die van de aanroeper zijn (een geleende textuur wordt opnieuw geopend als de eigenaar haar vrijgeeft); `wait` (2 s); de hooks `prepare`, `show`, `update`, `drawBehind`, `drawOver`, `close`. Een definitie is een laag of `{ layers = { ... } }`, een laag is `{ path, x, y, scale, scroll }`, en de `music` van een definitie speelt zolang die getoond wordt. |
| `music` | `{ play(key), fadeOut(sec), playing(), update(dt), stop() }`, allemaal optioneel. |
| `sound`, `blip`, `clickSound` | `fn(name)` speelt een geluid op naam; `blip(node)` geeft het typgeluidje van een vensterknoop terug (`node.speaker`), nil voor stilte; de klik bij verdergaan. |
| `cover` of `drawCover` | De textuur van de fade (of `fn()` die er een teruggeeft), over het scherm uitgerekt; of `fn(alpha)` die de fade tekent. |
| `drawBox`, `fadeSec`, `outcome` | `fn(dlg, scene)` die het venster tekent in plaats van `dlg:draw()`; de fadetijd (0.45 s); het resultaat als de regels gewoon op zijn (`"done"`). |
| `tr`, `speakerName`, `playerTag`, `playerName`, `random`, `onLine`, `order` | Worden doorgegeven aan de runner. |
| `directions`, `replay` | Extra regieaanwijzingen, of vervangingen voor de ingebouwde; hoe een hervatte bladwijzer ze opnieuw toepast. |
| `onBookmark`, `hooks` | `fn(mark)` bij elke regel die begint, met een bladwijzer om op te slaan; `hooks = { prose, start, expr, choose, update, draw, dispose }`. |

| Methode | Beschrijving |
| --- | --- |
| `Scene.new(opts)  -> scene` | Maakt een speler. |
| `scene:play(scene, ctx, onDone, resume?)` | Start een scène. `ctx` wordt door de voorwaarden gelezen en krijgt de keuzes; `onDone(result)` krijgt de uitkomst; `resume` is een bladwijzer om vanaf verder te gaan. |
| `scene:update(dt)  -> "running" or "done"`, `scene:draw()` | Roep ze elk frame aan. |
| `scene:continue()`, `scene:waitFor(fn)` | Beëindigt een stilstand; houdt stil tot `fn(dt)` true teruggeeft. |
| `scene:setBackground(key)`, `scene:setExpr(who, expr)`, `scene:shake(amp, dur)`, `scene:sound(name)` | De ingebouwde effecten, voor je eigen regieaanwijzingen. |
| `scene:bookmark()  -> mark`, `scene:active()  -> bool` | De bladwijzer van de runner; of er een scène speelt. |
| `scene:reset()`, `scene:dispose()` | Stopt de scène; geeft vrij wat de scène heeft geopend (en het venster, tenzij het van jou is). |

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

Het venster tekent via `VN.Dialogue`, dus zijn opmaak volgt de UI-bibliotheek die [UI.Pick](ui.md) geeft.
