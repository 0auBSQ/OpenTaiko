<!-- modules/vn.md -->

# VN

Las piezas de una escena de novela visual: el cuadro de diálogo, el ejecutor de guiones y el reproductor de escenas. `VN.Scene` reúne los otros dos; usa el ejecutor y el cuadro por separado cuando tu stage dibuja la escena por sí mismo.

## VN.Dialogue

Un cuadro de diálogo con efecto de máquina de escribir: el nombre de quien habla, una zona de retrato, texto con ajuste de palabras revelado letra a letra, y opciones. Se gestiona solo: llama a `dlg:update(dt)` y `dlg:draw()` cada fotograma, y lee `dlg.result` cuando informe `"done"`. El cuadro se dibuja con las formas y los sonidos de la biblioteca de interfaz, con un estilo propio (la opción `theme`), sea cual sea la biblioteca que eligió el skin.

Un guion es una lista de nodos `{ name, text, portrait, choices }`. Un nodo con `choices` (una lista de `{ label, value }`) las muestra cuando su texto ha terminado de aparecer; elegir una establece `dlg.result` a su `value` y termina el diálogo. Un guion que termina sin opción establece `dlg.result` a true.

Las etiquetas dentro de `text` se ejecutan cuando la máquina de escribir llega a ellas:

| Etiqueta | Efecto |
| --- | --- |
| `{pause:sec}` | Detiene la revelación (0.4 s por defecto). |
| `{speed:cps}` | Cambia la velocidad de revelación, en caracteres por segundo. |
| `{shake:amp,dur}` | Sacude la pantalla: `onShake(amp, dur)`, o la cámara 3D cuando no se da ningún manejador `onShake`. |
| `{sfx:name}`, `{voice:name}`, `{expr:name}` | Llaman a `onSfx(name)`, `onVoice(name)` o `onExpr(name)` (un cambio de expresión en mitad de una línea). |

| Campo de `opts` | Descripción |
| --- | --- |
| `gfont`, `gfontName` | Fuentes de glifos (`TEXT:CreateGlyphCached`) para el texto y el nombre: texto UTF-8 con ajuste de palabras. El nombre recurre a `gfont`. |
| `fonts` | Fuentes de texto `{ name, text }` en su lugar, la vía antigua (ASCII). |
| `ui` | `"popui"` (el predeterminado: una caja redondeada con sombra y una píldora para el nombre) o `"plain"` (un panel plano). |
| `theme` | Los colores de la caja: `face`, `face2`, `outline`, `namePill`, `namePill2`, `nameText`, `text`, `choiceRow`, `choiceSel`, `choiceText`, `caret`, `shadow`, `textEdge`. |
| `box` | `{ x, y, w, h }`: la posición de la caja (por defecto, a lo ancho de la parte inferior de la pantalla). |
| `portraitSize`, `portraits` | El cuadrado del retrato (0 lo oculta, para sprites grandes que dibuja el stage); `{ name = texture }` por nombre de quien habla. |
| `cps` | La velocidad de revelación con la que empieza cada nodo (42 por defecto). |
| `mouse` | true: un clic izquierdo también avanza y confirma. |
| `advanceInput` | Una función que devuelve true para una entrada de avance adicional (un pad del tambor). |
| `drawBox` | false dibuja solo el texto, las opciones y el indicador, sobre tu propio arte de caja. |
| `onBlip`, `blipEvery` | Un sonido de tecleo: `onBlip()` cada `blipEvery` caracteres revelados (3). |
| `onSfx`, `onVoice`, `onExpr`, `onShake` | Los callbacks de las etiquetas. |
| `sfx` | Sonidos por nombre para avanzar y elegir, como en [la interfaz](ui.md). |
| `opacity`, `caret` | La opacidad de toda la caja, y false para ocultar el indicador de avance; ambos pueden cambiar en cualquier fotograma. |

| Método | Descripción |
| --- | --- |
| `Dialogue.new(opts)  -> dlg` | Crea una caja. |
| `dlg:start(script)` | Inicia un guion desde su primer nodo. |
| `dlg:update(dt)  -> "running" or "done"` | Ejecuta un fotograma: Decide, Espacio (o un clic, o `advanceInput`) revela la línea entera y luego avanza; Arriba y Abajo eligen una opción. |
| `dlg:draw()` | Dibuja la caja. |
| `dlg:active()  -> bool` | Si hay un guion en curso. |
| `dlg.result` | El valor elegido, o true, una vez terminado. |
| `dlg:dispose()` | Libera los lienzos horneados de la caja. |

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

Recorre un guion de líneas: condiciones, elecciones aleatorias, opciones que insertan su rama y las acotaciones que registra el llamador. No dibuja nada: `VN.Scene` (más abajo) o tu propio stage lo hace avanzar y pone sus líneas de prosa en un cuadro `VN.Dialogue`.

Una línea es una tabla, con todos los campos opcionales:

