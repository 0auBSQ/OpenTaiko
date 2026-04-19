-- ═══════════════════════════════════════════════════════════════════════════════
-- dan_select / Script.lua
-- Dan song select stage
-- ═══════════════════════════════════════════════════════════════════════════════

local TX  = "Textures/"
local SND = "Sounds/"

-- ─────────────────────────────────────────────────────────────────────────────
-- CONSTANTS
-- ─────────────────────────────────────────────────────────────────────────────

local DIFF_DAN = 6

-- Page layout: 6 nodes above selection, 1 selected, 7 below = 14 total
local PAGE_BEFORE = 6
local PAGE_AFTER  = 7
local PAGE_SIZE   = PAGE_BEFORE + 1 + PAGE_AFTER  -- 14

-- Sidebar geometry
local BAR_SEL_X   = 1636
local BAR_SEL_Y   = 458
local BAR_OTH_X   = 1695
local BAR_GAP_Y   = 77
-- Hover.png bottom-portion Y offset (small-bar region)
local HOVER_SRC_Y = 156

-- Title on the bars (horizontal text)
local TITLE_SEL_X     = 1720
local TITLE_SEL_Y     = 508   -- +8 from old 500
local TITLE_OTH_X     = 1779
local TITLE_FONT_SIZE = 36
local TITLE_MAX_W     = 150

-- Header / content
local HEADER_X        = 373
local HEADER_Y        = 84
local HDR_TITLE_X     = 978
local HDR_TITLE_Y     = 138
local HDR_TITLE_FONT  = 56
local HDR_TITLE_MAX_W = 1170
local HDR_SUB_Y       = 208
local HDR_SUB_FONT    = 34

local CONTENT_X = 373
local CONTENT_Y = 287

local DANPLATE_X = 195
local DANPLATE_Y = 280

-- Player nameplate / character
local NP_X = 20
local NP_Y = 980

-- Door intro
local DOOR_DELAY_SEC      = 3.0
local DOOR_OPEN_SEC       = 0.5
local DOOR_POST_DELAY_SEC = 2.0   -- buffer after door opens before menu appears

-- Background zoom
local BG_ZOOM_START = 1.2
local BG_ZOOM_END   = 1.0
local BG_ZOOM_SEC   = 4.0

-- Hold-scroll
local HOLD_DELAY_SEC  = 0.25
local HOLD_REPEAT_SEC = 0.08

-- Bar entry animation (only on first entry, not folder navigation)
local BAR_STAGGER_SEC = 0.05

-- Content slide: full 1080 px per transition
local CONTENT_SLIDE_SEC = 0.25

-- Bar x-slide and y-slide duration
local BAR_SLIDE_SEC = 0.15

-- ─────────────────────────────────────────────────────────────────────────────
-- FORWARD DECLARATIONS
-- ─────────────────────────────────────────────────────────────────────────────

local doScroll, handleCancel, startSetupAnim, enterStandardDan, refreshPage

-- ─────────────────────────────────────────────────────────────────────────────
-- STATE
-- ─────────────────────────────────────────────────────────────────────────────

--[[
  "loading"           – waiting for afterSongEnum
  "intro"             – door + background zoom
  "menu_3way"         – 3-option selection
  "menu_3way_exit"    – scroll-down transition into standard dan
  "song_select_setup" – bar entry animation (first entry only)
  "song_select"       – browsing
  "confirm"           – confirm dialog
]]
local state = "loading"

-- Persists across activate/deactivate: once the intro door has been shown once,
-- skip it on all subsequent entries (cancel + re-enter).
local intro_shown = false

-- true once Standard Dan challenge has been entered at least once this session
local in_submenu = false

-- Song list
local song_enum_done = false
local song_list      = nil
local page_nodes     = {}  -- [1..14] LuaSongNode or nil

-- Previous selected node (kept for the outgoing content panel during scroll)
local prev_sel_node  = nil

-- Door / intro
local door_timer      = 0.0
local door_opening    = false
local door_done       = false
local door_open_t     = 0.0   -- 0..1 progress
local door_post_delay = 0.0   -- elapsed seconds after door fully open
local bg_zoom         = BG_ZOOM_START

-- Counters
local bg_zoom_counter       = nil
local menu_exit_counter     = nil
local content_slide_counter = nil
local content_slide_from    = 0.0
local bar_sel_x_counter     = nil
local bar_y_offset_counter  = nil

-- 3-way menu
local menu_sel    = 1   -- 1=Standard Dan  2=Pagoda  3=Forest
local menu_exit_y = 0.0

-- Bar setup animation
local bar_visible = {}
local bar_timer   = 0.0

-- Content slide (y)
local content_slide_y   = 0.0
local content_slide_dir = 0

-- Bar selected-x animation
local bar_sel_x_anim = BAR_SEL_X   -- current animated x of the selected bar

