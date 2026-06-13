-- Song Loading ROActivity
-- Draws the loading screen background and player character RENDER animations.
-- Textures are loaded from the Textures/ subfolder of this ROActivity.

local TEXTURES_DIR = "Textures/"

-- Difficulty constants (mirror the C# Difficulty enum)
local DIFF_TOWER = 5
local DIFF_DAN   = 6

-- Loaded background textures
local tx_bg_normal = nil
local tx_bg_ai     = nil
local tx_bg_dan    = nil
local tx_bg_tower  = nil

-- Current activation state
local mode         = "normal"   -- "normal" | "ai" | "tower" | "dan"

-- Layout (resolved at activate time)
local player_count = 1
local res_w        = 1920
local res_h        = 1080
local slot_w       = res_w

-- Per-player state (1-indexed), used in normal mode only
local characters   = {}
local slide_infos  = {}   -- { counter, delay, elapsed, started }
local setup_characters    -- forward declaration (defined after activate, called from activate + update)

-- Dan background vertical scroll (pixels, positive = scrolled up)
local dan_scroll_y = 0

-- Dan plate data (populated from activate args when mode == "dan")
local dan_tick    = 0
local dan_r       = 255
local dan_g       = 255
local dan_b       = 255
local dan_title   = ""
local dan_plate_x = 0
local dan_plate_y = 0

-- Slide animation tuning
local SLIDE_DURATION = 0.35   -- seconds for each character to slide into place
local SLIDE_STAGGER  = 0.28   -- seconds between consecutive character entrances

-- Dan scroll matches the original C# timing:
-- scrolls from 0 to 60 px over ~3 seconds, then holds
local DAN_SCROLL_SPEED = 20   -- px per second (60 px / 3 s)
local DAN_SCROLL_MAX   = 60

-- ────────────────────────────────────────────────────────────────────────────
-- ROActivity lifecycle
-- ────────────────────────────────────────────────────────────────────────────

function onStart()
	tx_bg_normal = TEXTURE:CreateTexture(TEXTURES_DIR .. "BgWait.png")
	tx_bg_ai     = TEXTURE:CreateTexture(TEXTURES_DIR .. "BgWait_AI.png")
	tx_bg_dan    = TEXTURE:CreateTexture(TEXTURES_DIR .. "BgWait_Dan.png")
	tx_bg_tower  = TEXTURE:CreateTexture(TEXTURES_DIR .. "BgWait_Tower.png")
end

-- activate(node, chosen_diff, dan_tick, dan_r, dan_g, dan_b, dan_title, plate_x, plate_y)
function activate(node, chosen_diff, tick, r, g, b, title, plate_x, plate_y)
	player_count = CONFIG.PlayerCount
	local res    = THEME:GetResolution()
	res_w        = res.X
	res_h        = res.Y
	slot_w       = res_w / player_count

	dan_scroll_y = 0

	-- Determine mode
	if chosen_diff == DIFF_DAN then
		mode = "dan"
	elseif chosen_diff == DIFF_TOWER then
		mode = "tower"
	elseif CONFIG.IsAIBattleMode then
		mode = "ai"
	else
		mode = "normal"
	end

	-- Store dan plate data (used in draw when mode == "dan").
	-- Read directly from the node's chart so both the Lua and legacy C# dan select
	-- paths work correctly (the C# path may pass empty/zero values when stageDanSongSelect
	-- is not populated, e.g. when using the Lua dan_select stage).
	if node ~= nil and node.IsSong then
		local chart = node:GetChart(DIFF_DAN)
		if chart ~= nil then
			dan_tick = chart.DanTick or 0
			local c  = chart.DanTickColor
			if c ~= nil then
				dan_r = c.R ; dan_g = c.G ; dan_b = c.B
			else
				dan_r = 255 ; dan_g = 255 ; dan_b = 255
			end
		else
			dan_tick = 0 ; dan_r = 255 ; dan_g = 255 ; dan_b = 255
		end
		dan_title = node.Title or ""
	else
		-- Fallback: use args passed by C# (legacy path)
		dan_tick  = tick  or 0
		dan_r     = r     or 255
		dan_g     = g     or 255
		dan_b     = b     or 255
		dan_title = title or ""
	end
	dan_plate_x = plate_x or 0
	dan_plate_y = plate_y or 0

	-- Set up the per-player character slide-in (normal mode only)
	setup_characters()
end

-- (Re)build the character renders + slide animations for the CURRENT player count, splitting the
-- screen into player_count equal slots. Extracted from activate so update() can re-run it if the
-- count changes after activation — online play sets the real N late (the lobby mounts virtual slots
-- right before launch), and if we sized for the stale count the renders would overlap.
setup_characters = function()
	-- dispose any previously-loaded renders first
	for i = 1, #characters do
		if characters[i] ~= nil then characters[i]:DisposeAnimation(CHARACTER.ANIM_RENDER) end
	end
	characters  = {}
	slide_infos = {}
	slot_w      = res_w / math.max(1, player_count)

	if mode ~= "normal" then return end

	-- Cap total stagger so the last player always starts within 0.7 s, regardless of player count.
	local stagger = (player_count > 1)
		and math.min(SLIDE_STAGGER, 0.7 / (player_count - 1))
		or  0.0

	for i = 1, player_count do
		local chara = CHARACTER:GetPlayerCharacter(i - 1)
		chara:LoadAnimation(CHARACTER.ANIM_RENDER)
		chara:SetOpacity(0.3)
		characters[i] = chara

		-- P1 enters first, then P2, P3, … in order
		local delay = (i - 1) * stagger

		-- Counter goes 0 → 1 over SLIDE_DURATION seconds.
		-- slot_x = startpos - counter * res_w
		--   at 0: fully off-screen right  (startpos = res_w + (i-1)*slot_w)
		--   at 1: resting position         (endpos   = (i-1)*slot_w)
		local cnt = COUNTER:CreateCounterDuration(0.0, 1.0, SLIDE_DURATION)
		cnt:SetEasing("OUT", "QUAD")

		slide_infos[i] = {
			counter  = cnt,
			delay    = delay,
			elapsed  = 0.0,
			started  = false,
			startpos = res_w + (i - 1) * slot_w,
			endpos   = (i - 1) * slot_w,
		}
	end
end

function deactivate()
	for i = 1, player_count do
		if characters[i] ~= nil then
			characters[i]:DisposeAnimation(CHARACTER.ANIM_RENDER)
		end
	end
	characters   = {}
	slide_infos  = {}
	dan_scroll_y = 0
end

function update()
	local dt = fps.deltaTime

	-- self-heal if the player count changed since activate (online sets the real N late) so the
	-- screen always splits into the right number of slots instead of overlapping renders
	local live = CONFIG.PlayerCount or player_count
	if live ~= player_count and live >= 1 then
		player_count = live
		setup_characters()
	end

	-- Dan background scroll
	if mode == "dan" then
		if dan_scroll_y < DAN_SCROLL_MAX then
			dan_scroll_y = math.min(dan_scroll_y + DAN_SCROLL_SPEED * dt, DAN_SCROLL_MAX)
		end
	end

	-- Character slide-in counters (normal mode only)
	for i = 1, player_count do
		local si = slide_infos[i]
		if si ~= nil then
			si.elapsed = si.elapsed + dt

			if not si.started and si.elapsed >= si.delay then
				si.counter:Start()
				si.started = true
				-- Don't tick this frame: DeltaTime may be large due to asset loading.
				-- The counter will begin advancing normally next frame.
			elseif si.started then
				si.counter:Tick()
			end
		end
	end
end

function draw()
	-- ── Background ───────────────────────────────────────────────────────────
	if mode == "dan" then
		if tx_bg_dan ~= nil then
			tx_bg_dan:Draw(0, -dan_scroll_y)
		end
		local danplate = ROACTIVITY:GetROActivity("danplate")
		if danplate ~= nil then
			danplate:Draw(dan_plate_x, dan_plate_y, 255, dan_tick, dan_r, dan_g, dan_b, dan_title)
		end

	elseif mode == "tower" then
		if tx_bg_tower ~= nil then
			tx_bg_tower:Draw(0, 0)
		end

	elseif mode == "ai" then
		if tx_bg_ai ~= nil then
			tx_bg_ai:Draw(0, 0)
		end

	else
		-- normal
		if tx_bg_normal ~= nil then
			tx_bg_normal:Draw(0, 0)
		end
	end

	-- ── Character renders (normal mode only) ─────────────────────────────────
	if mode ~= "normal" then return end

	for i = 1, player_count do
		local chara = characters[i]
		local si    = slide_infos[i]
		if chara == nil or si == nil then goto continue end

		local slot_x = si.startpos - si.counter.Value * res_w

		chara:Update(CHARACTER.ANIM_RENDER, true)

		-- Scale down renders taller than 1550 theme-pixels (preserving aspect ratio).
		local render_size = chara:GetAnimationSize(CHARACTER.ANIM_RENDER)
		local render_w    = render_size.X
		local render_h    = render_size.Y
		local scale = 1.0
		if render_h > 1450 then
			scale = 1450.0 / render_h
		end

		-- Compute visible crop area and source offset in theme-pixel units.
		-- CharaScript will divide these by the combined scale to get source pixels.
		local scaled_w = render_w * scale
		local scaled_h = render_h * scale          -- ≤ 1550
		local crop_w   = math.min(slot_w, scaled_w)
		local crop_h   = math.min(res_h,  scaled_h)
		local off_x    = math.max(0.0, (scaled_w - crop_w) / 2.0)
		local off_y    = math.max(0.0, (scaled_h - crop_h) / 2.0)

		chara:SetScale(scale, scale)
		chara:DrawRectAtAnchor(slot_x, 0, crop_w, crop_h, CHARACTER.ANIM_RENDER, 255, off_x, off_y)

		-- Nameplate: centered in slot, 200 px above the bottom edge, slides with the character
		local np_w  = 384
		local np_x  = math.floor(slot_x + (slot_w - np_w) / 2)
		local np_y  = res_h - 200
		NAMEPLATE:DrawPlayerNameplate(np_x, np_y, 255, i - 1)

		::continue::
	end
end

function afterSongEnum() end

function onDestroy()
	if tx_bg_normal ~= nil then tx_bg_normal:Dispose() ; tx_bg_normal = nil end
	if tx_bg_ai     ~= nil then tx_bg_ai:Dispose()     ; tx_bg_ai     = nil end
	if tx_bg_dan    ~= nil then tx_bg_dan:Dispose()     ; tx_bg_dan    = nil end
	if tx_bg_tower  ~= nil then tx_bg_tower:Dispose()   ; tx_bg_tower  = nil end
end
