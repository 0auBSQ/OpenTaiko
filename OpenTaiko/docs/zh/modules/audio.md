<!-- modules/audio.md -->

# Audio

## Audio.MusicDuck

在舞台上层有需要被听到的内容时（奖励弹窗、语音）降低舞台的音乐，之后再恢复。音量以 smoothstep 在完整音量和降低后的音量之间平滑过渡。

| 函数 | 说明 |
| --- | --- |
| `MusicDuck.new(opts)  -> duck` | `opts`：`sound`（音乐；由你的代码自己应用系数时为 nil）、`base`（完整音量时的音量百分比，100）、`level`（降低后的系数，0.3）、`inS`（降低所需秒数，0.35）、`outS`（恢复所需秒数，0.8）。 |
| `duck:update(dt, dimmed)  -> number` | 每帧调用，传入以秒为单位的帧时间；`dimmed` 为 true 时降低，为 false 时恢复。把 `base * factor` 应用到声音的音量百分比上，并返回系数（0 到 1）。 |
| `duck:reset()` | 立即回到完整音量（用于重新开始播放音乐的舞台）。 |
| `duck:apply()` | 再次把当前音量写入声音。 |

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
