-- regular_song_select: wrapper around song_select_core

local act = nil

function onStart()
end

function activate()
	if act == nil then act = ACTIVITY:GetActivity("song_select_core") end
	act:Activate()  -- defaults: allow player count, no lock, no AI slot
end

function deactivate()
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
	if signal == "play"   then return Exit("play", nil) end
	if signal == "cancel" then return Exit("title", nil, "pill_clouds_back") end
	if signal == "thanks" then return Exit("stage", "event_thanks") end   -- Event Mode: the last play is done
end

function draw()
	act:Draw()
end
