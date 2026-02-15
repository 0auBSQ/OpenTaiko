local textSmall = nil
local text = nil
local textLarge = nil

local textMenuState = nil
local textTex = nil

local sounds = {}

local songList = nil
local currentPage = {}
local pageTexts = {}
local genre_overlays = {}

local bars = {}
local bgtx = {}

local favoriteicon = nil

local ctx = {}

local currentBackground = 0
local backgroundScrollX = 0

local difficultySelection = false
local diffIndex = {-2, -2, -2, -2, -2}

-- UI constants
local SONGLIST_ORIGIN_X = 660
local SONGLIST_ORIGIN_Y = 500
local SONGLIST_OFFSET_X = 45
local SONGLIST_OFFSET_Y = 120
local SONGLIST_TEXT_OFFSET_X = -65
local SONGLIST_TEXT_OFFSET_Y = 15
local SONGLIST_SELECTED_X_DIFF = 20

local PREIMAGE_ORIGIN_X = 1276
local PREIMAGE_ORIGIN_Y = 146
local PREIMAGE_SIZE_X = 500
local PREIMAGE_SIZE_Y = 500

local HEADER_OFFSET_X = 1780
local HEADER_BOX_TEXT_OFFSET_X = 250
local HEADER_BOX_TEXT_OFFSET_Y = 12

local function reloadPreimage(songNode)
	if songNode.IsSong == true then 
		if songNode.HasPreimage then
			SHARED:SetSharedTextureUsingAbsolutePath("preimage", songNode.PreimagePath)
		else
			SHARED:SetSharedTexture("preimage", "Textures/preimage.png")
		end
	else
		SHARED:ClearSharedTexture("preimage")
	end
	SHARED:GetSharedTexture("preimage"):SetWrapMode("Border")
end

local function drawPreimage()
	local tex = SHARED:GetSharedTexture("preimage")
	if tex.Height > 0 and tex.Width > 0 then
		local sH = PREIMAGE_SIZE_X / tex.Height
		local sW = PREIMAGE_SIZE_Y / tex.Width
		tex:SetScale(sW, sH)
		tex:Draw(PREIMAGE_ORIGIN_X, PREIMAGE_ORIGIN_Y)
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
		else
			if i == 0 then
				pageTexts[i] = text:GetText(node.Title, false, 525, COLOR:CreateColorFromARGB(255,242,207,1))
			else
				pageTexts[i] = text:GetText(node.Title, false, 525)
			end

			if genre_overlays[node.Genre] == nil then
				if TEXTURE:Exists("Textures/Overlay/"..node.Genre..".png") then
					genre_overlays[node.Genre] = TEXTURE:CreateTexture("Textures/Overlay/"..node.Genre..".png")
				else
					genre_overlays[node.Genre] = TEXTURE:CreateTexture()
				end
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
			sounds.Cancel:Play()
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
	if songList == nil then return Exit("title", nil) end
	-- if no folder to close, trigger exit scene instead
	return songList:CloseFolder()
end


function draw()
	SHARED:GetSharedTexture("background"):Draw(-backgroundScrollX,0)
	SHARED:GetSharedTexture("background"):Draw(-backgroundScrollX+1920,0)

	-- Song info
	if currentPage[0].IsSong then
		bgtx["songinfo"]:DrawAtAnchor(1920,0,"topright")
	end
	
	drawPreimage()

	-- Song List
	if pageTexts ~= nil then
		for i, tx in pairs(pageTexts) do
			local xpos = SONGLIST_ORIGIN_X+i*SONGLIST_OFFSET_X
			local ypos = SONGLIST_ORIGIN_Y+i*SONGLIST_OFFSET_Y
			-- shift the box if selected to highlight it
			if i == 0 then
				xpos = xpos + SONGLIST_SELECTED_X_DIFF
			end
			-- can be nil if no modulo pagination
			if tx ~= nil then 
				if currentPage[i].IsSong or currentPage[i].IsFolder then
					if currentPage[i].IsLocked then
						bars["locked"]:DrawAtAnchor(xpos,ypos,"center")
					else
						bars["bar"]:SetColor(currentPage[i].BoxColor)
						bars["bar"]:DrawAtAnchor(xpos,ypos,"center")
						genre_overlays[currentPage[i].Genre]:DrawAtAnchor(xpos,ypos,"center")
					end
				elseif currentPage[i].IsRandom then
					bars["random"]:DrawAtAnchor(xpos,ypos,"center")
				elseif currentPage[i].IsReturn then
					bars["back"]:DrawAtAnchor(xpos,ypos,"center")
				end
				tx:DrawAtAnchor(xpos+SONGLIST_TEXT_OFFSET_X, ypos+SONGLIST_TEXT_OFFSET_Y,"center")
				if i == 0 then
					bars["selected"]:DrawAtAnchor(xpos,ypos,"center")
					bars["selected-arrows"]:DrawAtAnchor(xpos,ypos,"center")
				end
			end
		end
	end

	-- Folder Path
	bgtx["header"]:Draw(0, 0)
	local ssn = songList:GetSelectedSongNode()

	if ssn ~= nil then
		local pathStack = {}
		local currentNode = ssn.Parent

		-- Traverse up the tree
		while currentNode ~= nil do
			-- The root node as a Title of nil
			local _tx = "/"
			if currentNode.Title ~= nil then
				_tx = currentNode.Title
			end
			local textobj = text:GetText(_tx, false, 270)
			table.insert(pathStack, textobj)
			currentNode = currentNode.Parent
		end

		local xpos = HEADER_OFFSET_X

		for i, title in ipairs(pathStack) do
			bgtx["header-box"]:DrawAtAnchor(xpos, 0, "topright")
			title:DrawAtAnchor(xpos-bgtx["header-box"].Width+HEADER_BOX_TEXT_OFFSET_X, HEADER_BOX_TEXT_OFFSET_Y+bgtx["header-box"].Height/2, "center")
			if i ~= #pathStack then
				bgtx["header-arrow"]:DrawAtAnchor(xpos-bgtx["header-box"].Width, 0, "topright")
			end
			xpos = xpos - bgtx["header-box"].Width - bgtx["header-arrow"].Width
		end
	end

	-- if favoriteicon ~= nil then
	-- 	favoriteicon:Draw(1200, 400)
	-- end

	bgtx["overlay"]:Draw(0, 0)

	if textMenuState ~= nil then
		textMenuState:DrawAtAnchor(270,65,"Center")
	end

	NAMEPLATE:DrawPlayerNameplate(0, 1000, 255, 0)
