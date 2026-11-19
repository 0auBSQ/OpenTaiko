-- Core.Here: folder of a file loaded with require: local HERE = require("Core.Here")(...)
-- Lib files load their own assets with the *FromAbsolutePath loaders and this folder

-- (modname, path) from require -> folder with a trailing "/", or nil outside require
return function(_, path)
    if type(path) ~= "string" then return nil end
    local p = path:gsub("\\", "/")
    local dir = p:match("^(.*)/[^/]*$")
    return dir and (dir .. "/") or nil
end
