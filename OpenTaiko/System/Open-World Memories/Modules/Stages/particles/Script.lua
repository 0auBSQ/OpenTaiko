---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- particles/Script.lua — showcase for the Lua3DScene particle engine, now with alpha-textured
-- sprites (soft circles, ice crystals, glows) instead of flat squares. Free-fly camera
-- (WASD + mouse, Space/LShift up/down), number keys 1-7 switch scenes.

local floor, sin, cos, sqrt, pi, random = math.floor, math.sin, math.cos, math.sqrt, math.pi, math.random
local max = math.max
local exp = math.exp

-- ── config ──────────────────────────────────────────────────────────────────────────────────────
local SCREEN_W, SCREEN_H = 1920, 1080
local RW, RH      = 960, 540
local FOV         = 80.0
local MOUSE_SENS  = 0.16
local MOVE_SPEED  = 16.0
local SPRINT      = 3.0
local CAP         = 60000

-- sprite ids (registered in onStart)
local SPR_GLOW, SPR_SPARK, SPR_SMOKE, SPR_CRYSTAL = 1, 2, 3, 4
local SPR_STREAK, SPR_RING, SPR_DROPLET = 5, 6, 7   -- rain streak, flat ripple ring, splash water droplet

-- ── state ───────────────────────────────────────────────────────────────────────────────────────
local scene, fx
local fontBig, fontMid, fontSmall
local camX, camY, camZ = 0, 8, 30
local yaw, pitch = 180, -6
local fwd, rgt = { 0, 0, -1 }, { 1, 0, 0 }
local sceneIdx = 1
local t = 0
local emitAcc = 0
local fwTimer, expState, expT = 0, 0, 0
local lastTs = 0
local showHelp = true
local hudTex, hudT = nil, 0
-- rain scene state: a wet ground quad + a pool of flat expanding ripple objects + their live state
local RIPPLE_POOL = 56
local rainGround = nil
local rippleObjs = nil      -- { objId, ... } (created on rain enter, deleted on leave)
local ripples = nil         -- per-slot { active, x, z, age }
local splashAcc = 0         -- fractional accumulator for the splash spawn rate

-- ── helpers ─────────────────────────────────────────────────────────────────────────────────────
local function hsv(h, s, v)
    h = (h % 1.0) * 6
    local i = floor(h); local f = h - i
    local p = v * (1 - s); local q = v * (1 - s * f); local w = v * (1 - s * (1 - f))
    local r, g, b
    if i == 0 then r, g, b = v, w, p elseif i == 1 then r, g, b = q, v, p
    elseif i == 2 then r, g, b = p, v, w elseif i == 3 then r, g, b = p, q, v
    elseif i == 4 then r, g, b = w, p, v else r, g, b = v, p, q end
    return r * 255, g * 255, b * 255
end
local function budget(rate, dt)
    emitAcc = emitAcc + rate * dt
    local n = floor(emitAcc); emitAcc = emitAcc - n; return n
end
local function rnd(a, b) return a + (b - a) * random() end
-- set the sprite then burst, in one call
local function burst(spr, ...) scene:PsSetSprite(fx, spr); scene:PsBurst(fx, ...) end

-- ── rain helpers ─────────────────────────────────────────────────────────────────────────────────
-- independent spawn-rate accumulator for ground splashes (budget() is used by the streaks)
local function splashBudget(rate, dt) splashAcc = splashAcc + rate * dt; local n = floor(splashAcc); splashAcc = splashAcc - n; return n end

-- A thin, soft, vertical rain streak in a square sprite: a tight gaussian core column, brighter toward
-- the bottom (the drop "head") fading to a wispy tail at the top. White (the particle colour tints it).
local function buildStreakSprite(id)
    local W, H = 48, 48
    local px = {}
    local c = (W - 1) / 2
    for y = 0, H - 1 do
        local vy = y / (H - 1)                     -- 0 = top (trailing tail), 1 = bottom (leading end)
        local vp = 0.20 + 0.80 * vy                -- a touch brighter toward the leading end
        if vy < 0.28 then vp = vp * (vy / 0.28) end                      -- fade the trailing tail
        if vy > 0.94 then vp = vp * (1 - (vy - 0.94) / 0.06) end         -- soft leading tip
        for x = 0, W - 1 do
            local dh = (x - c) / c
            local line = exp(-(dh * dh) * 220)     -- razor-thin core column (~1px), like real rain
            local al = floor(line * vp * 255 + 0.5)
            if al < 0 then al = 0 elseif al > 255 then al = 255 end
            px[y * W + x + 1] = al * 16777216 + 0xFFFFFF
        end
    end
    scene:SetSpriteRGBA(id, px, W, H)
