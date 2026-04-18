---@diagnostic disable: undefined-global  -- globals injected at runtime

-- customize_dialog/Script.lua
-- Per-player dialog (activated from the difficulty select screen) that lets a
-- player choose their Character, Puchichara, Nameplate, and Hitsounds.
--
-- Only items that are unlocked (or have no explicit unlock condition) appear in
-- the selection lists.  Changes are staged locally and applied only when the
-- player confirms with OK.  Cancel discards all changes.

-- ─── State ────────────────────────────────────────────────────────────────────

local reactive = false
local player   = 0
local save     = nil

-- Current sub-screen: "main" | "character" | "puchichara" | "nameplate" | "hitsounds"
local currentScreen = "main"

-- Main-menu cursor (1-based).  1-4 = categories, 5 = OK, 6 = Cancel.
local mainIdx    = 1
local MAIN_LABELS = { "Character", "Puchichara", "Nameplate", "Hitsounds" }
local MAIN_COUNT  = 6   -- 4 categories + OK + Cancel
local OK_IDX      = 5
local CANCEL_IDX  = 6

-- Item lists (only available items, built on activate)
local characterList  = {}
local puchiList      = {}
local nameplateList  = {}
local hitsoundList   = {}

-- Selection indices inside each list (0-based, wrapping)
local charSubIdx  = 0
local puchiSubIdx = 0
local npSubIdx    = 0
local hsSubIdx    = 0

-- Character animation preview:
--   loadedCharaEntry   — reference to the LuaCharacterEntry (for FolderName lookup)
--   loadedCharaInst    — NEW LuaCharacter created and fully owned by this dialog;
--                        completely independent from song_select's character instances
local loadedCharaEntry = nil
local loadedCharaInst  = nil

-- Saved sub-indices (for cancel/restore on Esc in each sub-screen)
local savedCharSubIdx  = 0
local savedPuchiSubIdx = 0
local savedNpSubIdx    = 0
local savedHsSubIdx    = 0

-- Puchichara sine-float animation
local sineY = 0

-- Hitsound preview sounds (loaded on demand, disposed on each navigation / deactivate)
local hsPreviewDon = nil
local hsPreviewKa  = nil

-- Slide-in transition
local bgpos  = 1080
local bgtlop = 0

-- ─── Player input sets ────────────────────────────────────────────────────────

local inputSets = {
    { right = "RightChange", left = "LeftChange", decide1 = "Decide",  decide2 = "Decide",  cancel = "Cancel" },
    { right = "RBlue2P",     left = "LBlue2P",    decide1 = "RRed2P",  decide2 = "LRed2P",  cancel = nil },
    { right = "RBlue3P",     left = "LBlue3P",    decide1 = "RRed3P",  decide2 = "LRed3P",  cancel = nil },
    { right = "RBlue4P",     left = "LBlue4P",    decide1 = "RRed4P",  decide2 = "LRed4P",  cancel = nil },
    { right = "RBlue5P",     left = "LBlue5P",    decide1 = "RRed5P",  decide2 = "LRed5P",  cancel = nil },
}

-- ─── Fonts / textures / sounds / counters ────────────────────────────────────

local textSmall = nil
local text      = nil
local textLarge = nil
local tx        = {}
local sounds    = {}
local ctx       = {}

-- ─── Layout constants ─────────────────────────────────────────────────────────
-- Left panel  (menu / lists):  x 0–1060,  centre at 530
-- Right panel (preview):       x 1100–1920

-- Left panel
local MENU_CX        = 530
local MENU_TITLE_X   = 80
local MENU_TITLE_Y   = 60
local MENU_ORIGIN_Y  = 200
local MENU_SPACING_Y = 90

-- Footer OK / Cancel (matches mod_select_dialog layout)
local MENU_FOOTER_Y  = 835
local MENU_OK_X      = 510
local MENU_CANCEL_X  = 710

-- Horizontal list (character / puchichara): 3 visible slots (-1, 0, +1)
local ITEM_SLOT_W   = 220
local VISIBLE_COUNT = 3
local LIST_CX       = 530
local LIST_CY       = 840   -- moved down 320 px so items sit lower in the panel