-- Bar y-offset animation (all bars shift by BAR_GAP_Y when scrolling)
local bar_y_offset = 0.0

-- Select.png pulsing opacity counter
local bar_select_pulse = nil

-- Hold-scroll
local hold_dir     = 0
local hold_phase   = 0    -- 1=initial delay, 2=repeating
local hold_elapsed = 0.0

-- Confirm dialog
local confirm_sel    = 0
local confirm_active = false

-- Puchi sine bob (puchichara only, character does not float)
local puchi_sine_y       = 0.0
local puchi_sine_counter = nil
local PUCHI_FLOAT_AMP    = 6.0

-- Activities
local act            = {}
local act_was_active = {}  -- previous-frame IsActive, to detect close events

-- ─────────────────────────────────────────────────────────────────────────────
-- FONTS  (onStart / onDestroy)
-- ─────────────────────────────────────────────────────────────────────────────

local font_bar_title = nil   -- size TITLE_FONT_SIZE – horizontal title on each sidebar
local font_hdr_title = nil   -- size 56  – header main title
local font_hdr_sub   = nil   -- size 34  – header subtitle (song only)
local font_loading   = nil   -- size 30  – "Please wait" / small labels
local font_menu      = nil   -- size 44  – 3-way menu options

-- ─────────────────────────────────────────────────────────────────────────────
-- TEXTURES  (activate / deactivate)
-- ─────────────────────────────────────────────────────────────────────────────

local tx_bg            = nil
local tx_door          = nil
local tx_header        = nil
local tx_content       = nil
local tx_confirm_bg    = nil
local tx_confirm_hover = nil
local tx_confirm       = {}   -- [0..3]
local tx_bars          = {}   -- ["0"].."5"], "Back", "Folder", "Select"

-- ─────────────────────────────────────────────────────────────────────────────
-- SOUNDS  (activate / deactivate)
-- ─────────────────────────────────────────────────────────────────────────────

local snd_entry = nil
local snd_bgm   = nil
local snd_move  = nil

-- ─────────────────────────────────────────────────────────────────────────────
-- ROACTIVITIES
-- ─────────────────────────────────────────────────────────────────────────────

local danplate_ro = nil
local modicons_ro = nil

-- ─────────────────────────────────────────────────────────────────────────────
-- HELPERS
-- ─────────────────────────────────────────────────────────────────────────────

local function safeDisposeTx(t)
    if t ~= nil then t:Dispose() end
end

local function stopBGM()
    if snd_bgm ~= nil and snd_bgm.IsPlaying then snd_bgm:Stop() end
end

local function startBGM()
    if snd_bgm ~= nil and not snd_bgm.IsPlaying then
        snd_bgm:SetLoop(true)
        snd_bgm:Play()
    end
end

refreshPage = function()
    page_nodes = {}
    if song_list == nil then return end
    for offset = -PAGE_BEFORE, PAGE_AFTER do
        local n = song_list:GetSongNodeAtOffset(offset)
        page_nodes[PAGE_BEFORE + 1 + offset] = n
    end
end

-- animated=true  → staggered bar entry animation (first entry only)
-- animated=false → bars appear instantly, go straight to song_select
startSetupAnim = function(animated)
    bar_visible = {}
    bar_timer   = 0.0
    if animated then
        for i = 1, PAGE_SIZE do bar_visible[i] = false end
        state = "song_select_setup"
    else
        for i = 1, PAGE_SIZE do bar_visible[i] = true end
        state = "song_select"
    end
end

enterStandardDan = function()
    in_submenu = true
    startBGM()

    if song_list == nil then
        local lsls = GenerateSongListSettings()
        lsls.SubBackBoxFrequency  = 5
        lsls.ModuloPagination     = false
        lsls.AppendMainRandomBox  = false
        lsls.AppendSubRandomBoxes = false
        lsls.FlattenOpenedFolders = false
        lsls.RootGenreFolder      = "段位道場"
        lsls:SetMandatoryDifficultyList({DIFF_DAN})
        song_list = RequestSongList(lsls)
    end

    refreshPage()
    startSetupAnim(true)   -- animated entry only on first open
end

-- Sidebar texture for a node
local function getBarTex(node)
    if node == nil then return nil end
    if node.IsSong then
        local chart = node:GetChart(DIFF_DAN)
        local tick  = (chart ~= nil and chart.DanTick ~= nil) and chart.DanTick or 0
        return tx_bars[tostring(tick % 6)]
    elseif node.IsReturn then
        return tx_bars["Back"]
    elseif node.IsFolder then
        return tx_bars["Folder"]
    end
    return nil
end

