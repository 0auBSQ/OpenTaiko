-- Pagoda menu, challenge and practice logic.

local NavInput   = require("NavInput")
local EM         = require("EventMode")
local I18N       = require("i18n")
local selector   = require("selector")
local stories    = require("story/stories")
local small_talk = require("story/small_talk")
local vn         = require("story/vn")
local floor_ent  = require("floor_enter")
local floor_res  = require("floor_result")
local floor_tr   = require("floor_transition")
local levels     = require("levels")
local save       = require("save")
local rewards    = require("rewards")
local songs      = require("songs")
local skybox     = require("skybox")
local Floor      = require("Pagoda.Floor")
local Run        = require("Pagoda.Run")
local monsters   = require("encounter/monsters")
local title_screen = require("title_screen")
local bestiary   = require("bestiary")
local Config     = require("Core.Config")
local Layout     = Config.load("Config/layout.json")
local Animation  = Config.load("Config/animation.json")

local M = {}

local Look    = Layout.stage.pagoda
local Missing = Look.missing_songs
local Status  = Look.status
local StatusTime = Animation.stage.pagoda.status


-- state kept across activate / deactivate

local _pagoda_state    = "main_menu"
local _challenge_level = 1
local _practice_level  = 1
local _practice_hover  = nil     -- last practice floor hovered
local _in_challenge    = false   -- exited to a challenge play
local _in_practice     = false   -- exited to a practice play
local _missing_song_count = nil   -- nil until the first check
local _run_monsters    = {}      -- level -> monster for the current run (floors 1-4)
local _run_start       = 1       -- start floor of the current run
local _run_tower       = 0       -- highest floor the run's tower shows cleared

-- challenge preview
local _preview_songs   = nil    -- { {color, node, entry}, ... } for blue/green/red
local _preview_purple  = nil    -- { node, entry } or nil
local _preview_level   = -1     -- level the preview was rolled for
local _preview_speed   = 20     -- a CONFIG.SONGSPEED value

-- per-activation state

local _CB                 = nil
local _font_hero          = nil   -- title-screen heading
local _font_body          = nil
local _font_hint          = nil
local _status_msg         = ""
local _status_timer       = 0.0
local _menu_sel           = 1
local _btn_timer          = 0.0
local _cleared_level      = 1       -- floor the result screen shows cleared
local _cleared_shrouded   = false   -- the cleared floor was above the old highest

local NP_X            = Look.nameplate.x
local NP_Y            = Look.nameplate.y

-- valid challenge start levels for the current save (Pagoda.Run)
local function _start_options()
    return Run.startOptions()
end

-- for Script.lua's title scene, which darkens the floors above it
function M.highest_level() return save.highest_level() end

-- true on the screens drawn over the title tower; drawing it under the others would also move the selector's camera
function M.shows_title_scene()
    return not (_pagoda_state == "story" or _pagoda_state == "floor_transition"
                or _pagoda_state == "floor_enter" or _pagoda_state == "floor_result")
end

-- returns grey (0..1) and whether the sky shows this frame
function M.sky()
    local g, v = nil, nil
    if _pagoda_state == "floor_enter" then g, v = floor_ent.sky()
    elseif _pagoda_state == "floor_result" then g, v = floor_res.sky()
    elseif _pagoda_state == "floor_transition" then g, v = floor_tr.sky()
    elseif _pagoda_state == "story" then g, v = vn.sky() end
    if g == nil then return skybox.rule(), true end
    return g, v
end

-- songs and speed rolled for a floor; nil while the preview holds another floor
local function _floor_roll(level)
    if _preview_songs == nil or _preview_level ~= level then return nil end
    return { songs = _preview_songs, purple = _preview_purple, speed = _preview_speed }
end

-- highest = the save's progress, tower = the run's tower
local function _story_ctx(level)
    return {
        startLevel = level,
        highest    = save.highest_level(),
        tower      = _run_tower,
        firstTime  = level > save.highest_level(),
        playerName = save.player_name(),
        monster    = _run_monsters[level],
        lostBefore = save.lost_before(level),
        roll       = _floor_roll(level),
        runStart   = _run_start,
    }
end

