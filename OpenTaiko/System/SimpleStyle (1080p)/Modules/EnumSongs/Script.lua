import ('System.Drawing')

local config = nil

local font = nil

local enum_song = nil
local configstage_enum_song = nil

local enum_song_x = nil
local enum_song_y = nil

local configstage_enum_song_x = nil
local configstage_enum_song_y = nil

local text_x = nil
local text_y = nil

local counter = nil

local text_titlekey = nil

function enum_song_update()
end

function enum_song_draw()
end

function createTexts()
    text_titlekey = createTitleTextureKey("Now enumerating songs.\n         Please wait...", font, 99999)
end

function reloadLanguage(lang)
    createTexts()
end

function loadAssets()
    config = loadConfig("Config.json")
    
    font = loadFontRenderer(getNum(config["font_size"]), "regular")

    enum_song = loadTexture("Enum_Song.png")
    configstage_enum_song = loadTexture("ConfigStage_Enum_Song.png")

    enum_song_x = getNum(config["enum_song"]["x"])
    enum_song_y = getNum(config["enum_song"]["y"])

    configstage_enum_song_x = getNum(config["configstage_enum_song"]["x"])
    configstage_enum_song_y = getNum(config["configstage_enum_song"]["y"])

    text_x = getNum(config["text"]["x"])
    text_y = getNum(config["text"]["y"])
    
    createTexts()
end

function init()
    counter = 0
end

function final()
end

function update()
    counter = counter + (1 * fps.deltaTime)
    if counter > 1 then
        counter = 0
    end
end

function draw()
    enum_song.Opacity = math.ceil(math.sin(counter * math.pi) * 255)
    enum_song:t2D_DisplayImage(enum_song_x, enum_song_y)

    if enumsongsinfo.bFromConfigStage then
        configstage_enum_song:t2D_DisplayImage(configstage_enum_song_x, configstage_enum_song_y)
        getTextTex(text_titlekey, false, false):t2D_DisplayImage(text_x, text_y)
    end
end
