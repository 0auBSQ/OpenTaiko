---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/json.lua — map.json reader/validator (schema v1; see docs/owm3d-map-schema.md).
-- Parses through JSONLOADER (JsonParseFileAny → objects as Dictionary<string,object>, arrays as
-- 1-indexed Dictionary<int,object>) and normalizes everything into PLAIN LUA TABLES with defaults
-- applied, so the rest of the engine (and the lupa harness) never touches .NET types.
--
--   local def, err = Json.loadMap("maps/plaza")     -- reads maps/plaza/map.json
--   def.type == "iso"|"terrain" ; def.cells[r][c] = {h=,tex=,ramp=,wall=,liquid=} ; def.models[...]

local Json = {}

local SCHEMA_VERSION = 1

-- ── .NET → Lua conversion helpers (all access through JsonGet/JsonCount; never direct indexing) ──
local function jget(d, k) return JSONLOADER:JsonGet(d, k) end
local function jcount(d) return JSONLOADER:JsonCount(d) end

local function num(d, k, def)
    local v = jget(d, k)
    if type(v) == "number" then return v end
    return def
end
local function str(d, k, def)
    local v = jget(d, k)
    if type(v) == "string" then return v end
    return def
end
local function boolean(d, k, def)
    local v = jget(d, k)
    if type(v) == "boolean" then return v end
    return def
end
-- convert a JSON array (1-indexed dict) of numbers → Lua array
local function numArray(d, k)
    local a = jget(d, k)
    if a == nil then return nil end
    local out = {}
    for i = 1, jcount(a) do
        local v = jget(a, i)
        out[i] = (type(v) == "number") and v or 0
    end
    return out
end
-- iterate a JSON array; fn(elementDict, index)
local function each(d, k, fn)
    local a = jget(d, k)
    if a == nil then return end
    for i = 1, jcount(a) do
        local el = jget(a, i)
        if el ~= nil then fn(el, i) end
    end
end
-- localized name table {en=..., ja=...} or plain string → table
local function locName(d, k)
    local v = jget(d, k)
    if type(v) == "string" then return { en = v } end
    if v == nil then return nil end
    local out = {}
    for _, lang in ipairs({ "en", "ja", "default" }) do
        local s = jget(v, lang)
        if type(s) == "string" then out[lang] = s end
    end
    return out
end

-- ── cells: palette + CSV rows with RLE ("3x0" = index 0 three times) ─────────────────────────────
local function parsePalette(cellsD)
    local pal = {}
    each(cellsD, "palette", function(p, i)
        pal[i] = {
            tex    = str(p, "tex", "grass"),
            h      = num(p, "h", 0),
            wall   = boolean(p, "wall", false),
            liquid = str(p, "liquid", nil),
        }
    end)
    return pal
end

