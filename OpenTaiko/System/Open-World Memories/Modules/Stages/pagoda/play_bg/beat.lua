-- Syncs an idle cycle to the music.
-- Bar lines come from the bar index (it changes in every mode), not the chart time, which is offset in a Dan.

local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local Beat = {}
Beat.__index = Beat

local BAR_BEATS = 4                -- 4/4
local MIN_BEATS, MAX_BEATS = Animation.play_bg.beat.min_cycle_beats, Animation.play_bg.beat.max_cycle_beats

function Beat.new()
    return setmetatable({ bar = nil, anchored = false, fix = 0, fixLeft = 0 }, Beat)
end

-- beats per cycle: the power of two nearest the cycle's length in beats
local function cycleBeats(period, beat)
    local n = 2 ^ math.floor(math.log(period / beat, 2) + 0.5)
    return math.max(MIN_BEATS, math.min(MAX_BEATS, n))
end

-- idle seconds to run for dt real seconds. period, pos: the cycle's length and how far it is past
-- its accent (period nil: no cycle, the idle runs as is); bpm: the tempo; bar: the bar index
function Beat:step(dt, period, pos, bpm, bar)
    local crossed = self.bar ~= nil and bar ~= self.bar
    self.bar = bar
    if period == nil or bpm == nil or bpm <= 0 then return dt end
    local beat = 60 / bpm
    local n = cycleBeats(period, beat)
    local run = dt * period / (n * beat)
    if crossed and bar % math.max(1, n / BAR_BEATS) == 0 then
        if not self.anchored then
            self.anchored = true
            run = run + (period - pos) % period
        else
            local err = pos
            if err > period * 0.5 then err = err - period end
            self.fix, self.fixLeft = -err, BAR_BEATS * beat
        end
    end
    if self.fixLeft > 0 then
        local f = self.fix * math.min(1, dt / self.fixLeft)
        self.fix, self.fixLeft = self.fix - f, self.fixLeft - dt
        run = run + f
    end
    return math.max(0, run)
end

return Beat
