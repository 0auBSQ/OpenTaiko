-- The encounter screen's song bars UI.

local Easing   = require("Easing")
local data     = require("encounter/songbars_data")
local assets   = require("encounter/assets")
local reveal   = require("encounter/song_reveal")
local song_bar = require("encounter/song_bar")
local Flames   = require("encounter/bar_flames")
local label    = require("encounter/band_label")
local badge    = require("encounter/speed_badge")
local Destiny  = require("encounter/destiny")
local Config   = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

assets.add(data)

local Look = Layout.encounter.songbars
local Anim = Animation.encounter.songbars

local NAME_FONT, LABEL_FONT, FLAVOR_FONT = Look.name_font_size, Look.label_font_size, Look.flavor_font_size
local SLOT_X, SLOT_Y = Look.slot_x, Look.slot_y             -- first bar's centre
local SLOT_DX, SLOT_DY = Look.slot_dx, Look.slot_dy         -- step to each next bar
local SLOT_ROT, SLOT_DROT = Look.slot_rot_deg, Look.slot_drot_deg   -- first bar's turn and each next one's extra, degrees
local SLOT_DS = Look.slot_ds                    -- scale lost per bar
local FLOAT_Y, FLOAT_HZ = Anim.float_y_px, Anim.float_hz
local FLOAT_PHASE = Anim.float_phase
local FLOAT_ROT, FLOAT_ROT_HZ = Anim.float_rot_deg, Anim.float_rot_hz
local FLOAT_ROT_PHASE = Anim.float_rot_phase
local IN_STAGGER, IN_SEC = Anim.in_stagger_s, Anim.in_s
local IN_FROM_X, IN_ROT, IN_BACK = Anim.in_from_x, Anim.in_rot_deg, Anim.in_back
local EMBLEM_AT, EMBLEM_SEC, EMBLEM_BACK = Anim.emblem_at_s, Anim.emblem_s, Anim.emblem_back
local LABEL_AT = Anim.label_at_s                -- seconds after the last bar is in
local BADGE_AT, BADGE_SEC = Anim.badge_at_s, Anim.badge_s   -- BADGE_AT: seconds after the label starts
local DESTINY_BEAT = Anim.destiny_beat_s        -- seconds after everything is in
local SWISH_VOL = 55
local OUT_STAGGER, OUT_SEC = Anim.out_stagger_s, Anim.out_s
local OUT_TO_X, OUT_ROT, OUT_BACK = Anim.out_to_x, Anim.out_rot_deg, Anim.out_back
local EXIT_SEC = Anim.exit_s                    -- an unknown mystery bar's exit, seconds
local POP_STEP, POP_SEC = Anim.pop_step_s, Anim.pop_s       -- revealed name letters: step and duration, seconds
local LIFT_SEC, LIFT_POP_AT = Anim.lift_s, Anim.lift_pop_at_s   -- darkness lift duration; name delay after it starts
local WOB_STAGGER, WOB_SEC, WOB_HZ = Anim.wob_stagger_s, Anim.wob_s, Anim.wob_hz
local WOB_ROT, WOB_Y = Anim.wob_rot_deg, Anim.wob_y_px      -- wobble amplitude: degrees, px
local LOAD_WAIT = Anim.load_wait_s              -- seconds a new roll's bars wait at most for their art

local SFX = {
    swish = "Sounds/result/swish.ogg", zoom = "Sounds/result/zoom.ogg", crack1 = "Sounds/result/crack.ogg",
    crack2 = "Sounds/result/crack.ogg", crack3 = "Sounds/result/crack.ogg",
    shatter = "Sounds/result/shatter.ogg", ping = "Sounds/result/star_ping.ogg",
}
local DESTINY_SFX = { "zoom", "crack1", "crack2", "crack3", "shatter", "ping" }   -- one sound per crack so none cuts another

local _fonts = nil              -- glyph fonts: name (bars), label (tier), flavor (destiny line)
local _black = nil
local _tex = {}                 -- key -> encounter/assets handle, this visit
local _bars = {}
local _sfx, _swish = {}, {}
local _destinyBar, _destiny = nil, nil
local _started = false
local _t, _clock = 0, 0
local _labelAt, _allIn = 0, 0
local _slot = { x = 0, y = 0, s = 1, rot = 0 }
local _level, _highest, _speed, _roll = 1, 0, nil, nil
local _barsT0 = 0               -- clock time the bars started coming in
local _outT0 = nil              -- clock time they started leaving
local _wobT0 = nil              -- clock time the wobble started
local _told = false             -- a reveal happened: later rolls come in told
local _incoming, _incomingKeys, _incomingT, _incomingDestiny = nil, nil, 0, false

