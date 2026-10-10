---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
local Tex = require("tex")

local C = {}

C.DESK_Y = 778
local VPX, VPY = 960, 430
local HALF_W, FLOOR_Y, CEIL_Y = 1200, 660, -840
C.FLOOR_Y = FLOOR_Y
local KICK_Y = 520                             -- the glass's bottom edge (the kick plate's top)
local F = 1663                                 -- depth of z = 1 in px: world speeds along the walls are px / F
local TABLE_X, TABLE_Y = 960, 1080
local FISH_KINDS = 6

-- seaweed out on the sea floor: side, distance behind the glass, depth, kind, height (px at z = 1), sway, period;
-- the sea floor falls away from the glass, so a weed's foot always stays hidden behind the kick plate
local WEEDS = {
    { side = -1, d = 60,  z = 1.30, kind = 2, h = 470, sway = 5, period = 4.6 },
    { side = -1, d = 260, z = 1.75, kind = 1, h = 520, sway = 7, period = 3.8 },
    { side = -1, d = 120, z = 2.30, kind = 3, h = 430, sway = 6, period = 4.2 },
    { side = -1, d = 520, z = 2.80, kind = 2, h = 600, sway = 5, period = 5.1 },
    { side = 1,  d = 90,  z = 1.45, kind = 1, h = 480, sway = 6, period = 4.0 },
    { side = 1,  d = 340, z = 2.00, kind = 3, h = 520, sway = 7, period = 3.6 },
    { side = 1,  d = 160, z = 2.55, kind = 2, h = 460, sway = 5, period = 4.8 },
    { side = 1,  d = 600, z = 3.10, kind = 1, h = 620, sway = 6, period = 4.4 },
}
local WEED_SQUEEZE = 0.7                       -- seaweed drawn narrower than tall

local MAX_FISH, MAX_BUBBLES = 9, 40

local ctx = nil
local VaultWater = nil
do
    local ok, mod = pcall(require, "VaultWater")
    if ok and type(mod) == "table" then VaultWater = mod end
end
local water, waterTried = nil, false
local fish, bubbles = {}, {}
local nextFish, bubbleAcc = 0, 0
local clock = 0
local weedPhase = {}
local triedWater = false

local function rnd(a, b) return a + (b - a) * math.random() end

local function project(X, Y, z) return VPX + X / z, VPY + Y / z end
C.project = project
function C.unproject(x, y, z) return (x - VPX) * z, (y - VPY) * z end

-- 0 near the glass .. 1 far out in the murk
local function fog(z, d) return Tex.clamp01((z - 1) / 3.6 + d / 2600) end

