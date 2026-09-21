---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- replaylist.lua  —  Best-plays panel for single-player regular (performance) song select.
--
-- The top replays of the hovered chart + difficulty run down the right edge of the difficulty screen as
-- blocks styled after the result screen's panel: a white rounded face with the thick pink outline, the
-- pink checker as a column on the left and a crenellated band along the bottom. The column holds the
-- rank (gold, silver and bronze digit sheets for the first three places, shining like the level stars;
-- smaller plain white digits after), the result screen's score-rank badge and clear crown (none on a
-- fail); the right side holds the score in the result screen's digits and spacing (room for seven), the
-- player and date, and a stats sub-panel listing the
-- judgements in the game's order (Good!, OK, Bad, then Ad-Lib and BOOM!! on one row) with the in-game
-- judge words and right-aligned counts; the bottom band carries the play's mod icons and a badge when
-- the replay cannot be watched or comes with caveats. The keyboard keeps driving the difficulty bars;
-- the blocks answer to the mouse only: hover tints a block pink (nothing moves, so the viewport's edges
-- never cut a lifted block), a click opens a PopUI confirm, confirming arms the replay (REPLAY:Watch) and
-- mounts the chart, and song_select_core returns "play".
--
-- Scrolling is smooth (eased pixel offset, wheel + draggable scrollbar). Blocks past the strip's top or
-- bottom are cut at the viewport: the baked art through source rectangles, the text through the glyph
-- clip band, and the sheet-drawn parts (digits, badges, judge words, icons) only once their rows are
-- inside. Lists load asynchronously (REPLAY:ListReplaysAsync): every difficulty prefetches when the
-- difficulty screen opens, and a loading line shows until the hovered list lands.

local PopUI = require("PopUI")
local Shape = require("PopUI.shape")

local M = {}
local G            -- shared state injected by Script.lua

-- ── Layout ──────────────────────────────────────────────────────────────────────
local STRIP_X   = 1456
local STRIP_W   = 464                            -- to the screen's right edge
local CARD_W    = 440
local CARD_H    = 236
local MARGIN    = 10                             -- room around the block for its shadow
local CANVAS_W, CANVAS_H = CARD_W + 2 * MARGIN, CARD_H + 2 * MARGIN
local CARD_GAP  = 14
local STEP      = CARD_H + CARD_GAP
local CARD_X    = STRIP_X + 4
local LIST_TOP  = 262
local HEADER_Y  = 206
local VISIBLE   = 3
local VIEW_H    = VISIBLE * STEP - CARD_GAP
local VIEW_BOT  = LIST_TOP + VIEW_H
local SB_X, SB_W = STRIP_X + STRIP_W - 12, 6     -- 8 px clear of the blocks' right edge
local RADIUS    = 20
local OUTLINE_W = 4
local CELL      = 12                             -- the checker's square
-- the checker column and the bottom band (block-local)
local COL_X, COL_W, COL_BOT = OUTLINE_W, 108, 196
local FOOT_Y = 196
-- the right side
local RX, RR = 124, 428
local SCORE_SCALE = 0.48                         -- Score_Number.png digits (70x84 cells)
local SCORE_ADV   = 48                           -- the result screen's digit interval (Result_Score_Number_Interval)
local STATS = { x = RX, y = 78, w = RR - RX, h = 114, rowH = 27, pad = 8 }
local STATS_LEFT  = STATS.x + STATS.pad
local STATS_RIGHT = STATS.x + STATS.w - STATS.pad
local HALF_L_R, HALF_R_L = STATS_LEFT + 136, STATS_LEFT + 152   -- the split of the last row
local MODS_X, MODS_Y = 16, 195
local FLAG_X, FLAG_Y = 418, 214
-- the column: the rank's ink centre, then the badges' centres
local RANK_CX, RANK_CY = COL_X + COL_W / 2, 40
local BADGE_CY, CROWN_CY = 108, 170
local BADGE_SCALE = 0.38                         -- ScoreRank.png frames (210x180)
local CROWN_SCALE = 0.33                         -- CrownEffect.png frames (170x168)

