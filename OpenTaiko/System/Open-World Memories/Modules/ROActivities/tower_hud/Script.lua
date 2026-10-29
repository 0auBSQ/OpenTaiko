---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- tower_hud ROActivity
-- ------------------------------------------------------------------------------------------------------
-- The tower climb's head-up display, in the band between the top of the screen and the lane: one wooden
-- plank hanging from the top of the screen on two ropes, swaying a little. It holds three rows:
--   the "Reached floor" title
--   Floor n/k, the current floor on a small split-flap panel that flips when a floor is reached
--   Lives, a bar of one segment per life (a plain bar from a hundred lives up), blue while the lives are
--   above half, yellow above a fifth, red below (and on the last life)
-- The art is in Textures/ (the plank, the flap, a rope tile stretched to length, the knots, the bar back
-- and a white segment tinted per life); the texts come from the skin locale and centre on the plank.
-- Driven by the gameplay screen: activate(maxFloor), update() and draw() every frame, deactivate(). The
-- floor and the lives are read from PLAYSTATE.

-- positions, sizes and colours: Config/layout.json; the flip and the sway: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local PLANK, LIVES, FLIP, SWAY = Layout.plank, Layout.lives, Animation.flip, Animation.sway
local TILT1, TILT2 = SWAY.tilt[1], SWAY.tilt[2]
local SWAY_X1, SWAY_X2 = SWAY.x[1], SWAY.x[2]
local SWAY_Y = SWAY.y

local FONT_SIZES = Layout.font_sizes
local TITLE_SIZE, LABEL_SIZE, NUMBER_SIZE, LIVES_SIZE = FONT_SIZES.title, FONT_SIZES.label, FONT_SIZES.number, FONT_SIZES.lives
local CENTER_X = PLANK.center_x
local ROPE_TOP = PLANK.top    -- where the two ropes start, just above the screen
local ROPE_LENGTH = PLANK.rope_length   -- from there down to the plank's top edge
local HOLE_INSET_X, HOLE_INSET_Y = PLANK.hole_inset.x, PLANK.hole_inset.y   -- the rope holes, from the plank's ends and top edge
local ROWS = PLANK.rows   -- the rows' centre heights, from the plank's top edge
local FLIP_SECONDS = FLIP.flip_s
local BAR_W, BAR_H = LIVES.bar_width, LIVES.bar_height
local PLAIN_BAR_FROM = LIVES.plain_from   -- lives from which the bar has no segments

local fontTitle, fontLabel, fontNumber, fontLives
local colText, colOutline
local colLives = {}

local tex = {}                 -- the textures by file name
local plank = nil              -- texture, w, h, cx, cy (rest position), phase, holes, the rows' heights, pose
local strings = {}             -- the localized texts
local layout = {}              -- the centres of the floor and lives rows' pieces, as x offsets from the plank's centre
local maxFloor = 0

local floor, lives, maxLives = 1, 5, 5
local flip = nil               -- { from, to, t } while the floor number flips
local clock = 0
local active = false

-- a locale string; an empty one counts (the floor prefix is empty where the suffix carries the word)
local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

-- a glyph line anchored at its middle sits its ink high by half the box's bottom padding
local function nudge(gf) return math.floor((gf.BoxHeight - math.ceil(gf.LineHeight)) / 2) + Layout.text_nudge end

-- ── layout ───────────────────────────────────────────────────────────────────────────────────────────

local function buildLayout()
    strings.title = tr("TOWER_HUD_REACHED", "Reached floor")
    strings.prefix = tr("TOWER_HUD_FLOOR_PREFIX", "Floor ")
    strings.suffix = tr("TOWER_HUD_FLOOR_SUFFIX", "")
    strings.lives = tr("TOWER_HUD_LIVES", "Lives")
    strings.rest = "/" .. tostring(maxFloor) .. strings.suffix

    -- the floor row holds "<prefix>[ n ]/k<suffix>", centred as a group
    local prefixW = fontLabel:Measure(strings.prefix)
    local flapW = tex.flap.Width
    local restW = fontLabel:Measure(strings.rest)
    local floorGap = Layout.floor.gap
    local left = -(prefixW + flapW + floorGap + restW) / 2
    layout.prefixX = left + prefixW / 2
    layout.flapX = left + prefixW + flapW / 2
    layout.restX = left + prefixW + flapW + floorGap + restW / 2

    -- the lives row holds the label then the bar
    local livesW = fontLives:Measure(strings.lives)
    local contentW = livesW + LIVES.gap + BAR_W
    layout.livesX = -contentW / 2 + livesW / 2
    layout.barX = -contentW / 2 + livesW + LIVES.gap + BAR_W / 2

    local t = tex.plank
    local w, h = t.Width, t.Height
    local cy = ROPE_TOP + ROPE_LENGTH + h / 2
    plank = { tex = t, w = w, h = h, cx = CENTER_X, cy = cy, phase = SWAY.phase,
              hx = w / 2 - HOLE_INSET_X, hy = h / 2 - HOLE_INSET_Y,
              titleY = ROWS.title - h / 2, floorY = ROWS.floor - h / 2, livesY = ROWS.lives - h / 2,
              pose = { x = CENTER_X, y = cy, angle = 0, s = 0, c = 1 } }
