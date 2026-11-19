---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- intro_nokon setup screen (Mode / Players / Songs)

local I18N = require("i18n")
local NavInput = require("NavInput")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local SetupLayout, SetupAnim = Layout.setup, Animation.setup
local Rolls, Rounds = Layout.setup.rolls, Layout.setup.rounds

local M = {}

local tx         = {}   -- shared textures from Script.lua
local sounds     = {}   -- shared sounds
local txts       = {}   -- txts.title, txts.label
local countSongs = nil  -- function(scope) -> int, from Script.lua

local errorMsg   = nil
local errorTimer = nil

-- ==== Constants ====

local SCALE_SEL   = Rolls.selected_scale
local SCALE_UNSEL = Rolls.other_scale
local ANIM_DUR    = SetupAnim.zoom_s   -- s, zoom
local MAX_GAP     = Rolls.max_gap    -- px, max item gap

-- roll centre y
local ROLL_Y = Rolls.y

local WHITE = "ffffffff"

-- ==== Rolls ====

local function makeRolls()
    return {
        {
            label = "Mode",
            items = {
                { key = "mode_endurance", value = "Endurance" },
                { key = "mode_vs",        value = "VS"        },
            },
            selectedIdx = 1,
            animCounter = nil, animFromIdx = nil, animToIdx = nil,
        },
        {
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
        {
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

-- ==== Helpers ====

local function col(hex) return COLOR:CreateColorFromHex(hex) end

local function isAvailable(r, i)
    if r ~= 2 then return true end
    local modeIdx = rolls[1].selectedIdx
    if modeIdx == 1 then return i == 1 end   -- Endurance: only 1P
    return i >= 2                            -- VS: 2-5P
end

-- item centre x values
local function itemPositions(r)
    local items = rolls[r].items
    local n     = #items
    local maxW  = 0
    for _, item in ipairs(items) do
        local t = tx[item.key]
        if t and t.Width > maxW then maxW = t.Width end
    end
    if maxW == 0 then maxW = Rolls.item_fallback end

    -- gap so the total fits max_total_w
    local gap = math.max(Rolls.min_gap, math.min(MAX_GAP, math.floor((Rolls.max_total_w - n * maxW) / math.max(1, n - 1))))
    local totalW = n * maxW + (n - 1) * gap
    local x0     = Rolls.center_x - totalW / 2

    local pos = {}
    for i = 1, n do
        pos[i] = x0 + (i - 1) * (maxW + gap) + maxW / 2
    end
    return pos
end

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

local function changeSelection(r, newIdx)
    local roll = rolls[r]
    if newIdx == roll.selectedIdx then return end
    roll.animFromIdx = roll.selectedIdx
    roll.animToIdx   = newIdx
    roll.selectedIdx = newIdx
    roll.animCounter = COUNTER:CreateCounterDuration(0, 1, ANIM_DUR)
    roll.animCounter:Start()
end

-- after a Mode change: first valid Players option
local function snapPlayers()
    local modeIdx = rolls[1].selectedIdx
    local pRoll   = rolls[2]
    if modeIdx == 1 and pRoll.selectedIdx ~= 1 then
        changeSelection(2, 1)
    elseif modeIdx == 2 and pRoll.selectedIdx == 1 then
        changeSelection(2, 2)
    end
end

-- skips unavailable items
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
    goCounter = COUNTER:CreateCounter(0, 255, SetupAnim.go_flash_half_s / 255)
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
    errorTimer = COUNTER:CreateCounterDuration(0, 1, SetupAnim.error_s)
    errorTimer:Start()
end

local function validateAndStart()
    if countSongs == nil then startGo(); return end
    local cfg   = buildResult()
    local count = countSongs(cfg.songs)
    local ok    = true
    if cfg.mode == "Endurance" then
        -- 1 song: egg mode; 0 or 2-4: too few
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

-- ==== Init / lifecycle ====

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

    -- resume BGM (back from results)
    if not sounds.BGM.IsPlaying then
        sounds.BGM:SetLoop(true)
        sounds.BGM:Play()
    end
end

-- ==== Draw ====

local function drawRoll(r, isActive)
    local roll   = rolls[r]
    local rollY  = ROLL_Y[r]
    local items  = roll.items
    local pos    = itemPositions(r)

    if txts.label then
        local lc = isActive and col(Rolls.active_label_color) or col(Rolls.label_color)
        local lt = txts.label:GetText(I18N.tr(roll.label), false, Rolls.label_max_width, lc)
        -- label above the tallest image
        local maxH = 0
        for _, item in ipairs(items) do
            local t = tx[item.key]
            if t and t.Height > maxH then maxH = t.Height end
        end
        if maxH == 0 then maxH = Rolls.item_fallback end
        lt:DrawAtAnchor(Rolls.center_x, rollY - maxH * SCALE_SEL / 2 - Rolls.label_gap, "center")
    end

    local baseOp = isActive and 1.0 or Rolls.inactive_opacity

    for i, item in ipairs(items) do
        local t = tx[item.key]
        if t and t.Width > 0 then
            local scale = itemScale(r, i)
            local avail = isAvailable(r, i)
            local sel   = (i == roll.selectedIdx)

            if not avail then
                t:SetColor(col(Rolls.unavailable_color))
            elseif sel then
                t:SetColor(col(Rolls.selected_color))
            else
                t:SetColor(col(Rolls.other_color))
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
    if tx["Background"] then tx["Background"]:Draw(0, 0) end

    if txts.title then
        local Title = SetupLayout.title
        local tt = txts.title:GetText(I18N.tr("Let's set up the game show!"), false, Title.max_width, col(Title.color))
        tt:Draw(Title.x, Title.y)
    end

    -- no active highlight during the rounds step
    for r = 1, 3 do
        drawRoll(r, r == activeRoll and not showRounds)
    end

    -- rounds panel (VS, after Songs)
    if showRounds then
        local bgt = tx["bgtile"]
        if bgt ~= nil and bgt.Loaded then
            local res = THEME:GetResolution()
            bgt:SetOpacity(Rounds.veil_opacity)
            for rx = 0, math.ceil(res.X / bgt.Width) - 1 do
                for ry = 0, math.ceil(res.Y / bgt.Height) - 1 do
                    bgt:Draw(rx * bgt.Width, ry * bgt.Height)
                end
            end
            bgt:SetOpacity(1)
        end
        if txts.title then
            txts.title:GetText(I18N.tr("Round Count"), false, Rounds.title_max_width, col(Rounds.title_color)):DrawAtAnchor(Rounds.x, Rounds.title_y, "center")
        end
        if txts.label then
            local canDec = roundCount > roundStep
            local arrow_l = canDec and "◀   " or "      "
            txts.label:GetText(arrow_l .. tostring(roundCount) .. "   ▶", false, Rounds.count_max_width, col(Rounds.count_color))
                :DrawAtAnchor(Rounds.x, Rounds.count_y, "center")
        end
    end

    if showGo and tx["go"] then
        tx["go"]:SetOpacity(goOpacity / 255)
        tx["go"]:Draw(SetupLayout.go.x, SetupLayout.go.y)
        tx["go"]:SetOpacity(1)
    end

    if errorMsg ~= nil and txts.label then
        local Err = SetupLayout.error
        local ec = Config.color(Err.color)
        txts.label:GetText(errorMsg, false, Err.max_width, ec):DrawAtAnchor(Err.x, Err.y, "center")
    end
end

-- ==== Update ====
-- returns "back", a result table, or nil

function M.update()
    for r = 1, 3 do
        local roll = rolls[r]
        if roll.animCounter then
            roll.animCounter:Tick()
            if roll.animCounter.Value >= 1 then roll.animCounter = nil end
        end
    end

    if goCounter then goCounter:Tick() end

    -- any input dismisses the error
    if errorTimer ~= nil then
        errorTimer:Tick()
        if errorTimer.Value >= 1 then
            errorMsg   = nil
            errorTimer = nil
        end
    end

    local pressDecide = NavInput.decide()
    local pressCancel = NavInput.cancel()
    local pressRight  = NavInput.right()
    local pressLeft   = NavInput.left()

    -- error shown: input only dismisses it
    if errorMsg ~= nil then
        if pressDecide or pressCancel or pressRight or pressLeft then
            errorMsg   = nil
            errorTimer = nil
        end
        return nil
    end

    -- rounds step (VS, between Songs and Go)
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

    if showGo then
        if pressDecide then
            return buildResult()
        elseif pressCancel then
            stopGo()
            SHARED:GetSharedSound("Cancel"):Play()
        end
        return nil
    end

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
            SHARED:GetSharedSound("Cancel"):Play()
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
