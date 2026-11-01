-- Pagoda upper background on the play screen; play_setup passes the floor and run state as shared strings.

local ROOT = "../"      -- the stage folder

-- the stage's modules load from the stage folder, in this background's own VM
package.searchers[#package.searchers + 1] = function(name)
    local path = ROOT .. name:gsub("%.", "/") .. ".lua"
    local src = STORAGE:ReadText(path)
    if src == nil then return "\n\tno file '" .. path .. "'" end
    return assert(load(src, "@" .. path)), path
end

-- every Config.load of this VM reads from the stage folder
local Config = require("Core.Config")
local loadConfig = Config.load
Config.load = function(name) return loadConfig(ROOT .. name) end

local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local assets   = require("encounter/assets")
local hits     = require("hits")
local group    = require("group")
local tobiraph = require("tobiraph")

assets.root = ROOT
assets.add(require("manifest"))

local BANDS = {
    forest = "band_forest", pagoda = "band_pagoda", checkpoint = "band_checkpoint",
    storm = "band_storm", roof = "band_roof",
}
local TOBIRAPH_LEVEL = 20

local Flash = Animation.play_bg.flash
local FLASH_SEC = Flash.duration_s
local FLASH_A = { hit = Flash.hit_opacity, big = Flash.big_opacity, roll = Flash.roll_opacity }
local WHITE = Layout.play_bg.flash.white_gradient
local FLASH_MIN = Layout.play_bg.flash.min_opacity
local MAX_DT = Animation.play_bg.max_frame_dt_s

local _band, _foe, _hits = nil, nil, nil
local _white = nil
local _flashT, _flashA = 0, 0

local function shared(key)
    local s = SHARED:GetSharedString(key)
    if s == nil or s == "" then return nil end
    return s
end

local function bandFor(level)
    if level <= 10 then return BANDS.forest end
    if level == 15 then return BANDS.checkpoint end
    if level < TOBIRAPH_LEVEL then return BANDS.pagoda end
    if level == TOBIRAPH_LEVEL then return BANDS.storm end
    return BANDS.roof
end

local function flash()
    return _flashA * _flashT / FLASH_SEC
end

local function onHit(big, judge)
    local a = FLASH_A.hit
    if big then a = FLASH_A.big
    elseif judge == "roll" then a = FLASH_A.roll end
    _flashA, _flashT = math.max(flash(), a), FLASH_SEC
    _foe:react(big)
end

-- runs while the song loads; the floor's art loads synchronously here
function onStart()
    local ctx = {
        level = math.max(1, math.floor(tonumber(shared("pagoda_play_level") or "") or 1)),
        monster = shared("pagoda_play_monster"),
        highest = tonumber(shared("pagoda_play_highest") or "") or 1,
        practice = shared("pagoda_play_practice") == "1",
    }
    _band = require(bandFor(ctx.level))
    local Foe = (not ctx.practice) and ((ctx.level == TOBIRAPH_LEVEL) and tobiraph or group) or nil
    local keys = _band.keys(ctx)
    if Foe ~= nil then
        for _, k in ipairs(Foe.keys(ctx)) do keys[#keys + 1] = k end
    end
    assets.open(keys, true)
    _band.open(ctx)
    _foe = (Foe ~= nil) and Foe.new(ctx) or nil
end

function activate(state)
    _white = GRADIENT:Create(WHITE)
    _flashT, _flashA = 0, 0
    if _foe == nil then return end
    _hits = hits.new(state)
    state:SetFlyTarget(0, _foe:centre())
end

function update(timestamp, state)
    if _band == nil then return end
    local dt = math.min(MAX_DT, fps.deltaTime)
    _band.update(dt)
    if _foe == nil then return end
    _foe:update(dt, state)
    _flashT = math.max(0, _flashT - dt)
    _hits:poll(state, onHit)
    state:SetFlyTarget(0, _foe:centre())
end

function draw(state)
    if _band == nil then return end
    _band.drawBack()
    if _foe == nil then
        if _band.drawFront ~= nil then _band.drawFront() end
        return
    end
    local r, g, b = _band.filter()
    _foe:draw(1, r, g, b, _band.eyes(), _band.shadow)
    local a = flash()
    if _band.glow ~= nil then
        local moon = _band.glow()
        a = moon + (1 - moon) * a
    end
    if a > FLASH_MIN and _white ~= nil then
        GRADIENT:SetActive(_white)
        local ok, err = pcall(_foe.drawFlash, _foe, a)
        GRADIENT:ClearActive()
        if not ok then error(err, 0) end
    end
    if _band.drawFront ~= nil then _band.drawFront() end
end
