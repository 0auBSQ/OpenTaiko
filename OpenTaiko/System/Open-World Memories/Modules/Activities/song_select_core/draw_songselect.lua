---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- draw_songselect.lua  —  Song-select panel drawing for song_select_core.

local CFG = require("sscore_config")   -- Config/layout.json (skinner-editable); values fall back to the defaults below

local M = {}
-- hoisted colors (per-frame COLOR:Create* calls allocate userdata every frame); overridable via config
local SONGLIST_GOLD  = CFG.color("colors.songlist_gold", COLOR:CreateColorFromARGB(255, 242, 207, 1))   -- selected-bar title tint
local COL_WHITE      = COLOR:CreateColorFromHex("ffffffff")
local COL_BLACK      = COLOR:CreateColorFromHex("ff000000")
local COL_VAULT_GRAY = CFG.color("colors.vault_gray", COLOR:CreateColorFromHex("ff808080"))
local COL_TAG_FIRE   = CFG.color("colors.tag_fire", COLOR:CreateColorFromHex("ffac0c0c"))
local COL_TAG_STORM  = CFG.color("colors.tag_storm", COLOR:CreateColorFromHex("ff83159e"))
local COL_BPM_SLOW   = CFG.color("colors.bpm_slow", COLOR:CreateColorFromHex("ff95ccff"))
local COL_BPM_FAST   = CFG.color("colors.bpm_fast", COLOR:CreateColorFromHex("ffff9ec3"))

local statsCache = { player = -1, diff = -1 }   -- clear-count queries are save-file scans; key on (player, course)
local G   -- shared state injected by Script.lua

-- ── Layout constants (defaults; overridable via Config/layout.json) ─────────────

local SONGLIST_ORIGIN_X           = CFG.num("song_list.origin_x", 660)
local SONGLIST_ORIGIN_Y           = CFG.num("song_list.origin_y", 500)
local SONGLIST_OFFSET_X           = CFG.num("song_list.offset_x", 45)
local SONGLIST_OFFSET_Y           = CFG.num("song_list.offset_y", 120)
local SONGLIST_TEXT_OFFSET_X      = CFG.num("song_list.text_offset_x", -65)
local SONGLIST_TEXT_OFFSET_Y      = CFG.num("song_list.text_offset_y", 15)
local SONGLIST_SELECTED_X_DIFF    = CFG.num("song_list.selected_x_diff", 50)
local SONGLIST_SELECTED_ARROW_GAP = CFG.num("song_list.selected_arrow_gap", 925)
local SONGBAR_LABEL_X_OFFSET      = CFG.num("song_list.label_x_offset", 288)
-- the back bar's colour, and the random bar's rainbow: RAINBOW_STEPS hues made once, pastel enough for the
-- title to stay readable; the rainbow runs along the bar, one full pass every RAINBOW_MS
local BACK_BAR_COLOR = CFG.color("colors.back_bar", COLOR:CreateColorFromARGB(255, 131, 113, 96))
-- the song info under a BLURRED song, leaning purple
local BLURRED_INFO_TINT = CFG.color("colors.blurred_info", COLOR:CreateColorFromARGB(255, 222, 196, 255))
local RAINBOW_MS     = CFG.num("song_list.random_rainbow_ms", 4000)
local RAINBOW_STEPS  = 120
local RAINBOW_SLICE  = CFG.num("song_list.random_rainbow_slice", 8)   -- px per tinted slice of the bar
local RAINBOW_SPAN   = CFG.num("song_list.random_rainbow_span", 1)     -- hue turns across the bar
local RAINBOW = {}
for i = 0, RAINBOW_STEPS - 1 do
    local h, s, v = i / RAINBOW_STEPS * 6, 0.5, 1
    local c = v * s
    local x = c * (1 - math.abs(h % 2 - 1))
    local r, g, b = 0, 0, 0
    if h < 1 then r, g = c, x elseif h < 2 then r, g = x, c elseif h < 3 then g, b = c, x
    elseif h < 4 then g, b = x, c elseif h < 5 then r, b = x, c else r, b = c, x end
    local m = v - c
    RAINBOW[i] = COLOR:CreateColorFromARGB(255, math.floor((r + m) * 255 + 0.5), math.floor((g + m) * 255 + 0.5),
        math.floor((b + m) * 255 + 0.5))
end
-- The "+" of a plus level rides the last digit's top-right corner: its centre, from the digits' right cell
-- edge and their centre line (BarLevel/plus.png)
local SONGBAR_PLUS_DX             = CFG.num("song_list.plus_dx", -19)
local SONGBAR_PLUS_DY             = CFG.num("song_list.plus_dy", -27)
-- the plus overhangs the digits' ink by ~9px, so the whole number moves left by half of that to stay centred
local SONGBAR_PLUS_SHIFT          = CFG.num("song_list.plus_shift", -5)
-- the "[key] - Show shortcuts" line under the Select-a-Song overlay: position and its slow opacity pulse
local HELPER_X           = CFG.num("song_list.shortcuts_helper_x", 44)
local HELPER_Y           = CFG.num("song_list.shortcuts_helper_y", 178)
local HELPER_PULSE_MS    = CFG.num("song_list.shortcuts_helper_pulse_ms", 2400)
local HELPER_OPACITY_MIN = CFG.num("song_list.shortcuts_helper_opacity_min", 0.45)
local HELPER_PAD_X       = CFG.num("song_list.shortcuts_helper_pad_x", 16)       -- the dark box around the line
local HELPER_PAD_Y       = CFG.num("song_list.shortcuts_helper_pad_y", 8)
local HELPER_BOX_ALPHA   = CFG.num("song_list.shortcuts_helper_box_alpha", 150)
local HELPER_BOX_RADIUS  = CFG.num("song_list.shortcuts_helper_box_radius", 10)
-- the translucent band under the song list, sheared to the bars' diagonal (origin/offset above): its
-- horizontal width, its centre's horizontal offset from the bar centre line, its alpha and edge fade
local BACKDROP_W         = CFG.num("song_list.backdrop_width", 980)
local BACKDROP_DX        = CFG.num("song_list.backdrop_offset_x", 0)
-- the band's colour and alpha: blending toward a dark grey drains the saturation of what is under the
-- band (chroma scales by 1 - alpha) while pulling its brightness toward the grey, a mild darkening on
-- most backgrounds; black would only darken
local BACKDROP_COLOR     = CFG.numList("song_list.backdrop_color", { 88, 88, 96 })
local BACKDROP_ALPHA     = CFG.num("song_list.backdrop_alpha", 150)
local BACKDROP_FADE      = CFG.num("song_list.backdrop_fade", 36)
-- "normal" lays the colour over the background at that alpha; "screen" lightens the background by the
-- colour times the alpha, and never darkens it; "multi" multiplies the background by the colour, the
-- alpha saying how far (255 = the colour itself), and never lightens it
local BACKDROP_BLEND     = CFG.str("song_list.backdrop_blend", "normal")
-- Easy / Normal / Hard levels from which the tag plays its checker animation (bar_levelbgchecker<diff>.png)
local SONGBAR_CHECKER_LEVEL       = {
    [0] = CFG.num("song_list.checker_level_easy", 6),
    [1] = CFG.num("song_list.checker_level_normal", 8),
    [2] = CFG.num("song_list.checker_level_hard", 9),
}

