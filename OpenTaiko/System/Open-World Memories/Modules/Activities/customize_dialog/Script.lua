---@diagnostic disable: undefined-global, undefined-field  -- globals / C# methods injected at runtime

-- per-player customize dialog
-- changes staged until OK; Cancel discards them

local NavInput = require("NavInput")

-- layout: Config/layout.json; timings: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

-- skin locale string (Locales/<lang>/); fallback when missing
local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback or key
end

-- ==== State ====

local reactive = false
local player   = 0
local save     = nil

-- "main" | "character" | "palette" | "puchichara" | "nameplate_title" | "dan_title" | "player_name" | "hitsounds"
local currentScreen = "main"

-- main menu cursor: 1-7 categories, 8 OK, 9 Cancel
local mainIdx    = 1
local MAIN_LABELS = {}   -- filled by loadStrings
local MAIN_COUNT  = 9   -- 7 categories + OK + Cancel
local OK_IDX      = 8
local CANCEL_IDX  = 9

-- available items only, built on activate
local characterList  = {}
local puchiList      = {}
local nameplateList  = {}
local danList        = {}
local hitsoundList   = {}

-- list cursors, 0-based, wrapping
local charSubIdx  = 0
local puchiSubIdx = 0
local npSubIdx    = 0
local danSubIdx   = 0
local hsSubIdx    = 0

-- preview character; loadedCharaInst is owned here, apart from song_select's
local loadedCharaEntry = nil
local loadedCharaInst  = nil

-- restored on Esc in each sub-screen
local savedCharSubIdx  = 0
local savedPuchiSubIdx = 0
local savedNpSubIdx    = 0
local savedDanSubIdx   = 0
local savedHsSubIdx    = 0

local stagedPlayerName = ""
local savedPlayerName  = ""
local playerNameInput  = nil   -- LuaTextInput, alive while the "player_name" screen is open

-- puchichara float offset
local sineY = 0

-- hitsound preview; disposed on each move and on deactivate
local hsPreviewDon = nil
local hsPreviewKa  = nil

-- palettes of the preview character: { name, plays, gradient } (gradient nil: Default)
local paletteList          = {}
local paletteSubIdx        = 0
local savedPaletteSubIdx   = 0
local palFlashColor        = nil   -- nil: white; else flash colour (as in song_select_core)
local currentPaletteFolder = ""   -- folder name the palette list was built for

local SLIDE      = Animation.slide
local SLIDE_DIST = SLIDE.distance_px
local SLIDE_STEP = SLIDE.duration_s / SLIDE_DIST   -- counter sec per slide px
local bgpos  = SLIDE_DIST
local bgtlop = 0

-- ==== Resources ====

local textSmall = nil
local text      = nil
local textLarge = nil
local tx        = {}
local sounds    = {}
local ctx       = {}

-- ==== Layout ====

-- left panel
local MENU           = Layout.menu
local MENU_CX        = MENU.cx
local MENU_TITLE_X   = Layout.title.x
local MENU_TITLE_Y   = Layout.title.y
local MENU_ORIGIN_Y  = MENU.y
local MENU_SPACING_Y = MENU.spacing_y
local SCREEN_TITLE   = Layout.screen_title
local SCREEN_TITLE_Y = MENU_TITLE_Y + SCREEN_TITLE.below_title
local EMPTY_MAX_W    = Layout.empty_message_max_w
local DESC           = Layout.description
local NAME_SCREEN    = Layout.player_name
local TILE           = Layout.background_tile

-- OK / Cancel footer, as in mod_select_dialog
local FOOTER         = Layout.footer
local MENU_FOOTER_Y  = FOOTER.y
local MENU_OK_X      = FOOTER.ok_x
local MENU_CANCEL_X  = FOOTER.cancel_x

-- character / puchichara list: VISIBLE_COUNT slots around the selection
local HLIST         = Layout.horizontal_list
local ITEM_SLOT_W   = HLIST.slot_w
local VISIBLE_COUNT = HLIST.visible_count
local LIST_CX       = HLIST.cx
local LIST_CY       = HLIST.cy

-- nameplate / hitsound list, shared layout
local VLIST       = Layout.vertical_list
local NP_LIST_CX  = VLIST.cx
local NP_LIST_CY  = VLIST.cy
local NP_ROW_H    = VLIST.row_h
local NP_HALF_VIS = VLIST.half_visible
local NP_PLATE_W  = VLIST.plate_w
local DIM_ROW     = VLIST.unselected_opacity

local HS_LIST_CX  = NP_LIST_CX
local HS_LIST_CY  = NP_LIST_CY
local HS_ROW_H    = NP_ROW_H
local HS_HALF_VIS = NP_HALF_VIS

-- character bottom and nameplate top share PREVIEW_BASE_Y, as in game
local PREVIEW            = Layout.preview
local PREVIEW_BASE_Y     = PREVIEW.base_y         -- character "bottom" / nameplate top-left y
local PREVIEW_NP_X       = PREVIEW.nameplate_x    -- nameplate left-edge x
local PREVIEW_CHARA_X    = PREVIEW.character_x    -- character centre-x
local PREVIEW_PUCHI_X    = PREVIEW.puchichara_x   -- puchichara centre-x
local PREVIEW_PUCHI_BASE = PREVIEW.puchichara_y   -- puchichara centre-y

