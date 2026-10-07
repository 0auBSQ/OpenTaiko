---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- coin_shop/magazine.lua: the shop drawn as a front-facing catalog page. The page floats and turns very slightly;
-- every listing on it (the featured item, the catalog entries, the reroll coupon and the back note) has its own
-- small tilt and sway, and reacts to being selected, bought, sold out or out of reach.
--
-- Positions are in page pixels (0..PW, 0..PH, the Page.png content); a "frame" is a placed point with a
-- rotation (degrees, counter-clockwise like SetRotation) and a scale, and children are placed in their parent's
-- frame, so the whole page moves and turns as one.

local Easing = require("Easing")

local M = {}

local PW, PH = 1160, 990
local PAGE_X, PAGE_Y = 614, 541
local SHADOW_DX, SHADOW_DY = 12, 16

local HERO = { x = 221, y = 493, w = 390, h = 670 }
local GRID = {
	[4] = { cols = { 607.5, 966.5 }, rows = { 319.5, 663.5 }, w = 335, h = 323 },
	[6] = { cols = { 202.7, 580, 957.3 }, rows = { 319.5, 663.5 }, w = 353, h = 323 },
}
local COIN_PX = 42         -- the shared coin's drawn width at scale 1
local COUPON = { x = 930, y = 928, w = 390, h = 128 }
local NOTE = { x = 112, y = 936, w = 170, h = 150 }
local MAST_X, MAST_Y = 34, 69
local ISSUE_X = 1133

local ctx = nil
local list = {}            -- the listings in reading order: { id, kind, slot, x, y, w, h }
local byId = {}
local st = {}              -- per listing id: animation state
local page = { shuffleT = nil, shakeT = nil }
local lastFrame = nil      -- the page frame of the last draw (mouse hit tests)
local nudges = {}

function M.init(c)
	ctx = c
	list, byId, st = {}, {}, {}
	page = { shuffleT = nil, shakeT = nil }
	lastFrame = nil
end

-- ── frames ───────────────────────────────────────────────────────────────────

local function frame(x, y, rot, k)
	local r = math.rad(rot)
	return { x = x, y = y, rot = rot, c = math.cos(r), s = math.sin(r), k = k or 1 }
end

local function child(f, ox, oy, rot, k)
	return frame(f.x + (ox * f.c + oy * f.s) * f.k, f.y + (-ox * f.s + oy * f.c) * f.k, f.rot + (rot or 0), f.k * (k or 1))
end

local function sprite(tex, f, sx, sy, op, r, g, b)
	if tex == nil then return end
	tex:SetScale(f.k * sx, f.k * (sy or sx))
	tex:SetRotation(f.rot)
	if op ~= nil then tex:SetOpacity(op) end
	if r ~= nil then tex:SetColor(r, g, b) end
	tex:DrawAtAnchor(f.x, f.y, "center")
	tex:SetScale(1, 1)
	tex:SetRotation(0)
	if op ~= nil then tex:SetOpacity(1) end
	if r ~= nil then tex:SetColor(1, 1, 1) end
end

-- the "Coin" shared texture, COIN_PX * k wide
local function coin(f, k, op)
	local tex = ctx.coin
	if tex == nil or tex.Width <= 0 then return end
	sprite(tex, f, k * COIN_PX / tex.Width, nil, op)
end

-- the glyph box carries padding under the ink: centred text moves down by this to centre its ink
local function nudge(font)
	local n = nudges[font]
	if n == nil then
		n = math.floor((font.BoxHeight - math.ceil(font.LineHeight)) / 2) - 1
		nudges[font] = n
	end
	return n
end
M.nudge = nudge

-- anchor "center", "left" or "right" (vertically centred); maxInk = the widest the ink may get (0 = no limit):
-- a longer text first shrinks (down to FIT_MIN of its size), then squeezes
local FIT_MIN = 0.72
local function text(font, str, f, col, scale, maxInk, anchor, op)
	if str == nil or str == "" then return end
	local s = f.k * (scale or 1)
	if maxInk and maxInk > 0 then
		local ink = font:Measure(str) * (scale or 1)
		if ink > maxInk then s = s * math.max(FIT_MIN, maxInk / ink) end
	end
	local n = nudge(font) * s
	local x, y = f.x + n * f.s, f.y + n * f.c
	local maxW = (maxInk and maxInk > 0) and (maxInk * f.k + 50 * s) or 0
	font:Draw(str, x, y, col, ctx.col.clear, op or 1, s, maxW, anchor or "center", 0, f.rot)
