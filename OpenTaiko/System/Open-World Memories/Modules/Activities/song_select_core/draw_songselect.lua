---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- song select panel drawing (song_select_core)

local Config    = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local List      = Layout.song_list
local ListAnim  = Animation.song_list
local Info      = Layout.song_info
local Colors    = Layout.colors

local M = {}
-- hoisted colors (COLOR:Create* allocates per call)
local SONGLIST_GOLD  = COLOR:CreateColorFromHex(Colors.songlist_gold)   -- selected-bar title tint
local COL_WHITE      = COLOR:CreateColorFromHex("ffffffff")
local COL_BPM_SLOW   = COLOR:CreateColorFromHex(Colors.bpm_slow)
local COL_BPM_FAST   = COLOR:CreateColorFromHex(Colors.bpm_fast)

local statsCache = { player = -1, diff = -1 }   -- clear counts scan the save file; keyed on (player, course)
local G   -- shared state from Script.lua

-- ==== Layout ====

local SONGLIST_ORIGIN_X           = List.origin_x
local SONGLIST_ORIGIN_Y           = List.origin_y
local SONGLIST_OFFSET_X           = List.offset_x
local SONGLIST_OFFSET_Y           = List.offset_y
local SONGLIST_ROWS               = List.rows_each_side
local SONGLIST_TEXT_OFFSET_X      = List.text_offset_x
local SONGLIST_TEXT_OFFSET_Y      = List.text_offset_y
local SONGLIST_TITLE_MWIDTH       = List.title_max_width
local SONGLIST_SELECTED_X_DIFF    = List.selected_x_diff
local SONGLIST_SELECTED_ARROW_GAP = List.selected_arrow_gap
local SONGBAR_LABEL_X_OFFSET      = List.label_x_offset
local FAV_ICON_X, FAV_ICON_Y      = List.fav_icon_x, List.fav_icon_y
-- random bar rainbow: RAINBOW_STEPS hues made once, one pass per RAINBOW_MS
local BACK_BAR_COLOR = COLOR:CreateColorFromHex(Colors.back_bar)
-- song info tint for a BLURRED song
local BLURRED_INFO_TINT = COLOR:CreateColorFromHex(Colors.blurred_info)
local RAINBOW_MS     = ListAnim.random_rainbow_ms
local RAINBOW_STEPS  = List.random_rainbow_steps
local RAINBOW_SLICE  = List.random_rainbow_slice   -- px per slice
local RAINBOW_SPAN   = List.random_rainbow_span    -- hue turns across the bar
local RAINBOW = {}
for i = 0, RAINBOW_STEPS - 1 do
    local h, s, v = i / RAINBOW_STEPS * 6, List.random_rainbow_saturation, List.random_rainbow_value
    local c = v * s
    local x = c * (1 - math.abs(h % 2 - 1))
    local r, g, b = 0, 0, 0
    if h < 1 then r, g = c, x elseif h < 2 then r, g = x, c elseif h < 3 then g, b = c, x
    elseif h < 4 then g, b = x, c elseif h < 5 then r, b = x, c else r, b = c, x end
    local m = v - c
    RAINBOW[i] = COLOR:CreateColorFromARGB(255, math.floor((r + m) * 255 + 0.5), math.floor((g + m) * 255 + 0.5),
        math.floor((b + m) * 255 + 0.5))
