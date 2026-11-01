-- Plays the encounter dialogs: a floor's entrance variant (open + start) or a line list (play).
-- Line fields besides the story/entrance.lua format:
--   who = "you"     the player's name as the speaker (narration blip)
--   blip = "<name>" the voice blip (one of the Sounds/<name>.ogg loaded below)
--   wait = true     held while hooks.busy(): a line with text cannot advance, one without hides the box
--   cover = a       (cutscenes) a cover fade, held like a wait line
--   pick = { ... }  plays one option of a random pool in its place (story/dialog_runner.lua)
-- A variant's `sprite` is Ume's pose from the start; it reaches hooks.onLine as a line of its own.

local Dialogue = require("dialogue")
local runner   = require("story/dialog_runner")
local floors   = require("floors")
local react    = require("story/react")
local entrance = require("story/entrance")
local Config   = require("Core.Config")
local Layout   = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.script
local Anim = Animation.encounter.script
-- the box only places the text
local DIALOG_BOX = Config.plain(Look.dialog_box)
local CHOICE_Y   = Look.choice_y
local BLIP_EVERY = Anim.blip_every_chars
local TYPE_CPS   = Anim.chars_per_s
local TEXT_PAD   = 25        -- a glyph box starts this far left of its ink
local TEXT_INSET = 40        -- Dialogue puts the text this far right of its box
local TEXT_NUDGE = Look.text_nudge   -- centre-anchored text sits this far below the box centre
local TAG_Y      = Look.tag.y
local TAG_H      = Look.tag.h
local TAG_CAP    = Look.tag.cap_px   -- nametag.png cap width (the middle stretches)
local TAG_PAD    = Look.tag.pad      -- each side of the name

local SFX_NAMES = { "blip", "blip_narrator", "blip_monster", "blip_monster2",
                    "blip_monster3", "page", "shock", "roar" }
local NAMED_BLIPS = { "needle", "thorncushion", "counterplate", "pincerblaze", "startle",
                      "fray", "dragoon", "frangel", "zirr", "tobiraph" }
local MONSTER_BLIPS = { "blip_monster", "blip_monster2", "blip_monster3" }

local _dlg = nil
local _body, _nameFont = nil, nil
local _sfx = {}
local _gold, _black = nil, nil

local _level, _ctx, _hooks = 1, {}, {}
local _lines, _idx = nil, 0
local _lastProse = nil       -- previous spoken line; bare-choice lines keep it on screen
local _state = "idle"        -- idle | running | done
local _waiting = false       -- held on a wait line without text
local _pose = nil            -- Ume's current pose; a change to scared counts as a reaction

local function loadSfx(name)
    local ok, s = pcall(function() return SOUND:CreateSFX("Sounds/" .. name .. ".ogg") end)
    if ok and s ~= nil then return s end
    return nil
end

local function playSfx(name)
    local s = _sfx[name]
    if s ~= nil then s:Play() end
end

