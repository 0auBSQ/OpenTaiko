---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- fishtank.lua — the Fish Tank furniture: fish swimming back and forth and kelp swaying inside the
-- tank (flat pictures from Textures/FishTank/ drawn on quads inside the water), and its blue lights,
-- switched on and off by interacting with it (it.lit, saved with the room; off by default).
--
--   FT.init(ctx)         ctx: room(), scene(), propInstFor(it)
--   FT.tick(dt)          moves the fish and the kelp
--   FT.draw()            puts the fish and the kelp of every placed tank in the scene (each frame)
--   FT.lights(world)     the blue lights of the tanks that are on (appended to the room's lights)
--   FT.refresh()         the water's glow of every tank, on or off (after a rebuild or a switch)
--   FT.glowNow(it, mat)  the glow a tank's part has right now (the focus highlight restores it)
--   FT.toggle(it)        switches a tank's lights

local FT = {}

local ID = "fishtank"
local FISH = { "fish_blue", "fish_orange", "fish_pink", "fish_red", "fish_green" }
local KELP = { "seaweed_green_a", "seaweed_green_b", "seaweed_green_c", "seaweed_green_d" }
local FIRST_SPRITE = 4900                -- sprite ids of the fish (and their turned copies) and the kelp
-- the water inside the model, in its own units (Fish Tank by J-Toastie): sides, sand top, water top
local WATER = { x0 = -0.56, x1 = 0.56, y0 = 0.36, y1 = 1.26, z0 = -1.16, z1 = 1.16 }
local FISH_SIZE = 0.3
local LED = { 0.25, 0.55, 1.0 }          -- the colour of the lights
local LED_GLOW = 0.9                     -- how strongly the water glows when they are on

local ctx = nil
local spritesFor = nil                   -- the scene the sprites were registered in
local obj, objScene = nil, nil           -- the scene object holding every tank's fish and kelp
local clock = 0
local schools = {}                       -- placed tank → its fish and kelp
local floor, sin, cos, random = math.floor, math.sin, math.cos, math.random

function FT.init(c) ctx = c end

local function spriteId(i) return FIRST_SPRITE + i end

-- the pictures go into the scene once; a fish has a copy facing the other way (its quad turned)
local function registerSprites(scene)
    if spritesFor == scene then return true end
    local ok = pcall(function()
        for i, n in ipairs(FISH) do
            scene:RegisterSpriteFromTexture(spriteId(i), TEXTURE:CreateTexture("Textures/FishTank/" .. n .. ".png"))
        end
        for i, n in ipairs(KELP) do
            scene:RegisterSpriteFromTexture(spriteId(20 + i), TEXTURE:CreateTexture("Textures/FishTank/" .. n .. ".png"))
        end
    end)
    if ok then spritesFor = scene end
    return ok
end

local function newSchool()
    local s = { fish = {}, kelp = {} }
    for i = 1, 5 do
        s.fish[i] = {
            sprite = ((i - 1) % #FISH) + 1,
            x = WATER.x0 + 0.15 + (WATER.x1 - WATER.x0 - 0.3) * random(),
            y = WATER.y0 + 0.2 + (WATER.y1 - WATER.y0 - 0.35) * random(),
            z = WATER.z0 + 0.2 + (WATER.z1 - WATER.z0 - 0.4) * random(),
            dir = (random() < 0.5) and -1 or 1,
            speed = 0.25 + 0.25 * random(),
            bob = random() * 6.28,
            size = FISH_SIZE * (0.8 + 0.4 * random()),
        }
    end
    for i = 1, 6 do
        s.kelp[i] = {
            sprite = ((i - 1) % #KELP) + 1,
            x = WATER.x0 + 0.1 + (WATER.x1 - WATER.x0 - 0.2) * ((i - 0.5) / 6 + (random() - 0.5) * 0.1),
            z = WATER.z0 + 0.15 + (WATER.z1 - WATER.z0 - 0.3) * random(),
            h = 0.45 + 0.35 * random(),
            phase = random() * 6.28,
            speed = 0.8 + 0.6 * random(),
        }
    end
    return s
end

local function schoolFor(it)
    local s = schools[it]
    if s == nil then s = newSchool(); schools[it] = s end
    return s
end

local function eachTank(fn)
    if ctx == nil then return end
    local room = ctx.room()
    for _, it in ipairs(room and room.furniture or {}) do
        if it.id == ID then
            local inst = ctx.propInstFor(it)
            if inst then fn(it, inst) end
        end
    end
end

function FT.tick(dt)
    clock = clock + dt
    eachTank(function(it)
        for _, f in ipairs(schoolFor(it).fish) do
            f.z = f.z + f.dir * f.speed * dt
            local half = f.size * 0.5
            if f.z > WATER.z1 - half then f.z = WATER.z1 - half; f.dir = -1
            elseif f.z < WATER.z0 + half then f.z = WATER.z0 + half; f.dir = 1 end
            f.bob = f.bob + dt * 2.2
        end
    end)
end

-- a point of the model in the world, through the placed instance's pose (its bounds' bottom centre
-- stands at the instance's position)
local function placer(inst)
    local mnx, mny, mnz, mxx, _, mxz = inst.model:GetBounds()
    local ox, oz = (mnx + mxx) * 0.5, (mnz + mxz) * 0.5
    local yr = (inst.yaw or 0) * math.pi / 180
    local c, s, sc = cos(yr), sin(yr), inst.scale or 1
    return function(lx, ly, lz)
        lx, ly, lz = lx - ox, ly - mny, lz - oz
        return inst.x + sc * (c * lx + s * lz), inst.y + sc * ly, inst.z + sc * (-s * lx + c * lz)
    end
end

function FT.draw()
    if ctx == nil then return end
    local scene = ctx.scene()
    if scene == nil then return end
    if objScene ~= scene then
        obj, objScene = scene:NewObject(), scene
    end
    scene:ObjBegin(obj)
    if not registerSprites(scene) then return end
    eachTank(function(it, inst)
        local ok, at = pcall(placer, inst)
        if not ok then return end
        local s = schoolFor(it)
        -- kelp: rooted in the sand, the top swaying along the tank
        for _, k in ipairs(s.kelp) do
            local sway = 0.08 * sin(clock * k.speed + k.phase)
            local w = k.h * 0.5
            local x0, y0, z0 = at(k.x, WATER.y0, k.z - w * 0.5)
            local x1, y1, z1 = at(k.x, WATER.y0, k.z + w * 0.5)
            local x2, y2, z2 = at(k.x, WATER.y0 + k.h, k.z + w * 0.5 + sway)
            local x3, y3, z3 = at(k.x, WATER.y0 + k.h, k.z - w * 0.5 + sway)
            scene:ObjAddSpriteQuad(obj, x0, y0, z0, x1, y1, z1, x2, y2, z2, x3, y3, z3, spriteId(20 + k.sprite), 1)
        end
        -- fish: the picture faces +z as drawn; one swimming the other way has its quad turned
        for _, f in ipairs(s.fish) do
            local y = f.y + 0.03 * sin(f.bob)
            local hz, hy = f.size * 0.5 * f.dir, f.size * 0.5
            local x0, y0, z0 = at(f.x, y - hy, f.z - hz)
            local x1, y1, z1 = at(f.x, y - hy, f.z + hz)
            local x2, y2, z2 = at(f.x, y + hy, f.z + hz)
            local x3, y3, z3 = at(f.x, y + hy, f.z - hz)
            scene:ObjAddSpriteQuad(obj, x0, y0, z0, x1, y1, z1, x2, y2, z2, x3, y3, z3, spriteId(f.sprite), 1)
        end
    end)
end

local function waterObj(inst)
    for _, p in ipairs(inst.parts or {}) do
        if p.material == "Water" then return p.obj end
    end
    return nil
end

function FT.glowNow(it, material)
    if it ~= nil and it.lit == true and material == "Water" then
        return { LED[1] * LED_GLOW, LED[2] * LED_GLOW, LED[3] * LED_GLOW }
    end
    return { 0, 0, 0 }
end

function FT.refresh()
    if ctx == nil then return end
    local scene = ctx.scene()
    eachTank(function(it, inst)
        local o = waterObj(inst)
        if o and scene and scene.ObjSetEmissive then
            local g = FT.glowNow(it, "Water")
            pcall(function() scene:ObjSetEmissive(o, g[1], g[2], g[3]) end)
        end
    end)
end

function FT.lights(world)
    local out = {}
    eachTank(function(it, inst)
        if it.lit == true then
            local s = inst.scale or 1
            out[#out + 1] = { x = inst.x, y = inst.y + 0.8 * s, z = inst.z, r = LED[1], g = LED[2], b = LED[3],
                              intensity = 0.7, range = 3.5 }
        end
    end)
    return out
end

function FT.toggle(it) it.lit = not (it.lit == true) end

return FT
