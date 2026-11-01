---@diagnostic disable: undefined-global, lowercase-global
-- UI.Pick — the UI library a module builds its screens with:
--   local UI = require("UI.Pick")(name)     -- name optional; then UI.new{...}, ui:button{...}, as with SimpleUI
-- The library is name (a require name, e.g. "PopUI") when given, else the shared string "ui_library" a skin
-- sets once (OWM's _boot sets "PopUI"), else "UI.SimpleUI". A library that does not load is replaced by
-- UI.SimpleUI, so a name another skin left in the shared string costs nothing.

local DEFAULT = "UI.SimpleUI"

return function(name)
    local want = name
    if type(want) ~= "string" or want == "" then
        local ok, s = pcall(function() return SHARED:GetSharedString("ui_library") end)
        want = (ok and type(s) == "string" and s ~= "") and s or DEFAULT
    end
    if want ~= DEFAULT then
        local ok, lib = pcall(require, want)
        if ok and type(lib) == "table" and type(lib.new) == "function" then return lib end
    end
    return require(DEFAULT)
end
