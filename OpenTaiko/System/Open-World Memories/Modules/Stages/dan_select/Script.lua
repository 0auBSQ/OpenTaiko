-- dan select stage

local standard_dan = require("standard_dan")

local NavInput     = require("NavInput")
local Easing       = require("Easing")
local EM           = require("EventMode")
local PagodaAvatar = require("PagodaAvatar")
-- layout: Config/layout.json; timings: Config/animation.json
local Config       = require("Core.Config")
local Layout       = Config.load("Config/layout.json")
local Animation    = Config.load("Config/animation.json")

local TX  = "Textures/"
local SND = "Sounds/"

-- ==== Constants ====

local Player = Layout.player
local NP_X = Player.x
local NP_Y = Player.y

local Puchi = Animation.puchi
local PUCHI_FLOAT_AMP = Puchi.float_px
local PUCHI_N_FRAMES = Puchi.frames

-- two boards, exit pill below
local Menu = Layout.menu
local MenuAnim = Animation.menu
local CARD_CX   = Menu.card_cx
local CARD_CY   = Menu.card_cy
local CARD_W, CARD_H = Menu.card_w, Menu.card_h
local BAND_TOP, BAND_BOTTOM = Menu.band_top, Menu.band_bottom   -- text band in card_*.png
local DESC_W    = Menu.desc_w
local TITLE_GAP = Menu.title_gap                      -- title to description gap
local EXIT_CX, EXIT_CY = Menu.exit_cx, Menu.exit_cy
local EXIT_W, EXIT_H = Menu.exit_w, Menu.exit_h
local HOVER_RATE = MenuAnim.hover_rate_per_s           -- hover glow rate per s
local SLIDE_IN_SEC  = MenuAnim.slide_in_s      -- s, board fly-in
local SLIDE_OUT_SEC = MenuAnim.slide_out_s      -- s, fly-out
local CARD_STAGGER  = MenuAnim.card_stagger_s      -- s, right board delay
local EXIT_FADE_SEC = MenuAnim.exit_fade_s       -- s, exit pill fade
local ENTRY_DELAY_SEC = MenuAnim.entry_delay_s     -- s, wait for the doors
local OFF_X = MenuAnim.off_x_px              -- px off screen
local OUT_HOLD_SEC = MenuAnim.out_hold_s        -- s, empty dojo pause before the choice runs

-- ==== Locale ====
-- Locales/<code>/ via THEME; English fallback in code

local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

-- ==== Forward declarations ====

local startBGM, stopBGM

-- ==== State ====

--[[
  "loading"      waiting for afterSongEnum
  "menu"         boards in, waiting for a choice
  "menu_out"     boards out, then the choice runs
  "leaving"      Exit called, doors closing
  "standard_dan" standard_dan module active
]]
local state = "loading"
local active = false             -- afterSongEnum fires for every stage: no music when inactive

local song_enum_done = false

-- 1 standard dan, 2 pagoda, 3 exit
local menu_sel   = 1
local menu_hi    = { 0, 0, 0 }   -- per-entry glow, eased to 1 when selected
local menu_t     = 0.0           -- ring pulse clock
local mouse_over = nil           -- hovered entry (last mouse move)
local anim_t     = 0.0           -- s into the current flight
local anim_dir   = "in"          -- "in" | "out"
local anim_delay = 0.0           -- fly-in delay (doors opening)
local out_target = nil           -- "standard" | "pagoda" | "title", run after fly-out
local to_pagoda  = false         -- doors closing onto the pagoda

-- dojo BGM fade on the way out
local exiting     = false
local dan_bgm_vol = 100.0

-- Event Mode: event_timer ROActivity; event_over = the allowed dan play is done
local event_timer = nil
local event_over  = false

local function startEventTimer()
    event_timer = ROACTIVITY:GetROActivity("event_timer")
    if event_timer ~= nil then event_timer:Activate(EM.selectSeconds(), "dan") end
end

