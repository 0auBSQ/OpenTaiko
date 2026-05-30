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

local WX, WY, WZ = 64, 40, 64
local SEA_LEVEL  = 14
local SEED       = 1337

local AIR, GRASS, DIRT, STONE, SAND, WATER, WOOD, LEAVES = 0, 1, 2, 3, 4, 5, 6, 7
local T_GRASS_TOP, T_GRASS_SIDE, T_DIRT, T_STONE, T_SAND, T_LOG_TOP, T_LOG_SIDE, T_LEAVES = 1, 2, 3, 4, 5, 6, 7, 8

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
local chunks = {}   -- [cz*ncx + cx + 1] = { mq = {}, wq = {}, nMq = 0, nWq = 0 }

-- player
local feetX, feetY, feetZ = WX / 2, 24, WZ / 2
local vx, vy, vz = 0, 0, 0
local onGround = false
local camX, camY, camZ = 0, 0, 0
local yaw, pitch = 45.0, -18.0
-- camera basis mirrored from the scene (forward + right's horizontal components,
-- the only parts Lua needs for movement / picking / face-culling)
local Fx, Fy, Fz = 0, 0, 1
local Rx, Rz = 1, 0
local underwater = false

-- block selection
local hasSel = false
local selX, selY, selZ = 0, 0, 0
local selNx, selNy, selNz = 0, 1, 0

-- hotbar: the placeable block types, the held index, and break auto-repeat timer
local hotbar = { GRASS, DIRT, STONE, SAND, WOOD, LEAVES }
local heldIdx = 1
local hotbarIcons = {}            -- [i] = LuaCanvas preview of hotbar[i]
local BREAK_INTERVAL = 0.16       -- seconds between breaks while the button is held
local breakTimer = 0

-- pause menu
local paused = false
local menuItems = { "Resume", "Quit" }
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
                if y == h then b = beach and SAND or GRASS
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
end

-- Representative face texture for a block's hotbar icon.
local function iconTexId(b)
    if b == GRASS then return T_GRASS_SIDE
    elseif b == DIRT then return T_DIRT
    elseif b == STONE then return T_STONE
    elseif b == SAND then return T_SAND
    elseif b == WOOD then return T_LOG_SIDE
    elseif b == LEAVES then return T_LEAVES end
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
    elseif b == LEAVES then return T_LEAVES end
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

-- Mesh one chunk column (X,Z in [c*CHUNK, ..), full Y) into its own quad lists.
-- Each block's visible faces are emitted by the chunk that owns the block, so the
-- union of all chunks is identical to meshing the whole world at once.
local function meshChunk(cx, cz)
    local ci = cz * ncx + cx + 1
    local ch = chunks[ci]
    if not ch then ch = { mq = {}, wq = {}, nMq = 0, nWq = 0 }; chunks[ci] = ch end
    local mq, wq = ch.mq, ch.wq
    local nMq, nWq = 0, 0
    local mask = _mask
    -- per-axis world bounds for this chunk: axis 1=X, 2=Y, 3=Z
    local loX, hiX = cx * CHUNK, min((cx + 1) * CHUNK, WX)
    local loZ, hiZ = cz * CHUNK, min((cz + 1) * CHUNK, WZ)
    local lo = { loX, 0, loZ }
    local hi = { hiX, WY, hiZ }
    for di = 1, 6 do
        local cfg = DIRS[di]
        local da, sign, pa, qa = cfg[1], cfg[2], cfg[3], cfg[4]
        local nx, ny, nz, shade = cfg[5], cfg[6], cfg[7], cfg[8]
        local ddLo, ddHi = lo[da], hi[da]
        local pLo, pHi = lo[pa], hi[pa]
        local qLo, qHi = lo[qa], hi[qa]
        local qSpan = qHi - qLo
        for dd = ddLo, ddHi - 1 do
            for p = pLo, pHi - 1 do
                local b0 = (p - pLo) * qSpan - qLo
                for q = qLo, qHi - 1 do
                    local b = voxAt(da, dd, pa, p, qa, q)
                    local key = 0
                    if b ~= AIR then
                        local nb = voxAt(da, dd + sign, pa, p, qa, q)
                        if nb == AIR or nb == WATER then
                            if b == WATER then
                                if di == 3 and nb == AIR then key = -1 end
                            else key = texIdFor(b, di) end
                        end
                    end
                    mask[b0 + q + 1] = key
                end
            end
            local planePos = dd + (sign > 0 and 1 or 0)
            for p = pLo, pHi - 1 do
                local b0 = (p - pLo) * qSpan - qLo
                local q = qLo
                while q < qHi do
                    local key = mask[b0 + q + 1]
                    if key == 0 then q = q + 1
                    else
                        local qh, pw = 1, 1
                        mask[b0 + q + 1] = 0
                        local x0, y0, z0 = cw(da, planePos, pa, p, qa, q)
                        local x1, y1, z1 = cw(da, planePos, pa, p + pw, qa, q)
                        local x2, y2, z2 = cw(da, planePos, pa, p + pw, qa, q + qh)
                        local x3, y3, z3 = cw(da, planePos, pa, p, qa, q + qh)
                        local ccx, ccy, ccz = cw(da, planePos, pa, p + pw * 0.5, qa, q + qh * 0.5)
                        if key < 0 then
                            nWq = nWq + 1
                            wq[nWq] = { x0,y0,z0, x1,y1,z1, x2,y2,z2, x3,y3,z3, ccx,ccy,ccz, nx,ny,nz }
                        else
                            nMq = nMq + 1
                            mq[nMq] = { x0,y0,z0, x1,y1,z1, x2,y2,z2, x3,y3,z3, pw, qh, key, shade, ccx,ccy,ccz, nx,ny,nz }
                        end
                        q = q + qh
                    end
                end
            end
        end
    end
    -- drop quads left over from a previously larger mesh, then commit the new counts
    for i = nMq + 1, ch.nMq do mq[i] = nil end
    for i = nWq + 1, ch.nWq do wq[i] = nil end
    ch.nMq = nMq; ch.nWq = nWq
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