end
-- plus centre, from the digits' right cell edge and centre line (BarLevel/plus.png)
local SONGBAR_PLUS_DX             = List.plus_dx
local SONGBAR_PLUS_DY             = List.plus_dy
-- number shift left to stay centred with the plus
local SONGBAR_PLUS_SHIFT          = List.plus_shift
-- shortcuts helper line: position and opacity pulse
local HELPER_X           = List.shortcuts_helper_x
local HELPER_Y           = List.shortcuts_helper_y
local HELPER_PULSE_MS    = ListAnim.shortcuts_helper_pulse_ms
local HELPER_OPACITY_MIN = ListAnim.shortcuts_helper_opacity_min
local HELPER_PAD_X       = List.shortcuts_helper_pad_x       -- box padding
local HELPER_PAD_Y       = List.shortcuts_helper_pad_y
local HELPER_BOX_ALPHA   = List.shortcuts_helper_box_alpha
local HELPER_BOX_RADIUS  = List.shortcuts_helper_box_radius
local HELPER_BOX_COLOR   = List.shortcuts_helper_box_color
local HELPER_TEXT_COLOR    = Config.color(List.shortcuts_helper_text_color)
local HELPER_OUTLINE_COLOR = Config.color(List.shortcuts_helper_outline_color)
-- list backdrop band, sheared to the bars' diagonal
local BACKDROP_W         = List.backdrop_width
local BACKDROP_DX        = List.backdrop_offset_x
local BACKDROP_COLOR     = List.backdrop_color
local BACKDROP_ALPHA     = List.backdrop_alpha
local BACKDROP_FADE      = List.backdrop_fade
local BACKDROP_FADE_STEP = List.backdrop_fade_step
-- blend: "normal" (over at alpha), "screen" (lighten only), "multi" (multiply, alpha = amount)
local BACKDROP_BLEND     = List.backdrop_blend
-- min level for the checker tag per diff (bar_levelbgchecker<diff>.png)
local SONGBAR_CHECKER_LEVEL       = {
    [0] = List.checker_level_easy,
    [1] = List.checker_level_normal,
    [2] = List.checker_level_hard,
}

-- folder info (bg_folderinfo.png, folder colour tint)
local FolderInfo = Layout.folder_info
local FOLDERINFO_CENTER_X  = FolderInfo.center_x
local FOLDERINFO_PANEL_Y   = FolderInfo.panel_y
local FOLDERINFO_WIDTH     = FolderInfo.width
local FOLDERINFO_NAME_Y    = FolderInfo.name_y
local FOLDERINFO_TEXT_Y    = FolderInfo.text_y
local FOLDERINFO_LINE_GAP  = FolderInfo.line_gap
local FOLDERINFO_COUNT_Y   = FolderInfo.count_y
local FOLDERINFO_COUNT_GAP = FolderInfo.count_gap

local SONGINFO_DIFFICULTIES_ORIGIN_X = Info.difficulties_origin_x
local SONGINFO_DIFFICULTIES_ORIGIN_Y = Info.difficulties_origin_y
local SONGINFO_DIFFICULTIES_GAP_Y    = Info.difficulties_gap_y
local SONGINFO_PLUS_DX               = Info.plus_dx
local SONGINFO_PLUS_DY               = Info.plus_dy
local SONGINFO_HASVIDEO_ORIGIN_X     = Info.has_video_origin_x
local SONGINFO_HASVIDEO_ORIGIN_Y     = Info.has_video_origin_y
local SONGINFO_EXPLICIT_ORIGIN_X     = Info.explicit_origin_x
local SONGINFO_EXPLICIT_ORIGIN_Y     = Info.explicit_origin_y
local SONGINFO_SUBTITLE_ORIGIN_X     = Info.subtitle_origin_x
local SONGINFO_SUBTITLE_ORIGIN_Y     = Info.subtitle_origin_y
local SONGINFO_SUBTITLE_MWIDTH       = Info.subtitle_max_width
local SONGINFO_BPM_ORIGIN_X          = Info.bpm_origin_x
local SONGINFO_BPM_ORIGIN_Y          = Info.bpm_origin_y
local SONGINFO_BPM_MWIDTH            = Info.bpm_max_width
local SONGINFO_BPM_ROTATION          = Info.bpm_rotation   -- matches the BPM plate tilt
-- song speed notice (multiplier ~= 1)
local SONGINFO_SPEED_ORIGIN_X        = Info.speed_origin_x
local SONGINFO_SPEED_LABEL_Y         = Info.speed_label_y
local SONGINFO_SPEED_VALUE_Y         = Info.speed_value_y
local SONGINFO_SPEED_ARROW_GAP       = Info.speed_arrow_gap   -- between arrow centres
local SONGINFO_SPEED_LABEL           = Config.optional(Info, "speed_label")   -- skin text, else localized
local InfoAnim = Animation.song_info
local SPEED_ARROW_OPACITY_MIN        = InfoAnim.speed_arrow_opacity_min
local SONGINFO_CHARTER_ORIGIN_X      = Info.charter_origin_x
local SONGINFO_CHARTER_ORIGIN_Y      = Info.charter_origin_y
local SONGINFO_CHARTER_MWIDTH        = Info.charter_max_width
local SONGINFO_LENGTH_ORIGIN_X       = Info.length_origin_x
local SONGINFO_LENGTH_ORIGIN_Y       = Info.length_origin_y
local SONGINFO_LENGTH_MWIDTH         = Info.length_max_width

