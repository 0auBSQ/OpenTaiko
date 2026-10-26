-- standard_dan.lua  —  Standard Dan Challenge sub-module for dan_select

local NavInput       = require("NavInput")
local EM             = require("EventMode")

local M = {}
local ContentsDrawer = require("standard_dan_contents_draw")
local Config         = require("Core.Config")
local Layout         = Config.load("Config/layout.json")
local Animation      = Config.load("Config/animation.json")
local Bars, Header, ConfirmLayout = Layout.bars, Layout.header, Layout.confirm
local List, Player = Animation.list, Layout.player

-- ── Constants ────────────────────────────────────────────────────────────────

local TX  = "Textures/"

local DIFF_DAN = 6

-- one bar more than the screen holds at each end: the bar that slides in on a scroll is already
-- there, just past the edge, instead of appearing at it
local PAGE_BEFORE = Bars.visible_before
local PAGE_AFTER  = Bars.visible_after
local PAGE_SIZE   = PAGE_BEFORE + 1 + PAGE_AFTER

local BAR_SEL_X   = Bars.sel_x
local BAR_SEL_Y   = Bars.sel_y
local BAR_OTH_X   = Bars.other_x
local BAR_GAP_Y   = Bars.gap_y
local HOVER_SRC_Y = ConfirmLayout.hover_src_y

local TITLE_SEL_X     = Bars.title_sel_x
local TITLE_SEL_Y     = Bars.title_y
local TITLE_OTH_X     = Bars.title_other_x
local TITLE_FONT_SIZE = Layout.fonts.bar_title
local TITLE_MAX_W     = Bars.title_max_width

local HEADER_X        = Header.x
local HEADER_Y        = Header.y
local HDR_TITLE_X     = Header.title_x
local HDR_TITLE_Y     = Header.title_y
local HDR_TITLE_FONT  = Layout.fonts.header_title
local HDR_TITLE_MAX_W = Header.title_max_width
local HDR_SUB_Y       = Header.sub_y
local HDR_SUB_FONT    = Layout.fonts.header_sub

local CONTENT_X = Layout.content.x
local CONTENT_Y = Layout.content.y
local DANPLATE_X = Layout.danplate.x
local DANPLATE_Y = Layout.danplate.y

local NP_X = Player.x
local NP_Y = Player.y

local HOLD_DELAY_SEC    = List.hold_delay_s
local HOLD_REPEAT_SEC   = List.hold_repeat_s
local BAR_STAGGER_SEC   = List.bar_stagger_s
local CONTENT_SLIDE_SEC = List.content_slide_s
local BAR_SLIDE_SEC     = List.bar_slide_s

-- ── State persistent across activate/deactivate ───────────────────────────────

local _song_list = nil
local _in_play   = false
local _CB        = nil

-- ── Per-activation resources ──────────────────────────────────────────────────

local tx_header        = nil
local tx_content       = nil
local tx_confirm_bg    = nil
local tx_confirm_hover = nil
local tx_confirm       = {}
local tx_scroll_paper  = nil       -- the confirm scroll's parchment, cut to the open part
local tx_scroll_roller = nil       -- one roller, drawn at both edges of the open part
local gfont_confirm    = nil       -- "Take the challenge?" over the options
local col_confirm, col_confirm_outline = nil, nil
local tx_bars          = {}

local danplate_ro      = nil
local modicons_ro      = nil
local act              = {}
local act_was_active   = {}
local snd_move         = nil
local snd_tick         = nil       -- Sounds/tick.ogg: a wooden tok for each bar as the list builds up

local font_bar_title   = nil
local font_hdr_title   = nil
local font_hdr_sub     = nil

-- ── Per-enter animation state ─────────────────────────────────────────────────

