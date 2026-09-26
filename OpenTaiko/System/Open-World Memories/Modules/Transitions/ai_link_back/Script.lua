---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- ai_link_back: AI battle song select to the title (drawn by Lib/AILink).

local AI = require("AILink")
local anim = AI.new("TRANSITION_AI_OUT", "Disconnecting AItritus...")

FADE_OUT_SECONDS = AI.OUT_SECONDS
FADE_IN_SECONDS  = AI.IN_SECONDS

function fadeOut(t) anim:fadeOut(t) end
function loading(progress, elapsed) anim:loading() end
function fadeIn(t) anim:fadeIn(t) end
function onStart() anim:load() end
function onDestroy() anim:dispose() end
