---@diagnostic disable: undefined-global, undefined-field, need-check-nil, lowercase-global
-- rt_demo/Script.lua — Showcase for the Lua3DScene path-traced raytracer mode.
--
-- The same SCENE3D window that the voxel stage rasterizes is here switched to "raytrace"
-- mode (scene:SetMode). A progressive Monte-Carlo path tracer accumulates one sample per
-- pixel each frame: the image is grainy while you move and converges to a clean, globally
-- illuminated render when you hold still (camera / scene edits reset the accumulation).
--
-- Controls:
--   Move: WASD     Up/Down: Space / LeftCtrl     Faster: LeftShift
--   Look: hold Left-click and drag (rotating resets the accumulation, so it's opt-in)
--   Switch scene: 1 Museum  2 Materials  3 Implicit/SDF  4 Cornell  5 Voxel blocks
--                 6 Coloured lights  7 Emissive torus + diamond  8 Normal maps
--   Quit: Escape

-- ════════════════════════════════════════════════════════════════════════════════
-- Config / state
-- ════════════════════════════════════════════════════════════════════════════════

local SCREEN_W, SCREEN_H = 1920, 1080
local RW, RH      = 480, 270        -- internal path-trace resolution (kept low; tracing is heavy)
local FOV         = 65.0
local NEAR        = 0.05
local MOUSE_SENS  = 0.14            -- deg per mouse pixel
local MOVE_SPEED  = 4.0             -- units / sec
local FAST_MULT   = 3.0
local THREADS     = 8               -- path-trace threads (screen split into row bands)

-- procedurally-generated texture ids (built once in onStart)
local CHECKER    = 1                 -- material-scene floor
local GRASS_TOP  = 2
local GRASS_SIDE = 3
local DIRT       = 4
local TIN        = 5                 -- tin block albedo (metal)
local BRICK_ALB  = 6                 -- brick albedo  (normal-map scene)
local BRICK_NRM  = 7                 -- brick tangent-space normal map

local sin, cos, rad, floor, rnd = math.sin, math.cos, math.rad, math.floor, math.random

local scene
local fontMid, fontSmall
local objs = {}                     -- scene-object ids of the current scene (deleted on switch)
local sceneName = ""
local cam = { x = 0, y = 2, z = 6, yaw = 180, pitch = -6 }
local lastTs = 0

-- ════════════════════════════════════════════════════════════════════════════════
-- Helpers
-- ════════════════════════════════════════════════════════════════════════════════

local function txt(font, str, r, g, b)
    return font:GetText(str, false, 1800,
        COLOR:CreateColorFromRGBA(r or 255, g or 255, b or 255, 255),
        COLOR:CreateColorFromRGBA(0, 0, 0, 255))
end

local function clearObjs()
    for i = 1, #objs do scene:DeleteObject(objs[i]) end
    objs = {}
end

-- Flat (material-driven) quad from four corner tables {x,y,z}.
local function quadFlat(a, b, c, d, mat)
    local id = scene:NewObject()
    scene:ObjAddQuadFlat(id, a[1], a[2], a[3], b[1], b[2], b[3], c[1], c[2], c[3], d[1], d[2], d[3])
    if mat then scene:ObjSetMaterial(id, mat) end
    objs[#objs + 1] = id
    return id
end

-- Textured quad (gives UVs + albedo from a texture; demonstrates textured-quad RT). An optional
-- material overrides the albedo (and can add a metal/normal-map look while keeping the UVs).
local function quadTex(a, b, c, d, tex, um, vm, mat)
    local id = scene:NewObject()
    scene:ObjAddQuadTex(id, a[1], a[2], a[3], b[1], b[2], b[3], c[1], c[2], c[3], d[1], d[2], d[3],
        tex, um or 1, vm or 1, 1.0)
    if mat then scene:ObjSetMaterial(id, mat) end
    objs[#objs + 1] = id
    return id
end

-- A textured cube (six quads) spanning [x..x+s]^3, with per-face textures and an optional
-- material (used by the voxel-block scene for the grass cube and the metallic tin cube).
local function texCube(x, y, z, s, topTex, sideTex, botTex, mat)
    local x1, y1, z1 = x + s, y + s, z + s
    quadTex({x,y1,z},{x1,y1,z},{x1,y1,z1},{x,y1,z1}, topTex, 1, 1, mat)   -- top  (+y)
    quadTex({x,y,z},{x1,y,z},{x1,y,z1},{x,y,z1},     botTex, 1, 1, mat)   -- bottom (-y)
    quadTex({x,y,z},{x1,y,z},{x1,y1,z},{x,y1,z},     sideTex, 1, 1, mat)  -- -z
    quadTex({x,y,z1},{x1,y,z1},{x1,y1,z1},{x,y1,z1}, sideTex, 1, 1, mat)  -- +z
    quadTex({x,y,z},{x,y,z1},{x,y1,z1},{x,y1,z},     sideTex, 1, 1, mat)  -- -x
    quadTex({x1,y,z},{x1,y,z1},{x1,y1,z1},{x1,y1,z}, sideTex, 1, 1, mat)  -- +x
end

local function diffuse(r, g, b)
    local m = scene:NewMaterial(); scene:MatSetType(m, "diffuse"); scene:MatSetAlbedo(m, r, g, b); return m
end
local function metal(r, g, b, rough)
    local m = scene:NewMaterial(); scene:MatSetType(m, "metal"); scene:MatSetAlbedo(m, r, g, b)
    scene:MatSetRoughness(m, rough or 0.0); return m
end
local function glass(ior)
    local m = scene:NewMaterial(); scene:MatSetType(m, "glass"); scene:MatSetAlbedo(m, 0.96, 0.98, 1.0)
    scene:MatSetIOR(m, ior or 1.5); return m
end
local function emissive(r, g, b, strength)
    local m = scene:NewMaterial(); scene:MatSetType(m, "emissive"); scene:MatSetEmission(m, r, g, b, strength)
    return m
end

local function applyCamera()
    scene:SetCameraPosition(cam.x, cam.y, cam.z)
    scene:SetCameraAngles(cam.yaw, cam.pitch)
end

-- ════════════════════════════════════════════════════════════════════════════════
-- Scenes
-- ════════════════════════════════════════════════════════════════════════════════

-- 1) Museum room: a box room with 4 pillars in a square, each topped by a sphere of a
--    distinct material — glass, polished aluminium, emissive yellow, and wood (normal-mapped).
local function buildMuseum()
    scene:ClearRaytraceModel(); clearObjs()
    sceneName = "Museum room"
    local W, H, d = 7.0, 5.0, 3.2

    local mWall  = diffuse(0.72, 0.72, 0.74)
    local mFloor = diffuse(0.40, 0.41, 0.45)
    local mCeil  = diffuse(0.80, 0.80, 0.82)
    local mPil   = diffuse(0.62, 0.60, 0.56)

    -- room (six big quads). RT treats them double-sided, so winding is irrelevant.
    quadFlat({-W,0,-W},{ W,0,-W},{ W,0, W},{-W,0, W}, mFloor)            -- floor
    quadFlat({-W,H,-W},{ W,H,-W},{ W,H, W},{-W,H, W}, mCeil)             -- ceiling
    quadFlat({-W,0,-W},{ W,0,-W},{ W,H,-W},{-W,H,-W}, mWall)             -- back  (-z)
    quadFlat({-W,0, W},{ W,0, W},{ W,H, W},{-W,H, W}, mWall)             -- front (+z)
    quadFlat({-W,0,-W},{-W,0, W},{-W,H, W},{-W,H,-W}, mWall)             -- left  (-x)
    quadFlat({ W,0,-W},{ W,0, W},{ W,H, W},{ W,H,-W}, mWall)             -- right (+x)

    local ph, pr, sr = 2.4, 0.45, 0.7
    local mGlass = glass(1.5)
    local mAlu   = metal(0.91, 0.92, 0.93, 0.04)
    local mEmis  = emissive(1.0, 0.84, 0.34, 9.0)
    local mWood  = scene:NewMaterial()
    scene:MatSetType(mWood, "diffuse"); scene:MatSetAlbedo(mWood, 0.46, 0.28, 0.13)
    scene:MatSetNormalMap(mWood, "wood")

    local pillars = {
        { -d, -d, mGlass }, {  d, -d, mAlu }, {  d,  d, mEmis }, { -d,  d, mWood },
    }
    for _, p in ipairs(pillars) do
        local cx, cz, mat = p[1], p[2], p[3]
        scene:AddBox(cx - pr, 0, cz - pr, cx + pr, ph, cz + pr, mPil)
        scene:AddSphere(cx, ph + sr, cz, sr, mat)
    end

    -- lights: a soft white ceiling fill + a strong yellow light co-located with the emissive
    -- sphere so the room is lit (and tinted) by it quickly.
    scene:AddLight(0, H - 0.4, 0, 1.0, 0.98, 0.92, 55.0)
    scene:AddLight(d, ph + sr, d, 1.0, 0.84, 0.34, 70.0)
    scene:SetSky(0.10, 0.11, 0.14, 0.05, 0.05, 0.06, 1.0)   -- dim studio ambient

    cam = { x = 0, y = 2.4, z = W - 1.6, yaw = 180, pitch = -6 }
end

-- 2) Material spheres: a row over a checker floor — diffuse, mirror metal, rough metal,
--    glass, emissive — to show the material range.
local function buildMaterials()
    scene:ClearRaytraceModel(); clearObjs()
    sceneName = "Material spheres"

    quadTex({-12,0,-12},{12,0,-12},{12,0,12},{-12,0,12}, CHECKER, 12, 12)   -- textured floor
    quadFlat({-12,0,-7},{12,0,-7},{12,9,-7},{-12,9,-7}, diffuse(0.55, 0.57, 0.62))  -- back wall

    local r = 0.95
    scene:AddSphere(-4.0, r, 0, r, diffuse(0.85, 0.20, 0.20))
    scene:AddSphere(-2.0, r, 0, r, metal(0.95, 0.95, 0.97, 0.0))
    scene:AddSphere( 0.0, r, 0, r, metal(0.95, 0.78, 0.40, 0.28))
    scene:AddSphere( 2.0, r, 0, r, glass(1.5))
    scene:AddSphere( 4.0, r, 0, r, emissive(0.4, 0.7, 1.0, 6.0))

    scene:AddLight(-3, 7, 5, 1, 1, 1, 70.0)
    scene:AddLight( 4, 6, 4, 0.4, 0.7, 1.0, 30.0)
    scene:SetSky(0.45, 0.60, 0.95, 0.75, 0.80, 0.90, 1.0)   -- daylight

    cam = { x = 0, y = 2.0, z = 7.0, yaw = 180, pitch = -8 }
end

-- 3) Implicit / SDF showcase: analytic torus + ray-marched SDF presets on a plane.
local function buildImplicit()
    scene:ClearRaytraceModel(); clearObjs()
    sceneName = "Implicit / SDF surfaces"

    scene:AddPlane(0, 0, 0, 0, 1, 0, diffuse(0.55, 0.55, 0.58))
    scene:AddTorus(-3.2, 1.1, 0, 0.85, 0.32, 1, metal(0.95, 0.80, 0.42, 0.06))   -- gold torus
    scene:AddSphere(-1.0, 1.1, 0, 0.95, glass(1.5))
    scene:AddSDF("roundbox", 1.1, 1.0, 0, 0.75, 0.75, 0.75, metal(0.9, 0.92, 0.95, 0.12))
    scene:AddSDF("capsule",  2.9, 1.4, 0, 0.8, 0.35, 0, diffuse(0.85, 0.45, 0.20))
    scene:AddSDF("gyroid",   4.8, 1.3, 0, 0.85, 0, 0, diffuse(0.30, 0.78, 0.42))

    scene:AddLight(0, 6, 3, 1, 1, 0.96, 90.0)
    scene:AddLight(-4, 4, 4, 1, 0.9, 0.7, 30.0)
    scene:SetSky(0.45, 0.60, 0.95, 0.78, 0.82, 0.90, 1.0)

    cam = { x = 0.8, y = 2.3, z = 6.5, yaw = 180, pitch = -12 }
