---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- Online.Lobby: the online lobby's session on the NET core, with no drawing. Up to 5 players: the player
-- with the host role (NET:HasHostRole, rotated after each round) picks the song and its speed; every player
-- picks their own difficulty and mods and gets ready. On "go" each machine plays the song as a normal
-- N-player game: spot 0 is the local player, the others are mounted on virtual slots and auto-play with
-- their judges sampled from the wire (the gameplay screen does that part). Then every player broadcasts
-- their result and the host role moves on.
--
-- One session per Lua state (the module is a singleton). The skin reads Lobby.net each frame and calls the
-- functions below; see the Online page of the docs for the screen flow and the song-select wiring.

local P = require("Online.Protocol")
local Identity = require("Online.Identity")
local Media = require("Online.Media")
local Texts = require("Online.Texts")

local floor = math.floor

local LO = {}

LO.net = {
    online = false, isHost = false, connecting = false, roomGone = false, code = nil, msg = nil,
    msgCode = nil, msgArg = nil,      -- msg's stable code ("open_failed", ..., "net" for a NET text) and its argument
    notice = nil,                     -- a message NET sent while in a room, for the lobby to show once
    nameByPeer = {}, infoByPeer = {}, diffByPeer = {}, modByPeer = {}, readyByPeer = {}, resultByPeer = {},
    lackByPeer = {}, watchByPeer = {},
    song = nil,                       -- {uid, title, diff, speed, dyn(0/1)}
    songLevels = {}, songPlus = {}, iLackSong = false,
    goSignal = false, songChanged = false,
    resultsT = 0, resultsReadyT = nil,
}
local net = LO.net
LO.MAXP = P.MAXP
LO.ROOM_ID = P.ROOM_ID
LO.RESULTS_WAIT = 20                  -- seconds the results wait for the last players

-- an optional listener the skin sets: LO.onEvent(kind, peer, data) for every event drain handles
LO.onEvent = nil
local function emit(kind, peer, data) if LO.onEvent then LO.onEvent(kind, peer, data) end end

local function setMsg(code, text, arg) net.msg, net.msgCode, net.msgArg = text, code, arg end

-- re-exported pieces of the other modules
LO.myName = Identity.myName
LO.selfInfo = Identity.selfInfo
LO.selfPaletteJson = Identity.selfPaletteJson
LO.haveSong = Media.haveSong
LO.resolveTitle = Media.resolveTitle
LO.stopPreview = Media.stopPreview
function LO.loadSongMedia() Media.load(net) end
function LO.restoreMedia() Media.restore(net) end
function LO.tickPreview(dt) Media.tick(net, dt) end

function LO.amController() return net.online and NET:HasHostRole() end
function LO.count() local c = 0; for _ in pairs(net.nameByPeer) do c = c + 1 end return c end

-- ── roster ──────────────────────────────────────────────────────────────────────────────────────
function LO.refreshRoster()
    local names = {}
    local arr = JSONLOADER:JsonParseStringAny(NET:PeersJson()); local n = arr and JSONLOADER:JsonCount(arr) or 0
    for i = 1, n do
        local e = JSONLOADER:JsonGet(arr, i); local id = e and JSONLOADER:JsonGet(e, "id")
        if id then
            id = floor(id)
            local io = e and JSONLOADER:JsonGet(e, "info"); io = io and JSONLOADER:JsonParseStringAny(io) or nil
            local inf = Identity.parseInfo(io, id)
            names[id] = inf.name
            net.infoByPeer[id] = inf
            if net.diffByPeer[id] == nil then net.diffByPeer[id] = 1 end
            if net.modByPeer[id] == nil then net.modByPeer[id] = P.defMods() end
            if net.readyByPeer[id] == nil then net.readyByPeer[id] = false end
        end
    end
    net.nameByPeer = names
    LO.mountRemotes()
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
    net.notice = nil
    net.nameByPeer, net.infoByPeer, net.diffByPeer, net.modByPeer, net.readyByPeer = {}, {}, {}, {}, {}
    net.resultByPeer, net.lackByPeer, net.watchByPeer = {}, {}, {}
    net.song = nil; net.songLevels = {}; net.songPlus = {}; net.iLackSong = false
    net.goSignal = false; net.songChanged = false
    net.resultsT, net.resultsReadyT = 0, nil
    Media.stopPreview()
