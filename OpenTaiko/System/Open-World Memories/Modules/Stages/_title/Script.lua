---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- _title/Script.lua  —  Main menu for OpenTaiko

-- ── Resources ─────────────────────────────────────────────────────────────────

local boxTex    = nil
local hoverTex  = nil
local bgtile    = nil
local background = nil
local sounds    = {}

local textBig     = nil   -- menu title inside box
local textSmall   = nil   -- menu description inside box
local textVersion = nil   -- version watermark

-- ── Color helpers ─────────────────────────────────────────────────────────────

local function col(r, g, b)     return { r = r, g = g, b = b } end
local function mkCol(c, a)      a = a or 255; return COLOR:CreateColorFromRGBA(c.r, c.g, c.b, a) end
local function darken(c, f)     f = f or 0.35
    return col(math.floor(c.r * f), math.floor(c.g * f), math.floor(c.b * f)) end
local white = col(255, 255, 255)
local black = col(0, 0, 0)

-- ── Vault unlock check ────────────────────────────────────────────────────────

local function isVaultUnlocked()
    return GetSaveFile(0):GetGlobalTrigger(".vault_opened") == true
end

-- ── Menu definitions ──────────────────────────────────────────────────────────

local function buildMenus()
    local dbg = col(150, 150, 155)

    local m = {
        {
            title = "Performance Mode",
            desc  = "Play Taiko charts with your preferred settings!\nPlayable between 1 and 5 players.",
            c     = col(255, 140, 0),
            via   = "stage", stage = "regular_song_select",
            playerPrompt = true,
        },
        {
            title = "The Fox Dojo",
            desc  = "Challenge various dan exams tailored by the Fox Band!\nSingle player only.",
            c     = col(0, 50, 150),
            via   = "stage", stage = "dan_select",
        },
        {
            title = "Taiko Towers",
            desc  = "Climb the towers through survival challenges and try getting to the top!\nSingle player only.",
            c     = col(100, 210, 50),
            via   = "stage", stage = "tower_select",
        },
        {
            title = "AI Battle Mode",
            desc  = "Fight AItritus on your favorite charts and try to get the W!\nSingle player only.",
            c     = col(0, 200, 220),
            via   = "stage", stage = "ai_battle_song_select",
        },
        {
            title = "Training Mode",
            desc  = "Practice your favorite charts to get the hang of them!\nSingle player only.",
            c     = col(210, 185, 130),
            via   = "stage", stage = "training_song_select",
        },
        {
            title = "Intro Nokon",
            desc  = "It's show time! Show your musical knowledge through Nokon's best show!\nPlayable between 1 and 5 players.",
            c     = col(140, 80, 30),
            via   = "stage", stage = "intro_nokon",
        },
        {
            title = "My Room",
            desc  = "Customize your player profile and buy new characters and puchicharas using your coins here!",
            c     = col(30, 150, 60),
            via   = "heya",
        },
        {
            title = "OpenTaiko's General Store",
            desc  = "Spend your OpenTaiko coins for very nice goods! *wink*",
            c     = col(0, 160, 170),
            via   = "stage", stage = "coin_shop",
        },
    }

    -- Vault entry: conditional on unlock state
    if isVaultUnlocked() then
        m[#m + 1] = {
            title = "Secret Vault",
            desc  = "A place full of mysteries where keys seems to have a particular value...",
            c     = col(80, 80, 90),
            via   = "stage", stage = "secret_vault_rw",
        }
    else
        m[#m + 1] = {
            title  = "???",
            desc   = "Can you hear me...?",
            c      = col(80, 80, 90),
            via    = "stage", stage = "secret_vault_rw",
            static = true,
        }
    end

    m[#m + 1] = {
        title = "Settings",
        desc  = "Adjust your settings to fit with your play experience!",
        c     = col(170, 170, 175),
        via   = "config",
    }
    m[#m + 1] = {
        title = "Exit",
        desc  = "See you next time!",
        c     = col(100, 100, 105),
        via   = "exit",
    }

    -- Debug stages
    local function dbgEntry(title, desc, stage)
        return { title = title, desc = desc .. "\nThis Debug stage will not be included in the full 0.6.1 release.", c = dbg, via = "stage", stage = stage }
    end
    m[#m + 1] = dbgEntry("Demo1 (Debug)",              "The first demo using Lua stages.",                          "demo1")
    m[#m + 1] = dbgEntry("Demo2 (Debug)",              "Was used to debug and test song lists.",                    "demo2")
    m[#m + 1] = dbgEntry("Demo3 (Debug)",              "Was used to debug and test databases.",                     "demo3")
    m[#m + 1] = dbgEntry("Theme Settings Test (Debug)","Shows the current theme settings values and specifications.","theme_settings_test")
    m[#m + 1] = dbgEntry("Dan Plate Test (Debug)",     "Shows multiple dan plates with different styling.",         "dan_plate_test")
    m[#m + 1] = dbgEntry("Dan Builder Test (Debug)",   "A minimalist random Dan player.",                          "dan_builder_test")
    m[#m + 1] = dbgEntry("Character Shop (Debug)",     "A minimalist My Room reproduction.",                       "character_shop")

    return m
end

-- ── State ─────────────────────────────────────────────────────────────────────

local menus      = {}
local curIdx     = 1
local textCache  = {}   -- [i] = { title = LuaTexture, desc = LuaTexture }

-- Player count prompt
local inPrompt   = false
local promptCnt  = 1

-- Hold-scroll tracking
local holdDir    = 0
local holdStart  = 0
local holdLast   = 0
local HOLD_DELAY  = 180   -- ms before auto-repeat starts
local HOLD_REPEAT = 70    -- ms between auto-repeat fires

-- Scroll animation
local scrollPos  = 1.0   -- float index, lerps toward curIdx each frame
local lastTs     = 0     -- previous update timestamp for dt calculation

-- Static flicker state for locked vault entry (all times in ms)
local currentTs       = 0   -- last timestamp from update(), used by draw()
local staticUntilMs   = 0   -- timestamp when current static burst ends
local staticNextMs    = 0   -- earliest timestamp for next burst check

-- ── Text cache ────────────────────────────────────────────────────────────────

local function getTitleTex(i)
    if textCache[i] and textCache[i].title then return textCache[i].title end
    if not textCache[i] then textCache[i] = {} end
    local m = menus[i]
    textCache[i].title = textBig:GetText(m.title, false, 750, mkCol(white), mkCol(darken(m.c)))
    return textCache[i].title
end

local function getDescTex(i)
    if textCache[i] and textCache[i].desc then return textCache[i].desc end
    if not textCache[i] then textCache[i] = {} end
    local m = menus[i]
    textCache[i].desc = textSmall:GetText(m.desc, false, 750, mkCol(white), mkCol(darken(m.c)))
    return textCache[i].desc
end

-- ── Draw helpers ──────────────────────────────────────────────────────────────

local function drawBgTile(opacity)
    if bgtile == nil then return end
    opacity = opacity or 1.0
    bgtile:SetOpacity(opacity)
    local tw = math.max(1, bgtile.Width)
    local th = math.max(1, bgtile.Height)
    for x = 0, 1919, tw do
        for y = 0, 1079, th do
            bgtile:Draw(x, y)
        end
    end
    bgtile:SetOpacity(1.0)
end

local function drawBoxes()
    if boxTex == nil or #menus == 0 then return end
    local bh      = boxTex.Height
    local spacing = bh + 20

    for i = 1, #menus do
        local offset = i - scrollPos
        local boxY   = 540 + offset * spacing

        if boxY < -(bh / 2 + 60) or boxY > 1080 + (bh / 2 + 60) then goto skip end

        local m = menus[i]

        local isStatic = m.static and currentTs < staticUntilMs

        -- Box
        if isStatic then boxTex:SetUseNoiseEffect(true) end
        boxTex:SetColor(mkCol(m.c))
        boxTex:DrawAtAnchor(960, boxY, "center")
        boxTex:SetColor(mkCol(white))
        if isStatic then boxTex:SetUseNoiseEffect(false) end

        -- Hover overlay for selected entry
        if i == curIdx and hoverTex ~= nil then
            if isStatic then hoverTex:SetUseNoiseEffect(true) end
            hoverTex:DrawAtAnchor(960, boxY, "center")
            if isStatic then hoverTex:SetUseNoiseEffect(false) end
        end

        -- Title text (top = boxTop + 10)
        local titleTex = getTitleTex(i)
        if titleTex ~= nil then
            if isStatic then titleTex:SetUseNoiseEffect(true) end
            titleTex:DrawAtAnchor(960, boxY - bh / 2 + 10, "top")
            if isStatic then titleTex:SetUseNoiseEffect(false) end
        end

        -- Description text (top = boxTop + 100)
        local descTex = getDescTex(i)
        if descTex ~= nil then
            if isStatic then descTex:SetUseNoiseEffect(true) end
            descTex:DrawAtAnchor(960, boxY - bh / 2 + 100, "top")
            if isStatic then descTex:SetUseNoiseEffect(false) end
        end

        ::skip::
    end
end

-- ── Navigation helpers ────────────────────────────────────────────────────────

local function moveMenu(d)
    if #menus == 0 then return end
    curIdx = ((curIdx - 1 + d) % #menus) + 1
    sounds.Move:Play()
end

local function doExit()
    local m = menus[curIdx]
    if     m.via == "stage"       then return Exit("stage",  m.stage)
    elseif m.via == "heya"        then return Exit("legacy", "heya")
    elseif m.via == "config"      then return Exit("legacy", "config")
    elseif m.via == "exit"        then return Exit("legacy", "exit")
    elseif m.via == "onlinelounge"then return Exit("legacy", "onlinelounge")
    end
end

-- ── Lifecycle ─────────────────────────────────────────────────────────────────

function onStart()
    boxTex     = TEXTURE:CreateTexture("Textures/Box.png")
    hoverTex   = TEXTURE:CreateTexture("Textures/Hover.png")
    bgtile     = TEXTURE:CreateTexture("Textures/BgTile.png")
    background = TEXTURE:CreateTexture("Textures/Background.png")

    textBig     = TEXT:Create(28)
    textSmall   = TEXT:Create(17)
    textVersion = TEXT:Create(15)

    sounds.BGM    = SOUND:CreateBGM("Sounds/BGM.ogg")
    sounds.BGM:SetLoop(true)
    sounds.Decide = SHARED:GetSharedSound("Decide")
    sounds.Cancel = SHARED:GetSharedSound("Cancel")
    sounds.Move   = SHARED:GetSharedSound("Move")
end

function activate()
    menus     = buildMenus()
    textCache = {}
    curIdx    = math.max(1, math.min(curIdx, #menus))  -- keep last position, clamp to new size
    scrollPos = curIdx   -- start at target so there's no animation on first open
    inPrompt  = false
    promptCnt = math.max(1, math.min(5, CONFIG.PlayerCount or 1))
    holdDir   = 0
    if sounds.BGM ~= nil then sounds.BGM:Play() end
end

function deactivate()
    if sounds.BGM ~= nil then sounds.BGM:Stop() end
end

function afterSongEnum()
end

-- ── Draw ──────────────────────────────────────────────────────────────────────

function draw()
    if background ~= nil then background:Draw(0, 0) end
    drawBgTile(0.65)
    drawBoxes()

    -- Version watermark (top-left, no anchor)
    if textVersion ~= nil then
        local vTex = textVersion:GetText("OpenTaiko 0.6.1 Pre-release 1 (Demo)", false, 1920, mkCol(white), mkCol(black))
        if vTex ~= nil then vTex:Draw(0, 0) end
    end

    -- Player-count prompt overlay
    if inPrompt then
        drawBgTile(0.85)

        if textBig ~= nil then
            local pTitle = textBig:GetText("Select number of players", false, 750, mkCol(white), mkCol(black))
            local pCount = textBig:GetText(tostring(promptCnt), false, 200, mkCol(white), mkCol(black))
            local pHint  = textSmall:GetText(
                "◄ / ►  to change   •   Decide to confirm   •   Cancel to go back",
                false, 900, mkCol(white), mkCol(black))
            if pTitle ~= nil then pTitle:DrawAtAnchor(960, 380, "center") end
            if pCount ~= nil then pCount:DrawAtAnchor(960, 490, "center") end
            if pHint  ~= nil then pHint:DrawAtAnchor(960,  600, "center") end
        end
    end
end

-- ── Update ────────────────────────────────────────────────────────────────────

function update(ts)
    local dt  = math.min(ts - lastTs, 100)  -- ms since last frame, capped to avoid jump after pause
    lastTs    = ts
    currentTs = ts

    -- Scroll animation: exponential smoothing, FPS-independent
    local alpha = 1.0 - math.exp(-dt / 45.0)  -- half-life ~31 ms → ~120 ms to reach target
    scrollPos = scrollPos + (curIdx - scrollPos) * alpha
    if math.abs(scrollPos - curIdx) < 0.005 then scrollPos = curIdx end

    -- Update static burst for locked vault (time-based, FPS-independent)
    if ts >= staticNextMs then
        staticNextMs = ts + 100  -- re-check every 100 ms
        if currentTs >= staticUntilMs and math.random(1, 10) <= 2 then
            -- 20% chance every 100 ms → bursts roughly 2×/sec on average
            staticUntilMs = ts + math.random(150, 500)
        end
    end

    -- ── Prompt mode ───────────────────────────────────────────────────────────
    if inPrompt then
        if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
            sounds.Cancel:Play()
            inPrompt = false
            holdDir  = 0
            return nil
        end

        if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
            sounds.Decide:Play()
            CONFIG.PlayerCount = promptCnt
            inPrompt = false
            holdDir  = 0
            return doExit()
        end

        local rp = INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") or INPUT:KeyboardPressed("DownArrow")
        local lp = INPUT:Pressed("LeftChange")  or INPUT:KeyboardPressed("LeftArrow")  or INPUT:KeyboardPressed("UpArrow")
        if rp then promptCnt = promptCnt % 5 + 1;              sounds.Move:Play() end
        if lp then promptCnt = (promptCnt - 2) % 5 + 1;        sounds.Move:Play() end

        -- Hold repeat for prompt
        local rHeld = INPUT:Pressing("RightChange") or INPUT:KeyboardPressing("RightArrow") or INPUT:KeyboardPressing("DownArrow")
        local lHeld = INPUT:Pressing("LeftChange")  or INPUT:KeyboardPressing("LeftArrow")  or INPUT:KeyboardPressing("UpArrow")
        local pDir  = rHeld and 1 or (lHeld and -1 or 0)
        if pDir ~= holdDir then holdDir = pDir; holdStart = ts; holdLast = ts
        elseif pDir ~= 0 and ts - holdStart >= HOLD_DELAY and ts - holdLast >= HOLD_REPEAT then
            promptCnt = (promptCnt - 1 + (pDir == 1 and 1 or 4)) % 5 + 1
            sounds.Move:Play()
            holdLast = ts
        end

        return nil
    end

    -- ── Cancel → boot ─────────────────────────────────────────────────────────
    if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
        sounds.Cancel:Play()
        return Exit("stage", "_boot")
    end

    -- ── Navigate ──────────────────────────────────────────────────────────────
    local rp = INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") or INPUT:KeyboardPressed("DownArrow")
    local lp = INPUT:Pressed("LeftChange")  or INPUT:KeyboardPressed("LeftArrow")  or INPUT:KeyboardPressed("UpArrow")
    if rp then moveMenu(1)  end
    if lp then moveMenu(-1) end

    -- Hold-scroll auto-repeat
    local rHeld = INPUT:Pressing("RightChange") or INPUT:KeyboardPressing("RightArrow") or INPUT:KeyboardPressing("DownArrow")
    local lHeld = INPUT:Pressing("LeftChange")  or INPUT:KeyboardPressing("LeftArrow")  or INPUT:KeyboardPressing("UpArrow")
    local dir   = rHeld and 1 or (lHeld and -1 or 0)
    if dir ~= holdDir then
        holdDir = dir; holdStart = ts; holdLast = ts
    elseif dir ~= 0 and ts - holdStart >= HOLD_DELAY and ts - holdLast >= HOLD_REPEAT then
        moveMenu(dir)
        holdLast = ts
    end

    -- ── Decide ────────────────────────────────────────────────────────────────
    if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
        sounds.Decide:Play()
        local m = menus[curIdx]
        if m.playerPrompt then
            inPrompt  = true
            promptCnt = math.max(1, math.min(5, CONFIG.PlayerCount or 1))
            holdDir   = 0
            return nil
        end
        return doExit()
    end
end

function onDestroy()
end
