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
local config_background_scroll_interval = 0

local config_item_x = { }
local config_item_y = { }
local config_item_width = -1

local config_description_panel_x = 0
local config_description_panel_y = 0

local config_itembox_count = 0
local config_itembox_center = 0
local config_itembox_x = { }
local config_itembox_y = { }

local config_itembox_name_offset_x = nil
local config_itembox_name_offset_y = nil

local config_itembox_value_offset_x = nil
local config_itembox_value_offset_y = nil

local config_keyassign_x = 0
local config_keyassign_y = 0

local itembox_infos = { }

function genItembox()
    for i = 1, config_itembox_count do
        itembox_infos[i] = getItemBox(i - config_itembox_center - 1)
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
    background_x = background_x - (background.szTextureSize.Width * fps.deltaTime / config_background_scroll_interval)
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
    x = config_item_x[configstageinfo.nCursorIndex]
    y = config_item_y[configstageinfo.nCursorIndex]
    width = cursor.szTextureSize.Width
    height = cursor.szTextureSize.Height
    
    cursor:t2D_DisplayImage_AnchorCenter(x - (width / 2) - config_item_width, y, Rectangle(0, 0, width, height))
    cursor:t2D_DisplayImage_AnchorCenter(x + (width / 2) + config_item_width, y, Rectangle(width * 2, 0, width, height))

    cursor:tSetScale((config_item_width / width) * 2.0, 1.0)
    cursor:t2D_DisplayImage_AnchorCenter(x, y, Rectangle(width, 0, width, height))
    cursor:tSetScale(1.0, 1.0)
end

function text_update()
end

function text_draw()
    for i = 1, 3 do
        x = config_item_x[i - 1]
        y = config_item_y[i - 1]
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
        desc_panel_tex:t2D_DisplayImage(config_description_panel_x, config_description_panel_y)
    end
end

function itembox_update()
    for i = 1, config_itembox_count do
    end
end

function itembox_draw()
    for i = 1, config_itembox_count do
        x = config_itembox_x[i - 1]
        y = config_itembox_y[i - 1]

        itembox:t2D_DisplayImage(x, y)

        itembox_name_tex = getTextTex(itembox_texts[i].name, false, false)
        itembox_name_tex:t2D_DisplayImage(x + config_itembox_name_offset_x, y + config_itembox_name_offset_y)

        itembox_value_tex = getTextTex(itembox_texts[i].value, false, false)
        itembox_value_tex:t2D_DisplayImage(x + config_itembox_value_offset_x, y + config_itembox_value_offset_y)
    end
end

function Keyassign_update()
end

function Keyassign_draw()
    if configstageinfo.bWaitingKeyInput then
        keyassign:t2D_DisplayImage(config_keyassign_x, config_keyassign_y)
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
    
    config_background_scroll_interval = getNum(config["background"]["scroll_interval"])
    config_item_x = getNumArray(config["item"]["x"])
    config_item_y = getNumArray(config["item"]["y"])
    config_item_width = getNum(config["item"]["width"])

    config_description_panel_x = getNum(config["description_panel"]["x"])
    config_description_panel_y = getNum(config["description_panel"]["y"])

    config_itembox_count = getNum(config["itembox"]["count"])
    config_itembox_center = getNum(config["itembox"]["center"])
    config_itembox_x = getNumArray(config["itembox"]["x"])
    config_itembox_y = getNumArray(config["itembox"]["y"])

    config_itembox_name_offset_x = getNum(config["itembox_name"]["offset_x"])
    config_itembox_name_offset_y = getNum(config["itembox_name"]["offset_y"])

    config_itembox_value_offset_x = getNum(config["itembox_value"]["offset_x"])
    config_itembox_value_offset_y = getNum(config["itembox_value"]["offset_y"])

    config_keyassign_x = getNum(config["keyassign"]["x"])
    config_keyassign_y = getNum(config["keyassign"]["y"])
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
