---@diagnostic disable: undefined-global, lowercase-global
-- SimpleUI canvas cache: pool by size, baked surfaces shared by key
-- both capped in bytes, oldest freed first

local Shape = require("UI.SimpleUI.shape")

local C = {}

local canvasPool, canvasPoolBytes = {}, 0
local CANVAS_POOL_MAX_BYTES = 24 * 1024 * 1024

function C.releaseCanvas(cv)
    if cv == nil then return end
    local w, h = cv.Width, cv.Height
    local bytes = w * h * 4
    if canvasPoolBytes + bytes > CANVAS_POOL_MAX_BYTES then pcall(function() cv:Dispose() end); return end
    local key = w * 65536 + h
    local list = canvasPool[key]
    if list == nil then list = {}; canvasPool[key] = list end
    list[#list + 1] = cv
    canvasPoolBytes = canvasPoolBytes + bytes
end

function C.acquireCanvas(w, h)
    local list = canvasPool[w * 65536 + h]
    if list ~= nil and #list > 0 then
        local cv = table.remove(list)
        canvasPoolBytes = canvasPoolBytes - w * h * 4
        cv:ClearTransparent()
        return cv
    end
    return CANVAS:CreateCanvas(w, h)
end

function C.flushCanvasPool()
    for _, list in pairs(canvasPool) do
        for _, cv in ipairs(list) do pcall(function() cv:Dispose() end) end
    end
    canvasPool, canvasPoolBytes = {}, 0
end

local baked = {}                  -- key -> { canvas, refs, bytes }
local parkedKeys, parkedBytes = {}, 0
local BAKED_PARK_MAX_BYTES = 32 * 1024 * 1024

local function unpark(key)
    for i, k in ipairs(parkedKeys) do
        if k == key then table.remove(parkedKeys, i); break end
    end
end

-- shared canvas for `key`, baked by bake(cv) on first use
-- uploaded once with no CPU copy
function C.bakedCanvas(key, w, h, bake)
    local e = baked[key]
    if e ~= nil then
        if e.refs == 0 then unpark(key); parkedBytes = parkedBytes - e.bytes end
        e.refs = e.refs + 1
        return e.canvas
    end
    local cv = C.acquireCanvas(w, h)
    bake(cv)
    Shape.upload(cv, true)
    baked[key] = { canvas = cv, refs = 1, bytes = w * h * 4 }
    return cv
end

function C.releaseBaked(key)
    local e = baked[key]
    if e == nil or e.refs <= 0 then return end
    e.refs = e.refs - 1
    if e.refs > 0 then return end
    parkedKeys[#parkedKeys + 1] = key
    parkedBytes = parkedBytes + e.bytes
    while parkedBytes > BAKED_PARK_MAX_BYTES and #parkedKeys > 0 do
        local old = table.remove(parkedKeys, 1)
        local oe = baked[old]
        baked[old] = nil
        if oe then
            parkedBytes = parkedBytes - oe.bytes
            C.releaseCanvas(oe.canvas)
        end
    end
end

-- frees parked surfaces (live ones stay)
function C.flushBakedCanvases()
    for k, e in pairs(baked) do
        if e.refs <= 0 then pcall(function() e.canvas:Dispose() end); baked[k] = nil end
    end
    parkedKeys, parkedBytes = {}, 0
end

-- bake key of a resolved theme, cached per theme table
local themeKeys = setmetatable({}, { __mode = "k" })
local function serialize(v, out)
    local t = type(v)
    if t == "number" then out[#out + 1] = string.format("%g", v)
    elseif t == "boolean" then out[#out + 1] = v and "T" or "F"
    elseif t == "string" then out[#out + 1] = v
    elseif t == "table" then
        local keys = {}
        for k in pairs(v) do if type(k) == "string" or type(k) == "number" then keys[#keys + 1] = k end end
        table.sort(keys, function(a, b) return tostring(a) < tostring(b) end)
        out[#out + 1] = "{"
        for _, k in ipairs(keys) do
            local x = v[k]
            if type(x) ~= "function" and type(x) ~= "userdata" then
                out[#out + 1] = tostring(k) .. "="; serialize(x, out); out[#out + 1] = ";"
            end
        end
        out[#out + 1] = "}"
    end
end

function C.keyOf(...)
    local out = {}
    for i = 1, select("#", ...) do serialize((select(i, ...)), out); out[#out + 1] = "," end
    return table.concat(out)
end

function C.themeKey(eff)
    local k = themeKeys[eff]
    if k == nil then k = C.keyOf(eff); themeKeys[eff] = k end
    return k
end

return C
