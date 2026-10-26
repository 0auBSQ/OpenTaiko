---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- stage.lua — Stage/stand rendering and curtain transitions for intro_nokon.
--
-- Public API (called by game.lua and Script.lua):
--   Stage.init(tx, snd, txts)
--   Stage.reset(numPlayers)   — call once when entering the game stage
--   Stage.closeCurtain(onDone)
--   Stage.openCurtain(onDone)
--   Stage.draw()              — draws everything except dialogue
--   Stage.update()
--   Stage.setSpotlight(p, on)
--   Stage.setPlayerPressing(p, on)
--   Stage.setPlayerDenied(p, on)
--   Stage.clearRoundState()   — reset pressing/denied for a new round
--   Stage.setScore(p, score)
--   Stage.setBlackboard(text) — nil to clear
--   Stage.setDarken(on)       — darkening overlay for evaluation
--   Stage.setChip(chipIdx)    — player chip to display, nil = none
--   Stage.setSongList(cache, selIdx)  — song list data, nil to hide
--   Stage.setGenreList(genres, selIdx) -- genre list data, nil to hide
--   Stage.setClock(remainingMs, show) -- nil remainingMs hides clock
--   Stage.showFail(on)

local I18N = require("i18n")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Stands, Board, Clock, List = Layout.stands, Layout.blackboard, Layout.clock, Layout.list

local M = {}

local tx   = {}
local snd  = {}
local txts = {}

-- ── Constants ─────────────────────────────────────────────────────────────────

local SCREEN_W  = 1920
local SCREEN_H  = 1080
local CURTAIN_W = Layout.curtain.half_w

local STAND_Y   = Stands.y
local STAND_RANGE_LO = Stands.range_lo
local STAND_RANGE_HI = Stands.range_hi
local STAND_PADDING  = Stands.padding   -- inset from borders when numPlayers < 5

local BODY_DY   = Stands.body_dy
local HEAD_DY   = Stands.head_dy
local HANDS_DY  = Stands.hands_dy
local SCORE_DY  = Stands.score_dy

local BB_X     = Board.x
local BB_Y     = Board.y
local BB_MWIDTH = Board.max_width

local CLOCK_X  = Clock.x
local CLOCK_Y  = Clock.y
local CLOCK_NUM_X = Clock.num_x
local CLOCK_NUM_Y = Clock.num_y

local LIST_BG_X  = List.bg_x
local LIST_BG_Y  = List.bg_y
local LIST_HEADER_Y = List.header_y
local LIST_START_Y  = List.start_y
local LIST_SPACING  = List.spacing
local LIST_ROWS     = List.rows_each_side

local PLAYER_COLORS = nil  -- lazy-initialized

-- ── State ─────────────────────────────────────────────────────────────────────

local numPlayers   = 1
local standXPos    = {}   -- [p] = x center

-- Curtain
local curtainX     = CURTAIN_W  -- 0=closed, CURTAIN_W=fully open
local curtainState = "open"     -- "open"|"closing"|"opening"|"closed"
local curtainCtr   = nil
local curtainDone  = nil

-- Per-player
local spotlights  = {}   -- [p] bool
local pressing    = {}   -- [p] bool  (Head_Down + Hands_Down)
local denied      = {}   -- [p] bool
local scores      = {}   -- [p] int

-- UI
local blackboard  = nil   -- string or nil
local darkenOn    = false
local chipIdx     = nil   -- 1-5 or nil
local showFail    = false

local clockMs     = nil   -- remaining ms, nil=hidden
local clockShow   = false

local songList    = nil   -- {cache, selIdx} or nil
local genreList   = nil   -- {genres, selIdx} or nil

-- ── Helpers ───────────────────────────────────────────────────────────────────

local function empty() return COUNTER:EmptyCounter() end

local function col(hex) return COLOR:CreateColorFromHex(hex) end

local function lazyColors()
    if PLAYER_COLORS then return end
    PLAYER_COLORS = {}
    for i, hex in ipairs(Stands.player_colors) do PLAYER_COLORS[i] = col(hex) end
end

