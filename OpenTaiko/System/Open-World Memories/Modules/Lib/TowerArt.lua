---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- TowerArt.lua — draws a tower out of the pieces of a tower look, so the tower select, the loading screen and
-- the result show the very tower the player climbs.
--
-- A tower look is one Down background folder of the tower gameplay, Graphics/5_Game/5_Background/Tower/Down/
-- <look>/: its Script.lua draws the sky and the tower during play, and next to it sit the pieces this module
-- stacks (Base/BaseN.png the floors, cycled every ten floors; Deco/DecoN.png a decoration per floor; Top.png
-- the roof) and Config.json, the layout the script uses (body_x/body_y, deco_x/deco_y, move_x/move_y: the
-- slide from one floor to the next is the floor pitch here). The chart names its look with TOWERTYPE; a
-- chart without one gets the default look, the first lower background of the tower's default scene preset.
--
--   local TowerArt = require("TowerArt")
--   local art = TowerArt.load()             -- call it where textures may be created (activate / onStart)
--   art:resolve(look)                       -- the folder a chart's TowerType lands on (default look otherwise)
--   art:preload(look)                       -- loads a look's pieces now (behind a cover) rather than at first draw
--   art:draw(look, x, bottomY, scale, floors, opts)
--       bottom-centre anchored; opts.color a COLOR tint, opts.opacity 0..1, opts.clipTop / opts.clipBottom
--       the screen rows outside of which floors are skipped (tall towers)
--   art:height(look, scale, floors)         -- the drawn height in pixels
--   art:dispose()
--
-- Paths are relative to the calling module's folder; TowerArt.load(skinRoot) takes another root than the
-- default "../../../" of a Stages/, Transitions/ or ROActivities/ module. Textures load asynchronously, so a
-- look drawn right after preload pops in a frame or two later.

local TowerArt = {}
TowerArt.__index = TowerArt

local LOOKS_DIR = "Graphics/5_Game/5_Background/Tower/Down/"
local PRESETS = "Graphics/5_Game/5_Background/Presets.json"
local BASES_PER_FLOOR_CYCLE = 10

local function jget(node, key) return JSONLOADER:JsonGet(node, key) end

local function defaultLook(skinRoot)
    local look = nil
    pcall(function()
        local presets = JSONLOADER:JsonParseFileAny(skinRoot .. PRESETS)
        look = jget(jget(jget(jget(presets, "Tower"), ""), "DOWN"), 1)
    end)
    if type(look) == "string" and look ~= "" then return look end
    return nil
end

function TowerArt.load(skinRoot)
    local self = setmetatable({}, TowerArt)
    self.root = skinRoot or "../../../"
    self.default = defaultLook(self.root)
    self.looks = {}
    return self
end

-- the look folder a chart's TowerType lands on: itself when it exists, the default look otherwise
function TowerArt:resolve(look)
    if look ~= nil and look ~= "" and STORAGE:DirectoryExists(self.root .. LOOKS_DIR .. tostring(look)) then
        return tostring(look)
    end
    return self.default
end

local function loadSequence(dir, prefix)
    local list, i = {}, 0
    while STORAGE:FileExists(dir .. prefix .. i .. ".png") do
        list[#list + 1] = TEXTURE:CreateTexture(dir .. prefix .. i .. ".png")
        i = i + 1
    end
    return list
end

local function readLayout(dir)
    local layout = { body_x = 960, body_y = 1014, deco_x = 690, deco_y = 960, move_x = 0, move_y = 432 }
    if STORAGE:FileExists(dir .. "Config.json") then
        pcall(function()
            local cfg = JSONLOADER:JsonParseFileAny(dir .. "Config.json")
            for k, _ in pairs(layout) do
                local v = tonumber(jget(cfg, k))
                if v ~= nil then layout[k] = v end
            end
        end)
    end
    return layout
end

function TowerArt:preload(look)
    look = self:resolve(look)
    if look == nil then return nil end
    local pieces = self.looks[look]
    if pieces ~= nil then return pieces end
    local dir = self.root .. LOOKS_DIR .. look .. "/"
    pieces = {
        bases = loadSequence(dir .. "Base/", "Base"),
        decos = loadSequence(dir .. "Deco/", "Deco"),
        top = STORAGE:FileExists(dir .. "Top.png") and TEXTURE:CreateTexture(dir .. "Top.png") or nil,
        layout = readLayout(dir),
    }
    self.looks[look] = pieces
    return pieces
end

function TowerArt:height(look, scale, floors)
    local pieces = self:preload(look)
    if pieces == nil then return 0 end
    local topH = pieces.top ~= nil and pieces.top.Height or 0
    return (math.max(0, floors) * pieces.layout.move_y + topH) * scale
end

local function stamp(tex, x, y, scale, color, opacity)
    tex:SetScale(scale, scale)
    if color ~= nil then tex:SetColor(color) else tex:SetColor(1.0, 1.0, 1.0) end
    tex:SetOpacity(opacity)
    tex:DrawAtAnchor(x, y, "bottom")
end

function TowerArt:draw(look, x, bottomY, scale, floors, opts)
    local pieces = self:preload(look)
    if pieces == nil then return end
    opts = opts or {}
    local opacity = opts.opacity or 1.0
    local clipTop = opts.clipTop or -math.huge
    local clipBottom = opts.clipBottom or math.huge
    local layout = pieces.layout
    local pitch = layout.move_y * scale
    local decoDx, decoDy = (layout.deco_x - layout.body_x) * scale, (layout.deco_y - layout.body_y) * scale
    floors = math.max(0, math.floor(floors or 0))

    -- each floor: its base, then its decoration; the loop ends once the floors leave the top of the clip
    local nb, nd = #pieces.bases, #pieces.decos
    for i = 0, floors - 1 do
        local y = bottomY - i * pitch
        if nb > 0 then
            local base = pieces.bases[(math.floor(i / BASES_PER_FLOOR_CYCLE) % nb) + 1]
            if y - base.Height * scale > clipBottom then goto continue end
            if y < clipTop then break end
            stamp(base, x, y, scale, opts.color, opacity)
            if nd > 0 then
                stamp(pieces.decos[(i % nd) + 1], x + decoDx, y + decoDy, scale, opts.color, opacity)
            end
        end
        ::continue::
    end

    if pieces.top ~= nil then
        local y = bottomY - floors * pitch
        if y >= clipTop and y - pieces.top.Height * scale <= clipBottom then
            stamp(pieces.top, x, y, scale, opts.color, opacity)
        end
    end
end

function TowerArt:dispose()
    for _, pieces in pairs(self.looks) do
        for _, t in ipairs(pieces.bases) do t:Dispose() end
        for _, t in ipairs(pieces.decos) do t:Dispose() end
        if pieces.top ~= nil then pieces.top:Dispose() end
    end
    self.looks = {}
end

return TowerArt
