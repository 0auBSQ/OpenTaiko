-- Focused item's name and description panel over the icons.

local I18N = require("i18n")
local pill = require("encounter/ui_pill")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.items.tooltip
local Anim = Animation.encounter.items.tooltip

local TIP_X, TIP_BOTTOM = Look.x, Look.bottom   -- panel left edge and bottom
local PAD_X, NAME_Y, DESC_Y = Look.pad_x, Look.name_y, Look.desc_y   -- text left inset; name and first line centre y
local DESC_W = Look.desc_w
local NUDGE = Look.text_nudge_px                -- glyph box centre is this far below its ink centre
local GLYPH_PAD = 25                            -- glyph box starts this far left of its ink
local FADE_SEC, RISE = Anim.fade_s, Anim.rise_px

local _name, _desc = nil, nil                   -- glyph fonts
local _white, _black = nil, nil
local _lines = {}                               -- id -> wrapped description lines
local _names = {}                               -- id -> item name
local _texts = {}
local _id, _a = nil, 0
local _tex = nil

-- fonts: the stage's glyph fonts (gfontName: name, gfontSmall: description)
function M.setup(fonts)
    fonts = fonts or {}
    _name = fonts.gfontName or fonts.gfont
    _desc = fonts.gfontSmall or fonts.gfont
    if _white == nil then
        _white = Config.color(Look.text_color)
        _black = Config.color(Look.outline_color)
    end
end

function M.keys() return { "items_tip" } end

-- texts: encounter/items/texts (translated here); tex: the panel's handle
function M.open(texts, tex)
    _texts, _tex, _lines, _names = texts or {}, tex, {}, {}
    _id, _a = nil, 0
    for id, t in pairs(_texts) do _names[id] = I18N.tr(t.name) end
    if _desc == nil then return end
    for id, t in pairs(_texts) do
        local arr = _desc:WrapToLines(I18N.tr(t.hint or ""), DESC_W)
        local lines = {}
        for i = 0, arr.Length - 1 do lines[i + 1] = arr[i] end
        _lines[id] = lines
    end
end

-- focus: the focused item's id or nil
function M.update(dt, focus)
    if focus ~= _id then
        -- the old description fades out before the new one comes in
        _a = math.max(0, _a - dt / FADE_SEC)
        if _a <= 0 then _id = focus end
    elseif _id ~= nil then
        _a = math.min(1, _a + dt / FADE_SEC)
    end
end

function M.draw()
    if _id == nil or _a <= 0 or _tex == nil or not _tex.Ready then return end
    local name = _names[_id]
    if name == nil then return end
    local k = 1 - (1 - _a) * (1 - _a)
    local top = TIP_BOTTOM - _tex.Height + RISE * (1 - k)
    _tex:SetOpacity(k)
    _tex:Draw(TIP_X, top)
    _tex:SetOpacity(1)
    local x = TIP_X + PAD_X - GLYPH_PAD
    if _name ~= nil then _name:Draw(name, x, top + NAME_Y + NUDGE, pill.labelColor(true), _black, k, 1, 0, "left") end
    if _desc ~= nil then
        local lh = _desc.LineHeight
        for i, line in ipairs(_lines[_id] or {}) do
            _desc:Draw(line, x, top + DESC_Y + (i - 1) * lh + NUDGE, _white, _black, k, 1, 0, "left")
        end
    end
end

function M.close()
    _id, _a, _tex, _names = nil, 0, nil, {}
end

return M
