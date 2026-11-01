-- Floor-selection screen for Practice (any beaten level) and Challenge (a checkpoint).
-- UI offsets are px from the selected floor's normal.png top-left (source: plan1.xcf).

local scene   = require("selector_scene")
local confirm = require("selector_confirm")
local skybox  = require("skybox")
local plaque  = require("selectui/plaque")
local numbers = require("selectui/numbers")
local hover   = require("selectui/hover")
local carets  = require("selectui/carets")
local difficulty = require("selectui/difficulty")
local Config  = require("Core.Config")
local Layout  = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

-- tunables
local NORMAL_W, NORMAL_H = scene.NORMAL_W, scene.NORMAL_H

local SCROLL_SPEED  = Animation.stage.selector.scroll_rate     -- exp-smoothing rate of the tower scroll

-- sky pitch follows the camera; rest pose: the base floor at its title height
local BASE_PITCH     = Layout.stage.sky.pitch_deg
local BASE_BOTTOM_WY = Layout.stage.selector.base_bottom_wy         -- world-Y of the pagoda base bottom (pitch rest point)
local SKY_PITCH_RATE = Layout.stage.selector.sky_pitch_deg_per_px

-- state
local _world, _tex, _fonts = nil, nil, nil
local _color_fn = nil           -- level -> {r,g,b} dan-plate colour (from pagoda.lua)
local _highest  = 0

local _items   = {}             -- top to bottom: { {kind="level", level=N} ..., {kind="exit"} }
local _sel     = 1              -- index into _items
local _mode    = "practice"
local _lowest  = 1              -- lowest selectable level (the Exit entry shares its position)
local _cam     = 0              -- animated world-Y at the focus
local _bob_t   = 0
local _plaque_level = nil       -- last level drawn through drawPlaque

M.active = false

local function _sk(key, fallback)
    local s = THEME:GetSkinString(key)
    if s == nil or s == "" then return fallback end
    return s
end

-- sky hour for a roof tier
function M.roofSkyHour(level)
    return skybox.hourFor(level)
end

-- camera
local function focus_y() return THEME:GetResolution().Y * 0.5 end

-- Exit shares the lowest level's spot so selecting it never scrolls the tower
local function item_level(item)
    if item.kind == "exit" then return _lowest end
    return item.level
end

local function cam_target(item)
    return scene.floor_center(item_level(item)) - focus_y()
end

-- pitch parallax; skybox only pushes hour and pitch when they change; a nil hour keeps the current one
local function apply_sky(hour, cam)
    if _world == nil then return end
    local rest = BASE_BOTTOM_WY - THEME:GetResolution().Y
    skybox.setView(hour, BASE_PITCH + (rest - (cam or _cam)) * SKY_PITCH_RATE)
end

-- for other screens drawing the tower scene (floor transition)
function M.applySky(hour, cam) apply_sky(hour, cam) end

-- session control
function M.setup(world, tex, fonts)
    _world, _tex, _fonts = world, tex, fonts
    scene.setup(tex)
    confirm.setup(tex, fonts)
    plaque.setup(fonts)
    numbers.setup()
    hover.setup()
    carets.setup()
    difficulty.setup(fonts.name)
    _cam = scene.floor_center(10) - focus_y()   -- start at the title pose
end

function M.dispose()
    scene.dispose()
    plaque.dispose()
    numbers.dispose()
    hover.dispose()
    carets.dispose()
    difficulty.dispose()
    confirm.dispose()
end

local function selected_level()
    local it = _items[_sel]
    return (it and it.kind == "level") and it.level or nil
end

-- current cursor level (nil on Exit); used to reopen practice on the same floor
function M.hovered_level() return selected_level() end

-- called on every selection change
local function on_select()
    hover.select(selected_level())
    plaque.pop()
end

