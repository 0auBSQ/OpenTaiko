-- Core.Json — JSON text from Lua values and back, in plain Lua.
--   Json.encode(value)      -> string; tables with keys 1..n are arrays, other tables objects (keys sorted,
--                              so the same value always gives the same text); nil, functions and userdata
--                              are left out of objects; numbers that are whole are written without a point
--   Json.decode(text)       -> value, or nil and an error message; JSON null becomes Json.null
--   Json.null               -> the value standing for JSON null (encodes back to null)

local Json = {}

Json.null = setmetatable({}, { __tostring = function() return "null" end })

local ESC = { ['"'] = '\\"', ["\\"] = "\\\\", ["\b"] = "\\b", ["\f"] = "\\f", ["\n"] = "\\n", ["\r"] = "\\r", ["\t"] = "\\t" }

local function encodeString(s)
    return '"' .. s:gsub('[%c"\\]', function(c) return ESC[c] or string.format("\\u%04x", c:byte()) end) .. '"'
end

local function isArray(t)
    local n = 0
    for k in pairs(t) do
        if type(k) ~= "number" or k < 1 or k % 1 ~= 0 then return false end
        n = n + 1
    end
    for i = 1, n do if t[i] == nil then return false end end
    return true, n
end

local function encodeValue(v, out, seen)
    local tv = type(v)
    if v == nil or v == Json.null then
        out[#out + 1] = "null"
    elseif tv == "boolean" then
        out[#out + 1] = v and "true" or "false"
    elseif tv == "number" then
        if v ~= v or v == math.huge or v == -math.huge then
            out[#out + 1] = "null"
        elseif v % 1 == 0 and math.abs(v) < 2 ^ 53 then
            out[#out + 1] = string.format("%d", v)
        else
            out[#out + 1] = string.format("%.17g", v)
        end
    elseif tv == "string" then
        out[#out + 1] = encodeString(v)
    elseif tv == "table" then
        if seen[v] then error("Json.encode: a table contains itself") end
        seen[v] = true
        local arr, n = isArray(v)
        if arr and n > 0 then
            out[#out + 1] = "["
            for i = 1, n do
                if i > 1 then out[#out + 1] = "," end
                encodeValue(v[i], out, seen)
            end
            out[#out + 1] = "]"
        else
            local keys = {}
            for k, val in pairs(v) do
                local tk, tval = type(k), type(val)
                if (tk == "string" or tk == "number") and tval ~= "function" and tval ~= "userdata" and tval ~= "thread" then
                    keys[#keys + 1] = tostring(k)
                end
            end
            table.sort(keys)
            out[#out + 1] = "{"
            for i, k in ipairs(keys) do
                if i > 1 then out[#out + 1] = "," end
                local val = v[k]
                if val == nil then val = v[tonumber(k)] end
                out[#out + 1] = encodeString(k)
                out[#out + 1] = ":"
                encodeValue(val, out, seen)
            end
            out[#out + 1] = "}"
        end
        seen[v] = nil
    else
        out[#out + 1] = "null"
    end
end

function Json.encode(value)
    local out = {}
    encodeValue(value, out, {})
    return table.concat(out)
end

-- ── decoding ──────────────────────────────────────────────────────────────────

local function utf8char(cp)
    if cp < 0x80 then return string.char(cp) end
    if cp < 0x800 then return string.char(0xC0 | (cp >> 6), 0x80 | (cp & 0x3F)) end
    if cp < 0x10000 then return string.char(0xE0 | (cp >> 12), 0x80 | ((cp >> 6) & 0x3F), 0x80 | (cp & 0x3F)) end
    return string.char(0xF0 | (cp >> 18), 0x80 | ((cp >> 12) & 0x3F), 0x80 | ((cp >> 6) & 0x3F), 0x80 | (cp & 0x3F))
end

local UNESC = { ['"'] = '"', ["\\"] = "\\", ["/"] = "/", b = "\b", f = "\f", n = "\n", r = "\r", t = "\t" }

local decodeValue

local function skip(s, i)
    return s:find("[^ \t\r\n]", i) or (#s + 1)
end

local function decodeString(s, i)
    local out, j = {}, i + 1
    while true do
        local c = s:sub(j, j)
        if c == "" then error("unterminated string at " .. i) end
        if c == '"' then return table.concat(out), j + 1 end
        if c == "\\" then
            local e = s:sub(j + 1, j + 1)
            if e == "u" then
                local cp = tonumber(s:sub(j + 2, j + 5), 16)
                if cp == nil then error("bad \\u escape at " .. j) end
                j = j + 6
                if cp >= 0xD800 and cp <= 0xDBFF and s:sub(j, j + 1) == "\\u" then
                    local lo = tonumber(s:sub(j + 2, j + 5), 16)
                    if lo and lo >= 0xDC00 and lo <= 0xDFFF then
                        cp = 0x10000 + ((cp - 0xD800) << 10) + (lo - 0xDC00)
                        j = j + 6
                    end
                end
                out[#out + 1] = utf8char(cp)
            elseif UNESC[e] then
                out[#out + 1] = UNESC[e]
                j = j + 2
            else
                error("bad escape at " .. j)
            end
        else
            local k = s:find('["\\]', j) or (#s + 1)
            out[#out + 1] = s:sub(j, k - 1)
            j = k
        end
    end
end

decodeValue = function(s, i)
    i = skip(s, i)
    local c = s:sub(i, i)
    if c == "{" then
        local obj = {}
        i = skip(s, i + 1)
        if s:sub(i, i) == "}" then return obj, i + 1 end
        while true do
            if s:sub(i, i) ~= '"' then error("expected a key at " .. i) end
            local k
            k, i = decodeString(s, i)
            i = skip(s, i)
            if s:sub(i, i) ~= ":" then error("expected ':' at " .. i) end
            local v
            v, i = decodeValue(s, i + 1)
            obj[k] = v
            i = skip(s, i)
            local d = s:sub(i, i)
            if d == "}" then return obj, i + 1 end
            if d ~= "," then error("expected ',' or '}' at " .. i) end
            i = skip(s, i + 1)
        end
    elseif c == "[" then
        local arr = {}
        i = skip(s, i + 1)
        if s:sub(i, i) == "]" then return arr, i + 1 end
        while true do
            local v
            v, i = decodeValue(s, i)
            arr[#arr + 1] = v
            i = skip(s, i)
            local d = s:sub(i, i)
            if d == "]" then return arr, i + 1 end
            if d ~= "," then error("expected ',' or ']' at " .. i) end
            i = i + 1
        end
    elseif c == '"' then
        return decodeString(s, i)
    elseif s:sub(i, i + 3) == "true" then
        return true, i + 4
    elseif s:sub(i, i + 4) == "false" then
        return false, i + 5
    elseif s:sub(i, i + 3) == "null" then
        return Json.null, i + 4
    else
        local num = s:match("^-?%d+%.?%d*[eE]?[-+]?%d*", i)
        if num == nil or num == "" then error("unexpected character at " .. i) end
        return tonumber(num), i + #num
    end
end

function Json.decode(text)
    if type(text) ~= "string" then return nil, "Json.decode: not a string" end
    local ok, v, i = pcall(decodeValue, text, 1)
    if not ok then return nil, "Json.decode: " .. tostring(v) end
    if skip(text, i) <= #text then return nil, "Json.decode: trailing text at " .. i end
    return v
end

return Json
