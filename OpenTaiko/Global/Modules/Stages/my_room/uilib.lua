-- My Room UI library, the skin's pick through UI.Pick

local Pick = require("UI.Pick")

local lib = nil

-- picked on first use: the skin names its library in _boot onStart, after modules load
return setmetatable({}, { __index = function(_, k)
    lib = lib or Pick()
    return lib[k]
end })
