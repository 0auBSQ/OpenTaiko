---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- results_dan ROActivity: the result screen of a dan play.
-- The result stage calls activate(info) with a LuaResultInfo, update() and draw() every frame, skip() on decide
-- (true while still animating), swap() on left or right (true when the exams slide away or back) and deactivate().

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local E = Layout.exam

local W = 1920
local tex = {}
local fonts = {}
local col = {}
local info = nil
local active = false
local clock = 0                 -- milliseconds since activate
local phase = 0                 -- the same, up to phaseEnd; a skip jumps it there
local phaseEnd = 0
local songCount = 0
local swapShift, swapAt = 0, nil

-- ── helpers ──────────────────────────────────────────────────────────────────────────────────────────

local function load(path)
    if not STORAGE:FileExists(path) then return nil end
    return TEXTURE:CreateTexture(path)
end

local function clamp(x, a, b) return math.max(a, math.min(b, x)) end

local function digits(n)
    n = math.max(0, math.floor(n))
    local list = {}
    repeat
        list[#list + 1] = n % 10
        n = math.floor(n / 10)
    until n == 0
    return list
end

-- a count in Number.png's digits, its last digit's left edge two digits right of x
local function drawNumber(x, y, value, opacity)
    local t = tex.number
    if t == nil then return end
    local w, h = t.Width / 11, t.Height / 2
    local iv = Layout.numbers.interval
    t:SetOpacity(opacity)
    for j, d in ipairs(digits(value)) do
        t:DrawRectAtAnchor(x - iv[1] * (j - 1) + w * 2 + w / 2, y - iv[2] * (j - 1) + h / 2, d * w, 0, w, h, "center")
    end
    t:SetOpacity(1)
end

-- the score in Score_Number.png's digits, its last digit centred on x
local function drawScore(x, y, value)
    local t = tex.scoreNumber
    if t == nil then return end
    local w, h = t.Width / 10, t.Height
    local iv = Layout.score.interval
    for j, d in ipairs(digits(value)) do
        t:DrawRectAtAnchor(x - iv[1] * (j - 1), y - iv[2] * (j - 1) + h / 2, d * w, 0, w, h, "center")
    end
end

-- a song's level in the dan select's digits
local function drawLevel(x, y, value, opacity)
    local t = tex.level
    if t == nil then return end
    local w, h = t.Width / 10, t.Height
    local iv = Layout.songs.level.interval
    t:SetOpacity(opacity)
    for j, d in ipairs(digits(value)) do
        t:DrawRectAtAnchor(x - (iv[1] * (j - 1) + w / 2), y - (iv[2] * (j - 1) - w / 2), d * w, 0, w, h, "topleft")
    end
    t:SetOpacity(1)
end

-- the dan select's song badge: the n-th song's colour and its "1st", "2nd"...
local ORDINALS = { "st", "nd", "rd" }
local function drawDanIcon(n, x, y, opacity, scale, fade)
    local I = Layout.dan_icon
    local c = I.colors[math.min(n, #I.colors)]
    local icon = tex.danIcon
    local f = tex.danIconFade
    if fade and f ~= nil then
        f:SetScale(scale, scale)
        f:SetOpacity(opacity)
        f:SetColor(c[1] / 255, c[2] / 255, c[3] / 255)
        f:DrawAtAnchor(x - (icon ~= nil and icon.Width / 2 or 0) * scale, y, "left")
        f:SetColor(1, 1, 1)
        f:SetOpacity(1)
        f:SetScale(1, 1)
    end
    if icon == nil then return end
    icon:SetScale(scale, scale)
    icon:SetOpacity(opacity)
    icon:SetColor(c[1] / 255, c[2] / 255, c[3] / 255)
    icon:DrawAtAnchor(x, y, "center")
    icon:SetColor(1, 1, 1)
    icon:SetOpacity(1)
    icon:SetScale(1, 1)
    fonts.icon:Draw(n .. (ORDINALS[n] or "th"), x + I.title_offset[1], y + I.title_offset[2], col.white, col.black, opacity, scale, 0, "center")
end

-- ── the exams, as the play's exam bars draw them ─────────────────────────────────────────────────────

-- an exam's digits, left to right from x, standing on y: big ones for the amount, small ones for the condition
local function drawExamNumber(value, x, y, padding, big, status, sx, sy)
    local t = big and tex.examNumber or tex.examSmallNumber
    if t == nil or value < 0 then return end
    local size = big and E.number_size or E.small_number_size
    local row = (big and status == 2) and size[2] or 0
    if value == 0 or status == 0 then t:SetColor(0.5, 0.5, 0.5) end
    t:SetScale(sx, sy)
    local s = tostring(math.floor(value))
    for i = 1, #s do
        local d = tonumber(s:sub(i, i))
        t:DrawRectAtAnchor(x + (i - 1) * padding - size[1] / 2, y - size[2] * sy, math.max(0, size[1] * d - 1), row, size[1], size[2], "topleft")
    end
    t:SetScale(1, 1)
    t:SetColor(1, 1, 1)
end

local function drawMiniNumber(value, x, y, status)
    local t = tex.examMiniNumber
    if t == nil or value < 0 then return end
    local size = E.mini_number_size
    local row = status == 2 and size[2] or 0
    local s = tostring(math.floor(value))
    for i = 1, #s do
        local d = tonumber(s:sub(i, i))
        t:DrawRectAtAnchor(x + (i - 1) * E.mini_number_padding - size[1] / 2, y - size[2], math.max(0, size[1] * d - 1), row, size[1], size[2], "topleft")
    end
end

-- a gauge filled to percent from its bottom left corner
local function drawBar(t, x, y, percent, sx, anchor)
    if t == nil or percent <= 0 then return end
    t:SetScale(sx, 1)
    t:DrawRectAtAnchor(x, y, 0, 0, math.floor(percent * t.Width / 100), t.Height, anchor or "bottomleft")
    t:SetScale(1, 1)
end

-- a gauge texture for a status: the reach one is never shown once the play is over
local function gaugeFor(status)
    return status >= 1 and tex.examGauge.clear or tex.examGauge.normal
end

-- a per-song exam's bars for the two songs before the one shown
local function drawSongBars(exam, barX, barY, small)
    local S = E.scales
    local now = info.DanSongReached
    if exam:GetSong(info.DanExamSong) == nil then return end
    local baseSx = small and S.small_base or 1
    for j = 0, songCount - 2 do
        local e = exam:GetSong(j)
        if e ~= nil and j > now - 3 and j <= now - 1 then
            local mx = barX + E.small_base_offset_x[small and 2 or 1]
            local my = barY + E.small_base_offset_y[small and 2 or 1] + (j % 2) * E.small_bar_gap - (E.size[2] + E.padding)
            if tex.examSmallBase ~= nil then
                tex.examSmallBase:SetScale(baseSx, 1)
                tex.examSmallBase:Draw(mx, my)
                tex.examSmallBase:SetScale(1, 1)
            end
            local C = E.small_cymbol
            if tex.examSmallCymbol ~= nil then
                tex.examSmallCymbol:DrawRectAtAnchor(mx + C.offset[1], my + C.offset[2], 0, j * C.cell[2], C.cell[1], C.cell[2], "topleft")
            end
            local bar = (e.Status == 2 and tex.rainbow[1]) or gaugeFor(e.Status)
            if bar ~= nil and e.Percent > 0 then
                bar:SetScale(S.mini_gauge_x * baseSx * (small and S.mini_gauge_small or 1), S.mini_gauge_y)
                bar:DrawRectAtAnchor(mx + E.mini_gauge_offset[1], my + E.mini_gauge_offset[2], 0, 0, math.floor(e.Percent * bar.Width / 100), bar.Height, "topleft")
                bar:SetScale(1, 1)
            end
            drawMiniNumber(e.Amount, mx + E.mini_number_offset[1], my + E.mini_number_offset[2], e.Status)
            drawDanIcon(j + 1, mx + E.icon_offset_mini[1], my + E.icon_offset_mini[2], 1, S.icon_mini, false)
        end
    end
end

local function drawExams(offX)
    local S = E.scales
    local count, noGauge = info.ExamCount, 0
    for k = 0, count - 1 do
        if info:GetExam(k).Type ~= 0 then noGauge = noGauge + 1 end
    end
    local pos = -1
    for k = 0, count - 1 do
        local exam = info:GetExam(k)
        local isGauge = exam.Type == 0
        if not isGauge then pos = pos + 1 end
        -- a fourth exam and more halve the rows; the gauge exam has the top row
        local small = pos >= 3 or (noGauge > 3 and noGauge % 3 > pos) or noGauge == 6
        local yIndex = (pos % 3) + 1
        if isGauge then yIndex, small = 0, false end
        local second = pos >= 3 and 2 or 1
        local barX = E.origin[1] + offX + E.base_offset_x[second]
        local barY = E.origin[2] + E.base_offset_y[second] + (E.size[2] + E.padding) * yIndex
        local lowerY = barY + E.size[2] + E.padding
        local perSong = exam.IsPerSong

        local base = small and tex.examBaseSmall or tex.examBase
        if base ~= nil then
            local hh = base.Height / 2
            base:DrawRectAtAnchor(barX, barY, 0, perSong and hh or 0, base.Width, hh, "topleft")
        end
        if perSong then
            drawSongBars(exam, barX, barY, small)
            local C = E.cymbol
            if tex.examCymbol ~= nil then
                tex.examCymbol:DrawRectAtAnchor(barX + C.offset[1], lowerY + C.offset[2], 0, C.cell[2] * info.DanExamSong, C.cell[1], C.cell[2], "topleft")
            end
        end

        -- the bar: rainbow once gold, hidden once failed
        local sx
        if perSong then sx = small and S.per_song_small or S.per_song else sx = small and S.small or 1 end
        local gx, gy = barX + E.gauge_offset[1], lowerY - E.gauge_offset[2]
        local frames = tex.rainbow
        if exam.Status == 2 and #frames > 0 then
            local i = math.floor(clock / Animation.rainbow_frame) % #frames
            drawBar(frames[i + 1], gx, gy, exam.Percent, sx)
            if i == #frames - 1 then drawBar(frames[1], gx, gy, exam.Percent, sx) end
        elseif exam.Status >= 1 then
            drawBar(tex.examGauge.clear, gx, gy, exam.Percent, sx)
        end

        local nx = small and S.small_number_x or 1
        local ny = small and S.small_number_y or 1
        drawExamNumber(exam.Amount, barX + E.number_offset[1], lowerY - E.number_offset[2], math.floor(E.number_padding * nx),
            true, exam.Status, E.number_scale * nx, E.number_scale * ny)
        if perSong then drawDanIcon(info.DanExamSong + 1, barX + E.icon_offset[1], barY + E.icon_offset[2], 1, S.icon, true) end

        -- the condition: more or less than, its value and the exam's name
        local cx, cy = barX + E.condition_offset[1], lowerY - E.condition_offset[2]
        if tex.examRange ~= nil then
            local R = E.range_size
            tex.examRange:DrawRectAtAnchor(cx - tex.examRange.Width, cy, 0, R[2] * exam.Range, R[1], R[2], "bottomleft")
        end
        local pad = math.floor(E.small_number_padding * E.condition_number_scale)
        local value = exam.Value
        drawExamNumber(value, cx - E.range_padding - #tostring(value) * pad, cy - 1, pad, false, exam.Status, 1, 1)
        local tx = cx - (tex.examType ~= nil and tex.examType.Width or 0) + E.type_offset[1]
        local ty = cy - E.type_offset[2]
        if tex.examType ~= nil then
            tex.examType:DrawRectAtAnchor(tx, ty, 0, 0, E.type_size[1], E.type_size[2], "bottomleft")
        end
        fonts.exam:Draw(LANG:GetExamName(exam.Type), tx + E.type_size[1] / 2, ty - E.type_size[2] / 2, col.examText, col.examOutline, 1, 1, 1000, "center")

        if exam.Status == 0 and tex.examFailed ~= nil then
            tex.examFailed:SetScale(small and S.failed_small or 1, 1)
            tex.examFailed:DrawAtAnchor(gx, gy, "bottomleft")
            tex.examFailed:SetScale(1, 1)
        end
    end
end

-- ── the screen ───────────────────────────────────────────────────────────────────────────────────────

local function drawSong(i, offX, opacity)
    local L = Layout.songs
    local r = i % L.per_page + 1
    local s = info:GetDanSong(i)
    local panel = tex.songPanel
    if panel ~= nil then
        local h = panel.Height / 3
        panel:SetOpacity(opacity)
        panel:DrawRectAtAnchor(L.panel.x[r] + offX, L.panel.y[r], 0, h * math.min(i, 2), panel.Width, h, "topleft")
        panel:SetOpacity(1)
    end
    local cym = tex.difficulty
    if cym ~= nil then
        local cw = cym.Width / 5
        cym:SetOpacity(opacity)
        cym:DrawRectAtAnchor(L.difficulty.x[r] + offX, L.difficulty.y[r], s.Difficulty * cw, 0, cw, cym.Height, "center")
        cym:SetOpacity(1)
    end
    drawLevel(L.level.x[r] + offX, L.level.y[r], s.Level, opacity)
    drawNumber(L.good.x[r] + offX, L.good.y[r], s.Good, opacity)
    drawNumber(L.ok.x[r] + offX, L.ok.y[r], s.Ok, opacity)
    drawNumber(L.bad.x[r] + offX, L.bad.y[r], s.Bad, opacity)
    drawNumber(L.roll.x[r] + offX, L.roll.y[r], s.Roll, opacity)
    local T = L.title
    fonts.song:Draw(s.Title, T.x[r] + offX, T.y[r], col.song, col.songOutline, opacity, 1, T.max_width, "topleft")
    drawDanIcon(i + 1, L.icon.x[r] + offX, L.icon.y[r], opacity, 1, false)
end

-- the page of songs shown and its fade: past three songs, the pages take turns
local function songPage()
    local per = Layout.songs.per_page
    local P = Animation.pages
    if songCount <= per or clock < P.first then return 0, 1 end
    local t = clock - P.first
    local pages = math.ceil(songCount / per)
    return (math.floor(t / P.each) + 1) % pages, math.min(t % P.each, P.fade) / P.fade
end

local function examOffset()
    if swapShift ~= 0 then
        local k = clamp((clock - swapAt) / Animation.swap, 0, 1)
        return (swapShift < 0 and (1 - k) or k) * W
    end
    local A = Animation.exams
    return math.max(0, A.at + A.step * songCount - phase) * A.px_per_ms
end

local function drawPanel(offX)
    local P = Layout.panel
    if tex.stateBase ~= nil then tex.stateBase:Draw(P[1] + offX, P[2]) end
    if tex.stateMain ~= nil then tex.stateMain:Draw(P[1] + offX, P[2]) end
    local T = Layout.totals
    drawNumber(T.good[1] + offX, T.good[2], info:GetGoodCount(0), 1)
    drawNumber(T.ok[1] + offX, T.ok[2], info:GetOkCount(0), 1)
    drawNumber(T.bad[1] + offX, T.bad[2], info:GetBadCount(0), 1)
    drawNumber(T.roll[1] + offX, T.roll[2], info:GetRollCount(0), 1)
    drawNumber(T.combo[1] + offX, T.combo[2], info:GetMaxCombo(0), 1)
    drawNumber(T.hits[1] + offX, T.hits[2], info.TotalHits, 1)
    drawScore(T.score[1] + offX, T.score[2], info:GetScore(0))
    drawExams(offX)
end

-- the pass or fail mark: a pass grows in and settles, a fail drops in
local function drawRank()
    local t = tex.rank
    if t == nil then return end
    local A = Animation.rank
    local r = math.max(0, phaseEnd - phase)
    if r > A.appear then return end
    local cw = t.Width / 7
    local k = clamp((A.appear - r) / A.fade, 0, 1)
    local x, y = Layout.rank[1], Layout.rank[2]
    local cell, scale = 0, 1
    if info.DanResult > 0 then
        cell = 2 * info.DanCombo + info.DanResult
        if k < 1 then scale = 1 + math.sin(math.rad(60 + 120 * k)) * A.grow_by end
    else
        y = y - r * A.drop_px_per_ms
    end
    t:SetScale(scale, scale)
    t:SetOpacity(k)
    t:DrawRectAtAnchor(x, y, cell * cw, 0, cw, t.Height, "center")
    t:SetOpacity(1)
    t:SetScale(1, 1)
end

local function drawPlayer()
    local plate = ROACTIVITY:GetROActivity("nameplate")
    if plate ~= nil then plate:Draw(0, Layout.nameplate[1], Layout.nameplate[2], 255, 0, info.PlayerSide) end
    local mods = ROACTIVITY:GetROActivity("modicons")
    if mods ~= nil then
        if not mods.IsActive then mods:Activate() end
        mods:Draw(Layout.modicons[1], Layout.modicons[2], 0, "menu")
    end
end

-- ── lifecycle ────────────────────────────────────────────────────────────────────────────────────────

function onStart()
    W = THEME:GetResolution().X
    fonts.title = TEXT:CreateGlyphCached(Layout.music_name.font_size)
    fonts.song = TEXT:CreateGlyphCached(Layout.songs.title.font_size)
    fonts.icon = TEXT:CreateGlyphCached(Layout.dan_icon.title_size)
    fonts.exam = TEXT:CreateGlyphCached(E.font_size)
    col.title = Config.color(Layout.music_name.color)
    col.titleOutline = Config.color(Layout.music_name.outline)
    col.song = Config.color(Layout.songs.title.color)
    col.songOutline = Config.color(Layout.songs.title.outline)
    col.examText = Config.color(E.text_color)
    col.examOutline = Config.color(E.text_outline)
    col.white = Config.color({ 255, 255, 255, 255 })
    col.black = Config.color({ 0, 0, 0, 255 })
end

local function loadTextures()
    tex.songBase = load("Textures/SongPanel_Base.png")
    tex.songPanel = load("Textures/SongPanel_Main.png")
    tex.stateBase = load("Textures/StatePanel_Base.png")
    tex.stateMain = load("Textures/StatePanel_Main.png")
    tex.rank = load("Textures/Rank.png")
    tex.number = load("Textures/Number.png")
    tex.scoreNumber = load("Textures/Score_Number.png")
    tex.difficulty = load("Textures/Dan/Difficulty_Cymbol.png")
    tex.level = load("Textures/Dan/Level_Number.png")
    tex.danIcon = load("Textures/Dan/DanIcon.png")
    tex.danIconFade = load("Textures/Dan/DanIcon_Fade.png")
    tex.examBase = load("Textures/Exam/Base.png")
    tex.examBaseSmall = load("Textures/Exam/Base_Small.png")
    tex.examSmallBase = load("Textures/Exam/SmallBase.png")
    tex.examCymbol = load("Textures/Exam/ExamCymbol.png")
    tex.examSmallCymbol = load("Textures/Exam/Small_ExamCymbol.png")
    tex.examRange = load("Textures/Exam/ExamRange.png")
    tex.examType = load("Textures/Exam/ExamType.png")
    tex.examFailed = load("Textures/Exam/Failed.png")
    tex.examNumber = load("Textures/Exam/Number.png")
    tex.examSmallNumber = load("Textures/Exam/Small_Number.png")
    tex.examMiniNumber = load("Textures/Exam/MiniNumber.png")
    tex.examGauge = { normal = load("Textures/Exam/Gauge_Normal.png"), clear = load("Textures/Exam/Gauge_Clear.png") }
    tex.rainbow = {}
    local i = 0
    while STORAGE:FileExists("Textures/Exam/Rainbow/" .. i .. ".png") do
        tex.rainbow[#tex.rainbow + 1] = TEXTURE:CreateTexture("Textures/Exam/Rainbow/" .. i .. ".png")
        i = i + 1
    end
end

local function disposeTextures(t)
    for _, v in pairs(t) do
        if type(v) == "table" then disposeTextures(v)
        elseif v ~= nil then pcall(function() v:Dispose() end) end
    end
end

function activate(result)
    deactivate()
    info = result
    songCount = info.DanSongCount
    clock, phase = 0, 0
    phaseEnd = Animation.rank.at + Animation.rank.step * songCount
    swapShift, swapAt = 0, nil
    loadTextures()
    -- the glyphs shown later are made now, not on their first draw
    fonts.title:Draw(info.Title, -10000, -10000, col.title, col.titleOutline)
    for i = 0, songCount - 1 do fonts.song:Draw(info:GetDanSong(i).Title, -10000, -10000, col.song, col.songOutline) end
    active = true
end

function deactivate()
    active = false
    disposeTextures(tex)
    tex = {}
    info = nil
end

function update()
    if not active then return end
    local dt = fps.deltaTime * 1000
    clock = clock + dt
    phase = math.min(phaseEnd, phase + dt)
end

-- true while still animating: everything jumps to its place
function skip()
    if not active or phase >= phaseEnd then return false end
    phase = phaseEnd
    return true
end

-- once everything is shown, the exams slide away to show the songs, or back
function swap()
    if not active or phase < phaseEnd then return false end
    if swapAt ~= nil and clock - swapAt < Animation.swap then return false end
    swapShift = (swapShift == 0) and 1 or -swapShift
    swapAt = clock
    return true
end

function draw()
    if not active then return end
    if tex.songBase ~= nil then tex.songBase:Draw(0, 0) end
    local plate = ROACTIVITY:GetROActivity("danplate")
    if plate ~= nil then
        local A = Animation.plate
        local y = Layout.dan_plate[2] - math.max(0, A.at - phase) * A.px_per_ms
        plate:Draw(Layout.dan_plate[1], y, 255, info.DanTick, info.DanTickRed, info.DanTickGreen, info.DanTickBlue, info.Title)
    end
    local page, fade = songPage()
    local S = Animation.songs
    for i = 0, songCount - 1 do
        if math.floor(i / Layout.songs.per_page) == page then
            drawSong(i, math.max(0, S.at + S.step * i - phase) * S.px_per_ms, fade)
        end
    end
    drawPanel(examOffset())
    drawRank()
    local M = Layout.music_name
    fonts.title:Draw(info.Title, M.x, M.y, col.title, col.titleOutline, 1, 1, M.max_width, "top")
    drawPlayer()
end

function afterSongEnum() end

function onDestroy()
    deactivate()
    for _, f in pairs(fonts) do pcall(function() f:Dispose() end) end
    fonts = {}
end