end

-- ── lifecycle ────────────────────────────────────────────────────────────────────────────────────────

function onStart()
    for _, name in ipairs({ "plank", "flap", "rope", "knot", "bar_back", "bar_segment" }) do
        tex[name] = TEXTURE:CreateTexture("Textures/" .. name .. ".png")
    end
    fontTitle = TEXT:CreateGlyphCached(TITLE_SIZE)
    fontLabel = TEXT:CreateGlyphCached(LABEL_SIZE)
    fontNumber = TEXT:CreateGlyphCached(NUMBER_SIZE)
    fontLives = TEXT:CreateGlyphCached(LIVES_SIZE)
    colText = Config.color(Layout.text_color)
    colOutline = Config.color(Layout.outline_color)
    colLives.blue = Config.color(LIVES.blue_color)
    colLives.yellow = Config.color(LIVES.yellow_color)
    colLives.red = Config.color(LIVES.red_color)
    colLives.empty = Config.color(LIVES.empty_color)
end

local function readState()
    pcall(function()
        floor = PLAYSTATE.LastRegisteredFloor
        lives = PLAYSTATE.CurrentNumberOfLives
        maxLives = PLAYSTATE.MaxNumberOfLives
    end)
end

function activate(count)
    maxFloor = math.max(1, math.floor(tonumber(count) or 1))
    buildLayout()
    -- every glyph the HUD shows is baked here, inside the song load, not on its first draw during play
    fontTitle:Draw(strings.title, -10000, -10000, colText, colOutline, 0)
    fontLabel:Draw(strings.prefix .. strings.rest, -10000, -10000, colText, colOutline, 0)
    fontLives:Draw(strings.lives, -10000, -10000, colText, colOutline, 0)
    fontNumber:Draw("0123456789", -10000, -10000, colText, colOutline, 0)
    readState()
    flip, clock = nil, 0
    active = true
end

function deactivate()
    active = false
    flip = nil
    plank = nil
end

function update()
    if not active then return end
    clock = clock + fps.deltaTime
    local before = floor
    readState()
    if floor ~= before then
        flip = { from = before, to = floor, t = 0 }
    end
    if flip ~= nil then
        flip.t = flip.t + fps.deltaTime
        if flip.t >= FLIP_SECONDS then flip = nil end
    end
end

-- ── drawing ──────────────────────────────────────────────────────────────────────────────────────────

-- the plank's swaying pose this frame (kept in the plank, nothing allocated per frame): centre, tilt in
-- degrees, and the sine and cosine of the tilt
local function pose(p)
    local t = clock
    local ph = p.phase
    local q = p.pose
    q.angle = TILT1.deg * math.sin(t * TILT1.rate + ph * TILT1.phase_scale)
        + TILT2.deg * math.sin(t * TILT2.rate + ph * TILT2.phase_scale)
    q.x = p.cx + SWAY_X1.px * math.sin(t * SWAY_X1.rate + ph * SWAY_X1.phase_scale)
        + SWAY_X2.px * math.sin(t * SWAY_X2.rate + ph * SWAY_X2.phase_scale)
    q.y = p.cy + SWAY_Y.px * math.sin(t * SWAY_Y.rate + ph * SWAY_Y.phase_scale)
    local rad = math.rad(q.angle)
    q.s, q.c = math.sin(rad), math.cos(rad)
    return q
end

-- a point given in the plank's own frame (offsets from its centre, y down), on screen: the tilt is
-- counter-clockwise on screen like SetRotation
local function at(p, q, dx, dy)
    return q.x + dx * q.c + dy * q.s, q.y - dx * q.s + dy * q.c
end

local function drawRope(x0, y0, x1, y1)
    local dx, dy = x1 - x0, y1 - y0
    local len = math.sqrt(dx * dx + dy * dy)
    if len < 1 then return end
    local rope = tex.rope
    rope:SetScale(1, len / rope.Height)
    rope:SetRotation(math.deg(math.atan(dx, dy)))   -- the rope's length runs down its texture
    rope:DrawAtAnchor((x0 + x1) / 2, (y0 + y1) / 2, "center")
end

local function livesColor()
    local ratio = maxLives > 0 and lives / maxLives or 0
    local lastLife = lives == 1 and maxLives ~= 1
    if ratio > LIVES.blue_above and not lastLife then return colLives.blue end
    if ratio >= LIVES.yellow_from and not lastLife then return colLives.yellow end
    return colLives.red
end

