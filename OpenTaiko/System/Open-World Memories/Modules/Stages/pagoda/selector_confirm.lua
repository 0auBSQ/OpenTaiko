-- Floor confirmation dialog, in the encounter menu's look and layout.

local Easing  = require("Easing")
local pill    = require("encounter/ui_pill")
local layout  = require("encounter/confirm_layout")
local Config  = require("Core.Config")
local Layout  = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local metrics = Layout.selectui.metrics
local Look    = Layout.stage.confirm
local Time    = Animation.stage.confirm

local UI = "Textures/encounter/ui/"
local GLOW_PATH = "Textures/encounter/fx/glow_soft.png"

local OPEN_SEC, CLOSE_SEC = Time.open_s, Time.close_s
local OPEN_FROM   = Time.open_from_scale    -- panel scale at the start of the fade-in
local ACCEPT_FADE = Time.accept_fade_s      -- Yes before the story: fade to black, then hand over
local HANDOFF_SEC = CLOSE_SEC               -- Yes before song_loading: the panel's ease-out over the dim

local _tex, _fonts = nil, nil
local _ui = nil                             -- panel, button, marker, glow
local _white, _black = nil, nil
local _c = nil                              -- dialog state, nil while inactive
local _covered = nil                        -- opacity of the black left up after Yes
local _t = 0                                -- seconds, for the hovered option's bounce

local function _sk(key, fallback)
    local s = THEME:GetSkinString(key)
    if s == nil or s == "" then return fallback end
    return s
end

function M.setup(tex, fonts)
    _tex, _fonts = tex, fonts
    if _white == nil then
        _white = Config.color(Look.text_color)
        _black = Config.color(Look.text_edge_color)
    end
    if _ui ~= nil then return end
    _ui = {
        panel  = TEXTURE:CreateTexture(UI .. "panel.png"),
        button = TEXTURE:CreateTexture(UI .. "button.png"),
        marker = TEXTURE:CreateTexture(UI .. "marker.png"),
        glow   = TEXTURE:CreateTexture(GLOW_PATH),
    }
end

function M.dispose()
    if _ui == nil then return end
    for _, t in pairs(_ui) do t:Dispose() end
    _ui = nil
end

function M.active() return _c ~= nil end

-- toBlack: the next screen fades in from black (the story); else it cross-fades over this one (song_loading)
function M.enter(level, toBlack)
    _c = { level = level, t = 0, sel = 1, closing = false, close_t = 0, accepted = false, accept_t = 0,
           toBlack = toBlack == true }
end

function M.reset()
    _c = nil
    _covered = nil
end

-- returns nil | { sel = level } (Yes accepted; its last frame holds until reset()); No/Cancel ease it out
function M.update(dt, nav)
    local c = _c
    _t = _t + dt
    -- after Yes, ease into the next screen's first frame (all black, or the dim without the panel)
    if c.accepted then
        c.accept_t = c.accept_t + dt
        if c.accept_t >= (c.toBlack and ACCEPT_FADE or HANDOFF_SEC) then
            local lvl = c.level
            _c = nil
            _covered = c.toBlack and 1 or layout.DIM
            return { sel = lvl }
        end
        return nil
    end
    if c.closing then
        c.close_t = c.close_t + dt
        if c.close_t >= CLOSE_SEC then _c = nil end
        return nil
    end
    c.t = c.t + dt
    if c.t < OPEN_SEC then return nil end
    if nav.left()  or nav.upOrPadLeft()    then c.sel = 1 ; SHARED:GetSharedSound("Move"):Play() end
    if nav.right() or nav.downOrPadRight() then c.sel = 2 ; SHARED:GetSharedSound("Move"):Play() end
    if nav.decide() then
        if c.sel == 1 then
            SHARED:GetSharedSound("SongDecide"):Play()
            c.accepted = true ; c.accept_t = 0
        else
            c.closing = true ; c.close_t = 0 ; SHARED:GetSharedSound("Cancel"):Play()
        end
    elseif nav.cancel() then
        c.closing = true ; c.close_t = 0 ; SHARED:GetSharedSound("Cancel"):Play()
    end
    return nil
end

-- draw
local function ready(tx) return tx ~= nil and tx.Ready end

local function reset(tx)
    tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
    tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

-- 0..1: how far the panel is in (opening, open, closing, handing over under the dim)
local function presence(c)
    if c.closing then return 1 - Easing.outCubic(c.close_t / CLOSE_SEC) end
    if c.accepted and not c.toBlack then return 1 - Easing.outCubic(c.accept_t / HANDOFF_SEC) end
    return Easing.outCubic(c.t / OPEN_SEC)
end

local function fill_screen(alpha)
    local b = _tex ~= nil and _tex.black1 or nil
    if not ready(b) or alpha <= 0 then return end
    local res = THEME:GetResolution()
    b:SetScale(res.X / math.max(1, b.Width), res.Y / math.max(1, b.Height))
    b:SetOpacity(alpha)
    b:Draw(0, 0)
    reset(b)
end

-- text centred on (x, y) by its capitals, at scale s and squeezed to maxW
local function draw_text(tx, x, y, s, maxW, alpha)
    if not ready(tx) then return end
    local sq = math.min(1, maxW / math.max(1, tx.Width))
    tx:SetScale(s * sq, s)
    tx:SetOpacity(alpha)
    tx:DrawAtAnchor(x, y + metrics.label.nudge * s, "center")
    reset(tx)
end

-- one answer at (x, y) and scale `scale`; alpha fades the whole of it
local function draw_button(x, y, scale, label, selected, alpha)
    local s = pill.draw(_ui, x, y, scale, selected, _t, alpha)
    local t = _fonts.name:GetText(label, false, pill.BASE_W, pill.labelColor(selected), _black)
    draw_text(t, x, y, s, pill.BASE_W, alpha)
end

function M.draw()
    if _covered ~= nil then fill_screen(_covered) return end
    local c = _c
    if c == nil or _ui == nil then return end
    local k = presence(c)
    -- after Yes the dim holds while the panel leaves
    fill_screen(c.accepted and layout.DIM or layout.DIM * k)

    -- scaled about the panel centre while fading in
    local px, py = layout.PANEL_X, layout.PANEL_Y
    local g = OPEN_FROM + (1 - OPEN_FROM) * k
    local function at(x, y) return px + (x - px) * g, py + (y - py) * g end
    if ready(_ui.panel) then
        _ui.panel:SetScale(g, g)
        _ui.panel:SetOpacity(k)
        _ui.panel:DrawAtAnchor(px, py, "center")
        reset(_ui.panel)
    end
    local q = _fonts.name:GetText(_sk("PAGODA_ACCEPT", "Accept the Challenge?"), false, layout.QUESTION_W, _white, _black)
    local qx, qy = at(px, layout.QUESTION_Y)
    draw_text(q, qx, qy, g, layout.QUESTION_W, k)
    local answers = { _sk("PAGODA_YES", "Yes"), _sk("PAGODA_NO", "No") }
    for i = 1, 2 do
        local bx, by = at(layout.BUTTON_X[i], layout.BUTTON_Y)
        draw_button(bx, by, layout.BUTTON_SCALE * g, answers[i], i == c.sel, k)
    end

    -- Yes before the story: fade to black before the handover
    if c.accepted and c.toBlack then fill_screen(math.min(1, c.accept_t / ACCEPT_FADE)) end
end

return M