-- the folder info placeholder: a folder (bg_folderinfo.png) tinted with the folder's colour, its top
-- centred on the info column; the rows of its name, description lines and song count
local FOLDERINFO_CENTER_X  = CFG.num("folder_info.center_x", 1548)
local FOLDERINFO_PANEL_Y   = CFG.num("folder_info.panel_y", 270)
local FOLDERINFO_WIDTH     = CFG.num("folder_info.width", 600)
local FOLDERINFO_NAME_Y    = CFG.num("folder_info.name_y", 382)
local FOLDERINFO_TEXT_Y    = CFG.num("folder_info.text_y", 478)
local FOLDERINFO_LINE_GAP  = CFG.num("folder_info.line_gap", 48)
local FOLDERINFO_COUNT_Y   = CFG.num("folder_info.count_y", 668)
local FOLDERINFO_COUNT_GAP = CFG.num("folder_info.count_gap", 54)

local SONGINFO_DIFFICULTIES_ORIGIN_X = CFG.num("song_info.difficulties_origin_x", 1790)
local SONGINFO_DIFFICULTIES_ORIGIN_Y = CFG.num("song_info.difficulties_origin_y", 154)
local SONGINFO_DIFFICULTIES_GAP_Y    = CFG.num("song_info.difficulties_gap_y", 130)
local SONGINFO_PLUS_DX               = CFG.num("song_info.plus_dx", -8)
local SONGINFO_PLUS_DY               = CFG.num("song_info.plus_dy", -14)
local SONGINFO_HASVIDEO_ORIGIN_X     = CFG.num("song_info.has_video_origin_x", 1064)
local SONGINFO_HASVIDEO_ORIGIN_Y     = CFG.num("song_info.has_video_origin_y", 257)
local SONGINFO_EXPLICIT_ORIGIN_X     = CFG.num("song_info.explicit_origin_x", 1266)
local SONGINFO_EXPLICIT_ORIGIN_Y     = CFG.num("song_info.explicit_origin_y", 151)
local SONGINFO_SUBTITLE_ORIGIN_X     = CFG.num("song_info.subtitle_origin_x", 1536)
local SONGINFO_SUBTITLE_ORIGIN_Y     = CFG.num("song_info.subtitle_origin_y", 689)
local SONGINFO_SUBTITLE_MWIDTH       = CFG.num("song_info.subtitle_max_width", 530)
local SONGINFO_BPM_ORIGIN_X          = CFG.num("song_info.bpm_origin_x", 1780)
local SONGINFO_BPM_ORIGIN_Y          = CFG.num("song_info.bpm_origin_y", 877)
local SONGINFO_BPM_MWIDTH            = CFG.num("song_info.bpm_max_width", 240)
local SONGINFO_BPM_ROTATION          = CFG.num("song_info.bpm_rotation", 355.55)   -- tilt to match the BPM plate image
-- Song speed notice over the metronome (only when the multiplier is not 1): a label and the multiplier,
-- with two large ▶ behind them, centred on the text block, pulsing in turn once per beat of the sped-up
-- BPM. Everything is tinted with the BPM text colour.
local SONGINFO_SPEED_ORIGIN_X        = CFG.num("song_info.speed_origin_x", 1804)
local SONGINFO_SPEED_LABEL_Y         = CFG.num("song_info.speed_label_y", 729)
local SONGINFO_SPEED_VALUE_Y         = CFG.num("song_info.speed_value_y", 753)
local SONGINFO_SPEED_ARROW_GAP       = CFG.num("song_info.speed_arrow_gap", 40)   -- between the two ▶ centres
local SONGINFO_SPEED_LABEL           = CFG.str("song_info.speed_label", nil)   -- a skin's own text; else the localized one
local SONGINFO_CHARTER_ORIGIN_X      = CFG.num("song_info.charter_origin_x", 1216)
local SONGINFO_CHARTER_ORIGIN_Y      = CFG.num("song_info.charter_origin_y", 750)
local SONGINFO_CHARTER_MWIDTH        = CFG.num("song_info.charter_max_width", 512)
local SONGINFO_LENGTH_ORIGIN_X       = CFG.num("song_info.length_origin_x", 1216)
local SONGINFO_LENGTH_ORIGIN_Y       = CFG.num("song_info.length_origin_y", 806)
local SONGINFO_LENGTH_MWIDTH         = CFG.num("song_info.length_max_width", 420)

local PREIMAGE_ORIGIN_X = CFG.num("preimage.origin_x", 1276)
local PREIMAGE_ORIGIN_Y = CFG.num("preimage.origin_y", 146)
local PREIMAGE_SIZE_X   = CFG.num("preimage.size_x", 500)
local PREIMAGE_SIZE_Y   = CFG.num("preimage.size_y", 500)

local HEADER_OFFSET_X          = CFG.num("header.offset_x", 1780)
local HEADER_BOX_TEXT_OFFSET_X = CFG.num("header.box_text_offset_x", 250)
local HEADER_BOX_TEXT_OFFSET_Y = CFG.num("header.box_text_offset_y", 12)
local HEADER_DIFF_ALPHA        = CFG.num("header.diffselect_alpha", 0.4)

local BARLEFT_X_OFFSET             = CFG.num("song_list.barleft_x_offset", -67)   -- pixels left of bar.png topleft

local NAMEPLATE_BOX_FOLDED_SIZE_Y  = CFG.num("nameplate.box_folded_size_y", 182)
local NAMEPLATE_SECONDARY_OFFSET_Y = CFG.num("nameplate.secondary_offset_y", 81)
local NAMEPLATE_BOX_START_X        = CFG.num("nameplate.box_start_x", 0)
local NAMEPLATE_BOX_SPACING_X      = CFG.num("nameplate.box_spacing_x", 384)
local NAMEPLATE_OFFSET_X           = CFG.num("nameplate.offset_x", 27)
local NAMEPLATE_OFFSET_Y           = CFG.num("nameplate.offset_y", 37)
local NAMEPLATE_HEIGHT             = CFG.num("nameplate.height", 81)
local PUCHI_OFFSET_X               = CFG.num("nameplate.puchi_offset_x", 60)

-- ── Init ──────────────────────────────────────────────────────────────────────

function M.init(g)
    G = g
end

-- ── Local utilities ───────────────────────────────────────────────────────────

local function formatDuration(ms)
    if ms <= 0 then return "?:??" end
    local totalSec = math.floor(ms / 1000)
    return string.format("%d:%02d", math.floor(totalSec / 60), totalSec % 60)
end

local function formatNumber(n, decimals)
    local s = string.format("%." .. decimals .. "f", n)
    s = s:gsub("0+$", ""):gsub("%.$", "")
    return s
end

