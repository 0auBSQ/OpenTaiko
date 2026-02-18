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

local songSelectShift = 0
local songSelectElemOpacity = 255

local levelLabelFrame = 0
local difficultyFade4 = 0

local arrowsDistance = 0

-- Screens/Statuses

--- songselect | pretransition | transition | difficultyselect
local activeScreen = "songselect"
local songSelectModes = { songselect = true, pretransition = true, transition = true }
local difficultySelectModes = { difficultyselect = true, transition = true }

--- none | inputinfo | playerinfo | quicksettings | unlockconfirm | unlockanim | modselect
local activeModal = "none"

-- To-play song info 
local selectedSongNode = nil

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
local SONGINFO_SUBTITLE_ORIGIN_X = 1536
local SONGINFO_SUBTITLE_ORIGIN_Y = 689
local SONGINFO_SUBTITLE_MWIDTH = 530
local SONGINFO_BPM_ORIGIN_X = 1780
local SONGINFO_BPM_ORIGIN_Y = 877
local SONGINFO_BPM_MWIDTH = 240
local SONGINFO_CHARTER_ORIGIN_X = 1216
local SONGINFO_CHARTER_ORIGIN_Y = 750
local SONGINFO_CHARTER_MWIDTH = 512
local SONGINFO_LENGTH_ORIGIN_X = 1216
local SONGINFO_LENGTH_ORIGIN_Y = 806
local SONGINFO_LENGTH_MWIDTH = 420

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

-- Add counter helper
local function startCounter(key, startVal, endVal, interval, mode, updateCallback, onFinish)
    local c = COUNTER:CreateCounter(startVal, endVal, interval, onFinish)
    if mode == "loop" then c:SetLoop(true)
    elseif mode == "bounce" then c:SetBounce(true) end
    if updateCallback then c:Listen(updateCallback) end
    c:Start()
    ctx[key] = c
    return c
end

-- BPM number helper (up to 3 digits for decimal)
local function formatNumber(n, decimals)
    local s = string.format("%."..decimals.."f", n)
    -- remove trailing zeros
    s = s:gsub("0+$", "")
    -- remove trailing dot if needed
    s = s:gsub("%.$", "")
    return s
end

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
    
    local cursorX = 0
    local prevWidth = 0
    local lastWidth = 0
    
    for i = 1, #str do
        local digit = string.sub(str, i, i)
        local tex = bgtx[txstr .. digit]
        
        if tex then
            local w = tex.Width
            
            if i > 1 then
                cursorX = cursorX + (prevWidth * 0.5)
            end
            
            prevWidth = w
            lastWidth = w
        end
    end
    
    return cursorX + lastWidth
end

local function drawNumberCentered(nb, txstr, x, y, color, opacity)
	local white = COLOR:CreateColorFromHex("ffffffff")
	color = color or white
	opacity = opacity or 1

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
			tex:SetOpacity(opacity)
            tex:DrawAtAnchor(cursorX + (w / 2), y, "center")
			tex:SetOpacity(1)
			tex:SetColor(white)
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
		tex:Draw(PREIMAGE_ORIGIN_X-songSelectShift, PREIMAGE_ORIGIN_Y)
	end
end

local function playPreview(songNode)
	--local psnd = SHARED:GetSharedSound("presound")
	SHARED:SetSharedPreview("presound", "Sounds/empty.ogg")
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

local function handleDecideSongSelect()
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
		sounds.SongDecide:Play()
		return ssn
	elseif ssn.IsRandom == true then
		local rdNd = songList:GetRandomNodeInFolder(ssn)
		if rdNd ~= nil then
			sounds.SongDecide:Play()
			return rdNd
		end
	end
	-- any route note ending up to play a song return no selected node
	return nil
end

local function handleFolderClose()
	if songList == nil then return Exit("title", nil) end
	-- if no folder to close, trigger exit scene instead
	return songList:CloseFolder()
end


