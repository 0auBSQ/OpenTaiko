local NavInput = require("NavInput")
local TowerArt = require("TowerArt")
local EM = require("EventMode")
-- positions, sizes and colours: Config/layout.json; timings: Config/animation.json
local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local Animation = Config.load("Config/animation.json")
local TOWERS, INFO, CONF = Layout.towers, Layout.info, Layout.confirm
local SIGNS = Animation.signs

local songlist = nil

local art = nil   -- the towers, composed from the pieces of each chart's tower look (Lib/TowerArt.lua)
local tex_pedestal = nil      -- back, random and folder entries: a badge floating over a pedestal
local tex_icons = {}          -- the badges' rim and glyph
local tex_disc = nil          -- the face under a badge, tinted: cream for back, the folder's colour for folders
local tex_rainbow = nil       -- the face under the random badge, turning
local tex_glow = nil          -- the halo under the random badge, breathing
local tex_sparkle = nil       -- the glitter twinkling over it
local tex_bg = TEXTURE:CreateTexture()
local tex_info = TEXTURE:CreateTexture()
local tex_info_spicy = TEXTURE:CreateTexture()
local tex_info_sweet = TEXTURE:CreateTexture()
local tex_notice = TEXTURE:CreateTexture()
local tex_number = TEXTURE:CreateTexture()
local font = nil
local font_small = nil
local font_path = nil         -- the folder path under a back or random entry's name

local move_counter = COUNTER:EmptyCounter()
local hold_counter = COUNTER:EmptyCounter()
local right = true

local loop_start = -TOWERS.visible_each_side
local loop_end = TOWERS.visible_each_side

local tower_x = {}
local tower_y = {}
local tower_fade = {}
local title_y = {}
local tower_scale = {}

local tower_titles = {}
local tower_paths = {}   -- per slot: the folder path texture of a back or random entry, nil otherwise
local tower_kinds = {}   -- per slot: kind "song" (with the tower look and the floors drawn), "back", "random", "folder" or "none"
local tower_info = nil
local clock = 0

local ICON_LIFT = Layout.badge.lift              -- the badge floats this far over the pedestal
local ICON_BOB = Animation.badge.bob_px           -- and bobs by this much
local RAINBOW_TURN = Animation.rainbow_deg_per_s
local GLOW_PULSE = Animation.glow.pulse_rad_per_s
local SPARKLES = Animation.sparkles.count         -- twinkling over the random badge at once
local SPARKLE_RATE = Animation.sparkles.rate_per_s -- twinkles per second for each
local PATH_GREY = Config.color(TOWERS.path_color)
local DISC_CREAM = { R = Layout.badge.disc_color[1], G = Layout.badge.disc_color[2], B = Layout.badge.disc_color[3] }

-- the confirm menu: wooden signs dropped on ropes over the chosen tower, the question on the top one and
-- an option on each of the signs hanging under it
local confirm = nil       -- nil while closed
local act = {}            -- the customize and song options dialogs the confirm opens
local ctex = {}
local snd_planks = {}     -- one landing sound per sign so they overlap
local font_title = nil
local COL_SIGN = Config.color(CONF.sign_color)
local COL_SIGN_OUTLINE = Config.color(CONF.sign_outline_color)
local SIGN_TOP_Y = CONF.title_y       -- the title sign's centre once landed
local SIGN_DROP = SIGNS.drop_px       -- the title sign starts this far above it
local SIGN_ROPE = CONF.rope_length    -- rope length between the title sign and an option
local SIGN_FALL = SIGNS.fall_s        -- seconds a sign takes to fall
local SIGN_STAGGER = SIGNS.stagger_s  -- seconds between two options starting to fall
local SIGN_SETTLE = SIGNS.settle_s    -- seconds for the sway to reach its full swing after a landing
local SIGN_CLOSE = SIGNS.close_s      -- seconds the signs take to leave
local SIGN_GAP = CONF.option_gap      -- between two options
local HOLE_X, HOLE_Y = CONF.hole_x, CONF.hole_y
local BUTTON_PAD_X, BUTTON_PAD_TOP, BUTTON_PAD_BOTTOM = CONF.button_pad_x, CONF.button_pad_top, CONF.button_pad_bottom   -- wood kept clear around an option's button
local OPTION_SLOTS = CONF.option_slots   -- the title plank has a pair of holes over each of these slots
local OPTION_SLOT = CONF.option_slot