local function sk(key, fallback)
    local s = THEME:GetSkinString(key)
    if s == nil or s == "" then return fallback end
    return s
end

-- tex = the stage's resident textures (black1 dims behind the destiny);
-- fonts = the stage's glyph fonts (gfontSmall, gfontName: the speed badge's lines)
function M.setup(tex, fonts)
    _black = tex ~= nil and tex.black1 or nil
    if _fonts == nil then
        _fonts = { name = TEXT:CreateGlyphCached(NAME_FONT), label = TEXT:CreateGlyphCached(LABEL_FONT),
            flavor = TEXT:CreateGlyphCached(FLAVOR_FONT) }
    end
    fonts = fonts or {}
    song_bar.setup(_fonts.name, _fonts.flavor, NAME_FONT)
    label.setup(_fonts.label)
    badge.setup({ label = fonts.gfontSmall or fonts.gfont, value = fonts.gfontName or fonts.gfont })
end

local function freeSounds()
    for _, s in pairs(_sfx) do s:Stop() ; s:Dispose() end
    for _, s in ipairs(_swish) do s:Stop() ; s:Dispose() end
    _sfx, _swish = {}, {}
end

local function sound(path)
    local ok, s = pcall(function() return SOUND:CreateSFX(path) end)
    if ok then return s end
    return nil
end

local function play(name)
    local s = _sfx[name]
    if s ~= nil then s:Play() end
end

-- swish sounds for bar slots 1..n
local function swishes(n)
    for i = #_swish + 1, n do
        local s = sound(SFX.swish)
        if s == nil then break end
        pcall(function() s:SetVolumePercent(SWISH_VOL) end)
        _swish[i] = s
    end
end

local function swish(i)
    local s = _swish[i]
    if s ~= nil then s:Play() end
end

