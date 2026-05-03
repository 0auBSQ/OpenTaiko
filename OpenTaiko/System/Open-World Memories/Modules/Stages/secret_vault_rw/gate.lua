---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- gate.lua — Gate phase for secret_vault_rw.
--
-- Gate.init(tx, snd, txts)
-- Gate.reset()          — call from activate(); reads .vault_opened to decide start mode
-- Gate.draw()
-- Gate.update()         — returns "vault" when gate fully open, "back" to exit, nil otherwise

local M = {}

local tx   = {}
local snd  = {}
local txts = {}

-- ── Constants ─────────────────────────────────────────────────────────────────

local SCREEN_W = 1920
local SCREEN_H = 1080
local HALF_W   = 960   -- each gate half is 960 px wide

local KEY_DEFS = {
    { name = "Key of Greed", hint = "Occasionally available in the General Store for a juicy price",
      color = "ff00ffff", baseX = 300,  baseY = 300, half = "left"  },
    { name = "Key of Pride", hint = "Beat Nokon at his own show",
      color = "ff00ff00", baseX = 540,  baseY = 540, half = "left"  },
    { name = "Key of Wrath", hint = "This key is free for now... Until a further update...",
      color = "ffff0000", baseX = 1620, baseY = 300, half = "right" },
    { name = "Key of Envy",  hint = "Reach the 6th door in the Pagoda of the Unknown",
      color = "ffffff00", baseX = 1380, baseY = 540, half = "right" },
}

-- ── State ─────────────────────────────────────────────────────────────────────

local phase       = "idle"   -- "idle" | "animating" | "opening" | "done"
local selectedKey = 1
local gateOffset  = 0        -- 0 = fully closed, HALF_W = fully open

local keyRotations = {0, 0, 0, 0}   -- degrees; 90 = turned

-- Sequential animation queue
local animQueue  = {}
local animCtr    = nil
local animCtrMax = 0
local animActKey = 0   -- key index currently being rotated (0 = none)

-- ── Helpers ───────────────────────────────────────────────────────────────────

local function col(hex) return COLOR:CreateColorFromHex(hex) end

local function keyOwned(i)
    local sf = GetSaveFile(0)
    if sf == nil then return false end
    if i == 1 then return sf:GetGlobalTrigger(".vault_key_obtained_greed") == true end
    if i == 2 then return sf:GetGlobalTrigger(".vault_key_obtained_pride") == true end
    if i == 3 then return true end
    if i == 4 then return (sf:GetGlobalCounter("pagoda_highest_level") or 0) >= 10 end
    return false
end

local function allKeysOwned()
    for i = 1, 4 do if not keyOwned(i) then return false end end
    return true
end

local function keyScreenX(i)
    if KEY_DEFS[i].half == "left" then
        return KEY_DEFS[i].baseX - gateOffset
    else
        return KEY_DEFS[i].baseX + gateOffset
    end
end

-- ── Animation queue ───────────────────────────────────────────────────────────

local function processNext()
    while #animQueue > 0 do
        local act = table.remove(animQueue, 1)
        if act.type == "wait" then
            animCtr    = COUNTER:CreateCounterDuration(0, 1, act.dur)
            animCtrMax = 1
            animCtr:Start()
            return
        elseif act.type == "rot" then
            if snd["Unlock"] then snd["Unlock"]:Play() end
            animActKey = act.key
            animCtr    = COUNTER:CreateCounterDuration(0, 90, 0.2)
            animCtrMax = 90
            animCtr:Listen(function(v) keyRotations[act.key] = v end)
            animCtr:Start()
            return
        elseif act.type == "gate_open" then
            phase = "opening"
            if snd[act.sndKey] then snd[act.sndKey]:Play() end
            animCtr    = COUNTER:CreateCounterDuration(0, HALF_W, act.dur)
            animCtrMax = HALF_W
            animCtr:Listen(function(v) gateOffset = math.floor(v) end)
            animCtr:Start()
            return
        elseif act.type == "bgm" then
            if snd["BGM"] then snd["BGM"]:SetLoop(true); snd["BGM"]:Play() end
            -- no counter needed, fall through to next action
        elseif act.type == "done" then
            phase = "done"
            return
        end
    end
end

local function tickAnim()
    if animCtr == nil then return end
    animCtr:Tick()
    if animCtr.Value >= animCtrMax then
        animCtr = nil
        if animActKey > 0 then
            keyRotations[animActKey] = 90
            animActKey = 0
        end
        processNext()
    end
end

