---@diagnostic disable: undefined-global, undefined-field, need-check-nil, unused-local
-- demo_aviutl2 (debug stage)
-- ------------------------------------------------------------------------------------------------------
-- The AviUtl2 projects of Project/, Telop/, MovementTest/ and Camera/ in the video_player ROActivity, to try the AVIUTL2 API: a list of the
-- projects, Decide plays one, closing the player comes back to the list and Esc on the list brings My
-- Room back.

local PopUI = require("UI.Pick")()

local PROJECTS = {
    { text = "project.aup2",       value = "Project/project.aup2", nameplates = true },
    { text = "project-5p.aup2",    value = "Project/project-5p.aup2", nameplates = true },
    { text = "telop.aup2",         value = "Telop/telop.aup2" },
    { text = "MovementTest.aup2",  value = "MovementTest/MovementTest.aup2" },
    { text = "camera.aup2 (3D)",   value = "Camera/camera.aup2" },
    { text = "camera-nofilters.aup2 (3D)", value = "Camera/camera-nofilters.aup2" },
}

local PANEL_W, PANEL_H = 760, 556
local ROW = 64

-- player_box.png holds the difficulty only: the nameplate of the box's player is drawn in the room left
local BOX_W, BOX_H = 520, 216
local PLATE_X, PLATE_Y = 170, 88

local ui, player
local playing = false
local chosen
local boxTex

local function withNameplates(project)
    project:SetImageDrawer("player_box.png", BOX_W, BOX_H, function(rank)
        if boxTex ~= nil then boxTex:Draw(0, 0) end
        NAMEPLATE:DrawPlayerNameplate(PLATE_X, PLATE_Y, 255, rank)
    end)
end

local function sfx(name)
    pcall(function() SHARED:GetSharedSound(name):Play() end)
end

function onStart() end

function activate()
    player = ROACTIVITY:GetROActivity("video_player")
    ui = PopUI.new{}
    local x, y = (1920 - PANEL_W) / 2, (1080 - PANEL_H) / 2
    local panel = ui:panel{ x = x, y = y, w = PANEL_W, h = PANEL_H, pad = 30, title = "AviUtl2" }
    local cx, cy, cw, ch = panel:content()
    ui:menu{ x = cx, y = cy, w = cw, h = ch, rowHeight = ROW, items = PROJECTS, selected = 1,
        onSelect = function(i) chosen = PROJECTS[i]; return true end }
    boxTex = TEXTURE:CreateTexture("Project/player_box.png")
end

function deactivate()
    if player ~= nil and player.IsActive then player:Deactivate() end
    player = nil
    if ui ~= nil then ui:disposeWidgets(); ui = nil end
    if boxTex ~= nil then boxTex:Dispose(); boxTex = nil end
end

function update(ts)
    if playing then
        player:Update()
        if not player.IsActive then playing = false end
        return
    end
    local r = ui:update(ts)
    if r == "cancel" then
        sfx("Cancel")
        return Exit("stage", "my_room")
    end
    if chosen ~= nil then
        local it = chosen
        chosen = nil
        if player ~= nil then
            local loaded = player:Activate(info.dir .. "/" .. it.value)
            if it.nameplates and loaded ~= nil and loaded[0] ~= nil then withNameplates(loaded[0]) end
            playing = player.IsActive
        end
    end
end

function draw()
    if playing and player ~= nil and player.IsActive then
        player:Draw()
    elseif ui ~= nil then
        ui:draw()
    end
end

function afterSongEnum() end
function onDestroy() end
