<!-- modules/songs.md -->

# Songs

## Songs.PlayStrings

ステージが開始する 1 回のプレイのために設定する共有文字列です。表示するローディング背景、段位プレートのスタイルなど、そのプレイのバックグラウンドや ROActivity が読むものなら何でも扱えます。文字列はスタンプされた譜面に対してだけ値を返すため、ステージが戻ってこなかったプレイが残した値が他のプレイに届くことはありません。

| 関数 | 説明 |
| --- | --- |
| `PlayStrings.stamp()` | 今選ばれている譜面 (`SONGMOUNT`) を、文字列が属する譜面としてスタンプします。譜面をマウントした直後、`Exit("play")` の前に呼び出してください。 |
| `PlayStrings.clear()` | スタンプを終了します。プレイから戻ったときに呼び出してください。 |
| `PlayStrings.get(key)  -> string` | 選ばれている譜面がスタンプされた譜面である間は共有文字列 `key`、そうでなければ `""`。 |
| `PlayStrings.forTitle(key, title)  -> string` | 同じ確認を譜面のタイトルで行います。タイトルしか知らない読み手 (段位プレート) 用です。 |

値そのものは通常の共有文字列で、`SHARED:SetSharedString(key, value)` で設定します。スタンプは共有文字列 `play_strings_chart` に保持されます。

```lua
local PlayStrings = require("Songs.PlayStrings")

-- the stage, after mounting the chart
SHARED:SetSharedString("my_loading_art", "night")
PlayStrings.stamp()

-- a background script of that play
local art = PlayStrings.get("my_loading_art")   -- "night"; "" for any other chart
```
