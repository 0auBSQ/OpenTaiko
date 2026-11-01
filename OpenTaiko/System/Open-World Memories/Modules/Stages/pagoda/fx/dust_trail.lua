-- Dust of a dissolving sprite gathered into a swirl, then flown along a curve; 1920x1080 screen space.

local assets = require("encounter/assets")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local T = {}
T.__index = T

local TAU = math.pi * 2
local sin, cos, sqrt, atan = math.sin, math.cos, math.sqrt, math.atan
local min, max, random = math.min, math.max, math.random

local look = Layout.fx.dust_trail
local motion = Animation.fx.dust_trail

-- what a point sheds into the swirl: `count_base` of each kind, plus one more at the chance `extra_chance`
local SPECK = Config.plain(motion.speck)
SPECK.key = "fx_ash"
local SMOKE = Config.plain(motion.smoke)
SMOKE.key = "fx_dust"
local KINDS = { SMOKE, SPECK }  -- drawing order
-- left where a point is shed
local PUFF = Config.plain(motion.puff)
PUFF.key = "fx_dust"
local DUST_R, DUST_G, DUST_B = look.dust_color[1], look.dust_color[2], look.dust_color[3]
local JITTER = look.spawn_jitter_px

-- the swirl
local FLAT = look.flat                          -- its height over its width
local RADIUS = look.radius_px                   -- each piece settles on a radius in this range
local PULL_SEC = motion.pull_s                  -- time constant of a piece's fall onto its radius
local PULL_MAX = motion.pull_max_px_per_s       -- fastest fall, px/s
local SPIN = motion.spin_rad_per_s              -- rad/s at SPIN_R
local SPIN_R, SPIN_MAX = motion.spin_r_px, motion.spin_max_rad_per_s
local SPIN_MIN_R = motion.spin_min_radius_px
local SPIN_W_BASE, SPIN_W_JITTER = motion.spin_jitter.base, motion.spin_jitter.jitter
local BOB, BOB_HZ, BOB_IN = motion.bob.px, motion.bob.hz, motion.bob.in_s   -- px, Hz, seconds to reach the full bob

-- the trail
local SPOUT = math.pi                           -- the swirl's left side
local SPOUT_R = look.spout_r_px                 -- the curve starts this far left of the swirl's middle
local RELEASE_SEC = motion.release_s            -- from the first piece leaving to the last
local FLY_SEC = motion.fly_s                    -- each piece's run along the curve
local ACCEL = motion.accel                      -- the run speeds up into the end (u = k ^ ACCEL)
local JOIN = motion.join_share                  -- share of the run spent joining the curve from where it left
local WAVE_AMP, WAVE_N, WAVE_HZ = motion.wave.amp_px, motion.wave.along, motion.wave.per_s
local WAVE_ENV = motion.wave.envelope_power
local SPREAD = look.spread_px                   -- half-width at the trail's start, px, tapering to 0
local FADE_FROM = motion.fade_from_share        -- share of the run after which a piece fades into the end
local SHRINK = motion.shrink_share              -- share of the size lost by the end of the run
local GHOST_LAG = motion.ghost_lag_s            -- lag of a flying speck's ghost copy
local GHOST_A, GHOST_S = look.ghost.opacity, look.ghost.scale

local function rnd(r) return r[1] + random() * (r[2] - r[1]) end

local function bez(u, a, b, c, d)
    local v = 1 - u
    return v * v * v * a + 3 * v * v * u * b + 3 * v * u * u * c + u * u * u * d
end

local function bezD(u, a, b, c, d)
    local v = 1 - u
    return 3 * v * v * (b - a) + 6 * v * u * (c - b) + 3 * u * u * (d - c)
end

function T.keys()
    return { SPECK.key, SMOKE.key }
end

-- points: silhouette points in sprite px, top-down; centre = { x, y } of the swirl; path = the
-- curve's two inner control points and its end, { { x, y }, { x, y }, { x, y } }
function T.new(points, centre, path)
    local cx, cy = centre.x, centre.y
    local pts = points or {}
    local t = setmetatable({
        pts = pts, peeled = {}, left = #pts,
        cx = cx, cy = cy,
        px = { cx + SPOUT_R * cos(SPOUT), path[1].x, path[2].x, path[3].x },
        py = { cy + SPOUT_R * FLAT * sin(SPOUT), path[1].y, path[2].y, path[3].y },
        parts = {}, tex = {}, puffs = {},
        flying = false, ft = 0,
    }, T)
    for _, k in ipairs(KINDS) do
        t.parts[k] = {}
        t.tex[k] = (assets.get(k.key))
    end
    t.puffTex = (assets.get(PUFF.key))
    return t
