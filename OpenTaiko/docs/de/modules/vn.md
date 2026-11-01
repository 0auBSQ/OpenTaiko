<!-- modules/vn.md -->

# VN

Die Bausteine einer Visual-Novel-Szene: das Dialogfenster, der Skript-Runner und der Szenen-Player. `VN.Scene` fügt die beiden anderen zusammen; verwenden Sie den Runner und das Fenster allein, wenn Ihre Stage die Szene selbst zeichnet.

## VN.Dialogue

Ein Schreibmaschinen-Dialogfenster: ein Sprechername, ein Porträtbereich, umbrochener Text, der Buchstabe für Buchstabe erscheint, und Auswahlmöglichkeiten. Es steuert sich selbst: Rufen Sie `dlg:update(dt)` und `dlg:draw()` in jedem Frame auf und lesen Sie `dlg.result`, sobald es `"done"` meldet. Das Fenster zeichnet mit den Formen und Sounds der UI-Bibliothek, in einem eigenen Aussehen (die Option `theme`), egal welche Bibliothek der Skin gewählt hat.

Ein Skript ist eine Liste von Knoten `{ name, text, portrait, choices }`. Ein Knoten mit `choices` (eine Liste von `{ label, value }`) zeigt sie, sobald sein Text vollständig ist; die Wahl einer davon setzt `dlg.result` auf ihren `value` und beendet den Dialog. Ein Skript, das ohne Auswahl endet, setzt `dlg.result` auf true.

Tags in `text` werden ausgeführt, wenn die Schreibmaschine sie erreicht:

| Tag | Wirkung |
| --- | --- |
| `{pause:sec}` | Hält die Anzeige an (standardmäßig 0.4 s). |
| `{speed:cps}` | Ändert die Anzeigegeschwindigkeit, in Zeichen pro Sekunde. |
| `{shake:amp,dur}` | Schüttelt den Bildschirm: `onShake(amp, dur)`, oder die 3D-Kamera, wenn kein `onShake`-Handler angegeben ist. |
| `{sfx:name}`, `{voice:name}`, `{expr:name}` | Rufen `onSfx(name)`, `onVoice(name)` oder `onExpr(name)` auf (ein Wechsel des Gesichtsausdrucks mitten in einer Zeile). |

| Feld von `opts` | Beschreibung |
| --- | --- |
| `gfont`, `gfontName` | Glyphenschriften (`TEXT:CreateGlyphCached`) für den Text und den Namen: UTF-8-Text mit Zeilenumbruch. Der Name fällt auf `gfont` zurück. |
| `fonts` | Stattdessen Textschriften `{ name, text }`, der ältere Weg (ASCII). |
| `ui` | `"popui"` (der Standard: ein abgerundetes Fenster mit Schatten und einer Namenspille) oder `"plain"` (eine flache Fläche). |
| `theme` | Die Farben des Fensters: `face`, `face2`, `outline`, `namePill`, `namePill2`, `nameText`, `text`, `choiceRow`, `choiceSel`, `choiceText`, `caret`, `shadow`, `textEdge`. |
| `box` | `{ x, y, w, h }`: die Position des Fensters (standardmäßig quer über den unteren Bildschirmrand). |
| `portraitSize`, `portraits` | Das Porträtquadrat (0 blendet es aus, für große Sprites, die die Stage zeichnet); `{ name = texture }` nach Sprechername. |
| `cps` | Die Anzeigegeschwindigkeit, mit der jeder Knoten beginnt (standardmäßig 42). |
| `mouse` | true: Ein Linksklick schaltet ebenfalls weiter und bestätigt. |
| `advanceInput` | Eine Funktion, die für eine zusätzliche Weiter-Eingabe true zurückgibt (ein Trommelpad). |
| `drawBox` | false zeichnet nur Text, Auswahl und Cursor, über Ihrer eigenen Fenstergrafik. |
| `onBlip`, `blipEvery` | Ein Tipp-Blip: `onBlip()` alle `blipEvery` angezeigten Zeichen (3). |
| `onSfx`, `onVoice`, `onExpr`, `onShake` | Die Callbacks der Tags. |
| `sfx` | Sounds nach Namen für Weiterschalten und Auswählen, wie in [der UI](ui.md). |
| `opacity`, `caret` | Die Deckkraft des ganzen Fensters, und false, um die Weiter-Markierung auszublenden; beide dürfen sich in jedem Frame ändern. |

