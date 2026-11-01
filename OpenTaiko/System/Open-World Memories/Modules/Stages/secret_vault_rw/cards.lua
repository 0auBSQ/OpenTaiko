---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
local Tex = require("tex")
local Easing = require("Easing")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Cards, Chip, Stock = Layout.cards, Layout.cards.chip, Layout.cards.stock
local Motion = Animation.cards

local K = {}

local N = 4
local CARD_Y = Cards.y
local SPREAD1, SPREAD2 = Cards.spread1, Cards.spread2
local ROLL_SPEED = Motion.roll_rate_per_s
local FACE_W = Cards.face_w
local TITLE_Y, CHEST_Y, CHEST_K = Cards.title_y, Cards.chest_y, Cards.chest_scale
local STOCK_Y, STOCK_H = Stock.y, Stock.h
local CHIP_Y = Cards.chip_y
local LEAVE_LABEL_Y = Cards.leave_label_y
local CAP = Chip.cap                             -- UI/Chip.png's end caps (its middle stretches between them)
local ICON_PX = Chip.icon_px                     -- a key icon on a chip
local PAD_L, GAP, PAD_R = Chip.pad_l, Chip.gap, Chip.pad_r   -- inside a chip: left edge, icon, gap, count, right edge
local Top = Layout.cards.top_chips
local TOP_Y, TOP_RIGHT, TOP_GAP = Top.y, Top.right, Top.gap   -- the top right chips

-- the stock messages: the share of songs and nameplates still to get in the chest picks one (their look:
-- Layout.cards.stock.kinds)
local STOCK = {
    plenty  = { key = "VAULT_STOCK_PLENTY", en = "There are plenty of items left to get on this chest" },
    various = { key = "VAULT_STOCK_VARIOUS", en = "There are various items left to get on this chest" },
    few     = { key = "VAULT_STOCK_FEW", en = "There are few items left to get on this chest" },
    none    = { key = "VAULT_STOCK_NONE", en = "There is nothing left to get on this chest" },
}
for kind, st in pairs(STOCK) do
    local look = Stock.kinds[kind]
    st.grad = Config.optional(look, "grad")
    st.fore = look.fore
    st.back = Config.optional(look, "back")
    st.shadow = Config.optional(look, "shadow")
    st.k = look.scale
end

local CHEST_NAMES = {
    { "VAULT_CHEST_1", "Wooden Chest" },
    { "VAULT_CHEST_2", "Golden Chest" },
    { "VAULT_CHEST_3", "Platinum Chest" },
}

local INK = Cards.ink_color                      -- brown ink on the parchment
local RED_INK = Cards.red_ink_color
local function ink(c, k)
    k = k or 1
    return Tex.col(math.floor(c[1] * k), math.floor(c[2] * k), math.floor(c[3] * k))
end

local ctx = nil
local sel, pos = 2, 2
local shakeT = nil
local confirm, confirmT = false, nil
local popT = 0

function K.init(c) ctx = c end

function K.reset(now, index)
    sel = index or sel
    pos = sel
    shakeT, confirm, confirmT = nil, false, nil
    popT = now
end

-- the cards pop in again (the menu coming back)
function K.pop(now) popT = now end

function K.move(dir) sel = (sel - 1 + dir) % N + 1 end
function K.select(index) sel = index end
function K.selected() return sel end
function K.chest() return sel > 1 and (sel - 1) or nil end
function K.shake(now) shakeT = now end
function K.setConfirm(on, now) confirm, confirmT = on, now end

-- an offset between card places, into [-N/2, N/2)
local function wrap(d) return (d + N / 2) % N - N / 2 end

function K.tick(dt)
    local d = wrap(sel - pos)
    if math.abs(d) < 0.002 then pos = sel; return end
    pos = pos + d * math.min(1, dt * ROLL_SPEED)
end

function K.stockKind(left, total)
    if left == nil or left <= 0 or total == nil or total <= 0 then return "none" end
    local share = left / total
    if share >= 0.7 then return "plenty" end
    if share >= 0.3 then return "various" end
    return "few"
end

-- one line of text centred on f, shrunk to fit w
local function line(font, str, f, fore, back, w, op, k)
    local s = (k or 1) * Tex.fitScale(font, str, w / (k or 1))
    Tex.text(font, str, f, fore, back, s, w, "center", op)
end

