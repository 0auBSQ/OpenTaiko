---@diagnostic disable: undefined-global
-- almanac.lua — date patterns (JST) and the opaque "enc:" tokens they can be written as, so a
-- schedule does not have to be readable in the source. Encrypt with the companion tool (kept
-- out of the skin) or with Almanac.encrypt.
--
-- Pattern syntax:
--   "2020-06-15"             one day
--   "2020-06-15+30"          that day and the 29 after it (a window of 30 days)
--   "2020-06-15..2020-07-31" inclusive range (both ends fully specified)
--   "*-06-15"                that day every year          "*-06-15+7"   a yearly week
--   "*-*-01"                 the 1st of every month       "2020-*-13"   every 13th of 2020
--   "*-06-*"                 a whole month, every year    "2020-12-*"   one month
--   { "...", "..." }         any of several patterns
--
-- An entry is { code=, when=, priority=, sticky= }: while `when` matches, the entry is active;
-- `sticky` keeps it active for that save file once it has matched. `code` may itself be an
-- "enc:" token (see plain()).

local M = {}

-- ── one-way mixer (also the keystream source) ────────────────────────────────────────────────
local function mix(n, salt)
    local h = 2166136261
    local s = tostring(n) .. "|" .. salt
    for i = 1, #s do
        h = h ~ s:byte(i)
        h = (h * 16777619) & 0xFFFFFFFF
    end
    for _ = 1, 4 do
        h = (h ~ (h >> 13)) & 0xFFFFFFFF
        h = (h * 0x5bd1e995) & 0xFFFFFFFF
        h = (h ~ (h >> 15)) & 0xFFFFFFFF
    end
    return h
end

-- ── token cipher: XOR keystream + 16-bit checksum, base32 text ───────────────────────────────
local KEY = "0x9E37-79B1|tide"
local PREFIX = "enc:"
local ALPHA = "abcdefghijklmnopqrstuvwxyz234567"
local UNALPHA = {}
for i = 1, #ALPHA do UNALPHA[ALPHA:sub(i, i)] = i - 1 end

