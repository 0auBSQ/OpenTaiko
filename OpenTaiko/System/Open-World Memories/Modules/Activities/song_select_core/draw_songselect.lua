---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- draw_songselect.lua  —  Song-select panel drawing for song_select_core.

local CFG = require("sscore_config")   -- Config/layout.json (skinner-editable); values fall back to the defaults below

local M = {}
-- hoisted colors (per-frame COLOR:Create* calls allocate userdata every frame); overridable via config
local SONGLIST_GOLD  = CFG.color("colors.songlist_gold", COLOR:CreateColorFromARGB(255, 242, 207, 1))   -- selected-bar title tint
local COL_WHITE      = COLOR:CreateColorFromHex("ffffffff")
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

local SONGINFO_DIFFICULTIES_ORIGIN_X = CFG.num("song_info.difficulties_origin_x", 1790)
local SONGINFO_DIFFICULTIES_ORIGIN_Y = CFG.num("song_info.difficulties_origin_y", 154)
local SONGINFO_DIFFICULTIES_GAP_Y    = CFG.num("song_info.difficulties_gap_y", 130)
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
    elseif lv.diff < 3 or lv.lv <= 10 then
        G.bars["levellabels"]:DrawRectAtAnchor(x, y, 0, labelH * lv.diff, labelW, labelH, "center")
    elseif lv.diff == 3 then
        G.bars["levellabelsfire"]:DrawRectAtAnchor(x, y, 0, labelH * G.levelLabelFrame, labelW, labelH, "center")
    else
        G.bars["levellabelsstorm"]:DrawRectAtAnchor(x, y, 0, labelH * G.levelLabelFrame, labelW, labelH, "center")
    end
    G.drawNumberCentered(lv.lv, "levellabels", x, y)
    -- Fill numbers over the base ones, tinted to the genre (song bar) colour brightened 50% (any level)
    G.drawNumberCentered(lv.lv, "levellabelsfill", x, y, lv.fillColor)
    if lv.isPlus then
        if lv.isVault then
            G.bars["levellabelsplusvault"]:DrawAtAnchor(x, y, "center")
        else
            G.bars["levellabelsplus"]:DrawRectAtAnchor(x, y, 0, labelH * lv.diff, labelW, labelH, "center")
        end
    end
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
    if sel == nil or not sel.isSong or sel.hi >= 2 then return end
    G.bgtx["preimage_load"]:Draw(PREIMAGE_ORIGIN_X - G.songSelectShift, PREIMAGE_ORIGIN_Y)
    G.bgtx["load"]:DrawAtAnchor(
        PREIMAGE_ORIGIN_X - G.songSelectShift + PREIMAGE_SIZE_X / 2,
        PREIMAGE_ORIGIN_Y + PREIMAGE_SIZE_Y / 2, "center")
    local tex = SHARED:GetSharedTexture("preimage")
    if tex.Height > 0 and tex.Width > 0 then
        tex:SetScale(PREIMAGE_SIZE_X / tex.Height, PREIMAGE_SIZE_Y / tex.Width)
        tex:Draw(PREIMAGE_ORIGIN_X - G.songSelectShift, PREIMAGE_ORIGIN_Y)
    end
end

-- ── Draw panel ────────────────────────────────────────────────────────────────

