local text = nil
local save = nil

local sounds = {}
local textures = {}

local songList = nil
local quizSongList = nil -- Limited list for answering
local currentSongNode = nil
local correctSongNode = nil

local active = false
local songsEnumerated = false

-- Game state machine
local state = "waiting_enum" -- waiting_enum, cutscene1, cutscene2, player_select, scope_select, intro,
                               -- round_start, genre_select, song_playing, answering, answer_reveal,
                               -- results, solo_playing, solo_answering, solo_correct, solo_results, pride_modal

-- Game settings
local numPlayers = 1
local numRounds = 5
local songScope = "All" -- "OpTk", "Customs", "All"
local originalPlayerCount = 1

-- Game progress
local currentRound = 1
local currentPlayerTurn = 1
local playerScores = {}
local soloScore = 0

-- High scores - TODO: Load from SQL database
-- Format: {name = "Player Name", score = 123}
local bestScores = {
    {name = "Nokon", score = 100},
    {name = "Ras & Berry", score = 80},
    {name = "Shadow Dancer", score = 60},
    {name = "Riku", score = 40},
    {name = "Mr. Hikaru", score = 30},
    {name = "Lyall", score = 20},
    {name = "Ume", score = 10},
    {name = "Aoi", score = 5}
}

-- UI state variables
local selectedPlayerOption = 1
local selectedScopeOption = 1
local selectedGenreIndex = 1
local answerTimer = 0
local answeringPlayer = 0
local availableGenres = {}
local currentPageCache = {}
local previewStarted = false

-- Cutscene
local cutsceneCounter = nil
local cutsceneSkipped = false

-- Timers using LuaCounters
local roundStartCounter = nil
local answerTimerCounter = nil
local celebrationCounter = nil
local prideModalCounter = nil

---------------------------------------
-- Utility Functions
---------------------------------------

local function resetGame()
    currentRound = 1
    currentPlayerTurn = 1
    playerScores = {}
    for i = 1, 5 do
        playerScores[i] = 0
    end
    soloScore = 0

    -- Reset all counters
    cutsceneCounter = nil
    roundStartCounter = nil
    answerTimerCounter = nil
    celebrationCounter = nil
    prideModalCounter = nil
    previewStarted = false
end

local function getChart(song)
    if song == nil then return nil end
    return song:GetChart(4)
end

local function isSongValid(song)
    if song == nil then return false end
    if song.IsSong == false then return false end
    local chart = getChart(song)
    return chart ~= nil
end

---------------------------------------
-- Utility Functions for C# Collections
---------------------------------------

local function csharpEnumerableToLuaArray(enumerable)
    local luaArray = {}
    if enumerable == nil then return luaArray end

    -- Check for the expected C# GetEnumerator method
    if not enumerable.GetEnumerator then
        -- Handle a case where the object might be iterable by Lua 'pairs' already,
        -- or if the C# name is different (e.g., 'getEnumerator').
        -- We'll assume the C# binding is consistent with 'GetEnumerator'.
        return luaArray -- Return empty if the method is not found.
    end

    -- Use GetEnumerator for IEnumerable (e.g., List<T>, Array, etc.)
    local enumerator = enumerable:GetEnumerator()

    -- The Current property of an IEnumerator<T> returns the item 'T', not a KeyValuePair.
    while enumerator:MoveNext() do
        -- Simply insert the current item (T) into the Lua array
        table.insert(luaArray, enumerator.Current)
    end

    return luaArray
end

---------------------------------------
-- Song List Management
---------------------------------------

local function loadMainSongList()
    local lsls = GenerateSongListSettings()
    lsls.ModuloPagination = false
    lsls.AppendMainRandomBox = false
    lsls.AppendSubRandomBoxes = false
    lsls.SubBackBoxFrequency = 0

    -- Configure based on song scope
    if songScope == "Customs" then
        lsls.RootGenreFolder = "Custom Charts"
    elseif songScope == "OpTk" then
        -- Exclude custom charts and special folders
        lsls:SetExcludedGenreFolders({"Custom Charts", "Download", "段位道場", "太鼓タワー", "Favorite", "最近遊んだ曲", "SearchD", "SearchT", "Secret Vault"})
    elseif songScope == "All" then
        -- Exclude only special system folders
        lsls:SetExcludedGenreFolders({"Download", "段位道場", "太鼓タワー", "Favorite", "最近遊んだ曲", "SearchD", "SearchT", "Secret Vault"})
    end

    songList = RequestSongList(lsls)
end

local function refreshQuizSongListCache()
    currentPageCache = {}

    if quizSongList == nil then return end

    for i = -5, 5 do
        local node = quizSongList:GetSongNodeAtOffset(i)
        if node ~= nil and node.IsSong then
            currentPageCache[i] = {
                node = node,
                text = text:GetText(node.Title, false, 99999,
                    i == 0 and COLOR:CreateColorFromARGB(255, 242, 207, 1) or nil)
            }
        else
            currentPageCache[i] = nil
        end
    end
