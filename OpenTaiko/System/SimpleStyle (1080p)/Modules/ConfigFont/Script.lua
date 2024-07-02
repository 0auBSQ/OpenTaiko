import ('System.Drawing')

local font = -1
local font_bold = -1
local menu_font_size = -1

local chars = { ' ', '!', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/',
'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 
':', ';', '<', '=', '>', '?',
'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
'[]', '\\', ']', '^', '_', 
'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
'{', '|', '}', '~' }

local font_texs = -1
local font_bold_texs = -1

function loadAssets()
    config = loadConfig("Config.json")

    --font = loadTexture("Font.png")
    --font_bold = loadTexture("Font_Bold.png")
    menu_font_size = loadFontRenderer(getNum(config["font_size"]), "regular")

    for i = 1, #chars do
        font_texturekey = createTitleTextureKey(chars[i], menu_font_size, 99999)
        font_texs[chars[i]] = getTextTex(font_texturekey, false, false)
        
        font_bold_texturekey = createTitleTextureKey(chars[i], menu_font_size, 99999)
        font_bold_texs[chars[i]] = getTextTex(font_bold_texturekey, false, false)
    end
end

function drawText(x, y, text, bold, scale)
    for i = 1, string.len(text) do
    end
end