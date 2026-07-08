---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- OWM3d/terrain.lua — heightmap terrain maps: builds a tessellated mesh from a raw heightfield
-- (HEIGHTMAP:Load — see LuaHeightmap.cs) with per-quad splat texturing, emits matching physics
-- triangles, and PLANTS GRASS: wind-swayed GPU blades (scene:GrassAdd) scattered over grass-layer
-- quads with deterministic density. heightAt() is the same bilinear sample the mesh uses.
--
-- def.terrain = { heights="height.r16", w, h, minY, maxY, cellSize, splat="splat.png", layers={...} }
-- The splat image (optional) picks the DOMINANT of up to 4 texture layers per quad (r/g/b/a
-- channels, loaded via HEIGHTMAP:LoadSplat). Without one, layer 1 is used with slope-based rock
-- (layer 2) on steep quads. Long builds may yield through world.buildYield(frac) (loading screens).

local floor, sin, max, min = math.floor, math.sin, math.max, math.min

local Terrain = {}
Terrain.__index = Terrain

local GRASS_SPR = 915

local function makeHeightAt(hf, terr)
    local sx = 1.0 / (terr.cellSize or 1)
    local range = (terr.maxY or 20) - (terr.minY or 0)
    local base = terr.minY or 0
    return function(wx, wz)
        return base + hf:Sample(wx * sx, wz * sx) * range
    end
end

-- deterministic 2D hash (no math.random: rebuilds must be identical)
local function h2(a, b)
    local n = sin(a * 127.1 + b * 311.7) * 43758.5453
    return n - floor(n)
end

-- a tapered grass-blade sprite (cutout, slightly curved, two green tones)
local function ensureGrassSprite(scene)
    if scene.GrassSprite == nil or scene.SetSpriteRGBA == nil then return false end
    if Terrain._sprDone then return true end
    Terrain._sprDone = true
    -- higher-res, smoothly tapered blade with a soft edge falloff (reads granular, not chunky)
    local w, h = 24, 64
    local t = {}
    for i = 1, w * h do t[i] = 0 end
    for y = 0, h - 1 do
        local f = 1 - y / (h - 1)                       -- 1 at the tip, 0 at the root
        local half = 0.9 + (1 - f) * 5.4                -- tapers toward the tip
        local bend = f * f * 4.2                        -- gentle curve
        local cx = w * 0.5 + bend
        for x = 0, w - 1 do
            local d = math.abs(x - cx)
            if d <= half then
                local edge = 1 - d / half               -- soft silhouette (alpha ramps at the rim)
                local a = edge > 0.35 and 255 or floor(edge / 0.35 * 255)
                local g = 118 + 74 * (1 - f) + (h2(x, y) - 0.5) * 22
                local r = 50 + 32 * (1 - f)
                t[y * w + x + 1] = (a << 24) | (floor(r) << 16) | (floor(max(60, min(212, g))) << 8) | 42
            end
        end
    end
    scene:SetSpriteRGBA(GRASS_SPR, t, w, h)
    if scene.SetSpriteFilter then scene:SetSpriteFilter(GRASS_SPR, "linear") end   -- soft blade edges
    scene:GrassSprite(GRASS_SPR)
    return true
end

