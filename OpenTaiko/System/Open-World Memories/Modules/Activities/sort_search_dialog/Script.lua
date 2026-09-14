---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- sort_search_dialog — the song select's sort and search dialogs, as PopUI panels over the list.
--
-- Sort: one row per method with three buttons, ascending / descending / off, the active one lit; only one
-- method is on at a time. The choice persists per player in the save file's global counters
-- "ss_sort_method" (0-6) and "ss_sort_dir" (0 = off, 1 = ascending, 2 = descending); song_select_core
-- re-sorts when the dialog closes.
-- Search: a difficulty chooser, level sliders (from 0..13, to 0..12 then "13~" = no upper bound), three
-- text fields, a live match count. Confirming
-- hands the parameters to song_select_core through Lib/SongSearch (save counters + shared strings and the
-- ss_search_ready flag), which opens the results as a virtual folder.
--
-- activate(player, mode, baseFolder): player = whose save file; mode = "sort" (default) | "search";
-- baseFolder = the folder the search counts in.

local PopUI = require("PopUI")
local SongSearch = require("SongSearch")

local SW, SH = 1920, 1080
local SORT_METHODS = { "SONGSELECT_SORT_FILEPATH", "SONGSELECT_SORT_SONG_TITLE", "SONGSELECT_SORT_SUBTITLE",
                       "SONGSELECT_SORT_LEVEL", "SONGSELECT_SORT_BPM", "SONGSELECT_SORT_BEST_SCORE", "SONGSELECT_SORT_CLEAR_STATUS" }
local METHOD_KEY, DIR_KEY = "ss_sort_method", "ss_sort_dir"
local DEFAULT_METHOD, DEFAULT_DIR = 0, 1

local UI_THEME = {
    colors = {
        surface  = { 250, 252, 255, 255 }, surface2 = { 230, 236, 244, 255 },
        primary  = { 96, 132, 180, 255 },  primary2 = { 72, 108, 156, 255 },
        accent   = { 126, 196, 200, 255 }, accent2  = { 96, 168, 176, 255 },
        outline  = { 70, 84, 104, 255 },   text = { 44, 56, 74, 255 }, textOnAccent = { 255, 255, 255, 255 },
    },
    font = { small = 18, label = 22, button = 22, title = 30 },
}
local CLOSE_STYLE = {
    radius = 40,
    colors = { primary = { 208, 62, 56, 255 }, primary2 = { 158, 34, 30, 255 },
               outline = { 96, 24, 20, 255 }, textOnAccent = { 255, 255, 255, 255 } },
}
local COL_MUTED = { 108, 114, 146 }

local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback or key
end

local ui = nil
local sounds = {}
local activePlayer, activeMode, activeBaseFolder = 0, "sort", nil
local wantClose, justOpened = false, false
local panel = { x = 0, y = 0, w = 0, h = 0 }
local counter = { text = "", label = nil }
local sortButtons = {}   -- [method 0-6] = { [1] = asc, [2] = desc, [0] = off }

-- ── sort ─────────────────────────────────────────────────────────────────────

local function readSort()
    local sav = GetSaveFile(activePlayer)
    local m = math.floor(sav:GetGlobalCounter(METHOD_KEY) + 0.5)
    local d = math.floor(sav:GetGlobalCounter(DIR_KEY) + 0.5)
    if m < 0 or m >= #SORT_METHODS then m = DEFAULT_METHOD end
    if d < 1 or d > 2 then m, d = DEFAULT_METHOD, DEFAULT_DIR end   -- off (a fresh save) = the file order
    return m, d
end

local function writeSort(m, d)
    if d == 0 then m, d = DEFAULT_METHOD, DEFAULT_DIR end   -- off = back to the file order
    local sav = GetSaveFile(activePlayer)
    sav:SetGlobalCounter(METHOD_KEY, m)
    sav:SetGlobalCounter(DIR_KEY, d)
end

-- every row's three buttons show the saved state: the active method's direction is lit, everything else off
local function refreshSortButtons()
    local m, d = readSort()
    for method, btns in pairs(sortButtons) do
        for dir, btn in pairs(btns) do
            local lit = (method == m and dir == d) or (method ~= m and dir == 0)
            if btn.accent ~= lit then btn.accent = lit; btn:restyle() end
        end
    end
end

