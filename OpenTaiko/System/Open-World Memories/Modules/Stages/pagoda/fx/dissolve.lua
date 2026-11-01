-- Dissolve of a beaten monster's sprite into dust, drawn as column strips through encounter/sprite.

local assets = require("encounter/assets")
local sprite = require("encounter/sprite")
local Easing = require("Easing")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local D = {}
D.__index = D

local TAU = math.pi * 2
local sin, floor, min, max, random = math.sin, math.floor, math.min, math.max, math.random

local look = Layout.fx.dissolve
local motion = Animation.fx.dissolve

local DARK_SEC = motion.dark_s
local EAT_FROM, EAT_SEC = motion.eat_from_s, motion.eat_s     -- front start, and its run from top to bottom
local FRONT_BASE, FRONT_AMP = motion.front_ease.base, motion.front_ease.amp
local noise = motion.front_noise
local NA_AMP, NA_ACROSS, NA_HZ = noise.wave_a.amp, noise.wave_a.across, noise.wave_a.hz
local NB_AMP, NB_ACROSS, NB_HZ = noise.wave_b.amp, noise.wave_b.across, noise.wave_b.hz
local N_JITTER, N_FLICKER = noise.jitter, noise.flicker
local COL_HZ_BASE, COL_HZ_JITTER = motion.column_flicker_hz.base, motion.column_flicker_hz.jitter
local DARK_R, DARK_G, DARK_B = look.dark_color[1], look.dark_color[2], look.dark_color[3]
local COLUMNS, COLUMN_MIN = look.columns, look.column_min_px  -- strips across the sprite, smallest mean strip width (px)
local RAG, RAG_MIN = look.rag_share, look.rag_min_px          -- ragged front depth: share of the height, px
local EDGE, EDGE_ALPHA = look.edge_px, look.edge_opacity      -- px under the front drawn thinned
local JITTER = look.spawn_jitter_px

-- shed per point as the front passes (plain copies: haze has no spin)
local GRAIN = Config.plain(motion.grain)
GRAIN.key = "fx_ash"
local HAZE = Config.plain(motion.haze)
HAZE.key = "fx_dust"
local KINDS = { HAZE, GRAIN }               -- drawing order
local WIND = motion.wind_px_per_s           -- px/s to the right
local RISE, DRAG = motion.rise_px_per_s2, motion.drag_per_s   -- upward pull (px/s per s), share of the speed lost per s
local TURB, TURB_HZ = motion.turb_px_per_s, motion.turb_hz    -- sideways, px/s
local DUST_R, DUST_G, DUST_B = look.dust_color[1], look.dust_color[2], look.dust_color[3]

local function rnd(r) return r[1] + random() * (r[2] - r[1]) end

function D.keys()
    local keys = {}
    for i, k in ipairs(KINDS) do keys[i] = k.key end
    return keys
end

-- spriteKey = the manifest key of the drawn sprite; the dust starts on manifest_result dissolve[key] (sprite px)
function D.new(spriteKey)
    local meta = assets.meta(spriteKey)
    local w, h = 0, 0
    if meta ~= nil then w, h = meta.w, meta.h end
    -- strips of random widths around the mean; colAt[x] = the strip holding sprite column x
    local mean = max(COLUMN_MIN, w / COLUMNS)
    local cols, colAt, x0 = {}, {}, 0
    while x0 < w do
        local cw = min(w - x0, max(2, floor(mean * (0.5 + random()) + 0.5)))
        cols[#cols + 1] = {
            x = x0, w = cw, u = (x0 + cw * 0.5) / w, cut = 0,
            j = random() * 2 - 1, hz = COL_HZ_BASE + COL_HZ_JITTER * random(), ph = random() * TAU,
        }
        for x = x0, x0 + cw - 1 do colAt[x] = #cols end
        x0 = x0 + cw
    end
    local all = assets.data("dissolve")
    local d = setmetatable({
        h = h, w = w, cols = cols, colAt = colAt,
        rag = max(RAG_MIN, RAG * h),
        pts = (meta ~= nil and all ~= nil and all[spriteKey]) or {},
        next = 1, peeled = {},
        parts = {}, tex = {},
        t = 0, ph1 = random() * TAU, ph2 = random() * TAU,
    }, D)
    for _, k in ipairs(KINDS) do
        d.parts[k] = {}
        d.tex[k] = (assets.get(k.key))
    end
    return d
