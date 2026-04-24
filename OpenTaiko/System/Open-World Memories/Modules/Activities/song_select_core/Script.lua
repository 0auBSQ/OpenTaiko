-- song_select_core Activity
-- Core song-select logic shared by regular_song_select, ai_battle_song_select,
-- and training_song_select.
--
-- activate(config) parameters (all optional, nil = default):
--   config.allowPlayerCount  (bool)   true by default — allow L key to cycle player count
--   config.lockedPlayerCount (int)    nil by default  — forces CONFIG.PlayerCount to this value
--   config.mountAISlotToP2   (bool)   false by default — mounts the AI virtual slot onto spot 2
--
-- update() return values:
--   "play"   — a song was successfully mounted, parent stage should Exit("play")
--   "cancel" — user backed out entirely, parent stage should decide where to go
--   nil      — still running, no action needed

local textSmall = nil
local text = nil
local textLarge = nil

local sounds = {}

-- The song list is kept alive across activate/deactivate cycles so all three
-- song-select stages see the same list (and the same current position).
local songList = nil
local currentPage = {}
local pageTexts = {}
local genre_overlays = {}

local bars = {}
local bgtx = {}

local favoriteicon = nil

local highlightedPlayer = 0

-- Inner activities (dialogs opened from within song select)
local act_inner = {}

-- ROActivities
local modicons_ro = nil

-- Animations and counters
local ctx = {}

local currentBackground = 0
local backgroundScrollX = 0

local songSelectShift = 0
local songSelectElemOpacity = 255
local difficultySelectElemOpacity = 0

local levelLabelFrame = 0
local difficultyFade4 = 0

local arrowsDistance = 0

-- -1/1 to 0 when moving
local selectBoxDist = 0

local BOX_SCROLL_SECONDS = 0.06

-- Hold scroll state
local holdDir = 0
local HOLD_DELAY_SECONDS  = 0.16
local HOLD_REPEAT_SECONDS = 0.06

-- Screens/Statuses
--- songselect | pretransition | transition | difficultyselect
local activeScreen = "songselect"
local songSelectModes = { songselect = true, pretransition = true, transition = true }
local difficultySelectModes = { difficultyselect = true, transition = true }

-- Tracks whether the customize dialog was active on the previous frame
local wasCustomizeActive = false

-- Puchichara floating sine animation
local puchiSineY = 0

-- To-play song info
local selectedSongNode = nil

-- Preview state
local previewDemoStartRaw = 0
local previewDemoStart = 0
local previewDurationMs = 0
local previewLoaded = false

-- Difficulty select variables
local diffBars = {}
local diffIndex = {0, 0, 0, 0, 0}
local diffSelected = {false, false, false, false, false}

-- Config set on each activate() call
local activeConfig = {}
-- Last signal returned by update(); used so deactivate() knows whether we're heading to play
local lastSignal = nil


-- Inputs for each player
local inputSets = {
	{
		right = "RightChange",
		left = "LeftChange",
		decide1 = "Decide",
		decide2 = "Decide",
		cancel = "Cancel",
		auto = "ToggleAutoP1"
	},
	{
		right = "RBlue2P",
		left = "LBlue2P",
		decide1 = "RRed2P",
		decide2 = "LRed2P",
		cancel = nil,
		auto = "ToggleAutoP2"
	},
	{
		right = "RBlue3P",
		left = "LBlue3P",
		decide1 = "RRed3P",
		decide2 = "LRed3P",
		cancel = nil,
		auto = nil
	},
	{
		right = "RBlue4P",
		left = "LBlue4P",
		decide1 = "RRed4P",
		decide2 = "LRed4P",
		cancel = nil,
		auto = nil
	},
	{
		right = "RBlue5P",
		left = "LBlue5P",
		decide1 = "RRed5P",
		decide2 = "LRed5P",
		cancel = nil,
		auto = nil
	},
}

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

local PREVIEW_THROTTLE_MS = 200

local HEADER_OFFSET_X = 1780
local HEADER_BOX_TEXT_OFFSET_X = 250
local HEADER_BOX_TEXT_OFFSET_Y = 12

local NAMEPLATE_BOX_FOLDED_SIZE_Y = 182
local NAMEPLATE_SECONDARY_OFFSET_Y = 81
local NAMEPLATE_BOX_START_X = 0
local NAMEPLATE_BOX_SPACING_X = 384
local NAMEPLATE_OFFSET_X = 27
local NAMEPLATE_OFFSET_Y = 37
local NAMEPLATE_HEIGHT = 81

local PUCHI_OFFSET_X = 60
local PUCHI_FLOAT_AMP = 8

local DIFFSELECT_CHARA_ORIG_X_35P = 1250
local DIFFSELECT_CHARA_ORIG_Y_35P = 470
local DIFFSELECT_CHARA_GAP_X_35P = 332
local DIFFSELECT_CHARA_GAP_Y_35P = 457
local DIFFSELECT_CHARA_SCALE_35P = 0.5
local DIFFSELECT_CHARA_ORIG_X_12P = 1300
local DIFFSELECT_CHARA_ORIG_Y_12P = 760
local DIFFSELECT_CHARA_GAP_X_12P = 468
local DIFFSELECT_CHARA_SCALE_12P = 0.8

