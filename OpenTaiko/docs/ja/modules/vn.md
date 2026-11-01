<!-- modules/vn.md -->

# VN

ビジュアルノベルのシーンを構成する部品です。ダイアログボックス、スクリプトランナー、シーンプレイヤー。`VN.Scene` は他の 2 つを組み合わせたものです。ステージが自分でシーンを描く場合は、ランナーとボックスだけを使ってください。

## VN.Dialogue

タイプライター式のダイアログボックスです。話者名、立ち絵の領域、1 文字ずつ表示される折り返し付きのテキスト、選択肢を持ちます。自分で動作するため、毎フレーム `dlg:update(dt)` と `dlg:draw()` を呼び出し、`"done"` を報告したら `dlg.result` を読んでください。ボックスは、スキンがどのライブラリを選んでいても、UI ライブラリの図形とサウンドを使い、独自の見た目 (`theme` オプション) で描画されます。

スクリプトはノード `{ name, text, portrait, choices }` のリストです。`choices` (`{ label, value }` のリスト) を持つノードは、テキストを出し終えるとそれを表示します。1 つを選ぶと `dlg.result` がその `value` になり、ダイアログが終わります。選択肢なしで終わったスクリプトは `dlg.result` を true にします。

`text` の中のタグは、タイプライターがそこに達したときに実行されます。

| タグ | 効果 |
| --- | --- |
| `{pause:sec}` | 表示を止めます (既定は 0.4 秒)。 |
| `{speed:cps}` | 表示速度を 1 秒あたりの文字数で変更します。 |
| `{shake:amp,dur}` | 画面を揺らします: `onShake(amp, dur)`、`onShake` ハンドラーが指定されていなければ 3D カメラ。 |
| `{sfx:name}`, `{voice:name}`, `{expr:name}` | `onSfx(name)`、`onVoice(name)`、`onExpr(name)` を呼び出します (行の途中での表情の変更)。 |

| `opts` のフィールド | 説明 |
| --- | --- |
| `gfont`, `gfontName` | テキストと名前のグリフフォント (`TEXT:CreateGlyphCached`): 折り返し付きの UTF-8 テキスト。名前は `gfont` にフォールバックします。 |
| `fonts` | 代わりに使う `{ name, text }` のテキストフォント。古い方式です (ASCII)。 |
| `ui` | `"popui"` (既定: 影と名前のピルを持つ角丸のボックス) または `"plain"` (平らなパネル)。 |
| `theme` | ボックスの色: `face`、`face2`、`outline`、`namePill`、`namePill2`、`nameText`、`text`、`choiceRow`、`choiceSel`、`choiceText`、`caret`、`shadow`、`textEdge`。 |
| `box` | `{ x, y, w, h }`: ボックスの位置 (既定では画面下部の横いっぱい)。 |
| `portraitSize`, `portraits` | 立ち絵の正方形 (0 で非表示。ステージが大きなスプライトを描く場合用)。話者名ごとの `{ name = texture }`。 |
| `cps` | 各ノードの開始時の表示速度 (既定は 42)。 |
| `mouse` | true: 左クリックでも送りと決定ができます。 |
| `advanceInput` | 追加の送り入力 (太鼓のパッド) で true を返す関数。 |
| `drawBox` | false にすると、テキスト、選択肢、キャレットだけを、独自のボックス画像の上に描画します。 |
| `onBlip`, `blipEvery` | タイピング音: 表示された文字 `blipEvery` 個ごとに `onBlip()` (3)。 |
| `onSfx`, `onVoice`, `onExpr`, `onShake` | タグのコールバック。 |
| `sfx` | 送りと選択のための名前ごとのサウンド。[UI](ui.md) と同じです。 |
| `opacity`, `caret` | ボックス全体の不透明度と、送りマーカーを隠すための false。どちらも毎フレーム変更できます。 |

