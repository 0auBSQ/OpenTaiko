<!-- modules/core.md -->

# Core

画面を持たない小さなヘルパーです。イージングカーブ、色の計算、テーブルのユーティリティ、JSON のエンコーダーとデコーダー、そして現在のファイルのフォルダ。読み込み時にエンジンの状態に触れないため、読み取り専用モジュールを含め、どのモジュールからも require できます。

## Core.Easing

0 から 1 の比率に対するイージングカーブで、自分のクロック (タイムライン、パーティクルの経過時間) で動かすアニメーション用です。エンジンのクロックで動くトゥイーンには、`COUNTER` グローバルの SetEasing という選択肢もあります。`hump` を除くすべてのカーブは入力を 0..1 に制限し、0 を 0 に、1 を 1 に対応させます。

| 関数 | 説明 |
| --- | --- |
| `Easing.linear(t)  -> number` | 制限した比率そのもの。 |
| `Easing.inQuad(t)`, `outQuad`, `inOutQuad`, `inCubic`, `outCubic`, `inOutCubic`, `inSine`, `outSine`, `inOutSine`, `inExpo`, `outExpo`  `-> number` | 名前どおりの一般的なカーブ。 |
| `Easing.inBack(t, s?)`, `Easing.outBack(t, s?)  -> number` | オーバーシュートするカーブ。`s` はオーバーシュート量 (既定は 1.7)。 |
| `Easing.inBounce(t)`, `Easing.outBounce(t)`, `Easing.outElastic(t)  -> number` | バウンドと弾性のカーブ。 |
| `Easing.hump(t)  -> number` | 0 から中央で 1 まで上がり、0 に戻ります。つぶしやフラッシュ用です。 |
| `Easing.span(t, t0, dur)  -> number` | `t0` に始まり `dur` だけ続く区間の進み具合を 0..1 に制限したもの。 |
| `Easing.lerp(a, b, k)  -> number` | `a + (b - a) * k`. |
| `Easing.clamp01(t)  -> number` | `t` を 0..1 に制限したもの。 |
| `Easing.get(kind, fn)  -> function` | エンジンのイージング名で指定するカーブ。`kind` は `"IN"`、`"OUT"`、`"INOUT"` のいずれか、`fn` は `"LINEAR"`、`"SINE"`、`"QUAD"`、`"CUBIC"`、`"EXPO"`、`"BACK"`、`"BOUNCE"`、`"ELASTIC"` のいずれかです。カーブのない組み合わせは QUAD のカーブになります。 |

```lua
local Easing = require("Core.Easing")
local k = Easing.outBack(Easing.span(t, startTime, 0.4))
x = Easing.lerp(x0, x1, k)
```

## Core.Color

色の変換と調整です。色相も含め、すべてのチャンネルは 0 から 1 の浮動小数点数です (色相は 0 から 1 で色相環を一周します)。関数は `r, g, b` を別々の値として受け取り、返します。

