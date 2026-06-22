-- pagoda.lua  —  Pagoda of the Unknown sub-module for dan_select

local M = {}

-- ── Song pools (loaded from pagoda_pools.json) ────────────────────────────────
-- Keys are integer level numbers; _get_pool() falls back downward if a level
-- has no dedicated pool entry.

local _pools_cache = nil

-- NLua cannot use pairs() on C# Dictionary objects; use GetEnumerator() instead.
local function _dict_iter(d)
    local e = d:GetEnumerator()
    return function()
        if e:MoveNext() then return e.Current.Key, e.Current.Value end
    end
end

local function _load_pools()
    if _pools_cache ~= nil then return _pools_cache end
    local raw = JSONLOADER:JsonParseFile("pagoda_pools.json")

    -- Parse a single entry object { "id": "...", "diff": N }
    local function parse_entry(e_obj)
        local id, diff = nil, 0
        for k, v in _dict_iter(e_obj) do
            if k == "id"   then id   = v end
            if k == "diff" then diff = tonumber(v) or 0 end
        end
        return id ~= nil and { id = id, diff = diff } or nil
    end

    -- Parse a pool object containing "blue", "green", "red", "purple" arrays
    local function parse_pool(raw_pool)
        local pool = {}
        for _, color in ipairs({ "blue", "green", "red", "purple" }) do
            pool[color] = {}
            -- find the color key among the pool's dictionary entries
            local raw_entries = nil
            for k, v in _dict_iter(raw_pool) do
                if k == color then raw_entries = v ; break end
            end
            if raw_entries ~= nil then
                for _, entry_obj in _dict_iter(raw_entries) do
                    local entry = parse_entry(entry_obj)
                    if entry ~= nil then table.insert(pool[color], entry) end
                end
            end
        end
        return pool
    end

    _pools_cache = {}
    for k, v in _dict_iter(raw) do
        local lv = tonumber(k)
        if lv ~= nil then
            _pools_cache[lv] = parse_pool(v)
        end
    end
    return _pools_cache
end

local function _get_pool(level)
    local pools = _load_pools()
    local lv = level
    while lv >= 1 do
        if pools[lv] ~= nil then return pools[lv] end
        lv = lv - 1
    end
    return pools[1]  -- ultimate fallback
end

-- ── Level helpers ─────────────────────────────────────────────────────────────

local KANJI_NUMS = { "一","二","三","四","五","六","七","八","九","十" }

local function _level_name(level)
    if     level == 1 then return "初級"
    elseif level == 2 then return "四級"
    elseif level == 3 then return "三級"
    elseif level == 4 then return "二級"
    elseif level == 5 then return "一級"
    elseif level >= 6 and level <= 15 then
        return "扉" .. (KANJI_NUMS[level - 5] or tostring(level - 5))
    else
        -- level 16 → 屋根1, level 17 → 屋根2, ...
        return "屋根" .. tostring(level - 15)
    end
end

local function _level_tick(level)
    if     level <= 5  then return 0
    elseif level <= 15 then return 2
    end
    return 4
end

local function _hsv_to_rgb(h, s, v)
    -- h in [0, 360], s and v in [0, 1]; returns r, g, b in [0, 255]
    local hi = math.floor(h / 60) % 6
    local f  = h / 60 - math.floor(h / 60)
    local p  = v * (1 - s)
    local q  = v * (1 - f * s)
    local t  = v * (1 - (1 - f) * s)
    local r, g, b
    if     hi == 0 then r, g, b = v, t, p
    elseif hi == 1 then r, g, b = q, v, p
    elseif hi == 2 then r, g, b = p, v, t
    elseif hi == 3 then r, g, b = p, q, v
    elseif hi == 4 then r, g, b = t, p, v
    else                r, g, b = v, p, q end
    return math.floor(r * 255), math.floor(g * 255), math.floor(b * 255)
end

local function _level_tick_color(level)
    if level <= 5 then
        return { 139, 69, 19 }       -- brown
    elseif level <= 15 then
        -- rainbow gradient: blue (level 6) → red (level 15)
        local t = (level - 6) / 9        -- 0.0 at level 6, 1.0 at level 15
        local h = 240 * (1 - t)^1.4     -- 240° (blue) → 0° (red)
        local r, g, b = _hsv_to_rgb(h, 0.88, 1.0)
        return { r, g, b }
    else
        return { 0, 0, 0 }           -- black
    end
end

-- EXAM 1: Gauge thresholds (red / gold), range = More (player must reach)
local function _exam1_gauge(level)
    if level <= 3 then
        return 70, 80
    elseif level <= 5 then
        return 80, 90
    elseif level <= 15 then
        local n = level - 6
        return math.min(100, 90 + n), math.min(100, 95 + n)
    else
        return 100, 100
    end
end

