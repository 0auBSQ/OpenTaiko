---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- voxel/worldgen.lua — terrain + cave + ore + tree generation for one chunk (split out of Script.lua).
-- Caves are STANDARD-sized now: tighter worm tunnels + occasional caverns, only well below the surface
-- (the previous settings carved most of the map hollow).

local floor, min, max, abs = math.floor, math.min, math.max, math.abs
local sin = math.sin

local W = {}
local ctx
local SEED = 1337                       -- bound at init (hash3 reads it at module level)
function W.init(c) ctx = c; SEED = c.SEED or 1337 end
local function ckey(cx, cz) return cx .. "," .. cz end

-- Plant a tree on the grass block at (x, groundY): trunk sits directly on the grass,
-- with a rounded leaf canopy. Sizes/jitter use math.random (organic, no noise banding).
local function plantTree(x, z, groundY)
    local B, setVox, getVox = ctx.B, ctx.setVox, ctx.getVox
    local th = 4 + math.random(0, 2)             -- trunk height 4..6
    for i = 1, th do setVox(x, groundY + i, z, B.WOOD) end
    local topY = groundY + th
    for dy = -2, 2 do
        local cy = topY + dy
        local r = (dy <= -1) and 2 or (dy <= 1 and 2 or 1)
        for dx = -r, r do for dz = -r, r do
            if dx * dx + dz * dz <= r * r + 1 and getVox(x + dx, cy, z + dz) == B.AIR then
                setVox(x + dx, cy, z + dz, B.LEAVES)
            end
        end end
    end
    setVox(x, topY + 2, z, B.LEAVES)               -- crown tip
end

-- 3D value noise for caves (trilinear-interpolated integer hash — no sin, fast enough to run
-- over every underground cell at gen time).
local function hash3(x, y, z)
    local n = (x * 374761393 + y * 668265263 + z * 1274126177 + SEED * 9176)
    n = (n ~ (n >> 13)) * 1274126177
    n = n ~ (n >> 16)
    return (n & 0xFFFFFF) / 16777216.0
end
local function vnoise3(x, y, z)
    local x0, y0, z0 = floor(x), floor(y), floor(z)
    local fx, fy, fz = x - x0, y - y0, z - z0
    fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy); fz = fz * fz * (3 - 2 * fz)
    local c000, c100 = hash3(x0, y0, z0),     hash3(x0 + 1, y0, z0)
    local c010, c110 = hash3(x0, y0 + 1, z0), hash3(x0 + 1, y0 + 1, z0)
    local c001, c101 = hash3(x0, y0, z0 + 1), hash3(x0 + 1, y0, z0 + 1)
    local c011, c111 = hash3(x0, y0 + 1, z0 + 1), hash3(x0 + 1, y0 + 1, z0 + 1)
    local a = c000 + (c100 - c000) * fx
    local b = c010 + (c110 - c010) * fx
    local cc = c001 + (c101 - c001) * fx
    local dd = c011 + (c111 - c011) * fx
    local e = a + (b - a) * fy
    local f = cc + (dd - cc) * fy
    return e + (f - e) * fz
end
-- Winding "worm" tunnels: where two independent noise fields are both near their mid value,
-- their zero-sets intersect in tube-like channels — a classic, cheap cave-carving trick.
local CAVE_FREQ   = 0.05    -- bigger = tighter/twistier tunnels
local CAVE_THRESH = 0.038   -- bigger = more/wider caves (kept lean: cave surface area is the main
                            -- per-frame cost, since the engine rebuilds all visible faces each frame)
local function caveAt(x, y, z)
    local n1 = vnoise3(x * CAVE_FREQ,        y * CAVE_FREQ * 1.7,      z * CAVE_FREQ)
    local n2 = vnoise3(x * CAVE_FREQ + 53.3, y * CAVE_FREQ * 1.7 + 18.7, z * CAVE_FREQ + 71.1)
    return abs(n1 - 0.5) < CAVE_THRESH and abs(n2 - 0.5) < CAVE_THRESH
end

local function latNil(A, i) return A[i] or 0.5 end

