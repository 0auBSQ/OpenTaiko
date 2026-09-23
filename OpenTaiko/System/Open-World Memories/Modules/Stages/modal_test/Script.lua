---@diagnostic disable: undefined-global, lowercase-global, need-check-nil, undefined-field
-- modal_test — a debug stage that plays the reward and unlock modals on demand: the coin piggy
-- bank with three amounts, and the chest reveal plus item card for each of the seven rarities of
-- every item type (character, puchichara, nameplate, song). Each button picks the first installed
-- item of that rarity, or the first item at all when none matches, and passes the rarity name to
-- the modal so the chest and colours always follow the button; nothing is written to the save
-- file. Esc returns to My Room.

local PopUI = require("PopUI")

local RARITY_NAMES = { "Poor", "Common", "Uncommon", "Rare", "Epic", "Legendary", "Mythical" }
local RARITY_COLORS = { { 150, 150, 162 }, { 236, 236, 244 }, { 96, 226, 118 }, { 72, 156, 255 }, { 200, 84, 255 }, { 255, 172, 64 }, { 255, 132, 204 } }
local RARITY_TO_INT = { Poor = 0, Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4, Mythical = 4 }
local COIN_AMOUNTS = { 5, 15, 21, 25, 42, 180, 1000 }

local ui, modal, status
local lists = { chara = {}, puchi = {}, plate = {}, song = {} }

local function collect(db)
    local out = {}
    if db == nil then return out end
    local ok, count = pcall(function() return db.Count end)
    if not ok or count == nil then return out end
    for i = 0, count - 1 do
        local e = db:GetByIndex(i)
        if e ~= nil then out[#out + 1] = e end
    end
    return out
end

local function collectSongs()
    local out = {}
    local ok = pcall(function()
        local lsls = GenerateSongListSettings()
        lsls.ModuloPagination = false
        lsls.AppendMainRandomBox = false
        lsls.AppendSubRandomBoxes = false
        lsls.SubBackBoxFrequency = 0
        lsls.IgnoreUnlockables = true
        local list = RequestSongList(lsls)
        if list == nil then return end
        local res = list:SearchSongsByPredicate(function(n)
            local keep = false
            pcall(function() keep = n.IsSong end)
            return keep
        end)
        for i = 0, res.Count - 1 do out[#out + 1] = res[i] end
    end)
    if not ok then out = {} end
    return out
end

-- the song list only exists once the enumeration is done; afterSongEnum picks it up when the stage
-- was entered before that
local function songsReady() return IsSongsEnumDone == nil or IsSongsEnumDone() end
local function refreshSongs()
    if songsReady() then lists.song = collectSongs() end
end

-- the first entry of that rarity, else the first entry
local function pick(entries, rarityName)
    for _, e in ipairs(entries) do
        local ok, r = pcall(function() return e.Rarity end)
        if ok and r == rarityName then return e end
    end
    return entries[1]
end

local function setStatus(s) status = s end

local function showCoins(amount)
    if modal == nil then return end
    local total = amount
    pcall(function() total = (GetSaveFile(0).Coins or 0) + amount end)
    modal:Activate(1, 0, 0, amount, total)   -- a negative amount is a payment
    setStatus("Coins: " .. (amount >= 0 and "+" or "") .. amount)
end

local function showItem(kind, rarityName)
    if modal == nil then return end
    local info
    if kind == 1 then
        local e = pick(lists.chara, rarityName)
        if e == nil then return setStatus("No character installed") end
        info = CHARACTER:CreateCharacter(e.FolderName)
    elseif kind == 2 then
        info = pick(lists.puchi, rarityName)
        if info == nil then return setStatus("No puchichara installed") end
    elseif kind == 3 then
        info = pick(lists.plate, rarityName)
        if info == nil then return setStatus("No nameplate found") end
    else
        if #lists.song == 0 then refreshSongs() end
        info = pick(lists.song, rarityName)
        if info == nil then return setStatus(songsReady() and "No song found" or "Songs are still being enumerated, try again in a moment") end
    end
    modal:Activate(1, RARITY_TO_INT[rarityName], kind, info, rarityName)
    setStatus(({ "Character", "Puchichara", "Nameplate", "Song" })[kind] .. " / " .. rarityName)
end

function onStart()
    local function sfx(name)
        local ok, s = pcall(function() return SOUND:CreateSFX(name) end)
        return ok and s or nil
    end
    ui = PopUI.new{ bg = true, sfx = { hover = sfx("Move.ogg"), click = sfx("Decide.ogg"), move = sfx("Move.ogg") } }
    ui:label{ text = "Modal Test", x = 60, y = 36, size = "title" }

    ui:label{ text = "Coins", x = 80, y = 150, size = "button" }
    for i, amount in ipairs(COIN_AMOUNTS) do
        ui:button{ text = "+" .. amount, x = 80 + (i - 1) * 200, y = 200, w = 180, h = 64, accent = true, onClick = function() showCoins(amount) end }
        ui:button{ text = "-" .. amount, x = 80 + (#COIN_AMOUNTS + i - 1) * 200, y = 200, w = 180, h = 64, onClick = function() showCoins(-amount) end }
    end

    local kinds = { { 1, "Character" }, { 2, "Puchichara" }, { 3, "Nameplate" }, { 4, "Song" } }
    for row, k in ipairs(kinds) do
        local y = 330 + (row - 1) * 150
        ui:label{ text = k[2], x = 80, y = y, size = "button" }
        for r, name in ipairs(RARITY_NAMES) do
            local c = RARITY_COLORS[r]
            ui:button{ text = name, x = 80 + (r - 1) * 252, y = y + 50, w = 236, h = 64,
                       style = { colors = { primary = c, primary2 = { math.floor(c[1] * 0.8), math.floor(c[2] * 0.8), math.floor(c[3] * 0.8) } } },
                       onClick = function() showItem(k[1], name) end }
        end
    end
end

function activate()
    modal = ROACTIVITY:GetROActivity("modal")
    lists.chara = collect(CHARACTERLIST)
    lists.puchi = collect(PUCHICHARALIST)
    lists.plate = collect(NAMEPLATESLIST)
    refreshSongs()
    setStatus(#lists.chara .. " characters, " .. #lists.puchi .. " puchicharas, " .. #lists.plate .. " nameplates, " .. #lists.song .. " songs")
end

function deactivate() end
function afterSongEnum()
    refreshSongs()
    setStatus(#lists.song .. " songs enumerated")
end
function onDestroy() end

function update(ts)
    if modal ~= nil and modal.IsActive then
        modal:Update()
        return
    end
    local r = ui:update(ts)
    if r == "cancel" then return Exit("stage", "myroom") end
end

function draw()
    ui:draw()
    ui:drawText(22, (status or "") .. "    Esc: back    Enter/drum: skip or close a modal", 60, 1006, ui.theme.colors.text)
    if modal ~= nil and modal.IsActive then modal:Draw() end
end
