-- Audio.MusicDuck: dims a stage's music while a modal needs its sounds heard
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

-- call every frame; factor eases between 1 and `level` (smoothstep), in over inS s, out over outS
-- volume percent = base * factor; with sound = nil, multiply the returned factor in
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