local tex_number_interval = INFO.digit_spacing

local TOWER_SCALE = TOWERS.art_scale   -- of the TowerArt pieces
local TOWER_FLOORS_MAX = TOWERS.floors_max

local function skinString(key, fallback)
    local ok, s = pcall(function() return THEME:GetSkinString(key) end)
    if ok and type(s) == "string" and s ~= "" and s:sub(1, 1) ~= "[" then return s end
    return fallback
end

-- every look of the list gets its pieces loaded up front (behind the transition cover), so browsing never
-- waits on a texture
local function preloadTowers()
    if art == nil or songlist == nil then return end
    art:preload(nil)
    local songs = songlist:SearchSongsByPredicate(function(n) return true end)
    if songs == nil then return end
    for i = 0, songs.Count - 1 do
        pcall(function()
            local chart = songs[i]:GetChart(5)
            if chart ~= nil then art:preload(chart.TowerType) end
        end)
    end
end

local function refresh()
    tower_titles = {}
    tower_paths = {}
    tower_kinds = {}
    tower_info = {
            IsSong = false,

            Title = TEXTURE:CreateTexture(),
            Subtitle = TEXTURE:CreateTexture(),
            Charter = TEXTURE:CreateTexture(),

            Color = COLOR:CreateColorFromRGBA(255,255,255),
            Level = 10,
            Life = 5,
            Floor = 100,
            Side = 1
        }

    for i = loop_start,loop_end do
        tower_titles[i] = TEXTURE:CreateTexture()
        tower_kinds[i] = { kind = "none", look = nil, floors = TOWER_FLOORS_MAX, color = nil }

        if songlist == nil then
            goto continue
        end

        local node = songlist:GetSongNodeAtOffset(i)
        if node ~= nil then
            if font ~= nil then
                if node.IsReturn or node.IsRandom then
                    local name = node.IsRandom and skinString("TOWERSELECT_RANDOM", "Random Tower") or skinString("TOWERSELECT_RETURN", "Return")
                    tower_titles[i] = font:GetText(name, false, TOWERS.title_max_width)
                    if font_path ~= nil then
                        local parent = node.Parent
                        tower_paths[i] = font_path:GetText(parent ~= nil and parent.Title or "/", false, TOWERS.path_max_width, PATH_GREY)
                    end
                else
                    tower_titles[i] = font:GetText(node.Title, false, TOWERS.title_max_width)
                end
            end

            if node.IsSong then
                tower_kinds[i].kind = "song"
                local chart = node:GetChart(5)
                if chart ~= nil then
                    tower_kinds[i].look = chart.TowerType
                    tower_kinds[i].floors = math.max(1, math.min(TOWER_FLOORS_MAX, chart.TotalFloorCount or TOWER_FLOORS_MAX))
                end
            elseif node.IsReturn then tower_kinds[i].kind = "back"
            elseif node.IsRandom then tower_kinds[i].kind = "random"
            elseif node.IsFolder then
                tower_kinds[i].kind = "folder"
                tower_kinds[i].color = node.BoxColor
            end

            if i == 0 then
                tower_info.IsSong = node.IsSong

                if tower_info.IsSong then
                    tower_info.Title = tower_titles[i]

                    local tower = node:GetChart(5)
                    local life = tower.Life
                    if life == nil then life = 5 end
                    local floor = tower.TotalFloorCount
                    if floor == nil then floor = 0 end

                    tower_info.Level = tower.Level
                    tower_info.Life = life
                    tower_info.Floor = floor
                    tower_info.Side = node.Side

                    if font_small ~= nil then
                        if node.Subtitle ~= nil then
                            tower_info.Subtitle = font_small:GetText(node.Subtitle, false, INFO.subtitle.max_width)
                        end
                        if node.Maker ~= nil then
                            tower_info.Charter = font_small:GetText("Charter: "..node.Maker, false, INFO.charter.max_width)
                        end
                    end

                    tower_info.Color = COLOR:CreateColorFromHex(INFO.ex_color)
                    if tower_info.Side == 0 then tower_info.Color = COLOR:CreateColorFromHex(INFO.normal_color) end
                end
            end
        end
        ::continue::
    end
end