end

-- A small water droplet: a vertically-elongated soft bead with a bright glinty core, so splash droplets
-- read as little beads of water in flight rather than flat dots. The particle colour tints it.
local function buildDropletSprite(id)
    local R = 20
    local px = {}
    local c = (R - 1) / 2
    for y = 0, R - 1 do
        for x = 0, R - 1 do
            local nx = (x - c) / c
            local ny = ((y - c) / c) * 0.6         -- squash Y → a taller-than-wide bead
            local d = sqrt(nx * nx + ny * ny)
            local body = max(0, 1 - d); body = body * body
            local core = exp(-(d * d) * 8)         -- bright centre glint (wet highlight)
            local al = floor(max(body, core) * 255 + 0.5)
            if al < 0 then al = 0 elseif al > 255 then al = 255 end
            px[y * R + x + 1] = al * 16777216 + 0xFFFFFF
        end
    end
    scene:SetSpriteRGBA(id, px, R, R)
end

-- A soft annulus (ring) sprite, laid flat on the ground as an expanding ripple. White; tinted bluish.
local function buildRingSprite(id)
    local R = 48
    local px = {}
    local c = (R - 1) / 2
    for y = 0, R - 1 do
        for x = 0, R - 1 do
            local dx, dy = (x - c) / c, (y - c) / c
            local rr = sqrt(dx * dx + dy * dy)
            local ring = exp(-((rr - 0.80) / 0.14) ^ 2)
            if rr > 1.0 then ring = ring * max(0, 1 - (rr - 1.0) * 8) end   -- clip the outer edge
            local al = floor(ring * 235 + 0.5)
            if al < 0 then al = 0 elseif al > 255 then al = 255 end
            px[y * R + x + 1] = al * 16777216 + 0xFFFFFF
        end
    end
    scene:SetSpriteRGBA(id, px, R, R)
end

-- Create the rain scene's retained objects: one big dark wet ground quad + a pool of ripple objects
-- (each a single flat ring quad, faded/sized per frame). NOT scene:ClearObjects (that also wipes the
-- particle system) — these are tracked and DeleteObject'd on leaving the scene.
local function rainSetup()
    rainGround = scene:NewObject()
    scene:ObjSetPass(rainGround, 0, 17, 21, 30, 255)        -- dark wet blue-grey, opaque
    scene:ObjSetBounds(rainGround, -600, -1, -600, 600, 1, 600)
    scene:ObjBegin(rainGround)
    scene:ObjAddQuadFlat(rainGround, -600, 0, -600, 600, 0, -600, 600, 0, 600, -600, 0, 600)
    rippleObjs = {}
    ripples = {}
    for i = 1, RIPPLE_POOL do
        local oid = scene:NewObject()
        scene:ObjSetTint(oid, 0.72, 0.82, 0.98)             -- bluish-white ripple
        scene:ObjSetVisible(oid, false)
        scene:ObjSetBounds(oid, -2, -0.3, -2, 2, 0.3, 2)
        rippleObjs[i] = oid
        ripples[i] = { active = false, x = 0, z = 0, age = 0, life = 0.7, r1 = 1.6, a0 = 1.0 }
    end
    splashAcc = 0
end

local function rainCleanup()
    scene:SetFog(false, 0, 0, 0, 0, 0)
    if rippleObjs then for _, oid in ipairs(rippleObjs) do scene:DeleteObject(oid) end; rippleObjs = nil end
    if rainGround then scene:DeleteObject(rainGround); rainGround = nil end
    ripples = nil
end

-- spawn a ripple at (x,z); strength (~0.3 small … ~1.7 big plop) scales its lifetime, max radius and
-- peak opacity, each with extra jitter so no two ripples are alike.
local function spawnRipple(x, z, strength)
    if not ripples then return end
    strength = strength or 1.0
    for i = 1, RIPPLE_POOL do
        local rp = ripples[i]
        if not rp.active then
            rp.active = true; rp.x = x; rp.z = z; rp.age = 0
            rp.life = (0.42 + 0.45 * strength) * rnd(0.85, 1.2)
            rp.r1 = (0.6 + 1.5 * strength) * rnd(0.8, 1.2)
            rp.a0 = (0.35 + 0.6 * strength) * rnd(0.7, 1.0)
            return
        end
    end
end

