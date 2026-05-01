local DBScores = require("DBControllers/dbScores")
local Opening  = require("opening")
local Setup    = require("setup")

local text  = nil
local texts = {}   -- text renderers for sub-screens (title, label)
local save  = nil

local playerNames = {}

local sounds   = {}
local textures = {}

local songList = nil
local quizSongList = nil -- Limited list for answering
local currentSongNode = nil
local correctSongNode = nil

local active = false
local songsEnumerated = false

local highScoreRegistered = false

-- Game state machine
local state = "waiting_enum" -- waiting_enum, opening, setup, intro,
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

-- After the UI state variables section
local maxScoreForRound = 10
local currentScore = 10
local scoreDropCounter = nil
local playersWhoAnsweredWrong = {}

-- High scores
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
local selectedGenreIndex = 1
local answerTimer = 0
local answeringPlayer = 0
local availableGenres = {}
local currentPageCache = {}
local previewStarted = false

-- Cutscene (kept for intro / round screens only)
local cutsceneCounter = nil

-- Timers using LuaCounters
local roundStartCounter = nil
local answerTimerCounter = nil
local celebrationCounter = nil
local prideModalCounter = nil

---------------------------------------
-- Utility Functions
---------------------------------------

local function stopBGM()
	if sounds.BGM ~= nil then
			sounds.BGM:Stop()
	end
end

local function calculateMaxScore(genreSongList)
    if genreSongList == nil then return 10 end

    local songCount = genreSongList.SongCount -- Count total songs in the genre
    if songCount == 0 then return 10 end

    local logScore = math.log(songCount) -- Natural logarithm (ln)
    local roundedScore = math.ceil(logScore) * 10 -- Round up to nearest 10 * 10

    return math.max(10, roundedScore) -- At least 10
end

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

		-- Reset score drop
    scoreDropCounter = nil
    maxScoreForRound = 10
    currentScore = 10
    playersWhoAnsweredWrong = {}
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

local function limitGenresRandomly(genres)
    local maxGenres = 5
    local numGenres = #genres

    -- Only proceed if the table has more than the maximum allowed elements
    if numGenres > maxGenres then
        -- 1. Perform a partial Fisher-Yates shuffle.
        -- We only need to shuffle the first 5 positions to get 5 random, unique elements.
        for i = 1, maxGenres do
            -- Choose a random index 'j' from the unshuffled remainder of the table (from 'i' to 'numGenres')
            -- math.random(a, b) returns a uniform integer between a and b, inclusive.
            local j = math.random(i, numGenres)

            -- Swap the element at the current position 'i' with the element at the random position 'j'
            local temp = genres[i]
            genres[i] = genres[j]
            genres[j] = temp
        end

        -- 2. Create a new table containing only the first 5 elements (the random sample)
        local newGenres = {}
        for i = 1, maxGenres do
            table.insert(newGenres, genres[i])
        end

        -- Return the new, limited table
        return newGenres
    end

    -- If the table is 5 or less, return it as is
    return genres
end

---------------------------------------
-- Utility Functions for C# Collections
---------------------------------------

-- Clone a C# Dictionary into a Lua table safely
local function cloneTable(t)
	local copy = {}

	-- Get enumerator from the dictionary
	local enumerator = t:GetEnumerator()
	while enumerator:MoveNext() do
		local kvp = enumerator.Current
		local key = kvp.Key
		local value = kvp.Value

		-- Recursively clone if it's another Dictionary
		if value ~= nil and type(value) == "userdata" and value.GetEnumerator then
			copy[key] = cloneTable(value)
		else
			copy[key] = value
		end
	end

	return copy
end

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
        lsls.RootGenreFolderNode = currentSongNode.Parent

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
    lsls.RootGenreFolderNode = genreFolder

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
    -- Initialize the target table
    bestScores = {}

    -- Fetch scores and convert the C# enumerable to a Lua array
    local rawScores = cloneTable(DBScores:GetScores())

    -- Use ipairs to iterate over the array of score objects
    for _, dbScore in ipairs(rawScores) do
        -- Directly insert the formatted score into the bestScores table
        table.insert(bestScores, {
            name = dbScore.player,
            score = dbScore.score
        })
    end
