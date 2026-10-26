---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- coin_shop/magazine.lua: the shop drawn as a front-facing catalog page. The page floats and turns very slightly;
-- every listing on it (the featured item, the catalog entries, the reroll coupon and the back note) has its own
-- small tilt and sway, and reacts to being selected, bought, sold out or out of reach.
--
-- Positions are in page pixels (0..PW, 0..PH, the Page.png content); a "frame" is a placed point with a
-- rotation (degrees, counter-clockwise like SetRotation) and a scale, and children are placed in their parent's
-- frame, so the whole page moves and turns as one.

local Easing = require("Easing")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Page = Layout.page
local PW, PH = Page.w, Page.h
local PAGE_X, PAGE_Y = Page.x, Page.y
local SHADOW_DX, SHADOW_DY = Page.shadow_dx, Page.shadow_dy

local Hero = Layout.hero
local Grid = Layout.grid
local COIN_PX = Layout.coin_px         -- the shared coin's drawn width at scale 1
local Coupon = Layout.coupon
local Note = Layout.note
local MAST_X, MAST_Y = Page.masthead.x, Page.masthead.y
local ISSUE_X = Page.issue.x
local Cell, Tilt, Sold, PlateOffset = Layout.cell, Layout.tilt, Layout.sold, Layout.title_plate_offset
local Listing, Pop, Stamp = Animation.listing, Animation.listing.pop, Animation.stamp

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
local FIT_MIN = Layout.text.fit_min
local function text(font, str, f, col, scale, maxInk, anchor, op)
	if str == nil or str == "" then return end
	local s = f.k * (scale or 1)
	if maxInk and maxInk > 0 then
		local ink = font:Measure(str) * (scale or 1)
		if ink > maxInk then s = s * math.max(FIT_MIN, maxInk / ink) end
	end
	local n = nudge(font) * s
	local x, y = f.x + n * f.s, f.y + n * f.c
	local maxW = (maxInk and maxInk > 0) and (maxInk * f.k + Layout.text.box_pad * s) or 0
	font:Draw(str, x, y, col, ctx.col.clear, op or 1, s, maxW, anchor or "center", 0, f.rot)
end

-- a coin and the amount next to it, centred on f
local function amount(font, n, f, col, scale, op)
	local str = tostring(n)
	scale = scale or 1
	local w = font:Measure(str) * scale
	local Amount = Layout.amount
	local cw, gap = Amount.coin_w * scale, Amount.gap * scale
	local total = cw + gap + w
	coin(child(f, -total / 2 + cw / 2, 0), Amount.coin_scale * scale, op)
	text(font, str, child(f, -total / 2 + cw + gap + w / 2, 0), col, scale, 0, "center", op)
end

local function fitPicture(icon, f, boxW, boxH, op, dim)
	if icon == nil then return end
	local w, h = icon.Width, icon.Height
	if w == nil or w <= 0 or h <= 0 then return end
	local s = math.min(boxW / w, boxH / h)
	if dim then
		sprite(icon, f, s, s, Sold.opacity * (op or 1), Sold.shade, Sold.shade, Sold.shade)
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
	return op * math.max(0, math.min(1, (now - s.popT - Animation.plate.delay_s) / Animation.plate.fade_s))
end

local function plate(item, pf, op, dim)
	if op <= 0 then return end
	local a = math.floor(255 * op * (dim and Sold.opacity or 1))
	NAMEPLATE:DrawNameplateTitleById(item.RefInt, pf.x + PlateOffset.x, pf.y + PlateOffset.y, a, ctx.text)
end

-- ── layout ───────────────────────────────────────────────────────────────────

local function rnd(a, b) return a + (b - a) * math.random() end

local Hz = Listing.sway_hz

