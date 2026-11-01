---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- Shortcuts.lua — a select screen's keyboard shortcuts as the song select shows them: the same bindings (theme
-- settings songselect_key_<id>), the "[key] - Show shortcuts" line, the help panel and the song speed notice.

local HERE = require("Core.Here")(...)
local PopUI = require("PopUI")
local Config = require("Core.Config")
local Layout = Config.load(HERE .. "Config/Shortcuts/layout.json")
local P, H, S = Layout.panel, Layout.helper, Layout.speed

local Shortcuts = {}
Shortcuts.__index = Shortcuts

-- default key and the locale key of the description, as in the song select
local DEFS = {
    help       = { default = "Tab",      text = "SONGSELECT_SC_HELP" },
    auto_p1    = { default = "F3",       text = "SONGSELECT_SC_AUTO_P1" },
    speed_down = { default = "PageDown", text = "SONGSELECT_SC_SPEED_DOWN" },
    speed_up   = { default = "PageUp",   text = "SONGSELECT_SC_SPEED_UP" },
}

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

-- a shortcut on a modifier key fires on a short tap's release, so desktop combinations never trigger it
local MODIFIERS = {
    LeftControl = true, RightControl = true, LeftShift = true, RightShift = true,
    LeftAlt = true, RightAlt = true, LeftWindowsKey = true, RightWindowsKey = true,
}
local TAP_MS = 500

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

local function sound(name)
    local s = SHARED:GetSharedSound(name)
    if s ~= nil then s:Play() end
end

-- o.ids: the shortcuts this screen offers, in the panel's order; o.nav: its player's NavInput;
-- o.disabled(id): true for a shortcut left out now (Event Mode...)
function Shortcuts.new(o)
    local self = setmetatable({}, Shortcuts)
    self.ids, self.nav = o.ids, o.nav
    self.disabled = o.disabled or function() return false end
    self.keys, self.tapStart = {}, {}
    self.ms = 0
    self.helperFont = TEXT:CreateGlyphCached(H.font)
    self.labelFont = TEXT:CreateGlyphCached(S.label_font)
    self.valueFont = TEXT:CreateGlyphCached(S.value_font)
    self.arrow = TEXTURE:CreateTextureFromAbsolutePath(HERE .. "Shortcuts/speed_arrow.png")
    self.colSlow, self.colFast = COLOR:CreateColorFromHex(S.slow_color), COLOR:CreateColorFromHex(S.fast_color)
    self.helperCol = Config.color(H.text_color)
    self.helperOutlineCol = Config.color(H.outline_color)
    self:reload()
    return self
end

function Shortcuts:reload()
    for _, id in ipairs(self.ids) do
        local ok, v = pcall(function() return THEME:GetThemeSetting("songselect_key_" .. id) end)
        self.keys[id] = (ok and type(v) == "string") and v or DEFS[id].default
    end
end

-- once a frame, before pressed()
function Shortcuts:tick() self.ms = self.ms + fps.deltaTime * 1000 end

function Shortcuts:pressed(id)
    if self.disabled(id) then return false end
    local k = self.keys[id]
    if k == nil or k == "" then return false end
    if not MODIFIERS[k] then return INPUT:KeyboardPressed(k) end
    if INPUT:KeyboardPressed(k) then self.tapStart[id] = self.ms end
    if INPUT:KeyboardReleased(k) then
        local start = self.tapStart[id]
        self.tapStart[id] = nil
        return start ~= nil and self.ms - start <= TAP_MS
    end
    return false
end

function Shortcuts:label(id)
    local l = keyLabel(self.keys[id])
    if l == nil then return tr("SONGSELECT_UNBOUND", "(not bound)") end
    return "[" .. l .. "]"
end

-- the song speed multiplier now
function Shortcuts.speed() return CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed) end