local function _new_run()
    _run_monsters = monsters.rollRun()
    _run_start = _challenge_level
    _run_tower = _challenge_level - 1
end

local function _selector_info()
    return { name = levels.level_name, color = levels.level_tick_color,
             highest = save.highest_level() }
end

local function _open_checkpoint_select()
    selector.open("checkpoint", _start_options(), _challenge_level, _selector_info())
end

-- "abort" returns to the checkpoint selector, "proceed" goes on to the floor transition
local function _start_story(level)
    local scene = stories.pick(level, save.highest_level(), save.story_first_seen())
    save.mark_story_first_seen()
    _pagoda_state = "story"
    vn.play(scene, _story_ctx(level), function(outcome)
        if outcome == "abort" then
            _pagoda_state = "start_choice"
            _open_checkpoint_select()
            if _CB ~= nil then _CB.startBGM() end
        else
            floor_tr.open(level, level, _run_tower, false)
            floor_ent.preload(level, _story_ctx(level))
            _pagoda_state = "floor_transition"
        end
    end)
    if _CB ~= nil then _CB.stopBGM() end       -- the story owns the audio
end

-- the transition plays its own audio; the entrance screen starts the floor's loop
local function _open_transition()
    floor_tr.open(_cleared_level, _challenge_level, _run_tower, _cleared_shrouded)
    floor_ent.preload(_challenge_level, _story_ctx(_challenge_level))
    _pagoda_state = "floor_transition"
end

local function _open_practice_select()
    -- selector.open() falls back to the bottom floor if the level isn't listed
    selector.open("practice", Run.practiceLevels(), _practice_hover or _practice_level, _selector_info())
end

-- challenge preview

-- returns a CONFIG.SONGSPEED value (Pagoda.Floor)
local function _floor_speed(level)
    return Floor.speed(level)
end

-- rolls and caches the level's songs and sets _preview_speed; avoid: optional song ids to skip while the pools have others
local function _build_preview(level, avoid)
    local roll = Floor.rollFloor(level, avoid, songs.get_song_list())
    _preview_songs  = roll.songs
    _preview_purple = roll.purple
    _preview_level  = level

    _preview_speed = _floor_speed(level)
end

local function _clear_preview()
    _preview_songs  = nil
    _preview_purple = nil
    _preview_level  = -1
end

-- Tambourine reroll: avoids the current songs where the pools allow; keepMystery keeps the mystery song; returns the new roll
local function _reroll_floor(keepMystery)
    local level = _challenge_level
    local avoid, purple = {}, _preview_purple
    for _, item in ipairs(_preview_songs or {}) do avoid[item.entry.id] = true end
    if purple ~= nil then avoid[purple.entry.id] = true end
    _build_preview(level, avoid)
    if keepMystery then _preview_purple = purple end
    return _floor_roll(level)
end