local function kinds(prefix, n)
    local out = {}
    for i = 1, n do
        if Tex.ok(ctx.tex[prefix .. i]) then out[#out + 1] = i end
    end
    return out
end

-- a fish swimming along a wall, away from us (dir 1) or towards us (dir -1); at = how far along it already is
local function spawnFish(at)
    local have = kinds("Cabin/Fish", FISH_KINDS)
    if #have == 0 then return end
    local side = (math.random() < 0.5) and -1 or 1
    local d = rnd(40, 900) * rnd(0.4, 1)
    local len = rnd(200, 300)
    local X = side * (HALF_W + d)
    -- from just off the screen's edge to hidden behind the back wall's corner
    local zNear = (HALF_W + d - len) / 960
    local zFar = (HALF_W + d + len) / 400
    local dir = (math.random() < 0.5) and 1 or -1
    local z0, z1 = zNear, zFar
    if dir < 0 then z0, z1 = zFar, zNear end
    local f = {
        kind = have[math.random(#have)], side = side, d = d, X = X, len = len, dir = dir,
        Y = rnd(-620, 400), z = Tex.lerp(z0, z1, at or 0), zEnd = z1,
        speed = rnd(170, 420) / F,
        bob = rnd(10, 26), bobF = rnd(0.3, 0.6), ph = rnd(0, 6.283),
    }
    fish[#fish + 1] = f
end

local function spawnBubble(Y)
    local side = (math.random() < 0.5) and -1 or 1
    local d = rnd(20, 700)
    bubbles[#bubbles + 1] = {
        X = side * (HALF_W + d), d = d, z = rnd(1.15, 3.4), Y = Y or (KICK_Y + 80) * (HALF_W + d) / HALF_W,
        vy = rnd(140, 260), r = rnd(14, 30), wob = rnd(8, 20), wobF = rnd(0.6, 1.4), ph = rnd(0, 6.283), age = 0,
    }
end

function C.init(c)
    ctx = c
end

function C.open()
    if waterTried or VaultWater == nil then return end
    waterTried = true
    local ok, w = pcall(VaultWater.new, { w = Tex.SW, h = Tex.SH })
    if ok and w ~= nil then water = w end
end

function C.close()
    if water ~= nil then
        pcall(function() water:dispose() end)
        water = nil
    end
    waterTried = false
end

function C.reset()
    fish, bubbles = {}, {}
    clock = 0
    triedWater = false
    for i = 1, 5 do spawnFish(rnd(0.1, 0.85)) end
    for i = 1, 22 do spawnBubble(rnd(CEIL_Y, KICK_Y)) end
    nextFish = rnd(1.5, 3)
    bubbleAcc = 0
    for i = 1, #WEEDS do weedPhase[i] = rnd(0, 6.283) end
end

local function step(dt)
    clock = clock + dt
    if water ~= nil and water.ok then water:update(dt) end

    nextFish = nextFish - dt
    if nextFish <= 0 then
        if #fish < MAX_FISH then spawnFish(0) end
        nextFish = rnd(1.8, 4.2)
    end
    local i = 1
    while i <= #fish do
        local f = fish[i]
        f.z = f.z + f.dir * f.speed * dt
        if (f.dir > 0 and f.z > f.zEnd) or (f.dir < 0 and f.z < f.zEnd) then
            table.remove(fish, i)
        else
            i = i + 1
        end
    end

    bubbleAcc = bubbleAcc + dt * 3
    while bubbleAcc >= 1 do
        bubbleAcc = bubbleAcc - 1
        if #bubbles < MAX_BUBBLES then spawnBubble() end
    end
    i = 1
    while i <= #bubbles do
        local b = bubbles[i]
        b.age = b.age + dt
        b.Y = b.Y - b.vy * dt
        if b.Y < CEIL_Y - 40 then table.remove(bubbles, i) else i = i + 1 end
    end
end

local function drawFish(f)
    local tex = ctx.tex["Cabin/Fish" .. f.kind]
    if not Tex.ok(tex) then return end
    local wave = 2 * math.pi * f.bobF * clock + f.ph
    local Y = f.Y + f.bob * math.sin(wave)
    local x, y = project(f.X, Y, f.z)
    -- which way it moves on screen: where it will be a moment later
    local x2, y2 = project(f.X, Y, f.z + f.dir * 0.02)
    local vx, vy = x2 - x, y2 - y
    local right = vx >= 0
    local tilt = math.deg(math.atan(vy, math.abs(vx) + 1e-6))
    tilt = math.max(-24, math.min(24, tilt)) + 5 * math.cos(wave)
    local k = (f.len / f.z) / math.max(1, tex.Width - 12)
    local g = fog(f.z, f.d)
    local shade = 1 - 0.35 * g
    tex:SetScale(right and k or -k, k)
    tex:SetRotation(right and -tilt or tilt)
    tex:SetColor(shade * (1 - 0.3 * g), shade * (1 - 0.1 * g), shade)
    tex:SetOpacity(1 - 0.4 * g)
    tex:DrawAtAnchor(x, y, "center")
    tex:SetScale(1, 1)
    tex:SetRotation(0)
    tex:SetColor(1, 1, 1)
    tex:SetOpacity(1)
end

local function drawWeed(i, w)
    local tex = ctx.tex["Cabin/Seaweed" .. w.kind]
    if not Tex.ok(tex) or tex.Height <= 0 then return end
    local X = w.side * (HALF_W + w.d)
    local foot = (KICK_Y + 50) * (HALF_W + w.d) / HALF_W
    local x, y = project(X, foot, w.z)
    local k = ((w.h + foot - KICK_Y) / w.z) / tex.Height
    local deg = w.sway * math.sin(2 * math.pi * clock / w.period + weedPhase[i])
    local dx, dy = Tex.turn(0, -tex.Height * k / 2, deg)
    local g = fog(w.z, w.d)
    local shade = 1 - 0.55 * g
    tex:SetScale((w.side < 0 and k or -k) * WEED_SQUEEZE, k)
    tex:SetRotation(deg)
    tex:SetColor(shade * 0.82, shade * 0.94, shade)
    tex:SetOpacity(1 - 0.4 * g)
    tex:DrawAtAnchor(x + dx, y + dy, "center")
    tex:SetScale(1, 1)
    tex:SetRotation(0)
    tex:SetColor(1, 1, 1)
    tex:SetOpacity(1)
end

local function drawBubble(b)
    local tex = ctx.tex["Cabin/Bubble"]
    if not Tex.ok(tex) or tex.Width <= 0 then return end
    local X = b.X + b.wob * math.sin(2 * math.pi * b.wobF * clock + b.ph)
    local x, y = project(X, b.Y, b.z)
    local k = (2 * b.r / b.z) / tex.Width
    local op = math.min(1, b.age / 0.4) * (1 - 0.5 * fog(b.z, b.d)) * 0.85
    Tex.place(tex, x, y, "center", k, k, op)
end

-- everything behind the glass, the furthest first
local function drawSea()
    local items = {}
    for i, w in ipairs(WEEDS) do items[#items + 1] = { z = w.z + w.d / F, kind = 1, i = i, o = w } end
    for _, f in ipairs(fish) do items[#items + 1] = { z = f.z + f.d / F, kind = 2, o = f } end
    for _, b in ipairs(bubbles) do items[#items + 1] = { z = b.z + b.d / F, kind = 3, o = b } end
    table.sort(items, function(a, b) return a.z > b.z end)
    for _, it in ipairs(items) do
        if it.kind == 1 then drawWeed(it.i, it.o)
        elseif it.kind == 2 then drawFish(it.o)
        else drawBubble(it.o) end
    end
end

function C.drawBack(now, dt)
    C.open()
    step(dt or 0)
    local tex = ctx.tex
    if water ~= nil and water.ok then
        water:drawUnderwater(1)
    else
        -- the still picture of the sea, loaded the first time it is needed (freed with the stage's textures)
        if tex["Cabin/Water"] == nil and not triedWater then
            triedWater = true
            local path = "Textures/Cabin/Water.png"
            if TEXTURE:Exists(path) then tex["Cabin/Water"] = TEXTURE:CreateTexture(path) end
        end
        Tex.place(tex["Cabin/Water"], 0, 0, "topleft")
    end
    drawSea()
    Tex.place(tex["Cabin/Room"], 0, 0, "topleft")
end

function C.drawFront(now)
    local tex = ctx.tex
    Tex.place(tex["Cabin/Table"], TABLE_X, TABLE_Y, "bottom")
end

return C