local DIFFSELECT_SMALL_BAR_X = {678, 718, 758}
local DIFFSELECT_SMALL_BAR_Y = {721, 900, 1079}
local DIFFSELECT_SMALL_BAR_SELECT_Y_OFFSET = 156
local DIFFSELECT_SMALL_BAR_SELECT_Y_OFFSET_CORRECTION = -6
local DIFFSELECT_BIG_BAR_ORIG_X = 676
local DIFFSELECT_BIG_BAR_ORIG_Y = 309
local DIFFSELECT_BIG_BAR_GAP_X = 3
local DIFFSELECT_BIG_BAR_GAP_Y = 180
local DIFFSELECT_NOTESDESIGNER_OFFSET_Y = -10
local DIFFSELECT_LEVEL_BAR_X = 480
local DIFFSELECT_LEVEL_BAR_Y = 187
local DIFFSELECT_LEVEL_NB_OFF_X = -226
local DIFFSELECT_LEVEL_NB_OFF_Y = -115
local DIFFSELECT_LEVEL_COLORS = {
	COLOR:CreateColorFromHex("FF65A9F7"),
	COLOR:CreateColorFromHex("FF8FF5A9"),
	COLOR:CreateColorFromHex("FFE6DA76"),
	COLOR:CreateColorFromHex("FFF39898"),
	COLOR:CreateColorFromHex("FFCD7EE6")
}

-- ============================================================
-- Helper functions
-- ============================================================

local function drawPlayerChara(player, x, y, scaleX, scaleY, opacity, flipX)
	local chara = GetSaveFile(player):GetCharacter()
	if chara ~= nil and chara.IsValid then
		chara:Update(CHARACTER.ANIM_MENU_NORMAL, true)
		local effectiveScaleX = (flipX or false) and -scaleX or scaleX
		chara:DrawAtAnchor(x, y, CHARACTER.ANIM_MENU_NORMAL, "bottom", effectiveScaleX, scaleY, math.floor(opacity * 255))
	end
end

local function drawPlayerPuchi(player, x, y, scaleX, scaleY, opacity)
	local puchi = GetSaveFile(player):GetPuchichara()
	if puchi == nil or puchi.tx == nil or not puchi.tx.Loaded then return end
	local frameW = math.floor(puchi.tx.Width / 2)
	local frameH = puchi.tx.Height
	puchi.tx:SetScale(scaleX, scaleY)
	puchi.tx:SetOpacity(opacity)
	puchi.tx:DrawRectAtAnchor(x, y, 0, 0, frameW, frameH, "bottom")
	puchi.tx:SetOpacity(1)
	puchi.tx:SetScale(1, 1)
end

local function drawCharaWithNameplate(player, x, y, scaleX, scaleY, opacity, flipX)
	drawPlayerChara(player, x + bgtx["nameplate_info"].Width/2 - NAMEPLATE_OFFSET_X, y, scaleX, scaleY, opacity, flipX)
	NAMEPLATE:DrawPlayerNameplate(x, y, opacity*255, player)
end

local function startCounter(key, startVal, endVal, interval, mode, updateCallback, onFinish)
	local c = COUNTER:CreateCounter(startVal, endVal, interval, onFinish)
	if mode == "loop" then c:SetLoop(true)
	elseif mode == "bounce" then c:SetBounce(true) end
	if updateCallback then c:Listen(updateCallback) end
	c:Start()
	ctx[key] = c
	return c
end

local function formatDuration(ms)
	if ms <= 0 then return "?:??" end
	local totalSec = math.floor(ms / 1000)
	local m = math.floor(totalSec / 60)
	local s = totalSec % 60
	return string.format("%d:%02d", m, s)
end

local function formatNumber(n, decimals)
	local s = string.format("%."..decimals.."f", n)
	s = s:gsub("0+$", "")
	s = s:gsub("%.$", "")
	return s
end

local function getSongNodeFocusChart(songNode)
	if songNode.IsSong ~= true then return nil end
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
			if i > 1 then cursorX = cursorX + (prevWidth * 0.5) end
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
			if i > 1 then cursorX = cursorX + (prevWidth * 0.5) end
			tex:SetColor(color)
			tex:SetOpacity(opacity)
			tex:DrawAtAnchor(cursorX + (w / 2), y, "center")
			tex:SetOpacity(1)
			tex:SetColor(white)
			prevWidth = w
		end
	end
end

local function drawDifficultyBar(index, barinfo)
	local xshift = 1920 - songSelectShift
	local tex = bars["difficultybar7"]
	if barinfo.vault == false then
		tex = bars["difficultybar"..(barinfo.difficulty + 2)]
	end
	local xpos = DIFFSELECT_BIG_BAR_ORIG_X + (index-3)*DIFFSELECT_BIG_BAR_GAP_X + xshift
	local ypos = DIFFSELECT_BIG_BAR_ORIG_Y + (index-3)*DIFFSELECT_BIG_BAR_GAP_Y
	for i = 1, CONFIG.PlayerCount, 1 do
		if diffIndex[i] == index and not (activeConfig.mountAISlotToP2 and i == 2) then
			bars["difficultybarselect"..i]:DrawRectAtAnchor(
				xpos,
				ypos+DIFFSELECT_SMALL_BAR_SELECT_Y_OFFSET_CORRECTION,
				0, 0,
				bars["difficultybarselect"..i].Width,
				DIFFSELECT_SMALL_BAR_SELECT_Y_OFFSET,
				"bottomright"
			)
		end
	end
	tex:DrawAtAnchor(xpos, ypos, "bottomright")
	local nd = textSmall:GetText("Charter - "..barinfo.charter, false, 1000)
	nd:DrawAtAnchor(xpos, ypos+DIFFSELECT_NOTESDESIGNER_OFFSET_Y, "topright")
	local xbar = xpos - tex.Width + DIFFSELECT_LEVEL_BAR_X
	local ybar = ypos - tex.Height + DIFFSELECT_LEVEL_BAR_Y
	local bartx = bars["difficultybarlevel5"]
	if barinfo.vault == false then
		bartx = bars["difficultybarlevel"..(math.min(3, barinfo.difficulty) + 1)]
		if barinfo.level > 10 then bartx = bars["difficultybarlevel6"] end
	end
	bartx:DrawRect(xbar, ybar, 0, 0, bartx.Width * (math.min(10, barinfo.level) / 10), bartx.Height)
	if barinfo.level > 10 then
		local bartxanim = bars["difficultybarlevel7"]
		bartxanim:DrawRect(xbar, ybar, 0, bartx.Height*levelLabelFrame,
			bartx.Width * (math.min(3, barinfo.level-10) / 3), bartx.Height)
	end
	local xlvnb = xpos+DIFFSELECT_LEVEL_NB_OFF_X
	local ylvnb = ypos+DIFFSELECT_LEVEL_NB_OFF_Y
	drawNumberCentered(barinfo.level, "diffsel_level", xlvnb, ylvnb)
	local lvnbcol = COLOR:CreateColorFromHex("FFB9B9B9")
	if barinfo.vault == false then lvnbcol = DIFFSELECT_LEVEL_COLORS[barinfo.difficulty + 1] end
	drawNumberCentered(barinfo.level, "diffsel_levelcol", xlvnb, ylvnb, lvnbcol)
