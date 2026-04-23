-- ═══════════════════════════════════════════════════════════════════════════════
-- dan_select / Script.lua
-- Main entry point — manages door intro, 3-way menu, and delegates to sub-modules.
-- ═══════════════════════════════════════════════════════════════════════════════

local standard_dan = require("standard_dan")
local pagoda       = require("pagoda")

local TX  = "Textures/"
local SND = "Sounds/"

-- ── Constants ────────────────────────────────────────────────────────────────

local NP_X = 20
local NP_Y = 980

local DOOR_DELAY_SEC      = 3.0
local DOOR_OPEN_SEC       = 0.5
local DOOR_POST_DELAY_SEC = 2.0

local BG_ZOOM_START = 1.2
local BG_ZOOM_END   = 1.0
local BG_ZOOM_SEC   = 4.0

local PUCHI_FLOAT_AMP = 6.0

-- ── Forward declarations ──────────────────────────────────────────────────────

local startBGM, stopBGM

-- ── State ────────────────────────────────────────────────────────────────────

--[[
  "loading"          – waiting for afterSongEnum
  "intro"            – door + background zoom
  "menu_3way"        – 3-option selection
  "menu_3way_exit"   – scroll-down transition into a sub-module
  "standard_dan"     – delegating to standard_dan module
  "pagoda"           – delegating to pagoda module
]]
local state = "loading"

-- Persists: once the door intro has been shown, skip it on all re-entries
local intro_shown    = false
local song_enum_done = false

-- Door / intro
local door_timer      = 0.0
local door_opening    = false
local door_done       = false
local door_open_t     = 0.0
local door_post_delay = 0.0
local bg_zoom         = BG_ZOOM_START

-- Counters
local bg_zoom_counter   = nil
local menu_exit_counter = nil

-- 3-way menu
local menu_sel    = 1
local menu_exit_y = 0.0
local menu_exit_target = nil   -- "standard_dan" | "pagoda"

-- Puchi sine bob
local puchi_sine_y       = 0.0
local puchi_sine_counter = nil

-- ── Textures / sounds ─────────────────────────────────────────────────────────

local tx_bg   = nil
local tx_door = nil

local snd_entry = nil
local snd_bgm   = nil

-- ── Fonts ────────────────────────────────────────────────────────────────────

local font_loading = nil
local font_menu    = nil

-- ── Shared callbacks passed to sub-modules ────────────────────────────────────

startBGM = function()
    if snd_bgm ~= nil and not snd_bgm.IsPlaying then
        snd_bgm:SetLoop(true)
        snd_bgm:Play()
    end
end

stopBGM = function()
    if snd_bgm ~= nil and snd_bgm.IsPlaying then snd_bgm:Stop() end
end

local shared_callbacks = {
    startBGM = startBGM,
    stopBGM  = stopBGM,
}

-- ── Draw helpers ──────────────────────────────────────────────────────────────

local function drawPlayerChara(x, y, opacity)
    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then
        chara:Update(CHARACTER.ANIM_MENU_NORMAL, true)
        chara:DrawAtAnchor(x, y, CHARACTER.ANIM_MENU_NORMAL, "bottom", 1.0, 1.0, math.floor(opacity * 255))
    end
end

local function drawPlayerPuchi(x, y, opacity)
    local puchi = GetSaveFile(0):GetPuchichara()
    if puchi == nil or puchi.tx == nil or not puchi.tx.Loaded then return end
    local frameW = math.floor(puchi.tx.Width / 2)
    puchi.tx:SetScale(1.0, 1.0)
    puchi.tx:SetOpacity(opacity)
    puchi.tx:DrawRectAtAnchor(x, y, 0, 0, frameW, puchi.tx.Height, "bottom")
    puchi.tx:SetOpacity(1.0)
end

-- ── Lifecycle ────────────────────────────────────────────────────────────────

function onStart()
    font_loading = TEXT:Create(30, "regular")
    font_menu    = TEXT:Create(44, "regular")
end

function onDestroy()
    if font_loading ~= nil then font_loading:Dispose() ; font_loading = nil end
    if font_menu    ~= nil then font_menu:Dispose()    ; font_menu    = nil end
    standard_dan.destroy()
    pagoda.destroy()
end

