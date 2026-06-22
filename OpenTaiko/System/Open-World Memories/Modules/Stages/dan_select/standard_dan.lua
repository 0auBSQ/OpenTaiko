-- standard_dan.lua  —  Standard Dan Challenge sub-module for dan_select

local M = {}
local ContentsDrawer = require("standard_dan_contents_draw")

-- ── Constants ────────────────────────────────────────────────────────────────

local TX  = "Textures/"

local DIFF_DAN = 6

local PAGE_BEFORE = 6
local PAGE_AFTER  = 7
local PAGE_SIZE   = PAGE_BEFORE + 1 + PAGE_AFTER  -- 14

local BAR_SEL_X   = 1636
local BAR_SEL_Y   = 458
local BAR_OTH_X   = 1695
local BAR_GAP_Y   = 77
local HOVER_SRC_Y = 156

local TITLE_SEL_X     = 1720
local TITLE_SEL_Y     = 508
local TITLE_OTH_X     = 1779
local TITLE_FONT_SIZE = 36
local TITLE_MAX_W     = 150

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

local NP_X = 20
local NP_Y = 980

local HOLD_DELAY_SEC    = 0.25
local HOLD_REPEAT_SEC   = 0.08
local BAR_STAGGER_SEC   = 0.05
local CONTENT_SLIDE_SEC = 0.25
local BAR_SLIDE_SEC     = 0.15
local PUCHI_FLOAT_AMP   = 6.0

-- ── State persistent across activate/deactivate ───────────────────────────────

local _song_list = nil
local _in_play   = false
local _callbacks = nil

-- ── Per-activation resources ──────────────────────────────────────────────────

local tx_header        = nil
local tx_content       = nil
local tx_confirm_bg    = nil
local tx_confirm_hover = nil
local tx_confirm       = {}
local tx_bars          = {}

local danplate_ro      = nil
local modicons_ro      = nil
local act              = {}
local act_was_active   = {}
local snd_move         = nil

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
local confirm_sel           = 0
local puchi_sine_y          = 0.0
local puchi_sine_counter    = nil

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
    content_slide_from = -dir * 1080
    content_slide_y    = content_slide_from
    content_slide_counter = COUNTER:CreateCounterDuration(content_slide_from, 0.0, CONTENT_SLIDE_SEC)
    content_slide_counter:SetEasing("OUT", "QUAD")
    content_slide_counter:Start()

    bar_sel_x_anim    = BAR_OTH_X
    bar_sel_x_counter = COUNTER:CreateCounterDuration(BAR_OTH_X, BAR_SEL_X, BAR_SLIDE_SEC)
    bar_sel_x_counter:SetEasing("OUT", "QUAD")
    bar_sel_x_counter:Start()

    bar_y_offset          = dir * BAR_GAP_Y
    bar_y_offset_counter  = COUNTER:CreateCounterDuration(dir * BAR_GAP_Y, 0.0, BAR_SLIDE_SEC)
    bar_y_offset_counter:SetEasing("OUT", "QUAD")
    bar_y_offset_counter:Start()
end

-- Returns "back" if we should return to the 3-way menu, nil otherwise
local function _handle_cancel()
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

local function _draw_player_chara(x, y, opacity)
    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then
        chara:Update(CHARACTER.ANIM_MENU_NORMAL, true)
        chara:DrawAtAnchor(x, y, CHARACTER.ANIM_MENU_NORMAL, "bottom", 1.0, 1.0, math.floor(opacity * 255))
    end
end

local function _draw_player_puchi(x, y, opacity)
    local puchi = GetSaveFile(0):GetPuchichara()
    if puchi == nil or puchi.tx == nil or not puchi.tx.Loaded then return end
    local frameW = math.floor(puchi.tx.Width / 2)
    puchi.tx:SetScale(1.0, 1.0)
    puchi.tx:SetOpacity(opacity)
    puchi.tx:DrawRectAtAnchor(x, y, 0, 0, frameW, puchi.tx.Height, "bottom")
    puchi.tx:SetOpacity(1.0)
end

