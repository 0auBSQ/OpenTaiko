---@diagnostic disable: undefined-global, undefined-field, need-check-nil
-- modal_card.lua — the "item got!" overlay after the reveal: out of the black, the header strip
-- and body panel (cut from the skin's one panel texture and tinted with the rarity's colour;
-- mythical runs a rainbow across it) pop in one after the other, then the item itself with a burst of sparkles and the
-- rarity's fanfare, then its name, a rarity pill (and the song's genre chip) and one line of
-- detail. A slow wheel of light in the rarity's colour keeps turning behind it and sparkles keep
-- drifting up around the item. Decide closes it (an early press finishes the pop-ins first).
--
-- Item visuals: characters draw their menu animation the way My Room's PC previews them; songs
-- are a CD jewel case in fake 3D, facing slightly down and to the left so its top edge and right
-- edge show as parallelograms, the jacket behind the case's front (case_*.png); puchicharas bounce
-- between their two frames; nameplates go through the real plate renderer. The poor tier's card is an abandoned one: a sepia-tinted panel, cobwebs in
-- its corners, dust motes drifting, no wheel of light.

local Fx = require("modal_fx")

local M = {}
M.__index = M

-- the header strip and body panel inside the 1920x1080 rarity panels
local HEAD = { x = 226, y = 72, w = 1470, h = 204 }
local BODY = { x = 305, y = 281, w = 1316, h = 728 }
local RAINBOW_W, RAINBOW_SPEED = 1024, 90      -- one hue cycle of rainbow.png, and how fast it scrolls (px/s)
local HEADER_TEXT_Y = 174
local ITEM_CX, ITEM_CY = 960, 540
local ITEM_BOX_W, ITEM_BOX_H = 700, 400        -- what a character preview is scaled to fit
local ITEM_BOTTOM = ITEM_CY + 200              -- where a character's feet stand
local NAME_Y, PILL_Y, DETAIL_Y = 790, 866, 936
local CASE_W, CASE_H, CASE_D = 380, 334, 46    -- the jewel case (case_front.png / case_side.png / case_top.png)
local JACKET = { x = 48, y = 8, w = 324, h = 318 }   -- the front's window the jacket shows through
local EDGE_SLICES = 14                         -- how many strips each edge is drawn in to skew it
local ATTIC_TINT = { 186, 170, 146 }
local ATTIC_DUST = { 168, 154, 132 }
local PANEL_PASTEL = 0.58                      -- how far the rarity colour is pulled toward white on the panel
local PUCHI_SCALE = 1.5                        -- a 256px frame drawn at 384
local PILL_H, PILL_GAP = 50, 16
local FADE_OUT = 0.3
local T_HEAD, T_BODY, T_ITEM, T_NAME, T_PILL, T_DETAIL = 0.05, 0.22, 0.5, 0.78, 0.92, 1.06
local T_READY = 1.2

local floor, min, max, sin, cos, random = math.floor, math.min, math.max, math.sin, math.cos, math.random

function M.new(A)
    local self = setmetatable({}, M)
    self.A = A
    self.sparks = Fx.pool(A.tex.spark, "add")
    self.pills = {}
    return self
end

-- white pills by width, tinted at draw time
local function pillCanvas(self, w)
    local cv = self.pills[w]
    if cv then return cv end
    cv = CANVAS:CreateCanvas(w, PILL_H)
    local rad = floor(PILL_H / 2)
    cv:FillRect(rad, 0, w - 2 * rad, PILL_H, 255, 255, 255, 255)
    cv:FillCircle(rad, rad, rad, 255, 255, 255, 255)
    cv:FillCircle(w - rad - 1, rad, rad, 255, 255, 255, 255)
    cv:Upload()
    self.pills[w] = cv
    return cv
end

-- the folder a song is found in: the top-most ancestor box sharing its genre, and its bar colour
local function genreOf(info)
    local title, color
    pcall(function()
        local genre = info.Genre
        local p, best = info.Parent, nil
        while p ~= nil and not p.IsRoot do
            if p.Genre == genre and p.Title ~= nil and p.Title ~= "" then best = p end
            p = p.Parent
        end
        title = best and best.Title or genre
        local bc = info.BoxColor
        if bc ~= nil then color = { bc.R, bc.G, bc.B } end
    end)
    if title == nil or title == "" then return nil end
    return title, color or { 120, 126, 150 }
