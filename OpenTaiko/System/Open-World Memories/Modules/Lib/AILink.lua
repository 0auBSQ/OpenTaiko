---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- AILink.lua: the AI Battle transitions (ai_link, ai_link_back).
-- A chip lights up on a dark screen, traces run from its pins to the screen edges,
-- then the status box at the bottom types its line.

local AI = {}
AI.__index = AI

AI.OUT_SECONDS = 1.5
AI.IN_SECONDS = 0.7

local SCREEN_W, SCREEN_H = 1920, 1080
local BG = { 0.03, 0.06, 0.10 }
local CHIP_FILL = { 0.05, 0.12, 0.18 }
local CYAN = { 0.25, 0.90, 1.00 }
local TIP = { 0.80, 1.00, 1.00 }
local BG_IN = 0.3                        -- fade-out part for the dark screen
local CHIP_AT, CHIP_IN = 0.08, 0.14      -- the chip lights up here...
local GROW_AT = 0.2                      -- ...then the traces grow until the box opens
local BOX_AT, BOX_OPEN = 0.7, 0.12
local TYPE_AT, TYPE_END = 0.8, 0.98      -- the box's line types in
local BOX_MIN_W, BOX_PAD, BOX_H, BOX_Y = 560, 90, 104, 930
local FRAME = 3
local FONT_SIZE = 40
local TEXT_PAD = 25                      -- glyph box side padding
local PULSE_SPEED = 700                  -- px/s

local CHIP_X, CHIP_Y, CHIP_HALF = SCREEN_W / 2, 430, 125
local PINS, PIN_GAP, PIN_LEN, PIN_W = 8, 26, 14, 6       -- per side
local DIE = 40                           -- inner square, this far inside the chip
local SIDE_MID, SIDE_SPREAD = 540, 5.6   -- left and right traces end in rows spread over the screen height
local END_SPREAD = 3.2                   -- top and bottom traces spread less
local RUN_SIDE, RUN_END = 30, 20         -- straight part before the 45° bend

local function clamp01(x) return x < 0 and 0 or (x > 1 and 1 or x) end
local function easeOut(x) return 1 - (1 - x) ^ 3 end
local function easeInOut(x) return x < 0.5 and 4 * x * x * x or 1 - (-2 * x + 2) ^ 3 / 2 end
local function rnd(i, k) local x = math.sin(i * 12.9898 + k * 78.233) * 43758.5453; return x - math.floor(x) end

local function frameDt()
	local ok, d = pcall(function() return fps.deltaTime end)
	d = ok and tonumber(d) or 0
	return d < 0 and 0 or (d > 0.1 and 0.1 or d)
end

-- offset of pin i from the middle of its side
local function pinOffset(i) return (i - (PINS + 1) / 2) * PIN_GAP end