-- Vertical nameplate / hitsound list (shared layout)
local NP_LIST_CX  = 530
local NP_LIST_CY  = 520
local NP_ROW_H    = 84
local NP_HALF_VIS = 3
local NP_PLATE_W  = 480

local HS_LIST_CX  = NP_LIST_CX
local HS_LIST_CY  = NP_LIST_CY
local HS_ROW_H    = NP_ROW_H
local HS_HALF_VIS = NP_HALF_VIS

-- Preview positions — character bottom and nameplate share PREVIEW_BASE_Y so the
-- nameplate appears directly under the character, matching the in-game layout.
local PREVIEW_BASE_Y  = 720    -- character "bottom" / nameplate top-left y
local PREVIEW_NP_X    = 1120   -- nameplate left-edge x
local PREVIEW_CHARA_X = 1258   -- character centre-x
local PREVIEW_PUCHI_X = 1198   -- puchichara centre-x (60 px left of character)
local PREVIEW_PUCHI_BASE = 500  -- puchichara centre-y (140 px lower than before)

-- ─── Color helpers ────────────────────────────────────────────────────────────

local COL_WHITE  = "FFFFFFFF"
local COL_YELLOW = "FFF2CF01"

local function rarityToLangInt(rarity)
    local map = { Poor=0, Common=1, Uncommon=2, Rare=3, Epic=4, Legendary=5, Mythical=6 }
    return map[rarity] or 1
end

-- ─── Misc helpers ─────────────────────────────────────────────────────────────

local function modWrap(v, n) return (v % n + n) % n end

local function isCharAvailable(entry)
    return save:IsCharacterUnlocked(entry.FolderName) or not entry.UnlockCondition.HasCondition
end
local function isPuchiAvailable(entry)
    return save:IsPuchicharaUnlocked(entry.FolderName) or not entry.UnlockCondition.HasCondition
end
local function isNpAvailable(entry)
    return save:IsNameplateUnlocked(entry.Id) or not entry.UnlockCondition.HasCondition
end

-- ─── List builders ────────────────────────────────────────────────────────────

