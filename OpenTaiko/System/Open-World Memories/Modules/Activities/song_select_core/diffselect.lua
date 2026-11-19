---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- difficulty select panel draw and update (song_select_core)

local Replay = require("replaylist")
local Color  = require("Color")            -- colour space helpers
local Config    = require("Core.Config")
local SongTitle = require("Songs.Title")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local DS        = Layout.difficulty_select
local Level     = DS.level
local LevelAnim = Animation.difficulty_select.level
local NoteAnim  = Animation.difficulty_select.note
local Colors    = Layout.colors

local M = {}
local G   -- shared state from Script.lua

-- ==== Layout ====

-- positions relative to Note.png top-left, rotating with the Note
local Chara35 = DS.chara_35p
local Chara12 = DS.chara_12p
local DIFFSELECT_CHARA_ORIG_X_35P = Chara35.origin_x
local DIFFSELECT_CHARA_ORIG_Y_35P = Chara35.origin_y
local DIFFSELECT_CHARA_GAP_X_35P  = Chara35.gap_x
local DIFFSELECT_CHARA_GAP_Y_35P  = Chara35.gap_y
local DIFFSELECT_CHARA_SCALE_35P  = Chara35.scale
local DIFFSELECT_CHARA_ROW1_35P   = Chara35.first_row_count   -- index = player count - 2
local DIFFSELECT_CHARA_ORIG_X_12P = Chara12.origin_x
local DIFFSELECT_CHARA_ORIG_Y_12P = Chara12.origin_y
local DIFFSELECT_CHARA_GAP_X_12P  = Chara12.gap_x
local DIFFSELECT_CHARA_SCALE_12P  = Chara12.scale

local function hexList(list)
    local out = {}
    for i, hex in ipairs(list) do out[i] = COLOR:CreateColorFromHex(hex) end
    return out
end

-- LevelCol tint per difficulty (index = difficulty + 1), vault: LVL_VAULT_COLOR
local DIFFSELECT_LEVEL_COLORS = hexList(Colors.level)
local COL_WHITE       = COLOR:CreateColorFromHex("FFFFFFFF")
local LVL_VAULT_COLOR = COLOR:CreateColorFromHex(Colors.level_vault)

-- plus_border.png colour under the level-tinted plus.png
local DIFFSELECT_PLUS_BORDER_COLORS = hexList(Colors.level_plus_border)
local LVL_VAULT_PLUS_BORDER_COLOR   = COLOR:CreateColorFromHex(Colors.level_plus_border_vault)
local VAULT_BLACK     = COLOR:CreateColorFromHex(Colors.vault_text)
local VAULT_NOOUTLINE = COLOR:CreateColorFromHex(Colors.vault_text_outline)

-- Note frame: base position (scroll-relative) and idle
local NOTE_BASE_X      = DS.note.base_x
local NOTE_BASE_Y      = DS.note.base_y
local NOTE_FLOAT_AMP_X = NoteAnim.float_amp_x     -- px
local NOTE_FLOAT_AMP_Y = NoteAnim.float_amp_y     -- px
local NOTE_FLOAT_FREQ_X = NoteAnim.float_freq_x   -- x frequency, in bobs
local NOTE_ROT_AMP     = NoteAnim.rot_amp         -- deg, sway sine amplitude
local NOTE_ROT_PHASE   = NoteAnim.rot_phase       -- rad, sway lead on the bob
local NOTE_FIXED_TILT  = DS.note.fixed_tilt       -- deg, constant tilt (positive = clockwise)

-- option bars (top-left): first position, step
local OPT_ORIG_X = DS.option_bars.origin_x
local OPT_ORIG_Y = DS.option_bars.origin_y
local OPT_STEP_X = DS.option_bars.step_x
local OPT_STEP_Y = DS.option_bars.step_y

-- difficulty bars: first position, step
local DBAR_ORIG_X = DS.difficulty_bars.origin_x
local DBAR_ORIG_Y = DS.difficulty_bars.origin_y
local DBAR_STEP_X = DS.difficulty_bars.step_x
local DBAR_STEP_Y = DS.difficulty_bars.step_y

-- level number, bar-relative: centre (LVL_CX, LVL_CY), digit step (LVL_DIGIT_DX, LVL_DIGIT_DY)
local LVL_CX       = Level.center_x
local LVL_CY       = Level.center_y
local LVL_DIGIT_DX = Level.digit_step_x
local LVL_DIGIT_DY = Level.digit_step_y
-- plus centre from the last digit's slot centre
local LVL_PLUS_DX  = Level.plus_dx
local LVL_PLUS_DY  = Level.plus_dy

