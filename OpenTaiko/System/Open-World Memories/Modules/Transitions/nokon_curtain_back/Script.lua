---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- nokon_curtain_back: Intro Nokon's stage curtain on the way back to the title (and the fast way in).
-- The two Curtain_Open.png halves slide shut over the outgoing screen (fadeOut), hold closed while the next
-- stage loads (loading — the seam is hidden by the full Curtain.png), then part to reveal it (fadeIn).

FADE_OUT_SECONDS = 0.9   -- curtain closing
FADE_IN_SECONDS  = 0.9   -- curtain opening

local SCREEN_W, SCREEN_H = 1920, 1080
local CURTAIN_W = 960                    -- each half

local tx_closed, tx_open = nil, nil
local snd_curtain = nil
local lastPhase = nil                    -- "out" | "loading" | "in" (for one-shot curtain sfx per phase)

-- openness: 0 = fully shut, 1 = fully open (halves off-screen)
local function draw_curtain(openness)
	if openness <= 0 then
		if tx_closed then tx_closed:Draw(0, 0) end   -- seamless closed image
		return
	end
	if tx_open == nil or tx_open.Width <= 0 then return end
	local off = math.floor(openness * CURTAIN_W)
	tx_open:DrawRect(-off,            0,         0, 0, CURTAIN_W, SCREEN_H)   -- left half slides left
	tx_open:DrawRect(CURTAIN_W + off, 0, CURTAIN_W, 0, CURTAIN_W, SCREEN_H)   -- right half slides right
end

local function phaseSfx(phase)
	if lastPhase ~= phase then
		lastPhase = phase
		if snd_curtain then snd_curtain:Play() end
	end
end

-- Close the curtain over the outgoing screen: t 0→1 = open → shut.
function fadeOut(t)
	phaseSfx("out")
	draw_curtain(1.0 - t)
end

-- Hold shut while the next stage loads behind it.
function loading(progress, elapsed)
	lastPhase = "loading"
	draw_curtain(0)
end

-- Part the curtain to reveal the loaded stage: t 0→1 = shut → open.
function fadeIn(t)
	phaseSfx("in")
	draw_curtain(t)
end

function onStart()
	-- Sync: Curtain_Open is drawn split (its Width is read), so it must be fully uploaded.
	tx_closed = TEXTURE:CreateTextureSync("Textures/Curtain.png")
	tx_open   = TEXTURE:CreateTextureSync("Textures/Curtain_Open.png")
	snd_curtain = SOUND:CreateSFX("Sounds/CurtainOpen.ogg")
end

function onDestroy()
	if tx_closed then tx_closed:Dispose(); tx_closed = nil end
	if tx_open then tx_open:Dispose(); tx_open = nil end
	if snd_curtain then snd_curtain:Dispose(); snd_curtain = nil end
end