-- decode one CSV row string into palette indices (supports "NxV" run-length tokens)
function Json.decodeRow(rowStr, expectW)
    local out = {}
    for rawTok in tostring(rowStr):gmatch("[^,]+") do
        local tok = rawTok:match("^%s*(.-)%s*$")
        local n, v = tok:match("^(%d+)x(%d+)$")
        if n then
            for _ = 1, tonumber(n) do out[#out + 1] = tonumber(v) end
        else
            out[#out + 1] = tonumber(tok) or 0
        end
    end
    if expectW then
        while #out < expectW do out[#out + 1] = 0 end
        while #out > expectW do table.remove(out) end
    end
    return out
end

local function parseCells(d, gridW, gridH)
    local cellsD = jget(d, "cells")
    if cellsD == nil then return nil end
    local pal = parsePalette(cellsD)
    if #pal == 0 then pal[1] = { tex = "grass", h = 0 } end
    local cells = {}
    local rowsA = jget(cellsD, "rows")
    for r = 0, gridH - 1 do
        cells[r] = {}
        local rowStr = rowsA and jget(rowsA, r + 1) or nil
        local idx = rowStr and Json.decodeRow(rowStr, gridW) or nil
        for c = 0, gridW - 1 do
            local pi = idx and idx[c + 1] or 0
            local p = pal[pi + 1] or pal[1]                 -- palette indices are 0-based in JSON
            cells[r][c] = { h = p.h, tex = p.tex, wall = p.wall or nil, liquid = p.liquid }
        end
    end
    -- per-cell overrides (ramp / tex / h / wall / door)
    each(cellsD, "overrides", function(o)
        local c, r = num(o, "c", -1), num(o, "r", -1)
        if c >= 0 and c < gridW and r >= 0 and r < gridH then
            local cell = cells[r][c]
            local h = jget(o, "h");    if type(h) == "number" then cell.h = h end
            local t = jget(o, "tex");  if type(t) == "string" then cell.tex = t end
            local w = jget(o, "wall"); if type(w) == "boolean" then cell.wall = w or nil end
            local rampD = jget(o, "ramp")
            if rampD ~= nil then
                cell.ramp = { dir = str(rampD, "dir", "x+"), lo = num(rampD, "lo", 0), hi = num(rampD, "hi", 0.5) }
            end
        end
    end)
    return cells
end

-- ── the loader ────────────────────────────────────────────────────────────────────────────────────
-- dir = map folder relative to the stage (e.g. "maps/plaza"); reads <dir>/map.json.
-- Returns (def, nil) or (nil, "error message").
function Json.loadMap(dir)
    local ok, d = pcall(function() return JSONLOADER:JsonParseFileAny(dir .. "/map.json") end)
    if not ok or d == nil then return nil, "map.json not found or unreadable in '" .. tostring(dir) .. "'" end
    return Json.parse(d, dir)
end

-- parse an already-loaded JSON dict (harness entry point)
function Json.parse(d, dir)
    local ver = num(d, "owm3d", -1)
    if ver ~= SCHEMA_VERSION then
        return nil, "unsupported map schema version " .. tostring(ver) .. " (engine supports " .. SCHEMA_VERSION .. ")"
    end
    local def = {
        dir  = dir or ".",
        id   = str(d, "id", "map"),
        name = locName(d, "name") or { en = str(d, "id", "map") },
        type = str(d, "type", "iso"),
    }
    if def.type ~= "iso" and def.type ~= "terrain" then
        return nil, "unknown map type '" .. tostring(def.type) .. "'"
    end

    -- grid + cells (iso)
    local gridD = jget(d, "grid")
    def.gridW, def.gridH = num(gridD, "w", 16), num(gridD, "h", 16)
    if def.type == "iso" then
        def.cells = parseCells(d, def.gridW, def.gridH)
        if def.cells == nil then return nil, "iso map has no cells" end
    end

    -- terrain
    local terrD = jget(d, "terrain")
    if terrD ~= nil then
        def.terrain = {
            heights  = str(terrD, "heights", "height.r16"),
            w        = num(terrD, "w", 256),
            h        = num(terrD, "h", 256),
            minY     = num(terrD, "minY", 0),
            maxY     = num(terrD, "maxY", 20),
            cellSize = num(terrD, "cellSize", 1.0),
            splat    = str(terrD, "splat", nil),
            layers   = {},
        }
        local layersA = jget(terrD, "layers")
        if layersA ~= nil then
            for i = 1, jcount(layersA) do
                local s = jget(layersA, i)
                if type(s) == "string" then def.terrain.layers[i] = s end
            end
        end
        def.terrain.grassDensity = num(terrD, "grassDensity", nil)
        -- optional grass block (v1 addition): { density=, clumpScale=, exclude=[{rect:[x0,z0,x1,z1]},...] }
        local grassD = jget(terrD, "grass")
        if grassD ~= nil then
            def.terrain.grass = {
                density    = num(grassD, "density", nil),
                clumpScale = num(grassD, "clumpScale", nil),
                exclude    = {},
            }
            each(grassD, "exclude", function(e)
                local rect = numArray(e, "rect")
                if rect then
                    def.terrain.grass.exclude[#def.terrain.grass.exclude + 1] =
                        { x0 = rect[1], z0 = rect[2], x1 = rect[3], z1 = rect[4] }
                end
            end)
        end
        if def.type == "terrain" then
            def.gridW = math.floor(def.terrain.w * def.terrain.cellSize)
            def.gridH = math.floor(def.terrain.h * def.terrain.cellSize)
        end
    elseif def.type == "terrain" then
        return nil, "terrain map has no terrain block"
    end

    -- textures: name → {builtin=} | {file=, filter=}
    def.textures = {}
    local texD = jget(d, "textures")
    if texD ~= nil then
        -- object keys aren't enumerable through JsonGet; textures are therefore an ARRAY of
        -- {name=,builtin=|file=,filter=} entries in the schema
        each(d, "textures", function(t)
            local nm = str(t, "name", nil)
            if nm then
                def.textures[nm] = {
                    builtin = str(t, "builtin", nil),
                    file    = str(t, "file", nil),
                    filter  = str(t, "filter", "nearest"),
                }
            end
        end)
    end

    -- buildings
    def.buildings = {}
    each(d, "buildings", function(b)
        local door = jget(b, "door")
        def.buildings[#def.buildings + 1] = {
            c = num(b, "c", 0), r = num(b, "r", 0),
            w = num(b, "w", 3), d = num(b, "d", 3),
            wallH = num(b, "wallH", 2.6),
            roof = str(b, "roof", "red"),
            roofH = num(b, "roofH", 1.3),
            winPhase = num(b, "winPhase", 0),
            door = door and { side = str(door, "side", "s"), off = num(door, "off", 1) } or nil,
            interiorGroup = str(b, "interiorGroup", nil),
        }
    end)

    -- models
    def.models = {}
    each(d, "models", function(m)
        local pos = numArray(m, "pos") or { 0, 0, 0 }
        local animD = jget(m, "anim")
        local parts = {}
        each(m, "parts", function(p)
            local e = numArray(p, "emissive")
            parts[#parts + 1] = {
                part = num(p, "part", 0),
                emissive = e,
                alpha = num(p, "alpha", nil),
                unlit = boolean(p, "unlit", false),
                mirror = boolean(p, "mirror", false),
            }
        end)
        def.models[#def.models + 1] = {
            id = str(m, "id", "model" .. (#def.models + 1)),
            file = str(m, "file", ""),
            x = pos[1] or 0, y = pos[2] or 0, z = pos[3] or 0,
            yaw = num(m, "yaw", 0), scale = num(m, "scale", 1),
            anchor = str(m, "anchor", nil),
            visible = boolean(m, "visible", true),
            collide = str(m, "collide", "box"),
            box = numArray(m, "box"),
            group = str(m, "group", nil),
            anim = animD and {
                index = num(animD, "index", 0),
                speed = num(animD, "speed", 1),
                loop = boolean(animD, "loop", true),
                playing = boolean(animD, "playing", true),
            } or nil,
            parts = parts,
        }
    end)

    -- lights
    def.lights = {}
    each(d, "lights", function(l)
        local pos = numArray(l, "pos") or { 0, 2, 0 }
        local rgb = numArray(l, "rgb") or { 1, 1, 1 }
        def.lights[#def.lights + 1] = {
            x = pos[1], y = pos[2], z = pos[3],
            r = rgb[1], g = rgb[2], b = rgb[3],
            intensity = num(l, "intensity", 1.5),
            range = num(l, "range", 7),
            flicker = num(l, "flicker", 0),
            nightOnly = boolean(l, "nightOnly", false),
        }
    end)

    -- water planes
    def.water = {}
    each(d, "water", function(w)
        local rect = numArray(w, "rect") or { 0, 0, 1, 1 }
        local col = numArray(w, "color")
        local deep = numArray(w, "deepColor")
        def.water[#def.water + 1] = {
            x0 = rect[1], z0 = rect[2], x1 = rect[3], z1 = rect[4],
            y = num(w, "y", 0), amp = num(w, "amp", 0.05),
            reflect = boolean(w, "reflect", false),
            ripple = num(w, "ripple", 0.5),
            tex = str(w, "tex", "water"),
            color = col, deepColor = deep,      -- optional shallow/deep tint (v1 addition)
        }
    end)

    -- particles
    def.particles = {}
    each(d, "particles", function(p)
        local pos = numArray(p, "pos") or { 0, 0, 0 }
        local params = {}
        local paramsD = jget(p, "params")
        if paramsD ~= nil then
            for _, key in ipairs({ "speed", "size", "life", "gravity", "spread", "rateScale" }) do
                local v = jget(paramsD, key)
                if type(v) == "number" then params[key] = v end
            end
        end
        def.particles[#def.particles + 1] = {
            preset = str(p, "preset", "embers"),
            x = pos[1], y = pos[2], z = pos[3],
            rate = num(p, "rate", 20),
            params = params,
        }
    end)

    -- NPCs
    def.npcs = {}
    each(d, "npcs", function(n)
        local pos = numArray(n, "pos") or { 0, 0 }
        local accent = numArray(n, "accent")
        def.npcs[#def.npcs + 1] = {
            id = str(n, "id", "npc" .. (#def.npcs + 1)),
            sprite = str(n, "sprite", "chara"),
            x = pos[1], z = pos[2],
            behavior = str(n, "behavior", "wander"),
            radius = num(n, "radius", 3),
            dialogue = str(n, "dialogue", nil),
            name = locName(n, "name"),
            accent = accent,
        }
    end)

    -- missions anchors (mission DATA still comes from Missions.db3)
    def.missions = {}
    each(d, "missions", function(m)
        local pos = numArray(m, "pos") or { 0, 0 }
        def.missions[#def.missions + 1] = { uid = num(m, "uid", 0), x = pos[1], z = pos[2] }
    end)

    -- portals (map transitions)
    def.portals = {}
    each(d, "portals", function(p)
        local cell = numArray(p, "cell")
        local pos = numArray(p, "pos")
        local tgt = jget(p, "target")
        def.portals[#def.portals + 1] = {
            id = str(p, "id", "portal" .. (#def.portals + 1)),
            c = cell and cell[1] or nil, r = cell and cell[2] or nil,
            x = pos and pos[1] or nil, z = pos and pos[2] or nil,
            map = tgt and str(tgt, "map", nil) or nil,
            spawn = tgt and str(tgt, "spawn", "default") or "default",
            label = locName(p, "label"),
        }
    end)

    -- triggers
    def.triggers = {}
    each(d, "triggers", function(t)
        local mn = numArray(t, "min") or { 0, 0, 0 }
        local mx = numArray(t, "max") or { 1, 1, 1 }
        local actions = {}
        each(t, "actions", function(a)
            local act = { type = str(a, "type", "custom") }
            act.group   = str(a, "group", nil)
            act.visible = boolean(a, "visible", nil)
            act.solid   = boolean(a, "solid", nil)
            act.door    = str(a, "door", nil)
            act.rig     = str(a, "rig", nil)
            act.model   = str(a, "model", nil)
            act.map     = str(a, "map", nil)
            act.spawn   = str(a, "spawn", nil)
            local to = numArray(a, "to")
            if to then act.x, act.y, act.z = to[1], to[2], to[3] end
            actions[#actions + 1] = act
        end)
        def.triggers[#def.triggers + 1] = {
            id = str(t, "id", "trigger" .. (#def.triggers + 1)),
            shape = str(t, "shape", "box"),
            min = { mn[1] or 0, mn[2] or 0, mn[3] or 0 },
            max = { mx[1] or 1, mx[2] or 1, mx[3] or 1 },
            on = str(t, "on", "enter"),
            once = boolean(t, "once", false),
            actions = actions,
        }
    end)

    -- spawns: array of {name=,x=,z=,yaw=} (same array-of-named-entries pattern as textures)
    def.spawns = {}
    each(d, "spawns", function(s)
        local nm = str(s, "name", "default")
        def.spawns[nm] = { x = num(s, "x", 0), z = num(s, "z", 0), yaw = num(s, "yaw", 45) }
    end)
    if def.spawns.default == nil then
        def.spawns.default = { x = def.gridW / 2, z = def.gridH / 2, yaw = 45 }
    end

    -- ambience
    local skyD = jget(d, "sky")
    def.sky = { mode = str(skyD, "mode", "cycle"), hour = num(skyD, "hour", nil) }
    local fogD = jget(d, "fog")
    def.fog = fogD and { near = num(fogD, "near", 22), far = num(fogD, "far", 120) } or nil
    local gradeD = jget(d, "grade")
    def.grade = gradeD and {
        tilt = num(gradeD, "tilt", 2.2), sat = num(gradeD, "sat", 1.15),
        vig = num(gradeD, "vig", 0.25), bloom = num(gradeD, "bloom", 0.5),
        exposure = num(gradeD, "exposure", 1.0),
    } or nil
    def.bgm = str(d, "bgm", nil)
    local weatherD = jget(d, "weather")
    def.weather = { default = str(weatherD, "default", "clear"), allowRain = boolean(weatherD, "allowRain", true) }

    return def, nil
end

return Json