| 関数 | 説明 |
| --- | --- |
| `Color.rgbToHsv(r, g, b)  -> h, s, v` | RGB から HSV へ。 |
| `Color.hsvToRgb(h, s, v)  -> r, g, b` | HSV から RGB へ。 |
| `Color.rgbToHsl(r, g, b)  -> h, s, l` | RGB から HSL へ。 |
| `Color.hslToRgb(h, s, l)  -> r, g, b` | HSL から RGB へ。 |
| `Color.mix(r, g, b, r2, g2, b2, t)  -> r, g, b` | 線形ブレンド。`t` = 0 で最初の色のまま、1 で 2 番目の色になります。 |
| `Color.lighten(r, g, b, amount)  -> r, g, b` | HSL の明度を、残りの範囲の `amount` の割合だけ白に近づけます (1 = 白)。色相と彩度は変わりません。 |
| `Color.darken(r, g, b, amount)  -> r, g, b` | HSL の明度を `amount` だけ黒に近づけます (1 = 黒)。 |
| `Color.saturate(r, g, b, factor, add?)  -> r, g, b` | HSV の彩度に `factor` を掛け、`add` を加え、1 に制限します。 |
| `Color.brighten(r, g, b, factor)  -> r, g, b` | HSV の明度に `factor` を掛け、1 に制限します。 |
| `Color.ramp(from, to, n)  -> table` | `{r, g, b}` から `{r, g, b}` へ段階的に変化する `n` 色を、`{r, g, b}` のリストとして返します。 |
| `Color.fromEngine(col)  -> r, g, b, a` | `COLOR` オブジェクトのチャンネルを浮動小数点数で返します。 |
| `Color.toEngine(r, g, b, a?)  -> color` | 浮動小数点数から作った `COLOR` オブジェクト (アルファの既定は 1)。 |
| `Color.rampEngine(fromCol, toCol, n)  -> table` | 2 つの `COLOR` オブジェクトの間を結ぶ、`n` 個の `COLOR` オブジェクトのランプ。 |
| `Color.clamp01(v)  -> number` | `v` を 0..1 に制限したもの。 |

## Core.Util

| 関数 | 説明 |
| --- | --- |
| `Util.clamp(v, lo, hi)  -> number` | `v` を `lo` と `hi` の間に収めたもの。 |
| `Util.lerp(a, b, t)  -> number` | 線形補間。 |
| `Util.round(v)  -> number` | 最も近い整数に丸めます (0.5 は切り上げ)。 |
| `Util.deepcopy(o)  -> value` | Lua テーブルのディープコピー。メタテーブルと循環参照も含みます。 |
| `Util.merge(base, over)  -> table` | `over` をマージした `base` のコピー。サブテーブルは再帰的にマージされ、それ以外の値は置き換えられます。 |
| `Util.cloneTable(dict)  -> table` | .NET の辞書 (解析済みの JSON オブジェクト、SQL の行セット) を再帰的に Lua テーブルへコピーしたもの。 |

## Core.Json

Lua の値から JSON テキストへ、またその逆へ、純粋な Lua で変換します。ファイルを .NET の辞書に解析する `JSONLOADER` と違い、Lua テーブルを扱うため、セーブ文字列 (`sf:SetGlobalString`) やデータベースの値に保存するデータに向いています。

| 関数 | 説明 |
| --- | --- |
| `Json.encode(value)  -> string` | キーが 1..n のテーブルは配列として、それ以外のテーブルはキーをソートしたオブジェクトとして書き出すため、同じ値からは常に同じテキストが得られます。関数と userdata はオブジェクトから除外され、整数は小数点なしで書き出されます。自分自身を含むテーブルはエラーになります。 |
| `Json.decode(text)  -> value` または `nil, message` | JSON テキストを解析します。JSON の `null` は `Json.null` になります。構文エラーのときは nil と、位置を含むメッセージを返します。 |
| `Json.null` | JSON の `null` を表す値。エンコードすると `null` に戻ります。 |

```lua
local Json = require("Core.Json")
local sf = GetSaveFile(0)
local data = Json.decode(sf:GetGlobalString("my_feature")) or { v = 1, items = {} }
data.items[#data.items + 1] = "lamp"
sf:SetGlobalString("my_feature", Json.encode(data))
```

## Core.Here

呼び出したファイルのフォルダを、`require` がそのファイルに渡す引数から求めて返す関数です。Lib ファイルはこれで自分のアセットを見つけます ([アセットは呼び出し元を基準に解決される](README.md)を参照)。

| 関数 | 説明 |
| --- | --- |
| `require("Core.Here")(...)  -> string` | ファイルの絶対フォルダ。区切りはスラッシュで、末尾に `/` が付きます。ファイルが `require` で読み込まれていない場合は nil。`...` がモジュール名とファイルパスを保持している、ファイルのトップレベルで呼び出してください。 |
