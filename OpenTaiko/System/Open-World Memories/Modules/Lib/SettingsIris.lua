---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- SettingsIris: shared drawing for settings_iris (title -> settings) and settings_iris_back.
-- The settings screen: gray, with connected L lines scrolling left and the crossed tools in the middle.
-- It opens and closes as a circle at the screen centre.

local SI = {}
SI.__index = SI

SI.OUT_SECONDS = 0.6
SI.IN_SECONDS = 0.6

local SCREEN_W, SCREEN_H = 1920, 1080
local CX, CY = SCREEN_W / 2, SCREEN_H / 2
local R_FULL = math.sqrt(CX * CX + CY * CY) + 14   -- past the corners, ring included
local SCROLL = 60                  -- px/s the lines move left
local TOOLS_HALF = 288             -- half of Tools.png
local RING_R = 502                 -- Ring.png: radius of its line (1024 px image)
local EDGE_ERR = 1.5               -- px: largest step of the circle's edge (it is built from clipped rows)
local MAX_ROW = 8

local function clamp01(x) return x < 0 and 0 or (x > 1 and 1 or x) end
local function ease(x) return x < 0.5 and 4 * x * x * x or 1 - (-2 * x + 2) ^ 3 / 2 end

local function frameDt()
	local ok, d = pcall(function() return fps.deltaTime end)
	d = ok and tonumber(d) or 0
	return d < 0 and 0 or (d > 0.1 and 0.1 or d)
end

-- way: "in" (title -> settings) or "back"
function SI.new(way)
	return setmetatable({ way = way, clock = 0, lastPhase = nil }, SI)
end

function SI:load()
	self.pattern = TEXTURE:CreateTexture("Textures/Pattern.png")
	self.tools = TEXTURE:CreateTexture("Textures/Tools.png")
	self.ring = TEXTURE:CreateTexture("Textures/Ring.png")
	self.sndZoom = SOUND:CreateSFX("Sounds/Zoom.ogg")
end

function SI:dispose()
	for _, k in ipairs({ "pattern", "tools", "ring", "sndZoom" }) do
		if self[k] then self[k]:Dispose(); self[k] = nil end
	end
end

-- the screen inside one clip rectangle (the tools only where the rectangle meets them)
function SI:drawIn(x, y, w, h, toolsScale)
	if w <= 0 or h <= 0 then return end
	GRAPHICS:SetClip(x, y, w, h)
	local p = self.pattern
	if p ~= nil and p.Loaded then
		local shift = (self.clock * SCROLL) % p.Width
		local whole = math.floor(shift)
		p:DrawRect(whole - shift, 0, whole, 0, SCREEN_W + 1, SCREEN_H)   -- the texture repeats past its size
	end
	local t = self.tools
	local half = TOOLS_HALF * toolsScale
	if t ~= nil and t.Loaded and x < CX + half and x + w > CX - half and y < CY + half and y + h > CY - half then
		t:SetScale(toolsScale, toolsScale)
		t:DrawAtAnchor(CX, CY, "center")
		t:SetScale(1, 1)
	end
end

-- the circle is drawn as clipped rows: thin where its edge is steep, so each step stays small
local function rowHeight(y, r)
	local dy = math.abs(y - CY)
	local half = math.sqrt(math.max(0, r * r - dy * dy))
	if dy < 1 then return MAX_ROW end
	return math.max(1, math.min(MAX_ROW, math.floor(EDGE_ERR * half / dy)))
end

local function chord(y, h, r)
	local dy = y + h / 2 - CY
	return math.sqrt(math.max(0, r * r - dy * dy))
end

function SI:drawRing(r)
	local ring = self.ring
	local a = clamp01((r - 16) / 48)   -- shrunk this far the thin line would break into dots, so it fades
	if ring == nil or not ring.Loaded or a <= 0 then return end
	local s = r / RING_R
	ring:SetScale(s, s); ring:SetOpacity(a)
	ring:DrawAtAnchor(CX, CY, "center")
	ring:SetScale(1, 1); ring:SetOpacity(1)
end

-- the screen inside the circle of radius r
function SI:drawDisc(r, toolsScale)
	if r <= 0 then return end
	if r >= R_FULL then
		self:drawIn(0, 0, SCREEN_W, SCREEN_H, toolsScale)
		GRAPHICS:ClearClip()
		return
	end
	local y = math.max(0, math.floor(CY - r))
	local bottom = math.min(SCREEN_H, math.ceil(CY + r))
	while y < bottom do
		local h = math.min(rowHeight(y, r), bottom - y)
		local half = chord(y, h, r)
		if half >= 0.5 then self:drawIn(math.floor(CX - half + 0.5), y, math.floor(2 * half + 0.5), h, toolsScale) end
		y = y + h
	end
	GRAPHICS:ClearClip()
	self:drawRing(r)
end

-- the screen outside the circle of radius r (a see-through hole in the middle)
function SI:drawHole(r, toolsScale)
	if r >= R_FULL then return end
	if r <= 0 then
		self:drawIn(0, 0, SCREEN_W, SCREEN_H, toolsScale)
		GRAPHICS:ClearClip()
		return
	end
	local top = math.max(0, math.floor(CY - r))
	local bottom = math.min(SCREEN_H, math.ceil(CY + r))
	self:drawIn(0, 0, SCREEN_W, top, toolsScale)
	self:drawIn(0, bottom, SCREEN_W, SCREEN_H - bottom, toolsScale)
	local y = top
	while y < bottom do
		local h = math.min(rowHeight(y, r), bottom - y)
		local half = chord(y, h, r)
		local x0, x1 = math.floor(CX - half + 0.5), math.floor(CX + half + 0.5)
		self:drawIn(0, y, x0, h, toolsScale)
		self:drawIn(x1, y, SCREEN_W - x1, h, toolsScale)
		y = y + h
	end
	GRAPHICS:ClearClip()
	self:drawRing(r)
end

local function play(snd) if snd then snd:Play() end end

-- a zoom sound as each circle starts moving
function SI:enter(phase)
	if self.lastPhase ~= phase then
		self.lastPhase = phase
		if phase ~= "load" then play(self.sndZoom) end
	end
end

-- way in: the screen grows as a circle over the title / way back: a hole in it shrinks over the settings
function SI:fadeOut(t)
	self.clock = self.clock + frameDt()
	self:enter("out")
	local e = ease(clamp01(t))
	if self.way == "in" then self:drawDisc(R_FULL * e, 0.6 + 0.4 * e)
	else self:drawHole(R_FULL * (1 - e), 1) end
end

function SI:loading()
	self.clock = self.clock + frameDt()
	self:enter("load")
	self:drawIn(0, 0, SCREEN_W, SCREEN_H, 1)
	GRAPHICS:ClearClip()
end

-- way in: a hole grows to show the settings / way back: the screen shrinks as a circle into the title
function SI:fadeIn(t)
	self.clock = self.clock + frameDt()
	self:enter("in")
	local e = ease(clamp01(t))
	if self.way == "in" then self:drawHole(R_FULL * e, 1)
	else self:drawDisc(R_FULL * (1 - e), 1 - 0.4 * e) end
end

return SI
