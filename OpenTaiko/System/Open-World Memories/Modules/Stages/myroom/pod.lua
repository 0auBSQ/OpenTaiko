---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- pod.lua — the Mysterious Pod's opening sequence: the player steps in, the pod rattles with
-- sparks, rests, vents a cloud of steam, and the room fades to black before Script.lua leaves for
-- the pod's own stage. Script.lua switches to mode "pod" while this runs and draws the player
-- through playerVisual(). Without that stage installed the pod keeps its "nothing opens" line.

local Room    = require("room")
local Almanac = require("almanac")

local Pod = {}

local ENTER_SEC = 0.55        -- player slides into the pod
local SHAKE_SEC = 2.4         -- rattling with sparks
local REST_SEC  = 0.35        -- still, before the steam
local SMOKE_SEC = 0.9         -- steam venting
local FADE_SEC  = 0.7         -- black over everything
local SHAKE_AMP = 0.045       -- world units of rattle
local SHAKE_YAW = 3.0         -- degrees of rattle

local FY = Room.FLOOR_Y

-- the stage the pod leads to (Modules/Stages/<name>), when installed
local STAGE = Almanac.plain("enc:duz5h2ol4uxwav3asmdq") or ""

local _phase = "idle"         -- idle | enter | shake | rest | smoke | fade
local _t = 0
local _world, _inst = nil, nil
local _pose = nil             -- { x, y, z, yaw, s } the pod's resting transform
local _from = nil             -- { x, z } where the player started walking in
local _sparkAcc = 0
local _sfx = {}

local function loadSfx(name)
    if _sfx[name] == nil then
        local ok, s = pcall(function() return SOUND:CreateSFX("Sounds/" .. name .. ".ogg") end)
        _sfx[name] = (ok and s ~= nil) and s or false
    end
    return _sfx[name] or nil
end

local function play(name)
    local s = loadSfx(name)
    if s ~= nil then s:Play() end
end

local function stop(name)
    local s = _sfx[name]
    if s then pcall(function() s:Stop() end) end
end

function Pod.stageName() return STAGE end

function Pod.stageExists()
    if STAGE == "" then return false end
    local ok, res = pcall(function() return TEXTURE:Exists("../" .. STAGE .. "/Script.lua") end)
    return ok and res == true
end

function Pod.active() return _phase ~= "idle" end

-- it = the furniture entry, inst = its GLB instance (world._propInst[it]); px/pz = player feet;
-- playerIndex = whose room this is (the stage reads it back from a global store)
function Pod.start(world, it, inst, px, pz, playerIndex)
    _world, _inst = world, inst
    pcall(function()
        local db = DATABASE:OpenGlobalDatabase(STAGE .. "_handoff")
        db:Write("player", tostring(playerIndex or 0))
    end)
    local cat = Room.CATALOG[it.id]
    local w, h = cat.w, cat.h
    if (it.facing or 0) % 2 == 1 then w, h = h, w end
    local cx, cz = world:footprintCenter(it.c, it.r, w, h)
    local s = (inst and Room.modelFit(inst.model, cat)) or 1
    _pose = { x = cx, y = FY, z = cz, yaw = (it.facing or 0) * 90, s = s }
    _from = { x = px, z = pz }
    _phase, _t, _sparkAcc = "enter", 0, 0
end

function Pod.reset()
    if _inst and _pose then
        pcall(function() _inst:setTransform(_pose.x, _pose.y, _pose.z, _pose.yaw, _pose.s) end)
    end
    stop("pod_rumble")
    _phase, _t = "idle", 0
    _world, _inst, _pose, _from = nil, nil, nil, nil
end

function Pod.dispose()
    Pod.reset()
    for _, s in pairs(_sfx) do if s then pcall(function() s:Dispose() end) end end
    _sfx = {}
end

local function rattle(k)
    if _inst == nil or _pose == nil then return end
    local jx = (math.random() - 0.5) * 2 * SHAKE_AMP * k
    local jz = (math.random() - 0.5) * 2 * SHAKE_AMP * k
    local jy = math.random() * SHAKE_AMP * 0.5 * k
    local jyaw = (math.random() - 0.5) * 2 * SHAKE_YAW * k
    pcall(function() _inst:setTransform(_pose.x + jx, _pose.y + jy, _pose.z + jz, _pose.yaw + jyaw, _pose.s) end)
end

local function settle()
    if _inst == nil or _pose == nil then return end
    pcall(function() _inst:setTransform(_pose.x, _pose.y, _pose.z, _pose.yaw, _pose.s) end)
end

-- returns nil while running, "go" once the fade is complete
function Pod.update(dt)
    if _phase == "idle" then return nil end
    _t = _t + dt
    local ps = _world and _world.particles or nil

    if _phase == "enter" then
        if _t >= ENTER_SEC then
            _phase, _t = "shake", 0
            play("pod_rumble")
        end
    elseif _phase == "shake" then
        local k = math.min(1, _t / 0.4) * (1 - 0.35 * (_t / SHAKE_SEC))
        rattle(k)
        _sparkAcc = _sparkAcc + dt
        if ps and _sparkAcc >= 0.09 then
            _sparkAcc = 0
            ps:burst("embers", _pose.x, _pose.y + 0.5 + math.random() * 0.9, _pose.z, 5, { speed = 2.6 })
        end
        if _t >= SHAKE_SEC then
            settle()
            stop("pod_rumble")
            _phase, _t = "rest", 0
        end
    elseif _phase == "rest" then
        if _t >= REST_SEC then
            _phase, _t = "smoke", 0
            play("pod_hiss")
            if ps then
                ps:burst("smoke", _pose.x, _pose.y + 1.1, _pose.z, 30, { size = 0.9, life = 1.7 })
                ps:burst("smoke", _pose.x, _pose.y + 0.4, _pose.z, 14, { size = 1.1, life = 2.0 })
            end
        end
    elseif _phase == "smoke" then
        if ps and _t < 0.5 and math.random() < 0.5 then
            ps:burst("smoke", _pose.x, _pose.y + 1.0, _pose.z, 3, { size = 0.8, life = 1.5 })
        end
        if _t >= SMOKE_SEC then _phase, _t = "fade", 0 end
    elseif _phase == "fade" then
        if _t >= FADE_SEC then return "go" end
    end
    return nil
end

-- where Script.lua draws the player during the sequence: x, z, scale — or nil once inside
function Pod.playerVisual()
    if _phase == "idle" or _from == nil then return nil end
    if _phase ~= "enter" then return nil end
    local k = math.min(1, _t / ENTER_SEC)
    local e = k * k * (3 - 2 * k)
    return _from.x + (_pose.x - _from.x) * e, _from.z + (_pose.z - _from.z) * e, 1 - 0.85 * e
end

-- the black cover for the last phase (drawn over the blitted world)
function Pod.draw(hud, sw, sh)
    if _phase ~= "fade" then return end
    local a = math.floor(255 * math.min(1, _t / FADE_SEC))
    hud:rect(0, 0, sw, sh, 0, 0, 0, a)
end

return Pod
