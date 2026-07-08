---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- assets.lua — textures + palette + the animated character for myroom (OWM3d edition).
--   * Floor/wall TEXTURES: procedural fallbacks are always registered first; when the CC0 files in
--     Textures/Surfaces/ exist they overwrite the same ids (RegisterTextureFromImage + mipmap filter).
--   * A PALETTE of solid-colour textures for the shaded-box furniture builders.
--   * The CHARACTER: the 4-direction animated walk sprites (CharaTemplate), loaded as billboards.

local floor, sin, random = math.floor, math.sin, math.random

local A = {}

A.TEX = { wood = 200, carpet = 201, wall = 202, skirt = 203 }
-- floor "paints" (flooring) and wall paints, by id → texture id. The DEFAULT floor (wood) and wall
-- (plaster) are NOT paints — they are the empty state, so they are not listed here. The 24x ids are
-- the CC0 surface photos (stone/grass/sand/snow/brick/rock), loaded in FILE_TEX below.
A.FLOORPAINT = { carpet = 201, stone = 240, grass = 241, sand = 242, snow = 243 }
A.WALLPAINT  = { blue = 220, beige = 221, sage = 222, brick = 244, rock = 245, stonewall = 240 }
-- representative RGB for the flooring/paint icon swatches (id → {r,g,b})
A.SWATCH = { carpet = { 158, 44, 50 }, blue = { 120, 150, 205 }, beige = { 212, 196, 164 }, sage = { 150, 185, 150 },
             stone = { 150, 148, 142 }, grass = { 96, 150, 78 }, sand = { 214, 196, 150 }, snow = { 226, 232, 240 },
             brick = { 150, 86, 70 }, rock = { 120, 116, 112 }, stonewall = { 150, 148, 142 },
             default_floor = { 150, 110, 66 }, default_paint = { 196, 188, 176 } }   -- "revert to default" swatches
