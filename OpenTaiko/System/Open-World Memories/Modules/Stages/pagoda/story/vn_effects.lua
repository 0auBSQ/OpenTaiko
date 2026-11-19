-- encounter band effects for VN backgrounds with a level

local assets = require("encounter/assets")
local bands  = require("encounter/bands")
local view   = require("encounter/bands/view")
local skybox = require("skybox")

local M = {}

local _held = {}            -- keys opened here in encounter/assets
local _seen = {}            -- key -> handle at prepare (false: none)
local _band, _variant, _level, _ctx = nil, nil, nil, nil

-- returns band, variant, ctx; ctx.highest = def.level (as beaten)
local function bandOf(def)
    if def == nil or def.level == nil then return nil end
    local ctx = { highest = def.level }
    local band, variant = bands.forLevel(def.level, ctx)
    return band, variant, ctx
end

-- opens unheld keys, owned here
local function hold(keys)
    local list = {}
    for _, k in ipairs(keys) do
        if assets.get(k) == nil then
            list[#list + 1] = k
            _held[k] = true
        end
    end
    if #list > 0 then assets.open(list) end
    for _, k in ipairs(keys) do _seen[k] = assets.get(k) or false end
end

-- starts band texture loads; a layer with the same asset key draws the same handle
function M.prepare(def)
    local band, variant, ctx = bandOf(def)
    if band == nil then return end
    hold(band.keys(def.level, ctx, variant))
end

local function closeBand()
    if _band == nil then return end
    _band.close()
    _band, _variant, _level, _ctx = nil, nil, nil, nil
    skybox.reset()
end

-- same band and level: kept open
function M.show(def)
    local band, variant, ctx = bandOf(def)
    if band == _band and variant == _variant and (def and def.level) == _level then return end
    closeBand()
    if band == nil then return end
    M.prepare(def)
    skybox.reset()
    band.open(def.level, ctx, variant)
    _band, _variant, _level, _ctx = band, variant, def.level, ctx
end

-- reopens handles released by another screen, then rebuilds the band
local function keep()
    local lost = false
    for k, h in pairs(_seen) do
        if h and assets.get(k) ~= h then lost = true ; break end
    end
    if not lost then return end
    local keys = {}
    for k in pairs(_seen) do keys[#keys + 1] = k end
    table.sort(keys)
    hold(keys)
    if _band ~= nil then
        _band.close()
        _band.open(_level, _ctx, _variant)
    end
end

function M.update(dt)
    keep()
    if _band ~= nil then _band.update(dt) end
end

local function drawBehind(ox, oy)
    _band.drawBehind(ox, oy)
end

local function drawOver(ox, oy)
    _band.drawBack(ox, oy)
    _band.drawMid(ox, oy)
    _band.drawFront(ox, oy)
end

-- under the background layers
function M.drawBehind(ox, oy)
    if _band ~= nil then view.effectsOnly(drawBehind, ox, oy) end
end

-- over the background layers
function M.draw(ox, oy)
    if _band ~= nil then view.effectsOnly(drawOver, ox, oy) end
end

-- also drops textures opened here
function M.close()
    closeBand()
    assets.drop(_held)
    _held, _seen = {}, {}
end

return M
