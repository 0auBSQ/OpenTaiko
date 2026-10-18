<!-- modules/characters.md -->

# Characters

## Characters.Student

プレイヤーが操作するストーリーの生徒 (A または B) と、その生徒のカラーパレットです。これにより、すべてのストーリーシーンが同じ生徒を同じ色で描きます。生徒は `Global/Characters/` のキャラクター `02v2 - Student (A)` と `03v2 - Student (B)` です。

すべてはセーブファイルに保存されます。

| セーブキー | 種類 | 内容 |
| --- | --- | --- |
| `story_student` | カウンター | 選んだ生徒。1 は B で、それ以外の値は未設定も含めて A として読まれます。 |
| `.character_palette_<folder>` | カウンター | 生徒のキャラクターの 0 始まりのパレットインデックス。キャラクターのカスタマイズ画面が書き込むのと同じカウンターなので、両者は常に一致します。A と B はそれぞれ自分のものを持ちます。 |
| `.character_playcount_<folder>` | カウンター | そのキャラクターでのプレイ回数で、これによりパレットがアンロックされます。 |

生徒のパレットは、キャラクターの `Palettes.json` に続けて、モジュール自身のコピー (`Global/Modules/Lib/Characters/Student/<id>/Palettes.json`) のパレットのうち、キャラクターのファイルにないもの (`name` で照合) を並べたものです。パレットのないキャラクターや、インストールされていないキャラクターでも、コピーのパレットは使えます。リストの範囲外のインデックスは既定のパレット 0 として読まれます。

| 関数 | 説明 |
| --- | --- |
| `Student.current(save)  -> "a" or "b"` | セーブが選んでいる生徒。 |
| `Student.ensure(save)  -> "a" or "b"` | 同じですが、カウンターに生徒がない場合は A を書き込みます。 |
| `Student.set(save, id)  -> id` | 生徒を選びます。 |
| `Student.folder(id)  -> string` | 生徒のキャラクターフォルダ名。 |
| `Student.index(save, id)  -> number` | 生徒のパレットインデックス。常に有効な値です。 |
| `Student.setIndex(save, id, idx)  -> number` | パレットインデックスを保存します。そのキャラクターがセーブで装備中のものなら、表示中のパレットも変わります。 |
| `Student.palettes(id)  -> table` | パレットのリスト: `{ name, plays, blend, stops }` の要素で、要素 `i` がインデックス `i - 1` に対応します。ファイルが訳を持つ場合、`name` はゲームの言語になります。 |
| `Student.count(id)`, `Student.entry(id, idx)`, `Student.clamp(id, idx)` | パレットの数、1 つの要素、有効な値にしたインデックス。 |
| `Student.unlocked(save, id, idx)  -> bool` | パレットが開放されているかどうか: プレイ回数の条件がないか、そのキャラクターで十分にプレイしているか。 |
| `Student.gradient(id, idx)  -> gradient` | パレットを `GRADIENT` マップとして返します。色のないパレットでは nil。 |
| `Student.withPalette(id, idx, fn, ...)` | パレットを有効にして `fn` (2D 描画) を実行します。pcall の結果を返します。 |
| `Student.registerSprite(scene, spriteId, tex, id, idx)  -> bool` | `tex` を、パレットで色を変えた 3D シーンのビルボードスプライトとして登録します。 |
| `Student.reload()` | 解析済みのパレットファイルを忘れます。次の問い合わせで再び読み込まれます。 |

`palettes`、`count`、`entry`、`clamp`、`gradient`、`registerSprite` は省略可能な最後の引数 `visitor` を取ります。true にすると、別のプレイヤーの生徒 (オンラインの訪問者) のリストになり、プレイヤー自身のものとは別にキャッシュされます。

```lua
local Student = require("Characters.Student")
local save = GetSaveFile(0)
local id  = Student.current(save)
local idx = Student.index(save, id)
Student.withPalette(id, idx, function() tex:Draw(x, y) end)
```