-- returns success, entered ("song"/"folder"/nil)
local function handleDecide()
    if songlist == nil then return false end
    local node = songlist:GetSelectedSongNode()
    if node == nil then return false end

    if node.IsFolder then
        local success = songlist:OpenFolder()
        refresh()
        return success, "folder"
    elseif node.IsReturn then
        local success = songlist:CloseFolder()
        refresh()
        return success
    elseif node.IsRandom then
        local pick = songlist:GetRandomNodeInFolder(node, true)
        if pick == nil then return false end
        return pick:Mount(5), "song"
    elseif node.IsSong then
        local success = node:Mount(5)
        return success, "song"
    end

    return false
end

local function nudge(gf) return math.floor((gf.BoxHeight - math.ceil(gf.LineHeight)) / 2) - 1 end

local function confirmItems()
    if EM.on() then return { "back", "settings", "challenge" } end
    return { "back", "settings", "customize", "challenge" }
end

local function openConfirm()
    local items = confirmItems()
    local tw, th = ctex.title.Width, ctex.title.Height
    local ow, oh = ctex.option.Width, ctex.option.Height
    local signs = { { w = tw, h = th, cx = CONF.center_x, cy = SIGN_TOP_Y, from = SIGN_TOP_Y - SIGN_DROP, start = 0, phase = SIGNS.title_phase, pose = {} } }
    local total = OPTION_SLOTS * ow + (OPTION_SLOTS - 1) * SIGN_GAP
    for i, id in ipairs(items) do
        local slot = OPTION_SLOT[id]
        signs[i + 1] = { id = id, w = ow, h = oh, cx = CONF.center_x - total / 2 + (slot - 0.5) * ow + (slot - 1) * SIGN_GAP,
                         cy = SIGN_TOP_Y + th / 2 + SIGN_ROPE + oh / 2, from = SIGN_TOP_Y,
                         start = SIGN_FALL + SIGNS.first_option_delay_s + (i - 1) * SIGN_STAGGER, phase = SIGNS.option_phase_step * i, pose = {} }
    end
    confirm = { items = items, pos = 1, t = 0, closing = nil, signs = signs }
end

-- a sign's vertical offset from its landed place at time t: not started, falling, or bouncing on its ropes
local function signDrop(s, t)
    local u = t - s.start
    if u < 0 then return nil end
    if u < SIGN_FALL then
        local k = u / SIGN_FALL
        return (s.from - s.cy) * (1 - k * k), false
    end
    u = u - SIGN_FALL
    local b = SIGNS.bounce
    return b.amplitude_px * math.sin(u * b.rad_per_s) * math.exp(-u * b.decay_per_s), true
end

-- a landed sign swings from its drop (u seconds ago), the swing dying down into the idle sway
local function signPose(s, t, u)
    local q, ph = s.pose, s.phase
    local settle = math.min(1, u / SIGN_SETTLE)
    local sw, a, x, y = SIGNS.swing, SIGNS.sway.angle, SIGNS.sway.x, SIGNS.sway.y[1]
    local swing = sw.amplitude_deg * math.sin(u * sw.rad_per_s + ph) * math.exp(-u * sw.decay_per_s)
    q.angle = swing + settle * (a[1].amplitude * math.sin(t * a[1].rad_per_s + ph * a[1].phase_scale) + a[2].amplitude * math.sin(t * a[2].rad_per_s + ph * a[2].phase_scale))
    q.x = s.cx + swing * sw.x_px_per_deg + settle * (x[1].amplitude * math.sin(t * x[1].rad_per_s + ph * x[1].phase_scale) + x[2].amplitude * math.sin(t * x[2].rad_per_s + ph * x[2].phase_scale))
    q.y = s.cy + settle * y.amplitude * math.sin(t * y.rad_per_s + ph * y.phase_scale)
    return q
end

local function at(q, dx, dy)
    return q.x + dx * q.c + dy * q.s, q.y - dx * q.s + dy * q.c
end

local function drawRope(x0, y0, x1, y1)
    local dx, dy = x1 - x0, y1 - y0
    local len = math.sqrt(dx * dx + dy * dy)
    if len < 1 then return end
    local rope = ctex.rope
    rope:SetScale(1, len / rope.Height)
    rope:SetRotation(math.deg(math.atan(dx, dy)))
    rope:DrawAtAnchor((x0 + x1) / 2, (y0 + y1) / 2, "center")
end