function draw()
	-- Background draw 
	SHARED:GetSharedTexture("background"):Draw(-backgroundScrollX,0)
	SHARED:GetSharedTexture("background"):Draw(-backgroundScrollX+1920,0)

	local ssn = songList:GetSelectedSongNode()

	if difficultySelectModes[activeScreen] then
		bgtx["difficultyselect"]:Draw(1920-songSelectShift, 0)

	end

	-- Not an elseif, both display during the transition
	if songSelectModes[activeScreen] then

		-- Song info
		if ssn ~= nil and ssn.IsSong then
			bgtx["songinfo"]:DrawAtAnchor(1920-songSelectShift,0,"topright")
			-- Side tags (Left)
			if ssn.HasVideo then
				bgtx["sinfo_video"]:Draw(SONGINFO_HASVIDEO_ORIGIN_X-songSelectShift,SONGINFO_HASVIDEO_ORIGIN_Y)
			end
			if ssn.Explicit then
				bgtx["sinfo_explicit"]:DrawAtAnchor(SONGINFO_EXPLICIT_ORIGIN_X-songSelectShift,SONGINFO_EXPLICIT_ORIGIN_Y,"topright")
			end
			-- Difficulties (Right)
			for i = 0, 4, 1 do
				local chart = ssn:GetChart(i)
				local xpos = SONGINFO_DIFFICULTIES_ORIGIN_X-songSelectShift
				local ypos = SONGINFO_DIFFICULTIES_ORIGIN_Y + SONGINFO_DIFFICULTIES_GAP_Y*math.min(i, 3)
				if ssn:GetChart(3) ~= nil and i == 4 then
					if chart ~= nil then
						bgtx["sinfo_difficulties_4"]:SetOpacity(difficultyFade4/255)
						bgtx["sinfo_difficulties_4"]:Draw(xpos,ypos)
						bgtx["sinfo_difficulties_4"]:SetOpacity(1)
						drawNumberCentered(chart.Level, "sinfo_level", xpos+bgtx["sinfo_difficulties_4"].Width/2, ypos+bgtx["sinfo_difficulties_4"].Height/2,nil,difficultyFade4/255)
						if chart.IsPlus then
							bgtx["sinfo_difficulties_"..i.."_plus"]:SetOpacity(difficultyFade4/255)
							bgtx["sinfo_difficulties_"..i.."_plus"]:Draw(xpos,ypos)
							bgtx["sinfo_difficulties_"..i.."_plus"]:SetOpacity(1)
						end
					end
				elseif chart == nil then
					if ssn:GetChart(4) == nil or i ~= 3 then
						bgtx["sinfo_difficulties_missing"]:Draw(xpos,ypos)
					end
				else
					bgtx["sinfo_difficulties_"..i]:Draw(xpos,ypos)
					drawNumberCentered(chart.Level, "sinfo_level", xpos+bgtx["sinfo_difficulties_"..i].Width/2, ypos+bgtx["sinfo_difficulties_"..i].Height/2)
					if chart.IsPlus then
						bgtx["sinfo_difficulties_"..i.."_plus"]:Draw(xpos,ypos)
					end
				end
			end
			-- Metadata (Down)
			local focusedChart = getSongNodeFocusChart(ssn)
			local subtitleTx = textSmall:GetText(ssn.Subtitle, false, SONGINFO_SUBTITLE_MWIDTH)
			local charterTx = textSmall:GetText("Chart - "..ssn.Maker, false, SONGINFO_CHARTER_MWIDTH)
			local lengthTx = textSmall:GetText("Length - 2:00 (tmp)", false, SONGINFO_LENGTH_MWIDTH)
			subtitleTx:DrawAtAnchor(SONGINFO_SUBTITLE_ORIGIN_X-songSelectShift, SONGINFO_SUBTITLE_ORIGIN_Y, "center")
			charterTx:Draw(SONGINFO_CHARTER_ORIGIN_X-songSelectShift, SONGINFO_CHARTER_ORIGIN_Y)
			lengthTx:Draw(SONGINFO_LENGTH_ORIGIN_X-songSelectShift, SONGINFO_LENGTH_ORIGIN_Y)
			if focusedChart ~= nil then
				local mult = CONFIG.SongSpeed / 20
				local bpmText = formatNumber(focusedChart.BaseBPM*mult,3)
				if focusedChart.BaseBPM ~= focusedChart.MinBPM or focusedChart.BaseBPM ~= focusedChart.MaxBPM then
					bpmText = bpmText.." ("..formatNumber(focusedChart.MinBPM*mult,3).."-"..formatNumber(focusedChart.MaxBPM*mult,3)..")"
				end
				local color = "FFFFFFFF"
				if mult < 1 then
					color = "ff95ccff"
				elseif mult > 1 then
					color = "ffff9ec3"
				end
				local bpmTx = text:GetText(bpmText, false, SONGINFO_BPM_MWIDTH, COLOR:CreateColorFromHex(color))
				bpmTx:DrawAtAnchor(SONGINFO_BPM_ORIGIN_X-songSelectShift, SONGINFO_BPM_ORIGIN_Y, "center")
			end
		end
		
		drawPreimage()

		-- Song List
		if pageTexts ~= nil then
			for i, tx in pairs(pageTexts) do
				local xpos = SONGLIST_ORIGIN_X+i*SONGLIST_OFFSET_X-songSelectShift
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
			local x0 = SONGLIST_ORIGIN_X+SONGLIST_SELECTED_X_DIFF-songSelectShift
			local y0 = SONGLIST_ORIGIN_Y
			bars["selected"]:DrawAtAnchor(x0,y0,"center")
			local xlshift = arrowsDistance * math.cos(7*math.pi/12)
			local ylshift = arrowsDistance * math.sin(7*math.pi/12)
			bars["selected-arrow-l"]:DrawAtAnchor(x0-SONGLIST_SELECTED_ARROW_GAP/2+xlshift,y0-ylshift,"left")
			bars["selected-arrow-r"]:DrawAtAnchor(x0+SONGLIST_SELECTED_ARROW_GAP/2-xlshift,y0+ylshift,"right")
		end

		-- Folder Path
		local opacityNorm = songSelectElemOpacity/255
		bgtx["header"]:SetOpacity(opacityNorm)
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
				bgtx["header-box"]:SetOpacity(opacityNorm)
				bgtx["header-box"]:DrawAtAnchor(xpos, 0, "topright")
				title:SetOpacity(opacityNorm)
				title:DrawAtAnchor(xpos-bgtx["header-box"].Width+HEADER_BOX_TEXT_OFFSET_X, HEADER_BOX_TEXT_OFFSET_Y+bgtx["header-box"].Height/2, "center")
				bgtx["header-arrow"]:SetOpacity(opacityNorm)
				if i ~= #pathStack then
					bgtx["header-arrow"]:DrawAtAnchor(xpos-bgtx["header-box"].Width, 0, "topright")
				end
				xpos = xpos - bgtx["header-box"].Width - bgtx["header-arrow"].Width
			end
		end

		-- if favoriteicon ~= nil then
		-- 	favoriteicon:Draw(1200, 400)
		-- end
		bgtx["overlay"]:SetOpacity(opacityNorm)
		bgtx["overlay"]:Draw(0, 0)

		if textMenuState ~= nil then
			textMenuState:SetOpacity(opacityNorm)
			textMenuState:DrawAtAnchor(270,65,"Center")
		end

		-- Nameplates space
		local playerCount = CONFIG.PlayerCount
		bgtx["nameplate_info"]:SetOpacity(opacityNorm)
		for i = 1, playerCount, 1 do
			local xpos = NAMEPLATE_BOX_START_X + (i - 1) * NAMEPLATE_BOX_SPACING_X
			local ypos = 1080 - NAMEPLATE_BOX_FOLDED_SIZE_Y
			bgtx["nameplate_info"]:Draw(xpos, ypos)
			NAMEPLATE:DrawPlayerNameplate(xpos+NAMEPLATE_OFFSET_X, ypos+NAMEPLATE_OFFSET_Y, songSelectElemOpacity, i - 1)
		end
	end

