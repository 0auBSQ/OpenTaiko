---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- sort_search_dialog — pure UI dialog for sorting and searching.
--
-- Sort state persisted via save-file global counters (per player):
--   "ss_sort_method"  (0–6)   active sort method
--   "ss_sort_dir"     (0–2)   0=OFF, 1=ASC, 2=DESC
--
-- When search OK is confirmed the params are written to shared storage and
-- a ready flag is set so song_select_core/search.lua can apply the search:
--   SHARED strings : "ss_title", "ss_subtitle", "ss_charter"
--   Save counters  : "ss_diff", "ss_levelFrom", "ss_levelFromP",
--                    "ss_levelTo",   "ss_levelToP",   "ss_levelToOE"
--   Save counter   : "ss_search_ready" = 1  (cleared by search.lua after reading)
--
-- activate(player, mode?)
--   player — which player's save file to use for sort persistence
--   mode   — "sort" (default) | "search"

local bg        = nil
local text      = nil
local textSmall = nil
local sounds    = {}
local tx        = {}
local ctx       = {}

local activePlayer = 0
local activeMode   = "sort"   -- "sort" | "search"
local reactive     = false

-- ── Slide-in transition ──────────────────────────────────────────────────────

local bgpos  = 1080
local bgtlop = 0

-- ── Sort state ───────────────────────────────────────────────────────────────

local SORT_METHODS = {
    { key = "filepath",    label = "File Path"       },
    { key = "title",       label = "Song Title"      },
    { key = "subtitle",    label = "Song Subtitle"   },
    { key = "level",       label = "Displayed Level" },
    { key = "bpm",         label = "Base BPM"        },
    { key = "bestscore",   label = "Best Score"      },
    { key = "clearstatus", label = "Clear Status"    },
}
local METHOD_KEY     = "ss_sort_method"
local DIR_KEY        = "ss_sort_dir"
local DEFAULT_METHOD = 0
local DEFAULT_DIR    = 1

local sortCursorIndex = 1   -- 1-based

-- ── Search state ─────────────────────────────────────────────────────────────

local activeBaseFolder = nil   -- LuaSongNode passed from navigation for live count

local DIFF_OPTIONS = {
    { label = "Any",      value = -1 },
    { label = "Easy",     value =  0 },
    { label = "Normal",   value =  1 },
    { label = "Hard",     value =  2 },
    { label = "Oni+Edit", value = 34 },
}

local LEVEL_OPTIONS = {
    { label = "Any",  value = -1, plus = false, openEnd = false },
    { label = "1",    value =  1, plus = false, openEnd = false },
    { label = "2",    value =  2, plus = false, openEnd = false },
    { label = "3",    value =  3, plus = false, openEnd = false },
    { label = "4",    value =  4, plus = false, openEnd = false },
    { label = "5",    value =  5, plus = false, openEnd = false },
    { label = "6",    value =  6, plus = false, openEnd = false },
    { label = "7",    value =  7, plus = false, openEnd = false },
    { label = "8",    value =  8, plus = false, openEnd = false },
    { label = "9",    value =  9, plus = false, openEnd = false },
    { label = "10",   value = 10, plus = false, openEnd = false },
    { label = "10+",  value = 10, plus = true,  openEnd = false },
    { label = "11",   value = 11, plus = false, openEnd = false },
    { label = "11+",  value = 11, plus = true,  openEnd = false },
    { label = "12",   value = 12, plus = false, openEnd = false },
    { label = "12+",  value = 12, plus = true,  openEnd = false },
    { label = "13~",  value = 13, plus = false, openEnd = true  },
}

local SF_DIFF      = 1
local SF_LEVELFROM = 2
local SF_LEVELTO   = 3
local SF_TITLE     = 4
local SF_SUBTITLE  = 5
local SF_CHARTER   = 6
local SF_OK        = 7
local SF_CANCEL    = 8
local SF_COUNT     = 8

local SF_FIELDS = {
    { label = "Difficulty",    ftype = "cycle"  },
    { label = "Level From",    ftype = "cycle"  },
    { label = "Level To",      ftype = "cycle"  },
    { label = "Song Title",    ftype = "text"   },
    { label = "Song Subtitle", ftype = "text"   },
    { label = "Charter",       ftype = "text"   },
    { label = "OK",            ftype = "button" },
    { label = "Cancel",        ftype = "button" },
}

local diffIdx      = 1
local levelFromIdx = 1
local levelToIdx   = #LEVEL_OPTIONS
local titleText    = ""
local subtitleText = ""
local charterText  = ""
local sfFieldIdx   = SF_DIFF
local textInput    = nil
local editField    = nil

