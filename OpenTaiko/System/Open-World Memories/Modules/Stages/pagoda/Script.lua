-- Pagoda stage shell; the menu and run logic live in pagoda.lua.

local OWM       = require("OWM3d")
local pagoda    = require("pagoda")
local selector  = require("selector")
local NavInput  = require("NavInput")
local EM        = require("EventMode")
local I18N      = require("i18n")
local vn        = require("story/vn")
local floor_ent = require("floor_enter")
local floor_res = require("floor_result")
local floor_tr  = require("floor_transition")
local skybox    = require("skybox")
local tiles     = require("tower_tiles")
local title_screen = require("title_screen")
local bestiary  = require("bestiary")
local PagodaAvatar = require("PagodaAvatar")

local TXT = "Textures/tower/"
local SEL = "Textures/selector/"
local PTC = "Textures/particles/"
local SND = "Sounds/"

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

-- constants

local Puchi = Animation.stage.script.puchi
local PUCHI_FLOAT_AMP = Puchi.float_amp
local PUCHI_N_FRAMES  = Puchi.frames

local BGM_FADE_SEC = Animation.stage.script.bgm_fade_s   -- match dan_doors_back FADE_OUT_SECONDS / FADE_IN_SECONDS

-- multiplier on the sky scene time (1 = real time)
local SKY_SPEED = Animation.stage.script.sky_speed

local Fonts = Layout.stage.script.fonts
local World = Layout.stage.script.world
local Sky = Layout.stage.sky

-- state

local world        = nil   -- created on first activate
local world_failed = false -- set on a scene error: no skybox for the session
local tex         = nil    -- tower + selector textures
local font_header  = nil   -- selector headers
local font_name    = nil   -- selector level name
local gfont        = nil   -- story/VN body text
local gfont_name   = nil   -- story/VN speaker names
local gfont_small  = nil   -- floor-entrance dialog
local fonts_lang   = nil   -- language code the fonts were made for
local snd_bgm   = nil

local bgm_vol   = 0.0    -- percent, 0..100
local bgm_fade  = nil    -- "in" | "out" | nil
local exiting   = false  -- set once Exit() is called
local event_timer = nil  -- event_timer ROActivity (Event Mode)
local event_over  = false -- Event Mode: the run ended in a game over
local was_selecting = false -- Event Mode: pagoda.is_selecting() on the last update
local keep_avatar = false   -- leaving for a play or re-entering: keep the avatar

-- BGM

local function startBGM()
    if snd_bgm == nil then return end
    if not snd_bgm.IsPlaying then
        bgm_vol = 0.0
        snd_bgm:SetVolumePercent(0)
        snd_bgm:SetLoop(true)
        snd_bgm:Play()
    end
    bgm_fade = "in"
end

local function stopBGM()
    if snd_bgm ~= nil and snd_bgm.IsPlaying then snd_bgm:Stop() end
    bgm_fade = nil
end

-- callbacks for pagoda.lua

local CB = {
    startBGM = startBGM,
    stopBGM  = stopBGM,
    ctx = {},
    puchiSineY    = 0,
    puchiIdxFrame = 0,
}

function CB.startCounter(key, startVal, endVal, interval, mode, updateCallback, onFinish)
    local c = COUNTER:CreateCounter(startVal, endVal, interval, onFinish)
    if mode == "loop"   then c:SetLoop(true)
    elseif mode == "bounce" then c:SetBounce(true) end
    if updateCallback then c:Listen(updateCallback) end
    c:Start()
    CB.ctx[key] = c
    return c
end

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

local function load_menu_chara()
    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL) end
end

-- lazy resources

local function dispose_fonts()
    if font_header  ~= nil then font_header:Dispose()  ; font_header  = nil end
    if font_name    ~= nil then font_name:Dispose()    ; font_name    = nil end
    if gfont        ~= nil then gfont:Dispose()        ; gfont        = nil end
    if gfont_name   ~= nil then gfont_name:Dispose()   ; gfont_name   = nil end
    if gfont_small  ~= nil then gfont_small:Dispose()  ; gfont_small  = nil end
end

-- built on first entry (onStart runs at skin load for every stage), kept until teardown
local function ensure_world()
    if world ~= nil then return end
    local res = THEME:GetResolution()
    world = OWM.World.new{
        rw = World.render_w, rh = World.render_h,
        screenW = res.X, screenH = res.Y,
        fov = World.fov_deg, yaw = World.yaw_deg, pitch = Sky.pitch_deg,
        lit = true, hour = Sky.hour,
    }
    skybox.setup(world)
    world:setFog(false)

    -- render once while the doors are shut; a shader compile on the first draw would skip the FadeIn animation
    pcall(function() world:update(0, 0, 0, 0) ; world:render() end)
