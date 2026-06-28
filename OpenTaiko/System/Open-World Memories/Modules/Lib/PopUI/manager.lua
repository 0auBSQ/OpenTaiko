---@diagnostic disable: undefined-global, lowercase-global, need-check-nil, undefined-field
-- PopUI/manager.lua — the thing you actually use. `local ui = require("PopUI").new{...}` then
-- `ui:button{...}`, `ui:update(ts)`, `ui:draw()`. It owns the widgets, routes the mouse + keyboard/gamepad
-- into ONE shared hover/focus highlight, caches fonts + colours, and plays optional injected SFX.

local U      = require("PopUI.util")
local Theme  = require("PopUI.theme")
local Button = require("PopUI.widgets.button")
local Label  = require("PopUI.widgets.label")
local Toggle = require("PopUI.widgets.toggle")
local Slider = require("PopUI.widgets.slider")
local TextBox = require("PopUI.widgets.textbox")
local Panel  = require("PopUI.widgets.panel")
local Bubble = require("PopUI.widgets.bubble")
local Menu   = require("PopUI.widgets.menu")
local Chooser = require("PopUI.widgets.chooser")
local SettingsList = require("PopUI.widgets.settingslist")

local SCREEN_W, SCREEN_H = 1920, 1080
local HOLD_DELAY, HOLD_REPEAT = 0.20, 0.07

local M = {}
M.__index = M

function M.new(opts)
    opts = opts or {}
    local self = setmetatable({}, M)
    self.userTheme = opts.theme or {}
    self.theme = Theme.resolve(self.userTheme, nil)
    self.sfx = opts.sfx or {}
    self.drawBg = opts.bg == true
    self.widgets = {}
    self.focusables = {}
    self.focusIdx = 0
    self._focusW = nil
    self._hoverW = nil
    self._pressW = nil
    self._captureWidget = nil   -- a textbox currently eating keystrokes
    self._fonts = {}
    self._colors = {}
    self._atlases = {}          -- per-size glyph atlas for dynamic text (see _atlas/drawText)
    self._rep = {}
    self._lastTs = 0
    self._ctx = {}
    self.cancelRequested = false
    if self.drawBg then
        self._bg = CANVAS:CreateCanvas(2, 2); self._bg:Clear(255, 255, 255, 255); self._bg:Upload()
    end
    return self
end

-- ── caches ───────────────────────────────────────────────────────────────────────
function M:resolveTheme(style) return Theme.resolve(self.userTheme, style) end

function M:font(size)
    local f = self._fonts[size]
    if not f then f = TEXT:Create(size); self._fonts[size] = f end
    return f
end

function M:color(r, g, b, a)
    local key = U.packRGBA(r, g, b, a or 255)
    local c = self._colors[key]
    if not c then c = COLOR:CreateColorFromRGBA(math.floor(r), math.floor(g), math.floor(b), math.floor(a or 255)); self._colors[key] = c end
    return c
end

-- render (cached) text → LuaTexture. fg/bg are {r,g,b,a} colour arrays (bg = outline/shadow colour).
function M:renderText(size, str, fg, bg, centered, maxWidth)
    local f = self:font(size)
    local fc = fg and self:color(fg[1], fg[2], fg[3], fg[4]) or nil
    local bc = bg and self:color(bg[1], bg[2], bg[3], bg[4]) or nil
    return f:GetText(tostring(str), centered or false, maxWidth or 99999, fc, bc)
end

-- ── glyph-atlas text letter-by-letter ───────────────
-- A glyph texture's Width includes ~pad px of edge padding; the true advance is (Width - pad) where
-- pad = 2*W("8") - W("88"). We cache one White/Black glyph texture per char (tinted at draw → colour-free,
-- so the cache is the alphabet, not strings). for dynamic strings (slider value, textbox,
-- typewriter, footer) in order to prevent clogging the RAM.
-- ASCII only (iterates bytes); fall back to renderText for non-ASCII.
function M:_atlas(size)
    local a = self._atlases[size]
    if not a then
        local f = self:font(size)
        -- White fill + transparent edge (alpha 0): tinting at draw gives clean coloured text with NO black
        -- outline (matching the static labels, which also use a transparent backcolor) → lighter, not "super dark".
        local white, edge = self:color(255, 255, 255), self:color(0, 0, 0, 0)
        local function W(s) return f:GetText(s, false, 9999, white, edge).Width or 0 end
        a = {
            font = f, white = white, edge = edge,
            pad = 2 * W("8") - W("88"),
            space = math.max(1, W("0 0") - W("00")),
            h = f:GetText("8", false, 9999, white, edge).Height or size,
            glyphs = {},
        }
        self._atlases[size] = a
    end
    return a