end
local function clearLobbyAuto() pcall(function() for sp = 0, 4 do CONFIG:SetAutoStatus(sp, false) end end) end   -- no Auto icon online
-- the reason NET queued for a refused host (read before any room exists, so drain never sees it)
local function takeError()
    local why = nil
    while true do
        local e = NET:Poll(); if e == nil then break end
        if e.Type == "error" then why = e.Data end
    end
    return why
end

-- opens a room; false with net.msg when NET refused
function LO.host()
    clearLobbyAuto()
    NET:SetLocalPlayer(Identity.selfInfo())
    local code = NET:CreateRoom(P.ROOM_ID, "", LO.MAXP)
    if not code or code == "" then
        local why = takeError()
        if why then setMsg("net", why) else setMsg("open_failed", Texts.tr("msg_open_failed")) end
        return false
    end
    net.code, net.online, net.isHost, net.connecting = code, true, true, false
    net.nameByPeer, net.infoByPeer, net.diffByPeer, net.modByPeer, net.readyByPeer = {}, {}, {}, {}, {}
    STORAGE:WriteLobbyCode("lobby.txt", code)
    STORAGE:RevealLobbyCodes()
    LO.refreshRoster()
    setMsg("room_open", Texts.tr("msg_room_open"))
    return true
end

-- starts joining the room of a code (the answer comes through drain); false with net.msg when refused at once
function LO.join(code)
    code = (code or ""):gsub("%s", "")
    if code == "" then setMsg("no_code", Texts.tr("msg_no_code")); return false end
    local sid = NET:PeekStageId(code)
    if sid ~= P.ROOM_ID then
        if sid then setMsg("code_other", Texts.trf("msg_code_other", sid), sid) else setMsg("code_invalid", Texts.tr("msg_code_invalid")) end
        return false
    end
    NET:SetLocalPlayer(Identity.selfInfo())
    net.connecting, net.isHost = true, false
    net.nameByPeer, net.infoByPeer, net.diffByPeer, net.modByPeer, net.readyByPeer = {}, {}, {}, {}, {}
    if not NET:JoinRoom(code) then
        net.connecting = false
        local why = takeError()
        if why then setMsg("net", why) else setMsg("join_failed", Texts.tr("msg_join_failed")) end
        return false
    end
    setMsg("connecting", Texts.tr("connecting"))
    return true
end
-- always ends the NET session: after a failed join its workers are still there
function LO.leave() NET:Leave(); reset(); LO.restoreSpots() end