end

local function add(self, k, x, y)
    local list = self.parts[k]
    if #list >= k.max then return end
    x, y = x + (random() * 2 - 1) * JITTER, y + (random() * 2 - 1) * JITTER
    local dx, dy = x - self.cx, (y - self.cy) / FLAT
    list[#list + 1] = {
        x = x, y = y, r = sqrt(dx * dx + dy * dy), th = atan(dy, dx), rt = rnd(RADIUS),
        w = SPIN_W_BASE + SPIN_W_JITTER * random(), ph = random() * TAU, hz = rnd(BOB_HZ), age = 0,
        s = rnd(k.size), sq = 1 + (random() * 2 - 1) * k.squash,
        rot = random() * 360, spin = rnd(k.spin_deg_per_s) * (random() < 0.5 and -1 or 1),
    }
end

local function addPuff(self, x, y)
    if #self.puffs >= PUFF.max then return end
    self.puffs[#self.puffs + 1] = {
        x = x, y = y, vy = rnd(PUFF.rise_px_per_s), age = 0, life = rnd(PUFF.life_s),
        s = rnd(PUFF.size), sq = 1 + (random() * 2 - 1) * PUFF.squash,
    }
end

-- sheds the points eaten so far: eaten(x, y) -> bool and place(x, y) -> screen x, y take sprite px
function T:shed(eaten, place)
    if self.left <= 0 or self.flying then return end
    for i, p in ipairs(self.pts) do
        if not self.peeled[i] and eaten(p[1], p[2]) then
            self.peeled[i] = true
            self.left = self.left - 1
            local x, y = place(p[1], p[2])
            for _, k in ipairs(KINDS) do
                for _ = 1, k.count_base + ((random() < k.extra_chance) and 1 or 0) do add(self, k, x, y) end
            end
            if random() < PUFF.chance then addPuff(self, x, y) end
        end
    end
end

