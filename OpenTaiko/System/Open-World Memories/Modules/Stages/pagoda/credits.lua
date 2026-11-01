-- End credits after the level-20 clear.

local Easing    = require("Easing")
local I18N      = require("i18n")
local backdrop  = require("encounter/credits/backdrop")
local roll      = require("story/credits")
local composers = require("story/credits_composers")
local Config    = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.stage.credits
local Time = Animation.stage.credits

local SONG_PATH = "Sounds/credits.ogg"
local BG_DIM    = Look.bg_dim           -- brightness of the first picture
local SCENE_DIM = Look.scene_dim        -- brightness of the backgrounds after it
local BG_DARKEN = Time.bg_darken_s

-- timings, seconds into the song
local ROLL_START = Time.roll_start_s
local ROLL_STOP  = Time.roll_stop_s     -- last line at rest mid-screen, on the song's last chord
local SETTLE_SEC = Time.settle_s
local DONE_AT    = Time.done_at_s
local SONG_WAIT  = Time.song_wait_s     -- the roll starts after this long when no song plays

-- roll speed factor while decide is held
local FAST       = Time.fast
local SPEED_RATE = Time.speed_rate
-- a roll resting this far ahead of the song fades the song out
local AHEAD_FADE = Time.ahead_fade_s

-- layout
local HEADER_PAD, LINE_PAD, NAME_PAD = Look.header_pad, Look.line_pad, Look.name_pad
local GAP_PX    = Look.gap_px
local COLUMNS   = Look.columns
local COLUMN_W  = Look.column_w
local EDGE_FADE = Look.edge_fade

local _fonts = {}
local _cHeader, _cLine, _cName, _cLast, _cEdge = nil, nil, nil, nil, nil

local _items = nil              -- { text, font, color, x, y, h, maxW } in roll space (first line at y 0)
local _speedPx = 0              -- px/s, before the slow-down
local _song = nil
local _t, _real, _wait = 0, 0, 0
local _dark = 0                 -- 0..1
local _started = false
local _speed = 1
local _fadeFrom = nil           -- roll time the song fade-out started at, or nil

-- fonts = { gfont, gfontName, gfontSmall }
function M.setup(_, fonts)
    _fonts = fonts or {}
    if _cHeader == nil then
        _cHeader = Config.color(Look.header_color)
        _cLine   = Config.color(Look.line_color)
        _cName   = Config.color(Look.name_color)
        _cLast   = Config.color(Look.last_color)
        _cEdge   = Config.color(Look.edge_color)
    end
end

function M.keys()
    return backdrop.keys()
end

local function lineHeight(font)
    return (font ~= nil and font.LineHeight) or 0
end

