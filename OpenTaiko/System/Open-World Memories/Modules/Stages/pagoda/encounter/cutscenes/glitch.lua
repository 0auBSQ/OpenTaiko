-- Glitch effect of the level-20 cutscene figure.

local Config = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local Glitch = {}
Glitch.__index = Glitch

local Look = Layout.encounter.cutscenes.glitch
local Anim = Animation.encounter.cutscenes.glitch

local REF_W, REF_H = 1920, 1080
local BURST_EVERY = Anim.burst_every_s -- at rate 1
local BURST_LEN   = Anim.burst_s
local RESLICE_SEC = Anim.reslice_s
local BANDS       = Look.slices
local BAND_SHIFT  = Look.slice_shift_share  -- share of the bands shifted
local BAND_DX     = Look.slice_dx_px   -- texture px
local SPLIT_DX    = Look.split_dx_px
local SPLIT_A     = Look.split_opacity
local SPLIT_RED, SPLIT_CYAN = Look.split_red_color, Look.split_cyan_color
local SOUND_ODDS  = Anim.sound_odds    -- chance a voiced burst plays the sound

-- relative to the figure's eye (1920 space at scale 1)
local Ambient = Look.ambient
local AMBIENT_N    = Ambient.count
local AMBIENT_AREA = Ambient.area
local AMBIENT_HOLD = Anim.ambient_hold_s
local AMBIENT_GAP  = Anim.ambient_gap_s
local AMBIENT_FADE = Anim.ambient_fade_s
local AMBIENT_SCALE = Ambient.scale
local AMBIENT_A    = Ambient.opacity
local JUMP_ODDS    = Anim.jump_per_s   -- per second
local JUMP_X, JUMP_Y = Anim.jump_px.x, Anim.jump_px.y

local Tears = Look.tears
local TEARS     = Tears.count
local TEAR_X    = { Tears.left_px, REF_W - Tears.right_inset_px }
local TEAR_W    = Tears.w_px
local TEAR_H    = Tears.h_px
local TEAR_A    = Tears.opacity
local TEAR_TILES = Tears.extra_count
local TINTS = Look.tint_colors

local function rnd(r) return r[1] + math.random() * (r[2] - r[1]) end
local function irnd(r) return math.random(r[1], r[2]) end

local function reset(tx)
    tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
    tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

-- tex = { atlas, white }; rects = the atlas tiles ({ x, y, w, h }); sfx = the glitch sound or nil;
-- size (default 1) scales the slice push and colour split, in texture px
function Glitch.new(tex, rects, sfx, size)
    local self = setmetatable({ tex = tex, rects = rects or {}, sfx = sfx, size = size or 1 }, Glitch)
    self.rate, self.voiced = 1, true
    self.wait = rnd(BURST_EVERY) * 0.5
    self.burst, self.slice, self.forced = 0, 0, 0
    self.bands, self.split, self.tears = {}, 0, {}
    self.ambient = {}
    for i = 1, AMBIENT_N do self.ambient[i] = { a = 0, state = "gap", t = rnd(AMBIENT_GAP) * i } end
    return self
end

-- k scales how often bursts come (0: none); voiced: they play the glitch sound
function Glitch:setRate(k, voiced)
    self.rate = math.max(0, k or 1)
    self.voiced = voiced ~= false
end

-- bursts without a break for sec seconds
function Glitch:force(sec)
    self.forced = math.max(self.forced, sec or 0)
end

function Glitch:active()
    return self.burst > 0 or self.forced > 0
end

