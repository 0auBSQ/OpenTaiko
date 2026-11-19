---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- song select core, shared by the song select stages
-- layout: Config/layout.json; timings: Config/animation.json

local Sort    = require("sort")
local Nav     = require("navigation")
local Diff    = require("diffselect")
local Replay  = require("replaylist")
local EM      = require("EventMode")
local DrawSS  = require("draw_songselect")
local Config  = require("Core.Config")
local Layout  = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local NavInput = require("NavInput")
local I18N    = require("i18n")

-- baked-text overlays, per language: loaded in activate(), freed in deactivate()
-- G.bgtx key -> default file; "<name>_<code>.png" used when present
local OVERLAY_FILES = { overlay = "Textures/bg_overlay.png", overlay_difficulty = "Textures/bg_overlay_difficulty.png" }

local PREVIEW_FADE_IN_MS  = Animation.preview.fade_in_ms
local PREVIEW_FADE_OUT_MS = Animation.preview.fade_out_ms
local Fonts = Layout.fonts
local WaitScreen = Layout.wait_screen
local WAIT_TEXT_COLOR = Config.color(WaitScreen.text_color)
local MusicDuck = require("MusicDuck")
local previewDuck = MusicDuck.new()   -- ducks the preview during the unlock reward modal
local Search  = require("search")
local Unlocks = require("unlockables")
local Favs    = require("favorites")
local BG      = require("backgrounds")   -- per-genre background sets
local SC      = require("shortcuts")     -- keyboard shortcuts (theme "key" settings), help panel
local Featured = require("featured")     -- curated boxes at the top of the root list

-- ==== Shared state (G) ====
-- passed to every module's init()

local G = {
    -- fonts
    text = nil, textSmall = nil, textLarge = nil, textStats = nil,

    sounds = {},

    -- song list
    songList = nil,
    currentPage = {}, pageTexts = {}, genre_overlays = {},

    -- textures
    bars = {}, bgtx = {},
    favoriteicon = nil,
    portraits = {},

    highlightedPlayer = 0,

    -- inner activities
    act_inner = {}, modicons_ro = nil,

    -- counters
    ctx = {},

    -- scroll / animation
    nowMs               = 0,
    backgroundScrollX   = 0,
    songSelectShift     = 0,
    songSelectElemOpacity      = 255,
    difficultySelectElemOpacity = 0,
    levelLabelFrame = 0,
    difficultyFade4 = 0,
    arrowsDistance  = 0,
    selectBoxDist   = 0,
    noteFloatPhase  = 0,

    -- screen state
    activeScreen         = "songselect",
    songSelectModes      = { songselect = true, pretransition = true, transition = true },
    difficultySelectModes = { difficultyselect = true, transition = true },

    -- dialog close tracking
    wasCustomizeActive      = false,
    wasSortDialogActive     = false,
    wasConfirmDialogActive  = false,

    -- search state: LuaSongList:OpenVirtualFolder / CloseFolder

    -- preview
    puchiSineY          = 0,
    puchiIdxFrame       = 0,
    selectedSongNode    = nil,
    previewDemoStartRaw = 0,
    previewDemoStart    = 0,
    previewDurationMs   = 0,
    previewLoaded       = false,
    previewWasPlaying   = false, -- falling edge restarts the loop
    previewFadeVol      = 0,    -- applied volume 0..100, eased toward previewFadeTarget
    previewFadeTarget   = 0,    -- 0: fade out, 100: fade in
    preimagePopScale    = 1,    -- jacket pop-in scale (set by navigation)

    -- difficulty select
    diffBars     = {},
    diffIndex    = {0, 0, 0, 0, 0},
    diffSelected = {false, false, false, false, false},

    -- config / lifecycle
    activeConfig = {},
    lastSignal   = nil,

    -- sort
    originalOrders = {},

    -- hold scroll
    holdDir = 0,

    NavInput = NavInput,
}

-- ==== Shared helpers (G) ====

-- kept in G.ctx against GC
G.startCounter = function(key, startVal, endVal, interval, mode, updateCallback, onFinish)
    local c = COUNTER:CreateCounter(startVal, endVal, interval, onFinish)
    if mode == "loop"   then c:SetLoop(true)
    elseif mode == "bounce" then c:SetBounce(true) end
    if updateCallback then c:Listen(updateCallback) end
    c:Start()
    G.ctx[key] = c
    return c
