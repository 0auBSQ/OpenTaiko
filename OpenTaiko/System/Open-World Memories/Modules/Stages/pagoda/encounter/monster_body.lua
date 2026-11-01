-- One monster on the encounter screen.

local assets  = require("encounter/assets")
local sprite  = require("encounter/sprite")
local motion  = require("encounter/motion")
local moves   = require("encounter/monster_moves")
local Emitter = require("fx/emitter")
local presets = require("fx/presets")
local dissolve = require("fx/dissolve")
local document = require("encounter/document")
local Config  = require("Core.Config")
local Layout  = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.monster_body
local Glow = Animation.encounter.monster_body.glow
local Shadow = Look.shadow
local AURA_W, AURA_H, AURA_FOOT = Look.aura.w, Look.aura.h, Look.aura.foot    -- spawn box above the feet
local DARK_SPREAD = Look.aura.dark_spread         -- spawn box margin per side, as a share of the sprite size
local GLOW_RATE, GLOW_BASE, GLOW_PULSE = Glow.rad_per_s, Glow.base, Glow.pulse
local GLINT_SCALE = Look.glint_scale
local GLINT_R, GLINT_G, GLINT_B = Look.glint_color[1], Look.glint_color[2], Look.glint_color[3]
local SHADOW_W = Shadow.width_share               -- share of the sprite width
local SHADOW_MIN, SHADOW_MAX = Shadow.min_scale, Shadow.max_scale
local SHADOW_ALPHA, LIFT_FULL = Shadow.opacity, Shadow.lift_full_px
local LIFT_SHRINK, LIFT_FADE = Shadow.lift_shrink, Shadow.lift_fade
local FLOAT_SHADOW_SCALE, FLOAT_SHADOW_ALPHA = Shadow.float_scale, Shadow.float_opacity

local Body = {}
Body.__index = Body

local _o = {}

local function mirrorKey(m)
    if m.kind ~= "wander" then return nil end
    local meta = assets.meta("m_" .. m.key)
    local key = (meta ~= nil and meta.mirror) or ("m_" .. m.key .. "_m")
    if assets.meta(key) == nil then return nil end
    return key
end

-- jump frame (meta.jump), on the standing frame's canvas
local function jumpKey(m)
    local meta = assets.meta("m_" .. m.key)
    if meta == nil or meta.jump == nil or assets.meta(meta.jump) == nil then return nil end
    return meta.jump
end

local function presetTex(name)
    return Emitter.texKeys(presets[name])[1] or "fx_mote"
end

local function floats(m)
    return m.kind == "float" or m.params.float == true
end