-- lv = the slot's cached { lv, diff, isPlus, isVault } (nil when the node has no chart)
local function drawLevelTag(lv, x, y)
    if lv == nil then return end
    local labelH = G.bars["levellabels"].Height / 5
    local labelW = G.bars["levellabels"].Width

    if lv.isVault then
        -- Vault songs: animated strip (same frame counter as storm)
        G.bars["levellabelsvault"]:DrawRectAtAnchor(x, y, 0, labelH * G.levelLabelFrame, labelW, labelH, "center")
    elseif lv.diff <= 2 and lv.lv >= SONGBAR_CHECKER_LEVEL[lv.diff] and G.bars["levellabelschecker" .. lv.diff] then
        -- a high level for its difficulty: the tag's colour with the turning checker
        G.bars["levellabelschecker" .. lv.diff]:DrawRectAtAnchor(x, y, 0, labelH * G.levelLabelFrame, labelW, labelH, "center")
    elseif lv.diff < 3 or lv.lv <= 10 then
        G.bars["levellabels"]:DrawRectAtAnchor(x, y, 0, labelH * lv.diff, labelW, labelH, "center")
    elseif lv.diff == 3 then
        G.bars["levellabelsfire"]:DrawRectAtAnchor(x, y, 0, labelH * G.levelLabelFrame, labelW, labelH, "center")
    else
        G.bars["levellabelsstorm"]:DrawRectAtAnchor(x, y, 0, labelH * G.levelLabelFrame, labelW, labelH, "center")
    end
    if lv.isPlus then x = x + SONGBAR_PLUS_SHIFT end
    G.drawNumberCentered(lv.lv, "levellabels", x, y)
    -- Fill numbers over the base ones, tinted to the genre (song bar) colour brightened 50% (any level)
    G.drawNumberCentered(lv.lv, "levellabelsfill", x, y, lv.fillColor)
    if lv.isPlus then
        -- the "+" overlaps the last digit's top-right corner
        local px = x + G.calculateNumberWidth(lv.lv, "levellabels") / 2 + SONGBAR_PLUS_DX
        local py = y + SONGBAR_PLUS_DY
        G.bgtx["levellabels+"]:DrawAtAnchor(px, py, "center")
        local fill = G.bgtx["levellabelsfill+"]
        fill:SetColor(lv.fillColor or COL_WHITE)
        fill:DrawAtAnchor(px, py, "center")
        fill:SetColor(COL_WHITE)
    end
end

-- Level number of a song-info difficulty icon, centred at (cx,cy); a plus level's "+" overlaps the last
-- digit's top-right corner (SinfoLevel/plus.png)
local function drawInfoLevel(d, cx, cy, opacity)
    G.drawNumberCentered(d.level, "sinfo_level", cx, cy, nil, opacity)
    if d.isPlus then
        local plus = G.bgtx["sinfo_level+"]
        plus:SetOpacity(opacity)
        plus:DrawAtAnchor(cx + G.calculateNumberWidth(d.level, "sinfo_level") / 2 + SONGINFO_PLUS_DX,
            cy + SONGINFO_PLUS_DY, "center")
        plus:SetOpacity(1)
    end
end

