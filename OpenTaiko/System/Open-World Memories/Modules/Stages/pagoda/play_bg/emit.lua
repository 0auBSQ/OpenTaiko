-- Emitter on a copy of an fx/presets.lua preset with some fields replaced.

local Emitter = require("fx/emitter")
local presets = require("fx/presets")

-- name: the preset; tex: a handle or a key -> handle map; over: fields to replace. nil when missing.
return function(name, tex, over)
    local src = presets[name]
    if src == nil or tex == nil then return nil end
    local p = {}
    for k, v in pairs(src) do p[k] = v end
    for k, v in pairs(over or {}) do p[k] = v end
    return Emitter.new(p, tex)
end