-- skin locale string (Locales/*.json), fallback when missing
local function _sk(key, fallback)
    local s = THEME:GetSkinString(key)
    if s == nil or s == "" then return fallback end
    return s
end

-- dan building

local play_setup = require("play_setup")

-- practice: no monster in the background, and the save's tower
local function _build_dan(level, practice)
    local roll = _floor_roll(level)
    if roll ~= nil then
        _clear_preview()
    else
        -- practice, or no cached preview
        roll = Floor.rollFloor(level, nil, songs.get_song_list())
        _preview_speed = _floor_speed(level)
    end

    if not Floor.buildDan(level, roll, { genre = _sk("PAGODA_TITLE", "Pagoda of the Unknown") }) then return false end

    if practice then
        play_setup.begin(level, _preview_speed, nil, save.highest_level(), true)
    else
        play_setup.begin(level, _preview_speed, _run_monsters[level], _run_tower, false)
    end
    if DANBUILDER:Mount() then
        play_setup.mounted()
        return true
    end
    play_setup.finish()
    return false
end

-- draw helpers

local function _ensure_fonts()
    if _font_body ~= nil then return end
    _font_hero  = TEXT:Create(Look.fonts.hero, "regular")
    _font_body  = TEXT:Create(Look.fonts.body, "regular")
    _font_hint  = TEXT:Create(Look.fonts.hint, "regular")
end

local function _set_status(msg, t)
    _status_msg   = msg
    _status_timer = t or StatusTime.default_s
end

-- public API

function M.is_returning_from_play()
    return _in_challenge or _in_practice
end

-- true while a run is being chosen (the Event Mode countdown runs then)
function M.is_selecting()
    return _pagoda_state == "main_menu" or _pagoda_state == "start_choice" or _pagoda_state == "practice_select"
        or _pagoda_state == "missing_songs"
end

-- items: 1 Challenge, 2 Practice, 3 Exit, 4 Bestiary (listed above Exit)
local function menu_items()
    if EM.on() then
        if EM.canLeave() then return { 1, 3 } end
        return { 1 }
    end
    if bestiary.available() then return { 1, 2, 4, 3 } end
    return { 1, 2, 3 }
end

local function menu_step(dir)
    local items = menu_items()
    local at = 1
    for i, v in ipairs(items) do if v == _menu_sel then at = i end end
    at = math.max(1, math.min(#items, at + dir))
    return items[at]
end

-- fresh entry (not a return from play)
function M.enter(CB)
    _CB    = CB
    save.migrate_save()
    play_setup.finish()          -- restores the player's mods after a play the game never returned from
    _btn_timer    = 0.15
    _status_msg   = ""
    _status_timer = 0.0
    _ensure_fonts()

    if _missing_song_count == nil then
        _missing_song_count = songs.count_missing_songs()
    end

    if _missing_song_count > 0 then
        _pagoda_state = "missing_songs"
    else
        _pagoda_state = "main_menu"
        _menu_sel     = 1
    end

    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL) end
end

function M.leave()
    _in_challenge = false
    _in_practice  = false
    _pagoda_state = "main_menu"
    M.deactivate()
end

-- called from Script.lua's activate()
function M.activate(CB)
    _CB = CB
    save.migrate_save()
    _btn_timer = 0.15
    _ensure_fonts()

    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL) end
end

-- called from Script.lua's deactivate()
function M.deactivate()
    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then chara:DisposeAnimation(CHARACTER.ANIM_MENU_NORMAL) end
end

-- called after activate() when is_returning_from_play(); PLAYSTATE still holds the finished dan
function M.on_return(CB)
    _CB = CB
    play_setup.finish()          -- restores the player's mods, drops the roof plate style
    floor_ent.release()          -- drops the frame held through the exit
    -- a quit play never reaches the results, so IsPass would judge the last finished play's exams
    local passed = not PLAYSTATE:WasPlayAborted() and PLAYSTATE:IsPass()

    local outcome = passed and "clear" or "game_over"
    if _in_challenge then
        _in_challenge = false
        local played = _challenge_level
        -- ctx taken before saving: the result screen shows the floor as visited
        local ctx = _story_ctx(played)
        -- bestiary counts
        for _, k in ipairs(monsters.keysForLevel(played, _run_monsters[played])) do
            save.count_monster_result(k, passed)
        end
        local got = nil
        if passed then
            got = rewards.grant(played)            -- saved here, shown by the result screen
            local prev   = _run_tower
            save.set_highest_level(played)
            _run_tower = math.max(_run_tower, played)
            _challenge_level = played + 1
            _build_preview(_challenge_level)
            _cleared_level, _cleared_shrouded = played, played > prev
        else
            save.mark_lost(played)
        end
        floor_res.open(played, ctx, passed and "clear" or "fail", got)
        _pagoda_state = "floor_result"
        -- no title BGM: the result screen, then the transition or the title, own the audio
    elseif _in_practice then
        -- practice has no result screen: back to the selector on the played floor
        _in_practice  = false
        _pagoda_state = "practice_select"
        _open_practice_select()
        if _CB ~= nil then _CB.startBGM() end
    end

    _menu_sel  = 1
    _btn_timer = 0.15
    return outcome
end

function M.afterSongEnum()
    songs.clear_cache()
    levels.clear_pools()
    _missing_song_count = nil
    _clear_preview()
end

function M.destroy()
    if _font_hero  ~= nil then _font_hero:Dispose()  ; _font_hero  = nil end
    if _font_body  ~= nil then _font_body:Dispose()  ; _font_body  = nil end
    if _font_hint  ~= nil then _font_hint:Dispose()  ; _font_hint  = nil end
end

-- returns nil | "back" | "play" | "reopen" (a lost run: re-enter onto the title)
function M.update(dt)
    if _CB then
        for _, c in pairs(_CB.ctx) do c:Tick() end
    end

    _btn_timer    = math.max(0, _btn_timer - dt)
    _status_timer = math.max(0, _status_timer - dt)

    local navPn = NavInput.p[1]
    if INPUT:Pressed("ToggleAutoP1") then
        CONFIG:SetAutoStatus(0, not CONFIG:GetAutoStatus(0))
        SHARED:GetSharedSound("Move"):Play()
    end

    if _btn_timer > 0 then return nil end

    local up_p   = navPn.upOrPadLeft()
    local down_p = navPn.downOrPadRight()
    local ok_p   = navPn.decide()
    local back_p = navPn.cancel()

    if _pagoda_state == "missing_songs" then
        if ok_p or back_p then
            SHARED:GetSharedSound("Cancel"):Play()
            return "back"
        end
        return nil
    end

    if _pagoda_state == "main_menu" then
        if up_p   then
            _menu_sel = menu_step(-1)
            SHARED:GetSharedSound("Move"):Play()
        end
        if down_p then
            _menu_sel = menu_step(1)
            SHARED:GetSharedSound("Move"):Play()
        end
        if back_p then
            if not EM.canLeave() then
                SHARED:GetSharedSound("Error"):Play()
                return nil
            end
            SHARED:GetSharedSound("Cancel"):Play()
            return "back"
        end
        if ok_p then
            if _menu_sel == 1 then
                _pagoda_state = "start_choice"
                _open_checkpoint_select()
                SHARED:GetSharedSound("Decide"):Play()
            elseif _menu_sel == 2 then
                _pagoda_state = "practice_select"
                _open_practice_select()
                SHARED:GetSharedSound("Decide"):Play()
            elseif _menu_sel == 3 then
                SHARED:GetSharedSound("Cancel"):Play()
                return "back"
            elseif _menu_sel == 4 then
                _pagoda_state = "bestiary"
                bestiary.open()
                SHARED:GetSharedSound("Decide"):Play()
            end
        end
        return nil
    end

    if _pagoda_state == "bestiary" then
        if bestiary.update(dt, navPn) == "back" then
            bestiary.close()
            _pagoda_state = "main_menu" ; _menu_sel = 4
        end
        return nil
    end

    if _pagoda_state == "start_choice" then
        local r = selector.update(dt, navPn)
        if r == "exit" then
            selector.close(true)
            _pagoda_state = "main_menu" ; _menu_sel = 1   -- cursor on "Challenge"
        elseif type(r) == "table" and r.sel ~= nil then
            selector.close()
            _challenge_level = r.sel
            _build_preview(_challenge_level)
            _new_run()
            _start_story(_challenge_level)
        end
        return nil
    end

    if _pagoda_state == "story" then
        vn.update(dt)
        return nil
    end

    if _pagoda_state == "floor_result" then
        local r = floor_res.update(dt, navPn)
        if r == "clear" then
            -- small_talk.roll may give a talk to play before the transition
            local talk = small_talk.roll(_cleared_level)
            if talk ~= nil then
                _pagoda_state = "story"
                vn.play(talk, _story_ctx(_cleared_level), function() _open_transition() end)
            else
                _open_transition()
            end
        elseif r == "fail" then
            return "reopen"
        end
        return nil
    end

    if _pagoda_state == "floor_transition" then
        if floor_tr.update(dt) == "done" then
            local ctx = _story_ctx(_challenge_level)
            ctx.reroll = _reroll_floor               -- Tambourine reroll
            save.set_highest_reached(_challenge_level)   -- opens this floor in practice
            floor_ent.open(_challenge_level, ctx)
            _pagoda_state = "floor_enter"
        end
        return nil
    end

    if _pagoda_state == "floor_enter" then
        local r = floor_ent.update(dt, navPn)
        if r == "start" then
            if _build_dan(_challenge_level) then
                _in_challenge = true
                if _CB ~= nil then _CB.stopBGM() end
                return "play"
            end
            SHARED:GetSharedSound("Error"):Play()
            _set_status("Failed to load songs!", StatusTime.load_fail_s)
            floor_ent.release()
            _pagoda_state = "start_choice" ; _open_checkpoint_select()
            if _CB ~= nil then _CB.startBGM() end   -- the floor loop is already stopped
        elseif r == "leave" then
            floor_ent.release()
            _pagoda_state = "start_choice" ; _open_checkpoint_select()
            if _CB ~= nil then _CB.startBGM() end
        end
        return nil
    end

    -- the selector runs the confirm dialog itself: a returned selection is confirmed
    if _pagoda_state == "practice_select" then
        local r = selector.update(dt, navPn)
        if r == "exit" then
            selector.close(true)
            _pagoda_state = "main_menu" ; _menu_sel = 2   -- cursor on "Practice"
        elseif type(r) == "table" and r.sel ~= nil then
            _practice_level = r.sel
            if _build_dan(_practice_level, true) then
                -- the selector stays up until deactivate: song_loading fades in over it
                _in_practice = true
                if _CB ~= nil then _CB.stopBGM() end
                return "play"
            else
                SHARED:GetSharedSound("Error"):Play()
                selector.close()
                _open_practice_select()
            end
        else
            _practice_hover = selector.hovered_level() or _practice_hover
        end
        return nil
    end

    return nil
end

-- draw

function M.draw()
    if _font_body == nil then return end

    local res   = THEME:GetResolution()
    local res_w = res.X
    local res_h = res.Y
    local cx    = res_w / 2

    local C_WHITE  = Config.color(Missing.text_color)
    local C_DIM    = Config.color(Missing.dim_color)
    local C_FAIL   = Config.color(Missing.fail_color)

    local base_y = res_h / 2

    if _pagoda_state == "missing_songs" then
        _font_body:GetText(I18N.tr("Some songs are missing to play this game mode"),    false, Missing.body_max_w, C_FAIL):DrawAtAnchor(cx, base_y + Missing.message_dy,  "center")
        _font_body:GetText(I18N.tr("Please update the OpenTaiko soundtrack and try again"), false, Missing.body_max_w, C_WHITE):DrawAtAnchor(cx, base_y + Missing.update_dy, "center")
        _font_hint:GetText(I18N.trf("(Missing song count: %d)", _missing_song_count), false, Missing.hint_max_w, C_DIM):DrawAtAnchor(cx, base_y + Missing.count_dy, "center")
        _font_hint:GetText(I18N.tr("Press any button to go back"), false, Missing.hint_max_w, C_DIM):DrawAtAnchor(cx, base_y + Missing.back_dy, "center")

    elseif _pagoda_state == "main_menu" then
        title_screen.draw(_menu_sel, menu_items(), save.highest_level(), _font_hero, _font_body)

    elseif _pagoda_state == "bestiary" then
        bestiary.draw()

    elseif _pagoda_state == "start_choice" then
        selector.draw()

    elseif _pagoda_state == "story" then
        vn.draw()

    elseif _pagoda_state == "floor_transition" then
        floor_tr.draw()

    elseif _pagoda_state == "floor_enter" then
        floor_ent.draw()

    elseif _pagoda_state == "floor_result" then
        floor_res.draw()

    elseif _pagoda_state == "practice_select" then
        selector.draw()
    end

    if _status_timer > 0 then
        local alpha = math.min(255, math.floor(_status_timer * 255))
        local sc = Status.color
        _font_hint:GetText(I18N.tr(_status_msg), false, Status.max_w,
            COLOR:CreateColorFromRGBA(sc[1], sc[2], sc[3], alpha)):DrawAtAnchor(cx, res_h - Status.bottom_offset, "center")
    end

    -- nameplate only (no character or puchichara), hidden on the full-screen scenes
    local scene_state = (_pagoda_state == "story" or _pagoda_state == "floor_transition"
                         or _pagoda_state == "floor_enter" or _pagoda_state == "floor_result")
    if not selector.active and not scene_state then
        NAMEPLATE:DrawPlayerNameplate(NP_X, NP_Y, 255, 0)
    end
end

return M
