-- Floor 20 band: the level-20 cutscene's storm (encounter/cutscenes/storm.lua, silent) behind Tobiraph.

local assets = require("encounter/assets")
local view   = require("encounter/bands/view")
local Storm  = require("encounter/cutscenes/storm")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local SETTLED = Animation.play_bg.storm.settle_s
local BOLT_X = Config.plain(Layout.play_bg.storm.bolt_x)
local H = Layout.play_bg.view.h

local _storm = nil
local _scale = 1

function M.keys(ctx)
    return { "up_storm", "cut20_cloud", "cut20_bolt", "cut20_rain", "cut20_white" }
end

function M.open(ctx)
    -- the storm places through the encounter view; reset so it is settled and unshaken
    view.reset()
    view.update(SETTLED)
    view.shake(0, 0)
    local data = assets.data("storm") or {}
    _scale = data.scale or 1
    local _, cloud = assets.get("cut20_cloud")
    _storm = Storm.new({ rain = (assets.get("cut20_rain")), bolt = (assets.get("cut20_bolt")),
                         white = (assets.get("cut20_white")), cloud = (assets.get("cut20_cloud")) },
                       cloud and { w = cloud.w, y = data.cloud_y or 0, scale = data.cloud_scale or 1 }, {}, H, BOLT_X)
end

function M.filter() return 1, 1, 1 end
function M.eyes() return 0 end
M.shadow = false

function M.update(dt)
    if _storm ~= nil then _storm:update(dt) end
end

function M.drawBack()
    local bg = assets.get("up_storm")
    if bg == nil or not bg.Ready then return end
    bg:SetScale(_scale, _scale)
    bg:Draw(0, 0)
    bg:SetScale(1, 1)
    if _storm ~= nil then _storm:drawBack() end
end

function M.drawFront()
    if _storm == nil then return end
    _storm:drawRain(1, 0, 0)
    _storm:drawRain(2, 0, 0)
    _storm:drawFlash()
end

return M
