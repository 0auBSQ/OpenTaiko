-- Songs.Title: a song's menu title and subtitle (.ALTERNATIVETITLE / .ALTERNATIVESUBTITLE)
-- headers before any COURSE:; TITLE and SUBTITLE stay for the play screen
local M = {}

M.KEY = ".ALTERNATIVETITLE"
M.SUB_KEY = ".ALTERNATIVESUBTITLE"

local function custom(node, key)
    if node == nil then return nil end
    local ok, v = pcall(function() return node:GetCustomCommand(key) end)
    if ok and type(v) == "string" and v:match("%S") then return v end
    return nil
end

-- the alternative, or nil
function M.alternative(node) return custom(node, M.KEY) end
function M.alternativeSubtitle(node) return custom(node, M.SUB_KEY) end

-- the alternative, else node.Title / node.Subtitle
function M.of(node)
    if node == nil then return nil end
    return M.alternative(node) or node.Title
end

function M.subtitleOf(node)
    if node == nil then return nil end
    return M.alternativeSubtitle(node) or node.Subtitle
end

return M