local _state                = "song_select"
local page_nodes            = {}
local prev_sel_node         = nil
local bar_visible           = {}
local bar_timer             = 0.0
local content_slide_y       = 0.0
local content_slide_dir     = 0
local content_slide_from    = 0.0
local content_slide_counter = nil
local bar_sel_x_anim        = BAR_SEL_X
local bar_sel_x_counter     = nil
local bar_y_offset          = 0.0
local bar_y_offset_counter  = nil
local bar_select_pulse      = nil
local hold_dir              = 0
local hold_phase            = 0
local hold_elapsed          = 0.0
local confirm_items         = { 0, 1, 2, 3 }   -- the options on the scroll: 0 back, 1 customize, 2 mods, 3 challenge
local confirm_pos           = 1                -- the selected option's place in confirm_items
local confirm_anim          = 0.0              -- the scroll: 0 rolled up, 1 open
local confirm_dir           = 1                -- 1 unrolling, -1 rolling back up (the list returns once shut)
local CONFIRM_OPEN_SEC      = Animation.confirm.open_s
local CONFIRM_CLOSE_SEC     = Animation.confirm.close_s
local CONFIRM_BTN_SCALE     = ConfirmLayout.button_scale
local CONFIRM_BTN_GAP       = ConfirmLayout.button_gap
local CONFIRM_TITLE_DY      = ConfirmLayout.title_dy               -- the title's line below the paper's top
local CONFIRM_BTN_DY        = ConfirmLayout.button_dy               -- the buttons' centre below the paper's centre

-- ── Internals ─────────────────────────────────────────────────────────────────

local function _build_dan_songs(chart)
    if chart == nil or chart.DanSongs == nil then return {} end
    local ds = {}
    for i = 0, chart.DanSongs.Length - 1 do
        ds[i + 1] = chart.DanSongs[i]
    end
    return ds
end

local function _build_dan_exams(chart)
    if chart == nil or chart.DanExams == nil then return {} end
    local de = {}
    for i = 0, chart.DanExams.Length - 1 do
        de[i + 1] = chart.DanExams[i]
    end
    return de
end

-- dan_song_exams[si][slot] = LuaSongDanExam for song si (1-based), exam slot slot (1-based).
local function _build_dan_song_exams(chart, song_count)
    if chart == nil then return {} end
    local t = {}
    for si = 1, song_count do
        t[si] = {}
        for slot = 1, 7 do
            t[si][slot] = chart:GetSongExam(si, slot)
        end
    end
    return t
end

local function _refresh_page()
    page_nodes = {}
    if _song_list == nil then return end
    for offset = -PAGE_BEFORE, PAGE_AFTER do
        local n = _song_list:GetSongNodeAtOffset(offset)
        page_nodes[PAGE_BEFORE + 1 + offset] = n
    end
end

local function _start_setup_anim(animated)
    bar_visible = {}
    bar_timer   = 0.0
    if animated then
        for i = 1, PAGE_SIZE do bar_visible[i] = false end
        _state = "song_select_setup"
    else
        for i = 1, PAGE_SIZE do bar_visible[i] = true end
        _state = "song_select"
    end
end

local function _do_scroll(dir)
    if _song_list == nil then return end
    prev_sel_node = _song_list:GetSelectedSongNode()
    _song_list:Move(dir)
    _refresh_page()
    if snd_move ~= nil then snd_move:Play() end

    content_slide_dir  = dir
    content_slide_from = -dir * List.content_slide_px
    content_slide_y    = content_slide_from
    content_slide_counter = COUNTER:CreateCounterDuration(content_slide_from, 0.0, CONTENT_SLIDE_SEC)
    content_slide_counter:SetEasing(List.ease_mode, List.ease_curve)
    content_slide_counter:Start()

    bar_sel_x_anim    = BAR_OTH_X
    bar_sel_x_counter = COUNTER:CreateCounterDuration(BAR_OTH_X, BAR_SEL_X, BAR_SLIDE_SEC)
    bar_sel_x_counter:SetEasing(List.ease_mode, List.ease_curve)
    bar_sel_x_counter:Start()

    bar_y_offset          = dir * BAR_GAP_Y
    bar_y_offset_counter  = COUNTER:CreateCounterDuration(dir * BAR_GAP_Y, 0.0, BAR_SLIDE_SEC)
    bar_y_offset_counter:SetEasing(List.ease_mode, List.ease_curve)
    bar_y_offset_counter:Start()
end

-- Returns "back" if we should return to the 3-way menu, nil otherwise
local function _handle_cancel()
    SHARED:GetSharedSound("Cancel"):Play()
    if _song_list == nil then return "back" end
    local ssn = _song_list:GetSelectedSongNode()
    if ssn ~= nil and not ssn.IsRoot then
        if _song_list:CloseFolder() then
            _refresh_page()
            _start_setup_anim(false)
            return nil
        end
    end
    return "back"