local FLOAT = Animation.puchichara_float

-- ==== Colors ====

local COL_WHITE  = Layout.colors.white_color
local COL_YELLOW = Layout.colors.yellow_color

-- ==== Texts ====

-- current language, read on activate
local strings = {}

local function loadStrings()
    strings.title          = tr("SONGSELECT_CUSTOMIZE_TITLE", "Customize")
    strings.character      = tr("SONGSELECT_CUSTOMIZE_CHARACTER", "Character")
    strings.palette        = tr("SONGSELECT_CUSTOMIZE_PALETTE", "Palette")
    strings.puchichara     = tr("SONGSELECT_CUSTOMIZE_PUCHICHARA", "Puchichara")
    strings.nameplateTitle = tr("SONGSELECT_CUSTOMIZE_NAMEPLATE_TITLE", "Nameplate title")
    strings.danTitle       = tr("SONGSELECT_CUSTOMIZE_DAN_TITLE", "Dan title")
    strings.playerName     = tr("SONGSELECT_CUSTOMIZE_PLAYER_NAME", "Player name")
    strings.hitsounds      = tr("SONGSELECT_CUSTOMIZE_HITSOUNDS", "Hitsounds")
    strings.desc           = tr("SONGSELECT_CUSTOMIZE_DESC", "Choose your character, puchichara, nameplate and hitsounds.")
    strings.ok             = tr("SONGSELECT_CONFIRM_OK", "OK")
    strings.cancel         = tr("SONGSELECT_CONFIRM_CANCEL", "Cancel")
    strings.paletteDefault = tr("SONGSELECT_CUSTOMIZE_PALETTE_DEFAULT", "Default")
    strings.paletteLock    = tr("SONGSELECT_CUSTOMIZE_PALETTE_LOCK", "Play %d more times with this character to unlock")
    strings.paletteLockOne = tr("SONGSELECT_CUSTOMIZE_PALETTE_LOCK_ONE", "Play %d more time with this character to unlock")
    strings.noCharacters   = tr("SONGSELECT_CUSTOMIZE_NO_CHARACTERS", "No characters available.")
    strings.noPuchichara   = tr("SONGSELECT_CUSTOMIZE_NO_PUCHICHARA", "No puchichara available.")
    strings.noDanTitles    = tr("SONGSELECT_CUSTOMIZE_NO_DAN_TITLES", "No dan titles available.")
    strings.noHitsounds    = tr("SONGSELECT_CUSTOMIZE_NO_HITSOUNDS", "No hitsound sets available.")
    strings.nameHint       = tr("SONGSELECT_CUSTOMIZE_NAME_HINT", "Enter: confirm   Esc: cancel")
    MAIN_LABELS = { strings.character, strings.palette, strings.puchichara, strings.nameplateTitle,
                    strings.danTitle, strings.playerName, strings.hitsounds }
end

-- ==== Helpers ====

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

-- ==== List builders ====

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