end

-- skin locale string (Locales/<lang>/); fallback when missing
-- cached until activate() clears G.skinTexts (language change: next visit)
G.skinTexts = {}
G.skinText = function(key, fallback)
    local s = G.skinTexts[key]
    if s == nil then
        local ok, v = pcall(function() return THEME:GetSkinString(key) end)
        s = (ok and type(v) == "string" and v ~= "" and v:sub(1, 1) ~= "[") and v or fallback
        G.skinTexts[key] = s
    end
    return s
end

-- used by draw_songselect and diffselect
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

G.drawPlayerChara = function(player, x, y, scaleX, scaleY, opacity, flipX)
    local chara = GetSaveFile(player):GetCharacter()
    if chara ~= nil and chara.IsValid then
        chara:Update(CHARACTER.ANIM_MENU_NORMAL, true)
        local esx = (flipX or false) and -scaleX or scaleX
        chara:DrawAtAnchor(x, y, CHARACTER.ANIM_MENU_NORMAL, "bottom", esx, scaleY, math.floor(opacity * 255))
    end
end

local Puchi = Animation.puchi
local PUCHI_N_FRAMES = Puchi.frame_count  -- TODO: Read from PuchiConfig.txt
G.drawPlayerPuchi = function(player, x, y, scaleX, scaleY, opacity, flipX, idxFrame)
    local puchi = GetSaveFile(player):GetPuchichara()
    if puchi == nil or puchi.tx == nil or not puchi.tx.Loaded then return end
    local frameW = math.floor(puchi.tx.Width / PUCHI_N_FRAMES)
    local frameH = puchi.tx.Height
    local esx = (flipX or false) and -scaleX or scaleX
    idxFrame = idxFrame or 0
    puchi.tx:SetScale(esx, scaleY)
    puchi.tx:SetOpacity(opacity)
    puchi.tx:DrawRectAtAnchor(x, y, idxFrame * frameW, 0, frameW, frameH, "bottom")
    puchi.tx:SetOpacity(1); puchi.tx:SetScale(1, 1)
end

local NAMEPLATE_OFFSET_X = Layout.difficulty_select.nameplate.offset_x
G.drawCharaWithNameplate = function(player, x, y, scaleX, scaleY, opacity, flipX)
    G.drawPlayerChara(player, x + G.bgtx["nameplate_info"].Width / 2 - NAMEPLATE_OFFSET_X,
        y, scaleX, scaleY, opacity, flipX)
    NAMEPLATE:DrawPlayerNameplate(x, y, opacity * 255, player)
end

-- ==== Module init ====

Sort.init(G)
Nav.init(G)
Diff.init(G)
DrawSS.init(G)
Search.init(G)
Unlocks.init(G)
Favs.init(G)
BG.init(G)
G.backgrounds = BG
SC.init(G)
G.shortcuts = SC
G.songListHit = DrawSS.hitTest
-- bool theme settings, read once per visit; missing counts as on
local themeFlags = {}
G.reloadThemeFlags = function()
    themeFlags = {}
    for _, id in ipairs({ "songselect_mouse", "songselect_replays" }) do
        local ok, v = pcall(function() return THEME:GetThemeSetting(id) end)
        themeFlags[id] = not (ok and (v == "0" or v == "false"))
    end
end
G.themeFlag = function(id)
    local v = themeFlags[id]
    if v == nil then return true end
    return v
end
-- one human player (AI battle counts as one) and songselect_mouse on
-- the best-plays list handles the mouse on its own
G.mouseAllowed = function()
    if not G.themeFlag("songselect_mouse") then return false end
    return CONFIG.PlayerCount == 1 or G.activeConfig.mountAISlotToP2 == true
end
-- Event Mode countdown and hud: event_timer ROActivity
-- eventCountdown: running this visit (diffselect hides its back bar); eventOver: visit ends the session
G.event = EM
G.eventTimer = nil
G.eventCountdown = false
G.eventOver = false
G.playedSinceLastVisit = false
Featured.init(G)

-- for modules without a Sort reference
G.applySort = function() Sort.applySort() end

G.unlocks = Unlocks

-- ==== Lifecycle ====