end

-- 4) Cornell box: walls / boxes built from quads + analytic boxes, lit only by an emissive
--    ceiling panel — proves the path tracer renders the rasterizer's geometry with true GI
--    (colour bleeding from the red/green walls onto the white boxes).
local function buildCornell()
    scene:ClearRaytraceModel(); clearObjs()
    sceneName = "Cornell box (GI)"
    local W, H = 3.0, 6.0

    local white = diffuse(0.73, 0.73, 0.73)
    local red   = diffuse(0.65, 0.10, 0.10)
    local green = diffuse(0.12, 0.55, 0.18)
    quadFlat({-W,0,-W},{ W,0,-W},{ W,0, W},{-W,0, W}, white)            -- floor
    quadFlat({-W,H,-W},{ W,H,-W},{ W,H, W},{-W,H, W}, white)            -- ceiling
    quadFlat({-W,0,-W},{ W,0,-W},{ W,H,-W},{-W,H,-W}, white)            -- back
    quadFlat({-W,0,-W},{-W,0, W},{-W,H, W},{-W,H,-W}, red)              -- left
    quadFlat({ W,0,-W},{ W,0, W},{ W,H, W},{ W,H,-W}, green)            -- right

    -- emissive ceiling panel (the only light source)
    local mLight = emissive(1.0, 0.95, 0.85, 14.0)
    local s = 1.1
    quadFlat({-s,H-0.02,-s},{ s,H-0.02,-s},{ s,H-0.02, s},{-s,H-0.02, s}, mLight)
    scene:AddLight(0, H - 0.3, 0, 1.0, 0.95, 0.85, 36.0)   -- co-located, for faster convergence

    scene:AddBox(-1.7, 0, -1.2, -0.3, 3.4, 0.2, white)     -- tall box
    scene:AddBox( 0.3, 0, -0.4,  1.7, 1.7, 1.0, white)     -- short box
    scene:SetSky(0, 0, 0, 0, 0, 0, 0.0)                    -- closed box: no sky

    cam = { x = 0, y = 3.0, z = 8.5, yaw = 180, pitch = 0 }