-- EXAM 2: Accuracy % (levels 1-15, More) or Ok count (levels 16+, Less)
-- Returns: type, red, gold, lessThan
local function _exam2(level)
    if level <= 15 then
        local t = {
            [1]={80,90},  [2]={84,92},  [3]={88,94},
            [4]={70,85},  [5]={75,87},
            [6]={80,90},  [7]={84,92},  [8]={88,94},  [9]={90,95},
            [10]={92,96}, [11]={94,97}, [12]={95,97}, [13]={96,98},
            [14]={97,98}, [15]={98,99},
        }
        local e = t[level] or {80, 90}
        return "a", e[1], e[2], false
    elseif level == 16 then return "jg", 50, 25, true
    elseif level == 17 then return "jg", 40, 20, true
    elseif level == 18 then return "jg", 30, 15, true
    elseif level == 19 then return "jg", 20, 10, true
    elseif level == 20 then return "jg", 15,  8, true
    elseif level == 21 then return "jg", 10,  5, true
    else
        -- level 22+: red -1/level, gold -1/2 levels, both floor at 1
        local n    = level - 22
        local red  = math.max(1, 10 - n)
        local gold = math.max(1, 5 - math.floor(n / 2))
        return "jg", red, gold, true
    end
end

-- EXAM 3: Bad count base values (red / gold); multiply by 1.25 (floor) if purple picked
local function _exam3_base(level)
    if level <= 15 then
        local t = {
            [1]={30,20}, [2]={28,19}, [3]={26,18},
            [4]={24,17}, [5]={22,16},
            [6]={20,15}, [7]={16,12}, [8]={12,8},  [9]={10,7},
            [10]={9,6},  [11]={8,6},  [12]={7,5},  [13]={6,4},
            [14]={5,3},  [15]={4,2},
        }
        local e = t[level] or {4, 2}
        return e[1], e[2]
    elseif level <= 18 then return 3, 1
    elseif level <= 21 then return 2, 1
    else                    return 1, 1
    end
end


local function _purple_prob(level)
    if     level <= 14 then return 0.10
    elseif level == 15 then return 1.00
    elseif level <= 21 then return 0.10
    else
        -- level 22 → 0 %, level 23 → 10 %, level 24 → 20 %, …
        return math.max(0.0, math.min(1.0, (level - 22) * 0.10))
    end
end

