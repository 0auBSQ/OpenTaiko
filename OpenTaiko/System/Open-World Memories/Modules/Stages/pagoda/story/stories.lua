-- Picks the story scene that opens a challenge run.

local M = {}

-- seenFirst: the intro has already played once on this save
-- startLevel: the checkpoint the player chose
-- highest: highest level beaten
function M.pick(startLevel, highest, seenFirst)
    if not seenFirst        then return require("story/story_first")  end
    if startLevel >= 21     then return require("story/story_roof")   end
    if highest >= 20        then return require("story/story_past20") end
    return require("story/story_next")
end

return M
