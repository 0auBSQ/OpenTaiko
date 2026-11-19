---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/triggers.lua: trigger volumes and their actions

local Triggers = {}
Triggers.__index = Triggers

function Triggers.new(world)
    local self = setmetatable({}, Triggers)
    self.world = world
    self.list = {}
    return self
end

-- t: id, shape, min, max, on ("enter" | "leave" | "interact"), once, actions
function Triggers:add(t)
    t.entered = false
    t.spent = false
    self.list[#self.list + 1] = t
    return t
end

function Triggers:clear()
    -- drop action closures: a stale trigger would keep the unloaded map alive
    for _, t in ipairs(self.list) do t.actions = nil end
    self.list = {}
end

local function inside(t, px, py, pz)
    return px >= t.min[1] and px <= t.max[1]
       and py >= t.min[2] and py <= t.max[2]
       and pz >= t.min[3] and pz <= t.max[3]
end
Triggers.inside = inside   -- exposed for tests

-- re-seed entered flags without firing; call after a spawn or teleport
function Triggers:resetStates(px, py, pz)
    for _, t in ipairs(self.list) do
        t.entered = (px ~= nil) and inside(t, px, py or 0, pz or 0) or false
    end
end

-- run a trigger's actions (the stage calls this for "interact")
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
            if world.onMapChange then world.onMapChange(a.map, a.spawn or "default", t) end
        elseif kind == "door" and a.door then
            -- no-op: doors open by proximity (isomap updateDoors)
        elseif kind == "custom" and a.fn then
            a.fn(world, t)
        end
    end
end

-- on leave: reverse toggleGroup and rig actions
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

-- the "interact" trigger the player stands in (nil if none)
-- the stage shows the prompt and calls fire(t) on the interact key
function Triggers:interactHere(px, py, pz)
    for _, t in ipairs(self.list) do
        if t.on == "interact" and not (t.once and t.spent) and inside(t, px, py, pz) then
            return t
        end
    end
    return nil
end

return Triggers
