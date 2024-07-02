import ('System.Drawing')

local config = -1

local menu_font_normal = -1
local menu_font_description = -1
local menu_item_text = { }
local menu_item_text_selected = { }
local description_panel_text = -1
local background = -1
local arrow = -1
local cursor = -1
local enum_song = -1
local header = -1
local itembox = -1
local keyassign = -1

local background_x = 0
local background_y = 0
local background_scroll_interval = 0

local item_x = { }
local item_y = { }
local item_width = -1

local description_panel_x = 0
local description_panel_y = 0

local itembox_count = 0
local itembox_center = 0
local itembox_x = { }
local itembox_y = { }

local keyassign_x = 0
local keyassign_y = 0

local enum_song_x = 0
local enum_song_y = 0

function genItembox()
    for i = 1, itembox_count do
    end
end

function genMenuItemLeft(index, name)
    menu_item_text[index + 1] = createTitleTextureKey(name, menu_font_normal, 99999)
    menu_item_text_selected[index + 1] = createTitleTextureKey(name, menu_font_normal, 99999, Color.Yellow)
end

function genDescriptionPanel(text)
    description_panel_text = createTitleTextureKey(text, menu_font_description, 99999)
end

function background_update()
    background_x = background_x - (background.szTextureSize.Width * fps.deltaTime / background_scroll_interval)
    if background_x < -background.szTextureSize.Width then
        background_x = 0
    end
end

function background_draw()
    for i = 0, 3 do
        background:t2D_DisplayImage(background_x + i * background.szTextureSize.Width, background_y)
    end
end

function header_update()
end

function header_draw()
    header:t2D_DisplayImage(0, 0)
end

function cursor_update()
end

function cursor_draw()
    x = item_x[configstageinfo.nCursorIndex]
    y = item_y[configstageinfo.nCursorIndex]
    width = cursor.szTextureSize.Width
    height = cursor.szTextureSize.Height
    
    cursor:t2D_DisplayImage_AnchorCenter(x - (width / 2) - item_width, y, Rectangle(0, 0, width, height))
    cursor:t2D_DisplayImage_AnchorCenter(x + (width / 2) + item_width, y, Rectangle(width * 2, 0, width, height))

    cursor:tSetScale((item_width / width) * 2.0, 1.0)
    cursor:t2D_DisplayImage_AnchorCenter(x, y, Rectangle(width, 0, width, height))
    cursor:tSetScale(1.0, 1.0)
end

function text_update()
end

function text_draw()
    for i = 1, 3 do
        x = item_x[i - 1]
        y = item_y[i - 1]
        if configstageinfo.nCursorIndex == i - 1 then
            tex = getTextTex(menu_item_text_selected[i], false, false)
            tex:t2D_DisplayImage_AnchorCenter(x, y)
        else
            tex = getTextTex(menu_item_text[i], false, false)
            tex:t2D_DisplayImage_AnchorCenter(x, y)
        end
    end
end

function description_panel_update()
end

function description_panel_draw()
    if not(description_panel_text == -1) then 
        tex = getTextTex(description_panel_text, false, false)
        tex:t2D_DisplayImage(description_panel_x, description_panel_y)
    end
end

function itembox_update()
    for i = 1, itembox_count do
    end
end

function itembox_draw()
    for i = 1, itembox_count do
        x = itembox_x[i - 1]
        y = itembox_y[i - 1]

        itembox:t2D_DisplayImage(x, y)
    end
end

function Keyassign_update()
end

function Keyassign_draw()
    keyassign:t2D_DisplayImage(keyassign_x, keyassign_y)
end

function enum_song_update()
end

function enum_song_draw()
    enum_song:t2D_DisplayImage(enum_song_x, enum_song_y)
end

function loadAssets()
    config = loadConfig("Config.json")
    
    menu_font_normal = loadFontRenderer(getNum(config["font_size"]["normal"]), "regular")
    menu_font_description = loadFontRenderer(getNum(config["font_size"]["description"]), "regular")
    background = loadTexture("Background.png")
    arrow = loadTexture("Arrow.png")
    cursor = loadTexture("Cursor.png")
    enum_song = loadTexture("Enum_Song.png")
    header = loadTexture("Header.png")
    itembox = loadTexture("ItemBox.png")
    keyassign = loadTexture("KeyAssign.png")
    
    background_scroll_interval = getNum(config["background"]["scroll_interval"])
    item_x = getNumArray(config["item"]["x"])
    item_y = getNumArray(config["item"]["y"])
    item_width = getNum(config["item"]["width"])

    description_panel_x = getNum(config["description_panel"]["x"])
    description_panel_y = getNum(config["description_panel"]["y"])

    itembox_count = getNum(config["itembox"]["count"])
    itembox_center = getNum(config["itembox"]["center"])
    itembox_x = getNumArray(config["itembox"]["x"])
    itembox_y = getNumArray(config["itembox"]["y"])

    keyassign_x = getNum(config["keyassign"]["x"])
    keyassign_y = getNum(config["keyassign"]["y"])

    enum_song_x = getNum(config["enum_song"]["x"])
    enum_song_y = getNum(config["enum_song"]["y"])
end

function init()
end

function final()
end

function update()
    background_update()
    header_update()
    cursor_update()
    text_update()
    description_panel_update()
    itembox_update()
    Keyassign_update()
end

function draw()
    background_draw()
    header_draw()
    cursor_draw()
    text_draw()
    description_panel_draw()
    itembox_draw()
    Keyassign_draw()
end
