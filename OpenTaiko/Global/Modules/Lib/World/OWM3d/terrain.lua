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
local grassScenes = setmetatable({}, { __mode = "k" })   -- the scenes that have the blade sprite (a new world has a new scene)

local function makeHeightAt(hf, terr)
    local cs = terr.cellSize or 1
    local sx = 1.0 / cs
    local range = (terr.maxY or 20) - (terr.minY or 0)
    local base = terr.minY or 0
    if cs == 1 then
        return function(wx, wz)
            return base + hf:Sample(wx * sx, wz * sx) * range
        end
    end
    -- larger cells: follow the mesh triangles
    local W, H = terr.w - 1, terr.h - 1
    return function(wx, wz)
        local fx, fz = wx * sx, wz * sx
        if fx < 0 then fx = 0 elseif fx > W then fx = W end
        if fz < 0 then fz = 0 elseif fz > H then fz = H end
        local ix, iz = floor(fx), floor(fz)
        if ix >= W then ix = W - 1 end
        if iz >= H then iz = H - 1 end
        local tx, tz = fx - ix, fz - iz
        local h00, h11 = hf:At(ix, iz), hf:At(ix + 1, iz + 1)
        local s
        if tz >= tx then
            local h01 = hf:At(ix, iz + 1)
            s = h00 + (h11 - h01) * tx + (h01 - h00) * tz
        else
            local h10 = hf:At(ix + 1, iz)
            s = h00 + (h10 - h00) * tx + (h11 - h10) * tz
        end
        return base + s * range
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
    if grassScenes[scene] then return true end
    grassScenes[scene] = true
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

-- ── opt-in looks ──
local SPLAT_SPR, ATLAS_SPR = 917, 918

