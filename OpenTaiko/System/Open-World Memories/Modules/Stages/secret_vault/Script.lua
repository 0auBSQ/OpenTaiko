local text = nil
local textTex = nil

local save = nil

local sounds = {}

local songList = nil
local currentPage = {}
local pageTexts = {}

local textures = {}
local TxTextChar = {}

local active = false

local state = "select"

local stateUnlockVal = 0

local function drawNumber(x, y, str)
	local fontSize = 16
	local xInit = x
	for i = 1, #str do
		local char = str:sub(i, i)
		if char == "\n" then
			y = y + fontSize
			x = xInit
		else
			if TxTextChar[char] ~= nil then
				TxTextChar[char]:Draw(x, y)
			end
			x = x + fontSize
		end
	end
end

local function getChart(song)
	return song:GetChart(4)
end

local function isChartUnlocked(song)
	if song == nil then
		return false
	end
	return save:GetGlobalTrigger(".vault_song_unlocked_"..song.UniqueId)
end

local function reloadPreimage(songNode)
	if songNode.IsSong == true and isChartUnlocked(songNode) then 
		SHARED:SetSharedTextureUsingAbsolutePath("preimage_vault", songNode.PreimagePath)
	else
		SHARED:SetSharedTextureUsingAbsolutePath("preimage_vault", '')
	end
end

local function drawPreimage()
	local tex = SHARED:GetSharedTexture("preimage_vault")
	if tex.Height > 0 and tex.Width > 0 then
		local sH = 400 / tex.Height
		local sW = 400 / tex.Width
		tex:SetScale(sW, sH)
		tex:Draw(646, 400)
	end
end

local function playPreview(songNode)
	local psnd = SHARED:GetSharedSound("presound_vault")
	psnd:Stop()
	if songNode.IsSong == true and isChartUnlocked(songNode) then
		SHARED:SetSharedPreviewUsingAbsolutePath("presound_vault", songNode.AudioPath, function (snd)
			snd:Play()
			snd:SetTimestamp(songNode.DemoStart)
		end)
	end
end

local function refreshPageNoPreview()
	currentPage = {}
	pageTexts = {}

	for i = -5,5 do
		local node = songList:GetSongNodeAtOffset(i)
		currentPage[i] = node
		if node == nil then pageTexts[i] = nil
		elseif i == 0 then
			pageTexts[i] = text:GetText(node.Title, false, 99999, COLOR:CreateColorFromARGB(255,242,207,1))
		else
			pageTexts[i] = text:GetText(node.Title)
		end
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
		else
			pageTexts[i] = text:GetText(node.Title)
		end

		-- Assets reload for selected songs
		if i == 0 and node ~= nil then
			reloadPreimage(node)
			-- playPreview(node)
		end
	end
end

local function getKeyCount()
	if save:GetGlobalTrigger(".vault_free_key_obtained") == false then
		save:SetGlobalCounter(".vault_key_count_simple", save:GetGlobalCounter(".vault_key_count_simple") + 1)
		save:SetGlobalTrigger(".vault_free_key_obtained", true)
	end
	
	return {
		save:GetGlobalCounter(".vault_key_count_simple"),
		save:GetGlobalCounter(".vault_key_count_gold"),
		save:GetGlobalCounter(".vault_key_count_optk")
	}
end

local function getKeyPrice(song)
    local chart = getChart(song)
    local level = chart.Level

    if level > 2 then
        return {0, 0, 1}
    elseif level > 1 then
        return {0, 1, 0}
    end
    return {1, 0, 0}
end

local function canBuy(song)
    local keyPrice = getKeyPrice(song)
    local keyCount = getKeyCount()

    for i, price in ipairs(keyPrice) do
        if keyCount[i] < price then
            return false
        end
    end
    return true
end

