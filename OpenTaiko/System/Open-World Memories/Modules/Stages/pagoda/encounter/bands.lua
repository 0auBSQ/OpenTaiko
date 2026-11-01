-- Picks an encounter's background band from the level and the run context.
-- Band modules implement:
--   keys(level, ctx, variant) -> manifest keys to load for the visit
--   open(level, ctx, variant), close()
--   sky() -> grey, visible
--   castAlpha(), castFilter() -> r, g, b, groundDust() -> r, g, b
--   update(dt)
--   drawBehind(ox, oy), drawBack(ox, oy), drawMid(ox, oy), drawFront(ox, oy)
--   optional monsterAlpha() -> monster body opacity (1 when absent)
-- ox, oy: optional screen-shake offset (view.shake keeps the last one).

local forest     = require("encounter/bands/forest")
local pagoda     = require("encounter/bands/pagoda")
local checkpoint = require("encounter/bands/checkpoint")
local sky        = require("encounter/bands/sky")

local M = {}

local ALL = { forest, pagoda, checkpoint, sky }

-- tex: the stage's resident textures (cloud, shadow, spiral) the bands borrow
function M.setup(tex)
    for _, band in ipairs(ALL) do
        if band.setup ~= nil then band.setup(tex) end
    end
end

-- returns the band module and variant; ctx.tower (or ctx.highest): highest floor shown cleared
function M.forLevel(level, ctx)
    level = level or 1
    if level <= 5 then return forest, "trail" end
    if level <= 10 then return forest, "stair" end
    if level == 15 then
        if ctx ~= nil and (ctx.tower or ctx.highest or 0) >= 15 then return checkpoint, "clear" end
        return checkpoint, "shadow"
    end
    if level <= 20 then return pagoda, "pagoda" end
    return sky, "sky"
end

return M
