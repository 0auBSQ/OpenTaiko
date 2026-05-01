---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- game.lua — Core game logic for intro_nokon (Endurance and VS modes).
--
-- Game.start(config) begins a session.
-- Game.update() / Game.draw() called every frame.
-- Game.update() returns "exit" when the player leaves the result screen.

local M = {}

local Stage    = nil
local Dialogue = nil
local utils    = nil   -- injected from Script.lua
local txts     = {}

-- ── State ─────────────────────────────────────────────────────────────────────

local state      = "idle"
local gameMode   = "Endurance"
local numPlayers = 1
local playerNames = {}
local currentRound = 1
local totalRounds  = 999

-- Song/quiz references (set via utils.selectRandomSong)
local correctUniqueId = nil

-- Wait helper
local waitCounter = nil
local waitDoneCb  = nil

-- Endurance/VS score (indexed by player 1-5)
local scores = {}

-- Clock counter (0→1 over N seconds, value >= 1 means expired)
local clockCtr    = nil
local clockMs     = 0   -- kept in sync by counter listener
local clockActive = false

-- VS state
local vsMaxScore       = 10
local vsRoundScore     = 10
local vsScoreDropCtr   = nil
local vsAnsweringPlayer = 0
local vsBuzzScore      = 0
local vsDenied         = {}

-- Endurance: available genres cache
local availGenres    = {}
local selectedGenre  = nil

-- VS: genre selection
local vsGenres       = {}
local vsGenreIdx     = 1

-- Quiz list navigation (page cache for stage display)
local pageCache = {}

-- Endurance end-of-game data
local highScores = {}

-- ── Helpers ───────────────────────────────────────────────────────────────────

