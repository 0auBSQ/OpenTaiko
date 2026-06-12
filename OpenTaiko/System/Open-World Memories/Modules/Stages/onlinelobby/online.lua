---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- onlinelobby/online.lua - P2P rhythm-lobby logic on the NET core (kept out of Script.lua, which owns the
-- state machine + drawing). Up to 5 players: the lobby authority (NET host-role, rotates each round) picks the
-- SONG (no difficulty prompt) + speed + dynamic-beat; every player picks their own difficulty + mods + ready.
-- On "go" each machine plays the shared song as a real N-player game: spot 0 is you (real input), the others are
-- mounted onto VIRTUAL SLOTS (character + nameplate) and auto-play their own chart with each note's judge sampled
-- from that player's broadcast good/ok/bad rates (CStage演奏画面共通.AlterJudgement) - flying notes hidden, score
-- snapped from the wire (OnlinePlaySync). Loading + finish are barrier-synced; results sync + host rotates.

local LO = {}

LO.net = {
    online = false, isHost = false, connecting = false, roomGone = false, code = nil, msg = nil,
    nameByPeer = {}, infoByPeer = {}, diffByPeer = {}, modByPeer = {}, readyByPeer = {}, resultByPeer = {},
    lackByPeer = {}, watchByPeer = {},
    song = nil,                       -- {uid, title, diff, speed, dyn(0/1)}
    songLevels = {}, iLackSong = false,
    goSignal = false, songChanged = false,
    resultsT = 0, resultsReadyT = nil,
}
local net = LO.net
local floor = math.floor
LO.MAXP = 5
LO.DIFF_NAMES = { [0] = "Easy", [1] = "Normal", [2] = "Hard", [3] = "Oni", [4] = "Edit" }
local function defMods() return { r = 0, st = 0, ju = 0, tz = 2, ss = 9 } end   -- none / normal timing / x1 scroll

-- ── identity ────────────────────────────────────────────────────────────────────────────────────
local function jstr(s)
    return '"' .. tostring(s):gsub('[%c\\"]', function(ch)
        local m = { ['"'] = '\\"', ['\\'] = '\\\\', ['\n'] = '\\n', ['\r'] = '\\r', ['\t'] = '\\t' }
        return m[ch] or string.format('\\u%04x', string.byte(ch))
    end) .. '"'
end
function LO.myName() local sf = GetSaveFile(0); local n = sf and sf.Name; if n == nil or n == "" then n = "Player" end return n end
-- full join info so the host can mount each remote player onto a virtual slot (character + puchi + nameplate)
function LO.selfInfo()
    local sf = GetSaveFile(0)
    local char, puchi, title, dan = "None", "None", "", ""
    local npid, dgold, dtype = -1, false, 0
    pcall(function() char = sf.CharacterName or "None" end)
    pcall(function() local p = sf:GetPuchichara(); if p then puchi = p.FolderName or "None" end end)
    pcall(function() local np = sf.NameplateInfo; if np then title = np.Title or ""; npid = np.Id or -1 end end)
    pcall(function() dan = sf.SelectedDan or "" end)
    pcall(function() local dp = sf.DanplateInfo; if dp then dgold = dp.Gold and true or false; dtype = dp.ClearStatus or 0 end end)
    -- npid lets the receiver resolve nameplate type+rarity+text from its (identical) catalogue; dgold/dtype style the dan plate
    -- pal carries the character palette gradient (stops+blend) so remote V-slots render in the right colours.
    return string.format('{"name":%s,"char":%s,"puchi":%s,"title":%s,"dan":%s,"npid":%d,"dgold":%s,"dtype":%d,"pal":%s}',
        jstr(LO.myName()), jstr(char), jstr(puchi), jstr(title), jstr(dan), npid, tostring(dgold), dtype, LO.selfPaletteJson())
