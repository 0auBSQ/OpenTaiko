---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- replaylist.lua  —  Best-plays panel for single-player regular (performance) song select.
--
-- Shows the top replays for the hovered chart+difficulty as PopUI cards down the right edge of the
-- difficulty-select screen. Cards are MOUSE-ONLY (the keyboard drives the difficulty bars): hover + click
-- animation come from PopUI widgets, but hover/press are driven here by the mouse so Enter never activates a
-- card. Clicking a watchable card opens a confirm showing the play's rank/score/date/player; confirming arms
-- the replay (REPLAY:Watch) and mounts the chart, then song_select_core returns "play".

local PopUI = require("PopUI")

local M = {}
local G            -- shared state injected by Script.lua
local ui           -- PopUI manager (widget baking + theme + sfx)
local cards = {}   -- pool of card widgets, reused across scroll
local dimCanvas    -- full-screen dim behind the confirm

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
local CARD_X    = 1920 - STRIP_W + math.floor((STRIP_W - CARD_W) / 2)   -- 1495
local LIST_TOP  = 262    -- moved down 150px
local HEADER_Y  = 206    -- moved down 150px
local ROWS      = 3      -- 262 + 3*(212+10) = 928 -> bottom 918 <= 1080
local PAD       = 18

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

-- ── Init ──────────────────────────────────────────────────────────────────────
function M.init(g)
    G = g
    G.replayList    = nil   -- best plays for the hovered chart+difficulty (a C# ReplayHeader[])
    G.replayListKey = nil   -- uniqueId/diff the list was fetched for
    G.replayScroll  = 0      -- index of the top visible card
    G.replayConfirm = nil    -- { filepath, diff, header, rank } while the confirm prompt is up
end

-- regular (performance) song select: the only mode that shows best plays / can launch a replay.
-- AI battle mounts an AI slot, training locks the player count, the online lobby is song-only.
function M.isRegular()
    return G.activeConfig ~= nil
        and G.activeConfig.lockedPlayerCount == nil
        and not G.activeConfig.mountAISlotToP2
        and not G.activeConfig.songOnly
end

local function active()
    return CONFIG.PlayerCount == 1 and M.isRegular()
end

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

-- (re)fetch the ranked replay list when the hovered chart/difficulty changes
local function refresh(chart, diff)
    local key = (chart.UniqueId or "") .. "/" .. diff
    if G.replayListKey == key and G.replayList ~= nil then return end
    G.replayList    = REPLAY:ListReplays(chart.SongFolder, chart.UniqueId, diff, 50)
    G.replayListKey = key
    G.replayScroll  = 0
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

-- draw one card's content; cx,cy = card centre, s = hover/press scale
local function drawCardContent(slot, cx, cy, s)
    local r = slot.row
    if r == nil then return end
    local op = slot.enabled and 1.0 or 0.45

    -- local (card top-left origin) → screen, with the hover scale baked in
    local function lx(x) return cx + (x - CARD_W / 2) * s end
    local function ly(y) return cy + (y - CARD_H / 2) * s end

    -- rank / score / player / date (left column)
    ui:drawText(22, "#" .. (slot.rank or 0), lx(PAD), ly(12), COL_ACCENT, op, s)
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
    for i = 1, ROWS do
        local slot = ui:button{
            text = " ", w = CARD_W, h = CARD_H, x = CARD_X, y = LIST_TOP,
            onClick = function(self)
                if self.row ~= nil and self.row.Watchable then
                    G.replayConfirm = {
                        filepath = self.row.FilePath, diff = self.diff,
                        header   = self.row, rank = self.rank,
                    }
                end
            end,
        }
        slot.focusable = false            -- mouse-only; never take keyboard focus
        slot.drawContent = function(self, cx, cy, sc) drawCardContent(self, cx, cy, sc) end
        cards[i] = slot
    end
end

local function hideAll()
    for _, c in ipairs(cards) do c:setVisible(false); c:setHover(false) end
end

-- ── Per-frame update (mouse only) ─────────────────────────────────────────────────
-- returns "play" (start the replay), "consume" (took input; skip difficulty nav), or nil.
function M.handleUpdate(ts)
    -- confirm prompt takes over input until answered
    if G.replayConfirm ~= nil then
        if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
            local c = G.replayConfirm
            G.replayConfirm = nil
            if REPLAY:Watch(c.filepath) then
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

    local chart, diff = hoveredChart()
    if chart == nil then if ui then hideAll() end return nil end
    refresh(chart, diff)
    ensureUI()

    local n = (G.replayList and G.replayList.Length) or 0
    local maxScroll = math.max(0, n - ROWS)
    local _, dy = INPUT:GetScrollDelta()
    if dy ~= 0 then G.replayScroll = math.max(0, math.min(maxScroll, G.replayScroll - dy)) end
    if G.replayScroll > maxScroll then G.replayScroll = maxScroll end

    -- assign data + position the pool
    for i = 1, ROWS do
        local slot = cards[i]
        local idx  = G.replayScroll + (i - 1)
        if idx < n then
            slot.row  = G.replayList[idx]
            slot.rank = idx + 1
            slot.diff = diff
            slot.x    = CARD_X
            slot.y    = LIST_TOP + (i - 1) * (CARD_H + CARD_GAP)
            slot:setVisible(true)
            slot:setEnabled(slot.row.Watchable)
        else
            slot.row = nil
            slot:setVisible(false)
        end
    end

    -- manual mouse hover/press (so the keyboard stays free for the difficulty bars)
    local mx, my  = INPUT:GetMouseXY()
    local inside  = INPUT:IsMouseInside()
    local hoverW  = nil
    if inside then
        for i = ROWS, 1, -1 do
            local slot = cards[i]
            if slot.visible and slot.enabled and slot:hitTest(mx, my) then hoverW = slot; break end
        end
    end
    if hoverW ~= M._hoverW then
        if M._hoverW then M._hoverW:setHover(false) end
        if hoverW then hoverW:setHover(true); G.sounds.Skip:Play() end
        M._hoverW = hoverW
    end
    if INPUT:MousePressed("Left") and hoverW then hoverW:press(); M._pressW = hoverW end
    if INPUT:MouseReleased("Left") and M._pressW then
        M._pressW:release(M._pressW:hitTest(mx, my)); M._pressW = nil
    end

    for i = 1, ROWS do if cards[i].visible then cards[i]:update(nil) end end
    return nil
end

-- ── Draw (called from diffselect.drawPanel; only when the panel is settled) ─────────
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

    -- header
    ui:drawText(28, "Best Plays", CARD_X, HEADER_Y, COL_ACCENT, 1)
    if n == 0 then
        ui:drawText(18, "No replays yet", CARD_X, LIST_TOP, COL_SUB, 1)
    else
        for i = 1, ROWS do if cards[i].visible then cards[i]:draw() end end
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

            local line1 = "#" .. (c.rank or 0) .. "    " .. commas(h.Score) .. " pts"
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

-- clear the confirm prompt (called when leaving difficulty select)
function M.reset()
    G.replayConfirm = nil
    if M._hoverW then M._hoverW:setHover(false); M._hoverW = nil end
    M._pressW = nil
end

-- free baked GPU canvases (called from the activity's onDestroy)
function M.dispose()
    if ui ~= nil then ui:disposeWidgets() end
    if dimCanvas ~= nil then dimCanvas:Dispose(); dimCanvas = nil end
    ui = nil; cards = {}
end

return M