local function buildCharacterList()
    characterList = {}
    for i = 0, CHARACTERLIST.Count - 1 do
        local e = CHARACTERLIST:GetByIndex(i)
        if e ~= nil and isCharAvailable(e) then
            characterList[#characterList + 1] = e
        end
    end
end

local function buildPuchiList()
    puchiList = {}
    for i = 0, PUCHICHARALIST.Count - 1 do
        local e = PUCHICHARALIST:GetByIndex(i)
        if e ~= nil and isPuchiAvailable(e) then
            puchiList[#puchiList + 1] = e
        end
    end
end

local function buildNameplateList()
    nameplateList = {}
    for i = 0, NAMEPLATESLIST.Count - 1 do
        local e = NAMEPLATESLIST:GetByIndex(i)
        if e ~= nil and isNpAvailable(e) then
            nameplateList[#nameplateList + 1] = e
        end
    end
end

local function buildHitsoundList()
    hitsoundList = {}
    for i = 0, HITSOUNDSLIST.Count - 1 do
        local e = HITSOUNDSLIST:GetByIndex(i)
        if e ~= nil then hitsoundList[#hitsoundList + 1] = e end
    end
end

-- ─── Character preview instance management ────────────────────────────────────
-- Each preview character is a freshly-created LuaCharacter (via CHARACTER:CreateCharacter)
-- that is fully owned and isolated from the shared instances used by song_select.
-- Only one instance is alive at any moment.

local function disposePreviewCharacter()
    if loadedCharaInst ~= nil then
        loadedCharaInst:DisposeAnimation(CHARACTER.ANIM_MENU_NORMAL)
        loadedCharaInst:Dispose()
        loadedCharaInst = nil
    end
    loadedCharaEntry = nil
end

local function swapPreviewCharacter(newEntry)
    if newEntry == nil then return end
    if loadedCharaEntry ~= nil and loadedCharaEntry.FolderName == newEntry.FolderName then return end

    -- Create a new owned instance, load its animation
    local newInst = CHARACTER:CreateCharacter(newEntry.FolderName)
    if newInst ~= nil then
        newInst:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)
    end

    -- Dispose the previous owned instance first
    disposePreviewCharacter()

    loadedCharaEntry = newEntry
    loadedCharaInst  = newInst
end

-- ─── Find initial sub-indices (currently equipped items) ─────────────────────

local function findCharSubIdx()
    local name = save.CharacterName
    for i, e in ipairs(characterList) do
        if e.FolderName == name then return i - 1 end
    end
    return 0
end

local function findPuchiSubIdx()
    local p    = save:GetPuchichara()
    local name = p ~= nil and p.FolderName or ""
    for i, e in ipairs(puchiList) do
        if e.FolderName == name then return i - 1 end
    end
    return 0
end

local function findNpSubIdx()
    local info = save.NameplateInfo
    local id   = (info ~= nil) and info.Id or -1
    for i, e in ipairs(nameplateList) do
        if e.Id == id then return i - 1 end
    end
    return 0
end

local function findHsSubIdx()
    local active = save.SelectedHitsounds
    for i, e in ipairs(hitsoundList) do
        if e.FolderName == active then return i - 1 end
    end
    return 0
end

-- ─── Apply pending changes on OK ──────────────────────────────────────────────

local function applyChanges()
    if #characterList  > 0 then save:ChangeCharacter(characterList[charSubIdx + 1].FolderName) end
    if #puchiList      > 0 then save:ChangePuchichara(puchiList[puchiSubIdx + 1].FolderName)   end
    if #nameplateList  > 0 then save:ChangeNameplate(nameplateList[npSubIdx + 1].Id)            end
    if #hitsoundList   > 0 then save.SelectedHitsounds = hitsoundList[hsSubIdx + 1].FolderName  end
end

-- ─── Background draw ──────────────────────────────────────────────────────────

local function drawBg(opacity)
    tx["bgtile"]:SetOpacity((opacity * bgtlop) / 255)
    for i = 0, 10 do
        for j = 0, 10 do
            tx["bgtile"]:Draw(i * 192, j * 108)
        end
    end
end

-- ─── Preview draw (right panel) ───────────────────────────────────────────────
-- Layout mirrors the in-game / song_select look:
--   character bottom-anchored at (PREVIEW_CHARA_X, PREVIEW_BASE_Y)
--   nameplate at                 (PREVIEW_NP_X,    PREVIEW_BASE_Y)   ← same Y
--   puchichara floating at       (PREVIEW_PUCHI_X, PREVIEW_PUCHI_BASE + sineY)

local function drawPreview(alpha)
    local opacity = math.floor(alpha * 255)

    -- ── Character (owned preview instance) ──
    if loadedCharaInst ~= nil and loadedCharaInst.IsValid then
        loadedCharaInst:SetScale(1, 1)
        loadedCharaInst:Update(CHARACTER.ANIM_MENU_NORMAL, true)
        loadedCharaInst:DrawAtAnchor(
            PREVIEW_CHARA_X, PREVIEW_BASE_Y,
            CHARACTER.ANIM_MENU_NORMAL, "bottom",
            1, 1, opacity
        )
    end

    -- ── Nameplate — draw the selected nameplate with the player's real name/dan grade.
    --   Uses nameplate ROActivity draw mode 3: full nameplate with title overridden by args. ──
    local npEntry = #nameplateList > 0 and nameplateList[npSubIdx + 1] or nil
    if npEntry ~= nil then
        ROACTIVITY:GetROActivity("nameplate"):Draw(
            3, PREVIEW_NP_X, PREVIEW_BASE_Y, opacity, player, 0,
            npEntry.Title, npEntry.Type, rarityToLangInt(npEntry.Rarity), npEntry.Id
        )
    else
        NAMEPLATE:DrawPlayerNameplate(PREVIEW_NP_X, PREVIEW_BASE_Y, opacity, player)
    end

    -- ── Puchichara (floating sine) ──
    if #puchiList > 0 then
        local e = puchiList[puchiSubIdx + 1]
        if e ~= nil and e.tx ~= nil and e.tx.Loaded then
            local frameW = math.floor(e.tx.Width / 2)
            local frameH = e.tx.Height
            e.tx:SetOpacity(alpha)
            e.tx:DrawRectAtAnchor(
                PREVIEW_PUCHI_X, PREVIEW_PUCHI_BASE + math.floor(sineY),
                0, 0, frameW, frameH, "center"
            )
            e.tx:SetOpacity(1)
        end
    end
end

-- ─── Horizontal item-list helpers ─────────────────────────────────────────────

local function drawHorizList(list, selectedIdx, alpha, itemDrawFn)
    if #list == 0 then return end
    local half = math.floor(VISIBLE_COUNT / 2)
    for slot = -half, half do
        local dataIdx = modWrap(selectedIdx + slot, #list)
        local cx      = LIST_CX + slot * ITEM_SLOT_W
        itemDrawFn(list[dataIdx + 1], cx, LIST_CY, slot == 0, alpha)
    end
end

-- Character: show the owned preview animation only for the centre slot.
local function drawCharItem(entry, cx, cy, isSelected, alpha)
    if entry == nil then return end
    if isSelected
        and loadedCharaInst ~= nil
        and loadedCharaEntry ~= nil
        and loadedCharaEntry.FolderName == entry.FolderName
        and loadedCharaInst.IsValid
    then
        -- Animation was already updated this frame in drawPreview; just draw again at list pos.
        loadedCharaInst:SetScale(1.1, 1.1)
        loadedCharaInst:DrawAtAnchor(
            cx, cy, CHARACTER.ANIM_MENU_NORMAL, "bottom",
            1, 1, math.floor(alpha * 255)
        )
        loadedCharaInst:SetScale(1, 1)
    end
    local col   = isSelected and COLOR:CreateColorFromHex(COL_YELLOW) or COLOR:CreateColorFromHex(COL_WHITE)
    local namTx = textSmall:GetText(entry.DisplayName, false, ITEM_SLOT_W - 10, col)
    namTx:SetOpacity(alpha)
    namTx:DrawAtAnchor(cx, cy + 8, "top")
end

-- Puchichara: show first sprite-sheet frame.
local function drawPuchiItem(entry, cx, cy, isSelected, alpha)
    if entry == nil then return end
    if entry.tx ~= nil and entry.tx.Loaded then
        local frameW = math.floor(entry.tx.Width / 2)
        local frameH = entry.tx.Height
        local scale  = isSelected and 1.4 or 1.0
        entry.tx:SetScale(scale, scale)
        entry.tx:SetOpacity(alpha)
        entry.tx:DrawRectAtAnchor(cx, cy, 0, 0, frameW, frameH, "bottom")
        entry.tx:SetOpacity(1)
        entry.tx:SetScale(1, 1)
    end
    local col   = isSelected and COLOR:CreateColorFromHex(COL_YELLOW) or COLOR:CreateColorFromHex(COL_WHITE)
    local namTx = textSmall:GetText(entry.Name, false, ITEM_SLOT_W - 10, col)
    namTx:SetOpacity(alpha)
    namTx:DrawAtAnchor(cx, cy + 8, "top")
end

-- ─── Hitsound preview ────────────────────────────────────────────────────────
-- Plays Don + Ka from the currently selected hitsound set.
-- Stops and disposes any previously playing preview sounds first.

local function stopHitsoundPreview()
    if hsPreviewDon ~= nil then hsPreviewDon:Stop() ; hsPreviewDon:Dispose() ; hsPreviewDon = nil end
    if hsPreviewKa  ~= nil then hsPreviewKa:Stop()  ; hsPreviewKa:Dispose()  ; hsPreviewKa  = nil end
end

local function playHitsoundPreview()
    stopHitsoundPreview()
    if #hitsoundList == 0 then return end
    local entry = hitsoundList[hsSubIdx + 1]
    if entry == nil then return end
    local donPath = entry.DonPath
    local kaPath  = entry.KaPath
    if donPath ~= nil and donPath ~= "" then
        hsPreviewDon = SOUND:CreateSFXFromAbsolutePath(donPath)
        if hsPreviewDon ~= nil and hsPreviewDon.Loaded then hsPreviewDon:Play() end
    end
    if kaPath ~= nil and kaPath ~= "" then
        hsPreviewKa = SOUND:CreateSFXFromAbsolutePath(kaPath)
        if hsPreviewKa ~= nil and hsPreviewKa.Loaded then hsPreviewKa:Play() end
    end
end

-- Vertical hitsound list (mirrors the nameplate list style — name text only).
local function drawHitsoundList(alpha)
    if #hitsoundList == 0 then return end
    for slot = -HS_HALF_VIS, HS_HALF_VIS do
        local dataIdx    = modWrap(hsSubIdx + slot, #hitsoundList)
        local entry      = hitsoundList[dataIdx + 1]
        if entry ~= nil then
            local rowY       = HS_LIST_CY + slot * HS_ROW_H
            local isSelected = (slot == 0)
            local opacity    = isSelected and alpha or alpha * 0.5
            local col        = isSelected and COLOR:CreateColorFromHex(COL_YELLOW) or COLOR:CreateColorFromHex(COL_WHITE)
            local namTx      = text:GetText(entry.DisplayName, false, 500, col)
            namTx:SetOpacity(opacity)
            namTx:DrawAtAnchor(HS_LIST_CX, rowY, "center")
        end
    end
end

-- Vertical nameplate list — each row shows the full in-game nameplate
-- (player name, dan grade, nameplate style) for a specific nameplate entry.
local function drawNameplateList(alpha)
    if #nameplateList == 0 then return end
    for slot = -NP_HALF_VIS, NP_HALF_VIS do
        local dataIdx    = modWrap(npSubIdx + slot, #nameplateList)
        local entry      = nameplateList[dataIdx + 1]
        if entry ~= nil then
            local rowY       = NP_LIST_CY + slot * NP_ROW_H
            local isSelected = (slot == 0)
            local opacity    = math.floor((isSelected and alpha or alpha * 0.5) * 255)
            local nx         = NP_LIST_CX - math.floor(NP_PLATE_W / 2)
            ROACTIVITY:GetROActivity("nameplate"):Draw(
                3, nx, rowY - 30, opacity, player, 0,
                entry.Title, entry.Type, rarityToLangInt(entry.Rarity), entry.Id
            )
        end
    end
end

-- ─── Counter helper ───────────────────────────────────────────────────────────

local function startCounter(key, s, e, interval, mode, cb, onFinish)
    local c = COUNTER:CreateCounter(s, e, interval, onFinish)
    if mode == "loop"   then c:SetLoop(true)   end
    if mode == "bounce" then c:SetBounce(true) end
    if cb then c:Listen(cb) end
    c:Start()
    ctx[key] = c
    return c
end

local function updateTransitionVisuals(val)
    bgpos  = val
    local op = 255 - (val * (255 / 540))
    bgtlop = math.max(0, math.min(255, op))
end

-- ─── draw() ───────────────────────────────────────────────────────────────────

function draw()
    drawBg(0.5)
    tx["bg"]:SetOpacity(bgtlop / 255)
    tx["bg"]:Draw(0, bgpos)
    tx["player" .. player]:SetOpacity(bgtlop / 255)
    tx["player" .. player]:DrawAtAnchor(1920, 1080 + bgpos, "bottomright")

    if bgtlop == 0 then return end
    local alpha = bgtlop / 255

    -- Preview panel (always visible while the dialog is open)
    drawPreview(alpha)

    -- Title
    local titleTx = textLarge:GetText("Customize", false, 700)
    titleTx:SetOpacity(alpha)
    titleTx:Draw(MENU_TITLE_X, MENU_TITLE_Y)

    if currentScreen == "main" then
        -- ── Category rows (1-4) ──
        for i = 1, #MAIN_LABELS do
            local isSel = (i == mainIdx)
            local col   = isSel and COLOR:CreateColorFromHex(COL_YELLOW) or COLOR:CreateColorFromHex(COL_WHITE)
            local item  = text:GetText(MAIN_LABELS[i], false, 500, col)
            item:SetOpacity(alpha)
            item:DrawAtAnchor(MENU_CX, MENU_ORIGIN_Y + (i - 1) * MENU_SPACING_Y, "top")
        end

        -- ── OK / Cancel footer (matches mod_select_dialog layout) ──
        local okCol  = (mainIdx == OK_IDX)     and COLOR:CreateColorFromHex(COL_YELLOW) or COLOR:CreateColorFromHex(COL_WHITE)
        local canCol = (mainIdx == CANCEL_IDX) and COLOR:CreateColorFromHex(COL_YELLOW) or COLOR:CreateColorFromHex(COL_WHITE)
        local okTx  = text:GetText("OK",     false, 200, okCol)
        local canTx = text:GetText("Cancel", false, 200, canCol)
        okTx:SetOpacity(alpha)  ; okTx:Draw(MENU_OK_X,     MENU_FOOTER_Y)
        canTx:SetOpacity(alpha) ; canTx:Draw(MENU_CANCEL_X, MENU_FOOTER_Y)

        local desc = textSmall:GetText("Choose your character, puchichara, nameplate and hitsounds.", false, 900)
        desc:SetOpacity(alpha)
        desc:DrawAtAnchor(MENU_CX, 950, "top")

    elseif currentScreen == "character" then
        local ttl = text:GetText("Character", false, 400, COLOR:CreateColorFromHex(COL_YELLOW))
        ttl:SetOpacity(alpha) ; ttl:DrawAtAnchor(MENU_CX, MENU_TITLE_Y + 40, "top")

        if #characterList == 0 then
            local e = textSmall:GetText("No characters available.", false, 600)
            e:SetOpacity(alpha) ; e:DrawAtAnchor(MENU_CX, LIST_CY, "center")
        else
            drawHorizList(characterList, charSubIdx, alpha, drawCharItem)
        end

    elseif currentScreen == "puchichara" then
        local ttl = text:GetText("Puchichara", false, 400, COLOR:CreateColorFromHex(COL_YELLOW))
        ttl:SetOpacity(alpha) ; ttl:DrawAtAnchor(MENU_CX, MENU_TITLE_Y + 40, "top")

        if #puchiList == 0 then
            local e = textSmall:GetText("No puchichara available.", false, 600)
            e:SetOpacity(alpha) ; e:DrawAtAnchor(MENU_CX, LIST_CY, "center")
        else
            drawHorizList(puchiList, puchiSubIdx, alpha, drawPuchiItem)
        end

    elseif currentScreen == "nameplate" then
        local ttl = text:GetText("Nameplate", false, 400, COLOR:CreateColorFromHex(COL_YELLOW))
        ttl:SetOpacity(alpha) ; ttl:DrawAtAnchor(MENU_CX, MENU_TITLE_Y + 40, "top")

        drawNameplateList(alpha)

    elseif currentScreen == "hitsounds" then
        local ttl = text:GetText("Hitsounds", false, 400, COLOR:CreateColorFromHex(COL_YELLOW))
        ttl:SetOpacity(alpha) ; ttl:DrawAtAnchor(MENU_CX, MENU_TITLE_Y + 40, "top")

        if #hitsoundList == 0 then
            local e = textSmall:GetText("No hitsound sets available.", false, 600)
            e:SetOpacity(alpha) ; e:DrawAtAnchor(MENU_CX, HS_LIST_CY, "center")
        else
            drawHitsoundList(alpha)
        end

    end
end

-- ─── update() ─────────────────────────────────────────────────────────────────

function update()
    for _, c in pairs(ctx) do c:Tick() end
    if not reactive then return end

    local inputs = inputSets[player + 1]

    local function right()  return INPUT:Pressed(inputs.right)   or INPUT:KeyboardPressed("RightArrow") end
    local function left()   return INPUT:Pressed(inputs.left)    or INPUT:KeyboardPressed("LeftArrow")  end
    local function up()     return INPUT:Pressed(inputs.left)    or INPUT:KeyboardPressed("UpArrow")    end
    local function down()   return INPUT:Pressed(inputs.right)   or INPUT:KeyboardPressed("DownArrow")  end
    local function decide() return INPUT:Pressed(inputs.decide1) or INPUT:Pressed(inputs.decide2) or INPUT:KeyboardPressed("Return") end
    local function cancel() return (inputs.cancel ~= nil and INPUT:Pressed(inputs.cancel)) or INPUT:KeyboardPressed("Escape") end

    local function exitDialog(save_changes)
        reactive = false
        if save_changes then applyChanges() end
        startCounter("exit", 0, 1080, 0.5 / 1080, "none", updateTransitionVisuals, function()
            DEACTIVATE()
        end)
    end

    -- ── Main menu ─────────────────────────────────────────────────────────────
    if currentScreen == "main" then
        if down() then
            sounds.Skip:Play()
            mainIdx = (mainIdx % MAIN_COUNT) + 1
        elseif up() then
            sounds.Skip:Play()
            mainIdx = ((mainIdx - 2 + MAIN_COUNT) % MAIN_COUNT) + 1
        elseif decide() then
            if     mainIdx == OK_IDX     then sounds.Decide:Play() ; exitDialog(true)
            elseif mainIdx == CANCEL_IDX then sounds.Cancel:Play() ; exitDialog(false)
            elseif mainIdx == 1 then sounds.Decide:Play() ; savedCharSubIdx  = charSubIdx  ; currentScreen = "character"
            elseif mainIdx == 2 then sounds.Decide:Play() ; savedPuchiSubIdx = puchiSubIdx ; currentScreen = "puchichara"
            elseif mainIdx == 3 then sounds.Decide:Play() ; savedNpSubIdx    = npSubIdx    ; currentScreen = "nameplate"
            elseif mainIdx == 4 then sounds.Decide:Play() ; savedHsSubIdx    = hsSubIdx    ; currentScreen = "hitsounds"
            end
        elseif cancel() then
            sounds.Cancel:Play()
            exitDialog(false)
        end

    -- ── Character ─────────────────────────────────────────────────────────────
    elseif currentScreen == "character" then
        if right() and #characterList > 0 then
            sounds.Skip:Play()
            local newIdx = modWrap(charSubIdx + 1, #characterList)
            swapPreviewCharacter(characterList[newIdx + 1])
            charSubIdx = newIdx
        elseif left() and #characterList > 0 then
            sounds.Skip:Play()
            local newIdx = modWrap(charSubIdx - 1, #characterList)
            swapPreviewCharacter(characterList[newIdx + 1])
            charSubIdx = newIdx
        elseif decide() then
            sounds.Decide:Play() ; currentScreen = "main"
        elseif cancel() then
            sounds.Cancel:Play()
            charSubIdx = savedCharSubIdx
            swapPreviewCharacter(characterList[charSubIdx + 1])
            currentScreen = "main"
        end

    -- ── Puchichara ────────────────────────────────────────────────────────────
    elseif currentScreen == "puchichara" then
        if right() and #puchiList > 0 then
            sounds.Skip:Play() ; puchiSubIdx = modWrap(puchiSubIdx + 1, #puchiList)
        elseif left() and #puchiList > 0 then
            sounds.Skip:Play() ; puchiSubIdx = modWrap(puchiSubIdx - 1, #puchiList)
        elseif decide() then
            sounds.Decide:Play() ; currentScreen = "main"
        elseif cancel() then
            sounds.Cancel:Play() ; puchiSubIdx = savedPuchiSubIdx ; currentScreen = "main"
        end

    -- ── Nameplate ─────────────────────────────────────────────────────────────
    elseif currentScreen == "nameplate" then
        if down() and #nameplateList > 0 then
            sounds.Skip:Play() ; npSubIdx = modWrap(npSubIdx + 1, #nameplateList)
        elseif up() and #nameplateList > 0 then
            sounds.Skip:Play() ; npSubIdx = modWrap(npSubIdx - 1, #nameplateList)
        elseif decide() then
            sounds.Decide:Play() ; currentScreen = "main"
        elseif cancel() then
            sounds.Cancel:Play() ; npSubIdx = savedNpSubIdx ; currentScreen = "main"
        end

    -- ── Hitsounds ─────────────────────────────────────────────────────────────
    elseif currentScreen == "hitsounds" then
        if down() and #hitsoundList > 0 then
            sounds.Skip:Play() ; hsSubIdx = modWrap(hsSubIdx + 1, #hitsoundList) ; playHitsoundPreview()
        elseif up() and #hitsoundList > 0 then
            sounds.Skip:Play() ; hsSubIdx = modWrap(hsSubIdx - 1, #hitsoundList) ; playHitsoundPreview()
        elseif decide() then
            sounds.Decide:Play() ; currentScreen = "main"
        elseif cancel() then
            sounds.Cancel:Play() ; hsSubIdx = savedHsSubIdx ; stopHitsoundPreview() ; currentScreen = "main"
        end
    end
end

-- ─── activate() ───────────────────────────────────────────────────────────────

function activate(pl)
    player = pl or 0
    save   = GetSaveFile(player)

    -- Build filtered lists (unlocked / no-condition items only)
    buildCharacterList()
    buildPuchiList()
    buildNameplateList()
    buildHitsoundList()

    -- Initialise selection at the currently equipped item
    charSubIdx  = findCharSubIdx()
    puchiSubIdx = findPuchiSubIdx()
    npSubIdx    = findNpSubIdx()
    hsSubIdx    = findHsSubIdx()

    -- Create a fresh, owned preview character instance (independent of song_select)
    loadedCharaEntry = nil
    loadedCharaInst  = nil
    if #characterList > 0 then
        local e = characterList[charSubIdx + 1]
        if e ~= nil then
            local inst = CHARACTER:CreateCharacter(e.FolderName)
            if inst ~= nil then inst:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL) end
            loadedCharaEntry = e
            loadedCharaInst  = inst
        end
    end

    currentScreen = "main"
    mainIdx       = 1

    sounds.Skip   = SHARED:GetSharedSound("Skip")
    sounds.Decide = SHARED:GetSharedSound("Decide")
    sounds.Cancel = SHARED:GetSharedSound("Cancel")

    -- Slide-in
    startCounter("enter", 1080, 0, -0.5 / 1080, "none", updateTransitionVisuals, function()
        reactive = true
    end)

    -- Puchichara floating animation (±15 px sine)
    startCounter("sine", 0, 360, 1 / 120, "loop", function(val)
        sineY = math.sin(val * math.pi / 180) * 15
    end)
end

-- ─── deactivate() ─────────────────────────────────────────────────────────────

function deactivate()
    reactive = false

    -- Stop and dispose any in-progress hitsound preview
    stopHitsoundPreview()

    -- Dispose the owned preview character instance (no effect on song_select)
    disposePreviewCharacter()

    -- Stop all counters
    for k in pairs(ctx) do
        ctx[k] = COUNTER:EmptyCounter()
    end

    -- Clear lists (entries are owned by the global databases, not by us)
    characterList = {}
    puchiList     = {}
    nameplateList = {}
    hitsoundList  = {}
end

-- ─── onStart() / onDestroy() ──────────────────────────────────────────────────

function onStart()
    textSmall = TEXT:Create(18)
    text      = TEXT:Create(26)
    textLarge = TEXT:Create(40)

    tx["bg"]     = TEXTURE:CreateTexture("Textures/Background.png")
    tx["bgtile"] = TEXTURE:CreateTexture("Textures/BgTile.png")
    for i = 1, 5 do
        tx["player" .. (i - 1)] = TEXTURE:CreateTexture("Textures/" .. i .. "P.png")
    end
end

function onDestroy()
    if textSmall ~= nil then textSmall:Dispose() ; textSmall = nil end
    if text      ~= nil then text:Dispose()      ; text      = nil end
    if textLarge ~= nil then textLarge:Dispose() ; textLarge = nil end
    for _, t in pairs(tx) do t:Dispose() end
    tx = {}
end
