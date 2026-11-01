---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- PagodaAvatar.lua — in the pagoda, player 1 plays as their story student with Ume as puchichara.
-- The character and puchichara they had are held in the save until they leave or the game closes (the
-- pagoda's onDestroy); a run the game crashed in gives them back at the next title. Every stage runs in
-- its own Lua VM, so all the state lives in the save (and one shared string), never in this module.
--
--   PA.student()              "A" or "B", the story student chosen in My Room's mirror (Lib/Student)
--   PA.studentCharacter(s)    the student's v2 character folder name ("A" / "B", default the chosen one)
--   PA.swapped()              a swap is held in the save
--   PA.enter()                the dan select -> pagoda hand-off: hold the current pair, swap them
--   PA.leave()                put the held pair back (nothing when none is held)
--   PA.recover()              PA.leave() for a swap an earlier session never gave back (a crash)

local Student = require("Student")

local PA = {}

local UME = "A05 - Ume"

local HELD    = "pagoda_avatar_held"     -- global trigger: a pair is held
local CHARA   = "pagoda_avatar_chara"    -- the held character's folder name
local PUCHI   = "pagoda_avatar_puchi"    -- the held puchichara's folder name
local LIVE    = "pagoda_avatar_live"     -- shared string, "1" while the swap belongs to this session
local PALETTE = ".character_palette_"

-- a folder name kept in global counters: its byte count, then 6 bytes per counter (48 bits, exact in
-- a double), so any name survives without reaching the save database's text
local CHUNK   = 6
local MAX_LEN = 240

local function storeString(sf, key, s)
    if #s > MAX_LEN then return false end
    local function set(k, v)
        if sf:GetGlobalCounter(k) ~= v then sf:SetGlobalCounter(k, v) end
    end
    set(key .. "_n", #s)
    for c = 0, (#s - 1) // CHUNK do
        local v = 0
        for j = CHUNK, 1, -1 do v = v * 256 + (s:byte(c * CHUNK + j) or 0) end
        set(key .. "_" .. c, v)
    end
    return true
end

local function loadString(sf, key)
    local n = math.tointeger(sf:GetGlobalCounter(key .. "_n"))
    if n == nil or n <= 0 or n > MAX_LEN then return nil end
    local bytes = {}
    for c = 0, (n - 1) // CHUNK do
        local v = math.tointeger(sf:GetGlobalCounter(key .. "_" .. c))
        if v == nil or v < 0 or v >= (1 << (8 * CHUNK)) then return nil end
        for _ = 1, CHUNK do
            if #bytes < n then bytes[#bytes + 1] = v & 0xFF end
            v = v >> 8
        end
    end
    return string.char(table.unpack(bytes))
end

local function save()
    local ok, sf = pcall(GetSaveFile, 0)
    if ok then return sf end
    return nil
end

function PA.student()
    return Student.current(save()):upper()
end

function PA.studentCharacter(s)
    return Student.folder(s or PA.student())
end

function PA.swapped()
    local sf = save()
    if sf == nil then return false end
    local ok, held = pcall(function() return sf:GetGlobalTrigger(HELD) end)
    return ok and held == true
end

-- one Palettes.json entry as SetPaletteGradient's stops, or nil for the default colours
local function paletteStops(entry)
    local raw = JSONLOADER:JsonGet(entry, "stops")
    local stops = {}
    for i = 1, JSONLOADER:JsonCount(raw) do
        local s = JSONLOADER:JsonGet(raw, i)
        local n = JSONLOADER:JsonCount(s)
        if n >= 4 then
            local stop = {}
            for k = 1, math.min(n, 5) do
                local v = tonumber(JSONLOADER:JsonGet(s, k))
                if v == nil then return nil end
                stop[k] = v
            end
            stops[#stops + 1] = stop
        end
    end
    if #stops < 2 then return nil end
    return stops, tonumber(JSONLOADER:JsonGet(entry, "blend")) or 0
end

-- player 1's palette slot follows the character now equipped: its saved index in its Palettes.json,
-- the default colours when the index or the file is missing or bad
local function applyPalette(sf)
    pcall(function()
        local name = sf.CharacterName
        local chara = sf:GetCharacter()
        if chara == nil or not chara.IsValid or chara.FolderName ~= name then return end
        local stops, blend = nil, 0
        local idx = math.tointeger(sf:GetGlobalCounter(PALETTE .. name))
        if idx ~= nil and idx >= 0 then
            local ok, data = pcall(function() return JSONLOADER:JsonParseFileAny(chara.FullPath .. "/Palettes.json") end)
            if ok and data ~= nil and idx < JSONLOADER:JsonCount(data) then
                stops, blend = paletteStops(JSONLOADER:JsonGet(data, idx + 1))
            end
        end
        if stops ~= nil then chara:SetPaletteGradient(stops, blend) else chara:ClearPaletteGradient() end
    end)
end

function PA.enter()
    local sf = save()
    if sf == nil then return end
    pcall(function()
        -- a pair already held stays the one to give back
        if not sf:GetGlobalTrigger(HELD) then
            local puchi = sf:GetPuchichara()
            if not storeString(sf, CHARA, sf.CharacterName or "") then return end
            if not storeString(sf, PUCHI, puchi ~= nil and puchi.FolderName or "") then return end
            sf:SetGlobalTrigger(HELD, true)
        end
        SHARED:SetSharedString(LIVE, "1")
        pcall(function() sf:ChangeCharacter(PA.studentCharacter()) end)   -- no change when not installed
        pcall(function()
            if PUCHICHARALIST ~= nil and PUCHICHARALIST:GetByName(UME) ~= nil then sf:ChangePuchichara(UME) end
        end)
        applyPalette(sf)
    end)
end

function PA.leave()
    local sf = save()
    if sf == nil then return end
    pcall(function() SHARED:SetSharedString(LIVE, "") end)
    if not PA.swapped() then return end
    -- the hold is dropped once the character is back, or once there is nothing to put back
    local restored = pcall(function()
        local chara = loadString(sf, CHARA)
        if chara ~= nil and chara ~= "" then sf:ChangeCharacter(chara) end
    end)
    pcall(function()
        local puchi = loadString(sf, PUCHI)
        if puchi ~= nil and puchi ~= "" then sf:ChangePuchichara(puchi) end
    end)
    applyPalette(sf)
    if restored then pcall(function() sf:SetGlobalTrigger(HELD, false) end) end
end

function PA.recover()
    local ok, live = pcall(function() return SHARED:GetSharedString(LIVE) end)
    if ok and live == "1" then return end
    PA.leave()
end

return PA