local function _load_menu_chara()
    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL) end
end

function activate()
    CONFIG.PlayerCount = 1
    CONFIG.SongSpeed   = 20   -- reset speed (pagoda may have changed it)

    _load_menu_chara()

    -- Load shared resources
    tx_bg   = TEXTURE:CreateTexture(TX .. "Background.png")
    tx_door = TEXTURE:CreateTexture(TX .. "Door.png")

    snd_entry = SOUND:CreateSFX(SND .. "Entry.ogg")
    snd_bgm   = SOUND:CreateBGM(SND .. "BGM.ogg")

    -- Puchi sine bob
    puchi_sine_counter = COUNTER:CreateCounter(0, 360, 1 / 120)
    puchi_sine_counter:SetLoop(true)
    puchi_sine_counter:Start()

    -- ── Returning from pagoda play ─────────────────────────────────────────────
    if pagoda.is_returning_from_play() then
        door_done = true
        state     = "pagoda"
        pagoda.activate(shared_callbacks)
        pagoda.on_return(shared_callbacks)
        startBGM()
        return
    end

    -- ── Returning from standard dan play ──────────────────────────────────────
    if standard_dan.is_returning_from_play() then
        door_done = true
        state     = "standard_dan"
        standard_dan.enter(shared_callbacks, true)
        startBGM()
        return
    end

    -- ── Re-entering after cancel (door already shown) ─────────────────────────
    if intro_shown then
        door_done       = true
        door_post_delay = DOOR_POST_DELAY_SEC
        if song_enum_done then
            state = "menu_3way"
            startBGM()
        else
            state = "loading"
        end
        return
    end

    -- ── First time entry: door intro ──────────────────────────────────────────
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

function deactivate()
    stopBGM()

    -- Deactivate the active sub-module (if any)
    if state == "standard_dan" then
        standard_dan.deactivate()
    elseif state == "pagoda" then
        pagoda.deactivate()
    end

    if tx_bg   ~= nil then tx_bg:Dispose()   ; tx_bg   = nil end
    if tx_door ~= nil then tx_door:Dispose()  ; tx_door = nil end

    if snd_entry ~= nil then snd_entry:Dispose() ; snd_entry = nil end
    if snd_bgm   ~= nil then snd_bgm:Dispose()   ; snd_bgm   = nil end

    puchi_sine_counter = nil
    bg_zoom_counter    = nil
    menu_exit_counter  = nil
end

function afterSongEnum()
    song_enum_done = true
    standard_dan.afterSongEnum()
    pagoda.afterSongEnum()

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

-- ── Update ────────────────────────────────────────────────────────────────────

function update()
    local dt = fps.deltaTime

    -- Puchi sine bob (drives menu + any state that needs it)
    if puchi_sine_counter ~= nil then
        puchi_sine_counter:Tick()
        puchi_sine_y = math.sin(puchi_sine_counter.Value * math.pi / 180) * PUCHI_FLOAT_AMP
    end

    -- F3 auto toggle (always available)
    if INPUT:KeyboardPressed("F3") then
        CONFIG:SetAutoStatus(0, not CONFIG:GetAutoStatus(0))
    end

    -- ── Sub-module states ──────────────────────────────────────────────────────

    if state == "standard_dan" then
        local result = standard_dan.update(dt)
        if result == "back" then
            standard_dan.leave()
            state = "menu_3way"
            _load_menu_chara()
        elseif result == "play" then
            -- standard_dan already called stopBGM() and set _in_play = true
            return Exit("play", nil)
        end
        return
    end

    if state == "pagoda" then
        local result = pagoda.update(dt)
        if result == "back" then
            pagoda.leave()
            state = "menu_3way"
            startBGM()
            _load_menu_chara()
        elseif result == "play" then
            -- pagoda already called stopBGM() and set _in_challenge/_in_practice
            return Exit("play", nil)
        end
        return
    end

    -- ── LOADING ───────────────────────────────────────────────────────────────
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
            door_opening    = true
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
                menu_exit_target  = "standard_dan"
                state             = "menu_3way_exit"
                menu_exit_y       = 0.0
                menu_exit_counter = COUNTER:CreateCounterDuration(0.0, 1080.0, 0.4)
                menu_exit_counter:SetEasing("IN", "QUAD")
                menu_exit_counter:Start()
            elseif menu_sel == 2 then
                menu_exit_target  = "pagoda"
                state             = "menu_3way_exit"
                menu_exit_y       = 0.0
                menu_exit_counter = COUNTER:CreateCounterDuration(0.0, 1080.0, 0.4)
                menu_exit_counter:SetEasing("IN", "QUAD")
                menu_exit_counter:Start()
            end
            -- Option 3 (Forest of Strata): not yet implemented
        end
        return
    end

    -- ── MENU 3-WAY EXIT ───────────────────────────────────────────────────────
    if state == "menu_3way_exit" then
        if bg_zoom_counter    ~= nil then bg_zoom_counter:Tick() end
        if menu_exit_counter  ~= nil then
            menu_exit_counter:Tick()
            menu_exit_y = menu_exit_counter.Value
            if menu_exit_y >= 1080.0 then
                menu_exit_counter = nil
                if menu_exit_target == "standard_dan" then
                    state = "standard_dan"
                    standard_dan.enter(shared_callbacks, false)
                elseif menu_exit_target == "pagoda" then
                    state = "pagoda"
                    pagoda.enter(shared_callbacks)
                end
                menu_exit_target = nil
            end
        end
        return
    end
