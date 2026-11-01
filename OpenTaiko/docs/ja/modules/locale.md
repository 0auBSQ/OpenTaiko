<!-- modules/locale.md -->

# Locale

## Locale.i18n

モジュール自身の翻訳で、スキンの `Locales/` ではなくモジュールの隣に置きます。テキストは `lang/<code>/<name>.json` に、機能ごとに 1 ファイル、安定した id をキーとして置きます。`lang/en/` が英語を持ち、他のすべての言語のフォールバックになります。コードはゲームの言語 id で、`en`、`de`、`es`、`fr`、`ja`、`ko`、`nl`、`ru`、`zh` です。

```
Global/Modules/Stages/my_stage/
  Script.lua
  lang/
    en/menu.json      { "title": "Settings", "count": "%d items", "tips": ["One", "Two"] }
    ja/menu.json      { "title": "設定", "count": "%d 件" }
```

| 関数またはフィールド | 説明 |
| --- | --- |
| `I18N.texts(name, dir?)  -> texts` | `lang/<code>/<name>.json` のテキスト。`dir` は `lang/` を含むフォルダです。ステージやアクティビティでは省略 (または `""` を渡す) すると自身のフォルダになり、Lib ファイルでは `Core.Here` のフォルダを渡します。ファイルは最初に使うときに読み込まれ、保持されます。 |
| `I18N.detect()` | ゲームの現在の言語を読み取り、すべての参照が使う言語にします。入場のたびに実行される `activate` で呼び出せば、設定で変更した言語が次の訪問で反映されます。 |
| `I18N.load(code)` | 言語 `code` を使います (テストハーネスや言語のプレビュー用)。 |
| `I18N.currentCode()  -> string` | ゲームの現在の言語 id。エンジンが何も返さない場合は `"en"`。 |
| `I18N.code` | 参照が現在使っている言語。 |
| `I18N.localizedPath(path, code?)  -> string` | `path` の隣にその名前のテクスチャがあれば `<name>_<code>.<ext>`、なければ `path`。文字が焼き込まれた画像用です。 |

| メソッド | 説明 |
| --- | --- |
| `texts:tr(id)  -> string` | `id` のテキスト。どの言語にもない場合は id そのものを返すため、抜けが画面上で目に見えます。 |
| `texts:trf(id, ...)  -> string` | `id` のテキストを `string.format` のパターンとして、引数とともに使ったもの。 |
| `texts:get(...)  -> string` | キーパスにある文字列 (入れ子のオブジェクトには `texts:get("menu", "title")`)。ない場合や空の場合は nil。 |
| `texts:list(...)  -> table` | キーパスにある文字列の配列。各要素は個別に英語へフォールバックします。 |
| `texts:loc(...)  -> handle` | 呼び出し時にキーパスを読む `GetString(fallback)` を持つハンドル。読み込み時にキャッシュした名前が、後の言語変更に追従します。 |
| `texts:node(...)  -> value` | キーパスにある生の JSON ノード (現在の言語、なければ英語)。 |

```lua
local I18N = require("Locale.i18n")
local T = I18N.texts("menu")
local title, count

function activate()
    I18N.detect()
    title = T:tr("title")
    count = T:trf("count", #items)
end
```

<div class="callout warn">
参照は `I18N.code` に従い、これは `detect` か `load` が実行されたときにだけ変わります。プレイヤーが言語を変更する間も画面に表示されたままのモジュールは、グローバルの `reloadLanguage(lang)` を定義し、そこで `detect` を呼び出せます。Pagoda、Vault、Shop のテキストモジュールは、参照のたびに自分で言語を確認します。
</div>

古いモジュールは、代わりに英語の原文をキーとする辞書を使います。`lang/<code>.lua` が各英語文字列からその訳へのテーブルを返し、`detect` が現在の言語のものを読み込み、関数 `I18N.tr(s)` と `I18N.trf(fmt, ...)` がそこから文字列を引きます。この形式も引き続き動作しますが、新しいモジュールは `texts` を使ってください。