-- RGB tint (0..1) for a node's sidebar
local function getBarColor(node)
    if node == nil then return 1.0, 1.0, 1.0 end
    if node.IsSong then
        local chart = node:GetChart(DIFF_DAN)
        if chart ~= nil then
            local c = chart.DanTickColor
            if c ~= nil then
                return c.R / 255.0, c.G / 255.0, c.B / 255.0
            end
        end
    elseif node.IsFolder then
        local c = node.BoxColor
        if c ~= nil then
            return c.R / 255.0, c.G / 255.0, c.B / 255.0
        end
    end
    return 1.0, 1.0, 1.0
end

-- Draw Content.png + inner elements at an arbitrary Y offset.
local function drawContent(node, y_offset)
    if tx_content == nil or not tx_content.Loaded then return end
    if node == nil or not node.IsSong then return end
    local cy = CONTENT_Y + (y_offset or 0)
    tx_content:SetOpacity(1.0)
    tx_content:SetColor(1.0, 1.0, 1.0)
    tx_content:DrawAtAnchor(CONTENT_X, cy, "topleft")
    -- Song-specific content inside the panel will be added here later
end

-- Draw the character menu animation for P1 (no sine bob – puchi only bobs)
local function drawPlayerChara(x, y, opacity)
    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then
        chara:Update(CHARACTER.ANIM_MENU_NORMAL, true)
        chara:DrawAtAnchor(x, y, CHARACTER.ANIM_MENU_NORMAL, "bottom", 1.0, 1.0, math.floor(opacity * 255))
    end
end

-- Draw puchichara for P1 (bobs via y argument)
local function drawPlayerPuchi(x, y, opacity)
    local puchi = GetSaveFile(0):GetPuchichara()
    if puchi == nil or puchi.tx == nil or not puchi.tx.Loaded then return end
    local frameW = math.floor(puchi.tx.Width / 2)
    local frameH = puchi.tx.Height
    puchi.tx:SetScale(1.0, 1.0)
    puchi.tx:SetOpacity(opacity)
    puchi.tx:DrawRectAtAnchor(x, y, 0, 0, frameW, frameH, "bottom")
    puchi.tx:SetOpacity(1.0)
end

doScroll = function(dir)
    if song_list == nil then return end

    -- Save outgoing selection before moving (dual-panel content animation)
    prev_sel_node = song_list:GetSelectedSongNode()

    song_list:Move(dir)
    refreshPage()
    if snd_move ~= nil then snd_move:Play() end

    -- Content slides in from ±1080
    content_slide_dir  = dir
    content_slide_from = -dir * 1080
    content_slide_y    = content_slide_from
    content_slide_counter = COUNTER:CreateCounterDuration(content_slide_from, 0.0, CONTENT_SLIDE_SEC)
    content_slide_counter:SetEasing("OUT", "QUAD")
    content_slide_counter:Start()

    -- Selected bar x slides from BAR_OTH_X to BAR_SEL_X
    bar_sel_x_anim = BAR_OTH_X
    bar_sel_x_counter = COUNTER:CreateCounterDuration(BAR_OTH_X, BAR_SEL_X, BAR_SLIDE_SEC)
    bar_sel_x_counter:SetEasing("OUT", "QUAD")
    bar_sel_x_counter:Start()

    -- All bars slide one slot in the scroll direction (dir * BAR_GAP_Y → 0)
    bar_y_offset = dir * BAR_GAP_Y
    bar_y_offset_counter = COUNTER:CreateCounterDuration(dir * BAR_GAP_Y, 0.0, BAR_SLIDE_SEC)
    bar_y_offset_counter:SetEasing("OUT", "QUAD")
    bar_y_offset_counter:Start()
end

handleCancel = function()
    if song_list == nil then return end
    local ssn = song_list:GetSelectedSongNode()
    -- Try closing a subfolder first
    if ssn ~= nil and not ssn.IsRoot then
        if song_list:CloseFolder() then
            refreshPage()
            startSetupAnim(false)   -- instant, no animation for folder navigation
            return
        end
    end
    -- Root level: go back to 3-way menu immediately, no fade, BGM keeps playing
    in_submenu = false
    state      = "menu_3way"
end

-- ─────────────────────────────────────────────────────────────────────────────
-- LIFECYCLE
-- ─────────────────────────────────────────────────────────────────────────────

function onStart()
    font_bar_title = TEXT:Create(TITLE_FONT_SIZE, "regular")
    font_hdr_title = TEXT:Create(HDR_TITLE_FONT,  "regular")
    font_hdr_sub   = TEXT:Create(HDR_SUB_FONT,    "regular")
    font_loading   = TEXT:Create(30,               "regular")
    font_menu      = TEXT:Create(44,               "regular")
end

