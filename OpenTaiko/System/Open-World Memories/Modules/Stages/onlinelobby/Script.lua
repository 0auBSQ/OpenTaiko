---@diagnostic disable: undefined-global, undefined-field, lowercase-global, need-check-nil
-- onlinelobby/Script.lua — a P2P 5-player rhythm lobby. Create/join a room (paste-in code); the lobby AUTHORITY
-- (rotates each round) picks the SONG (no difficulty prompt — the jacket + levels + preview show in the lobby) and
-- sets speed/dynamic-beat; every player picks their own difficulty + mods (real mod_select_dialog) + ready. The
-- host's first menu state is "Start play!" (selectable anytime). On start everyone plays the SAME song; remote
-- players render as real virtual-slot spots whose judges are sampled from the broadcast accuracy (no flying notes),
-- with synced loading/finish + results + host rotation. The session logic is the global Online.Lobby (LO).

local NavInput = require("NavInput")
local LO = require("Online.Lobby")
local PopUI = require("PopUI")
local I18N = require("Locale.i18n")
local T = I18N.texts("lobby")     -- lang/<code>/lobby.json
local net = LO.net
-- positions, sizes and colours: Config/layout.json; timings: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local COL = Layout.colors
local MSG = Animation.message_s
local CX = Layout.center_x

-- the lobby's texts, looked up once per visit (resetTexts on activate); the engine's own words for the
-- difficulty names
local texts = {}
local function L(id)
    local s = texts[id]
    if s == nil then s = T:tr(id); texts[id] = s end
    return s
end
local function LF(id, ...) return string.format(L(id), ...) end
local DIFF_KEYS = { [0] = "DIFF_EASY", [1] = "DIFF_NORMAL", [2] = "DIFF_HARD", [3] = "DIFF_EX", [4] = "DIFF_EXTRA" }
local DIFF_FALLBACK = { [0] = "Easy", [1] = "Normal", [2] = "Hard", [3] = "Extreme", [4] = "Extra" }
local diffNames = {}
local function diffName(d)
    local s = diffNames[d]
    if s == nil then
        local ok, v = pcall(function() return LANG:GetString(DIFF_KEYS[d]) end)
        s = (ok and type(v) == "string" and v ~= "" and v ~= DIFF_KEYS[d]) and v or (DIFF_FALLBACK[d] or "?")
        diffNames[d] = s
    end
    return s
end
local function resetTexts() I18N.detect(); texts, diffNames = {}, {} end