-- texture keys of one member (an entry of monsters.forLevel)
function M.keys(m)
    local keys = { "m_" .. m.key, "fx_shadow" }
    local mk = mirrorKey(m)
    if mk ~= nil then keys[#keys + 1] = mk end
    local jk = jumpKey(m)
    if jk ~= nil then keys[#keys + 1] = jk end
    if m.aura == "divine" then
        keys[#keys + 1] = presetTex("divine_dark")
        keys[#keys + 1] = presetTex("divine_shine")
    elseif m.aura == "dark" then
        keys[#keys + 1] = presetTex("door_dark")
    end
    if m.eyes then keys[#keys + 1] = "fx_glint" end
    for _, k in ipairs(document.keys(assets.meta("m_" .. m.key))) do keys[#keys + 1] = k end
    if m.dissolve then
        for _, k in ipairs(dissolve.keys()) do keys[#keys + 1] = k end
    end
    return keys
end

local function opts(sx, sy, rot, pivot, alpha, r, g, b, blend)
    _o.sx, _o.sy, _o.rot, _o.pivot = sx, sy, rot, pivot
    _o.alpha, _o.r, _o.g, _o.b, _o.blend = alpha, r, g, b, blend
    return _o
end

-- how far the sprite's middle moves when it leans rot degrees about its feet
function Body:footLean(rot, sx, sy)
    if self.meta == nil or rot == 0 then return 0, 0 end
    local x1, y1 = sprite.center(self.meta, 0, 0, opts(sx, sy, rot, "foot"))
    local x0, y0 = sprite.center(self.meta, 0, 0, opts(sx, sy, 0, "foot"))
    return x1 - x0, y1 - y0
end

-- idle + charge + vanish; a walker with no mirrored copy faces right at a negative x scale
function Body:compose()
    local p, c, s = self.pose, self.cp, self.scale
    c.x, c.dy, c.sx, c.sy, c.rot = self.x + p.dx * s, p.dy * s, p.sx, p.sy, p.rot
    c.pivot, c.mirrored, c.jump, c.lift = p.pivot, p.mirrored, p.jump, p.lift * s
    c.ox, c.oy = 0, 0
    c.alpha, c.shade, c.shadowS = 1, 1, 1
    local cs = self.chargeState
    if cs ~= nil then
        local m = moves.chargePose(cs)
        local w = m.w
        -- the idle's turn holds until the hit
        local rot, pivot = p.rot, p.pivot
        if not cs.hit then rot, pivot = self.rot0, self.pivot0 end
        c.x = m.x + p.dx * s * w
        c.dy = m.dy + (p.dy * s - m.dy) * w
        c.rot = m.rot + rot * w
        c.sx = m.sx * (1 + (p.sx - 1) * w)
        c.sy = m.sy * (1 + (p.sy - 1) * w)
        if w < 1 then
            c.pivot = m.pivot
            -- keep a centre pivot while blending; the feet lean becomes a move of the middle
            if w > 0 and pivot == "center" and m.pivot == "foot" then
                c.pivot = "center"
                c.ox, c.oy = self:footLean(m.rot, c.sx, c.sy)
            end
        end
        c.mirrored = false
        c.jump = cs.style == "hop" and m.w == 0 and m.dy < 0
        c.lift = math.max(0, -c.dy)
    end
    local vs = self.vanishState
    if vs ~= nil then
        if vs.fx ~= nil then
            c.shade = vs.fx:remaining()
        else
            local v = self.v
            c.dy = c.dy + v.dy
            c.sx, c.sy = c.sx * v.sx, c.sy * v.sy
            c.alpha, c.shade = v.alpha, v.alpha
            c.shadowS = math.min(1, v.sx)
        end
    end
    if s ~= 1 then c.sx, c.sy = c.sx * s, c.sy * s end
    if c.mirrored and self.mkey == nil then c.sx = -c.sx end
end

function Body:placeAura()
    if self.shine == nil then
        local meta, c = self.meta, self.cp
        if meta == nil then return end
        local cx, cy = self:centre()
        local w = meta.w * math.abs(c.sx) * (1 + 2 * DARK_SPREAD)
        local h = meta.h * math.abs(c.sy) * (1 + 2 * DARK_SPREAD)
        self.dark:setArea(cx - w * 0.5, cy - h * 0.5, w, h)
        return
    end
    local s = self.scale
    local x = self.cp.x - AURA_W * s * 0.5
    local y = self.footY + self.cp.dy - (AURA_FOOT + AURA_H) * s
    self.dark:setArea(x, y, AURA_W * s, AURA_H * s)
    self.shine:setArea(x, y, AURA_W * s, AURA_H * s)
end

-- m = an entry of monsters.forLevel; x, footY = the feet; seed = idle randomness;
-- scale = size against the encounter's (1 when absent)
function M.new(m, x, footY, seed, scale)
    local self = setmetatable({
        m = m, x = x, footY = footY, t = 0, gone = false, scale = scale or 1,
        cp = { x = 0, dy = 0, ox = 0, oy = 0, sx = 1, sy = 1, rot = 0, pivot = "foot", mirrored = false,
               jump = false, lift = 0, alpha = 1, shade = 1, shadowS = 1 },
        v = { sx = 1, sy = 1, dy = 0, alpha = 1 },
        rot0 = 0, pivot0 = "foot",                -- the idle's turn when the charge started
    }, Body)
    self.key = "m_" .. m.key
    self.tex, self.meta = assets.get(self.key)
    self.mkey = mirrorKey(m)
    if self.mkey ~= nil then self.texM, self.metaM = assets.get(self.mkey) end
    local jk = jumpKey(m)
    if jk ~= nil then self.texJ = assets.get(jk) end
    self.shadow, self.shadowMeta = assets.get("fx_shadow")
    self.motion = motion.new(m.kind, m.params, seed)
    self.pose = motion.step(self.motion, 0)
    self:compose()
    if m.aura == "divine" then
        self.dark = Emitter.new(presets.divine_dark, (assets.get(presetTex("divine_dark"))))
        self.shine = Emitter.new(presets.divine_shine, (assets.get(presetTex("divine_shine"))))
        self:placeAura()
    elseif m.aura == "dark" then
        local tx = assets.get(presetTex("door_dark"))     -- nil where the screen did not open it
        if tx ~= nil then
            self.dark = Emitter.new(presets.door_dark, tx)
            self:placeAura()
        end
    end
    if m.eyes then
        self.glint = assets.get("fx_glint")
        self.eyePts = assets.data("eyes15")
    end
    -- parts drawn over the sprite (encounter/document.lua)
    if self.meta ~= nil and self.meta.parts ~= nil then self.doc = document.new(self.meta, seed) end
    return self
end

function Body:update(dt)
    if self.gone then return end
    self.t = self.t + dt
    local vs = self.vanishState
    if vs == nil then self.pose = motion.step(self.motion, dt * self.m.tempo) end
    if self.doc ~= nil then self.doc:update(dt) end
    local cs = self.chargeState
    if cs ~= nil then
        cs.t = cs.t + dt
        if not cs.hit and cs.t >= cs.sec then cs.hit = true end
    end
    if vs ~= nil then
        vs.t = vs.t + dt
        if vs.fx ~= nil then vs.fx:update(dt) else moves.squishPose(vs.t, self.v) end
    end
    self:compose()
    if self.dark ~= nil then
        local ax = self.cp.x + self.cp.ox
        local dx = ax - (self.auraX or ax)
        self.auraX = ax
        self.dark:carry(dx, 0)
        if self.shine ~= nil then self.shine:carry(dx, 0) end
        self:placeAura()
        self.dark:update(dt)
        if self.shine ~= nil then self.shine:update(dt) end
    end
    if vs ~= nil then
        local over
        if vs.fx ~= nil then over = vs.fx:done() else over = vs.t >= moves.SQUISH_SEC end
        if over then
            self.gone = true
            if self.dark ~= nil then self.dark:clear() end
            if self.shine ~= nil then self.shine:clear() end
        end
    end
end

function Body:notice()
    motion.notice(self.motion)
end

-- charges left until its left edge reaches edgeX plus its offset from the lead, in sec seconds;
-- ignored once charging or vanishing
function Body:charge(edgeX, sec)
    if self.chargeState ~= nil or self.vanishState ~= nil then return end
    local m, meta = self.m, self.meta
    local fx = (meta ~= nil and (meta.fx or meta.w * 0.5)) or 0
    local style = "dash"
    if floats(m) then style = "swoop"
    elseif m.kind == "wander" or m.kind == "star" then style = "hop" end
    self.chargeState = moves.newCharge(self.x, edgeX + m.dx, fx, sec, style, floats(m) and self.pose.dy or 0)
    self.rot0, self.pivot0 = (self.pose.rot + 180) % 360 - 180, self.pose.pivot
    motion.calm(self.motion)
end

-- true from the frame its charge arrives
function Body:arrived()
    return self.chargeState ~= nil and self.chargeState.hit
end

-- style = "squish" | "dissolve"; the idle stops where it is
function Body:vanish(style)
    if self.vanishState ~= nil then return end
    local vs = { t = 0 }
    if style == "dissolve" then
        vs.fx = dissolve.new((self.cp.mirrored and self.texM ~= nil) and self.mkey or self.key)
    else
        moves.squishPose(0, self.v)
    end
    self.vanishState = vs
    if self.dark ~= nil then self.dark:setRate(0) end
    if self.shine ~= nil then self.shine:setRate(0) end
end

function Body:vanishing()
    return self.vanishState ~= nil
end

-- ox, oy: the screen shake
function Body:drawShadow(alpha, ox, oy)
    if self.gone or self.shadow == nil or self.shadowMeta == nil or self.meta == nil then return end
    local c = self.cp
    alpha = alpha * c.shade
    if alpha <= 0 then return end
    local base = math.max(SHADOW_MIN, math.min(SHADOW_MAX, SHADOW_W * self.meta.w / math.max(1, self.shadowMeta.w)))
    local s, a
    if floats(self.m) then
        s, a = FLOAT_SHADOW_SCALE, FLOAT_SHADOW_ALPHA
    else
        local l = math.min(1, math.max(0, c.lift) / (LIFT_FULL * self.scale))
        s, a = 1 - LIFT_SHRINK * l, SHADOW_ALPHA * (1 - LIFT_FADE * l)
    end
    s = s * c.shadowS * self.scale
    sprite.drawAt(self.shadow, c.x + ox, self.footY + oy, opts(base * s, base * s, 0, nil, a * alpha, 1, 1, 1, "Normal"))
end

-- alpha: monster and aura opacity; r, g, b: the band's filter; eyes: 0..1 glint; ox, oy: the screen shake
function Body:draw(alpha, r, g, b, eyes, ox, oy)
    if self.gone then return end
    local p = self.cp
    local tx, meta = self.tex, self.meta
    if p.mirrored and self.texM ~= nil then tx, meta = self.texM, self.metaM end
    if p.jump and self.texJ ~= nil then tx = self.texJ end
    if meta == nil then return end
    local x, y = p.x + p.ox + ox, self.footY + p.dy + p.oy + oy
    local fx = self.vanishState ~= nil and self.vanishState.fx or nil
    local keep = 1
    if fx ~= nil then keep = 1 - fx:darkness() end
    local va = alpha * p.alpha
    local rot = p.rot
    if self.doc ~= nil and self.chargeState == nil and self.vanishState == nil then rot = rot + self.doc:spin() end
    if va > 0 then
        if self.dark ~= nil then self.dark:draw(ox, oy, va, r, g, b) end
        local o = opts(p.sx, p.sy, rot, p.pivot, va, r, g, b, "Normal")
        if fx ~= nil then fx:draw(tx, meta, x, y, o, va, r, g, b)
        else sprite.draw(tx, meta, x, y, o) end
        local glow = (self.m.glow or 0) * keep
        if glow > 0 then
            local a = glow * (GLOW_BASE + GLOW_PULSE * math.sin(GLOW_RATE * self.t)) * va
            sprite.draw(tx, meta, x, y, opts(p.sx, p.sy, rot, p.pivot, a, r, g, b, "Add"))
        end
        if self.doc ~= nil and fx == nil then
            self.doc:draw(meta, x, y, opts(p.sx, p.sy, rot, p.pivot), va, r, g, b)
        end
        if self.shine ~= nil then self.shine:draw(ox, oy, va, r, g, b) end
    end
    eyes = eyes * p.alpha * keep
    if eyes > 0 and self.eyePts ~= nil and self.glint ~= nil then
        local gs = GLINT_SCALE * self.scale
        opts(p.sx, p.sy, p.rot, p.pivot, 1, 1, 1, 1, "Normal")
        for _, e in ipairs(self.eyePts) do
            local ex, ey = sprite.point(meta, x, y, _o, e.x or e[1], e.y or e[2])
            sprite.drawAt(self.glint, ex, ey, opts(gs, gs, 0, nil, eyes, GLINT_R, GLINT_G, GLINT_B, "Add"))
            opts(p.sx, p.sy, p.rot, p.pivot, 1, 1, 1, 1, "Normal")
        end
    end
end

-- screen point at the sprite's middle this frame (no screen shake)
function Body:centre()
    local p, meta = self.cp, self.meta
    if p.mirrored and self.metaM ~= nil then meta = self.metaM end
    if meta == nil then return p.x, self.footY end
    return sprite.center(meta, p.x + p.ox, self.footY + p.dy + p.oy, opts(p.sx, p.sy, p.rot, p.pivot))
end

function Body:close()
    if self.dark ~= nil then self.dark:clear() end
    if self.shine ~= nil then self.shine:clear() end
end

return M
