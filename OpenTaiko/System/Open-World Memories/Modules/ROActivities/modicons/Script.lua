---@diagnostic disable: undefined-global  -- CONFIG/TEXTURE injected by CLuaScript at runtime

-- modicons ROActivity: 8 mod icons for a player (Textures/Mods/)
-- modicons:Draw(x, y, player, layout): "menu" single row (default), "game" 2x4 grid

-- slot offsets and value font: Config/layout.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")

local _isActive = false

-- loaded in onStart(), disposed in onDestroy()
local tx = {}
local valueFont       -- scroll / song speed value font
local colWhite

-- ==== Layouts ====
-- slots: 1 HS, 2 Stealth, 3 Random, 4 Fun, 5 Just, 6 Timing, 7 SongSpeed, 8 Auto / special
-- "menu": single row
local OFFSET_X_MENU = Layout.menu.offset_x
local OFFSET_Y_MENU = Layout.menu.offset_y

-- "game": slots 1-4 top row, 5-8 bottom row
local OFFSET_X_GAME = Layout.game.offset_x
local OFFSET_Y_GAME = Layout.game.offset_y

-- ==== Speed values ====
-- scroll: 9 = x1, multiplier (v + 1) / 10; song: 20 = x1, multiplier v / 20
-- icons ~37px: keep the value font small
local SCROLL_SPEED_NORMAL = CONFIG.SCROLLSPEED.Normal
local SONG_SPEED_NORMAL = CONFIG.SONGSPEED.Normal
local VALUE_FONT_SIZE = Layout.value.font_size

-- CONFIG:GetSpecialMod value -> Auto slot icon (Auto follows CONFIG:GetAutoStatus)
local SPECIAL_ICONS = { [2] = "Flawless", [3] = "Timed", [4] = "TimedHard" }

-- ==== Helpers ====

local function drawIcon(icon, x, y, alpha)
    if icon == nil then return end
    if alpha ~= nil then icon:SetOpacity(alpha / 255) end
    icon:Draw(x, y)
    if alpha ~= nil then icon:SetOpacity(1) end
end

-- x1: none; else the HS icon (value drawn as text)
local function getHsIconForSpeed(speed)
    if speed == SCROLL_SPEED_NORMAL then return tx["None"] end
    return tx["HS"]
end

-- nil at x1; scroll 1 decimal, song 2 decimals
-- explicit if: `cond and nil or x` breaks on a nil true-value
local function scrollValueText(speed)
    if speed == SCROLL_SPEED_NORMAL then return nil end
    return string.format("%.1f", CONFIG.SCROLLSPEED:ToActual(speed))
end
local function songValueText(song)
    if song == SONG_SPEED_NORMAL then return nil end
    return string.format("%.2f", CONFIG.SONGSPEED:ToActual(song))
end

-- glyph pad (~25px) exceeds the icon: centred half a line above its bottom
-- maxWidth = icon width + pad; longer values squish
local function drawValue(str, icon, x, y, alpha)
    if str == nil or icon == nil or valueFont == nil then return end
    valueFont:Draw(str, x + icon.Width / 2, y + icon.Height - VALUE_FONT_SIZE / 2, colWhite, nil,
        (alpha or 255) / 255, 1, icon.Width + 50, "center")
end

-- ==== Lifecycle ====

function activate()   _isActive = true  end
function deactivate() _isActive = false end

