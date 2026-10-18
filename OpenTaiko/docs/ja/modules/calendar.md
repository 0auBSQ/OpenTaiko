<!-- modules/calendar.md -->

# Calendar

## Calendar.almanac

暦に合わせて現れては消えるイベントのための日付パターンです。クリスマスの注目商品、エイプリルフールのシーン、毎月のボーナスなど。日はプレイヤーの時計に関係なく日本標準時 (UTC+9) の日なので、すべてのプレイヤーが同じ日を見ます。パターンは不透明な `enc:` トークンとして書けるため、サプライズがソースに現れません。

| パターン | 一致する日 |
| --- | --- |
| `"2026-06-15"` | その日。 |
| `"2026-06-15+30"` | その日と、その後の 29 日。 |
| `"2026-06-15..2026-07-31"` | 両端を含む期間 (両方とも完全に書きます)。 |
| `"*-06-15"`, `"*-06-15+7"` | 毎年のその日、またはその週。 |
| `"*-*-01"`, `"2026-*-13"` | 毎月 1 日。2026 年のすべての 13 日。 |
| `"*-06-*"`, `"2026-12-*"` | 毎年のひと月全体。特定のひと月。 |
| `{ "...", "..." }` | 複数のパターンのいずれか。 |

エントリはテーブル `{ code, when, priority, sticky }` で、`when` が一致している間有効です。`sticky = true` にすると、あるセーブファイルで一度一致した後は、そのセーブのトリガー `.almanac_<code>` によって、そのセーブファイルで有効なままになります。`code` 自体を `enc:` トークンにすることもできます。

| 関数 | 説明 |
| --- | --- |
| `Almanac.matches(spec)  -> bool` | 今日がパターン、トークン、またはそれらのリストに一致するかどうか。 |
| `Almanac.active(entry, save?)  -> bool` | エントリが今日有効かどうか。セーブファイルを渡すと、一度一致した sticky のエントリは有効なままになり、一致するとそのトリガーが設定されます。 |
| `Almanac.pick(entries, save?, skip?)  -> entry` | `priority` が最も高い有効なエントリ (同点なら最初のもの)、または nil。`skip(entry)` が true を返すとそのエントリは除外されます (例えばプレイヤーがすでに持っているもの)。 |
| `Almanac.encrypt(pattern)  -> string` | パターンまたはコードの `enc:` トークン。 |
| `Almanac.decrypt(token)  -> string` | トークンのテキスト。有効なトークンでなければ nil。 |
| `Almanac.plain(s)  -> string` | 復号したトークン、または `s` そのもの。 |
| `Almanac.parse(spec)  -> table` | 解析した 1 つのパターン (不正な場合は nil)。`matchesPattern` 用です。 |
| `Almanac.matchesPattern(p)  -> bool` | 今日が解析済みのパターンに一致するかどうか。 |

```lua
local Almanac = require("Calendar.almanac")
local EVENTS = {
    { code = "winter", when = "*-12-20+12", priority = 80 },
    { code = "spring", when = "*-03-20+14", priority = 50, sticky = true },
}
local now = Almanac.pick(EVENTS, GetSaveFile(0))
if now ~= nil then banner = now.code end
```
