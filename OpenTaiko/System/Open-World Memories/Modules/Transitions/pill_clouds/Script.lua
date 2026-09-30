---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- pill_clouds: from the title into the regular song select (Lib/PillClouds).

local PC = require("PillClouds")
local anim = PC.new({ { 1.00, 0.80, 0.60 }, { 0.60, 0.90, 1.00 }, { 1.00, 0.80, 0.60 }, { 0.60, 0.90, 1.00 }, })

FADE_OUT_SECONDS = PC.PILLS_OUT_SECONDS
FADE_IN_SECONDS  = PC.IN_SECONDS

function fadeOut(t) anim:fadeOut(t) end
function loading(progress, elapsed) anim:loading() end
function fadeIn(t) anim:fadeIn(t) end
function onStart() anim:load() end
function onDestroy() anim:dispose() end
