---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local, redundant-parameter, inject-field
local DBScores  = require("DBControllers/dbScores")
local Opening   = require("opening")
local Setup     = require("setup")
local Dialogue  = require("dialogue")
local Stage     = require("stage")
local Game      = require("game")

local text  = nil
local texts = {}

local playerNames = {}

local sounds   = {}
local textures = {}

local songList        = nil
local quizSongList    = nil
local currentSongNode = nil

local active          = false
local songsEnumerated = false

-- State machine: waiting_enum → opening → setup → game_transition → game
local state = "waiting_enum"

local songScope           = "All"
local originalPlayerCount = 1
local currentPageCache    = {}
local pendingConfig       = nil

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
    local songCount = genreSongList.SongCount
    if songCount == 0 then return 10 end
    local roundedScore = math.ceil(math.log(songCount)) * 10
    return math.max(10, roundedScore)
end

local function csharpEnumerableToLuaArray(enumerable)
    local luaArray = {}
    if enumerable == nil or not enumerable.GetEnumerator then return luaArray end
    local enumerator = enumerable:GetEnumerator()
    while enumerator:MoveNext() do
        table.insert(luaArray, enumerator.Current)
    end
    return luaArray
end

local function loadHighScores()
    -- GetScores() returns Dictionary<int, Dictionary<string, object?>>
    -- Outer KVP: .Key = row index, .Value = inner dict with "player"/"score" keys
    local result = {}
    local outerDict = DBScores:GetScores()
    if outerDict == nil then return result end
    local outerEnum = outerDict:GetEnumerator()
    while outerEnum:MoveNext() do
        local rowDict = outerEnum.Current.Value
        if rowDict ~= nil then
            table.insert(result, {
                name  = tostring(rowDict.player  or rowDict["player"]  or ""),
                score = tonumber(tostring(rowDict.score or rowDict["score"] or 0)) or 0,
            })
        end
    end
    return result
end

local function saveHighScore(score, name)
    DBScores:RegisterScore(name, score)
end

---------------------------------------
-- Song List Management
---------------------------------------

local function loadMainSongList()
    local lsls = GenerateSongListSettings()
    lsls.ModuloPagination     = false
    lsls.AppendMainRandomBox  = false
    lsls.AppendSubRandomBoxes = false
    lsls.SubBackBoxFrequency  = 0
    lsls.ExcludeLockedSongs   = true

    if songScope == "Customs" then
        lsls.RootGenreFolder = "Custom Charts"
    elseif songScope == "OpTk" then
        lsls:SetExcludedGenreFolders({"Custom Charts", "Download", "段位道場", "太鼓タワー", "Favorite", "最近遊んだ曲", "SearchD", "SearchT", "Secret Vault"})
    else -- "All"
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
                    i == 0 and COLOR:CreateColorFromARGB(255, 242, 207, 1) or nil),
            }
        else
            currentPageCache[i] = nil
        end
    end
end

local function selectRandomSongFromGenre(genreFolder)
    if genreFolder == nil then
        if songList == nil then return false end
        currentSongNode = songList:GetRandomNodeInFolder(songList:GetSelectedSongNode(), true, function(node)
            return songList:GetSelectedSongNode().Parent ~= node.Parent
        end)
        if currentSongNode == nil or currentSongNode.IsSong == false then return false end
        local lsls = GenerateSongListSettings()
        lsls.ModuloPagination     = false
        lsls.AppendMainRandomBox  = false
        lsls.AppendSubRandomBoxes = false
        lsls.SubBackBoxFrequency  = 0
        lsls.ExcludeLockedSongs   = true
        lsls.RootGenreFolderNode  = currentSongNode.Parent
        quizSongList = RequestSongList(lsls)
        refreshQuizSongListCache()
        return true
    end

    local lsls = GenerateSongListSettings()
    lsls.ModuloPagination     = false
    lsls.AppendMainRandomBox  = false
    lsls.AppendSubRandomBoxes = false
    lsls.SubBackBoxFrequency  = 0
    lsls.ExcludeLockedSongs   = true
    lsls.RootGenreFolderNode  = genreFolder

    local genreSongList = RequestSongList(lsls)
    if genreSongList == nil then return false end

    currentSongNode = genreSongList:GetRandomNodeInFolder(genreSongList:GetSelectedSongNode(), true)
    if currentSongNode == nil or currentSongNode.IsSong == false then return false end

    quizSongList = genreSongList
    refreshQuizSongListCache()
    return true
