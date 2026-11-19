---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local, redundant-parameter
-- song_select_core page and input handling.

local Config    = require("Core.Config")
local SongTitle = require("Songs.Title")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local List      = Layout.song_list
local ListAnim  = Animation.song_list
local PopAnim   = Animation.preimage
local FolderAnim = Animation.folder_anim

local M = {}
local G   -- shared state injected by Script.lua

local PREVIEW_THROTTLE_MS  = Animation.preview.throttle_ms
local BOX_SCROLL_SECONDS   = ListAnim.scroll_s
local HOLD_DELAY_SECONDS   = ListAnim.hold_delay_s
local HOLD_REPEAT_SECONDS  = ListAnim.hold_repeat_s
local ROWS                 = List.rows_each_side   -- the page: the selected node and ROWS on each side
local TRANSITION_S         = Animation.screen_transition.duration_s
local JACKET_MAX_SIZE      = Layout.preimage.decode_max_size

-- jacket pop: scale PREIMAGE_POP_START -> 1 with overshoot
local PREIMAGE_POP_START = PopAnim.pop_start_scale
local PREIMAGE_POP_MS    = PopAnim.pop_duration_ms

-- called from the shared-texture onCreate; the counter's OUT/BACK easing gives the overshoot
-- the listener value (already eased) maps onto [pop_start .. 1]
local function startPreimagePop()
    G.preimagePopScale = PREIMAGE_POP_START
    local c = G.startCounter("preimage_pop", 0, 1, PREIMAGE_POP_MS / 1000, "none",
        function(t) G.preimagePopScale = PREIMAGE_POP_START + (1 - PREIMAGE_POP_START) * t end,
        function() G.preimagePopScale = 1 end)
    c:SetEasing(PopAnim.pop_easing[1], PopAnim.pop_easing[2])
end

-- ==== Media loaders ====

local function reloadPreimage(songNode)
    SHARED:ClearSharedTexture("preimage")
    if songNode.IsSong == true then
        G.startCounter("throttle_preimage", 0, PREVIEW_THROTTLE_MS, PREVIEW_THROTTLE_MS / 1000 / PREVIEW_THROTTLE_MS, "none", nil, function()
            -- skip unless still selected after the throttle window
            if G.songList == nil or G.songList:GetSelectedSongNode() ~= songNode then return end
            -- maxSize: jackets up to 3000x3000 (34 MB decoded) show at ~400 px
            -- onCreate: jacket swapped in, starts the pop
            local function onCreate(tex)
                tex:SetWrapMode("Border")
                if G.songList == nil or G.songList:GetSelectedSongNode() ~= songNode then return end
                startPreimagePop()
            end
            if songNode.HasPreimage then
                SHARED:SetSharedTextureUsingAbsolutePath("preimage", songNode.PreimagePath, { maxSize = JACKET_MAX_SIZE }, onCreate)
            else
                SHARED:SetSharedTexture("preimage", "Textures/preimage.png", { maxSize = JACKET_MAX_SIZE }, onCreate)
            end
        end)
    else
        G.ctx["throttle_preimage"] = COUNTER:EmptyCounter()
    end
end