-- level gauge: slices in Textures/DifficultyBars/LevelGauge (seg8 Easy/Normal, seg10 rest)
-- lit slices = min(level, slices)
local GAUGE_ROT       = Level.gauge_rotation       -- deg, band rise (CCW)
-- 1 = dark-bar style (vivid first), index = difficulty + 1
local GAUGE_VIVID_FIRST       = Level.gauge_vivid_first
local GAUGE_VIVID_FIRST_VAULT = Level.gauge_vivid_first_vault
local GAUGE_SETS = {
    [8]  = { first = { Level.gauge8_first_x, Level.gauge8_first_y },
             mid   = { Level.gauge8_mid_x,   Level.gauge8_mid_y },
             step  = { Level.gauge8_step_x,  Level.gauge8_step_y },
             last  = { Level.gauge8_last_x,  Level.gauge8_last_y } },
    [10] = { first = { Level.gauge10_first_x, Level.gauge10_first_y },
             mid   = { Level.gauge10_mid_x,   Level.gauge10_mid_y },
             step  = { Level.gauge10_step_x,  Level.gauge10_step_y },
             last  = { Level.gauge10_last_x,  Level.gauge10_last_y } },
}
local GAUGE_EMPTY     = Level.gauge_empty_opacity   -- unlit slices; 0 hides them
-- high-level slices from the checker levels: pulse + space overlay (LevelGauge/seg<n>_space.png)
local GAUGE_FX_LEVEL = {
    [0] = Layout.song_list.checker_level_easy,
    [1] = Layout.song_list.checker_level_normal,
    [2] = Layout.song_list.checker_level_hard,
}
local GAUGE_FX_PULSE_MS  = LevelAnim.gauge_fx_pulse_ms        -- ms per pulse
local GAUGE_FX_PULSE_OP  = LevelAnim.gauge_fx_pulse_opacity   -- pulse peak
local GAUGE_FX_STAGGER   = LevelAnim.gauge_fx_stagger         -- phase step per slice
local GAUGE_FX_LIGHTEN   = Level.gauge_fx_lighten             -- pulse shade toward white
local GAUGE_FX_FRAME_MS  = LevelAnim.gauge_fx_frame_ms        -- space overlay frame time
local GAUGE_FX_SPACE_OP  = Level.gauge_fx_space_opacity       -- space overlay opacity
-- level stars: one per level past the gauge, three bar-relative slots
-- 13+: three; 12: two pulled in by STAR_PAIR_PULL; 11: one between slots 1 and 3
local STAR_FROM_LEVEL = Level.star_from_level
local STAR_SLOTS = {
    { Level.star1_x, Level.star1_y },
    { Level.star2_x, Level.star2_y },
    { Level.star3_x, Level.star3_y },
}
local STAR_PAIR_PULL      = Level.star_pair_pull
-- additive shine pulse + two glints
local STAR_SHINE_MS       = LevelAnim.star_shine_ms
local STAR_SHINE_OP       = LevelAnim.star_shine_opacity
local STAR_GLINT_MS       = LevelAnim.star_glint_ms
local STAR_GLINT_OP       = LevelAnim.star_glint_opacity
local STAR_GLINT_OFFSETS  = {   -- star-relative, bar coordinates
    { Level.star_glint1_dx, Level.star_glint1_dy },
    { Level.star_glint2_dx, Level.star_glint2_dy },
}
local GAUGE_START_MIX = Level.gauge_start_mix          -- light bars, first slice toward white
local GAUGE_END_SAT   = Level.gauge_end_saturation     -- vivid end: saturation multiplier
local GAUGE_END_SAT_ADD = Level.gauge_end_saturation_add   -- vivid end: flat saturation added
local GAUGE_END_LIGHT = Level.gauge_end_lighten        -- dark bars, last slice toward white (HSL lightness)
-- vault gauge base (level colour = band colour)
local LVL_VAULT_GAUGE_COLOR = COLOR:CreateColorFromHex(Colors.level_gauge_vault)

-- n level colour shades (direction by vividFirst), plus pulse shades
local function gaugeShades(levelCol, n, vividFirst)
    local ok, shades, bright = pcall(function()
        local r, g, b = Color.fromEngine(levelCol)
        local vr, vg, vb = Color.saturate(r, g, b, GAUGE_END_SAT, GAUGE_END_SAT_ADD)
        local from, to
        if vividFirst then
            from, to = { vr, vg, vb }, { Color.lighten(r, g, b, GAUGE_END_LIGHT) }
        else
            from, to = { Color.mix(r, g, b, 1, 1, 1, GAUGE_START_MIX) }, { vr, vg, vb }
        end
        local out, up = {}, {}
        for i, c in ipairs(Color.ramp(from, to, n)) do
            out[i] = Color.toEngine(c[1], c[2], c[3])
            up[i]  = Color.toEngine(Color.lighten(c[1], c[2], c[3], GAUGE_FX_LIGHTEN))
        end
        return out, up
    end)
    if ok then return shades, bright end
    local out = {}
    for i = 1, n do out[i] = levelCol end
    return out, out