local function computeStandPositions(n)
    local pos = {}
    if n == 1 then
        pos[1] = Stands.solo_x
    else
        local lo = n >= 5 and STAND_RANGE_LO or (STAND_RANGE_LO + STAND_PADDING)
        local hi = n >= 5 and STAND_RANGE_HI or (STAND_RANGE_HI - STAND_PADDING)
        for i = 1, n do
            pos[i] = math.floor(lo + (i - 1) * (hi - lo) / (n - 1))
        end
    end
    return pos
end

local function tileOverlay(opacity)
    local t = tx["bgtile"]
    if not t or t.Width == 0 then return end
    local cols = math.ceil(SCREEN_W / t.Width) + 1
    local rows = math.ceil(SCREEN_H / t.Height) + 1
    t:SetOpacity(opacity)
    for c = 0, cols do
        for r = 0, rows do t:Draw(c * t.Width, r * t.Height) end
    end
    t:SetOpacity(1)
end

-- Darken a color to a given factor (0=black, 1=original).
local function darkenColor(hex, factor)
    -- Parse AARRGGBB hex string
    local a = tonumber(hex:sub(1,2), 16) or 255
    local r = tonumber(hex:sub(3,4), 16) or 255
    local g = tonumber(hex:sub(5,6), 16) or 255
    local b = tonumber(hex:sub(7,8), 16) or 255
    return COLOR:CreateColorFromARGB(a,
        math.floor(r * factor),
        math.floor(g * factor),
        math.floor(b * factor))
end

-- ── Init / lifecycle ──────────────────────────────────────────────────────────

function M.init(t, s, txtsRef)
    tx   = t
    snd  = s
    txts = txtsRef
end

function M.reset(n)
    numPlayers  = n or 1
    standXPos   = computeStandPositions(numPlayers)
    curtainX    = 0
    curtainState = "closed"
    curtainCtr  = nil
    for i = 1, 5 do
        spotlights[i] = false
        pressing[i]   = false
        denied[i]     = false
        scores[i]     = 0
    end
    blackboard = nil
    darkenOn   = false
    chipIdx    = nil
    showFail   = false
    clockMs    = nil
    clockShow  = false
    songList   = nil
    genreList  = nil
end

-- ── Curtain transitions ───────────────────────────────────────────────────────

function M.closeCurtain(onDone)
    curtainState = "closing"
    curtainDone  = onDone
    curtainCtr   = COUNTER:CreateCounterDuration(0, CURTAIN_W, Animation.curtain_s)
    curtainCtr:Listen(function(v) curtainX = CURTAIN_W - math.floor(v) end)
    curtainCtr:Start()
    if snd["CurtainOpen"] then snd["CurtainOpen"]:Play() end
end

function M.openCurtain(onDone)
    curtainState = "opening"
    curtainDone  = onDone
    curtainCtr   = COUNTER:CreateCounterDuration(0, CURTAIN_W, Animation.curtain_s)
    curtainCtr:Listen(function(v) curtainX = math.floor(v) end)
    curtainCtr:Start()
    if snd["CurtainOpen"] then snd["CurtainOpen"]:Play() end
end

-- ── Setters ───────────────────────────────────────────────────────────────────

function M.setSpotlight(p, on)      spotlights[p] = on      end
function M.setPlayerPressing(p, on) pressing[p]   = on      end
function M.setPlayerDenied(p, on)   denied[p]     = on      end
function M.setScore(p, v)           scores[p]     = v       end
function M.setBlackboard(text)      blackboard     = text    end
function M.setDarken(on)            darkenOn       = on      end
function M.setChip(idx)             chipIdx        = idx     end
function M.setShowFail(on)          showFail       = on      end
function M.setClock(ms, show)       clockMs = ms; clockShow = (show == true) end
function M.setSongList(cache, sel)  songList = cache and {cache=cache, sel=sel} or nil  end
function M.setGenreList(gen, sel)   genreList = gen and {cache=gen, sel=sel} or nil     end

function M.clearRoundState()
    for i = 1, 5 do pressing[i] = false; denied[i] = false end
end

-- ── Draw ──────────────────────────────────────────────────────────────────────

