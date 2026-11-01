-- Result screen after a challenge floor: the clear sequence, or the fail sequence and flight.
-- The save counts a pagoda play as soon as a fail screen opens.

local Easing     = require("Easing")
local save       = require("save")
local skybox     = require("skybox")
local assets     = require("encounter/assets")
local bands      = require("encounter/bands")
local scene      = require("encounter/scene")
local monster    = require("encounter/monster")
local reveal     = require("encounter/reveal")
local cast       = require("encounter/cast")
local script     = require("encounter/script")
local banner     = require("encounter/banner")
local impact     = require("fx/impact")
local flight     = require("fail_flight")
local clearLines = require("story/clear_lines")
local failLines  = require("story/fail_lines")
local cutscenes  = require("encounter/cutscenes/init")
local exclaim    = require("encounter/exclaim")
local rewards    = require("rewards")
local Config     = require("Core.Config")
local Layout     = Config.load("Config/layout.json")
local Animation  = Config.load("Config/animation.json")

local M = {}

-- timings, seconds
local Time = Animation.stage.floor_result
local LOAD_TIMEOUT    = Time.load_timeout_s     -- then the sequence starts even if art is still loading
local FADE_SEC        = Time.fade_s
local SETTLE_SEC      = Time.settle_s           -- after the fade-in, before the sequence starts
local BANNER_BEAT     = Time.banner_beat_s      -- after the banner has landed
local LINES_HOLD      = Time.lines_hold_s       -- replaces the dialog when the floor has no lines
local CHARGE_SEC      = Time.charge_s
local FLOWN_BEAT      = Time.flown_beat_s       -- after the cast has flown off, before the fade-out
-- safety limits: a step that never reports its end moves on after this long
local Timeouts = Time.timeouts
local VANISH_TIMEOUT  = Timeouts.vanish_s
local CLEAR_FX_TIMEOUT = Timeouts.clear_fx_s
local BANNER_TIMEOUT  = Timeouts.banner_s
local RUN_TIMEOUT     = Timeouts.run_s
local CHARGE_TIMEOUT  = Timeouts.charge_s
local FLY_TIMEOUT     = Timeouts.fly_s
local COME_IN_TIMEOUT = Timeouts.come_in_s      -- the dark room's monster coming in
local CREDITS_FADE    = Time.credits_fade_s
local CREDITS_TIMEOUT = Timeouts.credits_s

local IMPACT_SHAKE_AMP, IMPACT_SHAKE_SEC = Time.impact_shake.amp, Time.impact_shake.duration_s
local ROOF_LEVEL = 21

-- doorway centre x (band art, 1920 space) the cast climbs to after a clear on 11-19
local CLIMB_DOOR_X = Layout.stage.floor_result.climb_door_x

local SFX_PATH = {
    beaten     = "Sounds/result/beaten.ogg",
    dissolve   = "Sounds/result/dissolve.ogg",
    clear      = "Sounds/result/clear.ogg",
    fail       = "Sounds/result/fail.ogg",
    door_open  = "Sounds/result/door_open.ogg",
    wood_break = "Sounds/result/wood_break.ogg",
    impact     = "Sounds/result/impact.ogg",
    scatter    = "Sounds/scatter.ogg",
}

