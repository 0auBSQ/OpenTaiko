---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- song_select_core Activity
-- Core song-select logic shared by regular_song_select, ai_battle_song_select,
-- and training_song_select.
--
-- activate(config) parameters (all optional, nil = default):
--   allowPlayerCount  (bool)  true by default — allow L key to cycle player count
--   lockedPlayerCount (int)   nil by default  — forces CONFIG.PlayerCount to this value
--   mountAISlotToP2   (bool)  false by default — mounts the AI virtual slot onto spot 2
--
-- update() return values:
--   "play"   — a song was successfully mounted, parent stage should Exit("play")
--   "cancel" — user backed out entirely, parent stage should decide where to go
--   nil      — still running, no action needed

local Sort    = require("sort")
local Nav     = require("navigation")
local Diff    = require("diffselect")
local Replay  = require("replaylist")
local DrawSS  = require("draw_songselect")
local Search  = require("search")
local Unlocks = require("unlockables")
local Favs    = require("favorites")

-- ── Shared state (G) ─────────────────────────────────────────────────────────
-- All modules receive a reference to this table via their init() call.
-- Mutations through G are visible to every module immediately.

local G = {
    -- Fonts
    text = nil, textSmall = nil, textLarge = nil, textStats = nil,

    -- Sounds
    sounds = {},

    -- Song list
    songList = nil,
    currentPage = {}, pageTexts = {}, genre_overlays = {},

    -- Textures
    bars = {}, bgtx = {},
    favoriteicon = nil,
    portraits = {},

    -- Player
    highlightedPlayer = 0,

    -- Inner activities (dialogs)
    act_inner = {}, modicons_ro = nil,

    -- Counters
    ctx = {},

    -- Scroll / animation state
    currentBackground   = 0,
    backgroundScrollX   = 0,
    songSelectShift     = 0,
    songSelectElemOpacity      = 255,
    difficultySelectElemOpacity = 0,
    levelLabelFrame = 0,
    difficultyFade4 = 0,
    arrowsDistance  = 0,
    selectBoxDist   = 0,
    noteFloatPhase  = 0,

    -- Screen state
    activeScreen         = "songselect",
    songSelectModes      = { songselect = true, pretransition = true, transition = true },
    difficultySelectModes = { difficultyselect = true, transition = true },

    -- Dialog-close tracking
    wasCustomizeActive      = false,
    wasSortDialogActive     = false,
    wasConfirmDialogActive  = false,

    -- (search state is managed entirely by LuaSongList:OpenVirtualFolder / CloseFolder)

    -- Preview state
    puchiSineY          = 0,
    selectedSongNode    = nil,
    previewDemoStartRaw = 0,
    previewDemoStart    = 0,
    previewDurationMs   = 0,
    previewLoaded       = false,
    previewLoopCooldown = false,

    -- Difficulty select
    diffBars     = {},
    diffIndex    = {0, 0, 0, 0, 0},
    diffSelected = {false, false, false, false, false},

    -- Config / lifecycle
    activeConfig = {},
    lastSignal   = nil,

    -- Sort state
    originalOrders = {},

    -- Hold scroll
    holdDir = 0,

    -- Per-player input bindings
    inputSets = {
        { right = "RightChange", left = "LeftChange", decide1 = "Decide",  decide2 = "Decide",  cancel = "Cancel", auto = "ToggleAutoP1" },
        { right = "RBlue2P",     left = "LBlue2P",    decide1 = "RRed2P",  decide2 = "LRed2P",  cancel = nil,      auto = "ToggleAutoP2" },
        { right = "RBlue3P",     left = "LBlue3P",    decide1 = "RRed3P",  decide2 = "LRed3P",  cancel = nil,      auto = nil },
        { right = "RBlue4P",     left = "LBlue4P",    decide1 = "RRed4P",  decide2 = "LRed4P",  cancel = nil,      auto = nil },
        { right = "RBlue5P",     left = "LBlue5P",    decide1 = "RRed5P",  decide2 = "LRed5P",  cancel = nil,      auto = nil },
    },
}

-- ── Shared utility functions (stored in G so every module can call them) ──────

-- Counter helper — wraps COUNTER:CreateCounter and stores in G.ctx to prevent GC.
G.startCounter = function(key, startVal, endVal, interval, mode, updateCallback, onFinish)
    local c = COUNTER:CreateCounter(startVal, endVal, interval, onFinish)
    if mode == "loop"   then c:SetLoop(true)
    elseif mode == "bounce" then c:SetBounce(true) end
    if updateCallback then c:Listen(updateCallback) end
    c:Start()
    G.ctx[key] = c
    return c
