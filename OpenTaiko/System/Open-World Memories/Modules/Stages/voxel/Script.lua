---@diagnostic disable: undefined-global, undefined-field, need-check-nil, lowercase-global
-- voxel/Script.lua — A small voxel engine running as a Lua stage.
--
-- Terrain rendered by the SCENE3D rasterizer (textured, z-buffered, water
-- alpha). First-person player physics with AABB collision + jumping + swimming, free
-- mouse-look (cursor lock), a block-break cursor with selection wireframe + crosshair,
-- and a pause menu.
--
-- Controls:
--   Move: WASD / arrows    Jump: Space    Sprint: LCtrl    Look: mouse
--   Break block: Left click    Pause: Escape

-- ════════════════════════════════════════════════════════════════════════════════
-- Config
-- ════════════════════════════════════════════════════════════════════════════════

local SCREEN_W, SCREEN_H = 1920, 1080
local RW, RH    = 640, 360       -- internal resolution (raster fill is native; tune freely)

local FOV       = 70.0
local NEAR      = 0.06
local MOUSE_SENS = 0.16          -- deg per mouse pixel
local ARROW_TURN = 90.0          -- deg/sec for arrow-key look

-- view distance + fog (chunks past RENDER_DIST are culled; terrain fades to fog over
-- [FOG_START, FOG_END] so the cull boundary isn't a hard pop). Lower RENDER_DIST = faster.
local RENDER_DIST = 60.0
local FOG_START   = 38.0
local FOG_END     = 60.0
local THREADS     = 4            -- rasterizer threads (screen split into this many row bands)

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

local WX, WY, WZ = 256, 128, 256
local SEA_LEVEL  = 64
local SEED       = 1337

local AIR, GRASS, DIRT, STONE, SAND, WATER, WOOD, LEAVES, TORCH, BEDROCK = 0, 1, 2, 3, 4, 5, 6, 7, 8, 9
local SNOW, GLASS, COPPER_DIRT = 10, 11, 42   -- COPPER_DIRT: copper showing in the dirt layer

-- texture ids in one table (keeps the chunk under Lua's 200-locals limit)
local TX = { grasstop = 1, grassside = 2, dirt = 3, stone = 4, sand = 5, logtop = 6,
             logside = 7, leaves = 8, torch = 9, bedrock = 10, snow = 11, glass = 12,
             snowside = 28, copperdirt = 44 }
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
}
local oreTexOf = {}   -- [blockId] = textureId (ore blocks AND refined "Block of X"), for texIdFor
for _, m in ipairs(ORE_DEFS) do oreTexOf[m.id] = m.tex; oreTexOf[m.bid] = m.btex end

-- lighting
local LIGHT_MAX   = 15       -- full skylight
local TORCH_MAX   = 13       -- torch emission level
local LIGHT_AMB   = 0.34     -- minimum brightness multiplier in full darkness
local SKY_FALLOFF = 3        -- skylight lost per block of vertical cover (0 = hard shadow)

local SKY_TOP_R, SKY_TOP_G, SKY_TOP_B = 70, 130, 225
local SKY_HOR_R, SKY_HOR_G, SKY_HOR_B = 200, 225, 255
local WATER_R, WATER_G, WATER_B, WATER_A = 46, 110, 175, 150

local floor, sqrt, abs, min, max = math.floor, math.sqrt, math.abs, math.min, math.max
local sin = math.sin

-- ════════════════════════════════════════════════════════════════════════════════
-- State
-- ════════════════════════════════════════════════════════════════════════════════

local scene, dim = nil, nil
local fontBig, fontMid, fontSmall = nil, nil, nil

local vox = {}

-- Chunked mesh: the world is split into CHUNK×CHUNK columns (full height) in X/Z.
-- Each chunk keeps its own quad lists, so a block edit only remeshes the 1-5 chunks
-- it can affect instead of the whole world (the remesh hitch on break/place).
local CHUNK = 16
local ncx = floor((WX + CHUNK - 1) / CHUNK)
local ncz = floor((WZ + CHUNK - 1) / CHUNK)
local chunks = {}   -- [cz*ncx+cx+1] = { ci, cx, cz, minX,maxX,minZ,maxZ, cenX,cenZ }
                    -- (geometry lives in the scene's batches: tex id ci*8+dir, water id ci)

-- player
local feetX, feetY, feetZ = WX / 2, 76, WZ / 2
local vx, vy, vz = 0, 0, 0
local onGround = false
local camX, camY, camZ = 0, 0, 0
local yaw, pitch = 45.0, -18.0
local spawnX, spawnY, spawnZ = WX / 2, 24, WZ / 2   -- starting spot (Reset position)
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

-- hotbar: the placeable block types (bedrock is intentionally absent), the held index,
-- and the break/place auto-repeat timers
local hotbar = { GRASS, DIRT, STONE, SAND, WOOD, LEAVES, TORCH, GLASS, SNOW }
for _, m in ipairs(ORE_DEFS) do hotbar[#hotbar + 1] = m.id end    -- ore/gem blocks
hotbar[#hotbar + 1] = COPPER_DIRT                                 -- copper-in-dirt variant
for _, m in ipairs(ORE_DEFS) do hotbar[#hotbar + 1] = m.bid end   -- refined "Block of X"
local heldIdx = 1
local hotbarIcons = {}            -- [i] = LuaCanvas preview of hotbar[i]
local EDIT_INTERVAL = 0.16        -- seconds between edits while a mouse button is held
local breakTimer = 0
local placeTimer = 0

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
    if x < 0 or x >= WX or y < 0 or y >= WY or z < 0 or z >= WZ then return AIR end
    return vox[(y * WZ + z) * WX + x + 1]
end
local function setVox(x, y, z, b)
    if x < 0 or x >= WX or y < 0 or y >= WY or z < 0 or z >= WZ then return end
    vox[(y * WZ + z) * WX + x + 1] = b
end

-- ════════════════════════════════════════════════════════════════════════════════
-- Block predicates + lighting
-- ════════════════════════════════════════════════════════════════════════════════

-- Solid for collision. Air, water and torches are walk-through; glass is solid (you bump it).
local function isSolid(b) return b ~= AIR and b ~= WATER and b ~= TORCH end
-- Blocks skylight / torch light. Same as solid but glass lets light through.
local function blocksLight(b) return b ~= AIR and b ~= WATER and b ~= TORCH and b ~= GLASS end
-- A face is drawn when the neighbour is see-through (air, water, torch or glass).
local function isTransparent(b) return b == AIR or b == WATER or b == TORCH or b == GLASS end
-- Targetable by the crosshair (so torches/glass can be broken, water/air cannot).
local function isSelectable(b) return b ~= AIR and b ~= WATER end

-- Lighting = max(vertical skylight, torch block-light). Skylight is a cheap per-column
-- value (a cell is lit by sky only if nothing solid is above it in its column), so
-- tunnels and overhangs go dark. Torch light is a proper flood fill with occlusion,
-- updated incrementally so placing/removing a torch is cheap.
local skyTop = {}     -- [x*WZ+z+1] = lowest y that still sees the sky (everything >= it is lit)
local torchLight = {} -- [(y*WZ+z)*WX+x+1] = 0..TORCH_MAX
local torches = {}    -- [packed idx] = {x,y,z} of each torch, for the slim-post render pass
local torchesDirty = false   -- set when a torch is added/removed → rebuild the torch object
local _lq, _lqv = {}, {}   -- reused BFS queues (packed index / value)

local function recomputeSkyColumn(x, z)
    local top = 0
    for y = WY - 1, 0, -1 do
        if blocksLight(getVox(x, y, z)) then top = y + 1; break end
    end
    skyTop[x * WZ + z + 1] = top
end

local function skyAt(x, y, z)
    if x < 0 or x >= WX or y < 0 or y >= WY or z < 0 or z >= WZ then return 0 end
    local top = skyTop[x * WZ + z + 1] or 0
    if y >= top then return LIGHT_MAX end
    -- gradual falloff with depth of cover, so a block just under another isn't pitch black
    local v = LIGHT_MAX - (top - y) * SKY_FALLOFF
    return v > 0 and v or 0
end
local function torchAt(x, y, z)
    if x < 0 or x >= WX or y < 0 or y >= WY or z < 0 or z >= WZ then return 0 end
    return torchLight[(y * WZ + z) * WX + x + 1] or 0
end
-- Combined 0..LIGHT_MAX light reaching a (transparent) cell.
local function lightAt(x, y, z)
    local s = skyAt(x, y, z)
    local t = torchAt(x, y, z)
    return s >= t and s or t
end

-- Flood the torch light outward from every seed already written into _lq[1..qn].
local function propagateTorch(qn)
    local head = 1
    while head <= qn do
        local i = _lq[head]; head = head + 1
        local cl = torchLight[i] or 0
        if cl > 1 then
            local t = i - 1
            local x = t % WX; t = (t - x) // WX
            local z = t % WZ; local y = (t - z) // WZ
            local nl = cl - 1
            local function spread(nx, ny, nz)
                if nx < 0 or nx >= WX or ny < 0 or ny >= WY or nz < 0 or nz >= WZ then return end
                if blocksLight(getVox(nx, ny, nz)) then return end
                local ni = (ny * WZ + nz) * WX + nx + 1
                if (torchLight[ni] or 0) < nl then torchLight[ni] = nl; qn = qn + 1; _lq[qn] = ni end
            end
            spread(x - 1, y, z); spread(x + 1, y, z)
            spread(x, y - 1, z); spread(x, y + 1, z)
            spread(x, y, z - 1); spread(x, y, z + 1)
        end
    end
end

local function addTorchLight(x, y, z, level)
    local i0 = (y * WZ + z) * WX + x + 1
    if (torchLight[i0] or 0) >= level then return end
    torchLight[i0] = level
    _lq[1] = i0
    propagateTorch(1)
end

local function removeTorchLight(x, y, z)
    local i0 = (y * WZ + z) * WX + x + 1
    local startVal = torchLight[i0] or 0
    if startVal == 0 then return end
    torchLight[i0] = 0
    local qn = 1; _lq[1] = i0; _lqv[1] = startVal
    local relight, rn = {}, 0
    local head = 1
    while head <= qn do
        local i = _lq[head]; local val = _lqv[head]; head = head + 1
        local t = i - 1
        local x2 = t % WX; t = (t - x2) // WX
        local z2 = t % WZ; local y2 = (t - z2) // WZ
        local function visit(nx, ny, nz)
            if nx < 0 or nx >= WX or ny < 0 or ny >= WY or nz < 0 or nz >= WZ then return end
            if blocksLight(getVox(nx, ny, nz)) then return end
            local ni = (ny * WZ + nz) * WX + nx + 1
            local nv = torchLight[ni] or 0
            if nv ~= 0 and nv < val then
                torchLight[ni] = 0; qn = qn + 1; _lq[qn] = ni; _lqv[qn] = nv
            elseif nv >= val then
                rn = rn + 1; relight[rn] = ni   -- still lit by another source → re-seed
            end
        end
        visit(x2 - 1, y2, z2); visit(x2 + 1, y2, z2)
        visit(x2, y2 - 1, z2); visit(x2, y2 + 1, z2)
        visit(x2, y2, z2 - 1); visit(x2, y2, z2 + 1)
    end
    -- re-propagate from the surviving sources
    if rn > 0 then
        for k = 1, rn do _lq[k] = relight[k] end
        propagateTorch(rn)
    end
end

local function initLight()
    skyTop = {}
    for x = 0, WX - 1 do for z = 0, WZ - 1 do recomputeSkyColumn(x, z) end end
    torchLight = {}
    for y = 0, WY - 1 do for z = 0, WZ - 1 do for x = 0, WX - 1 do
        if getVox(x, y, z) == TORCH then addTorchLight(x, y, z, TORCH_MAX) end
    end end end
end

-- Plant a tree on the grass block at (x, groundY): trunk sits directly on the grass,
-- with a rounded leaf canopy. Sizes/jitter use math.random (organic, no noise banding).
local function plantTree(x, z, groundY)
    local th = 4 + math.random(0, 2)             -- trunk height 4..6
    for i = 1, th do setVox(x, groundY + i, z, WOOD) end
    local topY = groundY + th
    for dy = -2, 2 do
        local cy = topY + dy
        local r = (dy <= -1) and 2 or (dy <= 1 and 2 or 1)
        for dx = -r, r do for dz = -r, r do
            if dx * dx + dz * dz <= r * r + 1 and getVox(x + dx, cy, z + dz) == AIR then
                setVox(x + dx, cy, z + dz, LEAVES)
            end
        end end
    end
    setVox(x, topY + 2, z, LEAVES)               -- crown tip
end

local function generateWorld()
    -- Snow caps higher up; bare rock on the highest peaks (no grass/dirt over them).
    local SNOW_LEVEL = SEA_LEVEL + 18
    local STONE_PEAK = SEA_LEVEL + 26
    math.randomseed(SEED)
    vox = {}
    for i = 1, WX * WY * WZ do vox[i] = AIR end
    for x = 0, WX - 1 do
        for z = 0, WZ - 1 do
            local h = min(terrainHeight(x, z), WY - 1)
            local beach = (h <= SEA_LEVEL + 1)
            for y = 0, h do
                local b
                if y == 0 then b = BEDROCK                         -- unbreakable floor
                elseif y < h - 3 then b = STONE
                elseif y == h then                                 -- surface block by elevation
                    if beach then b = SAND
                    elseif h >= STONE_PEAK then b = STONE
                    elseif h >= SNOW_LEVEL then b = SNOW
                    else b = GRASS end
                else                                               -- subsurface cap
                    if beach then b = SAND
                    elseif h >= STONE_PEAK then b = STONE
                    else
                        b = DIRT
                        if math.random() < 0.02 then b = COPPER_DIRT end -- copper occasionally in the dirt layer
                    end
                end
                setVox(x, y, z, b)
            end
            for y = h + 1, SEA_LEVEL do setVox(x, y, z, WATER) end
        end
    end
    -- Ore + gem veins: random-walk clusters that replace stone, within each material's
    -- depth band. Vein count scales with map area.
    for _, m in ipairs(ORE_DEFS) do
        local veins = floor(m.rate * WX * WZ)
        for _ = 1, veins do
            local x, y, z = math.random(1, WX - 2), math.random(m.yMin, m.yMax), math.random(1, WZ - 2)
            for _ = 1, math.random(m.smin, m.smax) do
                if x >= 1 and x < WX - 1 and y >= 1 and y < WY - 1 and z >= 1 and z < WZ - 1 then
                    if getVox(x, y, z) == STONE then setVox(x, y, z, m.id) end
                else break end
                local d = math.random(6)
                if d == 1 then x = x + 1 elseif d == 2 then x = x - 1
                elseif d == 3 then y = y + 1 elseif d == 4 then y = y - 1
                elseif d == 5 then z = z + 1 else z = z - 1 end
            end
        end
    end
    -- Trees: scatter on real grass surfaces (math.random = organic, no lines), spaced out.
    local function surfaceY(x, z)
        for y = WY - 1, 1, -1 do if isSolid(getVox(x, y, z)) then return y end end
        return 0
    end
    for x = 2, WX - 3 do for z = 2, WZ - 3 do
        if math.random() < 0.020 then
            local y = surfaceY(x, z)
            if getVox(x, y, z) == GRASS and y < SNOW_LEVEL
               and getVox(x - 1, y + 1, z) ~= WOOD and getVox(x + 1, y + 1, z) ~= WOOD
               and getVox(x, y + 1, z - 1) ~= WOOD and getVox(x, y + 1, z + 1) ~= WOOD then
                plantTree(x, z, y)
            end
        end
    end end
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
        local dark = hash2(u, v, 41) < 0.18 and -26 or 0
        leaves[v * 16 + u + 1] = packc(60 + n + dark, 118 + n + dark, 46 + n + dark)
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
    elseif b == COPPER_DIRT then return TX.copperdirt end
    return oreTexOf[b] or TX.dirt
end

-- Build a 16×16 canvas preview for each hotbar block from its stored pixel table.
local function buildHotbarIcons()
    for i = 1, #hotbar do
        local px = texPix[iconTexId(hotbar[i])]
        local c = CANVAS:CreateCanvas(16, 16)
        if px then c:BlitPacked(px, 256) end
        hotbarIcons[i] = c
    end
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
    elseif b == COPPER_DIRT then return TX.copperdirt end
    return oreTexOf[b] or 0
end

local function cw(da, dv, pa, pval, qa, qval)
    local x, y, z = 0, 0, 0
    if da == 1 then x = dv elseif da == 2 then y = dv else z = dv end
    if pa == 1 then x = pval elseif pa == 2 then y = pval else z = pval end
    if qa == 1 then x = qval elseif qa == 2 then y = qval else z = qval end
    return x, y, z
end
local _vc = { 0, 0, 0 }
local function voxAt(da, dv, pa, pval, qa, qval)
    _vc[1] = 0; _vc[2] = 0; _vc[3] = 0
    _vc[da] = dv; _vc[pa] = pval; _vc[qa] = qval
    return getVox(_vc[1], _vc[2], _vc[3])
end

local _mask = {}   -- scratch face mask, reused across meshChunk calls

-- light → brightness multiplier (ambient floor so nothing is pure black)
local function liteMul(l) return LIGHT_AMB + (1 - LIGHT_AMB) * (l / LIGHT_MAX) end

-- Mesh one chunk column (X,Z in [c*CHUNK, ..), full Y) straight into the scene's geometry
-- batches: one textured batch per face direction (id = ci*8 + di) plus one flat batch for
-- water (id = ci). The geometry then lives in C# and is drawn per frame with one call per
-- bucket — no per-quad Lua↔C# traffic on the hot path.
local function meshChunk(cx, cz)
    local ci = cz * ncx + cx + 1
    local loX, hiX = cx * CHUNK, min((cx + 1) * CHUNK, WX)
    local loZ, hiZ = cz * CHUNK, min((cz + 1) * CHUNK, WZ)
    local ch = chunks[ci]
    if not ch then
        -- first time: create this chunk's retained objects (6 face directions + water).
        -- Bounds (chunk AABB) + face normals let the scene cull & order them internally.
        ch = { objTex = {} }
        chunks[ci] = ch
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
    end
    local objWater, objGlass = ch.objWater, ch.objGlass
    scene:ObjBegin(objWater)
    scene:ObjBegin(objGlass)
    local mask = _mask
    local lo = { loX, 0, loZ }
    local hi = { hiX, WY, hiZ }
    local nb1, nb2, nb3 = 0, 0, 0            -- scratch for the lit neighbour cell coords
    for di = 1, 6 do
        local cfg = DIRS[di]
        local da, sign, pa, qa = cfg[1], cfg[2], cfg[3], cfg[4]
        local shade = cfg[8]
        local ddLo, ddHi = lo[da], hi[da]
        local pLo, pHi = lo[pa], hi[pa]
        local qLo, qHi = lo[qa], hi[qa]
        local qSpan = qHi - qLo
        local objT = ch.objTex[di]
        scene:ObjBegin(objT)
        for dd = ddLo, ddHi - 1 do
            for p = pLo, pHi - 1 do
                local b0 = (p - pLo) * qSpan - qLo
                for q = qLo, qHi - 1 do
                    local b = voxAt(da, dd, pa, p, qa, q)
                    -- mask value: 0 none, -1 water, -100-light glass, else texId*32 + light
                    -- (so greedy only merges faces sharing texture+light; glass/water never
                    -- merge with opaque). Torches aren't meshed here (drawn as slim posts).
                    local val = 0
                    if b ~= AIR and b ~= TORCH then
                        local nb = voxAt(da, dd + sign, pa, p, qa, q)
                        -- glass shows a face against anything but glass; other blocks only
                        -- where the neighbour is see-through.
                        local show = (b == GLASS) and (nb ~= GLASS) or (b ~= GLASS and isTransparent(nb))
                        if show then
                            if b == WATER then
                                if di == 3 and nb == AIR then val = -1 end
                            else
                                nb1 = 0; nb2 = 0; nb3 = 0
                                if da == 1 then nb1 = dd + sign elseif da == 2 then nb2 = dd + sign else nb3 = dd + sign end
                                if pa == 1 then nb1 = p elseif pa == 2 then nb2 = p else nb3 = p end
                                if qa == 1 then nb1 = q elseif qa == 2 then nb2 = q else nb3 = q end
                                local lite = lightAt(nb1, nb2, nb3)
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
                        -- greedy rectangle merge: grow along q, then along p, over equal vals
                        local qh = 1
                        while q + qh < qHi and mask[b0 + q + qh + 1] == val do qh = qh + 1 end
                        local pw = 1; local grow = true
                        while p + pw < pHi and grow do
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
                            scene:ObjAddQuadFlat(objWater, x0,y0,z0, x1,y1,z1, x2,y2,z2, x3,y3,z3)
                        elseif val <= -100 then
                            local lite = -val - 100
                            scene:ObjAddQuadTex(objGlass, x0,y0,z0, x1,y1,z1, x2,y2,z2, x3,y3,z3,
                                TX.glass, pw, qh, shade * liteMul(lite))
                        else
                            local texId = val // 32
                            local lite = val - texId * 32
                            scene:ObjAddQuadTex(objT, x0,y0,z0, x1,y1,z1, x2,y2,z2, x3,y3,z3,
                                texId, pw, qh, shade * liteMul(lite))
                        end
                        q = q + qh
                    end
                end
            end
        end
    end
end

-- Full (re)mesh of every chunk — used once at startup.
local function greedyMesh()
    for cz = 0, ncz - 1 do
        for cx = 0, ncx - 1 do meshChunk(cx, cz) end
    end
end

-- Remesh just the chunks affected by a block change at (x,y,z): the block's own chunk
-- plus the chunks of its horizontal neighbours (their faces toward the block change).
local function remeshAt(x, y, z)
    local done = {}
    local function mark(bx, bz)
        if bx < 0 or bx >= WX or bz < 0 or bz >= WZ then return end
        local cx, cz = floor(bx / CHUNK), floor(bz / CHUNK)
        local ci = cz * ncx + cx + 1
        if not done[ci] then done[ci] = true; meshChunk(cx, cz) end
    end
    mark(x, z); mark(x - 1, z); mark(x + 1, z); mark(x, z - 1); mark(x, z + 1)
end

-- Remesh every chunk overlapping a world X/Z box (used when torch light changes,
-- which can reach up to TORCH_MAX blocks away).
local function remeshBox(x0, z0, x1, z1)
    local cx0 = max(0, floor(x0 / CHUNK)); local cx1 = min(ncx - 1, floor(x1 / CHUNK))
    local cz0 = max(0, floor(z0 / CHUNK)); local cz1 = min(ncz - 1, floor(z1 / CHUNK))
    for cz = cz0, cz1 do for cx = cx0, cx1 do meshChunk(cx, cz) end end
end

local torchCount = 0   -- when 0 we skip all torch-light work (the common, fast path)

-- The one entry point for editing a voxel: updates the block, the column's skylight and
-- the torch-light field, then remeshes only the chunks that can have changed.
local function setBlock(x, y, z, newType)
    local old = getVox(x, y, z)
    if old == newType then return end
    local wasSolid = blocksLight(old)   -- for light bookkeeping (glass doesn't block light)
    setVox(x, y, z, newType)
    recomputeSkyColumn(x, z)
    local nowSolid = blocksLight(newType)
    local tIdx = (y * WZ + z) * WX + x + 1
    if old == TORCH then torchCount = torchCount - 1; torches[tIdx] = nil; torchesDirty = true end
    if newType == TORCH then torchCount = torchCount + 1; torches[tIdx] = { x, y, z }; torchesDirty = true end

    local lightTouched = false
    if torchCount > 0 or old == TORCH or newType == TORCH then
        if old == TORCH then removeTorchLight(x, y, z); lightTouched = true end
        if (not wasSolid) and nowSolid and torchAt(x, y, z) > 0 then
            removeTorchLight(x, y, z); lightTouched = true              -- new block casts a shadow
        elseif wasSolid and (not nowSolid) then
            local qn = 0                                               -- opening lets light flow in
            local function seed(nx, ny, nz)
                if nx < 0 or nx >= WX or ny < 0 or ny >= WY or nz < 0 or nz >= WZ then return end
                if isSolid(getVox(nx, ny, nz)) then return end
                if torchAt(nx, ny, nz) > 0 then qn = qn + 1; _lq[qn] = (ny * WZ + nz) * WX + nx + 1 end
            end
            seed(x - 1, y, z); seed(x + 1, y, z); seed(x, y - 1, z)
            seed(x, y + 1, z); seed(x, y, z - 1); seed(x, y, z + 1)
            if qn > 0 then propagateTorch(qn); lightTouched = true end
        end
        if newType == TORCH then addTorchLight(x, y, z, TORCH_MAX); lightTouched = true end
    end

    if lightTouched then
        remeshBox(x - TORCH_MAX, z - TORCH_MAX, x + TORCH_MAX, z + TORCH_MAX)
    else
        remeshAt(x, y, z)
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

local function drawSky()
    local bands = 24
    local bh = RH / bands
    for i = 0, bands - 1 do
        local t = i / (bands - 1)
        scene:FillRect(0, floor(i * bh), RW, floor(bh) + 2,
            floor(SKY_TOP_R + (SKY_HOR_R - SKY_TOP_R) * t),
            floor(SKY_TOP_G + (SKY_HOR_G - SKY_TOP_G) * t),
            floor(SKY_TOP_B + (SKY_HOR_B - SKY_TOP_B) * t), 255)
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

-- Outline only the face the ray hit (the visible front face) instead of the whole
-- cube, so occluded back edges no longer poke through neighbouring blocks.
local function drawSelection()
    if not hasSel then return end
    local c = { faceCorners(selX, selY, selZ, selNx, selNy, selNz) }
    local sxv, syv, okv = {}, {}, {}
    for i = 1, 4 do
        local sx, sy, ok = project(c[i][1], c[i][2], c[i][3])
        sxv[i] = sx; syv[i] = sy; okv[i] = ok
    end
    for i = 1, 4 do
        local a, b = i, (i % 4) + 1
        if okv[a] and okv[b] then
            scene:DrawLine(floor(sxv[a]), floor(syv[a]), floor(sxv[b]), floor(syv[b]), 20, 20, 20)
        end
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
local torchObj = nil
local function rebuildTorches()
    if torchObj == nil then return end
    scene:ObjBegin(torchObj)
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
    end
end

-- Per frame: fill the sky background, let the scene render every object (cull/sort/raster
-- all internal now), then the 2D overlays on top.
local function renderFrame()
    if torchesDirty then rebuildTorches(); torchesDirty = false end
    drawSky()
    scene:SetCameraPosition(camX, camY, camZ)
    scene:Render()
    drawSelection()
    drawCrosshair()
    scene:Upload()
end

-- ════════════════════════════════════════════════════════════════════════════════
-- Lifecycle
-- ════════════════════════════════════════════════════════════════════════════════

function onStart()
    scene = SCENE3D:CreateScene(RW, RH)
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
    buildHotbarIcons()
    torchObj = scene:NewObject()                      -- one retained object for all torches
    scene:ObjSetPass(torchObj, 0, 0, 0, 0, 255)       -- opaque (drawn before water)
    generateWorld()
    initLight()
    greedyMesh()
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
    underwater = getVox(floor(camX), floor(camY), floor(camZ)) == WATER
    local swim = underwater or getVox(floor(feetX), floor(feetY + 0.1), floor(feetZ)) == WATER

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

    -- keep the player inside the world bounds (WX/WY/WZ are the map size, set at the top)
    if feetX < PW then feetX = PW; vx = 0 elseif feetX > WX - PW then feetX = WX - PW; vx = 0 end
    if feetZ < PW then feetZ = PW; vz = 0 elseif feetZ > WZ - PW then feetZ = WZ - PW; vz = 0 end
    if feetY < 0 then feetY = 0; vy = 0 end

    camX, camY, camZ = feetX, feetY + EYE, feetZ

    -- hotbar selection via mouse wheel
    local _, sdy = INPUT:GetScrollDelta()
    if sdy ~= 0 then
        local n = #hotbar
        if sdy > 0 then heldIdx = heldIdx % n + 1 else heldIdx = (heldIdx - 2) % n + 1 end
    end

    -- block pick
    local bx, by, bz, fnx, fny, fnz = pickBlock()
    if bx then hasSel = true; selX = bx; selY = by; selZ = bz; selNx = fnx; selNy = fny; selNz = fnz else hasSel = false end

    -- break (hold to repeat). Bedrock is unbreakable.
    if hasSel and INPUT:MousePressing("Left") then
        breakTimer = breakTimer - dt
        if INPUT:MousePressed("Left") or breakTimer <= 0 then
            if getVox(selX, selY, selZ) ~= BEDROCK then setBlock(selX, selY, selZ, AIR) end
            breakTimer = EDIT_INTERVAL
            hasSel = false   -- re-picked next frame; keeps digging into the block behind
        end
    else
        breakTimer = 0
    end

    -- place the held block against the targeted face (hold to repeat).
    if hasSel and INPUT:MousePressing("Right") then
        placeTimer = placeTimer - dt
        if INPUT:MousePressed("Right") or placeTimer <= 0 then
            local px, py, pz = selX + selNx, selY + selNy, selZ + selNz
            local b = hotbar[heldIdx]
            -- torches are non-solid, so they may sit where the player stands; solids may not
            local hitsPlayer = b ~= TORCH
                and (feetX + PW > px) and (feetX - PW < px + 1)
                and (feetY + PH > py) and (feetY < py + 1)
                and (feetZ + PW > pz) and (feetZ - PW < pz + 1)
            if getVox(px, py, pz) == AIR and not hitsPlayer then setBlock(px, py, pz, b) end
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

local BLOCK_NAMES = { [AIR] = "Air", [GRASS] = "Grass", [DIRT] = "Dirt", [STONE] = "Stone",
                      [SAND] = "Sand", [WATER] = "Water", [WOOD] = "Wood", [LEAVES] = "Leaves",
                      [TORCH] = "Torch", [BEDROCK] = "Bedrock", [SNOW] = "Snow", [GLASS] = "Glass" }
for _, m in ipairs(ORE_DEFS) do
    BLOCK_NAMES[m.id] = m.gem and m.name or (m.name .. " Ore")   -- "Gold Ore"; gems keep their name
    BLOCK_NAMES[m.bid] = "Block of " .. m.name                   -- "Block of Iron"
end
BLOCK_NAMES[COPPER_DIRT] = "Copper Ore"

-- Held-block widget in the bottom-left: ‹ icon ›, with the block name under it.
local function drawHotbar()
    local icon = hotbarIcons[heldIdx]
    if not icon then return end
    local scale = 6
    local sz = 16 * scale              -- icon size in screen px
    local x = 72
    local y = SCREEN_H - sz - 80
    local pad = 14
    -- dark panel behind the icon (dim is a 2×2 white canvas: scale s → 2s px)
    dim:SetColor(0.07, 0.07, 0.09)
    dim:SetOpacity(0.5)
    dim:SetScale((sz + pad * 2) / 2, (sz + pad * 2) / 2)
    dim:Draw(x - pad, y - pad)
    -- block icon
    icon:SetColor(1, 1, 1); icon:SetOpacity(1)
    icon:SetScale(scale, scale)
    icon:Draw(x, y)
    -- left / right arrows (mouse wheel cycles the selection)
    txt(fontBig, "<", 235, 235, 240):DrawAtAnchor(x - pad - 26, y + sz / 2, "center")
    txt(fontBig, ">", 235, 235, 240):DrawAtAnchor(x + sz + pad + 26, y + sz / 2, "center")
    -- block id over the block name
    local bid = hotbar[heldIdx]
    txt(fontSmall, "ID " .. bid, 190, 205, 235):DrawAtAnchor(x + sz / 2, y + sz + pad + 6, "top")
    txt(fontSmall, BLOCK_NAMES[bid] or "?", 235, 235, 240):DrawAtAnchor(x + sz / 2, y + sz + pad + 30, "top")
end

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
    end

    txt(fontMid, "Voxel Engine", 255, 255, 255):Draw(24, 16)
    if showHelp then
        txt(fontSmall, "WASD/arrows move   Space jump/swim   LCtrl sprint   mouse look   L-click break   R-click place   wheel: select   Esc: pause",
            235, 235, 235):Draw(24, 52)
    end

    if not paused then
        drawHotbar()
        -- hovered block: name in a textbox centred at the bottom
        if hasSel then
            local nm = BLOCK_NAMES[getVox(selX, selY, selZ)] or "?"
            local bw, bh = 360, 56
            local bx, by = (SCREEN_W - bw) / 2, SCREEN_H - bh - 40
            dim:SetColor(0.07, 0.07, 0.09); dim:SetOpacity(0.55)
            dim:SetScale(bw / 2, bh / 2); dim:Draw(bx, by)
            txt(fontMid, nm, 245, 245, 250):DrawAtAnchor(SCREEN_W / 2, by + bh / 2, "center")
        end
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
