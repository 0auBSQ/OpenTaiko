local DBItems = require("DBControllers/dbItems")

local save = nil

-- Assets
local sounds = {}
local textures = {}
local icons = {}

local text = nil
local TxTextChar = {}

-- Menu navigation
local layoutSize = 5
local selectedItem = -2

-- Items
local bigItem = nil
local normalItems = {}

-- Confirm screen
local toBuyItem = nil
local toBuyItemIcon = nil
local confirmIdx = 0

-- Rerolls
local executedRerolls = 0

-- Current Screen
local currentScreen = "shop"

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

function isUnique(entry)
	if entry.Type == "counterable" then
		return false
	end
	return true
end

function isOwned(entry)
	if entry.Type == "triggerable" then
		return save:GetGlobalTrigger(entry.RefText)
	end
	if entry.Type == "nameplate" then
		return save:IsNameplateUnlocked(entry.RefInt)
	end
	return false
end

function isEntryIncluded(entry)
	if entry.Condition ~= nil then
		return save:GetGlobalTrigger(entry.Condition)
	end
	if isUnique(entry) then
		return not isOwned(entry)
	end
	return true
end

function entryHasicon(entry)
	if entry.Type == "nameplate" then
		return false
	end
	return true
end

local _cachedPools = nil

function poolItems()
	math.randomseed(os.time() + tonumber(tostring({}):sub(8), 16))

	local itemPools = {
		normal = {},
		big = {}
	}

	local poolSizes = {
		normal = 0,
		big = 0
	}


	if _cachedPools == nil then
		_cachedPools = {
			normal = cloneTable(DBItems:GetItems("regular")),
			big = cloneTable(DBItems:GetItems("major"))
		}
	end
	local _pools = _cachedPools


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

	-- TODO: Use freeze key to freeze each day shop in a module storage
	local now = os.date("*t")
	local freezeKey = now.year * 10000 + now.month * 100 + now.day

	-- helper to roll from weighted pool
	local function pickFromPool(pool, poolSize, roll)
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
		bigItem = pickFromPool(itemPools.big, poolSizes.big, roll)
		if bigItem.Type == "empty" then
			-- a nothing is picked (can happen to have days with 6 small slots)
			bigItem = nil
		else
			bigItem.LocalizedName = LANG:FromString(bigItem.Name):GetString("")
			bigItem.NameTx = text:GetText(bigItem.LocalizedName, true, 380)
			bigItem.SoldOut = false
			if entryHasicon(bigItem) then
				icons[5] = TEXTURE:CreateTexture("Textures/Icons/"..bigItem.Code..".png")
			else
				icons[5] = nil
			end
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
			local chosen = pickFromPool(pool, poolSize, roll)
			normalItems[n] = deepcopy(chosen)
			normalItems[n].LocalizedName = LANG:FromString(normalItems[n].Name):GetString("")
			normalItems[n].NameTx = text:GetText(normalItems[n].LocalizedName, true, 380)
			normalItems[n].SoldOut = false
			if entryHasicon(chosen) then
				icons[n] = TEXTURE:CreateTexture("Textures/Icons/"..chosen.Code..".png")
			else
				icons[n] = nil
			end


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
	for k,v in pairs(normalItems) do
		debugLog(v.LocalizedName .. " (" .. v.Code .. ")")
	end
end

function drawPrice(x, y, price)
	local color = COLOR:CreateColorFromHex("FFFFFFFF")
	if price > save.Coins then
		color = COLOR:CreateColorFromHex("FFFF0000")
	end
	drawNumberColor(x, y, tostring(price), color, true)
end

function drawPriceWithTag(x, y, price)
	textures["PriceBox"]:Draw(x, y)
	drawPrice(x+166, y+26, price)
end