-- arcade screen art (procedural, generated in buildTextures) + poster/painting picture textures
-- (reuse the CC0 surface photos as landscape "souvenirs"); ids the item definitions reference
A.SCREEN  = { taiko = 250, retro = 251 }
A.PICTURE = { nokon = 246, bol = 247 }   -- character portraits (Textures/Screens/*.png), loaded in FILE_TEX
-- solid-colour palette (texture ids) used by the shaded-box builders
A.COL = { wood = 210, gray = 211, dark = 212, screen = 213, metal = 214, red = 215, white = 216, black = 217,
          green = 230, leaf = 231, fabric = 232, cream = 233, gold = 234, posterA = 235, posterB = 236, canvasArt = 237 }
-- character: A.CHARA[dir(1-4)][state] = spriteId ; dir 1↙ 2↘ 3↗ 4↖ ; state "idle"/"run1"/"run2"
A.CHARA = { w = 70, h = 128 }

local function rgb(r, g, b)
    local function cl(v) return floor(v < 0 and 0 or (v > 255 and 255 or v)) end
    return (cl(r) << 16) | (cl(g) << 8) | cl(b)
end
local function solid(scene, id, r, g, b)
    scene:RegisterTexture(id, { rgb(r, g, b), rgb(r, g, b), rgb(r, g, b), rgb(r, g, b) }, 2, 2)
end

-- ── procedural textures (always registered; the file pass may overwrite them) ─────────────────────
function A.buildTextures(scene)
    -- wooden floor: planks with grain + plank seams
    do
        local w = 16; local t = {}
        for y = 0, w - 1 do for x = 0, w - 1 do
            local g = 8 * sin(x * 0.9) + (random() * 2 - 1) * 6
            local seam = (y % 8 == 0) and -28 or 0
            t[y * w + x + 1] = rgb(150 + g + seam, 100 + g + seam, 56 + g + seam)
        end end
        scene:RegisterTexture(A.TEX.wood, t, w, w)
    end
    -- red carpet: deep red weave speckle
    do
        local w = 16; local t = {}
        for i = 1, w * w do local n = (random() * 2 - 1) * 14; t[i] = rgb(150 + n, 32 + n, 38 + n) end
        scene:RegisterTexture(A.TEX.carpet, t, w, w)
    end
    -- plaster wall
    do
        local w = 16; local t = {}
        for i = 1, w * w do local n = (random() * 2 - 1) * 8; t[i] = rgb(196 + n, 188 + n, 176 + n) end
        scene:RegisterTexture(A.TEX.wall, t, w, w)
    end
    -- dark plank skirt for the raised-platform sides
    do
        local w = 16; local t = {}
        for y = 0, w - 1 do for x = 0, w - 1 do
            local seam = (x % 8 == 0) and -20 or 0
            local n = (random() * 2 - 1) * 6
            t[y * w + x + 1] = rgb(96 + n + seam, 66 + n + seam, 40 + n + seam)
        end end
        scene:RegisterTexture(A.TEX.skirt, t, w, w)
    end
    -- wall paints (faint speckle like the plaster, tinted)
    for id, base in pairs({ [A.WALLPAINT.blue] = { 120, 150, 205 }, [A.WALLPAINT.beige] = { 212, 196, 164 }, [A.WALLPAINT.sage] = { 150, 185, 150 } }) do
        local w = 16; local t = {}
        for i = 1, w * w do local n = (random() * 2 - 1) * 8; t[i] = rgb(base[1] + n, base[2] + n, base[3] + n) end
        scene:RegisterTexture(id, t, w, w)
    end
    -- furniture palette
    solid(scene, A.COL.wood, 150, 102, 58)
    solid(scene, A.COL.gray, 116, 120, 128)
    solid(scene, A.COL.dark, 44, 46, 52)
    solid(scene, A.COL.screen, 110, 170, 222)
    solid(scene, A.COL.metal, 98, 102, 112)
    solid(scene, A.COL.red, 170, 74, 74)
    solid(scene, A.COL.white, 234, 234, 228)
    solid(scene, A.COL.black, 26, 26, 32)
    solid(scene, A.COL.green, 74, 132, 74)
    solid(scene, A.COL.leaf, 96, 168, 88)
    solid(scene, A.COL.fabric, 108, 128, 168)
    solid(scene, A.COL.cream, 238, 226, 200)
    solid(scene, A.COL.gold, 208, 172, 92)
    solid(scene, A.COL.posterA, 226, 150, 92)
    solid(scene, A.COL.posterB, 120, 168, 210)
    solid(scene, A.COL.canvasArt, 168, 190, 150)
    -- arcade screen placeholders
    do
        local w = 32
        local taiko, retro = {}, {}
        for y = 0, w - 1 do for x = 0, w - 1 do
            local i = y * w + x + 1
            -- taiko: a big red drum face on cream with a dark rim
            local dx, dy = (x - w / 2 + 0.5) / (w / 2), (y - w / 2 + 0.5) / (w / 2)
            local d = math.sqrt(dx * dx + dy * dy)
            if d < 0.62 then taiko[i] = rgb(214, 60, 52)
            elseif d < 0.74 then taiko[i] = rgb(40, 30, 34)
            else taiko[i] = rgb(240, 226, 198) end
            -- retro: neon grid on near-black
            local grid = (x % 6 == 0 or y % 6 == 0)
            retro[i] = grid and rgb(70, 220, 200) or rgb(18, 14, 34)
        end end
        scene:RegisterTexture(A.SCREEN.taiko, taiko, w, w)
        scene:RegisterTexture(A.SCREEN.retro, retro, w, w)
    end
end

-- ── file texture pass: overwrite the procedural ids with the CC0 surfaces when present ────────────
-- Each file load is pcall-guarded and the procedural texture stays if anything is missing/fails.
local FILE_TEX = {
    { file = "Textures/Surfaces/woodfloor.png", id = 200 },   -- A.TEX.wood
    { file = "Textures/Surfaces/plaster.png",   id = 202 },   -- A.TEX.wall
    { file = "Textures/Surfaces/rug.png",       id = 201 },   -- A.TEX.carpet / FLOORPAINT.carpet
    { file = "Textures/Surfaces/paving.png",    id = 240 },   -- stone floor / stone wall
    { file = "Textures/Surfaces/grass.png",     id = 241 },   -- grass floor / meadow poster
    { file = "Textures/Surfaces/sand.png",      id = 242 },   -- sand floor / dunes poster
    { file = "Textures/Surfaces/snow.png",      id = 243 },   -- snow floor / snowfield poster
    { file = "Textures/Surfaces/brick.png",     id = 244 },   -- brick wall
    { file = "Textures/Surfaces/rock.png",      id = 245 },   -- rock wall
    { file = "Textures/Screens/nokon.png",      id = 246 },   -- Nokon portrait (picture wall item)
    { file = "Textures/Screens/bol.png",        id = 247 },   -- Bol portrait (picture wall item)
}
-- decoded surface PNGs, kept for the whole stage session. CreateTextureSync (disk read + PNG decode)
-- is expensive; doing it per icon scene (every builder icon calls registerInto) was a multi-second
-- stall. Decode ONCE here, then just re-register the cached texture into each scene (upload only).
local fileCache = nil
function A.buildFileTextures(scene)
    if fileCache == nil then
        fileCache = {}
        for _, e in ipairs(FILE_TEX) do
            local t = nil
            pcall(function() t = TEXTURE:CreateTextureSync(e.file) end)
            fileCache[e.id] = t or false
        end
    end
    for _, e in ipairs(FILE_TEX) do
        local t = fileCache[e.id]
        if t then
            pcall(function()
                scene:RegisterTextureFromImage(e.id, t)
                scene:SetTextureFilter(e.id, "mipmap")
            end)
        end
    end
end

-- ── character (4 dirs × idle/run1/run2) ───────────────────────────────────────────────────────────
function A.buildCharacter(scene)
    local states = { "idle", "run1", "run2" }
    local id = 320
    for dir = 1, 4 do
        A.CHARA[dir] = {}
        for _, st in ipairs(states) do
            -- Sync load: RegisterSpriteFromTexture reads the pixels back immediately, so the texture must be
            -- fully uploaded NOW (a plain async CreateTexture would register an empty sprite → invisible character).
            local tex = TEXTURE:CreateTextureSync("Textures/CharaTemplate/" .. dir .. "/" .. st .. ".png")
            scene:RegisterSpriteFromTexture(id, tex)
            tex:Dispose()   -- the sprite copied the pixels; free the GPU texture so rebuilds don't accumulate
            A.CHARA[dir][st] = id
            id = id + 1
        end
    end
end

function A.buildAll(scene)
    A.buildTextures(scene)
    A.buildFileTextures(scene)
    A.buildCharacter(scene)
end

-- register the procedural + file textures into an ARBITRARY scene (no character sprites) —
-- ModelIcon.newBuilder icon scenes start with an empty registry and sample these ids
function A.registerInto(scene)
    A.buildTextures(scene)
    A.buildFileTextures(scene)
end

return A
