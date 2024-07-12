import ('System.Drawing')

local font = -1
local font_offset = -1
local font_size = -1
local font_padding = -1

local chars = { ' ', '!', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/',
'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 
':', ';', '<', '=', '>', '?',
'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
'[', '\\', ']', '^', '_', 
'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
'{', '|', '}', '~' }

local font_white_texturekey = { }
local font_cyan_texturekey = { }
local font_gray_texturekey = { }

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

    font_size = getNum(config["font"]["size"])
    font_offset = getNum(config["font"]["offset"])
    font_padding = getNum(config["font"]["padding"])
    font = loadFontRenderer(font_size, "regular")

    for i = 1, #chars do
        font_white_texturekey[i] = createTitleTextureKey(chars[i], font, 99999, Color.White)
        font_cyan_texturekey[i] = createTitleTextureKey(chars[i], font, 99999, Color.Cyan)
        font_gray_texturekey[i] = createTitleTextureKey(chars[i], font, 99999, Color.Gray)
    end
end

function drawText(x, y, type, text)
    font_keys = nil
    if type == "white" then
        font_keys = font_white_texturekey
    elseif type == "cyan" then
        font_keys = font_cyan_texturekey
    elseif type == "gray" then
        font_keys = font_gray_texturekey
    elseif type == "whiteslim" then
        font_keys = font_white_texturekey
    elseif type == "cyanslim" then
        font_keys = font_cyan_texturekey
    elseif type == "grayslim" then
        font_keys = font_gray_texturekey
    else
        font_keys = font_white_texturekey
    end

    for i = 1, string.len(text) do
        getTextTex(font_keys[getIndex(string.sub(text, i, i))], false, false):t2D_DisplayImage_AnchorCenter(x + (font_size * font_offset), y + (font_size * font_offset))
        x = x + font_padding
    end
end