end

-- 5) Voxel blocks: four cubes inspired by the voxel stage — a reflective tin block (tin
--    texture, metal), a glass block, a procedurally-textured grass block, and a wood block
--    with a procedural wood normal map.
local function buildBlocks()
    scene:ClearRaytraceModel(); clearObjs()
    sceneName = "Voxel blocks"

    scene:AddPlane(0, 0, 0, 0, 1, 0, diffuse(0.62, 0.63, 0.66))

    local s, gap = 1.4, 0.7
    local step = s + gap
    local x0 = -1.5 * step                                   -- leftmost block's min-x

    -- grass block (procedural top/side/dirt textures, diffuse)
    texCube(x0, 0, -s / 2, s, GRASS_TOP, GRASS_SIDE, DIRT)

    -- glass block (analytic box, refraction)
    local gx = x0 + step
    scene:AddBox(gx, 0, -s / 2, gx + s, s, s / 2, glass(1.5))

    -- tin block: tin albedo texture on a shiny metal material (reflects its neighbours + sky)
    local mTin = scene:NewMaterial()
    scene:MatSetType(mTin, "metal"); scene:MatSetAlbedo(mTin, 0.80, 0.81, 0.84)
    scene:MatSetRoughness(mTin, 0.04); scene:MatSetTexture(mTin, TIN)
    local tx = x0 + step * 2
    texCube(tx, 0, -s / 2, s, TIN, TIN, TIN, mTin)

    -- wood block: diffuse wood with the procedural wood normal map (analytic box)
    local mWood = scene:NewMaterial()
    scene:MatSetType(mWood, "diffuse"); scene:MatSetAlbedo(mWood, 0.46, 0.28, 0.13)
    scene:MatSetNormalMap(mWood, "wood")
    local wx = x0 + step * 3
    scene:AddBox(wx, 0, -s / 2, wx + s, s, s / 2, mWood)

    scene:AddLight(-2, 6, 5, 1.0, 0.98, 0.95, 80.0)
    scene:AddLight( 4, 5, 3, 0.9, 0.95, 1.0, 35.0)
    scene:SetSky(0.45, 0.62, 0.98, 0.78, 0.83, 0.92, 1.0)   -- daylight (so the tin block reflects something)

    cam = { x = 0.7, y = 1.7, z = 5.4, yaw = 180, pitch = -10 }
