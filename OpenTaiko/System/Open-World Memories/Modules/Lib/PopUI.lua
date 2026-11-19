---@diagnostic disable: lowercase-global
-- PopUI: UI.SimpleUI with the Bubblegum theme (PopUI/theme.lua)
-- _boot names it the skin UI library (UI.Pick)

local SimpleUI = require("UI.SimpleUI")

return SimpleUI.derive{
    name  = "PopUI",
    Theme = require("PopUI.theme"),
    Coins = require("PopUI.widgets.coins"),
}
