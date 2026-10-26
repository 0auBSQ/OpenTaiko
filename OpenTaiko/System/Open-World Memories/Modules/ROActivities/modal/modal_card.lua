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
-- between their two frames; nameplates go through the real plate renderer; a custom item is the
-- picture the caller shared, fitted to a square. The poor tier's card is an abandoned one: a sepia-tinted panel, cobwebs in
-- its corners, dust motes drifting, no wheel of light.

local Fx = require("modal_fx")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}
M.__index = M

local L = Layout.card
local T = Animation.card
local CASE = Layout.card.case
local CASE_T = Animation.card.case
local WEBS = Layout.card.webs
local TRICKLE = Animation.card.trickle
local MOTES = Layout.card.attic_motes
local MOTES_T = Animation.card.attic_motes
local GLOW = Layout.card.glow
local WHEEL = Layout.card.wheel
local HEAD = Layout.card.head
local BODY = Layout.card.body
-- the header strip and body panel inside the 1920x1080 rarity panels: HEAD, BODY
local RAINBOW_W, RAINBOW_SPEED = L.rainbow_w, T.rainbow_speed   -- one hue cycle of rainbow.png, and how fast it scrolls (px/s)
local HEADER_TEXT_Y = L.header_text_y
local ITEM_CX, ITEM_CY = L.item_cx, L.item_cy
local ITEM_BOX_W, ITEM_BOX_H = L.item_box_w, L.item_box_h   -- what a character preview is scaled to fit
local ITEM_BOTTOM = ITEM_CY + L.item_bottom_below           -- where a character's feet stand
local NAME_Y, PILL_Y, DETAIL_Y = L.name_y, L.pill_y, L.detail_y
local CASE_W, CASE_H, CASE_D = CASE.w, CASE.h, CASE.d       -- the jewel case (case_front.png / case_side.png / case_top.png)
local JACKET = CASE.jacket                     -- the front's window the jacket shows through
local EDGE_SLICES = CASE.edge_slices           -- how many strips each edge is drawn in to skew it
local ATTIC_TINT = L.attic_tint_color
local ATTIC_DUST = L.attic_dust_color
local PANEL_PASTEL = L.panel_pastel            -- how far the rarity colour is pulled toward white on the panel
local PUCHI_SCALE = L.puchichara_scale         -- a 256px frame drawn at 384
local PICTURE_BOX = L.picture_box              -- the square a custom item's picture is fitted to
local PILL_H, PILL_GAP = L.pill_h, L.pill_gap
local FADE_OUT = T.fade_out_s
local T_HEAD, T_BODY, T_ITEM, T_NAME, T_PILL, T_DETAIL = T.head_at_s, T.body_at_s, T.item_at_s, T.name_at_s, T.pill_at_s, T.detail_at_s
local T_READY = T.ready_at_s

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
    return title, color or L.genre_fallback_color
end