local function buySong(song)
    local keyPrice = getKeyPrice(song)
    local keyCount = getKeyCount()

    for i, price in ipairs(keyPrice) do
        if keyCount[i] < price then
            return false
        end
    end

    for i, price in ipairs(keyPrice) do
        keyCount[i] = keyCount[i] - price
    end
	
	save:SetGlobalCounter(".vault_key_count_simple", keyCount[1])
	save:SetGlobalCounter(".vault_key_count_gold", keyCount[2])
	save:SetGlobalCounter(".vault_key_count_optk", keyCount[3])
	
	save:SetGlobalTrigger(".vault_song_unlocked_"..song.UniqueId, true)

    return true
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
		if isChartUnlocked(ssn) then
			local success = ssn:Mount(4)
			if success == true then
				sounds.SongDecide:Play()
				return true -- transition to song select if true
			end 
		else
			-- If locked song, toggle a prompt to unlock it (if possible)
			if canBuy(ssn) then
				state = "unlock"
				stateUnlockVal = 0
				sounds.Decide:Play()
			else
				sounds.NoKey:Play()
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
	textures["VaultBg"]:Draw(0,0)
	textures["Overlay"]:Draw(0,0)

	drawPreimage()

	if textTex ~= nil then
		textTex:Draw(400,400)
	end
	if pageTexts ~= nil then
		for i, tx in pairs(pageTexts) do
			-- can be nil if no modulo pagination
			if tx ~= nil then 
				if currentPage[i].IsSong then
					local chart = getChart(currentPage[i])
					local level = chart.Level
					local xOff = 0
					if level > 2 then
						xOff = 700
					elseif level > 1 then
						xOff = 350
					end
					
					-- textures["Chests"]:DrawRectAtAnchor(25, 675+i*100, xOff, 0, 350, 350, "bottom")
					
					if isChartUnlocked(currentPage[i]) then
						textures["ChestsOpen"]:DrawRectAtAnchor(960+i*376, 400, xOff, 0, 350, 450, "bottom")
						
						if i == 0 then
							textures["DifficultyBar"]:DrawRect(1100, 630, 100, 0, 700, 100)
							if chart.IsPlus and level < 10 then
								textures["DifficultyBarSteel"]:DrawRect(1100+70*math.min(10, level), 630, 100+70*math.min(10, level), 0, 70, 100)
							end
							textures["DifficultyBarRuby"]:DrawRect(1100, 630, 100, 0, 70*math.min(10, level), 100)
							
							textures["DifficultyText"]:DrawRect(1100, 730, 0, 100*math.min(5,level), 700, 100)
							
							local _title = text:GetText(currentPage[i].Title)
							local _subtitle = text:GetText(currentPage[i].Subtitle)
							local _charter = text:GetText("Charter: "..chart.NotesDesigner)
							
							_title:DrawAtAnchor(1450, 400, "top")
							_subtitle:DrawAtAnchor(1450, 430, "top")
							_charter:DrawAtAnchor(1450, 460, "top")
						end
						
						
						-- 1100, 400
					else
						textures["Chests"]:DrawRectAtAnchor(960+i*376, 400, xOff, 0, 350, 450, "bottom")
						
						-- Locked tab (only for current)
						if i == 0 then
							if state == "unlock" then
								textures["UnlockConfirm"]:Draw(0,0)
								textures["UnlockConfirmSelect"]:Draw(945 + stateUnlockVal*375, 811)
							else
								textures["Locked"]:Draw(629, 380)
							end
							
							-- Get the starting X depending on the number of key types to display
							local keyPrice = getKeyPrice(currentPage[i])
							local nonZero = 0
							for _, price in ipairs(keyPrice) do
								if price ~= 0 then
									nonZero = nonZero + 1
								end
							end
							-- Display key price (can be composite, though not used yet)
							local startingX = 796+(3-nonZero)*150
							textures["VaultKeys"]:SetScale(1/3, 1/3)
							for i, price in ipairs(keyPrice) do
								if price > 0 then
									textures["VaultKeys"]:DrawRect(startingX, 608, (i-1)*600, 0, 600, 600)
									drawNumber(startingX+200, 694, ("%d"):format(price))
									startingX = startingX+300
								end
							end
						end
						
					end
					
					
					
					-- bars["bar"]:SetColor(currentPage[i].BoxColor)
					-- bars["bar"]:DrawAtAnchor(400,500+i*100,"Center")
				end
				-- tx:DrawAtAnchor(400, 500+i*100,"Center")
			end
		end
	end
	
	local keys = getKeyCount()
	drawNumber(1100, 950, ("%d"):format(keys[1]))
	drawNumber(1400, 950, ("%d"):format(keys[2]))
	drawNumber(1700, 950, ("%d"):format(keys[3]))

	NAMEPLATE:DrawPlayerNameplate(20, 980, 255, 0)

