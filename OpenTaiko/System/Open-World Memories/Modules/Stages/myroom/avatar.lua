---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- avatar.lua — the story students as billboard sprites in the room: the player and every visitor.
--
-- Each student has, per facing (1 bottom-left, 2 bottom-right, 3 top-right, 4 top-left), a standing frame
-- and four walk frames in Textures/Students/<id>/<facing>/. A look (student + palette) is registered once
-- per scene as its own block of sprites, recoloured in that palette (Lib/Student), so any number of
-- palettes can share the room. A missing frame borrows the facing's standing frame, or any frame at all.

local Student = require("Student")

local AV = {}

AV.W, AV.H = 70, 128               -- a frame's size in pixels (read back from the first frame loaded)
local WALK_FRAMES = 4
local WALK_STEP = 0.14             -- seconds per walk frame at full speed
local FIRST_SPRITE = 4000          -- sprite ids: one block of 20 per look
local BLOCK = 20
local NAMES = { "idle", "walk1", "walk2", "walk3", "walk4" }

local scene = nil
local textures = {}                -- id → [facing][name] = LuaTexture | false
local looks = {}                   -- "source:id:palette" →{ [facing] = { idle = spriteId, walk = { ids } } } | false
local nextBlock = 0

local function frames(id)
    if textures[id] == nil then
        local set = {}
        for d = 1, 4 do
            set[d] = {}
            for _, n in ipairs(NAMES) do
                local tex, w, h = nil, 0, 0
                pcall(function()
                    tex = TEXTURE:CreateTextureSync("Textures/Students/" .. id .. "/" .. d .. "/" .. n .. ".png")
                    w, h = tex.Width or 0, tex.Height or 0
                end)
                if tex and (w <= 0 or h <= 0) then   -- a missing file loads as an empty texture
                    pcall(function() tex:Dispose() end)
                    tex = nil
                end
                set[d][n] = tex or false
                if tex and not AV._sized then AV.W, AV.H, AV._sized = w, h, true end
            end
        end
        textures[id] = set
    end
    return textures[id]
end

-- the texture shown for a facing/frame name, with the fallbacks above
local function pick(set, d, n)
    local f = set[d] and set[d][n]
    if f then return f end
    f = set[d] and set[d].idle
    if f then return f end
    for dd = 1, 4 do
        for _, nn in ipairs(NAMES) do if set[dd] and set[dd][nn] then return set[dd][nn] end end
    end
    return nil
end

-- the scene the sprites go into (a new room world): every look registers again on its next use
function AV.attach(s)
    scene = s
    looks = {}
    nextBlock = 0
end

-- the sprites of a student in a palette, registered on first use (call outside draw); visitor: another
-- player's student, whose palette index reads against Lib/Student's visitor list
function AV.prepare(id, palette, visitor)
    id = Student.valid(id)
    palette = Student.clamp(id, palette, visitor)
    local key = Student.paletteSource(id, visitor) .. ":" .. id .. ":" .. palette
    if looks[key] ~= nil then return looks[key] or nil end
    if scene == nil then return nil end
    local set = frames(id)
    local base = FIRST_SPRITE + nextBlock * BLOCK
    nextBlock = nextBlock + 1
    local look, any = {}, false
    for d = 1, 4 do
        look[d] = { walk = {} }
        for i, n in ipairs(NAMES) do
            local tex = pick(set, d, n)
            local sid = base + (d - 1) * #NAMES + (i - 1)
            if tex and Student.registerSprite(scene, sid, tex, id, palette, visitor) then
                any = true
                if n == "idle" then look[d].idle = sid else look[d].walk[#look[d].walk + 1] = sid end
            end
        end
    end
    looks[key] = any and look or false
    return looks[key] or nil
end

-- the sprite for a facing, standing or walking (t = the walk clock in seconds)
function AV.sprite(look, dir, moving, t)
    if look == nil then return nil end
    local f = look[dir] or look[2]
    if f == nil then return nil end
    if moving and #f.walk > 0 then
        return f.walk[math.floor((t or 0) / WALK_STEP) % #f.walk + 1]
    end
    return f.idle or f.walk[1]
end

-- the frame texture for 2D previews (the mirror): facing d, standing or walk frame k
function AV.frame(id, d, k)
    local set = frames(Student.valid(id))
    return pick(set, d, (k and k >= 1 and k <= WALK_FRAMES) and ("walk" .. k) or "idle")
end

AV.WALK_FRAMES, AV.WALK_STEP = WALK_FRAMES, WALK_STEP

function AV.dispose()
    for _, set in pairs(textures) do
        for d = 1, 4 do
            for _, tex in pairs(set[d] or {}) do
                if tex then pcall(function() tex:Dispose() end) end
            end
        end
    end
    textures, looks, scene, nextBlock = {}, {}, nil, 0
end

return AV