local function playPreview(songNode)
    -- the online lobby's preview of this song is already playing: it goes on instead of starting over
    if G.activeConfig.songOnly and songNode.IsSong == true and SHARED:GetSharedString("presound_path") == songNode.AudioPath then
        local snd = SHARED:GetSharedSound("presound")
        if snd ~= nil and snd.Loaded and snd.IsPlaying then
            G.ctx["throttle_presound"] = COUNTER:EmptyCounter()
            local speed = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
            G.previewDurationMs   = snd:GetDurationMs()
            G.previewDemoStartRaw = songNode.DemoStart
            G.previewDemoStart    = math.floor(songNode.DemoStart / speed)
            G.previewFadeTarget   = 100
            G.previewWasPlaying   = true
            G.previewLoaded       = true
            return
        end
    end
    G.previewLoaded         = false
    G.previewDurationMs     = 0
    G.previewDemoStartRaw   = 0
    G.previewDemoStart      = 0
    G.previewWasPlaying     = false
    -- fade the playing preview out (Script.lua eases previewFadeVol to the target)
    -- the throttled new preview replaces it on load
    G.previewFadeTarget     = 0
    if songNode.IsSong == true then
        -- Suppress audio preview for GRAYED/BLURED locked songs
        if G.unlocks ~= nil and G.unlocks.suppressPreview(songNode) then
            G.ctx["throttle_presound"] = COUNTER:EmptyCounter()
            return
        end
        G.startCounter("throttle_presound", 0, PREVIEW_THROTTLE_MS, PREVIEW_THROTTLE_MS / 1000 / PREVIEW_THROTTLE_MS, "none", nil, function()
            -- skip unless still selected after the throttle window
            if G.songList == nil or G.songList:GetSelectedSongNode() ~= songNode then return end
            local demoStart = songNode.DemoStart
            SHARED:SetSharedString("presound_path", songNode.AudioPath)
            SHARED:SetSharedPreviewUsingAbsolutePath("presound", songNode.AudioPath, function(snd)
                -- async load: skip Play if the selection moved on
                -- (no Stop: the shared slot may already hold the new selection)
                if G.songList == nil or G.songList:GetSelectedSongNode() ~= songNode then return end
                local speed = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
                snd:SetSpeed(speed)
                snd:SetVolume(0)            -- start silent; Script.lua fades it in to full
                snd:Play()
                snd:SetTimestamp(math.floor(demoStart / speed))
                G.previewDurationMs   = snd:GetDurationMs()
                G.previewDemoStartRaw = demoStart
                G.previewDemoStart    = math.floor(demoStart / speed)
                G.previewFadeVol      = 0
                G.previewFadeTarget   = 100
                G.previewWasPlaying   = false   -- falling-edge loop starts fresh (see Script.lua update)
                G.previewLoaded       = true
            end)
        end)
    else
        G.ctx["throttle_presound"] = COUNTER:EmptyCounter()
    end
end

function M.init(g)
    G = g
    -- media reload for other modules
    G.reloadMediaForNode = function(node)
        reloadPreimage(node)
        playPreview(node)
    end
    G.navRefreshPage = M.refreshPage
end

-- ==== Page refresh ====
-- node draw data is read here once per selection change: NLua property reads allocate
-- the draw code reads only these plain-Lua caches

-- BarLevelFill tint: box colour lifted FILL_LIGHTEN toward white (white without one)
-- per selection: COLOR:Create allocates
local FILL_LIGHTEN = List.level_fill_lighten
local function brightenedFill(boxColor)
    local function up(c) return math.min(255, math.floor(c + (255 - c) * FILL_LIGHTEN)) end
    if boxColor == nil then return COLOR:CreateColorFromRGBA(255, 255, 255, 255) end
    return COLOR:CreateColorFromRGBA(up(boxColor.R), up(boxColor.G), up(boxColor.B), 255)
end

local function focusChart(songNode)
    if songNode.IsSong ~= true then return nil end
    local default = math.min(4, CONFIG:GetDefaultCourse(0))
    local chart   = songNode:GetChart(default)
    local i = 4
    while chart == nil and i >= 0 do chart = songNode:GetChart(i); i = i - 1 end
    return chart
end

-- vault lock icon key from the highest available difficulty's level
local function vaultLockKey(node)
    local c = nil
    for d = 4, 0, -1 do
        c = node:GetChart(d)
        if c ~= nil then break end
    end
    local lvl = c and c.Level or 0
    if lvl >= 3 then return "vault_lock2"
    elseif lvl >= 2 then return "vault_lock1"
    else return "vault_lock0" end
end

