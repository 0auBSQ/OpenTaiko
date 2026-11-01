-- Ambience of the pagoda's ground floor (groundfloor.png, level 10).

local F   = require("towerfx/field")
local art = require("towerfx/art")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local look = Layout.towerfx.base
local motion = Animation.towerfx.base

local TILE_W = look.tile_w_px
local FACADE = look.facade                      -- x, y, w, h of the facade in groundfloor.png
local VINES  = look.vines                       -- vine area in groundfloor.png

local DUST      = look.dust.count
local DUST_LIFE = motion.dust.life_s
local DUST_COL  = look.dust.color
local DUST_SIZE = look.dust.size
local DUST_A    = look.dust.opacity
local dustMove  = motion.dust
local DUST_DRIFT = dustMove.drift_px_per_s
local DUST_SX, DUST_SX_W = dustMove.sway_x.px, dustMove.sway_x.rad_per_s
local DUST_SY, DUST_SY_W = dustMove.sway_y.px, dustMove.sway_y.rad_per_s
local DUST_FIN, DUST_FOUT = dustMove.fade_in_share, dustMove.fade_out_share
local FLIES     = look.flies.count
local FLY_COL   = look.flies.color
local FLY_SIZE  = look.flies.size
local FLY_A     = look.flies.opacity
local flyMove   = motion.flies
local FLY_LIFE  = flyMove.life_s
local FLY_WX, FLY_WX_W = flyMove.wander_x.px, flyMove.wander_x.rad_per_s
local FLY_WY, FLY_WY_W, FLY_WY_PH = flyMove.wander_y.px, flyMove.wander_y.rad_per_s, flyMove.wander_y.phase_ratio
local BLINK_BASE, BLINK_AMP, BLINK_W = flyMove.blink.base, flyMove.blink.amp, flyMove.blink.rad_per_s
local FLY_FIN, FLY_FOUT = flyMove.fade_in_share, flyMove.fade_out_share

local function dust(t, xl, top)
    local tx = art.get("mote")
    if tx == nil then return end
    tx:SetBlendMode("Add")
    for i = 1, DUST do
        local id = 1000 + i
        local k, u, life = F.cycle(i, 101, t, DUST_LIFE[1], DUST_LIFE[2])
        local age = u * life
        local ph = F.rand(id, k, 3) * F.TAU
        local x = xl + FACADE[1] + F.rand(id, k, 1) * FACADE[3] + DUST_DRIFT * age + DUST_SX * math.sin(age * DUST_SX_W + ph)
        local y = top + FACADE[2] + F.rand(id, k, 2) * FACADE[4] + DUST_SY * math.sin(age * DUST_SY_W + ph)
        local s = F.lerp(DUST_SIZE[1], DUST_SIZE[2], F.rand(id, k, 4))
        F.sprite(tx, x, y, s, s, 0, DUST_COL[1], DUST_COL[2], DUST_COL[3], F.fade(u, DUST_FIN, DUST_FOUT) * DUST_A)
    end
    for i = 1, FLIES do
        local id = 1100 + i
        local k, u = F.cycle(i, 102, t, FLY_LIFE[1], FLY_LIFE[2])
        local ph = F.rand(id, k, 3) * F.TAU
        local x = xl + VINES[1] + F.rand(id, k, 1) * VINES[3] + FLY_WX * math.sin(t * FLY_WX_W + ph)
        local y = top + VINES[2] + F.rand(id, k, 2) * VINES[4] + FLY_WY * math.sin(t * FLY_WY_W + ph * FLY_WY_PH)
        local blink = BLINK_BASE + BLINK_AMP * math.sin(t * BLINK_W + ph)
        F.sprite(tx, x, y, FLY_SIZE, FLY_SIZE, 0, FLY_COL[1], FLY_COL[2], FLY_COL[3], F.fade(u, FLY_FIN, FLY_FOUT) * blink * FLY_A)
    end
    F.reset(tx)
end

function M.front(L, ctx)
    dust(ctx.t, ctx.resX - TILE_W, ctx.top(L))
end

return M
