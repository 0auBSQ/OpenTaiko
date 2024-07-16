import ('System.Drawing')

local font = -1
local config_font_offset = -1
local config_font_size = -1
local config_font_padding = -1

local chars = { ' ', '!', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/',
'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 
':', ';', '<', '=', '>', '?',
'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
'[', '\\', ']', '^', '_', 
'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
'{', '|', '}', '~' }

local font_texturekey = { }
local font_bold_texturekey = { }

function getIndex(char)
    for i = 1, #chars do
        if chars[i] == char then
            return i
        end
    end
    return 1
end

function reloadLanguage(lang)
end

function loadAssets()
    config = loadConfig("Config.json")

    config_font_size = getNum(config["font"]["size"])
    config_font_offset = getNum(config["font"]["offset"])
    config_font_padding = getNum(config["font"]["padding"])
    font = loadFontRenderer(config_font_size, "regular")

    for i = 1, #chars do
        font_texturekey[i] = createTitleTextureKey(chars[i], font, 99999)
        font_bold_texturekey[i] = createTitleTextureKey(chars[i], font, 99999, Color.Blue)
    end
end

function drawText(x, y, text, bold, scale)
    font_keys = nil
    if bold then
        font_keys = font_bold_texturekey
    else
        font_keys = font_texturekey
    end

    for i = 1, string.len(text) do
        getTextTex(font_keys[getIndex(string.sub(text, i, i))], false, false):t2D_DisplayImage_AnchorCenter(x + (config_font_size * config_font_offset), y + (config_font_size * config_font_offset))
        x = x + config_font_padding
    end
end