end
local gaugeShadeCache = {}   -- difficulty or "vault" -> { shades, bright }

-- charter names, bar-relative: first at (CHARTER_CX, CHARTER_CY), step (CHARTER_DX, CHARTER_DY)
local Charter = DS.charter
local CHARTER_CX   = Charter.center_x
local CHARTER_CY   = Charter.center_y
local CHARTER_DX   = Charter.step_x
local CHARTER_DY   = Charter.step_y
local CHARTER_MAXW = Charter.max_width
local CHARTER_ROT  = Charter.rotation   -- deg, CCW
local CHARTER_MAX  = Charter.max_count
local CHARTER_BORDER_DARKEN = Charter.border_darken

-- vault chart name, bar-relative centre
local Vault = DS.vault
local VAULT_CX   = Vault.center_x
local VAULT_CY   = Vault.center_y
local VAULT_MAXW = Vault.max_width

-- one GetText texture per label (rotated glyph quads jitter)
local charterFont, vaultFont
local function ensureLabelFonts()
    if charterFont == nil then charterFont = TEXT:Create(Charter.font_size) end
    if vaultFont   == nil then vaultFont   = TEXT:Create(Vault.font_size) end
end

-- charter outline colour, one COLOR per level colour
local charterBorders = {}
local function charterBorderFor(col)
    local border = charterBorders[col]
    if border == nil then
        local ok, made = pcall(function()
            local r, g, b = Color.fromEngine(col)
            return Color.toEngine(Color.darken(r, g, b, CHARTER_BORDER_DARKEN))
        end)
        border = (ok and made) or COLOR:CreateColorFromRGBA(0, 0, 0, 255)
        charterBorders[col] = border
    end
    return border
end

-- player selector split at PSEL_SPLIT_Y: top frames a difficulty bar, bottom an option bar
-- bottom art spans the left PSEL_OPT_W px (centred on the option bar)
local PSel = DS.player_selector
local PSEL_SPLIT_Y = PSel.split_y
local PSEL_DIFF_DX = PSel.diff_offset_x
local PSEL_DIFF_DY = PSel.diff_offset_y
local PSEL_OPT_W   = PSel.opt_width
local PSEL_OPT_DX  = PSel.opt_offset_x
local PSEL_OPT_DY  = PSel.opt_offset_y

-- header alpha on difficulty select
local HEADER_DIFF_ALPHA  = Layout.header.diffselect_alpha

local DSNameplate = DS.nameplate
local NAMEPLATE_HEIGHT   = DSNameplate.height
local NAMEPLATE_OFFSET_X = DSNameplate.offset_x
local NAMEPLATE_OFFSET_Y = DSNameplate.offset_y
local PUCHI_OFFSET_X     = DSNameplate.puchi_offset_x
local MODICONS_GAP       = DSNameplate.modicons_gap

local Title   = DS.title
local AILevel = DS.ai_level
local Transition = Animation.screen_transition

-- ==== Init ====

function M.init(g)
    G = g
    Replay.init(g)
end

-- ==== Helpers ====

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
    local fadePx     = Transition.fade_px
    local opacity    = 255 - (val * (255 / fadePx))
    G.songSelectElemOpacity      = math.max(0, math.min(255, opacity))
    local diffOpacity = (val - (1920 - fadePx)) * (255 / fadePx)
    G.difficultySelectElemOpacity = math.max(0, math.min(255, diffOpacity))
end

function M.resetTransitionVisuals()
    M.updateTransitionVisuals(0)
end

function M.resetToSongSelect()
    M.resetTransitionVisuals()
    G.activeScreen    = "songselect"
    Replay.reset()
end

-- ==== Note frame transform ====
-- set once per drawPanel, read by the draw helpers

local nAngleDeg, nCosA, nSinA, nNoteX, nNoteY, nPivX, nPivY