local function newTear(self)
    local n = #self.rects
    local t = {
        x = rnd(TEAR_X), y = rnd({ 0, REF_H }),
        w = rnd(TEAR_W), h = rnd(TEAR_H), a = rnd(TEAR_A), c = TINTS[math.random(#TINTS)],
    }
    if n > 0 and math.random() < Tears.tile_share then
        t.tile = math.random(n)
        t.s = rnd(Tears.tile_scale)
    end
    return t
end

local function reslice(self)
    local n = irnd(BANDS)
    local cuts = {}
    for i = 1, n - 1 do cuts[i] = math.random() end
    table.sort(cuts)
    self.bands = {}
    local y0 = 0
    for i = 1, n do
        local y1 = cuts[i] or 1
        local dx = 0
        if math.random() < BAND_SHIFT then dx = rnd(BAND_DX) * self.size * (math.random() < 0.5 and -1 or 1) end
        self.bands[i] = { y0 = y0, y1 = y1, dx = dx }
        y0 = y1
    end
    self.split = rnd(SPLIT_DX) * self.size
    self.tears = {}
    for i = 1, irnd(TEARS) do self.tears[i] = newTear(self) end
    for _ = 1, irnd(TEAR_TILES) do self.tears[#self.tears + 1] = newTear(self) end
end

local function startBurst(self)
    self.burst = rnd(BURST_LEN)
    self.slice = RESLICE_SEC
    reslice(self)
    if self.voiced and self.sfx ~= nil and math.random() < SOUND_ODDS then self.sfx:Play() end
end

local function stepAmbient(self, dt)
    local n = #self.rects
    if n == 0 then return end
    for _, g in ipairs(self.ambient) do
        g.t = g.t - dt
        if g.state == "gap" then
            g.a = 0
            if g.t <= 0 then
                g.state, g.t = "on", rnd(AMBIENT_HOLD) + 2 * AMBIENT_FADE
                g.hold = g.t
                g.tile = math.random(n)
                g.x = AMBIENT_AREA[1] + math.random() * AMBIENT_AREA[3]
                g.y = AMBIENT_AREA[2] + math.random() * AMBIENT_AREA[4]
                g.s = rnd(AMBIENT_SCALE)
            end
        else
            local age = g.hold - g.t
            g.a = math.min(1, age / AMBIENT_FADE, math.max(0, g.t) / AMBIENT_FADE) * AMBIENT_A
            if math.random() < JUMP_ODDS * dt then
                g.x = g.x + rnd({ -JUMP_X, JUMP_X })
                g.y = g.y + rnd({ -JUMP_Y, JUMP_Y })
            end
            if g.t <= 0 then g.state, g.t, g.a = "gap", rnd(AMBIENT_GAP), 0 end
        end
    end
end

function Glitch:update(dt)
    stepAmbient(self, dt)
    if self.forced > 0 then
        self.forced = math.max(0, self.forced - dt)
        self.slice = self.slice - dt
        if self.slice <= 0 then self.slice = RESLICE_SEC ; reslice(self) end
        return
    end
    if self.burst > 0 then
        self.burst = math.max(0, self.burst - dt)
        self.slice = self.slice - dt
        if self.slice <= 0 then self.slice = RESLICE_SEC ; reslice(self) end
        return
    end
    if self.rate <= 0 then return end
    self.wait = self.wait - dt * self.rate
    if self.wait <= 0 then
        self.wait = rnd(BURST_EVERY)
        startBurst(self)
    end
end

local function drawTile(self, tx, i, x, y, s)
    local r = self.rects[i]
    if r == nil then return end
    tx:SetScale(s, s)
    tx:DrawRectAtAnchor(x, y, r.x, r.y, r.w, r.h, "center")
end

-- (cx, cy) = the figure's eye on screen, k = screen scale
function Glitch:drawBack(cx, cy, k, alpha)
    local tx = self.tex.atlas
    if tx == nil or not tx.Ready or (alpha or 1) <= 0 then return end
    for _, g in ipairs(self.ambient) do
        local a = g.a * (alpha or 1)
        if a > 0.002 and g.tile ~= nil then
            tx:SetOpacity(math.min(1, a))
            drawTile(self, tx, g.tile, cx + g.x * k, cy + g.y * k, g.s * k)
        end
    end
    reset(tx)
end

-- tx with its top-left at screen (x, y), screen scale s
function Glitch:drawFigure(tx, x, y, s, a)
    if tx == nil or not tx.Ready or a <= 0.002 then return end
    a = math.min(1, a)
    tx:SetScale(s, s)
    if not self:active() then
        tx:SetOpacity(a)
        tx:DrawAtAnchor(x, y, "topleft")
        reset(tx)
        return
    end
    local w, h = tx.Width, tx.Height
    tx:SetBlendMode("Add")
    tx:SetOpacity(a * SPLIT_A)
    tx:SetColor(SPLIT_RED[1], SPLIT_RED[2], SPLIT_RED[3])
    tx:DrawAtAnchor(x - self.split * s, y, "topleft")
    tx:SetColor(SPLIT_CYAN[1], SPLIT_CYAN[2], SPLIT_CYAN[3])
    tx:DrawAtAnchor(x + self.split * s, y, "topleft")
    tx:SetColor(1, 1, 1)
    tx:SetBlendMode("Normal")
    tx:SetOpacity(a)
    for _, b in ipairs(self.bands) do
        local y0, y1 = math.floor(b.y0 * h), math.floor(b.y1 * h)
        if y1 > y0 then tx:DrawRectAtAnchor(x + b.dx * s, y + y0 * s, 0, y0, w, y1 - y0, "topleft") end
    end
    reset(tx)
end

-- ox, oy = shake offset
function Glitch:drawOver(ox, oy, alpha)
    if not self:active() or (alpha or 1) <= 0 then return end
    local res = THEME:GetResolution()
    local k = res.X / REF_W
    local white, atlas = self.tex.white, self.tex.atlas
    for _, t in ipairs(self.tears) do
        local a = math.min(1, t.a * (alpha or 1))
        if t.tile ~= nil then
            if atlas ~= nil and atlas.Ready then
                atlas:SetBlendMode("Add")
                atlas:SetColor(t.c[1], t.c[2], t.c[3])
                atlas:SetOpacity(math.min(1, a * Tears.tile_opacity_scale))
                drawTile(self, atlas, t.tile, t.x * k + ox, t.y * k + oy, t.s * k)
                reset(atlas)
            end
        elseif white ~= nil and white.Ready then
            white:SetBlendMode("Add")
            white:SetColor(t.c[1], t.c[2], t.c[3])
            white:SetOpacity(a)
            white:SetScale(t.w * k / math.max(1, white.Width), t.h * k / math.max(1, white.Height))
            white:DrawAtAnchor(t.x * k + ox, t.y * k + oy, "topleft")
            reset(white)
        end
    end
end

return Glitch
