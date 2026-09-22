local NavInput = require("NavInput")
local TowerArt = require("TowerArt")

local songlist = nil

local art = nil   -- the towers, composed from the pieces of each chart's tower look (Lib/TowerArt.lua)
local tex_pedestal = nil      -- back, random and folder entries: a badge floating over a pedestal
local tex_icons = {}
local tex_bg = TEXTURE:CreateTexture()
local tex_info = TEXTURE:CreateTexture()
local tex_info_spicy = TEXTURE:CreateTexture()
local tex_info_sweet = TEXTURE:CreateTexture()
local tex_notice = TEXTURE:CreateTexture()
local tex_number = TEXTURE:CreateTexture()
local font = nil
local font_small = nil

local move_counter = COUNTER:EmptyCounter()
local hold_counter = COUNTER:EmptyCounter()
local right = true

local loop_start = -3
local loop_end = 3

local tower_x = {}
local tower_y = {}
local tower_fade = {}
local title_y = {}
local tower_scale = {}

local tower_titles = {}
local tower_kinds = {}   -- per slot: kind "song" (with the tower look and the floors drawn), "back", "random", "folder" or "none"
local tower_info = nil
local clock = 0

local ICON_LIFT = 60      -- the badge floats this far over the pedestal
local ICON_BOB = 8        -- and bobs by this much

local tex_number_interval = 32

local config = JSONLOADER:JsonParseFile("Config.json")
local TOWER_SCALE = JSONLOADER:JsonGet(config, "tower_scale") or 0.36
local TOWER_FLOORS_MAX = JSONLOADER:JsonGet(config, "tower_floors_max") or 3

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
        tower_kinds[i] = { kind = "none", look = nil, floors = TOWER_FLOORS_MAX }

        if songlist == nil then
            goto continue
        end

        local node = songlist:GetSongNodeAtOffset(i)
        if node ~= nil then
            if font ~= nil then
                tower_titles[i] = font:GetText(node.Title, false, 480)
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
            elseif node.IsFolder then tower_kinds[i].kind = "folder"
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
                            tower_info.Subtitle = font_small:GetText(node.Subtitle, false, 420)
                        end
                        if node.Maker ~= nil then
                            tower_info.Charter = font_small:GetText("Charter: "..node.Maker, false, 230)
                        end
                    end

                    tower_info.Color = COLOR:CreateColorFromHex(config["ex_color"])
                    if tower_info.Side == 0 then tower_info.Color = COLOR:CreateColorFromHex(config["normal_color"]) end
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

local function closeFolder()
    if songlist == nil then return Exit("title", nil) end
    local success = songlist:CloseFolder()
    refresh()
    return success
end

local function move(amount)
    if songlist == nil then return end
    songlist:Move(amount)

    if math.abs(amount) ~= 1 then move_counter = COUNTER:EmptyCounter()
    else move_counter = COUNTER:CreateCounter(1.0, 0.0, -config["move_speed"] or -0.25) end
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

    towerinfo.Title:DrawAtAnchor(x+270, y+60, "Center")
    towerinfo.Subtitle:DrawAtAnchor(x+270, y+93, "Center")
    towerinfo.Charter:DrawAtAnchor(x+395, y+160, "Center")

    drawNumber(x+202, y+150, towerinfo.Level, towerinfo.Color)
    drawNumber(x+202, y+233, towerinfo.Life, towerinfo.Color, false)
    drawNumber(x+434, y+233, towerinfo.Floor, towerinfo.Color, false)
end

function draw()
    tex_bg:Draw(0,0)
    if songlist == nil then
        tex_notice:DrawAtAnchor(960, 540, "Center")
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
                local bob = math.sin(clock * 2 + i * 0.8) * ICON_BOB
                icon:SetScale(scale, scale)
                icon:SetColor(fade)
                icon:DrawAtAnchor(x, y - (tex_pedestal.Height + ICON_LIFT + icon.Height / 2 + bob) * scale, "Center")
            end
            tower_titles[i]:SetScale(scale, scale)
            tower_titles[i]:SetColor(fade)
            tower_titles[i]:DrawAtAnchor(x, y-540+(540*(1-scale)), "Center")

            if i == 0 and tower_info ~= nil and tower_info.IsSong then
                local y_top = y-1080
                local r = (fade.R * tower_info.Color.R) / 255
                local g = (fade.G * tower_info.Color.G) / 255
                local b = (fade.B * tower_info.Color.B) / 255
                local sidecolor = COLOR:CreateColorFromRGBA(r,g,b)

                drawInfo(x, y_top+175, tower_info)
            end
        end
    end
end

function update()
    clock = clock + fps.deltaTime
    local navPn = NavInput.p[1]
    if songlist ~= nil then
        if navPn.right() then
            move(1)
            SHARED:GetSharedSound("Move"):Play()
        elseif navPn.left() then
            move(-1)
            SHARED:GetSharedSound("Move"):Play()
        elseif navPn.decide() then
            local success, entered = handleDecide()
            if entered == "song" and success then
                SHARED:GetSharedSound("SongDecide"):Play()
                return Exit("play", nil)
            end
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
                return Exit("title", nil)
            end
        end
    else
        if navPn.cancel() then
            SHARED:GetSharedSound("Cancel"):Play()
            return Exit("title", nil)
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
    tex_bg = TEXTURE:CreateTexture("Textures/BG.png")
    tex_info = TEXTURE:CreateTexture("Textures/Info.png")
    tex_info_spicy = TEXTURE:CreateTexture("Textures/Spicy_Full.png")
    tex_info_sweet = TEXTURE:CreateTexture("Textures/Sweet_Full.png")
    tex_number = TEXTURE:CreateTexture("Textures/Number.png")
    font = TEXT:Create(20)
    font_small = TEXT:Create(12)
    tex_notice = font:GetText("Songlist is currently enumerating or unavailable.")
    refresh()
end

function deactivate()
    if art ~= nil then art:dispose(); art = nil end
    if tex_pedestal ~= nil then tex_pedestal:Dispose(); tex_pedestal = nil end
    for _, t in pairs(tex_icons) do t:Dispose() end
    tex_icons = {}
    if tex_bg ~= nil then tex_bg:Dispose() end
    if tex_info ~= nil then tex_info:Dispose() end
    if tex_info_spicy ~= nil then tex_info_spicy:Dispose() end
    if tex_info_sweet ~= nil then tex_info_sweet:Dispose() end
    if tex_number ~= nil then tex_number:Dispose() end
    if font ~= nil then font:Dispose() end
    if font_small ~= nil then font_small:Dispose() end
    if tex_notice ~= nil then tex_notice:Dispose() end
    move_counter = COUNTER:EmptyCounter()
end

function onStart()
    for i = loop_start,loop_end do
        local arc = i + math.floor(i*0.75)
        tower_x[i] = 960 + (500 * i)
        tower_y[i] = 1080 + math.abs(20 * arc)
        --title_y[i] = 540 + math.abs(20 * arc)
        tower_scale[i] = 1 - math.abs(i / 10)
        if i ~= 0 then tower_fade[i] = math.floor(255 / ((math.abs(i)+1)*0.75))
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
