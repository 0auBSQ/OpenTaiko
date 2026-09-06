<!-- api/graphics.md -->

# グラフィックとテキスト

テクスチャ、キャンバス、クリッピング、テキスト、動画、色、グラデーションマップ、サイズ。すべての位置とサイズは論理スクリーンピクセル、つまりスクリプトが描画する座標空間です (`INPUT:GetSurfaceWidth()` / `GetSurfaceHeight()` を参照)。

テクスチャ、キャンバス、テキスト、動画、グラデーションのハンドルは作成したスクリプトが所有し、ゲームはそのスクリプトをアンロードするときにハンドルを破棄します。ハンドルを早めに解放するには自分で `Dispose()` を呼んでください。

## テクスチャ

### TEXTURE

画像ファイルをテクスチャハンドルに読み込みます。

<div class="callout warn">
相対パスはスクリプトのディレクトリを基準に解決されます。FromAbsolutePath 系は完全なパスを取ります。ファイルがない場合は何も描画しない空のハンドルを返します。CreateTexture は非同期に読み込みます。バックグラウンドのデコードとアップロードが完了するまで、ハンドルは何も描画せず、Width と Height は 0 を報告します。サイズやピクセルがすぐに必要な場合は CreateTextureSync を使ってください。オプションテーブルは { maxSize = N } を受け付け、デコード時に画像の長辺が最大 N ピクセルになるよう縮小します。
</div>

| メソッド | 説明 |
| --- | --- |
| `TEXTURE:CreateTexture()  -> texture` | 画像を持たない空のハンドルを作成します。 |
| `TEXTURE:CreateTexture(path)  -> texture` | スクリプトのディレクトリからの相対パスで画像を非同期に読み込みます。 |
| `TEXTURE:CreateTexture(path, options)  -> texture` | 同上。オプションテーブル (`{ maxSize = N }`) 付き。 |
| `TEXTURE:CreateTextureSync(path)  -> texture` | 画像を同期的に読み込みます。戻った時点でサイズとピクセルが利用できます。 |
| `TEXTURE:CreateTextureFromAbsolutePath(path)  -> texture` | ファイルシステムの完全なパスから画像を読み込みます。 |
| `TEXTURE:CreateTextureFromAbsolutePath(path, options)  -> texture` | 同上。オプションテーブル (`{ maxSize = N }`) 付き。 |
| `TEXTURE:Exists(path)  -> bool` | スクリプトのディレクトリからの相対パスにファイルが存在するかどうかを返します。 |

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

### テクスチャハンドル

`TEXTURE` ファクトリ、`text:GetText` / `GetVerticalText`、`video.Texture` から返される描画可能な 2D 画像です。

<div class="callout warn">
アンカー名: topleft、top、topright、left、center、right、bottomleft、bottom、bottomright (大文字小文字を区別しません。不明な名前は topleft にフォールバックします)。アンカー付きの描画は現在のスケールを考慮します。ブレンドモード名: Normal、Add、Multi、Sub、Screen。ラップモード名: Edge、Border、Repeat、Mirror。新しいハンドルの既定は Repeat です。空のハンドルではすべてのメソッドが何もせず、ゲッターは以下に示す既定値を返します。
</div>