local function buildSlot(node, isSelected)
    local slot = { text = SongTitle.of(node), gold = isSelected }
    slot.isSong, slot.isFolder = node.IsSong == true, node.IsFolder == true
    slot.isRandom, slot.isReturn = node.IsRandom == true, node.IsReturn == true
    slot.genre = node.Genre
    if slot.isSong or slot.isFolder then
        slot.boxColor    = node.BoxColor
        slot.vaultLocked = slot.isSong and G.unlocks.isVaultLocked(node) or false
        slot.vaultFolder = slot.isFolder and G.unlocks.isVaultFolder(node) or false
        slot.isLocked    = slot.isSong and node.IsLocked == true or false
        slot.hi          = slot.isSong and G.unlocks.effectiveHiddenIndex(node) or 0
        if slot.vaultLocked then slot.vaultLockKey = vaultLockKey(node) end
        if slot.isLocked then
            slot.lockedBarOverride = slot.hi >= 1                       -- GRAYED/BLURED use bar_1
            slot.lockKey = slot.hi >= 1 and "lock_1" or "lock_0"
        end
        if slot.isSong then
            local saveId = GetSaveFile(G.highlightedPlayer).SaveId
            slot.fav = node.UniqueId ~= nil and G.favs ~= nil and G.favs.isFavorite(saveId, node.UniqueId) or false
            local chart = focusChart(node)
            if chart ~= nil then
                slot.level = { lv = chart.Level, diff = chart.DifficultyAsInt,
                               isPlus = chart.IsPlus == true, isVault = slot.genre == "Secret Vault",
                               fillColor = brightenedFill(slot.boxColor) }
                local info = chart:GetPlayerBestScore(G.highlightedPlayer)
                slot.barleft = { played = info.HasBeenPlayed, cs = info.ClearStatus, sr = info.ScoreRank }
            end
        end
    end
    return slot
end

-- song counts of a folder's whole tree: unlocked, and all but locked mythical ones
-- cached per folder node until the next activate
local function folderCounts(node)
    G.folderCountCache = G.folderCountCache or {}
    local c = G.folderCountCache[node]
    if c ~= nil then return c[1], c[2] end
    local open, total = 0, 0
    local function walk(n)
        local children = n.Children
        for i = 0, children.Count - 1 do
            local ch = children[i]
            if ch.IsSong then
                local locked = ch.IsLocked == true
                if not locked then open = open + 1 end
                if not (locked and ch.Rarity == "Mythical") then total = total + 1 end
            elseif ch.IsFolder then
                walk(ch)
            end
        end
    end
    walk(node)
    G.folderCountCache[node] = { open, total }
    return open, total
end

-- selected-row band in a folder: folder colour, darkened if light, lightened if dark
-- nil at the top level
local BAND_LIGHT_THRESHOLD = List.select_band_folder_light_threshold
local BAND_DARKEN          = List.select_band_folder_darken
local BAND_LIGHTEN         = List.select_band_folder_lighten
local function bandColorFor(folder)
    if folder == nil or folder.IsRoot then return nil end
    G.bandColorCache = G.bandColorCache or {}
    local cached = G.bandColorCache[folder]
    if cached ~= nil then return cached end
    local c = folder.BoxColor
    if c == nil then return nil end
    local r, g, b = c.R, c.G, c.B
    -- Rec. 601 luma
    if 0.299 * r + 0.587 * g + 0.114 * b > BAND_LIGHT_THRESHOLD then
        r, g, b = r * BAND_DARKEN, g * BAND_DARKEN, b * BAND_DARKEN
    else
        r, g, b = r + (255 - r) * BAND_LIGHTEN, g + (255 - g) * BAND_LIGHTEN, b + (255 - b) * BAND_LIGHTEN
    end
    cached = COLOR:CreateColorFromARGB(255, math.floor(r + 0.5), math.floor(g + 0.5), math.floor(b + 0.5))
    G.bandColorCache[folder] = cached
    return cached
end

