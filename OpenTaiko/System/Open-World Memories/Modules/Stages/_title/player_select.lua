---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- _title/player_select.lua  —  player-count selector drawn over the title menu
-- Two ribbon bands, the prompt and a carousel of boxes (back, then 1 to 5 players); the title script feeds it input.

local M = {}

local TEX = "Textures/Players/"
local SND = "Sounds/"

-- ── Layout ────────────────────────────────────────────────────────────────────
-- screen px on 1920x1080; the art sizes are fixed, so nothing is read back from a texture size

local L = {
    BAND_TOP_Y = 24,  BAND_BOT_Y = 984, TILE_W = 960, TIP_W = 56,
    PROMPT_CY  = 262, TEXT_NUDGE = 11,
    BOX_CX     = 960, BOX_CY     = 590,
    BOX_W      = 400, BOX_H      = 468,
    LABEL_DX   = -2.5, LABEL_TOP = 10,      -- the label's top in box px, as the title menu draws its titles
    ART_DX     = -2.5, ART_DY    = 26.5,    -- the art section's centre from the box centre
    STEP_1     = 392, STEP_2     = 344,     -- centre distance to the first neighbour, then to each further one
    SIDE_SHRINK = 0.24, SIDE_DARKEN = 0.3,  -- scale and grey lost one step away from the centre
}

local ITEMS = 6   -- item 0 is the back box, items 1 to 5 the player counts

-- box colours, 0..1: brown for back, as the other menus style it, then the player colours
local PC = {
    [0] = { 140 / 255,  90 / 255,  50 / 255 },
    { 238 / 255,  51 / 255,  51 / 255 },
    {  51 / 255,  85 / 255, 238 / 255 },
    { 187 / 255, 187 / 255,         0 },
    {  51 / 255, 187 / 255,  68 / 255 },
    { 187 / 255,  51 / 255, 187 / 255 },
}

-- open timeline (ms)
local T_UPPER_LAND = 460
local T_LOWER_IN   = 70
local T_LOWER_LAND = 530
local POP_AT       = 180    -- the selected box starts popping in
local POP_STEP     = 60     -- delay per box further out
local POP_MS       = 320
local DECIDE_LOCK  = 200
local DECIDE_MS    = 300
local CLOSE_MS     = 260
local DIM_MAX      = 0.6

-- ── Resources ─────────────────────────────────────────────────────────────────

local tex        = {}
local sfx        = {}
local promptFont = nil
local labelFont  = nil
local promptTex  = nil
local labelTex   = {}
local tr         = function(_, fallback) return fallback end

-- ── State ─────────────────────────────────────────────────────────────────────

local state     = "closed"   -- closed / open / deciding / closing
local armed     = false      -- opened this frame: the timeline starts on the next update
local nowTs     = 0
local openTs    = 0
local decideTs  = 0
local closeTs   = 0
local sel       = 1          -- selected item, 0 = back
local shown     = 1          -- player count on the bands; back keeps the last one
local openSel   = 1          -- selection at open; the pop-in order counts from it
local scroll    = 1          -- carousel position, easing toward sel
local bounceTs  = nil        -- last selection change, for the art bounce
local bounceAmp = 0
local dimCarry  = 0          -- dim left over from a cancel outro when reopening
local closeFrom = nil        -- values at the cancel frame { dim, v }
local queue     = {}         -- delayed sounds { at, sfx, pan }
local bands     = { {}, {} } -- 1 = upper, 2 = lower
local xsU, xsL  = {}, {}

-- ── Math ──────────────────────────────────────────────────────────────────────

local sin, cos, exp, pi = math.sin, math.cos, math.exp, math.pi

local function clamp01(u) if u < 0 then return 0 elseif u > 1 then return 1 end return u end
local function round(v)   return math.floor(v + 0.5) end
local function outQuad(u)  u = clamp01(u); return 1 - (1 - u) ^ 2 end
local function outCubic(u) u = clamp01(u); return 1 - (1 - u) ^ 3 end
local function outQuart(u) u = clamp01(u); return 1 - (1 - u) ^ 4 end
local function inCubic(u)  u = clamp01(u); return u ^ 3 end
local function outBack(u)  u = clamp01(u); return 1 + 2.70158 * (u - 1) ^ 3 + 1.70158 * (u - 1) ^ 2 end

