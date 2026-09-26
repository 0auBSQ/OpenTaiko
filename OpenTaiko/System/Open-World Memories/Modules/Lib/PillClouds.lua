---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- PillClouds: shared drawing for the pill_clouds transitions.
-- Four tilted pills rise over the screen, then a cloud bank rises over them.

local PC = {}
PC.__index = PC

PC.PILLS_OUT_SECONDS = 1.5               -- pills, then clouds
PC.CLOUDS_OUT_SECONDS = 0.9              -- clouds only
PC.IN_SECONDS = 1.0

local SCREEN_W, SCREEN_H = 1920, 1080
local TILT = 15                          -- degrees; everything moves up along this lean
local SIN, COS = math.sin(math.rad(TILT)), math.cos(math.rad(TILT))
local REACH = 770                        -- farthest screen corner along the travel axis

local PILL_W, PILL_L = 600, 2160
local PILL_PX_W, PILL_PX_L = 300, 1080   -- the capsule in Pill.png, inside its clear border
local PILL_STEP = 560                    -- neighbours overlap by 40 px
local PILL_START = -(REACH + PILL_L / 2 + 40)   -- below the screen
local ARRIVE = 0.4                       -- fade-out progress when the pills cover the screen
local CREEP = 60                         -- then they drift this far (px)
local CLOUDS_FROM = 0.45                 -- fade-out progress when the clouds start

local EDGE_W, EDGE_H = 1024, 384         -- CloudEdge.png size; it repeats across
local TIPS, SOLID, BASE = 47, 183, 382   -- CloudEdge.png rows: first puff, first all body colour, clear border
local STRIP_W = 2400                     -- the edge is one quad this long, its ends off screen
local BURY = 8                           -- the body ends this far under the edge's solid rows
local BODY_PX = 8                        -- CloudBody.png size
local BODY_W = 2800
local FRONT_START = -REACH - 10 + TIPS   -- puffs just below the screen
local FRONT_END = REACH + 10 + SOLID     -- screen fully covered
local BACK_START = -REACH - 10 - SOLID   -- fade-in: the bank's back, from below the screen
local BACK_END = REACH + 10 - TIPS       -- to past the top
local DRIFT = 25                         -- px/s the puffs slide across

local function clamp01(x) return x < 0 and 0 or (x > 1 and 1 or x) end
local function easeInOut(x) return x < 0.5 and 4 * x * x * x or 1 - (-2 * x + 2) ^ 3 / 2 end

local function frameDt()
	local ok, d = pcall(function() return fps.deltaTime end)
	d = ok and tonumber(d) or 0
	return d < 0 and 0 or (d > 0.1 and 0.1 or d)
end

-- screen point from the centre, `along` and `across` the travel axis
local function place(along, across)
	return SCREEN_W / 2 + along * SIN + across * COS, SCREEN_H / 2 - along * COS + across * SIN
end

-- cols: four { r, g, b } pill colours (0-1), or nil for clouds only
function PC.new(cols)
	return setmetatable({ cols = cols, clock = 0, phase = nil }, PC)
end

function PC:load()
	if self.cols then
		self.pill = TEXTURE:CreateTexture("Textures/Pill.png")
		self.shine = TEXTURE:CreateTexture("Textures/PillShine.png")
	end
	self.edge = TEXTURE:CreateTexture("Textures/CloudEdge.png")
	self.edge:SetWrapMode("Repeat")
	self.body = TEXTURE:CreateTexture("Textures/CloudBody.png")
	self.sndWind = SOUND:CreateSFX("Sounds/Wind.ogg")
end

local function play(snd) if snd then snd:Play() end end

function PC:dispose()
	for _, k in ipairs({ "pill", "shine", "edge", "body", "sndWind" }) do
		if self[k] then self[k]:Dispose(); self[k] = nil end
	end
end

function PC:drawPills(along)
	local pill, shine = self.pill, self.shine
	if pill == nil or shine == nil then return end
	for _, tex in ipairs({ pill, shine }) do
		tex:SetScale(PILL_W / PILL_PX_W, PILL_L / PILL_PX_L)
		tex:SetRotation(-TILT)                                      -- tops lean right
	end
	for i, c in ipairs(self.cols) do
		local x, y = place(along, (i - 2.5) * PILL_STEP)
		pill:SetColor(c[1], c[2], c[3])
		pill:DrawAtAnchor(x, y, "center")
		shine:DrawAtAnchor(x, y, "center")                          -- untinted gloss
	end
	for _, tex in ipairs({ pill, shine }) do tex:SetScale(1, 1); tex:SetRotation(0) end
	pill:SetColor(1, 1, 1)
end

-- the bank's body, from `lo` to `hi` along the travel axis
function PC:drawBody(lo, hi)
	lo, hi = math.max(lo, -REACH - 60), math.min(hi, REACH + 60)
	if hi <= lo or self.body == nil then return end
	local x, y = place((lo + hi) / 2, 0)
	self.body:SetScale(BODY_W / BODY_PX, (hi - lo) / BODY_PX)
	self.body:SetRotation(-TILT)
	self.body:DrawAtAnchor(x, y, "center")
	self.body:SetScale(1, 1); self.body:SetRotation(0)
end

-- a row of puffs facing forward (front) or back; `mid` is its centre along the travel axis.
-- One long quad: the texture repeats across, and its clear top and bottom rows keep the long sides soft.
function PC:drawEdge(mid, front)
	local edge = self.edge
	if edge == nil or mid < -REACH - EDGE_H or mid > REACH + EDGE_H then return end
	local shift = (self.clock * DRIFT) % EDGE_W                   -- the puffs slide right
	local whole = math.floor(shift)
	local x, y = place(mid, shift - whole)                         -- the part under a pixel moves the quad
	edge:SetRotation(front and -TILT or 180 - TILT)
	edge:DrawRectAtAnchor(x, y, front and -whole or whole, 0, STRIP_W, EDGE_H, "center")
	edge:SetRotation(0)
end

-- the bank below its front row; row r of the edge sits at `front - r`
function PC:drawFront(front)
	self:drawBody(-100000, front - BASE + BURY)
	self:drawEdge(front - EDGE_H / 2, true)
end

-- the bank above its back row; row r of the edge sits at `back + r`
function PC:drawBack(back)
	self:drawBody(back + BASE - BURY, 100000)
	self:drawEdge(back + EDGE_H / 2, false)
end

function PC:fadeOut(t)
	self.clock = self.clock + frameDt()
	if self.phase ~= "out" then                                  -- a new trip
		self.phase, self.cloudsIn = "out", false
	end
	local k = t
	if self.cols then
		local along = t < ARRIVE and PILL_START * (1 - t / ARRIVE) ^ 3 or CREEP * (t - ARRIVE) / (1 - ARRIVE)
		self:drawPills(along)
		k = clamp01((t - CLOUDS_FROM) / (1 - CLOUDS_FROM))
	end
	if k > 0 then
		if not self.cloudsIn then self.cloudsIn = true; play(self.sndWind) end
		self:drawFront(FRONT_START + (FRONT_END - FRONT_START) * easeInOut(k))
	end
end

function PC:loading()
	self.phase = "load"
	self.clock = self.clock + frameDt()
	self:drawFront(FRONT_END)
end

function PC:fadeIn(t)
	self.phase = "in"
	self.clock = self.clock + frameDt()
	self:drawBack(BACK_START + (BACK_END - BACK_START) * easeInOut(t))
end

return PC
