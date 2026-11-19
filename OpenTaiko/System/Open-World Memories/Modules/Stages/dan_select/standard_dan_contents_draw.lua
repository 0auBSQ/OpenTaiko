---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- standard dan select contents: song bars and exam panel (Contents.png)

local M = {}
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Contents = Layout.contents
local Songs, SongBar, ExamBar, ExamValues = Contents.songs, Contents.song_bar, Contents.exam_bar, Contents.exam_values

-- ==== Constants ====

local TX = "Textures/Contents/"

-- clip window y, from Contents.png top
local CLIP_Y1 = Contents.clip_top
local CLIP_Y2 = Contents.clip_bottom

-- bars within Contents.png
local BAR_X       = Songs.x
local BAR_Y_FIRST = Songs.first_y
local BAR_GAP_Y   = Songs.gap_y

-- offsets within DanSong.png
local NB_REL_X      = SongBar.nb.x        ; local NB_REL_Y      = SongBar.nb.y
local NB_TXT_REL_X  = SongBar.nb_text.x   ; local NB_TXT_REL_Y  = SongBar.nb_text.y
local DIFF_REL_X    = SongBar.diff.x      ; local DIFF_REL_Y     = SongBar.diff.y
local LVL_REL_X     = SongBar.level.x     ; local LVL_REL_Y      = SongBar.level.y
local LVL_PLUS_DX   = SongBar.level.plus_dx ; local LVL_PLUS_DY  = SongBar.level.plus_dy ; local LVL_PLUS_SHIFT = SongBar.level.plus_shift
local TITLE_REL_X   = SongBar.title.x     ; local TITLE_REL_Y    = SongBar.title.y  ; local TITLE_MAX_W    = SongBar.title.max_width
local SUB_REL_X     = SongBar.sub.x       ; local SUB_REL_Y      = SongBar.sub.y    ; local SUB_MAX_W      = SongBar.sub.max_width

-- px/s, bars move up
local SCROLL_SPEED = Animation.contents.scroll_px_per_s

-- EXAM1 gauge top-left, from Contents.png
local GAUGE_X = Contents.gauge.x
local GAUGE_Y = Contents.gauge.y

-- exam grid top-left, from Contents.png
local EXAM_GRID_X = Contents.exam_grid.x
local EXAM_GRID_Y = Contents.exam_grid.y
local EXAM_GRID_GAP = Contents.exam_grid.gap

-- offsets within a DanExam bar
local EXAM_NAME_REL_X    = ExamBar.name.x   -- ExamName.png
local EXAM_NAME_REL_Y    = ExamBar.name.y
local EXAM_RANGE_REL_X   = ExamBar.range.x   -- ExamRange.png
local EXAM_RANGE_REL_Y   = ExamBar.range.y
local EXAM_TXT_NAME_CX   = ExamBar.name_text.x
local EXAM_TXT_NAME_CY   = ExamBar.name_text.y
local EXAM_TXT_RANGE_CX  = ExamBar.range_text.x
local EXAM_TXT_RANGE_CY  = ExamBar.range_text.y
local EXAM_TXT_MAX_W     = ExamBar.text_max_width

-- exam outline colours
local EXAM_NAME_OUTLINE   = ExamBar.name_outline_color
local EXAM_RANGE_MORE_COL = ExamBar.range_more_color
local EXAM_RANGE_LESS_COL = ExamBar.range_less_color

-- RangeAsInt + 1 -> text (0 More, 1 Less); localized in M.load()
local EXAM_RANGE_STRINGS  = {"or More", "Less than"}

local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

-- pre-baked number characters
local NUM_CHARS    = "0123456789-."
local NUM_CHAR_IDX = {}
for i = 1, #NUM_CHARS do NUM_CHAR_IDX[NUM_CHARS:sub(i,i)] = i end

-- global exam values, from the exam bar top-left
local EXAM_GVAL_X  = ExamBar.global_value.x  ; local EXAM_GVAL_Y  = ExamBar.global_value.y   -- left-center anchor
local EXAM_GBEST_X = ExamBar.global_best.x   ; local EXAM_GBEST_Y = ExamBar.global_best.y

