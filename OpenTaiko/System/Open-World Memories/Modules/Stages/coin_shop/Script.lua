---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local, inject-field, param-type-mismatch
local Daily = require("Shop.Daily")       -- the day's stock, its saved state and the purchases
local Catalog = require("Shop.Catalog")   -- the items
local PopUI = require("PopUI")
local SavePicker = require("SavePicker")
local NavInput = require("NavInput")
local Magazine = require("magazine")   -- the catalog page
local Keeper = require("keeper")       -- the shopkeeper and its speech bubble
local Easing = require("Easing")
-- positions, sizes and colours: Config/layout.json; timings: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Talk = Animation.talk

local save = nil
local shop = nil               -- Shop.Daily for the chosen save
local playerIndex = 0          -- which of the 5 local saves' shop we're browsing (chosen on entry)
local confirmUI = nil          -- PopUI modal for the buy-confirm / reroll-confirm / player-select

local EXIT_TRANSITION = "newspaper_back"

-- Assets
local sounds = {}
local textures = {}
local icons = {}
local sharedIcon = {}   -- iconIdx -> true for shared (My Room furniture) textures we must NOT dispose
local coinTex = nil     -- the "Coin" shared texture (registered by _boot), never disposed here

local text = nil               -- whole-string font, for the nameplate titles
local fonts = {}               -- glyph fonts: mast, big, mid, small, talk
local cols = {}

-- Menu navigation
local selected = "back"        -- the listing in focus: "big", "n1".."n6", "reroll" or "back"

-- Animation clock (seconds, advanced in draw so the page keeps moving while the stage fades out)
local now = 0
local lastDt = 0
local coinFrom, coinT = nil, nil      -- the coin counter rolling down after a purchase
local browseT, browseSaid = 0, true   -- when the focus last moved, and whether the keeper commented on it
local lastBrowseLine = -100
local idleT, idleNext = 0, Talk.first_idle_s
local revealAt = 0                    -- when the way-in transition stops hiding the page (on this clock)
local pendingLine = nil               -- a keeper line held until then

-- Confirm screen
local toBuyItem = nil
local toBuyItemIcon = nil
local toBuySlot = 0   -- 1‥6 = normal slot, SLOT_BIG = big item
local confirmIdx = 0

-- Current Screen
local currentScreen = "shop"

local SLOT_BIG = Daily.SLOT_BIG

-- ── Helpers ───────────────────────────────────────────────────────────────────

-- the skin's Locales/<code>/ through THEME; the English text stays in the code as the fallback
local function tr(key, fallback)
	local ok, s = pcall(function() return THEME:GetSkinString(key) end)
	if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
	return fallback
end

-- a localized format string, falling back to the English one if the translation does not fit the values
local function trf(key, fallback, ...)
	local ok, s = pcall(string.format, tr(key, fallback), ...)
	if ok then return s end
	return string.format(fallback, ...)
end

local function entryHasicon(entry)
	if entry.Type == "nameplate" then
		return false
	end
	return true
end

-- ── Icons ─────────────────────────────────────────────────────────────────────

-- an item's picture: My Room furniture shows the shared thumbnail My Room baked at onStart
-- (myroom_thumb_<id>, never disposed here; none before My Room was opened), other items
-- Textures/Icons/<code>.png
local function iconFor(item)
	if not entryHasicon(item) then return nil, nil end
	local fid = Catalog.furnitureId(item.Code)
	if fid then
		local shared = SHARED:GetSharedTexture("myroom_thumb_"..fid)
		if shared and shared.Loaded then return shared, true end
		return nil, nil
	end
	return TEXTURE:CreateTexture("Textures/Icons/"..item.Code..".png"), nil
end

local function freeIcons()
	for k, v in pairs(icons) do
		if not sharedIcon[k] then v:Dispose() end
	end
	icons = {}
	sharedIcon = {}
end

-- the pictures of the listed items: the featured one in icon 5, the entries in 1..6
local function refreshIcons()
	freeIcons()
	if shop.big then icons[5], sharedIcon[5] = iconFor(shop.big) end
	for i = 1, 6 do
		if shop.normals[i] then icons[i], sharedIcon[i] = iconFor(shop.normals[i]) end
	end
