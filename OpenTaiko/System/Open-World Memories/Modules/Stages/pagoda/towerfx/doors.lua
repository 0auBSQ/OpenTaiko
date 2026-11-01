-- Ambience of a door floor's room (normal.png, levels 11-19), in the floor's tint.

local F     = require("towerfx/field")
local art   = require("towerfx/art")
local Light = require("fx/light")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local look = Layout.towerfx.doors
local motion = Animation.towerfx.doors

local NORMAL_W = look.tile_w_px
local DOOR       = look.door                    -- door area in normal.png
local ROOM       = look.room                    -- x, y, w, h of the room's open floor space
local WINDOWS    = look.windows                 -- x, y, slant of the light from the high windows

local doorGlow   = look.door_glow
local GLOW_S     = doorGlow.scale
local INNER_SX, INNER_SY, INNER_DY = doorGlow.inner_scale_x, doorGlow.inner_scale_y, doorGlow.inner_dy_px
local SHAFT_SX, SHAFT_SY, SHAFT_WHITE = look.shaft.scale_x, look.shaft.scale_y, look.shaft.whiten
local DOOR_A     = motion.door_glow.opacity
local DOOR_W, DOOR_PH = motion.door_glow.rad_per_s, motion.door_glow.level_phase
local SHAFT_A    = motion.shaft.opacity
local SHAFT_PERIOD, SHAFT_PH = motion.shaft.period_s, motion.shaft.window_phase
local DUST       = look.dust.count
local DUST_LIFE  = motion.dust.life_s
local DUST_SIZE  = look.dust.size
local DUST_WHITE, DUST_A = look.dust.whiten, look.dust.opacity
local dustMove   = motion.dust
local SWAY_A, SWAY_A_W = dustMove.sway_a.px, dustMove.sway_a.rad_per_s
local SWAY_B, SWAY_B_W = dustMove.sway_b.px, dustMove.sway_b.rad_per_s
local DUST_RISE  = dustMove.rise_px_per_s
local GLINT_BASE, GLINT_AMP, GLINT_W = dustMove.glint.base, dustMove.glint.amp, dustMove.glint.rad_per_s
local DUST_FIN, DUST_FOUT = dustMove.fade_in_share, dustMove.fade_out_share

-- c moved toward white by k
local function toward_white(c, k) return c + (1 - c) * k end

local function lights(L, t, xl, top, lit, r, g, b)
    local glow = art.get("glow")
    if glow ~= nil then
        local k = 0.5 + 0.5 * math.sin(t * DOOR_W + L * DOOR_PH)
        glow:SetBlendMode("Add")
        F.sprite(glow, xl + DOOR[1], top + DOOR[2], GLOW_S, GLOW_S, 0, r, g, b, F.lerp(DOOR_A[1], DOOR_A[2], k) * lit)
        F.sprite(glow, xl + DOOR[1], top + DOOR[2] + INNER_DY, INNER_SX, INNER_SY, 0, r, g, b, F.lerp(DOOR_A[1], DOOR_A[2], k) * lit)
        F.reset(glow)
    end
    local shaft = art.get("shaft")
    if shaft ~= nil then
        for i, w in ipairs(WINDOWS) do
            local k = 0.5 + 0.5 * math.sin(F.TAU * t / SHAFT_PERIOD + i * SHAFT_PH + L)
            Light.shaft(shaft, xl + w[1], top + w[2], w[3], SHAFT_SX, SHAFT_SY,
                        toward_white(r, SHAFT_WHITE), toward_white(g, SHAFT_WHITE), toward_white(b, SHAFT_WHITE),
                        F.lerp(SHAFT_A[1], SHAFT_A[2], k) * lit)
        end
    end
end

local function dust(L, t, xl, top, lit, r, g, b)
    local tx = art.get("mote")
    if tx == nil then return end
    tx:SetBlendMode("Add")
    local cr, cg, cb = toward_white(r, DUST_WHITE), toward_white(g, DUST_WHITE), toward_white(b, DUST_WHITE)
    for i = 1, DUST do
        local id = L * 37 + i
        local k, u, life = F.cycle(i, L * 7 + 4, t, DUST_LIFE[1], DUST_LIFE[2])
        local age = u * life
        local ph = F.rand(id, k, 4) * F.TAU
        local x = xl + ROOM[1] + F.rand(id, k, 1) * ROOM[3] + SWAY_A * math.sin(age * SWAY_A_W + ph) + SWAY_B * math.sin(age * SWAY_B_W + ph)
        local y = top + ROOM[2] + F.rand(id, k, 2) * ROOM[4] - F.lerp(DUST_RISE[1], DUST_RISE[2], F.rand(id, k, 3)) * age
        local s = F.lerp(DUST_SIZE[1], DUST_SIZE[2], F.rand(id, k, 5))
        local glint = GLINT_BASE + GLINT_AMP * math.sin(age * F.lerp(GLINT_W[1], GLINT_W[2], F.rand(id, k, 6)) + ph)
        F.sprite(tx, x, y, s, s, 0, cr, cg, cb, F.fade(u, DUST_FIN, DUST_FOUT) * glint * DUST_A * lit)
    end
    F.reset(tx)
end

function M.front(L, ctx)
    local lit = ctx.lit(L)
    if lit <= 0.001 then return end
    local t, top = ctx.t, ctx.top(L)
    local xl = ctx.resX - NORMAL_W
    local r, g, b = ctx.tint(L)
    lights(L, t, xl, top, lit, r, g, b)
    dust(L, t, xl, top, lit, r, g, b)
end

return M