end

local function saveHighScore(score, playerName)
		DBScores:RegisterScore(playerName, score)
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
    state = "pride_modal"

    sounds.KeyGot:Play()
end

---------------------------------------
-- State Handlers
---------------------------------------

local function handleWaitingEnum()
    -- Just wait, afterSongEnum will handle transition
end

local function handleOpening()
    local result = Opening.update()
    if result == "start" then
        state = "setup"
        Setup.reset()
    elseif result == "back" then
        return true  -- exit to _title
    end
    return false
end

local function handleSetup()
    local result = Setup.update()
    if result == "back" then
        Opening.resetToMenu()
        state = "opening"
    elseif result ~= nil then
        -- result = {mode, players, songs}
        numPlayers = result.players
        songScope  = result.songs
        CONFIG.PlayerCount = numPlayers
        loadMainSongList()
        resetGame()
        stopBGM()
        SHARED:GetSharedSound("Decide"):Play()
        if result.mode == "Endurance" then
            numRounds = 999
            state = "solo_intro"
        else
            numRounds = 5
            state = "intro"
        end
    end
    return false
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
        availableGenres = limitGenresRandomly(getAvailableGenres())

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
        SHARED:GetSharedSound("Cancel"):Play()
        return
    end

    -- Navigation using LBlue/RBlue for player 1
    if currentPlayerTurn == 1 then
        if INPUT:Pressed("LBlue") or INPUT:Pressed("RBlue") or INPUT:KeyboardPressed("RightArrow") then
            selectedGenreIndex = selectedGenreIndex + 1
            if selectedGenreIndex > #availableGenres then
                selectedGenreIndex = 1
            end
            SHARED:GetSharedSound("Skip"):Play()
        end

        if INPUT:KeyboardPressed("LeftArrow") then
            selectedGenreIndex = selectedGenreIndex - 1
            if selectedGenreIndex < 1 then
                selectedGenreIndex = #availableGenres
            end
            SHARED:GetSharedSound("Skip"):Play()
        end

        if INPUT:Pressed("LRed") or INPUT:Pressed("RRed") or INPUT:KeyboardPressed("Return") then
            local selectedGenre = availableGenres[selectedGenreIndex]

            if selectedGenre ~= nil then
                local success = selectRandomSongFromGenre(selectedGenre)

                if success then
										-- Calculate max score based on genre
				            maxScoreForRound = calculateMaxScore(selectedGenre)
				            currentScore = maxScoreForRound
				            playersWhoAnsweredWrong = {}
				            scoreDropCounter = nil

                    state = "song_playing"
                    answerTimerCounter = nil
                    answeringPlayer = 0
                    previewStarted = false
                    SHARED:GetSharedSound("Decide"):Play()
                else
                    SHARED:GetSharedSound("Cancel"):Play()
                end
            end
        end
    elseif currentPlayerTurn == 2 then
        if INPUT:Pressed("LBlue2P") or INPUT:Pressed("RBlue2P") then
            selectedGenreIndex = selectedGenreIndex + 1
            if selectedGenreIndex > #availableGenres then
                selectedGenreIndex = 1
            end
            SHARED:GetSharedSound("Skip"):Play()
        end

        if INPUT:Pressed("LRed2P") or INPUT:Pressed("RRed2P") then
            local selectedGenre = availableGenres[selectedGenreIndex]

            if selectedGenre ~= nil then
                local success = selectRandomSongFromGenre(selectedGenre)

                if success then
										-- Calculate max score based on genre
										maxScoreForRound = calculateMaxScore(selectedGenre)
										currentScore = maxScoreForRound
										playersWhoAnsweredWrong = {}
										scoreDropCounter = nil

                    state = "song_playing"
                    answerTimerCounter = nil
                    answeringPlayer = 0
                    previewStarted = false
                    SHARED:GetSharedSound("Decide"):Play()
                else
                    SHARED:GetSharedSound("Cancel"):Play()
                end
            end
        end
    elseif currentPlayerTurn == 3 then
        if INPUT:Pressed("LBlue3P") or INPUT:Pressed("RBlue3P") then
            selectedGenreIndex = selectedGenreIndex + 1
            if selectedGenreIndex > #availableGenres then
                selectedGenreIndex = 1
            end
            SHARED:GetSharedSound("Skip"):Play()
        end

        if INPUT:Pressed("LRed3P") or INPUT:Pressed("RRed3P") then
            local selectedGenre = availableGenres[selectedGenreIndex]

            if selectedGenre ~= nil then
                local success = selectRandomSongFromGenre(selectedGenre)

                if success then
										-- Calculate max score based on genre
										maxScoreForRound = calculateMaxScore(selectedGenre)
										currentScore = maxScoreForRound
										playersWhoAnsweredWrong = {}
										scoreDropCounter = nil

                    state = "song_playing"
                    answerTimerCounter = nil
                    answeringPlayer = 0
                    previewStarted = false
                    SHARED:GetSharedSound("Decide"):Play()
                else
                    SHARED:GetSharedSound("Cancel"):Play()
                end
            end
        end
    elseif currentPlayerTurn == 4 then
        if INPUT:Pressed("LBlue4P") or INPUT:Pressed("RBlue4P") then
            selectedGenreIndex = selectedGenreIndex + 1
            if selectedGenreIndex > #availableGenres then
                selectedGenreIndex = 1
            end
            SHARED:GetSharedSound("Skip"):Play()
        end

        if INPUT:Pressed("LRed4P") or INPUT:Pressed("RRed4P") then
            local selectedGenre = availableGenres[selectedGenreIndex]

            if selectedGenre ~= nil then
                local success = selectRandomSongFromGenre(selectedGenre)

                if success then
										-- Calculate max score based on genre
										maxScoreForRound = calculateMaxScore(selectedGenre)
										currentScore = maxScoreForRound
										playersWhoAnsweredWrong = {}
										scoreDropCounter = nil

                    state = "song_playing"
                    answerTimerCounter = nil
                    answeringPlayer = 0
                    previewStarted = false
                    SHARED:GetSharedSound("Decide"):Play()
                else
                    SHARED:GetSharedSound("Cancel"):Play()
                end
            end
        end
    elseif currentPlayerTurn == 5 then
        if INPUT:Pressed("LBlue5P") or INPUT:Pressed("RBlue5P") then
            selectedGenreIndex = selectedGenreIndex + 1
            if selectedGenreIndex > #availableGenres then
                selectedGenreIndex = 1
            end
            SHARED:GetSharedSound("Skip"):Play()
        end

        if INPUT:Pressed("LRed5P") or INPUT:Pressed("RRed5P") then
            local selectedGenre = availableGenres[selectedGenreIndex]

            if selectedGenre ~= nil then
                local success = selectRandomSongFromGenre(selectedGenre)

                if success then
										-- Calculate max score based on genre
										maxScoreForRound = calculateMaxScore(selectedGenre)
										currentScore = maxScoreForRound
										playersWhoAnsweredWrong = {}
										scoreDropCounter = nil

                    state = "song_playing"
                    answerTimerCounter = nil
                    answeringPlayer = 0
                    previewStarted = false
                    SHARED:GetSharedSound("Decide"):Play()
                else
                    SHARED:GetSharedSound("Cancel"):Play()
                end
            end
        end
    end
