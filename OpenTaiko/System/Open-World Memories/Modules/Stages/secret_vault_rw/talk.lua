---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local, inject-field
-- the small talks over coffee (Vault.Talk runs them): Mr.Caspian's offer after a chest, the staging (the BGM
-- fades, the light spot, the cup slides in, the jazz), the lines and menus in the vault's dialogue box, Gris in
-- the tank, at the side window or at the counter on Mr.Caspian's left, her challenge song and the rewards
local Talk = require("Vault.Talk")
local Chests = require("vault")
local Dialogue = require("dialogue")
local Easing = require("Easing")
local NavInput = require("NavInput")
local Tex = require("tex")
local Caspian = require("caspian")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Look, Motion = Layout.talk, Animation.talk
local Sea = Layout.sea

local S = {}

local EXPR = { idle = "neutral", laugh = "laugh", stern = "grumpy" }
local GRIS_TEX = { idle = "Idle", talk = "Talk", talkworry = "TalkWorry", talkdiscomfort = "TalkDiscomfort",
    discomfort = "Discomfort" }
local REST = { talk = "idle", talkworry = "idle", talkdiscomfort = "discomfort" }   -- her face once she is quiet
local RARITY = { Poor = 0, Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4, Mythical = 4 }
local VOICES = 4
local SILENT = {}
for _, ch in ipairs({ "、", "。", "，", "．", "！", "？", "：", "；", "…", "‥", "・", "「", "」", "『", "』", "（", "）",
    "～", "　", "¡", "¿", "«", "»", "“", "”", "‘", "’" }) do SILENT[ch] = true end

local ctx = nil
local dlg = nil
local co, req = nil, nil          -- the running flow, and its request
local run = nil                   -- the runner of a "say"
local talking = false             -- the dialogue box runs lines
local stageAnim = nil             -- { name, t0 }
local pendingPlay, mounted = nil, false
local returning = false           -- left for a song: the next activate comes back into the talk
local modalOn = false
local spot = nil                  -- where Gris is this visit
local blinkAt = nil               -- when Gris at the window blinks next
local clock = 0
local speaker = "C"
local grisPose = "idle"
local lastVoice = nil

-- the staging, 0 .. 1
local dim, light, wide = 0, 0, 0
local windowA, counterA = 1, 0
local cupK, cupOut = 0, 0
local fadeA, fadeTarget = 0, 0
local boxA, boxTarget = 0, 0
local bgmK, jazzK = 1, 0          -- the vault BGM keeps playing under the talk, ducked to bgmK
local clinked = false

local function play(name, volume)
    local s = ctx.snd[name]
    if s == nil then return end
    if volume then s:SetVolumePercent(volume) end
    s:Play()
end

local function tex(name) return ctx.tex[name] end

-- ── music ───────────────────────────────────────────────────────────────────────────────────────

local function applyMusic()
    local bgm, jazz = ctx.snd.BGM, ctx.snd.Jazz
    if bgm then bgm:SetVolumePercent(Motion.bgm_volume * bgmK) end
    if jazz then jazz:SetVolumePercent(Motion.jazz_volume * jazzK) end
end

local function startJazz()
    local jazz = ctx.snd.Jazz
    if jazz == nil then return end
    jazz:SetLoop(true)
    jazz:SetVolumePercent(0)
    jazz:Play()
end

-- ── dialogue ────────────────────────────────────────────────────────────────────────────────────

local function shownChar(n)
    if dlg == nil or dlg.glines == nil then return nil end
    for _, l in ipairs(dlg.glines) do
        if n <= l.n then return l.chars[n] end
        n = n - l.n
    end
    return nil
end

local function blip()
    if speaker == "N" then return end
    local ch = shownChar(math.floor(dlg.revealed or 0))
    if ch == nil or SILENT[ch] or ch:match("^[%s%p]$") then return end
    local i = math.random(1, VOICES)
    if i == lastVoice then i = i % VOICES + 1 end
    lastVoice = i
    local s = ctx.snd[(speaker == "G" and "GrisBlip" or "VoiceBlip") .. i]
    if s then s:Play() end
