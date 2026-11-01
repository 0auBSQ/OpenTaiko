-- Where a floor's group stands on the band (x as encounter/monsters.place).

local monsters = require("encounter/monsters")
local Config   = require("Core.Config")
local Layout   = Config.load("Config/layout.json")

local View     = Layout.play_bg.view

local NONE = { lo = 0, hi = 0 }

-- group: monsters.forLevel; metas: the members' band sprite entries, in group order; foes:
-- the manifest's foes. Returns the lead's foot point.
return function(group, metas, foes)
    local x = monsters.place(group, metas)
    local lead = metas[1]
    local lf = foes[group[1].key] or NONE
    local fy = lead.fy or lead.h
    local footY = View.foe_y + (fy - (lf.aim_y or lead.h * 0.5)) + (lf.lo + lf.hi) * 0.5
    local top = math.huge
    for i, m in ipairs(group) do
        local f, meta = foes[m.key] or lf, metas[i]
        top = math.min(top, footY + m.dy - f.hi - (meta.fy or meta.h))
    end
    if top > View.top then footY = footY - (top - View.top) end
    return x, footY
end