end

local function handleSongPlaying()
    if answerTimerCounter == nil then
        answerTimerCounter = COUNTER:CreateCounterDuration(0, 1, 1) -- 1 second delay before starting
        answerTimerCounter:Start()
				sounds.Question:Play()
        previewStarted = false
				scoreDropCounter = nil
    end

    answerTimerCounter:Tick()

    -- Start preview after 1 second (only once)
    if answerTimerCounter.Value >= 1 and previewStarted == false then
        startSongPreview()
        previewStarted = true
				-- Initialize score drop counter
        scoreDropCounter = COUNTER:CreateCounterDuration(0, 1, 2) -- 2 seconds
        scoreDropCounter:Start()
    end

    -- Don't allow answering until preview has started
    if previewStarted == false then
        return
    end

		-- Drop score every 2 seconds (10% of max)
    if scoreDropCounter ~= nil then
        scoreDropCounter:Tick()

        if scoreDropCounter.Value >= 1 then
            local scoreDrop = math.floor(maxScoreForRound * 0.1)
            currentScore = math.max(0, currentScore - scoreDrop)

            -- Reset counter for next drop
            scoreDropCounter = COUNTER:CreateCounterDuration(0, 1, 2)
            scoreDropCounter:Start()

            -- If score reaches 0, end round with no points
            if currentScore <= 0 then
                stopSongPreview()
                previewStarted = false
                scoreDropCounter = nil
                state = "answer_reveal"
                sounds.Wrong:Play()
                return
            end
        end
    end

		-- Check for any player input to answer
    -- Only allow players who haven't answered wrong yet
		if INPUT:Pressed("LRed") or INPUT:Pressed("RRed") then
				if playersWhoAnsweredWrong[1] ~= true then
						answeringPlayer = 1
						state = "answering"
						stopSongPreview()
						previewStarted = false
						scoreDropCounter = nil
						sounds.Answering:Play()
				end
		elseif INPUT:Pressed("LRed2P") or INPUT:Pressed("RRed2P") then
				if playersWhoAnsweredWrong[2] ~= true then
						answeringPlayer = 2
						state = "answering"
						stopSongPreview()
						previewStarted = false
						scoreDropCounter = nil
						sounds.Answering:Play()
				end
		elseif INPUT:Pressed("LRed3P") or INPUT:Pressed("RRed3P") then
				if playersWhoAnsweredWrong[3] ~= true then
						answeringPlayer = 3
						state = "answering"
						stopSongPreview()
						previewStarted = false
						scoreDropCounter = nil
						sounds.Answering:Play()
				end
		elseif INPUT:Pressed("LRed4P") or INPUT:Pressed("RRed4P") then
				if playersWhoAnsweredWrong[4] ~= true then
						answeringPlayer = 4
						state = "answering"
						stopSongPreview()
						previewStarted = false
						scoreDropCounter = nil
						sounds.Answering:Play()
				end
		elseif INPUT:Pressed("LRed5P") or INPUT:Pressed("RRed5P") then
				if playersWhoAnsweredWrong[5] ~= true then
						answeringPlayer = 5
						state = "answering"
						stopSongPreview()
						previewStarted = false
						scoreDropCounter = nil
						sounds.Answering:Play()
				end
		end
