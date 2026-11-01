-- Instance-based 2D particle pool driven by a preset table (fx/presets.lua).
--
-- Preset fields (ranges are {a, b} or a plain number):
--   rate (spawns/s), max, life                  required
--   tex          manifest key or list of keys, looked up when tex is a key -> handle map
--   area         default spawn rect {x, y, w, h} (screen space, fx.emitter.default_area when absent)
--   zones        {{x, y, w, h, p = chance}, ...} rects tried before the area
--   prewarm      seconds simulated on the first update
--   every, burst, spread   periodic bursts: every {a, b} s, `burst` particles, jitter {sx, sy}
--   blend        'Normal' | 'Add'; addMix = chance a particle draws in a second Add pass
--   size, sizeEnd, vx, vy, gravity, drag
--   sway {amp, freq}   horizontal offset amp*sin(2pi*freq*age), freq in Hz
--   spin               deg/s with a random sign; flutterHz squeezes scaleX to 0.25..1
--   turb {amp, hz}     horizontal velocity amp*sin(2pi*hz*age); rangeX clamps x to spawn x +- rangeX
--   colors | ramp {c0, c1[, c2]}
--   alpha, fade 'arch' | 'out' | 'inout' (fin, fout s) | {fin, fout}, twinkle {amp, hz}

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local E = {}
E.__index = E

local TAU = math.pi * 2
local MAX_SPAWN = Animation.fx.emitter.spawn_backlog_max
local PREWARM_STEP = 1 / Animation.fx.emitter.prewarm_steps_per_s
local FLUTTER_BASE, FLUTTER_AMP = Animation.fx.emitter.flutter.base, Animation.fx.emitter.flutter.amp
local defaults = Animation.fx.emitter.defaults
local DEF_LIFE, DEF_SWAY_HZ, DEF_TWINKLE_HZ = defaults.life_s, defaults.sway_freq_hz, defaults.twinkle_hz
local DEF_FADE, DEF_FIN, DEF_FOUT = defaults.fade, defaults.fade_in_s, defaults.fade_out_s
local DEF_AREA = Layout.fx.emitter.default_area
local DEF_SIZE, DEF_ALPHA = Layout.fx.emitter.default_size, Layout.fx.emitter.default_opacity

local function rnd(r, default)
    if r == nil then return default or 0 end
    if type(r) == "number" then return r end
    return r[1] + math.random() * (r[2] - r[1])
end

local function is_handle(t)
    return type(t) == "userdata" or (type(t) == "table" and t.DrawAtAnchor ~= nil)
end

-- the manifest keys a preset draws with, as a list (empty without a preset)
function E.texKeys(preset)
    local k = preset ~= nil and preset.tex or nil
    if type(k) == "string" then return { k } end
    if type(k) == "table" then return k end
    return {}
end

