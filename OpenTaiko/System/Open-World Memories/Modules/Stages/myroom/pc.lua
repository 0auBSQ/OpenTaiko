---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- pc.lua — the computer screen: a PopUI two-pane window over the save file (replaces nameplates.lua).
--
-- Tabs (top row = 1st focus layer, list = 2nd layer, config_ui-style handoff):
--   Characters — browse CHARACTERLIST (rarity + unlock state); select → LuaSaveFile:ChangeCharacter.
--   Puchichara — same via PUCHICHARALIST / ChangePuchichara.
--   Dan Titles — the earned dan ranks (ChangeDan), previewed with the REAL dan plate renderer
--                (NAMEPLATE:DrawDanPlate with the clear-status grade + the gold gradient text markup,
--                exactly how C# CStageHeya bakes its gallery).
--   Nameplates — NAMEPLATESLIST / ChangeNameplate with a DrawTitlePlate preview. Unowned MYTHICAL
--                plates stay hidden and opaque conditions (ig/gt/gc) read "???".
--   Rename     — a PopUI textbox (engine text input) → ChangeName.
--
-- Interaction pattern copied from ROActivities/config_ui: Left/Right move between tabs while a tab
-- is focused; Down from a tab enters the list; Up past the first row (or Down past the last) hands
-- focus back to the active tab; the wheel scrolls the hovered list. The list is a ClippedMenu (a
-- stage-local PopUI.Menu subclass) that culls PARTIAL rows entirely — rows can never poke out of
-- the panel — and paints per-row states: locked = desaturated + dimmed, equipped = yellow face.
--
-- Locked entries always show a real unlock line (message → blocked reason → coin price; "???" only
-- for the genuinely hidden condition types). Coin unlocks spend via Coins/SpendCoins like the shop.
-- While the screen is open the C# MyRoom BGM loops (lazy, pcall-guarded).
--
-- NOTE for the planned furniture SHOP on this screen: use OWM3d ModelIcon previews at res = 512,
-- created when the screen opens and dispose()d when it closes (see editmode.lua for the pattern).

local PopUI = require("PopUI")
local I18N = require("i18n")

local PC = {}
PC.__index = PC

local SW, SH = 1920, 1080
local PANEL = { x = 200, y = 56, w = 1520, h = 952 }
local LIST = { x = PANEL.x + 44, y = PANEL.y + 196, w = 580, h = 660 }   -- exactly 10 rows of 66
local DETAIL_X = LIST.x + LIST.w + 50                -- right pane left edge
local DETAIL_W = PANEL.x + PANEL.w - 44 - DETAIL_X
local DETAIL_CX = math.floor(DETAIL_X + DETAIL_W / 2)
local ROW_H = 66

local TABS = { "Characters", "Puchichara", "Dan Titles", "Nameplates", "Rename" }
local TAB_W, TAB_STEP, TAB_X0 = 278, 290, PANEL.x + 36
-- conditions that read exactly "???" — re-checked against the FULL CUnlockConditionFactory list
-- (ch/cs/cm/ce coin paths, sd/dp/lp/sp/sg/sc performance, tp/ap/aw play counts are all earnable
-- and self-describing): "ig" is literally "Impossible to Get"; "gt"/"gc" are story-driven global
-- trigger/counter flags with no player-readable condition
local OPAQUE_COND = { ig = true, gt = true, gc = true }

local RARITY_COL = {
    Poor = { 150, 150, 150 }, Common = { 200, 205, 210 }, Uncommon = { 120, 220, 130 }, Rare = { 110, 170, 255 },
    Epic = { 200, 130, 255 }, Legendary = { 255, 200, 90 }, Mythical = { 255, 120, 120 },
}
-- rarity → the lang-int DrawTitlePlate expects (matches HRarity.RarityToLangInt)
local RARITY_LANG = { Poor = 0, Common = 1, Uncommon = 2, Rare = 3, Epic = 4, Legendary = 5, Mythical = 6 }
-- rarity sort order inside a nameplate type group (Poor → Mythical; DB order is the tiebreak)
local RARITY_ORDER = { Poor = 1, Common = 2, Uncommon = 3, Rare = 4, Epic = 5, Legendary = 6, Mythical = 7 }
local GOLD_MARKUP = "<g.#FFE34A.#EA9622>%s</g>"      -- the gold dan-title gradient (as C# Heya bakes it)
-- gallery text bakes 1:1 with C# CStageHeya (pfHeyaFont at Heya_Font_Scale default = 14):
--   title plates: Color.Black on Color.Transparent    dan rows: White (gold rows: Color.Gold) on Black
local HEYA_FONT = 14
local plateFg, plateBg, danFg, danGoldFg, danBg = nil, nil, nil, nil, nil
local function bakeColors()
    plateFg   = plateFg or COLOR:CreateColorFromRGBA(0, 0, 0, 255)
    plateBg   = plateBg or COLOR:CreateColorFromRGBA(0, 0, 0, 0)
    danFg     = danFg or COLOR:CreateColorFromRGBA(255, 255, 255, 255)
    danGoldFg = danGoldFg or COLOR:CreateColorFromRGBA(255, 215, 0, 255)   -- Color.Gold
    danBg     = danBg or COLOR:CreateColorFromRGBA(0, 0, 0, 255)
end

-- nameplate type names (data/nameplate_types.json: per-language names + "special" hidden types)
local typeCfg, typeCfgTried = nil, false
local function loadTypeCfg()
    if typeCfg ~= nil or typeCfgTried then return end
    typeCfgTried = true
    pcall(function() typeCfg = JSONLOADER:LoadJson("data/nameplate_types.json") end)
end

local THEME = {
    colors = {
        surface  = { 246, 244, 250, 255 }, surface2 = { 224, 226, 240, 255 },
        primary  = { 110, 140, 210, 255 }, primary2 = { 78, 104, 176, 255 },
        outline  = { 52, 58, 92, 255 },    text = { 52, 58, 92, 255 },
    },
    font = { small = 17, label = 21, button = 22, title = 30 },
}
-- the round red close button (w == h and radius ≥ half the size bakes a circle)
local CLOSE_STYLE = {
    radius = 40,
    colors = { primary = { 208, 62, 56, 255 }, primary2 = { 158, 34, 30, 255 },
               outline = { 96, 24, 20, 255 }, textOnAccent = { 255, 255, 255, 255 } },
}

-- ── ClippedMenu: PopUI.Menu subclass — edge-faded rows + per-row state faces + scrollbar ──────────
-- rowStyle(i) → "normal" | "locked" | "equipped" | "header". Neither LuaCanvas (baked row faces)
-- nor the glyph fonts expose a source-rectangle draw, so a hard mask is impossible — edge rows use
-- the graceful fallback instead: a row that would overflow the panel SLIDES against the edge and
-- alpha-fades out proportionally to its overflow (no pop-in). visibleRowRects() keeps the strict
-- fully-inside maths for the harness clipping check.
local ClippedMenu = setmetatable({}, { __index = PopUI.Menu })
ClippedMenu.__index = ClippedMenu

function ClippedMenu.new(o)
    local m = PopUI.Menu.new(o)
    return setmetatable(m, ClippedMenu)
end

-- a row's ink rect at the current scroll, or nil when it isn't FULLY inside (harness contract)
function ClippedMenu:rowRect(i)
    local rh = self.rowHeight
    local ry = self.y + (i - 1) * rh - self._scrollCur
    local ink = rh - 10
    if ry < self.y - 0.5 or ry + ink > self.y + self.h + 0.5 then return nil end
    return ry, ink
end

function ClippedMenu:visibleRowRects()
    local out = {}
    for i = 1, self:_count() do
        local ry, ink = self:rowRect(i)
        if ry then out[#out + 1] = { i = i, y = ry, h = ink } end
    end
    return out
end

-- the draw-time variant: TRUE positions, coarse cull only — rows that overlap the viewport at
-- all are drawn and pixel-cut by the GRAPHICS scissor clip (no fades, no clamping)
function ClippedMenu:rowDraw(i)
    local rh = self.rowHeight
    local ry = self.y + (i - 1) * rh - self._scrollCur
    local ink = rh - 10
    if ry + ink < self.y - 2 or ry > self.y + self.h + 2 then return nil end
    return ry, ink
end

-- side scrollbar pieces, baked ONCE per menu build (content size is fixed until a tab switch)
local function bakePill(w, h, col)
    local cv = CANVAS:CreateCanvas(w, h)
    cv:ClearTransparent()
    local rad = math.max(1, math.floor(w / 2))
    if h > 2 * rad then cv:FillRect(0, rad, w, h - 2 * rad, col[1], col[2], col[3], col[4]) end
    cv:FillCircle(rad, rad, rad, col[1], col[2], col[3], col[4])
    cv:FillCircle(rad, h - rad - 1, rad, col[1], col[2], col[3], col[4])
    cv:Upload()
    return cv
end

function ClippedMenu:ensureScrollbar()
    if self._sbDone then return end
    self._sbDone = true
    local contentH = self:_count() * self.rowHeight
    if contentH <= self.h then return end                      -- everything fits: no bar
    self._sbTrack = bakePill(6, self.h, { 52, 58, 92, 60 })
    self._sbThumbH = math.max(36, math.floor(self.h * self.h / contentH))
    self._sbThumb = bakePill(12, self._sbThumbH, { 110, 140, 210, 235 })
end

function ClippedMenu:drawScrollbar()
    self:ensureScrollbar()
    if not self._sbThumb then return end
    local contentH = self:_count() * self.rowHeight
    local tx = math.floor(self.x + self.w + 12)
    self._sbTrack:Draw(tx + 3, math.floor(self.y))
    local range = contentH - self.h
    local k = range > 0 and math.max(0, math.min(1, (self._scrollCur or 0) / range)) or 0
    self._sbThumb:Draw(tx, math.floor(self.y + (self.h - self._sbThumbH) * k))
end

function ClippedMenu:dispose()
    if PopUI.Menu.dispose then PopUI.Menu.dispose(self) end
    if self._sbTrack then self._sbTrack:Dispose(); self._sbTrack = nil end
    if self._sbThumb then self._sbThumb:Dispose(); self._sbThumb = nil end
end

local STYLE_TINT = {
    normal   = { { 1, 1, 1 },          { 1, 1, 1 } },          -- { unselected, selected } face tints
    locked   = { { 0.74, 0.74, 0.78 }, { 0.56, 0.56, 0.62 } },
    equipped = { { 1.0, 0.85, 0.42 },  { 1.0, 0.74, 0.2 } },
}
local STYLE_FG = {
    locked   = { 122, 126, 136 },
    equipped = { 96, 64, 8 },
}

function ClippedMenu:draw()
    if not self.visible then return end
    local c = self.eff.colors
    local U = PopUI.Util
    -- TRUE scissor clipping: everything between SetClip/ClearClip (faces, glyph text) is
    -- pixel-cut at the list rect — partial edge rows render at their real positions and are
    -- simply cut, exactly like a native scrolling list. ALWAYS paired with ClearClip below.
    local clip = GRAPHICS ~= nil and GRAPHICS.SetClip ~= nil
    if clip then GRAPHICS:SetClip(self.x - 8, self.y - 1, self.w + 16, self.h + 2) end
    for i = 1, self:_count() do
        local ry, ink = self:rowDraw(i)
        if ry then
            local rcy = math.floor(ry + ink * 0.5 + self.mgr:textNudge(self.eff.font.label))
            local rcx = math.floor(self.x + self.w * 0.5)
            local isSel = (i == self.selected)
            local st = self.rowStyle and self.rowStyle(i) or "normal"
            if st == "header" then
                -- type-section header: no row face / focus ring, just a centred divider label
                local it = self.items[i]
                local label = it and (it.text or tostring(it)) or ""
                self.mgr:drawTextEx(self.eff.font.small, label, rcx, rcy,
                    { 108, 114, 146 }, { 255, 255, 255, 140 }, 1, 1, self.w - 40, "center")
            else
                local tint = (STYLE_TINT[st] or STYLE_TINT.normal)[isSel and 2 or 1]
                -- failed-unlock feedback: the baked face flashes red for ~0.4 s (tint only,
                -- no new textures); _flashI/_flashK are driven by PC:update
                local fk = (self._flashI == i and self._flashK) and self._flashK or 0
                local tr = math.min(1, tint[1] + fk * 0.9)
                local tg = tint[2] * (1 - 0.8 * fk)
                local tb = tint[3] * (1 - 0.8 * fk)
                -- equipped/locked keep their own face even when selected (state beats selection
                -- colour); selection still reads via the deeper tint + the focus ring
                local piece = (isSel and st == "normal") and self._rowSel or self._row
                piece.canvas:SetColor(tr, tg, tb)
                piece.canvas:SetOpacity(1)
                piece.canvas:SetScale(1, 1)
                piece.canvas:DrawAtAnchor(rcx, rcy, "center")
                piece.canvas:SetColor(1, 1, 1)
                if isSel and self.focused and self._hiCur > 0.01 then
                    self._ring.canvas:SetOpacity(self._hiCur)
                    self._ring.canvas:DrawAtAnchor(rcx, rcy, "center")
                end
                local it = self.items[i]
                if it then
                    local fg = STYLE_FG[st] or ((isSel and st == "normal") and c.textOnAccent or c.text)
                    if fk > 0.01 then fg = { 200, 40, 40 } end
                    local bg
                    if st == "locked" then bg = { 235, 235, 240, 110 }
                    elseif st == "equipped" then bg = { 255, 250, 225, 160 }
                    elseif isSel then bg = U.shade(c.primary2, 0.5)
                    else bg = { 255, 255, 255, 150 } end
                    self.mgr:drawTextEx(self.eff.font.label, it.text, math.floor(self.x + 28), rcy,
                        fg, bg, 1, 1, self.w - 48, "left")
                end
            end
        end
    end
    if clip then GRAPHICS:ClearClip() end
    self:drawScrollbar()                 -- the bar lives beside the panel, OUTSIDE the clip
end

-- clickable scrollbar: thumb dragging scrolls proportionally, track clicks jump a page.
-- Runs inside the widget update (PopUI cursor ctx: mx/my/mPressed/mPressing).
function ClippedMenu:update(ctx)
    PopUI.Menu.update(self, ctx)
    if not self._sbThumb then return end
    local contentH = self:_count() * self.rowHeight
    local range = contentH - self.h
    if range <= 0 then return end
    local tx = math.floor(self.x + self.w + 12)
    local thumbY = self.y + (self.h - self._sbThumbH) * math.max(0, math.min(1, (self._scrollTarget or 0) / range))
    if self._sbDrag then
        if not ctx.mPressing then
            self._sbDrag = nil
        else
            local k = (ctx.my - self._sbDrag - self.y) / math.max(1, self.h - self._sbThumbH)
            self._scrollTarget = range * math.max(0, math.min(1, k))
            self:_clampScroll()
        end
        return
    end
    if ctx.mPressed and ctx.mx >= tx - 8 and ctx.mx <= tx + 20
        and ctx.my >= self.y - 4 and ctx.my <= self.y + self.h + 4 then
        if ctx.my >= thumbY and ctx.my <= thumbY + self._sbThumbH then
            self._sbDrag = ctx.my - thumbY               -- grab offset inside the thumb
        else
            local dir = (ctx.my < thumbY) and -1 or 1    -- track click: one page toward the click
            self._scrollTarget = (self._scrollTarget or 0) + dir * self.h
            self:_clampScroll()
        end
    end
end

-- ── the MyRoom BGM (created lazily ONCE for the stage lifetime; disposed via PC.disposeBgm) ───────
local bgm, bgmFailed = nil, false
local function bgmPlay()
    if bgm == nil and not bgmFailed then
        local ok = pcall(function()
            bgm = SOUND:CreateBGM("../../../Sounds/Heya/BGM.ogg")
            bgm:SetLoop(true)
        end)
        if not ok or bgm == nil then bgmFailed = true; bgm = nil end
    end
    if bgm then pcall(function() bgm:Play() end) end
end
local function bgmStop()
    if bgm then pcall(function() bgm:Stop() end) end
end
-- the failed-unlock error blip: the skin's own error sound (same file the C# UI uses;
-- character_shop's sounds.Error is the same idea). Cached once, disposed with the BGM.
local errSfx, errSfxFailed = nil, false
local function errPlay()
    if errSfx == nil and not errSfxFailed then
        local ok = pcall(function() errSfx = SOUND:CreateSFX("../../../Sounds/Error.ogg") end)
        if not ok or errSfx == nil then errSfxFailed = true; errSfx = nil end
    end
    if errSfx then pcall(function() errSfx:Play() end) end
end

function PC.disposeBgm()
    if bgm then pcall(function() bgm:Dispose() end) end
    bgm, bgmFailed = nil, false
    if errSfx then pcall(function() errSfx:Dispose() end) end
    errSfx, errSfxFailed = nil, false
end

function PC.new()
    return setmetatable({ isOpen = false }, PC)
end
function PC:active() return self.isOpen end

-- ── list building ─────────────────────────────────────────────────────────────────────────────────
function PC:buildLists()
    local save = self.save
    self.chars, self.puchis, self.dans, self.nps = {}, {}, {}, {}
    pcall(function()
        for i = 0, CHARACTERLIST.Count - 1 do
            local e = CHARACTERLIST:GetByIndex(i)
            if e ~= nil then self.chars[#self.chars + 1] = e end
        end
    end)
    pcall(function()
        for i = 0, PUCHICHARALIST.Count - 1 do
            local e = PUCHICHARALIST:GetByIndex(i)
            if e ~= nil then self.puchis[#self.puchis + 1] = e end
        end
    end)
    pcall(function()
        for i = 0, save.DanTitleCount - 1 do
            local e = save:GetDanTitleByIndex(i)
            if e ~= nil then self.dans[#self.dans + 1] = e end
        end
    end)
    pcall(function()
        for i = 0, NAMEPLATESLIST.Count - 1 do
            local np = NAMEPLATESLIST:GetByIndex(i)
            if np ~= nil then
                local owned = save:IsNameplateUnlocked(np.Id)
                -- unowned MYTHICAL nameplates stay hidden (kept secret until earned)
                if owned or np.Rarity ~= "Mythical" then
                    self.nps[#self.nps + 1] = np
                end
            end
        end
    end)
    -- Nameplates grouped by their TYPE (nameplateInfo.iType — the plate art family the customize
    -- screen keys on): a named header row opens each type section; entries sort by rarity
    -- ascending (Poor → Mythical) with DB order as the tiebreak.
    self.npRows = {}
    local byType = {}
    for idx, np in ipairs(self.nps) do
        local t = 0
        pcall(function() t = np.Type or 0 end)
        byType[t] = byType[t] or {}
        byType[t][#byType[t] + 1] = { np = np, idx = idx }
    end
    local types = {}
    for t in pairs(byType) do types[#types + 1] = t end
    table.sort(types)
    for _, t in ipairs(types) do
        table.sort(byType[t], function(a, b)
            local ra = RARITY_ORDER[a.np.Rarity] or 99
            local rb = RARITY_ORDER[b.np.Rarity] or 99
            if ra ~= rb then return ra < rb end
            return a.idx < b.idx
        end)
        self.npRows[#self.npRows + 1] = { header = "◆  " .. self:typeName(t) .. "  ◆" }
        for _, rec in ipairs(byType[t]) do
            self.npRows[#self.npRows + 1] = { e = rec.np }
        end
    end
end

-- display name of a nameplate type: data/nameplate_types.json per-language entry (the current
-- i18n language key, "default" fallback — no fixed language set). "special" types stay "???"
-- until the player owns at least one UNLOCKED nameplate of that type. Unknown types fall back
-- to a generic numbered label.
function PC:typeName(t)
    loadTypeCfg()
    local node = nil
    pcall(function() node = typeCfg and typeCfg[tostring(t)] or nil end)
    if node == nil then return I18N.trf("Plate Type %d", t) end
    local special = 0
    pcall(function() special = JSONLOADER:ExtractNumber(node["special"]) or 0 end)
    if special == 1 then
        local revealed = false
        for _, np in ipairs(self.nps or {}) do
            local ty = nil
            pcall(function() ty = np.Type end)
            if ty == t and self.save and self.save:IsNameplateUnlocked(np.Id) then revealed = true; break end
        end
        if not revealed then return I18N.tr("???") end
    end
    local name = nil
    pcall(function() name = JSONLOADER:ExtractText(node[I18N.lang]) end)
    if name == nil or name == "" then
        pcall(function() name = JSONLOADER:ExtractText(node["default"]) end)
    end
    if name == nil or name == "" then return I18N.trf("Plate Type %d", t) end
    return name
end

function PC:curList()
    if self.tab == 1 then return self.chars
    elseif self.tab == 2 then return self.puchis
    elseif self.tab == 3 then return self.dans
    elseif self.tab == 4 then return self.nps end
    return {}
end

-- ── owned / equipped state helpers ────────────────────────────────────────────────────────────────
local function isOwned(save, tab, e)
    if tab == 1 then return save:IsCharacterUnlocked(e.FolderName) or not e.UnlockCondition.HasCondition end
    if tab == 2 then return save:IsPuchicharaUnlocked(e.FolderName) or not e.UnlockCondition.HasCondition end
    if tab == 3 then return true end                                     -- listed dans are earned
    return save:IsNameplateUnlocked(e.Id) or not e.UnlockCondition.HasCondition
end
local function isEquipped(save, tab, e)
    if tab == 1 then return save.CharacterName == e.FolderName end
    if tab == 2 then
        local p = nil
        pcall(function() p = save:GetPuchichara() end)
        return p ~= nil and p.FolderName == e.FolderName
    end
    if tab == 3 then return save.SelectedDan == e.Title end
    local npi = nil
    pcall(function() npi = save.NameplateInfo end)
    return npi ~= nil and npi.Id == e.Id
end
local function entryName(save, tab, e)
    if tab == 1 then return e.DisplayName or e.FolderName end
    if tab == 2 then return e.Name or e.FolderName end
    if tab == 3 then return e.Title or "?" end
    local owned = save:IsNameplateUnlocked(e.Id) or not e.UnlockCondition.HasCondition
    return owned and (e.Title or "") or I18N.tr("???")
end

-- the ENTRY behind row i (nil for type-section headers on the nameplates tab)
function PC:entryAt(i)
    if self.tab == 4 then
        local row = self.npRows and self.npRows[i]
        return row and row.e or nil
    end
    return self:curList()[i]
end

-- header rows are display-only: navigation and Equip skip them
function PC:rowSelectable(i)
    if self.tab ~= 4 then return true end
    local row = self.npRows and self.npRows[i]
    return row ~= nil and row.e ~= nil
end

-- the state of row i in the current list (drives the ClippedMenu row faces)
function PC:rowStyleFor(i)
    if self.tab == 4 and not self:rowSelectable(i) then return "header" end
    local e = self:entryAt(i)
    if e == nil then return "normal" end
    if isEquipped(self.save, self.tab, e) then return "equipped" end
    if not isOwned(self.save, self.tab, e) then return "locked" end
    return "normal"
end

-- ── UI build ──────────────────────────────────────────────────────────────────────────────────────
local function setFocusTo(ui, w)
    if not w then return end
    for i, fw in ipairs(ui.focusables) do if fw == w then ui.focusIdx = i; return end end
end

function PC:buildUI()
    if self.ui then self.ui:disposeWidgets() end
    local ui = PopUI.new{ theme = THEME }
    self.ui = ui
    local pc = self
    ui:panel{ x = PANEL.x, y = PANEL.y, w = PANEL.w, h = PANEL.h, title = I18N.tr("My Computer") }
    self.tabBtns = {}
    for ti, name in ipairs(TABS) do
        local i = ti
        local t = ui:button{
            text = I18N.tr(name), x = TAB_X0 + (ti - 1) * TAB_STEP, y = PANEL.y + 74, w = TAB_W, h = 62,
            accent = (self.tab == i),
            style = { font = { button = 19 } },
            onClick = function() pc:setTab(i) end,
        }
        t._tab = i
        -- config_ui handoff: Down from a tab enters the 2nd layer (list / rename box); Up wraps too
        t.onNavDown = function() return pc:focusList() end
        t.onNavUp = function() return pc:focusList() end
        self.tabBtns[i] = t
    end
    -- the round RED close button
    ui:button{ text = "×", x = PANEL.x + PANEL.w - 74, y = PANEL.y - 14, w = 64, h = 64,
               accent = true, style = CLOSE_STYLE,
               onClick = function() pc._wantClose = true end }

    if self.tab == 5 then
        -- Rename pane
        self.menu, self.actionBtn = nil, nil
        self.renameVal = self.save and self.save.Name or ""
        self.tb = ui:textbox{
            x = LIST.x, y = LIST.y + 90, w = 640, h = 76, value = self.renameVal, maxLen = 24,
            placeholder = I18N.tr("new name..."),
            onChange = function(t) pc.renameVal = t end,
            onSubmit = function(t) pc.renameVal = t; pc:applyRename() end,
        }
        self.tb.onNavUp = function() setFocusTo(ui, pc.tabBtns[pc.tab]); return true end
        ui:button{ text = I18N.tr("Apply"), x = LIST.x + 670, y = LIST.y + 86, w = 180, h = 82, accent = true,
                   onClick = function() pc:applyRename() end }
    else
        self.tb = nil
        local items = {}
        if self.tab == 4 then
            -- nameplates: type-section headers interleaved with the entries (rows = npRows)
            for _, row in ipairs(self.npRows or {}) do
                items[#items + 1] = row.e and entryName(self.save, self.tab, row.e) or row.header
            end
        else
            for _, e in ipairs(self:curList()) do
                items[#items + 1] = entryName(self.save, self.tab, e)
            end
        end
        -- the initial cursor may never rest on a header row
        local sel = math.max(1, math.min(self.sel or 1, math.max(1, #items)))
        if not self:rowSelectable(sel) then
            local found = nil
            for i = sel + 1, #items do if self:rowSelectable(i) then found = i; break end end
            if not found then
                for i = sel - 1, 1, -1 do if self:rowSelectable(i) then found = i; break end end
            end
            sel = found or sel
        end
        local menu = ClippedMenu.new{
            x = LIST.x, y = LIST.y, w = LIST.w, h = LIST.h, items = items, rowHeight = ROW_H,
            selected = sel,
            rowStyle = function(i) return pc:rowStyleFor(i) end,
            onChange = function(i) pc.sel = i; pc:refreshDetail() end,
            onSelect = function(i) pc.sel = i; pc:activateSelected() end,
        }
        ui:add(menu)
        self.menu = menu
        -- 2nd-layer ↔ 1st-layer handoff at the list edges (config_ui pattern); header rows are
        -- skipped in BOTH directions — the cursor hops straight between the entries around them
        menu.onNavUp = function(m)
            local i = m.selected - 1
            while i >= 1 and not pc:rowSelectable(i) do i = i - 1 end
            if i >= 1 then m:setSelected(i); return true end
            setFocusTo(ui, pc.tabBtns[pc.tab]); return true
        end
        menu.onNavDown = function(m)
            local i = m.selected + 1
            while i <= #m.items and not pc:rowSelectable(i) do i = i + 1 end
            if i <= #m.items then m:setSelected(i); return true end
            setFocusTo(ui, pc.tabBtns[pc.tab]); return true
        end
        self.actionBtn = ui:button{
            text = I18N.tr("Equip"), x = DETAIL_X + 40, y = PANEL.y + PANEL.h - 130, w = DETAIL_W - 80, h = 76, accent = true,
            onClick = function() pc:activateSelected() end,
        }
        self.sel = sel
        self:refreshDetail()
    end
    setFocusTo(ui, self.tabBtns[self.tab])
end

function PC:focusList()
    if self.menu then setFocusTo(self.ui, self.menu); return true end
    if self.tb then setFocusTo(self.ui, self.tb); return true end
    return false
end

function PC:setTab(i)
    if self.tab ~= i then
        self.tab = i
        self.sel = 1
        self.msg = nil
        self:buildUI()
    end
end

-- Left/Right switch tabs while a tab button is focused (config_ui's handleTabKeys)
function PC:handleTabKeys()
    local ui = self.ui
    if ui == nil then return end
    local fw = ui.focusables[ui.focusIdx]
    if not (fw and fw._tab) then return end
    local right = INPUT:KeyboardPressed("RightArrow") or INPUT:Pressed("RBlue")
    local left  = INPUT:KeyboardPressed("LeftArrow") or INPUT:Pressed("LBlue")
    if right then self:setTab(fw._tab % #TABS + 1)
    elseif left then self:setTab((fw._tab - 2) % #TABS + 1) end
end

-- ── detail state (recomputed on selection change, drawn every frame) ──────────────────────────────
function PC:refreshDetail()
    local save = self.save
    local e = self:entryAt(self.sel or 1)
    self.detail = nil
    if e == nil then
        if self.actionBtn then self.actionBtn:setVisible(false) end
        return
    end
    local d = { entry = e }
    d.owned = isOwned(save, self.tab, e)
    d.equipped = isEquipped(save, self.tab, e)
    d.name = entryName(save, self.tab, e)
    local uc = nil
    if self.tab == 1 or self.tab == 2 then
        d.rarity = e.Rarity
        uc = e.UnlockCondition
    elseif self.tab == 3 then
        -- dan plate preview, baked 1:1 with C# CStageHeya's gallery (CStageHeya.cs:71-81):
        -- gold ranks: TitleTextureKey("<g...>title</g>", font14, Color.Gold, Color.Black, 1000)
        -- normal:     TitleTextureKey(title, font14, Color.White, Color.Black, 1000)
        d.danGrade = e.ClearStatus or 0
        d.danGold = e.IsGold and true or false
        local label = d.danGold and string.format(GOLD_MARKUP, e.Title or "") or (e.Title or "")
        pcall(function()
            bakeColors()
            d.plateTx = self.ui:font(HEYA_FONT):GetText(label, false, 1000,
                d.danGold and danGoldFg or danFg, danBg)
        end)
    elseif self.tab == 4 then
        d.rarity = e.Rarity
        uc = e.UnlockCondition
        local title = d.owned and (e.Title or "") or I18N.tr("???")
        -- baked EXACTLY like the C# customize gallery: black on transparent, maxwidth 1000
        -- (the skin's drawTitlePlate clamps the drawn width itself)
        pcall(function()
            bakeColors()
            d.plateTx = self.ui:font(HEYA_FONT):GetText(title, false, 1000, plateFg, plateBg)
        end)
    end
    if not d.owned and uc and uc.HasCondition then
        d.price = 0
        pcall(function() d.price = uc:GetCoinPrice() or 0 end)
        local ctype = ""
        pcall(function() ctype = uc:GetConditionType() or "" end)
        -- a locked entry must ALWAYS show a real line: message → blocked reason → coin price;
        -- "???" only for the genuinely hidden condition types
        local m = ""
        if OPAQUE_COND[ctype] then
            m = I18N.tr("???")
        else
            pcall(function() m = uc:GetConditionMessage() or "" end)
            if m == "" then pcall(function() m = uc:GetBlockedMessage(0) or "" end) end
            if m == "" and d.price > 0 then m = I18N.trf("Purchase for %d coins.", d.price) end
            if m == "" then m = I18N.tr("???") end
        end
        d.cond = m
        d.unlockable = false
        pcall(function() d.unlockable = uc:IsUnlockable(0) end)
    end
    self.detail = d
    -- action button state
    if self.actionBtn then
        self.actionBtn:setVisible(true)
        if d.equipped then
            self.actionBtn:setText(I18N.tr("Equipped")); self.actionBtn:setEnabled(false)
        elseif d.owned then
            self.actionBtn:setText(I18N.tr("Equip")); self.actionBtn:setEnabled(true)
        elseif d.price and d.price > 0 and d.unlockable then
            self.actionBtn:setText(I18N.trf("Buy & Equip  (%d coins)", d.price))
            self.actionBtn:setEnabled(d.price <= (save.Coins or 0))
        else
            self.actionBtn:setText(I18N.tr("Locked")); self.actionBtn:setEnabled(false)
        end
    end
end

-- ── actions ───────────────────────────────────────────────────────────────────────────────────────
function PC:activateSelected()
    local save = self.save
    local e = self:entryAt(self.sel or 1)
    if e == nil then return end          -- header rows (nameplates tab) are inert
    local tab = self.tab
    -- failed-unlock feedback: red row flash + the error blip (no text line, no rebuild)
    local function denied()
        if self.menu then
            self.menu._flashI = self.sel
            self.menu._flashT = 0.4
        end
        errPlay()
    end
    local function unlockEquip(owned, uc, unlock, equip)
        if owned then equip(); self.msg = I18N.tr("Equipped!"); return end
        if not (uc and uc.HasCondition) then equip(); self.msg = I18N.tr("Equipped!"); return end
        local can = false
        pcall(function() can = uc:IsUnlockable(0) end)
        if not can then denied(); return end
        local price = uc:GetCoinPrice() or 0
        if price > 0 and price > (save.Coins or 0) then denied(); self.msg = I18N.tr("Not enough coins."); return end
        if price > 0 then save:SpendCoins(price) end
        unlock(); equip()
        self.msg = (price > 0) and I18N.trf("Purchased for %d coins!", price) or I18N.tr("Unlocked!")
    end
    if tab == 1 then
        unlockEquip(save:IsCharacterUnlocked(e.FolderName), e.UnlockCondition,
            function() save:UnlockCharacter(e.FolderName) end,
            function() save:ChangeCharacter(e.FolderName) end)
    elseif tab == 2 then
        unlockEquip(save:IsPuchicharaUnlocked(e.FolderName), e.UnlockCondition,
            function() save:UnlockPuchichara(e.FolderName) end,
            function() save:ChangePuchichara(e.FolderName) end)
    elseif tab == 3 then
        save:ChangeDan(e.Title)
        self.msg = I18N.tr("Dan title set.")
    elseif tab == 4 then
        unlockEquip(save:IsNameplateUnlocked(e.Id), e.UnlockCondition,
            function() save:UnlockNameplate(e.Id) end,
            function() save:ChangeNameplate(e.Id) end)
    end
    -- surgical refresh — NO rebuild (scroll + cursor survive): row faces read live save state
    -- through rowStyle every frame; only the selected row's TEXT can change (a locked "???"
    -- nameplate revealing its title on unlock), and the detail pane re-derives its state
    if self.menu then
        local it = self.menu.items[self.sel or 1]
        if it then it.text = entryName(save, tab, e) end
    end
    self:refreshDetail()
end

function PC:applyRename()
    local n = self.renameVal
    if self.tb and self.tb.value and self.tb.value ~= "" then n = self.tb.value end
    if n == nil or n == "" then self.msg = I18N.tr("Enter a name first."); return end
    if self.save then
        self.save:ChangeName(n)
        self.msg = I18N.trf("Name changed to %s.", n)
    end
end

-- ── open / close ──────────────────────────────────────────────────────────────────────────────────
function PC:openScreen()
    I18N.detect()                        -- pick up the active game language on every open
    self.save = GetSaveFile(self.playerIndex or 0)   -- the room's chosen player (coins/unlocks are theirs)
    self.isOpen = true
    self.tab = 1
    self.sel = 1
    self.msg = nil
    self._wantClose = false
    self._justOpened = true
    self.loadedAnims = {}
    self:buildLists()
    self:buildUI()
    bgmPlay()
end

function PC:close()
    self.isOpen = false
    if self.ui then self.ui:disposeWidgets(); self.ui = nil end
    self.menu, self.actionBtn, self.tb, self.tabBtns = nil, nil, nil, nil
    -- free the character menu animations loaded for previews
    for _, entry in pairs(self.loadedAnims or {}) do
        pcall(function() entry.Character:DisposeAnimation(CHARACTER.ANIM_MENU_NORMAL) end)
    end
    self.loadedAnims = {}
    bgmStop()
end

-- returns "closed" once the screen has shut, else "running"
function PC:update(ts)
    if not self.isOpen then return "closed" end
    if self.ui == nil then self:close(); return "closed" end
    -- consume the opening frame: the Enter/Space edge that opened the screen must never reach the
    -- freshly built UI (it could read as Decide/cancel inside the manager on the same frame)
    if self._justOpened then self._justOpened = false; return "running" end
    -- The manager returns NIL on a normal frame and "cancel" only on Esc/Cancel. This was once
    -- written `self.ui and self.ui:update(ts) or "cancel"` — the nil result collapsed to "cancel",
    -- closing the screen on its first update (and the next Esc/Enter then quit the stage). Keep the
    -- comparison explicit.
    local r = self.ui:update(ts)
    if self.ui and not self.ui:isCapturing() then self:handleTabKeys() end
    -- failed-unlock red flash decay (~0.4 s)
    local dt = math.max(0, math.min(0.1, (ts - (self._lastTs or ts)) / 1000))
    self._lastTs = ts
    if self.menu and self.menu._flashT then
        self.menu._flashT = self.menu._flashT - dt
        if self.menu._flashT <= 0 then
            self.menu._flashT, self.menu._flashI, self.menu._flashK = nil, nil, nil
        else
            self.menu._flashK = math.min(1, self.menu._flashT / 0.4)
        end
    end
    if self._wantClose or r == "cancel" then
        self:close()
        return "closed"
    end
    return "running"
end

-- ── draw ──────────────────────────────────────────────────────────────────────────────────────────
local function ensureCharaAnim(self, entry)
    if self.loadedAnims[entry.FolderName] == nil then
        local ok = pcall(function() entry.Character:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL) end)
        self.loadedAnims[entry.FolderName] = ok and entry or nil
        if not ok then return false end
    end
    return true
end

function PC:draw()
    if not self.isOpen or self.ui == nil then return end
    local ui = self.ui
    ui:rect(0, 0, SW, SH, 8, 10, 18, 150)                        -- dim the room behind
    ui:draw()
    local save = self.save

    -- coins (dynamic → glyph text, no per-frame texture churn)
    ui:drawTextEx(22, I18N.trf("Coins: %d", save and save.Coins or 0),
        PANEL.x + PANEL.w - 60, PANEL.y + 152, { 255, 226, 130 }, { 0, 0, 0, 220 }, 1, 1, 0, "right")

    if self.tab == 5 then
        ui:drawTextEx(24, I18N.trf("Current name:  %s", tostring(save and save.Name or "")),
            LIST.x, LIST.y, { 52, 58, 92 }, { 255, 255, 255, 160 })
        ui:drawTextEx(18, I18N.tr("Type a new name, then Apply (or press Enter)."),
            LIST.x, LIST.y + 46, { 108, 114, 146 }, { 255, 255, 255, 120 })
    else
        local d = self.detail
        if d then
            local y = PANEL.y + 196
            ui:drawTextEx(28, d.name, DETAIL_CX, y, { 52, 58, 92 }, { 255, 255, 255, 170 }, 1, 1, DETAIL_W - 60, "top")
            if d.rarity then
                local rc = RARITY_COL[d.rarity] or { 190, 195, 200 }
                ui:drawTextEx(20, tostring(d.rarity), DETAIL_CX, y + 44, rc, { 0, 0, 0, 200 }, 1, 1, 0, "top")
            end
            -- preview BOX y+96 .. y+396: previews are MEASURED and scaled to stay inside it, so they
            -- can no longer overlap the name/rarity strip above or the unlock text below
            local boxTop, boxH = y + 96, 300
            local boxBottom = boxTop + boxH
            local e = d.entry
            if self.tab == 1 then
                if ensureCharaAnim(self, e) then
                    pcall(function()
                        local sc = 0.8
                        local sz = e.Character:GetAnimationSize(CHARACTER.ANIM_MENU_NORMAL)
                        if sz and sz.Y and sz.Y > 0 then
                            sc = math.min(1.15, boxH / sz.Y, (DETAIL_W - 100) / math.max(1, sz.X))
                        end
                        local col = d.owned and 1.0 or 0.42
                        e.Character:SetColor(col, col, col)
                        e.Character:SetScale(sc, sc)
                        e.Character:Update(CHARACTER.ANIM_MENU_NORMAL, true)
                        e.Character:DrawAtAnchor(DETAIL_CX, boxBottom, CHARACTER.ANIM_MENU_NORMAL, "bottom")
                    end)
                end
            elseif self.tab == 2 then
                pcall(function()
                    if e.tx ~= nil and e.tx.Loaded then
                        local frameW = math.floor(e.tx.Width / 2)
                        local frameH = e.tx.Height
                        local sc = math.min(1.5, boxH / math.max(1, frameH), (DETAIL_W - 140) / math.max(1, frameW))
                        local g = d.owned and 255 or 110
                        e.tx:SetColor(COLOR:CreateColorFromRGBA(g, g, g, 255))
                        e.tx:SetScale(sc, sc)
                        e.tx:DrawRectAtAnchor(DETAIL_CX, boxBottom, 0, 0, frameW, frameH, "bottom")
                    end
                end)
            elseif self.tab == 3 then
                if d.plateTx then
                    pcall(function()
                        -- the exact customize-screen call (CStageHeya's dan row → NamePlate.DrawDan):
                        -- the skin's mode-1 handler shares the mode-2 geometry (Base.png 330x81 drawn
                        -- top-left at (x-180, y+5)) — pass the plate's centre point like the titles
                        NAMEPLATE:DrawDanPlate(math.floor(DETAIL_CX + 15),
                            math.floor(boxTop + boxH / 2 - 46), 255, d.danGrade or 0, d.plateTx)
                    end)
                end
            elseif self.tab == 4 then
                if d.plateTx then
                    pcall(function()
                        -- the exact customize-screen call (CStageHeya:429): the skin's mode-2
                        -- handler draws Base.png (330x81) top-left at (x-180, y+5) and centres
                        -- the text itself — pass the plate's centre point, never hand-place by
                        -- the text texture's height. Unowned plates use Heya's fallback args
                        -- (type -1, rarity 1, id -1: base plate only, no type art / badges).
                        local px2 = math.floor(DETAIL_CX + 15)
                        local py2 = math.floor(boxTop + boxH / 2 - 46)
                        if d.owned then
                            NAMEPLATE:DrawTitlePlate(px2, py2, 255, e.Type or 0, d.plateTx,
                                RARITY_LANG[e.Rarity] or 1, e.Id or -1)
                        else
                            NAMEPLATE:DrawTitlePlate(px2, py2, 130, -1, d.plateTx, 1, -1)
                        end
                    end)
                end
            end
            -- status / unlock block, always BELOW the preview box (the coin price line is gone:
            -- the Buy button already carries the price)
            local yU = boxBottom + 18
            if d.equipped then
                ui:drawTextEx(20, I18N.tr("[ Equipped ]"), DETAIL_CX, yU, { 255, 226, 130 }, { 0, 0, 0, 220 }, 1, 1, 0, "top")
            elseif not d.owned then
                ui:drawTextEx(20, I18N.tr("How to unlock:"), DETAIL_X + 40, yU, { 255, 210, 140 }, { 0, 0, 0, 210 })
                -- drawWrapped has no outline (transparent back): the condition text must use the
                -- panel's DARK theme colour — near-white here vanished into the light surface
                ui:drawWrapped(19, d.cond or I18N.tr("???"), DETAIL_X + 40, yU + 32, DETAIL_W - 80, { 52, 58, 92 })
            end
        end
    end

    if self.msg then
        ui:drawTextEx(22, self.msg, math.floor(PANEL.x + PANEL.w / 2), PANEL.y + PANEL.h - 34,
            { 190, 255, 190 }, { 0, 0, 0, 230 }, 1, 1, PANEL.w - 100, "top")
    end
end

return PC
