---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- sort_search_dialog
-- Dialog activity for the song select screen.
-- Currently implements "sort" mode only; search mode is blank for later.
-- Activated with the player index as argument.
-- Sort state (one active method + direction) is persisted per player via save file global counters:
--   "ss_sort_method"  (0–6 integer)   which sort method is active
--   "ss_sort_dir"     (0–2 integer)   0=OFF(→filepath ASC), 1=ASC, 2=DESC

local bg      = nil
local text    = nil
local textSmall = nil
local sounds  = {}
local tx      = {}
local ctx     = {}

local activePlayer = 0
local reactive     = false

-- Slide-in transition
local bgpos  = 1080
local bgtlop = 0

local SORT_METHODS = {
	{ key = "filepath",    label = "File Path" },
	{ key = "title",       label = "Song Title" },
	{ key = "subtitle",    label = "Song Subtitle" },
	{ key = "level",       label = "Displayed Level" },
	{ key = "bpm",         label = "Base BPM" },
	{ key = "bestscore",   label = "Best Score" },
	{ key = "clearstatus", label = "Clear Status" },
}
local METHOD_KEY     = "ss_sort_method"
local DIR_KEY        = "ss_sort_dir"
local DEFAULT_METHOD = 0   -- filepath
local DEFAULT_DIR    = 1   -- ASC

local cursorIndex = 1  -- 1-based index into SORT_METHODS

-- ── Save helpers ────────────────────────────────────────────────────

local function readMethod()
	local v = math.floor(GetSaveFile(activePlayer):GetGlobalCounter(METHOD_KEY) + 0.5)
	if v < 0 or v >= #SORT_METHODS then v = DEFAULT_METHOD end
	return v  -- 0-based
end

local function readDir()
	local v = math.floor(GetSaveFile(activePlayer):GetGlobalCounter(DIR_KEY) + 0.5)
	if v < 0 or v > 2 then v = DEFAULT_DIR end
	return v
end

local function writeSort(methodIdx0, dirIdx)
	-- If direction is OFF (0), revert to default filepath ASC
	if dirIdx == 0 then methodIdx0 = DEFAULT_METHOD; dirIdx = DEFAULT_DIR end
	GetSaveFile(activePlayer):SetGlobalCounter(METHOD_KEY, methodIdx0)
	GetSaveFile(activePlayer):SetGlobalCounter(DIR_KEY, dirIdx)
end

-- ── Counter helper ───────────────────────────────────────────────────

local function startCounter(key, s, e, interval, mode, cb, onFinish)
	local c = COUNTER:CreateCounter(s, e, interval, onFinish)
	if mode == "loop" then c:SetLoop(true) end
	if cb ~= nil then c:Listen(cb) end
	ctx[key] = c
	c:Start()
end

local function updateTransitionVisuals(val)
	bgpos  = val
	local op = 255 - (val * (255 / 540))
	bgtlop = math.max(0, math.min(255, op))
end

-- ── Background draw ──────────────────────────────────────────────────

local function drawBg(opacity)
	tx["bgtile"]:SetOpacity((opacity * bgtlop) / 255)
	for i = 0, 10 do
		for j = 0, 10 do
			tx["bgtile"]:Draw(i * 192, j * 108)
		end
	end
end

-- ── Lifecycle ───────────────────────────────────────────────────────

function onStart()
	bg              = TEXTURE:CreateTexture("Textures/Background.png")
	tx["bgtile"]    = TEXTURE:CreateTexture("Textures/BgTile.png")
	text            = TEXT:Create(28)
	textSmall       = TEXT:Create(18)
	sounds.Move   = SHARED:GetSharedSound("Move")
	sounds.Decide = SHARED:GetSharedSound("Decide")
	sounds.Cancel = SHARED:GetSharedSound("Cancel")
end

function activate(player)
	activePlayer = player or 0
	cursorIndex  = readMethod() + 1  -- sync cursor to active method
	bgpos  = 1080
	bgtlop = 0
	startCounter("enter", 1080, 0, -0.5 / 1080, "none", updateTransitionVisuals, function()
		reactive = true
	end)
