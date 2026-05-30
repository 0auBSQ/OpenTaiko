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

local WX, WY, WZ = 128, 40, 128
local SEA_LEVEL  = 14
local SEED       = 1337

local AIR, GRASS, DIRT, STONE, SAND, WATER, WOOD, LEAVES, TORCH, BEDROCK = 0, 1, 2, 3, 4, 5, 6, 7, 8, 9
local T_GRASS_TOP, T_GRASS_SIDE, T_DIRT, T_STONE, T_SAND, T_LOG_TOP, T_LOG_SIDE, T_LEAVES = 1, 2, 3, 4, 5, 6, 7, 8
local T_TORCH, T_BEDROCK = 9, 10

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
local feetX, feetY, feetZ = WX / 2, 24, WZ / 2
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
local hotbar = { GRASS, DIRT, STONE, SAND, WOOD, LEAVES, TORCH }
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
local function terrainHeight(x, z)
    local n = vnoise(x * 0.045, z * 0.045) * 1.0 + vnoise(x * 0.11, z * 0.11) * 0.45 + vnoise(x * 0.23, z * 0.23) * 0.22
    return floor(6 + (n / 1.67) * 26)
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

-- Solid for collision AND for blocking light/sky. Water and torches are non-solid.
local function isSolid(b) return b ~= AIR and b ~= WATER and b ~= TORCH end
-- A face is drawn when the neighbour is see-through (air, water or a torch).
local function isTransparent(b) return b == AIR or b == WATER or b == TORCH end
-- Targetable by the crosshair (so torches can be broken, water/air cannot).
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
        if isSolid(getVox(x, y, z)) then top = y + 1; break end
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
                if isSolid(getVox(nx, ny, nz)) then return end
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
            if isSolid(getVox(nx, ny, nz)) then return end
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

local function plantTree(x, z, groundY)
    local th = 4 + floor(hash2(x, z, 31) * 3)
    for i = 1, th do setVox(x, groundY + i, z, WOOD) end
    local topY = groundY + th
    for dy = -1, 2 do
        local r = (dy <= 0) and 2 or 1
        local cy = topY + dy
        for dx = -r, r do for dz = -r, r do
            if abs(dx) + abs(dz) <= r + 1 and getVox(x + dx, cy, z + dz) == AIR then
                setVox(x + dx, cy, z + dz, LEAVES)
            end
        end end
    end
    setVox(x, topY + 3, z, LEAVES)
end

local function generateWorld()
    vox = {}
    for i = 1, WX * WY * WZ do vox[i] = AIR end
    for x = 0, WX - 1 do
        for z = 0, WZ - 1 do
            local h = min(terrainHeight(x, z), WY - 1)
            local beach = (h <= SEA_LEVEL + 1)
            for y = 0, h do
                local b
                if y == 0 then b = BEDROCK              -- unbreakable floor
                elseif y == h then b = beach and SAND or GRASS
                elseif y >= h - 3 then b = beach and SAND or DIRT
                else b = STONE end
                setVox(x, y, z, b)
            end
            for y = h + 1, SEA_LEVEL do setVox(x, y, z, WATER) end
        end
    end
    for x = 2, WX - 3 do for z = 2, WZ - 3 do
        local h = terrainHeight(x, z)
        if h > SEA_LEVEL + 1 and h < WY - 8 and getVox(x, h, z) == GRASS then
            if hash2(x, z, 9991) < 0.018 then plantTree(x, h, z) end
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
local texPix = {}   -- [texId] = packed-RGB pixel table (kept for hotbar icon previews)
local function registerTextures()
    local function reg(id, t) texPix[id] = t; scene:RegisterTexture(id, t, 16, 16) end
    reg(T_GRASS_TOP, texFlat(96, 152, 60, 22, 3))
    reg(T_DIRT, texFlat(134, 96, 67, 20, 7))
    reg(T_STONE, texFlat(122, 122, 128, 22, 11))
    reg(T_SAND, texFlat(214, 201, 146, 16, 17))
    local leaves = {}
    for v = 0, 15 do for u = 0, 15 do
        local n = (hash2(u, v, 23) - 0.5) * 40
        local dark = hash2(u, v, 41) < 0.18 and -26 or 0
        leaves[v * 16 + u + 1] = packc(60 + n + dark, 118 + n + dark, 46 + n + dark)
    end end
    reg(T_LEAVES, leaves)
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
    reg(T_GRASS_SIDE, gs)
    local lsd = {}
    for v = 0, 15 do for u = 0, 15 do
        local streak = (u % 4 == 0) and -22 or 0
        local n = (hash2(u, v, 5) - 0.5) * 16
        lsd[v * 16 + u + 1] = packc(120 + n + streak, 85 + n + streak, 50 + n + streak)
    end end
    reg(T_LOG_SIDE, lsd)
    local lt = {}
    for v = 0, 15 do for u = 0, 15 do
        local d = sqrt((u - 7.5) ^ 2 + (v - 7.5) ^ 2)
        local ring = (floor(d) % 2 == 0) and 18 or -10
        local n = (hash2(u, v, 9) - 0.5) * 10
        lt[v * 16 + u + 1] = packc(165 + ring + n, 125 + ring + n, 78 + ring + n)
    end end
    reg(T_LOG_TOP, lt)
    local bd = {}
    for v = 0, 15 do for u = 0, 15 do
        local n = (hash2(u, v, 13) - 0.5) * 2 * 26
        local blot = hash2(u + 5, v + 5, 71) < 0.22 and -28 or 0
        bd[v * 16 + u + 1] = packc(58 + n + blot, 58 + n + blot, 64 + n + blot)
    end end
    reg(T_BEDROCK, bd)
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
    reg(T_TORCH, tc)