end

-- kind: 1 character (LuaCharacter), 2 puchichara, 3 nameplate, 4 song
-- key (one of Fx.RARITIES) picks the colour, label and fanfare; modalInt is kept for callers
function M:start(kind, modalInt, key, info)
    self.kind, self.info, self.key = kind, info, key
    self.rarity = max(0, min(4, modalInt or 0))
    self.rainbow = key == "mythical"
    self.attic = key == "poor"
    self.moteAcc = 0
    -- the panel's tint: sepia for the attic, the rarity's pastel otherwise (common stays as drawn)
    if self.attic then self.panelTint = ATTIC_TINT
    elseif key == "common" or self.rainbow then self.panelTint = nil
    else self.panelTint = Fx.pastel(Fx.rarityColor(key), PANEL_PASTEL) end
    self.langInt = Fx.RARITY_LANG_INT[key] or 1
    self.t, self.closing, self.closeT = 0, false, 0
    self.wheelRot, self.emitAcc = 0, 0
    self.sparks:clear()
    self.itemShown = false
    self.panel = self.A.tex.panel
    self.name, self.detail = "", nil
    self.plateTex, self.preimage = nil, nil
    self.genreTitle, self.genreColor = nil, nil
    self.frame = 0

    local ok = pcall(function()
        if kind == 1 then
            self.header = LANG:GetString("MODAL_TITLE_CHARA")
            self.name = info.DisplayName or ""
            info:LoadAnimation(CHARACTER.ANIM_MENU_NORMAL)       -- a no-op when the reveal already warmed it
        elseif kind == 2 then
            self.header = LANG:GetString("MODAL_TITLE_PUCHI")
            self.name = info.Name or ""
            local author = info.Author
            if author ~= nil and author ~= "" then self.detail = LANG:GetString("HEYA_DESCRIPTION_AUTHOR", author) end
        elseif kind == 3 then
            self.header = LANG:GetString("MODAL_TITLE_NAMEPLATE")
            self.name = info.Title or ""
            self.plateTex = self.A.fontPlate:GetText(self.name, false, 99999, self.A.colPlateFg, self.A.colPlateBg)
        elseif kind == 4 then
            self.header = LANG:GetString("MODAL_TITLE_SONG")
            self.name = info.Title or "???"
            local sub = info.Subtitle
            if sub ~= nil and sub ~= "" then self.detail = sub end
            self.preimage = info:GetPreimage()
            self.genreTitle, self.genreColor = genreOf(info)
        end
    end)
    if not ok then self.header = self.header or "" end
    self.rarityText = LANG:GetString("HEYA_DESCRIPTION_RARITY", LANG:GetString("HEYA_DESCRIPTION_RARITY" .. self.langInt))
end

function M:ready() return self.t >= T_READY end

function M:color()
    if self.attic then return ATTIC_DUST end
    return Fx.rarityColor(self.key, self.t)
end

