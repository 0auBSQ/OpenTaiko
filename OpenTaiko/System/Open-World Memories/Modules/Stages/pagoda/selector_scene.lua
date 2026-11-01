-- Landscape and tower scene of the floor selector and the title screen.

local tiles    = require("tower_tiles")
local Darkness = require("fx/darkness")
local levels   = require("levels")
local skybox   = require("skybox")
local towerfx  = require("tower_fx")
local Config   = require("Core.Config")
local Layout   = Config.load("Config/layout.json")

local M = {}

-- layout
local Scene = Layout.stage.scene
M.NORMAL_W, M.NORMAL_H = Scene.normal.w, Scene.normal.h      -- normal.png; every level occupies one NORMAL_H slot
local NORMAL_W, NORMAL_H = M.NORMAL_W, M.NORMAL_H
local ROOF_W, ROOF_H = Scene.roof.w, Scene.roof.h            -- roof.png (gate 10 = level 20)

-- world-space vertical centre of a level's slot (y grows downward; higher level = higher up)
function M.floor_center(level)
    return -NORMAL_H * (level - 10) + NORMAL_H * 0.5
end

-- background layers
-- Layers draw at native pixel size; `rate` only scales scroll speed. Forest rows go `tile` apart and
-- draw bottom-up, so each row hides the top band of the row below. Mountains draw far to near;
-- hTotal = strip height, wyBottom = its bottom edge, kept behind the next nearer layer's solid rows.
local BG_FOREST = { key = "bg_near", rate = Scene.forest.rate, wyTop = Scene.forest.wy_top, rows = Scene.forest.rows,
                    tile = Scene.forest.tile }
-- forest lift (px up) per camera level around the summit (level 9), sampled every `step` levels from
-- `from`, interpolated, zero outside; it keeps the treeline over summit.png's open bottom-left corner
-- (else the sky shows there between levels ~7.2 and ~8.2)
local SUMMIT_LIFT = Scene.summit_lift
local BG_FRINGE = { key = "bg_near_top", overlap = Scene.fringe.overlap }
local BG_MTNS = {}
for i, key in ipairs({ "bg_far1", "bg_far2", "bg_far3" }) do
    local m = Scene.mountains[i]
    BG_MTNS[i] = { key = key, rate = m.rate, wyBottom = m.wy_bottom, hTotal = m.h_total }
end

-- corruption effects (doors above the highest reached level)
local Dark = Scene.darkness
local FLOOR_DARKEN   = Dark.floor_tint           -- tint on an undiscovered floor's art
-- tint from two floors above the highest cleared one (the next floor up keeps FLOOR_DARKEN)
local FLOOR_DARKEN_DEEP = Dark.floor_tint_deep
local CHECKPOINT_DEEP   = Dark.checkpoint_deep   -- extra dim of the checkpoint art when deep
local DARK_CROP_LEFT = Dark.crop_left            -- skip the eave; effects cover the trunk only
local DARK_SPLIT_X   = Dark.split_x              -- dispersing motes blow left or right of this x

-- a door floor's normal.png takes its dan colour moved this far toward white
local FLOOR_TINT_LIGHTEN = Scene.floor_tint_lighten

-- state
local _tex = nil
local _dark = nil                       -- fx/darkness instance for the door banks
local _boxes = {}                       -- darkness boxes, rebuilt every draw
local _cam, _highest = 0, 0             -- refreshed on every draw() call
local _scLevel, _scT = nil, 0           -- floor whose darkness is dispersing, and its 0..1 progress
local _tints = {}                       -- level -> { r, g, b } of its normal.png, built on first use
local _clock = 0                        -- seconds of scene drawn, for the section effects
local _fr, _fg, _fb = 1, 1, 1           -- the sky's time-of-day filter this frame

function M.setup(tex)
    if _dark == nil or _tex ~= tex then _dark = Darkness.new(tex, { splitX = DARK_SPLIT_X }) end
    _tex = tex
    towerfx.setup()
end

function M.dispose()
    towerfx.dispose()
end

function M.reset()
    if _dark ~= nil then _dark:reset() end
    _scLevel, _scT = nil, 0
end

-- disperses a floor's darkness as t goes 0 -> 1; level = nil clears
function M.setScatter(level, t)
    _scLevel, _scT = level, t or 0
end

-- draw helpers
local function screen_y(world_y) return world_y - _cam end

