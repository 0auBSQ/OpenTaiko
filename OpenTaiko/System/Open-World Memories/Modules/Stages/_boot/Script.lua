local sounds = {}

local video = nil

local text = nil
local text_enter = TEXTURE:CreateTexture()

local counter = nil

-- First Time Menu --
local database = nil
local firsttime = false
local firsttimepage = 1
local volselected = false
local volcurrent = 0

local langindex = 1
local volindex = 1

local bg = TEXTURE:CreateTexture()
local header_langs = TEXTURE:CreateTexture()
local menu_langs = {}
local menu_vols = {}

local color_white = COLOR:CreateColorFromRGBA(255, 255, 255)
local color_red = COLOR:CreateColorFromRGBA(255, 50, 50)
local color_yellow = COLOR:CreateColorFromRGBA(255, 255, 50)
-- First Time Menu --

local function movedUp()
	return INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") or INPUT:KeyboardPressed("UpArrow")
end
local function movedDown()
	return INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") or INPUT:KeyboardPressed("DownArrow")
end
local function confirmed()
	return INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return")
end
local function cancelled()
	return INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape")
end

local function updateVolText()
	if text ~= nil then
		menu_vols = {
			[1] = {Id = "master", Text = text:GetText(LANG:GetString("SETTINGS_SYSTEM_MASTERVOL"))},
			[2] = {Id = "sfx", Text = text:GetText(LANG:GetString("SETTINGS_SYSTEM_SEVOL"))},
			[3] = {Id = "voice", Text = text:GetText(LANG:GetString("SETTINGS_SYSTEM_VOICEVOL"))},
			[4] = {Id = "song", Text = text:GetText(LANG:GetString("SETTINGS_SYSTEM_SONGVOL"))},
			[5] = {Id = "continue", Text = text:GetText("Continue")},
		}
	else
		menu_vols = {
			[1] = {Id = "master", Text = TEXTURE:CreateTexture()},
			[2] = {Id = "sfx", Text = TEXTURE:CreateTexture()},
			[3] = {Id = "voice", Text = TEXTURE:CreateTexture()},
			[4] = {Id = "song", Text = TEXTURE:CreateTexture()},
			[5] = {Id = "continue", Text = TEXTURE:CreateTexture()},
		}
	end
end
local function shiftVolume(amount)
	volcurrent = math.max(0, math.min(100, volcurrent + amount))
end
local function setVolume(channel, volume)
	if channel == "master" then
		CONFIG.MasterVolume = volume
	elseif channel == "sfx" then
		CONFIG.SoundEffectVolume = volume
	elseif channel == "voice" then
		CONFIG.VoiceVolume = volume
	elseif channel == "song" then
		CONFIG.SongVolume = volume
		CONFIG.PreviewVolume = math.floor(volume * 0.9)
	end
end
local function getVolume(channel)
	if channel == "master" then
		return CONFIG.MasterVolume
	elseif channel == "sfx" then
		return CONFIG.SoundEffectVolume
	elseif channel == "voice" then
		return CONFIG.VoiceVolume
	elseif channel == "song" then
		return CONFIG.SongVolume
	end
	return 0
end