| メソッド | 説明 |
| --- | --- |
| `texture:Draw(x, y)  -> nil` | テクスチャ全体を、左上を (x, y) に合わせて描画します。 |
| `texture:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | ソースの部分矩形 (テクスチャピクセル単位) を、左上を (x, y) に合わせて描画します。 |
| `texture:DrawAtAnchor(x, y, anchor)  -> nil` | 指定したアンカー点が (x, y) に来るようにテクスチャ全体を描画します。 |
| `texture:DrawRectAtAnchor(x, y, rect_x, rect_y, rect_width, rect_height, anchor)  -> nil` | 指定したアンカー点が (x, y) に来るようにソースの部分矩形を描画します。 |
| `texture.Loaded  -> bool` | ハンドルがテクスチャをラップしているとき true。ゲームはファイルを見つけた時点で、非同期読み込みの完了前にこれを設定します。 |
| `texture.Width  -> int` | ピクセル幅。空のハンドルでは -1、非同期読み込み中は 0。 |
| `texture.Height  -> int` | ピクセル高さ。空のハンドルでは -1、非同期読み込み中は 0。 |
| `texture.Pointer  -> int` | ネイティブの GL テクスチャ id。なければ 0。 |
| `texture:GetScale()  -> vector2` | 現在の描画スケール (vector2 の `X`、`Y`)。 |
| `texture:GetOpacity()  -> number` | 現在の不透明度 (0..1)。空のハンドルでは -1。 |
| `texture:GetColor()  -> number, number, number` | 現在のティントを 0..1 の 3 つの値 (赤、緑、青) で返します。 |
| `texture:GetRotation()  -> number` | 現在の回転 (度)。 |
| `texture:GetBlendMode()  -> string` | 現在のブレンドモード名。 |
| `texture:GetWrapMode()  -> string` | 現在のラップモード名。 |
| `texture:SetScale(scale_x, scale_y)  -> nil` | 水平・垂直の描画スケールを設定します (1 = 原寸)。 |
| `texture:SetOpacity(opacity)  -> nil` | 不透明度を設定します (0..1)。 |
| `texture:SetColor(color)  -> nil` | `COLOR` 値からティントを設定します。ハンドルは色のアルファを無視します。透明度は SetOpacity で設定してください。 |
| `texture:SetColor(red, green, blue)  -> nil` | 0..1 の 3 つの値からティントを設定します。 |
| `texture:SetRotation(angle)  -> nil` | テクスチャ中心を軸とする回転を度で設定します。 |
| `texture:SetBlendMode(mode)  -> nil` | ブレンドモードを名前で設定します (大文字小文字を区別しません)。不明な名前はハンドルが無視します。 |
| `texture:SetWrapMode(mode)  -> nil` | ラップモードを名前で設定します (大文字小文字を区別しません)。不明な名前はハンドルが無視します。 |
| `texture:SetUseNoiseEffect(enabled)  -> nil` | 有効にすると、毎回の描画でテクスチャの色をアニメーションするランダムなグレースケールノイズに置き換え、アルファは維持します。 |
| `texture:SetGradientMap(gradient)  -> nil` | このテクスチャのすべての描画に `GRADIENT` マップを適用します (GRADIENT を参照)。 |
| `texture:ClearGradientMap()  -> nil` | テクスチャごとのグラデーションマップを取り除きます。 |
| `texture:Dispose()  -> nil` | テクスチャを解放します。 |

## キャンバスとクリッピング

### CANVAS

Lua が CPU 上で編集し、1 つのテクスチャとしてアップロードする書き込み可能なピクセルサーフェスを作成します。

<div class="callout warn">
キャンバスは、Lua がピクセルを設定し (SetPixel、FillRect など)、Upload で GPU に送るテクスチャです。ソフトウェアレンダリングと UI 形状のベイクに向いており、スクリプトが一度描いて一度アップロードし、その結果を毎フレーム描画します。新しいキャンバスは完全に透明な状態から始まります。
</div>

| メソッド | 説明 |
| --- | --- |
| `CANVAS:CreateCanvas(width, height)  -> canvas` | 指定サイズ (最小 1x1) の透明なキャンバスを作成します。 |

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

### キャンバスハンドル

`CANVAS:CreateCanvas` が返す書き込み可能な RGBA ピクセルバッファで、テクスチャと同様に描画できます。

<div class="callout warn">
色の引数 r、g、b、a は 0-255 の整数です。ピクセル編集はダーティ矩形に蓄積され、Upload を呼んだときにのみ GPU に届きます (BlitPacked は自分でアップロードします)。描画座標は整数です。アンカー名はテクスチャハンドルと同じです。
</div>

| メソッド | 説明 |
| --- | --- |
| `canvas.Width  -> int` | ピクセル幅。 |
| `canvas.Height  -> int` | ピクセル高さ。 |
| `canvas.Pointer  -> int` | ネイティブの GL テクスチャ id。なければ 0。 |
| `canvas:SetPixel(x, y, r, g, b, a)  -> nil` | 1 ピクセルを設定します。範囲外の座標はキャンバスが無視します。 |
| `canvas:FillRect(x, y, w, h, r, g, b, a)  -> nil` | 軸に平行な矩形を塗りつぶします。キャンバスにクリップされます。 |
| `canvas:FillCircle(cx, cy, radius, r, g, b, a)  -> nil` | 指定した半径 (ピクセル) の円板を塗りつぶします。 |
| `canvas:StrokeLine(x0, y0, x1, y1, radius, r, g, b, a)  -> nil` | 指定した半径の円板を重ねて太い線を描きます。 |
| `canvas:PasteTexture(texture, x, y)  -> nil` | テクスチャを左上が (x, y) になるようキャンバスにアルファ合成します。キャンバスはテクスチャのピクセルをテクスチャハンドルごとに 1 回 GPU から読み戻します。これは遅い操作で、セットアップコードに置くものです。 |
| `canvas:PasteTextureTransformed(texture, x, y, scale, rotationDeg, anchor)  -> nil` | `scale` で拡大縮小し `rotationDeg` で時計回りに回転したテクスチャを、最近傍サンプリングでアルファ合成します。anchor が "center" ならテクスチャの中心が (x, y) に、それ以外の値なら左上が (x, y) に来ます。 |
| `canvas:Clear(r, g, b, a)  -> nil` | キャンバス全体を 1 色で塗りつぶします。 |
| `canvas:ClearTransparent()  -> nil` | キャンバス全体を完全に透明にリセットします。 |
| `canvas:CopyFrom(other)  -> nil` | 同じサイズの別のキャンバスのピクセルをこのキャンバスにコピーします。サイズが異なる場合、この呼び出しは何もしません。 |
| `canvas:BlitPacked(data, count)  -> nil` | 1 始まりの Lua 配列に行優先で並んだパックされた `0xRRGGBB` 整数からサーフェスを埋め (ピクセルは不透明になり、負の値は透明なピクセルになります)、アップロードします。 |
| `canvas:Upload()  -> nil` | 保留中の編集 (変更された領域のみ) を GPU に送ります。変更がなければ何もしません。 |
| `canvas:Draw(x, y)  -> nil` | キャンバスを、左上を (x, y) に合わせて描画します。 |
| `canvas:DrawRect(x, y, rect_x, rect_y, rect_width, rect_height)  -> nil` | ソースの部分矩形 (キャンバスピクセル単位) を、左上を (x, y) に合わせて描画します。 |
| `canvas:DrawAtAnchor(x, y, anchor)  -> nil` | 指定したアンカー点が (x, y) に来るようにキャンバスを描画します。 |
| `canvas:SetScale(scale_x, scale_y)  -> nil` | 水平・垂直の描画スケールを設定します。 |
| `canvas:SetOpacity(opacity)  -> nil` | 不透明度を設定します (0..1)。 |
| `canvas:SetColor(red, green, blue)  -> nil` | 0..1 の 3 つの値からティントを設定します。 |
| `canvas:Dispose()  -> nil` | キャンバスの GPU テクスチャと CPU バッファを解放します。 |

### GRAPHICS

スクロールパネル用のシザークリッピングです。

<div class="callout warn">
SetClip は論理スクリーン座標を取り、現在のビューポートに対応付けるため、レンダースケーリングやレターボックスの下でもクリッピングは同じです。SetClip と ClearClip の間に矩形の外へ描画されたピクセルはすべて GPU が破棄します。SetClip は前の矩形を置き換えます (スタックはありません)。SetClip と ClearClip は対にしてください。ゲームは毎フレームの開始時にシザーを無効化します。
</div>

| メソッド | 説明 |
| --- | --- |
| `GRAPHICS:SetClip(x, y, w, h)  -> nil` | 指定した矩形へのクリッピングを有効にします。 |
| `GRAPHICS:ClearClip()  -> nil` | クリッピングを無効にします。 |

```lua
GRAPHICS:SetClip(100, 200, 600, 400)
for i, row in ipairs(rows) do
    row.tex:Draw(100, 200 + (i - 1) * 48 - scrollY)
