<!-- api/aviutl2.md -->

# AviUtl2 プロジェクト

`AVIUTL2` グローバルは、動画編集ソフト AviUtl2 で保存したプロジェクトファイル (`.aup2`) を再生します。ゲームはプロジェクトを読み込み、そのオブジェクト、移動、エフェクトをその場で描画します。そのためスキンは、レンダリング済みの動画の代わりに、プロジェクトとその画像の形でアニメーションを同梱できます。

プロジェクトのハンドルは読み込んだスクリプトが所有し、ゲームはそのスクリプトをアンロードするときにハンドルを破棄します。ハンドルを早めに解放するには自分で `Dispose()` を呼んでください。

<div class="callout warn">
ゲームは独自のレンダラーでプロジェクトを描画します。位置、サイズ、回転、移動カーブ、カメラ制御、よく使われるエフェクトはエディタと同じ結果になります。ランダム移動と振動は異なる乱数値を使い、一部のエフェクトは近似です。プロジェクトを読み込んだら GetUnsupported と GetApproximated を読み、ゲームが省いたものや簡略化したものを確認してください。
</div>

## 読み込み

### AVIUTL2

| メソッド | 説明 |
| --- | --- |
| `AVIUTL2:LoadProject(path)  -> project` | プロジェクトファイルを開き、そのハンドルを返します。パスはスクリプトのディレクトリからの相対パスです。 |
| `AVIUTL2:LoadProjectFromAbsolutePath(path)  -> project` | 完全なパスからプロジェクトファイルを開きます。 |

<div class="callout warn">
プロジェクトには、保存したマシンでの完全なパスが記録されています。ゲームは各画像、動画、音声、モデルをプロジェクトファイルの近くで探します。まず記録されたパスとプロジェクト自身のパスに共通するフォルダを手がかりにし、次にファイル名で探します。プロジェクトのファイルは、プロジェクトのフォルダかそのサブフォルダに置いてください。プロジェクトが見つからない、または読めない場合は、Loaded が false のハンドルが返ります。
</div>

## プロジェクトハンドル

`AVIUTL2:LoadProject` が返すプロジェクトです。再生用のメソッドは動画ハンドルのものと同じ名前です。

### 再生

| メソッド | 説明 |
| --- | --- |
| `project:Start()  -> nil` | 先頭から再生を開始します。 |
| `project:Resume()  -> nil` | Pause の後に再生を再開します。 |
| `project:Pause()  -> nil` | 音声も含めて再生を一時停止します。 |
| `project:Stop()  -> nil` | 再生を停止します。 |
| `project:Reset()  -> nil` | 先頭にシークします。 |
| `project.IsPlaying  -> bool` | プロジェクトが再生中で、まだ終端に達していないあいだ true。 |
| `project:IsFinished()  -> bool` | ループしないプロジェクトが終端に達すると true。 |
| `project:GetTimestampMs()  -> number` | 現在の位置 (ミリ秒)。 |
| `project:SetTimestampMs(ms)  -> nil` | ミリ秒単位の位置にシークします。 |
| `project:GetFrame()  -> number` | 現在の位置 (プロジェクトのフレーム単位)。 |
| `project:SetFrame(frame)  -> nil` | 指定したフレームにシークします。 |
| `project:GetSpeed()  -> number` | 現在の再生速度の倍率 (1 = 通常)。 |
| `project:SetSpeed(speed)  -> nil` | 再生速度の倍率を設定します。 |
| `project:GetLoop()  -> bool` | プロジェクトが終端で最初から再生し直す設定なら true。 |
| `project:SetLoop(loop)  -> nil` | プロジェクトが終端で最初から再生し直すようにします。 |
| `project:GetVolumePercent()  -> number` | プロジェクトの音声の音量 (パーセント)。 |
| `project:SetVolumePercent(vol)  -> nil` | プロジェクトの音声の音量をパーセントで設定します。 |
| `project:Dispose()  -> nil` | プロジェクトと、その画像、動画、音声を解放します。 |

### 描画

| メソッド | 説明 |
| --- | --- |
| `project:Draw(x, y)  -> nil` | 現在のフレームを、左上隅を (x, y) に合わせ、SetScale で設定したスケールで描画します。 |
| `project:DrawFit(x, y, w, h)  -> nil` | 現在のフレームを、縦横比を保ったまま矩形に収まる最大の大きさで、矩形の中央に描画します。 |
| `project:SetScale(scaleX, scaleY)  -> nil` | Draw が使うスケールを設定します (1 = プロジェクト本来のサイズ)。 |
| `project:SetOpacity(opacity)  -> nil` | フレーム全体の不透明度を 0 から 1 で設定します。 |
| `project:SetBackground(r, g, b, a)  -> nil` | オブジェクトの背後を指定した色で塗りつぶします。各値は 0 から 255 です。アルファが 0 ならフレームは透明のままです。 |
| `project:ClearBackground()  -> nil` | プロジェクトファイルが指定する背景に戻します。 |
| `project.HasProjectBackground  -> bool` | シーンが自身の背景色を持っていれば true。 |
| `project.ProjectBackground  -> int` | シーンの背景色 (0xRRGGBB)。背景色がなければ -1。 |

<div class="callout warn">
背景色のないシーンは、オブジェクトが描かれていない部分が透明です。そのためプロジェクトは、スクリプトが先に描いたものの上に重ねて描画されます。エディタはこのようなシーンを黒の上に表示します。同じ見た目にするには、先に黒い矩形を描くか、SetBackground(0, 0, 0, 255) を呼んでください。
</div>