-- returns true once the closing fade is over
function M:update(dt, decide)
    if self.closing then
        self.closeT = self.closeT + dt
        return self.closeT >= FADE_OUT
    end
    self.t = self.t + dt
    self.wheelRot = (self.wheelRot + 9 * dt) % 360
    self.frame = self.frame + dt
    if self.t >= T_ITEM and not self.itemShown then
        self.itemShown = true
        self.A:play("reveal")
        self.A:playFanfare(self.key)
        if self.attic then
            self.sparks:burst(ITEM_CX, ITEM_CY, 16, { speed = 140, size = 0.14, size1 = 0.06, life = 2.0, color = { 230, 214, 180 }, gravity = 10, fade = "hump" })
        elseif self.rainbow then
            for i = 0, 5 do self.sparks:burst(ITEM_CX, ITEM_CY, 6, { speed = 560, size = 0.42, life = 0.9, color = Fx.rainbow(i / 6, 0.1), gravity = 260 }) end
        else
            self.sparks:burst(ITEM_CX, ITEM_CY, 36, { speed = 560, size = 0.42, life = 0.9, color = self:color(), gravity = 260 })
        end
    end
    -- a trickle of sparkles drifting up around the item (dust motes across the whole attic)
    if self.itemShown then
        self.emitAcc = self.emitAcc + dt * (self.attic and 12 or 9)
        while self.emitAcc >= 1 do
            self.emitAcc = self.emitAcc - 1
            if self.attic then
                self.sparks:emit{ x = random() * 1920, y = 150 + random() * 900, vy = -(8 + random() * 22), vx = (random() - 0.5) * 24,
                                  life = 3 + random() * 3, s0 = 0.05 + random() * 0.09, s1 = 0.04, rot = random() * 360, spin = 40,
                                  color = { 232, 218, 186 }, fade = "hump", alpha = 0.6 }
            else
                local col = self.rainbow and Fx.rainbow(random(), 0.25) or { 255, 255, 255 }
                self.sparks:emit{ x = ITEM_CX + (random() - 0.5) * 620, y = ITEM_CY + 120 + random() * 160, vy = -(40 + random() * 70),
                                  vx = (random() - 0.5) * 30, life = 1.6 + random(), s0 = 0.16 + random() * 0.26, s1 = 0,
                                  rot = random() * 360, spin = 90, color = col, fade = "hump", alpha = 0.9 }
            end
        end
    end
    self.sparks:update(dt)
    if decide then
        if self:ready() then
            self.closing = true
            self.A:play("close")
        else
            self.t = T_READY
            if not self.itemShown then self.itemShown = true ; self.A:playFanfare(self.key) end
        end
    end
    return false
end

local function pop(t, t0) return Fx.outBack(Fx.span(t, t0, 0.38), 1.6), Fx.span(t, t0, 0.2) end

-- the panel pieces: one tinted draw; for mythical the untinted panel with a smooth scrolling
-- rainbow multiplied over it (one hue cycle of the strip, stretched to the panel)
function M:drawPanel(box, s, alpha)
    local cx, cy = box.x + box.w / 2, box.y + box.h / 2
    Fx.draw(self.panel, cx, cy, { s = s, opacity = alpha, rx = box.x, ry = box.y, rw = box.w, rh = box.h, color = self.panelTint })
    if self.rainbow then
        local A = self.A
        local rb = A.tex.rainbow
        local rh = rb.Height or 8
        local off = floor((self.t * RAINBOW_SPEED) % RAINBOW_W)
        Fx.draw(rb, cx, cy, { sx = box.w * s / RAINBOW_W, sy = box.h * s / rh, opacity = alpha, rx = off, ry = 0, rw = RAINBOW_W, rh = rh, blend = "multi" })
    end
end