function onStart()
    -- glyph fonts: one texture per unique character, no per-string cache
    G.text      = TEXT:CreateGlyphCached(Fonts.text)
    G.textSmall = TEXT:CreateGlyphCached(Fonts.small)
    G.textLarge = TEXT:CreateGlyphCached(Fonts.large)
    G.textStats = TEXT:CreateGlyphCached(Fonts.stats)

    BG.loadDefault()   -- bg1.png, fallback set for genres without one

    G.bgtx["load"]                      = TEXTURE:CreateTexture("Textures/load.png")
    G.bgtx["preimage_load"]             = TEXTURE:CreateTexture("Textures/preimage_load.png")
    G.bgtx["songinfo"]                  = TEXTURE:CreateTexture("Textures/bg_songinfo.png")
    G.bgtx["randominfo"]                = TEXTURE:CreateTexture("Textures/bg_randominfo.png")
    G.bgtx["folderinfo"]                = TEXTURE:CreateTexture("Textures/bg_folderinfo.png")
    G.bgtx["difficultyselect"]          = TEXTURE:CreateTexture("Textures/bg_difficultyselect.png")
    G.bgtx["header"]                    = TEXTURE:CreateTexture("Textures/bg_header.png")
    G.bgtx["header-box"]                = TEXTURE:CreateTexture("Textures/bg_header-box.png")
    G.bgtx["header-arrow"]              = TEXTURE:CreateTexture("Textures/bg_header-arrow.png")
    G.bgtx["nameplate_info"]            = TEXTURE:CreateTexture("Textures/nameplate_info.png")
    G.bgtx["sinfo_video"]               = TEXTURE:CreateTexture("Textures/sinfo_video.png")
    G.bgtx["sinfo_speed_arrow"]         = TEXTURE:CreateTexture("Textures/sinfo_speed_arrow.png")   -- tinted per draw
    G.bgtx["sinfo_explicit"]            = TEXTURE:CreateTexture("Textures/sinfo_explicit.png")
    G.bgtx["sinfo_difficulties_missing"] = TEXTURE:CreateTexture("Textures/sinfo_difficulties_missing.png")
    G.bgtx["sinfo_difficulties_vault"]      = TEXTURE:CreateTexture("Textures/sinfo_difficulties_vault.png")
    for i = 0, 4 do
        G.bgtx["sinfo_difficulties_" .. i]           = TEXTURE:CreateTexture("Textures/sinfo_difficulties_" .. i .. ".png")
    end
    -- level digits; "+" is the last character of a plus level
    for i = 0, 9 do
        G.bgtx["levellabelsfill" .. i]   = TEXTURE:CreateTexture("Textures/BarLevelFill/" .. i .. ".png")
        G.bgtx["levellabels" .. i]       = TEXTURE:CreateTexture("Textures/BarLevel/" .. i .. ".png")
        G.bgtx["sinfo_level" .. i]       = TEXTURE:CreateTexture("Textures/SinfoLevel/" .. i .. ".png")
        G.bgtx["diffsel_levelcol" .. i]  = TEXTURE:CreateTexture("Textures/DifficultyBars/LevelCol/" .. i .. ".png")
    end
    G.bgtx["levellabelsfill+"]  = TEXTURE:CreateTexture("Textures/BarLevelFill/plus.png")
    G.bgtx["levellabels+"]      = TEXTURE:CreateTexture("Textures/BarLevel/plus.png")
    G.bgtx["sinfo_level+"]      = TEXTURE:CreateTexture("Textures/SinfoLevel/plus.png")
    G.bgtx["diffsel_levelcol+"] = TEXTURE:CreateTexture("Textures/DifficultyBars/LevelCol/plus.png")
    G.bgtx["diffsel_levelcol+border"] = TEXTURE:CreateTexture("Textures/DifficultyBars/LevelCol/plus_border.png")
    -- seg8: Easy/Normal, seg10: others; 3 slice cells each, placed by diffselect
    G.bgtx["diffsel_gauge8"]  = TEXTURE:CreateTexture("Textures/DifficultyBars/LevelGauge/seg8.png")
    G.bgtx["diffsel_gauge10"] = TEXTURE:CreateTexture("Textures/DifficultyBars/LevelGauge/seg10.png")
    G.bgtx["diffsel_gauge8_space"]  = TEXTURE:CreateTexture("Textures/DifficultyBars/LevelGauge/seg8_space.png")
    G.bgtx["diffsel_gauge10_space"] = TEXTURE:CreateTexture("Textures/DifficultyBars/LevelGauge/seg10_space.png")
    -- level stars past the gauge's 10 slices: star<bar>.png, bar 2 = Easy .. 6 = Ura, 7 = vault
    -- optional per bar
    for i = 2, 7 do
        if TEXTURE:Exists("Textures/DifficultyBars/star" .. i .. ".png") then
            G.bgtx["diffsel_star" .. i] = TEXTURE:CreateTexture("Textures/DifficultyBars/star" .. i .. ".png")
        end
    end
    G.bgtx["diffsel_star_glint"] = TEXTURE:CreateTexture("Textures/DifficultyBars/star_glint.png")
    G.bgtx["placeholder_portrait"] = TEXTURE:CreateTexture("Textures/placeholder_portrait.png")

    G.bars["bar"]              = TEXTURE:CreateTexture("Textures/bar.png")
    -- sticker of the random / back / folder bars, with icons
    G.bars["sticker"]          = TEXTURE:CreateTexture("Textures/bar_sticker.png")
    G.bars["sticker_random"]   = TEXTURE:CreateTexture("Textures/bar_sticker_random.png")
    G.bars["sticker_back"]     = TEXTURE:CreateTexture("Textures/bar_sticker_back.png")
    G.bars["sticker_folder"]   = TEXTURE:CreateTexture("Textures/bar_sticker_folder.png")
    G.bars["locked"]           = TEXTURE:CreateTexture("Textures/locked.png")
    G.bars["selected-arrow-l"] = TEXTURE:CreateTexture("Textures/selected-arrow-l.png")
    G.bars["selected-arrow-r"] = TEXTURE:CreateTexture("Textures/selected-arrow-r.png")
    G.bars["levellabels"]        = TEXTURE:CreateTexture("Textures/bar_levelbg.png")
    G.bars["levellabelsvault"]   = TEXTURE:CreateTexture("Textures/bar_levelbgvault.png")
    G.bars["levellabelsfire"]    = TEXTURE:CreateTexture("Textures/bar_levelbgfire.png")
    G.bars["levellabelsstorm"]   = TEXTURE:CreateTexture("Textures/bar_levelbgstorm.png")
    -- Easy / Normal / Hard high levels (draw_songselect SONGBAR_CHECKER_LEVEL_*)
    for i = 0, 2 do
        G.bars["levellabelschecker" .. i] = TEXTURE:CreateTexture("Textures/bar_levelbgchecker" .. i .. ".png")
    end
    for i = 1, 5 do
        G.bars["difficultybarselect" .. i] = TEXTURE:CreateTexture("Textures/DifficultyBars/P" .. i .. ".png")
    end
    for i = 2, 7 do
        G.bars["difficultybar" .. i] = TEXTURE:CreateTexture("Textures/DifficultyBars/" .. i .. ".png")
    end
    -- difficulty select layout frame (floats, rotates)
    G.bars["diffnote"] = TEXTURE:CreateTexture("Textures/DifficultyBars/Note.png")
    -- option bars in screen order (diffselect.lua)
    G.bars["smallbar0"] = TEXTURE:CreateTexture("Textures/DifficultyBars/0.png")
    G.bars["smallbar1"] = TEXTURE:CreateTexture("Textures/DifficultyBars/1.png")
    G.bars["smallbar2"] = TEXTURE:CreateTexture("Textures/DifficultyBars/Customize.png")

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

