---@diagnostic disable: lowercase-global
-- Quick start (in a stage Script.lua):
--   local PopUI = require("PopUI")
--   local ui
--   function onStart()
--       ui = PopUI.new{ bg = true, sfx = { hover = SOUND:CreateSFX("Sounds/Move.ogg"),
--                                          click = SOUND:CreateSFX("Sounds/Decide.ogg") } }
--       ui:button{ text = "Play", x = 810, y = 460, onClick = function() print("hi") end }
--       ui:toggle{ text = "Fullscreen", x = 120, y = 200, value = true, onChange = function(v) end }
--   end
--   function update(ts) if ui:update(ts) == "cancel" then return Exit("stage", "_title") end end
--   function draw() ui:draw() end
--
-- Everything is theme-customizable: pass `theme = { colors = { primary = {…} }, radius = 30, … }` to
-- PopUI.new (merged over the default "Bubblegum" palette), or `style = {…}` per widget. Hover (mouse) and
-- focus (keyboard/gamepad: arrows + Decide/Cancel) share one highlight, so both feel identical.

local PopUI = {}

PopUI.Theme   = require("PopUI.theme")
PopUI.Shape   = require("PopUI.shape")
PopUI.Util    = require("PopUI.util")
PopUI.Widget  = require("PopUI.widget")
PopUI.Manager = require("PopUI.manager")

-- widget classes (for advanced users who want to subclass)
PopUI.Button  = require("PopUI.widgets.button")
PopUI.Label   = require("PopUI.widgets.label")
PopUI.Toggle  = require("PopUI.widgets.toggle")
PopUI.Slider  = require("PopUI.widgets.slider")
PopUI.TextBox = require("PopUI.widgets.textbox")
PopUI.Panel   = require("PopUI.widgets.panel")
PopUI.Bubble  = require("PopUI.widgets.bubble")
PopUI.Menu    = require("PopUI.widgets.menu")
PopUI.Chooser = require("PopUI.widgets.chooser")
PopUI.SettingsList = require("PopUI.widgets.settingslist")

--- Create a UI manager. opts = { theme = {…}?, sfx = { hover, click, move, toggle, error }?, bg = bool? }
function PopUI.new(opts) return PopUI.Manager.new(opts) end

return PopUI
