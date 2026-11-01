-- Ambience of the stone stairs (levels 6-9).

local F   = require("towerfx/field")
local art = require("towerfx/art")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local look = Layout.towerfx.stairs
local motion = Animation.towerfx.stairs

local SLOT_H = look.slot_h_px

local mistLook   = look.mists
local MISTS      = mistLook.count                   -- per slot
local MIST_SIZE  = mistLook.size
local MIST_SQUASH = mistLook.squash_y
local MIST_EDGE  = mistLook.edge_px                 -- off-screen run-out on both sides (over half the widest bank)
local MIST_TOP, MIST_BOTTOM = mistLook.y_top_px, mistLook.y_bottom_inset_px
local MIST_COL   = mistLook.color
local MIST_SPEED = motion.mists.speed_px_per_s      -- rightwards
local MIST_A     = motion.mists.opacity
local MIST_BREATH = motion.mists.breath_period_s

local petalLook  = look.petals
local PETALS     = petalLook.count
local PETAL_X0, PETAL_X1 = petalLook.x_left_px, petalLook.x_right_share
local PETAL_Y    = petalLook.y_px
local PETAL_SIZE = petalLook.size
local PETAL_A    = petalLook.opacity
local petalMove  = motion.petals
local PETAL_LIFE = petalMove.life_s
local PETAL_VX, PETAL_VY = petalMove.vx_px_per_s, petalMove.vy_px_per_s
local PETAL_SWAY, PETAL_SWAY_HZ = petalMove.sway.px, petalMove.sway.hz
local PETAL_SPIN = petalMove.spin_deg_per_s
local FLIP_BASE, FLIP_AMP, FLIP_HZ = petalMove.flip.base, petalMove.flip.amp, petalMove.flip.hz
local PETAL_FIN, PETAL_FOUT = petalMove.fade_in_share, petalMove.fade_out_share

local birdLook   = look.birds
local FLOCK      = birdLook.flock                   -- birds per flock, one flock per slot
local BIRD_W, BIRD_H = birdLook.frame_w_px, birdLook.frame_h_px   -- one frame of bird.png (frames side by side)
local BIRD_COL   = birdLook.color
local BIRD_A     = birdLook.opacity
local BIRD_Y     = birdLook.y_px
local RUN_OUT    = birdLook.run_out_px
local BIRD_BACK  = birdLook.back_px
local BIRD_SIDE  = birdLook.side_px
local birdMove   = motion.birds
local FLOCK_GAP  = birdMove.gap_s
local FLIGHT     = birdMove.flight_s                -- to cross the screen
local FLAP_HZ    = birdMove.flap_hz
local FRAMES     = birdMove.frames
local FLAP_STEP  = birdMove.flap_phase_step
local SINK, BOB, BOB_W = birdMove.sink_px, birdMove.bob_px, birdMove.bob_rad_per_s

local function mist(L, t, top, W)
    local tx = art.get("mist")
    if tx == nil then return end
    for i = 1, MISTS do
        local id = L * 17 + i
        local v = F.lerp(MIST_SPEED[1], MIST_SPEED[2], F.rand(id, 0, 1))
        local x = F.wrap(F.rand(id, 0, 2) * (W + 2 * MIST_EDGE) + v * t, W, MIST_EDGE)
        local y = top + F.lerp(MIST_TOP, SLOT_H - MIST_BOTTOM, F.rand(id, 0, 3))
        local s = F.lerp(MIST_SIZE[1], MIST_SIZE[2], F.rand(id, 0, 4))
        local k = 0.5 + 0.5 * math.sin(F.TAU * t / F.lerp(MIST_BREATH[1], MIST_BREATH[2], F.rand(id, 0, 5)) + F.rand(id, 0, 6) * F.TAU)
        F.sprite(tx, x, y, s, s * MIST_SQUASH, 0, MIST_COL[1], MIST_COL[2], MIST_COL[3], F.lerp(MIST_A[1], MIST_A[2], k))
    end
    F.reset(tx)
end

local function petals(L, t, top, W)
    local tx = art.get("petal")
    if tx == nil then return end
    for i = 1, PETALS do
        local id = L * 41 + i
        local k, u, life = F.cycle(i, L * 7 + 3, t, PETAL_LIFE[1], PETAL_LIFE[2])
        local age = u * life
        local ph = F.rand(id, k, 5) * F.TAU
        local x = F.lerp(PETAL_X0, W * PETAL_X1, F.rand(id, k, 1)) + F.lerp(PETAL_VX[1], PETAL_VX[2], F.rand(id, k, 3)) * age
                  + PETAL_SWAY * math.sin(F.TAU * PETAL_SWAY_HZ * age + ph)
        local y = top + F.lerp(PETAL_Y[1], PETAL_Y[2], F.rand(id, k, 2)) + F.lerp(PETAL_VY[1], PETAL_VY[2], F.rand(id, k, 4)) * age
        local s = F.lerp(PETAL_SIZE[1], PETAL_SIZE[2], F.rand(id, k, 6))
        local flip = FLIP_BASE + FLIP_AMP * math.abs(math.cos(F.TAU * FLIP_HZ * age + ph))
        F.sprite(tx, x, y, s * flip, s, F.rand(id, k, 7) * 360 + PETAL_SPIN * age, 1, 1, 1,
                 F.fade(u, PETAL_FIN, PETAL_FOUT) * PETAL_A)
    end
    F.reset(tx)
end

-- the flock crosses during the first FLIGHT seconds of each gap, from a random side and height
local function birds(L, t, top, W)
    local tx = art.get("bird")
    if tx == nil then return end
    local id = L * 23
    local gap = F.lerp(FLOCK_GAP[1], FLOCK_GAP[2], F.rand(id, 0, 1))
    local p = t / gap + F.rand(id, 0, 2)
    local k = math.floor(p)
    local age = (p - k) * gap
    if age >= FLIGHT then return end
    local dir = (F.rand(id, k, 3) < 0.5) and 1 or -1
    local y0 = top + F.lerp(BIRD_Y[1], BIRD_Y[2], F.rand(id, k, 4))
    local lead = (W + 2 * RUN_OUT) * age / FLIGHT
    local x0 = (dir > 0) and (lead - RUN_OUT) or (W + RUN_OUT - lead)
    tx:SetColor(BIRD_COL[1], BIRD_COL[2], BIRD_COL[3])
    tx:SetOpacity(BIRD_A)
    for b = 0, FLOCK - 1 do
        local back = (b == 0) and 0 or F.lerp(BIRD_BACK[1], BIRD_BACK[2], F.rand(id, k, 5 + b))
        local side = (b == 0) and 0 or ((b == 1) and BIRD_SIDE[1] or BIRD_SIDE[2])
        local x = x0 - dir * back
        local y = y0 + side - SINK * age / FLIGHT + BOB * math.sin(age * BOB_W + b)
        local frame = math.floor(age * FLAP_HZ + b * FLAP_STEP) % FRAMES
        tx:DrawRectAtAnchor(math.floor(x + 0.5), math.floor(y + 0.5), frame * BIRD_W, 0, BIRD_W, BIRD_H, "center")
    end
    F.reset(tx)
end

function M.front(L, ctx)
    local t, top, W = ctx.t, ctx.top(L), ctx.resX
    mist(L, t, top, W)
    petals(L, t, top, W)
    birds(L, t, top, W)
end

return M