end
GRAPHICS:ClearClip()
```

## テキスト

### TEXT

スキンのメインフォントのフォントレンダラーを作成します。

<div class="callout warn">
Create は文字列全体をキャッシュされたテクスチャに描画するテキストハンドルを返します。めったに変わらないラベルに使ってください。CreateGlyphCached は文字ごとに 1 つのテクスチャをキャッシュし、描画時に文字列を組み立てるグリフテキストハンドルを返します。頻繁に変わるテキスト (タイマー、スコア、入力中の文字) と折り返しブロックに使ってください。スタイル引数は bold、italic、underline、strikeout のいずれかです (大文字小文字を区別しません。他の値は通常を意味します)。1 つのスクリプト内で、同じファクトリメソッドを最初はスタイル引数なしで呼び、後からちょうど 1 つのスタイル引数で呼ぶと、同梱の NLua のバージョンは失敗します。スタイルを一切渡さないか、最初の呼び出しから常に渡すか、あるいは 2 つのスタイルトークン (例えば "bold", "regular") を渡してください。
</div>

| メソッド | 説明 |
| --- | --- |
| `TEXT:Create(size, ...style)  -> text` | 指定したピクセルサイズで文字列全体のテキストレンダラーを作成します。 |
| `TEXT:CreateGlyphCached(size, ...style)  -> glyphText` | 指定したピクセルサイズでグリフ合成のテキストレンダラーを作成します。 |

#### インラインの色タグ

両方のレンダラーは文字列内のこれらのタグを解釈します。タグは入れ子にでき、閉じられていないタグは文字列の末尾まで続きます。計測関数はタグを無視します。

| タグ | 効果 |
| --- | --- |
| `<c.#rrggbb>` ... `</c>` | 塗り色。 |
| `<c.#rrggbb.#rrggbb>` ... `</c>` | 塗り色と縁取り色。 |
| `<g.#rrggbb.#rrggbb>` ... `</g>` | 垂直グラデーションの塗り (上の色、下の色)。 |

