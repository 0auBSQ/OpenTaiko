---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
local Tex = require("tex")
local Easing = require("Easing")

local K = {}

local N = 4
local CARD_Y = 452
local SPREAD1, SPREAD2 = 440, 330
local ROLL_SPEED = 11
local FACE_W = 316
local TITLE_Y, CHEST_Y, CHEST_K = -212, -70, 0.8
local STOCK_Y, STOCK_H = 100, 116
local CHIP_Y = 206
local LEAVE_LABEL_Y = 90
local CAP = 42                                   -- UI/Chip.png's end caps (its middle 60 px stretch between them)
local ICON_PX = 50                               -- a key icon on a chip
local PAD_L, GAP, PAD_R = 16, 8, 24              -- inside a chip: left edge, icon, gap, count, right edge
local TOP_Y, TOP_RIGHT, TOP_GAP = 60, 1890, 14   -- the top right chips

-- the stock messages: the share of songs and nameplates still to get in the chest picks one (light colours with a
-- dark outline and a soft shadow on the parchment; nothing left is plain grey and smaller)
local STOCK = {
    plenty  = { key = "VAULT_STOCK_PLENTY", en = "There are plenty of items left to get on this chest",
                grad = "<g.#A8DCFF.#3482E2>%s</g>", fore = { 255, 255, 255 }, back = { 10, 32, 78 }, shadow = true, k = 1 },
    various = { key = "VAULT_STOCK_VARIOUS", en = "There are various items left to get on this chest",
                grad = "<g.#C4F2AA.#3A9E45>%s</g>", fore = { 255, 255, 255 }, back = { 10, 46, 18 }, shadow = true, k = 1 },
    few     = { key = "VAULT_STOCK_FEW", en = "There are few items left to get on this chest",
                fore = { 255, 255, 255 }, back = { 44, 32, 24 }, shadow = true, k = 1 },
    none    = { key = "VAULT_STOCK_NONE", en = "There is nothing left to get on this chest",
                fore = { 124, 116, 108 }, k = 0.86 },
}

local CHEST_NAMES = {
    { "VAULT_CHEST_1", "Wooden Chest" },
    { "VAULT_CHEST_2", "Golden Chest" },
    { "VAULT_CHEST_3", "Platinum Chest" },
}

local INK = { 62, 38, 20 }                       -- brown ink on the parchment
local RED_INK = { 166, 40, 26 }
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
    local lines, k, pitch = Tex.fitBox(font, text, FACE_W, STOCK_H, st.k, 0.6)
    local y0 = -(#lines - 1) * pitch / 2
    local fore = Tex.col(st.fore[1], st.fore[2], st.fore[3])
    local back = st.back and Tex.col(st.back[1], st.back[2], st.back[3]) or Tex.clear()
    for j, l in ipairs(lines) do
        local lf = Tex.child(f, 0, y0 + (j - 1) * pitch)
        if st.shadow then
            Tex.text(font, l, Tex.child(lf, 1, 3), Tex.col(40, 26, 14), Tex.col(40, 26, 14), k, FACE_W, "center", 0.3 * op)
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
        Tex.sprite(icon, Tex.child(f, -w / 2 + PAD_L + ICON_PX / 2, 0), ICON_PX / (icon.Width - 12), nil, op, shade)
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
        local a = confirmT and (now - confirmT) or 1
        local pulse = 1 + 0.04 * math.sin(now * 2 * math.pi * 1.4) + 0.1 * (1 - Easing.outBack(a / 0.3, 2))
        line(ctx.fonts.title, Tex.tr("VAULT_CONFIRM", "Open this chest?"), Tex.child(f, 0, TITLE_Y),
            ink(RED_INK), Tex.clear(), FACE_W / 1.14, op, pulse)
    else
        local nm = CHEST_NAMES[c]
        line(ctx.fonts.title, Tex.tr(nm[1], nm[2]), Tex.child(f, 0, TITLE_Y), ink(INK), Tex.clear(), FACE_W, op)
    end
    Tex.sprite(ctx.tex["Chest/Chest" .. c], Tex.child(f, 0, CHEST_Y), CHEST_K, CHEST_K, op, shade)
    local info = d or {}
    drawStock(Tex.child(f, 0, STOCK_Y), info.left, info.total, op * (0.45 + 0.55 * shade))
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

    local enter = Easing.outBack(Tex.clamp01((now - popT) / 0.45), 1.6)
    for _, o in ipairs(order) do
        local a, d = o.a, o.d
        local side = d < 0 and -1 or 1
        local x = 960 + side * (a <= 1 and SPREAD1 * a or SPREAD1 + SPREAD2 * (a - 1))
        local k = (a <= 1) and (1 - 0.26 * a) or (0.74 - 0.2 * (a - 1))
        local shade = 1 - 0.45 * math.min(1, a)
        local op = alpha * ((a <= 1.5) and 1 or math.max(0, 1 - (a - 1.5) / 0.5))
        local y = CARD_Y + 22 * math.min(1, a) ^ 2 + (1 - alpha) * 50
        local rot = -4 * d
        local front = o.i == sel and a < 0.5
        if front then
            if shakeT ~= nil then
                local s = now - shakeT
                if s < 0.6 then x = x + 14 * math.exp(-8 * s) * math.sin(52 * s) end
            end
            if confirm then
                local c = confirmT and (now - confirmT) or 1
                local lift = Easing.outBack(Tex.clamp01(c / 0.3), 2)
                y, k = y - 16 * lift, k * (1 + 0.04 * lift)
            end
        end
        k = k * (0.9 + 0.1 * enter)
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
        local k = (focus == i) and (1.06 + 0.02 * math.sin(now * 2 * math.pi)) or 1
        chip(Tex.frame(xs[i].x, TOP_Y, 0, k), i, keys and keys[i] or 0, alpha, 1)
    end
end

return K