end

-- Number rendering — used by draw_songselect and diffselect panels.
G.calculateNumberWidth = function(nb, txstr)
    local str = tostring(nb)
    local cursorX, prevWidth, lastWidth = 0, 0, 0
    for i = 1, #str do
        local digit = string.sub(str, i, i)
        local tex   = G.bgtx[txstr .. digit]
        if tex then
            local w = tex.Width
            if i > 1 then cursorX = cursorX + prevWidth * 0.5 end
            prevWidth = w; lastWidth = w
        end
    end
    return cursorX + lastWidth
end

G.drawNumberCentered = function(nb, txstr, x, y, color, opacity)
    local white   = COLOR:CreateColorFromHex("ffffffff")
    color   = color   or white
    opacity = opacity or 1
    local str        = tostring(nb)
    local totalWidth = G.calculateNumberWidth(nb, txstr)
    local cursorX    = x - totalWidth / 2
    local prevWidth  = 0
    for i = 1, #str do
        local digit = string.sub(str, i, i)
        local tex   = G.bgtx[txstr .. digit]
        if tex then
            local w = tex.Width
            if i > 1 then cursorX = cursorX + prevWidth * 0.5 end
            tex:SetColor(color); tex:SetOpacity(opacity)
            tex:DrawAtAnchor(cursorX + w / 2, y, "center")
            tex:SetOpacity(1); tex:SetColor(white)
            prevWidth = w
        end
    end
end

-- Character / nameplate drawing — shared by song select and difficulty select panels.
G.drawPlayerChara = function(player, x, y, scaleX, scaleY, opacity, flipX)
    local chara = GetSaveFile(player):GetCharacter()
    if chara ~= nil and chara.IsValid then
        chara:Update(CHARACTER.ANIM_MENU_NORMAL, true)
        local esx = (flipX or false) and -scaleX or scaleX
        chara:DrawAtAnchor(x, y, CHARACTER.ANIM_MENU_NORMAL, "bottom", esx, scaleY, math.floor(opacity * 255))
    end
end

G.drawPlayerPuchi = function(player, x, y, scaleX, scaleY, opacity)
    local puchi = GetSaveFile(player):GetPuchichara()
    if puchi == nil or puchi.tx == nil or not puchi.tx.Loaded then return end
    local frameW = math.floor(puchi.tx.Width / 2)
    local frameH = puchi.tx.Height
    puchi.tx:SetScale(scaleX, scaleY)
    puchi.tx:SetOpacity(opacity)
    puchi.tx:DrawRectAtAnchor(x, y, 0, 0, frameW, frameH, "bottom")
    puchi.tx:SetOpacity(1); puchi.tx:SetScale(1, 1)
end

G.drawCharaWithNameplate = function(player, x, y, scaleX, scaleY, opacity, flipX)
    local NAMEPLATE_OFFSET_X = 27
    G.drawPlayerChara(player, x + G.bgtx["nameplate_info"].Width / 2 - NAMEPLATE_OFFSET_X,
        y, scaleX, scaleY, opacity, flipX)
    NAMEPLATE:DrawPlayerNameplate(x, y, opacity * 255, player)
end

-- ── Module initialisation ─────────────────────────────────────────────────────

Sort.init(G)
Nav.init(G)
Diff.init(G)
DrawSS.init(G)
Search.init(G)
Unlocks.init(G)
Favs.init(G)

-- Expose applySort through G so other modules (e.g. search.lua) can call it
-- without needing a direct reference to Sort.
G.applySort = function() Sort.applySort() end

-- Expose Unlocks through G so draw_songselect and navigation can reach it.
G.unlocks = Unlocks

-- ── Lifecycle ─────────────────────────────────────────────────────────────────

