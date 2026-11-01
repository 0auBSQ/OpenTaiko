-- Ambience of the cleared checkpoint gallery (checkpoint_cleared.png, level 15).

local F     = require("towerfx/field")
local art   = require("towerfx/art")
local Light = require("fx/light")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local look = Layout.towerfx.checkpoint
local motion = Animation.towerfx.checkpoint

local NORMAL_W = look.tile_w_px
local LANTERNS = look.lanterns                  -- x, y, size

local HALO_COL = look.halo.color
local HALO_S, HALO_A = look.halo.scale, look.halo.opacity
local CORE_COL = look.core.color
local CORE_SX, CORE_SY, CORE_A = look.core.scale_x, look.core.scale_y, look.core.opacity
local FLICKER_STEP = motion.flicker_seed_step
local sparkLook = look.sparks
local SPARKS     = sparkLook.count
local SPARK_DX   = sparkLook.jitter_x_px
local SPARK_DY   = sparkLook.start_dy_px
local SPARK_SIZE = sparkLook.size
local SPARK_A    = sparkLook.opacity
local sparkMove  = motion.sparks
local SPARK_LIFE = sparkMove.life_s
local SPARK_SWAY, SPARK_SWAY_W = sparkMove.sway.px, sparkMove.sway.rad_per_s
local SPARK_RISE = sparkMove.rise_px_per_s
local SPARK_FIN, SPARK_FOUT = sparkMove.fade_in_share, sparkMove.fade_out_share

local function lanterns(t, xl, top, lit)
    local glow = art.get("glow")
    if glow == nil then return end
    glow:SetBlendMode("Add")
    for i, ln in ipairs(LANTERNS) do
        local f = Light.flicker(t, i * FLICKER_STEP)
        local x, y, s = xl + ln[1], top + ln[2], ln[3]
        F.sprite(glow, x, y, HALO_S * s, HALO_S * s, 0, HALO_COL[1], HALO_COL[2], HALO_COL[3], HALO_A * f * lit)
        F.sprite(glow, x, y, CORE_SX * s, CORE_SY * s, 0, CORE_COL[1], CORE_COL[2], CORE_COL[3], CORE_A * f * lit)
    end
    F.reset(glow)
end

local function sparks(t, xl, top, lit)
    local tx = art.get("ember")
    if tx == nil then return end
    tx:SetBlendMode("Add")
    for i = 1, SPARKS do
        local id = 1500 + i
        local k, u, life = F.cycle(i, 151, t, SPARK_LIFE[1], SPARK_LIFE[2])
        local age = u * life
        local ln = LANTERNS[1 + math.floor(F.rand(id, k, 1) * #LANTERNS) % #LANTERNS]
        local ph = F.rand(id, k, 2) * F.TAU
        local x = xl + ln[1] + F.lerp(SPARK_DX[1], SPARK_DX[2], F.rand(id, k, 3)) + SPARK_SWAY * math.sin(age * SPARK_SWAY_W + ph)
        local y = top + ln[2] + SPARK_DY - F.lerp(SPARK_RISE[1], SPARK_RISE[2], F.rand(id, k, 4)) * age
        local s = F.lerp(SPARK_SIZE[1], SPARK_SIZE[2], F.rand(id, k, 5))
        F.sprite(tx, x, y, s, s, 0, HALO_COL[1], HALO_COL[2], HALO_COL[3], F.fade(u, SPARK_FIN, SPARK_FOUT) * SPARK_A * lit)
    end
    F.reset(tx)
end

function M.front(L, ctx)
    local lit = ctx.lit(L)
    if lit <= 0.001 then return end
    local t, top = ctx.t, ctx.top(L)
    local xl = ctx.resX - NORMAL_W
    lanterns(t, xl, top, lit)
    sparks(t, xl, top, lit)
end

return M