end

-- ── Draw ──────────────────────────────────────────────────────────────────────

local DESK_X, DESK_Y = Layout.desk.x, Layout.desk.y
local PLATE_X, PLATE_Y = Layout.nameplate.x, Layout.nameplate.y
local Till = Layout.till
local TILL_X, TILL_Y = Till.x, Till.y
local Confirm = Layout.confirm
local Buy, Reroll = Confirm.buy, Confirm.reroll
local CONFIRM_CX, BTN_W, BTN_H = Confirm.center_x, Confirm.button_w, Confirm.button_h
local PlateOffset = Layout.title_plate_offset

local function rerollPrice()
	return shop and shop:rerollPrice() or Daily.rerollPrice(0)
end

-- draw a flat texture centered on (cx,cy), scaled to fit a boxSize square (the confirm preview)
local function drawSharedIcon(tex, cx, cy, boxSize)
	local w, h = tex.Width, tex.Height
	if not w or w <= 0 or not h or h <= 0 then return end
	local s = boxSize / math.max(w, h)
	tex:SetScale(s, s)
	tex:DrawAtAnchor(cx, cy, "center")
	tex:SetScale(1, 1)
end

local function frameDelta()
	local ok, d = pcall(function() return fps.deltaTime end)
	d = (ok and type(d) == "number") and d or (1 / 60)
	if d < 0 then d = 0 elseif d > Animation.frame_step_max_s then d = Animation.frame_step_max_s end
	return d
end

-- the coins on the till, rolling down to the new amount after a purchase
local function shownCoins()
	local coins = save.Coins
	if coinFrom == nil then return coins end
	local k = Easing.outCubic((now - coinT) / Animation.coins.roll_s)
	if k >= 1 then coinFrom = nil; return coins end
	return math.floor(coinFrom + (coins - coinFrom) * k + 0.5)
end

local function drawDesk()
	textures["Desk"]:Draw(DESK_X, DESK_Y)
	if save == nil then return end
	NAMEPLATE:DrawPlayerNameplate(PLATE_X, PLATE_Y, 255, playerIndex)
	textures["Till"]:DrawAtAnchor(TILL_X, TILL_Y, "center")
	if coinTex then drawSharedIcon(coinTex, TILL_X + Till.coin_dx, TILL_Y, Till.coin_size) end
	local f = fonts.mid
	f:Draw(tostring(shownCoins()), TILL_X + Till.amount_dx, TILL_Y + Magazine.nudge(f), cols.ink, cols.clear, 1, 1, Till.amount_max_width, "right")
end

local view = { ready = false }

function draw()
	lastDt = frameDelta()
	now = now + lastDt
	if pendingLine ~= nil and now >= revealAt then
		Keeper.say(pendingLine, now)
		pendingLine = nil
	end
	textures["Bg"]:Draw(0, 0)
	Keeper.drawBody(now)
	drawDesk()

	-- the page; before a save is chosen only its masthead shows
	view.ready = save ~= nil
	view.big, view.normals, view.icons, view.selected = shop and shop.big, shop and shop.normals or {}, icons, selected
	Magazine.draw(now, lastDt, view)

	if currentScreen ~= "shop" and confirmUI then
		local dim = Layout.dim_color
		confirmUI:rect(0, 0, 1920, 1080, dim[1], dim[2], dim[3], dim[4])   -- dim the shop behind the modal
		confirmUI:draw()
		-- the item preview sits on top of the panel body (in the gap above the price line), scaled to a
		-- fixed box so large icons (vault-key PNGs) don't spill over the panel text
		if currentScreen == "confirm" and toBuyItem then
			local px, py = CONFIRM_CX, Buy.preview_y
			if toBuyItem.Type == "nameplate" then
				NAMEPLATE:DrawNameplateTitleById(toBuyItem.RefInt, px + PlateOffset.x, py + PlateOffset.y, 255, text)
			elseif toBuyItemIcon ~= nil then
				drawSharedIcon(toBuyItemIcon, px, py, Buy.preview_size)   -- scales any texture down to the box
			end
		end
	end

	Keeper.drawBubble(now)
