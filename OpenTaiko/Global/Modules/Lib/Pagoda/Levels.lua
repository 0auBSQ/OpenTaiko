-- Pagoda.Levels: lookups derived from a floor number
-- pure: no screen or run state

local HERE = require("Core.Here")(...)

local M = {}

-- ==== Song pools (pools.json) ====
-- per level from 1: { blue, green, red, purple } lists of { id (song unique id), diff }
-- get_pool falls back to lower levels

M.COLORS = { "blue", "green", "red", "purple" }

local _pools_cache = nil

-- dropped after a song re-enumeration
function M.clear_pools() _pools_cache = nil end

-- NLua: no pairs() on C# Dictionary; GetEnumerator instead
local function _dict_iter(d)
    local e = d:GetEnumerator()
    return function()
        if e:MoveNext() then return e.Current.Key, e.Current.Value end
    end
end

function M.load_pools()
    if _pools_cache ~= nil then return _pools_cache end
    local raw = JSONLOADER:JsonParseFileAny((HERE or "") .. "pools.json")

    -- entry { "id": ..., "diff": N }
    local function parse_entry(e_obj)
        local id, diff = nil, 0
        for k, v in _dict_iter(e_obj) do
            if k == "id"   then id   = v end
            if k == "diff" then diff = tonumber(v) or 0 end
        end
        return id ~= nil and { id = id, diff = diff } or nil
    end

    -- pool { blue, green, red, purple } arrays
    local function parse_pool(raw_pool)
        local pool = {}
        for _, color in ipairs(M.COLORS) do
            pool[color] = {}
            local raw_entries = nil
            for k, v in _dict_iter(raw_pool) do
                if k == color then raw_entries = v ; break end
            end
            if raw_entries ~= nil then
                for _, entry_obj in _dict_iter(raw_entries) do
                    local entry = parse_entry(entry_obj)
                    if entry ~= nil then table.insert(pool[color], entry) end
                end
            end
        end
        return pool
    end

    _pools_cache = {}
    if raw == nil then return _pools_cache end
    for k, v in _dict_iter(raw) do
        local lv = tonumber(k)
        if lv ~= nil then
            _pools_cache[lv] = parse_pool(v)
        end
    end
    return _pools_cache
end

function M.get_pool(level)
    local pools = M.load_pools()
    local lv = level
    while lv >= 1 do
        if pools[lv] ~= nil then return pools[lv] end
        lv = lv - 1
    end
    return pools[1]  -- fallback
end

-- ==== Level helpers ====

local KANJI_NUMS = { "一","二","三","四","五","六","七","八","九","十" }

-- 道一..道十 (1-10), 扉一..扉十 (11-20), 穹天1.. (21+)
-- same kanji in every language: they are the dan plate and dan title
function M.level_name(level)
    if level >= 1 and level <= 10 then
        return "道" .. (KANJI_NUMS[level] or tostring(level))
    elseif level >= 11 and level <= 20 then
        return "扉" .. (KANJI_NUMS[level - 10] or tostring(level - 10))
    else
        return string.format("穹天%d", level - 20)
    end
end

-- the dan plate's frame: 0 for levels 1-4, 1 for 5-9, 2 for 10-14, 3 for 15-19, 4 from 20
function M.level_tick(level)
    if     level <= 4  then return 0
    elseif level <= 9  then return 1
    elseif level <= 14 then return 2
    elseif level <= 19 then return 3
    end
    return 4
end

local function _hsv_to_rgb(h, s, v)
    -- h in [0, 360], s and v in [0, 1]; returns r, g, b in [0, 255]
    local hi = math.floor(h / 60) % 6
    local f  = h / 60 - math.floor(h / 60)
    local p  = v * (1 - s)
    local q  = v * (1 - f * s)
    local t  = v * (1 - (1 - f) * s)
    local r, g, b
    if     hi == 0 then r, g, b = v, t, p
    elseif hi == 1 then r, g, b = q, v, p
    elseif hi == 2 then r, g, b = p, v, t
    elseif hi == 3 then r, g, b = p, q, v
    elseif hi == 4 then r, g, b = t, p, v
    else                r, g, b = v, p, q end
    return math.floor(r * 255), math.floor(g * 255), math.floor(b * 255)
