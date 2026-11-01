-- Corruption darkness over boxes; one instance per screen.

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local D = {}
D.__index = D

local look = Layout.fx.darkness
local motion = Animation.fx.darkness

local SHADOW_MAX   = motion.max_particles            -- particle pool cap (motes + spirals)
local SHADOW_RATE  = motion.mote.per_s               -- motes per second per visible box
local SHADOW_LIFE  = motion.mote.life_s
local SHADOW_RISE  = motion.rise_px_per_s            -- upward drift (world px/s)
local SHADOW_SIZE  = look.mote.size
local SHADOW_SWIRL = motion.swirl.px_per_s           -- horizontal swirl (px/s)
local SWIRL_RATE   = motion.swirl.rad_per_s
local SHADOW_BACKLOG = motion.mote.backlog_max
local SHADOW_ALPHA = look.mote.opacity
local SPIRAL_RATE  = motion.spiral.per_s
local SPIRAL_LIFE  = motion.spiral.life_s
local SPIRAL_SIZE  = look.spiral.size
local SPIRAL_SPIN  = motion.spiral.spin_deg_per_s    -- deg/s, random direction
local SPIRAL_BACKLOG = motion.spiral.backlog_max
local SPIRAL_ALPHA = look.spiral.opacity
local SHADOW_COLS  = look.shadow_colors

local CLOUD_COLS, CLOUD_ROWS = look.cloud.cols, look.cloud.rows
local CLOUD_SCALE   = look.cloud.scale
local CLOUD_STEPS   = look.cloud.scale_steps
local CLOUD_SWAY    = motion.cloud.sway_px
local CLOUD_DRIFT   = motion.cloud.drift_px
local CLOUD_DRIFT_RATE = motion.cloud.drift_rad_per_s
local CLOUD_SWAY_X_RATE, CLOUD_SWAY_Y_RATE = motion.cloud.sway_x_rad_per_s, motion.cloud.sway_y_rad_per_s
local CLOUD_SWAY_Y_STEP = motion.cloud.sway_y_phase_step
local CLOUD_OPACITY = look.cloud.opacity
local CLOUD_COL_A   = look.cloud.a_color
local CLOUD_COL_B   = look.cloud.b_color
-- b.deep (0..1) moves a box toward these colours and a thicker bank
local DEEP_SHADOW_COLS = look.deep_shadow_colors
local DEEP_CLOUD_COL_A = look.cloud.deep_a_color
local DEEP_CLOUD_COL_B = look.cloud.deep_b_color
local DEEP_CLOUD_OPACITY = look.cloud.deep_opacity

local scatter = motion.scatter
local SCATTER_REACH, SCATTER_RISE, SCATTER_PUSH = scatter.reach_px, scatter.rise_px_per_s, scatter.push_px_per_s
local SCATTER_DRAIN = scatter.life_drain
local SCATTER_SPREAD, SCATTER_CLOUD_RISE = scatter.cloud_spread, scatter.cloud_rise_px

local CULL_MARGIN = look.cull_margin_px              -- boxes this far off screen still simulate
local SPLIT_X = look.split_x_px

-- tex = { cloud, shadow, spiral } (their state is reset after drawing); opts: splitX (a scatter blows the
-- motes left of it left, the others right), margin (the cull margin), density (share of the motes, default 1)
function D.new(tex, opts)
    opts = opts or {}
    local density = opts.density or 1
    return setmetatable({
        tex = tex,
        splitX = opts.splitX or SPLIT_X,
        margin = opts.margin or CULL_MARGIN,
        rate = SHADOW_RATE * density, spirals = SPIRAL_RATE * density,
        max = math.ceil(SHADOW_MAX * density),
        t = 0, cam = 0,
        particles = {}, emit_acc = 0, spiral_acc = 0,
    }, D)
end

function D:reset()
    self.particles, self.emit_acc, self.spiral_acc = {}, 0, 0
end