local function isSolid(b) return b ~= AIR and b ~= WATER end

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
    if isSolid(getVox(ix, iy, iz)) then return ix, iy, iz, fnx, fny, fnz end
    for _ = 1, 64 do
        local t
        if tMaxX < tMaxY and tMaxX < tMaxZ then t = tMaxX; ix = ix + stepX; tMaxX = tMaxX + tDX; fnx, fny, fnz = -stepX, 0, 0
        elseif tMaxY < tMaxZ then t = tMaxY; iy = iy + stepY; tMaxY = tMaxY + tDY; fnx, fny, fnz = 0, -stepY, 0
        else t = tMaxZ; iz = iz + stepZ; tMaxZ = tMaxZ + tDZ; fnx, fny, fnz = 0, 0, -stepZ end
        if t > REACH then return nil end
        if isSolid(getVox(ix, iy, iz)) then return ix, iy, iz, fnx, fny, fnz end
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

local function renderFrame()
    drawSky()
    scene:ClearDepth()
    scene:SetCameraPosition(camX, camY, camZ)
    local cxm, cym, czm = camX, camY, camZ
    local fgx, fgy, fgz = Fx, Fy, Fz
    local nChunks = ncx * ncz

    -- Opaque pass over every chunk first, then the water pass — water alpha must blend
    -- over already-drawn terrain, so all opaque faces have to land before any water.
    for ci = 1, nChunks do
        local ch = chunks[ci]
        if ch then
            local mq, nMq = ch.mq, ch.nMq
            for i = 1, nMq do
                local f = mq[i]
                local dxc = cxm - f[17]; local dyc = cym - f[18]; local dzc = czm - f[19]
                if f[20] * dxc + f[21] * dyc + f[22] * dzc > 0 and (fgx * dxc + fgy * dyc + fgz * dzc) <= 0.5 then
                    scene:FillQuadWorldTex(f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9], f[10], f[11], f[12],
                        f[15], f[13], f[14], f[16], 255)
                end
            end
        end
    end
    for ci = 1, nChunks do
        local ch = chunks[ci]
        if ch then
            local wq, nWq = ch.wq, ch.nWq
            for i = 1, nWq do
                local f = wq[i]
                local dxc = cxm - f[13]; local dyc = cym - f[14]; local dzc = czm - f[15]
                if f[16] * dxc + f[17] * dyc + f[18] * dzc > 0 and (fgx * dxc + fgy * dyc + fgz * dzc) <= 0.5 then
                    scene:FillQuadWorld(f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9], f[10], f[11], f[12],
                        WATER_R, WATER_G, WATER_B, WATER_A)
                end
            end
        end
    end

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
    registerTextures()
    buildHotbarIcons()
    generateWorld()
    greedyMesh()
    local gh = terrainHeight(floor(feetX), floor(feetZ))
    feetY = max(gh + 1, SEA_LEVEL + 1)
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
local MENU_RECTS = { { 960, 470, 360, 72 }, { 960, 560, 360, 72 } }

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

    -- break (hold to repeat, only remeshing the affected chunks)
    if hasSel and INPUT:MousePressing("Left") then
        breakTimer = breakTimer - dt
        if INPUT:MousePressed("Left") or breakTimer <= 0 then
            setVox(selX, selY, selZ, AIR)
            remeshAt(selX, selY, selZ)
            breakTimer = BREAK_INTERVAL
            hasSel = false   -- re-picked next frame; keeps digging into the block behind
        end
    else
        breakTimer = 0
    end

    -- place the held block against the targeted face (right click)
    if hasSel and INPUT:MousePressed("Right") then
        local px, py, pz = selX + selNx, selY + selNy, selZ + selNz
        local hitsPlayer = (feetX + PW > px) and (feetX - PW < px + 1)
            and (feetY + PH > py) and (feetY < py + 1)
            and (feetZ + PW > pz) and (feetZ - PW < pz + 1)
        if getVox(px, py, pz) == AIR and not hitsPlayer then
            setVox(px, py, pz, hotbar[heldIdx])
            remeshAt(px, py, pz)
        end
    end

    renderFrame()
end

local function menuActivate(idx)
    if menuItems[idx] == "Resume" then
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
                      [SAND] = "Sand", [WOOD] = "Wood", [LEAVES] = "Leaves" }

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