end

-- a coin and the amount next to it, centred on f
local function amount(font, n, f, col, scale, op)
	local str = tostring(n)
	scale = scale or 1
	local w = font:Measure(str) * scale
	local cw, gap = 34 * scale, 6 * scale
	local total = cw + gap + w
	coin(child(f, -total / 2 + cw / 2, 0), 0.85 * scale, op)
	text(font, str, child(f, -total / 2 + cw + gap + w / 2, 0), col, scale, 0, "center", op)
end

local function fitPicture(icon, f, boxW, boxH, op, dim)
	if icon == nil then return end
	local w, h = icon.Width, icon.Height
	if w == nil or w <= 0 or h <= 0 then return end
	local s = math.min(boxW / w, boxH / h)
	if dim then
		sprite(icon, f, s, s, 0.45 * (op or 1), 0.7, 0.7, 0.7)
	else
		sprite(icon, f, s, s, op)
	end
end

-- a title plate is drawn by the nameplate module, never scaled or turned: it sits level at its listing's point,
-- and shows once its listing has popped in
local function plateFrame(f)
	return frame(f.x, f.y, 0, 1)
end

local function plateOpacity(s, now, op)
	if s.popT == nil then return op end
	return op * math.max(0, math.min(1, (now - s.popT - 0.3) / 0.12))
end

local function plate(item, pf, op, dim)
	if op <= 0 then return end
	local a = math.floor(255 * op * (dim and 0.45 or 1))
	NAMEPLATE:DrawNameplateTitleById(item.RefInt, pf.x + 15, pf.y - 45, a, ctx.text)
end

-- ── layout ───────────────────────────────────────────────────────────────────

local function rnd(a, b) return a + (b - a) * math.random() end

local function freshState(id, kind)
	local s = {
		sel = 0, selT = nil, popT = nil, popSpin = 0,
		buyT = nil, stampT = nil, poorT = nil, nopeT = nil,
		ph1 = rnd(0, 6.28), ph2 = rnd(0, 6.28), ph3 = rnd(0, 6.28),
		f1 = rnd(0.11, 0.2), f2 = rnd(0.09, 0.17), f3 = rnd(0.12, 0.22),
		tapeRot = rnd(-7, 7), tagRot = rnd(-4, 4), splashRot = rnd(-8, 8),
	}
	if kind == "big" then s.tilt = rnd(-0.6, 0.6)
	elseif kind == "normal" then s.tilt = rnd(-1.3, 1.3)
	elseif kind == "reroll" then s.tilt = rnd(-2.4, -1.2)
	else s.tilt = rnd(3, 5) end
	s.dx, s.dy = rnd(-3, 3), rnd(-3, 3)
	return s
end

