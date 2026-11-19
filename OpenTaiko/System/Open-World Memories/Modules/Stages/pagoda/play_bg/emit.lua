-- emitter on a copy of an fx/presets.lua preset with fields replaced

local Emitter = require("fx/emitter")
local presets = require("fx/presets")

-- tex: handle or key -> handle map; over: replaced fields; nil if preset missing
return function(name, tex, over)
    local src = presets[name]
    if src == nil or tex == nil then return nil end
    local p = {}
    for k, v in pairs(src) do p[k] = v end
    for k, v in pairs(over or {}) do p[k] = v end
    return Emitter.new(p, tex)
end
