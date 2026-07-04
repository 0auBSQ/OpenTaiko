---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- replaylist.lua  —  Best-plays panel for single-player regular (performance) song select.
--
-- Shows the top replays for the hovered chart+difficulty as PopUI cards down the right edge of the
-- difficulty-select screen. Cards are MOUSE-ONLY (the keyboard drives the difficulty bars): hover + click
-- animation come from PopUI widgets, but hover/press are driven here by the mouse so Enter never activates a
-- card. Clicking a watchable card opens a confirm showing the play's rank/score/date/player; confirming arms
-- the replay (REPLAY:Watch) and mounts the chart, then song_select_core returns "play".
--
-- Scrolling is smooth (eased pixel offset) with a draggable scrollbar; cards sliding past the strip's top or
-- bottom stay partially visible and are cut cleanly settings-style: masks drawn with the panel background's
-- own pixels (DrawRect, source rect == screen rect) cover the overflow, and the header redraws on top.
-- Lists load asynchronously (REPLAY:ListReplaysAsync): every difficulty prefetches when the difficulty
-- screen opens, and a loading line shows until the hovered list lands.

local PopUI = require("PopUI")

local M = {}
local G            -- shared state injected by Script.lua
local ui           -- PopUI manager (widget baking + theme + sfx)
local cards = {}   -- pool of card widgets, reused across scroll
local dimCanvas    -- full-screen dim behind the confirm
local warnTex      -- baked warning-triangle canvas (2x supersampled, drawn at half scale)
local errTex       -- baked error circle (red, white X) for unwatchable replays

-- ── Layout ──────────────────────────────────────────────────────────────────────
-- Card vertical budget (local y, top-left origin; CARD_H = 212):
--   12..34  rank #   |  40..70  score   |  80..97 player  |  100..113 date
--   12..72  rank+clear badges (top-right, overlaid, 0.46x of 120x130)
--   116..148  judges row (label 116, value 132)
--   166..203  mod icons (7-slot menu row: 7x45 + 37 = 307 wide, 37 tall) — 18px clear of the judges row
local STRIP_W   = 450
local CARD_W    = 400
local CARD_H    = 212
local CARD_GAP  = 10
local STEP      = CARD_H + CARD_GAP
local CARD_X    = 1920 - STRIP_W + math.floor((STRIP_W - CARD_W) / 2)   -- 1495
local LIST_TOP  = 262
local HEADER_Y  = 206
local VISIBLE   = 3                              -- cards fully visible at once
local POOL      = VISIBLE + 2                    -- pool covers the viewport + a partial card at each edge
local VIEW_H    = VISIBLE * STEP - CARD_GAP      -- 656; viewport bottom = 918 <= 1080
local VIEW_BOT  = LIST_TOP + VIEW_H
local PAD       = 18
local SB_X, SB_W = CARD_X + CARD_W + 10, 6       -- scrollbar gutter right of the cards

-- ── Theme (matches the settings menu: cool steel-blue, light card faces) ──────────
local THEME = {
    name = "ReplayCards",
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
    radius = 18, radiusSmall = 10, outlineWidth = 3, gloss = true,
    shadow = { dx = 0, dy = 4, layers = 4, grow = 3 },
}

local COL_TEXT   = { 44, 56, 74 }
local COL_SUB    = { 96, 108, 128 }
local COL_ACCENT = { 72, 108, 156 }
local COL_SCORE  = { 30, 42, 60 }
-- judge tag colours roughly mirror the in-game judgements
local JUDGES = {
    { tag = "GOOD",  field = "Good",  col = { 224, 168, 36 } },
    { tag = "OK",    field = "Ok",    col = { 120, 180, 96 } },
    { tag = "BAD",   field = "Bad",   col = { 196, 92, 92 } },
    { tag = "BOOM",  field = "Boom",  col = { 150, 96, 176 } },
    { tag = "ADL",   field = "ADLib", col = { 96, 150, 196 } },
}

