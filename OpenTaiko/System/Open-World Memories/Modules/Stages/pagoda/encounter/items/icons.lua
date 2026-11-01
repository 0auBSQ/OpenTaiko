-- Item icons at the bottom left of the encounter screen, in encounter/items/state order.

local Easing = require("Easing")
local assets = require("encounter/assets")
local pill   = require("encounter/ui_pill")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.encounter.items.icons
local Anim = Animation.encounter.items.icons

local SLOT_X0, SLOT_DX, ICON_Y = Look.slot_x0, Look.slot_dx, Look.icon_y   -- first icon centre, step to the next
local ICON_SCALE = Look.icon_scale
local SHADOW_SCALE, SHADOW_A = Look.shadow_scale, Look.shadow_opacity
local SHADOW_GREY_A = Look.shadow_grey_opacity  -- under a spent item
local RIM_PX, RIM_A = Look.rim_px, Look.rim_opacity   -- spent item rim: offset px, opacity per pass
local RIM = { { -1, -1 }, { 1, -1 }, { -1, 1 }, { 1, 1 } }
local GLOW_SCALE = Look.glow_scale
local HALO_R, HALO_A, HALO_HZ = Look.halo_r_px, Look.halo_opacity, Anim.halo_hz   -- focused icon halo: radius px, opacity, Hz
local POP_SEC, POP_STAGGER, POP_BACK = Anim.pop_s, Anim.pop_stagger_s, Anim.pop_back
local GREY = Look.grey_color
local GREY_A = Look.grey_opacity
local TURN_SEC = Anim.turn_s                    -- grey to colour and back
local FLASH_SEC, FLASH_A = Anim.flash_s, Look.flash_opacity
local REFILL_DELAY = Anim.refill_delay_s        -- s after the icon is in
local NEW_SEC = Anim.new_s                      -- new item burst
local BURST_GLINTS, BURST_R = Look.burst_glints, Anim.burst_r_px
local RING_R = 246                              -- items_ring's ring radius, px
local SHAKE_SEC, SHAKE_PX, SHAKE_HZ = Anim.shake_s, Anim.shake_px, Anim.shake_freq

local _slots = {}
local _byId = {}
local _t = 0
local _shown = false
local _tex = {}

local function ready(tx) return tx ~= nil and tx.Ready end

local function reset(tx)
    tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
    tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

local function icon(id) return "item_" .. id end
local function mask(id) return "item_" .. id .. "_mask" end