| Campo | Descripción |
| --- | --- |
| `text` | La prosa, con las etiquetas en línea del cuadro. Una línea con `choices` y sin `text` (o con `""`) mantiene en pantalla la línea de prosa anterior, ya leída, bajo sus opciones. |
| `who`, `name` | El id de quien habla; el nombre mostrado, que tiene prioridad sobre el propio de quien habla (`"???"`). |
| `expr` | La expresión de quien habla. |
| `choices`, `branch` | `{ { label, value }, ... }`; `branch = { [value] = { lines } }` se inserta después de la elección. El valor elegido va a `ctx.choice` y a `ctx.choices[value]`. |
| `when` | `function(ctx)` que devuelve true cuando la línea se reproduce; una condición que lanza un error cuenta como false. |
| `pick` | `{ { lines = { ... }, weight = n }, ... }`: una opción sorteada según su peso (1 por defecto), nunca la misma dos veces seguidas, insertada después de la línea. |
| cualquier otra clave | Una acotación (`enter`, `bg`, `fade`, `sfx`, `flag`, ...), ejecutada por la función registrada para ella. |

| Campo de `opts` | Descripción |
| --- | --- |
| `lines`, `ctx` | El guion (el ejecutor recorre una copia) y la tabla que leen las condiciones. |
| `tr` | `fn(s)`: traduce los textos, los nombres y las etiquetas de las opciones; sin ella se muestran tal como están escritos. |
| `speakerName` | El nombre que muestra una línea con `who` y sin `name`: una cadena, una tabla `{ [who] = name }` o `fn(who, line, ctx)`. |
| `playerTag`, `playerName` | La etiqueta que se reemplaza por el nombre del jugador en los textos (`"<player name>"`), y ese nombre: una cadena o `fn(ctx)`, `ctx.playerName` por defecto. |
| `directions`, `order` | `{ [key] = fn(line, runner) }`, y el orden en que se prueban (`Runner.ORDER` y luego las demás claves ordenadas). |
| `onLine`, `onChoose` | `fn(line, runner)` para cada línea que se reproduce, antes de sus acotaciones; `fn(line, value, runner)` cuando se toma una opción, incluida una reanudada. |
| `random` | `fn()` que devuelve un número en [0, 1), para los sorteos (`math.random` por defecto). |

Una acotación devuelve `false` cuando no gestiona la línea (se prueba la siguiente del orden), nil o true para pasar a la línea siguiente, `"hold"` para detener el recorrido hasta que el llamador vuelva a avanzar, o `"end"` para terminar el guion. Salvo que el llamador registre su propia acotación `text`, una línea con texto u opciones detiene el recorrido como `"line"`.

| Método | Descripción |
| --- | --- |
| `Runner.new(opts)  -> runner` | Crea un ejecutor sobre `opts.lines`. |
| `runner:step()  -> verdict, line` | Avanza hasta la siguiente parada: `"line"` (una línea de prosa que mostrar), `"hold"` o `"end"`. |
| `runner:present(dlg, line?)  -> node, carried` | Inicia la línea actual en un cuadro `VN.Dialogue`; una opción sin prosa arrastra la línea de prosa anterior, revelada por completo. |
| `runner:choose(value)  -> bool` | Comunica el resultado del cuadro: se registra el valor de una opción y se inserta su rama. |
| `runner:current()  -> line`, `runner:load(lines)` | La línea actual; vuelve a empezar con un guion. |
| `runner:ahead(fn, otherwise)  -> value` | La primera respuesta (true o false) que da `fn(line)` para las líneas posteriores a la actual; `otherwise` cuando el guion termina antes. |
| `runner:bookmark()  -> mark` | `{ idx, picks, rolls }`: la línea en pantalla, las opciones tomadas y los sorteos realizados. |
| `runner:resume(mark, replay?)` | Vuelve en silencio a la línea marcada, tomando las mismas opciones y sorteos; `replay = { [key] = fn(line, runner) }` vuelve a aplicar las acotaciones que dejan estado (un fondo, un indicador). El siguiente `step` muestra la línea marcada. |

Las utilidades `Runner.passes(line, ctx)`, `Runner.node(line, ctx, o, carry)`, `Runner.start(dlg, line, ctx, o, carry)`, `Runner.resolveChoice(lines, idx, ctx, value)` y `Runner.resolvePick(lines, idx, random, forced)` funcionan sin ejecutor, para un stage que recorre su guion por sí mismo.

## VN.Scene

Un reproductor de escenas construido sobre el ejecutor y el cuadro: capas de fondo que se muestran una vez cargadas, un reparto de sprites, fundidos y sacudidas de pantalla, y el cuadro de diálogo con sonidos de tecleo por personaje. No carga nada por sí mismo: los fondos, las texturas del reparto, la música, los sonidos y el marcador guardado vienen todos del llamador.

Una escena es `{ bg, music, lines }`: el primer fondo, la música (si no, la del fondo), y líneas en la forma del ejecutor con estas acotaciones incorporadas:

| Línea | Efecto |
| --- | --- |
| `{ enter = who, expr, side }` | Un miembro del reparto entra con un fundido (y un deslizamiento); la escena espera a que termine. Si ya está en pantalla, solo cambia su cara. |
| `{ leave = who }` | Sale; la escena espera a que termine. |
| `{ expr = name }` | La cara de `who`, o del miembro enfocado. |
| `{ bg = key }` | El fondo, mostrado cuando sus capas se han cargado (o tras `backgrounds.wait` segundos). |
| `{ fade = "out" or "in", keepSprite }` | Un fundido sobre la escena; `keepSprite = true` mantiene iluminado el reparto. |
| `{ shake = true }`, `{ sfx = name }` | Una sacudida de pantalla (`shake` también puede ser una amplitud, o `{ amp, dur }`); un sonido. |
| `{ flag = name, value }` | Establece `ctx.flags[name]` (true cuando falta `value`). |
| `{ outcome = value }` | La escena hace un fundido de salida y comunica `value`. |

Una línea de un miembro del reparto que aún no ha entrado lo hace entrar primero con un fundido. Una acotación del llamador que devuelve `"hold"` retiene la escena hasta `scene:continue()`, o hasta que la función pasada a `scene:waitFor(fn)` devuelva true (`fn(dt)` se llama cada fotograma).

| Campo de `opts` | Descripción |
| --- | --- |
| `box` | Opciones de `VN.Dialogue.new`; la escena rellena `onBlip`, `onShake`, `onExpr` y `onSfx` cuando no están definidas. O bien `dialogue`, un cuadro propio. |
| `cast` | `{ [who] = member }`. Un miembro: `body` (una textura, o `fn(expr)` para una imagen por expresión), `face` (`fn(expr)`, dibujada sobre el cuerpo en `faceX`, `faceY`), `name`, `expr` (la expresión con la que entra), `side` (`"left"`, `"center"`, `"right"`) o `x`, `y` y `anchor` (`"top"` o `"bottom"`), `scale` o `height` (la escala que ajusta el cuerpo a esa cantidad de píxeles), `fade` (0.35 s), `slide` (px), `bounce` (0.36 s, 0 para ninguno) y `squash` para el rebote al cambiar de cara. |
| `sides`, `focus`, `dim` | La x de cada lado; la cara de quién cambia un `expr` sin hablante; el color `{ r, g, b }` de los miembros que no están hablando (sin él, nunca se oscurecen). |
| `backgrounds` | `defs` (`{ [key] = def }`) y `path` (`"Textures/bg/%s.png"` para una clave sin def), o `resolve(key)`; `open(layer)` para texturas que posee el llamador (una textura prestada se vuelve a abrir si su dueño la libera); `wait` (2 s); los ganchos `prepare`, `show`, `update`, `drawBehind`, `drawOver` y `close`. Una def es una capa o `{ layers = { ... } }`, una capa es `{ path, x, y, scale, scroll }`, y la `music` de una def suena mientras se muestra. |
| `music` | `{ play(key), fadeOut(sec), playing(), update(dt), stop() }`, todos opcionales. |
| `sound`, `blip`, `clickSound` | `fn(name)` reproduce un sonido por nombre; `blip(node)` devuelve el sonido de tecleo de un nodo del cuadro (`node.speaker`), nil para silencio; el clic de avance. |
| `cover` o `drawCover` | La textura del fundido (o `fn()` que devuelve una), estirada sobre la pantalla; o `fn(alpha)` que dibuja el fundido. |
| `drawBox`, `fadeSec`, `outcome` | `fn(dlg, scene)` que dibuja el cuadro en lugar de `dlg:draw()`; la duración del fundido (0.45 s); el resultado cuando simplemente se acaban las líneas (`"done"`). |
| `tr`, `speakerName`, `playerTag`, `playerName`, `random`, `onLine`, `order` | Se pasan al ejecutor. |
| `directions`, `replay` | Acotaciones adicionales, o sustitutas de las incorporadas; cómo las vuelve a aplicar un marcador reanudado. |
| `onBookmark`, `hooks` | `fn(mark)` en cada línea que empieza, con un marcador para guardar; `hooks = { prose, start, expr, choose, update, draw, dispose }`. |

| Método | Descripción |
| --- | --- |
| `Scene.new(opts)  -> scene` | Crea un reproductor. |
| `scene:play(scene, ctx, onDone, resume?)` | Inicia una escena. Las condiciones leen `ctx`, que también recibe las opciones elegidas; `onDone(result)` recibe el desenlace; `resume` es un marcador desde el que continuar. |
| `scene:update(dt)  -> "running" or "done"`, `scene:draw()` | Llámalas cada fotograma. |
| `scene:continue()`, `scene:waitFor(fn)` | Termina una retención; retiene hasta que `fn(dt)` devuelva true. |
| `scene:setBackground(key)`, `scene:setExpr(who, expr)`, `scene:shake(amp, dur)`, `scene:sound(name)` | Los efectos incorporados, para tus propias acotaciones. |
| `scene:bookmark()  -> mark`, `scene:active()  -> bool` | El marcador del ejecutor; si hay una escena en curso. |
| `scene:reset()`, `scene:dispose()` | Detiene la escena; libera lo que abrió la escena (y el cuadro, salvo que sea tuyo). |

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

El cuadro se dibuja a través de `VN.Dialogue`, así que su marco sigue la biblioteca de interfaz que da [UI.Pick](ui.md).