local function drawStock(f, left, total, op)
    local st = STOCK[K.stockKind(left, total)]
    local font = ctx.fonts.small
    local text = Tex.tr(st.key, st.en)
    local lines, k, pitch = Tex.fitBox(font, text, FACE_W, STOCK_H, st.k, Stock.min_scale)
    local y0 = -(#lines - 1) * pitch / 2
    local fore = Tex.col(st.fore[1], st.fore[2], st.fore[3])
    local back = st.back and Tex.col(st.back[1], st.back[2], st.back[3]) or Tex.clear()
    for j, l in ipairs(lines) do
        local lf = Tex.child(f, 0, y0 + (j - 1) * pitch)
        if st.shadow then
            local Sh = Stock.shadow
            local sc = Tex.col(Sh.color[1], Sh.color[2], Sh.color[3])
            Tex.text(font, l, Tex.child(lf, Sh.x, Sh.y), sc, sc, k, FACE_W, "center", Sh.opacity * op)
        end
        Tex.text(font, st.grad and string.format(st.grad, l) or l, lf, fore, back, k, FACE_W, "center", op)
    end
end

-- a key chip: the pill, key i's icon and the number owned, laid out from the count's width; returns its width
local function chipWidth(count)
    return PAD_L + ICON_PX + GAP + Tex.measure(ctx.fonts.text, "x" .. Tex.int(count)) + PAD_R
end

local function chip(f, i, count, op, shade)
    local text = "x" .. Tex.int(count)
    local w = chipWidth(count)
    Tex.pill(ctx.tex["UI/Chip"], f, w, CAP, op, shade)
    local icon = ctx.tex["UI/Key" .. i]
    if Tex.ok(icon) and icon.Width > 0 then
        Tex.sprite(icon, Tex.child(f, -w / 2 + PAD_L + ICON_PX / 2, 0), ICON_PX / (icon.Width - Chip.icon_art_pad), nil, op, shade)
    end
    local col = ink((tonumber(count) or 0) > 0 and INK or RED_INK, shade)
    local tw = Tex.measure(ctx.fonts.text, text)
    Tex.text(ctx.fonts.text, text, Tex.child(f, -w / 2 + PAD_L + ICON_PX + GAP + tw / 2, 0), col, Tex.clear(), 1, 0,
        "center", op)
    return w
end

local function drawLeave(f, op, shade)
    Tex.sprite(ctx.tex["UI/CardLeave"], f, 1, 1, op, shade)
    line(ctx.fonts.title, Tex.tr("VAULT_LEAVE", "Leave"), Tex.child(f, 0, LEAVE_LABEL_Y), ink(INK), Tex.clear(),
        FACE_W, op)
end

local function drawChest(f, c, op, shade, now, front, d)
    Tex.sprite(ctx.tex["UI/Card"], f, 1, 1, op, shade)
    if front and confirm then
        local P = Motion.confirm_pulse
        local a = confirmT and (now - confirmT) or 1
        local pulse = 1 + P.amp * math.sin(now * 2 * math.pi * P.hz) + P.pop * (1 - Easing.outBack(a / P.pop_s, P.overshoot))
        line(ctx.fonts.title, Tex.tr("VAULT_CONFIRM", "Open this chest?"), Tex.child(f, 0, TITLE_Y),
            ink(RED_INK), Tex.clear(), FACE_W / Cards.confirm_squeeze, op, pulse)
    else
        local nm = CHEST_NAMES[c]
        line(ctx.fonts.title, Tex.tr(nm[1], nm[2]), Tex.child(f, 0, TITLE_Y), ink(INK), Tex.clear(), FACE_W, op)
    end
    Tex.sprite(ctx.tex["Chest/Chest" .. c], Tex.child(f, 0, CHEST_Y), CHEST_K, CHEST_K, op, shade)
    local info = d or {}
    drawStock(Tex.child(f, 0, STOCK_Y), info.left, info.total, op * (Stock.dim_opacity + (1 - Stock.dim_opacity) * shade))
    chip(Tex.child(f, 0, CHIP_Y), c, info.keys, op, shade)
end

function K.draw(now, alpha, info)
    if alpha <= 0 then return end
    local order = {}
    for i = 1, N do
        local d = wrap(i - pos)
        order[#order + 1] = { i = i, d = d, a = math.abs(d) }
    end
    table.sort(order, function(p, q) return p.a > q.a end)

    local enter = Easing.outBack(Tex.clamp01((now - popT) / Motion.enter_s), Motion.enter_overshoot)
    local Shake, Lift = Motion.shake, Motion.confirm_lift
    for _, o in ipairs(order) do
        local a, d = o.a, o.d
        local side = d < 0 and -1 or 1
        local x = Cards.center_x + side * (a <= 1 and SPREAD1 * a or SPREAD1 + SPREAD2 * (a - 1))
        local k = (a <= 1) and (1 - Cards.near_shrink * a) or (Cards.far_scale - Cards.far_shrink * (a - 1))
        local shade = 1 - Cards.side_shade * math.min(1, a)
        local op = alpha * ((a <= Cards.fade_from) and 1 or math.max(0, 1 - (a - Cards.fade_from) / Cards.fade_over))
        local y = CARD_Y + Cards.side_drop * math.min(1, a) ^ 2 + (1 - alpha) * Motion.hide_drop_px
        local rot = Cards.rot_deg * d
        local front = o.i == sel and a < 0.5
        if front then
            if shakeT ~= nil then
                local s = now - shakeT
                if s < Shake.dur_s then x = x + Shake.amp * math.exp(-Shake.decay * s) * math.sin(Shake.rad_per_s * s) end
            end
            if confirm then
                local c = confirmT and (now - confirmT) or 1
                local lift = Easing.outBack(Tex.clamp01(c / Lift.s), Lift.overshoot)
                y, k = y - Lift.rise_px * lift, k * (1 + Lift.grow * lift)
            end
        end
        k = k * (Motion.enter_start_scale + (1 - Motion.enter_start_scale) * enter)
        if op > 0 then
            local f = Tex.frame(x, y, rot, k)
            if o.i == 1 then drawLeave(f, op, shade)
            else drawChest(f, o.i - 1, op, shade, now, front, info and info[o.i - 1]) end
        end
    end
end

-- the top right chips, right-aligned, each as wide as its count needs
local function topChips(keys)
    local xs, x = {}, TOP_RIGHT
    for i = 3, 1, -1 do
        local w = chipWidth(keys and keys[i] or 0)
        xs[i] = { x = x - w / 2, w = w }
        x = x - w - TOP_GAP
    end
    return xs
end

function K.drawChips(now, alpha, keys, focus)
    if alpha <= 0 then return end
    local xs = topChips(keys)
    for i = 1, 3 do
        local Focus = Motion.focus_chip
        local k = (focus == i) and (Focus.scale + Focus.amp * math.sin(now * 2 * math.pi)) or 1
        chip(Tex.frame(xs[i].x, TOP_Y, 0, k), i, keys and keys[i] or 0, alpha, 1)
    end
end

return K