local SORT_ROW_H = 66
local function buildSort()
    panel.w, panel.h = 900, 96 + #SORT_METHODS * SORT_ROW_H + 120
    panel.x, panel.y = math.floor((SW - panel.w) / 2), math.floor((SH - panel.h) / 2)
    ui:panel{ x = panel.x, y = panel.y, w = panel.w, h = panel.h, title = tr("SONGSELECT_SORT_TITLE", "Sort songs") }
    -- the choice is saved as it is made; OK just closes the dialog
    ui:button{ text = tr("SONGSELECT_SORT_OK", "OK"), x = panel.x + panel.w / 2 - 140, y = panel.y + panel.h - 90, w = 280, h = 60, accent = true,
               onClick = function() sounds.Decide:Play(); wantClose = true end }
    local bw, bh, gap = 124, 50, 8
    local bx0 = panel.x + panel.w - 40 - 3 * bw - 2 * gap
    sortButtons = {}
    for i, key in ipairs(SORT_METHODS) do
        local y = panel.y + 84 + (i - 1) * SORT_ROW_H
        local method = i - 1
        ui:label{ text = tr(key, key), x = panel.x + 40, y = y + math.floor((bh - 22) / 2), size = "label", color = UI_THEME.colors.text, maxWidth = bx0 - panel.x - 60 }
        sortButtons[method] = {}
        for k, dir in ipairs({ 1, 2, 0 }) do
            local labelKey = (dir == 1) and "SONGSELECT_SORT_ASC" or ((dir == 2) and "SONGSELECT_SORT_DESC" or "SONGSELECT_SORT_OFF")
            local fallback = (dir == 1) and "ascending" or ((dir == 2) and "descending" or "off")
            sortButtons[method][dir] = ui:button{
                text = tr(labelKey, fallback), x = bx0 + (k - 1) * (bw + gap), y = y, w = bw, h = bh, style = { font = { button = 18 } },
                onClick = function() writeSort(method, dir); refreshSortButtons() end,
            }
        end
    end
    refreshSortButtons()
end

-- ── search ───────────────────────────────────────────────────────────────────

local LEVEL_MAX = SongSearch.LEVEL_MAX
local search = { diffIdx = 1, from = 0, to = LEVEL_MAX, title = "", subtitle = "", charter = "" }

local function optionLabel(opt)
    if opt.key ~= nil then return tr(opt.key, opt.label or opt.key) end
    return opt.label
end

-- the sliders: from 0..13; to 0..12, its top value reads "13~" and means no upper bound
local function searchParams()
    return {
        diff = SongSearch.DIFF_OPTIONS[search.diffIdx].value,
        levelFrom = search.from,
        levelTo = search.to, levelToOpenEnd = search.to >= LEVEL_MAX,
        title = search.title, subtitle = search.subtitle, charter = search.charter,
    }
end

-- the live count runs on every change, not every frame (it walks the whole folder)
local function recount()
    local n = SongSearch.count(activeBaseFolder, searchParams())
    counter.text = string.format(tr("SONGSELECT_SEARCH_COUNT", "%d songs found"), n)
    if counter.label then counter.label:setText(counter.text) end
end

