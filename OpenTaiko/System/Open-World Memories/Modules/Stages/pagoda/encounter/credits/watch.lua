-- Third end-credits background (the level-15 room).

local assets     = require("encounter/assets")
local checkpoint = require("encounter/bands/checkpoint")
local student    = require("encounter/student")
local moves      = require("encounter/cast_moves")
local actor      = require("encounter/credits/actor")
local Emitter    = require("fx/emitter")
local presets    = require("fx/presets")
local Config     = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

M.holes = true

local Look = Layout.encounter.credits.watch
local Anim = Animation.encounter.credits.watch

local UME_X, UME_Y = Look.ume_x, Look.ume_y
local STUDENT_Y = Look.student_y
local START_X = Look.start_x
local DOOR_X = Look.door_x
local STUDENT_AT = Anim.student_at_s    -- seconds into the scene
local BACK_AT = Anim.back_at_s
-- art, start (s after the Student), lane (feet y), stop by the doorway, idle tempo
local SPIRITS = {
    { key = "norm4b" },
    { key = "critter" },
    { key = "norm4b" },
    { key = "critter" },
    { key = "critter" },
    { key = "norm4b" },
}
for i, sp in ipairs(SPIRITS) do
    local p, m = Look.spirits[i], Anim.spirits[i]
    sp.at, sp.y, sp.door, sp.tempo = m.at_s, p.y, p.door, m.tempo
end
local SPIRIT_SCALE = Look.spirit_scale
local SPIRIT_HOP, SPIRIT_HOP_SEC = Anim.spirit_hop_px, Anim.spirit_hop_s
local IDLE = Config.plain(Anim.ume_idle)
local DUST_COUNT = Anim.dust_count

local _u = 0
local _ume, _student = nil, nil
local _frames = nil
local _shadow, _shadowMeta = nil, nil
local _dust = nil
local _braked, _turned, _back, _startled = false, false, false, false
local _spirits = {}
local _list = {}

local function dustKey()
    return Emitter.texKeys(presets.dust_puff)[1] or "fx_dust"
end

function M.keys()
    local keys = checkpoint.keys(15, nil, "clear")
    for _, k in ipairs({ "ume_normal", "ume_normal_m", "ume_brave", "fx_shadow", dustKey() }) do keys[#keys + 1] = k end
    for _, k in ipairs(student.keys()) do keys[#keys + 1] = k end
    for _, k in ipairs({ "m_norm4b", "m_norm4b_m", "m_critter", "m_critter_m" }) do keys[#keys + 1] = k end
    return keys
end

function M.open()
    M.close()
    checkpoint.open(15, { tower = 20, highest = 20 }, "clear")
    student.prepare()
    _frames = student.frames()
    _shadow, _shadowMeta = assets.get("fx_shadow")
    _dust = Emitter.new(presets.dust_puff, (assets.get(dustKey())))
    _ume = actor.new("ume_normal_m", UME_X, UME_Y, { idle = IDLE, seed = 152, shadowKey = "ume_brave" })
    _student = actor.new(_frames.run, START_X, STUDENT_Y, { grad = student.palette(), shadowKey = _frames.normal, shadowPx = Look.student_shadow_w })
    _student.visible = false
    _list = { _ume, _student }
    _spirits = {}
    for i, sp in ipairs(SPIRITS) do
        local idle = actor.monsterIdle(sp.key)
        local a = actor.new("m_" .. sp.key .. "_m", START_X, sp.y, { idle = idle, seed = 160 + i })
        a.visible, a.idleK, a.base = false, Anim.idle_weight, "m_" .. sp.key
        _spirits[i] = a
        _list[#_list + 1] = a
    end
    _u = 0
    _braked, _turned, _back, _startled = false, false, false, false
end

-- the Student's run and climb, tc seconds in
local function climb(tc)
    local s = _student
    local x, y, dy, rot, sx, sy, alpha, mirrored, step = moves.climb(START_X, STUDENT_Y, DOOR_X, tc)
    s.x, s.y, s.gy, s.dy, s.rot, s.sx, s.sy, s.alpha = x, y, y, dy, rot, sx, sy, alpha
    s.shadowA = alpha
    s.key = mirrored and _frames.run_m or _frames.run
    s.visible = step ~= "done"
    if step == "brake" and not _braked then
        _braked = true
        if _dust ~= nil then _dust:burst(DUST_COUNT, x, y) end
    end
end

-- spirit sp after the Student, tc seconds after its start
local function chase(a, sp, tc)
    local x, y, dy, rot, sx, sy, alpha, turned, step = moves.climb(START_X, sp.y, DOOR_X + sp.door, tc)
    if step == "run" then
        dy = dy - SPIRIT_HOP * math.abs(math.sin(math.pi * tc / SPIRIT_HOP_SEC * sp.tempo))
    end
    a.x, a.y, a.gy, a.dy, a.rot = x, y, y, dy, rot
    a.sx, a.sy = sx * SPIRIT_SCALE, sy * SPIRIT_SCALE
    a.alpha, a.shadowA = alpha, alpha
    a.key = turned and a.base or (a.base .. "_m")
    a.visible = step ~= "done"
end

function M.update(dt, u)
    _u = u
    checkpoint.update(dt)
    if _u >= STUDENT_AT then climb(_u - STUDENT_AT) end
    for i, sp in ipairs(SPIRITS) do
        local tc = _u - STUDENT_AT - sp.at
        if tc >= 0 then chase(_spirits[i], sp, tc) else _spirits[i].visible = false end
    end
    if _turned and not _startled and _spirits[1].visible and _spirits[1].x > UME_X - Look.startle_dist then
        _startled = true
        _ume:notice()
    end
    if not _turned and _student.visible and _student.x > UME_X then
        _turned = true
        _ume:face("ume_normal", true)
        _ume:notice()
    end
    if _turned and not _back and _u >= BACK_AT then
        _back = true
        _ume:face("ume_normal_m", true)
    end
    _ume:update(dt)
    for _, a in ipairs(_spirits) do a:update(dt) end
    if _dust ~= nil then _dust:update(dt) end
end

-- drawn under the scene it fades into or from, so a = 1; c = 1 (the sky is dimmed over it)
function M.draw(a, c)
    checkpoint.drawBehind(0, 0)
    checkpoint.drawBack(0, 0)
    checkpoint.drawMid(0, 0)
    actor.drawAll(_list, _shadow, _shadowMeta, a, c)
    if _dust ~= nil then
        local r, g, b = checkpoint.groundDust()
        _dust:draw(0, 0, a, r * c, g * c, b * c)
    end
    checkpoint.drawFront(0, 0)
end

function M.close()
    checkpoint.close()
    if _dust ~= nil then _dust:clear() end
    _dust, _ume, _student, _frames = nil, nil, nil, nil
    _shadow, _shadowMeta = nil, nil
    _spirits, _list = {}, {}
end

return M
