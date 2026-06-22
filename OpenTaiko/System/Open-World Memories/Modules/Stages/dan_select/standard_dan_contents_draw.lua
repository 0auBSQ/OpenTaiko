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

-- Pre-baked number character set (0-9, -, .)
local NUM_CHARS    = "0123456789-."
local NUM_CHAR_IDX = {}
for i = 1, #NUM_CHARS do NUM_CHAR_IDX[NUM_CHARS:sub(i,i)] = i end

-- Exam value display — global exam (relative to exam bar TL)
local EXAM_GVAL_X  = 250  ; local EXAM_GVAL_Y  = 35   -- left-center anchor
local EXAM_GBEST_X = 250  ; local EXAM_GBEST_Y = 98

-- ExamValue block layout — individual exams
local EV_START_X    = 220   -- first block left edge, relative to bar
local EV_CLIP_PAD_R = 7     -- right clip inset: clip_x2 = bar_x + bar_w - 7
local EV_ORD_CX     = 57   ; local EV_ORD_CY  = 22    -- ordinal, centered in block
local EV_VAL_CX     = 57   ; local EV_VAL_CY  = 71    -- value, centered in block
local EV_BEST_CX    = 57   ; local EV_BEST_CY = 110   -- best score, centered

local EV_SCROLL_THRES_SHORT = 4    -- DanExam scrolls at ≥4 songs
local EV_SCROLL_THRES_LONG  = 9    -- DanExamLong scrolls at ≥9 songs
local EV_SCROLL_SPEED       = 40.0

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

