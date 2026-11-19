-- ai_battle_song_select: song select for AI battle mode.

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
	-- PlayerCount is 2 (IsAIBattleMode): lock it, AI slot on spot 2
	act:Activate(false, CONFIG.PlayerCount, true, false, "shared")
	-- "shared": the activity scrolls this texture instead of its per-genre sets
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
	if signal == "cancel" then return Exit("title", nil, "ai_link_back") end
	if signal == "thanks" then return Exit("stage", "event_thanks") end   -- Event Mode: the last play is done
end

function draw()
	-- BG_Space only
	act:Draw("bg_only")
	-- BG_Frame between the background and the UI
	bgFrame:Draw(0, 0)
	-- song select UI
	act:Draw("no_bg")
end

function onDestroy()
	if bgFrame ~= nil then bgFrame:Dispose() end
end