local function stopEventTimer()
    if event_timer ~= nil and event_timer.IsActive then event_timer:Deactivate() end
end

-- ==== Textures / sounds ====

local tx_bg = nil
local tx_menu = {}               -- card_standard, card_pagoda, card_hover, exit, exit_hover

local snd_bgm = nil

-- ==== Fonts ====

local font_loading = nil
local loading_text = "Please wait for song enumeration to complete..."   -- localized in activate()
local gfont_title, gfont_desc, gfont_exit = nil, nil, nil
local col_ink, col_none = nil, nil

-- ==== Shared callbacks ====

startBGM = function()
    if snd_bgm ~= nil and not snd_bgm.IsPlaying then
        snd_bgm:SetLoop(true)
        snd_bgm:SetVolumePercent(100)
        snd_bgm:Play()
    end
end

stopBGM = function()
    if snd_bgm ~= nil and snd_bgm.IsPlaying then snd_bgm:Stop() end
end

local CB = {
    startBGM = startBGM,
    stopBGM  = stopBGM,

    ctx = {},

    -- preview state
    puchiSineY          = 0,
    puchiIdxFrame       = 0,
}

-- ==== Shared utilities (CB) ====

-- kept in CB.ctx against GC
function CB.startCounter(key, startVal, endVal, interval, mode, updateCallback, onFinish)
    local c = COUNTER:CreateCounter(startVal, endVal, interval, onFinish)
    if mode == "loop"   then c:SetLoop(true)
    elseif mode == "bounce" then c:SetBounce(true) end
    if updateCallback then c:Listen(updateCallback) end
    c:Start()
    CB.ctx[key] = c
    return c
end

-- ==== Draw helpers ====

function CB.drawPlayerChara(x, y, opacity)
    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then
        chara:Update(CHARACTER.ANIM_MENU_NORMAL, true)
        chara:DrawAtAnchor(x, y, CHARACTER.ANIM_MENU_NORMAL, "bottom", 1.0, 1.0, math.floor(opacity * 255))
    end
end

function CB.drawPlayerPuchi(x, y, opacity, idxFrame)
    local puchi = GetSaveFile(0):GetPuchichara()
    if puchi == nil or puchi.tx == nil or not puchi.tx.Loaded then return end
    local frameW = math.floor(puchi.tx.Width / PUCHI_N_FRAMES)
    idxFrame = idxFrame or 0
    puchi.tx:SetScale(1.0, 1.0)
    puchi.tx:SetOpacity(opacity)
    puchi.tx:DrawRectAtAnchor(x, y, idxFrame * frameW, 0, frameW, puchi.tx.Height, "bottom")
    puchi.tx:SetOpacity(1.0)
end

-- ==== Menu entries ====

