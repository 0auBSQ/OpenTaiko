<!-- modules/audio.md -->

# Audio

## Audio.MusicDuck

ステージの上で何かを聞かせる必要がある間 (報酬のポップアップ、ボイス)、ステージの音楽を下げ、その後元に戻します。音量はスムーズステップで、最大と下げたレベルの間をなめらかに変化します。

| 関数 | 説明 |
| --- | --- |
| `MusicDuck.new(opts)  -> duck` | `opts`: `sound` (音楽。コード側で係数を適用する場合は nil)、`base` (最大時の音量パーセント、100)、`level` (下げたときの係数、0.3)、`inS` (下げるまでの秒数、0.35)、`outS` (戻るまでの秒数、0.8)。 |
| `duck:update(dt, dimmed)  -> number` | 毎フレーム、フレーム時間 (秒) を渡して呼び出します。`dimmed` が true なら下げ、false なら戻します。サウンドの音量パーセントに `base * factor` を適用し、係数 (0 から 1) を返します。 |
| `duck:reset()` | ただちに最大音量に戻します (音楽を再開するステージ用)。 |
| `duck:apply()` | 現在の音量をサウンドにもう一度書き込みます。 |

```lua
local MusicDuck = require("Audio.MusicDuck")
local duck, last = nil, 0

function activate()
    duck = MusicDuck.new{ sound = bgm, base = 80 }
end

function update(ts)
    local dt = (ts - last) / 1000; last = ts
    duck:update(dt, rewardPopupShowing)
end
```
