---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- dialogue.lua — a reusable typewriter dialogue box (speaker name + portrait area + word-wrapped
-- typewriter text + choices), with inline command tags for flavour. Self-driven: the stage calls
-- dlg:update(dt) and dlg:draw() each frame and reads dlg.result when it reports "done".
--
-- Script = array of nodes: { name=, text=, portrait=, choices={ {label=, value=}, ... } }
--   text may contain inline tags executed as the typewriter reaches them:
--     {shake:amp,dur}  screen shake (via GLOBALCAMERA)   {pause:sec}  hold        {speed:cps}  retype speed
--     {sfx:name}       calls opts.onSfx(name)            {voice:name} calls opts.onVoice(name)
--     {expr:name}      calls opts.onExpr(name)           -- e.g. swap the speaker's expression mid-line
-- A node with `choices` shows them once the text finishes; selecting one sets dlg.result and ends.
--
-- Dialogue.new(opts): opts = {
--   fonts={name=,text=},            -- classic path: LuaText fonts (per-character byte atlas, ASCII)
--   gfont=,  gfontName=,            -- OR glyph path: LuaGlyphText objects (TEXT:CreateGlyphCached; UTF-8,
--                                   --   word wrap via WrapToLines). gfontName falls back to gfont.
--   ui="plain"|"popui",             -- box style: classic flat panel, or a PopUI-Shape rounded/shadowed box
--   theme={...},                    -- popui box colours (see THEME_DEFAULT below), all optional
--   box={x=,y=,w=,h=},              -- geometry override (defaults preserved)
--   portraitSize=,                  -- 0 hides the portrait area (e.g. when big VN sprites are drawn instead)
--   mouse=true,                     -- also advance / confirm choices with left click
--   drawBox=false,                  -- skip the box/name chrome
--   advanceInput=fn,                -- extra advance predicate (e.g. the drum Decide pad)
--   cps=28,                         -- typewriter speed each node starts at (default 42; {speed:} overrides)
--   onSfx=fn, onVoice=fn, onExpr=fn, portraits={name=LuaTexture} }

local floor = math.floor
local Dialogue = {}
Dialogue.__index = Dialogue

local SCREEN_W, SCREEN_H = 1920, 1080
local DEF_BOX_X, DEF_BOX_W = 90, 1740
local DEF_BOX_Y, DEF_BOX_H = 770, 250
local DEF_PORTRAIT = 210                      -- portrait square size
local LINE_GAP = 8
local DEFAULT_CPS = 42

local THEME_DEFAULT = {
    face    = { 252, 248, 244, 255 },   -- box face (top of gradient; opaque — canvas bakes overwrite)
    face2   = { 244, 231, 233, 255 },   -- box face (bottom)
    outline = { 108, 82, 94, 255 },
    namePill  = { 233, 116, 152, 255 },
    namePill2 = { 214, 88, 128, 255 },
    nameText  = { 255, 255, 255 },
    text      = { 68, 52, 60 },
    choiceRow = { 255, 255, 255, 235 },
    choiceSel = { 250, 214, 226, 255 },
    choiceText = { 96, 72, 84 },
    caret     = { 222, 120, 152 },
    shadow    = { 70, 40, 55, 90 },
}

local UTF8_PAT = "[%z\1-\127\194-\244][\128-\191]*"

-- ── per-character glyph cache for the CLASSIC (LuaText) path — unchanged behaviour ──
local function atlasFor(self, font)
    local a = self._atlas[font]
    if not a then
        local W = COLOR:CreateColorFromRGBA(255, 255, 255, 255)
        local B = COLOR:CreateColorFromRGBA(0, 0, 0, 255)
        local w1 = font:GetText("8", false, 1800, W, B).Width
        local w2 = font:GetText("88", false, 1800, W, B).Width
        a = { font = font, glyphs = {}, pad = 2 * w1 - w2, h = font:GetText("8", false, 1800, W, B).Height }
        self._atlas[font] = a
    end
    return a
