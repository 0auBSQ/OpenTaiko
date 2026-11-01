-- Core.Here — the folder of a file loaded with require, from the arguments require gives the file:
--   local HERE = require("Core.Here")(...)      -- e.g. "C:/Game/Global/Modules/Lib/UI/"
-- A Lib file has no folder of its own for TEXTURE, SOUND and the other loaders: they resolve against the
-- module that loaded it. A Lib file that ships its own assets loads them with the *FromAbsolutePath
-- variants and this folder. nil when the file was not loaded through require.

return function(_, path)
    if type(path) ~= "string" then return nil end
    local p = path:gsub("\\", "/")
    local dir = p:match("^(.*)/[^/]*$")
    return dir and (dir .. "/") or nil
end
