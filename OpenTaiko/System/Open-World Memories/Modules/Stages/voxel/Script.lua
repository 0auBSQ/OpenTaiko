---@diagnostic disable: undefined-global, undefined-field, need-check-nil, lowercase-global
-- voxel/Script.lua — a creative-mode voxel engine running as a Lua stage.
--
-- INFINITE chunked terrain (generated on demand around the player) rendered by the SCENE3D
-- rasterizer; big lattice-noise caves with glowing LAVA pools; level-based FLOWING water.
-- CREATIVE building: left-click breaks any block instantly, right-click places the selected
-- block (infinite), and E opens a palette of every block to fill the 9-slot hotbar.
--
-- Controls:
--   Move: WASD    Jump/swim: Space    Sprint: LCtrl    Look: mouse
--   Break: L-click    Place: R-click    Block palette: E    Hotbar: wheel + 1-9    Pause: Esc

-- ════════════════════════════════════════════════════════════════════════════════
-- Config
-- ════════════════════════════════════════════════════════════════════════════════

local SCREEN_W, SCREEN_H = 1920, 1080
local RW, RH    = 1920, 1080       -- internal resolution (raster fill is native; tune freely)

local FOV       = 70.0
local NEAR      = 0.06
local MOUSE_SENS = 0.16          -- deg per mouse pixel
local ARROW_TURN = 90.0          -- deg/sec for arrow-key look