-- x, y: slot 1 top-left
-- player: 0-based
-- layout: "menu" (default) | "game"
-- alpha: 0-255 (default 255)
function draw(x, y, player, layout, alpha)
    if not _isActive then return end

    local ox = (layout == "game") and OFFSET_X_GAME or OFFSET_X_MENU
    local oy = (layout == "game") and OFFSET_Y_GAME or OFFSET_Y_MENU

    -- slot 1: scroll speed
    local scroll = CONFIG:GetScrollSpeed(player)
    local hsIcon = getHsIconForSpeed(scroll)
    drawIcon(hsIcon, x + ox[1], y + oy[1], alpha)
    drawValue(scrollValueText(scroll), hsIcon, x + ox[1], y + oy[1], alpha)

    -- slot 2: Doron / Stealth / Hidden / Flashlight
    local stealth = CONFIG:GetStealthMod(player)
    local STEALTH = CONFIG.STEALTH
    for _, v in ipairs{ { STEALTH.Doron, "Doron" }, { STEALTH.Stealth, "Stealth" },
        { STEALTH.Hidden or -1, "Hidden" }, { STEALTH.Flashlight or -1, "Flashlight" }, { stealth, "None" } } do
        local mod, key = table.unpack(v)
        if stealth == mod then drawIcon(tx[key], x + ox[2], y + oy[2], alpha); break end
    end

    -- slot 3: Random / Mirror / Super / Hyper
    local random = CONFIG:GetRandomMod(player)
    local RANDOM = CONFIG.RANDOM
    for _, v in ipairs{ { RANDOM.Random, "Random" }, { RANDOM.Mirror, "Mirror" },
        { RANDOM.SuperRandom, "Super" }, { RANDOM.MirrorRandom, "Hyper" }, { random, "None" },
        } do
        local mod, key = table.unpack(v)
        if random == mod then drawIcon(tx[key], x + ox[3], y + oy[3], alpha); break end
    end

    -- slot 4: Fun Mod
    local fun = CONFIG:GetFunMod(player)
    if fun > 0 then drawIcon(tx["Fun_" .. fun], x + ox[4], y + oy[4], alpha)
    else            drawIcon(tx["None"],        x + ox[4], y + oy[4], alpha)
    end

    -- slot 5: Just / Safe
    local just = CONFIG:GetJusticeMod(player)
    local JUST = CONFIG.JUSTICE
    for _, v in ipairs{ { JUST.Just, "Just" }, { JUST.Safe, "Safe" }, { just, "None" } } do
        local mod, key = table.unpack(v)
        if just == mod then drawIcon(tx[key], x + ox[5], y + oy[5], alpha); break end
    end

    -- slot 6: timing zone (2 = Normal: no icon)
    local timing = CONFIG:GetTimingZone(player)
    local TIMING = CONFIG.TIMINGZONE
    if timing - 2 ~= TIMING.Normal then
         drawIcon(tx["Timing_" .. timing], x + ox[6], y + oy[6], alpha)
    else drawIcon(tx["None"],              x + ox[6], y + oy[6], alpha)
    end

    -- slot 7: song speed (20 = x1: no icon)
    local songSpeed = CONFIG.SongSpeed
    local ssIcon = (songSpeed > SONG_SPEED_NORMAL) and tx["SongSpeed_1"]
        or (songSpeed < SONG_SPEED_NORMAL) and tx["SongSpeed_0"]
        or tx["None"]
    drawIcon(ssIcon, x + ox[7], y + oy[7], alpha)
    drawValue(songValueText(songSpeed), ssIcon, x + ox[7], y + oy[7], alpha)

    -- slot 8: special mod, else Auto (auto-play or replay); the AI battle's AI shows none
    local special = SPECIAL_ICONS[CONFIG:GetSpecialMod(player)]
    if CONFIG.IsAIBattleMode and player == 1 then special = nil end
    if special ~= nil then                   drawIcon(tx[special], x + ox[8], y + oy[8], alpha)
    elseif CONFIG:GetAutoStatus(player) then drawIcon(tx["Auto"],  x + ox[8], y + oy[8], alpha)
    else                                     drawIcon(tx["None"],  x + ox[8], y + oy[8], alpha)
    end
end

-- mod icons for a recorded play (replay) instead of a live CONFIG
-- mods: ModFlags bitfield (CSongReplay.EModFlag); scroll 9 = x1; song 20 = x1; timing 2 = Normal
-- slot 8: the special mod only (Auto is never recorded)
function drawFlags(x, y, mods, scroll, song, timing, layout, alpha)
    if not _isActive then return end
    mods = mods or 0
    local ox = (layout == "game") and OFFSET_X_GAME or OFFSET_X_MENU
    local oy = (layout == "game") and OFFSET_Y_GAME or OFFSET_Y_MENU

    local function has(flags) return mods & flags == flags end
    local MOD = REPLAY.MODFLAG

    -- slot 1: scroll speed
    local sc = scroll or SCROLL_SPEED_NORMAL
    local hsIcon = getHsIconForSpeed(sc)
    drawIcon(hsIcon, x + ox[1], y + oy[1], alpha)
    drawValue(scrollValueText(sc), hsIcon, x + ox[1], y + oy[1], alpha)

    -- slot 2: Doron / Stealth / Hidden / Flashlight
    for _, v in ipairs{ { MOD.Invisible, "Doron" }, { MOD.PerfectMemory, "Stealth" },
        { MOD.Hidden or -1, "Hidden" }, { MOD.Flashlight or -1, "Flashlight" }, { 0, "None" } } do
        local bit, name = table.unpack(v)
        if has(bit) then drawIcon(tx[name], x + ox[2], y + oy[2], alpha); break end
    end

    -- slot 3: Random / Mirror / Super / Hyper (Mirror+Random)
    for _, v in ipairs{
        { MOD.Mirror | MOD.Random, "Hyper" }, { MOD.SuperRandom, "Super" },
        { MOD.Random, "Random" }, { MOD.Mirror, "Mirror" }, { 0, "None" },
        } do
        local bit, name = table.unpack(v)
        if has(bit) then drawIcon(tx[name], x + ox[3], y + oy[3], alpha); break end
    end

    -- slot 4: Fun Mod
    for _, v in ipairs{
        { MOD.Avalanche, "Fun_1" }, { MOD.Minesweeper, "Fun_2" },
        { MOD.DynamicBeat, "Fun_3" }, { 0, "None" },
        } do
        local bit, name = table.unpack(v)
        if has(bit) then drawIcon(tx[name], x + ox[4], y + oy[4], alpha); break end
    end

    -- slot 5: Just / Safe
    for _, v in ipairs{ { MOD.Just, "Just" }, { MOD.Safe, "Safe" }, { 0, "None" } } do
        local bit, name = table.unpack(v)
        if has(bit) then drawIcon(tx[name], x + ox[5], y + oy[5], alpha); break end
    end

    -- slot 6: timing zone (2 = Normal: no icon)
    local TIMING = CONFIG.TIMINGZONE
    if timing ~= nil and timing - 2 ~= TIMING.Normal then
         drawIcon(tx["Timing_" .. timing], x + ox[6], y + oy[6], alpha)
    else drawIcon(tx["None"],              x + ox[6], y + oy[6], alpha)
    end

    -- slot 7: song speed (20 = x1: no icon)
    local sp = song or SONG_SPEED_NORMAL
    local ssIcon = (sp > SONG_SPEED_NORMAL) and tx["SongSpeed_1"]
        or (sp < SONG_SPEED_NORMAL) and tx["SongSpeed_0"]
        or tx["None"]
    drawIcon(ssIcon, x + ox[7], y + oy[7], alpha)
    drawValue(songValueText(sp), ssIcon, x + ox[7], y + oy[7], alpha)

    -- slot 8: Flawless / Timed / Timed (Hard)
    for _, v in ipairs{ { MOD.Flawless or -1, "Flawless" }, { MOD.TimedHard or -1, "TimedHard" }, { MOD.Timed or -1, "Timed" } } do
        local bit, name = table.unpack(v)
        if has(bit) then drawIcon(tx[name], x + ox[8], y + oy[8], alpha); break end
    end
