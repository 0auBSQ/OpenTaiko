-- Encounter Leave / Start menu and leave confirmation; the focus also covers the item icons.

local pill   = require("encounter/ui_pill")
local layout = require("encounter/confirm_layout")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Look = Layout.encounter.menu

local M = {}

local PILL_Y     = Look.pill_y
local PILL_X     = Look.pill_x           -- 1 = Leave, 2 = Start
local TEXT_NUDGE = Look.text_nudge_px    -- centre-anchored text sits this far below the box centre
local LINE_GAP   = Look.line_gap_px      -- extra px between the confirm question's lines
local QUESTION_PAD = Look.question_pad_px

local _font = nil
local _white, _black = nil, nil

local _slots = { "leave", "start" }     -- focus order: the items, then leave, start
local _sel = 2
local _confirm = false
local _confirmSel = 2
local _question = {}                     -- the confirm question's lines
local _t = 0

local function sk(key, fallback)
    local s = THEME:GetSkinString(key)
    if s == nil or s == "" then return fallback end
    return s
end

function M.setup(fonts)
    _font = fonts ~= nil and fonts.gfont or nil
    if _white == nil then
        _white = Config.color(Look.text_color)
        _black = Config.color(Look.outline_color)
    end
end

function M.keys() return { "ui_button", "ui_marker", "ui_panel", "fx_glow" } end

function M.open()
    _slots = { "leave", "start" }
    _sel, _confirm, _confirmSel, _t = 2, false, 2, 0
end

-- ids: items left of Leave; the focus stays where it was
function M.setItems(ids)
    local was = _slots[_sel]
    _slots = {}
    for _, id in ipairs(ids or {}) do _slots[#_slots + 1] = id end
    _slots[#_slots + 1] = "leave"
    _slots[#_slots + 1] = "start"
    _sel = #_slots
    for i, s in ipairs(_slots) do if s == was then _sel = i end end
end

function M.confirming() return _confirm end

-- the focused item's id, or nil when the focus is on Leave or Start
function M.focused()
    local s = _slots[_sel]
    if _confirm or s == "leave" or s == "start" then return nil end
    return s
end

local function toggled(nav)
    return nav.upOrPadLeft() or nav.downOrPadRight() or nav.left() or nav.right()
end

-- -1 (left), 1 (right) or 0
local function step(nav)
    local back = nav.upOrPadLeft() or nav.left()
    local fwd = nav.downOrPadRight() or nav.right()
    if back == fwd then return 0 end
    return back and -1 or 1
end

-- wraps the confirm question once per opening
local function openConfirm()
    _confirm, _confirmSel = true, 2
    _question = {}
    local text = sk("PAGODA_LEAVE_CONFIRM", "Leave the pagoda? All progress beyond the checkpoints you have discovered will be lost")
    if _font == nil then return end
    local arr = _font:WrapToLines(text, layout.QUESTION_W - 2 * QUESTION_PAD)
    for i = 0, arr.Length - 1 do _question[i + 1] = arr[i] end
end

-- returns nil | "start" | "leave" | "item", id (the item picked)
function M.update(dt, nav)
    _t = _t + dt
    if not _confirm then
        local d = step(nav)
        if d ~= 0 then
            _sel = (_sel - 1 + d) % #_slots + 1
            SHARED:GetSharedSound("Move"):Play()
        end
        if nav.decide() then
            local s = _slots[_sel]
            if s == "start" then
                SHARED:GetSharedSound("SongDecide"):Play()
                return "start"
            elseif s == "leave" then
                SHARED:GetSharedSound("Decide"):Play()
                openConfirm()
            else
                return "item", s
            end
        elseif nav.cancel() then
            SHARED:GetSharedSound("Cancel"):Play()
            openConfirm()
        end
        return nil
    end
    if toggled(nav) then
        _confirmSel = (_confirmSel == 1) and 2 or 1
        SHARED:GetSharedSound("Move"):Play()
    end
    if nav.decide() then
        SHARED:GetSharedSound("Cancel"):Play()
        if _confirmSel == 1 then return "leave" end
        _confirm = false
    elseif nav.cancel() then
        SHARED:GetSharedSound("Cancel"):Play()
        _confirm = false
    end
    return nil
end

local function ready(tx) return tx ~= nil and tx.Ready end

local function reset(tx)
    tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
    tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

local function drawPill(tex, x, y, scale, label, selected)
    local s = pill.draw(tex, x, y, scale, selected, _t)
    if _font ~= nil then
        _font:Draw(label, x, y + TEXT_NUDGE * s, pill.labelColor(selected), _black,
            1, s, pill.BASE_W * s, "center")
    end
end

local function drawDim(black, a)
    if not ready(black) then return end
    local res = THEME:GetResolution()
    black:SetScale(res.X / math.max(1, black.Width), res.Y / math.max(1, black.Height))
    black:SetOpacity(a)
    black:Draw(0, 0)
    reset(black)
end

-- tex = { button, marker, panel, glow, black }
function M.draw(tex)
    tex = tex or {}
    local labels = { sk("PAGODA_LEAVE", "Leave"), sk("PAGODA_START", "Start") }
    local names = { "leave", "start" }
    for i = 1, 2 do
        drawPill(tex, PILL_X[i], PILL_Y, 1, labels[i], (not _confirm) and _slots[_sel] == names[i])
    end
end

-- drawn over everything while open
function M.drawConfirm(tex)
    if not _confirm then return end
    tex = tex or {}
    drawDim(tex.black, layout.DIM)
    if ready(tex.panel) then
        tex.panel:DrawAtAnchor(layout.PANEL_X, layout.PANEL_Y, "center")
        reset(tex.panel)
    end
    if _font ~= nil and #_question > 0 then
        local lh = _font.LineHeight + LINE_GAP
        local y0 = layout.QUESTION_Y - (#_question - 1) * lh / 2
        for i, line in ipairs(_question) do
            _font:Draw(line, layout.PANEL_X, y0 + (i - 1) * lh + TEXT_NUDGE, _white, _black, 1, 1,
                layout.QUESTION_W, "center")
        end
    end
    local answers = { sk("PAGODA_YES", "Yes"), sk("PAGODA_NO", "No") }
    for i = 1, 2 do
        drawPill(tex, layout.BUTTON_X[i], layout.BUTTON_Y, layout.BUTTON_SCALE, answers[i], i == _confirmSel)
    end
end

return M
