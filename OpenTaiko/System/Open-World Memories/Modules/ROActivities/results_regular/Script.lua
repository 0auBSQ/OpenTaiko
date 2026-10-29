---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- results_regular ROActivity: the result screen of a play, an AI battle's included.
-- The result stage calls activate(info) with a LuaResultInfo, update() and draw() every frame, skip() on
-- decide (true while still animating) and deactivate().

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local GAUGE, NUMBERS, SCORE, BADGES, CLEAR = Animation.gauge, Animation.numbers, Animation.score, Animation.badges, Animation.clear

local RESULT = "Textures/"
local W = 1920

local tex = {}
local sounds = {}
local fonts = {}
local col = {}
local played = {}
local charas = {}
local info = nil
local active = false
local clock = 0                 -- milliseconds since activate

local players, drawn, layoutN, kind, is1P, is2PSide = 1, 1, 1, "2p", true, false
local endSeg = {}               -- each player's gauge, in segments of 2 %
local times = {}                -- the timeline, from the fullest gauge
local randomText = 0

-- ── helpers ──────────────────────────────────────────────────────────────────────────────────────────

local function load(path)
    if not STORAGE:FileExists(path) then return nil end
    return TEXTURE:CreateTexture(path)
end

local function rad(deg) return deg * math.pi / 180 end
local function clamp(x, a, b) return math.max(a, math.min(b, x)) end

local function cue(name, key)
    if played[key] then return end
    played[key] = true
    local s = sounds[name]
    if s ~= nil then s:Play() end
end

-- the side or slot a player's elements use
local function posOf(i)
    if kind == "2p" and is2PSide then return 1 end
    return i
end

-- an element's place for the player at pos; ui: it moves with a lone player's panel
local function place(el, pos, ui)
    if kind == "2p" then
        return el.x[pos + 1] + (ui or 0), el.y[pos + 1]
    end
    local base, move = el[kind], Layout.ui_move[kind]
    return base[1] + move.x[pos + 1], base[2] + move.y[pos + 1]
end

-- 1.3 down to 1 over pop ms from t0, as the old screen popped its counts and pictures in
local function popAt(t0)
    local t = clock - t0
    if t < 0 or t > NUMBERS.pop then return 1 end
    return NUMBERS.pop_from - math.sin(rad(t * 90 / NUMBERS.pop)) * (NUMBERS.pop_from - 1)
end

-- how far a lone player's panel has slid aside since the clear, in pixels
local function uiOffset()
    if not (is1P and Layout.use_1p_ui) or clock < times.clear then return 0 end
    local k = clamp((clock - times.clear) / CLEAR.ui_move, 0, 1)
    local off = math.sin(k * math.pi / 2) * W / 2
    return is2PSide and -off or off
end

-- the lit row of a badge sheet (0 unlit), from the flash loop
local function flashRow(starts)
    local t = clock % Animation.flash.loop
    local step = Animation.flash.step
    for _, s in ipairs(starts) do
        if t >= s and t <= s + 3 * step then
            return math.min(3, math.floor((t - s) / step) + 1)
        end
    end
    return 0
end

local function sheetCell(t, x, y, colN, rowN, c, r, scale, opacity)
    if t == nil then return end
    local cw, ch = t.Width / colN, t.Height / rowN
    t:SetScale(scale, scale)
    t:SetOpacity(opacity or 1)
    t:DrawRectAtAnchor(x, y, c * cw, r * ch, cw, ch, "center")
    t:SetOpacity(1)
    t:SetScale(1, 1)
end

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
local function drawNumber(x, y, value, scale, pop)
    local t = tex.number
    if t == nil then return end
    local w, h = t.Width / 11, t.Height / 2
    local iv = Layout.numbers.interval
    t:SetScale(pop * scale, pop * scale)
    for j, d in ipairs(digits(value)) do
        local dx = x - iv[1] * scale * (j - 1) + w * 2
        local dy = y - iv[2] * scale * (j - 1)
        t:DrawRectAtAnchor(dx + w * scale / 2, dy + h * scale / 2, d * w, 0, w, h, "center")
    end
    t:SetScale(1, 1)
end

