-- Pagoda title (main_menu): title plaque, best floor plaque and menu buttons.
-- setup() makes the textures, dispose() frees them; draw() only places them.

local I18N    = require("i18n")
local levels  = require("levels")
local slice3  = require("title_ui/slice3")
local shine   = require("title_ui/shine")
local buttons = require("title_ui/buttons")
local Rainbow = require("Rainbow")
local Config  = require("Core.Config")
local Layout  = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look    = Layout.stage.title_screen
local metrics = Layout.title_ui.metrics

local DIR = "Textures/title/"
local PIECES = {
    plaque = { l = "plaque_l.png", m = "plaque_m.png", r = "plaque_r.png" },
    best   = { l = "best_l.png",   m = "best_m.png",   r = "best_r.png" },
    button = { l = "button_l.png", m = "button_m.png", r = "button_r.png" },
    exit   = { l = "exit_l.png",   m = "exit_m.png",   r = "exit_r.png" },
}
local MARKER = "Textures/encounter/ui/marker.png"
local GLOW   = "Textures/encounter/fx/glow_soft.png"
local GLINT  = "Textures/encounter/fx/eye_glint.png"

local TITLE_Y, BEST_Y = Look.title_y_share, Look.best_y_share   -- centres, as shares of the screen height
local MENU_Y = Look.menu_y_shares             -- by menu index: Challenge, Practice, Exit
local MENU_Y_BESTIARY = Look.menu_y_shares_with_bestiary   -- the same with Bestiary (index 4) above Exit
local BESTIARY = 4
local TITLE_FILL = Look.title.fill_tag        -- the title's fill (a GetText colour tag)
local TITLE_EDGE = Look.title.edge_color
local TITLE_MAX_INK = Look.title.max_ink_w    -- a longer title is squeezed to this width
local TITLE_MIN_W = Look.title.min_w          -- the plaque's width under a short title
local SHADOW_DX, SHADOW_DY, SHADOW_A = Look.title.shadow.dx, Look.title.shadow.dy, Look.title.shadow.opacity
local BEST_LIFT = Look.best.lift              -- share of white mixed into the level colours
local ROOF_FROM = 21
local TEXT_PAD = 25                           -- a GetText texture's margin left of the ink and below it

local _tex = nil
local _white, _black, _edge = nil, nil, nil
local _shine = shine.new()
local _t = 0
local _labels, _ys = {}, {}
local _rainbow = { t = 0, anchor = "topleft", sx = 1, sy = 1, cycles = Look.best.rainbow.cycles,
                  speed = Animation.stage.title_screen.rainbow_speed, sat = Look.best.rainbow.sat,
                  val = Look.best.rainbow.val }

local function sk(key, fallback)
    local s = THEME:GetSkinString(key)
    if s == nil or s == "" then return fallback end
    return s
end

local function ready(tx) return tx ~= nil and tx.Ready end

-- the middle of the letters in a GetText texture, from its top, for the game language's font
local function inkMid(m) return Config.optional(m, I18N.code) or m.main end

local function loadPiece(path)
    local tx = TEXTURE:CreateTexture(path)
    tx:SetWrapMode("Edge")
    return tx
end

-- once per activation; restarts the shine and the clock
function M.setup()
    _shine, _t = shine.new(), 0
    if _tex ~= nil then return end
    _tex = {}
    for key, files in pairs(PIECES) do
        _tex[key] = { l = loadPiece(DIR .. files.l), m = loadPiece(DIR .. files.m), r = loadPiece(DIR .. files.r) }
    end
    _tex.marker = TEXTURE:CreateTexture(MARKER)
    _tex.glow = TEXTURE:CreateTexture(GLOW)
    _tex.glint = TEXTURE:CreateTexture(GLINT)
    _white = Config.color(Look.text_color)
    _black = Config.color(Look.text_edge_color)
    _edge  = COLOR:CreateColorFromRGBA(TITLE_EDGE[1], TITLE_EDGE[2], TITLE_EDGE[3], 255)
end

function M.dispose()
    if _tex == nil then return end
    for key in pairs(PIECES) do
        for _, tx in pairs(_tex[key]) do tx:Dispose() end
    end
    _tex.marker:Dispose() ; _tex.glow:Dispose() ; _tex.glint:Dispose()
    _tex = nil
