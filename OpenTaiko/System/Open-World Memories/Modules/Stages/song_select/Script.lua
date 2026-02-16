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

-- Animations and counters
local ctx = {}

local currentBackground = 0
local backgroundScrollX = 0

local levelLabelFrame = 0
local difficultyFade4 = 0

local arrowsDistance = 0

-- ???
local difficultySelection = false
local diffIndex = {-2, -2, -2, -2, -2}

-- UI constants
local SONGLIST_ORIGIN_X = 660
local SONGLIST_ORIGIN_Y = 500
local SONGLIST_OFFSET_X = 45
local SONGLIST_OFFSET_Y = 120
local SONGLIST_TEXT_OFFSET_X = -65
local SONGLIST_TEXT_OFFSET_Y = 15
local SONGLIST_SELECTED_X_DIFF = 50
local SONGLIST_SELECTED_ARROW_GAP = 925

local SONGBAR_LEVEL_EX_ANIMATION_FRAMECOUNT = 20
local SONGBAR_LABEL_X_OFFSET = 288

local SONGINFO_DIFFICULTIES_ORIGIN_X = 1790
local SONGINFO_DIFFICULTIES_ORIGIN_Y = 154
local SONGINFO_DIFFICULTIES_GAP_Y = 130
local SONGINFO_HASVIDEO_ORIGIN_X = 1064
local SONGINFO_HASVIDEO_ORIGIN_Y = 257
local SONGINFO_EXPLICIT_ORIGIN_X = 1266
local SONGINFO_EXPLICIT_ORIGIN_Y = 151

local PREIMAGE_ORIGIN_X = 1276
local PREIMAGE_ORIGIN_Y = 146
local PREIMAGE_SIZE_X = 500
local PREIMAGE_SIZE_Y = 500

local HEADER_OFFSET_X = 1780
local HEADER_BOX_TEXT_OFFSET_X = 250
local HEADER_BOX_TEXT_OFFSET_Y = 12

local NAMEPLATE_BOX_FOLDED_SIZE_Y = 182
local NAMEPLATE_BOX_START_X = 0
local NAMEPLATE_BOX_SPACING_X = 384
local NAMEPLATE_OFFSET_X = 27
local NAMEPLATE_OFFSET_Y = 37

-- Used for difficulty number on song bars, no texture/number if nil
local function getSongNodeFocusChart(songNode)
	if songNode.IsSong ~= true then
		return nil
	end
	local default = math.min(4, CONFIG:GetDefaultCourse(0))
	local chart = songNode:GetChart(default)
	local i = 4
	while chart == nil and i >= 0 do
		chart = songNode:GetChart(i)
		i = i-1
	end
	return chart
end

local function calculateNumberWidth(nb, txstr)
    local str = tostring(nb)
    local totalWidth = 0
    local prevWidth = 0
    
    for i = 1, #str do
        local digit = string.sub(str, i, i)
        local tex = bgtx[txstr .. digit]
        
        if tex then
            local w = tex.Width
            
            if i == 1 then
                totalWidth = w
            else
                -- overlap offset (50%)
                totalWidth = totalWidth + (prevWidth * 0.5)
            end
            prevWidth = w
        end
    end
    return totalWidth
end

local function drawNumberCentered(nb, txstr, x, y, color)
	color = color or COLOR:CreateColorFromHex("ffffffff")

    local str = tostring(nb)
    local totalWidth = calculateNumberWidth(nb, txstr)
    
    local cursorX = x - (totalWidth / 2)
    local prevWidth = 0
    
    for i = 1, #str do
        local digit = string.sub(str, i, i)
        local tex = bgtx[txstr .. digit]
        
        if tex then
            local w = tex.Width
            if i == 1 then
            else
                -- overlap offset (50%)
                cursorX = cursorX + (prevWidth * 0.5)
            end
			tex:SetColor(color)
            tex:DrawAtAnchor(cursorX + (w / 2), y, "center")
            prevWidth = w
        end
    end
end

local function drawLevelTag(songNode, x, y)
	local chart = getSongNodeFocusChart(songNode)
	if chart == nil then
		return nil
	end
	local level = chart.Level
	local difficulty = chart.DifficultyAsInt
	local labelH = bars["levellabels"].Height/5
	local labelW = bars["levellabels"].Width
	if difficulty < 3 or level <= 10 then
		bars["levellabels"]:DrawRectAtAnchor(x, y, 0, labelH*difficulty, labelW, labelH, "center")
	elseif difficulty == 3 then
		bars["levellabelsfire"]:DrawRectAtAnchor(x, y, 0, labelH*levelLabelFrame, labelW, labelH, "center")
		drawNumberCentered(level, "levellabelsborder", x, y, COLOR:CreateColorFromHex("ffac0c0c"))
	else
		bars["levellabelsstorm"]:DrawRectAtAnchor(x, y, 0, labelH*levelLabelFrame, labelW, labelH, "center")
		drawNumberCentered(level, "levellabelsborder", x, y, COLOR:CreateColorFromHex("ff83159e"))
	end
	drawNumberCentered(level, "levellabels", x, y)
	if chart.IsPlus == true then
		bars["levellabelsplus"]:DrawRectAtAnchor(x, y, 0, labelH*difficulty, labelW, labelH, "center")
	end