end

function deactivate()
	reactive = false
end

-- ── Update ──────────────────────────────────────────────────────────

function update()
	for _, c in pairs(ctx) do c:Tick() end
	if not reactive then return end

	-- Cursor navigation: LeftChange=up, RightChange=down
	if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("UpArrow") then
		sounds.Move:Play()
		cursorIndex = ((cursorIndex - 2) % #SORT_METHODS) + 1
	elseif INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("DownArrow") then
		sounds.Move:Play()
		cursorIndex = (cursorIndex % #SORT_METHODS) + 1
	end

	-- Direction cycling: right = ASC→DESC→OFF→ASC, left = reverse
	local curMethod0    = cursorIndex - 1  -- 0-based
	local activeMethod0 = readMethod()
	local activeDir     = readDir()

	-- Current dir for the pointed-at method (only the active method has a real dir)
	local pointedDir = (curMethod0 == activeMethod0) and activeDir or 0

	if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("RightArrow") or INPUT:Pressed("RBlue2P") then
		sounds.Decide:Play()
		-- Always start at ASC when selecting a fresh method
		local newDir = (pointedDir == 0) and 1 or ((pointedDir % 3) + 1)
		writeSort(curMethod0, newDir == 3 and 0 or newDir)  -- wrap 3 back to 0=OFF
	elseif INPUT:KeyboardPressed("LeftArrow") or INPUT:Pressed("LBlue2P") then
		sounds.Decide:Play()
		local newDir = (pointedDir - 1 + 3) % 3
		writeSort(curMethod0, newDir)
	end

	-- Close on Cancel / Escape
	if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
		sounds.Cancel:Play()
		reactive = false
		startCounter("exit", 0, 1080, 0.5 / 1080, "none", updateTransitionVisuals, function()
			DEACTIVATE()
		end)
	end
end

-- ── Draw ────────────────────────────────────────────────────────────

function draw()
	drawBg(0.5)
	bg:SetOpacity(bgtlop / 255)
	bg:Draw(0, bgpos)

	if bgtlop == 0 then return end

	local activeMethod0 = readMethod()
	local activeDir     = readDir()
	local DIR_LABELS    = { [0] = "OFF", [1] = "ASC", [2] = "DESC" }

	local cx    = 960
	local cy    = 280
	local alpha = bgtlop / 255

	local titleTx = text:GetText("Sort Songs", false, 600)
	titleTx:SetOpacity(alpha)
	titleTx:DrawAtAnchor(cx, cy - 70, "center")

	for i, method in ipairs(SORT_METHODS) do
		local isActive = (activeMethod0 == i - 1)
		local isCursor = (cursorIndex == i)
		local dir      = isActive and activeDir or 0

		local color
		if isCursor and isActive then
			color = COLOR:CreateColorFromARGB(255, 255, 220, 60)   -- gold: cursor + active
		elseif isCursor then
			color = COLOR:CreateColorFromARGB(255, 242, 207, 1)    -- yellow: cursor only
		elseif isActive then
			color = COLOR:CreateColorFromARGB(255, 140, 230, 140)  -- green: active, no cursor
		else
			color = COLOR:CreateColorFromARGB(255, 200, 200, 200)  -- grey: inactive
		end

		local dirStr = isActive and DIR_LABELS[dir] or "—"
		local rowTx  = text:GetText(method.label .. "     " .. dirStr, false, 760, color)
		rowTx:SetOpacity(alpha)
		rowTx:DrawAtAnchor(cx, cy + (i - 1) * 58, "center")
	end

	local hintTx = textSmall:GetText(
		"↑↓ Navigate     → Cycle ASC/DESC     ← Cycle Back     Esc Close",
		false, 960)
	hintTx:SetOpacity(alpha)
	hintTx:DrawAtAnchor(cx, cy + #SORT_METHODS * 58 + 50, "center")
end

function onDestroy()
	if bg        ~= nil then bg:Dispose()        end
	if text      ~= nil then text:Dispose()      end
	if textSmall ~= nil then textSmall:Dispose() end
end

function afterSongEnum() end
