-- VN backgrounds by key (scene.bg, {bg=} lines)

local Config = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local Bg = Layout.story.backgrounds
local BgAnim = Animation.story.backgrounds

local trail = { path = "Textures/encounter/bg/trail.jpg", asset = "bg_trail", level = 1, music = "forest" }

-- no entry: Textures/story/bg/<key>.png, full screen
-- entry: one layer or { layers = { ... } }, bottom to top
-- layer keys:
--   path    file path from the stage folder
--   asset   encounter/assets key, drawn while the encounter holds it
--   x, y    top-left in 1920x1080; unset: full screen
--   scale   default 1
--   scroll  px/s, repeats horizontally
-- entry keys:
--   sky     stage sky grey, 0..1
--   level   encounter level for story/vn_effects
--   music   story/vn_music.lua loop
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