end

local function selectRandomSongFromGenre(genreFolder)
    if genreFolder == nil then
        -- Solo mode: select from entire collection
        if songList == nil then return false end

        currentSongNode = songList:GetRandomNodeInFolder(songList:GetSelectedSongNode(), true, function(node)
		        return songList:GetSelectedSongNode().Parent ~= node.Parent
		    end)
        correctSongNode = currentSongNode

        if currentSongNode == nil or currentSongNode.IsSong == false then
            return false
        end

        -- Generate quiz list from the same genre
        local lsls = GenerateSongListSettings()
        lsls.ModuloPagination = false
        lsls.AppendMainRandomBox = false
        lsls.AppendSubRandomBoxes = false
        lsls.SubBackBoxFrequency = 0
        lsls.RootGenreFolder = currentSongNode.Genre

        quizSongList = RequestSongList(lsls)
        refreshQuizSongListCache()
        return true
    end

    -- Multiplayer mode: select from specific genre
    -- Create a new song list with the selected genre as root
    local lsls = GenerateSongListSettings()
    lsls.ModuloPagination = false
    lsls.AppendMainRandomBox = false
    lsls.AppendSubRandomBoxes = false
    lsls.SubBackBoxFrequency = 0
    lsls.RootGenreFolder = genreFolder.Genre

    local genreSongList = RequestSongList(lsls)

    if genreSongList == nil then return false end

    -- Get random song from this genre
    currentSongNode = genreSongList:GetRandomNodeInFolder(genreSongList:GetSelectedSongNode(), true)
    correctSongNode = currentSongNode

    if currentSongNode == nil or currentSongNode.IsSong == false then
        return false
    end

    -- Use the genre song list for answering
    quizSongList = genreSongList
    refreshQuizSongListCache()

    return true
end

local function startSongPreview()
    if currentSongNode == nil then return end

    local psnd = SHARED:GetSharedSound("quiz_preview")
    psnd:Stop()

    if currentSongNode.IsSong == true then
        SHARED:SetSharedPreviewUsingAbsolutePath("quiz_preview", currentSongNode.AudioPath, function(snd)
            snd:Play()
            snd:SetTimestamp(currentSongNode.DemoStart)
            snd:SetLoop(true) -- For solo mode
        end)
    end
end

local function stopSongPreview()
    local psnd = SHARED:GetSharedSound("quiz_preview")
    psnd:Stop()
end

---------------------------------------
-- Player Management
---------------------------------------

local function getAvailableGenres()
    if songList == nil then return {} end

    -- Search for folders that are not subfolders and contain songs
    local genresCSharp = songList:SearchNodesByPredicate(function(node)
        return node.IsFolder == true and
               node.ChildrenCount == node.SongCount and
               node.SongCount > 0
    end)

    -- Convert C# Enumerable to Lua array
    return csharpEnumerableToLuaArray(genresCSharp)
end

local function selectNextPlayer()
    -- TODO: Implement random or procedural player selection
    -- For now, just cycle through players
    currentPlayerTurn = currentPlayerTurn + 1
    if currentPlayerTurn > numPlayers then
        currentPlayerTurn = 1
    end
end

local function addScore(playerIndex, points)
    if playerScores[playerIndex] == nil then
        playerScores[playerIndex] = 0
    end
    playerScores[playerIndex] = playerScores[playerIndex] + points
end

---------------------------------------
-- High Score Management
---------------------------------------

local function loadHighScores()
    -- TODO: Load from SQL database via your backend
    -- This function will be called on activate()
    -- bestScores should be populated here
end

local function saveHighScore(score, playerName)
    -- Insert score into bestScores array
    table.insert(bestScores, {name = playerName, score = score})

    -- Sort by score descending
    table.sort(bestScores, function(a, b) return a.score > b.score end)

    -- Keep only top 8
    while #bestScores > 8 do
        table.remove(bestScores)
    end

    -- TODO: Save to SQL database via your backend
    -- PLACEHOLDER FOR SQL SAVE:
    -- SaveQuizHighScore(playerName, score)
end

local function checkIfNewHighScore(score)
    for i, entry in ipairs(bestScores) do
        if score > entry.score then
            return true, i -- Return if it's a high score and at what rank
        end
    end
    return false, 0
end

local function checkIfBeatNokon(score)
    -- Check if player beat Nokon's score (100)
    return score > 100
end

local function grantKeyOfPride()
    if save == nil then return end

    -- Check if already obtained
    if save:GetGlobalTrigger(".vault_key_obtained_pride") == true then
        return
    end

    save:SetGlobalTrigger(".vault_key_obtained_pride", true)

    -- Show modal
    showPrideModal = true
    prideModalTimer = 0
    state = "pride_modal"

    -- Play special sound
    if sounds.PrideKey ~= nil then
        sounds.PrideKey:Play()
    end