end

-- 6) Coloured lights: red / green / blue / warm-white point lights (each shown by a small
--    emissive bulb) cast and mix coloured illumination over a white floor and white objects.
local function buildLights()
    scene:ClearRaytraceModel(); clearObjs()
    sceneName = "Coloured lights"

    scene:AddPlane(0, 0, 0, 0, 1, 0, diffuse(0.85, 0.85, 0.85))
    quadFlat({-8,0,-5},{8,0,-5},{8,8,-5},{-8,8,-5}, diffuse(0.85, 0.85, 0.85))   -- back wall

    -- white objects to catch the coloured light
    scene:AddSphere(-1.6, 1.0, 0, 1.0, diffuse(0.9, 0.9, 0.9))
    scene:AddSphere( 1.6, 1.0, 0, 1.0, diffuse(0.9, 0.9, 0.9))
    scene:AddBox(-0.7, 0, -0.7, 0.7, 1.4, 0.7, diffuse(0.9, 0.9, 0.9))

    local lamps = {
        { -4.5, 4.0,  2.5, 1.0, 0.15, 0.15 },   -- red
        {  4.5, 4.0,  2.5, 0.15, 1.0, 0.20 },   -- green
        {  0.0, 4.2, -3.0, 0.20, 0.35, 1.0 },   -- blue
        {  0.0, 6.0,  3.5, 1.0, 0.85, 0.55 },   -- warm white
    }
    for _, L in ipairs(lamps) do
        local x, y, z, r, g, b = L[1], L[2], L[3], L[4], L[5], L[6]
        scene:AddSphere(x, y, z, 0.18, emissive(r, g, b, 14.0))   -- visible bulb
        scene:AddLight(x, y, z, r, g, b, 90.0)
    end
    scene:SetSky(0.02, 0.02, 0.03, 0.01, 0.01, 0.02, 1.0)   -- near-black: the lamps do the lighting

    cam = { x = 0, y = 2.2, z = 6.5, yaw = 180, pitch = -10 }