end
local function glyph(self, a, ch)
    local g = a.glyphs[ch]
    if not g then
        local tex = a.font:GetText(ch, false, 1800, COLOR:CreateColorFromRGBA(255, 255, 255, 255), COLOR:CreateColorFromRGBA(0, 0, 0, 255))
        g = { tex = tex, adv = tex.Width - a.pad, col = -1 }
        a.glyphs[ch] = g
    end
    return g
end

function Dialogue.new(opts)
    opts = opts or {}
    local self = setmetatable({}, Dialogue)
    self.fonts = opts.fonts or {}
    self.gfont = opts.gfont
    self.gfontName = opts.gfontName or opts.gfont
    self.ui = opts.ui or "popui"   -- the rounded PopUI-style chrome is the default skin-wide look
                                   -- (pass ui="plain" for the old flat panel)
    self.theme = {}
    for k, v in pairs(THEME_DEFAULT) do self.theme[k] = (opts.theme and opts.theme[k]) or v end
    local box = opts.box or {}
    self.boxX = box.x or DEF_BOX_X
    self.boxY = box.y or DEF_BOX_Y
    self.boxW = box.w or DEF_BOX_W
    self.boxH = box.h or DEF_BOX_H
    self.portraitSize = opts.portraitSize or DEF_PORTRAIT
    self.textX = self.boxX + (self.portraitSize > 0 and (self.portraitSize + 40) or 40)
    self.textY = self.boxY + 64
    self.textW = self.boxW - (self.portraitSize > 0 and (self.portraitSize + 80) or 80)
    self.mouse = opts.mouse == true
    self.drawBox = opts.drawBox ~= false  -- false → caller draws its own box art; text/choices/caret only
    self.advanceInput = opts.advanceInput -- optional extra advance predicate (e.g. the drum Decide pad)
    self.cpsDefault = opts.cps            -- typewriter speed a node starts at (default 42; {speed:} overrides)
    self.onSfx = opts.onSfx
    self.onVoice = opts.onVoice
    self.onExpr = opts.onExpr
    self.portraits = opts.portraits or {}
    self._atlas = setmetatable({}, { __mode = "k" })
    self._colors = {}
    self.activeFlag = false
    return self
end

function Dialogue:active() return self.activeFlag end

function Dialogue:color(r, g, b, a)
    local key = r .. "," .. g .. "," .. b .. "," .. (a or 255)
    local c = self._colors[key]
    if not c then c = COLOR:CreateColorFromRGBA(r, g, b, a or 255); self._colors[key] = c end
    return c
end