local Preimage = Layout.preimage
local PREIMAGE_ORIGIN_X = Preimage.origin_x
local PREIMAGE_ORIGIN_Y = Preimage.origin_y
local PREIMAGE_SIZE_X   = Preimage.size_x
local PREIMAGE_SIZE_Y   = Preimage.size_y

local Header = Layout.header
local HEADER_OFFSET_X          = Header.offset_x
local HEADER_BOX_TEXT_OFFSET_X = Header.box_text_offset_x
local HEADER_BOX_TEXT_OFFSET_Y = Header.box_text_offset_y
local HEADER_BOX_TEXT_MWIDTH   = Header.box_text_max_width
local HEADER_DIFF_ALPHA        = Header.diffselect_alpha

local BARLEFT_X_OFFSET             = List.barleft_x_offset   -- px left of bar.png top-left
local ARROW_ANGLE                  = math.rad(Animation.selected_arrows.angle_deg)   -- arrow bounce direction

local Nameplate = Layout.nameplate
local NAMEPLATE_BOX_FOLDED_SIZE_Y  = Nameplate.box_folded_size_y
local NAMEPLATE_SECONDARY_OFFSET_Y = Nameplate.secondary_offset_y
local NAMEPLATE_BOX_START_X        = Nameplate.box_start_x
local NAMEPLATE_BOX_SPACING_X      = Nameplate.box_spacing_x
local NAMEPLATE_OFFSET_X           = Nameplate.offset_x
local NAMEPLATE_OFFSET_Y           = Nameplate.offset_y
local NAMEPLATE_HEIGHT             = Nameplate.height
local PUCHI_OFFSET_X               = Nameplate.puchi_offset_x
local STATS_Y, STATS_X             = Nameplate.stats_y, Nameplate.stats_x

-- ==== Init ====

function M.init(g)
    G = g
end

-- ==== Utilities ====

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

-- lv: cached { lv, diff, isPlus, isVault }, nil without chart
local function drawLevelTag(lv, x, y)
    if lv == nil then return end
    local labelH = G.bars["levellabels"].Height / 5
    local labelW = G.bars["levellabels"].Width

    if lv.isVault then
        -- vault: animated strip (storm frame counter)
        G.bars["levellabelsvault"]:DrawRectAtAnchor(x, y, 0, labelH * G.levelLabelFrame, labelW, labelH, "center")
    elseif lv.diff <= 2 and lv.lv >= SONGBAR_CHECKER_LEVEL[lv.diff] and G.bars["levellabelschecker" .. lv.diff] then
        -- high level for its diff: checker tag
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
    -- fill digits, genre colour +50%
    G.drawNumberCentered(lv.lv, "levellabelsfill", x, y, lv.fillColor)
    if lv.isPlus then
        local px = x + G.calculateNumberWidth(lv.lv, "levellabels") / 2 + SONGBAR_PLUS_DX
        local py = y + SONGBAR_PLUS_DY
        G.bgtx["levellabels+"]:DrawAtAnchor(px, py, "center")
        local fill = G.bgtx["levellabelsfill+"]
        fill:SetColor(lv.fillColor or COL_WHITE)
        fill:DrawAtAnchor(px, py, "center")
        fill:SetColor(COL_WHITE)
    end
end

-- song info difficulty level, centred at (cx, cy)
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

-- song speed notice; arrows pulse half a beat apart (reversed when slowed)
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
            tex:SetOpacity(SPEED_ARROW_OPACITY_MIN + (1 - SPEED_ARROW_OPACITY_MIN) * (0.5 + 0.5 * math.cos(2 * math.pi * p)))
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

-- fav.png at (fav_icon_x, fav_icon_y) from bar.png top-left
local function drawFavIcon(xpos, ypos)
    if G.favoriteicon == nil then return end
    local bar_tl_x = xpos - G.bars["bar"].Width  / 2
    local bar_tl_y = ypos - G.bars["bar"].Height / 2
    G.favoriteicon:Draw(bar_tl_x + FAV_ICON_X, bar_tl_y + FAV_ICON_Y)
end

