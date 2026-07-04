---@diagnostic disable: undefined-global  -- CONFIG/TEXTURE injected by CLuaScript at runtime

-- modicons ROActivity
-- Draws the 8 active mod icons for a given player.
-- Images live in Textures/Mods/ so custom skins can override them.
--
-- Usage from any script that has ROACTIVITY:
--   local modicons = ROACTIVITY:GetROActivity("modicons")
--   modicons:Draw(x, y, player)            -- uses "menu" layout (single row)
--   modicons:Draw(x, y, player, "game")    -- uses "game" layout (2×4 grid)

local _isActive = false

-- Loaded in onStart(), disposed in onDestroy()
local tx = {}

-- ─── Layouts ─────────────────────────────────────────────────────────────────
-- Each table has 8 entries (one per slot, Lua-1-based):
--   Slot 1: HS   2: Stealth   3: Random   4: Fun
--   Slot 5: Just  6: Timing   7: SongSpeed  8: Auto
--
-- "menu" : single horizontal row, 45 px between slots
local OFFSET_X_MENU = {  0, 45, 90, 135, 180, 225, 270, 315 }
local OFFSET_Y_MENU = {  0,  0,  0,   0,   0,   0,   0,   0 }

-- "game" : 4+4 grid (top row = slots 1-4, bottom row = slots 5-8)
local OFFSET_X_GAME = {  0, 45, 90, 135,   0,  45,  90, 135 }
local OFFSET_Y_GAME = {  0,  0,  0,   0,  45,  45,  45,  45 }

-- ─── HS thresholds ───────────────────────────────────────────────────────────
-- Fast speeds (>= 10): ascending thresholds.
-- Show HS/i.png for the highest i where speed >= HS_THRESHOLDS[i+1].
local HS_THRESHOLDS = {10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 24, 29, 34, 39}

-- Slow speeds (<= 8): descending thresholds.
-- Show HS/slow/i.png for the highest i where speed <= LOW_HS_THRESHOLDS[i+1].
-- By default every integer from 8 down to 0, giving 9 slow icons (slow/0 … slow/8).
local LOW_HS_THRESHOLDS = {8, 7, 6, 5, 4, 3, 2, 1, 0}

-- ─── Helpers ─────────────────────────────────────────────────────────────────

local function drawIcon(icon, x, y, alpha)
    if icon == nil then return end
    if alpha ~= nil then icon:SetOpacity(alpha / 255) end
    icon:Draw(x, y)
    if alpha ~= nil then icon:SetOpacity(1) end
end

local function getHsIconForSpeed(speed)
    if speed >= HS_THRESHOLDS[1] then
        -- Fast: find the last threshold still satisfied
        local idx = -1
        for i, t in ipairs(HS_THRESHOLDS) do
            if speed >= t then idx = i - 1 else break end
        end
        if idx >= 0 then return tx["HS_" .. idx] end
    elseif speed <= LOW_HS_THRESHOLDS[1] then
        -- Slow: find the last (smallest) threshold still satisfied
        local idx = -1
        for i, t in ipairs(LOW_HS_THRESHOLDS) do
            if speed <= t then idx = i - 1 else break end
        end
        if idx >= 0 then return tx["HS_slow_" .. idx] end
    end
    -- speed == 9 (default) or no threshold matched → no icon
    return tx["None"]
end

local function getHsIcon(player)
    return getHsIconForSpeed(CONFIG:GetScrollSpeed(player))
end

-- ─── Lifecycle ───────────────────────────────────────────────────────────────

function activate()   _isActive = true  end
function deactivate() _isActive = false end