-- The song speed notice over the metronome. The two ▶ go first, behind the text, centred on the value
-- line (its ink top to bottom); they are one shared texture drawn twice with SetColor/SetOpacity,
-- pulsing half a beat apart (the chase runs backward for a slowed-down song). The label and value are
-- glyph text tinted with the BPM colour.
local function drawSpeedNotice(sel)
    local x = SONGINFO_SPEED_ORIGIN_X - G.songSelectShift
    local tex = G.bgtx["sinfo_speed_arrow"]
    if tex ~= nil then
        local cy = SONGINFO_SPEED_VALUE_Y + G.text.LineHeight / 2
        local phase = ((G.nowMs or 0) % sel.speedBeatMs) / sel.speedBeatMs
        if sel.bpmMult < 1 then phase = 1 - phase end
        tex:SetColor(sel.bpmColor)
        for i = 0, 1 do
            local p = (phase - i * 0.5) % 1
            tex:SetOpacity(0.25 + 0.75 * (0.5 + 0.5 * math.cos(2 * math.pi * p)))
            tex:DrawAtAnchor(x + (i - 0.5) * SONGINFO_SPEED_ARROW_GAP, cy, "center")
        end
        tex:SetOpacity(1)
        tex:SetColor(COL_WHITE)
    end
    if G.speedLabel == nil then
        local ok, s = pcall(function() return THEME:GetSkinString("SONGSELECT_SONG_SPEED") end)
        G.speedLabel = SONGINFO_SPEED_LABEL
            or ((ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[") and s or "Song speed")
    end
    G.textSmall:Draw(G.speedLabel, x, SONGINFO_SPEED_LABEL_Y, sel.bpmColor, nil, 1, 1, 0, "top")
    G.text:Draw(sel.speedText, x, SONGINFO_SPEED_VALUE_Y, sel.bpmColor, nil, 1, 1, 0, "top")
end

-- Draw fav.png at x=39 y=80 relative to bar.png top-left (the favorite flag is cached in the page slot).
local function drawFavIcon(xpos, ypos)
    if G.favoriteicon == nil then return end
    local bar_tl_x = xpos - G.bars["bar"].Width  / 2
    local bar_tl_y = ypos - G.bars["bar"].Height / 2
    G.favoriteicon:Draw(bar_tl_x + 39, bar_tl_y + 80)
end

-- bl = the slot's cached { played, cs, sr } best-score info (nil when the node has no chart)
local function drawBarleft(bl, xpos, ypos)
    local barW = G.bars["bar"].Width
    local barH = G.bars["bar"].Height
    local lx   = xpos - barW / 2 + BARLEFT_X_OFFSET
    local ly   = ypos - barH / 2

    G.bars["barleft"]:Draw(lx, ly)

    if bl == nil then
        G.bars["scorerank_none"]:Draw(lx, ly)
        G.bars["clearstatus_none"]:Draw(lx, ly)
        return
    end

    local played = bl.played
    local cs     = bl.cs
    local sr     = bl.sr

    -- cs stored: 0=never played/failed, 1=assisted, 2=clear, 3=FC, 4=perfect.
    if not played then
        G.bars["clearstatus_none"]:Draw(lx, ly)
        G.bars["scorerank_none"]:Draw(lx, ly)
    elseif cs == 0 then
        -- Played but failed
        G.bars["clearstatus_m1"]:Draw(lx, ly)
        if sr == 0 then
            G.bars["scorerank_m1"]:Draw(lx, ly)
        else
            G.bars["scorerank_" .. (sr - 1)]:Draw(lx, ly)
        end
    else
        G.bars["clearstatus_" .. (cs - 1)]:Draw(lx, ly)
        if sr == 0 then
            G.bars["scorerank_m1"]:Draw(lx, ly)
        else
            G.bars["scorerank_" .. (sr - 1)]:Draw(lx, ly)
        end
    end
end

local function drawPreimage()
    local sel = G.selInfo
    if sel == nil or not sel.isSong then return end
    -- BLURRED: static in the jacket's place, never the jacket
    if sel.blurredInfo then
        local st = G.bgtx["preimage_load"]
        st:SetUseNoiseEffect(true)
        st:Draw(PREIMAGE_ORIGIN_X - G.songSelectShift, PREIMAGE_ORIGIN_Y)
        st:SetUseNoiseEffect(false)
        return
    end
    if sel.hi >= 2 then return end
    G.bgtx["preimage_load"]:Draw(PREIMAGE_ORIGIN_X - G.songSelectShift, PREIMAGE_ORIGIN_Y)
    G.bgtx["load"]:DrawAtAnchor(
        PREIMAGE_ORIGIN_X - G.songSelectShift + PREIMAGE_SIZE_X / 2,
        PREIMAGE_ORIGIN_Y + PREIMAGE_SIZE_Y / 2, "center")
    local tex = SHARED:GetSharedTexture("preimage")
    if tex.Height > 0 and tex.Width > 0 then
        -- Pop-in: scale the jacket around its centre from pop_start_scale up to 1.0 with a short overshoot
        -- when a new one appears (navigation.lua drives G.preimagePopScale). Centre-anchored so it grows in place.
        local pop = G.preimagePopScale or 1
        -- one scale for both axes: a jacket that is not square fits inside the frame, centred
        local fit = math.min(PREIMAGE_SIZE_X / tex.Width, PREIMAGE_SIZE_Y / tex.Height) * pop
        tex:SetScale(fit, fit)
        tex:DrawAtAnchor(PREIMAGE_ORIGIN_X - G.songSelectShift + PREIMAGE_SIZE_X / 2,
                         PREIMAGE_ORIGIN_Y + PREIMAGE_SIZE_Y / 2, "center")
        tex:SetScale(1, 1)
    end
end

-- ── Song-list bar drawing (shared by the normal list + the folder open/close animation) ─────

local function drawBarTitle(pt, x, y)
    G.text:Draw(pt.text, x, y, pt.gold and SONGLIST_GOLD or nil, nil, 1, 1, 525, "center")
end

-- the random bar: bar.png drawn in narrow vertical slices, each tinted the hue its place and the time give,
-- so the rainbow runs along the bar (whole pixels, so the slices meet without seams)
local function drawRainbowBar(xpos, ypos)
    local tex = G.bars["bar"]
    local w, h = tex.Width, tex.Height
    local x0, y0 = math.floor(xpos - w / 2 + 0.5), math.floor(ypos - h / 2 + 0.5)
    local shift = (G.nowMs or 0) % RAINBOW_MS / RAINBOW_MS
    for sx = 0, w - 1, RAINBOW_SLICE do
        local hue = (sx / w * RAINBOW_SPAN - shift) % 1
        tex:SetColor(RAINBOW[math.floor(hue * RAINBOW_STEPS) % RAINBOW_STEPS])
        tex:DrawRectAtAnchor(x0 + sx, y0, sx, 0, math.min(RAINBOW_SLICE, w - sx), h, "topleft")
    end
    tex:SetColor(COL_WHITE)
end

-- the black sticker in the level tag's place, with the icon of a random, back or folder bar
local function drawSticker(kind, xpos, ypos)
    local x = xpos + SONGBAR_LABEL_X_OFFSET
    G.bars["sticker"]:DrawAtAnchor(x, ypos, "center")
    G.bars["sticker_" .. kind]:DrawAtAnchor(x, ypos, "center")
end

-- Draw one song-list bar's full visuals (bar art + overlays + title + level tag) at (xpos, ypos).
local function drawBar(tx, xpos, ypos)
    if tx == nil then return end
    if tx.isSong or tx.isFolder then
        if tx.isSong and tx.vaultLocked then
            G.bars["vault_bar"]:DrawAtAnchor(xpos, ypos, "center")
            if G.bars[tx.vaultLockKey] then
                G.bars[tx.vaultLockKey]:DrawAtAnchor(xpos - G.bars["bar"].Width / 2, ypos, "left")
            end
        elseif tx.isFolder and tx.vaultFolder then
            G.bars["bar"]:SetColor(tx.boxColor)
            G.bars["bar"]:DrawAtAnchor(xpos, ypos, "center")
            G.unlocks.drawBluredStatic(xpos, ypos)
            if G.bars["vault_lockF"] then G.bars["vault_lockF"]:DrawAtAnchor(xpos, ypos, "center") end
        elseif tx.isLocked then
            if tx.lockedBarOverride then
                G.bars["bar_1"]:DrawAtAnchor(xpos, ypos, "center")
            else
                G.bars["bar"]:SetColor(tx.boxColor)
                G.bars["bar"]:DrawAtAnchor(xpos, ypos, "center")
                G.genre_overlays[tx.genre]:DrawAtAnchor(xpos, ypos, "center")
            end
            if tx.hi == 2 then
                G.unlocks.drawBluredStatic(xpos, ypos)
            else
                drawBarTitle(tx, xpos + SONGLIST_TEXT_OFFSET_X, ypos + SONGLIST_TEXT_OFFSET_Y)
            end
            if tx.lockKey and G.bars[tx.lockKey] then
                G.bars[tx.lockKey]:DrawAtAnchor(xpos - G.bars["bar"].Width / 2, ypos, "left")
            end
            if tx.isSong and tx.hi == 0 then
                drawLevelTag(tx.level, xpos + SONGBAR_LABEL_X_OFFSET, ypos)
            end
        else
            G.bars["bar"]:SetColor(tx.boxColor)
            G.bars["bar"]:DrawAtAnchor(xpos, ypos, "center")
            G.genre_overlays[tx.genre]:DrawAtAnchor(xpos, ypos, "center")
            if tx.fav then drawFavIcon(xpos, ypos) end
            if tx.isSong then drawBarleft(tx.barleft, xpos, ypos) end
            if tx.isFolder then drawSticker("folder", xpos, ypos) end
            drawLevelTag(tx.level, xpos + SONGBAR_LABEL_X_OFFSET, ypos)
            drawBarTitle(tx, xpos + SONGLIST_TEXT_OFFSET_X, ypos + SONGLIST_TEXT_OFFSET_Y)
        end
    elseif tx.isRandom or tx.isReturn then
        if tx.isRandom then
            drawRainbowBar(xpos, ypos)
        else
            G.bars["bar"]:SetColor(BACK_BAR_COLOR)
            G.bars["bar"]:DrawAtAnchor(xpos, ypos, "center")
        end
        drawSticker(tx.isRandom and "random" or "back", xpos, ypos)
        drawBarTitle(tx, xpos + SONGLIST_TEXT_OFFSET_X, ypos + SONGLIST_TEXT_OFFSET_Y)
    end
end
M.drawBar = drawBar

-- Rest position of the bar at page offset i (dist = the in-flight scroll amount, 0 at rest).
function M.barPos(i, dist)
    dist = dist or 0
    local xpos = SONGLIST_ORIGIN_X + (i + dist) * SONGLIST_OFFSET_X - G.songSelectShift
    local ypos = SONGLIST_ORIGIN_Y + (i + dist) * SONGLIST_OFFSET_Y
    if i == 0 then xpos = xpos + SONGLIST_SELECTED_X_DIFF end
    return xpos, ypos
end

-- ── The shortcuts helper line: white glyph text on a rounded dark box sized to it ─────────
local helperBox = nil          -- { canvas, w, h, text }: rebaked when the text changes (language, binding)
local function drawHelperLine(text, opacity)
    local gf = G.textStats
    local inkH = math.ceil(gf.LineHeight)
    local pad = gf.BoxHeight - inkH            -- the glyph box's padding around the ink
    if helperBox == nil or helperBox.text ~= text then
        if helperBox ~= nil then helperBox.canvas:Dispose() end
        local w = math.ceil(gf:Measure(text)) + 2 * HELPER_PAD_X
        local h = inkH + 2 * HELPER_PAD_Y
        local cv = CANVAS:CreateCanvas(w, h)
        cv:ClearTransparent()
        local r = math.max(0, math.min(HELPER_BOX_RADIUS, math.floor(h / 2)))
        cv:FillRect(r, 0, w - 2 * r, h, 0, 0, 0, HELPER_BOX_ALPHA)
        cv:FillRect(0, r, w, h - 2 * r, 0, 0, 0, HELPER_BOX_ALPHA)
        for _, c in ipairs({ { r, r }, { w - r - 1, r }, { r, h - r - 1 }, { w - r - 1, h - r - 1 } }) do
            cv:FillCircle(c[1], c[2], r, 0, 0, 0, HELPER_BOX_ALPHA)
        end
        cv:Upload()
        helperBox = { canvas = cv, w = w, h = h, text = text }
    end
    helperBox.canvas:SetOpacity(opacity)
    helperBox.canvas:Draw(HELPER_X, HELPER_Y)
    helperBox.canvas:SetOpacity(1)
    gf:Draw(text, HELPER_X + HELPER_PAD_X - pad, HELPER_Y + HELPER_PAD_Y, COL_WHITE, COL_BLACK, opacity, 1, 0, "topleft")
end

-- ── The song list backdrop: a translucent grey band behind the bars, along their diagonal ──
-- Baked once as a screen-high canvas: every row is the band shifted by the bars' slope (offset_x per
-- offset_y), so its edges are the list's own diagonal; the edges fade over BACKDROP_FADE px in 4 px steps
-- (0 = a clean hard edge).
local backdrop = nil           -- { canvas, x0 }: x0 = the canvas' screen x before the panel shift
local function drawListBackdrop(opacity)
    if backdrop == nil then
        local slope = SONGLIST_OFFSET_X / SONGLIST_OFFSET_Y
        local W, H = math.floor(BACKDROP_W), 1080
        local span = math.ceil(H * slope)
        local cv = CANVAS:CreateCanvas(W + span + 2, H)
        cv:ClearTransparent()
        local fade = math.max(0, math.min(math.floor(BACKDROP_FADE), math.floor(W / 4)))
        local step = 4
        local cr, cg, cb = BACKDROP_COLOR[1] or 0, BACKDROP_COLOR[2] or 0, BACKDROP_COLOR[3] or 0
        -- a screen blend reads the colour alone: the alpha goes into the colour, on solid pixels
        local screen = BACKDROP_BLEND == "screen"
        -- a multiply reads the colour times the alpha, next to the alpha
        local multi = BACKDROP_BLEND == "multi"
        local function fill(x, y, w, a)
            if screen or multi then
                local k = a / 255
                cv:FillRect(x, y, w, 1, math.floor(cr * k + 0.5), math.floor(cg * k + 0.5), math.floor(cb * k + 0.5), multi and a or 255)
            else
                cv:FillRect(x, y, w, 1, cr, cg, cb, a)
            end
        end
        for y = 0, H - 1 do
            if fade == 0 then
                -- a hard edge: the pixel each edge crosses is filled by the share the band covers
                local xf = y * slope
                local x = math.floor(xf)
                local part = xf - x
                fill(x, y, 1, math.floor(BACKDROP_ALPHA * (1 - part) + 0.5))
                fill(x + 1, y, W - 1, BACKDROP_ALPHA)
                fill(x + W, y, 1, math.floor(BACKDROP_ALPHA * part + 0.5))
            end
            local x = math.floor(y * slope + 0.5)
            if fade > 0 then fill(x + fade, y, W - 2 * fade, BACKDROP_ALPHA) end
            for f = 0, fade - 1, step do
                local a = math.floor(BACKDROP_ALPHA * (f + step) / (fade + step) + 0.5)
                local wdt = math.min(step, fade - f)
                fill(x + f, y, wdt, a)
                fill(x + W - f - wdt, y, wdt, a)
            end
        end
        cv:Upload()
        -- the band is centred on the bar centre line (BACKDROP_DX to its right): row 0's left edge
        backdrop = { canvas = cv, x0 = SONGLIST_ORIGIN_X + BACKDROP_DX - SONGLIST_ORIGIN_Y * slope - W / 2 }
    end
    local cv = backdrop.canvas
    if BACKDROP_BLEND == "screen" or BACKDROP_BLEND == "multi" then
        cv:SetBlendMode(BACKDROP_BLEND)
        cv:SetColor(opacity, opacity, opacity)
        if BACKDROP_BLEND == "multi" then cv:SetOpacity(opacity) end
        cv:Draw(math.floor(backdrop.x0 - G.songSelectShift + 0.5), 0)
        cv:SetOpacity(1)
        cv:SetColor(1, 1, 1)
        cv:SetBlendMode("normal")
        return
    end
    cv:SetOpacity(opacity)
    cv:Draw(math.floor(backdrop.x0 - G.songSelectShift + 0.5), 0)
    cv:SetOpacity(1)
end

-- the list slot (-5..5) whose bar plate is under (mx, my), or nil. The plate is the slanted strip inside
-- the bar texture (765x130: opaque rows 17..116, its left edge sliding 0.34 px right per row).
local BAR_PLATE = { top = 17, bottom = 116, left = 20, width = 690, skew = 0.34 }
function M.hitTest(mx, my)
    local tex = G.bars["bar"]
    local hw, hh = tex.Width / 2, tex.Height / 2
    for i = -5, 5 do
        local cx, cy = M.barPos(i, G.selectBoxDist)
        local row = my - (cy - hh)
        if row >= BAR_PLATE.top and row <= BAR_PLATE.bottom then
            local left = cx - hw + BAR_PLATE.left + (row - BAR_PLATE.top) * BAR_PLATE.skew
            if mx >= left and mx <= left + BAR_PLATE.width then return i end
        end
    end
    return nil
end

-- ── The selected row: a segment of the band at the centre bar's height, its colour bouncing in and out ──
-- Baked once like the backdrop (same width, slope and edges), as high as the bar plate plus half the gap to
-- the next bar above and below (one row of the list); G.selectBandPulse
-- (0..1, the "selectband_animation" counter) scales its alpha.
local SELECT_BAND_COLOR = CFG.numList("song_list.select_band_color", { 72, 210, 104 })
-- green at the top level; inside a folder, G.selectBandColor (navigation.lua: the folder's colour, moved
-- away from its bars')
local SELECT_BAND_TOP = COLOR:CreateColorFromARGB(255, SELECT_BAND_COLOR[1] or 0, SELECT_BAND_COLOR[2] or 0,
    SELECT_BAND_COLOR[3] or 0)
local SELECT_BAND_ALPHA = CFG.num("song_list.select_band_alpha", 170)
local selectBand = nil         -- { canvas, dx, y }: dx = its offset from the band's canvas x, y = its screen row
local function drawSelectBand(opacity)
    local a = opacity * (G.selectBandPulse or 0) * SELECT_BAND_ALPHA / 255
    if a <= 0 then return end
    local slope = SONGLIST_OFFSET_X / SONGLIST_OFFSET_Y
    local W = math.floor(BACKDROP_W)
    if selectBand == nil then
        local plateH = BAR_PLATE.bottom - BAR_PLATE.top + 1
        local gap = math.max(0, math.floor((SONGLIST_OFFSET_Y - plateH) / 2))
        local top = math.floor(SONGLIST_ORIGIN_Y - G.bars["bar"].Height / 2 + BAR_PLATE.top) - gap
        local H = plateH + 2 * gap
        local dx = math.floor(top * slope)
        local cv = CANVAS:CreateCanvas(W + math.ceil(H * slope) + 2, H)
        cv:ClearTransparent()
        local fade = math.max(0, math.min(math.floor(BACKDROP_FADE), math.floor(W / 4)))
        local step = 4
        local function fill(x, y, w, al) cv:FillRect(x, y, w, 1, 255, 255, 255, al) end
        for r = 0, H - 1 do
            -- the band's own row top + r, so both edges line up with the backdrop's
            local xf = (top + r) * slope - dx
            if fade == 0 then
                local x = math.floor(xf)
                local part = xf - x
                fill(x, r, 1, math.floor(255 * (1 - part) + 0.5))
                fill(x + 1, r, W - 1, 255)
                fill(x + W, r, 1, math.floor(255 * part + 0.5))
            else
                local x = math.floor(xf + 0.5)
                fill(x + fade, r, W - 2 * fade, 255)
                for f = 0, fade - 1, step do
                    local al = math.floor(255 * (f + step) / (fade + step) + 0.5)
                    local wdt = math.min(step, fade - f)
                    fill(x + f, r, wdt, al)
                    fill(x + W - f - wdt, r, wdt, al)
                end
            end
        end
        cv:Upload()
        selectBand = { canvas = cv, dx = dx, y = top }
    end
    local x0 = SONGLIST_ORIGIN_X + BACKDROP_DX - SONGLIST_ORIGIN_Y * slope - W / 2
    local cv = selectBand.canvas
    cv:SetColor(G.selectBandColor or SELECT_BAND_TOP)
    cv:SetOpacity(a)
    cv:Draw(math.floor(x0 - G.songSelectShift + 0.5) + selectBand.dx, selectBand.y)
    cv:SetOpacity(1)
    cv:SetColor(COL_WHITE)
end

-- ── Folder open/close animation ─────────────────────────────────────────────────
-- Driven by G.folderAnim (set up in navigation.lua): { mode="open"/"close", phase=1/2, t=0..1,
-- oldBars/newBars = { {i, pt}, ... } sorted outer-first, folderPt = the folder's own bar }.
-- t is the phase counter's value, already EASED by the counter (navigation sets SetEasing per phase),
-- so drawing is a plain lerp between the bars' rest slots (M.barPos) and the off-screen / centre point.
local ANIM_OFFLEFT_X = CFG.num("folder_anim.offscreen_left_x", -1000)  -- where bars slide out to / in from
local function lerp(a, b, t) return a + (b - a) * t end

local function drawFolderAnim()
    local fa = G.folderAnim
    local cx, cy = M.barPos(0, 0)
    local t = fa.t or 0
    if fa.mode == "open" then
        if fa.phase == 1 then
            -- slide the parent siblings out to the left; the opened folder bar stays centred
            for _, b in ipairs(fa.oldBars) do
                local bx, by = M.barPos(b.i, 0)
                drawBar(b.pt, lerp(bx, ANIM_OFFLEFT_X, t), by)
            end
            drawBar(fa.folderPt, cx, cy)
        else
            -- fan the folder's contents out from the centre into their slots ("opening a book")
            for _, b in ipairs(fa.newBars) do
                local bx, by = M.barPos(b.i, 0)
                drawBar(b.pt, lerp(cx, bx, t), lerp(cy, by, t))
            end
        end
    else
        if fa.phase == 1 then
            -- group the folder's contents back toward the focused spot
            for _, b in ipairs(fa.oldBars) do
                local bx, by = M.barPos(b.i, 0)
                drawBar(b.pt, lerp(bx, cx, t), lerp(by, cy, t))
            end
        else
            -- the folder bar sits at the focus; the parent siblings slide back in from the left
            drawBar(fa.folderPt, cx, cy)
            for _, b in ipairs(fa.newBars) do
                local bx, by = M.barPos(b.i, 0)
                drawBar(b.pt, lerp(ANIM_OFFLEFT_X, bx, t), by)
            end
        end
    end
end
M.drawFolderAnim = drawFolderAnim

-- ── Header breadcrumb (shared by the normal draw + the folder open/close animation) ──
-- Draw one crumb: its box + title, right edge at xRight, with alpha; plus its connecting arrow (to the left).
local function drawCrumb(title, xRight, alpha, drawArrow)
    local boxW = G.bgtx["header-box"].Width
    local boxH = G.bgtx["header-box"].Height
    G.bgtx["header-box"]:SetOpacity(alpha)
    G.bgtx["header-box"]:DrawAtAnchor(xRight, 0, "topright")
    G.text:Draw(title, xRight - boxW + HEADER_BOX_TEXT_OFFSET_X,
        HEADER_BOX_TEXT_OFFSET_Y + boxH / 2, nil, nil, alpha, 1, 270, "center")
    if drawArrow then
        G.bgtx["header-arrow"]:SetOpacity(alpha)
        G.bgtx["header-arrow"]:DrawAtAnchor(xRight - boxW, 0, "topright")
    end
    G.bgtx["header-box"]:SetOpacity(1)
    G.bgtx["header-arrow"]:SetOpacity(1)
end

-- Header path animation, synced to G.folderAnim (same mode/phase/t as the song-list animation).
-- Opening: existing crumbs slide left (phase 1), the new folder crumb fades in (phase 2).
-- Closing: the innermost crumb fades out (phase 1), the remaining crumbs slide right (phase 2).
local function drawHeaderCrumbsAnim(opacityNorm)
    local fa  = G.folderAnim
    local old = fa.oldCrumbs or {}
    local new = (G.selInfo and G.selInfo.crumbs) or {}
    local SLOT = G.bgtx["header-box"].Width + G.bgtx["header-arrow"].Width
    local X0   = HEADER_OFFSET_X - G.songSelectShift
    local t    = fa.t or 0
    local function slotX(s) return X0 - s * SLOT end

    if fa.mode == "open" then
        local N = #old                     -- new path = { opened folder } ++ old
        for j = 1, N do                    -- existing crumbs move from slot (j-1) to slot j
            local x = (fa.phase == 1) and slotX(lerp(j - 1, j, t)) or slotX(j)
            drawCrumb(old[j], x, opacityNorm, j < N)
        end
        if fa.phase == 2 and new[1] ~= nil then
            drawCrumb(new[1], slotX(0), opacityNorm * t, #new > 1)   -- new folder crumb fades in
        end
    else
        local N = #old                     -- old path = { closed folder } ++ new
        for k = 2, N do                    -- remaining crumbs slide from slot (k-1) to slot (k-2)
            local x = (fa.phase == 1) and slotX(k - 1) or slotX(lerp(k - 1, k - 2, t))
            drawCrumb(old[k], x, opacityNorm, k < N)
        end
        if fa.phase == 1 and old[1] ~= nil then
            drawCrumb(old[1], slotX(0), opacityNorm * (1 - t), N > 1)   -- innermost crumb fades out
        end
    end
end
M.drawHeaderCrumbsAnim = drawHeaderCrumbsAnim

-- ── Draw panel ────────────────────────────────────────────────────────────────

function M.drawPanel()
    local opacityNorm = G.songSelectElemOpacity / 255
    local sel = G.selInfo

    -- the translucent band the bars sit on: under everything else of the panel, the song info included
    if opacityNorm > 0 then drawListBackdrop(opacityNorm) end
    if G.folderAnim == nil and G.pageTexts ~= nil then drawSelectBand(opacityNorm) end

    -- Random / song info panels (all node-derived data comes from the selection cache — see navigation.lua)
    if sel ~= nil and sel.isRandom then
        G.bgtx["randominfo"]:DrawAtAnchor(1920 - G.songSelectShift, 0, "topright")
    end

    -- folder info (placeholder): its name, its box.def description, and its unlocked / total songs
    if sel ~= nil and sel.isFolder and not sel.vaultFolder then
        local cx = FOLDERINFO_CENTER_X - G.songSelectShift
        local panel = G.bgtx["folderinfo"]
        panel:SetColor(sel.folderColor or COL_WHITE)
        panel:DrawAtAnchor(cx, FOLDERINFO_PANEL_Y, "top")
        panel:SetColor(COL_WHITE)
        G.textLarge:Draw(sel.folderName or "", cx, FOLDERINFO_NAME_Y, nil, nil, 1, 1, FOLDERINFO_WIDTH, "top")
        for i, line in ipairs(sel.folderLines) do
            G.text:Draw(line, cx, FOLDERINFO_TEXT_Y + (i - 1) * FOLDERINFO_LINE_GAP, nil, nil, 1, 1, FOLDERINFO_WIDTH, "top")
        end
        if G.folderSongsLabel == nil then
            local ok, s = pcall(function() return THEME:GetSkinString("SONGSELECT_FOLDER_SONGS") end)
            G.folderSongsLabel = (ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[") and s or "Songs"
        end
        G.textStats:Draw(G.folderSongsLabel, cx, FOLDERINFO_COUNT_Y, nil, nil, 1, 1, FOLDERINFO_WIDTH, "top")
        G.textLarge:Draw(sel.folderCount, cx, FOLDERINFO_COUNT_Y + FOLDERINFO_COUNT_GAP, nil, nil, 1, 1, FOLDERINFO_WIDTH, "top")
    end

    if sel ~= nil and sel.isSong then
        -- vault locked: no song info; BLURRED: drawn darker, its texts as ???
        local hide = sel.blurredInfo
        if sel.hi < 2 or hide then
            local tint = hide and BLURRED_INFO_TINT or COL_WHITE
            G.bgtx["songinfo"]:SetColor(tint)
            G.bgtx["songinfo"]:DrawAtAnchor(1920 - G.songSelectShift, 0, "topright")
            G.bgtx["songinfo"]:SetColor(COL_WHITE)
            if sel.hasVideo then
                G.bgtx["sinfo_video"]:SetColor(tint)
                G.bgtx["sinfo_video"]:Draw(SONGINFO_HASVIDEO_ORIGIN_X - G.songSelectShift, SONGINFO_HASVIDEO_ORIGIN_Y)
                G.bgtx["sinfo_video"]:SetColor(COL_WHITE)
            end
            if sel.explicit then
                G.bgtx["sinfo_explicit"]:SetColor(tint)
                G.bgtx["sinfo_explicit"]:DrawAtAnchor(
                    SONGINFO_EXPLICIT_ORIGIN_X - G.songSelectShift, SONGINFO_EXPLICIT_ORIGIN_Y, "topright")
                G.bgtx["sinfo_explicit"]:SetColor(COL_WHITE)
            end

            -- Difficulty icons: hidden for GRAYED and above
            if sel.hi == 0 then
                local isVaultSong = sel.isVault
                local has3, has4 = sel.diffs[3] ~= nil, sel.diffs[4] ~= nil
                for i = 0, 4 do
                    local d     = sel.diffs[i]
                    local xpos  = SONGINFO_DIFFICULTIES_ORIGIN_X - G.songSelectShift
                    local ypos  = SONGINFO_DIFFICULTIES_ORIGIN_Y + SONGINFO_DIFFICULTIES_GAP_Y * math.min(i, 3)
                    if has3 and i == 4 then
                        if d ~= nil then
                            local difftx = isVaultSong and G.bgtx["sinfo_difficulties_vault"] or G.bgtx["sinfo_difficulties_4"]
                            difftx:SetOpacity(G.difficultyFade4 / 255)
                            difftx:Draw(xpos, ypos)
                            difftx:SetOpacity(1)
                            drawInfoLevel(d, xpos + difftx.Width / 2, ypos + difftx.Height / 2, G.difficultyFade4 / 255)
                        end
                    elseif d == nil then
                        -- Vault songs: never show the "missing" indicator
                        if not isVaultSong and (not has4 or i ~= 3) then
                            G.bgtx["sinfo_difficulties_missing"]:Draw(xpos, ypos)
                        end
                    else
                        local difftx = isVaultSong and G.bgtx["sinfo_difficulties_vault"] or G.bgtx["sinfo_difficulties_" .. i]
                        difftx:Draw(xpos, ypos)
                        drawInfoLevel(d, xpos + difftx.Width / 2, ypos + difftx.Height / 2, 1)
                    end
                end
            end

            G.textSmall:Draw(hide and "???" or sel.subtitle, SONGINFO_SUBTITLE_ORIGIN_X - G.songSelectShift, SONGINFO_SUBTITLE_ORIGIN_Y,
                nil, nil, 1, 1, SONGINFO_SUBTITLE_MWIDTH, "center")
            G.textSmall:Draw(hide and "???" or sel.charter, SONGINFO_CHARTER_ORIGIN_X - G.songSelectShift, SONGINFO_CHARTER_ORIGIN_Y,
                nil, nil, 1, 1, SONGINFO_CHARTER_MWIDTH)
            -- rebuild the length string only when the async preview load lands a new duration
            if sel.lenMs ~= G.previewDurationMs then
                sel.lenMs   = G.previewDurationMs
                sel.lenText = "Length - " .. formatDuration(sel.lenMs)
            end
            G.textSmall:Draw(hide and "???" or sel.lenText, SONGINFO_LENGTH_ORIGIN_X - G.songSelectShift, SONGINFO_LENGTH_ORIGIN_Y,
                nil, nil, 1, 1, SONGINFO_LENGTH_MWIDTH)

            if hide then
                G.text:Draw("???", SONGINFO_BPM_ORIGIN_X - G.songSelectShift, SONGINFO_BPM_ORIGIN_Y,
                    COL_WHITE, nil, 1, 1, SONGINFO_BPM_MWIDTH, "center", 0, SONGINFO_BPM_ROTATION)
            elseif sel.bpmBase ~= nil then
                -- rebuild the BPM string/color only when the song-speed multiplier changes
                local mult = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
                if sel.bpmMult ~= mult then
                    sel.bpmMult = mult
                    local bpmText = formatNumber(sel.bpmBase * mult, 3)
                    if sel.bpmBase ~= sel.bpmMin or sel.bpmBase ~= sel.bpmMax then
                        bpmText = bpmText .. " (" .. formatNumber(sel.bpmMin * mult, 3)
                               .. "-" .. formatNumber(sel.bpmMax * mult, 3) .. ")"
                    end
                    sel.bpmText  = bpmText
                    sel.bpmColor = (mult < 1) and COL_BPM_SLOW or (mult > 1) and COL_BPM_FAST or COL_WHITE
                    sel.speedText = (mult ~= 1) and string.format("x%.2f", mult) or nil
                    -- one beat of the sped-up song, in ms (clamped so extreme BPMs still read as a pulse)
                    local bpm = (sel.bpmBase or 0) * mult
                    sel.speedBeatMs = (bpm > 0) and math.max(150, math.min(2000, 60000 / bpm)) or 600
                end
                G.text:Draw(sel.bpmText, SONGINFO_BPM_ORIGIN_X - G.songSelectShift, SONGINFO_BPM_ORIGIN_Y,
                    sel.bpmColor, nil, 1, 1, SONGINFO_BPM_MWIDTH, "center", 0, SONGINFO_BPM_ROTATION)
                if sel.speedText then drawSpeedNotice(sel) end
            end
        end
    end

    drawPreimage()

    -- Song list bars. While a folder is opening/closing, the animation owns the list (and hides the
    -- selected row's band + arrows); otherwise draw the normal diagonal list.
    if G.folderAnim ~= nil then
        drawFolderAnim()
    elseif G.pageTexts ~= nil then
        for i, tx in pairs(G.pageTexts) do
            local xpos, ypos = M.barPos(i, G.selectBoxDist)
            drawBar(tx, xpos, ypos)
        end

        -- the animated arrows around the selected bar
        local x0     = SONGLIST_ORIGIN_X + SONGLIST_SELECTED_X_DIFF - G.songSelectShift
        local y0     = SONGLIST_ORIGIN_Y
        local ax     = G.arrowsDistance
        local xlshift = ax * math.cos(7 * math.pi / 12)
        local ylshift = ax * math.sin(7 * math.pi / 12)
        local isUnlockedSong = sel ~= nil and sel.isSong and sel.isUnlockedSong
        -- Left arrow shifts 67px further left when an unlocked song is selected to clear barleft
        local arrowLOffset = isUnlockedSong and BARLEFT_X_OFFSET or 0
        G.bars["selected-arrow-l"]:DrawAtAnchor(x0 - SONGLIST_SELECTED_ARROW_GAP/2 + xlshift + arrowLOffset, y0 - ylshift, "left")
        G.bars["selected-arrow-r"]:DrawAtAnchor(x0 + SONGLIST_SELECTED_ARROW_GAP/2 - xlshift, y0 + ylshift, "right")
    end

    -- Header breadcrumb. Header alpha eases 100%→HEADER_DIFF_ALPHA across songselect→diffselect (matches diffselect).
    local headerAlpha = 1.0 - (1.0 - HEADER_DIFF_ALPHA) * math.min(1, G.songSelectShift / 1920)
    G.bgtx["header"]:SetOpacity(headerAlpha)
    G.bgtx["header"]:Draw(-G.songSelectShift, 0)
    G.bgtx["header"]:SetOpacity(1)
    -- Breadcrumbs: the folder open/close animation drives them (in sync with the list) while it runs.
    if G.folderAnim ~= nil then
        drawHeaderCrumbsAnim(opacityNorm)
    elseif sel ~= nil then
        local SLOT = G.bgtx["header-box"].Width + G.bgtx["header-arrow"].Width
        local X0   = HEADER_OFFSET_X - G.songSelectShift
        for i, title in ipairs(sel.crumbs) do
            drawCrumb(title, X0 - (i - 1) * SLOT, opacityNorm, i ~= #sel.crumbs)
        end
    end

    -- Overlay, then the shortcuts helper line under it (song select only: it fades with the panel)
    G.bgtx["overlay"]:SetOpacity(opacityNorm)
    G.bgtx["overlay"]:Draw(0, 0)
    local helper = G.shortcuts and G.shortcuts.helperText()
    if helper ~= nil and opacityNorm > 0 then
        local pulse = 0.5 + 0.5 * math.sin(2 * math.pi * (G.nowMs or 0) / HELPER_PULSE_MS)
        drawHelperLine(helper, opacityNorm * (HELPER_OPACITY_MIN + (1 - HELPER_OPACITY_MIN) * pulse))
    end

    -- Unlock conditions panel (shown when a locked song is highlighted)
    G.unlocks.drawCondsPanel()
    G.unlocks.drawVaultCondsPanel()

    -- Nameplates (the online lobby shows its players itself)
    if G.activeConfig.songOnly then return end
    local playerCount = CONFIG.PlayerCount
    G.highlightedPlayer = G.highlightedPlayer % playerCount

    G.bgtx["nameplate_info"]:SetOpacity(opacityNorm)
    do
        local x0       = NAMEPLATE_BOX_START_X
        local y0       = 1080 - NAMEPLATE_BOX_FOLDED_SIZE_Y
        local ssCharaX = x0 + G.bgtx["nameplate_info"].Width / 2
        G.bgtx["nameplate_info"]:Draw(x0, y0)
        G.drawPlayerChara(G.highlightedPlayer, ssCharaX, y0 + NAMEPLATE_OFFSET_Y, 1, 1, opacityNorm, false)
        G.drawPlayerPuchi(G.highlightedPlayer, ssCharaX - PUCHI_OFFSET_X, y0 + NAMEPLATE_OFFSET_Y + G.puchiSineY, 1, 1, opacityNorm, false, G.puchiIdxFrame)
        NAMEPLATE:DrawPlayerNameplate(x0 + NAMEPLATE_OFFSET_X, y0 + NAMEPLATE_OFFSET_Y, G.songSelectElemOpacity, G.highlightedPlayer)

        -- Perfect / FC / Clear counts for the highlighted player at the displayed difficulty
        -- (queried only when the player/course key changes — the counts are save-file scans)
        if G.textStats ~= nil then
            local diff = math.min(4, CONFIG:GetDefaultCourse(0))
            local sc = statsCache
            if sc.player ~= G.highlightedPlayer or sc.diff ~= diff then
                local sav = GetSaveFile(G.highlightedPlayer)
                sc.player, sc.diff = G.highlightedPlayer, diff
                sc.perfect = tostring(sav:GetClearStatusCount(diff, 4))
                sc.fc      = tostring(sav:GetClearStatusCount(diff, 3))
                sc.clear   = tostring(sav:GetClearStatusCount(diff, 2))
            end
            local function drawStat(n, x)
                G.textStats:Draw(n, x, 1058, COL_WHITE, nil, opacityNorm, 1, 0, "center")
            end
            drawStat(sc.perfect, 95)
            drawStat(sc.fc,     212)
            drawStat(sc.clear,  329)
        end
    end

    for i = 1, playerCount - 1 do
        local j    = i
        if j - 1 >= G.highlightedPlayer then j = j + 1 end
        local xpos    = NAMEPLATE_BOX_START_X + i * NAMEPLATE_BOX_SPACING_X
        local ypos    = 1080 - NAMEPLATE_SECONDARY_OFFSET_Y
        local portCx  = xpos + G.bgtx["nameplate_info"].Width / 2
        G.bgtx["placeholder_portrait"]:SetOpacity(opacityNorm)
        G.bgtx["placeholder_portrait"]:DrawAtAnchor(portCx, ypos, "bottom")
        G.bgtx["placeholder_portrait"]:SetOpacity(1)
        -- Draw Portrait.png over the placeholder if it loaded for this player slot
        local portrait = G.portraits ~= nil and G.portraits[j - 1]
        if portrait ~= nil and portrait.Loaded then
            local gm = CHARACTER:GetPlayerGradientMap(j - 1)
            if gm ~= nil then GRADIENT:SetActive(gm) end
            portrait:SetOpacity(opacityNorm)
            portrait:DrawAtAnchor(portCx, ypos, "bottom")
            portrait:SetOpacity(1)
            if gm ~= nil then GRADIENT:ClearActive() end
        end
        NAMEPLATE:DrawPlayerNameplate(xpos + NAMEPLATE_OFFSET_X, ypos, G.songSelectElemOpacity, j - 1)
    end
end

return M