end

-- ── Navigation ────────────────────────────────────────────────────────────────

local function slotOf(id)
	if id == "big" then return SLOT_BIG end
	local k = id:match("^n(%d)$")
	return k and tonumber(k) or nil
end

local function idOfSlot(slot)
	if slot == SLOT_BIG then return "big" end
	return "n" .. slot
end

local function itemOf(id)
	if id == "big" then return shop.big, icons[5], SLOT_BIG end
	local k = slotOf(id)
	return shop.normals[k], icons[k], k
end

-- lay the page out for the current items (featured + 4 entries, or 6 entries); the focus stays where it was
-- when that listing still exists
local function relayout(reshuffle)
	local ids = Magazine.layout(shop.big ~= nil, reshuffle)
	for _, id in ipairs(ids) do
		if id == selected then return end
	end
	selected = "back"
end

-- reading order: the featured item, the entries row by row, the coupon, the back note (wrapping around)
local function moveSelection(direction)
	local ids = Magazine.ids()
	local idx = #ids
	for i, id in ipairs(ids) do
		if id == selected then idx = i end
	end
	return ids[(idx - 1 + direction) % #ids + 1]
end

local function focus(id)
	if id == selected then return end
	selected = id
	Magazine.select(id, now)
	browseT, browseSaid = now, false
end

-- what the keeper says about the listing in focus once it stays there a moment
local function browseEvent(id)
	if id == "reroll" then return "coupon" end
	if id == "back" then return "backtab" end
	local item = itemOf(id)
	if item == nil or item.SoldOut then return nil end
	return id == "big" and "featured" or "browse"
end

local function closeConfirm()
	currentScreen = "shop"
	if confirmUI then confirmUI:disposeWidgets(); confirmUI = nil end
end

-- refresh the live "Total: N" line + gray the Buy button when the selected quantity is unaffordable
local function updateConfirmTotals()
	if not (confirmUI and confirmUI._item) then return end
	local qty = (confirmUI._qty and tonumber(confirmUI._qty:value())) or 1
	local total = confirmUI._item.Price * qty
	if confirmUI._totalLabel then confirmUI._totalLabel:setText(trf("SHOP_UI_TOTAL", "Total: %d", total)) end
	if confirmUI._buyBtn then confirmUI._buyBtn.enabled = (total <= save.Coins) end
end

local SHOP_SFX  -- set in activate() once sounds exist

-- the modals in the magazine's paper and ink
local MAG_THEME = Config.plain(Confirm.theme)

-- confirm dialog: centred panel with the item name (title), a scaled preview (drawn in draw()), price,
-- an optional quantity stepper, total, and VERTICALLY-stacked Buy/Cancel (matches the up/down nav). Buy
-- is focused by default. BTN_W/CONFIRM_CX keep the two buttons aligned under the panel centre.
local function buildConfirmUI(item, slot)
	if confirmUI then confirmUI:disposeWidgets() end
	confirmUI = PopUI.new{ theme = MAG_THEME, sfx = SHOP_SFX, navPlayer = playerIndex + 1 }
	local cx, bx = CONFIRM_CX, CONFIRM_CX - BTN_W / 2
	local hasQty = (item.Stock or 1) > 1
	local panelH = hasQty and Buy.panel_h_quantity or Buy.panel_h
	confirmUI:panel{ x = CONFIRM_CX - Confirm.panel_w / 2, y = Buy.panel_y, w = Confirm.panel_w, h = panelH, title = item.LocalizedName or "" }
	confirmUI:label{ text = trf("SHOP_UI_PRICE", "Price: %d", item.Price), x = cx, y = Buy.price_y, size = "label", align = "center" }
	local qtyChooser, totalY, buyY
	if hasQty then
		confirmUI:label{ text = tr("SHOP_UI_QUANTITY", "Quantity"), x = cx, y = Buy.quantity_y, size = "small", align = "center" }
		local opts = {}
		for i = 1, item.Stock do opts[i] = tostring(i) end
		qtyChooser = confirmUI:chooser{ x = cx - BTN_W / 2, y = Buy.chooser_y, w = BTN_W, h = Buy.chooser_h, options = opts, index = 1,
			wrap = false, onChange = function() updateConfirmTotals() end }
		totalY, buyY = Buy.total_y_quantity, Buy.buy_y_quantity
	else
		totalY, buyY = Buy.total_y, Buy.buy_y
	end
	local totalLabel = confirmUI:label{ text = trf("SHOP_UI_TOTAL", "Total: %d", item.Price), x = cx, y = totalY, size = "label", align = "center" }
	local buyBtn = confirmUI:button{ text = tr("SHOP_UI_BUY", "Buy"), x = bx, y = buyY, w = BTN_W, h = BTN_H, accent = true,
		onClick = function()
			local qty = (qtyChooser and tonumber(qtyChooser:value())) or 1
			local before = save.Coins
			if not shop:buy(slot, qty) then   -- can't afford (guarded)
				sounds.SoldOut:Play(); Keeper.say("poor", now); return
			end
			sounds.Buy:Play()
			coinFrom, coinT = before, now
			Magazine.poke(idOfSlot(slot), "bought", now, item.SoldOut)
			Keeper.say("bought", now)
			closeConfirm()
		end,
		sfx = { click = "" } }
	confirmUI:button{ text = tr("SHOP_UI_CANCEL", "Cancel"), x = bx, y = buyY + Buy.button_gap, w = BTN_W, h = BTN_H,
		onClick = function() closeConfirm(); Keeper.say("cancel", now) end,
		sfx = { click = "cancel" } }
	confirmUI._item, confirmUI._qty = item, qtyChooser
	confirmUI._totalLabel, confirmUI._buyBtn = totalLabel, buyBtn
	updateConfirmTotals()
	confirmUI:_setFocusIndex(qtyChooser and 2 or 1)   -- default focus/highlight on Buy
end

local function buildRefreshUI()
	if confirmUI then confirmUI:disposeWidgets() end
	confirmUI = PopUI.new{ theme = MAG_THEME, sfx = SHOP_SFX, navPlayer = playerIndex + 1 }
	local cx, bx = CONFIRM_CX, CONFIRM_CX - BTN_W / 2
	local price = rerollPrice()
	confirmUI:panel{ x = CONFIRM_CX - Confirm.panel_w / 2, y = Reroll.panel_y, w = Confirm.panel_w, h = Reroll.panel_h, title = tr("SHOP_UI_REROLL", "New picks") }
	confirmUI:label{ text = tr("SHOP_UI_REROLL_ASK", "Reshuffle the shop?"), x = cx, y = Reroll.ask_y, size = "label", align = "center" }
	confirmUI:label{ text = trf("SHOP_UI_REROLL_COST", "Cost: %d", price), x = cx, y = Reroll.cost_y, size = "label", align = "center" }
	local rb = confirmUI:button{ text = tr("SHOP_UI_REROLL_GO", "Reroll"), x = bx, y = Reroll.go_y, w = BTN_W, h = BTN_H, accent = true,
		onClick = function()
			if price > save.Coins then sounds.SoldOut:Play(); Keeper.say("poor", now); return end
			local before = save.Coins
			shop:reroll(); refreshIcons(); sounds.Buy:Play()
			coinFrom, coinT = before, now
			closeConfirm()
			relayout(true)
			Magazine.popAll(now)
			Magazine.shuffle(now)
			Keeper.say("reroll", now)
			return true
		end }
	rb.enabled = (price <= save.Coins)
	confirmUI:button{ text = tr("SHOP_UI_CANCEL", "Cancel"), x = bx, y = Reroll.cancel_y, w = BTN_W, h = BTN_H,
		onClick = function() closeConfirm(); Keeper.say("cancel", now) end,
		sfx = { click = "cancel" } }
	confirmUI:_setFocusIndex(1)   -- default focus/highlight on Reroll
end

-- ── Update ────────────────────────────────────────────────────────────────────

-- a keeper line said as soon as the page shows
local function sayOnReveal(event)
	if now >= revealAt then
		Keeper.say(event, now)
		pendingLine = nil
	else
		pendingLine = event
	end
end

local function leave()
	sounds.Cancel:Play()
	pendingLine = nil
	Keeper.say("leave", now)
	return Exit("title", nil, EXIT_TRANSITION)
end

-- Decide on a listing: the back note leaves, the coupon asks for a reroll, an item asks to be bought
local function decideOn(id)
	idleT = 0
	if id == "back" then return leave() end
	if id == "reroll" then
		sounds.Decide:Play()
		currentScreen = "refresh"
		buildRefreshUI()
		Keeper.say(rerollPrice() > save.Coins and "poor" or "rerollask", now)
		return
	end
	toBuyItem, toBuyItemIcon, toBuySlot = itemOf(id)
	if toBuyItem ~= nil and toBuyItem.SoldOut == false then
		sounds.Decide:Play()
		currentScreen = "confirm"
		buildConfirmUI(toBuyItem, toBuySlot)
		if toBuyItem.Price > save.Coins then
			Keeper.say("poor", now)
			Magazine.poke(id, "poor", now)
		else
			Keeper.say("trybuy", now)
		end
	else
		sounds.SoldOut:Play()
		Keeper.say("soldout", now)
		Magazine.poke(id, "soldout", now)
	end
end

function update(ts)
	if currentScreen == "confirm" or currentScreen == "refresh" then
		if confirmUI then
			if confirmUI:update(ts) == "cancel" then      -- Escape: PopUI reports it; the buttons handle the rest
				sounds.Cancel:Play(); closeConfirm()
				Keeper.say("cancel", now)
			end
		else
			currentScreen = "shop"
		end
	elseif currentScreen == "shop" then
		local acted = false
		local navPn = NavInput.p[playerIndex + 1]
		if navPn.right() then
			sounds.Skip:Play()
			focus(moveSelection(1))
			acted = true
		end
		if navPn.left() then
			sounds.Skip:Play()
			focus(moveSelection(-1))
			acted = true
		end
		if navPn.cancel() then
			return leave()
		end
		if navPn.decide() then
			return decideOn(selected)
		end

		-- the mouse: hovering a listing focuses it, a click decides on it
		local mx, my = INPUT:GetMouseXY()
		local mdx, mdy = INPUT:GetMouseDelta()
		if INPUT:IsMouseInside() then
			local hit = Magazine.hit(mx, my)
			if mdx ~= 0 or mdy ~= 0 then
				acted = true
				if hit ~= nil and hit ~= selected then
					sounds.Skip:Play()
					focus(hit)
				end
			end
			if hit ~= nil and INPUT:MousePressed("Left") then
				focus(hit)
				return decideOn(hit)
			end
		end

		-- the keeper comments on the listing in focus, and fills a long silence
		if not browseSaid and now - browseT > Talk.browse_after_s then
			browseSaid = true
			local ev = browseEvent(selected)
			if ev ~= nil and now - lastBrowseLine > Talk.browse_gap_s and Keeper.say(ev, now, true) then lastBrowseLine = now end
		end
		if acted then
			idleT = 0
		elseif now >= revealAt then
			idleT = idleT + lastDt
			if idleT >= idleNext then
				idleT = 0
				if Keeper.say("idle", now, true) then idleNext = Talk.idle_repeat_s end
			end
		end
	end
end

-- ── Player select + per-save shop state ────────────────────────────────────────

-- commit to a save file: each save has its OWN daily shop (Shop.Daily)
local function enterShopFor(index)
	playerIndex = index
	shop = Daily.open(index)
	save = shop.save
	refreshIcons()
	selected = "back"
	currentScreen = "shop"
	relayout(true)
	local start = math.max(now, revealAt)
	Magazine.popAll(start + Animation.entrance.pop_delay_s)
	sayOnReveal("greet")
	browseT, browseSaid = start, true
	idleT, idleNext = 0, Talk.first_idle_s
end

-- the save chosen on the title; opened without one, the first save
local function openShopForChosenSave()
	enterShopFor(SavePicker.take() or SavePicker.first())
end

-- ── Lifecycle ─────────────────────────────────────────────────────────────────

local TEXTURE_NAMES = {
	"Bg", "Desk", "Bubble", "Till",
	"Page", "PageShadow", "HeroPanel", "PhotoBig", "PhotoSmall", "Tape",
	"Burst", "Sticker", "Tag", "Stamp", "Badge", "Coupon", "Note",
	"Marker", "MarkerTall", "MarkerWide", "MarkerNote", "EntryShadow", "Splash",
}

function activate()
	save = nil
	SHOP_SFX = {
		move = function() sounds.Skip:Play() end,
		click = function() sounds.Decide:Play() end,
		cancel = function() sounds.Cancel:Play() end,
	}

	for _, v in ipairs(TEXTURE_NAMES) do
		textures[v] = TEXTURE:CreateTexture("Textures/" .. v .. ".png")
	end
	coinTex = nil
	pcall(function() coinTex = SHARED:GetSharedTexture("Coin") end)

	now, lastDt = 0, 0
	coinFrom, coinT = nil, nil
	browseT, browseSaid, lastBrowseLine = 0, true, -100
	idleT, idleNext = 0, Talk.first_idle_s
	-- the newspaper way in keeps the page hidden this long into its fade-in; the entrance waits for it
	revealAt = tonumber(SHARED:GetSharedString("newspaper_cover")) or 0
	SHARED:SetSharedString("newspaper_cover", "")
	pendingLine = nil
	selected = "back"
	local ctx = {
		tex = textures, coin = coinTex, fonts = fonts, col = cols, tr = tr, text = text, font = fonts.talk,
		coins = function() return save and save.Coins or 0 end,
		rerollPrice = rerollPrice,
	}
	Magazine.init(ctx)
	ctx.nudge = Magazine.nudge
	Keeper.init(ctx)
	Keeper.load(Daily.freezeKey())

	sounds.BGM:SetLoop(true)
	sounds.BGM:Play()

	openShopForChosenSave()
end

function deactivate()
	for _, v in pairs(textures) do
		v:Dispose()
	end
	textures = {}
	coinTex = nil
	Keeper.free()

	freeIcons()   -- shared My Room textures are owned by the global store

	if confirmUI then confirmUI:disposeWidgets(); confirmUI = nil end

	if shop then shop:close() end
	shop = nil

	sounds.BGM:Stop()
end


function onStart()
	local Fonts, Colors = Layout.fonts, Layout.colors
	text = TEXT:Create(Fonts.plate_title)
	fonts.mast = TEXT:CreateGlyphCached(Fonts.mast)
	fonts.big = TEXT:CreateGlyphCached(Fonts.big)
	fonts.talk = TEXT:CreateGlyphCached(Fonts.talk)
	fonts.mid = TEXT:CreateGlyphCached(Fonts.mid)
	fonts.small = TEXT:CreateGlyphCached(Fonts.small)
	cols.ink = Config.color(Colors.ink_color)
	cols.red = Config.color(Colors.red_color)
	cols.stamp = Config.color(Colors.stamp_color)
	cols.white = Config.color(Colors.white_color)
	cols.cream = Config.color(Colors.cream_color)
	cols.gold = Config.color(Colors.gold_color)
	cols.clear = Config.color(Colors.clear_color)

	sounds.Skip = SOUND:CreateSFX("Sounds/Skip.ogg")
	sounds.Cancel = SOUND:CreateSFX("Sounds/Cancel.ogg")
	sounds.Decide = SOUND:CreateSFX("Sounds/Decide.ogg")
	sounds.SoldOut = SOUND:CreateSFX("Sounds/SoldOut.ogg")
	sounds.Buy = SOUND:CreateSFX("Sounds/Buy.ogg")
	sounds.BGM = SOUND:CreateBGM("Sounds/BGM.ogg")
end


function onDestroy()
	if text ~= nil then
		text:Dispose()
	end
	for k, f in pairs(fonts) do
		f:Dispose()
		fonts[k] = nil
	end
	for _, sound in pairs(sounds) do
		sound:Dispose()
	end
end