end

local function drawDiffSelectBar(index, barinfo)
	local xshift = 1920 - songSelectShift
	if index < 3 then
		local xpos = DIFFSELECT_SMALL_BAR_X[index + 1] + xshift
		local ypos = DIFFSELECT_SMALL_BAR_Y[index + 1]
		for i = 1, CONFIG.PlayerCount, 1 do
			if diffIndex[i] == index and not (activeConfig.mountAISlotToP2 and i == 2) then
				bars["difficultybarselect"..i]:DrawRectAtAnchor(
					xpos, ypos, 0,
					DIFFSELECT_SMALL_BAR_SELECT_Y_OFFSET,
					bars["difficultybarselect"..i].Width,
					bars["difficultybarselect"..i].Height-DIFFSELECT_SMALL_BAR_SELECT_Y_OFFSET,
					"bottomleft"
				)
			end
		end
		bars["smallbar"..index]:DrawAtAnchor(xpos, ypos, "bottomleft")
	else
		drawDifficultyBar(index, barinfo)
	end
end

local function drawLevelTag(songNode, x, y)
	local chart = getSongNodeFocusChart(songNode)
	if chart == nil then return nil end
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
	SHARED:ClearSharedTexture("preimage")
	if songNode.IsSong == true then
		startCounter("throttle_preimage", 0, PREVIEW_THROTTLE_MS, 0.2/PREVIEW_THROTTLE_MS, "none", nil, function()
			if songNode.HasPreimage then
				SHARED:SetSharedTextureUsingAbsolutePath("preimage", songNode.PreimagePath)
			else
				SHARED:SetSharedTexture("preimage", "Textures/preimage.png")
			end
		end)
	else
		ctx["throttle_preimage"] = COUNTER:EmptyCounter()
	end
	SHARED:GetSharedTexture("preimage"):SetWrapMode("Border")
end

local function drawPreimage()
	local node = songList:GetSongNodeAtOffset(0)
	if node.IsSong == true then
		bgtx["preimage_load"]:Draw(PREIMAGE_ORIGIN_X-songSelectShift, PREIMAGE_ORIGIN_Y)
		bgtx["load"]:DrawAtAnchor(PREIMAGE_ORIGIN_X-songSelectShift+PREIMAGE_SIZE_X/2, PREIMAGE_ORIGIN_Y+PREIMAGE_SIZE_Y/2, "center")
		local tex = SHARED:GetSharedTexture("preimage")
		if tex.Height > 0 and tex.Width > 0 then
			local sH = PREIMAGE_SIZE_X / tex.Height
			local sW = PREIMAGE_SIZE_Y / tex.Width
			tex:SetScale(sW, sH)
			tex:Draw(PREIMAGE_ORIGIN_X-songSelectShift, PREIMAGE_ORIGIN_Y)
		end
	end
end

local function playPreview(songNode)
	previewLoaded = false
	previewDurationMs = 0
	previewDemoStartRaw = 0
	previewDemoStart = 0
	SHARED:SetSharedPreview("presound", "Sounds/empty.ogg")
	if songNode.IsSong == true then
		startCounter("throttle_presound", 0, PREVIEW_THROTTLE_MS, 0.2/PREVIEW_THROTTLE_MS, "none", nil, function()
			local demoStart = songNode.DemoStart
			SHARED:SetSharedPreviewUsingAbsolutePath("presound", songNode.AudioPath, function(snd)
				local speed = CONFIG.SongSpeed / 20
				snd:SetSpeed(speed)
				snd:Play()
				snd:SetTimestamp(math.floor(demoStart / speed))
				previewDurationMs = snd:GetDurationMs()
				previewDemoStartRaw = demoStart
				previewDemoStart = math.floor(demoStart / speed)
				previewLoaded = true
			end)
		end)
	else
		ctx["throttle_presound"] = COUNTER:EmptyCounter()
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

		if i == 0 and node ~= nil then
			reloadPreimage(node)
			playPreview(node)
		end
	end

	if ctx["extreme_fade"] then ctx["extreme_fade"]:Start() end
end

local function handleDecideSongSelect()
	local ssn = songList:GetSelectedSongNode()

	if ssn.IsFolder == true then
		local success = songList:OpenFolder()
		refreshPage()
		if success == true then sounds.Decide:Play() end
	elseif ssn.IsReturn == true then
		local success = songList:CloseFolder()
		refreshPage()
		if success == true then sounds.Cancel:Play() end
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
	return nil
end

-- Returns false if there is no folder to close (caller should exit to parent stage)
local function handleFolderClose()
	if songList == nil then return false end
	return songList:CloseFolder()
end

-- ============================================================
-- Hold scroll helpers
-- ============================================================

local startRepeat

local function stopHold()
	holdDir = 0
	ctx["hold_delay"]  = COUNTER:EmptyCounter()
	ctx["hold_repeat"] = COUNTER:EmptyCounter()
	startRepeat = function() end
end

local function doMove(dir)
	sounds.Skip:Play()
	songList:Move(dir)
	refreshPage()
	startCounter("scroll_box_anim", dir, 0, dir > 0 and -BOX_SCROLL_SECONDS or BOX_SCROLL_SECONDS, "none", function(val)
		selectBoxDist = val
	end)