local function computeNoteXform()
    local ph  = (G.noteFloatPhase or 0) * math.pi / 180
    local fx  = math.sin(ph * NOTE_FLOAT_FREQ_X) * NOTE_FLOAT_AMP_X
    local fy  = math.sin(ph) * NOTE_FLOAT_AMP_Y
    -- sway minus fixed tilt (SetRotation is CCW)
    nAngleDeg = math.sin(ph + NOTE_ROT_PHASE) * NOTE_ROT_AMP - NOTE_FIXED_TILT
    local rad = nAngleDeg * math.pi / 180
    nCosA, nSinA = math.cos(rad), math.sin(rad)

    local note = G.bars["diffnote"]
    local nw, nh = note.Width, note.Height
    nNoteX = NOTE_BASE_X + (1920 - G.songSelectShift) + fx
    nNoteY = NOTE_BASE_Y + fy
    nPivX, nPivY = nNoteX + nw / 2, nNoteY + nh / 2
end

-- Note-local (lx, ly) -> screen, rotated about the Note centre
-- CCW to match SetRotation (y-down screen)
local function nmap(lx, ly)
    local rx = (nNoteX + lx) - nPivX
    local ry = (nNoteY + ly) - nPivY
    return nPivX + rx * nCosA + ry * nSinA, nPivY - rx * nSinA + ry * nCosA
end

-- screen -> Note-local (inverse of nmap)
local function unmap(sx, sy)
    local dx, dy = sx - nPivX, sy - nPivY
    local rx = dx * nCosA - dy * nSinA
    local ry = dx * nSinA + dy * nCosA
    return rx + nPivX - nNoteX, ry + nPivY - nNoteY
end

-- bar plates: slanted rects in the textures (centre, rise angle, half extents)
local PLATE_DIFF = DS.plate_diff
local PLATE_OPT  = DS.plate_opt
local function onPlate(plate, bx, by, lx, ly)
    local a = math.rad(plate.angle)
    local dx, dy = lx - (bx + plate.cx), ly - (by + plate.cy)
    local u = dx * math.cos(a) - dy * math.sin(a)
    local v = dx * math.sin(a) + dy * math.cos(a)
    return math.abs(u) <= plate.along and math.abs(v) <= plate.across
end

-- Event Mode hides customize, and back during the countdown
local function optionShown(o)
    if G.event == nil or not G.event.on() then return true end
    if o == 2 then return false end
    if o == 0 then return not G.eventCountdown end
    return true
end

-- next selectable index from idx in dir
local function stepIndex(idx, dir)
    local n = 3 + #G.diffBars
    for _ = 1, n do
        idx = (idx + dir) % n
        if idx >= 3 or optionShown(idx) then return idx end
    end
    return idx
end

-- diffIndex under the mouse (options 0..2, difficulties 3+) or nil; later-drawn bars win
local function mouseTarget()
    if nCosA == nil or not INPUT:IsMouseInside() then return nil end
    local mx, my = INPUT:GetMouseXY()
    local lx, ly = unmap(mx, my)
    for s = #G.diffBars - 1, 0, -1 do
        if onPlate(PLATE_DIFF, DBAR_ORIG_X + s * DBAR_STEP_X, DBAR_ORIG_Y + s * DBAR_STEP_Y, lx, ly) then return 3 + s end
    end
    for o = 0, 2 do
        local pslot = 2 - o
        if optionShown(o) and onPlate(PLATE_OPT, OPT_ORIG_X + pslot * OPT_STEP_X, OPT_ORIG_Y + pslot * OPT_STEP_Y, lx, ly) then return o end
    end
    return nil
end

-- texture at Note-local top-left (ox, oy), rotating with the Note
-- no floor: sub-pixel positions keep the rotation smooth
local function drawTexTL(tex, ox, oy, opacity)
    local cx, cy = nmap(ox + tex.Width / 2, oy + tex.Height / 2)
    tex:SetRotation(nAngleDeg)
    tex:SetOpacity(opacity)
    tex:DrawAtAnchor(cx, cy, "center")
    tex:SetRotation(0)
    tex:SetOpacity(1)
end

-- sub-rect at Note-local top-left (ox, oy)
local function drawTexRectTL(tex, ox, oy, sx, sy, sw, sh, opacity)
    local cx, cy = nmap(ox + sw / 2, oy + sh / 2)
    tex:SetRotation(nAngleDeg)
    tex:SetOpacity(opacity)
    tex:DrawRectAtAnchor(cx, cy, sx, sy, sw, sh, "center")
    tex:SetRotation(0)
    tex:SetOpacity(1)
end

-- player selector i over the bar at Note-local (bx, by)
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

