---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- coin_shop/keeper.lua: the shopkeeper's spot behind the desk and its speech bubble.
-- The keeper is one of the part-timers in Textures/Keepers/0, 1, 2..., picked from the day when the shop opens (the
-- same one all day); each folder holds one picture per expression: Neutral, Happy, Surprised and Sad (.png), a
-- missing one showing Neutral. Every event picks one of that part-timer's own lines and its expression (PART_TIMERS
-- below); the keeper hops (squash and stretch) when the expression changes and nods when it keeps it, and the
-- bubble types the line in letter by letter.

local Easing = require("Easing")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local K = {}

local Bubble, Talk, Hop, BubbleAnim = Layout.bubble, Animation.keeper, Animation.hop, Animation.bubble
-- the stand-in's feet (bottom centre, behind the desk's front)
local FOOT_X, FOOT_Y = Layout.keeper.foot.x, Layout.keeper.foot.y
-- the bubble's tail tip; the image centre sits at TIP + (BUB_DX, BUB_DY), its body centre BODY_DY above that
local TIP_X, TIP_Y = Bubble.tip_x, Bubble.tip_y
local BUB_DX, BUB_DY = Bubble.dx, Bubble.dy
local BODY_DY = Bubble.body_dy
local TEXT_W = Bubble.text_w
local CPS = Talk.chars_per_s
local HOLD, HOLD_PER_CHAR = Talk.hold_s, Talk.hold_per_char_s
local POP, CLOSE = BubbleAnim.pop_s, BubbleAnim.close_s
local LOW_GAP = Talk.low_gap_s           -- a low-priority line waits this long after the last line finished typing

-- part-timer 0, Calla: event -> expression, then her English lines; quiet and tired, says as little as she
-- can (keys SHOP_TALK_CALLA_<EVENT>_<n>)
local CALLA = {
	greet     = { "neutral", "...Welcome.", "Oh. Hi. Everything's on the page.", "Welcome in. Look around, if you want." },
	browse    = { "neutral", "That one's nice. I think.", "People like that one.", "Mm. Good pick." },
	featured  = { "neutral", "Today's special. ...So I'm told.", "That's the featured one. It's, um... good." },
	coupon    = { "neutral", "New picks cost extra. Just so you know.", "A new page? ...If you want." },
	backtab   = { "sad", "Leaving?", "Done already? ...Okay." },
	trybuy    = { "neutral", "That one? Okay.", "I'll ring it up.", "Sure. One sec." },
	poor      = { "sad", "You're a bit short. ...Sorry.", "Not enough coins. I could-- Tss... Never mind.", "It's a lot, huh. I get it." },
	bought    = { "happy", "Thanks. ...Really.", "Here. Hope you like it.", "Thank you for shopping wi-- ...Thanks." },
	soldout   = { "sad", "That one's gone. Sorry.", "Sold out. ...Yeah.", "Someone got it first." },
	rerollask = { "surprised", "All of it? ...Okay.", "A whole new page? Sure." },
	reroll    = { "neutral", "...There. New stuff.", "Okay. Fresh page.", "Restocked. That's... all." },
	cancel    = { "neutral", "Okay. No rush.", "That's fine." },
	leave     = { "sad", "Bye. ...Take care.", "See you.", "Thanks for coming by." },
	idle      = { "neutral", "Hah... Sorry. Long day.", "Take your time. I'm not going anywhere.", "Nice song. You could almost-- ...Never mind.", "Anything you want? ...No pressure.", "My shift ends at-- Tss... Doesn't matter." },
	whose     = { "neutral", "Whose coins today?", "Who's paying?" },
}
-- folder number -> the key prefix of its lines, how much it hops, its lines; a folder missing here uses 0's
local PART_TIMERS = {
	[0] = { prefix = "SHOP_TALK_CALLA_", hop = Hop.calla, lines = CALLA },
}
local KEEPERS = "Textures/Keepers/"
local FRAMES = { neutral = "Neutral", happy = "Happy", surprised = "Surprised", sad = "Sad" }