-- bl: cached { played, cs, sr }, nil without chart
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

    -- cs: 0 failed or unplayed, 1 assisted, 2 clear, 3 FC, 4 perfect
    if not played then
        G.bars["clearstatus_none"]:Draw(lx, ly)
        G.bars["scorerank_none"]:Draw(lx, ly)
    elseif cs == 0 then
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
    -- BLURRED: static instead of the jacket
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
        -- pop-in scale (G.preimagePopScale, navigation.lua)
        local pop = G.preimagePopScale or 1
        -- uniform fit inside the frame
        local fit = math.min(PREIMAGE_SIZE_X / tex.Width, PREIMAGE_SIZE_Y / tex.Height) * pop
        tex:SetScale(fit, fit)
        tex:DrawAtAnchor(PREIMAGE_ORIGIN_X - G.songSelectShift + PREIMAGE_SIZE_X / 2,
                         PREIMAGE_ORIGIN_Y + PREIMAGE_SIZE_Y / 2, "center")
        tex:SetScale(1, 1)
    end
end

-- ==== Song-list bars ====

local function drawBarTitle(pt, x, y)
    G.text:Draw(pt.text, x, y, pt.gold and SONGLIST_GOLD or nil, nil, 1, 1, SONGLIST_TITLE_MWIDTH, "center")
end

-- random bar: bar.png in tinted vertical slices (whole px, no seams)
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

-- sticker in the level tag slot (non-song bars)
local function drawSticker(kind, xpos, ypos)
    local x = xpos + SONGBAR_LABEL_X_OFFSET
    G.bars["sticker"]:DrawAtAnchor(x, ypos, "center")
    G.bars["sticker_" .. kind]:DrawAtAnchor(x, ypos, "center")
end

-- one list bar at (xpos, ypos)
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

-- bar rest position at page offset i (dist: scroll in flight, 0 at rest)
function M.barPos(i, dist)
    dist = dist or 0
    local xpos = SONGLIST_ORIGIN_X + (i + dist) * SONGLIST_OFFSET_X - G.songSelectShift
    local ypos = SONGLIST_ORIGIN_Y + (i + dist) * SONGLIST_OFFSET_Y
    if i == 0 then xpos = xpos + SONGLIST_SELECTED_X_DIFF end
    return xpos, ypos
end

-- ==== Shortcuts helper line ====
local helperBox = nil          -- { canvas, w, h, text }, rebaked on text change
local function drawHelperLine(text, opacity)
    local gf = G.textStats
    local inkH = math.ceil(gf.LineHeight)
    local pad = gf.BoxHeight - inkH            -- glyph box padding
    if helperBox == nil or helperBox.text ~= text then
        if helperBox ~= nil then helperBox.canvas:Dispose() end
        local w = math.ceil(gf:Measure(text)) + 2 * HELPER_PAD_X
        local h = inkH + 2 * HELPER_PAD_Y
        local cv = CANVAS:CreateCanvas(w, h)
        cv:ClearTransparent()
        local r = math.max(0, math.min(HELPER_BOX_RADIUS, math.floor(h / 2)))
        local br, bg, bb = HELPER_BOX_COLOR[1], HELPER_BOX_COLOR[2], HELPER_BOX_COLOR[3]
        cv:FillRect(r, 0, w - 2 * r, h, br, bg, bb, HELPER_BOX_ALPHA)
        cv:FillRect(0, r, w, h - 2 * r, br, bg, bb, HELPER_BOX_ALPHA)
        for _, c in ipairs({ { r, r }, { w - r - 1, r }, { r, h - r - 1 }, { w - r - 1, h - r - 1 } }) do
            cv:FillCircle(c[1], c[2], r, br, bg, bb, HELPER_BOX_ALPHA)
        end
        cv:Upload()
        helperBox = { canvas = cv, w = w, h = h, text = text }
    end
    helperBox.canvas:SetOpacity(opacity)
    helperBox.canvas:Draw(HELPER_X, HELPER_Y)
    helperBox.canvas:SetOpacity(1)
    gf:Draw(text, HELPER_X + HELPER_PAD_X - pad, HELPER_Y + HELPER_PAD_Y, HELPER_TEXT_COLOR, HELPER_OUTLINE_COLOR, opacity, 1, 0, "topleft")
end