local function drawBar(cx, q)
    local p = plank
    local bx, by = at(p, q, cx, p.livesY)
    tex.bar_back:SetRotation(q.angle)
    tex.bar_back:DrawAtAnchor(bx, by, "center")
    local col = livesColor()
    local n = math.max(1, maxLives)
    local segments = (n >= PLAIN_BAR_FROM) and 1 or n
    local gap = 0
    for _, g in ipairs(LIVES.segment_gaps) do
        if n <= g.up_to then gap = g.gap; break end
    end
    if segments == 1 then gap = 0 end
    local segW = (BAR_W - gap * (segments - 1)) / segments
    local seg = tex.bar_segment
    seg:SetRotation(q.angle)
    for i = 1, segments do
        local filled = segments == 1 and (lives > 0) or (i <= lives)
        local w = segW
        if segments == 1 then w = BAR_W * math.max(0, math.min(1, lives / n)) end
        if w > 0.2 then
            local left = cx - BAR_W / 2 + (i - 1) * (segW + gap)
            local sx, sy = at(p, q, left + w / 2, p.livesY)
            seg:SetScale(w / seg.Width, BAR_H / seg.Height)
            seg:SetColor(filled and col or colLives.empty)
            seg:DrawAtAnchor(sx, sy, "center")
        end
    end
    seg:SetColor(1.0, 1.0, 1.0)
end

-- the current floor on its flap: the old number folds away along the seam, the new one unfolds
local function drawFloorFlap(q)
    local p = plank
    local px, py = at(p, q, layout.flapX, p.floorY)
    local value, sy, shade = floor, 1, 1
    if flip ~= nil then
        local k = flip.t / FLIP_SECONDS
        if k < 0.5 then
            value = flip.from
            sy = math.cos(k * math.pi)
        else
            value = flip.to
            sy = -math.cos(k * math.pi)
        end
        sy = math.max(0.02, sy)
        shade = FLIP.shade_min + FLIP.shade_range * sy
    end
    local flap = tex.flap
    flap:SetScale(1, sy)
    flap:SetRotation(q.angle)
    flap:SetColor(shade, shade, shade)
    flap:DrawAtAnchor(px, py, "center")
    flap:SetColor(1.0, 1.0, 1.0)
    local col = colText
    if shade < 1 then
        local c = Layout.text_color
        col = COLOR:CreateColorFromRGBA(math.floor(c[1] * shade), math.floor(c[2] * shade), math.floor(c[3] * shade), c[4])
    end
    fontNumber:Draw(tostring(value), px, py + nudge(fontNumber) * sy, col, colOutline, 1, 1, flap.Width - Layout.floor.text_margin, "center", sy, q.angle)
end

function draw()
    if not active or plank == nil then return end
    local p = plank
    local q = pose(p)

    -- the two ropes, from the top of the screen to the holes at the plank's top corners, then the plank
    local hx, hy = p.hx, p.hy
    local lx, ly = at(p, q, -hx, -hy)
    local rx, ry = at(p, q, hx, -hy)
    drawRope(p.cx - hx, ROPE_TOP, lx, ly)
    drawRope(p.cx + hx, ROPE_TOP, rx, ry)
    p.tex:SetRotation(q.angle)
    p.tex:DrawAtAnchor(q.x, q.y, "center")
    tex.knot:DrawAtAnchor(lx, ly, "center")
    tex.knot:DrawAtAnchor(rx, ry, "center")

    -- the title
    local tx, ty = at(p, q, 0, p.titleY)
    fontTitle:Draw(strings.title, tx, ty + nudge(fontTitle), colText, colOutline, 1, 1, p.w - Layout.title.text_margin, "center", 0, q.angle)
    -- prefix, the flap, "/k" and the suffix
    local fx, fy = at(p, q, layout.prefixX, p.floorY)
    fontLabel:Draw(strings.prefix, fx, fy + nudge(fontLabel), colText, colOutline, 1, 1, 0, "center", 0, q.angle)
    drawFloorFlap(q)
    fx, fy = at(p, q, layout.restX, p.floorY)
    fontLabel:Draw(strings.rest, fx, fy + nudge(fontLabel), colText, colOutline, 1, 1, 0, "center", 0, q.angle)
    -- the label and the bar
    local bx, by = at(p, q, layout.livesX, p.livesY)
    fontLives:Draw(strings.lives, bx, by + nudge(fontLives), colText, colOutline, 1, 1, 0, "center", 0, q.angle)
    drawBar(layout.barX, q)
end

function afterSongEnum() end

function onDestroy()
    deactivate()
    for _, t in pairs(tex) do pcall(function() t:Dispose() end) end
    tex = {}
    for _, f in ipairs({ fontTitle, fontLabel, fontNumber, fontLives }) do if f ~= nil then pcall(function() f:Dispose() end) end end
    fontTitle, fontLabel, fontNumber, fontLives = nil, nil, nil, nil
end
