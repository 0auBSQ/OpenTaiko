-- Level 20's Tobiraph on the band; the notes land on its eye.

local assets = require("encounter/assets")
local Glitch = require("encounter/cutscenes/glitch")
local Beat   = require("beat")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local T = {}
T.__index = T

local TAU = math.pi * 2
local View = Layout.play_bg.view
local Move = Animation.play_bg.tobiraph
local BOB_PX, BOB_SEC = Move.bob_px, Move.bob_period_s
local SWAY_PX, SWAY_SEC = Move.sway_px, Move.sway_period_s
local TILE_SCALE = Layout.play_bg.tobiraph.glitch_tile_scale
local FLASH_SHARE = Layout.play_bg.tobiraph.flash_share

function T.keys(ctx)
    return { "up_tobiraph", "cut20_glitch", "cut20_white" }
end

function T.new(ctx)
    local tx, meta = assets.get("up_tobiraph")
    local place = assets.data("tobiraph") or {}
    local glitch = Glitch.new({ atlas = (assets.get("cut20_glitch")), white = (assets.get("cut20_white")) },
                              assets.data("cut20_glitch") or {}, nil)
    return setmetatable({
        tx = tx, meta = meta, glitch = glitch, beat = Beat.new(), t = 0,
        x = place.x or View.foe_x, y = place.y or View.foe_y,
    }, T)
end

-- its eye on screen this frame
function T:centre()
    return self.x + math.sin(self.t * TAU / SWAY_SEC) * SWAY_PX,
           self.y + math.sin(self.t * TAU / BOB_SEC) * BOB_PX
end

-- the bob's accent is its lowest point, a quarter cycle in
function T:update(dt, state)
    local pos = (self.t - BOB_SEC * 0.25) % BOB_SEC
    self.t = self.t + self.beat:step(dt, BOB_SEC, pos, state.bpm[0], state.towerFloor)
    self.glitch:update(dt)
end

-- the figure's top-left on whole pixels, so the glitch's slices meet without gaps
local function topLeft(self)
    local ex, ey = self:centre()
    local m = self.meta
    return math.floor(ex - (m.fx or 0) + 0.5), math.floor(ey - (m.fy or 0) + 0.5), ex, ey
end

function T:draw(alpha, r, g, b, eyes, shadow)
    if self.meta == nil then return end
    local x, y, ex, ey = topLeft(self)
    self.glitch:drawBack(ex, ey, TILE_SCALE, alpha)
    self.glitch:drawFigure(self.tx, x, y, 1, alpha)
end

-- redraws the figure at flash share a; the caller sets the white gradient map
function T:drawFlash(a)
    local tx = self.tx
    if self.meta == nil or tx == nil or not tx.Ready then return end
    local x, y = topLeft(self)
    tx:SetOpacity(math.min(1, a * FLASH_SHARE))
    tx:DrawAtAnchor(x, y, "topleft")
    tx:SetOpacity(1)
end

function T:react(big)
end

return T
