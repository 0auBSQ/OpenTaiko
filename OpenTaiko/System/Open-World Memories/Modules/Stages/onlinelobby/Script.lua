---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- onlinelobby/Script.lua — a P2P 5-player rhythm lobby. Create/join a room (paste-in code); the lobby AUTHORITY
-- (rotates each round) picks the SONG (no difficulty prompt — the jacket + levels + preview show in the lobby) and
-- sets speed/dynamic-beat; every player picks their own difficulty + mods (real mod_select_dialog) + ready. The
-- host's first menu state is "Start play!" (selectable anytime). On start everyone plays the SAME song; remote
-- players render as real virtual-slot spots whose judges are sampled from the broadcast accuracy (no flying notes),
-- with synced loading/finish + results + host rotation. Net logic lives in online.lua (LO). See LuaNetworking.

local LO = require("online")
local net = LO.net

local SCREEN_W, SCREEN_H = 1920, 1080
local fontBig, fontMid, fontSmall
local dim
local mode = "menu"                         -- "menu" | "code" | "lobby" | "songselect" | "results"
local lastTs = 0
local menuSel = 1
local tab = 1                               -- per-player menu: 1 Ready/Start, 2 Difficulty, 3 Mods
local ti = nil
local act = nil                             -- song_select_core (host)
local modDlg = nil                          -- mod_select_dialog (per-player)
local modWasActive = false
local modicons = nil                        -- modicons ROActivity (draws a player's mod icons)
local pendingReturn = false
local msg, msgT = nil, 0

local DIFF_NAMES = LO.DIFF_NAMES
local function kp(k) return INPUT:KeyboardPressed(k) end

-- ── lifecycle ─────────────────────────────────────────────────────────────────────────────────────
function onStart()
    fontBig   = TEXT:Create(44)
    fontMid   = TEXT:Create(28)
    fontSmall = TEXT:Create(20)
    dim = CANVAS:CreateCanvas(2, 2); dim:Clear(255, 255, 255, 255); dim:Upload()
    -- NOTE: do NOT fetch activities here. At skin load, stages' PropagateOnStart runs BEFORE Activities and
    -- ROActivities are registered (see CSkin.FetchMenusAndModules), so GetActivity/GetROActivity return nil in
    -- onStart. We fetch them in activate() (runs on entry, after everything is loaded) — same as song_select_core.
end

function activate()
    lastTs = 0; INPUT:SetMouseLocked(false)
    if not modDlg then modDlg = ACTIVITY:GetActivity("mod_select_dialog") end
    if not modicons then modicons = ROACTIVITY:GetROActivity("modicons"); if modicons then pcall(function() modicons:Activate() end) end end
    if pendingReturn then                    -- returned from a song
        pendingReturn = false
        if net.online then LO.broadcastResult(); LO.setWatching(true) end
        net.resultsT, net.resultsReadyT = 0, nil
        mode = "results"
    end
end
function deactivate() LO.stopPreview(); if act and mode == "songselect" then act:Deactivate() end end
function afterSongEnum() end
function onDestroy() if modicons then pcall(function() modicons:Deactivate() end) end LO.leave() end

-- ── helpers ────────────────────────────────────────────────────────────────────────────────────────
local function txt(f, s, r, g, b, x, y, anchor)
    local t = f:GetText(s, false, 2400, COLOR:CreateColorFromRGBA(r, g, b, 255), COLOR:CreateColorFromRGBA(0, 0, 0, 255))
    if anchor == "center" then t:Draw(x - (t.Width or 0) / 2, y) else t:Draw(x, y) end
end
local function panel(x, y, w, h, r, g, b, a) dim:SetColor(r, g, b); dim:SetOpacity(a); dim:SetScale(w / 2, h / 2); dim:Draw(x, y) end
local function setMsg(m, t) msg = m; msgT = t or 4 end
local function getSignal(result)
    if type(result) == "string" then return result end
    if result == nil then return nil end
    local ok, val = pcall(function() return result[0] end)
    return ok and val or nil
end
local function backToMenu(m) LO.leave(); mode = "menu"; menuSel = 1; if m then setMsg(m, 5) end end

-- ── host song select (song only — no difficulty prompt; Auto disabled) ────────────────────────────────
local function enterSongSelect()
    if act == nil then act = ACTIVITY:GetActivity("song_select_core") end
    CONFIG.IsAIBattleMode = false
    act:Activate(false, 1, false, true)      -- allowPlayerCount=false, lockedPlayerCount=1, no AI slot, songOnly=true
    CONFIG:SetAutoStatus(0, false)
    mode = "songselect"
end
local function updateSongSelect()
    local sig = getSignal(act:Update())
    if sig == "play" then                    -- a SONG was confirmed (no difficulty step)
        local uid  = SONGMOUNT:ChosenUniqueId()
        local title = LO.resolveTitle(uid) or "Unknown Song"
        local sp = (net.song and net.song.speed) or 20
        local dy = (net.song and net.song.dyn == 1) or false
        act:Deactivate(); mode = "lobby"
        LO.setSong(uid, title, 0, sp, dy)
        LO.setDiff(LO.myDiff())
    elseif sig == "cancel" then
        act:Deactivate(); mode = "lobby"
    end
end

-- ── update ───────────────────────────────────────────────────────────────────────────────────────
function update(ts)
    local dt = (ts - lastTs) / 1000.0; lastTs = ts
    if dt < 0 then dt = 0 elseif dt > 0.1 then dt = 0.1 end
    if msgT > 0 then msgT = msgT - dt end

    if net.online or net.connecting then
        LO.drain()
        if net.roomGone then backToMenu("The room was closed."); return nil end
    end
    if mode == "lobby" or mode == "results" then LO.tickPreview(dt) end   -- loop the jacket preview (song select owns its own)

    if mode == "menu" then
        if kp("UpArrow") or kp("DownArrow") then menuSel = (menuSel == 1) and 2 or 1 end
        if kp("Return") or kp("Space") then
            if menuSel == 1 then if LO.host() then mode = "lobby"; tab = 1 end
            else mode = "code"; ti = INPUT:CreateTextInput("", 4096) end
        end
        if kp("Escape") then return Exit("stage", "_title") end

    elseif mode == "code" then
        if ti:Update() then
            local code = (ti.Text ~= "" and ti.Text) or nil
            if code and LO.join(code) then mode = "lobby"; tab = 1 else setMsg(net.msg or "Could not join.", 5); mode = "menu" end
        end
        if kp("Escape") then mode = "menu" end

    elseif mode == "songselect" then
        updateSongSelect()

    elseif mode == "lobby" then
        if not net.online then
            if not net.connecting then backToMenu(net.msg or "Disconnected.") end
            return nil
        end
        -- Play start is handled FIRST, even if the mod dialog is open (it is then force-closed WITHOUT saving).
        if net.goSignal then
            net.goSignal = false
            if modDlg and modDlg.IsActive then pcall(function() modDlg:Deactivate() end); modWasActive = false end
            local r = LO.launchPlay()
            if r == true then pendingReturn = true; return Exit("play", nil)
            elseif r == "eject" then backToMenu("You don't have this song — left the room."); return nil
            else setMsg(net.msg or "Can't start.", 5) end
        end
        -- mod_select_dialog modal: while open it owns all input (Esc = its Cancel)
        if modDlg and modDlg.IsActive then modDlg:Update(); modWasActive = true; return nil end
        if modWasActive then modWasActive = false; LO.broadcastMods() end

        if kp("UpArrow") then tab = (tab - 2) % 3 + 1 end
        if kp("DownArrow") then tab = tab % 3 + 1 end
        if tab == 1 then
            if LO.amController() then
                if kp("Space") or kp("Return") then if LO.hostStart() then setMsg("Starting…", 2) else setMsg("Pick a song first.", 3) end end
            else
                if kp("Space") or kp("Return") then LO.setReady(not LO.myReady()) end
            end
        elseif tab == 2 then
            if kp("LeftArrow") then LO.cycleDiff(-1) end    -- only difficulties the song actually has
            if kp("RightArrow") then LO.cycleDiff(1) end
        elseif tab == 3 then
            if kp("Space") or kp("Return") then
                if not modDlg then modDlg = ACTIVITY:GetActivity("mod_select_dialog") end   -- lazily (re)fetch if onStart missed it
                if modDlg then modDlg:Activate(0, true) else setMsg("Mod select unavailable.", 3) end   -- restrict: no Auto (Dynamic Beat IS allowed, per-player)
            end
        end
        -- controller-only song controls
        if LO.amController() then
            if kp("S") then enterSongSelect(); return nil end
            if net.song then
                if kp("Q") then LO.adjustSpeed(-1) end
                if kp("E") then LO.adjustSpeed(1) end
            end
        end
        -- Escape: un-ready if readied (Ready is reversible), otherwise leave the room
        if kp("Escape") then
            if LO.myReady() then LO.setReady(false) else backToMenu() end
        end

    elseif mode == "results" then
        net.resultsT = net.resultsT + dt
        if net.resultsReadyT == nil and (LO.haveAllResults() or net.resultsT > 20) then net.resultsReadyT = net.resultsT end
        if net.goSignal then        -- a new round started while we lingered on the results → kicked
            net.goSignal = false
            backToMenu("A new round started without you."); return nil
        end
        -- stay as long as you want; press Enter (once results are in) to return to the lobby
        if net.resultsReadyT ~= nil and (kp("Return") or kp("Space")) then
            LO.nextRound(); mode = "lobby"; tab = 1
        end
        if kp("Escape") then backToMenu(); return nil end
    end
    return nil
end

-- ── draw ───────────────────────────────────────────────────────────────────────────────────────────
local function drawMenu()
    txt(fontBig, "ONLINE LOBBY", 255, 230, 140, SCREEN_W / 2, 300, "center")
    local items = { "Create a room (host)", "Join a room (enter code)" }
    for i, label in ipairs(items) do
        local sel = (i == menuSel)
        txt(fontMid, (sel and "> " or "  ") .. label, sel and 255 or 200, sel and 235 or 205, sel and 120 or 210, SCREEN_W / 2, 470 + (i - 1) * 64, "center")
    end
    txt(fontSmall, "Up/Down choose   Enter select   Esc back", 180, 185, 195, SCREEN_W / 2, SCREEN_H - 100, "center")
end

local function drawCode()
    txt(fontBig, "JOIN A ROOM", 255, 230, 140, SCREEN_W / 2, 320, "center")
    txt(fontMid, "Paste the room code, then press Enter:", 220, 224, 235, SCREEN_W / 2, 470, "center")
    local shown = (ti and ti.DisplayText) or ""
    if #shown > 64 then shown = "..." .. string.sub(shown, #shown - 63) end
    txt(fontMid, shown .. " ", 255, 255, 255, SCREEN_W / 2, 540, "center")
    txt(fontSmall, "Ctrl+V paste   Enter join   Esc back", 180, 185, 195, SCREEN_W / 2, SCREEN_H - 100, "center")
end

-- jacket box (top-left); draws the SHARED preimage scaled to fit, with "Song not found" in red if we lack it
local JK_X, JK_Y, JK_W, JK_H = 96, 170, 240, 240
local function drawJacket()
    panel(JK_X - 6, JK_Y - 6, JK_W + 12, JK_H + 12, 0.10, 0.12, 0.18, 1.0)
    if net.song and not net.iLackSong then
        local jk = SHARED:GetSharedTexture("preimage")    -- same path as regular song select
        if jk then pcall(function()
            if jk.Height > 0 and jk.Width > 0 then
                jk:SetScale(JK_W / jk.Height, JK_H / jk.Width)
                jk:Draw(JK_X, JK_Y)
            end
        end) end
    end
    if net.song and net.iLackSong then
        panel(JK_X, JK_Y, JK_W, JK_H, 0.14, 0.05, 0.05, 1.0)
        txt(fontMid, "Song not found", 255, 110, 110, JK_X + JK_W / 2, JK_Y + JK_H / 2 - 16, "center")
    end
end

local function drawLobby()
    panel(0, 0, SCREEN_W, SCREEN_H, 0.05, 0.06, 0.10, 1.0)
    txt(fontBig, "LOBBY", 255, 235, 150, 96, 50)
    local cnt = LO.count()
    txt(fontSmall, (LO.amController() and "HOST — you pick the song" or "Player") .. "    " .. cnt .. "/" .. LO.MAXP .. " players",
        250, 220, 255, 320, 56)

    drawJacket()
    -- song info beside the jacket
    local sx = JK_X + JK_W + 30
    if net.song then
        txt(fontMid, net.song.title or "?", 255, 255, 255, sx, JK_Y + 6)
        if net.songSubtitle and net.songSubtitle ~= "" then txt(fontSmall, net.songSubtitle, 205, 205, 220, sx, JK_Y + 46) end
        txt(fontSmall, string.format("Speed x%.2f", (net.song.speed or 20) / 20), 190, 220, 255, sx, JK_Y + 76)
        -- available difficulties + their level numbers
        local lx = sx
        for d = 0, 4 do
            local lv = net.songLevels[d]
            if lv ~= nil then
                txt(fontSmall, (DIFF_NAMES[d] or "?") .. " " .. lv, 235, 225, 160, lx, JK_Y + 122)
                lx = lx + 200
            end
        end
    else
        txt(fontMid, LO.amController() and "Press [S] to choose a song" or "Waiting for the host to choose a song…", 220, 220, 160, sx, JK_Y + 80)
    end

    -- roster
    local ids = LO.peerIds(); local me = NET:SelfId()
    local y = 470
    txt(fontSmall, "PLAYERS", 200, 205, 215, 96, y - 34)
    for _, id in ipairs(ids) do
        local mine = (id == me)
        if mine then panel(88, y - 4, 1744, 48, 0.16, 0.22, 0.34, 1.0) end
        local nm = net.nameByPeer[id] or ("P" .. id)
        local ctrl = (NET:HostRoleId() == id) and "  [HOST]" or ""
        txt(fontMid, nm .. (mine and "  (You)" or "") .. ctrl, mine and 255 or 225, mine and 235 or 225, mine and 150 or 235, 110, y)
        txt(fontSmall, "Diff: " .. (DIFF_NAMES[net.diffByPeer[id] or 1] or "?"), 200, 220, 255, 640, y + 6)
        -- mod icons (real icons): self uses CONFIG[0], others use a scratch slot
        if modicons then
            pcall(function()
                if mine then modicons:Draw(880, y - 6, 0, "menu", 255)
                else LO.applyModsToSlot(4, net.modByPeer[id]); modicons:Draw(880, y - 6, 4, "menu", 255) end
            end)
        end
        if net.lackByPeer[id] then txt(fontSmall, "no song", 255, 120, 110, 1360, y + 6) end
        if net.readyByPeer[id] then txt(fontSmall, "READY", 130, 235, 130, 1500, y + 6)
        elseif net.watchByPeer[id] then txt(fontSmall, "watching results", 200, 200, 140, 1500, y + 6)
        else txt(fontSmall, "...", 180, 180, 185, 1500, y + 6) end
        y = y + 52
    end

    -- the local player's 3-state menu
    local panelY = SCREEN_H - 250
    panel(80, panelY, 1760, 150, 0.08, 0.10, 0.16, 1.0)
    local t1 = LO.amController() and "START PLAY!" or "READY!"
    local tabs = { t1, "DIFFICULTY", "MODS" }
    for i, tn in ipairs(tabs) do
        local sel = (i == tab)
        txt(fontMid, (sel and "> " or "  ") .. tn, sel and 255 or 190, sel and 235 or 195, sel and 130 or 200, 120 + (i - 1) * 360, panelY + 20)
    end
    if tab == 1 then
        if LO.amController() then
            txt(fontMid, net.song and "[Space] START the song (anytime)" or "Pick a song first ([S])", 230, 235, 150, 120, panelY + 80)
        else
            txt(fontMid, LO.myReady() and "You are READY  —  [Esc] to un-ready" or "[Space] to ready up", LO.myReady() and 130 or 230, 235, 150, 120, panelY + 80)
        end
    elseif tab == 2 then
        txt(fontMid, "Your difficulty:  < " .. (DIFF_NAMES[LO.myDiff()] or "?") .. " >   ([Left]/[Right])", 230, 235, 200, 120, panelY + 80)
    else
        txt(fontMid, "[Space] open Mod Select", 230, 235, 200, 120, panelY + 80)
    end

    local hint = LO.amController()
        and "[Up/Down] menu   [S] song   [Q]/[E] speed   [Esc] leave   (Dynamic Beat = your Mods)"
        or  "[Up/Down] menu   pick difficulty/mods, then Ready   [Esc] un-ready / leave"
    txt(fontSmall, hint, 255, 235, 150, SCREEN_W / 2, SCREEN_H - 60, "center")
    if net.connecting then
        panel(0, 0, SCREEN_W, SCREEN_H, 0.02, 0.03, 0.05, 0.7)
        txt(fontBig, "Connecting…", 255, 255, 255, SCREEN_W / 2, SCREEN_H / 2 - 30, "center")
    end
end

local function drawResults()
    panel(0, 0, SCREEN_W, SCREEN_H, 0.05, 0.06, 0.10, 1.0)
    txt(fontBig, "RESULTS", 255, 235, 150, SCREEN_W / 2, 70, "center")
    if net.song then txt(fontMid, net.song.title or "", 230, 230, 240, SCREEN_W / 2, 150, "center") end
    local rows = LO.standings()
    local y = 250
    for i, row in ipairs(rows) do
        local r = row.r
        if i == 1 and r and not r.ab then panel(SCREEN_W / 2 - 700, y - 6, 1400, 56, 0.20, 0.18, 0.06, 1.0) end
        local rc = (i == 1) and { 255, 215, 90 } or { 220, 222, 230 }
        txt(fontMid, i .. ".  " .. (row.name or "?"), rc[1], rc[2], rc[3], SCREEN_W / 2 - 680, y)
        if r and not r.ab then
            txt(fontMid, string.format("%d", r.sc), 255, 255, 255, SCREEN_W / 2 - 120, y)
            txt(fontSmall, string.format("%.2f%%  combo %d", r.ac, r.co), 200, 220, 255, SCREEN_W / 2 + 120, y + 6)
            local badge = r.pf and "PERFECT" or r.fc and "FULL COMBO" or r.cl and "CLEAR" or "FAILED"
            local bc = r.pf and { 255, 220, 120 } or r.fc and { 130, 235, 235 } or r.cl and { 130, 235, 130 } or { 230, 140, 140 }
            txt(fontSmall, badge, bc[1], bc[2], bc[3], SCREEN_W / 2 + 470, y + 6)
        else
            txt(fontSmall, r and "ABORTED" or "…still playing", 200, 180, 160, SCREEN_W / 2 - 120, y + 6)
        end
        y = y + 60
    end
    if net.resultsReadyT == nil then
        txt(fontSmall, "Waiting for everyone to finish…", 220, 220, 160, SCREEN_W / 2, SCREEN_H - 130, "center")
    else
        txt(fontSmall, "[Enter] back to the lobby   (stay as long as you like — but a new round will drop you)", 255, 235, 150, SCREEN_W / 2, SCREEN_H - 130, "center")
    end
end

function draw()
    if mode == "songselect" then act:Draw(); return end
    panel(0, 0, SCREEN_W, SCREEN_H, 0.04, 0.05, 0.09, 1.0)
    if mode == "menu" then drawMenu()
    elseif mode == "code" then drawCode()
    elseif mode == "lobby" then drawLobby()
    elseif mode == "results" then drawResults() end
    if mode == "lobby" and modDlg and modDlg.IsActive then modDlg:Draw() end
    if msgT and msgT > 0 and msg then txt(fontMid, msg, 255, 235, 160, SCREEN_W / 2, SCREEN_H - 40, "center") end
end
