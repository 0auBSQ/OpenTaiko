---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- shortcuts.lua — the song select's keyboard shortcuts.
--
-- Every shortcut is a theme setting of type "key" (ThemeSettings.json, ids songselect_key_<id>): one keyboard
-- key each, bound in the settings menu's Theme tab, "" = unbound. The bindings are read on every activate so
-- a change in the settings shows on the next visit. Below the "Select a Song" overlay the helper line
-- "[key] - Show shortcuts" pulses; that key opens a PopUI panel listing every shortcut as "[key] - what it does".
--
--   SC.init(G)   SC.reload()   SC.pressed(id)   SC.label(id)   SC.helperText()
--   SC.open()    SC.isOpen()   SC.update(ts)    SC.draw(opacity)   SC.dispose()

local PopUI = require("PopUI")

local M = {}
local G

-- id, default key, locale key of the description; the order is the help panel's order
local DEFS = {
    { id = "help",             default = "Tab",         text = "SONGSELECT_SC_HELP" },
    { id = "sort",             default = "Space",       text = "SONGSELECT_SC_SORT" },
    { id = "search",           default = "F1",          text = "SONGSELECT_SC_SEARCH" },
    { id = "favorite",         default = "LeftControl", text = "SONGSELECT_SC_FAVORITE" },
    { id = "favorites_folder", default = "F2",          text = "SONGSELECT_SC_FAVORITES_FOLDER" },
    { id = "auto_p1",          default = "F3",          text = "SONGSELECT_SC_AUTO_P1" },
    { id = "auto_p2",          default = "F4",          text = "SONGSELECT_SC_AUTO_P2" },
    { id = "player_count",     default = "F5",          text = "SONGSELECT_SC_PLAYER_COUNT" },
    { id = "player",           default = "F6",          text = "SONGSELECT_SC_PLAYER" },
    { id = "course",           default = "F7",          text = "SONGSELECT_SC_COURSE" },
    { id = "speed_down",       default = "PageDown",    text = "SONGSELECT_SC_SPEED_DOWN" },
    { id = "speed_up",         default = "PageUp",      text = "SONGSELECT_SC_SPEED_UP" },
}
M.DEFS = DEFS

-- SlimDX key names that read better than their enum name; letters, digits and function keys pass through
local KEY_NAMES = {
    LeftControl = "L-Ctrl", RightControl = "R-Ctrl", LeftShift = "L-Shift", RightShift = "R-Shift",
    LeftAlt = "L-Alt", RightAlt = "R-Alt", Return = "Enter", Escape = "Esc", Back = "Backspace",
    UpArrow = "Up", DownArrow = "Down", LeftArrow = "Left", RightArrow = "Right",
    PageUp = "PgUp", PageDown = "PgDn", Insert = "Ins", Delete = "Del", CapsLock = "Caps",
    Grave = "`", Minus = "-", Equals = "=", LeftBracket = "[", RightBracket = "]", Semicolon = ";",
    Apostrophe = "'", Backslash = "\\", Comma = ",", Period = ".", Slash = "/",
    NumberPadEnter = "Num Enter", NumberPadPlus = "Num +", NumberPadMinus = "Num -",
    NumberPadStar = "Num *", NumberPadSlash = "Num /", NumberPadPeriod = "Num .",
}

local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

local function keyLabel(name)
    if name == nil or name == "" then return nil end
    if KEY_NAMES[name] then return KEY_NAMES[name] end
    local d = name:match("^D(%d)$")
    if d then return d end
    local np = name:match("^NumberPad(%d)$")
    if np then return "Num " .. np end
    return name
end

M.keys = {}      -- id -> key name ("" = unbound)

function M.init(g) G = g end

-- read every binding from the theme settings (a missing definition keeps the default)
function M.reload()
    for _, d in ipairs(DEFS) do
        local ok, v = pcall(function() return THEME:GetThemeSetting("songselect_key_" .. d.id) end)
        if ok and type(v) == "string" then M.keys[d.id] = v else M.keys[d.id] = d.default end
    end
end

