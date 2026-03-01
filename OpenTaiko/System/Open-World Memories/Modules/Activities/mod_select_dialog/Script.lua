-- Reactive once the enter animation is done
local reactive = false
local player = 0
local save = nil

-- Mod options
local options = {}

local SETTER_MAP = {
    ["auto"]         = "SetAutoStatus",
    ["scroll-speed"] = "SetScrollSpeed",
    ["game-mode"]    = "SetGameType",
    ["timing"]       = "SetTimingZone",
    ["just"]         = "SetJusticeMod",
    ["invisible"]    = "SetStealthMod",
    ["shuffle"]      = "SetRandomMod",
    ["fun-mod"]      = "SetFunMod"
}

-- Fonts
local textSmall = nil
local text = nil
local textLarge = nil

-- Textures
local tx = {}

-- Sounds
local sounds = {}

-- Animations and counters
local ctx = {}

local bgpos = 1080
local bgtlop = 0

-- ============================================================
-- UI Layout Constants
-- ============================================================
local MENU_TITLE_X          = 200    -- X of the "Mod Select" title
local MENU_TITLE_Y          = 60     -- Y of the title
local MENU_ORIGIN_Y         = 150    -- Y of the first option row
local MENU_OPTION_SPACING_Y = 82     -- Vertical gap between option rows
local MENU_LABEL_X          = 200    -- X of the option name label
local MENU_CHOICES_ORIGIN_X = 560    -- X where choices/values start
local MENU_CHOICE_SPACING_X = 155    -- Horizontal gap between individual choices
local MENU_FOOTER_Y         = 835    -- Y of the OK / Cancel row
local MENU_OK_X             = 700    -- X of the OK button
local MENU_CANCEL_X         = 900    -- X of the Cancel button
local MENU_DESC_Y           = 950    -- Y of the description line at the bottom
local MENU_DESC_X           = 200    -- X of the description line

-- ============================================================
-- Color palette (hex strings for COLOR:CreateColorFromHex)
-- ============================================================
local COL_WHITE      = "FFFFFFFF"
local COL_GRAY       = "FF777777"
local COL_HIGHLIGHT  = "FFF2CF01"   -- selected option label
local COL_GREEN      = "FF00DD00"
local COL_LIGHTGREEN = "FF88EE88"
local COL_ORANGE     = "FFFFAA00"
local COL_RED        = "FFFF5555"

-- ============================================================
-- Static option definitions (choices, colors, per-choice desc)
-- ============================================================
local OPTION_DEFS = {
    {
        meta = "auto",
        text = "Auto",
        desc = "Watch a perfect play of the selected chart.",
        type = "multi",
        choices = {
            { label = "No",  color = COL_WHITE, desc = "Play the chart yourself." },
            { label = "Yes", color = COL_WHITE, desc = "Watch a CPU perfect auto-play." },
        }
    },
    {
        meta = "scroll-speed",
        text = "Scroll Speed",
        desc = "Determines how fast the notes scroll through the lane.",
        type = "scroll",
        min  = 0,
        max  = 99,
    },
    {
        meta = "game-mode",
        text = "Game Mode",
        desc = "Play with your favorite instrument!",
        type = "multi",
        choices = {
            { label = "Taiko", color = COL_WHITE, desc = "Play with a Taiko drum." },
            { label = "Bongo", color = COL_WHITE, desc = "Play with bongos." },
        }
    },
    {
        meta = "timing",
        text = "Timing",
        desc = "Adjust the precision required for a good/ok/bad input.",
        type = "multi",
        choices = {
            { label = "Loose",    color = COL_GREEN,      desc = "Very forgiving timing windows, for beginners." },
            { label = "Lenient",  color = COL_LIGHTGREEN, desc = "Forgiving timing windows." },
            { label = "Normal",   color = COL_WHITE,      desc = "Standard timing windows." },
            { label = "Strict",   color = COL_ORANGE,     desc = "Tight timing windows." },
            { label = "Rigorous", color = COL_RED,        desc = "Very tight timing windows, for experts only." },
        }
    },
    {
        meta = "just",
        text = "Just",
        desc = "Only 2 judge zones, all or nothing.",
        type = "multi",
        choices = {
            { label = "Normal", color = COL_WHITE, desc = "Standard 3-zone judgement." },
            { label = "Just",   color = COL_RED,   desc = "Good or Miss only. No leniency at all." },
            { label = "Safe",   color = COL_GREEN, desc = "Ok counts as Good. Very forgiving." },
        }
    },
    {
        meta = "invisible",
        text = "Invisible",
        desc = "Hide the notes and rely on your memory to win!",
        type = "multi",
        choices = {
            { label = "None",    color = COL_WHITE, desc = "Notes are fully visible." },
            { label = "Doron",   color = COL_WHITE, desc = "Invisible notes, but SE notes indications are still here." },
            { label = "Stealth", color = COL_WHITE, desc = "Nothing here, only your memory can help you!" },
        }
    },
    {
        meta = "shuffle",
        text = "Shuffle",
        desc = "Swap dons and kas to challenge your muscle memory!",
        type = "multi",
        choices = {
            { label = "None",    color = COL_WHITE, desc = "No note modification." },
            { label = "Random",  color = COL_WHITE, desc = "Don/Ka notes are randomly swapped." },
            { label = "Mirror",  color = COL_WHITE, desc = "Don/Ka notes are mirrored." },
            { label = "Chaos",   color = COL_WHITE, desc = "Notes are randomly rearranged." },
            { label = "MRandom", color = COL_WHITE, desc = "Mirror and Random combined." },
        }
    },
    {
        meta = "fun-mod",
        text = "Fun Mod",
        desc = "Add some goofiness to your play!",
        type = "multi",
        choices = {
            { label = "None",        color = COL_WHITE, desc = "No fun mod active." },
            { label = "Avalanche",   color = COL_WHITE, desc = "Very chaotic scroll speeds!" },
            { label = "Minesweeper", color = COL_WHITE, desc = "Watch out for notes swapped to bombs!" },
        }
    },
}

