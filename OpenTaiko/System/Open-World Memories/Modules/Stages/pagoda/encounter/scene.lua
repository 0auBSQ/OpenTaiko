-- Encounter scene shared by the entrance and result screens.

local skybox   = require("skybox")
local assets   = require("encounter/assets")
local bands    = require("encounter/bands")
local view     = require("encounter/bands/view")
local monsters = require("encounter/monsters")
local monster  = require("encounter/monster")
local cast     = require("encounter/cast")
local shake    = require("encounter/shake")
local Config   = require("Core.Config")
local Layout   = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.scene
local Anim = Animation.encounter.scene
local SCRIM_SX, SCRIM_SY = Look.scrim_scale_x, Look.scrim_h / 147   -- scrim.png is 147 px tall
local SCRIM_ALPHA = Look.scrim_opacity -- full in the pagoda's rooms (see scrimAlpha)
local SHARED_KEYS = { "scrim", "ui_nametag" }
local COVER_SEC = Anim.cover_s         -- for a cover line without `sec`
local RUMBLE_HZ_X, RUMBLE_HZ_Y = Anim.rumble.x_rad_per_s, Anim.rumble.y_rad_per_s
local RUMBLE_SHARE_Y = Anim.rumble.y_share

local _tex = nil
local _open = false
local _level = nil
local _band, _variant, _spec = nil, nil, nil
local _cut = nil
local _scrim, _nametag = nil, nil
local _shake = shake.new()
local _rumble, _rumbleT = 0, 0
local _cover, _coverFrom, _coverTo, _coverT, _coverSec = 0, 0, 0, 0, 0

-- tex = the stage's resident textures (black1 for the cover; some bands borrow others)
function M.setup(tex)
    _tex = tex
    if bands.setup ~= nil then bands.setup(tex) end
end