### テキストハンドル

`TEXT:Create` が返す、文字列全体を描画するレンダラーです。

<div class="callout warn">
GetText と GetVerticalText は、文字列、塗り色、縁取り色、サイズ制限の一意な組み合わせごとに 1 つのテクスチャをキャッシュし、ハンドルを破棄するまで保持します。そのため毎フレーム変わる文字列は毎フレームテクスチャを追加し、GPU メモリをリークします。そのようなテキストは DrawDirect かグリフテキストハンドルで描画してください。forecolor と backcolor は COLOR 値で、既定は白の塗りと不透明な黒の縁取りです。centered フラグは複数行の文字列の各行を中央揃えにします。キャッシュキーにはこれが含まれないため、ある文字列に対する最初の呼び出しが決定します。
</div>

| メソッド | 説明 |
| --- | --- |
| `text:GetText(text, centered, max_width, forecolor, backcolor)  -> texture` | 横書きの文字列をキャッシュされたテクスチャに描画します。`text` より後の引数はすべて任意です。描画されたテクスチャが `max_width` より広い場合は、収まるよう水平方向に圧縮して描画されます。 |
| `text:GetVerticalText(text, centered, max_height, forecolor, backcolor)  -> texture` | 縦に積んだ文字列をキャッシュされたテクスチャに描画します。描画されたテクスチャが `max_height` より高い場合は、収まるよう垂直方向に圧縮して描画されます。 |
| `text:DrawDirect(text, x, y, r, g, b, opacity, spacing)  -> number` | 文字ごとのキャッシュから文字列をグリフ単位で描画し、最後のグリフの後の x 位置を返します。`r`、`g`、`b` は 0-255 (既定 255)、`opacity` は 0..1 (既定 1)、`spacing` はグリフ間の間隔のピクセル数 (既定 -6) です。 |
| `text:Dispose()  -> nil` | フォントとキャッシュされたすべてのテクスチャを解放します。 |

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

### グリフテキストハンドル

`TEXT:CreateGlyphCached` が返す、グリフ合成のレンダラーです。

<div class="callout warn">
forecolor と backcolor は COLOR 値で、既定は白の塗りと黒の縁取りです。位置より後の引数はすべて任意です。maxWidth (0 = 制限なし) は行全体が収まるよう各グリフを水平方向に圧縮します。anchor はテクスチャハンドルと同じ 9 つの名前を使います。'\n' は改行し、Draw は行を左揃えで積みます。Draw は描画されたボックスの右端の x 座標を返します。ボックスの形状は GetText のテクスチャと一致する (インクの左右と下に 25 px のパディング) ため、同じ位置に描いたグリフテキストと GetText のテクスチャは揃います。
</div>