local function addComposers(items, y, cx)
    local font = _fonts.gfontSmall or _fonts.gfont
    local h = lineHeight(font)
    for first = 1, #composers, COLUMNS do
        local n = math.min(COLUMNS, #composers - first + 1)
        for i = 0, n - 1 do
            items[#items + 1] = { text = composers[first + i], font = font, color = _cName,
                                  x = cx + (i - (n - 1) / 2) * COLUMN_W, y = y, h = h, maxW = COLUMN_W - Look.column_margin }
        end
        y = y + h + NAME_PAD
    end
    return y
end

-- returns the items and the roll distance that centres the last line (or a too-tall last section)
local function layout()
    local res = THEME:GetResolution()
    local cx = res.X * 0.5
    local items, y = {}, 0
    local lastHeader, lastItem = nil, nil
    for _, e in ipairs(roll) do
        if e.gap then
            y = y + GAP_PX
        elseif e.composers then
            y = addComposers(items, y, cx)
        elseif (e.header or e.line or e.last) ~= nil then
            local header = e.header ~= nil
            local font, color = _fonts.gfont, _cLine
            if header then font, color = _fonts.gfontName or font, _cHeader end
            if e.last ~= nil then font, color = _fonts.gfontName or font, _cLast end
            local h = lineHeight(font)
            items[#items + 1] = { text = I18N.tr(e.header or e.line or e.last), font = font, color = color,
                                  x = cx, y = y, h = h, maxW = res.X - Look.line_margin }
            if header then lastHeader = #items end
            if e.last ~= nil then lastItem = items[#items] end
            y = y + h + (header and HEADER_PAD or LINE_PAD)
        end
    end
    lastItem = lastItem or items[#items]
    local distance = (lastItem ~= nil) and (lastItem.y + lastItem.h * 0.5 + res.Y * 0.5) or res.Y
    if lastHeader ~= nil and lastItem ~= nil and items[lastHeader].y - (distance - res.Y) < EDGE_FADE then
        distance = (items[lastHeader].y + lastItem.y + lastItem.h) * 0.5 + res.Y * 0.5
    end
    if lastHeader ~= nil and lastHeader > 1 then
        local bottom = 0
        for i = 1, lastHeader - 1 do bottom = math.max(bottom, items[i].y + items[i].h) end
        local extra = math.max(0, bottom - (distance - res.Y))
        for i = lastHeader, #items do items[i].y = items[i].y + extra end
        distance = distance + extra
    end
    return items, distance
end

-- roll offset (px) at roll time t (s)
local function offset(t)
    if t <= ROLL_START then return 0 end
    local steadyEnd = ROLL_STOP - SETTLE_SEC
    if t <= steadyEnd then return _speedPx * (t - ROLL_START) end
    local u = math.min(t, ROLL_STOP) - steadyEnd
    return _speedPx * (steadyEnd - ROLL_START) + _speedPx * (u - u * u / (2 * SETTLE_SEC))
end

function M.open()
    local items, distance = layout()
    _items = items
    _speedPx = distance / (ROLL_STOP - ROLL_START - SETTLE_SEC * 0.5)
    backdrop.open()
    _t, _real, _wait, _dark = 0, 0, 0, 0
    _started, _speed, _fadeFrom = false, 1, nil
    local ok, s = pcall(function() return SOUND:CreateBGM(SONG_PATH) end)
    _song = ok and s or nil
    if _song ~= nil then _song:Play() end
end

-- nav = NavInput; returns "done" once the roll has rested
function M.update(dt, nav)
    if _items == nil then return "done" end
    _dark = math.min(1, _dark + dt / BG_DARKEN)
    if not _started then
        _wait = _wait + dt
        _started = (_song ~= nil and _song.IsPlaying) or _wait >= SONG_WAIT
        if not _started then return nil end
    end
    local held = nav ~= nil and nav.decidePressing ~= nil and nav.decidePressing()
    _speed = _speed + ((held and FAST or 1) - _speed) * math.min(1, dt * SPEED_RATE)
    _t = _t + dt * _speed
    _real = _real + dt
    backdrop.update(dt * _speed, _t)
    if _t >= DONE_AT then return "done" end
    if _fadeFrom == nil and _t >= ROLL_STOP and _t - _real > AHEAD_FADE then _fadeFrom = _t end
    if _fadeFrom ~= nil and _song ~= nil then
        _song:SetVolumePercent(100 * (1 - Easing.span(_t, _fadeFrom, DONE_AT - _fadeFrom)))
    end
    return nil
end

-- returns (grey, visible) for the skybox
function M.sky()
    return backdrop.sky()
end

-- text opacity at screen y
local function edgeAlpha(y, res)
    local a = math.min(1, y / EDGE_FADE, (res.Y - y) / EDGE_FADE)
    if a <= 0 then return 0 end
    return a * a * (3 - 2 * a)
end

function M.draw()
    local res = THEME:GetResolution()
    local k = Easing.inOutSine(_dark)
    backdrop.draw(1 + (BG_DIM - 1) * k, 1 + (SCENE_DIM - 1) * k)
    if _items == nil then return end
    local top = res.Y - offset(_t)
    for _, it in ipairs(_items) do
        local y = top + it.y
        if y > res.Y then break end
        if it.font ~= nil and y + it.h > 0 then
            local a = edgeAlpha(y + it.h * 0.5, res)
            if a > 0 then it.font:Draw(it.text, it.x, y, it.color, _cEdge, a, 1, it.maxW, "top") end
        end
    end
end

function M.close()
    if _song ~= nil then
        _song:Stop()
        _song:Dispose()
        _song = nil
    end
    backdrop.close()
    _items = nil
end

return M
