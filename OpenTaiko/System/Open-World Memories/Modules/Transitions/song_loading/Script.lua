---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- song_loading transition — the whole song-select → gameplay flow as ONE transition, owning ALL the visuals.
--   fadeOut(t)            cover the song-select screen (t 0→1)
--   loading(progress, t)  draw the loading screen (bg, player characters + nameplates / dan plate, song title
--                         + plate, progress bar) while the engine loads the chart / WAV / game screen
--   fadeIn(t)             reveal the loaded game screen (t 0→1)
-- The C# loader (CStageSongLoading) runs the actual load + reports `progress` (0..1); everything drawn here.

-- Fade durations (seconds) for this transition; the engine reads these globals (default is 0.5s elsewhere).
-- The song-select → gameplay flow reads better with a slightly slower 1s curtain in + reveal out.
FADE_OUT_SECONDS = 1.0
FADE_IN_SECONDS  = 1.0

-- part of class TextureLoader in TextureLoader.cs
local TL = {
	BASE = "Graphics/",
	TOWERRESULT = "8_TowerResult/",
}

local TEXTURES_DIR = "Textures/"
local SKIN_DIR = "../../../"
local SKIN_GRAPHICS_DIR = SKIN_DIR .. TL.BASE
local DIFF_TOWER, DIFF_DAN = 5, 6

-- Textures
local pixel                                   -- 1x1 white, for fades + bar
local tx_bg_normal, tx_bg_ai, tx_bg_dan, tx_bg_tower
local tx_towers = {}
local tx_plate
local titleFont, subtitleFont
local titleTex, subtitleTex

-- State (re-resolved on each new song load)
local mode         = "normal"                 -- "normal" | "ai" | "tower" | "dan"
local player_count = 1
local res_w, res_h = 1920, 1080
local slot_w       = 1920
local characters, slide_infos = {}, {}
local bg_scroll_y = 0
local px_tower_bg_scroll_y = 0
local tower_type = "0"
local dan_tick, dan_r, dan_g, dan_b, dan_title = 0, 255, 255, 255, ""
local initialized  = false
local luaPhase     = nil                      -- "out" | "load" | "in" — used to detect a fresh transition

-- Tuning (mirrors the old ROActivity)
local SLIDE_DURATION, SLIDE_STAGGER = 0.35, 0.28
local TOWER_SCROLL_SPEED, TOWER_SCROLL_MAX = 20, 120
local DAN_SCROLL_SPEED, DAN_SCROLL_MAX = 20, 60

-- Skin layout (1920x1080, centered) — from SongLoadingConfig.ini
local PLATE_X, PLATE_Y      = 960, 540
local TITLE_X, TITLE_Y      = 960, 420
local SUBTITLE_X, SUBTITLE_Y = 960, 487
local DANPLATE_X, DANPLATE_Y = 1682, 320

-- ── helpers ──────────────────────────────────────────────────────────────────
local function rect(x, y, w, h, r, g, b, a)
	if pixel == nil or w <= 0 or h <= 0 then return end
	pixel:SetColor(r, g, b); pixel:SetOpacity(a); pixel:SetScale(w, h); pixel:Draw(x, y)
end

-- The loading background for the current mode, at the given opacity (used to cross-fade in from song select).
local function draw_bg(alpha)
	local tx
	if mode == "dan" then tx = tx_bg_dan
	elseif mode == "tower" then tx = tx_bg_tower
	elseif mode == "ai" then tx = tx_bg_ai
	else tx = tx_bg_normal end
	if tx ~= nil then
		tx:SetOpacity(alpha)
		if mode == "tower" then tx:Draw(0, -px_tower_bg_scroll_y)
		elseif mode == "dan" then tx:Draw(0, -bg_scroll_y)
		else tx:Draw(0, 0)
		end
	end
end

-- (Re)build the per-player character renders + slide-in animations for the current player count.
local function setup_characters()
	for i = 1, #characters do
		if characters[i] ~= nil then characters[i]:DisposeAnimation(CHARACTER.ANIM_RENDER) end
	end
	characters, slide_infos = {}, {}
	slot_w = res_w / math.max(1, player_count)
	if mode ~= "normal" then return end

	local stagger = (player_count > 1) and math.min(SLIDE_STAGGER, 0.7 / (player_count - 1)) or 0.0
	for i = 1, player_count do
		local chara = CHARACTER:GetPlayerCharacter(i - 1)
		chara:LoadAnimation(CHARACTER.ANIM_RENDER)
		chara:SetOpacity(0.3)
		characters[i] = chara
		local cnt = COUNTER:CreateCounterDuration(0.0, 1.0, SLIDE_DURATION)
		cnt:SetEasing("OUT", "QUAD")
		slide_infos[i] = {
			counter  = cnt,
			delay    = (i - 1) * stagger,
			elapsed  = 0.0,
			started  = false,
			startpos = res_w + (i - 1) * slot_w,
			endpos   = (i - 1) * slot_w,
		}
	end