local function spawn_particle(self, kind, b)
    local lr = (kind == "spiral") and SPIRAL_LIFE or SHADOW_LIFE
    local sr = (kind == "spiral") and SPIRAL_SIZE or SHADOW_SIZE
    local life = lr[1] + math.random() * (lr[2] - lr[1])
    local dk = b.deep or 0
    self.particles[#self.particles + 1] = {
        kind = kind,
        x  = b[1] + math.random() * b[3],
        wy = b[2] + math.random() * b[4],
        vy = -(SHADOW_RISE[1] + math.random() * (SHADOW_RISE[2] - SHADOW_RISE[1])),
        phase = math.random() * 6.28,
        life = life, max = life,
        size = sr[1] + math.random() * (sr[2] - sr[1]),
        col  = (dk > 0 and (dk >= 1 or math.random() < dk)) and DEEP_SHADOW_COLS[math.random(#DEEP_SHADOW_COLS)]
               or SHADOW_COLS[math.random(#SHADOW_COLS)],
        rot  = math.random() * 360,
        spin = (SPIRAL_SPIN[1] + math.random() * (SPIRAL_SPIN[2] - SPIRAL_SPIN[1]))
               * (math.random() < 0.5 and 1 or -1),
    }
end

local function emit_particles(self, dt, all)
    local res = THEME:GetResolution()
    local mg = self.margin
    local boxes = {}
    for _, b in ipairs(all) do
        if b.scatter == nil then
            local sy = b[2] - self.cam
            if sy < res.Y + mg and sy + b[4] > -mg then boxes[#boxes + 1] = b end
        end
    end
    if #boxes == 0 then return end
    local particles = self.particles
    self.emit_acc = math.min(SHADOW_BACKLOG, self.emit_acc + self.rate * #boxes * dt)
    while self.emit_acc >= 1 and #particles < self.max do
        self.emit_acc = self.emit_acc - 1
        spawn_particle(self, "mote", boxes[math.random(#boxes)])
    end
    self.spiral_acc = math.min(SPIRAL_BACKLOG, self.spiral_acc + self.spirals * #boxes * dt)
    while self.spiral_acc >= 1 and #particles < self.max do
        self.spiral_acc = self.spiral_acc - 1
        spawn_particle(self, "spiral", boxes[math.random(#boxes)])
    end
end

local function update_particles(self, dt, all)
    local scat = nil
    for _, b in ipairs(all) do
        if b.scatter ~= nil then
            scat = scat or {}
            scat[#scat + 1] = b
        end
    end
    local split = self.splitX
    local particles = self.particles
    local i = 1
    while i <= #particles do
        local p = particles[i]
        p.life = p.life - dt
        if p.life <= 0 then
            particles[i] = particles[#particles]
            particles[#particles] = nil
        else
            p.phase = p.phase + dt * SWIRL_RATE
            p.wy = p.wy + p.vy * dt
            p.x  = p.x + math.cos(p.phase) * SHADOW_SWIRL * dt
            if scat ~= nil then
                for _, b in ipairs(scat) do
                    if p.wy >= b[2] - SCATTER_REACH and p.wy <= b[2] + b[4] + SCATTER_REACH then
                        p.wy = p.wy - SCATTER_RISE * dt
                        p.x  = p.x + ((p.x > split) and SCATTER_PUSH or -SCATTER_PUSH) * dt
                        p.life = p.life - dt * SCATTER_DRAIN
                        break
                    end
                end
            end
            if p.kind == "spiral" then p.rot = p.rot + p.spin * dt end
            i = i + 1
        end
    end
end

local function draw_particles(self)
    local tex, cam = self.tex, self.cam
    local shadow, spiral = tex.shadow, tex.spiral
    local shReady = shadow ~= nil and shadow.Ready
    local spReady = spiral ~= nil and spiral.Ready
    for _, p in ipairs(self.particles) do
        local sp = p.kind == "spiral"
        local tx = sp and spiral or shadow
        if (sp and spReady) or (not sp and shReady) then
            local a = math.max(0, math.sin((1 - p.life / p.max) * math.pi))
            tx:SetScale(p.size, p.size)
            tx:SetColor(p.col[1], p.col[2], p.col[3])
            tx:SetOpacity(a * (sp and SPIRAL_ALPHA or SHADOW_ALPHA))
            if sp then tx:SetRotation(p.rot) end
            tx:DrawAtAnchor(p.x, p.wy - cam, "center")
            if sp then tx:SetRotation(0) end
        end
    end
    if shadow ~= nil then
        shadow:SetColor(1, 1, 1) ; shadow:SetOpacity(1) ; shadow:SetScale(1, 1)
        shadow:SetRotation(0) ; shadow:SetBlendMode("Normal")
    end
    if spiral ~= nil then
        spiral:SetColor(1, 1, 1) ; spiral:SetOpacity(1) ; spiral:SetScale(1, 1)
        spiral:SetRotation(0) ; spiral:SetBlendMode("Normal")
    end
end

local function clamp_center(v, lo, hi)
    if lo >= hi then return (lo + hi) * 0.5 end
    return math.max(lo, math.min(hi, v))
end

-- puffs clamped fully inside each box
local function draw_clouds(self, all)
    local cl = self.tex.cloud
    if cl == nil or not cl.Ready then return end
    local res = THEME:GetResolution()
    local mg = self.margin
    local t = self.t
    local drift = math.sin(t * CLOUD_DRIFT_RATE) * CLOUD_DRIFT
    for _, b in ipairs(all) do
        local sct = b.scatter or 0
        local x, wy, w, h = b[1], b[2], b[3], b[4]
        local dk = b.deep or 0
        local a1 = CLOUD_COL_A[1] + (DEEP_CLOUD_COL_A[1] - CLOUD_COL_A[1]) * dk
        local a2 = CLOUD_COL_A[2] + (DEEP_CLOUD_COL_A[2] - CLOUD_COL_A[2]) * dk
        local a3 = CLOUD_COL_A[3] + (DEEP_CLOUD_COL_A[3] - CLOUD_COL_A[3]) * dk
        local b1 = CLOUD_COL_B[1] + (DEEP_CLOUD_COL_B[1] - CLOUD_COL_B[1]) * dk
        local b2 = CLOUD_COL_B[2] + (DEEP_CLOUD_COL_B[2] - CLOUD_COL_B[2]) * dk
        local b3 = CLOUD_COL_B[3] + (DEEP_CLOUD_COL_B[3] - CLOUD_COL_B[3]) * dk
        local op = CLOUD_OPACITY + (DEEP_CLOUD_OPACITY - CLOUD_OPACITY) * dk
        if sct < 1 then
            local sy = wy - self.cam
            if sy < res.Y + mg and sy + h > -mg then
                for cxi = 0, CLOUD_COLS - 1 do
                    for cyi = 0, CLOUD_ROWS - 1 do
                        local u = (cxi + 0.5) / CLOUD_COLS * 2 - 1
                        local v = (cyi + 0.5) / CLOUD_ROWS * 2 - 1
                        if u * u + v * v <= 1.0 then
                            local idx = cxi * CLOUD_ROWS + cyi
                            local sc = CLOUD_SCALE[1] + (idx % CLOUD_STEPS) / CLOUD_STEPS * (CLOUD_SCALE[2] - CLOUD_SCALE[1])
                            local hw, hh = sc * cl.Width * 0.5, sc * cl.Height * 0.5
                            local px = x  + (cxi + 0.5) / CLOUD_COLS * w + drift + math.sin(t * CLOUD_SWAY_X_RATE + idx) * CLOUD_SWAY
                            local py = sy + (cyi + 0.5) / CLOUD_ROWS * h
                                       + math.cos(t * CLOUD_SWAY_Y_RATE + idx * CLOUD_SWAY_Y_STEP) * (CLOUD_SWAY * 0.5)
                            px = clamp_center(px, x + hw,  x + w - hw)
                            py = clamp_center(py, sy + hh, sy + h - hh)
                            if sct > 0 then
                                px = px + (px - (x + w * 0.5)) * sct * SCATTER_SPREAD
                                py = py + (py - (sy + h * 0.5)) * sct * SCATTER_SPREAD - sct * SCATTER_CLOUD_RISE
                            end
                            local k = (idx * 0.618034) % 1
                            cl:SetScale(sc, sc)
                            cl:SetColor(a1 + (b1 - a1) * k, a2 + (b2 - a2) * k, a3 + (b3 - a3) * k)
                            cl:SetOpacity(op * (1 - sct) * (1 - sct))
                            cl:DrawAtAnchor(px, py, "center")
                        end
                    end
                end
            end
        end
    end
    cl:SetColor(1, 1, 1) ; cl:SetOpacity(1) ; cl:SetScale(1, 1)
    cl:SetRotation(0) ; cl:SetBlendMode("Normal")
end

-- once per draw call; a box is { x, wy, w, h } (x screen space, screen y = wy - camY);
-- b.scatter (0..1), when set, stops its emission and blows its motes and bank away
function D:frame(dt, boxes, camY)
    boxes = boxes or {}
    self.cam = camY or 0
    self.t = self.t + dt
    draw_clouds(self, boxes)
    update_particles(self, dt, boxes)
    emit_particles(self, dt, boxes)
    draw_particles(self)
end

return D
