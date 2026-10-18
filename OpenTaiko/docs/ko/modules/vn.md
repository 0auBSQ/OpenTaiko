<!-- modules/vn.md -->

# VN

비주얼 노벨 장면을 이루는 부품들입니다: 대화 상자, 스크립트 러너, 장면 플레이어. `VN.Scene`은 나머지 둘을 하나로 묶습니다. 스테이지가 장면을 직접 그린다면 러너와 상자만 쓰십시오.

## VN.Dialogue

타자기 방식의 대화 상자입니다. 화자 이름, 초상화 영역, 한 글자씩 드러나는 자동 줄바꿈 텍스트, 선택지를 가집니다. 스스로 동작하므로 매 프레임 `dlg:update(dt)`와 `dlg:draw()`를 호출하고, `"done"`을 보고하면 `dlg.result`를 읽으십시오. 상자는 스킨이 어떤 라이브러리를 골랐든 UI 라이브러리의 도형과 소리를 쓰며, 자체 모양(`theme` 옵션)으로 그려집니다.

스크립트는 노드 `{ name, text, portrait, choices }`의 목록입니다. `choices`(`{ label, value }`의 목록)가 있는 노드는 텍스트가 다 나오면 선택지를 보여 줍니다. 하나를 고르면 `dlg.result`가 그 `value`가 되고 대화가 끝납니다. 선택 없이 끝나는 스크립트는 `dlg.result`를 true로 설정합니다.

`text` 안의 태그는 타자기가 그 위치에 도달하면 실행됩니다.

| 태그 | 효과 |
| --- | --- |
| `{pause:sec}` | 표시를 멈춥니다(기본값 0.4초). |
| `{speed:cps}` | 표시 속도를 초당 글자 수로 바꿉니다. |
| `{shake:amp,dur}` | 화면을 흔듭니다: `onShake(amp, dur)`, `onShake` 핸들러가 주어지지 않았으면 3D 카메라. |
| `{sfx:name}`, `{voice:name}`, `{expr:name}` | `onSfx(name)`, `onVoice(name)`, `onExpr(name)`을 호출합니다(대사 중간의 표정 변화). |

| `opts` 필드 | 설명 |
| --- | --- |
| `gfont`, `gfontName` | 텍스트와 이름용 글리프 폰트(`TEXT:CreateGlyphCached`): 자동 줄바꿈되는 UTF-8 텍스트. 이름은 `gfont`로 폴백합니다. |
| `fonts` | 대신 쓰는 `{ name, text }` 텍스트 폰트로, 예전 방식입니다(ASCII). |
| `ui` | `"popui"`(기본값: 그림자와 이름 알약이 있는 둥근 상자) 또는 `"plain"`(평평한 패널). |
| `theme` | 상자의 색: `face`, `face2`, `outline`, `namePill`, `namePill2`, `nameText`, `text`, `choiceRow`, `choiceSel`, `choiceText`, `caret`, `shadow`, `textEdge`. |
| `box` | `{ x, y, w, h }`: 상자의 위치(기본적으로 화면 아래쪽을 가로지름). |
| `portraitSize`, `portraits` | 초상화 정사각형(0이면 숨김. 스테이지가 큰 스프라이트를 그릴 때용); 화자 이름별 `{ name = texture }`. |
| `cps` | 각 노드가 시작할 때의 표시 속도(기본값 42). |
| `mouse` | true: 왼쪽 클릭으로도 넘기고 확정합니다. |
| `advanceInput` | 추가 넘김 입력(북 패드)에 대해 true를 반환하는 함수. |
| `drawBox` | false이면 텍스트, 선택지, 캐럿만 그려 자신의 상자 그림 위에 겹칩니다. |
| `onBlip`, `blipEvery` | 타이핑 효과음: 드러난 글자 `blipEvery`개마다 `onBlip()`(3). |
| `onSfx`, `onVoice`, `onExpr`, `onShake` | 태그 콜백. |
| `sfx` | 넘기기와 선택용 이름별 소리로, [UI](ui.md)와 같습니다. |
| `opacity`, `caret` | 상자 전체의 불투명도, 그리고 넘김 표시를 숨기는 false. 둘 다 어느 프레임에서나 바꿀 수 있습니다. |

