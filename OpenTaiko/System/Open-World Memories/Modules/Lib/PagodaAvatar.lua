---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- pagoda avatar: P1 plays as their story student, Ume as puchichara
-- state in the save and one shared string (each stage has its own Lua VM)

local Student = require("Student")

local PA = {}

local UME = "A05 - Ume"

local HELD    = "pagoda_avatar_held"     -- global trigger: a pair is held
local CHARA   = "pagoda_avatar_chara"    -- held character folder
local PUCHI   = "pagoda_avatar_puchi"    -- held puchichara folder
local LIVE    = "pagoda_avatar_live"     -- shared string, "1" while this session owns the swap
local PALETTE = ".character_palette_"

-- name stored in global counters: byte count, then 6 bytes per counter (48 bits, exact in a double)
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

-- "A" or "B": story student chosen in My Room's mirror (Lib/Student)
function PA.student()
    return Student.current(save()):upper()
end

-- v2 character folder of student s (default: the chosen one)
function PA.studentCharacter(s)
    return Student.folder(s or PA.student())
end

-- a swap is held in the save
function PA.swapped()
    local sf = save()
    if sf == nil then return false end
    local ok, held = pcall(function() return sf:GetGlobalTrigger(HELD) end)
    return ok and held == true
end

-- Palettes.json entry -> SetPaletteGradient stops, or nil (default colours)
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

-- P1 palette for the equipped character: saved index in its Palettes.json
-- default colours when the index or file is missing or bad
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

-- dan select -> pagoda: hold the current pair, swap in student + Ume
function PA.enter()
    local sf = save()
    if sf == nil then return end
    pcall(function()
        -- an already held pair stays the one to restore
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

-- restore the held pair (no-op when none)
function PA.leave()
    local sf = save()
    if sf == nil then return end
    pcall(function() SHARED:SetSharedString(LIVE, "") end)
    if not PA.swapped() then return end
    -- hold dropped once the character is back or nothing is left to restore
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

-- leave() for a swap a crashed session never gave back
function PA.recover()
    local ok, live = pcall(function() return SHARED:GetSharedString(LIVE) end)
    if ok and live == "1" then return end
    PA.leave()
end

return PA