local function drawStands()
    lazyColors()
    for p = 1, numPlayers do
        local sx = standXPos[p]
        local sy = STAND_Y
        local isDenied   = denied[p]
        local isPressing = pressing[p]
        local pcol = PLAYER_COLORS[p] or col("ffffffff")
        local gray = isDenied and col(Stands.denied_color) or col("ffffffff")

        -- Spotlight (behind everything on the stand); always on when pressing
        if (spotlights[p] or pressing[p]) and tx["Spotlight"] then
            tx["Spotlight"]:DrawAtAnchor(sx, 0, "top")
        end

        -- Body (under Stand, under Head/Hands)
        if tx["player_body"] then
            tx["player_body"]:SetColor(isDenied and darkenColor(Stands.denied_body_color, Stands.denied_body_shade) or pcol)
            tx["player_body"]:DrawAtAnchor(sx, sy + BODY_DY, "center")
            tx["player_body"]:SetColor(col("ffffffff"))
        end

        -- Head (under Stand but over Body)
        local headKey = isPressing and "player_head_down" or "player_head"
        if tx[headKey] then
            tx[headKey]:SetColor(gray)
            tx[headKey]:DrawAtAnchor(sx, sy + HEAD_DY, "center")
            tx[headKey]:SetColor(col("ffffffff"))
        end

        -- Hands BEHIND stand (under Stand)
        if not isPressing and tx["player_hands"] then
            tx["player_hands"]:SetColor(gray)
            tx["player_hands"]:DrawAtAnchor(sx, sy + HANDS_DY, "center")
            tx["player_hands"]:SetColor(col("ffffffff"))
        end

        -- Stand itself
        if tx["Stand"] then
            tx["Stand"]:SetColor(isDenied and col(Stands.denied_stand_color) or col("ffffffff"))
            tx["Stand"]:DrawAtAnchor(sx, sy, "center")
            tx["Stand"]:SetColor(col("ffffffff"))
        end

        -- Hands OVER stand (when pressing)
        if isPressing and tx["player_hands_down"] then
            tx["player_hands_down"]:SetColor(gray)
            tx["player_hands_down"]:DrawAtAnchor(sx, sy + HANDS_DY, "center")
            tx["player_hands_down"]:SetColor(col("ffffffff"))
        end

        -- Score text (over Stand) — title renderer for bigger digits
        if txts.title then
            local st = txts.title:GetText(tostring(scores[p] or 0), false, Stands.score_max_width)
            st:DrawAtAnchor(sx, sy + SCORE_DY, "center")
        end

        -- Denied overlay (over score)
        if isDenied and tx["Denied"] then
            tx["Denied"]:DrawAtAnchor(sx, sy, "center")
        end
    end
end

local function drawBlackboard()
    if blackboard == nil or blackboard == "" then return end
    if txts.title then
        local t = txts.title:GetText(blackboard, false, BB_MWIDTH, col(Board.color))
        t:DrawAtAnchor(BB_X, BB_Y, "center")
    end
end

local function drawClock()
    if not clockShow or clockMs == nil then return end
    if tx["Clock"] then
        tx["Clock"]:DrawAtAnchor(CLOCK_X, CLOCK_Y, "topright")
    end
    if txts.title then
        local secs = math.max(0, math.ceil(clockMs / 1000))
        local tcol      = secs <= Clock.warn_at_s and col(Clock.warn_color) or col(Clock.color)
        local noOutline = Config.color(Clock.outline_color)  -- transparent edge
        local ct = txts.title:GetText(tostring(secs), false, Clock.max_width, tcol, noOutline)
        ct:DrawAtAnchor(CLOCK_NUM_X, CLOCK_NUM_Y, "center")
    end
end