end

local function updateTransitionVisuals(val)
    songSelectShift = val
    
    -- Fade from 255 (at 0) to 0 (at 960)
    -- Formula: 255 - (current_val * ratio)
    local opacity = 255 - (val * (255 / 960))
    songSelectElemOpacity = math.max(0, math.min(255, opacity))
end

function update()
	for k, counter in pairs(ctx) do
        counter:Tick()
    end

	if activeScreen == "songselect" then
		selectedSongNode = nil

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
		elseif (INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow")) and songList ~= nil then
			sounds.Skip:Play()
			songList:Move(-1)
			refreshPage()
		elseif INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
			-- Select song and move to difficulty select if applicable
			selectedSongNode = handleDecideSongSelect()
			-- if isPlayStarted == true then
			--	return Exit("play", nil)
			-- end 
		elseif INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
			-- Close folder 
			local closeFolder = handleFolderClose()
			if closeFolder == true then
				refreshPage()
				sounds.Decide:Play()
			else
				sounds.Cancel:Play()
				return Exit("title", nil)
			end
		end

		-- Transition to difficulty select if a screen was selected
		if selectedSongNode ~= nil then
			-- TODO? implement pretransition for songs having one
			activeScreen = "transition"
			startCounter("screen_transition", 0, 1920, 0.5/1920, "none", updateTransitionVisuals, function() 
				activeScreen = "difficultyselect" 
			end)
		end

	elseif activeScreen == "difficultyselect" then

		-- Placeholder quit method
		if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
			sounds.Decide:Play()
			activeScreen = "transition"
			startCounter("screen_transition", 1920, 0, -0.5/1920, "none", updateTransitionVisuals, function() 
            	activeScreen = "songselect" 
        	end)
		end

	end

	-- Test search song
	--[[
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
	]]

	if INPUT:KeyboardPressed("Q") then
		sounds.Skip:Play()
		CONFIG.SongSpeed = CONFIG.SongSpeed - 1
	end

	if INPUT:KeyboardPressed("W") then
		sounds.Skip:Play()
		CONFIG.SongSpeed = CONFIG.SongSpeed + 1
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

	-- Background Scroll
    startCounter("background", 1920, 0, 1/48, "loop", function(val) 
        backgroundScrollX = val 
    end)

    -- Extreme Icon Fade
    startCounter("extreme_fade", 2000, 0, 1/400, "loop", function(val)
        local fadeIn = val - 745
    	local fadeOut = 2000 - val
    	difficultyFade4 = math.max(0, math.min(255, fadeIn, fadeOut))
    end)

    -- Select Box Animation
    startCounter("selectbox_animation", 2000, 0, 1/600, "loop", function(val)
        if bars["selected"] then
            local n = 1.01 + (math.sin(val * (math.pi * 2 / 2000)) * 0.01)
            bars["selected"]:SetScale(n, n)
        end
    end)

    -- Arrows Bounce
    startCounter("arrows_animation", 10, 0, 1/10, "bounce", function(val)
        arrowsDistance = val
    end)

    -- Level Tag Animation
    startCounter("leveltag_animation", SONGBAR_LEVEL_EX_ANIMATION_FRAMECOUNT, 0, 1/SONGBAR_LEVEL_EX_ANIMATION_FRAMECOUNT*2, "loop", function(val)
        levelLabelFrame = math.floor(val)
    end)
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
	bgtx["difficultyselect"] = TEXTURE:CreateTexture("Textures/bg_difficultyselect.png")
	bgtx["header"] = TEXTURE:CreateTexture("Textures/bg_header.png")
	bgtx["header-box"] = TEXTURE:CreateTexture("Textures/bg_header-box.png")
	bgtx["header-arrow"] = TEXTURE:CreateTexture("Textures/bg_header-arrow.png")
	bgtx["nameplate_info"] = TEXTURE:CreateTexture("Textures/nameplate_info.png")
	bgtx["sinfo_video"] = TEXTURE:CreateTexture("Textures/sinfo_video.png")
	bgtx["sinfo_explicit"] = TEXTURE:CreateTexture("Textures/sinfo_explicit.png")
	bgtx["sinfo_difficulties_missing"] = TEXTURE:CreateTexture("Textures/sinfo_difficulties_missing.png")
	for i = 0, 4, 1 do
		bgtx["sinfo_difficulties_"..i] = TEXTURE:CreateTexture("Textures/sinfo_difficulties_"..i..".png")
		-- Only the 0 for now as they're all monocolor, replace by the new one once new diff textures ready
		bgtx["sinfo_difficulties_"..i.."_plus"] = TEXTURE:CreateTexture("Textures/sinfo_difficulties_0_plus.png")
	end
	for i = 0, 9, 1 do
		bgtx["levellabelsborder"..i] = TEXTURE:CreateTexture("Textures/BarLevelBorder/"..i..".png")
		bgtx["levellabels"..i] = TEXTURE:CreateTexture("Textures/BarLevel/"..i..".png")
		bgtx["sinfo_level"..i] = TEXTURE:CreateTexture("Textures/SinfoLevel/"..i..".png")
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