end

function M:_glyph(a, ch)
    local g = a.glyphs[ch]
    if not g then
        local tex = a.font:GetText(ch, false, 9999, a.white, a.edge)
        g = { tex = tex, adv = math.max(1, (tex.Width or 0) - a.pad) }
        a.glyphs[ch] = g
    end
    return g
end

function M:measureText(size, str)
    local a = self:_atlas(size)
    local w = 0
    str = tostring(str)
    for i = 1, #str do
        local ch = str:sub(i, i)
        w = w + ((ch == " ") and a.space or self:_glyph(a, ch).adv)
    end
    return w
end

function M:textHeight(size) return self:_atlas(size).h end

-- draw str glyph-by-glyph, top-anchored at (x,y). color = {r,g,b}. returns the end x.
function M:drawText(size, str, x, y, color, opacity, scale)
    local a = self:_atlas(size)
    local r, g, b = color[1] / 255, color[2] / 255, color[3] / 255
    local op, s = opacity or 1, scale or 1
    local px = x
    str = tostring(str)
    for i = 1, #str do
        local ch = str:sub(i, i)
        if ch == " " then
            px = px + a.space * s
        else
            local gl = self:_glyph(a, ch)
            local t = gl.tex
            t:SetColor(r, g, b); t:SetOpacity(op); t:SetScale(s, s)
            t:Draw(math.floor(px), math.floor(y))
            t:SetColor(1, 1, 1); t:SetOpacity(1); t:SetScale(1, 1)   -- reset (glyph tex is shared)
            px = px + gl.adv * s
        end
    end
    return px
end