| Methode | Beschreibung |
| --- | --- |
| `Dialogue.new(opts)  -> dlg` | Erzeugt ein Fenster. |
| `dlg:start(script)` | Startet ein Skript bei seinem ersten Knoten. |
| `dlg:update(dt)  -> "running" or "done"` | Führt einen Frame aus: Decide, Leertaste (oder ein Klick, oder `advanceInput`) zeigt die ganze Zeile an und schaltet dann weiter; Hoch und Runter wählen eine Auswahl. |
| `dlg:draw()` | Zeichnet das Fenster. |
| `dlg:active()  -> bool` | Ob ein Skript läuft. |
| `dlg.result` | Der gewählte Wert, oder true, sobald fertig. |
| `dlg:dispose()` | Gibt die vorgerenderten Canvases des Fensters frei. |

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

Geht ein Skript aus Zeilen durch: Bedingungen, Zufallsauswahlen, Auswahlen, die ihren Zweig einfügen, und die Regieanweisungen, die der Aufrufer registriert. Er zeichnet nichts: `VN.Scene` (unten) oder Ihre eigene Stage schaltet ihn weiter und bringt seine Prosazeilen in ein `VN.Dialogue`-Fenster.

Eine Zeile ist eine Tabelle, jedes Feld ist optional:

| Feld | Beschreibung |
| --- | --- |
| `text` | Die Prosa, mit den Inline-Tags des Fensters. Eine Zeile mit `choices` und ohne `text` (oder mit `""`) lässt die vorige Prosazeile, bereits gelesen, unter ihren Auswahlmöglichkeiten auf dem Bildschirm. |
| `who`, `name` | Die ID des Sprechers; der angezeigte Name, der Vorrang vor dem eigenen Namen des Sprechers hat (`"???"`). |
| `expr` | Der Gesichtsausdruck des Sprechers. |
| `choices`, `branch` | `{ { label, value }, ... }`; `branch = { [value] = { lines } }` wird nach der Auswahl eingefügt. Der gewählte Wert landet in `ctx.choice` und `ctx.choices[value]`. |
| `when` | `function(ctx)`, die true zurückgibt, wenn die Zeile abgespielt wird; eine Bedingung, die einen Fehler auslöst, gilt als false. |
| `pick` | `{ { lines = { ... }, weight = n }, ... }`: eine nach Gewicht gezogene Option (standardmäßig 1), nie zweimal hintereinander dieselbe, nach der Zeile eingefügt. |
| jeder andere Schlüssel | Eine Regieanweisung (`enter`, `bg`, `fade`, `sfx`, `flag`, ...), ausgeführt von der dafür registrierten Funktion. |

| Feld von `opts` | Beschreibung |
| --- | --- |
| `lines`, `ctx` | Das Skript (der Runner geht eine Kopie durch) und die Tabelle, die die Bedingungen lesen. |
| `tr` | `fn(s)`: übersetzt Texte, Namen und Beschriftungen der Auswahlen; ohne sie erscheinen sie wie geschrieben. |
| `speakerName` | Der Name, den eine Zeile mit `who` und ohne `name` zeigt: ein String, eine Tabelle `{ [who] = name }` oder `fn(who, line, ctx)`. |
| `playerTag`, `playerName` | Das Tag, das in Texten durch den Namen des Spielers ersetzt wird (`"<player name>"`), und dieser Name: ein String oder `fn(ctx)`, standardmäßig `ctx.playerName`. |
| `directions`, `order` | `{ [key] = fn(line, runner) }` und die Reihenfolge, in der sie versucht werden (`Runner.ORDER`, dann die übrigen Schlüssel sortiert). |
| `onLine`, `onChoose` | `fn(line, runner)` für jede Zeile, die abgespielt wird, vor ihren Regieanweisungen; `fn(line, value, runner)`, wenn eine Auswahl getroffen wird, auch eine wiederaufgenommene. |
| `random` | `fn()`, die eine Zahl in [0, 1) zurückgibt, für die Zufallsauswahlen (standardmäßig `math.random`). |

Eine Regieanweisung gibt `false` zurück, wenn sie die Zeile nicht behandelt (dann wird die nächste in der Reihenfolge versucht), nil oder true, um zur nächsten Zeile weiterzugehen, `"hold"`, um den Durchlauf anzuhalten, bis der Aufrufer erneut weiterschaltet, oder `"end"`, um das Skript zu beenden. Sofern der Aufrufer keine eigene Regieanweisung `text` registriert, hält eine Zeile mit Text oder Auswahlen den Durchlauf als `"line"` an.

