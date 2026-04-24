-- ai_battle_song_select
-- Song select for AI battle mode.
-- Player count is forced to 2 via CONFIG.IsAIBattleMode.
-- The AI virtual slot is mounted onto player spot 2.
-- Background: BG_Space.png (scrolling) + BG_Frame.png (static frame, between BG and UI).

local act = nil
local bgFrame = nil
local exitingToPlay = false

function onStart()
	bgFrame = TEXTURE:CreateTexture("Textures/BG_Frame.png")
end

function activate()
	if act == nil then act = ACTIVITY:GetActivity("song_select_core") end
	exitingToPlay = false
	CONFIG.IsAIBattleMode = true
	-- PlayerCount now returns 2 (forced by IsAIBattleMode); lock it and mount AI slot.
	act:Activate(false, CONFIG.PlayerCount, true)
	-- Override the scrolling background set by the activity.
	SHARED:SetSharedTexture("background", "Textures/BG_Space.png")
end

function deactivate()
	if not exitingToPlay then
		CONFIG.IsAIBattleMode = false
	end
	act:Deactivate()
end

local function getSignal(result)
	if type(result) == "string" then return result end
	if result == nil then return nil end
	local ok, val = pcall(function() return result[0] end)
	return ok and val or nil
end

function update()
	local signal = getSignal(act:Update())
	if signal == "play" then
		exitingToPlay = true
		return Exit("play", nil)
	end
	if signal == "cancel" then return Exit("title", nil) end
end

function draw()
	-- Draw scrolling BG_Space only (no UI elements yet)
	act:Draw("bg_only")
	-- Static frame sits just above the scrolling background
	bgFrame:Draw(0, 0)
	-- Draw all song select UI on top
	act:Draw("no_bg")
end

function onDestroy()
	if bgFrame ~= nil then bgFrame:Dispose() end
end
