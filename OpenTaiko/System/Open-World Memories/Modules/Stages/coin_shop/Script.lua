---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local, inject-field, param-type-mismatch
local DBItems = require("DBControllers/dbItems")
local PopUI = require("PopUI")

local save = nil
local playerIndex = 0          -- which of the 5 local saves' shop we're browsing (chosen on entry)
local confirmUI = nil          -- PopUI modal for the buy-confirm / reroll-confirm / player-select

-- Assets
local sounds = {}
local textures = {}
local icons = {}
local sharedIcon = {}   -- iconIdx -> true for shared (My Room furniture) textures we must NOT dispose

local text = nil
local TxTextChar = {}
local glyphFont = nil          -- proper variable-width glyph font for the stock badge
local STOCK_FG, STOCK_BG = nil, nil

-- Menu navigation
local layoutSize = 5
local selectedItem = -2

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

-- Clone a C# Dictionary into a Lua table safely
local function cloneTable(t)
	local copy = {}

	-- Get enumerator from the dictionary
	local enumerator = t:GetEnumerator()
	while enumerator:MoveNext() do
		local kvp = enumerator.Current
		local key = kvp.Key
		local value = kvp.Value

		-- Recursively clone if it's another Dictionary
		if value ~= nil and type(value) == "userdata" and value.GetEnumerator then
			copy[key] = cloneTable(value)
		else
			copy[key] = value
		end
	end

	return copy
end

-- Deep copy Lua table
local function deepcopy(o, seen)
  seen = seen or {}
  if o == nil then return nil end
  if seen[o] then return seen[o] end

  local no
  if type(o) == 'table' then
    no = {}
    seen[o] = no

    for k, v in next, o, nil do
      no[deepcopy(k, seen)] = deepcopy(v, seen)
    end
    setmetatable(no, deepcopy(getmetatable(o), seen))
  else -- number, string, boolean, etc
    no = o
  end
  return no
end


