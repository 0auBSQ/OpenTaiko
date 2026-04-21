-- ╔══════════════════════════════════════════════════════════════════════════╗
-- ║  dan_builder_test  —  Dynamic Dan Builder Test Stage                     ║
-- ║                                                                          ║
-- ║  Controls:                                                               ║
-- ║    Q        Decrease song count by 1 (min 1)                             ║
-- ║    W        Increase song count by 1 (max 9)                             ║
-- ║    A        Shift preferred difficulty down (Easy → Edit wraps)          ║
-- ║    S        Shift preferred difficulty up   (Edit → Easy wraps)          ║
-- ║    P        Randomise songs from the full song list                      ║
-- ║    L        Mount & launch the Dan with preset exams                     ║
-- ║    Escape   Return to title                                               ║
-- ╚══════════════════════════════════════════════════════════════════════════╝

-- ── Constants ────────────────────────────────────────────────────────────────

local MIN_SONGS = 1
local MAX_SONGS = 9

-- Difficulty constants
local DIFF_EASY   = 0
local DIFF_NORMAL = 1
local DIFF_HARD   = 2
local DIFF_ONI    = 3
local DIFF_EDIT   = 4

-- ── Fonts & layout ───────────────────────────────────────────────────────────

local font_title  = nil
local font_body   = nil
local font_hint   = nil

local DIFF_NAMES  = { [0]="Easy", [1]="Normal", [2]="Hard", [3]="Oni", [4]="Edit", [5]="Tower", [6]="Dan" }
local DIFF_COLORS = {}  -- populated in onStart() via COLOR:CreateColorFromRGBA

local RES_H = 1080

-- ── Song list (populated in afterSongEnum) ───────────────────────────────────

local all_songs     = {}   -- flat list of LuaSongNode (songs only, all difficulties)
local song_count    = 3    -- how many songs to put in the Dan
local selected      = {}   -- { node=LuaSongNode, diff=int } list, length == song_count

-- Preferred maximum difficulty for randomised picks.
-- A/S keys cycle this. _best_diff() will try this first, then fall back downward.
-- nil = use default order (Oni > Hard > Normal > Easy > Edit)
local pref_diff     = nil  -- nil | DIFF_EASY | DIFF_NORMAL | DIFF_HARD | DIFF_ONI | DIFF_EDIT

local DIFF_ORDER    = { DIFF_ONI, DIFF_HARD, DIFF_NORMAL, DIFF_EASY, DIFF_EDIT }

-- ── State ─────────────────────────────────────────────────────────────────────

local enumerated    = false
local status_msg    = ""
local status_timer  = 0

-- ─────────────────────────────────────────────────────────────────────────────
-- Lifecycle
-- ─────────────────────────────────────────────────────────────────────────────

function onStart()
    font_title = TEXT:Create(32, "regular")
    font_body  = TEXT:Create(24, "regular")
    font_hint  = TEXT:Create(18, "regular")

    DIFF_COLORS[0] = COLOR:CreateColorFromRGBA(100, 180, 255, 255)
    DIFF_COLORS[1] = COLOR:CreateColorFromRGBA(100, 220, 100, 255)
    DIFF_COLORS[2] = COLOR:CreateColorFromRGBA(255, 180, 60,  255)
    DIFF_COLORS[3] = COLOR:CreateColorFromRGBA(255, 80,  80,  255)
    DIFF_COLORS[4] = COLOR:CreateColorFromRGBA(180, 80,  255, 255)
    _init_colors()
end

function activate()
    CONFIG.PlayerCount = 1
    status_msg   = ""
    status_timer = 0
end

function deactivate()
end

