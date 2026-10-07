---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- coin_shop/keeper.lua: the shopkeeper's spot behind the desk and its speech bubble.
-- The keeper is one of the folders Textures/Keepers/0, 1, 2..., picked from the day when the shop opens (the same
-- one all day); each folder holds one picture per expression: Neutral, Happy, Surprised and Sad (.png), a missing
-- one showing Neutral. Every event picks a short line and an expression; the keeper hops (squash and stretch) when
-- the expression changes and nods when it keeps it, and the bubble types the line in letter by letter.

local Easing = require("Easing")

local K = {}

-- the stand-in's feet (bottom centre, behind the desk's front)
local FOOT_X, FOOT_Y = 1556, 845
-- the bubble's tail tip; the image centre sits at TIP + (BUB_DX, BUB_DY), its body centre BODY_DY above that
local TIP_X, TIP_Y = 1600, 354
local BUB_DX, BUB_DY = -108, -133
local BODY_DY = -32
local TEXT_W = 548
local CPS = 36
local HOLD, HOLD_PER_CHAR = 2.6, 0.045
local POP, CLOSE = 0.24, 0.18
local LOW_GAP = 1.0           -- a low-priority line waits this long after the last line finished typing

-- event -> expression, then the English lines (keys SHOP_TALK_<EVENT>_<n>)
local LINES = {
	greet     = { "happy", "Welcome! Fresh picks in today's issue.", "Oh, a customer! Take a look around.", "Hello there! Everything's on the page." },
	browse    = { "happy", "Ooh, that one's lovely.", "Good eye!", "A popular pick, that one." },
	featured  = { "surprised", "Today's special! Don't miss it.", "The star of this issue!" },
	coupon    = { "neutral", "Want a new page of goods?", "A fresh batch? It'll cost you." },
	backtab   = { "sad", "Leaving already?", "Done browsing?" },
	trybuy    = { "happy", "Shall I wrap it up?", "Excellent choice!", "Ready when you are!" },
	poor      = { "sad", "Hmm, you're a little short...", "Not enough coins, I'm afraid.", "Come back with a few more coins!" },
	bought    = { "happy", "Thank you kindly!", "Pleasure doing business!", "Enjoy it!" },
	soldout   = { "sad", "Sorry, that one's gone!", "All sold out, I'm afraid.", "Too late for that one!" },
	rerollask = { "surprised", "A whole new page? Sure!", "Shall I shuffle the stock?" },
	reroll    = { "surprised", "Fresh stock, coming right up!", "Let's see what we have now!", "Brand new picks!" },
	cancel    = { "neutral", "No rush, take your time.", "Maybe next time!" },
	leave     = { "happy", "See you soon!", "Come back anytime!", "Thanks for stopping by!" },
	idle      = { "neutral", "Take your time, no rush.", "Psst... the stock changes every day.", "Hmm hm hmm...", "Anything catch your eye?" },
	whose     = { "neutral", "Who's shopping today?", "Whose wallet are we using?" },
}
local KEEPERS = "Textures/Keepers/"
local FRAMES = { neutral = "Neutral", happy = "Happy", surprised = "Surprised", sad = "Sad" }

local ctx = nil
local lastPick = {}
local frames = {}        -- expression -> picture of today's keeper

K.expr = "neutral"
local hopT, hopAmp = nil, 0
local talk = nil         -- { text, lines = { { chars } }, total, t0, ends }
local bubble = { openT = nil, boingT = nil, closeT = nil }

function K.init(c)
	ctx = c
	K.reset()
end

function K.free()
	for k, tex in pairs(frames) do
		tex:Dispose()
		frames[k] = nil
	end
end

-- day: a whole number for the day (e.g. 20261007); only the picked keeper's pictures are loaded
function K.load(day)
	K.free()
	local n = 0
	while STORAGE:DirectoryExists(KEEPERS .. n) do n = n + 1 end
	if n == 0 then return end
	local h = day
	h = ((h ~ (h >> 16)) * 0x45d9f3b) & 0xffffffff
	h = ((h ~ (h >> 16)) * 0x45d9f3b) & 0xffffffff
	h = h ~ (h >> 16)
	local dir = KEEPERS .. (h % n) .. "/"
	for expr, name in pairs(FRAMES) do
		local path = dir .. name .. ".png"
		if STORAGE:FileExists(path) then frames[expr] = TEXTURE:CreateTexture(path) end
	end
end

function K.reset()
	K.expr = "neutral"
	hopT, hopAmp = nil, 0
	talk = nil
	bubble = { openT = nil, boingT = nil, closeT = nil }
end