end

-- the front's base row: from above the top (nothing eaten) to below the bottom (all eaten)
local function front(self)
    local u = Easing.span(self.t, EAT_FROM, EAT_SEC)
    u = u * (FRONT_BASE + FRONT_AMP * u)
    return -self.rag + (self.h + 2 * self.rag) * u
end

-- each strip's first drawn row: the base row plus noise; it only ever moves down
local function cutColumns(self)
    local base, rag, t, h = front(self), self.rag, self.t, self.h
    for _, c in ipairs(self.cols) do
        local n = NA_AMP * sin(TAU * (NA_ACROSS * c.u + NA_HZ * t) + self.ph1)
                + NB_AMP * sin(TAU * (NB_ACROSS * c.u - NB_HZ * t) + self.ph2)
                + N_JITTER * c.j + N_FLICKER * sin(TAU * c.hz * t + c.ph)
        c.cut = max(c.cut, min(h, floor(base + rag * n)))
    end
    return base
end

local function spawn(self, k, x, y)
    local list = self.parts[k]
    if #list >= k.max then return end
    list[#list + 1] = {
        x = x + (random() * 2 - 1) * JITTER, y = y + (random() * 2 - 1) * JITTER,
        dx = 0, dy = 0, vx = rnd(k.vx_px_per_s), vy = rnd(k.vy_px_per_s), wind = rnd(WIND),
        age = 0, life = rnd(k.life_s), s = rnd(k.size), sq = 1 + (random() * 2 - 1) * k.squash,
        rot = k.spin_deg_per_s and random() * 360 or 0,
        spin = k.spin_deg_per_s and rnd(k.spin_deg_per_s) * (random() < 0.5 and -1 or 1) or 0,
        ph = random() * TAU, hz = rnd(TURB_HZ),
    }
end

-- the points the front has passed shed their dust (points are sorted top-down)
local function peel(self, base)
    local pts, peeled, cols, colAt = self.pts, self.peeled, self.cols, self.colAt
    local limit = base + self.rag
    local i = self.next
    while i <= #pts and pts[i][2] < limit do
        if not peeled[i] then
            local p = pts[i]
            local c = cols[colAt[max(0, min(self.w - 1, p[1]))] or 1]
            if p[2] < c.cut then
                peeled[i] = true
                for _, k in ipairs(KINDS) do
                    local n = k.count_base + ((random() < k.extra_chance) and 1 or 0)
                    for _ = 1, n do spawn(self, k, p[1], p[2]) end
                end
            end
        end
        i = i + 1
    end
    while self.next <= #pts and peeled[self.next] do self.next = self.next + 1 end
end

