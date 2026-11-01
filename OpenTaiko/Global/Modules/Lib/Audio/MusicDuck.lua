-- Dims a stage's own music while something on top of it (a reward modal) needs its sounds heard.
-- Call duck:update(dt, dimmed) every frame: the factor eases between 1 and `level` with a smoothstep,
-- in over inS seconds and back out over outS, and `base * factor` is applied to the sound's volume
-- percent. Music that manages its own volume can leave `sound` nil and multiply the returned factor in.
local Duck = {}
Duck.__index = Duck

function Duck.new(opts)
    opts = opts or {}
    local d = setmetatable({
        sound = opts.sound,
        base  = opts.base or 100,
        level = opts.level or 0.3,
        inS   = opts.inS or 0.35,
        outS  = opts.outS or 0.8,
    }, Duck)
    d:reset()
    return d
end

-- back to full volume at once (a stage that restarts its music)
function Duck:reset()
    self.factor, self.from, self.to, self.t, self.dur = 1, 1, 1, 0, 0
    self:apply()
end

function Duck:apply()
    if self.sound then self.sound:SetVolumePercent(self.base * self.factor) end
end

function Duck:update(dt, dimmed)
    local target = dimmed and self.level or 1
    if target ~= self.to then
        self.from, self.to, self.t = self.factor, target, 0
        self.dur = dimmed and self.inS or self.outS
    end
    if self.factor ~= self.to then
        self.t = self.t + (dt or 0)
        local k = (self.dur > 0) and math.min(1, self.t / self.dur) or 1
        if k >= 1 then
            self.factor = self.to
        else
            k = k * k * (3 - 2 * k)
            self.factor = self.from + (self.to - self.from) * k
        end
        self:apply()
    end
    return self.factor
end

return Duck