-- right-aligned floor image at a world-Y top, on a whole pixel so abutting slots never seam;
-- dark: 0..1 or a boolean (a fraction lets the tint lift with the scatter); r, g, b: optional art colour;
-- deep: 0..1, optional, toward FLOOR_DARKEN_DEEP
local function draw_floor_img(tx, w, h, world_top, dark, r, g, b, deep)
    if tx == nil or not tx.Ready then return end
    local res = THEME:GetResolution()
    local y = math.floor(screen_y(world_top) + 0.5)
    if y > res.Y or y + h < 0 then return end
    local da = (dark == true) and 1 or (type(dark) == "number" and dark or 0)
    r, g, b = r or 1, g or 1, b or 1
    local tinted = da > 0 or r ~= 1 or g ~= 1 or b ~= 1
    if tinted then
        local k = deep or 0
        local d1 = FLOOR_DARKEN[1] + (FLOOR_DARKEN_DEEP[1] - FLOOR_DARKEN[1]) * k
        local d2 = FLOOR_DARKEN[2] + (FLOOR_DARKEN_DEEP[2] - FLOOR_DARKEN[2]) * k
        local d3 = FLOOR_DARKEN[3] + (FLOOR_DARKEN_DEEP[3] - FLOOR_DARKEN[3]) * k
        tx:SetColor(r * (1 + (d1 - 1) * da), g * (1 + (d2 - 1) * da), b * (1 + (d3 - 1) * da))
    end
    tx:Draw(res.X - w, y)
    if tinted then tx:SetColor(1, 1, 1) end
end

-- 0..1; the dispersing floor brightens with _scT
local function floor_darkness(L)
    if L == _scLevel then return math.max(0, 1 - _scT) end
    return (L > _highest) and 1 or 0
end

-- 0..1: full from two floors above the highest cleared one; eases with a dispersal below it
local function floor_deep(L)
    if _scLevel ~= nil and L == _scLevel + 1 then return math.max(0, 1 - _scT) end
    return (L >= _highest + 2) and 1 or 0
end

-- the multiplier a door floor's normal.png is drawn with (its dan colour toward white)
local function floor_tint(L)
    local t = _tints[L]
    if t == nil then
        local c = levels.level_tick_color(L)
        t = {}
        for i = 1, 3 do
            local v = c[i] / 255
            t[i] = v + (1 - v) * FLOOR_TINT_LIGHTEN
        end
        _tints[L] = t
    end
    return t[1], t[2], t[3]
end

-- level 15 always draws the checkpoint art
local function shows_checkpoint(L)
    return L == 15 and _tex.checkpoint_cleared ~= nil
end

-- what the section effects read each frame (see tower_fx.lua)
local _ctx = { t = 0, resX = 1920, resY = 1080, hour = 12, fr = 1, fg = 1, fb = 1, roofLit = 0 }
function _ctx.top(L) return -NORMAL_H * (L - 10) - _cam end
function _ctx.lit(L) return 1 - floor_darkness(L) end
_ctx.tint = floor_tint
_ctx.checkpoint = shows_checkpoint