local ctx = nil
local lastPick = {}
local frames = {}        -- expression -> picture of today's keeper
local who = PART_TIMERS[0]   -- today's part-timer
K.id = 0

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
	K.id, who, lastPick = 0, PART_TIMERS[0], {}
	local n = 0
	while STORAGE:DirectoryExists(KEEPERS .. n) do n = n + 1 end
	if n == 0 then return end
	local h = day
	h = ((h ~ (h >> 16)) * 0x45d9f3b) & 0xffffffff
	h = ((h ~ (h >> 16)) * 0x45d9f3b) & 0xffffffff
	h = h ~ (h >> 16)
	K.id = h % n
	who = PART_TIMERS[K.id] or PART_TIMERS[0]
	local dir = KEEPERS .. K.id .. "/"
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
	local set = who.lines[event]
	return ctx.tr(who.prefix .. event:upper() .. "_" .. i, set[i + 1])
end

-- true while a line is still typing or was only just finished
function K.busy(now)
	if talk == nil or bubble.closeT ~= nil then return false end
	return now < talk.t0 + talk.total / CPS + LOW_GAP
end

-- low: browsing / idle chatter, skipped while another line is being said
function K.say(event, now, low)
	local set = who.lines[event]
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
		hopT, hopAmp = now, who.hop
	else
		hopT, hopAmp = now, Hop.nod_factor * who.hop
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
	local riseEnd = Hop.squash_s + Hop.rise_s
	if b == nil or b < 0 or b >= riseEnd + Hop.settle_s then return 1, 1, 0 end
	if b < Hop.squash_s then
		local k = Easing.outQuad(b / Hop.squash_s) * amp
		return 1 + Hop.squash_x * k, 1 - Hop.squash_y * k, 0
	end
	if b < riseEnd then
		local u = (b - Hop.squash_s) / Hop.rise_s
		local v = math.abs(math.cos(u * math.pi)) * amp
		return 1 - Hop.stretch_x * v, 1 + Hop.stretch_y * v, Hop.height_px * amp * math.sin(u * math.pi)
	end
	local d = (b - riseEnd) / Hop.settle_s
	local e = (1 - d) * (1 - d) * math.cos(d * Hop.settle_wobbles * math.pi) * amp
	return 1 + Hop.settle_x * e, 1 - Hop.settle_y * e, 0
end

function K.drawBody(now)
	local tex = frames[K.expr] or frames.neutral
	if tex == nil then return end
	local sx, sy, lift = hopShape(hopT and (now - hopT) or nil, hopAmp)
	local breath = 1 + Talk.breath.amp * math.sin(now * 2 * math.pi / Talk.breath.period_s)
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
		s = BubbleAnim.start_scale + (1 - BubbleAnim.start_scale) * Easing.outBack(a / POP, BubbleAnim.overshoot)
		op = math.min(1, a / BubbleAnim.fade_s)
	end
	if bubble.boingT ~= nil then
		local b = now - bubble.boingT
		if b < BubbleAnim.boing_s then s = s * (1 + BubbleAnim.boing_grow * Easing.hump(b / BubbleAnim.boing_s)) end
	end
	if bubble.closeT ~= nil then
		local c = now - bubble.closeT
		if c >= CLOSE then
			talk, bubble.openT, bubble.closeT, bubble.boingT = nil, nil, nil, nil
			return
		end
		s = s * (1 - BubbleAnim.close_shrink * Easing.outQuad(c / CLOSE))
		op = op * (1 - c / CLOSE)
	end
	local fy = BubbleAnim.float_px * math.sin(now * 2 * math.pi / BubbleAnim.float_period_s)
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
		local left = cx - chars.w * s / 2 - Bubble.text_pad * s
		f:Draw(table.concat(chars, "", 1, k), left, top + (i - 1) * lh, ctx.col.ink, ctx.col.clear, op, s, 0, "topleft")
	end
end

return K