local RIP_R0 = 0.05
local function updateRipples(dt)
    if not ripples then return end
    for i = 1, RIPPLE_POOL do
        local rp = ripples[i]
        if rp.active then
            rp.age = rp.age + dt
            local oid = rippleObjs[i]
            if rp.age >= rp.life then
                rp.active = false
                scene:ObjSetVisible(oid, false)
            else
                local f = rp.age / rp.life
                local rad = RIP_R0 + (rp.r1 - RIP_R0) * f
                local al = floor((1 - f) * (1 - f) * rp.a0 * 235)   -- quadratic fade, scaled by impact
                local x, z, yy = rp.x, rp.z, 0.02
                scene:ObjSetVisible(oid, true)
                scene:ObjSetPass(oid, 1, 0, 0, 0, al)         -- transparent pass; a fades the sprite
                scene:ObjSetBounds(oid, x - rad, -0.2, z - rad, x + rad, 0.2, z + rad)
                scene:ObjBegin(oid)
                scene:ObjAddSpriteQuad(oid, x - rad, yy, z - rad, x + rad, yy, z - rad,
                    x + rad, yy, z + rad, x - rad, yy, z + rad, SPR_RING, 0)
            end
        end
    end
end

-- ════════════════════════════════════════════════════════════════════════════════════════════════
-- Scenes
-- ════════════════════════════════════════════════════════════════════════════════════════════════
local SCENES = {
    -- 1) FIREWORKS
    {
        name = "Fireworks Finale", bg = { 4, 5, 12 },
        help = "Periodic shells burst into soft glowing sparks that arc down on gravity.",
        enter = function() fwTimer = 0 end,
        emit = function(dt)
            fwTimer = fwTimer - dt
            while fwTimer <= 0 do
                fwTimer = fwTimer + rnd(0.18, 0.42)
                local x, y, z = rnd(-26, 26), rnd(14, 30), rnd(-26, 26)
                local hue = random()
                local r, g, b = hsv(hue, 0.7, 1.0)
                burst(SPR_SPARK, x, y, z, floor(rnd(700, 1400)), 0, 1, 0, 1.0, rnd(9, 16),
                    r, g, b, 25, 1.0, 0.5, 0.05, 1.8, 0.45, 6.0, 0.55, 1)
                burst(SPR_GLOW, x, y, z, 200, 0, 1, 0, 1.0, rnd(4, 7),
                    255, 245, 220, 10, 1.0, 0.7, 0.05, 0.7, 0.4, 3.0, 0.7, 1)
                local r2, g2, b2 = hsv(hue + 0.5, 0.8, 1.0)
                burst(SPR_SPARK, x, y, z, 400, 0, 1, 0, 1.0, rnd(5, 9),
                    r2, g2, b2, 20, 1.0, 0.4, 0.04, 1.5, 0.4, 6.0, 0.6, 1)
            end
        end,
    },
    -- 2) FIRE — a single realistic campfire (buoyant flame, embers, smoke), with flicker
    {
        name = "Fire", bg = { 12, 7, 4 },
        help = "A single realistic fire: layered buoyant flame, rising embers and dark smoke.",
        enter = function() end,
        emit = function(dt)
            -- NOTE: soft/overlapping particles look the same with far fewer of them, so counts are kept
            -- modest — big additive/alpha billboards are fill (overdraw) bound, not count bound.
            local flick = 0.7 + 0.3 * sin(t * 17) * sin(t * 6.3)   -- flicker the intensity
            local bx, bz = rnd(-0.35, 0.35), rnd(-0.35, 0.35)
            -- outer flame: orange, soft, buoyant (negative gravity = rises), shrinks
            burst(SPR_GLOW, bx, 0.15, bz, budget(2000 * flick, dt), 0, 1, 0, 0.55, rnd(2.2, 3.4),
                255, rnd(70, 150), 24, 30, 0.9, 0.9, 0.12, rnd(0.7, 1.1), 0.35, -3.2, 0.7, 1)
            -- hot inner core: white-yellow, smaller, short-lived, near the base
            burst(SPR_GLOW, bx, 0.1, bz, budget(1000, dt), 0, 1, 0, 0.5, 1.8,
                255, 225, 130, 18, 1.0, 0.7, 0.08, 0.5, 0.3, -2.4, 0.7, 1)
            -- embers: bright sharp sparks that pop upward and drift (small → cheap)
            burst(SPR_SPARK, bx, 0.4, bz, budget(200, dt), 0, 1, 0, 0.7, 5.0,
                255, 190, 80, 25, 1.0, 0.12, 0.02, rnd(1.0, 1.8), 0.6, -1.6, 0.25, 1)
            -- smoke: dark, soft, alpha-blended, rises and grows (few — big puffs overlap)
            burst(SPR_SMOKE, rnd(-0.5, 0.5), 2.2, rnd(-0.5, 0.5), budget(230, dt), 0, 1, 0, 0.5, 1.4,
                36, 33, 38, 10, 0.55, 1.1, 2.8, 2.6, 0.4, -0.7, 0.45, 0)
        end,
    },
    -- 3) SPACE — drifting starfield + faint nebula clouds all around the camera
    {
        name = "Lost in Space", bg = { 2, 2, 6 },
        help = "A starfield and slow nebula clouds emitted all around you — fly through the void.",
        enter = function() end,
        emit = function(dt)
            -- stars: tiny sharp additive points in a big box around the camera, very slow, long life
            local n = budget(2600, dt)
            for _ = 1, n do
                local hue = rnd(0.55, 0.72)
                local r, g, b = hsv(hue, rnd(0.0, 0.4), 1.0)
                scene:PsSetSprite(fx, SPR_SPARK)
                scene:PsEmit(fx, camX + rnd(-70, 70), camY + rnd(-50, 50), camZ + rnd(-70, 70),
                    rnd(-0.2, 0.2), rnd(-0.2, 0.2), rnd(-0.2, 0.2),
                    r, g, b, 1.0, rnd(0.06, 0.22), rnd(0.06, 0.22), rnd(10, 22), 0, 0, 1)
            end
            -- nebula: a few big, very faint coloured glows drifting slowly
            if budget(8, dt) > 0 then
                local r, g, b = hsv(rnd(0.6, 0.95), 0.7, 1.0)
                scene:PsSetSprite(fx, SPR_SMOKE)
                scene:PsEmit(fx, camX + rnd(-50, 50), camY + rnd(-35, 35), camZ + rnd(-50, 50),
                    rnd(-0.3, 0.3), rnd(-0.2, 0.2), rnd(-0.3, 0.3),
                    r * 0.5, g * 0.5, b * 0.6, 0.18, rnd(14, 26), rnd(20, 34), rnd(10, 18), 0, 0.04, 1)
            end
        end,
    },
    -- 4) FOREST — sunlit pollen/dust motes, drifting leaves, blinking fireflies
    {
        name = "Forest Clearing", bg = { 8, 16, 8 },
        help = "Sunlit pollen drifting in the air, slow falling leaves and blinking fireflies.",
        enter = function() end,
        emit = function(dt)
            -- pollen / dust: tiny warm motes hanging in shafts of light, very slow, additive
            local n = budget(1800, dt)
            for _ = 1, n do
                scene:PsSetSprite(fx, SPR_GLOW)
                scene:PsEmit(fx, camX + rnd(-26, 26), camY + rnd(-8, 16), camZ + rnd(-26, 26),
                    rnd(-0.25, 0.25), rnd(-0.15, 0.05), rnd(-0.25, 0.25),
                    rnd(220, 255), rnd(220, 245), rnd(120, 170), 0.5, rnd(0.04, 0.1), rnd(0.04, 0.1), rnd(6, 12), 0.02, 0.4, 1)
            end
            -- leaves: green/brown/orange, alpha, falling and swaying
            for _ = 1, budget(120, dt) do
                local r, g, b = hsv(rnd(0.18, 0.32), rnd(0.5, 0.9), rnd(0.55, 0.95))
                scene:PsSetSprite(fx, SPR_GLOW)
                scene:PsEmit(fx, camX + rnd(-22, 22), camY + rnd(14, 22), camZ + rnd(-22, 22),
                    rnd(-1.2, 1.2), rnd(-1.0, -0.4), rnd(-1.2, 1.2),
                    r, g, b, 0.9, rnd(0.25, 0.5), rnd(0.25, 0.5), rnd(4, 7), 0.8, 0.9, 0)
            end
            -- fireflies: occasional bright green-yellow blinking glows
            for _ = 1, budget(40, dt) do
                scene:PsSetSprite(fx, SPR_GLOW)
                scene:PsEmit(fx, camX + rnd(-18, 18), camY + rnd(-2, 8), camZ + rnd(-18, 18),
                    rnd(-0.6, 0.6), rnd(-0.3, 0.3), rnd(-0.6, 0.6),
                    180, 255, 120, 1.0, 0.16, 0.02, rnd(0.6, 1.4), 0, 0.5, 1)
            end
        end,
    },
    -- 5) BLIZZARD — dense ice crystals driven by a strong, gusting horizontal wind
    {
        name = "Blizzard", bg = { 8, 11, 20 },
        help = "Dense ice crystals driven by a strong gusting wind, lit against a night sky.",
        enter = function() end,
        emit = function(dt)
            -- wind: a strong horizontal gust that swings direction and strength over time
            local wDir = 0.5 + 0.6 * sin(t * 0.35)
            local wMag = 10 + 7 * (0.5 + 0.5 * sin(t * 0.8)) + 4 * sin(t * 2.3)   -- gusts
            local wx, wz = cos(wDir) * wMag, sin(wDir) * wMag
            -- emit crystals from the upwind side, blown across the camera volume
            local n = budget(9000, dt)
            for _ = 1, n do
                scene:PsSetSprite(fx, SPR_CRYSTAL)
                -- spawn upwind + spread across a tall wall; turbulence per particle
                local ox = camX - wx * 3.2 + rnd(-30, 30)
                local oz = camZ - wz * 3.2 + rnd(-30, 30)
                scene:PsEmit(fx, ox, camY + rnd(-20, 24), oz + rnd(-10, 10),
                    wx + rnd(-3, 3), rnd(-3.5, -0.5) + sin(t * 5 + ox) * 1.5, wz + rnd(-3, 3),
                    rnd(225, 245), rnd(235, 248), 255, rnd(0.7, 1.0), rnd(0.12, 0.32), rnd(0.12, 0.32),
                    rnd(2.5, 4.5), 0.9, 0.12, 0)
            end
        end,
    },
    -- 6) EXPLOSION — a staged, realistic blast: flash → turbulent multi-shell fireball + shockwave
    --    ring + arcing shrapnel, then a rising mushroom of smoke billowing over ~1.2s. R replays.
    {
        name = "Explosion", bg = { 6, 6, 10 },
        help = "Flash, turbulent fireball, shockwave ring, arcing shrapnel, then a rising smoke mushroom. R replays.",
        enter = function() expState = 1; expT = 0 end,
        emit = function(dt)
            expT = expT + dt
            local cy = 6
            if expState == 1 then
                expState = 2
                -- 1) white-hot flash (brief)
                burst(SPR_GLOW, 0, cy, 0, 220, 0, 1, 0, 1.0, 5, 255, 250, 230, 8, 1.0, 4.0, 0.4, 0.18, 0.2, 0, 0.8, 1)
                -- 2) turbulent fireball: shells from hot/slow (white-yellow core) to cool/fast (deep red
                --    edge), each at a jittered ignition point so the ball is irregular, not a clean sphere
                for k = 0, 4 do
                    local f = k / 4
                    local r, g, b = 255, 235 - f * 195, 130 - f * 115
                    burst(SPR_GLOW, rnd(-1, 1), cy + rnd(-0.6, 1.3), rnd(-1, 1), 520, 0, 1, 0, 1.0, 7 + k * 6,
                        r, g, b, 18, 1.0, 1.8 - f * 0.5, 0.18, rnd(0.35, 0.7), 0.45, -1.2, 0.7, 1)
                end
                -- 3) shockwave: a fast, thin, near-horizontal expanding ring
                scene:PsSetSprite(fx, SPR_GLOW)
                for _ = 1, 130 do
                    local a = rnd(0, 2 * pi); local sp = rnd(22, 28)
                    scene:PsEmit(fx, 0, cy, 0, cos(a) * sp, rnd(-1.6, 1.6), sin(a) * sp,
                        255, 238, 200, 0.85, 0.55, 0.04, 0.26, 0, 1.4, 1)
                end
                -- 4) shrapnel: fast small sparks, gravity arcs + long fading trails (cheap — small)
                burst(SPR_SPARK, 0, cy, 0, 3500, 0, 1, 0, 1.0, 32,
                    255, 200, 110, 25, 1.0, 0.2, 0.02, rnd(1.6, 2.8), 0.6, 11.0, 0.1, 1)
            end
            -- 5) mushroom smoke, emitted over ~1.2s so it billows upward instead of popping as a ball
            if expT < 1.2 then
                -- stem: rises from the blast, dark, growing
                burst(SPR_SMOKE, rnd(-0.6, 0.6), cy, rnd(-0.6, 0.6), budget(520, dt), 0, 1, 0, 0.5, 2.2,
                    42, 40, 44, 12, 0.6, 1.4, 4.0, 2.6, 0.4, -1.4, 0.5, 0)
                -- cap: once the stem has risen, spread outward to form the mushroom head
                if expT > 0.35 then
                    burst(SPR_SMOKE, 0, cy + (expT - 0.35) * 5, 0, budget(320, dt), 0, 1, 0, 0.85, 3.0,
                        50, 47, 52, 12, 0.6, 1.8, 4.4, 2.4, 0.4, -0.4, 0.6, 0)
                end
            end
        end,
    },
    -- 7) RAIN — heavy rain falling onto wet ground: wind-blown streaks, splash crowns + rebound jets,
    --    low mist where drops land, and expanding ripples on the ground. Misty fog adds depth.
    {
        name = "Rain", bg = { 26, 31, 41 },
        help = "Heavy rain on wet ground: long wind-blown streaks, splash crowns + mist where drops land, and expanding ripples.",
        enter = function()
            rainSetup()
            scene:SetFog(true, 34, 40, 52, 26, 170)   -- hazy depth + hides the far ground edge
        end,
        emit = function(dt)
            local RANGE = 30
            local wx = 2.4 * sin(t * 0.27)            -- gentle, slowly shifting wind
            local wz = 1.5 * cos(t * 0.19)
            local FALL, SPAWN_Y = 30, 26
            -- 1) falling rain: a mix of mostly fine, faint, fast drops with the occasional fatter one
            --    (size skewed small via random²). Per-drop length, opacity, fall speed and tint all vary,
            --    so the curtain never looks uniform — bigger drops are longer, brighter and faster.
            scene:PsSetSprite(fx, SPR_STREAK)
            for _ = 1, budget(9000, dt) do
                local rx = camX + rnd(-RANGE, RANGE)
                local rz = camZ + rnd(-RANGE, RANGE)
                local g = random(); g = g * g                -- 0..1, skewed toward small drops
                local len = 0.16 + g * 0.82                  -- ~0.16 (fine) … ~0.98 (fat), mostly small
                local fall = FALL + g * 16 + rnd(-2, 3)      -- bigger drops fall faster
                local a = 0.16 + g * 0.5 + rnd(-0.04, 0.07)  -- and are more opaque; many faint small ones
                local tb = rnd(-14, 14)                      -- subtle per-drop tint shift (sky glints)
                scene:PsEmit(fx, rx, SPAWN_Y, rz,
                    wx + rnd(-1.0, 1.0), -fall, wz + rnd(-1.0, 1.0),
                    158 + tb, 184 + tb, 216 + tb * 0.5, a, len, len, SPAWN_Y / fall, 0, 0, 0)
            end
            -- 2) ground impacts: real splash anatomy, not round puffs. Droplets are launched in a RING that
            --    flares up + outward (the crown / coronet), and a thin near-vertical central JET rebounds and
            --    beads off (Rayleigh jet); all are little elongated water beads that arc back under gravity.
            --    Classed by drop size so most impacts are light taps and only a few are big plops.
            for _ = 1, splashBudget(210, dt) do
                local ang = rnd(0, 2 * pi); local rr = sqrt(random()) * RANGE
                local sx = camX + cos(ang) * rr
                local sz = camZ + sin(ang) * rr
                local m = random()
                scene:PsSetSprite(fx, SPR_DROPLET)
                if m < 0.50 then
                    -- light tap: a small crown of a few beads flicked up + out, a faint little ripple
                    local n = 3 + floor(random() * 3)
                    local cout, cup = rnd(0.9, 1.7), rnd(1.2, 2.0)
                    for k = 1, n do
                        local a = (k / n) * 2 * pi + rnd(-0.5, 0.5)
                        local s = rnd(0.012, 0.024)
                        scene:PsEmit(fx, sx, 0.03, sz, cos(a) * cout * rnd(0.8, 1.2), cup * rnd(0.85, 1.25), sin(a) * cout * rnd(0.8, 1.2),
                            199, 215, 237, 0.7, s, s * 0.45, rnd(0.18, 0.32), 16.0, 0.3, 0)
                    end
                    spawnRipple(sx, sz, rnd(0.3, 0.65))
                elseif m < 0.85 then
                    -- splash: a flaring crown ring + a thin central rebound jet + a ripple, sometimes a wisp of mist
                    local n = 6 + floor(random() * 5)
                    local cout, cup = rnd(1.6, 2.6), rnd(2.0, 3.0)
                    for k = 1, n do
                        local a = (k / n) * 2 * pi + rnd(-0.35, 0.35)
                        local s = rnd(0.014, 0.028)
                        scene:PsEmit(fx, sx, 0.04, sz, cos(a) * cout * rnd(0.8, 1.2), cup * rnd(0.85, 1.3), sin(a) * cout * rnd(0.8, 1.2),
                            203, 219, 240, 0.82, s, s * 0.45, rnd(0.30, 0.5), 15.0, 0.25, 0)
                    end
                    local nj = 2 + floor(random() * 2)
                    for k = 1, nj do
                        local s = rnd(0.016, 0.026)
                        scene:PsEmit(fx, sx, 0.05, sz, rnd(-0.25, 0.25), rnd(3.0, 4.2) * (0.7 + 0.4 * k / nj), rnd(-0.25, 0.25),
                            210, 224, 243, 0.85, s, s * 0.6, rnd(0.40, 0.62), 15.0, 0.2, 0)
                    end
                    spawnRipple(sx, sz, rnd(0.7, 1.1))
                    if random() < 0.35 then
                        scene:PsSetSprite(fx, SPR_SMOKE)
                        scene:PsEmit(fx, sx, 0.05, sz, rnd(-0.12, 0.12), rnd(0.25, 0.5), rnd(-0.12, 0.12),
                            184, 200, 222, rnd(0.05, 0.11), 0.06, rnd(0.18, 0.28), rnd(0.18, 0.30), 0.7, 1.6, 0)
                    end
                else
                    -- big plop (rare): a tall flaring crown, a tall Rayleigh jet, scattered fine spray, wide ripple + mist
                    local n = 11 + floor(random() * 8)
                    local cout, cup = rnd(2.2, 3.4), rnd(2.8, 4.0)
                    for k = 1, n do
                        local a = (k / n) * 2 * pi + rnd(-0.3, 0.3)
                        local s = rnd(0.016, 0.034)
                        scene:PsEmit(fx, sx, 0.04, sz, cos(a) * cout * rnd(0.8, 1.25), cup * rnd(0.85, 1.35), sin(a) * cout * rnd(0.8, 1.25),
                            205, 221, 242, 0.85, s, s * 0.45, rnd(0.35, 0.6), 15.0, 0.2, 0)
                    end
                    local nj = 3 + floor(random() * 3)
                    for k = 1, nj do
                        local s = rnd(0.018, 0.03)
                        scene:PsEmit(fx, sx, 0.06, sz, rnd(-0.3, 0.3), rnd(4.2, 6.0) * (0.6 + 0.5 * k / nj), rnd(-0.3, 0.3),
                            213, 227, 245, 0.9, s, s * 0.6, rnd(0.50, 0.75), 15.0, 0.15, 0)
                    end
                    scene:PsBurst(fx, sx, 0.05, sz, 5 + floor(random() * 6), 0, 1, 0, 0.6, rnd(2.5, 4.0),   -- fine secondary spray
                        206, 222, 242, 12, 0.7, rnd(0.008, 0.016), 0.005, rnd(0.25, 0.45), 0.4, 15.0, 0.3, 0)
                    spawnRipple(sx, sz, rnd(1.1, 1.7))
                    scene:PsSetSprite(fx, SPR_SMOKE)
                    scene:PsEmit(fx, sx, 0.06, sz, rnd(-0.2, 0.2), rnd(0.4, 0.7), rnd(-0.2, 0.2),
                        186, 202, 226, rnd(0.10, 0.18), 0.08, rnd(0.30, 0.42), rnd(0.24, 0.38), 0.8, 1.4, 0)
                end
            end
            -- 3) advance + draw the flat expanding ripples
            updateRipples(dt)
        end,
    },
}

