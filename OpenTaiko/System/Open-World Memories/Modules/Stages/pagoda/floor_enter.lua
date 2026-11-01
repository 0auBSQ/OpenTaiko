-- Encounter screen on entering a floor.
-- After update() returns "start" or "leave" it holds a black frame until release().

local Easing   = require("Easing")
local skybox   = require("skybox")
local assets   = require("encounter/assets")
local scene    = require("encounter/scene")
local monster  = require("encounter/monster")
local cast     = require("encounter/cast")
local script   = require("encounter/script")
local menu     = require("encounter/menu")
local reveal   = require("encounter/reveal")
local music    = require("encounter/music")
local songbars = require("encounter/songbars")
local items    = require("encounter/items/init")
local cutscenes = require("encounter/cutscenes/init")
local Config   = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local M = {}

local Time = Animation.stage.floor_enter
local LOAD_TIMEOUT = Time.load_timeout_s
local FADE_SEC     = Time.fade_s
local ENTRANCE_AT  = Time.entrance_at_s        -- s after the fade-in starts
local CUT_OPENING_MAX = Time.cut_opening_max_s -- max s a cutscene's opening holds the dialog

local _tex = nil

local _phase = "idle"   -- idle|loading|fadein|entrance|dialog|items|bars|menu|walkout|fadeout|held
local _phaseT, _visT = 0, 0
local _level, _ctx = 1, nil
local _keys, _preKey = nil, nil
local _opened = false
local _entering = false
local _fade = 1
local _result = nil
local _menuTex = nil
local _cut, _cutScript = nil, nil       -- the visit's cutscene and its script data

local hooks = {
    onLine = function(line)
        if line.sprite ~= nil then cast.setSprite(line.sprite) end
        reveal.onLine(line)
    end,
    onShake = scene.shake,
}

local cutHooks = {
    onLine = function(line)
        if line.music == true and not music.active() then music.start(_level) end
        scene.onCutLine(line)
    end,
    onShake = scene.shake,
    busy = scene.busy,
}

function M.setup(tex, fonts)
    _tex = tex
    scene.setup(tex)
    script.setup(fonts)
    menu.setup(fonts)
    songbars.setup(tex, fonts)
    items.setup(tex, fonts)
end

function M.active() return _phase ~= "idle" end

-- starts loading the floor's encounter art (called when the floor transition opens)
function M.preload(level, ctx)
    if _opened then return end
    ctx = ctx or {}
    local cut = cutscenes.forLevel(level)
    local list
    if cut ~= nil then list = scene.cutKeys(cut, "enter", level, ctx, menu.keys(), songbars.keys(level, ctx), items.keys(level, ctx))
    else list = scene.keys(level, ctx, menu.keys(), songbars.keys(level, ctx), items.keys(level, ctx)) end
    local key = level .. "|" .. table.concat(list, ",")
    if key == _preKey then return end
    if _preKey ~= nil then assets.release() end
    assets.open(list)
    _keys, _preKey = list, key
end

-- frees everything the visit created
local function teardown()
    if not _opened then return end
    _opened = false
    music.stop()
    songbars.close()
    items.close()
    assets.release()
    scene.close()
    _keys, _preKey = nil, nil
    _menuTex = nil
    _cut, _cutScript = nil, nil
end

local function setPhase(p)
    _phase, _phaseT = p, 0
end

-- ctx = { startLevel, highest, tower, firstTime, playerName, monster, lostBefore, roll, runStart, reroll };
-- roll: the floor's rolled songs and speed (encounter/songbars.lua); runStart: the run's first floor;
-- tower: highest floor shown cleared; reroll(keepMystery): a new roll of the floor's songs
function M.open(level, ctx)
    teardown()
    _level, _ctx = level, ctx or {}
    M.preload(level, _ctx)
    _cut, _cutScript = cutscenes.forLevel(level)
    if _cut ~= nil then
        scene.openCut(_cut, "enter", level, _ctx)
        menu.open()
        script.open(level, _ctx, cutHooks, _cutScript.enter)
    else
        scene.open(level, _ctx)
        menu.open()
        script.open(level, _ctx, hooks)
        reveal.prepare(script.lines(), scene.band().castAlpha())
    end
    songbars.open(level, _ctx)
    items.open(level, _ctx, { lines = (_cut ~= nil) and cutHooks or hooks, shake = scene.shake })
    _menuTex = {
        button = assets.get("ui_button"), marker = assets.get("ui_marker"),
        panel  = assets.get("ui_panel"),  glow   = assets.get("fx_glow"),
        black  = _tex ~= nil and _tex.black1 or nil,
    }
    _opened = true
    setPhase("loading")
    _visT, _fade, _result, _entering = 0, 1, nil, false
