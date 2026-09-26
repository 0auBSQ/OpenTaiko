---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- space_voyage_back: out of the Online Lobby, over the lobby's own 3D sky (Lib/SpaceSky).

local Sky = require("SpaceSky")

FADE_OUT_SECONDS = 1.4
FADE_IN_SECONDS  = 1.2

local SCREEN_W, SCREEN_H = 1920, 1080
local BLEND = 140                -- atmosphere rows that overlap the sky edge
local FULL = SCREEN_H + BLEND    -- sky edge when it covers the whole screen
local LIFT_Y, LIFT_PITCH = 6, 22 -- camera height and tilt at the low point
local NIGHT = { 0.03, 0.03, 0.10 }  -- fallback colour when there is no sky

local tx_air = nil
local air_h = 600
local fill = nil
local snd_down = nil
local whooshed = false
local lastPhase = nil

local function easeInOut(x) return x < 0.5 and 4 * x * x * x or 1 - (-2 * x + 2) ^ 3 / 2 end

local function frameDt()
	local ok, d = pcall(function() return fps.deltaTime end)
	d = ok and tonumber(d) or 0
	return d < 0 and 0 or (d > 0.1 and 0.1 or d)
end

-- k lowers and tilts the camera (0 = the lobby's view)
local function drive(k)
	if not Sky.driving() then return end
	Sky.setLift(-LIFT_Y * k, -LIFT_PITCH * k)
	Sky.update(frameDt())
	Sky.render()
end

-- edge: screen y where the sky ends, with the atmosphere below it
local function drawSky(edge, alpha)
	if alpha <= 0 then return end
	local bottom = math.min(SCREEN_H, edge)
	if bottom > 0 then
		local clipped = bottom < SCREEN_H
		if clipped then GRAPHICS:SetClip(0, 0, SCREEN_W, bottom) end
		if not Sky.draw(0, alpha) and fill ~= nil then
			fill:SetColor(NIGHT[1], NIGHT[2], NIGHT[3]); fill:SetOpacity(alpha)
			fill:SetScale(SCREEN_W / 2, bottom / 2); fill:Draw(0, 0)
		end
		if clipped then GRAPHICS:ClearClip() end
	end
	local top = edge - BLEND
	if tx_air ~= nil and top < SCREEN_H and top + air_h > 0 then
		tx_air:SetOpacity(alpha)
		tx_air:Draw(0, math.floor(top + 0.5))
		tx_air:SetOpacity(1)
	end
end

function fadeOut(t)
	if lastPhase ~= "out" then                     -- a new trip
		lastPhase = "out"
		whooshed = false
		Sky.claim("transition")
	end
	drive(0)
	drawSky(FULL, easeInOut(t))                    -- the sky fades in over the menu
end

function loading(progress, elapsed)
	lastPhase = "load"
	drive(0)
	drawSky(FULL, 1)
end

function fadeIn(t)
	lastPhase = "in"
	if not whooshed then
		whooshed = true
		if snd_down then snd_down:Play() end
	end
	local e = easeInOut(t)
	drive(e)
	drawSky(FULL - (SCREEN_H + air_h) * e, 1)
	if t >= 1 then Sky.release() end
end

function onStart()
	-- sync, since Height sets the slide
	tx_air = TEXTURE:CreateTextureSync("Textures/Atmosphere.png")
	if tx_air.Height and tx_air.Height > 0 then air_h = tx_air.Height end
	fill = CANVAS:CreateCanvas(2, 2); fill:Clear(255, 255, 255, 255); fill:Upload()
	snd_down = SOUND:CreateSFX("Sounds/Descend.ogg")
end

function onDestroy()
	Sky.forget()
	if tx_air then tx_air:Dispose(); tx_air = nil end
	if fill then fill:Dispose(); fill = nil end
	if snd_down then snd_down:Dispose(); snd_down = nil end
end