-- ── CLASSIC layout (LuaText path) — unchanged ─────────────────────────────────────
function Dialogue:layout(text)
    local a = atlasFor(self, self.fonts.text)
    local glyphs, cmds = {}, {}
    local x, y = 0, 0
    local lineH = a.h - 16 + LINE_GAP                   -- the font box has ~padding; trim a little
    local i, n = 1, #text
    local function emit(ch)
        local g = glyph(self, a, ch)
        glyphs[#glyphs + 1] = { tex = g.tex, x = x, y = y }
        x = x + g.adv
    end
    while i <= n do
        local c = text:sub(i, i)
        if c == "{" then
            local close = text:find("}", i, true)
            if close then
                local tag = text:sub(i + 1, close - 1)   -- e.g. "shake:8,0.3"
                local name, args = tag:match("^(%w+):?(.*)$")
                cmds[#glyphs] = cmds[#glyphs] or {}
                table.insert(cmds[#glyphs], { name = name, args = args })   -- fire when this many glyphs shown
                i = close + 1
            else emit(c); i = i + 1 end
        elseif c == "\n" then
            x = 0; y = y + lineH; i = i + 1
        elseif c == " " then
            local j = text:find("[%s{]", i + 1) or (n + 1)
            local word = text:sub(i + 1, j - 1)
            local ww = 0
            for k = 1, #word do ww = ww + glyph(self, a, word:sub(k, k)).adv end
            if x + glyph(self, a, " ").adv + ww > self.textW then x = 0; y = y + lineH
            else emit(" ") end
            i = i + 1
        else emit(c); i = i + 1 end
    end
    return glyphs, cmds
end

-- ── GLYPH layout (LuaGlyphText path): strip tags → plain text, wrap via the engine, count UTF-8 chars ──
function Dialogue:layoutGlyph(text)
    local plain, cmds = {}, {}
    local count = 0
    local i, n = 1, #text
    while i <= n do
        local c = text:sub(i, i)
        if c == "{" then
            local close = text:find("}", i, true)
            if close then
                local tag = text:sub(i + 1, close - 1)
                local name, args = tag:match("^(%w+):?(.*)$")
                cmds[count] = cmds[count] or {}
                table.insert(cmds[count], { name = name, args = args })
                i = close + 1
            else
                plain[#plain + 1] = c; count = count + 1; i = i + 1
            end
        else
            -- consume one full UTF-8 sequence
            local seq = text:match("^" .. UTF8_PAT, i) or c
            plain[#plain + 1] = seq
            if seq ~= "\n" then count = count + 1 end
            i = i + #seq
        end
    end
    local full = table.concat(plain)
    -- engine word wrap (honors \n); returns plain string lines
    local arr = self.gfont:WrapToLines(full, self.textW)
    local lines = {}
    for k = 0, arr.Length - 1 do
        local chars = {}
        for ch in tostring(arr[k]):gmatch(UTF8_PAT) do chars[#chars + 1] = ch end
        lines[#lines + 1] = { chars = chars, n = #chars }
    end
    local total = 0
    for _, l in ipairs(lines) do total = total + l.n end
    return lines, cmds, total
end

function Dialogue:start(script)
    self.script = script or {}
    self.nodeIdx = 0
    self.result = nil
    self.activeFlag = true
    self:nextNode()
end

function Dialogue:nextNode()
    self.nodeIdx = self.nodeIdx + 1
    local node = self.script[self.nodeIdx]
    if not node then self.activeFlag = false; return end
    self.node = node
    if self.gfont ~= nil then
        self.glines, self.cmds, self.total = self:layoutGlyph(node.text or "")
    else
        self.glyphs, self.cmds = self:layout(node.text or "")
        self.total = #self.glyphs
    end
    self.revealed = 0
    self.cps = self.cpsDefault or DEFAULT_CPS
    self.pause = 0
    self.fired = {}
    self.choosing = false
    self.choiceIdx = 1
end

local function kp(k) return INPUT:KeyboardPressed(k) end

function Dialogue:advancePressed()
    if kp("Return") or kp("Space") then return true end
    if self.mouse and INPUT.MousePressed and INPUT:MousePressed("Left") then return true end
    if self.advanceInput ~= nil and self.advanceInput() then return true end
    return false
end

function Dialogue:update(dt)
    if not self.activeFlag then return "done" end
    local node = self.node
    -- choices phase
    if self.choosing then
        local ch = node.choices
        if kp("UpArrow") or kp("W") then self.choiceIdx = (self.choiceIdx - 2) % #ch + 1 end
        if kp("DownArrow") or kp("S") then self.choiceIdx = self.choiceIdx % #ch + 1 end
        if self:advancePressed() then
            self.result = ch[self.choiceIdx].value
            self.activeFlag = false
            return "done"
        end
        return "running"
    end
    -- typewriter phase
    if self.pause > 0 then self.pause = self.pause - dt
    elseif self.revealed < self.total then
        if self:advancePressed() then
            -- skip to fully revealed: fire every remaining command first (expr swaps etc. must land)
            for idx = 0, self.total do
                if self.cmds[idx] and not self.fired[idx] then
                    self.fired[idx] = true
                    for _, cmd in ipairs(self.cmds[idx]) do self:runCmd(cmd) end
                end
            end
            self.revealed = self.total
            self.pause = 0
        else
            self.revealed = math.min(self.total, self.revealed + self.cps * dt)
        end
        -- fire commands for newly-revealed glyphs
        local upto = floor(self.revealed)
        for idx = 0, upto do
            if self.cmds[idx] and not self.fired[idx] then
                self.fired[idx] = true
                for _, cmd in ipairs(self.cmds[idx]) do self:runCmd(cmd) end
            end
        end
    else
        -- fully revealed: either offer choices, or wait for advance
        if node.choices and #node.choices > 0 then
            self.choosing = true
        elseif self:advancePressed() then
            self:nextNode()
            if not self.activeFlag then self.result = self.result or true; return "done" end
        end
    end
    return "running"
end

function Dialogue:runCmd(cmd)
    local name, args = cmd.name, cmd.args or ""
    if name == "shake" then
        local amp, dur = args:match("([%d%.]+),?([%d%.]*)")
        GLOBALCAMERA:Shake(tonumber(amp) or 12, tonumber(dur) or 0.3)
    elseif name == "pause" then
        self.pause = tonumber(args) or 0.4
    elseif name == "speed" then
        self.cps = tonumber(args) or DEFAULT_CPS
    elseif name == "sfx" then
        if self.onSfx then self.onSfx(args) end
    elseif name == "voice" then
        if self.onVoice then self.onVoice(args) end
    elseif name == "expr" then
        if self.onExpr then self.onExpr(args) end
    end
end

-- bake the PopUI-style box (rounded panel + drop shadow + name pill) once per geometry
function Dialogue:bakeFancyBox()
    if self._fbox then return self._fbox end
    local Shape = require("PopUI.shape")
    local m = 24                                             -- shadow margin
    local t = self.theme
    local cv = CANVAS:CreateCanvas(self.boxW + 2 * m, self.boxH + 2 * m)
    cv:ClearTransparent()
    Shape.dropShadow(cv, m, m, self.boxW, self.boxH, 26, { col = t.shadow })
    Shape.panel(cv, m, m, self.boxW, self.boxH, {
        radius = 26, outline = { col = t.outline, width = 3 },
        top = t.face, bottom = t.face2, gloss = { 255, 255, 255, 40 },
    })
    cv:Upload()
    -- name pill (drawn above the box's top-left)
    local pw, ph = 320, 54
    local pill = CANVAS:CreateCanvas(pw + 2 * m, ph + 2 * m)
    pill:ClearTransparent()
    Shape.dropShadow(pill, m, m, pw, ph, 22, { col = t.shadow, layers = 3 })
    Shape.panel(pill, m, m, pw, ph, {
        radius = 22, outline = { col = t.outline, width = 3 },
        top = t.namePill, bottom = t.namePill2, gloss = { 255, 255, 255, 60 },
    })
    pill:Upload()
    -- choice row (baked at full width, tinted per state at draw)
    local rw, rh = math.min(760, self.textW), 52
    local row = CANVAS:CreateCanvas(rw, rh)
    row:ClearTransparent()
    Shape.fillRound(row, 0, 0, rw, rh, 18, { 255, 255, 255, 255 })
    row:Upload()
    -- portrait frame (a rounded inset panel the portrait texture sits inside)
    local pfr = nil
    if self.portraitSize > 0 then
        local ps = self.portraitSize + 16
        pfr = CANVAS:CreateCanvas(ps, ps)
        pfr:ClearTransparent()
        Shape.panel(pfr, 0, 0, ps, ps, {
            radius = 20, outline = { col = t.outline, width = 3 },
            top = { 52, 58, 76, 255 }, bottom = { 34, 38, 52, 255 }, gloss = { 255, 255, 255, 26 },
        })
        pfr:Upload()
    end
    self._fbox = { cv = cv, m = m, pill = pill, pw = pw, ph = ph, row = row, rw = rw, rh = rh, pfr = pfr }
    return self._fbox
end

function Dialogue:dispose()
    if self._fbox then
        self._fbox.cv:Dispose(); self._fbox.pill:Dispose(); self._fbox.row:Dispose()
        if self._fbox.pfr then self._fbox.pfr:Dispose() end
        self._fbox = nil
    end
    if self._dim then self._dim:Dispose(); self._dim = nil end
end

function Dialogue:draw()
    if not self.activeFlag then return end
    local node = self.node
    local t = self.theme

    if not self.drawBox then
        -- caller renders its own box art (e.g. intro_nokon's Dialogue.png); skip the chrome
    elseif self.ui == "popui" then
        local fb = self:bakeFancyBox()
        fb.cv:SetColor(1, 1, 1); fb.cv:SetOpacity(1); fb.cv:SetScale(1, 1)
        fb.cv:Draw(self.boxX - fb.m, self.boxY - fb.m)
        if node.name and node.name ~= "" then
            -- the pill sits above the box, aligned with the TEXT column (right of the portrait
            -- frame). At the old boxX+30 it overlapped the portrait frame's top band (the pill
            -- spans down to boxY+24, the frame starts at boxY+14) and the frame — drawn later —
            -- painted over the name.
            local pillX = self.boxX + (self.portraitSize > 0 and (self.portraitSize + 34) or 30)
            fb.pill:SetColor(1, 1, 1); fb.pill:SetOpacity(1)
            fb.pill:Draw(pillX - fb.m, self.boxY - 30 - fb.m)
            local nf = self.gfontName or self.fonts.name
            if self.gfontName then
                nf:Draw(node.name, pillX + fb.pw / 2, self.boxY - 30 + fb.ph / 2 + 2,
                    self:color(t.nameText[1], t.nameText[2], t.nameText[3]), nil, 1, 1, fb.pw - 24, "center")
            elseif nf then
                nf:GetText(node.name, false, 600, self:color(t.nameText[1], t.nameText[2], t.nameText[3]), self:color(0, 0, 0)):Draw(pillX + 14, self.boxY - 24)
            end
        end
    else
        -- classic flat panel (original look, unchanged)
        local dim = self._dim
        if not dim then dim = CANVAS:CreateCanvas(2, 2); dim:Clear(255, 255, 255, 255); dim:Upload(); self._dim = dim end
        dim:SetColor(0.04, 0.05, 0.08); dim:SetOpacity(0.82)
        dim:SetScale(self.boxW / 2, self.boxH / 2); dim:Draw(self.boxX, self.boxY)
        dim:SetColor(0.20, 0.45, 0.70); dim:SetOpacity(1.0)
        dim:SetScale(self.boxW / 2, 3); dim:Draw(self.boxX, self.boxY)                 -- top accent line
        if node.name and self.fonts.name then
            self.fonts.name:GetText(node.name, false, 600, COLOR:CreateColorFromRGBA(255, 230, 150, 255), COLOR:CreateColorFromRGBA(0, 0, 0, 255)):Draw(self.textX, self.boxY + 14)
        end
    end

    -- portrait area — a rounded frame in popui mode, the old flat quad otherwise
    if self.portraitSize > 0 then
        if self.ui == "popui" and self._fbox and self._fbox.pfr then
            self._fbox.pfr:SetColor(1, 1, 1); self._fbox.pfr:SetOpacity(1)
            self._fbox.pfr:Draw(self.boxX + 10, self.boxY + 14)
        else
            local dim = self._dim
            if not dim then dim = CANVAS:CreateCanvas(2, 2); dim:Clear(255, 255, 255, 255); dim:Upload(); self._dim = dim end
            dim:SetColor(0.10, 0.12, 0.18); dim:SetOpacity(1.0)
            dim:SetScale(self.portraitSize / 2, self.portraitSize / 2); dim:Draw(self.boxX + 18, self.boxY + 22)
        end
        local por = node.portrait and self.portraits[node.portrait]
        if por then
            por:SetScale(self.portraitSize / (por.Width > 0 and por.Width or self.portraitSize), self.portraitSize / (por.Height > 0 and por.Height or self.portraitSize))
            por:Draw(self.boxX + 18, self.boxY + 22)
            por:SetScale(1, 1)
        end
    end

    -- text
    local upto = floor(self.revealed)
    if self.gfont ~= nil then
        local tc = (self.ui == "popui") and self:color(t.text[1], t.text[2], t.text[3]) or self:color(255, 255, 255)
        -- a BLACK outline keeps the body readable over any panel/background (the transparent outline
        -- left light theme text as unreadable grey); pair with a light theme.text for white+border
        local oc = self:color(0, 0, 0, 255)
        local lineH = self.gfont.LineHeight + LINE_GAP
        local shown = 0
        for li, line in ipairs(self.glines) do
            if shown >= upto then break end
            local take = math.min(line.n, upto - shown)
            local s = (take == line.n) and table.concat(line.chars) or table.concat(line.chars, "", 1, take)
            if #s > 0 then
                self.gfont:Draw(s, self.textX, self.textY + (li - 1) * lineH, tc, oc, 1, 1, 0, "topleft")
            end
            shown = shown + line.n
        end
    else
        -- classic atlas glyphs bake white; tint them dark on the light popui panel
        local tr, tg, tb = 1, 1, 1
        if self.ui == "popui" then tr, tg, tb = t.text[1] / 255, t.text[2] / 255, t.text[3] / 255 end
        for i = 1, math.min(upto, self.total) do
            local gl = self.glyphs[i]
            gl.tex:SetColor(tr, tg, tb); gl.tex:Draw(self.textX + gl.x, self.textY + gl.y)
            gl.tex:SetColor(1, 1, 1)
        end
    end

    -- choices (after full reveal) or the advance caret
    if self.choosing then
        local ch = node.choices
        if self.ui == "popui" and self._fbox then
            local fb = self._fbox
            local totalH = #ch * (fb.rh + 10) - 10
            local cy0 = self.boxY - totalH - 26
            for i = 1, #ch do
                local sel = (i == self.choiceIdx)
                local ry = cy0 + (i - 1) * (fb.rh + 10)
                local rc = sel and t.choiceSel or t.choiceRow
                fb.row:SetColor(rc[1] / 255, rc[2] / 255, rc[3] / 255)
                fb.row:SetOpacity((rc[4] or 255) / 255)
                fb.row:Draw(self.textX, ry)
                local label = (sel and "\u{25B8} " or "   ") .. ch[i].label
                if self.gfont then
                    self.gfont:Draw(label, self.textX + 22, ry + fb.rh / 2,
                        self:color(t.choiceText[1], t.choiceText[2], t.choiceText[3]), self:color(0, 0, 0, 0), 1, 1, fb.rw - 44, "left")
                elseif self.fonts.text then
                    self.fonts.text:GetText(label, false, fb.rw - 44,
                        self:color(t.choiceText[1], t.choiceText[2], t.choiceText[3]), self:color(0, 0, 0, 0))
                        :Draw(self.textX + 22, ry + 6)
                end
            end
        else
            for i = 1, #ch do
                local sel = (i == self.choiceIdx)
                local r, g, b = sel and 255 or 200, sel and 235 or 200, sel and 140 or 205
                local pre = sel and "> " or "  "
                if self.gfont then
                    self.gfont:Draw(pre .. ch[i].label, self.textX + 30, self.textY + 70 + (i - 1) * 42, self:color(r, g, b), nil, 1, 1, 0, "topleft")
                else
                    self.fonts.text:GetText(pre .. ch[i].label, false, 800, COLOR:CreateColorFromRGBA(r, g, b, 255), COLOR:CreateColorFromRGBA(0, 0, 0, 255))
                        :Draw(self.textX + 30, self.textY + 70 + (i - 1) * 42)
                end
            end
        end
    elseif self.revealed >= self.total then
        if self.gfont then
            local blink = 0.55 + 0.45 * math.abs(math.sin((self._caretT or 0)))
            self._caretT = (self._caretT or 0) + 0.09
            local cc = (self.ui == "popui") and t.caret or { 255, 235, 160 }
            self.gfont:Draw("\u{25BC}", self.boxX + self.boxW - 60, self.boxY + self.boxH - 44,
                self:color(cc[1], cc[2], cc[3]), nil, blink, 1, 0, "center")
        elseif self.fonts.text then
            self.fonts.text:GetText("\u{25BC}", false, 100, COLOR:CreateColorFromRGBA(255, 235, 160, 255), COLOR:CreateColorFromRGBA(0, 0, 0, 255))
                :Draw(self.boxX + self.boxW - 60, self.boxY + self.boxH - 50)
        end
    end
end

return Dialogue
