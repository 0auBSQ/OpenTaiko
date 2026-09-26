---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- backgrounds.lua — the song-select background.
--
-- A genre can ship a background set in Textures/Backgrounds/<Genre>/: its images plus background.json,
-- which lists layers drawn bottom to top. Genres without one get the default: bg1.png scrolling.
-- The selected node's genre picks the set; changing it crossfades the new set in over the old ones
-- (a stack, so fast scrolling never pops). Sets seen on the visible page are prefetched so their images
-- are decoded before they come into view. Hosts that draw their own background (AI battle, training)
-- pass "shared" to activate and get the SHARED "background" texture scrolling instead.
--
-- background.json:
--   { "layers": [ <layer>, ... ] }
-- Layer types (every field optional unless noted; colours "RRGGBB"; [a, b] = a random range or a
-- min/max for a pulse):
--   image     file*, scroll (px/s, positive = leftwards, images narrower than the screen tile),
--             y, opacity, blend
--   particles sprite ("dot" = _common/dot.png or a file), count, size [min,max] px, color,
--             opacity [min,max], speed [min,max] px/s, direction (up/down/left/right), sway px,
--             sway_ms, twinkle_ms (0 = none), blend, area [x0,y0,x1,y1]
--   sprites   files* [..], count, y [min,max], speed [min,max] px/s (negative = leftwards),
--             scale [min,max], opacity [min,max], blend, margin px
--   rays      x, y (origin), angles [..] deg (0 = straight down, positive = clockwise), length,
--             width, color, opacity [min,max], pulse_ms, sway_deg, sway_ms, blend
--   glow      color, opacity [min,max], pulse_ms, blend — a full-screen tint
-- Files resolve in the set's folder first, then in Textures/Backgrounds/_common/.

local CFG = require("sscore_config")

local M = {}
local G
local COL_WHITE = COLOR:CreateColorFromHex("FFFFFFFF")

local FADE_MS   = CFG.num("background.fade_ms", 450)         -- crossfade between sets
local CACHE_MAX = CFG.num("background.cache_sets", 6)        -- decoded sets kept (least recently used go)
local STACK_MAX = CFG.num("background.stack_max", 4)         -- sets blended at once while scrolling fast
local SCREEN_W, SCREEN_H = 1920, 1080

local DEFAULT_DEF = { layers = { { type = "image", file = "Textures/bg1.png", scroll = 40, y = 0, opacity = { 1, 1 }, blend = "normal" } } }

local BASE   = "Textures/Backgrounds/"
local COMMON = BASE .. "_common/"

-- ── Textures ─────────────────────────────────────────────────────────────────

local texCache = {}      -- path -> LuaTexture (created async: draws nothing until its pixels are uploaded)

local function resolvePath(dir, file)
    if file == nil then return nil end
    if file:sub(1, 9) == "Textures/" then return file end
    if dir ~= nil and TEXTURE:Exists(dir .. file) then return dir .. file end
    if TEXTURE:Exists(COMMON .. file) then return COMMON .. file end
    return dir ~= nil and (dir .. file) or file
end

local function getTex(path)
    if path == nil then return nil end
    local t = texCache[path]
    if t == nil then
        t = TEXTURE:CreateTexture(path)
        texCache[path] = t
    end
    return t
end

-- ── Definitions ──────────────────────────────────────────────────────────────

local defCache = {}      -- genre -> { def = parsed table, dir = set folder } or false when the genre has none

local function jget(node, key) return JSONLOADER:JsonGet(node, key) end

local function jnum(node, key, default)
    local v = jget(node, key)
    return type(v) == "number" and v or default
end
local function jstr(node, key, default)
    local v = jget(node, key)
    return type(v) == "string" and v or default
end
local function jrange(node, key, default)
    local v = jget(node, key)
    if type(v) == "number" then return { v, v } end
    if v == nil then return default end
    local a, b = jget(v, 1), jget(v, 2)
    if type(a) == "number" and type(b) == "number" then return { a, b } end
    if type(a) == "number" then return { a, a } end
    return default