local function _load_resources()
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

    puchi_sine_counter = COUNTER:CreateCounter(0, 360, 1 / 120)
    puchi_sine_counter:SetLoop(true)
    puchi_sine_counter:Start()

    bar_select_pulse = COUNTER:CreateCounterDuration(0.3, 1.0, 0.6)
    bar_select_pulse:SetBounce(true)
    bar_select_pulse:Start()

    if font_bar_title == nil then
        font_bar_title = TEXT:Create(TITLE_FONT_SIZE, "regular")
        font_hdr_title = TEXT:Create(HDR_TITLE_FONT,  "regular")
        font_hdr_sub   = TEXT:Create(HDR_SUB_FONT,    "regular")
    end

    ContentsDrawer.load()
end

local function _unload_resources()
    if modicons_ro ~= nil then modicons_ro:Deactivate() end

    local function sd(t) if t ~= nil then t:Dispose() end end
    sd(tx_header)        ; tx_header        = nil
    sd(tx_content)       ; tx_content       = nil
    sd(tx_confirm_bg)    ; tx_confirm_bg    = nil
    sd(tx_confirm_hover) ; tx_confirm_hover = nil
    for i = 0, 3 do sd(tx_confirm[i]) ; tx_confirm[i] = nil end
    for _, t in pairs(tx_bars) do sd(t) end
    tx_bars = {}

    snd_move = nil

    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then
        chara:DisposeAnimation(CHARACTER.ANIM_MENU_NORMAL)
    end

    puchi_sine_counter   = nil
    bar_sel_x_counter    = nil
    bar_y_offset_counter = nil
    bar_select_pulse     = nil
    content_slide_counter = nil
    danplate_ro = nil
    modicons_ro = nil
    for k in pairs(act) do act[k] = nil end
    for k in pairs(act_was_active) do act_was_active[k] = nil end

    ContentsDrawer.unload()
end

local function _reset_anim_state()
    bar_sel_x_anim = BAR_SEL_X
    bar_y_offset   = 0.0
    hold_dir       = 0
    hold_phase     = 0
    hold_elapsed   = 0.0
    confirm_sel    = 0
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
function M.enter(shared, is_return)
    _callbacks = shared
    _in_play   = false

    _load_resources()
    _reset_anim_state()

    if not is_return and _song_list == nil then
        local lsls = GenerateSongListSettings()
        lsls.SubBackBoxFrequency  = 5
        lsls.ModuloPagination     = false
        lsls.AppendMainRandomBox  = false
        lsls.AppendSubRandomBoxes = false
        lsls.FlattenOpenedFolders = false
        lsls.RootGenreFolder      = "段位道場"
        lsls:SetMandatoryDifficultyList({DIFF_DAN})
        _song_list = RequestSongList(lsls)
    end

    _refresh_page()
    _start_setup_anim(true)
end

-- Called when user cancels back to the 3-way menu
function M.leave()
    _unload_resources()
end

-- Called from Script.lua's deactivate() while standard_dan is active
function M.deactivate()
    _unload_resources()
end

function M.afterSongEnum()
    _song_list = nil
end

function M.destroy()
    if font_bar_title ~= nil then font_bar_title:Dispose() ; font_bar_title = nil end
    if font_hdr_title ~= nil then font_hdr_title:Dispose() ; font_hdr_title = nil end
    if font_hdr_sub   ~= nil then font_hdr_sub:Dispose()   ; font_hdr_sub   = nil end
    ContentsDrawer.destroy()
end

