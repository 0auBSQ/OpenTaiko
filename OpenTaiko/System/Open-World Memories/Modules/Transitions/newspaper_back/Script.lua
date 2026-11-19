---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- newspaper_back: coin shop -> next stage transition
-- shared string "newspaper_cover": sec into the fade-in the papers hide the next stage (read by the coin shop)

local TA = require("TransitionArt")

-- layout: Config/layout.json; timings and flights: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local ARRIVE, LEAVING, DRIFT = Animation.arrive, Animation.leave, Animation.drift
local SHADOW = Layout.shadow

local SCATTER_AT, SCATTER = Animation.scatter_at_s, Animation.scatter_s   -- papers out
local REVEAL_AT = SCATTER_AT + Animation.reveal_after_s   -- next stage starts to show

FADE_OUT_SECONDS = Animation.fade_out_s  -- papers in
FADE_IN_SECONDS  = SCATTER_AT + SCATTER

local SCREEN_W = Layout.screen_width
local PAGES = 6                          -- Textures/Page1..6.png
local OFF = Layout.offscreen

-- page layout, re-rolled each trip
local SHEETS = {}
for i, R in ipairs(Layout.sheets.rest) do SHEETS[i] = { bx = R.bx, by = R.by, s = R.s } end

local tx_pages, tx_shadow = {}, nil
local snd_rush, snd_scatter = nil, nil

local lastPhase = nil
local played = {}
local clock = 0                          -- sec since the trip start, for the drift
local wait = TA.new()

local function clamp01(x) return x < 0 and 0 or (x > 1 and 1 or x) end

-- arrival: fade-out fraction; leave: sec from SCATTER_AT, top first
local function plan()
	local r = math.random
	local n = #SHEETS
	local J = Layout.sheets
	for i, S in ipairs(SHEETS) do
		local k = (i - 1) / (n - 1)
		S.x = S.bx + (r() - 0.5) * J.jitter_x
		S.y = S.by + (r() - 0.5) * J.jitter_y
		S.r = (r() - 0.5) * J.jitter_deg + ((r() < J.upside_down_chance) and 180 or 0)
		S.p = math.random(PAGES)
		S.inAt = math.max(0, ARRIVE.spread * k + ARRIVE.jitter * (r() - 0.5))
		S.inDur = math.min(1 - S.inAt, ARRIVE.duration_min + ARRIVE.duration_extra * r())
		S.fromY = S.y + (r() - 0.5) * ARRIVE.from_y
		S.arc = ARRIVE.arc_min + ARRIVE.arc_extra * r()
		S.spinIn = (r() < 0.5 and -1 or 1) * (ARRIVE.spin_deg_min + ARRIVE.spin_deg_extra * r())
		S.flapIn = ARRIVE.flap_min + ARRIVE.flap_extra * r()
		S.ph1, S.ph2, S.ph3 = r() * 6.283, r() * 6.283, r() * 6.283
		S.outAt = LEAVING.spread * (1 - k) + LEAVING.jitter * r()
		S.outDur = math.min(SCATTER - LEAVING.end_margin_s - S.outAt, LEAVING.duration_min + LEAVING.duration_extra * r())
		S.toY = S.y + (r() - 0.5) * LEAVING.to_y
		S.spinOut = (r() < 0.5 and -1 or 1) * (LEAVING.spin_deg_min + LEAVING.spin_deg_extra * r())
		S.flapOut = LEAVING.flap_min + LEAVING.flap_extra * r()
	end
end

-- textures live only during a trip (Lib/TransitionArt)
local function loadTextures()
	if tx_shadow ~= nil then return end
	for i = 1, PAGES do tx_pages[i] = TEXTURE:CreateTexture("Textures/Page" .. i .. ".png") end
	tx_shadow = TEXTURE:CreateTexture("Textures/Shadow.png")
end

local function freeTextures()
	for i, tx in pairs(tx_pages) do tx:Dispose(); tx_pages[i] = nil end
	if tx_shadow then tx_shadow:Dispose(); tx_shadow = nil end
end

local function once(key, snd)
	if played[key] then return end
	played[key] = true
	if snd then snd:Play() end
end

