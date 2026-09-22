---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- tower_hud ROActivity
-- ------------------------------------------------------------------------------------------------------
-- The tower climb's head-up display, in the band between the top of the screen and the lane: three wooden
-- planks hanging from one another on ropes, the whole chain swaying a little.
--   plank 1   the "Reached floor" title
--   plank 2   Floor n/k, the current floor on a small split-flap panel that flips when a floor is reached
--   plank 3   Lives, a bar of one segment per life (a plain bar from a hundred lives up), blue while the
--             lives are above half, yellow above a fifth, red below (and on the last life)
-- The art is in Textures/ (the three planks, the flap, a rope tile stretched to length, the knots, the bar
-- back and a white segment tinted per life); the texts come from the skin locale and centre on their plank.
-- Driven by the gameplay screen: activate(maxFloor), update() and draw() every frame, deactivate(). The
-- floor and the lives are read from PLAYSTATE.

local TITLE_SIZE, LABEL_SIZE, NUMBER_SIZE, LIVES_SIZE = 26, 30, 42, 24
local CENTER_X = 960
local CHAIN_TOP = -6           -- where the top ropes start, just above the screen
local ROPE_GAP = 18            -- rope length between two planks
local HOLE_INSET_X, HOLE_INSET_Y = 24, 11   -- the rope holes, from the plank's ends and edges
local FLIP_SECONDS = 0.45
local BAR_W, BAR_H = 220, 22
local PLAIN_BAR_FROM = 100     -- lives from which the bar has no segments

local fontTitle, fontLabel, fontNumber, fontLives
local colText, colOutline
local colLives = {}

local tex = {}                 -- the textures by file name
local planks = {}              -- per plank: texture, w, h, cx, cy (rest position), phase, holes, pose
local strings = {}             -- the localized texts
local layout = {}              -- the centres of plank 2's and plank 3's pieces, as offsets from the plank's centre
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
local function nudge(gf) return math.floor((gf.BoxHeight - math.ceil(gf.LineHeight)) / 2) - 1 end

-- ── layout ───────────────────────────────────────────────────────────────────────────────────────────

local function buildLayout()
    strings.title = tr("TOWER_HUD_REACHED", "Reached floor")
    strings.prefix = tr("TOWER_HUD_FLOOR_PREFIX", "Floor ")
    strings.suffix = tr("TOWER_HUD_FLOOR_SUFFIX", "")
    strings.lives = tr("TOWER_HUD_LIVES", "Lives")
    strings.rest = "/" .. tostring(maxFloor) .. strings.suffix

    -- plank 2 holds "<prefix>[ n ]/k<suffix>", centred as a group
    local prefixW = fontLabel:Measure(strings.prefix)
    local flapW = tex.flap.Width
    local restW = fontLabel:Measure(strings.rest)
    local left = -(prefixW + flapW + 6 + restW) / 2
    layout.prefixX = left + prefixW / 2
    layout.flapX = left + prefixW + flapW / 2
    layout.restX = left + prefixW + flapW + 6 + restW / 2

    -- plank 3 holds the label then the bar
    local livesW = fontLives:Measure(strings.lives)
    local contentW = livesW + 16 + BAR_W
    layout.livesX = -contentW / 2 + livesW / 2
    layout.barX = -contentW / 2 + livesW + 16 + BAR_W / 2

    local y = CHAIN_TOP + ROPE_GAP + 10
    for i, t in ipairs({ tex.plank_title, tex.plank_floor, tex.plank_lives }) do
        local w, h = t.Width, t.Height
        planks[i] = { tex = t, w = w, h = h, cx = CENTER_X, cy = y + h / 2, phase = i * 1.9,
                      hx = w / 2 - HOLE_INSET_X, hy = h / 2 - HOLE_INSET_Y,
                      pose = { x = CENTER_X, y = y + h / 2, angle = 0, s = 0, c = 1 } }
        y = y + h + ROPE_GAP
    end
end

-- ── lifecycle ────────────────────────────────────────────────────────────────────────────────────────

function onStart()
    for _, name in ipairs({ "plank_title", "plank_floor", "plank_lives", "flap", "rope", "knot", "bar_back", "bar_segment" }) do
        tex[name] = TEXTURE:CreateTexture("Textures/" .. name .. ".png")
    end
    fontTitle = TEXT:CreateGlyphCached(TITLE_SIZE)
    fontLabel = TEXT:CreateGlyphCached(LABEL_SIZE)
    fontNumber = TEXT:CreateGlyphCached(NUMBER_SIZE)
    fontLives = TEXT:CreateGlyphCached(LIVES_SIZE)
    colText = COLOR:CreateColorFromRGBA(250, 236, 200, 255)
    colOutline = COLOR:CreateColorFromRGBA(58, 34, 18, 255)
    colLives.blue = COLOR:CreateColorFromRGBA(72, 144, 255, 255)
    colLives.yellow = COLOR:CreateColorFromRGBA(255, 208, 64, 255)
    colLives.red = COLOR:CreateColorFromRGBA(236, 64, 56, 255)
    colLives.empty = COLOR:CreateColorFromRGBA(66, 46, 30, 255)
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
    readState()
    flip, clock = nil, 0
    active = true
end