end

local function drawTitle(font, x, y, dt)
    local title = sk("PAGODA_TITLE", "Pagoda of the Unknown")
    local fill = font:GetText(TITLE_FILL .. title .. "</g>", false, 99999, _white, _edge)
    local mask = font:GetText(title, false, 99999, _white, _black)
    if not ready(fill) then return end
    local ink = fill.Width - 2 * TEXT_PAD
    local f = math.min(1, TITLE_MAX_INK / math.max(1, ink))
    slice3.draw(_tex.plaque, x, y, math.max(TITLE_MIN_W, slice3.width(metrics.plaque, ink * f)))
    local tx = math.floor(x - (TEXT_PAD + ink / 2) * f + 0.5)
    local ty = math.floor(y - inkMid(metrics.hero) + 0.5)
    fill:SetScale(f, 1)
    fill:SetColor(0, 0, 0)
    fill:SetOpacity(SHADOW_A)
    fill:Draw(tx + SHADOW_DX, ty + SHADOW_DY)
    fill:SetColor(1, 1, 1)
    fill:SetOpacity(1)
    fill:Draw(tx, ty)
    fill:SetScale(1, 1)
    shine.draw(_shine, dt, mask, tx, ty, f, 0, mask.Height - TEXT_PAD, _tex.glint)
end

local function bestColour(level)
    if level >= ROOF_FROM then return 1, 1, 1 end
    local c = levels.level_tick_color(level)
    local k = BEST_LIFT
    return (c[1] + (255 - c[1]) * k) / 255, (c[2] + (255 - c[2]) * k) / 255, (c[3] + (255 - c[3]) * k) / 255
end

local function drawBest(font, highest, x, y)
    -- one pattern: the skin's "Best", the floor name, the floor number
    local text = I18N.trf("%s:  %s  (Lv.%d)", sk("PAGODA_BEST", "Best"), levels.level_name(highest), highest)
    local tex = font:GetText(text, false, 99999, _white, _black)
    if not ready(tex) then return end
    local ink = tex.Width - 2 * TEXT_PAD
    slice3.draw(_tex.best, x, y, slice3.width(metrics.best, ink))
    local tx = math.floor(x - TEXT_PAD - ink / 2 + 0.5)
    local ty = math.floor(y - inkMid(metrics.body) + 0.5)
    if highest >= ROOF_FROM then
        _rainbow.t = _t
        Rainbow.draw(tex, tx, ty, _rainbow)
    else
        tex:SetColor(bestColour(highest))
        tex:Draw(tx, ty)
        tex:SetColor(1, 1, 1)
    end
end

-- sel: the selected menu index; items: the indices shown (pagoda's menu rules); highest: the best
-- level reached; heroFont / bodyFont: the title's and the labels' TEXT fonts
function M.draw(sel, items, highest, heroFont, bodyFont)
    if _tex == nil or heroFont == nil or bodyFont == nil then return end
    local dt = fps.deltaTime
    _t = _t + dt
    local res = THEME:GetResolution()
    local cx = math.floor(res.X / 2)
    drawTitle(heroFont, cx, math.floor(res.Y * TITLE_Y + 0.5), dt)
    drawBest(bodyFont, highest, cx, math.floor(res.Y * BEST_Y + 0.5))

    _labels[1] = bodyFont:GetText(sk("PAGODA_CHALLENGE", "Challenge"), false, 99999, _white, _black)
    _labels[2] = bodyFont:GetText(sk("PAGODA_PRACTICE", "Practice"), false, 99999, _white, _black)
    _labels[3] = bodyFont:GetText(sk("PAGODA_EXIT", "Exit"), false, 99999, _white, _black)
    local shares = MENU_Y
    for _, i in ipairs(items) do if i == BESTIARY then shares = MENU_Y_BESTIARY end end
    _labels[BESTIARY] = (shares == MENU_Y_BESTIARY)
        and bodyFont:GetText(sk("PAGODA_BESTIARY", "Bestiary"), false, 99999, _white, _black) or nil
    for i, share in ipairs(shares) do _ys[i] = math.floor(res.Y * share + 0.5) end
    buttons.draw(items, sel, _labels, _ys, cx, buttons.width(_labels), inkMid(metrics.body), _t, _tex)
end

return M
