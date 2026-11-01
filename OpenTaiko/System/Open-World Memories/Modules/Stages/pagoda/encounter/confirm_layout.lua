-- Yes / No confirm panel layout, shared by the encounter and the floor select.

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Look = Layout.encounter.confirm

return {
    PANEL_X = Look.panel.x, PANEL_Y = Look.panel.y,
    QUESTION_Y = Look.question_y, QUESTION_W = Look.question_w,
    BUTTON_Y = Look.button_y,
    BUTTON_X = { Look.button_x[1], Look.button_x[2] },  -- 1 = Yes, 2 = No
    BUTTON_SCALE = Look.button_scale,
    DIM = Look.dim_opacity,             -- backdrop opacity
}
