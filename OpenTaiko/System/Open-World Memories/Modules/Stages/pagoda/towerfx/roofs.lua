-- Ambience of the roofs and the beanstalk (level 20 up); nothing shows while the roofs are shrouded.

local F   = require("towerfx/field")
local art = require("towerfx/art")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local look = Layout.towerfx.roofs
local motion = Animation.towerfx.roofs

local SLOT_H = look.slot_h_px

local NIGHT = motion.night_hours        -- hours over which the night effects take over

-- cloud layers: count per slot, hash salt, scale, drift (px/s) and opacity ranges
local BACK_CLOUDS  = { n = look.back_clouds.count, salt = 0, size = look.back_clouds.size,
                       speed = motion.back_clouds.speed_px_per_s, alpha = motion.back_clouds.opacity }
local FRONT_CLOUDS = { n = look.front_clouds.count, salt = 500, size = look.front_clouds.size,
                       speed = motion.front_clouds.speed_px_per_s, alpha = motion.front_clouds.opacity }
local clouds_look  = look.clouds
local CLOUD_EDGE   = clouds_look.edge_px        -- off-screen run-out (over half the widest cloud)
local CLOUD_MARGIN = clouds_look.y_margin_px
local CLOUD_SQUASH = clouds_look.squash_y
local CLOUD_NIGHT_DIM = clouds_look.night_dim   -- colour and opacity multipliers at full night
local CLOUD_NIGHT_A   = clouds_look.night_opacity
local CLOUD_BREATH = motion.cloud_breath_period_s

local WINDS     = look.winds.count
local WIND_MARGIN = look.winds.y_margin_px
local WIND_SX   = look.winds.scale_x
local WIND_A    = look.winds.opacity
local WIND_LIFE = motion.winds.life_s
local WIND_V    = motion.winds.speed_px_per_s
local WIND_BOB  = motion.winds.bob_px

local STARS     = look.stars.count
local STAR_SIZE = look.stars.size
local STAR_COL  = look.stars.color
local STAR_A    = look.stars.opacity
local STAR_LIFE = motion.stars.life_s
local STAR_TW   = motion.stars.twinkle_rad_per_s
local STAR_FIN, STAR_FOUT = motion.stars.fade_in_share, motion.stars.fade_out_share

local function clouds(L, ctx, layer)
    local tx = art.get("puff")
    if tx == nil then return end
    local t, top, W = ctx.t, ctx.top(L), ctx.resX
    local dim = F.lerp(1, CLOUD_NIGHT_DIM, ctx.night)
    local r, g, b = ctx.fr * dim, ctx.fg * dim, ctx.fb * dim
    local fade = F.lerp(1, CLOUD_NIGHT_A, ctx.night) * ctx.roofLit
    local size, alpha = layer.size, layer.alpha
    for i = 1, layer.n do
        local id = L * 19 + i + layer.salt
        local v = F.lerp(layer.speed[1], layer.speed[2], F.rand(id, 0, 1))
        local x = F.wrap(F.rand(id, 0, 2) * (W + 2 * CLOUD_EDGE) + v * t, W, CLOUD_EDGE)
        local y = top + F.lerp(CLOUD_MARGIN, SLOT_H - CLOUD_MARGIN, F.rand(id, 0, 3))
        local s = F.lerp(size[1], size[2], F.rand(id, 0, 4))
        local k = 0.5 + 0.5 * math.sin(F.TAU * t / F.lerp(CLOUD_BREATH[1], CLOUD_BREATH[2], F.rand(id, 0, 5)) + F.rand(id, 0, 6) * F.TAU)
        F.sprite(tx, x, y, s, s * CLOUD_SQUASH, 0, r, g, b, F.lerp(alpha[1], alpha[2], k) * fade)
    end
    F.reset(tx)
end

local function winds(L, ctx)
    local day = (1 - ctx.night) * ctx.roofLit
    local tx = art.get("streak")
    if tx == nil or day <= 0.001 then return end
    local t, top, W = ctx.t, ctx.top(L), ctx.resX
    for i = 1, WINDS do
        local id = L * 29 + i
        local k, u, life = F.cycle(i, L * 7 + 5, t, WIND_LIFE[1], WIND_LIFE[2])
        local x = F.rand(id, k, 1) * W + F.lerp(WIND_V[1], WIND_V[2], F.rand(id, k, 2)) * (u * life - life * 0.5)
        local y = top + F.lerp(WIND_MARGIN, SLOT_H - WIND_MARGIN, F.rand(id, k, 3)) + WIND_BOB * math.sin(u * F.TAU)
        local s = F.lerp(WIND_SX[1], WIND_SX[2], F.rand(id, k, 4))
        F.sprite(tx, x, y, s, 1, 0, 1, 1, 1, math.sin(u * math.pi) * WIND_A * day)
    end
    F.reset(tx)
end

local function stars(L, ctx)
    local amt = ctx.night * ctx.roofLit
    local tx = art.get("sparkle")
    if tx == nil or amt <= 0.001 then return end
    local t, top, W = ctx.t, ctx.top(L), ctx.resX
    tx:SetBlendMode("Add")
    for i = 1, STARS do
        local id = L * 47 + i
        local k, u = F.cycle(i, L * 7 + 7, t, STAR_LIFE[1], STAR_LIFE[2])
        local tw = 0.5 + 0.5 * math.sin(t * F.lerp(STAR_TW[1], STAR_TW[2], F.rand(id, k, 4)) + F.rand(id, k, 5) * F.TAU)
        local s = F.lerp(STAR_SIZE[1], STAR_SIZE[2], F.rand(id, k, 3))
        F.sprite(tx, F.rand(id, k, 1) * W, top + F.rand(id, k, 2) * SLOT_H, s, s, 0,
                 STAR_COL[1], STAR_COL[2], STAR_COL[3], F.fade(u, STAR_FIN, STAR_FOUT) * tw * tw * STAR_A * amt)
    end
    F.reset(tx)
end

-- 0 by day, 1 at night, for the sky's current hour
function M.nightOf(hour)
    return F.smooth(NIGHT[1], NIGHT[2], hour or 12)
end

function M.back(L, ctx)
    if ctx.roofLit <= 0.001 then return end
    clouds(L, ctx, BACK_CLOUDS)
end

function M.front(L, ctx)
    if ctx.roofLit <= 0.001 then return end
    stars(L, ctx)
    winds(L, ctx)
    clouds(L, ctx, FRONT_CLOUDS)
end

return M
