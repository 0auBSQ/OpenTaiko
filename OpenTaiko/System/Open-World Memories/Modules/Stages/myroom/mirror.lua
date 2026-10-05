---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- mirror.lua — the standing mirror (My Room): who the player walks as, and in which colours.
--
-- The window picks the story student (A or B, used by My Room and the pagoda) and that student's palette
-- among the ones its character offers. Moving over a palette previews it on the turning figure; choosing
-- it wears it. The palette lives on the student character's own counter (Lib/Student), so the customize
-- tab shows the same choice. Locked palettes show their padlock and the plays they need.

local PopUI = require("PopUI")
local I18N = require("i18n")
local Student = require("Student")
local AV = require("avatar")
local T = I18N.texts("mirror")     -- lang/<code>/mirror.json

local M = {}

M.ID = "mirror"

-- ── layout ─────────────────────────────────────────────────────────────────────────────────────
local PANEL = { w = 1240, h = 780 }
local LIST_W, ROW = 600, 60
local CARD_GAP = 40
local CARD_W = 460          -- the preview frame, centred in the column right of the list
local STAGE_PAD = 56        -- space above the figure, and below the hint
local TEXT_H = 100          -- the name and hint under the figure
local TURN = 1.6            -- seconds the figure faces each way
local TURN_ORDER = { 2, 1, 4, 3 }
local GREY = { 120, 126, 150, 255 }

local ctx = nil             -- Script.lua closures (see M.init)
local ui, W = nil, {}
local open = false
local justOpened = false
local student = "a"         -- the student shown (and worn)
local previewIdx = 0        -- the palette under the cursor
local pendingStudent = nil  -- a student switch from the chooser, applied after the UI update
local clock = 0

local function tr(id) return T:tr(id) end
local function playSfx(name) pcall(function() SHARED:GetSharedSound(name):Play() end) end
local function disposeUi(mgr) if mgr then pcall(function() mgr:disposeWidgets() end) end end

local function focusWidget(mgr, w)
    if mgr == nil or w == nil then return end
    for i, f in ipairs(mgr.focusables) do
        if f == w then mgr:_setFocusIndex(i); return end
    end
end

local function save() return ctx and ctx.save() or nil end

local function paletteItems()
    local items = {}
    local worn = Student.index(save(), student)
    for i, p in ipairs(Student.palettes(student)) do
        items[i] = { text = p.name, value = i - 1, mark = (i - 1 == worn) or nil,
                     locked = not Student.unlocked(save(), student, i - 1) or nil }
    end
    return items, worn
end

local function showPalette(idx)
    previewIdx = Student.clamp(student, idx)
    local p = Student.entry(student, previewIdx)
    if W.name then W.name:setText(p.name) end
    if W.hint then
        if Student.unlocked(save(), student, previewIdx) then
            W.hint:setText(previewIdx == Student.index(save(), student) and tr("worn") or "")
        else
            W.hint:setText(string.format(tr("locked"), Student.playCount(save(), student), p.plays))
        end
    end
end

-- refill the palette list for the shown student, the worn palette selected and marked
local function fillList()
    local items, worn = paletteItems()
    local list = W.list
    list.items = {}
    for i, it in ipairs(items) do list.items[i] = { text = it.text, value = it.value, mark = it.mark, locked = it.locked } end
    list.selected = worn + 1
    list._scrollTarget = 0
    list:_ensureVisible()
    list._scrollCur = list._scrollTarget
    showPalette(worn)
end

local function wear(idx)
    local applied = Student.setIndex(save(), student, idx)
    for _, it in ipairs(W.list.items) do it.mark = (it.value == applied) or nil end
    showPalette(applied)
    if ctx.onChanged then ctx.onChanged() end
end

