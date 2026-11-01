---@diagnostic disable: undefined-global, undefined-field
-- Online.Identity: what a player sends about themselves when they enter a room (NET:SetLocalPlayer), and
-- how the others read it back from NET:PeersJson. The info carries the name, character, puchichara,
-- nameplate id, dan plate and character palette, so every machine can show each remote player on a
-- virtual slot.

local P = require("Online.Protocol")

local floor = math.floor
local jstr = P.jstr

local M = {}

-- the local player's name (save slot 0)
function M.myName() local sf = GetSaveFile(0); local n = sf and sf.Name; if n == nil or n == "" then n = "Player" end return n end

-- the local player's selected character palette, as a JSON object {"b":blend,"s":[[pos,r,g,b(,a)],...]}
-- (or "null"). Read from the character's own Palettes.json by the saved selection index, so the receiver
-- does not need its own copy of that palette file.
function M.selfPaletteJson()
    local out = "null"
    pcall(function()
        local sf = GetSaveFile(0); local cha = sf:GetCharacter()
        if not (cha and cha.IsValid) then return end
        local idx = floor(sf:GetGlobalCounter(".character_palette_" .. (sf.CharacterName or "")) or 0)
        local raw = JSONLOADER:JsonParseFileAny(cha.FullPath .. "/Palettes.json")
        if not (raw and raw.Count and idx >= 0 and idx < raw.Count) then return end
        local e = raw[idx + 1]; if not e then return end
        local rawStops = JSONLOADER:JsonGet(e, "stops")
        if not (rawStops and rawStops.Count and rawStops.Count >= 2) then return end   -- no stops: the default palette
        local blend = tonumber(tostring(JSONLOADER:JsonGet(e, "blend") or 0)) or 0
        local parts = {}
        for j = 1, rawStops.Count do
            local s = rawStops[j]; local a = JSONLOADER:JsonGet(s, 5)
            parts[j] = a ~= nil and string.format("[%s,%s,%s,%s,%s]", s[1], s[2], s[3], s[4], a)
                or string.format("[%s,%s,%s,%s]", s[1], s[2], s[3], s[4])
        end
        out = string.format('{"b":%s,"s":[%s]}', blend, table.concat(parts, ","))
    end)
    return out
end

-- the local player's join info. npk is the nameplate id: the receiver takes title, type and rarity from its
-- own catalogue and shows no title when it lacks that id; dgold/dtype style the dan plate; pal is the palette.
function M.selfInfo()
    local sf = GetSaveFile(0)
    local char, puchi, dan = "None", "None", ""
    local npk, dgold, dtype = "", false, 0
    pcall(function() char = sf.CharacterName or "None" end)
    pcall(function() local p = sf:GetPuchichara(); if p then puchi = p.FolderName or "None" end end)
    pcall(function() local np = sf.NameplateInfo; if np then npk = np.Id or "" end end)
    pcall(function() dan = sf.SelectedDan or "" end)
    pcall(function() local dp = sf.DanplateInfo; if dp then dgold = dp.Gold and true or false; dtype = dp.ClearStatus or 0 end end)
    return string.format('{"name":%s,"char":%s,"puchi":%s,"dan":%s,"npk":%s,"dgold":%s,"dtype":%d,"pal":%s}',
        jstr(M.myName()), jstr(char), jstr(puchi), jstr(dan), jstr(npk), tostring(dgold), dtype, M.selfPaletteJson())
end

-- a received "pal" object back into (stops, blend) for SetPaletteGradient; nil, 0 when there is none
function M.parsePalette(io)
    if not io then return nil, 0 end
    local pal = JSONLOADER:JsonGet(io, "pal"); if not pal then return nil, 0 end
    local sArr = JSONLOADER:JsonGet(pal, "s")
    if not (sArr and sArr.Count and sArr.Count >= 2) then return nil, 0 end
    local blend = tonumber(tostring(JSONLOADER:JsonGet(pal, "b") or 0)) or 0
    local stops = {}
    for j = 1, sArr.Count do
        local s = sArr[j]; local a = JSONLOADER:JsonGet(s, 5)
        stops[j] = { s[1], s[2], s[3], s[4] }
        if a ~= nil then stops[j][5] = a end
    end
    return stops, blend
end

-- one roster entry of NET:PeersJson (its "info" already parsed, or nil) as
-- { name, char, puchi, dan, npk, dgold, dtype, palStops, palBlend }
function M.parseInfo(io, id)
    local name = (io and JSONLOADER:JsonGet(io, "name")) or ("Player " .. id)
    local palStops, palBlend = M.parsePalette(io)
    return { name = name,
        char  = (io and JSONLOADER:JsonGet(io, "char"))  or "None",
        puchi = (io and JSONLOADER:JsonGet(io, "puchi")) or "None",
        dan   = (io and JSONLOADER:JsonGet(io, "dan"))   or "",
        npk   = tostring((io and JSONLOADER:JsonGet(io, "npk")) or ""),
        dgold = (io and JSONLOADER:JsonGet(io, "dgold")) and true or false,
        dtype = floor((io and JSONLOADER:JsonGet(io, "dtype")) or 0),
        palStops = palStops, palBlend = palBlend }
end

return M
