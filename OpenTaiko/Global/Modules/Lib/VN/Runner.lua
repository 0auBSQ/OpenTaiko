---@diagnostic disable: undefined-global
-- VN.Runner: walks a visual-novel line script, no drawing
-- stepped by VN.Scene or a stage; prose lines go on a VN.Dialogue box

local Runner = {}
Runner.__index = Runner

Runner.ORDER = { "outcome", "fade", "bg", "enter", "leave", "shake", "sfx", "flag", "text", "expr" }
Runner.PLAYER_TAG = "<player name>"

-- ==== static helpers ====

-- a line plays unless its guard says otherwise
function Runner.passes(line, ctx)
    if line.when == nil then return true end
    local ok, res = pcall(line.when, ctx)
    return ok and res == true
end

-- inserts the lines of `insert` right after lines[idx]
function Runner.splice(lines, idx, insert)
    if insert == nil then return end
    for i = #insert, 1, -1 do table.insert(lines, idx + 1, insert[i]) end
end

local function translate(s, tr)
    if s == nil then return nil end
    if tr == nil then return s end
    return tr(s)
end
Runner.translate = translate

local function speakerName(o, who, line, ctx)
    local n = o.speakerName
    if type(n) == "function" then return n(who, line, ctx) end
    if type(n) == "table" then return n[who] end
    return n
end

-- dialogue node of `line`, translated (choice values never), player tag substituted
-- carry: previous prose line, shown by a bare-choice line
-- speaker = who (even under a masking name), voice = raw name (per-speaker sounds stay stable)
function Runner.node(line, ctx, o, carry)
    o = o or {}
    local src, carried = line, false
    if carry ~= nil and line.choices ~= nil and (line.text == nil or line.text == "") then
        src, carried = carry, true
    end
    local name = src.name
    if name == nil and src.who ~= nil then name = speakerName(o, src.who, src, ctx) end
    local choices = line.choices
    if choices ~= nil then
        local t = {}
        for i, c in ipairs(choices) do t[i] = { label = translate(c.label, o.tr), value = c.value } end
        choices = t
    end
    local text = translate(src.text, o.tr) or ""
    local player = o.playerName
    if type(player) == "function" then player = player(ctx) end
    if player == nil and ctx ~= nil then player = ctx.playerName end
    if player ~= nil then
        -- function replacement: a % in the name is no capture reference
        text = text:gsub(o.playerTag or Runner.PLAYER_TAG, function() return player end)
    end
    return { name = translate(name, o.tr), speaker = src.who, voice = name, text = text, choices = choices }, carried
end

-- starts `line` on a VN.Dialogue box; a carried line shows fully revealed (no retype, no blips)
function Runner.start(dlg, line, ctx, o, carry)
    local node, carried = Runner.node(line, ctx, o, carry)
    dlg:start({ node })
    if carried then dlg.revealed = dlg.total end
    return node, carried
end

-- records the value picked on lines[idx] and splices its branch in; false when nothing was picked
function Runner.resolveChoice(lines, idx, ctx, value)
    local line = lines[idx]
    if line == nil or line.choices == nil or value == nil or value == true then return false end
    ctx.choice = value
    ctx.choices = ctx.choices or {}
    ctx.choices[value] = true
    Runner.splice(lines, idx, line.branch and line.branch[value])
    return true
end

-- last option per pool, so the next pick differs
local _lastPick = setmetatable({}, { __mode = "k" })