local function add(id, kind, slot, x, y, w, h)
	local e = { id = id, kind = kind, slot = slot, x = x, y = y, w = w, h = h }
	list[#list + 1] = e
	byId[id] = e
	if st[id] == nil then st[id] = freshState(id, kind) end
end

-- hasBig: the featured listing is on the page (then 4 catalog entries, else 6)
function M.layout(hasBig, reshuffle)
	list, byId = {}, {}
	if reshuffle then st = {} end
	local cells = hasBig and 4 or 6
	if hasBig then add("big", "big", 7, HERO.x, HERO.y, HERO.w, HERO.h) end
	local g = GRID[cells]
	local k = 0
	for r = 1, #g.rows do
		for c = 1, #g.cols do
			k = k + 1
			add("n" .. k, "normal", k, g.cols[c], g.rows[r], g.w, g.h)
		end
	end
	add("reroll", "reroll", nil, COUPON.x, COUPON.y, COUPON.w, COUPON.h)
	add("back", "back", nil, NOTE.x, NOTE.y, NOTE.w, NOTE.h)
	return M.ids()
end

function M.ids()
	local ids = {}
	for i, e in ipairs(list) do ids[i] = e.id end
	return ids
end

function M.entry(id) return byId[id] end

-- ── events ───────────────────────────────────────────────────────────────────

function M.select(id, now)
	local s = st[id]
	if s then s.selT = now end
end

-- the listings pop onto the page one after the other
function M.popAll(now)
	for i, e in ipairs(list) do
		local s = st[e.id]
		s.popT = now + 0.05 * (i - 1)
		s.popSpin = rnd(-7, 7)
		s.stampT, s.buyT, s.poorT, s.nopeT = nil, nil, nil, nil
	end
end

function M.shuffle(now) page.shuffleT = now end

-- kind: "bought" (soldNow = it just sold out), "poor", "soldout"
function M.poke(id, kind, now, soldNow)
	local s = st[id]
	if s == nil then return end
	if kind == "bought" then
		s.buyT = now
		if soldNow then s.stampT = now; page.shakeT = now + 0.14 end
	elseif kind == "poor" then
		s.poorT = now
	elseif kind == "soldout" then
		s.nopeT = now
	end
end

-- ── drawing ──────────────────────────────────────────────────────────────────

local TAU = 2 * math.pi
local sin, exp = math.sin, math.exp

local function pageFrame(now)
	local fx = 3.0 * sin(TAU * 0.071 * now + 0.3) + 1.5 * sin(TAU * 0.173 * now + 1.9)
	local fy = 3.6 * sin(TAU * 0.089 * now + 2.2) + 1.6 * sin(TAU * 0.211 * now + 0.7)
	local r = 0.30 * sin(TAU * 0.061 * now + 1.1) + 0.15 * sin(TAU * 0.149 * now + 2.6)
	if page.shuffleT ~= nil then
		local a = now - page.shuffleT
		if a >= 0 and a < 1.6 then r = r + 1.0 * exp(-4.5 * a) * sin(13 * a) end
	end
	if page.shakeT ~= nil then
		local a = now - page.shakeT
		if a >= 0 and a < 0.6 then
			fx = fx + 5 * exp(-12 * a) * sin(70 * a)
			fy = fy + 3 * exp(-12 * a) * sin(55 * a + 1)
		end
	end
	return fx, fy, r
end

-- the listing's frame at this moment, plus its opacity (nil while it has not popped in yet)
local function listingFrame(pf, e, s, now, dt, selected)
	local target = selected and 1 or 0
	s.sel = s.sel + (target - s.sel) * math.min(1, dt * 12)
	local rot = s.tilt + 0.22 * sin(TAU * s.f1 * now + s.ph1)
	local ox = s.dx + 1.2 * sin(TAU * s.f2 * now + s.ph2)
	local oy = s.dy + 1.4 * sin(TAU * s.f3 * now + s.ph3) - 8 * s.sel
	local k = 1 + 0.045 * s.sel
	local op = 1
	if s.popT ~= nil then
		local a = now - s.popT
		if a < 0 then return nil end
		if a < 0.4 then
			k = k * (0.6 + 0.4 * Easing.outBack(a / 0.32, 2.0))
			rot = rot + (1 - Easing.outCubic(a / 0.32)) * s.popSpin
			op = math.min(1, a / 0.12)
		end
	end
	if selected and s.selT ~= nil then
		local a = now - s.selT
		if a >= 0 and a < 1 then rot = rot + 2.6 * exp(-6 * a) * sin(26 * a) end
	end
	if s.buyT ~= nil then
		local a = now - s.buyT
		if a >= 0 and a < 1.2 then k = k * (1 + 0.13 * exp(-7 * a) * sin(16 * a)) end
	end
	if s.poorT ~= nil then
		local a = now - s.poorT
		if a >= 0 and a < 1 then ox = ox + 9 * exp(-9 * a) * sin(48 * a) end
	end
	if s.nopeT ~= nil then
		local a = now - s.nopeT
		if a >= 0 and a < 1 then ox = ox + 5 * exp(-9 * a) * sin(44 * a) end
	end
	return child(pf, e.x - PW / 2 + ox, e.y - PH / 2 + oy, rot, k), op
end

-- the sold-out stamp, slammed down when the item has just sold out and shaken when someone still tries
local function stamp(f, s, now, op)
	local k, rot, sop = 1, 0, op
	if s.stampT ~= nil then
		local a = now - s.stampT
		if a < 0 then return end
		if a < 0.14 then
			k = 1 + 1.4 * (1 - Easing.outCubic(a / 0.14))
			sop = op * math.min(1, a / 0.06)
		elseif a < 0.8 then
			k = 1 + 0.05 * exp(-10 * (a - 0.14)) * sin(40 * (a - 0.14))
		end
	end
	if s.nopeT ~= nil then
		local a = now - s.nopeT
		if a >= 0 and a < 1 then rot = 6 * exp(-8 * a) * sin(40 * a) end
	end
	local sf = child(f, 0, 0, rot, k)
	sprite(ctx.tex.Stamp, sf, 1, 1, sop)
	text(ctx.fonts.big, ctx.tr("SHOP_UI_SOLD_OUT", "SOLD OUT"), sf, ctx.col.stamp, 1, 250, "center", sop)
end

local function stockBadge(f, stock, op)
	if stock == nil or stock <= 1 then return end
	sprite(ctx.tex.Badge, f, 1, 1, op)
	text(ctx.fonts.small, "x" .. tostring(stock), f, ctx.col.white, 1, 60, "center", op)
end

local function priceColor(price)
	return (ctx.coins() < price) and ctx.col.red or ctx.col.ink
end

-- the soft shadow under a piece of a listing that is lifted off the page
local function lift(f, w, h, s, op)
	if s.sel > 0.01 then sprite(ctx.tex.EntryShadow, child(f, 7, 11), w / 340, h / 330, 0.5 * s.sel * op) end
end

local function drawBurst(f, now, op)
	local bf = child(f, -96, -296, -9 + 3 * sin(TAU * now / 1.7))
	sprite(ctx.tex.Burst, bf, 1, 1, op)
	text(ctx.fonts.mid, ctx.tr("SHOP_UI_FEATURED", "Featured!"), bf, ctx.col.white, 1, 170, "center", op)
end

-- burstLast: the burst is left out, to be stuck on after the selection marker
local function drawHero(f, e, s, item, icon, now, op, burstLast)
	lift(f, e.w, e.h, s, op)
	sprite(ctx.tex.HeroPanel, f, 1, 1, op)
	local photo = child(f, 0, -100, 0)
	sprite(ctx.tex.PhotoBig, photo, 1, 1, op)
	local sold = item == nil or item.SoldOut
	if item ~= nil then
		if item.Type == "nameplate" then plate(item, plateFrame(photo), plateOpacity(s, now, op), sold)
		else fitPicture(icon, photo, 300, 300, op, sold) end
	end
	sprite(ctx.tex.Tape, child(photo, -168, -170, 42), 1, 1, op)
	sprite(ctx.tex.Tape, child(photo, 168, -170, -42), 1, 1, op)
	if not burstLast then drawBurst(f, now, op) end
	if item ~= nil then
		text(ctx.fonts.big, item.LocalizedName, child(f, 0, 185), ctx.col.ink, 1, 350, "center", op)
		if not sold then
			local sf = child(f, 118, 50, 8 + 2 * sin(TAU * now / 2.3 + 1))
			sprite(ctx.tex.Sticker, sf, 1, 1, op)
			coin(child(sf, 0, -34), 1, op)
			local str = tostring(item.Price)
			local fit = math.min(1, 116 / math.max(1, ctx.fonts.big:Measure(str)))
			text(ctx.fonts.big, str, child(sf, 0, 14), priceColor(item.Price), fit, 0, "center", op)
			stockBadge(child(f, 162, -262), item.Stock, op)
		end
	end
	if sold then stamp(child(photo, 0, 0, 14), s, now, op) end
end

-- the pastel blob behind each catalog entry's picture
local SPLASH = { { 0.82, 0.94, 0.88 }, { 1, 0.88, 0.80 }, { 0.90, 0.86, 0.98 }, { 0.84, 0.92, 1 }, { 1, 0.95, 0.78 }, { 1, 0.86, 0.90 } }

local function drawCell(f, e, s, item, icon, now, op)
	local c = SPLASH[(e.slot - 1) % #SPLASH + 1]
	sprite(ctx.tex.Splash, child(f, -24, -26, s.splashRot), 1.08, 1.08, op, c[1], c[2], c[3])
	local photo = child(f, 0, -50, 0)
	local sold = item == nil or item.SoldOut
	if item ~= nil and item.Type == "nameplate" then
		-- a title plate is wider than the photo: it sits on the page as a cut-out, taped by its top edge; its
		-- shadow and tape go with the plate
		photo = child(f, 0, -18, 0)
		local pf, pop = plateFrame(photo), plateOpacity(s, now, op)
		lift(pf, 330, 81, s, pop)
		plate(item, pf, pop, sold)
		sprite(ctx.tex.Tape, child(pf, 0, -40, s.tapeRot), 1, 1, pop)
	else
		lift(photo, 290, 190, s, op)
		sprite(ctx.tex.PhotoSmall, photo, 1, 1, op)
		if item ~= nil then fitPicture(icon, photo, 250, 150, op, sold) end
		sprite(ctx.tex.Tape, child(photo, 0, -98, s.tapeRot), 1, 1, op)
	end
	if item ~= nil then
		text(ctx.fonts.mid, item.LocalizedName, child(f, 0, 69), ctx.col.ink, 1, e.w - 30, "center", op)
		if not sold then
			local tf = child(f, 0, 120, s.tagRot)
			sprite(ctx.tex.Tag, tf, 1, 1, op)
			amount(ctx.fonts.mid, item.Price, child(tf, 14, 0), priceColor(item.Price), 1, op)
			stockBadge(child(f, 130, -134), item.Stock, op)
		end
	end
	if sold then stamp(child(photo, 0, 0, 12, 0.82), s, now, op) end
end

local function drawCoupon(f, e, s, now, op)
	lift(f, e.w, e.h, s, op)
	sprite(ctx.tex.Coupon, f, 1, 1, op)
	local price = ctx.rerollPrice()
	text(ctx.fonts.mid, ctx.tr("SHOP_UI_REROLL", "New picks"), child(f, 52, -24), ctx.col.red, 1, 260, "center", op)
	amount(ctx.fonts.mid, price, child(f, 52, 26), priceColor(price), 1, op)
end

local function drawNote(f, e, s, now, op)
	lift(f, e.w, e.h, s, op)
	sprite(ctx.tex.Note, f, 1, 1, op)
	text(ctx.fonts.big, ctx.tr("SHOP_UI_BACK", "Back"), child(f, -4, 36), ctx.col.ink, 1, 140, "center", op)
end

local MARKER = { big = "MarkerTall", normal = "Marker", reroll = "MarkerWide", back = "MarkerNote" }

local function drawListing(pf, e, now, dt, selected, view)
	local s = st[e.id]
	local f, op = listingFrame(pf, e, s, now, dt, selected)
	if f == nil then return nil end
	if e.kind == "big" then drawHero(f, e, s, view.big, view.icons[5], now, op, selected)
	elseif e.kind == "normal" then drawCell(f, e, s, view.normals[e.slot], view.icons[e.slot], now, op)
	elseif e.kind == "reroll" then drawCoupon(f, e, s, now, op)
	else drawNote(f, e, s, now, op) end
	return f, op
end

local function drawMarker(f, e, now)
	local s = st[e.id]
	local a = s.selT and (now - s.selT) or 1
	local k = 0.86 + 0.14 * Easing.outBack(math.min(1, a / 0.2), 2.2)
	local mf = child(f, 0, 0, -2 + 0.8 * sin(TAU * now / 2.3), k)
	sprite(ctx.tex[MARKER[e.kind]], mf, 1, 1, math.min(1, a / 0.06))
end

-- view: { ready, big, normals, icons, selected }
function M.draw(now, dt, view)
	local fx, fy, rot = pageFrame(now)
	local pf = frame(PAGE_X + fx, PAGE_Y + fy, rot, 1)
	lastFrame = pf
	sprite(ctx.tex.PageShadow, frame(PAGE_X + fx * 0.6 + SHADOW_DX, PAGE_Y + fy * 0.6 + SHADOW_DY, rot, 1), 1)
	sprite(ctx.tex.Page, pf, 1)
	text(ctx.fonts.mast, ctx.tr("SHOP_UI_MASTHEAD", "The Daily Shop"), child(pf, MAST_X - PW / 2, MAST_Y - PH / 2), ctx.col.cream, 1, 640, "left")
	text(ctx.fonts.small, ctx.tr("SHOP_UI_ISSUE", "New picks every day!"), child(pf, ISSUE_X - PW / 2, MAST_Y - PH / 2), ctx.col.gold, 1, 380, "right")
	if not view.ready then return end
	local selE = nil
	for _, e in ipairs(list) do
		if e.id == view.selected then selE = e
		else drawListing(pf, e, now, dt, false, view) end
	end
	if selE ~= nil then
		local f, op = drawListing(pf, selE, now, dt, true, view)
		if f ~= nil then
			drawMarker(f, selE, now)
			if selE.kind == "big" then drawBurst(f, now, op) end
		end
	end
end

-- the listing under a screen point (the page as last drawn), or nil
function M.hit(mx, my)
	local pf = lastFrame
	if pf == nil then return nil end
	for i = #list, 1, -1 do
		local e = list[i]
		local s = st[e.id]
		local f = child(pf, e.x - PW / 2 + s.dx, e.y - PH / 2 + s.dy, s.tilt)
		local dx, dy = mx - f.x, my - f.y
		local lx, ly = dx * f.c - dy * f.s, dx * f.s + dy * f.c
		if math.abs(lx) <= e.w / 2 and math.abs(ly) <= e.h / 2 then return e.id end
	end
	return nil
end

return M