function M.pressed(id)
    local k = M.keys[id]
    if k == nil or k == "" then return false end
    return INPUT:KeyboardPressed(k)
end

-- "[Space]" for a bound shortcut, the unbound text otherwise
function M.label(id)
    local l = keyLabel(M.keys[id])
    if l == nil then return tr("SONGSELECT_UNBOUND", "(not bound)") end
    return "[" .. l .. "]"
end

-- the helper line under the overlay; nil when the help shortcut is unbound
function M.helperText()
    local l = keyLabel(M.keys["help"])
    if l == nil then return nil end
    return "[" .. l .. "] - " .. tr("SONGSELECT_SHORTCUTS_HELPER", "Show shortcuts")
end

-- ── the help panel ───────────────────────────────────────────────────────────

local SW, SH = 1920, 1080
local PANEL_W, ROW_H, HEAD_H, FOOT_H = 1040, 46, 96, 70
local KEY_COL_W = 190
local THEME_UI = {
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

local ui = nil
local open = false
local lines = {}
local panel = { x = 0, y = 0, w = PANEL_W, h = 0 }
local wantClose = false
local justOpened = false

local function buildPanel()
    lines = {}
    for _, d in ipairs(DEFS) do
        lines[#lines + 1] = { key = M.label(d.id), text = tr(d.text, d.id) }
    end
    panel.h = HEAD_H + #lines * ROW_H + FOOT_H
    panel.x = math.floor((SW - panel.w) / 2)
    panel.y = math.floor((SH - panel.h) / 2)
    if ui ~= nil then ui:disposeWidgets() end
    ui = PopUI.new{
        theme = THEME_UI, bg = false,
        sfx = { click = function() G.sounds.Decide:Play() end, hover = function() G.sounds.Skip:Play() end },
    }
    ui:panel{ x = panel.x, y = panel.y, w = panel.w, h = panel.h, title = tr("SONGSELECT_SHORTCUTS_TITLE", "Shortcuts") }
    ui:button{ text = "×", x = panel.x + panel.w - 62, y = panel.y - 14, w = 60, h = 60, accent = true,
               style = CLOSE_STYLE, onClick = function() wantClose = true end, sfx = { click = "" } }
    local btn = ui:button{ text = tr("SONGSELECT_SHORTCUTS_CLOSE", "Close"), w = 260, h = 56, accent = true,
                           x = panel.x + math.floor((panel.w - 260) / 2), y = panel.y + panel.h - FOOT_H + 6,
                           onClick = function() wantClose = true end }
    btn.onDecide = function(self) self:keyActivate() end
end

function M.open()
    M.reload()
    buildPanel()
    open, wantClose, justOpened = true, false, true
    G.sounds.Decide:Play()
end

function M.isOpen() return open end

local function close()
    open = false
    if ui ~= nil then ui:disposeWidgets(); ui = nil end
    G.sounds.Cancel:Play()
end

-- runs instead of the song select input while the panel is up
function M.update(ts)
    if not open then return end
    if justOpened then justOpened = false; return end   -- the opening press never reaches the panel
    local r = nil
    if ui ~= nil then r = ui:update(ts) end
    if wantClose or r == "cancel" or M.pressed("help") or G.NavInput.decide() or G.NavInput.cancel() then close() end
end

function M.draw(opacity)
    if not open or ui == nil then return end
    ui:rect(0, 0, SW, SH, 8, 10, 18, 150)
    ui:draw()
    local y = panel.y + HEAD_H
    local kx = panel.x + 40 + KEY_COL_W
    for _, l in ipairs(lines) do
        ui:drawTextEx(22, l.key, kx, y, { 72, 108, 156 }, { 255, 255, 255, 160 }, 1, 1, KEY_COL_W, "topright")
        ui:drawTextEx(22, l.text, kx + 28, y, { 44, 56, 74 }, { 255, 255, 255, 160 }, 1, 1, panel.w - KEY_COL_W - 110)
        y = y + ROW_H
    end
end

function M.dispose()
    open = false
    if ui ~= nil then ui:disposeWidgets(); ui = nil end
end

return M
