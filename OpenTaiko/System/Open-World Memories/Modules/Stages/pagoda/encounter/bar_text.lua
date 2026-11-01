-- Song-bar lettering, laid out in local px (y down) and drawn through a transform xf.

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Look = Layout.encounter.bar_text

local M = {}

local NUDGE = Look.text_nudge_px      -- a glyph box's centre sits this far under its ink centre
local ROT_MIN, ROT_MAX = Look.rot_min_deg, Look.rot_max_deg
local FLIP = Look.same_turn_chance    -- chance of turning the same way as the previous character
local SIZE_MIN, SIZE_MAX = Look.size_min, Look.size_max
local FIRST = Look.first_size         -- size of the first character
local LIFT = Look.lift_px             -- max px up or down
local SQUISH_MIN = Look.squish_min
local POP_GROW = Animation.encounter.bar_text.pop_grow   -- start size factor of a popping character

local RAD = math.pi / 180
local cos, sin = math.cos, math.sin

-- seeded by the text, so a text always gets the same turns
local function seededRandom(text, seed)
    local h = seed or 0
    for i = 1, #text do h = (h * 31 + text:byte(i)) % 2147483647 end
    if h == 0 then h = 1 end
    return function()
        h = (h * 48271) % 2147483647
        return h / 2147483647
    end
end

-- screen point of local (lx, ly) through xf = { x, y, s, rot (degrees, counter-clockwise) }
function M.point(xf, lx, ly)
    local a = xf.rot * RAD
    local c, s = cos(a), sin(a)
    local k = xf.s
    return xf.x + k * (lx * c + ly * s), xf.y + k * (-lx * s + ly * c)
end

-- (vx, vy) turned deg degrees counter-clockwise on a y-down screen
local function turn(vx, vy, deg)
    local a = deg * RAD
    local c, s = cos(a), sin(a)
    return vx * c + vy * s, -vx * s + vy * c
end

-- text laid out from local x0 on y = 0 (centred on x0 when centre), squished to width at most;
-- size scales the font, jitter the turns, sizes and lifts (1 = full)
function M.layout(font, text, x0, width, size, jitter, seed, centre)
    local lay = { chars = {}, x0 = x0, w = 0 }
    if font == nil or text == nil or text == "" then return lay end
    size, jitter = size or 1, jitter or 1
    local r = seededRandom(text, seed)
    local list, total = {}, 0
    local okIter = pcall(function()
        for _, code in utf8.codes(text) do list[#list + 1] = utf8.char(code) end
    end)
    if not okIter then
        list = {}
        for i = 1, #text do list[#list + 1] = text:sub(i, i) end
    end
    local side = 1
    for i, ch in ipairs(list) do
        local k = SIZE_MIN + (SIZE_MAX - SIZE_MIN) * r()
        if i == 1 then k = FIRST end
        k = 1 + (k - 1) * jitter
        if r() >= FLIP then side = -side end
        local c = {
            ch = ch, blank = ch:match("^%s$") ~= nil,
            adv = font:Measure(ch) * size * k,
            s = size * k,
            rot = side * (ROT_MIN + (ROT_MAX - ROT_MIN) * r()) * jitter,
            y = (r() * 2 - 1) * LIFT * jitter,
        }
        total = total + c.adv
        lay.chars[i] = c
    end
    local sq = 1
    if total > width and total > 0 then sq = math.max(SQUISH_MIN, width / total) end
    local pen = x0
    for _, c in ipairs(lay.chars) do
        local a = c.adv * sq
        c.x, c.sx = pen + a * 0.5, sq
        pen = pen + a
    end
    lay.w = pen - x0
    if centre then
        for _, c in ipairs(lay.chars) do c.x = c.x - lay.w * 0.5 end
        lay.x0 = x0 - lay.w * 0.5
    end
    return lay
end

-- fore = ink colour, back = outline colour, (dx, dy) = local offset
local function pass(font, lay, xf, fore, back, dx, dy, alpha, pops)
    for i, c in ipairs(lay.chars) do
        if not c.blank then
            local p = 1
            if pops ~= nil then p = pops(i) end
            if p > 0 then
                local grow = 1 + POP_GROW * (1 - p)
                local sy = xf.s * c.s * grow
                local sx = sy * c.sx
                local rot = xf.rot + c.rot * p
                local x, y = M.point(xf, c.x + dx, c.y + dy)
                local nx, ny = turn(0, NUDGE * sy, rot)
                font:Draw(c.ch, x + nx, y + ny, fore, back, alpha * math.min(1, p * 2), sx, 0, "center", sy, rot)
            end
        end
    end
end

-- style = { face, rim, ext (colours; no ext: no extrusion), dx, dy (local offset of the extrusion) };
-- alpha 0..1; pops(i) -> 0..1, optional
function M.draw(font, lay, xf, style, alpha, pops)
    if font == nil or lay == nil or #lay.chars == 0 or alpha <= 0 then return end
    if style.ext ~= nil then pass(font, lay, xf, style.ext, style.ext, style.dx, style.dy, alpha, pops) end
    pass(font, lay, xf, style.face, style.rim, 0, 0, alpha, pops)
end

return M