function onDestroy()
    if font_bar_title ~= nil then font_bar_title:Dispose() ; font_bar_title = nil end
    if font_hdr_title ~= nil then font_hdr_title:Dispose() ; font_hdr_title = nil end
    if font_hdr_sub   ~= nil then font_hdr_sub:Dispose()   ; font_hdr_sub   = nil end
    if font_loading   ~= nil then font_loading:Dispose()   ; font_loading   = nil end
    if font_menu      ~= nil then font_menu:Dispose()      ; font_menu      = nil end
end

function activate()
    CONFIG.PlayerCount = 1
    CONFIG.SongSpeed   = 20  -- x1.0

    -- Textures
    tx_bg      = TEXTURE:CreateTexture(TX .. "Background.png")
    tx_door    = TEXTURE:CreateTexture(TX .. "Door.png")
    tx_header  = TEXTURE:CreateTexture(TX .. "Header.png")
    tx_content = TEXTURE:CreateTexture(TX .. "Contents.png")

    -- Confirm panel
    tx_confirm_bg    = TEXTURE:CreateTexture(TX .. "Confirm/BgTile.png")
    tx_confirm_hover = TEXTURE:CreateTexture(TX .. "Confirm/Hover.png")
    for i = 0, 3 do
        tx_confirm[i] = TEXTURE:CreateTexture(TX .. "Confirm/" .. i .. ".png")
    end

    -- Sidebars
    for i = 0, 5 do
        tx_bars[tostring(i)] = TEXTURE:CreateTexture(TX .. "SideBars/" .. i .. ".png")
    end
    tx_bars["Back"]   = TEXTURE:CreateTexture(TX .. "SideBars/Back.png")
    tx_bars["Folder"] = TEXTURE:CreateTexture(TX .. "SideBars/Folder.png")
    tx_bars["Select"] = TEXTURE:CreateTexture(TX .. "SideBars/Select.png")

    -- Sounds
    snd_entry = SOUND:CreateSFX(SND .. "Entry.ogg")
    snd_bgm   = SOUND:CreateBGM(SND .. "BGM.ogg")
    snd_move  = SHARED:GetSharedSound("Move")

    -- ROActivities
    danplate_ro = ROACTIVITY:GetROActivity("danplate")
    modicons_ro = ROACTIVITY:GetROActivity("modicons")
    if modicons_ro ~= nil then modicons_ro:Activate() end

    -- Character animation
    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then
        chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)
    end

    -- Activities
    act["customize_dialog"]  = ACTIVITY:GetActivity("customize_dialog")
    act["mod_select_dialog"] = ACTIVITY:GetActivity("mod_select_dialog")

    -- Puchi sine bob
    puchi_sine_counter = COUNTER:CreateCounter(0, 360, 1 / 120)
    puchi_sine_counter:SetLoop(true)
    puchi_sine_counter:Start()

    bar_select_pulse = COUNTER:CreateCounterDuration(0.3, 1.0, 0.6)
    bar_select_pulse:SetBounce(true)
    bar_select_pulse:Start()

    -- Reset animation state
    bar_sel_x_anim  = BAR_SEL_X
    bar_y_offset    = 0.0
    hold_dir        = 0
    hold_phase      = 0
    hold_elapsed    = 0.0
    confirm_active  = false
    confirm_sel     = 0
    prev_sel_node   = nil

    if in_submenu then
        -- Returning from a played dan: skip intro, resume at song list with animated entry
        door_done = true
        refreshPage()
        startBGM()
        startSetupAnim(true)
    elseif intro_shown then
        -- Re-entering after cancel: skip door entirely
        door_done       = true
        door_post_delay = DOOR_POST_DELAY_SEC  -- already passed
        if song_enum_done then
            state = "menu_3way"
            startBGM()
        else
            state = "loading"
        end
    else
        -- First time entry: full door intro
        door_timer      = 0.0
        door_opening    = false
        door_done       = false
        door_open_t     = 0.0
        door_post_delay = 0.0
        bg_zoom         = BG_ZOOM_START
        if song_enum_done then
            state = "intro"
            snd_entry:Play()
        else
            state = "loading"
        end
    end
end

function deactivate()
    stopBGM()

    if modicons_ro ~= nil then modicons_ro:Deactivate() end

    safeDisposeTx(tx_bg)            ; tx_bg            = nil
    safeDisposeTx(tx_door)          ; tx_door          = nil
    safeDisposeTx(tx_header)        ; tx_header        = nil
    safeDisposeTx(tx_content)       ; tx_content       = nil
    safeDisposeTx(tx_confirm_bg)    ; tx_confirm_bg    = nil
    safeDisposeTx(tx_confirm_hover) ; tx_confirm_hover = nil
    for i = 0, 3 do safeDisposeTx(tx_confirm[i]) ; tx_confirm[i] = nil end
    for _, t in pairs(tx_bars) do safeDisposeTx(t) end
    tx_bars = {}

    if snd_entry ~= nil then snd_entry:Dispose() ; snd_entry = nil end
    if snd_bgm   ~= nil then snd_bgm:Dispose()   ; snd_bgm   = nil end
    snd_move = nil

    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then
        chara:DisposeAnimation(CHARACTER.ANIM_MENU_NORMAL)
    end

    puchi_sine_counter    = nil
    bg_zoom_counter       = nil
    menu_exit_counter     = nil
    content_slide_counter = nil
    bar_sel_x_counter     = nil
    bar_y_offset_counter  = nil
    bar_select_pulse      = nil

    danplate_ro = nil
    modicons_ro = nil
    for k in pairs(act) do act[k] = nil end