local function buildSearch()
    panel.w, panel.h = 940, 96 + 6 * 74 + 170
    panel.x, panel.y = math.floor((SW - panel.w) / 2), math.floor((SH - panel.h) / 2)
    ui:panel{ x = panel.x, y = panel.y, w = panel.w, h = panel.h, title = tr("SONGSELECT_SEARCH_TITLE", "Search songs") }
    local labelX, fieldX, fieldW = panel.x + 40, panel.x + 330, panel.w - 370
    local y = panel.y + 90
    local function row(labelKey, fallback)
        ui:label{ text = tr(labelKey, fallback), x = labelX, y = y + 20, size = "label", color = UI_THEME.colors.text, maxWidth = 270 }
    end
    local function labels(opts)
        local out = {}
        for i, o in ipairs(opts) do out[i] = optionLabel(o) end
        return out
    end
    row("SONGSELECT_SEARCH_DIFFICULTY", "Difficulty")
    ui:chooser{ x = fieldX, y = y, w = fieldW, h = 60, options = labels(SongSearch.DIFF_OPTIONS), index = search.diffIdx, wrap = true,
                onChange = function(i) search.diffIdx = i; recount() end }
    y = y + 74
    row("SONGSELECT_SEARCH_LEVEL_FROM", "Level from")
    ui:slider{ x = fieldX, y = y + 10, w = fieldW - 90, h = 40, min = 0, max = LEVEL_MAX, step = 1, value = search.from,
               onChange = function(v) search.from = math.floor(v + 0.5); recount() end }
    y = y + 74
    row("SONGSELECT_SEARCH_LEVEL_TO", "Level to")
    ui:slider{ x = fieldX, y = y + 10, w = fieldW - 90, h = 40, min = 0, max = LEVEL_MAX, step = 1, value = search.to,
               showValue = function(self) local v = math.floor(self.value + 0.5); return (v >= LEVEL_MAX) and (LEVEL_MAX .. "~") or tostring(v) end,
               onChange = function(v) search.to = math.floor(v + 0.5); recount() end }
    y = y + 74
    local any = tr("SONGSELECT_SEARCH_ANY", "Any")
    for _, f in ipairs({ { "SONGSELECT_SEARCH_SONG_TITLE", "Song title", "title" }, { "SONGSELECT_SEARCH_SUBTITLE", "Song subtitle", "subtitle" },
                         { "SONGSELECT_SEARCH_CHARTER", "Charter", "charter" } }) do
        row(f[1], f[2])
        ui:textbox{ x = fieldX, y = y - 4, w = fieldW, h = 64, value = search[f[3]], placeholder = any, maxLen = 64,
                    onChange = function(t) search[f[3]] = t; recount() end }
        y = y + 74
    end
    counter.label = ui:label{ text = "", x = labelX, y = y + 8, size = "small", color = COL_MUTED, maxWidth = panel.w - 80 }
    recount()
    local by = panel.y + panel.h - 90
    ui:button{ text = tr("SONGSELECT_SEARCH_OK", "Search"), x = panel.x + panel.w / 2 - 300, y = by, w = 280, h = 60, accent = true,
               onClick = function() SongSearch.write(GetSaveFile(activePlayer), searchParams()); sounds.Decide:Play(); wantClose = true end }
    ui:button{ text = tr("SONGSELECT_SEARCH_CANCEL", "Cancel"), x = panel.x + panel.w / 2 + 20, y = by, w = 280, h = 60,
               onClick = function() sounds.Cancel:Play(); wantClose = true end }
end

-- ── lifecycle ────────────────────────────────────────────────────────────────

function onStart()
    sounds.Decide = SHARED:GetSharedSound("Decide")
    sounds.Cancel = SHARED:GetSharedSound("Cancel")
    sounds.Skip   = SHARED:GetSharedSound("Skip")
end

function activate(player, mode, baseFolder)
    activePlayer, activeMode, activeBaseFolder = player or 0, mode or "sort", baseFolder
    wantClose, justOpened = false, true
    if ui ~= nil then ui:disposeWidgets() end
    ui = PopUI.new{
        theme = UI_THEME, bg = false, navPlayer = activePlayer + 1,
        sfx = { click = function() sounds.Decide:Play() end, hover = function() sounds.Skip:Play() end },
    }
    ui:button{ text = "×", w = 60, h = 60, accent = true, style = CLOSE_STYLE, sfx = { click = "" },
               onClick = function() sounds.Cancel:Play(); wantClose = true end }
    if activeMode == "sort" then buildSort() else buildSearch() end
    -- the close button sits on the panel's corner once the panel size is known
    ui.widgets[1].x, ui.widgets[1].y = panel.x + panel.w - 62, panel.y - 14
end

function deactivate()
    if ui ~= nil then ui:disposeWidgets(); ui = nil end
    sortButtons, counter.label = {}, nil
end

function update(ts)
    if ui == nil then return end
    if justOpened then justOpened = false; return end   -- the press that opened the dialog never reaches it
    local r = ui:update(ts)
    if r == "cancel" and not ui:isCapturing() then sounds.Cancel:Play(); wantClose = true end
    if wantClose then DEACTIVATE() end
end

function draw()
    if ui == nil then return end
    ui:rect(0, 0, SW, SH, 8, 10, 18, 150)
    ui:draw()
end

function onDestroy()
    if ui ~= nil then ui:disposeWidgets(); ui = nil end
end
