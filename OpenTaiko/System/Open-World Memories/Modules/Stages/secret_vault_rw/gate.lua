---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
local Tex = require("tex")
local Easing = require("Easing")
local NavInput = require("NavInput")

local G = {}

local CX, CY = 960, 610                      -- the doorway's centre
local DOOR_R = 330                           -- the door; the doorway shows it within 304 px
local TRAVEL = 650                           -- the door rolls this far, into the wall beside the doorway
local ZOOM_IN = 3.95                         -- moving in this far, the doorway fills the screen
local BOLT_OUT, BOLT_IN = 310, 210           -- a bolt's centre from the door's, locked / drawn back
local WHEEL_SHADOW = { 8, 10 }
local KEY_SHADOW = { 7, 9 }
local TURNED = -90                           -- a turned key, a quarter turn clockwise
local RING_SPEED = 14
local SLAM = 7                               -- px of shake when the door seats

local SLOW = { turns = 3, spin = 2.2, boltsAt = 1.6, bolts = 0.45, rollAt = 2.5, roll = 2.6, push = 0.85,
               spinSnd = "WheelSpin", rollSnd = "GateRoll" }
local FAST = { turns = 1.5, spin = 1.0, boltsAt = 0.6, bolts = 0.38, rollAt = 1.05, roll = 1.2, push = 0.6,
               spinSnd = "WheelSpinShort", rollSnd = "GateRollShort" }
local CLOSE = { push = 0.55, rollAt = 0.4, roll = 1.2, boltsAt = 1.62, bolts = 0.2, spinAt = 1.7, spin = 0.6, turns = -0.5 }

local OWN_TEXTURES = { "Gate/Wall", "Gate/Wheel", "Gate/Bolt" }
local OWN_SOUNDS = { "WheelSpin", "WheelSpinShort", "BoltsOpen", "BoltsShut", "GateRoll", "GateRollShort", "GateRollShut" }

local PANEL_TOP = 18
local PAD_X, PAD_Y = 60, 24
local NAME_ROW, HINT_ROW, STATUS_ROW = 64, 36, 40
local HINT_MAX = 1480                        -- the hint wraps at this width
local PANEL_MIN_W, PANEL_MAX_W = 760, 1800

-- left to right around the door
local KEYS = {
    { id = "greed", x = 200,  y = 400, tex = "Gate/KeyGreed", col = { 96, 222, 255 },
      name = "VAULT_KEY_GREED", en = "Key of Greed",
      hint = "VAULT_KEY_GREED_HINT", enHint = "Occasionally available in the General Store for a juicy price" },
    { id = "pride", x = 420,  y = 760, tex = "Gate/KeyPride", col = { 104, 232, 120 },
      name = "VAULT_KEY_PRIDE", en = "Key of Pride",
      hint = "VAULT_KEY_PRIDE_HINT", enHint = "Beat Nokon at his own show" },
    { id = "envy",  x = 1500, y = 760, tex = "Gate/KeyEnvy",  col = { 255, 218, 72 },
      name = "VAULT_KEY_ENVY", en = "Key of Envy",
      hint = "VAULT_KEY_ENVY_HINT", enHint = "Reach the 6th door in the Pagoda of the Unknown" },
    { id = "wrath", x = 1720, y = 400, tex = "Gate/KeyWrath", col = { 255, 96, 84 },
      name = "VAULT_KEY_WRATH", en = "Key of Wrath",
      hint = "VAULT_KEY_WRATH_HINT", enHint = "This key is free for now... Until a further update..." },
}

local ctx = nil
local owned = {}
local turn = {}          -- per key: { t0, dur } while turning
local turned = {}
local seq = nil          -- the door's open or close: { kind, done, movingFrom, tw = { spin, bolts, roll, zoom } }
local spinRest = 0
local cues = {}          -- { at, snd }
local slams = {}         -- { at, amp }
local sel = 1
local ringX, ringY = KEYS[1].x, KEYS[1].y
local panelIn, panelOut = nil, nil
local denyT = nil

local function isOwned(id)
    local sf = GetSaveFile(0)
    if sf == nil then return false end
    if id == "greed" then return sf:GetGlobalTrigger(".vault_key_obtained_greed") == true end
    if id == "pride" then return sf:GetGlobalTrigger(".vault_key_obtained_pride") == true end
    if id == "wrath" then return true end
    if id == "envy" then return (sf:GetGlobalCounter("pagoda_highest_level") or 0) >= 15 end
    return false
end

local function allOwned()
    for i = 1, #KEYS do if not owned[i] then return false end end
    return true
end