-- Total selectable items in step 1 (8 options + OK + Cancel)
local OPTION_COUNT  = #OPTION_DEFS
local OK_INDEX      = OPTION_COUNT        -- 0-based index
local CANCEL_INDEX  = OPTION_COUNT + 1
local TOTAL_ITEMS   = OPTION_COUNT + 2

-- Navigation state
local selectedIndex = 0    -- 0-based; 0..OPTION_COUNT-1 = options, OK_INDEX, CANCEL_INDEX
local editingOption = false -- true when in step 2 (editing a specific option)

-- ============================================================
-- Helper: counter
-- ============================================================
local function startCounter(key, startVal, endVal, interval, mode, updateCallback, onFinish)
    local c = COUNTER:CreateCounter(startVal, endVal, interval, onFinish)
    if mode == "loop" then c:SetLoop(true)
    elseif mode == "bounce" then c:SetBounce(true) end
    if updateCallback then c:Listen(updateCallback) end
    c:Start()
    ctx[key] = c
    return c
end

local function updateTransitionVisuals(val)
    bgpos = val
    local bgOpacity = 255 - (val * (255 / 540))
    bgtlop = math.max(0, math.min(255, bgOpacity))
end

-- ============================================================
-- Helper: background draw (saves ~8 MB of RAM vs full texture)
-- ============================================================
local function drawBg(opacity)
    tx["bgtile"]:SetOpacity((opacity * bgtlop) / 255)
    for i = 0, 10, 1 do
        for j = 0, 10, 1 do
            tx["bgtile"]:Draw(i * 192, j * 108)
        end
    end
end

-- ============================================================
-- Helper: scroll speed display  e.g. value 19 -> "2.0"
-- ============================================================
local function scrollSpeedDisplay(value)
    return string.format("%.1f", (value + 1) / 10)
end

-- ============================================================
-- Helper: footer description depending on current state
-- ============================================================
local function getCurrentDesc()
    if selectedIndex == OK_INDEX then
        return "Confirm all changes and exit the menu."
    elseif selectedIndex == CANCEL_INDEX then
        return "Discard all changes and exit the menu."
    end

    local opt = options[selectedIndex + 1]
    if opt == nil then return "" end

    -- In edit mode, show per-choice desc (scroll always shows general desc)
    if editingOption and opt.type ~= "scroll" then
        local choice = opt.choices[opt.value + 1]
        if choice then return choice.desc end
    end

    return opt.desc
end

-- ============================================================
-- loadOptions – reads current config values into the options table
-- ============================================================
local function loadOptions()
    options = {}
    for _, def in ipairs(OPTION_DEFS) do
        local val
        if     def.meta == "auto"         then val = CONFIG:GetAutoStatus(player) and 1 or 0
        elseif def.meta == "scroll-speed" then val = CONFIG:GetScrollSpeed(player)
        elseif def.meta == "game-mode"    then val = CONFIG:GetGameType(player)
        elseif def.meta == "timing"       then val = CONFIG:GetTimingZone(player)
        elseif def.meta == "just"         then val = CONFIG:GetJusticeMod(player)
        elseif def.meta == "invisible"    then val = CONFIG:GetStealthMod(player)
        elseif def.meta == "shuffle"      then val = CONFIG:GetRandomMod(player)
        elseif def.meta == "fun-mod"      then val = CONFIG:GetFunMod(player)
        end
        table.insert(options, {
            meta    = def.meta,
            value   = val,
            text    = def.text,
            desc    = def.desc,
            type    = def.type,
            choices = def.choices,  -- nil for scroll type
            min     = def.min,      -- scroll only
            max     = def.max,      -- scroll only
        })
    end