-- the score in Score_Number.png's digits, its last digit centred on x
local function drawScore(x, y, value, scale, pop)
    local t = tex.scoreNumber
    if t == nil then return end
    local w, h = t.Width / 10, t.Height
    local iv = Layout.score.interval
    t:SetScale(pop * scale, pop * scale)
    for j, d in ipairs(digits(value)) do
        local dx = x - iv[1] * scale * (j - 1)
        local dy = y - iv[2] * scale * (j - 1)
        t:DrawRectAtAnchor(dx, dy + h * scale / 2, d * w, 0, w, h, "center")
    end
    t:SetScale(1, 1)
end

-- a texture part from its top left corner, scaled
local function part(t, x, y, rx, ry, rw, rh, sx, sy)
    if t == nil or rw <= 0 or rh <= 0 then return end
    t:SetScale(sx or 1, sy or 1)
    t:DrawRectAtAnchor(x, y, rx, ry, rw, rh, "topleft")
    t:SetScale(1, 1)
end

-- ── the timeline ─────────────────────────────────────────────────────────────────────────────────────

local function buildTimes()
    local factor = 0
    for p = 0, players - 1 do
        endSeg[p] = math.floor(math.floor(info:GetGauge(p)) / 2)
        factor = math.max(factor, endSeg[p])
    end
    times.numbers = NUMBERS.start + factor * NUMBERS.gauge_ms
    times.score = times.numbers + NUMBERS.row * SCORE.after_rows + SCORE.delay
    times.rank = times.score + BADGES.rank_delay
    times.crown = times.score + BADGES.crown_delay
    times.clear = CLEAR.start + factor * CLEAR.gauge_ms
    times.rainbow = GAUGE.start + 50 * GAUGE.segment
end

local function segments(p)
    if clock < GAUGE.start then return 0 end
    return math.min(endSeg[p], math.floor((clock - GAUGE.start) / GAUGE.segment))
end

local function gaugeEnded(p) return clock >= GAUGE.start + endSeg[p] * GAUGE.segment end

-- ── the gauge, as the play's result gauge drew it ────────────────────────────────────────────────────