| 메서드 | 설명 |
| --- | --- |
| `Dialogue.new(opts)  -> dlg` | 상자를 만듭니다. |
| `dlg:start(script)` | 스크립트를 첫 노드부터 시작합니다. |
| `dlg:update(dt)  -> "running" or "done"` | 한 프레임을 처리합니다: Decide, Space(또는 클릭이나 `advanceInput`)는 대사 전체를 드러낸 다음 넘깁니다. 위아래 키로 선택지를 고릅니다. |
| `dlg:draw()` | 상자를 그립니다. |
| `dlg:active()  -> bool` | 스크립트가 실행 중인지 여부. |
| `dlg.result` | 끝난 뒤의 선택된 값, 또는 true. |
| `dlg:dispose()` | 구워 둔 상자 캔버스를 해제합니다. |

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

줄로 이루어진 스크립트를 차례로 진행합니다: 조건, 무작위 선택, 분기를 끼워 넣는 선택지, 그리고 호출한 쪽이 등록하는 연출 지시. 아무것도 그리지 않습니다. `VN.Scene`(아래)이나 여러분의 스테이지가 이를 진행시키고 본문 줄을 `VN.Dialogue` 상자에 올립니다.

줄은 테이블이며, 모든 필드는 선택 사항입니다.

| 필드 | 설명 |
| --- | --- |
| `text` | 본문으로, 상자의 인라인 태그를 쓸 수 있습니다. `choices`가 있고 `text`가 없는(또는 `""`인) 줄은 직전 본문 줄을 이미 읽은 상태로 화면에 남기고 그 아래에 선택지를 보여 줍니다. |
| `who`, `name` | 화자의 id; 표시할 이름으로, 화자 자신의 이름보다 우선합니다(`"???"`). |
| `expr` | 화자의 표정. |
| `choices`, `branch` | `{ { label, value }, ... }`; `branch = { [value] = { lines } }`는 선택 뒤에 끼워 넣어집니다. 고른 값은 `ctx.choice`와 `ctx.choices[value]`에 들어갑니다. |
| `when` | 줄을 재생할 때 true를 반환하는 `function(ctx)`. 오류를 일으키는 조건은 false로 칩니다. |
| `pick` | `{ { lines = { ... }, weight = n }, ... }`: 가중치(기본값 1)로 하나를 뽑으며, 같은 것이 두 번 연달아 나오지 않고, 뽑힌 것은 그 줄 뒤에 끼워 넣어집니다. |
| 그 밖의 키 | 연출 지시(`enter`, `bg`, `fade`, `sfx`, `flag`, ...)로, 그 키에 등록된 함수가 실행합니다. |

| `opts` 필드 | 설명 |
| --- | --- |
| `lines`, `ctx` | 스크립트(러너는 그 복사본을 진행합니다)와 조건이 읽는 테이블. |
| `tr` | `fn(s)`: 텍스트, 이름, 선택지 레이블을 번역합니다. 없으면 적힌 그대로 표시됩니다. |
| `speakerName` | `who`는 있고 `name`은 없는 줄이 표시하는 이름: 문자열, `{ [who] = name }` 테이블, 또는 `fn(who, line, ctx)`. |
| `playerTag`, `playerName` | 텍스트에서 플레이어 이름으로 바뀌는 태그(`"<player name>"`)와 그 이름: 문자열 또는 `fn(ctx)`이며, 기본값은 `ctx.playerName`. |
| `directions`, `order` | `{ [key] = fn(line, runner) }`, 그리고 이를 시도하는 순서(`Runner.ORDER`, 그다음 나머지 키를 정렬한 순서). |
| `onLine`, `onChoose` | 재생되는 모든 줄에 대해 그 연출 지시보다 먼저 호출되는 `fn(line, runner)`; 선택지가 골라질 때(재개 시의 것 포함) 호출되는 `fn(line, value, runner)`. |
| `random` | 뽑기에 쓰는 [0, 1) 범위의 수를 반환하는 `fn()`(기본값 `math.random`). |

연출 지시는 그 줄을 처리하지 않을 때 `false`를 반환하고(순서상 다음 것을 시도합니다), 다음 줄로 넘어가려면 nil이나 true를, 호출한 쪽이 다시 진행시킬 때까지 멈추려면 `"hold"`를, 스크립트를 끝내려면 `"end"`를 반환합니다. 호출한 쪽이 자신만의 `text` 연출 지시를 등록하지 않는 한, 텍스트나 선택지가 있는 줄은 `"line"`으로 진행을 멈춥니다.

