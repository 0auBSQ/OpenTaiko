---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local, inject-field, param-type-mismatch
local DBItems = require("DBControllers/dbItems")
local Almanac  = require("almanac")     -- date patterns
local SEASONAL = require("seasonal")    -- featured big-slot items, by date
local PopUI = require("PopUI")
local NavInput = require("NavInput")
local Util = require("Util")
local Magazine = require("magazine")   -- the catalog page
local Keeper = require("keeper")       -- the shopkeeper and its speech bubble
local Easing = require("Easing")

local save = nil
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
local layoutSize = 5
local selected = "back"        -- the listing in focus: "big", "n1".."n6", "reroll" or "back"

-- Animation clock (seconds, advanced in draw so the page keeps moving while the stage fades out)
local now = 0
local lastDt = 0
local coinFrom, coinT = nil, nil      -- the coin counter rolling down after a purchase
local browseT, browseSaid = 0, true   -- when the focus last moved, and whether the keeper commented on it
local lastBrowseLine = -100
local idleT, idleNext = 0, 15
local revealAt = 0                    -- when the way-in transition stops hiding the page (on this clock)
local pendingLine = nil               -- a keeper line held until then

-- Items
local bigItem = nil
local normalItems = {}

-- Confirm screen
local toBuyItem = nil
local toBuyItemIcon = nil
local toBuySlot = 0   -- 1‥6 = normal slot, SLOT_BIG = big item
local confirmIdx = 0

-- Rerolls
local executedRerolls = 0

-- Current Screen
local currentScreen = "shop"

-- Daily shop persistence
local shopDB           = nil
local currentFreezeKey = 0
local soldOutMask      = 0   -- bitmask: bits 0‥5 = normalItems[1‥6], bit 6 = bigItem

local SLOT_BIG = 7   -- bit index 6 in the bitmask

-- ── Helpers ───────────────────────────────────────────────────────────────────

local function getJstFreezeKey()
	-- UTC+9; "!" forces UTC interpretation in os.date so we can add the offset manually
	local jst = os.date("!*t", os.time() + 9 * 3600)
	return jst.year * 10000 + jst.month * 100 + jst.day
end

local function isSoldOut(mask, slot)
	return ((mask >> (slot - 1)) & 1) == 1
end

local function markSoldOut(mask, slot)
	return mask | (1 << (slot - 1))
end

-- the skin's Locales/<code>.json through THEME; the English text stays in the code as the fallback
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

-- items buyable only ONCE ever, then never repooled: itempool.OneTime = 1 (e.g. the pod) — a plain DB
-- column so new one-time items need no code change. (Named OneTime, not "Unique": UNIQUE is a reserved
-- SQL word, and isUnique below already means "pulled from the pool within a roll".) A furniture grant
-- counter (.myroom_<id>) is drained by My Room and cannot signal lasting ownership, so a bought
-- one-time item sets a persistent "<RefText>_owned" trigger; isOwned reads it and isEntryIncluded then
-- drops the item from every future roll.
local function isOneTime(entry) return tonumber(entry.OneTime or 0) == 1 end
local function ownedTrigger(entry) return entry.RefText .. "_owned" end

local function isUnique(entry)
	if isOneTime(entry) then
		return true              -- treat as unique so it is pulled from the pool once picked / once owned
	end
	if entry.Type == "counterable" then
		return false
	end
	return true
end

local function isOwned(entry)
	if isOneTime(entry) then
		return save:GetGlobalTrigger(ownedTrigger(entry))
	end
	if entry.Type == "triggerable" then
		return save:GetGlobalTrigger(entry.RefText)
	end
	if entry.Type == "nameplate" then
		return save:IsNameplateUnlocked(entry.RefInt)
	end
	return false
end

local function isEntryIncluded(entry)
	if entry.Condition ~= nil then
		return save:GetGlobalTrigger(entry.Condition)
	end
	if isUnique(entry) then
		return not isOwned(entry)
	end
	return true