end

local function startHold(dir)
	holdDir = dir
	ctx["hold_delay"]  = COUNTER:EmptyCounter()
	ctx["hold_repeat"] = COUNTER:EmptyCounter()
	startRepeat = function()
		startCounter("hold_repeat", 0, 1, HOLD_REPEAT_SECONDS, "none", nil, function()
			if holdDir ~= 0 then
				doMove(holdDir)
				startRepeat()
			end
		end)
	end
	startCounter("hold_delay", 0, 1, HOLD_DELAY_SECONDS, "none", nil, function()
		if holdDir ~= 0 then startRepeat() end
	end)
end

-- ============================================================

local function loadDiffBars(ssn)
	diffBars = {}
	local startDiff = 0
	local isVault = ssn.Genre == "Secret Vault"
	if isVault then startDiff = 3 end
	for i = startDiff, 4, 1 do
		local chart = ssn:GetChart(i)
		if chart ~= nil then
			table.insert(diffBars, {
				vault = isVault,
				level = chart.Level,
				isplus = chart.IsPlus,
				charter = chart.NotesDesigner,
				difficulty = i
			})
		end
	end
end

local function updateTransitionVisuals(val)
	songSelectShift = val
	local opacity = 255 - (val * (255 / 960))
	songSelectElemOpacity = math.max(0, math.min(255, opacity))
	local diffOpacity = (val - 960) * (255 / 960)
	difficultySelectElemOpacity = math.max(0, math.min(255, diffOpacity))
end

local function resetToSongSelect()
	songSelectElemOpacity = 255
	difficultySelectElemOpacity = 0
	songSelectShift = 0
	activeScreen = "songselect"
end

-- ============================================================
-- Activity lifecycle
-- ============================================================

