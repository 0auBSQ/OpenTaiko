---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- setup.lua — Game configuration screen (Mode / Players / Songs) for intro_nokon.
-- update() returns "back" or {mode, players, songs} when confirmed, nil otherwise.

local I18N = require("i18n")

local M = {}

local tx         = {}   -- shared textures (allocated in Script.lua)
local sounds     = {}   -- shared sounds
local txts       = {}   -- text renderers: txts.title, txts.label
local countSongs = nil  -- function(scope) -> int, injected from Script.lua

local errorMsg   = nil
local errorTimer = nil

-- ── Constants ─────────────────────────────────────────────────────────────────

local SCALE_SEL   = 1.2
local SCALE_UNSEL = 0.8
local ANIM_DUR    = 0.12   -- seconds for zoom transition
local MAX_GAP     = 100    -- pixel cap on inter-item gap

-- Roll vertical centers (screen is 1080px; title uses top ~110px)
local ROLL_Y = { 285, 570, 855 }

local WHITE = "ffffffff"
local GRAY  = "ff888888"
local DARK  = "ff444444"
local GOLD  = "ffffd700"

-- ── Roll definitions ─────────────────────────────────────────────────────────

local function makeRolls()
    return {
        {   -- roll 1: Mode
            label = "Mode",
            items = {
                { key = "mode_endurance", value = "Endurance" },
                { key = "mode_vs",        value = "VS"        },
            },
            selectedIdx = 1,
            animCounter = nil, animFromIdx = nil, animToIdx = nil,
        },
        {   -- roll 2: Players
            label = "Players",
            items = {
                { key = "player_1", value = 1 },
                { key = "player_2", value = 2 },
                { key = "player_3", value = 3 },
                { key = "player_4", value = 4 },
                { key = "player_5", value = 5 },
            },
            selectedIdx = 1,
            animCounter = nil, animFromIdx = nil, animToIdx = nil,
        },
        {   -- roll 3: Songs
            label = "Songs",
            items = {
                { key = "songs_optk",   value = "OpTk"   },
                { key = "songs_custom", value = "Custom" },
                { key = "songs_all",    value = "All"    },
            },
            selectedIdx = 1,   -- default: OpTk
            animCounter = nil, animFromIdx = nil, animToIdx = nil,
        },
    }
end

local rolls      = {}
local activeRoll = 1   -- 1=Mode, 2=Players, 3=Songs
local showGo     = false
local goOpacity  = 255
local goCounter  = nil

local showRounds = false
local roundCount = 0
local roundStep  = 1

-- ── Helpers ───────────────────────────────────────────────────────────────────

local function col(hex) return COLOR:CreateColorFromHex(hex) end

-- Is item i of roll r available for selection?
local function isAvailable(r, i)
    if r ~= 2 then return true end
    local modeIdx = rolls[1].selectedIdx
    if modeIdx == 1 then return i == 1 end   -- Endurance: only 1P
    return i >= 2                            -- VS: 2-5P
end

-- Compute horizontal center x of each item in roll r.
local function itemPositions(r)
    local items = rolls[r].items
    local n     = #items
    local maxW  = 0
    for _, item in ipairs(items) do
        local t = tx[item.key]
        if t and t.Width > maxW then maxW = t.Width end
    end
    if maxW == 0 then maxW = 200 end

    -- Choose gap so items fit without script overflowing (target total ≤ 900px)
    local gap = math.max(20, math.min(MAX_GAP, math.floor((900 - n * maxW) / math.max(1, n - 1))))
    local totalW = n * maxW + (n - 1) * gap
    local x0     = 960 - totalW / 2

    local pos = {}
    for i = 1, n do
        pos[i] = x0 + (i - 1) * (maxW + gap) + maxW / 2
    end
    return pos
end

-- Current visual scale of item i in roll r.
local function itemScale(r, i)
    local roll = rolls[r]
    if roll.animCounter ~= nil then
        local t = roll.animCounter.Value
        if   i == roll.animToIdx   then return SCALE_UNSEL + (SCALE_SEL - SCALE_UNSEL) * t
        elseif i == roll.animFromIdx then return SCALE_SEL   + (SCALE_UNSEL - SCALE_SEL) * t
        end
    end
    return i == roll.selectedIdx and SCALE_SEL or SCALE_UNSEL
end

-- Change selection in roll r to newIdx with zoom animation.
local function changeSelection(r, newIdx)
    local roll = rolls[r]
    if newIdx == roll.selectedIdx then return end
    roll.animFromIdx = roll.selectedIdx
    roll.animToIdx   = newIdx
    roll.selectedIdx = newIdx
    roll.animCounter = COUNTER:CreateCounterDuration(0, 1, ANIM_DUR)
    roll.animCounter:Start()
end