end

local function entryHasicon(entry)
	if entry.Type == "nameplate" then
		return false
	end
	return true
end

-- ── Item pool cache ───────────────────────────────────────────────────────────

local _cachedPools = nil

local function ensureCachedPools()
	if _cachedPools == nil then
		_cachedPools = {
			normal = Util.cloneTable(DBItems:GetItems("regular")),
			big    = Util.cloneTable(DBItems:GetItems("major"))
		}
	end
end

local function findItemByCode(code)
	if code == nil or code == "" then return nil end
	ensureCachedPools()
	for _, pool in pairs(_cachedPools) do
		for i = 1, #pool do
			if pool[i].Code == code then return pool[i] end
		end
	end
	return nil
end

-- My Room furniture rows use the code "furn_<id>"; the shop draws their preview from a shared texture
-- baked by My Room (myroom_thumb_<id>) instead of loading a per-shop model/PNG.
local function furnitureId(code)
	if code and code:sub(1, 5) == "furn_" then return code:sub(6) end
	return nil
end

local function setupItem(entry, iconIdx)
	if entry == nil then return nil end
	local item = Util.deepcopy(entry)
	item.LocalizedName = LANG:FromString(item.Name):GetString("")
	item.SoldOut = false
	if icons[iconIdx] ~= nil and not sharedIcon[iconIdx] then icons[iconIdx]:Dispose() end   -- the slot's previous picture
	icons[iconIdx] = nil
	sharedIcon[iconIdx] = nil
	if entryHasicon(item) then
		local fid = furnitureId(item.Code)
		if fid then
			-- furniture preview = the shared texture My Room baked at onStart (myroom_thumb_<id>)
			local shared = SHARED:GetSharedTexture("myroom_thumb_"..fid)
			if shared and shared.Loaded then
				icons[iconIdx] = shared
				sharedIcon[iconIdx] = true         -- shared: never Dispose in deactivate
				item._shared = true                -- draw centered+scaled (thumbnails aren't full-stand art)
			else
				icons[iconIdx] = nil               -- not baked yet (My Room not opened) → name-only
			end
		else
			icons[iconIdx] = TEXTURE:CreateTexture("Textures/Icons/"..item.Code..".png")
		end
	else
		icons[iconIdx] = nil
	end
	return item
end

-- roll a slot's stock: ranged items (StockMax>0, e.g. paints/floorings) get a random quantity you buy
-- one unit at a time; everything else is a single purchase (Stock 1 → sold out on first buy).
local function rollStock(item)
	if item == nil then return end
	local smin = tonumber(item.StockMin) or 0
	local smax = tonumber(item.StockMax) or 0
	if smax > 0 then
		smin = math.max(1, smin)
		if smax < smin then smax = smin end
		item.Stock = math.random(smin, smax)
	else
		item.Stock = 1
	end
end

-- ── Shop generation ───────────────────────────────────────────────────────────

local function poolItems()
	-- Address-of-a-table as extra seed entropy. %p formatting is platform-dependent (bionic
	-- prints "0x..." where MSVC prints bare hex), so grab the trailing hex digits explicitly.
	local ptr = tonumber(tostring({}):match("(%x+)%s*$") or "0", 16) or 0
	math.randomseed(os.time() + ptr % 0x7FFFFFFF)

	ensureCachedPools()
	local _pools = _cachedPools

	local itemPools = {
		normal = {},
		big = {}
	}

	local poolSizes = {
		normal = 0,
		big = 0
	}

	for pool, entries in pairs(_pools) do
		local _count = #entries
		for i = 1, _count do
			local _e = entries[i]

			if isEntryIncluded(_e) then
				local _size = poolSizes[pool]
				itemPools[pool][_size] = _e
				poolSizes[pool] = _size + _e.PoolSize
			end
		end
	end

	debugLog(poolSizes["normal"] .. " - " .. poolSizes["big"])

	-- helper to roll from weighted pool
	local function pickFromPool(pool, roll)
		local chosen = nil
		local lastKey = -1
		for key, e in pairs(pool) do
			if key <= roll and key >= lastKey then
				chosen = e
				lastKey = key
			end
		end
		return chosen
	end

	-- pick big item
	if poolSizes.big > 0 then
		local roll = math.random(0, poolSizes.big - 1)
		bigItem = pickFromPool(itemPools.big, roll)
		if bigItem.Type == "empty" then
			-- a nothing is picked (can happen to have days with 6 small slots)
			bigItem = nil
		else
			bigItem = setupItem(bigItem, 5)
			rollStock(bigItem)
		end
	else
		bigItem = nil
	end

	-- decide number of normal items
	local count = bigItem and 4 or 6
	layoutSize = bigItem and 5 or 6
	normalItems = {}

	-- copy normal pool
	local pool = {}
	for k, v in pairs(itemPools.normal) do
		pool[k] = v
	end
	local poolSize = poolSizes.normal

	-- pick normal items one by one
	for n = 1, count do
		if poolSize == 0 then
			normalItems[n] = nil
		else
			local roll = math.random(0, poolSize - 1)
			debugLog("Pool " .. poolSize .. " - " .. roll)
			local chosen = pickFromPool(pool, roll)
			normalItems[n] = setupItem(chosen, n)
			rollStock(normalItems[n])

			if chosen and isUnique(chosen) then
				-- rebuild pool without this entry
				local newPool = {}
				local newSize = 0
				for _, e in pairs(pool) do
					if e ~= chosen then
						newPool[newSize] = e
						newSize = newSize + e.PoolSize
					end
				end
				pool = newPool
				poolSize = newSize
			end
		end
	end

	debugLog("Big: " .. tostring(bigItem and bigItem.LocalizedName or "nil"))
	debugLog("Normal count: " .. tostring(#normalItems))
	for _, v in pairs(normalItems) do
		debugLog(v.LocalizedName .. " (" .. v.Code .. ")")
	end
end

-- ── Featured items ────────────────────────────────────────────────────────────
-- The best active entry of seasonal.lua (highest priority, not owned) takes the big slot. A fresh
-- roll and a reroll can both displace it, so this runs after every roll/load.
local function applySeasonal()
	if save == nil then return end
	local pick = Almanac.pick(SEASONAL, save, function(e)
		local item = findItemByCode(Almanac.plain(e.code))
		return item == nil or isOwned(item)
	end)
	if pick == nil then return end
	local code = Almanac.plain(pick.code)
	if bigItem ~= nil and bigItem.Code == code then return end
	bigItem = setupItem(findItemByCode(code), 5)
	rollStock(bigItem)
	soldOutMask = soldOutMask & ~(1 << (SLOT_BIG - 1))
	layoutSize = 5
	for i = 5, 6 do normalItems[i] = nil end
end

-- ── Persistence helpers ───────────────────────────────────────────────────────

local DB_PREFIX = ""   -- set in activate() from save.SaveId so each save file has its own shop state

local function storeShopState(db)
	db:Write(DB_PREFIX .. "day",     tostring(currentFreezeKey))
	db:Write(DB_PREFIX .. "rerolls", tostring(executedRerolls))
	db:Write(DB_PREFIX .. "soldout", tostring(soldOutMask))
	db:Write(DB_PREFIX .. "big",       bigItem and bigItem.Code or "")
	db:Write(DB_PREFIX .. "big_stock", tostring(bigItem and bigItem.Stock or 0))
	for i = 1, 6 do
		db:Write(DB_PREFIX .. "n" .. i,          normalItems[i] and normalItems[i].Code or "")
		db:Write(DB_PREFIX .. "n" .. i .. "s",   tostring(normalItems[i] and normalItems[i].Stock or 0))
	end
end

local function loadShopState(db)
	soldOutMask = tonumber(db:Read(DB_PREFIX .. "soldout") or "0") or 0

	bigItem = setupItem(findItemByCode(db:Read(DB_PREFIX .. "big")), 5)
	if bigItem then bigItem.Stock = tonumber(db:Read(DB_PREFIX .. "big_stock") or "1") or 1 end

	normalItems = {}
	for i = 1, 6 do
		normalItems[i] = setupItem(findItemByCode(db:Read(DB_PREFIX .. "n" .. i)), i)
		if normalItems[i] then normalItems[i].Stock = tonumber(db:Read(DB_PREFIX .. "n" .. i .. "s") or "1") or 1 end
	end

	-- Apply soldout flags from saved mask
	if bigItem and isSoldOut(soldOutMask, SLOT_BIG) then bigItem.SoldOut = true end
	for i = 1, 6 do
		if normalItems[i] and isSoldOut(soldOutMask, i) then normalItems[i].SoldOut = true end
	end

	layoutSize = bigItem and 5 or 6
	executedRerolls = tonumber(db:Read(DB_PREFIX .. "rerolls") or "0") or 0
end

-- ── Draw ──────────────────────────────────────────────────────────────────────

local DESK_X, DESK_Y = 1180, 786          -- Desk.png's top-left (its wood starts 4 px in)
local PLATE_X, PLATE_Y = 1232, 903        -- the player's nameplate on the desk's front
local TILL_X, TILL_Y = 1738, 944          -- the coin counter on the desk's front
local CONFIRM_CX, BTN_W = 614, 340        -- the modals sit over the page, leaving the keeper in view

local function rerollPrice()
	return math.floor(10 * (2 ^ executedRerolls))
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
	if d < 0 then d = 0 elseif d > 0.1 then d = 0.1 end
	return d
end

-- the coins on the till, rolling down to the new amount after a purchase
local function shownCoins()
	local coins = save.Coins
	if coinFrom == nil then return coins end
	local k = Easing.outCubic((now - coinT) / 0.7)
	if k >= 1 then coinFrom = nil; return coins end
	return math.floor(coinFrom + (coins - coinFrom) * k + 0.5)
end

local function drawDesk()
	textures["Desk"]:Draw(DESK_X, DESK_Y)
	if save == nil then return end
	NAMEPLATE:DrawPlayerNameplate(PLATE_X, PLATE_Y, 255, playerIndex)
	textures["Till"]:DrawAtAnchor(TILL_X, TILL_Y, "center")
	if coinTex then drawSharedIcon(coinTex, TILL_X - 112, TILL_Y, 42) end
	local f = fonts.mid
	f:Draw(tostring(shownCoins()), TILL_X + 150, TILL_Y + Magazine.nudge(f), cols.ink, cols.clear, 1, 1, 260, "right")
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
	view.ready = save ~= nil and currentScreen ~= "playerselect"
	view.big, view.normals, view.icons, view.selected = bigItem, normalItems, icons, selected
	Magazine.draw(now, lastDt, view)

	if currentScreen ~= "shop" and confirmUI then
		confirmUI:rect(0, 0, 1920, 1080, 6, 8, 16, 150)   -- dim the shop behind the modal
		confirmUI:draw()
		-- the item preview sits on top of the panel body (in the gap above the price line), scaled to a
		-- fixed box so large icons (vault-key PNGs) don't spill over the panel text
		if currentScreen == "confirm" and toBuyItem then
			local px, py = CONFIRM_CX, 322
			if toBuyItem.Type == "nameplate" then
				NAMEPLATE:DrawNameplateTitleById(toBuyItem.RefInt, px + 15, py - 45, 255, text)
			elseif toBuyItemIcon ~= nil then
				drawSharedIcon(toBuyItemIcon, px, py, 168)   -- scales any texture down to the box
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
	if id == "big" then return bigItem, icons[5], SLOT_BIG end
	local k = slotOf(id)
	return normalItems[k], icons[k], k
end

-- lay the page out for the current items (featured + 4 entries, or 6 entries); the focus stays where it was
-- when that listing still exists
local function relayout(reshuffle)
	local ids = Magazine.layout(bigItem ~= nil, reshuffle)
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

-- buy `qty` units of a slot at once: grants qty (counterable → +qty to the counter; single-buy types
-- ignore qty), spends price×qty, decrements the slot stock, and marks it sold out only when depleted.
local function purchaseItemMultiple(item, slot, qty)
	qty = math.max(1, math.min(math.floor(qty or 1), item.Stock or 1))
	if item.Type == "nameplate" then
		save:UnlockNameplate(item.RefInt)
	elseif item.Type == "triggerable" then
		save:SetGlobalTrigger(item.RefText, true)
	elseif item.Type == "counterable" then
		-- My Room claims the counter deltas into its inventory
		save:SetGlobalCounter(item.RefText, save:GetGlobalCounter(item.RefText) + qty)
	end
	if isOneTime(item) then save:SetGlobalTrigger(ownedTrigger(item), true) end   -- bought once, never repooled
	save:SpendCoins(item.Price * qty)
	item.Stock = (item.Stock or 1) - qty
	if item.Stock <= 0 then
		item.SoldOut = true
		soldOutMask = markSoldOut(soldOutMask, slot)
	end
	storeShopState(shopDB)   -- persist the new stock + soldout so a reopen shows the same counts
	sounds.Buy:Play()
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
local MAG_THEME = {
	colors = {
		bg = { 251, 246, 232, 255 }, surface = { 255, 252, 243, 255 }, surface2 = { 243, 233, 210, 255 },
		primary = { 226, 72, 61, 255 }, primary2 = { 192, 50, 43, 255 },
		accent = { 255, 214, 77, 255 }, accent2 = { 236, 180, 40, 255 },
		outline = { 58, 46, 52, 255 }, text = { 58, 46, 52, 255 }, textOnAccent = { 255, 255, 255, 255 },
		textDisabled = { 170, 158, 150, 255 }, shadow = { 60, 40, 30, 90 }, gloss = { 255, 255, 255, 120 },
		focusRing = { 47, 160, 154, 255 }, track = { 236, 226, 206, 255 },
	},
	radius = 18,
}

-- confirm dialog: centred panel with the item name (title), a scaled preview (drawn in draw()), price,
-- an optional quantity stepper, total, and VERTICALLY-stacked Buy/Cancel (matches the up/down nav). Buy
-- is focused by default. BTN_W/CONFIRM_CX keep the two buttons aligned under the panel centre.
local function buildConfirmUI(item, slot)
	if confirmUI then confirmUI:disposeWidgets() end
	confirmUI = PopUI.new{ theme = MAG_THEME, sfx = SHOP_SFX, navPlayer = playerIndex + 1 }
	local cx, bx = CONFIRM_CX, CONFIRM_CX - BTN_W / 2
	local hasQty = (item.Stock or 1) > 1
	local panelH = hasQty and 720 or 640
	confirmUI:panel{ x = CONFIRM_CX - 300, y = 180, w = 600, h = panelH, title = item.LocalizedName or "" }
	confirmUI:label{ text = trf("SHOP_UI_PRICE", "Price: %d", item.Price), x = cx, y = 452, size = "label", align = "center" }
	local qtyChooser, totalY, buyY
	if hasQty then
		confirmUI:label{ text = tr("SHOP_UI_QUANTITY", "Quantity"), x = cx, y = 508, size = "small", align = "center" }
		local opts = {}
		for i = 1, item.Stock do opts[i] = tostring(i) end
		qtyChooser = confirmUI:chooser{ x = cx - 170, y = 546, w = BTN_W, h = 62, options = opts, index = 1,
			wrap = false, onChange = function() updateConfirmTotals() end }
		totalY, buyY = 634, 706
	else
		totalY, buyY = 520, 596
	end
	local totalLabel = confirmUI:label{ text = trf("SHOP_UI_TOTAL", "Total: %d", item.Price), x = cx, y = totalY, size = "label", align = "center" }
	local buyBtn = confirmUI:button{ text = tr("SHOP_UI_BUY", "Buy"), x = bx, y = buyY, w = BTN_W, h = 76, accent = true,
		onClick = function()
			local qty = (qtyChooser and tonumber(qtyChooser:value())) or 1
			if item.Price * qty > save.Coins then   -- can't afford (guarded)
				sounds.SoldOut:Play(); Keeper.say("poor", now); return
			end
			local before = save.Coins
			purchaseItemMultiple(item, slot, qty)
			coinFrom, coinT = before, now
			Magazine.poke(idOfSlot(slot), "bought", now, item.SoldOut)
			Keeper.say("bought", now)
			closeConfirm()
		end,
		sfx = { click = "" } }
	confirmUI:button{ text = tr("SHOP_UI_CANCEL", "Cancel"), x = bx, y = buyY + 92, w = BTN_W, h = 76,
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
	confirmUI:panel{ x = CONFIRM_CX - 300, y = 280, w = 600, h = 480, title = tr("SHOP_UI_REROLL", "New picks") }
	confirmUI:label{ text = tr("SHOP_UI_REROLL_ASK", "Reshuffle the shop?"), x = cx, y = 384, size = "label", align = "center" }
	confirmUI:label{ text = trf("SHOP_UI_REROLL_COST", "Cost: %d", price), x = cx, y = 448, size = "label", align = "center" }
	local rb = confirmUI:button{ text = tr("SHOP_UI_REROLL_GO", "Reroll"), x = bx, y = 528, w = BTN_W, h = 76, accent = true,
		onClick = function()
			if price > save.Coins then sounds.SoldOut:Play(); Keeper.say("poor", now); return end
			local before = save.Coins
			save:SpendCoins(price); executedRerolls = executedRerolls + 1; soldOutMask = 0
			poolItems(); applySeasonal(); storeShopState(shopDB); sounds.Buy:Play()
			coinFrom, coinT = before, now
			closeConfirm()
			relayout(true)
			Magazine.popAll(now)
			Magazine.shuffle(now)
			Keeper.say("reroll", now)
			return true
		end }
	rb.enabled = (price <= save.Coins)
	confirmUI:button{ text = tr("SHOP_UI_CANCEL", "Cancel"), x = bx, y = 620, w = BTN_W, h = 76,
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
	if currentScreen == "playerselect" then
		if confirmUI then
			local res = confirmUI:update(ts)
			if currentScreen ~= "playerselect" then       -- a save was picked (enterShopFor ran)
				confirmUI:disposeWidgets(); confirmUI = nil
			elseif res == "cancel" then
				return leave()
			end
		end
		return
	end
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
		if not browseSaid and now - browseT > 0.7 then
			browseSaid = true
			local ev = browseEvent(selected)
			if ev ~= nil and now - lastBrowseLine > 3.5 and Keeper.say(ev, now, true) then lastBrowseLine = now end
		end
		if acted then
			idleT = 0
		elseif now >= revealAt then
			idleT = idleT + lastDt
			if idleT >= idleNext then
				idleT = 0
				if Keeper.say("idle", now, true) then idleNext = 22 end
			end
		end
	end
end

-- ── Player select + per-save shop state ────────────────────────────────────────

-- commit to a save file: the shop DB is keyed by the save's SaveUID, so each save has its OWN daily
-- rolls (falls back to slot index for saves predating the SaveUID migration).
local function enterShopFor(index)
	playerIndex = index
	save = GetSaveFile(index)
	local uid = (save.SaveUID and save.SaveUID ~= "") and save.SaveUID or ("slot" .. index)
	DB_PREFIX = uid .. "_"
	shopDB = DATABASE:OpenLocalDatabase("shop_state")
	currentFreezeKey = getJstFreezeKey()
	local storedDay = tonumber(shopDB:Read(DB_PREFIX .. "day") or "0") or 0
	if storedDay ~= currentFreezeKey then
		executedRerolls = 0; soldOutMask = 0; poolItems(); applySeasonal(); storeShopState(shopDB)
	else
		loadShopState(shopDB)
		applySeasonal(); storeShopState(shopDB)
	end
	selected = "back"
	currentScreen = "shop"
	relayout(true)
	local start = math.max(now, revealAt)
	Magazine.popAll(start + 0.1)
	sayOnReveal("greet")
	browseT, browseSaid = start, true
	idleT, idleNext = 0, 15
end

-- on entering the shop, pick WHICH of the 5 local saves to browse (its coins/unlocks). Skips itself
-- when 0/1 saves exist.
local function openShopPlayerSelect()
	local entries = {}
	for i = 0, 4 do
		local sf = GetSaveFile(i)
		if sf and sf.SaveUID and sf.SaveUID ~= "" then
			entries[#entries + 1] = { text = trf("SHOP_UI_PLAYER", "Player %d — %s", i + 1, sf.Name or ""), value = i }
		end
	end
	if #entries <= 1 then enterShopFor(entries[1] and entries[1].value or 0); return end
	if confirmUI then confirmUI:disposeWidgets() end
	confirmUI = PopUI.new{ theme = MAG_THEME, sfx = SHOP_SFX, navPlayer = nil } -- accessible by all players
	local x, y, w = CONFIRM_CX - 380, 210, 760
	local h = 120 + #entries * 78 + 40
	confirmUI:panel{ x = x, y = y, w = w, h = h, title = tr("SHOP_UI_WHOSE", "Whose shop?") }
	confirmUI:menu{ x = x + 36, y = y + 92, w = w - 72, h = #entries * 78, rowHeight = 78, items = entries,
					onSelect = function(_, it) enterShopFor(it.value) end }
	confirmUI:_setFocusIndex(1)
	currentScreen = "playerselect"
	sayOnReveal("whose")
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
	idleT, idleNext = 0, 15
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
	Keeper.load(getJstFreezeKey())

	sounds.BGM:SetLoop(true)
	sounds.BGM:Play()

	openShopPlayerSelect()   -- pick whose shop to browse (per-SaveUID rolls); enters directly if ≤1 save
end

function deactivate()
	for _, v in pairs(textures) do
		v:Dispose()
	end
	textures = {}
	coinTex = nil
	Keeper.free()

	for k, v in pairs(icons) do
		if not sharedIcon[k] then v:Dispose() end   -- shared My Room textures are owned by the global store
	end
	icons = {}
	sharedIcon = {}

	if confirmUI then confirmUI:disposeWidgets(); confirmUI = nil end

	if shopDB then shopDB:Dispose() end
	shopDB = nil

	sounds.BGM:Stop()
end


function onStart()
	text = TEXT:Create(16)
	fonts.mast = TEXT:CreateGlyphCached(56)
	fonts.big = TEXT:CreateGlyphCached(40)
	fonts.talk = TEXT:CreateGlyphCached(32)
	fonts.mid = TEXT:CreateGlyphCached(30)
	fonts.small = TEXT:CreateGlyphCached(24)
	cols.ink = COLOR:CreateColorFromRGBA(52, 44, 52, 255)
	cols.red = COLOR:CreateColorFromRGBA(214, 52, 44, 255)
	cols.stamp = COLOR:CreateColorFromRGBA(214, 52, 44, 255)
	cols.white = COLOR:CreateColorFromRGBA(255, 255, 255, 255)
	cols.cream = COLOR:CreateColorFromRGBA(255, 248, 236, 255)
	cols.gold = COLOR:CreateColorFromRGBA(255, 226, 150, 255)
	cols.clear = COLOR:CreateColorFromRGBA(0, 0, 0, 0)

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