end
-- the local player's selected character palette, as a self-contained JSON object {"b":blend,"s":[[pos,r,g,b(,a)],...]}
-- (or "null"). Read from the char's own Palettes.json by the persisted selection index, so the receiver can apply
-- it to the matching V-slot without depending on its own copy of that character's palette file.
function LO.selfPaletteJson()
    local out = "null"
    pcall(function()
        local sf = GetSaveFile(0); local cha = sf:GetCharacter()
        if not (cha and cha.IsValid) then return end
        local idx = floor(sf:GetGlobalCounter(".character_palette_" .. (sf.CharacterName or "")) or 0)
        local raw = JSONLOADER:JsonParseFileAny(cha.FullPath .. "/Palettes.json")
        if not (raw and raw.Count and idx >= 0 and idx < raw.Count) then return end
        local e = raw[idx + 1]; if not e then return end
        local rawStops = JSONLOADER:JsonGet(e, "stops")
        if not (rawStops and rawStops.Count and rawStops.Count >= 2) then return end   -- no stops = Default (no gradient)
        local blend = tonumber(tostring(JSONLOADER:JsonGet(e, "blend") or 0)) or 0
        local parts = {}
        for j = 1, rawStops.Count do
            local s = rawStops[j]; local a = JSONLOADER:JsonGet(s, 5)
            parts[j] = a ~= nil and string.format("[%s,%s,%s,%s,%s]", s[1], s[2], s[3], s[4], a)
                or string.format("[%s,%s,%s,%s]", s[1], s[2], s[3], s[4])
        end
        out = string.format('{"b":%s,"s":[%s]}', blend, table.concat(parts, ","))
    end)
    return out
end
function LO.amController() return net.online and NET:HasHostRole() end
function LO.count() local c = 0; for _ in pairs(net.nameByPeer) do c = c + 1 end return c end

-- ── song list (lazy) ──────────────────────────────────────────────────────────────────────────────
local songList, songTried = nil, false
local function ensureSongList()
    if songList or songTried then return end
    songTried = true
    local ok, sl = pcall(function() return RequestSongList(GenerateSongListSettings()) end)
    if ok and sl then songList = sl end
end
local function findSong(uid)
    ensureSongList()
    if not songList or not uid or uid == "" then return nil end
    local ok, node = pcall(function() return songList:GetSongByUniqueId(uid) end)
    if ok and node and node ~= false then return node end
    return nil
end
function LO.haveSong(uid) return findSong(uid) ~= nil end
function LO.resolveTitle(uid)
    local node = findSong(uid); if not node then return nil end
    local ok, t = pcall(function() return node.Title end); return ok and t or nil
end
function LO.stopPreview() pcall(function() SHARED:SetSharedPreview("presound", "Sounds/empty.ogg") end) end

-- load the chosen song's jacket (SHARED "preimage"), start its preview, and read its difficulty levels.
-- Sets net.iLackSong when this client doesn't have the song. Safe to call on host + guests.
local function setBorder(t) pcall(function() t:SetWrapMode("Border") end) end
local function placeholderPreimage() pcall(function() SHARED:SetSharedTexture("preimage", "Textures/preimage.png", setBorder) end) end
local function loadSongMedia()
    net.songLevels = {}; net.iLackSong = true; net.songSubtitle = nil
    net.previewLoaded = false; net.previewDemoStart = 0; net.previewSpeed = 1.0; net.previewCool = 0
    LO.stopPreview()
    if not net.song then placeholderPreimage(); return end
    local node = findSong(net.song.uid)
    if not node then net.iLackSong = true; placeholderPreimage(); return end
    net.iLackSong = false
    pcall(function() net.songSubtitle = node.Subtitle end)
    -- jacket via the shared "preimage" texture. We do NOT ClearSharedTexture first: Clear bumps the resource's
    -- async version AFTER SetShared... captured it, so the loaded jacket was discarded as stale (showed once,
    -- never updated). SetShared... already swaps atomically; set the border wrap on the actual loaded texture.
    pcall(function()
        if node.HasPreimage then SHARED:SetSharedTextureUsingAbsolutePath("preimage", node.PreimagePath, setBorder)
        else placeholderPreimage() end
    end)
    for d = 0, 4 do pcall(function() if node.score[d] ~= nil then net.songLevels[d] = node.nLevel[d] end end) end
    pcall(function()
        local spd = (net.song.speed or 20) / 20
        net.previewSpeed = spd
        net.previewDemoStart = floor((node.DemoStart or 0) / spd)
        SHARED:SetSharedPreviewUsingAbsolutePath("presound", node.AudioPath, function(snd)
            snd:SetSpeed(spd); snd:Play(); snd:SetTimestamp(net.previewDemoStart)
            net.previewLoaded = true
        end)
    end)