function draw(mode)
	if mode ~= "no_bg" then
		-- Scrolling background (texture set by parent stage or defaulted in activate)
		SHARED:GetSharedTexture("background"):Draw(-backgroundScrollX, 0)
		SHARED:GetSharedTexture("background"):Draw(-backgroundScrollX+1920, 0)
	end
	if mode == "bg_only" then return end

	local ssn = songList:GetSelectedSongNode()

	if difficultySelectModes[activeScreen] then
		local opacityNorm = difficultySelectElemOpacity/255

		bgtx["difficultyselect"]:Draw(1920-songSelectShift, 0)
		bgtx["header"]:Draw(1920-songSelectShift, 0)

		bgtx["overlay_difficulty"]:SetOpacity(opacityNorm)
		bgtx["overlay_difficulty"]:DrawAtAnchor(1920, 0, "TopRight")

		if selectedSongNode ~= nil then
			local titleTx = textLarge:GetText(selectedSongNode.Title, false, 1280)
			local subtitleTx = text:GetText(selectedSongNode.Subtitle, false, 1280)
			titleTx:SetOpacity(opacityNorm)
			titleTx:Draw(1926 - songSelectShift, 0)
			subtitleTx:SetOpacity(opacityNorm)
			subtitleTx:Draw(1926 - songSelectShift, 67)

			for i = 0, 2+#diffBars, 1 do
				local barinfo = nil
				if i >= 3 then barinfo = diffBars[i-2] end
				drawDiffSelectBar(i, barinfo)
			end
		end

		do
			local p = CONFIG.PlayerCount
			local is35 = p > 2
			local ox = is35 and DIFFSELECT_CHARA_ORIG_X_35P or DIFFSELECT_CHARA_ORIG_X_12P
			local oy = is35 and DIFFSELECT_CHARA_ORIG_Y_35P or DIFFSELECT_CHARA_ORIG_Y_12P
			local gx = is35 and DIFFSELECT_CHARA_GAP_X_35P or DIFFSELECT_CHARA_GAP_X_12P
			local gy = is35 and DIFFSELECT_CHARA_GAP_Y_35P or 0
			local s  = is35 and DIFFSELECT_CHARA_SCALE_35P or DIFFSELECT_CHARA_SCALE_12P
			local r1Count = (p == 5 and 3) or (p > 2 and 2) or p

			for i = 0, p - 1 do
				local isRow2 = i >= r1Count
				local r = isRow2 and 1 or 0
				local cols = isRow2 and (p - r1Count) or r1Count
				local colIdx = isRow2 and (i - r1Count) or i
				local x = ox + (colIdx - (cols - 1) / 2) * gx
				local y = oy + r * gy
				drawCharaWithNameplate(i, x, y, s, s, opacityNorm, true)
				local charaX = x + bgtx["nameplate_info"].Width/2 - NAMEPLATE_OFFSET_X
				drawPlayerPuchi(i, charaX - PUCHI_OFFSET_X * s, y + puchiSineY * s, s, s, opacityNorm)
				if modicons_ro ~= nil then
					modicons_ro:Draw(x, y + NAMEPLATE_HEIGHT + 4, i, nil, difficultySelectElemOpacity)
				end
			end
		end

		-- AI level slider display (AI battle only)
		if activeConfig.mountAISlotToP2 then
			local xshift = 1920 - songSelectShift
			local cx = 1490 + xshift
			local labelTx = textSmall:GetText("Starting AI Level", false, 400)
			labelTx:SetOpacity(opacityNorm)
			labelTx:DrawAtAnchor(cx, 940, "center")
			local levelTx = text:GetText(tostring(CONFIG.AILevel), false, 200)
			levelTx:SetOpacity(opacityNorm)
			levelTx:DrawAtAnchor(cx, 980, "center")
			-- Animated bound arrows; hidden at level extremes to show clamp
			local ax = arrowsDistance
			if CONFIG.AILevel > 1 then
				local arrowL = textSmall:GetText("◀", false, 60)
				arrowL:SetOpacity(opacityNorm)
				arrowL:DrawAtAnchor(cx - 55 - ax, 980, "right")
			end
			if CONFIG.AILevel < 10 then
				local arrowR = textSmall:GetText("▶", false, 60)
				arrowR:SetOpacity(opacityNorm)
				arrowR:DrawAtAnchor(cx + 55 + ax, 980, "left")
			end
		end
	end

	if songSelectModes[activeScreen] then
		local opacityNorm = songSelectElemOpacity/255

		if ssn ~= nil and ssn.IsRandom then
			bgtx["randominfo"]:DrawAtAnchor(1920-songSelectShift,0,"topright")
		end

		if ssn ~= nil and ssn.IsSong then
			bgtx["songinfo"]:DrawAtAnchor(1920-songSelectShift,0,"topright")
			if ssn.HasVideo then
				bgtx["sinfo_video"]:Draw(SONGINFO_HASVIDEO_ORIGIN_X-songSelectShift,SONGINFO_HASVIDEO_ORIGIN_Y)
			end
			if ssn.Explicit then
				bgtx["sinfo_explicit"]:DrawAtAnchor(SONGINFO_EXPLICIT_ORIGIN_X-songSelectShift,SONGINFO_EXPLICIT_ORIGIN_Y,"topright")
			end
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
					if chart.IsPlus then bgtx["sinfo_difficulties_"..i.."_plus"]:Draw(xpos,ypos) end
				end
			end
			local focusedChart = getSongNodeFocusChart(ssn)
			local subtitleTx = textSmall:GetText(ssn.Subtitle, false, SONGINFO_SUBTITLE_MWIDTH)
			local charterTx = textSmall:GetText("Chart - "..ssn.Maker, false, SONGINFO_CHARTER_MWIDTH)
			local lengthTx = textSmall:GetText("Length - "..formatDuration(previewDurationMs), false, SONGINFO_LENGTH_MWIDTH)
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
				if mult < 1 then color = "ff95ccff"
				elseif mult > 1 then color = "ffff9ec3" end
				local bpmTx = text:GetText(bpmText, false, SONGINFO_BPM_MWIDTH, COLOR:CreateColorFromHex(color))
				bpmTx:DrawAtAnchor(SONGINFO_BPM_ORIGIN_X-songSelectShift, SONGINFO_BPM_ORIGIN_Y, "center")
			end
		end

		drawPreimage()

		if pageTexts ~= nil then
			for i, tx in pairs(pageTexts) do
				local xpos = SONGLIST_ORIGIN_X+(i+selectBoxDist)*SONGLIST_OFFSET_X-songSelectShift
				local ypos = SONGLIST_ORIGIN_Y+(i+selectBoxDist)*SONGLIST_OFFSET_Y
				if i == 0 then xpos = xpos + SONGLIST_SELECTED_X_DIFF end
				if tx ~= nil then
					if currentPage[i].IsSong or currentPage[i].IsFolder then
						if currentPage[i].IsLocked then
							bars["locked"]:DrawAtAnchor(xpos,ypos,"center")
						else
							bars["bar"]:SetColor(currentPage[i].BoxColor)
							bars["bar"]:DrawAtAnchor(xpos,ypos,"center")
							genre_overlays[currentPage[i].Genre]:DrawAtAnchor(xpos,ypos,"center")
						end
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

		bgtx["header"]:Draw(-songSelectShift, 0)
		if ssn ~= nil then
			local pathStack = {}
			local currentNode = ssn.Parent
			while currentNode ~= nil do
				local _tx = "/"
				if currentNode.Title ~= nil then _tx = currentNode.Title end
				table.insert(pathStack, text:GetText(_tx, false, 270))
				currentNode = currentNode.Parent
			end
			local xpos = HEADER_OFFSET_X-songSelectShift
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

		bgtx["overlay"]:SetOpacity(opacityNorm)
		bgtx["overlay"]:Draw(0, 0)

		local playerCount = CONFIG.PlayerCount
		highlightedPlayer = highlightedPlayer % CONFIG.PlayerCount

		bgtx["nameplate_info"]:SetOpacity(opacityNorm)
		do
			local x0 = NAMEPLATE_BOX_START_X
			local y0 = 1080 - NAMEPLATE_BOX_FOLDED_SIZE_Y
			bgtx["nameplate_info"]:Draw(x0, y0)
			local ssCharaX = x0 + bgtx["nameplate_info"].Width/2
			drawPlayerChara(highlightedPlayer, ssCharaX, y0+NAMEPLATE_OFFSET_Y, 1.0, 1.0, opacityNorm, false)
			drawPlayerPuchi(highlightedPlayer, ssCharaX - PUCHI_OFFSET_X, y0+NAMEPLATE_OFFSET_Y + puchiSineY, 1.0, 1.0, opacityNorm)
			NAMEPLATE:DrawPlayerNameplate(x0+NAMEPLATE_OFFSET_X, y0+NAMEPLATE_OFFSET_Y, songSelectElemOpacity, highlightedPlayer)
		end

		for i = 1, playerCount - 1, 1 do
			local j = i
			if j - 1 >= highlightedPlayer then j = j + 1 end
			local xpos = NAMEPLATE_BOX_START_X + i * NAMEPLATE_BOX_SPACING_X
			local ypos = 1080 - NAMEPLATE_SECONDARY_OFFSET_Y
			bgtx["placeholder_portrait"]:SetOpacity(opacityNorm)
			bgtx["placeholder_portrait"]:DrawAtAnchor(xpos+bgtx["nameplate_info"].Width/2, ypos, "bottom")
			bgtx["placeholder_portrait"]:SetOpacity(1)
			NAMEPLATE:DrawPlayerNameplate(xpos+NAMEPLATE_OFFSET_X, ypos, songSelectElemOpacity, j - 1)
		end
	end

	-- Inner activity draw calls (dialogs, mod select, etc.)
	for _, at in pairs(act_inner) do
		if at.IsActive then at:Draw() end
	end