local SCREEN_W, SCREEN_H = 1920, 1080
local mode = "menu"                         -- "menu" | "code" | "lobby" | "songselect" | "results"
local lastTs = nil                          -- nil right after activate
local tab = 1                               -- per-player menu: 1 Ready/Start, 2 Difficulty, 3 Mods
local ti = nil
local act = nil                             -- song_select_core (host)
local modDlg = nil                          -- mod_select_dialog (per-player)
local modWasActive = false
local modicons = nil                        -- modicons ROActivity (draws a player's mod icons)
local pendingReturn = false
local msg, msgT = nil, 0

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

-- ── look: PopUI panels on a flat background ───────────────────────────────────────────────────────
local TEX_NAMES = { "Difficulty/Missing", "Difficulty/Level/plus" }
for d = 0, 4 do TEX_NAMES[#TEX_NAMES + 1] = "Difficulty/" .. d end
for n = 0, 9 do TEX_NAMES[#TEX_NAMES + 1] = "Difficulty/Level/" .. n end
local tex = {}
local ui = nil                               -- the panels and menus of the screen shown
local builtFor = nil                         -- the screen they were built for
local builtHost = false                      -- and whether they were built for the room's controller
local click = {}                             -- what the menus and buttons were asked this frame
local nextBtn = nil                          -- the results' way back to the lobby, for the mouse

local INK, DIM, WHITE = COL.ink, COL.dim, COL.white
local ACCENT, ACCENT_SOFT = COL.accent, COL.accent_soft
local GREEN, RED, AMBER = COL.green, COL.red, COL.amber
local BG = COL.background
local ROW_FACE, ROW_EMPTY = COL.row_face, COL.row_empty
local NONE = COL.text_outline
local TEXT_PAD = 25                          -- a glyph box's side padding (PopUI's)
-- each difficulty's text colour: its emblem's hue, darkened for the light panels
local DIFF_COL = {}
for i, c in ipairs(Layout.difficulty_colors.list) do DIFF_COL[i - 1] = c end

local SONG = Layout.song_panel
local ROSTER = Layout.roster_panel
local ROWS = Layout.rows
local ROW_X, ROW_TOP, ROW_PITCH, ROW_W, ROW_H = ROWS.x, ROWS.top, ROWS.pitch, ROWS.w, ROWS.h
local DECK = Layout.deck
local DECK_X, DECK_Y, DECK_W, DECK_H, DECK_SPLIT = DECK.x, DECK.y, DECK.w, DECK.h, DECK.split

local function loadArt()
    for _, n in ipairs(TEX_NAMES) do tex[n] = TEXTURE:CreateTexture("Textures/" .. n .. ".png") end
end

local function rect(x, y, w, h, c, a) ui:rect(x, y, w, h, c[1], c[2], c[3], a or 255) end

-- text with its ink centred on y; align "left" puts the ink's left edge at x, "right" its right edge
local function text(size, str, x, y, c, align, op, maxW)
    align = align or "left"
    local ax = (align == "left") and (x - TEXT_PAD) or (align == "right") and (x + TEXT_PAD) or x
    ui:drawTextEx(size, tostring(str), ax, y + ui:textNudge(size), c, NONE, op or 1, 1,
        maxW and (maxW + 2 * TEXT_PAD) or 0, align)
end

-- a texture centred on (x, y), scaled and faded
local function sprite(name, x, y, k, op)
    local t = tex[name]
    if t == nil then return end
    t:SetScale(k or 1, k or 1); t:SetOpacity(op or 1)
    t:DrawAtAnchor(x, y, "center")
    t:SetScale(1, 1); t:SetOpacity(1)
end

-- a difficulty emblem centred on (cx, cy) at scale k with its level inside, the digits set like the song
-- select's song info (a plus level's "+" off the last digit's top-right corner); d nil = the empty emblem,
-- level nil = the emblem alone
local EMBLEM = Layout.emblem
local PLUS_DX, PLUS_DY = EMBLEM.plus_dx, EMBLEM.plus_dy
local function drawEmblem(d, level, isPlus, cx, cy, k, op)
    sprite(d ~= nil and ("Difficulty/" .. d) or "Difficulty/Missing", cx, cy, k, op)
    if level == nil then return end
    local s = tostring(level)
    local total, prev, last = 0, 0, 0
    for i = 1, #s do
        local t = tex["Difficulty/Level/" .. s:sub(i, i)]
        if t then
            local w = (t.Width or 0) * k
            if i > 1 then total = total + prev * EMBLEM.digit_advance end
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
            if i > 1 then cursor = cursor + prev * EMBLEM.digit_advance end
            sprite(name, cursor + w / 2, cy, k, op)
            prev = w
        end
    end
    if isPlus then sprite("Difficulty/Level/plus", cx + total / 2 + PLUS_DX * k, cy + PLUS_DY * k, k, op) end
end

-- the panels of each screen; they are built from update when the screen changes (never from draw)
local PANELS = {}
local function prepare()
    local host = (mode == "lobby" and LO.amController()) and true or false
    if ui == nil or (builtFor == mode and builtHost == host) then return end
    ui:disposeWidgets()
    ui:clear()                               -- disposing frees their canvases; the list is emptied here
    builtFor, builtHost = mode, host
    nextBtn = nil
    local build = PANELS[mode]
    if build then build() end
end

-- a message on one line, or wrapped onto as many as it needs when it is too long; the lines are kept
-- until the message changes
local TOAST = Layout.toast
local TOAST_W = TOAST.max_width
local toastOf, toastRows, toastSize = nil, nil, TOAST.font_size
local function toastLines(s)
    if toastOf ~= s then
        toastOf = s
        if ui:gfont(TOAST.font_size):Measure(s) <= TOAST_W then toastRows, toastSize = { s }, TOAST.font_size
        else
            toastRows, toastSize = ui:wrapLines(TOAST.wrap_font_size, s, TOAST_W), TOAST.wrap_font_size
            if #toastRows == 0 then toastRows = { s } end
        end
    end
    return toastRows, toastSize
end

-- the bar keeps its bottom edge and grows upwards with the lines
local function drawToast()
    if not (msgT and msgT > 0 and msg) then return end
    local op = math.min(1, msgT / Animation.toast_fade_s)
    local lines, size = toastLines(msg)
    local h = math.max(TOAST.min_height, TOAST.pad_height + #lines * TOAST.line_height)
    local top = TOAST.bottom - h
    rect(CX - TOAST.width / 2, top, TOAST.width, h, INK, TOAST.opacity * op)
    if #lines == 1 then text(size, lines[1], CX, TOAST.single_line_y, WHITE, "center", op, TOAST_W)
    else
        for i, line in ipairs(lines) do text(size, line, CX, top + TOAST.first_line_dy + (i - 1) * TOAST.line_height, WHITE, "center", op, TOAST_W) end
    end
end

local HINT = Layout.hint
local function drawHint(s) text(HINT.font_size, s, CX, HINT.y, DIM, "center", 1, HINT.max_width) end

-- ── menu / code ─────────────────────────────────────────────────────────────────────────────────────
local MENU = Layout.menu
local MENU_ROW = MENU.row_height

-- the way back at the top left of every screen, for the mouse; the keys use the cancel button
local function backButton()
    local B = Layout.back_button
    ui:button{ icon = "back", x = B.x, y = B.y, w = B.w, h = B.h, mouseOnly = true, onClick = function() click.back = true end }
end

-- a menu whose cursor goes round from one end to the other
local function wrapping(o)
    o.onNavDownOrPadRight = function(m) m:setSelected(m.selected % #m.items + 1); return true end
    o.onNavUpOrPadLeft = function(m) m:setSelected((m.selected - 2) % #m.items + 1); return true end
    return o
end

PANELS.menu = function()
    ui:panel{ x = MENU.x, y = MENU.y, w = MENU.w, h = MENU.h, pad = MENU.pad, title = L("logo_title") }
    ui:menu(wrapping{ x = CX - MENU.list.w / 2, y = MENU.y + MENU.list.y_offset, w = MENU.list.w, h = 2 * MENU_ROW, rowHeight = MENU_ROW,
        items = { L("menu_create"), L("menu_join") }, selected = 1, onSelect = function(i) click.pick = i end })
    backButton()
    ui:focusStay()
end

local function drawMenu()
    if builtFor ~= "menu" then return end
    ui:draw()
    text(MENU.subtitle.font_size, L("menu_subtitle"), CX, MENU.y + MENU.subtitle.y_offset, DIM, "center", 1, MENU.w - MENU.subtitle.inset)
    drawHint(LF("hint_menu", bindLabel("DECIDE"), bindLabel("CANCEL")))
end

local CODE = Layout.code

PANELS.code = function()
    ui:panel{ x = CODE.x, y = CODE.y, w = CODE.w, h = CODE.h, pad = CODE.pad, title = L("code_title") }
    backButton()
end

local function drawCode()
    if builtFor ~= "code" then return end
    ui:draw()
    local P, FB, F, TX = CODE.prompt, CODE.field_border, CODE.field, CODE.text
    text(P.font_size, L("code_prompt"), CX, CODE.y + P.y_offset, DIM, "center", 1, CODE.w - P.inset)
    rect(CX - FB.w / 2, CODE.y + FB.y_offset, FB.w, FB.h, INK)
    rect(CX - F.w / 2, CODE.y + F.y_offset, F.w, F.h, WHITE)
    local shown = (ti and ti.DisplayText) or ""
    if #shown > CODE.max_chars then shown = "..." .. string.sub(shown, #shown - (CODE.max_chars - 1)) end
    text(TX.font_size, shown .. " ", CX, CODE.y + TX.y_offset, INK, "center", 1, TX.max_width)
    drawHint(LF("hint_code", bindLabel("CANCEL")))
end

-- ── lobby ───────────────────────────────────────────────────────────────────────────────────────────
local JK = Layout.jacket
local JK_X, JK_Y, JK_S = JK.x, JK.y, JK.size        -- the jacket
local SI = Layout.song_info

-- the song speed: its multiplier, pink when faster and blue when slower
local SPEED_FAST, SPEED_SLOW = COL.speed_fast, COL.speed_slow
local function drawSpeed(x, labelY, valueY)
    local mult = net.song.speed and CONFIG.SONGSPEED:ToActual(net.song.speed) or 1
    local changed = math.abs(mult - 1) > 1e-4
    local col = not changed and INK or (mult > 1 and SPEED_FAST or SPEED_SLOW)
    text(SI.speed.label_font_size, L("song_speed_label"), x, labelY, col, "center")
    text(SI.speed.value_font_size, string.format("x%.2f", mult), x, valueY, col, "center")
end

local function drawSong()
    rect(JK_X - JK.border, JK_Y - JK.border, JK_S + 2 * JK.border, JK_S + 2 * JK.border, INK)
    rect(JK_X, JK_Y, JK_S, JK_S, ROW_EMPTY)
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
        text(JK.missing_font_size, L("song_not_found"), JK_X + JK_S / 2, JK_Y + JK_S / 2, RED, "center", 1, JK_S - JK.missing_inset)
    end
    if net.song then
        text(SI.title.font_size, net.song.title or "?", SI.center_x, SI.title.y, INK, "center", 1, SI.max_width)
        if net.songSubtitle and net.songSubtitle ~= "" then text(SI.subtitle.font_size, net.songSubtitle, SI.center_x, SI.subtitle.y, DIM, "center", 1, SI.max_width) end
        drawSpeed(SI.center_x, SI.speed.label_y, SI.speed.value_y)
        -- every difficulty's emblem with its level; the ones this chart lacks show the empty emblem
        local E = SI.emblems
        for d = 0, 4 do
            local lv = net.songLevels[d]
            drawEmblem(lv ~= nil and d or nil, lv, net.songPlus[d], E.x + d * E.step, E.y, E.scale, lv ~= nil and 1 or E.missing_opacity)
        end
    else
        text(SI.wait.font_size, LO.amController() and LF("song_pick", keyLabel("song")) or L("song_wait"), SI.center_x, SI.wait.y, AMBER, "center", 1, SI.max_width)
    end
end

local function seatStatus(id)
    if net.lackByPeer[id] then return L("status_no_song"), RED end
    if net.readyByPeer[id] then return L("status_ready"), GREEN end
    if net.watchByPeer[id] then return L("status_watching"), AMBER end
    return L("status_not_ready"), DIM
end

-- the roster: a row per seat with the player's nameplate, difficulty, mods and status
local function drawRoster(ids, me)
    local cx = ROW_X + ROW_W / 2
    for i = 1, LO.MAXP do
        local id = ids[i]
        local top = ROW_TOP + (i - 1) * ROW_PITCH
        local cy = top + ROW_H / 2
        if id == nil then
            rect(ROW_X, top, ROW_W, ROW_H, ROW_EMPTY)
            text(ROWS.waiting.font_size, L("seat_waiting"), cx, cy, DIM, "center", 1, ROWS.waiting.max_width)
        else
            local mine, host = (id == me), (NET:HostRoleId() == id)
            rect(ROW_X, top, ROW_W, ROW_H, mine and ACCENT_SOFT or ROW_FACE)
            if mine then rect(ROW_X, top, ROWS.marker_width, ROW_H, ACCENT) end
            local spot = LO.spotOf[id]
            local drawn = false
            if spot ~= nil then
                drawn = pcall(function() NAMEPLATE:DrawPlayerNameplate(ROW_X + ROWS.nameplate_dx, math.floor(cy + ROWS.nameplate_dy), 255, spot) end)
            end
            if not drawn then text(ROWS.name.font_size, net.nameByPeer[id] or ("P" .. id), ROW_X + ROWS.name.dx, cy, INK, "left", 1, ROWS.name.max_width) end
            local d = net.diffByPeer[id] or 1
            local lv = (net.song ~= nil and not net.iLackSong) and net.songLevels[d] or nil
            drawEmblem(d, lv, net.songPlus[d], ROW_X + ROWS.emblem.dx, cy, ROWS.emblem.scale, 1)
            -- mod icons (real icons): self uses CONFIG[0], others use a scratch slot
            if modicons then
                pcall(function()
                    if mine then modicons:Draw(ROW_X + ROWS.mods.dx, cy + ROWS.mods.dy, 0, "menu", 255)
                    else
                        -- the scratch slot is the local 5th player's: their special mod is put back after the draw
                        local keep = CONFIG:GetSpecialMod(4)
                        LO.applyModsToSlot(4, net.modByPeer[id]); modicons:Draw(ROW_X + ROWS.mods.dx, cy + ROWS.mods.dy, 4, "menu", 255)
                        CONFIG:SetSpecialMod(4, (keep ~= 1) and keep or 0)
                    end
                end)
            end
            local st, sc = seatStatus(id)
            local S = ROWS.status
            local right = ROW_X + ROW_W - S.right_inset
            if host then
                text(S.font_size, st, right, cy + S.host_dy, sc, "right", 1, S.max_width)
                text(ROWS.host_tag.font_size, L("tag_host"), right, cy + ROWS.host_tag.dy, ACCENT, "right", 1, S.max_width)
            else
                text(S.font_size, st, right, cy, sc, "right", 1, S.max_width)
            end
        end
    end
end

-- the difficulty row of the deck: all five emblems, the chosen one full size on a marker, the others
-- scaled down; the ones the song lacks stay greyed (the cursor never stops on them)
local DR = Layout.difficulty_row
local DIFF_STEP = DR.step
local DIFF_X0, DIFF_CY = DECK_X + DECK_SPLIT + DR.x_offset, DECK_Y + DECK_H / 2
local DECK_ROW = DECK.row_height
local SPEED_Y = Layout.speed_buttons.y        -- the host's song speed buttons
local function drawDiffRow(x0, cy)
    local d = LO.myDiff()
    rect(x0 + d * DIFF_STEP - DR.marker.w / 2, cy - DR.marker.h / 2, DR.marker.w, DR.marker.h, ACCENT_SOFT)
    local known = net.song ~= nil and not net.iLackSong
    for i = 0, 4 do
        local has = LO.diffAvailable(i)
        local s = (i == d) and DR.chosen_scale or (has and DR.other_scale or DR.missing_scale)
        local x = x0 + i * DIFF_STEP
        if has then drawEmblem(i, known and net.songLevels[i] or nil, net.songPlus[i], x, cy, s, (i == d) and DR.chosen_opacity or DR.other_opacity)
        else drawEmblem(nil, nil, false, x, cy, s, DR.missing_opacity) end
    end
end

local function drawDeck()
    rect(DECK_X + DECK_SPLIT, DECK_Y + DECK.divider_inset, DECK.divider_width, DECK_H - 2 * DECK.divider_inset, ROW_EMPTY)
    local cx = DECK_X + DECK_SPLIT + (DECK_W - DECK_SPLIT) / 2
    local cy = DECK_Y + DECK_H / 2
    local roomW = DECK_W - DECK_SPLIT - DECK.content_inset
    if tab == 2 then
        local d = LO.myDiff()
        local x0 = DIFF_X0
        drawDiffRow(x0, cy)
        local tx = x0 + 4 * DIFF_STEP + DR.name_gap
        local w = DECK_X + DECK_W - DR.right_inset - tx
        text(DR.name.font_size, diffName(d), tx, cy + DR.name.dy, DIFF_COL[d] or INK, "left", 1, w)
        text(DR.help.font_size, L("deck_diff_help"), tx, cy + DR.help.dy, DIM, "left", 1, w)
        return
    end
    local main, sub, mc = nil, nil, INK
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
        text(DECK.main_font_size, main, cx, cy + DECK.main_dy, mc, "center", 1, roomW)
        text(DECK.sub_font_size, sub, cx, cy + DECK.sub_dy, DIM, "center", 1, roomW)
    else
        text(DECK.main_font_size, main, cx, cy, mc, "center", 1, roomW)
    end
end

PANELS.lobby = function()
    ui:panel{ x = SONG.x, y = SONG.y, w = SONG.w, h = SONG.h, pad = SONG.pad, title = L("song_label") }
    ui:panel{ x = ROSTER.x, y = ROSTER.y, w = ROSTER.w, h = ROSTER.h, pad = ROSTER.pad }
    ui:panel{ x = DECK_X, y = DECK_Y, w = DECK_W, h = DECK_H, pad = DECK.pad }
    -- the deck's menu: choosing a row confirms it, Left / Right change the difficulty on its row
    ui:menu(wrapping{ x = DECK_X + DECK.menu_dx, y = DECK_Y + (DECK_H - 3 * DECK_ROW) / 2, w = DECK_SPLIT - DECK.menu_w_inset, h = 3 * DECK_ROW,
        rowHeight = DECK_ROW, selected = tab,
        items = { builtHost and L("tab_start") or L("tab_ready"), L("tab_difficulty"), L("tab_mods") },
        onChange = function(i) tab = i end, onSelect = function() click.confirm = true end,
        onNavLeft = function() click.diff = -1; return true end,
        onNavRight = function() click.diff = 1; return true end })
    backButton()
    if builtHost then
        local SB = Layout.speed_buttons
        ui:button{ icon = "rewind", x = SI.center_x + SB.rewind_dx, y = SPEED_Y, w = SB.w, h = SB.h, mouseOnly = true, onClick = function() click.speed = -1 end }
        ui:button{ icon = "forward", x = SI.center_x + SB.forward_dx, y = SPEED_Y, w = SB.w, h = SB.h, mouseOnly = true, onClick = function() click.speed = 1 end }
    end
    ui:focusStay()
end

local function drawLobby()
    local ids, me = LO.peerIds(), NET:SelfId()
    if builtFor ~= "lobby" then return end
    ui:draw()
    local H = Layout.header
    text(H.title.font_size, L("logo_title"), H.title.x, H.title.y, INK, "left", 1, H.title.max_width)
    text(H.role.font_size, LO.amController() and L("role_host") or L("role_guest"), H.role.x, H.role.y, DIM, "right", 1, H.role.max_width)
    text(H.count.font_size, LO.count() .. " / " .. LO.MAXP, H.count.x, H.count.y, INK, "right")
    drawSong()
    drawRoster(ids, me)
    drawDeck()
    if LO.amController() then drawHint(LF("hint_host", keyLabel("song"), speedLabel(), bindLabel("CANCEL")))
    else drawHint(LF("hint_guest", bindLabel("CANCEL"))) end
    if net.connecting then
        local C = Layout.connecting
        rect(0, 0, SCREEN_W, SCREEN_H, BG, C.overlay_opacity)
        text(C.title.font_size, L("connecting"), CX, C.title.y, INK, "center")
        text(C.hint.font_size, LF("hint_connecting", bindLabel("CANCEL")), CX, C.hint.y, DIM, "center", 1, C.hint.max_width)
    end
end

-- ── results ─────────────────────────────────────────────────────────────────────────────────────────
local RESULTS = Layout.results

PANELS.results = function()
    ui:panel{ x = RESULTS.x, y = RESULTS.y, w = RESULTS.w, h = RESULTS.h, pad = RESULTS.pad, title = L("results_title") }
    local NB = RESULTS.next_button
    nextBtn = ui:button{ icon = "play", accent = true, x = CX - NB.w / 2, y = RESULTS.y + RESULTS.h + NB.gap, w = NB.w, h = NB.h,
        mouseOnly = true, visible = false, onClick = function() click.next = true end }
    backButton()
end

local function drawResults()
    if builtFor ~= "results" then return end
    ui:draw()
    local R = RESULTS
    if net.song then text(R.title.font_size, net.song.title or "", CX, R.y + R.title.y_offset, DIM, "center", 1, R.title.max_width) end
    local rows = LO.standings()
    local RR = R.rows
    for i, row in ipairs(rows) do
        local r = row.r
        local y = R.y + RR.top + (i - 1) * RR.pitch
        local first = (i == 1 and r and not r.ab)
        rect(R.x + RR.inset, y - RR.h / 2, R.w - 2 * RR.inset, RR.h, first and ACCENT_SOFT or ROW_FACE)
        if first then rect(R.x + RR.inset, y - RR.h / 2, RR.marker_width, RR.h, ACCENT) end
        text(R.rank.font_size, i, R.x + R.rank.dx, y, first and ACCENT or INK, "center")
        text(R.name.font_size, row.name or "?", R.x + R.name.dx, y, INK, "left", 1, R.name.max_width)
        if r and not r.ab then
            text(R.score.font_size, string.format("%d", r.sc), R.x + R.score.dx, y, INK, "right")
            text(R.combo.font_size, LF("results_combo", r.ac, r.co), R.x + R.combo.dx, y, DIM, "left", 1, R.combo.max_width)
            local badge = r.pf and L("badge_perfect") or r.fc and L("badge_fc") or r.cl and L("badge_clear") or L("badge_failed")
            local bc = r.pf and AMBER or r.fc and COL.badge_full_combo or r.cl and GREEN or RED
            text(R.badge.font_size, badge, R.x + R.w - R.badge.right_inset, y, bc, "right", 1, R.badge.max_width)
        else
            text(R.status.font_size, r and L("results_aborted") or L("results_playing"), R.x + R.status.dx, y, AMBER, "left", 1, R.status.max_width)
        end
    end
    if net.resultsReadyT == nil then drawHint(L("hint_results_wait"))
    else drawHint(LF("hint_results_back", bindLabel("DECIDE"))) end
end

-- ── lifecycle ─────────────────────────────────────────────────────────────────────────────────────
function onStart()
    loadArt()
    -- NOTE: do NOT fetch activities here. At skin load, stages' PropagateOnStart runs BEFORE Activities and
    -- ROActivities are registered (see CSkin.FetchMenusAndModules), so GetActivity/GetROActivity return nil in
    -- onStart. We fetch them in activate() (runs on entry, after everything is loaded) — same as song_select_core.
end

function activate()
    lastTs = nil; INPUT:SetMouseLocked(false)
    resetTexts()
    keyLabels, bindNames = {}, {}
    reloadKeys()
    if ui == nil then ui = PopUI.new{} end
    builtFor = nil
    if not modDlg then modDlg = ACTIVITY:GetActivity("mod_select_dialog") end
    if not modicons then modicons = ROACTIVITY:GetROActivity("modicons"); if modicons then pcall(function() modicons:Activate() end) end end
    if pendingReturn then                    -- returned from a song
        pendingReturn = false
        LO.returnedFromPlay()
        mode = "results"
    end
end
function deactivate()
    LO.stopPreview()
    if act and mode == "songselect" then act:Deactivate() end
    if ui then ui:disposeWidgets(); ui:clear() end
    builtFor = nil
end
function afterSongEnum() end
function onDestroy() if modicons then pcall(function() modicons:Deactivate() end) end LO.leave() end

-- ── helpers ────────────────────────────────────────────────────────────────────────────────────────
local function setMsg(m, t) msg = m; msgT = t or MSG.default end
local function getSignal(result)
    if type(result) == "string" then return result end
    if result == nil then return nil end
    local ok, val = pcall(function() return result[0] end)
    return ok and val or nil
end
local function backToMenu(m, t) LO.leave(); mode = "menu"; if m then setMsg(m, t or MSG.back_to_menu) end end
local function playSfx(name) pcall(function() SHARED:GetSharedSound(name):Play() end) end
local function playError() playSfx("Error") end

-- the screen's menus and buttons take the keys and the mouse; what they were asked is left in click
local function widgets(ts)
    if builtFor == mode then ui:update(ts) end
end

-- a left click inside a box this frame, while the mouse is the device in use
local function clickedIn(x, y, w, h)
    if not (ui:mouseActive() and INPUT:MousePressed("Left")) then return false end
    local mx, my = INPUT:GetMouseXY()
    return mx >= x and mx < x + w and my >= y and my < y + h
end

-- ── host song select (song only — no difficulty prompt; Auto disabled) ────────────────────────────────
-- The song select shares the room's song speed through CONFIG.SongSpeed.
local function enterSongSelect()
    if act == nil then act = ACTIVITY:GetActivity("song_select_core") end
    -- allowPlayerCount=false, lockedPlayerCount=1, no AI slot, songOnly=true
    LO.openSongSelect(function() act:Activate(false, 1, false, true) end)
    mode = "songselect"
end
local function updateSongSelect()
    local sig = getSignal(act:Update())
    if sig == "play" then                    -- a SONG was confirmed (no difficulty step)
        act:Deactivate(); mode = "lobby"
        LO.songPicked()
    elseif sig == "cancel" then
        act:Deactivate(); mode = "lobby"
        LO.songPickCancelled()              -- its jacket and preview come back if the song select moved away
    end
end

-- ── update ───────────────────────────────────────────────────────────────────────────────────────
local function step(ts)
    -- first frame: the real frame time
    local dt
    if lastTs == nil then
        local ok, d = pcall(function() return fps.deltaTime end)
        dt = ok and tonumber(d) or 0
    else
        dt = (ts - lastTs) / 1000.0
    end
    lastTs = ts
    if dt < 0 then dt = 0 elseif dt > 0.1 then dt = 0.1 end
    if msgT > 0 then msgT = msgT - dt end

    if net.online or net.connecting then
        LO.drain()
        if net.roomGone then
            if mode == "songselect" and act then act:Deactivate() end
            backToMenu(L("msg_room_closed")); return nil
        end
        if net.notice then setMsg(net.notice, MSG.network_notice); net.notice = nil end
    end
    if mode == "lobby" or mode == "results" then LO.tickPreview(dt) end   -- loop the jacket preview (song select owns its own)

    local navPn = NavInput.p[1]
    click.back, click.pick, click.confirm, click.diff, click.speed, click.next = nil, nil, nil, nil, nil, nil
    if mode == "menu" then
        widgets(ts)
        if click.pick == 1 then
            if LO.host() then mode = "lobby"; tab = 1 else playError(); setMsg(net.msg or L("msg_open_failed"), MSG.open_failed) end
        elseif click.pick == 2 then mode = "code"; ti = INPUT:CreateTextInput("", 4096) end
        if navPn.cancel() or click.back then return Exit("stage", "_title") end

    elseif mode == "code" then
        widgets(ts)
        if ti:Update() then
            local code = (ti.Text ~= "" and ti.Text) or nil
            if code and LO.join(code) then mode = "lobby"; tab = 1
            else playError(); setMsg(code and (net.msg or L("msg_join_failed")) or L("msg_no_code"), code and MSG.join_failed or MSG.no_code); mode = "menu" end
        end
        if navPn.cancel() or click.back then mode = "menu" end

    elseif mode == "songselect" then
        updateSongSelect()

    elseif mode == "lobby" then
        if not net.online then
            -- a failed join comes back with its reason, shown long enough to read; Cancel gives up the wait
            if not net.connecting then backToMenu(net.msg or L("msg_disconnected"), MSG.disconnected)
            elseif navPn.cancel() then backToMenu() end
            return nil
        end
        -- Play start is handled FIRST, even if the mod dialog is open (it is then force-closed WITHOUT saving).
        if LO.takeGo() then
            if modDlg and modDlg.IsActive then pcall(function() modDlg:Deactivate() end); modWasActive = false end
            local r = LO.launchPlay()
            if r == true then pendingReturn = true; return Exit("play", nil)
            elseif r == "eject" then backToMenu(L("msg_no_song_eject")); return nil
            else setMsg(net.msg or L("msg_cant_start"), MSG.cant_start) end
        end
        -- mod_select_dialog modal: while open it owns all input (Esc = its Cancel)
        if modDlg and modDlg.IsActive then modDlg:Update(); modWasActive = true; return nil end
        if modWasActive then modWasActive = false; LO.broadcastMods() end
        LO.fixDiff()                                -- a song change can take my difficulty away

        widgets(ts)
        local confirm = click.confirm
        if tab == 1 then
            if LO.amController() then
                if confirm then
                    local why = LO.startProblem()
                    if why then playError(); setMsg(L("msg_" .. why), MSG.start_problem)
                    elseif LO.hostStart() then setMsg(L("msg_starting"), MSG.starting) end
                end
            else
                if confirm then LO.setReady(not LO.myReady()) end
            end
        elseif tab == 2 then
            -- only the difficulties the song has: the cursor steps over the others and stops at the ends
            if click.diff and LO.stepDiff(click.diff) then playSfx("Move") end
            -- the mouse picks an emblem directly
            for d = 0, 4 do
                if clickedIn(DIFF_X0 + d * DIFF_STEP - DR.marker.w / 2, DIFF_CY - DR.marker.h / 2, DR.marker.w, DR.marker.h) and LO.diffAvailable(d) and d ~= LO.myDiff() then
                    LO.setDiff(d); playSfx("Move")
                end
            end
        elseif tab == 3 then
            if confirm then
                if not modDlg then modDlg = ACTIVITY:GetActivity("mod_select_dialog") end   -- lazily (re)fetch if onStart missed it
                if modDlg then modDlg:Activate(0, true) else playError(); setMsg(L("msg_mods_unavailable"), MSG.mods_unavailable) end   -- restrict: no Auto (Dynamic Beat IS allowed, per-player)
            end
        end
        -- controller-only song controls
        if LO.amController() then
            -- the mouse opens the song select from the jacket
            if pressed("song") or clickedIn(JK_X, JK_Y, JK_S, JK_S) then enterSongSelect(); return nil end
            if net.song then
                if pressed("speed_down") or click.speed == -1 then LO.adjustSpeed(-1) end
                if pressed("speed_up") or click.speed == 1 then LO.adjustSpeed(1) end
            end
        end
        -- Escape: un-ready if readied (Ready is reversible), otherwise leave the room
        if navPn.cancel() or click.back then
            if LO.myReady() then LO.setReady(false) else backToMenu() end
        end

    elseif mode == "results" then
        LO.tickResults(dt)
        if LO.takeGo() then         -- a new round started while we lingered on the results → kicked
            backToMenu(L("msg_kicked_round")); return nil
        end
        -- stay as long as you want; press Enter (once results are in) to return to the lobby
        if nextBtn then nextBtn.visible = net.resultsReadyT ~= nil end
        widgets(ts)
        if net.resultsReadyT ~= nil and (navPn.decide() or click.next) then
            LO.nextRound(); mode = "lobby"; tab = 1
        end
        if navPn.cancel() or click.back then backToMenu(); return nil end
    end
    return nil
end

function update(ts)
    local r = step(ts)
    prepare()
    return r
end

-- ── draw ───────────────────────────────────────────────────────────────────────────────────────────
function draw()
    if ui == nil then return end
    if mode == "songselect" then act:Draw(); drawToast(); return end
    rect(0, 0, SCREEN_W, SCREEN_H, BG)
    if mode == "menu" then drawMenu()
    elseif mode == "code" then drawCode()
    elseif mode == "lobby" then drawLobby()
    elseif mode == "results" then drawResults() end
    if mode == "lobby" and modDlg and modDlg.IsActive then modDlg:Draw() end
    drawToast()
end