-- ── Palette (the result screen's panel) ─────────────────────────────────────────
local RGB = {
    ink = { 60, 44, 56 }, sub = { 150, 118, 134 }, white = { 255, 255, 255 },
    pink = { 255, 97, 137 }, pinkMid = { 255, 109, 142 }, pinkDeep = { 212, 0, 111 },
    checkA = { 255, 231, 231 }, checkB = { 255, 211, 210 }, rule = { 255, 211, 210 },
    hover = { 255, 131, 160 }, tipText = { 255, 244, 246 }, tipFace = { 60, 28, 44 },
    warn = { 232, 172, 40 }, err = { 214, 72, 72 }, sbTrack = { 120, 70, 90 }, sbThumb = { 255, 131, 160 },
}
-- the medal shine, the level stars' recipe: an additive pulse of the digit itself and two glints that blink
-- in turn (star_glint.png, shared with the difficulty bars) at fixed spots around the digit
local SHINE_MS, SHINE_OP = 2700, 0.45           -- the digit's flash, three times slower than the stars'
local GLINT_MS, GLINT_OP = 1500, 0.95           -- the glints at the stars' pace
local GLINT_SPOTS = { { 30, -34 }, { -28, 24 } }   -- ink-centre relative, in the sheet's pixels (scaled with the tier)
-- what C# reports for an unwatchable replay (ReplayHeader.UnwatchableReason) -> the skin's wording
local REASONS = {
    rng_mods             = { "SONGSELECT_REPLAY_REASON_RNG",          "Uses random mods that cannot be replayed" },
    dynamic_beat_version = { "SONGSELECT_REPLAY_REASON_DYNAMIC_BEAT", "Dynamic Beat replay from a different game version" },
    unseeded_shuffle     = { "SONGSELECT_REPLAY_REASON_UNSEEDED",     "Recorded before the note shuffle was seeded" },
}
-- the rank digit sheets (Textures/Replay/rank_<tier>.png, 96x120 cells, ink centred on (48, 69), advance
-- 70): gold, silver and bronze hold their one digit only; white holds 0-9, drawn smaller
local RANK_TIERS = {
    { sheet = "gold",   scale = 0.70, medal = true },
    { sheet = "silver", scale = 0.62, medal = true },
    { sheet = "bronze", scale = 0.55, medal = true },
    { sheet = "white",  scale = 0.42, medal = false },
}
local RANK_CELL_W, RANK_CELL_H, RANK_ADV, RANK_INK_CX, RANK_INK_CY = 96, 120, 70, 48, 69
local RANK_INK_UP, RANK_INK_DOWN = 44, 45          -- the ink's extent above and below its centre
-- Judge.png frames (150x100 each) cropped to their ink, in the order the stats list shows them
local JUDGES = {
    { frame = 0, field = "Good",  sx = 1,  sy = 43, sw = 149, sh = 50, scale = 0.30 },
    { frame = 1, field = "Ok",    sx = 39, sy = 41, sw = 66,  sh = 49, scale = 0.30 },
    { frame = 2, field = "Bad",   sx = 25, sy = 42, sw = 94,  sh = 43, scale = 0.30 },
    { frame = 3, field = "ADLib", sx = 7,  sy = 31, sw = 126, sh = 42, scale = 0.30 },
    { frame = 4, field = "Boom",  sx = 16, sy = 2,  sw = 109, sh = 87, scale = 0.26 },
}
local UI_THEME = {
    colors = {
        surface  = { 255, 255, 255, 255 }, surface2 = { 255, 231, 231, 255 },
        primary  = { 255, 109, 142, 255 }, primary2 = { 232, 86, 122, 255 },
        accent   = { 212, 0, 111, 255 },   accent2  = { 180, 0, 94, 255 },
        outline  = { 255, 97, 137, 255 },  text = { 60, 44, 56, 255 }, textOnAccent = { 255, 255, 255, 255 },
    },
    font = { small = 18, label = 22, button = 24, title = 30 },
}

local function clamp(v, lo, hi) if v < lo then return lo elseif v > hi then return hi else return v end end
local function tierOf(rank) return RANK_TIERS[rank] or RANK_TIERS[4] end

local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

-- ── Drawing helpers (fonts and colours cached; nothing allocates per frame) ─────
local fonts, colors = {}, {}
local function font(size)
    local f = fonts[size]
    if f == nil then f = TEXT:CreateGlyphCached(size); fonts[size] = f end
    return f
end
local function color(c, a)
    local key = c[1] * 65536 + c[2] * 256 + c[3] + (a or 255) * 16777216
    local col = colors[key]
    if col == nil then col = COLOR:CreateColorFromRGBA(c[1], c[2], c[3], a or 255); colors[key] = col end
    return col
end
local NO_OUTLINE

-- text by its ink: the glyph box carries padding (box height - line height) that the box is offset by
local function inkPad(size) local f = font(size); return f.BoxHeight - math.ceil(f.LineHeight) end
local function text(size, str, x, y, rgb, opacity, anchor, maxWidth, outline)
    local f = font(size)
    local pad = inkPad(size)
    anchor = anchor or "left"
    if anchor == "right" then x = x + pad elseif anchor == "left" then x = x - pad end
    local a = (anchor == "center") and "top" or (anchor == "right" and "topright" or "topleft")
    f:Draw(tostring(str), x, y, color(rgb), outline or NO_OUTLINE, opacity or 1, 1, maxWidth or 0, a)
end
local function textWidth(size, str) return font(size):Measure(tostring(str)) end
local TEXT_SIZES = { 14, 17, 18 }
local function clipText(on)
    for _, size in ipairs(TEXT_SIZES) do
        if on then font(size):SetClipY(LIST_TOP, VIEW_BOT) else font(size):SetClipY(0, 0) end
    end
end

local whiteCv = nil
local function rect(x, y, w, h, rgb, a)
    if whiteCv == nil then
        whiteCv = CANVAS:CreateCanvas(2, 2); whiteCv:Clear(255, 255, 255, 255); whiteCv:Upload()
    end
    whiteCv:SetColor(rgb[1] / 255, rgb[2] / 255, rgb[3] / 255)
    whiteCv:SetOpacity((a or 255) / 255)
    whiteCv:SetScale(w / 2, h / 2)
    whiteCv:DrawAtAnchor(math.floor(x), math.floor(y), "topleft")
    whiteCv:SetScale(1, 1); whiteCv:SetOpacity(1); whiteCv:SetColor(1, 1, 1)
end

-- ── Baked art ───────────────────────────────────────────────────────────────────
local art = nil          -- { face, hover, warn, err }
local sheets = nil       -- { digits, rank, crown, judge, ranks = { gold, silver, bronze, white } }

-- the horizontal inset of the face's inner rounded rectangle at block-local row py
local INNER_R = RADIUS - OUTLINE_W
local function innerInset(py)
    local edge = math.min(py - OUTLINE_W, (OUTLINE_W + CARD_H - 2 * OUTLINE_W - 1) - py)
    if edge < 0 then return CARD_W end
    if edge >= INNER_R then return 0 end
    local k = INNER_R - edge
    return INNER_R - math.sqrt(math.max(0, INNER_R * INNER_R - k * k))
end

-- one checker cell (grid i, j from the inner corner) clipped to the face's rounded inside; returns nothing
local function checkerCell(cv, i, j)
    local c = ((i + j) % 2 == 0) and RGB.checkB or RGB.checkA
    local cx0, cy0 = OUTLINE_W + i * CELL, OUTLINE_W + j * CELL
    for py = cy0, math.min(cy0 + CELL - 1, CARD_H - OUTLINE_W - 1) do
        local inset = math.floor(innerInset(py))
        local x0, x1 = math.max(cx0, OUTLINE_W + inset), math.min(cx0 + CELL, CARD_W - OUTLINE_W - inset)
        if x1 > x0 then cv:FillRect(MARGIN + x0, MARGIN + py, x1 - x0, 1, c[1], c[2], c[3], 255) end
    end
end

local function bakeFace()
    local cv = CANVAS:CreateCanvas(CANVAS_W, CANVAS_H)
    cv:ClearTransparent()
    local x, y = MARGIN, MARGIN
    Shape.dropShadow(cv, x, y, CARD_W, CARD_H, RADIUS, { col = { 60, 20, 40, 48 }, layers = 2, grow = 2, dx = 0, dy = 3 })
    Shape.panel(cv, x, y, CARD_W, CARD_H, { radius = RADIUS, outline = { col = { RGB.pink[1], RGB.pink[2], RGB.pink[3], 255 }, width = OUTLINE_W },
                                            top = { 255, 255, 255, 255 } })
    local cols = math.floor((CARD_W - 2 * OUTLINE_W) / CELL)
    local colCells, colRows = math.floor(COL_W / CELL), math.floor((COL_BOT - OUTLINE_W) / CELL)
    local footRow0 = math.floor((FOOT_Y - OUTLINE_W) / CELL)
    local footRows = math.ceil((CARD_H - OUTLINE_W - FOOT_Y) / CELL)
    for j = 0, colRows - 1 do
        for i = 0, colCells - 1 do checkerCell(cv, i, j) end
    end
    for j = footRow0, footRow0 + footRows - 1 do
        for i = 0, cols - 1 do checkerCell(cv, i, j) end
    end
    for i = colCells, cols - 1 do
        if (i + footRow0 - 1) % 2 == 0 then checkerCell(cv, i, footRow0 - 1) end
    end
    -- the stats sub-panel
    Shape.panel(cv, x + STATS.x, y + STATS.y, STATS.w, STATS.h, { radius = 8, outline = { col = { RGB.pinkDeep[1], RGB.pinkDeep[2], RGB.pinkDeep[3], 255 }, width = 2 },
                                                                  top = { 255, 255, 255, 255 } })
    for row = 0, 3 do
        local ry = y + STATS.y + 4 + row * STATS.rowH + STATS.rowH - 2
        if row < 3 then
            cv:FillRect(x + STATS_LEFT, ry, STATS_RIGHT - STATS_LEFT, 2, RGB.rule[1], RGB.rule[2], RGB.rule[3], 255)
        else
            cv:FillRect(x + STATS_LEFT, ry, HALF_L_R - STATS_LEFT, 2, RGB.rule[1], RGB.rule[2], RGB.rule[3], 255)
            cv:FillRect(x + HALF_R_L, ry, STATS_RIGHT - HALF_R_L, 2, RGB.rule[1], RGB.rule[2], RGB.rule[3], 255)
        end
    end
    cv:Upload()
    return cv
end

-- the hover tint: the face's shape filled with translucent pink, drawn over the block (twice on press)
local function bakeHover()
    local cv = CANVAS:CreateCanvas(CANVAS_W, CANVAS_H)
    cv:ClearTransparent()
    Shape.panel(cv, MARGIN, MARGIN, CARD_W, CARD_H, { radius = RADIUS, top = { RGB.hover[1], RGB.hover[2], RGB.hover[3], 52 } })
    cv:Upload()
    return cv
end

-- warning triangle (amber, dark outline, "!") at 2x, drawn at half scale so the edges smooth out
local function bakeWarnTriangle()
    local W, H = 64, 56
    local c = CANVAS:CreateCanvas(W, H)
    c:Clear(0, 0, 0, 0)
    local cx = W / 2
    for y = 0, H - 1 do
        local half = ((y + 1) / H) * (W / 2 - 1)
        c:FillRect(math.floor(cx - half), y, math.max(1, math.floor(half * 2 + 0.5)), 1, 146, 96, 10, 255)
    end
    for y = 6, H - 4 do
        local half = ((y - 5) / (H - 9)) * (W / 2 - 5)
        c:FillRect(math.floor(cx - half), y, math.max(1, math.floor(half * 2 + 0.5)), 1, 246, 189, 55, 255)
    end
    c:FillRect(math.floor(cx - 3), 20, 6, 18, 84, 54, 4, 255)
    c:FillRect(math.floor(cx - 2), 18, 4, 2, 84, 54, 4, 255)
    c:FillRect(math.floor(cx - 3), 43, 6, 6, 84, 54, 4, 255)
    c:Upload()
    return c
end

-- red error circle with a white X (2x, drawn at half scale) for unwatchable replays
local function bakeErrCircle()
    local D = 56
    local c = CANVAS:CreateCanvas(D, D)
    c:Clear(0, 0, 0, 0)
    local r, cx = D / 2, D / 2
    for y = 0, D - 1 do
        local dy = y + 0.5 - cx
        local half = math.sqrt(math.max(0, r * r - dy * dy))
        if half >= 1 then c:FillRect(math.floor(cx - half), y, math.max(1, math.floor(half * 2 + 0.5)), 1, 152, 36, 36, 255) end
    end
    for y = 3, D - 4 do
        local dy = y + 0.5 - cx
        local ri = r - 3
        local half = math.sqrt(math.max(0, ri * ri - dy * dy))
        if half >= 1 then c:FillRect(math.floor(cx - half), y, math.max(1, math.floor(half * 2 + 0.5)), 1, 206, 58, 58, 255) end
    end
    for t = -13, 13 do
        c:FillRect(math.floor(cx + t - 2), math.floor(cx + t), 5, 1, 255, 255, 255, 255)
        c:FillRect(math.floor(cx - t - 2), math.floor(cx + t), 5, 1, 255, 255, 255, 255)
    end
    c:Upload()
    return c
end

local function ensureArt()
    if art ~= nil then return end
    NO_OUTLINE = COLOR:CreateColorFromRGBA(0, 0, 0, 0)
    art = { face = bakeFace(), hover = bakeHover(), warn = bakeWarnTriangle(), err = bakeErrCircle() }
    local gfx = "../../../Graphics/"
    sheets = {
        digits = TEXTURE:CreateTexture(gfx .. "6_Result/Score_Number.png"),   -- 10 digits, 70x84
        rank   = TEXTURE:CreateTexture(gfx .. "5_Game/ScoreRank.png"),         -- 7 frames E..Ω, 210x180
        crown  = TEXTURE:CreateTexture(gfx .. "6_Result/CrownEffect.png"),     -- 4 columns x 4 rows, 170x168
        judge  = TEXTURE:CreateTexture(gfx .. "5_Game/Judge.png"),             -- 5 frames, 150x100
        ranks  = {},
    }
    for _, t in ipairs(RANK_TIERS) do
        if sheets.ranks[t.sheet] == nil then sheets.ranks[t.sheet] = TEXTURE:CreateTexture("Textures/Replay/rank_" .. t.sheet .. ".png") end
    end
end

-- ── Init ──────────────────────────────────────────────────────────────────────
function M.init(g)
    G = g
    G.replayList    = nil   -- best plays for the hovered chart+difficulty (a C# ReplayHeader[])
    G.replayListKey = nil   -- uniqueId/diff the list was fetched for
    G.replayLoading = false -- the hovered list's async fetch is still in flight
    G.replayScroll  = 0      -- current scroll offset in PIXELS (eased toward M._scrollTarget)
    G.replayConfirm = nil    -- { filepath, diff, header, rank, chartPath } while the confirm is up
    M._scrollTarget = 0
    M._sbDrag       = false
    M._lastTs       = nil
    M._lists        = {}     -- key → ReplayHeader[] (fetched)
    M._pending      = {}     -- key → async handle (fetch in flight)
    M._loadAnim     = 0
    M._hover        = nil    -- index (0-based) of the hovered block
    M._press        = nil    -- index of the block the mouse went down on
    M._confirmUI    = nil    -- the PopUI popup while the confirm is up
    M._confirmChoice = nil
    M._now          = 0
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
    return CONFIG.PlayerCount == 1 and M.isRegular() and G.themeFlag("songselect_replays")
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
        M._hover, M._press = nil, nil
    end
end

-- ── Block content ───────────────────────────────────────────────────────────────
local function rowVisible(y0, y1) return y0 >= LIST_TOP and y1 <= VIEW_BOT end
local function ready(tex) return tex ~= nil and tex.Width > 0 end

-- a number in the result screen's digits (Score_Number.png, 70x84 cells set SCORE_ADV apart, as the result
-- screen spaces them) with the last cell's right edge at right and the top at y
local function drawScore(n, right, y, scale, op)
    local tex = sheets.digits
    if not ready(tex) then return end
    local s = tostring(math.tointeger(n) or n)
    local adv = SCORE_ADV * scale
    local x0 = right - 70 * scale - (#s - 1) * adv
    tex:SetScale(scale, scale); tex:SetOpacity(op)
    for i = 1, #s do
        local d = tonumber(s:sub(i, i))
        if d ~= nil then tex:DrawRectAtAnchor(math.floor(x0 + (i - 1) * adv), math.floor(y), d * 70, 0, 70, 84, "topleft") end
    end
    tex:SetScale(1, 1); tex:SetOpacity(1)
end

-- the cells of a rank's digits, ink centred on (cx, cy), drawn with the sheet's current state; a medal
-- sheet holds its single digit in cell 0, the white sheet indexes by digit
local function drawRankCells(tex, s, sc, cx, cy, medal)
    local adv = RANK_ADV * sc
    local x0 = cx - #s * adv / 2
    for i = 1, #s do
        local d = tonumber(s:sub(i, i))
        if d ~= nil then
            local inkCx = x0 + (i - 0.5) * adv
            tex:DrawRectAtAnchor(math.floor(inkCx - RANK_INK_CX * sc + 0.5), math.floor(cy - RANK_INK_CY * sc + 0.5),
                                 medal and 0 or d * RANK_CELL_W, 0, RANK_CELL_W, RANK_CELL_H, "topleft")
        end
    end
end

-- the rank in its tier's digit sheet, the ink centred on (cx, cy); the medal tiers shine the way the level
-- stars do: the digit pulsing additively over itself, and glints blinking in turn beside it
local function drawRank(rank, cx, cy, op)
    local tier = tierOf(rank)
    local tex = sheets.ranks[tier.sheet]
    if not ready(tex) then return end
    local s, sc = tostring(rank), tier.scale
    tex:SetScale(sc, sc); tex:SetOpacity(op)
    drawRankCells(tex, s, sc, cx, cy, tier.medal)
    if tier.medal then
        local t = M._now
        local pulse = 0.5 + 0.5 * math.sin(2 * math.pi * t / SHINE_MS + rank * 1.3)
        tex:SetBlendMode("add"); tex:SetOpacity(op * SHINE_OP * pulse)
        drawRankCells(tex, s, sc, cx, cy, true)
        tex:SetBlendMode("normal")
        local glint = G.bgtx and G.bgtx["diffsel_star_glint"]
        if glint ~= nil then
            glint:SetBlendMode("add")
            for k, spot in ipairs(GLINT_SPOTS) do
                local gp = (t / GLINT_MS + rank * 0.37 + k * 0.5) % 1
                local blink = math.sin(gp * math.pi)
                blink = blink * blink * blink
                if blink > 0.02 then
                    local gs = (1.0 + 1.0 * blink) * (sc / 0.7)
                    glint:SetRotation(gp * 90); glint:SetScale(gs, gs); glint:SetOpacity(op * GLINT_OP * blink)
                    glint:DrawAtAnchor(math.floor(cx + spot[1] * sc + 0.5), math.floor(cy + spot[2] * sc + 0.5), "center")
                end
            end
            glint:SetBlendMode("normal"); glint:SetRotation(0); glint:SetScale(1, 1); glint:SetOpacity(1)
        end
    end
    tex:SetScale(1, 1); tex:SetOpacity(1)
end

local function drawSheetFrame(tex, cx, cy, sx, sy, sw, sh, scale, op)
    if not ready(tex) then return end
    tex:SetScale(scale, scale); tex:SetOpacity(op)
    tex:DrawRectAtAnchor(math.floor(cx), math.floor(cy), sx, sy, sw, sh, "center")
    tex:SetScale(1, 1); tex:SetOpacity(1)
end

-- the score-rank badge (sr: 0 = none, else the E..Ω frame sr-1) centred on (cx, cy)
local function drawRankBadge(r, cx, cy, op)
    if r.ScoreRank and r.ScoreRank > 0 then
        drawSheetFrame(sheets.rank, cx, cy, 0, (r.ScoreRank - 1) * 180, 210, 180, BADGE_SCALE, op)
    end
end

-- the clear crown (cs: the Assisted / Clear / Full Combo / Perfect column cs-1) centred on (cx, cy);
-- a failed play shows none
local function drawCrown(r, cx, cy, op)
    if r.ClearStatus and r.ClearStatus > 0 then
        drawSheetFrame(sheets.crown, cx, cy, (r.ClearStatus - 1) * 170, 0, 170, 168, CROWN_SCALE, op)
    end
end

-- a judge word (one Judge.png frame cropped to its ink) with its left edge at x, centred on cy
local function drawJudgeWord(j, x, cy, op)
    local tex = sheets.judge
    if not ready(tex) then return end
    tex:SetScale(j.scale, j.scale); tex:SetOpacity(op)
    tex:DrawRectAtAnchor(math.floor(x), math.floor(cy), j.sx, j.frame * 100 + j.sy, j.sw, j.sh, "left")
    tex:SetScale(1, 1); tex:SetOpacity(1)
end

-- the y of a block for list index idx (0-based) at the current scroll
local function cardTop(idx) return LIST_TOP + idx * STEP - G.replayScroll end

local function contentMetrics(n)
    local contentH = math.max(0, n * STEP - CARD_GAP)
    return contentH, math.max(0, contentH - VIEW_H)
end

-- draw one block whose face top-left is (x, y); the canvas has MARGIN around it. state: "plain",
-- "hover" or "press".
local function drawCard(r, rank, x, y, state)
    local op = r.Watchable and 1.0 or 0.45
    local cx0, cy0 = x - MARGIN, y - MARGIN                     -- the canvas' top-left
    local top, bot = math.max(cy0, LIST_TOP), math.min(cy0 + CANVAS_H, VIEW_BOT)
    if bot <= top then return end
    local src = { 0, math.floor(top - cy0), CANVAS_W, math.floor(bot - top) }
    art.face:SetOpacity(op)
    art.face:DrawRect(cx0, top, src[1], src[2], src[3], src[4])
    art.face:SetOpacity(1)
    -- the hover tint over the face, doubled while pressed
    if state ~= "plain" then
        art.hover:SetOpacity(op)
        art.hover:DrawRect(cx0, top, src[1], src[2], src[3], src[4])
        if state == "press" then art.hover:DrawRect(cx0, top, src[1], src[2], src[3], src[4]) end
        art.hover:SetOpacity(1)
    end

    -- the column: rank, then the result screen's badges
    local tier = tierOf(rank)
    local rcx, rcy = x + RANK_CX, y + RANK_CY
    if rowVisible(rcy - RANK_INK_UP * tier.scale, rcy + RANK_INK_DOWN * tier.scale) then
        drawRank(rank, rcx, rcy, op)
    end
    local badgeH, crownH = 180 * BADGE_SCALE, 168 * CROWN_SCALE
    if rowVisible(y + BADGE_CY - badgeH / 2, y + BADGE_CY + badgeH / 2) then drawRankBadge(r, rcx, y + BADGE_CY, op) end
    if rowVisible(y + CROWN_CY - crownH / 2, y + CROWN_CY + crownH / 2) then drawCrown(r, rcx, y + CROWN_CY, op) end

    -- the score row: the caption and the digits, right-aligned so seven fit
    if rowVisible(y + 8, y + 8 + 84 * SCORE_SCALE) then drawScore(r.Score, x + RR, y + 8, SCORE_SCALE, op) end
    clipText(true)
    text(14, tr("SONGSELECT_REPLAY_SCORE", "score"), x + RX, y + 16, RGB.pinkMid, op)
    text(18, r.PlayerName, x + RX, y + 50, RGB.ink, op, "left", 176)
    text(14, r.Date, x + RR, y + 54, RGB.sub, op, "right")
    -- the stats: three full rows then the Ad-Lib / BOOM!! row
    local function rowCy(i) return y + STATS.y + 4 + i * STATS.rowH + STATS.rowH / 2 - 1 end
    for i = 1, 3 do
        text(17, tostring(r[JUDGES[i].field] or 0), x + STATS_RIGHT, rowCy(i - 1) - 12, RGB.ink, op, "right")
    end
    text(17, tostring(r.ADLib or 0), x + HALF_L_R, rowCy(3) - 12, RGB.ink, op, "right")
    text(17, tostring(r.Boom or 0), x + STATS_RIGHT, rowCy(3) - 12, RGB.ink, op, "right")
    clipText(false)
    for i = 1, 3 do
        local cy = rowCy(i - 1)
        if rowVisible(cy - 13, cy + 13) then drawJudgeWord(JUDGES[i], x + STATS_LEFT, cy, op) end
    end
    do
        local cy = rowCy(3)
        if rowVisible(cy - 13, cy + 13) then
            drawJudgeWord(JUDGES[4], x + STATS_LEFT, cy, op)
            drawJudgeWord(JUDGES[5], x + HALF_R_L, cy, op)
        end
    end
    -- the bottom band: the play's mod icons, and the flag badge on the right
    if rowVisible(y + MODS_Y, y + MODS_Y + 37) and G.modicons_ro ~= nil then
        G.modicons_ro:Call("drawFlags", math.floor(x + MODS_X), math.floor(y + MODS_Y),
            r.ModFlags, r.ScrollSpeed, r.SongSpeed, r.JudgeStrictness, "menu", op * 255)
    end
    local bx, by = x + FLAG_X, y + FLAG_Y
    if rowVisible(by - 14, by + 14) then
        if not r.Watchable then
            art.err:SetScale(0.5, 0.5); art.err:DrawAtAnchor(math.floor(bx), math.floor(by), "center"); art.err:SetScale(1, 1)
        elseif r.OldVersion or r.ChecksumMismatch then
            art.warn:SetScale(0.5, 0.5); art.warn:SetOpacity(op)
            art.warn:DrawAtAnchor(math.floor(bx), math.floor(by), "center")
            art.warn:SetScale(1, 1); art.warn:SetOpacity(1)
        end
    end
end

-- ── The confirm popup (PopUI) ────────────────────────────────────────────────────
local CONFIRM_W, CONFIRM_H = 720, 440
local function openConfirm(row, rank, diff)
    G.replayConfirm = { filepath = row.FilePath, diff = diff, header = row, rank = rank, chartPath = G.replayChartPath }
    M._confirmChoice = nil
    local ui = PopUI.new{
        theme = UI_THEME, bg = false, navPlayer = 1,
        sfx = { click = function() G.sounds.Decide:Play() end, hover = function() G.sounds.Skip:Play() end },
    }
    local px, py = math.floor((1920 - CONFIRM_W) / 2), math.floor((1080 - CONFIRM_H) / 2)
    ui:panel{ x = px, y = py, w = CONFIRM_W, h = CONFIRM_H, title = tr("SONGSELECT_REPLAY_CONFIRM", "Watch this replay?") }
    local watch = ui:button{ text = tr("SONGSELECT_REPLAY_WATCH", "Watch"), x = px + CONFIRM_W / 2 - 250, y = py + CONFIRM_H - 92, w = 230, h = 62, accent = true,
                             onClick = function() M._confirmChoice = "watch" end }
    ui:button{ text = tr("SONGSELECT_SEARCH_CANCEL", "Cancel"), x = px + CONFIRM_W / 2 + 20, y = py + CONFIRM_H - 92, w = 230, h = 62,
               onClick = function() M._confirmChoice = "cancel" end }
    for i, w in ipairs(ui.focusables) do if w == watch then ui.focusIdx = i end end
    M._confirmUI = ui
    M._confirmJustOpened = true
end

local function closeConfirm()
    G.replayConfirm = nil
    if M._confirmUI ~= nil then M._confirmUI:disposeWidgets(); M._confirmUI = nil end
    M._confirmChoice = nil
end

-- ── Update ──────────────────────────────────────────────────────────────────────
function M.handleUpdate(ts)
    M._now = ts or M._now
    -- the confirm takes the input until answered
    if G.replayConfirm ~= nil then
        -- the release that opened it never reaches the popup (a click on Watch on the same frame)
        if M._confirmJustOpened then M._confirmJustOpened = false; return "consume" end
        local r = nil
        if M._confirmUI ~= nil then r = M._confirmUI:update(ts) end
        if M._confirmChoice == "watch" then
            local c = G.replayConfirm
            closeConfirm()
            if REPLAY:Watch(c.filepath, c.chartPath) then
                G.selectedSongNode:Mount(c.diff, 0, 0, 0, 0)
                G.sounds.SongDecide:Play()
                G.lastSignal = "play"
                return "play"
            end
        elseif M._confirmChoice == "cancel" or r == "cancel" then
            G.sounds.Cancel:Play(); closeConfirm()
        end
        return "consume"
    end

    pollFetches()

    local chart, diff = hoveredChart()
    if chart == nil then M._hover, M._press = nil, nil; return nil end
    refresh(chart, diff)

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
    local overStrip = inside and mx >= STRIP_X and mx <= STRIP_X + STRIP_W and my >= LIST_TOP and my <= VIEW_BOT

    -- mouse wheel over the strip: one block per notch, eased below
    local _, wheel = INPUT:GetScrollDelta()
    if wheel ~= 0 and overStrip then M._scrollTarget = clamp(M._scrollTarget - wheel * STEP, 0, maxScroll) end

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

    -- hover: the block under the mouse inside the viewport (gaps are dead space)
    local hover = nil
    if inside and not M._sbDrag and overStrip and mx >= CARD_X and mx <= CARD_X + CARD_W and mx < SB_X - 8 then
        local ly = my - LIST_TOP + G.replayScroll
        local idx = math.floor(ly / STEP)
        if idx >= 0 and idx < n and ly - idx * STEP <= CARD_H then hover = idx end
    end
    if hover ~= M._hover then
        if hover ~= nil and G.replayList[hover].Watchable then G.sounds.Skip:Play() end
        M._hover = hover
    end
    -- press on a watchable block, release on the same block = open the confirm
    if INPUT:MousePressed("Left") and hover ~= nil and G.replayList[hover].Watchable then M._press = hover end
    if INPUT:MouseReleased("Left") then
        if M._press ~= nil and M._press == hover then
            G.sounds.Decide:Play()
            openConfirm(G.replayList[hover], hover + 1, diff)
        end
        M._press = nil
    end
    return nil
end

-- ── Draw (called from diffselect.drawPanel; only when the panel is settled) ─────────
local dimCv = nil
local function drawDim(opacity)
    if dimCv == nil then
        dimCv = CANVAS:CreateCanvas(2, 2); dimCv:Clear(0, 0, 0, 255); dimCv:Upload()
    end
    dimCv:SetScale(960, 540); dimCv:SetOpacity(opacity)
    dimCv:Draw(0, 0)
    dimCv:SetScale(1, 1); dimCv:SetOpacity(1)
end

function M.draw()
    if not active() or G.activeScreen ~= "difficultyselect" then return end
    local chart = hoveredChart()
    if chart == nil and G.replayConfirm == nil then return end
    ensureArt()
    local n = (G.replayList and G.replayList.Length) or 0

    -- the header in the panel's solid pink, with the count once the list holds anything; the empty and
    -- loading lines in the secondary grey
    local title = tr("SONGSELECT_REPLAY_TITLE", "Best Plays")
    if n > 0 then title = title .. " (" .. n .. ")" end
    text(28, title, CARD_X + 6, HEADER_Y, RGB.pinkDeep)
    if G.replayLoading then
        local dots = string.rep(".", 1 + math.floor((M._loadAnim or 0) * 3) % 3)
        text(18, tr("SONGSELECT_REPLAY_LOADING", "Loading replays") .. dots, CARD_X + 6, LIST_TOP, RGB.sub)
    elseif n == 0 then
        text(18, tr("SONGSELECT_REPLAY_NONE", "No replays yet"), CARD_X + 6, LIST_TOP, RGB.sub)
    else
        local first = math.max(0, math.floor(G.replayScroll / STEP))
        for idx = first, math.min(n - 1, first + VISIBLE + 1) do
            local y = cardTop(idx)
            if y - MARGIN < VIEW_BOT and y + CARD_H + MARGIN > LIST_TOP then
                local state = (M._press == idx) and "press" or ((M._hover == idx) and "hover" or "plain")
                drawCard(G.replayList[idx], idx + 1, CARD_X, math.floor(y + 0.5), state)
            end
        end
    end

    -- scrollbar (track + thumb) when the list overflows the viewport
    local contentH, maxScroll = contentMetrics(n)
    if maxScroll > 0 then
        local thumbH = math.max(28, VIEW_H * VIEW_H / contentH)
        local t = clamp(G.replayScroll / maxScroll, 0, 1)
        rect(SB_X, LIST_TOP, SB_W, VIEW_H, RGB.sbTrack, 70)
        rect(SB_X, LIST_TOP + t * (VIEW_H - thumbH), SB_W, thumbH, RGB.sbThumb, M._sbDrag and 255 or 220)
    end

    -- tooltip while hovering a flagged block: red accent + the reason for an unwatchable replay (its code
    -- from C# in the skin's wording), amber + the caveats for a watchable one
    local hov = M._hover ~= nil and G.replayList ~= nil and G.replayList[M._hover] or nil
    if G.replayConfirm == nil and hov ~= nil then
        local lines = {}
        local accent = RGB.warn
        if not hov.Watchable then
            local code = hov.UnwatchableReason or ""
            local known = REASONS[code]
            if known ~= nil then lines[1] = tr(known[1], known[2])
            elseif code ~= "" then lines[1] = code
            else lines[1] = tr("SONGSELECT_REPLAY_UNWATCHABLE", "This replay cannot be watched") end
            accent = RGB.err
        else
            if hov.OldVersion then lines[#lines + 1] = tr("SONGSELECT_REPLAY_OLD_VERSION", "Recorded on an older game version") end
            if hov.ChecksumMismatch then lines[#lines + 1] = tr("SONGSELECT_REPLAY_CHART_CHANGED", "The chart was modified after this play") end
        end
        if #lines > 0 then
            local w = 0
            for _, l in ipairs(lines) do w = math.max(w, textWidth(16, l)) end
            local pad, lineH = 14, 24
            local bw, bh = w + pad * 2 + 14, #lines * lineH + pad * 2 - 6
            local mx, my = INPUT:GetMouseXY()
            local bx = math.max(8, math.min(mx + 18, 1920 - bw - 8))
            local by = math.max(8, my - bh - 12)
            rect(bx, by, bw, bh, RGB.tipFace, 235)
            rect(bx, by, bw, 4, accent, 255)
            for i, l in ipairs(lines) do text(16, l, bx + pad, by + pad - 2 + (i - 1) * lineH, RGB.tipText) end
        end
    end

    -- the confirm popup: dim, the PopUI panel and buttons, then the play's rank, badges, score and lines
    if G.replayConfirm ~= nil and M._confirmUI ~= nil then
        local c, h = G.replayConfirm, G.replayConfirm.header
        drawDim(0.55)
        M._confirmUI:draw()
        local cx, py = 960, math.floor((1080 - CONFIRM_H) / 2)
        if h ~= nil then
            -- one row: rank, score-rank badge, crown, then the score right-aligned; the name and date under it
            local ry = py + 130
            drawRank(c.rank or 0, cx - 230, ry, 1)
            drawRankBadge(h, cx - 120, ry, 1)
            drawCrown(h, cx - 30, ry, 1)
            drawScore(h.Score, cx + 300, ry - 42 * 0.5, 0.5, 1)
            M._confirmUI:drawTextEx(22, h.PlayerName, cx, py + 200, UI_THEME.colors.text, { 255, 255, 255, 160 }, 1, 1, CONFIRM_W - 120, "top")
            M._confirmUI:drawTextEx(18, h.Date, cx, py + 236, { 150, 118, 134 }, { 255, 255, 255, 140 }, 1, 1, 0, "top")
        end
    end
end

-- clear the confirm and drop the cached list (called when leaving difficulty select and on activity
-- re-activation — i.e. also right after a play, so a freshly saved replay shows up without switching difficulty)
function M.reset()
    closeConfirm()
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
    M._hover, M._press = nil, nil
end

-- free the baked canvases, the sheets and the fonts (called from the activity's onDestroy)
function M.dispose()
    closeConfirm()
    if art ~= nil then
        for _, k in ipairs({ "face", "hover", "warn", "err" }) do art[k]:Dispose() end
        art = nil
    end
    if sheets ~= nil then
        for _, t in pairs(sheets.ranks) do t:Dispose() end
        sheets.ranks = nil
        for _, t in pairs(sheets) do t:Dispose() end
        sheets = nil
    end
    if dimCv ~= nil then dimCv:Dispose(); dimCv = nil end
    if whiteCv ~= nil then whiteCv:Dispose(); whiteCv = nil end
    for _, f in pairs(fonts) do f:Dispose() end
    fonts, colors = {}, {}
end

return M
