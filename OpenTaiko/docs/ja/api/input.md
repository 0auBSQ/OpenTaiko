<!-- api/input.md -->

# 入力

太鼓入力、キーボード、マウスの読み取りと、画面上のテキスト入力。

## INPUT

設定された太鼓入力、生のキーボード、マウスを読み取り、テキスト入力ウィジェットを作成します。すべてのスクリプトで利用できます。

<div class="callout warn">
太鼓入力の名前はキーコンフィグの名前で、大文字小文字を区別せずに照合されます: LRed、RRed、LBlue、RBlue (プレイヤー 1)、プレイヤー 2-5 用の LRed2P ... RBlue5P、Clap、Clap2P ... Clap5P、LeftChange、RightChange、Decide、Cancel、およびシステムキー (Capture、SongVolumeIncrease、SongVolumeDecrease、DisplayHits、DisplayDebug、QuickConfig、SortSongs、ToggleAutoP1、ToggleAutoP2、ToggleTrainingMode、CycleVideoDisplayMode、および Training* キー)。不明な名前は false を返します。太鼓のメソッドは、プレイヤーがキーコンフィグでその入力に割り当てたデバイスを読み取ります。INPUT はキーボードのキー名をエンジンのキーリストに対して大文字小文字を区別せずに照合します: 文字 A-Z、数字 D0-D9、F1-F15、Space、Return、Escape、Tab、Backspace、Delete、UpArrow、DownArrow、LeftArrow、RightArrow、LeftShift、RightShift、LeftControl、RightControl、LeftAlt、RightAlt、Home、End、PageUp、PageDown、NumberPad0-9、およびその他の標準キー。マウス位置はレターボックスを補正した論理ゲームサーフェス座標です。
</div>

### 太鼓入力

| メソッド | 説明 |
| --- | --- |
| `INPUT:Pressed(input)  -> bool` | 入力が押されたフレームに true。 |
| `INPUT:Pressing(input)  -> bool` | 入力が押されている間 true。 |
| `INPUT:Released(input)  -> bool` | 入力が離されたフレームに true。 |
| `INPUT:Releasing(input)  -> bool` | 入力が押されていない間 true。 |
| `INPUT:RepeatWhilePressing(input, interval_seconds, callback)  -> nil` | 毎フレーム呼びます。プレイヤーが入力を押している間、`interval_seconds` ごとに `callback()` を 1 回呼び (最初の呼び出しは 1 インターバル後)、プレイヤーが入力を離すと止まります。 |

### キーボード

| メソッド | 説明 |
| --- | --- |
| `INPUT:KeyboardPressed(key)  -> bool` | キーが押されたフレームに true。 |
| `INPUT:KeyboardPressing(key)  -> bool` | キーが押されている間 true。 |
| `INPUT:KeyboardReleased(key)  -> bool` | キーが離されたフレームに true。 |
| `INPUT:KeyboardReleasing(key)  -> bool` | キーが押されていない間 true。 |

### マウス

<div class="callout warn">
ボタン名: left、right、middle、button4、button5、または数値インデックス (大文字小文字を区別しません)。位置はゲームサーフェス座標です。マウスデバイスがない場合、位置のゲッターは -1 を返します。GetMouseDelta と GetScrollDelta は前回の呼び出しからの移動量を返してリセットするため、それぞれ 1 フレームに 1 回呼んでください。
</div>

| メソッド | 説明 |
| --- | --- |
| `INPUT:GetMouseX()  -> number` | ゲームサーフェス座標でのマウスの x。 |
| `INPUT:GetMouseY()  -> number` | ゲームサーフェス座標でのマウスの y。 |
| `INPUT:GetMouseXY()  -> number, number` | マウスの x と y を 2 つの戻り値として返します。 |
| `INPUT:IsMouseInside()  -> bool` | マウスが描画サーフェス上にあるとき true、レターボックスの縁の上にあるとき false。 |
| `INPUT:GetSurfaceWidth()  -> int` | ゲームサーフェスの幅 (スクリプトが描画する座標空間)。 |
| `INPUT:GetSurfaceHeight()  -> int` | ゲームサーフェスの高さ。 |
| `INPUT:MousePressed(button)  -> bool` | ボタンが押されたフレームに true。 |
| `INPUT:MousePressing(button)  -> bool` | ボタンが押されている間 true。 |
| `INPUT:MouseReleased(button)  -> bool` | ボタンが離されたフレームに true。 |
| `INPUT:GetMouseDelta()  -> number, number` | 前回の呼び出しからのマウスの移動量 (ウィンドウピクセル) を dx、dy で返します。 |
| `INPUT:GetScrollDelta()  -> number, number` | 前回の呼び出しからのホイールの移動量 (ノッチ単位) を dx、dy で返します (上にスクロールすると dy は正)。 |
| `INPUT:SetMouseLocked(locked)  -> nil` | フリールック用にカーソルをロックして隠す (true) か、元に戻し (false)、デルタの基準をリセットします。 |

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

### テキスト入力

| メソッド | 説明 |
| --- | --- |
| `INPUT:CreateTextInput(initialText, maxLength)  -> textInput` | `initialText` があらかじめ入力され、UTF-8 で `maxLength` バイト (既定 64) に制限されたテキスト入力ウィジェットを作成します。 |

## テキスト入力ハンドル

`INPUT:CreateTextInput` が返す画面上のテキストフィールドで、IME の変換を含む入力テキストを収集します。

<div class="callout warn">
フィールドがアクティブな間は 1 フレームに 1 回 Update を呼び、DisplayText を自分で描画してください。同時にアクティブにするテキスト入力は 1 つだけにしてください。Debug ビルドではオーバーレイウィンドウにもフィールドが表示されます。Release ビルドは自身では何も表示しません。iOS と Android ではゲームがプラットフォームのネイティブテキストダイアログを開き、プレイヤーがそれを確定すると Update が true を返します。
</div>

| メソッド | 説明 |
| --- | --- |
| `textInput:Update()  -> bool` | このフレームの入力を処理します。プレイヤーが Enter を押したフレームに true を返します。 |
| `textInput.Text  -> string` | 現在のテキスト。読み取りと代入が可能です (nil は空文字列になります)。 |
| `textInput.DisplayText  -> string` | 描画用に、カーソル位置に点滅するキャレットを挿入した現在のテキスト。 |

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
