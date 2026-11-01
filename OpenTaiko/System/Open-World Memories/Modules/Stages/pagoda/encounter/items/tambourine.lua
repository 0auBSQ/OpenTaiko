-- Ume's Tambourine effect: the floor's songs are rerolled while the song bars are off screen.

local Easing   = require("Easing")
local assets   = require("encounter/assets")
local songbars = require("encounter/songbars")
local reveal   = require("encounter/song_reveal")
local Config   = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.items.tambourine
local Anim = Animation.encounter.items.tambourine

local CX, CY = Look.center.x, Look.center.y
local POP_SEC, POP_BACK, SIZE = Anim.pop_s, Anim.pop_back, Look.size
local SHAKE_DEG, SHAKE_HZ, SHAKE_SEC = Anim.shake_deg, Anim.shake_hz, Anim.shake_s
local GONE_AT, GONE_SEC = Anim.gone_at_s, Anim.gone_s
local WAVES, WAVE_EVERY, WAVE_AT = Anim.waves, Anim.wave_every_s, Anim.wave_at_s
local WAVE_SPEED, WAVE_LIFE, WAVE_R, WAVE_TURN = Anim.wave_speed_px_per_s, Anim.wave_life_s, 230, Anim.wave_turn_deg_per_s   -- px/s, s, art radius px, deg/s
local WAVE_BREATH, WAVE_BREATH_HZ = Anim.wave_breath, Anim.wave_breath_hz
local WAVE_A = Look.wave_opacity
local GLINTS, GLINT_R = Look.glints, Anim.glint_r_px
local WOBBLE_AT, LEAVE_AT = Anim.wobble_at_s, Anim.leave_at_s
local HALO_SCALE, HALO_A = Look.halo_scale, Look.halo_opacity
local SHAKE_PX, SHAKE_PX_SEC = Anim.screen_shake_px, Anim.screen_shake_s
local GOLD = Look.gold_color
local SFX = "Sounds/result/item_tambourine.ogg"

local _tex = {}
local _sfx = nil
local _on = false
local _t = 0
local _reroll, _shake, _level = nil, nil, 1
local _step = "play"                               -- play | away | back | over
local _wobbled, _left = false, false

local function ready(tx) return tx ~= nil and tx.Ready end

local function reset(tx)
    tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
    tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

function M.keys() return { "item_tambourine", "items_wave", "fx_glow", "fx_glint" } end

-- grabs the visit's handles and sound (encounter/assets opens the keys); level: the floor;
-- reroll(keepMystery) -> the floor's new roll; shake(px, sec) shakes the screen
function M.open(level, reroll, shake)
    M.close()
    _level, _reroll, _shake = level, reroll, shake
    for _, k in ipairs(M.keys()) do _tex[k] = (assets.get(k)) end
    local ok, s = pcall(function() return SOUND:CreateSFX(SFX) end)
    if ok then _sfx = s end
end

function M.start()
    _on, _t, _step = true, 0, "play"
    _wobbled, _left = false, false
    if _sfx ~= nil then _sfx:Play() end
    if _shake ~= nil then _shake(SHAKE_PX, SHAKE_PX_SEC) end
end

-- true while the menu waits for it
function M.busy() return _on and _step ~= "over" end

-- true while any of it is on screen
function M.active() return _on end

function M.update(dt)
    if not _on then return end
    _t = _t + dt
    if not _wobbled and _t >= WOBBLE_AT then _wobbled = true ; songbars.wobble() end
    if not _left and _t >= LEAVE_AT then _left = true ; songbars.leave() end
    if _step == "play" and _left and songbars.gone() then
        -- the destiny level keeps its mystery song
        songbars.reroll(_reroll ~= nil and _reroll(_level == reveal.DESTINY_LEVEL) or nil)
        _step = "back"
    elseif _step == "back" and songbars.settled() then
        _step = "over"
    end
    if _step == "over" and _t >= WAVE_AT + (WAVES - 1) * WAVE_EVERY + WAVE_LIFE then _on = false end
end

local function drawAt(tx, x, y, sx, sy, a, c, blend, rot)
    if not ready(tx) or a <= 0.003 then return end
    tx:SetScale(sx, sy)
    if rot ~= nil then tx:SetRotation(rot) end
    tx:SetOpacity(math.min(1, a))
    if c ~= nil then tx:SetColor(c[1], c[2], c[3]) end
    if blend ~= nil then tx:SetBlendMode(blend) end
    tx:DrawAtAnchor(x, y, "center")
    reset(tx)
end

-- drawn over the song bars
function M.drawOver()
    if not _on then return end
    local wave, glow, glint = _tex.items_wave, _tex.fx_glow, _tex.fx_glint
    for k = 1, WAVES do
        local age = _t - WAVE_AT - (k - 1) * WAVE_EVERY
        if age > 0 and age < WAVE_LIFE then
            local r = WAVE_SPEED * age * (1 - Anim.wave_slow * age / WAVE_LIFE)
            local swell = 1 + WAVE_BREATH * math.sin(age * WAVE_BREATH_HZ * 2 * math.pi + k)
            local s = r / WAVE_R
            local fade = (1 - age / WAVE_LIFE) ^ Anim.wave_fade_power * (1 - Anim.wave_fade_step * (k - 1))
            local turn = ((k % 2 == 0) and -1 or 1) * WAVE_TURN * age + k * Anim.wave_turn_step_deg
            drawAt(wave, CX, CY, s * swell, s / swell, WAVE_A * fade, GOLD, "Add", turn)
        end
    end
    local pop = Easing.outBack(_t / POP_SEC, POP_BACK)
    local gone = 1 - Easing.inBack((_t - GONE_AT) / GONE_SEC, Anim.gone_back)
    local size = SIZE * pop * math.max(0, gone)
    if size <= 0.01 then return end
    local a = math.min(1, _t / Anim.fade_in_s) * math.max(0, gone)
    drawAt(glow, CX, CY, HALO_SCALE * size / SIZE, HALO_SCALE * size / SIZE, HALO_A * a, GOLD, "Add")
    for g = 1, GLINTS do
        local ang = g * 2 * math.pi / GLINTS + _t * Anim.glint_orbit_freq
        local d = GLINT_R * Easing.outCubic(_t / Anim.glint_out_s) * size / SIZE
        local tw = Anim.glint_scale_min + Anim.glint_twinkle * math.sin(_t * Anim.glint_twinkle_freq + g * Anim.glint_twinkle_phase)
        drawAt(glint, CX + d * math.cos(ang), CY + d * math.sin(ang), tw, tw, a, GOLD, "Add",
            _t * Anim.glint_spin_deg_per_s + g * Anim.glint_turn_step_deg)
    end
    local damp = math.max(0, 1 - _t / SHAKE_SEC)
    local rot = SHAKE_DEG * damp * math.sin(_t * SHAKE_HZ * 2 * math.pi)
    drawAt(_tex.item_tambourine, CX, CY, size, size, a, nil, nil, rot)
end

function M.close()
    if _sfx ~= nil then _sfx:Stop() ; _sfx:Dispose() ; _sfx = nil end
    _tex, _on = {}, false
end

return M