-- build the terrain into the world's layers + physics; returns the map handle.
-- texId(name) resolves layer texture names.
function Terrain.build(world, def, texId)
    local scene = world.scene
    local phys = world.phys
    local terr = def.terrain

    local hf = HEIGHTMAP:Load(def.dir .. "/" .. terr.heights, terr.w, terr.h)
    local heightAt = makeHeightAt(hf, terr)

    -- splat: dominant-channel layer per texel (image size may differ from the sample grid)
    local splatMap = nil          -- the raw LuaSplatmap (native path)
    local splat = nil             -- per-quad closure (Lua fallback path)
    if terr.splat and HEIGHTMAP.LoadSplat then
        local sm = HEIGHTMAP:LoadSplat(def.dir .. "/" .. terr.splat)
        if sm and sm:W() > 0 then
            splatMap = sm
            local sxk = sm:W() / terr.w
            local szk = sm:H() / terr.h
            splat = function(ix, iz) return sm:At(floor(ix * sxk), floor(iz * szk)) end
        end
    end

    local cs = terr.cellSize or 1
    local W, H = terr.w - 1, terr.h - 1          -- quads between samples
    world:setGridSize(floor(W * cs), floor(H * cs))

    local layerTex = {}
    for i = 1, 4 do
        layerTex[i] = texId(terr.layers[i] or terr.layers[1] or "grass")
    end

    scene:ObjBegin(world.floorObj)
    scene:ObjBegin(world.wallObj)
    scene:ObjBegin(world.roofObj)
    scene:ObjBegin(world.prop3dObj)
    world._boxTarget = world.prop3dObj

    local range = (terr.maxY or 20) - (terr.minY or 0)
    local base = terr.minY or 0
    local function sampleY(ix, iz) return base + hf:At(ix, iz) * range end

    local grassOk = ensureGrassSprite(scene)
    if grassOk then scene:GrassClear(); scene:GrassFade(26, 44) end
    local gcfg = terr.grass or {}
    local density = gcfg.density or terr.grassDensity or 7   -- blades per grass quad (average)
    local clump = gcfg.clumpScale or 1                       -- 0 = uniform (legacy), >0 = noisy patches
    local blades = 0

    -- map-driven grass exclusions (roads, pads) — applied right after planting
    local function applyGrassExcludes()
        if not (grassOk and scene.GrassRemoveRect) then return end
        for _, ex in ipairs(gcfg.exclude or {}) do
            scene:GrassRemoveRect(ex.x0, ex.z0, ex.x1, ex.z1)
        end
    end

    -- GPU splat shading: the floor blends its four layer textures per PIXEL by the weight image,
    -- so painted roads/beaches feather smoothly instead of cutting at quad edges (the per-quad
    -- dominant texture stays baked in as the CPU-renderer fallback look)
    local SPLAT_SPR = 917
    local function wireSplatShader()
        if splatMap and splatMap.RegisterSprite and scene.ObjSetTerrainSplat then
            splatMap:RegisterSprite(scene, SPLAT_SPR)
            scene:ObjSetTerrainSplat(world.floorObj, SPLAT_SPR,
                layerTex[1], layerTex[2], layerTex[3], layerTex[4],
                terr.w * cs, terr.h * cs, 1 / cs)
        end
    end

    -- native bulk path: the whole mesh + physics + grass in ONE interop call (the per-quad Lua
    -- loop boxed ~8 calls × quad — a huge GC spike on 400² maps). Lua loop below = harness/CPU fallback.
    if HEIGHTMAP.BuildTerrain then
        if world.buildYield then world.buildYield(0.15) end
        blades = HEIGHTMAP:BuildTerrain(scene, world.floorObj, world.phys.w, hf, splatMap,
            terr.w, terr.h, base, base + range, cs,
            layerTex[1], layerTex[2], layerTex[3], layerTex[4],
            grassOk and density or 0, clump)
        world.phys._triCount = world.phys._triCount + W * H * 2
        applyGrassExcludes()
        wireSplatShader()
        if world.buildYield then world.buildYield(0.9) end
        return setmetatable({
            def = def,
            hf = hf,
            splat = splatMap,
            grassBlades = blades,
            heightAt = function(_, wx, wz) return heightAt(wx, wz) end,
            isWall = function() return false end,
        }, { __index = { updateDoors = function() end } })
    end

    for iz = 0, H - 1 do
        for ix = 0, W - 1 do
            local x0, z0 = ix * cs, iz * cs
            local x1, z1 = x0 + cs, z0 + cs
            local h00 = sampleY(ix, iz)
            local h10 = sampleY(ix + 1, iz)
            local h11 = sampleY(ix + 1, iz + 1)
            local h01 = sampleY(ix, iz + 1)
            local slope = max(math.abs(h10 - h00), math.abs(h01 - h00), math.abs(h11 - h00)) / cs
            local li
            if splat then li = splat(ix, iz)
            else li = (slope > 0.9) and 2 or 1 end
            local tex = layerTex[li] or layerTex[1]
            scene:ObjAddQuadTex(world.floorObj, x0, h00, z0, x0, h01, z1, x1, h11, z1, x1, h10, z0, tex, 1, 1, 1.0)
            phys:addQuad(x0, h00, z0, x0, h01, z1, x1, h11, z1, x1, h10, z0)

            -- grass on gentle layer-1 quads: deterministic scatter, denser on flatter ground
            if grassOk and li == 1 and slope < 0.55 then
                local n = floor(density * (1 - slope) * (0.5 + h2(ix, iz)))
                for b = 1, n do
                    local fx = h2(ix * 3 + b, iz * 7 + b)
                    local fz = h2(ix * 7 - b, iz * 3 + b * 5)
                    local gx, gz = x0 + fx * cs, z0 + fz * cs
                    local gy = heightAt(gx, gz)
                    scene:GrassAdd(gx, gy - 0.02, gz,
                        0.28 + 0.3 * h2(gx * 13, gz * 17),
                        h2(gx * 5, gz * 5) * 6.28318,
                        0.7 + 0.3 * h2(gx, gz))
                    blades = blades + 1
                end
            end
        end
        if world.buildYield and iz % 8 == 7 then world.buildYield(iz / H) end
    end

    -- invisible border walls (Lua fallback path; the native builder emits its own)
    do
        local bw, bd = W * cs, H * cs
        local lo, hi = base - 3, base + range + 6
        phys:addQuad(0, lo, 0, bw, lo, 0, bw, hi, 0, 0, hi, 0)
        phys:addQuad(0, lo, bd, bw, lo, bd, bw, hi, bd, 0, hi, bd)
        phys:addQuad(0, lo, 0, 0, lo, bd, 0, hi, bd, 0, hi, 0)
        phys:addQuad(bw, lo, 0, bw, lo, bd, bw, hi, bd, bw, hi, 0)
    end

    applyGrassExcludes()
    wireSplatShader()
    return setmetatable({
        def = def,
        hf = hf,
        splat = splatMap,
        grassBlades = blades,
        heightAt = function(_, wx, wz) return heightAt(wx, wz) end,
        isWall = function() return false end,
    }, { __index = {
        updateDoors = function() end,
    } })
end

return Terrain
