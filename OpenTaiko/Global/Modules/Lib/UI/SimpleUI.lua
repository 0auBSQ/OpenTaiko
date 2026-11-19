---@diagnostic disable: lowercase-global
-- UI.SimpleUI: widget library with one shared hover/focus highlight

local SimpleUI = {}

SimpleUI.name    = "UI.SimpleUI"
SimpleUI.Theme   = require("UI.SimpleUI.theme")
SimpleUI.Shape   = require("UI.SimpleUI.shape")
SimpleUI.Util    = require("UI.SimpleUI.util")
SimpleUI.Widget  = require("UI.SimpleUI.widget")
SimpleUI.Manager = require("UI.SimpleUI.manager")
SimpleUI.Cache   = require("UI.SimpleUI.cache")
SimpleUI.Sfx     = require("UI.SimpleUI.sfx")

-- widget classes (for subclassing)
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

--- opts = { theme? (merged over the default), sfx = { hover, click, move, toggle, error }?, bg?,
--- navPlayer?, nav = "spatial"? (arrows move to the nearest widget) }
function SimpleUI.new(opts) return SimpleUI.Manager.new(opts, SimpleUI) end

--- dispose the shared font caches (call on a language change)
function SimpleUI.flushSharedFonts() return SimpleUI.Manager.flushSharedFonts() end

--- same widgets with another look: spec = { Theme = { DEFAULT = {…}, … }, Coins = class?, name = "..."? }
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