local function drawList(listData, headerText)
    if listData == nil then return end
    local cache  = listData.cache
    local selIdx = listData.sel or 1

    -- Player chip
    if chipIdx and tx["chip_" .. chipIdx] then
        tx["chip_" .. chipIdx]:Draw(0, 0)
    end

    -- Background panel (semi-transparent)
    if tx["SongList"] then
        tx["SongList"]:SetOpacity(List.bg_opacity)
        tx["SongList"]:DrawAtAnchor(LIST_BG_X, LIST_BG_Y, "top")
        tx["SongList"]:SetOpacity(1)
    end

    -- Header text
    if txts.label and headerText then
        local ht = txts.label:GetText(headerText, false, List.header_max_width)
        ht:DrawAtAnchor(LIST_BG_X, LIST_HEADER_Y, "center")
    end

    -- Entries
    local entries = {}
    if type(cache) == "table" then
        for i = -LIST_ROWS, LIST_ROWS do
            local entry = cache[i]
            if entry ~= nil then
                entries[#entries + 1] = {offset = i, entry = entry}
            end
        end
    end

    for _, e in ipairs(entries) do
        local offset = e.offset
        local entry  = e.entry
        local y = LIST_START_Y + (offset + LIST_ROWS) * LIST_SPACING
        local isSel = (offset == 0)

        -- Song block — tinted with the node's genre color (BoxColor)
        if tx["SongBlock"] then
            local nodeColor = (entry.node and entry.node.BoxColor) or col("ffffffff")
            tx["SongBlock"]:SetColor(nodeColor)
            tx["SongBlock"]:SetOpacity(isSel and 1.0 or List.other_opacity)
            tx["SongBlock"]:DrawAtAnchor(LIST_BG_X, y, "center")
            tx["SongBlock"]:SetColor(col("ffffffff"))
            tx["SongBlock"]:SetOpacity(1)
        end

        -- Song/genre name
        if txts.label and entry.text then
            entry.text:SetColor(isSel and col(List.text_color) or col(List.other_text_color))
            entry.text:DrawAtAnchor(LIST_BG_X, y, "center")
            entry.text:SetColor(col("ffffffff"))
        end
    end
end

local function drawCurtain()
    if curtainState == "closed" then
        if tx["Curtain"] then tx["Curtain"]:Draw(0, 0) end
        return
    end
    local co = tx["CurtainOpen"]
    if co and co.Width > 0 then
        co:DrawRect(-curtainX,            0,         0, 0, CURTAIN_W, SCREEN_H)
        co:DrawRect(CURTAIN_W + curtainX, 0, CURTAIN_W, 0, CURTAIN_W, SCREEN_H)
    end
end

function M.draw()
    -- Stage background
    if tx["Stage"] then tx["Stage"]:Draw(0, 0) end

    -- Stands (behind curtain)
    drawStands()

    -- UI elements (behind curtain)
    drawBlackboard()
    drawClock()
    if songList then
        drawList(songList, I18N.tr("Pick the correct song!"))
    elseif genreList then
        drawList(genreList, I18N.tr("Choose the song genre!"))
    end

    -- Darkening overlay for evaluation phases
    if darkenOn then tileOverlay(Layout.darken_opacity) end

    -- Curtain on top of everything
    drawCurtain()
end

-- Draw Fail.png on top of any dialogue overlay — call after Dialogue.draw().
function M.drawFail()
    if showFail and tx["Fail"] then
        tx["Fail"]:DrawAtAnchor(SCREEN_W / 2, SCREEN_H / 2, "center")
    end
end

-- ── Update ────────────────────────────────────────────────────────────────────

function M.update()
    if curtainCtr then
        curtainCtr:Tick()
        local done = false
        if curtainState == "closing" and curtainX <= 0 then
            curtainX = 0; curtainState = "closed"; done = true
        elseif curtainState == "opening" and curtainX >= CURTAIN_W then
            curtainX = CURTAIN_W; curtainState = "open"; done = true
        end
        if done then
            curtainCtr = nil
            local cb = curtainDone
            curtainDone = nil
            if cb then cb() end
        end
    end
end

function M.isCurtainMoving()
    return curtainState == "closing" or curtainState == "opening"
end

function M.isCurtainClosed()
    return curtainState == "closed"
end

-- Instantly reset curtain to fully open (no animation).
function M.forceOpenCurtain()
    curtainX    = CURTAIN_W
    curtainState = "open"
    curtainCtr  = nil
    curtainDone = nil
end

-- Draw only the curtain layer — used during the setup→game transition.
function M.drawCurtainOnly()
    drawCurtain()
end

return M
