---@diagnostic disable: undefined-global, lowercase-global, need-check-nil
---@-- Util.lua — general math/data structure helpers

local Util = {}

function Util.clamp(v, lo, hi) if v < lo then return lo elseif v > hi then return hi else return v end end
function Util.lerp(a, b, t) return a + (b - a) * t end
function Util.round(v) return math.floor(v + 0.5) end

-- recursive functions need to be defined using local references

-- Clone a C# Dictionary into a Lua table safely
local function cloneTable(t)
    local copy = {}

    -- Get enumerator from the dictionary
    local enumerator = t:GetEnumerator()
    while enumerator:MoveNext() do
        local kvp = enumerator.Current
        local key = kvp.Key
        local value = kvp.Value

        -- Recursively clone if it's another Dictionary
        if value ~= nil and type(value) == "userdata" and value.GetEnumerator then
            copy[key] = cloneTable(value)
        else
            copy[key] = value
        end
    end

    return copy
end
Util.cloneTable = cloneTable

-- Deep copy Lua table
local function deepcopy(o, seen)
    seen = seen or {}
    if o == nil then return nil end
    if seen[o] then return seen[o] end

    local no
    if type(o) == 'table' then
        no = {}
        seen[o] = no

        for k, v in next, o, nil do
            no[deepcopy(k, seen)] = deepcopy(v, seen)
        end
        setmetatable(no, deepcopy(getmetatable(o), seen))
    else -- number, string, boolean, etc
        no = o
    end
    return no
end
Util.deepcopy = deepcopy

-- merge `over` onto a clone of `base` (recursive for sub-tables; scalars replaced whole).
local function merge(base, over)
    if type(over) ~= "table" then return over end
    local out = deepcopy(base)
    for k, v in pairs(over) do
        out[k] = merge(out[k], v)
    end
    return out
end
Util.merge = merge

return Util
