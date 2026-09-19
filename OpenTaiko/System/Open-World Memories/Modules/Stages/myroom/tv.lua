---@diagnostic disable: undefined-global, undefined-field, lowercase-global
-- tv.lua — the TV furniture (My Room).
--
-- The set itself: each placed TV is on or off (it.tvOn, persisted with the room). An on set routes
-- its screen material to an unlit part (room.lua, catalog screenPart) whose flat colour drifts through
-- a small palette, and the same colour is cast into the room as a live point light. The screen ignores
-- the sun and the shadow map, so it reads as bright without any global illumination.
--
-- The window: Enter on the TV opens a small menu (turn on/off, rewatch cutscenes). The cutscene
-- browser has one tab, Song cutscenes: the left list holds every genre folder with a song that has
-- a cutscene, the right list that genre's cutscenes ([song] · Intro / Outro n). Undiscovered ones are
-- locked rows with a padlock; a locked song shows ??? for its title. Cutscenes are read from the
-- songs' .CUTSCENE_* chart headers and their discovered state from the save's triggers; a discovered
-- one plays in the video_player ROActivity, with the jukebox ducked meanwhile.

local PopUI = require("PopUI")
local I18N = require("i18n")
local T = I18N.texts("tv")     -- lang/<code>/tv.json

local TV = {}

