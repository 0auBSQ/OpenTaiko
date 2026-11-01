-- Popup explaining an item right after Ume presents it.
-- It closes only after a moment, so the press that ended the dialog does not skip it.

local Easing = require("Easing")
local I18N   = require("i18n")
local assets = require("encounter/assets")
local pill   = require("encounter/ui_pill")
local layout = require("encounter/confirm_layout")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.items.popup
local Anim = Animation.encounter.items.popup

local CX, CY = Look.center.x, Look.center.y      -- panel centre; the panel is 1280 x 800
local NEW_Y, ICON_Y, TITLE_Y, SUB_Y, BODY_Y = Look.new_y, Look.icon_y, Look.title_y, Look.sub_y, Look.body_y
local BODY_W, PARA_GAP = Look.body_w, Look.para_gap_px
local OK_Y = Look.ok_y
local ICON_SCALE = Look.icon_scale
local TITLE_SIZE, BODY_SIZE = Look.title_font_size, Look.body_font_size
local NUDGE = Look.text_nudge_px
local IN_SEC, OUT_SEC, HOLD_SEC = Anim.in_s, Anim.out_s, Anim.hold_s
local IN_FROM, IN_BACK = Anim.in_from, Anim.in_back
local BOB_PX, BOB_HZ = Anim.bob_px, Anim.bob_hz
local HALO_SCALE, HALO_A, HALO_HZ = Look.halo_scale, Look.halo_opacity, Anim.halo_hz
local GLINTS = Look.glints

local _title, _body, _sub, _small = nil, nil, nil, nil
local _gold, _soft, _white, _black = nil, nil, nil, nil
local _tex = {}
local _pillTex = {}
local _text = nil
local _lines = {}                                -- explanation lines and paragraph gaps
local _id = nil
local _state = "idle"                            -- idle | in | up | out
local _t, _k = 0, 0

local function ready(tx) return tx ~= nil and tx.Ready end

local function reset(tx)
    tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
    tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

local function sk(key, fallback)
    local s = THEME:GetSkinString(key)
    if s == nil or s == "" then return fallback end
    return s
end

-- fonts: the stage's glyph fonts (gfont: subtitle, gfontSmall: "New item")
function M.setup(fonts)
    fonts = fonts or {}
    _sub = fonts.gfont
    _small = fonts.gfontSmall or fonts.gfont
    if _title == nil then
        _title = TEXT:CreateGlyphCached(TITLE_SIZE)
        _body = TEXT:CreateGlyphCached(BODY_SIZE)
    end
    if _white == nil then
        _gold = Config.color(Look.gold_color)
        _soft = Config.color(Look.soft_color)
        _white = Config.color(Look.white_color)
        _black = Config.color(Look.outline_color)
    end
end

function M.keys(id)
    return { "items_popup", "item_" .. id, "fx_glow", "fx_glint", "ui_button", "ui_marker" }
end