function onStart()
    -- glyph-composed fonts: one texture per unique character (bounded), no per-string texture cache
    G.text      = TEXT:CreateGlyphCached(28)
    G.textSmall = TEXT:CreateGlyphCached(18)
    G.textLarge = TEXT:CreateGlyphCached(40)
    G.textStats = TEXT:CreateGlyphCached(24)

    SHARED:SetSharedTexture("background", "Textures/bg0.png")

    G.bgtx["load"]                      = TEXTURE:CreateTexture("Textures/load.png")
    G.bgtx["preimage_load"]             = TEXTURE:CreateTexture("Textures/preimage_load.png")
    G.bgtx["overlay"]                   = TEXTURE:CreateTexture("Textures/bg_overlay.png")
    G.bgtx["overlay_difficulty"]        = TEXTURE:CreateTexture("Textures/bg_overlay_difficulty.png")
    G.bgtx["songinfo"]                  = TEXTURE:CreateTexture("Textures/bg_songinfo.png")
    G.bgtx["randominfo"]                = TEXTURE:CreateTexture("Textures/bg_randominfo.png")
    G.bgtx["difficultyselect"]          = TEXTURE:CreateTexture("Textures/bg_difficultyselect.png")
    G.bgtx["header"]                    = TEXTURE:CreateTexture("Textures/bg_header.png")
    G.bgtx["header-box"]                = TEXTURE:CreateTexture("Textures/bg_header-box.png")
    G.bgtx["header-arrow"]              = TEXTURE:CreateTexture("Textures/bg_header-arrow.png")
    G.bgtx["nameplate_info"]            = TEXTURE:CreateTexture("Textures/nameplate_info.png")
    G.bgtx["sinfo_video"]               = TEXTURE:CreateTexture("Textures/sinfo_video.png")
    G.bgtx["sinfo_explicit"]            = TEXTURE:CreateTexture("Textures/sinfo_explicit.png")
    G.bgtx["sinfo_difficulties_missing"] = TEXTURE:CreateTexture("Textures/sinfo_difficulties_missing.png")
    G.bgtx["sinfo_difficulties_vault"]      = TEXTURE:CreateTexture("Textures/sinfo_difficulties_vault.png")
    G.bgtx["sinfo_difficulties_vault_plus"] = TEXTURE:CreateTexture("Textures/sinfo_difficulties_vault_plus.png")
    for i = 0, 4 do
        G.bgtx["sinfo_difficulties_" .. i]           = TEXTURE:CreateTexture("Textures/sinfo_difficulties_" .. i .. ".png")
        G.bgtx["sinfo_difficulties_" .. i .. "_plus"] = TEXTURE:CreateTexture("Textures/sinfo_difficulties_0_plus.png")
    end
    for i = 0, 9 do
        G.bgtx["levellabelsfill" .. i]   = TEXTURE:CreateTexture("Textures/BarLevelFill/" .. i .. ".png")
        G.bgtx["levellabels" .. i]       = TEXTURE:CreateTexture("Textures/BarLevel/" .. i .. ".png")
        G.bgtx["sinfo_level" .. i]       = TEXTURE:CreateTexture("Textures/SinfoLevel/" .. i .. ".png")
        G.bgtx["diffsel_levelcol" .. i]  = TEXTURE:CreateTexture("Textures/DifficultyBars/LevelCol/" .. i .. ".png")
    end
    G.bgtx["placeholder_chara"]   = TEXTURE:CreateTexture("Textures/placeholder_chara.png")
    G.bgtx["placeholder_portrait"] = TEXTURE:CreateTexture("Textures/placeholder_portrait.png")

    G.bars["bar"]              = TEXTURE:CreateTexture("Textures/bar.png")
    G.bars["random"]           = TEXTURE:CreateTexture("Textures/random.png")
    G.bars["back"]             = TEXTURE:CreateTexture("Textures/back.png")
    G.bars["locked"]           = TEXTURE:CreateTexture("Textures/locked.png")
    G.bars["selected"]         = TEXTURE:CreateTexture("Textures/selected.png")
    G.bars["selectedlarge"]    = TEXTURE:CreateTexture("Textures/selectedlarge.png")
    G.bars["selected-arrow-l"] = TEXTURE:CreateTexture("Textures/selected-arrow-l.png")
    G.bars["selected-arrow-r"] = TEXTURE:CreateTexture("Textures/selected-arrow-r.png")
    G.bars["levellabels"]        = TEXTURE:CreateTexture("Textures/bar_levelbg.png")
    G.bars["levellabelsplus"]    = TEXTURE:CreateTexture("Textures/bar_levelbgplus.png")
    G.bars["levellabelsvault"]   = TEXTURE:CreateTexture("Textures/bar_levelbgvault.png")
    G.bars["levellabelsplusvault"] = TEXTURE:CreateTexture("Textures/bar_levelbgplusvault.png")
    G.bars["levellabelsfire"]    = TEXTURE:CreateTexture("Textures/bar_levelbgfire.png")
    G.bars["levellabelsstorm"]   = TEXTURE:CreateTexture("Textures/bar_levelbgstorm.png")
    for i = 1, 5 do
        G.bars["difficultybarselect" .. i] = TEXTURE:CreateTexture("Textures/DifficultyBars/P" .. i .. ".png")
    end
    for i = 2, 7 do
        G.bars["difficultybar" .. i] = TEXTURE:CreateTexture("Textures/DifficultyBars/" .. i .. ".png")
    end
    -- Note.png: the frame all difficulty-select elements are laid out relative to (floats + rotates).
    G.bars["diffnote"] = TEXTURE:CreateTexture("Textures/DifficultyBars/Note.png")
    -- Option bars, in the on-screen order 0 / 1 / Customize (see diffselect.lua positions + actions).
    G.bars["smallbar0"] = TEXTURE:CreateTexture("Textures/DifficultyBars/0.png")
    G.bars["smallbar1"] = TEXTURE:CreateTexture("Textures/DifficultyBars/1.png")
    G.bars["smallbar2"] = TEXTURE:CreateTexture("Textures/DifficultyBars/Customize.png")
    -- Diff1~Diff7 (level-fill gauges) and the Level/ number folder are deprecated: only LevelCol is used now.

    Unlocks.loadTextures()

    G.bars["barleft"]          = TEXTURE:CreateTexture("Textures/barleft.png")
    G.bars["scorerank_none"]   = TEXTURE:CreateTexture("Textures/ScoreRank/None.png")
    G.bars["scorerank_m1"]     = TEXTURE:CreateTexture("Textures/ScoreRank/-1.png")
    for i = 0, 6 do
        G.bars["scorerank_" .. i] = TEXTURE:CreateTexture("Textures/ScoreRank/" .. i .. ".png")
    end
    G.bars["clearstatus_none"] = TEXTURE:CreateTexture("Textures/ClearStatus/None.png")
    G.bars["clearstatus_m1"]   = TEXTURE:CreateTexture("Textures/ClearStatus/-1.png")
    for i = 0, 3 do
        G.bars["clearstatus_" .. i] = TEXTURE:CreateTexture("Textures/ClearStatus/" .. i .. ".png")
    end

    G.favoriteicon  = TEXTURE:CreateTexture("Textures/fav.png")
    G.genre_overlays = {}

    Favs.loadDB()
    G.favs = Favs
