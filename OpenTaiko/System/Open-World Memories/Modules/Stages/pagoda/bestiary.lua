-- Title-screen bestiary: setup(tex, fonts) once; open() / close(); update(dt, nav) -> "back" on leaving; draw().

local I18N     = require("i18n")
local assets   = require("encounter/assets")
local sprite   = require("encounter/sprite")
local body     = require("encounter/monster_body")
local document = require("encounter/document")
local monsters = require("encounter/monsters")
local floors   = require("floors")
local save     = require("save")
local Config   = require("Core.Config")
local Layout    = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")

local M = {}

local Look = Layout.stage.bestiary
local Anim = Animation.stage.bestiary
local Grid, Card, Panel, Art, Text = Look.grid, Look.card, Look.panel, Look.art, Look.text
local Desc = Look.description

local CARD_TEX = "Textures/encounter/ui/nametag.png"
local PANEL_TEX = "Textures/encounter/ui/panel.png"
local GLOW_TEX = "Textures/encounter/fx/glow_soft.png"
local QUESTION_TEX = "Textures/encounter/ui/question.png"
local WHITE_TEX = "Textures/encounter/ui/white1.png"
local ROOF_FROM = 21

-- names of monsters that have none in their floor's dialogs; the doors' come from floors.lua
local NAMES = {
    norm1 = "Bouncy Star", norm2 = "Karateka Dandelion", norm3 = "Pinecone Snake", norm4b = "Forest Spirit",
    critter = "Red Forest Spirit", norm5 = "Okami", pagodaguard = "Guardian Snake", scare1 = "Green Demon",
    scare2 = "Haunted Tree", scare3 = "Mimic", document = "Window Master",
}
local DESCRIPTIONS = {
    scare3 = "A flame-like spirit that houses itself in grown crops and carves wooden masks to imitate forest creatures, in an attempt to blend in with nature. Despite their potentially dangerous appearance, Mimics are very child-like, and love nothing more than to simply scare unsuspecting travelers, even when the effectiveness of their disguises and spookiness are very poor.",
}
local DESCRIPTION = "A description of this monster will come soon."   -- for a monster without one
local UNMET_DESCRIPTION = "You have not met this monster yet."

