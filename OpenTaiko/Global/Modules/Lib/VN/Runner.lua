---@diagnostic disable: undefined-global
-- VN.Runner — walks a visual-novel line script: `when` guards, random picks, choices that splice their
-- branch in, and the stage directions the caller registers. It draws nothing: a scene player (VN.Scene)
-- or a stage steps it and puts its prose lines on a VN.Dialogue box.
--   local Runner = require("VN.Runner")
--
-- A line is a table, every field optional:
--   text     the prose, with VN.Dialogue's inline tags; "" (or none) with choices = a bare choice, which
--            keeps the previous prose line on screen, already read, under its choices
--   who      the speaker's id; name = the shown name, which wins over who's (e.g. "???")
--   expr     the speaker's expression
--   choices  { { label =, value = }, ... }; branch = { [value] = { lines } } spliced in after the choice
--   when     function(ctx) -> true when the line plays; a guard that errors counts as false
--   pick     { { lines = { ... }, weight = n }, ... }: one option drawn by weight (1 when absent), never
--            the same one twice in a row, its lines spliced in after the pick line
--   any other key is a stage direction the caller registers (enter, leave, bg, fade, sfx, flag, ...)
-- "<player name>" in a text becomes ctx.playerName.
--
-- Runner.new(opts) -> runner, opts = {
--   lines = { ... },          the script; the runner walks a copy, since branches are spliced into it
--   ctx = { },                what `when` guards read; a choice sets ctx.choice and ctx.choices[value]
--   tr = fn(s) -> s,          translator for texts, names and choice labels (none: shown as written)
--   speakerName = ...,        the name a line with `who` and no `name` shows: a string, a { [who] = name }
--                             table or fn(who, line, ctx)
--   playerTag = "<player name>", playerName = string or fn(ctx)   (default ctx.playerName)
--   directions = { [key] = fn(line, runner) -> verdict },
--   order = { key, ... },     the order directions are tried in (default Runner.ORDER, then the rest sorted)
--   onLine = fn(line, runner) -> verdict,   every line that plays, before its directions
--   onChoose = fn(line, value, runner),     a choice was taken (also when a bookmark is resumed)
--   random = fn() -> [0, 1),  for picks (math.random)
-- }
-- A direction runs when its line carries its key, and returns:
--   false       not handled: the next direction in the order is tried
--   nil, true   handled: the walk goes on to the next line
--   "hold"      the walk stops here until the caller steps again
--   "end"       the script is over
-- "text" is the prose: unless the caller registers its own, a line with text or choices stops the walk
-- as "line", for the caller to show it (runner:present(dlg)) and to report the box's result (runner:choose).
--
-- runner:bookmark() -> { idx, picks, rolls }: the line on screen, the choice values taken and the pick
-- options drawn. runner:resume(mark, replay) walks silently up to that line again, re-taking the same
-- choices and picks; replay = { [key] = fn(line, runner) } re-applies the directions that leave state
-- behind (a background, a flag). The next step() then shows the bookmarked line.

local Runner = {}
Runner.__index = Runner

Runner.ORDER = { "outcome", "fade", "bg", "enter", "leave", "shake", "sfx", "flag", "text", "expr" }
Runner.PLAYER_TAG = "<player name>"

-- ── helpers that need no runner ──────────────────────────────────────────────

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

-- The dialogue node of `line`: name (o.speakerName covers `who`), text and choice labels translated,
-- the player tag substituted. Choice values are never translated. `carry` is the previous prose line:
-- a bare-choice line shows that line's text and speaker instead of an empty box.
-- `speaker` is the true identity (who) even when the shown name masks it, and `voice` the raw
-- untranslated name, so per-speaker sounds never change with the label or the language.
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
        -- a function replacement: a % in the name is not a capture reference
        text = text:gsub(o.playerTag or Runner.PLAYER_TAG, function() return player end)
    end
    return { name = translate(name, o.tr), speaker = src.who, voice = name, text = text, choices = choices }, carried
end

-- starts `line` on a VN.Dialogue box; a carried line shows fully revealed, with no retyping, no blips
-- and its inline tags left unfired
function Runner.start(dlg, line, ctx, o, carry)
    local node, carried = Runner.node(line, ctx, o, carry)
    dlg:start({ node })
    if carried then dlg.revealed = dlg.total end
    return node, carried
end

-- records the value picked on lines[idx] and splices its branch in after it; false when nothing was picked
function Runner.resolveChoice(lines, idx, ctx, value)
    local line = lines[idx]
    if line == nil or line.choices == nil or value == nil or value == true then return false end
    ctx.choice = value
    ctx.choices = ctx.choices or {}
    ctx.choices[value] = true
    Runner.splice(lines, idx, line.branch and line.branch[value])
    return true
end

-- the option each pool gave last time, so that the next pick from it is another one
local _lastPick = setmetatable({}, { __mode = "k" })

-- plays one option of lines[idx].pick: its lines are spliced in after the pick line. `forced` takes that
-- option instead of drawing one (a resumed bookmark). Returns the option's index.
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

-- ── a runner ─────────────────────────────────────────────────────────────────

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

-- the first direction the line carries that handles it
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

-- walks to the next stop: "line" (a prose line to present), "hold" (a direction holds) or "end";
-- the line it stopped on comes second
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

-- true when fn(line) says so for the first line after the current one that it gives an answer for
-- (true or false; nil goes on); `otherwise` when the script ends first
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

function Runner:bookmark()
    local p, r = {}, {}
    for i, v in ipairs(self.picks) do p[i] = v end
    for i, v in ipairs(self.rolls) do r[i] = v end
    return { idx = self.idx, picks = p, rolls = r }
end

function Runner:resume(mark, replay)
    if type(mark) ~= "table" or type(mark.idx) ~= "number" then return end
    replay = replay or {}
    -- the replayed directions, in the walk's order
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