-- ==== List backdrop ====
-- baked once: each row shifted by the bar slope (offset_x per offset_y)
-- edge fade: BACKDROP_FADE px in BACKDROP_FADE_STEP steps (0 = hard), max a quarter of the band
local backdrop = nil           -- { canvas, x0 }: x0 = screen x before the panel shift
local function drawListBackdrop(opacity)
    if backdrop == nil then
        local slope = SONGLIST_OFFSET_X / SONGLIST_OFFSET_Y
        local W, H = math.floor(BACKDROP_W), 1080
        local span = math.ceil(H * slope)
        local cv = CANVAS:CreateCanvas(W + span + 2, H)
        cv:ClearTransparent()
        local fade = math.max(0, math.min(math.floor(BACKDROP_FADE), math.floor(W / 4)))
        local step = BACKDROP_FADE_STEP
        local cr, cg, cb = BACKDROP_COLOR[1] or 0, BACKDROP_COLOR[2] or 0, BACKDROP_COLOR[3] or 0
        -- screen: alpha baked into the colour, solid pixels
        local screen = BACKDROP_BLEND == "screen"
        -- multi: colour * alpha, plus alpha
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
                -- hard edge: edge pixel filled by coverage
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
        -- row 0 left edge (band centred on the bar line + BACKDROP_DX)
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

-- list slot (-rows..rows) under (mx, my), or nil
-- plate: slanted strip in the bar texture (song_list.bar_plate)
local BAR_PLATE = List.bar_plate
function M.hitTest(mx, my)
    local tex = G.bars["bar"]
    local hw, hh = tex.Width / 2, tex.Height / 2
    for i = -SONGLIST_ROWS, SONGLIST_ROWS do
        local cx, cy = M.barPos(i, G.selectBoxDist)
        local row = my - (cy - hh)
        if row >= BAR_PLATE.top and row <= BAR_PLATE.bottom then
            local left = cx - hw + BAR_PLATE.left + (row - BAR_PLATE.top) * BAR_PLATE.skew
            if mx >= left and mx <= left + BAR_PLATE.width then return i end
        end
    end
    return nil
end

-- ==== Selected row band ====
-- baked like the backdrop, one list row high; G.selectBandPulse (0..1) scales alpha
local SELECT_BAND_COLOR = List.select_band_color
-- top level colour; in a folder, G.selectBandColor (navigation.lua)
local SELECT_BAND_TOP = COLOR:CreateColorFromARGB(255, SELECT_BAND_COLOR[1] or 0, SELECT_BAND_COLOR[2] or 0,
    SELECT_BAND_COLOR[3] or 0)
local SELECT_BAND_ALPHA = List.select_band_alpha
local selectBand = nil         -- { canvas, dx, y }: dx from the backdrop canvas x, y = screen row
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
        local step = BACKDROP_FADE_STEP
        local function fill(x, y, w, al) cv:FillRect(x, y, w, 1, 255, 255, 255, al) end
        for r = 0, H - 1 do
            -- row top + r, edges match the backdrop
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

-- ==== Folder open/close animation ====
-- G.folderAnim (navigation.lua): { mode = "open"/"close", phase = 1/2, t = 0..1,
--   oldBars/newBars = { {i, pt}, ... } outer-first, folderPt = folder bar }
-- t is pre-eased (SetEasing per phase): plain lerp
local ANIM_OFFLEFT_X = Animation.folder_anim.offscreen_left_x  -- bar slide off-screen x
local function lerp(a, b, t) return a + (b - a) * t end

local function drawFolderAnim()
    local fa = G.folderAnim
    local cx, cy = M.barPos(0, 0)
    local t = fa.t or 0
    if fa.mode == "open" then
        if fa.phase == 1 then
            -- parent siblings slide out left
            for _, b in ipairs(fa.oldBars) do
                local bx, by = M.barPos(b.i, 0)
                drawBar(b.pt, lerp(bx, ANIM_OFFLEFT_X, t), by)
            end
            drawBar(fa.folderPt, cx, cy)
        else
            -- contents fan out from the centre
            for _, b in ipairs(fa.newBars) do
                local bx, by = M.barPos(b.i, 0)
                drawBar(b.pt, lerp(cx, bx, t), lerp(cy, by, t))
            end
        end
    else
        if fa.phase == 1 then
            -- contents gather to the focus
            for _, b in ipairs(fa.oldBars) do
                local bx, by = M.barPos(b.i, 0)
                drawBar(b.pt, lerp(bx, cx, t), lerp(by, cy, t))
            end
        else
            -- parent siblings slide back in
            drawBar(fa.folderPt, cx, cy)
            for _, b in ipairs(fa.newBars) do
                local bx, by = M.barPos(b.i, 0)
                drawBar(b.pt, lerp(ANIM_OFFLEFT_X, bx, t), by)
            end
        end
    end