-- level number for the bar at Note-local (bx, by)
local function drawLevelNumber(level, isPlus, difficulty, isVault, bx, by, opacity)
    local str = tostring(level)
    local n   = #str
    local col = isVault and LVL_VAULT_COLOR or (DIFFSELECT_LEVEL_COLORS[difficulty + 1] or COL_WHITE)
    local borderCol = isVault and LVL_VAULT_PLUS_BORDER_COLOR or (DIFFSELECT_PLUS_BORDER_COLORS[difficulty + 1] or COL_WHITE)
    local setcx, setcy = bx + LVL_CX, by + LVL_CY
    local function glyph(tex, lx, ly, tint)
        local cx, cy = nmap(lx, ly)
        tex:SetRotation(nAngleDeg)
        tex:SetColor(tint)
        tex:SetOpacity(opacity)
        tex:DrawAtAnchor(cx, cy, "center")
        tex:SetRotation(0)
        tex:SetColor(COL_WHITE)
        tex:SetOpacity(1)
    end
    for k = 1, n do
        local tex = G.bgtx["diffsel_levelcol" .. string.sub(str, k, k)]
        if tex then
            local off = (k - 1) - (n - 1) / 2
            glyph(tex, setcx + off * LVL_DIGIT_DX, setcy + off * LVL_DIGIT_DY, col)
        end
    end
    if isPlus and G.bgtx["diffsel_levelcol+"] then
        local off = (n - 1) / 2
        local px, py = setcx + off * LVL_DIGIT_DX + LVL_PLUS_DX, setcy + off * LVL_DIGIT_DY + LVL_PLUS_DY
        if G.bgtx["diffsel_levelcol+border"] then glyph(G.bgtx["diffsel_levelcol+border"], px, py, borderCol) end
        glyph(G.bgtx["diffsel_levelcol+"], px, py, col)
    end
end

-- level gauge for the bar at Note-local (bx, by)
local function drawLevelGauge(level, difficulty, isVault, bx, by, opacity)
    local n = (not isVault and difficulty <= 1) and 8 or 10
    local tex, set = G.bgtx["diffsel_gauge" .. n], GAUGE_SETS[n]
    if tex == nil or set == nil then return end
    local cw, ch = tex.Width / 3, tex.Height
    local key = isVault and "vault" or difficulty
    local cached = gaugeShadeCache[key]
    if cached == nil then
        local col = isVault and LVL_VAULT_GAUGE_COLOR or (DIFFSELECT_LEVEL_COLORS[difficulty + 1] or COL_WHITE)
        local vividFirst = (isVault and GAUGE_VIVID_FIRST_VAULT or (GAUGE_VIVID_FIRST[difficulty + 1] or 0)) ~= 0
        cached = { gaugeShades(col, n, vividFirst) }
        gaugeShadeCache[key] = cached
    end
    local shades, bright = cached[1], cached[2]
    local lit = math.max(0, math.min(n, math.floor(tonumber(level) or 0)))
    -- high-level fx start slice (nil = none)
    local fxFrom = (not isVault) and GAUGE_FX_LEVEL[difficulty] or nil
    local fxTex, fxFrame, fxRows, t
    if fxFrom ~= nil and lit >= fxFrom then
        fxTex = G.bgtx["diffsel_gauge" .. n .. "_space"]
        t = G.nowMs or 0
        fxRows = (fxTex ~= nil and fxTex.Height > 0) and math.max(1, math.floor(fxTex.Height / ch)) or 1
        fxFrame = math.floor(t / GAUGE_FX_FRAME_MS) % fxRows
        if fxTex ~= nil then fxTex:SetRotation(GAUGE_ROT + nAngleDeg) end
    else
        fxFrom = nil
    end
    tex:SetRotation(GAUGE_ROT + nAngleDeg)
    for i = 1, n do
        local a = (i <= lit) and opacity or (opacity * GAUGE_EMPTY)
        if a > 0 then
            local cell, cx, cy
            if i == 1 then cell, cx, cy = 0, set.first[1], set.first[2]
            elseif i == n then cell, cx, cy = 2, set.last[1], set.last[2]
            else cell, cx, cy = 1, set.mid[1] + (i - 2) * set.step[1], set.mid[2] + (i - 2) * set.step[2] end
            local sx, sy = nmap(bx + cx, by + cy)
            tex:SetColor(shades[i])
            tex:SetOpacity(a)
            tex:DrawRectAtAnchor(sx, sy, cell * cw, 0, cw, ch, "center")
            if fxFrom ~= nil and i >= fxFrom and i <= lit then
                -- pulse: lighter shade over the slice, staggered
                local pulse = 0.5 + 0.5 * math.sin(2 * math.pi * t / GAUGE_FX_PULSE_MS + i * GAUGE_FX_STAGGER)
                tex:SetColor(bright[i])
                tex:SetOpacity(a * GAUGE_FX_PULSE_OP * pulse)
                tex:DrawRectAtAnchor(sx, sy, cell * cw, 0, cw, ch, "center")
                if fxTex ~= nil then
                    fxTex:SetOpacity(a * GAUGE_FX_SPACE_OP)
                    fxTex:DrawRectAtAnchor(sx, sy, cell * cw, fxFrame * ch, cw, ch, "center")
                end
            end
        end
    end
    tex:SetRotation(0)
    tex:SetColor(COL_WHITE)
    tex:SetOpacity(1)
    if fxTex ~= nil then fxTex:SetRotation(0); fxTex:SetOpacity(1) end
