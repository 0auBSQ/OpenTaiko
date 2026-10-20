---@diagnostic disable: undefined-global, undefined-field
-- Online.Protocol: the online lobby's wire messages. Each one is a JSON string sent on a NET channel with
-- NET:Broadcast or NET:SendTo. Every skin's lobby speaks these, so a room is joinable from any skin.
--
-- Channels: "song" (the room's song, from the host role), "diff", "mod", "ready", "have" (the song is on
-- this machine), "watch" (back on the results), "go" (start), "result" (a finished play).

local floor = math.floor

local M = {}

M.ROOM_ID = "onlinelobby"   -- the stage id in the room code; every skin's lobby uses it
M.MAXP = 5

-- a JSON string literal
function M.jstr(s)
    return '"' .. tostring(s):gsub('[%c\\"]', function(ch)
        local m = { ['"'] = '\\"', ['\\'] = '\\\\', ['\n'] = '\\n', ['\r'] = '\\r', ['\t'] = '\\t' }
        return m[ch] or string.format('\\u%04x', string.byte(ch))
    end) .. '"'
end
local jstr = M.jstr

local function jbool(b) return b and "true" or "false" end

-- the mods of a player: random, stealth, justice, timing zone, scroll speed, special mod
-- (none / normal timing / x1 scroll / no special mod)
function M.defMods() return { r = 0, st = 0, ju = 0, tz = 2, ss = 9, sm = 0 } end

function M.modJson(m) return string.format('{"r":%d,"st":%d,"ju":%d,"tz":%d,"ss":%d,"sm":%d}', m.r or 0, m.st or 0, m.ju or 0, m.tz or 2, m.ss or 9, m.sm or 0) end

-- the room's song {uid, title, diff, speed, dyn}
function M.songJson(s)
    return string.format('{"u":%s,"t":%s,"d":%d,"s":%d,"b":%d}', jstr(s.uid), jstr(s.title or "?"), s.diff or 0, s.speed or 20, s.dyn or 0)
end

function M.diffJson(d) return string.format('{"d":%d}', d) end
function M.readyJson(r) return string.format('{"r":%s}', jbool(r)) end
function M.haveJson(h) return string.format('{"h":%s}', jbool(h)) end
function M.watchJson(w) return string.format('{"w":%s}', jbool(w)) end
M.GO = "{}"

-- a result row { name, sc, gr, gd, ms, co, cl, fc, pf, ac, ab }
function M.resultJson(r)
    return string.format('{"n":%s,"sc":%d,"gr":%d,"gd":%d,"ms":%d,"co":%d,"cl":%s,"fc":%s,"pf":%s,"ac":%.2f,"ab":%s}',
        jstr(r.name), r.sc, r.gr, r.gd, r.ms, r.co, jbool(r.cl), jbool(r.fc), jbool(r.pf), r.ac, jbool(r.ab))
end

-- decoders take the payload parsed by JSONLOADER:JsonParseStringAny
local function get(s, k) return JSONLOADER:JsonGet(s, k) end

function M.decodeSong(s)
    return { uid = get(s, "u"), title = get(s, "t"),
             diff = floor(get(s, "d") or 0), speed = floor(get(s, "s") or 20), dyn = floor(get(s, "b") or 0) }
end

function M.decodeDiff(s) return floor(get(s, "d") or 1) end

function M.decodeMods(s)
    return { r = floor(get(s, "r") or 0), st = floor(get(s, "st") or 0),
        ju = floor(get(s, "ju") or 0), tz = floor(get(s, "tz") or 2), ss = floor(get(s, "ss") or 9),
        sm = floor(get(s, "sm") or 0) }
end

function M.decodeReady(s) return get(s, "r") and true or false end
-- true when the sender lacks the song
function M.decodeLack(s) return (get(s, "h") == false) end
-- true, or nil when the sender left the results
function M.decodeWatch(s) return get(s, "w") and true or nil end

function M.decodeResult(s)
    return { name = get(s, "n"), sc = floor(get(s, "sc") or 0),
        gr = floor(get(s, "gr") or 0), gd = floor(get(s, "gd") or 0), ms = floor(get(s, "ms") or 0),
        co = floor(get(s, "co") or 0), cl = get(s, "cl") and true or false,
        fc = get(s, "fc") and true or false, pf = get(s, "pf") and true or false,
        ac = get(s, "ac") or 0, ab = get(s, "ab") and true or false }
end

return M