end

-- a line starts: its speaker's face, and the screen dims around a faded one
local function onLine(tag)
    local who, sprite, flag = tag:match("^(%a)_(%w*)_?(%a*)$")
    if who == nil then return end
    speaker = who
    if who == "C" then
        if EXPR[sprite] then Caspian.setExpr(EXPR[sprite]) end
        grisPose = REST[grisPose] or grisPose
    elseif who == "G" then
        if GRIS_TEX[sprite] then grisPose = sprite end
    else
        grisPose = REST[grisPose] or grisPose
    end
    fadeTarget = flag == "fade" and 1 or 0
end

local function dlgNode(n)
    local text = n.key and Talk.text(n.key) or ""
    text = text:gsub("[{}]", "")
    local node = {
        name = Talk.name(n.who),
        text = "{expr:" .. n.who .. "_" .. (n.sprite or "") .. (n.fade and "_fade" or "") .. "}" .. text,
    }
    if n.choices then
        node.choices = {}
        for i, c in ipairs(n.choices) do node.choices[i] = { label = (c.label:gsub("[{}]", "")), value = i } end
    end
    return node
end

-- the last line stays in the box while the stage does something else
local function holdLine()
    local last = dlg.node
    if last == nil then return end
    dlg:start({ last })
    dlg.revealed = dlg.total or 0
    dlg.caret = false
end

-- ── the flow ────────────────────────────────────────────────────────────────────────────────────

local resume

local function stepRun()
    local kind, x = run:step()
    if kind == "lines" then
        local nodes = {}
        for i, n in ipairs(x) do nodes[i] = dlgNode(n) end
        dlg:start(nodes)
        dlg.caret = true
        if x[1].shown then dlg.revealed = dlg.total or 0 end
        talking = true
        boxTarget = 1
        return
    end
    talking = false
    if kind == "action" then resume(x) else resume(nil) end
end

local function showReward(r)
    local modal = ROACTIVITY:GetROActivity("modal")
    if modal == nil or r == nil then return false end
    if r.type == "character" then
        local ch = nil
        pcall(function() ch = CHARACTER:CreateCharacter(r.entry.FolderName) end)
        if ch == nil then return false end
        modal:Activate(0, RARITY[r.entry.Rarity] or 3, 1, ch)
        return true
    end
    return Chests.showReward(r)
end

local function begin(r)
    req = r
    local kind = r[1]
    if kind == "say" then
        run = r[2]
        stepRun()
    elseif kind == "stage" then
        stageAnim = { name = r[2], t0 = clock }
        if r[2] == "in" or r[2] == "out" then boxTarget = 0 end
        if r[2] == "in" then clinked = false end
    elseif kind == "play" then
        pendingPlay, mounted = r[2], false
    elseif kind == "reward" then
        if showReward(r[2]) then modalOn = true else resume() end
    end
end

resume = function(...)
    if co == nil then return end
    local ok, r = coroutine.resume(co, ...)
    if not ok then
        if debugLog then debugLog("secret_vault_rw talk: " .. tostring(r)) end
        co, req = nil, nil
        return
    end
    if coroutine.status(co) == "dead" or r == nil then
        co, req = nil, nil
        return
    end
    begin(r)
end

-- ── staging ─────────────────────────────────────────────────────────────────────────────────────