local function use(keys, key)
    if not keys[key] then
        keys[key] = true
        keys[#keys + 1] = key
    end
end

-- a bar's emblem and level digits; none while darkness covers it
local function emblemKeys(keys, b)
    use(keys, "sb_emblem" .. (b.diff or 0))
    if b.level ~= nil then
        local s = tostring(b.level)
        for i = 1, #s do use(keys, "sb_level" .. s:sub(i, i)) end
    end
    if b.plus then use(keys, "sb_level_plus") end
end

local function asList(keys)
    local list = {}
    for i = 1, #keys do list[i] = keys[i] end
    return list
end

-- encounter/assets keys a visit draws
local function visitKeys(level, bars, destiny, speed)
    local keys = {}
    for _, b in ipairs(bars) do
        use(keys, "sb_bar_" .. b.color)
        use(keys, "sb_checker")
        if b.dark then
            use(keys, "sb_emblem_missing") ; use(keys, "sb_shell")
            for i = 1, 3 do use(keys, "sb_flame" .. i) end
        end
        if not b.dark or b == destiny then emblemKeys(keys, b) end
    end
    if destiny ~= nil then
        for i = 1, 3 do use(keys, "sb_crack" .. i) end
        for _, s in ipairs(data.shards) do use(keys, s.key) end
        use(keys, "fx_glow")
    end
    for _, k in ipairs(label.keys(level)) do use(keys, k) end
    if badge.multiplier(speed) ~= nil then use(keys, "sb_speed_arrow") end
    return asList(keys)
end

-- ctx: highest (highest beaten floor) and roll = { songs, purple, speed } from pagoda.lua.
-- Returns the encounter/assets keys the visit draws.
function M.keys(level, ctx)
    ctx = ctx or {}
    local roll = ctx.roll
    local bars, destiny = reveal.bars(level, ctx.highest, roll)
    return visitKeys(level, bars, destiny and bars[#bars] or nil, roll ~= nil and roll.speed or nil)
end

-- opens the keys and takes their handles
local function take(keys)
    assets.open(keys)
    for _, key in ipairs(keys) do
        if _tex[key] == nil then _tex[key] = (assets.get(key)) end
    end
end

local function prepare(bars)
    for _, b in ipairs(bars) do
        if not b.stay then
            song_bar.prepare(b)
            b.xf = { x = 0, y = 0, s = 1, rot = 0 }
            b.alpha, b.crack, b.emblemPop = 0, 0, 0
            if b.dark then b.flames = Flames.new(data.bar.w, data.bar.h) end
        end
    end
end

-- bars the items move (not level 20's destiny bar after the tambourine)
local function movers()
    local n = 0
    for _, b in ipairs(_bars) do if not b.stay then n = n + 1 end end
    return n
end

-- ctx as for keys(); the screen has opened those keys
function M.open(level, ctx)
    M.close()
    ctx = ctx or {}
    local roll = ctx.roll
    _level, _highest, _roll = level, ctx.highest, roll
    local bars, destiny = reveal.bars(level, ctx.highest, roll)
    _bars = bars
    _destinyBar = destiny and bars[#bars] or nil
    prepare(_bars)
    _speed = roll ~= nil and roll.speed or nil
    for _, key in ipairs(visitKeys(level, _bars, _destinyBar, _speed)) do _tex[key] = (assets.get(key)) end
    label.open(level, _tex)
    badge.open(_speed)
    swishes(#_bars)
    if _destinyBar ~= nil then
        for _, n in ipairs(DESTINY_SFX) do _sfx[n] = sound(SFX[n]) end
    end
    local n = #_bars
    local intro = (n > 0) and ((n - 1) * IN_STAGGER + IN_SEC) or 0
    _labelAt = intro + LABEL_AT
    _allIn = _labelAt + math.max(label.duration(), badge.shown() and (BADGE_AT + BADGE_SEC) or 0)
    _started, _t, _clock = false, 0, 0
    _destiny = nil
    _barsT0, _outT0, _wobT0, _told = 0, nil, nil, false
    _incoming, _incomingKeys, _incomingT, _incomingDestiny = nil, nil, 0, false
end

function M.start()
    _started, _t = true, 0
end

-- jumps the bars' entrance to its end (the destiny sequence still plays)
function M.skip()
    if _started and _destiny == nil and _t < _allIn then
        _t = _allIn
        for _, b in ipairs(_bars) do b.swished = true end
    end
end

-- true once everything is in place and the destiny sequence, if any, is over
function M.done()
    if not _started or _t < _allIn then return false end
    if _destinyBar == nil and _destiny == nil then return true end
    return _destiny ~= nil and _destiny:done()
end

-- true once the bars are done and no item is moving them
function M.settled()
    if not M.done() or _outT0 ~= nil or _incoming ~= nil then return false end
    local n = movers()
    if n > 0 and _t < _barsT0 + (n - 1) * IN_STAGGER + IN_SEC then return false end
    for _, b in ipairs(_bars) do
        if b.exitAt ~= nil or b.popsEnd ~= nil or (b.veil or 0) > 0 then return false end
    end
    return true
end

local function hidden(b)
    return b.dark or (not b.purple and not b.named)
end

-- bars still hiding something: { { bar, x, y }, ... }
function M.hiddenBars()
    local out = {}
    for i, b in ipairs(_bars) do
        if hidden(b) then
            out[#out + 1] = { bar = b, x = SLOT_X + (i - 1) * SLOT_DX, y = SLOT_Y + (i - 1) * SLOT_DY }
        end
    end
    return out
end

function M.anyHidden()
    for _, b in ipairs(_bars) do
        if hidden(b) then return true end
    end
    return false
end

-- for a name revealed at time t0
local function popsFrom(b, t0)
    local n = 0
    for _ in (b.title or "???"):gmatch("[%z\1-\127\194-\244][\128-\191]*") do n = n + 1 end
    b.popsEnd = t0 + n * POP_STEP + POP_SEC
    b.pops = function(i)
        local u = (_t - t0 - (i - 1) * POP_STEP) / POP_SEC
        if u <= 0 then return 0 end
        return Easing.outCubic(u)
    end
end

-- the mystery song's bar with everything told, or nil when no mystery song came
local function toldMystery()
    local told = reveal.revealed(_level, _roll)
    local last = told[#told]
    if last ~= nil and last.purple then return last end
    return nil
end

-- tells a bar's secret; returns "name" (name shown), "lift" (darkness lifted),
-- "exit" (an unknown mystery bar leaves: none came) or nil
function M.reveal(b)
    if b == nil or not hidden(b) then return nil end
    _told = true
    if b.dark then
        if b.chance ~= nil then
            local m = toldMystery()
            if m == nil then
                b.exitAt = _t
                for i, o in ipairs(_bars) do if o == b then swish(i) end end
                return "exit"
            end
            b.title, b.diff, b.level, b.plus, b.chance = m.title, m.diff, m.level, m.plus, nil
            song_bar.prepare(b)
        end
        local keys = {}
        emblemKeys(keys, b)
        take(asList(keys))
        b.dark, b.named, b.revealed = false, true, true
        b.veil = 1
        if b.flames ~= nil then b.flames:stop() end
        b.emblemAt = _t + LIFT_POP_AT
        popsFrom(b, _t + LIFT_POP_AT)
        return "lift"
    end
    b.named, b.revealed = true, true
    popsFrom(b, _t)
    return "name"
end

-- level 20's destiny bar excepted
function M.wobble()
    _wobT0 = _t
    if _destinyBar ~= nil then _destinyBar.stay = true end
end

-- level 20's destiny bar excepted
function M.leave()
    if _outT0 ~= nil then return end
    _outT0 = _t
    if _destinyBar ~= nil then _destinyBar.stay = true end
    for _, b in ipairs(_bars) do b.outSwished = false end
end

-- true once the bars have left
function M.gone()
    if _outT0 == nil then return false end
    local n = movers()
    return _t >= _outT0 + math.max(0, n - 1) * OUT_STAGGER + OUT_SEC
end

-- roll = { songs, purple, speed }; the new bars come in once their art is loaded; level 20's destiny comes back told
function M.reroll(roll)
    _roll = roll
    local bars, destiny = reveal.bars(_level, _highest, roll)
    if _told then
        bars = reveal.revealed(_level, roll)
        destiny = _level == reveal.DESTINY_LEVEL and roll ~= nil and roll.purple ~= nil
    end
    local last = destiny and bars[#bars] or nil
    if last ~= nil and _destiny ~= nil then
        if _destinyBar ~= nil and _destinyBar.stay then
            bars[#bars] = _destinyBar
        else
            last.dark, last.named, last.revealed = false, true, true
            last.flavor, last.flavorA = sk("PAGODA_DESTINY", "You cannot change your destiny"), 1
        end
        last = nil
    end
    prepare(bars)
    for _, b in ipairs(_bars) do if b.flames ~= nil and not b.stay then b.flames:stop() end end
    _bars = {}
    _incoming, _incomingT, _incomingDestiny = bars, 0, destiny
    _incomingKeys = visitKeys(_level, bars, last, _speed)
    take(_incomingKeys)
    swishes(#bars)
end

local function arrive()
    _bars, _incoming = _incoming, nil
    _destinyBar = _incomingDestiny and _bars[#_bars] or nil
    _barsT0, _outT0, _wobT0 = _t, nil, nil
end

local function slotOf(i)
    local k = i - 1
    _slot.x = SLOT_X + k * SLOT_DX
    _slot.y = SLOT_Y + k * SLOT_DY + FLOAT_Y * math.sin(2 * math.pi * FLOAT_HZ * _clock + i * FLOAT_PHASE)
    _slot.s = 1 - k * SLOT_DS
    _slot.rot = SLOT_ROT + k * SLOT_DROT + FLOAT_ROT * math.sin(2 * math.pi * FLOAT_ROT_HZ * _clock + i * FLOAT_ROT_PHASE)
    return _slot
end

local function placeBars()
    for i, b in ipairs(_bars) do
        local start = _barsT0 + (i - 1) * IN_STAGGER
        local u = b.stay and 1 or (_t - start) / IN_SEC
        if u > 0 and not b.swished then
            b.swished = true
            swish(i)
        end
        local slot = slotOf(i)
        local xf = b.xf
        if b == _destinyBar and _destiny ~= nil and not _destiny:done() then
            _destiny:place(slot, xf)
        else
            xf.x = slot.x + IN_FROM_X * (1 - Easing.outBack(u, IN_BACK))
            xf.y = slot.y
            xf.s = slot.s
            xf.rot = slot.rot + IN_ROT * (1 - Easing.outCubic(u))
        end
        if _wobT0 ~= nil and not b.stay then
            local w = _t - _wobT0 - (i - 1) * WOB_STAGGER
            if w > 0 and w < WOB_SEC then
                local k = (1 - w / WOB_SEC) ^ 2 * math.sin(w * WOB_HZ * 2 * math.pi)
                xf.rot = xf.rot + WOB_ROT * k
                xf.y = xf.y + WOB_Y * k
            end
        end
        local out = nil
        if _outT0 ~= nil and not b.stay then out = (_t - _outT0 - (i - 1) * OUT_STAGGER) / OUT_SEC end
        if b.exitAt ~= nil then out = (_t - b.exitAt) / EXIT_SEC end
        if out ~= nil and out > 0 then
            if not b.outSwished and b.exitAt == nil then b.outSwished = true ; swish(i) end
            xf.x = xf.x + OUT_TO_X * Easing.inBack(out, OUT_BACK)
            xf.rot = xf.rot + OUT_ROT * Easing.inCubic(out)
        end
        b.alpha = (u > 0) and 1 or 0
        if not b.stay then
            local e0 = b.emblemAt or (start + EMBLEM_AT)
            local e = (_t - e0) / EMBLEM_SEC
            b.emblemPop = (e > 0) and Easing.outBack(e, EMBLEM_BACK) or 0
        end
    end
end

-- advances what the items started: name reveals, darkness lifts, a leaving bar
local function settleBars(dt)
    local i = 1
    while i <= #_bars do
        local b = _bars[i]
        if b.popsEnd ~= nil and _t >= b.popsEnd then b.pops, b.popsEnd = nil, nil end
        if b.veil ~= nil then
            b.veil = math.max(0, b.veil - dt / LIFT_SEC)
            if b.veil <= 0 then b.veil = nil end
        end
        if b.exitAt ~= nil and _t >= b.exitAt + EXIT_SEC then
            if b.flames ~= nil then b.flames:stop() end
            table.remove(_bars, i)
        else
            i = i + 1
        end
    end
end

function M.update(dt)
    _clock = _clock + dt
    for _, b in ipairs(_bars) do
        if b.flames ~= nil then
            b.flames:update(dt)
            if not b.flames:alive() then b.flames = nil end
        end
        if b.flavor ~= nil then b.flavorT = (b.flavorT or 0) + dt end
    end
    if not _started then return end
    _t = _t + dt
    if _incoming ~= nil then
        _incomingT = _incomingT + dt
        if assets.ready(_incomingKeys) or _incomingT >= LOAD_WAIT then arrive() end
    end
    if _destinyBar ~= nil and _destiny == nil and _t >= _allIn + DESTINY_BEAT then
        _destiny = Destiny.new(_destinyBar, play, song_bar.shellTint(_destinyBar),
            sk("PAGODA_DESTINY", "You cannot change your destiny"))
    end
    if _destiny ~= nil then _destiny:update(dt) end
    settleBars(dt)
    placeBars()
end

local function dim(a)
    local b = _black
    if b == nil or not b.Ready or a <= 0 then return end
    local res = THEME:GetResolution()
    b:SetScale(res.X / math.max(1, b.Width), res.Y / math.max(1, b.Height))
    b:SetOpacity(a)
    b:Draw(0, 0)
    b:SetOpacity(1) ; b:SetScale(1, 1)
end

function M.draw()
    if not _started then return end
    local lifted = (_destiny ~= nil and _destiny:zoom() > 0) and _destinyBar or nil
    for _, b in ipairs(_bars) do
        if b ~= lifted then song_bar.draw(b, _tex) end
    end
    if _t >= _labelAt then label.draw(_t - _labelAt, _clock) end
    if badge.shown() then
        local a = math.max(0, math.min(1, (_t - _labelAt - BADGE_AT) / BADGE_SEC))
        badge.draw(_tex.sb_speed_arrow, a, _clock)
    end
    if _destiny ~= nil then
        dim(_destiny:dim())
        if lifted ~= nil then song_bar.draw(lifted, _tex) end
        _destiny:draw(_tex, _tex.fx_glow)
    end
end

function M.close()
    _tex = {}
    freeSounds()
    label.close()
    _bars, _destinyBar, _destiny = {}, nil, nil
    _started, _t = false, 0
    _incoming, _incomingKeys = nil, nil
end

function M.dispose()
    M.close()
    if _fonts ~= nil then
        _fonts.name:Dispose()
        _fonts.label:Dispose()
        _fonts.flavor:Dispose()
        _fonts = nil
    end
end

return M