| メソッド | 説明 |
| --- | --- |
| `Dialogue.new(opts)  -> dlg` | ボックスを作ります。 |
| `dlg:start(script)` | スクリプトを最初のノードから開始します。 |
| `dlg:update(dt)  -> "running" or "done"` | 1 フレームを処理します: Decide、Space (またはクリックや `advanceInput`) で行全体を表示し、その後送ります。上下キーで選択肢を選びます。 |
| `dlg:draw()` | ボックスを描画します。 |
| `dlg:active()  -> bool` | スクリプトが実行中かどうか。 |
| `dlg.result` | 終了後の、選ばれた値または true。 |
| `dlg:dispose()` | 焼き込んだボックスのキャンバスを解放します。 |

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

行のスクリプトを順に進めます。ガード、ランダムな選択、分岐を差し込む選択肢、そして呼び出し元が登録するト書き。何も描画しません。`VN.Scene` (下記) か自分のステージがこれを進め、本文の行を `VN.Dialogue` のボックスに載せます。

行はテーブルで、どのフィールドも省略できます。

| フィールド | 説明 |
| --- | --- |
| `text` | 本文。ボックスのインラインタグを使えます。`choices` があり `text` がない (または `""`) 行は、直前の本文の行を読み終えた状態で画面に残し、その下に選択肢を出します。 |
| `who`, `name` | 話者の id。表示する名前で、話者自身の名前より優先されます (`"???"`)。 |
| `expr` | 話者の表情。 |
| `choices`, `branch` | `{ { label, value }, ... }`。`branch = { [value] = { lines } }` は選択の後に差し込まれます。選ばれた値は `ctx.choice` と `ctx.choices[value]` に入ります。 |
| `when` | 行を再生するときに true を返す `function(ctx)`。エラーを起こしたガードは false とみなされます。 |
| `pick` | `{ { lines = { ... }, weight = n }, ... }`: 重み (既定は 1) で選択肢を 1 つ抽選し、同じものが 2 回続くことはありません。抽選したものは行の後に差し込まれます。 |
| その他のキー | ト書き (`enter`、`bg`、`fade`、`sfx`、`flag`、...)。そのキーに登録された関数が実行します。 |

| `opts` のフィールド | 説明 |
| --- | --- |
| `lines`, `ctx` | スクリプト (ランナーはそのコピーを進めます) と、ガードが読むテーブル。 |
| `tr` | `fn(s)`: テキスト、名前、選択肢のラベルを翻訳します。ない場合は書かれたとおりに表示されます。 |
| `speakerName` | `who` があり `name` がない行が表示する名前: 文字列、`{ [who] = name }` テーブル、または `fn(who, line, ctx)`。 |
| `playerTag`, `playerName` | テキスト中でプレイヤーの名前に置き換えるタグ (`"<player name>"`) と、その名前: 文字列または `fn(ctx)` で、既定は `ctx.playerName`。 |
| `directions`, `order` | `{ [key] = fn(line, runner) }` と、それらを試す順序 (`Runner.ORDER`、次に残りのキーをソートしたもの)。 |
| `onLine`, `onChoose` | 再生されるすべての行に対し、そのト書きより前に呼ばれる `fn(line, runner)`。選択肢が選ばれたとき (再開時のものを含む) に呼ばれる `fn(line, value, runner)`。 |
| `random` | 抽選用に [0, 1) の数を返す `fn()` (既定は `math.random`)。 |

ト書きは、その行を扱わないときは `false` を返し (順序の次のものが試されます)、次の行へ進むときは nil か true、呼び出し元が再び進めるまで進行を止めるときは `"hold"`、スクリプトを終えるときは `"end"` を返します。呼び出し元が独自の `text` のト書きを登録しない限り、テキストか選択肢を持つ行は `"line"` として進行を止めます。

