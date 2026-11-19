---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- Saves.SavePicker: save list shown before a stage opens; the choice goes through SHARED

local HERE = require("Core.Here")(...)
local I18N = require("Locale.i18n")
local Pick = require("UI.Pick")

local T = I18N.texts("savepicker", HERE)

local SP = {}

local KEY = "entry_save"
local ROW, PLATE_W, PLATE_H = 104, 330, 81
local PANEL_W = 980
local INFO_SIZE = 28
local COINS_W = 320
local CLEAR = { 255, 255, 255, 0 }

-- title ids by kind; FALLBACK.title when neither title nor kind
local TITLES = { room = "title_room", shop = "title_shop" }
local FALLBACK = { title = "Whose save?", title_room = "Whose room?", title_shop = "Whose shop?" }

local ui, menu = nil, nil
local entries = {}
local fresh = false
local picked = nil

-- the text of one of this module's strings in the game's language
function SP.text(id)
    local code = I18N.currentCode()
    if I18N.code ~= code then I18N.load(code) end
    return T:get(id) or FALLBACK[id] or id
end

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

-- o: title (or kind = "room" | "shop"), info(slot, save) -> text|nil, coins (show coin counts),
-- theme, sfx, ui (library table or require name; default UI.Pick)
-- returns the slot at once when there is nothing to choose, else nil with the list open
function SP.open(o)
    o = o or {}
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
    local lib = type(o.ui) == "table" and o.ui or Pick(o.ui)
    local title = o.title or SP.text(TITLES[o.kind] or "title")
    ui = lib.new{ theme = o.theme, sfx = o.sfx, navPlayer = nil }
    local h = 132 + #items * ROW + 40
    local x, y = (1920 - PANEL_W) / 2, math.floor((1080 - h) / 2)
    ui:panel{ x = x, y = y, w = PANEL_W, h = h, title = title }
    menu = ui:menu{ x = x + 36, y = y + 100, w = PANEL_W - 72, h = #items * ROW, rowHeight = ROW, items = items,
                    onSelect = function(_, it) picked = it.value end }
    -- coin counter per save, right-aligned with the row text
    local right = menu.x + menu.w - 30
    for i, e in ipairs(entries) do
        if e.coins ~= nil then
            e.coinsW = ui:coins{ x = right - COINS_W, y = menu.y + (i - 1) * ROW, w = COINS_W, h = ROW,
                                 amount = e.coins, plain = true, size = INFO_SIZE }
        end
    end
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

-- row text colours: outlined set when the UI outlines text, else theme colours
local function rowColors(sel)
    local th = ui.theme
    if th.textOutline == false then
        return sel and th.colors.textOnAccent or th.colors.text, CLEAR
    end
    return sel and { 255, 255, 255 } or { 70, 72, 96 }, sel and { 0, 0, 0, 120 } or { 255, 255, 255, 150 }
end

function SP.draw()
    if ui == nil or menu == nil then return end
    for i, e in ipairs(entries) do
        if e.coinsW then e.coinsW.color, e.coinsW.edge = rowColors(i == menu.selected) end
    end
    ui:draw()
    local right = menu.x + menu.w - 30
    for i, e in ipairs(entries) do
        local ry = menu.y + (i - 1) * ROW
        local rcy = math.floor(ry + ROW / 2)
        pcall(function() NAMEPLATE:DrawPlayerNameplate(menu.x + 24, rcy - PLATE_H / 2, 255, e.value) end)
        local fg, bg = rowColors(i == menu.selected)
        local tcy = rcy + ui:textNudge(INFO_SIZE)
        local maxW = menu.w - PLATE_W - 90
        if e.coins == nil and e.text ~= nil then
            ui:drawTextEx(INFO_SIZE, e.text, right, tcy, fg, bg, 1, 1, maxW, "right")
        end
    end
end

return SP