end

function M.level_tick_color(level)
    if level <= 10 then
        return { 139, 69, 19 }       -- 道
    elseif level <= 20 then
        -- 扉: hue 240 (扉一) to 0 (扉八), then 325 and 290
        local d = level - 10             -- door index 1..10
        local h
        if d <= 8 then
            local t = (d - 1) / 7        -- 0.0 at 扉一, 1.0 at 扉八
            h = 240 * (1 - t)^1.4        -- 240° to 0°
        else
            h = 360 - (d - 8) * 35       -- 扉九 325°, 扉十 290°
        end
        local r, g, b = _hsv_to_rgb(h, 0.88, 1.0)
        return { r, g, b }
    else
        return { 0, 0, 0 }           -- 穹天
    end
end

-- exam 1: gauge thresholds (red / gold), range More
function M.exam1_gauge(level)
    if level <= 5 then
        return 70, 80
    elseif level <= 10 then
        return 80, 90
    elseif level <= 20 then
        local n = level - 11
        return math.min(100, 90 + n), math.min(100, 95 + n)
    else
        return 100, 100
    end
end

-- exam 2: accuracy % (levels 1-20, More) or Ok count (21+, Less)
-- returns type, red, gold, lessThan
function M.exam2(level)
    if level <= 20 then
        local t = {
            -- 道 1-10 (accuracy resets at 8 with the Hard band)
            [1]={78,88},  [2]={80,90},  [3]={84,92},  [4]={86,93},  [5]={88,94},
            [6]={90,95},  [7]={92,96},  [8]={70,85},  [9]={72,86},  [10]={75,87},
            -- 扉 11-20
            [11]={80,90}, [12]={84,92}, [13]={88,94}, [14]={90,95}, [15]={92,96},
            [16]={94,97}, [17]={95,97}, [18]={96,98}, [19]={97,98}, [20]={98,99},
        }
        local e = t[level] or {80, 90}
        return "a", e[1], e[2], false
    elseif level == 21 then return "jg", 50, 25, true
    elseif level == 22 then return "jg", 40, 20, true
    elseif level == 23 then return "jg", 30, 15, true
    elseif level == 24 then return "jg", 20, 10, true
    elseif level == 25 then return "jg", 15,  8, true
    elseif level == 26 then return "jg", 10,  5, true
    else
        -- level 27+: red -1/level, gold -1/2 levels, both floor at 1
        local n    = level - 27
        local red  = math.max(1, 10 - n)
        local gold = math.max(1, 5 - math.floor(n / 2))
        return "jg", red, gold, true
    end
end

-- exam 3: Bad count base (red / gold); x1.25 (floor) when purple is picked
function M.exam3_base(level)
    if level <= 20 then
        local t = {
            -- 道 1-10
            [1]={32,22}, [2]={30,20}, [3]={28,19}, [4]={27,18}, [5]={26,18},
            [6]={25,18}, [7]={25,17}, [8]={24,17}, [9]={23,16}, [10]={22,16},
            -- 扉 11-20
            [11]={20,15}, [12]={16,12}, [13]={12,8}, [14]={10,7}, [15]={9,6},
            [16]={8,6},   [17]={7,5},   [18]={6,4},  [19]={5,3},  [20]={4,2},
        }
        local e = t[level] or {4, 2}
        return e[1], e[2]
    elseif level <= 23 then return 3, 1
    elseif level <= 26 then return 2, 1
    else                    return 1, 1
    end
end


function M.purple_prob(level)
    if     level <= 19 then return 0.10
    elseif level == 20 then return 1.00   -- 扉十 (last door): guaranteed purple
    elseif level <= 26 then return 0.10
    else
        -- +10% per level from 27
        return math.max(0.0, math.min(1.0, (level - 27) * 0.10))
    end
end

function M.pick_entry(entries, used_ids)
    for _ = 1, 20 do
        local e = entries[math.random(#entries)]
        if not used_ids[e.id] then return e end
    end
    return entries[math.random(#entries)]   -- fallback: allow repeat
end

function M.shuffle(t)
    for i = #t, 2, -1 do
        local j = math.random(i)
        t[i], t[j] = t[j], t[i]
    end
end

return M