end

-- Representative face texture for a block's hotbar icon.
local function iconTexId(b)
    if b == GRASS then return T_GRASS_SIDE
    elseif b == DIRT then return T_DIRT
    elseif b == STONE then return T_STONE
    elseif b == SAND then return T_SAND
    elseif b == WOOD then return T_LOG_SIDE
    elseif b == LEAVES then return T_LEAVES
    elseif b == TORCH then return T_TORCH
    elseif b == BEDROCK then return T_BEDROCK end
    return T_DIRT
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
        if di == 3 then return T_GRASS_TOP elseif di == 4 then return T_DIRT else return T_GRASS_SIDE end
    elseif b == DIRT then return T_DIRT
    elseif b == STONE then return T_STONE
    elseif b == SAND then return T_SAND
    elseif b == WOOD then if di == 3 or di == 4 then return T_LOG_TOP else return T_LOG_SIDE end
    elseif b == LEAVES then return T_LEAVES
    elseif b == TORCH then return T_TORCH
    elseif b == BEDROCK then return T_BEDROCK end
    return 0
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
    end
    local objWater = ch.objWater
    scene:ObjBegin(objWater)
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
                    -- mask value: 0 none, -1 water, else texId*32 + light (so greedy only
                    -- merges faces that share BOTH texture and light level). Torches are not
                    -- meshed here — they're drawn separately as a slim post.
                    local val = 0
                    if b ~= AIR and b ~= TORCH then
                        local nb = voxAt(da, dd + sign, pa, p, qa, q)
                        if isTransparent(nb) then
                            if b == WATER then
                                if di == 3 and nb == AIR then val = -1 end
                            else
                                nb1 = 0; nb2 = 0; nb3 = 0
                                if da == 1 then nb1 = dd + sign elseif da == 2 then nb2 = dd + sign else nb3 = dd + sign end
                                if pa == 1 then nb1 = p elseif pa == 2 then nb2 = p else nb3 = p end
                                if qa == 1 then nb1 = q elseif qa == 2 then nb2 = q else nb3 = q end
                                val = texIdFor(b, di) * 32 + lightAt(nb1, nb2, nb3)
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
                        if val < 0 then
                            scene:ObjAddQuadFlat(objWater, x0,y0,z0, x1,y1,z1, x2,y2,z2, x3,y3,z3)
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
    local wasSolid = isSolid(old)
    setVox(x, y, z, newType)
    recomputeSkyColumn(x, z)
    local nowSolid = isSolid(newType)
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
        scene:ObjAddQuadTex(torchObj, x0,y0,z0, x1,y0,z0, x1,y1,z0, x0,y1,z0, T_TORCH, 1, 1, 1.0) -- -Z
        scene:ObjAddQuadTex(torchObj, x1,y0,z1, x0,y0,z1, x0,y1,z1, x1,y1,z1, T_TORCH, 1, 1, 1.0) -- +Z
        scene:ObjAddQuadTex(torchObj, x0,y0,z1, x0,y0,z0, x0,y1,z0, x0,y1,z1, T_TORCH, 1, 1, 1.0) -- -X
        scene:ObjAddQuadTex(torchObj, x1,y0,z0, x1,y0,z1, x1,y1,z1, x1,y1,z0, T_TORCH, 1, 1, 1.0) -- +X
        scene:ObjAddQuadTex(torchObj, x0,y1,z0, x1,y1,z0, x1,y1,z1, x0,y1,z1, T_TORCH, 1, 1, 1.0) -- top
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

local BLOCK_NAMES = { [GRASS] = "Grass", [DIRT] = "Dirt", [STONE] = "Stone",
                      [SAND] = "Sand", [WOOD] = "Wood", [LEAVES] = "Leaves", [TORCH] = "Torch" }

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
    -- block name
    txt(fontSmall, BLOCK_NAMES[hotbar[heldIdx]] or "?", 230, 230, 235)
        :DrawAtAnchor(x + sz / 2, y + sz + pad + 8, "top")
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

    if not paused then drawHotbar() end

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