-- one song speed step (+1 or -1); the song preview follows; returns the new multiplier
function Shortcuts.stepSpeed(step)
    CONFIG.SongSpeed = CONFIG.SongSpeed + step
    local spd = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
    local ps = SHARED:GetSharedSound("presound")
    if ps ~= nil then ps:SetSpeed(spd) end
    sound("Skip")
    return spd
end

-- ── the "[key] - Show shortcuts" line: white text on a rounded dark box, pulsing ──────────────────────────────
function Shortcuts:drawHelper(x, y, opacity)
    local l = keyLabel(self.keys["help"])
    if l == nil or self.disabled("help") or (opacity or 1) <= 0 then return end
    local text = "[" .. l .. "] - " .. tr("SONGSELECT_SHORTCUTS_HELPER", "Show shortcuts")
    local gf = self.helperFont
    local inkH = math.ceil(gf.LineHeight)
    local pad = gf.BoxHeight - inkH
    local box = self.box
    if box == nil or box.text ~= text then
        if box ~= nil then box.canvas:Dispose() end
        local w = math.ceil(gf:Measure(text)) + 2 * H.pad_x
        local h = inkH + 2 * H.pad_y
        local cv = CANVAS:CreateCanvas(w, h)
        cv:ClearTransparent()
        local r = math.max(0, math.min(H.box_radius, math.floor(h / 2)))
        local c = H.box_color
        cv:FillRect(r, 0, w - 2 * r, h, c[1], c[2], c[3], H.box_alpha)
        cv:FillRect(0, r, w, h - 2 * r, c[1], c[2], c[3], H.box_alpha)
        for _, p in ipairs({ { r, r }, { w - r - 1, r }, { r, h - r - 1 }, { w - r - 1, h - r - 1 } }) do
            cv:FillCircle(p[1], p[2], r, c[1], c[2], c[3], H.box_alpha)
        end
        cv:Upload()
        box = { canvas = cv, text = text }
        self.box = box
    end
    local pulse = 0.5 + 0.5 * math.sin(2 * math.pi * self.ms / H.pulse_ms)
    local a = (opacity or 1) * (H.opacity_min + (1 - H.opacity_min) * pulse)
    box.canvas:SetOpacity(a)
    box.canvas:Draw(x, y)
    box.canvas:SetOpacity(1)
    gf:Draw(text, x + H.pad_x - pad, y + H.pad_y, self.helperCol, self.helperOutlineCol, a, 1, 0, "topleft")
end

-- ── the song speed notice, centred on x from y: its label, its value with two ▶ pulsing on the beat, a note ────
function Shortcuts:drawSpeed(x, y, bpm, opacity, note)
    local mult = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
    if mult == 1 or (opacity or 1) <= 0 then return end
    opacity = opacity or 1
    local col = mult < 1 and self.colSlow or self.colFast
    local beat = S.beat_default_ms
    if type(bpm) == "number" and bpm > 0 then beat = math.max(S.beat_min_ms, math.min(S.beat_max_ms, 60000 / (bpm * mult))) end
    local vy = y + S.value_dy
    local phase = (self.ms % beat) / beat
    if mult < 1 then phase = 1 - phase end
    local tex = self.arrow
    tex:SetColor(col)
    for i = 0, 1 do
        local p = (phase - i * 0.5) % 1
        tex:SetOpacity(opacity * (S.arrow_opacity_min + (1 - S.arrow_opacity_min) * (0.5 + 0.5 * math.cos(2 * math.pi * p))))
        tex:DrawAtAnchor(x + (i - 0.5) * S.arrow_gap, vy + self.valueFont.LineHeight / 2, "center")
    end
    tex:SetOpacity(1)
    tex:SetColor(1, 1, 1)
    self.labelFont:Draw(tr("SONGSELECT_SONG_SPEED", "Song speed"), x, y, col, nil, opacity, 1, 0, "top")
    self.valueFont:Draw(string.format("x%.2f", mult), x, vy, col, nil, opacity, 1, 0, "top")
    if note ~= nil and note ~= "" then self.labelFont:Draw(note, x, vy + S.note_dy, col, nil, opacity, 1, 0, "top") end