end

-- 7) Emissive torus: a sky-blue light-emitting torus turned to face the camera (ring in the
--    view plane) with a refractive diamond floating in its centre.
local function buildTorus()
    scene:ClearRaytraceModel(); clearObjs()
    sceneName = "Emissive torus + diamond"

    scene:AddPlane(0, 0, 0, 0, 1, 0, diffuse(0.30, 0.31, 0.34))
    local cy = 2.1
    scene:AddTorus(0, cy, 0, 1.35, 0.34, 2, emissive(0.35, 0.65, 1.0, 7.0))   -- axis Z → faces camera
    scene:AddSDF("gem", 0, cy, 0, 1.25, 1.25, 1.25, glass(2.417))            -- brilliant-cut diamond (IOR 2.417)
    scene:AddLight(0, cy, 0.6, 0.35, 0.65, 1.0, 55.0)                         -- co-located, for convergence
    scene:AddLight(0, 5, 3, 0.8, 0.85, 1.0, 25.0)                            -- soft top fill
    scene:SetSky(0.05, 0.07, 0.12, 0.02, 0.03, 0.05, 1.0)

    cam = { x = 0, y = 2.1, z = 5.2, yaw = 180, pitch = 0 }
end

-- 8) Normal maps: spheres with the procedural wood / perlin / waves presets, plus a flat panel
--    using a tangent-space normal-map *texture* (bricks).
local function buildNormalMaps()
    scene:ClearRaytraceModel(); clearObjs()
    sceneName = "Normal maps"

    scene:AddPlane(0, 0, 0, 0, 1, 0, diffuse(0.55, 0.55, 0.58))

    local function bumped(r, g, b, preset)
        local m = diffuse(r, g, b); scene:MatSetNormalMap(m, preset); return m
    end
    scene:AddSphere(-3.4, 1.0, 0, 1.0, bumped(0.50, 0.30, 0.14, "wood"))
    scene:AddSphere(-1.1, 1.0, 0, 1.0, bumped(0.55, 0.55, 0.60, "perlin"))
    scene:AddSphere( 1.2, 1.0, 0, 1.0, bumped(0.30, 0.45, 0.75, "waves"))

    -- brick panel: albedo texture + tangent-space normal-map texture
    local mBrick = scene:NewMaterial()
    scene:MatSetType(mBrick, "diffuse"); scene:MatSetTexture(mBrick, BRICK_ALB)
    scene:MatSetNormalMapTexture(mBrick, BRICK_NRM)
    quadTex({2.6,0.1,0},{4.8,0.1,0},{4.8,2.3,0},{2.6,2.3,0}, BRICK_ALB, 1, 1, mBrick)

    scene:AddLight(-2, 5, 4, 1, 1, 0.98, 75.0)
    scene:AddLight( 3, 3, 3, 1, 0.95, 0.9, 35.0)
    scene:SetSky(0.45, 0.60, 0.95, 0.78, 0.82, 0.90, 1.0)

    cam = { x = 0.3, y = 1.8, z = 6.0, yaw = 180, pitch = -8 }