-- Generate ONE chunk: terrain columns, BIG lattice-sampled caves (smooth "cheese" caverns that
-- grow with depth + winding worm tunnels), lava flooding the deep carves, per-chunk ore veins
-- (deterministic LCG so a chunk always regenerates identically), trees, and the skylight bake.
function W.genChunk(cx, cz)
    local B, CHUNK, WY, SEA_LEVEL, WCFG, ORE_DEFS = ctx.B, ctx.CHUNK, ctx.WY, ctx.SEA_LEVEL, ctx.WCFG, ctx.ORE_DEFS
    local terrainHeight, blocksLight = ctx.terrainHeight, ctx.blocksLight
    local chunkData, chunkSky, chunkLava, chunkYBounds = ctx.chunkData, ctx.chunkSky, ctx.chunkLava, ctx.chunkYBounds
    local key = ckey(cx, cz)
    if chunkData[key] then return end
    local d = {}
    chunkData[key] = d
    chunkSky[key] = {}
    chunkLava[key] = {}
    local SNOW_LEVEL = SEA_LEVEL + 18
    local STONE_PEAK = SEA_LEVEL + 26
    local bx, bz = cx * CHUNK, cz * CHUNK
    -- cave noise on a 4-block lattice (then trilinear per cell): big smooth caverns, cheap to sample
    local LS = 4
    local NXZ, NY = CHUNK / LS, floor(WY / LS)
    local S = NXZ + 1
    local latC, latA, latB = {}, {}, {}
    for iy = 0, NY do for iz = 0, NXZ do for ix = 0, NXZ do
        local wx, wy, wz = bx + ix * LS, iy * LS, bz + iz * LS
        local idx = (iy * S + iz) * S + ix + 1
        latC[idx] = vnoise3(wx * 0.022, wy * 0.034, wz * 0.022)            -- cheese caverns
        latA[idx] = vnoise3(wx * 0.055, wy * 0.09, wz * 0.055)             -- worm tunnels (pair)
        latB[idx] = vnoise3(wx * 0.055 + 53.3, wy * 0.09 + 18.7, wz * 0.055 + 71.1)
    end end end
    local function lat3(A, lx, y, lz)
        local fx, fy, fz = lx / LS, y / LS, lz / LS
        local ix, iy, iz = floor(fx), floor(fy), floor(fz)
        if iy > NY - 1 then iy = NY - 1 end
        fx = fx - ix; fy = fy - iy; fz = fz - iz
        local i000 = (iy * S + iz) * S + ix + 1
        local i010 = i000 + S * S
        local a00 = latNil(A, i000) + (latNil(A, i000 + 1) - latNil(A, i000)) * fx
        local a01 = latNil(A, i000 + S) + (latNil(A, i000 + S + 1) - latNil(A, i000 + S)) * fx
        local a10 = latNil(A, i010) + (latNil(A, i010 + 1) - latNil(A, i010)) * fx
        local a11 = latNil(A, i010 + S) + (latNil(A, i010 + S + 1) - latNil(A, i010 + S)) * fx
        local a0 = a00 + (a01 - a00) * fz
        local a1 = a10 + (a11 - a10) * fz
        return a0 + (a1 - a0) * fy
    end
    for lx = 0, CHUNK - 1 do
        for lz = 0, CHUNK - 1 do
            local wx, wz = bx + lx, bz + lz
            local h = min(terrainHeight(wx, wz), WY - 1)
            local beach = (h <= SEA_LEVEL + 1)
            local col = (lz * CHUNK) + lx + 1                 -- + y*CHUNK*CHUNK per level
            for y = 0, h do
                local b
                if y == 0 then b = B.BEDROCK
                elseif y < h - 3 then b = B.STONE
                elseif y == h then
                    if beach then b = B.SAND
                    elseif h >= STONE_PEAK then b = B.STONE
                    elseif h >= SNOW_LEVEL then b = B.SNOW
                    else b = B.GRASS end
                else
                    if beach then b = B.SAND
                    elseif h >= STONE_PEAK then b = B.STONE
                    else
                        b = B.DIRT
                        if math.random() < 0.02 then b = B.COPPER_DIRT end
                    end
                end
                -- carve caves: cheese threshold loosens with depth (bigger caverns down deep);
                -- worm pair gives long winding tunnels. Deep carves flood with B.LAVA.
                if y >= 2 and y <= h - 5 then
                    local boost = min(0.05, (y < SEA_LEVEL - 14) and (SEA_LEVEL - 14 - y) * 0.0016 or 0)
                    local carve = lat3(latC, lx, y, lz) > 0.74 - boost
                    if not carve then
                        local a = lat3(latA, lx, y, lz) - 0.5
                        if a < 0.046 and a > -0.046 then
                            local bb = lat3(latB, lx, y, lz) - 0.5
                            carve = bb < 0.046 and bb > -0.046
                        end
                    end
                    if carve then
                        if y <= WCFG.LAVA_Y then
                            b = B.LAVA
                            if y == WCFG.LAVA_Y and lx % 6 == 1 and lz % 6 == 1 then
                                chunkLava[key][#chunkLava[key] + 1] = { wx, y + 1, wz }
                            end
                        else
                            b = B.AIR
                        end
                    end
                end
                d[y * 256 + col] = b
            end
            for y = h + 1, SEA_LEVEL do d[y * 256 + col] = B.WATER end
            for y = max(h, SEA_LEVEL) + 1, WY - 1 do d[y * 256 + col] = B.AIR end
        end
    end
    -- ore veins: deterministic per-chunk LCG → the same chunk always rolls the same veins
    local seed = (cx * 73856093 + cz * 19349663 + SEED * 83492791) % 2147483647
    if seed <= 0 then seed = seed + 2147483646 end
    local function rng() seed = (seed * 48271) % 2147483647; return seed / 2147483647 end
    for _, m in ipairs(ORE_DEFS) do
        local veins = m.rate * CHUNK * CHUNK
        local nv = floor(veins) + ((rng() < (veins - floor(veins))) and 1 or 0)
        for _ = 1, nv do
            local lx = floor(rng() * CHUNK); local lz = floor(rng() * CHUNK)
            local y = m.yMin + floor(rng() * (m.yMax - m.yMin + 1))
            for _ = 1, m.smin + floor(rng() * (m.smax - m.smin + 1)) do
                if lx >= 0 and lx < CHUNK and lz >= 0 and lz < CHUNK and y >= 1 and y < WY - 1 then
                    local idx = y * 256 + (lz * CHUNK) + lx + 1
                    if d[idx] == B.STONE then d[idx] = m.id end
                else break end
                local dr = floor(rng() * 6)
                if dr == 0 then lx = lx + 1 elseif dr == 1 then lx = lx - 1
                elseif dr == 2 then y = y + 1 elseif dr == 3 then y = y - 1
                elseif dr == 4 then lz = lz + 1 else lz = lz - 1 end
            end
        end
    end
    -- trees: trunks ≥2 cells from the chunk border so the canopy never crosses into a neighbour
    for lx = 2, CHUNK - 3 do
        for lz = 2, CHUNK - 3 do
            if rng() < 0.020 then
                local col = (lz * CHUNK) + lx + 1
                local y = 0
                for yy = WY - 1, 1, -1 do
                    local b = d[yy * 256 + col]
                    if b ~= B.AIR and b ~= B.WATER then y = yy; break end
                end
                if d[y * 256 + col] == B.GRASS and y < SNOW_LEVEL then plantTree(bx + lx, bz + lz, y) end
            end
        end
    end
    -- skylight bake: walk each column top-down over this chunk's own block array DIRECTLY (no per-cell
    -- getVox string keys, unlike recomputeSkyColumn), recording the height above the first light-blocker.
    -- Also capture the chunk's highest non-AIR block (yMax) so meshing can skip the all-air ceiling.
    local AIR = B.AIR
    local sk = chunkSky[key]
    local yMax = -1
    for lx = 0, CHUNK - 1 do
        for lz = 0, CHUNK - 1 do
            local col = (lz * CHUNK) + lx + 1
            local top = 0
            local seen = false
            for y = WY - 1, 0, -1 do
                local bl = d[y * 256 + col]
                if not seen and bl ~= AIR then seen = true; if y > yMax then yMax = y end end
                if blocksLight(bl) then top = y + 1; break end
            end
            sk[col] = top
        end
    end
    chunkYBounds[key] = yMax
    return #chunkLava[key] > 0
end



W.terrainHeight = nil  -- (Script keeps its own copy; spawn + textures use it)

return W