-- encounter/assets keys of the icons of ids
function M.keys(ids)
    local keys = { "fx_glow", "fx_glint", "items_ring", "ui_marker" }
    for _, id in ipairs(ids or {}) do
        keys[#keys + 1] = icon(id)
        keys[#keys + 1] = mask(id)
    end
    return keys
end

local function newSlot(id, isReady)
    local s = { id = id, ready = isReady, grey = isReady and 0 or 1, showAt = nil,
        flashT = nil, newT = nil, shakeT = nil, refillAt = nil }
    _slots[#_slots + 1] = s
    _byId[id] = s
    return s
end

local function fetch()
    for _, s in ipairs(_slots) do
        for _, k in ipairs({ icon(s.id), mask(s.id) }) do
            if _tex[k] == nil then _tex[k] = (assets.get(k)) end
        end
    end
    for _, k in ipairs({ "fx_glow", "fx_glint", "items_ring", "ui_marker" }) do
        if _tex[k] == nil then _tex[k] = (assets.get(k)) end
    end
end

-- ids: the player's items; isReady(id) -> bool; refilled: items a replenish just made ready
function M.open(ids, isReady, refilled)
    M.close()
    for _, id in ipairs(ids or {}) do newSlot(id, isReady(id)) end
    for _, id in ipairs(refilled or {}) do
        local s = _byId[id]
        if s ~= nil then s.ready, s.grey, s.refillAt = false, 1, true end
    end
    fetch()
end

-- adds a just-presented item, ready
function M.add(id)
    if _byId[id] == nil then newSlot(id, true) end
    fetch()
end

-- new (optional): set of ids shown with the new item burst
function M.show(new)
    if _shown then return end
    _shown = true
    for i, s in ipairs(_slots) do
        s.showAt = _t + (i - 1) * POP_STAGGER
        if s.refillAt == true then s.refillAt = s.showAt + POP_SEC + REFILL_DELAY end
        if new ~= nil and new[s.id] then s.newT = -(i - 1) * POP_STAGGER end
    end
end

function M.shown() return _shown end

-- the item was used
function M.spend(id)
    local s = _byId[id]
    if s == nil then return end
    s.ready, s.flashT = false, 0
end

-- the item cannot be used now
function M.deny(id)
    local s = _byId[id]
    if s ~= nil then s.shakeT = 0 end
end

function M.update(dt, refilled)
    _t = _t + dt
    for _, s in ipairs(_slots) do
        if type(s.refillAt) == "number" and _t >= s.refillAt then
            s.refillAt, s.ready, s.flashT = nil, true, 0
            if refilled ~= nil then refilled(s.id) end
        end
        local want = s.ready and 0 or 1
        if s.grey < want then s.grey = math.min(want, s.grey + dt / TURN_SEC)
        elseif s.grey > want then s.grey = math.max(want, s.grey - dt / TURN_SEC) end
        if s.flashT ~= nil then
            s.flashT = s.flashT + dt
            if s.flashT >= FLASH_SEC then s.flashT = nil end
        end
        if s.newT ~= nil then
            s.newT = s.newT + dt
            if s.newT >= NEW_SEC then s.newT = nil end
        end
        if s.shakeT ~= nil then
            s.shakeT = s.shakeT + dt
            if s.shakeT >= SHAKE_SEC then s.shakeT = nil end
        end
    end
end

local function drawTex(tx, x, y, s, a, c, blend, rot)
    if not ready(tx) or a <= 0.003 then return end
    tx:SetScale(s, s)
    if rot ~= nil then tx:SetRotation(rot) end
    tx:SetOpacity(math.min(1, a))
    if c ~= nil then tx:SetColor(c[1], c[2], c[3]) end
    if blend ~= nil then tx:SetBlendMode(blend) end
    tx:DrawAtAnchor(x, y, "center")
    reset(tx)
end

local GOLD = Look.gold_color
local WHITE = Look.flash_color
local BLACK = Look.shadow_color
local RIM_COLOR = Look.rim_color

-- new item burst
local function burst(s, x, y)
    local u = s.newT / NEW_SEC
    local ring = _tex.items_ring
    local r = Anim.burst_ring_from_px + Anim.burst_ring_grow_px * Easing.outCubic(u)
    drawTex(ring, x, y, r / RING_R, (1 - u) * Look.burst_ring_opacity, GOLD, "Add")
    local glint = _tex.fx_glint
    for k = 1, BURST_GLINTS do
        local a = k * 2 * math.pi / BURST_GLINTS + u * Anim.burst_turn_rad
        local d = BURST_R * Easing.outCubic(u)
        local gs = Anim.burst_glint_scale * (1 - u) + Anim.burst_glint_scale_min
        drawTex(glint, x + d * math.cos(a), y + d * math.sin(a), gs, (1 - u), GOLD, "Add", u * Anim.burst_glint_spin_deg)
    end
end

-- focus: the focused item's id or nil
function M.draw(focus)
    if not _shown then return end
    local glow, marker = _tex.fx_glow, _tex.ui_marker
    for i, s in ipairs(_slots) do
        if s.showAt ~= nil and _t >= s.showAt then
            local u = (_t - s.showAt) / POP_SEC
            local pop = Easing.outBack(u, POP_BACK)
            local alpha = math.min(1, u * 2)
            local x = SLOT_X0 + (i - 1) * SLOT_DX
            if s.shakeT ~= nil then
                x = x + SHAKE_PX * (1 - s.shakeT / SHAKE_SEC) * math.sin(s.shakeT * SHAKE_HZ)
            end
            local y = ICON_Y
            local focused = focus == s.id
            local sc = ICON_SCALE * pop
            if focused then sc = pill.bounce(sc, _t) end
            drawTex(glow, x, y, SHADOW_SCALE * pop, (SHADOW_A + (SHADOW_GREY_A - SHADOW_A) * s.grey) * alpha, BLACK)
            if focused then
                local k = alpha * (1 - Look.focus_grey_dim * s.grey)
                pill.glowAt(glow, x, y, GLOW_SCALE * pop, GLOW_SCALE * pop, k)
                local r = HALO_R * pop * (1 + Anim.halo_pulse * math.sin(_t * HALO_HZ * 2 * math.pi))
                drawTex(_tex.items_ring, x, y, r / RING_R, HALO_A * k, GOLD, "Add")
            end
            if s.newT ~= nil then burst(s, x, y) end
            if s.grey > 0 then
                for _, o in ipairs(RIM) do
                    drawTex(_tex[mask(s.id)], x + o[1] * RIM_PX, y + o[2] * RIM_PX, sc, alpha * RIM_A * s.grey, RIM_COLOR)
                end
            end
            drawTex(_tex[icon(s.id)], x, y, sc, alpha * (1 - s.grey))
            drawTex(_tex[mask(s.id)], x, y, sc, alpha * GREY_A * s.grey, GREY)
            if s.flashT ~= nil then
                local f = 1 - s.flashT / FLASH_SEC
                drawTex(_tex[mask(s.id)], x, y, sc, alpha * FLASH_A * f * f, WHITE, "Add")
            end
            if focused then
                local tx = _tex[icon(s.id)]
                local half = (tx ~= nil and tx.Height or Look.icon_h_fallback) * sc / 2
                pill.markerAbove(marker, x, y - half, 1, _t, alpha)
            end
        end
    end
end

function M.close()
    _slots, _byId, _tex = {}, {}, {}
    _shown, _t = false, 0
end

return M
