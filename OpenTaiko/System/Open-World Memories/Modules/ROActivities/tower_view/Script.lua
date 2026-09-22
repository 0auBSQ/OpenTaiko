---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- tower_view ROActivity
-- ------------------------------------------------------------------------------------------------------
-- The tower standing in its panorama (Textures/Panorama.png, a tall sky-to-ground view), composed from the
-- pieces of the chart's tower look by Lib/TowerArt.lua, with the chart's floor count. The loading screen
-- climbs it from the ground to the sky and the tower result comes back down; both hand over the scroll:
--   activate(look, floors)   look = the chart's TowerType (nil for the default look), floors = its floor count
--   draw(scroll, opacity)    scroll 0 = the ground, 1 = the sky; opacity 0..1
--   deactivate()
-- The panorama and the pieces load asynchronously at activate, so they pop in a frame or two later; the
-- callers fade the view in from nothing anyway.

local TowerArt = require("TowerArt")

local TOWER_X = 960          -- the tower's centre and the ground line, in panorama pixels
local TOWER_GROUND_Y = 1580
local TOWER_SCALE = 0.4

local panorama = nil
local art = nil
local look = nil
local floors = 0
local res_w, res_h = 1920, 1080

function onStart()
    local res = THEME:GetResolution()
    res_w, res_h = res.X, res.Y
end

function activate(which, count)
    art = TowerArt.load()
    look = art:resolve(which)
    floors = math.max(0, math.floor(tonumber(count) or 0))
    panorama = TEXTURE:CreateTexture("Textures/Panorama.png")
    art:preload(look)
end

function deactivate()
    if panorama ~= nil then panorama:Dispose(); panorama = nil end
    if art ~= nil then art:dispose(); art = nil end
end

function update() end

function draw(scroll, opacity)
    if panorama == nil or art == nil then return end
    scroll = math.max(0, math.min(1, tonumber(scroll) or 0))
    opacity = tonumber(opacity) or 1
    local span = math.max(0, panorama.Height - res_h)
    local offset = (1 - scroll) * span      -- the panorama rows hidden above the screen
    panorama:SetOpacity(opacity)
    panorama:Draw(0, -offset)
    art:draw(look, TOWER_X, TOWER_GROUND_Y - offset, TOWER_SCALE, floors,
        { opacity = opacity, clipTop = 0, clipBottom = res_h })
end

function afterSongEnum() end

function onDestroy()
    deactivate()
end