-- ExamValue blocks, individual exams
local EV_START_X    = ExamValues.start_x   -- first block left edge, relative to bar
local EV_CLIP_PAD_R = ExamValues.clip_pad_right     -- right clip inset: clip_x2 = bar_x + bar_w - EV_CLIP_PAD_R
local EV_ORD_CX     = ExamValues.ordinal.x   ; local EV_ORD_CY  = ExamValues.ordinal.y    -- centres within the block
local EV_VAL_CX     = ExamValues.value.x     ; local EV_VAL_CY  = ExamValues.value.y
local EV_BEST_CX    = ExamValues.best.x      ; local EV_BEST_CY = ExamValues.best.y

local EV_SCROLL_THRES_SHORT = ExamValues.scroll_from_short    -- DanExam scrolls from this many songs
local EV_SCROLL_THRES_LONG  = ExamValues.scroll_from_long    -- DanExamLong scrolls from this many songs
local EV_SCROLL_SPEED       = Animation.contents.exam_scroll_px_per_s

-- stamp colour per song index; the last repeats
local STAMP_COLORS = Contents.stamp_colors.list
local STAMP_OUTLINE_SHADE = Contents.stamp_colors.outline_shade

-- indexed by DifficultyAsInt (0-4) + 1
local DIFF_OUTLINE = Contents.diff_outline_colors.list

-- ==== Module state ====

local tx_dan_song  = nil
local tx_nb        = nil
local tx_diff      = {}   -- [0..4]

local tx_gauge     = nil  -- Gauge.png
local tx_exam_name = nil  -- ExamName.png overlay
local tx_exam_rng  = nil  -- ExamRange.png overlay
local tx_exam      = nil  -- DanExam.png (half-width)
local tx_exam_long = nil  -- DanExamLong.png (full-width)

local font_title      = nil
local font_sub        = nil  -- subtitle / genre
local font_nb         = nil
local font_level      = nil
local font_level_plus = nil  -- the "+" of a plus level
local font_exam_name  = nil
local font_exam_range = nil

local scroll_y    = 0.0  -- px offset (4+ songs)