local function menuEntries()
    local e = {
        { kind = "card", tex = "card_standard", cx = CARD_CX[1], cy = CARD_CY, side = -1,
          title = tr("DANSELECT_STANDARD", "Standard Dan Challenge"),
          desc  = tr("DANSELECT_STANDARD_DESC", "Prove your skills through the dojo's selection!") },
        { kind = "card", tex = "card_pagoda", cx = CARD_CX[2], cy = CARD_CY, side = 1,
          title = tr("PAGODA_TITLE", "Pagoda of the Unknown"),
          desc  = tr("DANSELECT_PAGODA_DESC", "Go through multiple randomized dans in a row!") },
    }
    -- Event Mode play limit: no exit entry
    if EM.canLeave() then
        e[#e + 1] = { kind = "exit", tex = "exit", cx = EXIT_CX, cy = EXIT_CY, title = tr("DANSELECT_EXIT", "Exit") }
    end
    return e
end
local entries = nil

local function entryAt(mx, my)
    for i, e in ipairs(entries) do
        local w, h = (e.kind == "card") and CARD_W or EXIT_W, (e.kind == "card") and CARD_H or EXIT_H
        if mx >= e.cx - w / 2 and mx <= e.cx + w / 2 and my >= e.cy - h / 2 and my <= e.cy + h / 2 then return i end
    end
    return nil
end

local function selectEntry(i)
    i = ((i - 1) % #entries) + 1
    if i == menu_sel then return end
    menu_sel = i
    SHARED:GetSharedSound("Move"):Play()
end

-- board flight: 0 on screen, 1 off screen; exit pill opacity follows
local function flight()
    if anim_dir == "in" then
        local t = anim_t - anim_delay
        local left  = 1 - Easing.outBack(Easing.span(t, 0, SLIDE_IN_SEC), MenuAnim.overshoot)
        local right = 1 - Easing.outBack(Easing.span(t, CARD_STAGGER, SLIDE_IN_SEC), MenuAnim.overshoot)
        local exitA = Easing.outQuad(Easing.span(t, SLIDE_IN_SEC * MenuAnim.exit_fade_start, EXIT_FADE_SEC))
        return left, right, exitA, t >= SLIDE_IN_SEC + CARD_STAGGER
    else
        local t = anim_t
        local left  = Easing.inCubic(Easing.span(t, CARD_STAGGER, SLIDE_OUT_SEC))
        local right = Easing.inCubic(Easing.span(t, 0, SLIDE_OUT_SEC))
        local exitA = 1 - Easing.outQuad(Easing.span(t, 0, EXIT_FADE_SEC))
        return left, right, exitA, t >= SLIDE_OUT_SEC + CARD_STAGGER + OUT_HOLD_SEC
    end
end

local function flyIn(delay)
    anim_dir, anim_t, anim_delay = "in", 0, delay or 0
    menu_hi = { 0, 0, 0 }
    mouse_over = nil
    menu_sel = math.min(menu_sel, #entries)
end

local function flyOut(target)
    anim_dir, anim_t, out_target = "out", 0, target
    state = "menu_out"
end

-- ==== Lifecycle ====

function onStart()
    local Fonts = Layout.fonts
    font_loading = TEXT:Create(Fonts.loading, "regular")
    gfont_title  = TEXT:CreateGlyphCached(Fonts.menu_title)
    gfont_desc   = TEXT:CreateGlyphCached(Fonts.menu_desc)
    gfont_exit   = TEXT:CreateGlyphCached(Fonts.menu_exit)
    col_ink  = Config.color(Layout.colors.ink_color)
    col_none = Config.color(Layout.colors.clear_color)

    -- loaded with the skin: ready on the first frame behind the doors
    tx_bg = TEXTURE:CreateTexture(TX .. "Background.png")
    for _, n in ipairs({ "card_standard", "card_pagoda", "card_hover", "exit", "exit_hover" }) do
        tx_menu[n] = TEXTURE:CreateTexture(TX .. "Menu/" .. n .. ".png")
    end
    snd_bgm = SOUND:CreateBGM(SND .. "BGM.ogg")
    standard_dan.init()       -- dan list art too
end

function onDestroy()
    if font_loading ~= nil then font_loading:Dispose() ; font_loading = nil end
    for _, f in ipairs({ gfont_title, gfont_desc, gfont_exit }) do if f ~= nil then f:Dispose() end end
    gfont_title, gfont_desc, gfont_exit = nil, nil, nil
    if tx_bg ~= nil then tx_bg:Dispose() ; tx_bg = nil end
    for n, t in pairs(tx_menu) do t:Dispose() ; tx_menu[n] = nil end
    if snd_bgm ~= nil then snd_bgm:Dispose() ; snd_bgm = nil end
    standard_dan.destroy()
end

local function _load_menu_chara()
    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL) end
end

function activate()
    CONFIG.PlayerCount = 1
    -- reset speed (pagoda may change it), kept after a dan play
    if not standard_dan.is_returning_from_play() then CONFIG.SongSpeed = 20 end
    PagodaAvatar.leave()      -- restore P1 character and puchichara

    active      = true
    exiting     = false
    dan_bgm_vol = 100.0
    entries = menuEntries()   -- rebuilt per entry (language)
    loading_text = tr("VAULT_LOADING", "Please wait for song enumeration to complete...")
    menu_t, out_target, to_pagoda = 0, nil, false
    -- dan_doors title time (s)
    local title_sec = tonumber(SHARED:GetSharedString("dan_doors_title")) or 0
    SHARED:SetSharedString("dan_doors_title", "")
    flyIn(ENTRY_DELAY_SEC + title_sec)
    event_timer, event_over = nil, false

    _load_menu_chara()

    CB.startCounter("puchi_sine", 0, 360, Puchi.float_period_s / 360, "loop", function(val)
        CB.puchiSineY = math.sin(val * math.pi / 180) * PUCHI_FLOAT_AMP
    end)
    CB.startCounter("puchi_frame", 0, 1, Puchi.frame_cycle_s, "loop", function(val)
        CB.puchiIdxFrame = math.floor(val * PUCHI_N_FRAMES)
    end)

    -- ==== Back from a dan play ====
    if standard_dan.is_returning_from_play() then
        -- Event Mode: one play, then the thank-you screen
        if EM.on() then
            EM.resetMods()
            if EM.registerPlay("dan") then
                event_over, state = true, "leaving"
                return
            end
        end
        state = "standard_dan"
        standard_dan.enter(CB, true)
        startBGM()
        if EM.on() then startEventTimer() end
        return
    end
    if EM.on() then startEventTimer() end

    -- ==== Enter the menu ====
    -- Event Mode without the pagoda skips the boards
    if song_enum_done then
        if EM.on() and not EM.pagodaEnabled() then
            state = "standard_dan"
            standard_dan.enter(CB, false)
        else
            state = "menu"
        end
        startBGM()
    else
        state = "loading"
    end
end

function deactivate()
    active = false
    stopBGM()
    stopEventTimer()

    -- into the pagoda: P1 becomes the story student
    if to_pagoda then PagodaAvatar.enter() end

    if state == "standard_dan" then
        standard_dan.deactivate()
    end

    for k in pairs(CB.ctx) do CB.ctx[k] = COUNTER:EmptyCounter() end
end

function afterSongEnum()
    song_enum_done = true
    standard_dan.afterSongEnum()

    -- inactive stages pick their state in activate
    if active and state == "loading" then
        if EM.on() and not EM.pagodaEnabled() then
            state = "standard_dan"
            standard_dan.enter(CB, false)
        else
            state = "menu"
            flyIn(0)
        end
        startBGM()
    end
end

-- ==== Update ====

local function leaveToTitle()
    if not EM.canLeave() then SHARED:GetSharedSound("Error"):Play(); return end
    SHARED:GetSharedSound("Cancel"):Play()
    flyOut("title")
end

local function decide()
    if menu_sel == 1 then
        SHARED:GetSharedSound("Decide"):Play()
        flyOut("standard")
    elseif menu_sel == 2 then
        SHARED:GetSharedSound("Decide"):Play()
        flyOut("pagoda")
    else
        leaveToTitle()
    end
end

-- runs the choice after fly-out
local function afterFlyOut()
    if out_target == "standard" then
        state = "standard_dan"
        standard_dan.enter(CB, false)
    elseif out_target == "pagoda" then
        -- pagoda is its own stage (doors close over dan_select)
        exiting, state, to_pagoda = true, "leaving", true
        return Exit("stage", "pagoda", "dan_doors_back")
    else
        exiting, state = true, "leaving"
        return Exit("title", nil, "dan_doors_back")
    end
end

local function tickGlow(dt)
    menu_t = menu_t + dt
    for i = 1, #entries do
        local target = (i == menu_sel) and 1 or 0
        menu_hi[i] = menu_hi[i] + (target - menu_hi[i]) * math.min(1, dt * HOVER_RATE)
    end
end

function update()
    local dt = fps.deltaTime
    for _, c in pairs(CB.ctx) do c:Tick() end

    -- Event Mode: last play done, thank-you screen
    if event_over then
        event_over = false
        return Exit("stage", "event_thanks")
    end

    -- Event Mode countdown: expiry sends to the title
    if event_timer ~= nil and event_timer.IsActive and (state == "menu" or state == "menu_out" or state == "standard_dan") then
        if EM.signal(event_timer:Update()) == "expired" then
            if state == "standard_dan" then standard_dan.leave() end
            exiting, state = true, "leaving"
            return Exit("title", nil, "dan_doors_back")
        end
    end

    -- F3 auto toggle (not in Event Mode; the dan list has its own)
	local navPn = NavInput.p[1]
    if INPUT:Pressed("ToggleAutoP1") and not EM.on() and state ~= "standard_dan" then
        CONFIG:SetAutoStatus(0, not CONFIG:GetAutoStatus(0))
        SHARED:GetSharedSound("Move"):Play()
    end

    -- ==== Sub-module states ====

    if state == "standard_dan" then
        local result = standard_dan.update(dt)
        if result == "back" and EM.on() and not EM.pagodaEnabled() then
            -- no boards: leave for the title when allowed
            if EM.canLeave() then
                standard_dan.leave()
                exiting, state = true, "leaving"
                return Exit("title", nil, "dan_doors_back")
            end
        elseif result == "back" then
            standard_dan.leave()
            state = "menu"
            flyIn(0)
            _load_menu_chara()
        elseif result == "play" then
            -- standard_dan already called stopBGM() and set _in_play = true
            stopEventTimer()
            return Exit("play", nil)
        end
        return
    end

    -- ==== Loading ====
    if state == "loading" then
        if navPn.cancel() and EM.canLeave() then
            exiting, state = true, "leaving"
            return Exit("title", nil, "dan_doors_back")
        end
        return
    end
    if state == "leaving" then return end

    -- ==== Menu ====
    if state == "menu" then
        anim_t = anim_t + dt
        tickGlow(dt)
        local _, _, _, landed = flight()

        if navPn.cancel() then return leaveToTitle() end
        if not landed then return end   -- input after landing

        -- entries loop
        if navPn.upOrPadLeft() or navPn.leftKeyboard() then selectEntry(menu_sel - 1)
        elseif navPn.downOrPadRight() or navPn.rightKeyboard() then selectEntry(menu_sel + 1) end

        -- mouse move hovers, click decides
        local mdx, mdy = INPUT:GetMouseDelta()
        if (mdx ~= 0 or mdy ~= 0) and INPUT:IsMouseInside() then
            mouse_over = entryAt(INPUT:GetMouseXY())
            if mouse_over ~= nil then selectEntry(mouse_over) end
        end
        local clicked = INPUT:MousePressed("Left") and mouse_over ~= nil and entryAt(INPUT:GetMouseXY()) == menu_sel

        if navPn.decide() or clicked then return decide() end
        return
    end

    -- ==== Menu out ====
    if state == "menu_out" then
        anim_t = anim_t + dt
        tickGlow(dt)
        local _, _, _, gone = flight()
        if gone then return afterFlyOut() end
        return
    end
end

-- ==== Draw ====

local function drawTex(name, cx, cy, scale, opacity, tint)
    local t = tx_menu[name]
    if t == nil or not t.Loaded or opacity <= 0.001 then return end
    t:SetScale(scale, scale)
    t:SetOpacity(opacity)
    if tint ~= nil then t:SetColor(tint, tint, tint) end
    t:DrawAtAnchor(cx, cy, "center")
    t:SetScale(1, 1) ; t:SetOpacity(1) ; t:SetColor(1, 1, 1)
end

-- title + description block, centred in the text band
local function drawBandText(e, cx, cy, s, opacity)
    local lines = gfont_desc:WrapToLines(e.desc, DESC_W)
    local titleH = gfont_title.LineHeight
    local descH  = gfont_desc.LineHeight
    local blockH = titleH + TITLE_GAP + lines.Length * descH
    local bandCy = cy + ((BAND_TOP + BAND_BOTTOM) / 2 - CARD_H / 2) * s
    local y = bandCy - blockH / 2 * s
    gfont_title:Draw(e.title, cx, y, col_ink, col_none, opacity, s, DESC_W * s, "top")
    y = y + (titleH + TITLE_GAP) * s
    for i = 0, lines.Length - 1 do
        gfont_desc:Draw(lines[i], cx, y + i * descH * s, col_ink, col_none, opacity, s, 0, "top")
    end
end

-- middle-anchored glyph ink sits high by half the bottom padding
local function textNudge(gf)
    return math.floor((gf.BoxHeight - math.ceil(gf.LineHeight)) / 2) - 1
end

local function drawMenu()
    local Ring = MenuAnim.ring_pulse
    local pulse = Ring.base + Ring.amp * math.sin(menu_t * Ring.rad_per_s)
    local left, right, exitA = flight()
    for i, e in ipairs(entries) do
        local hi = menu_hi[i]
        if e.kind == "card" then
            local k = (e.side < 0) and left or right
            local cx = e.cx + e.side * OFF_X * k
            local s = 1 + Menu.card_grow * hi
            drawTex("card_hover", cx, e.cy, s, hi * pulse)
            drawTex(e.tex, cx, e.cy, s, 1, Menu.card_shade + (1 - Menu.card_shade) * hi)
            drawBandText(e, cx, e.cy, s, 1)
        else
            local s = 1 + Menu.exit_grow * hi
            drawTex("exit_hover", e.cx, e.cy, s, hi * pulse * exitA)
            drawTex("exit", e.cx, e.cy, s, exitA, Menu.exit_shade + (1 - Menu.exit_shade) * hi)
            if exitA > 0.001 then
                gfont_exit:Draw(e.title, e.cx, e.cy + textNudge(gfont_exit) * s, col_ink, col_none, exitA, s, (EXIT_W - Menu.exit_text_inset) * s, "center")
            end
        end
    end
end

function draw()
    local res   = THEME:GetResolution()
    local res_w = res.X
    local res_h = res.Y

    -- BGM fade in draw(): update() stops at Exit(), draw() runs through the fade-out
    -- bgm_fade_s = dan_doors close
    if exiting and snd_bgm ~= nil then
        dan_bgm_vol = math.max(0, dan_bgm_vol - fps.deltaTime / Animation.bgm_fade_s * 100)
        snd_bgm:SetVolumePercent(dan_bgm_vol)
    end

    -- ==== Loading ====
    if state == "loading" then
        if font_loading ~= nil then
            font_loading:GetText(loading_text, false, Layout.loading.max_width)
                :DrawAtAnchor(res_w / 2, res_h / 2, "center")
        end
        return
    end
    if event_over then return end   -- thank-you screen next: draw nothing

    -- ==== Background ====
    if tx_bg ~= nil and tx_bg.Loaded then
        tx_bg:DrawAtAnchor(res_w / 2, res_h / 2, "center")
    end

    -- ==== standard_dan ====
    if state == "standard_dan" then
        standard_dan.draw()
        if event_timer ~= nil and event_timer.IsActive then event_timer:Draw() end
        return
    end

    -- ==== Menu ====
    if state == "menu" or state == "menu_out" then
        drawMenu()

        NAMEPLATE:DrawPlayerNameplate(NP_X, NP_Y, 255, 0)
        CB.drawPlayerChara(NP_X + Player.chara_dx, NP_Y + Player.chara_dy, 1.0)
        CB.drawPlayerPuchi(NP_X + Player.puchi_dx, NP_Y + CB.puchiSineY, 1.0, CB.puchiIdxFrame)
    end
    if event_timer ~= nil and event_timer.IsActive and state ~= "leaving" then event_timer:Draw() end
end
