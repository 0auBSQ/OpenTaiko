---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- results_tower ROActivity: the tower result screen.
-- The result stage calls activate(info) with a LuaResultInfo, update() and draw() every frame, skip() on
-- decide (true while still animating) and deactivate().

local TowerArt = require("TowerArt")

-- positions, sizes and colours: Config/layout.json; the timeline, the motion and the sounds: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local TOP, BOTTOM, ROPES, LIVES, TOWER, TAG = Layout.top_board, Layout.bottom_board, Layout.ropes, Layout.lives_bar, Layout.tower, Layout.tag
local BOARDS, SWAY, GLITTER, SHINE = Animation.boards, Animation.boards.sway, Animation.glitter, Animation.shine

local GRAPHICS = "../../../Graphics/"
local RANK_SHEET = GRAPHICS .. "8_TowerResult/ScoreRankEffect.png"   -- 7 cells side by side: 10 25 50 75 %, clear, full combo, perfect
local RANK_CELLS = 7
local SKY_DIR = GRAPHICS .. "5_Game/5_Background/Tower/Down/"       -- <look>/Sky_Gradient.png: the gameplay tower sky, day at its bottom
local MARKS = { 10, 25, 50, 75 }

local res_w, res_h = 1920, 1080
local judgeFrames = 5   -- the selected judges pack's Judge.png frames (Good, OK, Bad, Ad-Lib, BOOM, then Rolls and Combo)

local tex = {}
local fonts = {}
local col = {}
local sounds = {}            -- each a list of notes (one for a single sound)
local strings = {}

local info = nil
local boards = nil
local timeline = nil
local art, look = nil, nil   -- the tower's pieces and its look
local chara = nil            -- the player's character, and the tower animations it shows: stand, climb, finish
local charaAnims = nil
local charaAnim = nil        -- the one showing now
local judgeWords = nil       -- the judge words, each copied with clear pixels round it (built once the sheet is loaded)
local shine = nil            -- the badge's cell copied twice: as it is, and with the light band cut out of it
local played = {}
local notes = {}             -- the last note played in each running phase (the climb, the score, the lives)
local clock = 0              -- seconds on the timeline
local idle = 0               -- seconds since activate, for the sway (it keeps going after a skip)
local active = false

-- ── helpers ──────────────────────────────────────────────────────────────────────────────────────────

local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

local function fill(s, value) return (s:gsub("{0}", tostring(value))) end

-- "812,450"
local function thousands(n)
    local s = tostring(math.floor(n))
    local out = s:reverse():gsub("(%d%d%d)", "%1,"):reverse()
    return (out:gsub("^,", ""))
end

local function clamp01(x) return math.max(0, math.min(1, x)) end
local function easeOut(x) x = clamp01(x); return 1 - (1 - x) ^ 3 end
local function easeInOut(x) x = clamp01(x); return x < 0.5 and 4 * x * x * x or 1 - (-2 * x + 2) ^ 3 / 2 end