-- the piece closest to the spout leaves first
function T:fly()
    if self.flying then return end
    self.flying, self.ft = true, 0
    local order = {}
    for _, k in ipairs(KINDS) do
        for _, q in ipairs(self.parts[k]) do order[#order + 1] = { q = q, d = (q.th - SPOUT) % TAU } end
    end
    table.sort(order, function(a, b) return a.d < b.d end)
    local n = #order
    for i, o in ipairs(order) do o.q.leaveAt = (n > 1) and (i - 1) / (n - 1) * RELEASE_SEC or 0 end
end

-- the piece's point on the trail k (0..1) into its run, and how far along the curve that is
local function runPoint(self, q, k)
    local u = k ^ ACCEL
    local px, py = self.px, self.py
    local bx, by = bez(u, px[1], px[2], px[3], px[4]), bez(u, py[1], py[2], py[3], py[4])
    local tx, ty = bezD(u, px[1], px[2], px[3], px[4]), bezD(u, py[1], py[2], py[3], py[4])
    local l = max(1e-6, sqrt(tx * tx + ty * ty))
    local env = sin(math.pi * u) ^ WAVE_ENV
    local off = WAVE_AMP * sin(TAU * (WAVE_N * u - WAVE_HZ * self.ft)) * env + SPREAD * q.j * (1 - u)
    local x, y = bx - ty / l * off, by + tx / l * off
    local j = min(1, k / JOIN)
    j = j * j * (3 - 2 * j)
    return q.x0 + (x - q.x0) * j, q.y0 + (y - q.y0) * j, u
end

local function swirl(self, q, dt)
    local dr = (q.rt - q.r) * min(1, dt / PULL_SEC)
    q.r = q.r + max(-PULL_MAX * dt, min(PULL_MAX * dt, dr))
    local w = SPIN * q.w * sqrt(SPIN_R / max(q.r, SPIN_MIN_R))
    q.th = q.th + max(-SPIN_MAX, min(SPIN_MAX, w)) * dt
    q.x = self.cx + q.r * cos(q.th)
    q.y = self.cy + q.r * FLAT * sin(q.th) + BOB * sin(TAU * q.hz * q.age + q.ph) * min(1, q.age / BOB_IN)
end

-- true once the piece has run its course
local function step(self, k, q, dt)
    q.age = q.age + dt
    q.rot = q.rot + q.spin * dt
    if q.leaveAt == nil or self.ft < q.leaveAt then
        swirl(self, q, dt)
        return false
    end
    if q.run == nil then
        q.run, q.dur, q.j, q.x0, q.y0 = 0, rnd(FLY_SEC), random() * 2 - 1, q.x, q.y
    end
    q.run = q.run + dt
    local f = q.run / q.dur
    if f >= 1 then return true end
    q.x, q.y, q.u = runPoint(self, q, f)
    if k.ghost then q.gx, q.gy = runPoint(self, q, max(0, f - GHOST_LAG / q.dur)) end
    return false
end

function T:update(dt)
    if dt == nil or dt <= 0 then return end
    if self.flying then self.ft = self.ft + dt end
    for _, k in ipairs(KINDS) do
        local list, i = self.parts[k], 1
        while i <= #list do
            if step(self, k, list[i], dt) then
                list[i] = list[#list]
                list[#list] = nil
            else
                i = i + 1
            end
        end
    end
    local list, i = self.puffs, 1
    while i <= #list do
        local q = list[i]
        q.age = q.age + dt
        if q.age >= q.life then
            list[i] = list[#list]
            list[#list] = nil
        else
            q.y = q.y + q.vy * dt
            i = i + 1
        end
    end
end

-- true once the trail has run and all the dust is gone
function T:done()
    if not self.flying or #self.puffs > 0 then return false end
    for _, k in ipairs(KINDS) do
        if #self.parts[k] > 0 then return false end
    end
    return true
end

local function drawPuffs(self, ox, oy)
    local tx = self.puffTex
    if tx == nil or not tx.Ready or #self.puffs == 0 then return end
    tx:SetBlendMode("Normal")
    tx:SetColor(DUST_R, DUST_G, DUST_B)
    for _, q in ipairs(self.puffs) do
        local u = q.age / q.life
        local a = min(1, q.age / PUFF.fade_in_s, (1 - u) / PUFF.fade_out_share) * PUFF.opacity
        if a > 0.01 then
            local s = q.s * (1 + (PUFF.size_end - 1) * u)
            tx:SetScale(s * q.sq, s / q.sq)
            tx:SetOpacity(a)
            tx:DrawAtAnchor(q.x + ox, q.y + oy, "center")
        end
    end
    tx:SetScale(1, 1)
    tx:SetOpacity(1)
    tx:SetColor(1, 1, 1)
end

local function drawKind(self, k, ox, oy)
    local tx, list = self.tex[k], self.parts[k]
    if tx == nil or not tx.Ready or #list == 0 then return end
    tx:SetBlendMode("Normal")
    tx:SetColor(DUST_R, DUST_G, DUST_B)
    for _, q in ipairs(list) do
        local a = min(1, q.age / k.fade_in_s) * k.opacity
        local s = q.s
        if q.u ~= nil then
            a = a * min(1, (1 - q.u) / (1 - FADE_FROM))
            s = s * (1 - SHRINK * q.u)
        end
        if a > 0.01 then
            tx:SetRotation(q.rot)
            if q.gx ~= nil then
                tx:SetScale(s * q.sq * GHOST_S, s / q.sq * GHOST_S)
                tx:SetOpacity(a * GHOST_A)
                tx:DrawAtAnchor(q.gx + ox, q.gy + oy, "center")
            end
            tx:SetScale(s * q.sq, s / q.sq)
            tx:SetOpacity(a)
            tx:DrawAtAnchor(q.x + ox, q.y + oy, "center")
        end
    end
    tx:SetScale(1, 1)
    tx:SetRotation(0)
    tx:SetOpacity(1)
    tx:SetColor(1, 1, 1)
end

-- ox, oy: the screen shake
function T:draw(ox, oy)
    ox, oy = ox or 0, oy or 0
    drawPuffs(self, ox, oy)
    for _, k in ipairs(KINDS) do drawKind(self, k, ox, oy) end
end

function T:clear()
    for _, k in ipairs(KINDS) do self.parts[k] = {} end
    self.puffs = {}
end

return T
