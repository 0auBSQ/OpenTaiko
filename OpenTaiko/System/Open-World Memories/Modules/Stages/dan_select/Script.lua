-- ═══════════════════════════════════════════════════════════════════════════════
-- dan_select / Script.lua
-- ═══════════════════════════════════════════════════════════════════════════════

local standard_dan = require("standard_dan")

local NavInput     = require("NavInput")
local Easing       = require("Easing")
local EM           = require("EventMode")

local TX  = "Textures/"
local SND = "Sounds/"

-- ── Constants ────────────────────────────────────────────────────────────────

local NP_X = 20
local NP_Y = 980

local PUCHI_FLOAT_AMP = 6.0
local PUCHI_N_FRAMES = 2

-- the two boards side by side, the exit pill under them (the art's sizes: 600x540, 320x84)
local CARD_CX   = { 600, 1320 }
local CARD_CY   = 560
local CARD_W, CARD_H = 600, 540
local BAND_TOP, BAND_BOTTOM = 352, 506   -- the text band inside card_*.png (title, then the description)
local DESC_W    = 520
local TITLE_GAP = 6                      -- between the title's line and the description's first line
local EXIT_CX, EXIT_CY = 960, 930
local EXIT_W, EXIT_H = 320, 84
local HOVER_RATE = 12           -- how fast the selection glow follows the cursor
local SLIDE_IN_SEC  = 0.55      -- a board's flight in from its side of the screen
local SLIDE_OUT_SEC = 0.38      -- and back out
local CARD_STAGGER  = 0.09      -- the right board follows the left one by this much
local EXIT_FADE_SEC = 0.3       -- the exit pill fades after the boards
local ENTRY_DELAY_SEC = 0.5     -- entering through the doors: the boards wait for them to open
                                -- (dan_doors FADE_IN_SECONDS is 0.7; update() runs from its start)
local OFF_X = 1000              -- a board's resting offset off screen: the left one (centre 600, half
                                -- width 300 plus the ring and the hover scale) ends 100 px past the
                                -- edge, the right one likewise
local OUT_HOLD_SEC = 0.1        -- a beat with the dojo empty before what was chosen happens

-- ── Locale ────────────────────────────────────────────────────────────────────
-- the skin's Locales/<code>.json through THEME; the English text stays in the code as the fallback

local function tr(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

-- ── Forward declarations ──────────────────────────────────────────────────────

local startBGM, stopBGM

-- ── State ────────────────────────────────────────────────────────────────────

--[[
  "loading"      – waiting for afterSongEnum
  "menu"         – the boards fly in, then wait for a choice
  "menu_out"     – the boards fly out; what was chosen happens once they are gone
  "leaving"      – Exit was called; the doors close over the empty dojo
  "standard_dan" – delegating to standard_dan module
  (Pagoda of the Unknown is its own stage, entered via the dan_doors transition.)
]]
local state = "loading"
local active = false             -- between activate and deactivate: afterSongEnum fires for every stage,
                                 -- active or not, and must not start this one's music under another

local song_enum_done = false

-- the menu: 1 standard dan, 2 pagoda, 3 exit
local menu_sel   = 1
local menu_hi    = { 0, 0, 0 }   -- each entry's selection glow, eased toward 1 when selected
local menu_t     = 0.0           -- for the ring's pulse
local mouse_over = nil           -- the entry under the cursor, from the last mouse move
local anim_t     = 0.0           -- time into the current in or out flight
local anim_dir   = "in"          -- "in" | "out"
local anim_delay = 0.0           -- the in flight waits this long (the doors opening)
local out_target = nil           -- "standard" | "pagoda" | "title", acted on once the out flight ends

-- Fade the dojo BGM out as the doors close on the way out (to the Pagoda stage or the title)
local exiting     = false
local dan_bgm_vol = 100.0

-- Event Mode: the countdown to the title while a dan is being chosen (the event_timer ROActivity, with
-- the plays-left hud), and whether the one allowed dan play is done (the thank-you screen follows)
local event_timer = nil
local event_over  = false

local function startEventTimer()
    event_timer = ROACTIVITY:GetROActivity("event_timer")
    if event_timer ~= nil then event_timer:Activate(EM.selectSeconds(), "dan") end
end

local function stopEventTimer()
    if event_timer ~= nil and event_timer.IsActive then event_timer:Deactivate() end
end

-- ── Textures / sounds ─────────────────────────────────────────────────────────

local tx_bg = nil
local tx_menu = {}               -- card_standard, card_pagoda, card_hover, exit, exit_hover

local snd_bgm = nil

-- ── Fonts ────────────────────────────────────────────────────────────────────

local font_loading = nil
local gfont_title, gfont_desc, gfont_exit = nil, nil, nil
local col_ink, col_none = nil, nil

-- ── Shared callbacks passed to sub-modules ────────────────────────────────────

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

    -- Counters
    ctx = {},

    -- Preview state
    puchiSineY          = 0,
    puchiIdxFrame       = 0,
}

