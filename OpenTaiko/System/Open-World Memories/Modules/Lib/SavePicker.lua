---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- The save chosen before a stage opens: a list of the local saves, each with its nameplate and a line of
-- information the caller gives. The choice travels to the stage through SHARED.

local PopUI = require("PopUI")

local SP = {}

local KEY = "entry_save"
local ROW, PLATE_W, PLATE_H = 104, 330, 81
local PANEL_W = 980
local INFO_SIZE = 28
local COIN_PX = 44
local TEXT_PAD = 25

local ui, menu = nil, nil
local entries = {}
local fresh = false
local picked = nil
local coinTex = nil

-- the local saves that exist: { { value = slot, save = save file }, ... }
function SP.saves()
    local out = {}
    for i = 0, 4 do
        local sf = GetSaveFile(i)
        if sf and sf.SaveUID and sf.SaveUID ~= "" then out[#out + 1] = { value = i, save = sf } end
    end
    return out
end

-- hands a slot to the stage about to open
function SP.pass(slot) pcall(function() SHARED:SetSharedString(KEY, tostring(slot)) end) end

-- the slot handed to this stage (read once), or nil when it was opened without one
function SP.take()
    local ok, s = pcall(function() return SHARED:GetSharedString(KEY) end)
    pcall(function() SHARED:SetSharedString(KEY, "") end)
    local n = ok and tonumber(s) or nil
    if n == nil then return nil end
    local sf = GetSaveFile(n)
    if sf and sf.SaveUID and sf.SaveUID ~= "" then return n end
    return nil
end

-- the first save that exists, for a stage opened without a choice
function SP.first()
    local all = SP.saves()
    return all[1] and all[1].value or 0
end

function SP.isOpen() return ui ~= nil end

function SP.close()
    if ui then pcall(function() ui:disposeWidgets() end) end
    ui, menu, entries, picked = nil, nil, {}, nil
end

-- o: title, info(slot, save) -> text|nil, coins (show each save's coin count), theme, sfx
-- returns the slot at once when there is nothing to choose, else nil with the list open
function SP.open(o)
    SP.close()
    local all = SP.saves()
    if #all <= 1 then return all[1] and all[1].value or 0 end
    entries = {}
    local items = {}
    for i, e in ipairs(all) do
        local text = nil
        if o.info then
            local ok, s = pcall(o.info, e.value, e.save)
            if ok and type(s) == "string" and s ~= "" then text = s end
        end
        entries[i] = { value = e.value, save = e.save, text = text, coins = o.coins and (e.save.Coins or 0) or nil }
        items[i] = { text = "", value = e.value }
    end
    if o.coins and coinTex == nil then
        pcall(function() coinTex = SHARED:GetSharedTexture("Coin") end)
    end
    ui = PopUI.new{ theme = o.theme, sfx = o.sfx, navPlayer = nil }
    local h = 132 + #items * ROW + 40
    local x, y = (1920 - PANEL_W) / 2, math.floor((1080 - h) / 2)
    ui:panel{ x = x, y = y, w = PANEL_W, h = h, title = o.title or "" }
    menu = ui:menu{ x = x + 36, y = y + 100, w = PANEL_W - 72, h = #items * ROW, rowHeight = ROW, items = items,
                    onSelect = function(_, it) picked = it.value end }
    ui:_setFocusIndex(1)
    fresh, picked = true, nil
    return nil
end

-- nil while the list is up, "cancel", or the chosen slot (the list is closed in both cases)
function SP.update(ts)
    if ui == nil then return nil end
    if fresh then fresh = false; return nil end
    local r = ui:update(ts)
    if picked ~= nil then
        local slot = picked
        SP.close()
        return slot
    end
    if r == "cancel" then SP.close(); return "cancel" end
    return nil
end

function SP.draw()
    if ui == nil or menu == nil then return end
    ui:draw()
    local right = menu.x + menu.w - 30
    for i, e in ipairs(entries) do
        local ry = menu.y + (i - 1) * ROW
        local rcy = math.floor(ry + ROW / 2)
        pcall(function() NAMEPLATE:DrawPlayerNameplate(menu.x + 24, rcy - PLATE_H / 2, 255, e.value) end)
        local sel = (i == menu.selected)
        local fg = sel and { 255, 255, 255 } or { 70, 72, 96 }
        local bg = sel and { 0, 0, 0, 120 } or { 255, 255, 255, 150 }
        local tcy = rcy + ui:textNudge(INFO_SIZE)
        local maxW = menu.w - PLATE_W - 90
        if e.coins ~= nil then
            local count = string.format("%d", e.coins)
            ui:drawTextEx(INFO_SIZE, count, right, tcy, fg, bg, 1, 1, 0, "right")
            if coinTex ~= nil then
                pcall(function()
                    -- the text box carries TEXT_PAD px around its ink: the coin stands left of the first digit
                    local first = right - TEXT_PAD - ui:gfont(INFO_SIZE):Measure(count)
                    local k = COIN_PX / math.max(1, coinTex.Width)
                    coinTex:SetScale(k, k)
                    coinTex:DrawAtAnchor(first - 10 - COIN_PX / 2, rcy, "center")
                    coinTex:SetScale(1, 1)
                end)
            end
        elseif e.text ~= nil then
            ui:drawTextEx(INFO_SIZE, e.text, right, tcy, fg, bg, 1, 1, maxW, "right")
        end
    end
end

return SP