end

-- ============================================================
-- saveOptions – writes modified values back to config
-- ============================================================
local function saveOptions()
    for _, option in ipairs(options) do
        local methodName = SETTER_MAP[option.meta]
        if methodName and CONFIG[methodName] then
            local value = option.value
            if option.meta == "auto" then value = (value == 1) end  -- convert back to bool
            CONFIG[methodName](CONFIG, player, value)
        else
            debugLog("No setter mapping found for: " .. tostring(option.meta))
        end
    end
end

-- ============================================================
-- Draw
-- ============================================================
function draw()
    drawBg(0.5)
    tx["bg"]:SetOpacity(bgtlop / 255)
    tx["bg"]:Draw(0, bgpos)

    if bgtlop == 0 then return end
    local alpha = bgtlop / 255

    -- Title
    local titleTx = textLarge:GetText("Mod Select", false, 800)
    titleTx:SetOpacity(alpha)
    titleTx:Draw(MENU_TITLE_X, MENU_TITLE_Y)

    -- --------------------------------------------------------
    -- Option rows
    -- --------------------------------------------------------
    for i, opt in ipairs(options) do
        local idx    = i - 1
        local ypos   = MENU_ORIGIN_Y + idx * MENU_OPTION_SPACING_Y
        local isSel  = (selectedIndex == idx)
        local isEdit = (editingOption and isSel)

        -- Label (highlighted when row is selected in step 1 or being edited in step 2)
        local labelCol = isSel and COLOR:CreateColorFromHex(COL_HIGHLIGHT)
                                or COLOR:CreateColorFromHex(COL_WHITE)
        local labelTx = text:GetText(opt.text, false, 340, labelCol)
        labelTx:SetOpacity(alpha)
        labelTx:Draw(MENU_LABEL_X, ypos)

        -- Value area
        if opt.type == "scroll" then
            -- Single value display; highlighted when being edited
            local valCol = isEdit and COLOR:CreateColorFromHex(COL_HIGHLIGHT)
                                   or COLOR:CreateColorFromHex(COL_WHITE)
            local valTx = text:GetText(scrollSpeedDisplay(opt.value), false, 200, valCol)
            valTx:SetOpacity(alpha)
            valTx:Draw(MENU_CHOICES_ORIGIN_X, ypos)
        else
            -- Choices side by side
            local xpos = MENU_CHOICES_ORIGIN_X
            for ci, choice in ipairs(opt.choices) do
                local choiceVal = ci - 1
                local isActive  = (opt.value == choiceVal)

                -- Color logic:
                --   Active choice: use its defined color
                --   Inactive choice: gray
                --   When NOT in edit mode the active choice is always shown in its color
                --     so the player can see what is currently set at a glance
                local choiceCol
                if isActive then
                    choiceCol = COLOR:CreateColorFromHex(choice.color)
                else
                    choiceCol = COLOR:CreateColorFromHex(COL_GRAY)
                end

                local choiceTx = text:GetText(choice.label, false, MENU_CHOICE_SPACING_X - 10, choiceCol)
                choiceTx:SetOpacity(alpha)
                choiceTx:Draw(xpos, ypos)
                xpos = xpos + MENU_CHOICE_SPACING_X
            end
        end
    end

    -- --------------------------------------------------------
    -- OK / Cancel footer buttons
    -- --------------------------------------------------------
    local okCol  = (selectedIndex == OK_INDEX)     and COLOR:CreateColorFromHex(COL_HIGHLIGHT)
                                                   or  COLOR:CreateColorFromHex(COL_WHITE)
    local canCol = (selectedIndex == CANCEL_INDEX) and COLOR:CreateColorFromHex(COL_HIGHLIGHT)
                                                   or  COLOR:CreateColorFromHex(COL_WHITE)

    local okTx  = text:GetText("OK",     false, 200, okCol)
    local canTx = text:GetText("Cancel", false, 200, canCol)
    okTx:SetOpacity(alpha)
    canTx:SetOpacity(alpha)
    okTx:Draw(MENU_OK_X,     MENU_FOOTER_Y)
    canTx:Draw(MENU_CANCEL_X, MENU_FOOTER_Y)

    -- --------------------------------------------------------
    -- Description line (bottom)
    -- --------------------------------------------------------
    local descStr = getCurrentDesc()
    if descStr and descStr ~= "" then
        local descTx = textSmall:GetText(descStr, false, 1550)
        descTx:SetOpacity(alpha)
        descTx:Draw(MENU_DESC_X, MENU_DESC_Y)
    end