end

function afterSongEnum()
    song_enum_done = true
    if state == "loading" then
        if intro_shown then
            state = "menu_3way"
            startBGM()
        else
            state = "intro"
            if snd_entry ~= nil then snd_entry:Play() end
        end
    end
end

-- ─────────────────────────────────────────────────────────────────────────────
-- UPDATE
-- ─────────────────────────────────────────────────────────────────────────────

function update()
    local dt = fps.deltaTime

    -- Activities take full input control while active; still tick counters below
    local any_activity_active = false
    for k, a in pairs(act) do
        if a ~= nil and a.IsActive then
            a:Update()
            any_activity_active = true
        end
        -- Detect close event: reload character animation when customize_dialog closes
        if a ~= nil then
            local is_now = a.IsActive
            if act_was_active[k] and not is_now and k == "customize_dialog" then
                local chara = GetSaveFile(0):GetCharacter()
                if chara ~= nil and chara.IsValid then
                    chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)
                end
            end
            act_was_active[k] = is_now
        end
    end
    if any_activity_active then return end

    -- Puchi sine bob
    if puchi_sine_counter ~= nil then
        puchi_sine_counter:Tick()
        puchi_sine_y = math.sin(puchi_sine_counter.Value * math.pi / 180) * PUCHI_FLOAT_AMP
    end

    -- Content slide
    if content_slide_counter ~= nil then
        content_slide_counter:Tick()
        content_slide_y = content_slide_counter.Value
        if content_slide_y == 0.0 then
            content_slide_counter = nil
            content_slide_dir     = 0
        end
    end

    -- Bar selected-x slide
    if bar_sel_x_counter ~= nil then
        bar_sel_x_counter:Tick()
        bar_sel_x_anim = bar_sel_x_counter.Value
        if bar_sel_x_anim == BAR_SEL_X then
            bar_sel_x_counter = nil
        end
    end

    -- Select.png pulse
    if bar_select_pulse ~= nil then bar_select_pulse:Tick() end

    -- Bar y-offset slide (all bars move one slot on scroll)
    if bar_y_offset_counter ~= nil then
        bar_y_offset_counter:Tick()
        bar_y_offset = bar_y_offset_counter.Value
        if bar_y_offset == 0.0 then
            bar_y_offset_counter = nil
        end
    end

    -- ── LOADING ──────────────────────────────────────────────────────────────
    if state == "loading" then
        if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
            return Exit("title", nil)
        end
        return
    end

    -- ── INTRO ─────────────────────────────────────────────────────────────────
    if state == "intro" then
        door_timer = door_timer + dt

        if not door_opening and door_timer >= DOOR_DELAY_SEC then
            door_opening = true
            bg_zoom_counter = COUNTER:CreateCounterDuration(BG_ZOOM_START, BG_ZOOM_END, BG_ZOOM_SEC)
            bg_zoom_counter:SetEasing("OUT", "QUAD")
            bg_zoom_counter:Start()
        end

        if bg_zoom_counter ~= nil then
            bg_zoom_counter:Tick()
            bg_zoom = bg_zoom_counter.Value
        end

        if door_opening and not door_done then
            if door_open_t < 1.0 then
                door_open_t = math.min(1.0, door_open_t + dt / DOOR_OPEN_SEC)
            else
                -- Door fully open: wait DOOR_POST_DELAY_SEC before showing menu + BGM
                door_post_delay = door_post_delay + dt
                if door_post_delay >= DOOR_POST_DELAY_SEC then
                    door_done   = true
                    intro_shown = true
                    state       = "menu_3way"
                    startBGM()
                end
            end
        end

        if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
            return Exit("title", nil)
        end
        return
    end

    -- ── MENU 3-WAY ────────────────────────────────────────────────────────────
    if state == "menu_3way" then
        if bg_zoom_counter ~= nil then bg_zoom_counter:Tick() end

        if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
            return Exit("title", nil)
        end

        if INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("UpArrow") then
            menu_sel = math.max(1, menu_sel - 1)
        elseif INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("DownArrow") then
            menu_sel = math.min(3, menu_sel + 1)
        end

        if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
            if menu_sel == 1 then
                state = "menu_3way_exit"
                menu_exit_y     = 0.0
                menu_exit_counter = COUNTER:CreateCounterDuration(0.0, 1080.0, 0.4)
                menu_exit_counter:SetEasing("IN", "QUAD")
                menu_exit_counter:Start()
            end
            -- Options 2 and 3: not yet implemented
        end
        return
    end

    -- ── MENU 3-WAY EXIT ───────────────────────────────────────────────────────
    if state == "menu_3way_exit" then
        if bg_zoom_counter ~= nil then bg_zoom_counter:Tick() end
        if menu_exit_counter ~= nil then
            menu_exit_counter:Tick()
            menu_exit_y = menu_exit_counter.Value
            if menu_exit_y >= 1080.0 then
                menu_exit_counter = nil
                enterStandardDan()
            end
        end
        return
    end

    -- ── SONG SELECT SETUP ─────────────────────────────────────────────────────
    if state == "song_select_setup" then
        bar_timer = bar_timer + dt
        for i = 1, PAGE_SIZE do
            if not bar_visible[i] and bar_timer >= (i - 1) * BAR_STAGGER_SEC then
                bar_visible[i] = true
            end
        end
        if bar_visible[PAGE_SIZE] then
            state = "song_select"
        end
        return
    end

    -- ── SONG SELECT ───────────────────────────────────────────────────────────
    if state == "song_select" then
        -- Hold-scroll release detection
        if hold_dir ~= 0 then
            local still_pressing =
                (hold_dir ==  1 and (INPUT:Pressing("RightChange") or INPUT:KeyboardPressing("RightArrow"))) or
                (hold_dir == -1 and (INPUT:Pressing("LeftChange")  or INPUT:KeyboardPressing("LeftArrow")))
            if not still_pressing then
                hold_dir = 0 ; hold_phase = 0 ; hold_elapsed = 0.0
            else
                hold_elapsed = hold_elapsed + dt
                if hold_phase == 1 and hold_elapsed >= HOLD_DELAY_SEC then
                    hold_phase = 2 ; hold_elapsed = 0.0
                    doScroll(hold_dir)
                elseif hold_phase == 2 and hold_elapsed >= HOLD_REPEAT_SEC then
                    hold_elapsed = 0.0
                    doScroll(hold_dir)
                end
            end
        end

        if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
            doScroll(1)
            hold_dir = 1 ; hold_phase = 1 ; hold_elapsed = 0.0
        elseif INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
            doScroll(-1)
            hold_dir = -1 ; hold_phase = 1 ; hold_elapsed = 0.0
        end

        if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
            local ssn = song_list:GetSelectedSongNode()
            if ssn ~= nil then
                if ssn.IsSong then
                    state = "confirm"
                    confirm_active = true
                    confirm_sel    = 0
                elseif ssn.IsFolder then
                    song_list:OpenFolder()
                    refreshPage()
                    startSetupAnim(false)  -- instant, no stagger on folder open
                elseif ssn.IsReturn then
                    handleCancel()
                end
            end
        end

        if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
            handleCancel()
        end
        return
    end

    -- ── CONFIRM ───────────────────────────────────────────────────────────────
    if state == "confirm" then
        if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
            confirm_sel = (confirm_sel + 1) % 4
        elseif INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
            confirm_sel = (confirm_sel + 3) % 4
        end

        if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
            if confirm_sel == 0 then
                confirm_active = false
                state = "song_select"
            elseif confirm_sel == 1 then
                if act["customize_dialog"] ~= nil then
                    act["customize_dialog"]:Activate(0)
                end
            elseif confirm_sel == 2 then
                if act["mod_select_dialog"] ~= nil then
                    act["mod_select_dialog"]:Activate(0)
                end
            elseif confirm_sel == 3 then
                local ssn = song_list:GetSelectedSongNode()
                if ssn ~= nil and ssn.IsSong then
                    ssn:Mount(DIFF_DAN)
                    stopBGM()
                    return Exit("play", nil)
                end
            end
        end

        if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
            confirm_active = false
            state = "song_select"
        end
        return
    end