end

local previewCancelled = false

local function startSongPreview()
    if currentSongNode == nil then return end
    previewCancelled = false
    local psnd = SHARED:GetSharedSound("quiz_preview")
    psnd:Stop()
    if currentSongNode.IsSong == true then
        SHARED:SetSharedPreviewUsingAbsolutePath("quiz_preview", currentSongNode.AudioPath, function(snd)
            if not previewCancelled then
                snd:Play()
                -- snd:SetTimestamp(currentSongNode.DemoStart)
                snd:SetLoop(true)
            end
        end)
    end
end

local function stopSongPreview()
    previewCancelled = true
    SHARED:GetSharedSound("quiz_preview"):Stop()
end

local function getAvailableGenres()
    if songList == nil then return {} end
    local genresCSharp = songList:SearchNodesByPredicate(function(node)
        return node.IsFolder == true and
               node.ChildrenCount == node.SongCount and
               node.SongCount > 0
    end)
    return csharpEnumerableToLuaArray(genresCSharp)
end

---------------------------------------
-- State Handlers
---------------------------------------

local function handleWaitingEnum()
    -- afterSongEnum handles transition
end

local function handleOpening()
    local result = Opening.update()
    if result == "start" then
        state = "setup"
        Setup.reset()
    elseif result == "back" then
        return true
    end
    return false
end

local function handleSetup()
    local result = Setup.update()
    if result == "back" then
        Opening.resetToMenu()
        state = "opening"
    elseif result ~= nil then
        stopBGM()
        SHARED:GetSharedSound("Decide"):Play()
        CONFIG.PlayerCount = result.players
        local names = {}
        for i = 1, result.players do names[i] = playerNames[i] end
        pendingConfig = { mode = result.mode, players = result.players, songs = result.songs, names = names }
        -- Always close the curtain over the setup screen before starting.
        -- forceOpenCurtain resets position in case a previous game left it closed.
        Stage.forceOpenCurtain()
        Stage.closeCurtain(function()
            state = "game"
            Game.start(pendingConfig)
            pendingConfig = nil
        end)
        state = "game_transition"
    end
    return false
end

---------------------------------------
-- Main Functions
---------------------------------------

function draw()
    if state == "waiting_enum" then
        if text ~= nil then
            text:GetText("Loading songs..."):DrawAtAnchor(960, 540, "center")
        end
    elseif state == "opening" then
        Opening.draw()
    elseif state == "setup" then
        Setup.draw()
    elseif state == "game_transition" then
        Setup.draw()             -- setup visuals beneath…
        Stage.drawCurtainOnly()  -- …closing curtain on top
    elseif state == "game" then
        Game.draw()
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
    elseif state == "game_transition" then
        Stage.update()  -- tick curtain animation; callback switches to "game"
    elseif state == "game" then
        local result = Game.update()
        if result == "exit" then
            quitted = true
        elseif result == "play_again" then
            Opening.resetToMenu()
            state = "opening"
        end
    end

    if quitted == true then
        return Exit("title", nil)
    end
end

function activate()
    playerNames = {}
    for i = 1, 5 do
        playerNames[i] = GetSaveFile(i - 1).Name
    end
    active = true
    originalPlayerCount = CONFIG.PlayerCount

    if songsEnumerated then
        state = "opening"
        Opening.reset()
    else
        state = "waiting_enum"
    end
end

