---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/npc.lua: NPCs with physics character bodies

local NPC = {}
NPC.__index = NPC

-- o: x, z, name, accent, behavior ("wander" | "idle"), radius, speed, lines, dialogue
function NPC.new(world, o)
    o = o or {}
    local self = setmetatable({}, NPC)
    self.id = o.id
    self.name = o.name or "???"
    self.accent = o.accent
    self.lines = o.lines
    self.dialogue = o.dialogue
    self.behavior = o.behavior or "wander"
    self.homeX, self.homeZ = o.homeX or o.x or 0, o.homeZ or o.z or 0
    self.speed = o.speed or 1.7
    self.radius = o.radius or 3.0          -- wander radius around home
    self.sprite = o.sprite or "chara"

    local y = 0
    if world.maps.current then y = world.maps.current:heightAt(o.x or 0, o.z or 0) end
    self.char = world.phys:newCharacter{ radius = 0.30, x = o.x or 0, y = y + 0.05, z = o.z or 0, layer = "npc" }

    self.dirX, self.dirZ = 0, 0
    self.moveT = 0
    self.facing = 2
    self.animT = 0
    self.frame = "idle"
    self.talking = false
    return self
end

function NPC:pos() return self.char:pos() end

function NPC:update(dt, world)
    if self.talking then
        self.char:move(dt, 0, 0, 0, false)
        self.frame = "idle"
        return
    end
    if self.behavior == "idle" then
        self.char:move(dt, 0, 0, 0, false)
        self.frame = "idle"
        return
    end
    -- wander: new direction every 1.2-3 s, head home outside radius
    self.moveT = self.moveT - dt
    if self.moveT <= 0 then
        self.moveT = 1.2 + math.random() * 1.8
        if math.random() < 0.35 then
            self.dirX, self.dirZ = 0, 0                                  -- pause
        else
            local x, _, z = self.char:pos()
            local hx, hz = self.homeX - x, self.homeZ - z
            local dist = math.sqrt(hx * hx + hz * hz)
            if dist > self.radius then
                self.dirX, self.dirZ = hx / dist, hz / dist              -- head home
            else
                local a = math.random() * 2 * math.pi
                self.dirX, self.dirZ = math.cos(a), math.sin(a)
            end
        end
    end
    local moving = (self.dirX ~= 0 or self.dirZ ~= 0)
    self.char:move(dt, self.dirX, self.dirZ, self.speed, false)
    if moving then
        self.facing = world:facingFromWorld(self.dirX, self.dirZ, self.facing)
        self.animT = self.animT + dt
        self.frame = (math.floor(self.animT / 0.16) % 2 == 0) and "run1" or "run2"
    else
        self.frame = "idle"
    end
end

function NPC:faceTowards(px, pz, world)
    local x, _, z = self.char:pos()
    self.facing = world:facingFromWorld(px - x, pz - z, self.facing)
end

function NPC:setTalking(t) self.talking = t and true or false end

function NPC:remove()
    self.char:remove()
end

return NPC
