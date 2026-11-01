-- Screen shake; each screen keeps its own.

local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local M = {}
M.__index = M

local Anim = Animation.encounter.shake
local DEFAULT_AMP, DEFAULT_DUR = Anim.default_px, Anim.default_s
local RATE_X, RATE_Y, SHARE_Y = Anim.x_rad_per_s, Anim.y_rad_per_s, Anim.y_share

function M.new()
    return setmetatable({ t = 0, dur = 0, amp = 0 }, M)
end

-- amp: px, dur: seconds
function M:start(amp, dur)
    self.amp, self.dur, self.t = amp or DEFAULT_AMP, dur or DEFAULT_DUR, 0
end

function M:stop()
    self.dur = 0
end

function M:update(dt)
    if self.dur <= 0 then return end
    self.t = self.t + dt
    if self.t >= self.dur then self.dur = 0 end
end

-- this frame's offset x, y in px
function M:offset()
    if self.dur <= 0 then return 0, 0 end
    local k = 1 - self.t / self.dur
    return math.sin(self.t * RATE_X) * self.amp * k,
           math.cos(self.t * RATE_Y) * self.amp * k * SHARE_Y
end

return M
