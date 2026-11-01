-- Ambience of the forest path (the dojo and levels 1-5).

local F     = require("towerfx/field")
local art   = require("towerfx/art")
local Light = require("fx/light")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local look = Layout.towerfx.forest
local motion = Animation.towerfx.forest

local SLOT_H = look.slot_h_px

local shaftLook  = look.shafts
local SHAFTS     = shaftLook.count                  -- per slot
local SHAFT_ROT  = shaftLook.rot_deg                -- shared by every shaft
local SHAFT_X0, SHAFT_X1 = shaftLook.x_left_px, shaftLook.x_right_inset_px
local SHAFT_UP   = shaftLook.above_px
local SHAFT_SX, SHAFT_SY = shaftLook.scale_x, shaftLook.scale_y
local SHAFT_COL  = shaftLook.color
local shaftMove  = motion.shafts
local SHAFT_SWAY = shaftMove.sway_deg
local SHAFT_A    = shaftMove.opacity                -- base opacity
local SHAFT_PERIOD = shaftMove.period_s
local SHAFT_SWAY_PERIOD = shaftMove.sway_period_ratio

local FLIES     = look.flies.count
local FLY_SIZE  = look.flies.size
local FLY_COL   = look.flies.color
local FLY_A     = look.flies.opacity
local FLY_MARGIN = look.flies.y_margin_px
local flyMove   = motion.flies
local FLY_LIFE  = flyMove.life_s
local WA, WA_W = flyMove.wander_a.px, flyMove.wander_a.rad_per_s
local WB, WB_W, WB_PH = flyMove.wander_b.px, flyMove.wander_b.rad_per_s, flyMove.wander_b.phase_ratio
local WY, WY_W, WY_PH = flyMove.wander_y.px, flyMove.wander_y.rad_per_s, flyMove.wander_y.phase_ratio
local FLY_RISE  = flyMove.rise_px
local BLINK_BASE, BLINK_AMP, BLINK_W = flyMove.blink.base, flyMove.blink.amp, flyMove.blink.rad_per_s
local FLY_FIN, FLY_FOUT = flyMove.fade_in_share, flyMove.fade_out_share

local LEAVES     = look.leaves.count
local LEAF_MARGIN = look.leaves.x_margin_px
local LEAF_Y     = look.leaves.y_px
local LEAF_SIZE  = look.leaves.size
local LEAF_A     = look.leaves.opacity
local leafMove   = motion.leaves
local LEAF_LIFE  = leafMove.life_s
local LEAF_VX    = leafMove.vx_px_per_s
local LEAF_VY    = leafMove.vy_px_per_s
local LEAF_SWAY, LEAF_SWAY_HZ = leafMove.sway.px, leafMove.sway.hz
local LEAF_SPIN  = leafMove.spin_deg_per_s
local FLIP_BASE, FLIP_AMP, FLIP_HZ = leafMove.flip.base, leafMove.flip.amp, leafMove.flip.hz
local LEAF_FIN, LEAF_FOUT = leafMove.fade_in_share, leafMove.fade_out_share

local function shafts(L, t, top, W)
    local tx = art.get("shaft")
    if tx == nil then return end
    for i = 1, SHAFTS do
        local id = L * 11 + i
        local x = F.lerp(SHAFT_X0, W - SHAFT_X1, F.rand(id, 1, 1))
        local period = F.lerp(SHAFT_PERIOD[1], SHAFT_PERIOD[2], F.rand(id, 1, 2))
        local ph = F.rand(id, 1, 3) * F.TAU
        local k = 0.5 + 0.5 * math.sin(F.TAU * t / period + ph)
        local rot = SHAFT_ROT + SHAFT_SWAY * math.sin(F.TAU * t / (period * SHAFT_SWAY_PERIOD) + ph)
        Light.shaft(tx, x, top - SHAFT_UP, rot, F.lerp(SHAFT_SX[1], SHAFT_SX[2], F.rand(id, 1, 4)),
                    F.lerp(SHAFT_SY[1], SHAFT_SY[2], F.rand(id, 1, 5)),
                    SHAFT_COL[1], SHAFT_COL[2], SHAFT_COL[3], F.lerp(SHAFT_A[1], SHAFT_A[2], k))
    end
end

local function fireflies(L, t, top, W)
    local tx = art.get("mote")
    if tx == nil then return end
    tx:SetBlendMode("Add")
    for i = 1, FLIES do
        local id = L * 31 + i
        local k, u = F.cycle(i, L * 7 + 1, t, FLY_LIFE[1], FLY_LIFE[2])
        local ph = F.rand(id, k, 3) * F.TAU
        local x = F.rand(id, k, 1) * W + WA * math.sin(t * WA_W + ph) + WB * math.sin(t * WB_W + ph * WB_PH)
        local y = top + F.lerp(FLY_MARGIN, SLOT_H - FLY_MARGIN, F.rand(id, k, 2)) + WY * math.sin(t * WY_W + ph * WY_PH) - FLY_RISE * u
        local blink = BLINK_BASE + BLINK_AMP * math.sin(t * F.lerp(BLINK_W[1], BLINK_W[2], F.rand(id, k, 4)) + ph)
        local s = F.lerp(FLY_SIZE[1], FLY_SIZE[2], F.rand(id, k, 5))
        F.sprite(tx, x, y, s, s, 0, FLY_COL[1], FLY_COL[2], FLY_COL[3], F.fade(u, FLY_FIN, FLY_FOUT) * blink * FLY_A)
    end
    F.reset(tx)
end

local function leaves(L, t, top, W)
    local a, b = art.get("leaf_a"), art.get("leaf_b")
    if a == nil or b == nil then return end
    for i = 1, LEAVES do
        local id = L * 53 + i
        local k, u, life = F.cycle(i, L * 7 + 2, t, LEAF_LIFE[1], LEAF_LIFE[2])
        local age = u * life
        local ph = F.rand(id, k, 6) * F.TAU
        local x = F.lerp(-LEAF_MARGIN, W + LEAF_MARGIN, F.rand(id, k, 2)) + F.lerp(LEAF_VX[1], LEAF_VX[2], F.rand(id, k, 4)) * age
                  + LEAF_SWAY * math.sin(F.TAU * LEAF_SWAY_HZ * age + ph)
        local y = top + F.lerp(LEAF_Y[1], LEAF_Y[2], F.rand(id, k, 3)) + F.lerp(LEAF_VY[1], LEAF_VY[2], F.rand(id, k, 5)) * age
        local spin = F.lerp(LEAF_SPIN[1], LEAF_SPIN[2], F.rand(id, k, 9)) * ((F.rand(id, k, 8) < 0.5) and -1 or 1)
        local s = F.lerp(LEAF_SIZE[1], LEAF_SIZE[2], F.rand(id, k, 10))
        local flip = FLIP_BASE + FLIP_AMP * math.abs(math.cos(F.TAU * FLIP_HZ * age + ph))
        F.sprite((F.rand(id, k, 1) < 0.5) and a or b, x, y, s * flip, s, F.rand(id, k, 7) * 360 + spin * age,
                 1, 1, 1, F.fade(u, LEAF_FIN, LEAF_FOUT) * LEAF_A)
    end
    F.reset(a) ; F.reset(b)
end

function M.front(L, ctx)
    local t, top, W = ctx.t, ctx.top(L), ctx.resX
    shafts(L, t, top, W)
    leaves(L, t, top, W)
    fireflies(L, t, top, W)
end

return M