-- ── Shared utility functions (stored in CB so every module can call them) ──────

-- Counter helper — wraps COUNTER:CreateCounter and stores in CB.ctx to prevent GC.
function CB.startCounter(key, startVal, endVal, interval, mode, updateCallback, onFinish)
    local c = COUNTER:CreateCounter(startVal, endVal, interval, onFinish)
    if mode == "loop"   then c:SetLoop(true)
    elseif mode == "bounce" then c:SetBounce(true) end
    if updateCallback then c:Listen(updateCallback) end
    c:Start()
    CB.ctx[key] = c
    return c
end

-- ── Draw helpers ──────────────────────────────────────────────────────────────

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

-- ── The menu entries ──────────────────────────────────────────────────────────

local function menuEntries()
    local e = {
        { kind = "card", tex = "card_standard", cx = CARD_CX[1], cy = CARD_CY, side = -1,
          title = tr("DANSELECT_STANDARD", "Standard Dan Challenge"),
          desc  = tr("DANSELECT_STANDARD_DESC", "Prove your skills through the dojo's selection!") },
        { kind = "card", tex = "card_pagoda", cx = CARD_CX[2], cy = CARD_CY, side = 1,
          title = tr("PAGODA_TITLE", "Pagoda of the Unknown"),
          desc  = tr("DANSELECT_PAGODA_DESC", "Go through multiple randomized dans in a row!") },
    }
    -- Event Mode with a play limit: no way out of the dojo but the countdown or the play itself
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

-- the boards' flight: 0 = at rest on screen, 1 = off screen on their side; the exit pill's opacity
-- follows the boards, fading last on the way in and first on the way out
local function flight()
    if anim_dir == "in" then
        local t = anim_t - anim_delay
        local left  = 1 - Easing.outBack(Easing.span(t, 0, SLIDE_IN_SEC), 1.15)
        local right = 1 - Easing.outBack(Easing.span(t, CARD_STAGGER, SLIDE_IN_SEC), 1.15)
        local exitA = Easing.outQuad(Easing.span(t, SLIDE_IN_SEC * 0.6, EXIT_FADE_SEC))
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

-- ── Lifecycle ────────────────────────────────────────────────────────────────

function onStart()
    font_loading = TEXT:Create(30, "regular")
    gfont_title  = TEXT:CreateGlyphCached(40)
    gfont_desc   = TEXT:CreateGlyphCached(24)
    gfont_exit   = TEXT:CreateGlyphCached(30)
    col_ink  = COLOR:CreateColorFromRGBA(76, 40, 32, 255)
    col_none = COLOR:CreateColorFromRGBA(0, 0, 0, 0)

    -- the background, the menu art and the music stay loaded with the skin, like the title's: the
    -- stage then has everything on its very first frame behind the opening doors
    tx_bg = TEXTURE:CreateTexture(TX .. "Background.png")
    for _, n in ipairs({ "card_standard", "card_pagoda", "card_hover", "exit", "exit_hover" }) do
        tx_menu[n] = TEXTURE:CreateTexture(TX .. "Menu/" .. n .. ".png")
    end
    snd_bgm = SOUND:CreateBGM(SND .. "BGM.ogg")
    standard_dan.init()       -- the dan list's own art too, so choosing it costs nothing
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
    CONFIG.SongSpeed   = 20   -- reset speed (pagoda may have changed it)

    active      = true
    exiting     = false
    dan_bgm_vol = 100.0
    entries = menuEntries()   -- built on every entry so the texts follow the language
    menu_t, out_target = 0, nil
    flyIn(ENTRY_DELAY_SEC)
    event_timer, event_over = nil, false

    _load_menu_chara()

    CB.startCounter("puchi_sine", 0, 360, 1/120, "loop", function(val)  -- 1/3 cycles/s
        CB.puchiSineY = math.sin(val * math.pi / 180) * PUCHI_FLOAT_AMP
    end)
    CB.startCounter("puchi_frame", 0, 1, 4.8, "loop", function(val)  -- 1/4.8 cycles/s
        CB.puchiIdxFrame = math.floor(val * PUCHI_N_FRAMES)
    end)

    -- ── Returning from standard dan play ──────────────────────────────────────
    if standard_dan.is_returning_from_play() then
        -- Event Mode: the dojo allows one play; the thank-you screen follows it
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

    -- ── Enter the menu directly ───────────────────────────────────────────────
    -- The dojo doors are the dan_doors transition (played by _title on entry). Event Mode without the
    -- Pagoda skips the boards: the dan list is the dojo.
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

    -- Deactivate the active sub-module (if any)
    if state == "standard_dan" then
        standard_dan.deactivate()
    end

    for k in pairs(CB.ctx) do CB.ctx[k] = COUNTER:EmptyCounter() end