| メソッド | 説明 |
| --- | --- |
| `Runner.new(opts)  -> runner` | `opts.lines` に対するランナーを作ります。 |
| `runner:step()  -> verdict, line` | 次の停止点まで進みます: `"line"` (表示する本文の行)、`"hold"`、`"end"`。 |
| `runner:present(dlg, line?)  -> node, carried` | 現在の行を `VN.Dialogue` のボックスで開始します。選択肢だけの行は、直前の本文の行を全文表示した状態で引き継ぎます。 |
| `runner:choose(value)  -> bool` | ボックスの結果を伝えます。選択肢の値が記録され、その分岐が差し込まれます。 |
| `runner:current()  -> line`, `runner:load(lines)` | 現在の行。スクリプトを最初からやり直します。 |
| `runner:ahead(fn, otherwise)  -> value` | 現在の行より後の行に対して `fn(line)` が最初に返す答え (true または false)。先にスクリプトが終わった場合は `otherwise`。 |
| `runner:bookmark()  -> mark` | `{ idx, picks, rolls }`: 画面上の行、選ばれた選択肢、抽選結果。 |
| `runner:resume(mark, replay?)` | 同じ選択肢と抽選結果をたどりながら、しおりを挟んだ行まで黙って進みます。`replay = { [key] = fn(line, runner) }` は、状態を残すト書き (背景、フラグ) を再適用します。次の `step` でしおりの行が表示されます。 |

ヘルパー `Runner.passes(line, ctx)`、`Runner.node(line, ctx, o, carry)`、`Runner.start(dlg, line, ctx, o, carry)`、`Runner.resolveChoice(lines, idx, ctx, value)`、`Runner.resolvePick(lines, idx, random, forced)` はランナーなしで動作し、自分でスクリプトを進めるステージ用です。

## VN.Scene

ランナーとボックスの上に作られたシーンプレイヤーです。読み込みが終わると表示される背景レイヤー、スプライトのキャスト、画面のフェードと揺れ、話者ごとのタイピング音付きのダイアログボックス。自分では何も読み込みません。背景、キャストのテクスチャ、音楽、サウンド、保存したしおりは、すべて呼び出し元から渡されます。

シーンは `{ bg, music, lines }` です。最初の背景、音楽 (なければ背景のもの)、そしてランナーの形式の行で、次のト書きが組み込まれています。

| 行 | 効果 |
| --- | --- |
| `{ enter = who, expr, side }` | キャストのメンバーが、フェード (とスライド) しながら登場します。シーンは登場し終えるまで待ちます。すでに画面にいるメンバーは表情だけが変わります。 |
| `{ leave = who }` | 退場します。シーンは退場し終えるまで待ちます。 |
| `{ expr = name }` | `who`、またはフォーカス中のメンバーの表情。 |
| `{ bg = key }` | 背景。レイヤーの読み込みが終わると (または `backgrounds.wait` 秒後に) 表示されます。 |
| `{ fade = "out" or "in", keepSprite }` | シーンにかかるフェード。`keepSprite = true` でキャストを明るいまま残します。 |
| `{ shake = true }`, `{ sfx = name }` | 画面の揺れ (`shake` は振幅、または `{ amp, dur }` でも指定できます)。サウンド。 |
| `{ flag = name, value }` | `ctx.flags[name]` を設定します (`value` がない場合は true)。 |
| `{ outcome = value }` | シーンがフェードアウトし、`value` を報告します。 |

まだ登場していないキャストのメンバーの行は、まずそのメンバーをフェードインさせます。呼び出し元のト書きが `"hold"` を返すと、`scene:continue()` まで、または `scene:waitFor(fn)` に渡した関数が true を返すまで (`fn(dt)` は毎フレーム呼ばれます)、シーンは止まります。

