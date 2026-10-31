local NavInput = require("NavInput")
local TowerArt = require("TowerArt")
local EM = require("EventMode")
local Shortcuts = require("Shortcuts")
local OWM = require("World.OWM3d")
local Sky3D = require("World.OWM3d.sky")
local Terrain = require("World.OWM3d.terrain")
-- positions, sizes and colours: Config/layout.json; timings: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local ROAD, TOWERS, BLOCKS, MONO, PAPER = Layout.road, Layout.towers, Layout.blocks, Layout.monoliths, Layout.paper
local INFO, TAGS, CONF, CAMERA, TERR = Layout.info, Layout.tags, Layout.confirm, Layout.camera, Layout.terrain
local ENTER, SIGNS = Animation.enter, Animation.signs
local VISIBLE = TOWERS.visible_each_side
local MAX_SCROLL = 3              -- how far (in slots) the towers may lag behind the cursor while it moves fast
local SPAN = VISIBLE + MAX_SCROLL -- slots kept on each side: the outer ones only while they slide in or out
local A, B = ROAD.half_width, ROAD.half_depth
local CZ = ROAD.z0 + ROAD.half_depth
local OX, OZ = TERR.origin[1], TERR.origin[2]   -- the camera's foot on the terrain (the terrain starts at 0, 0)
local Y_ROAD, Y_LINE, Y_PLAZA, Y_RIM, Y_RING = 0.02, 0.03, 0.04, 0.045, 0.05
local GROUND_SHADE = Layout.light.ground_shade

local songlist = nil
local art = nil
local world = nil           -- the OWM3d world: its scene, camera, sun and sky (as the pagoda draws it)
local scene = nil
local clock = 0

-- "modes" (the two monoliths), "enter" and "leave" (the way between them), "select" (the towers)
local state = "modes"
local T = 0                 -- time along the enter timeline
local focus = 1             -- the mode screen's choice: 1 and 2 the monoliths, 3 its back box
local refused = -100        -- clock when Coming Soon refused
local landShake = -100      -- clock of the last tower landing
local scroll, scrollV = 0, 0 -- the towers' lag behind their plazas, in slots, and its speed (each move adds one)
local backFromPlay = false  -- coming back from a tower play opens on the towers, not the monoliths
local depth = 0             -- folders opened under the list's root
local onBack = false        -- the cursor on the root's way back to the mode select (after the random box)

local tex, obj, snd, act, fonts, col = {}, {}, {}, {}, {}, {}
local models = {}
local slots = {}            -- per offset -SPAN..SPAN: what stands there
local chosen = nil          -- the chosen tower's info, nil for anything but a song
local confirm = nil
local chara = nil
local grey = nil
local modicons = nil         -- the mod icons under the nameplate (the modicons ROActivity)
local sc = nil              -- the song select's shortcuts: help panel, auto, song speed

local arcPhi, arcLen, perimeter, step = {}, {}, 0, ROAD.slot

