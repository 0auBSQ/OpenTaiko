---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- diffselect.lua  —  Difficulty-select draw panel and update handler
--                    for song_select_core.
--
-- Layout: every DifficultyBars element is positioned RELATIVE to Note.png's top-left. The Note frame floats
-- and gently rotates (a "in the sky" idle), and every child follows rigidly — positions rotate about the
-- Note's centre and each sprite/number/text spins by the same angle. The whole group fades in with the
-- songselect→diffselect transition (difficultySelectElemOpacity) and shares the horizontal scroll (xshift).

local Replay = require("replaylist")
local CFG    = require("sscore_config")   -- Config/layout.json (skinner-editable); values fall back to the defaults below

local M = {}
local G   -- shared state injected by Script.lua

-- ── Layout constants (defaults; overridable via Config/layout.json) ─────────────

local DIFFSELECT_CHARA_ORIG_X_35P = CFG.num("difficulty_select.chara_35p.origin_x", 1250)
local DIFFSELECT_CHARA_ORIG_Y_35P = CFG.num("difficulty_select.chara_35p.origin_y", 470)
local DIFFSELECT_CHARA_GAP_X_35P  = CFG.num("difficulty_select.chara_35p.gap_x", 332)
local DIFFSELECT_CHARA_GAP_Y_35P  = CFG.num("difficulty_select.chara_35p.gap_y", 457)
local DIFFSELECT_CHARA_SCALE_35P  = CFG.num("difficulty_select.chara_35p.scale", 0.5)
local DIFFSELECT_CHARA_ORIG_X_12P = CFG.num("difficulty_select.chara_12p.origin_x", 1300)
local DIFFSELECT_CHARA_ORIG_Y_12P = CFG.num("difficulty_select.chara_12p.origin_y", 760)
local DIFFSELECT_CHARA_GAP_X_12P  = CFG.num("difficulty_select.chara_12p.gap_x", 468)
local DIFFSELECT_CHARA_SCALE_12P  = CFG.num("difficulty_select.chara_12p.scale", 0.8)

-- Per-difficulty LevelCol tint (index = difficulty + 1); vault overrides to LVL_VAULT_COLOR.
local DIFFSELECT_LEVEL_COLORS = CFG.colorList("colors.level", {
    COLOR:CreateColorFromHex("FF65A9F7"),
    COLOR:CreateColorFromHex("FF8FF5A9"),
    COLOR:CreateColorFromHex("FFE6DA76"),
    COLOR:CreateColorFromHex("FFF39898"),
    COLOR:CreateColorFromHex("FFCD7EE6"),
})
local COL_WHITE       = COLOR:CreateColorFromHex("FFFFFFFF")
local LVL_VAULT_COLOR = CFG.color("colors.level_vault", COLOR:CreateColorFromHex("FF1F5050"))
local VAULT_BLACK     = CFG.color("colors.vault_text", COLOR:CreateColorFromARGB(255, 0, 0, 0))
local VAULT_NOOUTLINE = CFG.color("colors.vault_text_outline", COLOR:CreateColorFromARGB(0, 0, 0, 0))

-- Note frame: base position relative to the difficulty-select scroll, plus the float/rotate idle.
local NOTE_BASE_X      = CFG.num("difficulty_select.note.base_x", -115)
local NOTE_BASE_Y      = CFG.num("difficulty_select.note.base_y", -78)
local NOTE_FLOAT_AMP_X = CFG.num("difficulty_select.note.float_amp_x", 6)     -- px, gentle horizontal drift
local NOTE_FLOAT_AMP_Y = CFG.num("difficulty_select.note.float_amp_y", 12)    -- px, gentle vertical bob
local NOTE_ROT_AMP     = CFG.num("difficulty_select.note.rot_amp", 1.5)       -- degrees; sine amplitude of the gentle sway (not a turn count)

-- Option bars (0 / 1 / Customize), top-left anchor relative to Note; first position, step to each next.
local OPT_ORIG_X = CFG.num("difficulty_select.option_bars.origin_x", 595)
local OPT_ORIG_Y = CFG.num("difficulty_select.option_bars.origin_y", 139)
local OPT_STEP_X = CFG.num("difficulty_select.option_bars.step_x", -245)
local OPT_STEP_Y = CFG.num("difficulty_select.option_bars.step_y", 57)