end

local function ensure_resources()
    if tex == nil then
        tex = {
            -- levels 1-10 use tower_tiles.lua's tiles
            normal = TEXTURE:CreateTexture(TXT .. "normal.png"),
            roof   = TEXTURE:CreateTexture(TXT .. "roof.png"),
            roof_beanstalk = TEXTURE:CreateTexture(TXT .. "roof_beanstalk.png"), -- roof 1 once door 10 is beaten
            beanstalk = TEXTURE:CreateTexture(TXT .. "beanstalk.png"),    -- roof 2+, repeated
            checkpoint_cleared = TEXTURE:CreateTexture(TXT .. "checkpoint_cleared.png"),
            dojo    = TEXTURE:CreateTexture(TXT .. "dojo.png"),   -- slot 0
            bg_far1     = TEXTURE:CreateTexture(TXT .. "bg/far1.png"),
            bg_far2     = TEXTURE:CreateTexture(TXT .. "bg/far2.png"),
            bg_far3     = TEXTURE:CreateTexture(TXT .. "bg/far3.png"),
            bg_near     = TEXTURE:CreateTexture(TXT .. "bg/near.png"),
            bg_near_top = TEXTURE:CreateTexture(TXT .. "bg/near_top.png"),
            shadow   = TEXTURE:CreateTexture(PTC .. "shadow.png"),
            spiral   = TEXTURE:CreateTexture(PTC .. "spiral.png"),
            cloud    = TEXTURE:CreateTexture(PTC .. "cloud.png"),
            black1   = TEXTURE:CreateTexture(SEL .. "black1.png"),
            flash    = TEXTURE:CreateTexture("Textures/story/flash.png"),
            exclaim  = TEXTURE:CreateTexture("Textures/rewards/exclaim.png"),
        }
    end
    tiles.setup()
    title_screen.setup()
    -- fonts follow the language (SkinConfig FontName<code>) and are rebuilt when it changes
    local lang = I18N.currentCode()
    if fonts_lang ~= nil and fonts_lang ~= lang then
        dispose_fonts()
        floor_ent.dispose()
        pagoda.destroy()
    end
    fonts_lang = lang
    if font_header  == nil then font_header  = TEXT:Create(Fonts.header, "regular") end
    if font_name    == nil then font_name    = TEXT:Create(Fonts.name, "regular") end
    -- size only: NLua breaks if styled and unstyled CreateGlyphCached calls are mixed
    if gfont       == nil then gfont       = TEXT:CreateGlyphCached(Fonts.story) end
    if gfont_name  == nil then gfont_name  = TEXT:CreateGlyphCached(Fonts.story_name) end
    if gfont_small == nil then gfont_small = TEXT:CreateGlyphCached(Fonts.story_small) end
    selector.setup(world, tex, { header = font_header, name = font_name })

    local story_fonts = { gfont = gfont, gfontName = gfont_name, gfontSmall = gfont_small }
    vn.setup(tex, story_fonts)
    floor_ent.setup(tex, story_fonts, world)
    floor_res.setup(tex, story_fonts)
    floor_tr.setup(tex)
    bestiary.setup(tex, story_fonts)
end

-- lifecycle

function onStart()
    -- nothing heavy here: the scene and textures are made in activate()
    I18N.detect()
end

-- engine-called when the language changes
function reloadLanguage()
    I18N.detect()
end

function onDestroy()
    PagodaAvatar.leave()   -- game closing or skin change
    if world ~= nil then world:dispose() ; world = nil end
    if tex ~= nil then
        for _, t in pairs(tex) do
            if type(t) == "table" then for _, g in pairs(t) do if g ~= nil then g:Dispose() end end
            elseif t ~= nil then t:Dispose() end
        end
        tex = nil
    end
    vn.dispose()
    floor_res.dispose()
    floor_ent.dispose()
    selector.dispose()
    tiles.dispose()
    title_screen.dispose()
    bestiary.dispose()
    skybox.dispose()
    dispose_fonts()
    fonts_lang = nil
    pagoda.destroy()
end