local function clamp(v, lo, hi) if v < lo then return lo elseif v > hi then return hi else return v end end

-- ── Init ──────────────────────────────────────────────────────────────────────
function M.init(g)
    G = g
    G.replayList    = nil   -- best plays for the hovered chart+difficulty (a C# ReplayHeader[])
    G.replayListKey = nil   -- uniqueId/diff the list was fetched for
    G.replayLoading = false -- the hovered list's async fetch is still in flight
    G.replayScroll  = 0      -- current scroll offset in PIXELS (eased toward M._scrollTarget)
    G.replayConfirm = nil    -- { filepath, diff, header, rank, chartPath } while the confirm prompt is up
    M._scrollTarget = 0
    M._sbDrag       = false
    M._lastTs       = nil
    M._lists        = {}     -- key → ReplayHeader[] (fetched)
    M._pending      = {}     -- key → async handle (fetch in flight)
    M._loadAnim     = 0
end

-- regular (performance) song select: the only mode that shows best plays / can launch a replay.
-- AI battle mounts an AI slot, the online lobby is song-only, and training (checked directly so no
-- activation-parameter path can leak through) keeps the plain layout with no replays.
function M.isRegular()
    return G.activeConfig ~= nil
        and G.activeConfig.lockedPlayerCount == nil
        and not G.activeConfig.mountAISlotToP2
        and not G.activeConfig.songOnly
        and not CONFIG.IsTrainingMode
end

local function active()
    return CONFIG.PlayerCount == 1 and M.isRegular()
end

-- diffselect shifts the chara/nameplate left only when the strip actually shows
function M.isActive() return active() end

-- the chart + difficulty the P1 cursor is hovering, or nil (only on a real difficulty bar)
local function hoveredChart()
    if not active() or G.selectedSongNode == nil then return nil, nil end
    local idx = G.diffIndex[1]
    if idx < 3 then return nil, nil end
    local bar = G.diffBars[idx - 2]
    if bar == nil then return nil, nil end
    local chart = G.selectedSongNode:GetChart(bar.difficulty)
    if chart == nil then return nil, nil end
    return chart, bar.difficulty
end

-- ── Async list fetch ─────────────────────────────────────────────────────────────
-- Lists load off-thread. Passing the chart file path lets C# flag each replay whose stored chart md5 no
-- longer matches (chart edited since the play).
local function keyFor(chart, diff) return (chart.UniqueId or "") .. "/" .. diff end

local function startFetch(chart, diff)
    local key = keyFor(chart, diff)
    if M._lists[key] ~= nil or M._pending[key] ~= nil then return end
    M._pending[key] = REPLAY:ListReplaysAsync(chart.SongFolder, chart.UniqueId, diff, 50, chart.ChartPath)
end

local function pollFetches()
    for key, h in pairs(M._pending) do
        if h.IsDone then
            M._lists[key]  = h.Result
            M._pending[key] = nil
        end
    end
end

-- kick the fetches for every difficulty of the song; called when the difficulty screen opens
function M.prefetchForSong(node)
    if not active() or node == nil then return end
    for d = 0, 4 do
        local chart = node:GetChart(d)
        if chart ~= nil then startFetch(chart, d) end
    end
end

-- point G.replayList at the hovered difficulty's fetched list (nil + loading flag while in flight)
local function refresh(chart, diff)
    local key = keyFor(chart, diff)
    startFetch(chart, diff)
    G.replayChartPath = chart.ChartPath
    local rows = M._lists[key]
    G.replayLoading = rows == nil
    if G.replayListKey ~= key or G.replayList ~= rows then
        G.replayList    = rows
        G.replayListKey = key
        G.replayScroll  = 0
        M._scrollTarget = 0
    end
end

-- ── Card content ────────────────────────────────────────────────────────────────
local function commas(n)
    local s = tostring(math.tointeger(n) or n)
    local out, c = "", 0
    for i = #s, 1, -1 do
        out = s:sub(i, i) .. out; c = c + 1
        if c % 3 == 0 and i > 1 then out = "," .. out end
    end
    return out
end

-- score-rank / clear-status textures use the same encoding as the song bar:
--   cs: 0 = failed (m1 icon), else cs-1.   sr: 0 = m1 icon, else sr-1.
local function clearTex(cs)
    if cs == 0 then return G.bars["clearstatus_m1"] end
    return G.bars["clearstatus_" .. (cs - 1)]
end
local function rankTex(sr)
    if sr == 0 then return G.bars["scorerank_m1"] end
    return G.bars["scorerank_" .. (sr - 1)]
end

local function drawIconScaled(tex, cx, cy, scale, op)
    if tex == nil then return end
    tex:SetScale(scale, scale)
    tex:SetOpacity(op)
    tex:DrawAtAnchor(math.floor(cx), math.floor(cy), "center")
    tex:SetScale(1, 1)
    tex:SetOpacity(1)
end

-- bake a clean warning triangle (amber, dark outline, "!" cut in) at 2x; drawn at half scale so the edges are
-- smoothed by the downscale filtering
local function bakeWarnTriangle()
    local W, H = 64, 56
    local c = CANVAS:CreateCanvas(W, H)
    c:Clear(0, 0, 0, 0)
    local cx = W / 2
    -- dark outline triangle (full size)
    for y = 0, H - 1 do
        local half = ((y + 1) / H) * (W / 2 - 1)
        c:FillRect(math.floor(cx - half), y, math.max(1, math.floor(half * 2 + 0.5)), 1, 146, 96, 10, 255)
    end
    -- amber face, inset from the outline
    for y = 6, H - 4 do
        local half = ((y - 5) / (H - 9)) * (W / 2 - 5)
        c:FillRect(math.floor(cx - half), y, math.max(1, math.floor(half * 2 + 0.5)), 1, 246, 189, 55, 255)
    end
    -- "!" glyph: rounded bar + dot, dark
    c:FillRect(math.floor(cx - 3), 20, 6, 18, 84, 54, 4, 255)
    c:FillRect(math.floor(cx - 2), 18, 4, 2, 84, 54, 4, 255)
    c:FillRect(math.floor(cx - 3), 43, 6, 6, 84, 54, 4, 255)
    c:Upload()
    return c
end

-- red error circle with a white X (2x supersampled, drawn at half scale) for unwatchable replays
local function bakeErrCircle()
    local D = 56
    local c = CANVAS:CreateCanvas(D, D)
    c:Clear(0, 0, 0, 0)
    local r, cx = D / 2, D / 2
    -- dark red rim
    for y = 0, D - 1 do
        local dy = y + 0.5 - cx
        local half = math.sqrt(math.max(0, r * r - dy * dy))
        if half >= 1 then
            c:FillRect(math.floor(cx - half), y, math.max(1, math.floor(half * 2 + 0.5)), 1, 152, 36, 36, 255)
        end
    end
    -- lighter face inset from the rim
    for y = 3, D - 4 do
        local dy = y + 0.5 - cx
        local ri = r - 3
        local half = math.sqrt(math.max(0, ri * ri - dy * dy))
        if half >= 1 then
            c:FillRect(math.floor(cx - half), y, math.max(1, math.floor(half * 2 + 0.5)), 1, 206, 58, 58, 255)
        end
    end
    -- white X: two 45-degree bars
    for t = -13, 13 do
        c:FillRect(math.floor(cx + t - 2), math.floor(cx + t), 5, 1, 255, 255, 255, 255)
        c:FillRect(math.floor(cx - t - 2), math.floor(cx + t), 5, 1, 255, 255, 255, 255)
    end
    c:Upload()
    return c
end

-- draw one card's content; cx,cy = card centre, s = hover/press scale
local function drawCardContent(slot, cx, cy, s)
    local r = slot.row
    if r == nil then return end
    local op = slot.enabled and 1.0 or 0.45

    -- local (card top-left origin) → screen, with the hover scale baked in
    local function lx(x) return cx + (x - CARD_W / 2) * s end
    local function ly(y) return cy + (y - CARD_H / 2) * s end

    -- rank / score / player / date (left column)
    ui:drawText(22, string.format("#%d", slot.rank or 0), lx(PAD), ly(12), COL_ACCENT, op, s)
    ui:drawText(30, commas(r.Score),         lx(PAD), ly(40), COL_SCORE,  op, s)
    ui:drawText(17, r.PlayerName,            lx(PAD), ly(80), COL_TEXT,   op, s)
    ui:drawText(13, r.Date,                  lx(PAD), ly(100), COL_SUB,   op, s)

    -- score-rank + clear-status badges (top-right, overlaid like the song bar)
    local iconCx, iconCy = lx(CARD_W - 50), ly(42)
    drawIconScaled(clearTex(r.ClearStatus), iconCx, iconCy, 0.46 * s, op)
    drawIconScaled(rankTex(r.ScoreRank),    iconCx, iconCy, 0.46 * s, op)

    -- judges row (single line, 5 columns)
    local jy = ly(116)
    local colW = (CARD_W - PAD * 2) / #JUDGES
    for i, j in ipairs(JUDGES) do
        local jx = lx(PAD + (i - 1) * colW + 2)
        ui:drawText(12, j.tag,      jx, jy,        op < 1 and COL_SUB or j.col, op, s)
        ui:drawText(15, tostring(r[j.field] or 0), jx, jy + 16 * s, COL_TEXT, op, s)
    end

    -- mod icons for this play (the menu row reads the replay's recorded mod values; no Auto)
    if G.modicons_ro ~= nil then
        G.modicons_ro:Call("drawFlags", math.floor(lx(PAD)), math.floor(ly(166)),
            r.ModFlags, r.ScrollSpeed, r.SongSpeed, r.JudgeStrictness, "menu", op * 255)
    end

    -- top-left badge; the details show in a tooltip while the card is hovered (see M.draw).
    -- unwatchable → red error circle (full opacity so it pops on the dimmed card);
    -- watchable with caveats (older version / edited chart) → amber warning triangle.
    if not r.Watchable then
        if errTex == nil then errTex = bakeErrCircle() end
        errTex:SetScale(0.5 * s, 0.5 * s)
        errTex:DrawAtAnchor(math.floor(lx(24)), math.floor(ly(22)), "center")
        errTex:SetScale(1, 1)
    elseif r.OldVersion or r.ChecksumMismatch then
        if warnTex == nil then warnTex = bakeWarnTriangle() end
        warnTex:SetScale(0.5 * s, 0.5 * s)
        warnTex:SetOpacity(op)
        warnTex:DrawAtAnchor(math.floor(lx(24)), math.floor(ly(22)), "center")
        warnTex:SetScale(1, 1)
        warnTex:SetOpacity(1)
    end
end

-- ── UI build ────────────────────────────────────────────────────────────────────
local function ensureUI()
    if ui ~= nil then return end
    ui = PopUI.new{
        theme = THEME,
        bg    = false,
        sfx   = {
            click = function() G.sounds.Decide:Play() end,
            hover = function() G.sounds.Skip:Play() end,
        },
    }
    for i = 1, POOL do
        local slot = ui:button{
            text = " ", w = CARD_W, h = CARD_H, x = CARD_X, y = LIST_TOP,
            onClick = function(self)
                if self.row ~= nil and self.row.Watchable then
                    G.replayConfirm = {
                        filepath = self.row.FilePath, diff = self.diff,
                        header   = self.row, rank = self.rank,
                        chartPath = G.replayChartPath,
                    }
                end
            end,
        }
        slot.focusable = false            -- mouse-only; never take keyboard focus
        slot.drawContent = function(self, cx, cy, sc) drawCardContent(self, cx, cy, sc) end
        -- widget draw (partial cards are cut by the clip rect set in M.draw, no fading needed)
        slot.draw = function(self)
            if not self.visible then return end
            local ccx, ccy = self.x + self.w * 0.5, self.y + self.h * 0.5
            local sc = self._scaleCur or 1
            if self._ring and (self._hiCur or 0) > 0.01 then
                self._ring.canvas:SetOpacity(self._hiCur)
                self._ring.canvas:SetScale(sc, sc)
                self._ring.canvas:DrawAtAnchor(math.floor(ccx), math.floor(ccy), "center")
            end
            if self._body then
                self._body.canvas:SetColor(1, 1, 1)
                self._body.canvas:SetOpacity(self.enabled and 1.0 or 0.5)
                self._body.canvas:SetScale(sc, sc)
                self._body.canvas:DrawAtAnchor(math.floor(ccx), math.floor(ccy), "center")
            end
            self:drawContent(ccx, ccy, sc)
        end
        cards[i] = slot
    end
end

local function hideAll()
    for _, c in ipairs(cards) do c:setVisible(false); c:setHover(false) end
end

local function contentMetrics(n)
    local contentH = n * STEP - CARD_GAP
    return contentH, math.max(0, contentH - VIEW_H)
end

-- ── Per-frame update (mouse only) ─────────────────────────────────────────────────
-- returns "play" (start the replay), "consume" (took input; skip difficulty nav), or nil.
function M.handleUpdate(ts)
    -- confirm prompt takes over input until answered
    if G.replayConfirm ~= nil then
        if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
            local c = G.replayConfirm
            G.replayConfirm = nil
            if REPLAY:Watch(c.filepath, c.chartPath) then
                G.selectedSongNode:Mount(c.diff, 0, 0, 0, 0)
                G.sounds.Decide:Play()
                G.lastSignal = "play"
                return "play"
            end
        elseif INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
            G.sounds.Cancel:Play(); G.replayConfirm = nil
        end
        return "consume"
    end

    pollFetches()

    local chart, diff = hoveredChart()
    if chart == nil then if ui then hideAll() end return nil end
    refresh(chart, diff)
    ensureUI()

    -- frame delta for the scroll easing
    local dt = 1 / 60
    if ts ~= nil then
        if M._lastTs ~= nil then dt = clamp((ts - M._lastTs) / 1000.0, 0, 0.1) end
        M._lastTs = ts
    end
    if G.replayLoading then M._loadAnim = (M._loadAnim or 0) + dt end

    local n = (G.replayList and G.replayList.Length) or 0
    local contentH, maxScroll = contentMetrics(n)
    local mx, my  = INPUT:GetMouseXY()
    local inside  = INPUT:IsMouseInside()

    -- mouse wheel: one card per notch, eased below
    local _, dy = INPUT:GetScrollDelta()
    if dy ~= 0 then M._scrollTarget = clamp(M._scrollTarget - dy * STEP, 0, maxScroll) end

    -- scrollbar drag: press anywhere on the gutter jumps/drags the thumb
    if maxScroll > 0 and inside then
        local onGutter = mx >= SB_X - 6 and mx <= SB_X + SB_W + 6 and my >= LIST_TOP and my <= VIEW_BOT
        if INPUT:MousePressed("Left") and onGutter then M._sbDrag = true end
    end
    if M._sbDrag then
        if INPUT:MousePressing("Left") then
            local thumbH = math.max(28, VIEW_H * VIEW_H / math.max(1, contentH))
            local t = clamp((my - LIST_TOP - thumbH / 2) / math.max(1, VIEW_H - thumbH), 0, 1)
            M._scrollTarget = t * maxScroll
            G.replayScroll  = M._scrollTarget          -- track the pointer directly while dragging
        else
            M._sbDrag = false
        end
    end

    -- ease the scroll toward the target
    M._scrollTarget = clamp(M._scrollTarget, 0, maxScroll)
    G.replayScroll = G.replayScroll + (M._scrollTarget - G.replayScroll) * math.min(1, dt * 14)
    if math.abs(M._scrollTarget - G.replayScroll) < 0.5 then G.replayScroll = M._scrollTarget end

    -- assign data + position the pool (pixel-scrolled; cards partially past the viewport edges stay
    -- visible and get cut by the clip rect in M.draw)
    local firstIdx = math.max(0, math.floor(G.replayScroll / STEP))
    for i = 1, POOL do
        local slot = cards[i]
        local idx  = firstIdx + (i - 1)
        local y    = LIST_TOP + idx * STEP - G.replayScroll
        if idx < n and y + CARD_H > LIST_TOP and y < VIEW_BOT then
            slot.row  = G.replayList[idx]
            slot.rank = idx + 1
            slot.diff = diff
            slot.x    = CARD_X
            slot.y    = math.floor(y + 0.5)
            slot:setVisible(true)
            slot:setEnabled(slot.row.Watchable)
        else
            slot.row = nil
            slot:setVisible(false)
        end
    end

    -- manual mouse hover/press (so the keyboard stays free for the difficulty bars); gated to the viewport so
    -- a clipped edge card can't be clicked through the header or below the strip. Unwatchable (disabled)
    -- cards still hover so their error tooltip shows — they just never press.
    local hoverW = nil
    if inside and not M._sbDrag and my >= LIST_TOP and my <= VIEW_BOT then
        for i = POOL, 1, -1 do
            local slot = cards[i]
            if slot.visible and slot:hitTest(mx, my) then
                hoverW = slot; break
            end
        end
    end
    if hoverW ~= M._hoverW then
        if M._hoverW then M._hoverW:setHover(false) end
        if hoverW and hoverW.enabled then hoverW:setHover(true); G.sounds.Skip:Play() end
        M._hoverW = hoverW
    end
    if INPUT:MousePressed("Left") and hoverW and hoverW.enabled then hoverW:press(); M._pressW = hoverW end
    if INPUT:MouseReleased("Left") and M._pressW then
        M._pressW:release(M._pressW:hitTest(mx, my)); M._pressW = nil
    end

    for i = 1, POOL do if cards[i].visible then cards[i]:update(nil) end end
    return nil
end

-- ── Draw (called from diffselect.drawPanel; only when the panel is settled) ─────────

-- repaint the layers that actually cover the strip's screen region, in their draw order (Script.lua draw
-- + diffselect.drawPanel), each clipped to the band. The strip sits over the WRAPPING SCROLLED shared
-- background — bg_difficultyselect is a 1006px-wide left panel that never reaches it (sampling it out of
-- range was the "weird patterns" bug), the header strip covers y<120 and the top-right overlay y<218.
local function drawMaskBand(bx, by, bw, bh)
    local bgTex = SHARED:GetSharedTexture("background")
    if bgTex.Width > 0 then
        for k = 0, 1 do
            local tileX = -G.backgroundScrollX + 1920 * k
            local x0 = math.max(bx, tileX)
            local x1 = math.min(bx + bw, tileX + 1920)
            if x1 > x0 then
                bgTex:DrawRect(math.floor(x0), by, math.floor(x0 - tileX), by, math.ceil(x1 - x0), bh)
            end
        end
    end
    local pan = G.bgtx["difficultyselect"]
    if bx < pan.Width then
        pan:DrawRect(bx, by, bx, by, math.min(bw, pan.Width - bx), math.min(bh, pan.Height - by))
    end
    local hd = G.bgtx["header"]
    if by < hd.Height and bx < hd.Width then
        hd:DrawRect(bx, by, bx, by, math.min(bw, hd.Width - bx), math.min(bh, hd.Height - by))
    end
    local ov = G.bgtx["overlay_difficulty"]
    local ox0 = 1920 - ov.Width
    local ix0 = math.max(bx, ox0)
    if ix0 < bx + bw and by < ov.Height then
        ov:SetOpacity(G.difficultySelectElemOpacity / 255)
        ov:DrawRect(math.floor(ix0), by, math.floor(ix0 - ox0), by,
            math.min(bx + bw, 1920) - math.floor(ix0), math.min(bh, ov.Height - by))
        ov:SetOpacity(1)
    end
end

local function drawDim(opacity)
    if dimCanvas == nil then
        local c = CANVAS:CreateCanvas(2, 2)
        c:Clear(0, 0, 0, 255); c:Upload()
        dimCanvas = c
    end
    dimCanvas:SetScale(960, 540)
    dimCanvas:SetOpacity(opacity)
    dimCanvas:Draw(0, 0)
    dimCanvas:SetScale(1, 1)
    dimCanvas:SetOpacity(1)
end

function M.draw()
    if not active() or G.activeScreen ~= "difficultyselect" then return end
    local chart = hoveredChart()
    if chart == nil and G.replayConfirm == nil then return end

    ensureUI()
    local n = (G.replayList and G.replayList.Length) or 0

    if G.replayLoading then
        local dots = string.rep(".", 1 + math.floor((M._loadAnim or 0) * 3) % 3)
        ui:drawText(18, "Loading replays" .. dots, CARD_X, LIST_TOP, COL_SUB, 1)
    elseif n == 0 then
        ui:drawText(18, "No replays yet", CARD_X, LIST_TOP, COL_SUB, 1)
    else
        for i = 1, POOL do if cards[i].visible then cards[i]:draw() end end
        -- settings-style masks: cover the card overflow above/below the strip by repainting the panel's
        -- background layers over the band (source rect == screen rect since they draw at fixed positions),
        -- in the same order drawPanel uses: bg, header, top-right overlay.
        local bandX, bandW = CARD_X - 30, CARD_W + 60
        local topH = CARD_H + 20
        drawMaskBand(bandX, LIST_TOP - topH, bandW, topH)
        local botH = math.min(CARD_H + 20, 1080 - VIEW_BOT)
        if botH > 0 then drawMaskBand(bandX, VIEW_BOT, bandW, botH) end
    end

    -- header drawn after the masks (it sits inside the top band, and a protruding card must not cover it)
    ui:drawText(28, "Best Plays", CARD_X, HEADER_Y, COL_ACCENT, 1)

    -- scrollbar (track + thumb) when the list overflows the viewport
    local contentH, maxScroll = contentMetrics(n)
    if maxScroll > 0 then
        local thumbH = math.max(28, VIEW_H * VIEW_H / contentH)
        local t = clamp(G.replayScroll / maxScroll, 0, 1)
        local thumbY = LIST_TOP + t * (VIEW_H - thumbH)
        ui:rect(SB_X, LIST_TOP, SB_W, VIEW_H, 70, 84, 104, 70)
        ui:rect(SB_X, thumbY, SB_W, thumbH,
            M._sbDrag and 200 or 150, 200, 232, M._sbDrag and 255 or 220)
    end

    -- tooltip while hovering a flagged card: red accent + reason for unwatchable replays (error circle),
    -- amber accent for watchable-with-caveats (warning triangle)
    local hov = M._hoverW
    if G.replayConfirm == nil and hov ~= nil and hov.visible and hov.row ~= nil then
        local lines = {}
        local accent = { 232, 172, 40 }
        if not hov.row.Watchable then
            local reason = hov.row.UnwatchableReason
            lines[#lines + 1] = (reason ~= nil and reason ~= "" and reason) or "This replay cannot be watched"
            accent = { 214, 72, 72 }
        end
        if hov.row.OldVersion then lines[#lines + 1] = "Recorded on an older game version" end
        if hov.row.ChecksumMismatch then lines[#lines + 1] = "The chart was modified after this play" end
        if #lines > 0 then
            local w = 0
            for _, l in ipairs(lines) do w = math.max(w, ui:measureText(16, l)) end
            local pad, lineH = 14, 24
            local bw = w + pad * 2 + 14              -- extra slack: glyph edges render wider than the advance sum
            local bh = #lines * lineH + pad * 2 - 6
            local mx, my = INPUT:GetMouseXY()
            local bx = math.max(8, math.min(mx + 18, 1920 - bw - 8))
            local by = math.max(8, my - bh - 12)
            ui:rect(bx, by, bw, bh, 32, 36, 46, 235)
            ui:rect(bx, by, bw, 4, accent[1], accent[2], accent[3], 255)
            for i, l in ipairs(lines) do
                ui:drawText(16, l, bx + pad, by + pad - 2 + (i - 1) * lineH, { 236, 228, 205 }, 1)
            end
        end
    end

    -- confirm prompt
    if G.replayConfirm ~= nil then
        local c   = G.replayConfirm
        local h   = c.header
        drawDim(0.62)
        local bx, by, bw, bh = 960 - 380, 540 - 190, 760, 380
        ui:rect(bx, by, bw, bh, 28, 36, 52, 235)
        ui:rect(bx, by, bw, 6, 96, 132, 180, 255)

        local cx = 960
        local wTitle = ui:measureText(38, "Watch this replay?")
        ui:drawText(38, "Watch this replay?", cx - wTitle / 2, by + 34, { 236, 242, 250 }, 1)

        if h ~= nil then
            -- rank + clear badges, centred above the details
            drawIconScaled(clearTex(h.ClearStatus), cx,        by + 150, 0.55, 1)
            drawIconScaled(rankTex(h.ScoreRank),    cx,        by + 150, 0.55, 1)

            local line1 = string.format("#%d", c.rank or 0) .. "    " .. commas(h.Score) .. " pts"
            local w1 = ui:measureText(26, line1)
            ui:drawText(26, line1, cx - w1 / 2, by + 218, { 210, 222, 236 }, 1)

            local w2 = ui:measureText(22, h.PlayerName)
            ui:drawText(22, h.PlayerName, cx - w2 / 2, by + 254, { 190, 204, 220 }, 1)

            local w3 = ui:measureText(18, h.Date)
            ui:drawText(18, h.Date, cx - w3 / 2, by + 284, { 150, 166, 186 }, 1)
        end

        local hint = "Enter: Yes        Esc: No"
        local wh = ui:measureText(20, hint)
        ui:drawText(20, hint, cx - wh / 2, by + bh - 44, { 170, 184, 202 }, 1)
    end
end

-- clear the confirm prompt and drop the cached list (called when leaving difficulty select and on activity
-- re-activation — i.e. also right after a play, so a freshly saved replay shows up without switching difficulty)
function M.reset()
    G.replayConfirm = nil
    G.replayList    = nil
    G.replayListKey = nil
    G.replayLoading = false
    G.replayScroll  = 0
    M._scrollTarget = 0
    M._sbDrag       = false
    M._lastTs       = nil
    M._lists        = {}
    M._pending      = {}   -- dropped handles just complete into garbage; results are re-fetched fresh
    M._loadAnim     = 0
    if M._hoverW then M._hoverW:setHover(false); M._hoverW = nil end
    M._pressW = nil
end

-- free baked GPU canvases (called from the activity's onDestroy)
function M.dispose()
    if ui ~= nil then ui:disposeWidgets() end
    if dimCanvas ~= nil then dimCanvas:Dispose(); dimCanvas = nil end
    if warnTex ~= nil then warnTex:Dispose(); warnTex = nil end
    if errTex ~= nil then errTex:Dispose(); errTex = nil end
    ui = nil; cards = {}
end

return M