end

---------------------------------------
-- State Handlers
---------------------------------------

local function handleWaitingEnum()
    -- Just wait, afterSongEnum will handle transition
end

local function handleCutscene1()
    if cutsceneCounter == nil then
        cutsceneCounter = COUNTER:CreateCounterDuration(0, 1, 5) -- 5 seconds
        cutsceneCounter:Start()
    end

    cutsceneCounter:Tick()

    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") or
       INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        cutsceneSkipped = true
        sounds.Decide:Play()
    end

    -- Auto-advance after counter ends or if skipped
    if cutsceneCounter.Value >= 1 or cutsceneSkipped then
        state = "cutscene2"
        cutsceneCounter = nil
        cutsceneSkipped = false
    end
end

local function handleCutscene2()
    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
        state = "player_select"
        sounds.Decide:Play()
        selectedPlayerOption = 1 -- Reset player selection
				return false
    end

		if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        sounds.Cancel:Play()
				return true
    end
end

local function handlePlayerSelect()
    -- Select number of players (1-5) and rounds (for multiplayer)

    if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
        selectedPlayerOption = math.min(selectedPlayerOption + 1, 5)
        sounds.Skip:Play()
    end

    if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
        selectedPlayerOption = math.max(selectedPlayerOption - 1, 1)
        sounds.Skip:Play()
    end

    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
        numPlayers = selectedPlayerOption

        -- Set player count in config
        CONFIG.PlayerCount = numPlayers

        if numPlayers == 1 then
            -- Solo play, skip round selection
            state = "scope_select"
            numRounds = 999 -- Endless
        else
            -- TODO: Add round selection screen
            state = "scope_select"
            numRounds = 5 -- Default
        end

        sounds.Decide:Play()
        selectedScopeOption = 1 -- Reset scope selection
        resetGame()
    end

    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        state = "cutscene2"
        sounds.Cancel:Play()
    end
end

local function handleScopeSelect()
    -- Select song scope: OpTk, Customs, or All
    local scopes = {"OpTk", "Customs", "All"}

    if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
        selectedScopeOption = math.min(selectedScopeOption + 1, 3)
        sounds.Skip:Play()
    end

    if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
        selectedScopeOption = math.max(selectedScopeOption - 1, 1)
        sounds.Skip:Play()
    end

    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
        songScope = scopes[selectedScopeOption]

        -- Reload song list with new scope
        loadMainSongList()

        if numPlayers == 1 then
            state = "solo_intro"
        else
            state = "intro"
        end

        sounds.Decide:Play()
    end

    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        state = "player_select"
        sounds.Cancel:Play()
    end
end

local function handleIntro()
    -- Cool introduction for multiplayer
    if cutsceneCounter == nil then
        cutsceneCounter = COUNTER:CreateCounterDuration(0, 1, 3) -- 3 seconds
        cutsceneCounter:Start()
    end

    cutsceneCounter:Tick()

    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") or cutsceneCounter.Value >= 1 then
        state = "round_start"
        cutsceneCounter = nil
        currentRound = 1
        currentPlayerTurn = 1 -- Start with player 1
    end
end

local function handleRoundStart()
    -- Show which player's turn and round number
    if roundStartCounter == nil then
        roundStartCounter = COUNTER:CreateCounterDuration(0, 1, 2) -- 2 seconds
        roundStartCounter:Start()
    end

    roundStartCounter:Tick()

    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") or roundStartCounter.Value >= 1 then
        -- Load available genres for selection
        availableGenres = getAvailableGenres()
        selectedGenreIndex = 1

        state = "genre_select"
        roundStartCounter = nil
    end
end