-- view distance + fog (chunks past RENDER_DIST are culled; terrain fades to fog over
-- [FOG_START, FOG_END] so the cull boundary isn't a hard pop). Lower RENDER_DIST = faster.
local RENDER_DIST = 60.0
local FOG_START   = 38.0
local FOG_END     = 60.0
local THREADS     = 8            -- rasterizer threads (screen split into this many row bands)

-- physics
local GRAV       = 28.0
local JUMP_VEL   = 8.6
local WALK_SPEED = 5.2
local SPRINT_MULT = 1.7
local EYE        = 1.62
local PW         = 0.3           -- player half-width
local PH         = 1.8           -- player height
local REACH      = 5.0           -- block-break reach
-- underwater
local WATER_GRAV = 6.0
local SWIM_UP    = 4.2
local WATER_MOVE = 0.6
local WATER_DRAG = 0.86

-- INFINITE world: chunks (16×WY×16) generate on demand around the player and never end. WY is the
-- world height; SEA_LEVEL the water table; WCFG.LAVA_Y the cave lava table (carved caves below it
-- flood with glowing lava). WCFG packs the streaming knobs into one table (200-locals limit).
local WY         = 128
local SEA_LEVEL  = 64
local SEED       = 1337
local WCFG = {
    LAVA_Y      = 12,    -- caves carved at/below this height fill with lava
    GEN_CHUNKS  = 5,     -- generate chunks within this chunk radius of the player
    MESH_UNLOAD = 7,     -- unmesh (free scene geometry of) chunks beyond this radius
    genBudget   = 3,     -- adaptive mesh direction-steps per frame (dt-feedback in updateStreaming)
    remeshQ = {},        -- deferred chunk remeshes (streaming + water put keys here)
    yBounds = {},        -- [key] = highest non-AIR y in the chunk (mesh Y-bound: skips the all-air ceiling)
    meshJobs = {},       -- FIFO queue of in-progress incremental mesh jobs (one chunk meshed across frames)
    meshHead = 1,        -- index of the job currently being stepped in meshJobs
    meshSet = {},        -- [key] = the job, while a chunk is queued/in-progress (dedup + stale-cancel)
    ready = false,       -- world generated yet? (deferred to the first activate)
}

local AIR, GRASS, DIRT, STONE, SAND, WATER, WOOD, LEAVES, TORCH, BEDROCK = 0, 1, 2, 3, 4, 5, 6, 7, 8, 9
local SNOW, GLASS, COPPER_DIRT = 10, 11, 42   -- COPPER_DIRT: copper showing in the dirt layer
local LAVA, FURNACE, PLANKS, COAL_ORE = 45, 46, 47, 48   -- lava pools / smelting / crafted planks / coal

-- texture ids in one table (keeps the chunk under Lua's 200-locals limit)
local TX = { grasstop = 1, grassside = 2, dirt = 3, stone = 4, sand = 5, logtop = 6,
             logside = 7, leaves = 8, torch = 9, bedrock = 10, snow = 11, glass = 12,
             snowside = 28, copperdirt = 44,
             lava = 70, furnace = 71, furnacetop = 72, planks = 73, coalore = 74 }
local GLASS_A = 120   -- glass transparency (0..255)

-- Ores & gems live in one table (so we stay under Lua's 200-locals-per-chunk limit). Each:
-- ore block id+tex+name, refined "Block of X" id (bid) + tex (btex), speckle colour {r,g,b}
-- (+ col2 for two-tone), optional ore base colour, gem flag, depth band [yMin,yMax], vein rate
-- (veins ≈ rate*WX*WZ) and vein size range. "dirt" = also sprinkles into the dirt layer.
-- Values are flavourful, not geologically accurate.
local ORE_DEFS = {
    { name = "Copper",      id = 12, tex = 13, bid = 27, btex = 29, col = {184,115,51},  yMin = 24, yMax = 92, rate = 0.038,  smin = 10, smax = 20 },
    { name = "Zinc",        id = 13, tex = 14, bid = 28, btex = 30, col = {150,165,180}, yMin = 18, yMax = 80, rate = 0.035,  smin = 10, smax = 20 },
    { name = "Iron",        id = 14, tex = 15, bid = 29, btex = 31, col = {184,138,110}, yMin = 10, yMax = 72, rate = 0.034,  smin = 9,  smax = 18 },
    { name = "Tin",         id = 15, tex = 16, bid = 30, btex = 32, col = {196,198,205}, yMin = 14, yMax = 76, rate = 0.032,  smin = 9,  smax = 18 },
    { name = "Silver",      id = 16, tex = 17, bid = 31, btex = 33, col = {222,222,232}, yMin = 8,  yMax = 46, rate = 0.014,  smin = 7,  smax = 14 },
    { name = "Gold",        id = 17, tex = 18, bid = 32, btex = 34, col = {232,192,60},  yMin = 5,  yMax = 36, rate = 0.0095, smin = 6,  smax = 12 },
    { name = "Platinum",    id = 18, tex = 19, bid = 33, btex = 35, col = {205,210,220}, yMin = 3,  yMax = 26, rate = 0.0050, smin = 5,  smax = 10 },
    { name = "Mithril",     id = 19, tex = 20, bid = 34, btex = 36, col = {90,220,214},  yMin = 2,  yMax = 18, rate = 0.0032, smin = 4,  smax = 9 },
    { name = "OpenTaikium", id = 20, tex = 21, bid = 35, btex = 37, col = {220,40,55}, col2 = {45,80,225}, yMin = 1, yMax = 14, rate = 0.0015, smin = 4, smax = 8 },
    { name = "Copium",      id = 21, tex = 22, bid = 36, btex = 38, col = {20,120,44},   yMin = 1,  yMax = 42, rate = 0.0070, smin = 5,  smax = 10 },
    { name = "Sapphire", gem = true, id = 22, tex = 23, bid = 37, btex = 39, col = {45,95,225},   yMin = 5,  yMax = 42, rate = 0.0060,  smin = 2, smax = 5 },
    { name = "Ruby",     gem = true, id = 23, tex = 24, bid = 38, btex = 40, col = {214,45,66},   yMin = 5,  yMax = 42, rate = 0.0060,  smin = 2, smax = 5 },
    { name = "Emerald",  gem = true, id = 24, tex = 25, bid = 39, btex = 41, col = {34,194,96},   yMin = 8,  yMax = 46, rate = 0.0042,  smin = 2, smax = 5 },
    { name = "Quartz",   gem = true, id = 25, tex = 26, bid = 40, btex = 42, col = {236,222,228}, yMin = 10, yMax = 70, rate = 0.0140,  smin = 3, smax = 6 },
    { name = "Diamond",  gem = true, id = 26, tex = 27, bid = 41, btex = 43, col = {150,232,242}, yMin = 2,  yMax = 18, rate = 0.0024,  smin = 2, smax = 5 },
    -- the periodic-table expansion (+ Coal, which fuels the new furnaces). coal=true: drops the coal
    -- ITEM when mined (like gems) instead of the ore block.
    { name = "Coal", coal = true, id = 48, tex = 74, bid = 49, btex = 66, col = {52,52,58},   yMin = 16, yMax = 100, rate = 0.050, smin = 12, smax = 24 },
    { name = "Titanium",  id = 60, tex = 75, bid = 61, btex = 76, col = {180,190,200}, yMin = 6,  yMax = 40, rate = 0.010,  smin = 5, smax = 10 },
    { name = "Chromium",  id = 62, tex = 77, bid = 63, btex = 78, col = {170,205,215}, yMin = 8,  yMax = 50, rate = 0.012,  smin = 5, smax = 10 },
    { name = "Cobalt",    id = 64, tex = 79, bid = 65, btex = 80, col = {60,90,210},   yMin = 6,  yMax = 44, rate = 0.011,  smin = 5, smax = 10 },
    { name = "Tungsten",  id = 66, tex = 81, bid = 67, btex = 82, col = {120,118,130}, yMin = 2,  yMax = 24, rate = 0.006,  smin = 4, smax = 8 },
    { name = "Aluminium", id = 68, tex = 83, bid = 69, btex = 84, col = {205,210,218}, yMin = 20, yMax = 80, rate = 0.020,  smin = 7, smax = 14 },
    { name = "Nickel",    id = 70, tex = 85, bid = 71, btex = 86, col = {160,156,130}, yMin = 10, yMax = 60, rate = 0.014,  smin = 6, smax = 11 },
    { name = "Lead",      id = 72, tex = 87, bid = 73, btex = 88, col = {96,100,116},  yMin = 4,  yMax = 40, rate = 0.012,  smin = 5, smax = 10 },
    { name = "Uranium",   id = 74, tex = 89, bid = 75, btex = 90, col = {110,230,80},  yMin = 1,  yMax = 16, rate = 0.0035, smin = 3, smax = 7 },
}
local oreTexOf = {}   -- [blockId] = textureId (ore blocks AND refined "Block of X"), for texIdFor
for _, m in ipairs(ORE_DEFS) do oreTexOf[m.id] = m.tex; oreTexOf[m.bid] = m.btex end

-- lighting: faces bake a sky-exposure value (0..1); the engine sun is gated by it and a global
-- ambient fills the rest. Torch point lights add on top.
local LIGHT_MAX   = 15       -- full skylight
local SKY_FALLOFF = 3        -- skylight lost per block of vertical cover (0 = hard shadow)
-- Faces with baked light <= LIT_MERGE_LITE (caves/shade) cap their greedy-merge size at
-- LIT_MERGE_CAP blocks so torch point lights get enough vertices to look right. Bright faces
-- merge freely. Lower the cap for nicer torch light, raise it (or the threshold) for more speed.
local LIT_MERGE_LITE = 8
local LIT_MERGE_CAP  = 4

local SKY_TOP_R, SKY_TOP_G, SKY_TOP_B = 70, 130, 225
local SKY_HOR_R, SKY_HOR_G, SKY_HOR_B = 200, 225, 255
local WATER_R, WATER_G, WATER_B, WATER_A = 46, 110, 175, 150

local floor, sqrt, abs, min, max = math.floor, math.sqrt, math.abs, math.min, math.max
local sin = math.sin
local Inv = require("inventory")   -- creative block palette + hotbar

-- ════════════════════════════════════════════════════════════════════════════════
-- State
-- ════════════════════════════════════════════════════════════════════════════════

local scene, dim = nil, nil
local fontBig, fontMid, fontSmall = nil, nil, nil

local inLava = false   -- feet/eye in cave lava (red tint; you wade through it)

-- INFINITE chunked world: 16×WY×16 chunks generate on demand around the player (chunkData),
-- mesh into per-chunk retained scene objects (chunks) and unmesh when far. Block data persists
-- for the whole session once generated, so edits survive leaving and returning.
local CHUNK = 16
local chunkData = {}   -- [key] = flat block array (CHUNK*WY*CHUNK)
local chunkSky  = {}   -- [key] = per-column "lowest y that still sees sky" (skylight bake)
local chunks    = {}   -- meshed chunks: [key] = { cx, cz, objTex = {6}, objWater, objGlass }
local chunkLava = {}   -- [key] = gen-time lava-light anchors { {x,y,z}, ... }
-- streaming scratch (genOrder/remeshQ) lives in WCFG: the file rides Lua's 200-local limit
local function ckey(cx, cz) return cx .. "," .. cz end

-- player
local feetX, feetY, feetZ = 8, 76, 8
local vx, vy, vz = 0, 0, 0
local onGround = false
local camX, camY, camZ = 0, 0, 0
local yaw, pitch = 45.0, -18.0
local spawnX, spawnY, spawnZ = 8, 24, 8             -- starting spot (Reset position)
local spawnYaw, spawnPitch = 45.0, -18.0
-- camera basis mirrored from the scene (forward + right's horizontal components,
-- the only parts Lua needs for movement / picking / face-culling)
local Fx, Fy, Fz = 0, 0, 1
local Rx, Rz = 1, 0
local underwater = false

-- block selection
local hasSel = false
local selX, selY, selZ = 0, 0, 0
local selNx, selNy, selNz = 0, 1, 0

-- mining/placing: both auto-repeat while the button is held (creative = instant break, no tools)
local EDIT_INTERVAL = 0.16        -- seconds between edits while a mouse button is held
local placeTimer = 0
local breakTimer = 0              -- same repeat cadence for breaking

-- pause menu
local paused = false
local menuItems = { "Resume", "Reset position", "Quit" }
local menuSel = 1

local lastTs = 0
local showHelp = true

-- ════════════════════════════════════════════════════════════════════════════════
-- Noise + generation
-- ════════════════════════════════════════════════════════════════════════════════

local function hash2(x, z, s)
    local n = sin(x * 127.1 + z * 311.7 + s * 0.137) * 43758.5453
    return n - floor(n)
end
local function vnoise(x, z)
    local x0, z0 = floor(x), floor(z)
    local fx, fz = x - x0, z - z0
    local v00 = hash2(x0, z0, SEED); local v10 = hash2(x0 + 1, z0, SEED)
    local v01 = hash2(x0, z0 + 1, SEED); local v11 = hash2(x0 + 1, z0 + 1, SEED)
    local sx = fx * fx * (3 - 2 * fx); local sz = fz * fz * (3 - 2 * fz)
    local a = v00 + (v10 - v00) * sx
    local b = v01 + (v11 - v01) * sx
    return a + (b - a) * sz
end
-- Terrain spans from TERRAIN_BELOW blocks under sea level (valley floors / lake beds) up to
-- TERRAIN_ABOVE blocks over it (hilltops), so it always straddles SEA_LEVEL whatever it's set to.
local TERRAIN_BELOW = 14
local TERRAIN_ABOVE = 30
local function terrainHeight(x, z)
    local n = vnoise(x * 0.045, z * 0.045) * 1.0 + vnoise(x * 0.11, z * 0.11) * 0.45 + vnoise(x * 0.23, z * 0.23) * 0.22
    local h = floor((SEA_LEVEL - TERRAIN_BELOW) + (n / 1.67) * (TERRAIN_BELOW + TERRAIN_ABOVE))
    if h > WY - 10 then h = WY - 10 end   -- leave headroom for trees
    return h
end

local function getVox(x, y, z)
    if y < 0 or y >= WY then return AIR end
    local cx, cz = floor(x / CHUNK), floor(z / CHUNK)
    local d = chunkData[cx .. "," .. cz]
    if not d then return AIR end
    return d[((y * CHUNK) + (z - cz * CHUNK)) * CHUNK + (x - cx * CHUNK) + 1]
end
local function setVox(x, y, z, b)
    if y < 0 or y >= WY then return end
    local cx, cz = floor(x / CHUNK), floor(z / CHUNK)
    local d = chunkData[cx .. "," .. cz]
    if not d then return end
    d[((y * CHUNK) + (z - cz * CHUNK)) * CHUNK + (x - cx * CHUNK) + 1] = b
end

-- ════════════════════════════════════════════════════════════════════════════════
-- Block predicates + lighting
-- ════════════════════════════════════════════════════════════════════════════════

-- Solid for collision. Air, water and torches are walk-through; glass is solid (you bump it).
local function isSolid(b) return b ~= AIR and b ~= WATER and b ~= TORCH and b ~= LAVA end
-- Blocks skylight / torch light. Same as solid but glass lets light through.
local function blocksLight(b) return b ~= AIR and b ~= WATER and b ~= TORCH and b ~= GLASS end
-- A face is drawn when the neighbour is see-through (air, water, torch or glass).
-- See-through for face culling: a neighbour that doesn't fully hide the face behind it. Leaves
-- count (they're cutout), so blocks next to leaves still draw their face (no see-through holes
-- into solid blocks) and leaves draw faces against each other (holes reveal the leaves behind).
local function isTransparent(b) return b == AIR or b == WATER or b == LAVA or b == TORCH or b == GLASS or b == LEAVES end
-- Targetable by the crosshair (so torches/glass can be broken, water/air cannot).
local function isSelectable(b) return b ~= AIR and b ~= WATER and b ~= LAVA end

-- Skylight is a cheap per-column value (a cell is lit by sky only if nothing solid is above it
-- in its column), so tunnels/overhangs go dark — it's baked into each face's shade. Torch light
-- is now produced by the engine's forward point lights (each with a small per-torch occupancy
-- grid so it casts real shadows), so there is no torch flood-fill here any more.
local torches = {}    -- [key string] = {x,y,z} of each torch (for the posts + the point lights)
local torchesDirty = false   -- set when a torch/lava chunk changes → rebuild posts + lights

local function recomputeSkyColumn(x, z)
    local cx, cz = floor(x / CHUNK), floor(z / CHUNK)
    local sk = chunkSky[cx .. "," .. cz]; if not sk then return end
    local top = 0
    for y = WY - 1, 0, -1 do
        if blocksLight(getVox(x, y, z)) then top = y + 1; break end
    end
    sk[(z - cz * CHUNK) * CHUNK + (x - cx * CHUNK) + 1] = top
end

local function skyAt(x, y, z)
    if y >= WY then return LIGHT_MAX end
    if y < 0 then return 0 end
    local cx, cz = floor(x / CHUNK), floor(z / CHUNK)
    local sk = chunkSky[cx .. "," .. cz]
    if not sk then return LIGHT_MAX end                      -- ungenerated frontier: assume lit
    local top = sk[(z - cz * CHUNK) * CHUNK + (x - cx * CHUNK) + 1] or 0
    if y >= top then return LIGHT_MAX end
    -- gradual falloff with depth of cover, so a block just under another isn't pitch black
    local v = LIGHT_MAX - (top - y) * SKY_FALLOFF
    return v > 0 and v or 0
end

-- World generation lives in worldgen.lua (split for readability); genChunk wraps it so the
-- torch/lava light rebuild flag stays a Script-local.
local Worldgen = require("worldgen")
Worldgen.init({
    B = { AIR = AIR, GRASS = GRASS, DIRT = DIRT, STONE = STONE, SAND = SAND, WATER = WATER, WOOD = WOOD,
          LEAVES = LEAVES, BEDROCK = BEDROCK, SNOW = SNOW, COPPER_DIRT = COPPER_DIRT, LAVA = LAVA },
    CHUNK = CHUNK, WY = WY, SEA_LEVEL = SEA_LEVEL, SEED = SEED, WCFG = WCFG, ORE_DEFS = ORE_DEFS,
    chunkData = chunkData, chunkSky = chunkSky, chunkLava = chunkLava, chunkYBounds = WCFG.yBounds,
    terrainHeight = terrainHeight, recomputeSkyColumn = recomputeSkyColumn, blocksLight = blocksLight,
    setVox = setVox, getVox = getVox,
})
local function genChunk(cx, cz)
    if Worldgen.genChunk(cx, cz) then torchesDirty = true end
end

-- ════════════════════════════════════════════════════════════════════════════════
-- Textures
-- ════════════════════════════════════════════════════════════════════════════════

local function packc(r, g, b)
    if r < 0 then r = 0 elseif r > 255 then r = 255 end
    if g < 0 then g = 0 elseif g > 255 then g = 255 end
    if b < 0 then b = 0 elseif b > 255 then b = 255 end
    return floor(r) * 65536 + floor(g) * 256 + floor(b)
end
local function texFlat(r, g, b, amp, salt)
    local t = {}
    for v = 0, 15 do for u = 0, 15 do
        local n = (hash2(u + salt, v - salt, salt) - 0.5) * 2 * amp
        t[v * 16 + u + 1] = packc(r + n, g + n, b + n)
    end end
    return t
end
-- Ore/gem texture over a base (stone, or m.base). Ores: fine scattered dots plus a few short
-- random-walk veins that link some of them into organic chunks. Gems: a faceted central
-- crystal (4 facets + a sparkle) with a couple of tiny chips.
local function texMineral(m, salt)
    local bc = m.base or {122, 122, 128}
    local bright = m.gem and 30 or 0
    local t = {}
    for v = 0, 15 do for u = 0, 15 do
        local sn = (hash2(u + 11, v - 9, salt) - 0.5) * 2 * 22
        t[v * 16 + u + 1] = packc(bc[1] + sn, bc[2] + sn, bc[3] + sn)
    end end
    local function pix(x, y, col, extra)
        if x < 0 or x > 15 or y < 0 or y > 15 then return end
        local hn = (hash2(x, y, salt + 57) - 0.5) * 22 + (extra or 0)
        t[y * 16 + x + 1] = packc(col[1] + hn + bright, col[2] + hn + bright, col[3] + hn + bright)
    end
    if m.gem then
        local cx, cy = 7, 8                                    -- faceted central crystal (diamond)
        for v = 3, 13 do
            local hw = 5 - floor(abs(v - 8) * 0.85)
            if hw >= 0 then
                for u = cx - hw, cx + hw do
                    local facet = (u <= cx and v <= cy) and 22 or (u > cx and v <= cy) and 6
                        or (u <= cx and v > cy) and -8 or -22
                    pix(u, v, m.col, facet)
                end
            end
        end
        pix(cx - 1, 5, m.col, 64)                             -- sparkle
        for k = 1, 3 do                                        -- a couple of tiny chips
            pix(1 + floor(hash2(k, 7, salt) * 14), 1 + floor(hash2(k, 13, salt) * 14), m.col)
        end
    else
        for v = 0, 15 do for u = 0, 15 do                      -- fine scattered dots
            if hash2(u, v, salt + 131) < 0.10 then
                pix(u, v, (m.col2 and hash2(u, v, salt + 260) < 0.5) and m.col2 or m.col)
            end
        end end
        for k = 1, 4 do                                        -- short veins linking some dots
            local x, y = 2 + floor(hash2(k, 11, salt) * 12), 2 + floor(hash2(k, 17, salt) * 12)
            local col = (m.col2 and k % 2 == 0) and m.col2 or m.col
            for s = 0, 4 + floor(hash2(k, 23, salt) * 5) do
                pix(x, y, col)
                local d = floor(hash2(x + y * 16, s + 1, salt + 5) * 4)
                if d == 0 then x = x + 1 elseif d == 1 then x = x - 1 elseif d == 2 then y = y + 1 else y = y - 1 end
                if x < 0 then x = 0 elseif x > 15 then x = 15 end
                if y < 0 then y = 0 elseif y > 15 then y = 15 end
            end
        end
    end
    return t
end
-- Refined "Block of X": a beveled, metallic-looking solid — bright top/left edge, dark
-- bottom/right, diagonal sheen. OpenTaikium's block is red on the top half, blue on the bottom.
local function texSolid(m, salt)
    local c1, c2 = m.col, m.col2 or m.col
    local t = {}
    for v = 0, 15 do for u = 0, 15 do
        local c = m.col2 and (v < 8 and c1 or c2) or c1
        local n = (hash2(u + salt, v - salt, salt) - 0.5) * 2 * 8
        local sheen = (u + v < 13) and 24 or (u + v > 17 and -24 or 0)
        local r, g, b = c[1] + n + 16 + sheen, c[2] + n + 16 + sheen, c[3] + n + 16 + sheen
        if u == 0 or v == 0 then r, g, b = r + 36, g + 36, b + 36        -- highlight frame
        elseif u == 15 or v == 15 then r, g, b = r - 40, g - 40, b - 40 end -- shadow frame
        t[v * 16 + u + 1] = packc(r, g, b)
    end end
    return t
end
local texPix = {}   -- [texId] = packed-RGB pixel table (kept for hotbar icon previews)
local function registerTextures()
    local function reg(id, t) texPix[id] = t; scene:RegisterTexture(id, t, 16, 16) end
    reg(TX.grasstop, texFlat(96, 152, 60, 22, 3))
    reg(TX.dirt, texFlat(134, 96, 67, 20, 7))
    reg(TX.stone, texFlat(122, 122, 128, 22, 11))
    reg(TX.sand, texFlat(214, 201, 146, 16, 17))
    local leaves = {}
    for v = 0, 15 do for u = 0, 15 do
        local n = (hash2(u, v, 23) - 0.5) * 40
        -- scattered holes (cutout): a negative texel reads as transparent in the rasterizer, so
        -- the leaf block shows see-through gaps. Keep the border mostly solid so the leaf
        -- texture still tiles as a recognisable block.
        if hash2(u, v, 63) < 0.16 then
            leaves[v * 16 + u + 1] = -1
        else
            local dark = hash2(u, v, 41) < 0.18 and -26 or 0
            leaves[v * 16 + u + 1] = packc(60 + n + dark, 118 + n + dark, 46 + n + dark)
        end
    end end
    reg(TX.leaves, leaves)
    local gs = {}
    for v = 0, 15 do for u = 0, 15 do
        local n = (hash2(u + 3, v - 3, 3) - 0.5) * 2 * 20
        if v <= 3 then
            local edge = (v == 3) and -14 or 0
            gs[v * 16 + u + 1] = packc(96 + n + edge, 152 + n + edge, 60 + n + edge)
        else
            gs[v * 16 + u + 1] = packc(134 + n, 96 + n, 67 + n)
        end
    end end
    reg(TX.grassside, gs)
    local lsd = {}
    for v = 0, 15 do for u = 0, 15 do
        local streak = (u % 4 == 0) and -22 or 0
        local n = (hash2(u, v, 5) - 0.5) * 16
        lsd[v * 16 + u + 1] = packc(120 + n + streak, 85 + n + streak, 50 + n + streak)
    end end
    reg(TX.logside, lsd)
    local lt = {}
    for v = 0, 15 do for u = 0, 15 do
        local d = sqrt((u - 7.5) ^ 2 + (v - 7.5) ^ 2)
        local ring = (floor(d) % 2 == 0) and 18 or -10
        local n = (hash2(u, v, 9) - 0.5) * 10
        lt[v * 16 + u + 1] = packc(165 + ring + n, 125 + ring + n, 78 + ring + n)
    end end
    reg(TX.logtop, lt)
    local bd = {}
    for v = 0, 15 do for u = 0, 15 do
        local n = (hash2(u, v, 13) - 0.5) * 2 * 26
        local blot = hash2(u + 5, v + 5, 71) < 0.22 and -28 or 0
        bd[v * 16 + u + 1] = packc(58 + n + blot, 58 + n + blot, 64 + n + blot)
    end end
    reg(TX.bedrock, bd)
    -- torch: a full-width vertical post — flame at the top, wooden stick below (the post
    -- geometry is slim, so the whole texture is the visible stick/flame).
    local tc = {}
    for v = 0, 15 do for u = 0, 15 do
        local r, g, b
        if v <= 5 then                                   -- flame (top)
            local fn = (hash2(u, v, 88) - 0.5) * 50
            local core = (v <= 2) and 60 or 0
            r, g, b = 255, 170 + fn + core, 30 + fn
        else                                             -- wooden stick
            local sn = (hash2(u, v, 19) - 0.5) * 18
            local grain = (u % 5 == 0) and -16 or 0
            r, g, b = 122 + sn + grain, 82 + sn + grain, 44 + sn + grain
        end
        tc[v * 16 + u + 1] = packc(r, g, b)
    end end
    reg(TX.torch, tc)
    reg(TX.snow, texFlat(238, 244, 252, 9, 31))
    -- glass: light frame so panes read as panes; the see-through comes from the object alpha.
    local gl = {}
    for v = 0, 15 do for u = 0, 15 do
        if u == 0 or u == 15 or v == 0 or v == 15 then
            gl[v * 16 + u + 1] = packc(176, 202, 214)
        else
            local n = (hash2(u, v, 44) - 0.5) * 10
            gl[v * 16 + u + 1] = packc(206 + n, 228 + n, 236 + n)
        end
    end end
    reg(TX.glass, gl)
    -- snow side: dirt with a snowy top band (like the grass side) so snow blocks look capped
    local ss = {}
    for v = 0, 15 do for u = 0, 15 do
        local n = (hash2(u + 3, v - 3, 3) - 0.5) * 2 * 18
        if v <= 3 then
            local edge = (v == 3) and -16 or 0
            ss[v * 16 + u + 1] = packc(232 + n + edge, 238 + n + edge, 248 + n + edge)
        else
            ss[v * 16 + u + 1] = packc(134 + n, 96 + n, 67 + n)
        end
    end end
    reg(TX.snowside, ss)
    reg(TX.copperdirt, texMineral({ col = {184,115,51}, base = {134,96,67} }, 999))  -- copper in dirt
    for _, m in ipairs(ORE_DEFS) do
        reg(m.tex, texMineral(m, m.id))        -- ore in the ground
        reg(m.btex, texSolid(m, m.id + 100))   -- refined "Block of X"
    end
    -- LAVA: dark crust with bright glowing cracks (meshed full-bright, so the cracks burn)
    local lv = {}
    for v = 0, 15 do for u = 0, 15 do
        local crack = sin(u * 0.9 + sin(v * 0.7) * 2.0) + sin(v * 1.1)
        local glow = crack > 0 and crack or 0
        lv[v * 16 + u + 1] = packc(90 + glow * 160, 26 + glow * 95, 14 + glow * 14)
    end end
    reg(TX.lava, lv)
    -- FURNACE: stone body, dark mouth with embers on the side, vents on top
    local fu = {}
    for v = 0, 15 do for u = 0, 15 do
        local n = (hash2(u, v, 77) - 0.5) * 2 * 16
        local c = packc(108 + n, 108 + n, 114 + n)
        if v >= 9 and v <= 13 and u >= 4 and u <= 11 then
            c = (v >= 11 and hash2(u, v, 91) < 0.5) and packc(235, 120, 30) or packc(24, 22, 24)   -- mouth + embers
        end
        fu[v * 16 + u + 1] = c
    end end
    reg(TX.furnace, fu)
    local ft = {}
    for v = 0, 15 do for u = 0, 15 do
        local n = (hash2(u, v, 79) - 0.5) * 2 * 14
        local vent = ((u % 5 == 2) and v >= 3 and v <= 12) and -36 or 0
        ft[v * 16 + u + 1] = packc(96 + n + vent, 96 + n + vent, 102 + n + vent)
    end end
    reg(TX.furnacetop, ft)
    -- PLANKS: warm boards with seams
    local pl = {}
    for v = 0, 15 do for u = 0, 15 do
        local n = (hash2(u, v, 81) - 0.5) * 14
        local seam = (v % 4 == 0) and -26 or (((u + floor(v / 4) * 5) % 8 == 0) and -14 or 0)
        pl[v * 16 + u + 1] = packc(178 + n + seam, 134 + n + seam, 84 + n + seam)
    end end
    reg(TX.planks, pl)
end

-- Representative face texture for a block's hotbar icon.
local function iconTexId(b)
    if b == GRASS then return TX.grassside
    elseif b == DIRT then return TX.dirt
    elseif b == STONE then return TX.stone
    elseif b == SAND then return TX.sand
    elseif b == WOOD then return TX.logside
    elseif b == LEAVES then return TX.leaves
    elseif b == TORCH then return TX.torch
    elseif b == BEDROCK then return TX.bedrock
    elseif b == SNOW then return TX.snow
    elseif b == GLASS then return TX.glass
    elseif b == COPPER_DIRT then return TX.copperdirt
    elseif b == LAVA then return TX.lava
    elseif b == FURNACE then return TX.furnace
    elseif b == PLANKS then return TX.planks end
    return oreTexOf[b] or TX.dirt
end



-- ════════════════════════════════════════════════════════════════════════════════
-- Greedy meshing
-- ════════════════════════════════════════════════════════════════════════════════

local DIRS = {
    { 1, 1, 3, 2, 1, 0, 0, 0.80 }, { 1, -1, 3, 2, -1, 0, 0, 0.80 },
    { 2, 1, 1, 3, 0, 1, 0, 1.00 }, { 2, -1, 1, 3, 0, -1, 0, 0.50 },
    { 3, 1, 1, 2, 0, 0, 1, 0.62 }, { 3, -1, 1, 2, 0, 0, -1, 0.62 },
}

local function texIdFor(b, di)
    if b == GRASS then
        if di == 3 then return TX.grasstop elseif di == 4 then return TX.dirt else return TX.grassside end
    elseif b == DIRT then return TX.dirt
    elseif b == STONE then return TX.stone
    elseif b == SAND then return TX.sand
    elseif b == WOOD then if di == 3 or di == 4 then return TX.logtop else return TX.logside end
    elseif b == LEAVES then return TX.leaves
    elseif b == TORCH then return TX.torch
    elseif b == BEDROCK then return TX.bedrock
    elseif b == SNOW then
        if di == 3 then return TX.snow elseif di == 4 then return TX.dirt else return TX.snowside end
    elseif b == GLASS then return TX.glass
    elseif b == COPPER_DIRT then return TX.copperdirt
    elseif b == LAVA then return TX.lava
    elseif b == PLANKS then return TX.planks
    elseif b == FURNACE then return (di == 3 or di == 4) and TX.furnacetop or TX.furnace end
    return oreTexOf[b] or 0
end

local function cw(da, dv, pa, pval, qa, qval)
    local x, y, z = 0, 0, 0
    if da == 1 then x = dv elseif da == 2 then y = dv else z = dv end
    if pa == 1 then x = pval elseif pa == 2 then y = pval else z = pval end
    if qa == 1 then x = qval elseif qa == 2 then y = qval else z = qval end
    return x, y, z
end
-- voxAt for the mesh hot path: map the (da,pa,qa)-axis coords to (x,y,z) and read the chunk's own
-- flat block array DIRECTLY when the cell is inside this chunk (the common case — the cell itself
-- always is; only a +sign neighbour at a chunk face crosses out), falling back to the string-keyed
-- getVox only for that out-of-chunk border neighbour. This keeps meshing off the per-cell string
-- churn (~half a million "cx,cz" concats/chunk) that made each chunk cost hundreds of ms.
local function voxAt(d, cx, cz, da, dv, pa, pval, qa, qval)
    local x, y, z = 0, 0, 0
    if da == 1 then x = dv elseif da == 2 then y = dv else z = dv end
    if pa == 1 then x = pval elseif pa == 2 then y = pval else z = pval end
    if qa == 1 then x = qval elseif qa == 2 then y = qval else z = qval end
    if y < 0 or y >= WY then return AIR end
    local lx, lz = x - cx * CHUNK, z - cz * CHUNK
    if lx >= 0 and lx < CHUNK and lz >= 0 and lz < CHUNK then
        return d[(y * CHUNK + lz) * CHUNK + lx + 1]
    end
    return getVox(x, y, z)
end

-- skyAt for the mesh hot path: index the chunk's own skylight array directly for in-chunk columns,
-- falling back to skyAt only for a neighbour column that lies in an adjacent chunk.
local function skyFast(sk, cx, cz, x, y, z)
    if y >= WY then return LIGHT_MAX end
    if y < 0 then return 0 end
    local lx, lz = x - cx * CHUNK, z - cz * CHUNK
    if lx < 0 or lx >= CHUNK or lz < 0 or lz >= CHUNK then return skyAt(x, y, z) end
    local top = sk[lz * CHUNK + lx + 1] or 0
    if y >= top then return LIGHT_MAX end
    local v = LIGHT_MAX - (top - y) * SKY_FALLOFF
    return v > 0 and v or 0
end

local _mask = {}   -- scratch face mask, reused across meshChunk calls

-- Mesh one chunk column (X,Z in [c*CHUNK, ..), full Y) straight into the scene's geometry
-- batches: one textured batch per face direction (id = ci*8 + di) plus one flat batch for
-- water (id = ci). The geometry then lives in C# and is drawn per frame with one call per
-- bucket — no per-quad Lua↔C# traffic on the hot path.
-- ── chunk MESHING (incremental) ──────────────────────────────────────────────────────────────
-- Meshing one 16×128×16 chunk is the heavy per-chunk cost, so it runs as a resumable JOB: each step
-- computes ONE of the 6 face directions into flat Lua buffers (pure Lua, no scene calls); when all
-- six are done the buffered quads are pushed into the scene in one emit. updateStreaming drives a few
-- steps per frame so the framerate never drops while the world streams in. The Y range is clamped to
-- the chunk's solid span (WCFG.yBounds) so the all-air ceiling above the terrain is never walked.
-- All these live on WCFG (the file is at Lua's 200-local ceiling — no new top-level locals).

-- Ensure a chunk's retained scene objects exist (6 face dirs + water + glass). Reused across remeshes.
WCFG.ensureObjects = function(cx, cz)
    local ch = chunks[ckey(cx, cz)]
    if ch then return ch end
    local loX, loZ = cx * CHUNK, cz * CHUNK
    local hiX, hiZ = loX + CHUNK, loZ + CHUNK
    ch = { objTex = {}, cx = cx, cz = cz }
    for di = 1, 6 do
        local id = scene:NewObject()
        local cfg = DIRS[di]
        scene:ObjSetNormal(id, cfg[5], cfg[6], cfg[7])
        scene:ObjSetPass(id, 0, 0, 0, 0, 255)
        scene:ObjSetBounds(id, loX, 0, loZ, hiX, WY, hiZ)
        ch.objTex[di] = id
    end
    ch.objWater = scene:NewObject()
    scene:ObjSetPass(ch.objWater, 1, WATER_R, WATER_G, WATER_B, WATER_A)
    scene:ObjSetBounds(ch.objWater, loX, 0, loZ, hiX, WY, hiZ)
    ch.objGlass = scene:NewObject()   -- transparent textured pass (glass panes)
    scene:ObjSetPass(ch.objGlass, 1, 0, 0, 0, GLASS_A)
    scene:ObjSetBounds(ch.objGlass, loX, 0, loZ, hiX, WY, hiZ)
    return ch
end

-- Build a fresh mesh job for a chunk: pure-Lua state (block/sky refs + flat quad buffers); no scene
-- objects exist until emitChunk, so an unfinished job can be dropped for free.
WCFG.newJob = function(cx, cz)
    local key = ckey(cx, cz)
    local yMax = WCFG.yBounds[key]
    local yHi
    if yMax == nil then yHi = WY                 -- defensive: unknown bound → mesh the full column
    elseif yMax >= 0 then yHi = yMax + 1          -- mesh y∈[0, yMax]; skip the empty ceiling
    else yHi = 0 end                              -- all-air chunk → nothing to mesh
    return {
        cx = cx, cz = cz, key = key,
        d = chunkData[key], sk = chunkSky[key],
        loX = cx * CHUNK, loZ = cz * CHUNK, hiX = cx * CHUNK + CHUNK, hiZ = cz * CHUNK + CHUNK,
        yLo = 0, yHi = yHi, di = 1,
        tex = { {}, {}, {}, {}, {}, {} }, texN = { 0, 0, 0, 0, 0, 0 },
        water = {}, waterN = 0, glass = {}, glassN = 0,
        _lo = { 0, 0, 0 }, _hi = { 0, 0, 0 },
    }
end

-- Compute ONE face direction's greedy-merged quads into the job's flat buffers (NO scene calls).
-- Quads are stored as flat number runs so emitChunk can replay them: textured = 16 numbers (12
-- coords + texId,pw,qh,shade), water = 12 (coords only). Mask value: 0 none, -1 water, -100-light
-- glass, else texId*32+light (greedy only merges faces sharing texture+light).
WCFG.meshDir = function(job, di)
    local d, sk, cx, cz = job.d, job.sk, job.cx, job.cz
    local cfg = DIRS[di]
    local da, sign, pa, qa = cfg[1], cfg[2], cfg[3], cfg[4]
    local lo, hi = job._lo, job._hi
    lo[1] = job.loX; lo[2] = job.yLo; lo[3] = job.loZ      -- Y (axis 2) is clamped to the solid span
    hi[1] = job.hiX; hi[2] = job.yHi; hi[3] = job.hiZ
    local ddLo, ddHi = lo[da], hi[da]
    local pLo, pHi = lo[pa], hi[pa]
    local qLo, qHi = lo[qa], hi[qa]
    local qSpan = qHi - qLo
    local mask = _mask
    local CH = CHUNK
    local cxB, czB = cx * CH, cz * CH
    local tex, tn = job.tex[di], 0
    local water, wn = job.water, job.waterN
    local glass, gn = job.glass, job.glassN
    local nb1, nb2, nb3 = 0, 0, 0            -- scratch for the lit neighbour cell coords
    for dd = ddLo, ddHi - 1 do
        for p = pLo, pHi - 1 do
            local b0 = (p - pLo) * qSpan - qLo
            for q = qLo, qHi - 1 do
                -- the meshed cell is ALWAYS in-chunk → inline the block read (skip the voxAt call)
                local x, y, z = 0, 0, 0
                if da == 1 then x = dd elseif da == 2 then y = dd else z = dd end
                if pa == 1 then x = p elseif pa == 2 then y = p else z = p end
                if qa == 1 then x = q elseif qa == 2 then y = q else z = q end
                local b = d[(y * CH + (z - czB)) * CH + (x - cxB) + 1]
                local val = 0
                if b ~= AIR and b ~= TORCH then
                    local nb = voxAt(d, cx, cz, da, dd + sign, pa, p, qa, q)   -- neighbour may cross a border
                    -- glass shows a face against anything but glass; everything else shows where the
                    -- neighbour is see-through (leaves included, but leaf-vs-leaf stays a hollow shell).
                    local show
                    if b == GLASS then show = nb ~= GLASS
                    elseif b == LEAVES then show = isTransparent(nb) and nb ~= LEAVES
                    else show = isTransparent(nb) end
                    if show then
                        if b == WATER then
                            if nb == AIR then val = -1 end
                        else
                            nb1 = 0; nb2 = 0; nb3 = 0
                            if da == 1 then nb1 = dd + sign elseif da == 2 then nb2 = dd + sign else nb3 = dd + sign end
                            if pa == 1 then nb1 = p elseif pa == 2 then nb2 = p else nb3 = p end
                            if qa == 1 then nb1 = q elseif qa == 2 then nb2 = q else nb3 = q end
                            -- bake skylight exposure only; sun/ambient/torch lights are per-frame in the engine
                            local lite = skyFast(sk, cx, cz, nb1, nb2, nb3)
                            if b == LAVA then lite = LIGHT_MAX end       -- lava glows on its own
                            if b == GLASS then val = -100 - lite
                            else val = texIdFor(b, di) * 32 + lite end
                        end
                    end
                end
                mask[b0 + q + 1] = val
            end
        end
        local planePos = dd + (sign > 0 and 1 or 0)
        for p = pLo, pHi - 1 do
            local b0 = (p - pLo) * qSpan - qLo
            local q = qLo
            while q < qHi do
                local val = mask[b0 + q + 1]
                if val == 0 then q = q + 1
                else
                    -- greedy rectangle merge: grow along q, then along p, over equal vals. Low-light
                    -- faces cap their size so per-vertex torch falloff has enough vertices; bright
                    -- outdoor faces merge uncapped (kept cheap).
                    local litev = (val <= -100) and (-val - 100) or (val % 32)
                    local cap = (litev <= LIT_MERGE_LITE) and LIT_MERGE_CAP or 4096
                    local qh = 1
                    while q + qh < qHi and qh < cap and mask[b0 + q + qh + 1] == val do qh = qh + 1 end
                    local pw = 1; local grow = true
                    while p + pw < pHi and pw < cap and grow do
                        local b2 = (p + pw - pLo) * qSpan - qLo
                        for qq = q, q + qh - 1 do if mask[b2 + qq + 1] ~= val then grow = false; break end end
                        if grow then pw = pw + 1 end
                    end
                    for pp = p, p + pw - 1 do
                        local bc = (pp - pLo) * qSpan - qLo
                        for qq = q, q + qh - 1 do mask[bc + qq + 1] = 0 end
                    end
                    local x0, y0, z0 = cw(da, planePos, pa, p, qa, q)
                    local x1, y1, z1 = cw(da, planePos, pa, p + pw, qa, q)
                    local x2, y2, z2 = cw(da, planePos, pa, p + pw, qa, q + qh)
                    local x3, y3, z3 = cw(da, planePos, pa, p, qa, q + qh)
                    if val == -1 then
                        water[wn+1]=x0; water[wn+2]=y0; water[wn+3]=z0; water[wn+4]=x1; water[wn+5]=y1; water[wn+6]=z1
                        water[wn+7]=x2; water[wn+8]=y2; water[wn+9]=z2; water[wn+10]=x3; water[wn+11]=y3; water[wn+12]=z3
                        wn = wn + 12
                    elseif val <= -100 then
                        local lite = -val - 100   -- glass: pass SKY EXPOSURE (0..1) as shade (gates the sun)
                        glass[gn+1]=x0; glass[gn+2]=y0; glass[gn+3]=z0; glass[gn+4]=x1; glass[gn+5]=y1; glass[gn+6]=z1
                        glass[gn+7]=x2; glass[gn+8]=y2; glass[gn+9]=z2; glass[gn+10]=x3; glass[gn+11]=y3; glass[gn+12]=z3
                        glass[gn+13]=TX.glass; glass[gn+14]=pw; glass[gn+15]=qh; glass[gn+16]=lite / LIGHT_MAX
                        gn = gn + 16
                    else
                        local texId = val // 32
                        local lite = val - texId * 32
                        tex[tn+1]=x0; tex[tn+2]=y0; tex[tn+3]=z0; tex[tn+4]=x1; tex[tn+5]=y1; tex[tn+6]=z1
                        tex[tn+7]=x2; tex[tn+8]=y2; tex[tn+9]=z2; tex[tn+10]=x3; tex[tn+11]=y3; tex[tn+12]=z3
                        tex[tn+13]=texId; tex[tn+14]=pw; tex[tn+15]=qh; tex[tn+16]=lite / LIGHT_MAX
                        tn = tn + 16
                    end
                    q = q + qh
                end
            end
        end
    end
    job.texN[di] = tn
    job.waterN = wn
    job.glassN = gn
end

-- Push a finished job's buffered quads into the scene (the only scene-touching part) and register the
-- chunk as live. ObjBegin resets each object, so a remesh cleanly replaces its geometry in one swap.
WCFG.emitChunk = function(job)
    local ch = WCFG.ensureObjects(job.cx, job.cz)
    for di = 1, 6 do
        local objT = ch.objTex[di]
        scene:ObjBegin(objT)
        local tex, tn, k = job.tex[di], job.texN[di], 0
        while k < tn do
            scene:ObjAddQuadTex(objT, tex[k+1],tex[k+2],tex[k+3], tex[k+4],tex[k+5],tex[k+6],
                tex[k+7],tex[k+8],tex[k+9], tex[k+10],tex[k+11],tex[k+12], tex[k+13],tex[k+14],tex[k+15],tex[k+16])
            k = k + 16
        end
    end
    scene:ObjBegin(ch.objWater)
    local water, wn, k = job.water, job.waterN, 0
    while k < wn do
        scene:ObjAddQuadFlat(ch.objWater, water[k+1],water[k+2],water[k+3], water[k+4],water[k+5],water[k+6],
            water[k+7],water[k+8],water[k+9], water[k+10],water[k+11],water[k+12])
        k = k + 12
    end
    scene:ObjBegin(ch.objGlass)
    local glass, gn = job.glass, job.glassN
    k = 0
    while k < gn do
        scene:ObjAddQuadTex(ch.objGlass, glass[k+1],glass[k+2],glass[k+3], glass[k+4],glass[k+5],glass[k+6],
            glass[k+7],glass[k+8],glass[k+9], glass[k+10],glass[k+11],glass[k+12], glass[k+13],glass[k+14],glass[k+15],glass[k+16])
        k = k + 16
    end
    chunks[job.key] = ch
end

-- Synchronous full mesh (block edits): all six directions + emit at once. Cheap enough after the
-- Y-bound + inlined reads to keep the edited block's own chunk responsive. Cancels any in-progress
-- incremental job for this chunk so it can't later emit stale geometry over this fresh mesh.
WCFG.meshChunk = function(cx, cz)
    local key = ckey(cx, cz)
    local j = WCFG.meshSet[key]
    if j then j.dead = true; WCFG.meshSet[key] = nil end
    local job = WCFG.newJob(cx, cz)
    for di = 1, 6 do WCFG.meshDir(job, di) end
    WCFG.emitChunk(job)
end

-- Enqueue an incremental mesh job (deduped via meshSet). The chunk must already be generated.
WCFG.enqueueMesh = function(cx, cz)
    local key = ckey(cx, cz)
    if WCFG.meshSet[key] or not chunkData[key] then return end
    local job = WCFG.newJob(cx, cz)
    WCFG.meshSet[key] = job
    local q = WCFG.meshJobs
    q[#q + 1] = job
end

-- free a far chunk's scene geometry (the block DATA stays — edits persist; remeshed on return)
local function unmeshChunk(key)
    local ch = chunks[key]
    if not ch then return end
    for di = 1, 6 do scene:DeleteObject(ch.objTex[di]) end
    scene:DeleteObject(ch.objWater)
    scene:DeleteObject(ch.objGlass)
    chunks[key] = nil
    if chunkLava[key] and #chunkLava[key] > 0 then torchesDirty = true end
end

-- Remesh just the chunks affected by a block change at (x,y,z): the block's own chunk
-- plus the chunks of its horizontal neighbours (their faces toward the block change).
local function remeshAt(x, y, z)
    -- The edited block's OWN chunk re-meshes immediately so the change shows the same frame (cheap now
    -- that meshChunk indexes the chunk array directly). Its horizontal neighbours only change when the
    -- edit sits on a shared border, so they are DEFERRED to the budgeted remesh drain in updateStreaming
    -- — this kills the old "re-mesh up to five chunks on the edit frame" break/place spike.
    local cx, cz = floor(x / CHUNK), floor(z / CHUNK)
    if chunkData[ckey(cx, cz)] then WCFG.meshChunk(cx, cz) end
    local function enqueue(bx, bz)
        local ncx, ncz = floor(bx / CHUNK), floor(bz / CHUNK)
        if ncx ~= cx or ncz ~= cz then
            local nk = ckey(ncx, ncz)
            if chunks[nk] then WCFG.remeshQ[nk] = true end   -- only if currently meshed (matches the drain)
        end
    end
    enqueue(x - 1, z); enqueue(x + 1, z); enqueue(x, z - 1); enqueue(x, z + 1)
end

-- ── chunk STREAMING: generate the nearest missing chunk (1/frame), drain a couple of queued
-- remeshes, and unmesh chunks that drifted out of range. Called every frame from update.
local function updateStreaming(dt)
    local pcx, pcz = floor(feetX / CHUNK), floor(feetZ / CHUNK)
    if pcx ~= WCFG.lastPcx or pcz ~= WCFG.lastPcz then WCFG.lastPcx, WCFG.lastPcz = pcx, pcz; torchesDirty = true end
    if not WCFG.genOrder then
        WCFG.genOrder = {}
        local R = WCFG.GEN_CHUNKS
        for dz = -R, R do for dx = -R, R do
            if dx * dx + dz * dz <= (R + 0.5) * (R + 0.5) then WCFG.genOrder[#WCFG.genOrder + 1] = { dx, dz, dx * dx + dz * dz } end
        end end
        table.sort(WCFG.genOrder, function(a, b) return a[3] < b[3] end)
    end
    -- Adaptive per-frame MESH budget: meshing is incremental (one face-direction per step), so steer the
    -- steps-per-frame off the PREVIOUS frame's dt — ramp up when there's headroom, back off when a frame
    -- runs heavy — to fill fast while holding framerate. Lua has no sub-frame clock (os.clock stripped),
    -- hence dt feedback. The budget self-limits stalls: it only rises while frames stay under target.
    local target = 0.024
    dt = dt or target
    if dt < target then WCFG.genBudget = min(WCFG.genBudget + 1, 8)
    elseif dt > target * 1.5 then WCFG.genBudget = max(WCFG.genBudget - 1, 1) end
    local budget = WCFG.genBudget
    -- GENERATE the nearest missing chunks (DATA only; capped — gen is ~a frame's worth each) and queue
    -- their meshes; queue revisited chunks too. Generating a chunk also queues neighbour remeshes so they
    -- hide the faces that just became interior.
    local genned = 0
    for _, o in ipairs(WCFG.genOrder) do
        local cx, cz = pcx + o[1], pcz + o[2]
        local key = ckey(cx, cz)
        if not chunkData[key] then
            if genned < 2 then
                genChunk(cx, cz)
                WCFG.enqueueMesh(cx, cz)
                for _, n in ipairs({ { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } }) do
                    local nk = ckey(cx + n[1], cz + n[2])
                    if chunks[nk] then WCFG.remeshQ[nk] = true end
                end
                genned = genned + 1
            end
        elseif not chunks[key] and not WCFG.meshSet[key] then
            WCFG.enqueueMesh(cx, cz)          -- revisited area: data kept, re-mesh it
        end
    end
    -- fold deferred remeshes (block edits + frontier hides) into the incremental mesh queue
    for k in pairs(WCFG.remeshQ) do
        WCFG.remeshQ[k] = nil
        local ch = chunks[k]
        if ch and not WCFG.meshSet[k] then WCFG.enqueueMesh(ch.cx, ch.cz) end
    end
    -- DRAIN the mesh queue: up to `budget` direction-steps this frame; at most ONE chunk emitted per
    -- frame (the emit is the single burst of Lua→C# quad calls — capping it keeps frames even).
    local steps = 0
    while steps < budget do
        local job = WCFG.meshJobs[WCFG.meshHead]
        if not job then break end
        if job.dead then
            WCFG.meshHead = WCFG.meshHead + 1
        elseif job.di <= 6 then
            WCFG.meshDir(job, job.di); job.di = job.di + 1; steps = steps + 1
        else
            WCFG.emitChunk(job); WCFG.meshSet[job.key] = nil
            WCFG.meshHead = WCFG.meshHead + 1
            break
        end
    end
    -- compact the processed prefix of the queue (drop dead weight; reset when fully drained)
    local q = WCFG.meshJobs
    if WCFG.meshHead > #q then
        WCFG.meshJobs = {}; WCFG.meshHead = 1
    elseif WCFG.meshHead > 48 then
        local nq = {}
        for i = WCFG.meshHead, #q do nq[#nq + 1] = q[i] end
        WCFG.meshJobs = nq; WCFG.meshHead = 1
    end
    -- UNLOAD far chunks (free their scene geometry; block data persists). Also cancel any queued job
    -- for a chunk that drifted out of range so it can't pop in behind the player.
    for k, ch in pairs(chunks) do
        local ddx, ddz = ch.cx - pcx, ch.cz - pcz
        if ddx * ddx + ddz * ddz > WCFG.MESH_UNLOAD * WCFG.MESH_UNLOAD then unmeshChunk(k) end
    end
    for k, job in pairs(WCFG.meshSet) do
        local ddx, ddz = job.cx - pcx, job.cz - pcz
        if ddx * ddx + ddz * ddz > WCFG.MESH_UNLOAD * WCFG.MESH_UNLOAD then job.dead = true; WCFG.meshSet[k] = nil end
    end
end

local torchCount = 0   -- number of torches placed (kept for HUD/debug; lights come from `torches`)

-- The one entry point for editing a voxel: updates the block + the column's skylight, tracks
-- torches (they drive the engine point lights, rebuilt from `torches` on the next frame), then
-- remeshes the affected chunks. Torch light no longer touches the baked mesh, so a plain local
-- remesh is enough.
-- ── flowing water ────────────────────────────────────────────────────────────────────────────
-- Water cells are either SOURCES (generated ocean/lakes; no entry in water.lvl) or FLOWING
-- (water.lvl[k] = 7..1). Active cells sit in a queue ticked every 0.2 s: water falls into air
-- below (the falling column stays strong), else spreads sideways with a decreasing level; flowing
-- water with no feed (no water above, no stronger neighbour) dries up. Edits wake neighbours.
local water = { lvl = {}, q = {}, t = 0 }   -- flowing-water state (one table: 200-locals limit)
local function wkey(x, y, z) return x .. ":" .. y .. ":" .. z end
local function activateWater(x, y, z) water.q[#water.q + 1] = { x, y, z } end
local function isLiquid(b) return b == WATER or b == LAVA end
local function activateWaterAround(x, y, z)
    if isLiquid(getVox(x, y, z)) then activateWater(x, y, z) end
    if isLiquid(getVox(x - 1, y, z)) then activateWater(x - 1, y, z) end
    if isLiquid(getVox(x + 1, y, z)) then activateWater(x + 1, y, z) end
    if isLiquid(getVox(x, y, z - 1)) then activateWater(x, y, z - 1) end
    if isLiquid(getVox(x, y, z + 1)) then activateWater(x, y, z + 1) end
    if isLiquid(getVox(x, y + 1, z)) then activateWater(x, y + 1, z) end
end

local function setBlock(x, y, z, newType)
    local old = getVox(x, y, z)
    if old == newType then return end
    setVox(x, y, z, newType)
    recomputeSkyColumn(x, z)
    -- keep the chunk's mesh Y-bound covering a block placed above the old top, or it'd fall outside the
    -- meshed range and stay invisible (yMax only grows here; shrinking is a harmless perf-only miss).
    if newType ~= AIR then
        local bk = ckey(floor(x / CHUNK), floor(z / CHUNK))
        if y > (WCFG.yBounds[bk] or -1) then WCFG.yBounds[bk] = y end
    end
    local tIdx = x .. ":" .. y .. ":" .. z
    if old == TORCH then torchCount = torchCount - 1; torches[tIdx] = nil; torchesDirty = true end
    if newType == TORCH then torchCount = torchCount + 1; torches[tIdx] = { x, y, z }; torchesDirty = true end
    -- wake any water around the edit so it can flow into / re-settle around the change
    activateWaterAround(x, y, z)
    remeshAt(x, y, z)
end

local function tickWater(dt)
    water.t = water.t + dt
    if water.t < 0.2 then return end
    water.t = 0
    water.lavaPhase = ((water.lavaPhase or 0) + 1) % 3          -- lava flows at a third of the water rate
    if #water.q == 0 then return end
    local q = water.q
    water.q = {}
    local dirty = {}
    local function setW(x, y, z, lvl, liquid)
        liquid = liquid or WATER
        -- water meeting lava (or the reverse) quenches into STONE
        local function near(b)
            return getVox(x - 1, y, z) == b or getVox(x + 1, y, z) == b
                or getVox(x, y, z - 1) == b or getVox(x, y, z + 1) == b or getVox(x, y + 1, z) == b
        end
        if (liquid == WATER and near(LAVA)) or (liquid == LAVA and near(WATER)) then
            setVox(x, y, z, STONE)
            water.lvl[wkey(x, y, z)] = nil
            dirty[ckey(floor(x / CHUNK), floor(z / CHUNK))] = true
            recomputeSkyColumn(x, z)
            return
        end
        setVox(x, y, z, liquid)
        if lvl then water.lvl[wkey(x, y, z)] = lvl else water.lvl[wkey(x, y, z)] = nil end
        dirty[ckey(floor(x / CHUNK), floor(z / CHUNK))] = true
        activateWater(x, y, z)
    end
    local function clearW(x, y, z)
        setVox(x, y, z, AIR)
        water.lvl[wkey(x, y, z)] = nil
        dirty[ckey(floor(x / CHUNK), floor(z / CHUNK))] = true
        activateWaterAround(x, y, z)
    end
    local n = min(#q, 260)                      -- budget per tick; the rest re-queues
    for i = n + 1, #q do water.q[#water.q + 1] = q[i] end
    for i = 1, n do
        local c = q[i]
        local x, y, z = c[1], c[2], c[3]
        local me = getVox(x, y, z)
        if me == LAVA and water.lavaPhase ~= 0 then
            water.q[#water.q + 1] = c                                   -- lava waits for its slow tick
        elseif me == WATER or me == LAVA then
            local maxSpread = (me == LAVA) and 4 or 8                   -- lava creeps a short distance
            local lvl = math.min(water.lvl[wkey(x, y, z)] or 8, maxSpread)
            -- dry up flowing water that lost its feed
            local fed = (lvl >= maxSpread) or getVox(x, y + 1, z) == me
            if not fed then
                local function nl(nx, nz)
                    if getVox(nx, y, nz) ~= me then return 0 end
                    return water.lvl[wkey(nx, y, nz)] or 8
                end
                fed = nl(x - 1, z) > lvl or nl(x + 1, z) > lvl or nl(x, z - 1) > lvl or nl(x, z + 1) > lvl
            end
            if not fed and lvl < maxSpread then
                clearW(x, y, z)
            else
                if getVox(x, y - 1, z) == AIR and y > 1 then
                    setW(x, y - 1, z, (me == LAVA) and 3 or 7, me)        -- fall
                elseif lvl > 1 then
                    local below = getVox(x, y - 1, z)
                    if below ~= AIR then                                  -- pooled: spread sideways
                        local function trySpread(nx, nz)
                            if getVox(nx, y, nz) == AIR then setW(nx, y, nz, lvl - 1, me)
                            elseif getVox(nx, y, nz) == me then
                                local ol = water.lvl[wkey(nx, y, nz)]
                                if ol and ol < lvl - 1 then setW(nx, y, nz, lvl - 1, me) end
                            end
                        end
                        trySpread(x - 1, z); trySpread(x + 1, z); trySpread(x, z - 1); trySpread(x, z + 1)
                    end
                end
            end
        end
    end
    for k in pairs(dirty) do
        if chunks[k] then WCFG.remeshQ[k] = true end
    end
end

-- ════════════════════════════════════════════════════════════════════════════════
-- Camera + projection helpers
-- ════════════════════════════════════════════════════════════════════════════════

-- The Lua3DScene owns the camera (position, yaw/pitch → basis, fov → scale, near).
-- We push yaw/pitch and read the right/up/forward basis back for movement & picking.
local function rebuildBasis()
    scene:SetCameraAngles(yaw, pitch)
    Fx, Fy, Fz = scene:GetCameraForward()
    Rx, _, Rz = scene:GetCameraRight()
end

-- world → screen (RW×RH). returns sx, sy, inFront (projection lives in the scene)
local function project(wx, wy, wz)
    return scene:Project(wx, wy, wz)
end

-- ════════════════════════════════════════════════════════════════════════════════
-- Collision + physics
-- ════════════════════════════════════════════════════════════════════════════════

local function aabbSolid(minx, miny, minz, maxx, maxy, maxz)
    for X = floor(minx), floor(maxx) do
        for Y = floor(miny), floor(maxy) do
            for Z = floor(minz), floor(maxz) do
                if isSolid(getVox(X, Y, Z)) then return true end
            end
        end
    end
    return false
end

-- ════════════════════════════════════════════════════════════════════════════════
-- Block picking (DDA raycast from the camera)
-- ════════════════════════════════════════════════════════════════════════════════

local function pickBlock()
    local ox, oy, oz = camX, camY, camZ
    local dx, dy, dz = Fx, Fy, Fz
    local ix, iy, iz = floor(ox), floor(oy), floor(oz)
    local stepX = dx > 0 and 1 or -1
    local stepY = dy > 0 and 1 or -1
    local stepZ = dz > 0 and 1 or -1
    local adx = dx < 0 and -dx or dx
    local ady = dy < 0 and -dy or dy
    local adz = dz < 0 and -dz or dz
    local tDX = adx > 1e-9 and 1 / adx or 1e30
    local tDY = ady > 1e-9 and 1 / ady or 1e30
    local tDZ = adz > 1e-9 and 1 / adz or 1e30
    local tMaxX = dx > 0 and (ix + 1 - ox) / dx or (dx < 0 and (ix - ox) / dx or 1e30)
    local tMaxY = dy > 0 and (iy + 1 - oy) / dy or (dy < 0 and (iy - oy) / dy or 1e30)
    local tMaxZ = dz > 0 and (iz + 1 - oz) / dz or (dz < 0 and (iz - oz) / dz or 1e30)
    -- face normal of the entered face (points back toward the ray origin)
    local fnx, fny, fnz = 0, 1, 0
    if isSelectable(getVox(ix, iy, iz)) then return ix, iy, iz, fnx, fny, fnz end
    for _ = 1, 64 do
        local t
        if tMaxX < tMaxY and tMaxX < tMaxZ then t = tMaxX; ix = ix + stepX; tMaxX = tMaxX + tDX; fnx, fny, fnz = -stepX, 0, 0
        elseif tMaxY < tMaxZ then t = tMaxY; iy = iy + stepY; tMaxY = tMaxY + tDY; fnx, fny, fnz = 0, -stepY, 0
        else t = tMaxZ; iz = iz + stepZ; tMaxZ = tMaxZ + tDZ; fnx, fny, fnz = 0, 0, -stepZ end
        if t > REACH then return nil end
        if isSelectable(getVox(ix, iy, iz)) then return ix, iy, iz, fnx, fny, fnz end
    end
    return nil
end

-- ════════════════════════════════════════════════════════════════════════════════
-- Render
-- ════════════════════════════════════════════════════════════════════════════════

-- ── Day/night sun ───────────────────────────────────────────────────────────────
-- A single directional "sun" arcs around the world — cheap (one dot product per face in the
-- engine) — plus a global ambient fill. Torches add local light on top. This is what lights the
-- world generally, instead of relying on many point lights.
local DAY_LEN = 360.0       -- seconds for a full day/night cycle
local dayTime = 0.28       -- [0,1) phase; starts mid-morning
local skyMul  = 1.0        -- sky/fog brightness for the current time of day

local function updateSun(dt)
    dayTime = (dayTime + dt / DAY_LEN) % 1.0
    local theta = dayTime * 2 * math.pi
    local dx, dy, dz = math.cos(theta), math.sin(theta), 0.35    -- arcs over the +X/-X sky
    local day = dy > 0 and dy or 0                               -- elevation: 0 at night, 1 at noon
    local warm = 1 - day                                        -- dimmer + redder near the horizon
    scene:SetSun(dx, dy, dz, 1.35 * day, (1.18 - 0.18 * warm) * day, (1.02 - 0.50 * warm) * day)
    scene:SetAmbient(0.07 + 0.26 * day, 0.08 + 0.28 * day, 0.13 + 0.30 * day)
    skyMul = 0.16 + 0.84 * day                                  -- keep a dark-blue night sky
    scene:SetFog(true, SKY_HOR_R * skyMul, SKY_HOR_G * skyMul, SKY_HOR_B * skyMul, FOG_START, FOG_END)
end

local function drawSky()
    local bands = 24
    local bh = RH / bands
    local m = skyMul
    for i = 0, bands - 1 do
        local t = i / (bands - 1)
        scene:FillRect(0, floor(i * bh), RW, floor(bh) + 2,
            floor((SKY_TOP_R + (SKY_HOR_R - SKY_TOP_R) * t) * m),
            floor((SKY_TOP_G + (SKY_HOR_G - SKY_TOP_G) * t) * m),
            floor((SKY_TOP_B + (SKY_HOR_B - SKY_TOP_B) * t) * m), 255)
    end
end

-- World-space corners (CCW) of the cube face whose outward normal is (nx,ny,nz).
local function faceCorners(x, y, z, nx, ny, nz)
    if nx > 0 then return {x+1,y,z},{x+1,y+1,z},{x+1,y+1,z+1},{x+1,y,z+1}
    elseif nx < 0 then return {x,y,z},{x,y+1,z},{x,y+1,z+1},{x,y,z+1}
    elseif ny > 0 then return {x,y+1,z},{x+1,y+1,z},{x+1,y+1,z+1},{x,y+1,z+1}
    elseif ny < 0 then return {x,y,z},{x+1,y,z},{x+1,y,z+1},{x,y,z+1}
    elseif nz > 0 then return {x,y,z+1},{x+1,y,z+1},{x+1,y+1,z+1},{x,y+1,z+1}
    else return {x,y,z},{x+1,y,z},{x+1,y+1,z},{x,y+1,z} end
end

local selObj = nil
-- The selected face's outline as four thin YELLOW quads in 3D (unlit, opaque → depth-tested), so it
-- sits in the world: nearer blocks occlude it and it stays visible in the dark. Built each frame BEFORE
-- scene:Render() (geometry submitted before the render is what gets drawn).
local function buildSelection()
    if selObj == nil then return end
    scene:ObjBegin(selObj)
    if not hasSel then return end
    local c = { faceCorners(selX, selY, selZ, selNx, selNy, selNz) }
    local nx, ny, nz = selNx, selNy, selNz
    local off, tw = 0.012, 0.045                       -- lift off the face (no z-fight) + outline width
    for i = 1, 4 do c[i] = { c[i][1] + nx * off, c[i][2] + ny * off, c[i][3] + nz * off } end
    local fcx = (c[1][1] + c[2][1] + c[3][1] + c[4][1]) * 0.25   -- face centre (to push the border inward)
    local fcy = (c[1][2] + c[2][2] + c[3][2] + c[4][2]) * 0.25
    local fcz = (c[1][3] + c[2][3] + c[3][3] + c[4][3]) * 0.25
    for i = 1, 4 do
        local a, b = c[i], c[(i % 4) + 1]
        local ex, ey, ez = b[1] - a[1], b[2] - a[2], b[3] - a[3]
        local tx, ty, tz = ey * nz - ez * ny, ez * nx - ex * nz, ex * ny - ey * nx   -- edge × normal, in-plane
        local tl = math.sqrt(tx * tx + ty * ty + tz * tz); if tl > 1e-6 then tx, ty, tz = tx / tl * tw, ty / tl * tw, tz / tl * tw end
        local mx, my, mz = (a[1] + b[1]) * 0.5, (a[2] + b[2]) * 0.5, (a[3] + b[3]) * 0.5
        if tx * (fcx - mx) + ty * (fcy - my) + tz * (fcz - mz) < 0 then tx, ty, tz = -tx, -ty, -tz end   -- inward
        scene:ObjAddQuadFlat(selObj, a[1], a[2], a[3], b[1], b[2], b[3], b[1] + tx, b[2] + ty, b[3] + tz, a[1] + tx, a[2] + ty, a[3] + tz)
    end
end

local function drawCrosshair()
    local cx, cy = floor(RW / 2), floor(RH / 2)
    scene:DrawLine(cx - 8, cy, cx + 8, cy, 255, 255, 255)
    scene:DrawLine(cx, cy - 8, cx, cy + 8, 255, 255, 255)
end

-- Torches are a single retained object: slim emissive posts (one per torch), rebuilt only
-- when a torch is added/removed. Opaque pass, so the scene draws them before water.
local TORCH_HW = 0.09   -- half-width of the post
local TORCH_H  = 0.62   -- post height
-- Torch light (engine forward lighting): warm orange, finite range so it glows locally.
local TL_R, TL_G, TL_B = 1.0, 0.55, 0.22
local TL_INTENSITY     = 2.3
local TL_RANGE         = 9.5
local torchObj = nil
local function rebuildTorches()
    if torchObj == nil then return end
    scene:ObjBegin(torchObj)
    scene:ClearLights()                      -- torches + cave lava ARE the scene's point lights
    -- lava anchors of meshed chunks become point lights too, capped to the 56 nearest (engine max 64)
    local cand = {}
    for k in pairs(chunks) do
        for _, L in ipairs(chunkLava[k] or {}) do
            local dx, dy, dz = L[1] - feetX, L[2] - feetY, L[3] - feetZ
            cand[#cand + 1] = { p = L, d = dx * dx + dy * dy + dz * dz }
        end
    end
    table.sort(cand, function(a, b) return a.d < b.d end)
    for i = 1, min(#cand, 24) do      -- tighter cap: the per-fragment light loop was the orientation FPS cliff
        local L = cand[i].p
        scene:AddLightRanged(L[1] + 0.5, L[2] + 0.4, L[3] + 0.5, 1.0, 0.40, 0.10, 2.4, 8)
    end
    for _, t in pairs(torches) do
        local x, y, z = t[1], t[2], t[3]
        local cxp, czp = x + 0.5, z + 0.5
        local x0, x1 = cxp - TORCH_HW, cxp + TORCH_HW
        local z0, z1 = czp - TORCH_HW, czp + TORCH_HW
        local y0, y1 = y, y + TORCH_H
        scene:ObjAddQuadTex(torchObj, x0,y0,z0, x1,y0,z0, x1,y1,z0, x0,y1,z0, TX.torch, 1, 1, 1.0) -- -Z
        scene:ObjAddQuadTex(torchObj, x1,y0,z1, x0,y0,z1, x0,y1,z1, x1,y1,z1, TX.torch, 1, 1, 1.0) -- +Z
        scene:ObjAddQuadTex(torchObj, x0,y0,z1, x0,y0,z0, x0,y1,z0, x0,y1,z1, TX.torch, 1, 1, 1.0) -- -X
        scene:ObjAddQuadTex(torchObj, x1,y0,z0, x1,y0,z1, x1,y1,z1, x1,y1,z0, TX.torch, 1, 1, 1.0) -- +X
        scene:ObjAddQuadTex(torchObj, x0,y1,z0, x1,y1,z0, x1,y1,z1, x0,y1,z1, TX.torch, 1, 1, 1.0) -- top
        -- a warm point light at the flame (top of the post); unshadowed (the engine no longer casts
        -- shadow rays), so torches glow locally and add on top of the sun + ambient.
        scene:AddLightRanged(cxp, y + TORCH_H * 0.85, czp, TL_R, TL_G, TL_B, TL_INTENSITY, TL_RANGE)
    end
end

-- Per frame: fill the sky background, let the scene render every object (cull/sort/raster
-- all internal now), then the 2D overlays on top.
local function renderFrame()
    if torchesDirty then rebuildTorches(); torchesDirty = false end
    drawSky()
    buildSelection()                       -- 3D outline, submitted before Render so it's depth-tested
    scene:SetCameraPosition(camX, camY, camZ)
    scene:Render()
    drawCrosshair()
    scene:Upload()
end

-- ════════════════════════════════════════════════════════════════════════════════
-- Lifecycle
-- ════════════════════════════════════════════════════════════════════════════════

local BLOCK_NAMES = { [AIR] = "Air", [GRASS] = "Grass", [DIRT] = "Dirt", [STONE] = "Stone",
                      [SAND] = "Sand", [WATER] = "Water", [WOOD] = "Wood", [LEAVES] = "Leaves",
                      [TORCH] = "Torch", [BEDROCK] = "Bedrock", [SNOW] = "Snow", [GLASS] = "Glass" }
for _, m in ipairs(ORE_DEFS) do
    BLOCK_NAMES[m.id] = m.gem and m.name or (m.name .. " Ore")   -- "Gold Ore"; gems keep their name
    BLOCK_NAMES[m.bid] = "Block of " .. m.name                   -- "Block of Iron"
end
BLOCK_NAMES[COPPER_DIRT] = "Copper Ore"
BLOCK_NAMES[LAVA] = "Lava"
BLOCK_NAMES[FURNACE] = "Furnace"
BLOCK_NAMES[PLANKS] = "Planks"

function onStart()
    scene = SCENE3D:CreateScene(RW, RH)
    scene:SetMode("raster")                            -- GPU hardware-pipeline rasterizer (now the engine default; CPU fallback if GLES 3.1 unavailable)
    scene:SetLighting(true)                            -- forward lighting: rotating sun + ambient + torches
    dim   = CANVAS:CreateCanvas(2, 2)
    dim:Clear(255, 255, 255, 255); dim:Upload()   -- white so SetColor can tint it any colour
    fontBig   = TEXT:Create(40)
    fontMid   = TEXT:Create(26)
    fontSmall = TEXT:Create(18)

    scene:SetCameraFov(FOV)
    scene:SetCameraNear(NEAR)
    scene:SetRenderDistance(RENDER_DIST)
    scene:SetFog(true, SKY_HOR_R, SKY_HOR_G, SKY_HOR_B, FOG_START, FOG_END)
    scene:SetThreads(THREADS)
    registerTextures()
    torchObj = scene:NewObject()                      -- one retained object for all torches
    scene:ObjSetPass(torchObj, 0, 0, 0, 0, 255)       -- opaque (drawn before water)
    scene:ObjSetLit(torchObj, false)                  -- posts are the light source: render full-bright
    selObj = scene:NewObject()                        -- block-selection outline (3D, depth-tested)
    scene:ObjSetPass(selObj, 0, 255, 255, 0, 255)     -- opaque, yellow (shows even in the dark)
    scene:ObjSetLit(selObj, false)                    -- full-bright yellow regardless of lighting
    -- item/recipe registry (needs BLOCK_NAMES + texPix, both ready by now)
    -- creative block palette: every named block becomes a placeable item with its face-texture icon
    Inv.init({ texPix = texPix, iconTexId = iconTexId, BLOCK_NAMES = BLOCK_NAMES,
               blocks = { GRASS = GRASS, DIRT = DIRT, STONE = STONE, SAND = SAND, WATER = WATER,
                          WOOD = WOOD, LEAVES = LEAVES, TORCH = TORCH, BEDROCK = BEDROCK, SNOW = SNOW,
                          GLASS = GLASS, COPPER_DIRT = COPPER_DIRT, LAVA = LAVA, FURNACE = FURNACE,
                          PLANKS = PLANKS, COAL_ORE = COAL_ORE } })
    updateSun(0)                                      -- set the initial sun / ambient / sky
    rebuildBasis()
end

-- World generation is DEFERRED to the first entry (every stage's onStart runs at skin BOOT, so
-- generating + meshing the spawn chunks there cost RAM/time even when the stage is never opened).
local function ensureWorld()
    if WCFG.ready then return end
    WCFG.ready = true
    math.randomseed(SEED)
    -- Report real progress: 5×5 spawn chunks × 2 passes (generate, then mesh). LOADING:Tick yields a frame +
    -- advances the bar per chunk, so the loading screen fills smoothly instead of snapping 0→100.
    local total, done = 50, 0
    for cz = -2, 2 do for cx = -2, 2 do genChunk(cx, cz); done = done + 1; LOADING:Tick(done / total) end end
    for cz = -2, 2 do for cx = -2, 2 do WCFG.meshChunk(cx, cz); done = done + 1; LOADING:Tick(done / total) end end
    local gh = terrainHeight(floor(feetX), floor(feetZ))
    feetY = max(gh + 1, SEA_LEVEL + 1)
    spawnX, spawnY, spawnZ = feetX, feetY, feetZ
    spawnYaw, spawnPitch = yaw, pitch
    rebuildBasis()
end

-- Return the player to the starting spot (used by the pause menu's Reset position).
local function resetPosition()
    feetX, feetY, feetZ = spawnX, spawnY, spawnZ
    vx, vy, vz = 0, 0, 0
    yaw, pitch = spawnYaw, spawnPitch
    camX, camY, camZ = feetX, feetY + EYE, feetZ
    rebuildBasis()
end

function activate()
    lastTs = 0
    paused = false
    -- First entry: build the spawn chunks behind the loading bar (LOADING:Add gives ensureWorld's Ticks a
    -- block so they report real progress). On re-entry the world persists (WCFG.ready) → no rebuild.
    if not WCFG.ready then LOADING:Add("World", 1, ensureWorld) end
    INPUT:SetMouseLocked(true)
end

function deactivate()
    INPUT:SetMouseLocked(false)
end
function afterSongEnum() end
function onDestroy() end

-- ════════════════════════════════════════════════════════════════════════════════
-- Update
-- ════════════════════════════════════════════════════════════════════════════════

local function kd(k) return INPUT:KeyboardPressing(k) end
local function kp(k) return INPUT:KeyboardPressed(k) end

-- pause-menu button rects in surface (1920×1080) coords: {cx, cy, w, h}
local MENU_RECTS = { { 960, 452, 380, 72 }, { 960, 540, 380, 72 }, { 960, 628, 380, 72 } }

local function updateGame(dt)
    updateSun(dt)                       -- advance the day/night sun + ambient
    updateStreaming(dt)                 -- infinite world: gen/mesh/unmesh around the player (dt drives the budget)
    tickWater(dt)                       -- flowing water

    -- look: mouse delta + arrow keys
    local dmx, dmy = INPUT:GetMouseDelta()
    yaw = yaw + dmx * MOUSE_SENS
    pitch = pitch - dmy * MOUSE_SENS
    if kd("LeftArrow")  then yaw = yaw - ARROW_TURN * dt end
    if kd("RightArrow") then yaw = yaw + ARROW_TURN * dt end
    if kd("UpArrow")    then pitch = pitch + ARROW_TURN * dt end
    if kd("DownArrow")  then pitch = pitch - ARROW_TURN * dt end
    if pitch > 89 then pitch = 89 elseif pitch < -89 then pitch = -89 end
    rebuildBasis()

    -- water state: `underwater` (eye submerged) drives the screen tint;
    -- `swim` (eye OR feet in water) drives buoyancy so you can paddle/step out at the surface.
    local eyeBlock = getVox(floor(camX), floor(camY), floor(camZ))
    underwater = eyeBlock == WATER
    inLava = eyeBlock == LAVA or getVox(floor(feetX), floor(feetY + 0.1), floor(feetZ)) == LAVA
    local swim = underwater or inLava or getVox(floor(feetX), floor(feetY + 0.1), floor(feetZ)) == WATER

    -- horizontal movement (instant velocity from input, relative to yaw)
    local hl = sqrt(Fx * Fx + Fz * Fz); if hl < 1e-6 then hl = 1e-6 end
    local hfx, hfz = Fx / hl, Fz / hl
    local wishx, wishz = 0, 0
    if kd("W") then wishx = wishx + hfx; wishz = wishz + hfz end
    if kd("S") then wishx = wishx - hfx; wishz = wishz - hfz end
    if kd("D") then wishx = wishx + Rx;  wishz = wishz + Rz end
    if kd("A") then wishx = wishx - Rx;  wishz = wishz - Rz end
    local speed = WALK_SPEED * (kd("LeftControl") and SPRINT_MULT or 1)
    if swim then speed = speed * WATER_MOVE end
    if wishx ~= 0 or wishz ~= 0 then
        local wl = sqrt(wishx * wishx + wishz * wishz)
        vx = wishx / wl * speed; vz = wishz / wl * speed
    else
        vx = 0; vz = 0
    end

    -- vertical
    if swim then
        if not underwater and kd("Space") then
            -- surfaced (feet wet, head out): a full jump to hop out onto a 1-block ledge
            vy = JUMP_VEL
        else
            vy = vy - WATER_GRAV * dt
            if kd("Space") then vy = SWIM_UP end
            vy = vy * WATER_DRAG
            if vy < -6 then vy = -6 elseif vy > 6 then vy = 6 end
        end
    else
        vy = vy - GRAV * dt
        if kd("Space") and onGround then vy = JUMP_VEL end
        if vy < -50 then vy = -50 end
    end

    -- collide + resolve per axis (player AABB). No auto step-up: 1-block steps need a jump.
    local nx = feetX + vx * dt
    if aabbSolid(nx - PW, feetY + 0.1, feetZ - PW, nx + PW, feetY + PH - 0.1, feetZ + PW) then vx = 0 else feetX = nx end
    local nz = feetZ + vz * dt
    if aabbSolid(feetX - PW, feetY + 0.1, nz - PW, feetX + PW, feetY + PH - 0.1, nz + PW) then vz = 0 else feetZ = nz end
    local ny = feetY + vy * dt
    if aabbSolid(feetX - PW, ny, feetZ - PW, feetX + PW, ny + PH, feetZ + PW) then
        if vy < 0 then onGround = true end
        vy = 0
    else
        feetY = ny; onGround = false
    end

    if feetY < 0 then feetY = 0; vy = 0 end          -- bedrock floor (X/Z are infinite now)

    camX, camY, camZ = feetX, feetY + EYE, feetZ

    -- hotbar selection (mouse wheel + number keys; owned by the inventory module)
    Inv.hotbarInput()

    -- block pick
    local bx, by, bz, fnx, fny, fnz = pickBlock()
    if bx then hasSel = true; selX = bx; selY = by; selZ = bz; selNx = fnx; selNy = fny; selNz = fnz else hasSel = false end

    -- CREATIVE mining: left-click removes the block instantly (hold to repeat). Bedrock and liquids
    -- stay put so the world floor survives.
    if hasSel and INPUT:MousePressing("Left") then
        breakTimer = breakTimer - dt
        if INPUT:MousePressed("Left") or breakTimer <= 0 then
            local b = getVox(selX, selY, selZ)
            if isSelectable(b) and b ~= BEDROCK then
                setBlock(selX, selY, selZ, AIR)
                hasSel = false
            end
            breakTimer = EDIT_INTERVAL
        end
    else
        breakTimer = 0
    end

    -- right-click: place the selected hotbar block (infinite; hold to repeat)
    if hasSel and INPUT:MousePressing("Right") then
        placeTimer = placeTimer - dt
        if INPUT:MousePressed("Right") or placeTimer <= 0 then
            local px, py, pz = selX + selNx, selY + selNy, selZ + selNz
            local _, _, item = Inv.selected()
            local b = item and item.block or nil
            if b then
                -- torches are non-solid, so they may sit where the player stands; solids may not
                local hitsPlayer = b ~= TORCH
                    and (feetX + PW > px) and (feetX - PW < px + 1)
                    and (feetY + PH > py) and (feetY < py + 1)
                    and (feetZ + PW > pz) and (feetZ - PW < pz + 1)
                if getVox(px, py, pz) == AIR and not hitsPlayer then
                    setBlock(px, py, pz, b)
                end
            end
            placeTimer = EDIT_INTERVAL
            hasSel = false
        end
    else
        placeTimer = 0
    end

    renderFrame()
end

local function menuActivate(idx)
    if menuItems[idx] == "Resume" then
        paused = false; INPUT:SetMouseLocked(true)
    elseif menuItems[idx] == "Reset position" then
        resetPosition()
        paused = false; INPUT:SetMouseLocked(true)
    elseif menuItems[idx] == "Quit" then
        INPUT:SetMouseLocked(false)
        return Exit("stage", "_title")
    end
    return nil
end

function update(ts)
    local dt = (ts - lastTs) / 1000.0
    lastTs = ts
    if dt < 0 then dt = 0 end
    if dt > 0.1 then dt = 0.1 end

    -- inventory / furnace UI: E toggles, Esc closes; while open it owns the mouse + keys
    if Inv.openMode then
        if kp("E") or kp("Escape") then
            Inv.close()
            INPUT:SetMouseLocked(true)
            return nil
        end
        Inv.updateUI()
        tickWater(dt)
        renderFrame()
        return nil
    end
    if kp("E") then
        Inv.openInv()
        INPUT:SetMouseLocked(false)
        return nil
    end

    if kp("Escape") then
        paused = not paused
        INPUT:SetMouseLocked(not paused)
        return nil
    end

    if paused then
        -- mouse hover selection
        local mx, my = INPUT:GetMouseXY()
        for i = 1, #MENU_RECTS do
            local r = MENU_RECTS[i]
            if mx >= r[1] - r[3] / 2 and mx <= r[1] + r[3] / 2 and my >= r[2] - r[4] / 2 and my <= r[2] + r[4] / 2 then
                menuSel = i
            end
        end
        if kp("DownArrow") or kp("S") then menuSel = menuSel % #menuItems + 1 end
        if kp("UpArrow") or kp("W") then menuSel = (menuSel - 2) % #menuItems + 1 end
        if kp("Return") then return menuActivate(menuSel) end
        if INPUT:MousePressed("Left") then
            local r = MENU_RECTS[menuSel]
            if mx >= r[1] - r[3] / 2 and mx <= r[1] + r[3] / 2 and my >= r[2] - r[4] / 2 and my <= r[2] + r[4] / 2 then
                return menuActivate(menuSel)
            end
        end
        return nil
    end

    if kp("H") then showHelp = not showHelp end
    updateGame(dt)
    return nil
end

-- ════════════════════════════════════════════════════════════════════════════════
-- Draw
-- ════════════════════════════════════════════════════════════════════════════════

local function txt(font, str, r, g, b)
    return font:GetText(str, false, 1800,
        COLOR:CreateColorFromRGBA(r or 255, g or 255, b or 255, 255),
        COLOR:CreateColorFromRGBA(0, 0, 0, 255))
end

-- (BLOCK_NAMES is defined up top, before onStart — Inv.init needs it at boot)

function draw()
    scene:SetColor(1, 1, 1)
    scene:SetOpacity(1.0)
    scene:SetScale(SCREEN_W / RW, SCREEN_H / RH)
    scene:Draw(0, 0)

    if underwater and not paused then
        dim:SetColor(WATER_R / 255, WATER_G / 255, WATER_B / 255)
        dim:SetOpacity(0.42)
        dim:SetScale(SCREEN_W / 2, SCREEN_H / 2)
        dim:Draw(0, 0)
    elseif inLava and not paused then
        dim:SetColor(0.95, 0.30, 0.05)
        dim:SetOpacity(0.5)
        dim:SetScale(SCREEN_W / 2, SCREEN_H / 2)
        dim:Draw(0, 0)
    end

    txt(fontMid, "Voxel Engine", 255, 255, 255):Draw(24, 16)
    if showHelp then
        txt(fontSmall, "WASD move   Space jump/swim   LCtrl sprint   L-click break   R-click place   E block palette   wheel/1-9 hotbar   Esc pause",
            235, 235, 235):Draw(24, 52)
    end

    if not paused then
        Inv.drawHotbar(dim, { big = fontBig, mid = fontMid, small = fontSmall }, SCREEN_W, SCREEN_H)
        -- hovered block: name in a textbox centred above the hotbar
        if hasSel and not Inv.openMode then
            local nm = BLOCK_NAMES[getVox(selX, selY, selZ)] or "?"
            local bw, bh = 360, 50
            local bx, by = (SCREEN_W - bw) / 2, SCREEN_H - bh - 130
            dim:SetColor(0.07, 0.07, 0.09); dim:SetOpacity(0.55)
            dim:SetScale(bw / 2, bh / 2); dim:Draw(bx, by)
            txt(fontMid, nm, 245, 245, 250):DrawAtAnchor(SCREEN_W / 2, by + bh / 2, "center")
        end
        Inv.draw(dim, { big = fontBig, mid = fontMid, small = fontSmall }, SCREEN_W, SCREEN_H)
    end

    if paused then
        dim:SetColor(0, 0, 0)
        dim:SetOpacity(0.55)
        dim:SetScale(SCREEN_W / 2, SCREEN_H / 2)
        dim:Draw(0, 0)
        txt(fontBig, "Paused", 255, 255, 255):DrawAtAnchor(SCREEN_W / 2, 320, "center")
        for i = 1, #menuItems do
            local r = MENU_RECTS[i]
            local sel = (i == menuSel)
            txt(fontMid, (sel and "> " or "") .. menuItems[i] .. (sel and " <" or ""),
                sel and 255 or 210, sel and 235 or 210, sel and 120 or 215):DrawAtAnchor(r[1], r[2], "center")
        end
    end
end