function activate()
    CONFIG.PlayerCount = 1

    exiting      = false
    event_timer, event_over = nil, false
    world_failed = false
    keep_avatar  = false
    ensure_world()
    ensure_resources()
    load_menu_chara()

    snd_bgm = SOUND:CreateBGM(SND .. "Title.ogg")

    CB.startCounter("puchi_sine", 0, 360, 1 / Puchi.sine_steps_per_s, "loop", function(val)
        CB.puchiSineY = math.sin(val * math.pi / 180) * PUCHI_FLOAT_AMP
    end)
    CB.startCounter("puchi_frame", 0, 1, Puchi.frame_step_s, "loop", function(val)
        CB.puchiIdxFrame = math.floor(val * PUCHI_N_FRAMES)
    end)

    if pagoda.is_returning_from_play() then
        pagoda.activate(CB)
        -- on_return starts the BGM when needed; otherwise the floor transition owns the audio
        local outcome = pagoda.on_return(CB)
        if EM.on() then
            EM.resetMods()
            if outcome == "game_over" and EM.registerPlay("dan") then event_over = true end
        end
    else
        pagoda.enter(CB)
        startBGM()
    end
    if EM.on() and not event_over then
        event_timer = ROACTIVITY:GetROActivity("event_timer")
        if event_timer ~= nil then event_timer:Activate(pagoda.is_selecting() and EM.selectSeconds() or 0, "dan") end
        was_selecting = pagoda.is_selecting()
    end
end

function deactivate()
    stopBGM()
    if event_timer ~= nil and event_timer.IsActive then event_timer:Deactivate() end
    selector.close()   -- a practice play leaves it open; also restores the sky
    floor_res.release()   -- frees a lost run's held landing or an unfinished result screen
    pagoda.deactivate()
    -- leaving the pagoda: player 1's own characters back
    if not keep_avatar then PagodaAvatar.leave() end

    for k in pairs(CB.ctx) do CB.ctx[k] = COUNTER:EmptyCounter() end

    if snd_bgm ~= nil then snd_bgm:Dispose() ; snd_bgm = nil end
end

function afterSongEnum()
    PagodaAvatar.recover()   -- a run the game crashed in; the characters are loaded at this point
    pagoda.afterSongEnum()
end

-- update

function update()
    local dt = fps.deltaTime

    -- guarded: on an error the skybox is dropped for the session
    if world ~= nil and not world_failed then
        if not pcall(world.update, world, dt * SKY_SPEED * skybox.speed(), 0, 0, 0) then world_failed = true end
    end

    if bgm_fade == "in" and snd_bgm ~= nil then
        bgm_vol = math.min(100, bgm_vol + dt / BGM_FADE_SEC * 100)
        snd_bgm:SetVolumePercent(bgm_vol)
        if bgm_vol >= 100 then bgm_fade = nil end
    end

    if INPUT:Pressed("ToggleAutoP1") and not EM.on() then
        CONFIG:SetAutoStatus(0, not CONFIG:GetAutoStatus(0))
        SHARED:GetSharedSound("Move"):Play()
    end

    if event_over then
        event_over = false
        exiting = true
        return Exit("stage", "event_thanks")
    end

    -- Event Mode countdown: runs only while choosing, restarts when choosing resumes
    if event_timer ~= nil and event_timer.IsActive then
        local selecting = pagoda.is_selecting()
        if not selecting then
            event_timer:Call("stop")
        elseif not was_selecting then
            event_timer:Call("restart", EM.selectSeconds())
        elseif EM.signal(event_timer:Update()) == "expired" then
            exiting  = true
            bgm_fade = "out"
            return Exit("title", nil, "dan_doors_back")
        end
        was_selecting = selecting
    end

    -- ticks CB.ctx itself and returns a verb
    local result = pagoda.update(dt)
    keep_avatar = (result == "play" or result == "reopen")
    if result == "back" then
        -- fade out in draw(): update() stops during FadeOut
        exiting  = true
        bgm_fade = "out"
        return Exit("stage", "dan_select", "dan_doors_back")
    elseif result == "play" then
        -- pagoda stopped the BGM before returning "play"
        return Exit("play", nil)
    elseif result == "reopen" then
        -- lost run: the stage re-enters itself onto a fresh title
        exiting = true
        return Exit("stage", "pagoda", "dan_doors_back")
    end
end

-- draw

function draw()
    -- skybox.draw is pcall-guarded
    if world ~= nil and not world_failed then
        local g, v = pagoda.sky()
        if not skybox.draw(g, v) then world_failed = true end
    end

    -- selector.draw() renders this scene itself while the selector is up
    if tex ~= nil and not selector.active and pagoda.shows_title_scene() then
        selector.drawTitle(pagoda.highest_level())
    end

    -- here, not in update(): update() stops once Exit() is called
    if exiting and bgm_fade == "out" and snd_bgm ~= nil then
        bgm_vol = math.max(0, bgm_vol - fps.deltaTime / BGM_FADE_SEC * 100)
        snd_bgm:SetVolumePercent(bgm_vol)
    end

    if event_over then return end   -- leaving: nothing under the fade
    pagoda.draw()
    if event_timer ~= nil and event_timer.IsActive and not exiting and pagoda.is_selecting() then event_timer:Draw() end
end
