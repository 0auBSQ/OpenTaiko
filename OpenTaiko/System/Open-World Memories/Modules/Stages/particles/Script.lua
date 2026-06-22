---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- particles/Script.lua — showcase for the Lua3DScene particle engine, now with alpha-textured
-- sprites (soft circles, ice crystals, glows) instead of flat squares. Free-fly camera
-- (WASD + mouse, Space/LShift up/down), number keys 1-6 switch scenes.

local floor, sin, cos, sqrt, pi, random = math.floor, math.sin, math.cos, math.sqrt, math.pi, math.random
local max = math.max

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
}

-- ════════════════════════════════════════════════════════════════════════════════════════════════
function setScene(i)
    sceneIdx = ((i - 1) % #SCENES) + 1
    t = 0; emitAcc = 0
    scene:PsClear(fx)
    scene:PsSetSprite(fx, -1)
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
            "1-6 scenes ([ ] cycle)   WASD+mouse fly   Space/LShift up-down   LCtrl sprint   R replay scene   C reset cam   H help   Esc quit",
            false, 1850, COLOR:CreateColorFromRGBA(210, 220, 230, 255), COLOR:CreateColorFromRGBA(0, 0, 0, 255)):Draw(30, 110)
        fontSmall:GetText(SCENES[sceneIdx].help, false, 1850,
            COLOR:CreateColorFromRGBA(150, 200, 170, 255), COLOR:CreateColorFromRGBA(0, 0, 0, 255)):Draw(30, 138)
    end
end
