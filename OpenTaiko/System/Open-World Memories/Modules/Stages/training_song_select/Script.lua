---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- training_song_select: training mode song select, 1P only
-- IsTrainingMode set on activate; cleared on deactivate unless heading into a play, and on cancel

local act = nil
local exitingToPlay = false

function onStart()
end

function activate()
	if act == nil then act = ACTIVITY:GetActivity("song_select_core") end
	exitingToPlay = false
	CONFIG.IsTrainingMode = true
	act:Activate(false, 1, false, false, "shared")  -- no player count toggle, locked to 1P, no AI slot
	-- "shared": the activity scrolls this texture instead of its per-genre sets
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
	if signal == "cancel" then return Exit("title", nil, "pill_clouds_back") end
end

function draw()
	act:Draw()
end
