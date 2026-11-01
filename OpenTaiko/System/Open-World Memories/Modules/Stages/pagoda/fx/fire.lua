-- One fire at a base point.
-- tex = {core, puff, glow, ember[, coreMeta]}; coreMeta = the core's manifest entry (w, h, fx, fy), else
-- the base is fx.fire.base_above_bottom_px above the core's bottom centre.
-- xf maps a 1920-space point to the screen (a function, or a table with .xf).

local Emitter = require("fx/emitter")
local presets = require("fx/presets")
local Light   = require("fx/light")
local sprite  = require("encounter/sprite")
local Config  = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local Fire = {}
Fire.__index = Fire

local look = Layout.fx.fire
local core = Animation.fx.fire.core

local CORE_PER_H  = look.core_h_px / look.marker_h_px   -- core height per marker height
local MARKER_H    = look.marker_h_px
local BASE_UP     = look.base_above_bottom_px
local function glow_of(g)
    return { scale = g.scale, r = g.color[1], g = g.color[2], b = g.color[3], a = g.opacity }
end
local HALO        = glow_of(look.halo)
local ROOM        = glow_of(look.room)
local GLOW_Y      = look.glow_y_share    -- glow centre, as a fraction of the core height above the base
local ADD_COPY    = look.add_copy_opacity
local PUFFS, TONGUES, EMBERS = look.puffs, look.tongues, look.embers
local SQX_A, SQX_W = core.squash_x.amp, core.squash_x.rad_per_s
local SQY_A, SQY_W, SQY_PH = core.squash_y.amp, core.squash_y.rad_per_s, core.squash_y.phase
local SQY2_A, SQY2_W = core.squash_y2.amp, core.squash_y2.rad_per_s
local LEAN, LEAN_W = core.lean.deg, core.lean.rad_per_s

local _o = { sx = 1, sy = 1, rot = 0, pivot = "foot" }

local function place(xf, x, y)
    if xf == nil then return x, y, 1 end
    local f = (type(xf) == "table") and xf.xf or xf
    local px, py = f(x, y)
    local qx = f(x + 100, y)
    return px, py, (qx - px) / 100
end

function Fire.new(tex, x, y, h, seed)
    local f = setmetatable({
        core = tex.core, glow = tex.glow, meta = tex.coreMeta,
        x = x, y = y, coreH = (h or MARKER_H) * CORE_PER_H,
        seed = seed or 0, t = 0,
    }, Fire)
    local ch = f.coreH
    f.puffs   = Emitter.new(presets.fire_puffs, tex.puff)
    f.tongues = Emitter.new(presets.fire_tongues, tex.puff)
    f.embers  = Emitter.new(presets.embers, tex.ember)
    f.puffs:setPoint(x, y - PUFFS.y_share * ch, PUFFS.spread_px[1], PUFFS.spread_px[2])
    f.tongues:setPoint(x, y - TONGUES.y_share * ch, TONGUES.spread_px[1], TONGUES.spread_px[2])
    f.embers:setPoint(x, y - EMBERS.y_share * ch, EMBERS.spread_px[1], EMBERS.spread_px[2])
    return f
end

function Fire:update(dt)
    self.t = self.t + dt
    self.puffs:update(dt)
    self.tongues:update(dt)
    self.embers:update(dt)
end

function Fire:flicker()
    return Light.flicker(self.t, self.seed)
end

function Fire:drawLight(xf)
    local fl = self:flicker()
    local px, py, s = place(xf, self.x, self.y - GLOW_Y * self.coreH)
    Light.glow(self.glow, px, py, ROOM.scale * s, ROOM.r, ROOM.g, ROOM.b, ROOM.a * fl)
    Light.glow(self.glow, px, py, HALO.scale * s, HALO.r, HALO.g, HALO.b, HALO.a * fl)
end

local function draw_core(self, px, py, s)
    local tx = self.core
    if tx == nil or not tx.Ready then return end
    local meta = self.meta
    if meta == nil then
        local w, h = tx.Width, tx.Height
        meta = { w = w, h = h, fx = w * 0.5, fy = h - BASE_UP }
        self.meta = meta
    end
    local t, ph = self.t, self.seed
    local k = s * self.coreH / meta.fy
    _o.sx  = k * (1 + SQX_A * math.sin(SQX_W * t + ph))
    _o.sy  = k * (1 + SQY_A * math.sin(SQY_W * t + SQY_PH + ph) + SQY2_A * math.sin(SQY2_W * t + ph))
    _o.rot = LEAN * math.sin(LEAN_W * t + ph)
    local cx, cy = sprite.center(meta, px, py, _o)
    tx:SetScale(_o.sx, _o.sy)
    tx:SetRotation(_o.rot)
    tx:SetBlendMode("Normal")
    tx:DrawAtAnchor(cx, cy, "center")
    tx:SetBlendMode("Add")
    tx:SetOpacity(ADD_COPY)
    tx:DrawAtAnchor(cx, cy, "center")
    tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
    tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

function Fire:drawBody(xf)
    local px, py, s = place(xf, self.x, self.y)
    local ox, oy = px - self.x, py - self.y
    self.puffs:draw(ox, oy)
    self.tongues:draw(ox, oy)
    draw_core(self, px, py, s)
    self.embers:draw(ox, oy)
end

function Fire:clear()
    self.puffs:clear()
    self.tongues:clear()
    self.embers:clear()
end

return Fire
