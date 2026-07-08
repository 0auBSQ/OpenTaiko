---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- OWM3d.lua — facade for the Open-World Memories 3D engine (world engine).
--
--   local OWM = require("OWM3d")
--   local world = OWM.World.new{ ... }          -- see OWM3d/core.lua for the full surface
--
-- Successor to Lib/isoengine.lua (which stays frozen for its legacy consumers): three map types
-- (procedural / iso-JSON / terrain-JSON), real 3D physics on the engine PhysicsWorld (collide-and-
-- slide characters, raycasts, toggleable collider groups), a non-clipping physics camera boom,
-- glTF model instances with per-part material flags, and a trigger/action system for streamed
-- interiors, doors and map transitions. Map format: docs/owm3d-map-schema.md (repo root).

return {
    World     = require("OWM3d.core"),
    Camera    = require("OWM3d.camera"),
    Json      = require("OWM3d.json"),
    IsoMap    = require("OWM3d.isomap"),
    Terrain   = require("OWM3d.terrain"),
    NPC       = require("OWM3d.npc"),
    ModelIcon = require("OWM3d.modelicon"),   -- 3D-model preview thumbnails (catalog icons, shop cards)
    version   = 1,
}