end
-- loop the lobby preview (call each frame in lobby/results; song select owns its own preview). Replays from the
-- demo start when the preview finishes, with a short cooldown to avoid double-seeking the same restart.
function LO.tickPreview(dt)
    if net.iLackSong or not net.song or not net.previewLoaded then return end
    if (net.previewCool or 0) > 0 then net.previewCool = net.previewCool - dt; return end
    pcall(function()
        local snd = SHARED:GetSharedSound("presound")
        if snd and snd.Loaded and not snd.IsPlaying then
            snd:SetSpeed(net.previewSpeed or 1.0); snd:Play(); snd:SetTimestamp(net.previewDemoStart or 0)
            net.previewCool = 0.5
        end
    end)
end
LO.loadSongMedia = loadSongMedia

-- parse a received "pal" object {b, s:[[pos,r,g,b(,a)],...]} back into (luaStops, blend) for SetPaletteGradient.
local function parsePalette(io)
    if not io then return nil, 0 end
    local pal = JSONLOADER:JsonGet(io, "pal"); if not pal then return nil, 0 end
    local sArr = JSONLOADER:JsonGet(pal, "s")
    if not (sArr and sArr.Count and sArr.Count >= 2) then return nil, 0 end
    local blend = tonumber(tostring(JSONLOADER:JsonGet(pal, "b") or 0)) or 0
    local stops = {}
    for j = 1, sArr.Count do
        local s = sArr[j]; local a = JSONLOADER:JsonGet(s, 5)
        stops[j] = { s[1], s[2], s[3], s[4] }
        if a ~= nil then stops[j][5] = a end
    end
    return stops, blend
end

-- ── roster ──────────────────────────────────────────────────────────────────────────────────────
function LO.refreshRoster()
    local names = {}
    local arr = JSONLOADER:JsonParseStringAny(NET:PeersJson()); local n = arr and JSONLOADER:JsonCount(arr) or 0
    for i = 1, n do
        local e = JSONLOADER:JsonGet(arr, i); local id = e and JSONLOADER:JsonGet(e, "id")
        if id then
            id = floor(id)
            local io = e and JSONLOADER:JsonGet(e, "info"); io = io and JSONLOADER:JsonParseStringAny(io) or nil
            names[id] = (io and JSONLOADER:JsonGet(io, "name")) or ("Player " .. id)
            local palStops, palBlend = parsePalette(io)
            net.infoByPeer[id] = { name = names[id],
                char  = (io and JSONLOADER:JsonGet(io, "char"))  or "None",
                puchi = (io and JSONLOADER:JsonGet(io, "puchi")) or "None",
                title = (io and JSONLOADER:JsonGet(io, "title")) or "",
                dan   = (io and JSONLOADER:JsonGet(io, "dan"))   or "",
                npid  = floor((io and JSONLOADER:JsonGet(io, "npid")) or -1),
                dgold = (io and JSONLOADER:JsonGet(io, "dgold")) and true or false,
                dtype = floor((io and JSONLOADER:JsonGet(io, "dtype")) or 0),
                palStops = palStops, palBlend = palBlend }
            if net.diffByPeer[id] == nil then net.diffByPeer[id] = 1 end
            if net.modByPeer[id] == nil then net.modByPeer[id] = defMods() end
            if net.readyByPeer[id] == nil then net.readyByPeer[id] = false end
        end
    end
    net.nameByPeer = names
    for id in pairs(net.diffByPeer) do
        if not names[id] then
            net.diffByPeer[id] = nil; net.modByPeer[id] = nil; net.readyByPeer[id] = nil
            net.resultByPeer[id] = nil; net.infoByPeer[id] = nil; net.lackByPeer[id] = nil; net.watchByPeer[id] = nil
        end
    end