-- node.voice is the raw authored name (node.name is localized);
-- "???" is this floor's monster and keeps its voice before the reveal
local function blipFor(node)
    if node == nil then return "blip_narrator" end
    if node.blip ~= nil then return node.blip end
    if node.speaker == "you" then return "blip_narrator" end
    if node.speaker == "ume" or node.voice == "Ume" then return "blip" end
    local name = node.voice or node.name
    if name == nil or name == "" then return "blip_narrator" end
    if name == "???" then name = floors.monsterName(_level) or name end
    local voiced = "blip_" .. name:lower()
    if _sfx[voiced] ~= nil then return voiced end
    local h = 0
    for i = 1, #name do h = (h + name:byte(i) * i) % 997 end
    return MONSTER_BLIPS[(h % #MONSTER_BLIPS) + 1]
end

function M.setup(fonts)
    fonts = fonts or {}
    local body = fonts.gfontSmall or fonts.gfont
    if _dlg ~= nil and _body == body and _nameFont == fonts.gfont then return end
    if _dlg ~= nil then _dlg:dispose() ; _dlg = nil end
    _body, _nameFont = body, fonts.gfont
    for _, n in ipairs(SFX_NAMES) do
        if _sfx[n] == nil then _sfx[n] = loadSfx(n) end
    end
    for _, n in ipairs(react.SOUNDS) do
        if _sfx[n] == nil then _sfx[n] = loadSfx(n) end
    end
    react.setup()
    for _, n in ipairs(NAMED_BLIPS) do
        if _sfx["blip_" .. n] == nil then _sfx["blip_" .. n] = loadSfx("blip_" .. n) end
    end
    if _gold == nil then
        _gold  = Config.color(Look.name_color)
        _black = Config.color(Look.name_outline_color)
    end
    if body == nil then return end
    _dlg = Dialogue.new{
        gfont = body, gfontName = fonts.gfont,
        ui = "plain", drawBox = false,     -- the scrim is the panel
        portraitSize = 0,
        box = DIALOG_BOX, choiceY = CHOICE_Y,
        cps = TYPE_CPS,
        sfx = { click = function() playSfx("page") end },
        onBlip = function() playSfx(blipFor(_dlg ~= nil and _dlg.node or nil)) end,
        blipEvery = BLIP_EVERY,
        onShake = function(amp, dur)
            if _hooks.onShake ~= nil then _hooks.onShake(amp, dur) end
        end,
        onSfx = function(name)
            if _sfx[name] ~= nil then playSfx(name)
            else SHARED:GetSharedSound("Move"):Play() end
        end,
    }
end

function M.dispose()
    if _dlg ~= nil then _dlg:dispose() ; _dlg = nil end
    react.dispose()
    for _, s in pairs(_sfx) do s:Dispose() end
    _sfx = {}
    _body, _nameFont = nil, nil
    _state, _waiting = "idle", false
end

-- the lines a list plays and their variant: a variant list ({ { when, lines }, ... } as in
-- story/entrance.lua) gives the first passing variant's lines; a plain line list is taken as is
local function pickLines(list, ctx)
    if type(list) ~= "table" then return nil end
    if type(list[1]) ~= "table" or list[1].lines == nil then return list end
    for _, v in ipairs(list) do
        if runner.passes(v, ctx) then return v.lines or {}, v end
    end
    return nil
end

-- the picked variant's starting pose
local function poseFor(variant)
    if variant ~= nil and variant.sprite ~= nil and _hooks.onLine ~= nil then
        _hooks.onLine({ sprite = variant.sprite })
    end
end

-- copies a line list, since the branches are spliced into it
local function copyLines(lines)
    if lines == nil then return nil end
    local out = {}
    for i, l in ipairs(lines) do out[i] = l end
    return out
end

-- hooks = { onLine = fn(line), onShake = fn(amp, dur), busy = fn() -> bool };
-- lines (optional, a line list or a variant list) replaces the floor's story/entrance.lua variants
function M.open(level, ctx, hooks, lines)
    _level, _ctx, _hooks = level, ctx or {}, hooks or {}
    _idx, _lastProse = 0, nil
    _state, _waiting = "idle", false
    if lines == nil then lines = entrance[level] end
    local picked, variant = pickLines(lines, _ctx)
    _lines = copyLines(picked)
    poseFor(variant)
end

-- the picked variant's lines (branches are spliced in as choices resolve)
function M.lines() return _lines or {} end
function M.hasLines() return _lines ~= nil and #_lines > 0 end

-- the name `who` shows: the player's for "you", else Ume's; a bare-choice line keeps
-- the previous prose line and its speaker
local function whoName(line)
    local src = line
    if _lastProse ~= nil and line.choices ~= nil and (line.text == nil or line.text == "") then src = _lastProse end
    if src.who == "you" then return _ctx.playerName or "You" end
    return "Ume"
end

local function shake(amp, dur)
    if _hooks.onShake ~= nil then _hooks.onShake(amp, dur) end
end

-- plays the sting on a line's `react` or a turn to a scared pose
local function reactTo(line)
    local kind = line.react
    if kind == nil and line.sprite ~= nil then
        if line.sprite ~= _pose then kind = react.forPose(line.sprite) end
        _pose = line.sprite
    end
    if kind ~= nil then react.start(kind, playSfx, shake, react.hasSound(line)) end
end

local function startLine(line)
    runner.startLine(_dlg, line, _ctx, whoName(line), _lastProse)
    if _dlg.node ~= nil and line.blip ~= nil then _dlg.node.blip = line.blip end
    if line.text ~= nil and line.text ~= "" then _lastProse = line end
end

local function waits(line)
    return line ~= nil and (line.wait == true or line.cover ~= nil)
end

local function busy()
    return _hooks.busy ~= nil and _hooks.busy() == true
end

local function step()
    _waiting = false
    while true do
        _idx = _idx + 1
        local line = _lines and _lines[_idx]
        if line == nil then _state = "done" ; return end
        if runner.passes(line, _ctx) then
            if line.pick ~= nil then runner.resolvePick(_lines, _idx) end
            reactTo(line)
            if _hooks.onLine ~= nil then _hooks.onLine(line) end
            if line.text ~= nil or line.choices ~= nil then startLine(line) ; return end
            if waits(line) and busy() then _waiting = true ; return end
        end
    end
end

function M.start()
    if _dlg == nil or not M.hasLines() then _state = "done" ; return _state end
    _state, _idx, _waiting = "running", 0, false
    step()
    return _state
end

-- starts a line list (story/entrance.lua format or a variant list); returns the state;
-- ctx (optional) feeds the `when` guards, <player name> and the "???" voice (ctx.startLevel)
function M.play(lines, hooks, ctx)
    _ctx, _hooks = ctx or {}, hooks or {}
    if _ctx.startLevel ~= nil then _level = _ctx.startLevel end
    local picked, variant = pickLines(lines, _ctx)
    _lines, _lastProse = copyLines(picked) or {}, nil
    _pose = variant ~= nil and variant.sprite or nil
    poseFor(variant)
    _state = "idle"
    return M.start()
end

-- returns the state ("running" | "done") and whether advance was pressed this frame; a fully
-- shown line stays up while canAdvance is false, or on a wait line while hooks.busy()
function M.update(dt, canAdvance)
    react.update(dt)
    if _state ~= "running" then return _state, false end
    if _waiting then
        if not busy() then step() end
        return _state, false
    end
    local pressed = _dlg:advancePressed() and true or false
    local held = canAdvance == false or (waits(_lines[_idx]) and busy())
    if held and _dlg.revealed >= _dlg.total and not _dlg.choosing then
        return _state, pressed
    end
    if _dlg:update(dt) == "done" then
        runner.resolveChoice(_dlg, _lines, _idx, _ctx)
        step()
    end
    return _state, pressed
end

local function drawTag(tx, x, w)
    local tw, th = tx.Width, tx.Height
    local mid = tw - 2 * TAG_CAP
    local midW = math.max(0, w - 2 * TAG_CAP)
    tx:DrawRect(x, TAG_Y, 0, 0, TAG_CAP, th)
    if midW > 0 and mid > 0 then
        tx:SetScale(midW / mid, 1)
        tx:DrawRect(x + TAG_CAP, TAG_Y, TAG_CAP, 0, mid, th)
        tx:SetScale(1, 1)
    end
    tx:DrawRect(x + TAG_CAP + midW, TAG_Y, tw - TAG_CAP, 0, TAG_CAP, th)
end

function M.draw(nametagTex)
    react.draw()
    if _state ~= "running" or _waiting or _dlg == nil then return end
    local node = _dlg.node
    local name = node ~= nil and node.name or nil
    if name ~= nil and name ~= "" and _nameFont ~= nil then
        -- the name's ink lines up with the dialog text's ink
        local w = math.floor(_nameFont:Measure(name) + 2 * TAG_PAD + 0.5)
        local x = DIALOG_BOX.x + TEXT_INSET + TEXT_PAD - TAG_PAD
        if nametagTex ~= nil and nametagTex.Ready then drawTag(nametagTex, x, w) end
        _nameFont:Draw(name, x + w / 2, TAG_Y + TAG_H / 2 + TEXT_NUDGE, _gold, _black, 1, 1, 0, "center")
    end
    _dlg:draw()
end

return M