-- note i (from 1) of a sound, clamped to its notes
local function play(name, i)
    local set = sounds[name]
    if set == nil or #set == 0 then return end
    local s = set[math.max(1, math.min(#set, i or 1))]
    if s ~= nil then s:Play() end
end

-- a sound once, when the timeline first reaches it
local function cue(name, key, i)
    if played[key] then return end
    played[key] = true
    play(name, i)
end

-- how far an event that starts at t0 and lasts d has gone, 0 to 1
local function progress(t0, d) return clamp01((clock - t0) / d) end

-- a pop: up to the overshoot then back to 1
local function popScale(t0, d, over)
    local k = progress(t0, d)
    if k <= 0 then return 0 end
    if k < 0.6 then return over * easeOut(k / 0.6) end
    return over + (1 - over) * easeOut((k - 0.6) / 0.4)
end

-- ── boards ───────────────────────────────────────────────────────────────────────────────────────────

local function newBoard(t, cfg, phase)
    local w, h = t.Width, t.Height
    return { tex = t, w = w, h = h, cx = cfg.center_x, cy = cfg.top + h / 2, phase = phase,
             hx = w / 2 - ROPES.hole_inset.x, hy = h / 2 - ROPES.hole_inset.y,
             pose = { x = cfg.center_x, y = cfg.top + h / 2, angle = 0, s = 0, c = 1 } }
end

-- a board's drop at the clock: `from` px off its place before t0, falling over fall_s, then bouncing on its
-- ropes. Returns the offset and the seconds since it landed (negative before)
local function drop(t0, from)
    local u = clock - t0
    if u < 0 then return from, -1 end
    if u < BOARDS.fall_s then
        local k = u / BOARDS.fall_s
        return from * (1 - k * k), -1
    end
    local v = u - BOARDS.fall_s
    local bo = BOARDS.bounce
    return bo.amplitude_px * math.sin(v * bo.rad_per_s) * math.exp(-v * bo.decay_per_s), v
end

-- a landed board's swing from its drop, dying down into the idle sway: tilt in degrees, x and y offsets
local function sway(phase, landed)
    if landed < 0 then return 0, 0, 0 end
    local settle = math.min(1, landed / BOARDS.settle_s)
    local sw, a, x, y = BOARDS.swing, SWAY.angle, SWAY.x, SWAY.y[1]
    local swing = sw.amplitude_deg * math.sin(landed * sw.rad_per_s + phase) * math.exp(-landed * sw.decay_per_s)
    local t, ph = idle, phase
    local angle = swing + settle * (a[1].amplitude * math.sin(t * a[1].rad_per_s + ph * a[1].phase_scale) + a[2].amplitude * math.sin(t * a[2].rad_per_s + ph * a[2].phase_scale))
    local dx = swing * sw.x_px_per_deg + settle * (x[1].amplitude * math.sin(t * x[1].rad_per_s + ph * x[1].phase_scale) + x[2].amplitude * math.sin(t * x[2].rad_per_s + ph * x[2].phase_scale))
    local dy = settle * y.amplitude * math.sin(t * y.rad_per_s + ph * y.phase_scale)
    return angle, dx, dy
end

local function setAngle(q, angle)
    q.angle = angle
    local rad = math.rad(angle)
    q.s, q.c = math.sin(rad), math.cos(rad)
end

-- the top board: from above the screen
local function poseTop(b)
    local q = b.pose
    local dy, landed = drop(BOARDS.top_start_s, -BOARDS.drop_px)
    local angle, sx, sy = sway(b.phase, landed)
    q.x, q.y = b.cx + sx, b.cy + dy + sy
    setAngle(q, angle)
    return q
end

-- a point on the board (from its top left corner) on screen; the tilt is counter-clockwise like SetRotation
local function at(b, q, x, y)
    local dx, dy = x - b.w / 2, y - b.h / 2
    return q.x + dx * q.c + dy * q.s, q.y - dx * q.s + dy * q.c
end

-- the bottom board hangs from the top one: it waits behind it, its top hidden_px under the top board's, then
-- drops to its place; it follows the top board's tilt and adds its own sway
local function poseBottom(b, top, qt)
    local q = b.pose
    local rest = b.cy - top.cy
    local hidden = BOARDS.hidden_px + b.h / 2 - top.h / 2
    local dy, landed = drop(BOARDS.bottom_start_s, hidden - rest)
    local angle, sx, sy = sway(b.phase, landed)
    q.x, q.y = at(top, qt, top.w / 2 + (b.cx - top.cx) + sx, top.h / 2 + rest + dy + sy)
    setAngle(q, qt.angle + angle)
    return q
end

local function drawRope(x0, y0, x1, y1)
    local dx, dy = x1 - x0, y1 - y0
    local len = math.sqrt(dx * dx + dy * dy)
    if len < 1 then return end
    local rope = tex.rope
    rope:SetScale(1, len / rope.Height)
    rope:SetRotation(math.deg(math.atan(dx, dy)))
    rope:DrawAtAnchor((x0 + x1) / 2, (y0 + y1) / 2, "center")
end

-- the board on its two ropes, from (lx0, ly0) and (rx0, ry0) to its top holes, the knots on the holes
local function drawBoard(b, q, lx0, ly0, rx0, ry0)
    local lx, ly = at(b, q, b.w / 2 - b.hx, b.h / 2 - b.hy)
    local rx, ry = at(b, q, b.w / 2 + b.hx, b.h / 2 - b.hy)
    drawRope(lx0, ly0, lx, ly)
    drawRope(rx0, ry0, rx, ry)
    b.tex:SetRotation(q.angle)
    b.tex:DrawAtAnchor(q.x, q.y, "center")
    tex.knot:DrawAtAnchor(lx, ly, "center")
    tex.knot:DrawAtAnchor(rx, ry, "center")
end

-- a texture at a board point, turned with the board
local function putXY(t, b, q, x, y, sx, sy, angle, opacity)
    local px, py = at(b, q, x, y)
    t:SetScale(sx, sy)
    t:SetRotation(q.angle + (angle or 0))
    t:SetOpacity(opacity or 1)
    t:DrawAtAnchor(px, py, "center")
    t:SetOpacity(1)
end

local function put(t, b, q, x, y, scale, angle, opacity)
    putXY(t, b, q, x, y, scale or 1, scale or 1, angle, opacity)
end

-- ── text ─────────────────────────────────────────────────────────────────────────────────────────────

-- the clear margin a glyph line keeps left, right and under its ink (the engine's text padding)
local function pad(font) return font.BoxHeight - math.ceil(font.LineHeight) end

-- a glyph line anchored at its middle sits its ink high by half the box's bottom padding
local function nudge(font) return math.floor(pad(font) / 2) + Layout.text_nudge end

-- the ink width of s
local function width(font, s, scale) return font:Measure(s) * (scale or 1) end

-- a text at a board point, its ink's left edge, centre or right edge at x ("left", "center" or "right") and
-- its line's middle at y; no outline, a soft shadow under it. o: scale, opacity, maxW, angle, color, shadow (px)
local function text(font, s, b, q, x, y, anchor, o)
    if s == nil or s == "" then return end
    o = o or {}
    local scale, opacity = o.scale or 1, o.opacity or 1
    if opacity <= 0 then return end
    local p = pad(font) * scale
    if anchor == "left" then x = x - p elseif anchor == "right" then x = x + p end
    local angle = q.angle + (o.angle or 0)
    local sx, sy = at(b, q, x, y)
    sy = sy + nudge(font) * scale
    local sh = o.shadow or Layout.shadow_px
    if sh > 0 then
        font:Draw(s, sx, sy + sh, o.shadowColor or col.shadow, col.clear, opacity * Layout.shadow_opacity, scale, o.maxW or 0, anchor, 0, angle)
    end
    font:Draw(s, sx, sy, o.color or col.text, col.clear, opacity, scale, o.maxW or 0, anchor, 0, angle)
end

-- ── timeline ─────────────────────────────────────────────────────────────────────────────────────────

local function buildTimeline()
    local tl = {}
    local F = Animation.floor
    tl.floorStart = F.start_s
    tl.floorEnd = tl.floorStart + F.count_s
    tl.badge = tl.floorEnd + Animation.badge.delay_s
    tl.badgeLanded = tl.badge + Animation.badge.stamp_s
    tl.floorBest = tl.floorEnd + Animation.new_best.delay_s
    local J = Animation.judges
    tl.judges = {}
    for i = 1, #BOTTOM.judges do tl.judges[i] = tl.badgeLanded + J.delay_s + (i - 1) * J.step_s end
    tl.scoreStart = tl.judges[#tl.judges] + Animation.score.delay_s
    tl.scoreEnd = tl.scoreStart + Animation.score.count_s
    tl.scoreBest = tl.scoreEnd + Animation.new_best.delay_s
    tl.livesStart = tl.scoreEnd + Animation.lives.delay_s
    tl.livesEnd = tl.livesStart + math.max(1, info.MaxLives) * Animation.lives.step_s
    tl.topLanded = BOARDS.top_start_s + BOARDS.fall_s
    tl.bottomLanded = BOARDS.bottom_start_s + BOARDS.fall_s
    tl.done = math.max(tl.badgeLanded + GLITTER.burst_s, tl.floorBest + Animation.new_best.pop_s,
        tl.scoreBest + Animation.new_best.pop_s, tl.livesEnd, tl.bottomLanded)
    return tl
end

-- ── the scene: the sky and the tower ─────────────────────────────────────────────────────────────────

-- how far the floor count has gone, 0 to 1, and the floor the count is at (fractional)
local function climbProgress() return easeInOut(progress(timeline.floorStart, Animation.floor.count_s)) end
local function climbFloor() return info.ReachedFloor * climbProgress() end

-- the play reached the top when it ended with lives left, as the play screen tells
local function topReached() return info.Lives > 0 end

-- the tower piece the character ends on, from 0: the roof (one over the last floor) on the top, else the floor
-- it was playing
local function endPiece()
    local total = math.max(1, info.TotalFloors)
    if topReached() then return math.min(total, info.ReachedFloor) end
    return math.max(0, math.min(total, info.ReachedFloor - 1))
end

-- the character on the piece standing on the screen's bottom edge
local function drawChara()
    if chara == nil or charaAnim == nil then return end
    local C = TOWER.chara
    chara:Draw(TOWER.x + C.dx, res_h + C.dy, charaAnim, C.scale, C.scale)
end

-- f: the tower piece on the screen's bottom edge (fractional while it climbs)
local function drawScene(f)
    local F = Animation.floor
    local total = math.max(1, info.TotalFloors)
    -- the sky strip from its bottom (day) to its top (night), as the gameplay tower background scrolls it
    local sky = tex.sky
    if sky ~= nil and sky.Width > 0 then
        local night = math.min(1, f / math.max(F.night_floors, total * F.night_share))
        local span = math.max(0, sky.Height - res_h)
        sky:DrawRect(0, 0, 0, math.floor(span * (1 - night)), math.min(sky.Width, res_w), res_h)
    end
    -- piece f stands on the screen's bottom edge: the first floor at the start, the character's last one at the end
    if art ~= nil and art:ready(look) then
        local pitch = art:height(look, TOWER.scale, 1) - art:height(look, TOWER.scale, 0)
        art:draw(look, TOWER.x, res_h + f * pitch, TOWER.scale, total, { clipTop = 0, clipBottom = res_h })
    end
    drawChara()
end

-- ── copies made once their sheets are loaded ─────────────────────────────────────────────────────────

local function loaded(t) return t ~= nil and t.Loaded and t.Ready end

-- a canvas area erased (with its edge `soft` px wide): the polygon's points as x1, y1, x2, y2, ...
local function cut(canvas, points, soft)
    canvas:FillPolygon(points, 0, 0, 0, 255, { mode = "cut", soft = soft or 1 })
end

-- each judge word with judge_padding clear pixels round it: the packs' words reach their cells' edges, which a
-- turned board would show cut straight
local function buildJudgeWords()
    if judgeWords ~= nil or not loaded(tex.judge) then return end
    local sheet = tex.judge
    local P = BOTTOM.judge_padding
    local cw, ch = sheet.Width, math.floor(sheet.Height / judgeFrames)
    local w, h = cw + 2 * P, ch + 2 * P
    judgeWords = {}
    for f = 0, judgeFrames - 1 do
        local c = CANVAS:CreateCanvas(w, h)
        c:PasteTexture(sheet, P, P - f * ch)
        cut(c, { 0, 0, w, 0, w, P, 0, P })                     -- the frames above and under it
        cut(c, { 0, P + ch, w, P + ch, w, h, 0, h })
        c:Upload({ extendEdges = true, release = true })
        judgeWords[f] = c
    end
end

-- the badge's cell, for its shine: the light is the badge's own pixels inside a band, added over it (so it lights
-- the badge alone, never the board round it)
local function buildShine()
    if shine ~= nil or info.TowerRank <= 0 or not loaded(tex.ranks) then return end
    local sheet = tex.ranks
    local cw, ch = math.floor(sheet.Width / RANK_CELLS), sheet.Height
    local base = CANVAS:CreateCanvas(cw, ch)
    base:PasteTexture(sheet, -(info.TowerRank - 1) * cw, 0)
    local frame = CANVAS:CreateCanvas(cw, ch)
    frame:SetBlendMode("add")
    shine = { base = base, frame = frame, w = cw, h = ch }
end

-- the light band across the badge drawn at (sx, sy), `since` seconds after it landed
local function drawShine(sx, sy, scale, angle, since)
    if shine == nil then return end
    local t = since - SHINE.first_delay_s
    if t < 0 then return end
    local k = (t % SHINE.period_s) / SHINE.sweep_s
    if k > 1 then return end
    cue("flash", "shine")
    -- the band runs angle_deg off the vertical; u measures across it
    local th = math.rad(SHINE.angle_deg)
    local nx, ny = math.cos(th), -math.sin(th)
    local tx, ty = -ny, nx
    local u0 = math.min(0, shine.h * ny)
    local u1 = shine.w * nx + math.max(0, shine.h * ny)
    local band = SHINE.band_px
    local c = u0 - band + (u1 - u0 + 2 * band) * easeInOut(k)
    local L = shine.w + shine.h
    local f = shine.frame
    f:CopyFrom(shine.base)
    for _, side in ipairs({ -1, 1 }) do
        local d = c + side * band / 2
        local ax, ay = d * nx + L * tx, d * ny + L * ty
        local bx, by = d * nx - L * tx, d * ny - L * ty
        cut(f, { ax, ay, bx, by, bx + side * L * nx, by + side * L * ny, ax + side * L * nx, ay + side * L * ny }, SHINE.soft_px)
    end
    f:Upload()
    f:SetScale(scale, scale)
    f:SetRotation(angle)
    f:SetOpacity(SHINE.opacity)
    for _ = 1, SHINE.passes do f:DrawAtAnchor(sx, sy, "center") end
end

-- the burst of sparkles when the badge lands, then a few twinkling round it
local function drawGlitter(b, q, cx, cy, since)
    local G = GLITTER
    local t = tex.glitter
    if since < G.burst_s then
        local k = since / G.burst_s
        local r = G.burst_from_px + (G.burst_to_px - G.burst_from_px) * easeOut(k)
        local s = G.burst_scale * math.sin(math.min(1, k * 2) * math.pi / 2)
        for i = 1, G.burst_count do
            local a = (i / G.burst_count) * 2 * math.pi + 0.35 * math.sin(i * 2.7)
            local ri = r * (0.75 + 0.25 * ((i * 0.618) % 1))
            t:SetColor(i % 2 == 0 and col.gold or col.white)
            put(t, b, q, cx + math.cos(a) * ri, cy + math.sin(a) * ri, s * (0.7 + 0.3 * ((i * 0.37) % 1)), G.spin_deg * k + i * 37, 1 - k * k)
        end
    end
    local tw = since - G.burst_s * 0.6
    if tw > 0 then
        for i = 1, G.twinkle_count do
            local cycle = tw * G.twinkle_rate_per_s + i / G.twinkle_count
            local n = math.floor(cycle)
            local u = cycle - n
            local seed = (i * 7.13 + n * 3.71) % 1
            local a = seed * 2 * math.pi
            local r = G.radius_min + G.radius_range * ((seed * 9.37 + i * 0.29) % 1)
            local glow = math.sin(u * math.pi)
            t:SetColor(i % 2 == 0 and col.gold or col.white)
            put(t, b, q, cx + math.cos(a) * r, cy + math.sin(a) * r, G.twinkle_scale * glow, u * 90, glow)
        end
    end
    t:SetColor(1.0, 1.0, 1.0)
end

-- ── the boards' contents ─────────────────────────────────────────────────────────────────────────────

-- a rounded bar from x0 to x1 on the board: a disc at each end and the body stretched between their centres,
-- so the moving end stays round and every edge stays smooth when the board turns
local function pill(b, q, x0, x1, y, h, color, opacity, scale)
    scale = scale or 1
    local w = x1 - x0
    if w <= 0.5 then return end
    local e, body = tex.fill_end, tex.fill_body
    e:SetColor(color)
    if w < h then
        put(e, b, q, (x0 + x1) / 2, y, w / (e.Width - 4), 0, opacity)
    else
        local s = h / (e.Height - 4)
        if w > h then
            body:SetColor(color)
            putXY(body, b, q, (x0 + x1) / 2, y, (w - h) / body.Width, h / (body.Height - 4), 0, opacity)
            body:SetColor(1.0, 1.0, 1.0)
        end
        put(e, b, q, x0 + h / 2, y, s, 0, opacity)
        put(e, b, q, x1 - h / 2, y, s, 0, opacity)
    end
    e:SetColor(1.0, 1.0, 1.0)
end

-- the width of the New best! tag
local function tagWidth() return width(fonts.tag, strings.newBest) + 2 * TAG.pad_x end

-- the New best! tag centred at (cx, cy) on the board, popping from t0: a gold pill on a darker rim, its text
-- white, a sparkle twinkling at each end
local function drawTag(b, q, cx, cy, t0)
    local s = popScale(t0, Animation.new_best.pop_s, Animation.new_best.overshoot)
    if s <= 0 then return end
    local w, h, r = tagWidth() * s, TAG.height * s, TAG.rim * s
    pill(b, q, cx - w / 2 - r, cx + w / 2 + r, cy, h + 2 * r, col.tagRim)
    pill(b, q, cx - w / 2, cx + w / 2, cy, h, col.tagFill)
    text(fonts.tag, strings.newBest, b, q, cx, cy, "center", { scale = s, color = col.tagText, shadowColor = col.tagShadow })
    local tw = 1 + Animation.new_best.twinkle_scale * math.sin(idle * Animation.new_best.twinkle_rad_per_s)
    put(tex.sparkle, b, q, cx - w / 2 - TAG.sparkle_dx * s, cy - TAG.sparkle_dy * s, s * tw, 0)
    put(tex.sparkle, b, q, cx + w / 2 + TAG.sparkle_dx * s, cy + TAG.sparkle_dy * s, s * (2 - tw) * 0.8, 20)
end

local function drawFloorBar(b, q, climb, shown)
    local B = TOP.bar
    local trackW = tex.floor_track.Width - 4
    local barW, barH = B.width, B.height
    local left = B.x + (trackW - barW) / 2
    put(tex.floor_track, b, q, B.x + trackW / 2, B.y)
    local total = math.max(1, info.TotalFloors)
    local function px(floors) return left + barW * math.min(1, floors / total) end
    local prev = info.PreviousBestFloor
    if info.IsNewBestFloor and prev > 0 then
        pill(b, q, left, px(climb), B.y, barH, col.gain)
        pill(b, q, left, px(math.min(prev, climb)), B.y, barH, col.side)
    else
        pill(b, q, left, px(climb), B.y, barH, col.side)
    end
    pill(b, q, left + B.shine_inset, px(climb) - B.shine_inset, B.y + B.shine_dy, B.shine_height, col.white, B.shine_opacity)
    for i, m in ipairs(MARKS) do
        local reached = shown * 100 >= total * m
        local mx = left + barW * m / 100
        tex.floor_mark:SetColor(reached and col.shadow or col.markOffTint)
        put(tex.floor_mark, b, q, mx, B.y)
        tex.floor_mark:SetColor(1.0, 1.0, 1.0)
        text(fonts.mark, tostring(m) .. "%", b, q, mx, B.y + B.label_dy, "center",
            { color = reached and col.text or col.markOff, shadow = reached and Layout.shadow_px or 0 })
        if reached and clock < timeline.floorEnd + 0.05 then cue("mark", "mark" .. m, i) end
    end
    -- the top: one note over the last mark's
    if shown >= total and clock < timeline.floorEnd + 0.05 then cue("mark", "mark100", #MARKS + 1) end
    if prev > 0 then
        put(tex.best_notch, b, q, px(prev), B.y + B.notch_dy)
    end
end

local function drawBadge(b, q)
    if info.TowerRank <= 0 or clock < timeline.badge then return end
    cue("stamp", "badge")
    local k = easeOut(progress(timeline.badge, Animation.badge.stamp_s))
    local s = (Animation.badge.from_scale + (1 - Animation.badge.from_scale) * k) * TOP.badge.scale
    local sheet = tex.ranks
    local cw = math.floor(sheet.Width / RANK_CELLS)
    local sx, sy = at(b, q, TOP.badge.x, TOP.badge.y)
    sheet:SetScale(s, s)
    sheet:SetRotation(q.angle)
    sheet:SetOpacity(k)
    sheet:DrawRectAtAnchor(sx, sy, (info.TowerRank - 1) * cw, 0, cw, sheet.Height, "center")
    sheet:SetOpacity(1)
    local since = clock - timeline.badgeLanded
    if since >= 0 then
        drawShine(sx, sy, s, q.angle, since)
        drawGlitter(b, q, TOP.badge.x, TOP.badge.y, since)
    end
end

local function drawTop(b, q, climb)
    local pillTex = info.TowerSide == 1 and tex.pill_spicy or tex.pill_sweet
    put(pillTex, b, q, TOP.pill.x, TOP.pill.y, TOP.pill.scale)
    text(fonts.title, info.Title, b, q, TOP.title.x, TOP.title.y, "center", { maxW = TOP.title.max_width })
    text(fonts.subtitle, info.Subtitle, b, q, TOP.subtitle.x, TOP.subtitle.y, "center", { maxW = TOP.subtitle.max_width, color = col.dim })
    text(fonts.caption, strings.reached, b, q, TOP.caption.x, TOP.caption.y, "left", { color = col.dim })

    -- the floor, right-aligned where its final count ends, the total after it
    local shown = math.floor(climb + 0.5)
    local right = TOP.floor.x + width(fonts.floor, tostring(info.ReachedFloor))
    text(fonts.floor, tostring(shown), b, q, right, TOP.floor.y, "right", { shadow = Layout.big_shadow_px })
    local totalX = right + TOP.floor_total.gap
    text(fonts.floorTotal, strings.ofFloors, b, q, totalX, TOP.floor.y + TOP.floor_total.dy, "left", { color = col.dim })

    drawFloorBar(b, q, climb, shown)

    -- the best block after the total: the New best! tag and its lines, or the best and the floors to go
    local B = TOP.best
    local x0 = math.max(B.min_x, totalX + width(fonts.floorTotal, strings.ofFloors) + B.gap)
    local cx = x0 + B.width / 2
    local prev = info.PreviousBestFloor
    if info.IsNewBestFloor then
        drawTag(b, q, cx, B.tag_y, timeline.floorBest)
        if clock >= timeline.floorBest then
            cue("jingle", "floorBest")
            if prev > 0 then
                local k = progress(timeline.floorBest, Animation.new_best.pop_s)
                text(fonts.best, fill(strings.previousBest, prev), b, q, cx, B.line1_y, "center", { color = col.dim, opacity = k })
                local gained = info.ReachedFloor - prev
                text(fonts.gain, fill(gained == 1 and strings.gainedOne or strings.gained, gained), b, q, cx, B.line2_y, "center", { color = col.gain, opacity = k })
            end
        end
    elseif prev > 0 and clock >= timeline.floorEnd then
        local k = progress(timeline.floorEnd, Animation.new_best.pop_s)
        text(fonts.best, fill(strings.best, prev), b, q, cx, B.tag_y, "center", { color = col.dim, opacity = k })
        local left = prev - info.ReachedFloor
        if left > 0 then
            text(fonts.best, fill(left == 1 and strings.toGoOne or strings.toGo, left), b, q, cx, B.line1_y, "center", { color = col.dim, opacity = k })
        end
    end

    drawBadge(b, q)
end

local function livesColor()
    local most = info.MaxLives
    local ratio = most > 0 and info.Lives / most or 0
    local last = info.Lives == 1 and most ~= 1
    if ratio > LIVES.blue_above and not last then return col.blue end
    if ratio >= LIVES.yellow_from and not last then return col.yellow end
    return col.red
end

-- the lives segments shown filled so far
local function livesShown()
    local n = math.max(1, info.MaxLives)
    return math.min(info.Lives, math.floor(clamp01((clock - timeline.livesStart) / (n * Animation.lives.step_s)) * n + 0.0001))
end

local function drawLives(b, q)
    local L = BOTTOM.lives
    local s = L.scale
    local back = tex.bar_back
    local cx = L.right - back.Width * s / 2
    put(back, b, q, cx, L.y, s)
    text(fonts.caption, strings.lives, b, q, L.right - back.Width * s + 2, L.caption_y, "left", { color = col.dim })
    local n = math.max(1, info.MaxLives)
    local segments = (n >= LIVES.plain_from) and 1 or n
    local gap = 0
    for _, g in ipairs(LIVES.segment_gaps) do
        if n <= g.up_to then gap = g.gap; break end
    end
    if segments == 1 then gap = 0 end
    local barW, barH = LIVES.bar_width, LIVES.bar_height
    local segW = (barW - gap * (segments - 1)) / segments
    local lives = livesShown()
    local fillCol = livesColor()
    local seg = tex.bar_segment
    local innerW, innerH = seg.Width - 4, seg.Height - 4   -- its clear border scales with it
    for i = 1, segments do
        local filled = segments == 1 and (lives > 0) or (i <= lives)
        local w = segW
        if segments == 1 then w = barW * clamp01(lives / n) end
        if w > 0.2 then
            local x = cx - barW * s / 2 + ((i - 1) * (segW + gap) + w / 2) * s
            local sx, sy = at(b, q, x, L.y)
            seg:SetScale(w * s / innerW, barH * s / innerH)
            seg:SetRotation(q.angle)
            seg:SetColor(filled and fillCol or col.empty)
            seg:DrawAtAnchor(sx, sy, "center")
        end
    end
    seg:SetColor(1.0, 1.0, 1.0)
end

local JUDGE_VALUE = {
    good = function() return info:GetGoodCount(0) end, ok = function() return info:GetOkCount(0) end, bad = function() return info:GetBadCount(0) end,
    rolls = function() return info:GetRollCount(0) end, combo = function() return info:GetMaxCombo(0) end,
    adlib = function() return info:GetADLibCount(0) end, boom = function() return info:GetBoomCount(0) end,
}

local function drawBottom(b, q)
    local J = Animation.judges
    for i, j in ipairs(BOTTOM.judges) do
        local t0 = timeline.judges[i]
        if clock >= t0 then
            cue("judge", "judge" .. i, i)
            local k = progress(t0, J.pop_s)
            local s = J.pop_scale + (1 - J.pop_scale) * easeOut(k)
            local x, y = BOTTOM.columns[j.column], BOTTOM.rows[j.row]
            local word = judgeWords ~= nil and judgeWords[j.frame] or nil
            if word ~= nil then
                -- the selected judges pack's word, its cell starting at the column
                local cw = word.Width - 2 * BOTTOM.judge_padding
                put(word, b, q, x + cw * BOTTOM.judge_scale / 2, y + BOTTOM.judge_dy, BOTTOM.judge_scale * s, 0, k)
            end
            text(fonts.judge, thousands(JUDGE_VALUE[j.name]()), b, q, x + BOTTOM.row_width, y, "right", { scale = s, opacity = k })
        end
    end

    -- the score counts up; its New best! tag after it, or its best
    local S = BOTTOM.score
    text(fonts.caption, strings.score, b, q, S.x, S.caption_y, "left", { color = col.dim })
    local final = thousands(info:GetScore(0))
    if clock >= timeline.scoreStart then
        local shown = info:GetScore(0) * easeOut(progress(timeline.scoreStart, Animation.score.count_s))
        text(fonts.score, thousands(shown), b, q, S.x, S.y, "left", { shadow = Layout.big_shadow_px })
    end
    local SB = BOTTOM.score_best
    local x0 = S.x + width(fonts.score, final) + SB.gap
    if info.IsNewBestScore then
        local cx = x0 + tagWidth() / 2
        drawTag(b, q, cx, SB.tag_y, timeline.scoreBest)
        if clock >= timeline.scoreBest then
            cue("jingle_high", "scoreBest")
            if info.PreviousBestScore > 0 then
                local k = progress(timeline.scoreBest, Animation.new_best.pop_s)
                text(fonts.best, "+" .. thousands(info:GetScore(0) - info.PreviousBestScore), b, q, cx, SB.line_y, "center", { color = col.gain, opacity = k })
            end
        end
    elseif info.PreviousBestScore > 0 and clock >= timeline.scoreEnd then
        local k = progress(timeline.scoreEnd, Animation.new_best.pop_s)
        text(fonts.best, fill(strings.best, thousands(info.PreviousBestScore)), b, q, x0, S.y, "left", { color = col.dim, opacity = k })
    end

    drawLives(b, q)
end

-- the player's nameplate and mod icons, under the boards
local function drawPlayer()
    local P = Layout.player
    local plate = ROACTIVITY:GetROActivity("nameplate")
    if plate ~= nil then plate:Draw(0, P.nameplate.x, P.nameplate.y, 255, 0, info.PlayerSide) end
    local mods = ROACTIVITY:GetROActivity("modicons")
    if mods ~= nil then
        if not mods.IsActive then mods:Activate() end
        mods:Draw(P.modicons.x, P.modicons.y, 0, "menu")
    end
end

-- ── lifecycle ────────────────────────────────────────────────────────────────────────────────────────

function onStart()
    local res = THEME:GetResolution()
    res_w, res_h = res.X, res.Y
    local F = Layout.font_sizes
    for _, name in ipairs({ "title", "subtitle", "caption", "floor", "floor_total", "best", "gain", "tag", "mark", "judge", "score" }) do
        fonts[name] = TEXT:CreateGlyphCached(F[name])
    end
    fonts.floorTotal = fonts.floor_total
    col.text = Config.color(Layout.text_color)
    col.dim = Config.color(Layout.dim_color)
    col.gain = Config.color(Layout.gain_color)
    col.shadow = Config.color(Layout.shadow_color)
    col.markOff = Config.color(Layout.mark_off_color)
    col.markOffTint = Config.color(Layout.mark_off_tint)
    col.sweet = Config.color(Layout.side_colors.sweet)
    col.spicy = Config.color(Layout.side_colors.spicy)
    col.tagFill = Config.color(TAG.fill_color)
    col.tagRim = Config.color(TAG.rim_color)
    col.tagText = Config.color(TAG.text_color)
    col.tagShadow = Config.color(TAG.shadow_color)
    col.blue = Config.color(LIVES.blue_color)
    col.yellow = Config.color(LIVES.yellow_color)
    col.red = Config.color(LIVES.red_color)
    col.empty = Config.color(LIVES.empty_color)
    col.gold = Config.color(GLITTER.gold)
    col.white = COLOR:CreateColorFromRGBA(255, 255, 255, 255)
    col.clear = COLOR:CreateColorFromRGBA(0, 0, 0, 0)
end

local TEXTURES = { "board_top", "board_bottom", "rope", "knot", "floor_track", "fill_end", "fill_body", "floor_mark",
    "best_notch", "pill_sweet", "pill_spicy", "sparkle", "glitter", "bar_back", "bar_segment" }

-- the player's character on the tower animations its play ended with: tired or not, its clear one on the top,
-- its fail one otherwise (a character missing one falls back as in play)
local function loadChara()
    local C, tired = CHARACTER, info.IsTired
    chara = CHARACTER:GetPlayerCharacter(0)
    charaAnims = {
        stand = tired and C.ANIM_GAME_TOWER_STANDING_TIRED or C.ANIM_GAME_TOWER_STANDING,
        climb = tired and C.ANIM_GAME_TOWER_CLIMBING_TIRED or C.ANIM_GAME_TOWER_CLIMBING,
        finish = not topReached() and C.ANIM_GAME_TOWER_FAIL
            or (tired and C.ANIM_GAME_TOWER_CLEAR_TIRED or C.ANIM_GAME_TOWER_CLEAR),
    }
    for _, a in pairs(charaAnims) do chara:LoadAnimation(a) end
end

local function disposeChara()
    if chara ~= nil and charaAnims ~= nil then
        for _, a in pairs(charaAnims) do pcall(function() chara:DisposeAnimation(a) end) end
    end
    chara, charaAnims, charaAnim = nil, nil, nil
end

-- standing until the climb, climbing while the floors go by (if any), then the end of the play
local function updateChara()
    if chara == nil then return end
    local A = charaAnims
    local a = A.stand
    if clock >= timeline.floorEnd then a = A.finish
    elseif clock >= timeline.floorStart and endPiece() > 0 then a = A.climb end
    if a ~= charaAnim then
        charaAnim = a
        chara:ResetAnimationCounter(a)
    end
    chara:SetAnimationCyclesFromBPM(a, info.Bpm > 0 and info.Bpm or 120)
    chara:Update(a)
end

local function loadSounds()
    for name, s in pairs(Animation.sounds) do
        if name ~= "_note" then
            local set = {}
            local count = Config.optional(s, "count") or 1
            for i = 1, count do
                local path = count > 1 and string.format(s.file, i - 1) or s.file
                local snd = SOUND:CreateSFX(path)
                snd:SetVolumePercent(s.volume)
                set[i] = snd
            end
            sounds[name] = set
        end
    end
end

function activate(result)
    deactivate()
    info = result
    for _, name in ipairs(TEXTURES) do tex[name] = TEXTURE:CreateTexture("Textures/" .. name .. ".png") end
    tex.glitter:SetBlendMode("add")
    tex.ranks = TEXTURE:CreateTexture(RANK_SHEET)
    -- the judges pack the player picked, as the play screen and the best plays list show it
    local judgeFile = THEME:GetNotesSkinFile("judges", "Judge.png")
    if judgeFile == "" then judgeFile = "5_Game/Judge.png" end
    tex.judge = TEXTURE:CreateTexture(GRAPHICS .. judgeFile)
    judgeFrames = THEME:GetJudgeFrames()
    -- the chart's tower and the sky of its look
    art = TowerArt.load()
    look = art:resolve(info.TowerType ~= "" and info.TowerType or nil)
    art:preload(look)
    local skyFile = SKY_DIR .. look .. "/Sky_Gradient.png"
    if STORAGE:FileExists(skyFile) then tex.sky = TEXTURE:CreateTexture(skyFile) end
    loadChara()
    loadSounds()
    strings.reached = tr("TOWER_HUD_REACHED", "Reached floor")
    strings.lives = tr("TOWER_HUD_LIVES", "Lives")
    strings.score = tr("TOWER_RESULTS_SCORE", "Score")
    strings.newBest = tr("TOWER_RESULTS_NEW_BEST", "New best!")
    strings.previousBest = tr("TOWER_RESULTS_PREVIOUS_BEST", "previous best {0}")
    strings.gained = tr("TOWER_RESULTS_FLOORS_GAINED", "+{0} floors")
    strings.gainedOne = tr("TOWER_RESULTS_FLOORS_GAINED_ONE", "+{0} floor")
    strings.best = tr("TOWER_RESULTS_BEST", "best {0}")
    strings.toGo = tr("TOWER_RESULTS_FLOORS_TO_GO", "{0} floors to go")
    strings.toGoOne = tr("TOWER_RESULTS_FLOORS_TO_GO_ONE", "{0} floor to go")
    strings.ofFloors = fill(tr("TOWER_RESULTS_OF_FLOORS", "/ {0} F"), info.TotalFloors)
    col.side = info.TowerSide == 1 and col.spicy or col.sweet

    boards = {
        top = newBoard(tex.board_top, TOP, BOARDS.top_phase),
        bottom = newBoard(tex.board_bottom, BOTTOM, BOARDS.bottom_phase),
    }
    timeline = buildTimeline()
    played, notes, clock, idle = {}, { step = { t = -1, floor = 0 }, count = -1, lives = 0 }, 0, 0
    -- the glyphs the screen shows are baked here, not on their first draw while it animates
    local off = -10000
    for _, f in pairs(fonts) do f:Draw("0123456789,%+/", off, off, col.text, col.clear) end
    fonts.title:Draw(info.Title, off, off, col.text, col.clear)
    fonts.subtitle:Draw(info.Subtitle, off, off, col.text, col.clear)
    fonts.tag:Draw(strings.newBest, off, off, col.text, col.clear)
    for _, s in ipairs({ strings.reached, strings.lives, strings.score }) do fonts.caption:Draw(s, off, off, col.text, col.clear) end
    fonts.floorTotal:Draw(strings.ofFloors, off, off, col.text, col.clear)
    for _, s in ipairs({ strings.previousBest, strings.best, strings.toGo, strings.toGoOne }) do fonts.best:Draw(s, off, off, col.text, col.clear) end
    for _, s in ipairs({ strings.gained, strings.gainedOne }) do fonts.gain:Draw(s, off, off, col.text, col.clear) end
    active = true
end

function deactivate()
    active = false
    for _, set in pairs(sounds) do
        for _, s in ipairs(set) do pcall(function() s:Stop(); s:Dispose() end) end
    end
    sounds = {}
    for _, t in pairs(tex) do pcall(function() t:Dispose() end) end
    tex = {}
    for _, c in pairs(judgeWords or {}) do pcall(function() c:Dispose() end) end
    if shine ~= nil then
        pcall(function() shine.base:Dispose(); shine.frame:Dispose() end)
    end
    if art ~= nil then art:dispose() end
    disposeChara()
    info, boards, timeline, art, look, judgeWords, shine = nil, nil, nil, nil, nil, nil, nil
end

function update()
    if not active then return end
    clock = clock + fps.deltaTime
    idle = idle + fps.deltaTime
    local tl = timeline
    if clock >= tl.topLanded then cue("plank", "topLanded") end
    if clock >= BOARDS.bottom_start_s then cue("slide", "bottomDrop") end
    if clock >= tl.bottomLanded then cue("plank_light", "bottomLanded") end
    updateChara()
    -- the climb: a step note for the floors going by, at most steps_per_s a second, climbing the scale
    if clock >= tl.floorStart and clock < tl.floorEnd + 0.05 then
        local k = climbProgress()
        local floor = math.floor(info.ReachedFloor * k + 0.5)
        local st = notes.step
        if floor > st.floor and clock - st.t >= 1 / Animation.floor.steps_per_s then
            st.floor, st.t = floor, clock
            play("step", 1 + math.floor(k * (#(sounds.step or {}) - 1) + 0.5))
        end
    end
    -- the score: a count note every tick_s while it counts, climbing the scale
    if clock >= tl.scoreStart and clock < tl.scoreEnd then
        local n = math.floor((clock - tl.scoreStart) / Animation.score.tick_s)
        if n > notes.count then
            notes.count = n
            local k = progress(tl.scoreStart, Animation.score.count_s)
            play("count", 1 + math.floor(k * (#(sounds.count or {}) - 1) * 0.6))
        end
    end
    -- the lives: a count note for each segment filling, going on up the scale
    local lives = livesShown()
    if lives > notes.lives then notes.lives = lives; play("count", 5 + lives) end
end

-- true while the screen was still animating: everything jumps to its end, with no sound
function skip()
    if not active or clock >= timeline.done then return false end
    clock = timeline.done
    for _, k in ipairs({ "topLanded", "bottomDrop", "bottomLanded", "badge", "floorBest", "scoreBest",
        "mark10", "mark25", "mark50", "mark75", "mark100" }) do played[k] = true end
    for i = 1, #BOTTOM.judges do played["judge" .. i] = true end
    notes.step.floor, notes.count, notes.lives = math.huge, math.huge, math.huge
    return true
end

function draw()
    if not active then return end
    buildJudgeWords()
    buildShine()
    local climb = climbFloor()
    drawScene(endPiece() * climbProgress())
    local top, bottom = boards.top, boards.bottom
    local qt = poseTop(top)
    local qb = poseBottom(bottom, top, qt)
    -- the bottom board first: it hangs from behind the top one, its ropes from under_top above the top board's
    -- bottom edge
    local lx, ly = at(top, qt, top.w / 2 - bottom.hx, top.h - ROPES.under_top)
    local rx, ry = at(top, qt, top.w / 2 + bottom.hx, top.h - ROPES.under_top)
    drawBoard(bottom, qb, lx, ly, rx, ry)
    drawBottom(bottom, qb)
    drawBoard(top, qt, top.cx - top.hx, ROPES.top, top.cx + top.hx, ROPES.top)
    drawTop(top, qt, climb)
    drawPlayer()
end

function afterSongEnum() end

function onDestroy()
    deactivate()
    for _, f in pairs(fonts) do pcall(function() f:Dispose() end) end
    fonts = {}
end