local function summit_lift(focus)
    local s, px = SUMMIT_LIFT, SUMMIT_LIFT.px
    local u = (10 - (_cam + focus - NORMAL_H * 0.5) / NORMAL_H - s.from) / s.step
    if u <= 0 or u >= #px - 1 then return 0 end
    local i = math.floor(u)
    local t = u - i
    local p0, p1, p2, p3 = px[math.max(i, 1)], px[i + 1], px[i + 2], px[math.min(i + 3, #px)]
    return 0.5 * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
        + (3 * p1 - p0 - 3 * p2 + p3) * t * t * t)                -- Catmull-Rom through the samples
end

local function draw_parallax()
    local res   = THEME:GetResolution()
    local focus = res.Y * 0.5
    for _, mt in ipairs(BG_MTNS) do
        local m = _tex[mt.key]
        if m ~= nil and m.Loaded then
            local mb  = (mt.wyBottom - _cam - focus) * mt.rate + focus
            local top = math.floor(mb + 0.5) - mt.hTotal
            if top < res.Y and top + mt.hTotal > 0 then
                local x = 0
                while x < res.X do m:DrawAtAnchor(x, top, "topleft") ; x = x + m.Width end
            end
        end
    end
    local f = _tex[BG_FOREST.key]
    if f ~= nil and f.Loaded then
        -- convert the origin once and round once; tiles go at exact integer offsets from it
        local base = (BG_FOREST.wyTop - _cam - focus) * BG_FOREST.rate + focus - summit_lift(focus)
        base = math.floor(base + 0.5)
        local fr = _tex[BG_FRINGE.key]
        if fr ~= nil and fr.Loaded then
            local fy = base - (fr.Height - BG_FRINGE.overlap)
            if fy < res.Y and fy + fr.Height > 0 then
                local x = 0
                while x < res.X do fr:DrawAtAnchor(x, fy, "topleft") ; x = x + fr.Width end
            end
        end
        for row = BG_FOREST.rows - 1, 0, -1 do
            local y = base + row * BG_FOREST.tile
            if y < res.Y and y + f.Height > 0 then
                local x = 0
                while x < res.X do f:DrawAtAnchor(x, y, "topleft") ; x = x + f.Width end
            end
        end
    end
end

-- tower art
-- slot 0 = dojo, 1-9 = forest path and stairs, 10 = pagoda base, 11-19 = doors,
-- 20 = gate 10 with the crown, 21+ = beanstalk once gate 10 is beaten
local function draw_level_art(L)
    local wtop = -NORMAL_H * (L - 10)
    if L == 0 then
        local dj = _tex.dojo
        if dj ~= nil and dj.Loaded then draw_floor_img(dj, dj.Width, dj.Height, wtop, false) end
    elseif L >= 1 and L <= 10 then
        local tx = tiles.forLevel(L)
        if tx ~= nil then draw_floor_img(tx, tx.Width, tx.Height, wtop, false) end
    elseif L >= 11 and L <= 20 then
        if L == 20 then
            -- the roof stalk takes the sky's filter like the beanstalk
            local stalk = _highest >= 20 and _tex.roof_beanstalk ~= nil and _tex.roof_beanstalk.Loaded
            if stalk then
                draw_floor_img(_tex.roof_beanstalk, ROOF_W, ROOF_H, wtop, floor_darkness(L), _fr, _fg, _fb, floor_deep(L))
            else
                draw_floor_img(_tex.roof, ROOF_W, ROOF_H, wtop, floor_darkness(L), nil, nil, nil, floor_deep(L))
            end
            -- gate 10 beaten: the roof stalk follows the dispersal
            if _scLevel == 20 and _tex.roof_beanstalk ~= nil and _tex.roof_beanstalk.Loaded then
                _tex.roof_beanstalk:SetOpacity(_scT)
                draw_floor_img(_tex.roof_beanstalk, ROOF_W, ROOF_H, wtop, floor_darkness(L), _fr, _fg, _fb)
                _tex.roof_beanstalk:SetOpacity(1)
            end
        elseif shows_checkpoint(L) then
            local c = 1 - floor_darkness(L)
            c = c * (1 - (1 - CHECKPOINT_DEEP) * floor_deep(L))
            draw_floor_img(_tex.checkpoint_cleared, NORMAL_W, NORMAL_H, wtop, false, c, c, c)
        else
            local r, g, b = floor_tint(L)
            draw_floor_img(_tex.normal, NORMAL_W, NORMAL_H, wtop, floor_darkness(L), r, g, b, floor_deep(L))
        end
    else
        if _highest >= 20 then
            draw_floor_img(_tex.beanstalk, ROOF_W, NORMAL_H, wtop, false, _fr, _fg, _fb)
        elseif _scLevel == 20 and _tex.beanstalk ~= nil and _tex.beanstalk.Loaded then
            _tex.beanstalk:SetOpacity(_scT)
            draw_floor_img(_tex.beanstalk, ROOF_W, NORMAL_H, wtop, false, _fr, _fg, _fb)
            _tex.beanstalk:SetOpacity(1)
        end
    end
end

-- corruption clouds and particles
-- effect box for an undiscovered door, in world space (x is screen space: no horizontal scroll)
local function floor_world_box(L)
    local res = THEME:GetResolution()
    local c = DARK_CROP_LEFT
    if L >= 11 and L <= 20 then
        return res.X - NORMAL_W + c, -NORMAL_H * (L - 10), NORMAL_W - c, NORMAL_H
    end
    return nil
end

-- doors above the highest reached level within a slot of the screen, plus the dispersing floor;
-- box tables are reused
local function dark_boxes()
    local res = THEME:GetResolution()
    local mg = NORMAL_H                     -- one slot beyond the screen to avoid pop-in
    local n = 0
    for L = 11, 20 do
        local x, wy, w, h = floor_world_box(L)
        local scatter = (L == _scLevel)
        local sy = screen_y(wy)
        if scatter or (L > _highest and sy < res.Y + mg and sy + h > -mg) then
            n = n + 1
            local b = _boxes[n]
            if b == nil then b = {} ; _boxes[n] = b end
            b[1], b[2], b[3], b[4] = x, wy, w, h
            b.scatter = scatter and _scT or nil
            b.deep = floor_deep(L)
        end
    end
    for i = #_boxes, n + 1, -1 do _boxes[i] = nil end
    return _boxes
end

-- entry point, called each frame
function M.draw(cam, highest)
    _cam = cam
    if highest ~= nil then _highest = highest end
    local res = THEME:GetResolution()
    _clock = _clock + fps.deltaTime
    local hour = skybox.hour()
    _fr, _fg, _fb = skybox.filterFor(hour)
    _ctx.t, _ctx.resX, _ctx.resY, _ctx.hour = _clock, res.X, res.Y, hour
    _ctx.fr, _ctx.fg, _ctx.fb = _fr, _fg, _fb
    _ctx.roofLit = (_highest >= 20) and 1 or ((_scLevel == 20) and _scT or 0)

    draw_parallax()
    local Lmin = math.max(0, math.floor(10 - (_cam + res.Y + NORMAL_H) / NORMAL_H) - 1)
    local Lmax =             math.floor(10 - (_cam - NORMAL_H) / NORMAL_H) + 1
    towerfx.draw("back", Lmin, Lmax, _ctx)
    for L = Lmin, Lmax do draw_level_art(L) end
    towerfx.draw("front", Lmin, Lmax, _ctx)
    if _dark ~= nil then _dark:frame(fps.deltaTime, dark_boxes(), _cam) end
end

return M