local function freshState(id, kind)
	local s = {
		sel = 0, selT = nil, popT = nil, popSpin = 0,
		buyT = nil, stampT = nil, poorT = nil, nopeT = nil,
		ph1 = rnd(0, 6.28), ph2 = rnd(0, 6.28), ph3 = rnd(0, 6.28),
		f1 = rnd(Hz.rot[1], Hz.rot[2]), f2 = rnd(Hz.x[1], Hz.x[2]), f3 = rnd(Hz.y[1], Hz.y[2]),
		tapeRot = rnd(Tilt.tape_deg[1], Tilt.tape_deg[2]), tagRot = rnd(Tilt.tag_deg[1], Tilt.tag_deg[2]),
		splashRot = rnd(Tilt.splash_deg[1], Tilt.splash_deg[2]),
	}
	local range = Tilt.back_deg
	if kind == "big" then range = Tilt.big_deg
	elseif kind == "normal" then range = Tilt.normal_deg
	elseif kind == "reroll" then range = Tilt.reroll_deg end
	s.tilt = rnd(range[1], range[2])
	s.dx, s.dy = rnd(Tilt.offset_px[1], Tilt.offset_px[2]), rnd(Tilt.offset_px[1], Tilt.offset_px[2])
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
	if hasBig then add("big", "big", 7, Hero.x, Hero.y, Hero.w, Hero.h) end
	local g = hasBig and Grid.with_featured or Grid.full
	local k = 0
	for r = 1, #g.rows do
		for c = 1, #g.cols do
			k = k + 1
			add("n" .. k, "normal", k, g.cols[c], g.rows[r], g.w, g.h)
		end
	end
	add("reroll", "reroll", nil, Coupon.x, Coupon.y, Coupon.w, Coupon.h)
	add("back", "back", nil, Note.x, Note.y, Note.w, Note.h)
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
		s.popT = now + Pop.stagger_s * (i - 1)
		s.popSpin = rnd(-Pop.spin_deg, Pop.spin_deg)
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
		if soldNow then s.stampT = now; page.shakeT = now + Animation.page.shake.delay_s end
	elseif kind == "poor" then
		s.poorT = now
	elseif kind == "soldout" then
		s.nopeT = now
	end
end

-- ── drawing ──────────────────────────────────────────────────────────────────

local TAU = 2 * math.pi
local sin, exp = math.sin, math.exp

local PageAnim = Animation.page

-- the sum of the waves at this moment
local function waves(ws, now)
	local v = 0
	for _, w in ipairs(ws) do v = v + w.amp * sin(TAU * w.hz * now + w.phase_rad) end
	return v
end

-- amp * e^(-decay * a) * sin(rad_per_s * a + phase)
local function wobble(w, a, phase)
	return w.amp * exp(-w.decay * a) * sin(w.rad_per_s * a + (phase or 0))
end

local function pageFrame(now)
	local fx = waves(PageAnim.float_x, now)
	local fy = waves(PageAnim.float_y, now)
	local r = waves(PageAnim.float_rot, now)
	if page.shuffleT ~= nil then
		local a = now - page.shuffleT
		if a >= 0 and a < PageAnim.shuffle.dur_s then r = r + wobble(PageAnim.shuffle, a) end
	end
	if page.shakeT ~= nil then
		local a = now - page.shakeT
		local Shake = PageAnim.shake
		if a >= 0 and a < Shake.dur_s then
			fx = fx + wobble(Shake.x, a)
			fy = fy + wobble(Shake.y, a, Shake.y.phase_rad)
		end
	end
	return fx, fy, r
end

-- the listing's frame at this moment, plus its opacity (nil while it has not popped in yet)
local function listingFrame(pf, e, s, now, dt, selected)
	local target = selected and 1 or 0
	s.sel = s.sel + (target - s.sel) * math.min(1, dt * Listing.select_rate_per_s)
	local rot = s.tilt + Listing.sway_rot_deg * sin(TAU * s.f1 * now + s.ph1)
	local ox = s.dx + Listing.sway_x_px * sin(TAU * s.f2 * now + s.ph2)
	local oy = s.dy + Listing.sway_y_px * sin(TAU * s.f3 * now + s.ph3) - Listing.select_lift_px * s.sel
	local k = 1 + Listing.select_grow * s.sel
	local op = 1
	if s.popT ~= nil then
		local a = now - s.popT
		if a < 0 then return nil end
		if a < Pop.dur_s then
			k = k * (Pop.start_scale + (1 - Pop.start_scale) * Easing.outBack(a / Pop.ease_s, Pop.overshoot))
			rot = rot + (1 - Easing.outCubic(a / Pop.ease_s)) * s.popSpin
			op = math.min(1, a / Pop.fade_s)
		end
	end
	if selected and s.selT ~= nil then
		local a = now - s.selT
		if a >= 0 and a < Listing.select_wobble.dur_s then rot = rot + wobble(Listing.select_wobble, a) end
	end
	if s.buyT ~= nil then
		local a = now - s.buyT
		if a >= 0 and a < Listing.bought.dur_s then k = k * (1 + wobble(Listing.bought, a)) end
	end
	if s.poorT ~= nil then
		local a = now - s.poorT
		if a >= 0 and a < Listing.poor.dur_s then ox = ox + wobble(Listing.poor, a) end
	end
	if s.nopeT ~= nil then
		local a = now - s.nopeT
		if a >= 0 and a < Listing.sold_out.dur_s then ox = ox + wobble(Listing.sold_out, a) end
	end
	return child(pf, e.x - PW / 2 + ox, e.y - PH / 2 + oy, rot, k), op
