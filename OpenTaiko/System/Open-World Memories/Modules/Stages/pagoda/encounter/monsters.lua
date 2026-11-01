-- Monsters per floor; keys are art file stems, held in the manifest as "m_" .. key.

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

M.DAI_POOL = { "norm1", "norm2", "norm3", "norm4b", "critter" }

local Look = Layout.encounter.monsters
local Anim = Animation.encounter.monsters
local Idle = Anim.idle
local RUN_FLOORS = 4         -- floors whose monsters a run rolls
local FOOT_Y  = Look.foot_y
local HOME_X  = Look.home_x
local RIGHT_X = Look.right_x         -- every member's right half stays left of this
local DOOR_GLOW = Look.door_glow_opacity       -- glow opacity of the doors' monsters (floors 11+)
local SOFT_FLASH = Look.soft_flash_share       -- share of the play screen's flash for filled bodies

-- a "counter" idle's params from its config entry
local function counter(c)
    return { P = c.period_s, A = c.amp }
end

-- floors 1-10
local DAI = {
    norm1       = { kind = "star" },
    norm2       = { kind = "wander" },
    norm4b      = { kind = "wander" },
    critter     = { kind = "wander" },
    scare3      = { kind = "wander" },
    norm3       = { kind = "counter", params = counter(Idle.norm3) },
    norm5       = { kind = "counter", params = counter(Idle.norm5) },
    pagodaguard = { kind = "counter", params = counter(Idle.pagodaguard) },
    scare2      = { kind = "counter", params = counter(Idle.scare2) },
    scare1      = { kind = "float",   params = { hover = Idle.scare1.hover_px, tilt = Idle.scare1.tilt_deg } },
}

-- floors 5-10, members front to back: dx, dy = feet offset from the lead's; tempo = idle clock scale; params replace the monster's
local PAIR_RANGE = { range = Anim.pair.range_px }
local FIXED_DAI = {
    [5]  = { { key = "norm5" } },
    [6]  = { { key = "norm4b", params = PAIR_RANGE },
             { key = "critter", dx = Look.pair.critter_dx, dy = Look.pair.critter_dy, tempo = Anim.pair.critter_tempo,
               params = PAIR_RANGE } },
    [7]  = { { key = "scare1" } },
    [8]  = { { key = "scare2" } },
    [9]  = { { key = "scare3" } },
    [10] = { { key = "pagodaguard" } },
}

-- floors 11-20 and the roof
local DOORS = {
    [11] = { key = "monster1", kind = "float", flash = SOFT_FLASH },
    [12] = { key = "monster2", kind = "float" },
    [13] = { key = "monster3", kind = "float" },
    [14] = { key = "monster4", kind = "counter", params = counter(Idle.monster4), flash = SOFT_FLASH },
    [15] = { key = "monster5", kind = "erratic", params = { float = true }, eyes = true, flash = SOFT_FLASH },
    [16] = { key = "monster6", kind = "counter", params = counter(Idle.monster6) },
    [17] = { key = "monster7", kind = "counter", params = counter(Idle.monster7), flash = SOFT_FLASH },
    [18] = { key = "monster8", kind = "float", flash = SOFT_FLASH },
    [19] = { key = "monster9", kind = "counter", params = counter(Idle.monster9), flash = SOFT_FLASH },
    [20] = { key = "monsterfinal", kind = "float", params = { mul = Idle.monsterfinal.hover_mul }, aura = "divine" },
}
local ROOF = { key = "document", kind = "float" }        -- drawn in parts (encounter/document.lua)

-- level -> pool key for floors 1-4 of a new run: min(RUN_FLOORS, #pool) picks without repeats
function M.rollRun()
    local pool, run = {}, {}
    for i, k in ipairs(M.DAI_POOL) do pool[i] = k end
    for level = 1, math.min(RUN_FLOORS, #pool) do
        local i = math.random(level, #pool)
        pool[level], pool[i] = pool[i], pool[level]
        run[level] = pool[level]
    end
    return run
end

-- one member of a floor's group; place = its { dx, dy, tempo, params } (all optional)
local function member(level, key, def, place)
    return {
        key = key,
        kind = def.kind,
        params = place.params or def.params or {},
        aura = def.aura or ((level >= 11 and level <= 19) and "dark" or nil),
        glow = (level >= 11) and DOOR_GLOW or 0,
        flash = def.flash or 1,
        eyes = def.eyes == true,
        dissolve = DOORS[level] ~= nil,
        dx = place.dx or 0,
        dy = place.dy or 0,
        tempo = place.tempo or 1,
    }
end

-- the floor's monsters, front to back, each { key, kind, params, aura, glow, flash, eyes, dissolve, dx, dy, tempo };
-- picked = the run's roll (floors 1-4); flash = share of the play screen's flash a landing gives it
function M.forLevel(level, picked)
    level = math.max(1, math.floor(level or 1))
    local group = {}
    if level <= RUN_FLOORS then
        local key = (picked ~= nil and DAI[picked] ~= nil) and picked or M.DAI_POOL[1]
        group[1] = member(level, key, DAI[key], {})
    elseif level <= 10 then
        for i, p in ipairs(FIXED_DAI[level]) do group[i] = member(level, p.key, DAI[p.key], p) end
    else
        local def = DOORS[level] or ROOF
        group[1] = member(level, def.key, def, {})
    end
    return group
end

-- bestiary

-- every monster, in climb order
M.ORDER = { "norm1", "norm2", "norm3", "norm4b", "critter", "norm5", "scare1", "scare2", "scare3",
            "pagodaguard", "monster1", "monster2", "monster3", "monster4", "monster5", "monster6",
            "monster7", "monster8", "monster9", "monsterfinal", ROOF.key }

-- the floors a monster stands on: a sorted list of levels, plus true when it stands on the roofs (21+)
function M.floors(key)
    local list, roof = {}, ROOF.key == key
    for level = 1, 20 do
        local found = false
        if level <= RUN_FLOORS then
            for _, k in ipairs(M.DAI_POOL) do if k == key then found = true end end
        elseif level <= 10 then
            for _, p in ipairs(FIXED_DAI[level]) do if p.key == key then found = true end end
        elseif DOORS[level] ~= nil and DOORS[level].key == key then
            found = true
        end
        if found then list[#list + 1] = level end
    end
    return list, roof
end

-- monsters a cleared or lost floor counts, front to back
function M.keysForLevel(level, picked)
    local keys = {}
    for i, m in ipairs(M.forLevel(level, picked)) do keys[i] = m.key end
    return keys
end

-- one monster alone, without its floor's aura, glow or dissolve (the bestiary)
function M.member(key)
    local def = DAI[key]
    if def == nil then
        for _, d in pairs(DOORS) do if d.key == key then def = d end end
    end
    if def == nil and ROOF.key == key then def = ROOF end
    if def == nil then return nil end
    return {
        key = key, kind = def.kind, params = def.params or {},
        aura = nil, glow = 0, flash = 1, eyes = def.eyes == true, dissolve = false,
        dx = 0, dy = 0, tempo = 1,
    }
end

-- the lead's feet x: HOME_X, moved left until every member's right half is left of RIGHT_X;
-- metas = the members' sprite entries, in group order
function M.place(group, metas)
    local x = HOME_X
    for i, m in ipairs(group) do
        local w = (metas[i] ~= nil and metas[i].w) or 0
        x = math.min(x, RIGHT_X - w / 2 - m.dx)
    end
    return x, FOOT_Y
end

return M