-- clear: idle|loading|fadein|settle|vanish|banner|bannerBeat|bannerOut|lines|hold|door|run|fadeout
--   (the dark level-15 room skips vanish)
-- fail:  idle|loading|fadein|settle|banner|bannerBeat|bannerOut|lines|hold|charge|fly|flown|fadeout|flight|landed
-- cutscene: idle|loading|fadein|settle|banner|bannerBeat|bannerOut|lines|hold, then
--   credits|creditsOut (a clear ending on the credits), or fadeout then flight|landed or held (fail)
local _phase = "idle"
local _phaseT = 0
local _outcome = "clear"
local _level, _ctx = 1, {}
local _keys = nil
local _opened = false
local _fade = 1
local _scatter = false          -- level 15 cleared while still dark
local _litT = nil               -- s since that room started lighting up, nil before
local _dissolveT = nil          -- s since its monster started dissolving, nil before
local _bannerOn, _impactOn, _flightOn = false, false, false
local _hitX, _hitY = 0, 0
local _sfx = {}
local _cut, _cutScript = nil, nil       -- the visit's cutscene and its script data
local _ending = nil                     -- the cutscene's ending: transition | credits | flight | title
local _credits, _creditsOn = nil, false
local _rewards, _rewardIdx = {}, 0      -- the clear's pop-ups (rewards.grant) and the one showing
local _rewardsShown = false
local _rewardThen = nil                 -- what follows the last pop-up
local _afterCredits = false             -- credits over: the pop-ups play over black

-- the endings a cutscene may ask for, by outcome, and the default of each
local ENDINGS = {
    clear = { transition = true, credits = true },
    fail  = { flight = true, title = true },
}
local DEFAULT_ENDING = { clear = "transition", fail = "flight" }

local lightUp, lighting         -- defined below

-- dialog hooks for the clear and fail lines
local hooks = {
    onLine = function(line)
        if line.sprite ~= nil then cast.setSprite(line.sprite) end
        if line.bubble == "exclaim" then exclaim.start() end
        if line.cheer == true and cast.hasUme() then cast.cheer() end
        if line.event == "light_up" then lightUp() end
    end,
    onShake = scene.shake,
    busy = function() return lighting() end,
}

local cutHooks = {
    onLine = scene.onCutLine,
    onShake = scene.shake,
    busy = scene.busy,
}

-- levels inside the pagoda
local function inPagoda(level) return level >= 11 and level <= 20 end

-- the floors that end on a door
local function isDoor(level) return inPagoda(level) and level ~= 15 end

local function runDir(level)
    if level >= ROOF_LEVEL then return "up" end
    if level >= 6 and level <= 10 then return "diag" end
    return "right"
end

-- the doorway the cast climbs through after a clear, nil where they run off-screen
local function climbDoorX(level)
    if level < 11 or level > 19 then return nil end
    return (level == 15) and CLIMB_DOOR_X.checkpoint or CLIMB_DOOR_X.pagoda
end

-- a level's lines from a story/*_lines.lua table: its own entry, else the roof or default list
local function linesFor(set, level)
    if set == nil then return {} end
    local lines = set[level]
    if lines == nil then lines = (level >= ROOF_LEVEL) and set.roof or set.default end
    return lines or {}
end

local function loadSfx(names)
    for _, n in ipairs(names) do
        if _sfx[n] == nil and SFX_PATH[n] ~= nil then
            local ok, s = pcall(function() return SOUND:CreateSFX(SFX_PATH[n]) end)
            if ok and s ~= nil then _sfx[n] = s end
        end
    end
end

local function playSfx(name)
    local s = _sfx[name]
    if s ~= nil then s:Play() end
end

local function freeSfx()
    for _, s in pairs(_sfx) do s:Dispose() end
    _sfx = {}
end