-- CD jewel case in fake 3D
function M:drawCase(alpha, s)
    local A = self.A
    local pre = self.preimage or A.tex.preimage
    local settle = Fx.outCubic(Fx.span(self.t, T_ITEM, 1.2))
    local yaw = 0.70 * (1 - settle) + 0.30 + 0.07 * sin(self.t * 1.3)          -- turned to the left
    local pitch = 0.50 * (1 - settle) + 0.26 + 0.05 * sin(self.t * 0.9 + 1.2)   -- tilted down
    local cy, sy = cos(yaw), sin(yaw)
    local cp, sp = cos(pitch), sin(pitch)
    local fw, fh = CASE_W * cy * s, CASE_H * cp * s          -- the front, foreshortened
    local sideW = max(1, CASE_D * sy * s)                     -- the right edge's width on screen
    local topH = max(1, CASE_D * sp * s)                      -- the top edge's height on screen
    local left, top = ITEM_CX - (fw + sideW) / 2, ITEM_CY - (fh - topH) / 2
    local cxF, cyF = left + fw / 2, top + fh / 2
    Fx.draw(A.tex.glow, ITEM_CX, top + fh + 26, { sx = 1.5 * s, sy = 0.28 * s, opacity = 0.35 * alpha, color = { 0, 0, 0 } })

    -- the top edge: strips from the near row (against the front) to the far row, each shifted
    -- right toward the far corner
    local n = EDGE_SLICES
    local tex = A.tex.case_top
    local rows = CASE_D / n
    for i = 0, n - 1 do
        local k = (i + 0.5) / n                               -- 0 at the far row, 1 at the front
        local y = top - topH * (1 - k)
        Fx.draw(tex, cxF + sideW * (1 - k), y, { sx = fw / CASE_W, sy = topH / CASE_D, opacity = alpha, rx = 0, ry = floor(i * rows), rw = CASE_W, rh = floor(rows) + 1 })
    end
    -- the right edge: strips from the near column (against the front) to the far column, each
    -- shifted up toward the far corner
    tex = A.tex.case_side
    local cols = CASE_D / n
    for i = 0, n - 1 do
        local k = (i + 0.5) / n                               -- 0 at the front, 1 at the far column
        local x = left + fw + sideW * k
        Fx.draw(tex, x, cyF - topH * k, { sx = sideW / CASE_D, sy = fh / CASE_H, opacity = alpha, rx = floor(i * cols), ry = 0, rw = floor(cols) + 1, rh = CASE_H,
                                          color = { 176, 178, 190 } })
    end
    -- the jacket in the window, then the front over it
    if pre ~= nil and pre.Width > 0 then
        local k = min(JACKET.w / pre.Width, JACKET.h / pre.Height)
        local jx = left + (JACKET.x + JACKET.w / 2) * cy * s
        local jy = top + (JACKET.y + JACKET.h / 2) * cp * s
        Fx.draw(pre, jx, jy, { sx = k * cy * s, sy = k * cp * s, opacity = alpha })
    end
    Fx.draw(A.tex.case_front, cxF, cyF, { sx = cy * s, sy = cp * s, opacity = alpha })
end

-- cobwebs in the abandoned panel's corners: the header's top-left, the body's top-right and
-- bottom-left (hung upside down)
function M:drawWebs(s, alpha)
    local A = self.A
    local webs = {
        { tex = A.tex.web, x = HEAD.x + 6, y = HEAD.y + 6, anchor = "topleft", rot = 0, k = 0.9 },
        { tex = A.tex.web2, x = BODY.x + BODY.w - 6, y = BODY.y + 6, anchor = "topright", rot = 0, k = 1.1 },
        { tex = A.tex.web2, x = BODY.x + 6, y = BODY.y + BODY.h - 6, anchor = "bottomleft", rot = 180, k = 0.75 },
    }
    for _, w in ipairs(webs) do
        Fx.draw(w.tex, w.x, w.y, { s = w.k * s, opacity = 0.7 * alpha, anchor = w.anchor, rot = w.rot, color = { 236, 230, 218 } })
    end
end

function M:drawItem(alpha, s)
    local kind, info = self.kind, self.info
    if kind == 1 and info ~= nil then
        pcall(function()
            local sc = 0.8
            local sz = info:GetAnimationSize(CHARACTER.ANIM_MENU_NORMAL)
            if sz and sz.Y and sz.Y > 0 then sc = min(1.15, ITEM_BOX_H / sz.Y, ITEM_BOX_W / max(1, sz.X)) end
            info:SetScale(sc * s, sc * s)
            info:SetOpacity(alpha)
            info:Update(CHARACTER.ANIM_MENU_NORMAL, true)
            info:DrawAtAnchor(ITEM_CX, ITEM_BOTTOM, CHARACTER.ANIM_MENU_NORMAL, "bottom")
        end)
    elseif kind == 2 and info ~= nil and info.tx ~= nil then
        -- the two-frame sheet bounces between its frames like it does on stage
        local tx = info.tx
        local fw, fh = floor(tx.Width / 2), tx.Height
        local frame = (floor(self.frame * 2.5) % 2 == 0) and 0 or fw
        if fw > 0 then
            Fx.draw(tx, ITEM_CX, ITEM_CY + 10, { s = PUCHI_SCALE * s, opacity = alpha, rx = frame, ry = 0, rw = fw, rh = fh })
        end
    elseif kind == 3 and info ~= nil and self.plateTex ~= nil then
        NAMEPLATE:DrawTitlePlate(ITEM_CX, ITEM_CY, floor(alpha * 255), info.Type, self.plateTex, self.langInt, info.Id)
    elseif kind == 4 then
        self:drawCase(alpha, s)
    end
