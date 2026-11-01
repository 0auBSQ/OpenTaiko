-- Named emitter presets for fx/emitter.lua, from Config/animation.json (fx.presets).

local Config = require("Core.Config")
local Animation = Config.load("Config/animation.json")

local TAU = math.pi * 2

local LEAVES = { "fx_leaf_a", "fx_leaf_b", "fx_leaf_c" }

local TEX = {
    pollen = "fx_mote",
    fireflies = "fx_mote",
    forest_leaves = LEAVES,
    near_leaves = { LEAVES[1], LEAVES[2], LEAVES[3] },
    mist = "fx_mist",
    interior_dust = "fx_mote",
    ceiling_specks = "fx_mote",
    fire_puffs = "fx_puff",
    fire_tongues = "fx_puff",
    embers = "fx_ember",
    dust_puff = "fx_dust",
    divine_shine = "fx_mote",
    divine_dark = "fx_mote",
    door_dark = "fx_puff",
    spores = "fx_mote",
    sky_glints = "fx_glint",
    glitter = "fx_sparkle",
    impact_spark = { "fx_star", "fx_sparkle" },
    guard_smoke = "fx_dust",
}

local presets = Config.plain(Animation.fx.presets)

-- a wave given in rad/s gets the Hz field the emitter reads
local function hz(w, field)
    if w ~= nil and w.rad_per_s ~= nil then w[field], w.rad_per_s = w.rad_per_s / TAU, nil end
end

for name, p in pairs(presets) do
    if type(p) ~= "table" then
        presets[name] = nil
    else
        for k in pairs(p) do
            if type(k) == "string" and k:sub(1, 1) == "_" then p[k] = nil end
        end
        p.tex = TEX[name]
        hz(p.sway, "freq")
        hz(p.turb, "hz")
        hz(p.twinkle, "hz")
        if p.zones ~= nil then
            for i, z in ipairs(p.zones) do
                local r = z.rect
                p.zones[i] = { r[1], r[2], r[3], r[4], p = z.p }
            end
        end
    end
end

return presets