| 메서드 | 설명 |
| --- | --- |
| `Runner.new(opts)  -> runner` | `opts.lines`에 대한 러너를 만듭니다. |
| `runner:step()  -> verdict, line` | 다음 정지 지점까지 진행합니다: `"line"`(보여 줄 본문 줄), `"hold"` 또는 `"end"`. |
| `runner:present(dlg, line?)  -> node, carried` | 현재 줄을 `VN.Dialogue` 상자에서 시작합니다. 선택지만 있는 줄은 직전 본문 줄을 전부 드러낸 상태로 이어받습니다. |
| `runner:choose(value)  -> bool` | 상자의 결과를 알립니다: 선택지 값이 기록되고 그 분기가 끼워 넣어집니다. |
| `runner:current()  -> line`, `runner:load(lines)` | 현재 줄; 스크립트를 처음부터 다시 시작합니다. |
| `runner:ahead(fn, otherwise)  -> value` | 현재 줄 이후의 줄들에 대해 `fn(line)`이 처음으로 내놓는 답(true 또는 false). 그 전에 스크립트가 끝나면 `otherwise`. |
| `runner:bookmark()  -> mark` | `{ idx, picks, rolls }`: 화면에 있는 줄, 고른 선택지, 뽑은 결과. |
| `runner:resume(mark, replay?)` | 같은 선택지와 뽑기 결과를 따라 책갈피를 꽂은 줄까지 조용히 되돌아갑니다. `replay = { [key] = fn(line, runner) }`는 상태를 남기는 연출 지시(배경, 플래그)를 다시 적용합니다. 다음 `step`이 책갈피의 줄을 보여 줍니다. |

헬퍼 `Runner.passes(line, ctx)`, `Runner.node(line, ctx, o, carry)`, `Runner.start(dlg, line, ctx, o, carry)`, `Runner.resolveChoice(lines, idx, ctx, value)`, `Runner.resolvePick(lines, idx, random, forced)`는 러너 없이 동작하며, 스크립트를 직접 진행하는 스테이지용입니다.

## VN.Scene

러너와 상자 위에 만든 장면 플레이어입니다: 로드가 끝나면 보이는 배경 레이어, 스프라이트로 된 등장인물, 화면 페이드와 흔들림, 그리고 화자별 타이핑 효과음이 있는 대화 상자. 스스로는 아무것도 로드하지 않습니다. 배경, 등장인물의 텍스처, 음악, 소리, 저장된 책갈피는 모두 호출한 쪽에서 옵니다.

장면은 `{ bg, music, lines }`입니다: 첫 배경, 음악(없으면 배경의 음악), 그리고 러너 형식의 줄로, 다음 연출 지시가 내장되어 있습니다.

| 줄 | 효과 |
| --- | --- |
| `{ enter = who, expr, side }` | 등장인물이 페이드(와 슬라이드)하며 나타납니다. 장면은 다 나타날 때까지 기다립니다. 이미 화면에 있는 인물은 표정만 바뀝니다. |
| `{ leave = who }` | 퇴장합니다. 장면은 다 퇴장할 때까지 기다립니다. |
| `{ expr = name }` | `who`의 표정, 또는 포커스된 인물의 표정. |
| `{ bg = key }` | 배경으로, 레이어가 로드되면(또는 `backgrounds.wait`초 뒤에) 표시됩니다. |
| `{ fade = "out" or "in", keepSprite }` | 장면 위의 페이드; `keepSprite = true`이면 등장인물을 밝게 유지합니다. |
| `{ shake = true }`, `{ sfx = name }` | 화면 흔들림(`shake`는 진폭이나 `{ amp, dur }`일 수도 있습니다); 소리. |
| `{ flag = name, value }` | `ctx.flags[name]`을 설정합니다(`value`가 없으면 true). |
| `{ outcome = value }` | 장면이 페이드아웃하고 `value`를 보고합니다. |

아직 등장하지 않은 인물의 줄은 먼저 그 인물을 페이드인시킵니다. 호출한 쪽의 연출 지시가 `"hold"`를 반환하면 장면은 `scene:continue()`가 호출될 때까지, 또는 `scene:waitFor(fn)`에 준 함수가 true를 반환할 때까지 멈춥니다(`fn(dt)`는 매 프레임 호출됩니다).

