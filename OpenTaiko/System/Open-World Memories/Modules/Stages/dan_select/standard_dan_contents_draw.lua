---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- standard_dan_contents_draw.lua
-- Draws the per-song detail bars (DanSong, ordinal stamp, difficulty stamp,
-- level, title, genre) inside Contents.png for the standard dan select.
--
-- Public API:
--   M.load()                       — create textures/fonts (call from _load_resources)
--   M.unload()                     — dispose (call from _unload_resources)
--   M.update(dt, song_count)       — advance scroll counter for ≥4-song dans
--   M.draw(abs_x, abs_y, dan_songs) — draw all bars; abs_x/y = Contents.png top-left in screen coords

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

local tx_dan_song = nil
local tx_nb       = nil
local tx_diff     = {}   -- [0..4]

local font_title  = nil  -- title text
local font_sub    = nil  -- subtitle text
local font_nb     = nil  -- ordinal label
local font_level  = nil  -- star-level number

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
    tx_dan_song = TEXTURE:CreateTexture(TX .. "DanSong.png")
    tx_nb       = TEXTURE:CreateTexture(TX .. "DanSongNb.png")
    for i = 0, 4 do
        tx_diff[i] = TEXTURE:CreateTexture(TX .. "DanSongDiff_" .. i .. ".png")
    end
    if font_title == nil then
        font_title = TEXT:Create(28, "regular")
        font_sub   = TEXT:Create(20, "regular")
        font_nb    = TEXT:Create(22, "regular")
        font_level = TEXT:Create(22, "regular")
    end
    scroll_y = 0.0
end

function M.unload()
    local function sd(t) if t ~= nil then t:Dispose() end end
    sd(tx_dan_song) ; tx_dan_song = nil
    sd(tx_nb)       ; tx_nb       = nil
    for i = 0, 4 do sd(tx_diff[i]) ; tx_diff[i] = nil end
    -- fonts are preserved across load/unload cycles (destroyed in M.destroy)
end

function M.destroy()
    local function sd(t) if t ~= nil then t:Dispose() end end
    sd(font_title) ; font_title = nil
    sd(font_sub)   ; font_sub   = nil
    sd(font_nb)    ; font_nb    = nil
    sd(font_level) ; font_level = nil
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

-- Main draw entry point.
-- abs_x, abs_y  : screen-space top-left of Contents.png (CONTENT_X, CONTENT_Y + slide)
-- dan_songs     : Lua table (1-indexed) of LuaSongDanSong objects
function M.draw(abs_x, abs_y, dan_songs)
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
end

return M
