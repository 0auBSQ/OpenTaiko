---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- opening.lua — Curtain intro + title menu for intro_nokon.
--
-- Sequence:
--   closed (1.5s wait) → opening (curtain splits open, ~0.9s)
--   → post_open (1s wait) → menu (BGM + Start/Back selection)
--
-- update() returns "start", "back", or nil.

local NavInput = require("NavInput")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Open, Anim = Layout.opening, Animation.opening

local M = {}

local tx     = {}   -- shared with Script.lua (allocated/freed there)
local sounds = {}   -- shared with Script.lua (BGM, shared SFX)

local CURTAIN_W = Layout.curtain.half_w  -- each curtain half's width

-- Sub-state
local subState = "closed"

-- Curtain draw position (0 = fully closed, CURTAIN_W = fully open)
local curtainX = 0

-- Menu
local selectedItem = 0    -- 0 = Start, 1 = Back
local flashOpacity = 255

-- Logo / Light animation
local logoAngle   = 0
local lightAngle  = 0
local jiggling    = false

-- Counters
local waitOpenCounter    = nil
local curtainCounter     = nil
local postOpenCounter    = nil
local flashCounter       = nil
local jiggleDelayCounter = nil
local jiggleCounter      = nil
local lightRotCounter    = nil

-- ── Helpers ───────────────────────────────────────────────────────────────────

local function empty() return COUNTER:EmptyCounter() end

local function startJiggleDelay()
    local delay = Anim.jiggle_delay_s + math.random() * Anim.jiggle_delay_spread_s
    jiggleDelayCounter = COUNTER:CreateCounterDuration(0, 1, delay)
    jiggleDelayCounter:Start()
end

local function startFlash()
    -- 0→255 bounce: each half-cycle flash_half_s
    flashCounter = COUNTER:CreateCounter(0, 255, Anim.flash_half_s / 255)
    flashCounter:SetBounce(true)
    flashCounter:Listen(function(v) flashOpacity = math.floor(v) end)
    flashCounter:Start()
end

local function startLightRot()
    -- One full rotation every light_turn_s, looping forever
    lightRotCounter = COUNTER:CreateCounter(0, 360, Anim.light_turn_s / 360)
    lightRotCounter:SetLoop(true)
    lightRotCounter:Listen(function(v) lightAngle = v end)
    lightRotCounter:Start()
end

-- ── Init / lifecycle ──────────────────────────────────────────────────────────

function M.init(t, snd)
    tx     = t
    sounds = snd
end


-- Full reset: show closed curtain, wait 1.5 s, then open.
function M.reset()
    subState     = "closed"
    curtainX     = 0
    selectedItem = 0
    flashOpacity = 255
    logoAngle    = 0
    lightAngle   = 0
    jiggling     = false

    waitOpenCounter    = COUNTER:CreateCounterDuration(0, 1, Anim.wait_s)
    curtainCounter     = empty()
    postOpenCounter    = empty()
    flashCounter       = empty()
    jiggleDelayCounter = empty()
    jiggleCounter      = empty()
    lightRotCounter    = empty()

    waitOpenCounter:Start()
end

-- Jump straight to the menu (used when returning from player_select).
function M.resetToMenu()
    subState     = "menu"
    curtainX     = CURTAIN_W
    selectedItem = 0
    logoAngle    = 0
    jiggling     = false

    waitOpenCounter = empty()
    curtainCounter  = empty()
    postOpenCounter = empty()
    jiggleCounter   = empty()

    startFlash()
    startJiggleDelay()
    startLightRot()

    if not sounds.BGM.IsPlaying then
        sounds.BGM:SetLoop(true)
        sounds.BGM:Play()
    end
end

-- ── Draw ──────────────────────────────────────────────────────────────────────