local function drawNumberColor(x, y, str, color, centered)
	local fontSize = 16
	local xInit = x
	if centered == true then
		xInit = xInit - (fontSize * #str) / 2
	end
	x = xInit
	for i = 1, #str do
		local char = str:sub(i, i)
		if char == "\n" then
			y = y + fontSize
			x = xInit
		else
			if TxTextChar[char] ~= nil then
				TxTextChar[char]:SetColor(color)
				TxTextChar[char]:Draw(x, y)
			end
			x = x + fontSize
		end
	end
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
			normal = cloneTable(DBItems:GetItems("regular")),
			big    = cloneTable(DBItems:GetItems("major"))
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
	local item = deepcopy(entry)
	item.LocalizedName = LANG:FromString(item.Name):GetString("")
	item.NameTx = text:GetText(item.LocalizedName, true, 380)
	item.SoldOut = false
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

local function drawPrice(x, y, price)
	local color = COLOR:CreateColorFromHex("FFFFFFFF")
	if price > save.Coins then
		color = COLOR:CreateColorFromHex("FFFF0000")
	end
	drawNumberColor(x, y, tostring(price), color, true)
end

local function drawPriceWithTag(x, y, price)
	textures["PriceBox"]:Draw(x, y)
	drawPrice(x+166, y+26, price)
end

-- "x[N]" remaining-stock badge — glyph font, RIGHT-ANCHORED at rightX (variable-width, never overflows)
local function drawStock(rightX, y, stock)
	if not glyphFont then return end
	glyphFont:Draw("x"..tostring(stock), rightX, y, STOCK_FG, STOCK_BG, 1.0, 1.0, 0, "topright")
end

-- draw a flat texture centered on (cx,cy), scaled to fill a boxSize square (portraits/swatches)
local function drawSharedIcon(tex, cx, cy, boxSize)
	local w = tex.Width
	if not w or w <= 0 then return end
	local s = boxSize / w
	tex:SetScale(s, s)
	tex:DrawAtAnchor(cx, cy, "center")
	tex:SetScale(1, 1)
end

function draw()
	textures["Bg"]:Draw(0,0)

	-- player-select modal (before a save is chosen: no shop contents / no `save` yet)
	if currentScreen == "playerselect" or save == nil then
		if confirmUI then confirmUI:rect(0, 0, 1920, 1080, 6, 8, 16, 150); confirmUI:draw() end
		return
	end

	-- Normal items
	for i, v in ipairs(normalItems) do
		local halfIndex = (i - 1)
		local xOrig = 800 - (halfIndex // 2) * 400
		local yOrig = 480 - (halfIndex % 2) * 480
		textures["StandNormal"]:Draw(xOrig, yOrig)

		if v ~= nil and v.SoldOut == false then
			-- Draw icon
			local iconTex = icons[i]
			local xCenter = xOrig + 200
			local yCenter = yOrig + 200
			if v.Type == "nameplate" then
				NAMEPLATE:DrawNameplateTitleById(v.RefInt, xCenter, yCenter - 40, 255, text)
			elseif v._shared and iconTex ~= nil then
				drawSharedIcon(iconTex, xCenter, yCenter - 20, 300)
			elseif iconTex ~= nil then
				iconTex:Draw(xOrig, yOrig)
			end

			-- Name
			v.NameTx:DrawAtAnchor(xOrig + 200, yOrig + 440, "center")

			-- Price
			drawPriceWithTag(xOrig, yOrig + 320, v.Price)

			-- Stock badge (x[N]) at the TOP-RIGHT of the slot
			if v.Stock and v.Stock > 1 then drawStock(xOrig + 388, yOrig + 12, v.Stock) end
		else
			textures["SoldOut"]:Draw(xOrig, yOrig)
		end
	end

	-- Big item
	if bigItem ~= nil then
		textures["StandBig"]:Draw(0, 0)

		if bigItem ~= nil and bigItem.SoldOut == false then
			-- Draw icon
			local iconTex = icons[5]
			if bigItem.Type == "nameplate" then
				NAMEPLATE:DrawNameplateTitleById(bigItem.RefInt, 200, 400, 255, text)
			elseif bigItem._shared and iconTex ~= nil then
				drawSharedIcon(iconTex, 200, 400, 380)
			elseif iconTex ~= nil then
				iconTex:Draw(0, 0)
			end

			-- Name
			bigItem.NameTx:DrawAtAnchor(200, 920, "center")

			-- Price
			drawPriceWithTag(0, 800, bigItem.Price)

			-- Stock badge (x[N]) at the TOP-RIGHT of the big slot
			if bigItem.Stock and bigItem.Stock > 1 then drawStock(388, 12, bigItem.Stock) end
		else
			textures["SoldOut"]:Draw(0, 220)
		end
	end

	-- Bottom Panel
	textures["BottomPanel"]:Draw(1205, 806)
	local rerollPrice = math.floor(10 * (2 ^ executedRerolls))
	drawPrice(1312, 926, rerollPrice)

	-- Selected rect
	if selectedItem >= 0 then
		local xBox = 800 - (selectedItem // 2) * 400
		local yBox = 959 - (selectedItem % 2) * 480
		textures["Selected"]:DrawAtAnchor(xBox, yBox, "bottomleft")
	else
		local xBox = 1233 + (-1 * selectedItem - 1) * 193
		local yBox = 831
		textures["BottomPanelHover"]:Draw(xBox, yBox)
	end

	if currentScreen == "confirm" or currentScreen == "refresh" then
		if confirmUI then confirmUI:rect(0, 0, 1920, 1080, 6, 8, 16, 150) end   -- dim the shop behind the modal
		if confirmUI then confirmUI:draw() end
		-- the item preview sits on top of the panel body (in the gap above the price line), SCALED to a
		-- fixed box so large icons (vault-key PNGs) don't spill over the panel text
		if currentScreen == "confirm" and toBuyItem then
			local px, py = 960, 322
			if toBuyItem.Type == "nameplate" then
				NAMEPLATE:DrawNameplateTitleById(toBuyItem.RefInt, px, py, 255, text)
			elseif toBuyItemIcon ~= nil then
				drawSharedIcon(toBuyItemIcon, px, py, 168)   -- scales ANY texture down to the box
			end
		end
	end

	-- Player info
	drawPrice(1734, 1035, save.Coins)
	NAMEPLATE:DrawPlayerNameplate(20, 980, 255, playerIndex)
end

-- ── Navigation ────────────────────────────────────────────────────────────────

-- Build the custom cycle order
local function buildCycle()
  local cycle = {}

  -- odd indices first (from high to low)
  local oddStart = (layoutSize - 1) % 2 == 1 and (layoutSize - 1) or (layoutSize - 2)
  for i = oddStart, 1, -2 do
    table.insert(cycle, i)
  end

  -- even indices after (from high to low)
  local evenStart = (layoutSize - 1) % 2 == 0 and (layoutSize - 1) or (layoutSize - 2)
  for i = evenStart, 0, -2 do
    table.insert(cycle, i)
  end

  -- reroll and return
  table.insert(cycle, -1)
  table.insert(cycle, -2)

  return cycle
end

-- Move in the cycle
local function moveInCycle(direction)
  local cycle = buildCycle()
  -- find current index in cycle
  local idx
  for i,v in ipairs(cycle) do
      if v == selectedItem then
          idx = i
          break
      end
  end
  if not idx then return selectedItem end

  -- move left or right
  idx = idx + direction
  if idx < 1 then idx = #cycle end
  if idx > #cycle then idx = 1 end

  return cycle[idx]
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
	if confirmUI._totalLabel then confirmUI._totalLabel:setText(("Total: %d"):format(total)) end
	if confirmUI._buyBtn then confirmUI._buyBtn.enabled = (total <= save.Coins) end
end

local SHOP_SFX  -- set in activate() once sounds exist

-- confirm dialog: centred panel with the item name (title), a scaled preview (drawn in draw()), price,
-- an optional quantity stepper, total, and VERTICALLY-stacked Buy/Cancel (matches the up/down nav). Buy
-- is focused by default. BTN_W/CONFIRM_CX keep the two buttons aligned under the panel centre.
local CONFIRM_CX, BTN_W = 960, 340
local function buildConfirmUI(item, slot)
	if confirmUI then confirmUI:disposeWidgets() end
	confirmUI = PopUI.new{ theme = {}, sfx = SHOP_SFX }
	local cx, bx = CONFIRM_CX, CONFIRM_CX - BTN_W / 2
	local hasQty = (item.Stock or 1) > 1
	local panelH = hasQty and 720 or 640
	confirmUI:panel{ x = 660, y = 180, w = 600, h = panelH, title = item.LocalizedName or "" }
	confirmUI:label{ text = ("Price: %d"):format(item.Price), x = cx, y = 452, size = "label", align = "center" }
	local qtyChooser, totalY, buyY
	if hasQty then
		confirmUI:label{ text = "Quantity", x = cx, y = 508, size = "small", align = "center" }
		local opts = {}
		for i = 1, item.Stock do opts[i] = tostring(i) end
		qtyChooser = confirmUI:chooser{ x = cx - 170, y = 546, w = BTN_W, h = 62, options = opts, index = 1,
			wrap = false, onChange = function() updateConfirmTotals() end }
		totalY, buyY = 634, 706
	else
		totalY, buyY = 520, 596
	end
	local totalLabel = confirmUI:label{ text = ("Total: %d"):format(item.Price), x = cx, y = totalY, size = "label", align = "center" }
	local buyBtn = confirmUI:button{ text = "Buy", x = bx, y = buyY, w = BTN_W, h = 76, accent = true,
		onClick = function()
			local qty = (qtyChooser and tonumber(qtyChooser:value())) or 1
			if item.Price * qty > save.Coins then sounds.SoldOut:Play(); return end   -- can't afford (guarded)
			purchaseItemMultiple(item, slot, qty)
			closeConfirm()
		end }
	confirmUI:button{ text = "Cancel", x = bx, y = buyY + 92, w = BTN_W, h = 76,
		onClick = function() sounds.Cancel:Play(); closeConfirm() end }
	confirmUI._item, confirmUI._qty = item, qtyChooser
	confirmUI._totalLabel, confirmUI._buyBtn = totalLabel, buyBtn
	updateConfirmTotals()
	confirmUI:_setFocusIndex(qtyChooser and 2 or 1)   -- default focus/highlight on Buy
end

local function buildRefreshUI()
	if confirmUI then confirmUI:disposeWidgets() end
	confirmUI = PopUI.new{ theme = {}, sfx = SHOP_SFX }
	local cx, bx = CONFIRM_CX, CONFIRM_CX - BTN_W / 2
	local rerollPrice = math.floor(10 * (2 ^ executedRerolls))
	confirmUI:panel{ x = 660, y = 280, w = 600, h = 480, title = "Reroll" }
	confirmUI:label{ text = "Reshuffle the shop?", x = cx, y = 384, size = "label", align = "center" }
	confirmUI:label{ text = ("Cost: %d"):format(rerollPrice), x = cx, y = 448, size = "label", align = "center" }
	local rb = confirmUI:button{ text = "Reroll", x = bx, y = 528, w = BTN_W, h = 76, accent = true,
		onClick = function()
			if rerollPrice > save.Coins then sounds.SoldOut:Play(); return end
			save:SpendCoins(rerollPrice); executedRerolls = executedRerolls + 1; soldOutMask = 0
			poolItems(); storeShopState(shopDB); sounds.Buy:Play()
			closeConfirm()
		end }
	rb.enabled = (rerollPrice <= save.Coins)
	confirmUI:button{ text = "Cancel", x = bx, y = 620, w = BTN_W, h = 76,
		onClick = function() sounds.Cancel:Play(); closeConfirm() end }
	confirmUI:_setFocusIndex(1)   -- default focus/highlight on Reroll
end

-- ── Update ────────────────────────────────────────────────────────────────────

function update(ts)
	if currentScreen == "playerselect" then
		if confirmUI then
			local res = confirmUI:update(ts)
			if currentScreen ~= "playerselect" then       -- a save was picked (enterShopFor ran)
				confirmUI:disposeWidgets(); confirmUI = nil
			elseif res == "cancel" then
				sounds.Cancel:Play(); return Exit("title", nil)
			end
		end
		return
	end
	if currentScreen == "confirm" or currentScreen == "refresh" then
		if confirmUI then
			if confirmUI:update(ts) == "cancel" then      -- Escape: PopUI reports it; the buttons handle the rest
				sounds.Cancel:Play(); closeConfirm()
			end
		else
			currentScreen = "shop"
		end
	elseif currentScreen == "shop" then
		if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
			sounds.Skip:Play()
			selectedItem = moveInCycle(1)
		end
		if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
			sounds.Skip:Play()
			selectedItem = moveInCycle(-1)
		end
		if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
			sounds.Cancel:Play()
			return Exit("title", nil)
		end
		if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
				-- Back button
				if selectedItem == -2 then
					sounds.Decide:Play()
					return Exit("title", nil)
				-- Reroll button
				elseif selectedItem == -1 then
					sounds.Decide:Play()
					currentScreen = "refresh"
					buildRefreshUI()
				else
					if bigItem ~= nil and selectedItem >= #normalItems then
						toBuyItem = bigItem
						toBuyItemIcon = icons[5]
						toBuySlot = SLOT_BIG
					else
						local slotIdx = selectedItem + 1
						toBuyItem = normalItems[slotIdx]
						toBuyItemIcon = icons[slotIdx]
						toBuySlot = slotIdx
					end

					if toBuyItem ~= nil and toBuyItem.SoldOut == false then
						sounds.Decide:Play()
						currentScreen = "confirm"
						buildConfirmUI(toBuyItem, toBuySlot)
					else
						sounds.SoldOut:Play()
					end
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
		executedRerolls = 0; soldOutMask = 0; poolItems(); storeShopState(shopDB)
	else
		loadShopState(shopDB)
	end
	selectedItem = -2
	currentScreen = "shop"
end

-- on entering the shop, pick WHICH of the 5 local saves to browse (its coins/unlocks). Skips itself
-- when 0/1 saves exist.
local function openShopPlayerSelect()
	local entries = {}
	for i = 0, 4 do
		local sf = GetSaveFile(i)
		if sf and sf.SaveUID and sf.SaveUID ~= "" then
			entries[#entries + 1] = { text = ("Player %d — %s"):format(i + 1, sf.Name or ""), value = i }
		end
	end
	if #entries <= 1 then enterShopFor(entries[1] and entries[1].value or 0); return end
	if confirmUI then confirmUI:disposeWidgets() end
	confirmUI = PopUI.new{ theme = {}, sfx = SHOP_SFX }
	local x, y, w = 960 - 380, 210, 760
	local h = 120 + #entries * 78 + 40
	confirmUI:panel{ x = x, y = y, w = w, h = h, title = "Whose shop?" }
	confirmUI:menu{ x = x + 36, y = y + 92, w = w - 72, h = #entries * 78, rowHeight = 78, items = entries,
	                onSelect = function(_, it) enterShopFor(it.value) end }
	confirmUI:_setFocusIndex(1)
	currentScreen = "playerselect"
end

-- ── Lifecycle ─────────────────────────────────────────────────────────────────

function activate()
	save = nil
	SHOP_SFX = { move = function() sounds.Skip:Play() end, click = function() sounds.Decide:Play() end }

	-- (the old Confirm/Refresh/Buttons textures are no longer used — the confirm/reroll dialogs are PopUI)
	local txNm = {
		"Bg",
		"Selected",
		"StandNormal",
		"StandBig",
		"SoldOut",
		"PriceBox",
		"BottomPanel",
		"BottomPanelHover"
	}
	for _, v in pairs(txNm) do
		textures[v] = TEXTURE:CreateTexture("Textures/"..v..".png")
	end

	local charMap = "+-0123456789.(), x"
	TxTextChar = {}
	for i = 1, #charMap do
		local char = charMap:sub(i, i)
		TxTextChar[char] = text:GetText(char)
	end

	selectedItem = -2

	sounds.BGM:SetLoop(true)
	sounds.BGM:Play()

	openShopPlayerSelect()   -- pick whose shop to browse (per-SaveUID rolls); enters directly if ≤1 save
end

function deactivate()
	for _, v in pairs(textures) do
		v:Dispose()
	end
	textures = {}

	for k, v in pairs(icons) do
		if not sharedIcon[k] then v:Dispose() end   -- shared My Room textures are owned by the global store
	end
	icons = {}
	sharedIcon = {}

	-- for k, v in pairs(TxTextChar) do
	-- 	v:Dispose()
	-- end
	-- TxTextChar = {}

	if confirmUI then confirmUI:disposeWidgets(); confirmUI = nil end

	if shopDB then shopDB:Dispose() end
	shopDB = nil

	sounds.BGM:Stop()
end


function onStart()
	text = TEXT:Create(16)
	glyphFont = TEXT:CreateGlyphCached(30)
	STOCK_FG = COLOR:CreateColorFromHex("FFFFE9A0")
	STOCK_BG = COLOR:CreateColorFromHex("FF000000")

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
	if glyphFont ~= nil then
		glyphFont:Dispose()
		glyphFont = nil
	end
	for _, sound in pairs(sounds) do
		sound:Dispose()
	end
end