-- kind: 1 character (LuaCharacter), 2 puchichara, 3 nameplate, 4 song, 5 custom (info = its name,
-- picture = the shared texture's key, header = the card's title)
-- key (one of Fx.RARITIES) picks the colour, label and fanfare; modalInt is kept for callers
function M:start(kind, modalInt, key, info, picture, header)
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
    self.plateTex, self.preimage, self.picture = nil, nil, nil
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
        elseif kind == 5 then
            self.header = tostring(header or "")
            self.name = tostring(info or "")
            if picture ~= nil then self.picture = SHARED:GetSharedTexture(tostring(picture)) end
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
    self.wheelRot = (self.wheelRot + T.wheel_speed_deg_per_s * dt) % 360
    self.frame = self.frame + dt
    if self.t >= T_ITEM and not self.itemShown then
        self.itemShown = true
        self.A:play("reveal")
        self.A:playFanfare(self.key)
        local sp = T.sparks
        if self.attic then
            local at = T.attic_sparks
            self.sparks:burst(ITEM_CX, ITEM_CY, at.count, { speed = at.speed, size = at.size, size1 = at.size1, life = at.life_s, color = L.attic_sparks_color, gravity = at.gravity, fade = at.fade })
        elseif self.rainbow then
            for i = 0, sp.rainbow_hues - 1 do self.sparks:burst(ITEM_CX, ITEM_CY, sp.rainbow_count, { speed = sp.speed, size = sp.size, life = sp.life_s, color = Fx.rainbow(i / sp.rainbow_hues, L.sparks_rainbow_pastel), gravity = sp.gravity }) end
        else
            self.sparks:burst(ITEM_CX, ITEM_CY, sp.count, { speed = sp.speed, size = sp.size, life = sp.life_s, color = self:color(), gravity = sp.gravity })
        end
    end
    -- a trickle of sparkles drifting up around the item (dust motes across the whole attic)
    if self.itemShown then
        self.emitAcc = self.emitAcc + dt * (self.attic and T.attic_trickle_per_s or T.trickle_per_s)
        while self.emitAcc >= 1 do
            self.emitAcc = self.emitAcc - 1
            if self.attic then
                self.sparks:emit{ x = random() * MOTES.w, y = MOTES.y + random() * MOTES.h, vy = -(MOTES_T.vy_min + random() * MOTES_T.vy_range), vx = (random() - 0.5) * MOTES_T.vx_range,
                                  life = MOTES_T.life_min + random() * MOTES_T.life_range, s0 = MOTES_T.size_min + random() * MOTES_T.size_range, s1 = MOTES_T.size1, rot = random() * 360, spin = MOTES_T.spin,
                                  color = MOTES.color, fade = MOTES_T.fade, alpha = MOTES_T.alpha }
            else
                local col = self.rainbow and Fx.rainbow(random(), L.trickle_rainbow_pastel) or L.trickle_color
                self.sparks:emit{ x = ITEM_CX + (random() - 0.5) * L.trickle_spread_x, y = ITEM_CY + L.trickle_y + random() * L.trickle_h, vy = -(TRICKLE.vy_min + random() * TRICKLE.vy_range),
                                  vx = (random() - 0.5) * TRICKLE.vx_range, life = TRICKLE.life_min + random(), s0 = TRICKLE.size_min + random() * TRICKLE.size_range, s1 = TRICKLE.size1,
                                  rot = random() * 360, spin = TRICKLE.spin, color = col, fade = TRICKLE.fade, alpha = TRICKLE.alpha }
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

local function pop(t, t0) return Fx.outBack(Fx.span(t, t0, T.pop_s), T.pop_overshoot), Fx.span(t, t0, T.pop_fade_s) end

-- the panel pieces: one tinted draw; for mythical the untinted panel with a smooth scrolling
-- rainbow multiplied over it (one hue cycle of the strip, stretched to the panel)
function M:drawPanel(box, s, alpha)
    local cx, cy = box.x + box.w / 2, box.y + box.h / 2
    Fx.draw(self.panel, cx, cy, { s = s, opacity = alpha, rx = box.x, ry = box.y, rw = box.w, rh = box.h, color = self.panelTint })
    if self.rainbow then
        local A = self.A
        local rb = A.tex.rainbow
        local rh = rb.Height or L.rainbow_fallback_h
        local off = floor((self.t * RAINBOW_SPEED) % RAINBOW_W)
        Fx.draw(rb, cx, cy, { sx = box.w * s / RAINBOW_W, sy = box.h * s / rh, opacity = alpha, rx = off, ry = 0, rw = RAINBOW_W, rh = rh, blend = "multi" })
    end
end