end

function update(...) end

function onStart()
    tx["None"]    = TEXTURE:CreateTexture("Textures/Mods/None.png")
    tx["Auto"]    = TEXTURE:CreateTexture("Textures/Mods/Auto.png")
    tx["Flawless"]  = TEXTURE:CreateTexture("Textures/Mods/Flawless.png")
    tx["Timed"]     = TEXTURE:CreateTexture("Textures/Mods/Timed.png")
    tx["TimedHard"] = TEXTURE:CreateTexture("Textures/Mods/TimedHard.png")
    tx["Doron"]   = TEXTURE:CreateTexture("Textures/Mods/Doron.png")
    tx["Stealth"] = TEXTURE:CreateTexture("Textures/Mods/Stealth.png")
    tx["Hidden"]  = TEXTURE:CreateTexture("Textures/Mods/Hidden.png")
    tx["Flashlight"] = TEXTURE:CreateTexture("Textures/Mods/Flashlight.png")
    tx["Just"]    = TEXTURE:CreateTexture("Textures/Mods/Just.png")
    tx["Safe"]    = TEXTURE:CreateTexture("Textures/Mods/Safe.png")
    tx["Mirror"]  = TEXTURE:CreateTexture("Textures/Mods/Mirror.png")
    tx["Random"]  = TEXTURE:CreateTexture("Textures/Mods/Random.png")
    tx["Super"]   = TEXTURE:CreateTexture("Textures/Mods/Super.png")
    tx["Hyper"]   = TEXTURE:CreateTexture("Textures/Mods/Hyper.png")
    tx["SongSpeed_0"] = TEXTURE:CreateTexture("Textures/Mods/SongSpeed/0.png")
    tx["SongSpeed_1"] = TEXTURE:CreateTexture("Textures/Mods/SongSpeed/1.png")

    -- one HS icon for any non-x1 scroll speed
    tx["HS"] = TEXTURE:CreateTexture("Textures/Mods/HS.png")

    valueFont = TEXT:CreateGlyphCached(VALUE_FONT_SIZE)
    colWhite  = Config.color(Layout.value.color)

    -- Fun: 1=Avalanche, 2=Minesweeper, 3=DynamicBeat
    for i = 1, 3 do
        tx["Fun_" .. i] = TEXTURE:CreateTexture("Textures/Mods/Fun/" .. i .. ".png")
    end

    -- Timing: 0 = Loose .. 4 = Rigorous (2 = Normal loaded, not shown)
    for i = 0, 4 do
        tx["Timing_" .. i] = TEXTURE:CreateTexture("Textures/Mods/Timing/" .. i .. ".png")
    end
end

function onDestroy()
    for _, t in pairs(tx) do
        if t ~= nil then t:Dispose() end
    end
    tx = {}
    if valueFont ~= nil then valueFont:Dispose(); valueFont = nil end
end