end
local function jlist(node, key)
    local v = jget(node, key)
    if v == nil then return {} end
    local out = {}
    for i = 1, JSONLOADER:JsonCount(v) do out[i] = jget(v, i) end
    return out
end

-- one layer table from its JSON node (plain Lua tables from here on: no per-frame dictionary access)
local function parseLayer(node)
    local kind = jstr(node, "type", "image")
    local L = { type = kind, blend = jstr(node, "blend", "normal"), opacity = jrange(node, "opacity", { 1, 1 }) }
    if kind == "image" then
        L.file = jstr(node, "file", nil); L.scroll = jnum(node, "scroll", 0); L.y = jnum(node, "y", 0)
    elseif kind == "particles" then
        L.sprite = jstr(node, "sprite", "dot.png"); if L.sprite == "dot" then L.sprite = "dot.png" end
        L.count = math.floor(jnum(node, "count", 30)); L.size = jrange(node, "size", { 8, 24 })
        L.color = jstr(node, "color", "FFFFFF"); L.speed = jrange(node, "speed", { 5, 20 })
        L.direction = jstr(node, "direction", "up"); L.sway = jnum(node, "sway", 0); L.swayMs = jnum(node, "sway_ms", 4000)
        L.twinkleMs = jnum(node, "twinkle_ms", 0)
        local area = jlist(node, "area")
        L.area = (#area == 4) and area or { -60, -60, SCREEN_W + 60, SCREEN_H + 60 }
    elseif kind == "sprites" then
        L.files = jlist(node, "files"); L.count = math.floor(jnum(node, "count", 4))
        L.y = jrange(node, "y", { 0, SCREEN_H / 2 }); L.speed = jrange(node, "speed", { 10, 30 })
        L.scale = jrange(node, "scale", { 1, 1 }); L.margin = jnum(node, "margin", 0)
    elseif kind == "rays" then
        L.x = jnum(node, "x", SCREEN_W); L.y = jnum(node, "y", 0); L.angles = jlist(node, "angles")
        if #L.angles == 0 then L.angles = { 30 } end
        L.length = jnum(node, "length", 1.5); L.width = jnum(node, "width", 1)
        L.color = jstr(node, "color", "FFFFFF"); L.pulseMs = jnum(node, "pulse_ms", 5000)
        L.swayDeg = jnum(node, "sway_deg", 0); L.swayMs = jnum(node, "sway_ms", 9000)
        if L.blend == "normal" then L.blend = "add" end
    elseif kind == "glow" then
        L.color = jstr(node, "color", "FFFFFF"); L.pulseMs = jnum(node, "pulse_ms", 5000)
        if L.blend == "normal" then L.blend = "add" end
    end
    return L
end

local function loadDef(genre)
    if genre == nil or genre == "" then return nil end
    local cached = defCache[genre]
    if cached ~= nil then return cached or nil end
    local dir = BASE .. genre .. "/"
    local ok, entry = pcall(function()
        if not TEXTURE:Exists(dir .. "background.json") then return false end
        local doc = JSONLOADER:JsonParseFileAny(dir .. "background.json")
        if doc == nil then return false end
        local layers = {}
        for _, node in ipairs(jlist(doc, "layers")) do layers[#layers + 1] = parseLayer(node) end
        if #layers == 0 then return false end
        return { def = { layers = layers }, dir = dir }
    end)
    if not ok then debugLog("backgrounds: bad definition for '" .. tostring(genre) .. "': " .. tostring(entry)); entry = false end
    defCache[genre] = entry
    return entry or nil
end

-- ── Instances (a set with its live state) ────────────────────────────────────

local function hexColor(hex)
    if #hex == 6 then hex = "FF" .. hex end
    return COLOR:CreateColorFromHex(hex)
end
local function rnd(range) return range[1] + (range[2] - range[1]) * math.random() end

local function spawnParticle(L, p, fresh)
    local a = L.area
    p.x = a[1] + (a[3] - a[1]) * math.random()
    p.y = a[2] + (a[4] - a[2]) * math.random()
    if not fresh then
        -- re-enter from the edge the drift comes from
        if L.direction == "up" then p.y = a[4] elseif L.direction == "down" then p.y = a[2]
        elseif L.direction == "left" then p.x = a[3] elseif L.direction == "right" then p.x = a[1] end
    end
    p.size = rnd(L.size); p.speed = rnd(L.speed); p.op = rnd(L.opacity)
    p.phase = math.random() * 2 * math.pi
end

local function spawnSprite(L, s, fresh, texW)
    s.scale = rnd(L.scale); s.speed = rnd(L.speed); s.op = rnd(L.opacity); s.y = rnd(L.y)
    local w = (texW or 400) * s.scale
    if fresh then s.x = -w + (SCREEN_W + 2 * w) * math.random()
    elseif s.speed >= 0 then s.x = -w - L.margin
    else s.x = SCREEN_W + w + L.margin end
end

local function newInstance(genre, entry)
    local def, dir = entry and entry.def or DEFAULT_DEF, entry and entry.dir or nil
    local inst = { genre = genre, layers = {}, dir = dir }
    for _, L in ipairs(def.layers) do
        local S = { def = L, t = 0 }
        if L.type == "image" then
            S.tex = getTex(resolvePath(dir, L.file)); S.offset = 0
        elseif L.type == "particles" then
            S.tex = getTex(resolvePath(dir, L.sprite)); S.color = hexColor(L.color); S.items = {}
            for i = 1, L.count do S.items[i] = {}; spawnParticle(L, S.items[i], true) end
        elseif L.type == "sprites" then
            S.texes = {}
            for i, f in ipairs(L.files) do S.texes[i] = getTex(resolvePath(dir, f)) end
            S.items = {}
            for i = 1, L.count do
                local s = { tex = S.texes[((i - 1) % math.max(1, #S.texes)) + 1] }
                spawnSprite(L, s, true, s.tex and s.tex.Width or 400); S.items[i] = s
            end
        elseif L.type == "rays" then
            S.tex = getTex(COMMON .. "ray.png"); S.color = hexColor(L.color)
        elseif L.type == "glow" then
            S.tex = getTex(COMMON .. "white.png"); S.color = hexColor(L.color)
        end
        inst.layers[#inst.layers + 1] = S
    end
    return inst
end

-- ── State ────────────────────────────────────────────────────────────────────

local stack = {}         -- entries { inst, alpha }, oldest first; the last one is the current set fading in
                         -- (an instance can appear twice: as the opaque base and again on top, fading in)
local instances = {}     -- genre key -> instance (kept for reuse, capped to CACHE_MAX)
local lru = {}           -- genre keys, most recently selected last
local mode = "genre"     -- or "shared" (host-provided SHARED "background")
local lastMs = nil

local function keyOf(genre)
    if genre == nil or genre == "" then return "\0default" end
    return genre
end

local function touch(key)
    for i, k in ipairs(lru) do if k == key then table.remove(lru, i); break end end
    lru[#lru + 1] = key
    -- evict: instances not on the stack, least recently used first
    while #lru > CACHE_MAX do
        local victim = nil
        for i, k in ipairs(lru) do
            local onStack = false
            for _, e in ipairs(stack) do if e.inst.genre == k then onStack = true; break end end
            if not onStack then victim = i; break end
        end
        if victim == nil then break end
        local k = table.remove(lru, victim)
        local inst = instances[k]
        instances[k] = nil
        if inst ~= nil and inst.dir ~= nil then
            -- the set's own images go with it (common sprites and the default stay cached)
            for path, tex in pairs(texCache) do
                if path:sub(1, #inst.dir) == inst.dir then tex:Dispose(); texCache[path] = nil end
            end
        end
    end
end

local function instanceFor(genre)
    local key = keyOf(genre)
    local inst = instances[key]
    if inst == nil then
        local entry = loadDef(genre)
        if entry == nil then
            -- no set: share the default instance
            key = "\0default"; inst = instances[key]
            if inst == nil then inst = newInstance(key, nil); instances[key] = inst end
        else
            inst = newInstance(key, entry); instances[key] = inst
        end
    end
    touch(key)
    return inst
end

function M.init(g) G = g end

-- the default set (bg1.png) is created at onStart so its image loads with the activity
function M.loadDefault() instanceFor(nil) end

-- warm the caches for a genre that may come into view (definition parsed, images decoding)
function M.prefetch(genre)
    if mode ~= "genre" then return end
    local entry = loadDef(genre)
    if entry == nil then return end
    for _, L in ipairs(entry.def.layers) do
        if L.type == "image" then getTex(resolvePath(entry.dir, L.file))
        elseif L.type == "particles" then getTex(resolvePath(entry.dir, L.sprite))
        elseif L.type == "sprites" then for _, f in ipairs(L.files) do getTex(resolvePath(entry.dir, f)) end
        elseif L.type == "rays" then getTex(COMMON .. "ray.png")
        elseif L.type == "glow" then getTex(COMMON .. "white.png") end
    end
end

-- the selected node's genre changed: make its set current (fading in over whatever is showing)
function M.select(genre)
    if mode ~= "genre" then return end
    local inst = instanceFor(genre)
    local top = stack[#stack]
    if top ~= nil and top.inst == inst then return end
    if #stack == 0 then
        -- nothing showing yet: the default (loaded at onStart) goes under, so a set still decoding
        -- fades in over it instead of over black
        local base = instanceFor(nil)
        if base ~= inst then stack[1] = { inst = base, alpha = 1 } else stack[1] = { inst = inst, alpha = 1 }; return end
    end
    -- entries below the top keep their opacity; only the top fades in, then the stack collapses to it
    stack[#stack + 1] = { inst = inst, alpha = 0 }
    -- scrolling faster than the fade: drop the faintest entry between the base and the top
    while #stack > STACK_MAX do
        local victim, low = nil, 2
        for i = 2, #stack - 1 do
            if victim == nil or stack[i].alpha < low then victim, low = i, stack[i].alpha end
        end
        if victim == nil then break end
        table.remove(stack, victim)
    end
end

-- a set is ready to show once every image layer's pixels are uploaded (a missing file is an empty handle,
-- nothing to wait for)
local function isReady(inst)
    for _, S in ipairs(inst.layers) do
        if S.def.type == "image" and S.tex ~= nil and S.tex.Loaded and not S.tex.Ready then return false end
    end
    return true
end

-- "genre" (default) or "shared": the host's SHARED "background" texture, scrolling
function M.setMode(m)
    mode = (m == "shared") and "shared" or "genre"
    lastMs = nil
end

function M.reset()
    stack = {}
    lastMs = nil
end

-- ── Update ───────────────────────────────────────────────────────────────────

local function updateInstance(inst, dt)
    local ds = dt / 1000
    for _, S in ipairs(inst.layers) do
        local L = S.def
        S.t = S.t + dt
        if L.type == "image" then
            local w = (S.tex ~= nil and S.tex.Width > 0) and S.tex.Width or SCREEN_W
            S.offset = (S.offset + L.scroll * ds) % w
        elseif L.type == "particles" then
            local a = L.area
            for _, p in ipairs(S.items) do
                local d = p.speed * ds
                if L.direction == "up" then p.y = p.y - d elseif L.direction == "down" then p.y = p.y + d
                elseif L.direction == "left" then p.x = p.x - d else p.x = p.x + d end
                if p.x < a[1] or p.x > a[3] or p.y < a[2] or p.y > a[4] then spawnParticle(L, p, false) end
            end
        elseif L.type == "sprites" then
            for _, s in ipairs(S.items) do
                s.x = s.x + s.speed * ds
                local w = (s.tex ~= nil and s.tex.Width > 0 and s.tex.Width or 400) * s.scale
                if (s.speed >= 0 and s.x > SCREEN_W + w + L.margin) or (s.speed < 0 and s.x < -w - L.margin) then
                    spawnSprite(L, s, false, s.tex and s.tex.Width or 400)
                end
            end
        end
    end
end

function M.update()
    local now = G.nowMs or 0
    local dt = (lastMs ~= nil) and (now - lastMs) or 0
    lastMs = now
    if dt < 0 or dt > 200 then dt = 16 end
    if mode ~= "genre" or #stack == 0 then return end
    local n = #stack
    local top = stack[n]
    -- the crossfade waits for the incoming set's images (prefetched, so usually already there)
    if isReady(top.inst) then top.alpha = math.min(1, top.alpha + dt / FADE_MS) end
    local seen = {}
    for _, e in ipairs(stack) do
        if not seen[e.inst] then seen[e.inst] = true; updateInstance(e.inst, dt) end
    end
    -- the current set is opaque: everything under it is invisible, drop it
    if top.alpha >= 1 and n > 1 then
        stack = { top }
    else
        for i = n - 1, 1, -1 do if stack[i].alpha <= 0 then table.remove(stack, i) end end
    end
end

-- ── Draw ─────────────────────────────────────────────────────────────────────

local function pulse(S, L)
    local rng = L.opacity
    if L.pulseMs <= 0 then return rng[2] end
    local k = 0.5 + 0.5 * math.sin(2 * math.pi * S.t / L.pulseMs)
    return rng[1] + (rng[2] - rng[1]) * k
end

local function drawImage(S, L, alpha)
    local tex = S.tex
    if tex == nil or tex.Width <= 0 then return end
    tex:SetOpacity(alpha * L.opacity[2])
    tex:SetBlendMode(L.blend)
    local w = tex.Width
    local x = -S.offset
    while x < SCREEN_W do
        tex:Draw(x, L.y)
        x = x + w
        if L.scroll == 0 and w >= SCREEN_W then break end
    end
    tex:SetBlendMode("normal")
    tex:SetOpacity(1)
end

local function drawParticles(S, L, alpha)
    local tex = S.tex
    if tex == nil or tex.Width <= 0 then return end
    tex:SetColor(S.color)
    tex:SetBlendMode(L.blend)
    local tw = tex.Width
    for _, p in ipairs(S.items) do
        local op = p.op
        if L.twinkleMs > 0 then op = op * (0.55 + 0.45 * math.sin(2 * math.pi * S.t / L.twinkleMs + p.phase)) end
        local x = p.x
        if L.sway > 0 then x = x + L.sway * math.sin(2 * math.pi * S.t / L.swayMs + p.phase) end
        local sc = p.size / tw
        tex:SetScale(sc, sc)
        tex:SetOpacity(alpha * op)
        tex:DrawAtAnchor(x, p.y, "center")
    end
    tex:SetScale(1, 1)
    tex:SetBlendMode("normal")
    tex:SetColor(COL_WHITE)
    tex:SetOpacity(1)
end

local function drawSprites(S, L, alpha)
    for _, s in ipairs(S.items) do
        local tex = s.tex
        if tex ~= nil and tex.Width > 0 then
            tex:SetScale(s.scale, s.scale)
            tex:SetBlendMode(L.blend)
            tex:SetOpacity(alpha * s.op)
            tex:DrawAtAnchor(s.x, s.y, "center")
            tex:SetOpacity(1)
            tex:SetBlendMode("normal")
            tex:SetScale(1, 1)
        end
    end
end

local function drawRays(S, L, alpha)
    local tex = S.tex
    if tex == nil or tex.Width <= 0 then return end
    local op = pulse(S, L)
    local sway = (L.swayDeg > 0) and (L.swayDeg * math.sin(2 * math.pi * S.t / L.swayMs)) or 0
    local h = tex.Height * L.length
    tex:SetColor(S.color)
    tex:SetBlendMode(L.blend)
    tex:SetScale(L.width, L.length)
    for i, ang in ipairs(L.angles) do
        local a = ang + sway * ((i % 2 == 0) and -1 or 1)
        local rad = math.rad(a)
        -- the beam hangs from its origin: its centre sits half a length along the beam direction
        local cx = L.x + math.sin(rad) * h / 2 * -1
        local cy = L.y + math.cos(rad) * h / 2
        tex:SetRotation(-a)   -- SetRotation is counter-clockwise
        tex:SetOpacity(alpha * op * (0.8 + 0.2 * math.sin(S.t / 700 + i)))
        tex:DrawAtAnchor(cx, cy, "center")
    end
    tex:SetRotation(0)
    tex:SetScale(1, 1)
    tex:SetBlendMode("normal")
    tex:SetColor(COL_WHITE)
    tex:SetOpacity(1)
end

local function drawGlow(S, L, alpha)
    local tex = S.tex
    if tex == nil or tex.Width <= 0 then return end
    tex:SetColor(S.color)
    tex:SetBlendMode(L.blend)
    tex:SetScale(SCREEN_W / tex.Width, SCREEN_H / tex.Height)
    tex:SetOpacity(alpha * pulse(S, L))
    tex:DrawAtAnchor(0, 0, "topleft")
    tex:SetScale(1, 1)
    tex:SetBlendMode("normal")
    tex:SetColor(COL_WHITE)
    tex:SetOpacity(1)
end

local function drawInstance(inst, alpha)
    if alpha <= 0 then return end
    for _, S in ipairs(inst.layers) do
        local L = S.def
        if L.type == "image" then drawImage(S, L, alpha)
        elseif L.type == "particles" then drawParticles(S, L, alpha)
        elseif L.type == "sprites" then drawSprites(S, L, alpha)
        elseif L.type == "rays" then drawRays(S, L, alpha)
        elseif L.type == "glow" then drawGlow(S, L, alpha) end
    end
end

local function drawShared()
    local tex = SHARED:GetSharedTexture("background")
    if tex == nil or tex.Width <= 0 then return end
    tex:Draw(-G.backgroundScrollX, 0)
    tex:Draw(-G.backgroundScrollX + SCREEN_W, 0)
end

function M.draw()
    if mode ~= "genre" then drawShared(); return end
    if #stack == 0 then M.select(nil) end
    for _, e in ipairs(stack) do drawInstance(e.inst, e.alpha) end
end

-- the background's image layers repainted inside a screen rectangle (the replay list's clip band);
-- the effect layers are left out, they are too small to matter under the strip
function M.drawBand(bx, by, bw, bh)
    if mode ~= "genre" then
        local tex = SHARED:GetSharedTexture("background")
        if tex == nil or tex.Width <= 0 then return end
        for k = 0, 1 do
            local tileX = -G.backgroundScrollX + SCREEN_W * k
            local x0, x1 = math.max(bx, tileX), math.min(bx + bw, tileX + SCREEN_W)
            if x1 > x0 then tex:DrawRect(math.floor(x0), by, math.floor(x0 - tileX), by, math.ceil(x1 - x0), bh) end
        end
        return
    end
    local top = stack[#stack]
    if top == nil then return end
    for _, S in ipairs(top.inst.layers) do
        local L = S.def
        if L.type == "image" and S.tex ~= nil and S.tex.Width > 0 then
            local tex, w, h = S.tex, S.tex.Width, S.tex.Height
            local x = -S.offset
            while x < SCREEN_W do
                local x0, x1 = math.max(bx, x), math.min(bx + bw, x + w)
                local y0, y1 = math.max(by, L.y), math.min(by + bh, L.y + h)
                if x1 > x0 and y1 > y0 then
                    tex:DrawRect(math.floor(x0), math.floor(y0), math.floor(x0 - x), math.floor(y0 - L.y), math.ceil(x1 - x0), math.ceil(y1 - y0))
                end
                x = x + w
                if L.scroll == 0 and w >= SCREEN_W then break end
            end
        end
    end
end

-- headless harness hooks
function M._top() local e = stack[#stack]; return e and { alpha = e.alpha, genre = e.inst.genre } or nil end
function M._depth() return #stack end
function M._textures() return texCache end

function M.dispose()
    for _, tex in pairs(texCache) do tex:Dispose() end
    texCache = {}; instances = {}; stack = {}; lru = {}; defCache = {}
end

return M