-- CD jewel case in fake 3D
function M:drawCase(alpha, s)
    local A = self.A
    local pre = self.preimage or A.tex.preimage
    local settle = Fx.outCubic(Fx.span(self.t, T_ITEM, CASE_T.settle_s))
    local yaw = CASE_T.yaw_start * (1 - settle) + CASE_T.yaw_rest + CASE_T.yaw_sway * sin(self.t * CASE_T.yaw_sway_freq)          -- turned to the left
    local pitch = CASE_T.pitch_start * (1 - settle) + CASE_T.pitch_rest + CASE_T.pitch_sway * sin(self.t * CASE_T.pitch_sway_freq + CASE_T.pitch_sway_phase)   -- tilted down
    local cy, sy = cos(yaw), sin(yaw)
    local cp, sp = cos(pitch), sin(pitch)
    local fw, fh = CASE_W * cy * s, CASE_H * cp * s          -- the front, foreshortened
    local sideW = max(1, CASE_D * sy * s)                     -- the right edge's width on screen
    local topH = max(1, CASE_D * sp * s)                      -- the top edge's height on screen
    local left, top = ITEM_CX - (fw + sideW) / 2, ITEM_CY - (fh - topH) / 2
    local cxF, cyF = left + fw / 2, top + fh / 2
    Fx.draw(A.tex.glow, ITEM_CX, top + fh + CASE.shadow_y, { sx = CASE.shadow_sx * s, sy = CASE.shadow_sy * s, opacity = CASE.shadow_opacity * alpha, color = CASE.shadow_color })

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
                                          color = CASE.side_color })
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
        { tex = A.tex.web, x = HEAD.x + WEBS.inset, y = HEAD.y + WEBS.inset, anchor = "topleft", rot = 0, k = WEBS.top_left_scale },
        { tex = A.tex.web2, x = BODY.x + BODY.w - WEBS.inset, y = BODY.y + WEBS.inset, anchor = "topright", rot = 0, k = WEBS.top_right_scale },
        { tex = A.tex.web2, x = BODY.x + WEBS.inset, y = BODY.y + BODY.h - WEBS.inset, anchor = "bottomleft", rot = WEBS.bottom_left_rot_deg, k = WEBS.bottom_left_scale },
    }
    for _, w in ipairs(webs) do
        Fx.draw(w.tex, w.x, w.y, { s = w.k * s, opacity = WEBS.opacity * alpha, anchor = w.anchor, rot = w.rot, color = WEBS.color })
    end
end

function M:drawItem(alpha, s)
    local kind, info = self.kind, self.info
    if kind == 1 and info ~= nil then
        pcall(function()
            local sc = L.character_scale
            local sz = info:GetAnimationSize(CHARACTER.ANIM_MENU_NORMAL)
            if sz and sz.Y and sz.Y > 0 then sc = min(L.character_max_scale, ITEM_BOX_H / sz.Y, ITEM_BOX_W / max(1, sz.X)) end
            info:SetScale(sc * s, sc * s)
            info:SetOpacity(alpha)
            info:Update(CHARACTER.ANIM_MENU_NORMAL, true)
            info:DrawAtAnchor(ITEM_CX, ITEM_BOTTOM, CHARACTER.ANIM_MENU_NORMAL, "bottom")
        end)
    elseif kind == 2 and info ~= nil and info.tx ~= nil then
        -- the two-frame sheet bounces between its frames like it does on stage
        local tx = info.tx
        local fw, fh = floor(tx.Width / 2), tx.Height
        local frame = (floor(self.frame * T.puchichara_frames_per_s) % 2 == 0) and 0 or fw
        if fw > 0 then
            Fx.draw(tx, ITEM_CX, ITEM_CY + L.item_low_y, { s = PUCHI_SCALE * s, opacity = alpha, rx = frame, ry = 0, rw = fw, rh = fh })
        end
    elseif kind == 3 and info ~= nil and self.plateTex ~= nil then
        NAMEPLATE:DrawTitlePlate(ITEM_CX, ITEM_CY, floor(alpha * 255), info.Type, self.plateTex, self.langInt, info.Id)
    elseif kind == 4 then
        self:drawCase(alpha, s)
    elseif kind == 5 and self.picture ~= nil then
        local tx = self.picture
        local w, h = tx.Width or 0, tx.Height or 0
        if w > 0 and h > 0 then
            Fx.draw(tx, ITEM_CX, ITEM_CY + L.item_low_y, { s = min(PICTURE_BOX / w, PICTURE_BOX / h) * s, opacity = alpha })
        end
    end