end

-- Resolve the chosen song + difficulty (from SONGMOUNT) into mode + dan plate data + title textures.
local function setup()
	player_count = CONFIG.PlayerCount
	local res = THEME:GetResolution(); res_w, res_h = res.X, res.Y
	bg_scroll_y = 0
	px_tower_bg_scroll_y = 0

	local diff = SONGMOUNT:ChosenDifficulty()
	local node = SONGMOUNT:ChosenSongNode()
	if diff == DIFF_DAN then mode = "dan"
	elseif diff == DIFF_TOWER then mode = "tower"
	elseif CONFIG.IsAIBattleMode then mode = "ai"
	else mode = "normal" end

	tower_type = "0"
	dan_tick, dan_r, dan_g, dan_b, dan_title = 0, 255, 255, 255, ""
	if node ~= nil and node.IsSong then
		local chart
		if mode == "tower" then chart = node:GetChart(DIFF_TOWER)
		elseif mode == "dan" then chart = node:GetChart(DIFF_DAN)
		end
		if chart ~= nil then
			tower_type = chart.TowerType or "0"
			dan_tick = chart.DanTick or 0
			local c = chart.DanTickColor
			if c ~= nil then dan_r, dan_g, dan_b = c.R, c.G, c.B end
		end
		dan_title = node.Title or ""
	end

	if titleTex ~= nil then titleTex:Dispose(); titleTex = nil end
	if subtitleTex ~= nil then subtitleTex:Dispose(); subtitleTex = nil end
	local title    = (node ~= nil and node.Title) or ""
	local subtitle = (node ~= nil and node.Subtitle) or ""
	if title ~= "" and titleFont ~= nil then titleTex = titleFont:GetText(title) end
	if subtitle ~= "" and subtitleFont ~= nil then subtitleTex = subtitleFont:GetText(subtitle) end

	setup_characters()
	initialized = true
end

local function tick_update()
	local dt = fps.deltaTime

	local live = CONFIG.PlayerCount or player_count
	if live ~= player_count and live >= 1 then player_count = live; setup_characters() end

	if mode == "tower" and bg_scroll_y < TOWER_SCROLL_MAX then
		bg_scroll_y = math.min(bg_scroll_y + TOWER_SCROLL_SPEED * dt, TOWER_SCROLL_MAX)
		px_tower_bg_scroll_y = (1 - bg_scroll_y / TOWER_SCROLL_MAX) * (tx_bg_tower.Height - res_h)
	elseif mode == "dan" and bg_scroll_y < DAN_SCROLL_MAX then
		bg_scroll_y = math.min(bg_scroll_y + DAN_SCROLL_SPEED * dt, DAN_SCROLL_MAX)
	end

	for i = 1, player_count do
		local si = slide_infos[i]
		if si ~= nil then
			si.elapsed = si.elapsed + dt
			if not si.started and si.elapsed >= si.delay then
				si.counter:Start(); si.started = true
			elseif si.started then
				si.counter:Tick()
			end
		end
	end
end

local function draw_characters(opacity)
	opacity = opacity or 255
	for i = 1, player_count do
		local chara, si = characters[i], slide_infos[i]
		if chara ~= nil and si ~= nil then
			local slot_x = si.startpos - si.counter.Value * res_w
			chara:Update(CHARACTER.ANIM_RENDER, true)
			local rs = chara:GetAnimationSize(CHARACTER.ANIM_RENDER)
			local render_w, render_h = rs.X, rs.Y
			local scale = (render_h > 1450) and (1450.0 / render_h) or 1.0
			local scaled_w, scaled_h = render_w * scale, render_h * scale
			local crop_w = math.min(slot_w, scaled_w)
			local crop_h = math.min(res_h, scaled_h)
			local off_x = math.max(0.0, (scaled_w - crop_w) / 2.0)
			local off_y = math.max(0.0, (scaled_h - crop_h) / 2.0)
			chara:SetScale(scale, scale)
			chara:DrawRectAtAnchor(slot_x, 0, crop_w, crop_h, CHARACTER.ANIM_RENDER, opacity, off_x, off_y)
			local np_w = 384
			local np_x = math.floor(slot_x + (slot_w - np_w) / 2)
			NAMEPLATE:DrawPlayerNameplate(np_x, res_h - 200, opacity, i - 1)
		end
	end
end

