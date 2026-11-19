---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- TowerArt: draws the gameplay tower from a look's pieces, for menus
-- looks: Modules/Lib/TowerArt/<look>/, named as Tower/Down/<look>/ (chart TOWERTYPE); else DEFAULT_LOOK

-- a look's default floor sizes: Config/TowerArt/layout.json
local HERE = require("Core.Here")(...)
local Config = require("Core.Config")
local Layout = Config.load(HERE .. "Config/TowerArt/layout.json")

local TowerArt = {}
TowerArt.__index = TowerArt

local ART_DIR = "Modules/Lib/TowerArt/"
local DEFAULT_LOOK = "Day"          -- also gameplay's: the Tower "" preset in Presets.json lists only this look
local BASES_PER_FLOOR_CYCLE = 10

local function jget(node, key) return JSONLOADER:JsonGet(node, key) end

-- call where textures may be created (activate / onStart); skinRoot default "../../../"
function TowerArt.load(skinRoot)
    local self = setmetatable({}, TowerArt)
    self.dir = (skinRoot or "../../../") .. ART_DIR
    self.looks = {}
    self.resolved = {}
    return self
end

-- the look folder a chart's TowerType lands on: itself when it is here, the default look otherwise
function TowerArt:resolve(look)
    local key = look ~= nil and tostring(look) or ""
    local found = self.resolved[key]
    if found == nil then
        found = (key ~= "" and STORAGE:DirectoryExists(self.dir .. key)) and key or DEFAULT_LOOK
        self.resolved[key] = found
    end
    return found
end

local function loadSequence(dir, prefix)
    local list, i = {}, 0
    while STORAGE:FileExists(dir .. prefix .. i .. ".png") do
        list[#list + 1] = TEXTURE:CreateTexture(dir .. prefix .. i .. ".png")
        i = i + 1
    end
    return list
end

local LookDefaults = Layout.look_defaults

local function readLayout(dir)
    local layout = { floor_height = LookDefaults.floor_height, deco_dx = LookDefaults.deco_dx, deco_dy = LookDefaults.deco_dy }
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

-- pieces at half the gameplay size:
--   Base/BaseN.png   floors, cycled every ten floors
--   Deco/DecoN.png   optional decoration, cycled every floor
--   Top.png          roof over the last floor
--   Config.json      piece px: floor_height, deco_dx / deco_dy (deco bottom centre from the floor's)
-- textures load async: wait for art:ready(look) to avoid a pop-in
function TowerArt:preload(look)
    look = self:resolve(look)
    local pieces = self.looks[look]
    if pieces ~= nil then return pieces end
    local dir = self.dir .. look .. "/"
    pieces = {
        bases = loadSequence(dir .. "Base/", "Base"),
        decos = loadSequence(dir .. "Deco/", "Deco"),
        top = STORAGE:FileExists(dir .. "Top.png") and TEXTURE:CreateTexture(dir .. "Top.png") or nil,
        layout = readLayout(dir),
    }
    self.looks[look] = pieces
    return pieces
end

-- a missing file counts as ready: it never arrives
function TowerArt:ready(look)
    local pieces = self:preload(look)
    if pieces == nil then return true end
    local function done(t) return t == nil or not t.Loaded or t.Ready end
    for _, t in ipairs(pieces.bases) do if not done(t) then return false end end
    for _, t in ipairs(pieces.decos) do if not done(t) then return false end end
    return done(pieces.top)
end

function TowerArt:height(look, scale, floors)
    local pieces = self:preload(look)
    if pieces == nil then return 0 end
    local topH = pieces.top ~= nil and pieces.top.Height or 0
    return (math.max(0, floors) * pieces.layout.floor_height + topH) * scale
end

local function stamp(tex, x, y, scale, color, opacity)
    tex:SetScale(scale, scale)
    if color ~= nil then tex:SetColor(color) else tex:SetColor(1.0, 1.0, 1.0) end
    tex:SetOpacity(opacity)
    tex:DrawAtAnchor(x, y, "bottom")
end

-- bottom-centre anchor, scale 1 = piece size; opts: color (COLOR), opacity 0..1, clipTop / clipBottom rows
function TowerArt:draw(look, x, bottomY, scale, floors, opts)
    local pieces = self:preload(look)
    if pieces == nil then return end
    opts = opts or {}
    local opacity = opts.opacity or 1.0
    local clipTop = opts.clipTop or -math.huge
    local clipBottom = opts.clipBottom or math.huge
    local layout = pieces.layout
    local pitch = layout.floor_height * scale
    local decoDx, decoDy = layout.deco_dx * scale, layout.deco_dy * scale
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