end

function update()
	for k, counter in pairs(ctx) do
        counter:Tick()
    end

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
	textMenuState = textLarge:GetText("Select a song!")
	textTex = textSmall:GetText("You've played " .. tostring(test.TotalPlaycount) .. " charts.\nHow many more will you play?")

	sounds.Skip = SHARED:GetSharedSound("Skip")
	sounds.Cancel = SHARED:GetSharedSound("Cancel")
	sounds.Decide = SHARED:GetSharedSound("Decide")
	sounds.SongDecide = SHARED:GetSharedSound("SongDecide")

	ctx["background"] = COUNTER:CreateCounter(1920, 0, 1 / 48)
	ctx["background"]:SetLoop(true)
	ctx["background"]:Listen(function (val)
		backgroundScrollX = val
	end)
	ctx["background"]:Start()
end

function deactivate()
	for k, counter in pairs(ctx) do
        counter = COUNTER:EmptyCounter()
    end

	local psnd = SHARED:GetSharedSound("presound")
	psnd:Stop()
end

function onStart()
	textSmall = TEXT:Create(18)
	text = TEXT:Create(28)
	textLarge = TEXT:Create(42)
	
	SHARED:SetSharedTexture("background", "Textures/bg0.png")
	bgtx["overlay"] = TEXTURE:CreateTexture("Textures/bg_overlay.png")
	bgtx["songinfo"] = TEXTURE:CreateTexture("Textures/bg_songinfo.png")
	bgtx["header"] = TEXTURE:CreateTexture("Textures/bg_header.png")
	bgtx["header-box"] = TEXTURE:CreateTexture("Textures/bg_header-box.png")
	bgtx["header-arrow"] = TEXTURE:CreateTexture("Textures/bg_header-arrow.png")
	bars["bar"] = TEXTURE:CreateTexture("Textures/bar.png")
	bars["random"] = TEXTURE:CreateTexture("Textures/random.png")
	bars["back"] = TEXTURE:CreateTexture("Textures/back.png")
	bars["locked"] = TEXTURE:CreateTexture("Textures/locked.png")
	bars["selected"] = TEXTURE:CreateTexture("Textures/selected.png")
	bars["selected-arrows"] = TEXTURE:CreateTexture("Textures/selected-arrows.png")

	favoriteicon = TEXTURE:CreateTexture("Textures/fav.png")

	genre_overlays = {}

end 

function afterSongEnum()
	local lsls = GenerateSongListSettings()

	lsls:SetExcludedGenreFolders({"段位道場", "太鼓タワー"})

	lsls.ModuloPagination = false
	lsls.HideEmptyFolders = true
	lsls.FlattenOpenedFolders = false
	
	-- lsls.RootGenreFolder = "太鼓タワー"

	-- Get song list 
	songList = RequestSongList(lsls)
	refreshPage()
end

function onDestroy()
	if text ~= nil then
		text:Dispose()
	end
	if favoriteicon ~= nil then
		favoriteicon:Dispose()
	end
	-- for _, sound in pairs(sounds) do
	-- 	sound:Dispose()
	-- end
	for _, bar in pairs(bars) do
		bar:Dispose()
	end
	for _, overlay in pairs(genre_overlays) do
		overlay:Dispose()
	end
end