end

-- ── Draw ─────────────────────────────────────────────────────────────────────

function draw()
    local res   = THEME:GetResolution()
    local res_w = res.X
    local res_h = res.Y

    -- ── LOADING ───────────────────────────────────────────────────────────────
    if state == "loading" then
        if font_loading ~= nil then
            font_loading:GetText("Please wait for song enumeration to complete...", false, 900)
                :DrawAtAnchor(res_w / 2, res_h / 2, "center")
        end
        return
    end

    -- ── BACKGROUND (all post-loading states) ──────────────────────────────────
    if tx_bg ~= nil and tx_bg.Loaded then
        tx_bg:SetScale(bg_zoom, bg_zoom)
        tx_bg:DrawAtAnchor(res_w / 2, res_h / 2, "center")
        tx_bg:SetScale(1.0, 1.0)
    end

    -- ── DOOR (intro only) ─────────────────────────────────────────────────────
    if not door_done and tx_door ~= nil and tx_door.Loaded then
        local dw   = tx_door.Width
        local dh   = tx_door.Height
        local half = dw / 2
        if not door_opening then
            tx_door:Draw(0, 0)
        else
            local offset = math.floor(door_open_t * half)
            tx_door:DrawRectAtAnchor(-offset,      0,    0, 0, half, dh, "topleft")
            tx_door:DrawRectAtAnchor(half + offset, 0, half, 0, half, dh, "topleft")
        end
    end

    if state == "intro" then return end

    -- ── Sub-module draw (standard_dan / pagoda) ────────────────────────────────
    if state == "standard_dan" then
        standard_dan.draw()
        return
    end

    if state == "pagoda" then
        pagoda.draw()
        return
    end

    -- ── 3-WAY MENU (menu_3way + menu_3way_exit) ───────────────────────────────
    if state == "menu_3way" or state == "menu_3way_exit" then
        local menu_y_off = (state == "menu_3way_exit") and menu_exit_y or 0.0

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
            local oy     = opt_y_start + (i - 1) * opt_gap + menu_y_off
            local is_sel = (i == menu_sel)

            if font_menu ~= nil then
                local t = font_menu:GetText(label, false, 900)
                t:SetOpacity(is_sel and 1.0 or 0.5)
                t:SetColor(is_sel and 1.0 or 0.7, is_sel and 1.0 or 0.7, is_sel and 1.0 or 0.7)
                t:DrawAtAnchor(opt_x, oy, "center")
                t:SetColor(1.0, 1.0, 1.0)
            end

            if is_sel and font_loading ~= nil then
                font_loading:GetText(descriptions[i], false, 800):DrawAtAnchor(opt_x, oy + 48, "center")
            end
        end

        NAMEPLATE:DrawPlayerNameplate(NP_X, NP_Y, 255, 0)
        drawPlayerChara(NP_X + 140, NP_Y - 6,            1.0)
        drawPlayerPuchi(NP_X + 220, NP_Y + puchi_sine_y, 1.0)
    end
end
