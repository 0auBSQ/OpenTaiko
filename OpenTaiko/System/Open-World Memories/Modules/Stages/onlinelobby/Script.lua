---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- onlinelobby/Script.lua — a P2P 5-player rhythm lobby. Create/join a room (paste-in code); the lobby AUTHORITY
-- (rotates each round) picks the SONG (no difficulty prompt — the jacket + levels + preview show in the lobby) and
-- sets speed/dynamic-beat; every player picks their own difficulty + mods (real mod_select_dialog) + ready. The
-- host's first menu state is "Start play!" (selectable anytime). On start everyone plays the SAME song; remote
-- players render as real virtual-slot spots whose judges are sampled from the broadcast accuracy (no flying notes),
-- with synced loading/finish + results + host rotation. Net logic lives in online.lua (LO). See LuaNetworking.

local NavInput = require("NavInput")
local LO = require("online")
local Space = require("space")
local net = LO.net
local L, LF = LO.tr, LO.trf

local SCREEN_W, SCREEN_H = 1920, 1080
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

local function panel(x, y, w, h, r, g, b, a) dim:SetColor(r, g, b); dim:SetOpacity(a); dim:SetScale(w / 2, h / 2); dim:Draw(x, y) end

-- ── keys: theme settings of type "key" ("" = unbound); the song speed keys are the song select's, and
-- choosing / confirming is the decide button ─────────────────────────────────────────────────────────────
local KEY_SETTINGS = {
    song = { "onlinelobby_key_song", "S" },
    speed_down = { "songselect_key_speed_down", "PageDown" }, speed_up = { "songselect_key_speed_up", "PageUp" },
}
local keys = {}

local function reloadKeys()
    for id, s in pairs(KEY_SETTINGS) do
        local ok, v = pcall(function() return THEME:GetThemeSetting(s[1]) end)
        keys[id] = (ok and type(v) == "string") and v or s[2]
    end
end

local function pressed(id)
    local k = keys[id]
    return k ~= nil and k ~= "" and INPUT:KeyboardPressed(k)
end

-- the labels in the hints, kept for the visit (the language and the binds can change between visits)
local keyLabels, bindNames = {}, {}

local function keyLabel(id)
    local s = keyLabels[id]
    if s == nil then
        local n = NavInput.keyName(keys[id])
        s = n and ("[" .. n .. "]") or L("key_unbound")
        keyLabels[id] = s
    end
    return s
end

local function speedLabel()
    local s = keyLabels.speed
    if s == nil then s = keyLabel("speed_down") .. "/" .. keyLabel("speed_up"); keyLabels.speed = s end
    return s
end

-- the game's Decide / Cancel buttons, named as the key config names them ("[Decide]", "[Cancel]")
local function bindLabel(which)
    local s = bindNames[which]
    if s == nil then
        local key = "SETTINGS_KEYASSIGN_GAME_" .. which
        local ok, v = pcall(function() return LANG:GetString(key) end)
        s = "[" .. ((ok and type(v) == "string" and v ~= "" and v ~= key) and v or (which == "DECIDE" and "Decide" or "Cancel")) .. "]"
        bindNames[which] = s
    end
    return s
end

-- space UI with a constellation theming (as it connects multiple players though the different open worlds within the open universe very poetic)
local TEX_NAMES = { "Logo", "LogoSmall", "Sparkle", "Glow", "Link", "Chart", "SongPlate", "RosterRow", "RosterRowYou",
    "RosterRowEmpty", "Deck", "MenuPlate", "MenuPlateOn", "CodePlate", "ResultRow", "ResultRowFirst", "HeaderPill",
    "Toast", "TabOn", "JacketFrame", "Halo", "SpeedArrow", "Difficulty/Missing", "Difficulty/Level/plus" }