end
M.drawFolderAnim = drawFolderAnim

-- ==== Header breadcrumb ====
-- one crumb, right edge at xRight (arrow to its left)
local function drawCrumb(title, xRight, alpha, drawArrow)
    local boxW = G.bgtx["header-box"].Width
    local boxH = G.bgtx["header-box"].Height
    G.bgtx["header-box"]:SetOpacity(alpha)
    G.bgtx["header-box"]:DrawAtAnchor(xRight, 0, "topright")
    G.text:Draw(title, xRight - boxW + HEADER_BOX_TEXT_OFFSET_X,
        HEADER_BOX_TEXT_OFFSET_Y + boxH / 2, nil, nil, alpha, 1, HEADER_BOX_TEXT_MWIDTH, "center")
    if drawArrow then
        G.bgtx["header-arrow"]:SetOpacity(alpha)
        G.bgtx["header-arrow"]:DrawAtAnchor(xRight - boxW, 0, "topright")
    end
    G.bgtx["header-box"]:SetOpacity(1)
    G.bgtx["header-arrow"]:SetOpacity(1)
end

-- header crumbs, synced to G.folderAnim
-- open: crumbs slide left (phase 1), new crumb fades in (phase 2)
-- close: innermost fades out (phase 1), rest slide right (phase 2)
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
        for j = 1, N do                    -- slot j-1 -> j
            local x = (fa.phase == 1) and slotX(lerp(j - 1, j, t)) or slotX(j)
            drawCrumb(old[j], x, opacityNorm, j < N)
        end
        if fa.phase == 2 and new[1] ~= nil then
            drawCrumb(new[1], slotX(0), opacityNorm * t, #new > 1)
        end
    else
        local N = #old                     -- old path = { closed folder } ++ new
        for k = 2, N do                    -- slot k-1 -> k-2
            local x = (fa.phase == 1) and slotX(k - 1) or slotX(lerp(k - 1, k - 2, t))
            drawCrumb(old[k], x, opacityNorm, k < N)
        end
        if fa.phase == 1 and old[1] ~= nil then
            drawCrumb(old[1], slotX(0), opacityNorm * (1 - t), N > 1)
        end
    end
end
M.drawHeaderCrumbsAnim = drawHeaderCrumbsAnim

-- ==== Draw panel ====

function M.drawPanel()
    local opacityNorm = G.songSelectElemOpacity / 255
    local sel = G.selInfo

    -- backdrop under the rest of the panel
    if opacityNorm > 0 then drawListBackdrop(opacityNorm) end
    if G.folderAnim == nil and G.pageTexts ~= nil then drawSelectBand(opacityNorm) end

    -- info panels (node data from the navigation.lua selection cache)
    if sel ~= nil and sel.isRandom then
        G.bgtx["randominfo"]:DrawAtAnchor(1920 - G.songSelectShift, 0, "topright")
    end

    -- folder info (placeholder)
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
        -- vault locked: no info; BLURRED: darker, texts as ???
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

            -- difficulty icons hidden for GRAYED and above
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
                        -- vault songs: no missing indicator
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
            -- rebuild on new async preview duration
            if sel.lenMs ~= G.previewDurationMs then
                sel.lenMs   = G.previewDurationMs
                sel.lenText = string.format(G.skinText("SONGSELECT_INFO_LENGTH", "Length - %s"), formatDuration(sel.lenMs))
            end
            G.textSmall:Draw(hide and "???" or sel.lenText, SONGINFO_LENGTH_ORIGIN_X - G.songSelectShift, SONGINFO_LENGTH_ORIGIN_Y,
                nil, nil, 1, 1, SONGINFO_LENGTH_MWIDTH)

            if hide then
                G.text:Draw("???", SONGINFO_BPM_ORIGIN_X - G.songSelectShift, SONGINFO_BPM_ORIGIN_Y,
                    COL_WHITE, nil, 1, 1, SONGINFO_BPM_MWIDTH, "center", 0, SONGINFO_BPM_ROTATION)
            elseif sel.bpmBase ~= nil then
                -- rebuild on song speed change
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
                    -- ms per sped-up beat (clamped)
                    local bpm = (sel.bpmBase or 0) * mult
                    sel.speedBeatMs = (bpm > 0) and math.max(InfoAnim.speed_beat_min_ms, math.min(InfoAnim.speed_beat_max_ms, 60000 / bpm))
                        or InfoAnim.speed_beat_default_ms
                end
                G.text:Draw(sel.bpmText, SONGINFO_BPM_ORIGIN_X - G.songSelectShift, SONGINFO_BPM_ORIGIN_Y,
                    sel.bpmColor, nil, 1, 1, SONGINFO_BPM_MWIDTH, "center", 0, SONGINFO_BPM_ROTATION)
                if sel.speedText then drawSpeedNotice(sel) end
            end
        end
    end

    drawPreimage()

    -- list bars (folder anim owns the list while it runs)
    if G.folderAnim ~= nil then
        drawFolderAnim()
    elseif G.pageTexts ~= nil then
        for i, tx in pairs(G.pageTexts) do
            local xpos, ypos = M.barPos(i, G.selectBoxDist)
            drawBar(tx, xpos, ypos)
        end

        -- selected bar arrows
        local x0     = SONGLIST_ORIGIN_X + SONGLIST_SELECTED_X_DIFF - G.songSelectShift
        local y0     = SONGLIST_ORIGIN_Y
        local ax     = G.arrowsDistance
        local xlshift = ax * math.cos(ARROW_ANGLE)
        local ylshift = ax * math.sin(ARROW_ANGLE)
        local isUnlockedSong = sel ~= nil and sel.isSong and sel.isUnlockedSong
        -- left arrow clears barleft on unlocked songs
        local arrowLOffset = isUnlockedSong and BARLEFT_X_OFFSET or 0
        G.bars["selected-arrow-l"]:DrawAtAnchor(x0 - SONGLIST_SELECTED_ARROW_GAP/2 + xlshift + arrowLOffset, y0 - ylshift, "left")
        G.bars["selected-arrow-r"]:DrawAtAnchor(x0 + SONGLIST_SELECTED_ARROW_GAP/2 - xlshift, y0 + ylshift, "right")
    end

    -- header alpha 1 -> HEADER_DIFF_ALPHA toward diffselect
    local headerAlpha = 1.0 - (1.0 - HEADER_DIFF_ALPHA) * math.min(1, G.songSelectShift / 1920)
    G.bgtx["header"]:SetOpacity(headerAlpha)
    G.bgtx["header"]:Draw(-G.songSelectShift, 0)
    G.bgtx["header"]:SetOpacity(1)
    if G.folderAnim ~= nil then
        drawHeaderCrumbsAnim(opacityNorm)
    elseif sel ~= nil then
        local SLOT = G.bgtx["header-box"].Width + G.bgtx["header-arrow"].Width
        local X0   = HEADER_OFFSET_X - G.songSelectShift
        for i, title in ipairs(sel.crumbs) do
            drawCrumb(title, X0 - (i - 1) * SLOT, opacityNorm, i ~= #sel.crumbs)
        end
    end

    -- overlay, then shortcuts helper line (fades with the panel)
    G.bgtx["overlay"]:SetOpacity(opacityNorm)
    G.bgtx["overlay"]:Draw(0, 0)
    local helper = G.shortcuts and G.shortcuts.helperText()
    if helper ~= nil and opacityNorm > 0 then
        local pulse = 0.5 + 0.5 * math.sin(2 * math.pi * (G.nowMs or 0) / HELPER_PULSE_MS)
        drawHelperLine(helper, opacityNorm * (HELPER_OPACITY_MIN + (1 - HELPER_OPACITY_MIN) * pulse))
    end

    -- unlock conditions (locked song highlighted)
    G.unlocks.drawCondsPanel()
    G.unlocks.drawVaultCondsPanel()

    -- nameplates (online lobby draws its own)
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

        -- clear counts at the shown difficulty (re-queried on player/course change)
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
                G.textStats:Draw(n, x, STATS_Y, COL_WHITE, nil, opacityNorm, 1, 0, "center")
            end
            drawStat(sc.perfect, STATS_X[1])
            drawStat(sc.fc,      STATS_X[2])
            drawStat(sc.clear,   STATS_X[3])
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
        -- Portrait.png over the placeholder when loaded
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