end

local function _get_bar_tex(node)
    if node == nil then return nil end
    if node.IsSong then
        local chart = node:GetChart(DIFF_DAN)
        local tick  = (chart ~= nil and chart.DanTick ~= nil) and chart.DanTick or 0
        return tx_bars[tostring(tick % 6)]
    elseif node.IsReturn then return tx_bars["Back"]
    elseif node.IsFolder then return tx_bars["Folder"]
    end
    return nil
end

local function _get_bar_color(node)
    if node == nil then return 1.0, 1.0, 1.0 end
    if node.IsSong then
        local chart = node:GetChart(DIFF_DAN)
        if chart ~= nil then
            local c = chart.DanTickColor
            if c ~= nil then return c.R/255.0, c.G/255.0, c.B/255.0 end
        end
    elseif node.IsFolder then
        local c = node.BoxColor
        if c ~= nil then return c.R/255.0, c.G/255.0, c.B/255.0 end
    end
    return 1.0, 1.0, 1.0
end

local function _draw_content(node, y_offset)
    if tx_content == nil or not tx_content.Loaded then return end
    if node == nil or not node.IsSong then return end
    tx_content:SetOpacity(1.0)
    tx_content:SetColor(1.0, 1.0, 1.0)
    tx_content:DrawAtAnchor(CONTENT_X, CONTENT_Y + (y_offset or 0), "topleft")
end

-- the assets stay loaded with the skin (M.init at the stage's onStart, freed in M.destroy), so entering
-- the list costs nothing but the per-entry setup below: nothing to decode or upload on the way in
function M.init()
    if tx_header ~= nil then return end
    tx_header        = TEXTURE:CreateTexture(TX .. "Header.png")
    tx_content       = TEXTURE:CreateTexture(TX .. "Contents.png")
    tx_confirm_bg    = TEXTURE:CreateTexture(TX .. "Confirm/BgTile.png")
    tx_confirm_hover = TEXTURE:CreateTexture(TX .. "Confirm/Hover.png")
    for i = 0, 3 do
        tx_confirm[i] = TEXTURE:CreateTexture(TX .. "Confirm/" .. i .. ".png")
    end
    for i = 0, 5 do
        tx_bars[tostring(i)] = TEXTURE:CreateTexture(TX .. "SideBars/" .. i .. ".png")
    end
    tx_bars["Back"]   = TEXTURE:CreateTexture(TX .. "SideBars/Back.png")
    tx_bars["Folder"] = TEXTURE:CreateTexture(TX .. "SideBars/Folder.png")
    tx_bars["Select"] = TEXTURE:CreateTexture(TX .. "SideBars/Select.png")
    tx_scroll_paper   = TEXTURE:CreateTexture(TX .. "Confirm/scroll_paper.png")
    tx_scroll_roller  = TEXTURE:CreateTexture(TX .. "Confirm/scroll_roller.png")
    gfont_confirm     = TEXT:CreateGlyphCached(Layout.fonts.confirm)
    col_confirm         = Config.color(Layout.colors.confirm_color)
    col_confirm_outline = Config.color(Layout.colors.confirm_outline_color)

    local ok, s = pcall(function() return SOUND:CreateSFX("Sounds/tick.ogg") end)
    if ok then snd_tick = s end

    font_bar_title = TEXT:Create(TITLE_FONT_SIZE, "regular")
    font_hdr_title = TEXT:Create(HDR_TITLE_FONT,  "regular")
    font_hdr_sub   = TEXT:Create(HDR_SUB_FONT,    "regular")

    ContentsDrawer.load()
end

-- the dan folder's song list, built when enumeration finishes so the first entry has it ready
local function _build_song_list()
    local lsls = GenerateSongListSettings()
    lsls.SubBackBoxFrequency  = 5
    lsls.ModuloPagination     = false
    lsls.AppendMainRandomBox  = false
    lsls.AppendSubRandomBoxes = false
    lsls.FlattenOpenedFolders = false
    lsls.RootGenreFolder      = "段位道場"
    lsls:SetMandatoryDifficultyList({DIFF_DAN})
    return RequestSongList(lsls)
end

local function _setup_entry()
    danplate_ro = ROACTIVITY:GetROActivity("danplate")
    modicons_ro = ROACTIVITY:GetROActivity("modicons")
    if modicons_ro ~= nil then modicons_ro:Activate() end

    act["customize_dialog"]  = ACTIVITY:GetActivity("customize_dialog")
    act["mod_select_dialog"] = ACTIVITY:GetActivity("mod_select_dialog")
    snd_move = SHARED:GetSharedSound("Move")

    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then
        chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)
    end

    local Pulse = List.select_pulse
    bar_select_pulse = COUNTER:CreateCounterDuration(Pulse.from, Pulse.to, Pulse.dur_s)
    bar_select_pulse:SetBounce(true)
    bar_select_pulse:Start()