for d = 0, 4 do TEX_NAMES[#TEX_NAMES + 1] = "Difficulty/" .. d end
for n = 0, 9 do TEX_NAMES[#TEX_NAMES + 1] = "Difficulty/Level/" .. n end
local tex = {}
local fonts = {}
local colors = {}
local clock = 0

local CREAM, GOLD, LAVENDER, DIM = { 250, 238, 206 }, { 255, 214, 122 }, { 184, 192, 232 }, { 128, 136, 178 }
local GREEN, RED, AMBER = { 142, 230, 160 }, { 255, 132, 120 }, { 240, 206, 128 }
local LINE_COL = { 240, 226, 180 }
local OUTLINE = { 6, 8, 26, 210 }
local TEXT_PAD = 25                          -- a glyph box's side padding
-- each difficulty's text colour: its emblem's hue (Easy blue, Normal green, Hard amber, Extreme coral, Extra
-- purple), lightened for the dark panels
local DIFF_COL = { [0] = { 110, 178, 255 }, [1] = { 84, 230, 180 }, [2] = { 250, 196, 76 }, [3] = { 255, 122, 112 }, [4] = { 180, 124, 255 } }

local SEAT_X = { 814, 1042, 1270, 1498, 1726 }
local SEAT_Y = { 176, 262, 200, 272, 164 }
local SEAT_COL = { { 170, 200, 255 }, { 236, 242, 255 }, { 255, 236, 170 }, { 255, 200, 140 }, { 255, 160, 140 } }
local ROW_X, ROW_TOP, ROW_PITCH, ROW_W, ROW_H = 680, 330, 92, 1160, 86
local DECK_X, DECK_Y, DECK_W, DECK_H, DECK_SPLIT = 80, 809, 1760, 140, 440

local function loadArt()
    for _, n in ipairs(TEX_NAMES) do tex[n] = TEXTURE:CreateTexture("Textures/" .. n .. ".png") end
end

local function font(size)
    local f = fonts[size]
    if f == nil then f = TEXT:CreateGlyphCached(size); fonts[size] = f end
    return f
end

local function color(c, a)
    a = a or c[4] or 255
    local key = c[1] .. "," .. c[2] .. "," .. c[3] .. "," .. a
    local v = colors[key]
    if v == nil then v = COLOR:CreateColorFromRGBA(c[1], c[2], c[3], a); colors[key] = v end
    return v
end

-- text with its ink centred on y; align "left" puts the ink's left edge at x, "right" its right edge
local function text(size, str, x, y, c, align, op, maxW)
    local f = font(size)
    align = align or "left"
    local ax = (align == "left") and (x - TEXT_PAD) or (align == "right") and (x + TEXT_PAD) or x
    local nudge = math.floor((f.BoxHeight - math.ceil(f.LineHeight)) / 2) - 1
    f:Draw(tostring(str), ax, y + nudge, color(c), color(OUTLINE), op or 1, 1, maxW and (maxW + 2 * TEXT_PAD) or 0, align)
end

local function textW(size, str) return font(size):Measure(tostring(str)) end

-- a texture centred on (x, y), scaled, faded, tinted, turned (degrees, counter-clockwise) and blended
local function sprite(name, x, y, sx, sy, op, rgb, rot, blend)
    local t = tex[name]
    if t == nil then return end
    t:SetScale(sx or 1, sy or sx or 1); t:SetOpacity(op or 1)
    if rgb then t:SetColor(rgb[1] / 255, rgb[2] / 255, rgb[3] / 255) end
    if rot then t:SetRotation(rot) end
    if blend then t:SetBlendMode(blend) end
    t:DrawAtAnchor(x, y, "center")
    t:SetScale(1, 1); t:SetOpacity(1)
    if rgb then t:SetColor(1, 1, 1) end
    if rot then t:SetRotation(0) end
    if blend then t:SetBlendMode("normal") end
end

-- a constellation line between two stars, stopping short of both
local function link(x1, y1, x2, y2, op, inset)
    local dx, dy = x2 - x1, y2 - y1
    local len = math.sqrt(dx * dx + dy * dy)
    inset = inset or 20
    if len <= 2 * inset then return end
    local ux, uy = dx / len, dy / len
    local ax, ay, bx, by = x1 + ux * inset, y1 + uy * inset, x2 - ux * inset, y2 - uy * inset
    sprite("Link", (ax + bx) / 2, (ay + by) / 2, (len - 2 * inset) / 128, 1, op, LINE_COL, -math.deg(math.atan(dy, dx)), "add")
end

-- a lit star: its glow, its sparkle, and for the room's host a second, turning sparkle
local function star(x, y, rgb, size, bright, host)
    local tw = 0.5 + 0.5 * math.sin(clock * 2.3 + x * 0.013)
    sprite("Glow", x, y, size * (1.0 + 0.12 * tw), nil, bright * (0.45 + 0.15 * tw), rgb, nil, "add")
    sprite("Sparkle", x, y, size * (0.92 + 0.1 * tw), nil, 0.75 + 0.25 * bright, rgb, nil, "add")
    if host then sprite("Sparkle", x, y, size * 0.62, nil, 0.8, GOLD, 45 + clock * 14, "add") end
end

-- a difficulty emblem centred on (cx, cy) at scale k with its level inside, the digits set like the song
-- select's song info (a plus level's "+" off the last digit's top-right corner); d nil = the empty emblem,
-- level nil = the emblem alone
local PLUS_DX, PLUS_DY = -8, -14
local function drawEmblem(d, level, isPlus, cx, cy, k, op)
    sprite(d ~= nil and ("Difficulty/" .. d) or "Difficulty/Missing", cx, cy, k, nil, op)
    if level == nil then return end
    local s = tostring(level)
    local total, prev, last = 0, 0, 0
    for i = 1, #s do
        local t = tex["Difficulty/Level/" .. s:sub(i, i)]
        if t then
            local w = (t.Width or 0) * k
            if i > 1 then total = total + prev * 0.5 end
            prev, last = w, w
        end
    end
    total = total + last
    local cursor = cx - total / 2
    prev = 0
    for i = 1, #s do
        local name = "Difficulty/Level/" .. s:sub(i, i)
        local t = tex[name]
        if t then
            local w = (t.Width or 0) * k
            if i > 1 then cursor = cursor + prev * 0.5 end
            sprite(name, cursor + w / 2, cy, k, nil, op)
            prev = w
        end
    end
    if isPlus then sprite("Difficulty/Level/plus", cx + total / 2 + PLUS_DX * k, cy + PLUS_DY * k, k, nil, op) end
end

-- ── the sky: space.lua renders it; the 2D screens only keep a clock for their own twinkles ──────────
local function tickLook(dt)
    clock = clock + dt
    Space.update(dt)
end

local function drawSky()
    if not Space.draw() then panel(0, 0, SCREEN_W, SCREEN_H, 0.02, 0.03, 0.09, 1) end
end

-- wordmarks over Logo/LogoSmall
local GILD = "<g.#FFFFFF.#BE7007>%s</g>"
local ASCENT = 0.7706
local WORD_GAP = 0.30
local WHITE, SHADOW = { 255, 255, 255 }, { 18, 16, 46 }
local LOGO_W, LOGO_H, LOGO_BASE = 1560, 380, 237
local LOGO_WORDS = { 66, 80, 0.24 }
local LOGO_STARS = { { 780, 62, 1.0 }, { 520, 83, 0.5 }, { 1040, 83, 0.5 }, { 64, 206, 0.6 }, { 1496, 206, 0.6 },
    { 702, 288, 0.4 }, { 741, 309, 0.4 }, { 780, 294, 0.4 }, { 819, 312, 0.4 }, { 858, 284, 0.4 } }
local SMALL_W, SMALL_H, SMALL_BASE, SMALL_X = 620, 100, 66, 58
local SMALL_WORDS = { 24, 29, 0.16 }
local SMALL_STARS = { { 28, 55, 0.7 }, { 594, 55, 0.45 } }
local LOGO_MAX_W, SMALL_MAX_W = 840, 280     -- room left for the text
local advances = {}

local function advance(size, ch)
    local key = size .. ch
    local a = advances[key]
    if a == nil then a = font(size):Measure(ch); advances[key] = a end
    return a
end

-- a letter with a capital form: ASCII, Latin-1 and Cyrillic capitals (the scripts without case get no initials)
local function isCapital(ch)
    local b1, b2 = ch:byte(1, 2)
    if #ch == 1 then return b1 >= 65 and b1 <= 90 end
    if b1 == 0xC3 then return b2 >= 0x80 and b2 <= 0x9E and b2 ~= 0x97 end
    if b1 == 0xD0 then return (b2 >= 0x90 and b2 <= 0xAF) or b2 == 0x81 end
    return false
end

-- the localized title as characters; a capital that starts a word (after a space or a hyphen) is an initial
local titleChars = { src = nil, list = {} }
local function title()
    local s = L("logo_title")
    if titleChars.src ~= s then
        local list, prev = {}, " "
        for ch in s:gmatch("[%z\1-\127\194-\244][\128-\191]*") do
            list[#list + 1] = { ch = ch, initial = (prev == " " or prev == "-") and isCapital(ch) }
            prev = ch
        end
        titleChars.src, titleChars.list = s, list
    end
    return titleChars.list
end

-- the title from x (its left end, or its middle for "center") on the baseline, shrunk to maxW when longer
local function drawWords(x, baseline, size, initial, track, align, maxW)
    local chars = title()
    local tr, gap = track * size * 1.3, WORD_GAP * size * 1.3
    local w = 0
    for i, c in ipairs(chars) do
        if c.ch == " " then w = w + gap
        else w = w + advance(c.initial and initial or size, c.ch) + (i < #chars and tr or 0) end
    end
    local f = (maxW and w > maxW) and maxW / w or 1
    local x0 = (align == "center") and (x - w * f / 2) or x
    local pen = 0
    for _, c in ipairs(chars) do
        if c.ch == " " then pen = pen + gap
        else
            local s = c.initial and initial or size
            local fnt = font(s)
            local top = baseline - fnt.LineHeight * ASCENT * f
            local bx = x0 + pen * f - TEXT_PAD * f
            fnt:Draw(c.ch, bx, top + 3 * f, color(SHADOW), color(SHADOW, 0), 0.85, f, 0, "topleft")
            fnt:Draw(string.format(GILD, c.ch), bx, top, color(WHITE), color(SHADOW, 0), 1, f, 0, "topleft")
            pen = pen + advance(s, c.ch) + tr
        end
    end
end

local function twinkleAt(x, y, s, i)
    local tw = 0.5 + 0.5 * math.sin(clock * (1.7 + i * 0.37) + i * 1.9)
    sprite("Sparkle", x, y, s * (0.8 + 0.35 * tw), nil, 0.25 + 0.55 * tw, GOLD, nil, "add")
end

local function drawLogo(cx, cy, k)
    sprite("Logo", cx, cy, k)
    drawWords(cx, cy + (LOGO_BASE - LOGO_H / 2) * k, math.floor(LOGO_WORDS[1] * k + 0.5), math.floor(LOGO_WORDS[2] * k + 0.5),
        LOGO_WORDS[3], "center", LOGO_MAX_W * k)
    for i, st in ipairs(LOGO_STARS) do
        twinkleAt(cx + (st[1] - LOGO_W / 2) * k, cy + (st[2] - LOGO_H / 2) * k, st[3] * 0.6 * k, i)
    end
end

local function drawSmallLogo(left, cy)
    sprite("LogoSmall", left + SMALL_W / 2, cy)
    drawWords(left + SMALL_X, cy + SMALL_BASE - SMALL_H / 2, SMALL_WORDS[1], SMALL_WORDS[2], SMALL_WORDS[3], "left", SMALL_MAX_W)
    for i, st in ipairs(SMALL_STARS) do twinkleAt(left + st[1], cy + st[2] - SMALL_H / 2, st[3] * 0.5, i) end
end

-- a message on one line, or on two split at the space nearest its middle when it is too long
local TOAST_W = 1120
local function toastLines(s)
    if textW(22, s) <= TOAST_W then return { s } end
    local best, bestD = nil, math.huge
    for i in s:gmatch("() ") do
        local d = math.abs(i - #s / 2)
        if d < bestD then best, bestD = i, d end
    end
    if best == nil then return { s } end
    return { s:sub(1, best - 1), s:sub(best + 1) }
end

local function drawToast()
    if not (msgT and msgT > 0 and msg) then return end
    local op = math.min(1, msgT / 0.4)
    sprite("Toast", 960, 988, 1, 1, op)
    local lines = toastLines(msg)
    if #lines == 1 then text(22, lines[1], 960, 988, GOLD, "center", op, TOAST_W)
    else
        text(20, lines[1], 960, 972, GOLD, "center", op, TOAST_W)
        text(20, lines[2], 960, 1004, GOLD, "center", op, TOAST_W)
    end
end

local function drawHint(s) text(20, s, 960, 1052, DIM, "center", 1, 1800) end

-- ── menu / code ─────────────────────────────────────────────────────────────────────────────────────
local function drawMenu()
    drawLogo(960, 250, 1)
    text(22, L("menu_subtitle"), 960, 470, LAVENDER, "center", 1, 1500)
    local items = { L("menu_create"), L("menu_join") }
    for i, label in ipairs(items) do
        local sel = (i == menuSel)
        local y = 590 + (i - 1) * 110
        sprite(sel and "MenuPlateOn" or "MenuPlate", 960, y)
        if sel then sprite("Sparkle", 960 - 350 + 44, y, 0.42 + 0.06 * math.sin(clock * 4), nil, 1, GOLD, nil, "add") end
        text(30, label, 960, y, sel and CREAM or LAVENDER, "center", 1, 560)
    end
    drawHint(LF("hint_menu", bindLabel("DECIDE"), bindLabel("CANCEL")))
end

local function drawCode()
    drawLogo(960, 210, 0.8)
    text(44, L("code_title"), 960, 420, CREAM, "center", 1, 1400)
    text(24, L("code_prompt"), 960, 486, LAVENDER, "center", 1, 1400)
    sprite("CodePlate", 960, 600)
    local shown = (ti and ti.DisplayText) or ""
    if #shown > 64 then shown = "..." .. string.sub(shown, #shown - 63) end
    text(28, shown .. " ", 960, 600, CREAM, "center", 1, 1040)
    drawHint(LF("hint_code", bindLabel("CANCEL")))
end

-- ── lobby ───────────────────────────────────────────────────────────────────────────────────────────
local JK_X, JK_Y, JK_S = 210, 200, 300        -- the jacket window inside its frame

-- the song speed as the song select shows it: white at x1.00, pink faster, blue slower; the two arrows
-- behind a changed value pulse half a beat apart (the chase runs backward when slowed down)
local SPEED_FAST, SPEED_SLOW = { 255, 158, 195 }, { 149, 204, 255 }
local function drawSpeed(x, labelY, valueY)
    local mult = net.song.speed and CONFIG.SONGSPEED:ToActual(net.song.speed) or 1
    local changed = math.abs(mult - 1) > 1e-4
    local col = not changed and WHITE or (mult > 1 and SPEED_FAST or SPEED_SLOW)
    if changed then
        local bpm = (net.songBpm or 0) * mult
        local beat = (bpm > 0) and math.max(150, math.min(2000, 60000 / bpm)) or 600
        local phase = ((clock * 1000) % beat) / beat
        if mult < 1 then phase = 1 - phase end
        for i = 0, 1 do
            local p = (phase - i * 0.5) % 1
            sprite("SpeedArrow", x + (i - 0.5) * 40, valueY, 1, nil, 0.25 + 0.75 * (0.5 + 0.5 * math.cos(2 * math.pi * p)), col)
        end
    end
    text(16, L("song_speed_label"), x, labelY, col, "center")
    text(26, string.format("x%.2f", mult), x, valueY, col, "center")
end

local function drawSong()
    sprite("SongPlate", 360, 465)
    text(18, L("song_label"), 360, 166, GOLD, "center")
    sprite("JacketFrame", JK_X + JK_S / 2, JK_Y + JK_S / 2)
    if net.song and not net.iLackSong then
        local jk = SHARED:GetSharedTexture("preimage")     -- same path as regular song select
        if jk then pcall(function()
            if jk.Height > 0 and jk.Width > 0 then
                jk:SetScale(JK_S / jk.Width, JK_S / jk.Height)
                jk:Draw(JK_X, JK_Y)
                jk:SetScale(1, 1)
            end
        end) end
    elseif net.song and net.iLackSong then
        panel(JK_X, JK_Y, JK_S, JK_S, 0.16, 0.05, 0.08, 0.9)
        text(26, L("song_not_found"), JK_X + JK_S / 2, JK_Y + JK_S / 2, RED, "center", 1, JK_S - 20)
    else
        star(JK_X + JK_S / 2, JK_Y + JK_S / 2, LAVENDER, 1.3, 0.5, false)
    end
    if net.song then
        text(28, net.song.title or "?", 360, 544, CREAM, "center", 1, 500)
        if net.songSubtitle and net.songSubtitle ~= "" then text(20, net.songSubtitle, 360, 579, LAVENDER, "center", 1, 500) end
        drawSpeed(360, 614, 644)
        -- every difficulty's emblem with its level; the ones this chart lacks show the empty emblem
        for d = 0, 4 do
            local lv = net.songLevels[d]
            drawEmblem(lv ~= nil and d or nil, lv, net.songPlus[d], 160 + d * 100, 716, 0.8, lv ~= nil and 1 or 0.5)
        end
    else
        text(22, LO.amController() and LF("song_pick", keyLabel("song")) or L("song_wait"), 360, 590, AMBER, "center", 1, 500)
    end
end

local function seatStatus(id)
    if net.lackByPeer[id] then return L("status_no_song"), RED end
    if net.readyByPeer[id] then return L("status_ready"), GREEN end
    if net.watchByPeer[id] then return L("status_watching"), AMBER end
    return L("status_not_ready"), DIM
end

-- the figure over the roster: one star per seat, lit by its player
local function drawFigure(ids)
    for i = 1, 4 do
        local lit = ids[i] ~= nil and ids[i + 1] ~= nil
        link(SEAT_X[i], SEAT_Y[i], SEAT_X[i + 1], SEAT_Y[i + 1], lit and 0.7 or 0.22, 30)
    end
    for i = 1, LO.MAXP do
        local x, y, id = SEAT_X[i], SEAT_Y[i], ids[i]
        if id == nil then
            sprite("Sparkle", x, y, 0.45, nil, 0.45, nil, nil, "add")
            text(18, L("seat_open"), x, y + 34, DIM, "center", 0.8, 210)
        else
            local ready = net.readyByPeer[id] == true
            star(x, y, SEAT_COL[i], ready and 1.2 or 0.95, ready and 1 or 0.55, NET:HostRoleId() == id)
            text(18, net.nameByPeer[id] or ("P" .. id), x, y + 36, CREAM, "center", 1, 210)
        end
    end
end

-- the roster: a row per seat with the player's nameplate, difficulty, mods and status
local function drawRoster(ids, me)
    local cx = ROW_X + ROW_W / 2
    for i = 1, LO.MAXP do
        local id = ids[i]
        local cy = ROW_TOP + (i - 1) * ROW_PITCH + ROW_H / 2
        if id == nil then
            sprite("RosterRowEmpty", cx, cy)
            sprite("Sparkle", ROW_X + 30, cy, 0.32, nil, 0.4, nil, nil, "add")
            text(20, L("seat_waiting"), cx, cy, DIM, "center", 0.85, 600)
        else
            local mine, host = (id == me), (NET:HostRoleId() == id)
            sprite(mine and "RosterRowYou" or "RosterRow", cx, cy)
            star(ROW_X + 30, cy, SEAT_COL[i], 0.5, net.readyByPeer[id] and 1 or 0.6, false)
            local spot = LO.spotOf[id]
            local drawn = false
            if spot ~= nil then
                drawn = pcall(function() NAMEPLATE:DrawPlayerNameplate(ROW_X + 52, math.floor(cy - 40), 255, spot) end)
            end
            if not drawn then text(24, net.nameByPeer[id] or ("P" .. id), ROW_X + 64, cy, mine and GOLD or CREAM, "left", 1, 310) end
            local d = net.diffByPeer[id] or 1
            local lv = (net.song ~= nil and not net.iLackSong) and net.songLevels[d] or nil
            drawEmblem(d, lv, net.songPlus[d], ROW_X + 448, cy, 0.68, 1)
            -- mod icons (real icons): self uses CONFIG[0], others use a scratch slot
            if modicons then
                pcall(function()
                    if mine then modicons:Draw(ROW_X + 510, cy - 18, 0, "menu", 255)
                    else LO.applyModsToSlot(4, net.modByPeer[id]); modicons:Draw(ROW_X + 510, cy - 18, 4, "menu", 255) end
                end)
            end
            local st, sc = seatStatus(id)
            local right = ROW_X + ROW_W - 26
            if host then
                text(20, st, right, cy - 12, sc, "right", 1, 250)
                text(16, L("tag_host"), right, cy + 16, GOLD, "right", 1, 250)
            else
                text(20, st, right, cy, sc, "right", 1, 250)
            end
        end
    end
end

-- the difficulty row of the deck: all five emblems, the chosen one full size under a halo that glides to
-- it, the others scaled down; the ones the song lacks stay greyed (the cursor never stops on them)
local DIFF_STEP = 128
local diffView = { haloX = nil, scale = {}, t = nil }
local function drawDiffRow(x0, cy)
    local d = LO.myDiff()
    local dt = diffView.t and (clock - diffView.t) or 0
    diffView.t = clock
    local k = 1 - math.exp(-dt * 14)
    local target = x0 + d * DIFF_STEP
    diffView.haloX = diffView.haloX and (diffView.haloX + (target - diffView.haloX) * k) or target
    local pulse = 0.5 + 0.5 * math.sin(clock * 3)
    sprite("Halo", diffView.haloX, cy, 1.0 + 0.05 * pulse, nil, 0.7 + 0.3 * pulse, GOLD, nil, "add")
    local known = net.song ~= nil and not net.iLackSong
    for i = 0, 4 do
        local has = LO.diffAvailable(i)
        local want = (i == d) and 1.0 or (has and 0.72 or 0.6)
        local s = diffView.scale[i] or want
        s = s + (want - s) * k
        diffView.scale[i] = s
        local x = x0 + i * DIFF_STEP
        if has then drawEmblem(i, known and net.songLevels[i] or nil, net.songPlus[i], x, cy, s, (i == d) and 1 or 0.75)
        else drawEmblem(nil, nil, false, x, cy, s, 0.45) end
    end
end

local function drawDeck()
    sprite("Deck", DECK_X + DECK_W / 2, DECK_Y + DECK_H / 2)
    local tabs = { LO.amController() and L("tab_start") or L("tab_ready"), L("tab_difficulty"), L("tab_mods") }
    for i, tn in ipairs(tabs) do
        local sel = (i == tab)
        local y = DECK_Y + 30 + (i - 1) * 40
        if sel then
            sprite("TabOn", DECK_X + 20 + 190, y)
            sprite("Sparkle", DECK_X + 44, y, 0.3 + 0.05 * math.sin(clock * 5), nil, 1, GOLD, nil, "add")
        end
        text(26, tn, DECK_X + 70, y, sel and CREAM or DIM, "left", 1, DECK_SPLIT - 90)
    end
    local cx = DECK_X + DECK_SPLIT + (DECK_W - DECK_SPLIT) / 2
    local cy = DECK_Y + DECK_H / 2
    local roomW = DECK_W - DECK_SPLIT - 80
    if tab == 2 then
        local d = LO.myDiff()
        local x0 = DECK_X + DECK_SPLIT + 110
        drawDiffRow(x0, cy)
        local tx = x0 + 4 * DIFF_STEP + 96
        local w = DECK_X + DECK_W - 40 - tx
        text(34, LO.diffName(d), tx, cy - 18, DIFF_COL[d] or CREAM, "left", 1, w)
        text(20, L("deck_diff_help"), tx, cy + 26, LAVENDER, "left", 1, w)
        return
    end
    local main, sub, mc = nil, nil, CREAM
    if tab == 1 then
        if LO.amController() then
            local why = LO.startProblem()
            if why == "need_song" then main = LF("deck_need_song", keyLabel("song"))
            else
                main = LF("deck_start", bindLabel("DECIDE"))
                if why == "need_players" then mc, sub = DIM, L("deck_need_players") end
            end
        else
            if LO.myReady() then main, mc = LF("deck_ready_on", bindLabel("CANCEL")), GREEN
            else main = LF("deck_ready_off", bindLabel("DECIDE")) end
        end
    else
        main = LF("deck_mods", bindLabel("DECIDE"))
        sub = L("deck_mods_help")
    end
    if sub then
        text(30, main, cx, cy - 18, mc, "center", 1, roomW)
        text(20, sub, cx, cy + 24, LAVENDER, "center", 1, roomW)
    else
        text(30, main, cx, cy, mc, "center", 1, roomW)
    end
end

local function drawLobby()
    local ids, me = LO.peerIds(), NET:SelfId()
    drawSmallLogo(80, 62)
    sprite("HeaderPill", 1544, 62)
    text(20, LO.amController() and L("role_host") or L("role_guest"), 1292, 62, LAVENDER, "left", 1, 390)
    local count = LO.count() .. " / " .. LO.MAXP
    text(22, count, 1796, 62, CREAM, "right")
    sprite("Sparkle", 1796 - textW(22, count) - 22, 62, 0.26, nil, 1, GOLD, nil, "add")
    sprite("Chart", 1260, 470, 1, 1, 0.8)
    drawSong()
    drawFigure(ids)
    drawRoster(ids, me)
    drawDeck()
    if LO.amController() then drawHint(LF("hint_host", keyLabel("song"), speedLabel(), bindLabel("CANCEL")))
    else drawHint(LF("hint_guest", bindLabel("CANCEL"))) end
    if net.connecting then
        panel(0, 0, SCREEN_W, SCREEN_H, 0.02, 0.03, 0.08, 0.72)
        sprite("Sparkle", 960, 470, 1.3, nil, 1, GOLD, clock * 60, "add")
        text(44, L("connecting"), 960, 570, CREAM, "center")
    end
end

-- ── results ─────────────────────────────────────────────────────────────────────────────────────────
local RANK_SIZE = { 0.5, 0.42, 0.38, 0.35, 0.32 }
local RANK_STAR = { 1.5, 1.15, 1.0, 0.9, 0.85 }
local RESULTS_FIG_Y = { 236, 372, 282, 392, 214 }   -- the figure's full height, under the results table

-- the room's figure under the table: every player's star shines by their rank
local function drawRankFigure(rows)
    local ids = LO.peerIds()
    local rankOf, first = {}, nil
    for i, row in ipairs(rows) do
        if row.id then rankOf[row.id] = i end
        if i == 1 and row.r and not row.r.ab then first = row.id end
    end
    local k, cx, cy = 0.8, 960, 770
    local function at(i) return cx + (SEAT_X[i] - 1270) * k, cy + (RESULTS_FIG_Y[i] - 300) * k end
    for i = 1, 4 do
        local x1, y1 = at(i); local x2, y2 = at(i + 1)
        link(x1, y1, x2, y2, (ids[i] and ids[i + 1]) and 0.6 or 0.18, 26)
    end
    for i = 1, LO.MAXP do
        local x, y = at(i)
        local id = ids[i]
        if id == nil then
            sprite("Sparkle", x, y, 0.4, nil, 0.4, nil, nil, "add")
        else
            local rank = rankOf[id] or LO.MAXP
            star(x, y, SEAT_COL[i], RANK_STAR[rank] or 0.85, rank == 1 and 1 or 0.7, id == first)
            text(20, net.nameByPeer[id] or ("P" .. id), x, y + 50, id == first and GOLD or CREAM, "center", 1, 190)
        end
    end
end

local function drawResults()
    local title = L("results_title")
    text(48, title, 960, 84, CREAM, "center")
    local w = textW(48, title)
    sprite("Sparkle", 960 - w / 2 - 40, 84, 0.4, nil, 1, GOLD, nil, "add")
    sprite("Sparkle", 960 + w / 2 + 40, 84, 0.4, nil, 1, GOLD, nil, "add")
    if net.song then text(26, net.song.title or "", 960, 146, LAVENDER, "center", 1, 1200) end
    local rows = LO.standings()
    for i, row in ipairs(rows) do
        local r = row.r
        local y = 236 + (i - 1) * 76
        local first = (i == 1 and r and not r.ab)
        sprite(first and "ResultRowFirst" or "ResultRow", 960, y)
        sprite("Sparkle", 292, y, RANK_SIZE[i] or 0.32, nil, first and 1 or 0.7, first and GOLD or LAVENDER, nil, "add")
        text(24, i, 328, y, first and GOLD or CREAM, "center")
        text(26, row.name or "?", 370, y, first and GOLD or CREAM, "left", 1, 440)
        if r and not r.ab then
            text(28, string.format("%d", r.sc), 1060, y, CREAM, "right")
            text(20, LF("results_combo", r.ac, r.co), 1110, y, LAVENDER, "left", 1, 330)
            local badge = r.pf and L("badge_perfect") or r.fc and L("badge_fc") or r.cl and L("badge_clear") or L("badge_failed")
            local bc = r.pf and { 255, 220, 120 } or r.fc and { 130, 235, 235 } or r.cl and GREEN or RED
            text(20, badge, 1630, y, bc, "right", 1, 180)
        else
            text(20, r and L("results_aborted") or L("results_playing"), 1110, y, AMBER, "left", 1, 500)
        end
    end
    drawRankFigure(rows)
    if net.resultsReadyT == nil then drawHint(L("hint_results_wait"))
    else drawHint(LF("hint_results_back", bindLabel("DECIDE"))) end
end

-- ── lifecycle ─────────────────────────────────────────────────────────────────────────────────────
function onStart()
    dim = CANVAS:CreateCanvas(2, 2); dim:Clear(255, 255, 255, 255); dim:Upload()
    loadArt()
    -- NOTE: do NOT fetch activities here. At skin load, stages' PropagateOnStart runs BEFORE Activities and
    -- ROActivities are registered (see CSkin.FetchMenusAndModules), so GetActivity/GetROActivity return nil in
    -- onStart. We fetch them in activate() (runs on entry, after everything is loaded) — same as song_select_core.
end

function activate()
    lastTs = 0; INPUT:SetMouseLocked(false)
    LO.resetTexts()
    keyLabels, bindNames = {}, {}
    reloadKeys()
    if not modDlg then modDlg = ACTIVITY:GetActivity("mod_select_dialog") end
    if not modicons then modicons = ROACTIVITY:GetROActivity("modicons"); if modicons then pcall(function() modicons:Activate() end) end end
    Space.open()
    if pendingReturn then                    -- returned from a song
        pendingReturn = false
        if net.online then LO.broadcastResult(); LO.setWatching(true) end
        net.resultsT, net.resultsReadyT = 0, nil
        mode = "results"
    end
end
function deactivate() LO.stopPreview(); if act and mode == "songselect" then act:Deactivate() end; Space.close() end
function afterSongEnum() end
function onDestroy() if modicons then pcall(function() modicons:Deactivate() end) end LO.leave(); Space.close() end

-- ── helpers ────────────────────────────────────────────────────────────────────────────────────────
local function setMsg(m, t) msg = m; msgT = t or 4 end
local function getSignal(result)
    if type(result) == "string" then return result end
    if result == nil then return nil end
    local ok, val = pcall(function() return result[0] end)
    return ok and val or nil
end
local function backToMenu(m) LO.leave(); mode = "menu"; menuSel = 1; if m then setMsg(m, 5) end end
local function playSfx(name) pcall(function() SHARED:GetSharedSound(name):Play() end) end
local function playError() playSfx("Error") end

-- ── host song select (song only — no difficulty prompt; Auto disabled) ────────────────────────────────
-- The song select runs over the lobby's sky and shares the room's song speed through CONFIG.SongSpeed.
local function enterSongSelect()
    if act == nil then act = ACTIVITY:GetActivity("song_select_core") end
    CONFIG.IsAIBattleMode = false
    if net.song and net.song.speed then pcall(function() CONFIG.SongSpeed = net.song.speed end) end
    act:Activate(false, 1, false, true)      -- allowPlayerCount=false, lockedPlayerCount=1, no AI slot, songOnly=true
    CONFIG:SetAutoStatus(0, false)
    mode = "songselect"
end
local function updateSongSelect()
    local sig = getSignal(act:Update())
    if sig == "play" then                    -- a SONG was confirmed (no difficulty step)
        local uid  = SONGMOUNT:ChosenUniqueId()
        local title = LO.resolveTitle(uid) or L("unknown_song")
        local sp = CONFIG.SongSpeed or (net.song and net.song.speed) or 20
        local dy = (net.song and net.song.dyn == 1) or false
        act:Deactivate(); mode = "lobby"
        LO.setSong(uid, title, 0, sp, dy)
        LO.setDiff(LO.myDiff())
    elseif sig == "cancel" then
        act:Deactivate(); mode = "lobby"
        if net.song and CONFIG.SongSpeed ~= net.song.speed then LO.setSpeed(CONFIG.SongSpeed) end
        LO.restoreMedia()                   -- its jacket and preview come back if the song select moved away
    end
end

-- ── update ───────────────────────────────────────────────────────────────────────────────────────
function update(ts)
    local dt = (ts - lastTs) / 1000.0; lastTs = ts
    if dt < 0 then dt = 0 elseif dt > 0.1 then dt = 0.1 end
    if msgT > 0 then msgT = msgT - dt end
    tickLook(dt)

    if net.online or net.connecting then
        LO.drain()
        if net.roomGone then
            if mode == "songselect" and act then act:Deactivate() end
            backToMenu(L("msg_room_closed")); return nil
        end
    end
    if mode == "lobby" or mode == "results" then LO.tickPreview(dt) end   -- loop the jacket preview (song select owns its own)

    local navPn = NavInput.p[1]
    if mode == "menu" then
        if navPn.upOrPadLeft() or navPn.downOrPadRight() then menuSel = (menuSel == 1) and 2 or 1 end
        if navPn.decide() then
            if menuSel == 1 then
                if LO.host() then mode = "lobby"; tab = 1 else playError(); setMsg(net.msg or L("msg_open_failed"), 6) end
            else mode = "code"; ti = INPUT:CreateTextInput("", 4096) end
        end
        if navPn.cancel() then return Exit("stage", "_title") end

    elseif mode == "code" then
        if ti:Update() then
            local code = (ti.Text ~= "" and ti.Text) or nil
            if code and LO.join(code) then mode = "lobby"; tab = 1
            else playError(); setMsg(code and (net.msg or L("msg_join_failed")) or L("msg_no_code"), 5); mode = "menu" end
        end
        if navPn.cancel() then mode = "menu" end

    elseif mode == "songselect" then
        updateSongSelect()

    elseif mode == "lobby" then
        if not net.online then
            if not net.connecting then backToMenu(net.msg or L("msg_disconnected")) end
            return nil
        end
        -- Play start is handled FIRST, even if the mod dialog is open (it is then force-closed WITHOUT saving).
        if net.goSignal then
            net.goSignal = false
            if modDlg and modDlg.IsActive then pcall(function() modDlg:Deactivate() end); modWasActive = false end
            local r = LO.launchPlay()
            if r == true then pendingReturn = true; return Exit("play", nil)
            elseif r == "eject" then backToMenu(L("msg_no_song_eject")); return nil
            else setMsg(net.msg or L("msg_cant_start"), 5) end
        end
        -- mod_select_dialog modal: while open it owns all input (Esc = its Cancel)
        if modDlg and modDlg.IsActive then modDlg:Update(); modWasActive = true; return nil end
        if modWasActive then modWasActive = false; LO.broadcastMods() end
        LO.fixDiff()                                -- a song change can take my difficulty away

        if navPn.up() then tab = (tab - 2) % 3 + 1 end
        if navPn.down() then tab = tab % 3 + 1 end
        local confirm = navPn.decide()
        if tab == 1 then
            if LO.amController() then
                if confirm then
                    local why = LO.startProblem()
                    if why then playError(); setMsg(L("msg_" .. why), 3)
                    elseif LO.hostStart() then setMsg(L("msg_starting"), 2) end
                end
            else
                if confirm then LO.setReady(not LO.myReady()) end
            end
        elseif tab == 2 then
            -- only the difficulties the song has: the cursor steps over the others and stops at the ends
            if navPn.left() and LO.stepDiff(-1) then playSfx("Move") end
            if navPn.right() and LO.stepDiff(1) then playSfx("Move") end
        elseif tab == 3 then
            if confirm then
                if not modDlg then modDlg = ACTIVITY:GetActivity("mod_select_dialog") end   -- lazily (re)fetch if onStart missed it
                if modDlg then modDlg:Activate(0, true) else playError(); setMsg(L("msg_mods_unavailable"), 3) end   -- restrict: no Auto (Dynamic Beat IS allowed, per-player)
            end
        end
        -- controller-only song controls
        if LO.amController() then
            if pressed("song") then enterSongSelect(); return nil end
            if net.song then
                if pressed("speed_down") then LO.adjustSpeed(-1) end
                if pressed("speed_up") then LO.adjustSpeed(1) end
            end
        end
        -- Escape: un-ready if readied (Ready is reversible), otherwise leave the room
        if navPn.cancel() then
            if LO.myReady() then LO.setReady(false) else backToMenu() end
        end

    elseif mode == "results" then
        net.resultsT = net.resultsT + dt
        if net.resultsReadyT == nil and (LO.haveAllResults() or net.resultsT > 20) then net.resultsReadyT = net.resultsT end
        if net.goSignal then        -- a new round started while we lingered on the results → kicked
            net.goSignal = false
            backToMenu(L("msg_kicked_round")); return nil
        end
        -- stay as long as you want; press Enter (once results are in) to return to the lobby
        if net.resultsReadyT ~= nil and navPn.decide() then
            LO.nextRound(); mode = "lobby"; tab = 1
        end
        if navPn.cancel() then backToMenu(); return nil end
    end
    return nil
end

-- ── draw ───────────────────────────────────────────────────────────────────────────────────────────
function draw()
    drawSky()
    if mode == "songselect" then act:Draw("no_bg"); drawToast(); return end
    if mode == "menu" then drawMenu()
    elseif mode == "code" then drawCode()
    elseif mode == "lobby" then drawLobby()
    elseif mode == "results" then drawResults() end
    if mode == "lobby" and modDlg and modDlg.IsActive then modDlg:Draw() end
    drawToast()
end