end

local SCENES = { buildMuseum, buildMaterials, buildImplicit, buildCornell,
                 buildBlocks, buildLights, buildTorus, buildNormalMaps }

local function switchScene(n)
    SCENES[n]()
    applyCamera()
end

-- ════════════════════════════════════════════════════════════════════════════════
-- Lifecycle
-- ════════════════════════════════════════════════════════════════════════════════

local function clampb(v) if v < 0 then return 0 elseif v > 255 then return 255 else return floor(v) end end
local function packrgb(r, g, b) return clampb(r) * 65536 + clampb(g) * 256 + clampb(b) end

-- Build every procedural texture the scenes use (called once at load).
local function buildTextures()
    -- checker floor (material scene)
    do
        local t, n = {}, 0
        for y = 0, 63 do for x = 0, 63 do
            n = n + 1; t[n] = (((x // 8) + (y // 8)) % 2 == 0) and 0x9aa0a8 or 0x33373d
        end end
        scene:RegisterTexture(CHECKER, t, 64, 64)
    end

    -- grass top: noisy green with occasional darker blades
    do
        local t, n = {}, 0
        for _ = 0, 15 do for _ = 0, 15 do
            local v = rnd()
            local r, g, b = 58 + v * 30, 142 + v * 48, 46 + v * 24
            if rnd() < 0.12 then r = r - 16; g = g - 40; b = b - 8 end
            n = n + 1; t[n] = packrgb(r, g, b)
        end end
        scene:RegisterTexture(GRASS_TOP, t, 16, 16)
    end

    -- grass side: a jagged green fringe over a dirt body
    do
        local fr = {}
        for x = 0, 15 do fr[x] = 4 + floor(rnd() * 3) end          -- 4..6 px of grass per column
        local t, n = {}, 0
        for y = 0, 15 do for x = 0, 15 do
            local r, g, b
            if y < fr[x] then
                local v = rnd(); r, g, b = 56 + v * 26, 140 + v * 46, 44 + v * 20
            else
                local v = rnd(); r, g, b = 120 + v * 30, 86 + v * 22, 56 + v * 18
                if rnd() < 0.1 then r = r - 24; g = g - 16; b = b - 12 end
            end
            n = n + 1; t[n] = packrgb(r, g, b)
        end end
        scene:RegisterTexture(GRASS_SIDE, t, 16, 16)
    end

    -- dirt
    do
        local t, n = {}, 0
        for _ = 0, 15 do for _ = 0, 15 do
            local v = rnd(); local r, g, b = 122 + v * 30, 88 + v * 22, 57 + v * 18
            if rnd() < 0.12 then r = r - 26; g = g - 18; b = b - 12 end
            n = n + 1; t[n] = packrgb(r, g, b)
        end end
        scene:RegisterTexture(DIRT, t, 16, 16)
    end

    -- tin: silvery base with a raised bevel (lighter top-left, darker border)
    do
        local t, n = {}, 0
        for y = 0, 15 do for x = 0, 15 do
            local shade = (((15 - x) + (15 - y)) / 30 - 0.5) * 38
            if x == 0 or x == 15 or y == 0 or y == 15 then shade = shade - 45 end
            local v = rnd() * 8 - 4
            n = n + 1; t[n] = packrgb(196 + shade + v, 198 + shade + v, 206 + shade + v)
        end end
        scene:RegisterTexture(TIN, t, 16, 16)
    end

    -- brick albedo + matching tangent-space normal map (mortar grooves bevelled)
    local function heightAt(x, y)
        local row = floor(y / 8)
        local offx = (row % 2 == 1) and 16 or 0
        local mx, my = (x + offx) % 16, y % 8        -- mortar occupies indices 0,1 of each
        local function edge(td) if td <= 0 then return 0 elseif td >= 2.5 then return 1 else return td / 2.5 end end
        local hx, hy = edge(mx - 1.5), edge(my - 1.5)
        return hx < hy and hx or hy
    end
    do
        local alb, nrm, n = {}, {}, 0
        for y = 0, 31 do for x = 0, 31 do
            n = n + 1
            local row = floor(y / 8)
            local offx = (row % 2 == 1) and 16 or 0
            local mortar = (y % 8 < 2) or ((x + offx) % 16 < 2)
            if mortar then
                local v = rnd() * 14; alb[n] = packrgb(176 + v, 171 + v, 160 + v)
            else
                local v = rnd() * 22; alb[n] = packrgb(150 + v, 52 + v * 0.5, 46 + v * 0.4)
            end
            -- normal from the height-field gradient
            local hL, hR = heightAt(x - 1, y), heightAt(x + 1, y)
            local hD, hU = heightAt(x, y - 1), heightAt(x, y + 1)
            local nx, ny, nz = (hL - hR) * 2.0, (hD - hU) * 2.0, 1.0
            local il = 1.0 / math.sqrt(nx * nx + ny * ny + nz * nz)
            nrm[n] = packrgb((nx * il * 0.5 + 0.5) * 255, (ny * il * 0.5 + 0.5) * 255, (nz * il * 0.5 + 0.5) * 255)
        end end
        scene:RegisterTexture(BRICK_ALB, alb, 32, 32)
        scene:RegisterTexture(BRICK_NRM, nrm, 32, 32)
    end
end

function onStart()
    scene = SCENE3D:CreateScene(RW, RH)
    fontMid   = TEXT:Create(26)
    fontSmall = TEXT:Create(18)

    scene:SetCameraFov(FOV)
    scene:SetCameraNear(NEAR)
    scene:SetThreads(THREADS)
    scene:SetMode("raytrace")
    buildTextures()
    switchScene(1)
end

function activate()
    lastTs = 0
    -- Mouse stays unlocked: looking around is bound to holding Left-click (rotating resets the
    -- progressive accumulation, so we don't want stray cursor motion to keep re-noising it).
    INPUT:SetMouseLocked(false)
end
function deactivate() INPUT:SetMouseLocked(false) end
function afterSongEnum() end
function onDestroy() end

local function kd(k) return INPUT:KeyboardPressing(k) end
local function kp(k) return INPUT:KeyboardPressed(k) end

function update(ts)
    local dt = (ts - lastTs) / 1000.0
    lastTs = ts
    if dt < 0 then dt = 0 elseif dt > 0.1 then dt = 0.1 end

    if kp("Escape") then return Exit("stage", "_title") end
    for i = 1, 8 do if kp("D" .. i) then switchScene(i) end end

    local moved = false

    -- mouse look — only while Left-click is held. We read the delta EVERY frame (even when not
    -- looking) so its baseline stays current; otherwise the motion accumulated while the button
    -- was up would dump as one big jump on the next click.
    local dmx, dmy = INPUT:GetMouseDelta()
    if INPUT:MousePressing("Left") and (dmx ~= 0 or dmy ~= 0) then
        cam.yaw = cam.yaw + dmx * MOUSE_SENS
        cam.pitch = cam.pitch - dmy * MOUSE_SENS
        if cam.pitch > 89 then cam.pitch = 89 elseif cam.pitch < -89 then cam.pitch = -89 end
        moved = true
    end

    -- free-fly movement along the look basis (matches scene:SetCameraAngles)
    local yr, pr = rad(cam.yaw), rad(cam.pitch)
    local Fx, Fy, Fz = sin(yr) * cos(pr), sin(pr), cos(yr) * cos(pr)
    local Rx, Rz = cos(yr), -sin(yr)
    local mx, my, mz = 0, 0, 0
    if kd("W") then mx = mx + Fx; my = my + Fy; mz = mz + Fz end
    if kd("S") then mx = mx - Fx; my = my - Fy; mz = mz - Fz end
    if kd("D") then mx = mx + Rx; mz = mz + Rz end
    if kd("A") then mx = mx - Rx; mz = mz - Rz end
    if kd("Space") then my = my + 1 end
    if kd("LeftControl") then my = my - 1 end
    if mx ~= 0 or my ~= 0 or mz ~= 0 then
        local sp = MOVE_SPEED * (kd("LeftShift") and FAST_MULT or 1) * dt
        cam.x = cam.x + mx * sp; cam.y = cam.y + my * sp; cam.z = cam.z + mz * sp
        moved = true
    end

    -- Only push the camera (which resets accumulation) when it actually changed, so the image
    -- can keep converging while you hold still.
    if moved then applyCamera() end

    scene:Render()
    scene:Upload()
    return nil
end

function draw()
    scene:SetColor(1, 1, 1)
    scene:SetOpacity(1.0)
    scene:SetScale(SCREEN_W / RW, SCREEN_H / RH)
    scene:Draw(0, 0)

    txt(fontMid, "Raytracer " .. sceneName, 255, 255, 255):Draw(24, 16)
    txt(fontSmall, "samples: " .. scene:GetSampleCount() .. "  (hold still to converge)", 220, 230, 245):Draw(24, 52)
    txt(fontSmall, "WASD move   Space/LCtrl up·down   Shift faster   hold L-click + drag to look   1-8 scenes   Esc quit",
        220, 220, 225):Draw(24, SCREEN_H - 40)
end
