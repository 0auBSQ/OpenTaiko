---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- game.lua — Core game logic for intro_nokon (Endurance and VS modes).
--
-- Game.start(config) begins a session.
-- Game.update() / Game.draw() called every frame.
-- Game.update() returns "exit" when the player leaves the result screen.

local I18N = require("i18n")
local NavInput = require("NavInput")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Results, DialogueLayout = Layout.results, Layout.dialogue

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

-- Single-song egg mode (Endurance with exactly 1 song in scope)
local singleSongMode = false

-- Enter = universal decide for any player; Escape = universal cancel.
local function pDecide(p)
    local inp = NavInput.p[p]
    if inp == nil then return false end
    return inp.decide()
end

local function pRight(p)
    local inp = NavInput.p[p]
    if inp == nil then return false end
    return inp.right(p == 1)
end

local function pLeft(p)
    local inp = NavInput.p[p]
    if inp == nil then return false end
    return inp.left(p == 1)
end

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
        processed[#processed + 1] = { text = sub(I18N.tr(b.text or "")), onConfirm = b.onConfirm }
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
    Stage.setBlackboard(genre.Title or "???")
    utils.playSound("Question")  -- play when the genre name appears on the blackboard
    setState("e_genre")

    startWait(Animation.game.genre_reveal_s, function()
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
    local sel = utils.getSelectedSong()
    local songTitle = (sel and sel.Title) or "?"
    local line
    if singleSongMode and scores[1] >= 100 then
        line = pick({
            "Do you feel smart? How will you save your score Scherlock?",
            "Are you enjoying the game? Perfect, because you are here forever!",
            "You did beat my score? Wait until you reach the results first...",
            "...",
        })
    else
        line = I18N.trf(pick({
            "Correct, it was \"%s\"! Splendiferous!",
            "\"%s\" it was! Greeeeeeat!",
            "Yes, \"%s\"! Good answer!",
            "It was \"%s\"! Very for real!",
            "\"%s\" indeed! Sheeeeeeeesh!",
        }), songTitle)
    end
    showDialogue({
        { text = line, onConfirm = function() Stage.setSpotlight(1, false) end },
    }, function() eStartTurn() end, 0)
end

local function eEvalFail()
    stopClock()
    utils.playSound("Wrong")
    Stage.setPlayerPressing(1, false)
    Stage.setSongList(nil, nil)
    -- reveal the correct answer on the stage panel
    local sel = utils.getSelectedSong()
    Stage.setBlackboard(sel and sel.Title or nil)
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
            if (scores[1] or 0) > 100 then utils.grantPrideKey() end
            utils.saveHighScore(scores[1] or 0, playerNames[1] or "Player 1")
            highScores = utils.getHighScores()
            setState("e_gameover")
        end)
    end, DialogueLayout.fail_dim_opacity)
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
    -- nobody found it in time — reveal the correct answer on the stage panel
    local sel = utils.getSelectedSong()
    Stage.setBlackboard(sel and sel.Title or nil)
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
    end, DialogueLayout.vs_fail_dim_opacity)
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
    local wsel = utils.getSelectedSong()
    local wtitle = (wsel and wsel.Title) or "?"
    showDialogue({
        { text = I18N.trf(pick({
            "And that's another W for %s! The song was \"%s\"!",
            "%s takes the bag with \"%s\"! Brutal!",
            "I always knew %s was the best! It was \"%s\"!",
        }), wname, wtitle),
          onConfirm = function() Stage.setSpotlight(winner, false) end },
    }, function() vsNextRound() end, 0)
end