local function drawConfirm()
    local c = confirm
    local t = c.t
    local k = c.closing ~= nil and math.min(1, c.closing / SIGN_CLOSE) or 0
    local title = c.signs[1]
    local rise = (title.from - title.cy) * k * k
    ctex.veil:SetScale(CONF.veil_scale.x, CONF.veil_scale.y)
    ctex.veil:SetOpacity(CONF.veil_opacity * math.min(1, t / SIGN_FALL) * (1 - k))
    ctex.veil:Draw(0, 0)

    -- where every sign is this frame: the options follow the title sign they hang from
    local poses = {}
    for i, s in ipairs(c.signs) do
        local dy, landed = signDrop(s, t)
        if dy ~= nil then
            local q = signPose(s, t, landed and (t - s.start - SIGN_FALL) or 0)
            if i > 1 then
                q.x = q.x + (poses[1].x - title.cx)
                q.y = q.y + (poses[1].y - title.cy)
            end
            q.y = q.y + dy + rise
            local rad = math.rad(q.angle)
            q.s, q.c = math.sin(rad), math.cos(rad)
            poses[i] = q
        end
    end
    local tq = poses[1]
    if tq == nil then return end

    -- ropes: the title's from the top of the screen, each option's from its pair of holes along the title's bottom edge
    local thx = title.w / 2 - HOLE_X
    local tlx, tly = at(tq, -thx, -title.h / 2 + HOLE_Y)
    local trx, tr_y = at(tq, thx, -title.h / 2 + HOLE_Y)
    drawRope(title.cx - thx, CONF.rope_top_y, tlx, tly)
    drawRope(title.cx + thx, CONF.rope_top_y, trx, tr_y)
    local knot = ctex.knot
    knot:SetRotation(0)
    for i = 2, #c.signs do
        local s, q = c.signs[i], poses[i]
        if q ~= nil then
            local hx = s.w / 2 - HOLE_X
            for _, side in ipairs({ -1, 1 }) do
                local x0, y0 = at(tq, side * hx + (s.cx - title.cx), title.h / 2 - HOLE_Y)
                local x1, y1 = at(q, side * hx, -s.h / 2 + HOLE_Y)
                drawRope(x0, y0, x1, y1)
            end
        end
    end

    -- the option signs, the chosen one framed, then the title sign over them
    for i = 2, #c.signs do
        local s, q = c.signs[i], poses[i]
        if q ~= nil then
            local chosen = c.items[c.pos] == s.id
            if chosen then
                ctex.frame:SetRotation(q.angle)
                local fp = Animation.frame_pulse
                ctex.frame:SetOpacity(fp.opacity_base + fp.opacity_range * math.sin(clock * fp.rad_per_s))
                ctex.frame:DrawAtAnchor(q.x, q.y, "center")
            end
            local shade = chosen and 1 or CONF.unchosen_shade
            ctex.option:SetColor(shade, shade, shade)
            ctex.option:SetRotation(q.angle)
            ctex.option:DrawAtAnchor(q.x, q.y, "center")
            local button = ctex.buttons[s.id]
            local fit = math.min(1, (s.w - 2 * BUTTON_PAD_X) / button.Width, (s.h - BUTTON_PAD_TOP - BUTTON_PAD_BOTTOM) / button.Height)
            local bx, by = at(q, 0, (BUTTON_PAD_TOP - BUTTON_PAD_BOTTOM) / 2)
            button:SetScale(fit, fit)
            button:SetColor(shade, shade, shade)
            button:SetRotation(q.angle)
            button:DrawAtAnchor(bx, by, "center")
            local hx = s.w / 2 - HOLE_X
            for _, side in ipairs({ -1, 1 }) do
                local kx, ky = at(q, side * hx, -s.h / 2 + HOLE_Y)
                knot:DrawAtAnchor(kx, ky, "center")
            end
        end
    end
    ctex.title:SetRotation(tq.angle)
    ctex.title:DrawAtAnchor(tq.x, tq.y, "center")
    knot:DrawAtAnchor(tlx, tly, "center")
    knot:DrawAtAnchor(trx, tr_y, "center")
    for i = 2, #c.signs do
        local s = c.signs[i]
        if poses[i] ~= nil then
            for _, side in ipairs({ -1, 1 }) do
                local kx, ky = at(tq, side * (s.w / 2 - HOLE_X) + (s.cx - title.cx), title.h / 2 - HOLE_Y)
                knot:DrawAtAnchor(kx, ky, "center")
            end
        end
    end

    -- the texts, turned with their signs
    if font_title ~= nil then
        local x, y = at(tq, 0, nudge(font_title))
        font_title:Draw(skinString("TOWERSELECT_CONFIRM", "Challenge the Tower?"), x, y, COL_SIGN, COL_SIGN_OUTLINE, 1, 1, title.w - CONF.title_text_inset, "center", 0, tq.angle)
    end