-- After changing Mode, snap Players to first valid option.
local function snapPlayers()
    local modeIdx = rolls[1].selectedIdx
    local pRoll   = rolls[2]
    if modeIdx == 1 and pRoll.selectedIdx ~= 1 then
        changeSelection(2, 1)
    elseif modeIdx == 2 and pRoll.selectedIdx == 1 then
        changeSelection(2, 2)
    end
end

-- Move selection in roll r by dir, skipping unavailable items.
local function navigate(r, dir)
    local roll = rolls[r]
    local n    = #roll.items
    local idx  = roll.selectedIdx + dir
    for _ = 1, n do
        if idx < 1 then idx = n elseif idx > n then idx = 1 end
        if isAvailable(r, idx) then
            changeSelection(r, idx)
            SHARED:GetSharedSound("Skip"):Play()
            return
        end
        idx = idx + dir
    end
end

local function startGo()
    showGo    = true
    goOpacity = 255
    goCounter = COUNTER:CreateCounter(0, 255, 0.5 / 255)
    goCounter:SetBounce(true)
    goCounter:Listen(function(v) goOpacity = math.floor(v) end)
    goCounter:Start()
end

local function stopGo()
    showGo    = false
    goCounter = nil
    goOpacity = 255
end

local function buildResult()
    local mode = rolls[1].items[rolls[1].selectedIdx].value
    return {
        mode    = mode,
        players = rolls[2].items[rolls[2].selectedIdx].value,
        songs   = rolls[3].items[rolls[3].selectedIdx].value,
        rounds  = (mode == "VS") and roundCount or nil,
    }
end

local function enterRoundsStep()
    local players = rolls[2].items[rolls[2].selectedIdx].value
    roundStep  = players
    roundCount = players * 2
    showRounds = true
    SHARED:GetSharedSound("Decide"):Play()
end

local function exitRoundsStep()
    showRounds = false
end

local function showError(msg)
    errorMsg   = msg
    errorTimer = COUNTER:CreateCounterDuration(0, 1, 5)
    errorTimer:Start()
end

local function validateAndStart()
    if countSongs == nil then startGo(); return end
    local cfg   = buildResult()
    local count = countSongs(cfg.songs)
    local ok    = true
    if cfg.mode == "Endurance" then
        -- 1-song is the egg mode (allowed); 0 or 2–4 songs are not enough
        if count == 0 or (count >= 2 and count <= 4) then ok = false end
    else -- VS
        if count < 5 then ok = false end
    end
    if ok then
        startGo()
    else
        showError(I18N.tr("The game can only be started if there are at least 5 available songs."))
    end
end

-- ── Init / lifecycle ──────────────────────────────────────────────────────────

function M.init(t, snd, txt, countFn)
    tx         = t
    sounds     = snd
    txts       = txt
    countSongs = countFn
end

function M.reset()
    rolls      = makeRolls()
    activeRoll = 1
    stopGo()
    showRounds = false
    roundCount = 0
    roundStep  = 1
    errorMsg   = nil
    errorTimer = nil

    -- Resume BGM if needed (e.g. returning from game results)
    if not sounds.BGM.IsPlaying then
        sounds.BGM:SetLoop(true)
        sounds.BGM:Play()
    end
end

-- ── Draw ──────────────────────────────────────────────────────────────────────

local function drawRoll(r, isActive)
    local roll   = rolls[r]
    local rollY  = ROLL_Y[r]
    local items  = roll.items
    local pos    = itemPositions(r)

    -- Label (gold for active, gray for others)
    if txts.label then
        local lc = isActive and col(GOLD) or col(GRAY)
        local lt = txts.label:GetText(I18N.tr(roll.label), false, 500, lc)
        -- Position label just above the tallest image
        local maxH = 0
        for _, item in ipairs(items) do
            local t = tx[item.key]
            if t and t.Height > maxH then maxH = t.Height end
        end
        if maxH == 0 then maxH = 200 end
        lt:DrawAtAnchor(960, rollY - maxH * SCALE_SEL / 2 - 20, "center")
    end

    local baseOp = isActive and 1.0 or 0.6

    for i, item in ipairs(items) do
        local t = tx[item.key]
        if t and t.Width > 0 then
            local scale = itemScale(r, i)
            local avail = isAvailable(r, i)
            local sel   = (i == roll.selectedIdx)

            if not avail then
                t:SetColor(col(DARK))
            elseif sel then
                t:SetColor(col(WHITE))
            else
                t:SetColor(col(GRAY))
            end

            t:SetOpacity(baseOp)
            t:SetScale(scale, scale)
            t:DrawAtAnchor(pos[i], rollY, "center")
            t:SetScale(1, 1)
            t:SetOpacity(1)
            t:SetColor(col(WHITE))
        end
    end
end