local function build()
    disposeUi(ui)
    W = {}
    ui = PopUI.new{ theme = ctx.theme, navPlayer = ctx.playerIndex() + 1 }
    local x, y = (1920 - PANEL.w) / 2, (1080 - PANEL.h) / 2
    local panel = ui:panel{ x = x, y = y, w = PANEL.w, h = PANEL.h, pad = 30,
                            title = ctx.displayName and ctx.displayName() or M.ID }
    local cx, cy, cw, ch = panel:content()
    ui:label{ x = cx, y = cy + 4, text = tr("student"), size = 22, color = GREY }
    W.chooser = ui:chooser{ x = cx, y = cy + 44, w = LIST_W, h = 64,
        options = { tr("student_a"), tr("student_b") }, index = (student == "b") and 2 or 1,
        onChange = function(i) pendingStudent = (i == 2) and "b" or "a" end }
    ui:label{ x = cx, y = cy + 130, text = tr("palette"), size = 22, color = GREY }
    W.list = ui:menu{ x = cx, y = cy + 170, w = LIST_W, h = ch - 170, rowHeight = ROW, items = {},
        onChange = function(_, it) showPalette(it.value) end,
        onSelect = function(_, it) playSfx("Decide"); wear(it.value); return true end }
    -- the figure is drawn at 1x, the size of the room's sprite frames, so the frame fits it
    local colX, colW = cx + LIST_W + CARD_GAP, cw - LIST_W - CARD_GAP
    local kh = STAGE_PAD * 2 + AV.H + TEXT_H
    local kx, ky = math.floor(colX + (colW - CARD_W) / 2), math.floor(cy + (ch - kh) / 2)
    local mid = kx + math.floor(CARD_W / 2)
    W.card = ui:panel{ x = kx, y = ky, w = CARD_W, h = kh, pad = 24,
                       style = { colors = { surface = { 226, 229, 242, 255 }, surface2 = { 208, 213, 232, 255 } } } }
    W.stage = { x = mid, y = ky + STAGE_PAD + AV.H }
    W.name = ui:label{ x = mid, y = W.stage.y + 18, text = "", size = 30, align = "center", maxWidth = CARD_W - 40 }
    W.hint = ui:label{ x = mid, y = W.stage.y + 72, text = "", size = 22, align = "center", maxWidth = CARD_W - 40,
                       color = GREY }
    fillList()
    focusWidget(ui, W.list)
end

-- ── public API ─────────────────────────────────────────────────────────────────────────────────
-- ctx: { theme, playerIndex()->n, save()->save file|nil, displayName()->string, onChanged()
--        (the worn student or palette changed) }
function M.init(c) ctx = c end

function M.isOpen() return open end

function M.open()
    if open or ctx == nil then return end
    open, justOpened, pendingStudent, clock = true, true, nil, 0
    student = Student.ensure(save())
    AV.frame(student, 2)           -- the preview frames load here, never in draw
    build()
end

function M.close(silent)
    disposeUi(ui)
    ui, W, open, pendingStudent = nil, {}, false, nil
    if not silent then playSfx("Cancel") end
end

-- returns "closed" the frame the window shuts
function M.update(dt, ts)
    if not open then return nil end
    clock = clock + (dt or 0)
    if justOpened then justOpened = false; return nil end
    if ui == nil then M.close(true); return "closed" end
    local r = ui:update(ts)
    if pendingStudent and pendingStudent ~= student then
        student = Student.set(save(), pendingStudent)
        AV.frame(student, 2)
        fillList()
        if ctx.onChanged then ctx.onChanged() end
    end
    pendingStudent = nil
    if r == "cancel" then M.close(); return "closed" end
    return nil
end

function M.draw()
    if not open or ui == nil then return end
    ui:draw()
    local st = W.stage
    if st == nil then return end
    local turn = math.floor(clock / TURN) % #TURN_ORDER + 1
    local walk = math.floor(clock / (AV.WALK_STEP * 1.4)) % AV.WALK_FRAMES + 1
    local tex = AV.frame(student, TURN_ORDER[turn], walk)
    if tex == nil then return end
    Student.withPalette(student, previewIdx, function() tex:DrawAtAnchor(st.x, st.y, "bottom") end)
end

function M.dispose() M.close(true) end

return M
