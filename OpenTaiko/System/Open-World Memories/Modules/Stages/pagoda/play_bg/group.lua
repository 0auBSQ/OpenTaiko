-- The floor's monsters on the band, placed by place.lua; the notes land on the lead's aim point.

local assets   = require("encounter/assets")
local monsters = require("encounter/monsters")
local body     = require("encounter/monster_body")
local document = require("encounter/document")
local motion   = require("encounter/motion")
local sprite   = require("encounter/sprite")
local place    = require("place")
local Beat     = require("beat")
local Config   = require("Core.Config")
local Layout   = Config.load("Config/layout.json")

local G = {}
G.__index = G

local View = Layout.play_bg.view

local EMPTY = { w = 0, h = 0, fx = 0, fy = 0 }

local _o = {}

local function members(ctx)
    return monsters.forLevel(ctx.level, ctx.monster)
end

-- ctx = { level, monster }; returns the texture keys of the floor's monsters
function G.keys(ctx)
    local list, seen = {}, {}
    local function add(k)
        if k ~= nil and not seen[k] and assets.meta(k) ~= nil then
            seen[k] = true
            list[#list + 1] = k
        end
    end
    for _, m in ipairs(members(ctx)) do
        local meta = assets.meta("m_" .. m.key)
        add("m_" .. m.key)
        if m.kind == "wander" and meta ~= nil then add(meta.mirror or ("m_" .. m.key .. "_m")) end
        if meta ~= nil then add(meta.jump) end
        for _, k in ipairs(document.keys(meta)) do add(k) end
        if m.eyes then add("fx_glint") end
    end
    add("fx_shadow")
    return list
end

-- call once the textures are open; aim points and idle lifts come from the manifest's foes
function G.new(ctx)
    local foes = assets.data("foes") or {}
    local group = members(ctx)
    local metas = {}
    for i, m in ipairs(group) do metas[i] = assets.meta("m_" .. m.key) or EMPTY end
    local x, footY = place(group, metas, foes)
    local bodies = {}
    for i, m in ipairs(group) do
        bodies[i] = body.new(m, x + m.dx, footY + m.dy, ctx.level * 10 + i)
    end
    local lf, lead = foes[group[1].key] or {}, metas[1]
    return setmetatable({ bodies = bodies, beat = Beat.new(),
                          aimX = lf.aim_x or lead.w * 0.5, aimY = lf.aim_y or lead.h * 0.5 }, G)
end

function G:update(dt, state)
    local b = self.bodies
    local lead = b[1]
    if lead == nil then return end
    local period, pos = motion.beat(lead.motion)
    lead:update(self.beat:step(dt, period, pos, state.bpm[0], state.towerFloor))
    for i = 2, #b do b[i]:update(dt) end
end

-- back to front; r, g, b: band filter; eyes: 0..1 glint; shadow: contact shadows of the feet
-- that stand in the band
function G:draw(alpha, r, g, b, eyes, shadow)
    local list = self.bodies
    if shadow then
        for i = #list, 1, -1 do
            if list[i].footY < View.h then list[i]:drawShadow(alpha, 0, 0) end
        end
    end
    for i = #list, 1, -1 do list[i]:draw(alpha, r, g, b, eyes, 0, 0) end
end

-- redraws at opacity a times each member's flash share; the caller sets the white gradient map
function G:drawFlash(a)
    local list = self.bodies
    for i = #list, 1, -1 do list[i]:draw(a * (list[i].m.flash or 1), 1, 1, 1, 0, 0, 0) end
end

-- where the notes land: the lead's aim point this frame, kept in the rows in view
function G:centre()
    local lead = self.bodies[1]
    if lead == nil then return View.foe_x, View.foe_y end
    local p, meta, ax = lead.cp, lead.meta, self.aimX
    if p.mirrored and lead.metaM ~= nil then meta, ax = lead.metaM, lead.metaM.w - ax end
    if meta == nil then return lead:centre() end
    _o.sx, _o.sy, _o.rot, _o.pivot = p.sx, p.sy, p.rot, p.pivot
    local x, y = sprite.point(meta, p.x + p.ox, lead.footY + p.dy + p.oy, _o, ax, self.aimY)
    return x, math.max(0, math.min(View.view_bottom, y))
end

function G:react(big)
    if big and self.bodies[1] ~= nil then self.bodies[1]:notice() end
end

return G
