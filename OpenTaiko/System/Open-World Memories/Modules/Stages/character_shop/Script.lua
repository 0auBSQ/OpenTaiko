-- character_shop/Script.lua
-- Allows the player to unlock and equip characters, puchichara, and nameplates.

-- ─────────────────────────────────────────────────────────────────────────────
-- State
-- ─────────────────────────────────────────────────────────────────────────────

local save = nil
local modal = nil

-- Current screen: "main" | "character" | "puchichara" | "nameplate" | "hitsounds"
local currentScreen = "main"

-- Main menu: 1=Character, 2=Puchichara, 3=Nameplate, 4=Hitsounds
local mainIdx = 1
local MAIN_OPTIONS = { "Character", "Puchichara", "Nameplate", "Hitsounds" }

-- Sub-menu selection index (0-based)
local subIdx = 0

-- Cached lists built on activate (tables of entries)
local characterList = {}
local puchiList = {}
local nameplateList = {}
local hitsoundList = {}

-- Tracks which character entries have had ANIM_MENU_NORMAL loaded (FolderName → entry).
-- Populated lazily during draw; all disposed in deactivate().
local loadedCharaAnims = {}


-- Text font
local font = nil

-- Sounds
local sounds = {}

-- ─────────────────────────────────────────────────────────────────────────────
-- Helpers
-- ─────────────────────────────────────────────────────────────────────────────

local function modWrap(v, size)
    -- 0-based modulo wrap
    return (v % size + size) % size
end

-- Maps rarity strings to the integer expected by modal:Activate (matches HRarity.RarityToModalInt).
local function rarityToInt(rarity)
    local map = { Poor=0, Common=0, Uncommon=1, Rare=2, Epic=3, Legendary=4, Mythical=4 }
    return map[rarity] or 0
end

-- Maps rarity strings to the integer expected by DrawTitlePlate (matches HRarity.RarityToLangInt).
local function rarityToLangInt(rarity)
    local map = { Poor=0, Common=1, Uncommon=2, Rare=3, Epic=4, Legendary=5, Mythical=6 }
    return map[rarity] or 1
end

local function getGrayColor()
    return COLOR:CreateColorFromRGBA(120, 120, 120, 255)
end

local function getWhiteColor()
    return COLOR:CreateColorFromRGBA(255, 255, 255, 255)
end

local function getYellowColor()
    return COLOR:CreateColorFromRGBA(242, 207, 1, 255)
end

-- ─────────────────────────────────────────────────────────────────────────────
-- Animation lazy-loader (avoids loading all N characters up front)
-- ─────────────────────────────────────────────────────────────────────────────

local function ensureCharaAnimLoaded(entry)
    if loadedCharaAnims[entry.FolderName] == nil then
        entry.Character:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)
        loadedCharaAnims[entry.FolderName] = entry
    end
end

-- ─────────────────────────────────────────────────────────────────────────────
-- List builders
-- ─────────────────────────────────────────────────────────────────────────────

