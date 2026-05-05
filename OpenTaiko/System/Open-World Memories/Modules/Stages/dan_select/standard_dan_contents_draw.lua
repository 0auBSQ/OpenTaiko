---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- standard_dan_contents_draw.lua
-- Draws the per-song detail bars (DanSong, ordinal stamp, difficulty stamp,
-- level, title, genre) and the exam panel (gauge + 2×3 grid) inside
-- Contents.png for the standard dan select.
--
-- Public API:
--   M.load()                                  — create textures/fonts (call from _load_resources)
--   M.unload()                                — dispose (call from _unload_resources)
--   M.update(dt, song_count)                  — advance scroll counter for ≥4-song dans
--   M.draw(abs_x, abs_y, dan_songs, dan_exams) — draw all bars + exam panel

local M = {}

-- ── Constants ─────────────────────────────────────────────────────────────────

local TX = "Textures/Contents/"

-- Clip window, relative to Contents.png top-left (y only)
local CLIP_Y1 = 33
local CLIP_Y2 = 334

-- Bar layout within Contents.png
local BAR_X       = 49
local BAR_Y_FIRST = 38
local BAR_GAP_Y   = 100

-- Positions within DanSong.png (relative to its own top-left)
local NB_REL_X      = 0    ; local NB_REL_Y      = 0
local NB_TXT_REL_X  = 46   ; local NB_TXT_REL_Y  = 49
local DIFF_REL_X    = 94   ; local DIFF_REL_Y     = 0
local LVL_REL_X     = 156  ; local LVL_REL_Y      = 74
local TITLE_REL_X   = 250  ; local TITLE_REL_Y    = 32  ; local TITLE_MAX_W    = 840
local SUB_REL_X     = 250  ; local SUB_REL_Y      = 72  ; local SUB_MAX_W      = 840

-- Scroll speed (pixels per second, bars move up)
local SCROLL_SPEED = 35.0

-- Gauge drawing (EXAM1), position relative to Contents.png, without anchor
local GAUGE_X = 421
local GAUGE_Y = 335

-- Exam grid top-left, relative to Contents.png, without anchor
local EXAM_GRID_X = 36
local EXAM_GRID_Y = 369

-- Positions within each DanExam bar (relative to its own top-left)
local EXAM_NAME_REL_X    = 0   -- ExamName.png
local EXAM_NAME_REL_Y    = 0
local EXAM_RANGE_REL_X   = 0   -- ExamRange.png
local EXAM_RANGE_REL_Y   = 63
local EXAM_TXT_NAME_CX   = 110 -- exam name text, center-x
local EXAM_TXT_NAME_CY   = 35  -- exam name text, center-y
local EXAM_TXT_RANGE_CX  = 110 -- exam range text, center-x
local EXAM_TXT_RANGE_CY  = 98  -- exam range text, center-y
local EXAM_TXT_MAX_W     = 220

-- Outline colours for exam elements (dark brown for name; red/blue for range)
local EXAM_NAME_OUTLINE   = {101,  67,  33}  -- dark brown (SaddleBrown-ish)
local EXAM_RANGE_MORE_COL = {139,   0,   0}  -- dark red
local EXAM_RANGE_LESS_COL = {  0,   0, 100}  -- dark blue

-- Range display strings (RangeAsInt: 0=More, 1=Less)
local EXAM_RANGE_STRINGS  = {"or More", "Less than"}

-- Ordinal stamp colours (1 = 1st … ≥10 all use Black)
local STAMP_COLORS = {
    {255,   0,   0},   -- 1st  Red
    {  0, 128,   0},   -- 2nd  Green
    {  0,   0, 255},   -- 3rd  Blue
    {255,   0, 255},   -- 4th  Magenta
    {255, 255,   0},   -- 5th  Yellow
    {  0, 255, 255},   -- 6th  Cyan
    {165,  42,  42},   -- 7th  Brown
    {128, 128, 128},   -- 8th  Gray
    {  0, 100,   0},   -- 9th  DarkGreen
    {  0,   0,   0},   -- 10th+ Black
}

-- Difficulty outline colours indexed by DifficultyAsInt (0–4)
local DIFF_OUTLINE = {
    [0] = {  0,   0, 100},   -- Easy    dark blue
    [1] = {  0, 100,   0},   -- Normal  dark green
    [2] = {150, 110,   0},   -- Hard    dark yellow
    [3] = {139,   0,   0},   -- Extreme dark red
    [4] = { 75,   0, 130},   -- Edit    dark purple
}

-- ── Module state ──────────────────────────────────────────────────────────────