-- adds each key list to list, skipping keys already in seen
local function addKeys(list, seen, ...)
    for i = 1, select("#", ...) do
        for _, k in ipairs((select(i, ...)) or {}) do
            if not seen[k] then seen[k] = true ; list[#list + 1] = k end
        end
    end
end

-- every texture key of the scene for a visit, then each extra key list, without duplicates
function M.keys(level, ctx, ...)
    ctx = ctx or {}
    local band, variant = bands.forLevel(level, ctx)
    local spec = monsters.forLevel(level, ctx.monster)
    local list, seen = {}, {}
    addKeys(list, seen, band.keys(level, ctx, variant), monster.keys(spec), cast.keys(level), ...)
    addKeys(list, seen, SHARED_KEYS)
    return list
end

-- the same for a cutscene screen (phase "enter" | "clear" | "fail"), in place of band, monster and cast
function M.cutKeys(cut, phase, level, ctx, ...)
    local list, seen = {}, {}
    addKeys(list, seen, cut.keys(level, ctx or {}, phase), ...)
    addKeys(list, seen, SHARED_KEYS)
    return list
end

local function resetEffects()
    _shake:stop()
    _rumble, _rumbleT = 0, 0
    _cover, _coverFrom, _coverTo, _coverT, _coverSec = 0, 0, 0, 0, 0
end

-- ctx = { startLevel, highest, firstTime, playerName, monster, lostBefore }; castOpts goes to cast.open
function M.open(level, ctx, castOpts)
    M.close()
    ctx = ctx or {}
    _level = level
    _band, _variant = bands.forLevel(level, ctx)
    _spec = monsters.forLevel(level, ctx.monster)
    skybox.reset()                     -- bands with a sky set their own view in open
    _band.open(level, ctx, _variant)
    monster.open(_spec, level)
    cast.open(level, castOpts)
    _scrim   = assets.get("scrim")
    _nametag = assets.get("ui_nametag")
    resetEffects()
    _open = true
end

-- a cutscene screen; the cutscene's textures are already opened by the screen
function M.openCut(cut, phase, level, ctx)
    M.close()
    skybox.reset()                     -- a cutscene with a sky sets its own view in open
    _level = level
    _cut = cut
    _cut.open(level, ctx or {}, phase)
    _scrim   = assets.get("scrim")
    _nametag = assets.get("ui_nametag")
    resetEffects()
    _open = true
end

function M.close()
    if not _open then return end
    _open = false
    if _cut ~= nil then
        _cut.close()
    else
        if _band ~= nil then _band.close() end
        monster.close()
        cast.close()
    end
    _band, _variant, _spec, _cut, _level = nil, nil, nil, nil, nil
    _scrim, _nametag = nil, nil
    resetEffects()
end

function M.isOpen() return _open end

-- the band module and variant of the open visit (nil for a cutscene)
function M.band() return _band, _variant end

-- the cutscene of the open visit, or nil
function M.cut() return _cut end

function M.nametag() return _nametag end

-- restarts the background push-in; called as the screen fades in
function M.begin()
    view.reset()
    if _cut ~= nil and _cut.begin ~= nil then _cut.begin() end
end

function M.shake(amp, dur)
    _shake:start(amp, dur)
end

-- a steady shake of amp px that lasts until rumble(0)
function M.rumble(amp)
    _rumble = amp or 0
    if _rumble <= 0 then _rumble, _rumbleT = 0, 0 end
end

-- fades the cover (under the dialog) to opacity `to` over sec seconds
function M.cover(to, sec)
    _coverFrom, _coverTo = _cover, math.max(0, math.min(1, to or 1))
    _coverT, _coverSec = 0, math.max(0, sec or COVER_SEC)
    if _coverSec <= 0 then _cover = _coverTo end
end

-- a cutscene script line: its event goes to the cutscene, its cover to M.cover
function M.onCutLine(line)
    if _cut == nil or line == nil then return end
    if line.event ~= nil then _cut.event(line.event, line) end
    if line.cover ~= nil then M.cover(line.cover, line.sec) end
end

-- true while the cover fades or the cutscene reports an animation still running
function M.busy()
    if _coverT < _coverSec then return true end
    return _cut ~= nil and _cut.busy() == true
end

function M.update(dt)
    if not _open then return end
    _shake:update(dt)
    if _rumble > 0 then _rumbleT = _rumbleT + dt end
    if _coverT < _coverSec then
        _coverT = math.min(_coverSec, _coverT + dt)
        _cover = _coverFrom + (_coverTo - _coverFrom) * (_coverT / _coverSec)
    end
    if _cut ~= nil then
        _cut.update(dt)
        view.update(dt)
        return
    end
    _band.update(dt)
    view.update(dt)
    monster.update(dt)
    cast.update(dt)
end

-- grey (0..1) and whether the sky shows behind the band; nil while closed
function M.sky()
    if not _open then return nil end
    if _cut ~= nil then
        if _cut.sky == nil then return 0, false end
        return _cut.sky()
    end
    return _band.sky()
end

-- the scrim's opacity on a level's screens: full in the pagoda's rooms 11-14 and 16-19
function M.scrimAlpha(level)
    if level ~= nil and level >= 11 and level <= 19 and level ~= 15 then return 1 end
    return SCRIM_ALPHA
end

-- a scrim texture at opacity a over the top of the screen, behind the dialog
function M.drawScrim(scrim, a, ox, oy)
    if scrim == nil or not scrim.Ready or a <= 0 then return end
    scrim:SetScale(SCRIM_SX, SCRIM_SY)
    scrim:SetOpacity(a)
    scrim:Draw(ox, oy)
    scrim:SetOpacity(1)
    scrim:SetScale(1, 1)
end

-- this frame's shake offset, the rumble included
local function offset()
    local ox, oy = _shake:offset()
    if _rumble > 0 then
        ox = ox + math.sin(_rumbleT * RUMBLE_HZ_X) * _rumble
        oy = oy + math.cos(_rumbleT * RUMBLE_HZ_Y) * _rumble * RUMBLE_SHARE_Y
    end
    return ox, oy
end

-- layers, scrim, overlay, then the cover
local function drawCut(ox, oy)
    _cut.draw(ox, oy)
    local a = M.scrimAlpha(_level)
    if _cut.scrimAlpha ~= nil then a = _cut.scrimAlpha() or a end
    M.drawScrim(_scrim, a, ox, oy)
    if _cut.drawOver ~= nil then _cut.drawOver(ox, oy) end
    M.drawBlack(_cover)
end

-- ma = the monster's opacity, eyes = 0..1; a band may hide the body (monsterAlpha), not the eyes.
-- Returns the shake offset so the screen's own layers can follow it.
function M.draw(ma, eyes)
    local ox, oy = offset()
    if not _open then return ox, oy end
    if _cut ~= nil then drawCut(ox, oy) ; return ox, oy end
    local ca = _band.castAlpha()
    local r, g, b = _band.castFilter()
    if _band.monsterAlpha ~= nil then ma = (ma or 1) * _band.monsterAlpha() end

    _band.drawBehind(ox, oy)
    _band.drawBack(ox, oy)
    _band.drawMid(ox, oy)
    M.drawScrim(_scrim, M.scrimAlpha(_level), ox, oy)
    cast.drawShadows(ca, ox, oy)
    monster.drawShadow(ma, ox, oy)
    monster.draw(ma, r, g, b, eyes, ox, oy)
    cast.draw(ca, r, g, b, ox, oy)
    cast.drawDust(ca, ox, oy, _band.groundDust())
    _band.drawFront(ox, oy)
    return ox, oy
end

-- full-screen black at opacity a
function M.drawBlack(a)
    local b = _tex ~= nil and _tex.black1 or nil
    if b == nil or not b.Ready or a <= 0 then return end
    local res = THEME:GetResolution()
    b:SetScale(res.X / math.max(1, b.Width), res.Y / math.max(1, b.Height))
    b:SetOpacity(a) ; b:Draw(0, 0)
    b:SetOpacity(1) ; b:SetScale(1, 1)
end

return M