end

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

	-- Reset the Extra fade counter
	if ctx["extreme_fade"] then
		ctx["extreme_fade"]:Start()
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

	local ssn = songList:GetSelectedSongNode()

	-- Song info
	if ssn ~= nil and ssn.IsSong then
		bgtx["songinfo"]:DrawAtAnchor(1920,0,"topright")
		if ssn.HasVideo then
			bgtx["sinfo_video"]:Draw(SONGINFO_HASVIDEO_ORIGIN_X,SONGINFO_HASVIDEO_ORIGIN_Y)
		end
		if ssn.Explicit then
			bgtx["sinfo_explicit"]:DrawAtAnchor(SONGINFO_EXPLICIT_ORIGIN_X,SONGINFO_EXPLICIT_ORIGIN_Y,"topright")
		end
		for i = 0, 4, 1 do
			local chart = ssn:GetChart(i)
			local xpos = SONGINFO_DIFFICULTIES_ORIGIN_X
			local ypos = SONGINFO_DIFFICULTIES_ORIGIN_Y + SONGINFO_DIFFICULTIES_GAP_Y*math.min(i, 3)
			if ssn:GetChart(3) ~= nil and i == 4 then
				if chart ~= nil then
					bgtx["sinfo_difficulties_4"]:SetOpacity(difficultyFade4/255)
					bgtx["sinfo_difficulties_4"]:Draw(xpos,ypos)
					bgtx["sinfo_difficulties_4"]:SetOpacity(1)
				end
			elseif chart == nil then
				if ssn:GetChart(4) == nil or i ~= 3 then
					bgtx["sinfo_difficulties_missing"]:Draw(xpos,ypos)
				end
			else
				bgtx["sinfo_difficulties_"..i]:Draw(xpos,ypos)
			end
		end
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
					-- TODO: Don't show if locked AND grayed/blurred
					drawLevelTag(currentPage[i],xpos+SONGBAR_LABEL_X_OFFSET,ypos)
				elseif currentPage[i].IsRandom then
					bars["random"]:DrawAtAnchor(xpos,ypos,"center")
				elseif currentPage[i].IsReturn then
					bars["back"]:DrawAtAnchor(xpos,ypos,"center")
				end
				tx:DrawAtAnchor(xpos+SONGLIST_TEXT_OFFSET_X, ypos+SONGLIST_TEXT_OFFSET_Y,"center")
			end
		end
		local x0 = SONGLIST_ORIGIN_X+SONGLIST_SELECTED_X_DIFF
		local y0 = SONGLIST_ORIGIN_Y
		bars["selected"]:DrawAtAnchor(x0,y0,"center")
		local xlshift = arrowsDistance * math.cos(7*math.pi/12)
		local ylshift = arrowsDistance * math.sin(7*math.pi/12)
		bars["selected-arrow-l"]:DrawAtAnchor(x0-SONGLIST_SELECTED_ARROW_GAP/2+xlshift,y0-ylshift,"left")
		bars["selected-arrow-r"]:DrawAtAnchor(x0+SONGLIST_SELECTED_ARROW_GAP/2-xlshift,y0+ylshift,"right")
	end

	-- Folder Path
	bgtx["header"]:Draw(0, 0)
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

	-- Nameplates space
	local playerCount = CONFIG.PlayerCount
	for i = 1, playerCount, 1 do
		local xpos = NAMEPLATE_BOX_START_X + (i - 1) * NAMEPLATE_BOX_SPACING_X
		local ypos = 1080 - NAMEPLATE_BOX_FOLDED_SIZE_Y
		bgtx["nameplate_info"]:Draw(xpos, ypos)
		NAMEPLATE:DrawPlayerNameplate(xpos+NAMEPLATE_OFFSET_X, ypos+NAMEPLATE_OFFSET_Y, 255, i - 1)
	end

end