end

-- the rarity pill, and the genre chip beside it for songs, centred as a row
function M:drawChips(s, alpha)
    local A = self.A
    local chips = { { text = self.rarityText, color = self:color() } }
    if self.genreTitle ~= nil then chips[#chips + 1] = { text = self.genreTitle, color = self.genreColor } end
    local total = 0
    for _, c in ipairs(chips) do
        c.w = floor(A.fontSmall:Measure(c.text) + 64)
        total = total + c.w
    end
    total = total + PILL_GAP * (#chips - 1)
    local x = 960 - total / 2
    for _, c in ipairs(chips) do
        local cx = x + c.w / 2
        Fx.drawCanvas(pillCanvas(self, c.w), cx, PILL_Y, { s = s, opacity = alpha, color = c.color })
        A.fontSmall:Draw(c.text, cx, PILL_Y + 10, A.colPillText, A.colPillOutline, alpha, s, 0, "center")
        x = x + c.w + PILL_GAP
    end
end

function M:draw()
    local A = self.A
    local t = self.t
    local fade = self.closing and (1 - Fx.span(self.closeT, 0, FADE_OUT)) or 1
    Fx.fill(A.fillCv, { 0, 0, 0 }, 1)
    local col = self:color()
    if self.attic then
        Fx.draw(A.tex.glow, 960, 560, { s = 4.2, opacity = 0.16 * fade, color = col, blend = "add" })
    else
        Fx.draw(A.tex.glow, 960, 560, { s = 4.2, opacity = 0.35 * fade, color = col, blend = "add" })
        Fx.draw(A.tex.wheel, 960, 540, { s = 1.9, rot = self.wheelRot, opacity = 0.12 * fade, color = col, blend = "add" })
    end

    local hs, ha = pop(t, T_HEAD)
    if ha > 0 then
        self:drawPanel(HEAD, hs, ha * fade)
        A.fontHeader:Draw(self.header or "", 960, HEADER_TEXT_Y, nil, nil, ha * fade, hs, 0, "center")
    end
    local bs, ba = pop(t, T_BODY)
    if ba > 0 then self:drawPanel(BODY, bs, ba * fade) end
    if self.attic and ba > 0 then self:drawWebs(bs, ba * fade) end
    if self.itemShown then
        local is, ia = pop(t, T_ITEM)
        local breathe = 1 + 0.015 * sin(t * 2.2)
        self:drawItem(ia * fade, is * breathe)
    end
    self.sparks:draw(fade)

    local ns, na = pop(t, T_NAME)
    if na > 0 then
        A.fontName:Draw(self.name, 960, NAME_Y + 24 * (1 - ns), A.colInk, A.colInkOutline, na * fade, 1, 1200, "center")
    end
    local ps, pa = pop(t, T_PILL)
    if pa > 0 and self.rarityText ~= "" then self:drawChips(ps, pa * fade) end
    local ds, da = pop(t, T_DETAIL)
    if da > 0 and self.detail ~= nil then
        A.fontSmall:Draw(self.detail, 960, DETAIL_Y + 16 * (1 - ds), A.colInk, A.colInkOutline, da * fade, 1, 1200, "center")
    end
end

-- releases what the card owns: the character's animation and the character itself, the jacket
function M:stop()
    local info = self.info
    if self.kind == 1 and info ~= nil then
        pcall(function() info:DisposeAnimation(CHARACTER.ANIM_MENU_NORMAL) end)
        pcall(function() info:Dispose() end)
    end
    if self.preimage ~= nil then pcall(function() self.preimage:Dispose() end) ; self.preimage = nil end
    self.info, self.plateTex = nil, nil
    self.sparks:clear()
end

function M:dispose()
    for _, cv in pairs(self.pills) do pcall(function() cv:Dispose() end) end
    self.pills = {}

end

return M
