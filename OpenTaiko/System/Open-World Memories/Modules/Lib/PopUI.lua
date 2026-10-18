---@diagnostic disable: lowercase-global
-- Quick start (in a stage Script.lua):
--   local PopUI = require("PopUI")
--   local ui
--   function onStart()
--       ui = PopUI.new{ bg = true, sfx = { hover = SOUND:CreateSFX("Sounds/Move.ogg"),
--                                          click = SOUND:CreateSFX("Sounds/Decide.ogg") }, navPlayer = 1 }
--       ui:button{ text = "Play", x = 810, y = 460, onClick = function() print("hi") end }
--       ui:toggle{ text = "Fullscreen", x = 120, y = 200, value = true, onChange = function(v) end }
--   end
--   function update(ts) if ui:update(ts) == "cancel" then return Exit("stage", "_title") end end
--   function draw() ui:draw() end
--
-- Everything is theme-customizable: pass `theme = { colors = { primary = {…} }, radius = 30, … }` to
-- PopUI.new (merged over the default "Bubblegum" palette), or `style = {…}` per widget. Hover (mouse) and
-- focus (keyboard/gamepad: arrows + Decide/Cancel) share one highlight, so both feel identical.
--
-- PopUI is the global UI.SimpleUI (the widgets, the manager, focus and input) with OWM's Bubblegum theme
-- (PopUI/theme.lua) and CoinBox's look for ui:coins (PopUI/widgets/coins.lua). OWM's _boot names it the
-- skin's UI library, so global modules taking theirs through UI.Pick build on it too.

local SimpleUI = require("UI.SimpleUI")

return SimpleUI.derive{
    name  = "PopUI",
    Theme = require("PopUI.theme"),
    Coins = require("PopUI.widgets.coins"),
}