end

function update()
	if state == "select" then
	
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
		
	elseif state == "unlock" then
	
		if (INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") or INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow")) and songList ~= nil then
			stateUnlockVal = 1 - stateUnlockVal
			sounds.Skip:Play()
			
		end
		-- refreshPage()
		
		if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
			if stateUnlockVal == 0 then 
				buySong(songList:GetSelectedSongNode())
				sounds.Unlock:Play()
				refreshPage()
			else
				sounds.Decide:Play()
			end
			state = "select"
		end
	
		if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
			state = "select"
			sounds.Cancel:Play()
		end
	
	end

end

function activate()
	save = GetSaveFile(0)
	-- textTex = text:GetText("You played " .. tostring(test.TotalPlaycount) .. " charts...")
	
	local txNm = { 
		"Chests",
		"ChestsOpen",
		"VaultBg",
		"VaultKeys",
		"GateBg",
		"GateKeys",
		"Overlay",
		"Locked",
		"UnlockConfirm",
		"UnlockConfirmSelect",
		"DifficultyBar",
		"DifficultyBarSteel",
		"DifficultyBarRuby",
		"DifficultyText"
	}
	for _, v in pairs(txNm) do
		textures[v] = TEXTURE:CreateTexture("Textures/"..v..".png")
	end
	
	local charMap = "+-0123456789.(), "
	TxTextChar = {}
	for i = 1, #charMap do
		local char = charMap:sub(i, i)
		TxTextChar[char] = text:GetText(char)
	end
	
	refreshPage()
	
	sounds.BGM:SetLoop(true)
	sounds.BGM:Play()
	
	active = true
end

function deactivate()
	for _, v in pairs(textures) do
		v:Dispose()
	end
	
	-- for k, v in pairs(TxTextChar) do
	-- 	v:Dispose()
	-- end
	-- TxTextChar = {}

	-- local psnd = SHARED:GetSharedSound("presound_vault")
	-- psnd:Stop()
	
	sounds.BGM:Stop()
	
	active = false
end

function onStart()
	text = TEXT:Create(16)

	sounds.Skip = SOUND:CreateSFX("Sounds/Skip.ogg")
	sounds.Cancel = SOUND:CreateSFX("Sounds/Cancel.ogg")
	sounds.Decide = SOUND:CreateSFX("Sounds/Decide.ogg")
	sounds.SongDecide = SOUND:CreateSFX("Sounds/SongDecide.ogg")
	sounds.NoKey = SOUND:CreateSFX("Sounds/NoKey.ogg")
	sounds.Unlock = SOUND:CreateSFX("Sounds/Unlock.ogg")
	sounds.BGM = SOUND:CreateBGM("Sounds/BGM.ogg")
end 

function afterSongEnum()
	local lsls = GenerateSongListSettings()
	-- Test options here
	lsls.ModuloPagination = false
	lsls.AppendMainRandomBox = false
	lsls.AppendSubRandomBoxes = false
	lsls.SubBackBoxFrequency = 0
	lsls.RootGenreFolder = "Secret Vault"

	-- Get song list 
	songList = RequestSongList(lsls)
	
	if active == false then
		refreshPageNoPreview()
	else
		refreshPage()
	end
end

function onDestroy()
	if text ~= nil then
		text:Dispose()
	end
	for _, sound in pairs(sounds) do
		sound:Dispose()
	end
end