end

local function handleAnswering()
    -- Player navigates quiz song list using their own controls

    -- Player 1
    if answeringPlayer == 1 then
        if INPUT:Pressed("LBlue") or INPUT:Pressed("RBlue") or INPUT:KeyboardPressed("RightArrow") then
            if quizSongList ~= nil then
                quizSongList:Move(1)
                refreshQuizSongListCache()
                SHARED:GetSharedSound("Skip"):Play()
            end
        end

        if INPUT:KeyboardPressed("LeftArrow") then
            if quizSongList ~= nil then
                quizSongList:Move(-1)
                refreshQuizSongListCache()
                SHARED:GetSharedSound("Skip"):Play()
            end
        end

        if INPUT:Pressed("LRed") or INPUT:Pressed("RRed") or INPUT:KeyboardPressed("Return") then
            local selectedSong = quizSongList:GetSelectedSongNode()

            if selectedSong ~= nil and selectedSong.UniqueId == correctSongNode.UniqueId then
                -- Correct answer!
                addScore(answeringPlayer, currentScore)
                sounds.Right:Play()
                state = "answer_reveal"
            else
                -- Wrong answer
                addScore(answeringPlayer, -10)
                playersWhoAnsweredWrong[answeringPlayer] = true
                sounds.Wrong:Play()

                -- Resume song playing if other players can still answer
                local allPlayersAnswered = true
                for i = 1, numPlayers do
                    if playersWhoAnsweredWrong[i] ~= true then
                        allPlayersAnswered = false
                        break
                    end
                end

                if allPlayersAnswered or currentScore <= 0 then
                    state = "answer_reveal"
                else
                    state = "song_playing"
                    previewStarted = true -- Resume from where we left off
                    startSongPreview()
                    -- Reinitialize score drop counter
                    scoreDropCounter = COUNTER:CreateCounterDuration(0, 1, 2)
                    scoreDropCounter:Start()
                end
            end
        end

    -- Player 2
    elseif answeringPlayer == 2 then
        if INPUT:Pressed("LBlue2P") or INPUT:Pressed("RBlue2P") then
            if quizSongList ~= nil then
                quizSongList:Move(1)
                refreshQuizSongListCache()
                SHARED:GetSharedSound("Skip"):Play()
            end
        end

        if INPUT:Pressed("LRed2P") or INPUT:Pressed("RRed2P") then
            local selectedSong = quizSongList:GetSelectedSongNode()

            if selectedSong ~= nil and selectedSong.UniqueId == correctSongNode.UniqueId then
                addScore(answeringPlayer, currentScore)
                sounds.Right:Play()
                state = "answer_reveal"
            else
                addScore(answeringPlayer, -10)
                playersWhoAnsweredWrong[answeringPlayer] = true
                sounds.Wrong:Play()

                local allPlayersAnswered = true
                for i = 1, numPlayers do
                    if playersWhoAnsweredWrong[i] ~= true then
                        allPlayersAnswered = false
                        break
                    end
                end

                if allPlayersAnswered or currentScore <= 0 then
                    state = "answer_reveal"
                else
                    state = "song_playing"
                    previewStarted = true
                    startSongPreview()
                    scoreDropCounter = COUNTER:CreateCounterDuration(0, 1, 2)
                    scoreDropCounter:Start()
                end
            end
        end

    -- Player 3
    elseif answeringPlayer == 3 then
        if INPUT:Pressed("LBlue3P") or INPUT:Pressed("RBlue3P") then
            if quizSongList ~= nil then
                quizSongList:Move(1)
                refreshQuizSongListCache()
                SHARED:GetSharedSound("Skip"):Play()
            end
        end

        if INPUT:Pressed("LRed3P") or INPUT:Pressed("RRed3P") then
            local selectedSong = quizSongList:GetSelectedSongNode()

            if selectedSong ~= nil and selectedSong.UniqueId == correctSongNode.UniqueId then
                addScore(answeringPlayer, currentScore)
                sounds.Right:Play()
                state = "answer_reveal"
            else
                addScore(answeringPlayer, -10)
                playersWhoAnsweredWrong[answeringPlayer] = true
                sounds.Wrong:Play()

                local allPlayersAnswered = true
                for i = 1, numPlayers do
                    if playersWhoAnsweredWrong[i] ~= true then
                        allPlayersAnswered = false
                        break
                    end
                end

                if allPlayersAnswered or currentScore <= 0 then
                    state = "answer_reveal"
                else
                    state = "song_playing"
                    previewStarted = true
                    startSongPreview()
                    scoreDropCounter = COUNTER:CreateCounterDuration(0, 1, 2)
                    scoreDropCounter:Start()
                end
            end
        end

    -- Player 4
    elseif answeringPlayer == 4 then
        if INPUT:Pressed("LBlue4P") or INPUT:Pressed("RBlue4P") then
            if quizSongList ~= nil then
                quizSongList:Move(1)
                refreshQuizSongListCache()
                SHARED:GetSharedSound("Skip"):Play()
            end
        end

        if INPUT:Pressed("LRed4P") or INPUT:Pressed("RRed4P") then
            local selectedSong = quizSongList:GetSelectedSongNode()

            if selectedSong ~= nil and selectedSong.UniqueId == correctSongNode.UniqueId then
                addScore(answeringPlayer, currentScore)
                sounds.Right:Play()
                state = "answer_reveal"
            else
                addScore(answeringPlayer, -10)
                playersWhoAnsweredWrong[answeringPlayer] = true
                sounds.Wrong:Play()

                local allPlayersAnswered = true
                for i = 1, numPlayers do
                    if playersWhoAnsweredWrong[i] ~= true then
                        allPlayersAnswered = false
                        break
                    end
                end

                if allPlayersAnswered or currentScore <= 0 then
                    state = "answer_reveal"
                else
                    state = "song_playing"
                    previewStarted = true
                    startSongPreview()
                    scoreDropCounter = COUNTER:CreateCounterDuration(0, 1, 2)
                    scoreDropCounter:Start()
                end
            end
        end

    -- Player 5
    elseif answeringPlayer == 5 then
        if INPUT:Pressed("LBlue5P") or INPUT:Pressed("RBlue5P") then
            if quizSongList ~= nil then
                quizSongList:Move(1)
                refreshQuizSongListCache()
                SHARED:GetSharedSound("Skip"):Play()
            end
        end

        if INPUT:Pressed("LRed5P") or INPUT:Pressed("RRed5P") then
            local selectedSong = quizSongList:GetSelectedSongNode()

            if selectedSong ~= nil and selectedSong.UniqueId == correctSongNode.UniqueId then
                addScore(answeringPlayer, currentScore)
                sounds.Right:Play()
                state = "answer_reveal"
            else
                addScore(answeringPlayer, -10)
                playersWhoAnsweredWrong[answeringPlayer] = true
                sounds.Wrong:Play()

                local allPlayersAnswered = true
                for i = 1, numPlayers do
                    if playersWhoAnsweredWrong[i] ~= true then
                        allPlayersAnswered = false
                        break
                    end
                end

                if allPlayersAnswered or currentScore <= 0 then
                    state = "answer_reveal"
                else
                    state = "song_playing"
                    previewStarted = true
                    startSongPreview()
                    scoreDropCounter = COUNTER:CreateCounterDuration(0, 1, 2)
                    scoreDropCounter:Start()
                end
            end
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
        CONFIG.PlayerCount = originalPlayerCount
        state = "setup"
        Setup.reset()
        SHARED:GetSharedSound("Decide"):Play()
        return false
    end

    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        -- Reset player count to original
        CONFIG.PlayerCount = originalPlayerCount
        SHARED:GetSharedSound("Cancel"):Play()
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
        SHARED:GetSharedSound("Cancel"):Play()
    end