function deactivate()
    active = false
    flip = nil
    planks = {}
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
    q.angle = 1.4 * math.sin(t * 0.9 + ph) + 0.5 * math.sin(t * 2.3 + ph * 0.7)
    q.x = p.cx + 4 * math.sin(t * 0.7 + ph) + 1.5 * math.sin(t * 1.9 + ph)
    q.y = p.cy + 3 * math.sin(t * 1.1 + ph * 1.3)
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
    if ratio > 0.5 and not lastLife then return colLives.blue end
    if ratio >= 0.2 and not lastLife then return colLives.yellow end
    return colLives.red
end

local function drawBar(cx, q)
    local p = planks[3]
    local bx, by = at(p, q, cx, 0)
    tex.bar_back:SetRotation(q.angle)
    tex.bar_back:DrawAtAnchor(bx, by, "center")
    local col = livesColor()
    local n = math.max(1, maxLives)
    local segments = (n >= PLAIN_BAR_FROM) and 1 or n
    local gap = (n <= 12 and 3) or (n <= 30 and 2) or (n <= 60 and 1) or 0
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
            local sx, sy = at(p, q, left + w / 2, 0)
            seg:SetScale(w / seg.Width, BAR_H / seg.Height)
            seg:SetColor(filled and col or colLives.empty)
            seg:DrawAtAnchor(sx, sy, "center")
        end
    end
    seg:SetColor(1.0, 1.0, 1.0)
end

-- the current floor on its flap: the old number folds away along the seam, the new one unfolds
local function drawFloorFlap(q)
    local p = planks[2]
    local px, py = at(p, q, layout.flapX, 0)
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
        shade = 0.55 + 0.45 * sy
    end
    local flap = tex.flap
    flap:SetScale(1, sy)
    flap:SetRotation(q.angle)
    flap:SetColor(shade, shade, shade)
    flap:DrawAtAnchor(px, py, "center")
    flap:SetColor(1.0, 1.0, 1.0)
    local col = colText
    if shade < 1 then col = COLOR:CreateColorFromRGBA(math.floor(250 * shade), math.floor(236 * shade), math.floor(200 * shade), 255) end
    fontNumber:Draw(tostring(value), px, py + nudge(fontNumber) * sy, col, colOutline, 1, 1, flap.Width - 10, "center", sy, q.angle)
end

local poses = {}

function draw()
    if not active or #planks < 3 then return end
    for i = 1, 3 do poses[i] = pose(planks[i]) end

    -- ropes first: from the top of the screen to plank 1, then from each plank's bottom holes to the next
    local elx, ely, erx, ery = 0, 0, 0, 0
    for i = 1, 3 do
        local p, q = planks[i], poses[i]
        local hx, hy = p.hx, p.hy
        local lx, ly = at(p, q, -hx, -hy)
        local rx, ry = at(p, q, hx, -hy)
        if i == 1 then
            drawRope(p.cx - hx, CHAIN_TOP, lx, ly)
            drawRope(p.cx + hx, CHAIN_TOP, rx, ry)
        else
            drawRope(elx, ely, lx, ly)
            drawRope(erx, ery, rx, ry)
        end
        elx, ely = at(p, q, -hx, hy)
        erx, ery = at(p, q, hx, hy)
    end

    local knot = tex.knot
    for i = 1, 3 do
        local p, q = planks[i], poses[i]
        p.tex:SetRotation(q.angle)
        p.tex:DrawAtAnchor(q.x, q.y, "center")
        local hx, hy = p.hx, p.hy
        local kx, ky = at(p, q, -hx, -hy)
        knot:DrawAtAnchor(kx, ky, "center")
        kx, ky = at(p, q, hx, -hy)
        knot:DrawAtAnchor(kx, ky, "center")
        if i < 3 then
            kx, ky = at(p, q, -hx, hy)
            knot:DrawAtAnchor(kx, ky, "center")
            kx, ky = at(p, q, hx, hy)
            knot:DrawAtAnchor(kx, ky, "center")
        end
    end

    -- plank 1: the title
    do
        local p, q = planks[1], poses[1]
        fontTitle:Draw(strings.title, q.x, q.y + nudge(fontTitle), colText, colOutline, 1, 1, p.w - 40, "center", 0, q.angle)
    end
    -- plank 2: prefix, the flap, "/k" and the suffix
    do
        local p, q = planks[2], poses[2]
        local lx, ly = at(p, q, layout.prefixX, 0)
        fontLabel:Draw(strings.prefix, lx, ly + nudge(fontLabel), colText, colOutline, 1, 1, 0, "center", 0, q.angle)
        drawFloorFlap(q)
        local rx, ry = at(p, q, layout.restX, 0)
        fontLabel:Draw(strings.rest, rx, ry + nudge(fontLabel), colText, colOutline, 1, 1, 0, "center", 0, q.angle)
    end
    -- plank 3: the label and the bar
    do
        local p, q = planks[3], poses[3]
        local lx, ly = at(p, q, layout.livesX, 0)
        fontLives:Draw(strings.lives, lx, ly + nudge(fontLives), colText, colOutline, 1, 1, 0, "center", 0, q.angle)
        drawBar(layout.barX, q)
    end
end

function afterSongEnum() end

function onDestroy()
    deactivate()
    for _, t in pairs(tex) do pcall(function() t:Dispose() end) end
    tex = {}
    for _, f in ipairs({ fontTitle, fontLabel, fontNumber, fontLives }) do if f ~= nil then pcall(function() f:Dispose() end) end end
    fontTitle, fontLabel, fontNumber, fontLives = nil, nil, nil, nil
end