-- Difficulty bars (compact list, one slot each along the diagonal); first position, step to each next.
local DBAR_ORIG_X = CFG.num("difficulty_select.difficulty_bars.origin_x", 173)
local DBAR_ORIG_Y = CFG.num("difficulty_select.difficulty_bars.origin_y", 332)
local DBAR_STEP_X = CFG.num("difficulty_select.difficulty_bars.step_x", 39)
local DBAR_STEP_Y = CFG.num("difficulty_select.difficulty_bars.step_y", 140)

-- LevelCol number, relative to a difficulty bar's top-left. The whole digit set is centred at (LVL_CX,LVL_CY);
-- multiple digits step by (LVL_DIGIT_DX, LVL_DIGIT_DY) (a slight up-right slant matching the layout).
local LVL_CX       = CFG.num("difficulty_select.level.center_x", 399)
local LVL_CY       = CFG.num("difficulty_select.level.center_y", 108)
local LVL_DIGIT_DX = CFG.num("difficulty_select.level.digit_step_x", 34)
local LVL_DIGIT_DY = CFG.num("difficulty_select.level.digit_step_y", -9)

-- Charter names (up to CHARTER_MAX), relative to a difficulty bar's top-left. First centred at
-- (CHARTER_CX,CHARTER_CY), each next offset by (CHARTER_DX,CHARTER_DY); squished to CHARTER_MAXW, tilted CHARTER_ROT.
local CHARTER_CX   = CFG.num("difficulty_select.charter.center_x", 551)
local CHARTER_CY   = CFG.num("difficulty_select.charter.center_y", 58)
local CHARTER_DX   = CFG.num("difficulty_select.charter.step_x", 7)
local CHARTER_DY   = CFG.num("difficulty_select.charter.step_y", 26)
local CHARTER_MAXW = CFG.num("difficulty_select.charter.max_width", 100)
local CHARTER_ROT  = CFG.num("difficulty_select.charter.rotation", 13.76)   -- degrees; up-right tilt (CCW). Flip if it tilts down.
local CHARTER_MAX  = CFG.num("difficulty_select.charter.max_count", 3)

-- Vault chart name, relative to a difficulty bar's top-left (centred), squished + tilted like the charters.
local VAULT_CX   = CFG.num("difficulty_select.vault.center_x", 178)
local VAULT_CY   = CFG.num("difficulty_select.vault.center_y", 215)
local VAULT_MAXW = CFG.num("difficulty_select.vault.max_width", 222)

-- Charter/vault labels render to a single cached texture each (TEXT:GetText) rather than per-glyph, so
-- rotating them stays smooth (a rotated composite of many small glyph quads jitters). Bounded strings.
local charterFont, vaultFont
local function ensureLabelFonts()
    if charterFont == nil then charterFont = TEXT:Create(18) end
    if vaultFont   == nil then vaultFont   = TEXT:Create(24) end
end

