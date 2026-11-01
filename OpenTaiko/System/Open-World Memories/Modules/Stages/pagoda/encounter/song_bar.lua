-- One encounter song bar, drawn through its transform b.xf (encounter/bar_text's xf).
-- A bar is an entry of encounter/song_reveal; prepare() adds its layouts; draw() reads b.xf, b.alpha,
-- b.crack (0..3), b.flames, b.revealed, b.pops, b.flavor, b.flavorA, b.flavorT (scroll time),
-- b.emblemPop and b.veil (0..1, the lifted darkness).

local I18N  = require("i18n")
local data  = require("encounter/songbars_data")
local text  = require("encounter/bar_text")
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local Look = Layout.encounter.song_bar

local M = {}

local NAME_SIZE, CHANCE_SIZE = Look.name_size, Look.chance_size
local CHANCE_JITTER = Look.chance_jitter
local FLAVOR_SIZE = Look.flavor_size
local FLAVOR_Y = Look.flavor_y                   -- each line's centre, local px from the bar's middle
local FLAVOR_X0, FLAVOR_X1 = Look.flavor_x0, Look.flavor_x1   -- the lines' run, from the box's left end to the emblem
local FLAVOR_FADE = Look.flavor_fade_px          -- fade length at each end
local FLAVOR_GAP = Look.flavor_gap_px            -- between two repeats of the line
local FLAVOR_SPEED = Animation.encounter.song_bar.flavor_speed_px_per_s
local FLAVOR_ALPHA = Look.flavor_opacity
local EXT_DX, EXT_DY = Look.ext_dx, Look.ext_dy  -- the extrusion's offset, local px
local CHECK_K = Look.check_k
local QUESTION_SIZE = Look.question_size
local PLUS_DX, PLUS_DY = Look.plus_dx, Look.plus_dy   -- the song select's "+" off the level's top right
local NUDGE = Look.text_nudge_px
local EMBLEM_GAP = Look.emblem_gap_px            -- local px between a line's end and the emblem
local OUTLINE = 1.3 * 4 / 30                     -- a glyph outline's reach past its ink, per unit of font size
local FIT_TRIES = 4

local HUES = Look.hues
local RIM = Look.rim_color
local DARK = Look.dark

local _font, _small = nil, nil
local _size = 0                                  -- lettering font size
local _styles, _darkStyles = nil, nil
local _clear, _white, _ink = nil, nil, nil

local function color(c, a)
    return COLOR:CreateColorFromRGBA(math.floor(c[1] * 255 + 0.5), math.floor(c[2] * 255 + 0.5),
        math.floor(c[3] * 255 + 0.5), a or 255)
end

local function sk(key, fallback)
    local s = THEME:GetSkinString(key)
    if s == nil or s == "" then return fallback end
    return s
end

-- font = the bars' glyph font (size: its font size), small = the flavour's
function M.setup(font, small, size)
    _font, _small, _size = font, small, size or 0
    if _styles ~= nil then return end
    local rim = color(RIM)
    _styles = {}
    for name, h in pairs(HUES) do
        if name:sub(1, 1) ~= "_" then
            local ext = h.ext
            local check = { ext[1] * CHECK_K, ext[2] * CHECK_K, ext[3] * CHECK_K }
            _styles[name] = {
                face = color(h.face), rim = rim, ext = color(ext), dx = EXT_DX, dy = EXT_DY,
                check = check, flavor = color(check),
            }
        end
    end
    _darkStyles = {}
    for name, d in pairs(DARK) do
        if name:sub(1, 1) ~= "_" then
            _darkStyles[name] = { face = color(d.face), rim = color(d.rim), ext = color(d.ext), dx = EXT_DX, dy = EXT_DY }
        end
    end
    _clear = COLOR:CreateColorFromRGBA(0, 0, 0, 0)
    _white = Config.color(Look.question_color)
    _ink = Config.color(Look.question_outline_color)
end

local function darkLook(b) return (b.color == "black") and DARK.black or DARK.purple end

-- the chance line of an unknown mystery song
local function chanceText(n)
    local fmt = sk("PAGODA_MYSTERY_CHANCE", "Probability of a mystery: %d%%")
    local ok, s = pcall(string.format, fmt, n)
    if ok then return s end
    return "Probability of a mystery: " .. n .. "%"
end

-- local x where the lettering must end: the widest emblem's left edge less the gap
local function nameEnd()
    local bar = data.bar
    local w = data.tex.sb_emblem_missing.w
    for d = 0, 4 do w = math.max(w, data.tex["sb_emblem" .. d].w) end
    return bar.emblemX - w * 0.5 * bar.emblemScale - EMBLEM_GAP
end

-- local x where a layout's letters end as drawn (ink, outline and turn per letter, plus the extrusion)
local function reach(lay)
    local right = nil
    local lh = (_font ~= nil and _font.LineHeight) or 0
    for _, c in ipairs(lay.chars) do
        if not c.blank then
            local edge = OUTLINE * _size * c.s
            local hw = (c.adv * 0.5 + edge) * c.sx
            local hh = lh * c.s * 0.5 + edge
            local a = math.rad(math.abs(c.rot))
            local r = c.x + hw * math.cos(a) + hh * math.sin(a)
            if right == nil or r > right then right = r end
        end
    end
    if right == nil then return nil end
    return right + EXT_DX
end

-- fits before the emblem: squished first, then smaller once the squish is at its floor
local function fit(str, size, jitter)
    local x0, limit = data.bar.textX0, nameEnd()
    local room = limit - x0
    local lay = text.layout(_font, str, x0, room, size, jitter)
    for _ = 1, FIT_TRIES do
        local r = reach(lay)
        if r == nil or r <= limit then break end
        room = lay.w - (r - limit)
        if room <= 0 then break end
        lay = text.layout(_font, str, x0, room, size, jitter)
        if lay.w > room then
            size = size * room / lay.w
            lay = text.layout(_font, str, x0, room, size, jitter)
        end
    end
    return lay
end

-- layouts of the shown name, the darkness's line and, for a darkness that will lift, the revealed name
function M.prepare(b)
    local unknown = I18N.tr("???")
    b.lay = fit(b.named and b.title or unknown, NAME_SIZE, 1)
    b.layName = fit(b.title or unknown, NAME_SIZE, 1)
    if b.chance ~= nil then
        b.layDark = fit(chanceText(b.chance), CHANCE_SIZE, CHANCE_JITTER)
    else
        b.layDark = fit(unknown, NAME_SIZE, 1)
    end
end

local function ready(tx) return tx ~= nil and tx.Ready end

local function reset(tx)
    tx:SetScale(1, 1) ; tx:SetRotation(0) ; tx:SetOpacity(1)
    tx:SetColor(1, 1, 1) ; tx:SetBlendMode("Normal")
end

-- a texture centred on local (lx, ly) of the bar, at the bar's turn and scale k
local function at(tx, xf, lx, ly, k, alpha, c, blend)
    if not ready(tx) or alpha <= 0 then return end
    local x, y = text.point(xf, lx, ly)
    tx:SetScale(k, k)
    tx:SetRotation(xf.rot)
    tx:SetOpacity(alpha)
    if c ~= nil then tx:SetColor(c[1], c[2], c[3]) end
    if blend ~= nil then tx:SetBlendMode(blend) end
    tx:DrawAtAnchor(x, y, "center")
    reset(tx)
end

-- raises what is darker than c to c (a Sub then an Add pass of one tile), so only the dark letter faces show it
local function checker(tx, xf, c, alpha)
    if not ready(tx) or alpha <= 0 then return end
    local bar = data.bar
    local x, y = text.point(xf, (bar.textX0 + bar.textX1) * 0.5, 0)
    tx:SetScale(xf.s, xf.s)
    tx:SetRotation(xf.rot)
    tx:SetOpacity(alpha)
    tx:SetColor(c[1], c[2], c[3])
    tx:SetBlendMode("Sub")
    tx:DrawAtAnchor(x, y, "center")
    tx:SetBlendMode("Add")
    tx:DrawAtAnchor(x, y, "center")
    reset(tx)
end

-- chart level centred on local (lx, 0), set like the song select's info; k = the emblem's scale
local function level(tex, xf, lx, k, b, alpha)
    if b.level == nil then return end
    local s = tostring(b.level)
    local total, prev, last = 0, 0, 0
    for i = 1, #s do
        local adv = data.digits[tonumber(s:sub(i, i))] or 0
        if i > 1 then total = total + prev * 0.5 end
        prev, last = adv, adv
    end
    total = total + last
    local cursor, step = -total * 0.5, 0
    for i = 1, #s do
        local d = tonumber(s:sub(i, i))
        local adv = data.digits[d] or 0
        if i > 1 then cursor = cursor + step * 0.5 end
        at(tex["sb_level" .. d], xf, lx + (cursor + adv * 0.5) * k, 0, xf.s * k, alpha)
        step = adv
    end
    if b.plus then
        at(tex.sb_level_plus, xf, lx + (total * 0.5 + PLUS_DX) * k, PLUS_DY * k, xf.s * k, alpha)
    end
end

local function emblem(tex, xf, b, alpha)
    local bar = data.bar
    local veil = b.veil or 0
    if not b.dark and veil > 0 then at(tex.sb_emblem_missing, xf, bar.emblemX, 0, xf.s * bar.emblemScale, alpha * veil) end
    local pop = b.emblemPop or 1
    if pop <= 0 then return end
    local k = bar.emblemScale * pop
    local lx = bar.emblemX
    if b.dark then
        at(tex.sb_emblem_missing, xf, lx, 0, xf.s * k, alpha)
        if _font ~= nil then
            local q = QUESTION_SIZE * pop
            local x, y = text.point(xf, lx, NUDGE * q)
            _font:Draw("?", x, y, _white, _ink, alpha, xf.s * q, 0, "center", xf.s * q, xf.rot)
        end
    else
        at(tex["sb_emblem" .. (b.diff or 0)], xf, lx, 0, xf.s * k, alpha)
        level(tex, xf, lx, k, b, alpha)
    end
end

-- per character: pen position, advance and bytes in the line and in the line reversed
local function flavorLayout(b)
    local lay = b.flavorLay
    if lay ~= nil and lay.text == b.flavor then return lay end
    local str = b.flavor
    local chars = {}
    local ok = pcall(function()
        for p, code in utf8.codes(str) do chars[#chars + 1] = { b0 = p, ch = utf8.char(code) } end
    end)
    if not ok then
        chars = {}
        for i = 1, #str do chars[i] = { b0 = i, ch = str:sub(i, i) } end
    end
    local pen, rev = 0, {}
    for i, c in ipairs(chars) do
        c.b1 = c.b0 + #c.ch - 1
        c.x0, c.adv = pen, _small:Measure(c.ch) * FLAVOR_SIZE
        c.blank = c.ch:match("^%s$") ~= nil
        pen = pen + c.adv
        rev[#chars - i + 1] = c.ch
    end
    local r = 1
    for i = #chars, 1, -1 do
        local c = chars[i]
        c.r0, c.r1 = r, r + #c.ch - 1
        r = c.r1 + 1
    end
    lay = { text = str, rev = table.concat(rev), chars = chars, w = pen }
    b.flavorLay = lay
    return lay
end

-- characters i..j of one repeat from local x0, in one draw; flip draws the line reversed and turned
-- half a turn, which puts each letter upside down in its own place
local function flavorRun(lay, xf, x0, i, j, ly, flip, fore, alpha)
    local c1, c2 = lay.chars[i], lay.chars[j]
    local s = xf.s * FLAVOR_SIZE
    local x, y = text.point(xf, x0 + (c1.x0 + c2.x0 + c2.adv) * 0.5, ly)
    local rot, str = xf.rot, nil
    if flip then
        rot, str = rot + 180, lay.rev:sub(c2.r0, c1.r1)
    else
        str = lay.text:sub(c1.b0, c2.b1)
    end
    local a = math.rad(rot)
    x, y = x + NUDGE * s * math.sin(a), y + NUDGE * s * math.cos(a)
    _small:Draw(str, x, y, fore, _clear, alpha, s, 0, "center", s, rot)
end

local function smooth(u)
    if u <= 0 then return 0 elseif u >= 1 then return 1 end
    return u * u * (3 - 2 * u)
end

-- t = scroll time (s); letters at the faded ends draw one by one, the rest in runs
local function flavorLine(lay, xf, t, ly, flip, fore, alpha)
    local period = lay.w + FLAVOR_GAP
    local n = #lay.chars
    local start = FLAVOR_X0 - (t * FLAVOR_SPEED) % period
    while start < FLAVOR_X1 do
        local run = nil
        for i = 1, n do
            local c = lay.chars[i]
            local mid = start + c.x0 + c.adv * 0.5
            local f = smooth(math.min(mid - FLAVOR_X0, FLAVOR_X1 - mid) / FLAVOR_FADE)
            if f >= 1 then
                run = run or i
            else
                if run ~= nil then flavorRun(lay, xf, start, run, i - 1, ly, flip, fore, alpha) ; run = nil end
                if f > 0 and not c.blank then flavorRun(lay, xf, start, i, i, ly, flip, fore, alpha * f) end
            end
        end
        if run ~= nil then flavorRun(lay, xf, start, run, n, ly, flip, fore, alpha) end
        start = start + period
    end
end

local function flavor(b, xf, alpha, style)
    if b.flavor == nil or _small == nil then return end
    local a = alpha * (b.flavorA or 1) * FLAVOR_ALPHA
    if a <= 0 then return end
    local lay = flavorLayout(b)
    if #lay.chars == 0 or lay.w <= 0 then return end
    local t = b.flavorT or 0
    flavorLine(lay, xf, t, -FLAVOR_Y, false, style.flavor, a)
    flavorLine(lay, xf, t, FLAVOR_Y, true, style.flavor, a)
end

local _flameTex, _flameMeta = {}, { data.tex.sb_flame1, data.tex.sb_flame2, data.tex.sb_flame3 }

local function flames(tex)
    _flameTex[1], _flameTex[2], _flameTex[3] = tex.sb_flame1, tex.sb_flame2, tex.sb_flame3
    return _flameTex, _flameMeta
end

local function darkness(tex, b, xf, alpha)
    local look = darkLook(b)
    local ft, fm = flames(tex)
    at(tex.sb_shell, xf, 0, 0, xf.s, alpha, look.shell)
    if (b.crack or 0) > 0 then
        at(tex["sb_crack" .. math.min(3, b.crack)], xf, 0, 0, xf.s, alpha, look.crack, "Add")
    end
    if b.flames ~= nil then b.flames:draw(ft, fm, xf, look.flame, false, alpha) end
    text.draw(_font, b.layDark, xf, _darkStyles[b.color == "black" and "black" or "purple"], alpha)
    if b.flames ~= nil then b.flames:draw(ft, fm, xf, look.glow, true, alpha) end
end

-- tex = key -> handle of the bars' textures
function M.draw(b, tex)
    local xf = b.xf
    local alpha = b.alpha or 1
    if xf == nil or alpha <= 0 or _styles == nil or _darkStyles == nil then return end
    at(tex["sb_bar_" .. b.color], xf, 0, 0, xf.s, alpha)
    if b.dark then
        darkness(tex, b, xf, alpha)
    else
        local style = _styles[b.color] or _styles.blue
        flavor(b, xf, alpha, style)
        local lay = b.revealed and b.layName or b.lay
        text.draw(_font, lay, xf, style, alpha, b.pops)
        checker(tex.sb_checker, xf, style.check, alpha)
        if (b.veil or 0) > 0 then at(tex.sb_shell, xf, 0, 0, xf.s, alpha * b.veil, darkLook(b).shell) end
        if b.flames ~= nil then
            local look = darkLook(b)
            local ft, fm = flames(tex)
            b.flames:draw(ft, fm, xf, look.flame, false, alpha)
            b.flames:draw(ft, fm, xf, look.glow, true, alpha)
        end
    end
    emblem(tex, xf, b, alpha)
end

-- the shards' tint for a bar's darkness
function M.shellTint(b) return darkLook(b).shell end

return M