local _tex, _fonts = nil, nil
local _keys, _opened = {}, {}          -- keys drawn; keys opened here
local _sel, _t, _since = 1, 0, 0       -- selected slot (#ORDER + 1 = Back), clock, seconds since the selection changed
local _body, _bodyKey = nil, nil       -- the panel's monster body
local _state = {}                      -- per key: "known", "seen" (silhouette) or "hidden" (question mark)
local _reading = false                 -- Up/Down scroll the description instead of moving through the grid
local _scroll, _scrollTo = 0, 0        -- description scroll and its target, px
local _measured = {}                   -- text -> wrapped height
local _white, _dim, _gold, _edge = nil, nil, nil, nil
local _fit = {}

local function ready(tx) return tx ~= nil and tx.Ready end

local function stateOf(key)
    if save.monster_beaten(key) + save.monster_wins(key) > 0 then return "known" end
    local list, roof = monsters.floors(key)
    local reached = save.highest_reached()
    if (list[1] ~= nil and list[1] <= reached) or (roof and reached >= ROOF_FROM) then return "seen" end
    return "hidden"
end

local function nameOf(key)
    if NAMES[key] ~= nil then return NAMES[key] end
    local list = monsters.floors(key)
    return (list[1] ~= nil and floors.monsterName(list[1])) or key
end

-- "1-4, 6" from a sorted list of levels, "21+" for the roofs
local function levelsText(key)
    local list, roof = monsters.floors(key)
    local parts, i = {}, 1
    while i <= #list do
        local j = i
        while j < #list and list[j + 1] == list[j] + 1 do j = j + 1 end
        parts[#parts + 1] = (j > i) and (list[i] .. "-" .. list[j]) or tostring(list[i])
        i = j + 1
    end
    if roof then parts[#parts + 1] = "21+" end
    return table.concat(parts, ", ")
end

-- nine-slice draw to (x, y, w, h)
local function nine(tx, x, y, w, h, c, r, g, b, a)
    if not ready(tx) then return end
    local tw, th = tx.Width, tx.Height
    local mw, mh = tw - 2 * c, th - 2 * c
    local iw, ih = math.max(0, w - 2 * c), math.max(0, h - 2 * c)
    tx:SetColor(r or 1, g or 1, b or 1)
    tx:SetOpacity(a or 1)
    local cols = { { 0, c, x, c }, { c, mw, x + c, iw }, { tw - c, c, x + c + iw, c } }
    local rows = { { 0, c, y, c }, { c, mh, y + c, ih }, { th - c, c, y + c + ih, c } }
    for _, col in ipairs(cols) do
        for _, row in ipairs(rows) do
            if col[4] > 0 and row[4] > 0 then
                tx:SetScale(col[4] / col[2], row[4] / row[2])
                tx:DrawRect(col[3], row[3], col[1], row[1], col[2], row[2])
            end
        end
    end
    tx:SetScale(1, 1) ; tx:SetColor(1, 1, 1) ; tx:SetOpacity(1)
end

-- returns left, top, right, bottom in the art's own px, parts included
local function extent(meta)
    local l, t, r, b = 0, 0, meta.w, meta.h
    for _, part in pairs(meta.parts or {}) do
        local pm = assets.meta(part.key)
        if pm ~= nil then
            l, t = math.min(l, part.x - pm.w / 2), math.min(t, part.y - pm.h / 2)
            r, b = math.max(r, part.x + pm.w / 2), math.max(b, part.y + pm.h / 2)
        end
    end
    return l, t, r, b
end

-- returns the scale and foot point fitting the art in (bx, by, bw, bh), centred, scale <= 1
local function fit(meta, bx, by, bw, bh)
    local l, t, r, b = extent(meta)
    local s = math.min(1, bw / (r - l), bh / (b - t))
    local fx, fy = meta.fx or meta.w / 2, meta.fy or meta.h
    local x = bx + (bw - (r - l) * s) / 2 + (fx - l) * s
    local y = by + (bh - (b - t) * s) / 2 + (fy - t) * s
    return s, x, y
end

function M.setup(tex, fonts)
    _fonts = fonts
    if _tex ~= nil then return end
    _tex = {
        card = TEXTURE:CreateTexture(CARD_TEX), panel = TEXTURE:CreateTexture(PANEL_TEX),
        glow = TEXTURE:CreateTexture(GLOW_TEX), question = TEXTURE:CreateTexture(QUESTION_TEX),
        white = TEXTURE:CreateTexture(WHITE_TEX), black = tex.black1,
    }
    _white = Config.color(Text.color)
    _dim = Config.color(Text.dim_color)
    _gold = Config.color(Text.title_color)
    _edge = Config.color(Text.edge_color)end

function M.dispose()
    if _tex == nil then return end
    _tex.card:Dispose() ; _tex.panel:Dispose() ; _tex.glow:Dispose() ; _tex.question:Dispose() ; _tex.white:Dispose()
    _tex = nil
end

-- true once a monster has been beaten or has won
function M.available()
    return save.any_monster_fought(monsters.ORDER)
end

local function setBody()
    local key = monsters.ORDER[_sel]
    if key == _bodyKey then return end
    if _body ~= nil then _body:close() end
    _bodyKey, _body = key, nil
    _since = 0
    _reading, _scroll, _scrollTo = false, 0, 0
    if key == nil or _state[key] == "hidden" then return end
    local m = monsters.member(key)
    local meta = assets.meta("m_" .. key)
    if m == nil or meta == nil then return end
    local s, x, y = fit(meta, Art.x, Art.y, Art.w, Art.h)
    _body = body.new(m, x, y, _sel * 7, s)
end

function M.open()
    _sel, _t, _since = 1, 0, 0
    _keys, _opened, _state, _measured = {}, {}, {}, {}
    for _, key in ipairs(monsters.ORDER) do _state[key] = stateOf(key) end
    local seen = {}
    local function add(k)
        if k ~= nil and not seen[k] and assets.meta(k) ~= nil then seen[k] = true ; _keys[#_keys + 1] = k end
    end
    for _, key in ipairs(monsters.ORDER) do
        local m = monsters.member(key)
        if m ~= nil then for _, k in ipairs(body.keys(m)) do add(k) end end
    end
    add("fx_glint")
    -- only keys opened here are released: another screen's stay
    for _, k in ipairs(_keys) do if assets.get(k) == nil then _opened[#_opened + 1] = k end end
    assets.open(_keys)
    _bodyKey, _body = nil, nil
    setBody()
end

function M.close()
    if _body ~= nil then _body:close() end
    _body, _bodyKey = nil, nil
    assets.drop(_opened)
    _opened = {}
end

-- slot after a step: columns wrap, Back follows the last card
local function step(dx, dy)
    local n = #monsters.ORDER + 1
    local cols = Grid.columns
    local s = _sel
    if dx ~= 0 then
        s = ((s - 1 + dx) % n) + 1
    elseif dy ~= 0 then
        if s == n then
            s = (dy < 0) and (#monsters.ORDER) or s
        else
            s = s + dy * cols
            if s < 1 then s = _sel elseif s > #monsters.ORDER then s = n end
        end
    end
    return s
end

-- returns the description text and the panel's text width
local function descText(key)
    if _state[key] ~= "known" then return I18N.tr(UNMET_DESCRIPTION) end
    return I18N.tr(DESCRIPTIONS[key] or DESCRIPTION)
end

local function descW() return Panel.w - 2 * Text.left_pad end

-- max scroll, px (0 when it fits)
local function maxScroll(text)
    if _fonts == nil or _fonts.gfontSmall == nil then return 0 end
    local h = _measured[text]
    if h == nil then
        h = _fonts.gfontSmall:MeasureWrapped(text, descW(), 1, 1)
        _measured[text] = h
    end
    return math.max(0, h - Desc.h)
end

local function updateReading(nav, limit)
    if nav.cancel() or nav.decide() then
        _reading = false
        SHARED:GetSharedSound("Cancel"):Play()
        return
    end
    local step = _fonts.gfontSmall.LineHeight * Desc.lines_per_step
    local to = _scrollTo
    if nav.up() then to = math.max(0, _scrollTo - step)
    elseif nav.down() then to = math.min(limit, _scrollTo + step) end
    if to ~= _scrollTo then
        _scrollTo = to
        SHARED:GetSharedSound("Move"):Play()
    end
end

function M.update(dt, nav)
    _t, _since = _t + dt, _since + dt
    if _body ~= nil then _body:update(dt) end
    local key = monsters.ORDER[_sel]
    local limit = (key ~= nil) and maxScroll(descText(key)) or 0
    -- read every frame so wheel turns made elsewhere don't pile up
    local _, wheel = INPUT:GetScrollDelta()
    if wheel ~= nil and wheel ~= 0 and limit > 0 and INPUT:IsMouseInside() then
        local mx, my = INPUT:GetMouseXY()
        if mx >= Panel.x and mx <= Panel.x + Panel.w and my >= Panel.y and my <= Panel.y + Panel.h then
            local step = _fonts.gfontSmall.LineHeight * Desc.wheel_lines
            _scrollTo = math.max(0, math.min(limit, _scrollTo - wheel * step))
        end
    end
    _scroll = _scroll + (_scrollTo - _scroll) * (1 - math.exp(-Anim.scroll_rate * dt))
    if nav == nil then return nil end
    if _reading then
        updateReading(nav, limit)
        return nil
    end
    if nav.decide() and key ~= nil and limit > 0 then
        _reading = true
        SHARED:GetSharedSound("Decide"):Play()
        return nil
    end
    local moved = nil
    if nav.left() then moved = step(-1, 0)
    elseif nav.right() then moved = step(1, 0)
    elseif nav.up() then moved = step(0, -1)
    elseif nav.down() then moved = step(0, 1) end
    if moved ~= nil and moved ~= _sel then
        _sel = moved
        SHARED:GetSharedSound("Move"):Play()
        setBody()
    end
    if nav.cancel() or (nav.decide() and _sel == #monsters.ORDER + 1) then
        SHARED:GetSharedSound("Cancel"):Play()
        return "back"
    end
    return nil
end

-- extra scale of the selected card or button after a selection change
local function pop()
    local k = math.min(1, _since / Anim.select_pop_s)
    return 1 + Anim.select_pop_scale * (1 - k) * (1 - k)
end

local function cardRect(i)
    local col, row = (i - 1) % Grid.columns, math.floor((i - 1) / Grid.columns)
    return Grid.x + col * (Grid.card_w + Grid.gap), Grid.y + row * (Grid.card_h + Grid.gap), Grid.card_w, Grid.card_h
end

-- centred on (cx, cy), h px tall
local function drawQuestion(cx, cy, h, a)
    local q = _tex.question
    if not ready(q) then return end
    local s = h / q.Height
    q:SetScale(s, s) ; q:SetOpacity(a)
    q:DrawAtAnchor(cx, cy, "center")
    q:SetScale(1, 1) ; q:SetOpacity(1)
end

local function drawStill(key, met, bx, by, bw, bh, s0)
    local tx, meta = assets.get("m_" .. key)
    if meta == nil or not ready(tx) then return end
    local s, x, y = fit(meta, bx, by, bw, bh)
    s = s * s0
    _fit.sx, _fit.sy, _fit.rot, _fit.pivot = s, s, 0, "foot"
    local r, a = met and 1 or 0, met and 1 or Card.unmet_opacity
    _fit.alpha, _fit.r, _fit.g, _fit.b, _fit.blend = a, r, r, r, "Normal"
    sprite.draw(tx, meta, x, y, _fit)
    if meta.parts ~= nil then document.drawStill(meta, x, y, _fit, a, r, r, r) end
end

local function drawCards()
    local pad = Grid.thumb_pad
    for i, key in ipairs(monsters.ORDER) do
        local x, y, w, h = cardRect(i)
        local selected = (i == _sel)
        local s = selected and pop() or 1
        local cx, cy = x + w / 2, y + h / 2
        local dw, dh = w * s, h * s
        local c = selected and Card.selected_color or Card.idle_color
        if selected and ready(_tex.glow) then
            local gs = Card.glow_scale * dw / _tex.glow.Width
            _tex.glow:SetBlendMode("Add") ; _tex.glow:SetScale(gs, gs) ; _tex.glow:SetOpacity(Card.glow_opacity)
            _tex.glow:DrawAtAnchor(cx, cy, "center")
            _tex.glow:SetBlendMode("Normal") ; _tex.glow:SetScale(1, 1) ; _tex.glow:SetOpacity(1)
        end
        nine(_tex.card, cx - dw / 2, cy - dh / 2, dw, dh, Card.corner_px, c[1], c[2], c[3], 1)
        if _state[key] == "hidden" then
            drawQuestion(cx, cy, Card.question_h * s, Card.unmet_opacity)
        else
            drawStill(key, _state[key] == "known", cx - dw / 2 + pad * s, cy - dh / 2 + pad * s,
                dw - 2 * pad * s, dh - 2 * pad * s, 1)
        end
    end
    -- Back (for drum input) follows the last card
    local selected = (_sel == #monsters.ORDER + 1)
    local s = selected and pop() or 1
    local b = Look.back
    local c = selected and Card.selected_color or Card.idle_color
    nine(_tex.card, b.x - b.w * s / 2, b.y - b.h * s / 2, b.w * s, b.h * s, Card.corner_px, c[1], c[2], c[3], 1)
    -- the glyph box has a bottom pad: its centre is text_nudge_px above the letters'
    local ts = s * Text.button_scale
    _fonts.gfontName:Draw(I18N.tr("Back"), b.x, b.y + b.text_nudge_px * ts, selected and _gold or _white, _edge, 1, ts, 0,
        "center")
end

local function fill(x, y, w, h, c, a)
    local tx = _tex.white
    if not ready(tx) or w <= 0 or h <= 0 then return end
    tx:SetScale(w, h) ; tx:SetColor(c[1] / 255, c[2] / 255, c[3] / 255) ; tx:SetOpacity(a)
    tx:Draw(x, y)
    tx:SetScale(1, 1) ; tx:SetColor(1, 1, 1) ; tx:SetOpacity(1)
end

-- at (x, description_y), scrolled and clipped to its box
local function drawDescription(key, x, a)
    local text = descText(key)
    local y, w, h = Text.description_y, descW(), Desc.h
    local limit = maxScroll(text)
    if _reading then
        local c, m = Desc.frame_color, Desc.frame_pad
        nine(_tex.card, x - m, y - m, w + 2 * m, h + 2 * m, Card.corner_px, c[1], c[2], c[3], Desc.frame_opacity * a)
    end
    local cm = Desc.clip_margin_px
    GRAPHICS:SetClip(x - cm, y, w + 2 * cm, h)
    _fonts.gfontSmall:DrawWrapped(text, x, y - math.min(_scroll, limit), w, _dim, _edge, a)
    GRAPHICS:ClearClip()
    if limit <= 0 then return end
    local bx = x + w + Desc.bar_dx
    fill(bx, y, Desc.bar_w, h, { 0, 0, 0 }, Desc.track_opacity * a)
    local th = h * h / (h + limit)
    local ty = y + (h - th) * math.min(1, _scroll / limit)
    fill(bx, ty, Desc.bar_w, th, _reading and Desc.thumb_reading_color or Desc.thumb_color, a)
end

local function drawPanel()
    local p = Panel
    nine(_tex.panel, p.x, p.y, p.w, p.h, p.corner_px, 1, 1, 1, 1)
    local key = monsters.ORDER[_sel]
    if key == nil then return end
    local state = _state[key]
    local met = (state == "known")
    local a = math.min(1, _since / Anim.panel_fade_s)
    if state == "hidden" then
        drawQuestion(Art.x + Art.w / 2, Art.y + Art.h / 2, Art.question_h, a * Card.unmet_opacity)
    elseif _body ~= nil then
        local k = met and 1 or 0
        _body:drawShadow(a * (met and 1 or 0), 0, 0)
        _body:draw(a * (met and 1 or Card.unmet_opacity), k, k, k, met and 1 or 0, 0, 0)
    end
    local cx, x = p.x + p.w / 2, p.x + Text.left_pad
    local f = _fonts
    f.gfontName:Draw(met and I18N.tr(nameOf(key)) or "???", cx, Text.name_y, _gold, _edge, a, Text.name_scale,
        p.w - 2 * Text.left_pad, "center")
    local lv = (state == "hidden") and "???" or levelsText(key)
    f.gfont:Draw(I18N.trf("Levels: %s", lv), x, Text.levels_y, _white, _edge, a, 1, p.w - 2 * Text.left_pad)
    if met then
        f.gfont:Draw(I18N.trf("Beaten: %d", save.monster_beaten(key)), x, Text.counts_y, _white, _edge, a)
        f.gfont:Draw(I18N.trf("Defeated you: %d", save.monster_wins(key)), x + Text.counts_gap, Text.counts_y,
            _white, _edge, a)
    end
    drawDescription(key, x, a)
end

function M.draw()
    if _tex == nil or _fonts == nil then return end
    local res = THEME:GetResolution()
    if ready(_tex.black) then
        _tex.black:SetScale(res.X / _tex.black.Width, res.Y / _tex.black.Height)
        _tex.black:SetOpacity(Look.dim_opacity)
        _tex.black:Draw(0, 0)
        _tex.black:SetScale(1, 1) ; _tex.black:SetOpacity(1)
    end
    _fonts.gfontName:Draw(I18N.tr("Bestiary"), res.X / 2, Look.title.y, _gold, _edge, 1, Text.title_scale, 0, "center")
    drawCards()
    drawPanel()
end

-- every text the screen shows, for the translation files
function M.texts()
    local out = { "Bestiary", "Back", "Levels: %s", "Beaten: %d", "Defeated you: %d", DESCRIPTION, UNMET_DESCRIPTION }
    for _, key in ipairs(monsters.ORDER) do
        if NAMES[key] ~= nil and NAMES[key] ~= "???" then out[#out + 1] = NAMES[key] end
        if DESCRIPTIONS[key] ~= nil then out[#out + 1] = DESCRIPTIONS[key] end
    end
    return out
end

return M
