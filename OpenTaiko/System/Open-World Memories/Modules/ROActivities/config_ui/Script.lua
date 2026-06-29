---@diagnostic disable: undefined-global, lowercase-global, need-check-nil, undefined-field
-- config_ui — the settings menu, rendered with PopUI as a two-pane layout: a top row of category tabs, a
-- left pane of the active category's settings (per-type controls, grouped + scrollable), and a right pane
-- showing the focused setting's name + description (and the thumbnail for the skin setting).
-- The C# CStageConfig stage builds the model (a CLuaConfigModel) and hands it to activate(model); this script
-- renders it and calls the option mutators + key-config service back in C#. C# owns the config and saving.
--
-- model (via NLua): Options is a 0-based list (Options[i], Options.Count); each option exposes
-- .Category/.Section/.Kind/.Name/.Desc/.On/.Value/.Min/.Max/.Step/.Choices/.ChoiceCount/.Index/.KeyGroup/
-- .Thumbnails, :Display(), and the mutators :SetOn(b)/:SetValue(v)/:SetIndex(i)/:Activate(). model.Keys is the
-- key-config service; model.RequestExit()/model.PlaySfx(name) are callbacks.

local PopUI = require("PopUI")

local SW, SH = 1920, 1080

-- ── layout ──────────────────────────────────────────────────────────────────────────
local TAB_Y, TAB_H = 44, 64
local VX, VY       = 70, 150            -- left pane viewport top-left
local VW, VH       = 1020, SH - 150 - 50 -- left pane size
local PX, PW       = 1120, 730          -- right preview pane
local NAME_X       = VX + 34            -- setting-name column x
local HEADER_X     = VX + 16            -- section header x
local CTRL_RIGHT   = VX + VW - 30       -- every control right-aligns to this edge
local ROW_H, HEADER_H, ROW_GAP = 64, 50, 8
local W_SLIDER, VALUE_W, W_CHOOSER = 240, 96, 300   -- value_w reserves room so slider numbers stay clear of the scrollbar
local SB_X, SB_W = VX + VW - 9, 7                    -- scrollbar gutter
local NAME_MAXW    = 600
local descFont     = 22                 -- preview description font size
local nameFont     = 30                 -- preview heading font size
local TRANSP       = { 255, 255, 255, 0 }   -- transparent text backcolor
local DESC_COLOR   = { 64, 76, 96, 255 }

-- ── settings theme (cool slate / steel-blue) ─────────────────────────────────────────
local THEME = {
    name = "ConfigCalm",
    colors = {
        bg           = { 232, 238, 245, 255 },
        surface      = { 250, 252, 255, 255 },
        surface2     = { 230, 236, 244, 255 },
        primary      = { 96, 132, 180, 255 },
        primary2     = { 72, 108, 156, 255 },
        accent       = { 126, 196, 200, 255 },
        accent2      = { 96, 168, 176, 255 },
        outline      = { 70, 84, 104, 255 },
        text         = { 44, 56, 74, 255 },
        textOnAccent = { 255, 255, 255, 255 },
        textDisabled = { 150, 162, 178, 255 },
        shadow       = { 40, 52, 72, 70 },
        gloss        = { 255, 255, 255, 90 },
        focusRing    = { 120, 184, 232, 255 },
        track        = { 214, 222, 232, 255 },
    },
    radius = 22, radiusSmall = 12, outlineWidth = 4, gloss = true,
    shadow = { dx = 0, dy = 5, layers = 4, grow = 3 },
}

-- ── module state ──────────────────────────────────────────────────────────────────
ui   = nil   -- PopUI manager
mode = nil   -- "main" (a category / keys-index page) | "bind" (a key-binding capture page)

local M               -- the model
local tabs            -- the category tabs + keys tab + trailing exit tab
local nTabs           -- #tabs
local keysTabIndex    -- index of the keys tab (exit is the last tab)
local catPages        -- catPages[catId]  = page
local keysIndexPage   -- the keys tab page (lists the key-config rows)
local bindPages       -- bindPages[group] = the key-binding page for a group
local current         -- the page being shown + scrolled
local activeTabIndex
local previewPanel
local keysBack, keysTitle
local descCache       -- { key, title, nameTex, descTex }
local bgCanvas
local scrollTarget, scrollCur = 0, 0
local lastTs = 0
local lastFocusIdx = -1   -- only re-snap the scroll to the focused row when focus changes, so the wheel scrolls freely
local wasCapturing = false
local captureBtn = nil
local lastThumbIdx, lastThumbOpt = nil, nil
local thumbWantIdx, thumbWantOpt, thumbWantSince = nil, nil, 0   -- debounce: load the skin thumbnail after the selection settles
local exitRequested = false   -- set by the exit tab; update() returns "exit" next
local sbDrag = false          -- dragging the scrollbar thumb