local function skinString(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

local function clamp01(v) return math.max(0, math.min(1, v)) end
local function smooth(v) v = clamp01(v); return v * v * (3 - 2 * v) end
local function span(t, range) return clamp01((t - range[1]) / math.max(1e-6, range[2] - range[1])) end
local function lerp(a, b, k) return a + (b - a) * k end

-- ── text: no outline, centred on the line's middle (the engine pads a glyph box under and around its ink) ──────
local function pad(font) return font.BoxHeight - math.ceil(font.LineHeight) end

local function text(font, s, x, y, anchor, color, scale, maxW, opacity)
    if s == nil or s == "" then return end
    scale = scale or 1
    local p = pad(font) * scale
    if anchor == "left" then x = x - p elseif anchor == "right" then x = x + p end
    font:Draw(s, x, y + (math.floor(pad(font) / 2) - 1) * scale, color, col.clear, opacity or 1, scale, maxW and (maxW * scale + 2 * p) or 0, anchor)
end

-- ── the road: an ellipse long front to back, its front point straight ahead ────────────────────────────────────
local function buildArc()
    local n = 4096
    local px, pz = 0, ROAD.z0
    arcPhi[1], arcLen[1], perimeter = 0, 0, 0
    for i = 1, n do
        local phi = 2 * math.pi * i / n
        local x, z = A * math.sin(phi), CZ - B * math.cos(phi)
        perimeter = perimeter + math.sqrt((x - px) ^ 2 + (z - pz) ^ 2)
        arcPhi[i + 1], arcLen[i + 1] = phi, perimeter
        px, pz = x, z
    end
    step = perimeter / math.max(1, math.floor(perimeter / ROAD.slot + 0.5))
end

-- the road's middle s metres from the front point (negative: to the left) on the terrain, and its outward normal
local function roadAt(s)
    s = s % perimeter
    local lo, hi = 1, #arcLen
    while hi - lo > 1 do
        local mid = math.floor((lo + hi) / 2)
        if arcLen[mid] <= s then lo = mid else hi = mid end
    end
    local k = (s - arcLen[lo]) / math.max(1e-9, arcLen[hi] - arcLen[lo])
    local phi = arcPhi[lo] + (arcPhi[hi] - arcPhi[lo]) * k
    local tx, tz = A * math.cos(phi), B * math.sin(phi)
    local len = math.sqrt(tx * tx + tz * tz)
    return OX + A * math.sin(phi), OZ + CZ - B * math.cos(phi), tz / len, -tx / len
end

local function slotAt(pos) return roadAt(pos * step) end

-- ── the 3D field ──────────────────────────────────────────────────────────────────────────────────────────────
local TEX_IDS = { grass = 1, road = 2, pave = 3, rim = 4, rock = 6, snow = 7 }
local SPRITE_IDS = { disc = 5, rainbow = 6, back = 7, random = 8, folder = 9 }
local nextSprite = 32
local pieceSprites = {}

local function registerTexture(id, path, filter)
    local t = TEXTURE:CreateTexture(path)
    tex[#tex + 1] = t
    scene:RegisterTextureFromImage(id, t)
    scene:SetTextureFilter(id, filter or "mipmap")
end

local function registerSprite(id, path)
    local t = TEXTURE:CreateTexture(path)
    tex[#tex + 1] = t
    scene:RegisterSpriteFromTexture(id, t)
    scene:SetSpriteFilter(id, "linear")
end

-- a tower piece (a TowerArt texture) as a sprite, registered the first time it is used
local function pieceSprite(t)
    local id = pieceSprites[t]
    if id == nil then
        id = nextSprite
        nextSprite = nextSprite + 1
        scene:RegisterSpriteFromTexture(id, t)
        scene:SetSpriteFilter(id, "linear")
        pieceSprites[t] = id
    end
    return id
end

local function newObject(pass, lit)
    local id = scene:NewObject()
    scene:ObjSetPass(id, pass or 0, 255, 255, 255, 255)
    scene:ObjSetLit(id, lit == true)
    return id
end

-- the ground's pieces: lit (they take the sun and the shadows), casting none, toned down under the sun
local function groundObject()
    local id = newObject(0, true)
    scene:ObjSetCastShadow(id, false)
    scene:ObjSetTint(id, GROUND_SHADE, GROUND_SHADE, GROUND_SHADE)
    return id
end

local function place(id, x, y, z, s)
    s = s or 1
    scene:ObjSetTransform16(id, s, 0, 0, x, 0, s, 0, y, 0, 0, s, z, 0, 0, 0, 1)
end

-- a flat quad facing up (the engine's normal is (c1 - c0) x (c3 - c0)): x0..x1 by z0..z1
local function groundQuad(id, x0, z0, x1, z1, y, texId, tile)
    scene:ObjAddQuadTex(id, x0, y, z0, x0, y, z1, x1, y, z1, x1, y, z0, texId, (z1 - z0) / tile, (x1 - x0) / tile, 1)
end

-- the terrain (Maps/field: the ground rising into the mountains inside the road), and plain grass around it
local function buildGround()
    local w = (TERR.samples - 1) * TERR.cell
    local def = { dir = TERR.dir, terrain = { heights = "height.r16", w = TERR.samples, h = TERR.samples, minY = 0, maxY = TERR.max_y,
        cellSize = TERR.cell, splat = "splat.png", layers = { "grass", "rock", "snow", "grass" }, grass = { density = 0 } } }
    world.phys:beginStatic()
    local ok, err = pcall(function() Terrain.build(world, def, function(name) return TEX_IDS[name] or TEX_IDS.grass end) end)
    world.phys:endStatic()
    if not ok and debugLog then debugLog("[tower_select] terrain: " .. tostring(err)) end
    scene:ObjSetCastShadow(world.floorObj, false)
    scene:ObjSetTint(world.floorObj, GROUND_SHADE, GROUND_SHADE, GROUND_SHADE)
    local f = TERR.frame
    obj.ground = groundObject()
    groundQuad(obj.ground, -f, -f, 0, w + f, 0, TEX_IDS.grass, ROAD.grass_tile)
    groundQuad(obj.ground, w, -f, w + f, w + f, 0, TEX_IDS.grass, ROAD.grass_tile)
    groundQuad(obj.ground, 0, -f, w, 0, 0, TEX_IDS.grass, ROAD.grass_tile)
    groundQuad(obj.ground, 0, w, w, w + f, 0, TEX_IDS.grass, ROAD.grass_tile)
end

-- the road laid over `reveal` (0..1) of its length, from the front point both ways; faces up
local function buildRoad(reveal)
    scene:ObjBegin(obj.road)
    scene:ObjBegin(obj.lines)
    if reveal <= 0 then return end
    local half = perimeter / 2 * math.min(1, reveal)
    local n = math.max(1, math.ceil(2 * half / ROAD.segment))
    local hw, lw = ROAD.width / 2, ROAD.line / 2
    local prev
    for i = 0, n do
        local s = -half + 2 * half * i / n
        local x, z, nx, nz = roadAt(s)
        local cur = { s = s, x = x, z = z, nx = nx, nz = nz }
        if prev ~= nil then
            local v0, v1 = prev.s / ROAD.road_tile, s / ROAD.road_tile
            local aix, aiz, aox, aoz = prev.x - prev.nx * hw, prev.z - prev.nz * hw, prev.x + prev.nx * hw, prev.z + prev.nz * hw
            local bix, biz, box, boz = x - nx * hw, z - nz * hw, x + nx * hw, z + nz * hw
            scene:ObjAddTriTex(obj.road, aix, Y_ROAD, aiz, 0, v0, box, Y_ROAD, boz, 1, v1, aox, Y_ROAD, aoz, 1, v0, TEX_IDS.road, 1)
            scene:ObjAddTriTex(obj.road, aix, Y_ROAD, aiz, 0, v0, bix, Y_ROAD, biz, 0, v1, box, Y_ROAD, boz, 1, v1, TEX_IDS.road, 1)
            for _, e in ipairs({ -hw, hw }) do
                scene:ObjAddQuadFlat(obj.lines,
                    prev.x + prev.nx * (e - lw), Y_LINE, prev.z + prev.nz * (e - lw),
                    x + nx * (e - lw), Y_LINE, z + nz * (e - lw),
                    x + nx * (e + lw), Y_LINE, z + nz * (e + lw),
                    prev.x + prev.nx * (e + lw), Y_LINE, prev.z + prev.nz * (e + lw))
            end
        end
        prev = cur
    end
end

-- a plaza around the origin, facing up: the paving, its stone rim, an ink line on both edges of the rim
local function buildPlaza(fill, ring)
    local R, seg, tile = ROAD.plaza_radius, ROAD.plaza_segments, ROAD.pave_tile
    local ri = R - ROAD.rim
    local lw = ROAD.line / 2
    for i = 0, seg - 1 do
        local a0, a1 = 2 * math.pi * i / seg, 2 * math.pi * (i + 1) / seg
        local c0, s0, c1, s1 = math.cos(a0), math.sin(a0), math.cos(a1), math.sin(a1)
        scene:ObjAddTriTex(fill, 0, Y_PLAZA, 0, 0, 0, ri * c1, Y_PLAZA, ri * s1, ri * c1 / tile, ri * s1 / tile,
            ri * c0, Y_PLAZA, ri * s0, ri * c0 / tile, ri * s0 / tile, TEX_IDS.pave, 1)
        scene:ObjAddTriTex(fill, ri * c0, Y_RIM, ri * s0, ri * c0 / tile, ri * s0 / tile, ri * c1, Y_RIM, ri * s1, ri * c1 / tile, ri * s1 / tile,
            R * c1, Y_RIM, R * s1, R * c1 / tile, R * s1 / tile, TEX_IDS.rim, 1)
        scene:ObjAddTriTex(fill, ri * c0, Y_RIM, ri * s0, ri * c0 / tile, ri * s0 / tile, R * c1, Y_RIM, R * s1, R * c1 / tile, R * s1 / tile,
            R * c0, Y_RIM, R * s0, R * c0 / tile, R * s0 / tile, TEX_IDS.rim, 1)
        for _, r in ipairs({ R, ri }) do
            scene:ObjAddQuadFlat(ring, (r - lw) * c0, Y_RING, (r - lw) * s0, (r - lw) * c1, Y_RING, (r - lw) * s1,
                (r + lw) * c1, Y_RING, (r + lw) * s1, (r + lw) * c0, Y_RING, (r + lw) * s0)
        end
    end
end

local function loadModel(path)
    local m = MODEL:Load(path)
    if m ~= nil then m:Register(scene) end
    return m
end

-- the monoliths keep their own proportions: the stone scaled to Layout.monoliths.size, its foot centre on the ground
local monoFit = nil

local function buildMonoliths()
    obj.monoliths = {}
    if models.monolith == nil then return end
    local x0, y0, z0, x1, y1, z1 = models.monolith:GetBounds()
    monoFit = { sx = MONO.size[1] / (x1 - x0), sy = MONO.size[2] / (y1 - y0), sz = MONO.size[3] / (z1 - z0), cx = (x0 + x1) / 2, cy = y0, cz = (z0 + z1) / 2 }
    for i = 1, 2 do
        local id = newObject(0, true)
        models.monolith:Pose(scene, id, -1, 0, 0, 0, 0, 0, 1)
        obj.monoliths[i] = id
    end
end

local function placeMonolith(id, x, y, z)
    local f = monoFit
    scene:ObjSetTransform16(id, f.sx, 0, 0, OX + x - f.sx * f.cx, 0, f.sy, 0, y - f.sy * f.cy, 0, 0, f.sz, OZ + z - f.sz * f.cz, 0, 0, 0, 1)
end

-- the signs: the model (its board on the camera's side) scaled to Layout.blocks.width, its foot centre on the plaza;
-- the board's middle and front (Layout.blocks.board, model units) in placed metres
local signFit = nil

local function fitSign()
    if models.sign == nil then return end
    local x0, y0, z0, x1, y1, z1 = models.sign:GetBounds()
    local s = BLOCKS.width / (x1 - x0)
    local cz = (z0 + z1) / 2
    signFit = { s = s, cx = (x0 + x1) / 2, cy = y0, cz = cz, height = (y1 - y0) * s,
        boardY = (BLOCKS.board[1] - y0) * s, boardZ = (BLOCKS.board[2] - cz) * s }
end

local function placeSign(id, x, y, z)
    local f = signFit
    scene:ObjSetTransform16(id, f.s, 0, 0, x - f.s * f.cx, 0, f.s, 0, y - f.s * f.cy, 0, 0, f.s, z - f.s * f.cz, 0, 0, 0, 1)
end

local function buildScene()
    local L = Layout.light
    world = OWM.World.new{ rw = 1920, rh = 1080, screenW = 1920, screenH = 1080, fov = CAMERA.fov, yaw = 0,
        pitch = CAMERA.modes.pitch, dist = CAMERA.target_dist, lit = true, hour = L.hour }
    scene = world.scene
    scene:SetAntialias(true)
    scene:SetCameraNear(CAMERA.near)
    scene:SetShadowArea(L.shadow_area)

    registerTexture(TEX_IDS.grass, "Textures/Field/grass.png")
    registerTexture(TEX_IDS.road, "Textures/Field/road.png")
    registerTexture(TEX_IDS.pave, "Textures/Field/pave.png")
    registerTexture(TEX_IDS.rim, "Textures/Field/rim.png")
    registerTexture(TEX_IDS.rock, "Textures/Field/rock.png")
    registerTexture(TEX_IDS.snow, "Textures/Field/snow.png")
    registerSprite(SPRITE_IDS.disc, "Textures/icon_disc.png")
    registerSprite(SPRITE_IDS.rainbow, "Textures/icon_rainbow.png")
    registerSprite(SPRITE_IDS.back, "Textures/icon_back.png")
    registerSprite(SPRITE_IDS.random, "Textures/icon_random.png")
    registerSprite(SPRITE_IDS.folder, "Textures/icon_folder.png")

    buildGround()
    local ink = Layout.colors.ink
    obj.road = groundObject()
    obj.lines = groundObject()
    scene:ObjSetPass(obj.lines, 0, ink[1], ink[2], ink[3], 255)
    scene:ObjSetDepthBias(obj.road, -1)
    scene:ObjSetDepthBias(obj.lines, -2)

    models.monolith = loadModel("Models/monolith.glb")
    buildMonoliths()
    models.sign = loadModel("Models/sign.glb")
    fitSign()

    -- per slot: its plaza (it slides with the tower), the tower, or a sign with its badge
    for k = -SPAN, SPAN do
        local s = {}
        s.plaza, s.ring = groundObject(), groundObject()
        scene:ObjSetPass(s.ring, 0, ink[1], ink[2], ink[3], 255)
        scene:ObjSetDepthBias(s.plaza, -3)
        scene:ObjSetDepthBias(s.ring, -4)
        buildPlaza(s.plaza, s.ring)
        s.tower = newObject(1)
        s.block = newObject(0, true)
        if models.sign ~= nil then models.sign:Pose(scene, s.block, -1, 0, 0, 0, 0, 0, 1) end
        s.disc = newObject(1)
        s.icon = newObject(1)
        scene:ObjSetCastShadow(s.disc, false)
        scene:ObjSetCastShadow(s.icon, false)
        obj[k] = s
    end
end

-- ── the slots' contents ───────────────────────────────────────────────────────────────────────────────────────
local MPP = 0                -- metres per tower art pixel

local function buildTower(id, look, floors)
    scene:ObjBegin(id)
    local p = art:preload(look)
    if p == nil or #p.bases == 0 then return 0 end
    local base0 = p.bases[1]
    MPP = TOWERS.width / math.max(1, base0.Width)
    local pitch = p.layout.floor_height * MPP
    for i = 0, floors - 1 do
        local b = p.bases[(math.floor(i / 10) % #p.bases) + 1]
        scene:ObjAddSprite(id, 0, i * pitch, -0.002 * i, b.Width * MPP, b.Height * MPP, pieceSprite(b), 0, 0)
    end
    local top = floors * pitch
    local half = base0.Width * MPP / 2
    if p.top ~= nil then
        scene:ObjAddSprite(id, 0, top, -0.002 * floors, p.top.Width * MPP, p.top.Height * MPP, pieceSprite(p.top), 0, 0)
        top = top + p.top.Height * MPP
        half = math.max(half, p.top.Width * MPP / 2)
    end
    scene:ObjSetBounds(id, -half, 0, -0.1, half, top, 0.1)
    return top
end

-- the badge on a sign's board (the sign itself is posed once per slot); returns the sign's height
local function buildBlock(s, kind, color)
    local f = signFit or { height = 3, boardY = 1.5, boardZ = 0 }
    local h = f.height
    local r = BLOCKS.badge / 2
    local cy, zf = f.boardY, f.boardZ - 0.03
    scene:ObjBegin(s.icon)
    scene:ObjAddSpriteQuad(s.icon, -r, cy - r, zf - 0.01, r, cy - r, zf - 0.01, r, cy + r, zf - 0.01, -r, cy + r, zf - 0.01, SPRITE_IDS[kind], 0)
    scene:ObjSetBounds(s.icon, -r, cy - r, zf - 0.05, r, cy + r, zf + 0.05)
    scene:ObjSetBounds(s.disc, -r, cy - r, zf - 0.05, r, cy + r, zf + 0.05)
    s.discR, s.discY, s.discZ = r * 0.86, cy, zf
    s.rainbow = kind == "random"
    if not s.rainbow then
        scene:ObjBegin(s.disc)
        local q = s.discR
        scene:ObjAddSpriteQuad(s.disc, -q, cy - q, zf, q, cy - q, zf, q, cy + q, zf, -q, cy + q, zf, SPRITE_IDS.disc, 0)
        local c = color or BLOCKS.disc_color
        local cr, cg, cb = c.R or c[1], c.G or c[2], c.B or c[3]
        scene:ObjSetTint(s.disc, cr / 255, cg / 255, cb / 255)
    end
    return h
end

local function turnRainbow(s, deg)
    local q, cy, zf = s.discR, s.discY, s.discZ
    local a = math.rad(deg)
    local function corner(k)
        local ang = a + math.rad(225 + 90 * k)
        return math.cos(ang) * q * math.sqrt(2), cy + math.sin(ang) * q * math.sqrt(2)
    end
    local ax, ay = corner(0); local bx, by = corner(1); local cx, cyy = corner(2); local dx, dy = corner(3)
    scene:ObjBegin(s.disc)
    scene:ObjAddSpriteQuad(s.disc, ax, ay, zf, bx, by, zf, cx, cyy, zf, dx, dy, zf, SPRITE_IDS.rainbow, 0)
    scene:ObjSetTint(s.disc, 1, 1, 1)
end

-- a song's folder colour, lightened toward white as song select lightens its level digits ({ r, g, b }, 0-255)
local function folderColor(node)
    local c = node ~= nil and node.BoxColor or nil
    if c == nil then return Config.plain(Layout.colors.no_folder) end
    local k = Layout.colors.folder_lighten
    local function up(v) return math.floor(v + (255 - v) * k + 0.5) end
    return { up(c.R), up(c.G), up(c.B) }
end

local BADGE_CELLS = { [2] = 0, [3] = 1, [4] = 2, [5] = 3, [6] = 4, [7] = 5, [8] = 6 }

local function towerInfo(node)
    local chart = node:GetChart(5)
    local best = chart ~= nil and chart:GetPlayerBestScore(0) or nil
    local played = best ~= nil and best.HasBeenPlayed
    return {
        status = played and best.ClearStatus or 0,
        highScore = played and best.HighScore or nil,
        bestFloor = played and best.TowerReachedFloor or 0,
        floors = chart ~= nil and (chart.TotalFloorCount or 0) or 0,
        lives = chart ~= nil and (chart.Life or 5) or 5,
        level = chart ~= nil and chart.Level or 0,
        plus = chart ~= nil and chart.IsPlus == true,
        look = chart ~= nil and chart.TowerType or nil,
        bpm = chart ~= nil and chart.BPM or nil,
    }
end

-- the page around the cursor: the song list's nodes, and at the root the way back to the mode select right
-- after the random box (the list wraps from the random box to its first entry there)
local TO_MODES = { toModes = true }

local function atRoot() return depth == 0 end

local function page()
    local out = {}
    if songlist == nil then return out end
    if not atRoot() then
        for k = -SPAN, SPAN do out[k] = songlist:GetSongNodeAtOffset(k) end
        return out
    end
    local function isRandom(n) return n ~= nil and n ~= TO_MODES and n.IsRandom end
    if onBack then
        out[0] = TO_MODES
        local r, l = 1, 0
        for k = 1, SPAN do out[k] = songlist:GetSongNodeAtOffset(r); r = r + 1 end
        for k = -1, -SPAN, -1 do out[k] = songlist:GetSongNodeAtOffset(l); l = l - 1 end
        return out
    end
    out[0] = songlist:GetSongNodeAtOffset(0)
    local r = 1
    for k = 1, SPAN do
        if isRandom(out[k - 1]) then out[k] = TO_MODES
        else out[k] = songlist:GetSongNodeAtOffset(r); r = r + 1 end
    end
    local l = -1
    for k = -1, -SPAN, -1 do
        local n = songlist:GetSongNodeAtOffset(l)
        if isRandom(n) and out[k + 1] ~= TO_MODES then out[k] = TO_MODES
        else out[k] = n; l = l - 1 end
    end
    return out
end

local function selectedNode()
    if songlist == nil or onBack then return nil end
    return songlist:GetSelectedSongNode()
end

local function refresh()
    chosen = nil
    local nodes = page()
    for k = -SPAN, SPAN do
        local s = obj[k]
        local slot = { kind = "none" }
        local node = nodes[k]
        if node == TO_MODES then
            slot = { kind = "back", toModes = true, name = skinString("TOWERSELECT_RETURN", "Return"), sub = skinString("TOWERSELECT_TO_MODES", "Mode select") }
            slot.top = buildBlock(s, "back")
        elseif node ~= nil then
            if node.IsSong then
                local t = towerInfo(node)
                slot = { kind = "tower", name = node.Title, side = node.Side == 1 and "spicy" or "sweet", status = t.status, level = t.level, plus = t.plus,
                         color = folderColor(node) }
                local floors = math.max(1, math.min(TOWERS.floors_max, t.floors > 0 and t.floors or TOWERS.floors_max))
                slot.top = buildTower(s.tower, t.look, floors)
                if k == 0 then
                    chosen = t
                    chosen.node = node
                    chosen.title = node.Title
                    chosen.subtitle = node.Subtitle
                    chosen.charter = node.Maker
                    chosen.side = slot.side
                    chosen.color = slot.color
                end
            elseif node.IsReturn or node.IsRandom or node.IsFolder then
                local kind = node.IsReturn and "back" or node.IsRandom and "random" or "folder"
                slot = { kind = kind }
                if kind == "folder" then
                    slot.name = node.Title
                else
                    slot.name = kind == "random" and skinString("TOWERSELECT_RANDOM", "Random Tower") or skinString("TOWERSELECT_RETURN", "Return")
                    local parent = node.Parent
                    slot.sub = parent ~= nil and parent.Title or "/"
                end
                slot.top = buildBlock(s, kind, kind == "folder" and node.BoxColor or nil)
            end
        end
        if slot.kind ~= "tower" then scene:ObjBegin(s.tower) end
        if slot.kind ~= "back" and slot.kind ~= "random" and slot.kind ~= "folder" then
            scene:ObjBegin(s.disc); scene:ObjBegin(s.icon)
        end
        slots[k] = slot
    end
end

local function preloadTowers()
    if art == nil or songlist == nil then return end
    art:preload(nil)
    local songs = songlist:SearchSongsByPredicate(function(n) return true end)
    if songs == nil then return end
    for i = 0, songs.Count - 1 do
        pcall(function()
            local chart = songs[i]:GetChart(5)
            if chart ~= nil then art:preload(chart.TowerType) end
        end)
    end
end

-- ── the song preview, as in the song select: loaded after a short wait, faded in, looped ───────────────────────
local preview = { node = nil, key = "", wait = -1, vol = 0, target = 0, loaded = false, playing = false, demo = 0, speed = 1, length = nil }
local P = Animation.preview

local function previewFor(node)
    local key = (node ~= nil and node.IsSong) and (node.AudioPath or "") or ""
    if key == preview.key then return end
    preview.node, preview.key, preview.loaded, preview.playing, preview.target, preview.length = node, key, false, false, 0, nil
    preview.wait = key ~= "" and P.throttle_ms / 1000 or -1
end

local function previewUpdate(dt)
    local snd = SHARED:GetSharedSound("presound")
    if preview.wait >= 0 then
        preview.wait = preview.wait - dt
        if preview.wait < 0 and preview.node ~= nil then
            local node, key = preview.node, preview.key
            SHARED:SetSharedString("presound_path", key)
            SHARED:SetSharedPreviewUsingAbsolutePath("presound", key, function(s)
                if preview.key ~= key then return end
                local speed = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
                s:SetSpeed(speed)
                s:SetVolume(0)
                s:Play()
                preview.demo, preview.speed = math.floor(node.DemoStart / speed), speed
                s:SetTimestamp(preview.demo)
                preview.vol, preview.target, preview.loaded, preview.playing = 0, 100, true, false
            end)
        end
    end
    if snd == nil or not snd.Loaded then return end
    if preview.loaded and snd.DurationMs > 0 then preview.length = snd.DurationMs / preview.speed end
    if preview.vol ~= preview.target then
        local ms = preview.target > preview.vol and P.fade_in_ms or P.fade_out_ms
        local stepv = ms > 0 and 100 * dt * 1000 / ms or 100
        preview.vol = preview.vol < preview.target and math.min(preview.target, preview.vol + stepv) or math.max(preview.target, preview.vol - stepv)
        snd:SetVolume(math.floor(preview.vol + 0.5))
    end
    if preview.loaded then
        if snd.IsPlaying then preview.playing = true
        elseif preview.playing then
            preview.playing = false
            snd:Stop(); snd:Play(); snd:SetTimestamp(preview.demo)
        end
    end
end

local function previewStop()
    preview.node, preview.key, preview.wait, preview.target, preview.loaded, preview.length = nil, "", -1, 0, false, nil
end

-- ── sounds ────────────────────────────────────────────────────────────────────────────────────────────────────
local function loadSounds()
    for k, path in pairs(Config.plain(Animation.sounds)) do
        if STORAGE:FileExists(path) then snd[k] = SOUND:CreateSFX(path) end
    end
    if snd.slide ~= nil then snd.slide:SetVolumePercent(Animation.slide_volume) end
    snd.lands = {}
    if STORAGE:FileExists(Animation.sounds.land) then
        for i, sp in ipairs(Animation.land_speeds) do
            local s = SOUND:CreateSFX(Animation.sounds.land)
            s:SetSpeed(sp)
            snd.lands[i] = s
        end
    end
    -- one plank sound per confirm sign so they overlap; the big sign lands lower, each option a bit higher
    snd.planks = {}
    if STORAGE:FileExists(SIGNS.sound) then
        for i = 1, 5 do
            local s = SOUND:CreateSFX(SIGNS.sound)
            s:SetSpeed(i == 1 and 0.8 or 1 + 0.08 * (i - 2))
            snd.planks[i] = s
        end
    end
end

local function play(name) if snd[name] ~= nil then snd[name]:Play() end end
local function shared(name) SHARED:GetSharedSound(name):Play() end

-- ── the enter timeline: what each part is at time t ───────────────────────────────────────────────────────────
-- how far up the monoliths are (0 on the ground, 1 out of sight): pulled up when entering, falling back when leaving
local function monoAt(t)
    local k = span(t, ENTER.monoliths)
    if state == "leave" then return 1 - (1 - k) * (1 - k) end
    return k * k
end
local function cameraAt(t) return smooth(span(t, ENTER.camera)) end
local function roadRevealAt(t) return smooth(span(t, ENTER.road)) end
local function boxAt(t) return smooth(span(t, ENTER.box)) end

local function fallStart(k) return ENTER.fall_start + math.min(math.abs(k), VISIBLE + 1) * ENTER.fall_stagger end

-- how high a slot's tower still is (nil: not dropped yet)
local function liftAt(t, k)
    local u = (t - fallStart(k)) / ENTER.fall_s
    if u <= 0 then return nil end
    if u >= 1 then return 0 end
    return ENTER.fall_height * (1 - u * u)
end

local function enterSounds(prev, t)
    local function crossed(at) return prev < at and t >= at end
    for k = 0, VISIBLE do
        if crossed(fallStart(k)) then play("fall") end
        if crossed(fallStart(k) + ENTER.fall_s) then
            local s = snd.lands[(k % math.max(1, #snd.lands)) + 1]
            if s ~= nil then s:Play() end
            landShake = clock
        end
    end
    if crossed(ENTER.box[1]) then play("box") end
end

-- ── the camera, the sky and the sun ───────────────────────────────────────────────────────────────────────────
local function cameraUpdate()
    local k = cameraAt(T)
    local h = lerp(CAMERA.modes.height, CAMERA.menu.height, k)
    local pitch = lerp(CAMERA.modes.pitch, CAMERA.menu.pitch, k)
    local S = Animation.land_shake
    local u = clock - landShake
    local sy = u >= 0 and S.metres * math.exp(-u * S.decay_per_s) * math.sin(u * S.rad_per_s) or 0
    local cam, d, p = world.cam, CAMERA.target_dist, math.rad(pitch)
    cam.yaw, cam.pitch, cam.fov, cam.dist = 0, pitch, CAMERA.fov, d
    cam:setTarget(OX, h + sy + math.sin(p) * d, OZ + math.cos(p) * d)
end

-- the pagoda's sky (the OWM3d sky shader, or its bands without one); the field fades into the colour the sky
-- has on the horizon; the sun comes from the camera's side, so the fronts the player sees are lit
local function render()
    local L = Layout.light
    world:drawSky()
    local pal = Sky3D.paletteAt(world.hour or L.hour)
    local function c(i) return math.max(0, math.min(255, math.floor(lerp(pal.hor[i], pal.zen[i], L.horizon_mix) * 255 + 0.5))) end
    scene:SetFog(true, c(1), c(2), c(3), L.fog_start, L.fog_end)
    local sd = L.sun_dir
    local sun = pal.sun
    scene:SetSun(sd[1], sd[2], sd[3], sun[1] * L.sun_strength, sun[2] * L.sun_strength * 0.92, sun[3] * L.sun_strength * 0.82)
    scene:SetShadowFocus(OX, 0, OZ + lerp(L.shadow_focus_modes, L.shadow_focus_menu, cameraAt(T)))
    scene:Render()
    scene:Upload()
    world:blit()
end

-- ── per frame: where everything is ────────────────────────────────────────────────────────────────────────────
local lastReveal = -1

local function slotPos(k) return k + scroll end

-- the scroll eases to rest as a critically damped spring (exact step): the further behind, the faster it goes
local function scrollUpdate(dt)
    local w = Animation.scroll_rad_per_s
    local e = math.exp(-w * dt)
    local j = scrollV + w * scroll
    scroll, scrollV = (scroll + j * dt) * e, (scrollV - w * j * dt) * e
    if math.abs(scroll) < 1e-3 and math.abs(scrollV) < 1e-2 then scroll, scrollV = 0, 0 end
end

local function refuseShake()
    local u = clock - refused
    local R = Animation.refuse_shake
    if u >= 0 and u < R.seconds then return R.metres * math.sin(u * R.rad_per_s) * (1 - u / R.seconds) end
    return 0
end

local function sceneUpdate()
    local reveal = roadRevealAt(T)
    if math.abs(reveal - lastReveal) > 1e-4 then
        buildRoad(reveal)
        lastReveal = reveal
    end
    local half = perimeter / 2 * reveal

    -- the monoliths fly up out of sight
    local up = monoAt(T)
    for i, id in ipairs(obj.monoliths) do
        local x = (i == 1 and -MONO.x or MONO.x)
        if i == 2 then x = x + refuseShake() end
        scene:ObjSetVisible(id, up < 0.999)
        placeMonolith(id, x, MONO.lift * up, MONO.z)
        local k = i == focus and 1 or MONO.unfocused_shade
        scene:ObjSetTint(id, k, k, k)
    end

    -- the slots
    for k = -SPAN, SPAN do
        local s, slot = obj[k], slots[k] or { kind = "none" }
        local pos = slotPos(k)
        local alpha = clamp01(VISIBLE + 1 - math.abs(pos))
        local lift = liftAt(T, k)
        local shown = slot.kind ~= "none" and lift ~= nil and alpha > 0
        local x, z = slotAt(pos)
        local y = lift or 0
        -- its plaza: laid once the road reaches it, sliding with it, shrinking away past the last slot
        local pop = clamp01((half - math.abs(pos) * step) / 4 + 0.25)
        local ps = (0.4 + 0.6 * smooth(pop)) * smooth(alpha)
        local plaza = slot.kind ~= "none" and reveal > 0 and pop > 0 and ps > 0.05
        scene:ObjSetVisible(s.plaza, plaza); scene:ObjSetVisible(s.ring, plaza)
        if plaza then place(s.plaza, x, 0, z, ps); place(s.ring, x, 0, z, ps) end
        local isTower, isBlock = slot.kind == "tower", slot.kind == "back" or slot.kind == "random" or slot.kind == "folder"
        scene:ObjSetVisible(s.tower, shown and isTower)
        scene:ObjSetVisible(s.block, shown and isBlock and alpha > 0.5)
        scene:ObjSetVisible(s.disc, shown and isBlock)
        scene:ObjSetVisible(s.icon, shown and isBlock)
        if shown then
            local a = math.floor(255 * alpha)
            local shade = k == 0 and 1 or TOWERS.unchosen_shade
            if isTower then
                place(s.tower, x, y, z)
                scene:ObjSetPass(s.tower, 1, 255, 255, 255, a)
                scene:ObjSetTint(s.tower, shade, shade, shade)
            else
                if signFit ~= nil then placeSign(s.block, x, y, z) end
                place(s.disc, x, y, z); place(s.icon, x, y, z)
                scene:ObjSetPass(s.disc, 1, 255, 255, 255, a)
                scene:ObjSetPass(s.icon, 1, 255, 255, 255, a)
                scene:ObjSetTint(s.block, shade, shade, shade)
                scene:ObjSetTint(s.icon, shade, shade, shade)
                if slot.kind == "random" and s.rainbow then turnRainbow(s, clock * Animation.rainbow_deg_per_s) end
            end
        end
    end
end

-- ── the papers on the monoliths ───────────────────────────────────────────────────────────────────────────────
local function intersect(a, b)
    local x0, y0 = math.max(a[1], b[1]), math.max(a[2], b[2])
    local x1, y1 = math.min(a[1] + a[3], b[1] + b[3]), math.min(a[2] + a[4], b[2] + b[4])
    return { x0, y0, math.max(0, x1 - x0), math.max(0, y1 - y0) }
end

local function setClip(r)
    local x, y = math.floor(r[1]), math.floor(r[2])
    GRAPHICS:SetClip(x, y, math.max(0, math.ceil(r[1] + r[3]) - x), math.max(0, math.ceil(r[2] + r[4]) - y))
end

-- the Standard Tower picture (its sky and tower baked in) and the player's character climbing it, greyed
local function drawPicture(r, shade)
    if grey ~= nil then GRADIENT:SetActive(grey) end
    local pic = tex.picture
    local k = r[3] / pic.Width
    pic:SetScale(k, r[4] / pic.Height)
    pic:SetColor(shade, shade, shade)
    pic:Draw(r[1], r[2])
    if chara ~= nil then
        local c = PAPER.chara
        chara:SetColor(shade, shade, shade)
        chara:DrawAtAnchor(r[1] + c[1] * k, r[2] + c[2] * k, CHARACTER.ANIM_GAME_TOWER_CLIMBING, "bottom", c[3] * k, c[3] * k)
        chara:SetColor(1, 1, 1)
    end
    if grey ~= nil then GRADIENT:ClearActive() end
end

local MODE_TEXTS = {
    { "TOWERSELECT_STANDARD", "Standard Tower", "TOWERSELECT_STANDARD_DESC", "Climb the tower floor by floor and reach the top before your lives run out!" },
    { "TOWERSELECT_SOON", "Coming Soon...", "TOWERSELECT_SOON_DESC", "This game mode will come later, stay tuned!" },
}

local function drawPaper(i)
    local up = monoAt(T)
    if up >= 0.999 then return end
    local Q = MONO.paper
    local mx = OX + (i == 1 and -MONO.x or MONO.x)
    if i == 2 then mx = mx + refuseShake() end
    local mz = OZ + MONO.z
    local dy = MONO.lift * up
    local tlx, tly = scene:WorldToScreen(mx - Q.width / 2, Q.top + dy, mz + Q.z_top)
    local trx, try = scene:WorldToScreen(mx + Q.width / 2, Q.top + dy, mz + Q.z_top)
    local blx, bly = scene:WorldToScreen(mx - Q.width / 2, Q.bottom + dy, mz + Q.z_bottom)
    local brx, bry = scene:WorldToScreen(mx + Q.width / 2, Q.bottom + dy, mz + Q.z_bottom)
    local x0, x1 = (tlx + blx) / 2, (trx + brx) / 2
    local y0, y1 = (tly + try) / 2, (bly + bry) / 2
    local W, H = PAPER.size[1], PAPER.size[2]
    local sx, sy = (x1 - x0) / W, (y1 - y0) / H
    local shade = i == focus and 1 or MONO.unfocused_shade
    local paper = tex.paper
    paper:SetScale(sx, sy)
    paper:SetColor(shade, shade, shade)
    paper:Draw(x0, y0)
    local pic = PAPER.picture
    local r = { x0 + pic[1] * sx, y0 + pic[2] * sy, pic[3] * sx, pic[3] * sy }
    if i == 1 then
        setClip(intersect(r, { 0, 0, 1920, 1080 }))
        drawPicture(r, shade)
        GRAPHICS:ClearClip()
    end
    local t = MODE_TEXTS[i]
    local ink = i == focus and col.paperInk or col.paperInkDim
    local cx = x0 + W / 2 * sx
    text(fonts.paperTitle, skinString(t[1], t[2]), cx, y0 + PAPER.title_y * sy, "center", ink, sx, PAPER.title_w)
    local lines = fonts.paperText:WrapToLines(skinString(t[3], t[4]), PAPER.text_w, 1)
    for n = 0, lines.Length ~= nil and lines.Length - 1 or #lines - 1 do
        local line = lines.Length ~= nil and lines[n] or lines[n + 1]
        text(fonts.paperText, line, cx, y0 + (PAPER.text_y + n * PAPER.text_pitch) * sy, "center", ink, sx, PAPER.text_w)
    end
end

-- the mode screen's back box, at the bottom: a regular box with the back badge and its name
local function drawModesBack(alpha)
    if alpha <= 0 then return end
    local MB = Layout.modes_back
    local picked = focus == 3
    local shade = picked and 1 or MONO.unfocused_shade
    local s = MB.scale * (picked and (1 + Animation.modes_back.pulse * math.sin(clock * Animation.modes_back.pulse_rad_per_s)) or 1)
    local box = tex.tag
    box:SetScale(s, s)
    box:SetColor(shade, shade, shade)
    box:SetOpacity(alpha)
    box:DrawAtAnchor(MB.x, MB.y, "center")
    local w, h = box.Width * s, box.Height * s
    local ix = MB.x - w / 2 + MB.icon_x * s
    local disc, icon = tex.disc, tex.backIcon
    local is = MB.icon * s / icon.Width
    local c = BLOCKS.disc_color
    disc:SetScale(is, is); disc:SetColor(c[1] / 255 * shade, c[2] / 255 * shade, c[3] / 255 * shade); disc:SetOpacity(alpha)
    disc:DrawAtAnchor(ix, MB.y, "center")
    icon:SetScale(is, is); icon:SetColor(shade, shade, shade); icon:SetOpacity(alpha)
    icon:DrawAtAnchor(ix, MB.y, "center")
    text(fonts.tag, skinString("TOWERSELECT_RETURN", "Return"), MB.x + MB.text_dx * s, MB.y, "center", picked and col.text or col.grey, s, MB.text_w, alpha)
    box:SetColor(1, 1, 1)
    box:SetOpacity(1)
end

-- ── the boxes: name tags over the slots, the chosen tower's info ──────────────────────────────────────────────
local function badgeTexture(status)
    local cell = BADGE_CELLS[status]
    if cell ~= nil then return tex.ranks, cell end
    if status == 1 then return tex.assisted, nil end
    return nil
end

-- assisted: the crown's height against a rank cell's (its art fills more of its frame)
local function drawBadge(status, cx, cy, h, opacity, assisted)
    local t, cell = badgeTexture(status)
    if t == nil then return end
    if cell ~= nil then
        local w = math.floor(t.Width / 7)
        local s = h / t.Height
        t:SetScale(s, s)
        t:SetOpacity(opacity or 1)
        t:DrawRectAtAnchor(cx, cy, w * cell, 0, w, t.Height, "center")
    else
        local s = h * (assisted or 1) / t.Height
        t:SetScale(s, s)
        t:SetOpacity(opacity or 1)
        t:DrawAtAnchor(cx, cy, "center")
    end
end

-- a number in the song bars' level digits (their outlined base, the fill tinted over it, a "+" for a plus
-- level), its left edge at x and its middle at cy; returns its width
local function barNumber(n, x, cy, scale, fill, plus, opacity)
    local D = Layout.digits
    local s = tostring(math.floor(n))
    local width = 0
    for i = 1, #s do
        local w = tex.digits[tonumber(s:sub(i, i))].Width
        width = width + (i < #s and w * 0.5 or w)
    end
    width = width * scale
    local x0 = x
    if plus then x0 = x0 + D.plus_shift * scale end
    local cursor = x0
    for i = 1, #s do
        local d = tonumber(s:sub(i, i))
        local base, over = tex.digits[d], tex.digitFills[d]
        local w = base.Width * scale
        for _, g in ipairs({ base, over }) do
            g:SetScale(scale, scale)
            g:SetOpacity(opacity or 1)
            if g == over then g:SetColor(fill[1] / 255, fill[2] / 255, fill[3] / 255) end
            g:DrawAtAnchor(cursor + w / 2, cy, "center")
            g:SetColor(1, 1, 1)
            g:SetOpacity(1)
        end
        if i < #s then cursor = cursor + w * 0.5 end
    end
    if plus then
        local px, py = x0 + width + D.plus[1] * scale, cy + D.plus[2] * scale
        for _, g in ipairs({ tex.plus, tex.plusFill }) do
            g:SetScale(scale, scale)
            g:SetOpacity(opacity or 1)
            if g == tex.plusFill then g:SetColor(fill[1] / 255, fill[2] / 255, fill[3] / 255) end
            g:DrawAtAnchor(px, py, "center")
            g:SetColor(1, 1, 1)
            g:SetOpacity(1)
        end
    end
    return width
end

local function drawTags(alpha)
    if alpha <= 0 or songlist == nil then return end
    local list = {}
    for k = -SPAN, SPAN do
        local slot = slots[k]
        local pos = slotPos(k)
        local a = clamp01(VISIBLE + 0.5 - math.abs(pos)) * alpha
        if slot ~= nil and slot.kind ~= "none" and a > 0 and not (k == 0 and slot.kind == "tower") and slot.top ~= nil then
            local x, z = slotAt(pos)
            local sx, sy, d = scene:WorldToScreen(x, slot.top + 0.4, z)
            if d > 0 then list[#list + 1] = { k = k, slot = slot, x = sx, y = sy, depth = d, a = a } end
        end
    end
    table.sort(list, function(p, q) return p.depth > q.depth end)
    for _, e in ipairs(list) do
        local slot = e.slot
        local scale = math.min(1, ROAD.z0 / e.depth) * ((e.k == 0) and TAGS.chosen_scale or TAGS.scale)
        local t = slot.sub ~= nil and tex.tagSub or tex.tag
        t:SetScale(scale, scale)
        t:SetOpacity(e.a)
        local w, h = t.Width * scale, t.Height * scale
        local x0, y0 = e.x - w / 2, e.y - TAGS.gap * scale - h
        t:Draw(x0, y0)
        if slot.kind == "tower" then
            -- the tower's tag: its top strip in the song's folder colour
            local strip, c = tex.tagStrip, slot.color
            strip:SetScale(scale, scale)
            strip:SetOpacity(e.a)
            strip:SetColor(c[1] / 255, c[2] / 255, c[3] / 255)
            strip:Draw(x0, y0)
        end
        local hasBadge = slot.kind == "tower" and badgeTexture(slot.status) ~= nil
        local nameW = hasBadge and TAGS.name_w_badge or TAGS.name_w
        local tx = hasBadge and x0 + (TAGS.badge_x / 2 + 6) * scale or e.x
        if slot.kind == "tower" then
            -- the chart's level on the left, the name between it and the badge
            local lw = barNumber(slot.level, x0 + TAGS.level_x * scale, y0 + TAGS.level_y * scale, TAGS.level_scale * scale,
                slot.color, slot.plus, e.a) / scale
            local left = TAGS.level_x + lw + TAGS.level_gap
            local right = hasBadge and TAGS.name_right_badge or TAGS.name_right
            nameW, tx = right - left, x0 + (left + right) / 2 * scale
        end
        if slot.sub ~= nil then
            text(fonts.tag, slot.name, tx, y0 + TAGS.sub_text_y[1] * scale, "center", col.text, scale, nameW, e.a)
            text(fonts.tagSub, slot.sub, tx, y0 + TAGS.sub_text_y[2] * scale, "center", col.grey, scale, nameW, e.a)
        else
            text(fonts.tag, slot.name, tx, y0 + TAGS.text_y * scale, "center", col.text, scale, nameW, e.a)
        end
        if hasBadge then drawBadge(slot.status, x0 + TAGS.badge_x * scale, y0 + h / 2, TAGS.badge_h * scale, e.a) end
    end
end

local function drawInfo(alpha)
    local c = chosen
    if c == nil or alpha <= 0 then return end
    local box, tint, pill = tex.infoBox, tex.infoTint, tex.pills[c.side]
    local fill = c.side == "spicy" and tex.fillSpicy or tex.fillSweet
    local x0 = INFO.x - box.Width / 2
    local y0 = INFO.y - (1 - alpha) * (box.Height + 40)
    local I, D = INFO, Layout.digits
    local df = c.color
    -- the box, its header, heart and stairs in the song's folder colour, the Sweet / Spicy pill as it is
    box:Draw(x0, y0)
    tint:SetColor(df[1] / 255, df[2] / 255, df[3] / 255)
    tint:Draw(x0, y0)
    pill:Draw(x0 + I.pill[1], y0 + I.pill[2] - math.floor(pill.Height / 2))
    text(fonts.title, c.title, x0 + box.Width / 2, y0 + I.title_y, "center", col.text, 1, I.title_w)
    text(fonts.subtitle, c.subtitle, x0 + I.subtitle[1], y0 + I.subtitle[2], "center", col.text, 1, I.subtitle_w)
    text(fonts.charter, c.charter, x0 + I.charter[1], y0 + I.charter[2], "right", col.grey, 1, I.charter_w)
    barNumber(c.level, x0 + I.level_x, y0 + I.row1_y, D.scale, df, c.plus)
    barNumber(c.lives, x0 + I.lives_x, y0 + I.row1_y, D.scale, df)
    text(fonts.floorCount, tostring(math.floor(c.floors)), x0 + I.floors_x, y0 + I.floors_y, "left", col.text)
    if preview.length ~= nil and c.node ~= nil and preview.key == (c.node.AudioPath or "") then
        local sec = math.floor(preview.length / 1000 + 0.5)
        text(fonts.length, string.format("%d:%02d", math.floor(sec / 60), sec % 60), x0 + I.floors_x, y0 + I.length_y, "left", col.grey)
    end
    text(fonts.label, skinString("TOWERSELECT_HIGH_SCORE", "High score"), x0 + I.score_label[1], y0 + I.score_label[2], "left", col.grey)
    text(fonts.score, c.highScore ~= nil and tostring(math.floor(c.highScore)) or "--", x0 + I.score[1], y0 + I.score[2], "left", col.text)
    text(fonts.label, skinString("TOWERSELECT_BEST_FLOOR", "Best floor"), x0 + I.floor_label[1], y0 + I.floor_label[2], "left", col.grey)
    local total = "/ " .. tostring(math.floor(c.floors))
    text(fonts.floorTotal, total, x0 + I.floor_right, y0 + I.best_total_y, "right", col.grey)
    local tw = fonts.floorTotal:Measure(total)
    text(fonts.best, tostring(math.floor(c.bestFloor)), x0 + I.floor_right - tw - 8, y0 + I.best_y, "right", col.text)
    -- the best floor bar: the fill, then the 10 / 25 / 50 / 75 % ticks over it
    local bar = I.bar
    local k = c.floors > 0 and clamp01(c.bestFloor / c.floors) or 0
    if k > 0 then
        setClip({ x0 + bar[1], y0 + bar[2], bar[3] * k, bar[4] })
        fill:Draw(x0 + bar[1], y0 + bar[2])
        GRAPHICS:ClearClip()
    end
    local tick = tex.tick
    for _, pct in ipairs({ 10, 25, 50, 75 }) do
        local reached = c.floors > 0 and c.bestFloor * 100 >= c.floors * pct
        local tx = x0 + bar[1] + bar[3] * pct / 100
        tick:SetOpacity(reached and 1 or I.tick_unreached)
        tick:DrawAtAnchor(tx, y0 + bar[2] + bar[4] / 2, "center")
        text(fonts.percent, pct .. "%", tx, y0 + I.percent_y, "center", reached and col.text or col.grey)
    end
    tick:SetOpacity(1)
    drawBadge(c.status, x0 + I.badge[1], y0 + I.badge[2], I.badge[3], 1, I.assisted_scale)
end

-- ── the confirm menu: wooden signs dropped on ropes, hanging under the chosen tower's info ────────────────────
local function confirmItems()
    if EM.on() then return { "back", "settings", "challenge" } end
    return { "back", "settings", "customize", "challenge" }
end

local function openConfirm()
    local items = confirmItems()
    local tw, th = tex.planks.title.Width, tex.planks.title.Height
    local ow, oh = tex.planks.option.Width, tex.planks.option.Height
    local top = CONF.title_y
    local signs = { { w = tw, h = th, cx = CONF.center_x, cy = top, from = top - SIGNS.drop_px, start = 0, phase = SIGNS.title_phase, pose = {} } }
    local total = CONF.option_slots * ow + (CONF.option_slots - 1) * CONF.option_gap
    for i, id in ipairs(items) do
        local slot = CONF.option_slot[id]
        signs[i + 1] = { id = id, w = ow, h = oh, cx = CONF.center_x - total / 2 + (slot - 0.5) * ow + (slot - 1) * CONF.option_gap,
                         cy = top + th / 2 + CONF.rope_length + oh / 2, from = top,
                         start = SIGNS.fall_s + SIGNS.first_option_delay_s + (i - 1) * SIGNS.stagger_s, phase = SIGNS.option_phase_step * i, pose = {} }
    end
    confirm = { items = items, pos = 1, t = 0, closing = nil, signs = signs }
end

-- a sign's vertical offset from its landed place at time t: not started, falling, or bouncing on its ropes
local function signDrop(s, t)
    local u = t - s.start
    if u < 0 then return nil end
    if u < SIGNS.fall_s then
        local k = u / SIGNS.fall_s
        return (s.from - s.cy) * (1 - k * k), false
    end
    u = u - SIGNS.fall_s
    local b = SIGNS.bounce
    return b.amplitude_px * math.sin(u * b.rad_per_s) * math.exp(-u * b.decay_per_s), true
end

-- a landed sign swings from its drop (u seconds ago), the swing dying down into the idle sway
local function signPose(s, t, u)
    local q, ph = s.pose, s.phase
    local settle = math.min(1, u / SIGNS.settle_s)
    local sw, a, x, y = SIGNS.swing, SIGNS.sway.angle, SIGNS.sway.x, SIGNS.sway.y[1]
    local swing = sw.amplitude_deg * math.sin(u * sw.rad_per_s + ph) * math.exp(-u * sw.decay_per_s)
    q.angle = swing + settle * (a[1].amplitude * math.sin(t * a[1].rad_per_s + ph * a[1].phase_scale) + a[2].amplitude * math.sin(t * a[2].rad_per_s + ph * a[2].phase_scale))
    q.x = s.cx + swing * sw.x_px_per_deg + settle * (x[1].amplitude * math.sin(t * x[1].rad_per_s + ph * x[1].phase_scale) + x[2].amplitude * math.sin(t * x[2].rad_per_s + ph * x[2].phase_scale))
    q.y = s.cy + settle * y.amplitude * math.sin(t * y.rad_per_s + ph * y.phase_scale)
    return q
end

local function at(q, dx, dy)
    return q.x + dx * q.c + dy * q.s, q.y - dx * q.s + dy * q.c
end

local function drawRope(x0, y0, x1, y1)
    local dx, dy = x1 - x0, y1 - y0
    local len = math.sqrt(dx * dx + dy * dy)
    if len < 1 then return end
    local rope = tex.planks.rope
    rope:SetScale(1, len / rope.Height)
    rope:SetRotation(math.deg(math.atan(dx, dy)))
    rope:DrawAtAnchor((x0 + x1) / 2, (y0 + y1) / 2, "center")
end

local function drawConfirm()
    local c = confirm
    local t = c.t
    local k = c.closing ~= nil and math.min(1, c.closing / SIGNS.close_s) or 0
    local title = c.signs[1]
    local rise = (title.from - title.cy) * k * k
    local pk = tex.planks
    pk.veil:SetScale(CONF.veil_scale.x, CONF.veil_scale.y)
    pk.veil:SetOpacity(CONF.veil_opacity * math.min(1, t / SIGNS.fall_s) * (1 - k))
    pk.veil:Draw(0, 0)

    -- where every sign is this frame: the options follow the title sign they hang from
    local poses = {}
    for i, s in ipairs(c.signs) do
        local dy, landed = signDrop(s, t)
        if dy ~= nil then
            local q = signPose(s, t, landed and (t - s.start - SIGNS.fall_s) or 0)
            if i > 1 then
                q.x = q.x + (poses[1].x - title.cx)
                q.y = q.y + (poses[1].y - title.cy)
            end
            q.y = q.y + dy + rise
            local rad = math.rad(q.angle)
            q.s, q.c = math.sin(rad), math.cos(rad)
            poses[i] = q
        end
    end
    local tq = poses[1]
    if tq == nil then return end

    -- ropes: the title's from the top of the screen, each option's from its pair of holes along the title's bottom edge
    local hx0, hy = CONF.hole_x, CONF.hole_y
    local thx = title.w / 2 - hx0
    local tlx, tly = at(tq, -thx, -title.h / 2 + hy)
    local trx, tr_y = at(tq, thx, -title.h / 2 + hy)
    drawRope(title.cx - thx, CONF.rope_top_y, tlx, tly)
    drawRope(title.cx + thx, CONF.rope_top_y, trx, tr_y)
    local knot = pk.knot
    knot:SetRotation(0)
    for i = 2, #c.signs do
        local s, q = c.signs[i], poses[i]
        if q ~= nil then
            local hx = s.w / 2 - hx0
            for _, side in ipairs({ -1, 1 }) do
                local ax, ay = at(tq, side * hx + (s.cx - title.cx), title.h / 2 - hy)
                local bx, by = at(q, side * hx, -s.h / 2 + hy)
                drawRope(ax, ay, bx, by)
            end
        end
    end

    -- the option signs, the chosen one framed, then the title sign over them
    for i = 2, #c.signs do
        local s, q = c.signs[i], poses[i]
        if q ~= nil then
            local picked = c.items[c.pos] == s.id
            if picked then
                pk.frame:SetRotation(q.angle)
                local fp = Animation.frame_pulse
                pk.frame:SetOpacity(fp.opacity_base + fp.opacity_range * math.sin(clock * fp.rad_per_s))
                pk.frame:DrawAtAnchor(q.x, q.y, "center")
            end
            local shade = picked and 1 or CONF.unchosen_shade
            pk.option:SetColor(shade, shade, shade)
            pk.option:SetRotation(q.angle)
            pk.option:DrawAtAnchor(q.x, q.y, "center")
            local button = tex.buttons[s.id]
            local fit = math.min(1, (s.w - 2 * CONF.button_pad_x) / button.Width, (s.h - CONF.button_pad_top - CONF.button_pad_bottom) / button.Height)
            local bx, by = at(q, 0, (CONF.button_pad_top - CONF.button_pad_bottom) / 2)
            button:SetScale(fit, fit)
            button:SetColor(shade, shade, shade)
            button:SetRotation(q.angle)
            button:DrawAtAnchor(bx, by, "center")
            local hx = s.w / 2 - hx0
            for _, side in ipairs({ -1, 1 }) do
                local kx, ky = at(q, side * hx, -s.h / 2 + hy)
                knot:DrawAtAnchor(kx, ky, "center")
            end
        end
    end
    pk.title:SetRotation(tq.angle)
    pk.title:DrawAtAnchor(tq.x, tq.y, "center")
    knot:DrawAtAnchor(tlx, tly, "center")
    knot:DrawAtAnchor(trx, tr_y, "center")
    for i = 2, #c.signs do
        local s = c.signs[i]
        if poses[i] ~= nil then
            for _, side in ipairs({ -1, 1 }) do
                local kx, ky = at(tq, side * (s.w / 2 - hx0) + (s.cx - title.cx), title.h / 2 - hy)
                knot:DrawAtAnchor(kx, ky, "center")
            end
        end
    end

    -- the question, turned with its sign
    local f = fonts.confirm
    local x, y = at(tq, 0, math.floor((f.BoxHeight - math.ceil(f.LineHeight)) / 2) - 1)
    f:Draw(skinString("TOWERSELECT_CONFIRM", "Challenge the Tower?"), x, y, col.sign, col.signOutline, 1, 1, title.w - CONF.title_text_inset, "center", 0, tq.angle)
end

-- returns success, entered ("song"/"folder"/nil)
local function handleDecide()
    if songlist == nil then return false end
    local node = songlist:GetSelectedSongNode()
    if node == nil then return false end
    if node.IsFolder then
        local success = songlist:OpenFolder()
        if success then depth = depth + 1 end
        refresh()
        return success, "folder"
    elseif node.IsReturn then
        local success = songlist:CloseFolder()
        if success then depth = math.max(0, depth - 1) end
        refresh()
        return success
    elseif node.IsRandom then
        local pick = songlist:GetRandomNodeInFolder(node, true)
        if pick == nil then return false end
        return pick:Mount(5), "song"
    elseif node.IsSong then
        return node:Mount(5), "song"
    end
    return false
end

-- returns "play" once a tower is mounted
local function updateConfirm()
    local c = confirm
    c.t = c.t + fps.deltaTime
    if c.closing ~= nil then
        c.closing = c.closing + fps.deltaTime
        if c.closing >= SIGNS.close_s then confirm = nil end
        return nil
    end
    local falling = false
    for i, s in ipairs(c.signs) do
        if not s.landed then
            if c.t - s.start >= SIGNS.fall_s then
                s.landed = true
                if snd.planks[i] ~= nil then snd.planks[i]:Play() end
            else
                falling = true
            end
        end
    end

    local nav = NavInput.p[1]
    if nav.cancel() then
        shared("Cancel")
        c.closing = 0
        return nil
    end
    if falling then return nil end
    if nav.right() then
        c.pos = c.pos % #c.items + 1
        shared("Move")
    elseif nav.left() then
        c.pos = (c.pos - 2) % #c.items + 1
        shared("Move")
    elseif nav.decide() then
        local id = c.items[c.pos]
        if id == "back" then
            shared("Cancel")
            c.closing = 0
        elseif id == "settings" then
            if act.mod_select_dialog ~= nil then act.mod_select_dialog:Activate(0) end
            shared("Decide")
        elseif id == "customize" then
            if act.customize_dialog ~= nil then act.customize_dialog:Activate(0) end
            shared("Decide")
        elseif id == "challenge" then
            local success, entered = handleDecide()
            if success and entered == "song" then
                shared("SongDecide")
                return "play"
            end
            shared("Error")
        end
    end
    return nil
end

-- ── moving and leaving ────────────────────────────────────────────────────────────────────────────────────────
local function move(amount)
    if songlist == nil then return end
    if atRoot() then
        if amount > 0 then
            if onBack then onBack = false; songlist:Move(1)
            elseif songlist:GetSelectedSongNode() ~= nil and songlist:GetSelectedSongNode().IsRandom then onBack = true
            else songlist:Move(1) end
        else
            local left = songlist:GetSongNodeAtOffset(-1)
            if onBack then onBack = false
            elseif left ~= nil and left.IsRandom then songlist:Move(-1); onBack = true
            else songlist:Move(-1) end
        end
    else
        songlist:Move(amount)
    end
    scroll = math.max(-MAX_SCROLL, math.min(MAX_SCROLL, scroll + (amount > 0 and 1 or -1)))
    refresh()
    if snd.slide ~= nil then snd.slide:Play() else shared("Move") end
end

local function startEnter()
    state = "enter"
    play("lift")
    previewStop()
end

local function startLeave()
    state = "leave"
    confirm = nil
    if onBack then onBack = false; refresh() end
    previewStop()
    play("fall")
end

local function closeFolder()
    if songlist == nil then return false end
    local success = songlist:CloseFolder()
    if success then depth = math.max(0, depth - 1) end
    refresh()
    return success
end

-- ── the player, bottom left as in the other selects: the nameplate, the character and puchichara standing on it,
-- the mod icons under it ───────────────────────────────────────────────────────────────────────────────────────
local function drawPlayer()
    local L, A = Layout.player, Animation.puchi
    NAMEPLATE:DrawPlayerNameplate(L.x, L.y, 255, 0)
    if chara ~= nil then
        chara:Update(CHARACTER.ANIM_MENU_NORMAL, true)
        chara:DrawAtAnchor(L.x + L.chara_dx, L.y + L.chara_dy, CHARACTER.ANIM_MENU_NORMAL, "bottom", 1, 1, 255)
    end
    local puchi = GetSaveFile(0):GetPuchichara()
    if puchi ~= nil and puchi.tx ~= nil and puchi.tx.Loaded then
        local frameW = math.floor(puchi.tx.Width / A.frames)
        local frame = math.floor(clock / A.frame_cycle_s * A.frames) % A.frames
        local bob = math.sin(2 * math.pi * clock / A.float_period_s) * A.float_px
        puchi.tx:SetScale(1, 1)
        puchi.tx:DrawRectAtAnchor(L.x + L.puchi_dx, L.y + bob, frame * frameW, 0, frameW, puchi.tx.Height, "bottom")
    end
    if modicons ~= nil then modicons:Draw(L.x, L.y + L.modicons_dy, 0, "menu", 255) end
end

-- ── stage callbacks ───────────────────────────────────────────────────────────────────────────────────────────
function draw()
    if world == nil then return end
    sceneUpdate()
    render()

    local modesShown = 1 - monoAt(T)
    if modesShown > 0.001 then
        drawPaper(1)
        drawPaper(2)
        drawModesBack(clamp01(1 - T / ENTER.monoliths[2]))
    end
    if state ~= "modes" and songlist == nil then
        text(fonts.notice, "Songlist is currently enumerating or unavailable.", Layout.notice[1], Layout.notice[2], "center", col.text)
    end
    local a = boxAt(T)
    drawTags(a)
    drawPlayer()
    if confirm ~= nil then drawConfirm() end
    drawInfo(a)
    if sc ~= nil and state == "select" then
        local SC = Layout.shortcuts
        if confirm == nil then sc:drawHelper(SC.helper[1], SC.helper[2], a) end
        -- a tower played at another speed than 1x keeps no record
        sc:drawSpeed(SC.speed[1], SC.speed[2], chosen ~= nil and chosen.bpm or nil, a, skinString("TOWERSELECT_NOT_RECORDED", "Not recorded"))
    end
    for _, d in pairs(act) do
        if d ~= nil and d.IsActive then d:Draw() end
    end
    if sc ~= nil then sc:draw() end
end

local function updateModes(nav)
    if nav.right() then
        focus = focus % 3 + 1
        shared("Move")
    elseif nav.left() then
        focus = (focus - 2) % 3 + 1
        shared("Move")
    elseif nav.down ~= nil and nav.down() and focus ~= 3 then
        focus = 3
        shared("Move")
    elseif nav.up ~= nil and nav.up() and focus == 3 then
        focus = 1
        shared("Move")
    elseif nav.decide() then
        if focus == 1 then
            shared("Decide")
            startEnter()
        elseif focus == 2 then
            refused = clock
            shared("Error")
        else
            shared("Cancel")
            return Exit("title", nil, "tower_gate_back")
        end
    elseif nav.cancel() then
        shared("Cancel")
        return Exit("title", nil, "tower_gate_back")
    end
end

-- the song select's shortcuts: the help panel, auto play, the song speed (the preview follows it)
local function shortcuts()
    if sc:pressed("help") then sc:open(); return true end
    if sc:pressed("auto_p1") then
        CONFIG:SetAutoStatus(0, not CONFIG:GetAutoStatus(0))
        shared("Decide")
    end
    for _, s in ipairs({ { "speed_down", -1 }, { "speed_up", 1 } }) do
        if sc:pressed(s[1]) then
            preview.speed = Shortcuts.stepSpeed(s[2])
            if preview.node ~= nil then preview.demo = math.floor(preview.node.DemoStart / preview.speed) end
        end
    end
    return false
end

local function updateSelect(nav)
    if shortcuts() then return end
    if songlist == nil then
        if nav.cancel() then shared("Cancel"); startLeave() end
        return
    end
    if nav.right() then
        move(1)
    elseif nav.left() then
        move(-1)
    elseif nav.decide() then
        if onBack then
            shared("Cancel")
            startLeave()
            return
        end
        local node = songlist:GetSelectedSongNode()
        if node ~= nil and (node.IsSong or node.IsRandom) then
            openConfirm()
            shared("Decide")
            return
        end
        local success, entered = handleDecide()
        shared(success and (entered and "Decide" or "Cancel") or "Error")
    elseif nav.cancel() then
        shared("Cancel")
        if not closeFolder() then startLeave() end
    end
end

function update()
    local dt = fps.deltaTime
    clock = clock + dt
    if sc ~= nil then sc:tick() end
    if world ~= nil then
        cameraUpdate()
        world:update(dt)
    end
    scrollUpdate(dt)
    if confirm ~= nil or state == "select" then previewFor(selectedNode()) end
    previewUpdate(dt)

    local busy = false
    for _, d in pairs(act) do
        if d ~= nil and d.IsActive then d:Update(); busy = true end
    end
    if busy then return end
    if sc ~= nil and sc:update() then return end

    local nav = NavInput.p[1]
    if state == "enter" then
        local prev = T
        T = math.min(ENTER["end"], T + dt)
        enterSounds(prev, T)
        if nav.decide() then T = ENTER["end"] end
        if T >= ENTER["end"] then state = "select" end
        return
    elseif state == "leave" then
        local prev = T
        T = math.max(0, T - dt * ENTER.leave_speed)
        if prev > ENTER.monoliths[2] and T <= ENTER.monoliths[2] then play("fall") end
        if T <= 0 then
            state = "modes"
            local s = snd.lands[(1 % math.max(1, #snd.lands)) + 1]
            if s ~= nil then s:Play() end
            landShake = clock
        end
        return
    end
    if confirm ~= nil then
        if updateConfirm() == "play" then
            backFromPlay = true
            return Exit("play", nil)
        end
        return
    end
    if state == "modes" then return updateModes(nav) end
    return updateSelect(nav)
end

local function loadTextures()
    local function t(path) local x = TEXTURE:CreateTexture(path); tex[#tex + 1] = x; return x end
    tex.paper = t("Textures/Modes/paper.png")
    tex.picture = t("Textures/Modes/picture.png")
    tex.infoBox = t("Textures/Box/info_box.png")
    tex.infoTint = t("Textures/Box/info_tint.png")
    tex.pills = { sweet = t("Textures/Box/pill_sweet.png"), spicy = t("Textures/Box/pill_spicy.png") }
    tex.fillSweet = t("Textures/Box/bar_fill_sweet.png")
    tex.fillSpicy = t("Textures/Box/bar_fill_spicy.png")
    tex.tick = t("Textures/Box/bar_tick.png")
    tex.tag = t("Textures/Box/tag.png")
    tex.tagSub = t("Textures/Box/tag_sub.png")
    tex.tagStrip = t("Textures/Box/tag_strip.png")
    tex.ranks = t("Textures/Badges/ranks.png")
    tex.assisted = t("Textures/Badges/assisted.png")
    tex.disc = t("Textures/icon_disc.png")
    tex.backIcon = t("Textures/icon_back.png")
    tex.digits, tex.digitFills = {}, {}
    for i = 0, 9 do
        tex.digits[i] = t("Textures/Digits/bar_" .. i .. ".png")
        tex.digitFills[i] = t("Textures/Digits/bar_fill_" .. i .. ".png")
    end
    tex.plus = t("Textures/Digits/bar_plus.png")
    tex.plusFill = t("Textures/Digits/bar_fill_plus.png")
    tex.planks = {
        title = t("Textures/Confirm/plank_title.png"),
        option = t("Textures/Confirm/plank_option.png"),
        rope = t("Textures/Confirm/rope.png"),
        knot = t("Textures/Confirm/knot.png"),
        frame = t("Textures/Confirm/frame.png"),
        veil = t("Textures/Confirm/veil.png"),
    }
    tex.buttons = {}
    for _, id in ipairs({ "back", "settings", "customize", "challenge" }) do
        tex.buttons[id] = t("Textures/Confirm/option_" .. id .. ".png")
    end
end

local function loadFonts()
    local F = Layout.fonts
    for key, size in pairs({ title = F.title, subtitle = F.subtitle, charter = F.charter, label = F.label,
        score = F.score, best = F.best, floorCount = F.floor_count, length = F.length, floorTotal = F.floor_total, percent = F.percent, tag = F.tag, tagSub = F.tag_sub,
        confirm = F.confirm, paperTitle = F.paper_title, paperText = F.paper_text, notice = F.notice }) do
        fonts[key] = TEXT:CreateGlyphCached(size)
    end
    local C = Layout.colors
    col.clear = COLOR:CreateColorFromRGBA(0, 0, 0, 0)
    col.text = Config.color(C.text)
    col.grey = Config.color(C.grey)
    col.paperInk = Config.color(C.paper_ink)
    col.sign = Config.color(CONF.sign_color)
    col.signOutline = Config.color(CONF.sign_outline_color)
    local k = MONO.unfocused_shade
    col.paperInkDim = COLOR:CreateColorFromRGBA(math.floor(C.paper_ink[1] * k), math.floor(C.paper_ink[2] * k), math.floor(C.paper_ink[3] * k), 255)
end

function activate()
    CONFIG.PlayerCount = 1
    if not backFromPlay then CONFIG.SongSpeed = 20 end   -- other modes may have changed it; a tower play keeps it

    clock, T, focus, state, refused, landShake = 0, 0, 1, "modes", -100, -100
    if backFromPlay then state, T, backFromPlay = "select", ENTER["end"], false end
    scroll, scrollV, lastReveal, confirm, onBack = 0, 0, -1, nil, false
    art = TowerArt.load()
    art:preload("Day")
    preloadTowers()
    buildArc()
    loadTextures()
    loadFonts()
    loadSounds()
    buildScene()
    grey = GRADIENT:Create(Config.plain(Layout.colors.grey_ramp), 1)
    chara = CHARACTER:GetPlayerCharacter(0)
    if chara ~= nil then
        -- a still pose: one update picks the first frame (none is drawn before), and none follows
        chara:LoadAnimation(CHARACTER.ANIM_GAME_TOWER_CLIMBING)
        chara:SetAnimationCyclesFromBPM(CHARACTER.ANIM_GAME_TOWER_CLIMBING, 120)
        chara:ResetAnimationCounter(CHARACTER.ANIM_GAME_TOWER_CLIMBING)
        chara:Update(CHARACTER.ANIM_GAME_TOWER_CLIMBING)
        chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)
    end
    act.customize_dialog = ACTIVITY:GetActivity("customize_dialog")
    act.mod_select_dialog = ACTIVITY:GetActivity("mod_select_dialog")
    modicons = ROACTIVITY:GetROActivity("modicons")
    if modicons ~= nil then modicons:Activate() end
    sc = Shortcuts.new{ ids = { "help", "auto_p1", "speed_down", "speed_up" }, nav = NavInput.p[1],
        disabled = function(id) return id == "auto_p1" and EM.on() end }
    refresh()
end

function deactivate()
    if sc ~= nil then sc:dispose(); sc = nil end
    previewStop()
    local ps = SHARED:GetSharedSound("presound")
    if ps ~= nil then ps:Stop() end
    SHARED:SetSharedString("presound_path", "")
    if chara ~= nil then
        pcall(function() chara:DisposeAnimation(CHARACTER.ANIM_GAME_TOWER_CLIMBING) end)
        pcall(function() chara:DisposeAnimation(CHARACTER.ANIM_MENU_NORMAL) end)
    end
    if modicons ~= nil then modicons:Deactivate(); modicons = nil end
    chara = nil
    if grey ~= nil then grey:Dispose(); grey = nil end
    if art ~= nil then art:dispose(); art = nil end
    for _, t in ipairs(tex) do pcall(function() t:Dispose() end) end
    tex = {}
    for _, list in ipairs({ snd.lands or {}, snd.planks or {} }) do
        for _, s in ipairs(list) do s:Dispose() end
    end
    snd.lands, snd.planks = nil, nil
    for _, s in pairs(snd) do s:Dispose() end
    snd = {}
    for _, f in pairs(fonts) do f:Dispose() end
    fonts = {}
    models = {}
    if world ~= nil then world:dispose(); world = nil end
    scene = nil
    obj, slots, pieceSprites, nextSprite = {}, {}, {}, 32
    act = {}
    confirm, chosen = nil, nil
end

function onStart()
end

function afterSongEnum()
    local settings = GenerateSongListSettings()
    settings.RootGenreFolder = "太鼓タワー"
    settings.MandatoryDifficultyList = { 5 }
    settings.FlattenOpenedFolders = false
    settings.AppendMainRandomBox = true
    songlist = RequestSongList(settings)
    depth, onBack = 0, false
    preloadTowers()
    if scene ~= nil then refresh() end
end

function onDestroy()
end
