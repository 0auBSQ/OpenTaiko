-- Floor-select tower effects (towerfx/*), drawn only for the slots on screen.
-- "back" draws under the tower art, "front" over it and under the corruption clouds.
-- ctx (filled by the scene each frame): t (scene clock, s), resX, resY, hour, fr/fg/fb (time-of-day filter),
-- roofLit (0..1), top(L) (screen y of slot L's top), lit(L) (0..1, door floor free of corruption), tint(L),
-- checkpoint(L) (cleared checkpoint art shown); draw() adds night (0..1).

local art        = require("towerfx/art")
local forest     = require("towerfx/forest")
local stairs     = require("towerfx/stairs")
local base       = require("towerfx/base")
local doors      = require("towerfx/doors")
local checkpoint = require("towerfx/checkpoint")
local roofs      = require("towerfx/roofs")
local Config     = require("Core.Config")
local Layout     = Config.load("Config/layout.json")

local M = {}

local SLOT_H = Layout.stage.scene.normal.h
local REACH  = Layout.stage.tower_fx.reach      -- how far a slot's effects reach past its edges

local function section(L, ctx)
    if L <= 5 then return forest end
    if L <= 9 then return stairs end
    if L == 10 then return base end
    if L <= 19 then return ctx.checkpoint(L) and checkpoint or doors end
    return roofs
end

function M.setup() art.setup() end
function M.dispose() art.dispose() end

function M.draw(layer, Lmin, Lmax, ctx)
    ctx.night = roofs.nightOf(ctx.hour)
    for L = math.max(0, Lmin), Lmax do
        local top = ctx.top(L)
        if top < ctx.resY + REACH and top + SLOT_H > -REACH then
            local fn = section(L, ctx)[layer]
            if fn ~= nil then fn(L, ctx) end
        end
    end
end

return M
