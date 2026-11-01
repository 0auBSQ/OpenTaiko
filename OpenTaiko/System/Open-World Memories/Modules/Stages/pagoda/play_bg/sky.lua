-- Band sky in the floor select's colours for an hour, greyed until level 20 is beaten.

local assets = require("encounter/assets")
local skybox = require("skybox")
local Sky3D  = require("OWM3d.sky")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")

local S = {}
S.__index = S

local TOP_ZENITH = Layout.play_bg.sky.top_zenith_share

function S.keys()
    return { "cut20_white", "up_sky_fade" }
end

local function luma(c)
    return 0.299 * c[1] + 0.587 * c[2] + 0.114 * c[3]
end

-- grey ramp colour for c's luminance, as the pagoda sky's gradient map greys it
local function grey(c)
    local stops = skybox.GREY_STOPS
    local l = luma(c)
    local a, b = stops[1], stops[#stops]
    if l <= a[1] then return { a[2] / 255, a[3] / 255, a[4] / 255 } end
    for i = 2, #stops do
        b = stops[i]
        if l <= b[1] then
            a = stops[i - 1]
            local t = (l - a[1]) / (b[1] - a[1])
            return { (a[2] + (b[2] - a[2]) * t) / 255, (a[3] + (b[3] - a[3]) * t) / 255, (a[4] + (b[4] - a[4]) * t) / 255 }
        end
    end
    return { b[2] / 255, b[3] / 255, b[4] / 255 }
end

-- hour: the floor select's hour; greyed: the sky is still grey (level 20 not beaten)
function S.new(hour, greyed)
    local pal = Sky3D.paletteAt(hour)
    local zen, hor = pal.zen, pal.hor
    local top = {}
    for i = 1, 3 do top[i] = hor[i] + (zen[i] - hor[i]) * TOP_ZENITH end
    local bottom = { hor[1], hor[2], hor[3] }
    if greyed then top, bottom = grey(top), grey(bottom) end
    return setmetatable({ top = top, bottom = bottom, star = pal.star, white = (assets.get("cut20_white")),
                          fade = (assets.get("up_sky_fade")) }, S)
end

-- 0..1: how much the stars show at an hour, and at this sky's
function S.starsAt(hour)
    return Sky3D.paletteAt(hour).star or 0
end

function S:stars()
    return self.star or 0
end

-- 0..1: the sky's brightness halfway down the band
function S:luminance()
    return (luma(self.top) + luma(self.bottom)) * 0.5
end

local function fill(tx, w, h, c)
    tx:SetScale(w / tx.Width, h / tx.Height)
    tx:SetColor(c[1], c[2], c[3])
    tx:DrawAtAnchor(0, 0, "topleft")
    tx:SetScale(1, 1)
    tx:SetColor(1, 1, 1)
end

-- over the w x h area from the screen's top-left corner
function S:draw(w, h)
    local white, fade = self.white, self.fade
    if white == nil or not white.Ready or fade == nil or not fade.Ready then return end
    fill(white, w, h, self.top)
    fill(fade, w, h, self.bottom)
end

return S