local function drawGauge(p, pos, shift, ui)
    local G = Layout.gauge
    local box = G.rect
    local boxW = box[3]
    local sx = (players >= 3) and G.scale_x_3p or 1
    local x, y = place(G, pos, ui)
    local diff = info:GetDifficulty(p)
    if diff > 4 then return end
    local gtype = info:GetGaugeType(p)
    local norma = info:GetNorma(p)
    local kz = info:GetKillZone(p)
    local percent = segments(p) * 2
    local normal = gtype == 0
    local n = shift + 1

    part(tex.gaugeBase[n], x, y, box[1], box[2], boxW, box[4], sx, 1)

    local norma2 = norma - 2
    local texStart = math.floor(boxW * norma2 / 100)
    if normal then
        part(tex.gaugeClearBase, x + math.floor(sx * texStart), y, box[1] + texStart, box[2], math.floor(boxW * (100 - norma2) / 100), box[4], sx, 1)
    end

    local closestTwo = math.floor(math.floor(percent / 2) * (boxW / 50))
    local closestNorma = normal and math.floor(boxW * norma2 / 100) or boxW
    local rainbowFrames = tex.rainbow
    local isRainbow = #rainbowFrames > 0 and percent >= 100 and normal
    if not isRainbow then
        part(tex.gauge[n], x, y, box[1], box[2], math.min(closestTwo, closestNorma), box[4], sx, 1)
    end
    if normal and percent >= norma then
        if not isRainbow then
            part(tex.gaugeClear, x + math.floor(sx * texStart), y, box[1] + texStart, box[2], closestTwo - closestNorma, box[4], sx, 1)
        end
        if #rainbowFrames > 0 and percent >= 100 then
            local f = rainbowFrames[(math.floor(math.max(0, clock - times.rainbow) / GAUGE.rainbow_frame) % #rainbowFrames) + 1]
            local rx, ry = place(Layout.rainbow, pos, ui)
            f:SetScale(sx, 1)
            f:DrawAtAnchor(rx, ry, "topleft")
            f:SetScale(1, 1)
        end
    end

    if gtype == 2 then
        part(tex.killzone, x, y, box[1], box[2], math.floor(boxW * kz / 100), box[4], sx, 1)
    end

    -- the clear mark: lit once the gauge clears (normal), or while it is in danger (hard, extreme)
    local icon = normal and tex.gauge[1] or tex.killzone
    local lit
    if normal then lit = percent >= norma else lit = math.min(100, percent) < 100 - (100 - kz) * 0.7 end
    local C = Layout.clear_text
    local r = lit and C.clear_rect or C.rect
    local cx, cy = place(C, pos, ui)
    part(icon, cx, cy, r[1], r[2], r[3], r[4], sx, 1)

    if normal then
        local fire, letter = tex.soulFire, tex.soulText
        if fire ~= nil and percent >= 100 then
            local fw = fire.Width / GAUGE.soul_frames
            local frame = math.floor(math.max(0, clock - times.rainbow) / GAUGE.soul_frame) % GAUGE.soul_frames
            local fx, fy = place(Layout.soul_fire, pos, ui)
            part(fire, fx - math.floor(fire.Width / 16), fy - math.floor(fire.Height / 2), frame * fw, 0, fw, fire.Height)
        end
        if letter ~= nil then
            local lh = math.floor(letter.Height / 2)
            local lx, ly = place(Layout.soul_text, pos, ui)
            part(letter, lx - math.floor(letter.Width / 2), ly - math.floor(letter.Height / 4), 0, (percent >= norma) and 0 or lh, letter.Width, lh)
        end
    end
end

-- ── the characters ───────────────────────────────────────────────────────────────────────────────────

local function resultAnims()
    return { CHARACTER.ANIM_RESULT_NORMAL, CHARACTER.ANIM_RESULT_FAILED_IN, CHARACTER.ANIM_RESULT_FAILED, CHARACTER.ANIM_RESULT_CLEAR }
end

local function resultVoices()
    return { CHARACTER.VOICE_RESULT_CLEARSUCCESS, CHARACTER.VOICE_RESULT_CLEARFAILED }
end

local function loadCharas()
    for p = 0, drawn - 1 do
        local c = CHARACTER:GetPlayerCharacter(p)
        for _, a in ipairs(resultAnims()) do c:LoadAnimation(a) end
        for _, v in ipairs(resultVoices()) do c:LoadVoice(v) end
        charas[p] = { c = c, loop = CHARACTER.ANIM_RESULT_NORMAL, action = nil }
        c:ResetAnimationCounter(CHARACTER.ANIM_RESULT_NORMAL)
    end
end

local function disposeCharas()
    for _, s in pairs(charas) do
        pcall(function()
            for _, a in ipairs(resultAnims()) do s.c:DisposeAnimation(a) end
            for _, v in ipairs(resultVoices()) do s.c:DisposeVoice(v) end
        end)
    end
    charas = {}
end

-- the clear: a cleared player's character cheers, a failed one's falls and stays down
local function react(p)
    local s = charas[p]
    if s == nil then return end
    if info:GetClearType(p) >= 1 then
        s.c:PlayVoice(CHARACTER.VOICE_RESULT_CLEARSUCCESS)
        s.loop, s.action = CHARACTER.ANIM_RESULT_CLEAR, nil
        s.c:ResetAnimationCounter(s.loop)
    else
        s.c:PlayVoice(CHARACTER.VOICE_RESULT_CLEARFAILED)
        s.loop = CHARACTER.ANIM_RESULT_FAILED
        s.c:ResetAnimationCounter(s.loop)
        if s.c:AvailableAnimation(CHARACTER.ANIM_RESULT_FAILED_IN) then
            s.action = CHARACTER.ANIM_RESULT_FAILED_IN
            s.c:ResetAnimationCounter(s.action)
        end
    end
end

local function updateChara(s)
    local cur = s.action or s.loop
    s.c:SetAnimationDuration(cur, Animation.character_ms)
    local ended = s.c:Update(cur, s.action == nil)
    if s.action ~= nil and ended then
        s.action = nil
        s.c:ResetAnimationCounter(s.loop)
        s.c:SetAnimationDuration(s.loop, Animation.character_ms)
        s.c:Update(s.loop, true)
    end
end

-- the nameplate's place, which the character and its puchichara stand at
local function plateAt(pos) return place(Layout.nameplate, pos) end

local function drawPuchichara(p, pos, x, y)
    local ok, show = pcall(function() return CONFIG.ShowPuchiChara end)
    if ok and show == false then return end
    local puchi = PUCHICHARALIST ~= nil and PUCHICHARALIST:GetPlayerPuchichara(p) or nil
    if puchi == nil or puchi.tx == nil or not puchi.tx.Loaded then return end
    local P = Layout.puchichara
    local side = (layoutN <= 2) and pos + 1 or 1
    local t = puchi.tx
    local fw = t.Width / P.frames
    local frame = math.floor(clock / Animation.puchichara.frame) % P.frames
    local bob = math.sin(rad(1 + (clock % Animation.puchichara.bob) / Animation.puchichara.bob * 359)) * P.bob_px
    t:SetScale(P.scale, P.scale)
    t:DrawRectAtAnchor(x + P.dx[side] + P.offset[1], y + P.dy[side] + P.offset[2] + bob, frame * fw, 0, fw, t.Height, "center")
    t:SetScale(1, 1)
end

-- ── the clear's flowers, shines and speech bubbles ───────────────────────────────────────────────────

local function moods(p)
    local c = info:GetClearType(p)
    if c == 4 then return 3, 5 end
    if c == 3 then return 3, 4 end
    if c >= 1 then
        if info:GetGauge(p) >= 100 then return 3, 3 end
        return 2, 2
    end
    if info:GetGauge(p) >= 40 then return 1, 1 end
    return 0, 0
end

local function drawFlowers(pos)
    local pop = popAt(times.clear)
    local F = Layout.flower
    if tex.flower ~= nil then
        local t = tex.flower
        t:SetScale(F.scale * pop, F.scale * pop)
        t:DrawRectAtAnchor(F.x[pos + 1], F.y[pos + 1], 0, 0, t.Width, t.Height / 2, "center")
        t:SetScale(1, 1)
    end
    local R, A = Layout.flower_rotate, Animation.flowers
    local t = clock % A.loop
    for i = 1, 5 do
        local f = tex.flowerRotate[i]
        if f ~= nil then
            local s = A.at[i]
            local o
            if t < s or t > s + 2 * A.fade + A.hold then o = 0
            elseif t >= s + A.fade and t <= s + A.hold + A.fade then o = 1
            elseif t >= s + A.fade + A.hold then o = (A.fade - (t - s - A.hold - A.fade)) / A.fade
            else o = (t - s) / A.fade end
            f:SetOpacity(o)
            f:SetScale(R.scale, R.scale)
            f:SetRotation(math.deg((t - s) / (A.turns[i] * 360)))
            f:DrawAtAnchor(R.x[pos + 1][i], R.y[pos + 1][i], "center")
            f:SetRotation(0)
            f:SetScale(1, 1)
            f:SetOpacity(1)
        end
    end
    local S, AS = Layout.plate_shine, Animation.plate_shine
    local sh = tex.shine
    if sh ~= nil then
        local st = clock % AS.loop
        local half = AS.loop / 2
        local q = st % half
        for i = 1, #S.x[pos + 1] do
            local o
            if (i <= 3 and st >= half) or (i > 3 and st < half) then o = 0
            elseif q >= AS.fade and q <= half - AS.fade then o = 1
            else o = math.min(q, half - q) / AS.fade end
            sh:SetOpacity(o)
            sh:SetScale(S.scale, S.scale)
            sh:DrawAtAnchor(S.x[pos + 1][i], S.y[pos + 1][i], "center")
        end
        sh:SetScale(1, 1)
        sh:SetOpacity(1)
    end
end

local function drawBubbles(p, pos)
    local pop = popAt(times.clear)
    local mood, moodV2 = moods(p)
    if layoutN <= 2 then
        local B = Layout.speech_bubble
        sheetCell(tex.bubble[pos + 1], B.x[pos + 1], B.y[pos + 1], 4, 3, mood, randomText, B.scale * pop)
    end
    local V = Layout.speech_bubble_v2
    local b = tex.bubbleV2[(layoutN <= 2) and pos + 1 or 3]
    if b == nil then return end
    local bx, by, scale
    if kind ~= "2p" then
        bx, by = place(V, pos)
        scale = V.scale_more
    elseif layoutN == 2 then
        bx, by, scale = V.x_2p[pos + 1], V.y_2p[pos + 1], V.scale_more
    else
        bx, by, scale = V.x[pos + 1], V.y[pos + 1], V.scale_1p
    end
    sheetCell(b, bx, by, 1, 6, 0, moodV2, V.scale * scale * pop)
    local line = info:GetSpeechText(p, moodV2)
    if line ~= nil and line ~= "" then
        fonts.speech:Draw(line, bx + V.text_offset[1] * scale, by + V.text_offset[2] * scale, col.speech, col.speechOutline,
            1, scale, V.text_max_width * scale, "center")
    end
end

-- ── the AI battle ────────────────────────────────────────────────────────────────────────────────────

local function drawAI()
    local A = Layout.ai
    if tex.aiPanel ~= nil then tex.aiPanel:Draw(0, 0) end
    local count = info.AISectionCount
    local batch = tex.batch
    local pages = math.max(1, math.ceil(count / A.per_page))
    -- the pages take turns, each fading in over its first 255 ms (the first one is shown from the start)
    local page, fade = 0, 1
    if count > A.per_page then
        local t = clock + 255
        page = math.floor(t / Animation.ai_page) % pages
        fade = math.min(t % Animation.ai_page, 255) / 255
    end
    for i = 0, count - 1 do
        local nowIndex = math.floor(i / A.per_page)
        local drawCount = math.min(count - nowIndex * A.per_page, A.per_page)
        local drawPos = i % A.per_page
        local total = A.batch_move[1] * drawCount
        local x = A.batch[1] + A.batch_move[1] * drawPos - math.floor(total / 2)
        local y = A.batch[2] + A.batch_move[2] * (drawPos % 2)
        local o = (nowIndex == page) and fade or (1 - fade)
        if batch ~= nil then
            local bw, bh = batch.Width / 3, batch.Height
            batch:SetOpacity(o)
            batch:DrawRectAtAnchor(x, y, 0, 0, bw, bh, "topleft")
            local ending = info:GetAISectionEnd(i)
            if ending == 1 or ending == 2 then batch:DrawRectAtAnchor(x, y, bw * ending, 0, bw, bh, "topleft") end
            batch:SetOpacity(1)
        end
        if tex.sectionPlate ~= nil then
            tex.sectionPlate:SetOpacity(o)
            tex.sectionPlate:Draw(x + A.plate_offset[1], y + A.plate_offset[2])
            tex.sectionPlate:SetOpacity(1)
        end
        fonts.ai:Draw(LANG:GetString("AI_SECTION", i + 1), x + A.text_offset[1], y + A.text_offset[2], col.ai, col.aiOutline, o, 1, 0, "center")
    end
    if clock >= times.clear then
        local flag = info:IsCleared(0) and tex.winFlag or tex.loseFlag
        if flag ~= nil then
            local s = 2 - clamp(clock - times.clear, 0, CLEAR.win_flag) / CLEAR.win_flag
            flag:SetScale(s, s)
            flag:DrawAtAnchor(A.win_flag[1], A.win_flag[2], "center")
            flag:SetScale(1, 1)
        end
    end
end

-- ── a player's panel ─────────────────────────────────────────────────────────────────────────────────

local ROW_VALUE = {
    good = function(p) return info:GetGoodCount(p) end, ok = function(p) return info:GetOkCount(p) end,
    bad = function(p) return info:GetBadCount(p) end, roll = function(p) return info:GetRollCount(p) end,
    combo = function(p) return info:GetMaxCombo(p) end, adlib = function(p) return info:GetADLibCount(p) end,
    boom = function(p) return info:GetBoomCount(p) end,
}

local function drawPanel(i, ui)
    local pos = posOf(i)
    local shift = (i == 1 or is2PSide) and 1 or i

    if kind == "2p" then
        local t = tex.panel[shift + 1]
        if t ~= nil then t:Draw(ui, 0) end
    else
        local t = tex.panelN[i + 1]
        local move = Layout.ui_move[kind]
        if t ~= nil then t:Draw(move.x[i + 1], move.y[i + 1]) end
    end

    if tex.gaugeFrame ~= nil then
        local D = Layout.difficulty_bar
        local bx, by = place(D, pos, ui)
        part(tex.diffBar, bx, by, 0, info:GetDifficulty(i) * D.size[2], D.size[1], D.size[2])
        local fx, fy = place(Layout.gauge_frame, pos, ui)
        tex.gaugeFrame:SetScale((kind == "2p") and 1 or 0.5, 1)
        tex.gaugeFrame:Draw(fx, fy)
        tex.gaugeFrame:SetScale(1, 1)
    end

    if clock < GAUGE.start then return end
    cue("gauge", "gauge")
    drawGauge(i, pos, shift, ui)

    local N = Layout.numbers
    local numScale = (kind == "5p" and N.scale_5p) or (kind == "4p" and N.scale_4p) or 1
    for k, row in ipairs(N.rows) do
        local t0 = times.numbers + NUMBERS.row * (k - 1)
        if clock < t0 then break end
        local shown = row.show ~= false
        if shown then
            local x, y = place(row, pos, ui)
            drawNumber(x, y, ROW_VALUE[row.value](i), numScale, popAt(t0))
            cue("pon", "pon" .. k)
        end
    end

    if clock >= times.score then
        local S = Layout.score
        local scale = (kind == "5p" and S.scale_5p) or (kind == "4p" and S.scale_4p) or 1
        local t = clock - times.score
        local pop = 1
        if t <= SCORE.grow then pop = 1 + math.sin(rad(t / 1.5)) * SCORE.grow_by
        elseif t <= SCORE.grow + SCORE.settle then pop = 1 - math.sin(rad(t - SCORE.grow)) * SCORE.settle_by end
        local x, y = place(S, pos, ui)
        drawScore(x, y, info:GetScore(i), scale, pop)
        cue("score_don", "score")
    end
end

-- the score rank or the crown: fades in shrinking from big, dips and settles
local function badge(t0)
    local t = clock - t0
    if t < 0 then return nil end
    local B = BADGES
    if t <= B.fade then
        return 1 + math.sin(rad((t + B.dip) / 1.5)) * B.grow_by, t / B.fade
    elseif t <= B.fade + B.dip then
        return 0.5 + math.sin(rad(t - B.fade)) * 0.5, 1
    end
    return 1, 1
end

local function drawBadges(p, pos, ui)
    local rank = info:GetScoreRank(p)
    if rank > 0 then
        local s, o = badge(times.rank)
        if s ~= nil then
            local x, y = place(Layout.score_rank, pos, ui)
            sheetCell(tex.rank, x, y, 7, 4, rank - 1, flashRow(Animation.flash.rank_at), s, o)
            if clock >= times.rank + BADGES.sound_after then cue("rank_in", "rank") end
        end
    end
    local clearType = info:GetClearType(p)
    if clearType >= 1 then
        local s, o = badge(times.crown)
        if s ~= nil then
            local x, y = place(Layout.crown, pos, ui)
            sheetCell(tex.crown, x, y, 4, 4, clearType - 1, flashRow(Animation.flash.crown_at), s, o)
            if clock >= times.crown + BADGES.sound_after then cue("crown_in", "crown") end
        end
    end
end

local function drawPlayers()
    local plate = ROACTIVITY:GetROActivity("nameplate")
    local mods = ROACTIVITY:GetROActivity("modicons")
    if mods ~= nil and not mods.IsActive then mods:Activate() end
    for i = 0, drawn - 1 do
        local pos = posOf(i)
        if plate ~= nil then
            local x, y = place(Layout.nameplate, pos)
            plate:Draw(0, x, y, 255, i, info.PlayerSide)
        end
        if mods ~= nil then
            local x, y = place(Layout.modicons, pos)
            mods:Draw(x, y, i, "menu")
        end
    end
end

-- ── the music ────────────────────────────────────────────────────────────────────────────────────────

local music = {}

-- an optional intro, then the loop; a missing file stays silent
local function startMusic(intro, loop)
    music = {}
    if loop ~= nil and STORAGE:FileExists(loop) then
        music.loop = SOUND:CreateBGM(loop)
        music.loop:SetLoop(true)
    end
    if intro ~= nil and STORAGE:FileExists(intro) then
        music.intro = SOUND:CreateBGM(intro)
        music.intro:Play()
    elseif music.loop ~= nil then
        music.loop:Play()
    end
end

-- the loop takes over once the intro has played
local function updateMusic()
    local intro = music.intro
    if intro ~= nil and intro.Loaded and not intro.IsPlaying then
        music.intro = nil
        pcall(function() intro:Dispose() end)
        if music.loop ~= nil then music.loop:Play() end
    end
end

local function stopMusic()
    for _, s in pairs(music) do pcall(function() s:Stop(); s:Dispose() end) end
    music = {}
end

-- the result stage dims the music while a reward pop-up shows, percent from 0 to 100
function setMusicLevel(percent)
    for _, s in pairs(music) do s:SetVolumePercent(percent) end
end

-- ── lifecycle ────────────────────────────────────────────────────────────────────────────────────────

function onStart()
    W = THEME:GetResolution().X
    fonts.title = TEXT:CreateGlyphCached(Layout.music_name.font_size)
    fonts.speech = TEXT:CreateGlyphCached(Layout.speech_bubble_v2.text_size)
    fonts.ai = TEXT:CreateGlyphCached(Layout.ai.text_size)
    col.title = Config.color(Layout.music_name.color)
    col.titleOutline = Config.color(Layout.music_name.outline)
    col.speech = Config.color(Layout.speech_bubble_v2.text_color)
    col.speechOutline = Config.color(Layout.speech_bubble_v2.text_outline)
    col.ai = Config.color(Layout.ai.text_color)
    col.aiOutline = Config.color(Layout.ai.text_outline)
end

local function loadTextures()
    tex.header = load(RESULT .. "Header.png")
    tex.panel = { load(RESULT .. "Panel.png"), load(RESULT .. "Panel_2.png") }
    tex.panelN = {}
    if kind ~= "2p" then
        local prefix = (kind == "5p") and "Panel_5P_" or "Panel_4P_"
        for i = 1, players do tex.panelN[i] = load(RESULT .. prefix .. i .. ".png") end
    end
    tex.gauge, tex.gaugeBase = {}, {}
    for i = 1, 5 do
        local suffix = (i == 1) and "" or ("_" .. i)
        tex.gauge[i] = load(RESULT .. "Gauge" .. suffix .. ".png")
        tex.gaugeBase[i] = load(RESULT .. "Gauge_Base" .. suffix .. ".png")
    end
    tex.gaugeFrame = load(RESULT .. "Gauge_Frame.png")
    tex.gaugeClear = load(RESULT .. "Gauge_Clear.png")
    tex.gaugeClearBase = load(RESULT .. "Gauge_Clear_Base.png")
    tex.killzone = load(RESULT .. "Gauge_Killzone.png")
    tex.rainbow = {}
    local i = 0
    while STORAGE:FileExists(RESULT .. "Rainbow/" .. i .. ".png") do
        tex.rainbow[#tex.rainbow + 1] = TEXTURE:CreateTexture(RESULT .. "Rainbow/" .. i .. ".png")
        i = i + 1
    end
    tex.soulText = load(RESULT .. "Soul_Text.png")
    tex.soulFire = load(RESULT .. "Result_Soul_Fire.png")
    tex.diffBar = load(RESULT .. "DifficultyBar.png")
    tex.number = load(RESULT .. "Number.png")
    tex.scoreNumber = load(RESULT .. "Score_Number.png")
    tex.rank = load(RESULT .. "ScoreRankEffect.png")
    tex.crown = load(RESULT .. "CrownEffect.png")
    tex.shine = load(RESULT .. "Shine.png")
    tex.bubble = { load(RESULT .. "Speech_Bubble.png"), load(RESULT .. "Speech_Bubble_2.png") }
    tex.bubbleV2 = { load(RESULT .. "Speech_Bubble_V2_Left.png"), load(RESULT .. "Speech_Bubble_V2_Right.png"), load(RESULT .. "Speech_Bubble_V2_4P_5P.png") }
    tex.flower = load(RESULT .. "Flower/Flower.png")
    tex.flowerRotate = {}
    for f = 1, 5 do tex.flowerRotate[f] = load(RESULT .. "Flower/Rotate_" .. f .. ".png") end
    if info.IsAIBattle then
        tex.aiPanel = load(RESULT .. "AIBattle/Panel_AI.png")
        tex.batch = load(RESULT .. "AIBattle/Batch.png")
        tex.sectionPlate = load(RESULT .. "AIBattle/SectionPlate.png")
        tex.winFlag = load(RESULT .. "AIBattle/WinFlag_Win.png")
        tex.loseFlag = load(RESULT .. "AIBattle/WinFlag_Lose.png")
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
    players = math.max(1, info.PlayerCount)
    drawn = info.IsAIBattle and 1 or players
    layoutN = info.IsAIBattle and 2 or players
    kind = (layoutN == 5) and "5p" or ((layoutN >= 3) and "4p" or "2p")
    is1P = layoutN == 1
    is2PSide = info.PlayerSide == 1
    randomText = math.random(0, 2)
    clock, played = 0, {}
    buildTimes()
    loadTextures()
    for name, path in pairs(Animation.sounds) do
        if name ~= "_note" and STORAGE:FileExists(path) then sounds[name] = SOUND:CreateSFX(path) end
    end
    loadCharas()
    local M = Animation.music
    local ai = info.IsAIBattle and "ai_" or ""
    startMusic(Config.optional(M, ai .. "intro"), Config.optional(M, ai .. "loop"))
    -- the glyphs shown later are made now, not on their first draw
    fonts.title:Draw(info.Title, -10000, -10000, col.title, col.titleOutline)
    for p = 0, drawn - 1 do
        local _, moodV2 = moods(p)
        fonts.speech:Draw(info:GetSpeechText(p, moodV2), -10000, -10000, col.speech, col.speechOutline)
    end
    active = true
end

function deactivate()
    active = false
    stopMusic()
    for _, s in pairs(sounds) do pcall(function() s:Stop(); s:Dispose() end) end
    sounds = {}
    disposeTextures(tex)
    tex = {}
    disposeCharas()
    info = nil
end

function update()
    if not active then return end
    clock = clock + fps.deltaTime * 1000
    updateMusic()
    -- the gauge's sound stops once every gauge has filled, unless they all went rainbow (it plays out then)
    if played.gauge and not played.gaugeStop then
        local allEnded, allRainbow = true, true
        for p = 0, drawn - 1 do
            if not gaugeEnded(p) then allEnded = false end
            if endSeg[p] < 50 then allRainbow = false end
        end
        if allEnded then
            played.gaugeStop = true
            if not allRainbow and sounds.gauge ~= nil then sounds.gauge:Stop() end
        end
    end
    for _, s in pairs(charas) do updateChara(s) end
    if clock >= times.clear and not played.clear then
        played.clear = true
        for p = 0, drawn - 1 do react(p) end
    end
end

-- true while still animating: everything jumps to the clear
function skip()
    if not active or clock >= times.clear then return false end
    clock = times.clear
    for _, k in ipairs({ "gauge", "gaugeStop", "score", "rank", "crown" }) do played[k] = true end
    for k = 1, #Layout.numbers.rows do played["pon" .. k] = true end
    if sounds.gauge ~= nil then sounds.gauge:Stop() end
    return true
end

function draw()
    if not active then return end
    local ui = uiOffset()
    if not info.IsAIBattle and tex.header ~= nil then tex.header:Draw(0, 0) end
    for i = 0, drawn - 1 do drawPanel(i, ui) end
    if info.IsAIBattle then drawAI() end
    for p = 0, drawn - 1 do
        local pos = posOf(p)
        local x, y = plateAt(pos)
        local cx, cy = x + Layout.chara.dx, y
        local s = charas[p]
        if s ~= nil then s.c:Draw(cx, cy, s.action or s.loop, (pos % 2 == 1) and -1 or 1, 1) end
        drawPuchichara(p, pos, cx, cy)
        if clock >= times.clear then
            if info:GetClearType(p) >= 1 and layoutN <= 2 then drawFlowers(pos) end
            drawBubbles(p, pos)
        end
        drawBadges(p, pos, ui)
    end
    local M = Layout.music_name
    fonts.title:Draw(info.Title, M.x, M.y, col.title, col.titleOutline, 1, 1, M.max_width, "top")
    drawPlayers()
end

function afterSongEnum() end

function onDestroy()
    deactivate()
    for _, f in pairs(fonts) do pcall(function() f:Dispose() end) end
    fonts = {}
end
