---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/maps.lua — the map registry + load/unload lifecycle. Dispatches the three map types and
-- wires a loaded definition into every subsystem (geometry+collision → models → triggers → NPCs →
-- lights → water → spawn), all inside one physics build session.
--
--   world:registerMap("plaza",   { type = "folder", dir = "maps/plaza" })     -- map.json + assets
--   world:registerMap("volcano", { type = "proc", build = function(world, maps) ... return map end })
--   local map = world:loadMap("plaza", "fromHills")
--
-- A "map handle" is: { def=, heightAt(self,wx,wz)=, isWall=, updateDoors=, spawns=, npcs=,
--                      portals=, waterDefs=, spawnUsed= }. Procedural builders return their own
-- handle (same contract) and manage their geometry directly.

local Json    = require("OWM3d.json")
local IsoMap  = require("OWM3d.isomap")
local Terrain = require("OWM3d.terrain")
local NPC     = require("OWM3d.npc")

local Maps = {}
Maps.__index = Maps

function Maps.new(world)
    local self = setmetatable({}, Maps)
    self.world = world
    self.defs = {}
    self.current = nil
    self.texResolver = nil        -- set by the stage: function(name, texDef) → texture id
    return self
end

function Maps:register(id, def) self.defs[id] = def end

-- the stage provides texture resolution (it owns procedural generators + file registration)
function Maps:setTextureResolver(fn) self.texResolver = fn end