local function normPath(p)
    local parts = {}
    for seg in p:gsub("\\", "/"):gmatch("[^/]+") do
        if seg == ".." and #parts > 0 and parts[#parts] ~= ".." then parts[#parts] = nil
        elseif seg ~= "." then parts[#parts + 1] = seg end
    end
    return table.concat(parts, "/")
end

-- pooled texture id; nil when unreadable
local function loadTexture(world, path, key, filter)
    local scene = world.scene
    local id = world:texIdFor(key)
    local ok = pcall(function()
        local tex = TEXTURE:CreateTextureSync(normPath(path))
        if (tex.Width or 0) <= 0 then tex:Dispose(); error("missing") end
        scene:RegisterTextureFromImage(id, tex)
        if scene.SetTextureFilter then scene:SetTextureFilter(id, filter or "mipmap") end
        tex:Dispose()
    end)
    return ok and id or nil
end

local function at(t, i, d) return (type(t) == "table" and t[i]) or d end

-- L: terrain.look keys, others kept; false without the engine call
function Terrain.setLook(world, L)
    local scene = world.scene
    if scene.SetTerrainParam == nil then return false end
    local cur = world._terrLook or {}
    for k, v in pairs(L or {}) do cur[k] = v end
    world._terrLook = cur
    scene:SetTerrainParam("detail", cur.scale2 or 0.137, cur.macro or 0.6, cur.bump or 0, cur.sharp or 0.5)
    scene:SetTerrainParam("maps", cur.normal or 1, cur.tint or 1, cur.rim or 0, 0)
    scene:SetTerrainParam("triplanar", at(cur.triplanar, 1, 0), at(cur.triplanar, 2, 0), at(cur.triplanar, 3, 0), at(cur.triplanar, 4, 0))
    scene:SetTerrainParam("skyamb", at(cur.skyAmb, 1, 1), at(cur.skyAmb, 2, 1), at(cur.skyAmb, 3, 1), 0)
    scene:SetTerrainParam("groundamb", at(cur.groundAmb, 1, 1), at(cur.groundAmb, 2, 1), at(cur.groundAmb, 3, 1), 0)
    scene:SetTerrainParam("cloud", at(cur.cloud, 1, 0), at(cur.cloud, 2, 60), at(cur.cloud, 3, 1.5), at(cur.cloud, 4, 0.6))
    if world._grassTile and scene.GrassTileParam then
        local T = world._grassTile
        scene:GrassTileParam("ground", T.minWeight or 0.45, T.maxSlope or 0.7, cur.scale2 or 0.137, cur.macro or 0.6)
    end
    return true
end

-- T: terrain.grass.tile keys, others kept, false = off; returns false when off or unsupported
-- atlas, mask paths: stage-relative from Lua, beside map.json from a map file
function Terrain.setGrass(world, T, dir)
    local scene = world.scene
    if scene.GrassTileEnable == nil then return false end
    local ctx = world._terrCtx
    if not T or ctx == nil then
        scene:GrassTileEnable(false)
        world._grassTile = nil
        return false
    end
    local cur = world._grassTile or {}
    for k, v in pairs(T) do cur[k] = v end
    world._grassTile = cur
    if cur.meta and cur.meta ~= cur._metaAt then
        local ok, m = pcall(require, cur.meta)
        if ok and type(m) == "table" then
            cur.cols, cur.rows = m.cols or cur.cols, m.rows or cur.rows
            cur.grassCells, cur.flowerCells = m.grass or cur.grassCells, m.flowers or cur.flowerCells
            cur.avg, cur.cells = m.avg or cur.avg, m.cells or cur.cells
            cur._metaAt = cur.meta
        end
    end
    local base = dir and (dir .. "/") or ""
    if cur.atlas and base .. cur.atlas ~= cur._atlasAt then
        local path = normPath(base .. cur.atlas)
        local ok = pcall(function()
            local tex = TEXTURE:CreateTextureSync(path)
            if (tex.Width or 0) <= 0 then tex:Dispose(); error("missing") end
            scene:RegisterSpriteFromTexture(ATLAS_SPR, tex)
            scene:SetSpriteFilter(ATLAS_SPR, "mipmap")
            tex:Dispose()
        end)
        cur._atlasAt = ok and (base .. cur.atlas) or nil
    end
    if cur.mask and base .. cur.mask ~= cur._maskAt then
        ctx.maskId = loadTexture(world, base .. cur.mask, "terrain:grassmask", "linear")
        cur._maskAt = ctx.maskId and (base .. cur.mask) or nil
    end
    if not cur._atlasAt then
        scene:GrassTileEnable(false)
        return false
    end
    scene:GrassTileHeights(ctx.hf, ctx.minY, ctx.maxY, ctx.cs)
    if scene.GrassTileOwner then scene:GrassTileOwner(world.floorObj) end
    scene:GrassTileMaps(SPLAT_SPR, ctx.splatW, ctx.splatH, ctx.groundTex, ctx.uvScale,
        ctx.tintId or -1, ctx.maskId or -1, ctx.mapSize, ATLAS_SPR)
    local look = world._terrLook or {}
    local wx, wz = at(cur.wind, 1, 0.8), at(cur.wind, 2, 0.6)
    local wl = math.sqrt(wx * wx + wz * wz)
    if wl < 1e-6 then wx, wz, wl = 1, 0, 1 end
    scene:GrassTileParam("density", cur.density or 6, cur.quality or 1, cur.thin or 0.15, cur.patchSize or 11)
    scene:GrassTileParam("range", cur.fadeStart or 30, cur.fadeEnd or 75, cur.tilt or 0.35, cur.contrast or 0.8)
    scene:GrassTileParam("size", at(cur.width, 1, 1.05), at(cur.width, 2, 1.45), at(cur.height, 1, 0.65), at(cur.height, 2, 1.0))
    scene:GrassTileParam("wind", wx / wl, wz / wl, cur.windStrength or 0.3, cur.gustSize or 24)
    scene:GrassTileParam("wind2", cur.gustSpeed or 5, cur.flutter or 0.02, cur.alphaCut or 0.5, 0)
    scene:GrassTileParam("kind", cur.flowers or 0.06, cur.flowerPatch or 26, cur.dryPatch or 40, cur.dryAmount or 0.5)
    scene:GrassTileParam("atlas", cur.cols or 1, cur.rows or 1, cur.grassCells or 1, cur.flowerCells or 0)
    scene:GrassTileParam("tip", at(cur.tip, 1, 1.0), at(cur.tip, 2, 1.0), at(cur.tip, 3, 0.95), cur.rootShade or 0.85)
    scene:GrassTileParam("dry", at(cur.dry, 1, 1.12), at(cur.dry, 2, 1.04), at(cur.dry, 3, 0.78), cur.ownColour or 0.5)
    scene:GrassTileParam("ground", cur.minWeight or 0.45, cur.maxSlope or 0.7, look.scale2 or 0.137, look.macro or 0.6)
    local avg = cur.avg or {}
    scene:GrassTileParam("avg", (avg[1] or avg.r or 87) / 255, (avg[2] or avg.g or 131) / 255, (avg[3] or avg.b or 55) / 255, 0)
    local cells = cur.cells or {}
    local flat = type(cells[1]) == "number"
    for i = 0, 15 do
        local w, h
        if flat then w, h = cells[i * 2 + 1], cells[i * 2 + 2]
        else local c = cells[i + 1]; if type(c) == "table" then w, h = c.w or c[1], c.h or c[2] end end
        scene:GrassTileCell(i, w or 0.8, h or 0.5)
    end
    scene:GrassTileEnable(true)
    return true
end

function Terrain.wire(world, def, hf, layerTex, uvScale)
    local scene, terr = world.scene, def.terrain
    local groundTex = layerTex[1]
    local cs = terr.cellSize or 1
    local maps = terr.maps or {}
    local ctx = { hf = hf, minY = terr.minY or 0, maxY = terr.maxY or 20, cs = cs,
                  splatW = terr.w * cs, splatH = terr.h * cs, groundTex = groundTex, uvScale = uvScale,
                  mapSize = maps.size or ((terr.w - 1) * cs) }
    world._terrCtx, world._terrLook, world._grassTile = ctx, nil, nil
    if scene.ObjSetTerrainMaps and (maps.normal or maps.tint) then
        -- aniso: 0 = plain mipmaps, 2/4/16 = that level, else 8x
        local lv = terr.look and terr.look.aniso
        local aniso = lv ~= 0
        local filter = (not aniso and "mipmap") or ((lv == 2 or lv == 4 or lv == 16) and ("aniso" .. math.floor(lv))) or "aniso"
        if maps.normal then ctx.normalId = loadTexture(world, def.dir .. "/" .. maps.normal, "terrain:normal", filter) end
        if maps.tint then ctx.tintId = loadTexture(world, def.dir .. "/" .. maps.tint, "terrain:tint", filter) end
        if ctx.normalId or ctx.tintId then
            scene:ObjSetTerrainMaps(world.floorObj, ctx.normalId or -1, ctx.tintId or -1, ctx.mapSize)
            Terrain.setLook(world, terr.look)
            for i = 1, 4 do
                if aniso and layerTex[i] and (def.textures[terr.layers[i] or ""] or {}).filter == "mipmap" then
                    scene:SetTextureFilter(layerTex[i], filter)
                end
            end
        end
    end
    local tile = terr.grass and terr.grass.tile
    if tile then
        if maps.grass and tile.mask == nil then tile.mask = maps.grass end
        Terrain.setGrass(world, tile, def.dir)
    end
end

function Terrain.unwire(world)
    local scene, ctx = world.scene, world._terrCtx
    if scene.GrassTileEnable then scene:GrassTileEnable(false); scene:GrassTileHeights(nil, 0, 0, 1) end
    if scene.ObjSetTerrainMaps then scene:ObjSetTerrainMaps(world.floorObj, -1, -1, 1) end
    if ctx and scene.UnregisterTexture then
        if ctx.normalId then scene:UnregisterTexture(ctx.normalId) end
        if ctx.tintId then scene:UnregisterTexture(ctx.tintId) end
        if ctx.maskId then scene:UnregisterTexture(ctx.maskId) end
    end
    if world._grassTile and scene.UnregisterSprite then scene:UnregisterSprite(ATLAS_SPR) end
    world._terrCtx, world._terrLook, world._grassTile = nil, nil, nil
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
    -- layerRepeat: per world unit; nil = once per cell
    local layerRep = (terr.layerRepeat or 0) > 0 and terr.layerRepeat or nil

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
    -- density: blades per square unit; clumps scale with the cell
    local density = (gcfg.density or terr.grassDensity or 7) * cs * cs
    local clump = gcfg.clumpScale or 1                       -- 0 = uniform (legacy), >0 = noisy patches
    if density > 0 then clump = clump * cs end
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
    local function wireSplatShader()
        if splatMap and splatMap.RegisterSprite and scene.ObjSetTerrainSplat then
            splatMap:RegisterSprite(scene, SPLAT_SPR)
            scene:ObjSetTerrainSplat(world.floorObj, SPLAT_SPR,
                layerTex[1], layerTex[2], layerTex[3], layerTex[4],
                terr.w * cs, terr.h * cs, layerRep or (1 / cs))
            Terrain.wire(world, def, hf, layerTex, layerRep or (1 / cs))
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

    local quadRep = layerRep and layerRep * cs or 1
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
            scene:ObjAddQuadTex(world.floorObj, x0, h00, z0, x0, h01, z1, x1, h11, z1, x1, h10, z0, tex, quadRep, quadRep, 1.0)
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