end

-- the rarity pill, and the genre chip beside it for songs, centred as a row
function M:drawChips(s, alpha)
    local A = self.A
    local chips = { { text = self.rarityText, color = self:color() } }
    if self.genreTitle ~= nil then chips[#chips + 1] = { text = self.genreTitle, color = self.genreColor } end
    local total = 0
    for _, c in ipairs(chips) do
        c.w = floor(A.fontSmall:Measure(c.text) + L.pill_padding)
        total = total + c.w
    end
    total = total + PILL_GAP * (#chips - 1)
    local x = L.pill_x - total / 2
    for _, c in ipairs(chips) do
        local cx = x + c.w / 2
        Fx.drawCanvas(pillCanvas(self, c.w), cx, PILL_Y, { s = s, opacity = alpha, color = c.color })
        A.fontSmall:Draw(c.text, cx, PILL_Y + L.pill_text_y, A.colPillText, A.colPillOutline, alpha, s, 0, "center")
        x = x + c.w + PILL_GAP
    end
end

function M:draw()
    local A = self.A
    local t = self.t
    local fade = self.closing and (1 - Fx.span(self.closeT, 0, FADE_OUT)) or 1
    Fx.fill(A.fillCv, L.background_color, 1)
    local col = self:color()
    if self.attic then
        Fx.draw(A.tex.glow, GLOW.x, GLOW.y, { s = GLOW.scale, opacity = GLOW.attic_opacity * fade, color = col, blend = "add" })
    else
        Fx.draw(A.tex.glow, GLOW.x, GLOW.y, { s = GLOW.scale, opacity = GLOW.opacity * fade, color = col, blend = "add" })
        Fx.draw(A.tex.wheel, WHEEL.x, WHEEL.y, { s = WHEEL.scale, rot = self.wheelRot, opacity = WHEEL.opacity * fade, color = col, blend = "add" })
    end

    local hs, ha = pop(t, T_HEAD)
    if ha > 0 then
        self:drawPanel(HEAD, hs, ha * fade)
        A.fontHeader:Draw(self.header or "", L.header_text_x, HEADER_TEXT_Y, nil, nil, ha * fade, hs, 0, "center")
    end
    local bs, ba = pop(t, T_BODY)
    if ba > 0 then self:drawPanel(BODY, bs, ba * fade) end
    if self.attic and ba > 0 then self:drawWebs(bs, ba * fade) end
    if self.itemShown then
        local is, ia = pop(t, T_ITEM)
        local breathe = 1 + T.breathe * sin(t * T.breathe_freq)
        self:drawItem(ia * fade, is * breathe)
    end
    self.sparks:draw(fade)

    local ns, na = pop(t, T_NAME)
    if na > 0 then
        A.fontName:Draw(self.name, L.name_x, NAME_Y + T.name_rise_px * (1 - ns), A.colInk, A.colInkOutline, na * fade, 1, L.name_max_w, "center")
    end
    local ps, pa = pop(t, T_PILL)
    if pa > 0 and self.rarityText ~= "" then self:drawChips(ps, pa * fade) end
    local ds, da = pop(t, T_DETAIL)
    if da > 0 and self.detail ~= nil then
        A.fontSmall:Draw(self.detail, L.detail_x, DETAIL_Y + T.detail_rise_px * (1 - ds), A.colInk, A.colInkOutline, da * fade, 1, L.detail_max_w, "center")
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
    self.info, self.plateTex, self.picture = nil, nil, nil
    self.sparks:clear()
end

function M:dispose()
    for _, cv in pairs(self.pills) do pcall(function() cv:Dispose() end) end
    self.pills = {}

end

return M