| Methode | Beschreibung |
| --- | --- |
| `Runner.new(opts)  -> runner` | Erzeugt einen Runner auf `opts.lines`. |
| `runner:step()  -> verdict, line` | Geht bis zum nächsten Halt: `"line"` (eine anzuzeigende Prosazeile), `"hold"` oder `"end"`. |
| `runner:present(dlg, line?)  -> node, carried` | Startet die aktuelle Zeile in einem `VN.Dialogue`-Fenster; eine reine Auswahl übernimmt die vorige Prosazeile, vollständig angezeigt. |
| `runner:choose(value)  -> bool` | Meldet das Ergebnis des Fensters: Ein Auswahlwert wird festgehalten und sein Zweig eingefügt. |
| `runner:current()  -> line`, `runner:load(lines)` | Die aktuelle Zeile; beginnt mit einem Skript von vorn. |
| `runner:ahead(fn, otherwise)  -> value` | Die erste Antwort (true oder false), die `fn(line)` für die Zeilen nach der aktuellen gibt; `otherwise`, wenn das Skript vorher endet. |
| `runner:bookmark()  -> mark` | `{ idx, picks, rolls }`: die Zeile auf dem Bildschirm, die getroffenen Auswahlen und die gezogenen Zufallsauswahlen. |
| `runner:resume(mark, replay?)` | Geht still bis zur Lesezeichenzeile vor und trifft dabei dieselben Auswahlen und Zufallsauswahlen; `replay = { [key] = fn(line, runner) }` wendet die Regieanweisungen erneut an, die Zustand hinterlassen (ein Hintergrund, ein Flag). Der nächste `step` zeigt die Lesezeichenzeile. |

Die Hilfsfunktionen `Runner.passes(line, ctx)`, `Runner.node(line, ctx, o, carry)`, `Runner.start(dlg, line, ctx, o, carry)`, `Runner.resolveChoice(lines, idx, ctx, value)` und `Runner.resolvePick(lines, idx, random, forced)` funktionieren ohne Runner, für eine Stage, die ihr Skript selbst durchgeht.

## VN.Scene

Ein Szenen-Player auf Basis von Runner und Fenster: Hintergrundebenen, die erscheinen, sobald sie geladen sind, eine Besetzung aus Sprites, Bildschirmblenden und -wackler sowie das Dialogfenster mit Tipp-Blips pro Sprecher. Er lädt nichts selbst: Die Hintergründe, die Texturen der Besetzung, die Musik, die Sounds und das gespeicherte Lesezeichen kommen alle vom Aufrufer.

Eine Szene ist `{ bg, music, lines }`: der erste Hintergrund, die Musik (sonst die des Hintergrunds) und Zeilen in der Form des Runners, mit diesen eingebauten Regieanweisungen:

| Zeile | Wirkung |
| --- | --- |
| `{ enter = who, expr, side }` | Eine Figur der Besetzung tritt auf und wird eingeblendet (und eingeschoben); die Szene wartet, bis sie da ist. Eine Figur, die schon auf dem Bildschirm ist, ändert nur ihr Gesicht. |
| `{ leave = who }` | Sie tritt ab; die Szene wartet, bis sie weg ist. |
| `{ expr = name }` | Das Gesicht von `who` oder der fokussierten Figur. |
| `{ bg = key }` | Der Hintergrund, angezeigt, sobald seine Ebenen geladen sind (oder nach `backgrounds.wait` Sekunden). |
| `{ fade = "out" or "in", keepSprite }` | Eine Blende über der Szene; `keepSprite = true` lässt die Besetzung beleuchtet. |
| `{ shake = true }`, `{ sfx = name }` | Ein Bildschirmwackeln (`shake` kann auch eine Amplitude oder `{ amp, dur }` sein); ein Sound. |
| `{ flag = name, value }` | Setzt `ctx.flags[name]` (true, wenn `value` fehlt). |
| `{ outcome = value }` | Die Szene blendet aus und meldet `value`. |

Eine Zeile einer Figur, die noch nicht aufgetreten ist, blendet sie zuerst ein. Eine Regieanweisung des Aufrufers, die `"hold"` zurückgibt, hält die Szene an, bis `scene:continue()` aufgerufen wird oder die an `scene:waitFor(fn)` übergebene Funktion true zurückgibt (`fn(dt)` wird in jedem Frame aufgerufen).

