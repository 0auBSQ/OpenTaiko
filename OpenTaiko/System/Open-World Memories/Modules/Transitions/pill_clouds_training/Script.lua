---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- pill_clouds_training: from the title into the training song select (Lib/PillClouds).

local PC = require("PillClouds")
-- the pill colours: Config/layout.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local anim = PC.new(Config.plain(Layout.pill_colors))

FADE_OUT_SECONDS = PC.PILLS_OUT_SECONDS
FADE_IN_SECONDS  = PC.IN_SECONDS

function fadeOut(t) anim:fadeOut(t) end
function loading(progress, elapsed) anim:loading() end
function fadeIn(t) anim:fadeIn(t) end
function onStart() anim:load() end
function onDestroy() anim:dispose() end