local function updateStage()
    local a = stageAnim
    if a == nil then return end
    local t = clock - a.t0
    local done = false
    if a.name == "in" then
        local k = Tex.clamp01(t / Motion.in_s)
        dim, light = Easing.inOutQuad(k), Easing.inOutQuad(k)
        bgmK = 1 - Tex.clamp01(t / Motion.bgm_fade_s)
        if t >= Motion.jazz_start_s then
            if jazzK == 0 and not a.jazz then a.jazz = true; startJazz() end
            jazzK = Tex.clamp01((t - Motion.jazz_start_s) / Motion.jazz_fade_s)
        end
        cupOut = 0
        cupK = Tex.clamp01((t - Motion.cup_delay_s) / Motion.cup_s)
        if cupK >= 1 and not clinked then clinked = true; play("Clink") end
        done = k >= 1 and cupK >= 1 and jazzK >= 1
    elseif a.name == "out" then
        local k = Tex.clamp01(t / Motion.out_s)
        dim, light = 1 - Easing.inOutQuad(k), 1 - Easing.inOutQuad(k)
        cupOut = Tex.clamp01(t / Motion.cup_out_s)
        jazzK = 1 - Tex.clamp01(t / Motion.jazz_fade_s)
        bgmK = Tex.clamp01(t / Motion.bgm_fade_s)
        done = k >= 1 and jazzK <= 0 and bgmK >= 1
        if done and ctx.snd.Jazz then ctx.snd.Jazz:Stop() end
    elseif a.name == "come" or a.name == "back" then
        local k = Tex.clamp01(t / Motion.come_s)
        local toCounter = a.name == "come"
        local away = Tex.clamp01(k * 2)           -- she fades out where she was ...
        local here = Tex.clamp01(k * 2 - 1)        -- ... then in where she goes
        windowA = toCounter and (1 - away) or here
        counterA = toCounter and here or (1 - away)
        wide = toCounter and Easing.inOutQuad(k) or (1 - Easing.inOutQuad(k))
        if not toCounter and k >= 1 then grisPose = "idle" end
        done = k >= 1
    elseif a.name == "leave" then
        -- she goes to lie down in the back room: gone from the room for the rest of the visit
        local k = Tex.clamp01(t / Motion.come_s)
        counterA, windowA = 1 - k, 0
        wide = 1 - Easing.inOutQuad(k)
        done = k >= 1
        if done then spot, grisPose = nil, "idle" end
    else
        done = true
    end
    applyMusic()
    if done then
        stageAnim = nil
        if a.name ~= "out" then boxTarget = 1 end
        resume()
    end
end

-- ── API ─────────────────────────────────────────────────────────────────────────────────────────

function S.init(c)
    ctx = c
    local Cas = Layout.caspian
    dlg = Dialogue.new{
        gfont = c.fonts.talk, gfontName = c.fonts.name,
        ui = "popui", theme = Config.plain(Cas.theme), box = Config.plain(Cas.box), portraitSize = Cas.portrait_size,
        growLines = 2,
        cps = Animation.caspian.chars_per_s,
        sfx = { click = function() if c.snd.Decide then c.snd.Decide:Play() end end },
        onExpr = onLine, onBlip = blip, blipEvery = Motion.blip_every,
        advanceInput = function() return NavInput.cancel() end,
    }
end

function S.setNameFont(f) if dlg then dlg.gfontName = f end end

function S.dispose() if dlg then dlg:dispose() end end

-- a new visit: where Gris is
function S.visit()
    Talk.slot = 0
    Talk.applyTrust()
    spot = Talk.rollSpot()
    blinkAt = nil
    windowA, counterA, wide = 1, 0, 0
    grisPose = "idle"
end

function S.reset()
    co, req, run, talking, stageAnim, pendingPlay, mounted, modalOn = nil, nil, nil, false, nil, nil, false, false
    dim, light, cupK, cupOut, fadeA, fadeTarget, boxA, boxTarget = 0, 0, 0, 0, 0, 0, 0, 0
    bgmK, jazzK = 1, 0
    if dlg then dlg.activeFlag = false end
end

-- after a chest that opened (1 simple, 2 gold, 3 optk): Mr.Caspian may offer a talk
function S.offer(chest)
    Talk.slot = 0
    if not Talk.wantOffer(chest) then return false end
    co = Talk.flow(Talk.vaultTalk, spot)
    speaker = "C"
    resume()
    return co ~= nil
