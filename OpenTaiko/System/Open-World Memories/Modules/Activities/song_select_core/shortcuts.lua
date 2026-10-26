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
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local P      = Layout.shortcuts

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

-- Event Mode leaves these out: no auto play, no favorites; sort and search stay
local EVENT_DISABLED = { favorite = true, favorites_folder = true, auto_p1 = true, auto_p2 = true }
-- the online lobby's song select picks the song for one player: no auto play, no player count or switch
local ONLINE_DISABLED = { auto_p1 = true, auto_p2 = true, player_count = true, player = true }

function M.init(g) G = g end

-- A shortcut this song select cannot use is left out everywhere: the help panel does not list it and its key
-- does nothing. Decided from the select's set-up (G.activeConfig), never from the current player count: the
-- regular select can change its count at any time, so it keeps every player shortcut.
local function disabled(id)
    if G == nil then return false end
    local cfg = G.activeConfig or {}
    if ONLINE_DISABLED[id] and cfg.songOnly then return true end
    if EVENT_DISABLED[id] and G.event ~= nil and G.event.on() then return true end
    if id == "player_count" then return cfg.allowPlayerCount == false end   -- training, AI battle: the count is fixed
    -- locked to one player (training), or P2 is the AI: nobody to switch to, no P2 auto play
    if id == "player" or id == "auto_p2" then
        return cfg.mountAISlotToP2 == true or (cfg.lockedPlayerCount ~= nil and cfg.lockedPlayerCount < 2)
    end
    return false
end

-- read every binding from the theme settings (a missing definition keeps the default)
function M.reload()
    for _, d in ipairs(DEFS) do
        local ok, v = pcall(function() return THEME:GetThemeSetting("songselect_key_" .. d.id) end)
        if ok and type(v) == "string" then M.keys[d.id] = v else M.keys[d.id] = d.default end
    end
end

-- A shortcut on a modifier key fires when the key is let go after a short tap, so a desktop combination
-- (Ctrl+Q closing the game, Alt+Tab...) never triggers it: the game closes or the key is held longer first.
local MODIFIERS = {
    LeftControl = true, RightControl = true, LeftShift = true, RightShift = true,
    LeftAlt = true, RightAlt = true, LeftWindowsKey = true, RightWindowsKey = true,
}
local TAP_MS = 500
local tapStart = {}   -- id -> the time its modifier went down

function M.pressed(id)
    if disabled(id) then return false end
    local k = M.keys[id]
    if k == nil or k == "" then return false end
    if not MODIFIERS[k] then return INPUT:KeyboardPressed(k) end
    local now = (G and G.nowMs) or 0
    if INPUT:KeyboardPressed(k) then tapStart[id] = now end
    if INPUT:KeyboardReleased(k) then
        local start = tapStart[id]
        tapStart[id] = nil
        return start ~= nil and now - start <= TAP_MS
    end
    return false
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
local PANEL_W, ROW_H, HEAD_H, FOOT_H = P.panel_w, P.row_h, P.head_h, P.foot_h
local KEY_COL_W = P.key_col_w
local THEME_UI = Config.plain(P.theme)
local CLOSE_STYLE = Config.plain(P.close_style)
local CloseX, CloseButton = P.close_x, P.close_button
local DIM = P.dim_color

local ui = nil
local open = false
local lines = {}
local panel = { x = 0, y = 0, w = PANEL_W, h = 0 }
local wantClose = false
local justOpened = false

local function buildPanel()
    lines = {}
    for _, d in ipairs(DEFS) do
        if not disabled(d.id) then lines[#lines + 1] = { key = M.label(d.id), text = tr(d.text, d.id) } end
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
    ui:button{ text = "×", x = panel.x + panel.w - CloseX.right, y = panel.y + CloseX.y, w = CloseX.size, h = CloseX.size, accent = true,
               style = CLOSE_STYLE, onClick = function() wantClose = true end, sfx = { click = "" } }
    local btn = ui:button{ text = tr("SONGSELECT_SHORTCUTS_CLOSE", "Close"), w = CloseButton.w, h = CloseButton.h, accent = true,
                           x = panel.x + math.floor((panel.w - CloseButton.w) / 2), y = panel.y + panel.h - FOOT_H + CloseButton.y,
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
M.close = close

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
    ui:rect(0, 0, SW, SH, DIM[1], DIM[2], DIM[3], DIM[4])
    ui:draw()
    local y = panel.y + HEAD_H
    local kx = panel.x + P.key_x + KEY_COL_W
    for _, l in ipairs(lines) do
        ui:drawTextEx(P.font_size, l.key, kx, y, P.key_color, P.text_outline_color, 1, 1, KEY_COL_W, "topright")
        ui:drawTextEx(P.font_size, l.text, kx + P.text_gap, y, P.text_color, P.text_outline_color, 1, 1, panel.w - KEY_COL_W - P.text_right_margin)
        y = y + ROW_H
    end
end

function M.dispose()
    open = false
    if ui ~= nil then ui:disposeWidgets(); ui = nil end
end

return M