-- x, y   : top-left anchor for slot 1
-- player : 0-based player index
-- layout : "menu" (default, single row) | "game" (2×4 grid)
-- alpha  : opacity 0-255 (default 255)
function draw(x, y, player, layout, alpha)
    if not _isActive then return end

    local ox = (layout == "game") and OFFSET_X_GAME or OFFSET_X_MENU
    local oy = (layout == "game") and OFFSET_Y_GAME or OFFSET_Y_MENU

    -- Slot 1: Scroll speed (HS / slow HS)
    drawIcon(getHsIcon(player), x + ox[1], y + oy[1], alpha)

    -- Slot 2: Doron / Stealth
    local stealth = CONFIG:GetStealthMod(player)
    if     stealth == 1 then drawIcon(tx["Doron"],   x + ox[2], y + oy[2], alpha)
    elseif stealth == 2 then drawIcon(tx["Stealth"], x + ox[2], y + oy[2], alpha)
    else                     drawIcon(tx["None"],    x + ox[2], y + oy[2], alpha)
    end

    -- Slot 3: Random / Mirror / Super / Hyper
    local random = CONFIG:GetRandomMod(player)
    if     random == 1 then drawIcon(tx["Mirror"], x + ox[3], y + oy[3], alpha)
    elseif random == 2 then drawIcon(tx["Random"], x + ox[3], y + oy[3], alpha)
    elseif random == 3 then drawIcon(tx["Super"],  x + ox[3], y + oy[3], alpha)
    elseif random == 4 then drawIcon(tx["Hyper"],  x + ox[3], y + oy[3], alpha)
    else                    drawIcon(tx["None"],   x + ox[3], y + oy[3], alpha)
    end

    -- Slot 4: Fun Mod
    local fun = CONFIG:GetFunMod(player)
    if fun > 0 then drawIcon(tx["Fun_" .. fun], x + ox[4], y + oy[4], alpha)
    else            drawIcon(tx["None"],        x + ox[4], y + oy[4], alpha)
    end

    -- Slot 5: Just / Safe
    local just = CONFIG:GetJusticeMod(player)
    if     just == 1 then drawIcon(tx["Just"], x + ox[5], y + oy[5], alpha)
    elseif just == 2 then drawIcon(tx["Safe"], x + ox[5], y + oy[5], alpha)
    else                  drawIcon(tx["None"], x + ox[5], y + oy[5], alpha)
    end

    -- Slot 6: Timing zone  (2 = Normal → no icon)
    local timing = CONFIG:GetTimingZone(player)
    if timing ~= 2 then drawIcon(tx["Timing_" .. timing], x + ox[6], y + oy[6], alpha)
    else                drawIcon(tx["None"],               x + ox[6], y + oy[6], alpha)
    end

    -- Slot 7: Song speed  (20 = 1.0× → no icon)
    local songSpeed = CONFIG.SongSpeed
    if     songSpeed > 20 then drawIcon(tx["SongSpeed_1"], x + ox[7], y + oy[7], alpha)
    elseif songSpeed < 20 then drawIcon(tx["SongSpeed_0"], x + ox[7], y + oy[7], alpha)
    else                       drawIcon(tx["None"],        x + ox[7], y + oy[7], alpha)
    end

    -- Slot 8: Auto
    if CONFIG:GetAutoStatus(player) then drawIcon(tx["Auto"], x + ox[8], y + oy[8], alpha)
    else                                 drawIcon(tx["None"], x + ox[8], y + oy[8], alpha)
    end
end

