---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- story students as billboard sprites in the room: player and visitors

local Student = require("Characters.Student")

local AV = {}

AV.W, AV.H = 70, 128               -- px, read back from the first frame loaded
local WALK_FRAMES = 4
local WALK_STEP = 0.14             -- seconds per walk frame at full speed
local FIRST_SPRITE = 4000          -- sprite ids: one block of 20 per look; fishtank from 4900, lantern from 4960
local BLOCK = 20
local MAX_BLOCKS = 45              -- later looks get no sprite
local NAMES = { "idle", "walk1", "walk2", "walk3", "walk4" }

local scene = nil
local textures = {}                -- id → [facing][name] = LuaTexture | false
local looks = {}                   -- "source:id:palette" →{ [facing] = { idle = spriteId, walk = { ids } } } | false
local nextBlock = 0

-- facing d: 1 bottom-left, 2 bottom-right, 3 top-right, 4 top-left
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

-- fallback: the facing's idle frame, then any frame
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

-- new room world: every look registers again on next use
function AV.attach(s)
    scene = s
    looks = {}
    nextBlock = 0
end

-- registered once per scene, recoloured through Lib/Student (call outside draw)
-- visitor: palette index reads against Lib/Student's visitor list
function AV.prepare(id, palette, visitor)
    id = Student.valid(id)
    palette = Student.clamp(id, palette, visitor)
    local key = Student.paletteSource(id, visitor) .. ":" .. id .. ":" .. palette
    if looks[key] ~= nil then return looks[key] or nil end
    if scene == nil or nextBlock >= MAX_BLOCKS then return nil end
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

-- t: walk clock in s
function AV.sprite(look, dir, moving, t)
    if look == nil then return nil end
    local f = look[dir] or look[2]
    if f == nil then return nil end
    if moving and #f.walk > 0 then
        return f.walk[math.floor((t or 0) / WALK_STEP) % #f.walk + 1]
    end
    return f.idle or f.walk[1]
end

-- frame texture for 2D previews (mirror); k: walk frame, nil for idle
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