local function pick(t)
    if #t == 0 then return "" end
    return t[math.random(#t)]
end

local function setState(s) state = s end

local function startWait(seconds, onDone)
    waitDoneCb  = onDone
    waitCounter = COUNTER:CreateCounterDuration(0, 1, seconds)
    waitCounter:Start()
end

local function tickWait()
    if waitCounter == nil then return end
    waitCounter:Tick()
    if waitCounter.Value >= 1 then
        waitCounter = nil
        local f = waitDoneCb
        waitDoneCb = nil
        if f then f() end
    end
end

local function startClock(ms)
    clockMs     = ms
    clockActive = true
    -- counter: 0→ms over ms/1000 seconds.  interval = (ms/1000)/ms = 1/1000
    clockCtr = COUNTER:CreateCounter(0, ms, 1 / 1000)
    clockCtr:Listen(function(v) clockMs = math.max(0, ms - v) end)
    clockCtr:Start()
    Stage.setClock(clockMs, true)
end

local function tickClock()
    if not clockActive or clockCtr == nil then return end
    clockCtr:Tick()
    Stage.setClock(clockMs, true)
end

local function stopClock()
    clockActive = false
    clockCtr    = nil
    Stage.setClock(nil, false)
end

local function moveList(dir)
    local ql = utils.getQuizList()
    if ql == nil then return end
    ql:Move(dir)
    pageCache = utils.buildPageCache()
    Stage.setSongList(pageCache, 0)
    SHARED:GetSharedSound("Skip"):Play()
end

local function sub(text)
    for i = 1, 5 do
        text = text:gsub("{Player " .. i .. " name}", playerNames[i] or ("Player " .. i))
    end
    return text
end

local function showDialogue(blocks, onDone, opacity)
    local processed = {}
    for _, b in ipairs(blocks) do
        processed[#processed + 1] = { text = sub(b.text or ""), onConfirm = b.onConfirm }
    end
    Dialogue.show(processed, onDone, opacity or 0)
end

-- ── Forward declarations (mutual recursion) ───────────────────────────────────

local eStartTurn
local vsStartTurn
local vsNextRound

-- ── Curtain sequences ─────────────────────────────────────────────────────────

local function curtainCloseTo(onClosed)
    setState("curtain_closing")
    Stage.closeCurtain(function()
        setState("idle")
        if onClosed then onClosed() end
    end)
end

-- ── Opening ceremony ──────────────────────────────────────────────────────────

local function runCeremony(onDone)
    setState("ceremony")
    if gameMode == "Endurance" then
        showDialogue({
            { text = "Welcome to today's Intro Nokon session!\nI am Nokon and we are here for an Endurance session!" },
            { text = "Here today's challenger, {Player 1 name}, will show us their musical knowledge!\nLet's cheer them and wish them good luck!",
              onConfirm = function() Stage.setSpotlight(1, true) end },
            { text = "Alright, quiz time!\nIt's shoooooowtime!",
              onConfirm = function() Stage.setSpotlight(1, false) end },
        }, onDone, 0)
    else
        -- VS: "Let me present..." confirms to spotlight player 1,
        -- each name block turns off its spotlight and turns on the next one.
        local blocks = {
            { text = "Welcome to today's Intro Nokon session!\nI am Nokon and we are here for a VS session!" },
            { text = "Let me present today's challengers...",
              onConfirm = function() Stage.setSpotlight(1, true) end },
        }
        for i = 1, numPlayers do
            local p = i
            local cb
            if p < numPlayers then
                cb = function()
                    Stage.setSpotlight(p, false)
                    Stage.setSpotlight(p + 1, true)
                end
            else
                cb = function() Stage.setSpotlight(p, false) end
            end
            blocks[#blocks + 1] = { text = "{Player " .. p .. " name}", onConfirm = cb }
        end
        blocks[#blocks + 1] = { text = "Who will win? We'll know it very soon...\nLet's cheer them and wish them good luck!" }
        blocks[#blocks + 1] = { text = "Alright, quiz time!\nIt's shoooooowtime!" }
        showDialogue(blocks, onDone, 0)
    end
end

-- ── Endurance ─────────────────────────────────────────────────────────────────

eStartTurn = function()
    Stage.clearRoundState()
    Stage.setChip(nil)
    Stage.setSongList(nil, nil)
    Stage.setShowFail(false)
    Stage.setDarken(false)
    stopClock()

    availGenres = utils.getGenres()
    if #availGenres == 0 then setState("e_gameover"); return end

    local genre = availGenres[math.random(#availGenres)]
    if not utils.selectRandomSong(genre) then setState("e_gameover"); return end

    local sel = utils.getSelectedSong()
    correctUniqueId = sel and sel.UniqueId or nil
    Stage.setBlackboard(sel and sel.Genre or "???")
    utils.playSound("Question")  -- play when the genre name appears on the blackboard
    setState("e_genre")

    startWait(1, function()
        utils.startPreview()
        startClock(10000)
        setState("e_listen")
    end)
end

local function eEnterAnswer()
    stopClock()
    utils.playSound("Answering")
    Stage.setPlayerPressing(1, true)
    utils.stopPreview()
    Stage.setBlackboard(nil)
    pageCache = utils.buildPageCache()
    Stage.setSongList(pageCache, 0)
    Stage.setChip(1)
    startClock(30000)
    setState("e_answer")
end

local function eEvalCorrect()
    stopClock()
    utils.playSound("Right")
    Stage.setPlayerPressing(1, false)
    Stage.setSongList(nil, nil)
    Stage.setSpotlight(1, true)
    scores[1] = (scores[1] or 0) + 1
    Stage.setScore(1, scores[1])
    setState("e_correct")
    showDialogue({
        { text = pick({"Splendiferous!", "Greeeeeeat!", "Good answer!", "Very for real!", "Sheeeeeeeesh!"}),
          onConfirm = function() Stage.setSpotlight(1, false) end },
    }, function() eStartTurn() end, 0)
end

local function eEvalFail()
    stopClock()
    utils.playSound("Wrong")
    Stage.setPlayerPressing(1, false)
    Stage.setSongList(nil, nil)
    Stage.setDarken(true)
    Stage.setShowFail(true)
    setState("e_fail")
    showDialogue({
        { text = pick({
            "Too bad! I guess I cannot bet on a losing horse!",
            "Come on, be for real!",
            "So you prefer dogs? Sorry, we do not do that here!",
            "And that's a wrap! Time to end the show!",
        }) },
    }, function()
        Stage.setDarken(false)
        Stage.setShowFail(false)
        curtainCloseTo(function()
            utils.playSound("ResultsSolo")
            utils.saveHighScore(scores[1] or 0, playerNames[1] or "Player 1")
            highScores = utils.getHighScores()
            setState("e_gameover")
        end)
    end, 0.6)
end

-- ── VS ────────────────────────────────────────────────────────────────────────

local function vsAllDenied()
    for i = 1, numPlayers do
        if not vsDenied[i] then return false end
    end
    return true
end

local function vsTurnPlayer()
    return ((currentRound - 1) % numPlayers) + 1
end

vsNextRound = function()
    currentRound = currentRound + 1
    if currentRound > totalRounds then
        curtainCloseTo(function()
            utils.playSound("ResultsMulti")
            setState("vs_results")
        end)
    else
        vsStartTurn()
    end
end

local function vsEvalFail()
    stopClock()
    vsScoreDropCtr = nil
    utils.stopPreview()
    utils.playSound("Wrong")
    Stage.setSongList(nil, nil)
    Stage.setBlackboard(nil)
    Stage.clearRoundState()
    Stage.setDarken(true)
    Stage.setShowFail(true)
    setState("vs_fail")
    showDialogue({
        { text = pick({
            "No one found it?... Really?...",
            "Come on, be for real!",
            "And a skewer of loosers, chief!",
            "I have no words...",
        }) },
    }, function()
        Stage.setDarken(false)
        Stage.setShowFail(false)
        vsNextRound()
    end, 0.4)
end

local function vsEvalWin(winner, score)
    stopClock()
    vsScoreDropCtr = nil
    utils.stopPreview()
    utils.playSound("Right")
    Stage.setSongList(nil, nil)
    Stage.setBlackboard(nil)
    Stage.clearRoundState()
    Stage.setSpotlight(winner, true)
    scores[winner] = (scores[winner] or 0) + score
    Stage.setScore(winner, scores[winner])
    setState("vs_win")
    local wname = playerNames[winner] or ("Player " .. winner)
    showDialogue({
        { text = pick({
            "And that's another W for " .. wname .. "!",
            wname .. " takes the bag! Brutal!",
            "I always knew " .. wname .. " was the best!",
        }),
          onConfirm = function() Stage.setSpotlight(winner, false) end },
    }, function() vsNextRound() end, 0)
end

vsStartTurn = function()
    Stage.clearRoundState()
    Stage.setChip(nil)
    Stage.setSongList(nil, nil)
    Stage.setGenreList(nil, nil)
    Stage.setClock(nil, false)
    Stage.setShowFail(false)
    Stage.setDarken(false)
    vsDenied = {}
    vsAnsweringPlayer = 0
    stopClock()

    vsGenres = utils.getGenres()
    if #vsGenres == 0 then vsEvalFail(); return end

    -- Randomly pick up to 5 genres (partial Fisher-Yates)
    if #vsGenres > 5 then
        for i = 1, 5 do
            local j = math.random(i, #vsGenres)
            vsGenres[i], vsGenres[j] = vsGenres[j], vsGenres[i]
        end
        while #vsGenres > 5 do table.remove(vsGenres) end
    end
    vsGenreIdx = 1

    -- Build genre page cache centered on vsGenreIdx
    local function rebuildGenreCache()
        local cache = {}
        for off = -5, 5 do
            local idx = vsGenreIdx + off
            if idx >= 1 and idx <= #vsGenres then
                local g = vsGenres[idx]
                local maxSc = utils.calculateMaxScore(g)
                local label = (g.Title or "?") .. " (" .. maxSc .. ")"
                cache[off] = {
                    node = g,
                    text = txts.label and txts.label:GetText(label, false, 750,
                        off == 0 and COLOR:CreateColorFromARGB(255, 242, 207, 1) or nil) or nil,
                }
            end
        end
        Stage.setGenreList(cache, 0)
    end

    rebuildGenreCache()
    utils.playSound("Question")  -- play when genre selection appears
    Stage.setChip(vsTurnPlayer())
    setState("vs_genre")

    -- Store rebuild fn for navigation
    M._vsRebuildGenre = rebuildGenreCache
end

-- ── Public API ────────────────────────────────────────────────────────────────

function M.init(stageRef, dialogueRef, utilsRef, txtsRef)
    Stage    = stageRef
    Dialogue = dialogueRef
    utils    = utilsRef
    txts     = txtsRef
end

function M.start(config)
    gameMode    = config.mode    or "Endurance"
    numPlayers  = config.players or 1
    playerNames = config.names   or {}
    totalRounds = gameMode == "Endurance" and 999 or 5
    currentRound = 1
    scores       = {}
    for i = 1, 5 do scores[i] = 0 end
    vsDenied = {}

    utils.loadSongList(config.songs)
    Stage.reset(numPlayers)  -- starts with curtain closed
    for i = 1, numPlayers do Stage.setScore(i, 0) end

    -- Curtain was pre-closed by the setup→game transition; open to reveal stage
    Stage.openCurtain(function()
        runCeremony(function()
            if gameMode == "Endurance" then
                eStartTurn()
            else
                vsStartTurn()
            end
        end)
    end)
end

-- ── Draw ──────────────────────────────────────────────────────────────────────

function M.draw()
    Stage.draw()
    Dialogue.draw()
    Stage.drawFail()  -- over dialogue's darkening overlay

    -- VS round counter (always visible during play)
    if gameMode == "VS" and state ~= "vs_results" and state ~= "idle" and state ~= "ceremony" then
        if txts.label then
            txts.label:GetText("Round " .. currentRound .. " / " .. totalRounds, false, 500)
                :DrawAtAnchor(960, 28, "center")
        end
    end

    -- Result screen text (rendered over the closed curtain)
    if state == "e_gameover" or state == "vs_results" then
        if txts.title then
            local header = state == "e_gameover" and "Game Over!" or "Final Results!"
            txts.title:GetText(header, false, 1000):DrawAtAnchor(960, 200, "center")
        end
        if txts.label then
            if state == "e_gameover" then
                local s = txts.label:GetText("Score: " .. (scores[1] or 0), false, 800)
                s:DrawAtAnchor(960, 320, "center")
                -- High scores
                local hs = txts.label:GetText("High Scores", false, 600)
                hs:DrawAtAnchor(960, 410, "center")
                local playerScore = scores[1] or 0
                for i, entry in ipairs(highScores) do
                    if i > 8 then break end
                    local isPlayer = (entry.score == playerScore and entry.name == (playerNames[1] or ""))
                    local ecol = isPlayer and COLOR:CreateColorFromARGB(255, 242, 207, 1) or nil
                    local et = txts.label:GetText(i .. ". " .. entry.name .. "  " .. entry.score, false, 700, ecol)
                    et:DrawAtAnchor(960, 450 + (i - 1) * 40, "center")
                end
            else
                -- VS: sort and show scores
                local sorted = {}
                for i = 1, numPlayers do sorted[#sorted + 1] = {p=i, s=scores[i] or 0} end
                table.sort(sorted, function(a, b) return a.s > b.s end)
                for rank, entry in ipairs(sorted) do
                    local nm = playerNames[entry.p] or ("P" .. entry.p)
                    local t = txts.label:GetText(rank .. ". " .. nm .. "  " .. entry.s, false, 800)
                    t:DrawAtAnchor(960, 280 + rank * 60, "center")
                end
            end
            txts.label:GetText("Decide: Play again   Cancel: Exit", false, 1000)
                :DrawAtAnchor(960, 900, "center")
        end
    end
end

-- ── Update ────────────────────────────────────────────────────────────────────
-- Returns "exit" when the player dismisses the result/gameover screen.

function M.update()
    Stage.update()
    Dialogue.update()
    tickWait()
    tickClock()

    -- Don't handle game input while dialogue or curtain is animating
    if Dialogue.isActive() or Stage.isCurtainMoving() then return nil end
    -- Also skip during wait
    if waitCounter ~= nil then return nil end

    local decide = INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return")
    local cancel = INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape")

    -- ── Result / gameover ─────────────────────────────────────────────────────

    if state == "e_gameover" or state == "vs_results" then
        if decide or cancel then
            local sfx = state == "e_gameover" and "ResultsSolo" or "ResultsMulti"
            utils.stopSound(sfx)
            setState("idle")
            return decide and "play_again" or "exit"
        end
        return nil
    end

    -- ── Endurance ─────────────────────────────────────────────────────────────

    if state == "e_listen" then
        if clockMs <= 0 or decide then
            clockActive = false
            eEnterAnswer()
        end

    elseif state == "e_answer" then
        if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then moveList(1)
        elseif INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then moveList(-1)
        end
        if clockMs <= 0 or decide then
            clockActive = false
            local sel = utils.getQuizList() and utils.getQuizList():GetSelectedSongNode()
            if sel and correctUniqueId and sel.UniqueId == correctUniqueId then
                eEvalCorrect()
            else
                eEvalFail()
            end
        end

    -- ── VS ────────────────────────────────────────────────────────────────────

    elseif state == "vs_genre" then
        if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
            vsGenreIdx = math.min(vsGenreIdx + 1, #vsGenres)
            if M._vsRebuildGenre then M._vsRebuildGenre() end
            SHARED:GetSharedSound("Skip"):Play()
        elseif INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
            vsGenreIdx = math.max(vsGenreIdx - 1, 1)
            if M._vsRebuildGenre then M._vsRebuildGenre() end
            SHARED:GetSharedSound("Skip"):Play()
        elseif decide then
            local genre = vsGenres[vsGenreIdx]
            Stage.setGenreList(nil, nil)
            Stage.setChip(nil)
            SHARED:GetSharedSound("Decide"):Play()
            if not utils.selectRandomSong(genre) then vsEvalFail(); return nil end

            local sel = utils.getSelectedSong()
            correctUniqueId = sel and sel.UniqueId or nil
            vsMaxScore  = utils.calculateMaxScore(genre)
            vsRoundScore = vsMaxScore
            vsDenied    = {}

            Stage.setBlackboard(sel and sel.Genre or "???")
            setState("vs_pregame")
            startWait(1, function()
                utils.startPreview()
                Stage.setBlackboard(tostring(vsRoundScore))
                vsScoreDropCtr = COUNTER:CreateCounterDuration(0, 1, 2)
                vsScoreDropCtr:Start()
                setState("vs_listen")
            end)
        end

    elseif state == "vs_listen" then
        -- Score drop
        if vsScoreDropCtr then
            vsScoreDropCtr:Tick()
            if vsScoreDropCtr.Value >= 1 then
                local drop = math.max(1, math.floor(vsMaxScore * 0.1))
                vsRoundScore = math.max(0, vsRoundScore - drop)
                Stage.setBlackboard(tostring(vsRoundScore))
                if vsRoundScore <= 0 then vsScoreDropCtr = nil; vsEvalFail(); return nil end
                vsScoreDropCtr = COUNTER:CreateCounterDuration(0, 1, 2)
                vsScoreDropCtr:Start()
            end
        end
        if vsAllDenied() then vsEvalFail(); return nil end

        -- Buzz-in
        for p = 1, numPlayers do
            local buzzed = false
            if p == 1 then buzzed = decide end
            if p == 2 then buzzed = INPUT:Pressed("RRed2P") end
            if p == 3 then buzzed = INPUT:Pressed("RRed3P") end
            if p == 4 then buzzed = INPUT:Pressed("RRed4P") end
            if p == 5 then buzzed = INPUT:Pressed("RRed5P") end
            if buzzed and not vsDenied[p] then
                vsScoreDropCtr = nil
                vsAnsweringPlayer = p
                vsBuzzScore = vsRoundScore
                utils.stopPreview()
                utils.playSound("Answering")
                Stage.setPlayerPressing(p, true)
                pageCache = utils.buildPageCache()
                Stage.setSongList(pageCache, 0)
                Stage.setChip(p)
                startClock(30000)
                setState("vs_answer")
                break
            end
        end

    elseif state == "vs_answer" then
        if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then moveList(1)
        elseif INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then moveList(-1)
        end
        if clockMs <= 0 or decide then
            stopClock()
            Stage.setPlayerPressing(vsAnsweringPlayer, false)
            Stage.setSongList(nil, nil)
            local sel = utils.getQuizList() and utils.getQuizList():GetSelectedSongNode()
            if sel and correctUniqueId and sel.UniqueId == correctUniqueId then
                vsEvalWin(vsAnsweringPlayer, vsBuzzScore)
            else
                -- Wrong: deny player, fixed 10-point penalty; also reduce prize pool
                vsDenied[vsAnsweringPlayer] = true
                Stage.setPlayerDenied(vsAnsweringPlayer, true)
                scores[vsAnsweringPlayer] = math.max(0, (scores[vsAnsweringPlayer] or 0) - 10)
                Stage.setScore(vsAnsweringPlayer, scores[vsAnsweringPlayer])
                utils.playSound("Wrong")
                local drop = math.max(1, math.floor(vsMaxScore * 0.1))
                vsRoundScore = math.max(0, vsRoundScore - drop)
                Stage.setBlackboard(tostring(vsRoundScore))
                Stage.setChip(nil)
                if vsAllDenied() or vsRoundScore <= 0 then
                    vsEvalFail()
                else
                    utils.startPreview()
                    utils.playSound("Question")
                    vsScoreDropCtr = COUNTER:CreateCounterDuration(0, 1, 2)
                    vsScoreDropCtr:Start()
                    setState("vs_listen")
                end
            end
        end
    end

    return nil
end

return M
