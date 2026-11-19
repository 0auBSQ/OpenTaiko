---@diagnostic disable: undefined-global, undefined-field
-- default transition: black cross-fade, loading screen with a progress bar
-- skins add Modules/Transitions/<name>/, picked with Exit(target, name, "<name>")
-- layout: Config/layout.json

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Bar = Layout.bar

local pixel = nil          -- 1x1 white, tinted and scaled for rects
local loadingText = nil    -- created once: per-frame GetText leaks

-- alpha 0..1; over-scaled to fill any resolution
local function cover(alpha)
	if pixel == nil then return end
	pixel:SetColor(0, 0, 0)
	pixel:SetOpacity(alpha)
	pixel:SetScale(Layout.cover_scale, Layout.cover_scale)
	pixel:Draw(0, 0)
end

local function rect(x, y, w, h, color, a)
	if pixel == nil or w <= 0 or h <= 0 then return end
	pixel:SetColor(color[1], color[2], color[3])
	pixel:SetOpacity(a)
	pixel:SetScale(w, h)
	pixel:Draw(x, y)
end

-- engine (CStageTransition): t 0..1, 1 = covered
function fadeOut(t)
	cover(t)
end

-- t 0..1, 1 = revealed
function fadeIn(t)
	cover(1.0 - t)
end

-- called only once a load passes ~0.5s
function loading(progress, elapsed)
	cover(1.0)

	if loadingText ~= nil then
		loadingText:DrawAtAnchor(Layout.loading_text.x, Layout.loading_text.y, "center")
	end

	-- fill follows the new stage's asset streaming
	local barW = Bar.width
	local barH = Bar.height
	local x = Bar.center_x - barW / 2
	local y = Bar.y
	local b = Bar.border
	rect(x - b, y - b, barW + 2 * b, barH + 2 * b, Bar.border_color, Bar.border_opacity)
	rect(x, y, barW, barH, Bar.track_color, Bar.track_opacity)
	local p = progress
	if p < 0 then p = 0 elseif p > 1 then p = 1 end
	rect(x, y, barW * p, barH, Bar.fill_color, Bar.fill_opacity)
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