end

function S.active() return co ~= nil or stageAnim ~= nil end

-- while a talk runs, it sets the music's volumes (the reward modal's ducking waits)
function S.ownsMusic() return S.active() end

-- "done" once the talk is over, "play" when the stage has to leave for a song (see S.mountPending)
function S.update(dt, now)
    clock = now
    boxA = Tex.approach(boxA, boxTarget, dt / Animation.ui_fade_s)
    fadeA = Tex.approach(fadeA, fadeTarget, dt / Motion.fade_s)
    if co == nil and stageAnim == nil then
        talking = false
        boxTarget = 0
        return "done"
    end
    if stageAnim then
        updateStage()
    elseif modalOn then
        local modal = ROACTIVITY:GetROActivity("modal")
        if modal then modal:Update() end
        if modal == nil or not modal.IsActive then modalOn = false; resume() end
    elseif pendingPlay then
        return "play"
    elseif talking then
        if dlg:update(dt) == "done" then
            if run.asking then run:answer(dlg.result) end
            holdLine()
            stepRun()
        end
    end
    applyMusic()
    return nil
end

-- mounts the song asked for; false when it cannot be played (the talk goes on as if it failed)
function S.mountPending()
    local node = pendingPlay
    pendingPlay = nil
    if node ~= nil and Talk.mount(node) then
        returning = true
        return true
    end
    resume(false)
    return false
end

function S.returning() return returning end

-- back from the song: the talk goes on where it left, the staging as it was
function S.afterPlay()
    returning = false
    blinkAt = nil
    local cleared = Talk.cleared()
    dim, light, wide, windowA, counterA = 1, 1, 1, 0, 1
    cupK, cupOut, boxA, boxTarget = 1, 0, 0, 1
    bgmK, jazzK = 0, 1
    -- the trip stopped the vault BGM: it plays again from the top, ducked until the talk ends
    local bgm = ctx.snd.BGM
    if bgm then bgm:SetLoop(true); bgm:SetVolumePercent(0); bgm:Play() end
    startJazz()
    applyMusic()
    speaker = "G"
    grisPose = "idle"
    resume(cleared)
end

-- ── drawing ─────────────────────────────────────────────────────────────────────────────────────

local function fill(op, y0)
    local w = tex("Talk/White")
    if not Tex.ok(w) or op <= 0 then return end
    y0 = y0 or 0
    w:SetColor(0, 0, 0)
    Tex.place(w, 0, y0, "topleft", Tex.SW / w.Width, (Tex.SH - y0) / w.Height, op)
    w:SetColor(1, 1, 1)
end

local function glow(t, x, y, sx, sy, op, color)
    if not Tex.ok(t) or op <= 0 then return end
    t:SetBlendMode("Add")
    t:SetColor(color[1] / 255, color[2] / 255, color[3] / 255)
    Tex.place(t, x, y, "center", sx, sy, op)
    t:SetColor(1, 1, 1)
    t:SetBlendMode("Normal")
end

-- Gris swimming in the tank, behind the glass (cabin.lua calls it with its projection)
function S.drawTank(project)
    if spot ~= "tank" then return end
    local t = tex("Cabin/Gris/Tank")
    if not Tex.ok(t) then return end
    local G = Look.gris_tank
    local ph = clock * 2 * math.pi / Motion.tank_period_s
    local X = G.side * (Sea.half_w + G.d + G.drift * math.sin(ph * 0.5))
    local x, y = project(X, G.y + G.bob * math.sin(ph), G.z)
    local k = G.scale / G.z
    t:SetColor(G.shade, G.shade, G.shade)
    Tex.place(t, x, y, "center", k, k, 1)
    t:SetColor(1, 1, 1)
end