local function cue(at, name)
    cues[#cues + 1] = { at = at, snd = name }
end

local function play(name)
    local s = ctx.snd[name]
    if s then s:Play() end
end

local function tween(t0, dur, from, to, ease)
    return { t0 = t0, dur = dur, from = from, to = to, ease = ease }
end

local function at(tw, now)
    local u = (now - tw.t0) / tw.dur
    if u <= 0 then return tw.from end
    if u >= 1 then return tw.to end
    return tw.from + (tw.to - tw.from) * tw.ease(u)
end

-- spin (deg), bolts (1 locked .. 0 drawn back), roll (px), zoom
local function value(ch, now)
    if seq == nil then
        if ch == "spin" then return spinRest end
        if ch == "bolts" or ch == "zoom" then return 1 end
        return 0
    end
    return at(seq.tw[ch], now)
end

function G.init(c)
    ctx = c
    for _, name in ipairs(OWN_SOUNDS) do
        local path = "Sounds/" .. name .. ".ogg"
        if ctx.snd[name] == nil and STORAGE:FileExists(path) then ctx.snd[name] = SOUND:CreateSFX(path) end
    end
end

-- the gate's own textures join the stage's, so the stage frees them with the rest when it leaves
local function loadTextures()
    for _, name in ipairs(OWN_TEXTURES) do
        local path = "Textures/" .. name .. ".png"
        if ctx.tex[name] == nil and TEXTURE:Exists(path) then ctx.tex[name] = TEXTURE:CreateTexture(path) end
    end
end

function G.reset(mode, now, showAt)
    loadTextures()
    owned, turn, turned, cues, slams = {}, {}, {}, {}, {}
    -- an open vault had all four keys turned in it
    for i, k in ipairs(KEYS) do
        owned[i] = mode == "unlocked" or isOwned(k.id)
        turned[i] = mode == "unlocked"
    end
    seq = nil
    spinRest = 0
    sel = 1
    ringX, ringY = KEYS[1].x, KEYS[1].y
    panelIn = (mode == "locked") and (showAt or now) or nil
    panelOut = nil
    denyT = nil
end

-- movingFrom: when Gate.moving turns true (the stage starts its music then)
local function open(now, T, movingFrom)
    local t = {}
    local push = now + T.rollAt + T.roll - 0.12
    t.spin = tween(now, T.spin, 0, T.turns * 360, Easing.inOutCubic)
    t.bolts = tween(now + T.boltsAt, T.bolts, 1, 0, Easing.inOutQuad)
    t.roll = tween(now + T.rollAt, T.roll, 0, TRAVEL, Easing.inOutSine)
    t.zoom = tween(push, T.push, 1, ZOOM_IN, Easing.inCubic)
    seq = { kind = "open", done = push + T.push, movingFrom = movingFrom or now, tw = t }
    cue(now, T.spinSnd)
    cue(now + T.boltsAt, "BoltsOpen")
    cue(now + T.rollAt, T.rollSnd)
end

function G.startUnlock(now)
    local t = now + 0.5
    for i = 1, #KEYS do
        turn[i] = { t0 = t, dur = 0.32 }
        cue(t, "Unlock")
        t = t + 0.6
    end
    open(t + 0.3, SLOW, t + 0.3 + SLOW.rollAt)   -- the music comes in as the door starts to roll
    panelOut = now
end

function G.startOpen(now)
    open(now, FAST)
end

function G.startClose(now)
    local from = seq ~= nil and value("spin", now) or spinRest
    local T = CLOSE
    local t = {}
    t.zoom = tween(now, T.push, ZOOM_IN, 1, Easing.outCubic)
    t.roll = tween(now + T.rollAt, T.roll, TRAVEL, 0, Easing.inOutCubic)
    t.bolts = tween(now + T.boltsAt, T.bolts, 0, 1, Easing.inQuad)
    t.spin = tween(now + T.spinAt, T.spin, from, from + T.turns * 360, Easing.outCubic)
    seq = { kind = "close", done = now + T.spinAt + T.spin, movingFrom = now, tw = t }
    spinRest = from + T.turns * 360
    cue(now + T.rollAt, "GateRollShut")
    cue(now + T.boltsAt, "BoltsShut")
    slams[#slams + 1] = { at = now + T.rollAt + T.roll, amp = SLAM }
    slams[#slams + 1] = { at = now + T.boltsAt + T.bolts, amp = SLAM * 0.45 }
end

function G.update(now)
    local i = 1
    while i <= #cues do
        if now >= cues[i].at then
            play(cues[i].snd)
            table.remove(cues, i)
        else
            i = i + 1
        end
    end
    for k = 1, #KEYS do
        local tr = turn[k]
        if tr and now >= tr.t0 + tr.dur then turned[k], turn[k] = true, nil end
    end
end

function G.isOpen(now) return seq ~= nil and seq.kind == "open" and now >= seq.done end
function G.isClosed(now) return seq == nil or (seq.kind == "close" and now >= seq.done) end
function G.moving(now) return seq ~= nil and now >= seq.movingFrom end

-- the shut door and the wall hide everything behind them
function G.covers(now)
    local wall, door = ctx.tex["Gate/Wall"], ctx.tex["Gate/Door"]
    return value("roll", now) < 0.5 and value("zoom", now) <= 1.0001
        and wall ~= nil and wall.Ready and door ~= nil and door.Ready
end

-- the selected key's colour, as a glyph colour
local function keyColor(k, dim)
    local m = dim and 0.55 or 1
    return Tex.col(math.floor(k.col[1] * m), math.floor(k.col[2] * m), math.floor(k.col[3] * m))
end

function G.input(now)
    local nav = NavInput.p[1]
    if nav.right() then
        sel = sel % #KEYS + 1
        play("Skip")
    elseif nav.left() then
        sel = (sel - 2) % #KEYS + 1
        play("Skip")
    elseif nav.cancel() then
        play("Cancel")
        return "back"
    elseif nav.decide() then
        if allOwned() then
            play("Decide")
            return "unlock"
        end
        play("NoKey")
        denyT = now
    end
    return nil
end

local function keyAngle(i, now)
    if turned[i] then return TURNED end
    local tr = turn[i]
    if tr == nil or now < tr.t0 then return 0 end
    return TURNED * Easing.outBack((now - tr.t0) / tr.dur, 1.6)
end

local function shake(now)
    local x = 0
    if denyT ~= nil then
        local a = now - denyT
        if a < 0.6 then x = x + 9 * math.exp(-8 * a) * math.sin(62 * a) end
    end
    for _, s in ipairs(slams) do
        local a = now - s.at
        if a >= 0 and a < 0.4 then x = x + s.amp * math.exp(-10 * a) * math.sin(70 * a) end
    end
    return x
end

-- the bolts and the wheel, on the door at (x, y) turned by rot
local function drawDoorFront(now, x, y, rot, z)
    local bolt = ctx.tex["Gate/Bolt"]
    local r = (BOLT_IN + (BOLT_OUT - BOLT_IN) * value("bolts", now)) * z
    for i = 0, 7 do
        local a = 22.5 + 45 * i + rot
        local rad = math.rad(a)
        Tex.sprite(bolt, Tex.frame(x + r * math.cos(rad), y - r * math.sin(rad), a, z))
    end
    local wheel = ctx.tex["Gate/Wheel"]
    local w = value("spin", now) + rot
    Tex.sprite(wheel, Tex.frame(x + WHEEL_SHADOW[1] * z, y + WHEEL_SHADOW[2] * z, w, z), 1, 1, 0.35, 0)
    Tex.sprite(wheel, Tex.frame(x, y, w, z))
end

function G.draw(now, dt, ui)
    if G.isOpen(now) then return end
    local sx = shake(now)
    local z = value("zoom", now)
    local roll = value("roll", now)
    local home = roll < 0.5
    local rot = -math.deg(roll / DOOR_R)
    local function P(x, y) return CX + (x - CX) * z + sx, CY + (y - CY) * z end

    -- the door rolls behind the wall; at home its bolts reach over the wall's frame
    local dx, dy = P(CX + roll, CY)
    if roll < TRAVEL - 0.5 then
        Tex.sprite(ctx.tex["Gate/Door"], Tex.frame(dx, dy, rot, z))
        if not home then drawDoorFront(now, dx, dy, rot, z) end
    end
    local wx, wy = P(960, 540)
    Tex.sprite(ctx.tex["Gate/Wall"], Tex.frame(wx, wy, 0, z))
    if home then drawDoorFront(now, dx, dy, rot, z) end

    local hole = ctx.tex["Gate/Keyhole"]
    for i, k in ipairs(KEYS) do
        local x, y = P(k.x, k.y)
        Tex.sprite(hole, Tex.frame(x, y, 0, z))
        if owned[i] then
            local tex = ctx.tex[k.tex]
            local a = keyAngle(i, now)
            Tex.sprite(tex, Tex.frame(x + KEY_SHADOW[1] * z, y + KEY_SHADOW[2] * z, a, z), 1, 1, 0.3, 0)
            Tex.sprite(tex, Tex.frame(x, y, a, z))
        end
    end

    if not ui or seq ~= nil then return end
    local k = KEYS[sel]
    local m = math.min(1, dt * RING_SPEED)
    ringX, ringY = ringX + (k.x + sx - ringX) * m, ringY + (k.y - ringY) * m
    local pulse = 1 + 0.035 * math.sin(now * 2 * math.pi * 1.1)
    Tex.sprite(ctx.tex["Gate/Hover"], Tex.frame(ringX, ringY, 0, pulse))
end

-- the parchment over (x, y, w, h): its four quarters cropped from the four corners of Gate/HintPanel, a sheet as
-- big as the widest panel, so the paper and its rounded rim are never stretched (only a panel bigger than the sheet
-- stretches its quarters)
local function plate(tex, x, y, w, h)
    if not Tex.ok(tex) then return end
    local tw, th = tex.Width, tex.Height
    local lw, uh = math.floor(w / 2), math.floor(h / 2)
    local cols = { { x, lw }, { x + lw, w - lw } }
    local rows = { { y, uh }, { y + uh, h - uh } }
    for i, cl in ipairs(cols) do
        local sw = math.min(cl[2], math.floor(tw / 2))
        local sx = (i == 1) and 0 or (tw - sw)
        for j, rw in ipairs(rows) do
            local sh = math.min(rw[2], math.floor(th / 2))
            local sy = (j == 1) and 0 or (th - sh)
            if sw > 0 and sh > 0 then
                tex:SetScale(cl[2] / sw, rw[2] / sh)
                tex:DrawRectAtAnchor(cl[1], rw[1], sx, sy, sw, sh, "topleft")
            end
        end
    end
    tex:SetScale(1, 1)
end

-- the panel's status line and its ink (red, amber, green or grey on the parchment)
local function statusLine(now)
    if denyT ~= nil and now - denyT < 2.4 then
        return Tex.tr("VAULT_GATE_MISSING", "The door won't budge... Some keys are still missing."), Tex.col(170, 44, 28)
    elseif allOwned() then
        return Tex.tr("VAULT_GATE_READY", "All four keys are here: turn them to open the vault!"), Tex.col(150, 94, 10)
    elseif owned[sel] then
        return Tex.tr("VAULT_GATE_OWNED", "This key sits in its lock."), Tex.col(40, 108, 50)
    end
    return Tex.tr("VAULT_GATE_NOT_OWNED", "This key is still missing."), Tex.col(112, 100, 88)
end

-- the hint panel, sized to its text
function G.drawPanel(now, dt)
    if panelIn == nil then return end
    local a = now - panelIn
    if a < 0 then return end
    local f = ctx.fonts
    local k = KEYS[sel]
    local name = Tex.tr(k.name, k.en)
    local lines = Tex.wrap(f.text, Tex.tr(k.hint, k.enHint), HINT_MAX)
    local status, scol = statusLine(now)

    local inner = f.big and f.big:Measure(name) or 0
    for _, l in ipairs(lines) do inner = math.max(inner, f.text and f.text:Measure(l) or 0) end
    inner = math.max(inner, f.small and f.small:Measure(status) or 0)
    local w = math.floor(math.min(PANEL_MAX_W, math.max(PANEL_MIN_W, inner + 2 * PAD_X)) + 0.5)
    local h = 2 * PAD_Y + NAME_ROW + #lines * HINT_ROW + STATUS_ROW
    local room = w - 2 * PAD_X

    local y = PANEL_TOP - (h + 60) * (1 - Easing.outBack(a / 0.5, 1.3))
    if panelOut ~= nil then
        local b = now - panelOut
        if b >= 0.35 then return end
        y = y - (h + 80) * Easing.inQuad(b / 0.35)
    end
    plate(ctx.tex["Gate/HintPanel"], 960 - w / 2, y, w, h)

    local ink = Tex.col(24, 18, 14)
    local brown = Tex.col(62, 38, 20)               -- the hint in brown ink on the parchment
    local row = y + PAD_Y
    Tex.text(f.big, name, Tex.frame(960, row + NAME_ROW / 2), keyColor(k, not owned[sel]), ink, 1, room, "center")
    row = row + NAME_ROW
    for _, l in ipairs(lines) do
        Tex.text(f.text, l, Tex.frame(960, row + HINT_ROW / 2), brown, Tex.clear(), 1, room, "center")
        row = row + HINT_ROW
    end
    Tex.text(f.small, status, Tex.frame(960, row + STATUS_ROW / 2), scol, Tex.clear(), 1, room, "center")
end

return G
