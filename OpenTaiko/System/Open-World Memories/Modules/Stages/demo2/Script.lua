local text = nil
local textTex = nil

local sounds = {}

local songList = nil
local currentPage = {}
local pageTexts = {}
local genre_overlays = {}

local bars = {}

local favoriteicon = nil
local flashcounter = nil

local currentBackground = 0

local function reloadPreimage(songNode)
	if songNode.IsSong == true then 
		SHARED:SetSharedTextureUsingAbsolutePath("preimage", songNode.PreimagePath)
	else
		SHARED:SetSharedTextureUsingAbsolutePath("preimage", '')
	end
end

local function drawPreimage()
	local tex = SHARED:GetSharedTexture("preimage")
	if tex.Height > 0 and tex.Width > 0 then
		local sH = 400 / tex.Height
		local sW = 400 / tex.Width
		tex:SetScale(sW, sH)
		tex:Draw(800, 400)
	end
end

local function playPreview(songNode)
	local psnd = SHARED:GetSharedSound("presound")
	psnd:Stop()
	if songNode.IsSong == true then
		SHARED:SetSharedPreviewUsingAbsolutePath("presound", songNode.AudioPath, function (snd)
			snd:Play()
			snd:SetTimestamp(songNode.DemoStart)
		end)
	end
end

local function refreshPage()
	currentPage = {}
	pageTexts = {}

	for i = -5,5 do
		local node = songList:GetSongNodeAtOffset(i)
		currentPage[i] = node
		if node == nil then pageTexts[i] = nil
		elseif i == 0 then
			pageTexts[i] = text:GetText(node.Title, false, 99999, COLOR:CreateColorFromARGB(255,242,207,1))
			if genre_overlays[node.Genre] == nil then
				genre_overlays[node.Genre] = TEXTURE:CreateTexture("Textures/Overlay/"..node.Genre..".png")
			end
		else
			pageTexts[i] = text:GetText(node.Title)
			if genre_overlays[node.Genre] == nil then
				genre_overlays[node.Genre] = TEXTURE:CreateTexture("Textures/Overlay/"..node.Genre..".png")
			end
		end

		-- Assets reload for selected songs
		if i == 0  and node ~= nil then
			reloadPreimage(node)
			playPreview(node)
		end
	end
end

local function handleDecide()
	local ssn = songList:GetSelectedSongNode()

	if ssn.IsFolder == true then
		local success = songList:OpenFolder()
		refreshPage()
		if success == true then
			sounds.Decide:Play()
		end
	elseif ssn.IsReturn == true then
		local success = songList:CloseFolder()
		refreshPage()
		if success == true then
			sounds.Decide:Play()
		end
	elseif ssn.IsSong == true then
		local success = ssn:Mount(3)
		-- local success = ssn:Mount(5) -- for testing, go directly for tower and only 1P
		if success == true then
			sounds.SongDecide:Play()
			return true -- transition to song select if true
		end 
	elseif ssn.IsRandom == true then
		local rdNd = songList:GetRandomNodeInFolder(ssn)
		if rdNd ~= nil then
			local success = rdNd:Mount(3)
			-- local success = rdNd:Mount(5) -- for testing, go directly for tower and only 1P
			if success == true then
				sounds.SongDecide:Play()
				return true -- transition to song select if true
			end
		end
	end
	-- any route note ending up to play a song return false
	return false
end

local function handleFolderClose()
	if songlist == nil then return Exit("title", nil) end
	-- if no folder to close, trigger exit scene instead
	return songList:CloseFolder()
end


function draw()
	SHARED:GetSharedTexture("background"):Draw(0,0)
	drawPreimage()

	if textTex ~= nil then
		textTex:Draw(400,400)
	end
	if pageTexts ~= nil then
		for i, tx in pairs(pageTexts) do
			-- can be nil if no modulo pagination
			if tx ~= nil then 
				if currentPage[i].IsSong or currentPage[i].IsFolder then
					if currentPage[i].IsLocked then
						bars["locked"]:DrawAtAnchor(400,500+i*100,"Center")
					else
						bars["bar"]:SetColor(currentPage[i].BoxColor)
						bars["bar"]:DrawAtAnchor(400,500+i*100,"Center")
						genre_overlays[currentPage[i].Genre]:DrawAtAnchor(400,500+i*100,"Center")
					end
				elseif currentPage[i].IsRandom then
					bars["random"]:DrawAtAnchor(400,500+i*100,"Center")
				elseif currentPage[i].IsReturn then
					bars["back"]:DrawAtAnchor(400,500+i*100,"Center")
				end
				tx:DrawAtAnchor(360, 500+i*100,"Center")
			end
		end
	end

	NAMEPLATE:DrawPlayerNameplate(800, 100, 255, 0)

	if favoriteicon ~= nil then
		favoriteicon:Draw(1200, 400)
	end