local function resolve_textures(preset, tex)
    if tex == nil then return {} end
    if is_handle(tex) then return { tex } end
    local list = {}
    for _, k in ipairs(E.texKeys(preset)) do
        if tex[k] ~= nil then list[#list + 1] = tex[k] end
    end
    if #list == 0 then
        for _, h in ipairs(tex) do list[#list + 1] = h end
    end
    return list
end

function E.new(preset, tex)
    local e = setmetatable({
        p = preset,
        texs = resolve_textures(preset, tex),
        parts = {},
        acc = 0,
        mult = 1,
        burstT = nil,
        warm = preset.prewarm,
        mode = "area",
        ax = DEF_AREA[1], ay = DEF_AREA[2], aw = DEF_AREA[3], ah = DEF_AREA[4],
        px = 0, py = 0, sx = 0, sy = 0,
    }, E)
    local a = preset.area
    if a ~= nil then e.ax, e.ay, e.aw, e.ah = a[1], a[2], a[3], a[4] end
    return e
end

function E:setArea(x, y, w, h)
    self.mode = "area"
    self.ax, self.ay, self.aw, self.ah = x, y, w or 0, h or 0
end

function E:setPoint(x, y, sx, sy)
    self.mode = "point"
    self.px, self.py, self.sx, self.sy = x, y, sx or 0, sy or 0
end

function E:setRate(mult)
    self.mult = math.max(0, mult or 1)
end

-- moves the live particles with their source, for an emitter following something that moves
function E:carry(dx, dy)
    if dx == 0 and dy == 0 then return end
    for _, q in ipairs(self.parts) do
        q.x, q.y, q.x0 = q.x + dx, q.y + dy, q.x0 + dx
    end
end

local function spawn_pos(e)
    if e.mode == "point" then
        return e.px + (math.random() * 2 - 1) * e.sx, e.py + (math.random() * 2 - 1) * e.sy
    end
    local zones = e.p.zones
    if zones ~= nil then
        local r = math.random()
        for _, z in ipairs(zones) do
            if r < z.p then return z[1] + math.random() * z[3], z[2] + math.random() * z[4] end
            r = r - z.p
        end
    end
    return e.ax + math.random() * e.aw, e.ay + math.random() * e.ah
end

local function spawn(e, x, y)
    local p = e.p
    local s0 = rnd(p.size, DEF_SIZE)
    local q = {
        x = x, y = y, x0 = x,
        vx = rnd(p.vx), vy = rnd(p.vy),
        age = 0, life = math.max(0.01, rnd(p.life, DEF_LIFE)),
        s0 = s0, s1 = p.sizeEnd or s0,
        rot = 0, spin = 0,
        ph = math.random() * TAU,
        ti = (#e.texs > 1) and math.random(#e.texs) or 1,
    }
    if p.spin ~= nil then
        q.rot = math.random() * 360
        q.spin = rnd(p.spin) * (math.random() < 0.5 and 1 or -1)
    end
    if p.sway ~= nil then q.swA, q.swF = p.sway.amp or 0, rnd(p.sway.freq, DEF_SWAY_HZ) end
    if p.twinkle ~= nil then q.twF = rnd(p.twinkle.hz, DEF_TWINKLE_HZ) end
    if p.colors ~= nil then q.c = p.colors[math.random(#p.colors)] end
    if p.addMix ~= nil then q.add = math.random() < p.addMix end
    e.parts[#e.parts + 1] = q
end

function E:burst(n, x, y)
    local p = self.p
    local sp = p.spread
    for _ = 1, n or 1 do
        if #self.parts >= p.max then return end
        if x ~= nil then
            local jx = sp and (math.random() * 2 - 1) * sp[1] or 0
            local jy = sp and (math.random() * 2 - 1) * sp[2] or 0
            spawn(self, x + jx, (y or 0) + jy)
        else
            spawn(self, spawn_pos(self))
        end
    end
end

function E:update(dt)
    if self.warm ~= nil then
        local w = self.warm
        self.warm = nil
        for _ = 1, math.floor(w / PREWARM_STEP) do self:update(PREWARM_STEP) end
    end
    if dt == nil or dt <= 0 then return end
    local p = self.p
    local rate = (p.rate or 0) * self.mult
    if rate > 0 then
        self.acc = math.min(MAX_SPAWN, self.acc + rate * dt)
        while self.acc >= 1 and #self.parts < p.max do
            self.acc = self.acc - 1
            spawn(self, spawn_pos(self))
        end
    end
    if p.every ~= nil and self.mult > 0 then
        if self.burstT == nil then self.burstT = rnd(p.every) end
        self.burstT = self.burstT - dt
        if self.burstT <= 0 then
            self.burstT = rnd(p.every)
            local bx, by = spawn_pos(self)
            self:burst(p.burst or 1, bx, by)
        end
    end
    local grav, drag, turb, rx = p.gravity, p.drag, p.turb, p.rangeX
    local k = drag and math.max(0, 1 - drag * dt) or 1
    local parts = self.parts
    local i = 1
    while i <= #parts do
        local q = parts[i]
        q.age = q.age + dt
        if q.age >= q.life then
            parts[i] = parts[#parts]
            parts[#parts] = nil
        else
            if grav then q.vy = q.vy + grav * dt end
            if drag then q.vx, q.vy = q.vx * k, q.vy * k end
            q.x = q.x + q.vx * dt
            q.y = q.y + q.vy * dt
            if turb then q.x = q.x + turb.amp * math.sin(TAU * turb.hz * q.age + q.ph) * dt end
            if rx then q.x = math.max(q.x0 - rx, math.min(q.x0 + rx, q.x)) end
            q.rot = q.rot + q.spin * dt
            i = i + 1
        end
    end
end

local function fade_of(p, q, u)
    local f = p.fade or DEF_FADE
    if f == "arch" then return math.sin(u * math.pi) end
    if f == "out" then return 1 - u end
    local fin, fout = p.fin or DEF_FIN, p.fout or DEF_FOUT
    if type(f) == "table" then fin, fout = f.fin or f[1] or 0, f.fout or f[2] or 0 end
    local a = 1
    if fin > 0 and q.age < fin then a = q.age / fin end
    local left = q.life - q.age
    if fout > 0 and left < fout then a = math.min(a, left / fout) end
    return a
end

local function colour_of(p, q, u)
    local rp = p.ramp
    if rp ~= nil then
        local c0, c1 = rp[1], rp[2]
        if rp[3] ~= nil then
            if u < 0.5 then u = u * 2 else c0, c1, u = rp[2], rp[3], u * 2 - 1 end
        end
        return c0[1] + (c1[1] - c0[1]) * u, c0[2] + (c1[2] - c0[2]) * u, c0[3] + (c1[3] - c0[3]) * u
    end
    local c = q.c
    if c ~= nil then return c[1], c[2], c[3] end
    return 1, 1, 1
end

local function draw_pass(e, add, blend, ready, ox, oy, mulA, r, g, b)
    local p = e.p
    for i, tx in ipairs(e.texs) do
        if ready[i] then tx:SetBlendMode(blend) end
    end
    local peak = (p.alpha or DEF_ALPHA) * mulA
    local tw, fl, spin = p.twinkle, p.flutterHz, p.spin ~= nil
    for _, q in ipairs(e.parts) do
        if (add == nil or q.add == add) and ready[q.ti] then
            local u = q.age / q.life
            local a = fade_of(p, q, u) * peak
            if tw ~= nil then
                a = a * math.max(0, (1 - tw.amp) + tw.amp * math.sin(TAU * q.twF * q.age + q.ph))
            end
            if a > 0.002 then
                local tx = e.texs[q.ti]
                local s = q.s0 + (q.s1 - q.s0) * u
                local sx = s
                if fl ~= nil then sx = s * (FLUTTER_BASE + FLUTTER_AMP * math.abs(math.cos(TAU * fl * q.age + q.ph))) end
                local cr, cg, cb = colour_of(p, q, u)
                local x = q.x + ox
                if q.swA ~= nil then x = x + q.swA * math.sin(TAU * q.swF * q.age + q.ph) end
                tx:SetScale(sx, s)
                if spin then tx:SetRotation(q.rot) end
                tx:SetColor(cr * r, cg * g, cb * b)
                tx:SetOpacity(math.min(1, a))
                tx:DrawAtAnchor(x, q.y + oy, "center")
            end
        end
    end
end

function E:draw(ox, oy, mulA, r, g, b)
    if #self.parts == 0 then return end
    mulA = mulA or 1
    if mulA <= 0 then return end
    ox, oy, r, g, b = ox or 0, oy or 0, r or 1, g or 1, b or 1
    local ready, any = {}, false
    for i, tx in ipairs(self.texs) do
        ready[i] = tx.Ready
        any = any or ready[i]
    end
    if not any then return end
    local p = self.p
    if p.addMix ~= nil then
        draw_pass(self, false, "Normal", ready, ox, oy, mulA, r, g, b)
        draw_pass(self, true, "Add", ready, ox, oy, mulA, r, g, b)
    else
        draw_pass(self, nil, p.blend or "Normal", ready, ox, oy, mulA, r, g, b)
    end
    for i, tx in ipairs(self.texs) do
        if ready[i] then
            tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
            tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
        end
    end
end

function E:count()
    return #self.parts
end

function E:clear()
    self.parts, self.acc, self.burstT = {}, 0, nil
end

return E