end

-- returns "play" once a tower is mounted
local function updateConfirm()
    local c = confirm
    c.t = c.t + fps.deltaTime
    if c.closing ~= nil then
        c.closing = c.closing + fps.deltaTime
        if c.closing >= SIGN_CLOSE then confirm = nil end
        return nil
    end
    local falling = false
    for i, s in ipairs(c.signs) do
        if not s.landed then
            if c.t - s.start >= SIGN_FALL then
                s.landed = true
                if snd_planks[i] ~= nil then snd_planks[i]:Play() end
            else
                falling = true
            end
        end
    end

    local navPn = NavInput.p[1]
    if navPn.cancel() then
        SHARED:GetSharedSound("Cancel"):Play()
        c.closing = 0
        return nil
    end
    if falling then return nil end
    if navPn.right() then
        c.pos = c.pos % #c.items + 1
        SHARED:GetSharedSound("Move"):Play()
    elseif navPn.left() then
        c.pos = (c.pos - 2) % #c.items + 1
        SHARED:GetSharedSound("Move"):Play()
    elseif navPn.decide() then
        local id = c.items[c.pos]
        if id == "back" then
            SHARED:GetSharedSound("Cancel"):Play()
            c.closing = 0
        elseif id == "settings" then
            if act.mod_select_dialog ~= nil then act.mod_select_dialog:Activate(0) end
            SHARED:GetSharedSound("Decide"):Play()
        elseif id == "customize" then
            if act.customize_dialog ~= nil then act.customize_dialog:Activate(0) end
            SHARED:GetSharedSound("Decide"):Play()
        elseif id == "challenge" then
            local success, entered = handleDecide()
            if success and entered == "song" then
                SHARED:GetSharedSound("SongDecide"):Play()
                return "play"
            end
            SHARED:GetSharedSound("Error"):Play()
        end
    end
    return nil
end

local function closeFolder()
    if songlist == nil then return Exit("title", nil, "tower_gate_back") end
    local success = songlist:CloseFolder()
    refresh()
    return success
end

local function move(amount)
    if songlist == nil then return end
    songlist:Move(amount)

    if math.abs(amount) ~= 1 then move_counter = COUNTER:EmptyCounter()
    else move_counter = COUNTER:CreateCounter(1.0, 0.0, -Animation.move.slide_interval_s) end
    right = amount > 0
    move_counter:Start()

    refresh()
end

