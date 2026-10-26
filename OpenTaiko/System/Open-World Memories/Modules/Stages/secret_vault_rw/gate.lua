---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
local Tex = require("tex")
local Easing = require("Easing")
local NavInput = require("NavInput")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Door, Panel, Bolts = Layout.gate, Layout.gate.panel, Layout.gate.bolts
local Motion = Animation.gate

local G = {}

local CX, CY = Door.center.x, Door.center.y  -- the doorway's centre
local DOOR_R = Door.door_r                   -- the door; the doorway shows it within 304 px
local TRAVEL = Motion.travel_px              -- the door rolls this far, into the wall beside the doorway
local ZOOM_IN = Motion.zoom_in               -- moving in this far, the doorway fills the screen
local BOLT_OUT, BOLT_IN = Door.bolt_out, Door.bolt_in   -- a bolt's centre from the door's, locked / drawn back
local GUIDE_R = Door.guide_r                 -- the clamp each bolt slides through; the bolt's collar stops against it
local WheelShadow = Door.wheel_shadow
local KeyShadow = Door.key_shadow
local TURNED = Motion.turned_deg             -- a turned key, a quarter turn clockwise
local RING_SPEED = Motion.ring_rate_per_s
local SLAM = Motion.slam_px                  -- px of shake when the door seats

-- a door move's timings (s) and turns of the wheel
local function moves(t, spinSnd, rollSnd)
    return { turns = t.turns, spin = t.spin_s, boltsAt = t.bolts_at_s, bolts = t.bolts_s, rollAt = t.roll_at_s,
             roll = t.roll_s, push = t.push_s, spinSnd = spinSnd, rollSnd = rollSnd }
end
local SLOW = moves(Motion.slow, "WheelSpin", "GateRoll")
local FAST = moves(Motion.fast, "WheelSpinShort", "GateRollShort")
local CLOSE = moves(Motion.close)
CLOSE.spinAt = Motion.close.spin_at_s

local OWN_TEXTURES = { "Gate/Wall", "Gate/Wheel", "Gate/Bolt", "Gate/Guide" }
local OWN_SOUNDS = { "WheelSpin", "WheelSpinShort", "BoltsOpen", "BoltsShut", "GateRoll", "GateRollShort", "GateRollShut" }

local PANEL_TOP = Panel.top
local PAD_X, PAD_Y = Panel.pad_x, Panel.pad_y
local NAME_ROW, HINT_ROW, STATUS_ROW = Panel.name_row, Panel.hint_row, Panel.status_row
local HINT_MAX = Panel.hint_max_w            -- the hint wraps at this width
local PANEL_MIN_W, PANEL_MAX_W = Panel.min_w, Panel.max_w

-- left to right around the door; each key's keyhole and colour: Layout.gate.keys
local KEYS = {
    { id = "greed", tex = "Gate/KeyGreed",
      name = "VAULT_KEY_GREED", en = "Key of Greed",
      hint = "VAULT_KEY_GREED_HINT", enHint = "Occasionally available in the General Store for a juicy price" },
    { id = "pride", tex = "Gate/KeyPride",
      name = "VAULT_KEY_PRIDE", en = "Key of Pride",
      hint = "VAULT_KEY_PRIDE_HINT", enHint = "Beat Nokon at his own show" },
    { id = "envy",  tex = "Gate/KeyEnvy",
      name = "VAULT_KEY_ENVY", en = "Key of Envy",
      hint = "VAULT_KEY_ENVY_HINT", enHint = "Reach the 6th door in the Pagoda of the Unknown" },
    { id = "wrath", tex = "Gate/KeyWrath",
      name = "VAULT_KEY_WRATH", en = "Key of Wrath",
      hint = "VAULT_KEY_WRATH_HINT", enHint = "This key is free for now... Until a further update..." },
}
for i, k in ipairs(KEYS) do
    local place = Door.keys[i]
    k.x, k.y, k.col = place.x, place.y, place.color