local function move(list, dt)
    local k = max(0, 1 - DRAG * dt)
    local i = 1
    while i <= #list do
        local q = list[i]
        q.age = q.age + dt
        if q.age >= q.life then
            list[i] = list[#list]
            list[#list] = nil
        else
            q.vx = q.vx * k
            q.vy = (q.vy + RISE * dt) * k
            q.dx = q.dx + (q.vx + q.wind + TURB * sin(TAU * q.hz * q.age + q.ph)) * dt
            q.dy = q.dy + q.vy * dt
            q.rot = q.rot + q.spin * dt
            i = i + 1
        end
    end
end

function D:update(dt)
    if dt == nil or dt <= 0 then return end
    self.t = self.t + dt
    if self.t >= EAT_FROM then peel(self, cutColumns(self)) end
    for _, k in ipairs(KINDS) do move(self.parts[k], dt) end
end

-- true once the sprite is eaten and the last of the dust has faded
function D:done()
    if self.t < EAT_FROM + EAT_SEC then return false end
    for _, k in ipairs(KINDS) do
        if #self.parts[k] > 0 then return false end
    end
    return true
end

-- 0..1: how far the sprite has darkened
function D:darkness()
    return Easing.outQuad(self.t / DARK_SEC)
end

-- 0..1: the share of the sprite's height not eaten yet
function D:remaining()
    if self.h <= 0 then return 0 end
    return 1 - Easing.linear(front(self) / self.h)
end

-- true once the front has eaten the sprite's pixel (x, y)
function D:eaten(x, y)
    if self.w <= 0 then return true end
    local c = self.cols[self.colAt[max(0, min(self.w - 1, floor(x)))] or 1]
    return c ~= nil and y < c.cut
end

-- a sprite rect placed by its top-left, so strips of any size meet edge to edge; (bx, by) is the
-- shift the "center" anchor gives the whole sprite (half the int size), so the strips line up with it
local function strip(tex, meta, x, y, o, sx, sy, bx, by, rx, ry, rw, rh)
    local px, py = sprite.point(meta, x, y, o, rx + rw * 0.5, ry + rh * 0.5)
    tex:DrawRectAtAnchor(px + bx - rw * sx * 0.5, py + by - rh * sy * 0.5, rx, ry, rw, rh, "topleft")
end

local function drawSprite(self, tex, meta, x, y, o, alpha, r, g, b)
    if tex == nil or not tex.Ready or meta == nil or self.t >= EAT_FROM + EAT_SEC then return end
    local k = self:darkness()
    local sx, sy, rot = 1, 1, 0
    if o ~= nil then sx, sy, rot = o.sx or 1, o.sy or 1, o.rot or 0 end
    tex:SetScale(sx, sy)
    tex:SetRotation(rot)
    tex:SetColor(r * (1 - k + k * DARK_R), g * (1 - k + k * DARK_G), b * (1 - k + k * DARK_B))
    tex:SetBlendMode("Normal")
    local h = min(self.h, meta.h)
    if self.t < EAT_FROM then
        tex:SetOpacity(alpha)
        local cx, cy = sprite.center(meta, x, y, o)
        tex:DrawAtAnchor(cx, cy, "center")
    else
        local bx, by = (meta.w % 2) * sx * 0.5, (meta.h % 2) * sy * 0.5
        tex:SetOpacity(alpha * EDGE_ALPHA)
        for _, c in ipairs(self.cols) do
            if c.cut > 0 and c.cut < h then
                strip(tex, meta, x, y, o, sx, sy, bx, by, c.x, c.cut, c.w, min(h, c.cut + EDGE) - c.cut)
            end
        end
        tex:SetOpacity(alpha)
        for _, c in ipairs(self.cols) do
            local top = (c.cut > 0) and c.cut + EDGE or 0
            if top < h then strip(tex, meta, x, y, o, sx, sy, bx, by, c.x, top, c.w, h - top) end
        end
    end
    tex:SetScale(1, 1)
    tex:SetRotation(0)
    tex:SetOpacity(1)
    tex:SetColor(1, 1, 1)
end

local function drawDust(self, k, meta, x, y, o, sc, alpha, r, g, b)
    local tx, list = self.tex[k], self.parts[k]
    if tx == nil or not tx.Ready or #list == 0 then return end
    tx:SetBlendMode("Normal")
    tx:SetColor(DUST_R * r, DUST_G * g, DUST_B * b)
    tx:SetRotation(0)
    for _, q in ipairs(list) do
        local u = q.age / q.life
        local a = min(1, q.age / k.fade_in_s, (1 - u) / k.fade_out_share) * k.opacity * alpha
        if a > 0.01 then
            local px, py = sprite.point(meta, x, y, o, q.x, q.y)
            local s = q.s * sc * (1 + (k.size_end - 1) * u)
            tx:SetScale(s * q.sq, s / q.sq)
            if k.spin_deg_per_s then tx:SetRotation(q.rot) end
            tx:SetOpacity(a)
            tx:DrawAtAnchor(px + q.dx, py + q.dy, "center")
        end
    end
    tx:SetScale(1, 1)
    tx:SetRotation(0)
    tx:SetOpacity(1)
    tx:SetColor(1, 1, 1)
end

-- tex, meta, x, y, o: the sprite placement encounter/sprite.draw takes (o: sx, sy, rot, pivot);
-- alpha, r, g, b: opacity and filter of the sprite and its dust
function D:draw(tex, meta, x, y, o, alpha, r, g, b)
    alpha, r, g, b = alpha or 1, r or 1, g or 1, b or 1
    if alpha <= 0 then return end
    drawSprite(self, tex, meta, x, y, o, alpha, r, g, b)
    if meta == nil then return end
    local sc = 1
    if o ~= nil then sc = ((o.sx or 1) + (o.sy or 1)) * 0.5 end
    for _, k in ipairs(KINDS) do drawDust(self, k, meta, x, y, o, sc, alpha, r, g, b) end
end

return D