end

function update()
	flashcounter:Tick()

	if INPUT:KeyboardPressed("S") then
		sounds.Skip:Play()
		return Exit("stage", "demo1")
	end

	-- Navigation
	if (INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow")) and songList ~= nil then
		sounds.Skip:Play()
		songList:Move(1)
		refreshPage()
	end
	if (INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow")) and songList ~= nil then
		sounds.Skip:Play()
		songList:Move(-1)
		refreshPage()
	end
	if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
		local isPlayStarted = handleDecide()
		if isPlayStarted == true then
			return Exit("play", nil)
		end 
	end
	if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
		local closeFolder = handleFolderClose()
		if closeFolder == true then
			refreshPage()
			sounds.Decide:Play()
		else
			sounds.Cancel:Play()
			return Exit("title", nil)
		end
	end

	-- Test search song
	if INPUT:KeyboardPressed("P") then
		local sNode = songList:SearchFirstSongByPredicate(function(node)
		 	return node.Maker == "bol"
		end)
		-- local sNode = songList:GetSongByUniqueId("swtowerwttcSukima")
		if sNode ~= nil then
			local success = sNode:Mount(3)
			if success == true then
				sounds.SongDecide:Play()
				return Exit("play", nil)
			end 
		end
	end

	-- Test shared textures
	if INPUT:KeyboardPressed("O") then
		if currentBackground == 0 then
			SHARED:SetSharedTexture("background", "Textures/bg1.png")
			currentBackground = 1
		else
			SHARED:SetSharedTexture("background", "Textures/bg0.png")
			currentBackground = 0
		end
	end 
end

function activate()
	test = GetSaveFile(0)
	-- textTex = text:GetText("You played " .. tostring(test.TotalPlaycount) .. " charts...")

	flashcounter = COUNTER:CreateCounter(255, 0, -1 / 127)
	flashcounter:SetBounce(true)
	flashcounter:Listen(function (val)
		if favoriteicon ~= nil then
			favoriteicon:SetOpacity(val / 255)
		end
	end)
	flashcounter:Start()
end

function deactivate()
	flashcounter = COUNTER:EmptyCounter()

	local psnd = SHARED:GetSharedSound("presound")
	psnd:Stop()
end

function onStart()
	text = TEXT:Create(16)

	sounds.Skip = SOUND:CreateSFX("Sounds/Skip.ogg")
	sounds.Cancel = SOUND:CreateSFX("Sounds/Cancel.ogg")
	sounds.Decide = SOUND:CreateSFX("Sounds/Decide.ogg")
	sounds.SongDecide = SOUND:CreateSFX("Sounds/SongDecide.ogg")

	SHARED:SetSharedTexture("background", "Textures/bg0.png")
	bars["bar"] = TEXTURE:CreateTexture("Textures/bar.png")
	bars["random"] = TEXTURE:CreateTexture("Textures/random.png")
	bars["back"] = TEXTURE:CreateTexture("Textures/back.png")
	bars["locked"] = TEXTURE:CreateTexture("Textures/locked.png")

	favoriteicon = TEXTURE:CreateTexture("Textures/fav.png")

	genre_overlays = {}

	flashcounter = COUNTER:EmptyCounter()
end 

function afterSongEnum()
	local lsls = GenerateSongListSettings()
	-- Test options here
	lsls.ModuloPagination = false
	-- lsls.RootGenreFolder = "太鼓タワー"

	-- Get song list 
	songList = RequestSongList(lsls)
	refreshPage()
end

function onDestroy()
	if textTex ~= nil then
		textTex:Dispose()
	end
	if favoriteicon ~= nil then
		favoriteicon:Dispose()
	end
	for _, sound in pairs(sounds) do
		sound:Dispose()
	end
	for _, bar in pairs(bars) do
		bar:Dispose()
	end
	for _, overlay in pairs(genre_overlays) do
		overlay:Dispose()
	end
end