local function buildCharacterList()
    characterList = {}
    local count = CHARACTERLIST.Count
    for i = 0, count - 1 do
        local entry = CHARACTERLIST:GetByIndex(i)
        if entry ~= nil then
            characterList[#characterList + 1] = entry
        end
    end
end

local function buildPuchiList()
    puchiList = {}
    local count = PUCHICHARALIST.Count
    for i = 0, count - 1 do
        local entry = PUCHICHARALIST:GetByIndex(i)
        if entry ~= nil then
            puchiList[#puchiList + 1] = entry
        end
    end
end

local function buildNameplateList()
    nameplateList = {}
    local count = NAMEPLATESLIST.Count
    for i = 0, count - 1 do
        local entry = NAMEPLATESLIST:GetByIndex(i)
        if entry ~= nil then
            nameplateList[#nameplateList + 1] = entry
        end
    end
end

---@diagnostic disable-next-line: undefined-global
local function buildHitsoundList()
    hitsoundList = {}
    local count = HITSOUNDSLIST.Count
    for i = 0, count - 1 do
        local entry = HITSOUNDSLIST:GetByIndex(i) ---@diagnostic disable-line: undefined-global
        if entry ~= nil then
            hitsoundList[#hitsoundList + 1] = entry
        end
    end
end

-- ─────────────────────────────────────────────────────────────────────────────
-- Modal trigger helpers
-- ─────────────────────────────────────────────────────────────────────────────

local function showCharacterModal(entry)
    if modal == nil then return end
    -- Create a fresh name-bound LuaCharacter for the modal (modal disposes it)
    local charaForModal = CHARACTER:CreateCharacter(entry.FolderName)
    local rarity = rarityToInt(entry.Rarity)
    modal:Activate(1, rarity, 1, charaForModal, nil)

end

local function showPuchiModal(entry)
    if modal == nil then return end
    local rarity = rarityToInt(entry.Rarity)
    modal:Activate(1, rarity, 2, entry, nil)

end

local function showNameplateModal(entry)
    if modal == nil then return end
    local rarity = rarityToInt(entry.Rarity)
    modal:Activate(1, rarity, 3, entry, nil)

end

-- ─────────────────────────────────────────────────────────────────────────────
-- Unlock logic
-- ─────────────────────────────────────────────────────────────────────────────

local function tryUnlockCharacter(entry, force)
    if save:IsCharacterUnlocked(entry.FolderName) or not entry.UnlockCondition.HasCondition then
        -- Already unlocked (or available by default) — just equip
        save:ChangeCharacter(entry.FolderName)
        if sounds.Decide ~= nil then sounds.Decide:Play() end
        return
    end

    local uc = entry.UnlockCondition
    local canUnlock = force or uc:IsUnlockable(0)
    if not canUnlock then
        if sounds.Error ~= nil then sounds.Error:Play() end
        return
    end

    local price = uc:GetCoinPrice()
    if not force and price > 0 and price > save.Coins then
        if sounds.Error ~= nil then sounds.Error:Play() end
        return
    end

    if not force and price > 0 then
        save:SpendCoins(price)
    end
    save:UnlockCharacter(entry.FolderName)
    save:ChangeCharacter(entry.FolderName)
    showCharacterModal(entry)
end

local function tryUnlockPuchi(entry, force)
    if save:IsPuchicharaUnlocked(entry.FolderName) or not entry.UnlockCondition.HasCondition then
        -- Already unlocked (or available by default) — just equip
        save:ChangePuchichara(entry.FolderName)
        if sounds.Decide ~= nil then sounds.Decide:Play() end
        return
    end

    local uc = entry.UnlockCondition
    local canUnlock = force or uc:IsUnlockable(0)
    if not canUnlock then
        if sounds.Error ~= nil then sounds.Error:Play() end
        return
    end

    local price = uc:GetCoinPrice()
    if not force and price > 0 and price > save.Coins then
        if sounds.Error ~= nil then sounds.Error:Play() end
        return
    end

    if not force and price > 0 then
        save:SpendCoins(price)
    end
    save:UnlockPuchichara(entry.FolderName)
    save:ChangePuchichara(entry.FolderName)
    showPuchiModal(entry)
end

local function tryUnlockNameplate(entry, force)
    if save:IsNameplateUnlocked(entry.Id) or not entry.UnlockCondition.HasCondition then
        -- Already unlocked (or available by default) — just equip
        save:ChangeNameplate(entry.Id)
        if sounds.Decide ~= nil then sounds.Decide:Play() end
        return
    end

    local uc = entry.UnlockCondition
    local canUnlock = force or uc:IsUnlockable(0)
    if not canUnlock then
        if sounds.Error ~= nil then sounds.Error:Play() end
        return
    end

    local price = uc:GetCoinPrice()
    if not force and price > 0 and price > save.Coins then
        if sounds.Error ~= nil then sounds.Error:Play() end
        return
    end

    if not force and price > 0 then
        save:SpendCoins(price)
    end
    save:UnlockNameplate(entry.Id)
    save:ChangeNameplate(entry.Id)
    showNameplateModal(entry)
end

-- ─────────────────────────────────────────────────────────────────────────────
-- Draw helpers
-- ─────────────────────────────────────────────────────────────────────────────

-- Draw a horizontal item list centered on the screen.
-- itemCount: total items, selectedIdx: 0-based index
-- drawItem(i, cx, cy, isSelected, isLocked): called for each visible item
local ITEM_SLOT_W = 280
local VISIBLE_COUNT = 7   -- how many items to show at once
local LIST_CX = 960       -- center X of the list area
local LIST_CY = 520       -- center Y of each item (moved down for headroom)

local function drawItemList(itemCount, selectedIdx, drawItem)
    if itemCount == 0 then return end
    local half = math.floor(VISIBLE_COUNT / 2)
    for slot = -half, half do
        local dataIdx = modWrap(selectedIdx + slot, itemCount)
        local cx = LIST_CX + slot * ITEM_SLOT_W
        local isSelected = (slot == 0)
        drawItem(dataIdx, cx, LIST_CY, isSelected)
    end
end

local function drawCharacterItem(dataIdx, cx, cy, isSelected)
    local entry = characterList[dataIdx + 1]
    if entry == nil then return end

    ensureCharaAnimLoaded(entry)

    local locked = not save:IsCharacterUnlocked(entry.FolderName) and entry.UnlockCondition.HasCondition
    local isEquipped = (save.CharacterName == entry.FolderName)
    local color = locked and getGrayColor() or getWhiteColor()
    local scale = isSelected and 1.2 or 0.9

    entry.Character:SetColor(color.R / 255, color.G / 255, color.B / 255)
    entry.Character:SetScale(scale, scale)
    entry.Character:Update(CHARACTER.ANIM_MENU_NORMAL, true)
    entry.Character:DrawAtAnchor(cx, cy, CHARACTER.ANIM_MENU_NORMAL, "bottom")

    -- Name (centered below the character frame)
    local nameColor = isSelected and getYellowColor() or getWhiteColor()
    local nameTx = font:GetText(entry.DisplayName, false, ITEM_SLOT_W, nameColor)
    nameTx:DrawAtAnchor(cx, cy + 10, "top")

    -- Equipped badge
    if isEquipped then
        local badgeTx = font:GetText("[ Equipped ]", false, ITEM_SLOT_W, getYellowColor())
        badgeTx:DrawAtAnchor(cx, cy + 32, "top")
    end

    if isSelected then
        local infoStr = ""
        if locked then
            local price = entry.UnlockCondition:GetCoinPrice()
            if price > 0 then
                infoStr = "Cost: " .. tostring(price) .. " coins"
            else
                local msg = entry.UnlockCondition:GetConditionMessage()
                infoStr = msg ~= "" and msg or "Locked"
            end
        elseif not isEquipped then
            infoStr = "Press Enter to equip"
        end
        if infoStr ~= "" then
            local infoTx = font:GetText(infoStr, false, ITEM_SLOT_W * 2)
            infoTx:DrawAtAnchor(cx, cy + 56, "top")
        end
    end
end

local function drawPuchiItem(dataIdx, cx, cy, isSelected)
    local entry = puchiList[dataIdx + 1]
    if entry == nil then return end

    local locked = not save:IsPuchicharaUnlocked(entry.FolderName) and entry.UnlockCondition.HasCondition
    local equippedPuchi = save:GetPuchichara()
    local isEquipped = (equippedPuchi ~= nil and equippedPuchi.FolderName == entry.FolderName)
    local scale = isSelected and 1.2 or 0.9

    if entry.tx ~= nil and entry.tx.Loaded then
        -- Chara.png is a 2-frame horizontal sprite sheet; show only the first frame.
        local frameW = math.floor(entry.tx.Width / 2)
        local frameH = entry.tx.Height
        entry.tx:SetColor(locked and getGrayColor() or getWhiteColor())
        entry.tx:SetScale(scale, scale)
        -- DrawRectAtAnchor clips the source rect; "bottom" anchors the bottom-center at (cx, cy).
        entry.tx:DrawRectAtAnchor(cx, cy, 0, 0, frameW, frameH, "bottom")
    end

    -- Name centered below the puchi frame
    local nameColor = isSelected and getYellowColor() or getWhiteColor()
    local nameTx = font:GetText(entry.Name, false, ITEM_SLOT_W, nameColor)
    nameTx:DrawAtAnchor(cx, cy + 10, "top")

    if isEquipped then
        local badgeTx = font:GetText("[ Equipped ]", false, ITEM_SLOT_W, getYellowColor())
        badgeTx:DrawAtAnchor(cx, cy + 32, "top")
    end

    if isSelected then
        local infoStr = ""
        if locked then
            local price = entry.UnlockCondition:GetCoinPrice()
            if price > 0 then
                infoStr = "Cost: " .. tostring(price) .. " coins"
            else
                local msg = entry.UnlockCondition:GetConditionMessage()
                infoStr = msg ~= "" and msg or "Locked"
            end
        elseif not isEquipped then
            infoStr = "Press Enter to equip"
        end
        if infoStr ~= "" then
            local infoTx = font:GetText(infoStr, false, ITEM_SLOT_W * 2)
            infoTx:DrawAtAnchor(cx, cy + 56, "top")
        end
    end
end

-- ── Vertical nameplate list ───────────────────────────────────────────────────
-- Constants: the plate is drawn left-aligned at NP_PLATE_X; info sits to the right.
local NP_CX         = 960    -- horizontal center of the whole nameplate column
local NP_PLATE_W    = 520    -- source width passed to the text renderer
local NP_ROW_H      = 76     -- vertical gap between rows
local NP_HALF_VIS   = 3      -- rows shown above/below the selected one
local NP_LIST_CY    = 480    -- y of the center (selected) row

local function drawNameplateRow(dataIdx, rowY, isSelected)
    local entry = nameplateList[dataIdx + 1]
    if entry == nil then return end

    local locked     = not save:IsNameplateUnlocked(entry.Id) and entry.UnlockCondition.HasCondition
    local isEquipped = (save.NameplateInfo ~= nil and save.NameplateInfo.Id == entry.Id)
    local opacity    = locked and 130 or 255
    local scale      = isSelected and 1.05 or 0.82

    -- Title plate: anchor top-center at (NP_CX, rowY - plateH/2)
    local plateTx  = font:GetText(entry.Title, false, NP_PLATE_W)
    local plateH   = plateTx.Height > 0 and plateTx.Height or 36
    local plateX   = NP_CX - math.floor(NP_PLATE_W * scale / 2)
    local plateY   = rowY  - math.floor(plateH * scale / 2)
    plateTx:SetScale(scale, scale)
    NAMEPLATE:DrawTitlePlate(plateX, plateY, opacity, entry.Type, plateTx,
                             rarityToLangInt(entry.Rarity), entry.Id)

    if isSelected then
        -- Equipped badge
        if isEquipped then
            local badgeTx = font:GetText("[ Equipped ]", false, NP_PLATE_W, getYellowColor())
            badgeTx:DrawAtAnchor(NP_CX, rowY + math.floor(plateH * scale / 2) + 4, "top")
        end

        -- Unlock / equip info
        local infoStr = ""
        if locked then
            local price = entry.UnlockCondition:GetCoinPrice()
            if price > 0 then
                infoStr = "Cost: " .. tostring(price) .. " coins"
            else
                local msg = entry.UnlockCondition:GetConditionMessage()
                infoStr = msg ~= "" and msg or "Locked"
            end
        elseif not isEquipped then
            infoStr = "Press Enter to equip"
        end
        if infoStr ~= "" then
            local infoTx = font:GetText(infoStr, false, NP_PLATE_W * 2)
            infoTx:DrawAtAnchor(NP_CX,
                                rowY + math.floor(plateH * scale / 2) + (isEquipped and 24 or 4),
                                "top")
        end
    end
end

local function drawNameplateVertical()
    local count = #nameplateList
    if count == 0 then return end
    for slot = -NP_HALF_VIS, NP_HALF_VIS do
        local dataIdx = modWrap(subIdx + slot, count)
        local rowY    = NP_LIST_CY + slot * NP_ROW_H
        drawNameplateRow(dataIdx, rowY, slot == 0)
    end
end

local function drawHitsoundItem(dataIdx, cx, cy, isSelected)
    local entry = hitsoundList[dataIdx + 1]
    if entry == nil then return end

    local isActive = (save.SelectedHitsounds == entry.FolderName)
    local nameColor = isSelected and getYellowColor() or getWhiteColor()
    local nameTx = font:GetText(entry.DisplayName, false, ITEM_SLOT_W, nameColor)
    nameTx:DrawAtAnchor(cx, cy, "center")

    if isActive then
        local badgeTx = font:GetText("[ Active ]", false, ITEM_SLOT_W, getYellowColor())
        badgeTx:DrawAtAnchor(cx, cy + 28, "top")
    end

    if isSelected and not isActive then
        local hintTx = font:GetText("Press Enter to activate", false, ITEM_SLOT_W * 2)
        hintTx:DrawAtAnchor(cx, cy + 28, "top")
    end
end

-- ─────────────────────────────────────────────────────────────────────────────
-- draw()
-- ─────────────────────────────────────────────────────────────────────────────

function draw()
    -- Nameplate / coin HUD
    NAMEPLATE:DrawPlayerNameplate(20, 980, 255, 0)

    if font == nil then return end

    if currentScreen == "main" then
        -- Draw 3 main options
        for i = 1, #MAIN_OPTIONS do
            local color = (i == mainIdx) and getYellowColor() or getWhiteColor()
            local tx = font:GetText(MAIN_OPTIONS[i], false, 400, color)
            tx:DrawAtAnchor(960, 400 + (i - 2) * 100, "center")
        end

        local hintTx = font:GetText("[Left/Right] Browse   [Enter] Select   [Esc] Exit", false, 1200)
        hintTx:DrawAtAnchor(960, 980, "top")

    elseif currentScreen == "character" then
        local title = font:GetText(MAIN_OPTIONS[1], false, 600, getYellowColor())
        title:DrawAtAnchor(960, 80, "top")

        drawItemList(#characterList, subIdx, drawCharacterItem)

        local hintTx = font:GetText("[Left/Right] Browse   [Enter] Unlock/Equip   [P] Force unlock   [Esc] Back", false, 1500)
        hintTx:DrawAtAnchor(960, 980, "top")

    elseif currentScreen == "puchichara" then
        local title = font:GetText(MAIN_OPTIONS[2], false, 600, getYellowColor())
        title:DrawAtAnchor(960, 80, "top")

        drawItemList(#puchiList, subIdx, drawPuchiItem)

        local hintTx = font:GetText("[Left/Right] Browse   [Enter] Unlock/Equip   [P] Force unlock   [Esc] Back", false, 1500)
        hintTx:DrawAtAnchor(960, 980, "top")

    elseif currentScreen == "nameplate" then
        local title = font:GetText(MAIN_OPTIONS[3], false, 600, getYellowColor())
        title:DrawAtAnchor(960, 80, "top")

        drawNameplateVertical()

        local hintTx = font:GetText("[Up/Down] Browse   [Enter] Unlock/Equip   [P] Force unlock   [Esc] Back", false, 1500)
        hintTx:DrawAtAnchor(960, 980, "top")

    elseif currentScreen == "hitsounds" then
        local title = font:GetText(MAIN_OPTIONS[4], false, 600, getYellowColor())
        title:DrawAtAnchor(960, 80, "top")

        drawItemList(#hitsoundList, subIdx, drawHitsoundItem)

        local hintTx = font:GetText("[Left/Right] Browse   [Enter] Activate   [Esc] Back", false, 1500)
        hintTx:DrawAtAnchor(960, 980, "top")
    end

    -- Modal drawn on top
    if modal ~= nil and modal.IsActive then
        modal:Draw()
    end
end

-- ─────────────────────────────────────────────────────────────────────────────
-- update()
-- ─────────────────────────────────────────────────────────────────────────────

function update()
    -- Let the modal consume input while active
    if modal ~= nil and modal.IsActive then
        modal:Update()
        return
    end


    if currentScreen == "main" then
        if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
            if sounds.Cancel ~= nil then sounds.Cancel:Play() end
            return Exit("title", nil)
        end

        if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("DownArrow") then
            if sounds.Move ~= nil then sounds.Move:Play() end
            mainIdx = (mainIdx % #MAIN_OPTIONS) + 1
        end
        if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("UpArrow") then
            if sounds.Move ~= nil then sounds.Move:Play() end
            mainIdx = ((mainIdx - 2 + #MAIN_OPTIONS) % #MAIN_OPTIONS) + 1
        end

        if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
            if sounds.Decide ~= nil then sounds.Decide:Play() end
            subIdx = 0
            if mainIdx == 1 then
                currentScreen = "character"
            elseif mainIdx == 2 then
                currentScreen = "puchichara"
            elseif mainIdx == 3 then
                currentScreen = "nameplate"
            else
                currentScreen = "hitsounds"
                -- Pre-select the currently active hitsound
                local active = save.SelectedHitsounds ---@diagnostic disable-line: undefined-field, need-check-nil
                for i, entry in ipairs(hitsoundList) do
                    if entry.FolderName == active then
                        subIdx = i - 1
                        break
                    end
                end
            end
        end

    elseif currentScreen == "character" then
        if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
            if sounds.Cancel ~= nil then sounds.Cancel:Play() end
            currentScreen = "main"
            return
        end

        if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
            if sounds.Move ~= nil then sounds.Move:Play() end
            if #characterList > 0 then
                subIdx = modWrap(subIdx + 1, #characterList)
            end
        end
        if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
            if sounds.Move ~= nil then sounds.Move:Play() end
            if #characterList > 0 then
                subIdx = modWrap(subIdx - 1, #characterList)
            end
        end

        if (INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return")) and #characterList > 0 then
            tryUnlockCharacter(characterList[subIdx + 1], false)
        end

        -- P key = force unlock
        if INPUT:KeyboardPressed("P") and #characterList > 0 then
            tryUnlockCharacter(characterList[subIdx + 1], true)
        end

    elseif currentScreen == "puchichara" then
        if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
            if sounds.Cancel ~= nil then sounds.Cancel:Play() end
            currentScreen = "main"
            return
        end

        if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
            if sounds.Move ~= nil then sounds.Move:Play() end
            if #puchiList > 0 then
                subIdx = modWrap(subIdx + 1, #puchiList)
            end
        end
        if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
            if sounds.Move ~= nil then sounds.Move:Play() end
            if #puchiList > 0 then
                subIdx = modWrap(subIdx - 1, #puchiList)
            end
        end

        if (INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return")) and #puchiList > 0 then
            tryUnlockPuchi(puchiList[subIdx + 1], false)
        end

        if INPUT:KeyboardPressed("P") and #puchiList > 0 then
            tryUnlockPuchi(puchiList[subIdx + 1], true)
        end

    elseif currentScreen == "nameplate" then
        if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
            if sounds.Cancel ~= nil then sounds.Cancel:Play() end
            currentScreen = "main"
            return
        end

        if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("DownArrow") then
            if sounds.Move ~= nil then sounds.Move:Play() end
            if #nameplateList > 0 then
                subIdx = modWrap(subIdx + 1, #nameplateList)
            end
        end
        if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("UpArrow") then
            if sounds.Move ~= nil then sounds.Move:Play() end
            if #nameplateList > 0 then
                subIdx = modWrap(subIdx - 1, #nameplateList)
            end
        end

        if (INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return")) and #nameplateList > 0 then
            tryUnlockNameplate(nameplateList[subIdx + 1], false)
        end

        if INPUT:KeyboardPressed("P") and #nameplateList > 0 then
            tryUnlockNameplate(nameplateList[subIdx + 1], true)
        end

    elseif currentScreen == "hitsounds" then
        if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
            if sounds.Cancel ~= nil then sounds.Cancel:Play() end
            currentScreen = "main"
            return
        end

        if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
            if sounds.Move ~= nil then sounds.Move:Play() end
            if #hitsoundList > 0 then
                subIdx = modWrap(subIdx + 1, #hitsoundList)
            end
        end
        if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
            if sounds.Move ~= nil then sounds.Move:Play() end
            if #hitsoundList > 0 then
                subIdx = modWrap(subIdx - 1, #hitsoundList)
            end
        end

        if (INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return")) and #hitsoundList > 0 then
            local entry = hitsoundList[subIdx + 1]
            if entry ~= nil then
                save.SelectedHitsounds = entry.FolderName ---@diagnostic disable-line: inject-field
                if sounds.Decide ~= nil then sounds.Decide:Play() end
            end
        end
    end
end

-- ─────────────────────────────────────────────────────────────────────────────
-- activate() / deactivate()
-- ─────────────────────────────────────────────────────────────────────────────

function activate()
    save = GetSaveFile(0)
    CONFIG.PlayerCount = 1

    modal = ROACTIVITY:GetROActivity("modal")

    currentScreen = "main"
    mainIdx = 1
    subIdx = 0


    loadedCharaAnims = {}

    buildCharacterList()
    buildPuchiList()
    buildNameplateList()
    buildHitsoundList()
    -- Character animations are loaded lazily in drawCharacterItem via ensureCharaAnimLoaded.
end

function deactivate()
    -- Dispose only the animations that were actually loaded during this session.
    for _, entry in pairs(loadedCharaAnims) do
        entry.Character:DisposeAnimation(CHARACTER.ANIM_MENU_NORMAL)
    end
    loadedCharaAnims = {}
end

-- ─────────────────────────────────────────────────────────────────────────────
-- onStart() / onDestroy()
-- ─────────────────────────────────────────────────────────────────────────────

function onStart()
    font = TEXT:Create(20)

    -- Use skin sounds if available, fall back gracefully
    sounds.Move   = SOUND:CreateSFX("Sounds/Move.ogg")
    sounds.Decide = SOUND:CreateSFX("Sounds/Decide.ogg")
    sounds.Cancel = SOUND:CreateSFX("Sounds/Cancel.ogg")
    sounds.Error  = SOUND:CreateSFX("Sounds/SoldOut.ogg")
end

function onDestroy()
    if font ~= nil then
        font:Dispose()
        font = nil
    end
    for _, snd in pairs(sounds) do
        snd:Dispose()
    end
    sounds = {}
end