end

function afterSongEnum()
    song_enum_done = true
    standard_dan.afterSongEnum()

    -- only the active stage moves on; an inactive one picks its state in activate
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

-- ── Update ────────────────────────────────────────────────────────────────────

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

-- what the chosen entry does once the boards have left
local function afterFlyOut()
    if out_target == "standard" then
        state = "standard_dan"
        standard_dan.enter(CB, false)
    elseif out_target == "pagoda" then
        -- Pagoda of the Unknown is its own stage: close the dojo doors over dan_select and open
        -- onto it, fading the dojo BGM out as they close.
        exiting, state = true, "leaving"
        return Exit("stage", "pagoda", "dan_doors")
    else
        exiting, state = true, "leaving"
        return Exit("title", nil, "dan_doors")   -- close the doors over dan_select, open onto the title
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

    -- Event Mode: the last allowed play is done, the thank-you screen follows
    if event_over then
        event_over = false
        return Exit("stage", "event_thanks")
    end

    -- the Event Mode countdown: while a dan is still being chosen, running out sends the player to the title
    if event_timer ~= nil and event_timer.IsActive and (state == "menu" or state == "menu_out" or state == "standard_dan") then
        if EM.signal(event_timer:Update()) == "expired" then
            if state == "standard_dan" then standard_dan.leave() end
            exiting, state = true, "leaving"
            return Exit("title", nil, "dan_doors")
        end
    end

    -- F3 auto toggle (not in Event Mode)
	local navPn = NavInput.p[1]
    if INPUT:Pressed("ToggleAutoP1") and not EM.on() then
        CONFIG:SetAutoStatus(0, not CONFIG:GetAutoStatus(0))
        SHARED:GetSharedSound("Move"):Play()
    end

    -- ── Sub-module states ──────────────────────────────────────────────────────

    if state == "standard_dan" then
        local result = standard_dan.update(dt)
        if result == "back" and EM.on() and not EM.pagodaEnabled() then
            -- no boards behind the list: leave for the title when allowed, else stay
            if EM.canLeave() then
                standard_dan.leave()
                exiting, state = true, "leaving"
                return Exit("title", nil, "dan_doors")
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

    -- ── LOADING ───────────────────────────────────────────────────────────────
    if state == "loading" then
        if navPn.cancel() and EM.canLeave() then
            exiting, state = true, "leaving"
            return Exit("title", nil, "dan_doors")   -- close the doors over dan_select, open onto the title
        end
        return
    end
    if state == "leaving" then return end

    -- ── MENU ──────────────────────────────────────────────────────────────────
    if state == "menu" then
        anim_t = anim_t + dt
        tickGlow(dt)
        local _, _, _, landed = flight()

        if navPn.cancel() then return leaveToTitle() end
        if not landed then return end   -- the boards take their choice once they have landed

        -- keys, pad and drums walk the three entries in a loop
        if navPn.upOrPadLeft() or navPn.leftKeyboard() then selectEntry(menu_sel - 1)
        elseif navPn.downOrPadRight() or navPn.rightKeyboard() then selectEntry(menu_sel + 1) end

        -- the mouse picks what it hovers once it moves, and a click on it decides
        local mdx, mdy = INPUT:GetMouseDelta()
        if (mdx ~= 0 or mdy ~= 0) and INPUT:IsMouseInside() then
            mouse_over = entryAt(INPUT:GetMouseXY())
            if mouse_over ~= nil then selectEntry(mouse_over) end
        end
        local clicked = INPUT:MousePressed("Left") and mouse_over ~= nil and entryAt(INPUT:GetMouseXY()) == menu_sel

        if navPn.decide() or clicked then return decide() end
        return
    end

    -- ── MENU OUT ──────────────────────────────────────────────────────────────
    if state == "menu_out" then
        anim_t = anim_t + dt
        tickGlow(dt)
        local _, _, _, gone = flight()
        if gone then return afterFlyOut() end
        return
    end
