-- uilib.lua — the UI library My Room's screens are built with: the one the skin chose through UI.Pick
-- (OWM's PopUI, SimpleUI under a skin that names none). It is picked on first use, not when the stage
-- loads: a skin names its library in its _boot's onStart, which runs after every module has loaded.
--   local UI = require("uilib")      -- then UI.new{...}, UI.Menu, UI.Widget, UI.Shape, UI.Util as with SimpleUI

local Pick = require("UI.Pick")

local lib = nil

return setmetatable({}, { __index = function(_, k)
    lib = lib or Pick()
    return lib[k]
end })
