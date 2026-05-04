---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local, redundant-parameter, inject-field
-- vault.lua — Vault interior phase for secret_vault_rw.
--
-- Vault.init(tx, snd, txts)
-- Vault.reset()   — called when the gate fully opens
-- Vault.draw()
-- Vault.update()  — returns "back" to exit, nil otherwise

local M = {}

local tx   = {}
local snd  = {}
local txts = {}

-- ── Constants ──────────────────────────────────────────────────────────────────

local SCREEN_W    = 1920
local SCREEN_H    = 1080
local BG_MAX      = 0.8

local CHEST_X     = {460, 860, 1260}
local CHEST_Y     = 620

local KEY_COUNTERS = {
    ".vault_key_count_simple",
    ".vault_key_count_gold",
    ".vault_key_count_optk",
}

local NAMEPLATE_POOLS = {
    {361, 362, 363, 364, 365, 366, 367, 368},   -- Simple
    {369, 370, 371, 372, 373, 374, 375},          -- Gold
    {376, 377, 378, 379, 380},                    -- OpTk
}

-- Rarity string → modal integer (matches HRarity.RarityToModalInt in C#)
-- Poor=0, Common=0, Uncommon=1, Rare=2, Epic=3, Legendary=4, Mythical=4
local RARITY_MAP = {Poor=0, Common=0, Uncommon=1, Rare=2, Epic=3, Legendary=4, Mythical=4}

local function songLevelToRarity(lv)
    if lv <= 1 then return 0 end   -- Common
    if lv == 2 then return 1 end   -- Uncommon
    if lv == 3 then return 2 end   -- Rare
    if lv == 4 then return 3 end   -- Epic
    return 4                       -- Legendary (5+)
end

-- ── State ──────────────────────────────────────────────────────────────────────

-- States: idle | no_key_shake | confirm | confirm_out | jiggle | post_wait |
--         reward_modal | reward_show | reward_show_out
local state    = "idle"
local selected = 1   -- 1‥3 = chest, 4 = return

local enterCtr         = nil   -- 0→1 in 0.5 s: overlay/return/hover fade-in
local returnBounceCtr  = nil   -- bounce 0→1 while return is selected
local animCtr          = nil   -- general-purpose animation counter
local animCtrEnd       = 0     -- expected end value of animCtr
local animCtrUp        = true  -- true if counter goes upward (>=); false if downward (<=)

local bgAlpha          = 0.0
local shakeOffsetX     = 0.0
local jiggleRot        = 0.0

local confirmVisible   = false
local pendingReward    = nil   -- set when Decide is pressed in confirm state

local songPools        = {{}, {}, {}}   -- indexed by chest (1‥3)

-- ── Helpers ───────────────────────────────────────────────────────────────────

local function sf()
    return GetSaveFile(0)
end

local function getKeyCount(idx)
    return sf():GetGlobalCounter(KEY_COUNTERS[idx]) or 0
end

local function setKeyCount(idx, n)
    sf():SetGlobalCounter(KEY_COUNTERS[idx], n)
end

local function iterateCsharpList(list)
    local arr = {}
    if list == nil then return arr end
    local e = list:GetEnumerator()
    while e:MoveNext() do table.insert(arr, e.Current) end
    return arr
end

local function getMaxChartLevel(node)
    local maxLv = 0
    for diff = 0, 4 do
        local ch = node:GetChart(diff)
        if ch ~= nil and ch.Level > 0 then
            maxLv = math.max(maxLv, ch.Level)
        end
    end
    return maxLv
end

local function buildSongPools()
    songPools = {{}, {}, {}}

    local lsls = GenerateSongListSettings()
    lsls.ModuloPagination     = false
    lsls.AppendMainRandomBox  = false
    lsls.AppendSubRandomBoxes = false
    lsls.SubBackBoxFrequency  = 0
    lsls.RootGenreFolder      = "Secret Vault"
    lsls.IgnoreUnlockables    = true

    local vaultList = RequestSongList(lsls)
    if vaultList == nil then return end

    local allNodes = iterateCsharpList(vaultList:SearchSongsByPredicate(function(n) return true end))
    for _, node in ipairs(allNodes) do
        local lv = getMaxChartLevel(node)
        if lv <= 1 then
            table.insert(songPools[1], node)
        elseif lv == 2 then
            table.insert(songPools[2], node)
        else
            table.insert(songPools[3], node)
        end
    end
end

local function getUnobtainedSongs(poolIdx)
    local save = sf()
    local out = {}
    for _, node in ipairs(songPools[poolIdx]) do
        if not save:GetGlobalTrigger(".vault_song_unlocked_" .. node.UniqueId) then
            table.insert(out, node)
        end
    end
    return out
end

local function getUnobtainedNameplates(poolIdx)
    local save = sf()
    local out = {}
    for _, id in ipairs(NAMEPLATE_POOLS[poolIdx]) do
        if not save:IsNameplateUnlocked(id) then
            table.insert(out, id)
        end
    end
    return out
end

local function buildChestPool(idx)
    local pool = {}

    -- snap (nothing)
    local snapW = (idx == 3) and 50 or 20
    table.insert(pool, {w = snapW, type = "snap"})

    -- coins
    local coinW    = (idx == 2) and 35 or 30
    local coinMin  = ({20, 100, 200})[idx]
    local coinMax  = ({50, 200, 500})[idx]
    table.insert(pool, {w = coinW, type = "coins", min = coinMin, max = coinMax})

    -- key (chest1 → gold key, chest2 → optk key, chest3 → none)
    if idx == 1 then table.insert(pool, {w = 10, type = "key", target = 2}) end
    if idx == 2 then table.insert(pool, {w =  5, type = "key", target = 3}) end

    -- nameplate
    local uNp = getUnobtainedNameplates(idx)
    if #uNp > 0 then
        table.insert(pool, {w = 10, type = "nameplate", pool = uNp})
    end

    -- song
    local songW = (idx == 3) and 10 or 30
    local uSongs = getUnobtainedSongs(idx)
    if #uSongs > 0 then
        table.insert(pool, {w = songW, type = "song", pool = uSongs})
    end

    return pool
end

local function rollFromPool(pool)
    local total = 0
    for _, e in ipairs(pool) do total = total + e.w end
    if total == 0 then return {type = "snap"} end
    local r = math.random(1, total)
    local cum = 0
    for _, e in ipairs(pool) do
        cum = cum + e.w
        if r <= cum then return e end
    end
    return pool[#pool]
end

local function processRoll(idx)
    local pool   = buildChestPool(idx)
    local entry  = rollFromPool(pool)
    local save   = sf()

    setKeyCount(idx, getKeyCount(idx) - 1)

    if entry.type == "snap" then
        return {type = "snap", openedChest = idx}
    elseif entry.type == "coins" then
        local amount = math.random(entry.min, entry.max)
        save:EarnCoins(amount)
        return {type = "coins", amount = amount, openedChest = idx}
    elseif entry.type == "key" then
        setKeyCount(entry.target, getKeyCount(entry.target) + 1)
        return {type = "key", target = entry.target, openedChest = idx}
    elseif entry.type == "nameplate" then
        local id = entry.pool[math.random(1, #entry.pool)]
        save:UnlockNameplate(id)
        return {type = "nameplate", id = id, info = NAMEPLATESLIST:GetById(id), openedChest = idx}
    elseif entry.type == "song" then
        local node = entry.pool[math.random(1, #entry.pool)]
        save:SetGlobalTrigger(".vault_song_unlocked_" .. node.UniqueId, true)
        return {type = "song", node = node, openedChest = idx}
    end

    return {type = "snap", openedChest = idx}
end

-- ── Animation helpers ──────────────────────────────────────────────────────────

local function startAnim(from, to, seconds)
    animCtrEnd = to
    animCtrUp  = to >= from
    animCtr    = COUNTER:CreateCounterDuration(from, to, seconds)
    animCtr:Start()
end

local function startDarken(dur)
    bgAlpha = 0
    startAnim(0, BG_MAX, dur or 0.3)
    animCtr:Listen(function(v) bgAlpha = v end)
end

local function startUndark(dur)
    startAnim(bgAlpha, 0, dur or 0.3)
    animCtr:Listen(function(v) bgAlpha = v end)
end

local function animDone()
    if animCtr == nil then return true end
    if animCtrUp  then return animCtr.Value >= animCtrEnd end
    return animCtr.Value <= animCtrEnd
end

local function tickAnim()
    if animCtr ~= nil then animCtr:Tick() end
end

-- ── Init / Reset ───────────────────────────────────────────────────────────────

function M.init(t, s, txtsRef)
    tx   = t
    snd  = s
    txts = txtsRef
end

-- Call from Script.lua's activate() to hide vault UI while the gate is playing.
function M.onActivate()
    enterCtr = nil
end

function M.reset()
    state          = "idle"
    selected       = 1
    bgAlpha        = 0
    shakeOffsetX   = 0
    jiggleRot      = 0
    confirmVisible = false
    pendingReward  = nil
    animCtr        = nil

    -- First visit: give 1 simple key
    local save = sf()
    if save:GetGlobalTrigger(".vault_first_key_given") ~= true then
        setKeyCount(1, getKeyCount(1) + 1)
        save:SetGlobalTrigger(".vault_first_key_given", true)
    end

    buildSongPools()

    -- Fade-in counter for overlay/hover/return
    enterCtr = COUNTER:CreateCounterDuration(0, 1, 0.5)
    enterCtr:Start()

    -- Bounce counter for return button: 0→255 in 0.6 s, matches intro_nokon menu flash
    returnBounceCtr = COUNTER:CreateCounter(0, 255, 0.6 / 255)
    returnBounceCtr:SetBounce(true)
    returnBounceCtr:Start()
end

-- ── Draw ───────────────────────────────────────────────────────────────────────

function M.draw()
    if tx["Vault/Bg"] then tx["Vault/Bg"]:Draw(0, 0) end

    local enterA = math.min(enterCtr ~= nil and enterCtr.Value or 0.0, 1.0)

    -- Chests
    for i = 1, 3 do
        local cx = CHEST_X[i]
        local cy = CHEST_Y
        local ox = (state == "no_key_shake" and selected == i) and shakeOffsetX or 0
        local rot = ((state == "jiggle") and selected == i) and jiggleRot or 0

        if tx["Vault/Chest" .. i] then
            local t = tx["Vault/Chest" .. i]
            if rot ~= 0 then t:SetRotation(rot) end
            t:Draw(cx + ox, cy)
            if rot ~= 0 then t:SetRotation(0) end
        end

        -- ChestHover on selected chest (visible once faded in, not during reward screens)
        local showHover = selected == i and enterA > 0
            and (state == "idle" or state == "no_key_shake"
              or state == "confirm" or state == "confirm_out")
        if showHover and tx["Vault/ChestHover"] then
            local h = tx["Vault/ChestHover"]
            if rot ~= 0 then h:SetRotation(rot) end
            h:SetOpacity(enterA)
            h:Draw(cx + ox, cy)
            h:SetOpacity(1)
            if rot ~= 0 then h:SetRotation(0) end
        end
    end

    -- Return button
    if tx["Vault/Return"] and enterA > 0 then
        local alpha = enterA
        if selected == 4 then
            local bv = returnBounceCtr ~= nil and returnBounceCtr.Value or 0
            alpha = enterA * (bv / 255)
        end
        tx["Vault/Return"]:SetOpacity(alpha)
        tx["Vault/Return"]:DrawAtAnchor(0, SCREEN_H, "bottomleft")
        tx["Vault/Return"]:SetOpacity(1)
    end

    -- Overlay (key count display) — icons are embedded in the Overlay texture
    if tx["Vault/Overlay"] and enterA > 0 then
        tx["Vault/Overlay"]:SetOpacity(enterA)
        tx["Vault/Overlay"]:DrawAtAnchor(SCREEN_W, SCREEN_H, "bottomright")
        tx["Vault/Overlay"]:SetOpacity(1)
        if txts.label then
            local KEY_COUNT_POS = {{1100, 950}, {1400, 950}, {1700, 950}}
            -- sin(t*π): 0→1→0 over the shake, peak red at t=0.5
            local shakeV = (state == "no_key_shake" and animCtr ~= nil) and animCtr.Value or 0
            local flashT = (state == "no_key_shake") and math.sin(shakeV * math.pi) or 0
            for i = 1, 3 do
                local ct = txts.label:GetText(string.format("%d", getKeyCount(i)))
                ct:SetOpacity(enterA)
                if selected == i and flashT > 0 then
                    local g = math.floor(255 - 175 * flashT)
                    ct:SetColor(COLOR:CreateColorFromARGB(255, 255, g, g))
                end
                ct:DrawAtAnchor(KEY_COUNT_POS[i][1], KEY_COUNT_POS[i][2], "topleft")
                ct:SetOpacity(1)
                if selected == i and flashT > 0 then
                    ct:SetColor(COLOR:CreateColorFromARGB(255, 255, 255, 255))
                end
            end
        end
    end

    -- BgTile darken (tiled to fill screen)
    if bgAlpha > 0 and tx["Vault/BgTile"] then
        local t = tx["Vault/BgTile"]
        if t.Width > 0 and t.Height > 0 then
            local cols = math.ceil(SCREEN_W / t.Width)
            local rows = math.ceil(SCREEN_H / t.Height)
            t:SetOpacity(bgAlpha)
            for c = 0, cols do
                for r = 0, rows do t:Draw(c * t.Width, r * t.Height) end
            end
            t:SetOpacity(1)
        end
    end

    -- Confirm dialog
    if confirmVisible and txts.title and txts.label then
        local idx = selected
        if idx >= 1 and idx <= 3 then
            local uSongs = getUnobtainedSongs(idx)
            local uNp    = getUnobtainedNameplates(idx)
            local keyTex = tx["Vault/Key" .. idx]
            local keyH   = keyTex and keyTex.Height or 80

            local titleY = 290
            local keyY   = titleY + 60
            local statsY = keyY + keyH + 20

            local t1 = txts.title:GetText("Key Required:")
            t1:DrawAtAnchor(SCREEN_W / 2, titleY, "center")

            if keyTex then keyTex:DrawAtAnchor(SCREEN_W / 2, keyY, "top") end

            local t2 = txts.label:GetText("Available songs: " .. #uSongs .. "/" .. #songPools[idx])
            t2:DrawAtAnchor(SCREEN_W / 2, statsY, "center")
            local t3 = txts.label:GetText("Available nameplates: " .. #uNp .. "/" .. #NAMEPLATE_POOLS[idx])
            t3:DrawAtAnchor(SCREEN_W / 2, statsY + 40, "center")
        end
    end

    -- Reward: key obtained
    if (state == "reward_show" or state == "reward_show_out")
        and pendingReward and pendingReward.type == "key"
        and txts.title then
        local tgt    = pendingReward.target
        local keyTex = tgt and tx["Vault/Key" .. tgt]
        local keyH   = keyTex and keyTex.Height or 80
        local titleY = 420
        local keyY   = titleY + 60
        local t = txts.title:GetText("Key got!")
        t:DrawAtAnchor(SCREEN_W / 2, titleY, "center")
        if keyTex then keyTex:DrawAtAnchor(SCREEN_W / 2, keyY, "top") end
    end

    -- Reward: snap
    if (state == "reward_show" or state == "reward_show_out")
        and pendingReward and pendingReward.type == "snap"
        and txts.title then
        local t1 = txts.title:GetText("Oops!")
        t1:DrawAtAnchor(SCREEN_W / 2, 420, "center")
        local t2 = txts.title:GetText("The key snapped! That\39s a S-key-l issue!")
        t2:DrawAtAnchor(SCREEN_W / 2, 490, "center")
    end

    -- Modal draw
    if state == "reward_modal" then
        local modal = ROACTIVITY:GetROActivity("modal")
        if modal then modal:Draw() end
    end
end

-- ── Update ─────────────────────────────────────────────────────────────────────

function M.update()
    -- Tick persistent counters
    if enterCtr        then enterCtr:Tick() end
    if returnBounceCtr then returnBounceCtr:Tick() end

    if state == "idle" then
        local right  = INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow")
        local left   = INPUT:Pressed("LeftChange")  or INPUT:KeyboardPressed("LeftArrow")
        local decide = INPUT:Pressed("Decide")       or INPUT:KeyboardPressed("Return")
        local cancel = INPUT:Pressed("Cancel")       or INPUT:KeyboardPressed("Escape")

        if right then
            selected = (selected % 4) + 1
            if snd["Skip"] then snd["Skip"]:Play() end
        elseif left then
            selected = ((selected - 2 + 4) % 4) + 1
            if snd["Skip"] then snd["Skip"]:Play() end
        elseif cancel then
            if snd["Cancel"] then snd["Cancel"]:Play() end
            return "back"
        elseif decide then
            if selected == 4 then
                if snd["Decide"] then snd["Decide"]:Play() end
                return "back"
            else
                if getKeyCount(selected) < 1 then
                    if snd["NoKey"] then snd["NoKey"]:Play() end
                    state = "no_key_shake"
                    startAnim(0, 1, 0.2)
                else
                    state = "confirm"
                    confirmVisible = true
                    startDarken(0.3)
                end
            end
        end

    elseif state == "no_key_shake" then
        tickAnim()
        shakeOffsetX = math.sin(animCtr ~= nil and animCtr.Value * math.pi * 6 or 0) * 15
        if animDone() then
            shakeOffsetX = 0
            animCtr = nil
            state = "idle"
        end

    elseif state == "confirm" then
        tickAnim()  -- darken animation
        local decide = INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return")
        local cancel = INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape")

        if cancel then
            if snd["Cancel"] then snd["Cancel"]:Play() end
            confirmVisible = false
            pendingReward  = nil
            state = "confirm_out"
            startUndark(0.3)
        elseif decide then
            -- Roll immediately (anti-save-scum)
            pendingReward  = processRoll(selected)
            confirmVisible = false
            state = "confirm_out"
            startUndark(0.3)
        end

    elseif state == "confirm_out" then
        tickAnim()
        if animDone() then
            animCtr = nil
            if pendingReward ~= nil then
                state = "jiggle"
                if snd["Unlock"] then snd["Unlock"]:Play() end
                startAnim(0, 1, 0.2)
            else
                state = "idle"
            end
        end

    elseif state == "jiggle" then
        tickAnim()
        jiggleRot = math.sin(animCtr ~= nil and animCtr.Value * math.pi * 4 or 0) * 10
        if animDone() then
            jiggleRot = 0
            animCtr   = nil
            if pendingReward and pendingReward.type == "snap" then
                if snd["KeySnap"] then snd["KeySnap"]:Play() end
            end
            state = "post_wait"
            startAnim(0, 1, 0.3)
        end

    elseif state == "post_wait" then
        tickAnim()
        if animDone() then
            animCtr = nil
            local rtype = pendingReward and pendingReward.type or "snap"

            if rtype == "coins" or rtype == "nameplate" or rtype == "song" then
                state = "reward_modal"
                local modal = ROACTIVITY:GetROActivity("modal")
                if modal then
                    if rtype == "coins" then
                        modal:Activate(0, 1, 0, pendingReward.amount, sf().Coins)
                    elseif rtype == "nameplate" then
                        local npRarity = RARITY_MAP[pendingReward.info and pendingReward.info.Rarity] or 1
                        modal:Activate(0, npRarity, 3, pendingReward.info, nil)
                    elseif rtype == "song" then
                        local songRarity = songLevelToRarity(getMaxChartLevel(pendingReward.node))
                        modal:Activate(0, songRarity, 4, pendingReward.node, nil)
                    end
                else
                    pendingReward = nil
                    state = "idle"
                end
            else
                -- key or snap: darken and show overlay screen
                state = "reward_show"
                startDarken(0.3)
            end
        end

    elseif state == "reward_modal" then
        local modal = ROACTIVITY:GetROActivity("modal")
        if modal then
            modal:Update()
            if not modal.IsActive then
                pendingReward = nil
                state = "idle"
            end
        else
            pendingReward = nil
            state = "idle"
        end

    elseif state == "reward_show" then
        tickAnim()  -- darken
        if animDone() then
            animCtr = nil
            -- Wait for Decide
            local decide = INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return")
            if decide then
                state = "reward_show_out"
                startUndark(0.3)
            end
        end

    elseif state == "reward_show_out" then
        tickAnim()
        if animDone() then
            animCtr       = nil
            pendingReward = nil
            state         = "idle"
        end
    end

    return nil
end

return M
