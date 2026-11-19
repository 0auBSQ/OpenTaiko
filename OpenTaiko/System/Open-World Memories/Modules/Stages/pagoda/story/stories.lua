-- story scene picker for a challenge run start

local M = {}

-- startLevel: chosen checkpoint
-- highest: highest level beaten
-- seenFirst: intro already played on this save
function M.pick(startLevel, highest, seenFirst)
    if not seenFirst        then return require("story/story_first")  end
    if startLevel >= 21     then return require("story/story_roof")   end
    if highest >= 20        then return require("story/story_past20") end
    return require("story/story_next")
end

return M