-- Draw the mod icons for an arbitrary recorded play (e.g. a replay) instead of a live player's CONFIG.
-- mods   : the replay's ModFlags bitfield (see CSongReplay.EModFlag)
-- scroll : scroll-speed value (9 = x1), song : song-speed value (20 = x1), timing : timing zone (2 = Normal)
-- The Auto slot is intentionally omitted here (replay cards don't show Auto).
function drawFlags(x, y, mods, scroll, song, timing, layout, alpha)
    if not _isActive then return end
    mods = mods or 0
    local ox = (layout == "game") and OFFSET_X_GAME or OFFSET_X_MENU
    local oy = (layout == "game") and OFFSET_Y_GAME or OFFSET_Y_MENU

    -- bit values mirror CSongReplay.EModFlag
    local function has(bit) return math.floor(mods / bit) % 2 == 1 end
    local Mirror, Random, Super, Invisible, PerfectMem = 1, 2, 4, 8, 16
    local Avalanche, Minesweeper, Just, Safe, DynamicBeat = 32, 64, 128, 256, 512

    -- Slot 1: Scroll speed
    drawIcon(getHsIconForSpeed(scroll or 9), x + ox[1], y + oy[1], alpha)

    -- Slot 2: Doron / Stealth
    if     has(Invisible)  then drawIcon(tx["Doron"],   x + ox[2], y + oy[2], alpha)
    elseif has(PerfectMem) then drawIcon(tx["Stealth"], x + ox[2], y + oy[2], alpha)
    else                        drawIcon(tx["None"],    x + ox[2], y + oy[2], alpha)
    end

    -- Slot 3: Random / Mirror / Super / Hyper (Mirror+Random)
    if     has(Mirror) and has(Random) then drawIcon(tx["Hyper"],  x + ox[3], y + oy[3], alpha)
    elseif has(Super)                  then drawIcon(tx["Super"],  x + ox[3], y + oy[3], alpha)
    elseif has(Random)                 then drawIcon(tx["Random"], x + ox[3], y + oy[3], alpha)
    elseif has(Mirror)                 then drawIcon(tx["Mirror"], x + ox[3], y + oy[3], alpha)
    else                                    drawIcon(tx["None"],   x + ox[3], y + oy[3], alpha)
    end

    -- Slot 4: Fun Mod
    if     has(Avalanche)   then drawIcon(tx["Fun_1"], x + ox[4], y + oy[4], alpha)
    elseif has(Minesweeper) then drawIcon(tx["Fun_2"], x + ox[4], y + oy[4], alpha)
    elseif has(DynamicBeat) then drawIcon(tx["Fun_3"], x + ox[4], y + oy[4], alpha)
    else                         drawIcon(tx["None"],  x + ox[4], y + oy[4], alpha)
    end

    -- Slot 5: Just / Safe
    if     has(Just) then drawIcon(tx["Just"], x + ox[5], y + oy[5], alpha)
    elseif has(Safe) then drawIcon(tx["Safe"], x + ox[5], y + oy[5], alpha)
    else                  drawIcon(tx["None"], x + ox[5], y + oy[5], alpha)
    end

    -- Slot 6: Timing zone (2 = Normal → no icon)
    if timing ~= nil and timing ~= 2 then drawIcon(tx["Timing_" .. timing], x + ox[6], y + oy[6], alpha)
    else                                   drawIcon(tx["None"],              x + ox[6], y + oy[6], alpha)
    end

    -- Slot 7: Song speed (20 = 1.0× → no icon)
    if     (song or 20) > 20 then drawIcon(tx["SongSpeed_1"], x + ox[7], y + oy[7], alpha)
    elseif (song or 20) < 20 then drawIcon(tx["SongSpeed_0"], x + ox[7], y + oy[7], alpha)
    else                          drawIcon(tx["None"],        x + ox[7], y + oy[7], alpha)
    end
    -- (Slot 8 / Auto intentionally not drawn for replay cards)
end

function update(...) end

function onStart()
    tx["None"]    = TEXTURE:CreateTexture("Textures/Mods/None.png")
    tx["Auto"]    = TEXTURE:CreateTexture("Textures/Mods/Auto.png")
    tx["Doron"]   = TEXTURE:CreateTexture("Textures/Mods/Doron.png")
    tx["Stealth"] = TEXTURE:CreateTexture("Textures/Mods/Stealth.png")
    tx["Just"]    = TEXTURE:CreateTexture("Textures/Mods/Just.png")
    tx["Safe"]    = TEXTURE:CreateTexture("Textures/Mods/Safe.png")
    tx["Mirror"]  = TEXTURE:CreateTexture("Textures/Mods/Mirror.png")
    tx["Random"]  = TEXTURE:CreateTexture("Textures/Mods/Random.png")
    tx["Super"]   = TEXTURE:CreateTexture("Textures/Mods/Super.png")
    tx["Hyper"]   = TEXTURE:CreateTexture("Textures/Mods/Hyper.png")
    tx["SongSpeed_0"] = TEXTURE:CreateTexture("Textures/Mods/SongSpeed/0.png")
    tx["SongSpeed_1"] = TEXTURE:CreateTexture("Textures/Mods/SongSpeed/1.png")

    -- Fast HS icons: HS/0.png … HS/13.png
    for i = 0, #HS_THRESHOLDS - 1 do
        tx["HS_" .. i] = TEXTURE:CreateTexture("Textures/Mods/HS/" .. i .. ".png")
    end

    -- Slow HS icons: HS/slow/0.png … HS/slow/N.png  (one per LOW_HS_THRESHOLDS entry)
    for i = 0, #LOW_HS_THRESHOLDS - 1 do
        tx["HS_slow_" .. i] = TEXTURE:CreateTexture("Textures/Mods/HS/slow/" .. i .. ".png")
    end

    -- Fun: 1=Avalanche, 2=Minesweeper, 3=DynamicBeat
    for i = 1, 3 do
        tx["Fun_" .. i] = TEXTURE:CreateTexture("Textures/Mods/Fun/" .. i .. ".png")
    end

    -- Timing: 0=Loose … 4=Rigorous  (2=Normal is hidden, but loaded for completeness)
    for i = 0, 4 do
        tx["Timing_" .. i] = TEXTURE:CreateTexture("Textures/Mods/Timing/" .. i .. ".png")
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
end