-- alpha 0..1 scales the whole screen's opacity (1 while loading; ramps to 0 during the fade-in reveal of
-- gameplay). showBar: only while actually loading. The note chips draw on TOP (main loop), so they stay
-- visible as the loading screen clears.
local function draw_screen(progress, alpha, showBar)
	alpha = alpha or 1.0
	local op = math.floor(alpha * 255)
	draw_bg(alpha)

	-- foreground: dan plate, or player characters + the song title plate
	if mode == "dan" then
		local danplate = ROACTIVITY:GetROActivity("danplate")
		if danplate ~= nil then danplate:Draw(DANPLATE_X, DANPLATE_Y, op, dan_tick, dan_r, dan_g, dan_b, dan_title) end
	else
		if mode == "tower" then
			local tx_tower = tx_towers[tower_type] or tx_towers["0"]
			if tx_bg_tower ~= nil and tx_tower ~= nil then
				local xFactor = (tx_bg_tower.Width - tx_tower.Width) / 2;
				local yFactor = tx_tower.Height / tx_bg_tower.Height;
				tx_tower:Draw(xFactor, -1 * yFactor * px_tower_bg_scroll_y);
			end
		end
		if mode == "normal" then draw_characters(op) end
		if tx_plate ~= nil then tx_plate:SetOpacity(alpha); tx_plate:DrawAtAnchor(PLATE_X, PLATE_Y, "center") end
		if titleTex ~= nil then titleTex:SetOpacity(alpha); titleTex:DrawAtAnchor(TITLE_X, TITLE_Y, "center") end
		if subtitleTex ~= nil then subtitleTex:SetOpacity(alpha); subtitleTex:DrawAtAnchor(SUBTITLE_X, SUBTITLE_Y, "center") end
	end

	if showBar then   -- progress bar (bottom centre)
		local barW, barH = 640, 14
		local bx, by = res_w / 2 - barW / 2, res_h - 70
		local p = math.max(0, math.min(1, progress or 0))
		rect(bx - 2, by - 2, barW + 4, barH + 4, 1, 1, 1, 0.25)
		rect(bx, by, barW, barH, 0.12, 0.12, 0.12, 1.0)
		rect(bx, by, barW * p, barH, 0.95, 0.62, 0.10, 1.0)
	end
end

-- ── transition callbacks ──────────────────────────────────────────────────────
function fadeOut(t)
	if luaPhase ~= "out" then initialized = false; luaPhase = "out" end   -- fresh transition → re-resolve song
	if not initialized then setup() end
	draw_bg(t)   -- cross-fade the loading background IN over the song-select screen (not through black)
end

function loading(progress, elapsed)
	luaPhase = "load"
	if not initialized then setup() end
	tick_update()
	draw_screen(progress, 1.0, true)
end

function fadeIn(t)
	luaPhase = "in"
	tick_update()                       -- keep characters animating while they fade
	draw_screen(1.0, 1.0 - t, false)    -- fade the loading screen out → reveal gameplay (notes draw on top)
end

function onStart()
	pixel        = TEXTURE:CreateTexture("pixel.png")
	tx_bg_normal = TEXTURE:CreateTexture(TEXTURES_DIR .. "BgWait.png")
	tx_bg_ai     = TEXTURE:CreateTexture(TEXTURES_DIR .. "BgWait_AI.png")
	tx_bg_dan    = TEXTURE:CreateTexture(TEXTURES_DIR .. "BgWait_Dan.png")
	tx_bg_tower  = TEXTURE:CreateTexture(SKIN_DIR .. TL.BASE .. TL.TOWERRESULT .. "Background.png") --[["BgWait_Tower.png" did not exist]]
	tx_plate     = TEXTURE:CreateTexture(TEXTURES_DIR .. "Plate.png")
	titleFont    = TEXT:Create(46)
	subtitleFont = TEXT:Create(30)

	local dir_name = SKIN_DIR .. TL.BASE .. TL.TOWERRESULT .. "Tower"
	if STORAGE:DirectoryExists(dir_name) then
		local files = STORAGE:GetFiles(dir_name, "*.png")
		for k, v in pairs(files) do
			local file_path = dir_name.."/"..tostring(v)..".png"
			if STORAGE:FileExists(file_path) then
				tx_towers[v] = TEXTURE:CreateTexture(file_path)
			end
		end
	end
end

function onDestroy()
	for i = 1, #characters do
		if characters[i] ~= nil then characters[i]:DisposeAnimation(CHARACTER.ANIM_RENDER) end
	end
	characters, slide_infos = {}, {}
	for _, tx in pairs({ pixel, tx_bg_normal, tx_bg_ai, tx_bg_dan, tx_bg_tower, tx_plate, titleTex, subtitleTex }) do
		if tx ~= nil then tx:Dispose() end
	end
	for _, tx in pairs(tx_towers) do
		if tx ~= nil then tx:Dispose() end
	end
	tx_towers = {}
end
