-- End-credits backgrounds, cross-faded on the roll's clock.
-- scene module: keys(), open() (textures created, not Ready yet), update(dt, u) (u = s since its fade-in),
-- draw(a, c) (opacity, brightness), close(); holes = true when the sky shows through it (then a cover
-- over it and the sky dims it; other scenes dim themselves)

local Easing = require("Easing")
local assets = require("encounter/assets")
local scene  = require("encounter/scene")
local view   = require("encounter/bands/view")
local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local M = {}

assets.add(require("encounter/credits/manifest"))

local Anim = Animation.encounter.credits.backdrop
local XFADE = Anim.crossfade_s

-- at = roll time (s) the scene starts fading in; Anim.scene_at_s is in this order
local SCENES = {
    { mod = require("encounter/credits/hug") },
    { mod = require("encounter/credits/forest") },
    { mod = require("encounter/credits/watch") },
    { mod = require("encounter/credits/chase") },
    { mod = require("encounter/credits/fall") },
    { mod = require("encounter/credits/loading") },
    { mod = require("encounter/credits/beanstalk") },
}
for i, s in ipairs(SCENES) do s.at = Anim.scene_at_s[i] end

local _open = false
local _t = 0
local _opened = nil             -- keys this showing created (the first scene's come with the cutscene)

-- the first scene's keys, loaded with the screen before the credits
function M.keys()
    return SCENES[1].mod.keys()
end

local function allKeys()
    local list, seen = {}, {}
    for _, s in ipairs(SCENES) do
        for _, k in ipairs(s.mod.keys()) do
            if not seen[k] then seen[k] = true ; list[#list + 1] = k end
        end
    end
    return list
end

-- creates the textures not loaded yet and opens every scene
function M.open()
    M.close()
    _opened = {}
    for _, k in ipairs(allKeys()) do
        if assets.get(k) == nil then _opened[#_opened + 1] = k end
    end
    assets.open(_opened)
    for _, s in ipairs(SCENES) do s.mod.open() end
    _t = 0
    _open = true
end

-- returns the scene at roll time t, the one before it and the share of the fade left
local function current(t)
    local i = 1
    for j = 2, #SCENES do
        if t >= SCENES[j].at then i = j end
    end
    local k = (i > 1) and Easing.inOutSine((t - SCENES[i].at) / XFADE) or 1
    if k >= 1 then return i, nil, 1 end
    return i, i - 1, k
end

-- dt = roll seconds since the last frame, t = the roll time
function M.update(dt, t)
    if not _open then return end
    _t = t
    local i, prev = current(t)
    SCENES[i].mod.update(dt, t - SCENES[i].at)
    if prev ~= nil then SCENES[prev].mod.update(dt, t - SCENES[prev].at) end
end

-- returns (grey, visible) for the skybox
function M.sky()
    if not _open then return 0, false end
    local i, prev = current(_t)
    local holes = SCENES[i].mod.holes or (prev ~= nil and SCENES[prev].mod.holes)
    return 0, holes == true
end

-- at full opacity; a scene with holes is dimmed by the cover
local function drawUnder(mod, c)
    if mod.holes then
        mod.draw(1, 1)
        scene.drawBlack(1 - c)
    else
        mod.draw(1, c)
    end
end

-- first, rest = brightness of the first scene and of the others
function M.draw(first, rest)
    if not _open then return end
    view.shake(0, 0)
    local i, prev, k = current(_t)
    local cur = SCENES[i].mod
    local c = (i == 1) and first or rest
    if prev == nil then
        drawUnder(cur, c)
        return
    end
    local old = SCENES[prev].mod
    local cOld = (prev == 1) and first or rest
    if cur.holes or not old.holes then
        drawUnder(cur, c)
        old.draw(1 - k, cOld)
    else
        drawUnder(old, cOld)
        cur.draw(k, c)
    end
end

function M.close()
    if not _open then return end
    _open = false
    for _, s in ipairs(SCENES) do s.mod.close() end
    if _opened ~= nil then assets.drop(_opened) end
    _opened = nil
end

return M