function M.draw()
    -- Background
    if tx["Background"] then tx["Background"]:Draw(0, 0) end

    -- Title
    if txts.title then
        local tt = txts.title:GetText(I18N.tr("Let's set up the game show!"), false, 1400, col(WHITE))
        tt:Draw(30, 45)
    end

    -- All 3 rolls (active highlighting suppressed during rounds step)
    for r = 1, 3 do
        drawRoll(r, r == activeRoll and not showRounds)
    end

    -- Rounds selection panel (VS only, after Songs)
    if showRounds then
        -- Full-screen dark overlay drawn over the rolls
        local bgt = tx["bgtile"]
        if bgt ~= nil then
            local res = THEME:GetResolution()
            bgt:SetOpacity(0.75)
            for rx = 0, math.ceil(res.X / math.max(1, bgt.Width)) - 1 do
                for ry = 0, math.ceil(res.Y / math.max(1, bgt.Height)) - 1 do
                    bgt:Draw(rx * bgt.Width, ry * bgt.Height)
                end
            end
            bgt:SetOpacity(1)
        end
        if txts.title then
            txts.title:GetText(I18N.tr("Round Count"), false, 800, col(GOLD)):DrawAtAnchor(960, 430, "center")
        end
        if txts.label then
            local canDec = roundCount > roundStep
            local arrow_l = canDec and "◀   " or "      "
            txts.label:GetText(arrow_l .. tostring(roundCount) .. "   ▶", false, 600, col(WHITE))
                :DrawAtAnchor(960, 560, "center")
        end
    end

    -- Go.png with bounce opacity (confirms everything is set)
    if showGo and tx["go"] then
        tx["go"]:SetOpacity(goOpacity / 255)
        tx["go"]:Draw(1428, 504)
        tx["go"]:SetOpacity(1)
    end

    -- Error message (bottom of screen, red)
    if errorMsg ~= nil and txts.label then
        local ec = COLOR:CreateColorFromARGB(255, 255, 80, 80)
        txts.label:GetText(errorMsg, false, 1600, ec):DrawAtAnchor(960, 1000, "center")
    end
end

-- ── Update ────────────────────────────────────────────────────────────────────
-- Returns "back", a result table, or nil.

function M.update()
    -- Tick roll animation counters
    for r = 1, 3 do
        local roll = rolls[r]
        if roll.animCounter then
            roll.animCounter:Tick()
            if roll.animCounter.Value >= 1 then roll.animCounter = nil end
        end
    end

    -- Tick Go bounce counter
    if goCounter then goCounter:Tick() end

    -- Tick error timer; any input dismisses the error early
    if errorTimer ~= nil then
        errorTimer:Tick()
        if errorTimer.Value >= 1 then
            errorMsg   = nil
            errorTimer = nil
        end
    end

    local pressDecide = INPUT:Pressed("Decide")  or INPUT:KeyboardPressed("Return")
    local pressCancel = INPUT:Pressed("Cancel")  or INPUT:KeyboardPressed("Escape")
    local pressRight  = INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow")
    local pressLeft   = INPUT:Pressed("LeftChange")  or INPUT:KeyboardPressed("LeftArrow")

    -- While an error is displayed, consume all input to dismiss it
    if errorMsg ~= nil then
        if pressDecide or pressCancel or pressRight or pressLeft then
            errorMsg   = nil
            errorTimer = nil
        end
        return nil
    end

    -- Rounds selection step (VS only, between Songs and Go)
    if showRounds then
        if pressRight then
            roundCount = roundCount + roundStep
            SHARED:GetSharedSound("Skip"):Play()
        elseif pressLeft then
            if roundCount > roundStep then
                roundCount = roundCount - roundStep
                SHARED:GetSharedSound("Skip"):Play()
            end
        elseif pressDecide then
            exitRoundsStep()
            validateAndStart()
        elseif pressCancel then
            exitRoundsStep()
            SHARED:GetSharedSound("Cancel"):Play()
        end
        return nil
    end

    -- Go confirmation overlay
    if showGo then
        if pressDecide then
            return buildResult()
        elseif pressCancel then
            stopGo()
            SHARED:GetSharedSound("Cancel"):Play()
        end
        return nil
    end

    -- Roll navigation
    if pressRight then
        navigate(activeRoll, 1)
        if activeRoll == 1 then snapPlayers() end
    elseif pressLeft then
        navigate(activeRoll, -1)
        if activeRoll == 1 then snapPlayers() end
    elseif pressDecide then
        if activeRoll < 3 then
            activeRoll = activeRoll + 1
            SHARED:GetSharedSound("Decide"):Play()
        else
            local mode = rolls[1].items[rolls[1].selectedIdx].value
            if mode == "VS" then
                enterRoundsStep()
            else
                validateAndStart()
            end
        end
    elseif pressCancel then
        if activeRoll == 1 then
            return "back"
        else
            if showGo then
                stopGo()
            else
                activeRoll = activeRoll - 1
            end
            SHARED:GetSharedSound("Cancel"):Play()
        end
    end

    return nil
end

return M
