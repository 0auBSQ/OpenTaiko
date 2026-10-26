---@diagnostic disable: undefined-global, undefined-field
-- Default transition: a black cross-fade with a "Loading..." screen + progress bar.
-- A skin can ship its own Modules/Transitions/<name>/ and select it with Exit(target, name, "<name>").
--
-- The engine (CStageTransition) calls:
--   fadeOut(t)            t 0->1, cover the outgoing stage  (1 = fully covered)
--   loading(progress, t)  draw the loading screen while the new stage activates + streams its assets
--                         (only called once a load exceeds ~0.5s, so quick switches don't blink)
--   fadeIn(t)             t 0->1, reveal the new stage       (1 = fully revealed)
-- Authored for a 1920x1080 skin; the black cover is over-scaled so it fills any resolution.
-- Positions, sizes and colours: Config/layout.json.

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Bar = Layout.bar

local pixel = nil          -- 1x1 white texture, tinted/scaled for solid rectangles
local loadingText = nil    -- "Loading..." (created once; never per-frame to avoid the text-texture leak)

-- Solid black full-screen cover at the given opacity (0..1).
local function cover(alpha)
	if pixel == nil then return end
	pixel:SetColor(0, 0, 0)
	pixel:SetOpacity(alpha)
	pixel:SetScale(Layout.cover_scale, Layout.cover_scale)
	pixel:Draw(0, 0)
end

-- A solid colored rectangle (re-using the shared pixel texture).
local function rect(x, y, w, h, color, a)
	if pixel == nil or w <= 0 or h <= 0 then return end
	pixel:SetColor(color[1], color[2], color[3])
	pixel:SetOpacity(a)
	pixel:SetScale(w, h)
	pixel:Draw(x, y)
end

function fadeOut(t)
	cover(t)
end

function fadeIn(t)
	cover(1.0 - t)
end

function loading(progress, elapsed)
	cover(1.0)

	-- "Loading..." text, centered above the bar.
	if loadingText ~= nil then
		loadingText:DrawAtAnchor(Layout.loading_text.x, Layout.loading_text.y, "center")
	end

	-- Progress bar (track + fill). The fill advances as the new stage's assets stream in, so the player
	-- always sees motion instead of a frozen frame.
	local barW = Bar.width
	local barH = Bar.height
	local x = Bar.center_x - barW / 2
	local y = Bar.y
	local b = Bar.border
	rect(x - b, y - b, barW + 2 * b, barH + 2 * b, Bar.border_color, Bar.border_opacity)   -- subtle border
	rect(x, y, barW, barH, Bar.track_color, Bar.track_opacity)                             -- track
	local p = progress
	if p < 0 then p = 0 elseif p > 1 then p = 1 end
	rect(x, y, barW * p, barH, Bar.fill_color, Bar.fill_opacity)                           -- fill
end

function onStart()
	pixel = TEXTURE:CreateTexture("pixel.png")
	local font = TEXT:Create(Layout.loading_text.font_size)
	loadingText = font:GetText("Loading...")
end

function onDestroy()
	if pixel ~= nil then pixel:Dispose() end
	if loadingText ~= nil then loadingText:Dispose() end
end