vsStartTurn = function()
    Stage.clearRoundState()
    Stage.setChip(nil)
    Stage.setBlackboard(nil)
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
        local rows = Layout.list.rows_each_side
        for off = -rows, rows do
            local idx = vsGenreIdx + off
            if idx >= 1 and idx <= #vsGenres then
                local g = vsGenres[idx]
                local maxSc = utils.calculateMaxScore(g)
                local label = (g.Title or "?") .. " (" .. maxSc .. ")"
                cache[off] = {
                    node = g,
                    text = txts.label and txts.label:GetText(label, false, Layout.list.genre_max_width,
                        off == 0 and Config.color(Layout.list.highlight_color) or nil) or nil,
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
    gameMode       = config.mode    or "Endurance"
    numPlayers     = config.players or 1
    playerNames    = config.names   or {}
    singleSongMode = config.singleSong or false
    totalRounds    = gameMode == "Endurance" and 999 or (config.rounds or 5)
    currentRound   = 1
    scores         = {}
    for i = 1, 5 do scores[i] = 0 end
    vsDenied = {}

    utils.loadSongList(config.songs, singleSongMode)
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
            local Counter = Layout.round_counter
            txts.label:GetText(I18N.trf("Round %d / %d", currentRound, totalRounds), false, Counter.max_width)
                :DrawAtAnchor(Counter.x, Counter.y, "center")
        end
    end

    -- Result screen text (rendered over the closed curtain)
    if state == "e_gameover" or state == "vs_results" then
        if txts.title then
            local header = I18N.tr(state == "e_gameover" and "Game Over!" or "Final Results!")
            txts.title:GetText(header, false, Results.title_max_width):DrawAtAnchor(Results.x, Results.title_y, "center")
        end
        if txts.label then
            if state == "e_gameover" then
                local s = txts.label:GetText(I18N.trf("Score: %d", scores[1] or 0), false, Results.score_max_width)
                s:DrawAtAnchor(Results.x, Results.score_y, "center")
                -- High scores
                local hs = txts.label:GetText(I18N.tr("High Scores"), false, Results.high_scores_max_width)
                hs:DrawAtAnchor(Results.x, Results.high_scores_y, "center")
                local playerScore = scores[1] or 0
                for i, entry in ipairs(highScores) do
                    if i > Results.high_score_rows then break end
                    local isPlayer = (entry.score == playerScore and entry.name == (playerNames[1] or ""))
                    local ecol = isPlayer and Config.color(Layout.list.highlight_color) or nil
                    local et = txts.label:GetText(i .. ". " .. entry.name .. "  " .. entry.score, false, Results.high_score_max_width, ecol)
                    et:DrawAtAnchor(Results.x, Results.high_score_y + (i - 1) * Results.high_score_gap, "center")
                end
            else
                -- VS: sort and show scores
                local sorted = {}
                for i = 1, numPlayers do sorted[#sorted + 1] = {p=i, s=scores[i] or 0} end
                table.sort(sorted, function(a, b) return a.s > b.s end)
                for rank, entry in ipairs(sorted) do
                    local nm = playerNames[entry.p] or ("P" .. entry.p)
                    local t = txts.label:GetText(rank .. ". " .. nm .. "  " .. entry.s, false, Results.vs_max_width)
                    t:DrawAtAnchor(Results.x, Results.vs_y + rank * Results.vs_gap, "center")
                end
            end
            txts.label:GetText(I18N.tr("Decide: Play again   Cancel: Exit"), false, Results.footer_max_width)
                :DrawAtAnchor(Results.x, Results.footer_y, "center")
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

    local decide = NavInput.decide()
    local cancel = NavInput.cancel()

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
        if pRight(1) then moveList(1)
        elseif pLeft(1) then moveList(-1)
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
        local tp = vsTurnPlayer()
        if pRight(tp) then
            vsGenreIdx = (vsGenreIdx % #vsGenres) + 1
            if M._vsRebuildGenre then M._vsRebuildGenre() end
            SHARED:GetSharedSound("Skip"):Play()
        elseif pLeft(tp) then
            vsGenreIdx = ((vsGenreIdx - 2 + #vsGenres) % #vsGenres) + 1
            if M._vsRebuildGenre then M._vsRebuildGenre() end
            SHARED:GetSharedSound("Skip"):Play()
        elseif pDecide(tp) then
            local genre = vsGenres[vsGenreIdx]
            Stage.setGenreList(nil, nil)
            Stage.setChip(nil)
            SHARED:GetSharedSound("Decide"):Play()
            if not utils.selectRandomSong(genre) then vsEvalFail(); return nil end

            local sel = utils.getSelectedSong()
            correctUniqueId = sel and sel.UniqueId or nil
            vsMaxScore   = utils.calculateMaxScore(genre)
            vsRoundScore = vsMaxScore
            vsDenied     = {}

            Stage.setBlackboard(genre.Title or "???")
            setState("vs_pregame")
            startWait(Animation.game.genre_reveal_s, function()
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

        -- Buzz-in: each player uses their own decide buttons
        for p = 1, numPlayers do
            if pDecide(p) and not vsDenied[p] then
                vsScoreDropCtr    = nil
                vsAnsweringPlayer = p
                vsBuzzScore       = vsRoundScore
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
        local ap = vsAnsweringPlayer
        if pRight(ap) then moveList(1)
        elseif pLeft(ap) then moveList(-1)
        end
        if clockMs <= 0 or pDecide(ap) then
            stopClock()
            Stage.setPlayerPressing(ap, false)
            Stage.setSongList(nil, nil)
            local sel = utils.getQuizList() and utils.getQuizList():GetSelectedSongNode()
            if sel and correctUniqueId and sel.UniqueId == correctUniqueId then
                vsEvalWin(ap, vsBuzzScore)
            else
                -- Wrong: deny player, fixed 10-point penalty; also reduce prize pool
                vsDenied[ap] = true
                Stage.setPlayerDenied(ap, true)
                scores[ap] = math.max(0, (scores[ap] or 0) - 10)
                Stage.setScore(ap, scores[ap])
                utils.playSound("Wrong")
                local drop = math.max(1, math.floor(vsMaxScore * 0.1))
                vsRoundScore = math.max(0, vsRoundScore - drop)
                Stage.setBlackboard(tostring(vsRoundScore))
                Stage.setChip(nil)
                if vsAllDenied() or vsRoundScore <= 0 then
                    vsEvalFail()
                else
                    utils.startPreview()
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
