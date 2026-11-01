---@diagnostic disable: undefined-global, undefined-field, need-check-nil, inject-field
-- results_dan ROActivity: the result screen of a dan play.
-- The result stage calls activate(info) with a LuaResultInfo, update() and draw() every frame, skip() on decide
-- (true while still animating), then advance() (true when a new or better dan grade gets its own screen before the
-- reward pop-ups), swap() on left or right (true when the verdict and the details trade places) and deactivate().

local PlayStrings = require("PlayStrings")

-- positions, sizes and colours: Config/layout.json; the timeline, the motion and the sounds: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local V, D, T, TAG, G = Layout.verdict, Layout.details, Layout.ticket, Layout.tag, Layout.grade
local A1, A3 = Animation.verdict, Animation.grade

local GRAPHICS_DIR = "../../../Graphics/"   -- the selected judges pack
-- a stage can give the dan it mounts its own background (the song_loading transition shows it too): this shared
-- string holds its path from the Modules folder, read only for the chart the stage stamped
local STAGE_BACKGROUND_KEY = "song_loading_dan_bg"
local RANK_CELLS = 7                        -- Rank.png: failed, then red and gold passes, full combos, perfects
local SCROLLS = { "Pass", "FullCombo", "Perfect" }
local BACKS = { "Silver", "Gold", "Rainbow" }   -- the grade screen's background by the clear: pass, full combo, perfect
local OUTER_RING, INNER_RING = 16, 12       -- the big kanji's outline: copies round it
local SEPARATOR = "·"                       -- between a per-song exam's values
local NO_OPTIONS = {}

local W, H = 1920, 1080
local tex = {}
local fonts = {}
local col = {}
local sounds = {}
local strings = {}
local info = nil
local active = false
local clock = 0             -- milliseconds since activate
local played = {}
local timeline = nil        -- the verdict's timeline
local exams, barItem, gridItems = {}, nil, {}
local newBest = false
local view, swapAt = 1, nil -- 1 the verdict, 2 the details; when the last swap started
local grade = nil           -- the grade screen's timeline, once advance() started it
local chara = nil
local judgeWords, judgeFrames, judgeCellH = nil, 5, 100
local stageTint = nil       -- the gradient map of a stage's own background
local duckMusic             -- with the music, below

-- ── helpers ──────────────────────────────────────────────────────────────────────────────────────────

local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

local function fill(s, value) return (s:gsub("{0}", function() return value end)) end

-- "812450", no separators
local function whole(n) return tostring(math.floor(n)) end

local function clamp01(x) return math.max(0, math.min(1, x)) end
local function lerp(a, b, k) return a + (b - a) * k end
local function easeOut(x) x = clamp01(x); return 1 - (1 - x) ^ 3 end
local function easeInOut(x) x = clamp01(x); return x < 0.5 and 4 * x * x * x or 1 - (-2 * x + 2) ^ 3 / 2 end
local function easeOutBack(x, s)
    x = clamp01(x) - 1
    return 1 + (s + 1) * x ^ 3 + s * x ^ 2
end

-- how far an event that starts at t0 and lasts d has gone, 0 to 1
local function progress(t0, d) return clamp01((clock - t0) / d) end

-- a pop: up to the overshoot then back to 1
local function popScale(k, over)
    if k <= 0 then return 0 end
    if k < 0.6 then return over * easeOut(k / 0.6) end
    return over + (1 - over) * easeOut((k - 0.6) / 0.4)
end

local function play(name)
    local s = sounds[name]
    if s ~= nil then s:Play() end
end

-- fn once, when the clock first reaches t
local function at(t, key, fn)
    if clock < t or played[key] then return end
    played[key] = true
    fn()
end

local function ordinal(n)
    local m = n % 100
    if m >= 11 and m <= 13 then return n .. "th" end
    return n .. (({ "st", "nd", "rd" })[n % 10] or "th")
end

local function statusIndex(s) return math.max(0, math.min(2, s)) + 1 end

