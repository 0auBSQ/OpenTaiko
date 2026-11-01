---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- settings_iris_back: from the settings back to the title (Lib/SettingsIris), the way in played backwards.

local SI = require("SettingsIris")
local anim = SI.new("back")

FADE_OUT_SECONDS = SI.OUT_SECONDS
FADE_IN_SECONDS  = SI.IN_SECONDS

function fadeOut(t) anim:fadeOut(t) end
function loading(progress, elapsed) anim:loading() end
function fadeIn(t) anim:fadeIn(t) end
function onStart() anim:load() end
function onDestroy() anim:dispose() end