end

local function handleSoloPlaying()
    if answerTimerCounter == nil then
        answerTimerCounter = COUNTER:CreateCounterDuration(0, 1, 1) -- 1 second delay
        answerTimerCounter:Start()
				sounds.Question:Play()
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
        sounds.Answering:Play()
    end

    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        stopSongPreview()
        previewStarted = false
        state = "solo_results"
        SHARED:GetSharedSound("Cancel"):Play()
    end
end

local function handleSoloAnswering()
    -- Navigate song list with preview still playing

    if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
        if quizSongList ~= nil then
            quizSongList:Move(1)
            refreshQuizSongListCache()
            SHARED:GetSharedSound("Skip"):Play()
        end
    end

    if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
        if quizSongList ~= nil then
            quizSongList:Move(-1)
            refreshQuizSongListCache()
            SHARED:GetSharedSound("Skip"):Play()
        end
    end

    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
        local selectedSong = quizSongList:GetSelectedSongNode()

        if selectedSong ~= nil and selectedSong.UniqueId == correctSongNode.UniqueId then
            -- Correct!
            soloScore = soloScore + 1
            stopSongPreview()
            sounds.Right:Play()

            -- Brief celebration then next round
            cutsceneTimer = 0
            state = "solo_correct"
        else
            -- Wrong! Game over
            stopSongPreview()
            sounds.Wrong:Play()

						cutsceneTimer = 0
            state = "solo_mistake"
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