local tx_dan_song  = nil
local tx_nb        = nil
local tx_diff      = {}   -- [0..4]

local tx_gauge     = nil  -- Gauge.png
local tx_exam_name = nil  -- ExamName.png overlay
local tx_exam_rng  = nil  -- ExamRange.png overlay
local tx_exam      = nil  -- DanExam.png (half-width)
local tx_exam_long = nil  -- DanExamLong.png (full-width)

local font_title      = nil  -- title text
local font_sub        = nil  -- subtitle / genre text
local font_nb         = nil  -- ordinal label
local font_level      = nil  -- star-level number
local font_exam_name  = nil  -- exam condition name
local font_exam_range = nil  -- exam range label

local scroll_y    = 0.0  -- current scroll pixel offset (≥4-song mode)

-- ── Helpers ───────────────────────────────────────────────────────────────────

local function ordinal(n)
    local mod100 = n % 100
    local suffix
    if mod100 >= 11 and mod100 <= 13 then
        suffix = "th"
    else
        local mod10 = n % 10
        if     mod10 == 1 then suffix = "st"
        elseif mod10 == 2 then suffix = "nd"
        elseif mod10 == 3 then suffix = "rd"
        else                    suffix = "th"
        end
    end
    return tostring(n) .. suffix
end

-- Returns the stamp RGB (1-based song index) and a darkened version for outline.
local function stampColors(idx)
    local c = STAMP_COLORS[math.min(idx, #STAMP_COLORS)]
    local r, g, b = c[1], c[2], c[3]
    return r, g, b,
           math.floor(r * 0.45), math.floor(g * 0.45), math.floor(b * 0.45)
end

-- Clip-draw a texture whose top-left is at (rel_x, rel_y) relative to Contents.png.
-- abs_x, abs_y = Contents.png top-left in screen coords.
local function drawTexClipped(tex, rel_x, rel_y, abs_x, abs_y)
    if tex == nil then return end
    local w, h = tex.Width, tex.Height
    if w == 0 or h == 0 then return end

    local vis_top = math.max(rel_y,     CLIP_Y1)
    local vis_bot = math.min(rel_y + h, CLIP_Y2)
    if vis_top >= vis_bot then return end

    tex:DrawRect(abs_x + rel_x, abs_y + vis_top,
                 0, vis_top - rel_y, w, vis_bot - vis_top)
end

-- Clip-draw a text object.
-- text_tl_x, text_tl_y = absolute screen coords of the text's top-left corner.
local function drawTextClipped(t, text_tl_x, text_tl_y, abs_y)
    if t == nil then return end
    local w, h = t.Width, t.Height
    if w == 0 or h == 0 then return end

    local rel_top = text_tl_y - abs_y          -- relative to Contents.png
    local vis_top = math.max(rel_top,     CLIP_Y1)
    local vis_bot = math.min(rel_top + h, CLIP_Y2)
    if vis_top >= vis_bot then return end

    t:DrawRect(text_tl_x, abs_y + vis_top,
               0, vis_top - rel_top, w, vis_bot - vis_top)
end

-- Clip-draw text with "center" anchor (center-x, center-y).
local function drawTextCenter(font, txt, max_w, col, outline,
                               rel_cx, rel_cy, abs_x, abs_y)
    if font == nil or txt == nil then return end
    local t = font:GetText(txt, false, max_w, col, outline)
    if t == nil then return end
    -- Top-left from center anchor
    drawTextClipped(t,
        abs_x + rel_cx - math.floor(t.Width  / 2),
        abs_y + rel_cy - math.floor(t.Height / 2),
        abs_y)
end

-- Clip-draw text with "left" (center-left) anchor.
local function drawTextLeft(font, txt, max_w, col, outline,
                             rel_lx, rel_cy, abs_x, abs_y)
    if font == nil or txt == nil then return end
    local t = font:GetText(txt, false, max_w, col, outline)
    if t == nil then return end
    -- Left anchor: x = left edge, y = vertical centre
    drawTextClipped(t,
        abs_x + rel_lx,
        abs_y + rel_cy - math.floor(t.Height / 2),
        abs_y)
end

-- ── Lifecycle ─────────────────────────────────────────────────────────────────

function M.load()
    tx_dan_song  = TEXTURE:CreateTexture(TX .. "DanSong.png")
    tx_nb        = TEXTURE:CreateTexture(TX .. "DanSongNb.png")
    for i = 0, 4 do
        tx_diff[i] = TEXTURE:CreateTexture(TX .. "DanSongDiff_" .. i .. ".png")
    end
    tx_gauge     = TEXTURE:CreateTexture(TX .. "Gauge.png")
    tx_exam_name = TEXTURE:CreateTexture(TX .. "ExamName.png")
    tx_exam_rng  = TEXTURE:CreateTexture(TX .. "ExamRange.png")
    tx_exam      = TEXTURE:CreateTexture(TX .. "DanExam.png")
    tx_exam_long = TEXTURE:CreateTexture(TX .. "DanExamLong.png")
    if font_title == nil then
        font_title      = TEXT:Create(28, "regular")
        font_sub        = TEXT:Create(20, "regular")
        font_nb         = TEXT:Create(22, "regular")
        font_level      = TEXT:Create(22, "regular")
        font_exam_name  = TEXT:Create(20, "regular")
        font_exam_range = TEXT:Create(18, "regular")
    end
    scroll_y = 0.0
end

function M.unload()
    local function sd(t) if t ~= nil then t:Dispose() end end
    sd(tx_dan_song)  ; tx_dan_song  = nil
    sd(tx_nb)        ; tx_nb        = nil
    for i = 0, 4 do sd(tx_diff[i]) ; tx_diff[i] = nil end
    sd(tx_gauge)     ; tx_gauge     = nil
    sd(tx_exam_name) ; tx_exam_name = nil
    sd(tx_exam_rng)  ; tx_exam_rng  = nil
    sd(tx_exam)      ; tx_exam      = nil
    sd(tx_exam_long) ; tx_exam_long = nil
    -- fonts are preserved across load/unload cycles (destroyed in M.destroy)
end

function M.destroy()
    local function sd(t) if t ~= nil then t:Dispose() end end
    sd(font_title)      ; font_title      = nil
    sd(font_sub)        ; font_sub        = nil
    sd(font_nb)         ; font_nb         = nil
    sd(font_level)      ; font_level      = nil
    sd(font_exam_name)  ; font_exam_name  = nil
    sd(font_exam_range) ; font_exam_range = nil
end

-- ── Update ─────────────────────────────────────────────────────────────────────

function M.update(dt, song_count)
    if song_count < 4 then
        scroll_y = 0.0
        return
    end
    local total_h = song_count * BAR_GAP_Y
    scroll_y = (scroll_y + SCROLL_SPEED * dt) % total_h
end

-- ── Draw ──────────────────────────────────────────────────────────────────────

-- Draw a single DanSong bar (all elements) at Contents-relative position (bar_rel_x, bar_rel_y).
local function drawBar(song_idx, ds, bar_rel_x, bar_rel_y, abs_x, abs_y)
    local white     = COLOR:CreateColorFromARGB(255, 255, 255, 255)
    local black     = COLOR:CreateColorFromARGB(255,   0,   0,   0)
    local no_outline = COLOR:CreateColorFromARGB(  0,   0,   0,   0)

    -- ── DanSong container ────────────────────────────────────────────────────
    drawTexClipped(tx_dan_song, bar_rel_x, bar_rel_y, abs_x, abs_y)

    -- ── Ordinal stamp (DanSongNb) ─────────────────────────────────────────────
    local sr, sg, sb, dr, dg, db = stampColors(song_idx)

    if tx_nb ~= nil then
        tx_nb:SetColor(COLOR:CreateColorFromARGB(255, sr, sg, sb))
        drawTexClipped(tx_nb,
            bar_rel_x + NB_REL_X,
            bar_rel_y + NB_REL_Y,
            abs_x, abs_y)
        tx_nb:SetColor(COLOR:CreateColorFromARGB(255, 255, 255, 255))
    end

    -- Ordinal text
    local outline_dark = COLOR:CreateColorFromARGB(255, dr, dg, db)
    drawTextCenter(font_nb, ordinal(song_idx), 120, white, outline_dark,
        bar_rel_x + NB_TXT_REL_X,
        bar_rel_y + NB_TXT_REL_Y,
        abs_x, abs_y)

    -- ── Difficulty stamp ──────────────────────────────────────────────────────
    local diff = ds.DifficultyAsInt
    if diff < 0 then diff = 0 end
    if diff > 4 then diff = 4 end

    drawTexClipped(tx_diff[diff],
        bar_rel_x + DIFF_REL_X,
        bar_rel_y + DIFF_REL_Y,
        abs_x, abs_y)

    -- Level number
    local dc = DIFF_OUTLINE[diff] or DIFF_OUTLINE[0]
    local lvl_outline = COLOR:CreateColorFromARGB(255, dc[1], dc[2], dc[3])
    drawTextCenter(font_level, tostring(ds.Level), 80, white, lvl_outline,
        bar_rel_x + LVL_REL_X,
        bar_rel_y + LVL_REL_Y,
        abs_x, abs_y)

    -- ── Title ─────────────────────────────────────────────────────────────────
    drawTextLeft(font_title, ds.Title or "", TITLE_MAX_W, black, no_outline,
        bar_rel_x + TITLE_REL_X,
        bar_rel_y + TITLE_REL_Y,
        abs_x, abs_y)

    -- ── Genre ─────────────────────────────────────────────────────────────────
    local genre = ds.Genre or ""
    if genre ~= "" then
        drawTextLeft(font_sub, genre, SUB_MAX_W, black, no_outline,
            bar_rel_x + SUB_REL_X,
            bar_rel_y + SUB_REL_Y,
            abs_x, abs_y)
    end
end

-- ── Exam drawing ──────────────────────────────────────────────────────────────

-- Draw the overlays and text labels inside one DanExam bar.
-- bar_x, bar_y = top-left of the exam bar in screen space.
local function drawExamBar(exam, bar_x, bar_y)
    local white      = COLOR:CreateColorFromARGB(255, 255, 255, 255)
    local is_more    = (exam.RangeAsInt == 0)
    local rc         = is_more and EXAM_RANGE_MORE_COL or EXAM_RANGE_LESS_COL
    local rng_color  = COLOR:CreateColorFromARGB(255, rc[1], rc[2], rc[3])

    -- ExamName.png (always white)
    if tx_exam_name ~= nil then
        tx_exam_name:Draw(bar_x + EXAM_NAME_REL_X, bar_y + EXAM_NAME_REL_Y)
    end

    -- ExamRange.png (red for More, blue for Less)
    if tx_exam_rng ~= nil then
        tx_exam_rng:SetColor(rng_color)
        tx_exam_rng:Draw(bar_x + EXAM_RANGE_REL_X, bar_y + EXAM_RANGE_REL_Y)
        tx_exam_rng:SetColor(white)
    end

    -- Exam condition name (e.g. "Good count"), dark-brown outline
    local db_outline = COLOR:CreateColorFromARGB(255,
        EXAM_NAME_OUTLINE[1], EXAM_NAME_OUTLINE[2], EXAM_NAME_OUTLINE[3])
    local exam_name  = LANG:GetExamName(exam.TypeAsInt)
    if font_exam_name ~= nil and exam_name ~= nil and exam_name ~= "" then
        local nt = font_exam_name:GetText(exam_name, false, EXAM_TXT_MAX_W, white, db_outline)
        if nt ~= nil then
            nt:DrawAtAnchor(bar_x + EXAM_TXT_NAME_CX, bar_y + EXAM_TXT_NAME_CY, "center")
        end
    end

    -- Exam range text ("or More" / "Less than"), matching outline colour
    local range_str = is_more and EXAM_RANGE_STRINGS[1] or EXAM_RANGE_STRINGS[2]
    if font_exam_range ~= nil then
        local rt = font_exam_range:GetText(range_str, false, EXAM_TXT_MAX_W, white, rng_color)
        if rt ~= nil then
            rt:DrawAtAnchor(bar_x + EXAM_TXT_RANGE_CX, bar_y + EXAM_TXT_RANGE_CY, "center")
        end
    end
end

-- Draw the full exam panel (gauge + 2×3 grid) for a given set of exams.
-- dan_exams: 1-indexed Lua table; [1]=EXAM1(gauge), [2..7]=EXAM2..7 (grid)
local function drawExams(dan_exams, abs_x, abs_y)
    if dan_exams == nil then return end

    local white     = COLOR:CreateColorFromARGB(255, 255, 255, 255)
    local yellow    = COLOR:CreateColorFromARGB(255, 255, 220,   0)
    local red_fill  = COLOR:CreateColorFromARGB(255, 200,   0,   0)

    -- ── Gauge (EXAM1) ─────────────────────────────────────────────────────────
    local gauge_exam = dan_exams[1]
    if tx_gauge ~= nil and gauge_exam ~= nil and gauge_exam.IsSet then
        local gx = abs_x + GAUGE_X
        local gy = abs_y + GAUGE_Y
        local gw = tx_gauge.Width
        local gh = tx_gauge.Height

        -- Full gauge in yellow
        tx_gauge:SetColor(yellow)
        tx_gauge:Draw(gx, gy)
        tx_gauge:SetColor(white)

        -- Left portion up to redValue% drawn in red on top
        local red_w = math.floor(gw * gauge_exam.RedValue / 100)
        if red_w > 0 then
            tx_gauge:SetColor(red_fill)
            tx_gauge:DrawRect(gx, gy, 0, 0, red_w, gh)
            tx_gauge:SetColor(white)
        end
    end

    -- ── Exam 2×3 grid (EXAM2–EXAM7) ──────────────────────────────────────────
    -- Grid layout (column-first):
    --   Row 0: col0 = EXAM2 [2], col1 = EXAM5 [5]
    --   Row 1: col0 = EXAM3 [3], col1 = EXAM6 [6]
    --   Row 2: col0 = EXAM4 [4], col1 = EXAM7 [7]
    -- Rows with two exams use DanExam.png (half-width); rows with only one use DanExamLong.png.
    local exam_h = (tx_exam ~= nil and tx_exam.Height > 0) and tx_exam.Height or 80
    local exam_w = (tx_exam ~= nil and tx_exam.Width  > 0) and tx_exam.Width  or 230

    for row = 0, 2 do
        local c0 = dan_exams[row + 2]   -- EXAM2..4
        local c1 = dan_exams[row + 5]   -- EXAM5..7
        local c0set = c0 ~= nil and c0.IsSet
        local c1set = c1 ~= nil and c1.IsSet

        if not c0set and not c1set then
            -- nothing in this row
        elseif c0set and c1set then
            -- both columns → half-width bars
            local row_y = abs_y + EXAM_GRID_Y + row * (exam_h + 1)
            local bx0   = abs_x + EXAM_GRID_X
            local bx1   = bx0 + exam_w + 1
            if tx_exam ~= nil then tx_exam:Draw(bx0, row_y) end
            drawExamBar(c0, bx0, row_y)
            if tx_exam ~= nil then tx_exam:Draw(bx1, row_y) end
            drawExamBar(c1, bx1, row_y)
        else
            -- col0 only → full-width bar
            local row_y = abs_y + EXAM_GRID_Y + row * (exam_h + 1)
            local bx0   = abs_x + EXAM_GRID_X
            if tx_exam_long ~= nil then tx_exam_long:Draw(bx0, row_y) end
            drawExamBar(c0, bx0, row_y)
        end
    end
end

-- Main draw entry point.
-- abs_x, abs_y  : screen-space top-left of Contents.png (CONTENT_X, CONTENT_Y + slide)
-- dan_songs     : 1-indexed Lua table of LuaSongDanSong objects
-- dan_exams     : 1-indexed Lua table of LuaSongDanExam objects ([1]=EXAM1 … [7]=EXAM7)
function M.draw(abs_x, abs_y, dan_songs, dan_exams)
    if dan_songs == nil then return end
    local song_count = #dan_songs
    if song_count == 0 then return end

    if song_count <= 3 then
        -- ── Static layout ────────────────────────────────────────────────────
        for i = 1, song_count do
            local rel_y = BAR_Y_FIRST + (i - 1) * BAR_GAP_Y
            drawBar(i, dan_songs[i], BAR_X, rel_y, abs_x, abs_y)
        end
    else
        -- ── Scrolling modulo layout ───────────────────────────────────────────
        -- Each bar's virtual y (before scroll) = BAR_Y_FIRST + (i-1)*BAR_GAP_Y
        -- After scroll (bars move upward): rel_y = (virtual_y - scroll_y) % total_h
        local total_h = song_count * BAR_GAP_Y

        local bar_h = (tx_dan_song ~= nil and tx_dan_song.Height > 0)
                      and tx_dan_song.Height or 100

        for i = 1, song_count do
            local virtual_y = BAR_Y_FIRST + (i - 1) * BAR_GAP_Y
            local rel_y = (virtual_y - scroll_y) % total_h

            -- Primary position
            if rel_y < CLIP_Y2 and rel_y + bar_h > CLIP_Y1 then
                drawBar(i, dan_songs[i], BAR_X, rel_y, abs_x, abs_y)
            end

            -- Wrap-around position: bar exiting through the top of the clip window.
            -- When rel_y is large (near total_h) the bar is actually just above CLIP_Y1.
            local rel_y_w = rel_y - total_h
            if rel_y_w < CLIP_Y2 and rel_y_w + bar_h > CLIP_Y1 then
                drawBar(i, dan_songs[i], BAR_X, rel_y_w, abs_x, abs_y)
            end
        end
    end

    -- ── Exam panel (gauge + grid) ─────────────────────────────────────────────
    drawExams(dan_exams, abs_x, abs_y)
end

return M