-- id: the item; text: its encounter/items/texts entry (translated here); encounter/assets opens its keys
function M.open(id, text)
    text = text or {}
    _id, _lines = id, {}
    _text = { name = I18N.tr(text.name), subtitle = I18N.tr(text.subtitle) }
    for _, k in ipairs(M.keys(id)) do _tex[k] = (assets.get(k)) end
    _pillTex = { button = _tex.ui_button, glow = _tex.fx_glow, marker = _tex.ui_marker }
    if _body ~= nil then
        for p, para in ipairs(text.explain or {}) do
            local arr = _body:WrapToLines(I18N.tr(para), BODY_W)
            for i = 0, arr.Length - 1 do
                _lines[#_lines + 1] = { text = arr[i], gap = (i == 0 and p > 1) and PARA_GAP or 0 }
            end
        end
    end
    _state, _t, _k = "in", 0, 0
end

-- returns true once the popup has closed
function M.update(dt, nav)
    if _state == "idle" then return true end
    _t = _t + dt
    if _state == "in" then
        _k = math.min(1, _t / IN_SEC)
        if _t >= IN_SEC then _state = "up" end
    elseif _state == "out" then
        _k = math.max(0, 1 - _t / OUT_SEC)
        if _t >= OUT_SEC then _state = "idle" ; return true end
        return false
    end
    if _state == "up" or _state == "in" then
        if _t >= HOLD_SEC and (nav.decide() or nav.cancel()) then
            SHARED:GetSharedSound("Decide"):Play()
            _state, _t = "out", 0
        end
    end
    return false
end

local function drawAt(tx, x, y, sx, sy, a, c, blend, rot)
    if not ready(tx) or a <= 0.003 then return end
    tx:SetScale(sx, sy)
    if rot ~= nil then tx:SetRotation(rot) end
    tx:SetOpacity(math.min(1, a))
    if c ~= nil then tx:SetColor(c[1], c[2], c[3]) end
    if blend ~= nil then tx:SetBlendMode(blend) end
    tx:DrawAtAnchor(x, y, "center")
    reset(tx)
end

local HALO = Look.halo_color

-- black: the stage's black texture
function M.draw(black)
    if _state == "idle" then return end
    local a = _k
    if ready(black) then
        local res = THEME:GetResolution()
        black:SetScale(res.X / math.max(1, black.Width), res.Y / math.max(1, black.Height))
        black:SetOpacity(layout.DIM * a)
        black:Draw(0, 0)
        reset(black)
    end
    local s = (_state == "in") and (IN_FROM + (1 - IN_FROM) * Easing.outBack(_k, IN_BACK)) or (1 - Anim.out_shrink * (1 - _k))
    local function py(y) return CY + (y - CY) * s end
    local panel = _tex.items_popup
    drawAt(panel, CX, CY, s, s, a)
    local iy = py(ICON_Y) + BOB_PX * math.sin(_t * BOB_HZ * 2 * math.pi)
    local halo = HALO_A * (Anim.halo_pulse_min + Anim.halo_pulse * math.sin(_t * HALO_HZ * 2 * math.pi))
    drawAt(_tex.fx_glow, CX, iy, HALO_SCALE * s, HALO_SCALE * s, halo * a, HALO, "Add")
    for g = 1, GLINTS do
        local ang = g * 2 * math.pi / GLINTS + _t * Anim.glint_orbit_freq
        local tw = 0.5 + 0.5 * math.sin(_t * Anim.glint_twinkle_freq + g * Anim.glint_twinkle_phase)
        local gs = Look.glint_scale * s
        drawAt(_tex.fx_glint, CX + Look.glint_orbit_x_px * s * math.cos(ang), iy + Look.glint_orbit_y_px * s * math.sin(ang), gs, gs,
            tw * a, HALO, "Add", _t * Anim.glint_spin_deg_per_s)
    end
    drawAt(_tex["item_" .. _id], CX, iy, ICON_SCALE * s, ICON_SCALE * s, a)
    local n = NUDGE * s
    if _small ~= nil then _small:Draw(sk("PAGODA_ITEM_NEW", "New item"), CX, py(NEW_Y) + n, _gold, _black, a, s, 0, "center") end
    if _title ~= nil and _text.name ~= nil then _title:Draw(_text.name, CX, py(TITLE_Y) + n, _gold, _black, a, s, 0, "center") end
    if _sub ~= nil and _text.subtitle ~= nil then _sub:Draw(_text.subtitle, CX, py(SUB_Y) + n, _soft, _black, a, s, 0, "center") end
    if _body ~= nil then
        local y = BODY_Y
        local lh = _body.LineHeight
        for _, l in ipairs(_lines) do
            y = y + l.gap
            _body:Draw(l.text, CX, py(y) + n, _white, _black, a, s, 0, "center")
            y = y + lh
        end
    end
    local ps = pill.draw(_pillTex, CX, py(OK_Y), layout.BUTTON_SCALE * s, true, _t, a)
    if _sub ~= nil then
        _sub:Draw(sk("PAGODA_OK", "OK"), CX, py(OK_Y) + NUDGE * ps, pill.labelColor(true), _black, a, ps,
            pill.BASE_W * ps, "center")
    end
end

function M.close()
    _state, _id, _text, _lines, _tex, _pillTex = "idle", nil, nil, {}, {}, {}
end

function M.dispose()
    M.close()
    if _title ~= nil then _title:Dispose() ; _title = nil end
    if _body ~= nil then _body:Dispose() ; _body = nil end
end

return M