-- the sounds one result plays
local function sfxNames(level, outcome, scatter)
    if outcome == "fail" then
        return inPagoda(level) and { "fail", "impact", "wood_break" } or { "fail", "impact" }
    end
    local names = { inPagoda(level) and "dissolve" or "beaten", "clear" }
    if isDoor(level) then names[#names + 1] = "door_open" end
    if scatter then names[#names + 1] = "scatter" end
    return names
end

-- the band's clear effect (optional per band) and whether it still runs
local function clearFx()
    local band = scene.band()
    if band ~= nil and band.onClear ~= nil then band.onClear() end
end

local function clearBusy()
    local band = scene.band()
    return band ~= nil and band.clearBusy ~= nil and band.clearBusy() == true
end

-- true once the band shows the monster's whole body
local function monsterIn()
    local band = scene.band()
    return band == nil or band.monsterAlpha == nil or band.monsterAlpha() >= 1
end

-- lights up the dark level-15 room
lightUp = function()
    if not _scatter or _litT ~= nil then return end
    _litT = 0
    playSfx("scatter")
    clearFx()
end

-- dissolves the dark room's monster once lit
local function dissolveLit()
    if _litT == nil or _dissolveT ~= nil then return end
    if not monsterIn() and _litT < COME_IN_TIMEOUT then return end
    _dissolveT = 0
    monster.vanish("dissolve")
    playSfx("dissolve")
end

-- true from the light-up until the monster is gone and the room is lit
lighting = function()
    if _litT == nil then return false end
    local gone = _dissolveT ~= nil and (monster.vanished() or _dissolveT >= VANISH_TIMEOUT)
    local lit = not clearBusy() or _litT >= CLEAR_FX_TIMEOUT
    return not (gone and lit)
end

local function setPhase(p)
    _phase, _phaseT = p, 0
end

-- frees everything the visit created
local function teardown()
    if not _opened then return end
    _opened = false
    if _bannerOn then banner.close() ; _bannerOn = false end
    if _impactOn then impact.close() ; _impactOn = false end
    if _flightOn then flight.close() ; _flightOn = false end
    if _creditsOn then _credits.close() ; _creditsOn = false end
    if _phase == "rewards" then rewards.stop() end
    exclaim.reset()
    scene.close()
    assets.release()
    freeSfx()
    _keys = nil
    _cut, _cutScript, _ending, _credits = nil, nil, nil, nil
end

local _keyPictures = false

-- the key pop-ups' pictures, shared once per session under the names the rewards module shows
local function shareKeyPictures()
    if _keyPictures then return end
    _keyPictures = true
    for id in pairs(rewards.KEYS) do
        SHARED:SetSharedTexture(rewards.KEY_PICTURE .. id, "Textures/rewards/key_" .. id .. ".png")
    end
end

-- tex: the stage's resident textures; fonts: the story fonts
function M.setup(tex, fonts)
    scene.setup(tex)
    script.setup(fonts)
    flight.setup(tex, fonts)
    exclaim.setup(tex)
    shareKeyPictures()
    local credits = cutscenes.credits()
    if credits ~= nil and credits.setup ~= nil then credits.setup(tex, fonts) end
end

function M.active() return _phase ~= "idle" end

-- the ending a cutscene's script asks for on this outcome
local function endingOf(data, outcome)
    local e = (type(data.ending) == "table") and data.ending[outcome] or nil
    if ENDINGS[outcome][e] then return e end
    return DEFAULT_ENDING[outcome]
end

-- opens a cutscene's result art (plus the credits art when it ends on them)
local function openCut()
    _scatter = false
    _ending = endingOf(_cutScript, _outcome)
    _credits = (_ending == "credits") and cutscenes.credits() or nil
    _keys = scene.cutKeys(_cut, _outcome, _level, _ctx, banner.keys(_outcome),
                          (_credits ~= nil) and _credits.keys() or nil)
    assets.open(_keys)
    scene.openCut(_cut, _outcome, _level, _ctx)
    loadSfx({ _outcome })
    _opened = true
    _fade = 1
    setPhase("loading")
end

-- level: the floor just played; ctx: its encounter ctx from before the result was saved;
-- outcome = "clear" | "fail"; got: the clear's rewards (rewards.grant)
function M.open(level, ctx, outcome, got)
    teardown()
    _level, _ctx = level, ctx or {}
    _outcome = (outcome == "clear") and "clear" or "fail"
    _rewards = (_outcome == "clear" and got) or {}
    _rewardIdx, _rewardsShown, _rewardThen, _afterCredits = 0, false, nil, false
    _litT, _dissolveT = nil, nil
    if _outcome == "fail" then save.count_play() end
    _cut, _cutScript = cutscenes.forLevel(level)
    if _cut ~= nil then return openCut() end
    _scatter = _outcome == "clear" and level == 15 and (_ctx.tower or _ctx.highest or 0) < 15

    -- the band's clear art is only drawn on a clear
    local band, variant = bands.forLevel(level, _ctx)
    local resultKeys = nil
    if _outcome == "clear" and band.resultKeys ~= nil then resultKeys = band.resultKeys(level, _ctx, variant) end
    _keys = scene.keys(level, _ctx, resultKeys, banner.keys(_outcome),
                       (_outcome == "fail") and impact.keys() or nil)
    assets.open(_keys)
    scene.open(level, _ctx, { placed = true, sprite = (_outcome == "clear") and "brave" or "fail" })
    if _outcome == "fail" then
        impact.open()
        _impactOn = true
    end
    loadSfx(sfxNames(level, _outcome, _scatter))
    _opened = true
    _fade = 1
    setPhase("loading")
end

local function startBanner()
    banner.open(_outcome)
    _bannerOn = true
    playSfx(_outcome)
    setPhase("banner")
end

local function startLines(lines, h)
    if script.play(lines, h or hooks, _ctx) == "running" then setPhase("lines")
    else setPhase("hold") end
end

-- the clear's pop-ups have not played yet
local function rewardsPending()
    return not _rewardsShown and #_rewards > 0
end

-- the next pop-up, or what follows them once they have all played
local function nextReward()
    while _rewardIdx < #_rewards do
        _rewardIdx = _rewardIdx + 1
        if rewards.show(_rewards[_rewardIdx]) then
            setPhase("rewards")
            return
        end
    end
    local after = _rewardThen
    _rewardThen = nil
    if after ~= nil then after() end
end

local function playRewards(after)
    _rewardsShown, _rewardIdx, _rewardThen = true, 0, after
    nextReward()
end

local function afterBanner()
    if _cut ~= nil then
        startLines(_cutScript[_outcome], cutHooks)
    elseif _outcome == "clear" then
        if cast.hasUme() then cast.cheer() end
        -- on the roofs the pop-ups come right after the banner
        if _level >= ROOF_LEVEL and rewardsPending() then
            playRewards(function() startLines(linesFor(clearLines, _level)) end)
            return
        end
        startLines(linesFor(clearLines, _level))
    else
        startLines(linesFor(failLines.encounter, _level))
    end
end

local function startRun()
    local doorX = climbDoorX(_level)
    if doorX ~= nil then cast.startClimb(doorX) else cast.startRun(runDir(_level)) end
    setPhase("run")
end

-- the cutscene's state goes; its textures stay for the credits, which open on its last frame
local function startCredits()
    scene.close()
    _credits.open()
    _creditsOn = true
    _fade = 0
    setPhase("credits")
end

local function afterLines()
    exclaim.stop()
    if _cut ~= nil then
        if _ending == "credits" and _credits ~= nil then startCredits() ; return end
        if _outcome == "clear" and rewardsPending() then playRewards(afterLines) ; return end
        setPhase("fadeout")
    elseif _outcome == "fail" then
        _hitX, _hitY = cast.hitPoint()
        monster.charge(_hitX, CHARGE_SEC)
        setPhase("charge")
    elseif _scatter and (_litT == nil or lighting()) then
        -- the dark room is lit before the cast leaves, by its lines or here
        lightUp()
        setPhase("lightup")
    elseif rewardsPending() then
        -- Ume's line, then the pop-ups, then the cast moves on
        _rewardsShown = true
        if script.play(clearLines.rewards or {}, hooks, _ctx) == "running" then setPhase("rewardLine")
        else playRewards(afterLines) end
    elseif isDoor(_level) then
        playSfx("door_open")
        clearFx()
        setPhase("door")
    else
        startRun()
    end
end

local function hit()
    impact.fire(_hitX, _hitY)
    playSfx("impact")
    scene.shake(IMPACT_SHAKE_AMP, IMPACT_SHAKE_SEC)
    cast.knockback()
    setPhase("fly")
end

-- on full black: the encounter art goes, the flight opens its own (it starts on black)
local function startFlight()
    if _bannerOn then banner.close() ; _bannerOn = false end
    if _impactOn then impact.close() ; _impactOn = false end
    scene.close()
    assets.release()
    _keys = nil
    local lines = (_cutScript ~= nil and _cutScript.flight) or linesFor(failLines.flight, _level)
    flight.open(_level, _ctx.tower or _ctx.highest or 0, lines, _ctx)
    _flightOn = true
    setPhase("flight")
end

local function finish(result)
    teardown()
    skybox.reset()
    _phase, _fade = "idle", 1
    return result
end

-- a lost run straight to the title: the visit's art goes and a black frame is held for the doors
local function holdBlack()
    scene.close()
    assets.release()
    _keys = nil
    _fade = 1
    setPhase("held")
end

-- the cutscene's ending other than the credits, from a fully black frame
local function endCut()
    if _ending == "title" then holdBlack() ; return "fail" end
    if _outcome == "clear" then return finish("clear") end
    startFlight()
    return nil
end

-- nil while running, then "clear" on a fully black frame, or "fail" once the flight has landed
-- (or a cutscene's black frame is held): it stays on screen (and "fail" keeps coming) until release()
function M.update(dt, nav)
    if _phase == "idle" then return nil end
    if _phase == "landed" or _phase == "held" then return "fail" end
    _phaseT = _phaseT + dt

    if _phase == "loading" then
        if assets.ready(_keys) or _phaseT >= LOAD_TIMEOUT then
            setPhase("fadein")
            scene.begin()
        end
        return nil
    end

    if _phase == "flight" then
        if flight.update(dt, nav) == "done" then
            setPhase("landed")
            return "fail"
        end
        return nil
    end

    if _phase == "credits" then
        if _credits.update(dt, nav) == "done" or _phaseT >= CREDITS_TIMEOUT then setPhase("creditsOut") end
        return nil
    end

    if _phase == "creditsOut" then
        _fade = math.min(1, _phaseT / CREDITS_FADE)
        if _phaseT >= CREDITS_FADE then
            if not rewardsPending() then return finish("clear") end
            _afterCredits = true
            playRewards(function() setPhase("rewardsOver") end)
        end
        return nil
    end

    -- after the credits, on black: the clear's pop-ups, then the transition
    if _afterCredits then
        if _phase == "rewards" then
            if not rewards.update() then nextReward() end
        elseif _phase == "rewardsOver" then
            return finish("clear")
        end
        return nil
    end

    scene.update(dt)
    exclaim.update(dt)
    if _bannerOn then banner.update(dt) end
    if _impactOn then impact.update(dt) end
    if _litT ~= nil then _litT = _litT + dt end
    if _dissolveT ~= nil then _dissolveT = _dissolveT + dt end
    dissolveLit()

    if _phase == "fadein" then
        _fade = 1 - Easing.outQuad(_phaseT / FADE_SEC)
        if _phaseT >= FADE_SEC then _fade = 0 ; setPhase("settle") end

    elseif _phase == "settle" then
        if _phaseT >= SETTLE_SEC then
            -- the dark room's monster stays for the lines that light the room up
            if _cut == nil and _outcome == "clear" and not _scatter then
                monster.vanish(inPagoda(_level) and "dissolve" or "squish")
                playSfx(inPagoda(_level) and "dissolve" or "beaten")
                setPhase("vanish")
            else
                startBanner()
            end
        end

    elseif _phase == "vanish" then
        if monster.vanished() or _phaseT >= VANISH_TIMEOUT then startBanner() end

    elseif _phase == "banner" then
        if banner.shown() or _phaseT >= BANNER_TIMEOUT then setPhase("bannerBeat") end

    elseif _phase == "bannerBeat" then
        if _phaseT >= BANNER_BEAT then
            banner.fadeOut()
            setPhase("bannerOut")
        end

    elseif _phase == "bannerOut" then
        if banner.gone() or _phaseT >= BANNER_TIMEOUT then
            banner.close()
            _bannerOn = false
            afterBanner()
        end

    elseif _phase == "lines" then
        if script.update(dt, true) ~= "running" then afterLines() end

    elseif _phase == "rewardLine" then
        if script.update(dt, true) ~= "running" then
            exclaim.stop()
            playRewards(afterLines)
        end

    elseif _phase == "rewards" then
        if not rewards.update() then nextReward() end

    elseif _phase == "hold" then
        if _phaseT >= LINES_HOLD then afterLines() end

    elseif _phase == "lightup" then
        if not lighting() then afterLines() end

    elseif _phase == "door" then
        if not clearBusy() or _phaseT >= CLEAR_FX_TIMEOUT then startRun() end

    elseif _phase == "run" then
        if cast.exited() or _phaseT >= RUN_TIMEOUT then setPhase("fadeout") end

    elseif _phase == "charge" then
        if monster.charged() or _phaseT >= CHARGE_TIMEOUT then hit() end

    elseif _phase == "fly" then
        if cast.flownOut() or _phaseT >= FLY_TIMEOUT then
            if inPagoda(_level) then playSfx("wood_break") end
            setPhase("flown")
        end

    elseif _phase == "flown" then
        if _phaseT >= FLOWN_BEAT then setPhase("fadeout") end

    elseif _phase == "fadeout" then
        _fade = math.min(1, _phaseT / FADE_SEC)
        if _phaseT >= FADE_SEC then
            if _cut ~= nil then return endCut() end
            if _outcome == "clear" then return finish("clear") end
            startFlight()
        end
    end
    return nil
end

-- frees the result screen as the stage leaves: a lost run's held landing, or a sequence cut short
function M.release()
    if _phase == "idle" then return end
    teardown()
    _phase = "idle"
    skybox.reset()
end

function M.dispose()
    teardown()
    _phase = "idle"
end

-- sky for this frame: nil when idle, otherwise grey (0..1) and whether the sky shows
function M.sky()
    if _phase == "idle" then return nil end
    if _phase == "loading" or _phase == "held" or _afterCredits then return 0, false end
    if _phase == "credits" or _phase == "creditsOut" then
        if _credits.sky ~= nil then return _credits.sky() end
        return 0, false
    end
    if _phase == "flight" or _phase == "landed" then return flight.sky() end
    local g, v = scene.sky()
    if g == nil then return 0, false end
    return g, v
end

function M.draw()
    if _phase == "idle" then return end
    if _phase == "loading" or _phase == "held" then scene.drawBlack(1) ; return end
    if _phase == "flight" or _phase == "landed" then flight.draw() ; return end
    if _phase == "credits" or _phase == "creditsOut" then
        _credits.draw()
        scene.drawBlack(_fade)
        return
    end
    if _afterCredits then
        scene.drawBlack(1)
        if _phase == "rewards" then rewards.draw() end
        return
    end

    -- the monster at full opacity; the dark level-15 room's band shows only its eyes until lit
    local _, variant = scene.band()
    local ox, oy = scene.draw(1, (variant == "shadow") and reveal.EYES_REST or 0)
    local hx, hy = cast.umeTop()
    exclaim.draw(hx, hy, ox, oy)
    if _impactOn then impact.draw(ox, oy) end
    if _bannerOn then banner.draw(ox, oy) end
    if _phase == "lines" or _phase == "rewardLine" then script.draw(scene.nametag()) end
    scene.drawBlack(_fade)
    if _phase == "rewards" then rewards.draw() end
end

return M
