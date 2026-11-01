---@diagnostic disable: undefined-global, lowercase-global, need-check-nil, undefined-field
-- SimpleUI/manager.lua — the thing you actually use. `local ui = require("UI.SimpleUI").new{...}` then
-- `ui:button{...}`, `ui:update(ts)`, `ui:draw()`. It owns the widgets, routes the mouse + keyboard/gamepad
-- into one shared hover/focus highlight, caches fonts + colours, and plays optional injected SFX.
--   SimpleUI.new{ theme =, sfx =, bg = bool, navPlayer = n, nav = "spatial" }
-- A library made with SimpleUI.derive (OWM's PopUI) passes itself as M.new's second argument: its
-- Theme.DEFAULT is the base every theme resolves over, and its Coins class makes ui:coins.
-- nav = "spatial": the arrow keys move focus to the nearest widget in their direction (a grid of buttons,
-- two columns); by default Up/Down step through the widgets in the order they were made.

local U      = require("UI.SimpleUI.util")
local Theme  = require("UI.SimpleUI.theme")
local Cache  = require("UI.SimpleUI.cache")
local Button = require("UI.SimpleUI.widgets.button")
local Label  = require("UI.SimpleUI.widgets.label")
local Toggle = require("UI.SimpleUI.widgets.toggle")
local Slider = require("UI.SimpleUI.widgets.slider")
local TextBox = require("UI.SimpleUI.widgets.textbox")
local Panel  = require("UI.SimpleUI.widgets.panel")
local Bubble = require("UI.SimpleUI.widgets.bubble")
local Menu   = require("UI.SimpleUI.widgets.menu")
local Chooser = require("UI.SimpleUI.widgets.chooser")
local SettingsList = require("UI.SimpleUI.widgets.settingslist")
local Coins        = require("UI.SimpleUI.widgets.coins")
local Sfx          = require("UI.SimpleUI.sfx")

local NavInput = require("Input.NavInput")

local SCREEN_W, SCREEN_H = 1920, 1080
local HOLD_DELAY, HOLD_REPEAT = 0.20, 0.07
local COLOR_CACHE_MAX = 1024
local SQUISH_MIN = 0.85          -- text squeezed narrower than this gets smaller instead

-- Module-shared font caches. Managers come and go (config_ui rebuilds on language change) but fonts are
-- size-keyed and theme-independent, so sharing them keeps the baked glyphs alive across rebuilds — no
-- re-rasterization lag and no orphaned font objects stacking up.
-- The main font is language-dependent (CLangManager.LangInstance.FontName): call M.flushSharedFonts() when
-- the language changes so the caches rebuild with the new typeface.
local sharedFonts, sharedGFonts = {}, {}
local measured, measuredCount = {}, 0   -- "size\0text" -> ink width, for the squish limit

local M = {}
M.__index = M

-- dispose + drop every shared font (their baked glyph textures free with them); the next text draw
-- recreates them with the current language's font
function M.flushSharedFonts()
    for _, f in pairs(sharedFonts) do pcall(function() f:Dispose() end) end
    for _, f in pairs(sharedGFonts) do pcall(function() f:Dispose() end) end
    sharedFonts, sharedGFonts = {}, {}
    measured, measuredCount = {}, 0
end

-- the canvas pool and the baked-surface cache (SimpleUI/cache.lua), kept reachable from the manager
M.releaseCanvas = Cache.releaseCanvas
M.acquireCanvas = Cache.acquireCanvas
M.flushCanvasPool = Cache.flushCanvasPool
M.releaseBaked = Cache.releaseBaked
M.flushBakedCanvases = Cache.flushBakedCanvases
M.keyOf = Cache.keyOf
function M:bakedCanvas(key, w, h, bake) return Cache.bakedCanvas(key, w, h, bake) end
function M:themeKey(eff) return Cache.themeKey(eff) end

local NO_STYLE = Theme.NONE

function M.new(opts, lib)
    opts = opts or {}
    local self = setmetatable({}, M)
    self.lib = lib
    self.baseTheme = (lib and lib.Theme and lib.Theme.DEFAULT) or Theme.DEFAULT
    self._coinsClass = (lib and lib.Coins) or Coins
    self.navPlayer = opts.navPlayer  -- for NavInput.getPn(navPlayer)
    self.spatial = opts.nav == "spatial"
    self.userTheme = opts.theme or {}
    self.theme = Theme.resolve(self.userTheme, nil, self.baseTheme)
    self._themes = setmetatable({}, { __mode = "k" })   -- widget style -> resolved theme
    self._sfxs = setmetatable({}, { __mode = "k" })     -- widget sfx table -> resolved sfx
    self.userSfx = opts.sfx or {}
    self.sfx = Sfx.resolve(self.userSfx, nil)
    self.drawBg = opts.bg == true
    self.widgets = {}
    self.focusables = {}
    self.focusIdx = 0
    self._focusW = nil
    self._hoverW = nil
    self._pressW = nil
    self._captureWidget = nil   -- a textbox currently eating keystrokes
    self._colors = {}
    self._colorCount = 0
    self._rep = {}
    self._lastTs = 0
    self._ctx = {}
    self._mouseLead = false
    self.cancelRequested = false
    if self.drawBg then
        self._bg = CANVAS:CreateCanvas(2, 2); self._bg:Clear(255, 255, 255, 255); self._bg:Upload()
    end
    return self
end

-- ── caches ───────────────────────────────────────────────────────────────────────
-- the effective theme of a widget style (default < manager theme < style); one resolved table per style,
-- so widgets with the same style share it (and its bake keys)
function M:resolveTheme(style)
    local key = style or NO_STYLE
    local t = self._themes[key]
    if t == nil then
        t = Theme.resolve(self.userTheme, style ~= NO_STYLE and style or nil, self.baseTheme)
        self._themes[key] = t
    end
    return t
end
function M:resolveSfx(sfx)
    local key = sfx or NO_STYLE
    local t = self._sfxs[key]
    if t == nil then
        t = Sfx.resolve(self.userSfx, sfx ~= NO_STYLE and sfx or nil)
        self._sfxs[key] = t
    end
    return t
end

function M:font(size)
    local f = sharedFonts[size]
    if not f then f = TEXT:Create(size); sharedFonts[size] = f end
    return f
end

function M:color(r, g, b, a)
    local key = U.packRGBA(r, g, b, a or 255)
    local c = self._colors[key]
    if not c then
        -- colours change every frame while they tween; past the cap the cache starts over
        if self._colorCount >= COLOR_CACHE_MAX then self._colors, self._colorCount = {}, 0 end
        c = COLOR:CreateColorFromRGBA(math.floor(r), math.floor(g), math.floor(b), math.floor(a or 255))
        self._colors[key] = c
        self._colorCount = self._colorCount + 1
    end
    return c
end

-- render (cached) text → LuaTexture. fg/bg are {r,g,b,a} colour arrays (bg = outline/shadow colour).
function M:renderText(size, str, fg, bg, centered, maxWidth)
    local f = self:font(size)
    local fc = fg and self:color(fg[1], fg[2], fg[3], fg[4]) or nil
    local bc = bg and self:color(bg[1], bg[2], bg[3], bg[4]) or nil
    return f:GetText(tostring(str), centered or false, maxWidth or 99999, fc, bc)
end

-- ── glyph-composed text (C# LuaGlyphText: bounded per-character texture cache) ─────
-- One texture per unique character; strings are composed at draw time with per-letter post-processing
-- (the maxWidth squish scales each glyph individually). UTF-8 correct (the C# side iterates code points),
-- exact font advances, and word wrap for text blocks. Dynamic strings never allocate per-frame textures.
function M:gfont(size)
    local f = sharedGFonts[size]
    if not f then f = TEXT:CreateGlyphCached(size); sharedGFonts[size] = f end
    return f
end

function M:measureText(size, str)
    return self:gfont(size):Measure(tostring(str))
end

function M:textHeight(size) return self:gfont(size).BoxHeight end

local function inkWidth(self, size, str)
    local k = size .. "\0" .. str
    local w = measured[k]
    if w == nil then
        if measuredCount >= 2048 then measured, measuredCount = {}, 0 end
        w = self:gfont(size):Measure(str)
        measured[k] = w
        measuredCount = measuredCount + 1
    end
    return w
end

-- the scale to draw str at within maxWidth: the glyph draw squeezes text that is too wide, and past
-- SQUISH_MIN the text is made smaller as well, so a long translation stays readable
function M:fitScale(size, str, scale, maxWidth)
    scale = scale or 1
    if not maxWidth or maxWidth <= 0 then return scale end
    local ink = inkWidth(self, size, str) * scale
    local room = maxWidth - 50 * scale            -- the glyph box keeps 25 px of padding on each side
    if room <= 0 or ink <= room or room / ink >= SQUISH_MIN then return scale end
    return scale * maxWidth / (SQUISH_MIN * ink + 50 * scale)
end

-- draw str glyph-by-glyph, top-anchored at (x,y). color = {r,g,b}. optional maxWidth applies the per-letter
-- squish. returns the end x (unsquished ink end).
function M:drawText(size, str, x, y, color, opacity, scale, maxWidth)
    str = tostring(str)
    local gf = self:gfont(size)
    local fc = color and self:color(color[1], color[2], color[3], color[4]) or self:color(255, 255, 255)
    scale = self:fitScale(size, str, scale, maxWidth)
    gf:Draw(str, x, y, fc, self:color(0, 0, 0, 0), opacity or 1, scale, maxWidth or 0, "topleft")
    return x + gf:Measure(str) * scale
end

-- full-control glyph draw for widget labels: fg/bg {r,g,b,a} (bg = outline; nil = none), anchor = the same
-- 9 anchor names as DrawAtAnchor, anchoring the text's box (ink + the same padding a GetText texture has).
-- Optional clipY0/clipY1 clip the text to a vertical band (pixel-exact glyph slicing) — scrolling lists
-- pass their viewport so edge rows cut cleanly instead of overflowing the list box.
function M:drawTextEx(size, str, x, y, fg, bg, opacity, scale, maxWidth, anchor, clipY0, clipY1)
    str = tostring(str)
    local gf = self:gfont(size)
    local fc = fg and self:color(fg[1], fg[2], fg[3], fg[4]) or self:color(255, 255, 255)
    local bc = bg and self:color(bg[1], bg[2], bg[3], bg[4]) or self:color(0, 0, 0, 0)
    scale = self:fitScale(size, str, scale, maxWidth)
    if clipY0 ~= nil then gf:SetClipY(clipY0, clipY1 or clipY0) end
    gf:Draw(str, x, y, fc, bc, opacity or 1, scale, maxWidth or 0, anchor or "topleft")
    if clipY0 ~= nil then gf:SetClipY(0, 0) end
end

-- the longest start of str that fits in maxWidth with an ellipsis after it (or str when it fits)
function M:ellipsize(size, str, maxWidth)
    str = tostring(str)
    local room = maxWidth - 50
    if inkWidth(self, size, str) <= room then return str end
    local chars = self:utf8chars(str)
    local dots = "…"
    local lo, hi = 0, #chars
    while lo < hi do
        local mid = math.floor((lo + hi + 1) / 2)
        if inkWidth(self, size, table.concat(chars, "", 1, mid) .. dots) <= room then lo = mid else hi = mid - 1 end
    end
    return table.concat(chars, "", 1, lo) .. dots
end

-- word/newline wrap to <= maxWidth (UTF-8 correct, CJK-aware; no texture allocation). Returns a list of lines.
function M:wrapLines(size, str, maxWidth)
    local arr = self:gfont(size):WrapToLines(tostring(str), maxWidth)
    local lines = {}
    for i = 0, arr.Length - 1 do lines[i + 1] = arr[i] end
    return lines
end

-- word-wrapped text block, left-aligned at (x,y); returns the drawn height (for stacking layout below it)
function M:drawWrapped(size, str, x, y, wrapWidth, color, opacity, scale, lineSpacing)
    local gf = self:gfont(size)
    local fc = color and self:color(color[1], color[2], color[3], color[4]) or self:color(255, 255, 255)
    return gf:DrawWrapped(tostring(str), x, y, wrapWidth, fc, self:color(0, 0, 0, 0),
        opacity or 1, scale or 1, lineSpacing or 1)
end

-- height drawWrapped would take, without drawing
function M:measureWrapped(size, str, wrapWidth, scale, lineSpacing)
    return self:gfont(size):MeasureWrapped(tostring(str), wrapWidth, scale or 1, lineSpacing or 1)
end

-- UTF-8 character list (the sandbox has no utf8.*); for suffix/prefix clipping without splitting sequences
function M:utf8chars(str)
    local chars = {}
    for ch in tostring(str):gmatch("[%z\1-\127\194-\244][\128-\191]*") do chars[#chars + 1] = ch end
    return chars
end

-- reuse an existing canvas if the size matches (just clear it); else release the old one + take a new one.
-- LuaCanvas has no finalizer: a canvas that is never released keeps its GL texture. Pass/return the raw canvas.
function M:reuseCanvas(old, w, h)
    w, h = math.floor(w), math.floor(h)
    if old and old.Width == w and old.Height == h then old:ClearTransparent(); return old end
    if old then M.releaseCanvas(old) end
    return M.acquireCanvas(w, h)
end

function M:playSfx(name) return Sfx.playSfx(self.sfx, name) end

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

-- Vertical-centring nudge for text: the glyph box is the ink plus the padding under it (the GetText
-- geometry), so a box anchored at its middle sits its ink half the padding too high; widgets add this to
-- their middle line to centre the ink.
function M:textNudge(size)
    local f = self:gfont(size or 22)
    return math.floor((f.BoxHeight - math.ceil(f.LineHeight)) / 2) - 1
end

-- ── widgets ────────────────────────────────────────────────────────────────────────
local function canFocus(w) return w.focusable and w.visible and w.enabled end

function M:add(w)
    w:init(self)
    self.widgets[#self.widgets + 1] = w
    if canFocus(w) then self.focusables[#self.focusables + 1] = w end
    return w
end

function M:remove(w)
    for i = #self.widgets, 1, -1 do if self.widgets[i] == w then table.remove(self.widgets, i) end end
    if self._hoverW == w then self._hoverW = nil end
    if self._pressW == w then self._pressW = nil end
    self:_rebuildFocus()
end

-- drops every widget, releasing their canvases first
function M:clear()
    self:disposeWidgets()
    self.widgets = {}; self.focusables = {}; self.focusIdx = 0; self._focusW = nil; self._hoverW = nil; self._pressW = nil; self._captureWidget = nil
end

-- free every widget's baked GPU canvases (call before dropping/rebuilding the UI so re-entry doesn't leak;
-- LuaCanvas has no finalizer). GetText label textures are font-cache-owned and left untouched. Also frees the
-- lazily-created shared rect canvas (it's recreated on the next M:rect call, so this is safe to call mid-life).
function M:disposeWidgets()
    for _, w in ipairs(self.widgets) do if w.dispose then w:dispose() end end
    if self._rectCv then self._rectCv:Dispose(); self._rectCv = nil end
end

-- rebuilds the list keys can stop on (after a widget was enabled, disabled, shown or hidden). The focused
-- widget keeps the focus; when it can't hold it any more the focus goes to the next widget that can.
function M:_rebuildFocus()
    local was = self.focusables[self.focusIdx]
    self.focusables = {}
    local pick, after = 0, false
    for _, w in ipairs(self.widgets) do
        if w == was then after = true end
        if canFocus(w) then
            self.focusables[#self.focusables + 1] = w
            if w == was then pick = #self.focusables
            elseif after and pick == 0 then pick = #self.focusables end
        end
    end
    if was == nil then pick = math.min(self.focusIdx, #self.focusables)
    elseif pick == 0 then pick = #self.focusables end
    self.focusIdx = pick
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
function M:coins(o)   return self:add(self._coinsClass.new(o)) end

-- ── theming ──────────────────────────────────────────────────────────────────────
function M:setTheme(t)
    self.userTheme = t or {}
    self.theme = Theme.resolve(self.userTheme, nil, self.baseTheme)
    self._themes = setmetatable({}, { __mode = "k" })
    self._colors, self._colorCount = {}, 0
    if self._bg then self._bg = self:reuseCanvas(self._bg, 2, 2); self._bg:Clear(255, 255, 255, 255); self._bg:Upload() end
    for _, w in ipairs(self.widgets) do w:restyle() end
end

function M:setSfx(t)
    self.userSfx = t or {}
    self.sfx = Sfx.resolve(self.userSfx, nil)
    self._sfxs = setmetatable({}, { __mode = "k" })
    for _, w in ipairs(self.widgets) do w.sfx = self:resolveSfx(w.userSfx) end
end

-- ── focus navigation ────────────────────────────────────────────────────────────
function M:_setFocusIndex(i)
    if #self.focusables == 0 then self.focusIdx = 0; return end
    i = ((i - 1) % #self.focusables) + 1
    self.focusIdx = i
end

--- Moves the focus to widget w; false when w can't take it (hidden, disabled, not focusable).
function M:focus(w)
    for i, f in ipairs(self.focusables) do
        if f == w then self.focusIdx = i; return true end
    end
    return false
end

--- The widget holding the focus, or nil.
function M:focused() return self.focusables[self.focusIdx] end

function M:clearPrevFocus() if self._focusW then self._focusW:setFocus(false); self._focusW = nil end end
-- the device used last, shared by every SimpleUI of the script: the pointer only hovers while it is the mouse
local lastInputMouse = false

-- moves the key focus by dir (0 = stay) to the nearest widget keys may stop on: a widget made with
-- mouseOnly = true is hovered and clicked like any other, but the keys step over it
function M:_stepFocus(dir)
    local n = #self.focusables
    local i = self.focusIdx < 1 and 1 or self.focusIdx + dir
    for _ = 1, n do
        i = ((i - 1) % n) + 1
        if not self.focusables[i].mouseOnly then self.focusIdx = i; return end
        i = i + (dir < 0 and -1 or 1)
    end
    self.focusIdx = 0
end
function M:focusStay() self:_stepFocus(0) end
function M:focusNext() self:_stepFocus(1) end
function M:focusPrev() self:_stepFocus(-1) end

-- the gap between the ranges [a0, a1] and [b0, b1] (0 when they overlap)
local function gap(a0, a1, b0, b1)
    if b0 > a1 then return b0 - a1 elseif a0 > b1 then return a0 - b1 end
    return 0
end

-- moves the focus to the nearest widget in direction dir ("left" / "right" / "up" / "down"): ahead of the
-- focused one, closest along the direction and most in line with it. Returns false when there is none.
function M:_moveSpatial(dir)
    local fw = self.focusables[self.focusIdx]
    if fw == nil then self:focusStay(); return true end
    local fx0, fy0, fx1, fy1 = fw.x, fw.y, fw.x + fw.w, fw.y + fw.h
    local fcx, fcy = (fx0 + fx1) * 0.5, (fy0 + fy1) * 0.5
    local best, bestScore = nil, math.huge
    for i, w in ipairs(self.focusables) do
        if w ~= fw and not w.mouseOnly then
            local x0, y0, x1, y1 = w.x, w.y, w.x + w.w, w.y + w.h
            local cx, cy = (x0 + x1) * 0.5, (y0 + y1) * 0.5
            local along, across
            if dir == "right" then along, across = cx - fcx, gap(fy0, fy1, y0, y1)
            elseif dir == "left" then along, across = fcx - cx, gap(fy0, fy1, y0, y1)
            elseif dir == "down" then along, across = cy - fcy, gap(fx0, fx1, x0, x1)
            else along, across = fcy - cy, gap(fx0, fx1, x0, x1) end
            if along > 1 then
                local score = along + across * 3
                if score < bestScore then best, bestScore = i, score end
            end
        end
    end
    if best then self.focusIdx = best; return true end
    return false
end

--- True while the mouse is the device used last (moved, scrolled or pressed after the last key or pad input).
function M:mouseActive() return lastInputMouse end

function M:captureKeys(w) self._captureWidget = w end
function M:releaseKeys(w) if self._captureWidget == w then self._captureWidget = nil end end
--- True while a widget (e.g. a textbox) is eating keystrokes. Gate stage-level hotkeys on `not ui:isCapturing()`
--- so typing a letter that's also a hotkey doesn't trigger a menu/page action.
function M:isCapturing() return self._captureWidget ~= nil end

function M:_repeatKey(key, dt)
    local st = self._rep[key]
    if not st then st = { held = false, t = 0 }; self._rep[key] = st end
    local navPn = NavInput.getPn(self.navPlayer)
    if navPn[key]() then st.held = true; st.t = 0; return true end
    if st.held and navPn[key .. "Pressing"]() then
        st.t = st.t + dt
        if st.t >= HOLD_DELAY then st.t = st.t - HOLD_REPEAT; return true end
        return false
    end
    st.held = false; st.t = 0; return false
end

-- Up/Down (keyboard), or the pad's Left/Right that step through the widgets. The focused widget may use the
-- step itself (a list moving its selection) and lets it go at its ends; then the focus moves.
local VERTICAL = {
    { key = "navDown", both = "navDownOrPadRight", own = "onNavDown", pad = "onNavRight", any = "onNavDownOrPadRight", dir = "down", step = 1 },
    { key = "navUp", both = "navUpOrPadLeft", own = "onNavUp", pad = "onNavLeft", any = "onNavUpOrPadLeft", dir = "up", step = -1 },
}
-- Left/Right on the keyboard: the focused widget may use them (a slider, a chooser); otherwise they move
-- the focus in spatial navigation and keep it where it is in the default one
local HORIZONTAL = { { key = "navRight", own = "onNavRight", dir = "right" }, { key = "navLeft", own = "onNavLeft", dir = "left" } }

function M:_navigate(c)
    for _, v in ipairs(VERTICAL) do
        local fromKeys = c[v.key]
        if fromKeys or c[v.both] then
            local fw = self.focusables[self.focusIdx]
            local used = false
            if fw then
                local own = fromKeys and fw[v.own] or (not fromKeys and fw[v.pad])
                used = own and own(fw, not fromKeys) or false
                if not used then
                    local any = fw[v.any]
                    used = any and any(fw) or false
                end
            end
            if not used then
                if not (fromKeys and self.spatial and self:_moveSpatial(v.dir)) then self:_stepFocus(v.step) end
            end
            c[v.key], c[v.both] = false, false
        end
    end
    for _, v in ipairs(HORIZONTAL) do
        if c[v.key] then
            local fw = self.focusables[self.focusIdx]
            local own = fw and fw[v.own]
            if own and own(fw, false) then c[v.key] = false
            elseif not (self.spatial and self:_moveSpatial(v.dir)) then self:focusStay() end
        end
    end
    -- Decide and Cancel go to the focused widget first (a slider leaving its edit mode); what it leaves is
    -- handled after the focus is applied
    local fw = self.focusables[self.focusIdx]
    if c.decide then
        if fw and fw.onDecide and fw.onDecide(fw, false) then c.decide = false end
    end
    if c.cancel then
        fw = self.focusables[self.focusIdx]
        if fw and fw.onCancel and fw.onCancel(fw, false) then c.cancel = false end
    end
end

-- ── per-frame ───────────────────────────────────────────────────────────────────
function M:update(ts)
    local dt = (ts - self._lastTs) / 1000.0
    self._lastTs = ts
    if dt < 0 then dt = 0 elseif dt > 0.1 then dt = 0.1 end

    local c = self._ctx
    c.dt, c.ts = dt, ts
    c.mx, c.my = INPUT:GetMouseXY()
    c.mdx, c.mdy = INPUT:GetMouseDelta()
    c.moved = c.mdx ~= 0 or c.mdy ~= 0
    c.inside = INPUT:IsMouseInside()
    c.mPressed  = INPUT:MousePressed("Left")
    c.mPressing = INPUT:MousePressing("Left")
    c.mReleased = INPUT:MouseReleased("Left")
    c.scrollDx, c.scrollDy = INPUT:GetScrollDelta()
    local mouseUsed = c.moved or c.mPressed or c.mPressing or c.mReleased
        or (c.scrollDx or 0) ~= 0 or (c.scrollDy or 0) ~= 0
    -- one-frame mouse suppression: a caller (e.g. a stage dragging its own scrollbar) sets self._suppressMouse
    -- = true before ui:update so nothing under the cursor hovers/presses/clicks/scrolls/drags this frame. The
    -- ctx mouse fields are neutralised at the source (the hover scan, the press/release block, and every
    -- widget's own :update that reads the mouse — slider drag, list/chooser scroll). One-shot: cleared here.
    if self._suppressMouse then
        c.mx, c.my = -100000, -100000
        c.mdx, c.mdy = 0, 0
        c.moved, c.inside = false, false
        c.mPressed, c.mPressing, c.mReleased = false, false, false
        c.scrollDx, c.scrollDy = 0, 0
        self._suppressMouse = false
    end
    -- drop a stale capture if its widget was hidden/disabled (e.g. the page changed while typing)
    if self._captureWidget and (not self._captureWidget.visible or not self._captureWidget.enabled) then
        self._captureWidget = nil
    end
    local captured = self._captureWidget ~= nil
    local navPn = NavInput.getPn(self.navPlayer)
    c.decide = (not captured) and (navPn.decide() or INPUT:KeyboardPressed("Space")) or false
    c.cancel = navPn.cancel()
    c.navDown  = (not captured) and self:_repeatKey("down", dt) or false
    c.navDownOrPadRight = (not captured) and self:_repeatKey("downOrPadRight", dt) or false
    c.navUp    = (not captured) and self:_repeatKey("up", dt) or false
    c.navUpOrPadLeft = (not captured) and self:_repeatKey("upOrPadLeft", dt) or false
    c.navLeft  = (not captured) and self:_repeatKey("leftKeyboard", dt) or false
    c.navRight = (not captured) and self:_repeatKey("rightKeyboard", dt) or false

    -- the mouse takes over when it moves, scrolls or presses; a key or pad input takes it back. While the
    -- keys lead, the pointer's place is ignored, so a mouse left resting on a widget does not hover it
    if mouseUsed then lastInputMouse = true
    elseif c.decide or c.cancel or c.navDown or c.navDownOrPadRight or c.navUp or c.navUpOrPadLeft
        or c.navLeft or c.navRight then lastInputMouse = false end
    if not lastInputMouse then
        c.mx, c.my = -100000, -100000
        c.inside = false
        local fw = self.focusables[self.focusIdx]
        if fw and fw.mouseOnly then self:focusStay() end
    end
    c.mouseLead = lastInputMouse

    self.cancelRequested = false

    local justHoveredIdx
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
            -- mouse moves focus to the newly hovered widget
            for i, w in ipairs(self.focusables) do if w == hoverW then self.focusIdx = i; justHoveredIdx = i; break end end
        end
        self:_navigate(c)
    end

    -- apply focus flags from focusIdx
    local focusW = self.focusables[self.focusIdx]
    if focusW ~= self._focusW then
        if self._focusW then self._focusW:setFocus(false, justHoveredIdx == self.focusIdx) end
        if focusW then focusW:setFocus(true, not self._focusW or justHoveredIdx == self.focusIdx) end
        self._focusW = focusW
    end
    -- while the mouse leads, only the widget under the pointer shows the ring: the focus stays where it
    -- was, ready for the keys
    if self._mouseLead ~= lastInputMouse then
        self._mouseLead = lastInputMouse
        if self._focusW then self._focusW:_refreshHighlight() end
    end

    if not captured then
        -- pointer press/release
        if c.mPressed and self._hoverW then self._hoverW:press(); self._pressW = self._hoverW end
        if c.mReleased and self._pressW then
            local inside = self._pressW:hitTest(c.mx, c.my)
            self._pressW:release(inside); self._pressW = nil
        end
        -- keyboard/gamepad activate (single-shot squish→boing, not a same-frame press+release that cancels it)
        if c.decide then
            if focusW then focusW:keyActivate(); c.decide = false
            else self:focusStay()
            end
        end
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