end

-- ── Draw ─────────────────────────────────────────────────────────────────────

local function drawTex(name, cx, cy, scale, opacity, tint)
    local t = tx_menu[name]
    if t == nil or not t.Loaded or opacity <= 0.001 then return end
    t:SetScale(scale, scale)
    t:SetOpacity(opacity)
    if tint ~= nil then t:SetColor(tint, tint, tint) end
    t:DrawAtAnchor(cx, cy, "center")
    t:SetScale(1, 1) ; t:SetOpacity(1) ; t:SetColor(1, 1, 1)
end

-- the title and the description as one block, centred in the board's text band and scaled with it
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

-- a glyph line anchored at its middle sits its ink high by half the box's bottom padding
local function textNudge(gf)
    return math.floor((gf.BoxHeight - math.ceil(gf.LineHeight)) / 2) - 1
end

local function drawMenu()
    local pulse = 0.78 + 0.22 * math.sin(menu_t * 5)
    local left, right, exitA = flight()
    for i, e in ipairs(entries) do
        local hi = menu_hi[i]
        if e.kind == "card" then
            -- the chosen board grows a little and lights up, its text with it; the other waits in the shade
            local k = (e.side < 0) and left or right
            local cx = e.cx + e.side * OFF_X * k
            local s = 1 + 0.04 * hi
            drawTex("card_hover", cx, e.cy, s, hi * pulse)
            drawTex(e.tex, cx, e.cy, s, 1, 0.78 + 0.22 * hi)
            drawBandText(e, cx, e.cy, s, 1)
        else
            local s = 1 + 0.06 * hi
            drawTex("exit_hover", e.cx, e.cy, s, hi * pulse * exitA)
            drawTex("exit", e.cx, e.cy, s, exitA, 0.84 + 0.16 * hi)
            if exitA > 0.001 then
                gfont_exit:Draw(e.title, e.cx, e.cy + textNudge(gfont_exit) * s, col_ink, col_none, exitA, s, (EXIT_W - 40) * s, "center")
            end
        end
    end
end

function draw()
    local res   = THEME:GetResolution()
    local res_w = res.X
    local res_h = res.Y

    -- Fade the dojo BGM out while the doors close on the way out. update() stops the frame
    -- Exit() is called; draw() keeps running through the transition's fade-out phase, so the
    -- ramp lives here (0.7s = dan_doors close).
    if exiting and snd_bgm ~= nil then
        dan_bgm_vol = math.max(0, dan_bgm_vol - fps.deltaTime / 0.7 * 100)
        snd_bgm:SetVolumePercent(dan_bgm_vol)
    end

    -- ── LOADING ───────────────────────────────────────────────────────────────
    if state == "loading" then
        if font_loading ~= nil then
            font_loading:GetText("Please wait for song enumeration to complete...", false, 900)
                :DrawAtAnchor(res_w / 2, res_h / 2, "center")
        end
        return
    end
    if event_over then return end   -- leaving for the thank-you screen: nothing under the fade

    -- ── BACKGROUND (all post-loading states) ──────────────────────────────────
    if tx_bg ~= nil and tx_bg.Loaded then
        tx_bg:DrawAtAnchor(res_w / 2, res_h / 2, "center")
    end

    -- ── Sub-module draw (standard_dan) ─────────────────────────────────────────
    if state == "standard_dan" then
        standard_dan.draw()
        if event_timer ~= nil and event_timer.IsActive then event_timer:Draw() end
        return
    end

    -- ── MENU (menu + menu_out) ─────────────────────────────────────────────────
    if state == "menu" or state == "menu_out" then
        drawMenu()

        NAMEPLATE:DrawPlayerNameplate(NP_X, NP_Y, 255, 0)
        CB.drawPlayerChara(NP_X + 140, NP_Y - 6,            1.0)
        CB.drawPlayerPuchi(NP_X + 220, NP_Y + CB.puchiSineY, 1.0, CB.puchiIdxFrame)
    end
    if event_timer ~= nil and event_timer.IsActive and state ~= "leaving" then event_timer:Draw() end
end