local tx_exam_value     = nil  -- ExamValue.png (individual exam block)
local font_num_big      = nil  -- numbers/dashes for exam values (large)
local font_num_sml      = nil  -- numbers/dashes for best scores (small)
local num_big           = {}   -- [1..#NUM_CHARS] pre-baked LuaTextures
local num_sml           = {}   -- [1..#NUM_CHARS] pre-baked LuaTextures
local exam_val_scroll_x = 0.0  -- horizontal scroll for individual ExamValue blocks

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
    tx_gauge      = TEXTURE:CreateTexture(TX .. "Gauge.png")
    tx_exam_name  = TEXTURE:CreateTexture(TX .. "ExamName.png")
    tx_exam_rng   = TEXTURE:CreateTexture(TX .. "ExamRange.png")
    tx_exam       = TEXTURE:CreateTexture(TX .. "DanExam.png")
    tx_exam_long  = TEXTURE:CreateTexture(TX .. "DanExamLong.png")
    tx_exam_value = TEXTURE:CreateTexture(TX .. "ExamValue.png")
    if font_title == nil then
        font_title      = TEXT:Create(28, "regular")
        font_sub        = TEXT:Create(20, "regular")
        font_nb         = TEXT:Create(22, "regular")
        font_level      = TEXT:Create(22, "regular")
        font_exam_name  = TEXT:Create(20, "regular")
        font_exam_range = TEXT:Create(18, "regular")
        font_num_big    = TEXT:Create(28, "regular")
        font_num_sml    = TEXT:Create(20, "regular")
    end
    -- Pre-bake number char textures (black, no outline)
    local black      = COLOR:CreateColorFromARGB(255, 0, 0, 0)
    local no_outline = COLOR:CreateColorFromARGB(0, 0, 0, 0)
    num_big = {}  ; num_sml = {}
    for i = 1, #NUM_CHARS do
        local c = NUM_CHARS:sub(i, i)
        if font_num_big ~= nil then num_big[i] = font_num_big:GetText(c, false, 999, black, no_outline) end
        if font_num_sml ~= nil then num_sml[i] = font_num_sml:GetText(c, false, 999, black, no_outline) end
    end
    scroll_y = 0.0
end

function M.unload()
    local function sd(t) if t ~= nil then t:Dispose() end end
    sd(tx_dan_song)  ; tx_dan_song  = nil
    sd(tx_nb)        ; tx_nb        = nil
    for i = 0, 4 do sd(tx_diff[i]) ; tx_diff[i] = nil end
    sd(tx_gauge)      ; tx_gauge      = nil
    sd(tx_exam_name)  ; tx_exam_name  = nil
    sd(tx_exam_rng)   ; tx_exam_rng   = nil
    sd(tx_exam)       ; tx_exam       = nil
    sd(tx_exam_long)  ; tx_exam_long  = nil
    sd(tx_exam_value) ; tx_exam_value = nil
    -- fonts and pre-baked char textures are preserved (destroyed in M.destroy)
end

function M.destroy()
    local function sd(t) if t ~= nil then t:Dispose() end end
    sd(font_title)      ; font_title      = nil
    sd(font_sub)        ; font_sub        = nil
    sd(font_nb)         ; font_nb         = nil
    sd(font_level)      ; font_level      = nil
    sd(font_exam_name)  ; font_exam_name  = nil
    sd(font_exam_range) ; font_exam_range = nil
    sd(font_num_big)    ; font_num_big    = nil
    sd(font_num_sml)    ; font_num_sml    = nil
    num_big = {}        ; num_sml         = {}
end

-- ── Update ─────────────────────────────────────────────────────────────────────

function M.update(dt, song_count)
    if song_count < 4 then
        scroll_y = 0.0
    else
        local total_h = song_count * BAR_GAP_Y
        scroll_y = (scroll_y + SCROLL_SPEED * dt) % total_h
    end

    -- ExamValue horizontal scroll (individual exam blocks)
    if song_count >= EV_SCROLL_THRES_SHORT then
        local ev_w    = (tx_exam_value ~= nil and tx_exam_value.Width > 0) and tx_exam_value.Width or 60
        local total_x = song_count * ev_w
        exam_val_scroll_x = (exam_val_scroll_x + EV_SCROLL_SPEED * dt) % total_x
    else
        exam_val_scroll_x = 0.0
    end
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

-- Draw a number string from pre-baked char textures.
-- CSkiaSharpTextRenderer.DrawText pads every bitmap by exactly 25px on the left,
-- 25px on the right, and 25px on the bottom.  So for a texture t:
--   visual_w = t.Width  - 50   (strip left + right padding)
--   visual_h = t.Height - 25   (strip bottom padding; top is flush with ascent)
--   content starts at pixel x=25 within the bitmap
--
-- anchor "left"   = visible left edge at ax, vertically centered at ay.
-- anchor "center" = visible horizontal center at ax, vertically centered at ay.
-- clip_x1/clip_x2 (optional) = screen-space horizontal clip bounds.
local function drawNumStr(tex_arr, str, ax, ay, anchor, clip_x1, clip_x2)
    if str == nil or str == "" or #tex_arr == 0 then return end
    -- Build parts with per-char visual dimensions
    local parts = {} ; local total_vw = 0 ; local max_vh = 0
    for i = 1, #str do
        local idx = NUM_CHAR_IDX[str:sub(i, i)]
        if idx then
            local t = tex_arr[idx]
            if t ~= nil then
                local vw = math.max(1, t.Width  - 50)  -- visible glyph width
                local vh = math.max(1, t.Height - 25)  -- visible glyph height
                parts[#parts+1] = {t=t, vw=vw, vh=vh}
                total_vw = total_vw + vw
                if vh > max_vh then max_vh = vh end
            end
        end
    end
    if #parts == 0 then return end
    -- vis_x = screen x of the VISIBLE left edge of the first character
    local vis_x = (anchor == "center") and (ax - math.floor(total_vw / 2)) or ax
    local sy    = ay - math.floor(max_vh / 2)

    for _, p in ipairs(parts) do
        -- Texture must be drawn 25px to the left of vis_x so content aligns
        local draw_x = vis_x - 25
        if clip_x1 == nil then
            p.t:Draw(draw_x, sy)
        else
            local vis_r = vis_x + p.vw
            local cl = math.max(vis_x, clip_x1)
            local cr = math.min(vis_r, clip_x2)
            if cl < cr then
                -- Source x: 25px left pad + offset into visible region
                local src_x = 25 + (cl - vis_x)
                p.t:DrawRect(cl, sy, src_x, 0, cr - cl, p.t.Height)
            end
        end
        vis_x = vis_x + p.vw
    end
end

-- Draw one ExamValue block at screen position (scr_x, scr_y).
-- si          = 1-based song index (for ordinal colour)
-- se          = LuaSongDanExam for this song's individual exam (may be nil)
-- best_score  = integer best score or -1 if none
-- clip_x1/x2  = horizontal clip window in screen space
local function drawOneEV(scr_x, scr_y, si, se, best_score, clip_x1, clip_x2)
    if tx_exam_value == nil then return end
    local ev_w = tx_exam_value.Width
    local ev_h = tx_exam_value.Height
    local vis_l = math.max(scr_x, clip_x1)
    local vis_r = math.min(scr_x + ev_w, clip_x2)
    if vis_l >= vis_r then return end

    local r, g, b, dr, dg, db = stampColors(si)
    local white = COLOR:CreateColorFromARGB(255, 255, 255, 255)

    -- ExamValue background (tinted with ordinal colour)
    tx_exam_value:SetColor(COLOR:CreateColorFromARGB(255, r, g, b))
    tx_exam_value:DrawRect(vis_l, scr_y, vis_l - scr_x, 0, vis_r - vis_l, ev_h)
    tx_exam_value:SetColor(white)

    -- Ordinal text (white with dark ordinal outline, horizontally clipped)
    -- Uses the same 25px left-pad / visual_w = Width-50 math as drawNumStr.
    local dark_col = COLOR:CreateColorFromARGB(255, dr, dg, db)
    local ot = font_nb ~= nil and font_nb:GetText(ordinal(si), false, 120, white, dark_col) or nil
    if ot ~= nil then
        local vw    = math.max(1, ot.Width  - 50)
        local vh    = math.max(1, ot.Height - 25)
        local ord_l = scr_x + EV_ORD_CX - math.floor(vw / 2)   -- visible left edge
        local oy    = scr_y + EV_ORD_CY - math.floor(vh / 2)
        local cl = math.max(ord_l, clip_x1)
        local cr = math.min(ord_l + vw, clip_x2)
        if cl < cr then
            ot:DrawRect(cl, oy, 25 + (cl - ord_l), 0, cr - cl, ot.Height)
        end
    end

    -- Value (per-song RedValue or "--")
    local val_str = (se ~= nil and se.IsSet) and tostring(se.RedValue) or "--"
    drawNumStr(num_big, val_str, scr_x + EV_VAL_CX, scr_y + EV_VAL_CY, "center", clip_x1, clip_x2)

    -- Best score
    local best_str = (best_score >= 0) and tostring(best_score) or "--"
    drawNumStr(num_sml, best_str, scr_x + EV_BEST_CX, scr_y + EV_BEST_CY, "center", clip_x1, clip_x2)
end

-- Draw all ExamValue blocks for an individual exam row.
-- bar_x, bar_y  = top-left of the exam bar in screen space
-- bar_w         = bar texture width
-- song_count    = total Dan song count
-- song_exams    = {[1..N] = LuaSongDanExam} for this exam slot
-- best_scores   = C# int[] (may be nil)
-- is_long       = true when bar is DanExamLong (higher scroll threshold)
local function drawIndivExamValues(bar_x, bar_y, bar_w, song_count, song_exams, best_scores, is_long)
    if tx_exam_value == nil then return end
    local ev_w      = (tx_exam_value.Width  > 0) and tx_exam_value.Width  or 60
    local clip_x1   = bar_x + EV_START_X
    local clip_x2   = bar_x + bar_w - EV_CLIP_PAD_R
    local threshold = is_long and EV_SCROLL_THRES_LONG or EV_SCROLL_THRES_SHORT
    local scrolling = song_count >= threshold
    local total_ev_w = song_count * ev_w

    for si = 1, song_count do
        local virtual_x = EV_START_X + (si - 1) * ev_w
        local rel_x = scrolling and (virtual_x - exam_val_scroll_x) % total_ev_w or virtual_x
        local scr_x = bar_x + rel_x
        local se    = song_exams and song_exams[si] or nil
        local bv    = -1
        if best_scores ~= nil and best_scores.Length > si - 1 then
            bv = best_scores[si - 1]
        end
        drawOneEV(scr_x, bar_y, si, se, bv, clip_x1, clip_x2)

        -- Wrap-around position (block entering from the right)
        if scrolling then
            local scr_x_w = bar_x + (rel_x - total_ev_w)
            if scr_x_w + ev_w > clip_x1 and scr_x_w < clip_x2 then
                drawOneEV(scr_x_w, bar_y, si, se, bv, clip_x1, clip_x2)
            end
        end
    end
end

-- Draw the fixed overlays + text labels inside one DanExam/DanExamLong bar.
-- exam_ref    = LuaSongDanExam used for type/range info
-- bar_x/bar_y = top-left of the bar in screen space
-- bar_w       = bar width
-- is_indiv    = true → show individual ExamValue blocks; false → show global value
-- exam_slot   = 1-based exam slot index (for best-play lookup)
-- song_count  = total Dan song count
-- song_exams  = {[1..N] = LuaSongDanExam} per-song exams for this slot (individual only)
-- best_scores = C# int[] from GetExam(slot) (may be nil)
-- is_long     = true when bar is DanExamLong
local function drawExamBar(exam_ref, bar_x, bar_y, bar_w,
                           is_indiv, song_count, song_exams, best_scores, is_long)
    local white     = COLOR:CreateColorFromARGB(255, 255, 255, 255)
    local is_more   = (exam_ref.RangeAsInt == 0)
    local rc        = is_more and EXAM_RANGE_MORE_COL or EXAM_RANGE_LESS_COL
    local rng_color = COLOR:CreateColorFromARGB(255, rc[1], rc[2], rc[3])

    -- ExamName.png overlay
    if tx_exam_name ~= nil then
        tx_exam_name:Draw(bar_x + EXAM_NAME_REL_X, bar_y + EXAM_NAME_REL_Y)
    end

    -- ExamRange.png (red for More, blue for Less)
    if tx_exam_rng ~= nil then
        tx_exam_rng:SetColor(rng_color)
        tx_exam_rng:Draw(bar_x + EXAM_RANGE_REL_X, bar_y + EXAM_RANGE_REL_Y)
        tx_exam_rng:SetColor(white)
    end

    -- Exam condition name, dark-brown outline, centered
    local db_outline = COLOR:CreateColorFromARGB(255,
        EXAM_NAME_OUTLINE[1], EXAM_NAME_OUTLINE[2], EXAM_NAME_OUTLINE[3])
    local exam_name = LANG:GetExamName(exam_ref.TypeAsInt)
    if font_exam_name ~= nil and exam_name ~= nil and exam_name ~= "" then
        local nt = font_exam_name:GetText(exam_name, false, EXAM_TXT_MAX_W, white, db_outline)
        if nt ~= nil then
            nt:DrawAtAnchor(bar_x + EXAM_TXT_NAME_CX, bar_y + EXAM_TXT_NAME_CY, "center")
        end
    end

    -- Exam range text, matching outline colour, centered
    local range_str = is_more and EXAM_RANGE_STRINGS[1] or EXAM_RANGE_STRINGS[2]
    if font_exam_range ~= nil then
        local rt = font_exam_range:GetText(range_str, false, EXAM_TXT_MAX_W, white, rng_color)
        if rt ~= nil then
            rt:DrawAtAnchor(bar_x + EXAM_TXT_RANGE_CX, bar_y + EXAM_TXT_RANGE_CY, "center")
        end
    end

    -- ── Value display ─────────────────────────────────────────────────────────
    if not is_indiv then
        -- Global: single value + best score at the right side of the bar
        local val_str  = tostring(exam_ref.RedValue)
        local bv       = (best_scores ~= nil and best_scores.Length > 0) and best_scores[0] or -1
        local best_str = (bv >= 0) and tostring(bv) or "--"
        drawNumStr(num_big, val_str,  bar_x + EXAM_GVAL_X,  bar_y + EXAM_GVAL_Y,  "left", nil, nil)
        drawNumStr(num_sml, best_str, bar_x + EXAM_GBEST_X, bar_y + EXAM_GBEST_Y, "left", nil, nil)
    else
        -- Individual: per-song ExamValue blocks (with optional scroll + clip)
        drawIndivExamValues(bar_x, bar_y, bar_w, song_count, song_exams, best_scores, is_long)
    end
end

-- Draw the full exam panel (gauge + 2×3 grid).
-- dan_exams      : 1-indexed Lua table; [1]=EXAM1(gauge) … [7]=EXAM7
-- dan_songs      : 1-indexed LuaSongDanSong table (for song_count)
-- dan_song_exams : dan_song_exams[si][slot] = LuaSongDanExam (1-based both)
-- dan_best_play  : LuaDanBestPlay object (may be nil)
local function drawExams(dan_exams, dan_songs, dan_song_exams, dan_best_play, abs_x, abs_y)
    if dan_exams == nil then return end

    local white    = COLOR:CreateColorFromARGB(255, 255, 255, 255)
    local yellow   = COLOR:CreateColorFromARGB(255, 255, 220,   0)
    local red_fill = COLOR:CreateColorFromARGB(255, 200,   0,   0)
    local song_count = dan_songs ~= nil and #dan_songs or 0

    -- ── Gauge (EXAM1) ─────────────────────────────────────────────────────────
    local gauge_exam = dan_exams[1]
    if tx_gauge ~= nil and gauge_exam ~= nil and gauge_exam.IsSet then
        local gx = abs_x + GAUGE_X
        local gy = abs_y + GAUGE_Y
        local gw = tx_gauge.Width
        local gh = tx_gauge.Height
        tx_gauge:SetColor(yellow)
        tx_gauge:Draw(gx, gy)
        tx_gauge:SetColor(white)
        local red_w = math.floor(gw * gauge_exam.RedValue / 100)
        if red_w > 0 then
            tx_gauge:SetColor(red_fill)
            tx_gauge:DrawRect(gx, gy, 0, 0, red_w, gh)
            tx_gauge:SetColor(white)
        end
    end

    -- ── Exam 2×3 grid (EXAM2–EXAM7) ──────────────────────────────────────────
    -- Column-first layout:
    --   Row 0: col0=EXAM2[2], col1=EXAM5[5]
    --   Row 1: col0=EXAM3[3], col1=EXAM6[6]
    --   Row 2: col0=EXAM4[4], col1=EXAM7[7]
    local exam_h = (tx_exam ~= nil and tx_exam.Height > 0) and tx_exam.Height or 80
    local exam_w = (tx_exam ~= nil and tx_exam.Width  > 0) and tx_exam.Width  or 230
    local long_w = (tx_exam_long ~= nil and tx_exam_long.Width > 0) and tx_exam_long.Width or exam_w * 2 + 1

    local function getSlotInfo(slot)
        -- Returns: exam_ref (LuaSongDanExam), is_indiv, song_exams_for_slot
        --
        -- The TJA parser sets CTja.Dan_C[i] from the FIRST song that defines the exam
        -- (both global and individual), so dan_exams[slot].IsSet is true for BOTH cases.
        -- To distinguish: global exams are only copied into DanSongs[0]; songs 2..N keep
        -- Dan_C[i] = null.  Individual exams explicitly populate every song's Dan_C[i].
        -- Therefore: if DanSongs[1] (song index 2, 1-based) has the exam set → individual.
        local global_e = dan_exams[slot]
        if global_e == nil or not global_e.IsSet then return nil, false, nil end

        -- Individual detection: song 2's per-song exam is set only for individual exams
        local is_indiv = false
        if song_count >= 2 and dan_song_exams ~= nil and dan_song_exams[2] ~= nil then
            local s2 = dan_song_exams[2][slot]
            is_indiv = s2 ~= nil and s2.IsSet
        end

        if not is_indiv then
            return global_e, false, nil
        end

        -- Collect per-song exams for this slot
        local song_exams = {}
        for si = 1, song_count do
            song_exams[si] = (dan_song_exams ~= nil and dan_song_exams[si]) and dan_song_exams[si][slot] or nil
        end
        return global_e, true, song_exams
    end

    local function drawSlot(slot, bx, by, bar_w, is_long)
        local exam_ref, is_indiv, song_exams = getSlotInfo(slot)
        if exam_ref == nil then return end
        local best_scores = dan_best_play ~= nil and dan_best_play:GetExam(slot) or nil
        drawExamBar(exam_ref, bx, by, bar_w, is_indiv, song_count, song_exams, best_scores, is_long)
    end

    for row = 0, 2 do
        local s0     = row + 2   -- EXAM2..4
        local s1     = row + 5   -- EXAM5..7
        local g0, _  = getSlotInfo(s0)
        local g1, _  = getSlotInfo(s1)
        local c0any  = g0 ~= nil
        local c1any  = g1 ~= nil

        if not c0any and not c1any then
            -- empty row, skip
        elseif c0any and c1any then
            local row_y = abs_y + EXAM_GRID_Y + row * (exam_h + 1)
            local bx0   = abs_x + EXAM_GRID_X
            local bx1   = bx0 + exam_w + 1
            if tx_exam ~= nil then tx_exam:Draw(bx0, row_y) ; tx_exam:Draw(bx1, row_y) end
            drawSlot(s0, bx0, row_y, exam_w, false)
            drawSlot(s1, bx1, row_y, exam_w, false)
        else
            local row_y = abs_y + EXAM_GRID_Y + row * (exam_h + 1)
            local bx0   = abs_x + EXAM_GRID_X
            if tx_exam_long ~= nil then tx_exam_long:Draw(bx0, row_y) end
            drawSlot(s0, bx0, row_y, long_w, true)
        end
    end
end

-- Main draw entry point.
-- abs_x, abs_y   : screen-space top-left of Contents.png (CONTENT_X, CONTENT_Y + slide)
-- dan_songs      : 1-indexed Lua table of LuaSongDanSong objects
-- dan_exams      : 1-indexed Lua table of LuaSongDanExam objects ([1]=EXAM1 … [7]=EXAM7)
-- dan_song_exams : dan_song_exams[si][slot] = per-song LuaSongDanExam (1-based both)
-- dan_best_play  : LuaDanBestPlay object (may be nil)
function M.draw(abs_x, abs_y, dan_songs, dan_exams, dan_song_exams, dan_best_play)
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
    drawExams(dan_exams, dan_songs, dan_song_exams, dan_best_play, abs_x, abs_y)
end

return M