| メソッド | 説明 |
| --- | --- |
| `glyphText.LineHeight  -> number` | 複数行テキストの行ピッチ (ピクセル)。 |
| `glyphText.BoxHeight  -> number` | 1 行分のボックスの高さ (インクとパディング)。1 行の GetText テクスチャと一致します。 |
| `glyphText:Measure(text, scale)  -> number` | 最も広い行のインク幅に `scale` (既定 1) を掛けたもの。 |
| `glyphText:Draw(text, x, y, forecolor, backcolor, opacity, scale, maxWidth, anchor, scaleY, rotationDeg)  -> number` | テキストを描画します。`opacity` は 0..1 (既定 1)、`scale` (既定 1)、`scaleY` は 0 より大きいとき垂直スケールを上書きし、`rotationDeg` はブロック全体を (x, y) の周りに回転させます。ボックスの右端の x を返します。 |
| `glyphText:SetClipY(y0, y1)  -> nil` | 以降の直立した Draw 呼び出しをスクリーン空間の垂直帯 y0..y1 に制限します。Draw は帯の外のグリフをスキップし、端にかかるグリフを切り取ります。y1 <= y0 を渡すと解除されます。回転した描画は帯を無視します。 |
| `glyphText:WrapToLines(text, wrapWidth, scale)  -> string[]` | `wrapWidth` で単語単位に折り返し、各行をプレーンな文字列 (タグを除去) として返します。 |
| `glyphText:MeasureWrapped(text, wrapWidth, scale, lineSpacing)  -> number` | 描画せずに、DrawWrapped が使う高さを返します。`lineSpacing` は行ピッチに掛かります (既定 1)。 |
| `glyphText:DrawWrapped(text, x, y, wrapWidth, forecolor, backcolor, opacity, scale, lineSpacing)  -> number` | `wrapWidth` で単語単位に折り返し、(x, y) から左揃えで描画します。描画した高さを返します。 |
| `glyphText:Dispose()  -> nil` | フォントとキャッシュされたすべてのグリフを解放します。 |

折り返しは、空白の位置、CJK 文字の間、そして折り返し幅より広い単語の内部で行われます。

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

## 動画

### VIDEO

動画ファイルを再生可能なデコーダハンドルに読み込みます。

<div class="callout warn">
パスはスクリプトのディレクトリからの相対パスです。デコーダはバックグラウンドスレッドで開かれます。準備ができるまでハンドルは何も描画せず、Start、シーク、速度の呼び出しをキューに入れ、デコーダが接続された時点でそれらを適用します。ファイルがない場合は空のハンドルを返します。
</div>

| メソッド | 説明 |
| --- | --- |
| `VIDEO:CreateVideo(path)  -> video` | 動画ファイルを開き、そのハンドルを返します。 |

### 動画ハンドル

`VIDEO:CreateVideo` が返す動画デコーダです。

<div class="callout warn">
位置はミリ秒、Duration は秒です。現在のフレームをテクスチャハンドルとして得るには毎フレーム Texture を読みます。そのハンドルは動画自身のフレームテクスチャをラップしています。描画に使い、破棄は動画ハンドルに任せてください。SetSpeed は 0 以下の値を無視します。
</div>

| メソッド | 説明 |
| --- | --- |
| `video:Start()  -> nil` | 再生を開始します。 |
| `video:Resume()  -> nil` | Pause の後に再生を再開します。 |
| `video:Pause()  -> nil` | 再生を一時停止します。 |
| `video:Stop()  -> nil` | 再生を停止します。 |
| `video:Reset()  -> nil` | 先頭にシークします。 |
| `video.Width  -> int` | フレームのピクセル幅。デコーダの準備ができるまで -1。 |
| `video.Height  -> int` | フレームのピクセル高さ。デコーダの準備ができるまで -1。 |
| `video.Duration  -> number` | 合計時間 (秒)。デコーダの準備ができるまで 1。 |
| `video.DurationMs  -> number` | 合計時間 (ミリ秒)。 |
| `video.Texture  -> texture` | 現在デコードされているフレーム。 |
| `video:IsFinished()  -> bool` | ストリームが終了し、フレームが残っていないとき true。 |
| `video:GetTimestampMs()  -> number` | 現在の再生位置 (ミリ秒)。 |
| `video:SetTimestampMs(ms)  -> nil` | ミリ秒単位の位置にシークします。 |
| `video:GetSpeed()  -> number` | 現在の再生速度の倍率 (1 = 通常)。 |
| `video:SetSpeed(speed)  -> nil` | 再生速度の倍率を設定します。 |
| `video:GetPlayPosition()  -> number` | 現在の位置 (秒) (GetTimestampMs / 1000 の古い名前)。 |
| `video:SetPlayPosition(seconds)  -> nil` | 秒単位の位置にシークします (SetTimestampMs の古い名前)。 |
| `video:GetPlaySpeed()  -> number` | GetSpeed の古い名前。 |
| `video:SetPlaySpeed(speed)  -> nil` | SetSpeed の古い名前。 |
| `video:Dispose()  -> nil` | デコーダとそのフレームテクスチャを解放します。 |

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