local function b32encode(bytes)
    local out, buf, bits = {}, 0, 0
    for _, b in ipairs(bytes) do
        buf = (buf << 8) | b
        bits = bits + 8
        while bits >= 5 do
            bits = bits - 5
            local idx = (buf >> bits) & 31
            out[#out + 1] = ALPHA:sub(idx + 1, idx + 1)
        end
    end
    if bits > 0 then
        local idx = (buf << (5 - bits)) & 31
        out[#out + 1] = ALPHA:sub(idx + 1, idx + 1)
    end
    return table.concat(out)
end

local function b32decode(s)
    local out, buf, bits = {}, 0, 0
    for i = 1, #s do
        local v = UNALPHA[s:sub(i, i)]
        if v == nil then return nil end
        buf = ((buf << 5) | v) & 0xFFFFFFFF
        bits = bits + 5
        if bits >= 8 then
            bits = bits - 8
            out[#out + 1] = (buf >> bits) & 0xFF
        end
    end
    return out
end

local function checksum(text) return mix(text, "sum|" .. KEY) & 0xFFFF end

function M.encrypt(pattern)
    local bytes = {}
    for i = 1, #pattern do
        bytes[i] = pattern:byte(i) ~ (mix(i, KEY) & 0xFF)
    end
    local sum = checksum(pattern)
    bytes[#bytes + 1] = (sum >> 8) ~ (mix(#pattern + 1, KEY) & 0xFF)
    bytes[#bytes + 1] = (sum & 0xFF) ~ (mix(#pattern + 2, KEY) & 0xFF)
    return PREFIX .. b32encode(bytes)
end

-- nil when the token is not ours (bad alphabet / checksum)
function M.decrypt(token)
    if type(token) ~= "string" or token:sub(1, #PREFIX) ~= PREFIX then return nil end
    local bytes = b32decode(token:sub(#PREFIX + 1))
    if bytes == nil or #bytes < 3 then return nil end
    local n = #bytes - 2
    local chars = {}
    for i = 1, n do chars[i] = string.char(bytes[i] ~ (mix(i, KEY) & 0xFF)) end
    local text = table.concat(chars)
    local hi = bytes[n + 1] ~ (mix(n + 1, KEY) & 0xFF)
    local lo = bytes[n + 2] ~ (mix(n + 2, KEY) & 0xFF)
    if ((hi << 8) | lo) ~= checksum(text) then return nil end
    return text
end

-- a token decrypted, or the string as it is
function M.plain(s)
    if type(s) == "string" and s:sub(1, #PREFIX) == PREFIX then return M.decrypt(s) end
    return s
end

-- ── patterns ─────────────────────────────────────────────────────────────────────────────────
local function field(s)
    if s == "*" then return false end
    local v = tonumber(s)
    if v == nil then return nil end
    return v
end

local function parseDate(s)
    local y, m, d = s:match("^(%d%d%d%d)%-(%d%d)%-(%d%d)$")
    if y == nil then return nil end
    return tonumber(y), tonumber(m), tonumber(d)
end

-- days since 1970-01-01 for a calendar day (pure arithmetic: no time zone, no DST)
local function dayIndex(y, m, d)
    if m <= 2 then y = y - 1 end
    local era = (y >= 0 and y or y - 399) // 400
    local yoe = y - era * 400
    local mp = (m + 9) % 12
    local doy = (153 * mp + 2) // 5 + d - 1
    local doe = yoe * 365 + yoe // 4 - yoe // 100 + doy
    return era * 146097 + doe - 719468
end

-- one spec string → { y=, m=, d= (number | false for *), days= } or { fromDay=, toDay= }
function M.parse(spec)
    if type(spec) ~= "string" then return nil end
    local text = spec
    if spec:sub(1, #PREFIX) == PREFIX then
        text = M.decrypt(spec)
        if text == nil then return nil end
    end
    local a, b = text:match("^(%S+)%.%.(%S+)$")
    if a ~= nil then
        local y1, m1, d1 = parseDate(a)
        local y2, m2, d2 = parseDate(b)
        if y1 == nil or y2 == nil then return nil end
        return { fromDay = dayIndex(y1, m1, d1), toDay = dayIndex(y2, m2, d2) }
    end
    local body, plus = text:match("^(.-)%+(%d+)$")
    body = body or text
    local ys, ms, ds = body:match("^([%d%*]+)%-([%d%*]+)%-([%d%*]+)$")
    if ys == nil then return nil end
    local y, m, d = field(ys), field(ms), field(ds)
    if y == nil or m == nil or d == nil then return nil end
    return { y = y, m = m, d = d, days = plus and tonumber(plus) or 1 }
end

-- the JST calendar day `back` days ago, and its day index
local function jst(back)
    local t = os.time() + 32400 - (back or 0) * 86400
    local dt = os.date("!*t", t)
    return dt.year, dt.month, dt.day, t // 86400
end

function M.matchesPattern(p)
    if p == nil then return false end
    if p.fromDay ~= nil then
        local _, _, _, today = jst(0)
        return today >= p.fromDay and today <= p.toDay
    end
    for k = 0, (p.days or 1) - 1 do
        local y, m, d = jst(k)
        if (p.y == false or p.y == y) and (p.m == false or p.m == m) and (p.d == false or p.d == d) then
            return true
        end
    end
    return false
end

-- spec = a pattern string / token, or a list of them
function M.matches(spec)
    if type(spec) == "table" then
        for _, s in ipairs(spec) do if M.matches(s) then return true end end
        return false
    end
    return M.matchesPattern(M.parse(spec))
end

-- ── entries ──────────────────────────────────────────────────────────────────────────────────
local function trigger(entry) return ".almanac_" .. tostring(M.plain(entry.code) or entry.id or "gate") end

-- is this entry active today for this save file (sticky entries stay active once matched)
function M.active(entry, saveFile)
    if entry == nil then return false end
    if entry.sticky and saveFile ~= nil then
        local ok, was = pcall(function() return saveFile:GetGlobalTrigger(trigger(entry)) end)
        if ok and was then return true end
    end
    if not M.matches(entry.when) then return false end
    if entry.sticky and saveFile ~= nil then
        pcall(function() saveFile:SetGlobalTrigger(trigger(entry), true) end)
    end
    return true
end

-- the active entry with the highest priority (ties: first in the list); `skip(entry)` can rule
-- entries out (e.g. already owned)
function M.pick(entries, saveFile, skip)
    local best = nil
    for _, e in ipairs(entries or {}) do
        if not (skip and skip(e)) and M.active(e, saveFile) then
            if best == nil or (e.priority or 0) > (best.priority or 0) then best = e end
        end
    end
    return best
end

return M
