---@diagnostic disable: undefined-global  -- TEXTURE/TEXT injected by CLuaScript at runtime
-- popup_menu ROActivity: pause list popup rendering; input and actions in C# (CPopupMenuManager)
-- activate(title, items ("\n"-joined), fontSize, <PopupMenu_* positions...>); draw(selectedIndex) per frame
-- the cursor's tile count: Config/layout.json

local Config = require("Core.Config")
local Layout = Config.load("Config/layout.json")
local CURSOR_TILES = Layout.cursor.middle_tiles

local _active = false

local bg = nil          -- Textures/Menu_Title.png
local cursor = nil      -- Textures/Menu_Highlight.png, 16-tile stretch
local font = nil
local fontSize = 18

local titleTex = nil
local itemTexs = {}

-- positions (skin PopupMenu_*), filled by activate()
local pTitleBgX, pTitleBgY = 460, 40
local pTitleX, pTitleY = 540, 44
local pHlX, pHlY = 480, 46
local pNameX, pNameY = 480, 77
local moveX, moveY = 0, 32

local function split(s)
    local t = {}
    for line in (s .. "\n"):gmatch("(.-)\n") do
        t[#t + 1] = line
    end
    return t
end

function onStart()
    -- sync load: cursor size valid on the first pause
    bg = TEXTURE:CreateTextureSync("Textures/Menu_Title.png")
    cursor = TEXTURE:CreateTextureSync("Textures/Menu_Highlight.png")
end

function activate(title, itemsJoined, size,
                  titleBgX, titleBgY, titleX, titleY, hlX, hlY, nameX, nameY, mvX, mvY)
    _active = true

    pTitleBgX, pTitleBgY = titleBgX, titleBgY
    pTitleX, pTitleY = titleX, titleY
    pHlX, pHlY = hlX, hlY
    pNameX, pNameY = nameX, nameY
    moveX, moveY = mvX, mvY

    -- GetText caches by string: reopening the same menu is free
    if font == nil or size ~= fontSize then
        fontSize = size
        font = TEXT:Create(fontSize)
    end
    titleTex = font:GetText(title)
    itemTexs = {}
    for _, name in ipairs(split(itemsJoined)) do
        itemTexs[#itemTexs + 1] = font:GetText(name)
    end
end

function deactivate()
    _active = false
end

function draw(selected)
    if not _active then return end

    if bg ~= nil then bg:Draw(pTitleBgX, pTitleBgY) end

    if titleTex ~= nil then titleTex:Draw(pTitleX, pTitleY) end

    -- cursor: left cap + 16 middle tiles + right cap, on the selected line
    if cursor ~= nil then
        local curX = pHlX + (moveX * (selected + 1))
        local curY = pHlY + (moveY * (selected + 1))
        local w = math.floor(cursor.Width / 2)
        local h = cursor.Height
        cursor:DrawRect(curX, curY, 0, 0, w, h)
        curX = curX + w
        for _ = 1, CURSOR_TILES do
            cursor:DrawRect(curX, curY, math.floor(w / 2), 0, w, h)
            curX = curX + w
        end
        cursor:DrawRect(curX, curY, w, 0, w, h)
    end

    for i, t in ipairs(itemTexs) do
        if t ~= nil then t:Draw(pNameX + (i - 1) * moveX, pNameY + (i - 1) * moveY) end
    end
end

function onDestroy()
    -- bg and cursor disposed with the script (CreateTextureSync); text owned by the GetText cache
end