local function handleSoloMistake()
	-- Brief fanfare but for loosers
	if celebrationCounter == nil then
			celebrationCounter = COUNTER:CreateCounterDuration(0, 1, 2) -- 2 seconds
			celebrationCounter:Start()
	end

	celebrationCounter:Tick()

	if celebrationCounter.Value >= 1 or INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
			celebrationCounter = nil
			state = "solo_results"
	end
end

local function handleSoloResults()
    -- Show final score and high scores
    local isNewHighScore, rank = checkIfNewHighScore(soloScore)

    if isNewHighScore and highScoreRegistered == false then
        local playerName = save.Name
				saveHighScore(soloScore, playerName)
				loadHighScores()

				highScoreRegistered = true

        -- Check if beat Nokon's score
        if checkIfBeatNokon(soloScore) then
            grantKeyOfPride()
            return -- Pride modal will take over
        end
    end

    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
        -- Reset player count to original
        CONFIG.PlayerCount = originalPlayerCount
        highScoreRegistered = false
        state = "setup"
        Setup.reset()
        SHARED:GetSharedSound("Decide"):Play()
        return false
    end

    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        -- Reset player count to original
        CONFIG.PlayerCount = originalPlayerCount
				highScoreRegistered = false
        SHARED:GetSharedSound("Cancel"):Play()
        return true
    end