local function utf8chars(s)
	local out = {}
	for ch in s:gmatch("[%z\1-\127\194-\244][\128-\191]*") do out[#out + 1] = ch end
	return out
end

-- the engine hands back a string[] (Length, 0-based)
local function lineList(arr)
	local out = {}
	if arr == nil then return out end
	local n = arr.Length or 0
	for i = 0, n - 1 do out[#out + 1] = arr[i] end
	return out
end

function K.text(event, i)
	local set = LINES[event]
	return ctx.tr("SHOP_TALK_" .. event:upper() .. "_" .. i, set[i + 1])
end

-- true while a line is still typing or was only just finished
function K.busy(now)
	if talk == nil or bubble.closeT ~= nil then return false end
	return now < talk.t0 + talk.total / CPS + LOW_GAP
end

-- low: browsing / idle chatter, skipped while another line is being said
function K.say(event, now, low)
	local set = LINES[event]
	if set == nil then return false end
	if low and K.busy(now) then return false end
	local n = #set - 1
	local i = math.random(1, n)
	if n > 1 and i == lastPick[event] then i = i % n + 1 end
	lastPick[event] = i
	local text = K.text(event, i)

	local expr = set[1]
	if expr ~= K.expr then
		K.expr = expr
		hopT, hopAmp = now, 1
	else
		hopT, hopAmp = now, 0.4
	end

	local lines = {}
	local total = 0
	for _, l in ipairs(lineList(ctx.font:WrapToLines(text, TEXT_W, 1))) do
		local chars = utf8chars(l)
		lines[#lines + 1] = chars
		total = total + #chars
	end
	local visible = bubble.openT ~= nil and bubble.closeT == nil
	talk = { text = text, event = event, lines = lines, total = total, t0 = now,
		ends = now + total / CPS + HOLD + total * HOLD_PER_CHAR }
	if visible then
		bubble.boingT = now
	else
		bubble.openT, bubble.boingT, bubble.closeT = now, nil, nil
	end
	return true
end

-- the current line (for checks)
function K.current()
	return talk and talk.text or nil, talk and talk.event or nil
end

local function hopShape(b, amp)
	if b == nil or b < 0 or b >= 0.62 then return 1, 1, 0 end
	if b < 0.08 then
		local k = Easing.outQuad(b / 0.08) * amp
		return 1 + 0.08 * k, 1 - 0.10 * k, 0
	end
	if b < 0.38 then
		local u = (b - 0.08) / 0.30
		local v = math.abs(math.cos(u * math.pi)) * amp
		return 1 - 0.06 * v, 1 + 0.09 * v, 32 * amp * math.sin(u * math.pi)
	end
	local d = (b - 0.38) / 0.24
	local e = (1 - d) * (1 - d) * math.cos(d * 3 * math.pi) * amp
	return 1 + 0.08 * e, 1 - 0.10 * e, 0
end

function K.drawBody(now)
	local tex = frames[K.expr] or frames.neutral
	if tex == nil then return end
	local sx, sy, lift = hopShape(hopT and (now - hopT) or nil, hopAmp)
	local breath = 1 + 0.008 * math.sin(now * 2 * math.pi / 3.4)
	tex:SetScale(sx, sy * breath)
	tex:DrawAtAnchor(FOOT_X, FOOT_Y - lift, "bottom")
	tex:SetScale(1, 1)
end

function K.drawBubble(now)
	local tex = ctx.tex.Bubble
	if tex == nil or talk == nil or bubble.openT == nil then return end
	if bubble.closeT == nil and now >= talk.ends then bubble.closeT = now end
	local s, op = 1, 1
	local a = now - bubble.openT
	if a < POP then
		s = 0.55 + 0.45 * Easing.outBack(a / POP, 2.2)
		op = math.min(1, a / 0.08)
	end
	if bubble.boingT ~= nil then
		local b = now - bubble.boingT
		if b < 0.22 then s = s * (1 + 0.06 * Easing.hump(b / 0.22)) end
	end
	if bubble.closeT ~= nil then
		local c = now - bubble.closeT
		if c >= CLOSE then
			talk, bubble.openT, bubble.closeT, bubble.boingT = nil, nil, nil, nil
			return
		end
		s = s * (1 - 0.12 * Easing.outQuad(c / CLOSE))
		op = op * (1 - c / CLOSE)
	end
	local fy = 2 * math.sin(now * 2 * math.pi / 2.7)
	local cx, cy = TIP_X + BUB_DX * s, TIP_Y + fy + BUB_DY * s
	tex:SetScale(s, s)
	tex:SetOpacity(op)
	tex:DrawAtAnchor(cx, cy, "center")
	tex:SetScale(1, 1)
	tex:SetOpacity(1)

	-- the typed part of the line; every line keeps the place its full text takes, centred in the body
	local f = ctx.font
	local lh = f.LineHeight * s
	local top = cy + BODY_DY * s - (#talk.lines * lh) / 2
	local shown = math.floor((now - talk.t0) * CPS)
	for i, chars in ipairs(talk.lines) do
		if shown <= 0 then break end
		local k = math.min(#chars, shown)
		shown = shown - k
		if chars.w == nil then chars.w = f:Measure(table.concat(chars)) end
		local left = cx - chars.w * s / 2 - 25 * s
		f:Draw(table.concat(chars, "", 1, k), left, top + (i - 1) * lh, ctx.col.ink, ctx.col.clear, op, s, 0, "topleft")
	end
end

return K