end

-- ── the help panel: every shortcut as "[key] - what it does", over a dimmed screen ───────────────────────────
function Shortcuts:open()
    self:reload()
    self.lines = {}
    for _, id in ipairs(self.ids) do
        if not self.disabled(id) then self.lines[#self.lines + 1] = { key = self:label(id), text = tr(DEFS[id].text, id) } end
    end
    local w = P.panel_w
    local h = P.head_h + #self.lines * P.row_h + P.foot_h
    local x, y = math.floor((1920 - w) / 2), math.floor((1080 - h) / 2)
    self.panel = { x = x, y = y, w = w, h = h }
    if self.ui ~= nil then self.ui:disposeWidgets() end
    local ui = PopUI.new{ theme = Config.plain(P.theme), bg = false,
        sfx = { click = function() sound("Decide") end, hover = function() sound("Skip") end } }
    ui:panel{ x = x, y = y, w = w, h = h, title = tr("SONGSELECT_SHORTCUTS_TITLE", "Shortcuts") }
    local cx, cb = P.close_x, P.close_button
    ui:button{ text = "×", x = x + w - cx.right, y = y + cx.y, w = cx.size, h = cx.size, accent = true,
               style = Config.plain(P.close_style), onClick = function() self.wantClose = true end, sfx = { click = "" } }
    local btn = ui:button{ text = tr("SONGSELECT_SHORTCUTS_CLOSE", "Close"), w = cb.w, h = cb.h, accent = true,
                           x = x + math.floor((w - cb.w) / 2), y = y + h - P.foot_h + cb.y,
                           onClick = function() self.wantClose = true end }
    btn.onDecide = function(b) b:keyActivate() end
    self.ui = ui
    self.isOpen_, self.wantClose, self.justOpened = true, false, true
    sound("Decide")
end

function Shortcuts:isOpen() return self.isOpen_ == true end

function Shortcuts:close()
    self.isOpen_ = false
    if self.ui ~= nil then self.ui:disposeWidgets(); self.ui = nil end
    sound("Cancel")
end

-- runs instead of the screen's input while the panel is up; true while it is
function Shortcuts:update()
    if not self.isOpen_ then return false end
    if self.justOpened then self.justOpened = false; return true end
    local r = self.ui ~= nil and self.ui:update(self.ms) or nil
    if self.wantClose or r == "cancel" or self:pressed("help") or self.nav.decide() or self.nav.cancel() then self:close() end
    return true
end

function Shortcuts:draw()
    if not self.isOpen_ or self.ui == nil then return end
    local d = P.dim_color
    self.ui:rect(0, 0, 1920, 1080, d[1], d[2], d[3], d[4])
    self.ui:draw()
    local p = self.panel
    local y = p.y + P.head_h
    local kx = p.x + P.key_x + P.key_col_w
    for _, l in ipairs(self.lines) do
        self.ui:drawTextEx(P.font_size, l.key, kx, y, P.key_color, P.text_outline_color, 1, 1, P.key_col_w, "topright")
        self.ui:drawTextEx(P.font_size, l.text, kx + P.text_gap, y, P.text_color, P.text_outline_color, 1, 1, p.w - P.key_col_w - P.text_right_margin)
        y = y + P.row_h
    end
end

function Shortcuts:dispose()
    if self.ui ~= nil then self.ui:disposeWidgets(); self.ui = nil end
    self.isOpen_ = false
    if self.box ~= nil then self.box.canvas:Dispose(); self.box = nil end
    for _, f in ipairs({ self.helperFont, self.labelFont, self.valueFont }) do pcall(function() f:Dispose() end) end
    if self.arrow ~= nil then pcall(function() self.arrow:Dispose() end); self.arrow = nil end
end

return Shortcuts
