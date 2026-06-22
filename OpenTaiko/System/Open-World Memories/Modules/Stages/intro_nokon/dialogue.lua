---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- dialogue.lua — Typewriter dialogue system for intro_nokon.
--
-- Usage:
--   Dialogue.show(blocks, onDone, bgOpacity)
--     blocks      : array of {text=string, onConfirm=function|nil}
--     onDone      : called after Nokon2 slides back out
--     bgOpacity   : 0.0–1.0 screen darkening (0 = none)
--
-- Call Dialogue.draw() after all other draws (renders on top).
-- Call Dialogue.update() every frame; it absorbs inputs while active.

local M = {}

local tx     = {}
local txts   = {}   -- txts.dialogue = TEXT:Create(20)

-- ── Layout constants ──────────────────────────────────────────────────────────

local NOKON_Y      = 507
local NOKON_X_IN   = 1480
local NOKON_X_OUT  = 1980
local SLIDE_DUR    = 0.4   -- seconds to slide Nokon2 in/out

local DIAL_X = 74
local DIAL_Y = 725
local TEXT_X = 102
local TEXT_Y = 752
local LINE_GAP = 4  -- extra px between lines beyond glyph height

local CHARS_PER_SEC = 28  -- typewriter speed

-- ── State ─────────────────────────────────────────────────────────────────────

local active    = false
local subState  = "idle"  -- "idle"|"slide_in"|"typing"|"waiting"|"slide_out"

local blocks    = {}
local blockIdx  = 0
local onDone    = nil
local bgOpacity = 0

-- Nokon2 slide
local nokonX       = NOKON_X_OUT
local slideCounter = nil

-- Typewriter
local charData     = {}  -- {glyph, x, y}
local totalChars   = 0
local revealed     = 0   -- float, floor'd for draw
local typeCounter  = nil

-- Input guard: prevent skip and advance firing on the same Decide press
local inputLatch  = false

-- BgTile overlay
local SCREEN_W = 1920
local SCREEN_H = 1080

-- Per-character advance cache (unique chars only, O(alphabet) entries).
-- advance(c) = Width(c + REF) - Width(REF) strips isolated border padding.
local ADVANCE_REF  = "|"
local advanceCache = {}

-- ── Helpers ───────────────────────────────────────────────────────────────────

local function empty() return COUNTER:EmptyCounter() end

local function tileOverlay(opacity)
    local t = tx["bgtile"]
    if t == nil or t.Width == 0 or t.Height == 0 then return end
    local cols = math.ceil(SCREEN_W / t.Width) + 1
    local rows = math.ceil(SCREEN_H / t.Height) + 1
    t:SetOpacity(opacity)
    for c = 0, cols do
        for r = 0, rows do
            t:Draw(c * t.Width, r * t.Height)
        end
    end
    t:SetOpacity(1)
end

-- Return the advance width of character c using a fixed reference glyph.
-- Only one cache entry per unique character (e.g. ~60 entries for English text).
local function charAdvance(c)
    if advanceCache[c] then return advanceCache[c] end
    if txts.dialogue == nil then return 8 end
    local withRef = txts.dialogue:GetText(c .. ADVANCE_REF, false, 9999)
    local ref     = txts.dialogue:GetText(ADVANCE_REF, false, 9999)
    local adv = math.max(1, withRef.Width - ref.Width)
    advanceCache[c] = adv
    return adv
end

