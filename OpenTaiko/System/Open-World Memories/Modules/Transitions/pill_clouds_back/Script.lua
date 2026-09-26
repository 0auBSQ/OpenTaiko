---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- pill_clouds_back: out of the song selects, clouds only (Lib/PillClouds).

local PC = require("PillClouds")
local anim = PC.new(nil)

FADE_OUT_SECONDS = PC.CLOUDS_OUT_SECONDS
FADE_IN_SECONDS  = PC.IN_SECONDS

function fadeOut(t) anim:fadeOut(t) end
function loading(progress, elapsed) anim:loading() end
function fadeIn(t) anim:fadeIn(t) end
function onStart() anim:load() end
function onDestroy() anim:dispose() end
