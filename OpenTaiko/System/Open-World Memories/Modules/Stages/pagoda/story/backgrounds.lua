-- VN backgrounds by key, for scene.bg and {bg=} lines.
-- A key with no entry loads Textures/story/bg/<key>.png stretched over the screen.
-- An entry is one layer or { layers = { ... } }, bottom to top. Layer keys:
--   path    image file, relative to the stage folder
--   asset   encounter/assets key of the same file, drawn while the encounter holds it
--   x, y    top-left in 1920x1080 space; without them the layer is stretched over the screen
--   scale   draw scale of a placed layer (default 1)
--   scroll  horizontal scroll, px/s; the layer repeats across the screen
-- Entry keys: sky = grey (0..1) of the stage sky shown through it; level = the encounter level whose
-- effects play over it (story/vn_effects); music = the loop played while it shows (story/vn_music.lua).

local Config = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local Bg = Layout.story.backgrounds
local BgAnim = Animation.story.backgrounds

local trail = { path = "Textures/encounter/bg/trail.jpg", asset = "bg_trail", level = 1, music = "forest" }

return {
    forest = trail,
    fox_village = trail,
    gate = trail,
    dojo = { path = "Textures/story/bg/dojo.png", music = "dojo" },
    stairs = { path = "Textures/encounter/bg/stair.png", asset = "bg_stair", sky = Bg.stairs.sky_grey, level = 6 },
    pagoda_front = { path = "Textures/story/bg/pagoda_front.jpg" },
    pagoda_interior = { path = "Textures/encounter/bg/pagoda.jpg", asset = "bg_pagoda", level = 11 },
    checkpoint = { path = "Textures/encounter/bg/checkpoint_clear.png", asset = "bg_checkpoint_clear", level = 15 },
    sky = { path = "Textures/encounter/bg/sky_clear.png", asset = "bg_sky_clear",
            x = Bg.sky.x, y = Bg.sky.y, sky = Bg.sky.sky_grey, level = 21 },
    beanstalk = {
        layers = {
            { path = "Textures/story/bg/beanstalk/sky.jpg" },
            { path = "Textures/story/bg/beanstalk/bean.png", x = Bg.beanstalk.bean.x, y = Bg.beanstalk.bean.y },
            { path = "Textures/story/bg/beanstalk/cloud.png", x = Bg.beanstalk.cloud.x, y = Bg.beanstalk.cloud.y,
              scale = Bg.beanstalk.cloud.scale, scroll = BgAnim.beanstalk.cloud.scroll_px_per_s },
        },
    },
}