| `opts` 필드 | 설명 |
| --- | --- |
| `box` | `VN.Dialogue.new` 옵션; `onBlip`, `onShake`, `onExpr`, `onSfx`가 설정되지 않았으면 장면이 채웁니다. 또는 직접 만든 상자인 `dialogue`. |
| `cast` | `{ [who] = member }`. 인물: `body`(텍스처, 또는 표정마다 그림 하나를 주는 `fn(expr)`), `face`(`fn(expr)`, `faceX`, `faceY` 위치에 몸 위로 그림), `name`, `expr`(등장할 때의 표정), `side`(`"left"`, `"center"`, `"right"`) 또는 `x`, `y`와 `anchor`(`"top"` 또는 `"bottom"`), `scale` 또는 `height`(몸을 그 픽셀 수에 맞추는 배율), `fade`(0.35초), `slide`(px), `bounce`(0.36초, 0이면 없음), 그리고 표정이 바뀔 때의 튐에 쓰는 `squash`. |
| `sides`, `focus`, `dim` | 각 위치의 x; 화자 없는 `expr`이 바꾸는 표정의 주인; 말하지 않는 인물의 색 `{ r, g, b }`(없음: 어둡게 하지 않음). |
| `backgrounds` | `defs`(`{ [key] = def }`)와 `path`(def가 없는 키용 `"Textures/bg/%s.png"`), 또는 `resolve(key)`; 호출한 쪽이 소유한 텍스처용 `open(layer)`(빌린 텍스처는 소유자가 해제하면 다시 엽니다); `wait`(2초); `prepare`, `show`, `update`, `drawBehind`, `drawOver`, `close` 훅. def는 레이어 하나 또는 `{ layers = { ... } }`이고, 레이어는 `{ path, x, y, scale, scroll }`이며, def의 `music`은 그 배경이 보이는 동안 재생됩니다. |
| `music` | `{ play(key), fadeOut(sec), playing(), update(dt), stop() }`, 모두 선택 사항. |
| `sound`, `blip`, `clickSound` | `fn(name)`은 이름으로 소리를 재생합니다; `blip(node)`는 상자 노드(`node.speaker`)의 타이핑 효과음을 반환하며, nil이면 무음; 넘김 클릭음. |
| `cover` 또는 `drawCover` | 화면 전체로 늘어나는 페이드 텍스처(또는 그것을 반환하는 `fn()`); 또는 페이드를 그리는 `fn(alpha)`. |
| `drawBox`, `fadeSec`, `outcome` | `dlg:draw()` 대신 상자를 그리는 `fn(dlg, scene)`; 페이드 시간(0.45초); 줄이 그냥 다 떨어졌을 때의 결과(`"done"`). |
| `tr`, `speakerName`, `playerTag`, `playerName`, `random`, `onLine`, `order` | 러너에 전달됩니다. |
| `directions`, `replay` | 추가 연출 지시, 또는 내장 지시의 대체; 재개한 책갈피가 이를 다시 적용하는 방법. |
| `onBookmark`, `hooks` | 시작하는 각 줄에서 저장할 책갈피와 함께 호출되는 `fn(mark)`; `hooks = { prose, start, expr, choose, update, draw, dispose }`. |

| 메서드 | 설명 |
| --- | --- |
| `Scene.new(opts)  -> scene` | 플레이어를 만듭니다. |
| `scene:play(scene, ctx, onDone, resume?)` | 장면을 시작합니다. `ctx`는 조건이 읽고 선택지를 받습니다. `onDone(result)`은 결과를 받습니다. `resume`은 이어서 시작할 책갈피입니다. |
| `scene:update(dt)  -> "running" or "done"`, `scene:draw()` | 매 프레임 호출하십시오. |
| `scene:continue()`, `scene:waitFor(fn)` | 멈춤을 끝냅니다; `fn(dt)`가 true를 반환할 때까지 멈춥니다. |
| `scene:setBackground(key)`, `scene:setExpr(who, expr)`, `scene:shake(amp, dur)`, `scene:sound(name)` | 내장 효과로, 자신만의 연출 지시에 씁니다. |
| `scene:bookmark()  -> mark`, `scene:active()  -> bool` | 러너의 책갈피; 장면이 재생 중인지 여부. |
| `scene:reset()`, `scene:dispose()` | 장면을 멈춥니다; 장면이 연 것(그리고 직접 만든 것이 아니라면 상자)을 해제합니다. |

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

상자는 `VN.Dialogue`로 그려지므로, 그 테두리 장식은 [UI.Pick](ui.md)이 주는 UI 라이브러리를 따릅니다.