function update()
	for k, counter in pairs(ctx) do
        counter:Tick()
    end

	if INPUT:KeyboardPressed("S") then
		sounds.Skip:Play()
		CONFIG:SetDefaultCourse(0, (CONFIG:GetDefaultCourse(0) + 1) % 5)
		-- return Exit("stage", "demo1")
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
	textMenuState = textLarge:GetText("Select a song!")
	-- textTex = textSmall:GetText("You've played " .. tostring(test.TotalPlaycount) .. " charts.\nHow many more will you play?")

	sounds.Skip = SHARED:GetSharedSound("Skip")
	sounds.Cancel = SHARED:GetSharedSound("Cancel")
	sounds.Decide = SHARED:GetSharedSound("Decide")
	sounds.SongDecide = SHARED:GetSharedSound("SongDecide")

	-- Background scroll counter
	ctx["background"] = COUNTER:CreateCounter(1920, 0, 1 / 48)
	ctx["background"]:SetLoop(true)
	ctx["background"]:Listen(function (val)
		backgroundScrollX = val
	end)
	ctx["background"]:Start()

	-- Extra icon fade counter
	ctx["extreme_fade"] = COUNTER:CreateCounter(2000, 0, 1 / 400)
	ctx["extreme_fade"]:SetLoop(true)
	ctx["extreme_fade"]:Listen(function (val)
		if val >= 1000 and val <= 1745 then
			difficultyFade4 = 255
		elseif val > 745 and val < 1000 then
			difficultyFade4 = math.max(0, math.min(255, val - 745))
		elseif val > 1745 and val < 2000 then
			difficultyFade4 = math.max(0, math.min(255, 2000 - val))
		else
			difficultyFade4 = 0
		end
	end)
	ctx["extreme_fade"]:Start()

	-- Select Box Scale Animation (1.0 to 1.02)
	ctx["selectbox_animation"] = COUNTER:CreateCounter(2000, 0, 1 / 600)
	ctx["selectbox_animation"]:SetLoop(true)
	ctx["selectbox_animation"]:Listen(function(val)
		if bars["selected"] then
			local n = 1.01 + (math.sin(val * (math.pi * 2 / 2000)) * 0.01)
			bars["selected"]:SetScale(n, n)
		end
	end)
	ctx["selectbox_animation"]:Start()

	-- Arrows animation
	ctx["arrows_animation"] = COUNTER:CreateCounter(10, 0, 1 / 10)
	ctx["arrows_animation"]:SetBounce(true)
	ctx["arrows_animation"]:Listen(function(val)
		arrowsDistance = val
	end)
	ctx["arrows_animation"]:Start()

	-- Special leveltag animation
	ctx["leveltag_animation"] = COUNTER:CreateCounter(SONGBAR_LEVEL_EX_ANIMATION_FRAMECOUNT, 0, 1 / SONGBAR_LEVEL_EX_ANIMATION_FRAMECOUNT*2)
	ctx["leveltag_animation"]:SetLoop(true)
	ctx["leveltag_animation"]:Listen(function(val)
		levelLabelFrame = math.floor(val)
	end)
	ctx["leveltag_animation"]:Start()
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
	
	-- General textures
	SHARED:SetSharedTexture("background", "Textures/bg0.png")
	bgtx["overlay"] = TEXTURE:CreateTexture("Textures/bg_overlay.png")
	bgtx["songinfo"] = TEXTURE:CreateTexture("Textures/bg_songinfo.png")
	bgtx["header"] = TEXTURE:CreateTexture("Textures/bg_header.png")
	bgtx["header-box"] = TEXTURE:CreateTexture("Textures/bg_header-box.png")
	bgtx["header-arrow"] = TEXTURE:CreateTexture("Textures/bg_header-arrow.png")
	bgtx["nameplate_info"] = TEXTURE:CreateTexture("Textures/nameplate_info.png")
	bgtx["sinfo_video"] = TEXTURE:CreateTexture("Textures/sinfo_video.png")
	bgtx["sinfo_explicit"] = TEXTURE:CreateTexture("Textures/sinfo_explicit.png")
	bgtx["sinfo_difficulties_missing"] = TEXTURE:CreateTexture("Textures/sinfo_difficulties_missing.png")
	for i = 0, 4, 1 do
		bgtx["sinfo_difficulties_"..i] = TEXTURE:CreateTexture("Textures/sinfo_difficulties_"..i..".png")
	end
	for i = 0, 9, 1 do
		bgtx["levellabels"..i] = TEXTURE:CreateTexture("Textures/BarLevel/"..i..".png")
	end
	for i = 0, 9, 1 do
		bgtx["levellabelsborder"..i] = TEXTURE:CreateTexture("Textures/BarLevelBorder/"..i..".png")
	end

	-- Song list textures
	bars["bar"] = TEXTURE:CreateTexture("Textures/bar.png")
	bars["random"] = TEXTURE:CreateTexture("Textures/random.png")
	bars["back"] = TEXTURE:CreateTexture("Textures/back.png")
	bars["locked"] = TEXTURE:CreateTexture("Textures/locked.png")
	bars["selected"] = TEXTURE:CreateTexture("Textures/selected.png")
	bars["selected-arrow-l"] = TEXTURE:CreateTexture("Textures/selected-arrow-l.png")
	bars["selected-arrow-r"] = TEXTURE:CreateTexture("Textures/selected-arrow-r.png")
	bars["levellabels"] = TEXTURE:CreateTexture("Textures/bar_levelbg.png")
	bars["levellabelsplus"] = TEXTURE:CreateTexture("Textures/bar_levelbgplus.png")
	bars["levellabelsfire"] = TEXTURE:CreateTexture("Textures/bar_levelbgfire.png")
	bars["levellabelsstorm"] = TEXTURE:CreateTexture("Textures/bar_levelbgstorm.png")

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
	for _, bg in pairs(bgtx) do
		bg:Dispose()
	end
	for _, overlay in pairs(genre_overlays) do
		overlay:Dispose()
	end
end
