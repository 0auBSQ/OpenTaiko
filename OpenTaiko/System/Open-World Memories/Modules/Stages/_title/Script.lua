---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- _title/Script.lua  —  Main menu for OpenTaiko

local NavInput = require("NavInput")
local EM       = require("EventMode")
local PS       = require("player_select")

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

-- ── Locale ────────────────────────────────────────────────────────────────────
-- the skin's Locales/<code>.json through THEME; the English text stays in the code as the fallback

local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

-- ── Vault unlock check ────────────────────────────────────────────────────────

local function isVaultUnlocked()
    return GetSaveFile(0):GetGlobalTrigger(".vault_opened") == true
end

-- ── Menu definitions ──────────────────────────────────────────────────────────

-- Event Mode: the title offers the three modes an event runs on and nothing else
local EVENT_STAGES = { regular_song_select = true, ai_battle_song_select = true, dan_select = true }

local function buildMenus()
    local m = {
        {
            title = tr("TITLE_PERFORMANCE", "Performance Mode"),
            desc  = tr("TITLE_PERFORMANCE_DESC", "Play Taiko charts with your preferred settings!\nPlayable between 1 and 5 players."),
            c     = col(255, 140, 0),
            via   = "stage", stage = "regular_song_select",
            playerPrompt = true,
        },
        {
            title = tr("TITLE_DOJO", "The Fox Dojo"),
            desc  = tr("TITLE_DOJO_DESC", "Challenge various dan exams tailored by the Fox Band!\nSingle player only."),
            c     = col(0, 50, 150),
            via   = "stage", stage = "dan_select", trans = "dan_doors",
        },
        {
            title = tr("TITLE_TOWERS", "Survival Mode"),
            desc  = tr("TITLE_TOWERS_DESC", "Climb the towers through survival challenges and try getting to the top!\nSingle player only."),
            c     = col(100, 210, 50),
            via   = "stage", stage = "tower_select",
        },
        {
            title = tr("TITLE_AI_BATTLE", "AI Battle Mode"),
            desc  = tr("TITLE_AI_BATTLE_DESC", "Fight AItritus on your favorite charts and try to get the W!\nSingle player only."),
            c     = col(0, 200, 220),
            via   = "stage", stage = "ai_battle_song_select",
        },
        {
            title = tr("TITLE_TRAINING", "Training Mode"),
            desc  = tr("TITLE_TRAINING_DESC", "Practice your favorite charts to get the hang of them!\nSingle player only."),
            c     = col(210, 185, 130),
            via   = "stage", stage = "training_song_select",
        },
        {
            title = tr("TITLE_INTRO_NOKON", "Intro Nokon"),
            desc  = tr("TITLE_INTRO_NOKON_DESC", "It's show time! Show your musical knowledge through Nokon's best show!\nPlayable between 1 and 5 players."),
            c     = col(140, 80, 30),
            via   = "stage", stage = "intro_nokon", trans = "nokon_curtain",
        },
        {
            title = tr("TITLE_MYROOM", "My Room"),
            desc  = tr("TITLE_MYROOM_DESC", "Decorate your personal room, place furniture, and visit other players' rooms!"),
            c     = col(30, 150, 60),
            via   = "stage", stage = "myroom",
        },
        {
            title = tr("TITLE_STORE", "OpenTaiko's General Store"),
            desc  = tr("TITLE_STORE_DESC", "Spend your OpenTaiko coins for very nice goods! *wink*"),
            c     = col(0, 160, 170),
            via   = "stage", stage = "coin_shop",
        },
    }

    -- Vault entry: conditional on unlock state
    if isVaultUnlocked() then
        m[#m + 1] = {
            title = tr("TITLE_VAULT", "Secret Vault"),
            desc  = tr("TITLE_VAULT_DESC", "A place full of mysteries where keys seems to have a particular value..."),
            c     = col(80, 80, 90),
            via   = "stage", stage = "secret_vault_rw",
        }
    else
        m[#m + 1] = {
            title  = "???",
            desc   = tr("TITLE_VAULT_LOCKED_DESC", "Can you hear me...?"),
            c      = col(80, 80, 90),
            via    = "stage", stage = "secret_vault_rw",
            static = true,
        }
    end

    m[#m + 1] = {
        title = tr("TITLE_SETTINGS", "Settings"),
        desc  = tr("TITLE_SETTINGS_DESC", "Adjust your settings to fit with your play experience!"),
        c     = col(170, 170, 175),
        via   = "config",
    }
    m[#m + 1] = {
        title = tr("TITLE_EXIT", "Exit"),
        desc  = tr("TITLE_EXIT_DESC", "See you next time!"),
        c     = col(100, 100, 105),
        via   = "exit",
    }
    m[#m + 1] = {
        title = tr("TITLE_ONLINE_LOBBY", "Online Lobby"),
        desc  = tr("TITLE_ONLINE_LOBBY_DESC", "(Beta, Share your room code only with people you trust)"),
        c     = col(72, 36, 112),
        via   = "stage", stage = "onlinelobby",
    }

    if EM.on() then
        local kept = {}
        for _, e in ipairs(m) do if e.via == "stage" and EVENT_STAGES[e.stage] then kept[#kept + 1] = e end end
        return kept
    end

    return m
end

-- ── State ─────────────────────────────────────────────────────────────────────

local menus      = {}
local curIdx     = 1
local textCache  = {}   -- [i] = { title = LuaTexture, desc = LuaTexture }

-- Player count prompt (player_select.lua)
local inPrompt   = false

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

local BGTILE_W, BGTILE_H = 192, 108   -- BgTile.png; Width reads 0 until the texture is uploaded

local function drawBgTile(opacity)
    if bgtile == nil or bgtile.Width <= 0 then return end
    opacity = opacity or 1.0
    bgtile:SetOpacity(opacity)
    for x = 0, 1919, BGTILE_W do
        for y = 0, 1079, BGTILE_H do
            bgtile:Draw(x, y)
        end
    end
    bgtile:SetOpacity(1.0)
end

-- alpha fades the whole menu out behind the player prompt
local function drawBoxes(alpha)
    if boxTex == nil or #menus == 0 or alpha <= 0 then return end
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
        boxTex:SetOpacity(alpha)
        boxTex:DrawAtAnchor(960, boxY, "center")
        boxTex:SetOpacity(1)
        boxTex:SetColor(mkCol(white))
        if isStatic then boxTex:SetUseNoiseEffect(false) end

        -- Hover overlay for selected entry
        if i == curIdx and hoverTex ~= nil then
            if isStatic then hoverTex:SetUseNoiseEffect(true) end
            hoverTex:SetOpacity(alpha)
            hoverTex:DrawAtAnchor(960, boxY, "center")
            hoverTex:SetOpacity(1)
            if isStatic then hoverTex:SetUseNoiseEffect(false) end
        end

        -- Title text (top = boxTop + 10)
        local titleTex = getTitleTex(i)
        if titleTex ~= nil then
            if isStatic then titleTex:SetUseNoiseEffect(true) end
            titleTex:SetOpacity(alpha)
            titleTex:DrawAtAnchor(960, boxY - bh / 2 + 10, "top")
            titleTex:SetOpacity(1)
            if isStatic then titleTex:SetUseNoiseEffect(false) end
        end

        -- Description text (top = boxTop + 100)
        local descTex = getDescTex(i)
        if descTex ~= nil then
            if isStatic then descTex:SetUseNoiseEffect(true) end
            descTex:SetOpacity(alpha)
            descTex:DrawAtAnchor(960, boxY - bh / 2 + 100, "top")
            descTex:SetOpacity(1)
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
    if     m.via == "stage"       then return Exit("stage",  m.stage, m.trans)
    elseif m.via == "heya"        then return Exit("legacy", "heya", m.trans)
    elseif m.via == "config"      then return Exit("legacy", "config", m.trans)
    elseif m.via == "exit"        then return Exit("legacy", "exit", m.trans)
    elseif m.via == "onlinelounge"then return Exit("legacy", "onlinelounge", m.trans)
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

    PS.load(tr)
end

function activate()
    if not EM.on() then EM.resetSession() end   -- no event running: the plays counter waits at zero
    menus     = buildMenus()
    textCache = {}
    curIdx    = math.max(1, math.min(curIdx, #menus))  -- keep last position, clamp to new size
    scrollPos = curIdx   -- start at target so there's no animation on first open
    inPrompt  = false
    PS.reset()
    holdDir   = 0
    sounds.Decide = SHARED:GetSharedSound("Decide")
    sounds.Cancel = SHARED:GetSharedSound("Cancel")
    sounds.Move   = SHARED:GetSharedSound("Move")
    if sounds.BGM ~= nil then sounds.BGM:Play() end
end

function deactivate()
    if sounds.BGM ~= nil then sounds.BGM:Stop() end
    PS.clearSounds()
end

function afterSongEnum()
end

-- the boxes carry localized text: rebuild them in the new language (the cache holds string textures)
function reloadLanguage()
    menus     = buildMenus()
    textCache = {}
    curIdx    = math.max(1, math.min(curIdx, #menus))
    PS.reloadLanguage()
end

-- ── Draw ──────────────────────────────────────────────────────────────────────

function draw()
    if background ~= nil then background:Draw(0, 0) end
    drawBgTile(0.65)
    drawBoxes(1 - PS.dim() / 0.6)

    -- Version watermark (top-left, no anchor)
    if textVersion ~= nil then
        local vTex = textVersion:GetText("OpenTaiko 0.6.1 Pre-release 1 (Demo)", false, 1920, mkCol(white), mkCol(black))
        if vTex ~= nil then vTex:Draw(0, 0) end
    end

    -- Player-count prompt overlay (also while its cancel outro plays)
    if PS.visible() then
        drawBgTile(PS.dim())
        PS.draw()
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
    if PS.update(ts, dt) then return doExit() end   -- the decide confirm has played out
    if inPrompt then
        if PS.deciding() then return nil end

        local decide = NavInput.decide() and PS.canDecide()
        if NavInput.cancel() or (decide and PS.count() == 0) then
            sounds.Cancel:Play()
            PS.cancel()
            inPrompt = false
            holdDir  = 0
            return nil
        end

        if decide then
            sounds.Decide:Play()
            CONFIG.PlayerCount = PS.count()
            PS.decide()
            holdDir  = 0
            return nil
        end

        local rp = NavInput.down() or NavInput.right()
        local lp = NavInput.up() or NavInput.left()
        if rp then PS.move(1, true);   sounds.Move:Play() end
        if lp then PS.move(-1, true);  sounds.Move:Play() end

        -- Hold repeat for prompt
        local rHeld = NavInput.downPressing() or NavInput.rightPressing()
        local lHeld = NavInput.upPressing() or NavInput.leftPressing()
        local pDir  = rHeld and 1 or (lHeld and -1 or 0)
        if pDir ~= holdDir then holdDir = pDir; holdStart = ts; holdLast = ts
        elseif pDir ~= 0 and ts - holdStart >= HOLD_DELAY and ts - holdLast >= HOLD_REPEAT then
            PS.move(pDir, false)
            sounds.Move:Play()
            holdLast = ts
        end

        return nil
    end

    -- ── Cancel → boot ─────────────────────────────────────────────────────────
    if NavInput.cancel() then
        sounds.Cancel:Play()
        return Exit("stage", "_boot")
    end

    -- ── Navigate ──────────────────────────────────────────────────────────────
    local rp = NavInput.down() or NavInput.right()
    local lp = NavInput.up() or NavInput.left()
    if rp then moveMenu(1)  end
    if lp then moveMenu(-1) end

    -- Hold-scroll auto-repeat
    local rHeld = NavInput.downPressing() or NavInput.rightPressing()
    local lHeld = NavInput.upPressing() or NavInput.leftPressing()
    local dir   = rHeld and 1 or (lHeld and -1 or 0)
    if dir ~= holdDir then
        holdDir = dir; holdStart = ts; holdLast = ts
    elseif dir ~= 0 and ts - holdStart >= HOLD_DELAY and ts - holdLast >= HOLD_REPEAT then
        moveMenu(dir)
        holdLast = ts
    end

    -- ── Decide ────────────────────────────────────────────────────────────────
    if NavInput.decide() then
        sounds.Decide:Play()
        local m = menus[curIdx]
        if m.playerPrompt then
            inPrompt  = true
            PS.open(CONFIG.PlayerCount or 1)
            holdDir   = 0
            return nil
        end
        return doExit()
    end
end

function onDestroy()
end