local function drawNumber(x, y, number, color, outline)
    if outline == nil then outline = true end

    local numarray = {}
    local value = math.floor(number)
    while value > 0 do
        table.insert(numarray, value % 10)
        value = math.floor(value / 10)
    end

    if #(numarray) <= 0 then return end

    x = x + ((tex_number_interval / 2) * (#(numarray)-1))
    local width = tex_number.Width / 10
    local height = tex_number.Height / 2

    if outline then
        local x_outline = x
        tex_number:SetColor(1.0, 1.0, 1.0)
        for _, i in ipairs(numarray) do
            tex_number:DrawRectAtAnchor(x_outline, y, width * i, 0, width, height, "Center")
            x_outline = x_outline - tex_number_interval
        end
    end

    tex_number:SetColor(color)
    for _, i in ipairs(numarray) do
        tex_number:DrawRectAtAnchor(x, y, width * i, height, width, height, "Center")
        x = x - tex_number_interval
    end
    tex_number:SetColor(1.0, 1.0, 1.0)
end

local function drawInfo(x, y, towerinfo)
    local box = towerinfo.Side == 1 and tex_info_spicy or tex_info_sweet
    box:DrawAtAnchor(x, y, "Center")

    x = x - (box.Width / 2)
    y = y - (box.Height / 2)

    towerinfo.Title:DrawAtAnchor(x+INFO.title.x, y+INFO.title.y, "Center")
    towerinfo.Subtitle:DrawAtAnchor(x+INFO.subtitle.x, y+INFO.subtitle.y, "Center")
    towerinfo.Charter:DrawAtAnchor(x+INFO.charter.x, y+INFO.charter.y, "Center")

    drawNumber(x+INFO.level.x, y+INFO.level.y, towerinfo.Level, towerinfo.Color)
    drawNumber(x+INFO.life.x, y+INFO.life.y, towerinfo.Life, towerinfo.Color, false)
    drawNumber(x+INFO.floor.x, y+INFO.floor.y, towerinfo.Floor, towerinfo.Color, false)
end

-- a few sparkles twinkling over the random badge, each turning up at a new spot every cycle
local function drawSparkles(x, y, scale, fade)
    local SP = Animation.sparkles
    for k = 1, SPARKLES do
        local cycle = clock * SPARKLE_RATE + k / SPARKLES
        local n = math.floor(cycle)
        local t = cycle - n
        local seed = (k * 7.13 + n * 3.71) % 1
        local angle = seed * 2 * math.pi
        local radius = SP.radius_min + SP.radius_range * ((seed * 9.37 + k * 0.29) % 1)
        local glow = math.sin(t * math.pi)
        local s = scale * (SP.glow_scale_base + SP.glow_scale_range * glow) * (SP.size_base + SP.size_range * ((seed * 4.1) % 1))
        tex_sparkle:SetScale(s, s)
        tex_sparkle:SetOpacity(glow * fade)
        tex_sparkle:SetRotation((n * SP.turn_per_cycle_deg + t * SP.spin_deg) % 360)
        tex_sparkle:DrawAtAnchor(x + math.cos(angle) * radius * scale, y + math.sin(angle) * radius * scale, "Center")
    end
end

function draw()
    tex_bg:Draw(0,0)
    if songlist == nil then
        tex_notice:DrawAtAnchor(Layout.notice.x, Layout.notice.y, "Center")
    else
        local function position(array, index)
            local direction = right and 1 or -1
            return array[index] - ((array[index] - (array[index+direction] or array[index])) * move_counter.Value)
        end
        -- local function scaling(pos, scale)
        --     return pos + (pos*(1-scale))
        -- end

        for i=loop_start,loop_end do
            local x = position(tower_x, i)
            local y = position(tower_y, i)
            local fade_amount = position(tower_fade, i)
            local fade = COLOR:CreateColorFromRGBA(fade_amount, fade_amount, fade_amount)
            local scale = position(tower_scale, i)

            local kind = tower_kinds[i]
            if kind ~= nil and kind.kind == "song" and art ~= nil then
                art:draw(kind.look, x, y, TOWER_SCALE * scale, kind.floors, { color = fade })
            elseif kind ~= nil and tex_icons[kind.kind] ~= nil then
                tex_pedestal:SetScale(scale, scale)
                tex_pedestal:SetColor(fade)
                tex_pedestal:DrawAtAnchor(x, y, "Bottom")
                local icon = tex_icons[kind.kind]
                local bob = math.sin(clock * Animation.badge.bob_rad_per_s + i * Animation.badge.bob_phase_step) * ICON_BOB
                local iy = y - (tex_pedestal.Height + ICON_LIFT + icon.Height / 2 + bob) * scale
                local f = fade_amount / 255
                if kind.kind == "random" then
                    local G = Animation.glow
                    local pulse = 0.5 + 0.5 * math.sin(clock * GLOW_PULSE)
                    tex_glow:SetScale(scale * (G.scale_base + G.scale_range * pulse), scale * (G.scale_base + G.scale_range * pulse))
                    tex_glow:SetOpacity((G.opacity_base + G.opacity_range * pulse) * f)
                    tex_glow:DrawAtAnchor(x, iy, "Center")
                    tex_rainbow:SetScale(scale, scale)
                    tex_rainbow:SetColor(f, f, f)
                    tex_rainbow:SetRotation((clock * RAINBOW_TURN) % 360)
                    tex_rainbow:DrawAtAnchor(x, iy, "Center")
                else
                    local c = kind.color or DISC_CREAM
                    tex_disc:SetScale(scale, scale)
                    tex_disc:SetColor(c.R / 255 * f, c.G / 255 * f, c.B / 255 * f)
                    tex_disc:DrawAtAnchor(x, iy, "Center")
                end
                icon:SetScale(scale, scale)
                icon:SetColor(fade)
                icon:DrawAtAnchor(x, iy, "Center")
                if kind.kind == "random" then drawSparkles(x, iy, scale, f) end
            end
            local title_y = y-TOWERS.title_rise+(TOWERS.title_rise*(1-scale))
            tower_titles[i]:SetScale(scale, scale)
            tower_titles[i]:SetColor(fade)
            tower_titles[i]:DrawAtAnchor(x, title_y, "Center")
            if tower_paths[i] ~= nil then
                local path = tower_paths[i]
                path:SetScale(scale, scale)
                path:SetColor(fade)
                path:DrawAtAnchor(x, title_y + (tower_titles[i].Height + path.Height) / 2 * scale, "Center")
            end

            if i == 0 and tower_info ~= nil and tower_info.IsSong then
                local y_top = y-INFO.top_offset
                local r = (fade.R * tower_info.Color.R) / 255
                local g = (fade.G * tower_info.Color.G) / 255
                local b = (fade.B * tower_info.Color.B) / 255
                local sidecolor = COLOR:CreateColorFromRGBA(r,g,b)

                drawInfo(x, y_top+INFO.y, tower_info)
            end
        end
        if confirm ~= nil then drawConfirm() end
    end
    for _, a in pairs(act) do
        if a ~= nil and a.IsActive then a:Draw() end
    end
end

function update()
    clock = clock + fps.deltaTime
    -- a dialog opened from the confirm takes the input while it shows
    local busy = false
    for _, a in pairs(act) do
        if a ~= nil and a.IsActive then a:Update(); busy = true end
    end
    if busy then
        move_counter:Tick()
        return
    end
    if confirm ~= nil then
        if updateConfirm() == "play" then return Exit("play", nil) end
        move_counter:Tick()
        return
    end
    local navPn = NavInput.p[1]
    if songlist ~= nil then
        if navPn.right() then
            move(1)
            SHARED:GetSharedSound("Move"):Play()
        elseif navPn.left() then
            move(-1)
            SHARED:GetSharedSound("Move"):Play()
        elseif navPn.decide() then
            local node = songlist:GetSelectedSongNode()
            if node ~= nil and (node.IsSong or node.IsRandom) then
                openConfirm()
                SHARED:GetSharedSound("Decide"):Play()
                move_counter:Tick()
                return
            end
            local success, entered = handleDecide()
            if success then
                SHARED:GetSharedSound(entered and "Decide" or "Cancel"):Play()
            else
                SHARED:GetSharedSound("Error"):Play()
            end
        elseif navPn.cancel() then
            if closeFolder() then
                SHARED:GetSharedSound("Cancel"):Play()
            else
                SHARED:GetSharedSound("Cancel"):Play()
                return Exit("title", nil, "tower_gate_back")
            end
        end
    else
        if navPn.cancel() then
            SHARED:GetSharedSound("Cancel"):Play()
            return Exit("title", nil, "tower_gate_back")
        end
    end
    move_counter:Tick()
end

function activate()
    CONFIG.PlayerCount = 1
    CONFIG.SongSpeed   = 20   -- reset speed (other modes may have changed it)

    art = TowerArt.load()
    preloadTowers()
    tex_pedestal = TEXTURE:CreateTexture("Textures/pedestal.png")
    tex_icons = {
        back = TEXTURE:CreateTexture("Textures/icon_back.png"),
        random = TEXTURE:CreateTexture("Textures/icon_random.png"),
        folder = TEXTURE:CreateTexture("Textures/icon_folder.png"),
    }
    tex_disc = TEXTURE:CreateTexture("Textures/icon_disc.png")
    tex_rainbow = TEXTURE:CreateTexture("Textures/icon_rainbow.png")
    tex_glow = TEXTURE:CreateTexture("Textures/icon_glow.png")
    tex_glow:SetBlendMode("add")
    tex_sparkle = TEXTURE:CreateTexture("Textures/sparkle.png")
    tex_sparkle:SetBlendMode("add")
    ctex = {
        title = TEXTURE:CreateTexture("Textures/Confirm/plank_title.png"),
        option = TEXTURE:CreateTexture("Textures/Confirm/plank_option.png"),
        rope = TEXTURE:CreateTexture("Textures/Confirm/rope.png"),
        knot = TEXTURE:CreateTexture("Textures/Confirm/knot.png"),
        frame = TEXTURE:CreateTexture("Textures/Confirm/frame.png"),
        veil = TEXTURE:CreateTexture("Textures/Confirm/veil.png"),
        buttons = {},
    }
    for _, id in ipairs({ "back", "settings", "customize", "challenge" }) do
        ctex.buttons[id] = TEXTURE:CreateTexture("Textures/Confirm/option_" .. id .. ".png")
    end
    for i = 1, 5 do
        local s = SOUND:CreateSFX("Sounds/plank.ogg")
        s:SetSpeed(i == 1 and 0.8 or 1 + 0.08 * (i - 2))   -- the big sign lands lower, each option a bit higher
        snd_planks[i] = s
    end
    font_title = TEXT:CreateGlyphCached(Layout.fonts.confirm)
    act.customize_dialog = ACTIVITY:GetActivity("customize_dialog")
    act.mod_select_dialog = ACTIVITY:GetActivity("mod_select_dialog")
    confirm = nil
    tex_bg = TEXTURE:CreateTexture("Textures/BG.png")
    tex_info = TEXTURE:CreateTexture("Textures/Info.png")
    tex_info_spicy = TEXTURE:CreateTexture("Textures/Spicy_Full.png")
    tex_info_sweet = TEXTURE:CreateTexture("Textures/Sweet_Full.png")
    tex_number = TEXTURE:CreateTexture("Textures/Number.png")
    font = TEXT:Create(Layout.fonts.title)
    font_small = TEXT:Create(Layout.fonts.small)
    font_path = TEXT:Create(Layout.fonts.path)
    tex_notice = font:GetText("Songlist is currently enumerating or unavailable.")
    refresh()
end

function deactivate()
    if art ~= nil then art:dispose(); art = nil end
    if tex_pedestal ~= nil then tex_pedestal:Dispose(); tex_pedestal = nil end
    for _, t in pairs(tex_icons) do t:Dispose() end
    tex_icons = {}
    if tex_disc ~= nil then tex_disc:Dispose(); tex_disc = nil end
    if tex_rainbow ~= nil then tex_rainbow:Dispose(); tex_rainbow = nil end
    if tex_glow ~= nil then tex_glow:Dispose(); tex_glow = nil end
    if tex_sparkle ~= nil then tex_sparkle:Dispose(); tex_sparkle = nil end
    for _, t in pairs(ctex.buttons or {}) do t:Dispose() end
    ctex.buttons = nil
    for _, t in pairs(ctex) do t:Dispose() end
    ctex = {}
    for _, s in pairs(snd_planks) do s:Dispose() end
    snd_planks = {}
    if font_title ~= nil then font_title:Dispose(); font_title = nil end
    act = {}
    confirm = nil
    if tex_bg ~= nil then tex_bg:Dispose() end
    if tex_info ~= nil then tex_info:Dispose() end
    if tex_info_spicy ~= nil then tex_info_spicy:Dispose() end
    if tex_info_sweet ~= nil then tex_info_sweet:Dispose() end
    if tex_number ~= nil then tex_number:Dispose() end
    if font ~= nil then font:Dispose() end
    if font_small ~= nil then font_small:Dispose() end
    if font_path ~= nil then font_path:Dispose(); font_path = nil end
    if tex_notice ~= nil then tex_notice:Dispose() end
    move_counter = COUNTER:EmptyCounter()
end

function onStart()
    for i = loop_start,loop_end do
        local arc = i + math.floor(i*TOWERS.arc_factor)
        tower_x[i] = TOWERS.center_x + (TOWERS.spacing_x * i)
        tower_y[i] = TOWERS.base_y + math.abs(TOWERS.arc_drop * arc)
        --title_y[i] = 540 + math.abs(20 * arc)
        tower_scale[i] = 1 - math.abs(i / TOWERS.scale_divisor)
        if i ~= 0 then tower_fade[i] = math.floor(255 / ((math.abs(i)+1)*TOWERS.fade_factor))
        else tower_fade[0] = 255
        end
    end
end

function afterSongEnum()
    local settings = GenerateSongListSettings()
    settings.RootGenreFolder = "太鼓タワー"
    settings.MandatoryDifficultyList = {5}
    settings.FlattenOpenedFolders = false
    songlist = RequestSongList(settings)
    preloadTowers()
    refresh()
end

function onDestroy()
end