function deactivate()
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
    textures["mode_endurance"] = TEXTURE:CreateTexture("Textures/Options/GameMode/Endurance.png")
    textures["mode_vs"]        = TEXTURE:CreateTexture("Textures/Options/GameMode/VS.png")
    for i = 1, 5 do
        textures["player_" .. i] = TEXTURE:CreateTexture("Textures/Options/PlayerCount/" .. i .. ".png")
    end
    textures["songs_optk"]   = TEXTURE:CreateTexture("Textures/Options/SongType/OpTk.png")
    textures["songs_custom"] = TEXTURE:CreateTexture("Textures/Options/SongType/Custom.png")
    textures["songs_all"]    = TEXTURE:CreateTexture("Textures/Options/SongType/All.png")
    textures["go"]           = TEXTURE:CreateTexture("Textures/Go.png")

    -- Game stage textures
    textures["Stage"]             = TEXTURE:CreateTexture("Textures/Stage.png")
    textures["Stand"]             = TEXTURE:CreateTexture("Textures/Stand.png")
    textures["player_body"]       = TEXTURE:CreateTexture("Textures/Player/Body.png")
    textures["player_head"]       = TEXTURE:CreateTexture("Textures/Player/Head.png")
    textures["player_head_down"]  = TEXTURE:CreateTexture("Textures/Player/Head_Down.png")
    textures["player_hands"]      = TEXTURE:CreateTexture("Textures/Player/Hands.png")
    textures["player_hands_down"] = TEXTURE:CreateTexture("Textures/Player/Hands_Down.png")
    textures["Spotlight"]  = TEXTURE:CreateTexture("Textures/Spotlight.png")
    textures["Denied"]     = TEXTURE:CreateTexture("Textures/Denied.png")
    textures["Fail"]       = TEXTURE:CreateTexture("Textures/Fail.png")
    textures["bgtile"]     = TEXTURE:CreateTexture("Textures/BgTile.png")
    textures["Clock"]      = TEXTURE:CreateTexture("Textures/Clock/Clock.png")
    textures["SongList"]   = TEXTURE:CreateTexture("Textures/SongList.png")
    textures["SongBlock"]  = TEXTURE:CreateTexture("Textures/SongBlock.png")
    textures["Dialogue"]   = TEXTURE:CreateTexture("Textures/Dialogue.png")
    for i = 1, 5 do
        textures["chip_" .. i] = TEXTURE:CreateTexture("Textures/Icons/" .. i .. "P.png")
    end

    -- Text renderers
    texts.title    = TEXT:Create(36)
    texts.label    = TEXT:Create(22)
    texts.dialogue = TEXT:Create(20)

    -- Utils table injected into Game module
    local utils = {
        loadSongList = function(songs)
            songScope = songs
            loadMainSongList()
        end,
        selectRandomSong  = function(genre) return selectRandomSongFromGenre(genre) end,
        getSelectedSong   = function() return currentSongNode end,
        getQuizList       = function() return quizSongList end,
        buildPageCache    = function()
            refreshQuizSongListCache()
            return currentPageCache
        end,
        getGenres         = getAvailableGenres,
        calculateMaxScore = calculateMaxScore,
        startPreview      = function() startSongPreview() end,
        stopPreview       = stopSongPreview,
        playSound         = function(name) if sounds[name] then sounds[name]:Play() end end,
        stopSound         = function(name) if sounds[name] then sounds[name]:Stop() end end,
        saveHighScore     = saveHighScore,
        getHighScores     = loadHighScores,
        grantPrideKey     = function()
            local sf = GetSaveFile(0)
            if sf and sf:GetGlobalTrigger(".vault_key_obtained_pride") ~= true then
                sf:SetGlobalTrigger(".vault_key_obtained_pride", true)
            end
        end,
    }

    Opening.init(textures, sounds)
    Setup.init(textures, sounds, texts)
    Dialogue.init(textures, texts)
    Stage.init(textures, sounds, texts)
    Game.init(Stage, Dialogue, utils, texts)
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
    for _, sound in pairs(sounds) do if sound ~= nil then sound:Dispose() end end
    for _, texture in pairs(textures) do if texture ~= nil then texture:Dispose() end end
end
