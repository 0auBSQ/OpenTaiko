local text = nil
local textTex = nil

local sounds = {}

local songList = nil
local currentPage = {}
local pageTexts = {}


local function refreshPage()
	currentPage = {}
	pageTexts = {}

	for i = -5,5 do
		local node = songList:GetSongNodeAtOffset(i)
		currentPage[i] = node
		if node == nil then pageTexts[i] = nil
		elseif i == 0 then pageTexts[i] = text:GetText(node.Title, false, 99999, COLOR:CreateColorFromARGB(255,242,207,1))
		else pageTexts[i] = text:GetText(node.Title)
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
		local success = ssn:Mount(5) -- for testing, go directly for tower and only 1P
		if success == true then
			sounds.SongDecide:Play()
			return true -- transition to song select if true
		end 
	elseif ssn.IsRandom == true then
		local rdNd = songList:GetRandomNodeInFolder(ssn)
		if rdNd ~= nil then
			local success = rdNd:Mount(5) -- for testing, go directly for tower and only 1P
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
	-- if no folder to close, trigger exit scene instead
	return songList:CloseFolder()
end


function draw()
	if textTex ~= nil then
		textTex:Draw(400,400)
	end
	if pageTexts ~= nil then
		for i, tx in pairs(pageTexts) do
			-- can be nil if no modulo pagination
			if tx ~= nil then 
				tx:Draw(100, 500+i*100)
			end
		end
	end

	NAMEPLATE:DrawPlayerNameplate(800, 100, 255, 0)
end

function update()
	if INPUT:Pressed("Cancel") == true or INPUT:KeyboardPressed("Escape") == true then
		local closeFolder = handleFolderClose()
		if closeFolder == true then
			refreshPage()
			sounds.Decide:Play()
		else
			sounds.Cancel:Play()
			return Exit("title", nil)
		end
	end
	if INPUT:KeyboardPressed("S") == true then
		sounds.Skip:Play()
		return Exit("stage", "demo1")
	end
	if INPUT:KeyboardPressed("K") == true and songList ~= nil then
		sounds.Skip:Play()
		songList:Move(1)
		refreshPage()
	end
	if INPUT:KeyboardPressed("D") == true and songList ~= nil then
		sounds.Skip:Play()
		songList:Move(-1)
		refreshPage()
	end
	if INPUT:Pressed("Decide") == true or INPUT:KeyboardPressed("Return") == true then
		local isPlayStarted = handleDecide()
		if isPlayStarted == true then
			return Exit("play", nil)
		end 
	end

	-- Test
	if INPUT:KeyboardPressed("P") then
		local sNode = songList:SearchFirstSongByPredicate(function(node)
		 	return node.Maker == "bol"
		end)
		-- local sNode = songList:GetSongByUniqueId("swtowerwttcSukima")
		if sNode ~= nil then
			local success = sNode:Mount(5)
			if success == true then
				sounds.SongDecide:Play()
				return Exit("play", nil)
			end 
		end
	end
end

function activate()
	test = GetSaveFile(0)
	-- textTex = text:GetText("You played " .. tostring(test.TotalPlaycount) .. " charts...")
end

function deactivate()

end

function onStart()
	text = TEXT:Create(16)

	sounds.Skip = SOUND:CreateSFX("Sounds/Skip.ogg")
	sounds.Cancel = SOUND:CreateSFX("Sounds/Cancel.ogg")
	sounds.Decide = SOUND:CreateSFX("Sounds/Decide.ogg")
	sounds.SongDecide = SOUND:CreateSFX("Sounds/SongDecide.ogg")
end 

function afterSongEnum()
	local lsls = GenerateSongListSettings()
	-- Test options here
	lsls.ModuloPagination = false
	lsls.RootGenreFolder = "太鼓タワー"

	-- Get song list 
	songList = RequestSongList(lsls)
	refreshPage()
end

function onDestroy()
	if textTex ~= nil then
		textTex:Dispose()
	end
	for _, sound in pairs(sounds) do
		sound:Dispose()
	end
end