-- Split `str` into per-character {glyph, x, y} using txts.dialogue.
-- Cache entries: one per unique character glyph + one per unique char for advance.
local function buildChars(str)
    if txts.dialogue == nil then return {} end
    local result = {}
    local lineH  = nil

    -- Split on newlines
    local lines  = {}
    local lStart = 1
    for i = 1, #str do
        if str:sub(i, i) == "\n" then
            lines[#lines + 1] = str:sub(lStart, i - 1)
            lStart = i + 1
        end
    end
    lines[#lines + 1] = str:sub(lStart)

    for lineIdx, line in ipairs(lines) do
        local x = TEXT_X
        for ci = 1, #line do
            local c = line:sub(ci, ci)
            local g = txts.dialogue:GetText(c, false, 9999)
            if lineH == nil and g.Height > 0 then lineH = g.Height end
            local lh = lineH or 26
            local y  = TEXT_Y + (lineIdx - 1) * (lh + LINE_GAP)
            result[#result + 1] = { glyph = g, x = x, y = y }
            x = x + charAdvance(c)
        end
    end
    return result
end

local function startBlock(idx)
    if idx > #blocks then
        -- All blocks done — slide Nokon2 out
        subState  = "slide_out"
        slideCounter = COUNTER:CreateCounterDuration(0, NOKON_X_OUT - NOKON_X_IN, SLIDE_DUR)
        slideCounter:Listen(function(v) nokonX = NOKON_X_IN + math.floor(v) end)
        slideCounter:Start()
        return
    end
    blockIdx  = idx
    local str = blocks[idx].text or ""
    charData  = buildChars(str)
    totalChars = #charData
    revealed  = 0
    inputLatch = true  -- consume the Decide press that triggered this block
    if totalChars == 0 then
        subState = "waiting"
    else
        subState = "typing"
        if totalChars > 0 then
            -- interval = 1/CHARS_PER_SEC so counter advances 28 units/s
            typeCounter = COUNTER:CreateCounter(0, totalChars, 1 / CHARS_PER_SEC)
            typeCounter:Listen(function(v) revealed = v end)
            typeCounter:Start()
        end
    end
end

-- ── Public API ────────────────────────────────────────────────────────────────

function M.init(t, txtsRef)
    tx   = t
    txts = txtsRef
end

-- Show a dialogue sequence.  blocks = array of {text, onConfirm}.
function M.show(newBlocks, completeCb, opacity)
    blocks    = newBlocks
    onDone    = completeCb
    bgOpacity = opacity or 0
    active    = true
    nokonX    = NOKON_X_OUT
    inputLatch = false
    -- Slide Nokon2 in
    subState     = "slide_in"
    slideCounter = COUNTER:CreateCounterDuration(0, NOKON_X_OUT - NOKON_X_IN, SLIDE_DUR)
    slideCounter:Listen(function(v) nokonX = NOKON_X_OUT - math.floor(v) end)
    slideCounter:Start()
end

function M.isActive() return active end

-- ── Draw ──────────────────────────────────────────────────────────────────────

function M.draw()
    if not active then return end

    -- Screen darkening overlay
    if bgOpacity > 0 then
        tileOverlay(bgOpacity)
    end

    -- Nokon2
    if tx["Nokon2"] then tx["Nokon2"]:Draw(nokonX, NOKON_Y) end

    if subState == "typing" or subState == "waiting" then
        -- Dialogue box (semi-transparent)
        if tx["Dialogue"] then
            tx["Dialogue"]:SetOpacity(0.7)
            tx["Dialogue"]:Draw(DIAL_X, DIAL_Y)
            tx["Dialogue"]:SetOpacity(1)
        end

        -- Typewriter text
        local count = math.min(math.floor(revealed), #charData)
        for i = 1, count do
            local cd = charData[i]
            cd.glyph:Draw(cd.x, cd.y)
        end
    end
end

-- ── Update ────────────────────────────────────────────────────────────────────

function M.update()
    if not active then return end

    -- Tick counters
    if slideCounter then slideCounter:Tick() end
    if typeCounter  then typeCounter:Tick()  end

    local decide = INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return")

    -- Release latch once Decide is lifted
    if inputLatch and not decide then
        inputLatch = false
    end

    local pressed = decide and not inputLatch

    if subState == "slide_in" then
        if slideCounter and slideCounter.Value >= (NOKON_X_OUT - NOKON_X_IN) then
            slideCounter = nil
            nokonX = NOKON_X_IN
            startBlock(1)
        end

    elseif subState == "typing" then
        if typeCounter and typeCounter.Value >= totalChars then
            typeCounter = nil
            revealed    = totalChars
            subState    = "waiting"
            inputLatch  = true  -- wait for Decide release before accepting advance
        elseif pressed then
            -- Skip: reveal all at once
            typeCounter = nil
            revealed    = totalChars
            subState    = "waiting"
            inputLatch  = true
        end

    elseif subState == "waiting" then
        if pressed then
            -- Fire onConfirm callback for the current block
            local cb = blocks[blockIdx] and blocks[blockIdx].onConfirm
            if cb then cb() end
            -- Advance to next block
            startBlock(blockIdx + 1)
        end

    elseif subState == "slide_out" then
        if slideCounter and slideCounter.Value >= (NOKON_X_OUT - NOKON_X_IN) then
            slideCounter = nil
            nokonX  = NOKON_X_OUT
            active  = false
            subState = "idle"
            if onDone then onDone() end
        end
    end
end

return M