-- ── Clocks ────────────────────────────────────────────────────────────────────

-- the open timeline, frozen while the cancel outro runs
local function tOpen()
    if armed then return 0 end
    if state == "closing" then return closeTs - openTs end
    return nowTs - openTs
end

-- a clock that keeps running through the outro, for the idle loops
local function tLive()
    if armed then return 0 end
    return nowTs - openTs
end

local function tClose() return nowTs - closeTs end

local function outroFade()
    if state == "closing" then return math.max(0, 1 - tClose() / 160) end
    return 1
end

-- box i pops in later the further it sits from the selection at open
local function popStart(i) return POP_AT + math.abs(i - openSel) * POP_STEP end

-- ── Sounds ────────────────────────────────────────────────────────────────────

local function play(s, pan)
    if s == nil then return end
    if pan ~= nil then s:SetPan(pan) end
    s:Play()
end

local function schedule(at, s, pan)
    if at <= nowTs then play(s, pan) return end
    queue[#queue + 1] = { at = at, sfx = s, pan = pan }
end

local function drain(ts)
    local i = 1
    while i <= #queue do
        local q = queue[i]
        if q.at <= ts then
            table.remove(queue, i)
            play(q.sfx, q.pan)
        else
            i = i + 1
        end
    end
end

-- ── Bands ─────────────────────────────────────────────────────────────────────

local function resetBand(band)
    band.phase   = 0
    band.swapTs  = nil
    band.swapOld = 1
end

-- while entering, the left edge of the upper strip and the right edge of the lower one; each starts with its
-- ribbon tip just off screen
local function headU(t) return (1920 + L.TIP_W) * (1 - outQuart(t / T_UPPER_LAND)) end
local function headL(t) return (1920 + L.TIP_W) * outQuart((t - T_LOWER_IN) / (T_LOWER_LAND - T_LOWER_IN)) - L.TIP_W end

-- scroll speed in px/s: it builds up once both ribbons have landed, and rushes while deciding
local function speed()
    if state == "deciding" then
        local u = clamp01((nowTs - decideTs) / DECIDE_MS)
        return 60 + 900 * u * u
    end
    if state == "closing" then return closeFrom and closeFrom.v or 60 end
    return 60 * clamp01((tOpen() - T_LOWER_LAND) / 300)
end

-- on-screen tile x positions of both bands, plus the tip x while a band slides in: the tiles wrap by the scroll
-- phase, and an entering strip starts at its tip
local function bandLayout()
    local t = tOpen()
    local nU, nL, tipU, tipL = 0, 0, nil, nil
    local left = (t >= T_UPPER_LAND) and round(-bands[1].phase) or round(headU(t))
    for m = 0, 3 do
        local x = left + L.TILE_W * m
        if x < 1920 and x + L.TILE_W > 0 then nU = nU + 1; xsU[nU] = x end
    end
    if t < T_UPPER_LAND and left > 0 and left - L.TIP_W < 1920 then tipU = left end
    local right = (t >= T_LOWER_LAND) and 1920 + round(bands[2].phase) or round(headL(t))
    for m = 0, 3 do
        local x = right - L.TILE_W * (m + 1)
        if x < 1920 and x + L.TILE_W > 0 then nL = nL + 1; xsL[nL] = x end
    end
    if t < T_LOWER_LAND and right < 1920 and right + L.TIP_W > 0 then tipL = right end
    return nU, nL, tipU, tipL
end

local function drawTiles(t, xs, n, y, rot, op)
    if t.Width <= 0 or op <= 0 then return end
    t:SetRotation(rot)
    t:SetOpacity(op)
    for m = 1, n do t:DrawAtAnchor(xs[m], y, "topleft") end
    t:SetOpacity(1)
    t:SetRotation(0)
end

-- the ribbons of the shown count, the previous count fading out over them for 120 ms after a change
local function drawBand(b, xs, n, y, tipX)
    local rot  = (b == 2) and 180 or 0
    local band = bands[b]
    local su   = band.swapTs and (nowTs - band.swapTs) or 1e9
    local fade = (su < 120) and 1 - su / 120 or 0
    local oldN = band.swapOld
    drawTiles(tex.band[shown], xs, n, y, rot, 1)
    if fade > 0 and oldN ~= shown then drawTiles(tex.band[oldN], xs, n, y, rot, fade) end
    if tipX ~= nil then
        local tip = tex.tip[shown]
        if tip.Width > 0 then
            tip:SetRotation(rot)
            tip:DrawAtAnchor(tipX, y, (b == 1) and "topright" or "topleft")
            tip:SetRotation(0)
        end
    end
end

local function drawBands()
    local nU, nL, tipU, tipL = bandLayout()
    local yU, yL = L.BAND_TOP_Y, L.BAND_BOT_Y
    if state == "closing" then
        local s = 136 * inCubic(tClose() / 240)
        yU, yL = round(yU - s), round(yL + s)
    end
    drawBand(1, xsU, nU, yU, tipU)
    drawBand(2, xsL, nL, yL, tipL)
end

-- ── Prompt ────────────────────────────────────────────────────────────────────

local function white() return COLOR:CreateColorFromRGBA(255, 255, 255, 255) end

local function getPromptTex()
    if promptTex == nil and promptFont ~= nil then
        promptTex = promptFont:GetText(tr("TITLE_PLAYERS_PROMPT", "How many drummers take the stage?"), false, 1600,
            white(), COLOR:CreateColorFromRGBA(0, 0, 0, 255))
    end
    return promptTex
end

local function drawPrompt()
    local u    = outCubic((tOpen() - 140) / 260)
    local op   = u * outroFade()
    local text = getPromptTex()
    if op <= 0 or text == nil then return end
    text:SetOpacity(op)
    text:DrawAtAnchor(960, L.PROMPT_CY + L.TEXT_NUDGE + round(-24 * (1 - u)), "center")
    text:SetOpacity(1)
end

-- ── Carousel ──────────────────────────────────────────────────────────────────

-- screen x of a box at offset o from the carousel position
local function carouselX(o)
    local a = math.abs(o)
    local d = (a <= 1) and a * L.STEP_1 or L.STEP_1 + (a - 1) * L.STEP_2
    return L.BOX_CX + ((o < 0) and -d or d)
end

-- centre, scale, opacity and grey factor of box i this frame
local function boxLook(i)
    local a = math.min(1, math.abs(i - scroll))
    local u = (tOpen() - popStart(i)) / POP_MS
    local extra, op = outBack(u), outCubic(u)
    local y = L.BOX_CY
    if i == sel then y = y - 4 * (0.5 - 0.5 * cos(2 * pi * tLive() / 1800)) end
    if state == "deciding" then
        local t = nowTs - decideTs
        if i == sel then
            extra = extra * (1 + 0.08 * sin(pi * clamp01(t / DECIDE_MS)))
        else
            op = op * (1 - 0.65 * clamp01(t / 200))
        end
    elseif state == "closing" then
        local v = inCubic(tClose() / 200)
        extra, op = extra * (1 - 0.15 * v), op * (1 - v)
    end
    return carouselX(i - scroll), y, (1 - L.SIDE_SHRINK * a) * extra, op, 1 - L.SIDE_DARKEN * a
end

-- draws t centred at (x, y) with a scale, tint, opacity and blend, then resets its state
local function drawCentered(t, x, y, sc, r, g, b, op, blend)
    if t.Width <= 0 or op <= 0 then return end
    t:SetScale(sc, sc)
    t:SetColor(r, g, b)
    t:SetOpacity(op)
    t:SetBlendMode(blend or "normal")
    t:DrawAtAnchor(x, y, "center")
    t:SetBlendMode("normal")
    t:SetOpacity(1)
    t:SetColor(1, 1, 1)
    t:SetScale(1, 1)
end

-- the item's label, white with an outline in its darkened colour, like the title menu's box titles
local function getLabelTex(i)
    if labelTex[i] == nil and labelFont ~= nil then
        local text = (i == 0) and tr("TITLE_PLAYERS_BACK", "Back")
            or tr("TITLE_PLAYERS_" .. i, (i == 1) and "1 Player" or (i .. " Players"))
        local c = PC[i]
        labelTex[i] = labelFont:GetText(text, false, 360, white(),
            COLOR:CreateColorFromRGBA(math.floor(c[1] * 89), math.floor(c[2] * 89), math.floor(c[3] * 89), 255))
    end
    return labelTex[i]
end

-- the tinted box, its label in the top strip, then its art over the section below; returns the box's centre,
-- scale and opacity
local function drawBox(i)
    local x, y, sc, op, f = boxLook(i)
    local c = PC[i]
    drawCentered(tex.box, x, y, sc, c[1] * f, c[2] * f, c[3] * f, op)

    local label = getLabelTex(i)
    if label ~= nil and op > 0 then
        label:SetScale(sc, sc)
        label:SetColor(f, f, f)
        label:SetOpacity(op)
        label:DrawAtAnchor(x + L.LABEL_DX * sc, y + (L.LABEL_TOP - L.BOX_H / 2) * sc, "top")
        label:SetOpacity(1)
        label:SetColor(1, 1, 1)
        label:SetScale(1, 1)
    end

    local art = sc
    if i == sel and bounceTs ~= nil and nowTs - bounceTs < 240 then
        art = sc * (1 + bounceAmp * sin(pi * (nowTs - bounceTs) / 240))
    end
    drawCentered(tex.art[i], x + L.ART_DX * sc, y + L.ART_DY * sc, art, f, f, f, op)
    return x, y, sc, op
end

-- the selection ring, plus its additive flash while deciding
local function drawRing(x, y, sc, op)
    local fade = clamp01((tOpen() - popStart(sel) - POP_MS) / 150)
    drawCentered(tex.hover, x, y, sc, 1, 1, 1, op * fade * (0.8 + 0.2 * sin(2 * pi * tLive() / 900)))
    if state == "deciding" then
        drawCentered(tex.hover, x, y, sc, 1, 1, 1, 1 - (nowTs - decideTs) / 240, "add")
    end
end

local function farther(p, q)
    local dp, dq = math.abs(p - scroll), math.abs(q - scroll)
    if dp ~= dq then return dp > dq end
    return p < q
end

local function onScreen(i)
    local half = L.BOX_W / 2 * (1 - L.SIDE_SHRINK * math.min(1, math.abs(i - scroll)))
    local x = carouselX(i - scroll)
    return x + half > 0 and x - half < 1920
end

-- far boxes first, the selected one last with its ring on top
local function drawCarousel()
    local order = {}
    for i = 0, ITEMS - 1 do
        if i ~= sel and onScreen(i) then order[#order + 1] = i end
    end
    table.sort(order, farther)
    for _, i in ipairs(order) do drawBox(i) end
    if onScreen(sel) then drawRing(drawBox(sel)) end
end

-- ── Lifecycle ─────────────────────────────────────────────────────────────────

-- trFn: the title's locale lookup, tr(key, fallback)
function M.load(trFn)
    if trFn ~= nil then tr = trFn end
    local function loadTex(name, edge)
        local t = TEXTURE:CreateTexture(TEX .. name .. ".png")
        if edge then t:SetWrapMode("edge") end
        return t
    end
    tex.band, tex.tip, tex.art = {}, {}, { [0] = loadTex("ArtBack", true) }
    for n = 1, 5 do
        tex.band[n] = loadTex("Band" .. n)
        tex.tip[n]  = loadTex("BandTip" .. n, true)
        tex.art[n]  = loadTex("Art" .. n, true)
    end
    tex.box   = loadTex("PlayerBox", true)
    tex.hover = loadTex("PlayerHover", true)

    promptFont = TEXT:Create(36)
    labelFont  = TEXT:Create(28)

    -- one instance per setting: pan, volume and speed also retune a voice that is playing
    local function sound(name, pan, vol, spd)
        local s = SOUND:CreateSFX(SND .. name .. ".ogg")
        s:SetPan(pan)
        s:SetVolume(vol)
        s:SetSpeed(spd)
        return s
    end
    sfx.whooshTop    = sound("PlayerWhoosh",  45, 60, 1.0)
    sfx.whooshBottom = sound("PlayerWhoosh", -45, 60, 0.94)
    sfx.whooshOut    = sound("PlayerWhoosh",   0, 35, 1.25)
    local speeds = { [0] = 0.889, 1.0, 1.125, 1.25, 1.5, 1.667 }
    sfx.pop = {}
    for i = 0, ITEMS - 1 do sfx.pop[i] = sound("PlayerPop", 0, 45, speeds[i]) end   -- panned by its box when played
end

function M.reset()
    state, armed = "closed", false
    bounceTs, closeFrom = nil, nil
    dimCarry = 0
    queue = {}
    promptTex, labelTex = nil, {}
    scroll = sel
    resetBand(bands[1])
    resetBand(bands[2])
end

function M.clearSounds()
    queue = {}
end

function M.reloadLanguage()
    promptTex, labelTex = nil, {}
end

-- ── Queries ───────────────────────────────────────────────────────────────────

function M.visible()   return state ~= "closed" end
function M.deciding()  return state == "deciding" end
function M.count()     return sel end   -- 0 while the back box is selected
function M.canDecide() return state == "open" and not armed and tOpen() >= DECIDE_LOCK end

-- dim opacity behind the prompt; the title menu fades by 1 - dim / 0.6
function M.dim()
    if state == "closed" then return 0 end
    if state == "closing" then
        return (closeFrom and closeFrom.dim or 0) * math.max(0, 1 - tClose() / CLOSE_MS)
    end
    return math.max(dimCarry, DIM_MAX * outQuad(tOpen() / 220))
end

-- ── Input ─────────────────────────────────────────────────────────────────────

function M.open(count)
    dimCarry = (state == "closing") and M.dim() or 0
    sel = math.max(1, math.min(5, math.floor(count or 1)))
    shown, openSel, scroll = sel, sel, sel
    state, armed = "open", true
    bounceTs, closeFrom = nil, nil
    queue = {}
    resetBand(bands[1])
    resetBand(bands[2])
end

-- d = +1 / -1; edge is false for a hold-repeat step
function M.move(d, edge)
    if state ~= "open" then return end
    sel = (sel + d) % ITEMS
    bounceTs, bounceAmp = nowTs, edge and 0.06 or 0.03
    if sel ~= 0 and sel ~= shown then
        for b = 1, 2 do bands[b].swapTs, bands[b].swapOld = nowTs, shown end
        shown = sel
    end
end

function M.decide()
    if state ~= "open" then return end
    state, decideTs = "deciding", nowTs
    queue = {}
end

function M.cancel()
    if state ~= "open" then return end
    closeFrom = { dim = M.dim(), v = speed() }
    closeTs, state = nowTs, "closing"
    queue = {}
    play(sfx.whooshOut)
end

-- ── Update / Draw ─────────────────────────────────────────────────────────────

local function start(ts)
    armed, openTs = false, ts
    schedule(ts, sfx.whooshTop)
    schedule(ts + T_LOWER_IN, sfx.whooshBottom)
    -- one pop per box as it appears, pitched by its count and panned by its rest x
    for i = 0, ITEMS - 1 do
        schedule(ts + popStart(i), sfx.pop[i], round((carouselX(i - openSel) - 960) / 960 * 60))
    end
end

-- ts and dt in ms; returns true on the frame the decide confirm ends
function M.update(ts, dt)
    nowTs = ts
    if state == "closed" then return false end
    if armed then start(ts) end
    drain(ts)
    if state == "closing" and tClose() >= CLOSE_MS then
        state = "closed"
        return false
    end
    if state == "deciding" and ts - decideTs >= DECIDE_MS then return true end

    scroll = scroll + (sel - scroll) * (1 - exp(-dt / 55))
    if math.abs(sel - scroll) < 0.001 then scroll = sel end

    local t, v = tOpen(), speed()
    if t >= T_UPPER_LAND then bands[1].phase = (bands[1].phase + v * dt / 1000) % L.TILE_W end
    if t >= T_LOWER_LAND then bands[2].phase = (bands[2].phase + v * dt / 1000) % L.TILE_W end
    return false
end

-- drawn after the title's dim, drawBgTile(M.dim())
function M.draw()
    if state == "closed" then return end
    drawPrompt()
    drawCarousel()
    drawBands()
end

return M