| Feld von `opts` | Beschreibung |
| --- | --- |
| `box` | Optionen für `VN.Dialogue.new`; die Szene füllt `onBlip`, `onShake`, `onExpr` und `onSfx`, wenn sie nicht gesetzt sind. Oder `dialogue`, ein eigenes Fenster. |
| `cast` | `{ [who] = member }`. Eine Figur: `body` (eine Textur, oder `fn(expr)` für ein Bild pro Ausdruck), `face` (`fn(expr)`, über dem Körper bei `faceX`, `faceY` gezeichnet), `name`, `expr` (der Ausdruck, mit dem sie auftritt), `side` (`"left"`, `"center"`, `"right"`) oder `x`, `y` und `anchor` (`"top"` oder `"bottom"`), `scale` oder `height` (die Skalierung, die den Körper auf so viele Pixel bringt), `fade` (0.35 s), `slide` (px), `bounce` (0.36 s, 0 für keines) und `squash` für das Hüpfen bei einem Gesichtswechsel. |
| `sides`, `focus`, `dim` | Das x jeder Seite; wessen Gesicht ein `expr` ohne Sprecher ändert; die Farbe `{ r, g, b }` der Figuren, die gerade nicht sprechen (ohne: nie abgedunkelt). |
| `backgrounds` | `defs` (`{ [key] = def }`) und `path` (`"Textures/bg/%s.png"` für einen Schlüssel ohne Definition), oder `resolve(key)`; `open(layer)` für Texturen, die dem Aufrufer gehören (eine geliehene Textur wird erneut geöffnet, wenn ihr Besitzer sie freigibt); `wait` (2 s); die Hooks `prepare`, `show`, `update`, `drawBehind`, `drawOver`, `close`. Eine Definition ist eine Ebene oder `{ layers = { ... } }`, eine Ebene ist `{ path, x, y, scale, scroll }`, und die `music` einer Definition läuft, solange sie angezeigt wird. |
| `music` | `{ play(key), fadeOut(sec), playing(), update(dt), stop() }`, alle optional. |
| `sound`, `blip`, `clickSound` | `fn(name)` spielt einen Sound nach Namen; `blip(node)` gibt den Tipp-Blip eines Fensterknotens zurück (`node.speaker`), nil für Stille; der Klick beim Weiterschalten. |
| `cover` oder `drawCover` | Die Textur der Blende (oder `fn()`, die eine zurückgibt), über den Bildschirm gestreckt; oder `fn(alpha)`, die die Blende zeichnet. |
| `drawBox`, `fadeSec`, `outcome` | `fn(dlg, scene)`, die das Fenster statt `dlg:draw()` zeichnet; die Blendzeit (0.45 s); das Ergebnis, wenn die Zeilen einfach zu Ende gehen (`"done"`). |
| `tr`, `speakerName`, `playerTag`, `playerName`, `random`, `onLine`, `order` | Werden an den Runner übergeben. |
| `directions`, `replay` | Zusätzliche Regieanweisungen oder Ersatz für die eingebauten; wie ein wiederaufgenommenes Lesezeichen sie erneut anwendet. |
| `onBookmark`, `hooks` | `fn(mark)` bei jeder beginnenden Zeile, mit einem zu speichernden Lesezeichen; `hooks = { prose, start, expr, choose, update, draw, dispose }`. |

| Methode | Beschreibung |
| --- | --- |
| `Scene.new(opts)  -> scene` | Erzeugt einen Player. |
| `scene:play(scene, ctx, onDone, resume?)` | Startet eine Szene. `ctx` wird von den Bedingungen gelesen und erhält die Auswahlen; `onDone(result)` bekommt das Ergebnis; `resume` ist ein Lesezeichen, ab dem fortgesetzt wird. |
| `scene:update(dt)  -> "running" or "done"`, `scene:draw()` | In jedem Frame aufrufen. |
| `scene:continue()`, `scene:waitFor(fn)` | Beendet ein Anhalten; hält an, bis `fn(dt)` true zurückgibt. |
| `scene:setBackground(key)`, `scene:setExpr(who, expr)`, `scene:shake(amp, dur)`, `scene:sound(name)` | Die eingebauten Effekte, für Ihre eigenen Regieanweisungen. |
| `scene:bookmark()  -> mark`, `scene:active()  -> bool` | Das Lesezeichen des Runners; ob eine Szene läuft. |
| `scene:reset()`, `scene:dispose()` | Stoppt die Szene; gibt frei, was die Szene geöffnet hat (und das Fenster, sofern es nicht Ihres ist). |

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

Das Fenster zeichnet über `VN.Dialogue`, sodass sein Rahmen der UI-Bibliothek folgt, die [UI.Pick](ui.md) liefert.