local function _pick_entry(entries, used_ids)
    for _ = 1, 20 do
        local e = entries[math.random(#entries)]
        if not used_ids[e.id] then return e end
    end
    return entries[math.random(#entries)]   -- fallback: allow repeat
end

local function _shuffle(t)
    for i = #t, 2, -1 do
        local j = math.random(i)
        t[i], t[j] = t[j], t[i]
    end
end

-- ── Persistent state (survives activate / deactivate) ─────────────────────────

local _pagoda_state    = "main_menu"
local _challenge_level = 1
local _practice_level  = 1
local _in_challenge    = false   -- true  = we just exited to play (challenge)
local _in_practice     = false   -- true  = we just exited to play (practice)
local _song_list_cache    = nil
local _missing_song_count = nil   -- nil = not yet validated; >=0 after first check

-- Challenge-mode preview (pre-rolled songs + speed slider)
local _preview_songs   = nil    -- { {color, node, entry}, ... } for blue/green/red
local _preview_purple  = nil    -- { node, entry } or nil
local _preview_level   = -1     -- which level the preview was rolled for
local _preview_speed   = 20     -- current slider value

-- ── Per-activation state ──────────────────────────────────────────────────────

local _callbacks          = nil
local _font_title         = nil
local _font_body          = nil
local _font_hint          = nil
local _puchi_sine_y       = 0.0
local _puchi_sine_counter = nil
local _status_msg         = ""
local _status_timer       = 0.0
local _menu_sel           = 1
local _practice_sel       = 1
local _result_was_clear   = false
local _btn_timer          = 0.0

local NP_X            = 20
local NP_Y            = 980
local PUCHI_FLOAT_AMP = 6.0

-- ── Song list ────────────────────────────────────────────────────────────────

local function _get_song_list()
    if _song_list_cache ~= nil then return _song_list_cache end
    local lsls = GenerateSongListSettings()
    lsls.AppendMainRandomBox  = false
    lsls.AppendSubRandomBoxes = false
    lsls.FlattenOpenedFolders = true
    _song_list_cache = RequestSongList(lsls)
    return _song_list_cache
end

-- ── Pool validation ──────────────────────────────────────────────────────────

local function _count_missing_songs()
    local pools = _load_pools()
    if pools == nil then return 0 end

    local lsls = GenerateSongListSettings()
    lsls.AppendMainRandomBox  = false
    lsls.AppendSubRandomBoxes = false
    lsls.FlattenOpenedFolders = true
    lsls.ExcludeHiddenSongs   = false
    local sl = RequestSongList(lsls)

    local diff_names = { [0] = "Easy", [1] = "Normal", [2] = "Hard", [3] = "Oni", [4] = "Edit" }

    local seen    = {}
    local missing = 0
    for _, pool in pairs(pools) do
        for _, color in ipairs({ "blue", "green", "red", "purple" }) do
            local entries = pool[color]
            if entries ~= nil then
                for _, entry in ipairs(entries) do
                    local key = (entry.id or "") .. "\0" .. tostring(entry.diff)
                    if not seen[key] then
                        seen[key] = true
                        local node  = sl:GetSongByUniqueId(entry.id)
                        local diff  = diff_names[entry.diff] or tostring(entry.diff)
                        if node == nil then
                            debugLog("[Pagoda] Missing song – id: " .. tostring(entry.id) .. "  diff: " .. diff)
                            missing = missing + 1
                        elseif node:GetChart(entry.diff) == nil then
                            local title = node.Title or entry.id
                            debugLog("[Pagoda] Missing difficulty – \"" .. title .. "\"  id: " .. tostring(entry.id) .. "  diff: " .. diff)
                            missing = missing + 1
                        end
                    end
                end
            end
        end
    end
    return missing
end

-- ── Persistence helpers ───────────────────────────────────────────────────────

local function _highest_level()
    return math.max(1, math.floor(GetSaveFile(0):GetGlobalCounter("pagoda_highest_level")))
end

local function _set_highest_level(level)
    if level > _highest_level() then
        GetSaveFile(0):SetGlobalCounter("pagoda_highest_level", level)
    end
end

-- Returns the checkpoint level to restart from when failing at `level`.
local function _checkpoint_for(level)
    if level >= 16     then return 16
    elseif level >= 11 then return 11
    elseif level >= 6  then return 6
    else                    return 1
    end
end

-- Returns the list of valid challenge starting levels for the current save.
local function _start_options()
    local highest = _highest_level()
    local opts = { 1, 6 }
    if highest >= 10 then table.insert(opts, 11) end
    if highest >= 15 then table.insert(opts, 16) end
    return opts
end

-- ── Challenge preview (pre-rolled song selection) ────────────────────────────

local DIFF_NAMES = { [0] = "Easy", [1] = "Normal", [2] = "Hard", [3] = "Oni", [4] = "Edit" }

local function _spd_range(level)
    local min = (level >= 33) and (20 + (level - 32)) or 20
    local max = min + 20
    return min, max
end

-- Roll and cache the songs that will be used for the given challenge level.
-- Also initialises _preview_speed for the slider.
local function _build_preview(level)
    _preview_songs  = {}
    _preview_purple = nil
    _preview_level  = level

    -- Initialise speed to x1 (or the level-scaled base for lv33+)
    local spd_min = _spd_range(level)
    _preview_speed = spd_min

    local sl   = _get_song_list()
    local pool = _get_pool(level)
    if pool == nil then return end

    local used_ids = {}
    local colors = { "blue", "green", "red" }
    _shuffle(colors)

    for _, color in ipairs(colors) do
        local entries = pool[color]
        if entries ~= nil and #entries > 0 then
            local entry = _pick_entry(entries, used_ids)
            if entry ~= nil then
                local node = sl:GetSongByUniqueId(entry.id)
                if node ~= nil and node.NotNull then
                    table.insert(_preview_songs, { color = color, node = node, entry = entry })
                    used_ids[entry.id] = true
                end
            end
        end
    end

    if math.random() < _purple_prob(level) then
        local entries = pool["purple"]
        if entries ~= nil and #entries > 0 then
            local entry = _pick_entry(entries, used_ids)
            if entry ~= nil then
                local node = sl:GetSongByUniqueId(entry.id)
                if node ~= nil and node.NotNull then
                    _preview_purple = { node = node, entry = entry }
                end
            end
        end
    end
end

local function _clear_preview()
    _preview_songs  = nil
    _preview_purple = nil
    _preview_level  = -1
end

-- ── Dan building ─────────────────────────────────────────────────────────────

local function _build_dan(level)
    local tc = _level_tick_color(level)

    DANBUILDER:Clear()
    DANBUILDER:SetTitle(_level_name(level))
    DANBUILDER:SetDanTick(_level_tick(level))
    DANBUILDER:SetDanTickColor(tc[1], tc[2], tc[3])

    local added_count = 0
    local had_purple  = false

    if _preview_songs ~= nil and _preview_level == level then
        -- Use pre-rolled songs from the preview
        for _, item in ipairs(_preview_songs) do
            DANBUILDER:AddSong(item.node, item.entry.diff)
            added_count = added_count + 1
        end
        if _preview_purple ~= nil then
            DANBUILDER:AddSong(_preview_purple.node, _preview_purple.entry.diff)
            added_count = added_count + 1
            had_purple  = true
        end
        _clear_preview()
    else
        -- Fallback: roll fresh (practice mode, or no preview cached)
        local sl   = _get_song_list()
        local pool = _get_pool(level)
        local used_ids = {}
        local colors = { "blue", "green", "red" }
        _shuffle(colors)

        for _, color in ipairs(colors) do
            local entries = pool[color]
            if entries ~= nil and #entries > 0 then
                local entry = _pick_entry(entries, used_ids)
                if entry ~= nil then
                    local node = sl:GetSongByUniqueId(entry.id)
                    if node ~= nil and node.NotNull then
                        DANBUILDER:AddSong(node, entry.diff)
                        used_ids[entry.id] = true
                        added_count = added_count + 1
                    end
                end
            end
        end

        if math.random() < _purple_prob(level) then
            local entries = pool["purple"]
            if entries ~= nil and #entries > 0 then
                local entry = _pick_entry(entries, used_ids)
                if entry ~= nil then
                    local node = sl:GetSongByUniqueId(entry.id)
                    if node ~= nil and node.NotNull then
                        DANBUILDER:AddSong(node, entry.diff)
                        added_count = added_count + 1
                        had_purple  = true
                    end
                end
            end
        end

        -- Use default speed when no preview (practice, or missing cache)
        _preview_speed = (level >= 33) and (20 + (level - 32)) or 20
    end

    if added_count == 0 then return false end

    -- EXAM 1: Gauge
    local g_red, g_gold = _exam1_gauge(level)
    DANBUILDER:SetGlobalExam(1, "g", g_red, g_gold, false)

    -- EXAM 2: Accuracy (levels 1-15, More) / Ok count (levels 16+, Less)
    local e2_type, e2_red, e2_gold, e2_less = _exam2(level)
    DANBUILDER:SetGlobalExam(2, e2_type, e2_red, e2_gold, e2_less)

    -- EXAM 3: Bad count (Less); purple multiplies threshold by 1.25 (floor)
    local bad_red, bad_gold = _exam3_base(level)
    if had_purple then
        bad_red  = math.floor(bad_red  * 1.25)
        bad_gold = math.floor(bad_gold * 1.25)
    end
    DANBUILDER:SetGlobalExam(3, "jb", bad_red, bad_gold, true)

    CONFIG.SongSpeed = _preview_speed
    CONFIG:SetAutoStatus(0, false)

    return DANBUILDER:Mount()
end

-- ── Draw helpers ─────────────────────────────────────────────────────────────

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

local function _ensure_fonts()
    if _font_title ~= nil then return end
    _font_title = TEXT:Create(44, "regular")
    _font_body  = TEXT:Create(32, "regular")
    _font_hint  = TEXT:Create(22, "regular")
end

local function _set_status(msg, t)
    _status_msg   = msg
    _status_timer = t or 2.0
end

-- ── Public API ────────────────────────────────────────────────────────────────

function M.is_returning_from_play()
    return _in_challenge or _in_practice
end

-- Called when the player selects Pagoda from the 3-way menu
function M.enter(shared)
    _callbacks    = shared
    _btn_timer    = 0.15
    _status_msg   = ""
    _status_timer = 0.0
    _ensure_fonts()

    if _missing_song_count == nil then
        _missing_song_count = _count_missing_songs()
    end

    if _missing_song_count > 0 then
        _pagoda_state = "missing_songs"
    else
        _pagoda_state = "main_menu"
        _menu_sel     = 1
    end

    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL) end

    _puchi_sine_counter = COUNTER:CreateCounter(0, 360, 1 / 120)
    _puchi_sine_counter:SetLoop(true)
    _puchi_sine_counter:Start()
end

-- Called when the player returns to the 3-way menu
function M.leave()
    _in_challenge = false
    _in_practice  = false
    _pagoda_state = "main_menu"
    M.deactivate()
end

-- Called from Script.lua's activate() whenever the pagoda module is active
function M.activate(shared)
    _callbacks = shared
    _btn_timer = 0.15
    _ensure_fonts()

    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then chara:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL) end

    _puchi_sine_counter = COUNTER:CreateCounter(0, 360, 1 / 120)
    _puchi_sine_counter:SetLoop(true)
    _puchi_sine_counter:Start()
end

-- Called from Script.lua's deactivate()
function M.deactivate()
    local chara = GetSaveFile(0):GetCharacter()
    if chara ~= nil and chara.IsValid then chara:DisposeAnimation(CHARACTER.ANIM_MENU_NORMAL) end
    _puchi_sine_counter = nil
end

-- Called in Script.lua's activate() after activate(), only when is_returning_from_play() is true.
-- PLAYSTATE still holds valid data from the just-completed dan at this point.
function M.on_return(shared)
    _callbacks = shared
    local passed = not PLAYSTATE:WasPlayAborted() and PLAYSTATE:IsPass()
    _result_was_clear = passed

    if _in_challenge then
        _in_challenge = false
        if passed then
            _set_highest_level(_challenge_level)   -- record the level we just cleared
            _challenge_level = _challenge_level + 1
            _pagoda_state = "level_clear"
        else
            _pagoda_state = "game_over"
        end
    elseif _in_practice then
        _in_practice  = false
        _pagoda_state = "practice_result"
    end

    _menu_sel  = 1
    _btn_timer = 0.15
end

function M.afterSongEnum()
    _song_list_cache    = nil
    _pools_cache        = nil
    _missing_song_count = nil
    _clear_preview()
end

function M.destroy()
    if _font_title ~= nil then _font_title:Dispose() ; _font_title = nil end
    if _font_body  ~= nil then _font_body:Dispose()  ; _font_body  = nil end
    if _font_hint  ~= nil then _font_hint:Dispose()  ; _font_hint  = nil end
end

-- Returns: nil | "back" (return to 3-way menu) | "play" (exit to play)
function M.update(dt)
    _btn_timer    = math.max(0, _btn_timer - dt)
    _status_timer = math.max(0, _status_timer - dt)

    if _puchi_sine_counter ~= nil then
        _puchi_sine_counter:Tick()
        _puchi_sine_y = math.sin(_puchi_sine_counter.Value * math.pi / 180) * PUCHI_FLOAT_AMP
    end

    if INPUT:KeyboardPressed("F3") then
        CONFIG:SetAutoStatus(0, not CONFIG:GetAutoStatus(0))
    end

    if _btn_timer > 0 then return nil end

    local up_p   = INPUT:Pressed("LeftChange")  or INPUT:KeyboardPressed("UpArrow")
    local down_p = INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("DownArrow")
    local ok_p   = INPUT:Pressed("Decide")      or INPUT:KeyboardPressed("Return")
    local back_p = INPUT:Pressed("Cancel")      or INPUT:KeyboardPressed("Escape")

    -- ── MISSING SONGS ──────────────────────────────────────────────────────────
    if _pagoda_state == "missing_songs" then
        if ok_p or back_p then return "back" end
        return nil
    end

    -- ── MAIN MENU ──────────────────────────────────────────────────────────────
    if _pagoda_state == "main_menu" then
        if up_p   then _menu_sel = math.max(1, _menu_sel - 1) end
        if down_p then _menu_sel = math.min(3, _menu_sel + 1) end
        if back_p then return "back" end
        if ok_p then
            if _menu_sel == 1 then
                _pagoda_state = "start_choice" ; _menu_sel = 1
            elseif _menu_sel == 2 then
                _practice_sel = 1 ; _pagoda_state = "practice_select"
            elseif _menu_sel == 3 then
                return "back"
            end
        end
        return nil
    end

    -- ── START CHOICE ───────────────────────────────────────────────────────────
    if _pagoda_state == "start_choice" then
        local opts = _start_options()
        local n    = #opts + 1  -- +1 for Cancel
        if up_p   then _menu_sel = math.max(1, _menu_sel - 1) end
        if down_p then _menu_sel = math.min(n, _menu_sel + 1) end
        if back_p then _pagoda_state = "main_menu" ; _menu_sel = 1 ; return nil end
        if ok_p then
            if _menu_sel <= #opts then
                _challenge_level = opts[_menu_sel]
                _build_preview(_challenge_level)
                _pagoda_state = "level_preview"
            else
                _pagoda_state = "main_menu" ; _menu_sel = 1
            end
        end
        return nil
    end

    -- ── LEVEL PREVIEW (challenge) ───────────────────────────────────────────────
    if _pagoda_state == "level_preview" then
        local highest  = _highest_level()
        local is_past  = (_challenge_level < highest)
        local spd_min, spd_max = _spd_range(_challenge_level)

        -- Speed slider (left/right), only for already-cleared levels
        if is_past then
            if INPUT:KeyboardPressed("LeftArrow")  or INPUT:Pressed("LeftChange")  then
                _preview_speed = math.max(spd_min, _preview_speed - 1)
            end
            if INPUT:KeyboardPressed("RightArrow") or INPUT:Pressed("RightChange") then
                _preview_speed = math.min(spd_max, _preview_speed + 1)
            end
        end

        if back_p then
            _clear_preview()
            _pagoda_state = "main_menu" ; _menu_sel = 1
            return nil
        end
        if ok_p then
            local ok = _build_dan(_challenge_level)
            if ok then
                _in_challenge = true
                if _callbacks ~= nil then _callbacks.stopBGM() end
                return "play"
            else
                _set_status("Failed to load songs!", 3.0)
            end
        end
        return nil
    end

    -- ── LEVEL CLEAR ────────────────────────────────────────────────────────────
    if _pagoda_state == "level_clear" then
        if up_p or INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
            _menu_sel = math.max(1, _menu_sel - 1)
        end
        if down_p or INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
            _menu_sel = math.min(2, _menu_sel + 1)
        end
        if ok_p or back_p then
            if _menu_sel == 1 and not back_p then
                _build_preview(_challenge_level)
                _pagoda_state = "level_preview"
            else
                _pagoda_state = "main_menu" ; _menu_sel = 1
                if _callbacks ~= nil then _callbacks.startBGM() end
            end
        end
        return nil
    end

    -- ── GAME OVER ──────────────────────────────────────────────────────────────
    if _pagoda_state == "game_over" then
        if up_p or INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
            _menu_sel = math.max(1, _menu_sel - 1)
        end
        if down_p or INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
            _menu_sel = math.min(2, _menu_sel + 1)
        end
        if ok_p or back_p then
            if _menu_sel == 1 and not back_p then
                local cp = _checkpoint_for(_challenge_level)
                _challenge_level = cp
                _build_preview(cp)
                _pagoda_state = "level_preview"
            else
                _pagoda_state = "main_menu" ; _menu_sel = 1
                if _callbacks ~= nil then _callbacks.startBGM() end
            end
        end
        return nil
    end

    -- ── PRACTICE SELECT ────────────────────────────────────────────────────────
    if _pagoda_state == "practice_select" then
        local highest = math.max(_highest_level(), 6)
        if up_p   then _practice_sel = math.max(1,       _practice_sel - 1) end
        if down_p then _practice_sel = math.min(highest, _practice_sel + 1) end
        if back_p then _pagoda_state = "main_menu" ; _menu_sel = 1 ; return nil end
        if ok_p   then _practice_level = _practice_sel ; _pagoda_state = "practice_preview" ; _menu_sel = 1 end
        return nil
    end

    -- ── PRACTICE PREVIEW ───────────────────────────────────────────────────────
    if _pagoda_state == "practice_preview" then
        if up_p or INPUT:Pressed("LeftChange") or INPUT:KeyboardPressed("LeftArrow") then
            _menu_sel = math.max(1, _menu_sel - 1)
        end
        if down_p or INPUT:Pressed("RightChange") or INPUT:KeyboardPressed("RightArrow") then
            _menu_sel = math.min(2, _menu_sel + 1)
        end
        if back_p then _pagoda_state = "practice_select" ; return nil end
        if ok_p then
            if _menu_sel == 1 then
                local ok = _build_dan(_practice_level)
                if ok then
                    _in_practice = true
                    if _callbacks ~= nil then _callbacks.stopBGM() end
                    return "play"
                else
                    _set_status("Failed to load songs!", 3.0)
                end
            else
                _pagoda_state = "practice_select"
            end
        end
        return nil
    end

    -- ── PRACTICE RESULT ────────────────────────────────────────────────────────
    if _pagoda_state == "practice_result" then
        if ok_p or back_p then
            _pagoda_state = "practice_select"
            if _callbacks ~= nil then _callbacks.startBGM() end
        end
        return nil
    end

    return nil
end

-- ── Draw ─────────────────────────────────────────────────────────────────────

function M.draw()
    if _font_title == nil then return end

    local res   = THEME:GetResolution()
    local res_w = res.X
    local res_h = res.Y
    local cx    = res_w / 2

    local C_WHITE  = COLOR:CreateColorFromRGBA(255, 255, 255, 255)
    local C_DIM    = COLOR:CreateColorFromRGBA(180, 180, 180, 220)
    local C_SEL    = COLOR:CreateColorFromRGBA(255, 220,  80, 255)
    local C_PASS   = COLOR:CreateColorFromRGBA(100, 255, 150, 255)
    local C_FAIL   = COLOR:CreateColorFromRGBA(255,  80,  80, 255)

    local function opt(label, x, y, sel)
        _font_body:GetText(label, false, 900, sel and C_SEL or C_DIM):DrawAtAnchor(x, y, "center")
    end

    local function level_color(level)
        local tc = _level_tick_color(level)
        return COLOR:CreateColorFromRGBA(tc[1], tc[2], tc[3], 255)
    end

    local base_y = res_h / 2

    -- ── MISSING SONGS ──────────────────────────────────────────────────────────
    if _pagoda_state == "missing_songs" then
        _font_body:GetText("Some songs are missing to play this game mode",    false, 900, C_FAIL):DrawAtAnchor(cx, base_y - 40,  "center")
        _font_body:GetText("Please update the OpenTaiko soundtrack and try again", false, 900, C_WHITE):DrawAtAnchor(cx, base_y + 20, "center")
        _font_hint:GetText("(Missing song count: " .. tostring(_missing_song_count) .. ")", false, 600, C_DIM):DrawAtAnchor(cx, base_y + 80, "center")
        _font_hint:GetText("Press any button to go back", false, 600, C_DIM):DrawAtAnchor(cx, base_y + 130, "center")

    -- ── MAIN MENU ──────────────────────────────────────────────────────────────
    elseif _pagoda_state == "main_menu" then
        _font_title:GetText("Pagoda of the Unknown", false, 800, C_WHITE):DrawAtAnchor(cx, base_y - 150, "center")

        local hl = _highest_level()
        _font_hint:GetText(
            "Highest reached: " .. _level_name(hl) .. "  (Lv." .. tostring(hl) .. ")",
            false, 700, C_DIM):DrawAtAnchor(cx, base_y - 90, "center")

        opt("Challenge",    cx, base_y - 10,  _menu_sel == 1)
        opt("Practice",     cx, base_y + 55,  _menu_sel == 2)
        opt("Back",         cx, base_y + 120, _menu_sel == 3)

        _font_hint:GetText(
            "[Up/Down] Select   [Confirm] OK   [Cancel] Back",
            false, 800, C_DIM):DrawAtAnchor(cx, res_h - 50, "center")

    -- ── START CHOICE ───────────────────────────────────────────────────────────
    elseif _pagoda_state == "start_choice" then
        _font_title:GetText("Choose starting level", false, 800, C_WHITE):DrawAtAnchor(cx, base_y - 130, "center")
        local opts = _start_options()
        for i, lv in ipairs(opts) do
            opt("Lv." .. tostring(lv) .. " — " .. _level_name(lv),
                cx, base_y - 30 + (i - 1) * 55, _menu_sel == i)
        end
        opt("Cancel", cx, base_y - 30 + #opts * 55, _menu_sel == #opts + 1)

    -- ── LEVEL PREVIEW ──────────────────────────────────────────────────────────
    elseif _pagoda_state == "level_preview" then
        local lv      = _challenge_level
        local name    = _level_name(lv)
        local highest = _highest_level()
        local is_past = (lv < highest)
        local gap     = highest - lv   -- levels below the frontier

        _font_title:GetText("Challenge  " .. name, false, 800, level_color(lv)):DrawAtAnchor(cx, base_y - 200, "center")
        _font_hint:GetText("Lv." .. tostring(lv), false, 200, C_DIM):DrawAtAnchor(cx, base_y - 150, "center")

        local prob = _purple_prob(lv)
        local sc_text
        if prob <= 0.0 then
            sc_text = "3 songs"
        elseif prob >= 1.0 then
            sc_text = "4 songs"
        else
            sc_text = string.format("3~4 songs (Purple %d%%)", math.floor(prob * 100))
        end
        _font_hint:GetText("Songs: " .. sc_text, false, 500, C_DIM):DrawAtAnchor(cx, base_y - 115, "center")

        -- local gr, gg = _exam1_gauge(lv)
        -- _font_hint:GetText(
        --     string.format("Clear condition: gauge %d%% (red) / %d%% (gold)", gr, gg),
        --     false, 700, C_DIM):DrawAtAnchor(cx, base_y - 82, "center")

        -- Speed slider — only for levels the player has already passed
        if is_past then
            local spd_min, spd_max = _spd_range(lv)
            local spd_label = string.format("◄  Speed  x%.2f  ►      min x%.2f  /  max x%.2f",
                _preview_speed / 20.0, spd_min / 20.0, spd_max / 20.0)
            _font_hint:GetText(spd_label, false, 800, C_SEL):DrawAtAnchor(cx, base_y - 50, "center")
        end

        -- Song list reveal
        if is_past and gap > 3 and _preview_songs ~= nil then
            local reveal_purple = (gap > 5)
            local list_y        = base_y - 10
            local COLOR_LABELS  = { blue = "Blue", green = "Green", red = "Red" }
            for i, item in ipairs(_preview_songs) do
                local title = (item.node ~= nil) and (item.node.Title or "???") or "???"
                local diff  = (item.entry ~= nil) and (DIFF_NAMES[item.entry.diff] or "?") or "?"
                local lbl   = string.format("[ %s ]  %s  (%s)",
                    COLOR_LABELS[item.color] or item.color, title, diff)
                _font_hint:GetText(lbl, false, 700, C_DIM):DrawAtAnchor(cx, list_y + (i - 1) * 30, "center")
            end
            -- Purple slot
            local purple_y = list_y + 3 * 30
            if reveal_purple then
                if _preview_purple ~= nil then
                    local title = (_preview_purple.node ~= nil) and (_preview_purple.node.Title or "???") or "???"
                    local diff  = (_preview_purple.entry ~= nil) and (DIFF_NAMES[_preview_purple.entry.diff] or "?") or "?"
                    local lbl   = string.format("[ Purple ]  %s  (%s)", title, diff)
                    local C_PURPLE = COLOR:CreateColorFromRGBA(200, 100, 255, 220)
                    _font_hint:GetText(lbl, false, 700, C_PURPLE):DrawAtAnchor(cx, purple_y, "center")
                else
                    _font_hint:GetText("[ Purple ]  —  (none)", false, 700, C_DIM):DrawAtAnchor(cx, purple_y, "center")
                end
            else
                local C_MYS = COLOR:CreateColorFromRGBA(160, 100, 200, 180)
                _font_hint:GetText("[ ??? ]  A mysterious force lurks beyond...", false, 700, C_MYS):DrawAtAnchor(cx, purple_y, "center")
            end
        end

        -- Start / Cancel (Confirm / Cancel buttons — no menu_sel)
        local C_START = COLOR:CreateColorFromRGBA(100, 255, 150, 255)
        _font_body:GetText("Confirm  —  Start", false, 900, C_START):DrawAtAnchor(cx, base_y + 145, "center")
        _font_body:GetText("Cancel  —  Back",   false, 900, C_DIM):DrawAtAnchor(cx, base_y + 205, "center")

    -- ── LEVEL CLEAR ────────────────────────────────────────────────────────────
    elseif _pagoda_state == "level_clear" then
        local cleared_name = _level_name(_challenge_level - 1)
        local next_name    = _level_name(_challenge_level)
        _font_title:GetText("CLEAR!", false, 600, C_PASS):DrawAtAnchor(cx, base_y - 130, "center")
        _font_body:GetText(cleared_name .. " cleared!", false, 700, C_WHITE):DrawAtAnchor(cx, base_y - 65, "center")
        _font_hint:GetText(
            "Next: " .. next_name .. "  (Lv." .. tostring(_challenge_level) .. ")",
            false, 700, C_DIM):DrawAtAnchor(cx, base_y - 15, "center")
        opt("Continue", cx - 130, base_y + 70, _menu_sel == 1)
        opt("Quit",     cx + 130, base_y + 70, _menu_sel == 2)

    -- ── GAME OVER ──────────────────────────────────────────────────────────────
    elseif _pagoda_state == "game_over" then
        local failed_name = _level_name(_challenge_level)
        _font_title:GetText("GAME OVER", false, 600, C_FAIL):DrawAtAnchor(cx, base_y - 130, "center")
        _font_body:GetText("Failed at " .. failed_name, false, 700, C_WHITE):DrawAtAnchor(cx, base_y - 65, "center")
        local hl = _highest_level()
        _font_hint:GetText(
            "Highest reached: " .. _level_name(hl) .. "  (Lv." .. tostring(hl) .. ")",
            false, 600, C_DIM):DrawAtAnchor(cx, base_y - 15, "center")
        local cp = _checkpoint_for(_challenge_level)
        local retry_lbl = (cp == _challenge_level) and "Retry" or ("Retry from Lv." .. tostring(cp))
        opt(retry_lbl,   cx, base_y + 70, _menu_sel == 1)
        opt("Main menu", cx, base_y + 130, _menu_sel == 2)

    -- ── PRACTICE SELECT ────────────────────────────────────────────────────────
    elseif _pagoda_state == "practice_select" then
        local highest = math.max(_highest_level(), 6)
        _font_title:GetText("Practice", false, 600, C_WHITE):DrawAtAnchor(cx, base_y - 170, "center")

        local start_lv = math.max(1, _practice_sel - 4)
        local end_lv   = math.min(highest, start_lv + 8)
        for lv = start_lv, end_lv do
            local y   = base_y - 100 + (lv - start_lv) * 36
            local sel = (lv == _practice_sel)
            local col = sel and level_color(lv) or C_DIM
            local lbl = string.format("Lv.%d  %s", lv, _level_name(lv))
            local f   = sel and _font_body or _font_hint
            f:GetText(lbl, false, 600, col):DrawAtAnchor(cx, y, "center")
        end

        _font_hint:GetText(
            "[Up/Down] Select   [Confirm] Practice   [Cancel] Back",
            false, 800, C_DIM):DrawAtAnchor(cx, res_h - 50, "center")

    -- ── PRACTICE PREVIEW ───────────────────────────────────────────────────────
    elseif _pagoda_state == "practice_preview" then
        local lv   = _practice_level
        local name = _level_name(lv)
        _font_title:GetText("Practice  " .. name, false, 800, level_color(lv)):DrawAtAnchor(cx, base_y - 130, "center")
        -- local gr, gg = _exam1_gauge(lv)
        -- _font_hint:GetText(
        --     string.format("Clear condition: gauge %d%% (red) / %d%% (gold)", gr, gg),
        --     false, 700, C_DIM):DrawAtAnchor(cx, base_y - 40, "center")
        opt("Start",  cx - 130, base_y + 50, _menu_sel == 1)
        opt("Cancel", cx + 130, base_y + 50, _menu_sel == 2)

    -- ── PRACTICE RESULT ────────────────────────────────────────────────────────
    elseif _pagoda_state == "practice_result" then
        local name = _level_name(_practice_level)
        if _result_was_clear then
            _font_title:GetText("CLEAR!", false, 600, C_PASS):DrawAtAnchor(cx, base_y - 70, "center")
        else
            _font_title:GetText("FAILED", false, 600, C_FAIL):DrawAtAnchor(cx, base_y - 70, "center")
        end
        _font_body:GetText(name, false, 400, C_WHITE):DrawAtAnchor(cx, base_y, "center")
        _font_hint:GetText("Press any button to continue", false, 500, C_DIM):DrawAtAnchor(cx, base_y + 60, "center")
    end

    -- Status flash
    if _status_timer > 0 then
        local alpha = math.min(255, math.floor(_status_timer * 255))
        _font_hint:GetText(_status_msg, false, 900,
            COLOR:CreateColorFromRGBA(255, 220, 80, alpha)):DrawAtAnchor(cx, res_h - 90, "center")
    end

    -- Nameplate + character
    NAMEPLATE:DrawPlayerNameplate(NP_X, NP_Y, 255, 0)
    _draw_player_chara(NP_X + 140, NP_Y - 6,            1.0)
    _draw_player_puchi(NP_X + 220, NP_Y + _puchi_sine_y, 1.0)
end

return M
