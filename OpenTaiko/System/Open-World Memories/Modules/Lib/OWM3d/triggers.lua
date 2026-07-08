---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/triggers.lua — trigger volumes + the action system that makes the world interactive:
-- streamed interiors, doors, camera rig changes, teleports, map transitions and custom callbacks.
--
--   world.triggers:add{
--       id = "innDoor", shape = "box", min = {4.6, 0, 6.6}, max = {6.4, 2.2, 8.4},
--       on = "enter" | "leave" | "interact",   once = false,
--       actions = {
--           { type = "toggleGroup", group = "inn", visible = true, solid = true },
--           { type = "rig", rig = "interior" },                   -- camera blend
--           { type = "teleport", to = {x, y, z} },
--           { type = "map", map = "hills", spawn = "fromPlaza" }, -- via world.onMapChange
--           { type = "custom", fn = function(world, trig) ... end },
--       },
--   }
--   Per frame: world.triggers:update(dt, px, py, pz). For prompts: world.triggers:interactHere(px,py,pz)
--   returns the nearest "interact" trigger the player stands in (stage draws the prompt + fires
--   :fire(trig) on the interact key).

local Triggers = {}
Triggers.__index = Triggers

function Triggers.new(world)
    local self = setmetatable({}, Triggers)
    self.world = world
    self.list = {}
    return self
end

function Triggers:add(t)
    t.entered = false
    t.spent = false
    self.list[#self.list + 1] = t
    return t
end

function Triggers:clear()
    -- break the action closures explicitly (they capture map/world state; a lingering reference
    -- through a stale trigger table would pin the whole unloaded map in the Lua heap)
    for _, t in ipairs(self.list) do t.actions = nil end
    self.list = {}
end

local function inside(t, px, py, pz)
    return px >= t.min[1] and px <= t.max[1]
       and py >= t.min[2] and py <= t.max[2]
       and pz >= t.min[3] and pz <= t.max[3]
end
Triggers.inside = inside   -- exposed for the harness

-- re-seed the entered flags from a position WITHOUT firing anything — call after a spawn or
-- teleport so a player materializing inside a volume doesn't fire a phantom enter (and stepping
-- out doesn't fire a phantom leave/unfire).
function Triggers:resetStates(px, py, pz)
    for _, t in ipairs(self.list) do
        t.entered = (px ~= nil) and inside(t, px, py or 0, pz or 0) or false
    end
end

-- run a trigger's actions (also the entry point for "interact" triggers from the stage)
function Triggers:fire(t)
    if t.once and t.spent then return end
    t.spent = true
    local world = self.world
    for _, a in ipairs(t.actions or {}) do
        local kind = a.type
        if kind == "toggleGroup" and a.group then
            world.models:toggleGroup(a.group, a.visible, a.solid)
            if a.roofHide ~= nil then world.scene:ObjSetVisible(world.roofObj, not a.roofHide) end
        elseif kind == "roof" then
            world.scene:ObjSetVisible(world.roofObj, a.visible ~= false)
        elseif kind == "rig" and a.rig then
            world.cam:blendRig(a.rig, a.dur or 0.35)
        elseif kind == "teleport" and a.x then
            if world.onTeleport then world.onTeleport(a.x, a.y or 0, a.z or 0) end
        elseif kind == "map" and a.map then
            if world.onMapChange then world.onMapChange(a.map, a.spawn or "default") end
        elseif kind == "door" and a.door then
            -- doors auto-open by proximity (isomap.updateDoors); action reserved for locked doors later
        elseif kind == "custom" and a.fn then
            a.fn(world, t)
        end
    end
end

-- inverse actions on leave for stateful toggles (interiors: leaving reverses the enter toggle)
function Triggers:unfire(t)
    local world = self.world
    for _, a in ipairs(t.actions or {}) do
        if a.type == "toggleGroup" and a.group and a.reversible ~= false then
            if a.visible ~= nil or a.solid ~= nil then
                world.models:toggleGroup(a.group,
                    a.visible ~= nil and (not a.visible) or nil,
                    a.solid ~= nil and (not a.solid) or nil)
            end
            if a.roofHide ~= nil then world.scene:ObjSetVisible(world.roofObj, a.roofHide) end
        elseif a.type == "rig" and a.rigLeave then
            world.cam:blendRig(a.rigLeave, a.dur or 0.35)
        end
    end
end

function Triggers:update(dt, px, py, pz)
    for _, t in ipairs(self.list) do
        local now = inside(t, px, py, pz)
        if now and not t.entered then
            t.entered = true
            if t.on == "enter" then self:fire(t) end
        elseif not now and t.entered then
            t.entered = false
            if t.on == "enter" and t.reversible then self:unfire(t)
            elseif t.on == "leave" then self:fire(t) end
        end
    end
end

-- the "interact" trigger the player currently stands in (nil if none)
function Triggers:interactHere(px, py, pz)
    for _, t in ipairs(self.list) do
        if t.on == "interact" and not (t.once and t.spent) and inside(t, px, py, pz) then
            return t
        end
    end
    return nil
end

return Triggers
