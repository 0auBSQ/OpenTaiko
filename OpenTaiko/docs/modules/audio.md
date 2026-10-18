<!-- modules/audio.md -->

# Audio

## Audio.MusicDuck

Lowers a stage's music while something on top of it needs to be heard (a reward pop-up, a voice line), then brings it back. The volume eases between full and the ducked level with a smoothstep.

| Function | Description |
| --- | --- |
| `MusicDuck.new(opts)  -> duck` | `opts`: `sound` (the music, or nil when your code applies the factor itself), `base` (its volume percent at full, 100), `level` (the ducked factor, 0.3), `inS` (seconds to duck, 0.35), `outS` (seconds to come back, 0.8). |
| `duck:update(dt, dimmed)  -> number` | Call every frame with the frame time in seconds; `dimmed` true ducks, false restores. Applies `base * factor` to the sound's volume percent and returns the factor (0 to 1). |
| `duck:reset()` | Back to full volume at once (a stage that restarts its music). |
| `duck:apply()` | Writes the current volume to the sound again. |

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