-- From each pin: a short straight part, a 45° bend that spreads the traces apart, then straight on
-- to the screen edge or to a via. All bends on a side start together, so the traces never cross.
local function buildTraces()
	local traces = {}
	local function add(pts, via)
		local segs, total = {}, 0
		for k = 1, #pts - 1 do
			local a, b = pts[k], pts[k + 1]
			local len = math.sqrt((b[1] - a[1]) ^ 2 + (b[2] - a[2]) ^ 2)
			segs[#segs + 1] = { a = a, b = b, len = len, from = total }
			total = total + len
		end
		traces[#traces + 1] = { pts = pts, segs = segs, len = total, via = via, pulse = rnd(#traces + 1, 9) }
	end
	-- the last part: `room` px to past the edge, in direction (dx, dy)
	local function finish(pts, dx, dy, room, viaOk)
		local id = #traces + 1
		local via = viaOk and room > 240 and rnd(id, 5) < 0.3
		local run = via and 100 + (room - 240) * rnd(id, 6) or room
		local p = pts[#pts]
		pts[#pts + 1] = { p[1] + dx * run, p[2] + dy * run }
		add(pts, via)
	end

	local tip = CHIP_HALF + PIN_LEN
	for i = 1, PINS do
		local o = pinOffset(i)
		for _, dir in ipairs({ -1, 1 }) do
			local x, y = CHIP_X + dir * tip, CHIP_Y + o                -- left and right
			local row = SIDE_MID + o * SIDE_SPREAD
			local bx = x + dir * RUN_SIDE
			local ex = bx + dir * math.abs(row - y)
			finish({ { x, y }, { bx, y }, { ex, row } }, dir, 0, dir > 0 and SCREEN_W + 20 - ex or ex + 20, true)

			x, y = CHIP_X + o, CHIP_Y + dir * tip                       -- top and bottom
			local col = CHIP_X + o * END_SPREAD
			local by = y + dir * RUN_END
			local ey = by + dir * math.abs(col - x)
			finish({ { x, y }, { x, by }, { col, ey } }, 0, dir, dir > 0 and SCREEN_H + 20 - ey or ey + 20, dir < 0)
		end
	end

	local longest = 0
	for _, w in ipairs(traces) do longest = math.max(longest, w.len) end
	return traces, longest
end

local TRACES, LONGEST = buildTraces()

local function pointAt(w, dist)
	for _, s in ipairs(w.segs) do
		if dist <= s.from + s.len then
			local k = s.len > 0 and (dist - s.from) / s.len or 0
			return s.a[1] + (s.b[1] - s.a[1]) * k, s.a[2] + (s.b[2] - s.a[2]) * k
		end
	end
	local p = w.pts[#w.pts]
	return p[1], p[2]
end

-- key, fallback: the skin string of the status line and its English text
function AI.new(key, fallback)
	return setmetatable({ key = key, fallback = fallback, clock = 0, phase = nil }, AI)
end

function AI:load()
	self.px = TEXTURE:CreateTexture("Textures/Pixel.png")
	self.line = TEXTURE:CreateTexture("Textures/Line.png")
	self.dot = TEXTURE:CreateTexture("Textures/Dot.png")
	self.ring = TEXTURE:CreateTexture("Textures/Ring.png")
	self.sndWires = SOUND:CreateSFX("Sounds/Wires.ogg")
	self.sndBeep = SOUND:CreateSFX("Sounds/Beep.ogg")
	self.sndTyping = SOUND:CreateSFX("Sounds/Typing.ogg")
	self.font = TEXT:CreateGlyphCached(FONT_SIZE)
	self.nudge = math.floor((self.font.BoxHeight - math.ceil(self.font.LineHeight)) / 2) - 1
	self.colText = COLOR:CreateColorFromRGBA(90, 232, 255, 255)
	self.colClear = COLOR:CreateColorFromRGBA(0, 0, 0, 0)
end

function AI:dispose()
	for _, k in ipairs({ "px", "line", "dot", "ring", "sndWires", "sndBeep", "sndTyping", "font" }) do
		if self[k] then self[k]:Dispose(); self[k] = nil end
	end
end

local function play(snd) if snd then snd:Play() end end

-- read the status line again each trip, in case the language changed
function AI:status()
	local ok, s = pcall(function() return THEME:GetSkinString(self.key) end)
	if not (ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[") then s = self.fallback end
	local chars = {}
	for ch in s:gmatch("[%z\1-\127\194-\244][\128-\191]*") do chars[#chars + 1] = ch end
	self.chars, self.textW = chars, self.font and self.font:Measure(s) or 0
	self.boxW = math.max(BOX_MIN_W, self.textW + 2 * BOX_PAD)
end

function AI:rect(x, y, w, h, c, a)
	if self.px == nil or a <= 0 or w <= 0 or h <= 0 then return end
	self.px:SetColor(c[1], c[2], c[3]); self.px:SetOpacity(a)
	self.px:SetScale(w / 8, h / 8); self.px:Draw(x, y)
end

function AI:frame(x, y, w, h, t, c, a)
	self:rect(x, y, w, t, c, a)
	self:rect(x, y + h - t, w, t, c, a)
	self:rect(x, y + t, t, h - 2 * t, c, a)
	self:rect(x + w - t, y + t, t, h - 2 * t, c, a)
end

-- a glow, then the line on top
function AI:segment(x1, y1, x2, y2, a)
	local dx, dy = x2 - x1, y2 - y1
	local len = math.sqrt(dx * dx + dy * dy)
	if len < 0.5 then return end
	local line = self.line
	line:SetRotation(-math.deg(math.atan(dy, dx)))
	line:SetBlendMode("add")
	line:SetScale(len / 64, 3); line:SetOpacity(0.28 * a)
	line:DrawAtAnchor((x1 + x2) / 2, (y1 + y2) / 2, "center")
	line:SetBlendMode("normal")
	line:SetScale(len / 64, 1); line:SetOpacity(a)
	line:DrawAtAnchor((x1 + x2) / 2, (y1 + y2) / 2, "center")
end

function AI:spot(tex, x, y, scale, c, a, add)
	if tex == nil or a <= 0 then return end
	tex:SetColor(c[1], c[2], c[3]); tex:SetScale(scale, scale); tex:SetOpacity(a)
	if add then tex:SetBlendMode("add") end
	tex:DrawAtAnchor(x, y, "center")
	if add then tex:SetBlendMode("normal") end
end

-- g: 0..1 of the growth (all traces grow at the same speed)
function AI:drawTraces(g, a, pulses)
	if self.line == nil or g <= 0 then return end
	self.line:SetColor(CYAN[1], CYAN[2], CYAN[3])
	local grown = g * LONGEST
	for _, w in ipairs(TRACES) do
		local reach = math.min(grown, w.len)
		for _, s in ipairs(w.segs) do
			if reach <= s.from then break end
			local k = math.min(1, (reach - s.from) / s.len)
			self:segment(s.a[1], s.a[2], s.a[1] + (s.b[1] - s.a[1]) * k, s.a[2] + (s.b[2] - s.a[2]) * k, a)
			if k >= 1 and s.from + s.len < w.len then self:spot(self.dot, s.b[1], s.b[2], 0.45, CYAN, a) end
		end
		if reach < w.len then
			local x, y = pointAt(w, reach)
			self:spot(self.dot, x, y, 0.9, TIP, a, true)
		else
			if w.via then
				local e = w.pts[#w.pts]
				self:spot(self.ring, e[1], e[2], 0.6 + 0.4 * easeOut(clamp01((grown - w.len) / 60)), CYAN, a)
			end
			if pulses then                                              -- signals leaving the chip
				local d = (self.clock * PULSE_SPEED + w.pulse * (w.len + 300)) % (w.len + 300) - 150
				if d > 0 and d < w.len then
					local x, y = pointAt(w, d)
					self:spot(self.dot, x, y, 0.7, TIP, a, true)
				end
			end
		end
	end
	self.line:SetRotation(0); self.line:SetScale(1, 1); self.line:SetOpacity(1); self.line:SetColor(1, 1, 1)
	for _, tex in ipairs({ self.dot, self.ring }) do tex:SetScale(1, 1); tex:SetOpacity(1); tex:SetColor(1, 1, 1) end
end

-- k: 0..1 as the chip lights up
function AI:drawChip(k, a)
	if k <= 0 then return end
	a = a * k
	local grow = 0.8 + 0.2 * easeOut(k)
	local h = CHIP_HALF * grow
	local x, y, s = CHIP_X - h, CHIP_Y - h, 2 * h
	for i = 1, PINS do
		local o = pinOffset(i) * grow
		self:rect(x - PIN_LEN, CHIP_Y + o - PIN_W / 2, PIN_LEN, PIN_W, CYAN, a)
		self:rect(x + s, CHIP_Y + o - PIN_W / 2, PIN_LEN, PIN_W, CYAN, a)
		self:rect(CHIP_X + o - PIN_W / 2, y - PIN_LEN, PIN_W, PIN_LEN, CYAN, a)
		self:rect(CHIP_X + o - PIN_W / 2, y + s, PIN_W, PIN_LEN, CYAN, a)
	end
	self:rect(x, y, s, s, CHIP_FILL, a)
	self:frame(x, y, s, s, FRAME, CYAN, a)
	local d = DIE * grow
	self:frame(x + d, y + d, s - 2 * d, s - 2 * d, 2, CYAN, 0.35 * a)
	self:spot(self.dot, x + d / 2, y + d / 2, 0.8, CYAN, a)          -- pin 1 mark
	if self.dot then self.dot:SetScale(1, 1); self.dot:SetOpacity(1); self.dot:SetColor(1, 1, 1) end
end

-- k: how open the box is (0..1); typed: how many characters show
function AI:drawBox(k, typed, a)
	if k <= 0 then return end
	local w = (self.boxW or BOX_MIN_W) * easeOut(k)
	local x, y = CHIP_X - w / 2, BOX_Y - BOX_H / 2
	self:rect(x, y, w, BOX_H, BG, a)
	self:frame(x, y, w, BOX_H, FRAME, CYAN, a)
	for _, c in ipairs({ { x, y }, { x + w - 12, y }, { x, y + BOX_H - 12 }, { x + w - 12, y + BOX_H - 12 } }) do
		self:rect(c[1], c[2], 12, 12, CYAN, a)
	end
	if k < 1 or self.font == nil or self.chars == nil then return end
	local s = table.concat(self.chars, "", 1, math.min(typed, #self.chars))
	if math.floor(self.clock * 2.5) % 2 == 0 then s = s .. "_" end
	self.font:Draw(s, CHIP_X - self.textW / 2 - TEXT_PAD, BOX_Y + self.nudge, self.colText, self.colClear, a, 1, 0, "left")
end

function AI:fadeOut(t)
	self.clock = self.clock + frameDt()
	if self.phase ~= "out" then                                      -- a new trip
		self.phase, self.beeped, self.typed = "out", false, false
		self:status()
		play(self.sndWires)
	end
	local k = clamp01((t - BOX_AT) / BOX_OPEN)
	if k > 0 and not self.beeped then self.beeped = true; play(self.sndBeep) end
	if t >= TYPE_AT and not self.typed then self.typed = true; play(self.sndTyping) end
	self:rect(0, 0, SCREEN_W, SCREEN_H, BG, easeOut(clamp01(t / BG_IN)))
	self:drawTraces(clamp01((t - GROW_AT) / (BOX_AT - GROW_AT)), 1, false)
	self:drawChip(clamp01((t - CHIP_AT) / CHIP_IN), 1)
	self:drawBox(k, math.floor(#self.chars * clamp01((t - TYPE_AT) / (TYPE_END - TYPE_AT)) + 0.5), 1)
end

function AI:loading()
	self.phase = "load"
	self.clock = self.clock + frameDt()
	self:rect(0, 0, SCREEN_W, SCREEN_H, BG, 1)
	self:drawTraces(1, 1, true)
	self:drawChip(1, 1)
	self:drawBox(1, #(self.chars or {}), 1)
end

function AI:fadeIn(t)
	self.phase = "in"
	self.clock = self.clock + frameDt()
	local a = 1 - easeInOut(t)
	self:rect(0, 0, SCREEN_W, SCREEN_H, BG, a)
	self:drawTraces(1, a, true)
	self:drawChip(1, a)
	self:drawBox(1, #(self.chars or {}), a)
end

return AI
