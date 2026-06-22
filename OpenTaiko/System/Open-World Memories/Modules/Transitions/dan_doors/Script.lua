---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- dan_doors transition — the dan-select doors, as a transition. Selected by _title when entering the Fox Dojo
-- via Exit("stage", "dan_select", "dan_doors"). The Door.png left/right halves slide together to cover the
-- screen (fadeOut = closing over the title), hold shut while dan_select loads (loading), then slide apart to
-- reveal it (fadeIn = opening). Authored for a 1920x1080 skin.

FADE_OUT_SECONDS = 0.7   -- doors closing
FADE_IN_SECONDS  = 0.7   -- doors opening

local SCREEN_W, SCREEN_H = 1920, 1080
local tx_door = nil

-- Draw the doors at the given "openness" (0 = shut / fully covering, 1 = wide open / off-screen). The two
-- halves slide out to the sides as openness → 1, revealing the stage the engine drew behind them.
local function draw_doors(openness)
	if tx_door == nil then return end
	if openness <= 0 then tx_door:Draw(0, 0); return end                 -- fully shut: one seamless image
	local w = tx_door.Width  > 0 and tx_door.Width  or SCREEN_W
	local h = tx_door.Height > 0 and tx_door.Height or SCREEN_H
	local half = math.floor(w / 2)
	local off  = math.floor(openness * half)
	tx_door:DrawRectAtAnchor(-off,       0,    0, 0, half, h, "topleft")  -- left half slides left
	tx_door:DrawRectAtAnchor(half + off, 0, half, 0, half, h, "topleft")  -- right half slides right
end

-- Close the doors over the outgoing title: t 0→1 = open → shut.
function fadeOut(t)
	draw_doors(1.0 - t)
end

-- Hold the doors shut while dan_select loads behind them.
function loading(progress, elapsed)
	draw_doors(0)
end

-- Open the doors to reveal the loaded dan_select: t 0→1 = shut → open.
function fadeIn(t)
	draw_doors(t)
end

function onStart()
	-- Sync: the door is drawn split (its Width is read), so it must be fully uploaded, not a blank async stub.
	tx_door = TEXTURE:CreateTextureSync("Textures/Door.png")
end

function onDestroy()
	if tx_door ~= nil then tx_door:Dispose() ; tx_door = nil end
end
