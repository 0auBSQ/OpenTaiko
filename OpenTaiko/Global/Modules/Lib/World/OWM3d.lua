---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- World.OWM3d: the OWM 3D world engine modules in one table
-- map format: docs/owm3d-map-schema.md

return {
    World     = require("World.OWM3d.core"),
    Camera    = require("World.OWM3d.camera"),
    Json      = require("World.OWM3d.json"),
    IsoMap    = require("World.OWM3d.isomap"),
    Terrain   = require("World.OWM3d.terrain"),
    NPC       = require("World.OWM3d.npc"),
    ModelIcon = require("World.OWM3d.modelicon"),   -- 3D-model preview thumbnails
    version   = 1,
}