local function tick()
	local ok, d = pcall(function() return fps.deltaTime end)
	d = ok and tonumber(d) or 0
	clock = clock + (d < 0 and 0 or (d > 0.1 and 0.1 or d))
end

-- lift 0..1: height off the pile
local function drawSheet(tx, x, y, sx, sy, rot, lift)
	if tx == nil or tx_shadow == nil then return end
	local grow = 1 + SHADOW.lift_grow * lift
	tx_shadow:SetOpacity(SHADOW.opacity * (1 - SHADOW.lift_fade * lift))
	tx_shadow:SetScale(sx * grow, sy * grow)
	tx_shadow:SetRotation(rot)
	tx_shadow:DrawAtAnchor(x + SHADOW.x + SHADOW.lift_x * lift, y + SHADOW.y + SHADOW.lift_y * lift, "center")
	tx:SetScale(sx, sy)
	tx:SetRotation(rot)
	tx:DrawAtAnchor(x, y, "center")
end

-- arrive: fade-out progress (1 = all landed); away: sec since SCATTER_AT
local function drawPile(arrive, away)
	for _, S in ipairs(SHEETS) do
		local a = S.inDur > 0 and clamp01((arrive - S.inAt) / S.inDur) or 1
		local l = clamp01((away - S.outAt) / S.outDur)
		local x, y, rot, sx, lift = S.x, S.y, S.r, 1, 0
		if a < 1 then
			local e = 1 - (1 - a) ^ ARRIVE.ease
			local f = 1 - e
			x = SCREEN_W + OFF + (S.x - SCREEN_W - OFF) * e
			y = S.fromY + (S.y - S.fromY) * e - S.arc * math.sin(math.pi * e)
			rot = S.r + S.spinIn * f ^ ARRIVE.spin_ease
			sx = 1 - Animation.flap_squash * math.abs(math.sin(math.pi * S.flapIn * a)) * f
			lift = f
		elseif l > 0 then
			local e = l ^ LEAVING.ease
			x = S.x + (-OFF - S.x) * e
			y = S.y + (S.toY - S.y) * e
			rot = S.r + S.spinOut * e
			sx = 1 - Animation.flap_squash * math.abs(math.sin(math.pi * S.flapOut * l)) * e
			lift = math.min(1, l * LEAVING.lift_rate)
		end
		x = x + DRIFT.x_px * math.sin(DRIFT.x_rate * clock + S.ph1)
		y = y + DRIFT.y_px * math.sin(DRIFT.y_rate * clock + S.ph2)
		rot = rot + DRIFT.turn_deg * math.sin(DRIFT.turn_rate * clock + S.ph3)
		if x > -OFF and x < SCREEN_W + OFF then
			local k = S.s * (1 + Layout.sheets.lift_grow * lift)
			drawSheet(tx_pages[S.p], x, y, k * sx, k, rot, lift)
		end
	end
end

function fadeOut(t)
	if lastPhase ~= "out" then                -- a new trip
		lastPhase = "out"
		played, clock = {}, 0
		plan()
		SHARED:SetSharedString("newspaper_cover", string.format("%.2f", REVEAL_AT))
		loadTextures(); wait:reset()
	end
	t = wait:progress(t, tx_shadow, tx_pages[1], tx_pages[2], tx_pages[3], tx_pages[4], tx_pages[5], tx_pages[6])
	tick()
	once("rush", snd_rush)
	drawPile(t, -1)
end

function loading(progress, elapsed)
	lastPhase = "load"
	tick()
	drawPile(1, -1)
end

function fadeIn(t)
	lastPhase = "in"
	tick()
	local now = t * FADE_IN_SECONDS
	if now >= SCATTER_AT then once("scatter", snd_scatter) end
	drawPile(1, now - SCATTER_AT)
	if t >= 1 then freeTextures() end   -- the trip is over
end

function onStart()
	snd_rush = SOUND:CreateSFX("Sounds/Rush.ogg")
	snd_scatter = SOUND:CreateSFX("Sounds/Scatter.ogg")
end

function onDestroy()
	freeTextures()
	if snd_rush then snd_rush:Dispose(); snd_rush = nil end
	if snd_scatter then snd_scatter:Dispose(); snd_scatter = nil end
end