end

local function handlePrideModal()
    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
        showPrideModal = false

        -- Show results
        state = "solo_results"
        SHARED:GetSharedSound("Decide"):Play()
    end
end

---------------------------------------
-- Main Functions
---------------------------------------

function draw()
    -- TODO: Add background texture
    -- textures["Background"]:Draw(0, 0)

    if state == "waiting_enum" then
        if text ~= nil then
            text:GetText("Loading songs..."):DrawAtAnchor(960, 540, "center")
        end

    elseif state == "opening" then
        Opening.draw()

    elseif state == "setup" then
        Setup.draw()

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
                local genreText = text:GetText(genreNode.Title .. " (" .. calculateMaxScore(genreNode) .. ")", false, 99999,
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

						-- Show current score
        		local scoreText = text:GetText("Current Score: " .. currentScore .. " / " .. maxScoreForRound)
        		scoreText:DrawAtAnchor(960, 550, "center")
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
                local scoreText = text:GetText(playerNames[i] .. ": " .. playerScores[i])
                scoreText:DrawAtAnchor(200 + i * 200, 600, "center")
            end
        end

    elseif state == "results" then
        if text ~= nil then
            local headerText = text:GetText("Final Results!")
            headerText:DrawAtAnchor(960, 200, "center")

            -- DEBUG: Show why we got here
            local debugText = text:GetText("Rounds completed: " .. (currentRound) .. " / " .. numRounds)
            debugText:DrawAtAnchor(960, 250, "center")

            -- Sort and display scores
            local sortedPlayers = {}
            for i = 1, numPlayers do
                table.insert(sortedPlayers, {index = i, score = playerScores[i]})
            end
            table.sort(sortedPlayers, function(a, b) return a.score > b.score end)

            for rank, player in ipairs(sortedPlayers) do
                local rankText = text:GetText(rank .. ". " .. playerNames[player.index] .. ": " .. player.score)
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

		elseif state == "solo_mistake" then
	      if text ~= nil then
	          local correctText = text:GetText("Too Bad!")
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

    -- Draw nameplates for all players (not during the opening sequence)
    if active and state ~= "opening" and state ~= "setup" then
        for i = 0, CONFIG.PlayerCount - 1 do
            NAMEPLATE:DrawPlayerNameplate(20 + i * 370, 980, 255, i)
        end
    end
end

function update()
		local quitted = false

    if state == "waiting_enum" then
        handleWaitingEnum()
    elseif state == "opening" then
        quitted = handleOpening()
    elseif state == "setup" then
        quitted = handleSetup()
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
		elseif state == "solo_mistake" then
				handleSoloMistake()
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
    playerNames = {}
    for i = 1, 5 do
        playerNames[i] = GetSaveFile(i - 1).Name
    end
    active = true

    originalPlayerCount = CONFIG.PlayerCount
    loadHighScores()

    if songsEnumerated then
        state = "opening"
        Opening.reset()
    else
        state = "waiting_enum"
    end
end

function deactivate()
    -- Reset player count to original
    CONFIG.PlayerCount = originalPlayerCount

    stopBGM()
    stopSongPreview()
    active = false
end

function onStart()
    text = TEXT:Create(16)

    -- Sounds
    sounds.BGM          = SOUND:CreateBGM("Sounds/BGM.ogg")
    sounds.CurtainOpen  = SOUND:CreateSFX("Sounds/CurtainOpen.ogg")
    sounds.Answering    = SOUND:CreateSFX("Sounds/Answering.ogg")
    sounds.Drumroll     = SOUND:CreateSFX("Sounds/Drumroll.ogg")
    sounds.KeyGot       = SOUND:CreateSFX("Sounds/KeyGot.ogg")
    sounds.Question     = SOUND:CreateSFX("Sounds/Question.ogg")
    sounds.ResultsMulti = SOUND:CreateSFX("Sounds/ResultsMulti.ogg")
    sounds.ResultsSolo  = SOUND:CreateSFX("Sounds/ResultsSolo.ogg")
    sounds.Right        = SOUND:CreateSFX("Sounds/Right.ogg")
    sounds.Wrong        = SOUND:CreateSFX("Sounds/Wrong.ogg")

    -- Opening textures
    textures["Curtain"]     = TEXTURE:CreateTexture("Textures/Curtain.png")
    textures["CurtainOpen"] = TEXTURE:CreateTexture("Textures/Curtain_Open.png")
    textures["Background"]  = TEXTURE:CreateTexture("Textures/Background.png")
    textures["Light"]       = TEXTURE:CreateTexture("Textures/Light.png")
    textures["Logo"]        = TEXTURE:CreateTexture("Textures/Logo.png")
    textures["Nokon2"]      = TEXTURE:CreateTexture("Textures/Nokon2.png")
    textures["Start"]       = TEXTURE:CreateTexture("Textures/Start.png")
    textures["Back"]        = TEXTURE:CreateTexture("Textures/Back.png")

    -- Setup textures
    textures["mode_endurance"]  = TEXTURE:CreateTexture("Textures/Options/GameMode/Endurance.png")
    textures["mode_vs"]         = TEXTURE:CreateTexture("Textures/Options/GameMode/VS.png")
    for i = 1, 5 do
        textures["player_" .. i] = TEXTURE:CreateTexture("Textures/Options/PlayerCount/" .. i .. ".png")
    end
    textures["songs_optk"]      = TEXTURE:CreateTexture("Textures/Options/SongType/OpTk.png")
    textures["songs_custom"]    = TEXTURE:CreateTexture("Textures/Options/SongType/Custom.png")
    textures["songs_all"]       = TEXTURE:CreateTexture("Textures/Options/SongType/All.png")
    textures["go"]              = TEXTURE:CreateTexture("Textures/Go.png")

    -- Text renderers
    texts.title = TEXT:Create(36)
    texts.label = TEXT:Create(22)

    Opening.init(textures, sounds)
    Setup.init(textures, sounds, texts)
end

function afterSongEnum()
    songsEnumerated = true
    loadMainSongList()

    if active and state == "waiting_enum" then
        state = "opening"
        Opening.reset()
    end
end

function onDestroy()
    if text ~= nil then text:Dispose() end
    for _, t in pairs(texts) do if t ~= nil then t:Dispose() end end

    for _, sound in pairs(sounds) do
        if sound ~= nil then sound:Dispose() end
    end

    for _, texture in pairs(textures) do
        if texture ~= nil then texture:Dispose() end
    end
end