-- Player selector (P1..P5) texture is split vertically at y = PSEL_SPLIT_Y: the wide TOP half frames a
-- difficulty bar (drawn at (PSEL_DIFF_DX,PSEL_DIFF_DY) from the bar's top-left), the small BOTTOM half
-- frames an option bar. The bottom half's art sits in the texture's left ~PSEL_OPT_W px, so it is centred
-- on the option bar (centring on the full 674px width put it far to the right), then nudged by (PSEL_OPT_DX,DY).
local PSEL_SPLIT_Y = CFG.num("difficulty_select.player_selector.split_y", 292)
local PSEL_DIFF_DX = CFG.num("difficulty_select.player_selector.diff_offset_x", 8)
local PSEL_DIFF_DY = CFG.num("difficulty_select.player_selector.diff_offset_y", -7)
local PSEL_OPT_W   = CFG.num("difficulty_select.player_selector.opt_width", 290)
local PSEL_OPT_DX  = CFG.num("difficulty_select.player_selector.opt_offset_x", 22)
local PSEL_OPT_DY  = CFG.num("difficulty_select.player_selector.opt_offset_y", -6)

-- Header alpha eases 100% (song select) → HEADER_DIFF_ALPHA (difficulty select) across the transition.
local HEADER_DIFF_ALPHA  = CFG.num("header.diffselect_alpha", 0.4)

local NAMEPLATE_HEIGHT   = CFG.num("difficulty_select.nameplate.height", 81)
local NAMEPLATE_OFFSET_X = CFG.num("difficulty_select.nameplate.offset_x", 27)
local NAMEPLATE_OFFSET_Y = CFG.num("difficulty_select.nameplate.offset_y", 37)
local PUCHI_OFFSET_X     = CFG.num("difficulty_select.nameplate.puchi_offset_x", 60)

-- ── Init ──────────────────────────────────────────────────────────────────────

function M.init(g)
    G = g
    Replay.init(g)
end

-- ── Helpers ───────────────────────────────────────────────────────────────────

-- Split a comma-separated charter string into up to CHARTER_MAX trimmed names.
local function splitCharters(s)
    local out = {}
    if s == nil then return out end
    for part in string.gmatch(s, "[^,]+") do
        local t = part:gsub("^%s+", ""):gsub("%s+$", "")
        if t ~= "" then
            out[#out + 1] = t
            if #out >= CHARTER_MAX then break end
        end
    end
    return out
end

function M.loadDiffBars(ssn)
    G.diffBars = {}
    local startDiff = 0
    local isVault   = ssn.Genre == "Secret Vault"
    if isVault then startDiff = 3 end
    for i = startDiff, 4 do
        local chart = ssn:GetChart(i)
        if chart ~= nil then
            local charters = splitCharters(chart.NotesDesigner)
            if #charters == 0 then charters = splitCharters(ssn.Maker) end
            table.insert(G.diffBars, {
                vault      = isVault,
                level      = chart.Level,
                isplus     = chart.IsPlus,
                charters   = charters,
                difficulty = i,
                vaultName  = isVault and chart:GetCustomCommand(".VAULT_NAME") or nil,
            })
        end
    end
end

function M.updateTransitionVisuals(val)
    G.songSelectShift = val
    local opacity    = 255 - (val * (255 / 960))
    G.songSelectElemOpacity      = math.max(0, math.min(255, opacity))
    local diffOpacity = (val - 960) * (255 / 960)
    G.difficultySelectElemOpacity = math.max(0, math.min(255, diffOpacity))
end

function M.resetToSongSelect()
    G.songSelectElemOpacity      = 255
    G.difficultySelectElemOpacity = 0
    G.songSelectShift = 0
    G.activeScreen    = "songselect"
    Replay.reset()
end

-- ── Note frame transform (float + rotation shared by every child) ───────────────
-- Computed once per drawPanel; the draw helpers below read these upvalues.

local nAngleDeg, nCosA, nSinA, nNoteX, nNoteY, nPivX, nPivY

local function computeNoteXform()
    local ph  = (G.noteFloatPhase or 0) * math.pi / 180
    local fx  = math.sin(ph * 2) * NOTE_FLOAT_AMP_X
    local fy  = math.sin(ph) * NOTE_FLOAT_AMP_Y
    nAngleDeg = math.sin(ph + 0.9) * NOTE_ROT_AMP
    local rad = nAngleDeg * math.pi / 180
    nCosA, nSinA = math.cos(rad), math.sin(rad)

    local note = G.bars["diffnote"]
    local nw, nh = note.Width, note.Height
    -- The Note (and its children) scroll in with the transition (xshift) AND fade — same as the panel.
    nNoteX = NOTE_BASE_X + (1920 - G.songSelectShift) + fx
    nNoteY = NOTE_BASE_Y + fy
    nPivX, nPivY = nNoteX + nw / 2, nNoteY + nh / 2
end

-- Map a Note-local point (lx,ly) to its screen position, rigidly rotated about the Note centre. The engine's
-- SetRotation is counter-clockwise on screen (quad +y is screen-up), so this uses the matching CCW rotation
-- (y-down screen) — otherwise positions and sprite spins disagree and the group visibly shears apart.
local function nmap(lx, ly)
    local rx = (nNoteX + lx) - nPivX
    local ry = (nNoteY + ly) - nPivY
    return nPivX + rx * nCosA + ry * nSinA, nPivY - rx * nSinA + ry * nCosA
end

-- Draw a whole texture whose Note-local top-left is (ox,oy), transformed rigidly with the Note. Sub-pixel
-- positions are kept (no floor) so the rotation stays smooth instead of snapping pixel-to-pixel.
local function drawTexTL(tex, ox, oy, opacity)
    local cx, cy = nmap(ox + tex.Width / 2, oy + tex.Height / 2)
    tex:SetRotation(nAngleDeg)
    tex:SetOpacity(opacity)
    tex:DrawAtAnchor(cx, cy, "center")
    tex:SetRotation(0)
    tex:SetOpacity(1)
end

-- Draw a sub-rect of a texture whose Note-local top-left (of the drawn region) is (ox,oy).
local function drawTexRectTL(tex, ox, oy, sx, sy, sw, sh, opacity)
    local cx, cy = nmap(ox + sw / 2, oy + sh / 2)
    tex:SetRotation(nAngleDeg)
    tex:SetOpacity(opacity)
    tex:DrawRectAtAnchor(cx, cy, sx, sy, sw, sh, "center")
    tex:SetRotation(0)
    tex:SetOpacity(1)
end

-- Player selector Pi over a bar whose Note-local top-left is (bx,by), sized barTex. The wide top half frames
-- a difficulty bar at (PSEL_DIFF_DX,PSEL_DIFF_DY); the small bottom half is centred on an option bar then
-- nudged by (PSEL_OPT_DX,PSEL_OPT_DY).
local function drawPlayerSelector(i, bx, by, barTex, isOption, opacity)
    local tex = G.bars["difficultybarselect" .. i]
    if tex == nil then return end
    if isOption then
        local sw, sh = PSEL_OPT_W, tex.Height - PSEL_SPLIT_Y
        local ox = bx + barTex.Width / 2 - sw / 2 + PSEL_OPT_DX
        local oy = by + barTex.Height / 2 - sh / 2 + PSEL_OPT_DY
        drawTexRectTL(tex, ox, oy, 0, PSEL_SPLIT_Y, sw, sh, opacity)
    else
        drawTexRectTL(tex, bx + PSEL_DIFF_DX, by + PSEL_DIFF_DY,
            0, 0, tex.Width, PSEL_SPLIT_Y, opacity)
    end
end

-- LevelCol number for a difficulty bar whose Note-local top-left is (bx,by).
local function drawLevelNumber(level, difficulty, isVault, bx, by, opacity)
    local str = tostring(level)
    local n   = #str
    local col = isVault and LVL_VAULT_COLOR or (DIFFSELECT_LEVEL_COLORS[difficulty + 1] or COL_WHITE)
    local setcx, setcy = bx + LVL_CX, by + LVL_CY
    for k = 1, n do
        local tex = G.bgtx["diffsel_levelcol" .. string.sub(str, k, k)]
        if tex then
            local off = (k - 1) - (n - 1) / 2
            local cx, cy = nmap(setcx + off * LVL_DIGIT_DX, setcy + off * LVL_DIGIT_DY)
            tex:SetRotation(nAngleDeg)
            tex:SetColor(col)
            tex:SetOpacity(opacity)
            tex:DrawAtAnchor(cx, cy, "center")
            tex:SetRotation(0)
            tex:SetColor(COL_WHITE)
            tex:SetOpacity(1)
        end
    end
end

-- Up to CHARTER_MAX charter names for a difficulty bar whose Note-local top-left is (bx,by). Each name is a
-- single cached texture (GetText), centre-anchored at its transformed point and rotated as one piece.
local function drawCharters(charters, bx, by, opacity)
    if charters == nil then return end
    ensureLabelFonts()
    for k = 1, math.min(CHARTER_MAX, #charters) do
        local sx, sy = nmap(bx + CHARTER_CX + (k - 1) * CHARTER_DX, by + CHARTER_CY + (k - 1) * CHARTER_DY)
        local tex = charterFont:GetText(charters[k], true, CHARTER_MAXW)
        tex:SetRotation(CHARTER_ROT + nAngleDeg)
        tex:SetOpacity(opacity)
        tex:DrawAtAnchor(sx, sy, "center")
        tex:SetRotation(0)
        tex:SetOpacity(1)
    end
end

-- ── Draw panel ────────────────────────────────────────────────────────────────

function M.drawPanel()
    local opacityNorm = G.difficultySelectElemOpacity / 255
    local xshift      = 1920 - G.songSelectShift

    G.bgtx["difficultyselect"]:Draw(xshift, 0)

    -- Note frame + all its children (float + rotate as one group), scrolling in with the transition.
    -- bg_header / bg_overlay_difficulty / title are drawn AFTER, over the Note (see below).
    computeNoteXform()
    drawTexTL(G.bars["diffnote"], 0, 0, opacityNorm)

    if G.selectedSongNode ~= nil then
        -- Option bars, shown left→right as 0 / 1 / Customize. diffIndex 0/1/2 select them (0=back, 1=mods,
        -- 2=customize); the slots run right→left (slot = 2 - diffIndex) so the on-screen order reads 0,1,Customize.
        for o = 0, 2 do
            local pslot = 2 - o
            local bx = OPT_ORIG_X + pslot * OPT_STEP_X
            local by = OPT_ORIG_Y + pslot * OPT_STEP_Y
            local barTex = G.bars["smallbar" .. o]
            for i = 1, CONFIG.PlayerCount do
                if G.diffIndex[i] == o and not (G.activeConfig.mountAISlotToP2 and i == 2) then
                    drawPlayerSelector(i, bx, by, barTex, true, opacityNorm)
                end
            end
            drawTexTL(barTex, bx, by, opacityNorm)
        end

        -- Difficulty bars (compact list; diffIndex 3+ select these).
        for s = 0, #G.diffBars - 1 do
            local barinfo = G.diffBars[s + 1]
            local bx = DBAR_ORIG_X + s * DBAR_STEP_X
            local by = DBAR_ORIG_Y + s * DBAR_STEP_Y
            local tex = barinfo.vault and G.bars["difficultybar7"]
                        or G.bars["difficultybar" .. (barinfo.difficulty + 2)]

            for i = 1, CONFIG.PlayerCount do
                if G.diffIndex[i] == (3 + s) and not (G.activeConfig.mountAISlotToP2 and i == 2) then
                    drawPlayerSelector(i, bx, by, tex, false, opacityNorm)
                end
            end
            drawTexTL(tex, bx, by, opacityNorm)
            drawCharters(barinfo.charters, bx, by, opacityNorm)
            drawLevelNumber(barinfo.level, barinfo.difficulty, barinfo.vault, bx, by, opacityNorm)

            if barinfo.vault and barinfo.vaultName ~= nil and barinfo.vaultName ~= "" then
                ensureLabelFonts()
                local sx, sy = nmap(bx + VAULT_CX, by + VAULT_CY)
                local vtex = vaultFont:GetText(barinfo.vaultName, true, VAULT_MAXW, VAULT_BLACK, VAULT_NOOUTLINE)
                vtex:SetRotation(CHARTER_ROT + nAngleDeg)
                vtex:SetOpacity(opacityNorm)
                vtex:DrawAtAnchor(sx, sy, "center")
                vtex:SetRotation(0)
                vtex:SetOpacity(1)
            end
        end
    end

    -- bg_header (semi-transparent on difficulty select) then bg_overlay_difficulty OVER it, then the song
    -- title/subtitle — all over the Note. Header alpha eases 100%→40% across the songselect→diffselect move.
    local headerAlpha = 1.0 - (1.0 - HEADER_DIFF_ALPHA) * math.min(1, G.songSelectShift / 1920)
    G.bgtx["header"]:SetOpacity(headerAlpha)
    G.bgtx["header"]:Draw(xshift, 0)
    G.bgtx["header"]:SetOpacity(1)
    G.bgtx["overlay_difficulty"]:SetOpacity(opacityNorm)
    G.bgtx["overlay_difficulty"]:DrawAtAnchor(1920, 0, "TopRight")
    if G.selectedSongNode ~= nil then
        G.textLarge:Draw(G.selectedSongNode.Title, 1926 - G.songSelectShift, 0, nil, nil, opacityNorm, 1, 1280)
        G.text:Draw(G.selectedSongNode.Subtitle, 1926 - G.songSelectShift, 67, nil, nil, opacityNorm, 1, 1280)
    end

    -- Characters and nameplates
    do
        local p     = CONFIG.PlayerCount
        local is35  = p > 2
        local ox    = is35 and DIFFSELECT_CHARA_ORIG_X_35P or DIFFSELECT_CHARA_ORIG_X_12P
        local oy    = is35 and DIFFSELECT_CHARA_ORIG_Y_35P or DIFFSELECT_CHARA_ORIG_Y_12P
        local gx    = is35 and DIFFSELECT_CHARA_GAP_X_35P  or DIFFSELECT_CHARA_GAP_X_12P
        local gy    = is35 and DIFFSELECT_CHARA_GAP_Y_35P  or 0
        local s     = is35 and DIFFSELECT_CHARA_SCALE_35P  or DIFFSELECT_CHARA_SCALE_12P
        local r1Count = (p == 5 and 3) or (p > 2 and 2) or p

        -- single-player performance mode shows the best-plays cards down the right edge;
        -- nudge the character/nameplate/mod-icons left so the two don't overlap. Any mode without the
        -- strip (training, AI battle, lobby, multiplayer) keeps the base layout.
        if Replay.isActive() then ox = ox - 200 end

        for i = 0, p - 1 do
            local isRow2 = i >= r1Count
            local r      = isRow2 and 1 or 0
            local cols   = isRow2 and (p - r1Count) or r1Count
            local colIdx = isRow2 and (i - r1Count) or i
            local x      = ox + (colIdx - (cols - 1) / 2) * gx
            local y      = oy + r * gy
            G.drawCharaWithNameplate(i, x, y, s, s, opacityNorm, true)
            local charaX = x + G.bgtx["nameplate_info"].Width / 2 - NAMEPLATE_OFFSET_X
            G.drawPlayerPuchi(i, charaX - PUCHI_OFFSET_X * s, y + G.puchiSineY * s, s, s, opacityNorm)
            if G.modicons_ro ~= nil then
                G.modicons_ro:Draw(x, y + NAMEPLATE_HEIGHT + 4, i, nil, G.difficultySelectElemOpacity)
            end
        end
    end

    -- AI level slider (AI battle only)
    if G.activeConfig.mountAISlotToP2 then
        local cx      = 1490 + xshift
        G.textSmall:Draw("Starting AI Level", cx, 940, nil, nil, opacityNorm, 1, 400, "center")
        G.text:Draw(tostring(CONFIG.AILevel), cx, 980, nil, nil, opacityNorm, 1, 200, "center")
        local ax = G.arrowsDistance
        if CONFIG.AILevel > 1 then
            G.textSmall:Draw("◀", cx - 55 - ax, 980, nil, nil, opacityNorm, 1, 60, "right")
        end
        if CONFIG.AILevel < 10 then
            G.textSmall:Draw("▶", cx + 55 + ax, 980, nil, nil, opacityNorm, 1, 60, "left")
        end
    end

    Replay.draw()
end

-- ── Update handler ────────────────────────────────────────────────────────────
-- Returns "play", "cancel" (unused here; cancel goes back to songselect), or nil.

function M.handleUpdate(ts)
    -- best-plays cards: confirm prompt + mouse hover/scroll/click (single-player performance mode only).
    -- "play" launches a replay; "consume" means the confirm prompt ate this frame's input.
    local rsig = Replay.handleUpdate(ts)
    if rsig == "play" then return "play" end
    if rsig == "consume" then return nil end

    local allDiffsSelected = true
    local canceled         = false

    for i = 1, CONFIG.PlayerCount do
        if G.activeConfig.mountAISlotToP2 and i == 2 then
            -- AI mirrors P1
            G.diffIndex[2]   = G.diffIndex[1]
            G.diffSelected[2] = G.diffSelected[1]
        else
            local inpset = G.inputSets[i]

            if G.diffSelected[i] == false then
                if INPUT:Pressed(inpset.right) or (i == 1 and INPUT:KeyboardPressed("RightArrow")) then
                    G.sounds.Skip:Play()
                    G.diffIndex[i] = (G.diffIndex[i] + 1) % (3 + #G.diffBars)
                elseif INPUT:Pressed(inpset.left) or (i == 1 and INPUT:KeyboardPressed("LeftArrow")) then
                    G.sounds.Skip:Play()
                    G.diffIndex[i] = (G.diffIndex[i] - 1) % (3 + #G.diffBars)
                elseif INPUT:Pressed(inpset.decide1) or INPUT:Pressed(inpset.decide2)
                        or (i == 1 and INPUT:KeyboardPressed("Return")) then
                    if G.diffIndex[i] == 0 then
                        G.sounds.Cancel:Play(); canceled = true
                    elseif G.diffIndex[i] == 1 then
                        G.act_inner["mod_select_dialog"]:Activate(i - 1); return nil
                    elseif G.diffIndex[i] == 2 then
                        G.act_inner["customize_dialog"]:Activate(i - 1); return nil
                    else
                        G.sounds.Decide:Play(); G.diffSelected[i] = true
                    end
                elseif (inpset.cancel ~= nil and INPUT:Pressed(inpset.cancel))
                        or (i == 1 and INPUT:KeyboardPressed("Escape")) then
                    G.sounds.Cancel:Play()
                    if G.diffSelected[i] then G.diffSelected[i] = false
                    else canceled = true end
                end
            end
        end

        if G.diffSelected[i] == false then allDiffsSelected = false end
    end

    if INPUT:KeyboardPressed("F3") then
        G.sounds.Decide:Play(); CONFIG:SetAutoStatus(0, not CONFIG:GetAutoStatus(0))
    end
    if INPUT:KeyboardPressed("F4") and CONFIG.PlayerCount >= 2 then
        G.sounds.Decide:Play(); CONFIG:SetAutoStatus(1, not CONFIG:GetAutoStatus(1))
    end

    if canceled or INPUT:KeyboardPressed("Escape") then
        G.sounds.Decide:Play()
        G.activeScreen = "transition"
        G.startCounter("screen_transition", 1920, 0, -0.5/1920, "none", M.updateTransitionVisuals, function()
            G.activeScreen = "songselect"
        end)
    elseif allDiffsSelected then
        local success = G.selectedSongNode:Mount(
            (G.diffIndex[1] >= 3) and G.diffBars[G.diffIndex[1] - 2].difficulty or 0,
            (G.diffIndex[2] >= 3) and G.diffBars[G.diffIndex[2] - 2].difficulty or 0,
            (G.diffIndex[3] >= 3) and G.diffBars[G.diffIndex[3] - 2].difficulty or 0,
            (G.diffIndex[4] >= 3) and G.diffBars[G.diffIndex[4] - 2].difficulty or 0,
            (G.diffIndex[5] >= 3) and G.diffBars[G.diffIndex[5] - 2].difficulty or 0
        )
        if success then
            G.lastSignal = "play"; return "play"
        else
            G.diffSelected = {false, false, false, false, false}
        end
    end

    -- AI level slider (AI battle only)
    if G.activeConfig.mountAISlotToP2 then
        if INPUT:Pressed("LBlue2P") and CONFIG.AILevel > 1 then
            CONFIG.AILevel = CONFIG.AILevel - 1; G.sounds.Skip:Play()
        elseif INPUT:Pressed("RBlue2P") and CONFIG.AILevel < 10 then
            CONFIG.AILevel = CONFIG.AILevel + 1; G.sounds.Skip:Play()
        end
    end

    return nil
end

return M