local function buildDanList()
    danList = {}
    for i = 0, save.DanTitleCount - 1 do
        local e = save:GetDanTitleByIndex(i)
        if e ~= nil then danList[#danList + 1] = e end
    end
end

-- ==== Palettes ====

local function disposePaletteGradients()
    if loadedCharaInst ~= nil then loadedCharaInst:ClearPaletteGradient() end
    paletteList = {}
end

-- JSON dictionary get, no throw
local function jsGet(dict, key, default)
    local v = JSONLOADER:JsonGet(dict, key)
    return v ~= nil and v or default
end

-- JSON stops -> Lua table for character:SetPaletteGradient
local function stopsDataToLuaTable(rawStops)
    local t = {}
    for j = 1, rawStops.Count do
        local s = rawStops[j]
        local alpha = JSONLOADER:JsonGet(s, 5)
        t[j] = { s[1], s[2], s[3], s[4] }
        if alpha ~= nil then t[j][5] = alpha end
    end
    return t
end

local function buildPaletteList()
    disposePaletteGradients()
    currentPaletteFolder = ""
    paletteList = { { name = strings.paletteDefault, plays = 0, blend = 0, luaStops = nil } }

    if loadedCharaInst == nil or not loadedCharaInst.IsValid then return end
    currentPaletteFolder = loadedCharaInst.FolderName

    local rawData = JSONLOADER:JsonParseFileAny(loadedCharaInst.FullPath .. "/Palettes.json")
    if rawData == nil then return end  -- no file: Default only

    paletteList = {}
    for i = 1, rawData.Count do
        local e        = rawData[i]
        local name     = tostring(jsGet(e, "name", "Palette " .. i))
        -- names: language id -> name
        local names    = jsGet(e, "names", nil)
        local own      = names ~= nil and JSONLOADER:JsonGet(names, LANG:GetLanguageId()) or nil
        if own ~= nil and tostring(own) ~= "" then name = tostring(own) end
        local plays    = math.floor(tonumber(tostring(jsGet(e, "plays", 0))) or 0)
        local blend    = tonumber(tostring(jsGet(e, "blend", 0))) or 0.0
        local rawStops = jsGet(e, "stops", nil)
        local luaStops = nil
        if rawStops ~= nil and rawStops.Count >= 2 then
            luaStops = stopsDataToLuaTable(rawStops)
        end
        paletteList[i] = { name = name, plays = plays, blend = blend, luaStops = luaStops }
    end
    if #paletteList == 0 then
        paletteList = { { name = strings.paletteDefault, plays = 0, blend = 0, luaStops = nil } }
    end
end

local function getCharPlayCount()
    if currentPaletteFolder == "" or save == nil then return 0 end
    return math.floor(save:GetGlobalCounter(".character_playcount_" .. currentPaletteFolder) or 0)
end

local function isPaletteUnlocked(palette)
    return palette.plays <= 0 or getCharPlayCount() >= palette.plays
end

-- read back with character:GetPaletteGradient()
local function applyPreviewPalette()
    if loadedCharaInst == nil then return end
    local p = paletteList[paletteSubIdx + 1]
    if p ~= nil and p.luaStops ~= nil then
        loadedCharaInst:SetPaletteGradient(p.luaStops, p.blend)
    else
        loadedCharaInst:ClearPaletteGradient()
    end
end

-- ==== Preview character ====
-- one owned instance (CHARACTER:CreateCharacter), apart from song_select's

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

    local newInst = CHARACTER:CreateCharacter(newEntry.FolderName)
    if newInst ~= nil then
        newInst:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)
    end

    disposePreviewCharacter()

    loadedCharaEntry = newEntry
    loadedCharaInst  = newInst

    buildPaletteList()
    if currentPaletteFolder ~= "" then
        local savedIdx = math.floor(save:GetGlobalCounter(".character_palette_" .. currentPaletteFolder) or 0)
        -- index past the list: default palette (as in game and story sprites)
        paletteSubIdx = (savedIdx >= 0 and savedIdx < #paletteList) and savedIdx or 0
    else
        paletteSubIdx = 0
    end
    applyPreviewPalette()
end

-- ==== Equipped item indices ====

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
    local id   = (info ~= nil) and info.Id or ""
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

local function findDanSubIdx()
    local currentDan = save.DanplateInfo.Title
    for i, e in ipairs(danList) do
        if e.Title == currentDan then return i - 1 end
    end
    return 0
end

-- ==== Apply on OK ====

local function applyChanges()
    if #characterList  > 0 then save:ChangeCharacter(characterList[charSubIdx + 1].FolderName) end
    if #puchiList      > 0 then save:ChangePuchichara(puchiList[puchiSubIdx + 1].FolderName)   end
    if #nameplateList  > 0 then save:ChangeNameplate(nameplateList[npSubIdx + 1].Id)            end
    if #danList        > 0 then save:ChangeDan(danList[danSubIdx + 1].Title)                    end
    if stagedPlayerName ~= "" then save:ChangeName(stagedPlayerName)                            end
    if #hitsoundList   > 0 then save.SelectedHitsounds = hitsoundList[hsSubIdx + 1].FolderName  end
    if currentPaletteFolder ~= "" then
        save:SetGlobalCounter(".character_palette_" .. currentPaletteFolder, paletteSubIdx)
    end
    -- also updates the persistent player LuaCharacter (ApplyPalette() path)
    local pBound = save:GetCharacter()
    if pBound ~= nil then
        local p = paletteList[paletteSubIdx + 1]
        if p ~= nil and p.luaStops ~= nil then
            pBound:SetPaletteGradient(p.luaStops, p.blend)
        else
            pBound:ClearPaletteGradient()
        end
    end
    -- .character_playcount_<folder>: incremented by the result stage
end

-- ==== Background ====

local function drawBg(opacity)
    tx["bgtile"]:SetOpacity((opacity * bgtlop) / 255)
    for i = 0, TILE.columns - 1 do
        for j = 0, TILE.rows - 1 do
            tx["bgtile"]:Draw(i * TILE.step_x, j * TILE.step_y)
        end
    end
end

-- ==== Preview ====

-- nameplate with every staged value
local function drawAllStagedNameplate(x, y, opacity)
    local np      = ROACTIVITY:GetROActivity("nameplate")
    local npEntry = #nameplateList > 0 and nameplateList[npSubIdx + 1] or nil
    local danEntry = #danList      > 0 and danList[danSubIdx + 1]      or nil
    if npEntry ~= nil then
        np:Draw(6, x, y, opacity, player, 0,
            npEntry.Title, npEntry.Type, npEntry.RarityInt, npEntry.Id,
            danEntry ~= nil and danEntry.Title       or nil,
            danEntry ~= nil and danEntry.ClearStatus or nil,
            stagedPlayerName)
    else
        NAMEPLATE:DrawPlayerNameplate(x, y, opacity, player)
    end
end

local function drawPreview(alpha)
    local opacity = math.floor(alpha * 255)

    if loadedCharaInst ~= nil and loadedCharaInst.IsValid then
        loadedCharaInst:SetScale(1, 1)
        loadedCharaInst:Update(CHARACTER.ANIM_MENU_NORMAL, true)
        loadedCharaInst:DrawAtAnchor(
            PREVIEW_CHARA_X, PREVIEW_BASE_Y,
            CHARACTER.ANIM_MENU_NORMAL, "bottom",
            1, 1, opacity
        )
    end

    drawAllStagedNameplate(PREVIEW_NP_X, PREVIEW_BASE_Y, opacity)

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

-- ==== Horizontal lists ====

local function drawHorizList(list, selectedIdx, alpha, itemDrawFn)
    if #list == 0 then return end
    local half = math.floor(VISIBLE_COUNT / 2)
    for slot = -half, half do
        local dataIdx = modWrap(selectedIdx + slot, #list)
        local cx      = LIST_CX + slot * ITEM_SLOT_W
        itemDrawFn(list[dataIdx + 1], cx, LIST_CY, slot == 0, alpha)
    end
end

-- centre slot: owned preview animation
local function drawCharItem(entry, cx, cy, isSelected, alpha)
    if entry == nil then return end
    if isSelected
        and loadedCharaInst ~= nil
        and loadedCharaEntry ~= nil
        and loadedCharaEntry.FolderName == entry.FolderName
        and loadedCharaInst.IsValid
    then
        -- updated this frame in drawPreview; draw only
        loadedCharaInst:SetScale(HLIST.character_selected_scale, HLIST.character_selected_scale)
        loadedCharaInst:DrawAtAnchor(
            cx, cy, CHARACTER.ANIM_MENU_NORMAL, "bottom",
            1, 1, math.floor(alpha * 255)
        )
        loadedCharaInst:SetScale(1, 1)
    end
    local col   = isSelected and COLOR:CreateColorFromHex(COL_YELLOW) or COLOR:CreateColorFromHex(COL_WHITE)
    local namTx = textSmall:GetText(entry.DisplayName, false, ITEM_SLOT_W - HLIST.name_padding, col)
    namTx:SetOpacity(alpha)
    namTx:DrawAtAnchor(cx, cy + HLIST.name_y, "top")
end

-- first sprite-sheet frame
local function drawPuchiItem(entry, cx, cy, isSelected, alpha)
    if entry == nil then return end
    if entry.tx ~= nil and entry.tx.Loaded then
        local frameW = math.floor(entry.tx.Width / 2)
        local frameH = entry.tx.Height
        local scale  = isSelected and HLIST.puchichara_selected_scale or 1.0
        entry.tx:SetScale(scale, scale)
        entry.tx:SetOpacity(alpha)
        entry.tx:DrawRectAtAnchor(cx, cy, 0, 0, frameW, frameH, "bottom")
        entry.tx:SetOpacity(1)
        entry.tx:SetScale(1, 1)
    end
    local col   = isSelected and COLOR:CreateColorFromHex(COL_YELLOW) or COLOR:CreateColorFromHex(COL_WHITE)
    local namTx = textSmall:GetText(entry.Name, false, ITEM_SLOT_W - HLIST.name_padding, col)
    namTx:SetOpacity(alpha)
    namTx:DrawAtAnchor(cx, cy + HLIST.name_y, "top")
end

-- ==== Hitsound preview ====

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

-- names only, nameplate list layout
local function drawHitsoundList(alpha)
    if #hitsoundList == 0 then return end
    for slot = -HS_HALF_VIS, HS_HALF_VIS do
        local dataIdx    = modWrap(hsSubIdx + slot, #hitsoundList)
        local entry      = hitsoundList[dataIdx + 1]
        if entry ~= nil then
            local rowY       = HS_LIST_CY + slot * HS_ROW_H
            local isSelected = (slot == 0)
            local opacity    = isSelected and alpha or alpha * DIM_ROW
            local col        = isSelected and COLOR:CreateColorFromHex(COL_YELLOW) or COLOR:CreateColorFromHex(COL_WHITE)
            local namTx      = text:GetText(entry.DisplayName, false, VLIST.text_max_w, col)
            namTx:SetOpacity(opacity)
            namTx:DrawAtAnchor(HS_LIST_CX, rowY, "center")
        end
    end
end

-- each row: its dan plus the other staged values
local function drawDanTitleList(alpha)
    if #danList == 0 then return end
    local npEntry = #nameplateList > 0 and nameplateList[npSubIdx + 1] or nil
    local np = ROACTIVITY:GetROActivity("nameplate")
    for slot = -NP_HALF_VIS, NP_HALF_VIS do
        local dataIdx    = modWrap(danSubIdx + slot, #danList)
        local entry      = danList[dataIdx + 1]
        if entry ~= nil then
            local rowY    = NP_LIST_CY + slot * NP_ROW_H
            local opacity = math.floor(((slot == 0) and alpha or alpha * DIM_ROW) * 255)
            local nx      = NP_LIST_CX - math.floor(NP_PLATE_W / 2)
            if npEntry ~= nil then
                np:Draw(6, nx, rowY + VLIST.plate_y, opacity, player, 0,
                    npEntry.Title, npEntry.Type, npEntry.RarityInt, npEntry.Id,
                    entry.Title, entry.ClearStatus, stagedPlayerName)
            else
                np:Draw(4, nx, rowY + VLIST.plate_y, opacity, player, 0,
                    entry.Title, entry.ClearStatus, stagedPlayerName)
            end
        end
    end
end

-- each row: its title plus the other staged values
local function drawNameplateList(alpha)
    if #nameplateList == 0 then return end
    local danEntry = #danList > 0 and danList[danSubIdx + 1] or nil
    local np = ROACTIVITY:GetROActivity("nameplate")
    for slot = -NP_HALF_VIS, NP_HALF_VIS do
        local dataIdx    = modWrap(npSubIdx + slot, #nameplateList)
        local entry      = nameplateList[dataIdx + 1]
        if entry ~= nil then
            local rowY    = NP_LIST_CY + slot * NP_ROW_H
            local opacity = math.floor(((slot == 0) and alpha or alpha * DIM_ROW) * 255)
            local nx      = NP_LIST_CX - math.floor(NP_PLATE_W / 2)
            np:Draw(6, nx, rowY + VLIST.plate_y, opacity, player, 0,
                entry.Title, entry.Type, entry.RarityInt, entry.Id,
                danEntry ~= nil and danEntry.Title       or nil,
                danEntry ~= nil and danEntry.ClearStatus or nil,
                stagedPlayerName)
        end
    end
end

-- ==== Counters ====

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
    local op = 255 - (val * (255 / SLIDE.fade_distance_px))
    bgtlop = math.max(0, math.min(255, op))
end

-- ==== draw ====

function draw()
    drawBg(TILE.opacity)
    tx["bg"]:SetOpacity(bgtlop / 255)
    tx["bg"]:Draw(0, bgpos)
    tx["player" .. player]:SetOpacity(bgtlop / 255)
    tx["player" .. player]:DrawAtAnchor(Layout.player_art.x, Layout.player_art.y + bgpos, "bottomright")

    if bgtlop == 0 then return end
    local alpha = bgtlop / 255

    drawPreview(alpha)

    local titleTx = textLarge:GetText(strings.title, false, Layout.title.max_w)
    titleTx:SetOpacity(alpha)
    titleTx:Draw(MENU_TITLE_X, MENU_TITLE_Y)

    if currentScreen == "main" then
        for i = 1, #MAIN_LABELS do
            local isSel = (i == mainIdx)
            local col   = isSel and COLOR:CreateColorFromHex(COL_YELLOW) or COLOR:CreateColorFromHex(COL_WHITE)
            local item  = text:GetText(MAIN_LABELS[i], false, MENU.item_max_w, col)
            item:SetOpacity(alpha)
            item:DrawAtAnchor(MENU_CX, MENU_ORIGIN_Y + (i - 1) * MENU_SPACING_Y, "top")
        end

        local okCol  = (mainIdx == OK_IDX)     and COLOR:CreateColorFromHex(COL_YELLOW) or COLOR:CreateColorFromHex(COL_WHITE)
        local canCol = (mainIdx == CANCEL_IDX) and COLOR:CreateColorFromHex(COL_YELLOW) or COLOR:CreateColorFromHex(COL_WHITE)
        local okTx  = text:GetText(strings.ok,     false, FOOTER.button_max_w, okCol)
        local canTx = text:GetText(strings.cancel, false, FOOTER.button_max_w, canCol)
        okTx:SetOpacity(alpha)  ; okTx:Draw(MENU_OK_X,     MENU_FOOTER_Y)
        canTx:SetOpacity(alpha) ; canTx:Draw(MENU_CANCEL_X, MENU_FOOTER_Y)

        local desc = textSmall:GetText(strings.desc, false, DESC.max_w)
        desc:SetOpacity(alpha)
        desc:DrawAtAnchor(MENU_CX, DESC.y, "top")

    elseif currentScreen == "palette" then
        local ttl = text:GetText(strings.palette, false, SCREEN_TITLE.max_w, COLOR:CreateColorFromHex(COL_YELLOW))
        ttl:SetOpacity(alpha) ; ttl:DrawAtAnchor(MENU_CX, SCREEN_TITLE_Y, "top")

        local playCount          = getCharPlayCount()
        local selUnlocked        = true
        local selRemaining       = 0
        for slot = -NP_HALF_VIS, NP_HALF_VIS do
            local dataIdx = modWrap(paletteSubIdx + slot, #paletteList)
            local p       = paletteList[dataIdx + 1]
            if p ~= nil then
                local rowY     = NP_LIST_CY + slot * NP_ROW_H
                local isSel    = (slot == 0)
                local unlocked = isPaletteUnlocked(p)
                if isSel then
                    selUnlocked  = unlocked
                    selRemaining = unlocked and 0 or ((p.plays or 0) - playCount)
                end
                local rowAlpha = isSel and alpha or alpha * DIM_ROW
                local col      = isSel and COLOR:CreateColorFromHex(COL_YELLOW) or COLOR:CreateColorFromHex(COL_WHITE)
                local namTx    = text:GetText(p.name, false, VLIST.text_max_w, col)
                namTx:SetOpacity(rowAlpha)
                namTx:DrawAtAnchor(MENU_CX, rowY, "center")
            end
        end

        if not selUnlocked then
            local lock = Layout.palette_lock
            local msgCol = palFlashColor or COLOR:CreateColorFromHex(lock.color)
            local pattern = selRemaining == 1 and strings.paletteLockOne or strings.paletteLock
            local msg = textSmall:GetText(string.format(pattern, selRemaining), false, lock.max_w, msgCol)
            msg:SetOpacity(alpha)
            msg:DrawAtAnchor(MENU_CX, NP_LIST_CY + NP_HALF_VIS * NP_ROW_H + lock.y, "top")
        end

    elseif currentScreen == "character" then
        local ttl = text:GetText(strings.character, false, SCREEN_TITLE.max_w, COLOR:CreateColorFromHex(COL_YELLOW))
        ttl:SetOpacity(alpha) ; ttl:DrawAtAnchor(MENU_CX, SCREEN_TITLE_Y, "top")

        if #characterList == 0 then
            local e = textSmall:GetText(strings.noCharacters, false, EMPTY_MAX_W)
            e:SetOpacity(alpha) ; e:DrawAtAnchor(MENU_CX, LIST_CY, "center")
        else
            drawHorizList(characterList, charSubIdx, alpha, drawCharItem)
        end

    elseif currentScreen == "puchichara" then
        local ttl = text:GetText(strings.puchichara, false, SCREEN_TITLE.max_w, COLOR:CreateColorFromHex(COL_YELLOW))
        ttl:SetOpacity(alpha) ; ttl:DrawAtAnchor(MENU_CX, SCREEN_TITLE_Y, "top")

        if #puchiList == 0 then
            local e = textSmall:GetText(strings.noPuchichara, false, EMPTY_MAX_W)
            e:SetOpacity(alpha) ; e:DrawAtAnchor(MENU_CX, LIST_CY, "center")
        else
            drawHorizList(puchiList, puchiSubIdx, alpha, drawPuchiItem)
        end

    elseif currentScreen == "nameplate_title" then
        local ttl = text:GetText(strings.nameplateTitle, false, SCREEN_TITLE.max_w, COLOR:CreateColorFromHex(COL_YELLOW))
        ttl:SetOpacity(alpha) ; ttl:DrawAtAnchor(MENU_CX, SCREEN_TITLE_Y, "top")

        drawNameplateList(alpha)

    elseif currentScreen == "dan_title" then
        local ttl = text:GetText(strings.danTitle, false, SCREEN_TITLE.max_w, COLOR:CreateColorFromHex(COL_YELLOW))
        ttl:SetOpacity(alpha) ; ttl:DrawAtAnchor(MENU_CX, SCREEN_TITLE_Y, "top")

        if #danList == 0 then
            local e = textSmall:GetText(strings.noDanTitles, false, EMPTY_MAX_W)
            e:SetOpacity(alpha) ; e:DrawAtAnchor(MENU_CX, NP_LIST_CY, "center")
        else
            drawDanTitleList(alpha)
        end

    elseif currentScreen == "player_name" then
        local ttl = text:GetText(strings.playerName, false, SCREEN_TITLE.max_w, COLOR:CreateColorFromHex(COL_YELLOW))
        ttl:SetOpacity(alpha) ; ttl:DrawAtAnchor(MENU_CX, SCREEN_TITLE_Y, "top")

        local displayText = playerNameInput ~= nil and playerNameInput.DisplayText or stagedPlayerName
        local nameTx = text:GetText(displayText, false, NAME_SCREEN.max_w)
        nameTx:SetOpacity(alpha) ; nameTx:DrawAtAnchor(MENU_CX, NAME_SCREEN.y, "center")

        local hint = textSmall:GetText(strings.nameHint, false, NAME_SCREEN.hint_max_w)
        hint:SetOpacity(alpha) ; hint:DrawAtAnchor(MENU_CX, NAME_SCREEN.hint_y, "top")

    elseif currentScreen == "hitsounds" then
        local ttl = text:GetText(strings.hitsounds, false, SCREEN_TITLE.max_w, COLOR:CreateColorFromHex(COL_YELLOW))
        ttl:SetOpacity(alpha) ; ttl:DrawAtAnchor(MENU_CX, SCREEN_TITLE_Y, "top")

        if #hitsoundList == 0 then
            local e = textSmall:GetText(strings.noHitsounds, false, EMPTY_MAX_W)
            e:SetOpacity(alpha) ; e:DrawAtAnchor(MENU_CX, HS_LIST_CY, "center")
        else
            drawHitsoundList(alpha)
        end

    end
end

-- ==== update ====

function update()
    for _, c in pairs(ctx) do c:Tick() end
    if not reactive then return end

    local navPn = NavInput.p[player + 1]

    local function exitDialog(save_changes)
        reactive = false
        if save_changes then applyChanges() end
        startCounter("exit", 0, SLIDE_DIST, SLIDE_STEP, "none", updateTransitionVisuals, function()
            DEACTIVATE()
        end)
    end

    if currentScreen == "main" then
        if navPn.downOrPadRight() then
            sounds.Skip:Play()
            mainIdx = (mainIdx % MAIN_COUNT) + 1
        elseif navPn.upOrPadLeft() then
            sounds.Skip:Play()
            mainIdx = ((mainIdx - 2 + MAIN_COUNT) % MAIN_COUNT) + 1
        elseif navPn.decide() then
            if     mainIdx == OK_IDX     then sounds.Decide:Play() ; exitDialog(true)
            elseif mainIdx == CANCEL_IDX then sounds.Cancel:Play() ; exitDialog(false)
            elseif mainIdx == 1 then sounds.Decide:Play() ; savedCharSubIdx    = charSubIdx    ; currentScreen = "character"
            elseif mainIdx == 2 then sounds.Decide:Play() ; savedPaletteSubIdx = paletteSubIdx ; currentScreen = "palette"
            elseif mainIdx == 3 then sounds.Decide:Play() ; savedPuchiSubIdx   = puchiSubIdx   ; currentScreen = "puchichara"
            elseif mainIdx == 4 then sounds.Decide:Play() ; savedNpSubIdx      = npSubIdx      ; currentScreen = "nameplate_title"
            elseif mainIdx == 5 then sounds.Decide:Play() ; savedDanSubIdx     = danSubIdx     ; currentScreen = "dan_title"
            elseif mainIdx == 6 then
                sounds.Decide:Play()
                savedPlayerName = stagedPlayerName
                playerNameInput = INPUT:CreateTextInput(stagedPlayerName, 64)
                currentScreen   = "player_name"
            elseif mainIdx == 7 then sounds.Decide:Play() ; savedHsSubIdx = hsSubIdx ; currentScreen = "hitsounds"
            end
        elseif navPn.cancel() then
            sounds.Cancel:Play()
            exitDialog(false)
        end

    elseif currentScreen == "character" then
        if navPn.right() and #characterList > 0 then
            sounds.Skip:Play()
            local newIdx = modWrap(charSubIdx + 1, #characterList)
            swapPreviewCharacter(characterList[newIdx + 1])
            charSubIdx = newIdx
        elseif navPn.left() and #characterList > 0 then
            sounds.Skip:Play()
            local newIdx = modWrap(charSubIdx - 1, #characterList)
            swapPreviewCharacter(characterList[newIdx + 1])
            charSubIdx = newIdx
        elseif navPn.decide() then
            sounds.Decide:Play() ; currentScreen = "main"
        elseif navPn.cancel() then
            sounds.Cancel:Play()
            charSubIdx = savedCharSubIdx
            swapPreviewCharacter(characterList[charSubIdx + 1])
            currentScreen = "main"
        end

    elseif currentScreen == "palette" then
        if navPn.downOrPadRight() and #paletteList > 0 then
            sounds.Skip:Play()
            paletteSubIdx = modWrap(paletteSubIdx + 1, #paletteList)
            applyPreviewPalette()
        elseif navPn.upOrPadLeft() and #paletteList > 0 then
            sounds.Skip:Play()
            paletteSubIdx = modWrap(paletteSubIdx - 1, #paletteList)
            applyPreviewPalette()
        elseif navPn.decide() then
            local p = paletteList[paletteSubIdx + 1]
            if p ~= nil and not isPaletteUnlocked(p) then
                startCounter("palFlash", 0, 255, Animation.palette_lock_flash.duration_s / 255, "none", function(val)
                    palFlashColor = COLOR:CreateColorFromARGB(255, 255, math.floor(val), math.floor(val))
                end, function()
                    palFlashColor = nil
                end)
            else
                sounds.Decide:Play() ; currentScreen = "main"
            end
        elseif navPn.cancel() then
            sounds.Cancel:Play()
            paletteSubIdx = savedPaletteSubIdx
            palFlashColor = nil
            applyPreviewPalette()
            currentScreen = "main"
        end

    elseif currentScreen == "puchichara" then
        if navPn.right() and #puchiList > 0 then
            sounds.Skip:Play() ; puchiSubIdx = modWrap(puchiSubIdx + 1, #puchiList)
        elseif navPn.left() and #puchiList > 0 then
            sounds.Skip:Play() ; puchiSubIdx = modWrap(puchiSubIdx - 1, #puchiList)
        elseif navPn.decide() then
            sounds.Decide:Play() ; currentScreen = "main"
        elseif navPn.cancel() then
            sounds.Cancel:Play() ; puchiSubIdx = savedPuchiSubIdx ; currentScreen = "main"
        end

    elseif currentScreen == "nameplate_title" then
        if navPn.downOrPadRight() and #nameplateList > 0 then
            sounds.Skip:Play() ; npSubIdx = modWrap(npSubIdx + 1, #nameplateList)
        elseif navPn.upOrPadLeft() and #nameplateList > 0 then
            sounds.Skip:Play() ; npSubIdx = modWrap(npSubIdx - 1, #nameplateList)
        elseif navPn.decide() then
            sounds.Decide:Play() ; currentScreen = "main"
        elseif navPn.cancel() then
            sounds.Cancel:Play() ; npSubIdx = savedNpSubIdx ; currentScreen = "main"
        end

    elseif currentScreen == "dan_title" then
        if navPn.downOrPadRight() and #danList > 0 then
            sounds.Skip:Play() ; danSubIdx = modWrap(danSubIdx + 1, #danList)
        elseif navPn.upOrPadLeft() and #danList > 0 then
            sounds.Skip:Play() ; danSubIdx = modWrap(danSubIdx - 1, #danList)
        elseif navPn.decide() then
            sounds.Decide:Play() ; currentScreen = "main"
        elseif navPn.cancel() then
            sounds.Cancel:Play() ; danSubIdx = savedDanSubIdx ; currentScreen = "main"
        end

    elseif currentScreen == "player_name" then
        if playerNameInput ~= nil then
            local confirmed = playerNameInput:Update()
            stagedPlayerName = playerNameInput.Text
            if confirmed then
                sounds.Decide:Play()
                playerNameInput:Dispose() ; playerNameInput = nil
                currentScreen = "main"
            elseif INPUT:KeyboardPressed("Escape") then
                sounds.Cancel:Play()
                stagedPlayerName = savedPlayerName
                playerNameInput:Dispose() ; playerNameInput = nil
                currentScreen = "main"
            end
        end

    elseif currentScreen == "hitsounds" then
        if navPn.downOrPadRight() and #hitsoundList > 0 then
            sounds.Skip:Play() ; hsSubIdx = modWrap(hsSubIdx + 1, #hitsoundList) ; playHitsoundPreview()
        elseif navPn.upOrPadLeft() and #hitsoundList > 0 then
            sounds.Skip:Play() ; hsSubIdx = modWrap(hsSubIdx - 1, #hitsoundList) ; playHitsoundPreview()
        elseif navPn.decide() then
            sounds.Decide:Play() ; currentScreen = "main"
        elseif navPn.cancel() then
            sounds.Cancel:Play() ; hsSubIdx = savedHsSubIdx ; stopHitsoundPreview() ; currentScreen = "main"
        end
    end
end

-- ==== activate ====

function activate(pl)
    player = pl or 0
    save   = GetSaveFile(player)
    loadStrings()

    -- unlocked or unconditioned items only
    buildCharacterList()
    buildPuchiList()
    buildNameplateList()
    buildDanList()
    buildHitsoundList()

    -- cursors on the equipped items
    charSubIdx  = findCharSubIdx()
    puchiSubIdx = findPuchiSubIdx()
    npSubIdx    = findNpSubIdx()
    danSubIdx   = findDanSubIdx()
    hsSubIdx    = findHsSubIdx()

    stagedPlayerName = save.Name

    -- owned preview character, apart from song_select's
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

    -- palette list, saved index restored
    buildPaletteList()
    if currentPaletteFolder ~= "" then
        local savedIdx = math.floor(save:GetGlobalCounter(".character_palette_" .. currentPaletteFolder) or 0)
        paletteSubIdx = (savedIdx >= 0 and savedIdx < #paletteList) and savedIdx or 0
    else
        paletteSubIdx = 0
    end
    applyPreviewPalette()

    currentScreen = "main"
    mainIdx       = 1

    sounds.Skip   = SHARED:GetSharedSound("Skip")
    sounds.Decide = SHARED:GetSharedSound("Decide")
    sounds.Cancel = SHARED:GetSharedSound("Cancel")

    startCounter("enter", SLIDE_DIST, 0, -SLIDE_STEP, "none", updateTransitionVisuals, function()
        reactive = true
    end)

    startCounter("sine", 0, 360, 1 / FLOAT.speed_deg_per_s, "loop", function(val)
        sineY = math.sin(val * math.pi / 180) * FLOAT.amplitude_px
    end)
end

-- ==== deactivate ====

function deactivate()
    reactive = false

    stopHitsoundPreview()

    if playerNameInput ~= nil then playerNameInput:Dispose() ; playerNameInput = nil end

    -- before the character: gradients use its GPU textures
    disposePaletteGradients()
    paletteSubIdx        = 0
    palFlashColor        = nil
    currentPaletteFolder = ""

    disposePreviewCharacter()

    for k in pairs(ctx) do
        ctx[k] = COUNTER:EmptyCounter()
    end

    -- entries owned by the global lists; no dispose
    characterList = {}
    puchiList     = {}
    nameplateList = {}
    danList       = {}
    hitsoundList  = {}
end

-- ==== onStart / onDestroy ====

function onStart()
    textSmall = TEXT:Create(Layout.fonts.small_size)
    text      = TEXT:Create(Layout.fonts.text_size)
    textLarge = TEXT:Create(Layout.fonts.large_size)
    loadStrings()

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