local function handleGenreSelect()
    -- Current player selects a genre

    if #availableGenres == 0 then
        -- No genres available, skip to results
        state = "results"
        sounds.Cancel:Play()
        return
    end

    -- Navigation using LBlue/RBlue for player 1
    if currentPlayerTurn == 1 then
        if INPUT:Pressed("LBlue") or INPUT:Pressed("RBlue") or INPUT:KeyboardPressed("RightArrow") then
            selectedGenreIndex = selectedGenreIndex + 1
            if selectedGenreIndex > #availableGenres then
                selectedGenreIndex = 1
            end
            sounds.Skip:Play()
        end

        if INPUT:KeyboardPressed("LeftArrow") then
            selectedGenreIndex = selectedGenreIndex - 1
            if selectedGenreIndex < 1 then
                selectedGenreIndex = #availableGenres
            end
            sounds.Skip:Play()
        end

        if INPUT:Pressed("LRed") or INPUT:Pressed("RRed") or INPUT:KeyboardPressed("Return") then
            local selectedGenre = availableGenres[selectedGenreIndex]

            if selectedGenre ~= nil then
                local success = selectRandomSongFromGenre(selectedGenre)

                if success then
                    state = "song_playing"
                    answerTimerCounter = nil
                    answeringPlayer = 0
                    previewStarted = false
                    sounds.Decide:Play()
                else
                    sounds.Cancel:Play()
                end
            end
        end
    elseif currentPlayerTurn == 2 then
        if INPUT:Pressed("LBlue2P") or INPUT:Pressed("RBlue2P") then
            selectedGenreIndex = selectedGenreIndex + 1
            if selectedGenreIndex > #availableGenres then
                selectedGenreIndex = 1
            end
            sounds.Skip:Play()
        end

        if INPUT:Pressed("LRed2P") or INPUT:Pressed("RRed2P") then
            local selectedGenre = availableGenres[selectedGenreIndex]

            if selectedGenre ~= nil then
                local success = selectRandomSongFromGenre(selectedGenre)

                if success then
                    state = "song_playing"
                    answerTimerCounter = nil
                    answeringPlayer = 0
                    previewStarted = false
                    sounds.Decide:Play()
                else
                    sounds.Cancel:Play()
                end
            end
        end
    elseif currentPlayerTurn == 3 then
        if INPUT:Pressed("LBlue3P") or INPUT:Pressed("RBlue3P") then
            selectedGenreIndex = selectedGenreIndex + 1
            if selectedGenreIndex > #availableGenres then
                selectedGenreIndex = 1
            end
            sounds.Skip:Play()
        end

        if INPUT:Pressed("LRed3P") or INPUT:Pressed("RRed3P") then
            local selectedGenre = availableGenres[selectedGenreIndex]

            if selectedGenre ~= nil then
                local success = selectRandomSongFromGenre(selectedGenre)

                if success then
                    state = "song_playing"
                    answerTimerCounter = nil
                    answeringPlayer = 0
                    previewStarted = false
                    sounds.Decide:Play()
                else
                    sounds.Cancel:Play()
                end
            end
        end
    elseif currentPlayerTurn == 4 then
        if INPUT:Pressed("LBlue4P") or INPUT:Pressed("RBlue4P") then
            selectedGenreIndex = selectedGenreIndex + 1
            if selectedGenreIndex > #availableGenres then
                selectedGenreIndex = 1
            end
            sounds.Skip:Play()
        end

        if INPUT:Pressed("LRed4P") or INPUT:Pressed("RRed4P") then
            local selectedGenre = availableGenres[selectedGenreIndex]

            if selectedGenre ~= nil then
                local success = selectRandomSongFromGenre(selectedGenre)

                if success then
                    state = "song_playing"
                    answerTimerCounter = nil
                    answeringPlayer = 0
                    previewStarted = false
                    sounds.Decide:Play()
                else
                    sounds.Cancel:Play()
                end
            end
        end
    elseif currentPlayerTurn == 5 then
        if INPUT:Pressed("LBlue5P") or INPUT:Pressed("RBlue5P") then
            selectedGenreIndex = selectedGenreIndex + 1
            if selectedGenreIndex > #availableGenres then
                selectedGenreIndex = 1
            end
            sounds.Skip:Play()
        end

        if INPUT:Pressed("LRed5P") or INPUT:Pressed("RRed5P") then
            local selectedGenre = availableGenres[selectedGenreIndex]

            if selectedGenre ~= nil then
                local success = selectRandomSongFromGenre(selectedGenre)

                if success then
                    state = "song_playing"
                    answerTimerCounter = nil
                    answeringPlayer = 0
                    previewStarted = false
                    sounds.Decide:Play()
                else
                    sounds.Cancel:Play()
                end
            end
        end
    end

    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        state = "player_select"
        sounds.Cancel:Play()
    end
end