-- what stands in the room: Gris at the side window (blinking: her closed eye drawn over the open one), then the
-- dim and the light spot over Mr.Caspian
function S.drawRoom(now)
    if spot == "window" and windowA > 0 then
        local t, W = tex("Cabin/Gris/StandOpen"), Look.gris_window
        Tex.place(t, W.x, W.y, "bottom", W.scale, W.scale, windowA)
        if blinkAt == nil or now < blinkAt - Motion.blink_every_s[2] then
            blinkAt = now + Tex.lerp(Motion.blink_every_s[1], Motion.blink_every_s[2], math.random())
        end
        if now >= blinkAt + Motion.blink_s then
            blinkAt = now + Tex.lerp(Motion.blink_every_s[1], Motion.blink_every_s[2], math.random())
        end
        local eye = tex("Cabin/Gris/StandBlink")
        if now >= blinkAt and Tex.ok(t) and Tex.ok(eye) then
            local left, top = W.x - t.Width * W.scale / 2, W.y - t.Height * W.scale
            Tex.place(eye, left + W.blink_x * W.scale, top + W.blink_y * W.scale, "topleft", W.scale, W.scale, windowA)
        end
    end
    fill(dim * Look.dim)
    local sp, gl = Look.spot, Look.glow
    if light > 0 then
        local cone = tex("Talk/Spotlight")
        if Tex.ok(cone) then
            local x = Tex.lerp(sp.x, sp.wide_x, wide)
            local w = Tex.lerp(sp.w, sp.wide_w, wide)
            cone:SetBlendMode("Add")
            cone:SetColor(sp.color[1] / 255, sp.color[2] / 255, sp.color[3] / 255)
            Tex.place(cone, x, sp.top, "top", w / cone.Width, sp.h / cone.Height, sp.opacity * light)
            cone:SetColor(1, 1, 1)
            cone:SetBlendMode("Normal")
        end
        local g = tex("Talk/Glow")
        if Tex.ok(g) then
            local size = Tex.lerp(gl.size, gl.wide_size, wide)
            glow(g, Tex.lerp(gl.x, gl.wide_x, wide), gl.y, size / g.Width, size / g.Height, gl.opacity * light, gl.color)
        end
    end
end

-- Gris at the counter, on Mr.Caspian's left (behind the counter, like him)
function S.drawCounter(now)
    if counterA <= 0 then return end
    local t = tex("Cabin/Gris/" .. (GRIS_TEX[grisPose] or "Idle"))
    if not Tex.ok(t) then t = tex("Cabin/Gris/Idle") end
    local G = Look.gris_counter
    local breath = 1 + Motion.breath_amp * math.sin(now * 2 * math.pi / Motion.breath_s)
    Tex.place(t, G.x, G.y, "bottom", G.scale, G.scale * breath, counterA)
end

-- in front of the counter: its dim, the light on it, and the cup
function S.drawFront(now)
    fill(dim * Look.dim, Look.table_dim_y)
    local g, cg = tex("Talk/Glow"), Look.counter_glow
    if Tex.ok(g) and light > 0 then
        glow(g, Tex.lerp(cg.x, cg.wide_x, wide), cg.y, cg.w / g.Width, cg.h / g.Height, cg.opacity * light, Look.glow.color)
    end
    local cup = tex("Talk/Cup")
    if Tex.ok(cup) and cupK > 0 then
        local C = Look.cup
        local x
        if cupOut > 0 then
            x = Tex.lerp(C.x, C.from_x, Easing.inCubic(cupOut))
        else
            x = Tex.lerp(C.from_x, C.x, Easing.outCubic(cupK))
        end
        Tex.place(cup, x, C.y, "bottom", C.scale, C.scale, 1)
    end
end

-- the dialogue box (and a faded line's dark screen, and a reward's modal)
function S.drawBox()
    fill(fadeA * Look.fade)
    if dlg ~= nil and dlg.activeFlag and boxA > 0 then
        dlg.opacity = boxA
        dlg:draw()
    end
    if modalOn then
        local modal = ROACTIVITY:GetROActivity("modal")
        if modal then modal:Draw() end
    end
end

return S