| `opts` のフィールド | 説明 |
| --- | --- |
| `box` | `VN.Dialogue.new` のオプション。`onBlip`、`onShake`、`onExpr`、`onSfx` が設定されていなければシーンが埋めます。または独自のボックスである `dialogue`。 |
| `cast` | `{ [who] = member }`。メンバー: `body` (テクスチャ、または表情ごとに 1 枚の画像を返す `fn(expr)`)、`face` (`fn(expr)`。`faceX`、`faceY` の位置で体の上に描画)、`name`、`expr` (登場時の表情)、`side` (`"left"`、`"center"`、`"right"`) または `x`、`y` と `anchor` (`"top"` または `"bottom"`)、`scale` または `height` (体をそのピクセル数に収める拡大率)、`fade` (0.35 秒)、`slide` (px)、`bounce` (0.36 秒、0 でなし)、表情変更時のバウンド用の `squash`。 |
| `sides`, `focus`, `dim` | 各サイドの x。話者のいない `expr` が変える表情の持ち主。話していないメンバーの色 `{ r, g, b }` (なし: 暗くしない)。 |
| `backgrounds` | `defs` (`{ [key] = def }`) と `path` (def のないキー用の `"Textures/bg/%s.png"`)、または `resolve(key)`。呼び出し元が所有するテクスチャ用の `open(layer)` (借りたテクスチャは、所有者が解放すると開き直されます)。`wait` (2 秒)。`prepare`、`show`、`update`、`drawBehind`、`drawOver`、`close` のフック。def はレイヤーまたは `{ layers = { ... } }`、レイヤーは `{ path, x, y, scale, scroll }` で、def の `music` はその表示中に再生されます。 |
| `music` | `{ play(key), fadeOut(sec), playing(), update(dt), stop() }`。すべて省略可能です。 |
| `sound`, `blip`, `clickSound` | `fn(name)` は名前でサウンドを鳴らします。`blip(node)` はボックスのノード (`node.speaker`) のタイピング音を返し、nil なら無音です。送りのクリック音。 |
| `cover` または `drawCover` | フェードのテクスチャ (またはそれを返す `fn()`) で、画面全体に引き伸ばされます。またはフェードを描く `fn(alpha)`。 |
| `drawBox`, `fadeSec`, `outcome` | `dlg:draw()` の代わりにボックスを描く `fn(dlg, scene)`。フェード時間 (0.45 秒)。行がただ尽きたときの結果 (`"done"`)。 |
| `tr`, `speakerName`, `playerTag`, `playerName`, `random`, `onLine`, `order` | ランナーに渡されます。 |
| `directions`, `replay` | 追加のト書き、または組み込みのものの置き換え。再開したしおりがそれらを再適用する方法。 |
| `onBookmark`, `hooks` | 開始する各行で、保存用のしおりとともに呼ばれる `fn(mark)`。`hooks = { prose, start, expr, choose, update, draw, dispose }`。 |

| メソッド | 説明 |
| --- | --- |
| `Scene.new(opts)  -> scene` | プレイヤーを作ります。 |
| `scene:play(scene, ctx, onDone, resume?)` | シーンを開始します。`ctx` はガードが読み、選択肢を受け取ります。`onDone(result)` は結果を受け取ります。`resume` は続きから再開するためのしおりです。 |
| `scene:update(dt)  -> "running" or "done"`, `scene:draw()` | 毎フレーム呼び出します。 |
| `scene:continue()`, `scene:waitFor(fn)` | 停止を終えます。`fn(dt)` が true を返すまで止めます。 |
| `scene:setBackground(key)`, `scene:setExpr(who, expr)`, `scene:shake(amp, dur)`, `scene:sound(name)` | 組み込みの効果。独自のト書きで使います。 |
| `scene:bookmark()  -> mark`, `scene:active()  -> bool` | ランナーのしおり。シーンが再生中かどうか。 |
| `scene:reset()`, `scene:dispose()` | シーンを止めます。シーンが開いたもの (と、自分のものでなければボックス) を解放します。 |

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

ボックスは `VN.Dialogue` で描画されるため、その外枠は [UI.Pick](ui.md) が与える UI ライブラリに従います。