local function handleSongPlaying()
    if answerTimerCounter == nil then
        answerTimerCounter = COUNTER:CreateCounterDuration(0, 1, 1) -- 1 second delay before starting
        answerTimerCounter:Start()
        previewStarted = false
    end

    answerTimerCounter:Tick()

    -- Start preview after 1 second (only once)
    if answerTimerCounter.Value >= 1 and previewStarted == false then
        startSongPreview()
        previewStarted = true
    end

    -- Don't allow answering until preview has started
    if previewStarted == false then
        return
    end

    -- Check for any player input to answer
    if numPlayers == 1 then
        -- Single player mode - check 1P controls
        if INPUT:Pressed("LRed") or INPUT:Pressed("RRed") then
            answeringPlayer = 1
            state = "answering"
            stopSongPreview()
            previewStarted = false
            sounds.Decide:Play()
        end
    else
        -- Multiplayer mode - check all players
        if INPUT:Pressed("LRed") or INPUT:Pressed("RRed") then
            answeringPlayer = 1
            state = "answering"
            stopSongPreview()
            previewStarted = false
            sounds.Decide:Play()
        elseif INPUT:Pressed("LRed2P") or INPUT:Pressed("RRed2P") then
            answeringPlayer = 2
            state = "answering"
            stopSongPreview()
            previewStarted = false
            sounds.Decide:Play()
        elseif INPUT:Pressed("LRed3P") or INPUT:Pressed("RRed3P") then
            answeringPlayer = 3
            state = "answering"
            stopSongPreview()
            previewStarted = false
            sounds.Decide:Play()
        elseif INPUT:Pressed("LRed4P") or INPUT:Pressed("RRed4P") then
            answeringPlayer = 4
            state = "answering"
            stopSongPreview()
            previewStarted = false
            sounds.Decide:Play()
        elseif INPUT:Pressed("LRed5P") or INPUT:Pressed("RRed5P") then
            answeringPlayer = 5
            state = "answering"
            stopSongPreview()
            previewStarted = false
            sounds.Decide:Play()
        end
    end

    -- Optional: Timeout if no one answers
    -- if answerTimer > 1800 then -- 30 seconds
    --     state = "answer_reveal"
    --     stopSongPreview()
    --     previewStarted = false
    -- end
end

local function handleAnswering()
    -- Player navigates quiz song list

    if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
        if quizSongList ~= nil then
            quizSongList:Move(1)
            refreshQuizSongListCache()
            sounds.Skip:Play()
        end
    end

    if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
        if quizSongList ~= nil then
            quizSongList:Move(-1)
            refreshQuizSongListCache()
            sounds.Skip:Play()
        end
    end

    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
        local selectedSong = quizSongList:GetSelectedSongNode()

        if selectedSong ~= nil and selectedSong.UniqueId == correctSongNode.UniqueId then
            -- Correct answer!
            addScore(answeringPlayer, 10)
            sounds.SongDecide:Play()
            state = "answer_reveal"
        else
            -- Wrong answer
            addScore(answeringPlayer, -2) -- Penalty
            sounds.Cancel:Play()
            -- TODO: Allow other players to try?
            state = "answer_reveal"
        end
    end
end

local function handleAnswerReveal()
    -- Show correct answer and scores
    if celebrationCounter == nil then
        celebrationCounter = COUNTER:CreateCounterDuration(0, 1, 3) -- 3 seconds
        celebrationCounter:Start()
    end

    celebrationCounter:Tick()

    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") or celebrationCounter.Value >= 1 then
        celebrationCounter = nil

        -- Check if this was the last round
        if currentRound >= numRounds then
            state = "results"
        else
            -- Move to next round
            currentRound = currentRound + 1
            selectNextPlayer()
            state = "round_start"
        end
    end
end

local function handleResults()
    -- Show final scores and winner

    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
        -- Reset player count to original
        CONFIG.PlayerCount = originalPlayerCount
        state = "player_select"
        sounds.Decide:Play()
				return false
    end

    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        -- Reset player count to original
        CONFIG.PlayerCount = originalPlayerCount
        sounds.Cancel:Play()
        return true
    end
end

-- Solo Mode Handlers

local function handleSoloIntro()
    if cutsceneCounter == nil then
        cutsceneCounter = COUNTER:CreateCounterDuration(0, 1, 3) -- 3 seconds
        cutsceneCounter:Start()
    end

    cutsceneCounter:Tick()

    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") or cutsceneCounter.Value >= 1 then
        state = "solo_round_start"
        cutsceneCounter = nil
    end
end

local function handleSoloRoundStart()
    -- Load random song and start preview
    local success = selectRandomSongFromGenre(nil)

    if success then
        state = "solo_playing"
        answerTimerCounter = nil
        previewStarted = false

        -- Wait a few seconds then start preview
    else
        -- Failed, jump to results
				state = "solo_results"
				answerTimerCounter = nil
        previewStarted = false
        sounds.Cancel:Play()
    end
end