-- allowPlayerCount: L key cycles the player count (default true)
-- lockedPlayerCount: forces CONFIG.PlayerCount
-- mountAISlotToP2: AI virtual slot on spot 2
-- songOnly: song pick only (online lobby)
-- backgroundMode: nil = per-genre sets; "shared" = SHARED "background" texture, set after this call
function activate(allowPlayerCount, lockedPlayerCount, mountAISlotToP2, songOnly, backgroundMode)
    G.folderCountCache = nil   -- unlocks may have changed since the last visit
    G.activeConfig = {
        allowPlayerCount  = allowPlayerCount,
        lockedPlayerCount = lockedPlayerCount,
        mountAISlotToP2   = mountAISlotToP2 == true,
        songOnly          = songOnly == true,   -- online lobby: no difficulty prompt, Auto off
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
    G.sounds.Error     = SHARED:GetSharedSound("Error")

    Diff.resetToSongSelect()
    Unlocks.invalidateCondCache()

    BG.setMode(backgroundMode)
    BG.reset()
    SC.reload()   -- bindings may change between visits
    G.reloadThemeFlags()

    -- Event Mode: count the last play, reset mods; countdown hud unless the session ended
    G.eventOver = false
    G.eventCountdown = false
    G.eventExpired = false
    G.eventTimer = ROACTIVITY:GetROActivity("event_timer")
    if EM.on() then
        if G.playedSinceLastVisit then
            EM.resetMods()
            if EM.registerPlay("song") then G.eventOver = true end
        end
        if not G.eventOver and G.eventTimer ~= nil then
            G.eventCountdown = EM.selectSeconds() > 0
            G.eventTimer:Activate(EM.selectSeconds(), "song")
        end
    end
    G.playedSinceLastVisit = false

    -- language may change between visits; freed in deactivate()
    for key, default in pairs(OVERLAY_FILES) do
        if G.bgtx[key] ~= nil then G.bgtx[key]:Dispose() end
        G.bgtx[key] = TEXTURE:CreateTexture(I18N.localizedPath(default))
    end

    G.speedLabel = nil                  -- re-read in the current language
    G.skinTexts = {}

    -- online lobby: a playing preview stays at full volume (navigation takes it over)
    if G.activeConfig.songOnly then
        local ps = SHARED:GetSharedSound("presound")
        if ps ~= nil and ps.Loaded and ps.IsPlaying then G.previewFadeVol, G.previewFadeTarget = 100, 100 end
    end

    if G.songList ~= nil then
        Sort.applySort()
        Nav.refreshPage()
    end

    -- animation counters
    local PUCHI_FLOAT_AMP                  = Puchi.float_amp_px
    local LevelTag = Animation.level_tag
    local SONGBAR_LEVEL_EX_ANIMATION_FRAMECOUNT = LevelTag.frame_count
    local UraFade = Animation.ura_fade
    local Arrows = Animation.selected_arrows

    G.startCounter("background", 1920, 0, 1 / Animation.background.shared_scroll_px_per_s, "loop", function(val)
        G.backgroundScrollX = val
    end)
    G.startCounter("extreme_fade", UraFade.cycle, 0, 1 / UraFade.speed_per_s, "loop", function(val)
        local fadeIn = val - UraFade.fade_in_from; local fadeOut = UraFade.cycle - val
        G.difficultyFade4 = math.max(0, math.min(255, fadeIn, fadeOut))
    end)
    -- select band pulse select_band_min..1, cosine eased; counter 0..1000 over select_band_ms
    local bandMin = Animation.song_list.select_band_min
    G.startCounter("selectband_animation", 0, 1000, Animation.song_list.select_band_ms / 1000000, "bounce", function(val)
        G.selectBandPulse = bandMin + (1 - bandMin) * (0.5 - 0.5 * math.cos(math.pi * val / 1000))
    end)
    G.startCounter("arrows_animation", Arrows.distance_px, 0, 1 / Arrows.speed_px_per_s, "bounce", function(val)
        G.arrowsDistance = val
    end)
    G.startCounter("leveltag_animation",
        SONGBAR_LEVEL_EX_ANIMATION_FRAMECOUNT, 0,
        LevelTag.loop_s / SONGBAR_LEVEL_EX_ANIMATION_FRAMECOUNT, "loop", function(val)
        G.levelLabelFrame = math.floor(val)
    end)
    G.startCounter("load_animation", 0, 360, 1 / Animation.loading_spinner.speed_deg_per_s, "loop", function(val)
        if G.bgtx["load"] ~= nil then G.bgtx["load"]:SetRotation(val) end
    end)
    G.startCounter("puchi_sine", 0, 360, 1 / Puchi.float_speed_deg_per_s, "loop", function(val)
        G.puchiSineY = math.sin(val * math.pi / 180) * PUCHI_FLOAT_AMP
    end)
    G.startCounter("puchi_frame", 0, 1, Puchi.frame_loop_s, "loop", function(val)
        G.puchiIdxFrame = math.floor(val * PUCHI_N_FRAMES)
    end)
    -- diffnote phase 0..360; ticks on every screen, so it runs during the transition
    G.startCounter("diffnote_float", 0, 360, 1 / Animation.difficulty_select.note.phase_speed_deg_per_s, "loop", function(val)
        G.noteFloatPhase = val
    end)

    -- no players' box in the online lobby
    G.portraits = {}
    if G.activeConfig.songOnly then return end
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
    G.playedSinceLastVisit = (G.lastSignal == "play")   -- the next activate is the return from that play
    G.lastSignal = nil
    if G.eventTimer ~= nil and G.eventTimer.IsActive then G.eventTimer:Deactivate() end
    SC.dispose()

    for k in pairs(G.ctx) do G.ctx[k] = COUNTER:EmptyCounter() end
    Diff.resetTransitionVisuals()

    -- online lobby keeps its preview playing
    if not G.activeConfig.songOnly then
        SHARED:GetSharedSound("presound"):Stop()
        SHARED:SetSharedString("presound_path", "")
    end
    previewDuck:reset()
    G.previewFadeVol    = 0
    G.previewFadeTarget = 0
    G.previewLoaded     = false
    G.previewWasPlaying = false

    if not G.activeConfig.songOnly then
        for p = 0, 4 do
            local chara = GetSaveFile(p):GetCharacter()
            if chara ~= nil and chara.IsValid then chara:DisposeAnimation(CHARACTER.ANIM_MENU_NORMAL) end
            if G.portraits[p] ~= nil then G.portraits[p]:Dispose(); G.portraits[p] = nil end
        end
    end
    G.portraits = {}

    for key in pairs(OVERLAY_FILES) do
        if G.bgtx[key] ~= nil then G.bgtx[key]:Dispose(); G.bgtx[key] = nil end
    end
end

function afterSongEnum()
    local lsls = GenerateSongListSettings()
    lsls:SetExcludedGenreFolders({"段位道場", "太鼓タワー"})
    lsls.ModuloPagination    = false
    lsls.HideEmptyFolders    = true
    lsls.FlattenOpenedFolders = false
    lsls:SetMandatoryDifficultyList({0, 1, 2, 3, 4})   -- Easy..Edit
    lsls.MandatoryDifficultyMatchAll = false           -- OR: at least one listed difficulty
    G.songList       = RequestSongList(lsls)
    Featured.insertAll()   -- before the first folder; cursor stays on that folder
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
    BG.dispose()
end

-- ==== Draw ====

local function drawWaitScreen(msg)
    local black = COLOR:CreateColorFromHex("ff000000")
    G.textLarge:Draw(msg, WaitScreen.x, WaitScreen.y, WAIT_TEXT_COLOR, nil, 1, 1, WaitScreen.max_width, "center")
end

function draw(mode)
    if G.eventOver then return end   -- leaving for the thank-you screen: nothing under the fade
    -- loading / no songs: status message only
    if IsSongsEnumerating() then
        drawWaitScreen(G.skinText("VAULT_LOADING", "Loading songs, please wait..."))
        return
    end
    if G.songList == nil or G.songList:GetSongNodeAtOffset(0) == nil then
        drawWaitScreen(G.skinText("SONGSELECT_NO_SONGS", "No song found."))
        return
    end

    if mode ~= "no_bg" then BG.draw() end
    if mode == "bg_only" then return end

    -- difficulty select over song select: the Note covers its right segment while scrolling in
    if G.songSelectModes[G.activeScreen]        then DrawSS.drawPanel() end
    if G.difficultySelectModes[G.activeScreen] then Diff.drawPanel()   end

    for _, at in pairs(G.act_inner) do
        if at.IsActive then at:Draw() end
    end
    SC.draw()
    if G.eventTimer ~= nil and G.eventTimer.IsActive then G.eventTimer:Draw() end
end

-- ==== Update ====

-- returns "play" | "cancel" | "thanks" | nil; the parent stage picks the exit
function update(ts)
    G.nowMs = ts                                   -- frame clock, ms, for draw-side animations
    for _, c in pairs(G.ctx) do c:Tick() end
    BG.update()

    -- session over: stage goes to the thank-you screen
    if G.eventOver then return "thanks" end

    -- Event Mode countdown runs under dialogs; on expiry they close
    -- the input handler then picks (eventExpired waits through folder animations)
    if G.eventTimer ~= nil and G.eventTimer.IsActive then
        if EM.signal(G.eventTimer:Update()) == "expired" then
            G.eventExpired = true
            for _, at in pairs(G.act_inner) do
                if at.IsActive then pcall(function() at:Deactivate() end) end
            end
            if SC.isOpen() then SC.close() end
        end
    end

    -- loading or no songs: cancel only
    if IsSongsEnumerating() or G.songList == nil or G.songList:GetSongNodeAtOffset(0) == nil then
        if G.NavInput.cancel() then
            G.sounds.Cancel:Play()
            return "cancel"
        end
        return nil
    end

    -- preview volume eased to its target (set by navigation.lua)
    -- reward modal (confirm_dialog) ducks it on top
    do
        local psnd = SHARED:GetSharedSound("presound")
        local dtms = 1000 / 60
        if ts ~= nil and G.previewPrevTs ~= nil then dtms = math.max(0, math.min(100, ts - G.previewPrevTs)) end
        local modal = ROACTIVITY:GetROActivity("modal")
        local duckBefore = previewDuck.factor
        local duck = previewDuck:update(dtms / 1000, modal ~= nil and modal.IsActive)
        if psnd.Loaded and (G.previewFadeVol ~= G.previewFadeTarget or duck ~= duckBefore) then
            local dur  = (G.previewFadeTarget > G.previewFadeVol) and PREVIEW_FADE_IN_MS or PREVIEW_FADE_OUT_MS
            local step = (dur > 0) and (100 * dtms / dur) or 100
            if G.previewFadeVol < G.previewFadeTarget then
                G.previewFadeVol = math.min(G.previewFadeTarget, G.previewFadeVol + step)
            else
                G.previewFadeVol = math.max(G.previewFadeTarget, G.previewFadeVol - step)
            end
            psnd:SetVolume(G.previewFadeVol * duck)
        end
    end
    if ts ~= nil then G.previewPrevTs = ts end

    -- loop on the IsPlaying falling edge: a fresh start/seek reads false before it was ever true
    -- Stop() before Play(): the two system sound buffers never overlap
    if G.previewLoaded then
        local psnd = SHARED:GetSharedSound("presound")
        if psnd.Loaded then
            if psnd.IsPlaying then
                G.previewWasPlaying = true
            elseif G.previewWasPlaying then
                G.previewWasPlaying = false
                psnd:Stop()
                psnd:Play()
                psnd:SetTimestamp(G.previewDemoStart)
            end
        end
    end

    local hasActiveInnerModal = false
    for k, at in pairs(G.act_inner) do
        if at.IsActive then
            at:Update()
            hasActiveInnerModal = true
        end
    end

    -- customize_dialog closed: reload characters
    local isCustomizeActive = G.act_inner["customize_dialog"] ~= nil and G.act_inner["customize_dialog"].IsActive
    if G.wasCustomizeActive and not isCustomizeActive and not G.activeConfig.songOnly then
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

    -- sort_search_dialog closed: search.lua handles search, else re-sort
    local isSortDialogActive = G.act_inner["sort_search_dialog"] ~= nil and G.act_inner["sort_search_dialog"].IsActive
    if G.wasSortDialogActive and not isSortDialogActive then
        if not Search.checkSearchReady() then
            Sort.applySort(); Nav.refreshPage(true)
        end
    end
    G.wasSortDialogActive = isSortDialogActive

    -- confirm_dialog closed: re-sort, the unlocked song leaves the locked section
    local isConfirmDialogActive = G.act_inner["confirm_dialog"] ~= nil and G.act_inner["confirm_dialog"].IsActive
    if G.wasConfirmDialogActive and not isConfirmDialogActive then
        G.applySort()
        Nav.refreshPage(true)
    end
    G.wasConfirmDialogActive = isConfirmDialogActive

    Unlocks.tick()

    if hasActiveInnerModal then return nil end

    -- shortcuts panel takes the input while open
    if SC.isOpen() then SC.update(ts); return nil end

    if G.activeScreen == "songselect" then
        return Nav.handleSongSelectInput(Sort, Diff)
    elseif G.activeScreen == "difficultyselect" then
        return Diff.handleUpdate(ts)
    end

    return nil
end
