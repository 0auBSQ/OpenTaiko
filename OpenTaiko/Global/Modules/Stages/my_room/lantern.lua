---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- lantern.lua — the Stone Lantern furniture: a small fire burning in its light box (the flame pictures
-- of Textures/StoneLantern/ played in turn on an upright sprite facing the camera, over a soft glow
-- filling the box) and its warm light, lit and put out by interacting with it (it.lit, saved with the
-- room; out by default).
--
--   LT.init(ctx)         ctx: room(), scene(), propInstFor(it)
--   LT.tick(dt)          moves the flames on
--   LT.draw()            puts the fire of every lit lantern in the scene (each frame)
--   LT.lights(world)     the light of the lanterns that are lit (appended to the room's lights)
--   LT.toggle(it)        lights or puts out a lantern

local LT = {}

local ID = "stone_lantern"
local FRAMES = 6                         -- Textures/StoneLantern/fire_1.png .. fire_6.png
local FPS = 10
local FIRST_SPRITE = 4960                -- sprite ids of the flame pictures, then the glow
local GLOW_SPRITE = FIRST_SPRITE + FRAMES + 1
-- the light box inside the model, in its own units (Japanese Stone Lamp by Flopsi): the centre of its
-- floor, the size of the flame standing on it, and of the glow around it (the box's opening; it starts
-- under the floor, where the stone hides it, so it sorts behind the flame)
local BOX = { x = 0.0, y = -0.06, z = -0.03 }
local FIRE_W, FIRE_H = 0.3, 0.28              -- the frames are 56x52
local GLOW_W, GLOW_H, GLOW_DROP = 0.5, 0.56, 0.14
local BRIGHT = 1.35                      -- how bright the pictures are drawn
local LIGHT = { 1.0, 0.66, 0.32 }        -- the colour of its light

local ctx = nil
local spritesFor = nil                   -- the scene the sprites were registered in
local obj, objScene = nil, nil           -- the scene object holding every lantern's flame
local clock = 0
local phases = {}                        -- placed lantern → its own offset in the flame loop
local cos, sin, random, floor = math.cos, math.sin, math.random, math.floor

function LT.init(c) ctx = c end

local function registerSprites(scene)
    if spritesFor == scene then return true end
    local ok = pcall(function()
        for i = 1, FRAMES do
            scene:RegisterSpriteFromTexture(FIRST_SPRITE + i, TEXTURE:CreateTexture("Textures/StoneLantern/fire_" .. i .. ".png"))
        end
        scene:RegisterSpriteFromTexture(GLOW_SPRITE, TEXTURE:CreateTexture("Textures/StoneLantern/glow.png"))
    end)
    if ok then spritesFor = scene end
    return ok
end

local function eachLit(fn)
    if ctx == nil then return end
    local room = ctx.room()
    for _, it in ipairs(room and room.furniture or {}) do
        if it.id == ID and it.lit == true then
            local inst = ctx.propInstFor(it)
            if inst then fn(it, inst) end
        end
    end
end

function LT.tick(dt) clock = clock + dt end

-- a point of the model in the world, through the placed instance's pose (the instance's position is
-- the model's origin: Inst:setTransform has already moved it off the anchor point)
local function placer(inst)
    local yr = (inst.yaw or 0) * math.pi / 180
    local c, s, sc = cos(yr), sin(yr), inst.scale or 1
    return function(lx, ly, lz)
        return inst.x + sc * (c * lx + s * lz), inst.y + sc * ly, inst.z + sc * (-s * lx + c * lz)
    end, sc
end

function LT.draw()
    if ctx == nil then return end
    local scene = ctx.scene()
    if scene == nil then return end
    if objScene ~= scene then
        obj, objScene = scene:NewObject(), scene
        scene:ObjSetPass(obj, 1, 1, 1, 1, 255)
        scene:ObjSetCastShadow(obj, false)
        scene:ObjSetTint(obj, BRIGHT, BRIGHT, BRIGHT)
    end
    scene:ObjBegin(obj)
    if not registerSprites(scene) then return end
    eachLit(function(it, inst)
        local ok, at, sc = pcall(placer, inst)
        if not ok then return end
        local ph = phases[it]
        if ph == nil then ph = random() * FRAMES; phases[it] = ph end
        local frame = floor(clock * FPS + ph) % FRAMES + 1
        local x, y, z = at(BOX.x, BOX.y, BOX.z)
        local breathe = 1 + 0.05 * sin(clock * 7 + ph * 2.1)
        scene:ObjAddSprite(obj, x, y - GLOW_DROP * sc, z, GLOW_W * sc * breathe, GLOW_H * sc * breathe, GLOW_SPRITE, 1, 0)
        scene:ObjAddSprite(obj, x, y + 0.005, z, FIRE_W * sc, FIRE_H * sc, FIRST_SPRITE + frame, 1, 0)
    end)
end

function LT.lights()
    local out = {}
    eachLit(function(_, inst)
        local ok, at = pcall(placer, inst)
        if ok then
            local x, y, z = at(BOX.x, BOX.y + FIRE_H * 0.5, BOX.z)
            out[#out + 1] = { x = x, y = y, z = z, r = LIGHT[1], g = LIGHT[2], b = LIGHT[3],
                              intensity = 0.6, range = 3.5 }
        end
    end)
    return out
end

function LT.toggle(it) it.lit = not (it.lit == true) end

return LT