local tx_exam_value     = nil  -- ExamValue.png (individual exam block)
local font_num_big      = nil  -- exam values
local font_num_sml      = nil  -- best scores
local num_big           = {}   -- [1..#NUM_CHARS] pre-baked LuaTextures
local num_sml           = {}   -- [1..#NUM_CHARS] pre-baked LuaTextures
local exam_val_scroll_x = 0.0  -- ExamValue blocks x scroll

-- ==== Helpers ====

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

-- idx 1-based; returns r, g, b and the outline r, g, b
local function stampColors(idx)
    local c = STAMP_COLORS[math.min(idx, #STAMP_COLORS)]
    local r, g, b = c[1], c[2], c[3]
    return r, g, b,
           math.floor(r * STAMP_OUTLINE_SHADE), math.floor(g * STAMP_OUTLINE_SHADE), math.floor(b * STAMP_OUTLINE_SHADE)
end

-- rel_x, rel_y: from Contents.png top-left; abs_x, abs_y: Contents.png on screen
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

-- text_tl_x, text_tl_y: text top-left on screen
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

-- centre anchor
local function drawTextCenter(font, txt, max_w, col, outline,
                               rel_cx, rel_cy, abs_x, abs_y)
    if font == nil or txt == nil then return end
    local t = font:GetText(txt, false, max_w, col, outline)
    if t == nil then return end
    drawTextClipped(t,
        abs_x + rel_cx - math.floor(t.Width  / 2),
        abs_y + rel_cy - math.floor(t.Height / 2),
        abs_y)
end

-- left-centre anchor
local function drawTextLeft(font, txt, max_w, col, outline,
                             rel_lx, rel_cy, abs_x, abs_y)
    if font == nil or txt == nil then return end
    local t = font:GetText(txt, false, max_w, col, outline)
    if t == nil then return end
    drawTextClipped(t,
        abs_x + rel_lx,
        abs_y + rel_cy - math.floor(t.Height / 2),
        abs_y)
end

-- ==== Lifecycle ====

-- call from _load_resources
function M.load()
    EXAM_RANGE_STRINGS = { tr("DANSELECT_EXAM_MORE", "or More"), tr("DANSELECT_EXAM_LESS", "Less than") }
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
        local Fonts = Contents.fonts
        font_title      = TEXT:Create(Fonts.title, "regular")
        font_sub        = TEXT:Create(Fonts.sub, "regular")
        font_nb         = TEXT:Create(Fonts.ordinal, "regular")
        font_level      = TEXT:Create(Fonts.level, "regular")
        font_level_plus = TEXT:Create(Fonts.level_plus, "regular")
        font_exam_name  = TEXT:Create(Fonts.exam_name, "regular")
        font_exam_range = TEXT:Create(Fonts.exam_range, "regular")
        font_num_big    = TEXT:Create(Fonts.number_big, "regular")
        font_num_sml    = TEXT:Create(Fonts.number_small, "regular")
    end
    -- pre-bake number glyphs, no outline
    local black      = Config.color(Contents.number_color)
    local no_outline = Config.color(Contents.clear_color)
    num_big = {}  ; num_sml = {}
    for i = 1, #NUM_CHARS do
        local c = NUM_CHARS:sub(i, i)
        if font_num_big ~= nil then num_big[i] = font_num_big:GetText(c, false, 999, black, no_outline) end
        if font_num_sml ~= nil then num_sml[i] = font_num_sml:GetText(c, false, 999, black, no_outline) end
    end
    scroll_y = 0.0
end

-- call from _unload_resources
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
    -- fonts and number glyphs kept until M.destroy
end

function M.destroy()
    local function sd(t) if t ~= nil then t:Dispose() end end
    sd(font_title)      ; font_title      = nil
    sd(font_sub)        ; font_sub        = nil
    sd(font_nb)         ; font_nb         = nil
    sd(font_level)      ; font_level      = nil
    sd(font_level_plus) ; font_level_plus = nil
    sd(font_exam_name)  ; font_exam_name  = nil
    sd(font_exam_range) ; font_exam_range = nil
    sd(font_num_big)    ; font_num_big    = nil
    sd(font_num_sml)    ; font_num_sml    = nil
    num_big = {}        ; num_sml         = {}
end

-- ==== Update ====

-- dt: sec
function M.update(dt, song_count)
    if song_count < Songs.scroll_from then
        scroll_y = 0.0
    else
        local total_h = song_count * BAR_GAP_Y
        scroll_y = (scroll_y + SCROLL_SPEED * dt) % total_h
    end

    -- ExamValue x scroll
    if song_count >= EV_SCROLL_THRES_SHORT then
        local ev_w    = (tx_exam_value ~= nil and tx_exam_value.Width > 0) and tx_exam_value.Width or ExamValues.block_w_fallback
        local total_x = song_count * ev_w
        exam_val_scroll_x = (exam_val_scroll_x + EV_SCROLL_SPEED * dt) % total_x
    else
        exam_val_scroll_x = 0.0
    end
end

-- ==== Draw ====

-- one DanSong bar at (bar_rel_x, bar_rel_y) from Contents.png
local function drawBar(song_idx, ds, bar_rel_x, bar_rel_y, abs_x, abs_y)
    local white     = Config.color(Contents.text_color)
    local black     = Config.color(Contents.number_color)
    local no_outline = Config.color(Contents.clear_color)

    drawTexClipped(tx_dan_song, bar_rel_x, bar_rel_y, abs_x, abs_y)

    -- ordinal stamp (DanSongNb)
    local sr, sg, sb, dr, dg, db = stampColors(song_idx)

    if tx_nb ~= nil then
        tx_nb:SetColor(COLOR:CreateColorFromARGB(255, sr, sg, sb))
        drawTexClipped(tx_nb,
            bar_rel_x + NB_REL_X,
            bar_rel_y + NB_REL_Y,
            abs_x, abs_y)
        tx_nb:SetColor(COLOR:CreateColorFromARGB(255, 255, 255, 255))
    end

    local outline_dark = COLOR:CreateColorFromARGB(255, dr, dg, db)
    drawTextCenter(font_nb, ordinal(song_idx), SongBar.nb_text.max_width, white, outline_dark,
        bar_rel_x + NB_TXT_REL_X,
        bar_rel_y + NB_TXT_REL_Y,
        abs_x, abs_y)

    -- difficulty stamp
    local diff = ds.DifficultyAsInt
    if diff < 0 then diff = 0 end
    if diff > 4 then diff = 4 end

    drawTexClipped(tx_diff[diff],
        bar_rel_x + DIFF_REL_X,
        bar_rel_y + DIFF_REL_Y,
        abs_x, abs_y)

    -- level; a plus level's "+" overlaps the last digit's top-right
    local dc = DIFF_OUTLINE[diff + 1] or DIFF_OUTLINE[1]
    local lvl_outline = COLOR:CreateColorFromARGB(255, dc[1], dc[2], dc[3])
    local plus = ds.LevelDecimal - ds.Level >= 0.5
    local lvl_x = LVL_REL_X + (plus and LVL_PLUS_SHIFT or 0)
    drawTextCenter(font_level, tostring(ds.Level), SongBar.level.max_width, white, lvl_outline,
        bar_rel_x + lvl_x,
        bar_rel_y + LVL_REL_Y,
        abs_x, abs_y)
    local t = (plus and font_level ~= nil) and font_level:GetText(tostring(ds.Level), false, SongBar.level.max_width, white, lvl_outline) or nil
    if t ~= nil then
        -- digits' right edge: bitmap pads 25px each side
        local right = lvl_x + math.floor(t.Width / 2) - 25
        drawTextCenter(font_level_plus, "+", SongBar.level.max_width, white, lvl_outline,
            bar_rel_x + right + LVL_PLUS_DX,
            bar_rel_y + LVL_REL_Y + LVL_PLUS_DY,
            abs_x, abs_y)
    end

    drawTextLeft(font_title, ds.Title or "", TITLE_MAX_W, black, no_outline,
        bar_rel_x + TITLE_REL_X,
        bar_rel_y + TITLE_REL_Y,
        abs_x, abs_y)

    local genre = ds.Genre or ""
    if genre ~= "" then
        drawTextLeft(font_sub, genre, SUB_MAX_W, black, no_outline,
            bar_rel_x + SUB_REL_X,
            bar_rel_y + SUB_REL_Y,
            abs_x, abs_y)
    end
end

-- ==== Exam drawing ====

-- number string from pre-baked glyphs
-- glyph bitmaps pad 25px on both sides and the bottom (CSkiaSharpTextRenderer)
-- anchor "left": visible left edge at ax; "center": visible centre at ax; y centred at ay
-- clip_x1, clip_x2: optional screen x clip
local function drawNumStr(tex_arr, str, ax, ay, anchor, clip_x1, clip_x2)
    if str == nil or str == "" or #tex_arr == 0 then return end
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
    -- vis_x: visible left edge of the first character
    local vis_x = (anchor == "center") and (ax - math.floor(total_vw / 2)) or ax
    local sy    = ay - math.floor(max_vh / 2)

    for _, p in ipairs(parts) do
        -- 25px left pad
        local draw_x = vis_x - 25
        if clip_x1 == nil then
            p.t:Draw(draw_x, sy)
        else
            local vis_r = vis_x + p.vw
            local cl = math.max(vis_x, clip_x1)
            local cr = math.min(vis_r, clip_x2)
            if cl < cr then
                -- src x: 25px pad + offset into the visible part
                local src_x = 25 + (cl - vis_x)
                p.t:DrawRect(cl, sy, src_x, 0, cr - cl, p.t.Height)
            end
        end
        vis_x = vis_x + p.vw
    end
end

-- one ExamValue block at screen (scr_x, scr_y)
-- si: 1-based song index; se: LuaSongDanExam or nil
-- best_score: -1 if none; clip_x1, clip_x2: screen x clip
local function drawOneEV(scr_x, scr_y, si, se, best_score, clip_x1, clip_x2)
    if tx_exam_value == nil then return end
    local ev_w = tx_exam_value.Width
    local ev_h = tx_exam_value.Height
    local vis_l = math.max(scr_x, clip_x1)
    local vis_r = math.min(scr_x + ev_w, clip_x2)
    if vis_l >= vis_r then return end

    local r, g, b, dr, dg, db = stampColors(si)
    local white = Config.color(Contents.text_color)

    -- tinted with the stamp colour
    tx_exam_value:SetColor(COLOR:CreateColorFromARGB(255, r, g, b))
    tx_exam_value:DrawRect(vis_l, scr_y, vis_l - scr_x, 0, vis_r - vis_l, ev_h)
    tx_exam_value:SetColor(white)

    -- ordinal, x clipped; bitmap pad math as in drawNumStr
    local dark_col = COLOR:CreateColorFromARGB(255, dr, dg, db)
    local ot = font_nb ~= nil and font_nb:GetText(ordinal(si), false, SongBar.nb_text.max_width, white, dark_col) or nil
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

    local val_str = (se ~= nil and se.IsSet) and tostring(se.RedValue) or "--"
    drawNumStr(num_big, val_str, scr_x + EV_VAL_CX, scr_y + EV_VAL_CY, "center", clip_x1, clip_x2)

    local best_str = (best_score >= 0) and tostring(best_score) or "--"
    drawNumStr(num_sml, best_str, scr_x + EV_BEST_CX, scr_y + EV_BEST_CY, "center", clip_x1, clip_x2)
end

-- ExamValue blocks of an individual exam row
-- bar_x, bar_y: exam bar top-left on screen; bar_w: bar texture width
-- song_exams: [1..N] LuaSongDanExam; best_scores: C# int[] or nil
-- is_long: DanExamLong (higher scroll threshold)
local function drawIndivExamValues(bar_x, bar_y, bar_w, song_count, song_exams, best_scores, is_long)
    if tx_exam_value == nil then return end
    local ev_w      = (tx_exam_value.Width  > 0) and tx_exam_value.Width  or ExamValues.block_w_fallback
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

        -- wrap-around copy entering from the right
        if scrolling then
            local scr_x_w = bar_x + (rel_x - total_ev_w)
            if scr_x_w + ev_w > clip_x1 and scr_x_w < clip_x2 then
                drawOneEV(scr_x_w, bar_y, si, se, bv, clip_x1, clip_x2)
            end
        end
    end
end

-- overlays and labels of one DanExam / DanExamLong bar
-- exam_ref: LuaSongDanExam for type and range; bar_x, bar_y: top-left on screen
-- is_indiv: per-song ExamValue blocks, else the global value
-- song_exams: [1..N] LuaSongDanExam (individual only); best_scores: GetExam(slot) int[] or nil
local function drawExamBar(exam_ref, bar_x, bar_y, bar_w,
                           is_indiv, song_count, song_exams, best_scores, is_long)
    local white     = Config.color(Contents.text_color)
    local is_more   = (exam_ref.RangeAsInt == 0)
    local rc        = is_more and EXAM_RANGE_MORE_COL or EXAM_RANGE_LESS_COL
    local rng_color = COLOR:CreateColorFromARGB(255, rc[1], rc[2], rc[3])

    if tx_exam_name ~= nil then
        tx_exam_name:Draw(bar_x + EXAM_NAME_REL_X, bar_y + EXAM_NAME_REL_Y)
    end

    -- tinted per range type
    if tx_exam_rng ~= nil then
        tx_exam_rng:SetColor(rng_color)
        tx_exam_rng:Draw(bar_x + EXAM_RANGE_REL_X, bar_y + EXAM_RANGE_REL_Y)
        tx_exam_rng:SetColor(white)
    end

    local db_outline = COLOR:CreateColorFromARGB(255,
        EXAM_NAME_OUTLINE[1], EXAM_NAME_OUTLINE[2], EXAM_NAME_OUTLINE[3])
    local exam_name = LANG:GetExamName(exam_ref.TypeAsInt)
    if font_exam_name ~= nil and exam_name ~= nil and exam_name ~= "" then
        local nt = font_exam_name:GetText(exam_name, false, EXAM_TXT_MAX_W, white, db_outline)
        if nt ~= nil then
            nt:DrawAtAnchor(bar_x + EXAM_TXT_NAME_CX, bar_y + EXAM_TXT_NAME_CY, "center")
        end
    end

    local range_str = is_more and EXAM_RANGE_STRINGS[1] or EXAM_RANGE_STRINGS[2]
    if font_exam_range ~= nil then
        local rt = font_exam_range:GetText(range_str, false, EXAM_TXT_MAX_W, white, rng_color)
        if rt ~= nil then
            rt:DrawAtAnchor(bar_x + EXAM_TXT_RANGE_CX, bar_y + EXAM_TXT_RANGE_CY, "center")
        end
    end

    if not is_indiv then
        -- global: value and best score
        local val_str  = tostring(exam_ref.RedValue)
        local bv       = (best_scores ~= nil and best_scores.Length > 0) and best_scores[0] or -1
        local best_str = (bv >= 0) and tostring(bv) or "--"
        drawNumStr(num_big, val_str,  bar_x + EXAM_GVAL_X,  bar_y + EXAM_GVAL_Y,  "left", nil, nil)
        drawNumStr(num_sml, best_str, bar_x + EXAM_GBEST_X, bar_y + EXAM_GBEST_Y, "left", nil, nil)
    else
        -- individual: per-song blocks
        drawIndivExamValues(bar_x, bar_y, bar_w, song_count, song_exams, best_scores, is_long)
    end
end

-- exam panel: EXAM1 gauge, then the 2x3 grid
-- dan_exams: [1] = EXAM1 .. [7] = EXAM7; dan_song_exams[si][slot]: LuaSongDanExam, 1-based
-- dan_best_play: LuaDanBestPlay or nil
local function drawExams(dan_exams, dan_songs, dan_song_exams, dan_best_play, abs_x, abs_y)
    if dan_exams == nil then return end

    local white    = Config.color(Contents.text_color)
    local yellow   = Config.color(Contents.gauge.color)
    local red_fill = Config.color(Contents.gauge.fill_color)
    local song_count = dan_songs ~= nil and #dan_songs or 0

    -- EXAM1 gauge
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

    -- 2x3 grid, column-first: EXAM2..4 left, EXAM5..7 right
    local exam_h = (tx_exam ~= nil and tx_exam.Height > 0) and tx_exam.Height or Contents.exam_grid.bar_h_fallback
    local exam_w = (tx_exam ~= nil and tx_exam.Width  > 0) and tx_exam.Width  or Contents.exam_grid.bar_w_fallback
    local long_w = (tx_exam_long ~= nil and tx_exam_long.Width > 0) and tx_exam_long.Width or exam_w * 2 + EXAM_GRID_GAP

    local function getSlotInfo(slot)
        -- returns exam_ref (LuaSongDanExam), is_indiv, song_exams
        -- IsSet holds for global and individual exams (Dan_C[i] from the first song defining it);
        -- global exams fill only DanSongs[0], so a set song-2 exam means individual
        local global_e = dan_exams[slot]
        if global_e == nil or not global_e.IsSet then return nil, false, nil end

        local is_indiv = false
        if song_count >= 2 and dan_song_exams ~= nil and dan_song_exams[2] ~= nil then
            local s2 = dan_song_exams[2][slot]
            is_indiv = s2 ~= nil and s2.IsSet
        end

        if not is_indiv then
            return global_e, false, nil
        end

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
            -- empty row
        elseif c0any and c1any then
            local row_y = abs_y + EXAM_GRID_Y + row * (exam_h + EXAM_GRID_GAP)
            local bx0   = abs_x + EXAM_GRID_X
            local bx1   = bx0 + exam_w + EXAM_GRID_GAP
            if tx_exam ~= nil then tx_exam:Draw(bx0, row_y) ; tx_exam:Draw(bx1, row_y) end
            drawSlot(s0, bx0, row_y, exam_w, false)
            drawSlot(s1, bx1, row_y, exam_w, false)
        else
            local row_y = abs_y + EXAM_GRID_Y + row * (exam_h + EXAM_GRID_GAP)
            local bx0   = abs_x + EXAM_GRID_X
            if tx_exam_long ~= nil then tx_exam_long:Draw(bx0, row_y) end
            drawSlot(s0, bx0, row_y, long_w, true)
        end
    end
end

-- abs_x, abs_y: Contents.png top-left on screen (CONTENT_X, CONTENT_Y + slide)
-- dan_songs: [1..N] LuaSongDanSong; other args as in drawExams
function M.draw(abs_x, abs_y, dan_songs, dan_exams, dan_song_exams, dan_best_play)
    if dan_songs == nil then return end
    local song_count = #dan_songs
    if song_count == 0 then return end

    if song_count < Songs.scroll_from then
        -- static layout
        for i = 1, song_count do
            local rel_y = BAR_Y_FIRST + (i - 1) * BAR_GAP_Y
            drawBar(i, dan_songs[i], BAR_X, rel_y, abs_x, abs_y)
        end
    else
        -- scrolling: rel_y = (virtual_y - scroll_y) % total_h, bars move up
        local total_h = song_count * BAR_GAP_Y

        local bar_h = (tx_dan_song ~= nil and tx_dan_song.Height > 0)
                      and tx_dan_song.Height or Songs.bar_h_fallback

        for i = 1, song_count do
            local virtual_y = BAR_Y_FIRST + (i - 1) * BAR_GAP_Y
            local rel_y = (virtual_y - scroll_y) % total_h

            if rel_y < CLIP_Y2 and rel_y + bar_h > CLIP_Y1 then
                drawBar(i, dan_songs[i], BAR_X, rel_y, abs_x, abs_y)
            end

            -- wrap-around copy: rel_y near total_h means just above CLIP_Y1
            local rel_y_w = rel_y - total_h
            if rel_y_w < CLIP_Y2 and rel_y_w + bar_h > CLIP_Y1 then
                drawBar(i, dan_songs[i], BAR_X, rel_y_w, abs_x, abs_y)
            end
        end
    end

    drawExams(dan_exams, dan_songs, dan_song_exams, dan_best_play, abs_x, abs_y)
end

return M