local floor, min, max = math.floor, math.min, math.max
local function lerp(a, b, t) return a + (b - a) * t end
local function clamp(v, lo, hi) if v < lo then return lo elseif v > hi then return hi else return v end end

-- ── skin locale ─────────────────────────────────────────────────────────────────────
-- option name/desc text arrives localized from C#; the menu chrome below (tabs, buttons, headers, prompts) is
-- authored here, so it is translated through the skin locale. the global THEME is the engine's LuaThemeFunc
-- (its GetSkinString reads the skin's Locales/<lang>.json) — distinct from the local THEME colour table above,
-- which shadows it in this file. tr(key, fallback) returns the skin string, or the fallback when the skin has
-- no such key (GetSkinString returns a "[LOCALE NOT FOUND: key]" sentinel, which we reject).
local function tr(key, fallback)
    local SL = _G.THEME
    if SL then
        local ok, s = pcall(SL.GetSkinString, SL, key)
        if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    end
    return fallback
end

-- ── small helpers ─────────────────────────────────────────────────────────────────
local function copyChoices(opt)
    local t = {}
    for i = 0, opt.ChoiceCount - 1 do t[#t + 1] = opt.Choices[i] end
    return t
end

local function groupTitle(group)
    if group == "system" then return tr("SETTINGS_UI_KEYS_GROUP_SYSTEM", "System Input Settings")
    elseif group == "drums" then return tr("SETTINGS_UI_KEYS_GROUP_GAME", "Gameplay Input Settings")
    elseif group == "training" then return tr("SETTINGS_UI_KEYS_GROUP_TRAINING", "Training Mode Input Settings") end
    return tr("SETTINGS_UI_KEYS_GROUP_DEFAULT", "Input Settings")
end

local function activeTab() return tabs[activeTabIndex] end

local function setFocusTo(w)
    if not w then return end
    for i, fw in ipairs(ui.focusables) do if fw == w then ui.focusIdx = i; return end end
end

local function requestExit() exitRequested = true end

function focusedOpt()
    local fw = ui.focusables[ui.focusIdx]
    if fw and fw._opt then return fw._opt end
    return nil
end

-- ── control factory: the widget for each option kind ──────────────────────────────
-- returns (widget, customDisplay); customDisplay = true means the int's :Display() differs from the raw number,
-- so we hide the slider value and draw :Display() ourselves.
local function makeControl(opt)
    local kind = opt.Kind
    if kind == "Toggle" then
        local w = ui:toggle{ value = opt.On, text = "", gap = 0, w = 92,
            onChange = function(v) opt:SetOn(v) end }
        return w, false
    elseif kind == "Int" and opt.TextInput then
        -- number-only text box (type the value), e.g. a port
        local maxLen = #tostring(opt.Max)
        local w = ui:textbox{ w = 200, h = 56, value = tostring(opt.Value), maxLen = maxLen,
            onChange = function(t, box)
                local digits = t:gsub("%D", "")            -- strip non-digits as you type
                if digits ~= t and box._ti then box._ti.Text = digits end
                if digits ~= "" then opt:SetValue(tonumber(digits)) end   -- SetValue clamps to [Min,Max]
            end,
            onSubmit = function(_, box) box.value = tostring(opt.Value) end }   -- snap display to the clamped value
        w._isTextInput = true
        return w, false
    elseif kind == "Int" then
        local custom = (opt:Display() ~= tostring(opt.Value))
        local w = ui:slider{ w = W_SLIDER, h = 40, min = opt.Min, max = opt.Max, step = opt.Step,
            value = opt.Value, showValue = not custom,
            onChange = function(v) opt:SetValue(floor(v + 0.5)) end }
        return w, custom
    elseif kind == "Choice" then
        local w = ui:chooser{ w = W_CHOOSER, h = 56, options = copyChoices(opt), index = opt.Index + 1,
            wrap = true, onChange = function(i) opt:SetIndex(i - 1) end }
        return w, false
    elseif kind == "Action" then
        local w = ui:button{ text = tr("SETTINGS_UI_RUN", "Run"), h = 56, onClick = function() opt:Activate() end }
        return w, false
    elseif kind == "KeyConfig" then
        local w = ui:button{ text = tr("SETTINGS_UI_CONFIGURE", "Configure"), h = 56,
            onClick = function() enterKeys(opt.KeyGroup ~= "" and opt.KeyGroup or "system") end }
        return w, false
    end
    return ui:button{ text = "?", h = 56 }, false
end

-- right-align the control and tag it for focus/preview/scroll
local function placeControl(ctrl, kind, entry, opt)
    local x
    if kind == "Int" and not opt.TextInput then x = CTRL_RIGHT - VALUE_W - W_SLIDER
    else x = CTRL_RIGHT - ctrl.w end
    ctrl.x = floor(x)
    ctrl._entry = entry
    ctrl._opt = opt
    -- up from the first row of a category page goes to the active tab (in main mode; in bind mode it falls
    -- through to focusPrev → the back button)
    ctrl.onNavUp = function(self)
        if mode == "main" and current and self == current.firstCtrl then setFocusTo(activeTab()); return true end
        return false
    end
    -- down from the last row wraps up to the tab strip (two-layer nav: tab strip <-> rows, both ways)
    ctrl.onNavDown = function(self)
        if mode == "main" and current and self == current.lastCtrl then setFocusTo(activeTab()); return true end
        return false
    end
end

-- ── page building ────────────────────────────────────────────────────────────────
local function layoutPage(page)
    local y = 0
    for _, e in ipairs(page.entries) do
        if e.kind == "header" then e.h = HEADER_H; e.baseY = y; y = y + HEADER_H
        else e.h = ROW_H; e.baseY = y; y = y + ROW_H + ROW_GAP end
    end
    page.contentH = y + 16   -- breathing room so the last row clears the pane edge
end

local function addHeader(page, text)
    local lbl = ui:label{ text = text, x = HEADER_X, size = "label", color = THEME.colors.accent2, maxWidth = VW }
    lbl:setVisible(false)
    page.widgets[#page.widgets + 1] = lbl
    page.entries[#page.entries + 1] = { kind = "header", label = lbl }
end

local function addOptionRow(page, opt, nameOverride)
    local nameLbl = ui:label{ text = nameOverride or opt.Name, x = NAME_X, size = "label",
        color = THEME.colors.text, maxWidth = NAME_MAXW }
    nameLbl:setVisible(false)
    local ctrl, custom = makeControl(opt)
    ctrl:setVisible(false)
    local entry = { kind = "option", nameLabel = nameLbl, ctrl = ctrl, opt = opt, customDisplay = custom }
    placeControl(ctrl, opt.Kind, entry, opt)
    page.widgets[#page.widgets + 1] = nameLbl
    page.widgets[#page.widgets + 1] = ctrl
    page.entries[#page.entries + 1] = entry
    if not page.firstCtrl then page.firstCtrl = ctrl end
    page.lastCtrl = ctrl
end

local function newPage()
    return { entries = {}, widgets = {}, firstCtrl = nil, contentH = 0 }
end

local function buildCatPage(catId)
    local page = newPage()
    local lastSection = nil
    for i = 0, M.Options.Count - 1 do
        local opt = M.Options[i]
        if opt.Category == catId and opt.Kind ~= "KeyConfig" then
            if opt.Section ~= "" and opt.Section ~= lastSection then
                addHeader(page, opt.Section); lastSection = opt.Section
            end
            addOptionRow(page, opt)
        end
    end
    layoutPage(page)
    return page
end

local function buildKeysIndexPage()
    local page = newPage()
    addHeader(page, tr("SETTINGS_UI_KEYS_INDEX_HEADER", "Input Settings"))
    for i = 0, M.Options.Count - 1 do
        local opt = M.Options[i]
        if opt.Kind == "KeyConfig" then addOptionRow(page, opt) end
    end
    layoutPage(page)
    return page
end

-- a key-binding row: name + a button showing the current binding; decide/click starts capture
local function addBindRow(page, act)
    local nameLbl = ui:label{ text = act.Name, x = NAME_X, size = "label", color = THEME.colors.text, maxWidth = NAME_MAXW }
    nameLbl:setVisible(false)
    -- an action can hold several bindings: the button shows them all; decide adds one into the next free slot,
    -- delete (handled in update) removes the most recent
    local btn = ui:button{ text = M.Keys:GetAllBindings(act), h = 56, w = 420,
        onClick = function(self)
            captureBtn = self
            local slot = M.Keys:FirstFreeSlot(act); if slot < 0 then slot = 0 end   -- full -> overwrite slot 0
            -- refresh via the capture callback and via the IsCapturing transition
            M.Keys:StartCapture(act, slot, function(ok) if ok then self:setText(M.Keys:GetAllBindings(act)); descCache = nil end end)
        end }
    btn:setVisible(false)
    local entry = { kind = "option", nameLabel = nameLbl, ctrl = btn, opt = { Name = act.Name, Desc = act.Desc }, customDisplay = false }
    btn.x = floor(CTRL_RIGHT - btn.w)
    btn._entry = entry
    btn._opt = entry.opt
    btn._act = act
    btn.onNavUp = function(self)
        if mode == "main" and current and self == current.firstCtrl then setFocusTo(activeTab()); return true end
        return false
    end
    page.widgets[#page.widgets + 1] = nameLbl
    page.widgets[#page.widgets + 1] = btn
    page.entries[#page.entries + 1] = entry
    if not page.firstCtrl then page.firstCtrl = btn end
    page.lastCtrl = btn
end

local function buildBindPage(group)
    local page = newPage()
    local actions = M.Keys:ListActions(group)
    local lastGroup = nil
    for i = 0, actions.Count - 1 do
        local act = actions[i]
        if act.Group ~= "" and act.Group ~= lastGroup then addHeader(page, act.Group); lastGroup = act.Group end
        addBindRow(page, act)
    end
    layoutPage(page)
    return page
end

-- ── page show / tab switching ───────────────────────────────────────────────────────
local function hideAllPages()
    for _, p in pairs(catPages) do for _, w in ipairs(p.widgets) do w:setVisible(false) end end
    if keysIndexPage then for _, w in ipairs(keysIndexPage.widgets) do w:setVisible(false) end end
    for _, p in pairs(bindPages) do for _, w in ipairs(p.widgets) do w:setVisible(false) end end
end

local function showPage(page)
    hideAllPages()
    for _, w in ipairs(page.widgets) do w:setVisible(true) end
    current = page
    scrollTarget, scrollCur = 0, 0
    descCache = nil
    lastFocusIdx = -1   -- force one ensureFocusedVisible after the page changes
    ui:_rebuildFocus()
end

local function setTabsVisible(b) for _, t in ipairs(tabs) do t:setVisible(b) end end

local function restyleTabs()
    for i, t in ipairs(tabs) do
        local want = (i == activeTabIndex)
        if t.accent ~= want then t.accent = want; t:restyle() end
    end
end

function switchTab(i)
    if tabs[i] and tabs[i]._exit then requestExit(); return end   -- never switch to the exit tab; it just leaves
    activeTabIndex = i
    mode = "main"
    keysBack:setVisible(false); keysTitle:setVisible(false)
    setTabsVisible(true)
    -- build the page lazily on first view (see the note in activate)
    if tabs[i]._catId then
        local catId = tabs[i]._catId
        if not catPages[catId] then catPages[catId] = buildCatPage(catId) end
        showPage(catPages[catId])
    else
        if not keysIndexPage then keysIndexPage = buildKeysIndexPage() end
        showPage(keysIndexPage)
    end
    restyleTabs()
    setFocusTo(tabs[i])
end

function enterKeys(group)
    if not bindPages[group] then bindPages[group] = buildBindPage(group) end
    mode = "bind"
    setTabsVisible(false)
    keysBack:setVisible(true)
    keysTitle:setVisible(true); keysTitle:setText(groupTitle(group))
    showPage(bindPages[group])
    setFocusTo(current.firstCtrl or keysBack)
end

local function backToKeysIndex()
    keysBack:setVisible(false); keysTitle:setVisible(false)
    switchTab(keysTabIndex)   -- back to the keys tab, not the exit tab
end

function enterMain() if mode == "bind" then backToKeysIndex() end end

-- ── scrolling + per-frame row placement ──────────────────────────────────────────
local function ensureFocusedVisible()
    if not current then return end
    local fw = ui.focusables[ui.focusIdx]
    if not (fw and fw._entry) then return end
    local e = fw._entry
    if e.baseY < scrollTarget then scrollTarget = e.baseY end
    local bottom = e.baseY + e.h
    if bottom > scrollTarget + VH then scrollTarget = bottom - VH end
end

local function reposition(dt)
    if not current then return end
    local maxS = max(0, current.contentH - VH)
    scrollTarget = clamp(scrollTarget, 0, maxS)
    scrollCur = scrollCur + (scrollTarget - scrollCur) * min(1, dt * 16)
    for _, e in ipairs(current.entries) do
        local sy = VY + e.baseY - scrollCur
        -- let rows scroll past the pane top/bottom (they get masked in draw for a clean cut); cull only when fully off
        local onscreen = (sy + e.h >= 0) and (sy <= VY + VH)
        if e.kind == "header" then
            e.label.y = onscreen and floor(sy + (HEADER_H - e.label.h) * 0.5) or -10000
        else
            local rowCenter = sy + ROW_H * 0.5
            if onscreen then
                e.nameLabel.y = floor(rowCenter - e.nameLabel.h * 0.5)
                e.ctrl.y = floor(rowCenter - e.ctrl.h * 0.5)
            else
                e.nameLabel.y = -10000; e.ctrl.y = -10000
            end
        end
    end
end

-- scrollbar geometry: returns maxScroll, thumbY, thumbH (or nil when the content fits)
local function scrollMetrics()
    if not current or not current.contentH or current.contentH <= VH then return nil end
    local maxS = max(0, current.contentH - VH)
    local thumbH = max(40, VH * VH / current.contentH)
    local t = (maxS > 0) and clamp(scrollCur / maxS, 0, 1) or 0
    local thumbY = VY + (VH - thumbH) * t
    return maxS, thumbY, thumbH
end

-- drag/click the scrollbar to scroll. runs before ui:update so a drag can set ui._suppressMouse and stop the
-- rows underneath from hovering/clicking as the cursor moves.
local function handleScrollbar()
    local maxS, _, thumbH = scrollMetrics()
    if not maxS then sbDrag = false; return end
    local mx, my = INPUT:GetMouseXY()
    local overTrack = mx >= SB_X - 6 and mx <= SB_X + SB_W + 6 and my >= VY and my <= VY + VH
    if INPUT:MousePressed("Left") and overTrack then sbDrag = true end
    if not INPUT:MousePressing("Left") then sbDrag = false end
    if sbDrag then
        ui._suppressMouse = true   -- the drag owns the mouse this frame; no hover/press on the rows
        local t = clamp((my - VY - thumbH * 0.5) / max(1, VH - thumbH), 0, 1)
        scrollTarget = t * maxS
    end
end

-- focus a tab: category/keys tabs switch their page on focus; the exit tab only highlights (decide/click on it
-- exits), so cycling onto it never exits by accident
local function focusTab(i)
    if tabs[i]._exit then setFocusTo(tabs[i]) else switchTab(i) end
end

-- horizontal arrows (or LBlue/RBlue) move between tabs, only while a tab is focused, and wrap both ways across
-- all tabs including the exit tab
local function handleTabKeys()
    local fw = ui.focusables[ui.focusIdx]
    if not (fw and fw._tab) then return end
    local right = INPUT:KeyboardPressed("RightArrow") or INPUT:Pressed("RBlue")
    local left  = INPUT:KeyboardPressed("LeftArrow") or INPUT:Pressed("LBlue")
    if right then focusTab(fw._tab % nTabs + 1)
    elseif left then focusTab((fw._tab - 2) % nTabs + 1) end
end

-- ── gradient background ───────────────────────────────────────────────────────────
local function bakeGradient()
    local H = 256
    bgCanvas = CANVAS:CreateCanvas(2, H)
    for iy = 0, H - 1 do
        local t = iy / (H - 1)
        bgCanvas:FillRect(0, iy, 2, 1, floor(lerp(226, 202, t)), floor(lerp(233, 213, t)), floor(lerp(243, 228, t)), 255)
    end
    bgCanvas:Upload()
end

local function drawGradient()
    if not bgCanvas then return end
    bgCanvas:SetScale(SW / 2, SH / bgCanvas.Height)
    bgCanvas:Draw(0, 0)
end

-- ── preview pane (focused setting's name + description + skin thumbnail) ──────────────
local function currentTitleDesc()
    local opt = focusedOpt()
    if opt then return opt.Name, opt.Desc, opt end
    if mode == "bind" then return keysTitle.text, tr("SETTINGS_UI_KEYS_BIND_DESC", "Pick an action. Each can hold several keys: Enter adds one, Delete removes the most recent."), nil end
    if activeTabIndex and activeTabIndex <= 3 then
        return M.CategoryLabels[activeTabIndex - 1], M.CategoryDescs[activeTabIndex - 1], nil
    end
    return tr("SETTINGS_UI_KEYS", "Input Settings"), tr("SETTINGS_UI_KEYS_PREVIEW_DESC", "Configure your controls."), nil
end

local function drawThumb(opt, x, y, w, h)
    if opt.Thumbnails == nil or not opt.Thumbnails.Length or opt.Thumbnails.Length == 0 then return end
    local idx = opt.Index
    -- debounce: only (re)load the thumbnail after the selection has been stable ~150 ms. each load decodes a
    -- full image, so loading one for every skin you scroll past would spike memory.
    if idx ~= thumbWantIdx or opt ~= thumbWantOpt then thumbWantIdx, thumbWantOpt, thumbWantSince = idx, opt, lastTs end
    if (idx ~= lastThumbIdx or lastThumbOpt ~= opt) and (lastTs - thumbWantSince) >= 150 then
        SHARED:SetSharedTextureUsingAbsolutePath("config_skin_thumb", opt.Thumbnails[idx])
        lastThumbIdx, lastThumbOpt = idx, opt
    end
    local tex = SHARED:GetSharedTexture("config_skin_thumb")
    if tex and tex.Width and tex.Width > 0 and tex.Height > 0 then
        local scale = min(w / tex.Width, h / tex.Height)
        local dw, dh = tex.Width * scale, tex.Height * scale
        tex:SetScale(scale, scale)
        tex:Draw(floor(x + (w - dw) * 0.5), floor(y))
    end
end

local function drawPreview()
    local innerX, innerY, innerW = PX + 34, VY + 34, PW - 68
    local title, desc, opt = currentTitleDesc()
    local key = opt or mode
    if not descCache or descCache.key ~= key or descCache.title ~= title then
        descCache = {
            key = key, title = title,
            nameTex = ui:renderText(nameFont, title or "", THEME.colors.primary2, TRANSP, false, innerW),
            descTex = (desc and desc ~= "") and ui:renderText(descFont, desc, DESC_COLOR, TRANSP, false, innerW) or nil,
        }
    end
    local y = innerY
    if descCache.nameTex then descCache.nameTex:Draw(innerX, y); y = y + (descCache.nameTex.Height or 30) + 22 end
    if descCache.descTex then descCache.descTex:Draw(innerX, y); y = y + (descCache.descTex.Height or 20) + 28 end
    if opt and opt.Thumbnails ~= nil then drawThumb(opt, innerX, y, innerW, 380) end
    -- multi-bind hint while configuring keys
    if mode == "bind" then
        ui:drawText(18, tr("SETTINGS_UI_KEYS_MULTIBIND_HINT", "Enter: add a key    Delete: remove the last"), innerX, VY + VH - 56, { 150, 162, 178 })
    end
end

-- int values whose :Display() differs from the raw number, drawn next to the slider
local function drawCustomValues()
    if not current then return end
    local sz = ui.theme.font.small
    for _, e in ipairs(current.entries) do
        if e.kind == "option" and e.customDisplay and e.ctrl.y > -9000 then
            local cy = e.ctrl.y + e.ctrl.h * 0.5
            ui:drawText(sz, e.opt:Display(), e.ctrl.x + e.ctrl.w + 18, floor(cy - ui:textHeight(sz) * 0.5), ui.theme.colors.text)
        end
    end
end

-- ── lifecycle ─────────────────────────────────────────────────────────────────────
function onStart() end

function activate(model)
    M = model
    mode = "main"; descCache = nil; scrollTarget, scrollCur = 0, 0; lastTs = 0
    wasCapturing = false; captureBtn = nil; lastThumbIdx, lastThumbOpt = nil, nil
    thumbWantIdx, thumbWantOpt, thumbWantSince = nil, nil, 0
    catPages = {}; bindPages = {}; tabs = {}; current = nil

    ui = PopUI.new{ theme = THEME, bg = false }
    ui:_prewarm(ui.theme.font.small)
    ui:_prewarm(ui.theme.font.button)
    bakeGradient()

    -- build the tabs first so they become the leading focusables (categories, then the keys tab)
    local tabLabels = {}
    for i = 0, M.CategoryLabels.Count - 1 do tabLabels[#tabLabels + 1] = M.CategoryLabels[i] end
    tabLabels[#tabLabels + 1] = tr("SETTINGS_UI_KEYS", "Input Settings")
    local tx = VX
    local function tabNavDown() if mode == "main" and current and current.firstCtrl then setFocusTo(current.firstCtrl); return true end return false end
    local function tabNavUp() if mode == "main" and current and current.lastCtrl then setFocusTo(current.lastCtrl); return true end return true end
    for i, label in ipairs(tabLabels) do
        local catId = (i <= M.Categories.Count) and M.Categories[i - 1] or nil
        local t = ui:button{ text = label, y = TAB_Y, h = TAB_H, onClick = function() switchTab(i) end }
        t._tab = i; t._catId = catId
        t.onNavDown = tabNavDown
        t.onNavUp = tabNavUp   -- up from a tab wraps to the last row
        t.x = floor(tx); tx = tx + t.w + 16
        tabs[i] = t
    end
    keysTabIndex = #tabs   -- the keys tab is the last category-strip entry (exit is appended after)
    -- trailing exit tab: decide/click leaves the config; cycling onto it just highlights it
    do
        local i = #tabs + 1
        local t = ui:button{ text = tr("SETTINGS_UI_EXIT", "Exit"), y = TAB_Y, h = TAB_H, onClick = function() requestExit() end }
        t._tab = i; t._exit = true
        t.onNavDown = tabNavDown
        t.onNavUp = tabNavUp
        t.x = floor(tx); tx = tx + t.w + 16
        tabs[i] = t
    end
    nTabs = #tabs

    -- keys-mode chrome, shown only while binding keys
    keysBack = ui:button{ text = tr("SETTINGS_UI_BACK", "< Back"), x = VX, y = TAB_Y, h = TAB_H, onClick = backToKeysIndex }
    keysBack:setVisible(false)
    keysTitle = ui:label{ text = "", x = floor(keysBack.x + keysBack.w + 40), y = TAB_Y + 18, size = "title", color = THEME.colors.text }
    keysTitle:setVisible(false)

    -- right preview pane
    previewPanel = ui:panel{ x = PX, y = VY, w = PW, h = VH }

    -- pages are built lazily on first view (switchTab builds a category/keys page, enterKeys builds a bind page),
    -- so opening settings doesn't build every page's widgets up-front
    switchTab(1)   -- builds + shows the first category only
end

function reload(model)
    if ui then ui:disposeWidgets(); ui:clear() end
    if bgCanvas then bgCanvas:Dispose(); bgCanvas = nil end
    activate(model)   -- language changed: rebuild with the new localized strings
end

function update(ts)
    if M == nil then return end

    -- key capture is owned by C#; don't run the UI input while it polls
    if M.Keys ~= nil and M.Keys.IsCapturing then wasCapturing = true; return end
    if wasCapturing then
        wasCapturing = false
        if captureBtn and captureBtn._act then captureBtn:setText(M.Keys:GetAllBindings(captureBtn._act)) end
        descCache = nil
    end

    local dt = (ts - lastTs) / 1000.0
    lastTs = ts
    if dt < 0 then dt = 0 elseif dt > 0.1 then dt = 0.1 end

    -- while a textbox is eating keystrokes (e.g. the port field) don't run the config-level input — the wheel,
    -- scrollbar drag, tab arrows and delete/backspace would all fight what's being typed
    local typing = ui:isCapturing()

    if not typing then
        -- mouse wheel: read before ui:update (which drains the scroll accumulator)
        local _, sdy = INPUT:GetScrollDelta()
        if sdy and sdy ~= 0 then
            local mx, my = INPUT:GetMouseXY()
            if INPUT:IsMouseInside() and mx >= VX and mx <= VX + VW and my >= VY and my <= VY + VH then
                scrollTarget = scrollTarget - sdy * 60
            end
        end
        handleScrollbar()  -- before ui:update: may set ui._suppressMouse so a drag doesn't hover/click rows
    end

    local r = ui:update(ts)

    if not typing then
        handleTabKeys()
        -- bind mode: delete/backspace clears the most-recently-added binding of the focused action
        if mode == "bind" and (INPUT:KeyboardPressed("Delete") or INPUT:KeyboardPressed("Backspace")) then
            local fw = ui.focusables[ui.focusIdx]
            if fw and fw._act then
                local slot = M.Keys:LastBoundSlot(fw._act)
                if slot >= 0 then M.Keys:ClearBinding(fw._act, slot) end
                fw:setText(M.Keys:GetAllBindings(fw._act)); descCache = nil
            end
        end
    end

    -- the exit tab (decide/click) leaves the config
    if exitRequested then
        exitRequested = false
        if M.RequestExit then M.RequestExit() end
        return "exit"
    end

    -- keep number-input boxes showing the clamped model value when not being edited (click-away / esc leave the
    -- raw typed text, so re-sync from opt.Value once editing ends)
    if current then
        for _, e in ipairs(current.entries) do
            if e.ctrl and e.ctrl._isTextInput and not e.ctrl._capturing then e.ctrl.value = tostring(e.opt.Value) end
        end
    end

    -- keep the focused row in view only when focus changed, so the wheel can scroll all the way down
    if ui.focusIdx ~= lastFocusIdx then ensureFocusedVisible(); lastFocusIdx = ui.focusIdx end
    reposition(dt)

    if r == "cancel" then
        if mode == "bind" then backToKeysIndex()
        else if M.RequestExit then M.RequestExit() end; return "exit" end
    end
end

function draw()
    if M == nil then return end
    drawGradient()
    ui:draw()             -- tabs (bottom z) + rows (top z) + preview panel
    drawCustomValues()    -- slider :Display() values, at row positions

    -- header mask: cover the area above the pane top so rows that scrolled up are cut cleanly (no abrupt pop, no
    -- overlap with the tab strip). the colour matches the gradient at y=VY for a seamless seam.
    local mt = VY / SH
    ui:rect(0, 0, SW, VY, lerp(226, 202, mt), lerp(233, 213, mt), lerp(243, 228, mt), 255)
    -- footer mask: same at the bottom edge. limited to the left pane's x-range so it never clips the preview shadow.
    local mb = (VY + VH) / SH
    ui:rect(0, VY + VH, VX + VW, SH - (VY + VH), lerp(226, 202, mb), lerp(233, 213, mb), lerp(243, 228, mb), 255)
    -- redraw the chrome on top of the mask
    for _, t in ipairs(tabs) do if t.visible then t:draw() end end
    if keysBack.visible then keysBack:draw() end
    if keysTitle.visible then keysTitle:draw() end

    -- scrollbar (track + thumb) in the right gutter
    local maxS, thumbY, thumbH = scrollMetrics()
    if maxS then
        ui:rect(SB_X, VY, SB_W, VH, 70, 84, 104, 70)
        ui:rect(SB_X, thumbY, SB_W, thumbH, sbDrag and 200 or 150, 200, 232, sbDrag and 255 or 220)
    end

    drawPreview()
    if M.Keys ~= nil and M.Keys.IsCapturing then
        local tex = ui:renderText(28, tr("SETTINGS_UI_KEYS_CAPTURE_PROMPT", "Press a key...  (Esc to cancel)"), { 230, 150, 70, 255 }, TRANSP, false, PW - 68)
        tex:Draw(PX + 34, VY + VH - 110)
    end
end

function deactivate()
    if ui then ui:disposeWidgets(); ui:clear() end
    if bgCanvas then bgCanvas:Dispose(); bgCanvas = nil end
    if SHARED ~= nil then SHARED:ClearSharedTexture("config_skin_thumb") end
end

function afterSongEnum() end
function onDestroy() end