end

-- ============================================================
-- Update
-- ============================================================
function update()
    for k, counter in pairs(ctx) do
        counter:Tick()
    end

    if reactive == false then return end

    if editingOption == false then
        -- --------------------------------------------------------
        -- Step 1 – navigate the list
        -- --------------------------------------------------------
        if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
            sounds.Skip:Play()
            selectedIndex = (selectedIndex + 1) % TOTAL_ITEMS

        elseif INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
            sounds.Skip:Play()
            selectedIndex = (selectedIndex - 1 + TOTAL_ITEMS) % TOTAL_ITEMS

        elseif INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
            if selectedIndex == OK_INDEX then
                sounds.Decide:Play()
                reactive = false
                startCounter("screen_transition", 0, 1080, 0.5/1080, "none", updateTransitionVisuals, function()
                    saveOptions()
                    DEACTIVATE()
                end)

            elseif selectedIndex == CANCEL_INDEX then
                sounds.Cancel:Play()
                reactive = false
                startCounter("screen_transition", 0, 1080, 0.5/1080, "none", updateTransitionVisuals, function()
                    DEACTIVATE()
                end)

            else
                -- Enter edit mode for this option
                sounds.Decide:Play()
                editingOption = true
            end

        elseif INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
            -- Escape from menu without saving
            sounds.Cancel:Play()
            reactive = false
            startCounter("screen_transition", 0, 1080, 0.5/1080, "none", updateTransitionVisuals, function()
                DEACTIVATE()
            end)
        end

    else
        -- --------------------------------------------------------
        -- Step 2 – edit selected option value
        -- --------------------------------------------------------
        local opt = options[selectedIndex + 1]
        if opt == nil then editingOption = false return end

        if opt.type == "scroll" then
            -- Left/right move value by 1, clamped to [min, max]
            if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
                sounds.Skip:Play()
                opt.value = math.min(opt.max, opt.value + 1)

            elseif INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
                sounds.Skip:Play()
                opt.value = math.max(opt.min, opt.value - 1)

            elseif INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
                sounds.Decide:Play()
                editingOption = false

            elseif INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
                sounds.Cancel:Play()
                editingOption = false
            end

        else
            -- Left/right cycle through choices (modulo)
            local choiceCount = #opt.choices

            if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
                sounds.Skip:Play()
                opt.value = (opt.value + 1) % choiceCount

            elseif INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
                sounds.Skip:Play()
                opt.value = (opt.value - 1 + choiceCount) % choiceCount

            elseif INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
                sounds.Decide:Play()
                editingOption = false

            elseif INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
                sounds.Cancel:Play()
                editingOption = false
            end
        end
    end
end

-- ============================================================
-- Lifecycle
-- ============================================================
function activate(pl)
    debugLog(tostring(pl))
    save   = GetSaveFile(pl)
    player = pl
    loadOptions()

    selectedIndex = 0
    editingOption = false

    sounds.Skip      = SHARED:GetSharedSound("Skip")
    sounds.Cancel    = SHARED:GetSharedSound("Cancel")
    sounds.Decide    = SHARED:GetSharedSound("Decide")
    sounds.SongDecide = SHARED:GetSharedSound("SongDecide")

    startCounter("screen_transition", 1080, 0, -0.5/1080, "none", updateTransitionVisuals, function()
        reactive = true
    end)
end

function deactivate()
    for k, counter in pairs(ctx) do
        counter = COUNTER:EmptyCounter()
    end
    -- Note: saveOptions() is called explicitly before DEACTIVATE() only on OK
end

function onStart()
    textSmall = TEXT:Create(18)
    text      = TEXT:Create(28)
    textLarge = TEXT:Create(40)

    tx["bg"]     = TEXTURE:CreateTexture("Textures/Background.png")
    tx["bgtile"] = TEXTURE:CreateTexture("Textures/BgTile.png")
end

function onDestroy()
    if text      ~= nil then text:Dispose()      end
    if textSmall ~= nil then textSmall:Dispose() end
    if textLarge ~= nil then textLarge:Dispose() end

    for _, t in pairs(tx) do
        t:Dispose()
    end
end