-- word/newline wrap to <= maxWidth using measureText (NO texture allocation). Returns a list of lines.
function M:wrapLines(size, str, maxWidth)
    local lines = {}
    for para in (tostring(str) .. "\n"):gmatch("(.-)\n") do
        local cur = ""
        for word in para:gmatch("%S+") do
            local cand = (cur == "") and word or (cur .. " " .. word)
            if self:measureText(size, cand) <= maxWidth or cur == "" then cur = cand
            else lines[#lines + 1] = cur; cur = word end
        end
        lines[#lines + 1] = cur
    end
    return lines
end

-- rasterize digits + printable ASCII once, so steady-state dynamic text allocates nothing
function M:_prewarm(size)
    local a = self:_atlas(size)
    local chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,:;!?+-/%()'\""
    for i = 1, #chars do self:_glyph(a, chars:sub(i, i)) end
end

-- reuse an existing canvas if the size matches (just clear it); else dispose the old one + make a new one.
-- This stops the GPU-texture leak on every re-bake (LuaCanvas has no finalizer; CreateCanvas allocates a
-- GL texture that is never freed unless Dispose() is called). Pass/return the RAW canvas (not a wrapper).
function M:reuseCanvas(old, w, h)
    w, h = math.floor(w), math.floor(h)
    if old and old.Width == w and old.Height == h then old:ClearTransparent(); return old end
    if old then old:Dispose() end
    return CANVAS:CreateCanvas(w, h)
end

function M:playSfx(name)
    local s = self.sfx[name]
    if not s then return end
    if type(s) == "function" then s() else pcall(function() s:Play() end) end
end

-- Draw a solid filled rectangle in screen space (tinted). Reuses one shared 2x2 white canvas so callers
-- (scrollbars, dividers, overlays) don't each allocate. Colours are 0-255.
function M:rect(x, y, w, h, r, g, b, a)
    if not self._rectCv then self._rectCv = CANVAS:CreateCanvas(2, 2); self._rectCv:Clear(255, 255, 255, 255); self._rectCv:Upload() end
    local cv = self._rectCv
    cv:SetColor((r or 255) / 255, (g or 255) / 255, (b or 255) / 255)
    cv:SetOpacity((a or 255) / 255)
    cv:SetScale(w / 2, h / 2)
    cv:Draw(math.floor(x), math.floor(y))
    cv:SetColor(1, 1, 1); cv:SetOpacity(1); cv:SetScale(1, 1)
end

-- Vertical-centering nudge for text: a GetText/atlas texture carries the font's line height (ascent +
-- descent), so a glyph anchored "center" reads as sitting slightly too HIGH. Nudge centered text down by a
-- small, size-proportional amount so it looks vertically centered inside buttons/rows/etc.
function M:textNudge(size) return math.floor((size or 22) * 0.13) end

-- ── widgets ────────────────────────────────────────────────────────────────────────
function M:add(w)
    w:init(self)
    self.widgets[#self.widgets + 1] = w
    self:_rebuildFocus()
    return w
end

function M:remove(w)
    for i = #self.widgets, 1, -1 do if self.widgets[i] == w then table.remove(self.widgets, i) end end
    self:_rebuildFocus()
end

function M:clear() self.widgets = {}; self.focusables = {}; self.focusIdx = 0; self._focusW = nil; self._hoverW = nil; self._pressW = nil; self._captureWidget = nil end

-- free every widget's baked GPU canvases (call before dropping/rebuilding the UI so re-entry doesn't leak;
-- LuaCanvas has no finalizer). GetText label textures are font-cache-owned and left untouched. Also frees the
-- lazily-created shared rect canvas (it's recreated on the next M:rect call, so this is safe to call mid-life).
function M:disposeWidgets()
    for _, w in ipairs(self.widgets) do if w.dispose then w:dispose() end end
    if self._rectCv then self._rectCv:Dispose(); self._rectCv = nil end
end

function M:_rebuildFocus()
    self.focusables = {}
    for _, w in ipairs(self.widgets) do
        if w.focusable and w.visible and w.enabled then self.focusables[#self.focusables + 1] = w end
    end
    if self.focusIdx > #self.focusables then self.focusIdx = #self.focusables end
end

-- factory shorthands
function M:button(o)  return self:add(Button.new(o)) end
function M:label(o)   return self:add(Label.new(o)) end
function M:toggle(o)  return self:add(Toggle.new(o)) end
function M:checkbox(o) o = o or {}; o.variant = "checkbox"; return self:add(Toggle.new(o)) end
function M:slider(o)  return self:add(Slider.new(o)) end
function M:textbox(o) return self:add(TextBox.new(o)) end
function M:panel(o)   return self:add(Panel.new(o)) end
function M:bubble(o)  return self:add(Bubble.new(o)) end
function M:menu(o)    return self:add(Menu.new(o)) end
function M:chooser(o) return self:add(Chooser.new(o)) end
function M:settingsList(o) return self:add(SettingsList.new(o)) end

-- ── theming ──────────────────────────────────────────────────────────────────────
function M:setTheme(t)
    self.userTheme = t or {}
    self.theme = Theme.resolve(self.userTheme, nil)
    self._colors = {}   -- colours may have changed (atlases are colour-independent → NOT rebuilt)
    if self._bg then self._bg = self:reuseCanvas(self._bg, 2, 2); self._bg:Clear(255, 255, 255, 255); self._bg:Upload() end
    for _, w in ipairs(self.widgets) do w:restyle() end
end

-- ── focus navigation ────────────────────────────────────────────────────────────
function M:_setFocusIndex(i)
    if #self.focusables == 0 then self.focusIdx = 0; return end
    i = ((i - 1) % #self.focusables) + 1
    self.focusIdx = i
end

function M:focusNext() self:_setFocusIndex(self.focusIdx < 1 and 1 or self.focusIdx + 1); self:playSfx("move") end
function M:focusPrev() self:_setFocusIndex(self.focusIdx < 1 and 1 or self.focusIdx - 1); self:playSfx("move") end

function M:captureKeys(w) self._captureWidget = w end
function M:releaseKeys(w) if self._captureWidget == w then self._captureWidget = nil end end
--- True while a widget (e.g. a textbox) is eating keystrokes. Gate stage-level hotkeys on `not ui:isCapturing()`
--- so typing a letter that's also a hotkey doesn't trigger a menu/page action.
function M:isCapturing() return self._captureWidget ~= nil end

function M:_repeatKey(key, dt)
    local st = self._rep[key]
    if not st then st = { held = false, t = 0 }; self._rep[key] = st end
    if INPUT:KeyboardPressed(key) then st.held = true; st.t = 0; return true end
    if st.held and INPUT:KeyboardPressing(key) then
        st.t = st.t + dt
        if st.t >= HOLD_DELAY then st.t = st.t - HOLD_REPEAT; return true end
        return false
    end
    st.held = false; st.t = 0; return false
end

-- ── per-frame ───────────────────────────────────────────────────────────────────
function M:update(ts)
    local dt = (ts - self._lastTs) / 1000.0
    self._lastTs = ts
    if dt < 0 then dt = 0 elseif dt > 0.1 then dt = 0.1 end

    local c = self._ctx
    c.dt, c.ts = dt, ts
    c.mx, c.my = INPUT:GetMouseXY()
    c.inside = INPUT:IsMouseInside()
    c.mPressed  = INPUT:MousePressed("Left")
    c.mPressing = INPUT:MousePressing("Left")
    c.mReleased = INPUT:MouseReleased("Left")
    c.scrollDx, c.scrollDy = INPUT:GetScrollDelta()
    -- one-frame mouse suppression: a caller (e.g. a stage dragging its own scrollbar) sets self._suppressMouse
    -- = true BEFORE ui:update so nothing under the cursor hovers/presses/clicks/scrolls/drags this frame. We
    -- neutralise the ctx mouse fields at the source (covers the hover scan, the press/release block, AND every
    -- widget's own :update that reads the mouse directly — slider drag, list/chooser scroll). One-shot: cleared here.
    if self._suppressMouse then
        c.mx, c.my = -100000, -100000
        c.inside = false
        c.mPressed, c.mPressing, c.mReleased = false, false, false
        c.scrollDx, c.scrollDy = 0, 0
        self._suppressMouse = false
    end
    -- drop a stale capture if its widget was hidden/disabled (e.g. the page changed while typing)
    if self._captureWidget and (not self._captureWidget.visible or not self._captureWidget.enabled) then
        self._captureWidget = nil
    end
    local captured = self._captureWidget ~= nil
    c.decide = (not captured) and (INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") or INPUT:KeyboardPressed("Space")) or false
    c.cancel = INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape")
    c.navDown  = (not captured) and self:_repeatKey("DownArrow", dt) or false
    c.navUp    = (not captured) and self:_repeatKey("UpArrow", dt) or false
    c.navLeft  = (not captured) and (self:_repeatKey("LeftArrow", dt) or INPUT:Pressed("LBlue")) or false
    c.navRight = (not captured) and (self:_repeatKey("RightArrow", dt) or INPUT:Pressed("RBlue")) or false

    self.cancelRequested = false

    if not captured then
        -- mouse hover: topmost visible+enabled hit
        local hoverW = nil
        if c.inside then
            for i = #self.widgets, 1, -1 do
                local w = self.widgets[i]
                if w.visible and w.enabled and w.focusable and w:hitTest(c.mx, c.my) then hoverW = w; break end
            end
        end
        if hoverW ~= self._hoverW then
            if self._hoverW then self._hoverW:setHover(false) end
            if hoverW then hoverW:setHover(true) end
            self._hoverW = hoverW
        end
        -- mouse moves focus to the hovered widget
        if hoverW then
            for i, w in ipairs(self.focusables) do if w == hoverW then self.focusIdx = i; break end end
        end
        -- keyboard/gamepad focus navigation; a focused widget (e.g. a list) may consume Up/Down for its
        -- own internal selection and only let focus escape at its boundary.
        local fw = self.focusables[self.focusIdx]
        if c.navDown then
            if not (fw and fw.onNavDown and fw:onNavDown()) then self:focusNext() end
        end
        if c.navUp then
            fw = self.focusables[self.focusIdx]
            if not (fw and fw.onNavUp and fw:onNavUp()) then self:focusPrev() end
        end
    end

    -- apply focus flags from focusIdx
    local focusW = self.focusables[self.focusIdx]
    if focusW ~= self._focusW then
        if self._focusW then self._focusW:setFocus(false) end
        if focusW then focusW:setFocus(true) end
        self._focusW = focusW
    end

    if not captured then
        -- pointer press/release
        if c.mPressed and self._hoverW then self._hoverW:press(); self._pressW = self._hoverW end
        if c.mReleased and self._pressW then
            local inside = self._pressW:hitTest(c.mx, c.my)
            self._pressW:release(inside); self._pressW = nil
        end
        -- keyboard/gamepad activate (single-shot squish→boing, not a same-frame press+release that cancels it)
        if c.decide and focusW then focusW:keyActivate() end
        -- cancel bubbles to the stage
        if c.cancel then self.cancelRequested = true end
    end

    -- per-widget update (anims + widget-specific logic: drag, capture, scroll, typewriter)
    for _, w in ipairs(self.widgets) do if w.visible then w:update(c) end end

    return self.cancelRequested and "cancel" or nil
end

function M:draw()
    if self._bg then
        local col = self.theme.colors.bg
        self._bg:SetColor(col[1] / 255, col[2] / 255, col[3] / 255)
        self._bg:SetOpacity((col[4] or 255) / 255)
        self._bg:SetScale(SCREEN_W / 2, SCREEN_H / 2)
        self._bg:Draw(0, 0)
    end
    for _, w in ipairs(self.widgets) do if w.visible then w:draw() end end
end

return M