## 色、グラデーション、サイズ

### COLOR

色の値を作成します。

<div class="callout warn">
チャンネルは 0-255 です。色を取るテクスチャ、キャンバス、テキストのメソッドはこの値を受け付けます。
</div>

| メソッド | 説明 |
| --- | --- |
| `COLOR:CreateColorFromRGBA(r, g, b, a)  -> color` | 赤、緑、青と任意のアルファ (既定 255) から色を作成します。 |
| `COLOR:CreateColorFromARGB(a, r, g, b)  -> color` | アルファ、赤、緑、青から色を作成します。 |
| `COLOR:CreateColorFromHex(value)  -> color` | 接頭辞なしの `AARRGGBB` 16 進文字列 (例: `"ff2080ff"`) を解析します。6 桁の文字列はアルファ 0 になります。解析できない文字列は不透明な白になります。 |

### 色ハンドル

`COLOR` ファクトリが返す色の値です。

| メソッド | 説明 |
| --- | --- |
| `color.R  -> int` | 赤チャンネル、0-255 (読み書き可能)。 |
| `color.G  -> int` | 緑チャンネル、0-255 (読み書き可能)。 |
| `color.B  -> int` | 青チャンネル、0-255 (読み書き可能)。 |
| `color.A  -> int` | アルファチャンネル、0-255 (読み書き可能)。 |

### GRADIENT

グラデーションマップは、各ピクセルの輝度をカラーランプに対応付けることで、1 つのテクスチャまたはすべての描画に対してテクスチャ描画の色を置き換えます。

<div class="callout warn">
Create は 2 つ以上のストップのテーブルと、ブレンド量を取ります。各ストップは { 位置 0-1, r 0-255, g 0-255, b 0-255 [, a 0-255] } の配列です。ブレンド量は 0 = 効果なし、1 = 完全に置き換え、その間の値は混合 (既定 1) です。ストップが 2 つ未満だとエラーになります。同一のストップとブレンドはプログラム全体で 1 つのキャッシュされた GPU テクスチャを再利用します。1 つのテクスチャには texture:SetGradientMap で、すべてのテクスチャ描画には SetActive で適用します。
</div>

| メソッド | 説明 |
| --- | --- |
| `GRADIENT:Create(stops, blend)  -> gradient` | カラーストップとブレンド量からグラデーションマップを構築 (または再利用) します。 |
| `GRADIENT:SetActive(gradient)  -> nil` | ClearActive までのすべてのテクスチャ描画にグラデーションを適用します。 |
| `GRADIENT:ClearActive()  -> nil` | グローバルなグラデーションマップを取り除きます。 |

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

### グラデーションハンドル

`GRADIENT:Create` が返すグラデーションマップです。

| メソッド | 説明 |
| --- | --- |
| `gradient.BlendStrength  -> number` | ブレンド量、0..1 (読み取り専用)。 |
| `gradient:Dispose()  -> nil` | ハンドルを解放します。共有の GPU テクスチャはキャッシュされたままです。 |

### SIZE

幅と高さの値オブジェクトを作成します。

| メソッド | 説明 |
| --- | --- |
| `SIZE:CreateSize(width, height)  -> size` | サイズオブジェクトを作成します。 |

### サイズハンドル

`SIZE:CreateSize` が返す幅と高さの組です。

| メソッド | 説明 |
| --- | --- |
| `size.Width  -> int` | 幅 (読み書き可能)。 |
| `size.Height  -> int` | 高さ (読み書き可能)。 |
