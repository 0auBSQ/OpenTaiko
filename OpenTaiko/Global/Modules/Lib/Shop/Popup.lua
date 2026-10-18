---@diagnostic disable: undefined-global, undefined-field
-- Shop.Popup: an optional pop-up for a purchase through the "modal" ROActivity, for a skin that wants
-- one (OWM's coin shop has its own feedback and does not use it).
--   Popup.show(item, qty, picture)   -- picture: a SHARED texture name; furniture defaults to My Room's
--                                    -- thumbnail "myroom_thumb_<id>"
--   Popup.update() -> true while it plays, Popup.draw(), Popup.stop()

local Catalog = require("Shop.Catalog")
local Texts   = require("Shop.Texts")

local M = {}

local RARITY_INT = { Poor = 0, Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4, Mythical = 4 }

local function modal() return ROACTIVITY:GetROActivity("modal") end

-- false when it can't show
function M.show(item, qty, picture)
    local m = modal()
    if m == nil or item == nil then return false end
    if item.Type == "nameplate" then
        local info = NAMEPLATESLIST:GetById(item.RefInt)
        if info == nil then return false end
        m:Activate(0, RARITY_INT[info.Rarity] or 0, 3, info, nil)
        return true
    end
    local name = item.LocalizedName or Catalog.name(item)
    if (qty or 1) > 1 then name = name .. " ×" .. qty end
    local fid = Catalog.furnitureId(item.Code)
    local header
    if fid then
        picture = picture or ("myroom_thumb_" .. fid)
        header = Texts.get("new_furniture", "New Furniture!")
    elseif item.Code:sub(1, 10) == "vault_key_" then
        header = Texts.get("new_key", "New Key!")
    end
    m:Activate(0, 0, 5, name, nil, picture, header)
    return true
end

function M.update()
    local m = modal()
    if m == nil or not m.IsActive then return false end
    m:Update()
    return m.IsActive
end

function M.draw()
    local m = modal()
    if m ~= nil and m.IsActive then m:Draw() end
end

function M.stop()
    local m = modal()
    if m ~= nil and m.IsActive then m:Deactivate() end
end

return M