end

function activate(allowPlayerCount, lockedPlayerCount, mountAISlotToP2, songOnly)
    G.activeConfig = {
        allowPlayerCount  = allowPlayerCount,
        lockedPlayerCount = lockedPlayerCount,
        mountAISlotToP2   = mountAISlotToP2 == true,
        songOnly          = songOnly == true,   -- online lobby: pick song only (no diff prompt), Auto disabled
    }

    if G.activeConfig.lockedPlayerCount ~= nil then
        CONFIG.PlayerCount = math.tointeger(G.activeConfig.lockedPlayerCount)
    end
    if G.activeConfig.mountAISlotToP2 then
        VIRTUALSLOTS:MountSlot(2, "AI")
    end

    local activities = {"mod_select_dialog", "customize_dialog", "sort_search_dialog", "confirm_dialog"}
    for _, at in ipairs(activities) do
        G.act_inner[at] = ACTIVITY:GetActivity(at)
    end

    G.modicons_ro = ROACTIVITY:GetROActivity("modicons")
    if G.modicons_ro ~= nil then G.modicons_ro:Activate() end

    G.sounds.Skip      = SHARED:GetSharedSound("Skip")
    G.sounds.Cancel    = SHARED:GetSharedSound("Cancel")
    G.sounds.Decide    = SHARED:GetSharedSound("Decide")
    G.sounds.SongDecide = SHARED:GetSharedSound("SongDecide")

    Diff.resetToSongSelect()
    Unlocks.invalidateCondCache()

    -- (search state is owned by LuaSongList; reloading the song list resets it)

    G.currentBackground = 0
    SHARED:SetSharedTexture("background", "Textures/bg0.png")

    if G.songList ~= nil then
        Sort.applySort()
        Nav.refreshPage()
    end

    -- Animation counters
    local PUCHI_FLOAT_AMP                  = 8
    local SONGBAR_LEVEL_EX_ANIMATION_FRAMECOUNT = 20

    G.startCounter("background", 1920, 0, 1/48, "loop", function(val)
        G.backgroundScrollX = val
    end)
    G.startCounter("extreme_fade", 2000, 0, 1/400, "loop", function(val)
        local fadeIn = val - 745; local fadeOut = 2000 - val
        G.difficultyFade4 = math.max(0, math.min(255, fadeIn, fadeOut))
    end)
    G.startCounter("selectbox_animation", 2000, 0, 1/600, "loop", function(val)
        local n = 1.01 + math.sin(val * (math.pi * 2 / 2000)) * 0.01
        if G.bars["selected"]      then G.bars["selected"]:SetScale(n, n)      end
        if G.bars["selectedlarge"] then G.bars["selectedlarge"]:SetScale(n, n) end
    end)
    G.startCounter("arrows_animation", 10, 0, 1/10, "bounce", function(val)
        G.arrowsDistance = val
    end)
    G.startCounter("leveltag_animation",
        SONGBAR_LEVEL_EX_ANIMATION_FRAMECOUNT, 0,
        1 / SONGBAR_LEVEL_EX_ANIMATION_FRAMECOUNT * 2, "loop", function(val)
        G.levelLabelFrame = math.floor(val)
    end)
    G.startCounter("load_animation", 0, 360, 2/300, "loop", function(val)
        if G.bgtx["load"] ~= nil then G.bgtx["load"]:SetRotation(val) end
    end)
    G.startCounter("puchi_sine", 0, 360, 1/120, "loop", function(val)
        G.puchiSineY = math.sin(val * math.pi / 180) * PUCHI_FLOAT_AMP
    end)
    -- Difficulty-select Note float/rotation phase (0..360, ~8s loop). Ticks in G.ctx every frame regardless of
    -- activeScreen, so the Note animates during the songselect→diffselect transition too. diffselect reads it.
    G.startCounter("diffnote_float", 0, 360, 1/45, "loop", function(val)
        G.noteFloatPhase = val
    end)

    G.portraits = {}
    for p = 0, 4 do
        local chara = GetSaveFile(p):GetCharacter()
        if chara ~= nil and chara.IsValid then
            chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)
            local portraitPath = chara.FullPath .. "/Portrait.png"
            G.portraits[p] = TEXTURE:CreateTextureFromAbsolutePath(portraitPath)
        end
    end
