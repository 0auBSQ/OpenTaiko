---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- jukebox.lua — the Jukebox furniture's audio player (My Room).
--
-- A two-pane PopUI window (left: BGM / Songs lists, right: now-playing panel with title, jacket,
-- transport buttons, speed slider and a clickable seek bar) driving a single LuaSound. Playback
-- OUTLIVES the menu: while the window is closed the volume follows the player's distance to the
-- jukebox (closer = louder) and the model's screen part (furniture.json emissivePart) pulses.
--
-- Songs come from the enumerated chart list (RequestSongList), grouped by genre; locked songs and
-- the whole "Secret Vault" genre are excluded. Until enumeration completes the Songs tab shows a
-- please-wait line (afterSongEnum flips the flag).
--
-- LuaSound gotchas honoured here (see song_select_core): there is NO pause — pause = remember
-- GetTimestampMs then Stop, resume = Play THEN SetTimestamp (Play always restarts at 0); always
-- Stop before re-Play (double-buffered sound would play twice); SetTimestamp takes timeline ms →
-- divide source ms by the current speed; SetLoop is only read at Play time.
--
-- Multiplayer: the HOST's jukebox state travels over the "jukebox" net channel as a full state
-- table (online.lua). Guests resolve the song against their OWN folders — UniqueId first, then
-- title candidates verified by chart md5, then a budgeted full-library md5 scan — and stay silent
-- when they don't have the chart. Guests never emit jukebox messages.

local PopUI = require("PopUI")
local I18N = require("i18n")

local JB = {}

-- ── layout ─────────────────────────────────────────────────────────────────────────────────────
local PANEL = { x = 160, y = 90, w = 1600, h = 900 }
local TAB   = { x = PANEL.x + 40, y = PANEL.y + 88, w = 220, h = 64, gap = 16 }
local LIST  = { x = PANEL.x + 40, y = PANEL.y + 190, w = 700, h = 620, row = 64 }
local RIGHT = { x = PANEL.x + 790, w = PANEL.w - 790 - 50 }
local RIGHT_CX = RIGHT.x + RIGHT.w / 2          -- PopUI centers align="center" labels ON x
local JACKET_BOX = 330
local JACKET_Y = PANEL.y + 200
local SEEK  = { x = RIGHT.x, y = PANEL.y + 716, w = RIGHT.w, h = 22 }

-- ── module state ───────────────────────────────────────────────────────────────────────────────
local ctx = nil                 -- Script.lua closures (see JB.init)
local ui = nil                  -- PopUI manager while the window is open
local isOpen = false
local tab = "bgm"               -- "bgm" | "songs"
local navGenre = nil            -- nil = genre level; a genre entry = its song list
local selEntry = nil            -- the list-selected entry (right pane preview before playing)
local justOpened = false        -- eat the opening Enter edge (phone pattern)

local songsReadyFlag = false    -- afterSongEnum fired (or a completed-enum build succeeded)
local grouped = nil             -- { {genre=, songs={entry...}}, ... } — the FILTERED, listable set
local resIndex = nil            -- unfiltered resolution index (guest md5/uid matching; lazy)
local resScan = nil             -- budgeted full-library md5 scan: { md5 = , i = , want = }
local md5Hits = {}              -- md5 -> entry|false (guest resolution cache)

-- playback (survives menu close). src = { kind="bgm" } or
-- { kind="song", title=, genre=, audioPath=, jacket=, node=, md5=, uid= } (guest: node may be nil)
local pb = {
    src = nil, snd = nil,
    playing = false, paused = false, pausedAt = 0,
    speed = 1.0, rep = false,
    cell = nil,                 -- {c=,r=} of the jukebox that owns playback (distance + glow)
    item = nil,                 -- the furniture entry (host side; guests only have cell)
    grace = 0,                  -- seconds to ignore IsPlaying==false after play/seek (async load)
    lastVol = -1,
    missing = false,            -- guest: state received but the chart isn't in our folders
}

-- UI widget refs (nil while closed)
local W = {}                    -- tabs/menu/labels/buttons/slider/toggle
local jacketTex, jacketPath = nil, nil
local seekDrag = nil            -- 0..1 while dragging the seek bar
local prevLeftPress = false
local netDirty, netAt, netNow = false, 0, 0
-- crossfade duck (the PC screen fades the jukebox down while its own BGM plays) + per-frame throttles:
-- the sound/scene bridge is only polled a few times a second, so playback never taxes the frame loop
local duck = { cur = 1, target = 1, delay = 0 }
local volAcc, endAcc, glowAcc = 1, 0, 0

local function tr(s) return I18N.tr(s) end
local function clamp(v, a, b) if v < a then return a end if v > b then return b end return v end

-- ── song list (filtered, grouped by genre) ─────────────────────────────────────────────────────
local function nodeEntry(n)
    local ok, e = pcall(function()
        return { kind = "song", title = n.Title or "?", subtitle = n.Subtitle or "", genre = n.Genre or "?",
                 audioPath = n.AudioPath or "", jacket = (n.HasPreimage and n.PreimagePath) or nil,
                 node = n, uid = n.UniqueId or "" }
    end)
    if ok then return e end
    return nil
end