end

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
    local push = now + T.rollAt + T.roll - Motion.push_overlap_s
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
    local U = Motion.unlock
    local t = now + U.delay_s
    for i = 1, #KEYS do
        turn[i] = { t0 = t, dur = U.turn_s }
        cue(t, "Unlock")
        t = t + U.stagger_s
    end
    open(t + U.door_after_s, SLOW, t + U.door_after_s + SLOW.rollAt)   -- the music comes in as the door starts to roll
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
    slams[#slams + 1] = { at = now + T.boltsAt + T.bolts, amp = SLAM * Motion.bolts_slam_factor }
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
    local m = dim and Panel.dim_shade or 1
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
    return TURNED * Easing.outBack((now - tr.t0) / tr.dur, Motion.key_overshoot)
end

local function shake(now)
    local x = 0
    local Deny, Slam = Motion.deny_shake, Motion.slam_shake
    if denyT ~= nil then
        local a = now - denyT
        if a < Deny.dur_s then x = x + Deny.amp * math.exp(-Deny.decay * a) * math.sin(Deny.rad_per_s * a) end
    end
    for _, s in ipairs(slams) do
        local a = now - s.at
        if a >= 0 and a < Slam.dur_s then x = x + s.amp * math.exp(-Slam.decay * a) * math.sin(Slam.rad_per_s * a) end
    end
    return x
end

-- the bolts, their clamps and the wheel, on the door at (x, y) turned by rot
local function drawDoorFront(now, x, y, rot, z)
    local bolt = ctx.tex["Gate/Bolt"]
    local r = (BOLT_IN + (BOLT_OUT - BOLT_IN) * value("bolts", now)) * z
    for i = 0, Bolts.count - 1 do
        local a = Bolts.first_deg + Bolts.step_deg * i + rot
        local rad = math.rad(a)
        Tex.sprite(bolt, Tex.frame(x + r * math.cos(rad), y - r * math.sin(rad), a, z))
    end
    local guide = ctx.tex["Gate/Guide"]
    for i = 0, Bolts.count - 1 do
        local a = Bolts.first_deg + Bolts.step_deg * i + rot
        local rad = math.rad(a)
        Tex.sprite(guide, Tex.frame(x + GUIDE_R * z * math.cos(rad), y - GUIDE_R * z * math.sin(rad), a, z))
    end
    local wheel = ctx.tex["Gate/Wheel"]
    local w = value("spin", now) + rot
    Tex.sprite(wheel, Tex.frame(x + WheelShadow.x * z, y + WheelShadow.y * z, w, z), 1, 1, WheelShadow.opacity, 0)
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
    local wx, wy = P(Door.wall.x, Door.wall.y)
    Tex.sprite(ctx.tex["Gate/Wall"], Tex.frame(wx, wy, 0, z))
    if home then drawDoorFront(now, dx, dy, rot, z) end

    local hole = ctx.tex["Gate/Keyhole"]
    for i, k in ipairs(KEYS) do
        local x, y = P(k.x, k.y)
        Tex.sprite(hole, Tex.frame(x, y, 0, z))
        if owned[i] then
            local tex = ctx.tex[k.tex]
            local a = keyAngle(i, now)
            Tex.sprite(tex, Tex.frame(x + KeyShadow.x * z, y + KeyShadow.y * z, a, z), 1, 1, KeyShadow.opacity, 0)
            Tex.sprite(tex, Tex.frame(x, y, a, z))
        end
    end

    if not ui or seq ~= nil then return end
    local k = KEYS[sel]
    local m = math.min(1, dt * RING_SPEED)
    ringX, ringY = ringX + (k.x + sx - ringX) * m, ringY + (k.y - ringY) * m
    local pulse = 1 + Motion.ring_pulse.amp * math.sin(now * 2 * math.pi * Motion.ring_pulse.hz)
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
    local S = Panel.status_colors
    if denyT ~= nil and now - denyT < Motion.deny_message_s then
        return Tex.tr("VAULT_GATE_MISSING", "The door won't budge... Some keys are still missing."), Tex.col(S.missing[1], S.missing[2], S.missing[3])
    elseif allOwned() then
        return Tex.tr("VAULT_GATE_READY", "All four keys are here: turn them to open the vault!"), Tex.col(S.ready[1], S.ready[2], S.ready[3])
    elseif owned[sel] then
        return Tex.tr("VAULT_GATE_OWNED", "This key sits in its lock."), Tex.col(S.owned[1], S.owned[2], S.owned[3])
    end
    return Tex.tr("VAULT_GATE_NOT_OWNED", "This key is still missing."), Tex.col(S.not_owned[1], S.not_owned[2], S.not_owned[3])
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

    local Slide = Motion.panel
    local y = PANEL_TOP - (h + Slide.in_rise_px) * (1 - Easing.outBack(a / Slide.in_s, Slide.overshoot))
    if panelOut ~= nil then
        local b = now - panelOut
        if b >= Slide.out_s then return end
        y = y - (h + Slide.out_rise_px) * Easing.inQuad(b / Slide.out_s)
    end
    local cx = Panel.center_x
    plate(ctx.tex["Gate/HintPanel"], cx - w / 2, y, w, h)

    local inkC, hintC = Panel.name_outline_color, Panel.hint_color
    local ink = Tex.col(inkC[1], inkC[2], inkC[3])
    local brown = Tex.col(hintC[1], hintC[2], hintC[3])               -- the hint in brown ink on the parchment
    local row = y + PAD_Y
    Tex.text(f.big, name, Tex.frame(cx, row + NAME_ROW / 2), keyColor(k, not owned[sel]), ink, 1, room, "center")
    row = row + NAME_ROW
    for _, l in ipairs(lines) do
        Tex.text(f.text, l, Tex.frame(cx, row + HINT_ROW / 2), brown, Tex.clear(), 1, room, "center")
        row = row + HINT_ROW
    end
    Tex.text(f.small, status, Tex.frame(cx, row + STATUS_ROW / 2), scol, Tex.clear(), 1, room, "center")
end

return G
