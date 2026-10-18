-- PlayStrings.lua — shared strings a stage sets for the one play it mounts (a loading background, the
-- dan plate's style, ...). The stage calls stamp() right after mounting the chart and clear() once the
-- play is back; readers take a string through get() or forTitle(), which answer "" unless the chart is
-- the stamped one, so strings left by a play the stage never came back from reach no other play.
local M = {}

local KEY = "play_strings_chart"

local function shared(key)
    local ok, v = pcall(function() return SHARED:GetSharedString(key) end)
    return (ok and v) or ""
end

-- the chosen chart's id and title
local function chosen()
    local ok, id, title = pcall(function()
        local node = SONGMOUNT:ChosenSongNode()
        if node == nil then return nil, nil end
        return SONGMOUNT:ChosenUniqueId(), node.Title or ""
    end)
    if not ok or id == nil then return nil end
    return id .. "\n" .. title
end

function M.stamp()
    SHARED:SetSharedString(KEY, chosen() or "")
end

function M.clear()
    SHARED:SetSharedString(KEY, "")
end

-- the shared string key, while the chosen chart is the stamped one
function M.get(key)
    local s = shared(KEY)
    if s == "" or s ~= chosen() then return "" end
    return shared(key)
end

-- the shared string key, for a chart drawn by its title (a dan plate): only the stamped chart's
function M.forTitle(key, title)
    local s = shared(KEY)
    if s == "" or title == nil or s:match("\n(.*)$") ~= title then return "" end
    return shared(key)
end

return M