end

-- ─────────────────────────────────────────────────────────────────────────────
-- DRAW
-- ─────────────────────────────────────────────────────────────────────────────

function draw()
    local res   = THEME:GetResolution()
    local res_w = res.X
    local res_h = res.Y

    -- ── LOADING ──────────────────────────────────────────────────────────────
    if state == "loading" then
        if font_loading ~= nil then
            local t = font_loading:GetText("Please wait for song enumeration to complete...", false, 900)
            t:DrawAtAnchor(res_w / 2, res_h / 2, "center")
        end
        return
    end

    -- ── BACKGROUND (all post-loading states) ─────────────────────────────────
    if tx_bg ~= nil and tx_bg.Loaded then
        tx_bg:SetScale(bg_zoom, bg_zoom)
        tx_bg:DrawAtAnchor(res_w / 2, res_h / 2, "center")
        tx_bg:SetScale(1.0, 1.0)
    end

    -- ── DOOR (intro only) ────────────────────────────────────────────────────
    if not door_done and tx_door ~= nil and tx_door.Loaded then
        local dw   = tx_door.Width
        local dh   = tx_door.Height
        local half = dw / 2
        if not door_opening then
            tx_door:Draw(0, 0)
        else
            local offset = math.floor(door_open_t * half)
            tx_door:DrawRectAtAnchor(-offset,       0, 0,    0, half, dh, "topleft")
            tx_door:DrawRectAtAnchor(half + offset,  0, half, 0, half, dh, "topleft")
        end
    end

    if state == "intro" then return end

    -- ── 3-WAY MENU ───────────────────────────────────────────────────────────
    if state == "menu_3way" or state == "menu_3way_exit" then
        local menu_y_offset = (state == "menu_3way_exit") and menu_exit_y or 0.0

        local options = {
            "Standard Dan Challenge",
            "Pagoda of the Unknown",
            "Forest of Strata",
        }
        local descriptions = {
            "Prove your skills through the dojo's selection!",
            "Go through multiple randomized dans in a row!",
            "Progress to new challenges freshly tailored by the Fox Band!",
        }

        local opt_x       = res_w / 2
        local opt_y_start = res_h / 2 - 80
        local opt_gap     = 100

        for i, label in ipairs(options) do
            local oy     = opt_y_start + (i - 1) * opt_gap + menu_y_offset
            local is_sel = (i == menu_sel)

            local t = font_menu:GetText(label, false, 900)
            t:SetOpacity(is_sel and 1.0 or 0.5)
            t:SetColor(is_sel and 1.0 or 0.7, is_sel and 1.0 or 0.7, is_sel and 1.0 or 0.7)
            t:DrawAtAnchor(opt_x, oy, "center")
            t:SetColor(1.0, 1.0, 1.0)
            if is_sel and font_loading ~= nil then
                local d = font_loading:GetText(descriptions[i], false, 800)
                d:DrawAtAnchor(opt_x, oy + 48, "center")
            end
        end

        NAMEPLATE:DrawPlayerNameplate(NP_X, NP_Y, 255, 0)
        drawPlayerChara(NP_X + 140, NP_Y - 6,            1.0)
        drawPlayerPuchi(NP_X + 220, NP_Y + puchi_sine_y, 1.0)
        return
    end

    -- ── SONG SELECT SETUP / SONG SELECT / CONFIRM ────────────────────────────

    -- Selected node
    local sel_node = (song_list ~= nil) and song_list:GetSelectedSongNode() or nil

    -- Content panels (drawn first, behind the header overlay)
    -- Outgoing panel (prev_sel_node sliding out)
    if content_slide_dir ~= 0 and content_slide_counter ~= nil
            and prev_sel_node ~= nil and prev_sel_node.IsSong then
        local out_y = content_slide_y - content_slide_from
        drawContent(prev_sel_node, out_y)
    end
    -- Incoming panel (sel_node sliding in)
    if sel_node ~= nil and sel_node.IsSong then
        drawContent(sel_node, content_slide_y)
    end

    -- Dan plate for selected song (also behind header)
    if sel_node ~= nil and sel_node.IsSong and danplate_ro ~= nil then
        local chart = sel_node:GetChart(DIFF_DAN)
        if chart ~= nil then
            local tick = chart.DanTick or 0
            local c    = chart.DanTickColor
            local cr   = (c ~= nil) and c.R or 255
            local cg   = (c ~= nil) and c.G or 255
            local cb   = (c ~= nil) and c.B or 255
            danplate_ro:Draw(DANPLATE_X, DANPLATE_Y, 255, tick, cr, cg, cb, sel_node.Title or "")
        end
    end

    -- Header (drawn on top of content panels)
    if tx_header ~= nil and tx_header.Loaded then
        tx_header:DrawAtAnchor(HEADER_X, HEADER_Y, "topleft")
    end

    -- Header title and subtitle (on top of header graphic)
    if sel_node ~= nil then
        if font_hdr_title ~= nil then
            local hdr_text = sel_node.IsReturn and "Back" or (sel_node.Title or "")
            local t = font_hdr_title:GetText(hdr_text, true, HDR_TITLE_MAX_W)
            t:DrawAtAnchor(HDR_TITLE_X, HDR_TITLE_Y, "center")
        end

        if sel_node.IsSong and font_hdr_sub ~= nil then
            local subtitle = sel_node.Subtitle or ""
            if subtitle ~= "" then
                local s = font_hdr_sub:GetText(subtitle, true, HDR_TITLE_MAX_W)
                s:DrawAtAnchor(HDR_TITLE_X, HDR_SUB_Y, "center")
            end
        end
    end

    -- ── SIDEBARS ─────────────────────────────────────────────────────────────

    for i = 1, PAGE_SIZE do
        if bar_visible[i] then
            local node   = page_nodes[i]
            local offset = i - (PAGE_BEFORE + 1)  -- 0 = selected
            local is_sel = (offset == 0)

            -- X: selected bar animates, others snap
            local bar_x = is_sel and bar_sel_x_anim or BAR_OTH_X
            -- Y: all bars shift one slot during scroll animation
            local bar_y = BAR_SEL_Y + offset * BAR_GAP_Y + bar_y_offset

            local tx = getBarTex(node)
            if tx ~= nil and tx.Loaded then
                local cr, cg, cb = getBarColor(node)
                tx:SetColor(cr, cg, cb)
                tx:DrawAtAnchor(bar_x, bar_y, "topleft")
                tx:SetColor(1.0, 1.0, 1.0)
            end

            -- Select.png highlight on selected bar (follows bar_y_offset, bouncing opacity)
            if is_sel and tx_bars["Select"] ~= nil and tx_bars["Select"].Loaded then
                local pulse_op = (bar_select_pulse ~= nil) and bar_select_pulse.Value or 1.0
                tx_bars["Select"]:SetOpacity(pulse_op)
                tx_bars["Select"]:DrawAtAnchor(bar_sel_x_anim, BAR_SEL_Y + bar_y_offset, "topleft")
                tx_bars["Select"]:SetOpacity(1.0)
            end

            -- Bar title: horizontal text, only for songs
            if node ~= nil and node.IsSong and font_bar_title ~= nil then
                local title_x_off = is_sel and (TITLE_SEL_X - BAR_SEL_X) or (TITLE_OTH_X - BAR_OTH_X)
                local title_x     = bar_x + title_x_off
                local title_y     = bar_y + (TITLE_SEL_Y - BAR_SEL_Y)   -- fixed relative offset
                local t = font_bar_title:GetText(node.Title or "", false, TITLE_MAX_W)
                if t ~= nil and t.Loaded then
                    t:SetColor(1.0, 1.0, 1.0)
                    t:DrawAtAnchor(title_x, title_y, "center")
                end
            end
        end
    end

    -- Nameplate + character (character does not bob; only puchi does)
    NAMEPLATE:DrawPlayerNameplate(NP_X, NP_Y, 255, 0)
    drawPlayerChara(NP_X + 140, NP_Y - 6,            1.0)
    drawPlayerPuchi(NP_X + 220, NP_Y + puchi_sine_y,  1.0)

    -- ── CONFIRM DIALOG ───────────────────────────────────────────────────────
    if state == "confirm" then
        -- Tile BgTile.png to darken the background (semi-transparent)
        if tx_confirm_bg ~= nil and tx_confirm_bg.Loaded then
            local bg_w   = tx_confirm_bg.Width
            local bg_h   = tx_confirm_bg.Height
            local reps_x = math.ceil(res_w / bg_w)
            local reps_y = math.ceil(res_h / bg_h)
            tx_confirm_bg:SetOpacity(0.6)
            for rx = 0, reps_x - 1 do
                for ry = 0, reps_y - 1 do
                    tx_confirm_bg:Draw(rx * bg_w, ry * bg_h)
                end
            end
            tx_confirm_bg:SetOpacity(1.0)
        end

        -- Lay out 4 buttons horizontally centred
        local btn_gap     = 10
        local total_btn_w = 0
        for i = 0, 3 do
            if tx_confirm[i] ~= nil and tx_confirm[i].Loaded then
                total_btn_w = total_btn_w + tx_confirm[i].Width + btn_gap
            end
        end
        local btn_x_start = (res_w - total_btn_w + btn_gap) / 2
        local btn_y = res_h / 2
        local btn_x = btn_x_start

        for i = 0, 3 do
            local btn = tx_confirm[i]
            if btn ~= nil and btn.Loaded then
                -- Hover drawn first (behind the button image)
                if i == confirm_sel and tx_confirm_hover ~= nil and tx_confirm_hover.Loaded then
                    local hw = tx_confirm_hover.Width
                    local hh = tx_confirm_hover.Height - HOVER_SRC_Y
                    tx_confirm_hover:DrawRectAtAnchor(btn_x, btn_y,
                        0, HOVER_SRC_Y, hw, hh, "center")
                end

                btn:DrawAtAnchor(btn_x, btn_y, "center")
                btn_x = btn_x + btn.Width + btn_gap
            end
        end

        -- Mod icons for P1
        if modicons_ro ~= nil then
            modicons_ro:Draw(NP_X, NP_Y - 50, 0, "menu", 255)
        end
    end

    -- Activities draw on top of everything
    for _, a in pairs(act) do
        if a ~= nil and a.IsActive then
            a:Draw()
        end
    end
end