local function startFullUnlock()
    animQueue = {
        {type="wait",      dur=1.0},
        {type="rot",       key=1},
        {type="wait",      dur=0.3},
        {type="rot",       key=2},
        {type="wait",      dur=0.3},
        {type="rot",       key=3},
        {type="wait",      dur=0.3},
        {type="rot",       key=4},
        {type="wait",      dur=0.3},
        {type="wait",      dur=1.0},
        {type="gate_open", dur=5.0, sndKey="Gate"},
        {type="wait",      dur=1.0},
        {type="bgm"},
        {type="done"},
    }
    phase = "animating"
    processNext()
end

local function startFastOpen()
    keyRotations = {90, 90, 90, 90}
    animQueue = {
        {type="wait",      dur=1.0},
        {type="gate_open", dur=1.0, sndKey="GateShort"},
        {type="wait",      dur=1.0},
        {type="bgm"},
        {type="done"},
    }
    phase = "animating"
    processNext()
end

-- ── Init / reset ──────────────────────────────────────────────────────────────

function M.init(t, s, txtsRef)
    tx   = t
    snd  = s
    txts = txtsRef
end

function M.reset()
    keyRotations = {0, 0, 0, 0}
    animQueue    = {}
    animCtr      = nil
    animCtrMax   = 0
    animActKey   = 0
    gateOffset   = 0
    selectedKey  = 1
    phase        = "idle"

    local sf = GetSaveFile(0)
    if sf and sf:GetGlobalTrigger(".vault_opened") == true then
        -- Show full unlock for now
        -- startFullUnlock()
        startFastOpen()
    end
end

-- ── Draw ─────────────────────────────────────────────────────────────────────

function M.draw()
    local bg = tx["Gate/Bg"]

    -- Gate halves (slide apart as gateOffset increases)
    if bg then
        bg:DrawRect(-gateOffset,          0, 0,      0, HALF_W, SCREEN_H)
        bg:DrawRect(HALF_W + gateOffset,  0, HALF_W, 0, HALF_W, SCREEN_H)
    end

    -- Keyholes and keys follow their respective gate halves
    for i = 1, 4 do
        local sx = keyScreenX(i)
        local sy = KEY_DEFS[i].baseY

        if tx["Gate/Keyhole"] then
            tx["Gate/Keyhole"]:DrawAtAnchor(sx, sy, "center")
        end

        if keyOwned(i) and tx["Gate/Key"] then
            tx["Gate/Key"]:SetColor(col(KEY_DEFS[i].color))
            tx["Gate/Key"]:SetRotation(keyRotations[i])
            tx["Gate/Key"]:DrawAtAnchor(sx, sy, "center")
            tx["Gate/Key"]:SetColor(col("ffffffff"))
            tx["Gate/Key"]:SetRotation(0)
        end
    end

    -- UI overlay: only during idle phase
    if phase == "idle" then
        local sx = keyScreenX(selectedKey)
        local sy = KEY_DEFS[selectedKey].baseY

        if tx["Gate/Hover"] then
            tx["Gate/Hover"]:DrawAtAnchor(sx, sy, "center")
        end

        if tx["Gate/Overlay"] then
            tx["Gate/Overlay"]:DrawAtAnchor(960, 0, "top")
        end

        local kd = KEY_DEFS[selectedKey]
        if txts.title then
            local kt = txts.title:GetText(kd.name, false, 1400, col(kd.color))
            kt:DrawAtAnchor(960, 280, "center")
        end
        if txts.label then
            local ht = txts.label:GetText(kd.hint, false, 1400)
            ht:DrawAtAnchor(960, 60, "center")
        end
    end
end

-- ── Update ────────────────────────────────────────────────────────────────────

function M.update()
    tickAnim()

    if phase == "done" then return "vault" end
    if phase ~= "idle"  then return nil    end

    local decide = INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return")
    local cancel = INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape")
    local right  = INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow")
    local left   = INPUT:Pressed("LeftChange")  or INPUT:KeyboardPressed("LeftArrow")

    if right then
        selectedKey = (selectedKey % 4) + 1
        if snd["Skip"] then snd["Skip"]:Play() end
    elseif left then
        selectedKey = ((selectedKey - 2 + 4) % 4) + 1
        if snd["Skip"] then snd["Skip"]:Play() end
    elseif cancel then
        if snd["Cancel"] then snd["Cancel"]:Play() end
        return "back"
    elseif decide then
        if not allKeysOwned() then
            if snd["NoKey"] then snd["NoKey"]:Play() end
            return "back"
        end
        local sf = GetSaveFile(0)
        if sf then sf:SetGlobalTrigger(".vault_opened", true) end
        if snd["Decide"] then snd["Decide"]:Play() end
        startFullUnlock()
    end

    return nil
end

return M