local function stampColor(song) return col.stamps[math.min(song + 1, #col.stamps)] end

-- ── text ─────────────────────────────────────────────────────────────────────────────────────────────

-- the clear margin a glyph line keeps left, right and under its ink (the engine's text padding)
local function pad(font) return font.BoxHeight - math.ceil(font.LineHeight) end

-- a glyph line anchored at its middle sits its ink high by half the box's bottom padding
local function nudge(font) return math.floor(pad(font) / 2) + Layout.text_nudge end

-- a text with its ink's left edge, centre or right edge at x ("left", "center" or "right") and its line's middle
-- at y; no outline, a soft shadow under it. o: color, scale, opacity, maxW (the ink's), shadow (px, 0 for none),
-- shadowColor, shadowOpacity
local function text(font, s, x, y, anchor, o)
    if s == nil or s == "" then return end
    o = o or NO_OPTIONS
    local scale, opacity = o.scale or 1, o.opacity or 1
    if opacity <= 0 then return end
    local p = pad(font) * scale
    if anchor == "left" then x = x - p elseif anchor == "right" then x = x + p end
    y = y + nudge(font) * scale
    local maxW = o.maxW and (o.maxW + 2 * p) or 0
    local sh = o.shadow or Layout.shadow_px
    if sh > 0 then
        font:Draw(s, x, y + sh, o.shadowColor or col.shadow, col.clear, opacity * (o.shadowOpacity or Layout.shadow_opacity), scale, maxW, anchor)
    end
    font:Draw(s, x, y, o.color or col.text, col.clear, opacity, scale, maxW, anchor)
end

-- ── shapes ───────────────────────────────────────────────────────────────────────────────────────────

-- a rounded bar centred at (cx, cy), w by h, turned angle degrees (counter-clockwise, like SetRotation): a disc at
-- each end and the body stretched between their centres
local function pill(cx, cy, w, h, angle, color, opacity)
    local e, body = tex.fillEnd, tex.fillBody
    angle = angle or 0
    local rad = math.rad(angle)
    local half = math.max(0, (w - h) / 2)
    if half > 0 then
        body:SetColor(color)
        body:SetOpacity(opacity)
        body:SetRotation(angle)
        body:SetScale(2 * half / body.Width, h / (body.Height - 4))
        body:DrawAtAnchor(cx, cy, "center")
        body:SetColor(1.0, 1.0, 1.0)
        body:SetRotation(0)
    end
    local s = h / (e.Height - 4)
    e:SetColor(color)
    e:SetOpacity(opacity)
    e:SetRotation(angle)
    e:SetScale(s, s)
    e:DrawAtAnchor(cx - half * math.cos(rad), cy + half * math.sin(rad), "center")
    e:DrawAtAnchor(cx + half * math.cos(rad), cy - half * math.sin(rad), "center")
    e:SetColor(1.0, 1.0, 1.0)
    e:SetRotation(0)
end

-- a texture centred at (x, y): scale, opacity, rotation and an optional tint (r, g, b from 0 to 1)
local function put(t, x, y, scale, opacity, angle, r, g, b)
    if t == nil or opacity <= 0 or scale <= 0 then return end
    t:SetScale(scale, scale)
    t:SetOpacity(opacity)
    t:SetRotation(angle or 0)
    if r ~= nil then t:SetColor(r, g, b) end
    t:DrawAtAnchor(x, y, "center")
    if r ~= nil then t:SetColor(1.0, 1.0, 1.0) end
end

local WHITE = { 1.0, 1.0, 1.0 }

-- a few glitters twinkling round (cx, cy) in colour c ({r, g, b} from 0 to 1), `since` ms after they started
local function twinkles(cx, cy, since, count, rMin, rRange, scale, opacity, seed, c)
    for i = 1, count do
        local cycle = since * A3.twinkle_rate_per_ms + i / count
        local n = math.floor(cycle)
        local u = cycle - n
        local s = (i * 7.13 + n * 3.71 + seed) % 1
        local a = s * 2 * math.pi
        local r = rMin + rRange * ((s * 9.37 + i * 0.29) % 1)
        local glow = math.sin(u * math.pi)
        put(tex.glitter, cx + math.cos(a) * r, cy + math.sin(a) * r, scale * glow, glow * opacity, u * 90, c[1], c[2], c[3])
    end
end

-- a hit at (cx, cy), `since` ms after it: a star flare pops and fades while glitters in colour c fly out of it
local function burst(cx, cy, since, opacity, seed, size, c)
    if since < 0 then return end
    local f = since / A3.flare_ms
    if f < 1 then put(tex.flare, cx, cy, A3.flare_scale * size * popScale(f, 1.25), (1 - f * f) * opacity, f * 45) end
    if since > A3.burst_ms then return end
    local k = since / A3.burst_ms
    local r = (A3.burst_from_px + (A3.burst_to_px - A3.burst_from_px) * easeOut(k)) * size
    for i = 1, A3.burst_count do
        local a = i / A3.burst_count * 2 * math.pi + seed
        put(tex.glitter, cx + math.cos(a) * r, cy + math.sin(a) * r * 0.85, (1.3 - 0.7 * k) * size, (1 - k * k) * opacity, k * 90 + i * 37, c[1], c[2], c[3])
    end
end

-- ── Asagao's review ticket ───────────────────────────────────────────────────────────────────────────

-- the ticket with its top left at (x, y), its numbers in the font
local function drawTicket(x, y, s, op)
    if op <= 0 then return end
    local R = T.rect
    local t = tex.ticket
    t:SetScale(s, s)
    t:SetOpacity(op)
    t:DrawRectAtAnchor(x, y, R[1], R[2], R[3], R[4], "topleft")
    t:SetScale(1.0, 1.0)
    text(fonts.ticketScore, whole(info:GetScore(0)), x + T.score.x * s, y + T.score.y * s, "center",
        { color = col.ticketInk, scale = s, opacity = op, shadow = 0, maxW = T.score.max_width * s })
    local counts = { info:GetGoodCount(0), info:GetOkCount(0), info:GetBadCount(0), info:GetRollCount(0), info:GetMaxCombo(0), info.TotalHits }
    for i, v in ipairs(counts) do
        local column, row = (i <= 3) and 1 or 2, (i - 1) % 3 + 1
        text(fonts.ticketCount, whole(v), x + T.count_right[column] * s, y + T.count_rows[row] * s, "right",
            { color = col.ticketInk, scale = s, opacity = op, shadow = 0 })
    end
end

-- ── exams ────────────────────────────────────────────────────────────────────────────────────────────

local function isPercent(e) return e.Type == 0 or e.Type == 8 end
local function shown(e, v) return whole(v) .. (isPercent(e) and "%" or "") end

-- the play's count on an exam: a "less than" exam's Amount is what was left
local function count(e) return e.Range == 1 and math.max(0, e.Value - e.Amount) or e.Amount end

-- the best count before this play, -1 for none
local function bestCount(e)
    local b = e.PreviousBestAmount
    if b < 0 then return -1 end
    return e.Range == 1 and math.max(0, e.Value - b) or b
end

local function condition(e) return fill(e.Range == 1 and strings.less or strings.more, shown(e, e.Value)) end

-- each exam with its played songs (a per-song exam) and its status over them; the details' bar row takes the soul
-- gauge exam (or the first one, when the grid could not hold the rest) and the grid the others
local function buildExams()
    exams, barItem, gridItems = {}, nil, {}
    local last = math.min(info.DanSongReached, info.DanSongCount - 1)
    for k = 0, info.ExamCount - 1 do
        local e = info:GetExam(k)
        local item = { e = e, status = e.Status }
        if e.IsPerSong then
            item.songs = {}
            local worst = 2
            for j = 0, last do
                local se = e:GetSong(j)
                if se ~= nil then
                    item.songs[#item.songs + 1] = se
                    worst = math.min(worst, se.Status)
                end
            end
            if #item.songs > 0 then item.status = worst end
        end
        exams[#exams + 1] = item
    end
    for _, item in ipairs(exams) do
        if barItem == nil and item.e.Type == 0 then barItem = item else gridItems[#gridItems + 1] = item end
    end
    if barItem == nil and #gridItems > 6 then barItem = table.remove(gridItems, 1) end
end

-- values (each { s, color }) centred at (cx, cy), a dot between them with the same gap on each side; wider than the
-- clip box (x, y, w, h), they scroll on a loop
local function marquee(font, items, cx, cy, box, s, op)
    local gap = Layout.tile.separator_gap * s
    local dotW = font:Measure(SEPARATOR) * s
    local sepW = dotW + 2 * gap
    local total = 0
    for _, it in ipairs(items) do
        it.w = font:Measure(it.s) * s
        total = total + it.w
    end
    total = total + sepW * (#items - 1)
    local o = { scale = s, opacity = op, shadow = 0 }
    local sep = { scale = s, opacity = op, shadow = 0, color = col.dimInk }
    if total <= box[3] then
        local x = cx - total / 2
        for i, it in ipairs(items) do
            o.color = it.color
            text(font, it.s, x, cy, "left", o)
            x = x + it.w
            if i < #items then
                text(font, SEPARATOR, x + gap, cy, "left", sep)
                x = x + sepW
            end
        end
        return
    end
    local period = total + sepW
    local off = (clock * A1.marquee_px_per_ms) % period
    local right = box[1] + box[3]
    GRAPHICS:SetClip(box[1], box[2], box[3], box[4])
    for rep = 0, 1 do
        local x = box[1] - off + rep * period
        for _, it in ipairs(items) do
            if x < right and x + it.w > box[1] then
                o.color = it.color
                text(font, it.s, x, cy, "left", o)
            end
            x = x + it.w
            if x + gap < right and x + gap + dotW > box[1] then text(font, SEPARATOR, x + gap, cy, "left", sep) end
            x = x + sepW
        end
    end
    GRAPHICS:ClearClip()
end

-- ── the verdict ──────────────────────────────────────────────────────────────────────────────────────

local function buildIntro()
    local t = { tiles = {} }
    for k = 1, #exams do t.tiles[k] = A1.tiles_at + (k - 1) * A1.tile_step end
    t.stamp = (t.tiles[#t.tiles] or A1.tiles_at) + A1.tile_ms + A1.stamp_delay
    t.done = t.stamp + A1.stamp_ms + A1.end_delay
    return t
end

local function tagWidth() return fonts.tag:Measure(strings.newBest) + 2 * TAG.pad_x end

-- the New best! tag centred at (cx, cy): a gold pill on a darker rim, a sparkle twinkling at each end
local function drawTag(cx, cy, s, op)
    local w, h, r = tagWidth() * s, TAG.height * s, TAG.rim * s
    pill(cx, cy, w + 2 * r, h + 2 * r, 0, col.tagRim, op)
    pill(cx, cy, w, h, 0, col.tagFill, op)
    text(fonts.tag, strings.newBest, cx, cy, "center", { scale = s, opacity = op, color = col.tagText, shadowColor = col.tagShadow })
    local tw = 1 + 0.25 * math.sin(clock * Animation.twinkle_rad_per_ms)
    put(tex.sparkle, cx - w / 2 - TAG.sparkle_dx * s, cy - TAG.sparkle_dy * s, s * tw, op, 0)
    put(tex.sparkle, cx + w / 2 + TAG.sparkle_dx * s, cy + TAG.sparkle_dy * s, s * (2 - tw) * 0.8, op, 20)
end

-- under the ticket: New best! and what it gained over the previous best (when there was one), or the best
local function drawBest(x, y, op)
    local prev = info:GetPreviousBestScore(0)
    local k = progress(A1.best_at, A1.pop_ms)
    if k <= 0 then return end
    if newBest then
        local s = popScale(k, A1.overshoot)
        local w = tagWidth() + 2 * TAG.rim
        drawTag(x + w / 2, y, s, op)
        if prev > 0 then
            text(fonts.best, "+" .. whole(info:GetScore(0) - prev), x + w + V.best.gap, y, "left", { color = col.gain, shadow = 0, opacity = op * k })
        end
    elseif prev > 0 then
        text(fonts.best, fill(strings.best, whole(prev)), x, y, "left", { color = col.note, shadow = 0, opacity = op * k })
    end
end

-- an exam's tile centred at (cx, cy): its name on the tile tinted for its status, its result under it
local function drawTile(item, cx, cy, s, op)
    local L = Layout.tile
    local e = item.e
    local h = L.range_y + tex.examRange.Height
    local left, top = cx - L.width * s / 2, cy - h * s / 2
    local tile, range = tex.examTile, tex.examRange
    tile:SetScale(s, s)
    tile:SetOpacity(op)
    tile:SetColor(col.statusTile[statusIndex(item.status)])
    tile:DrawAtAnchor(left, top, "topleft")
    tile:SetColor(1.0, 1.0, 1.0)
    range:SetScale(s, s)
    range:SetOpacity(op)
    range:DrawAtAnchor(left, top + L.range_y * s, "topleft")
    tile:SetScale(1.0, 1.0)
    range:SetScale(1.0, 1.0)
    text(fonts.tileName, LANG:GetExamName(e.Type), cx, top + L.name_y * s, "center", { scale = s, opacity = op, maxW = L.name_max_width * s })
    local vy = top + L.value_y * s
    if item.songs ~= nil and #item.songs > 0 then
        local values = {}
        for i, se in ipairs(item.songs) do values[i] = { s = shown(se, count(se)), color = col.statusValue[statusIndex(se.Status)] } end
        local C = L.clip
        marquee(fonts.tileValue, values, cx, vy, { left + C[1] * s, top + C[2] * s, C[3] * s, C[4] * s }, s, op)
    else
        text(fonts.tileValue, shown(e, count(e)), cx, vy, "center",
            { color = col.statusValue[statusIndex(item.status)], scale = s, opacity = op, shadow = 0, maxW = (L.width - 20) * s })
    end
end

local function drawVerdict(ox, op)
    local TV = V.ticket
    local tw = T.rect[3] * TV.scale
    local left = TV.x - tw / 2
    if clock >= A1.ticket_at then
        local k = easeOut(progress(A1.ticket_at, A1.ticket_ms))
        drawTicket(left + ox + (1 - k) * A1.ticket_from_px, TV.y, TV.scale, op * clamp01(k * 1.5))
    end
    drawBest(left + V.best.dx + ox, V.best.y, op)

    -- the tiles: up to per_row a row, each row centred
    local P = V.tiles
    local th = Layout.tile.range_y + tex.examRange.Height
    local n = #exams
    for k, item in ipairs(exams) do
        local row = math.floor((k - 1) / P.per_row)
        local first = row * P.per_row
        local inRow = math.min(P.per_row, n - first)
        local column = k - 1 - first
        local cx = P.x - (inRow * Layout.tile.width + (inRow - 1) * P.gap) / 2 + column * (Layout.tile.width + P.gap) + Layout.tile.width / 2
        local cy = P.y + row * (th + P.row_gap) + th / 2
        local t0 = timeline.tiles[k]
        if clock >= t0 then
            local p = progress(t0, A1.tile_ms)
            drawTile(item, cx + ox, cy, popScale(p, A1.overshoot), op * clamp01(p * 2))
        end
    end
    text(fonts.hint, strings.details, V.hint[1] + ox, V.hint[2], "right", { color = col.note, shadow = 0, opacity = op * progress(timeline.done, 400) })
end

-- ── the details ──────────────────────────────────────────────────────────────────────────────────────

-- the judge words, each copied with clear pixels round it (a turned sticker would show their cells cut straight)
local function buildJudgeWords()
    local sheet = tex.judge
    if judgeWords ~= nil or sheet == nil or not (sheet.Loaded and sheet.Ready) then return end
    local P = D.stickers.padding
    local cw, ch = sheet.Width, math.floor(sheet.Height / judgeFrames)
    local w, h = cw + 2 * P, ch + 2 * P
    judgeCellH = ch
    judgeWords = {}
    for f = 0, judgeFrames - 1 do
        local c = CANVAS:CreateCanvas(w, h)
        c:PasteTexture(sheet, P, P - f * ch)
        c:FillPolygon({ 0, 0, w, 0, w, P, 0, P }, 0, 0, 0, 255, { mode = "cut" })
        c:FillPolygon({ 0, P + ch, w, P + ch, w, h, 0, h }, 0, 0, 0, 255, { mode = "cut" })
        c:Upload({ extendEdges = true, release = true })
        judgeWords[f] = c
    end
end

-- a sticker's label, w by h, centred at (cx, cy) and turned angle degrees
local function label(cx, cy, w, h, angle, color, opacity)
    local t = tex.sticker
    t:SetScale(w / (t.Width - 4), h / (t.Height - 4))
    t:SetRotation(angle)
    t:SetColor(color)
    t:SetOpacity(opacity)
    t:DrawAtAnchor(cx, cy, "center")
    t:SetColor(1.0, 1.0, 1.0)
end

-- the stickers over the sheet's top edge naming the columns: the judges pack's words (or the exam names when the
-- pack has no such frame) on white labels, a little turned
local function drawStickers(sx, sy, op)
    local S = D.stickers
    local names = { 1, 2, 3, 5 }   -- the columns' exam types, for a pack without the frame
    for k, frame in ipairs(S.frames) do
        local cx = sx + D.songs.x + D.song_bar.columns[k]
        local cy = sy + S.y
        local a = S.angles[k] or 0
        local word = judgeWords ~= nil and judgeWords[frame] or nil
        local ws = S.word_height / judgeCellH
        local w = word ~= nil and (word.Width - 2 * S.padding) * ws + 2 * S.pad_x
            or fonts.exam_name:Measure(LANG:GetExamName(names[k])) + 2 * S.pad_x
        label(cx + S.shadow_dy * 0.5, cy + S.shadow_dy, w, S.height, a, col.stickerShadow, op * S.shadow_opacity)
        label(cx, cy, w, S.height, a, col.stickerFill, op)
        if word ~= nil then
            put(word, cx, cy, ws, op, a)
            word:SetRotation(0)
        else
            fonts.exam_name:Draw(LANG:GetExamName(names[k]), cx, cy + nudge(fonts.exam_name), col.ink, col.clear, op, 1, 0, "center", 0, a)
        end
    end
end

-- rows scrolled so far (fractional): past three songs, a row every hold + move, on a loop
local function songScroll()
    local n = info.DanSongCount
    if n <= D.songs.visible then return 0 end
    local S = Animation.songs_scroll
    local period = S.hold_ms + S.move_ms
    local cycles = math.floor(clock / period)
    local into = clock - cycles * period
    local k = into < S.hold_ms and 0 or easeInOut((into - S.hold_ms) / S.move_ms)
    return (cycles + k) % n
end

-- song i (from 0) as the dan select lists it, with its counts in the columns on the right
local function drawSongRow(i, x, y, op)
    local s = info:GetDanSong(i)
    local B = D.song_bar
    tex.song:SetOpacity(op)
    tex.song:Draw(x, y)
    local c = stampColor(i)
    tex.songNb:SetColor(c.fill)
    tex.songNb:SetOpacity(op)
    tex.songNb:Draw(x, y)
    tex.songNb:SetColor(1.0, 1.0, 1.0)
    text(fonts.ordinal, ordinal(i + 1), x + B.nb_text[1], y + B.nb_text[2], "center", { color = col.white, shadowColor = c.shade, shadowOpacity = 1, opacity = op, maxW = 80 })
    local d = math.max(0, math.min(4, s.Difficulty))
    local dt = tex.diff[d]
    dt:SetOpacity(op)
    dt:Draw(x + B.diff[1], y + B.diff[2])
    text(fonts.level, tostring(s.Level), x + B.level[1], y + B.level[2], "center", { color = col.white, shadowColor = col.diff[d + 1], shadowOpacity = 1, opacity = op })
    local tw = B.text_right - B.title[1]
    local o = { color = col.ink, shadow = 0, opacity = op, maxW = tw }
    if s.Subtitle ~= "" then
        text(fonts.songTitle, s.Title, x + B.title[1], y + B.title[2], "left", o)
        text(fonts.songSub, s.Subtitle, x + B.sub[1], y + B.sub[2], "left", { color = col.dimInk, shadow = 0, opacity = op, maxW = tw })
    else
        text(fonts.songTitle, s.Title, x + B.title[1], y + B.count_y, "left", o)
    end
    local reached = i <= info.DanSongReached
    local counts = { s.Good, s.Ok, s.Bad, s.Roll }
    local sep, body = B.separator, tex.fillBody
    for k, v in ipairs(counts) do
        local cx = x + B.columns[k]
        body:SetColor(col.separator)
        body:SetOpacity(op * sep.opacity)
        body:SetScale(sep.width / body.Width, sep.height / (body.Height - 4))
        body:DrawAtAnchor(cx - B.column_width / 2, y + B.count_y, "center")
        body:SetColor(1.0, 1.0, 1.0)
        text(fonts.count, reached and whole(v) or "--", cx, y + B.count_y, "center",
            { color = reached and col.ink or col.dimInk, shadow = 0, opacity = op, maxW = B.column_width - 12 })
    end
end

local function drawSongs(sx, sy, op)
    local S = D.songs
    local n = info.DanSongCount
    local p = songScroll()
    local top, bottom = sy + S.clip[1], sy + S.clip[1] + S.clip[2]
    GRAPHICS:SetClip(sx, top, tex.sheet.Width, S.clip[2])
    for i = 0, n - 1 do
        local r = (i - p) % n
        for _, row in ipairs({ r, r - n }) do
            local y = sy + S.y + row * S.pitch
            if y + tex.song.Height > top and y < bottom then drawSongRow(i, sx + S.x, y, op) end
        end
    end
    GRAPHICS:ClearClip()
end

-- the bar row: the soul gauge filled to the gauge, its requirement notched; another exam filled to its percent
local function drawBarRow(item, sx, sy, op)
    local Gd = D.gauge
    local g = tex.gauge
    local x, y = sx + Gd.x, sy + Gd.y
    local e = item.e
    g:SetOpacity(op)
    g:SetColor(col.gaugeBack)
    g:Draw(x, y)
    local w = math.floor(g.Width * clamp01((e.Type == 0 and e.Amount or e.Percent) / 100))
    if w > 0 then
        g:SetColor(col.statusTile[statusIndex(item.status)])
        g:DrawRect(x, y, 0, 0, w, g.Height)
    end
    g:SetColor(1.0, 1.0, 1.0)
    if e.Type == 0 then
        local body = tex.fillBody
        body:SetColor(col.notch)
        body:SetOpacity(op)
        body:SetScale(3 / body.Width, (g.Height + 10) / (body.Height - 4))
        body:DrawAtAnchor(x + g.Width * clamp01(e.Value / 100), y + g.Height / 2, "center")
        body:SetColor(1.0, 1.0, 1.0)
    end
    local line = LANG:GetExamName(e.Type) .. "   " .. condition(e) .. "   " .. shown(e, count(e))
    local b = bestCount(e)
    if b >= 0 then line = line .. "   " .. fill(strings.best, shown(e, b)) end
    text(fonts.gauge, line, x + g.Width / 2, y + g.Height / 2, "center", { opacity = op, maxW = g.Width - 24 })
end

-- a per-song exam's block for song j (from 0), in the song's stamp colour
local function drawBlock(e, j, x, y, op)
    local K = D.blocks
    local ev = tex.examValue
    local c = stampColor(j)
    ev:SetColor(c.fill)
    ev:SetOpacity(op)
    ev:Draw(x, y)
    ev:SetColor(1.0, 1.0, 1.0)
    text(fonts.blockOrdinal, ordinal(j + 1), x + K.ordinal[1], y + K.ordinal[2], "center", { color = col.white, shadowColor = c.shade, shadowOpacity = 1, opacity = op })
    local se = e:GetSong(j)
    local done = se ~= nil and j <= info.DanSongReached
    text(fonts.blockValue, done and shown(se, count(se)) or "--", x + K.value[1], y + K.value[2], "center",
        { color = done and col.statusValue[statusIndex(se.Status)] or col.dimInk, shadow = 0, opacity = op, maxW = ev.Width - 12 })
    local b = se ~= nil and bestCount(se) or -1
    text(fonts.blockBest, b >= 0 and shown(se, b) or "--", x + K.best[1], y + K.best[2], "center",
        { color = col.dimInk, shadow = 0, opacity = op, maxW = ev.Width - 10 })
end

-- the per-song blocks of an exam bar; more songs than fit scroll on a loop
local function drawBlocks(e, x, y, w, long, op)
    local K = D.blocks
    local ev = tex.examValue
    local n = info.DanSongCount
    local x1, x2 = x + K.start_x, x + w - K.clip_pad_right
    local scrolling = n >= (long and K.scroll_from_long or K.scroll_from_short)
    local period = n * ev.Width
    local off = scrolling and (clock * Animation.blocks_px_per_ms) % period or 0
    GRAPHICS:SetClip(x1, y, x2 - x1, ev.Height)
    for j = 0, n - 1 do
        local bx = x1 + j * ev.Width - off
        if bx + ev.Width > x1 and bx < x2 then drawBlock(e, j, bx, y, op) end
        if scrolling and bx + period < x2 then drawBlock(e, j, bx + period, y, op) end
    end
    GRAPHICS:ClearClip()
end

-- an exam's bar: its name on the tile tinted for its status, its condition, then its result and best (or a block
-- per song)
local function drawExamBar(item, x, y, long, op)
    local E = D.exam_bar
    local e = item.e
    local bar = long and tex.examBarLong or tex.examBar
    bar:SetOpacity(op)
    bar:Draw(x, y)
    tex.examTile:SetColor(col.statusTile[statusIndex(item.status)])
    tex.examTile:SetOpacity(op)
    tex.examTile:Draw(x, y)
    tex.examTile:SetColor(1.0, 1.0, 1.0)
    tex.examRange:SetOpacity(op)
    tex.examRange:Draw(x, y + Layout.tile.range_y)
    text(fonts.exam_name, LANG:GetExamName(e.Type), x + E.name_text[1], y + E.name_text[2], "center", { opacity = op, maxW = E.text_max_width })
    text(fonts.exam_cond, condition(e), x + E.range_text[1], y + E.range_text[2], "center",
        { color = col.range[e.Range == 1 and 2 or 1], shadow = 0, opacity = op, maxW = E.text_max_width })
    if item.songs ~= nil then
        drawBlocks(e, x, y, bar.Width, long, op)
    else
        text(fonts.exam_value, shown(e, count(e)), x + E.value[1], y + E.value[2], "left",
            { color = col.statusValue[statusIndex(item.status)], shadow = 0, opacity = op })
        local b = bestCount(e)
        if b >= 0 then
            text(fonts.exam_best, fill(strings.best, shown(e, b)), x + E.best[1], y + E.best[2], "left", { color = col.dimInk, shadow = 0, opacity = op })
        end
    end
end

-- the grid: two columns of three rows as the dan select fills them; a row with one exam takes the long bar
local function drawGrid(sx, sy, op)
    local R = D.grid
    local bw, bh = tex.examBar.Width, tex.examBar.Height
    for row = 0, 2 do
        local a, b = gridItems[row + 1], gridItems[row + 4]
        local x, y = sx + R.x, sy + R.y + row * (bh + R.gap)
        if a ~= nil and b ~= nil then
            drawExamBar(a, x, y, false, op)
            drawExamBar(b, x + bw + R.gap, y, false, op)
        elseif a ~= nil then
            drawExamBar(a, x, y, true, op)
        end
    end
end

local function drawDetails(ox, op)
    local DT = D.ticket
    drawTicket(DT.x + ox, DT.y, DT.scale, op)
    local sx, sy = D.sheet[1] + ox, D.sheet[2]
    tex.sheet:SetOpacity(op)
    tex.sheet:Draw(sx, sy)
    drawSongs(sx, sy, op)
    drawStickers(sx, sy, op)
    if barItem ~= nil then drawBarRow(barItem, sx, sy, op) end
    drawGrid(sx, sy, op)
    text(fonts.hint, strings.summary, D.hint[1] + ox, D.hint[2], "right", { color = col.note, shadow = 0, opacity = op })
end

-- ── what both screens share ──────────────────────────────────────────────────────────────────────────

-- 0 on the verdict, 1 on the details, in between while they slide
local function swapK()
    if swapAt == nil then return 0 end
    local k = easeInOut(progress(swapAt, Animation.swap_ms))
    return view == 2 and k or 1 - k
end

-- the dan plate drops in, then moves between its places on the two screens
local function drawPlate(k, op)
    local plate = ROACTIVITY:GetROActivity("danplate")
    if plate == nil or op <= 0 then return end
    local x = lerp(V.plate[1], D.plate[1], k)
    local y = lerp(V.plate[2], D.plate[2], k) + (1 - easeOutBack(progress(0, A1.plate_ms), 1.2)) * A1.plate_from_px
    local c = info.DanTickColor
    plate:Draw(x, y, 255 * op, info.DanTick, c.R, c.G, c.B, info.Title)
end

-- the pass or fail stamp lands, then moves between its places on the two screens
local function drawStamp(k, op)
    if clock < timeline.stamp or op <= 0 then return end
    local p = progress(timeline.stamp, A1.stamp_ms)
    local x, y = lerp(V.stamp.x, D.stamp.x, k), lerp(V.stamp.y, D.stamp.y, k)
    local s = lerp(V.stamp.scale, D.stamp.scale, k) * (A1.stamp_from_scale + (1 - A1.stamp_from_scale) * easeOut(p))
    local t = tex.rank
    local cw = math.floor(t.Width / RANK_CELLS)
    local cell = info.DanResult > 0 and (2 * info.DanCombo + info.DanResult) or 0
    t:SetScale(s, s)
    t:SetOpacity(op * clamp01(p * 2))
    t:DrawRectAtAnchor(x, y, cell * cw, 0, cw, t.Height, "center")
    t:SetScale(1.0, 1.0)
end

-- the nameplate with the dan it had before this play (old) or the one it has now
local function drawNameplate(x, y, op, old)
    local plate = ROACTIVITY:GetROActivity("nameplate")
    if plate == nil then return end
    if old then
        local p = info.PreviousNameplateDan
        local dan = p.Title
        if dan ~= "" and p.Gold then dan = "<g.#FFE34A.#EA9622>" .. dan .. "</g>" end
        plate:Draw(4, x, y, 255 * op, 0, info.PlayerSide, dan, p.Tier)
    else
        plate:Draw(0, x, y, 255 * op, 0, info.PlayerSide)
    end
end

-- the new grade shows on the nameplate once the grade screen has put it there
local function gradeShown() return info.DanGradeChange == 0 or (grade ~= nil and clock >= grade.title) end

local function drawPlayer(op)
    if op <= 0 then return end
    local P = Layout.player
    drawNameplate(P.nameplate[1], P.nameplate[2], op, not gradeShown())
    local mods = ROACTIVITY:GetROActivity("modicons")
    if mods ~= nil and op >= 1 then
        if not mods.IsActive then mods:Activate() end
        mods:Draw(P.modicons[1], P.modicons[2], 0, "menu")
    end
end

-- ── the grade screen ─────────────────────────────────────────────────────────────────────────────────

local function characters(s)
    local out = {}
    local ok = pcall(function()
        for _, c in utf8.codes(s) do out[#out + 1] = utf8.char(c) end
    end)
    return ok and out or { s }
end

-- the dan plate's characters, or the grade's (the title's first two) when the plate's title is long
local function gradeCharacters()
    local plate = characters(info.Title)
    if #plate >= 1 and #plate <= G.kanji.max_count then return plate end
    return characters(info.DanGrade.Title)
end

local function buildGrade()
    local g = { t0 = clock, chars = gradeCharacters() }
    g.glow = g.t0 + A3.glow_at
    g.kanji = {}
    for i = 1, #g.chars do g.kanji[i] = g.t0 + A3.kanji_at + (i - 1) * A3.kanji_step end
    g.rise = (g.kanji[#g.chars] or g.t0 + A3.kanji_at) + A3.slam_ms + A3.scroll_delay
    g.unroll = g.rise + A3.rise_ms
    g.fade = g.unroll + A3.unroll_ms + A3.hold_ms
    g.jump = g.fade + A3.fade_ms + A3.jump_delay
    g.landed = g.jump + A3.jump_ms
    g.title = g.landed + A3.title_delay
    g.done = g.title + A3.title_ms
    return g
end

-- the camera's shake: each kanji's landing shakes it, dying down
local function shake()
    local x, y = 0, 0
    for i, t in ipairs(grade.kanji) do
        local u = clock - t - A3.slam_ms
        if u >= 0 and u < A3.shake_ms then
            local k = 1 - u / A3.shake_ms
            local a = A3.shake_px * k * k
            x = x + a * math.sin(u * 0.11 + i)
            y = y + a * math.cos(u * 0.137 + i * 2)
        end
    end
    return x, y
end

-- the grade's background over the whole screen (silver, gold or rainbow by the clear), turning slowly: it floods in
-- behind a flash of light and stays to the end
local function drawBackground()
    local since = clock - grade.glow
    if since < 0 then return end
    local P = G.background
    local b = tex.back
    b:SetScale(P.scale, P.scale)
    b:SetRotation(clock * A3.back_deg_per_ms)
    b:SetOpacity(easeOut(since / A3.glow_ms))
    b:DrawAtAnchor(W / 2, H / 2, "center")
    local pulse = 1 + 0.05 * math.sin(clock * Animation.twinkle_rad_per_ms)
    put(tex.glow, P.glow[1], P.glow[2], P.glow_scale * pulse, P.glow_opacity * easeOut(since / A3.glow_ms), 0)
    local f = since / A3.flash_ms
    if f < 1 then put(tex.glow, P.glow[1], P.glow[2], P.flash_from + (P.flash_to - P.flash_from) * easeOut(f), 1 - f * f, 0) end
end

local function kanjiCenter(i, ox, oy)
    local n = #grade.chars
    return W / 2 + (i - (n + 1) / 2) * G.kanji.pitch + ox, G.kanji.y + oy
end

-- behind kanji i once it landed: a soft light
local function drawKanjiLight(i, ox, oy, fade)
    local since = clock - grade.kanji[i] - A3.slam_ms
    if since < 0 then return end
    local x, y = kanjiCenter(i, ox, oy)
    put(tex.glow, x, y, G.kanji.glow_scale * easeOut(since / 300), 0.45 * fade, 0)
end

-- the glyph copied round (x, y) at radius px, `copies` times: one ring of an outline
local function ring(font, ch, x, y, px, copies, color, op, s)
    for j = 0, copies - 1 do
        local a = j * 2 * math.pi / copies
        font:Draw(ch, x + math.cos(a) * px * s, y + math.sin(a) * px * s, color, col.clear, op, s, 0, "center")
    end
end

-- kanji i slams in, then floats: a shadow, a dark outline, a light ring and a thin rim round it, its fill red or
-- gold as the pass
local function drawKanji(i, ox, oy, fade)
    local t0 = grade.kanji[i]
    if clock < t0 then return end
    local K = G.kanji
    local font = fonts.kanji
    local kc = col.kanji
    local k = progress(t0, A3.slam_ms)
    local s = A3.slam_from + (1 - A3.slam_from) * k * k
    local op = clamp01(k * 2.5) * fade
    if op <= 0 then return end
    local ch = grade.chars[i]
    local x, y = kanjiCenter(i, ox, oy)
    local since = clock - t0 - A3.slam_ms
    if since > 0 then y = y - K.bob_px * math.sin(since * Animation.twinkle_rad_per_ms * 0.5) end
    y = y + nudge(font) * s
    font:Draw(ch, x + K.shadow[1] * s, y + K.shadow[2] * s, col.kanjiShadow, col.clear, op * 0.5, s, 0, "center")
    ring(font, ch, x, y, K.outline_px, OUTER_RING, kc.outline, op, s)
    ring(font, ch, x, y, K.ring_px, OUTER_RING, kc.ring, op, s)
    ring(font, ch, x, y, K.rim_px, INNER_RING, kc.rim, op, s)
    font:Draw(kc.tag .. ch .. "</g>", x, y, col.white, col.clear, op, s, 0, "center")
end

-- the in-game dan clear scroll: it rises, then unrolls from its right end to its left
local function drawScroll(ox, oy, fade)
    if clock < grade.rise or tex.scroll == nil then return end
    local S = G.scroll
    local back, roller = tex.scroll, tex.roller
    local k = progress(grade.rise, A3.rise_ms)
    local y = H - (H - S.y) * (1 + 2.1 * (k - 1) ^ 3 + 1.1 * (k - 1) ^ 2) + oy
    local u = easeInOut(progress(grade.unroll, A3.unroll_ms))
    local left = S.x + ox
    local rx = left + (back.Width - roller.Width) * (1 - u)
    if u > 0 then
        back:SetOpacity(fade)
        back:DrawRect(rx, y, rx - left, 0, left + back.Width - rx, back.Height)
    end
    roller:SetOpacity(fade)
    roller:Draw(rx, y)
end

-- the character and the nameplate jump into the middle; the nameplate's dan turns to the new grade with glitter
local function drawJump()
    if clock < grade.jump then return end
    local C = G.chara
    local k = progress(grade.jump, A3.jump_ms)
    local dy = (1 - easeOutBack(k, A3.overshoot)) * G.jump_from_px
    if chara ~= nil then chara:DrawAtAnchor(C.x, C.y + dy, CHARACTER.ANIM_RESULT_CLEAR, "bottom", C.scale, C.scale, 255) end
    local kp = progress(grade.jump + A3.plate_delay, A3.jump_ms)
    local x, y = G.nameplate[1], G.nameplate[2] + (1 - easeOutBack(kp, A3.overshoot)) * G.jump_from_px
    drawNameplate(x, y, clamp01(kp * 4), clock < grade.title)
    local since = clock - grade.title
    if since < 0 then return end
    local dx, dy2 = x + G.dan_text[1], y + G.dan_text[2]
    local f = since / 350
    if f < 1 then put(tex.glow, dx, dy2, 0.5 + 0.8 * f, 1 - f, 0) end
    burst(dx, dy2, since, 1, 0.3, 0.45, WHITE)
    twinkles(dx, dy2, since, A3.twinkle_count, 28, 50, 0.6, 1, 0.5, WHITE)
end

local function drawGrade()
    drawBackground()
    local fade = 1 - progress(grade.fade, A3.fade_ms)
    if fade > 0 then
        local ox, oy = shake()
        for i = 1, #grade.chars do drawKanjiLight(i, ox, oy, fade) end
        for i = 1, #grade.chars do drawKanji(i, ox, oy, fade) end
        for i = 1, #grade.chars do
            local x, y = kanjiCenter(i, ox, oy)
            local since = clock - grade.kanji[i] - A3.slam_ms
            burst(x, y, since, fade, i, 1, col.kanji.accent)
            if since > 0 then twinkles(x, y, since, A3.twinkle_count, 110, 60, 0.9, fade, i * 0.37, col.kanji.accent) end
        end
        drawScroll(ox, oy, fade)
    end
    drawJump()
end

local function updateGrade()
    local g = grade
    at(g.glow, "g_glow", function() play("flash") end)
    for i, t in ipairs(g.kanji) do
        at(t + A3.slam_ms, "g_kanji" .. i, function() play("dong") end)
    end
    at(g.rise, "g_rise", function()
        if sounds.fanfare ~= nil then sounds.fanfare:Play() end
        duckMusic(A3.music_level)
    end)
    at(g.jump, "g_jump", function()
        duckMusic(100)
        if chara ~= nil then chara:ResetAnimationCounter(CHARACTER.ANIM_RESULT_CLEAR) end
    end)
    at(g.landed, "g_landed", function()
        if chara ~= nil then chara:PlayVoice(info.DanGrade.Gold and CHARACTER.VOICE_RESULT_DANGOLDPASS or CHARACTER.VOICE_RESULT_DANREDPASS) end
    end)
    at(g.title, "g_title", function() play("title") end)
    if chara ~= nil and clock >= g.jump then
        chara:SetAnimationDuration(CHARACTER.ANIM_RESULT_CLEAR, A3.character_ms)
        chara:Update(CHARACTER.ANIM_RESULT_CLEAR)
    end
end

-- ── the music ────────────────────────────────────────────────────────────────────────────────────────

local music = {}
local stageLevel, ownLevel = 100, 100   -- the result stage's (reward pop-ups) and the grade screen's, the lower wins

local function applyMusicLevel()
    for _, s in pairs(music) do s:SetVolumePercent(math.min(stageLevel, ownLevel)) end
end

duckMusic = function(percent)
    ownLevel = percent
    applyMusicLevel()
end

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
    stageLevel = percent
    applyMusicLevel()
end

-- ── lifecycle ────────────────────────────────────────────────────────────────────────────────────────

function onStart()
    local res = THEME:GetResolution()
    W, H = res.X, res.Y
    local F = Layout.font_sizes
    fonts.ticketScore = TEXT:CreateGlyphCached(F.ticket_score)
    fonts.ticketCount = TEXT:CreateGlyphCached(F.ticket_count)
    fonts.tag = TEXT:CreateGlyphCached(F.tag)
    fonts.best = TEXT:CreateGlyphCached(F.best)
    fonts.hint = TEXT:CreateGlyphCached(F.hint)
    fonts.tileName = TEXT:CreateGlyphCached(F.tile_name)
    fonts.tileValue = TEXT:CreateGlyphCached(F.tile_value)
    fonts.songTitle = TEXT:CreateGlyphCached(F.song_title)
    fonts.songSub = TEXT:CreateGlyphCached(F.song_sub)
    fonts.ordinal = TEXT:CreateGlyphCached(F.ordinal)
    fonts.level = TEXT:CreateGlyphCached(F.level)
    fonts.count = TEXT:CreateGlyphCached(F.count)
    fonts.gauge = TEXT:CreateGlyphCached(F.gauge)
    fonts.exam_name = TEXT:CreateGlyphCached(F.exam_name)
    fonts.exam_cond = TEXT:CreateGlyphCached(F.exam_cond)
    fonts.exam_value = TEXT:CreateGlyphCached(F.exam_value)
    fonts.exam_best = TEXT:CreateGlyphCached(F.exam_best)
    fonts.blockOrdinal = TEXT:CreateGlyphCached(F.block_ordinal)
    fonts.blockValue = TEXT:CreateGlyphCached(F.block_value)
    fonts.blockBest = TEXT:CreateGlyphCached(F.block_best)
    fonts.kanji = TEXT:CreateGlyphCached(F.kanji)

    col.text = Config.color(Layout.text_color)
    col.note = Config.color(Layout.note_color)
    col.gain = Config.color(Layout.gain_color)
    col.ink = Config.color(Layout.ink_color)
    col.dimInk = Config.color(Layout.dim_ink_color)
    col.ticketInk = Config.color(Layout.ticket_ink_color)
    col.shadow = Config.color(Layout.shadow_color)
    col.white = COLOR:CreateColorFromRGBA(255, 255, 255, 255)
    col.clear = COLOR:CreateColorFromRGBA(0, 0, 0, 0)
    col.statusTile, col.statusValue = {}, {}
    for i = 1, 3 do
        col.statusTile[i] = Config.color(Layout.status.tile[i])
        col.statusValue[i] = Config.color(Layout.status.value[i])
    end
    col.tagFill = Config.color(TAG.fill_color)
    col.tagRim = Config.color(TAG.rim_color)
    col.tagText = Config.color(TAG.text_color)
    col.tagShadow = Config.color(TAG.shadow_color)
    col.stamps = {}
    local shade = Layout.stamp_colors.shade
    for i, c in ipairs(Layout.stamp_colors.list) do
        col.stamps[i] = { fill = Config.color(c), shade = Config.color({ c[1] * shade, c[2] * shade, c[3] * shade }) }
    end
    col.diff = {}
    for i, c in ipairs(Layout.diff_colors.list) do col.diff[i] = Config.color(c) end
    col.separator = Config.color(D.song_bar.separator.color)
    col.stickerFill = Config.color(D.stickers.fill_color)
    col.stickerShadow = Config.color(D.stickers.shadow_color)
    col.gaugeBack = Config.color(D.gauge.back_color)
    col.notch = Config.color(D.gauge.notch_color)
    col.range = { Config.color(D.exam_bar.range_colors[1]), Config.color(D.exam_bar.range_colors[2]) }
    col.kanjiShadow = Config.color(G.kanji_shadow_color)
    col.kanjiSets = {}
    for _, name in ipairs({ "red", "gold" }) do
        local k = G.kanji_colors[name]
        col.kanjiSets[name] = { outline = Config.color(k.outline), ring = Config.color(k.ring), rim = Config.color(k.rim),
            tag = "<g.#" .. k.fill[1] .. ".#" .. k.fill[2] .. ">", accent = { k.accent[1] / 255, k.accent[2] / 255, k.accent[3] / 255 } }
    end
    col.kanji = col.kanjiSets.red
end

local TEXTURES = {
    rank = "Rank.png", ticket = "StatePanel_Main.png",
    sheet = "Sheet/Contents.png", song = "Sheet/DanSong.png", songNb = "Sheet/DanSongNb.png", gauge = "Sheet/Gauge.png",
    examBar = "Sheet/DanExam.png", examBarLong = "Sheet/DanExamLong.png", examValue = "Sheet/ExamValue.png",
    examRange = "Sheet/ExamRange.png", examTile = "Sheet/ExamTile.png",
    fillEnd = "Tag/fill_end.png", fillBody = "Tag/fill_body.png", sparkle = "Tag/sparkle.png", sticker = "Tag/sticker.png",
}

local function loadTextures()
    for k, path in pairs(TEXTURES) do tex[k] = TEXTURE:CreateTexture("Textures/" .. path) end
    tex.diff = {}
    for d = 0, 4 do tex.diff[d] = TEXTURE:CreateTexture("Textures/Sheet/DanSongDiff_" .. d .. ".png") end
    -- the judges pack the player picked, as the play screen shows it
    local judgeFile = THEME:GetNotesSkinFile("judges", "Judge.png")
    if judgeFile == "" then judgeFile = "5_Game/Judge.png" end
    tex.judge = TEXTURE:CreateTexture(GRAPHICS_DIR .. judgeFile)
    judgeFrames = THEME:GetJudgeFrames()
    -- a stage's own background for its dan (the pagoda's), in the dan result's blues
    local own = PlayStrings.get(STAGE_BACKGROUND_KEY)
    if own ~= "" then
        tex.stageBack = TEXTURE:CreateTexture("../../" .. own)
        stageTint = GRADIENT:Create(Layout.stage_background.stops, 1)
        tex.stageBack:SetGradientMap(stageTint)
    end
end

-- a stage's own background over the result background, covering the screen's width
local function drawStageBackground()
    local b = tex.stageBack
    if b == nil then return end
    local s = W / b.Width
    b:SetScale(s, s)
    b:DrawAtAnchor(W / 2, H / 2, "center")
end

local function loadSounds()
    for name, s in pairs(Animation.sounds) do
        if name ~= "_note" and STORAGE:FileExists(s.file) then
            sounds[name] = SOUND:CreateSFX(s.file)
            sounds[name]:SetVolumePercent(s.volume)
        end
    end
end

-- what the grade screen needs: the background of its clear, its effects, the scroll and fanfare of the grade, the
-- character's clear animation and voice
local function loadGrade()
    local g = info.DanGrade
    local tier = math.max(0, math.min(2, g.Tier))
    tex.back = TEXTURE:CreateTexture("Textures/Grade/Back_" .. BACKS[tier + 1] .. ".png")
    tex.glow = TEXTURE:CreateTexture("Textures/Grade/glow.png")
    tex.flare = TEXTURE:CreateTexture("Textures/Grade/flare.png")
    tex.glitter = TEXTURE:CreateTexture("Textures/Grade/glitter.png")
    tex.glow:SetBlendMode("add")
    tex.flare:SetBlendMode("add")
    col.kanji = g.Gold and col.kanjiSets.gold or col.kanjiSets.red
    local name = (g.Gold and "Gold_" or "Red_") .. SCROLLS[tier + 1]
    tex.scroll = TEXTURE:CreateTexture("Textures/Grade/Scroll_" .. name .. ".png")
    tex.roller = TEXTURE:CreateTexture("Textures/Grade/Scroll_Roller.png")
    local F = Animation.fanfares
    local fanfare = "Sounds/Grade/fanfare_" .. (g.Gold and F.gold or F.red)[tier + 1] .. ".ogg"
    if STORAGE:FileExists(fanfare) then
        sounds.fanfare = SOUND:CreateSFX(fanfare)
        sounds.fanfare:SetVolumePercent(F.volume)
    end
    chara = CHARACTER:GetPlayerCharacter(0)
    chara:LoadAnimation(CHARACTER.ANIM_RESULT_CLEAR)
    chara:LoadVoice(g.Gold and CHARACTER.VOICE_RESULT_DANGOLDPASS or CHARACTER.VOICE_RESULT_DANREDPASS)
end

local function disposeAll(t)
    for _, v in pairs(t) do
        if type(v) == "table" then disposeAll(v) else pcall(function() v:Dispose() end) end
    end
end

function activate(result)
    deactivate()
    info = result
    clock, played, view, swapAt, grade = 0, {}, 1, nil, nil
    stageLevel, ownLevel = 100, 100
    loadTextures()
    loadSounds()
    buildExams()
    timeline = buildIntro()
    newBest = info:IsSaved(0) and info:GetScore(0) > info:GetPreviousBestScore(0)
    strings.newBest = tr("DAN_RESULTS_NEW_BEST", "New best!")
    strings.best = tr("DAN_RESULTS_BEST", "best {0}")
    strings.more = tr("DAN_RESULTS_EXAM_MORE", "{0} or more")
    strings.less = tr("DAN_RESULTS_EXAM_LESS", "less than {0}")
    strings.details = tr("DAN_RESULTS_DETAILS", "Left / Right: details")
    strings.summary = tr("DAN_RESULTS_SUMMARY", "Left / Right: summary")
    if info.DanGradeChange ~= 0 and info.DanGrade.Title ~= "" then loadGrade() end
    startMusic(Config.optional(Animation.music, "intro"), Config.optional(Animation.music, "loop"))
    -- the glyphs shown later are baked now, not on their first draw while the screen animates
    local off = -10000
    for _, f in pairs(fonts) do f:Draw("0123456789%-", off, off, col.text, col.clear) end
    for i = 0, info.DanSongCount - 1 do
        local s = info:GetDanSong(i)
        fonts.songTitle:Draw(s.Title, off, off, col.text, col.clear)
        fonts.songSub:Draw(s.Subtitle, off, off, col.text, col.clear)
    end
    local kanji = info.Title .. info.DanGrade.Title
    fonts.kanji:Draw(kanji .. col.kanji.tag .. kanji .. "</g>", off, off, col.text, col.clear)
    active = true
end

function deactivate()
    active = false
    stopMusic()
    for _, s in pairs(sounds) do pcall(function() s:Stop(); s:Dispose() end) end
    sounds = {}
    disposeAll(tex)
    tex = {}
    if stageTint ~= nil then pcall(function() stageTint:Dispose() end) end
    stageTint = nil
    for _, c in pairs(judgeWords or {}) do pcall(function() c:Dispose() end) end
    judgeWords = nil
    if chara ~= nil then
        pcall(function()
            chara:DisposeAnimation(CHARACTER.ANIM_RESULT_CLEAR)
            chara:DisposeVoice(CHARACTER.VOICE_RESULT_DANGOLDPASS)
            chara:DisposeVoice(CHARACTER.VOICE_RESULT_DANREDPASS)
        end)
    end
    chara, info, timeline, grade = nil, nil, nil, nil
end

function update()
    if not active then return end
    clock = clock + fps.deltaTime * 1000
    updateMusic()
    at(A1.best_at, "best", function() if newBest then play("best") end end)
    at(timeline.stamp, "stamp", function() play("rank") end)
    if grade ~= nil then updateGrade() end
end

-- the grade screen goes on to the character's jump at once: the kanji and the scroll shown so far fade out while
-- it jumps, the ones still to come never show
local function jumpNow()
    local g = grade
    for i, t in ipairs(g.kanji) do
        if t > clock then g.kanji[i], played["g_kanji" .. i] = math.huge, true end
    end
    if g.rise > clock then g.rise, played.g_rise = math.huge, true end
    g.glow = math.min(g.glow, clock)
    g.fade = math.min(g.fade, clock)
    local shift = clock - g.jump
    g.jump, g.landed, g.title, g.done = clock, g.landed + shift, g.title + shift, g.done + shift
end

-- true while still animating: the verdict jumps to its end with no sound; the grade screen, before the character
-- jumps in, goes on to that jump
function skip()
    if not active then return false end
    if grade ~= nil then
        if clock >= grade.jump then return false end
        jumpNow()
        return true
    end
    if clock >= timeline.done then return false end
    clock = timeline.done
    played.best, played.stamp = true, true
    return true
end

-- once the verdict is shown, decide goes on: from the verdict to the details, then to the grade screen when a new
-- or better dan grade has one (false then lets the reward pop-ups follow)
function advance()
    if not active or grade ~= nil or clock < timeline.done then return false end
    if view == 1 then
        swap()
        return true
    end
    if chara == nil then return false end
    grade = buildGrade()
    return true
end

-- once the verdict is shown, the details slide in, or back
function swap()
    if not active or grade ~= nil or clock < timeline.done then return false end
    if swapAt ~= nil and clock - swapAt < Animation.swap_ms then return false end
    view = 3 - view
    swapAt = clock
    return true
end

function draw()
    if not active then return end
    buildJudgeWords()
    local op = grade ~= nil and 1 - easeOut(progress(grade.t0, A3.fade_out_ms)) or 1
    drawStageBackground()
    if op > 0 then
        local k = swapK()
        drawPlate(k, op)
        if k < 1 then drawVerdict(-k * W, op) end
        if k > 0 then drawDetails((1 - k) * W, op) end
        drawStamp(k, op)
        drawPlayer(op)
    end
    if grade ~= nil then drawGrade() end
end

function afterSongEnum() end

function onDestroy()
    deactivate()
    for _, f in pairs(fonts) do pcall(function() f:Dispose() end) end
    fonts = {}
end