function afterSongEnum()
    -- Collect every leaf song node from the full song list
    local lsls = GenerateSongListSettings()
    lsls.AppendMainRandomBox  = false
    lsls.AppendSubRandomBoxes = false
    lsls.SubBackBoxFrequency  = 0
    lsls.HideEmptyFolders     = true
    lsls.FlattenOpenedFolders = true

    -- SearchSongsByPredicate returns a C# List<LuaSongNode> (0-based, .Count)
    -- Exclude Tower/Dan-only nodes (diffs 5/6) — they have no hittable notes and break Dan logic
    local song_list  = RequestSongList(lsls)
    local cs_results = song_list:SearchSongsByPredicate(function(node)
        if not node.NotNull then return false end
        for _, d in ipairs({ DIFF_EASY, DIFF_NORMAL, DIFF_HARD, DIFF_ONI, DIFF_EDIT }) do
            local chart = node:GetChart(d)
            if chart ~= nil and chart.NotNull then return true end
        end
        return false
    end)

    all_songs = {}
    for i = 0, cs_results.Count - 1 do
        all_songs[#all_songs + 1] = cs_results[i]
    end

    enumerated = true

    -- Auto-randomise on first load
    if #all_songs > 0 then
        _randomise()
    end
end

function onDestroy()
    if font_title ~= nil then font_title:Dispose() ; font_title = nil end
    if font_body  ~= nil then font_body:Dispose()  ; font_body  = nil end
    if font_hint  ~= nil then font_hint:Dispose()  ; font_hint  = nil end
end

-- ─────────────────────────────────────────────────────────────────────────────
-- Song selection helpers
-- ─────────────────────────────────────────────────────────────────────────────

function _randomise()
    selected = {}
    if #all_songs == 0 then
        status_msg   = "No songs available!"
        status_timer = 3.0
        return
    end

    -- Fisher-Yates shuffle on indices, pick first song_count
    local indices = {}
    for i = 1, #all_songs do indices[i] = i end
    for i = #indices, 2, -1 do
        local j = math.random(1, i)
        indices[i], indices[j] = indices[j], indices[i]
    end

    local pick_count = math.min(song_count, #all_songs)
    for k = 1, pick_count do
        local node = all_songs[indices[k]]
        -- Pick the highest available difficulty for variety
        local diff = _best_diff(node)
        selected[#selected + 1] = { node = node, diff = diff }
    end

    -- Pad with repeated picks if fewer songs than required
    while #selected < song_count and #all_songs > 0 do
        local node = all_songs[math.random(1, #all_songs)]
        selected[#selected + 1] = { node = node, diff = _best_diff(node) }
    end

    status_msg   = "Songs randomised!"
    status_timer = 1.5
end

function _best_diff(node)
    -- If a preferred difficulty is set, try it first, then fall back through the
    -- remaining difficulties in descending order (wrapping around Edit → Easy).
    local prefs
    if pref_diff ~= nil then
        -- Build a priority list starting from pref_diff, walking DIFF_ORDER from that point.
        -- Find pref_diff's position in DIFF_ORDER and rotate the list so it's first.
        local start = 1
        for i, d in ipairs(DIFF_ORDER) do
            if d == pref_diff then start = i break end
        end
        prefs = {}
        for i = 0, #DIFF_ORDER - 1 do
            prefs[#prefs + 1] = DIFF_ORDER[(start - 1 + i) % #DIFF_ORDER + 1]
        end
    else
        prefs = DIFF_ORDER
    end
    for _, d in ipairs(prefs) do
        local chart = node:GetChart(d)
        if chart ~= nil and chart.NotNull then return d end
    end
    return DIFF_ONI
end

-- Cycle pref_diff through Easy/Normal/Hard/Oni/Edit in the given direction (+1 or -1).
local function _shift_pref_diff(dir)
    -- Cycle order for A/S: Easy → Normal → Hard → Oni → Edit (and wrap)
    local cycle = { DIFF_EASY, DIFF_NORMAL, DIFF_HARD, DIFF_ONI, DIFF_EDIT }
    local current = pref_diff ~= nil and pref_diff or DIFF_ONI
    local idx = 1
    for i, d in ipairs(cycle) do
        if d == current then idx = i break end
    end
    idx = (idx - 1 + dir + #cycle) % #cycle + 1
    pref_diff = cycle[idx]
    _randomise()
end

-- ─────────────────────────────────────────────────────────────────────────────
-- Dan launch
-- ─────────────────────────────────────────────────────────────────────────────

function _launch_dan()
    if #selected == 0 then
        status_msg   = "No songs selected — press P first!"
        status_timer = 3.0
        return
    end

    DANBUILDER:Clear()
    DANBUILDER:SetTitle("乱打")
    DANBUILDER:SetDanTick(2)
    DANBUILDER:SetDanTickColor(220, 80, 255)   -- vivid purple

    -- Load songs into builder
    for _, entry in ipairs(selected) do
        DANBUILDER:AddSong(entry.node, entry.diff)
    end

    -- Preset exams
    --  EXAM1: gauge > 90 / 95  (global)
    DANBUILDER:SetGlobalExam(1, "g", 90, 95, false)
    --  EXAM2: oks < 1000 / 500  (global)
    DANBUILDER:SetGlobalExam(2, "jg", 1000, 500, true)
    --  EXAM3: bads < 50 / 20  (per song)
    for i = 1, #selected do
        DANBUILDER:SetPerSongExam(i, 3, "jb", 50, 20, true)
    end

    local ok = DANBUILDER:Mount()
    if ok then
        return Exit("play", nil)
    else
        status_msg   = "Mount failed — check the log!"
        status_timer = 4.0
    end
end

-- ─────────────────────────────────────────────────────────────────────────────
-- Update
-- ─────────────────────────────────────────────────────────────────────────────

function update()
    local dt = fps.deltaTime

    if status_timer > 0 then
        status_timer = status_timer - dt
        if status_timer < 0 then status_timer = 0 end
    end

    if not enumerated then return end

    -- Escape → back to title
    if INPUT:KeyboardPressed("Escape") then
        return Exit("title", nil)
    end

    -- Q: decrease song count
    if INPUT:KeyboardPressed("Q") then
        song_count = math.max(MIN_SONGS, song_count - 1)
        _randomise()
    end

    -- W: increase song count
    if INPUT:KeyboardPressed("W") then
        song_count = math.min(MAX_SONGS, song_count + 1)
        _randomise()
    end

    -- A: preferred difficulty down (Easy → Edit wraps)
    if INPUT:KeyboardPressed("A") then
        _shift_pref_diff(-1)
    end

    -- S: preferred difficulty up (Edit → Easy wraps)
    if INPUT:KeyboardPressed("S") then
        _shift_pref_diff(1)
    end

    -- P: randomise
    if INPUT:KeyboardPressed("P") then
        _randomise()
    end

    -- L: launch
    if INPUT:KeyboardPressed("L") then
        _launch_dan()
    end
end

-- ─────────────────────────────────────────────────────────────────────────────
-- Draw helpers
-- ─────────────────────────────────────────────────────────────────────────────

local C_TITLE    = nil  -- initialised in onStart() after COLOR is available
local C_WHITE    = nil
local C_DIM      = nil
local C_COUNT    = nil
local C_NONE     = nil
local C_HINT     = nil
local C_EXAM_H   = nil
local C_EXAM_L   = nil
local C_GRAY     = nil
local C_FALLBACK = nil

function _init_colors()
    C_TITLE    = COLOR:CreateColorFromRGBA(255, 220,  80, 255)
    C_WHITE    = COLOR:CreateColorFromRGBA(255, 255, 255, 255)
    C_DIM      = COLOR:CreateColorFromRGBA(180, 180, 180, 255)
    C_COUNT    = COLOR:CreateColorFromRGBA(200, 200, 255, 255)
    C_NONE     = COLOR:CreateColorFromRGBA(200, 100, 100, 255)
    C_HINT     = COLOR:CreateColorFromRGBA(160, 160, 200, 255)
    C_EXAM_H   = COLOR:CreateColorFromRGBA(255, 200, 100, 255)
    C_EXAM_L   = COLOR:CreateColorFromRGBA(200, 240, 200, 255)
    C_GRAY     = COLOR:CreateColorFromRGBA(120, 120, 120, 255)
    C_FALLBACK = COLOR:CreateColorFromRGBA(200, 200, 200, 255)
end

function draw()
    if font_title == nil or font_body == nil or font_hint == nil then return end

    -- Title
    font_title:GetText("Dan Builder Test", false, 800, C_TITLE):Draw(60, 40)

    if not enumerated then
        font_body:GetText("Loading song list...", false, 800, C_WHITE):Draw(60, 120)
        return
    end

    -- Controls hint
    font_hint:GetText(
        "[Q] fewer   [W] more   [A/S] pref diff   [P] randomise   [L] launch Dan   [Esc] back",
        false, 1800, C_HINT
    ):Draw(60, RES_H - 50)

    -- Song count + preferred difficulty badge
    local pref_name = pref_diff ~= nil and (DIFF_NAMES[pref_diff] or "?") or "default (Oni)"
    local pref_col  = pref_diff ~= nil and (DIFF_COLORS[pref_diff] or C_FALLBACK) or C_DIM
    font_body:GetText(
        string.format("Songs: %d / %d available", song_count, #all_songs),
        false, 600, C_COUNT
    ):Draw(60, 100)
    font_body:GetText(
        string.format("Pref diff: %s", pref_name),
        false, 400, pref_col
    ):Draw(720, 100)

    -- Selected song list
    local list_y = 160
    font_body:GetText("Selected songs:", false, 600, C_DIM):Draw(60, list_y)
    list_y = list_y + 36

    if #selected == 0 then
        font_body:GetText("  (none — press P to randomise)", false, 700, C_NONE):Draw(60, list_y)
    else
        for i, entry in ipairs(selected) do
            local node  = entry.node
            local diff  = entry.diff
            local title = node.Title or "(unknown)"
            local dname = DIFF_NAMES[diff] or "?"
            local dcol  = DIFF_COLORS[diff] or C_FALLBACK

            -- Index number
            font_body:GetText(string.format("%d.", i), false, 28, C_GRAY):Draw(60, list_y)

            -- Difficulty badge
            font_body:GetText(string.format("[%s]", dname), false, 120, dcol):Draw(90, list_y)

            -- Song title
            font_body:GetText(title, false, 660, C_WHITE):Draw(220, list_y)

            -- Genre (dimmed)
            local genre = node.Genre or ""
            if genre ~= "" then
                font_hint:GetText(genre, false, 400, C_HINT):Draw(900, list_y + 4)
            end

            list_y = list_y + 34
        end
    end

    -- Exam summary
    local ex_y = list_y + 30
    font_body:GetText("Preset Exams:", false, 600, C_EXAM_H):Draw(60, ex_y)
    ex_y = ex_y + 34

    font_hint:GetText("EXAM 1 (global)   — Gauge  >= 90 (red) / 95 (gold)",   false, 900, C_EXAM_L):Draw(80, ex_y) ; ex_y = ex_y + 28
    font_hint:GetText("EXAM 2 (global)   — Goods < 1000 (red) / 500 (gold)",  false, 900, C_EXAM_L):Draw(80, ex_y) ; ex_y = ex_y + 28
    font_hint:GetText("EXAM 3 (per song) — Bads  < 50 (red) / 20 (gold)",     false, 900, C_EXAM_L):Draw(80, ex_y)

    -- Status message (fades out)
    if status_timer > 0 then
        local alpha = math.min(255, math.floor(status_timer * 255))
        font_body:GetText(status_msg, false, 900, COLOR:CreateColorFromRGBA(255, 220, 80, alpha)):Draw(60, RES_H - 90)
    end
end