end

local function _teardown_entry()
    if modicons_ro ~= nil then modicons_ro:Deactivate() end

    snd_move = nil

    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then
        chara:DisposeAnimation(CHARACTER.ANIM_MENU_NORMAL)
    end

    bar_sel_x_counter    = nil
    bar_y_offset_counter = nil
    bar_select_pulse     = nil
    content_slide_counter = nil
    danplate_ro = nil
    modicons_ro = nil
    for k in pairs(act) do act[k] = nil end
    for k in pairs(act_was_active) do act_was_active[k] = nil end
end

-- the options the scroll offers: Event Mode leaves the customize one out
local function _confirm_options()
    if EM.on() then return { 0, 2, 3 } end
    return { 0, 1, 2, 3 }
end

local function _open_confirm()
    confirm_items = _confirm_options()
    confirm_pos, confirm_anim, confirm_dir = 1, 0.0, 1
    _state = "confirm"
end

local function _close_confirm()
    confirm_dir = -1
end

local function _sk(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

local function _reset_anim_state()
    bar_sel_x_anim = BAR_SEL_X
    bar_y_offset   = 0.0
    hold_dir       = 0
    hold_phase     = 0
    hold_elapsed   = 0.0
    confirm_pos    = 1
    confirm_anim   = 0.0
    confirm_dir    = 1
    prev_sel_node  = nil
    content_slide_dir = 0
    content_slide_y   = 0.0
end

-- ── Public API ────────────────────────────────────────────────────────────────

function M.is_returning_from_play()
    return _in_play
end

-- Called from Script.lua's activate() when is_returning_from_play() is true,
-- or when entering standard dan from the 3-way menu.
-- is_return = true  → returning from a played dan (resume song list)
-- is_return = false → first/subsequent entry from menu
function M.enter(CB, is_return)
    _CB = CB
    _in_play   = false

    M.init()
    _setup_entry()
    _reset_anim_state()

    if not is_return and _song_list == nil then _song_list = _build_song_list() end

    _refresh_page()
    _start_setup_anim(true)
end

-- Called when user cancels back to the 3-way menu
function M.leave()
    _teardown_entry()
end

-- Called from Script.lua's deactivate() while standard_dan is active
function M.deactivate()
    _teardown_entry()
end

function M.afterSongEnum()
    -- the list follows the library: rebuilt now (this runs for every stage, active or not) so the
    -- next entry into the dojo does not build it on the spot
    _song_list = nil
    local ok, list = pcall(_build_song_list)
    if ok then _song_list = list end
end

function M.destroy()
    _teardown_entry()
    local function sd(t) if t ~= nil then t:Dispose() end end
    sd(tx_header)        ; tx_header        = nil
    sd(tx_content)       ; tx_content       = nil
    sd(tx_confirm_bg)    ; tx_confirm_bg    = nil
    sd(tx_confirm_hover) ; tx_confirm_hover = nil
    for i = 0, 3 do sd(tx_confirm[i]) ; tx_confirm[i] = nil end
    for _, t in pairs(tx_bars) do sd(t) end
    tx_bars = {}
    sd(tx_scroll_paper)  ; tx_scroll_paper  = nil
    sd(tx_scroll_roller) ; tx_scroll_roller = nil
    sd(gfont_confirm)    ; gfont_confirm    = nil
    if snd_tick ~= nil then pcall(function() snd_tick:Dispose() end) ; snd_tick = nil end
    if font_bar_title ~= nil then font_bar_title:Dispose() ; font_bar_title = nil end
    if font_hdr_title ~= nil then font_hdr_title:Dispose() ; font_hdr_title = nil end
    if font_hdr_sub   ~= nil then font_hdr_sub:Dispose()   ; font_hdr_sub   = nil end
    ContentsDrawer.unload()
    ContentsDrawer.destroy()
end

-- Returns: nil (continue) | "back" (return to 3-way menu) | "play" (exit to play)
function M.update(dt)
    if _CB then
        for _, c in pairs(_CB.ctx) do c:Tick() end
    end

    -- Activities take full control while active
    local any_active = false
    for k, a in pairs(act) do
        if a ~= nil and a.IsActive then
            a:Update()
            any_active = true
        end
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

    -- Tick contents drawer scroll
    do
        local upd_node = _song_list ~= nil and _song_list:GetSelectedSongNode() or nil
        local upd_count = 0
        if upd_node ~= nil and upd_node.IsSong then
            local chart = upd_node:GetChart(DIFF_DAN)
            if chart ~= nil and chart.DanSongs ~= nil then
                upd_count = chart.DanSongs.Length
            end
        end
        ContentsDrawer.update(dt, upd_count)
    end

    if any_active then return nil end

    -- Tick animations
    if content_slide_counter ~= nil then
        content_slide_counter:Tick()
        content_slide_y = content_slide_counter.Value
        if content_slide_y == 0.0 then
            content_slide_counter = nil
            content_slide_dir     = 0
        end
    end
    if bar_sel_x_counter ~= nil then
        bar_sel_x_counter:Tick()
        bar_sel_x_anim = bar_sel_x_counter.Value
        if bar_sel_x_anim == BAR_SEL_X then bar_sel_x_counter = nil end
    end
    if bar_select_pulse ~= nil then bar_select_pulse:Tick() end
    if bar_y_offset_counter ~= nil then
        bar_y_offset_counter:Tick()
        bar_y_offset = bar_y_offset_counter.Value
        if bar_y_offset == 0.0 then bar_y_offset_counter = nil end
    end

    if INPUT:Pressed("ToggleAutoP1") and not EM.on() then
        CONFIG:SetAutoStatus(0, not CONFIG:GetAutoStatus(0))
        SHARED:GetSharedSound("Move"):Play()
    end

    -- ── SONG SELECT SETUP ─────────────────────────────────────────────────────
    if _state == "song_select_setup" then
        bar_timer = bar_timer + dt
        local shown = 0
        for i = 1, PAGE_SIZE do
            if not bar_visible[i] and bar_timer >= (i - 1) * BAR_STAGGER_SEC then
                bar_visible[i] = true
                -- each bar that appears ticks (once per frame, should two land on the same frame);
                -- an empty slot shows no bar and stays silent
                if _get_bar_tex(page_nodes[i]) ~= nil then
                    shown = shown + 1
                    if snd_tick ~= nil and shown == 1 then pcall(function() snd_tick:Play() end) end
                end
            end
        end
        if bar_visible[PAGE_SIZE] then _state = "song_select" end
        return nil
    end

    -- ── SONG SELECT ───────────────────────────────────────────────────────────
	local navPn = NavInput.p[1]
    if _state == "song_select" then
        if hold_dir ~= 0 then
            local still =
                (hold_dir ==  1 and navPn.right()) or
                (hold_dir == -1 and navPn.left())
            if not still then
                hold_dir = 0 ; hold_phase = 0 ; hold_elapsed = 0.0
            else
                hold_elapsed = hold_elapsed + dt
                if hold_phase == 1 and hold_elapsed >= HOLD_DELAY_SEC then
                    hold_phase = 2 ; hold_elapsed = 0.0 ; _do_scroll(hold_dir)
                elseif hold_phase == 2 and hold_elapsed >= HOLD_REPEAT_SEC then
                    hold_elapsed = 0.0 ; _do_scroll(hold_dir)
                end
            end
        end

        if navPn.right() then
            _do_scroll(1) ; hold_dir = 1 ; hold_phase = 1 ; hold_elapsed = 0.0
        elseif navPn.left() then
            _do_scroll(-1) ; hold_dir = -1 ; hold_phase = 1 ; hold_elapsed = 0.0
        end

        if navPn.decide() then
            local ssn = _song_list ~= nil and _song_list:GetSelectedSongNode() or nil
            if ssn ~= nil then
                if ssn.IsSong then
                    _open_confirm()
                    SHARED:GetSharedSound("Decide"):Play()
                elseif ssn.IsFolder then
                    _song_list:OpenFolder() ; _refresh_page() ; _start_setup_anim(false)
                    SHARED:GetSharedSound("Decide"):Play()
                elseif ssn.IsReturn then
                    local r = _handle_cancel()
                    if r == "back" then return "back" end
                end
            end
        end

        if navPn.cancel() then
            local r = _handle_cancel()
            if r == "back" then return "back" end
        end
        return nil
    end

    -- ── CONFIRM ───────────────────────────────────────────────────────────────
    if _state == "confirm" then
        -- the scroll unrolls before it takes input, and rolls back up before the list returns
        if confirm_dir > 0 then
            confirm_anim = math.min(1, confirm_anim + dt / CONFIRM_OPEN_SEC)
            if confirm_anim < 1 then return nil end
        else
            confirm_anim = math.max(0, confirm_anim - dt / CONFIRM_CLOSE_SEC)
            if confirm_anim <= 0 then _state = "song_select" end
            return nil
        end

        if navPn.right() then
            confirm_pos = confirm_pos % #confirm_items + 1
            SHARED:GetSharedSound("Move"):Play()
        elseif navPn.left() then
            confirm_pos = (confirm_pos - 2) % #confirm_items + 1
            SHARED:GetSharedSound("Move"):Play()
        end

        if navPn.cancel() then
            SHARED:GetSharedSound("Cancel"):Play()
            _close_confirm()
            return nil
        end

        if navPn.decide() then
            local confirm_sel = confirm_items[confirm_pos]
            if confirm_sel == 0 then
                SHARED:GetSharedSound("Cancel"):Play()
                _close_confirm()
            elseif confirm_sel == 1 then
                if act["customize_dialog"] ~= nil then act["customize_dialog"]:Activate(0) end
                SHARED:GetSharedSound("Decide"):Play()
            elseif confirm_sel == 2 then
                if act["mod_select_dialog"] ~= nil then act["mod_select_dialog"]:Activate(0) end
                SHARED:GetSharedSound("Decide"):Play()
            elseif confirm_sel == 3 then
                local ssn = _song_list ~= nil and _song_list:GetSelectedSongNode() or nil
                if ssn ~= nil and ssn.IsSong then
                    ssn:Mount(DIFF_DAN)
                    _in_play = true
                    if _CB ~= nil then _CB.stopBGM() end
                    SHARED:GetSharedSound("SongDecide"):Play()
                    return "play"
                end
                SHARED:GetSharedSound("Error"):Play()
            end
        end

        if navPn.cancel() then
            SHARED:GetSharedSound("Cancel"):Play()
            _state = "song_select"
        end
        return nil
    end

    return nil
end

function M.draw()
    local res   = THEME:GetResolution()
    local res_w = res.X
    local res_h = res.Y

    local sel_node = (_song_list ~= nil) and _song_list:GetSelectedSongNode() or nil

    -- Content panels
    if content_slide_dir ~= 0 and content_slide_counter ~= nil
            and prev_sel_node ~= nil and prev_sel_node.IsSong then
        local prev_off   = content_slide_y - content_slide_from
        _draw_content(prev_sel_node, prev_off)
        local prev_chart  = prev_sel_node:GetChart(DIFF_DAN)
        local prev_songs  = _build_dan_songs(prev_chart)
        ContentsDrawer.draw(CONTENT_X, CONTENT_Y + prev_off,
            prev_songs, _build_dan_exams(prev_chart),
            _build_dan_song_exams(prev_chart, #prev_songs),
            GetSaveFile(0):GetDanBestPlay(prev_sel_node))
    end
    if sel_node ~= nil and sel_node.IsSong then
        _draw_content(sel_node, content_slide_y)
        local chart      = sel_node:GetChart(DIFF_DAN)
        local dan_songs  = _build_dan_songs(chart)
        ContentsDrawer.draw(CONTENT_X, CONTENT_Y + content_slide_y,
            dan_songs, _build_dan_exams(chart),
            _build_dan_song_exams(chart, #dan_songs),
            GetSaveFile(0):GetDanBestPlay(sel_node))
    end

    -- Dan plate
    if sel_node ~= nil and sel_node.IsSong and danplate_ro ~= nil then
        local chart = sel_node:GetChart(DIFF_DAN)
        if chart ~= nil then
            local tick = chart.DanTick or 0
            local c    = chart.DanTickColor
            danplate_ro:Draw(DANPLATE_X, DANPLATE_Y, 255, tick,
                (c ~= nil) and c.R or 255,
                (c ~= nil) and c.G or 255,
                (c ~= nil) and c.B or 255,
                sel_node.Title or "")
        end
    end

    -- Header
    if tx_header ~= nil and tx_header.Loaded then
        tx_header:DrawAtAnchor(HEADER_X, HEADER_Y, "topleft")
    end

    -- Header title / subtitle
    if sel_node ~= nil and font_hdr_title ~= nil then
        local hdr_text = sel_node.IsReturn and "Back" or (sel_node.Title or "")
        font_hdr_title:GetText(hdr_text, true, HDR_TITLE_MAX_W):DrawAtAnchor(HDR_TITLE_X, HDR_TITLE_Y, "center")
        if sel_node.IsSong and font_hdr_sub ~= nil then
            local sub = sel_node.Subtitle or ""
            if sub ~= "" then
                font_hdr_sub:GetText(sub, true, HDR_TITLE_MAX_W):DrawAtAnchor(HDR_TITLE_X, HDR_SUB_Y, "center")
            end
        end
    end

    -- Sidebars
    for i = 1, PAGE_SIZE do
        if bar_visible[i] then
            local node   = page_nodes[i]
            local offset = i - (PAGE_BEFORE + 1)
            local is_sel = (offset == 0)
            local bar_x  = is_sel and bar_sel_x_anim or BAR_OTH_X
            local bar_y  = BAR_SEL_Y + offset * BAR_GAP_Y + bar_y_offset

            local tx = _get_bar_tex(node)
            if tx ~= nil and tx.Loaded then
                local cr, cg, cb = _get_bar_color(node)
                tx:SetColor(cr, cg, cb)
                tx:DrawAtAnchor(bar_x, bar_y, "topleft")
                tx:SetColor(1.0, 1.0, 1.0)
            end

            if is_sel and tx_bars["Select"] ~= nil and tx_bars["Select"].Loaded then
                local pulse_op = (bar_select_pulse ~= nil) and bar_select_pulse.Value or 1.0
                tx_bars["Select"]:SetOpacity(pulse_op)
                tx_bars["Select"]:DrawAtAnchor(bar_sel_x_anim, BAR_SEL_Y + bar_y_offset, "topleft")
                tx_bars["Select"]:SetOpacity(1.0)
            end

            if node ~= nil and node.IsSong and font_bar_title ~= nil then
                local title_x_off = is_sel and (TITLE_SEL_X - BAR_SEL_X) or (TITLE_OTH_X - BAR_OTH_X)
                local t = font_bar_title:GetText(node.Title or "", false, TITLE_MAX_W)
                if t ~= nil and t.Loaded then
                    t:SetColor(1.0, 1.0, 1.0)
                    t:DrawAtAnchor(bar_x + title_x_off, bar_y + (TITLE_SEL_Y - BAR_SEL_Y), "center")
                end
            end
        end
    end

    -- Nameplate + character
    NAMEPLATE:DrawPlayerNameplate(NP_X, NP_Y, 255, 0)
    if _CB then
        _CB.drawPlayerChara(NP_X + Player.chara_dx, NP_Y + Player.chara_dy, 1.0)
        _CB.drawPlayerPuchi(NP_X + Player.puchi_dx, NP_Y + _CB.puchiSineY, 1.0, _CB.puchiIdxFrame)
    end
    if modicons_ro ~= nil then modicons_ro:Draw(NP_X, NP_Y + Player.modicons_dy, 0, "menu", 255) end

    -- Confirm scroll: the parchment unrolls from its middle, the options come into view as it does (each
    -- cut at the rollers' edge), the question fades in once its line is uncovered
    if _state == "confirm" then
        local ease = 1 - (1 - confirm_anim) * (1 - confirm_anim)
        if tx_confirm_bg ~= nil and tx_confirm_bg.Loaded then
            local bw = tx_confirm_bg.Width
            local bh = tx_confirm_bg.Height
            tx_confirm_bg:SetOpacity(ConfirmLayout.veil_opacity * ease)
            for rx = 0, math.ceil(res_w / bw) - 1 do
                for ry = 0, math.ceil(res_h / bh) - 1 do
                    tx_confirm_bg:Draw(rx * bw, ry * bh)
                end
            end
            tx_confirm_bg:SetOpacity(1.0)
        end

        local cx, cy = res_w / 2, res_h / 2
        local paper = tx_scroll_paper
        local pw = (paper ~= nil and paper.Loaded) and paper.Width or ConfirmLayout.paper_w
        local ph = (paper ~= nil and paper.Loaded) and paper.Height or ConfirmLayout.paper_h
        local half = math.floor(pw / 2 * ease)
        local open_x0, open_x1 = cx - half, cx + half
        if half > 0 and paper ~= nil and paper.Loaded then
            paper:DrawRect(open_x0, cy - ph / 2, math.floor(pw / 2) - half, 0, half * 2, ph)
        end

        -- the options, scaled to fit the paper, each cut to the part the scroll has uncovered; the
        -- player's highlight frame goes under its option once the scroll is open
        local s = CONFIRM_BTN_SCALE
        local bw, bh = ConfirmLayout.button_w * s, ConfirmLayout.button_h * s
        local n = #confirm_items
        local total_w = n * bw + (n - 1) * CONFIRM_BTN_GAP
        local btn_y = cy + CONFIRM_BTN_DY
        local sel_x = cx - total_w / 2 + (confirm_pos - 1) * (bw + CONFIRM_BTN_GAP) + bw / 2
        if confirm_anim >= 1 and tx_confirm_hover ~= nil and tx_confirm_hover.Loaded then
            local hh = tx_confirm_hover.Height - HOVER_SRC_Y
            tx_confirm_hover:SetScale(s, s)
            tx_confirm_hover:DrawRectAtAnchor(sel_x, btn_y, 0, HOVER_SRC_Y, tx_confirm_hover.Width, hh, "center")
            tx_confirm_hover:SetScale(1, 1)
        end
        for i, id in ipairs(confirm_items) do
            local btn = tx_confirm[id]
            local bx = cx - total_w / 2 + (i - 1) * (bw + CONFIRM_BTN_GAP)
            if btn ~= nil and btn.Loaded then
                local x0, x1 = math.max(bx, open_x0), math.min(bx + bw, open_x1)
                if x1 > x0 then
                    btn:SetScale(s, s)
                    btn:DrawRectAtAnchor(x0, btn_y - bh / 2, math.floor((x0 - bx) / s), 0, math.ceil((x1 - x0) / s), btn.Height, "topleft")
                    btn:SetScale(1, 1)
                end
            end
        end
        -- the question once the scroll is open enough for it
        if gfont_confirm ~= nil then
            local title = _sk("DANSELECT_CONFIRM", "Take the challenge?")
            local tw = gfont_confirm:Measure(title) + ConfirmLayout.title_pad
            local fade = math.max(0, math.min(1, (half * 2 - tw) / ConfirmLayout.title_fade_px))
            if fade > 0 then
                local nudge = math.floor((gfont_confirm.BoxHeight - math.ceil(gfont_confirm.LineHeight)) / 2) - 1
                gfont_confirm:Draw(title, cx, cy - ph / 2 + CONFIRM_TITLE_DY + nudge, col_confirm, col_confirm_outline, fade, 1, pw - ConfirmLayout.title_inset, "center")
            end
        end

        if tx_scroll_roller ~= nil and tx_scroll_roller.Loaded then
            tx_scroll_roller:DrawAtAnchor(open_x0, cy, "center")
            tx_scroll_roller:DrawAtAnchor(open_x1, cy, "center")
        end
    end

    -- Activities
    for _, a in pairs(act) do
        if a ~= nil and a.IsActive then a:Draw() end
    end
end

return M