end

function deactivate()
    if G.activeConfig.mountAISlotToP2 and G.lastSignal ~= "play" then
        VIRTUALSLOTS:MountSlot(2, "2P")
    end
    G.lastSignal = nil

    for k in pairs(G.ctx) do G.ctx[k] = COUNTER:EmptyCounter() end

    SHARED:GetSharedSound("presound"):Stop()

    for p = 0, 4 do
        local chara = GetSaveFile(p):GetCharacter()
        if chara ~= nil and chara.IsValid then chara:DisposeAnimation(CHARACTER.ANIM_MENU_NORMAL) end
        if G.portraits[p] ~= nil then G.portraits[p]:Dispose(); G.portraits[p] = nil end
    end
    G.portraits = {}
end

function afterSongEnum()
    local lsls = GenerateSongListSettings()
    lsls:SetExcludedGenreFolders({"段位道場", "太鼓タワー"})
    lsls.ModuloPagination    = false
    lsls.HideEmptyFolders    = true
    lsls.FlattenOpenedFolders = false
    G.songList       = RequestSongList(lsls)
    G.originalOrders = {}
    Sort.applySort()
    Nav.refreshPage()
end

function onDestroy()
    Replay.dispose()
    if G.text      ~= nil then G.text:Dispose()      end
    if G.textSmall ~= nil then G.textSmall:Dispose()  end
    if G.textLarge ~= nil then G.textLarge:Dispose()  end
    if G.textStats ~= nil then G.textStats:Dispose()  end
    if G.favoriteicon ~= nil then G.favoriteicon:Dispose() end
    for _, bar     in pairs(G.bars)          do bar:Dispose()     end
    for _, bg      in pairs(G.bgtx)          do bg:Dispose()      end
    for _, overlay in pairs(G.genre_overlays) do overlay:Dispose() end
end

-- ── Draw ─────────────────────────────────────────────────────────────────────

local function drawWaitScreen(msg)
    -- Black fill
    local black = COLOR:CreateColorFromHex("ff000000")
    local white = COLOR:CreateColorFromHex("ffffffff")
    G.textLarge:Draw(msg, 960, 540, white, nil, 1, 1, 1600, "center")