-- ════════════════════════════════════════════════════════════════════════════════════════════════
function setScene(i)
    sceneIdx = ((i - 1) % #SCENES) + 1
    t = 0; emitAcc = 0
    scene:PsClear(fx)
    scene:PsSetSprite(fx, -1)
    rainCleanup()                 -- tear down any rain ground/ripple objects + fog from a previous scene
    SCENES[sceneIdx].enter()
    hudTex = nil
end

function onStart()
    scene = SCENE3D:CreateScene(RW, RH)
    scene:SetMode("raster")
    scene:SetLighting(false)
    scene:SetCameraFov(FOV)
    scene:SetCameraNear(0.05)
    scene:SetThreads(8)
    scene:SetFog(false, 0, 0, 0, 0, 0)
    fx = scene:NewParticleSystem()
    scene:PsSetCap(fx, CAP)
    -- particle sprites: a soft glow, a sharp spark, a wide soft smoke puff, an ice crystal
    scene:MakeSoftCircle(SPR_GLOW, 32, 2.2)
    scene:MakeSoftCircle(SPR_SPARK, 24, 0.7)
    scene:MakeSoftCircle(SPR_SMOKE, 48, 3.2)
    scene:MakeCrystal(SPR_CRYSTAL, 48)
    buildStreakSprite(SPR_STREAK)   -- rain streak
    buildRingSprite(SPR_RING)       -- ripple ring
    buildDropletSprite(SPR_DROPLET) -- splash water droplet
    fontBig   = TEXT:Create(40)
    fontMid   = TEXT:Create(26)
    fontSmall = TEXT:Create(18)
    setScene(1)
end

function activate() lastTs = 0; INPUT:SetMouseLocked(true) end
function deactivate() INPUT:SetMouseLocked(false) end
function afterSongEnum() end
function onDestroy() end

local function kd(k) return INPUT:KeyboardPressing(k) end
local function kp(k) return INPUT:KeyboardPressed(k) end

function update(ts)
    local dt = (ts - lastTs) / 1000.0
    lastTs = ts
    if dt < 0 then dt = 0 elseif dt > 0.1 then dt = 0.1 end

    if kp("Escape") then INPUT:SetMouseLocked(false); return Exit("stage", "_title") end
    if kp("H") then showHelp = not showHelp end
    for i = 1, #SCENES do if kp(tostring(i)) then setScene(i) end end
    if kp("LeftBracket") then setScene(sceneIdx - 1) end
    if kp("RightBracket") then setScene(sceneIdx + 1) end
    if kp("R") then setScene(sceneIdx) end                       -- reset / replay current scene
    if kp("C") then camX, camY, camZ, yaw, pitch = 0, 8, 30, 180, -6 end

    local dmx, dmy = INPUT:GetMouseDelta()
    yaw = yaw + dmx * MOUSE_SENS
    pitch = pitch - dmy * MOUSE_SENS
    if pitch > 89 then pitch = 89 elseif pitch < -89 then pitch = -89 end
    scene:SetCameraAngles(yaw, pitch)
    fwd = { scene:GetCameraForward() }
    rgt = { scene:GetCameraRight() }

    local sp = MOVE_SPEED * (kd("LeftControl") and SPRINT or 1) * dt
    local fX, fZ = fwd[1], fwd[3]
    local fl = sqrt(fX * fX + fZ * fZ); if fl < 1e-6 then fl = 1e-6 end
    fX, fZ = fX / fl, fZ / fl
    if kd("W") then camX = camX + fX * sp; camZ = camZ + fZ * sp end
    if kd("S") then camX = camX - fX * sp; camZ = camZ - fZ * sp end
    if kd("D") then camX = camX + rgt[1] * sp; camZ = camZ + rgt[3] * sp end
    if kd("A") then camX = camX - rgt[1] * sp; camZ = camZ - rgt[3] * sp end
    if kd("Space") then camY = camY + sp end
    if kd("LeftShift") then camY = camY - sp end
    scene:SetCameraPosition(camX, camY, camZ)

    SCENES[sceneIdx].emit(dt)
    scene:PsUpdate(fx, dt)
    t = t + dt

    hudT = hudT - dt
    if hudT <= 0 or hudTex == nil then
        hudT = 0.2
        hudTex = fontMid:GetText(
            string.format("[%d] %s    particles: %d    %.0f fps", sceneIdx, SCENES[sceneIdx].name, scene:PsCount(fx), 1 / max(dt, 1e-4)),
            false, 1700, COLOR:CreateColorFromRGBA(180, 240, 255, 255), COLOR:CreateColorFromRGBA(0, 0, 0, 255))
    end
    return nil
end

local function drawBg()
    local c = SCENES[sceneIdx].bg or { 6, 7, 14 }
    local bands = 16
    local bh = RH / bands
    for i = 0, bands - 1 do
        local f = i / (bands - 1)
        scene:FillRect(0, floor(i * bh), RW, floor(bh) + 2,
            floor(c[1] * (0.6 + 0.7 * f)), floor(c[2] * (0.6 + 0.7 * f)), floor(c[3] * (0.6 + 0.7 * f)), 255)
    end
end

function draw()
    drawBg()
    scene:Render()
    scene:Upload()
    scene:SetColor(1, 1, 1); scene:SetOpacity(1.0)
    scene:SetScale(SCREEN_W / RW, SCREEN_H / RH)
    scene:Draw(0, 0)

    fontBig:GetText("Particle Showcase", false, 900,
        COLOR:CreateColorFromRGBA(255, 255, 255, 255), COLOR:CreateColorFromRGBA(0, 0, 0, 255)):Draw(28, 18)
    if hudTex then hudTex:Draw(30, 74) end
    if showHelp then
        fontSmall:GetText(
            "1-7 scenes ([ ] cycle)   WASD+mouse fly   Space/LShift up-down   LCtrl sprint   R replay scene   C reset cam   H help   Esc quit",
            false, 1850, COLOR:CreateColorFromRGBA(210, 220, 230, 255), COLOR:CreateColorFromRGBA(0, 0, 0, 255)):Draw(30, 110)
        fontSmall:GetText(SCENES[sceneIdx].help, false, 1850,
            COLOR:CreateColorFromRGBA(150, 200, 170, 255), COLOR:CreateColorFromRGBA(0, 0, 0, 255)):Draw(30, 138)
    end
end