end

-- level stars for the bar at Note-local (bx, by) (star<bar>.png, optional)
local function drawLevelStars(level, bar, bx, by, opacity)
    local lv = math.floor(tonumber(level) or 0)
    if lv < STAR_FROM_LEVEL then return end
    local tex = G.bgtx["diffsel_star" .. bar]
    if tex == nil then return end
    local count = math.min(#STAR_SLOTS, lv - STAR_FROM_LEVEL + 1)
    local s1, s3 = STAR_SLOTS[1], STAR_SLOTS[3]
    local mx, my = (s1[1] + s3[1]) / 2, (s1[2] + s3[2]) / 2
    local t = G.nowMs or 0
    local glint = G.bgtx["diffsel_star_glint"]
    for i = 1, count do
        local px, py
        if count == #STAR_SLOTS then px, py = STAR_SLOTS[i][1], STAR_SLOTS[i][2]
        elseif count == 2 then
            local s = (i == 1) and s1 or s3
            px, py = s[1] + (mx - s[1]) * STAR_PAIR_PULL, s[2] + (my - s[2]) * STAR_PAIR_PULL
        else px, py = mx, my end
        local shine = 0.5 + 0.5 * math.sin(2 * math.pi * t / STAR_SHINE_MS + i * LevelAnim.star_shine_stagger)
        local sx, sy = nmap(bx + px, by + py)
        tex:SetRotation(nAngleDeg)
        tex:SetOpacity(opacity)
        tex:DrawAtAnchor(sx, sy, "center")
        tex:SetBlendMode("add")
        tex:SetOpacity(opacity * STAR_SHINE_OP * shine)
        tex:DrawAtAnchor(sx, sy, "center")
        tex:SetBlendMode("normal")
        tex:SetRotation(0)
        tex:SetOpacity(1)
        if glint ~= nil then
            glint:SetBlendMode("add")
            for k, off in ipairs(STAR_GLINT_OFFSETS) do
                -- one blink per STAR_GLINT_MS, staggered per star and glint
                local gp = (t / STAR_GLINT_MS + i * LevelAnim.star_glint_stagger + k * 0.5) % 1
                local blink = math.sin(gp * math.pi)
                blink = blink * blink * blink
                if blink > LevelAnim.star_glint_min then
                    local gx, gy = nmap(bx + px + off[1], by + py + off[2])
                    local gs = LevelAnim.star_glint_scale_min + LevelAnim.star_glint_scale_grow * blink
                    glint:SetRotation(nAngleDeg + gp * LevelAnim.star_glint_spin_deg)
                    glint:SetScale(gs, gs)
                    glint:SetOpacity(opacity * STAR_GLINT_OP * blink)
                    glint:DrawAtAnchor(gx, gy, "center")
                end
            end
            glint:SetBlendMode("normal")
            glint:SetRotation(0)
            glint:SetScale(1, 1)
            glint:SetOpacity(1)
        end
    end
end

-- charter names for the bar at Note-local (bx, by)
local function drawCharters(charters, difficulty, isVault, bx, by, opacity)
    if charters == nil then return end
    ensureLabelFonts()
    local col = isVault and LVL_VAULT_COLOR or (DIFFSELECT_LEVEL_COLORS[difficulty + 1] or COL_WHITE)
    local border = charterBorderFor(col)
    for k = 1, math.min(CHARTER_MAX, #charters) do
        local sx, sy = nmap(bx + CHARTER_CX + (k - 1) * CHARTER_DX, by + CHARTER_CY + (k - 1) * CHARTER_DY)
        local tex = charterFont:GetText(charters[k], true, CHARTER_MAXW, COL_WHITE, border)
        tex:SetRotation(CHARTER_ROT + nAngleDeg)
        tex:SetOpacity(opacity)
        tex:DrawAtAnchor(sx, sy, "center")
        tex:SetRotation(0)
        tex:SetOpacity(1)
    end
end

-- ==== Draw panel ====

function M.drawPanel()
    local opacityNorm = G.difficultySelectElemOpacity / 255
    local xshift      = 1920 - G.songSelectShift

    G.bgtx["difficultyselect"]:Draw(xshift, 0)

    -- Note frame and children (header layers drawn after)
    computeNoteXform()
    drawTexTL(G.bars["diffnote"], 0, 0, opacityNorm)

    if G.selectedSongNode ~= nil then
        -- option bars: diffIndex 0 back, 1 mods, 2 customize
        -- slot = 2 - diffIndex (slots run right to left)
        for o = 0, 2 do
            if optionShown(o) then
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
        end

        -- difficulty bars (diffIndex 3+)
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
            drawCharters(barinfo.charters, barinfo.difficulty, barinfo.vault, bx, by, opacityNorm)
            drawLevelNumber(barinfo.level, barinfo.isplus, barinfo.difficulty, barinfo.vault, bx, by, opacityNorm)
            drawLevelGauge(barinfo.level, barinfo.difficulty, barinfo.vault, bx, by, opacityNorm)
            drawLevelStars(barinfo.level, barinfo.vault and 7 or (barinfo.difficulty + 2), bx, by, opacityNorm)

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

    -- header layers and title over the Note
    local headerAlpha = 1.0 - (1.0 - HEADER_DIFF_ALPHA) * math.min(1, G.songSelectShift / 1920)
    G.bgtx["header"]:SetOpacity(headerAlpha)
    G.bgtx["header"]:Draw(xshift, 0)
    G.bgtx["header"]:SetOpacity(1)
    G.bgtx["overlay_difficulty"]:SetOpacity(opacityNorm)
    G.bgtx["overlay_difficulty"]:DrawAtAnchor(1920, 0, "TopRight")
    if G.selectedSongNode ~= nil then
        G.textLarge:Draw(SongTitle.of(G.selectedSongNode), Title.x - G.songSelectShift, Title.y, nil, nil, opacityNorm, 1, Title.max_width)
        G.text:Draw(SongTitle.subtitleOf(G.selectedSongNode), Title.x - G.songSelectShift, Title.subtitle_y, nil, nil, opacityNorm, 1, Title.max_width)
    end

    -- characters and nameplates
    do
        local p     = CONFIG.PlayerCount
        local is35  = p > 2
        local ox    = is35 and DIFFSELECT_CHARA_ORIG_X_35P or DIFFSELECT_CHARA_ORIG_X_12P
        local oy    = is35 and DIFFSELECT_CHARA_ORIG_Y_35P or DIFFSELECT_CHARA_ORIG_Y_12P
        local gx    = is35 and DIFFSELECT_CHARA_GAP_X_35P  or DIFFSELECT_CHARA_GAP_X_12P
        local gy    = is35 and DIFFSELECT_CHARA_GAP_Y_35P  or 0
        local s     = is35 and DIFFSELECT_CHARA_SCALE_35P  or DIFFSELECT_CHARA_SCALE_12P
        local r1Count = is35 and DIFFSELECT_CHARA_ROW1_35P[p - 2] or p

        -- shift left of the best-plays cards (replay list active)
        if Replay.isActive() then ox = ox - DS.replay_shift_x end

        for i = 0, p - 1 do
            local isRow2 = i >= r1Count
            local r      = isRow2 and 1 or 0
            local cols   = isRow2 and (p - r1Count) or r1Count
            local colIdx = isRow2 and (i - r1Count) or i
            local x      = ox + (colIdx - (cols - 1) / 2) * gx
            local y      = oy + r * gy
            G.drawCharaWithNameplate(i, x, y, s, s, opacityNorm, true)
            local charaX = x + G.bgtx["nameplate_info"].Width / 2 - NAMEPLATE_OFFSET_X
            G.drawPlayerPuchi(i, charaX - PUCHI_OFFSET_X * s, y + G.puchiSineY * s, s, s, opacityNorm, false, G.puchiIdxFrame)
            if G.modicons_ro ~= nil then
                G.modicons_ro:Draw(x, y + NAMEPLATE_HEIGHT + MODICONS_GAP, i, nil, G.difficultySelectElemOpacity)
            end
        end
    end

    -- AI level slider (AI battle)
    if G.activeConfig.mountAISlotToP2 then
        local cx      = AILevel.x + xshift
        G.textSmall:Draw(G.skinText("SONGSELECT_AI_LEVEL", "Starting AI Level"), cx, AILevel.label_y, nil, nil, opacityNorm, 1, AILevel.label_max_width, "center")
        G.text:Draw(tostring(CONFIG.AILevel), cx, AILevel.value_y, nil, nil, opacityNorm, 1, AILevel.value_max_width, "center")
        local ax = G.arrowsDistance
        if CONFIG.AILevel > 1 then
            G.textSmall:Draw("◀", cx - AILevel.arrow_gap - ax, AILevel.value_y, nil, nil, opacityNorm, 1, AILevel.arrow_max_width, "right")
        end
        if CONFIG.AILevel < 10 then
            G.textSmall:Draw("▶", cx + AILevel.arrow_gap + ax, AILevel.value_y, nil, nil, opacityNorm, 1, AILevel.arrow_max_width, "left")
        end
    end

    Replay.draw()
end

-- ==== Update handler ====
-- returns "play", "cancel" or nil

function M.handleUpdate(ts)
    -- best-plays cards: "play" starts a replay, "consume" = input used by the prompt
    local rsig = Replay.handleUpdate(ts)
    if rsig == "play" then return "play" end
    if rsig == "consume" then return nil end

    local allDiffsSelected = true
    local canceled         = false
    local decided          = false
    local uniNavPlayer = 1

    -- Event Mode timeout: undecided players take the hovered (or first) difficulty
    if G.eventExpired then
        G.eventExpired = false
        for i = 1, CONFIG.PlayerCount do
            if not G.diffSelected[i] then
                if G.diffIndex[i] < 3 then G.diffIndex[i] = 3 end
                G.diffSelected[i] = true
            end
        end
    end

    for i = 1, CONFIG.PlayerCount do
        if i == uniNavPlayer and G.diffSelected[i] then
            uniNavPlayer = i + 1
        end
        local navPn = G.NavInput.p[i]

        if G.activeConfig.mountAISlotToP2 and i == 2 then
            -- AI mirrors P1
            G.diffIndex[2]   = G.diffIndex[1]
            G.diffSelected[2] = G.diffSelected[1]

            -- AI level slider
            if navPn.left(i == uniNavPlayer) and CONFIG.AILevel > 1 then
                CONFIG.AILevel = CONFIG.AILevel - 1; G.sounds.Skip:Play()
            elseif navPn.right(i == uniNavPlayer) and CONFIG.AILevel < 10 then
                CONFIG.AILevel = CONFIG.AILevel + 1; G.sounds.Skip:Play()
            end
        elseif G.diffSelected[i] == false then
            -- P1 mouse: hover selects, click decides
            local click = false
            if i == 1 and G.mouseAllowed() then
                local target = mouseTarget()
                if target ~= nil and target ~= G.diffIndex[1] then G.sounds.Skip:Play(); G.diffIndex[1] = target end
                click = target ~= nil and INPUT:MousePressed("Left")
            end
            if navPn.right(i == uniNavPlayer) then
                G.sounds.Skip:Play()
                G.diffIndex[i] = stepIndex(G.diffIndex[i], 1)
            elseif navPn.left(i == uniNavPlayer) then
                G.sounds.Skip:Play()
                G.diffIndex[i] = stepIndex(G.diffIndex[i], -1)
            elseif navPn.decide(i == uniNavPlayer) or click then
                if G.diffIndex[i] == 0 then
                    canceled = true
                elseif G.diffIndex[i] == 1 then
                    G.sounds.Decide:Play()
                    G.act_inner["mod_select_dialog"]:Activate(i - 1); return nil
                elseif G.diffIndex[i] == 2 then
                    G.sounds.Decide:Play()
                    G.act_inner["customize_dialog"]:Activate(i - 1); return nil
                else
                    decided = true; G.diffSelected[i] = true
                end
            elseif navPn.cancel(i == uniNavPlayer) then
                if G.diffSelected[i] then G.diffSelected[i] = false; G.sounds.Cancel:Play()
                elseif not optionShown(0) then G.sounds.Error:Play()   -- no back during the countdown
                else canceled = true end
            end
                
            if i <= 2 and G.shortcuts.pressed("auto_p" .. i) then
                G.sounds.Decide:Play(); CONFIG:SetAutoStatus(i - 1, not CONFIG:GetAutoStatus(i - 1))
            end
        end

        if G.diffSelected[i] == false then allDiffsSelected = false end
    end

    if canceled or (G.NavInput.cancel() and optionShown(0)) then
        G.sounds.Cancel:Play()
        G.activeScreen = "transition"
        G.startCounter("screen_transition", 1920, 0, -Transition.duration_s / 1920, "none", M.updateTransitionVisuals, function()
            M.resetToSongSelect()
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
            G.sounds.SongDecide:Play()
            G.lastSignal = "play"; return "play"
        else
            G.sounds.Error:Play()
            G.diffSelected = {false, false, false, false, false}
        end
    elseif decided then
        G.sounds.Decide:Play()
    end

    return nil
end

return M