-- selected-node info for the right panel / breadcrumb / preimage / selected-bar visuals
local function buildSelInfo(node)
    local sel = {}
    sel.isSong, sel.isRandom = node.IsSong == true, node.IsRandom == true
    sel.isFolder = node.IsFolder == true
    sel.crumbs = {}
    local cur = node.Parent
    while cur ~= nil do
        sel.crumbs[#sel.crumbs + 1] = cur.Title ~= nil and cur.Title or "/"
        cur = cur.Parent
    end
    if sel.isFolder then
        sel.vaultFolder = G.unlocks.isVaultFolder(node)
        sel.folderName = node.Title
        sel.folderColor = brightenedFill(node.BoxColor)
        sel.folderLines = {}
        for line in (node.BoxText or ""):gmatch("[^\n]+") do sel.folderLines[#sel.folderLines + 1] = line end
        local open, total = folderCounts(node)
        sel.folderCount = string.format("%d / %d", open, total)
    end
    if not sel.isSong then return sel end
    sel.hi = G.unlocks.effectiveHiddenIndex(node)
    sel.hasVideo, sel.explicit = node.HasVideo == true, node.Explicit == true
    sel.isVault  = node.Genre == "Secret Vault"
    sel.subtitle = SongTitle.subtitleOf(node)
    sel.charter  = string.format(G.skinText("SONGSELECT_INFO_CHART", "Chart - %s"), node.Maker)
    sel.diffs = {}
    for i = 0, 4 do
        local c = node:GetChart(i)
        if c ~= nil then sel.diffs[i] = { level = c.Level, isPlus = c.IsPlus == true } end
    end
    local fc = focusChart(node)
    if fc ~= nil then sel.bpmBase, sel.bpmMin, sel.bpmMax = fc.BaseBPM, fc.MinBPM, fc.MaxBPM end
    local vaultLocked = G.unlocks.isVaultLocked(node)
    sel.isUnlockedSong = node.IsLocked ~= true and not vaultLocked
    -- BLURRED (vault songs excepted): texts as ???, static jacket
    sel.blurredInfo = sel.hi == 2 and node.IsLocked == true and not vaultLocked
    return sel
end

function M.refreshPage(skipMedia)
    G.currentPage = {}
    G.pageTexts   = {}
    G.selInfo     = nil
    G.unlocks.invalidateCondCache()   -- selection changed: recompute the unlock-condition panel cache

    for i = -ROWS, ROWS do
        local node = G.songList:GetSongNodeAtOffset(i)
        G.currentPage[i] = node
        if node == nil then
            G.pageTexts[i] = nil
        else
            G.pageTexts[i] = buildSlot(node, i == 0)
            if i == 0 then
                G.selInfo = buildSelInfo(node)
                G.selectBandColor = bandColorFor(node.Parent)
            end
            -- selected genre picks the background; other rows prefetch theirs
            if i == 0 then G.backgrounds.select(node.Genre) else G.backgrounds.prefetch(node.Genre) end
            if G.genre_overlays[node.Genre] == nil then
                if TEXTURE:Exists("Textures/Overlay/"..node.Genre..".png") then
                    G.genre_overlays[node.Genre] = TEXTURE:CreateTexture("Textures/Overlay/"..node.Genre..".png")
                else
                    G.genre_overlays[node.Genre] = TEXTURE:CreateTexture()
                end
            end
        end

        if i == 0 and node ~= nil and not skipMedia then
            reloadPreimage(node)
            playPreview(node)
        end
    end

    -- focused child per folder, restored on reopen
    -- keyed by node (stable cached wrappers; boxes have no UniqueId); any node type, back box included
    local sel = G.currentPage[0]
    if sel ~= nil and sel.Parent ~= nil then
        G.folderReopenMemory = G.folderReopenMemory or {}
        G.folderReopenMemory[sel.Parent] = sel
    end

    if G.ctx["extreme_fade"] then G.ctx["extreme_fade"]:Start() end
end

-- ==== Folder open/close animation ====
-- page bars as { {i, pt}, ... }, outer first (centre bar on top)
local function snapshotBars(excludeZero)
    local arr = {}
    for i = -ROWS, ROWS do
        local pt = G.pageTexts[i]
        if pt ~= nil and (not excludeZero or i ~= 0) then
            arr[#arr + 1] = { i = i, pt = pt }
        end
    end
    table.sort(arr, function(a, b) return math.abs(a.i) > math.abs(b.i) end)
    return arr
end

local function animTick(v) if G.folderAnim ~= nil then G.folderAnim.t = v end end
local function finishFolderAnim() G.folderAnim = nil end   -- normal list draw resumes from the settled page

-- breadcrumb titles (G.selInfo.crumbs) before an open/close, for the header animation
local function copyCrumbs()
    local c = {}
    if G.selInfo ~= nil and G.selInfo.crumbs ~= nil then
        for i, title in ipairs(G.selInfo.crumbs) do c[i] = title end
    end
    return c
end

-- Phase durations (seconds) and easings
local ANIM_OPEN_OUT_S  = FolderAnim.open_out_s     -- phase 1: parent siblings out
local ANIM_OPEN_FAN_S  = FolderAnim.open_fan_s     -- phase 2: folder contents in
local ANIM_CLOSE_GRP_S = FolderAnim.close_group_s  -- phase 1: folder contents to the focus
local ANIM_CLOSE_IN_S  = FolderAnim.close_in_s     -- phase 2: parent siblings back in

-- phase: 0..1 counter eased by the engine (SetEasing); the draw side lerps
local function startPhase(dur, easing, onFinish)
    local c = G.startCounter("folder_anim", 0, 1, dur, "none", animTick, onFinish)
    c:SetEasing(easing[1], easing[2])   -- listener receives the eased value
end

-- after OpenFolder: back to the child focused at the last close (reference match)
-- no match (removed / re-enumerated): stays on the first child
local function restoreFolderFocus(folder)
    if G.folderReopenMemory == nil then return end
    local target = G.folderReopenMemory[folder]
    if target == nil then return end
    local limit = (folder.ChildrenCount or 0) + 2   -- children + the Return/Random bars in the page
    for k = 1, limit do
        local n = G.songList:GetSongNodeAtOffset(k)
        if n == nil then break end
        if n == target then G.songList:Move(k); return end
    end
end

-- folder open: parent list out (folder bar kept), then contents in
-- false when there is nothing to open (state untouched)
local function startOpenAnim(Sort)
    if G.folderAnim ~= nil then return true end
    local folder    = G.songList:GetSelectedSongNode()   -- the folder being opened
    local folderPt  = G.pageTexts[0]
    local oldBars   = snapshotBars(true)                 -- parent siblings (the folder stays put)
    local oldCrumbs = copyCrumbs()                       -- header path before opening (new path adds the folder)
    if not G.songList:OpenFolder() then return false end
    Sort.applySort()
    restoreFolderFocus(folder)                          -- return to the previously-focused child, if any
    M.refreshPage()                                     -- page = folder contents; starts the selection's media
    G.folderAnim = { mode = "open", phase = 1, t = 0, oldCrumbs = oldCrumbs,
                     oldBars = oldBars, newBars = snapshotBars(false), folderPt = folderPt }
    G.sounds.Decide:Play()
    startPhase(ANIM_OPEN_OUT_S, FolderAnim.open_out_easing, function()
        if G.folderAnim == nil then return end
        G.folderAnim.phase = 2; G.folderAnim.t = 0
        startPhase(ANIM_OPEN_FAN_S, FolderAnim.open_fan_easing, finishFolderAnim)
    end)
    return true
end

-- folder close: contents to the focus, then the parent list back in
-- virtual folders (favorites/search) close at once; false at the root (the caller exits)
local function startCloseAnim(Sort)
    if G.folderAnim ~= nil then return true end
    local child     = G.songList:GetSelectedSongNode()
    local folder    = child ~= nil and child.Parent or nil
    local oldBars   = snapshotBars(false)                 -- the folder's contents
    local oldCrumbs = copyCrumbs()                        -- header path before closing (new path drops the folder)
    if not G.songList:CloseFolder() then return false end
    Sort.applySort()
    M.refreshPage()
    G.sounds.Cancel:Play()
    -- a virtual close moves the cursor off the folder: no animation
    local sel = G.songList:GetSelectedSongNode()
    if not (folder ~= nil and sel ~= nil and sel.IsFolder == true and sel.Title == folder.Title) then
        return true
    end
    G.folderAnim = { mode = "close", phase = 1, t = 0, oldCrumbs = oldCrumbs,
                     oldBars = oldBars, newBars = snapshotBars(true), folderPt = G.pageTexts[0] }
    startPhase(ANIM_CLOSE_GRP_S, FolderAnim.close_group_easing, function()
        if G.folderAnim == nil then return end
        G.folderAnim.phase = 2; G.folderAnim.t = 0
        startPhase(ANIM_CLOSE_IN_S, FolderAnim.close_in_easing, finishFolderAnim)
    end)
    return true
end

-- ==== Folder navigation ====

local function handleDecideSongSelect(Sort)
    local ssn = G.songList:GetSelectedSongNode()

    if ssn.IsFolder == true then
        -- Block opening a locked vault folder (same feedback as a locked song)
        if G.unlocks ~= nil and G.unlocks.isVaultFolder(ssn) then
            G.unlocks.onDecideVaultFolder(G.highlightedPlayer, ssn)
            return nil
        end
        startOpenAnim(Sort)
    elseif ssn.IsReturn == true then
        startCloseAnim(Sort)
    elseif ssn.IsSong == true then
        if G.unlocks ~= nil and G.unlocks.isVaultLocked(ssn) then
            G.unlocks.onDecideVaultLocked(G.highlightedPlayer, ssn)
            return nil
        end
        if ssn.IsLocked and G.unlocks ~= nil then
            G.unlocks.onDecideLocked(G.highlightedPlayer, ssn)
            return nil
        end
        -- play decide sfx by caller
        return ssn
    elseif ssn.IsRandom == true then
        local rdNd = G.songList:GetRandomNodeInFolder(ssn, true, function(node)
            return G.unlocks == nil or not G.unlocks.isVaultLocked(node)
        end)
        -- play decide sfx by caller
        if rdNd ~= nil then return rdNd end
    end
    return nil
end

-- ==== Hold scroll ====

local startRepeat

local function stopHold()
    G.holdDir = 0
    G.ctx["hold_delay"]  = COUNTER:EmptyCounter()
    G.ctx["hold_repeat"] = COUNTER:EmptyCounter()
    startRepeat = function() end
end

local function doMove(dir)
    G.sounds.Skip:Play()
    G.songList:Move(dir)
    M.refreshPage()
    G.startCounter("scroll_box_anim", dir, 0, dir > 0 and -BOX_SCROLL_SECONDS or BOX_SCROLL_SECONDS, "none", function(val)
        G.selectBoxDist = val
    end)
end

local function startHold(dir)
    G.holdDir = dir
    G.ctx["hold_delay"]  = COUNTER:EmptyCounter()
    G.ctx["hold_repeat"] = COUNTER:EmptyCounter()
    startRepeat = function()
        G.startCounter("hold_repeat", 0, 1, HOLD_REPEAT_SECONDS, "none", nil, function()
            if G.holdDir ~= 0 then doMove(G.holdDir); startRepeat() end
        end)
    end
    G.startCounter("hold_delay", 0, 1, HOLD_DELAY_SECONDS, "none", nil, function()
        if G.holdDir ~= 0 then startRepeat() end
    end)
end

-- ==== Song-select input ====
-- returns "cancel" or nil; a chosen song sets G.selectedSongNode and starts screen_transition

-- Event Mode countdown over: hovered song, else random from the box, else from the library
local function forcedChoice()
    local ssn = G.songList:GetSelectedSongNode()
    if ssn ~= nil and ssn.IsSong and not ssn.IsLocked and (G.unlocks == nil or not G.unlocks.isVaultLocked(ssn)) then return ssn end
    local pred = function(node) return G.unlocks == nil or not G.unlocks.isVaultLocked(node) end
    local pick = nil
    if ssn ~= nil then pick = G.songList:GetRandomNodeInFolder(ssn, true, pred) end
    if pick == nil then
        local root = G.songList:GetRoot()
        if root ~= nil and (root.ChildrenCount or 0) > 0 then pick = G.songList:GetRandomNodeInFolder(root:Child(0), true, pred) end
    end
    return pick
end

function M.handleSongSelectInput(Sort, Diff)
    G.selectedSongNode = nil

    -- Lock all input while a folder open/close animation is playing.
    if G.folderAnim ~= nil then return nil end

    local SC = G.shortcuts

    if G.eventExpired then
        G.eventExpired = false
        stopHold()
        G.selectedSongNode = forcedChoice()
        if G.selectedSongNode == nil then return nil end
    end

    -- the shortcuts panel
    if SC.pressed("help") then
        stopHold()
        SC.open()
        return nil
    end

    -- Displayed course / set-up player
    if SC.pressed("course") then
        G.sounds.Skip:Play()
        CONFIG:SetDefaultCourse(0, (CONFIG:GetDefaultCourse(0) + 1) % 5)
        Sort.applySort(); M.refreshPage(true)
    end
    -- shortcuts.lua decides which shortcuts this select can use
    if SC.pressed("player") then
        G.sounds.Skip:Play()
        local prev = G.highlightedPlayer
        G.highlightedPlayer = (G.highlightedPlayer + 1) % CONFIG.PlayerCount
        if G.highlightedPlayer ~= prev then Sort.applySort(); M.refreshPage(true) end
    end
    for p = 1, math.min(2, CONFIG.PlayerCount), 1 do
        if SC.pressed("auto_p" .. p) then
            G.sounds.Decide:Play(); CONFIG:SetAutoStatus(p - 1, not CONFIG:GetAutoStatus(p - 1))
        end
    end

    local navPn = G.NavInput.p[G.highlightedPlayer + 1]

    -- Release hold if direction key lifted
    if G.holdDir ==  1 and not navPn.rightPressing() then stopHold()
    elseif G.holdDir == -1 and not navPn.leftPressing() then stopHold()
    end

    -- pointer navigation (one human player): one song per wheel notch, down = next
    local mouse = G.mouseAllowed()
    local _, wheel = INPUT:GetScrollDelta()
    if mouse and wheel ~= 0 and G.songList ~= nil then
        stopHold()
        for _ = 1, math.min(5, math.floor(math.abs(wheel) + 0.5)) do doMove(wheel < 0 and 1 or -1) end
    end

    -- a click on a bar: the selected bar decides, another bar becomes the selection; a click on a locked
    -- song's strip or card decides too
    local clickDecide = false
    if mouse and INPUT:MousePressed("Left") and INPUT:IsMouseInside() and G.songList ~= nil then
        local mx, my = INPUT:GetMouseXY()
        local slot = G.songListHit(mx, my)
        if G.unlocks ~= nil and G.unlocks.clickDetails(mx, my) then
            slot = nil
        elseif slot == 0 then
            clickDecide = true
        elseif slot ~= nil then
            stopHold()
            for _ = 1, math.abs(slot) do doMove(slot > 0 and 1 or -1) end
        end
    end

    -- Main navigation
    if SC.pressed("sort") then
        stopHold()
        local sd = G.act_inner["sort_search_dialog"]
        if sd ~= nil and not sd.IsActive then
            sd:Activate(G.highlightedPlayer)
        end
    elseif SC.pressed("search") then
        stopHold()
        local sd = G.act_inner["sort_search_dialog"]
        if sd ~= nil and not sd.IsActive and G.songList ~= nil then
            local ssn        = G.songList:GetSelectedSongNode()
            local baseFolder = (ssn ~= nil and not ssn.IsRoot) and ssn.Parent or G.songList:GetRoot()
            sd:Activate(G.highlightedPlayer, "search", baseFolder)
        end
    elseif navPn.right() and G.songList ~= nil then
        doMove(1); startHold(1)
    elseif navPn.left() and G.songList ~= nil then
        doMove(-1); startHold(-1)
    elseif G.selectedSongNode ~= nil then
        -- the countdown chose above
    elseif navPn.decide() or clickDecide then
        stopHold()
        G.selectedSongNode = handleDecideSongSelect(Sort)
    elseif navPn.cancel() then
        stopHold()
        if not startCloseAnim(Sort) then
            -- Event Mode keeps the player here unless the plays are unlimited
            if not G.event.canLeave() then G.sounds.Error:Play(); return nil end
            G.sounds.Cancel:Play()
            return "cancel"
        end
    end

    -- Favorites: toggle on selected song (song node, not locked, not vault-locked)
    if SC.pressed("favorite") and G.favs ~= nil then
        local ssn = G.songList ~= nil and G.songList:GetSelectedSongNode() or nil
        if ssn ~= nil and ssn.IsSong and not ssn.IsLocked
                and (G.unlocks == nil or not G.unlocks.isVaultLocked(ssn)) then
            local saveId = GetSaveFile(G.highlightedPlayer).SaveId
            G.favs.toggleFavorite(saveId, ssn.UniqueId)
            G.sounds.Decide:Play()
            M.refreshPage(true)   -- the fav icon is cached in the page slots
        end
    end

    -- Favorites folder: open snapshot virtual folder for the highlighted player
    if SC.pressed("favorites_folder") and G.favs ~= nil then
        G.favs.openFavoritesFolder(G.highlightedPlayer)
    end

    -- Song speed
    if SC.pressed("speed_down") then
        G.sounds.Skip:Play()
        CONFIG.SongSpeed = CONFIG.SongSpeed - 1
        local spd = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
        SHARED:GetSharedSound("presound"):SetSpeed(spd)
        if G.previewDemoStartRaw > 0 then G.previewDemoStart = math.floor(G.previewDemoStartRaw / spd) end
    end
    if SC.pressed("speed_up") then
        G.sounds.Skip:Play()
        CONFIG.SongSpeed = CONFIG.SongSpeed + 1
        local spd = CONFIG.SONGSPEED:ToActual(CONFIG.SongSpeed)
        SHARED:GetSharedSound("presound"):SetSpeed(spd)
        if G.previewDemoStartRaw > 0 then G.previewDemoStart = math.floor(G.previewDemoStartRaw / spd) end
    end

    -- Player count
    if SC.pressed("player_count") then
        G.sounds.Skip:Play()
        CONFIG.PlayerCount = 1 + (CONFIG.PlayerCount % 5)
    end

    -- Enforce constraints
    if G.activeConfig.lockedPlayerCount ~= nil then
        CONFIG.PlayerCount = G.activeConfig.lockedPlayerCount
    end
    if G.activeConfig.mountAISlotToP2 then G.highlightedPlayer = 0 end

    -- Song chosen → start transition to difficulty select
    if G.selectedSongNode ~= nil then
        if G.activeConfig.songOnly then
            -- online lobby: song only, no difficulty prompt (chosen per player in the lobby)
            -- sets the global chosen song and tells the parent to return
            stopHold()
            G.sounds.SongDecide:Play()
            pcall(function() G.selectedSongNode:Mount(0, 0, 0, 0, 0) end)
            G.lastSignal = "play"
            return "play"
        end
        G.sounds.Decide:Play()
        Diff.loadDiffBars(G.selectedSongNode)
        require("replaylist").prefetchForSong(G.selectedSongNode)   -- async; lists land while the panel opens
        stopHold()
        G.activeScreen   = "transition"
        G.diffIndex      = {0, 0, 0, 0, 0}
        G.diffSelected   = {false, false, false, false, false}
        -- Event Mode: the difficulty choice gets its own, shorter countdown; the cursor starts on the
        -- first difficulty since the back and customize bars are out of reach
        if G.eventCountdown and G.eventTimer ~= nil then G.eventTimer:Call("restart", G.event.DIFF_SECONDS) end
        if G.event.on() then
            for i = 1, 5 do G.diffIndex[i] = 3 end
        end
        G.startCounter("screen_transition", 0, 1920, TRANSITION_S / 1920, "none", Diff.updateTransitionVisuals, function()
            G.activeScreen = "difficultyselect"
        end)
    end

    return nil
end

return M
