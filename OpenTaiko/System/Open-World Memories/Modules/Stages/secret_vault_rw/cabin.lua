---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
local Tex = require("tex")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Sea, SeaAnim = Layout.sea, Animation.sea
local Fish, Bubble, Weed = Sea.fish, Sea.bubble, Sea.weed
local FishAnim, BubbleAnim = SeaAnim.fish, SeaAnim.bubble

local C = {}

C.DESK_Y = Sea.desk_y
local VPX, VPY = Sea.vanish_x, Sea.vanish_y
local HALF_W, FLOOR_Y, CEIL_Y = Sea.half_w, Sea.floor_y, Sea.ceil_y
C.FLOOR_Y = FLOOR_Y
local KICK_Y = Sea.kick_y                      -- the height the sea's things are laid out from
local FOOT_Y = Sea.foot_y                      -- seaweed stands on this screen row, behind the counter
local F = Sea.depth_px                         -- depth of z = 1 in px: world speeds along the walls are px / F
local TABLE_X, TABLE_Y = Sea.table.x, Sea.table.y
local FISH_KINDS = 6

-- seaweed out on the sea floor (Layout.sea.weeds), swaying as Animation.sea.weeds says
local WEEDS = {}
for i, w in ipairs(Sea.weeds.list) do
    local s = SeaAnim.weeds.list[i]
    WEEDS[i] = { side = w.side, d = w.d, z = w.z, kind = w.kind, h = w.h, sway = s.sway_deg, period = s.period_s }
end
local WEED_SQUEEZE = Sea.weed_squeeze          -- seaweed drawn narrower than tall

local MAX_FISH, MAX_BUBBLES = Sea.max_fish, Sea.max_bubbles

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
local function fog(z, d) return Tex.clamp01((z - 1) / Sea.fog.z_range + d / Sea.fog.d_range) end

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
    local d = rnd(Fish.d[1], Fish.d[2]) * rnd(Fish.d_spread[1], Fish.d_spread[2])
    local len = rnd(Fish.len[1], Fish.len[2])
    local X = side * (HALF_W + d)
    -- from just off the screen's edge to hidden behind the back wall's corner
    local zNear = (HALF_W + d - len) / Fish.near_half_w
    local zFar = (HALF_W + d + len) / Fish.far_half_w
    local dir = (math.random() < 0.5) and 1 or -1
    local z0, z1 = zNear, zFar
    if dir < 0 then z0, z1 = zFar, zNear end
    local f = {
        kind = have[math.random(#have)], side = side, d = d, X = X, len = len, dir = dir,
        Y = rnd(Fish.y[1], Fish.y[2]), z = Tex.lerp(z0, z1, at or 0), zEnd = z1,
        speed = rnd(FishAnim.speed_px_per_s[1], FishAnim.speed_px_per_s[2]) / F,
        bob = rnd(FishAnim.bob_px[1], FishAnim.bob_px[2]), bobF = rnd(FishAnim.bob_hz[1], FishAnim.bob_hz[2]), ph = rnd(0, 6.283),
    }
    fish[#fish + 1] = f
end

local function spawnBubble(Y)
    local side = (math.random() < 0.5) and -1 or 1
    local d = rnd(Bubble.d[1], Bubble.d[2])
    bubbles[#bubbles + 1] = {
        X = side * (HALF_W + d), d = d, z = rnd(Bubble.z[1], Bubble.z[2]), Y = Y or (KICK_Y + Bubble.rise_from_dy) * (HALF_W + d) / HALF_W,
        vy = rnd(BubbleAnim.rise_px_per_s[1], BubbleAnim.rise_px_per_s[2]), r = rnd(Bubble.r[1], Bubble.r[2]),
        wob = rnd(BubbleAnim.wobble_px[1], BubbleAnim.wobble_px[2]), wobF = rnd(BubbleAnim.wobble_hz[1], BubbleAnim.wobble_hz[2]),
        ph = rnd(0, 6.283), age = 0,
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
    for i = 1, Sea.start_fish do spawnFish(rnd(FishAnim.start_along[1], FishAnim.start_along[2])) end
    for i = 1, Sea.start_bubbles do spawnBubble(rnd(CEIL_Y, KICK_Y)) end
    nextFish = rnd(FishAnim.first_s[1], FishAnim.first_s[2])
    bubbleAcc = 0
    for i = 1, #WEEDS do weedPhase[i] = rnd(0, 6.283) end
end

local function step(dt)
    clock = clock + dt
    if water ~= nil and water.ok then water:update(dt) end

    nextFish = nextFish - dt
    if nextFish <= 0 then
        if #fish < MAX_FISH then spawnFish(0) end
        nextFish = rnd(FishAnim.every_s[1], FishAnim.every_s[2])
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

    bubbleAcc = bubbleAcc + dt * BubbleAnim.per_s
    while bubbleAcc >= 1 do
        bubbleAcc = bubbleAcc - 1
        if #bubbles < MAX_BUBBLES then spawnBubble() end
    end
    i = 1
    while i <= #bubbles do
        local b = bubbles[i]
        b.age = b.age + dt
        b.Y = b.Y - b.vy * dt
        if b.Y < CEIL_Y - Bubble.gone_margin then table.remove(bubbles, i) else i = i + 1 end
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
    tilt = math.max(-Fish.max_tilt_deg, math.min(Fish.max_tilt_deg, tilt)) + FishAnim.tilt_wobble_deg * math.cos(wave)
    local k = (f.len / f.z) / math.max(1, tex.Width - Fish.art_pad)
    local g = fog(f.z, f.d)
    local shade = 1 - Fish.fog_shade * g
    tex:SetScale(right and k or -k, k)
    tex:SetRotation(right and -tilt or tilt)
    tex:SetColor(shade * (1 - Fish.fog_red * g), shade * (1 - Fish.fog_green * g), shade)
    tex:SetOpacity(1 - Fish.fog_fade * g)
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
    local foot = (FOOT_Y - VPY) * w.z
    local x, y = project(X, foot, w.z)
    local k = ((w.h + foot - KICK_Y) / w.z) / tex.Height
    local deg = w.sway * math.sin(2 * math.pi * clock / w.period + weedPhase[i])
    local dx, dy = Tex.turn(0, -tex.Height * k / 2, deg)
    local g = fog(w.z, w.d)
    local shade = 1 - Weed.fog_shade * g
    tex:SetScale((w.side < 0 and k or -k) * WEED_SQUEEZE, k)
    tex:SetRotation(deg)
    tex:SetColor(shade * Weed.red, shade * Weed.green, shade)
    tex:SetOpacity(1 - Weed.fog_fade * g)
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
    local op = math.min(1, b.age / BubbleAnim.fade_in_s) * (1 - Bubble.fog_fade * fog(b.z, b.d)) * Bubble.opacity
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