function draw()
	textures["Bg"]:Draw(0, 0)

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
			elseif iconTex ~= nil then
				iconTex:Draw(xOrig, yOrig)
			end

			-- Name
			v.NameTx:DrawAtAnchor(xOrig + 200, yOrig + 440, "center")

			-- Price
			drawPriceWithTag(xOrig, yOrig + 320, v.Price)
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
			elseif iconTex ~= nil then
				iconTex:Draw(0, 0)
			end

			-- Name
			bigItem.NameTx:DrawAtAnchor(200, 920, "center")

			-- Price
			drawPriceWithTag(0, 800, bigItem.Price)
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
		if currentScreen == "confirm" then
			textures["Confirm"]:Draw(0, 0)

			-- Display the selected item within the box here
			local xCenter = 551
			local yCenter = 490
			if toBuyItem.Type == "nameplate" then
				NAMEPLATE:DrawNameplateTitleById(toBuyItem.RefInt, xCenter, yCenter - 40, 255, text)
			elseif toBuyItemIcon ~= nil then
				toBuyItemIcon:DrawAtAnchor(xCenter, yCenter, "center")
			end
			drawPriceWithTag(364, 693, toBuyItem.Price)

			-- Display currently owned amount if counterable
			if toBuyItem.Type == "counterable" then
				local _count = save:GetGlobalCounter(toBuyItem.RefText)
				local _tx = text:GetText("Inventory: "..("%d"):format(_count), true, 600)
				_tx:DrawAtAnchor(550, 830, "center")
			end
		else
			textures["Refresh"]:Draw(0, 0)
			drawPriceWithTag(380, 440, rerollPrice)
		end

		textures["Buttons"]:Draw(211, 878)
		textures["ButtonsHover"]:Draw(211 + confirmIdx * 433, 878)
	end

	-- Player info
	drawPrice(1734, 1035, save.Coins)
	NAMEPLATE:DrawPlayerNameplate(20, 980, 255, 0)
end

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


local function purchaseItem(item)
	if item.Type == "nameplate" then
		save:UnlockNameplate(item.RefInt)
	elseif item.Type == "triggerable" then
		save:SetGlobalTrigger(item.RefText, true)
	elseif item.Type == "counterable" then
		local _count = save:GetGlobalCounter(item.RefText)
		save:SetGlobalCounter(item.RefText, _count + item.RefInt)
	end
	save:SpendCoins(item.Price)
	toBuyItem.SoldOut = true
end

function update()
	if currentScreen == "confirm" or currentScreen == "refresh" then
		if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") or INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
			sounds.Skip:Play()
			confirmIdx = 1 - confirmIdx
		elseif INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") or ((INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return")) and confirmIdx == 1) then
			sounds.Cancel:Play()
			currentScreen = "shop"
		elseif (INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return")) and confirmIdx == 0 then
			if currentScreen == "confirm" then
				purchaseItem(toBuyItem)
				sounds.Buy:Play()
			elseif currentScreen == "refresh" then
				local rerollPrice = math.floor(10 * (2 ^ executedRerolls))
				sounds.Buy:Play()
				save:SpendCoins(rerollPrice)
				executedRerolls = executedRerolls + 1
				poolItems()
			end
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
					local rerollPrice = math.floor(10 * (2 ^ executedRerolls))
					if rerollPrice <= save.Coins then
						currentScreen = "refresh"
						confirmIdx = 0
					else
						sounds.SoldOut:Play()
					end
				else
					if bigItem ~= nil and selectedItem >= #normalItems then
						toBuyItem = bigItem
						toBuyItemIcon = icons[5]
					else
						toBuyItem = normalItems[selectedItem + 1]
						toBuyItemIcon = icons[selectedItem + 1]
					end

					if toBuyItem ~= nil and toBuyItem.SoldOut == false and toBuyItem.Price <= save.Coins then
						currentScreen = "confirm"
						confirmIdx = 0
					else
						sounds.SoldOut:Play()
					end
				end
		end
	end
end


function activate()
	save = GetSaveFile(0)

	local txNm = {
		"Bg",
		"Selected",
		"StandNormal",
		"StandBig",
		"SoldOut",
		"PriceBox",
		"BottomPanel",
		"BottomPanelHover",
		"Confirm",
		"Refresh",
		"Buttons",
		"ButtonsHover"
	}
	for _, v in pairs(txNm) do
		textures[v] = TEXTURE:CreateTexture("Textures/"..v..".png")
	end

	local charMap = "+-0123456789.(), "
	TxTextChar = {}
	for i = 1, #charMap do
		local char = charMap:sub(i, i)
		TxTextChar[char] = text:GetText(char)
	end

	selectedItem = -2
	currentScreen = "shop"

	-- Either pool or get the frozen shop on opening
	executedRerolls = 0
	poolItems()

	sounds.BGM:SetLoop(true)
	sounds.BGM:Play()
end

function deactivate()
	for _, v in pairs(textures) do
		v:Dispose()
	end
	textures = {}

	for _, v in pairs(icons) do
		v:Dispose()
	end
	icons = {}

	-- for k, v in pairs(TxTextChar) do
	-- 	v:Dispose()
	-- end
	-- TxTextChar = {}

	sounds.BGM:Stop()
end


function onStart()
	text = TEXT:Create(16)

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
	for _, sound in pairs(sounds) do
		sound:Dispose()
	end
end