local function handleSoloPlaying()
    if answerTimerCounter == nil then
        answerTimerCounter = COUNTER:CreateCounterDuration(0, 1, 1) -- 1 second delay
        answerTimerCounter:Start()
        previewStarted = false
    end

    answerTimerCounter:Tick()

    -- Start preview after 1 second (only once)
    if answerTimerCounter.Value >= 1 and previewStarted == false then
        startSongPreview()
        previewStarted = true
    end

		if previewStarted and (INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return")) then
				stopSongPreview()
        previewStarted = false
				state = "solo_answering"
        sounds.Decide:Play()
    end

    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        stopSongPreview()
        previewStarted = false
        state = "solo_results"
        sounds.Cancel:Play()
    end
end

local function handleSoloAnswering()
    -- Navigate song list with preview still playing

    if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
        if quizSongList ~= nil then
            quizSongList:Move(1)
            refreshQuizSongListCache()
            sounds.Skip:Play()
        end
    end

    if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
        if quizSongList ~= nil then
            quizSongList:Move(-1)
            refreshQuizSongListCache()
            sounds.Skip:Play()
        end
    end

    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
        local selectedSong = quizSongList:GetSelectedSongNode()

        if selectedSong ~= nil and selectedSong.UniqueId == correctSongNode.UniqueId then
            -- Correct!
            soloScore = soloScore + 1
            stopSongPreview()
            sounds.SongDecide:Play()

            -- Brief celebration then next round
            cutsceneTimer = 0
            state = "solo_correct"
        else
            -- Wrong! Game over
            stopSongPreview()
            sounds.Cancel:Play()
            state = "solo_results"
        end
    end
end

local function handleSoloCorrect()
    -- Brief fanfare
    if celebrationCounter == nil then
        celebrationCounter = COUNTER:CreateCounterDuration(0, 1, 2) -- 2 seconds
        celebrationCounter:Start()
    end

    celebrationCounter:Tick()

    if celebrationCounter.Value >= 1 or INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
        celebrationCounter = nil
        state = "solo_round_start"
    end
end

local function handleSoloResults()
    -- Show final score and high scores

    local isNewHighScore, rank = checkIfNewHighScore(soloScore)

    if isNewHighScore then
        local playerName = save.Name
        saveHighScore(soloScore, playerName)

        -- Check if beat Nokon's score
        if checkIfBeatNokon(soloScore) then
            grantKeyOfPride()
            return -- Pride modal will take over
        end
    end

    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
        -- Reset player count to original
        CONFIG.PlayerCount = originalPlayerCount
        state = "player_select"
        sounds.Decide:Play()
				return false
    end

    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        -- Reset player count to original
        CONFIG.PlayerCount = originalPlayerCount
        sounds.Cancel:Play()
        return true
    end
end

local function handlePrideModal()
    prideModalTimer = prideModalTimer + 1

    -- Auto-close after 5 seconds or on button press
    if prideModalTimer > 300 or INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
        showPrideModal = false
        prideModalTimer = 0

        -- Return to results screen
        CONFIG.PlayerCount = originalPlayerCount
        state = "player_select"
        sounds.Decide:Play()
    end
end

---------------------------------------
-- Main Functions
---------------------------------------

function draw()
    -- TODO: Add background texture
    -- textures["Background"]:Draw(0, 0)

    if state == "waiting_enum" then
        -- Show loading message
        if text ~= nil then
            local loadingText = text:GetText("Loading songs...")
            loadingText:DrawAtAnchor(960, 540, "center")
        end

    elseif state == "cutscene1" then
        -- Draw intro cutscene
        if text ~= nil then
            local cutsceneText = text:GetText("Intro Nokon Cutscene")
            cutsceneText:DrawAtAnchor(960, 540, "center")
            local skipText = text:GetText("Press any button to skip")
            skipText:DrawAtAnchor(960, 600, "center")
        end

    elseif state == "cutscene2" then
        -- Draw title screen
        if text ~= nil then
            local titleText = text:GetText("MUSIC QUIZ MODE")
            titleText:DrawAtAnchor(960, 400, "center")
            local startText = text:GetText("Press DECIDE to start")
            startText:DrawAtAnchor(960, 600, "center")
        end

    elseif state == "player_select" then
        if text ~= nil then
            local headerText = text:GetText("Select Number of Players")
            headerText:DrawAtAnchor(960, 300, "center")

            for i = 1, 5 do
                local playerText = text:GetText(i .. " Player" .. (i > 1 and "s" or ""), false, 99999,
                    i == selectedPlayerOption and COLOR:CreateColorFromARGB(255, 242, 207, 1) or nil)
                playerText:DrawAtAnchor(960, 400 + i * 50, "center")
            end
        end

    elseif state == "scope_select" then
        if text ~= nil then
            local headerText = text:GetText("Select Song Scope")
            headerText:DrawAtAnchor(960, 300, "center")

            local scopes = {"OpTk", "Customs", "All"}
            for i, scope in ipairs(scopes) do
                local scopeText = text:GetText(scope, false, 99999,
                    i == selectedScopeOption and COLOR:CreateColorFromARGB(255, 242, 207, 1) or nil)
                scopeText:DrawAtAnchor(960, 400 + i * 50, "center")
            end
        end

    elseif state == "intro" or state == "solo_intro" then
        if text ~= nil then
            local introText = text:GetText("Get ready!")
            introText:DrawAtAnchor(960, 540, "center")
        end

    elseif state == "round_start" then
        if text ~= nil then
            local roundText = text:GetText("Round " .. currentRound .. " / " .. numRounds)
            roundText:DrawAtAnchor(960, 400, "center")
            local playerText = text:GetText("Player " .. currentPlayerTurn .. "'s turn")
            playerText:DrawAtAnchor(960, 500, "center")

            -- DEBUG
            local debugText = text:GetText("Press DECIDE to continue")
            debugText:DrawAtAnchor(960, 600, "center")
        end

    elseif state == "genre_select" then
        if text ~= nil then
            local promptText = text:GetText("Player " .. currentPlayerTurn .. ": Select a genre")
            promptText:DrawAtAnchor(960, 300, "center")

            -- Draw genre list
            for i, genreNode in ipairs(availableGenres) do
                local genreText = text:GetText(genreNode.Title, false, 99999,
                    i == selectedGenreIndex and COLOR:CreateColorFromARGB(255, 242, 207, 1) or nil)
                genreText:DrawAtAnchor(960, 350 + i * 50, "center")
            end
        end

    elseif state == "song_playing" then
        if text ~= nil then
            local promptText = text:GetText("Listen to the song...")
            promptText:DrawAtAnchor(960, 400, "center")
            local hintText = text:GetText("Press DON to answer!")
            hintText:DrawAtAnchor(960, 500, "center")
        end

    elseif state == "answering" then
        if text ~= nil then
            local promptText = text:GetText("Player " .. answeringPlayer .. ": Choose the song")
            promptText:DrawAtAnchor(960, 200, "center")

            -- Draw quiz song list using cached data
            for i = -5, 5 do
                if currentPageCache[i] ~= nil then
                    local entry = currentPageCache[i]
                    entry.text:DrawAtAnchor(960, 600 + i * 50, "center")
                end
            end
        end

    elseif state == "answer_reveal" then
        if text ~= nil then
            local answerText = text:GetText("Correct song: " .. (correctSongNode and correctSongNode.Title or "???"))
            answerText:DrawAtAnchor(960, 400, "center")

            -- Show scores
            for i = 1, numPlayers do
                local scoreText = text:GetText("P" .. i .. ": " .. playerScores[i])
                scoreText:DrawAtAnchor(200 + i * 200, 600, "center")
            end
        end

    elseif state == "results" then
        if text ~= nil then
            local headerText = text:GetText("Final Results!")
            headerText:DrawAtAnchor(960, 200, "center")

            -- DEBUG: Show why we got here
            local debugText = text:GetText("Rounds completed: " .. (currentRound - 1) .. " / " .. numRounds)
            debugText:DrawAtAnchor(960, 250, "center")

            -- Sort and display scores
            local sortedPlayers = {}
            for i = 1, numPlayers do
                table.insert(sortedPlayers, {index = i, score = playerScores[i]})
            end
            table.sort(sortedPlayers, function(a, b) return a.score > b.score end)

            for rank, player in ipairs(sortedPlayers) do
                local rankText = text:GetText(rank .. ". Player " .. player.index .. ": " .. player.score)
                rankText:DrawAtAnchor(960, 300 + rank * 50, "center")
            end
        end

    elseif state == "solo_playing" then
        if text ~= nil then
            local scoreText = text:GetText("Score: " .. soloScore)
            scoreText:DrawAtAnchor(960, 200, "center")

            local genreText = text:GetText("Genre: " .. (currentSongNode and currentSongNode.Genre or "???"))
            genreText:DrawAtAnchor(960, 400, "center")

            local promptText = text:GetText("Press DECIDE when ready to answer")
            promptText:DrawAtAnchor(960, 500, "center")
        end

    elseif state == "solo_answering" then
        if text ~= nil then
            local scoreText = text:GetText("Score: " .. soloScore)
            scoreText:DrawAtAnchor(960, 100, "center")

            local promptText = text:GetText("Choose the correct song")
            promptText:DrawAtAnchor(960, 200, "center")

            -- Draw quiz song list using cached data
            for i = -5, 5 do
                if currentPageCache[i] ~= nil then
                    local entry = currentPageCache[i]
                    entry.text:DrawAtAnchor(960, 600 + i * 50, "center")
                end
            end
        end

    elseif state == "solo_correct" then
        if text ~= nil then
            local correctText = text:GetText("CORRECT!")
            correctText:DrawAtAnchor(960, 540, "center")
        end

    elseif state == "solo_results" then
        if text ~= nil then
            local headerText = text:GetText("Game Over!")
            headerText:DrawAtAnchor(960, 200, "center")

            local scoreText = text:GetText("Your Score: " .. soloScore)
            scoreText:DrawAtAnchor(960, 300, "center")

            local highScoreHeader = text:GetText("High Scores:")
            highScoreHeader:DrawAtAnchor(960, 400, "center")

            for i, entry in ipairs(bestScores) do
                local isPlayerScore = (entry.score == soloScore and entry.name == save.Name)
                local highScoreText = text:GetText(i .. ". " .. entry.name .. " - " .. entry.score, false, 99999,
                    isPlayerScore and COLOR:CreateColorFromARGB(255, 242, 207, 1) or nil)
                highScoreText:DrawAtAnchor(960, 450 + i * 40, "center")
            end
        end

    elseif state == "pride_modal" then
        -- Draw modal overlay
        if text ~= nil then
            -- TODO: Add modal background texture
            -- textures["ModalBg"]:Draw(460, 240)

            local titleText = text:GetText("Congratulations!")
            titleText:DrawAtAnchor(960, 400, "center")

            local messageText = text:GetText("You have beaten Nokon's record!")
            messageText:DrawAtAnchor(960, 480, "center")

            local keyText = text:GetText("Key of Pride obtained!")
            keyText:DrawAtAnchor(960, 560, "center")

            -- TODO: Add Key of Pride icon
            -- textures["PrideKey"]:Draw(860, 620)

            local continueText = text:GetText("Press DECIDE to continue")
            continueText:DrawAtAnchor(960, 720, "center")
        end
    end

    -- Draw nameplates for all players
    if active then
        for i = 0, CONFIG.PlayerCount - 1 do
            NAMEPLATE:DrawPlayerNameplate(20 + i * 400, 980, 255, i)
        end
    end
end

function update()
		local quitted = false

    if state == "waiting_enum" then
        handleWaitingEnum()
    elseif state == "cutscene1" then
        handleCutscene1()
    elseif state == "cutscene2" then
        quitted = handleCutscene2()
    elseif state == "player_select" then
        handlePlayerSelect()
    elseif state == "scope_select" then
        handleScopeSelect()
    elseif state == "intro" then
        handleIntro()
    elseif state == "round_start" then
        handleRoundStart()
    elseif state == "genre_select" then
        handleGenreSelect()
    elseif state == "song_playing" then
        handleSongPlaying()
    elseif state == "answering" then
        handleAnswering()
    elseif state == "answer_reveal" then
        handleAnswerReveal()
    elseif state == "results" then
        quitted = handleResults()
    elseif state == "solo_intro" then
        handleSoloIntro()
    elseif state == "solo_round_start" then
        handleSoloRoundStart()
    elseif state == "solo_playing" then
        handleSoloPlaying()
    elseif state == "solo_answering" then
        handleSoloAnswering()
    elseif state == "solo_correct" then
        handleSoloCorrect()
    elseif state == "solo_results" then
        quitted = handleSoloResults()
    elseif state == "pride_modal" then
        handlePrideModal()
    end

		if quitted == true then
			return Exit("title", nil)
		end
end

function activate()
    save = GetSaveFile(0)
    active = true

    -- Store original player count to restore later
    originalPlayerCount = CONFIG.PlayerCount

    -- Load high scores
    loadHighScores()

    -- Play BGM if available
    if sounds.BGM ~= nil then
        sounds.BGM:SetLoop(true)
        sounds.BGM:Play()
    end

    -- Start at appropriate state
    if songsEnumerated then
        state = "cutscene1"
    else
        state = "waiting_enum"
    end
end

function deactivate()
    -- Reset player count to original
    CONFIG.PlayerCount = originalPlayerCount

    -- Cleanup
    for _, v in pairs(textures) do
        if v ~= nil then
            v:Dispose()
        end
    end

    if sounds.BGM ~= nil then
        sounds.BGM:Stop()
    end

    stopSongPreview()

    active = false
end

function onStart()
    text = TEXT:Create(16)

    -- Load sounds
    sounds.Skip = SOUND:CreateSFX("Sounds/Skip.ogg")
    sounds.Cancel = SOUND:CreateSFX("Sounds/Cancel.ogg")
    sounds.Decide = SOUND:CreateSFX("Sounds/Decide.ogg")
    sounds.SongDecide = SOUND:CreateSFX("Sounds/SongDecide.ogg")
    sounds.PrideKey = SOUND:CreateSFX("Sounds/PrideKey.ogg")
    -- sounds.BGM = SOUND:CreateBGM("Sounds/QuizBGM.ogg")

    -- Load textures
    -- textures["Background"] = TEXTURE:CreateTexture("Textures/QuizBackground.png")
    -- textures["ModalBg"] = TEXTURE:CreateTexture("Textures/ModalBackground.png")
    -- textures["PrideKey"] = TEXTURE:CreateTexture("Textures/PrideKey.png")
end

function afterSongEnum()
    songsEnumerated = true
    loadMainSongList()

    -- If already active, transition from waiting screen
    if active and state == "waiting_enum" then
        state = "cutscene1"
    end
end

function onDestroy()
    if text ~= nil then
        text:Dispose()
    end

    for _, sound in pairs(sounds) do
        if sound ~= nil then
            sound:Dispose()
        end
    end

    for _, texture in pairs(textures) do
        if texture ~= nil then
            texture:Dispose()
        end
    end
end