end

function draw(mode)
    -- Loading / no-songs guard: black screen with status message.
    if IsSongsEnumerating() then
        drawWaitScreen("Loading songs, please wait...")
        return
    end
    if G.songList == nil or G.songList:GetSongNodeAtOffset(0) == nil then
        drawWaitScreen("No song found.")
        return
    end

    if mode ~= "no_bg" then
        SHARED:GetSharedTexture("background"):Draw(-G.backgroundScrollX, 0)
        SHARED:GetSharedTexture("background"):Draw(-G.backgroundScrollX + 1920, 0)
    end
    if mode == "bg_only" then return end

    -- Song select first, then difficulty select OVER it, so the Note covers the song-select right segment
    -- as it scrolls in (drawing diff under song select left a cutout there).
    if G.songSelectModes[G.activeScreen]        then DrawSS.drawPanel() end
    if G.difficultySelectModes[G.activeScreen] then Diff.drawPanel()   end

    for _, at in pairs(G.act_inner) do
        if at.IsActive then at:Draw() end
    end
end

-- ── Update ────────────────────────────────────────────────────────────────────

function update(ts)
    for _, c in pairs(G.ctx) do c:Tick() end

    -- While songs are loading or unavailable, only allow Cancel/Escape to exit.
    if IsSongsEnumerating() or G.songList == nil or G.songList:GetSongNodeAtOffset(0) == nil then
        if INPUT:KeyboardPressed("Escape") or INPUT:Pressed("Cancel") then
            G.sounds.Cancel:Play()
            return "cancel"
        end
        return nil
    end

    -- Loop preview sound; cooldown prevents double-seek on the same restart.
    if G.previewLoaded and not G.previewLoopCooldown then
        local psnd = SHARED:GetSharedSound("presound")
        if psnd.Loaded and not psnd.IsPlaying then
            G.previewLoopCooldown = true
            psnd:Play()
            psnd:SetTimestamp(G.previewDemoStart)
            G.startCounter("preview_loop_cooldown", 0, 1, 0.5, "none", nil, function()
                G.previewLoopCooldown = false
            end)
        end
    end

    -- Tick inner activities
    local hasActiveInnerModal = false
    for k, at in pairs(G.act_inner) do
        if at.IsActive then
            at:Update()
            hasActiveInnerModal = true
        end
    end

    -- Reload characters when customize_dialog closes
    local isCustomizeActive = G.act_inner["customize_dialog"] ~= nil and G.act_inner["customize_dialog"].IsActive
    if G.wasCustomizeActive and not isCustomizeActive then
        for p = 0, 4 do
            local chara = GetSaveFile(p):GetCharacter()
            if chara ~= nil and chara.IsValid then
                if not chara:AvailableAnimation(CHARACTER.ANIM_MENU_NORMAL) then
                    chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)
                end
                if G.portraits[p] ~= nil then G.portraits[p]:Dispose() end
                G.portraits[p] = TEXTURE:CreateTextureFromAbsolutePath(chara.FullPath .. "/Portrait.png")
            end
        end
    end
    G.wasCustomizeActive = isCustomizeActive

    -- Handle sort_search_dialog close: search mode is handled by search.lua,
    -- sort mode just re-applies sort and refreshes.
    local isSortDialogActive = G.act_inner["sort_search_dialog"] ~= nil and G.act_inner["sort_search_dialog"].IsActive
    if G.wasSortDialogActive and not isSortDialogActive then
        if not Search.checkSearchReady() then
            Sort.applySort(); Nav.refreshPage(true)
        end
    end
    G.wasSortDialogActive = isSortDialogActive

    -- Handle confirm_dialog close: re-sort so the newly-unlocked song moves out of
    -- the locked section, then refresh the page.
    local isConfirmDialogActive = G.act_inner["confirm_dialog"] ~= nil and G.act_inner["confirm_dialog"].IsActive
    if G.wasConfirmDialogActive and not isConfirmDialogActive then
        G.applySort()
        Nav.refreshPage(true)
    end
    G.wasConfirmDialogActive = isConfirmDialogActive

    -- Advance unlock-related animations.
    Unlocks.tick()

    if hasActiveInnerModal then return nil end

    if G.activeScreen == "songselect" then
        return Nav.handleSongSelectInput(Sort, Diff)
    elseif G.activeScreen == "difficultyselect" then
        return Diff.handleUpdate(ts)
    end

    return nil
end