function M.drawPanel()
    local opacityNorm = G.songSelectElemOpacity / 255
    local sel = G.selInfo

    -- Random / song info panels (all node-derived data comes from the selection cache — see navigation.lua)
    if sel ~= nil and sel.isRandom then
        G.bgtx["randominfo"]:DrawAtAnchor(1920 - G.songSelectShift, 0, "topright")
    end

    if sel ~= nil and sel.isSong then
        -- BLURED / vault locked: skip the song info panel entirely
        if sel.hi < 2 then
            G.bgtx["songinfo"]:DrawAtAnchor(1920 - G.songSelectShift, 0, "topright")
            if sel.hasVideo then
                G.bgtx["sinfo_video"]:Draw(SONGINFO_HASVIDEO_ORIGIN_X - G.songSelectShift, SONGINFO_HASVIDEO_ORIGIN_Y)
            end
            if sel.explicit then
                G.bgtx["sinfo_explicit"]:DrawAtAnchor(
                    SONGINFO_EXPLICIT_ORIGIN_X - G.songSelectShift, SONGINFO_EXPLICIT_ORIGIN_Y, "topright")
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
                            G.drawNumberCentered(d.level, "sinfo_level",
                                xpos + difftx.Width / 2,
                                ypos + difftx.Height / 2,
                                nil, G.difficultyFade4 / 255)
                            if d.isPlus then
                                local plustx = isVaultSong and G.bgtx["sinfo_difficulties_vault_plus"] or G.bgtx["sinfo_difficulties_" .. i .. "_plus"]
                                plustx:SetOpacity(G.difficultyFade4 / 255)
                                plustx:Draw(xpos, ypos)
                                plustx:SetOpacity(1)
                            end
                        end
                    elseif d == nil then
                        -- Vault songs: never show the "missing" indicator
                        if not isVaultSong and (not has4 or i ~= 3) then
                            G.bgtx["sinfo_difficulties_missing"]:Draw(xpos, ypos)
                        end
                    else
                        local difftx = isVaultSong and G.bgtx["sinfo_difficulties_vault"] or G.bgtx["sinfo_difficulties_" .. i]
                        difftx:Draw(xpos, ypos)
                        G.drawNumberCentered(d.level, "sinfo_level",
                            xpos + difftx.Width / 2,
                            ypos + difftx.Height / 2)
                        if d.isPlus then
                            local plustx = isVaultSong and G.bgtx["sinfo_difficulties_vault_plus"] or G.bgtx["sinfo_difficulties_" .. i .. "_plus"]
                            plustx:Draw(xpos, ypos)
                        end
                    end
                end
            end

            G.textSmall:Draw(sel.subtitle, SONGINFO_SUBTITLE_ORIGIN_X - G.songSelectShift, SONGINFO_SUBTITLE_ORIGIN_Y,
                nil, nil, 1, 1, SONGINFO_SUBTITLE_MWIDTH, "center")
            G.textSmall:Draw(sel.charter, SONGINFO_CHARTER_ORIGIN_X - G.songSelectShift, SONGINFO_CHARTER_ORIGIN_Y,
                nil, nil, 1, 1, SONGINFO_CHARTER_MWIDTH)
            -- rebuild the length string only when the async preview load lands a new duration
            if sel.lenMs ~= G.previewDurationMs then
                sel.lenMs   = G.previewDurationMs
                sel.lenText = "Length - " .. formatDuration(sel.lenMs)
            end
            G.textSmall:Draw(sel.lenText, SONGINFO_LENGTH_ORIGIN_X - G.songSelectShift, SONGINFO_LENGTH_ORIGIN_Y,
                nil, nil, 1, 1, SONGINFO_LENGTH_MWIDTH)

            if sel.bpmBase ~= nil then
                -- rebuild the BPM string/color only when the song-speed multiplier changes
                local mult = CONFIG.SongSpeed / 20
                if sel.bpmMult ~= mult then
                    sel.bpmMult = mult
                    local bpmText = formatNumber(sel.bpmBase * mult, 3)
                    if sel.bpmBase ~= sel.bpmMin or sel.bpmBase ~= sel.bpmMax then
                        bpmText = bpmText .. " (" .. formatNumber(sel.bpmMin * mult, 3)
                               .. "-" .. formatNumber(sel.bpmMax * mult, 3) .. ")"
                    end
                    sel.bpmText  = bpmText
                    sel.bpmColor = (mult < 1) and COL_BPM_SLOW or (mult > 1) and COL_BPM_FAST or COL_WHITE
                end
                G.text:Draw(sel.bpmText, SONGINFO_BPM_ORIGIN_X - G.songSelectShift, SONGINFO_BPM_ORIGIN_Y,
                    sel.bpmColor, nil, 1, 1, SONGINFO_BPM_MWIDTH, "center", 0, SONGINFO_BPM_ROTATION)
            end
        end
    end

    drawPreimage()

    -- Song list bars (titles are glyph-drawn from the cached strings; gold = the selected bar)
    local function drawBarTitle(pt, x, y)
        G.text:Draw(pt.text, x, y, pt.gold and SONGLIST_GOLD or nil, nil, 1, 1, 525, "center")
    end
    if G.pageTexts ~= nil then
        for i, tx in pairs(G.pageTexts) do
            local xpos = SONGLIST_ORIGIN_X + (i + G.selectBoxDist) * SONGLIST_OFFSET_X - G.songSelectShift
            local ypos = SONGLIST_ORIGIN_Y + (i + G.selectBoxDist) * SONGLIST_OFFSET_Y
            if i == 0 then xpos = xpos + SONGLIST_SELECTED_X_DIFF end
            if tx ~= nil then
                if tx.isSong or tx.isFolder then
                    if tx.isSong and tx.vaultLocked then
                        -- Secret Vault locked song: vault bar + vault lock icon, no title, no level tag
                        G.bars["vault_bar"]:DrawAtAnchor(xpos, ypos, "center")
                        if G.bars[tx.vaultLockKey] then
                            G.bars[tx.vaultLockKey]:DrawAtAnchor(xpos - G.bars["bar"].Width / 2, ypos, "left")
                        end
                    elseif tx.isFolder and tx.vaultFolder then
                        -- Secret Vault folder (locked): normal bar, blurred glitch, lockF overlay, no title
                        G.bars["bar"]:SetColor(tx.boxColor)
                        G.bars["bar"]:DrawAtAnchor(xpos, ypos, "center")
                        G.unlocks.drawBluredStatic(xpos, ypos)
                        if G.bars["vault_lockF"] then G.bars["vault_lockF"]:DrawAtAnchor(xpos, ypos, "center") end
                    elseif tx.isLocked then
                        -- GRAYED/BLURED use bar_1; DISPLAYED locked songs keep the normal bar
                        if tx.lockedBarOverride then
                            G.bars["bar_1"]:DrawAtAnchor(xpos, ypos, "center")
                        else
                            G.bars["bar"]:SetColor(tx.boxColor)
                            G.bars["bar"]:DrawAtAnchor(xpos, ypos, "center")
                            G.genre_overlays[tx.genre]:DrawAtAnchor(xpos, ypos, "center")
                        end
                        -- Title area content: BLURED → static.png with GL noise; others → title text
                        if tx.hi == 2 then
                            G.unlocks.drawBluredStatic(xpos, ypos)
                        else
                            drawBarTitle(tx, xpos + SONGLIST_TEXT_OFFSET_X, ypos + SONGLIST_TEXT_OFFSET_Y)
                        end
                        -- Lock icon on top of title area
                        if tx.lockKey and G.bars[tx.lockKey] then
                            G.bars[tx.lockKey]:DrawAtAnchor(xpos - G.bars["bar"].Width / 2, ypos, "left")
                        end
                        -- Level tag: only DISPLAYED locked songs (hi == 0)
                        if tx.isSong and tx.hi == 0 then
                            drawLevelTag(tx.level, xpos + SONGBAR_LABEL_X_OFFSET, ypos)
                        end
                    else
                        G.bars["bar"]:SetColor(tx.boxColor)
                        G.bars["bar"]:DrawAtAnchor(xpos, ypos, "center")
                        G.genre_overlays[tx.genre]:DrawAtAnchor(xpos, ypos, "center")
                        if tx.fav then drawFavIcon(xpos, ypos) end
                        if tx.isSong then drawBarleft(tx.barleft, xpos, ypos) end
                        drawLevelTag(tx.level, xpos + SONGBAR_LABEL_X_OFFSET, ypos)
                        drawBarTitle(tx, xpos + SONGLIST_TEXT_OFFSET_X, ypos + SONGLIST_TEXT_OFFSET_Y)
                    end
                elseif tx.isRandom then
                    G.bars["random"]:DrawAtAnchor(xpos, ypos, "center")
                    drawBarTitle(tx, xpos + SONGLIST_TEXT_OFFSET_X, ypos + SONGLIST_TEXT_OFFSET_Y)
                elseif tx.isReturn then
                    G.bars["back"]:DrawAtAnchor(xpos, ypos, "center")
                    drawBarTitle(tx, xpos + SONGLIST_TEXT_OFFSET_X, ypos + SONGLIST_TEXT_OFFSET_Y)
                end

            end
        end

        -- Selected bar + animated arrows
        local x0     = SONGLIST_ORIGIN_X + SONGLIST_SELECTED_X_DIFF - G.songSelectShift
        local y0     = SONGLIST_ORIGIN_Y
        local ax     = G.arrowsDistance
        local xlshift = ax * math.cos(7 * math.pi / 12)
        local ylshift = ax * math.sin(7 * math.pi / 12)
        local isUnlockedSong = sel ~= nil and sel.isSong and sel.isUnlockedSong
        if isUnlockedSong then
            -- selectedlarge covers bar + barleft; align its right edge with selected's right edge
            local largeCenterX = x0 - (G.bars["selectedlarge"].Width - G.bars["selected"].Width) / 2
            G.bars["selectedlarge"]:DrawAtAnchor(largeCenterX, y0, "center")
        else
            G.bars["selected"]:DrawAtAnchor(x0, y0, "center")
        end
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
    if sel ~= nil then
        local pathStack = sel.crumbs
        local xpos = HEADER_OFFSET_X - G.songSelectShift
        for i, title in ipairs(pathStack) do
            G.bgtx["header-box"]:SetOpacity(opacityNorm)
            G.bgtx["header-box"]:DrawAtAnchor(xpos, 0, "topright")
            G.text:Draw(title,
                xpos - G.bgtx["header-box"].Width + HEADER_BOX_TEXT_OFFSET_X,
                HEADER_BOX_TEXT_OFFSET_Y + G.bgtx["header-box"].Height / 2,
                nil, nil, opacityNorm, 1, 270, "center")
            G.bgtx["header-arrow"]:SetOpacity(opacityNorm)
            if i ~= #pathStack then
                G.bgtx["header-arrow"]:DrawAtAnchor(xpos - G.bgtx["header-box"].Width, 0, "topright")
            end
            xpos = xpos - G.bgtx["header-box"].Width - G.bgtx["header-arrow"].Width
        end
    end

    -- Overlay
    G.bgtx["overlay"]:SetOpacity(opacityNorm)
    G.bgtx["overlay"]:Draw(0, 0)

    -- Unlock conditions panel (shown when a locked song is highlighted)
    G.unlocks.drawCondsPanel()
    G.unlocks.drawVaultCondsPanel()

    -- Nameplates
    local playerCount = CONFIG.PlayerCount
    G.highlightedPlayer = G.highlightedPlayer % playerCount

    G.bgtx["nameplate_info"]:SetOpacity(opacityNorm)
    do
        local x0       = NAMEPLATE_BOX_START_X
        local y0       = 1080 - NAMEPLATE_BOX_FOLDED_SIZE_Y
        local ssCharaX = x0 + G.bgtx["nameplate_info"].Width / 2
        G.bgtx["nameplate_info"]:Draw(x0, y0)
        G.drawPlayerChara(G.highlightedPlayer, ssCharaX, y0 + NAMEPLATE_OFFSET_Y, 1, 1, opacityNorm, false)
        G.drawPlayerPuchi(G.highlightedPlayer, ssCharaX - PUCHI_OFFSET_X, y0 + NAMEPLATE_OFFSET_Y + G.puchiSineY, 1, 1, opacityNorm)
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
