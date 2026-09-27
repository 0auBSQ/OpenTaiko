---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- TransitionArt: a transition makes its textures when a trip starts (its first fadeOut call) and
-- disposes them when the trip ends (fadeIn at t = 1), so it holds no texture memory between trips.
-- The textures load in the background: the fade-out waits at its start until they can be drawn,
-- then plays in the time left, so it still ends on time.

local TA = {}
TA.__index = TA

function TA.new()
	return setmetatable({ from = nil }, TA)
end

-- a new trip: wait for the textures again
function TA:reset()
	self.from = nil
end

-- fade-out progress for the animation: 0 while something given is still loading (a texture Loaded but not
-- Ready, or false), then 0 -> 1 in the time left
function TA:progress(t, ...)
	if t >= 1 then return 1 end
	if self.from == nil then
		for i = 1, select("#", ...) do
			local x = select(i, ...)
			if x == false then return 0 end
			if x ~= nil and x ~= true and x.Loaded and not x.Ready then return 0 end
		end
		self.from = t
	end
	return (t - self.from) / (1 - self.from)
end

return TA
