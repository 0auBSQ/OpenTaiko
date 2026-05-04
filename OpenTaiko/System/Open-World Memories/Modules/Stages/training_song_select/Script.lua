---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- training_song_select
-- Song select for training mode.
-- Player count is locked to 1P.
-- Training mode is enabled on activate and only disabled on deactivate when NOT heading into a play
-- (so the game stage receives IsTrainingMode = true). On cancel it is cleared before returning.

local act = nil
local exitingToPlay = false

function onStart()
end

function activate()
	if act == nil then act = ACTIVITY:GetActivity("song_select_core") end
	exitingToPlay = false
	CONFIG.IsTrainingMode = true
	act:Activate(false, 1)  -- no player count toggle, locked to 1P, no AI slot
	-- Override the background with the training-specific scrolling BG
	SHARED:SetSharedTexture("background", "Textures/BG.png")
end

function deactivate()
	if not exitingToPlay then
		CONFIG.IsTrainingMode = false
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
	act:Draw()
end
