-- Vault.Talk: Mr.Caspian's small talks and Gris's story, shared by the secret vault (the talks over coffee and
-- Gris's scenes) and My Room's phone (Mr.Caspian's and Gris's numbers). The lines and their order are in
-- Vault.TalkLines, their text in lang/<code>/talk.json next to this file; the progress is in the save.
--
-- A conversation is a flow, run as a coroutine: Talk.flow(fn) wraps one, and each resume returns what the
-- stage has to do next, then the stage resumes it with the answer:
--   { "say", run }      run a Runner (Talk.runner): its lines, choices and menus; resume with the action
--                       name the runner stopped at ("play", "come", "gift", "unlock_gris", "unlock_song"), or nil
--                       once it is over
--   { "stage", name }   the vault's staging ("in", "out", "come", "back"); resume when it is done
--   { "play", node }    play the song (the stage leaves and comes back); resume with true when cleared
--   { "reward", r }     a reward pop-up (Vault.Chests.showReward shape, or { type = "character", entry });
--                       resume when it is closed
-- A Runner hands the stage its lines: run:step() returns "lines", nodes (the last one may carry choices:
-- answer with run:answer(i)), "action", name, or "done". A node: { who = "C" | "G" | "N", sprite, key,
-- fade = true when the screen dims around it, shown = true for a line said again under a choice,
-- choices = { { label }, ... } }.

local Lines = require("Vault.TalkLines")
local Daily = require("Shop.Daily")
local I18N = require("Locale.i18n")
local HERE = require("Core.Here")(...)

local T = I18N.texts("talk", HERE)
local M = {}

M.CASPIAN_NUMBER = "75ea5"
M.GRIS_NUMBER = "0.?.>.#."
M.GRIS_SONG = "irregularity01"
M.GRIS_FOLDER = "333 - Gris"                                       -- her character
M.DASHY_GENRE = "Dashy's Secrets"                                   -- the folder shown as Dashy's Depictions
M.GIFTS = { "gris_teddy", "gris_crane", "gris_cat", "gris_flowers" }  -- by her question in GP1
M.GIFT_RARITY = "Epic"

local GRIS_FROM = 6               -- talks before Gris shows up in the vault
local TANK_CHANCE = 0.5           -- on an odd day she swims in the tank this often, else she is away
local OFFER_CHANCE = { 0.1, 0.2, 0.5 }   -- after the day's first chest, the chance of an offer per chest opened
                                         -- (simple, gold, optk)
local OFFER_PITY = 8              -- the 8th chest opened in a row without an offer always brings one
local E1_TALKS, E2_TALKS = 15, 20
local MENU_SIZE, STORY_SLOTS = 3, 2
local GRIS_SCENES = 6
local EX, EXEX = 3, 4             -- Oni and Edit: a song without one is played on the other

local S = {
    talks = ".vault_talks", offerDay = ".vault_talk_offer_day",
    dry = ".vault_talk_dry",                  -- chests opened since the last offer
    heard = ".vault_talk_", heardDay = ".vault_talk_day_", last = ".vault_talk_last",
    met = ".vault_caspian_met",
    casNumber = ".vault_caspian_number", casCallDay = ".vault_caspian_call_day",
    grisNumber = ".vault_gris_number", grisCalled = ".vault_gris_called",
    song = ".vault_gris_song_state",          -- 0 never offered, 1 declined or missing, 2 failed, 3 beaten
    grisTalks = ".vault_gris_talks", likes = ".vault_gris_likes",
    gift = ".vault_gris_gift", challengeSong = ".vault_gris_challenge_song",
    challenge = ".vault_gris_challenge", tried = ".vault_gris_challenge_tried",
    trust = ".vault_gris_trust",              -- GP6 done: she trusts you (she stays in the vault)
    playDiff = ".vault_play_difficulty",      -- the last difficulty chosen for a song, + 1 (0: none yet)
}
M.KEYS = S

-- ── save ────────────────────────────────────────────────────────────────────────────────────────

M.slot = 0                        -- the save the talks read and write (My Room's phone sets its room's)
local function sf() return GetSaveFile(M.slot) end
local function trig(k) local s = sf(); return s ~= nil and s:GetGlobalTrigger(k) == true end
local function setTrig(k, v) local s = sf(); if s then s:SetGlobalTrigger(k, v ~= false) end end
local function cnt(k)
    local s = sf()
    if s == nil then return 0 end
    return math.floor((tonumber(s:GetGlobalCounter(k)) or 0) + 0.5)
end
local function setCnt(k, n) local s = sf(); if s then s:SetGlobalCounter(k, n) end end
local function str(k)
    local s = sf()
    if s == nil then return "" end
    local ok, v = pcall(function() return s:GetGlobalString(k) end)
    return (ok and type(v) == "string") and v or ""
end
local function setStr(k, v) local s = sf(); if s then pcall(function() s:SetGlobalString(k, v) end) end end

function M.today() return Daily.freezeKey() end              -- the shop's day (Japan time), as yyyymmdd
function M.evenDay() return (M.today() % 100) % 2 == 0 end
function M.talks() return cnt(S.talks) end
function M.heard(id) return trig(S.heard .. id) end
function M.grisTalks() return cnt(S.grisTalks) end
function M.songState() return cnt(S.song) end

-- ── text ────────────────────────────────────────────────────────────────────────────────────────

local function sync() if I18N.code ~= I18N.currentCode() then I18N.detect() end end

local function playerName()
    local ok, n = pcall(function() return sf().Name end)
    return (ok and type(n) == "string" and n ~= "") and n or "..."
end

-- the line of key in the game's language, the player's name in it
function M.text(key)
    if key == nil then return "" end
    sync()
    local s = T:get(key) or key
    if s:find("{player}", 1, true) then
        s = s:gsub("{player}", (playerName():gsub("%%", "%%%%")))
    end
    return s
end

function M.name(who)
    if who == "C" then return M.text("name_caspian") end
    if who == "G" then return M.text("name_gris") end
    if who == "U" then return M.text("name_unknown") end
    if who == "O" then return M.text("name_unknown_man") end
    return ""
end

-- ── lines ───────────────────────────────────────────────────────────────────────────────────────

local function append(dst, src) for _, it in ipairs(src) do dst[#dst + 1] = it end; return dst end
local function pickOne(list) return list[math.random(1, #list)] end

-- a group's lines: its only alternative, or one of its alternatives at random
local function group(id, g, alt)
    local gr = Lines[id].g[g]
    local alts = gr.alts
    return append({}, alt and alts[alt] or (#alts == 1 and alts[1] or pickOne(alts)))
end
M.group = group

-- the option keys of a node's n-th choice (in its first group)
local function choiceKeys(id, n)
    local seen = 0
    local function walk(items)
        for _, it in ipairs(items) do
            if it[1] == "c" then
                seen = seen + 1
                if seen == n then
                    local keys = {}
                    for _, o in ipairs(it[2]) do keys[#keys + 1] = o.k end
                    return keys
                end
            end
        end
    end
    for _, gr in ipairs(Lines[id].g) do
        for _, alt in ipairs(gr.alts) do
            local k = walk(alt)
            if k then return k end
        end
    end
    return {}
end

local Runner = {}
Runner.__index = Runner

function M.runner(items)
    return setmetatable({ stack = { { items = items or {}, i = 1 } }, chosen = {}, removed = {} }, Runner)
end

function Runner:step()
    local nodes = {}
    while true do
        local top = self.stack[#self.stack]
        if top == nil then
            if #nodes > 0 then return "lines", nodes end
            return "done"
        end
        local it = top.items[top.i]
        if it == nil then
            table.remove(self.stack)
        elseif it[1] == "l" then
            top.i = top.i + 1
            local n = { who = it[2], sprite = it[3], key = it[4], fade = it[5] == "fade" }
            nodes[#nodes + 1] = n
            self.last = n
        elseif it[1] == "a" then
            if #nodes > 0 then return "lines", nodes end
            top.i = top.i + 1
            return "action", it[2]
        else
            -- a choice ("c", options) or a menu ("m", { { id, label } ... }) on the line just said
            top.i = top.i + 1
            local opts = {}
            if it[1] == "c" then
                local gone = self.removed[it] or {}
                for _, o in ipairs(it[2]) do
                    if not gone[o] then opts[#opts + 1] = { label = M.text(o.k), opt = o } end
                end
            else
                for _, e in ipairs(it[2]) do opts[#opts + 1] = { label = e.label, id = e.id } end
            end
            if #nodes == 0 then
                local l = self.last or { who = "N", sprite = "", key = nil }
                nodes[1] = { who = l.who, sprite = l.sprite, key = l.key, shown = true }
            end
            nodes[#nodes].choices = opts
            self.asking = { item = it, opts = opts }
            return "lines", nodes
        end
    end
end

-- the answer to the choice the last lines ended on (1-based, in the order shown)
function Runner:answer(i)
    local a = self.asking
    self.asking = nil
    if a == nil then return end
    local c = a.opts[tonumber(i) or 1] or a.opts[1]
    if c == nil then return end
    if c.id ~= nil then
        self.pick = c.id
        return
    end
    local o = c.opt
    self.chosen[#self.chosen + 1] = o.k
    if o.again then
        self.removed[a.item] = self.removed[a.item] or {}
        self.removed[a.item][o] = true
        table.insert(self.stack, { items = { a.item }, i = 1 })
    end
    local list = o.items
    if o.alts then list = pickOne(o.alts) end
    if list and #list > 0 then table.insert(self.stack, { items = list, i = 1 }) end
end

function Runner:picked(key)
    for _, k in ipairs(self.chosen) do if k == key then return true end end
    return false
end

-- ── flows ───────────────────────────────────────────────────────────────────────────────────────

local yield = coroutine.yield

-- runs items through the stage; onAction(name) handles each action the lines stop at
local function say(items, onAction)
    local run = M.runner(items)
    while true do
        local act = yield({ "say", run })
        if act == nil then return run end
        if onAction then onAction(act) end
    end
end

function M.flow(fn, ...)
    local args = { ... }
    return coroutine.create(function() return fn(table.unpack(args)) end)
end

-- ── the vault: topics ───────────────────────────────────────────────────────────────────────────

-- id, after this many talks, parent heard, also needs this one
local STORY = {
    { "S1", 0 }, { "S2", 1, "S1" }, { "S4", 1, "S1" }, { "S3", 2, "S1" }, { "S5", 3, "S1" }, { "S8", 3, "S4" },
    { "S19", 3, "S1" }, { "S28", 4, "S19" }, { "S6a", 4, "S2" }, { "S7", 5, "S4" }, { "S9", 5, "S1" },
    { "S10", 6, "S9" }, { "X", 6, "S1" }, { "S27", 7, "S19" }, { "S6b", 7, "S6a" }, { "S11", 8, "S10" },
    { "S12", 8, "S10" }, { "S17", 9, "S3", "S11" }, { "S18", 9, "S2" }, { "S13", 11, "S11" },
    { "S6c", 12, "S6b", "S11" }, { "S16", 12, "S7", "S11" }, { "S14", 13, "S13" }, { "S26", 13, "S18" },
    { "S29", 14, "S16" }, { "S30", 15, "S29" }, { "S15", 15, "S14" }, { "S25", 16, "S12" },
}
local IS_STORY = {}
for _, s in ipairs(STORY) do IS_STORY[s[1]] = true end
-- never on the day its parent was heard
local NOT_SAME_DAY = { S14 = "S13", S30 = "S29" }
local DAY_KEPT = { S13 = true, S29 = true }

-- id, from this many talks, needs this one heard, until this many talks
local RANDOM = {
    { "R1", 0 }, { "R6", 0, nil, 12 }, { "R10", 2 }, { "R4", 3, "S2" }, { "R5", 3, "S2" }, { "R3", 0, "S8" },
    { "R8", 4 }, { "R2", 0, "S12" }, { "R7", 0, "S6c" }, { "R9", 0, "X" },
}
-- variants that wait for a topic
local VARIANT_NEEDS = { R10 = { [4] = "S11" }, R5 = { [3] = "S10" }, R3 = { [4] = "X" }, R2 = { [4] = "S11" } }
local lastVariant = {}

local function storyOpen(s, talks, spot, today)
    local id = s[1]
    if M.heard(id) or talks < s[2] then return false end
    if s[3] and not M.heard(s[3]) then return false end
    if s[4] and not M.heard(s[4]) then return false end
    if id == "X" and spot == nil then return false end
    local p = NOT_SAME_DAY[id]
    if p and cnt(S.heardDay .. p) == today then return false end
    return true
end

local function randomOpen(r, talks)
    if talks < r[2] then return false end
    if r[3] and not M.heard(r[3]) then return false end
    if r[4] and talks >= r[4] then return false end
    return true
end

local function shuffle(t)
    for i = #t, 2, -1 do
        local j = math.random(1, i)
        t[i], t[j] = t[j], t[i]
    end
    return t
end

function M.menuLabel(id)
    if id == "GRIS" then return M.text("menu_gris") end
    if id == "SKIP" then return M.text("menu_skip") end
    if id == "BYE" then return M.text("menu_bye") end
    return M.text(id .. "_menu")
end

-- where Gris is this visit: "window", "tank" or nil (rolled once per visit)
function M.rollSpot()
    if M.talks() < GRIS_FROM then return nil end
    if M.evenDay() then return "window" end
    if math.random() < TANK_CHANCE then return "tank" end
    return nil
end

-- "I want to talk to Gris." is on the menu: whenever she watches from the window and has a scene left
function M.grisReady(spot)
    return spot == "window" and cnt(S.song) == 3 and trig(S.grisNumber) and not trig(S.trust)
        and M.grisTalks() < GRIS_SCENES
end

-- the topics on the menu, then the skip
function M.menu(spot)
    local talks, today = M.talks(), M.today()
    local stories, rands = {}, {}
    for _, s in ipairs(STORY) do
        if storyOpen(s, talks, spot, today) then
            -- X only opens while Gris is in sight: it comes first then
            if s[1] == "X" then table.insert(stories, 1, "X") else stories[#stories + 1] = s[1] end
        end
    end
    local last = str(S.last)
    for _, r in ipairs(RANDOM) do
        if randomOpen(r, talks) and r[1] ~= last then rands[#rands + 1] = r[1] end
    end
    shuffle(rands)
    local picks = {}
    for i = 1, math.min(STORY_SLOTS, #stories) do picks[#picks + 1] = stories[i] end
    for _, r in ipairs(rands) do
        if #picks >= MENU_SIZE then break end
        picks[#picks + 1] = r
    end
    local i = STORY_SLOTS + 1
    while #picks < MENU_SIZE and stories[i] do picks[#picks + 1] = stories[i]; i = i + 1 end
    if M.grisReady(spot) then
        if #picks >= MENU_SIZE then picks[#picks] = "GRIS" else picks[#picks + 1] = "GRIS" end
    end
    local out = {}
    for _, id in ipairs(picks) do out[#out + 1] = { id = id, label = M.menuLabel(id) } end
    out[#out + 1] = { id = "SKIP", label = M.menuLabel("SKIP") }
    return out
end

-- R9's lines follow Gris's progress
local function r9Items()
    if trig(S.trust) then return group("R9", 7) end
    local n = M.grisTalks()
    if n >= 3 then return group("R9", 6) end
    if n >= 1 then return group("R9", 5) end
    local st = cnt(S.song)
    if st == 3 then
        local items = group("R9", 4)
        if not trig(S.grisNumber) then table.remove(items) end      -- her window days come with E2
        return items
    end
    if st == 1 or st == 2 then return group("R9", 3) end
    if trig(S.grisNumber) and not trig(S.grisCalled) then return group("R9", 2) end
    return group("R9", 1)
end

local function cp2Items()
    if not M.heard("CP2") then return group("CP2", 1) end
    local alts = Lines.CP2.g[2].alts
    local ok = { alts[3] }
    if M.heard("X") then ok[#ok + 1] = alts[1] end
    if M.grisTalks() >= 3 then ok[#ok + 1] = alts[2] end
    return append({}, pickOne(ok))
end

-- a random topic's variant: one of those open, not the one heard last time
local function variantItems(id)
    if id == "R9" then return r9Items() end
    if id == "CP2" then return cp2Items() end
    local gs = Lines[id].g
    local needs = VARIANT_NEEDS[id] or {}
    local open = {}
    for i = 1, #gs do
        if (needs[i] == nil or M.heard(needs[i])) and i ~= lastVariant[id] then open[#open + 1] = i end
    end
    if #open == 0 then open = { 1 } end
    local g = pickOne(open)
    lastVariant[id] = g
    return group(id, g)
end

-- a topic's lines
function M.topic(id)
    if id:match("^R%d") or id == "CP1" or id == "CP2" then return variantItems(id) end
    return group(id, 1)
end

-- a topic heard over coffee
local function topicDone(id)
    setCnt(S.talks, M.talks() + 1)
    if IS_STORY[id] then
        local today = M.today()
        setTrig(S.heard .. id, true)
        if DAY_KEPT[id] then setCnt(S.heardDay .. id, today) end
    else
        setStr(S.last, id)
    end
end
M.topicDone = topicDone

-- an event due after a topic: "E1", "E2" or nil
function M.event(spot)
    local talks = M.talks()
    if not trig(S.casNumber) and talks >= E1_TALKS and M.heard("S15") and M.heard("X") then return "E1" end
    if trig(S.casNumber) and not trig(S.grisNumber) and M.heard("CP3") and talks >= E2_TALKS and spot ~= nil then
        return "E2"
    end
    return nil
end

local function eventItems(id)
    if id == "E1" then
        setTrig(S.casNumber, true)
        return group("E1", 1)
    end
    local g = (cnt(S.song) == 3) and 3 or (trig(S.grisCalled) and 2 or 1)
    setTrig(S.grisNumber, true)
    return group("E2", g)
end

-- after a chest that opened (not a snapped key; chest 1 simple, 2 gold, 3 optk): whether Mr.Caspian offers a talk
function M.wantOffer(chest)
    local today = M.today()
    local dry = cnt(S.dry) + 1
    local want
    if cnt(S.offerDay) ~= today then
        setCnt(S.offerDay, today)
        want = true
    else
        want = dry >= OFFER_PITY or math.random() < (OFFER_CHANCE[chest] or OFFER_CHANCE[1])
    end
    setCnt(S.dry, want and 0 or dry)
    return want
end

local function opener()
    local t = M.talks()
    local g = t == 0 and 1 or (t <= 4 and 2 or (t <= 11 and 3 or 4))
    local alts = Lines.OPEN.g[g].alts
    if g == 4 and not M.heard("S11") then alts = { alts[1], alts[2] } end
    return append({}, pickOne(alts))
end

-- ── the vault: Gris's scenes ────────────────────────────────────────────────────────────────────

-- her character (M.GRIS_FOLDER), when it is installed
function M.grisCharacter()
    local ok, e = pcall(function() return CHARACTERLIST:GetByName(M.GRIS_FOLDER) end)
    return ok and e or nil
end

function M.grisOwned()
    local e = M.grisCharacter()
    if e == nil then return false end
    local ok, v = pcall(function() return sf():IsCharacterUnlocked(e.FolderName) end)
    return ok and v == true
end

-- GP6 was played while her character was not installed: it is unlocked once it is
function M.applyTrust()
    if not trig(S.trust) then return end
    local e = M.grisCharacter()
    if e ~= nil and not M.grisOwned() then pcall(function() sf():UnlockCharacter(e.FolderName) end) end
end

local function giveGift()
    local likes = cnt(S.likes)
    local id = M.GIFTS[likes] or M.GIFTS[#M.GIFTS]
    local s = sf()
    if s ~= nil and not trig(S.gift) then
        s:SetGlobalCounter(".myroom_" .. id, cnt(".myroom_" .. id) + 1)
        s:SetGlobalCounter(".myroom_held_" .. id, cnt(".myroom_held_" .. id) + 1)
        setTrig(S.gift, true)
    end
    return { type = "furniture", id = id, rarity = M.GIFT_RARITY }
end

local function unlockGris()
    local e = M.grisCharacter()
    if e ~= nil and not M.grisOwned() then
        pcall(function() sf():UnlockCharacter(e.FolderName) end)
        return { type = "character", entry = e }
    end
    return nil
end

-- the next scene: GP1 .. GP6 (GP4 again until its song is cleared)
function M.grisScene()
    local n = M.grisTalks()
    if n >= GRIS_SCENES then return nil end
    return "GP" .. (n + 1)
end

-- ── songs ───────────────────────────────────────────────────────────────────────────────────────

local function songsReady()
    local ok, v = pcall(function() return IsSongsEnumDone() end)
    return not ok or v ~= false
end

-- a song by its unique id, locked and hidden ones included (Gris's song stays hidden until it is unlocked)
function M.findSong(uid)
    if not songsReady() then return nil end
    local node = nil
    pcall(function()
        local s = GenerateSongListSettings()
        s.ExcludeHiddenSongs = false
        s.IgnoreUnlockables = true
        local list = RequestSongList(s)
        node = list and list:GetSongByUniqueId(uid) or nil
    end)
    return node
end

local function dashySongs()
    local out = {}
    if not songsReady() then return out end
    pcall(function()
        local s = GenerateSongListSettings()
        s.AppendMainRandomBox = false
        s.AppendSubRandomBoxes = false
        s.SubBackBoxFrequency = 0
        s.IgnoreUnlockables = true
        s.RootGenreFolder = M.DASHY_GENRE
        local list = RequestSongList(s)
        local found = list:SearchSongsByPredicate(function(n) return true end)
        local e = found:GetEnumerator()
        while e:MoveNext() do
            local n = e.Current
            if n.Genre == M.DASHY_GENRE then out[#out + 1] = n end
        end
    end)
    return out
end

-- the difficulty the player plays: the one last chosen for a song (kept in the save for the next session);
-- Oni before any was chosen, or when the last choice was a tower or a dan
local function chosenDifficulty()
    local d = nil
    pcall(function()
        if SONGMOUNT:ChosenUniqueId() ~= "" then d = SONGMOUNT:ChosenDifficulty() end
    end)
    if d ~= nil and d >= 0 and d <= EXEX then
        setCnt(S.playDiff, d + 1)
        return d
    end
    local saved = cnt(S.playDiff)
    if saved >= 1 then return saved - 1 end
    return EX
end

-- the chart a song is played on: the chosen difficulty, or Edit for Oni and Oni for Edit; nil when it has neither,
-- and the song then counts as missing
function M.playableDiff(node, d)
    if node == nil then return nil end
    d = d or chosenDifficulty()
    local function has(x)
        local ok, ch = pcall(function() return node:GetChart(x) end)
        return ok and ch ~= nil
    end
    if has(d) then return d end
    if d == EX and has(EXEX) then return EXEX end
    if d == EXEX and has(EX) then return EX end
    return nil
end

-- GP4's song: the one locked in the save, or a new pick when it is not installed or has no chart to play (nil: none)
function M.challengeSong()
    local d = chosenDifficulty()
    local uid = str(S.challengeSong)
    if uid ~= "" then
        local n = M.findSong(uid)
        if M.playableDiff(n, d) then return n end
    end
    local pool = {}
    for _, n in ipairs(dashySongs()) do
        if M.playableDiff(n, d) then pool[#pool + 1] = n end
    end
    if #pool == 0 then return nil end
    local n = pickOne(pool)
    setStr(S.challengeSong, n.UniqueId)
    return n
end

-- mounts the song on its playable difficulty for every player; false when it cannot be played
function M.mount(node)
    local d = M.playableDiff(node)
    if d == nil then return false end
    local ok, r = pcall(function() return node:Mount(d, d, d, d, d) end)
    return ok and r ~= false
end

-- right after a play: whether it was cleared
function M.cleared()
    local ok, r = pcall(function()
        if PLAYSTATE:WasPlayAborted() then return false end
        return PLAYSTATE:IsClear(0) == true
    end)
    return ok and r == true
end

-- ── the flows ───────────────────────────────────────────────────────────────────────────────────

local function grisFlow(spot)
    local id = M.grisScene()
    if id == nil then return end
    if id ~= "GP1" then yield({ "stage", "come" }) end
    local left = false              -- she went to lie down: gone from the room for the rest of the visit
    local function act(a)
        if a == "come" then yield({ "stage", "come" })
        elseif a == "leave" then
            left = true
            yield({ "stage", "leave" })
        elseif a == "gift" then yield({ "reward", giveGift() })
        elseif a == "unlock_gris" then
            local r = unlockGris()
            if r then yield({ "reward", r }) end
        end
    end
    if id == "GP1" then
        local run = say(group("GP1", 1), act)
        for i, k in ipairs(choiceKeys("GP1", 3)) do
            if run:picked(k) then setCnt(S.likes, i) end
        end
        if cnt(S.likes) == 0 then setCnt(S.likes, #M.GIFTS) end
        setCnt(S.grisTalks, 1)
    elseif id == "GP2" or id == "GP5" then
        say(group(id, 1), act)
        setCnt(S.grisTalks, id == "GP2" and 2 or 5)
    elseif id == "GP3" then
        local likes = math.max(1, math.min(#M.GIFTS, cnt(S.likes)))
        local items = append(group("GP3", 1), group("GP3", 1 + likes))
        say(append(items, group("GP3", 6)), act)
        setCnt(S.grisTalks, 3)
    elseif id == "GP4" then
        local items = group("GP4", trig(S.tried) and 2 or 1)
        local node = M.challengeSong()
        if node == nil then
            say(append(items, group("GP4", 4)), act)
            setTrig(S.tried, true)
        else
            local played, clear = false, false
            say(append(items, group("GP4", 3)), function(a)
                if a == "play" then
                    played = true
                    clear = yield({ "play", node }) == true
                else act(a) end
            end)
            if played then
                if clear then
                    say(group("GP4", 5), act)
                    setTrig(S.challenge, true)
                    setCnt(S.grisTalks, 4)
                else
                    say(group("GP4", 6), act)
                    setTrig(S.tried, true)
                end
            end
        end
    elseif id == "GP6" then
        -- her trust; her character comes with it, or she feels she has known you long before
        say(append(group("GP6", 1), group("GP6", M.grisOwned() and 3 or 2)), act)
        setTrig(S.trust, true)
        setCnt(S.grisTalks, GRIS_SCENES)
    end
    if not left then yield({ "stage", "back" }) end
end

-- the talk over coffee after a chest; spot: where Gris is this visit
function M.vaultTalk(spot)
    local offer = append(group("OFFER", 1), group("OFFER", 2))
    local run = say(offer)
    if not run:picked(choiceKeys("OFFER", 1)[1]) then return end
    yield({ "stage", "in" })
    local menu = M.menu(spot)
    run = say(append(opener(), { { "m", menu } }))
    local id = run.pick or "SKIP"
    if id == "SKIP" then
        say(group("CLOSE", 2))
        yield({ "stage", "out" })
        return
    end
    if id == "GRIS" then
        grisFlow(spot)
    else
        say(M.topic(id))
        topicDone(id)
        local ev = M.event(spot)
        if ev then say(eventItems(ev)) end
    end
    say(group("CLOSE", 1))
    yield({ "stage", "out" })
end

-- ── the phone (My Room) ─────────────────────────────────────────────────────────────────────────

-- the cafe behind Mr.Caspian on the phone: it comes in with his first line, then these lines set its level
M.CAFE = { CP1_06 = 0.4, CP3_02 = 0.3, CALL_05 = 0, CALL_10 = 0, CALLEARLY_05 = 0, CALLEARLY_10 = 0 }

-- id, kind ("story" once, "random" again), the check that opens it
local CALL_TOPICS = {
    { "CP1", "random", function() return true end },
    { "CP2", "random", function() return M.heard("S11") end },
    { "CP3", "story", function() return M.heard("X") end },
    { "CP4", "story", function(today) return M.heard("CP3") and cnt(S.heardDay .. "CP3") ~= today end },
    { "CP5", "story", function() return cnt(S.song) == 3 end },
}

local function caspianCall()
    if not trig(S.casNumber) then
        say(group("CALLEARLY", trig(S.met) and 2 or 1))
        return
    end
    local today = M.today()
    if cnt(S.casCallDay) == today then
        say(group("CALL", 3))
        return
    end
    setCnt(S.casCallDay, today)
    local items = group("CALL", 1)
    -- the first call after GP6: he brings up her trust himself, before the menu
    if trig(S.trust) and not M.heard("CP6") then
        append(items, group("CP6", 1))
        setTrig(S.heard .. "CP6", true)
    end
    -- story topics first, so an open one is never pushed off the menu by the random ones
    local menu = {}
    for _, kind in ipairs({ "story", "random" }) do
        for _, t in ipairs(CALL_TOPICS) do
            local id = t[1]
            local open = t[2] == kind and t[3](today)
            if kind == "story" then open = open and not M.heard(id) end
            if open and #menu < MENU_SIZE then menu[#menu + 1] = { id = id, label = M.menuLabel(id) } end
        end
    end
    menu[#menu + 1] = { id = "BYE", label = M.menuLabel("BYE") }
    local run = say(append(items, { { "m", menu } }))
    local id = run.pick
    if id == nil or id == "BYE" then
        say(group("CALL", 2))
        return
    end
    -- one topic a day: then he gets back to work and hangs up
    local lines = M.topic(id)
    if not M.heard(id) then
        setTrig(S.heard .. id, true)
        if id == "CP3" then setCnt(S.heardDay .. id, today) end
    end
    say(append(append(lines, group("CALL", 4)), group("CALL", 5)))
end

-- the numbers the player has, for the phone's contacts: Mr.Caspian's once he gave it, Gris's once you have it
-- (from him, or dialed yourself) until her song is yours
M.CASPIAN_DISPLAY = "75EA5"
function M.contacts()
    local out = {}
    if trig(S.casNumber) then out[#out + 1] = { name = M.name("C"), number = M.CASPIAN_DISPLAY } end
    if (trig(S.grisNumber) or trig(S.grisCalled)) and cnt(S.song) ~= 3 then
        -- her name: from Mr.Caspian, or from her song once it was played
        local known = trig(S.grisNumber) or cnt(S.song) == 2
        out[#out + 1] = { name = M.name(known and "G" or "U"), number = M.GRIS_NUMBER }
    end
    return out
end

-- the unknown voice's lines with her name shown (her song told it)
local function named(items)
    local out = {}
    for i, it in ipairs(items) do
        if it[1] == "l" and it[2] == "U" then
            out[i] = { "l", "G", it[3], it[4], it[5] }
        elseif it[1] == "c" then
            local opts = {}
            for j, o in ipairs(it[2]) do
                local no = { k = o.k, again = o.again, items = o.items and named(o.items) }
                if o.alts then
                    no.alts = {}
                    for a, alt in ipairs(o.alts) do no.alts[a] = named(alt) end
                end
                opts[j] = no
            end
            out[i] = { "c", opts }
        else
            out[i] = it
        end
    end
    return out
end

local function grisCall()
    local st = cnt(S.song)
    local known = trig(S.grisNumber)          -- Mr.Caspian gave you her number: she knows who calls
    local again = trig(S.grisCalled)
    setTrig(S.grisCalled, true)
    local result = nil          -- "missing", true (cleared) or false (failed)
    local node = M.findSong(M.GRIS_SONG)
    if not M.playableDiff(node) then node = nil end      -- no chart for the chosen difficulty: as if missing
    local function onPlay(a)
        if a ~= "play" then return end
        if node == nil then result = "missing"; return end
        result = yield({ "play", node }) == true
    end
    if known and again then
        local items = group("GPhAgain", 1)
        if st == 1 then append(items, group("GPhAgain", 2)) elseif st == 2 then append(items, group("GPhAgain", 3)) end
        say(append(items, group("GPhOffer", 1)), onPlay)
        if result == "missing" then say(group("GPhMissing", 1)) end
    elseif known then
        -- the artist's script: one answer, which hangs up when her song is missing
        say(append(group("GPhC", 1), group("GPhC", node and 2 or 3)), onPlay)
        if node == nil then result = "missing" end
    else
        -- the artist's narration: one answer, which hangs up when her song is missing
        local id = again and "GPhA2" or "GPhA"
        local items = again and group(id, st == 2 and 2 or 1) or group(id, 1)
        items = append(items, group(id, node and (again and 3 or 2) or (again and 4 or 3)))
        say(st == 2 and named(items) or items, onPlay)
        if node == nil then result = "missing" end
    end
    -- the result: GPh-C's cleared and failed lines with his number, GPh-AFTER's ("????") without
    if result == "missing" then
        setCnt(S.song, 1)
    elseif result == true then
        say(known and group("GPhC", 4) or group("GPhAfter", 1), function(a)
            if a == "unlock_song" then
                pcall(function() sf():UnlockSong(M.GRIS_SONG) end)
                setCnt(S.song, 3)
                yield({ "reward", { type = "song", node = node } })
            end
        end)
        setCnt(S.song, 3)
    elseif result == false then
        say(known and group("GPhC", 5) or group("GPhAfter", 2))
        setCnt(S.song, 2)
    elseif st == 0 then
        setCnt(S.song, 1)          -- "Not right now."
    end
end

-- the call of a dialed number: a flow, or nil (Gris no longer answers once her song is hers)
function M.call(number)
    local key = tostring(number or ""):gsub("%s+", ""):lower()
    if key == M.CASPIAN_NUMBER then return M.flow(caspianCall) end
    if key == M.GRIS_NUMBER then
        if cnt(S.song) == 3 then return nil end
        return M.flow(grisCall)
    end
    return nil
end

return M