-- ── layout (the jukebox window's frame) ────────────────────────────────────────────────────────
local PANEL = { x = 160, y = 90, w = 1600, h = 900 }
local TAB   = { x = PANEL.x + 40, y = PANEL.y + 88, w = 300, h = 64 }
local LEFT  = { x = PANEL.x + 40, y = PANEL.y + 190, w = 560, h = 620, row = 64 }
local RIGHT = { x = PANEL.x + 640, y = PANEL.y + 190, w = PANEL.w - 640 - 50, h = 620, row = 64 }
local MENU_W, MENU_H, MENU_ROW = 620, 320, 72

-- ── the screen: palette, glow, light ───────────────────────────────────────────────────────────
local PALETTE = {
    { 0.55, 0.70, 1.00 },   -- cool blue-white
    { 1.00, 0.85, 0.60 },   -- warm
    { 0.45, 0.95, 0.85 },   -- cyan
    { 1.00, 0.55, 0.75 },   -- pink
    { 0.95, 0.60, 0.30 },   -- orange
    { 0.60, 0.95, 0.55 },   -- green
    { 0.85, 0.85, 0.95 },   -- white
}
TV.SCREEN_EMISSIVE = 0.7    -- the unlit screen's add over its flat colour (room.lua routes with it)
local LIGHT_INTENSITY, LIGHT_RANGE = 0.5, 4.0

local ctx = nil             -- Script.lua closures (see TV.init)
local anim = {}             -- furniture entry → { r,g,b, tr,tg,tb, hold }
local lightDefs = {}        -- furniture entry → the live light def handed to daynight

local function tr(id) return T:tr(id) end

function TV.isOn(it) return it ~= nil and it.tvOn == true end

-- the routed screen part object of a placed TV, or nil (off, no model, or not built yet)
function TV.screenObj(it)
    if ctx == nil then return nil end
    local inst = ctx.propInstFor(it)
    local cat = ctx.catalog(it.id)
    if inst == nil or inst.parts == nil or cat == nil or cat.screenPart == nil then return nil end
    for _, p in ipairs(inst.parts) do
        if p.material == cat.screenPart then return p.obj end
    end
    return nil
end

local function pickTarget(a)
    local c = PALETTE[math.random(#PALETTE)]
    a.tr, a.tg, a.tb = c[1], c[2], c[3]
    a.hold = 0.4 + math.random() * 0.9
end

local function animFor(it)
    local a = anim[it]
    if a == nil then
        local c = PALETTE[1]
        a = { r = c[1], g = c[2], b = c[3] }
        pickTarget(a)
        anim[it] = a
    end
    return a
end

-- the light defs of every on TV; Script.rebuild appends them to the room's light list. They are
-- live: the tick writes the colour into these same tables and daynight re-reads them every frame.
function TV.lights(world)
    local out = {}
    lightDefs = {}
    if ctx == nil then return out end
    for _, it in ipairs(ctx.room().furniture or {}) do
        if it.id == "tv" and it.tvOn then
            local inst = ctx.propInstFor(it)
            if inst then
                local a = animFor(it)
                local yaw = math.rad(inst.yaw or 0)
                local s = inst.scale or 1
                local def = { x = inst.x + 0.45 * math.sin(yaw), y = inst.y + 0.35 * s, z = inst.z + 0.45 * math.cos(yaw),
                              r = a.r, g = a.g, b = a.b, intensity = LIGHT_INTENSITY, range = LIGHT_RANGE, live = true }
                lightDefs[it] = def
                out[#out + 1] = def
            end
        end
    end
    return out
end

-- every frame: drift each on screen's colour and push it to the part and its light
function TV.tick(dt)
    if ctx == nil then return end
    local scene = ctx.scene()
    for _, it in ipairs(ctx.room().furniture or {}) do
        if it.id == "tv" and it.tvOn then
            local a = animFor(it)
            a.hold = a.hold - dt
            if a.hold <= 0 then pickTarget(a) end
            local k = 1 - math.exp(-dt * 5)
            a.r = a.r + (a.tr - a.r) * k
            a.g = a.g + (a.tg - a.g) * k
            a.b = a.b + (a.tb - a.b) * k
            local obj = TV.screenObj(it)
            if obj and scene and scene.ObjSetColor then
                pcall(function() scene:ObjSetColor(obj, a.r, a.g, a.b) end)
            end
            local def = lightDefs[it]
            if def then def.r, def.g, def.b = a.r, a.g, a.b end
        end
    end
end

-- ── window state ───────────────────────────────────────────────────────────────────────────────
local mode = nil            -- nil | "menu" | "browser" | "player"
local ui = nil
local W = {}
local openItem = nil        -- the TV the window was opened for
local justOpened = false    -- eat the Enter edge that opened the window
local player = nil          -- the video_player ROActivity while a cutscene plays
local grouped = nil         -- { { genre=, songs={ { title=, locked=, cuts={...} } } }, ... }
local curGenre = nil

local function playCancel() pcall(function() SHARED:GetSharedSound("Cancel"):Play() end) end

local function songsReady()
    if IsSongsEnumDone and IsSongsEnumDone() then return true end
    return false
end

-- ── a song's cutscenes, read from its chart headers ────────────────────────────────────────────
-- .CUTSCENE_INTRO:<file>,<repeat>  and  .CUTSCENE_OUTRO:<file>,<clear status>,<scope>,<repeat>,...
-- are custom (dot) commands, so they come through GetCustomCommand; the file sits in the chart's
-- folder. A cutscene counts as discovered when the save carries the trigger the cutscene stage sets
-- when it plays (.seencutscene_), or one of the ones it sets when its condition is spent or met. An
-- intro plays at every launch, so a song this save has played to its results also counts for it.
local INTRO_CMD, OUTRO_CMD = ".CUTSCENE_INTRO", ".CUTSCENE_OUTRO"

-- the engine's comma split: "\," is a literal comma, nothing is trimmed
local function splitComma(s)
    local out, cur, i = {}, {}, 1
    while i <= #s do
        local ch = s:sub(i, i)
        if ch == "\\" and s:sub(i + 1, i + 1) == "," then cur[#cur + 1] = ","; i = i + 2
        elseif ch == "," then out[#out + 1] = table.concat(cur); cur = {}; i = i + 1
        else cur[#cur + 1] = ch; i = i + 1 end
    end
    if #s > 0 then out[#out + 1] = table.concat(cur) end
    return out
end

local function fileNameOf(path) return path:match("([^/\\]+)$") or path end

-- the song's folder and whether this save has played any of its charts
local function songInfo(n, saveIndex)
    local folder, played = nil, false
    for d = 0, 6 do
        local c = n:GetChart(d)
        if c ~= nil then
            if folder == nil then
                local f = c.SongFolder or ""
                if f ~= "" then folder = f end
            end
            if not played then
                local bs = c:GetPlayerBestScore(saveIndex)
                if bs ~= nil and bs.HasBeenPlayed == true then played = true end
            end
        end
    end
    return folder, played
end

local function hasCutScenes(n)
    local a, b = n:GetCustomCommand(INTRO_CMD), n:GetCustomCommand(OUTRO_CMD)
    return (a ~= nil and a ~= "") or (b ~= nil and b ~= "")
end

local function discovered(sf, uid, modeName, file)
    if sf == nil then return false end
    local function has(prefix)
        local name = (prefix .. uid .. "_" .. modeName .. "_" .. file):gsub("'", "''")
        return sf:GetGlobalTrigger(name) == true
    end
    if has(".seencutscene_") or has(".regcutscene_") then return true end
    return modeName == "Outro" and has(".metcutscene_")
end

local function songCutScenes(n, folder, sf, played)
    local cuts = {}
    local uid = n.UniqueId or ""
    local intro = n:GetCustomCommand(INTRO_CMD)
    if intro ~= nil and intro ~= "" then
        local a = splitComma(intro)
        if a[1] ~= nil and a[1] ~= "" then
            local file = fileNameOf(a[1])
            cuts[#cuts + 1] = { path = folder .. a[1], kind = "intro", index = 0,
                                discovered = played or discovered(sf, uid, "Intro", file) }
        end
    end
    local outro = n:GetCustomCommand(OUTRO_CMD)
    if outro ~= nil and outro ~= "" then
        local a = splitComma(outro)
        local idx = 0
        for i = 1, #a, 4 do
            if a[i] ~= nil and a[i] ~= "" then
                idx = idx + 1
                local file = fileNameOf(a[i])
                cuts[#cuts + 1] = { path = folder .. a[i], kind = "outro", index = idx, discovered = discovered(sf, uid, "Outro", file) }
            end
        end
    end
    return cuts
end

-- every listable song with at least one cutscene, grouped by genre in library order
local function buildGrouped()
    local out, order = {}, {}
    local ok = pcall(function()
        local lsls = GenerateSongListSettings()
        lsls:SetExcludedGenreFolders({ "段位道場", "太鼓タワー" })
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
                keep = n.IsSong and (n.HiddenIndex or 0) < 3 and hasCutScenes(n)
            end)
            return keep
        end)
        local sf = ctx and ctx.save and ctx.save() or nil
        local saveIndex = ctx and ctx.playerIndex and ctx.playerIndex() or 0
        for i = 0, res.Count - 1 do
            local n = res[i]
            local e = nil
            pcall(function()
                local folder, played = songInfo(n, saveIndex)
                if folder == nil then return end
                local cuts = songCutScenes(n, folder, sf, played)
                if #cuts == 0 then return end
                e = { title = n.Title or "?", locked = n.IsLocked == true, genre = n.Genre or "?", cuts = cuts }
            end)
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

local function cutText(e, c)
    local title = e.locked and tr("unknown_song") or e.title
    local kind = (c.kind == "outro") and string.format(tr("outro"), c.index) or tr("intro")
    return title .. "  ·  " .. kind
end

local function focusWidget(w)
    if ui == nil or w == nil then return end
    for i, f in ipairs(ui.focusables) do
        if f == w then ui:_setFocusIndex(i); return end
    end
end

local function disposeWindow()
    if ui then pcall(function() ui:disposeWidgets() end); ui = nil end
    W = {}
end

-- ── the cutscene player ────────────────────────────────────────────────────────────────────────
local function openPlayer(path)
    player = ROACTIVITY:GetROActivity("video_player")
    if player == nil then return end
    if ctx and ctx.duck then ctx.duck(true) end
    player:Activate(path)
    mode = "player"
end

local function closePlayer()
    if player and player.IsActive then pcall(function() player:Deactivate() end) end
    player = nil
    if ctx and ctx.duck then ctx.duck(false) end
    mode = "browser"
end

-- ── the browser ────────────────────────────────────────────────────────────────────────────────
local rebuildRight

local function onGenreChange(_, it)
    local g = it and it.value
    if type(g) == "table" and g ~= curGenre then
        curGenre = g
        rebuildRight()
    end
end

local function onGenreSelect(_, it)
    onGenreChange(nil, it)
    if W.right then focusWidget(W.right) end
end

local function onCutSelect(_, it)
    local c = it.value
    if type(c) == "table" and c.discovered then openPlayer(c.path) end
end

rebuildRight = function()
    if ui == nil then return end
    if W.right then
        ui:remove(W.right)   -- unregister before dispose, as the jukebox does
        W.right:dispose()
        W.right = nil
    end
    local items = {}
    if curGenre then
        for _, e in ipairs(curGenre.songs) do
            for _, c in ipairs(e.cuts) do
                items[#items + 1] = { text = cutText(e, c), value = c, locked = not c.discovered }
            end
        end
    end
    if #items > 0 then
        W.right = ui:menu{ x = RIGHT.x, y = RIGHT.y, w = RIGHT.w, h = RIGHT.h, rowHeight = RIGHT.row,
                           items = items, onSelect = onCutSelect }
    end
end

local function buildBrowser()
    disposeWindow()
    ui = PopUI.new{ theme = ctx and ctx.theme or nil, navPlayer = (ctx and ctx.playerIndex() or 0) + 1 }
    ui:panel{ x = PANEL.x, y = PANEL.y, w = PANEL.w, h = PANEL.h, title = tr("title") }
    -- the one tab for now: styled like the jukebox's tabs, but out of the key/pad walk since it leads nowhere
    W.tab = ui:button{ x = TAB.x, y = TAB.y, w = TAB.w, h = TAB.h, text = tr("tab_songs"), accent = true,
                       focusable = false, onClick = function() end }
    W.note = ui:label{ x = PANEL.x + PANEL.w / 2, y = LEFT.y + 40, w = PANEL.w - 80, h = 48, size = 26,
                       align = "center", maxWidth = PANEL.w - 80, text = "" }
    W.note:setVisible(false)
    grouped = songsReady() and buildGrouped() or nil
    curGenre = nil
    if not songsReady() then
        W.note:setText(tr("waiting")); W.note:setVisible(true)
    elseif grouped == nil then
        W.note:setText(tr("empty")); W.note:setVisible(true)
    else
        local items = {}
        for _, g in ipairs(grouped) do
            local n = 0
            for _, e in ipairs(g.songs) do n = n + #e.cuts end
            items[#items + 1] = { text = string.format("%s  (%d)", g.genre, n), value = g }
        end
        W.left = ui:menu{ x = LEFT.x, y = LEFT.y, w = LEFT.w, h = LEFT.h, rowHeight = LEFT.row,
                          items = items, onChange = onGenreChange, onSelect = onGenreSelect }
        curGenre = grouped[1]
        rebuildRight()
        focusWidget(W.left)
    end
    mode = "browser"
end

-- ── the TV menu ────────────────────────────────────────────────────────────────────────────────
local function buildMenu(displayName)
    disposeWindow()
    ui = PopUI.new{ theme = ctx and ctx.theme or nil, navPlayer = (ctx and ctx.playerIndex() or 0) + 1 }
    local x, y = (1920 - MENU_W) / 2, (1080 - MENU_H) / 2
    local panel = ui:panel{ x = x, y = y, w = MENU_W, h = MENU_H, pad = 30, title = displayName or tr("title") }
    local cx, cy, cw = panel:content()
    W.menu = ui:menu{ x = cx, y = cy + 10, w = cw, h = MENU_ROW * 2, rowHeight = MENU_ROW, selected = 1,
        items = { tr(openItem and openItem.tvOn and "menu_off" or "menu_on"), tr("menu_rewatch") },
        onSelect = function(i)
            if i == 1 then
                if openItem then
                    openItem.tvOn = not openItem.tvOn
                    if not openItem.tvOn then anim[openItem] = nil end
                    if ctx and ctx.onToggled then ctx.onToggled(openItem) end
                end
                TV.close(true)
            else
                buildBrowser()
            end
        end }
    focusWidget(W.menu)
    mode = "menu"
end

-- ── public API ─────────────────────────────────────────────────────────────────────────────────
-- ctx: { theme, playerIndex()->n, save()->save file|nil, room()->room, scene()->scene|nil,
--        propInstFor(it)->inst|nil, catalog(id)->cat|nil, displayName()->string, onToggled(it), duck(on) }
function TV.init(c) ctx = c end

function TV.isOpen() return mode ~= nil end

function TV.openFor(item, displayName)
    if mode ~= nil then return end
    openItem = item
    justOpened = true
    buildMenu(displayName)
end

-- silent = true keeps the cancel sound for the caller's own feedback
function TV.close(silent)
    if mode == "player" then closePlayer() end
    disposeWindow()
    mode, openItem, curGenre, grouped = nil, nil, nil, nil
    if not silent then playCancel() end
end

-- returns "closed" the frame the window shuts so Script can flip modes
function TV.update(dt, ts)
    if mode == nil then return nil end
    if justOpened then justOpened = false; return nil end
    if mode == "player" then
        if player == nil then closePlayer(); return nil end
        player:Update()
        if not player.IsActive then closePlayer() end
        return nil
    end
    if ui == nil then TV.close(true); return "closed" end
    local r = ui:update(ts)
    if mode == nil then return "closed" end   -- a menu row closed the window (turn on/off)
    if r == "cancel" then
        if mode == "browser" then
            playCancel()
            if W.right and W.left and ui.focusables[ui.focusIdx] == W.right then
                focusWidget(W.left)   -- from the cutscenes back to the genres
            else
                buildMenu(ctx and ctx.displayName and ctx.displayName() or nil)   -- back to the TV menu
            end
        elseif mode == "menu" then
            TV.close()
            return "closed"
        end
    end
    return nil
end

function TV.draw()
    if mode == nil then return end
    if mode == "player" then
        if player and player.IsActive then player:Draw() end
        return
    end
    if ui then ui:draw() end
end

function TV.dispose()
    TV.close(true)
    anim, lightDefs = {}, {}
end

return TV