end

-- the sold-out stamp, slammed down when the item has just sold out and shaken when someone still tries
local function stamp(f, s, now, op)
	local k, rot, sop = 1, 0, op
	if s.stampT ~= nil then
		local a = now - s.stampT
		if a < 0 then return end
		if a < Stamp.slam_s then
			k = 1 + Stamp.slam_grow * (1 - Easing.outCubic(a / Stamp.slam_s))
			sop = op * math.min(1, a / Stamp.fade_s)
		elseif a < Stamp.settle_end_s then
			k = 1 + wobble(Stamp.settle, a - Stamp.slam_s)
		end
	end
	if s.nopeT ~= nil then
		local a = now - s.nopeT
		if a >= 0 and a < Stamp.shake.dur_s then rot = wobble(Stamp.shake, a) end
	end
	local sf = child(f, 0, 0, rot, k)
	sprite(ctx.tex.Stamp, sf, 1, 1, sop)
	text(ctx.fonts.big, ctx.tr("SHOP_UI_SOLD_OUT", "SOLD OUT"), sf, ctx.col.stamp, 1, Layout.stamp.text_max_width, "center", sop)
end

local function stockBadge(f, stock, op)
	if stock == nil or stock <= 1 then return end
	sprite(ctx.tex.Badge, f, 1, 1, op)
	text(ctx.fonts.small, "x" .. tostring(stock), f, ctx.col.white, 1, Layout.stock_badge.text_max_width, "center", op)
end

local function priceColor(price)
	return (ctx.coins() < price) and ctx.col.red or ctx.col.ink
end

-- the soft shadow under a piece of a listing that is lifted off the page
local function lift(f, w, h, s, op)
	local Shadow = Layout.lift_shadow
	if s.sel > 0.01 then sprite(ctx.tex.EntryShadow, child(f, Shadow.x, Shadow.y), w / Shadow.tex_w, h / Shadow.tex_h, Shadow.opacity * s.sel * op) end
end

local function drawBurst(f, now, op)
	local Burst, Sway = Layout.burst, Animation.burst
	local bf = child(f, Burst.x, Burst.y, Burst.rot_deg + Sway.sway_deg * sin(TAU * now / Sway.period_s))
	sprite(ctx.tex.Burst, bf, 1, 1, op)
	text(ctx.fonts.mid, ctx.tr("SHOP_UI_FEATURED", "Featured!"), bf, ctx.col.white, 1, Burst.max_width, "center", op)
end

-- burstLast: the burst is left out, to be stuck on after the selection marker
local function drawHero(f, e, s, item, icon, now, op, burstLast)
	lift(f, e.w, e.h, s, op)
	sprite(ctx.tex.HeroPanel, f, 1, 1, op)
	local photo = child(f, 0, Hero.photo_y, 0)
	sprite(ctx.tex.PhotoBig, photo, 1, 1, op)
	local sold = item == nil or item.SoldOut
	if item ~= nil then
		if item.Type == "nameplate" then plate(item, plateFrame(photo), plateOpacity(s, now, op), sold)
		else fitPicture(icon, photo, Hero.picture_w, Hero.picture_h, op, sold) end
	end
	for _, t in ipairs(Hero.tapes) do
		sprite(ctx.tex.Tape, child(photo, t.x, t.y, t.rot_deg), 1, 1, op)
	end
	if not burstLast then drawBurst(f, now, op) end
	if item ~= nil then
		text(ctx.fonts.big, item.LocalizedName, child(f, 0, Hero.name_y), ctx.col.ink, 1, Hero.name_max_width, "center", op)
		if not sold then
			local St, Sway = Hero.sticker, Animation.sticker
			local sf = child(f, St.x, St.y, St.rot_deg + Sway.sway_deg * sin(TAU * now / Sway.period_s + Sway.phase_rad))
			sprite(ctx.tex.Sticker, sf, 1, 1, op)
			coin(child(sf, 0, St.coin_y), 1, op)
			local str = tostring(item.Price)
			local fit = math.min(1, St.price_max_width / math.max(1, ctx.fonts.big:Measure(str)))
			text(ctx.fonts.big, str, child(sf, 0, St.price_y), priceColor(item.Price), fit, 0, "center", op)
			stockBadge(child(f, Hero.badge.x, Hero.badge.y), item.Stock, op)
		end
	end
	if sold then stamp(child(photo, 0, 0, Hero.stamp_rot_deg), s, now, op) end
end

-- the pastel blob behind each catalog entry's picture
local SPLASH = Layout.splash_colors