local function buildGrouped()
    local out, order = {}, {}
    local ok = pcall(function()
        local lsls = GenerateSongListSettings()
        lsls:SetExcludedGenreFolders({ "Secret Vault", "段位道場", "太鼓タワー" })
        lsls.ModuloPagination     = false
        lsls.AppendMainRandomBox  = false
        lsls.AppendSubRandomBoxes = false
        lsls.SubBackBoxFrequency  = 0
        lsls:SetMandatoryDifficultyList({ 0, 1, 2, 3, 4 })
        lsls.MandatoryDifficultyMatchAll = false
        local list = RequestSongList(lsls)
        if list == nil then return end
        local res = list:SearchSongsByPredicate(function(n)
            local keep = false
            pcall(function()
                keep = n.IsSong and (not n.IsLocked) and (n.HiddenIndex or 0) < 3
                       and n.Genre ~= "Secret Vault" and (n.AudioPath or "") ~= ""
            end)
            return keep
        end)
        for i = 0, res.Count - 1 do
            local e = nodeEntry(res[i])
            if e then
                local g = out[e.genre]
                if g == nil then g = { genre = e.genre, songs = {} }; out[e.genre] = g; order[#order + 1] = g end
                g.songs[#g.songs + 1] = e
            end
        end
    end)
    if not ok or #order == 0 then return nil end
    return order
end

local function songsReady()
    if songsReadyFlag then return true end
    if IsSongsEnumDone and IsSongsEnumDone() then songsReadyFlag = true; return true end
    return false
end

local function ensureGrouped()
    if grouped ~= nil then return true end
    if not songsReady() then return false end
    grouped = buildGrouped()
    return grouped ~= nil
end

local pendingRestore = nil      -- a saved song state that arrived before enumeration finished
function JB.onSongEnumDone()
    songsReadyFlag = true
    grouped, resIndex, md5Hits, resScan = nil, nil, {}, nil   -- stale wrappers → rebuild lazily
    if isOpen then JB._rebuildList() end
    if pendingRestore then
        local t = pendingRestore; pendingRestore = nil
        if JB._doRestore then JB._doRestore(t) end
    end
end

-- ── guest-side song resolution (UniqueId → title+md5 → budgeted md5 scan) ──────────────────────
local function ensureResIndex()
    if resIndex ~= nil then return resIndex end
    resIndex = { byUid = {}, byTitle = {}, all = {} }
    pcall(function()
        local lsls = GenerateSongListSettings()
        lsls.ModuloPagination     = false
        lsls.AppendMainRandomBox  = false
        lsls.AppendSubRandomBoxes = false
        lsls.SubBackBoxFrequency  = 0
        lsls.IgnoreUnlockables    = true        -- playback-only: presence in the folders is what counts
        local list = RequestSongList(lsls)
        if list == nil then return end
        local res = list:SearchSongsByPredicate(function(n)
            local keep = false
            pcall(function() keep = n.IsSong and (n.AudioPath or "") ~= "" end)
            return keep
        end)
        for i = 0, res.Count - 1 do
            local e = nodeEntry(res[i])
            if e then
                resIndex.all[#resIndex.all + 1] = e
                if e.uid ~= "" then resIndex.byUid[e.uid] = e end
                local t = resIndex.byTitle[e.title]
                if t == nil then t = {}; resIndex.byTitle[e.title] = t end
                t[#t + 1] = e
            end
        end
    end)
    return resIndex
end

local function entryMd5(e)
    if e.md5 == nil then
        local ok, m = pcall(function() return e.node.ChartMd5 end)
        e.md5 = (ok and m) or ""
    end
    return e.md5
end

-- resolve a remote {md5, uid, title} to a local entry; nil = not (yet) found. Starts the budgeted
-- scan on a miss; JB.update ticks it a few charts per frame so a huge library never hitches.
local function resolveRemote(md5, uid, title)
    if md5 == nil or md5 == "" then return nil end
    local hit = md5Hits[md5]
    if hit ~= nil then return hit or nil end
    local idx = ensureResIndex()
    if uid and uid ~= "" and idx.byUid[uid] then
        md5Hits[md5] = idx.byUid[uid]; return md5Hits[md5]
    end
    for _, e in ipairs(idx.byTitle[title or ""] or {}) do
        if entryMd5(e) == md5 then md5Hits[md5] = e; return e end
    end
    if resScan == nil or resScan.md5 ~= md5 then resScan = { md5 = md5, i = 1 } end
    return nil
end

local function tickResScan()
    if resScan == nil then return end
    local idx = ensureResIndex()
    local budget = 3
    while budget > 0 and resScan.i <= #idx.all do
        local e = idx.all[resScan.i]
        resScan.i = resScan.i + 1
        if entryMd5(e) == resScan.md5 then
            md5Hits[resScan.md5] = e
            local found, scanMd5 = e, resScan.md5
            resScan = nil
            if pb.missing and pb.src and pb.src.md5 == scanMd5 then JB._guestRestart(found) end
            return
        end
        budget = budget - 1
    end
    if resScan and resScan.i > #idx.all then md5Hits[resScan.md5] = false; resScan = nil end
end

-- ── playback engine ────────────────────────────────────────────────────────────────────────────
local function disposeSnd()
    if pb.snd then pcall(function() pb.snd:Stop() end); pcall(function() pb.snd:Dispose() end) end
    pb.snd = nil
end

local function createSnd(src)
    local snd = nil
    pcall(function()
        if src.kind == "bgm" then snd = SOUND:CreateBGM("Sounds/bgm.ogg")
        else snd = SOUND:CreateBGMFromAbsolutePath(src.audioPath) end
    end)
    return snd
end

local function applyVolumeNow(v)
    -- quantized to steps of 4 (inaudible): distance volume changes every step while walking, and
    -- each distinct SetVolume reaches the BASS mixer twice (double-buffered sound) — coarse steps
    -- keep the call rate tiny
    v = math.floor(clamp(v, 0, 100) / 4 + 0.5) * 4
    if v ~= pb.lastVol and pb.snd then
        pcall(function() pb.snd:SetVolume(v) end)
        pb.lastVol = v
    end
end

local function distanceVolume()
    if isOpen then return 100 end
    if pb.cell == nil or ctx == nil then return 100 end
    local px, pz = ctx.playerPos()
    local jx, jz = ctx.cellToWorld(pb.cell.c, pb.cell.r)
    if px == nil or jx == nil then return 100 end
    local dx, dz = px - jx, pz - jz
    local dist = math.sqrt(dx * dx + dz * dz)
    return 100 * clamp(1 - (dist - 1.5) / 8.5, 0, 1)   -- full ≤1.5 cells, silent from ~10 cells
end

local function markNet(immediate)
    netDirty = true
    if immediate then netAt = 0 end
    -- every playback op also notifies the stage so it can persist the state (leave/re-enter resume)
    if ctx and ctx.onState then pcall(ctx.onState) end
end

local function pbStart(src, posMs)
    disposeSnd()
    pb.src, pb.missing = src, false
    pb.snd = createSnd(src)
    pb.playing, pb.paused, pb.pausedAt = pb.snd ~= nil, false, 0
    pb.grace = 1.0
    pb.lastVol = -1
    if pb.snd then
        pcall(function()
            pb.snd:SetLoop(pb.rep)
            pb.snd:SetSpeed(pb.speed)
            pb.snd:Play()
            if (posMs or 0) > 0 then pb.snd:SetTimestamp(math.floor(posMs / pb.speed)) end
        end)
        applyVolumeNow(distanceVolume() * duck.cur)
    end
end

local function pbPos()
    if pb.paused then return pb.pausedAt end
    if pb.snd == nil then return 0 end
    local ok, p = pcall(function() return pb.snd:GetTimestampMs() end)
    return (ok and p) or 0
end

local function pbDur()
    if pb.snd == nil then return 0 end
    local ok, d = pcall(function() return pb.snd:GetDurationMs() end)
    return (ok and d) or 0
end

local function pbPause()
    if not pb.playing or pb.paused then return end
    pb.pausedAt = pbPos()
    pcall(function() pb.snd:Stop() end)
    pb.paused, pb.playing = true, false
end

local function pbResume()
    if not pb.paused or pb.snd == nil then return end
    pcall(function()
        pb.snd:SetLoop(pb.rep)
        pb.snd:SetSpeed(pb.speed)
        pb.snd:Play()                                             -- Play resets to 0 —
        pb.snd:SetTimestamp(math.floor(pb.pausedAt / pb.speed))   -- — so seek AFTER it
    end)
    pb.paused, pb.playing, pb.grace = false, true, 1.0
end

local function pbStop()
    if pb.snd then pcall(function() pb.snd:Stop() end) end
    pb.playing, pb.paused, pb.pausedAt = false, false, 0
end

local function pbSeek(ms)
    ms = clamp(ms, 0, math.max(0, pbDur()))
    if pb.paused then pb.pausedAt = ms; return end
    if pb.playing and pb.snd then
        pcall(function() pb.snd:SetTimestamp(math.floor(ms / pb.speed)) end)
        pb.grace = 0.5
    end
end

local function pbSetSpeed(s)
    s = clamp(s, 0.5, 2.0)
    if math.abs(s - pb.speed) < 0.001 then return end
    pb.speed = s
    if pb.playing and pb.snd then pcall(function() pb.snd:SetSpeed(s) end) end
end

local function pbSetRepeat(b)
    if pb.rep == b then return end
    pb.rep = b
    if pb.snd == nil then return end
    if pb.playing then
        -- SetLoop is read at Play time: restart in place with the new flag
        local pos = pbPos()
        pcall(function()
            pb.snd:Stop()
            pb.snd:SetLoop(b)
            pb.snd:SetSpeed(pb.speed)
            pb.snd:Play()
            pb.snd:SetTimestamp(math.floor(pos / pb.speed))
        end)
        pb.grace = 1.0
    else
        pcall(function() pb.snd:SetLoop(b) end)
    end
end

-- ── screen glow (the GLB's emissivePart part objects; falls back to nothing on box art) ────────
local glowItem = nil            -- the furniture entry whose screen we last touched (to restore)
local function setScreenGlow(item, r, g, b)
    if ctx == nil or item == nil then return end
    local inst = ctx.propInstFor(item)
    if inst == nil or inst.parts == nil then return end
    local scene = ctx.scene()
    if scene == nil or scene.ObjSetEmissive == nil then return end
    for _, p in ipairs(inst.parts) do
        pcall(function() scene:ObjSetEmissive(p.obj, r, g, b) end)
    end
end

local IDLE_GLOW = { 0.12, 0.114, 0.096 }   -- matches the build-time dim glow (emissive 1,0.95,0.8 × 0.12)
local function tickGlow(t)
    local item = pb.item or (pb.cell and ctx and ctx.jukeboxItemAt(pb.cell.c, pb.cell.r)) or nil
    if glowItem and glowItem ~= item then
        setScreenGlow(glowItem, IDLE_GLOW[1], IDLE_GLOW[2], IDLE_GLOW[3]); glowItem = nil
    end
    if item == nil then return end
    glowItem = item
    if pb.playing then
        local p = 0.55 + 0.3 * math.sin(t * 3.2)
        setScreenGlow(item, p, p * 0.95, p * 0.8)
    elseif pb.paused then
        setScreenGlow(item, 0.3, 0.285, 0.24)
    else
        setScreenGlow(item, IDLE_GLOW[1], IDLE_GLOW[2], IDLE_GLOW[3])
    end
end

-- ── net state (host emits; guests apply) ───────────────────────────────────────────────────────
function JB.netState()
    if pb.src == nil then return nil end
    local s = pb.src
    return { kind = s.kind, md5 = s.md5 or "", uid = s.uid or "", title = s.title or "",
             genre = s.genre or "", playing = pb.playing, paused = pb.paused,
             pos = pbPos(), speed = pb.speed, rep = pb.rep,
             c = pb.cell and pb.cell.c or 0, r = pb.cell and pb.cell.r or 0 }
end

local function hostEmit()
    if ctx and ctx.isHost() and ctx.isOnline() then
        -- no source = the explicit STOP state (kind "") so guests silence too, not a skipped send
        ctx.broadcast(JB.netState() or { kind = "" })
    end
end

-- host: ensure the outgoing song state carries the content-stable md5 (computed once per play)
local function fillSrcMd5(src)
    if src.kind == "song" and (src.md5 == nil or src.md5 == "") and src.node then
        local ok, m = pcall(function() return src.node.ChartMd5 end)
        src.md5 = (ok and m) or ""
    end
end

function JB._guestRestart(entry)
    local keep = pb.src
    local src = { kind = "song", title = keep.title, genre = keep.genre, md5 = keep.md5, uid = keep.uid,
                  audioPath = entry.audioPath, node = entry.node, jacket = entry.jacket }
    local posMs, playing, paused = keep._netPos or 0, keep._netPlaying, keep._netPaused
    pbStart(src, posMs)
    if paused then pbPause(); pb.pausedAt = posMs end   -- the stub reads position 0; keep the NET pos
    if not playing and not paused then pbStop() end
end

function JB.applyNetState(t)
    if ctx == nil or ctx.isHost() then return end
    if type(t) ~= "table" then return end
    local kind = t.kind
    if kind == nil or (kind ~= "bgm" and kind ~= "song") then pbStop(); pb.src = nil; return end
    pb.cell = { c = math.floor(tonumber(t.c) or 0), r = math.floor(tonumber(t.r) or 0) }
    pb.item = nil
    pb.rep = t.rep == true
    local speed = clamp(tonumber(t.speed) or 1.0, 0.5, 2.0)
    local pos = math.max(0, tonumber(t.pos) or 0)
    local sameSrc = pb.src ~= nil and pb.src.kind == kind
                    and (kind == "bgm" or pb.src.md5 == (t.md5 or ""))
    if not sameSrc then
        if kind == "bgm" then
            pb.speed = speed
            pbStart({ kind = "bgm", title = tr("My Room BGM") }, pos)
        else
            local e = resolveRemote(t.md5, t.uid, t.title)
            pb.speed = speed
            if e == nil then
                -- not in our folders (yet): stay silent but remember the state; the budgeted scan
                -- may still find it and _guestRestart picks it up
                disposeSnd()
                pb.src = { kind = "song", title = t.title, genre = t.genre, md5 = t.md5, uid = t.uid,
                           _netPos = pos, _netPlaying = t.playing == true, _netPaused = t.paused == true }
                pb.playing, pb.paused, pb.missing = false, false, true
                return
            end
            pbStart({ kind = "song", title = t.title, genre = t.genre, md5 = t.md5, uid = t.uid,
                      audioPath = e.audioPath, node = e.node, jacket = e.jacket }, pos)
        end
        if t.paused == true then pbPause(); pb.pausedAt = pos end   -- the fresh stub reports pos 0
        if t.playing ~= true and t.paused ~= true then pbStop() end
        return
    end
    -- same source: apply deltas
    if pb.missing then
        pb.src._netPos, pb.src._netPlaying, pb.src._netPaused = pos, t.playing == true, t.paused == true
        pb.speed, pb.rep = speed, t.rep == true      -- keep for the eventual _guestRestart
        return
    end
    pbSetSpeed(speed)
    pbSetRepeat(t.rep == true)
    if t.playing == true and not pb.playing then
        if pb.paused then pb.pausedAt = pos; pbResume() else pbStart(pb.src, pos) end
    elseif t.paused == true and not pb.paused then
        pbPause(); pb.pausedAt = pos
    elseif t.paused == true and pb.paused then
        pb.pausedAt = pos                            -- host seeked while paused
    elseif t.playing ~= true and t.paused ~= true and (pb.playing or pb.paused) then
        pbStop()
    elseif t.playing == true then
        -- both playing: treat a large position gap as a host seek
        local here = pbPos()
        if math.abs(here - pos) > 1500 then pbSeek(pos) end
    end
end

-- ── selection / play from the UI ───────────────────────────────────────────────────────────────
local function playEntry(e, item)
    local src
    if e.kind == "bgm" then
        src = { kind = "bgm", title = tr("My Room BGM") }
    else
        src = { kind = "song", title = e.title, subtitle = e.subtitle, genre = e.genre,
                audioPath = e.audioPath, node = e.node, jacket = e.jacket, uid = e.uid, md5 = e.md5 }
        fillSrcMd5(src)
        e.md5 = src.md5
    end
    if item then pb.item = item; pb.cell = { c = item.c, r = item.r } end
    pbStart(src, 0)
    markNet(true)
end

-- ── UI ─────────────────────────────────────────────────────────────────────────────────────────
local function fmtTime(ms)
    local s = math.max(0, math.floor((ms or 0) / 1000))
    return string.format("%d:%02d", math.floor(s / 60), s % 60)
end

local function listItems()
    if tab == "bgm" then
        return { { text = tr("My Room BGM"), value = { kind = "bgm", title = tr("My Room BGM") } } }
    end
    if not ensureGrouped() then return {} end
    local items = {}
    if navGenre == nil then
        for _, g in ipairs(grouped) do
            items[#items + 1] = { text = string.format("%s  (%d)", g.genre, #g.songs), value = g }
        end
    else
        -- back is the STICKY button above the list (W.backBtn), not a scrolling row
        for _, e in ipairs(navGenre.songs) do
            items[#items + 1] = { text = e.title, value = e }
        end
    end
    return items
end

local function setJacket(path)
    if path == jacketPath then return end
    if jacketTex then pcall(function() jacketTex:Dispose() end) end
    jacketTex, jacketPath = nil, path
    if path and path ~= "" then
        pcall(function() jacketTex = TEXTURE:CreateTextureFromAbsolutePath(path, { maxSize = 512 }) end)
    end
end

-- does a list entry correspond to what is playing right now?
local function entryMatchesPlaying(v)
    if pb.src == nil or type(v) ~= "table" then return false end
    if v.kind == "bgm" then return pb.src.kind == "bgm" end
    if v.kind == "song" and pb.src.kind == "song" then
        if v.audioPath and v.audioPath ~= "" and v.audioPath == pb.src.audioPath then return true end
        if v.uid and v.uid ~= "" and v.uid == pb.src.uid then return true end
    end
    return false
end

-- the now-playing row (and its genre folder) lights up gold; marks live on the menu's item tables so
-- no rebuild/rebake is needed when playback changes
local function refreshListMarks()
    if not (isOpen and W.menu and W.menu.items) then return end
    for _, it in ipairs(W.menu.items) do
        local v, m = it.value, false
        if type(v) == "table" and v.songs then
            for _, e in ipairs(v.songs) do if entryMatchesPlaying(e) then m = true; break end end
        else
            m = entryMatchesPlaying(v)
        end
        it.mark = m or nil
    end
end

-- the right pane always shows the PLAYING source (never the hovered row)
local function updateRightPane()
    local e = pb.src
    if W.title then W.title:setText((e and e.title) or tr("Nothing playing")) end
    if W.subtitle then
        local sub = e and e.subtitle or ""
        W.subtitle:setText(sub)
        W.subtitle:setVisible(sub ~= "")
    end
    setJacket(e and e.jacket or nil)
    if W.playBtn then
        W.playBtn:setIcon((pb.playing and not pb.paused) and "pause" or "play")
    end
    if W.repBtn and W.repBtn.accent ~= pb.rep then
        W.repBtn.accent = pb.rep
        W.repBtn:restyle()
    end
    refreshListMarks()
end

local openItem = nil            -- the jukebox furniture entry the window was opened for
local function onListSelect(_, it)
    local v = it.value
    if tab == "songs" and navGenre == nil and type(v) == "table" and v.songs then
        navGenre = v; JB._rebuildList(); return
    end
    -- a playable entry: selecting = play (audio player behavior)
    selEntry = v
    playEntry(v, openItem or pb.item)
    updateRightPane()
end

-- hover/selection movement only remembers the row (for the play button when idle); the right pane
-- stays bound to what is PLAYING
local function onListChange(_, it)
    local v = it and it.value
    if type(v) == "table" and (v.kind == "song" or v.kind == "bgm") then
        selEntry = v
    end
end

function JB._rebuildList()
    if not isOpen or ui == nil then return end
    if W.menu then
        ui:remove(W.menu)          -- unregister BEFORE dispose: a disposed widget left in the manager
        W.menu:dispose()           -- would nil-index its baked canvases on the next draw
        W.menu = nil
    end
    local items = listItems()
    if #items > 0 then
        W.menu = ui:menu{ x = LIST.x, y = LIST.y, w = LIST.w, h = LIST.h, rowHeight = LIST.row,
                          items = items, onSelect = onListSelect, onChange = onListChange }
    end
    if W.waiting then
        if tab == "songs" and not songsReady() then
            W.waiting:setText(tr("Sorting the records... the song list is still being prepared."))
            W.waiting:setVisible(true)
        elseif tab == "songs" and #items == 0 then
            W.waiting:setText(tr("No playable songs found."))
            W.waiting:setVisible(true)
        else
            W.waiting:setVisible(false)
        end
    end
    if W.backBtn then W.backBtn:setVisible(tab == "songs" and navGenre ~= nil) end
    updateRightPane()
end

local function switchTab(t)
    if tab == t then return end
    tab = t; navGenre = nil
    if W.tabBgm then W.tabBgm.accent = (t == "bgm"); W.tabBgm:restyle() end
    if W.tabSongs then W.tabSongs.accent = (t == "songs"); W.tabSongs:restyle() end
    JB._rebuildList()
end

local function speedText() return tr("Speed") .. string.format("  ×%.2f", pb.speed) end

local function buildUI(itemName)
    ui = PopUI.new{ theme = ctx and ctx.theme or nil }
    W = {}
    ui:panel{ x = PANEL.x, y = PANEL.y, w = PANEL.w, h = PANEL.h, title = itemName or tr("Jukebox") }
    W.tabBgm = ui:button{ x = TAB.x, y = TAB.y, w = TAB.w, h = TAB.h, text = tr("BGM"),
                          accent = (tab == "bgm"),
                          onClick = function() switchTab("bgm") end }
    W.tabSongs = ui:button{ x = TAB.x + TAB.w + TAB.gap, y = TAB.y, w = TAB.w, h = TAB.h, text = tr("Songs"),
                            accent = (tab == "songs"),
                            onClick = function() switchTab("songs") end }
    -- sticky back (song level only): fixed at the top of the list column, never scrolls with it
    W.backBtn = ui:button{ x = LIST.x + LIST.w - 96, y = TAB.y, w = 96, h = TAB.h, icon = "back",
                           onClick = function()
                               if navGenre ~= nil then navGenre = nil; JB._rebuildList() end
                           end }
    W.backBtn:setVisible(false)
    -- right pane: NOW-PLAYING title/subtitle + transport (centered labels take their CENTER x;
    -- both sit clear above the jacket box at JACKET_Y)
    W.title = ui:label{ x = RIGHT_CX, y = PANEL.y + 96, w = RIGHT.w, h = 44, text = tr("Nothing playing"),
                        size = 32, align = "center", maxWidth = RIGHT.w }
    W.subtitle = ui:label{ x = RIGHT_CX, y = PANEL.y + 148, w = RIGHT.w, h = 30, text = "",
                           size = 21, align = "center", maxWidth = RIGHT.w }
    W.subtitle:setVisible(false)
    W.waiting = ui:label{ x = LIST.x + LIST.w / 2, y = LIST.y + 40, w = LIST.w, h = 48, size = 26,
                          align = "center", maxWidth = LIST.w,
                          text = tr("Sorting the records... the song list is still being prepared.") }
    W.waiting:setVisible(false)
    -- icon transport (language-agnostic, like a real audio player): ▶/⏸ toggles, ⏹, and 🔁 that
    -- stays lit (accent) while repeat is on
    local by = PANEL.y + 550
    local bw, bh, bgap = 120, 64, 22
    local bx0 = RIGHT.x + (RIGHT.w - (bw * 3 + bgap * 2)) / 2
    W.playBtn = ui:button{ x = bx0, y = by, w = bw, h = bh, icon = "play", accent = true,
        onClick = function()
            if pb.playing and not pb.paused then pbPause(); markNet(true)
            elseif pb.paused then pbResume(); markNet(true)
            elseif selEntry then onListSelect(nil, { value = selEntry })
            elseif pb.src then pbStart(pb.src, 0); markNet(true) end
            updateRightPane()
        end }
    W.stopBtn = ui:button{ x = bx0 + bw + bgap, y = by, w = bw, h = bh, icon = "stop",
        onClick = function() pbStop(); markNet(true); updateRightPane() end }
    W.repBtn = ui:button{ x = bx0 + (bw + bgap) * 2, y = by, w = bw, h = bh, icon = "repeat",
                          accent = pb.rep,
        onClick = function()
            pbSetRepeat(not pb.rep); markNet(true); updateRightPane()
        end }
    W.speedLbl = ui:label{ x = RIGHT.x, y = by + 96, w = 220, h = 40, text = speedText(), size = 26 }
    W.speed = ui:slider{ x = RIGHT.x + 240, y = by + 96, w = RIGHT.w - 240, h = 40,
                         min = 0.5, max = 2.0, step = 0.05, value = pb.speed, showValue = false,
                         onChange = function(v)
                             pbSetSpeed(v); markNet(false)
                             if W.speedLbl then W.speedLbl:setText(speedText()) end
                         end }
    W.timeLbl = ui:label{ x = RIGHT_CX, y = SEEK.y + 34, w = SEEK.w, h = 36, size = 24, align = "center",
                          text = "0:00 / 0:00" }
    JB._rebuildList()
    if W.menu then ui:_setFocusIndex(1) end
end

-- ── seek bar (custom: drawn + dragged by hand so it behaves like a real audio player) ──────────
local function seekHitTest(mx, my)
    return mx >= SEEK.x - 8 and mx <= SEEK.x + SEEK.w + 8 and my >= SEEK.y - 12 and my <= SEEK.y + SEEK.h + 12
end

local function tickSeekBar()
    local ok, mx, my = pcall(function() return INPUT:GetMouseXY() end)
    if not ok then return end
    local pressing = false
    pcall(function() pressing = INPUT:MousePressing("left") end)
    if seekDrag ~= nil then
        seekDrag = clamp((mx - SEEK.x) / SEEK.w, 0, 1)
        if not pressing then
            local dur = pbDur()
            if dur > 0 then pbSeek(seekDrag * dur); markNet(true) end
            seekDrag = nil
        end
    elseif pressing and not prevLeftPress and seekHitTest(mx, my) then
        seekDrag = clamp((mx - SEEK.x) / SEEK.w, 0, 1)
    end
    prevLeftPress = pressing
    -- while (or the frame) the seek bar owns the mouse, blank it for PopUI so no widget under the
    -- cursor hovers/presses/drags in parallel (the manager's one-shot escape hatch, cleared each frame)
    if seekDrag ~= nil and ui then ui._suppressMouse = true end
end

-- ── public API ─────────────────────────────────────────────────────────────────────────────────
-- ctx: { theme, isHost(), isOnline(), broadcast(stateTable), playerPos()->px,pz,
--        cellToWorld(c,r)->x,z, propInstFor(item)->inst|nil, jukeboxItemAt(c,r)->item|nil,
--        scene()->scene|nil, currentJukeboxItem()->item|nil }
function JB.init(c) ctx = c end

function JB.isOpen() return isOpen end
function JB.isActive() return pb.playing or pb.paused end   -- pc.lua BGM suppression probe

function JB.openFor(item, displayName)
    if isOpen then return end
    isOpen, justOpened = true, true
    tab, navGenre, selEntry = "bgm", nil, nil
    openItem = item
    if item and not (pb.playing or pb.paused) then pb.item = item; pb.cell = { c = item.c, r = item.r } end
    buildUI(displayName)
    if pb.snd then applyVolumeNow(100) end
end

function JB.songsEnumReady() return songsReady() end

-- after an edit-mode commit: playback dies with its jukebox; a moved jukebox re-anchors the audio
function JB.onRoomEdited(roomObj)
    if pb.item == nil then return end
    local present = false
    for _, it in ipairs((roomObj and roomObj.furniture) or {}) do
        if it == pb.item then present = true; break end
    end
    if not present then JB.stopAll()
    else pb.cell = { c = pb.item.c, r = pb.item.r } end
end

function JB.close()
    if not isOpen then return end
    isOpen = false
    if ui then pcall(function() ui:disposeWidgets() end); ui = nil end
    W = {}
    setJacket(nil)
    seekDrag = nil
end

-- end-of-track probe, defined ONCE (no per-frame closure churn) and polled at 4 Hz. NOTE:
-- LuaSound.Loaded and .IsPlaying are C# PROPERTIES (field-style access) — calling them like
-- methods raises, which an early build did every frame inside this pcall (the "moving is laggy"
-- exception storm).
local function endCheck()
    local loaded = pb.snd.Loaded
    if loaded and not pb.snd.IsPlaying then return true end
    local dur = pb.snd:GetDurationMs()
    if loaded and dur > 0 and pb.snd:GetTimestampMs() >= dur - 25 then return true end
    return false
end

function JB.setDuck(on, delay)
    duck.target = on and 0 or 1
    duck.delay = delay or 0
end

-- every-frame upkeep; when the window is open also runs the UI. Returns "closed" the frame the
-- window shuts (Escape/cancel) so Script can flip modes.
local lastTimeTxt = nil
function JB.update(dt, ts)
    dt = dt or 0
    netNow = netNow + dt
    -- playback upkeep runs ALWAYS (audio outlives the menu)
    if pb.grace > 0 then pb.grace = math.max(0, pb.grace - dt) end
    if pb.playing and not pb.paused and pb.snd and pb.grace <= 0 and not pb.rep then
        endAcc = endAcc + dt
        if endAcc >= 0.25 then
            endAcc = 0
            local ok, over = pcall(endCheck)
            if ok and over then pbStop(); markNet(true); if isOpen then updateRightPane() end end
        end
    end
    -- duck ramp (PC-screen crossfade): ~0.3s full fade after the optional delay
    local fading = duck.cur ~= duck.target
    if duck.delay > 0 then duck.delay = math.max(0, duck.delay - dt)
    elseif fading then
        local d = duck.target - duck.cur
        local step = dt * 3.4
        if math.abs(d) <= step then duck.cur = duck.target
        else duck.cur = duck.cur + (d > 0 and step or -step) end
    end
    -- distance/duck volume at 10 Hz (every frame while a fade is animating so it stays smooth)
    if pb.snd and (pb.playing or pb.paused) then
        volAcc = volAcc + dt
        if fading or volAcc >= 0.1 then
            volAcc = 0
            applyVolumeNow(distanceVolume() * duck.cur)
        end
    end
    glowAcc = glowAcc + dt
    if glowAcc >= 0.033 then glowAcc = 0; tickGlow((ts or 0) / 1000) end
    tickResScan()
    -- host: flush pending net state (debounced for slider drags)
    if netDirty and ctx and ctx.isHost() and ctx.isOnline() then
        if netAt <= 0 or netNow - netAt > 0.25 then
            netDirty = false; netAt = netNow
            hostEmit()
        end
    elseif netDirty and (ctx == nil or not ctx.isOnline()) then
        netDirty = false
    end
    if not isOpen then return nil end
    -- UI
    if justOpened then justOpened = false; return nil end   -- eat the opening Enter edge
    tickSeekBar()
    local dur, pos = pbDur(), pbPos()
    if seekDrag ~= nil and dur > 0 then pos = seekDrag * dur end
    local txt = fmtTime(pos) .. " / " .. fmtTime(dur)
    if txt ~= lastTimeTxt and W.timeLbl then lastTimeTxt = txt; W.timeLbl:setText(txt) end
    local r = nil
    if ui then r = ui:update(ts) end
    if r == "cancel" then
        if tab == "songs" and navGenre ~= nil then
            navGenre = nil; JB._rebuildList()          -- Escape backs out of a genre first...
        else
            JB.close(); return "closed"                -- ...and closes the player from the top level
        end
    end
    return nil
end

function JB.draw()
    if not isOpen or ui == nil then return end
    ui:draw()
    -- jacket (under the title, like song select's preimage)
    if jacketTex and jacketTex.Loaded then
        pcall(function()
            local w, h = jacketTex.Width, jacketTex.Height
            if w > 0 and h > 0 then
                local s = math.min(JACKET_BOX / w, JACKET_BOX / h)
                jacketTex:SetScale(s, s)
                jacketTex:Draw(RIGHT.x + (RIGHT.w - w * s) / 2, JACKET_Y + (JACKET_BOX - h * s) / 2)
                jacketTex:SetScale(1, 1)
            end
        end)
    end
    -- seek bar: track, elapsed fill, knob
    local dur, pos = pbDur(), pbPos()
    local frac = (dur > 0) and clamp(pos / dur, 0, 1) or 0
    if seekDrag ~= nil then frac = seekDrag end
    ui:rect(SEEK.x, SEEK.y, SEEK.w, SEEK.h, 40, 36, 48, 235)
    ui:rect(SEEK.x, SEEK.y, math.floor(SEEK.w * frac), SEEK.h, 255, 176, 82, 255)
    local kx = SEEK.x + math.floor(SEEK.w * frac)
    ui:rect(kx - 5, SEEK.y - 6, 10, SEEK.h + 12, 255, 232, 190, 255)
end

-- ── persistence: the playing song survives leaving/re-entering the room ───────────────────────────
-- Script stores this table in the room LMDB (per save UID) on every playback op (ctx.onState) and
-- restores it in loadRoom. Songs re-resolve against the CURRENT library by uid/md5/title (the same
-- matcher guests use), so a moved/renamed chart degrades to silence instead of a broken path.
function JB.persistState()
    if pb.src == nil or (not pb.playing and not pb.paused) then return nil end
    local s = pb.src
    return { kind = s.kind, md5 = s.md5 or "", uid = s.uid or "", title = s.title or "",
             genre = s.genre or "", pos = pbPos(), speed = pb.speed, rep = pb.rep,
             c = pb.cell and pb.cell.c or -1, r = pb.cell and pb.cell.r or -1,
             paused = pb.paused == true }
end

function JB._doRestore(t)
    local item = nil
    if ctx then
        item = ctx.jukeboxItemAt(math.floor(tonumber(t.c) or -1), math.floor(tonumber(t.r) or -1))
        if item == nil and ctx.anyJukeboxItem then item = ctx.anyJukeboxItem() end
    end
    if item == nil then return end                       -- no jukebox placed anymore: stay quiet
    pb.item = item; pb.cell = { c = item.c, r = item.r }
    pb.rep = t.rep == true
    pb.speed = clamp(tonumber(t.speed) or 1.0, 0.5, 2.0)
    local pos = math.max(0, tonumber(t.pos) or 0)
    local src
    if t.kind == "bgm" then
        src = { kind = "bgm", title = tr("My Room BGM") }
    else
        local e = ((t.md5 or "") ~= "") and resolveRemote(t.md5, t.uid, t.title) or nil
        if e == nil then resScan = nil; return end       -- library changed: skip (no background scan)
        src = { kind = "song", title = e.title, subtitle = e.subtitle, genre = e.genre,
                audioPath = e.audioPath, node = e.node, jacket = e.jacket, uid = e.uid, md5 = t.md5 }
    end
    pbStart(src, pos)
    if t.paused == true then pbPause(); pb.pausedAt = pos end
end

function JB.restoreState(t)
    if type(t) ~= "table" or t.kind == nil then return end
    if t.kind == "song" and not songsReady() then pendingRestore = t; return end
    JB._doRestore(t)
end

-- stop + release everything (edit mode entry, room leave, stage teardown)
function JB.stopAll(keepState)
    pbStop()
    if not keepState then pb.src, pb.item, pb.cell = nil, nil, nil end
    if glowItem then setScreenGlow(glowItem, IDLE_GLOW[1], IDLE_GLOW[2], IDLE_GLOW[3]); glowItem = nil end
    disposeSnd()
    markNet(true)
end

function JB.dispose()
    JB.close()
    pbStop()
    disposeSnd()
    setJacket(nil)
    grouped, resIndex, md5Hits, resScan = nil, nil, {}, nil
end

return JB