end

function update()
	for k, counter in pairs(ctx) do counter:Tick() end

	-- Loop preview
	if previewLoaded then
		local psnd = SHARED:GetSharedSound("presound")
		if psnd.Loaded and not psnd.IsPlaying then
			psnd:Play()
			psnd:SetTimestamp(previewDemoStart)
		end
	end

	-- Inner activity updates (dialogs, mod select, etc.)
	local hasActiveInnerModal = false
	for _, at in pairs(act_inner) do
		if at.IsActive then
			at:Update()
			hasActiveInnerModal = true
		end
	end

	-- Reload character animations when customize_dialog closes
	local isCustomizeActive = act_inner["customize_dialog"] ~= nil and act_inner["customize_dialog"].IsActive
	if wasCustomizeActive and not isCustomizeActive then
		for p = 0, 4, 1 do
			local chara = GetSaveFile(p):GetCharacter()
			if chara ~= nil and chara.IsValid then
				if not chara:AvailableAnimation(CHARACTER.ANIM_MENU_NORMAL) then
					chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)
				end
			end
		end
	end
	wasCustomizeActive = isCustomizeActive

	if hasActiveInnerModal then return nil end

	if activeScreen == "songselect" then
		selectedSongNode = nil

		if INPUT:KeyboardPressed("S") then
			sounds.Skip:Play()
			CONFIG:SetDefaultCourse(0, (CONFIG:GetDefaultCourse(0) + 1) % 5)
		end

		if INPUT:KeyboardPressed("P") and not activeConfig.mountAISlotToP2 then
			sounds.Skip:Play()
			highlightedPlayer = (highlightedPlayer + 1) % CONFIG.PlayerCount
		end

		if INPUT:KeyboardPressed("F3") then
			sounds.Decide:Play()
			CONFIG:SetAutoStatus(0, not CONFIG:GetAutoStatus(0))
		end
		if INPUT:KeyboardPressed("F4") and CONFIG.PlayerCount >= 2 then
			sounds.Decide:Play()
			CONFIG:SetAutoStatus(1, not CONFIG:GetAutoStatus(1))
		end

		local inpset = inputSets[highlightedPlayer + 1]

		if holdDir == 1 and not INPUT:Pressing(inpset.right) and not INPUT:KeyboardPressing("RightArrow") then
			stopHold()
		elseif holdDir == -1 and not INPUT:Pressing(inpset.left) and not INPUT:KeyboardPressing("LeftArrow") then
			stopHold()
		end

		if (INPUT:Pressed(inpset.right) or INPUT:KeyboardPressed("RightArrow")) and songList ~= nil then
			doMove(1)
			startHold(1)
		elseif (INPUT:Pressed(inpset.left) or INPUT:KeyboardPressed("LeftArrow")) and songList ~= nil then
			doMove(-1)
			startHold(-1)
		elseif INPUT:Pressed(inpset.decide1) or INPUT:Pressed(inpset.decide2) or INPUT:KeyboardPressed("Return") then
			stopHold()
			selectedSongNode = handleDecideSongSelect()
		elseif (inpset.cancel ~= nil and INPUT:Pressed(inpset.cancel)) or INPUT:KeyboardPressed("Escape") then
			stopHold()
			local closeFolder = handleFolderClose()
			if closeFolder == true then
				refreshPage()
				sounds.Decide:Play()
			else
				sounds.Cancel:Play()
				return "cancel"
			end
		end

		-- Player count cycling (only when allowed by config)
		if activeConfig.allowPlayerCount ~= false then
			if INPUT:KeyboardPressed("L") then
				sounds.Skip:Play()
				CONFIG.PlayerCount = 1 + (CONFIG.PlayerCount % 5)
			end
		end

		if INPUT:KeyboardPressed("Q") then
			sounds.Skip:Play()
			CONFIG.SongSpeed = CONFIG.SongSpeed - 1
			local speed = CONFIG.SongSpeed / 20
			SHARED:GetSharedSound("presound"):SetSpeed(speed)
			if previewDemoStartRaw > 0 then previewDemoStart = math.floor(previewDemoStartRaw / speed) end
		end

		if INPUT:KeyboardPressed("W") then
			sounds.Skip:Play()
			CONFIG.SongSpeed = CONFIG.SongSpeed + 1
			local speed = CONFIG.SongSpeed / 20
			SHARED:GetSharedSound("presound"):SetSpeed(speed)
			if previewDemoStartRaw > 0 then previewDemoStart = math.floor(previewDemoStartRaw / speed) end
		end

		if INPUT:KeyboardPressed("O") then
			if currentBackground == 0 then
				SHARED:SetSharedTexture("background", "Textures/bg1.png")
				currentBackground = 1
			else
				SHARED:SetSharedTexture("background", "Textures/bg0.png")
				currentBackground = 0
			end
		end

		-- Enforce locked player count after any input
		if activeConfig.lockedPlayerCount ~= nil then
			CONFIG.PlayerCount = activeConfig.lockedPlayerCount
		end

		-- In AI battle, keep spotlight fixed on 1P
		if activeConfig.mountAISlotToP2 then
			highlightedPlayer = 0
		end

		if selectedSongNode ~= nil then
			loadDiffBars(selectedSongNode)
			stopHold()
			activeScreen = "transition"
			diffIndex = {0, 0, 0, 0, 0}
			diffSelected = {false, false, false, false, false}
			startCounter("screen_transition", 0, 1920, 0.5/1920, "none", updateTransitionVisuals, function()
				activeScreen = "difficultyselect"
			end)
		end

	elseif activeScreen == "difficultyselect" then
		local allDiffsSelected = true
		local canceled = false

		for i = 1, CONFIG.PlayerCount, 1 do
			-- In AI battle: player 2 (AI) automatically mirrors player 1's selection
			if activeConfig.mountAISlotToP2 and i == 2 then
				diffIndex[2]   = diffIndex[1]
				diffSelected[2] = diffSelected[1]
			else
			local inpset = inputSets[i]

			if diffSelected[i] == false then
				if INPUT:Pressed(inpset.right) or (i == 1 and INPUT:KeyboardPressed("RightArrow")) then
					sounds.Skip:Play()
					diffIndex[i] = (diffIndex[i] + 1) % (3 + #diffBars)
				elseif INPUT:Pressed(inpset.left) or (i == 1 and INPUT:KeyboardPressed("LeftArrow")) then
					sounds.Skip:Play()
					diffIndex[i] = (diffIndex[i] - 1) % (3 + #diffBars)
				elseif INPUT:Pressed(inpset.decide1) or INPUT:Pressed(inpset.decide2) or (i == 1 and INPUT:KeyboardPressed("Return")) then
					if diffIndex[i] == 0 then
						sounds.Cancel:Play()
						canceled = true
					elseif diffIndex[i] == 1 then
						act_inner["customize_dialog"]:Activate(i - 1)
						return nil
					elseif diffIndex[i] == 2 then
						act_inner["mod_select_dialog"]:Activate(i - 1)
						return nil
					elseif diffIndex[i] > 2 then
						sounds.Decide:Play()
						diffSelected[i] = true
					end
				elseif INPUT:Pressed(inpset.cancel) or (i == 1 and INPUT:KeyboardPressed("Escape")) then
					sounds.Cancel:Play()
					if diffSelected[i] == true then
						diffSelected[i] = false
					else
						canceled = true
					end
				end
			end -- if diffSelected[i] == false
			end -- else: normal player input

			if diffSelected[i] == false then allDiffsSelected = false end
		end

		if INPUT:KeyboardPressed("F3") then
			sounds.Decide:Play()
			CONFIG:SetAutoStatus(0, not CONFIG:GetAutoStatus(0))
		end
		if INPUT:KeyboardPressed("F4") and CONFIG.PlayerCount >= 2 then
			sounds.Decide:Play()
			CONFIG:SetAutoStatus(1, not CONFIG:GetAutoStatus(1))
		end

		if canceled or INPUT:KeyboardPressed("Escape") then
			sounds.Decide:Play()
			activeScreen = "transition"
			startCounter("screen_transition", 1920, 0, -0.5/1920, "none", updateTransitionVisuals, function()
				activeScreen = "songselect"
			end)
		elseif allDiffsSelected == true then
			local success = selectedSongNode:Mount(
				(diffIndex[1] >= 3) and diffBars[diffIndex[1] - 2].difficulty or 0,
				(diffIndex[2] >= 3) and diffBars[diffIndex[2] - 2].difficulty or 0,
				(diffIndex[3] >= 3) and diffBars[diffIndex[3] - 2].difficulty or 0,
				(diffIndex[4] >= 3) and diffBars[diffIndex[4] - 2].difficulty or 0,
				(diffIndex[5] >= 3) and diffBars[diffIndex[5] - 2].difficulty or 0
			)
			if success then
				lastSignal = "play"
				return "play"
			else
				diffSelected = {false, false, false, false, false}
			end
		end

		-- AI level slider: 2P blue drums adjust starting AI level (AI battle only)
		if activeConfig.mountAISlotToP2 then
			if INPUT:Pressed("LBlue2P") and CONFIG.AILevel > 1 then
				CONFIG.AILevel = CONFIG.AILevel - 1
				sounds.Skip:Play()
			elseif INPUT:Pressed("RBlue2P") and CONFIG.AILevel < 10 then
				CONFIG.AILevel = CONFIG.AILevel + 1
				sounds.Skip:Play()
			end
		end
	end

	return nil
end

-- activate(allowPlayerCount, lockedPlayerCount, mountAISlotToP2)
--   allowPlayerCount  : false to disable the L-key player count toggle (nil = true = allowed)
--   lockedPlayerCount : number to force CONFIG.PlayerCount each frame (nil = no lock)
--   mountAISlotToP2   : true to mount the AI virtual slot onto spot 2 (nil/false = no)
function activate(allowPlayerCount, lockedPlayerCount, mountAISlotToP2)
	activeConfig = {
		allowPlayerCount  = allowPlayerCount,
		lockedPlayerCount = lockedPlayerCount,
		mountAISlotToP2   = mountAISlotToP2 == true,
	}

	-- Force player count if locked
	if activeConfig.lockedPlayerCount ~= nil then
		CONFIG.PlayerCount = math.tointeger(activeConfig.lockedPlayerCount)
	end

	-- Mount the AI virtual slot onto player spot 2
	if activeConfig.mountAISlotToP2 then
		VIRTUALSLOTS:MountSlot(2, "AI")
	end

	-- Inner activities (dialog modals)
	local activities = {"mod_select_dialog", "customize_dialog"}
	for _, at in ipairs(activities) do
		act_inner[at] = ACTIVITY:GetActivity(at)
	end

	modicons_ro = ROACTIVITY:GetROActivity("modicons")
	if modicons_ro ~= nil then modicons_ro:Activate() end

	sounds.Skip     = SHARED:GetSharedSound("Skip")
	sounds.Cancel   = SHARED:GetSharedSound("Cancel")
	sounds.Decide   = SHARED:GetSharedSound("Decide")
	sounds.SongDecide = SHARED:GetSharedSound("SongDecide")

	resetToSongSelect()

	-- Restore default background; parent stage may override this after activate() returns
	currentBackground = 0
	SHARED:SetSharedTexture("background", "Textures/bg0.png")

	-- Refresh page display if we already have a song list (returning from a play)
	if songList ~= nil then refreshPage() end

	-- Counters
	startCounter("background", 1920, 0, 1/48, "loop", function(val)
		backgroundScrollX = val
	end)

	startCounter("extreme_fade", 2000, 0, 1/400, "loop", function(val)
		local fadeIn = val - 745
		local fadeOut = 2000 - val
		difficultyFade4 = math.max(0, math.min(255, fadeIn, fadeOut))
	end)

	startCounter("selectbox_animation", 2000, 0, 1/600, "loop", function(val)
		if bars["selected"] then
			local n = 1.01 + (math.sin(val * (math.pi * 2 / 2000)) * 0.01)
			bars["selected"]:SetScale(n, n)
		end
	end)

	startCounter("arrows_animation", 10, 0, 1/10, "bounce", function(val)
		arrowsDistance = val
	end)

	startCounter("leveltag_animation", SONGBAR_LEVEL_EX_ANIMATION_FRAMECOUNT, 0,
		1/SONGBAR_LEVEL_EX_ANIMATION_FRAMECOUNT*2, "loop", function(val)
		levelLabelFrame = math.floor(val)
	end)

	startCounter("load_animation", 0, 360, 2/300, "loop", function(val)
		if bgtx["load"] ~= nil then bgtx["load"]:SetRotation(val) end
	end)

	startCounter("puchi_sine", 0, 360, 1/120, "loop", function(val)
		puchiSineY = math.sin(val * math.pi / 180) * PUCHI_FLOAT_AMP
	end)

	for p = 0, 4, 1 do
		local chara = GetSaveFile(p):GetCharacter()
		if chara ~= nil and chara.IsValid then
			chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)
		end
	end
end

function deactivate()
	-- Restore AI slot mount only when not heading to a play (preserve it for gameplay)
	if activeConfig.mountAISlotToP2 and lastSignal ~= "play" then
		VIRTUALSLOTS:MountSlot(2, "2P")
	end
	lastSignal = nil

	-- Clear all counters
	for k, _ in pairs(ctx) do
		ctx[k] = COUNTER:EmptyCounter()
	end

	SHARED:GetSharedSound("presound"):Stop()

	for p = 0, 4, 1 do
		local chara = GetSaveFile(p):GetCharacter()
		if chara ~= nil and chara.IsValid then
			chara:DisposeAnimation(CHARACTER.ANIM_MENU_NORMAL)
		end
	end
end

function onStart()
	textSmall = TEXT:Create(18)
	text = TEXT:Create(28)
	textLarge = TEXT:Create(40)

	SHARED:SetSharedTexture("background", "Textures/bg0.png")
	bgtx["load"] = TEXTURE:CreateTexture("Textures/load.png")
	bgtx["preimage_load"] = TEXTURE:CreateTexture("Textures/preimage_load.png")
	bgtx["overlay"] = TEXTURE:CreateTexture("Textures/bg_overlay.png")
	bgtx["overlay_difficulty"] = TEXTURE:CreateTexture("Textures/bg_overlay_difficulty.png")
	bgtx["songinfo"] = TEXTURE:CreateTexture("Textures/bg_songinfo.png")
	bgtx["randominfo"] = TEXTURE:CreateTexture("Textures/bg_randominfo.png")
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
		bgtx["sinfo_difficulties_"..i.."_plus"] = TEXTURE:CreateTexture("Textures/sinfo_difficulties_0_plus.png")
	end
	for i = 0, 9, 1 do
		bgtx["levellabelsborder"..i] = TEXTURE:CreateTexture("Textures/BarLevelBorder/"..i..".png")
		bgtx["levellabels"..i] = TEXTURE:CreateTexture("Textures/BarLevel/"..i..".png")
		bgtx["sinfo_level"..i] = TEXTURE:CreateTexture("Textures/SinfoLevel/"..i..".png")
		bgtx["diffsel_level"..i] = TEXTURE:CreateTexture("Textures/DifficultyBars/Level/"..i..".png")
		bgtx["diffsel_levelcol"..i] = TEXTURE:CreateTexture("Textures/DifficultyBars/LevelCol/"..i..".png")
	end

	bgtx["placeholder_chara"] = TEXTURE:CreateTexture("Textures/placeholder_chara.png")
	bgtx["placeholder_portrait"] = TEXTURE:CreateTexture("Textures/placeholder_portrait.png")

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
	for i = 1, 5, 1 do
		bars["difficultybarselect"..i] = TEXTURE:CreateTexture("Textures/DifficultyBars/P"..i..".png")
	end
	for i = 2, 7, 1 do
		bars["difficultybar"..i] = TEXTURE:CreateTexture("Textures/DifficultyBars/"..i..".png")
	end
	bars["smallbar0"] = TEXTURE:CreateTexture("Textures/DifficultyBars/0.png")
	bars["smallbar1"] = TEXTURE:CreateTexture("Textures/DifficultyBars/Customize.png")
	bars["smallbar2"] = TEXTURE:CreateTexture("Textures/DifficultyBars/1.png")
	for i = 1, 7, 1 do
		bars["difficultybarlevel"..i] = TEXTURE:CreateTexture("Textures/DifficultyBars/Diff"..i..".png")
	end

	favoriteicon = TEXTURE:CreateTexture("Textures/fav.png")
	genre_overlays = {}
end

function afterSongEnum()
	local lsls = GenerateSongListSettings()
	lsls:SetExcludedGenreFolders({"段位道場", "太鼓タワー"})
	lsls.ModuloPagination = false
	lsls.HideEmptyFolders = true
	lsls.FlattenOpenedFolders = false
	songList = RequestSongList(lsls)
	refreshPage()
end

function onDestroy()
	if text ~= nil then text:Dispose() end
	if textSmall ~= nil then textSmall:Dispose() end
	if textLarge ~= nil then textLarge:Dispose() end
	if favoriteicon ~= nil then favoriteicon:Dispose() end
	for _, bar in pairs(bars) do bar:Dispose() end
	for _, bg in pairs(bgtx) do bg:Dispose() end
	for _, overlay in pairs(genre_overlays) do overlay:Dispose() end
end