function M.draw()
    if subState == "closed" then
        if tx["Curtain"] then tx["Curtain"]:Draw(0, 0) end
        return
    end

    -- Background layer
    if tx["Background"] then tx["Background"]:Draw(0, 0) end

    -- Light.png (behind Logo, perpetually rotating, centered on Logo center)
    if tx["Light"] and tx["Logo"] then
        local cx = Open.logo.x + tx["Logo"].Width  / 2
        local cy = Open.logo.y + tx["Logo"].Height / 2
        tx["Light"]:SetRotation(lightAngle)
        tx["Light"]:DrawAtAnchor(cx, cy, "center")
        tx["Light"]:SetRotation(0)
    end

    -- Logo (with occasional jiggle rotation)
    if tx["Logo"] then
        tx["Logo"]:SetRotation(logoAngle)
        tx["Logo"]:Draw(Open.logo.x, Open.logo.y)
        tx["Logo"]:SetRotation(0)
    end

    -- Nokon2
    if tx["Nokon2"] then tx["Nokon2"]:Draw(Open.nokon.x, Open.nokon.y) end

    -- Menu buttons (only once curtain is fully open)
    if subState == "menu" then
        local startOp = selectedItem == 0 and (flashOpacity / 255) or 1.0
        local backOp  = selectedItem == 1 and (flashOpacity / 255) or 1.0

        if tx["Start"] then
            tx["Start"]:SetOpacity(startOp)
            tx["Start"]:DrawAtAnchor(Open.start.x, Open.start.y, "center")
            tx["Start"]:SetOpacity(1)
        end
        if tx["Back"] then
            tx["Back"]:SetOpacity(backOp)
            tx["Back"]:Draw(Open.back.x, Open.back.y)
            tx["Back"]:SetOpacity(1)
        end
    end

    -- Curtain_Open drawn on top as two sliding halves
    local co = tx["CurtainOpen"]
    if co ~= nil and co.Width > 0 then
        co:DrawRect(-curtainX,            0,         0, 0, CURTAIN_W, 1080)
        co:DrawRect(CURTAIN_W + curtainX, 0, CURTAIN_W, 0, CURTAIN_W, 1080)
    end
end

-- ── Update ────────────────────────────────────────────────────────────────────
-- Returns "start", "back", or nil.

function M.update()
    waitOpenCounter:Tick()
    curtainCounter:Tick()
    postOpenCounter:Tick()
    flashCounter:Tick()
    jiggleDelayCounter:Tick()
    jiggleCounter:Tick()
    lightRotCounter:Tick()

    if subState == "closed" then
        if waitOpenCounter.Value >= 1 then
            waitOpenCounter = empty()
            subState = "opening"
            sounds["CurtainOpen"]:Play()
            curtainCounter = COUNTER:CreateCounterDuration(0, CURTAIN_W, Animation.curtain_s)
            curtainCounter:Listen(function(v) curtainX = math.floor(v) end)
            curtainCounter:Start()
            startLightRot()
        end

    elseif subState == "opening" then
        if curtainCounter.Value >= CURTAIN_W then
            curtainCounter = empty()
            curtainX = CURTAIN_W
            subState = "post_open"
            postOpenCounter = COUNTER:CreateCounterDuration(0, 1, Anim.post_open_s)
            postOpenCounter:Start()
        end

    elseif subState == "post_open" then
        if postOpenCounter.Value >= 1 then
            postOpenCounter = empty()
            subState = "menu"
            sounds.BGM:SetLoop(true)
            sounds.BGM:Play()
            startFlash()
            startJiggleDelay()
        end

    elseif subState == "menu" then
        -- Logo jiggle scheduling
        if not jiggling then
            if jiggleDelayCounter.Value >= 1 then
                jiggling = true
                jiggleDelayCounter = empty()
                jiggleCounter = COUNTER:CreateCounter(0, 360, Anim.jiggle_s / 360)
                jiggleCounter:Listen(function(v)
                    logoAngle = math.sin(v * math.pi / 180) * Anim.jiggle_deg
                end)
                jiggleCounter:Start()
            end
        else
            if jiggleCounter.Value >= 360 then
                jiggling  = false
                logoAngle = 0
                jiggleCounter = empty()
                startJiggleDelay()
            end
        end

        -- Navigation
        if NavInput.right() or NavInput.left() then
            selectedItem = 1 - selectedItem
            SHARED:GetSharedSound("Skip"):Play()
        end

        -- Confirm
        if NavInput.decide() then
            if selectedItem == 0 then
                SHARED:GetSharedSound("Decide"):Play()
                return "start"
            else
                sounds.BGM:Stop()
                SHARED:GetSharedSound("Cancel"):Play()
                return "back"
            end
        end
    end

    return nil
end

return M
