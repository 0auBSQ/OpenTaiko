---@diagnostic disable: lowercase-global
-- UI.SimpleUI — a minimalist UI library: buttons, labels, toggles, sliders, text fields, panels, bubbles,
-- menus, choosers, settings lists and a coin counter, driven by the mouse, the keyboard and the pad with one
-- shared highlight. Global modules take the library the skin chose through UI.Pick (SimpleUI by default).
--
-- Quick start (in a stage Script.lua):
--   local UI = require("UI.Pick")()             -- or require("UI.SimpleUI") for this one
--   local ui
--   function onStart()
--       ui = UI.new{ bg = true, sfx = { hover = SOUND:CreateSFX("Sounds/Move.ogg"),
--                                       click = SOUND:CreateSFX("Sounds/Decide.ogg") }, navPlayer = 1 }
--       ui:button{ text = "Play", x = 810, y = 460, onClick = function() print("hi") end }
--       ui:toggle{ text = "Fullscreen", x = 120, y = 200, value = true, onChange = function(v) end }
--   end
--   function update(ts) if ui:update(ts) == "cancel" then return Exit("stage", "_title") end end
--   function draw() ui:draw() end
--
-- Everything is theme-customizable: pass `theme = { colors = { primary = {…} }, radius = 12, … }` to
-- new (merged over the library's default theme), or `style = {…}` per widget. Hover (mouse) and focus
-- (keyboard/gamepad: arrows + Decide/Cancel) share one highlight, so both feel identical.
--
-- SimpleUI.derive{ Theme = <theme module>, Coins = <coins widget class>, name = "..." } makes another library
-- with the same widgets, manager, focus and input and its own look (OWM's PopUI is SimpleUI + Bubblegum).

local SimpleUI = {}

SimpleUI.name    = "UI.SimpleUI"
SimpleUI.Theme   = require("UI.SimpleUI.theme")
SimpleUI.Shape   = require("UI.SimpleUI.shape")
SimpleUI.Util    = require("UI.SimpleUI.util")
SimpleUI.Widget  = require("UI.SimpleUI.widget")
SimpleUI.Manager = require("UI.SimpleUI.manager")
SimpleUI.Cache   = require("UI.SimpleUI.cache")
SimpleUI.Sfx     = require("UI.SimpleUI.sfx")

-- widget classes (for advanced users who want to subclass)
SimpleUI.Button  = require("UI.SimpleUI.widgets.button")
SimpleUI.Label   = require("UI.SimpleUI.widgets.label")
SimpleUI.Toggle  = require("UI.SimpleUI.widgets.toggle")
SimpleUI.Slider  = require("UI.SimpleUI.widgets.slider")
SimpleUI.TextBox = require("UI.SimpleUI.widgets.textbox")
SimpleUI.Panel   = require("UI.SimpleUI.widgets.panel")
SimpleUI.Bubble  = require("UI.SimpleUI.widgets.bubble")
SimpleUI.Menu    = require("UI.SimpleUI.widgets.menu")
SimpleUI.Chooser = require("UI.SimpleUI.widgets.chooser")
SimpleUI.SettingsList = require("UI.SimpleUI.widgets.settingslist")
SimpleUI.ListBase = require("UI.SimpleUI.widgets.listbase")
SimpleUI.Coins   = require("UI.SimpleUI.widgets.coins")

--- Create a UI manager. opts = { theme = {…}?, sfx = { hover, click, move, toggle, error }?, bg = bool?,
--- navPlayer = n?, nav = "spatial"? (the arrow keys move to the nearest widget in their direction) }
function SimpleUI.new(opts) return SimpleUI.Manager.new(opts, SimpleUI) end

--- Dispose + drop the shared font caches (call when the language — and thus the main font — changes)
function SimpleUI.flushSharedFonts() return SimpleUI.Manager.flushSharedFonts() end

--- A library with SimpleUI's behaviour and another look: spec = { Theme = { DEFAULT = {…}, … }, Coins = class?, name = "..."? }
function SimpleUI.derive(spec)
    local L = {}
    for k, v in pairs(SimpleUI) do L[k] = v end
    if spec.Theme then L.Theme = spec.Theme end
    if spec.Coins then L.Coins = spec.Coins end
    L.name = spec.name or L.name
    function L.new(opts) return L.Manager.new(opts, L) end
    return L
end

return SimpleUI