-- Unload = the disposal CONTRACT for everything a map session created. Order matters: bodies
-- before physics reset, subsystems before render-cache drop, native buffers last. Every step is
-- pcall-guarded where one failure must not skip the rest (the old version leaked whole maps when
-- a single npc:remove errored).
function Maps:unload()
    local world = self.world
    if self.current == nil then return end
    local map = self.current
    -- clear layer geometry
    local scene = world.scene
    for _, obj in ipairs({ world.floorObj, world.wallObj, world.roofObj, world.prop3dObj,
                           world.doorObj, world.propObj, world.actorObj, world.npcObj,
                           world.shadowObj, world.bubbleObj, world.markerObj, world.waterObj,
                           world.gridObj, world.ghostObj, world.hiliteObj }) do
        scene:ObjBegin(obj)
    end
    scene:ObjSetVisible(world.roofObj, true)
    if scene.ClearLights then scene:ClearLights() end
    -- NPCs + their bodies (guarded per NPC — one failure must not orphan the rest)
    for _, npc in ipairs(map.npcs or {}) do pcall(npc.remove, npc) end
    -- model instances (file cache stays — it's per-stage)
    world.models:clear()
    world.triggers:clear()
    if world.water then world.water:clear() end
    if world.particles then world.particles:clear() end
    if world.weather and world.weather.setWeather then pcall(world.weather.setWeather, world.weather, "clear", 0) end
    if world.daynight then world.daynight:setLights(nil) end
    if scene.GrassClear then scene:GrassClear() end
    -- the floor object is reused by every map — drop the terrain-splat routing (an iso map after a
    -- terrain map would otherwise still be drawn through the splat shader with a stale weight image)
    if scene.ObjSetTerrainSplat then
        scene:ObjSetTerrainSplat(world.floorObj, -1, -1, -1, -1, -1, 1, 1, 1)
        if scene.UnregisterSprite then scene:UnregisterSprite(917) end
    end
    -- physics: release the soup (trims the native lists; beginStatic only Clear()s them)
    if world.phys.clearGeometry then world.phys:clearGeometry()
    else world.phys:beginStatic(); world.phys:endStatic() end
    -- rebuildable render caches (per-object VBOs / merged buffers / reflection targets)
    if scene.ClearRenderCaches then scene:ClearRenderCaches() end
    -- native terrain buffers (heightfield + splat) — deterministic free, not two GC generations
    if map.hf and map.hf.Release then pcall(map.hf.Release, map.hf) end
    if map.splat and map.splat.Release then pcall(map.splat.Release, map.splat) end
    self.current = nil
    collectgarbage("collect")
end

function Maps:load(id, spawnName)
    local world = self.world
    self:unload()
    local reg = self.defs[id]
    if reg == nil then return nil, "unknown map '" .. tostring(id) .. "'" end

    local map
    if reg.type == "proc" then
        -- procedural maps own their build (they still get the physics session + subsystems)
        world.phys:beginStatic()
        map = reg.build(world, self)
        for _, inst in ipairs(world.models.list) do world.models:emitCollision(inst) end
        if world.onMapDecorate then world.onMapDecorate(map, nil, id) end
        world.phys:endStatic()
        map = map or {}
        map.spawns = map.spawns or { default = { x = world.gridW / 2, z = world.gridH / 2, yaw = 45 } }
    else
        local def, err = Json.loadMap(reg.dir)
        if def == nil then return nil, err end
        map = self:buildFromDef(def)
        if map == nil then return nil, "map build failed" end
    end

    map.id = id
    self.current = map

    -- resolve the spawn
    local sp = (map.spawns and (map.spawns[spawnName or "default"] or map.spawns.default))
        or { x = world.gridW / 2, z = world.gridH / 2, yaw = 45 }
    map.spawnUsed = sp
    -- seed trigger states from the spawn point — spawning inside a volume must not phantom-fire
    if world.triggers.resetStates then
        local sy = 0
        if map.heightAt then local ok, h = pcall(map.heightAt, map, sp.x, sp.z); if ok and h then sy = h end end
        world.triggers:resetStates(sp.x, sy + 0.5, sp.z)
    end
    return map
end

-- build a JSON-defined map (iso or terrain) — the full wiring pass
function Maps:buildFromDef(def)
    local world = self.world
    -- resolver: the stage's (it owns procedural generators/registration), else the world's
    -- name→id pool (stable ids across loads — the registry must not grow per map switch)
    local texFn = self.texResolver or function(name) return world:texIdFor(name) end
    local function texId(name) return texFn(name, def.textures and def.textures[name] or nil) end

    world.phys:beginStatic()

    -- 1. base geometry + collision
    local map
    if def.type == "terrain" then
        map = Terrain.build(world, def, texId)
    else
        map = IsoMap.build(world, def, texId)
    end

    -- 2. models (visible immediately; collision emitted into the same session)
    for _, m in ipairs(def.models or {}) do
        local inst = world.models:add{
            file = def.dir .. "/" .. m.file,
            x = m.x, y = m.y, z = m.z, yaw = m.yaw, scale = m.scale, anchor = m.anchor,
            visible = m.visible, collide = m.collide, box = m.box, group = m.group,
            anim = m.anim, parts = m.parts,
        }
        if inst then world.models:emitCollision(inst) end
    end

    -- water is scenery, not a swimming pool: an invisible knee-high fence around each plane keeps
    -- the player out (emitted inside the session so it's part of the static soup). Grass never
    -- grows underwater either.
    local Water = require("OWM3d.water")
    for _, wd in ipairs(def.water or {}) do
        Water.emitFence(world.phys, wd)
        if world.scene.GrassRemoveRect then world.scene:GrassRemoveRect(wd.x0, wd.z0, wd.x1, wd.z1) end
    end

    -- stage dressing (props/furniture that also need collision) joins the same build session
    if world.onMapDecorate then world.onMapDecorate(map, def, def.id) end

    world.phys:endStatic()

    -- 3. triggers
    for _, t in ipairs(def.triggers or {}) do
        world.triggers:add{
            id = t.id, shape = t.shape, min = t.min, max = t.max,
            on = t.on, once = t.once, actions = t.actions,
            reversible = true,
        }
    end

    -- 4. NPCs (physics bodies must exist AFTER the static build so they rest on real ground)
    map.npcs = {}
    self.current = map      -- npc ground snap reads maps.current:heightAt
    for _, n in ipairs(def.npcs or {}) do
        map.npcs[#map.npcs + 1] = NPC.new(world, n)
    end

    -- 5. lights: daynight owns the list (nightOnly scheduling + flicker); static fallback without it
    map.lightDefs = def.lights or {}
    if world.daynight then
        world.daynight:setLights(map.lightDefs)
    elseif world.scene.AddLightRanged then
        for _, l in ipairs(map.lightDefs) do
            if not l.nightOnly then
                world.scene:AddLightRanged(l.x, l.y, l.z, l.r, l.g, l.b, l.intensity, l.range)
            end
        end
    end

    -- 6. water planes + ambient particle emitters
    if world.water then world.water:fromDefs(def.water) end
    if world.particles then world.particles:fromDefs(def.particles) end

    -- 7. ambience
    if def.sky and def.sky.hour ~= nil then world.hour = def.sky.hour else world.hour = nil end
    if def.fog then world:setFog(true, def.fog.near, def.fog.far) end
    if def.grade then world:setDiorama(def.grade.tilt, def.grade.sat, def.grade.vig, def.grade.bloom) end
    if def.weather and def.weather.default and world.weather then world.weather:setWeather(def.weather.default, 0.1) end

    map.spawns = def.spawns
    map.portals = def.portals
    map.missions = def.missions
    map.waterDefs = def.water
    map.particleDefs = def.particles
    map.bgm = def.bgm
    map.weatherDef = def.weather
    return map
end

return Maps