local function drawCell(f, e, s, item, icon, now, op)
	local c = SPLASH[(e.slot - 1) % #SPLASH + 1]
	local Splash = Cell.splash
	sprite(ctx.tex.Splash, child(f, Splash.x, Splash.y, s.splashRot), Splash.scale, Splash.scale, op, c[1], c[2], c[3])
	local photo = child(f, 0, Cell.photo_y, 0)
	local sold = item == nil or item.SoldOut
	if item ~= nil and item.Type == "nameplate" then
		-- a title plate is wider than the photo: it sits on the page as a cut-out, taped by its top edge; its
		-- shadow and tape go with the plate
		photo = child(f, 0, Cell.plate_y, 0)
		local pf, pop = plateFrame(photo), plateOpacity(s, now, op)
		lift(pf, Cell.plate_shadow.w, Cell.plate_shadow.h, s, pop)
		plate(item, pf, pop, sold)
		sprite(ctx.tex.Tape, child(pf, 0, Cell.plate_tape_y, s.tapeRot), 1, 1, pop)
	else
		lift(photo, Cell.photo_shadow.w, Cell.photo_shadow.h, s, op)
		sprite(ctx.tex.PhotoSmall, photo, 1, 1, op)
		if item ~= nil then fitPicture(icon, photo, Cell.picture.w, Cell.picture.h, op, sold) end
		sprite(ctx.tex.Tape, child(photo, 0, Cell.tape_y, s.tapeRot), 1, 1, op)
	end
	if item ~= nil then
		text(ctx.fonts.mid, item.LocalizedName, child(f, 0, Cell.name_y), ctx.col.ink, 1, e.w - Cell.name_inset, "center", op)
		if not sold then
			local tf = child(f, 0, Cell.tag_y, s.tagRot)
			sprite(ctx.tex.Tag, tf, 1, 1, op)
			amount(ctx.fonts.mid, item.Price, child(tf, Cell.price_dx, 0), priceColor(item.Price), 1, op)
			stockBadge(child(f, Cell.badge.x, Cell.badge.y), item.Stock, op)
		end
	end
	if sold then stamp(child(photo, 0, 0, Cell.stamp_rot_deg, Cell.stamp_scale), s, now, op) end
end

local function drawCoupon(f, e, s, now, op)
	lift(f, e.w, e.h, s, op)
	sprite(ctx.tex.Coupon, f, 1, 1, op)
	local price = ctx.rerollPrice()
	local Title, Price = Coupon.title, Coupon.price
	text(ctx.fonts.mid, ctx.tr("SHOP_UI_REROLL", "New picks"), child(f, Title.x, Title.y), ctx.col.red, 1, Title.max_width, "center", op)
	amount(ctx.fonts.mid, price, child(f, Price.x, Price.y), priceColor(price), 1, op)
end

local function drawNote(f, e, s, now, op)
	lift(f, e.w, e.h, s, op)
	sprite(ctx.tex.Note, f, 1, 1, op)
	local Label = Note.label
	text(ctx.fonts.big, ctx.tr("SHOP_UI_BACK", "Back"), child(f, Label.x, Label.y), ctx.col.ink, 1, Label.max_width, "center", op)
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
	local Marker = Animation.marker
	local a = s.selT and (now - s.selT) or 1
	local k = Marker.start_scale + (1 - Marker.start_scale) * Easing.outBack(math.min(1, a / Marker.grow_s), Marker.overshoot)
	local mf = child(f, 0, 0, Layout.marker.rot_deg + Marker.sway_deg * sin(TAU * now / Marker.period_s), k)
	sprite(ctx.tex[MARKER[e.kind]], mf, 1, 1, math.min(1, a / Marker.fade_s))
end

-- view: { ready, big, normals, icons, selected }
function M.draw(now, dt, view)
	local fx, fy, rot = pageFrame(now)
	local pf = frame(PAGE_X + fx, PAGE_Y + fy, rot, 1)
	lastFrame = pf
	local follow = PageAnim.shadow_follow
	sprite(ctx.tex.PageShadow, frame(PAGE_X + fx * follow + SHADOW_DX, PAGE_Y + fy * follow + SHADOW_DY, rot, 1), 1)
	sprite(ctx.tex.Page, pf, 1)
	text(ctx.fonts.mast, ctx.tr("SHOP_UI_MASTHEAD", "The Daily Shop"), child(pf, MAST_X - PW / 2, MAST_Y - PH / 2), ctx.col.cream, 1, Page.masthead.max_width, "left")
	text(ctx.fonts.small, ctx.tr("SHOP_UI_ISSUE", "New picks every day!"), child(pf, ISSUE_X - PW / 2, MAST_Y - PH / 2), ctx.col.gold, 1, Page.issue.max_width, "right")
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