-- mode: "practice" | "checkpoint";  levels: ascending;  initial: level to select.
-- info = { name = fn(level)->string, color = fn(level)->{r,g,b}, highest = N }
function M.open(mode, levels, initial, info)
    _mode     = mode
    _color_fn = info and info.color
    _highest  = (info and info.highest) or 0
    _lowest   = levels[1] or 1
    scene.reset()
    confirm.reset()
    _items = {}
    for i = #levels, 1, -1 do _items[#_items + 1] = { kind = "level", level = levels[i] } end
    _items[#_items + 1] = { kind = "exit" }
    _sel = #_items - 1
    for i, it in ipairs(_items) do
        if it.kind == "level" and it.level == initial then _sel = i ; break end
    end
    -- _cam is not snapped: it eases from the title pose / previous selection
    _bob_t     = 0
    M.active   = true
    on_select()
end

-- toTitle: the sky keeps its hour; the title moves it home as the tower scrolls down
function M.close(toTitle)
    M.active   = false
    confirm.reset()
    if _world ~= nil then skybox.reset(toTitle) end
end

-- update
-- Returns nil | { sel = level } | "exit"
function M.update(dt, nav)
    _bob_t = _bob_t + dt
    plaque.update(dt)
    hover.update(dt)

    local result = nil
    local prev = _sel
    if confirm.active() then
        result = confirm.update(dt, nav)
    else
        if nav.upOrPadLeft() then
            if _sel > 1 then _sel = _sel - 1 ; SHARED:GetSharedSound("Move"):Play() end
        elseif nav.downOrPadRight() then
            if _sel < #_items then _sel = _sel + 1 ; SHARED:GetSharedSound("Move"):Play() end
        end

        if nav.cancel() then
            SHARED:GetSharedSound("Cancel"):Play()
            result = "exit"
        elseif nav.decide() then
            local it = _items[_sel]
            if it.kind == "exit" then
                SHARED:GetSharedSound("Cancel"):Play()
                result = "exit"
            else
                -- both modes confirm with the dialog, which returns the selection; a challenge then goes to
                -- the story, practice to song_loading
                confirm.enter(it.level, _mode == "checkpoint")
                SHARED:GetSharedSound("Decide"):Play()
            end
        end
    end
    if _sel ~= prev then on_select() end

    -- the tower keeps easing toward the selection, also behind the confirm overlay
    local target = cam_target(_items[_sel])
    _cam = _cam + (target - _cam) * (1 - math.exp(-SCROLL_SPEED * dt))

    -- roofs at their own hour, everything else (and Exit) at noon
    apply_sky(skybox.glideHour(skybox.hourFor(selected_level()), dt))
    return result
end

-- selection UI
-- the selected item always scrolls to the centre, so the UI reference point is fixed there
local function ref_topleft()
    local res = THEME:GetResolution()
    return res.X - NORMAL_W, res.Y * 0.5 - NORMAL_H * 0.5
end

-- the level's estimated difficulty
local function draw_difficulty(level, rx, ry)
    local off = Layout.selectui.difficulty.offset
    difficulty.draw(level, rx + off[1], ry + off[2])
end

-- number tint: the level's dan-plate colour; roofs stay white
local function num_tint(level)
    if level >= 21 then return 1, 1, 1 end
    if _color_fn ~= nil then
        local c = _color_fn(level)
        if c ~= nil then return c[1] / 255, c[2] / 255, c[3] / 255 end
    end
    return 1, 1, 1
end

-- draw

-- floor plaque at the fixed screen-centre reference; the floor transition also draws it, with its own header
function M.drawPlaque(level, header)
    if level ~= _plaque_level then
        _plaque_level = level
        plaque.pop()
        hover.select(level)
    end
    plaque.update(fps.deltaTime)
    hover.update(fps.deltaTime)
    local rx, ry = ref_topleft()
    local r, g, b = num_tint(level)
    hover.draw(level, r, g, b, rx, ry)
    plaque.draw(level, header, rx, ry, num_tint)
    draw_difficulty(level, rx, ry)
end

-- title screen scene, called by Script.lua every title frame; `highest` syncs the door darkening with the save
function M.drawTitle(highest)
    if highest ~= nil then _highest = highest end
    local dt = fps.deltaTime
    local target = scene.floor_center(10) - focus_y()
    _cam = _cam + (target - _cam) * (1 - math.exp(-SCROLL_SPEED * dt))
    apply_sky(skybox.glideHour(skybox.hourFor(nil), dt))      -- back to noon
    scene.draw(_cam, _highest)
end

function M.draw()
    scene.draw(_cam, _highest)

    local item = _items[_sel]
    local rx, ry = ref_topleft()

    if item.kind == "exit" then
        plaque.drawExit(rx, ry)
    else
        local r, g, b = num_tint(item.level)
        hover.draw(item.level, r, g, b, rx, ry)
        plaque.draw(item.level, (_mode == "checkpoint") and _sk("PAGODA_START_AT", "Start at...")
                                                         or  _sk("PAGODA_PRACTICE", "Practice"),
                    rx, ry, num_tint)
        draw_difficulty(item.level, rx, ry)
    end

    -- carets only toward an available neighbour
    carets.draw(_sel > 1, _sel < #_items, _bob_t, rx, ry)

    confirm.draw()
end

return M
