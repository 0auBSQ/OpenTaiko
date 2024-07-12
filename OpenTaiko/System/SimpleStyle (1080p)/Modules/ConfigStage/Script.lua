import ('System.Drawing')

local config = -1

local font_normal = -1
local font_description = -1
local item_text = { }
local item_text_selected = { }
local description_panel_text = -1
local itembox_texts = { }
local background = -1
local cursor = -1
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
local itembox_infos = { }

local itembox_name_offset_x = nil
local itembox_name_offset_y = nil

local itembox_value_offset_x = nil
local itembox_value_offset_y = nil

local keyassign_x = 0
local keyassign_y = 0

function genItembox()
    for i = 1, itembox_count do
        itembox_infos[i] = getItemBox(i - itembox_center - 1)
        itembox_titlekey = {}
        itembox_titlekey.name = createTitleTextureKey(itembox_infos[i].strName, font_normal, 99999)
        itembox_titlekey.value = createTitleTextureKey(itembox_infos[i]:tGetValueText(), font_normal, 99999)
        itembox_texts[i] = itembox_titlekey
    end
end

function genMenuItemLeft(index, name)
    item_text[index + 1] = createTitleTextureKey(name, font_normal, 99999)
    item_text_selected[index + 1] = createTitleTextureKey(name, font_normal, 99999, Color.Yellow)
end

function genDescriptionPanel(text)
    description_panel_text = createTitleTextureKey(text, font_description, 99999)
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
            tex = getTextTex(item_text_selected[i], false, false)
            tex:t2D_DisplayImage_AnchorCenter(x, y)
        else
            tex = getTextTex(item_text[i], false, false)
            tex:t2D_DisplayImage_AnchorCenter(x, y)
        end
    end
end

function description_panel_update()
end

function description_panel_draw()
    if not(description_panel_text == -1) then 
        desc_panel_tex = getTextTex(description_panel_text, false, false)
        desc_panel_tex:t2D_DisplayImage(description_panel_x, description_panel_y)
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

        itembox_name_tex = getTextTex(itembox_texts[i].name, false, false)
        itembox_name_tex:t2D_DisplayImage(x + itembox_name_offset_x, y + itembox_name_offset_y)

        itembox_value_tex = getTextTex(itembox_texts[i].value, false, false)
        itembox_value_tex:t2D_DisplayImage(x + itembox_value_offset_x, y + itembox_value_offset_y)
    end
end

function Keyassign_update()
end

function Keyassign_draw()
    if configstageinfo.bWaitingKeyInput then
        keyassign:t2D_DisplayImage(keyassign_x, keyassign_y)
    end
end

function reloadLanguage(lang)
end

function loadAssets()
    config = loadConfig("Config.json")
    
    font_normal = loadFontRenderer(getNum(config["font_size"]["normal"]), "regular")
    font_description = loadFontRenderer(getNum(config["font_size"]["description"]), "regular")
    background = loadTexture("Background.png")
    cursor = loadTexture("Cursor.png")
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

    itembox_name_offset_x = getNum(config["itembox_name"]["offset_x"])
    itembox_name_offset_y = getNum(config["itembox_name"]["offset_y"])

    itembox_value_offset_x = getNum(config["itembox_value"]["offset_x"])
    itembox_value_offset_y = getNum(config["itembox_value"]["offset_y"])

    keyassign_x = getNum(config["keyassign"]["x"])
    keyassign_y = getNum(config["keyassign"]["y"])
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