end

-- the cast has arrived, or a cutscene's opening animation is over
local function arrived()
    if _cut ~= nil then return not scene.busy() or _phaseT >= CUT_OPENING_MAX end
    return _entering and cast.arrived()
end

-- starts the song bars and the floor loop (a cutscene's late loop starts here at the latest)
local function openBars()
    if not music.active() then music.start(_level) end
    songbars.start()
    items.show()
    setPhase("bars")
end

-- after the floor's dialog: the new item presentation, if any, comes first
local function afterDialog()
    if items.introPending() then
        items.startIntro()
        setPhase("items")
    else
        openBars()
    end
end

-- returns nil | "start" | "leave"
function M.update(dt, nav)
    if _phase == "idle" or _phase == "held" then return nil end
    _phaseT = _phaseT + dt

    if _phase == "loading" then
        if assets.ready(_keys) or _phaseT >= LOAD_TIMEOUT then
            setPhase("fadein")
            if _cut == nil or _cutScript.music ~= "late" then music.start(_level) end
            scene.begin()
        end
        return nil
    end

    _visT = _visT + dt
    if _cut == nil and not _entering and _visT >= ENTRANCE_AT then
        _entering = true
        cast.startEntrance()
    end
    scene.update(dt)
    songbars.update(dt)
    items.update(dt, (_phase == "menu" and not items.busy()) and menu.focused() or nil)
    if _cut == nil and cast.umeLanded() then monster.notice() end

    local pressed = false
    if _phase == "fadein" then
        _fade = 1 - Easing.outQuad(_phaseT / FADE_SEC)
        if _phaseT >= FADE_SEC then _fade = 0 ; setPhase("entrance") end

    elseif _phase == "entrance" then
        if arrived() then
            if script.hasLines() and script.start() == "running" then setPhase("dialog")
            else afterDialog() end
        end

    elseif _phase == "dialog" then
        local st
        st, pressed = script.update(dt, _cut ~= nil or not reveal.blocking())
        if st ~= "running" then afterDialog() end

    elseif _phase == "items" then
        if items.updateIntro(dt, nav, true) == "done" then openBars() end

    elseif _phase == "bars" then
        if nav.decide() then songbars.skip() end
        if songbars.done() then
            menu.setItems(items.slots())
            setPhase("menu")
        end

    elseif _phase == "menu" and not items.busy() then
        local r, id = menu.update(dt, nav)
        if r == "item" then
            items.use(id)
        elseif r == "start" then
            _result = "start" ; setPhase("fadeout")
        elseif r == "leave" then
            music.stop()
            if _cut ~= nil then
                _result = "leave" ; setPhase("fadeout")    -- no cast to walk off
            else
                cast.startExit()
                setPhase("walkout")
            end
        end

    elseif _phase == "walkout" then
        if cast.exited() then _result = "leave" ; setPhase("fadeout") end

    elseif _phase == "fadeout" then
        _fade = math.min(1, _phaseT / FADE_SEC)
        if _phaseT >= FADE_SEC then
            local r = _result
            -- hold a black frame: the stage still draws us through its exit transition, and going idle
            -- here would flash the scene again
            teardown()
            _phase, _fade = "held", 1
            return r
        end
    end

    if _cut == nil then reveal.update(dt, pressed) end
    return nil
end

-- called by the stage once it has moved on from this screen
function M.release()
    teardown()
    _phase = "idle"
    skybox.reset()
end

function M.dispose()
    teardown()
    assets.release()
    music.stop()
    script.dispose()
    songbars.dispose()
    items.dispose()
    _keys, _preKey = nil, nil
    _phase = "idle"
end

-- sky for this frame: nil when idle, otherwise grey (0..1) and whether the sky shows
function M.sky()
    if _phase == "idle" then return nil end
    if _phase == "loading" or _phase == "held" then return 0, false end
    local g, v = scene.sky()
    if g == nil then return 0, false end
    return g, v
end

function M.draw()
    if _phase == "idle" then return end
    if _phase == "held" or _phase == "loading" then scene.drawBlack(1) ; return end

    if _cut ~= nil then scene.draw() else scene.draw(reveal.monsterAlpha(), reveal.eyes()) end
    items.drawUnder()
    if _phase == "dialog" or items.talking() then script.draw(scene.nametag()) end
    songbars.draw()
    items.drawOver()
    if _phase == "menu" then menu.draw(_menuTex) end
    items.drawIcons((_phase == "menu" and not items.busy()) and menu.focused() or nil)
    if _phase == "menu" then menu.drawConfirm(_menuTex) end
    items.drawPopup()
    scene.drawBlack(_fade)
end

return M
