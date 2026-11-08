-- Title.lua — the title and subtitle a song shows in menus, results and cards. A chart may name itself there
-- with .ALTERNATIVETITLE / .ALTERNATIVESUBTITLE headers (before any COURSE:) while its TITLE and SUBTITLE
-- stay what the play screen and the song loading show.
--   local SongTitle = require("Songs.Title")
--   SongTitle.of(node) / SongTitle.subtitleOf(node)                    -- the alternative, else node.Title / node.Subtitle
--   SongTitle.alternative(node) / SongTitle.alternativeSubtitle(node)  -- the alternative, or nil
local M = {}

M.KEY = ".ALTERNATIVETITLE"
M.SUB_KEY = ".ALTERNATIVESUBTITLE"

local function custom(node, key)
    if node == nil then return nil end
    local ok, v = pcall(function() return node:GetCustomCommand(key) end)
    if ok and type(v) == "string" and v:match("%S") then return v end
    return nil
end

function M.alternative(node) return custom(node, M.KEY) end
function M.alternativeSubtitle(node) return custom(node, M.SUB_KEY) end

function M.of(node)
    if node == nil then return nil end
    return M.alternative(node) or node.Title
end

function M.subtitleOf(node)
    if node == nil then return nil end
    return M.alternativeSubtitle(node) or node.Subtitle
end

return M