-- plays one option of lines[idx].pick, splicing its lines in after it
-- forced: that option instead of a draw (resume); returns its index
function Runner.resolvePick(lines, idx, random, forced)
    local pool = lines[idx] and lines[idx].pick
    if type(pool) ~= "table" or #pool == 0 then return nil end
    local chosen = nil
    if forced ~= nil and pool[forced] ~= nil then
        chosen = forced
    else
        local skip = (#pool > 1) and _lastPick[pool] or nil
        local total = 0
        for i, o in ipairs(pool) do
            if i ~= skip then total = total + (o.weight or 1) end
        end
        local r = (random or math.random)() * total
        for i, o in ipairs(pool) do
            if i ~= skip then
                chosen = i
                r = r - (o.weight or 1)
                if r < 0 then break end
            end
        end
    end
    if chosen == nil then return nil end
    _lastPick[pool] = chosen
    Runner.splice(lines, idx, pool[chosen].lines)
    return chosen
end

-- ==== runner ====

-- line fields, all optional:
--   text     prose with VN.Dialogue inline tags; "" or none with choices = bare choice (keeps the last prose)
--   who      speaker id; name = shown name, wins over who's
--   expr     speaker expression
--   choices  { { label =, value = }, ... }; branch = { [value] = { lines } } spliced in after the choice
--   when     fn(ctx) -> true when the line plays; an erroring guard counts as false
--   pick     { { lines = { ... }, weight = n }, ... }: one by weight (default 1), never the same twice in a row
--   other keys: stage directions the caller registers
-- "<player name>" in text becomes ctx.playerName
-- opts:
--   lines = { ... }: the script, walked as a copy (branches get spliced in)
--   ctx: read by `when` guards; a choice sets ctx.choice and ctx.choices[value]
--   tr = fn(s) -> s: translates shown strings (none: as written)
--   speakerName: name for a line with `who` and no `name`: string, { [who] = name } or fn(who, line, ctx)
--   playerTag = "<player name>", playerName = string or fn(ctx) (default ctx.playerName)
--   directions = { [key] = fn(line, runner) -> verdict }
--   order = { key, ... }: direction order (default Runner.ORDER, then the rest sorted)
--   onLine = fn(line, runner) -> verdict: every played line, before its directions
--   onChoose = fn(line, value, runner): a choice was taken (also on resume)
--   random = fn() -> [0, 1) for picks (default math.random)
function Runner.new(opts)
    opts = opts or {}
    local self = setmetatable({}, Runner)
    self.opts = opts
    self.ctx = opts.ctx or {}
    self.directions = opts.directions or {}
    local order, seen = {}, {}
    for _, k in ipairs(opts.order or Runner.ORDER) do order[#order + 1] = k ; seen[k] = true end
    if opts.order == nil then
        local rest = {}
        for k in pairs(self.directions) do if not seen[k] then rest[#rest + 1] = k end end
        table.sort(rest)
        for _, k in ipairs(rest) do order[#order + 1] = k end
    end
    self.order = order
    self:load(opts.lines)
    return self
end

-- starts over on another script (or the same one)
function Runner:load(lines)
    self.lines = {}
    for i, l in ipairs(lines or {}) do self.lines[i] = l end
    self.idx = 0
    self.lastProse = nil
    self.picks, self.rolls = {}, {}
    self._forcedRolls = nil
end

function Runner:current() return self.lines[self.idx] end

local function isProse(line) return line.text ~= nil or line.choices ~= nil end

-- first direction the line carries that handles it; verdicts:
--   false: not handled, next direction tried; nil or true: the walk goes on
--   "hold": the walk stops until the caller steps; "end": script over
-- "text" unless registered: a line with text or choices stops as "line" (present, then choose)
function Runner:direct(line)
    for _, k in ipairs(self.order) do
        if (k == "text" and isProse(line)) or (k ~= "text" and line[k] ~= nil) then
            local fn = self.directions[k]
            local v
            if fn ~= nil then v = fn(line, self)
            elseif k == "text" then v = "line"
            else v = false end
            if v ~= false then return v end
        end
    end
    return nil
end

function Runner:_roll()
    local forced = nil
    if self._forcedRolls ~= nil then
        forced = self._forcedRolls[#self.rolls + 1]
    end
    local chosen = Runner.resolvePick(self.lines, self.idx, self.opts.random, forced)
    if chosen ~= nil then self.rolls[#self.rolls + 1] = chosen end
end

-- walks to the next stop: "line" (prose to present), "hold" or "end"; returns verdict, line
function Runner:step()
    while true do
        self.idx = self.idx + 1
        local line = self.lines[self.idx]
        if line == nil then return "end", nil end
        if Runner.passes(line, self.ctx) then
            if line.pick ~= nil then self:_roll() end
            local v = nil
            if self.opts.onLine ~= nil then v = self.opts.onLine(line, self) end
            if v == nil or v == false or v == true then v = self:direct(line) end
            if v == "line" or v == "hold" or v == "end" then return v, line end
        end
    end
end

-- the node options this runner shows lines with
function Runner:nodeOptions()
    local o = self.opts
    return { tr = o.tr, speakerName = o.speakerName, playerTag = o.playerTag, playerName = o.playerName }
end

-- puts the current line on the box; a bare choice carries the previous prose line
function Runner:present(dlg, line)
    line = line or self.lines[self.idx]
    local node, carried = Runner.start(dlg, line, self.ctx, self:nodeOptions(), self.lastProse)
    if line.text ~= nil and line.text ~= "" then self.lastProse = line end
    return node, carried
end

-- the box's result for the current line: a choice value is recorded and its branch spliced in
function Runner:choose(value)
    if Runner.resolveChoice(self.lines, self.idx, self.ctx, value) then
        self.picks[#self.picks + 1] = value
        if self.opts.onChoose ~= nil then self.opts.onChoose(self.lines[self.idx], value, self) end
        return true
    end
    return false
end

-- fn(line) of the first later line it answers for (true/false; nil goes on); `otherwise` at the end
function Runner:ahead(fn, otherwise)
    for i = self.idx + 1, #self.lines do
        local l = self.lines[i]
        if Runner.passes(l, self.ctx) then
            local v = fn(l)
            if v ~= nil then return v end
        end
    end
    return otherwise
end

-- { idx, picks, rolls }: the line on screen, choice values taken, pick options drawn
function Runner:bookmark()
    local p, r = {}, {}
    for i, v in ipairs(self.picks) do p[i] = v end
    for i, v in ipairs(self.rolls) do r[i] = v end
    return { idx = self.idx, picks = p, rolls = r }
end

-- walks silently up to mark's line, re-taking its choices and picks
-- replay = { [key] = fn(line, runner) } re-applies stateful directions; the next step() shows the line
function Runner:resume(mark, replay)
    if type(mark) ~= "table" or type(mark.idx) ~= "number" then return end
    replay = replay or {}
    -- replayed directions, in walk order
    local keys, seen = {}, {}
    for _, k in ipairs(self.order) do if replay[k] ~= nil then keys[#keys + 1] = k ; seen[k] = true end end
    local rest = {}
    for k in pairs(replay) do if not seen[k] then rest[#rest + 1] = k end end
    table.sort(rest)
    for _, k in ipairs(rest) do keys[#keys + 1] = k end
    self._forcedRolls = mark.rolls
    local pi = 1
    while self.idx < mark.idx - 1 do
        self.idx = self.idx + 1
        local line = self.lines[self.idx]
        if line == nil then break end
        if Runner.passes(line, self.ctx) then
            if line.pick ~= nil then self:_roll() end
            for _, k in ipairs(keys) do
                if line[k] ~= nil then replay[k](line, self) end
            end
            if line.text ~= nil and line.text ~= "" then self.lastProse = line end
            if line.choices ~= nil then
                local v = mark.picks and mark.picks[pi] or nil
                pi = pi + 1
                if v ~= nil then self:choose(v) end
            end
        end
    end
    self._forcedRolls = nil
end

return Runner