function draw()
	if firsttime then
		if bg ~= nil then
			bg:DrawRect(0, 0, 0, 0, 1920, 1080)
		end

		if firsttimepage == 1 then
			local offset = (#menu_langs + 1) / 2
			header_langs:DrawAtAnchor(960, 540 + (60 * -offset), "Center")
			for i = 1, #menu_langs do
				menu_langs[i].Text:DrawAtAnchor(960, 540 + (60 * (i - offset)), "Center")
			end
		elseif firsttimepage == 2 then
			local offset = #menu_vols / 2
			for i = 1, #menu_vols do
				local y = 540 + (60 * (i - offset - 1))
				if volselected and i == volindex then
					menu_vols[i].Text:SetOpacity(0.25)
					menu_vols[i].Text:DrawAtAnchor(960, y, "Center")
					menu_vols[i].Text:SetOpacity(1.0)
					text:GetText(tostring(volcurrent)):DrawAtAnchor(960, y, "Center")
				else
					menu_vols[i].Text:DrawAtAnchor(960, y, "Center")
				end
			end
		end
	else
		if video ~= nil then
			video.Texture:Draw(0,0)
		end

		if text_enter ~= nil then
			text_enter:DrawAtAnchor(960, 960, "Center")
		end
	end
end

function update()
	counter:Tick()

	if firsttime then
		if movedUp() then
			if firsttimepage == 1 then
				langindex = math.max(langindex - 1, 1)
				menu_langs[langindex].Text:SetColor(color_red)
				menu_langs[langindex+1].Text:SetColor(color_white)
			elseif firsttimepage == 2 then
				if volselected then
					shiftVolume(-1)
				else
					volindex = math.max(volindex - 1, 1)
					menu_vols[volindex].Text:SetColor(color_red)
					menu_vols[volindex+1].Text:SetColor(color_white)

					if volindex == 4 then
						sounds.Song:Play()
					else
						sounds.Song:Stop()
					end
				end
				sounds.Move:Play()
			end
		elseif movedDown() then
			if firsttimepage == 1 then
				langindex = math.min(langindex + 1, #menu_langs)
				menu_langs[langindex].Text:SetColor(color_red)
				menu_langs[langindex-1].Text:SetColor(color_white)
			elseif firsttimepage == 2 then
				if volselected then
					shiftVolume(1)
				else
					volindex = math.min(volindex + 1, #menu_vols)
					menu_vols[volindex].Text:SetColor(color_red)
					menu_vols[volindex-1].Text:SetColor(color_white)

					if volindex == 4 then
						sounds.Song:Play()
					else
						sounds.Song:Stop()
					end
				end
				sounds.Move:Play()
			end
		elseif confirmed() then
			if firsttimepage == 1 then
				LANG:ChangeLanguage(menu_langs[langindex].Id)
				updateVolText()
				menu_vols[volindex].Text:SetColor(color_red)
				firsttimepage = 2
				sounds.Decide:Play()
			elseif firsttimepage == 2 then
				if menu_vols[volindex].Id == "continue" then
					firsttime = false
					database:Write("new_user", "false")
					counter:Start()
					video:Start()
					sounds.BGM:Play()
				else
					volselected = not volselected
					if volselected then
						volcurrent = getVolume(menu_vols[volindex].Id)
						menu_vols[volindex].Text:SetColor(color_yellow)
					else
						setVolume(menu_vols[volindex].Id, volcurrent)
						menu_vols[volindex].Text:SetColor(color_red)
					end
					sounds.Decide:Play()
				end
			end
		elseif cancelled() then
			if firsttimepage == 2 and not volselected then
				firsttimepage = 1
				sounds.Cancel:Play()
			end
		end
	else
		if confirmed() then
			sounds.Skip:Play()
			return Exit("title", nil)
		end

		if text_enter ~= nil then
			text_enter:SetOpacity(counter.Value)
		end

		if video ~= nil and video.Duration > 0.0 and video:GetPlayPosition() >= math.floor(video.Duration) then
			return Exit("title", nil)
		end
	end
end

function activate()
	text = TEXT:Create(32)
	text_enter = text:GetText("Press Enter to Start!")
	video = VIDEO:CreateVideo("Videos/intro.mp4")
	
	sounds.BGM = SOUND:CreateBGM("Sounds/BGM.ogg")
	--sounds.BGM:SetLoop(true)
	sounds.Decide = SHARED:GetSharedSound("Decide")
	sounds.Skip = SHARED:GetSharedSound("Skip")
	sounds.Move = SHARED:GetSharedSound("Move")
	sounds.Cancel = SHARED:GetSharedSound("Cancel")

	counter = COUNTER:CreateCounter(0.0, 1.0, 1.0)
	counter:SetBounce(true)

	if not firsttime then
		counter:Start()
		video:Start()
		sounds.BGM:Play()
	else
		bg = TEXTURE:CreateTexture("Textures/Tile.png")
		sounds.Song = SOUND:CreateBGM("Sounds/StartupSong.ogg")
		sounds.Song:SetLoop(true)

		local langs = LANG:GetAvailableLanguages()
		local iter = langs:GetEnumerator()
		local index = 1
		while iter:MoveNext() do
			local cur = iter.Current
			local k = cur.Key
			local v = cur.Value
			menu_langs[index] = {Id = k, Text = text:GetText(v)}
			index = index + 1
		end
		menu_langs[1].Text:SetColor(color_red)

		header_langs = text:GetText(LANG:GetString("SETTINGS_SYSTEM_LANGUAGE"))
	end
end

function deactivate()
	if video ~= nil then
		video:Dispose()
	end
	if sounds.BGM ~= nil then
		sounds.BGM:Dispose()
	end
	if text ~= nil then
		text:Dispose()
	end
	-- First Time Menu
	if sounds.Song ~= nil then
		sounds.Song:Dispose()
	end
	for _, ltext in pairs(menu_langs) do
		ltext.Text:Dispose()
	end
	for _, vtext in pairs(menu_vols) do
		vtext.Text:Dispose()
	end
	if bg ~= nil then
		bg:Dispose()
	end
end

function onStart()
	SHARED:SetSharedSFX("Decide", "Sounds/Decide.ogg")
	SHARED:SetSharedSFX("Move", "Sounds/Move.ogg")
	SHARED:SetSharedSFX("Cancel", "Sounds/Cancel.ogg")
	SHARED:SetSharedSFX("Skip", "Sounds/Skip.ogg")
	SHARED:SetSharedSFX("SongDecide", "Sounds/SongDecide.ogg")

	database = DATABASE:OpenGlobalDatabase("GameStatus")
	if database:Read("new_user") == nil or CONFIG.ConfigIsNew then
		database:Write("new_user", tostring(CONFIG.ConfigIsNew))
	end

	local bool = {["true"]=true,["false"]=false}
	firsttime = bool[database:Read("new_user")] or CONFIG.ConfigIsNew
end

function afterSongEnum()
	if sounds ~= nil and sounds.BGM ~= nil and video ~= nil then
		sounds.BGM:SetTimestamp(video:GetPlayPosition() * 1000)
	end
end

function onDestroy()
	SHARED:ClearSharedSound("Decide")
	SHARED:ClearSharedSound("Move")
	SHARED:ClearSharedSound("Cancel")
	SHARED:ClearSharedSound("Skip")
	SHARED:ClearSharedSound("SongDecide")

	database:Dispose()
end