-- ── Counter helper ────────────────────────────────────────────────────────────

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

-- ── Sort save helpers ─────────────────────────────────────────────────────────

local function readMethod()
    local v = math.floor(GetSaveFile(activePlayer):GetGlobalCounter(METHOD_KEY) + 0.5)
    if v < 0 or v >= #SORT_METHODS then v = DEFAULT_METHOD end
    return v
end

local function readDir()
    local v = math.floor(GetSaveFile(activePlayer):GetGlobalCounter(DIR_KEY) + 0.5)
    if v < 0 or v > 2 then v = DEFAULT_DIR end
    return v
end

local function writeSort(methodIdx0, dirIdx)
    if dirIdx == 0 then methodIdx0 = DEFAULT_METHOD; dirIdx = DEFAULT_DIR end
    GetSaveFile(activePlayer):SetGlobalCounter(METHOD_KEY, methodIdx0)
    GetSaveFile(activePlayer):SetGlobalCounter(DIR_KEY, dirIdx)
end

-- ── Search helpers (for live count) ──────────────────────────────────────────

local function buildPredicate(params)
    local diff           = params.diff
    local levelFrom      = params.levelFrom
    local levelFromPlus  = params.levelFromPlus
    local levelTo        = params.levelTo
    local levelToPlus    = params.levelToPlus
    local levelToOpenEnd = params.levelToOpenEnd
    local titlePat       = (params.title    or ""):lower()
    local subtitlePat    = (params.subtitle or ""):lower()
    local charterPat     = (params.charter  or ""):lower()

    local effFrom = (levelFrom == -1) and -math.huge
                    or (levelFrom + (levelFromPlus and 0.5 or 0))
    local effTo   = (levelToOpenEnd or levelTo == -1) and math.huge
                    or (levelTo + (levelToPlus and 0.5 or 0))

    local function isAcceptedDiff(i)
        if diff == -1 then return true end
        if diff == 34 then return i == 3 or i == 4 end
        return i == diff
    end

    return function(node)
        if not node.IsSong then return false end
        if titlePat    ~= "" and not (node.Title    or ""):lower():find(titlePat,    1, true) then return false end
        if subtitlePat ~= "" and not (node.Subtitle or ""):lower():find(subtitlePat, 1, true) then return false end

        local accepted = {}
        for i = 0, 4 do
            if isAcceptedDiff(i) then
                local chart = node:GetChart(i)
                if chart ~= nil then accepted[#accepted + 1] = chart end
            end
        end
        if #accepted == 0 then return false end

        local levelOk = false
        for _, chart in ipairs(accepted) do
            local eff = chart.Level + (chart.IsPlus and 0.5 or 0)
            if eff >= effFrom and eff <= effTo then levelOk = true; break end
        end
        if not levelOk then return false end

        if charterPat ~= "" then
            local ok = false
            for _, chart in ipairs(accepted) do
                if (chart.NotesDesigner or ""):lower():find(charterPat, 1, true) then ok = true; break end
            end
            if not ok then return false end
        end

        return true
    end
end

local function collectSongs(folderNode, predicate, results)
    for i = 0, folderNode.ChildrenCount - 1 do
        local node = folderNode:Child(i)
        if node.IsSong then
            if predicate(node) then results[#results + 1] = node end
        elseif node.IsFolder then
            collectSongs(node, predicate, results)
        end
    end
end

local function currentParams()
    return {
        diff           = DIFF_OPTIONS[diffIdx].value,
        levelFrom      = LEVEL_OPTIONS[levelFromIdx].value,
        levelFromPlus  = LEVEL_OPTIONS[levelFromIdx].plus,
        levelTo        = LEVEL_OPTIONS[levelToIdx].value,
        levelToPlus    = LEVEL_OPTIONS[levelToIdx].plus,
        levelToOpenEnd = LEVEL_OPTIONS[levelToIdx].openEnd,
        title          = titleText,
        subtitle       = subtitleText,
        charter        = charterText,
    }
end

local function countMatches()
    if activeBaseFolder == nil then return 0 end
    local results = {}
    collectSongs(activeBaseFolder, buildPredicate(currentParams()), results)
    return #results
end

-- ── Search param helpers ──────────────────────────────────────────────────────

local function commitSearchParams()
    local sav = GetSaveFile(activePlayer)
    SHARED:SetSharedString("ss_title",    titleText)
    SHARED:SetSharedString("ss_subtitle", subtitleText)
    SHARED:SetSharedString("ss_charter",  charterText)
    sav:SetGlobalCounter("ss_diff",          DIFF_OPTIONS[diffIdx].value)
    sav:SetGlobalCounter("ss_levelFrom",     LEVEL_OPTIONS[levelFromIdx].value)
    sav:SetGlobalCounter("ss_levelFromP",    LEVEL_OPTIONS[levelFromIdx].plus  and 1 or 0)
    sav:SetGlobalCounter("ss_levelTo",       LEVEL_OPTIONS[levelToIdx].value)
    sav:SetGlobalCounter("ss_levelToP",      LEVEL_OPTIONS[levelToIdx].plus    and 1 or 0)
    sav:SetGlobalCounter("ss_levelToOE",     LEVEL_OPTIONS[levelToIdx].openEnd and 1 or 0)
    sav:SetGlobalCounter("ss_search_ready",  1)
end

-- ── Background draw ───────────────────────────────────────────────────────────

local function drawBg(opacity)
    tx["bgtile"]:SetOpacity((opacity * bgtlop) / 255)
    for i = 0, 10 do
        for j = 0, 10 do tx["bgtile"]:Draw(i * 192, j * 108) end
    end
end

-- ── Lifecycle ─────────────────────────────────────────────────────────────────

function onStart()
    bg              = TEXTURE:CreateTexture("Textures/Background.png")
    tx["bgtile"]    = TEXTURE:CreateTexture("Textures/BgTile.png")
    text            = TEXT:Create(28)
    textSmall       = TEXT:Create(18)
    sounds.Move   = SHARED:GetSharedSound("Move")
    sounds.Decide = SHARED:GetSharedSound("Decide")
    sounds.Cancel = SHARED:GetSharedSound("Cancel")
    sounds.Skip   = SHARED:GetSharedSound("Skip")
end

function activate(player, mode, baseFolder)
    activePlayer     = player or 0
    activeMode       = mode or "sort"
    activeBaseFolder = baseFolder

    if activeMode == "sort" then
        sortCursorIndex = readMethod() + 1
    else
        sfFieldIdx = SF_DIFF
        if textInput ~= nil then textInput:Dispose(); textInput = nil end
        editField = nil
    end

    bgpos  = 1080
    bgtlop = 0
    startCounter("enter", 1080, 0, -0.5 / 1080, "none", updateTransitionVisuals, function()
        reactive = true
    end)
end

function deactivate()
    reactive = false
    if textInput ~= nil then textInput:Dispose(); textInput = nil end
    editField = nil
end

local function closeDialog()
    reactive = false
    startCounter("exit", 0, 1080, 0.5 / 1080, "none", updateTransitionVisuals, function()
        DEACTIVATE()
    end)
end

-- ── Update ────────────────────────────────────────────────────────────────────

local function updateSort()
    if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("UpArrow") then
        sounds.Skip:Play()
        sortCursorIndex = ((sortCursorIndex - 2) % #SORT_METHODS) + 1
    elseif INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("DownArrow") then
        sounds.Skip:Play()
        sortCursorIndex = (sortCursorIndex % #SORT_METHODS) + 1
    end

    local curMethod0 = sortCursorIndex - 1
    local pointedDir = (curMethod0 == readMethod()) and readDir() or 0

    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("RightArrow") or INPUT:Pressed("RBlue2P") then
        sounds.Decide:Play()
        local newDir = (pointedDir == 0) and 1 or ((pointedDir % 3) + 1)
        writeSort(curMethod0, newDir == 3 and 0 or newDir)
    elseif INPUT:KeyboardPressed("LeftArrow") or INPUT:Pressed("LBlue2P") then
        sounds.Decide:Play()
        local newDir = (pointedDir - 1 + 3) % 3
        writeSort(curMethod0, newDir)
    end

    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        sounds.Cancel:Play()
        closeDialog()
    end
end

local function updateSearch()
    -- Text input active
    if editField ~= nil and textInput ~= nil then
        local confirmed = textInput:Update()
        local raw       = textInput.Text
        if confirmed or INPUT:KeyboardPressed("Return") then
            sounds.Decide:Play()
            if editField == SF_TITLE    then titleText    = raw end
            if editField == SF_SUBTITLE then subtitleText = raw end
            if editField == SF_CHARTER  then charterText  = raw end
            textInput:Dispose(); textInput = nil; editField = nil
        elseif INPUT:KeyboardPressed("Escape") then
            sounds.Cancel:Play()
            textInput:Dispose(); textInput = nil; editField = nil
        end
        return
    end

    -- Field navigation
    if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("UpArrow") then
        sounds.Skip:Play()
        sfFieldIdx = ((sfFieldIdx - 2) % SF_COUNT) + 1
    elseif INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("DownArrow") then
        sounds.Skip:Play()
        sfFieldIdx = (sfFieldIdx % SF_COUNT) + 1
    end

    local field = SF_FIELDS[sfFieldIdx]

    if field.ftype == "cycle" then
        local function right()
            if sfFieldIdx == SF_DIFF          then diffIdx      = (diffIdx      % #DIFF_OPTIONS)  + 1
            elseif sfFieldIdx == SF_LEVELFROM then levelFromIdx = (levelFromIdx % #LEVEL_OPTIONS) + 1
            else                                   levelToIdx   = (levelToIdx   % #LEVEL_OPTIONS) + 1
            end
        end
        local function left()
            if sfFieldIdx == SF_DIFF          then diffIdx      = ((diffIdx      - 2) % #DIFF_OPTIONS)  + 1
            elseif sfFieldIdx == SF_LEVELFROM then levelFromIdx = ((levelFromIdx - 2) % #LEVEL_OPTIONS) + 1
            else                                   levelToIdx   = ((levelToIdx   - 2) % #LEVEL_OPTIONS) + 1
            end
        end
        if INPUT:KeyboardPressed("RightArrow") or INPUT:Pressed("RBlue2P")
                or INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
            sounds.Skip:Play(); right()
        elseif INPUT:KeyboardPressed("LeftArrow") or INPUT:Pressed("LBlue2P") then
            sounds.Skip:Play(); left()
        end

    elseif field.ftype == "text" then
        local function curText()
            if sfFieldIdx == SF_TITLE    then return titleText    end
            if sfFieldIdx == SF_SUBTITLE then return subtitleText end
            return charterText
        end
        local function clearText()
            if sfFieldIdx == SF_TITLE    then titleText    = "" end
            if sfFieldIdx == SF_SUBTITLE then subtitleText = "" end
            if sfFieldIdx == SF_CHARTER  then charterText  = "" end
        end
        if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
            sounds.Decide:Play()
            textInput = INPUT:CreateTextInput(curText(), 64)
            editField = sfFieldIdx
        elseif INPUT:KeyboardPressed("RightArrow") or INPUT:Pressed("RBlue2P")
                or INPUT:KeyboardPressed("LeftArrow") or INPUT:Pressed("LBlue2P") then
            if curText() ~= "" then sounds.Skip:Play(); clearText() end
        end

    elseif field.ftype == "button" then
        if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
            if sfFieldIdx == SF_OK then
                sounds.Decide:Play()
                commitSearchParams()
                closeDialog()
            else
                sounds.Cancel:Play()
                closeDialog()
            end
        end
    end

    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        sounds.Cancel:Play()
        closeDialog()
    end
end

function update()
    for _, c in pairs(ctx) do c:Tick() end
    if not reactive then return end

    if activeMode == "sort" then
        updateSort()
    else
        updateSearch()
    end
end

-- ── Draw ──────────────────────────────────────────────────────────────────────

local function drawSort(alpha, cx)
    local activeMethod0 = readMethod()
    local activeDir     = readDir()
    local DIR_LABELS    = { [0] = "OFF", [1] = "ASC", [2] = "DESC" }

    local cy    = 280
    local title = text:GetText("Sort Songs", false, 600)
    title:SetOpacity(alpha); title:DrawAtAnchor(cx, cy - 70, "center"); title:SetOpacity(1)

    for i, method in ipairs(SORT_METHODS) do
        local isActive = (activeMethod0 == i - 1)
        local isCursor = (sortCursorIndex == i)
        local dir      = isActive and activeDir or 0
        local color
        if isCursor and isActive then
            color = COLOR:CreateColorFromARGB(255, 255, 220, 60)
        elseif isCursor then
            color = COLOR:CreateColorFromARGB(255, 242, 207, 1)
        elseif isActive then
            color = COLOR:CreateColorFromARGB(255, 140, 230, 140)
        else
            color = COLOR:CreateColorFromARGB(255, 200, 200, 200)
        end
        local dirStr = isActive and DIR_LABELS[dir] or "—"
        local rowTx  = text:GetText(method.label .. "     " .. dirStr, false, 760, color)
        rowTx:SetOpacity(alpha); rowTx:DrawAtAnchor(cx, cy + (i - 1) * 58, "center"); rowTx:SetOpacity(1)
    end

    local hint = textSmall:GetText(
        "↑↓ Navigate     → Cycle ASC/DESC     ← Cycle Back     Esc Close", false, 960)
    hint:SetOpacity(alpha); hint:DrawAtAnchor(cx, cy + #SORT_METHODS * 58 + 50, "center"); hint:SetOpacity(1)
end

local function drawSearch(alpha, cx)
    local cy   = 160
    local rowH = 68

    local lblCol = COLOR:CreateColorFromARGB(math.floor(200 * alpha), 180, 180, 180)
    local selCol = COLOR:CreateColorFromARGB(math.floor(255 * alpha), 242, 207,   1)
    local valCol = COLOR:CreateColorFromARGB(math.floor(255 * alpha), 255, 255, 255)

    local titleTx = text:GetText("Search Songs", false, 600)
    titleTx:SetOpacity(alpha); titleTx:DrawAtAnchor(cx, cy - 60, "center"); titleTx:SetOpacity(1)

    for i, field in ipairs(SF_FIELDS) do
        local isCursor = (sfFieldIdx == i)
        local y        = cy + (i - 1) * rowH
        local nameCol  = isCursor and selCol or lblCol

        if field.ftype == "cycle" then
            local valStr
            if i == SF_DIFF          then valStr = DIFF_OPTIONS[diffIdx].label
            elseif i == SF_LEVELFROM then valStr = LEVEL_OPTIONS[levelFromIdx].label
            else                          valStr = LEVEL_OPTIONS[levelToIdx].label
            end
            local lbl = text:GetText(field.label, false, 300, nameCol)
            local val = text:GetText(valStr,      false, 300, isCursor and selCol or valCol)
            lbl:SetOpacity(alpha); lbl:DrawAtAnchor(cx - 20, y, "right"); lbl:SetOpacity(1)
            val:SetOpacity(alpha); val:DrawAtAnchor(cx + 20, y, "left");  val:SetOpacity(1)

        elseif field.ftype == "text" then
            local raw     = (i == SF_TITLE and titleText or i == SF_SUBTITLE and subtitleText or charterText)
            local isEdit  = (editField == i)
            local display = (isEdit and textInput ~= nil) and textInput.DisplayText
                            or (raw == "" and "Any" or raw)
            local dispCol = (raw == "" and not isEdit) and lblCol or valCol
            local lbl = text:GetText(field.label, false, 300, nameCol)
            local val = text:GetText(display,     false, 380, isCursor and selCol or dispCol)
            lbl:SetOpacity(alpha); lbl:DrawAtAnchor(cx - 20, y, "right"); lbl:SetOpacity(1)
            val:SetOpacity(alpha); val:DrawAtAnchor(cx + 20, y, "left");  val:SetOpacity(1)

        elseif field.ftype == "button" then
            local bCol = isCursor and selCol or valCol
            local btn  = text:GetText(field.label, false, 200, bCol)
            btn:SetOpacity(alpha); btn:DrawAtAnchor(cx, y, "center"); btn:SetOpacity(1)
        end
    end

    -- Live match count
    local count    = countMatches()
    local countCol = COLOR:CreateColorFromARGB(math.floor(180 * alpha), 180, 230, 180)
    local countStr = count .. (count == 1 and " song found" or " songs found")
    local cTx = textSmall:GetText(countStr, false, 600, countCol)
    cTx:SetOpacity(alpha); cTx:DrawAtAnchor(cx, cy + SF_COUNT * rowH + 20, "center"); cTx:SetOpacity(1)

    local hint = textSmall:GetText(
        "↑↓ Navigate     ←→ Cycle / Clear     Decide Edit text     Esc Close", false, 960)
    hint:SetOpacity(alpha); hint:DrawAtAnchor(cx, cy + SF_COUNT * rowH + 50, "center"); hint:SetOpacity(1)
end

function draw()
    drawBg(0.5)
    bg:SetOpacity(bgtlop / 255)
    bg:Draw(0, bgpos)

    if bgtlop == 0 then return end
    local alpha = bgtlop / 255
    local cx    = 960

    if activeMode == "sort" then
        drawSort(alpha, cx)
    else
        drawSearch(alpha, cx)
    end
end

function onDestroy()
    if bg        ~= nil then bg:Dispose()        end
    if text      ~= nil then text:Dispose()      end
    if textSmall ~= nil then textSmall:Dispose() end
    if textInput ~= nil then textInput:Dispose(); textInput = nil end
end

function afterSongEnum() end