-- ── selections ────────────────────────────────────────────────────────────────────────────────────
function LO.rebroadcastSong() if net.song then NET:Broadcast("song", P.songJson(net.song)) end end
-- the difficulties the song has (all 5 when unknown or missing)
function LO.availDiffs()
    local a = {}
    for d = 0, 4 do if net.songLevels[d] ~= nil then a[#a + 1] = d end end
    if #a == 0 then a = { 0, 1, 2, 3, 4 } end
    return a
end
function LO.diffAvailable(d)
    for _, a in ipairs(LO.availDiffs()) do if a == d then return true end end
    return false
end
-- the difficulty nearest to d among those the song has (the lower one on a tie)
function LO.nearestDiff(d)
    local best, bestDist = d, math.huge
    for _, a in ipairs(LO.availDiffs()) do
        local dist = math.abs(a - d)
        if dist < bestDist then best, bestDist = a, dist end
    end
    return best
end
-- moves my difficulty to the next one the song has in that direction; false at the end of the row
function LO.stepDiff(dir)
    local d = LO.myDiff() + dir
    while d >= 0 and d <= 4 do
        if LO.diffAvailable(d) then LO.setDiff(d); return true end
        d = d + dir
    end
    return false
end
-- keeps my difficulty on one the song has (call every lobby frame; broadcasts only when it moves)
function LO.fixDiff()
    local cur = LO.myDiff()
    if not LO.diffAvailable(cur) then LO.setDiff(LO.nearestDiff(cur)) end
end
-- a song set or received: clears ready, loads its media, tells the room whether I have it, and keeps my
-- difficulty on one the song has
local function applySong()
    for id in pairs(net.readyByPeer) do net.readyByPeer[id] = false end
    net.songChanged = true
    Media.load(net)
    LO.fixDiff()
    NET:Broadcast("have", P.haveJson(not net.iLackSong))
end
function LO.setSong(uid, title, diff, speed, dyn)
    net.song = { uid = uid, title = title, diff = diff or 0, speed = speed or 20, dyn = dyn and 1 or 0 }
    if LO.amController() then LO.rebroadcastSong() end
    applySong()
end
function LO.adjustSpeed(delta)
    if not net.song then return end
    LO.setSpeed((net.song.speed or CONFIG.SONGSPEED.Normal) + delta)
end
-- the controller's song speed; the song select and the lobby share it through CONFIG.SongSpeed
function LO.setSpeed(sp)
    if not net.song then return end
    local SPEED = CONFIG.SONGSPEED
    local actual = SPEED:ToActual(sp)
    if actual < 0.1 then actual = 0.1; sp = SPEED:FromActual(actual)
    elseif actual > 10 then actual = 10; sp = SPEED:FromActual(actual)
    end
    net.song.speed = sp
    if LO.amController() then pcall(function() CONFIG.SongSpeed = sp end) end
    -- the live preview follows at once (guests get it with the song rebroadcast)
    net.previewSpeed = actual
    pcall(function() local snd = SHARED:GetSharedSound("presound"); if snd then snd:SetSpeed(actual) end end)
    if LO.amController() then LO.rebroadcastSong() end
end
function LO.setDiff(d) net.diffByPeer[NET:SelfId()] = d; NET:Broadcast("diff", P.diffJson(d)) end
function LO.setReady(r) net.readyByPeer[NET:SelfId()] = r; NET:Broadcast("ready", P.readyJson(r)) end
function LO.setWatching(w) net.watchByPeer[NET:SelfId()] = w and true or nil; NET:Broadcast("watch", P.watchJson(w)) end
-- sends all my selections to one peer (a late joiner), who otherwise sees the Normal / no-mods defaults
function LO.sendMyStateTo(peer)
    local me = NET:SelfId()
    NET:SendTo(peer, "diff", P.diffJson(net.diffByPeer[me] or 1))
    local m = net.modByPeer[me] or P.defMods()
    NET:SendTo(peer, "mod", P.modJson(m))
    NET:SendTo(peer, "ready", P.readyJson(net.readyByPeer[me]))
    NET:SendTo(peer, "have", P.haveJson(not net.iLackSong))
    if net.watchByPeer[me] then NET:SendTo(peer, "watch", P.watchJson(true)) end
end
-- broadcasts all my selections (just after joining, so the players already there see them)
function LO.broadcastMyState()
    local me = NET:SelfId()
    NET:Broadcast("diff", P.diffJson(net.diffByPeer[me] or 1))
    LO.broadcastMods()
    NET:Broadcast("ready", P.readyJson(net.readyByPeer[me]))
end
-- reads player 0's mods from CONFIG (no Auto and no fun mod: both are off online), stores and broadcasts
-- them; call it when the skin's mod select closes
function LO.broadcastMods()
    local m = { r = CONFIG:GetRandomMod(0), st = CONFIG:GetStealthMod(0), ju = CONFIG:GetJusticeMod(0), tz = CONFIG:GetTimingZone(0), ss = CONFIG:GetScrollSpeed(0) }
    m.sm = CONFIG:GetSpecialMod(0)                    -- Flawless / Timed / Timed (Hard); Auto (1) is not sent
    if m.sm == 1 then m.sm = 0 end
    net.modByPeer[NET:SelfId()] = m
    NET:Broadcast("mod", P.modJson(m))
end
-- puts a player's mods on a CONFIG player slot, for drawing their mod icons with the modicons ROActivity
function LO.applyModsToSlot(slot, m)
    m = m or P.defMods()
    pcall(function()
        CONFIG:SetRandomMod(slot, m.r or 0); CONFIG:SetStealthMod(slot, m.st or 0); CONFIG:SetJusticeMod(slot, m.ju or 0)
        CONFIG:SetTimingZone(slot, m.tz or 2); CONFIG:SetScrollSpeed(slot, m.ss or 9); CONFIG:SetFunMod(slot, 0); CONFIG:SetAutoStatus(slot, false)
        CONFIG:SetSpecialMod(slot, (m.sm ~= 1) and m.sm or 0)
    end)
end
function LO.myReady() return net.readyByPeer[NET:SelfId()] == true end
function LO.myDiff() return net.diffByPeer[NET:SelfId()] or 1 end

-- why the controller cannot start yet: "need_song", "need_players", or nil when it can (players need not be
-- ready)
function LO.startProblem()
    if net.song == nil then return "need_song" end
    if LO.count() < 2 then return "need_players" end
    return nil
end
function LO.canStart() return LO.amController() and LO.startProblem() == nil end
function LO.hostStart()
    if not LO.canStart() then return false end
    NET:Broadcast("go", P.GO); net.goSignal = true
    return true
end
-- true once when the round started (then call launchPlay)
function LO.takeGo()
    if not net.goSignal then return false end
    net.goSignal = false
    return true
end

-- ── remote players on the local spots ─────────────────────────────────────────────────────────────
-- The others take the local game's spots 2-5 (virtual slots V1-V4) in the order a round mounts them, as
-- soon as they are in the room: their nameplates can be drawn by spot (LO.spotOf[peer]), and their
-- characters are loaded before the song starts. Leaving the room gives the spots back to the save files.
local mountedKey, mountedAny = nil, false
LO.spotOf = {}
function LO.mountRemotes()
    local me = NET:SelfId()
    local others = {}
    for _, id in ipairs(LO.peerIds()) do if id ~= me then others[#others + 1] = id end end
    local spots, key = { [me] = 0 }, ""
    for i, id in ipairs(others) do
        if i > LO.MAXP - 1 then break end
        spots[id] = i
        local inf = net.infoByPeer[id] or {}
        key = key .. id .. "|" .. tostring(inf.name) .. "|" .. tostring(inf.npk) .. "|" .. tostring(inf.char) .. "|"
            .. tostring(inf.dan) .. ";"
    end
    LO.spotOf = spots
    if key == mountedKey then return end
    mountedKey = key
    pcall(function()
        for i, id in ipairs(others) do
            if i > LO.MAXP - 1 then break end
            local inf = net.infoByPeer[id] or {}
            VIRTUALSLOTS:SetCharacter(i, inf.char or "None")
            VIRTUALSLOTS:SetPuchichara(i, inf.puchi or "None")
            VIRTUALSLOTS:SetNameplateName(i, inf.name or ("P" .. id))
            VIRTUALSLOTS:SetNameplateDan(i, inf.dan or "")
            VIRTUALSLOTS:SetNameplateById(i, inf.npk or "")
            VIRTUALSLOTS:SetNameplateDanType(i, inf.dtype or 0)
            VIRTUALSLOTS:SetNameplateDanGold(i, inf.dgold and true or false)
            VIRTUALSLOTS:MountSlot(i + 1, "V" .. i)
            mountedAny = true
        end
    end)
end
function LO.restoreSpots()
    mountedKey = nil
    LO.spotOf = {}
    if not mountedAny then return end
    mountedAny = false
    pcall(function() for p = 2, LO.MAXP do VIRTUALSLOTS:MountSlot(p, p .. "P") end end)
end

-- ── play launch ─────────────────────────────────────────────────────────────────────────────────────
-- on "go": sets up and mounts the room's song, mounts the remote players on virtual slots and opens the
-- play round. Returns true (then Exit("play")), "eject" (this machine lacks the song: leave the room), or
-- false (with net.msg).
function LO.launchPlay()
    local s = net.song; if not s then return false end
    local node = Media.findSong(s.uid)
    if not node then return "eject" end
    local me = NET:SelfId()
    local order = { me }
    for _, id in ipairs(LO.peerIds()) do if id ~= me then order[#order + 1] = id end end
    local N = #order; if N > 5 then N = 5 end
    local function diffOf(i) return (order[i] and (net.diffByPeer[order[i]] or 1)) or 0 end
    pcall(function()
        CONFIG.IsAIBattleMode = false
        CONFIG.IsTrainingMode = false
        CONFIG.PlayerCount = N
        CONFIG.SongSpeed = s.speed or 20                 -- the room's playback rate
        CONFIG:SetAutoStatus(0, false)                   -- the local player plays for real
        for sp = 1, N - 1 do CONFIG:SetAutoStatus(sp, true) end   -- remote spots auto-play from the wire and are not saved
        -- Dynamic Beat is a per-player mod: the local fun mod is left as the player set it
    end)
    local mounted = false
    pcall(function() mounted = node:Mount(diffOf(1), diffOf(2), diffOf(3), diffOf(4), diffOf(5)) end)
    if not mounted then setMsg("chart_failed", Texts.tr("msg_chart_failed")); return false end
    pcall(function()
        for i = 2, N do
            local id = order[i]; local inf = net.infoByPeer[id] or {}
            local slot = i - 1
            VIRTUALSLOTS:SetCharacter(slot, inf.char or "None")
            VIRTUALSLOTS:SetPuchichara(slot, inf.puchi or "None")
            VIRTUALSLOTS:SetNameplateName(slot, inf.name or ("P" .. id))
            VIRTUALSLOTS:SetNameplateDan(slot, inf.dan or "")
            -- the nameplate id resolves title, type and rarity from this machine's catalogue (no title if unknown)
            VIRTUALSLOTS:SetNameplateById(slot, inf.npk or "")
            VIRTUALSLOTS:SetNameplateDanType(slot, inf.dtype or 0)
            VIRTUALSLOTS:SetNameplateDanGold(slot, inf.dgold and true or false)
            VIRTUALSLOTS:MountSlot(i, "V" .. slot)
            -- the remote player's character palette on this spot (player i-1), cleared when they have none
            pcall(function()
                local cha = GetSaveFile(i - 1):GetCharacter()
                if cha then
                    if inf.palStops then cha:SetPaletteGradient(inf.palStops, inf.palBlend or 0)
                    else cha:ClearPaletteGradient() end
                end
            end)
        end
    end)
    Media.stopPreview()
    NET:SetPlaySpots("[" .. table.concat(order, ",", 1, N) .. "]")
    NET:BeginPlaySync(Identity.myName())
    return true
end

-- ── results ─────────────────────────────────────────────────────────────────────────────────────────
-- after the song: reads the local result from PLAYSTATE, broadcasts it and stores it for the standings
function LO.broadcastResult()
    NET:EndPlaySync()
    local gr = PLAYSTATE:GetGoodCount(0)   -- GREAT (perfect)
    local gd = PLAYSTATE:GetOkCount(0)     -- GOOD (ok)
    local ms = PLAYSTATE:GetBadCount(0)    -- BAD
    local sc = PLAYSTATE:GetScore(0); local co = PLAYSTATE:GetHighestCombo(0)
    local clr = PLAYSTATE:IsClear(0); local fc = PLAYSTATE:IsFullCombo(0); local pf = PLAYSTATE:IsPerfect(0)
    local ab = not PLAYSTATE:WasPlayEndedNormally()
    local tot = gr + gd + ms; local acc = tot > 0 and ((gr + gd * 0.5) / tot * 100.0) or 0
    local r = { name = Identity.myName(), sc = sc, gr = gr, gd = gd, ms = ms, co = co, cl = clr, fc = fc, pf = pf, ac = acc, ab = ab }
    net.resultByPeer[NET:SelfId()] = r
    NET:Broadcast("result", P.resultJson(r))
end
-- the skin's stage was entered again after the play started by launchPlay: sends the result, marks me as
-- watching the results and starts their timer
function LO.returnedFromPlay()
    if net.online then LO.broadcastResult(); LO.setWatching(true) end
    net.resultsT, net.resultsReadyT = 0, nil
end
function LO.haveAllResults()
    for id in pairs(net.nameByPeer) do if not net.resultByPeer[id] then return false end end
    return true
end
-- each frame on the results: true once every result is in or RESULTS_WAIT seconds have passed
function LO.tickResults(dt)
    net.resultsT = net.resultsT + dt
    if net.resultsReadyT == nil and (LO.haveAllResults() or net.resultsT > LO.RESULTS_WAIT) then net.resultsReadyT = net.resultsT end
    return net.resultsReadyT ~= nil
end
-- the rows of the results, best score first (aborted or missing results last): { id, name, r }
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

-- back to the lobby for the next round: the server rotates the host role, everyone clears the round
function LO.nextRound()
    if NET:IsHost() then NET:RotateHost() end
    LO.setWatching(false)
    net.resultByPeer = {}; net.song = nil; net.songLevels = {}; net.songPlus = {}; net.iLackSong = false; net.goSignal = false
    net.resultsT, net.resultsReadyT = 0, nil
    for id in pairs(net.readyByPeer) do net.readyByPeer[id] = false end
    Media.stopPreview()
end

-- ── the song-select wiring ──────────────────────────────────────────────────────────────────────────
-- the host role opens its song select: open() activates the skin's song select (song only, no
-- difficulty step); the room's speed is in CONFIG.SongSpeed while it browses
function LO.openSongSelect(open)
    CONFIG.IsAIBattleMode = false
    if net.song and net.song.speed then pcall(function() CONFIG.SongSpeed = net.song.speed end) end
    if open then open() end
    CONFIG:SetAutoStatus(0, false)
end
-- a song was confirmed in the song select (after deactivating it): it becomes the room's song at the speed
-- chosen there. uid defaults to SONGMOUNT:ChosenUniqueId(); fallbackTitle names a song without a title.
function LO.songPicked(uid, fallbackTitle)
    uid = uid or SONGMOUNT:ChosenUniqueId()
    local title = Media.resolveTitle(uid) or fallbackTitle or Texts.tr("unknown_song")
    local sp = CONFIG.SongSpeed or (net.song and net.song.speed) or 20
    local dy = (net.song and net.song.dyn == 1) or false
    LO.setSong(uid, title, 0, sp, dy)
    LO.setDiff(LO.myDiff())
end
-- the song select was left without a song (after deactivating it): keeps a speed changed there, and puts
-- the room's jacket and preview back
function LO.songPickCancelled()
    if net.song and CONFIG.SongSpeed ~= net.song.speed then LO.setSpeed(CONFIG.SongSpeed) end
    Media.restore(net)
end

-- ── event drain (call every frame while online or connecting) ──────────────────────────────────────
function LO.drain()
    while true do
        local e = NET:Poll(); if e == nil then break end
        local ty = e.Type
        if ty == "connected" then
            net.online, net.connecting = true, false; LO.refreshRoster()
            -- the players already in the room get my current selections
            LO.broadcastMyState()
            emit("connected", e.Peer)
        elseif ty == "joined" then
            LO.refreshRoster()
            if net.song and LO.amController() then NET:SendTo(e.Peer, "song", P.songJson(net.song)) end
            -- a newcomer starts everyone at Normal / no mods: my real state goes straight to them
            LO.sendMyStateTo(e.Peer)
            emit("joined", e.Peer)
        elseif ty == "left" then
            net.diffByPeer[e.Peer] = nil; net.modByPeer[e.Peer] = nil; net.readyByPeer[e.Peer] = nil
            net.resultByPeer[e.Peer] = nil; net.lackByPeer[e.Peer] = nil; net.watchByPeer[e.Peer] = nil
            LO.refreshRoster()
            emit("left", e.Peer)
        elseif ty == "roomclosed" then
            net.roomGone = true
            emit("roomclosed", e.Peer)
        elseif ty == "error" then
            if e.Data then setMsg("net", e.Data) else setMsg("network_error", Texts.tr("msg_network_error")) end
            if net.online then net.notice = net.msg else net.connecting = false end
            emit("error", e.Peer, net.msg)
        elseif ty == "message" then
            local ch = e.Channel; local s = JSONLOADER:JsonParseStringAny(e.Data)
            if ch == "song" and s then
                net.song = P.decodeSong(s)
                applySong()
            elseif ch == "diff" and s then net.diffByPeer[e.Peer] = P.decodeDiff(s)
            elseif ch == "mod" and s then net.modByPeer[e.Peer] = P.decodeMods(s)
            elseif ch == "ready" and s then net.readyByPeer[e.Peer] = P.decodeReady(s)
            elseif ch == "have" and s then net.lackByPeer[e.Peer] = P.decodeLack(s)
            elseif ch == "watch" and s then net.watchByPeer[e.Peer] = P.decodeWatch(s)
            elseif ch == "go" then net.goSignal = true
            elseif ch == "result" and s then net.resultByPeer[e.Peer] = P.decodeResult(s)
            end
            emit(ch, e.Peer, s)
        else
            emit(ty, e.Peer, e.Data)
        end
    end
end

return LO
