---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- transition textures per trip: made on the first fadeOut, freed at fadeIn t = 1
-- fade-out waits for the textures, then plays in the time left

local TA = {}
TA.__index = TA

function TA.new()
	return setmetatable({ from = nil }, TA)
end

-- new trip: wait for the textures again
function TA:reset()
	self.from = nil
end

-- fade-out progress: 0 while a given texture is loading (Loaded but not Ready, or false)
-- then 0 -> 1 in the time left
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