end
function LO.peerIds() local ids = {} for id in pairs(net.nameByPeer) do ids[#ids + 1] = id end table.sort(ids) return ids end

-- ── session control ───────────────────────────────────────────────────────────────────────────────
local function reset()
    net.online, net.isHost, net.connecting, net.roomGone = false, false, false, false
    net.code = nil
    net.nameByPeer, net.infoByPeer, net.diffByPeer, net.modByPeer, net.readyByPeer = {}, {}, {}, {}, {}
    net.resultByPeer, net.lackByPeer, net.watchByPeer = {}, {}, {}
    net.song = nil; net.songLevels = {}; net.iLackSong = false
    net.goSignal = false; net.songChanged = false
    net.resultsT, net.resultsReadyT = 0, nil
    LO.stopPreview()
end
local function clearLobbyAuto() pcall(function() for sp = 0, 4 do CONFIG:SetAutoStatus(sp, false) end end) end   -- no stray Auto icon online
function LO.host()
    clearLobbyAuto()
    NET:SetLocalPlayer(LO.selfInfo())
    local code = NET:CreateRoom("onlinelobby", "", LO.MAXP)
    if not code or code == "" then net.msg = "Could not open the room."; return false end
    net.code, net.online, net.isHost, net.connecting = code, true, true, false
    net.nameByPeer, net.infoByPeer, net.diffByPeer, net.modByPeer, net.readyByPeer = {}, {}, {}, {}, {}
    STORAGE:WriteLobbyCode("lobby.txt", code)
    STORAGE:RevealLobbyCodes()
    LO.refreshRoster()
    net.msg = "Room open! The code was saved to a folder - share it so friends can join."
    return true
end
function LO.join(code)
    code = (code or ""):gsub("%s", "")
    if code == "" then net.msg = "No code entered."; return false end
    local sid = NET:PeekStageId(code)
    if sid ~= "onlinelobby" then net.msg = sid and ("That code is for a '" .. sid .. "' room.") or "That code isn't valid."; return false end
    NET:SetLocalPlayer(LO.selfInfo())
    net.connecting, net.isHost = true, false
    net.nameByPeer, net.infoByPeer, net.diffByPeer, net.modByPeer, net.readyByPeer = {}, {}, {}, {}, {}
    NET:JoinRoom(code)
    net.msg = "Connecting…"
    return true
end
function LO.leave() if net.online or net.connecting then NET:Leave() end reset() end

-- ── selections ────────────────────────────────────────────────────────────────────────────────────
local function songJson(s)
    return string.format('{"u":%s,"t":%s,"d":%d,"s":%d,"b":%d}', jstr(s.uid), jstr(s.title or "?"), s.diff or 0, s.speed or 20, s.dyn or 0)
end
function LO.rebroadcastSong() if net.song then NET:Broadcast("song", songJson(net.song)) end end
-- difficulties this song actually has (falls back to all 5 if unknown / song missing)
function LO.availDiffs()
    local a = {}
    for d = 0, 4 do if net.songLevels[d] ~= nil then a[#a + 1] = d end end
    if #a == 0 then a = { 0, 1, 2, 3, 4 } end
    return a
end
function LO.cycleDiff(dir)
    local a = LO.availDiffs(); local cur = net.diffByPeer[NET:SelfId()] or 1
    local idx = 1; for i, d in ipairs(a) do if d == cur then idx = i end end
    LO.setDiff(a[((idx - 1 + dir) % #a) + 1])
end
-- apply a freshly-set/received song: clear ready, load media (jacket/preview/levels), check have, broadcast have,
-- and snap my difficulty to one the song actually offers.
local function applySong()
    for id in pairs(net.readyByPeer) do net.readyByPeer[id] = false end
    net.songChanged = true
    loadSongMedia()
    local a = LO.availDiffs(); local cur = net.diffByPeer[NET:SelfId()] or 1; local ok = false
    for _, d in ipairs(a) do if d == cur then ok = true end end
    if not ok then LO.setDiff(a[1]) end
    NET:Broadcast("have", string.format('{"h":%s}', net.iLackSong and "false" or "true"))
end
function LO.setSong(uid, title, diff, speed, dyn)
    net.song = { uid = uid, title = title, diff = diff or 0, speed = speed or 20, dyn = dyn and 1 or 0 }
    if LO.amController() then LO.rebroadcastSong() end
    applySong()
end
function LO.adjustSpeed(delta)
    if not net.song then return end
    local sp = (net.song.speed or 20) + delta; if sp < 2 then sp = 2 elseif sp > 200 then sp = 200 end
    net.song.speed = sp
    -- apply to the live host preview immediately (guests pick it up via the song rebroadcast → applySong reload)
    net.previewSpeed = sp / 20
    pcall(function() local snd = SHARED:GetSharedSound("presound"); if snd then snd:SetSpeed(sp / 20) end end)
    if LO.amController() then LO.rebroadcastSong() end
end
function LO.setDiff(d) net.diffByPeer[NET:SelfId()] = d; NET:Broadcast("diff", string.format('{"d":%d}', d)) end
function LO.setReady(r) net.readyByPeer[NET:SelfId()] = r; NET:Broadcast("ready", string.format('{"r":%s}', r and "true" or "false")) end
function LO.setWatching(w) net.watchByPeer[NET:SelfId()] = w and true or nil; NET:Broadcast("watch", string.format('{"w":%s}', w and "true" or "false")) end
-- read the player's mod_select_dialog choices (CONFIG, player 0; excludes auto + fun-mod, which are disabled
-- online), store + broadcast them so everyone can render the player's mod ICONS.
function LO.broadcastMods()
    local m = { r = CONFIG:GetRandomMod(0), st = CONFIG:GetStealthMod(0), ju = CONFIG:GetJusticeMod(0), tz = CONFIG:GetTimingZone(0), ss = CONFIG:GetScrollSpeed(0) }
    net.modByPeer[NET:SelfId()] = m
    NET:Broadcast("mod", string.format('{"r":%d,"st":%d,"ju":%d,"tz":%d,"ss":%d}', m.r or 0, m.st or 0, m.ju or 0, m.tz or 2, m.ss or 9))
end
-- push a player's mods onto a scratch CONFIG slot so MODICONS:Draw(slot,...) can render their icons
function LO.applyModsToSlot(slot, m)
    m = m or defMods()
    pcall(function()
        CONFIG:SetRandomMod(slot, m.r or 0); CONFIG:SetStealthMod(slot, m.st or 0); CONFIG:SetJusticeMod(slot, m.ju or 0)
        CONFIG:SetTimingZone(slot, m.tz or 2); CONFIG:SetScrollSpeed(slot, m.ss or 9); CONFIG:SetFunMod(slot, 0); CONFIG:SetAutoStatus(slot, false)
    end)
end
function LO.myReady() return net.readyByPeer[NET:SelfId()] == true end
function LO.myDiff() return net.diffByPeer[NET:SelfId()] or 1 end

-- the controller can start whenever a song is chosen (even if some players aren't ready yet)
function LO.canStart() return LO.amController() and net.song ~= nil and LO.count() >= 2 end
function LO.hostStart()
    if not LO.canStart() then return false end
    NET:Broadcast("go", "{}"); net.goSignal = true
    return true
end

-- ── play launch ─────────────────────────────────────────────────────────────────────────────────────
-- on "go": configure + mount the shared song, mount remote players onto virtual slots, open the play round.
-- Returns true (Exit"play"), "eject" (this client lacks the song → leave the room), or false (with net.msg).
function LO.launchPlay()
    local s = net.song; if not s then return false end
    local node = findSong(s.uid)
    if not node then return "eject" end                  -- song eject: don't have it → kicked when play starts
    local me = NET:SelfId()
    local order = { me }
    for _, id in ipairs(LO.peerIds()) do if id ~= me then order[#order + 1] = id end end
    local N = #order; if N > 5 then N = 5 end
    local function diffOf(i) return (order[i] and (net.diffByPeer[order[i]] or 1)) or 0 end
    pcall(function()
        CONFIG.IsAIBattleMode = false
        CONFIG.IsTrainingMode = false
        CONFIG.PlayerCount = N
        CONFIG.SongSpeed = s.speed or 20                 -- song PLAYBACK rate (host-set, shared)
        CONFIG:SetAutoStatus(0, false)                   -- YOU (spot 0) play for real (overrides the dialog's Auto)
        for sp = 1, N - 1 do CONFIG:SetAutoStatus(sp, true) end   -- remote spots AUTO-PLAY (judge sampled from the wire) + excluded from saving
        -- Dynamic beat is now a PER-PLAYER mod (chosen in mod select). Spot 0 keeps whatever fun mod the local
        -- player set; remote spots are auto-judged so theirs doesn't matter locally. Don't override fun mod here.
    end)
    local mounted = false
    pcall(function() mounted = node:Mount(diffOf(1), diffOf(2), diffOf(3), diffOf(4), diffOf(5)) end)
    if not mounted then net.msg = "Could not load the chart."; return false end
    pcall(function()
        for i = 2, N do
            local id = order[i]; local inf = net.infoByPeer[id] or {}
            local slot = i - 1
            VIRTUALSLOTS:SetCharacter(slot, inf.char or "None")
            VIRTUALSLOTS:SetPuchichara(slot, inf.puchi or "None")
            VIRTUALSLOTS:SetNameplateName(slot, inf.name or ("P" .. id))
            VIRTUALSLOTS:SetNameplateTitle(slot, inf.title or "")
            VIRTUALSLOTS:SetNameplateDan(slot, inf.dan or "")
            -- share nameplate styling: id resolves type+rarity (+title text) from the catalogue; dan plate type/gold
            if (inf.npid or -1) >= 0 then VIRTUALSLOTS:SetNameplateById(slot, inf.npid) end
            VIRTUALSLOTS:SetNameplateDanType(slot, inf.dtype or 0)
            VIRTUALSLOTS:SetNameplateDanGold(slot, inf.dgold and true or false)
            VIRTUALSLOTS:MountSlot(i, "V" .. slot)
            -- apply the remote player's character palette to this spot (player index i-1); clear if they have none
            pcall(function()
                local cha = GetSaveFile(i - 1):GetCharacter()
                if cha then
                    if inf.palStops then cha:SetPaletteGradient(inf.palStops, inf.palBlend or 0)
                    else cha:ClearPaletteGradient() end
                end
            end)
        end
    end)
    LO.stopPreview()
    NET:SetPlaySpots("[" .. table.concat(order, ",", 1, N) .. "]")
    NET:BeginPlaySync(LO.myName())
    return true
end

-- ── results ─────────────────────────────────────────────────────────────────────────────────────────
-- after the song: read the local result via PLAYSTATE, broadcast it, store it for the standings.
function LO.broadcastResult()
    NET:EndPlaySync()
    local gr = PLAYSTATE:GetGoodCount(0)   -- GREAT (perfect)
    local gd = PLAYSTATE:GetOkCount(0)     -- GOOD (ok)
    local ms = PLAYSTATE:GetBadCount(0)    -- BAD
    local sc = PLAYSTATE:GetScore(0); local co = PLAYSTATE:GetHighestCombo(0)
    local clr = PLAYSTATE:IsClear(0); local fc = PLAYSTATE:IsFullCombo(0); local pf = PLAYSTATE:IsPerfect(0)
    local ab = not PLAYSTATE:WasPlayEndedNormally()
    local tot = gr + gd + ms; local acc = tot > 0 and ((gr + gd * 0.5) / tot * 100.0) or 0
    net.resultByPeer[NET:SelfId()] = { name = LO.myName(), sc = sc, gr = gr, gd = gd, ms = ms, co = co, cl = clr, fc = fc, pf = pf, ac = acc, ab = ab }
    NET:Broadcast("result", string.format('{"n":%s,"sc":%d,"gr":%d,"gd":%d,"ms":%d,"co":%d,"cl":%s,"fc":%s,"pf":%s,"ac":%.2f,"ab":%s}',
        jstr(LO.myName()), sc, gr, gd, ms, co, clr and "true" or "false", fc and "true" or "false", pf and "true" or "false", acc, ab and "true" or "false"))
end
function LO.haveAllResults()
    for id in pairs(net.nameByPeer) do if not net.resultByPeer[id] then return false end end
    return true
end
function LO.standings()
    local ids = LO.peerIds(); local rows = {}
    for _, id in ipairs(ids) do
        local r = net.resultByPeer[id]
        rows[#rows + 1] = { id = id, name = (r and r.name) or net.nameByPeer[id] or ("P" .. id), r = r }
    end
    table.sort(rows, function(a, b)
        local sa = (a.r and not a.r.ab) and a.r.sc or -1
        local sb = (b.r and not b.r.ab) and b.r.sc or -1
        return sa > sb
    end)
    return rows
end

-- back to the lobby for the next round: the SERVER rotates the host-role; everyone clears the round.
function LO.nextRound()
    if NET:IsHost() then NET:RotateHost() end
    LO.setWatching(false)
    net.resultByPeer = {}; net.song = nil; net.songLevels = {}; net.iLackSong = false; net.goSignal = false
    net.resultsT, net.resultsReadyT = 0, nil
    for id in pairs(net.readyByPeer) do net.readyByPeer[id] = false end
    LO.stopPreview()
end

-- ── event drain (call every frame while online/connecting) ──────────────────────────────────────────
function LO.drain()
    while true do
        local e = NET:Poll(); if e == nil then break end
        local ty = e.Type
        if ty == "connected" then
            net.online, net.connecting = true, false; LO.refreshRoster()
        elseif ty == "joined" then
            LO.refreshRoster()
            if net.song and LO.amController() then NET:SendTo(e.Peer, "song", songJson(net.song)) end
        elseif ty == "left" then
            net.diffByPeer[e.Peer] = nil; net.modByPeer[e.Peer] = nil; net.readyByPeer[e.Peer] = nil
            net.resultByPeer[e.Peer] = nil; net.lackByPeer[e.Peer] = nil; net.watchByPeer[e.Peer] = nil
            LO.refreshRoster()
        elseif ty == "roomclosed" then
            net.roomGone = true
        elseif ty == "error" then
            net.msg = e.Data or "Network error."; if not net.online then net.connecting = false end
        elseif ty == "message" then
            local ch = e.Channel; local s = JSONLOADER:JsonParseStringAny(e.Data)
            if ch == "song" and s then
                net.song = { uid = JSONLOADER:JsonGet(s, "u"), title = JSONLOADER:JsonGet(s, "t"),
                             diff = floor(JSONLOADER:JsonGet(s, "d") or 0), speed = floor(JSONLOADER:JsonGet(s, "s") or 20), dyn = floor(JSONLOADER:JsonGet(s, "b") or 0) }
                applySong()
            elseif ch == "diff" and s then net.diffByPeer[e.Peer] = floor(JSONLOADER:JsonGet(s, "d") or 1)
            elseif ch == "mod" and s then
                net.modByPeer[e.Peer] = { r = floor(JSONLOADER:JsonGet(s, "r") or 0), st = floor(JSONLOADER:JsonGet(s, "st") or 0),
                    ju = floor(JSONLOADER:JsonGet(s, "ju") or 0), tz = floor(JSONLOADER:JsonGet(s, "tz") or 2), ss = floor(JSONLOADER:JsonGet(s, "ss") or 9) }
            elseif ch == "ready" and s then net.readyByPeer[e.Peer] = JSONLOADER:JsonGet(s, "r") and true or false
            elseif ch == "have" and s then net.lackByPeer[e.Peer] = (JSONLOADER:JsonGet(s, "h") == false)
            elseif ch == "watch" and s then net.watchByPeer[e.Peer] = JSONLOADER:JsonGet(s, "w") and true or nil
            elseif ch == "go" then net.goSignal = true
            elseif ch == "result" and s then
                net.resultByPeer[e.Peer] = { name = JSONLOADER:JsonGet(s, "n"), sc = floor(JSONLOADER:JsonGet(s, "sc") or 0),
                    gr = floor(JSONLOADER:JsonGet(s, "gr") or 0), gd = floor(JSONLOADER:JsonGet(s, "gd") or 0), ms = floor(JSONLOADER:JsonGet(s, "ms") or 0),
                    co = floor(JSONLOADER:JsonGet(s, "co") or 0), cl = JSONLOADER:JsonGet(s, "cl") and true or false,
                    fc = JSONLOADER:JsonGet(s, "fc") and true or false, pf = JSONLOADER:JsonGet(s, "pf") and true or false,
                    ac = JSONLOADER:JsonGet(s, "ac") or 0, ab = JSONLOADER:JsonGet(s, "ab") and true or false }
            end
        end
    end
end

return LO