-- Returns: nil (continue) | "back" (return to 3-way menu) | "play" (exit to play)
function M.update(dt)
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

    -- Always tick puchi sine
    if puchi_sine_counter ~= nil then
        puchi_sine_counter:Tick()
        puchi_sine_y = math.sin(puchi_sine_counter.Value * math.pi / 180) * PUCHI_FLOAT_AMP
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

    if INPUT:KeyboardPressed("F3") then
        CONFIG:SetAutoStatus(0, not CONFIG:GetAutoStatus(0))
    end

    -- ── SONG SELECT SETUP ─────────────────────────────────────────────────────
    if _state == "song_select_setup" then
        bar_timer = bar_timer + dt
        for i = 1, PAGE_SIZE do
            if not bar_visible[i] and bar_timer >= (i - 1) * BAR_STAGGER_SEC then
                bar_visible[i] = true
            end
        end
        if bar_visible[PAGE_SIZE] then _state = "song_select" end
        return nil
    end

    -- ── SONG SELECT ───────────────────────────────────────────────────────────
    if _state == "song_select" then
        if hold_dir ~= 0 then
            local still =
                (hold_dir ==  1 and (INPUT:Pressing("RightChange") or INPUT:KeyboardPressing("RightArrow"))) or
                (hold_dir == -1 and (INPUT:Pressing("LeftChange")  or INPUT:KeyboardPressing("LeftArrow")))
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

        if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
            _do_scroll(1) ; hold_dir = 1 ; hold_phase = 1 ; hold_elapsed = 0.0
        elseif INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
            _do_scroll(-1) ; hold_dir = -1 ; hold_phase = 1 ; hold_elapsed = 0.0
        end

        if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
            local ssn = _song_list ~= nil and _song_list:GetSelectedSongNode() or nil
            if ssn ~= nil then
                if ssn.IsSong then
                    _state = "confirm" ; confirm_sel = 0
                elseif ssn.IsFolder then
                    _song_list:OpenFolder() ; _refresh_page() ; _start_setup_anim(false)
                elseif ssn.IsReturn then
                    local r = _handle_cancel()
                    if r == "back" then return "back" end
                end
            end
        end

        if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
            local r = _handle_cancel()
            if r == "back" then return "back" end
        end
        return nil
    end

    -- ── CONFIRM ───────────────────────────────────────────────────────────────
    if _state == "confirm" then
        if INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
            confirm_sel = (confirm_sel + 1) % 4
        elseif INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
            confirm_sel = (confirm_sel + 3) % 4
        end

        if INPUT:Pressed("Decide") or INPUT:KeyboardPressed("Return") then
            if confirm_sel == 0 then
                _state = "song_select"
            elseif confirm_sel == 1 then
                if act["customize_dialog"] ~= nil then act["customize_dialog"]:Activate(0) end
            elseif confirm_sel == 2 then
                if act["mod_select_dialog"] ~= nil then act["mod_select_dialog"]:Activate(0) end
            elseif confirm_sel == 3 then
                local ssn = _song_list ~= nil and _song_list:GetSelectedSongNode() or nil
                if ssn ~= nil and ssn.IsSong then
                    ssn:Mount(DIFF_DAN)
                    _in_play = true
                    if _callbacks ~= nil then _callbacks.stopBGM() end
                    return "play"
                end
            end
        end

        if INPUT:Pressed("Cancel") or INPUT:KeyboardPressed("Escape") then
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
    _draw_player_chara(NP_X + 140, NP_Y - 6,            1.0)
    _draw_player_puchi(NP_X + 220, NP_Y + puchi_sine_y, 1.0)
    if modicons_ro ~= nil then modicons_ro:Draw(NP_X, NP_Y - 50, 0, "menu", 255) end

    -- Confirm dialog
    if _state == "confirm" then
        if tx_confirm_bg ~= nil and tx_confirm_bg.Loaded then
            local bw = tx_confirm_bg.Width
            local bh = tx_confirm_bg.Height
            tx_confirm_bg:SetOpacity(0.6)
            for rx = 0, math.ceil(res_w / bw) - 1 do
                for ry = 0, math.ceil(res_h / bh) - 1 do
                    tx_confirm_bg:Draw(rx * bw, ry * bh)
                end
            end
            tx_confirm_bg:SetOpacity(1.0)
        end

        local btn_gap = 10
        local total_w = 0
        for i = 0, 3 do
            if tx_confirm[i] ~= nil and tx_confirm[i].Loaded then
                total_w = total_w + tx_confirm[i].Width + btn_gap
            end
        end
        local btn_x = (res_w - total_w + btn_gap) / 2
        local btn_y = res_h / 2

        for i = 0, 3 do
            local btn = tx_confirm[i]
            if btn ~= nil and btn.Loaded then
                if i == confirm_sel and tx_confirm_hover ~= nil and tx_confirm_hover.Loaded then
                    local hh = tx_confirm_hover.Height - HOVER_SRC_Y
                    tx_confirm_hover:DrawRectAtAnchor(btn_x, btn_y, 0, HOVER_SRC_Y,
                        tx_confirm_hover.Width, hh, "center")
                end
                btn:DrawAtAnchor(btn_x, btn_y, "center")
                btn_x = btn_x + btn.Width + btn_gap
            end
        end
    end

    -- Activities
    for _, a in pairs(act) do
        if a ~= nil and a.IsActive then a:Draw() end
    end
end

return M
