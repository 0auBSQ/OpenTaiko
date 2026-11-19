---@diagnostic disable: undefined-global, lowercase-global
-- UI.Pick: returns the UI library a module builds its screens with

local DEFAULT = "UI.SimpleUI"

-- library: name, else the shared string "ui_library", else UI.SimpleUI
-- a library that fails to load falls back to UI.SimpleUI
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