### スクリプトが描画する画像

スクリプトはプロジェクト内の画像を自分の描画で置き換えられます。たとえば、アニメーションする枠の中にプレイヤーのネームプレートを表示できます。

| メソッド | 説明 |
| --- | --- |
| `project:SetImageDrawer(fileName, width, height, draw)  -> nil` | 画像ファイル `fileName` を表示するすべてのオブジェクトが、関数 `draw` の描いたものを表示します。ゲームは毎フレーム `draw` を呼び、描画領域は `width` × `height` で、その左上隅が (0, 0) です。 |
| `project:ClearImageDrawer(fileName)  -> nil` | オブジェクトは再び画像ファイルを表示します。 |

ゲームは、そのようなオブジェクトを持つレイヤーごとに `draw(rank, layer)` を 1 回呼びます。`rank` はそれらのレイヤーを一番上のものから 0 始まりで数えた番号で、`layer` はプロジェクト内でのレイヤー番号です。`draw` の中では通常の描画呼び出しを使ってください。ゲームはそれらを画像へ向け直し、その画像はオブジェクトと一緒に移動、拡大縮小、フェードします。

<div class="callout warn">
関数は SetImageDrawer を呼ぶスクリプトのものでなければなりません。`draw` の中で描いたやわらかい縁は、画面に描いたときよりわずかに細くなります。
</div>

### プロジェクトの読み取り

| メソッド | 説明 |
| --- | --- |
| `project.Loaded  -> bool` | ファイルを読み込めていれば true。 |
| `project.Width  -> int` | シーンのピクセル幅。何も読み込まれていなければ -1。 |
| `project.Height  -> int` | シーンのピクセル高さ。何も読み込まれていなければ -1。 |
| `project.FrameRate  -> number` | シーンの 1 秒あたりのフレーム数。 |
| `project.FrameCount  -> int` | シーンの長さ (フレーム)。 |
| `project.DurationMs  -> number` | シーンの長さ (ミリ秒)。 |
| `project.SceneCount  -> int` | プロジェクト内のシーン数。 |
| `project:GetScene()  -> int` | 再生中のシーンのインデックス。 |
| `project:SetScene(index)  -> nil` | プロジェクトの別のシーンを先頭から再生します。 |
| `project:GetSceneName(index)  -> string` | シーンの名前。 |
| `project.ObjectCount  -> int` | シーン内のオブジェクト数。 |
| `project:GetProjectValue(key)  -> string` | プロジェクトの設定値。ファイルに書かれているとおりの文字列です。 |
| `project:GetSceneValue(key)  -> string` | 再生中のシーンの設定値。 |
| `project:GetValue(section, key)  -> string` | ファイル内の任意のセクションの設定値。セクション名で指定します (オブジェクトなら `"12"`、その 2 番目のエフェクトなら `"12.1"`)。 |
| `project:GetValueAtFrame(objectId, effect, key, frame)  -> number` | オブジェクトのエフェクトが持つ数値設定の、指定フレームでの値。移動を適用した後の値です。 |

### 描画内容の確認

| メソッド | 説明 |
| --- | --- |
| `project.UnsupportedCount  -> int` | プロジェクトが使っていてゲームが描画しないものの数。見つからないファイルも含みます。 |
| `project:GetUnsupported()  -> string` | それらの一覧。1 行に 1 件です。 |
| `project.ApproximatedCount  -> int` | ゲームが簡略化して描画するものの数。 |
| `project:GetApproximated()  -> string` | それらの一覧。1 行に 1 件です。 |
| `project:AddScriptFolder(path)  -> nil` | プロジェクトのエフェクトが名前で参照できる AviUtl スクリプトファイルのフォルダを追加します。プロジェクトファイルの隣にある `Script` フォルダは、ゲームが自動で読み込みます。 |
| `project.ScriptErrorCount  -> int` | 失敗したスクリプトファイルの数。 |
| `project:GetScriptErrors()  -> string` | 失敗した各スクリプトの最初のエラー。1 行に 1 件です。 |
| `project.GraphicsMemoryMB  -> number` | プロジェクトが自身の描画のために現在保持しているグラフィックスメモリ (メガバイト)。 |
| `project:SetSurfaceBudgetMB(mb)  -> nil` | プロジェクトがフレーム間で再利用するために保持するグラフィックスメモリの量を設定します。 |

<div class="callout warn">
プロジェクトに同梱されたスクリプトファイルは閉じた環境で動きます。ファイルの読み書き、プログラムの起動、ネイティブモジュールの読み込みはできません。
</div>

```lua
local intro
local box

function onStart()
    intro = AVIUTL2:LoadProject("Intro/intro.aup2")
    box = TEXTURE:CreateTexture("Intro/player_box.png")
    -- 各プレイヤーのネームプレート。プロジェクトのプレイヤーボックスの中に描画されます
    intro:SetImageDrawer("player_box.png", 520, 216, function(rank)
        box:Draw(0, 0)
        NAMEPLATE:DrawPlayerNameplate(170, 88, 255, rank)
    end)
end

function activate()
    intro:SetBackground(0, 0, 0, 255)
    intro:Start()
end

function draw()
    intro:DrawFit(0, 0, 1920, 1080)
    if intro:IsFinished() then Exit("title", nil) end
end

function deactivate()
    intro:Stop()
end

function onDestroy()
    intro:Dispose()
    box:Dispose()
end
```
